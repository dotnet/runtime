using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Microsoft.DotNet.InternalAbstractions;
using Microsoft.DotNet.Cli.Build.Framework;
using Newtonsoft.Json;

using static Microsoft.DotNet.Cli.Build.Framework.BuildHelpers;
using static Microsoft.DotNet.Cli.Build.FS;

namespace Microsoft.DotNet.Cli.Build
{
    public class SharedFrameworkPublisher
    {
        public static string s_sharedFrameworkName = "Microsoft.NETCore.App";

        private string _sharedFrameworkTemplateSourceRoot;
        private string _sharedFrameworkNugetVersion;
        private string _sharedFrameworkRid;
        private string _sharedFrameworkTarget;
        private string _sharedFrameworkSourceRoot;
        private string _repoRoot;
        private string _corehostLockedDirectory;
        private string _corehostLatestDirectory;

        private Crossgen _crossgenUtil;
        private string _corehostPackageSource;

        public SharedFrameworkPublisher(
            string repoRoot,
            string corehostLockedDirectory,
            string corehostLatestDirectory,
            string corehostPackageSource,
            string sharedFrameworkNugetVersion,
            string sharedFrameworkRid,
            string sharedFrameworkTarget)
        {
            _repoRoot = repoRoot;
            _corehostLockedDirectory = corehostLockedDirectory;
            _corehostLatestDirectory = corehostLatestDirectory;
            _corehostPackageSource = corehostPackageSource;

            string crossgenRID = null;
            
            // If we are dealing with cross-targeting compilation, then specify the 
            // correct RID for crossgen to use when compiling SharedFramework.
            // TODO-ARM-Crossgen: Add ubuntu.14.04-arm and ubuntu.16.04-arm
            if ((sharedFrameworkRid == "win8-arm") || (sharedFrameworkRid == "win10-arm64") || (sharedFrameworkRid.StartsWith("linux-")))
            {
                crossgenRID = sharedFrameworkRid;
            }

            _crossgenUtil = new Crossgen(DependencyVersions.CoreCLRVersion, DependencyVersions.JitVersion, crossgenRID);

            _sharedFrameworkTemplateSourceRoot = Path.Combine(repoRoot, "src", "sharedframework", "framework");
            _sharedFrameworkNugetVersion = sharedFrameworkNugetVersion;

            _sharedFrameworkRid = sharedFrameworkRid;
            _sharedFrameworkTarget = sharedFrameworkTarget;

            _sharedFrameworkSourceRoot = GenerateSharedFrameworkProject(
                _sharedFrameworkNugetVersion,
                _sharedFrameworkTemplateSourceRoot,
                _sharedFrameworkRid,
                _sharedFrameworkTarget);
        }

        public static string GetSharedFrameworkPublishPath(string outputRootDirectory, string sharedFrameworkNugetVersion)
        {
            return Path.Combine(
                outputRootDirectory,
                "shared",
                s_sharedFrameworkName,
                sharedFrameworkNugetVersion);
        }
        
        public static string GetNetCoreAppRuntimeLibSymbolsPath(string symbolsRoot, string sharedFrameworkRid, string sharedFrameworkTarget)
        {
            return Path.Combine(symbolsRoot, s_sharedFrameworkName, "runtimes", sharedFrameworkRid, "lib", sharedFrameworkTarget);
        }

        public static string GetNetCoreAppRuntimeNativeSymbolsPath(string symbolsRoot, string sharedFrameworkRid)
        {
            return Path.Combine(symbolsRoot, s_sharedFrameworkName, "runtimes", sharedFrameworkRid, "native");
        }

        public static string GetNetCoreAppToolsSymbolsPath(string symbolsRoot)
        {
            return Path.Combine(symbolsRoot, s_sharedFrameworkName, "tools");
        }

        public void CopyMuxer(string sharedFrameworkPublishRoot)
        {
            File.Copy(
                Path.Combine(_corehostLockedDirectory, HostArtifactNames.DotnetHostBaseName),
                Path.Combine(sharedFrameworkPublishRoot, HostArtifactNames.DotnetHostBaseName), true);
        }

