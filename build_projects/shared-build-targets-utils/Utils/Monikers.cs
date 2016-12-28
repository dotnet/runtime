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

        private static string GetBrandName(BuildTargetContext c, string suffix)
        {
            var buildVersion = c.BuildContext.Get<BuildVersion>("BuildVersion");
            return String.Format("Microsoft .NET Core {0}.{1}.{2} - {3}", buildVersion.Major, 
            buildVersion.Minor, buildVersion.Patch, suffix);
        }
        public static string GetSharedFxBrandName(BuildTargetContext c)
        {
            return GetBrandName(c, "Runtime");
        }

        public static string GetSharedHostBrandName(BuildTargetContext c)
        {
            return GetBrandName(c, "Host");
        }

        public static string GetHostFxrBrandName(BuildTargetContext c)
        {
            return GetBrandName(c, "Host FX Resolver");
        }

        public static string GetProductMoniker(BuildTargetContext c, string artifactPrefix, string version)
        {
            string rid = Environment.GetEnvironmentVariable("TARGETRID") ?? RuntimeEnvironment.GetRuntimeIdentifier();

            // Look for expected RIDs, including Portable one, for Linux
            if (rid.StartsWith("linux-") || rid == "ubuntu.16.04-x64" || rid == "ubuntu.16.10-x64" || rid == "fedora.23-x64" || rid == "fedora.24-x64" || rid == "opensuse.13.2-x64" || rid == "opensuse.42.1-x64")
            {
                return $"{artifactPrefix}-{rid}.{version}";
            }
            else
            {
                string osname = GetOSShortName();
                string arch = Environment.GetEnvironmentVariable("TARGETPLATFORM") ?? CurrentArchitecture.Current.ToString();
                return $"{artifactPrefix}-{osname}-{arch}.{version}";
            }
        }

        public static string GetBadgeMoniker()
        {
            string rid = Environment.GetEnvironmentVariable("TARGETRID") ?? RuntimeEnvironment.GetRuntimeIdentifier();

            if (rid.StartsWith("linux-"))
            {
                return $"Linux_{Environment.GetEnvironmentVariable("TARGETPLATFORM") ?? CurrentArchitecture.Current.ToString()}";
            }

            switch (RuntimeEnvironment.GetRuntimeIdentifier())
            {
                case "ubuntu.16.04-x64":
                     return "Ubuntu_16_04_x64";
                case "ubuntu.16.10-x64":
                     return "Ubuntu_16_10_x64";
                case "fedora.23-x64":
                     return "Fedora_23_x64";
                case "fedora.24-x64":
                     return "Fedora_24_x64";
                case "opensuse.13.2-x64":
                     return "openSUSE_13_2_x64";
                case "opensuse.42.1-x64":
                     return "openSUSE_42_1_x64";
            }

            return $"{CurrentPlatform.Current}_{Environment.GetEnvironmentVariable("TARGETPLATFORM") ?? CurrentArchitecture.Current.ToString()}";
        }

        public static string GetDebianHostFxrPackageName(string hostfxrNugetVersion)
        {
            return $"dotnet-hostfxr-{hostfxrNugetVersion}".ToLower();
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
