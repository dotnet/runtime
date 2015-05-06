// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class mincore
    {
        [DllImport(Interop.Libraries.Sspi, SetLastError = true)]
        internal static extern int LsaGetLogonSessionData(ref LUID LogonId, ref SafeLsaReturnBufferHandle ppLogonSessionData);
    }
}
