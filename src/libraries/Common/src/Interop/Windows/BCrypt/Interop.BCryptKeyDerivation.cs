// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class BCrypt
    {
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
#if NET
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
        [LibraryImport(Libraries.BCrypt)]
        internal static unsafe partial NTSTATUS BCryptKeyDerivation(
            SafeBCryptKeyHandle hKey,
            BCryptBufferDesc* pParameterList,
            byte* pbDerivedKey,
            int cbDerivedKey,
            out uint pcbResult,
            int dwFlags);
    }
}
