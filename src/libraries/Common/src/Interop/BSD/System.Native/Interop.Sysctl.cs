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

        private static unsafe void Sysctl(ReadOnlySpan<int> name, ref byte* value, ref uint len)
        {
            fixed (int* name_ptr = &MemoryMarshal.GetReference(name))
            {
                uint name_len = (uint)name.Length;
                nuint bytesLength = len;
                int ret;
                bool autoSize = (value == null && len == 0);

                if (autoSize)
                {
                    // do one try to see how much data we need
                    ret = Sysctl(name_ptr, name_len, value, &bytesLength);
                    if (ret != 0)
                    {
                        ThrowInvalidSysctlException(name, Marshal.GetLastPInvokeError());
                    }
                    value = (byte*)NativeMemory.Alloc(bytesLength);
                }

                ret = Sysctl(name_ptr, name_len, value, &bytesLength);
                while (autoSize && ret != 0 && GetLastErrorInfo().Error == Error.ENOMEM)
                {
                    // Do not realloc here: we don't care about
                    // previous contents, and proper checking of value returned
                    // will make code more complex.
                    NativeMemory.Free(value);
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
                        bytesLength *= 2;
                    }
                    value = (byte*)NativeMemory.Alloc(bytesLength);
                    ret = Sysctl(name_ptr, name_len, value, &bytesLength);
                }
                if (ret != 0)
                {
                    nint errno = Marshal.GetLastPInvokeError();
                    if (autoSize)
                    {
                        NativeMemory.Free(value);
                    }
                    ThrowInvalidSysctlException(name, errno);
                }

                len = (uint)bytesLength;
            }
        }

        static void ThrowInvalidSysctlException(ReadOnlySpan<int> name, nint errno)
        {
            throw new InvalidOperationException(SR.Format(SR.InvalidSysctl,
                                                          string.Join(",", name.ToArray()),
                                                          errno));
        }
    }
}
