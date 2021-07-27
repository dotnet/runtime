// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Ole32
    {
        [DllImport(Libraries.Ole32, PreserveSig = false)]
        internal static extern IStream CreateStreamOnHGlobal(IntPtr hGlobal, bool fDeleteOnRelease);
    }
}
