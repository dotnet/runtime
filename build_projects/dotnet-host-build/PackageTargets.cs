using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.InternalAbstractions;
using static Microsoft.DotNet.Cli.Build.Framework.BuildHelpers;

namespace Microsoft.DotNet.Host.Build
{
    public static class PackageTargets
    {
        public static readonly string[] ProjectsToPack  = new string[]
        {
            "Microsoft.DotNet.PlatformAbstractions",            
            "Microsoft.Extensions.DependencyModel",
        };

        [Target(
            nameof(PackageTargets.CopySharedHostLayout),
            nameof(PackageTargets.CopyHostFxrLayout),
            nameof(PackageTargets.CopySharedFxLayout),
            nameof(PackageTargets.CopyCombinedMuxerHostFxrFrameworkLayout))]
        public static BuildTargetResult InitPackage(BuildTargetContext c)
        {
            Directory.CreateDirectory(Dirs.Packages);
            return c.Success();
        }

        [Target(nameof(PrepareTargets.Init),
            nameof(PackageTargets.InitPackage),
            nameof(PackageTargets.GenerateVersionBadge),
            nameof(PackageTargets.GenerateCompressedFile),
            nameof(InstallerTargets.GenerateInstaller),
            nameof(PackageTargets.GenerateNugetPackages),
            nameof(InstallerTargets.TestInstaller))]
        [Environment("DOTNET_BUILD_SKIP_PACKAGING", null, "0", "false")]
        public static BuildTargetResult Package(BuildTargetContext c)
        {
            return c.Success();
        }

        [Target]
        public static BuildTargetResult GenerateVersionBadge(BuildTargetContext c)
        {
            var sharedFrameworkNugetVersion = c.BuildContext.Get<string>("SharedFrameworkNugetVersion");
            var versionSvg = Path.Combine(Dirs.RepoRoot, "resources", "images", "version_badge.svg");
            var outputVersionSvg = c.BuildContext.Get<string>("VersionBadge");

            var versionSvgContent = File.ReadAllText(versionSvg);
            versionSvgContent = versionSvgContent.Replace("ver_number", sharedFrameworkNugetVersion);
            File.WriteAllText(outputVersionSvg, versionSvgContent);

            return c.Success();
        }
        
        [Target]
        public static BuildTargetResult CopySharedHostLayout(BuildTargetContext c)
        {
            var sharedHostRoot = Path.Combine(Dirs.Output, "obj", "sharedHost");
            if (Directory.Exists(sharedHostRoot))
            {
                Utils.DeleteDirectory(sharedHostRoot);
            }

            Directory.CreateDirectory(sharedHostRoot);

            string sharedFrameworkPublishPath = GetSharedFrameworkPublishPath(c);

            foreach (var file in Directory.GetFiles(sharedFrameworkPublishPath, "*", SearchOption.TopDirectoryOnly))
            {
                var destFile = file.Replace(sharedFrameworkPublishPath, sharedHostRoot);
                File.Copy(file, destFile, true);
                c.Warn(destFile);
            }
            FixPermissions(sharedHostRoot);

            File.Copy(
                Path.Combine(Dirs.RepoRoot, "resources", "ThirdPartyNotices.txt"),
                Path.Combine(sharedHostRoot, "ThirdPartyNotices.txt"));

            File.Copy(
                Path.Combine(Dirs.RepoRoot, "resources", "LICENSE.txt"),
                Path.Combine(sharedHostRoot, "LICENSE.txt"));

            c.BuildContext["SharedHostPublishRoot"] = sharedHostRoot;
            return c.Success();
        }

        [Target]
        public static BuildTargetResult CopyHostFxrLayout(BuildTargetContext c)
        {
            var hostFxrRoot = Path.Combine(Dirs.Output, "obj", "hostFxr");
            if (Directory.Exists(hostFxrRoot))
            {
                Utils.DeleteDirectory(hostFxrRoot);
            }

            Directory.CreateDirectory(hostFxrRoot);

            string srcHostDir = Path.Combine(GetSharedFrameworkPublishPath(c), "host");
            string destHostDir = Path.Combine(hostFxrRoot, "host");

            FS.CopyRecursive(srcHostDir, destHostDir);
            FixPermissions(hostFxrRoot);

            c.BuildContext["HostFxrPublishRoot"] = hostFxrRoot;
            return c.Success();
        }

        [Target]
        public static BuildTargetResult CopySharedFxLayout(BuildTargetContext c)
        {
            var sharedFxRoot = Path.Combine(Dirs.Output, "obj", "sharedFx");
            if (Directory.Exists(sharedFxRoot))
            {
                Utils.DeleteDirectory(sharedFxRoot);
            }

            Directory.CreateDirectory(sharedFxRoot);

            Utils.CopyDirectoryRecursively(Path.Combine(GetSharedFrameworkPublishPath(c), "shared"), sharedFxRoot, true);
            FixPermissions(sharedFxRoot);

            c.BuildContext["SharedFrameworkPublishRoot"] = sharedFxRoot;
            c.BuildContext["SharedFrameworkPublishSymbolsRoot"] = $"{Dirs.SharedFrameworkPublish}.symbols";
            return c.Success();
        }

