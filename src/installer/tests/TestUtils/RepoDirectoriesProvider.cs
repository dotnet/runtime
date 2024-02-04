using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public sealed class RepoDirectoriesProvider
    {
        public static readonly RepoDirectoriesProvider Default = new RepoDirectoriesProvider();

        // Values from test context can be overridden in constructor
        public string BuiltDotnet { get; }

        // Paths computed by looking for the repo root
        public string BaseArtifactsFolder { get; }
        public string HostArtifacts { get; }
        public string HostTestArtifacts { get; }

        // Paths used for building/publishing projects
        public string TestAssetsFolder { get; }
        public string NugetPackages { get; }
        public string DotnetSDK { get; }

        public RepoDirectoriesProvider(
            string builtDotnet = null)
        {
            string repoRoot = GetRepoRootDirectory();
            BaseArtifactsFolder = Path.Combine(repoRoot, "artifacts");

            string osPlatformConfig = $"{TestContext.BuildRID}.{TestContext.Configuration}";
            string artifacts = Path.Combine(BaseArtifactsFolder, "bin", osPlatformConfig);
            HostArtifacts = Path.Combine(artifacts, "corehost");
            HostTestArtifacts = Path.Combine(artifacts, "corehost_test");

            TestAssetsFolder = TestContext.GetTestContextVariable("TEST_ASSETS");
            DotnetSDK = TestContext.GetTestContextVariable("DOTNET_SDK_PATH");
            if (!Directory.Exists(DotnetSDK))
            {
                throw new InvalidOperationException("ERROR: Test SDK folder not found.");
            }

            NugetPackages = TestContext.GetTestContextVariable("NUGET_PACKAGES");

            BuiltDotnet = builtDotnet ?? TestContext.BuiltDotNet.BinPath;
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
