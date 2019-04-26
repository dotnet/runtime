// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using Xunit;

namespace System
{
    public static partial class PlatformDetection
    {
        public static Version OSXVersion => throw new PlatformNotSupportedException();
        public static Version OpenSslVersion => throw new PlatformNotSupportedException();
        public static bool IsDrawingSupported => IsNotWindowsNanoServer && IsNotWindowsServerCore && !IsUap;
        public static bool IsSoundPlaySupported => IsNotWindowsNanoServer;
        public static bool IsSuperUser => throw new PlatformNotSupportedException();
        public static bool IsCentos6 => false;
        public static bool IsOpenSUSE => false;
        public static bool IsUbuntu => false;
        public static bool IsDebian => false;
        public static bool IsAlpine => false;
        public static bool IsDebian8 => false;
        public static bool IsUbuntu1404 => false;
        public static bool IsUbuntu1604 => false;
        public static bool IsUbuntu1704 => false;
        public static bool IsUbuntu1710 => false;
        public static bool IsUbuntu1710OrHigher => false;
        public static bool IsUbuntu1804 => false;
        public static bool IsUbuntu1810OrHigher => false;
        public static bool IsTizen => false;
        public static bool IsNotFedoraOrRedHatFamily => true;
        public static bool IsFedora => false;
        public static bool IsWindowsNanoServer => (IsNotWindowsIoTCore && GetInstallationType().Equals("Nano Server", StringComparison.OrdinalIgnoreCase));
        public static bool IsWindowsServerCore => GetInstallationType().Equals("Server Core", StringComparison.OrdinalIgnoreCase);
        public static int WindowsVersion => (int)GetWindowsVersion();
        public static bool IsMacOsHighSierraOrHigher { get; } = false;
        public static bool IsMacOsMojaveOrHigher { get; } = false;
        public static Version ICUVersion => new Version(0, 0, 0, 0);
        public static bool IsRedHatFamily => false;
        public static bool IsNotRedHatFamily => true;
        public static bool IsRedHatFamily6 => false;
        public static bool IsRedHatFamily7 => false;
        public static bool IsNotRedHatFamily6 => true;
        public static bool IsInContainer => !String.IsNullOrEmpty(GetContainerType());

        public static bool SupportsSsl3 => GetSsl3Support();

        // >= Windows 10 Anniversary Update
        public static bool IsWindows10Version1607OrGreater => 
            GetWindowsVersion() == 10 && GetWindowsMinorVersion() == 0 && GetWindowsBuildNumber() >= 14393;
        
         // >= Windows 10 Creators Update
        public static bool IsWindows10Version1703OrGreater => 
            GetWindowsVersion() == 10 && GetWindowsMinorVersion() == 0 && GetWindowsBuildNumber() >= 15063;
        
        // >= Windows 10 Fall Creators Update
        public static bool IsWindows10Version1709OrGreater => 
            GetWindowsVersion() == 10 && GetWindowsMinorVersion() == 0 && GetWindowsBuildNumber() >= 16299;
        
        // >= Windows 10 April 2018 Update
        public static bool IsWindows10Version1803OrGreater =>
            GetWindowsVersion() == 10 && GetWindowsMinorVersion() == 0 && GetWindowsBuildNumber() >= 17134;

        // >= Windows 10 May 2019 Update (19H1)
        public static bool IsWindows10Version1903OrGreater =>
            GetWindowsVersion() == 10 && GetWindowsMinorVersion() == 0 && GetWindowsBuildNumber() >= 18362;

