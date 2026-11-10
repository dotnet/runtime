// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;

namespace System.IO.Compression
{
    internal sealed partial class ZipCryptoStream
    {
        // Everywhere except browser and wasi, System.IO.Compression already references
        // System.Security.Cryptography because WinZip AES needs it, so the ZipCrypto header salt
        // is filled from RandomNumberGenerator. See ZipCryptoStream.Random.BrowserOrWasi.cs for
        // the interop-based implementation used there and for why the split exists.
        private static void FillHeaderRandomBytes(Span<byte> header) =>
            RandomNumberGenerator.Fill(header.Slice(0, 10));
    }
}
