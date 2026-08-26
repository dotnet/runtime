// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
#if NET
using System.Runtime.Versioning;
#endif

internal static partial class Interop
{
#if NET
    [SupportedOSPlatform("windows")]
#endif
    internal static partial class Secur32
    {
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [LibraryImport(Libraries.Secur32)]
        internal static partial uint LsaFreeReturnBuffer(IntPtr buffer);
    }
}
