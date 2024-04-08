// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Security.Authentication;

internal static partial class Interop
{
    internal static partial class AndroidCrypto
    {
        [LibraryImport(Interop.Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_SSLGetSupportedProtocols")]
        internal static partial SslProtocols SSLGetSupportedProtocols();

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_SSLSupportsApplicationProtocolsConfiguration")]
        [return: MarshalAs(UnmanagedType.U1)]
        internal static partial bool SSLSupportsApplicationProtocolsConfiguration();
    }
}
