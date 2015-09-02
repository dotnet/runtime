// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

internal partial class Interop
{
    internal partial class SspiCli
    {
        [DllImport(Interop.Libraries.Sspi)]
        internal static extern int LsaConnectUntrusted(out SafeLsaHandle LsaHandle);
    }
}
