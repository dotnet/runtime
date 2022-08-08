// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Internal.Cryptography;

using SimpleDigest = Interop.BrowserCrypto.SimpleDigest;

namespace System.Security.Cryptography
{
    internal static partial class Pbkdf2Implementation
    {
        public static void Fill(
            ReadOnlySpan<byte> password,
            ReadOnlySpan<byte> salt,
            int iterations,
            HashAlgorithmName hashAlgorithmName,
            Span<byte> destination)
        {
            Debug.Assert(!destination.IsEmpty);
            Debug.Assert(hashAlgorithmName.Name is not null);

            if (Interop.BrowserCrypto.CanUseSubtleCrypto)
            {
                FillSubtleCrypto(password, salt, iterations, hashAlgorithmName, destination);
            }
            else
            {
                FillManaged(password, salt, iterations, hashAlgorithmName, destination);
            }
        }

        private static unsafe void FillSubtleCrypto(
            ReadOnlySpan<byte> password,
            ReadOnlySpan<byte> salt,
            int iterations,
            HashAlgorithmName hashAlgorithmName,
            Span<byte> destination)
        {
            (SimpleDigest hashName, _) = SHANativeHashProvider.HashAlgorithmToPal(hashAlgorithmName.Name!);

            fixed (byte* pPassword = password)
            fixed (byte* pSalt = salt)
            fixed (byte* pDestination = destination)
            {
                int result = Interop.BrowserCrypto.DeriveBits(
                    pPassword, password.Length,
                    pSalt, salt.Length,
                    iterations,
                    hashName,
                    pDestination, destination.Length);

                if (result != 0)
                {
                    throw new CryptographicException(SR.Format(SR.Unknown_SubtleCrypto_Error, result));
                }
            }
        }

        private static void FillManaged(
            ReadOnlySpan<byte> password,
            ReadOnlySpan<byte> salt,
            int iterations,
            HashAlgorithmName hashAlgorithmName,
            Span<byte> destination)
        {
            using (Rfc2898DeriveBytes deriveBytes = new Rfc2898DeriveBytes(
                password,
                salt,
                iterations,
                hashAlgorithmName))
            {
                deriveBytes.GetBytes(destination);
            }
        }
    }
}
