using System;
using System.IO;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public sealed class RepoDirectoriesProvider
    {
        public static readonly RepoDirectoriesProvider Default = new RepoDirectoriesProvider();

        // Paths computed by looking for the repo root or helix correlation payload
        public string BaseArtifactsFolder { get; }
        public string HostArtifacts { get; }
        public string HostTestArtifacts { get; }

        private RepoDirectoriesProvider()
        {
            string helixPayload = Environment.GetEnvironmentVariable("HELIX_CORRELATION_PAYLOAD");
            if (helixPayload != null)
            {
                // See src/installer/tests/helixpublish.proj
                HostArtifacts = Path.Combine(helixPayload, "host_bin");
                HostTestArtifacts = Path.Combine(helixPayload, "host_test_bin");
            }
            else
            {
                string repoRoot = GetRepoRootDirectory();
                BaseArtifactsFolder = Path.Combine(repoRoot, "artifacts");

                string osPlatformConfig = $"{HostTestContext.BuildRID}.{HostTestContext.Configuration}";
                string artifacts = Path.Combine(BaseArtifactsFolder, "bin", osPlatformConfig);
                HostArtifacts = Path.Combine(artifacts, "corehost");
                HostTestArtifacts = Path.Combine(artifacts, "corehost_test");
            }
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
