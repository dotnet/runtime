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
            // crypto.getRandomValues() on browser and by wasi:random (getentropy/__wasi_random_get) on wasi.
            Span<byte> randomBytes = header.Slice(0, 10);
            fixed (byte* pRandomBytes = randomBytes)
            {
                if (Interop.Sys.GetCryptographicallySecureRandomBytes(pRandomBytes, randomBytes.Length) != 0)
                {
                    throw new IOException(SR.UnableToGenerateRandomBytes);
                }
            }
        }
    }
}
