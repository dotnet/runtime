// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

internal static partial class Interop
{
    internal static partial class AppleCrypto
    {
        internal static unsafe void KeyDerivationPBKDF(
            PAL_HashAlgorithm prfAlgorithm,
            ReadOnlySpan<byte> password,
            ReadOnlySpan<byte> salt,
            int iterations,
            Span<byte> destination)
        {
            fixed (byte* pPassword = password)
            fixed (byte* pSalt = salt)
            fixed (byte* pDestination = destination)
            {
                int ret = AppleCryptoNative_KeyDerivationPBKDF(
                    prfAlgorithm,
                    pPassword,
                    password.Length,
                    pSalt,
                    salt.Length,
                    iterations,
                    pDestination,
                    destination.Length,
                    out int ccStatus);

                if (ret == 0)
                {
                    throw Interop.AppleCrypto.CreateExceptionForCCError(
                        ccStatus,
                        Interop.AppleCrypto.CCCryptorStatus);
                }

                if (ret != 1)
                {
                    Debug.Fail($"KeyDerivationPBKDF failed with invalid input {ret}");
                    throw new CryptographicException();
                }
            }
        }

        [DllImport(Libraries.AppleCryptoNative)]
        private static extern unsafe int AppleCryptoNative_KeyDerivationPBKDF(
            PAL_HashAlgorithm prfAlgorithm,
            byte* password,
            int passwordLen,
            byte* salt,
            int saltLen,
            int iterations,
            byte* derivedKey,
            int derivedKeyLen,
            out int errorCode);
    }
}
