using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.DotNet.Cli.Build;
using static Microsoft.DotNet.Cli.Build.Framework.BuildHelpers;

namespace Microsoft.DotNet.Host.Build
{
    public static class PublishTargets
    {
        private static AzurePublisher AzurePublisherTool { get; set; }

        private static DebRepoPublisher DebRepoPublisherTool { get; set; }

        private static string Channel { get; set; }

        private static string SharedFrameworkNugetVersion { get; set; }

        private static string SharedHostNugetVersion { get; set; }

        [Target]
        public static BuildTargetResult InitPublish(BuildTargetContext c)
        {
            AzurePublisherTool = new AzurePublisher();
            DebRepoPublisherTool = new DebRepoPublisher(Dirs.Packages);
            SharedFrameworkNugetVersion = c.BuildContext.Get<string>("SharedFrameworkNugetVersion");
            SharedHostNugetVersion = c.BuildContext.Get<HostVersion>("HostVersion").LockedHostVersion;
            Channel = c.BuildContext.Get<string>("Channel");

            return c.Success();
        }

        [Target(nameof(PrepareTargets.Init),
        nameof(PublishTargets.InitPublish),
        nameof(PublishTargets.PublishArtifacts))]
        [Environment("PUBLISH_TO_AZURE_BLOB", "1", "true")] // This is set by CI systems
        public static BuildTargetResult Publish(BuildTargetContext c)
        {
            return c.Success();
        }

        [Target(
            nameof(PublishTargets.PublishInstallerFilesToAzure),
            nameof(PublishTargets.PublishArchivesToAzure),
            /*nameof(PublishTargets.PublishDebFilesToDebianRepo),*/ //https://github.com/dotnet/cli/issues/2973
            nameof(PublishTargets.PublishCoreHostPackages),
            nameof(PublishTargets.PublishSharedFrameworkVersionBadge))]
        public static BuildTargetResult PublishArtifacts(BuildTargetContext c) => c.Success();

        [Target(
            nameof(PublishTargets.PublishSharedHostInstallerFileToAzure),
            nameof(PublishTargets.PublishSharedFrameworkInstallerFileToAzure),
            nameof(PublishTargets.PublishCombinedFrameworkHostInstallerFileToAzure))]
        public static BuildTargetResult PublishInstallerFilesToAzure(BuildTargetContext c) => c.Success();

        [Target(nameof(PublishTargets.PublishCombinedHostFrameworkArchiveToAzure))]
        public static BuildTargetResult PublishArchivesToAzure(BuildTargetContext c) => c.Success();

        [Target(
            nameof(PublishSharedFrameworkDebToDebianRepo),
            nameof(PublishSharedHostDebToDebianRepo))]
        [BuildPlatforms(BuildPlatform.Ubuntu)]
        public static BuildTargetResult PublishDebFilesToDebianRepo(BuildTargetContext c)
        {
            return c.Success();
        }

        [Target]
        public static BuildTargetResult PublishSharedFrameworkVersionBadge(BuildTargetContext c)
        {
            var versionBadge = c.BuildContext.Get<string>("VersionBadge");
            var versionBadgeBlob = $"{Channel}/Binaries/{SharedFrameworkNugetVersion}/{Path.GetFileName(versionBadge)}";
            AzurePublisherTool.PublishFile(versionBadgeBlob, versionBadge);
            return c.Success();
        }

        [Target]
        public static BuildTargetResult PublishCoreHostPackages(BuildTargetContext c)
        {
            foreach (var file in Directory.GetFiles(Dirs.CorehostLocalPackages, "*.nupkg"))
            {
                var hostBlob = $"{Channel}/Binaries/{SharedFrameworkNugetVersion}/{Path.GetFileName(file)}";
                AzurePublisherTool.PublishFile(hostBlob, file);
                Console.WriteLine($"Publishing package {hostBlob} to Azure.");
            }

            return c.Success();
        }

        [Target]
        [BuildPlatforms(BuildPlatform.Ubuntu, BuildPlatform.Windows)]
        public static BuildTargetResult PublishSharedHostInstallerFileToAzure(BuildTargetContext c)
        {
            var version = SharedHostNugetVersion;
            var installerFile = c.BuildContext.Get<string>("SharedHostInstallerFile");

            if (CurrentPlatform.Current == BuildPlatform.Windows)
            {
                installerFile = Path.ChangeExtension(installerFile, "msi");
            }

            AzurePublisherTool.PublishInstallerFile(installerFile, Channel, version);

            return c.Success();
        }

        [Target]
        [BuildPlatforms(BuildPlatform.Ubuntu)]
        public static BuildTargetResult PublishSharedFrameworkInstallerFileToAzure(BuildTargetContext c)
        {
            var version = SharedFrameworkNugetVersion;
            var installerFile = c.BuildContext.Get<string>("SharedFrameworkInstallerFile");

            AzurePublisherTool.PublishInstallerFile(installerFile, Channel, version);

            return c.Success();
        }

        [Target]
        [BuildPlatforms(BuildPlatform.Windows, BuildPlatform.OSX)]
        public static BuildTargetResult PublishCombinedFrameworkHostInstallerFileToAzure(BuildTargetContext c)
        {
            var version = SharedFrameworkNugetVersion;
            var installerFile = c.BuildContext.Get<string>("CombinedFrameworkHostInstallerFile");

            AzurePublisherTool.PublishInstallerFile(installerFile, Channel, version);

            return c.Success();
        }

        [Target]
        public static BuildTargetResult PublishCombinedHostFrameworkArchiveToAzure(BuildTargetContext c)
        {
            var version = SharedFrameworkNugetVersion;
            var archiveFile = c.BuildContext.Get<string>("CombinedFrameworkHostCompressedFile");

            AzurePublisherTool.PublishArchive(archiveFile, Channel, version);
            return c.Success();
        }

        [Target]
        [BuildPlatforms(BuildPlatform.Ubuntu)]
        public static BuildTargetResult PublishSharedFrameworkDebToDebianRepo(BuildTargetContext c)
        {
            var version = SharedFrameworkNugetVersion;

            var packageName = Monikers.GetDebianSharedFrameworkPackageName(c);
            var installerFile = c.BuildContext.Get<string>("SharedFrameworkInstallerFile");
            var uploadUrl = AzurePublisherTool.CalculateInstallerUploadUrl(installerFile, Channel, version);

            DebRepoPublisherTool.PublishDebFileToDebianRepo(
                packageName,
                version,
                uploadUrl);

            return c.Success();
        }

        [Target]
        [BuildPlatforms(BuildPlatform.Ubuntu)]
        public static BuildTargetResult PublishSharedHostDebToDebianRepo(BuildTargetContext c)
        {
            var version = SharedHostNugetVersion;

            var packageName = Monikers.GetDebianSharedHostPackageName(c);
            var installerFile = c.BuildContext.Get<string>("SharedHostInstallerFile");
            var uploadUrl = AzurePublisherTool.CalculateInstallerUploadUrl(installerFile, Channel, version);

            DebRepoPublisherTool.PublishDebFileToDebianRepo(
                packageName,
                version,
                uploadUrl);

            return c.Success();
        }
    }
}

