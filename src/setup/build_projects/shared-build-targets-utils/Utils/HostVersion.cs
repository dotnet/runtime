using System;
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

        public struct VerInfo
        {
            public int Major;
            public int Minor;
            public int Patch;
            public string Release;
            public string BuildMajor;
            public string BuildMinor;
            public string CommitCountString;

            public VerInfo(int major, int minor, int patch, string release, string buildMajor, string buildMinor, string commitCountString)
            {
                Major = major;
                Minor = minor;
                Patch = patch;
                Release = release;
                BuildMajor = buildMajor;
                BuildMinor = buildMinor;
                CommitCountString = commitCountString;
            }

            public string GenerateMsiVersion()
            {
                return Version.GenerateMsiVersion(Major, Minor, Patch, Int32.Parse(VerRsrcBuildMajor));
            }

            public string WithoutSuffix => $"{Major}.{Minor}.{Patch}";

            // The version numbers to be included in the embedded version resource (.rc) files.
            public string VerRsrcBuildMajor => !string.IsNullOrEmpty(BuildMajor) ? BuildMajor : CommitCountString;
            public string VerRsrcBuildMinor => !string.IsNullOrEmpty(BuildMinor) ? BuildMinor : "00";

            public override string ToString()
            {
                string suffix = "";
                foreach (var verPad in new string[] { Release, BuildMajor, BuildMinor })
                {
                    if (!string.IsNullOrEmpty(verPad))
                    {
                        suffix += $"-{verPad}";
                    }
                }
                return $"{Major}.{Minor}.{Patch}{suffix}";
            }
        }
        //
        // Latest hosts for production of nupkgs.
        //

        // Full versions and package information.
        public string LatestHostBuildMajor => CommitCountString;
        public string LatestHostBuildMinor => "00";
        public bool EnsureStableVersion { get; set; }
        public VerInfo LatestHostVersion => new VerInfo(Major, Minor, Patch, ReleaseSuffix, LatestHostBuildMajor, LatestHostBuildMinor, CommitCountString);
        public VerInfo LatestAppHostVersion => new VerInfo(Major, Minor, Patch, ReleaseSuffix, LatestHostBuildMajor, LatestHostBuildMinor, CommitCountString);
        public VerInfo LatestHostFxrVersion => new VerInfo(Major, Minor, Patch, ReleaseSuffix, LatestHostBuildMajor, LatestHostBuildMinor, CommitCountString);
        public VerInfo LatestHostPolicyVersion => new VerInfo(Major, Minor, Patch, ReleaseSuffix, LatestHostBuildMajor, LatestHostBuildMinor, CommitCountString);
        public Dictionary<string, VerInfo> LatestHostPackages => new Dictionary<string, VerInfo>()
        {
            { "Microsoft.NETCore.DotNetHost", LatestHostVersion },
            { "Microsoft.NETCore.DotNetAppHost", LatestAppHostVersion },
            { "Microsoft.NETCore.DotNetHostResolver", LatestHostFxrVersion },
            { "Microsoft.NETCore.DotNetHostPolicy", LatestHostPolicyVersion }
        };
        public Dictionary<string, VerInfo> LatestHostPackagesToValidate => new Dictionary<string, VerInfo>()
        {
            { "Microsoft.NETCore.DotNetHost", LatestHostVersion },
            { "Microsoft.NETCore.DotNetAppHost", LatestAppHostVersion },
            { "Microsoft.NETCore.DotNetHostResolver", LatestHostFxrVersion },
            { "Microsoft.NETCore.DotNetHostPolicy", LatestHostPolicyVersion }
        };
        public Dictionary<string, VerInfo> LatestHostBinaries => new Dictionary<string, VerInfo>()
        {
            { "dotnet", LatestHostVersion },
            { "apphost", LatestAppHostVersion },
            { "hostfxr", LatestHostFxrVersion },
            { "hostpolicy", LatestHostPolicyVersion }
        };

        //
        // Locked muxer for consumption in CLI.
        //
        // Set this variable to toggle muxer locking.
        public bool IsLocked { get; set; } 
        public VerInfo LockedHostFxrVersion => IsLocked ? new VerInfo(Major, Minor, Patch, "", "", "", CommitCountString) : LatestHostFxrVersion;
        public VerInfo LockedHostVersion    => IsLocked ? new VerInfo(Major, Minor, Patch, "", "", "", CommitCountString) : LatestHostVersion;
    }
}
