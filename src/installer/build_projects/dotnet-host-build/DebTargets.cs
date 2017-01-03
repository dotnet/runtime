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
        [BuildPlatforms(BuildPlatform.Ubuntu, BuildPlatform.Debian)]
        [BuildArchitectures(BuildArchitecture.x64)]
        public static BuildTargetResult GenerateDebs(BuildTargetContext c)
        {
            return c.Success();
        }

        [Target]
        [BuildPlatforms(BuildPlatform.Ubuntu, BuildPlatform.Debian)]
        public static BuildTargetResult GenerateSharedHostDeb(BuildTargetContext c)
        {
            // Ubuntu 16.04 Jenkins Machines don't have docker or debian package build tools
            // So we need to skip this target if the tools aren't present.
            // https://github.com/dotnet/core-setup/issues/167
            if (DebuildNotPresent())
            {
                c.Info("Debuild not present, skipping target: {nameof(GenerateSharedHostDeb)}");
                return c.Success();
            }

            var packageName = Monikers.GetDebianSharedHostPackageName(c);
            var version = c.BuildContext.Get<HostVersion>("HostVersion").LockedHostVersion.ToString();
            var inputRoot = c.BuildContext.Get<string>("SharedHostPublishRoot");
            var debFile = c.BuildContext.Get<string>("SharedHostInstallerFile");
            var manPagesDir = Path.Combine(Dirs.RepoRoot, "Documentation", "manpages");
            var debianConfigFile = Path.Combine(Dirs.DebPackagingConfig, "dotnet-sharedhost-debian_config.json");

            var debianConfigVariables = new Dictionary<string, string>()
            {
                { "SHARED_HOST_BRAND_NAME", Monikers.GetSharedHostBrandName(c) }
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
        [BuildPlatforms(BuildPlatform.Ubuntu, BuildPlatform.Debian)]
        public static BuildTargetResult GenerateHostFxrDeb(BuildTargetContext c)
        {
            // Ubuntu 16.04 Jenkins Machines don't have docker or debian package build tools
            // So we need to skip this target if the tools aren't present.
            // https://github.com/dotnet/core-setup/issues/167
            if (DebuildNotPresent())
            {
                c.Info("Debuild not present, skipping target: {nameof(GenerateHostFxrDeb)}");
                return c.Success();
            }

            var hostFxrVersion = c.BuildContext.Get<HostVersion>("HostVersion").LockedHostFxrVersion.ToString();
            var packageName = Monikers.GetDebianHostFxrPackageName(hostFxrVersion);
            var sharedHostVersion = c.BuildContext.Get<HostVersion>("HostVersion").LockedHostVersion.ToString();
            var inputRoot = c.BuildContext.Get<string>("HostFxrPublishRoot");
            var debFile = c.BuildContext.Get<string>("HostFxrInstallerFile");
            var debianConfigFile = Path.Combine(Dirs.DebPackagingConfig, "dotnet-hostfxr-debian_config.json");

            var debianConfigVariables = new Dictionary<string, string>()
            {
                { "HOSTFXR_BRAND_NAME", Monikers.GetHostFxrBrandName(c) },
                { "SHARED_HOST_DEBIAN_VERSION", sharedHostVersion },
                { "HOSTFXR_NUGET_VERSION", hostFxrVersion },
                { "HOSTFXR_DEBIAN_PACKAGE_NAME", packageName }
            };

            var debCreator = new DebPackageCreator(
                DotNetCli.Stage0,
                Dirs.Intermediate,
                dotnetDebToolPackageSource: Dirs.Packages);

            debCreator.CreateDeb(
                debianConfigFile,
                packageName,
                hostFxrVersion,
                inputRoot,
                debianConfigVariables,
                debFile);

            return c.Success();
        }

        [Target(nameof(InstallSharedHost))]
        [BuildPlatforms(BuildPlatform.Ubuntu, BuildPlatform.Debian)]
        public static BuildTargetResult GenerateSharedFrameworkDeb(BuildTargetContext c)
        {
            // Ubuntu 16.04 Jenkins Machines don't have docker or debian package build tools
            // So we need to skip this target if the tools aren't present.
            // https://github.com/dotnet/core-setup/issues/167
            if (DebuildNotPresent())
            {
                c.Info("Debuild not present, skipping target: {nameof(GenerateSharedFrameworkDeb)}");
                return c.Success();
            }

            var sharedFrameworkNugetVersion = c.BuildContext.Get<string>("SharedFrameworkNugetVersion");
            var packageName = Monikers.GetDebianSharedFrameworkPackageName(sharedFrameworkNugetVersion);
            var sharedHostVersion = c.BuildContext.Get<HostVersion>("HostVersion").LockedHostVersion.ToString();
            var hostFxrVersion = c.BuildContext.Get<HostVersion>("HostVersion").LockedHostFxrVersion.ToString();
            var hostfxrDebianPackageName = Monikers.GetDebianHostFxrPackageName(hostFxrVersion);
            var version = c.BuildContext.Get<string>("SharedFrameworkNugetVersion");
            var inputRoot = c.BuildContext.Get<string>("SharedFrameworkPublishRoot");
            var debFile = c.BuildContext.Get<string>("SharedFrameworkInstallerFile");
            var debianConfigFile = Path.Combine(Dirs.DebPackagingConfig, "dotnet-sharedframework-debian_config.json");

            var debianConfigVariables = new Dictionary<string, string>()
            {
                { "SHARED_HOST_DEBIAN_VERSION", sharedHostVersion },
                { "HOSTFXR_DEBIAN_PACKAGE_NAME", hostfxrDebianPackageName},
                { "HOSTFXR_DEBIAN_VERSION", hostFxrVersion },
                { "SHARED_FRAMEWORK_DEBIAN_PACKAGE_NAME", packageName },
                { "SHARED_FRAMEWORK_NUGET_NAME", Monikers.SharedFrameworkName },
                { "SHARED_FRAMEWORK_NUGET_VERSION",  c.BuildContext.Get<string>("SharedFrameworkNugetVersion")},
                { "SHARED_FRAMEWORK_BRAND_NAME", Monikers.GetSharedFxBrandName(c) }
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
        [BuildPlatforms(BuildPlatform.Ubuntu, BuildPlatform.Debian)]
        [BuildArchitectures(BuildArchitecture.x64)]
        public static BuildTargetResult TestDebInstaller(BuildTargetContext c)
        {
            return c.Success();
        }
        
        [Target]
        public static BuildTargetResult InstallSharedHost(BuildTargetContext c)
        {
            // Ubuntu 16.04 Jenkins Machines don't have docker or debian package build tools
            // So we need to skip this target if the tools aren't present.
            // https://github.com/dotnet/core-setup/issues/167
            if (DebuildNotPresent())
            {
                c.Info("Debuild not present, skipping target: {nameof(InstallSharedHost)}");
                return c.Success();
            }

            InstallPackage(c.BuildContext.Get<string>("SharedHostInstallerFile"));
            
            return c.Success();
        }

        [Target(nameof(InstallSharedHost))]
        public static BuildTargetResult InstallHostFxr(BuildTargetContext c)
        {
            // Ubuntu 16.04 Jenkins Machines don't have docker or debian package build tools
            // So we need to skip this target if the tools aren't present.
            // https://github.com/dotnet/core-setup/issues/167
            if (DebuildNotPresent())
            {
                c.Info("Debuild not present, skipping target: {nameof(InstallHostFxr)}");
                return c.Success();
            }

            InstallPackage(c.BuildContext.Get<string>("HostFxrInstallerFile"));
            
            return c.Success();
        }
        
        [Target(nameof(InstallHostFxr))]
        public static BuildTargetResult InstallSharedFramework(BuildTargetContext c)
        {
            // Ubuntu 16.04 Jenkins Machines don't have docker or debian package build tools
            // So we need to skip this target if the tools aren't present.
            // https://github.com/dotnet/core-setup/issues/167
            if (DebuildNotPresent())
            {
                c.Info("Debuild not present, skipping target: {nameof(InstallSharedFramework)}");
                return c.Success();
            }

            InstallPackage(c.BuildContext.Get<string>("SharedFrameworkInstallerFile"));
            
            return c.Success();
        }
        
        [Target]
        public static BuildTargetResult RemovePackages(BuildTargetContext c)
        {
            // Ubuntu 16.04 Jenkins Machines don't have docker or debian package build tools
            // So we need to skip this target if the tools aren't present.
            // https://github.com/dotnet/core-setup/issues/167
            if (DebuildNotPresent())
            {
                c.Info("Debuild not present, skipping target: {nameof(RemovePackages)}");
                return c.Success();
            }

            var sharedFrameworkNugetVersion = c.BuildContext.Get<string>("SharedFrameworkNugetVersion");
            var hostFxrVersion = c.BuildContext.Get<HostVersion>("HostVersion").LockedHostFxrVersion.ToString();
            
            IEnumerable<string> orderedPackageNames = new List<string>()
            {
                Monikers.GetDebianSharedFrameworkPackageName(sharedFrameworkNugetVersion),
                Monikers.GetDebianHostFxrPackageName(hostFxrVersion),
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

        private static bool DebuildNotPresent()
        {
            return Cmd("/usr/bin/env", "debuild", "-h").Execute().ExitCode != 0;
        }
    }
}
