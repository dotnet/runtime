// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetUnixVersion", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern int GetUnixVersion(StringBuilder version, out int capacity);

        internal static string GetUnixVersion()
        {
            // max value of _UTSNAME_LENGTH on known Unix platforms is 1024.
            const int _UTSNAME_LENGTH = 1024;
            int capacity = _UTSNAME_LENGTH * 3 + 2;
            StringBuilder version = new StringBuilder(capacity);

            if (GetUnixVersion(version, out capacity) != 0)
            {
                // Check if the function failed due to insufficient buffer.
                if (capacity > version.Capacity)
                {
                    version.Capacity = capacity;
                    GetUnixVersion(version, out capacity);
                }
            }

            return version.ToString();
        }
    }
}
