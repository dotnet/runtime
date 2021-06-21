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
            IntPtr bytesLength = (nint)len;
            int ret = -1;
            bool autoSize = (value == null && len == 0);

            if (autoSize)
            {
                // do one try to see how much data we need
                ret = Sysctl(name, name_len, value, &bytesLength);
                if (ret != 0)
                {
                    throw new InvalidOperationException(SR.Format(SR.InvalidSysctl, *name, Marshal.GetLastWin32Error()));
                }
                value = (byte*)NativeMemory.Alloc((nuint)bytesLength);
            }

            ret = Sysctl(name, name_len, value, &bytesLength);
            while (autoSize && ret != 0 && GetLastErrorInfo().Error == Error.ENOMEM)
            {
                // Do not use ReAllocHGlobal() here: we don't care about
                // previous contents, and proper checking of value returned
                // will make code more complex.
                NativeMemory.Free(value);
                if ((int)bytesLength == int.MaxValue)
                {
                    throw new OutOfMemoryException();
                }
                if ((int)bytesLength >= int.MaxValue / 2)
                {
                    bytesLength = (nint)int.MaxValue;
                }
                else
                {
                    bytesLength = (nint)((int)bytesLength * 2);
                }
                value = (byte*)NativeMemory.Alloc(bytesLength);
                ret = Sysctl(name, name_len, value, &bytesLength);
            }
            if (ret != 0)
            {
                if (autoSize)
                {
                    NativeMemory.Alloc((nuint)value);
                }
                throw new InvalidOperationException(SR.Format(SR.InvalidSysctl, *name, Marshal.GetLastWin32Error()));
            }

            len = (int)bytesLength;
        }
    }
}
