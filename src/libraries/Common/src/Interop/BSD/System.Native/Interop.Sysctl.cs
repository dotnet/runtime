// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

// This implements shim for sysctl calls.
// They are available on BSD systems - FreeBSD, OSX and others.
// Linux has sysctl() but it is deprecated as well as it is missing sysctlbyname()

internal static partial class Interop
{
    internal static partial class Sys
    {
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_Sysctl", SetLastError = true)]
        private static unsafe partial int Sysctl(int* name, uint namelen, void* value, nuint* len);

        // This is 'raw' sysctl call, only wrapped to allocate memory if needed
        // caller always needs to free returned buffer using  Marshal.FreeHGlobal()

        internal static unsafe void Sysctl(ReadOnlySpan<int> name, ref byte* value, ref uint len)
        {
            fixed (int* ptr = &MemoryMarshal.GetReference(name))
            {
                Sysctl(ptr, (uint)name.Length, ref value, ref len);
            }
        }

        private static unsafe void Sysctl(int* name, uint name_len, ref byte* value, ref uint len)
        {
            nuint bytesLength = len;
            int ret = -1;
            bool autoSize = (value == null && len == 0);

            if (autoSize)
            {
                // do one try to see how much data we need
                ret = Sysctl(name, name_len, value, &bytesLength);
                if (ret != 0)
                {
                    ThrowInvalidSysctlException(name, name_len);
                }
                value = (byte*)Marshal.AllocHGlobal((int)bytesLength);
            }

            ret = Sysctl(name, name_len, value, &bytesLength);
            while (autoSize && ret != 0 && GetLastErrorInfo().Error == Error.ENOMEM)
            {
                // Do not use ReAllocHGlobal() here: we don't care about
                // previous contents, and proper checking of value returned
                // will make code more complex.
                Marshal.FreeHGlobal((IntPtr)value);
                if ((int)bytesLength == int.MaxValue)
                {
                    throw new OutOfMemoryException();
                }
                if ((int)bytesLength >= int.MaxValue / 2)
                {
                    bytesLength = int.MaxValue;
                }
                else
                {
                    bytesLength = bytesLength * 2;
                }
                value = (byte*)Marshal.AllocHGlobal((int)bytesLength);
                ret = Sysctl(name, name_len, value, &bytesLength);
            }
            if (ret != 0)
            {
                if (autoSize)
                {
                    Marshal.FreeHGlobal((IntPtr)value);
                }
                ThrowInvalidSysctlException(name, name_len);
            }

            len = (uint)bytesLength;
        }

        private static unsafe InvalidOperationException ThrowInvalidSysctlException(int* name, uint name_len)
        {
            int mib0 = name[0];
            int mib1 = name_len > 1 ? name[1] : -1;
            int mib2 = name_len > 2 ? name[2] : -1;
            int mib3 = name_len > 3 ? name[3] : -1;
            throw new InvalidOperationException(SR.Format(SR.InvalidSysctl,
                                                           mib0,
                                                           mib1,
                                                           mib2,
                                                           mib3,
                                                           Marshal.GetLastPInvokeError()));
        }
    }
}
