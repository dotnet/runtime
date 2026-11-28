// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Security.Cryptography
{
    internal static partial class HashAlgorithmNames
    {
        // These are accepted by CNG
        public const string MD5 = "MD5";
        public const string SHA1 = "SHA1";
        public const string SHA256 = "SHA256";
        public const string SHA384 = "SHA384";
        public const string SHA512 = "SHA512";

        public const string SHA3_256 = "SHA3-256";
        public const string SHA3_384 = "SHA3-384";
        public const string SHA3_512 = "SHA3-512";
        public const string SHAKE128 = "SHAKE128";
        public const string SHAKE256 = "SHAKE256";
        public const string CSHAKE128 = "CSHAKE128";
        public const string CSHAKE256 = "CSHAKE256";

        public const string KMAC128 = "KMAC128";
        public const string KMAC256 = "KMAC256";

        /// <summary>
        /// Map HashAlgorithm type to string; .NET Framework uses CryptoConfig functionality.
        /// </summary>
        public static string? ToAlgorithmName(this HashAlgorithm hashAlgorithm)
        {
            if (hashAlgorithm is SHA1)
                return HashAlgorithmNames.SHA1;
            if (hashAlgorithm is SHA256)
                return HashAlgorithmNames.SHA256;
            if (hashAlgorithm is SHA384)
                return HashAlgorithmNames.SHA384;
            if (hashAlgorithm is SHA512)
                return HashAlgorithmNames.SHA512;
            if (hashAlgorithm is MD5)
                return HashAlgorithmNames.MD5;

            // Fallback to ToString() which can be extended by derived classes
            return hashAlgorithm.ToString();
        }

        /// <summary>
        /// Uppercase known hash algorithms. BCrypt is case-sensitive and requires uppercase.
        /// </summary>
        public static string ToUpper(string hashAlgorithmName)
        {
            if (hashAlgorithmName.Equals(SHA256, StringComparison.OrdinalIgnoreCase))
            {
                return SHA256;
            }

            if (hashAlgorithmName.Equals(SHA384, StringComparison.OrdinalIgnoreCase))
            {
                return SHA384;
            }

            if (hashAlgorithmName.Equals(SHA512, StringComparison.OrdinalIgnoreCase))
            {
                return SHA512;
            }

            if (hashAlgorithmName.Equals(SHA1, StringComparison.OrdinalIgnoreCase))
            {
                return SHA1;
            }

            if (hashAlgorithmName.Equals(MD5, StringComparison.OrdinalIgnoreCase))
            {
                return MD5;
            }

            return hashAlgorithmName;
        }
    }
}
