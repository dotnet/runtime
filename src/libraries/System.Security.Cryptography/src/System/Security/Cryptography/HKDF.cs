// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Versioning;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    /// <summary>
    /// RFC5869  HMAC-based Extract-and-Expand Key Derivation (HKDF)
    /// </summary>
    /// <remarks>
    /// In situations where the input key material is already a uniformly random bitstring, the HKDF standard allows the Extract
    /// phase to be skipped, and the master key to be used directly as the pseudorandom key.
    /// See <a href="https://tools.ietf.org/html/rfc5869">RFC5869</a> for more information.
    /// </remarks>
    public static partial class HKDF
    {
        /// <summary>
        /// Performs the HKDF-Extract function.
        /// See section 2.2 of <a href="https://tools.ietf.org/html/rfc5869#section-2.2">RFC5869</a>
        /// </summary>
        /// <param name="hashAlgorithmName">The hash algorithm used for HMAC operations.</param>
        /// <param name="ikm">The input keying material.</param>
        /// <param name="salt">
        ///   The optional salt value (a non-secret random value). If not provided it defaults to a
        ///   byte array of the same length as the output of the specified hash algorithm.
        /// </param>
        /// <returns>The pseudo random key (prk).</returns>
        public static byte[] Extract(HashAlgorithmName hashAlgorithmName, byte[] ikm, byte[]? salt = null)
        {
            ArgumentNullException.ThrowIfNull(ikm);

            int hashLength = Helpers.HashLength(hashAlgorithmName);
            byte[] prk = new byte[hashLength];

            ExtractCore(hashAlgorithmName, ikm, salt, prk);
            return prk;
        }

        /// <summary>
        /// Performs the HKDF-Extract function.
        /// See section 2.2 of <a href="https://tools.ietf.org/html/rfc5869#section-2.2">RFC5869</a>
        /// </summary>
        /// <param name="hashAlgorithmName">The hash algorithm used for HMAC operations.</param>
        /// <param name="ikm">The input keying material.</param>
        /// <param name="salt">The salt value (a non-secret random value).</param>
        /// <param name="prk">The destination buffer to receive the pseudo-random key (prk).</param>
        /// <returns>The number of bytes written to the <paramref name="prk"/> buffer.</returns>
        public static int Extract(HashAlgorithmName hashAlgorithmName, ReadOnlySpan<byte> ikm, ReadOnlySpan<byte> salt, Span<byte> prk)
        {
            int hashLength = Helpers.HashLength(hashAlgorithmName);

            if (prk.Length < hashLength)
            {
                throw new ArgumentException(SR.Format(SR.Cryptography_Prk_TooSmall, hashLength), nameof(prk));
            }

            if (prk.Length > hashLength)
            {
                prk = prk.Slice(0, hashLength);
            }

            ExtractCore(hashAlgorithmName, ikm, salt, prk);
            return hashLength;
        }

        /// <summary>
        /// Performs the HKDF-Expand function
        /// See section 2.3 of <a href="https://tools.ietf.org/html/rfc5869#section-2.3">RFC5869</a>
        /// </summary>
        /// <param name="hashAlgorithmName">The hash algorithm used for HMAC operations.</param>
        /// <param name="prk">
        ///   The pseudorandom key that is at least as long as the output byte array of the specified hash
        ///   algorithm (usually the output from the Extract step).
        /// </param>
        /// <param name="outputLength">The length of the output keying material.</param>
        /// <param name="info">The optional context and application specific information.</param>
        /// <returns>The output keying material.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="prk"/>is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="outputLength"/> is less than 1.</exception>
        public static byte[] Expand(HashAlgorithmName hashAlgorithmName, byte[] prk, int outputLength, byte[]? info = null)
        {
            ArgumentNullException.ThrowIfNull(prk);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(outputLength);

            int hashLength = Helpers.HashLength(hashAlgorithmName);

            if (prk.Length < hashLength)
            {
                throw new ArgumentException(SR.Format(SR.Cryptography_Prk_TooSmall, hashLength), nameof(prk));
            }

            // Constant comes from section 2.3 (the constraint on L in the Inputs section)
            int maxOkmLength = 255 * hashLength;
            if (outputLength <= 0 || outputLength > maxOkmLength)
                throw new ArgumentOutOfRangeException(nameof(outputLength), SR.Format(SR.Cryptography_Okm_TooLarge, maxOkmLength));

            byte[] result = new byte[outputLength];
            Expand(hashAlgorithmName, hashLength, prk, result, info);

            return result;
        }

        /// <summary>
        /// Performs the HKDF-Expand function
        /// See section 2.3 of <a href="https://tools.ietf.org/html/rfc5869#section-2.3">RFC5869</a>
        /// </summary>
        /// <param name="hashAlgorithmName">The hash algorithm used for HMAC operations.</param>
        /// <param name="prk">
        ///   The pseudorandom key that is at least as long as the output byte array of the specified hash
        ///   algorithm (usually the output from the Extract step).
        /// </param>
        /// <param name="output">The destination buffer to receive the output keying material.</param>
        /// <param name="info">The context and application specific information (can be an empty span).</param>
        /// <exception cref="ArgumentException"><paramref name="output"/> is empty, or is larger than the maximum allowed length.</exception>
        public static void Expand(HashAlgorithmName hashAlgorithmName, ReadOnlySpan<byte> prk, Span<byte> output, ReadOnlySpan<byte> info)
        {
            int hashLength = Helpers.HashLength(hashAlgorithmName);

            if (output.Length == 0)
                throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(output));

            if (prk.Length < hashLength)
                throw new ArgumentException(SR.Format(SR.Cryptography_Prk_TooSmall, hashLength), nameof(prk));

            // Constant comes from section 2.3 (the constraint on L in the Inputs section)
            int maxOkmLength = 255 * hashLength;
            if (output.Length > maxOkmLength)
                throw new ArgumentException(SR.Format(SR.Cryptography_Okm_TooLarge, maxOkmLength), nameof(output));

            Expand(hashAlgorithmName, hashLength, prk, output, info);
        }

        /// <summary>
        /// Performs the key derivation HKDF Expand and Extract functions
        /// </summary>
        /// <param name="hashAlgorithmName">The hash algorithm used for HMAC operations.</param>
        /// <param name="ikm">The input keying material.</param>
        /// <param name="outputLength">The length of the output keying material.</param>
        /// <param name="salt">The optional salt value (a non-secret random value).</param>
        /// <param name="info">The optional context and application specific information.</param>
        /// <returns>The output keying material.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="ikm"/>is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="outputLength"/> is less than 1.</exception>
        public static byte[] DeriveKey(HashAlgorithmName hashAlgorithmName, byte[] ikm, int outputLength, byte[]? salt = null, byte[]? info = null)
        {
            ArgumentNullException.ThrowIfNull(ikm);

            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(outputLength);

            int hashLength = Helpers.HashLength(hashAlgorithmName);
            Debug.Assert(hashLength <= 512 / 8, "hashLength is larger than expected, consider increasing this value or using regular allocation");

            // Constant comes from section 2.3 (the constraint on L in the Inputs section)
            int maxOkmLength = 255 * hashLength;
            if (outputLength > maxOkmLength)
                throw new ArgumentOutOfRangeException(nameof(outputLength), SR.Format(SR.Cryptography_Okm_TooLarge, maxOkmLength));

            byte[] result = new byte[outputLength];
            DeriveKeyCore(hashAlgorithmName, hashLength, ikm, result, salt, info);
            return result;
        }

        /// <summary>
        /// Performs the key derivation HKDF Expand and Extract functions
        /// </summary>
        /// <param name="hashAlgorithmName">The hash algorithm used for HMAC operations.</param>
        /// <param name="ikm">The input keying material.</param>
        /// <param name="output">The output buffer representing output keying material.</param>
        /// <param name="salt">The salt value (a non-secret random value).</param>
        /// <param name="info">The context and application specific information (can be an empty span).</param>
        /// <exception cref="ArgumentException"><paramref name="output"/> is empty, or is larger than the maximum allowed length.</exception>
        public static void DeriveKey(HashAlgorithmName hashAlgorithmName, ReadOnlySpan<byte> ikm, Span<byte> output, ReadOnlySpan<byte> salt, ReadOnlySpan<byte> info)
        {
            int hashLength = Helpers.HashLength(hashAlgorithmName);

            if (output.Length == 0)
                throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(output));

            // Constant comes from section 2.3 (the constraint on L in the Inputs section)
            int maxOkmLength = 255 * hashLength;
            if (output.Length > maxOkmLength)
                throw new ArgumentException(SR.Format(SR.Cryptography_Okm_TooLarge, maxOkmLength), nameof(output));

            Debug.Assert(hashLength <= 512 / 8, "hashLength is larger than expected, consider increasing this value or using regular allocation");
            DeriveKeyCore(hashAlgorithmName, hashLength, ikm, output, salt, info);
        }
    }
}
