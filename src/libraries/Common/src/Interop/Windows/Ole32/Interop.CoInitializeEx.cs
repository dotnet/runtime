// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Ole32
    {
        internal const uint COINIT_APARTMENTTHREADED = 2;
        internal const uint COINIT_MULTITHREADED = 0;

        [LibraryImport(Interop.Libraries.Ole32)]
        internal static partial int CoInitializeEx(IntPtr reserved, uint dwCoInit);
    }
}
