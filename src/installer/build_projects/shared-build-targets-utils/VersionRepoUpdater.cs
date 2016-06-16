using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Cli.Build
{
    public class VersionRepoUpdater
    {
        private static Regex s_nugetFileRegex = new Regex("^(?<id>.*?)\\.(?<version>([0-9]+\\.)?[0-9]+\\.[0-9]+(-(?<prerelease>[A-z0-9-]+))?)(?<symbols>\\.symbols)?\\.nupkg$");

        private string _gitHubAuthToken;
        private string _gitHubUser;
        private string _gitHubEmail;
        private string _versionsRepoOwner;
        private string _versionsRepo;

        public VersionRepoUpdater(
            string gitHubAuthToken,
            string gitHubUser = null,
            string gitHubEmail = null,
            string versionRepoOwner = null,
            string versionsRepo = null)
        {
            if (string.IsNullOrEmpty(gitHubAuthToken))
            {
                throw new ArgumentNullException(nameof(gitHubAuthToken));
            }

            _gitHubAuthToken = gitHubAuthToken;
            _gitHubUser = gitHubUser ?? "dotnet-bot";
            _gitHubEmail = gitHubEmail ?? "dotnet-bot@microsoft.com";
            _versionsRepoOwner = versionRepoOwner ?? "dotnet";
            _versionsRepo = versionsRepo ?? "versions";
        }

        public async Task UpdatePublishedVersions(string nupkgFilePath, string versionsRepoPath)
        {
            List<NuGetPackageInfo> publishedPackages = GetPackageInfo(nupkgFilePath);

            string packageInfoFileContent = string.Join(
                Environment.NewLine,
                publishedPackages
                    .OrderBy(t => t.Id)
                    .Select(t => $"{t.Id} {t.Version}"));

            string prereleaseVersion = publishedPackages
                .Where(t => !string.IsNullOrEmpty(t.Prerelease))
                .Select(t => t.Prerelease)
                .FirstOrDefault();

            string packageInfoFilePath = $"{versionsRepoPath}_Packages.txt";
            string message = $"Adding package info to {packageInfoFilePath} for {prereleaseVersion}";

            await UpdateGitHubFile(packageInfoFilePath, packageInfoFileContent, message);
        }

        private static List<NuGetPackageInfo> GetPackageInfo(string nupkgFilePath)
        {
            List<NuGetPackageInfo> packages = new List<NuGetPackageInfo>();

            foreach (string filePath in Directory.GetFiles(nupkgFilePath, "*.nupkg"))
            {
                Match match = s_nugetFileRegex.Match(Path.GetFileName(filePath));

                // only look for non-symbols packages
                if (string.IsNullOrEmpty(match.Groups["symbols"].Value))
                {
                    packages.Add(new NuGetPackageInfo()
                    {
                        Id = match.Groups["id"].Value,
                        Version = match.Groups["version"].Value,
                        Prerelease = match.Groups["prerelease"].Value,
                    });
                }
            }

            return packages;
        }

        private async Task UpdateGitHubFile(string path, string newFileContent, string commitMessage)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
                client.DefaultRequestHeaders.Add("Authorization", $"token {_gitHubAuthToken}");
                client.DefaultRequestHeaders.Add("User-Agent", _gitHubUser);

                string fileUrl = $"https://api.github.com/repos/{_versionsRepoOwner}/{_versionsRepo}/contents/{path}";

                Console.WriteLine($"Getting the 'sha' of the current contents of file '{_versionsRepoOwner}/{_versionsRepo}/{path}'");

                string currentFile = await client.GetStringAsync(fileUrl);
                string currentSha = JObject.Parse(currentFile)["sha"].ToString();

                Console.WriteLine($"Got 'sha' value of '{currentSha}'");

                Console.WriteLine($"Request to update file '{_versionsRepoOwner}/{_versionsRepo}/{path}' contents to:");
                Console.WriteLine(newFileContent);

                string updateFileBody = $@"{{
  ""message"": ""{commitMessage}"",
  ""committer"": {{
    ""name"": ""{_gitHubUser}"",
    ""email"": ""{_gitHubEmail}""
  }},
  ""content"": ""{ToBase64(newFileContent)}"",
  ""sha"": ""{currentSha}""
}}";

                Console.WriteLine("Sending request...");
                StringContent content = new StringContent(updateFileBody);

                using (HttpResponseMessage response = await client.PutAsync(fileUrl, content))
                {
                    response.EnsureSuccessStatusCode();
                    Console.WriteLine("Updated the file successfully...");
                }
            }
        }

        private static string ToBase64(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        }

        private class NuGetPackageInfo
        {
            public string Id { get; set; }
            public string Version { get; set; }
            public string Prerelease { get; set; }
        }
    }
}
