using System.Collections.Generic;

namespace Microsoft.DotNet.Cli.Build
{
    public class HostVersion : Version
    {
        // ------------------------------------------HOST-VERSIONING-------------------------------------------
        //
        // Host versions are independent of CLI versions. Moreover, these version numbers
        // are baked into the binary and is used to look up a serviced binary replacement.
        //

        //
        // Latest hosts for production of nupkgs.
        //

        // Version constants without suffix
        public override int Major => 1;
        public override int Minor => 0;
        public override int Patch => 1;
        public override string ReleaseSuffix => "rc3";
        public string LatestHostVersionNoSuffix => $"{Major}.{Minor}.{Patch}";
        public string LatestHostFxrVersionNoSuffix => $"{Major}.{Minor}.{Patch}";
        public string LatestHostPolicyVersionNoSuffix => $"{Major}.{Minor}.{Patch}";
        public string LatestHostPrerelease => ReleaseSuffix;
        public string LatestHostBuildMajor => $"{CommitCountString}";
        public string LatestHostBuildMinor => "00";
        public string LatestHostSuffix => $"{ReleaseSuffix}-{LatestHostBuildMajor}-{LatestHostBuildMinor}";

        // Full versions and package information.
        public string LatestHostVersion => $"{LatestHostVersionNoSuffix}-{LatestHostSuffix}";
        public string LatestHostFxrVersion => $"{LatestHostFxrVersionNoSuffix}-{LatestHostSuffix}";
        public string LatestHostPolicyVersion => $"{LatestHostPolicyVersionNoSuffix}-{LatestHostSuffix}";
        public Dictionary<string, string> LatestHostPackages => new Dictionary<string, string>()
        {
            { "Microsoft.NETCore.DotNetHost", LatestHostVersion },
            { "Microsoft.NETCore.DotNetHostResolver", LatestHostFxrVersion },
            { "Microsoft.NETCore.DotNetHostPolicy", LatestHostPolicyVersion }
        };

        //
        // Locked muxer for consumption in CLI.
        //
        public bool IsLocked = false; // Set this variable to toggle muxer locking.
        public string LockedHostFxrVersion => IsLocked ? "1.0.1-rc2-002468-00" : LatestHostFxrVersion;
        public string LockedHostVersion => IsLocked ? "1.0.1-rc2-002468-00" : LatestHostVersion;
    }
}
