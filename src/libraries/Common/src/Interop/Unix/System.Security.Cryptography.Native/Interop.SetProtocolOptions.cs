// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using System.Security.Authentication;

internal static partial class Interop
{
    internal static partial class Ssl
    {
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SetProtocolOptions")]
        internal static extern void SetProtocolOptions(IntPtr ctx, SslProtocols protocols);
    }
}
