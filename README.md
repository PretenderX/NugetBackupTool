# NugetBackupTool
备份Nuget源的小工具（A tool to backup nuget packages of specified feed）

命令行参数说明：
第1个参数为nuget源的index.json的地址
第2个参数为访问nuget源的用户名
第3个参数为访问nuget源的密码

使用示例：
Nuget.Buckup.exe http://host/.../index.json yourname yourpassword

执行成功后会在当前目录建立Backup_[时间戳]的文件夹，内部文件与文件夹说明：
- index.json - 备份的nuget源的索引文件内容
- Packages - 包含备份目标源中所有的nuget包
- Caches - 包含备份目标源中所有的nuget包的解包内容，可当作本地nuget源进行配置
- BuildNugetCacheBatch.bat - 执行该批处理会将备份目标源的所有nuget包在本地执行一遍安装，以达到缓存所有包到nuget默认缓存目录中的目的
- PublishNugetPackagesBatch.bat - 执行该批处理会将备份好的所有包发布到指定的新nuget源，达到迁移nuget源的目的，该批处理的第1参为系统中配置好的源的名称，第2参为发布包时的Api Key

备注：
本工具目前只针对Azure DevOps（VSTS）的包管理器进行过测试，欢迎PR。
