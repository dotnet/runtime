using Microsoft.DotNet.Cli.Build;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Cli.Build.Framework;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public class RepoDirectoriesProvider
    {
        private string _repoRoot;
        private string _artifacts;
        private string _hostArtifacts;
        private string _builtDotnet;
        private string _nugetPackages;
        private string _corehostPackages;
        private string _dotnetSDK;

        private string _targetRID;
        private string _buildRID;
        private string _buildArchitecture;
        private string _mnaVersion;

        public string BuildRID => _buildRID;
        public string BuildArchitecture => _buildArchitecture;
        public string TargetRID => _targetRID;
        public string MicrosoftNETCoreAppVersion => _mnaVersion;
        public string RepoRoot => _repoRoot;
        public string Artifacts => _artifacts;
        public string HostArtifacts => _hostArtifacts;
        public string BuiltDotnet => _builtDotnet;
        public string NugetPackages => _nugetPackages;
        public string CorehostPackages => _corehostPackages;
        public string DotnetSDK => _dotnetSDK;

        public RepoDirectoriesProvider(
            string repoRoot = null,
            string artifacts = null,
            string builtDotnet = null,
            string nugetPackages = null,
            string corehostPackages = null,
            string dotnetSdk = null)
        {
            _repoRoot = repoRoot ?? GetRepoRootDirectory();

            string baseArtifactsFolder = artifacts ?? Path.Combine(_repoRoot, "Bin");

            _targetRID = Environment.GetEnvironmentVariable("TEST_TARGETRID");
            _buildRID = Environment.GetEnvironmentVariable("BUILDRID");
            _buildArchitecture = Environment.GetEnvironmentVariable("BUILD_ARCHITECTURE");
            _mnaVersion = Environment.GetEnvironmentVariable("MNA_VERSION");

            string configuration = Environment.GetEnvironmentVariable("BUILD_CONFIGURATION");
            string osPlatformConfig = $"{_buildRID}.{configuration}";

            _dotnetSDK = dotnetSdk ?? Environment.GetEnvironmentVariable("DOTNET_SDK_PATH");

            if (!Directory.Exists(_dotnetSDK))
            {
                throw new InvalidOperationException("ERROR: Test SDK folder not found.");
            }

            _artifacts = Path.Combine(baseArtifactsFolder, osPlatformConfig);
            _hostArtifacts = artifacts ?? Path.Combine(_artifacts, "corehost");

            _nugetPackages = nugetPackages ?? Path.Combine(_repoRoot, "packages");

            _corehostPackages = corehostPackages ?? Path.Combine(_artifacts, "corehost");
            _builtDotnet = builtDotnet ?? Path.Combine(baseArtifactsFolder, "obj", osPlatformConfig, "sharedFrameworkPublish");
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
