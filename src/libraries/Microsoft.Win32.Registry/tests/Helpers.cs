// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Win32.RegistryTests
{
    internal static partial class Helpers
    {
        [LibraryImport(Interop.Libraries.Advapi32,  EntryPoint = "RegSetValueW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        private static unsafe partial int RegSetValue(SafeRegistryHandle handle, string value, int regType, string sb, int sizeIgnored);

        internal static bool SetDefaultValue(this RegistryKey key, string value)
        {
            const int REG_SZ = 1;
            // SAFETY: To be audited
            unsafe
            {
                return RegSetValue(key.Handle, null, REG_SZ, value, 0) == 0;
            }
        }

        [LibraryImport(Interop.Libraries.Advapi32,  EntryPoint = "RegQueryValueExW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        private static unsafe partial int RegQueryValueEx(SafeRegistryHandle handle, string valueName, int[] reserved, IntPtr regType, byte[] value, ref int size);

        internal static bool IsDefaultValueSet(this RegistryKey key)
        {
            const int ERROR_FILE_NOT_FOUND = 2;
            byte[] b = new byte[4];
            int size = 4;
            // SAFETY: To be audited
            unsafe
            {
                return RegQueryValueEx(key.Handle, null, null, IntPtr.Zero, b, ref size) != ERROR_FILE_NOT_FOUND;
            }
        }

        internal static bool IsSystemKeyDefaultValueSet(this RegistryKey key)
        {
            const int ERROR_FILE_NOT_FOUND = 2;
            byte[] b = new byte[4];
            int size = 4;

            using SafeRegistryHandle handle = key.Handle;
            // SAFETY: To be audited
            unsafe
            {
                return RegQueryValueEx(handle, null, null, IntPtr.Zero, b, ref size) != ERROR_FILE_NOT_FOUND;
            }
        }

        [LibraryImport(Interop.Libraries.Kernel32, EntryPoint = "SetEnvironmentVariableW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool SetEnvironmentVariable(string lpName, string lpValue);
    }
}
