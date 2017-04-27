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
        //
        // Do not use the " { get; } = <expression> " pattern here. Having all the initialization happen in the type initializer
        // means that one exception anywhere means all tests using PlatformDetection fail. If you feel a value is worth latching,
        // do it in a way that failures don't cascade.
        //

        public static bool IsFullFramework => RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework", StringComparison.OrdinalIgnoreCase);
        public static bool IsNetNative => RuntimeInformation.FrameworkDescription.StartsWith(".NET Native", StringComparison.OrdinalIgnoreCase);

        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static bool IsWindows7 => IsWindows && GetWindowsVersion() == 6 && GetWindowsMinorVersion() == 1;
        public static bool IsWindows8x => IsWindows && GetWindowsVersion() == 6 && (GetWindowsMinorVersion() == 2 || GetWindowsMinorVersion() == 3);
        public static bool IsOSX => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        public static bool IsNetBSD => RuntimeInformation.IsOSPlatform(OSPlatform.Create("NETBSD"));
        public static bool IsOpenSUSE => IsDistroAndVersion("opensuse");
        public static bool IsUbuntu => IsDistroAndVersion("ubuntu");
        public static bool IsNotWindowsNanoServer => (!IsWindows ||
            File.Exists(Path.Combine(Environment.GetEnvironmentVariable("windir"), "regedit.exe")));
        public static bool IsWindows10Version1607OrGreater => IsWindows &&
            GetWindowsVersion() == 10 && GetWindowsMinorVersion() == 0 && GetWindowsBuildNumber() >= 14393;
        public static bool IsWindows10Version1703OrGreater => IsWindows &&
            GetWindowsVersion() == 10 && GetWindowsMinorVersion() == 0 && GetWindowsBuildNumber() >= 15063;
        // Windows OneCoreUAP SKU doesn't have httpapi.dll
        public static bool HasHttpApi => (IsWindows &&
            File.Exists(Path.Combine(Environment.GetEnvironmentVariable("windir"), "System32", "httpapi.dll")));

        public static bool IsNotOneCoreUAP => (!IsWindows || 
            File.Exists(Path.Combine(Environment.GetEnvironmentVariable("windir"), "System32", "httpapi.dll")));

        public static int WindowsVersion => GetWindowsVersion();

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

        public static bool IsFedora => IsDistroAndVersion("fedora");

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

        public static bool IsDebian8 => IsDistroAndVersion("debian", "8");
        public static bool IsUbuntu1404 => IsDistroAndVersion("ubuntu", "14.04");
        public static bool IsUbuntu1510 => IsDistroAndVersion("ubuntu", "15.10");
        public static bool IsUbuntu1604 => IsDistroAndVersion("ubuntu", "16.04");
        public static bool IsUbuntu1610 => IsDistroAndVersion("ubuntu", "16.10");
        public static bool IsFedora24 => IsDistroAndVersion("fedora", "24");
        public static bool IsFedora25 => IsDistroAndVersion("fedora", "25");
        public static bool IsFedora26 => IsDistroAndVersion("fedora", "26");
        public static bool IsCentos7 => IsDistroAndVersion("centos", "7");

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

        public static bool IsNonZeroLowerBoundArraySupported
        {
            get
            {
                if (s_lazyNonZeroLowerBoundArraySupported == null)
                {
                    bool nonZeroLowerBoundArraysSupported = false;
                    try
                    {
                        Array.CreateInstance(typeof(int), new int[] { 5 }, new int[] { 5 });
                        nonZeroLowerBoundArraysSupported = true;
                    }
                    catch (PlatformNotSupportedException)
                    {
                    }
                    s_lazyNonZeroLowerBoundArraySupported = Tuple.Create<bool>(nonZeroLowerBoundArraysSupported);
                }
                return s_lazyNonZeroLowerBoundArraySupported.Item1;
            }
        }

        private static volatile Tuple<bool> s_lazyNonZeroLowerBoundArraySupported;
    }
}
