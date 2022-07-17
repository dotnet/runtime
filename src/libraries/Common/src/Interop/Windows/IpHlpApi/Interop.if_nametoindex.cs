// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class IpHlpApi
    {
        [LibraryImport(Interop.Libraries.IpHlpApi, SetLastError = true)]
        internal static partial uint if_nametoindex([MarshalAs(UnmanagedType.LPStr)] string name);
    }
}
