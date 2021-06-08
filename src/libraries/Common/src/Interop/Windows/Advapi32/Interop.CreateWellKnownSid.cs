// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Interop.Libraries.Advapi32, EntryPoint = "CreateWellKnownSid", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static partial int CreateWellKnownSid(
#else
        [DllImport(Interop.Libraries.Advapi32, EntryPoint = "CreateWellKnownSid", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern int CreateWellKnownSid(
#endif
            int sidType,
            byte[]? domainSid,
            byte[] resultSid,
            ref uint resultSidLength);
    }
}
