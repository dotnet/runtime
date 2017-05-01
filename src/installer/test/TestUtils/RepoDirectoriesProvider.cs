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

        public string BuildRID => _buildRID;
        public string TargetRID => _targetRID;
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
            Console.WriteLine("Current Dir: " + Directory.GetCurrentDirectory());
            _repoRoot = repoRoot ?? Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.Parent.Parent.FullName;
            Console.WriteLine("Repo Root: " + _repoRoot);

            string baseArtifactsFolder = artifacts ?? Path.Combine(_repoRoot, "Bin");
            
            _targetRID = Environment.GetEnvironmentVariable("TEST_TARGETRID");
            Console.WriteLine("Test Target RID: " + _targetRID);
            _buildRID = Environment.GetEnvironmentVariable("BUILDRID");

            _dotnetSDK = dotnetSdk ?? Path.Combine(baseArtifactsFolder, "tests", _targetRID+".Debug", "Tools", "dotnetcli");
            if(!Directory.Exists(_dotnetSDK))
                _dotnetSDK = dotnetSdk ?? Path.Combine(baseArtifactsFolder, "tests", _targetRID+".Release", "Tools", "dotnetcli");

            _artifacts = Path.Combine(baseArtifactsFolder, _buildRID+".Debug");
            if(!Directory.Exists(_artifacts))
                _artifacts = Path.Combine(baseArtifactsFolder, _buildRID+".Release");
            _hostArtifacts = artifacts ?? Path.Combine(_artifacts, "corehost");

            _nugetPackages = nugetPackages ?? Path.Combine(_repoRoot, "packages");

            _corehostPackages = corehostPackages ?? Path.Combine(_artifacts, "corehost");

            _builtDotnet = builtDotnet ?? Path.Combine(baseArtifactsFolder, "obj", _buildRID+".Debug", "sharedFrameworkPublish");
            if(!Directory.Exists(_builtDotnet))
                _builtDotnet = builtDotnet ?? Path.Combine(baseArtifactsFolder, "obj", _buildRID+".Release", "sharedFrameworkPublish");
        }
    }
}