        public void CopyHostFxrToVersionedDirectory(string rootDirectory, string hostFxrVersion)
        {
            var hostFxrVersionedDirectory = Path.Combine(rootDirectory, "host", "fxr", hostFxrVersion);

            FS.Mkdirp(hostFxrVersionedDirectory);

            File.Copy(
                Path.Combine(_corehostLockedDirectory, HostArtifactNames.DotnetHostFxrBaseName),
                Path.Combine(hostFxrVersionedDirectory, HostArtifactNames.DotnetHostFxrBaseName), true);
        }

        public void PublishSharedFramework(string outputRootDirectory, string commitHash, DotNetCli dotnetCli, string hostFxrVersion)
        {
            dotnetCli.Restore(
                "--verbosity", "verbose",
                "--disable-parallel",
                "--infer-runtimes",
                "--fallbacksource", _corehostPackageSource)
                .WorkingDirectory(_sharedFrameworkSourceRoot)
                .Execute()
                .EnsureSuccessful();

            // We publish to a sub folder of the PublishRoot so tools like heat and zip can generate folder structures easier.
            string sharedFrameworkNameAndVersionRoot = GetSharedFrameworkPublishPath(outputRootDirectory, _sharedFrameworkNugetVersion);
            if (Directory.Exists(sharedFrameworkNameAndVersionRoot))
            {
                Utils.DeleteDirectory(sharedFrameworkNameAndVersionRoot);
            }

            dotnetCli.Publish(
                "--output", sharedFrameworkNameAndVersionRoot,
                "-r", _sharedFrameworkRid,
                _sharedFrameworkSourceRoot)
                .Execute()
                .EnsureSuccessful();

            // Clean up artifacts that dotnet-publish generates which we don't need
            PublishMutationUtilties.CleanPublishOutput(
                sharedFrameworkNameAndVersionRoot,
                "framework",
                new List<string> {"apphost", "hostfxr", "dotnet" },
                deleteRuntimeConfigJson: true,
                deleteDepsJson: false);

            // Rename the .deps file
            var destinationDeps = Path.Combine(sharedFrameworkNameAndVersionRoot, $"{s_sharedFrameworkName}.deps.json");
            File.Move(Path.Combine(sharedFrameworkNameAndVersionRoot, "framework.deps.json"), destinationDeps);
            PublishMutationUtilties.ChangeEntryPointLibraryName(destinationDeps, null);

            // Generate RID fallback graph
            CopyHostArtifactsToSharedFramework(sharedFrameworkNameAndVersionRoot);
            ProcessGeneratedDeps(dotnetCli, destinationDeps, new List<string> { $"runtime.{_sharedFrameworkRid}.microsoft.netcore.dotnetapphost", $"runtime.{_sharedFrameworkRid}.microsoft.netcore.dotnethostresolver", "microsoft.netcore.dotnetapphost", "microsoft.netcore.dotnethostresolver" });

            _crossgenUtil.CrossgenDirectory(sharedFrameworkNameAndVersionRoot, sharedFrameworkNameAndVersionRoot);

            // Generate .version file for sharedfx
            var version = _sharedFrameworkNugetVersion;
            var content = $@"{commitHash}{Environment.NewLine}{version}{Environment.NewLine}";
            File.WriteAllText(Path.Combine(sharedFrameworkNameAndVersionRoot, ".version"), content);

            // Populate symbols publish folder
            string sharedFrameworkNameAndVersionWithSymbolsRoot = $"{outputRootDirectory}.symbols";
            if (Directory.Exists(sharedFrameworkNameAndVersionWithSymbolsRoot))
            {
                Utils.DeleteDirectory(sharedFrameworkNameAndVersionWithSymbolsRoot);
            }
            Directory.CreateDirectory(sharedFrameworkNameAndVersionWithSymbolsRoot);

            // Copy symbols to publish folder
            List<string> pdbFiles = new List<string>();
            string symbolsRoot = Path.Combine(_repoRoot, "pkg", "bin", "symbols");
            string libPdbPath = GetNetCoreAppRuntimeLibSymbolsPath(symbolsRoot, _sharedFrameworkRid, _sharedFrameworkTarget);
            string nativePdbPath = GetNetCoreAppRuntimeNativeSymbolsPath(symbolsRoot, _sharedFrameworkRid);
            string toolsPdbPath = GetNetCoreAppToolsSymbolsPath(symbolsRoot);
            if (Directory.Exists(libPdbPath))
            {
                pdbFiles.AddRange(Directory.GetFiles(libPdbPath));
            }
            if(Directory.Exists(nativePdbPath))
            {
                pdbFiles.AddRange(Directory.GetFiles(nativePdbPath));
            }
            if (Directory.Exists(toolsPdbPath))
            {
                pdbFiles.AddRange(Directory.GetFiles(toolsPdbPath));
            }
            foreach (string pdbFile in pdbFiles)
            {
                string destinationPath = Path.Combine(sharedFrameworkNameAndVersionWithSymbolsRoot, Path.GetFileName(pdbFile));
                if (!File.Exists(destinationPath))
                {
                    File.Copy(pdbFile, destinationPath);
                }
            }

            return;
        }

