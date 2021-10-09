// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Internal.Cryptography
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
            // Fall back to managed implementation since Android doesn't support the full Pbkdf2 APIs
            // until API level 26.
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
