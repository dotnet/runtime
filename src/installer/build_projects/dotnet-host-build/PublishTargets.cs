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

        private static string CommitHash { get; set; }

        private static string SharedFrameworkNugetVersion { get; set; }

        private static string SharedHostNugetVersion { get; set; }

        private static string HostFxrNugetVersion { get; set; }

        private static bool IncludeSymbolPackages { get; set; }

        [Target]
        public static BuildTargetResult InitPublish(BuildTargetContext c)
        {
            AzurePublisherTool = new AzurePublisher();
            DebRepoPublisherTool = new DebRepoPublisher(Dirs.Packages);
            SharedFrameworkNugetVersion = c.BuildContext.Get<string>("SharedFrameworkNugetVersion");
            SharedHostNugetVersion = c.BuildContext.Get<HostVersion>("HostVersion").LockedHostVersion.ToString();
            HostFxrNugetVersion = c.BuildContext.Get<HostVersion>("HostVersion").LockedHostFxrVersion.ToString();
            Channel = c.BuildContext.Get<string>("Channel");
            CommitHash = c.BuildContext.Get<string>("CommitHash");

            // Do not publish symbol packages on a release branch.
            IncludeSymbolPackages = !c.BuildContext.Get<string>("BranchName").StartsWith("release/");

            return c.Success();
        }

        [Target(nameof(PrepareTargets.Init),
        nameof(PublishTargets.InitPublish),
        nameof(PublishTargets.PublishArtifacts),
        nameof(PublishTargets.FinalizeBuild))]
        [Environment("PUBLISH_TO_AZURE_BLOB", "1", "true")] // This is set by CI systems
        public static BuildTargetResult Publish(BuildTargetContext c)
        {
            return c.Success();
        }

        [Target]
        [BuildPlatforms(BuildPlatform.Ubuntu, "14.04")]
        public static BuildTargetResult PublishDotnetDebToolPackage(BuildTargetContext c)
        {
            string nugetFeedUrl = EnvVars.EnsureVariable("CLI_NUGET_FEED_URL");
            string apiKey = EnvVars.EnsureVariable("CLI_NUGET_API_KEY");
            NuGetUtil.PushPackages(Dirs.Packages, nugetFeedUrl, apiKey, IncludeSymbolPackages);

            return c.Success();
        }

        [Target]
        public static BuildTargetResult FinalizeBuild(BuildTargetContext c)
        {
            if (CheckIfAllBuildsHavePublished())
            {
                string targetContainer = $"{Channel}/Binaries/Latest/";
                string targetVersionFile = $"{targetContainer}{CommitHash}";
                string semaphoreBlob = $"{Channel}/Binaries/sharedFxPublishSemaphore";
                AzurePublisherTool.CreateBlobIfNotExists(semaphoreBlob);

                string leaseId = AzurePublisherTool.AcquireLeaseOnBlob(semaphoreBlob);

                // Prevent race conditions by dropping a version hint of what version this is. If we see this file
                // and it is the same as our version then we know that a race happened where two+ builds finished 
                // at the same time and someone already took care of publishing and we have no work to do.
                if (AzurePublisherTool.IsLatestSpecifiedVersion(targetVersionFile))
                {
                    AzurePublisherTool.ReleaseLeaseOnBlob(semaphoreBlob, leaseId);
                    return c.Success();
                }
                else
                {
                    Regex versionFileRegex = new Regex(@"(?<CommitHash>[\w\d]{40})");

                    // Delete old version files
                    AzurePublisherTool.ListBlobs($"{targetContainer}")
                        .Select(s => s.Replace("/dotnet/", ""))
                        .Where(s => versionFileRegex.IsMatch(s))
                        .ToList()
                        .ForEach(f => AzurePublisherTool.TryDeleteBlob(f));

                    // Drop the version file signaling such for any race-condition builds (see above comment).
                    AzurePublisherTool.DropLatestSpecifiedVersion(targetVersionFile);
                }

                try
                {
                    // Copy the shared framework + host Archives
                    CopyBlobs($"{Channel}/Binaries/{SharedFrameworkNugetVersion}/", targetContainer);

                    // Copy the shared framework installers
                    CopyBlobs($"{Channel}/Installers/{SharedFrameworkNugetVersion}/", $"{Channel}/Installers/Latest/");

                    // Copy the shared host installers
                    CopyBlobs($"{Channel}/Installers/{SharedHostNugetVersion}/", $"{Channel}/Installers/Latest/");

                    // Generate the Sharedfx Version text files
                    List<string> versionFiles = new List<string>() 
                    {
                        "win.x86.version",
                        "win.x64.version",
                        "win.arm.version",
                        "win.arm64.version",
                        "linux.x64.version",
                        "ubuntu.x64.version",
                        "ubuntu.14.04.arm.version",
                        "ubuntu.16.04.x64.version",
                        "ubuntu.16.04.arm.version",
                        "ubuntu.16.10.x64.version",
                        "rhel.x64.version",
                        "osx.x64.version",
                        "debian.x64.version",
                        "centos.x64.version",
                        "fedora.23.x64.version",
                        "fedora.24.x64.version",
                        "opensuse.13.2.x64.version",
                        "opensuse.42.1.x64.version"
                    };
                    
                    PublishCoreHostPackagesToFeed();

                    string sfxVersion = Utils.GetSharedFrameworkVersionFileContent(c);
                    foreach (string version in versionFiles)
                    {
                        AzurePublisherTool.PublishStringToBlob($"{Channel}/dnvm/latest.sharedfx.{version}", sfxVersion);
                    }
                }
                finally
                {
                    AzurePublisherTool.ReleaseLeaseOnBlob(semaphoreBlob, leaseId);
                }
            }

            return c.Success();
        }

        private static void CopyBlobs(string sourceFolder, string destinationFolder)
        {
            foreach (string blob in AzurePublisherTool.ListBlobs(sourceFolder))
            {
                string source = blob.Replace("/dotnet/", "");
                string targetName = Path.GetFileName(blob)
                                        .Replace(SharedFrameworkNugetVersion, "latest")
                                        .Replace(SharedHostNugetVersion, "latest");
                string target = $"{destinationFolder}{targetName}";
                AzurePublisherTool.CopyBlob(source, target);
            }
        }

        private static void PublishCoreHostPackagesToFeed()
        {
            var hostBlob = $"{Channel}/Binaries/{SharedFrameworkNugetVersion}";

            Directory.CreateDirectory(Dirs.PackagesNoRID);
            AzurePublisherTool.DownloadFilesWithExtension(hostBlob, ".nupkg", Dirs.PackagesNoRID);

            string nugetFeedUrl = EnvVars.EnsureVariable("NUGET_FEED_URL");
            string apiKey = EnvVars.EnsureVariable("NUGET_API_KEY");
            NuGetUtil.PushPackages(Dirs.PackagesNoRID, nugetFeedUrl, apiKey, IncludeSymbolPackages);

            string githubAuthToken = EnvVars.EnsureVariable("GITHUB_PASSWORD");
            VersionRepoUpdater repoUpdater = new VersionRepoUpdater(githubAuthToken);
            repoUpdater.UpdatePublishedVersions(Dirs.PackagesNoRID, $"build-info/dotnet/core-setup/{Channel}/Latest").Wait();
        }

        private static bool CheckIfAllBuildsHavePublished()
        {
            Dictionary<string, bool> badges = new Dictionary<string, bool>()
             {
                 { "sharedfx_Windows_x86", false },
                 { "sharedfx_Windows_x64", false },
                 { "sharedfx_Windows_arm", false },
                 { "sharedfx_Windows_arm64", false },
                 { "sharedfx_Linux_x64", false },
                 { "sharedfx_Ubuntu_x64", false },
                 // { "sharedfx_Ubuntu_14_04_arm", false },
                 { "sharedfx_Ubuntu_16_04_x64", false },
                 // { "sharedfx_Ubuntu_16_04_arm", false },
                 { "sharedfx_Ubuntu_16_10_x64", false },
                 { "sharedfx_RHEL_x64", false },
                 { "sharedfx_OSX_x64", false },
                 { "sharedfx_Debian_x64", false },
                 { "sharedfx_CentOS_x64", false },
                 { "sharedfx_Fedora_23_x64", false },
                 { "sharedfx_Fedora_24_x64", false },
                 { "sharedfx_openSUSE_13_2_x64", false },
                 { "sharedfx_openSUSE_42_1_x64", false }
             };

            List<string> blobs = new List<string>(AzurePublisherTool.ListBlobs($"{Channel}/Binaries/{SharedFrameworkNugetVersion}/"));

            var versionBadgeName = $"sharedfx_{Monikers.GetBadgeMoniker()}";
            if (badges.ContainsKey(versionBadgeName) == false)
            {
                throw new ArgumentException($"A new OS build ({versionBadgeName}) was added without adding the moniker to the {nameof(badges)} lookup");
            }

            foreach (string file in blobs)
            {
                string name = Path.GetFileName(file);

                if (!name.EndsWith(".svg"))
                {
                    continue;
                }

                // Include _ delimiter when matching to prevent finding both arm and arm64 badges
                // when checking the arm badge file.
                string[] matchingBadgeKeys = badges.Keys
                    .Where(badgeName => name.StartsWith($"{badgeName}_"))
                    .ToArray();

                if (matchingBadgeKeys.Length == 1)
                {
                    badges[matchingBadgeKeys[0]] = true;
                }
                else if (matchingBadgeKeys.Length > 1)
                {
                    throw new BuildFailureException(
                        $"Expected 0 or 1 badges matching file '{name}', " +
                        $"found {matchingBadgeKeys.Length}: " +
                        string.Join(", ", matchingBadgeKeys));
                }
            }

            foreach (string unfinishedBadge in badges.Where(pair => !pair.Value).Select(pair => pair.Key))
            {
                Console.WriteLine($"Not all builds complete, badge not found: {unfinishedBadge}");
            }

            return badges.Keys.All(key => badges[key]);
        }

        [Target(
            nameof(PublishTargets.PublishInstallerFilesToAzure),
            nameof(PublishTargets.PublishArchivesToAzure),
            nameof(PublishTargets.PublishDotnetDebToolPackage),
            nameof(PublishTargets.PublishDebFilesToDebianRepo),
            nameof(PublishTargets.PublishCoreHostPackages),
            nameof(PublishTargets.PublishManagedPackages),
            nameof(PublishTargets.PublishSharedFrameworkVersionBadge))]
        public static BuildTargetResult PublishArtifacts(BuildTargetContext c) => c.Success();

        [Target(
            nameof(PublishTargets.PublishSharedHostInstallerFileToAzure),
            nameof(PublishTargets.PublishHostFxrInstallerFileToAzure),
            nameof(PublishTargets.PublishSharedFrameworkInstallerFileToAzure),
            nameof(PublishTargets.PublishCombinedMuxerHostFxrFrameworkInstallerFileToAzure))]
        [BuildPlatforms(BuildPlatform.Ubuntu, BuildPlatform.OSX, BuildPlatform.Windows, BuildPlatform.Debian)]
        public static BuildTargetResult PublishInstallerFilesToAzure(BuildTargetContext c) => c.Success();

        [Target(
            nameof(PublishTargets.PublishHostFxrArchiveToAzure),
            nameof(PublishTargets.PublishSharedFrameworkArchiveToAzure),
            nameof(PublishTargets.PublishCombinedMuxerHostFxrFrameworkArchiveToAzure))]
        public static BuildTargetResult PublishArchivesToAzure(BuildTargetContext c) => c.Success();

        [Target(
            nameof(PublishSharedFrameworkDebToDebianRepo),
            nameof(PublishHostFxrDebToDebianRepo),
            nameof(PublishSharedHostDebToDebianRepo))]
        [BuildPlatforms(BuildPlatform.Ubuntu, BuildPlatform.Debian)]
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
        public static BuildTargetResult PublishManagedPackages(BuildTargetContext c)
        {
            if (EnvVars.Signed)
            {
                foreach (var file in Directory.GetFiles(Dirs.Packages, "*.nupkg"))
                {
                    var hostBlob = $"{Channel}/Binaries/{SharedFrameworkNugetVersion}/{Path.GetFileName(file)}";
                    AzurePublisherTool.PublishFile(hostBlob, file);
                    Console.WriteLine($"Publishing package {hostBlob} to Azure.");
                }
            }

            return c.Success();
        }

        [Target]
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
        public static BuildTargetResult PublishHostFxrInstallerFileToAzure(BuildTargetContext c)
        {
            var version = HostFxrNugetVersion;
            var installerFile = c.BuildContext.Get<string>("HostFxrInstallerFile");

            if (CurrentPlatform.Current == BuildPlatform.Windows)
            {
                installerFile = Path.ChangeExtension(installerFile, "msi");
            }

            AzurePublisherTool.PublishInstallerFile(installerFile, Channel, version);

            return c.Success();
        }

        [Target]
        public static BuildTargetResult PublishSharedFrameworkInstallerFileToAzure(BuildTargetContext c)
        {
            var version = SharedFrameworkNugetVersion;
            var installerFile = c.BuildContext.Get<string>("SharedFrameworkInstallerFile");

            AzurePublisherTool.PublishInstallerFile(installerFile, Channel, version);

            return c.Success();
        }

        [Target]
        [BuildPlatforms(BuildPlatform.OSX, BuildPlatform.Windows)]
        public static BuildTargetResult PublishCombinedMuxerHostFxrFrameworkInstallerFileToAzure(BuildTargetContext c)
        {
            var version = SharedFrameworkNugetVersion;
            var installerFile = c.BuildContext.Get<string>("CombinedMuxerHostFxrFrameworkInstallerFile");

            AzurePublisherTool.PublishInstallerFile(installerFile, Channel, version);

            return c.Success();
        }

        [Target]
        public static BuildTargetResult PublishCombinedMuxerHostFxrFrameworkArchiveToAzure(BuildTargetContext c)
        {
            var version = SharedFrameworkNugetVersion;
            var archiveFile = c.BuildContext.Get<string>("CombinedMuxerHostFxrFrameworkCompressedFile");

            AzurePublisherTool.PublishArchive(archiveFile, Channel, version);
            return c.Success();
        }

        [Target]
        public static BuildTargetResult PublishHostFxrArchiveToAzure(BuildTargetContext c)
        {
            var version = SharedFrameworkNugetVersion;
            var archiveFile = c.BuildContext.Get<string>("HostFxrCompressedFile");

            AzurePublisherTool.PublishArchive(archiveFile, Channel, version);
            return c.Success();
        }

        [Target]
        public static BuildTargetResult PublishSharedFrameworkArchiveToAzure(BuildTargetContext c)
        {
            var version = SharedFrameworkNugetVersion;
            var archiveFile = c.BuildContext.Get<string>("SharedFrameworkCompressedFile");

            AzurePublisherTool.PublishArchive(archiveFile, Channel, version);
            return c.Success();
        }

        [Target]
        [BuildPlatforms(BuildPlatform.Ubuntu, BuildPlatform.Debian)]
        public static BuildTargetResult PublishSharedFrameworkDebToDebianRepo(BuildTargetContext c)
        {
            var version = SharedFrameworkNugetVersion;

            var packageName = Monikers.GetDebianSharedFrameworkPackageName(version);
            var installerFile = c.BuildContext.Get<string>("SharedFrameworkInstallerFile");
            var uploadUrl = AzurePublisherTool.CalculateInstallerUploadUrl(installerFile, Channel, version);

            DebRepoPublisherTool.PublishDebFileToDebianRepo(
                packageName,
                version,
                uploadUrl);

            return c.Success();
        }

        [Target]
        [BuildPlatforms(BuildPlatform.Ubuntu, BuildPlatform.Debian)]
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

        [Target]
        [BuildPlatforms(BuildPlatform.Ubuntu, BuildPlatform.Debian)]
        public static BuildTargetResult PublishHostFxrDebToDebianRepo(BuildTargetContext c)
        {
            var version = HostFxrNugetVersion;

            var packageName = Monikers.GetDebianHostFxrPackageName(version);
            var installerFile = c.BuildContext.Get<string>("HostFxrInstallerFile");
            var uploadUrl = AzurePublisherTool.CalculateInstallerUploadUrl(installerFile, Channel, version);

            DebRepoPublisherTool.PublishDebFileToDebianRepo(
                packageName,
                version,
                uploadUrl);

            return c.Success();
        }
    }
}

