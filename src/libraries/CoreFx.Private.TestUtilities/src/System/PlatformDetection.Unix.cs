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
        public static bool IsWindows => false;
        public static bool IsWindows7 => false;
        public static bool IsWindows8x => false;
        public static bool IsWindows10Version1607OrGreater => false;
        public static bool IsWindows10Version1703OrGreater => false;
        public static bool IsWindows10Version1709OrGreater => false;
        public static bool IsNotOneCoreUAP =>  true;
        public static bool IsInAppContainer => false;
        public static int WindowsVersion => -1;

        public static bool IsCentos6 => IsDistroAndVersion("centos", 6);
        public static bool IsOpenSUSE => IsDistroAndVersion("opensuse");
        public static bool IsUbuntu => IsDistroAndVersion("ubuntu");
        public static bool IsDebian => IsDistroAndVersion("debian");
        public static bool IsDebian8 => IsDistroAndVersion("debian", 8);
        public static bool IsUbuntu1404 => IsDistroAndVersion("ubuntu", 14, 4);
        public static bool IsUbuntu1604 => IsDistroAndVersion("ubuntu", 16, 4);
        public static bool IsUbuntu1704 => IsDistroAndVersion("ubuntu", 17, 4);
        public static bool IsUbuntu1710 => IsDistroAndVersion("ubuntu", 17, 10);
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

        public static Version OSXVersion { get; } = ToVersion(Microsoft.DotNet.PlatformAbstractions.RuntimeEnvironment.OperatingSystemVersion);

        public static Version OpenSslVersion => RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? Interop.OpenSsl.OpenSslVersion : throw new PlatformNotSupportedException();

        public static string GetDistroVersionString()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "OSX Version=" + s_osxProductVersion.ToString();
            }

            DistroInfo v = GetDistroInfo();

            return "Distro=" + v.Id + " VersionId=" + v.VersionId;
        }

        private static readonly Version s_osxProductVersion = GetOSXProductVersion();

        public static bool IsMacOsHighSierraOrHigher { get; } =
            IsOSX && (s_osxProductVersion.Major > 10 || (s_osxProductVersion.Major == 10 && s_osxProductVersion.Minor >= 13));

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

        private static DistroInfo GetDistroInfo() => new DistroInfo()
        {
            Id = Microsoft.DotNet.PlatformAbstractions.RuntimeEnvironment.OperatingSystem,
            VersionId = ToVersion(Microsoft.DotNet.PlatformAbstractions.RuntimeEnvironment.OperatingSystemVersion)
        };

        private static bool IsRedHatFamilyAndVersion(int major = -1, int minor = -1, int build = -1, int revision = -1)
        {
            return IsDistroAndVersion((distro) => distro == "rhel" || distro == "centos", major, minor, build, revision);
        }

        /// <summary>
        /// Get whether the OS platform matches the given Linux distro and optional version.
        /// </summary>
        /// <param name="distroId">The distribution id.</param>
        /// <param name="versionId">The distro version.  If omitted, compares the distro only.</param>
        /// <returns>Whether the OS platform matches the given Linux distro and optional version.</returns>
        private static bool IsDistroAndVersion(string distroId, int major = -1, int minor = -1, int build = -1, int revision = -1)
        {
            return IsDistroAndVersion((distro) => distro == distroId, major, minor, build, revision);
        }

        private static bool IsDistroAndVersion(Predicate<string> distroPredicate, int major = -1, int minor = -1, int build = -1, int revision = -1)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                DistroInfo v = GetDistroInfo();
                if (distroPredicate(v.Id) && VersionEquivalentWith(major, minor, build, revision, v.VersionId))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool VersionEquivalentWith(int major, int minor, int build, int revision, Version actualVersionId)
        {
            return (major == -1 || major == actualVersionId.Major)
                && (minor == -1 || minor == actualVersionId.Minor)
                && (build == -1 || build == actualVersionId.Build)
                && (revision == -1 || revision == actualVersionId.Revision);
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

        [DllImport("libc", SetLastError = true)]
        private static extern int sysctlbyname(string ctlName, byte[] oldp, ref IntPtr oldpLen, byte[] newp, IntPtr newpLen);

        [DllImport("libc", SetLastError = true)]
        internal static extern unsafe uint geteuid();

        [DllImport("System.Globalization.Native", SetLastError = true)]
        private static extern int GlobalizationNative_GetICUVersion();

        public static bool IsSuperUser => geteuid() == 0;

        private struct DistroInfo
        {
            public string Id { get; set; }
            public Version VersionId { get; set; }
        }
    }
}
