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
        public static Version OSXKernelVersion => throw new PlatformNotSupportedException();
        public static bool IsSuperUser => throw new PlatformNotSupportedException();
        public static bool IsOpenSUSE => false;
        public static bool IsUbuntu => false;
        public static bool IsDebian => false;
        public static bool IsDebian8 => false;
        public static bool IsUbuntu1404 => false;
        public static bool IsCentos7 => false;
        public static bool IsTizen => false;
        public static bool IsNotFedoraOrRedHatOrCentos => true;
        public static bool IsFedora => false;
        public static bool IsWindowsNanoServer => (IsNotWindowsIoTCore && !File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "regedit.exe")));
        public static int WindowsVersion => GetWindowsVersion();
        public static bool IsMacOsHighSierraOrHigher { get; } = false;

        public static bool IsWindows10Version1607OrGreater => 
            GetWindowsVersion() == 10 && GetWindowsMinorVersion() == 0 && GetWindowsBuildNumber() >= 14393;
        public static bool IsWindows10Version1703OrGreater => 
            GetWindowsVersion() == 10 && GetWindowsMinorVersion() == 0 && GetWindowsBuildNumber() >= 15063;
        public static bool IsWindows10InsiderPreviewBuild16215OrGreater => 
            GetWindowsVersion() == 10 && GetWindowsMinorVersion() == 0 && GetWindowsBuildNumber() >= 16215;
        public static bool IsWindows10Version16251OrGreater => 
            GetWindowsVersion() == 10 && GetWindowsMinorVersion() == 0 && GetWindowsBuildNumber() >= 16251;

        // Windows OneCoreUAP SKU doesn't have httpapi.dll
        public static bool IsNotOneCoreUAP =>  
            File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "httpapi.dll"));

        public static bool IsWindowsIoTCore
        {
            get
            {
                int productType;
                Assert.True(GetProductInfo(Environment.OSVersion.Version.Major, Environment.OSVersion.Version.Minor, 0, 0, out productType));
                if ((productType == PRODUCT_IOTUAPCOMMERCIAL) ||
                    (productType == PRODUCT_IOTUAP))
                {
                    return true;
                }
                return false;
            }
        }

        public static bool IsWindows => true;
        public static bool IsWindows7 => GetWindowsVersion() == 6 && GetWindowsMinorVersion() == 1;
        public static bool IsWindows8x => GetWindowsVersion() == 6 && (GetWindowsMinorVersion() == 2 || GetWindowsMinorVersion() == 3);

        public static bool IsNetfx462OrNewer()
        {
            if (!IsFullFramework)
            {
                return false;
            }

            Version net462 = new Version(4, 6, 2);
            Version runningVersion = GetFrameworkVersion();
            return runningVersion != null && runningVersion >= net462;
        }

        public static bool IsNetfx470OrNewer()
        {
            if (!IsFullFramework)
            {
                return false;
            }

            Version net470 = new Version(4, 7, 0);
            Version runningVersion = GetFrameworkVersion();
            return runningVersion != null && runningVersion >= net470;
        }

        public static bool IsNetfx471OrNewer()
        {
            if (!IsFullFramework)
            {
                return false;
            }

            Version net471 = new Version(4, 7, 1);
            Version runningVersion = GetFrameworkVersion();
            return runningVersion != null && runningVersion >= net471;
        }

        private static Version GetFrameworkVersion()
        {
            string[] descriptionArray = RuntimeInformation.FrameworkDescription.Split(' ');
            if (descriptionArray.Length < 3)
                return null;

            if (!Version.TryParse(descriptionArray[2], out Version actualVersion))
                return null;

            foreach (Range currentRange in FrameworkRanges)
            {
                if (currentRange.IsInRange(actualVersion))
                    return currentRange.FrameworkVersion;
            }

            return null;
        }

        public static string GetDistroVersionString() { return ""; }

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

        private const uint TokenElevation = 20;
        private const uint STANDARD_RIGHTS_READ = 0x00020000;
        private const uint TOKEN_QUERY = 0x0008;
        private const uint TOKEN_READ = STANDARD_RIGHTS_READ | TOKEN_QUERY;

        [DllImport("advapi32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern bool GetTokenInformation(
            IntPtr TokenHandle,
            uint TokenInformationClass,
            out uint TokenInformation,
            uint TokenInformationLength,
            out uint ReturnLength);

        private const int PRODUCT_IOTUAP = 0x0000007B;
        private const int PRODUCT_IOTUAPCOMMERCIAL = 0x00000083;

        [DllImport("kernel32.dll", SetLastError = false)]
        private static extern bool GetProductInfo(
            int dwOSMajorVersion,
            int dwOSMinorVersion,
            int dwSpMajorVersion,
            int dwSpMinorVersion,
            out int pdwReturnedProductType
        );

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

        private static int GetWindowsVersion()
        {
            RTL_OSVERSIONINFOEX osvi = new RTL_OSVERSIONINFOEX();
            osvi.dwOSVersionInfoSize = (uint)Marshal.SizeOf(osvi);
            Assert.Equal(0, RtlGetVersion(out osvi));
            return (int)osvi.dwMajorVersion;
        }

        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern int GetCurrentApplicationUserModelId(ref uint applicationUserModelIdLength, byte[] applicationUserModelId);
            
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern bool CloseHandle(IntPtr handle);

        [DllImport("advapi32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccesss, out IntPtr TokenHandle);

        // The process handle does NOT need closing
        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetCurrentProcess();
 }
}