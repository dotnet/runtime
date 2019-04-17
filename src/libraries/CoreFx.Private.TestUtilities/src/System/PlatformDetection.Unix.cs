// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Xml.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace System
{
    public static partial class PlatformDetection
    {
        public static bool IsWindowsIoTCore => false;
        public static bool IsWindowsHomeEdition => false;
        public static bool IsWindows => false;
        public static bool IsWindows7 => false;
        public static bool IsWindows8x => false;
        public static bool IsWindows8xOrLater => false;
        public static bool IsWindows10Version1607OrGreater => false;
        public static bool IsWindows10Version1703OrGreater => false;
        public static bool IsWindows10Version1709OrGreater => false;
        public static bool IsWindows10Version1803OrGreater => false;
        public static bool IsWindows10Version1903OrGreater => false;
        public static bool IsNotOneCoreUAP =>  true;
        public static bool IsInAppContainer => false;
        public static int WindowsVersion => -1;

        public static bool IsCentos6 => IsDistroAndVersion("centos", 6);
        public static bool IsAlpine => IsDistroAndVersion("alpine");
        public static bool IsOpenSUSE => IsDistroAndVersion("opensuse");
        public static bool IsUbuntu => IsDistroAndVersion("ubuntu");
        public static bool IsDebian => IsDistroAndVersion("debian");
        public static bool IsDebian8 => IsDistroAndVersion("debian", 8);
        public static bool IsUbuntu1404 => IsDistroAndVersion("ubuntu", 14, 4);
        public static bool IsUbuntu1604 => IsDistroAndVersion("ubuntu", 16, 4);
        public static bool IsUbuntu1704 => IsDistroAndVersion("ubuntu", 17, 4);
        public static bool IsUbuntu1710 => IsDistroAndVersion("ubuntu", 17, 10);
        public static bool IsUbuntu1710OrHigher => IsDistroAndVersionOrHigher("ubuntu", 17, 10);
        public static bool IsUbuntu1804 => IsDistroAndVersion("ubuntu", 18, 04);
        public static bool IsUbuntu1810OrHigher => IsDistroAndVersionOrHigher("ubuntu", 18, 10);
        public static bool IsTizen => IsDistroAndVersion("tizen");
        public static bool IsFedora => IsDistroAndVersion("fedora");
        public static bool IsWindowsNanoServer => false;
        public static bool IsWindowsServerCore => false;
        public static bool IsWindowsAndElevated => false;

        // RedHat family covers RedHat and CentOS
        public static bool IsRedHatFamily => IsRedHatFamilyAndVersion();
        public static bool IsNotRedHatFamily => !IsRedHatFamily;
        public static bool IsRedHatFamily6 => IsRedHatFamilyAndVersion(6);
        public static bool IsNotRedHatFamily6 => !IsRedHatFamily6;
        public static bool IsRedHatFamily7 => IsRedHatFamilyAndVersion(7);
        public static bool IsNotFedoraOrRedHatFamily => !IsFedora && !IsRedHatFamily;

        public static bool TargetsNetFx452OrLower => false;
        public static bool IsNetfx462OrNewer => false;
        public static bool IsNetfx470OrNewer => false;
        public static bool IsNetfx471OrNewer => false;
        public static bool IsNetfx472OrNewer => false;

        public static bool SupportsSsl3 => (PlatformDetection.IsOSX || (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && PlatformDetection.OpenSslVersion < new Version(1, 0, 2) && !PlatformDetection.IsDebian));

        public static bool IsDrawingSupported { get; } =
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
#if netcoreapp20
                ? dlopen("libgdiplus.dylib", RTLD_LAZY) != IntPtr.Zero
                : dlopen("libgdiplus.so", RTLD_LAZY) != IntPtr.Zero || dlopen("libgdiplus.so.0", RTLD_LAZY) != IntPtr.Zero;

        [DllImport("libdl")]
        private static extern IntPtr dlopen(string libName, int flags);
        private const int RTLD_LAZY = 0x001;
#else // use managed NativeLibrary API from .NET Core 3 onwards
                ? NativeLibrary.TryLoad("libgdiplus.dylib", out _)
                : NativeLibrary.TryLoad("libgdiplus.so", out _) || NativeLibrary.TryLoad("libgdiplus.so.0", out _);
#endif

        public static bool IsInContainer => RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && File.Exists("/.dockerenv");

        public static bool IsSoundPlaySupported { get; } = false;

        public static Version OSXVersion { get; } = ToVersion(PlatformApis.GetOSVersion());

        public static Version OpenSslVersion => !RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? GetOpenSslVersion() : throw new PlatformNotSupportedException();

        public static string GetDistroVersionString()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "OSX Version=" + s_osxProductVersion.ToString();
            }

            var (name, version) = GetDistroInfo();

            return "Distro=" + name + " VersionId=" + version;
        }

        /// <summary>
        /// If gnulibc is available, returns the release, such as "stable".
        /// Otherwise returns "glibc_not_found".
        /// </summary>
        public static string LibcRelease
        {
            get
            {
                try
                {
                    return Marshal.PtrToStringUTF8(gnu_get_libc_release());
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
                try
                {
                    return Marshal.PtrToStringUTF8(gnu_get_libc_version());
                }
                catch (Exception e) when (e is DllNotFoundException || e is EntryPointNotFoundException)
                {
                    return "glibc_not_found";
                }
            }
        }

        private static readonly Version s_osxProductVersion = GetOSXProductVersion();

        public static bool IsMacOsHighSierraOrHigher { get; } =
            IsOSX && (s_osxProductVersion.Major > 10 || (s_osxProductVersion.Major == 10 && s_osxProductVersion.Minor >= 13));

        public static bool IsMacOsMojaveOrHigher { get; } =
            IsOSX && (s_osxProductVersion.Major > 10 || (s_osxProductVersion.Major == 10 && s_osxProductVersion.Minor >= 14));

        private static readonly Version s_icuVersion = GetICUVersion();
        public static Version ICUVersion => s_icuVersion;

        private static Version GetICUVersion()
        {
            int ver = GlobalizationNative_GetICUVersion();
            return new Version( ver & 0xFF,
                               (ver >> 8)  & 0xFF,
                               (ver >> 16) & 0xFF,
                                ver >> 24);
        }

        static Version ToVersion(string versionString)
        {
            if (versionString.IndexOf('.') != -1)
                return new Version(versionString);

            // minor version is required by Version
            // let's default it to 0
            return new Version(int.Parse(versionString), 0);
        }

        private static (string name, Version version) GetDistroInfo() =>
            (PlatformApis.GetOSName(), ToVersion(PlatformApis.GetOSVersion()));

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
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var (name, version) = GetDistroInfo();
                if (distroPredicate(name) && VersionEquivalentTo(major, minor, build, revision, version))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsDistroAndVersionOrHigher(Predicate<string> distroPredicate, int major = -1, int minor = -1, int build = -1, int revision = -1)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var (name, version) = GetDistroInfo();
                if (distroPredicate(name) && VersionEquivalentToOrHigher(major, minor, build, revision, version))
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

        private static Version GetOSXProductVersion()
        {
            try
            {
                if (IsOSX)
                {
                    // <plist version="1.0">
                    // <dict>
                    //         <key>ProductBuildVersion</key>
                    //         <string>17A330h</string>
                    //         <key>ProductCopyright</key>
                    //         <string>1983-2017 Apple Inc.</string>
                    //         <key>ProductName</key>
                    //         <string>Mac OS X</string>
                    //         <key>ProductUserVisibleVersion</key>
                    //         <string>10.13</string>
                    //         <key>ProductVersion</key>
                    //         <string>10.13</string>
                    // </dict>
                    // </plist>

                    XElement dict = XDocument.Load("/System/Library/CoreServices/SystemVersion.plist").Root.Element("dict");
                    if (dict != null)
                    {
                        foreach (XElement key in dict.Elements("key"))
                        {
                            if ("ProductVersion".Equals(key.Value))
                            {
                                XElement stringElement = key.NextNode as XElement;
                                if (stringElement != null && stringElement.Name.LocalName.Equals("string"))
                                {
                                    string versionString = stringElement.Value;
                                    if (versionString != null)
                                    {
                                        return Version.Parse(versionString);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            // In case of exception or couldn't get the version 
            return new Version(0, 0, 0);
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

        [DllImport("libc", SetLastError = true)]
        private static extern int sysctlbyname(string ctlName, byte[] oldp, ref IntPtr oldpLen, byte[] newp, IntPtr newpLen);

        [DllImport("libc", SetLastError = true)]
        internal static extern unsafe uint geteuid();

        [DllImport("libc", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr gnu_get_libc_release();

        [DllImport("libc", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr gnu_get_libc_version();

        [DllImport("System.Globalization.Native", SetLastError = true)]
        private static extern int GlobalizationNative_GetICUVersion();

        public static bool IsSuperUser => geteuid() == 0;
    }
}
