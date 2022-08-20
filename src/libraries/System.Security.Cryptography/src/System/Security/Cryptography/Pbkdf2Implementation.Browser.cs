// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Internal.Cryptography;

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

            FillManaged(password, salt, iterations, hashAlgorithmName, destination);
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
