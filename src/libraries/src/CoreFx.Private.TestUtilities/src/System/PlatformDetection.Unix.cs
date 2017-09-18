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
        public static bool IsWindows10InsiderPreviewBuild16215OrGreater => false;
        public static bool IsWindows10Version16251OrGreater => false;
        public static bool IsNotOneCoreUAP =>  true;
        public static bool IsNetfx462OrNewer() { return false; }
        public static bool IsNetfx470OrNewer() { return false; }
        public static bool IsNetfx471OrNewer() { return false; }
        public static bool IsInAppContainer => false;
        public static int WindowsVersion => -1;

        public static bool IsOpenSUSE => IsDistroAndVersion("opensuse");
        public static bool IsUbuntu => IsDistroAndVersion("ubuntu");
        public static bool IsDebian => IsDistroAndVersion("debian");
        public static bool IsDebian8 => IsDistroAndVersion("debian", "8");
        public static bool IsUbuntu1404 => IsDistroAndVersion("ubuntu", "14.04");
        public static bool IsUbuntu1604 => IsDistroAndVersion("ubuntu", "16.04");
        public static bool IsUbuntu1704 => IsDistroAndVersion("ubuntu", "17.04");
        public static bool IsUbuntu1710 => IsDistroAndVersion("ubuntu", "17.10");
        public static bool IsCentos7 => IsDistroAndVersion("centos", "7");
        public static bool IsTizen => IsDistroAndVersion("tizen");
        public static bool IsNotFedoraOrRedHatOrCentos => !IsDistroAndVersion("fedora") && !IsDistroAndVersion("rhel") && !IsDistroAndVersion("centos");
        public static bool IsFedora => IsDistroAndVersion("fedora");
        public static bool IsWindowsNanoServer => false;
        public static bool IsWindowsServerCore => false;
        public static bool IsWindowsAndElevated => false;
        public static bool IsWindowsRedStone2 => false; 

        public static bool IsRedHat => IsDistroAndVersion("rhel") || IsDistroAndVersion("rhl");
        public static bool IsNotRedHat => !IsRedHat;
        public static bool IsRedHat69 => IsDistroAndVersion("rhel", "6.9") || IsDistroAndVersion("rhl", "6.9");
        public static bool IsNotRedHat69 => !IsRedHat69;

        public static Version OSXKernelVersion { get; } = GetOSXKernelVersion();

        public static string GetDistroVersionString()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "OSX Version=" + s_osxProductVersion.ToString();
            }

            DistroInfo v = ParseOsReleaseFile();

            return "Distro=" + v.Id + " VersionId=" + v.VersionId + " Pretty=" + v.PrettyName + " Version=" + v.Version;
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

        private static DistroInfo ParseOsReleaseFile()
        {
            Debug.Assert(RuntimeInformation.IsOSPlatform(OSPlatform.Linux));

            DistroInfo ret = new DistroInfo();
            ret.Id = "";
            ret.VersionId = "";
            ret.Version = "";
            ret.PrettyName = "";

            if (File.Exists("/etc/os-release"))
            {
                foreach (string line in File.ReadLines("/etc/os-release"))
                {
                    if (line.StartsWith("ID=", System.StringComparison.Ordinal))
                    {
                        ret.Id = RemoveQuotes(line.Substring("ID=".Length));
                    }
                    else if (line.StartsWith("VERSION_ID=", System.StringComparison.Ordinal))
                    {
                        ret.VersionId = RemoveQuotes(line.Substring("VERSION_ID=".Length));
                    }
                    else if (line.StartsWith("VERSION=", System.StringComparison.Ordinal))
                    {
                        ret.Version = RemoveQuotes(line.Substring("VERSION=".Length));
                    }
                    else if (line.StartsWith("PRETTY_NAME=", System.StringComparison.Ordinal))
                    {
                        ret.PrettyName = RemoveQuotes(line.Substring("PRETTY_NAME=".Length));
                    }
                }
            }
            else 
            {
                string fileName = null;
                if (File.Exists("/etc/redhat-release"))
                    fileName = "/etc/redhat-release";

                if (fileName == null && File.Exists("/etc/system-release"))
                    fileName = "/etc/system-release";
                
                if (fileName != null)
                {
                    // Parse the format like the following line:
                    // Red Hat Enterprise Linux Server release 7.3 (Maipo)
                    using (StreamReader file = new StreamReader(fileName))
                    {
                        string line = file.ReadLine();
                        if (!String.IsNullOrEmpty(line))
                        {
                            if (line.StartsWith("Red Hat Enterprise Linux", StringComparison.OrdinalIgnoreCase))
                            {
                                ret.Id = "rhel";
                            }
                            else if (line.StartsWith("Centos", StringComparison.OrdinalIgnoreCase))
                            {
                                ret.Id = "centos";
                            }
                            else if (line.StartsWith("Red Hat", StringComparison.OrdinalIgnoreCase))
                            {
                                ret.Id = "rhl";
                            }
                            else 
                            {
                                // automatically generate the distro label
                                string [] words = line.Split(' ');
                                StringBuilder sb = new StringBuilder();

                                foreach (string word in words)
                                {
                                    if (word.Length > 0)
                                    {
                                        if (Char.IsNumber(word[0]) || 
                                            word.Equals("release", StringComparison.OrdinalIgnoreCase) ||
                                            word.Equals("server", StringComparison.OrdinalIgnoreCase))
                                            {
                                                break;
                                            }
                                        sb.Append(Char.ToLower(word[0]));
                                    }
                                }
                                ret.Id = sb.ToString();
                            }

                            int i = 0;
                            while (i < line.Length && !Char.IsNumber(line[i])) // stop at first number
                                i++;

                            if (i < line.Length)
                            {
                                int j = i + 1;
                                while (j < line.Length && (Char.IsNumber(line[j]) || line[j] == '.'))
                                    j++;

                                ret.VersionId = line.Substring(i, j - i);
                                ret.Version = line.Substring(i, line.Length - i);
                            }

                            ret.PrettyName = line;
                        }
                    }
                }
            }

            return ret;
        }

        private static string RemoveQuotes(string s)
        {
            s = s.Trim();
            if (s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == '"')
            {
                // Remove quotes.
                s = s.Substring(1, s.Length - 2);
            }

            return s;
        }

        private struct DistroInfo
        {
            public string Id { get; set; }
            public string VersionId { get; set; }
            public string Version { get; set; }
            public string PrettyName { get; set; }
        }

        /// <summary>
        /// Get whether the OS platform matches the given Linux distro and optional version.
        /// </summary>
        /// <param name="distroId">The distribution id.</param>
        /// <param name="versionId">The distro version.  If omitted, compares the distro only.</param>
        /// <returns>Whether the OS platform matches the given Linux distro and optional version.</returns>
        private static bool IsDistroAndVersion(string distroId, string versionId = null)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                DistroInfo v = ParseOsReleaseFile();
                if (v.Id == distroId && (versionId == null || v.VersionId == versionId))
                {
                    return true;
                }
            }

            return false;
        }

        private static Version GetOSXKernelVersion()
        {
            if (IsOSX)
            {
                byte[] bytes = new byte[256];
                IntPtr bytesLength = new IntPtr(bytes.Length);
                Assert.Equal(0, sysctlbyname("kern.osrelease", bytes, ref bytesLength, null, IntPtr.Zero));
                string versionString = Encoding.UTF8.GetString(bytes);
                return Version.Parse(versionString);
            }

            return new Version(0, 0, 0);
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
    }
}
