using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.InternalAbstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Cli.Build
{
    public class Monikers
    {
        public const string SharedFrameworkName = "Microsoft.NETCore.App";
        public const string CLISdkBrandName = "Microsoft .NET Core 1.0.0 - SDK Preview 2";
        public const string SharedFxBrandName = "Microsoft .NET Core 1.0.0 - Runtime";
        public const string SharedHostBrandName = "Microsoft .NET Core 1.0.0 - Host";
        public const string HostFxrBrandName = "Microsoft .NET Core 1.0.0 - HostFxr";

        public static string GetProductMoniker(BuildTargetContext c, string artifactPrefix, string version)
        {
            string rid = RuntimeEnvironment.GetRuntimeIdentifier();

            if (rid == "ubuntu.16.04-x64" || rid == "fedora.23-x64" || rid == "opensuse.13.2-x64")
            {
                return $"{artifactPrefix}-{rid}.{version}";
            }
            else
            {
                string osname = GetOSShortName();
                var arch = CurrentArchitecture.Current.ToString();
                return $"{artifactPrefix}-{osname}-{arch}.{version}";
            }
        }

        public static string GetBadgeMoniker()
        {
            switch (RuntimeEnvironment.GetRuntimeIdentifier())
            {
                case "ubuntu.16.04-x64":
                     return "Ubuntu_16_04_x64";
                case "fedora.23-x64":
                     return "Fedora_23_x64";
                case "opensuse.13.2-x64":
                     return "openSUSE_13_2_x64";
            }

            return $"{CurrentPlatform.Current}_{CurrentArchitecture.Current}";
        }

        public static string GetDebianHostFxrPackageName(BuildTargetContext c)
        {
            return $"dotnet-hostfxr".ToLower();
        }

        public static string GetSdkDebianPackageName(BuildTargetContext c)
        {
            var channel = c.BuildContext.Get<string>("Channel").ToLower();
            var nugetVersion = c.BuildContext.Get<BuildVersion>("BuildVersion").NuGetVersion;

            var packagePrefix = "";
            switch (channel)
            {
                case "dev":
                    packagePrefix = "dotnet-nightly";
                    break;
                case "beta":
                case "rc1":
                case "rc2":
                case "preview":
                case "rtm":
                    packagePrefix = "dotnet";
                    break;
                default:
                    throw new Exception($"Unknown channel - {channel}");
            }

            return $"{packagePrefix}-dev-{nugetVersion}";
        }

        public static string GetDebianSharedFrameworkPackageName(string sharedFrameworkNugetVersion)
        {
            return $"dotnet-sharedframework-{SharedFrameworkName}-{sharedFrameworkNugetVersion}".ToLower();
        }

        public static string GetDebianSharedHostPackageName(BuildTargetContext c)
        {
            return $"dotnet-host".ToLower();
        }

        public static string GetOSShortName()
        {
            string osname = "";
            switch (CurrentPlatform.Current)
            {
                case BuildPlatform.Windows:
                    osname = "win";
                    break;
                default:
                    osname = CurrentPlatform.Current.ToString().ToLower();
                    break;
            }

            return osname;
        }
    }
}
