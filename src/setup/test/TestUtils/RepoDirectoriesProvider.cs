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
        private string _corehostDummyPackages;

        public string RepoRoot => _repoRoot;
        public string Artifacts => _artifacts;
        public string HostArtifacts => _hostArtifacts;
        public string BuiltDotnet => _builtDotnet;
        public string NugetPackages => _nugetPackages;
        public string CorehostPackages => _corehostPackages;
        public string CorehostDummyPackages => _corehostDummyPackages;

        public RepoDirectoriesProvider(
            string repoRoot = null,
            string artifacts = null,
            string builtDotnet = null,
            string nugetPackages = null,
            string corehostPackages = null,
            string corehostDummyPackages = null)
        {
            _repoRoot = repoRoot ?? Path.Combine(Directory.GetCurrentDirectory(), "..", "..");
            
            string baseArtifactsFolder = artifacts ?? Path.Combine(_repoRoot, "artifacts");

            var targetRID = Environment.GetEnvironmentVariable("TEST_TARGETRID");

            _artifacts = Path.Combine(baseArtifactsFolder, targetRID);

            _hostArtifacts = artifacts ?? Path.Combine(_artifacts, "corehost");
            _nugetPackages = nugetPackages ?? Path.Combine(_repoRoot, ".nuget", "packages");
            _corehostPackages = corehostPackages ?? Path.Combine(_artifacts, "corehost");
            _corehostDummyPackages = corehostDummyPackages ?? Path.Combine(_artifacts, "corehostdummypackages");
            _builtDotnet = builtDotnet ?? Path.Combine(_artifacts, "intermediate", "sharedFrameworkPublish");
        }
    }
}
