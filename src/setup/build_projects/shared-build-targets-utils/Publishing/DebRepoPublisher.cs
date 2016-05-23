using System;
using System.IO;
using System.Net.Http;
using System.Text;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

using static Microsoft.DotNet.Cli.Build.Framework.BuildHelpers;

namespace Microsoft.DotNet.Cli.Build
{
    public class DebRepoPublisher
    {
        private const string _debianRevisionNumber = "1";
        private string _repoID;
        private string _uploadJsonDirectory;

        public DebRepoPublisher(string uploadJsonDirectory)
        {
            _uploadJsonDirectory = uploadJsonDirectory;
            _repoID = Environment.GetEnvironmentVariable("REPO_ID");
        }

        public void PublishDebFileToDebianRepo(string packageName, string packageVersion, string uploadUrl)
        {
            var uploadJson = GenerateUploadJsonFile(packageName, packageVersion, uploadUrl);

            Cmd(Path.Combine(Dirs.RepoRoot, "scripts", "publish", "repoapi_client.sh"), "-addpkg", uploadJson)
                    .Execute()
                    .EnsureSuccessful();
        }

        private string GenerateUploadJsonFile(string packageName, string packageVersion, string uploadUrl)
        {
            var uploadJson = Path.Combine(_uploadJsonDirectory, "package_upload.json");
            File.Delete(uploadJson);

            using (var fileStream = File.Create(uploadJson))
            {
                using (StreamWriter sw = new StreamWriter(fileStream))
                {
                    sw.WriteLine("{");
                    sw.WriteLine($"  \"name\":\"{packageName}\",");
                    sw.WriteLine($"  \"version\":\"{packageVersion}-{_debianRevisionNumber}\",");
                    sw.WriteLine($"  \"repositoryId\":\"{_repoID}\",");
                    sw.WriteLine($"  \"sourceUrl\":\"{uploadUrl}\"");
                    sw.WriteLine("}");
                }
            }

            return uploadJson;
        }
    }
}
