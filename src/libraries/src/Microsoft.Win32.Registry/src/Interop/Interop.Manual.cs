// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal const int KEY_QUERY_VALUE = 0x0001;
    internal const int KEY_SET_VALUE = 0x0002;
    internal const int KEY_CREATE_SUB_KEY = 0x0004;
    internal const int KEY_ENUMERATE_SUB_KEYS = 0x0008;
    internal const int KEY_NOTIFY = 0x0010;
    internal const int KEY_CREATE_LINK = 0x0020;
    internal const int KEY_READ = ((STANDARD_RIGHTS_READ |
                                                       KEY_QUERY_VALUE |
                                                       KEY_ENUMERATE_SUB_KEYS |
                                                       KEY_NOTIFY)
                                                      &
                                                      (~SYNCHRONIZE));

    internal const int KEY_WRITE = ((STANDARD_RIGHTS_WRITE |
                                                       KEY_SET_VALUE |
                                                       KEY_CREATE_SUB_KEY)
                                                      &
                                                      (~SYNCHRONIZE));
    internal const int KEY_WOW64_64KEY = 0x0100;     
    internal const int KEY_WOW64_32KEY = 0x0200;     
    internal const int REG_OPTION_NON_VOLATILE = 0x0000;     // (default) keys are persisted beyond reboot/unload
    internal const int REG_OPTION_VOLATILE = 0x0001;     // All keys created by the function are volatile
    internal const int REG_OPTION_CREATE_LINK = 0x0002;     // They key is a symbolic link
    internal const int REG_OPTION_BACKUP_RESTORE = 0x0004;  // Use SE_BACKUP_NAME process special privileges
    internal const int REG_NONE = 0;     // No value type
    internal const int REG_SZ = 1;     // Unicode nul terminated string
    internal const int REG_EXPAND_SZ = 2;     // Unicode nul terminated string
    // (with environment variable references)
    internal const int REG_BINARY = 3;     // Free form binary
    internal const int REG_DWORD = 4;     // 32-bit number
    internal const int REG_DWORD_LITTLE_ENDIAN = 4;     // 32-bit number (same as REG_DWORD)
    internal const int REG_DWORD_BIG_ENDIAN = 5;     // 32-bit number
    internal const int REG_LINK = 6;     // Symbolic Link (unicode)
    internal const int REG_MULTI_SZ = 7;     // Multiple Unicode strings
    internal const int REG_QWORD = 11;    // 64-bit number

    // Win32 ACL-related constants:
    internal const int READ_CONTROL = 0x00020000;
    internal const int SYNCHRONIZE = 0x00100000;

    internal const int STANDARD_RIGHTS_READ = READ_CONTROL;
    internal const int STANDARD_RIGHTS_WRITE = READ_CONTROL;

    internal const String LOCALIZATION_L1_APISET = "api-ms-win-core-localization-l1-2-0.dll";
    internal const String REGISTRY_L1_APISET = "api-ms-win-core-registry-l1-1-0.dll";
    internal const String REGISTRY_L2_APISET = "api-ms-win-core-registry-l2-1-0.dll";
    internal const String PROCESSENVIRONMENT_L1_APISET = "api-ms-win-core-processenvironment-l1-1-0.dll";

    // From WinBase.h
    internal const int SEM_FAILCRITICALERRORS = 1;

    private const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
    private const int FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;
    private const int FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000;

    // Error codes from WinError.h
    internal const int ERROR_SUCCESS = 0x0;
    internal const int ERROR_FILE_NOT_FOUND = 0x2;
    internal const int ERROR_ACCESS_DENIED = 0x5;
    internal const int ERROR_INVALID_HANDLE = 0x6;
    internal const int ERROR_FILENAME_EXCED_RANGE = 0xCE;  // filename too long.
    internal const int ERROR_MORE_DATA = 0xEA;
    internal const int ERROR_DLL_INIT_FAILED = 0x45A;
    internal const int ERROR_BAD_IMPERSONATION_LEVEL = 0x542;

    // Error codes from ntstatus.h
    internal const uint STATUS_ACCESS_DENIED = 0xC0000022;

    internal static partial class mincore
    {
        // Gets an error message for a Win32 error code.
        internal static String GetMessage(int errorCode)
        {
            char[] buffer = new char[512];
            uint result = Interop.mincore.FormatMessage(FORMAT_MESSAGE_IGNORE_INSERTS |
                FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_ARGUMENT_ARRAY,
                IntPtr.Zero, (uint)errorCode, 0, buffer, (uint)buffer.Length, IntPtr.Zero);
            if (result != 0)
            {
                // result is the # of characters copied to the StringBuilder.
                return new string(buffer, 0, (int)result);
            }
            else
            {
                return SR.Format(SR.UnknownError_Num, errorCode);
            }
        }

        [DllImport(REGISTRY_L2_APISET, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegConnectRegistryW")]
        internal static extern int RegConnectRegistry(String machineName,
                    SafeRegistryHandle key, out SafeRegistryHandle result);

        // Note: RegCreateKeyEx won't set the last error on failure - it returns
        // an error code if it fails.
        [DllImport(REGISTRY_L1_APISET, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegCreateKeyExW")]
        internal static extern int RegCreateKeyEx(SafeRegistryHandle hKey, String lpSubKey,
                    int Reserved, String lpClass, int dwOptions,
                    int samDesired, ref SECURITY_ATTRIBUTES secAttrs,
                    out SafeRegistryHandle hkResult, out int lpdwDisposition);

        [DllImport(REGISTRY_L1_APISET, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegDeleteKeyExW")]
        internal static extern int RegDeleteKeyEx(SafeRegistryHandle hKey, String lpSubKey,
                    int samDesired, int Reserved);

        [DllImport(REGISTRY_L1_APISET, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegDeleteValueW")]
        internal static extern int RegDeleteValue(SafeRegistryHandle hKey, String lpValueName);

        [DllImport(REGISTRY_L1_APISET, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegEnumKeyExW")]
        internal unsafe static extern int RegEnumKeyEx(SafeRegistryHandle hKey, int dwIndex,
                    char* lpName, ref int lpcbName, int[] lpReserved,
                    [Out]StringBuilder lpClass, int[] lpcbClass,
                    long[] lpftLastWriteTime);

        [DllImport(REGISTRY_L1_APISET, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegEnumValueW")]
        internal unsafe static extern int RegEnumValue(SafeRegistryHandle hKey, int dwIndex,
                    char* lpValueName, ref int lpcbValueName,
                    IntPtr lpReserved_MustBeZero, int[] lpType, byte[] lpData,
                    int[] lpcbData);


        [DllImport(REGISTRY_L1_APISET)]
        internal static extern int RegFlushKey(SafeRegistryHandle hKey);

        [DllImport(REGISTRY_L1_APISET, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegOpenKeyExW")]
        internal static extern int RegOpenKeyEx(SafeRegistryHandle hKey, String lpSubKey,
                    int ulOptions, int samDesired, out SafeRegistryHandle hkResult);

        [DllImport(REGISTRY_L1_APISET, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegOpenKeyExW")]
        internal static extern int RegOpenKeyEx(IntPtr hKey, String lpSubKey,
                    int ulOptions, int samDesired, out SafeRegistryHandle hkResult);

        [DllImport(REGISTRY_L1_APISET, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegQueryInfoKeyW")]
        internal static extern int RegQueryInfoKey(SafeRegistryHandle hKey, [Out]StringBuilder lpClass,
                    int[] lpcbClass, IntPtr lpReserved_MustBeZero, ref int lpcSubKeys,
                    int[] lpcbMaxSubKeyLen, int[] lpcbMaxClassLen,
                    ref int lpcValues, int[] lpcbMaxValueNameLen,
                    int[] lpcbMaxValueLen, int[] lpcbSecurityDescriptor,
                    int[] lpftLastWriteTime);

        [DllImport(REGISTRY_L1_APISET, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegQueryValueExW")]
        internal static extern int RegQueryValueEx(SafeRegistryHandle hKey, String lpValueName,
                    int[] lpReserved, ref int lpType, [Out] byte[] lpData,
                    ref int lpcbData);

        [DllImport(REGISTRY_L1_APISET, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegQueryValueExW")]
        internal static extern int RegQueryValueEx(SafeRegistryHandle hKey, String lpValueName,
                    int[] lpReserved, ref int lpType, ref int lpData,
                    ref int lpcbData);

        [DllImport(REGISTRY_L1_APISET, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegQueryValueExW")]
        internal static extern int RegQueryValueEx(SafeRegistryHandle hKey, String lpValueName,
                    int[] lpReserved, ref int lpType, ref long lpData,
                    ref int lpcbData);

        [DllImport(REGISTRY_L1_APISET, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegQueryValueExW")]
        internal static extern int RegQueryValueEx(SafeRegistryHandle hKey, String lpValueName,
                     int[] lpReserved, ref int lpType, [Out] char[] lpData,
                     ref int lpcbData);

        [DllImport(REGISTRY_L1_APISET, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegQueryValueExW")]
        internal static extern int RegQueryValueEx(SafeRegistryHandle hKey, String lpValueName,
                    int[] lpReserved, ref int lpType, [Out]StringBuilder lpData,
                    ref int lpcbData);

        [DllImport(REGISTRY_L1_APISET, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegSetValueExW")]
        internal static extern int RegSetValueEx(SafeRegistryHandle hKey, String lpValueName,
                    int Reserved, RegistryValueKind dwType, byte[] lpData, int cbData);

        [DllImport(REGISTRY_L1_APISET, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegSetValueExW")]
        internal static extern int RegSetValueEx(SafeRegistryHandle hKey, String lpValueName,
                    int Reserved, RegistryValueKind dwType, char[] lpData, int cbData);

        [DllImport(REGISTRY_L1_APISET, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegSetValueExW")]
        internal static extern int RegSetValueEx(SafeRegistryHandle hKey, String lpValueName,
                    int Reserved, RegistryValueKind dwType, ref int lpData, int cbData);

        [DllImport(REGISTRY_L1_APISET, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegSetValueExW")]
        internal static extern int RegSetValueEx(SafeRegistryHandle hKey, String lpValueName,
                    int Reserved, RegistryValueKind dwType, ref long lpData, int cbData);

        [DllImport(REGISTRY_L1_APISET, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegSetValueExW")]
        internal static extern int RegSetValueEx(SafeRegistryHandle hKey, String lpValueName,
                    int Reserved, RegistryValueKind dwType, String lpData, int cbData);

        [DllImport(PROCESSENVIRONMENT_L1_APISET, CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false, EntryPoint = "ExpandEnvironmentStringsW")]
        internal static extern int ExpandEnvironmentStrings(String lpSrc, [Out]StringBuilder lpDst, int nSize);

        [DllImport(REGISTRY_L1_APISET)]
        internal extern static int RegCloseKey(IntPtr hKey);


        [DllImport(LOCALIZATION_L1_APISET, EntryPoint = "FormatMessageW", SetLastError = true, CharSet = CharSet.Unicode)]
        internal extern static uint FormatMessage(
                    uint dwFlags,
                    IntPtr lpSource,
                    uint dwMessageId,
                    uint dwLanguageId,
                    char[] lpBuffer,
                    uint nSize,
                    IntPtr Arguments);
    }

    internal struct SECURITY_ATTRIBUTES
    {
        public uint nLength;
        public IntPtr lpSecurityDescriptor;
        public bool bInheritHandle;
    }    
}