        private void ProcessGeneratedDeps(DotNetCli dotnetCli, string destinationDeps, IReadOnlyList<string> packagesToBeRemoved)
        {
            string runtimeGraphGeneratorRuntime = null;
            switch (RuntimeEnvironment.OperatingSystemPlatform)
            {
                case Platform.Windows:
                    runtimeGraphGeneratorRuntime = "win";
                    break;
                case Platform.Linux:
                    runtimeGraphGeneratorRuntime = "linux";
                    break;
                case Platform.Darwin:
                    runtimeGraphGeneratorRuntime = "osx";
                    break;
            }
            if (!string.IsNullOrEmpty(runtimeGraphGeneratorRuntime))
            {
                var depsProcessorName = "DepsProcessor";
                var depsProcessorProject = Path.Combine(Dirs.RepoRoot, "setuptools", "independent", depsProcessorName);
                var depsProcessorOutput = Path.Combine(Dirs.Output, "setuptools", "independent", depsProcessorName);

                dotnetCli.Publish(
                    "--output", depsProcessorOutput,
                    depsProcessorProject).Execute().EnsureSuccessful();
                var depsProcessorExe = Path.Combine(depsProcessorOutput, $"{depsProcessorName}{Constants.ExeSuffix}");

                var args = new List<string>() { "--project", _sharedFrameworkSourceRoot, "--deps", destinationDeps, runtimeGraphGeneratorRuntime};

                foreach (var pkg in packagesToBeRemoved)
                {
                    args.Add("--remove");
                    args.Add(pkg);
                }

                Cmd(depsProcessorExe, args.ToArray())
                    .Execute()
                    .EnsureSuccessful();
            }
            else
            {
                throw new Exception($"Could not determine rid graph generation runtime for platform {RuntimeEnvironment.OperatingSystemPlatform}");
            }
        }

        private void CopyHostArtifactsToSharedFramework(string sharedFrameworkNameAndVersionRoot)
        {
            // Hostpolicy should be the latest and not the locked version as it is supposed to evolve for
            // the framework and has a tight coupling with coreclr's API in the framework.
            File.Copy(
                Path.Combine(_corehostLatestDirectory, HostArtifactNames.HostPolicyBaseName),
                Path.Combine(sharedFrameworkNameAndVersionRoot, HostArtifactNames.HostPolicyBaseName), true);
        }

        private string GenerateSharedFrameworkProject(
            string sharedFrameworkNugetVersion,
            string sharedFrameworkTemplatePath,
            string rid,
            string targetFramework)
        {
            string sharedFrameworkProjectPath = Path.Combine(Dirs.Intermediate, "sharedFramework", "framework");
            Utils.DeleteDirectory(sharedFrameworkProjectPath);
            CopyRecursive(sharedFrameworkTemplatePath, sharedFrameworkProjectPath, true);

            string templateFile = Path.Combine(sharedFrameworkProjectPath, "project.json.template");
            JObject sharedFrameworkProject = JsonUtils.ReadProject(templateFile);

            sharedFrameworkProject["dependencies"]["Microsoft.NETCore.App"] = sharedFrameworkNugetVersion;
            ((JObject)sharedFrameworkProject["runtimes"]).RemoveAll();
            sharedFrameworkProject["runtimes"][rid] = new JObject();
            ((JObject)sharedFrameworkProject["frameworks"]).RemoveAll();
            sharedFrameworkProject["frameworks"][targetFramework] = new JObject();

            string projectJsonPath = Path.Combine(sharedFrameworkProjectPath, "project.json");
            JsonUtils.WriteProject(sharedFrameworkProject, projectJsonPath);

            Rm(templateFile);

            return sharedFrameworkProjectPath;
        }
    }
}
