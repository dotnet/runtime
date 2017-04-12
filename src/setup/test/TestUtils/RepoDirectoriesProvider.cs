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

        private string _targetRID;

        public string TargetRID => _targetRID;
        public string RepoRoot => _repoRoot;
        public string Artifacts => _artifacts;
        public string HostArtifacts => _hostArtifacts;
        public string BuiltDotnet => _builtDotnet;
        public string NugetPackages => _nugetPackages;
        public string CorehostPackages => _corehostPackages;

        public RepoDirectoriesProvider(
            string repoRoot = null,
            string artifacts = null,
            string builtDotnet = null,
            string nugetPackages = null,
            string corehostPackages = null)
        {
            _repoRoot = repoRoot ?? Path.Combine(Directory.GetCurrentDirectory(), "..", "..");

            string baseArtifactsFolder = artifacts ?? Path.Combine(_repoRoot, "artifacts");

            _targetRID = Environment.GetEnvironmentVariable("TEST_TARGETRID");

            _artifacts = Path.Combine(baseArtifactsFolder, _targetRID);

            _hostArtifacts = artifacts ?? Path.Combine(_artifacts, "corehost");
            _nugetPackages = nugetPackages ?? Path.Combine(_repoRoot, ".nuget", "packages");
            _corehostPackages = corehostPackages ?? Path.Combine(_artifacts, "corehost");
            _builtDotnet = builtDotnet ?? Path.Combine(_artifacts, "intermediate", "sharedFrameworkPublish");
        }
    }
}
