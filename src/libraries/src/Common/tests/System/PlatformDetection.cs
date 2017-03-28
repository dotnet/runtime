// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace System
{
    public static partial class PlatformDetection
    {
        public static bool IsFullFramework => RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework", StringComparison.OrdinalIgnoreCase);

        public static bool IsWindows { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static bool IsWindows7 { get; } = IsWindows && GetWindowsVersion() == 6 && GetWindowsMinorVersion() == 1;
        public static bool IsWindows8x { get; } = IsWindows && GetWindowsVersion() == 6 && (GetWindowsMinorVersion() == 2 || GetWindowsMinorVersion() == 3);
        public static bool IsOSX { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        public static bool IsNetBSD { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Create("NETBSD"));
        public static bool IsOpenSUSE { get; } = IsDistroAndVersion("opensuse");
        public static bool IsUbuntu { get; } = IsDistroAndVersion("ubuntu");
        public static bool IsNotWindowsNanoServer { get; } = (!IsWindows ||
            File.Exists(Path.Combine(Environment.GetEnvironmentVariable("windir"), "regedit.exe")));
        public static bool IsWindows10Version1607OrGreater { get; } = IsWindows &&
            GetWindowsVersion() == 10 && GetWindowsMinorVersion() == 0 && GetWindowsBuildNumber() >= 14393;
        public static bool IsWindows10VersionInsiderPreviewOrGreater { get; } = IsWindows &&
            GetWindowsVersion() == 10 && GetWindowsMinorVersion() == 0 && GetWindowsBuildNumber() >= 14917;
        // Windows OneCoreUAP SKU doesn't have httpapi.dll
        public static bool HasHttpApi { get; } = (IsWindows &&
            File.Exists(Path.Combine(Environment.GetEnvironmentVariable("windir"), "System32", "httpapi.dll")));

        public static bool IsNotOneCoreUAP { get; } = (!IsWindows || 
            File.Exists(Path.Combine(Environment.GetEnvironmentVariable("windir"), "System32", "httpapi.dll")));

        public static int WindowsVersion { get; } = GetWindowsVersion();

        private static int s_isWinRT = -1;

        public static bool IsWinRT
        {
            get
            {
                if (s_isWinRT != -1)
                    return s_isWinRT == 1;

                if (!IsWindows || IsWindows7)
                {
                    s_isWinRT = 0;
                    return false;
                }

                byte[] buffer = new byte[0];
                uint bufferSize = 0;
                try
                {
                    int result = GetCurrentApplicationUserModelId(ref bufferSize, buffer);
                    switch (result)
                    {
                        case 15703: // APPMODEL_ERROR_NO_APPLICATION
                            s_isWinRT = 0;
                            break;
                        case 0:     // ERROR_SUCCESS
                        case 122:   // ERROR_INSUFFICIENT_BUFFER
                                    // Success is actually insufficent buffer as we're really only looking for
                                    // not NO_APPLICATION and we're not actually giving a buffer here. The
                                    // API will always return NO_APPLICATION if we're not running under a
                                    // WinRT process, no matter what size the buffer is.
                            s_isWinRT = 1;
                            break;
                        default:
                            throw new InvalidOperationException($"Failed to get AppId, result was {result}.");
                    }
                }
                catch (Exception e)
                {
                    // We could catch this here, being friendly with older portable surface area should we
                    // desire to use this method elsewhere.
                    if (e.GetType().FullName.Equals("System.EntryPointNotFoundException", StringComparison.Ordinal))
                    {
                        // API doesn't exist, likely pre Win8
                        s_isWinRT = 0;
                    }
                    else
                    {
                        throw;
                    }
                }

                return s_isWinRT == 1;
            }
        }

        private static Lazy<bool> m_isWindowsSubsystemForLinux = new Lazy<bool>(GetIsWindowsSubsystemForLinux);

        public static bool IsWindowsSubsystemForLinux => m_isWindowsSubsystemForLinux.Value;
        public static bool IsNotWindowsSubsystemForLinux => !IsWindowsSubsystemForLinux;

        public static bool IsNotFedoraOrRedHatOrCentos => !IsDistroAndVersion("fedora") && !IsDistroAndVersion("rhel") && !IsDistroAndVersion("centos");

        private static bool GetIsWindowsSubsystemForLinux()
        {
            // https://github.com/Microsoft/BashOnWindows/issues/423#issuecomment-221627364

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                const string versionFile = "/proc/version";
                if (File.Exists(versionFile))
                {
                    string s = File.ReadAllText(versionFile);

                    if (s.Contains("Microsoft") || s.Contains("WSL"))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsDebian8 { get; } = IsDistroAndVersion("debian", "8");
        public static bool IsUbuntu1404 { get; } = IsDistroAndVersion("ubuntu", "14.04");
        public static bool IsUbuntu1510 { get; } = IsDistroAndVersion("ubuntu", "15.10");
        public static bool IsUbuntu1604 { get; } = IsDistroAndVersion("ubuntu", "16.04");
        public static bool IsUbuntu1610 { get; } = IsDistroAndVersion("ubuntu", "16.10");
        public static bool IsFedora23 { get; } = IsDistroAndVersion("fedora", "23");
        public static bool IsFedora24 { get; } = IsDistroAndVersion("fedora", "24");
        public static bool IsFedora25 { get; } = IsDistroAndVersion("fedora", "25");
        public static bool IsFedora26 { get; } = IsDistroAndVersion("fedora", "26");
        public static bool IsCentos7 { get; } = IsDistroAndVersion("centos", "7");

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
                IdVersionPair v = ParseOsReleaseFile();
                if (v.Id == distroId && (versionId == null || v.VersionId == versionId))
                {
                    return true;
                }
            }

            return false;
        }

        private static IdVersionPair ParseOsReleaseFile()
        {
            Debug.Assert(RuntimeInformation.IsOSPlatform(OSPlatform.Linux));

            IdVersionPair ret = new IdVersionPair();
            ret.Id = "";
            ret.VersionId = "";

            if (File.Exists("/etc/os-release"))
            {
                foreach (string line in File.ReadLines("/etc/os-release"))
                {
                    if (line.StartsWith("ID=", System.StringComparison.Ordinal))
                    {
                        ret.Id = line.Substring("ID=".Length);
                    }
                    else if (line.StartsWith("VERSION_ID=", System.StringComparison.Ordinal))
                    {
                        ret.VersionId = line.Substring("VERSION_ID=".Length);
                    }
                }
            }

            string versionId = ret.VersionId;
            if (versionId.Length >= 2 && versionId[0] == '"' && versionId[versionId.Length - 1] == '"')
            {
                // Remove quotes.
                ret.VersionId = versionId.Substring(1, versionId.Length - 2);
            }

            if (ret.Id.Length >= 2 && ret.Id[0] == '"' && ret.Id[ret.Id.Length - 1] == '"')
            {
                // Remove quotes.
                ret.Id = ret.Id.Substring(1, ret.Id.Length - 2);
            }

            return ret;
        }

        private struct IdVersionPair
        {
            public string Id { get; set; }
            public string VersionId { get; set; }
        }

        private static int GetWindowsVersion()
        {
            if (IsWindows)
            {
                RTL_OSVERSIONINFOEX osvi = new RTL_OSVERSIONINFOEX();
                osvi.dwOSVersionInfoSize = (uint)Marshal.SizeOf(osvi);
                Assert.Equal(0, RtlGetVersion(out osvi));
                return (int)osvi.dwMajorVersion;
            }

            return -1;
        }

        private static int GetWindowsMinorVersion()
        {
            if (IsWindows)
            {
                RTL_OSVERSIONINFOEX osvi = new RTL_OSVERSIONINFOEX();
                osvi.dwOSVersionInfoSize = (uint)Marshal.SizeOf(osvi);
                Assert.Equal(0, RtlGetVersion(out osvi));
                return (int)osvi.dwMinorVersion;
            }

            return -1;
        }

        private static int GetWindowsBuildNumber()
        {
            if (IsWindows)
            {
                RTL_OSVERSIONINFOEX osvi = new RTL_OSVERSIONINFOEX();
                osvi.dwOSVersionInfoSize = (uint)Marshal.SizeOf(osvi);
                Assert.Equal(0, RtlGetVersion(out osvi));
                return (int)osvi.dwBuildNumber;
            }

            return -1;
        }

        [DllImport("ntdll.dll")]
        private static extern int RtlGetVersion(out RTL_OSVERSIONINFOEX lpVersionInformation);

        [StructLayout(LayoutKind.Sequential)]
        private struct RTL_OSVERSIONINFOEX
        {
            internal uint dwOSVersionInfoSize;
            internal uint dwMajorVersion;
            internal uint dwMinorVersion;
            internal uint dwBuildNumber;
            internal uint dwPlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            internal string szCSDVersion;
        }

        private static int s_isWindowsElevated = -1;

        public static bool IsWindowsAndElevated
        {
            get
            {
                if (s_isWindowsElevated != -1)
                    return s_isWindowsElevated == 1;

                if (!IsWindows || IsWinRT)
                {
                    s_isWindowsElevated = 0;
                    return false;
                }

                IntPtr processToken;
                Assert.True(OpenProcessToken(GetCurrentProcess(), TOKEN_READ, out processToken));

                try
                {
                    uint tokenInfo;
                    uint returnLength;
                    Assert.True(GetTokenInformation(
                        processToken, TokenElevation, out tokenInfo, sizeof(uint), out returnLength));

                    s_isWindowsElevated = tokenInfo == 0 ? 0 : 1;
                }
                finally
                {
                    CloseHandle(processToken);
                }

                return s_isWindowsElevated == 1;
            }
        }

        private const uint TokenElevation = 20;
        private const uint STANDARD_RIGHTS_READ = 0x00020000;
        private const uint TOKEN_QUERY = 0x0008;
        private const uint TOKEN_READ = STANDARD_RIGHTS_READ | TOKEN_QUERY;

        [DllImport("advapi32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern bool GetTokenInformation(
            IntPtr TokenHandle,
            uint TokenInformationClass,
            out uint TokenInformation,
            uint TokenInformationLength,
            out uint ReturnLength);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern bool CloseHandle(
            IntPtr handle);

        [DllImport("advapi32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern bool OpenProcessToken(
            IntPtr ProcessHandle,
            uint DesiredAccesss,
            out IntPtr TokenHandle);

        // The process handle does NOT need closing
        [DllImport("kernel32.dll", ExactSpelling = true)]
        public static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", ExactSpelling = true)]
        public static extern int GetCurrentApplicationUserModelId(
            ref uint applicationUserModelIdLength,
            byte[] applicationUserModelId);
    }
}
