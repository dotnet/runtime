// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    internal sealed partial class ZipCryptoStream
    {
        private static unsafe void FillHeaderRandomBytes(Span<byte> header)
        {
            Span<byte> randomBytes = header.Slice(0, 10);
            fixed (byte* pRandomBytes = randomBytes)
            {
                // Cryptographically secure on all platforms.
                Interop.GetCryptographicallySecureRandomBytes(pRandomBytes, randomBytes.Length);
            }
        }
    }
}
