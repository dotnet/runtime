// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    internal static partial class RsaPaddingProcessor
    {
        // DigestInfo header values taken from https://tools.ietf.org/html/rfc3447#section-9.2, Note 1.
        private static ReadOnlySpan<byte> DigestInfoMD5 =>
            [
                0x30, 0x20, 0x30, 0x0C, 0x06, 0x08, 0x2A, 0x86,
                0x48, 0x86, 0xF7, 0x0D, 0x02, 0x05, 0x05, 0x00,
                0x04, 0x10,
            ];

        private static ReadOnlySpan<byte> DigestInfoSha1 =>
            [
                0x30, 0x21, 0x30, 0x09, 0x06, 0x05, 0x2B, 0x0E, 0x03,
                0x02, 0x1A, 0x05, 0x00, 0x04, 0x14,
            ];

        private static ReadOnlySpan<byte> DigestInfoSha256 =>
            [
                0x30, 0x31, 0x30, 0x0D, 0x06, 0x09, 0x60, 0x86, 0x48,
                0x01, 0x65, 0x03, 0x04, 0x02, 0x01, 0x05, 0x00, 0x04,
                0x20,
            ];

        private static ReadOnlySpan<byte> DigestInfoSha384 =>
            [
                0x30, 0x41, 0x30, 0x0D, 0x06, 0x09, 0x60, 0x86, 0x48,
                0x01, 0x65, 0x03, 0x04, 0x02, 0x02, 0x05, 0x00, 0x04,
                0x30,
            ];

        private static ReadOnlySpan<byte> DigestInfoSha512 =>
            [
                0x30, 0x51, 0x30, 0x0D, 0x06, 0x09, 0x60, 0x86, 0x48,
                0x01, 0x65, 0x03, 0x04, 0x02, 0x03, 0x05, 0x00, 0x04,
                0x40,
            ];

        private static ReadOnlySpan<byte> DigestInfoSha3_256 =>
            [
                0x30, 0x31, 0x30, 0x0D, 0x06, 0x09, 0x60, 0x86, 0x48,
                0x01, 0x65, 0x03, 0x04, 0x02, 0x08, 0x05, 0x00, 0x04,
                0x20,
            ];

        private static ReadOnlySpan<byte> DigestInfoSha3_384 =>
            [
                0x30, 0x41, 0x30, 0x0D, 0x06, 0x09, 0x60, 0x86, 0x48,
                0x01, 0x65, 0x03, 0x04, 0x02, 0x09, 0x05, 0x00, 0x04,
                0x30,
            ];

        private static ReadOnlySpan<byte> DigestInfoSha3_512 =>
            [
                0x30, 0x51, 0x30, 0x0D, 0x06, 0x09, 0x60, 0x86, 0x48,
                0x01, 0x65, 0x03, 0x04, 0x02, 0x0A, 0x05, 0x00, 0x04,
                0x40,
            ];


        /// <summary>
        /// Represents a constant value indicating that the salt length should match the hash length.
        /// </summary>
        /// <remarks>This value is typically used in cryptographic operations where the salt length is required to
        /// be the same as the hash length.</remarks>
        internal const int PssSaltLengthIsHashLength = -1;

        /// <summary>
        /// Represents the maximum allowable length, in bytes, for a PSS (Probabilistic Signature Scheme) salt.
        /// </summary>
        /// <remarks>This constant is used to define the upper limit for the salt length in PSS-based
        /// cryptographic operations. The maximum length is determined by the hash algorithm's output size.</remarks>
        internal const int PssSaltLengthMax = -2;

        /// <summary>
        /// Calculates the length of the salt for PSS signatures based on the RSA key size and hash algorithm.
        /// </summary>
        /// <param name="pssSaltLength">The salt length used for the padding.</param>
        /// <param name="rsaKeySizeInBits">The key size of the RSA key used.</param>
        /// <param name="hashAlgorithm">The hash algorithm used.</param>
        /// <returns></returns>
        internal static int CalculatePssSaltLength(int pssSaltLength, int rsaKeySizeInBits, HashAlgorithmName hashAlgorithm)
        {
            int emLen = (rsaKeySizeInBits + 7) >>> 3;
            int hLen = HashLength(hashAlgorithm);
            return pssSaltLength switch
            {
                PssSaltLengthMax => Math.Max(0, emLen - hLen - 2),
                PssSaltLengthIsHashLength => hLen,
                _ => pssSaltLength
            };
        }

        private static ReadOnlySpan<byte> GetDigestInfoForAlgorithm(
          HashAlgorithmName hashAlgorithmName,
          out int digestLengthInBytes)
        {
            switch (hashAlgorithmName.Name)
            {
                case HashAlgorithmNames.MD5:
                    digestLengthInBytes = MD5.HashSizeInBytes;
                    return DigestInfoMD5;
                case HashAlgorithmNames.SHA1:
                    digestLengthInBytes = SHA1.HashSizeInBytes;
                    return DigestInfoSha1;
                case HashAlgorithmNames.SHA256:
                    digestLengthInBytes = SHA256.HashSizeInBytes;
                    return DigestInfoSha256;
                case HashAlgorithmNames.SHA384:
                    digestLengthInBytes = SHA384.HashSizeInBytes;
                    return DigestInfoSha384;
                case HashAlgorithmNames.SHA512:
                    digestLengthInBytes = SHA512.HashSizeInBytes;
                    return DigestInfoSha512;
                case HashAlgorithmNames.SHA3_256:
                    digestLengthInBytes = SHA3_256.HashSizeInBytes;
                    return DigestInfoSha3_256;
                case HashAlgorithmNames.SHA3_384:
                    digestLengthInBytes = SHA3_384.HashSizeInBytes;
                    return DigestInfoSha3_384;
                case HashAlgorithmNames.SHA3_512:
                    digestLengthInBytes = SHA3_512.HashSizeInBytes;
                    return DigestInfoSha3_512;
                default:
                    Debug.Fail("Unknown digest algorithm");
                    throw new CryptographicException();
            }
        }

        internal static int HashLength(HashAlgorithmName hashAlgorithmName)
        {
            GetDigestInfoForAlgorithm(hashAlgorithmName, out int hLen);
            return hLen;
        }

    }
}
