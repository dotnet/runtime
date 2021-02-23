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
            HashAlgorithmName hashAlgorithmName,
            Span<byte> destination)
        {
            Debug.Assert(!destination.IsEmpty);
            Debug.Assert(hashAlgorithmName.Name is not null);
            IntPtr evpHashType = HashProviderDispenser.HashAlgorithmToEvp(hashAlgorithmName.Name);
            int result = Interop.Crypto.Pbkdf2(password, salt, iterations, evpHashType, destination);
            const int Success = 1;

            if (result != Success)
            {
                Debug.Assert(result == 0, $"Unexpected result {result}");
                throw Interop.Crypto.CreateOpenSslCryptographicException();
            }
        }
    }
}
