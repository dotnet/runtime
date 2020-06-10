using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public class RepoDirectoriesProvider
    {
        public string BuildRID { get; }
        public string BuildArchitecture { get; }
        public string TargetRID { get; }
        public string MicrosoftNETCoreAppVersion { get; }
        public string TestAssetsFolder { get; }
        public string Configuration { get; }
        public string RepoRoot { get; }
        public string BaseArtifactsFolder { get; }
        public string BaseBinFolder { get; }
        public string BaseObjFolder { get; }
        public string Artifacts { get; }
        public string HostArtifacts { get; }
        public string BuiltDotnet { get; }
        public string NugetPackages { get; }
        public string CorehostPackages { get; }
        public string DotnetSDK { get; }

        private string _testContextVariableFilePath { get; }
        private ImmutableDictionary<string, string> _testContextVariables { get; }

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

            _testContextVariableFilePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "TestContextVariables.txt");

            _testContextVariables = File.ReadAllLines(_testContextVariableFilePath)
                .ToImmutableDictionary(
                    line => line.Substring(0, line.IndexOf('=')),
                    line => line.Substring(line.IndexOf('=') + 1),
                    StringComparer.OrdinalIgnoreCase);

            BaseArtifactsFolder = artifacts ?? Path.Combine(RepoRoot, "artifacts");
            BaseBinFolder = artifacts ?? Path.Combine(BaseArtifactsFolder, "bin");
            BaseObjFolder = artifacts ?? Path.Combine(BaseArtifactsFolder, "obj");

            TargetRID = GetTestContextVariable("TEST_TARGETRID");
            BuildRID = GetTestContextVariable("BUILDRID");
            BuildArchitecture = GetTestContextVariable("BUILD_ARCHITECTURE");
            MicrosoftNETCoreAppVersion = microsoftNETCoreAppVersion ?? GetTestContextVariable("MNA_VERSION");
            TestAssetsFolder = GetTestContextVariable("TEST_ASSETS");

            Configuration = GetTestContextVariable("BUILD_CONFIGURATION");
            string osPlatformConfig = $"{BuildRID}.{Configuration}";

            DotnetSDK = dotnetSdk ?? GetTestContextVariable("DOTNET_SDK_PATH");

            if (!Directory.Exists(DotnetSDK))
            {
                throw new InvalidOperationException("ERROR: Test SDK folder not found.");
            }

            Artifacts = Path.Combine(BaseBinFolder, osPlatformConfig);
            HostArtifacts = artifacts ?? Path.Combine(Artifacts, "corehost");

            NugetPackages = nugetPackages ??
                GetTestContextVariable("NUGET_PACKAGES") ??
                Path.Combine(RepoRoot, ".packages");

            CorehostPackages = corehostPackages ?? Path.Combine(Artifacts, "corehost");
            BuiltDotnet = builtDotnet ?? Path.Combine(BaseObjFolder, osPlatformConfig, "sharedFrameworkPublish");
        }

        public string GetTestContextVariable(string name)
        {
            return GetTestContextVariableOrNull(name) ?? throw new ArgumentException(
                $"Unable to find variable '{name}' in " +
                $"test context variable file '{_testContextVariableFilePath}'");
        }

        public string GetTestContextVariableOrNull(string name)
        {
            // Allow env var override, although normally the test context variables file is used.
            // Don't accept NUGET_PACKAGES env override specifically: Arcade sets this and it leaks
            // in during build.cmd/sh runs, replacing the test-specific dir.
            if (!name.Equals("NUGET_PACKAGES", StringComparison.OrdinalIgnoreCase))
            {
                if (Environment.GetEnvironmentVariable(name) is string envValue)
                {
                    return envValue;
                }
            }

            if (_testContextVariables.TryGetValue(name, out string value))
            {
                return value;
            }

            return null;
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
