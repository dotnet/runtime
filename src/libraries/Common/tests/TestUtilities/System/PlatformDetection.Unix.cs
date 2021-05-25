// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;

namespace System
{
    public static partial class PlatformDetection
    {
        //
        // Do not use the " { get; } = <expression> " pattern here. Having all the initialization happen in the type initializer
        // means that one exception anywhere means all tests using PlatformDetection fail. If you feel a value is worth latching,
        // do it in a way that failures don't cascade.
        //

        private static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        public static bool IsOpenSUSE => IsDistroAndVersion("opensuse");
        public static bool IsUbuntu => IsDistroAndVersion("ubuntu");
        public static bool IsDebian => IsDistroAndVersion("debian");
        public static bool IsAlpine => IsDistroAndVersion("alpine");
        public static bool IsDebian8 => IsDistroAndVersion("debian", 8);
        public static bool IsDebian10 => IsDistroAndVersion("debian", 10);
        public static bool IsUbuntu1604 => IsDistroAndVersion("ubuntu", 16, 4);
        public static bool IsUbuntu1704 => IsDistroAndVersion("ubuntu", 17, 4);
        public static bool IsUbuntu1710 => IsDistroAndVersion("ubuntu", 17, 10);
        public static bool IsUbuntu1710OrHigher => IsDistroAndVersionOrHigher("ubuntu", 17, 10);
        public static bool IsUbuntu1804 => IsDistroAndVersion("ubuntu", 18, 04);
        public static bool IsUbuntu1810OrHigher => IsDistroAndVersionOrHigher("ubuntu", 18, 10);
        public static bool IsSLES => IsDistroAndVersion("sles");
        public static bool IsTizen => IsDistroAndVersion("tizen");
        public static bool IsFedora => IsDistroAndVersion("fedora");

        // OSX family
        public static bool IsOSXLike => IsOSX || IsiOS || IstvOS || IsMacCatalyst;
        public static bool IsOSX => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        public static bool IsNotOSX => !IsOSX;
        public static bool IsMacOsMojaveOrHigher => IsOSX && Environment.OSVersion.Version >= new Version(10, 14);
        public static bool IsMacOsCatalinaOrHigher => IsOSX && Environment.OSVersion.Version >= new Version(10, 15);
        public static bool IsMacOsAppleSilicon => IsOSX && IsArm64Process;
        public static bool IsNotMacOsAppleSilicon => !IsMacOsAppleSilicon;

        // RedHat family covers RedHat and CentOS
        public static bool IsRedHatFamily => IsRedHatFamilyAndVersion();
        public static bool IsNotRedHatFamily => !IsRedHatFamily;
        public static bool IsRedHatFamily7 => IsRedHatFamilyAndVersion(7);
        public static bool IsNotFedoraOrRedHatFamily => !IsFedora && !IsRedHatFamily;
        public static bool IsNotDebian10 => !IsDebian10;

        public static bool IsSuperUser => IsBrowser || IsWindows ? false : libc.geteuid() == 0;

        public static Version OpenSslVersion => !IsOSXLike && !IsWindows && !IsAndroid ?
            GetOpenSslVersion() :
            throw new PlatformNotSupportedException();

        /// <summary>
        /// If gnulibc is available, returns the release, such as "stable".
        /// Otherwise returns "glibc_not_found".
        /// </summary>
        public static string LibcRelease
        {
            get
            {
                if (IsWindows)
                {
                    return "glibc_not_found";
                }

                try
                {
                    return Marshal.PtrToStringAnsi(libc.gnu_get_libc_release());
                }
                catch (Exception e) when (e is DllNotFoundException || e is EntryPointNotFoundException)
                {
                    return "glibc_not_found";
                }
            }
        }

        /// <summary>
        /// If gnulibc is available, returns the version, such as "2.22".
        /// Otherwise returns "glibc_not_found". (In future could run "ldd -version" for musl)
        /// </summary>
        public static string LibcVersion
        {
            get
            {
                if (IsWindows)
                {
                    return "glibc_not_found";
                }

                try
                {
                    return Marshal.PtrToStringAnsi(libc.gnu_get_libc_version());
                }
                catch (Exception e) when (e is DllNotFoundException || e is EntryPointNotFoundException)
                {
                    return "glibc_not_found";
                }
            }
        }

