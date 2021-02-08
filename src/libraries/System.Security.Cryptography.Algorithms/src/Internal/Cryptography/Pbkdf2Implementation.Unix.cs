// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    internal partial class Pbkdf2Implementation
    {
        public static unsafe void Fill(
            ReadOnlySpan<byte> password,
            ReadOnlySpan<byte> salt,
            int iterations,
            string hashAlgorithmName,
            Span<byte> destination)
        {
            IntPtr evpHashType = HashProviderDispenser.HashAlgorithmToEvp(hashAlgorithmName);
            int result = Interop.Crypto.Pkcs5Pbkdf2Hmac(password, salt, iterations, evpHashType, destination);
            const int Success = 1;

            if (result != Success)
            {
                Debug.Assert(result == 0, $"Unexpected result {result}");
                throw Interop.Crypto.CreateOpenSslCryptographicException();
            }
        }
    }
}
