// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class CoreFoundation
    {
        internal enum CFNumberType
        {
            kCFNumberIntType = 9,
        }

        [DllImport(Libraries.CoreFoundationLibrary)]
        private static extern int CFNumberGetValue(IntPtr handle, CFNumberType type, out int value);
    }
}