        public static bool OpenSslPresentOnSystem
        {
            get
            {
                if (IsAndroid || UsesMobileAppleCrypto || IsBrowser)
                {
                    return false;
                }

                return Interop.OpenSslNoInit.OpenSslIsAvailable;
            }
        }

        private static Version s_opensslVersion;
        private static Version GetOpenSslVersion()
        {
            if (s_opensslVersion == null)
            {
                // OpenSSL version numbers are encoded as
                // 0xMNNFFPPS: major (one nybble), minor (one byte, unaligned),
                // "fix" (one byte, unaligned), patch (one byte, unaligned), status (one nybble)
                //
                // e.g. 1.0.2a final is 0x1000201F
                //
                // Currently they don't exceed 29-bit values, but we use long here to account
                // for the expanded range on their 64-bit C-long return value.
                long versionNumber = Interop.OpenSsl.OpenSslVersionNumber();
                int major = (int)((versionNumber >> 28) & 0xF);
                int minor = (int)((versionNumber >> 20) & 0xFF);
                int fix = (int)((versionNumber >> 12) & 0xFF);

                s_opensslVersion = new Version(major, minor, fix);
            }

            return s_opensslVersion;
        }

        private static Version ToVersion(string versionString)
        {
            // In some distros/versions we cannot discover the distro version; return something valid.
            // Pick a high version number, since this seems to happen on newer distros.
            if (string.IsNullOrEmpty(versionString))
            {
                versionString = new Version(Int32.MaxValue, Int32.MaxValue).ToString();
            }

            try
            {
                if (versionString.IndexOf('.') != -1)
                    return new Version(versionString);

                // minor version is required by Version
                // let's default it to 0
                return new Version(int.Parse(versionString), 0);
            }
            catch (Exception exc)
            {
                throw new FormatException($"Failed to parse version string: '{versionString}'", exc);
            }
        }

        private static DistroInfo GetDistroInfo()
        {
            DistroInfo result = new DistroInfo();

            if (IsFreeBSD)
            {
                result.Id = "FreeBSD";
                // example:
                // FreeBSD 11.0-RELEASE-p1 FreeBSD 11.0-RELEASE-p1 #0 r306420: Thu Sep 29 01:43:23 UTC 2016     root@releng2.nyi.freebsd.org:/usr/obj/usr/src/sys/GENERIC
                // What we want is major release as minor releases should be compatible.
                result.VersionId = ToVersion(RuntimeInformation.OSDescription.Split()[1].Split('.')[0]);
            }
            else if (Isillumos)
            {
                // examples:
                //   on OmniOS
                //       SunOS 5.11 omnios-r151018-95eaa7e
                //   on OpenIndiana Hipster:
                //       SunOS 5.11 illumos-63878f749f
                //   on SmartOS:
                //       SunOS 5.11 joyent_20200408T231825Z
                var versionDescription = RuntimeInformation.OSDescription.Split(' ')[2];
                switch (versionDescription)
                {
                    case string version when version.StartsWith("omnios"):
                        result.Id = "OmniOS";
                        result.VersionId = ToVersion(version.Substring("omnios-r".Length, 2)); // e.g. 15
                        break;
                    case string version when version.StartsWith("joyent"):
                        result.Id = "SmartOS";
                        result.VersionId = ToVersion(version.Substring("joyent_".Length, 4)); // e.g. 2020
                        break;
                    case string version when version.StartsWith("illumos"):
                        result.Id = "OpenIndiana";
                        // version-less
                        break;
                }
            }
            else if (IsSolaris)
            {
                // example:
                //   SunOS 5.11 11.3
                result.Id = "Solaris";
                // we only need the major version; 11
                result.VersionId = ToVersion(RuntimeInformation.OSDescription.Split(' ')[2].Split('.')[0]); // e.g. 11
            }
            else if (File.Exists("/etc/os-release"))
            {
                foreach (string line in File.ReadAllLines("/etc/os-release"))
                {
                    if (line.StartsWith("ID=", StringComparison.Ordinal))
                    {
                        result.Id = line.Substring(3).Trim('"', '\'');
                    }
                    else if (line.StartsWith("VERSION_ID=", StringComparison.Ordinal))
                    {
                        result.VersionId = ToVersion(line.Substring(11).Trim('"', '\''));
                    }
                }
            }

            result.Id ??= "Linux";
            result.VersionId ??= ToVersion(string.Empty);

            return result;
        }

