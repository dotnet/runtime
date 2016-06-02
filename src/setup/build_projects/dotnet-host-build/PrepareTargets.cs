﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.InternalAbstractions;
using Newtonsoft.Json.Linq;
using Microsoft.DotNet.Cli.Build;
using static Microsoft.DotNet.Cli.Build.Framework.BuildHelpers;
using static Microsoft.DotNet.Cli.Build.FS;
using static Microsoft.DotNet.Cli.Build.Utils;

namespace Microsoft.DotNet.Host.Build
{
    public class PrepareTargets
    {
        [Target(nameof(Init))]
        public static BuildTargetResult Prepare(BuildTargetContext c) => c.Success();

        [Target(nameof(CheckPrereqCmakePresent), nameof(CheckPlatformDependencies))]
        public static BuildTargetResult CheckPrereqs(BuildTargetContext c) => c.Success();

        [Target(nameof(CheckCoreclrPlatformDependencies))]
        public static BuildTargetResult CheckPlatformDependencies(BuildTargetContext c) => c.Success();

        [Target(nameof(CheckUbuntuCoreclrAndCoreFxDependencies), nameof(CheckCentOSCoreclrAndCoreFxDependencies))]
        public static BuildTargetResult CheckCoreclrPlatformDependencies(BuildTargetContext c) => c.Success();

        // All major targets will depend on this in order to ensure variables are set up right if they are run independently
        [Target(
            nameof(GenerateVersions), 
            nameof(CheckPrereqs), 
            nameof(LocateStage0), 
            nameof(ExpectedBuildArtifacts),
            nameof(RestorePackages),
            nameof(PackDotnetDebTool))]
        public static BuildTargetResult Init(BuildTargetContext c)
        {
            var configEnv = Environment.GetEnvironmentVariable("CONFIGURATION");

            if (string.IsNullOrEmpty(configEnv))
            {
                configEnv = "Debug";
            }

            c.BuildContext["Configuration"] = configEnv;
            c.BuildContext["Channel"] = Environment.GetEnvironmentVariable("CHANNEL");

            c.Info($"Building {c.BuildContext["Configuration"]} to: {Dirs.Output}");
            c.Info("Build Environment:");
            c.Info($" Operating System: {RuntimeEnvironment.OperatingSystem} {RuntimeEnvironment.OperatingSystemVersion}");
            c.Info($" Platform: {RuntimeEnvironment.OperatingSystemPlatform}");

            return c.Success();
        }

        [Target]
        [BuildPlatforms(BuildPlatform.Ubuntu)]
        public static BuildTargetResult PackDotnetDebTool(BuildTargetContext c)
        {
            var dotnet = DotNetCli.Stage0;
            var versionSuffix = c.BuildContext.Get<BuildVersion>("BuildVersion").CommitCountString;

            dotnet.Pack(
                    Path.Combine(Dirs.RepoRoot, "tools", "dotnet-deb-tool", "project.json"),
                    "--output", Dirs.PackagesIntermediate,
                    "--version-suffix", versionSuffix)
                    .Execute()
                    .EnsureSuccessful();

            var packageFiles = Directory.EnumerateFiles(Dirs.PackagesIntermediate, "dotnet-deb-tool.*.nupkg");

            foreach (var packageFile in packageFiles)
            {
                if (!packageFile.EndsWith(".symbols.nupkg"))
                {
                    var destinationPath = Path.Combine(Dirs.Packages, Path.GetFileName(packageFile));
                    File.Copy(packageFile, destinationPath, overwrite: true);
                }
            }

            return c.Success();
        }

        [Target]
        public static BuildTargetResult GenerateVersions(BuildTargetContext c)
        {
            var commitCount = GitUtils.GetCommitCount();

            var commitHash = GitUtils.GetCommitHash(); 

            var branchInfo = new BranchInfo(Dirs.RepoRoot);

            var hostVersion = new HostVersion()
            {
                ReleaseSuffix = branchInfo.Entries["RELEASE_SUFFIX"],
                CommitCount = commitCount
            };

            var buildVersion = new BuildVersion()
            {
                Major = int.Parse(branchInfo.Entries["MAJOR_VERSION"]),
                Minor = int.Parse(branchInfo.Entries["MINOR_VERSION"]),
                Patch = int.Parse(branchInfo.Entries["PATCH_VERSION"]),
                ReleaseSuffix = branchInfo.Entries["RELEASE_SUFFIX"],
                CommitCount = commitCount
            };

            c.BuildContext["BuildVersion"] = buildVersion;
            c.BuildContext["HostVersion"] = hostVersion;
            c.BuildContext["CommitHash"] = commitHash;
            c.BuildContext["SharedFrameworkNugetVersion"] = buildVersion.NetCoreAppVersion;

            c.Info($"Building Version: {hostVersion.LatestHostVersion.WithoutSuffix} (NuGet Packages: {hostVersion.LatestHostVersion})");
            c.Info($"From Commit: {commitHash}");

            return c.Success();
        }

        [Target]
        public static BuildTargetResult LocateStage0(BuildTargetContext c)
        {
            // We should have been run in the repo root, so locate the stage 0 relative to current directory
            var stage0 = DotNetCli.Stage0.BinPath;

            if (!Directory.Exists(stage0))
            {
                return c.Failed($"Stage 0 directory does not exist: {stage0}");
            }

            // Identify the version
            string versionFile = Directory.GetFiles(stage0, ".version", SearchOption.AllDirectories).FirstOrDefault();

            if (string.IsNullOrEmpty(versionFile))
            {
                throw new Exception($"'.version' file not found in '{stage0}' folder");
            }

            var version = File.ReadAllLines(versionFile);
            c.Info($"Using Stage 0 Version: {version[1]}");

            return c.Success();
        }

