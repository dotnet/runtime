// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class SspiCli
    {
        [DllImport(Interop.Libraries.SspiCli)]
        internal static extern int LsaConnectUntrusted(out SafeLsaHandle LsaHandle);
    }
}
