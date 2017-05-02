// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;

namespace System.Runtime.InteropServices
{
    internal static class PInvokeMarshal
    {
        public static IntPtr AllocBSTR(int length)
        {
            IntPtr bstr = Win32Native.SysAllocStringLen(null, length);
            if (bstr == IntPtr.Zero)
                throw new OutOfMemoryException();
            return bstr;
        }

        public static void FreeBSTR(IntPtr ptr)
        {
            Win32Native.SysFreeString(ptr);
        }
    }
}