        [Target]
        public static BuildTargetResult ExpectedBuildArtifacts(BuildTargetContext c)
        {
            var config = Environment.GetEnvironmentVariable("CONFIGURATION");
            var versionBadgeName = $"sharedfx_{Monikers.GetBadgeMoniker()}_{config}_version_badge.svg";
            c.BuildContext["VersionBadge"] = Path.Combine(Dirs.Output, versionBadgeName);

            var sharedFrameworkVersion = c.BuildContext.Get<string>("SharedFrameworkNugetVersion");
            var hostVersion = c.BuildContext.Get<HostVersion>("HostVersion").LockedHostVersion.ToString();

            AddInstallerArtifactToContext(c, "dotnet-host", "SharedHost", hostVersion);
            AddInstallerArtifactToContext(c, "dotnet-sharedframework", "SharedFramework", sharedFrameworkVersion);
            AddInstallerArtifactToContext(c, "dotnet", "CombinedFrameworkHost", sharedFrameworkVersion);

            return c.Success();
        }

        private static void AddInstallerArtifactToContext(
            BuildTargetContext c,
            string artifactPrefix,
            string contextPrefix,
            string version)
        {
            var productName = Monikers.GetProductMoniker(c, artifactPrefix, version);

            var extension = CurrentPlatform.IsWindows ? ".zip" : ".tar.gz";
            c.BuildContext[contextPrefix + "CompressedFile"] = Path.Combine(Dirs.Packages, productName + extension);

            string installer = "";
            switch (CurrentPlatform.Current)
            {
                case BuildPlatform.Windows:
                    if (contextPrefix.Contains("Combined"))
                    {
                        installer = productName + ".exe";
                    }
                    else
                    {
                        installer = productName + ".msi";
                    }
                    break;
                case BuildPlatform.OSX:
                    installer = productName + ".pkg";
                    break;
                case BuildPlatform.Ubuntu:
                    installer = productName + ".deb";
                    break;
                default:
                    break;
            }

            if (!string.IsNullOrEmpty(installer))
            {
                c.BuildContext[contextPrefix + "InstallerFile"] = Path.Combine(Dirs.Packages, installer);
            }

        }

        [Target]
        [BuildPlatforms(BuildPlatform.Ubuntu, "14.04")]
        public static BuildTargetResult CheckUbuntuCoreclrAndCoreFxDependencies(BuildTargetContext c)
        {
            var errorMessageBuilder = new StringBuilder();
            var stage0 = DotNetCli.Stage0.BinPath;

            foreach (var package in PackageDependencies.UbuntuCoreclrAndCoreFxDependencies)
            {
                if (!AptDependencyUtility.PackageIsInstalled(package))
                {
                    errorMessageBuilder.Append($"Error: Coreclr package dependency {package} missing.");
                    errorMessageBuilder.Append(Environment.NewLine);
                    errorMessageBuilder.Append($"-> install with apt-get install {package}");
                    errorMessageBuilder.Append(Environment.NewLine);
                }
            }

            if (errorMessageBuilder.Length == 0)
            {
                return c.Success();
            }
            else
            {
                return c.Failed(errorMessageBuilder.ToString());
            }
        }

        [Target]
        [BuildPlatforms(BuildPlatform.CentOS)]
        public static BuildTargetResult CheckCentOSCoreclrAndCoreFxDependencies(BuildTargetContext c)
        {
            var errorMessageBuilder = new StringBuilder();

            foreach (var package in PackageDependencies.CentosCoreclrAndCoreFxDependencies)
            {
                if (!YumDependencyUtility.PackageIsInstalled(package))
                {
                    errorMessageBuilder.Append($"Error: Coreclr package dependency {package} missing.");
                    errorMessageBuilder.Append(Environment.NewLine);
                    errorMessageBuilder.Append($"-> install with yum install {package}");
                    errorMessageBuilder.Append(Environment.NewLine);
                }
            }

            if (errorMessageBuilder.Length == 0)
            {
                return c.Success();
            }
            else
            {
                return c.Failed(errorMessageBuilder.ToString());
            }
        }

        [Target]
        public static BuildTargetResult RestorePackages(BuildTargetContext c)
        {
            var dotnet = DotNetCli.Stage0;

            dotnet.Restore("--verbosity", "verbose", "--disable-parallel")
                .WorkingDirectory(Path.Combine(c.BuildContext.BuildDirectory, "tools"))
                .Execute()
                .EnsureSuccessful();
                
            return c.Success();
        }

        [Target]
        public static BuildTargetResult CheckPrereqCmakePresent(BuildTargetContext c)
        {
            try
            {
                Command.Create("cmake", "--version")
                    .CaptureStdOut()
                    .CaptureStdErr()
                    .Execute();
            }
            catch (Exception ex)
            {
                string message = $@"Error running cmake: {ex.Message}
cmake is required to build the native host 'corehost'";
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    message += Environment.NewLine + "Download it from https://www.cmake.org";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    message += Environment.NewLine + "Ubuntu: 'sudo apt-get install cmake'";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    message += Environment.NewLine + "OS X w/Homebrew: 'brew install cmake'";
                }
                return c.Failed(message);
            }

            return c.Success();
        }
    }
}
