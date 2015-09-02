// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class SspiCli
    {
        [DllImport(Interop.Libraries.Sspi)]
        internal static extern int LsaDeregisterLogonProcess(IntPtr LsaHandle);
    }
}