        [Target]
        public static BuildTargetResult CopyCombinedMuxerHostFxrFrameworkLayout(BuildTargetContext c)
        {
            var combinedRoot = Path.Combine(Dirs.Output, "obj", "combined-framework-host");
            if (Directory.Exists(combinedRoot))
            {
                Utils.DeleteDirectory(combinedRoot);
            }
            Directory.CreateDirectory(combinedRoot);

            string sharedFrameworkPublishRoot = c.BuildContext.Get<string>("SharedFrameworkPublishRoot");
            Utils.CopyDirectoryRecursively(sharedFrameworkPublishRoot, combinedRoot);

            string sharedHostPublishRoot = c.BuildContext.Get<string>("SharedHostPublishRoot");
            Utils.CopyDirectoryRecursively(sharedHostPublishRoot, combinedRoot);

            string hostFxrPublishRoot = c.BuildContext.Get<string>("HostFxrPublishRoot");
            Utils.CopyDirectoryRecursively(hostFxrPublishRoot, combinedRoot);

            c.BuildContext["CombinedMuxerHostFxrFrameworkPublishRoot"] = combinedRoot;
            return c.Success();
        }

        [Target(nameof(PackageTargets.GenerateZip), nameof(PackageTargets.GenerateTarBall))]
        public static BuildTargetResult GenerateCompressedFile(BuildTargetContext c)
        {
            return c.Success();
        }

        [Target(nameof(PackageTargets.InitPackage))]
        [BuildPlatforms(BuildPlatform.Windows)]
        public static BuildTargetResult GenerateZip(BuildTargetContext c)
        {
            CreateZipFromDirectory(
                c.BuildContext.Get<string>("CombinedMuxerHostFxrFrameworkPublishRoot"), 
                c.BuildContext.Get<string>("CombinedMuxerHostFxrFrameworkCompressedFile"));

            CreateZipFromDirectory(
                c.BuildContext.Get<string>("HostFxrPublishRoot"), 
                c.BuildContext.Get<string>("HostFxrCompressedFile"));
            
            CreateZipFromDirectory(
                c.BuildContext.Get<string>("SharedFrameworkPublishRoot"), 
                c.BuildContext.Get<string>("SharedFrameworkCompressedFile"));

            CreateZipFromDirectory(
                c.BuildContext.Get<string>("SharedFrameworkPublishSymbolsRoot"),
                c.BuildContext.Get<string>("SharedFrameworkSymbolsCompressedFile"));

            return c.Success();
        }

        [Target(nameof(PackageTargets.InitPackage))]
        [BuildPlatforms(BuildPlatform.Unix)]
        public static BuildTargetResult GenerateTarBall(BuildTargetContext c)
        {
            CreateTarBallFromDirectory(
                c.BuildContext.Get<string>("CombinedMuxerHostFxrFrameworkPublishRoot"), 
                c.BuildContext.Get<string>("CombinedMuxerHostFxrFrameworkCompressedFile"));

            CreateTarBallFromDirectory(
                c.BuildContext.Get<string>("HostFxrPublishRoot"), 
                c.BuildContext.Get<string>("HostFxrCompressedFile"));

            CreateTarBallFromDirectory(
                c.BuildContext.Get<string>("SharedFrameworkPublishRoot"), 
                c.BuildContext.Get<string>("SharedFrameworkCompressedFile"));

            CreateTarBallFromDirectory(
                c.BuildContext.Get<string>("SharedFrameworkPublishSymbolsRoot"),
                c.BuildContext.Get<string>("SharedFrameworkSymbolsCompressedFile"));

            return c.Success();
        }

        [Target]
        public static BuildTargetResult GenerateNugetPackages(BuildTargetContext c)
        {
            var versionSuffix = c.BuildContext.Get<BuildVersion>("BuildVersion").CommitCountString;
            var configuration = c.BuildContext.Get<string>("Configuration");

            var dotnet = DotNetCli.Stage0;

            var packagingBuildBasePath = Path.Combine(Dirs.Intermediate, "forPackaging");

            FS.Mkdirp(Dirs.Packages);

            foreach (var projectName in ProjectsToPack)
            {
                var projectFile = Path.Combine(Dirs.RepoRoot, "src", projectName, "project.json");

                dotnet.Pack(
                    projectFile,
                    "--no-build",
                    "--serviceable",
                    "--build-base-path", packagingBuildBasePath,
                    "--output", Dirs.Packages,
                    "--configuration", configuration,
                    "--version-suffix", versionSuffix)
                    .Execute()
                    .EnsureSuccessful();
            }

            return c.Success();
        }

        private static void CreateZipFromDirectory(string directory, string artifactPath)
        {
            if (File.Exists(artifactPath))
            {
                File.Delete(artifactPath);
            }

            ZipFile.CreateFromDirectory(directory, artifactPath, CompressionLevel.Optimal, false);
        }

        private static void CreateTarBallFromDirectory(string directory, string artifactPath)
        {
            if (File.Exists(artifactPath))
            {
                File.Delete(artifactPath);
            }

            Cmd("tar", "-czf", artifactPath, "-C", directory, ".")
                .Execute()
                .EnsureSuccessful();
        }

        private static void FixPermissions(string directory)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Reset everything to user readable/writeable and group and world readable.
                FS.ChmodAll(directory, "*", "644");

                // Now make things that should be executable, executable.
                FS.FixModeFlags(directory);
            }
        }

        private static string GetSharedFrameworkPublishPath(BuildTargetContext c)
        {
            string sharedFrameworkPublishPath = string.Empty;
            
            string preBuiltPortableStagingPath=c.BuildContext.Get<string>("PortableBuildStagingLocation");

            // If we are not generating distro specific installers for portable build, then we won't have access to staging location and thus, will use default binary location where SharedFX was published
            if(preBuiltPortableStagingPath == null)
            {
                sharedFrameworkPublishPath = Dirs.SharedFrameworkPublish;
            }
            else 
            {
                Console.WriteLine($"Installers will package binaries from path set by PORTABLE_BUILD_STAGING_LOCATION environment variable :{preBuiltPortableStagingPath}");
                sharedFrameworkPublishPath = preBuiltPortableStagingPath;
            }
            
            return sharedFrameworkPublishPath;
        }
    }
}
