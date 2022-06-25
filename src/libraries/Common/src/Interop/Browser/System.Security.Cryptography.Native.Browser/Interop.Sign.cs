// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class BrowserCrypto
    {
        [LibraryImport(Libraries.CryptoNative, EntryPoint = "SystemCryptoNativeBrowser_Sign")]
        internal static unsafe partial int Sign(
            SimpleDigest hashAlgorithm,
            byte* key_buffer,
            int key_len,
            byte* input_buffer,
            int input_len,
            byte* output_buffer,
            int output_len);
    }
}
