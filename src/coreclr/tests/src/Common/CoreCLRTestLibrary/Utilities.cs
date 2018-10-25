// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;

namespace TestLibrary
{
    public static partial class Utilities
    {

        static volatile bool verbose;
        static volatile bool verboseSet = false;
        const char HIGH_SURROGATE_START = '\ud800';
        const char HIGH_SURROGATE_END = '\udbff';
        const char LOW_SURROGATE_START = '\udc00';
        const char LOW_SURROGATE_END = '\udfff';

        static string sTestDirectory = string.Empty;

        public static string TestDirectory
        {
            get
            {
                return sTestDirectory;
            }
            set
            {
                sTestDirectory = value;
            }
        }

        public static bool Verbose
        {
            get
            {
                if (!verboseSet)
                {
                    verboseSet = true;
                    verbose = false;
                }
                return (bool)verbose;
            }
            set
            {
                verbose = value;
            }
        }

        public static bool IsWindows => (Path.DirectorySeparatorChar == '\\');

        public static bool IsWindowsNanoServer => (!IsWindowsIoTCore && GetInstallationType().Equals("Nano Server", StringComparison.OrdinalIgnoreCase));

        public static bool IsWindowsIoTCore
        {
            get
            {
                if (IsWindows)
                {
                    int productType = GetWindowsProductType();
                    return productType == Kernel32.PRODUCT_IOTUAPCOMMERCIAL
                        || productType == Kernel32.PRODUCT_IOTUAP;
                }

                return false;
            }
        }

        // return whether or not the OS is a 64 bit OS
        public static bool Is64 => (IntPtr.Size == 8);

        public static string ByteArrayToString(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder();

            foreach (byte b in bytes)
            {
                sb.Append(b.ToString("x2"));
                sb.Append(", ");
            }
            if (bytes.Length > 0)
                sb.Remove(sb.Length - 2, 2);

            return sb.ToString();
        }

        public static bool CompareBytes(byte[] arr1, byte[] arr2)
        {
            if (arr1 == null) return (arr2 == null);
            if (arr2 == null) return false;

            if (arr1.Length != arr2.Length) return false;

            for (int i = 0; i < arr1.Length; i++) if (arr1[i] != arr2[i]) return false;

            return true;
        }

        // Given a string, display the unicode characters in hex format, optionally displaying each 
        // characters unicode category
        public static string FormatHexStringFromUnicodeString(string string1, bool includeUnicodeCategory)
        {
            string returnString = "";
            if (null == string1)
            {
                return null;
            }

            foreach (char x in string1)
            {
                string tempString = FormatHexStringFromUnicodeChar(x, includeUnicodeCategory);
                if (!returnString.Equals("") && !includeUnicodeCategory)
                {
                    returnString = returnString + " " + tempString;
                }
                else
                {
                    returnString += tempString;
                }
            }

            return returnString;
        }

        // Given a character, display its unicode value in hex format. ProjectN doens't support 
        // unicode category as a Property on Char.
        public static string FormatHexStringFromUnicodeChar(char char1, bool includeUnicodeCategory)
        {
            if (includeUnicodeCategory)
                throw new Exception("Win8P does not support Char.UnicodeCategory");

            return ((int)char1).ToString("X4");
        }

