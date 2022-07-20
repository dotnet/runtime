// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
#if NET7_0_OR_GREATER
using System.Runtime.InteropServices.Marshalling;
#endif

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [LibraryImport(Libraries.Kernel32, SetLastError = true)]
        public static partial int GetSystemDefaultLCID();

        [LibraryImport(Libraries.Kernel32, EntryPoint = "GlobalAlloc", SetLastError = true)]
        internal static partial IntPtr IntGlobalAlloc(int uFlags, UIntPtr dwBytes); // size should be 32/64bits compatible

        internal static IntPtr GlobalAlloc(int uFlags, uint dwBytes)
        {
            return IntGlobalAlloc(uFlags, new UIntPtr(dwBytes));
        }

        [LibraryImport(Libraries.Gdi32, SetLastError = true)]
        internal static partial IntPtr SelectObject(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hdc,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef obj);
    }
}
