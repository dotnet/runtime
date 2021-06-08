// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class SspiCli
    {
#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Interop.Libraries.SspiCli, SetLastError = true)]
        internal static partial int LsaFreeReturnBuffer(IntPtr handle);
#else
        [DllImport(Interop.Libraries.SspiCli, SetLastError = true)]
        internal static extern int LsaFreeReturnBuffer(IntPtr handle);
#endif
    }
}
