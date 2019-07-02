using System;
using System.Collections;
using System.IO;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public class RepoDirectoriesProvider
    {
        public string BuildRID { get; }
        public string BuildArchitecture { get; }
        public string TargetRID { get; }
        public string MicrosoftNETCoreAppVersion { get; }
        public string RepoRoot { get; }
        public string Artifacts { get; }
        public string HostArtifacts { get; }
        public string BuiltDotnet { get; }
        public string NugetPackages { get; }
        public string CorehostPackages { get; }
        public string DotnetSDK { get; }

        public RepoDirectoriesProvider(
            string repoRoot = null,
            string artifacts = null,
            string builtDotnet = null,
            string nugetPackages = null,
            string corehostPackages = null,
            string dotnetSdk = null,
            string microsoftNETCoreAppVersion = null)
        {
            RepoRoot = repoRoot ?? GetRepoRootDirectory();

            string baseArtifactsFolder = artifacts ?? Path.Combine(RepoRoot, "artifacts");
            string baseBinFolder = artifacts ?? Path.Combine(baseArtifactsFolder, "bin");
            string baseObjFolder = artifacts ?? Path.Combine(baseArtifactsFolder, "obj");

            TargetRID = Environment.GetEnvironmentVariable("TEST_TARGETRID");
            BuildRID = Environment.GetEnvironmentVariable("BUILDRID");
            BuildArchitecture = Environment.GetEnvironmentVariable("BUILD_ARCHITECTURE");
            MicrosoftNETCoreAppVersion = microsoftNETCoreAppVersion ?? Environment.GetEnvironmentVariable("MNA_VERSION");

            string configuration = Environment.GetEnvironmentVariable("BUILD_CONFIGURATION");
            string osPlatformConfig = $"{BuildRID}.{configuration}";

            DotnetSDK = dotnetSdk ?? Environment.GetEnvironmentVariable("DOTNET_SDK_PATH");

            if (!Directory.Exists(DotnetSDK))
            {
                throw new InvalidOperationException("ERROR: Test SDK folder not found.");
            }

            Artifacts = Path.Combine(baseBinFolder, osPlatformConfig);
            HostArtifacts = artifacts ?? Path.Combine(Artifacts, "corehost");

            NugetPackages = nugetPackages ??
                Environment.GetEnvironmentVariable("NUGET_PACKAGES") ??
                Path.Combine(RepoRoot, ".packages");

            CorehostPackages = corehostPackages ?? Path.Combine(Artifacts, "corehost");
            BuiltDotnet = builtDotnet ?? Path.Combine(baseObjFolder, osPlatformConfig, "sharedFrameworkPublish");
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
