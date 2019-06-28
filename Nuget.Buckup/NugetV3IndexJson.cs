using System.Collections.Generic;
using RestSharp.Deserializers;

namespace Nuget.Buckup
{
    public class NugetV3IndexJson
    {
        public List<NugetResource> Resources { get; set; }

        public string Version { get; set; }
    }

    public class NugetResource
    {
        [DeserializeAs(Name = "@id")]
        public string Id { get; set; }

        [DeserializeAs(Name = "@type")]
        public string Type { get; set; }
    }
}