// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    internal sealed partial class ZipCryptoStream
    {
        private static unsafe void FillHeaderRandomBytes(Span<byte> header)
        {
            // System.Security.Cryptography's RandomNumberGenerator is not referenced on browser and is
            // not supported on wasi, so call the shared native CSPRNG directly. It is backed by
            // crypto.getRandomValues() on browser and by getrandom()/getentropy()/'/dev/urandom' on unix.
            Span<byte> randomBytes = header.Slice(0, 10);
            fixed (byte* pRandomBytes = randomBytes)
            {
                Interop.GetCryptographicallySecureRandomBytes(pRandomBytes, randomBytes.Length);
            }
        }
    }
}
