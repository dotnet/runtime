// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Data
{
    internal partial class SafeNativeMethods
    {
        internal static IntPtr LocalAlloc(IntPtr initialSize)
        {
            var handle = Marshal.AllocHGlobal(initialSize);
            ZeroMemory(handle, (int)initialSize);
            return handle;
        }

        internal static void LocalFree(IntPtr ptr)
        {
            Marshal.FreeHGlobal(ptr);
        }

        internal static void ZeroMemory(IntPtr ptr, int length)
        {
            var zeroes = new byte[length];
            Marshal.Copy(zeroes, 0, ptr, length);
        }
    }
}
