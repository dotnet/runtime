// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    internal static partial class Pbkdf2Implementation
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

            if (!Helpers.HasHMAC)
            {
                throw new CryptographicException(
                    SR.Format(SR.Cryptography_AlgorithmNotSupported, "HMAC" + hashAlgorithmName.Name));
            }

            using (Rfc2898DeriveBytes deriveBytes = new Rfc2898DeriveBytes(
                password.ToArray(),
                salt.ToArray(),
                iterations,
                hashAlgorithmName,
                clearPassword: true))
            {
                byte[] result = deriveBytes.GetBytes(destination.Length);
                result.AsSpan().CopyTo(destination);
            }
        }
    }
}