        // Windows OneCoreUAP SKU doesn't have httpapi.dll
        public static bool IsNotOneCoreUAP =>  
            File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "httpapi.dll"));

        public static bool IsWindowsIoTCore
        {
            get
            {
                int productType = GetWindowsProductType();
                if ((productType == PRODUCT_IOTUAPCOMMERCIAL) ||
                    (productType == PRODUCT_IOTUAP))
                {
                    return true;
                }
                return false;
            }
        }

        public static bool IsWindowsHomeEdition
        {
            get
            {
                int productType = GetWindowsProductType();
                switch (productType)
                {
                    case PRODUCT_CORE:
                    case PRODUCT_CORE_COUNTRYSPECIFIC:
                    case PRODUCT_CORE_N:
                    case PRODUCT_CORE_SINGLELANGUAGE:
                    case PRODUCT_HOME_BASIC:
                    case PRODUCT_HOME_BASIC_N:
                    case PRODUCT_HOME_PREMIUM:
                    case PRODUCT_HOME_PREMIUM_N:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public static bool IsWindows => true;
        public static bool IsWindows7 => GetWindowsVersion() == 6 && GetWindowsMinorVersion() == 1;
        public static bool IsWindows8x => GetWindowsVersion() == 6 && (GetWindowsMinorVersion() == 2 || GetWindowsMinorVersion() == 3);
        public static bool IsWindows8xOrLater => new Version((int)GetWindowsVersion(), (int)GetWindowsMinorVersion()) >= new Version(6, 2);

        public static string LibcRelease => "glibc_not_found";
        public static string LibcVersion => "glibc_not_found";

        public static string GetDistroVersionString() { return "WindowsProductType=" + GetWindowsProductType() + " WindowsInstallationType=" + GetInstallationType(); }

        private static int s_isInAppContainer = -1;

        public static bool IsInAppContainer
        {
            // This actually checks whether code is running in a modern app. 
            // Currently this is the only situation where we run in app container.
            // If we want to distinguish the two cases in future,
            // EnvironmentHelpers.IsAppContainerProcess in desktop code shows how to check for the AC token.
            get
            {
                if (s_isInAppContainer != -1)
                    return s_isInAppContainer == 1;

                if (!IsWindows || IsWindows7)
                {
                    s_isInAppContainer = 0;
                    return false;
                }

                byte[] buffer = Array.Empty<byte>();
                uint bufferSize = 0;
                try
                {
                    int result = GetCurrentApplicationUserModelId(ref bufferSize, buffer);
                    switch (result)
                    {
                        case 15703: // APPMODEL_ERROR_NO_APPLICATION
                            s_isInAppContainer = 0;
                            break;
                        case 0:     // ERROR_SUCCESS
                        case 122:   // ERROR_INSUFFICIENT_BUFFER
                                    // Success is actually insufficent buffer as we're really only looking for
                                    // not NO_APPLICATION and we're not actually giving a buffer here. The
                                    // API will always return NO_APPLICATION if we're not running under a
                                    // WinRT process, no matter what size the buffer is.
                            s_isInAppContainer = 1;
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
                        s_isInAppContainer = 0;
                    }
                    else
                    {
                        throw;
                    }
                }

                return s_isInAppContainer == 1;
            }
        }

        private static int s_isWindowsElevated = -1;

        public static bool IsWindowsAndElevated
        {
            get
            {
                if (s_isWindowsElevated != -1)
                    return s_isWindowsElevated == 1;

                if (!IsWindows || IsInAppContainer)
                {
                    s_isWindowsElevated = 0;
                    return false;
                }

                s_isWindowsElevated = AdminHelpers.IsProcessElevated() ? 1 : 0;

                return s_isWindowsElevated == 1;
            }
        }

        private static string GetInstallationType()
        {
            string key = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion";
            string value = "";

            try
            {
                value = (string)Registry.GetValue(key, "InstallationType", defaultValue: "");
            }
            catch (Exception e) when (e is SecurityException || e is InvalidCastException || e is PlatformNotSupportedException /* UAP */)
            {
            }

            return value;
        }

        private static bool GetSsl3Support()
        {
            string clientKey = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\SSL 3.0\Client";
            string serverKey = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\SSL 3.0\Server";
            bool enabled = true;

            // This may change in future but for now, missing key means protocol is enabled.
            try
            {
                if ((int)Registry.GetValue(clientKey, "Enabled", 1) == 0 || (int)Registry.GetValue(serverKey, "Enabled", 1) == 0)
                {
                    enabled = false;
                }
            }
            catch (Exception e) when (e is SecurityException || e is InvalidCastException || e is NullReferenceException)
            {
            }

            return enabled;
        }

        private static int GetWindowsProductType()
        {
            Assert.True(GetProductInfo(Environment.OSVersion.Version.Major, Environment.OSVersion.Version.Minor, 0, 0, out int productType));
            return productType;
        }

        private static string GetContainerType()
        {
            string key = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control";
            string value = "";

            try
            {
                value = (string)Registry.GetValue(key, "ContainerType", defaultValue: "");
            }
            catch
            {
            }

            return value;
        }

        private const int PRODUCT_IOTUAP = 0x0000007B;
        private const int PRODUCT_IOTUAPCOMMERCIAL = 0x00000083;
        private const int PRODUCT_CORE = 0x00000065;
        private const int PRODUCT_CORE_COUNTRYSPECIFIC = 0x00000063;
        private const int PRODUCT_CORE_N = 0x00000062;
        private const int PRODUCT_CORE_SINGLELANGUAGE = 0x00000064;
        private const int PRODUCT_HOME_BASIC = 0x00000002;
        private const int PRODUCT_HOME_BASIC_N = 0x00000005;
        private const int PRODUCT_HOME_PREMIUM = 0x00000003;
        private const int PRODUCT_HOME_PREMIUM_N = 0x0000001A;

        [DllImport("kernel32.dll", SetLastError = false)]
        private static extern bool GetProductInfo(
            int dwOSMajorVersion,
            int dwOSMinorVersion,
            int dwSpMajorVersion,
            int dwSpMinorVersion,
            out int pdwReturnedProductType
        );


        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern int GetCurrentApplicationUserModelId(ref uint applicationUserModelIdLength, byte[] applicationUserModelId);

        internal static uint GetWindowsVersion()
        {
            Assert.Equal(0, Interop.NtDll.RtlGetVersionEx(out Interop.NtDll.RTL_OSVERSIONINFOEX osvi));
            return osvi.dwMajorVersion;
        }
        internal static uint GetWindowsMinorVersion()
        {
            Assert.Equal(0, Interop.NtDll.RtlGetVersionEx(out Interop.NtDll.RTL_OSVERSIONINFOEX osvi));
            return osvi.dwMinorVersion;
        }
        internal static uint GetWindowsBuildNumber()
        {
            Assert.Equal(0, Interop.NtDll.RtlGetVersionEx(out Interop.NtDll.RTL_OSVERSIONINFOEX osvi));
            return osvi.dwBuildNumber;
        }
    }
}
