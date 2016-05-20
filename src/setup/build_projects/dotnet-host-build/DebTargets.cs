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
    public class DebTargets
    {
        [Target(nameof(GenerateSharedHostDeb),
                nameof(GenerateSharedFrameworkDeb))]
        [BuildPlatforms(BuildPlatform.Ubuntu)]
        public static BuildTargetResult GenerateDebs(BuildTargetContext c)
        {
            return c.Success();
        }

        [Target]
        [BuildPlatforms(BuildPlatform.Ubuntu)]
        public static BuildTargetResult GenerateSharedHostDeb(BuildTargetContext c)
        {
            var packageName = Monikers.GetDebianSharedHostPackageName(c);
            var version = c.BuildContext.Get<HostVersion>("HostVersion").LockedHostVersion;
            var inputRoot = c.BuildContext.Get<string>("SharedHostPublishRoot");
            var debFile = c.BuildContext.Get<string>("SharedHostInstallerFile");
            var objRoot = Path.Combine(Dirs.Output, "obj", "debian", "sharedhost");
            var manPagesDir = Path.Combine(Dirs.RepoRoot, "Documentation", "manpages");

            if (Directory.Exists(objRoot))
            {
                Directory.Delete(objRoot, true);
            }

            Directory.CreateDirectory(objRoot);

            Cmd(Path.Combine(Dirs.RepoRoot, "scripts", "package", "package-sharedhost-debian.sh"),
                    "--input", inputRoot, "--output", debFile, "-b", Monikers.SharedHostBrandName,
                    "--obj-root", objRoot, "--version", version, "-m", manPagesDir)
                    .Execute()
                    .EnsureSuccessful();
            return c.Success();
        }

        [Target(nameof(InstallSharedHost))]
        [BuildPlatforms(BuildPlatform.Ubuntu)]
        public static BuildTargetResult GenerateSharedFrameworkDeb(BuildTargetContext c)
        {
            var packageName = Monikers.GetDebianSharedFrameworkPackageName(c);
            var version = c.BuildContext.Get<string>("SharedFrameworkNugetVersion");
            var inputRoot = c.BuildContext.Get<string>("SharedFrameworkPublishRoot");
            var debFile = c.BuildContext.Get<string>("SharedFrameworkInstallerFile");
            var objRoot = Path.Combine(Dirs.Output, "obj", "debian", "sharedframework");

            if (Directory.Exists(objRoot))
            {
                Directory.Delete(objRoot, true);
            }

            Directory.CreateDirectory(objRoot);

            Cmd(Path.Combine(Dirs.RepoRoot, "scripts", "package", "package-sharedframework-debian.sh"),
                    "--input", inputRoot, "--output", debFile, "--package-name", packageName, "-b", Monikers.SharedFxBrandName,
                    "--framework-nuget-name", Monikers.SharedFrameworkName,
                    "--framework-nuget-version", c.BuildContext.Get<string>("SharedFrameworkNugetVersion"),
                    "--obj-root", objRoot, "--version", version)
                    .Execute()
                    .EnsureSuccessful();
            return c.Success();
        }

        [Target(nameof(InstallSharedFramework),
                nameof(RemovePackages))]
        [BuildPlatforms(BuildPlatform.Ubuntu)]
        public static BuildTargetResult TestDebInstaller(BuildTargetContext c)
        {
            return c.Success();
        }
        
        [Target]
        public static BuildTargetResult InstallSharedHost(BuildTargetContext c)
        {
            InstallPackage(c.BuildContext.Get<string>("SharedHostInstallerFile"));
            
            return c.Success();
        }
        
        [Target(nameof(InstallSharedHost))]
        public static BuildTargetResult InstallSharedFramework(BuildTargetContext c)
        {
            InstallPackage(c.BuildContext.Get<string>("SharedFrameworkInstallerFile"));
            
            return c.Success();
        }
        
        [Target]
        [BuildPlatforms(BuildPlatform.Ubuntu)]
        public static BuildTargetResult RemovePackages(BuildTargetContext c)
        {
            IEnumerable<string> orderedPackageNames = new List<string>()
            {
                Monikers.GetSdkDebianPackageName(c),
                Monikers.GetDebianSharedFrameworkPackageName(c),
                Monikers.GetDebianSharedHostPackageName(c)
            };
            
            foreach(var packageName in orderedPackageNames)
            {
                RemovePackage(packageName);
            }
            
            return c.Success();
        }
        
        private static void InstallPackage(string packagePath)
        {
            Cmd("sudo", "dpkg", "-i", packagePath)
                .Execute()
                .EnsureSuccessful();
        }
        
        private static void RemovePackage(string packageName)
        {
            Cmd("sudo", "dpkg", "-r", packageName)
                .Execute()
                .EnsureSuccessful();
        }
    }
}
