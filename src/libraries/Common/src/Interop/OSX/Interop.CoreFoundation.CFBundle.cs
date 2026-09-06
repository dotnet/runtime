// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class CoreFoundation
    {
        internal const uint kCFStringEncodingUTF8 = 0x08000100;

        [LibraryImport(Libraries.CoreFoundationLibrary)]
        internal static partial IntPtr CFBundleGetIdentifier(IntPtr bundle);

        [LibraryImport(Libraries.CoreFoundationLibrary)]
        internal static partial IntPtr CFBundleGetMainBundle();

        [LibraryImport(Libraries.CoreFoundationLibrary)]
        internal static unsafe partial byte CFStringGetCString(
            IntPtr value,
            byte* buffer,
            IntPtr bufferSize,
            uint encoding);

        [LibraryImport(Libraries.CoreFoundationLibrary)]
        internal static partial IntPtr CFStringGetLength(IntPtr value);

        [LibraryImport(Libraries.CoreFoundationLibrary)]
        internal static partial IntPtr CFStringGetMaximumSizeForEncoding(IntPtr length, uint encoding);
    }
}
