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
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetNodeName", SetLastError = true)]
        private static extern unsafe int GetNodeName(char* name, out int len);

        internal static unsafe string GetNodeName()
        {
            // max value of _UTSNAME_LENGTH on known Unix platforms is 1024.
            const int _UTSNAME_LENGTH = 1024;
            int len = _UTSNAME_LENGTH;
            char* name = stackalloc char[_UTSNAME_LENGTH];
            int err = GetNodeName(name, out len);
            if (err != 0)
            {
                // max domain name can be 255 chars. 
                Debug.Fail("getnodename failed");
                throw new InvalidOperationException(string.Format("getnodename returned {0}", err));
            }

            // Marshal.PtrToStringAnsi uses UTF8 on Unix.
            return Marshal.PtrToStringAnsi((IntPtr)name);
        }
    }
}
