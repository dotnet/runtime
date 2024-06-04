using System;
using System.IO;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public sealed class RepoDirectoriesProvider
    {
        public static readonly RepoDirectoriesProvider Default = new RepoDirectoriesProvider();

        // Paths computed by looking for the repo root
        public string BaseArtifactsFolder { get; }
        public string HostArtifacts { get; }
        public string HostTestArtifacts { get; }

        private RepoDirectoriesProvider()
        {
            string repoRoot = GetRepoRootDirectory();
            BaseArtifactsFolder = Path.Combine(repoRoot, "artifacts");

            string osPlatformConfig = $"{TestContext.BuildRID}.{TestContext.Configuration}";
            string artifacts = Path.Combine(BaseArtifactsFolder, "bin", osPlatformConfig);
            HostArtifacts = Path.Combine(artifacts, "corehost");
            HostTestArtifacts = Path.Combine(artifacts, "corehost_test");
        }

        private static string GetRepoRootDirectory()
        {
            string currentDirectory = Directory.GetCurrentDirectory();

            while (currentDirectory != null)
            {
                string gitDirOrFile = Path.Combine(currentDirectory, ".git");
                if (Directory.Exists(gitDirOrFile) || File.Exists(gitDirOrFile))
                {
                    break;
                }
                currentDirectory = Directory.GetParent(currentDirectory)?.FullName;
            }

            if (currentDirectory == null)
            {
                throw new Exception("Cannot find the git repository root");
            }

            return currentDirectory;
        }
    }
}
