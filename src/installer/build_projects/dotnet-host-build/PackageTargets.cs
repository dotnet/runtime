using System;
using System.Collections.Generic;
using System.IO;
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
        [Target(
            nameof(PackageTargets.CopySharedHostLayout),
            nameof(PackageTargets.CopySharedFxLayout),
            nameof(PackageTargets.CopyCombinedFrameworkHostLayout))]
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

            foreach (var file in Directory.GetFiles(Dirs.SharedFrameworkPublish, "*", SearchOption.TopDirectoryOnly))
            {
                var destFile = file.Replace(Dirs.SharedFrameworkPublish, sharedHostRoot);
                File.Copy(file, destFile, true);
                c.Warn(destFile);
            }
            FixPermissions(sharedHostRoot);

            c.BuildContext["SharedHostPublishRoot"] = sharedHostRoot;
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
            Utils.CopyDirectoryRecursively(Path.Combine(Dirs.SharedFrameworkPublish, "shared"), sharedFxRoot, true);
            FixPermissions(sharedFxRoot);

            c.BuildContext["SharedFrameworkPublishRoot"] = sharedFxRoot;
            return c.Success();
        }

        [Target]
        public static BuildTargetResult CopyCombinedFrameworkHostLayout(BuildTargetContext c)
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

            c.BuildContext["CombinedFrameworkHostRoot"] = combinedRoot;
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
            CreateZipFromDirectory(c.BuildContext.Get<string>("CombinedFrameworkHostRoot"), c.BuildContext.Get<string>("CombinedFrameworkHostCompressedFile"));

            return c.Success();
        }

        [Target(nameof(PackageTargets.InitPackage))]
        [BuildPlatforms(BuildPlatform.Unix)]
        public static BuildTargetResult GenerateTarBall(BuildTargetContext c)
        {
            CreateTarBallFromDirectory(c.BuildContext.Get<string>("CombinedFrameworkHostRoot"), c.BuildContext.Get<string>("CombinedFrameworkHostCompressedFile"));

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
    }
}
