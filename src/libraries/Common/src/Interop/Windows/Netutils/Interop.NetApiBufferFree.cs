// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Netutils
    {
        [LibraryImport(Libraries.Netutils)]
        internal static partial int NetApiBufferFree(IntPtr buffer);
    }
}
