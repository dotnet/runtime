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
        public const string RuntimeName = "Microsoft .NET Core";

        private static string GetBrandName(BuildTargetContext c, string suffix)
        {
            var buildVersion = c.BuildContext.Get<BuildVersion>("BuildVersion");
            string releaseBrandingSuffix = c.BuildContext.Get<string>("ReleaseBrandingSuffix")?.Trim();
            string runtimeBaseBranding = $"{RuntimeName} {buildVersion.Major}.{buildVersion.Minor}.{buildVersion.Patch}";

            string brandName = string.IsNullOrEmpty(releaseBrandingSuffix) ?
                               //RTM branding
                               $"{runtimeBaseBranding} {suffix}"
                               //Pre-Release branding
                               : $"{runtimeBaseBranding} {releaseBrandingSuffix} {suffix}";
            return brandName;
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

            // Look for expected RIDs, including Portable ones
            string platformRID = null;
            if (Utils.IsPortableRID(rid, out platformRID) || rid == "ubuntu.16.04-x64" || rid == "ubuntu.16.04-arm" || rid == "ubuntu.16.10-x64" || rid == "fedora.24-x64" || rid == "opensuse.42.1-x64" || rid == "debian.8-armel" || rid == "tizen.4.0.0-armel")
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

        public static string GetBadgeMoniker(BuildTargetContext c)
        {
            string rid = c.BuildContext.Get<string>("TargetRID");
            string platformRID = null;
            if (Utils.IsPortableRID(rid, out platformRID))
            {
                return $"{platformRID}_portable_{c.BuildContext.Get<string>("Platform")}";
            }

            switch (rid)
            {
                case "ubuntu.16.04-x64":
                     return "Ubuntu_16_04_x64";
                case "ubuntu.16.04-arm":
                     return "Ubuntu_16_04_arm";
                case "ubuntu.16.10-x64":
                     return "Ubuntu_16_10_x64";
                case "fedora.24-x64":
                     return "Fedora_24_x64";
                case "opensuse.42.1-x64":
                     return "openSUSE_42_1_x64";
                case "debian.8-armel":
                     return "Debian_8_armel";
                case "tizen.4.0.0-armel":
                     return "Tizen_4_0_0_armel";
            }

            return $"{CurrentPlatform.Current}_{c.BuildContext.Get<string>("Platform")}";
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
