using System;
using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using NuGet;
using RestSharp;
using RestSharp.Authenticators;

namespace Nuget.Buckup
{
    class Program
    {
        static void Main(string[] args)
        {
            var baseUrl = args[0];
            var tfsUserName = args[1];
            var tfsPwd = args[2];
            const string workDir = "Backup";
            const string packageHashAlgorithm = "sha512";
            var backupPath = $"{workDir}\\Packages";
            var cachePackageRootPath = $"{workDir}\\Caches";
            var cachePackagesBatchBuilder = new StringBuilder();

            if (baseUrl.ToLower().EndsWith("v3/index.json"))
            {
                Console.WriteLine("Detected v3 feed URL, try to find legacy v2 feed URL...");

                var client = new RestClient(baseUrl)
                {
                    Authenticator = new NtlmAuthenticator(tfsUserName, tfsPwd)
                };
                var request = new RestRequest(Method.GET);
                var response = client.Execute<NugetV3IndexJson>(request);

                if (response.IsSuccessful)
                {
                    var v2Feed = response.Data.Resources.FirstOrDefault(r => r.Type.Contains("LegacyGallery/2.0.0"));

                    if (v2Feed != null)
                    {
                        Console.WriteLine($"Found legacy v2 feed URL: {v2Feed.Id}");
                        baseUrl = v2Feed.Id;
                    }
                }
            }

            var packageRepository = PackageRepositoryFactory.Default.CreateRepository(baseUrl);
            var packages = packageRepository.GetPackages().ToList().Where(item => item.IsReleaseVersion());

            if (!Directory.Exists(backupPath))
            {
                Directory.CreateDirectory(backupPath);
            }

            if (!Directory.Exists(cachePackageRootPath))
            {
                Directory.CreateDirectory(cachePackageRootPath);
            }

            foreach (var package in packages)
            {
                var dataServicePackage = (DataServicePackage)package;
                var packageFileName = $"{package.Id}.{package.Version}.nupkg";
                var downloadPackagePath = $"{backupPath}\\{packageFileName}";

                cachePackagesBatchBuilder.AppendLine($"nuget.exe install {package.Id} -OutputDirectory tempPackages -Version {package.Version} -NonInteractive");

                var request = new RestRequest(dataServicePackage.DownloadUrl.Query, Method.GET)
                {
                    ResponseWriter = stream =>
                    {
                        using (var fileStream = File.Create(downloadPackagePath))
                        {
                            stream.CopyTo(fileStream);
                        }

                        var zipPackage = new ZipPackage(downloadPackagePath);
                        var cachePackagePath = $"{package.Id}\\{package.Version}".ToLower();
                        zipPackage.ExtractContents(new PhysicalFileSystem(cachePackageRootPath), cachePackagePath);
                        var packageHash = zipPackage.GetHash(packageHashAlgorithm);
                        var cachePackageFileName = $"{cachePackageRootPath}\\{cachePackagePath}\\{packageFileName.ToLower()}";
                        var fastZip = new FastZip();
                        fastZip.ExtractZip(downloadPackagePath, $"{cachePackageRootPath}\\{cachePackagePath}", @"\.nuspec$;\.nupkg$");
                        var nuspecFilePath = $"{cachePackageRootPath}\\{cachePackagePath}\\{package.Id}.nuspec";
                        if (File.Exists(nuspecFilePath))
                        {
                            File.Move(nuspecFilePath, nuspecFilePath.ToLower());
                        }

                        File.Copy(downloadPackagePath, cachePackageFileName, true);

                        var sha512FileName = $"{cachePackageFileName}.{packageHashAlgorithm}";
                        using (var fileStream = File.Create(sha512FileName))
                        {
                            var bytes = Encoding.UTF8.GetBytes(packageHash);
                            var ms = new MemoryStream(bytes);
                            ms.CopyTo(fileStream);
                        }
                    }
                };
                dataServicePackage.DownloadUrl.Query.Remove(0, 1).Split('&').ToList()
                    .ForEach(q =>
                    {
                        var kvStrings = q.Split('=');
                        request.AddQueryParameter(kvStrings[0], kvStrings[1]);
                    });

                var restClient = new RestClient(baseUrl)
                {
                    Authenticator = new NtlmAuthenticator(tfsUserName, tfsPwd)
                };
                var response = restClient.DownloadData(request);

                Console.WriteLine($"{packageFileName} is download.");
            }

            cachePackagesBatchBuilder.AppendLine("rmdir /Q /S tempPackages");

            using (var fileStream = File.Create($"{workDir}\\BuildNugetCacheBatch_{DateTime.Now:yyyyMMddHHmmss}.bat"))
            {
                var bytes = Encoding.UTF8.GetBytes(cachePackagesBatchBuilder.ToString());
                var ms = new MemoryStream(bytes);
                ms.CopyTo(fileStream);
            }
        }
    }
}
