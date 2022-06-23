// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class BrowserCrypto
    {
        // These values are also defined in the pal_crypto_webworker header file, and utilized in the dotnet-crypto-worker in the wasm runtime.
        internal enum SimpleDigest
        {
            Sha1,
            Sha256,
            Sha384,
            Sha512,
        };

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "SystemCryptoNativeBrowser_CanUseSimpleDigestHash")]
        internal static partial int CanUseSimpleDigestHash();

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "SystemCryptoNativeBrowser_SimpleDigestHash")]
        internal static unsafe partial int SimpleDigestHash(
            SimpleDigest hash,
            byte* input_buffer,
            int input_len,
            byte* output_buffer,
            int output_len);
    }
}