        public static CultureInfo CurrentCulture
        {
            get { return System.Globalization.CultureInfo.CurrentCulture; }
            set
            {
                System.Globalization.CultureInfo.DefaultThreadCurrentCulture = value;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int GetWindowsProductType()
        {
            if (!Kernel32.GetProductInfo(Environment.OSVersion.Version.Major, Environment.OSVersion.Version.Minor, 0, 0, out int productType))
            {
                return Kernel32.PRODUCT_UNDEFINED;
            }

            return productType;
        }

        private static string GetInstallationType()
        {
            if (IsWindows)
            {
                return GetInstallationTypeForWindows();
            }

            return string.Empty;
        }

        private static string GetInstallationTypeForWindows()
        {
            try
            {
                string key = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";
                string value = "InstallationType";
                return GetRegistryValueString(key, value);
            }
            catch (Exception e) when (e is SecurityException || e is InvalidCastException || e is PlatformNotSupportedException /* UAP */)
            {
                return string.Empty;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string GetRegistryValueString(string key, string value)
        {
            int dataSize = 0;
            Advapi32.RType type;
            int result = Advapi32.RegGetValueW(
                Advapi32.HKEY_LOCAL_MACHINE,
                key,
                value,
                Advapi32.RFlags.RRF_RT_REG_SZ,
                out type,
                IntPtr.Zero,
                ref dataSize);
            if (result != 0 || type != Advapi32.RType.RegSz)
            {
                throw new Exception($"Invalid {nameof(Advapi32.RegGetValueW)} result: 0x{result:x} type: {type}");
            }

            IntPtr data = Marshal.AllocCoTaskMem(dataSize + 1);
            result = Advapi32.RegGetValueW(
                Advapi32.HKEY_LOCAL_MACHINE,
                key,
                value,
                Advapi32.RFlags.RRF_RT_REG_SZ,
                out type,
                data,
                ref dataSize);
            if (result != 0 || type != Advapi32.RType.RegSz)
            {
                throw new Exception($"Invalid {nameof(Advapi32.RegGetValueW)} result: 0x{result:x} type: {type}");
            }

            string stringValue = Marshal.PtrToStringUni(data);
            Marshal.FreeCoTaskMem(data);

            return stringValue;
        }

        private sealed class Kernel32
        {
            public const int PRODUCT_UNDEFINED = 0;
            public const int PRODUCT_IOTUAP = 0x0000007B;
            public const int PRODUCT_IOTUAPCOMMERCIAL = 0x00000083;
            public const int PRODUCT_CORE = 0x00000065;
            public const int PRODUCT_CORE_COUNTRYSPECIFIC = 0x00000063;
            public const int PRODUCT_CORE_N = 0x00000062;
            public const int PRODUCT_CORE_SINGLELANGUAGE = 0x00000064;
            public const int PRODUCT_HOME_BASIC = 0x00000002;
            public const int PRODUCT_HOME_BASIC_N = 0x00000005;
            public const int PRODUCT_HOME_PREMIUM = 0x00000003;
            public const int PRODUCT_HOME_PREMIUM_N = 0x0000001A;

            /// <summary>
            /// https://docs.microsoft.com/en-us/windows/desktop/api/sysinfoapi/nf-sysinfoapi-getproductinfo
            /// </summary>
            [DllImport(nameof(Kernel32), SetLastError = false)]
            public static extern bool GetProductInfo(
                int dwOSMajorVersion,
                int dwOSMinorVersion,
                int dwSpMajorVersion,
                int dwSpMinorVersion,
                out int pdwReturnedProductType);
        }

        private sealed class Advapi32
        {
            /// <summary>
            /// http://msdn.microsoft.com/en-us/library/windows/desktop/ms724884(v=vs.85).aspx
            /// </summary>
            public enum RFlags
            {
                /// <summary>
                /// Any
                /// </summary>
                Any = 0xffff,

                /// <summary>
                /// A null-terminated string.
                /// This will be either a Unicode or an ANSI string, depending on whether you use the Unicode or ANSI function.
                /// </summary>
                RRF_RT_REG_SZ = 2,
            }

            /// <summary>
            /// http://msdn.microsoft.com/en-us/library/windows/desktop/ms724884(v=vs.85).aspx
            /// </summary>
            public enum RType
            {
                /// <summary>
                /// No defined value type
                /// </summary>
                RegNone = 0,

                /// <summary>
                /// A null-terminated string.
                /// This will be either a Unicode or an ANSI string, depending on whether you use the Unicode or ANSI function.
                /// </summary>
                RegSz = 1,
            }

            [DllImport(nameof(Advapi32), CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern int RegGetValueW(
                IntPtr hkey,
                string lpSubKey,
                string lpValue,
                RFlags dwFlags,
                out RType pdwType,
                IntPtr pvData,
                ref int pcbData);

            public static IntPtr HKEY_LOCAL_MACHINE => new IntPtr(unchecked((int)0x80000002));
        }
    }
}
