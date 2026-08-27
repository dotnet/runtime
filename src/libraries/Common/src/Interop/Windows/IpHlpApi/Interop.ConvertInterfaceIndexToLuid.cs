// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
#if NET
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
    internal static partial class IpHlpApi
    {
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [LibraryImport(Libraries.IpHlpApi)]
        internal static unsafe partial uint ConvertInterfaceIndexToLuid(uint ifIndex, ref ulong interfaceLuid);
    }
}
