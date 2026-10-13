// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class CoreFoundation
    {
        // CFNumberType is based on CFIndex, which is 64-bit on supported Apple platforms.
        internal enum CFNumberType : long
        {
            kCFNumberIntType = 9,
        }

        [LibraryImport(Libraries.CoreFoundationLibrary)]
        private static unsafe partial byte CFNumberGetValue(IntPtr handle, CFNumberType type, int* value);
    }
}
