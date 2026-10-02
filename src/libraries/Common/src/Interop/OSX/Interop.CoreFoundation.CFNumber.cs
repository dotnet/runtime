// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

// Declared as signed long, which has sizeof(void*) on OSX.
using CFIndex = System.IntPtr;

internal static partial class Interop
{
    internal static partial class CoreFoundation
    {
        internal enum CFNumberType
        {
            kCFNumberIntType = 9,
        }

        // CFNumberType is declared as CF_ENUM(CFIndex, CFNumberType), so it has to be passed as a CFIndex.
        // Passing the int-sized managed enum leaves the upper bits of the argument register unspecified.
        [LibraryImport(Libraries.CoreFoundationLibrary, EntryPoint = "CFNumberGetValue")]
        private static unsafe partial int _CFNumberGetValue(IntPtr handle, CFIndex type, int* value);

        private static unsafe int CFNumberGetValue(IntPtr handle, CFNumberType type, int* value)
        {
            return _CFNumberGetValue(handle, (CFIndex)type, value);
        }
    }
}
