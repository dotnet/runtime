// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

using size_t = System.IntPtr;

// This implements shim for sysctl calls.
// They are available on BSD systems - FreeBSD, OSX and others.
// Linux has sysctl() but it is deprecated as well as it is missing sysctlbyname()

internal static partial class Interop
{
    internal static partial class Sys
    {

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_Sysctl", SetLastError = true)]
        private static extern unsafe int Sysctl(int* name, int namelen, void* value, size_t* len);

        // This is 'raw' sysctl call, only wrapped to allocate memory if needed
        // caller always needs to free returned buffer using  Marshal.FreeHGlobal()

        internal static unsafe void Sysctl(Span<int> name, ref byte* value, ref int len)
        {
            fixed (int* ptr = &MemoryMarshal.GetReference(name))
            {
                Sysctl(ptr, name.Length, ref value, ref len);
            }
        }

        private static unsafe void Sysctl(int* name, int name_len, ref byte* value, ref int len)
        {
            IntPtr bytesLength = (IntPtr)len;
            int ret = -1;

            if (value == null && len == 0)
            {
                // do one try to see how much data we need
                ret = Sysctl(name, name_len, value, &bytesLength);
                if (ret != 0)
                {
                    throw new InvalidOperationException(SR.Format(SR.InvalidSysctl, *name, Marshal.GetLastWin32Error()));
                }
                value = (byte*)Marshal.AllocHGlobal((int)bytesLength);
            }

            ret = Sysctl(name, name_len, value, &bytesLength);
            if (ret != 0)
            {
                throw new InvalidOperationException(SR.Format(SR.InvalidSysctl, *name, Marshal.GetLastWin32Error()));
            }

            len = (int)bytesLength;
        }
    }
}
