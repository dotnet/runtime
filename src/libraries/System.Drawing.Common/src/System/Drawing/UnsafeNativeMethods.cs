// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Drawing
{
    internal class UnsafeNativeMethods
    {
        [DllImport(ExternDll.Kernel32, SetLastError = true, ExactSpelling = true, EntryPoint = "RtlMoveMemory")]
        public static extern void CopyMemory(HandleRef destData, HandleRef srcData, int size);

        [DllImport(ExternDll.Kernel32, SetLastError = true)]
        public static extern int GetSystemDefaultLCID();

        [DllImport(ExternDll.User32, SetLastError = true, ExactSpelling = true)]
        public static extern int GetSystemMetrics(int nIndex);
    }
}
