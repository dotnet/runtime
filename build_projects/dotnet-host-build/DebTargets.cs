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
                nameof(GenerateHostFxrDeb),
                nameof(GenerateSharedFrameworkDeb))]
        [BuildPlatforms(BuildPlatform.Ubuntu)]
        public static BuildTargetResult GenerateDebs(BuildTargetContext c)
        {
            return c.Success();
        }

        [Target]
        [BuildPlatforms(BuildPlatform.Ubuntu, "14.04")]
        public static BuildTargetResult GenerateSharedHostDeb(BuildTargetContext c)
        {
            var packageName = Monikers.GetDebianSharedHostPackageName(c);
            var version = c.BuildContext.Get<HostVersion>("HostVersion").LockedHostVersion.ToString();
            var inputRoot = c.BuildContext.Get<string>("SharedHostPublishRoot");
            var debFile = c.BuildContext.Get<string>("SharedHostInstallerFile");
            var manPagesDir = Path.Combine(Dirs.RepoRoot, "Documentation", "manpages");
            var debianConfigFile = Path.Combine(Dirs.DebPackagingConfig, "dotnet-sharedhost-debian_config.json");

            var debianConfigVariables = new Dictionary<string, string>()
            {
                { "SHARED_HOST_BRAND_NAME", Monikers.SharedHostBrandName }
            };

            var debCreator = new DebPackageCreator(
                DotNetCli.Stage0,
                Dirs.Intermediate,
                dotnetDebToolPackageSource: Dirs.Packages);

            debCreator.CreateDeb(
                debianConfigFile, 
                packageName, 
                version, 
                inputRoot, 
                debianConfigVariables, 
                debFile, 
                manPagesDir);

            return c.Success();
        }

        [Target]
        [BuildPlatforms(BuildPlatform.Ubuntu, "14.04")]
        public static BuildTargetResult GenerateHostFxrDeb(BuildTargetContext c)
        {
            var packageName = Monikers.GetDebianHostFxrPackageName(c);
            var version = c.BuildContext.Get<HostVersion>("HostVersion").LockedHostVersion.ToString();
            var sharedHostVersion = c.BuildContext.Get<HostVersion>("HostVersion").LockedHostVersion.ToString();
            var inputRoot = c.BuildContext.Get<string>("HostFxrPublishRoot");
            var debFile = c.BuildContext.Get<string>("HostFxrInstallerFile");
            var debianConfigFile = Path.Combine(Dirs.DebPackagingConfig, "dotnet-hostfxr-debian_config.json");

            var debianConfigVariables = new Dictionary<string, string>()
            {
                { "HOSTFXR_BRAND_NAME", Monikers.HostFxrBrandName },
                { "SHARED_HOST_DEBIAN_VERSION", sharedHostVersion }
            };

            var debCreator = new DebPackageCreator(
                DotNetCli.Stage0,
                Dirs.Intermediate,
                dotnetDebToolPackageSource: Dirs.Packages);

            debCreator.CreateDeb(
                debianConfigFile,
                packageName,
                version,
                inputRoot,
                debianConfigVariables,
                debFile);

            return c.Success();
        }

        [Target(nameof(InstallSharedHost))]
        [BuildPlatforms(BuildPlatform.Ubuntu, "14.04")]
        public static BuildTargetResult GenerateSharedFrameworkDeb(BuildTargetContext c)
        {
            var sharedFrameworkNugetVersion = c.BuildContext.Get<string>("SharedFrameworkNugetVersion");
            var packageName = Monikers.GetDebianSharedFrameworkPackageName(sharedFrameworkNugetVersion);
            var hostFxrVersion = c.BuildContext.Get<HostVersion>("HostVersion").LockedHostVersion.ToString();
            var version = c.BuildContext.Get<string>("SharedFrameworkNugetVersion");
            var inputRoot = c.BuildContext.Get<string>("SharedFrameworkPublishRoot");
            var debFile = c.BuildContext.Get<string>("SharedFrameworkInstallerFile");
            var debianConfigFile = Path.Combine(Dirs.DebPackagingConfig, "dotnet-sharedframework-debian_config.json");

            var debianConfigVariables = new Dictionary<string, string>()
            {
                { "HOSTFXR_DEBIAN_VERSION", hostFxrVersion },
                { "SHARED_FRAMEWORK_DEBIAN_PACKAGE_NAME", packageName },
                { "SHARED_FRAMEWORK_NUGET_NAME", Monikers.SharedFrameworkName },
                { "SHARED_FRAMEWORK_NUGET_VERSION",  c.BuildContext.Get<string>("SharedFrameworkNugetVersion")},
                { "SHARED_FRAMEWORK_BRAND_NAME", Monikers.SharedFxBrandName }
            };

            var debCreator = new DebPackageCreator(
                DotNetCli.Stage0,
                Dirs.Intermediate,
                dotnetDebToolPackageSource: Dirs.Packages);

            debCreator.CreateDeb(
                debianConfigFile,
                packageName,
                version,
                inputRoot,
                debianConfigVariables,
                debFile);

            return c.Success();
        }

        [Target(nameof(InstallSharedFramework),
                nameof(RemovePackages))]
        [BuildPlatforms(BuildPlatform.Ubuntu, "14.04")]
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
        public static BuildTargetResult InstallHostFxr(BuildTargetContext c)
        {
            InstallPackage(c.BuildContext.Get<string>("HostFxrInstallerFile"));
            
            return c.Success();
        }
        
        [Target(nameof(InstallHostFxr))]
        public static BuildTargetResult InstallSharedFramework(BuildTargetContext c)
        {
            InstallPackage(c.BuildContext.Get<string>("SharedFrameworkInstallerFile"));
            
            return c.Success();
        }
        
        [Target]
        public static BuildTargetResult RemovePackages(BuildTargetContext c)
        {
            var sharedFrameworkNugetVersion = c.BuildContext.Get<string>("SharedFrameworkNugetVersion");
            
            IEnumerable<string> orderedPackageNames = new List<string>()
            {
                Monikers.GetDebianSharedFrameworkPackageName(sharedFrameworkNugetVersion),
                Monikers.GetDebianHostFxrPackageName(c),
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