        private static bool IsRedHatFamilyAndVersion(int major = -1, int minor = -1, int build = -1, int revision = -1)
        {
            return IsDistroAndVersion((distro) => distro == "rhel" || distro == "centos", major, minor, build, revision);
        }

        /// <summary>
        /// Get whether the OS platform matches the given Linux distro and optional version.
        /// </summary>
        /// <param name="distroId">The distribution id.</param>
        /// <param name="major">The distro major version. If omitted, this portion of the version is not included in the comparison.</param>
        /// <param name="minor">The distro minor version. If omitted, this portion of the version is not included in the comparison.</param>
        /// <param name="build">The distro build version. If omitted, this portion of the version is not included in the comparison.</param>
        /// <param name="revision">The distro revision version. If omitted, this portion of the version is not included in the comparison.</param>
        /// <returns>Whether the OS platform matches the given Linux distro and optional version.</returns>
        private static bool IsDistroAndVersion(string distroId, int major = -1, int minor = -1, int build = -1, int revision = -1)
        {
            return IsDistroAndVersion(distro => (distro == distroId), major, minor, build, revision);
        }

        /// <summary>
        /// Get whether the OS platform matches the given Linux distro and optional version is same or higher.
        /// </summary>
        /// <param name="distroId">The distribution id.</param>
        /// <param name="major">The distro major version. If omitted, this portion of the version is not included in the comparison.</param>
        /// <param name="minor">The distro minor version. If omitted, this portion of the version is not included in the comparison.</param>
        /// <param name="build">The distro build version. If omitted, this portion of the version is not included in the comparison.</param>
        /// <param name="revision">The distro revision version.  If omitted, this portion of the version is not included in the comparison.</param>
        /// <returns>Whether the OS platform matches the given Linux distro and optional version is same or higher.</returns>
        private static bool IsDistroAndVersionOrHigher(string distroId, int major = -1, int minor = -1, int build = -1, int revision = -1)
        {
            return IsDistroAndVersionOrHigher(distro => (distro == distroId), major, minor, build, revision);
        }

        private static bool IsDistroAndVersion(Predicate<string> distroPredicate, int major = -1, int minor = -1, int build = -1, int revision = -1)
        {
            if (IsLinux)
            {
                DistroInfo v = GetDistroInfo();
                if (distroPredicate(v.Id) && VersionEquivalentTo(major, minor, build, revision, v.VersionId))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsDistroAndVersionOrHigher(Predicate<string> distroPredicate, int major = -1, int minor = -1, int build = -1, int revision = -1)
        {
            if (IsLinux)
            {
                DistroInfo v = GetDistroInfo();
                if (distroPredicate(v.Id) && VersionEquivalentToOrHigher(major, minor, build, revision, v.VersionId))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool VersionEquivalentTo(int major, int minor, int build, int revision, Version actualVersionId)
        {
            return (major == -1 || major == actualVersionId.Major)
                && (minor == -1 || minor == actualVersionId.Minor)
                && (build == -1 || build == actualVersionId.Build)
                && (revision == -1 || revision == actualVersionId.Revision);
        }

        private static bool VersionEquivalentToOrHigher(int major, int minor, int build, int revision, Version actualVersionId)
        {
            return
                VersionEquivalentTo(major, minor, build, revision, actualVersionId) ||
                    (actualVersionId.Major > major ||
                        (actualVersionId.Major == major && (actualVersionId.Minor > minor ||
                            (actualVersionId.Minor == minor && (actualVersionId.Build > build ||
                                (actualVersionId.Build == build && (actualVersionId.Revision > revision ||
                                    (actualVersionId.Revision == revision))))))));
        }

        private struct DistroInfo
        {
            public string Id { get; set; }
            public Version VersionId { get; set; }
        }

        private static class libc
        {
            [DllImport("libc", SetLastError = true)]
            public static extern unsafe uint geteuid();

            [DllImport("libc", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr gnu_get_libc_release();

            [DllImport("libc", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr gnu_get_libc_version();
        }
    }
}
