// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetUnixVersion", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern int GetUnixVersion(byte[] version, ref int capacity);

        internal static string GetUnixVersion()
        {
            // max value of _UTSNAME_LENGTH on known Unix platforms is 1024.
            const int _UTSNAME_LENGTH = 1024;
            int capacity = _UTSNAME_LENGTH + 1; // +1 for null terminator
            var version = new byte[capacity];

            bool success = GetUnixVersion(version, ref capacity) == 0;
            if (!success)
            {
                // Check if the function failed due to insufficient buffer.
                if (capacity > version.Length)
                {
                    version = new byte[capacity];
                    success = GetUnixVersion(version, ref capacity) == 0;
                }
            }

            if (!success)
            {
                return string.Empty;
            }

            Debug.Assert(Array.IndexOf<byte>(version, 0) != -1);
            unsafe
            {
                fixed (byte* ptr = version)
                {
                    return new string((sbyte*)ptr);
                }
            }
        }
    }
}
