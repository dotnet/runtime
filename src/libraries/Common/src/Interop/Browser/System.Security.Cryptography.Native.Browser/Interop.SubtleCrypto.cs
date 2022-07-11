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

        internal static readonly bool CanUseSubtleCrypto = CanUseSubtleCryptoImpl() == 1;

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "SystemCryptoNativeBrowser_CanUseSubtleCryptoImpl")]
        private static partial int CanUseSubtleCryptoImpl();

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "SystemCryptoNativeBrowser_SimpleDigestHash")]
        internal static unsafe partial int SimpleDigestHash(
            SimpleDigest hash,
            byte* input_buffer,
            int input_len,
            byte* output_buffer,
            int output_len);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "SystemCryptoNativeBrowser_Sign")]
        internal static unsafe partial int Sign(
            SimpleDigest hashAlgorithm,
            byte* key_buffer,
            int key_len,
            byte* input_buffer,
            int input_len,
            byte* output_buffer,
            int output_len);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "SystemCryptoNativeBrowser_EncryptDecrypt")]
        internal static unsafe partial int EncryptDecrypt(
            int encrypting,
            byte* key_buffer,
            int key_len,
            byte* iv_buffer,
            int iv_len,
            byte* input_buffer,
            int input_len,
            byte* output_buffer,
            int output_len);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "SystemCryptoNativeBrowser_DeriveBits")]
        internal static unsafe partial int DeriveBits(
            byte* password_buffer,
            int password_len,
            byte* salt_buffer,
            int salt_len,
            int iterations,
            SimpleDigest hashAlgorithm,
            byte* output_buffer,
            int output_len);
    }
}
