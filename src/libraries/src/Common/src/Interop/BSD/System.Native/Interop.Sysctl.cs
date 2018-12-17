// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        private extern static unsafe int Sysctl(int* name, int namelen, void* value, size_t* len);

        // This is 'raw' sysctl call, only wrapped to allocate memory if needed
        // caller always needs to free returned buffer using  Marshal.FreeHGlobal()

        public static unsafe int Sysctl(Span<int> name, ref byte* value, ref int len)
        {
            fixed (int * ptr = &MemoryMarshal.GetReference(name))
            {
                return Sysctl(ptr, name.Length, ref value, ref len);
            }
        }

        public static unsafe int Sysctl(int* name, int name_len, ref byte* value, ref int len)
        {
            IntPtr bytesLength = (IntPtr)len;
            byte * pBuffer = value;
            value = null;
            int ret=-1;

            if (value == null && len == 0)
            {
                // do one try to see how much data we need
                ret = Sysctl(name,  name_len, pBuffer, &bytesLength);
                if (ret != 0)
                {
                    throw new InvalidOperationException(SR.Format(SR.InvalidSysctl, *name, Marshal.GetLastWin32Error()));
                }
                pBuffer = (byte*)Marshal.AllocHGlobal((int)bytesLength);
            }
            ret = Sysctl(name,  name_len, pBuffer, &bytesLength);
            if (ret != 0)
            {
                if (value == null && len == 0)
                {
                    // This is case we allocated memory for caller
                    Marshal.FreeHGlobal((IntPtr)pBuffer);
                }
                throw new InvalidOperationException(SR.Format(SR.InvalidSysctl, *name, Marshal.GetLastWin32Error()));
            }

            value = pBuffer;
            len = (int)bytesLength;

            return ret;
        }
    }
}
