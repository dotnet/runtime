using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.InternalAbstractions;
using Microsoft.DotNet.Cli.Build;

using static Microsoft.DotNet.Cli.Build.Framework.BuildHelpers;

namespace Microsoft.DotNet.Host.Build
{
    public class PkgTargets
    {
        public static string PkgsIntermediateDir { get; set; }
        public static string SharedHostComponentId { get; set; }
        public static string SharedFxComponentId { get; set; }
        public static string SharedFxPkgId { get; set; }
        public static string SharedFrameworkNugetVersion { get; set; }
        public static string CLISdkComponentId { get; set; }
        public static string CLISdkPkgId { get; set; }
        public static string CLISdkNugetVersion { get; set; }

        [Target]
        [BuildPlatforms(BuildPlatform.OSX)]
        public static BuildTargetResult InitPkg(BuildTargetContext c)
        {
            PkgsIntermediateDir = Path.Combine(Dirs.Packages, "intermediate");
            Directory.CreateDirectory(PkgsIntermediateDir);

            SharedHostComponentId = $"com.microsoft.dotnet.sharedhost.component.osx.x64";

            string sharedFrameworkNugetName = Monikers.SharedFrameworkName;
            SharedFrameworkNugetVersion = c.BuildContext.Get<string>("SharedFrameworkNugetVersion");
            SharedFxComponentId = $"com.microsoft.dotnet.sharedframework.{sharedFrameworkNugetName}.{SharedFrameworkNugetVersion}.component.osx.x64";
            SharedFxPkgId = $"com.microsoft.dotnet.{sharedFrameworkNugetName}.{SharedFrameworkNugetVersion}.osx.x64";
            
            return c.Success();
        }

        [Target(nameof(InitPkg), nameof(GenerateSharedFrameworkProductArchive))]
        [BuildPlatforms(BuildPlatform.OSX)]
        public static BuildTargetResult GeneratePkgs(BuildTargetContext c)
        {
            return c.Success();
        }

        [Target(nameof(GenerateSharedFrameworkPkg), nameof(GenerateSharedHostPkg))]
        [BuildPlatforms(BuildPlatform.OSX)]
        public static BuildTargetResult GenerateSharedFrameworkProductArchive(BuildTargetContext c)
        {
            string resourcePath = Path.Combine(Dirs.RepoRoot, "packaging", "osx", "sharedframework", "resources");
            string outFilePath = Path.Combine(PkgsIntermediateDir, c.BuildContext.Get<string>("CombinedFrameworkHostInstallerFile"));

            string inputDistTemplatePath = Path.Combine(
                Dirs.RepoRoot,
                "packaging",
                "osx",
                "sharedframework",
                "shared-framework-distribution-template.xml");
            string distTemplate = File.ReadAllText(inputDistTemplatePath);
            string distributionPath = Path.Combine(PkgsIntermediateDir, "shared-framework-formatted-distribution.xml");
            string formattedDistContents =
                distTemplate.Replace("{SharedFxComponentId}", SharedFxComponentId)
                .Replace("{SharedHostComponentId}", SharedHostComponentId)
                .Replace("{SharedFrameworkNugetName}", Monikers.SharedFrameworkName)
                .Replace("{SharedFrameworkNugetVersion}", SharedFrameworkNugetVersion)
                .Replace("{SharedFxBrandName}", Monikers.SharedFxBrandName)
                .Replace("{SharedHostBrandName}", Monikers.SharedHostBrandName);
            File.WriteAllText(distributionPath, formattedDistContents);

            Cmd("productbuild",
                "--version", SharedFrameworkNugetVersion,
                "--identifier", SharedFxPkgId,
                "--package-path", PkgsIntermediateDir,
                "--resources", resourcePath,
                "--distribution", distributionPath,
                outFilePath)
            .Execute()
            .EnsureSuccessful();
            
            return c.Success();
        }

        [Target]
        [BuildPlatforms(BuildPlatform.OSX)]
        public static BuildTargetResult GenerateSharedFrameworkPkg(BuildTargetContext c)
        {
            string outFilePath = Path.Combine(PkgsIntermediateDir, SharedFxComponentId + ".pkg");
            string installLocation = "/usr/local/share/dotnet";
            string scriptsLocation = Path.Combine(Dirs.RepoRoot, "packaging", "osx", "sharedframework", "scripts");

            Cmd("pkgbuild",
                "--root", c.BuildContext.Get<string>("SharedFrameworkPublishRoot"),
                "--identifier", SharedFxComponentId,
                "--version", SharedFrameworkNugetVersion,
                "--install-location", installLocation,
                "--scripts", scriptsLocation,
                outFilePath)
                .Execute()
                .EnsureSuccessful();

            File.Copy(outFilePath, c.BuildContext.Get<string>("SharedFrameworkInstallerFile"), true);

            return c.Success();
        }

        [Target]
        [BuildPlatforms(BuildPlatform.OSX)]
        public static BuildTargetResult GenerateSharedHostPkg(BuildTargetContext c)
        {
            string version = c.BuildContext.Get<HostVersion>("HostVersion").LockedHostVersion;
            string outFilePath = Path.Combine(PkgsIntermediateDir, SharedHostComponentId + ".pkg");
            string installLocation = "/usr/local/share/dotnet";
            string scriptsLocation = Path.Combine(Dirs.RepoRoot, "packaging", "osx", "sharedhost", "scripts");

            Cmd("pkgbuild",
                "--root", c.BuildContext.Get<string>("SharedHostPublishRoot"),
                "--identifier", SharedHostComponentId,
                "--version", version,
                "--install-location", installLocation,
                "--scripts", scriptsLocation,
                outFilePath)
                .Execute()
                .EnsureSuccessful();

            File.Copy(outFilePath, c.BuildContext.Get<string>("SharedHostInstallerFile"), true);

            return c.Success();
        }
    }
}
