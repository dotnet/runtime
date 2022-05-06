// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Versioning;

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
    [UnsupportedOSPlatform("browser")]
    public static class HKDF
    {
        /// <summary>
        /// Performs the HKDF-Extract function.
        /// See section 2.2 of <a href="https://tools.ietf.org/html/rfc5869#section-2.2">RFC5869</a>
        /// </summary>
        /// <param name="hashAlgorithmName">The hash algorithm used for HMAC operations.</param>
        /// <param name="ikm">The input keying material.</param>
        /// <param name="salt">The optional salt value (a non-secret random value). If not provided it defaults to a byte array of <see cref="HashLength"/> zeros.</param>
        /// <returns>The pseudo random key (prk).</returns>
        public static byte[] Extract(HashAlgorithmName hashAlgorithmName, byte[] ikm, byte[]? salt = null)
        {
            ArgumentNullException.ThrowIfNull(ikm);

            int hashLength = HashLength(hashAlgorithmName);
            byte[] prk = new byte[hashLength];

            Extract(hashAlgorithmName, hashLength, ikm, salt, prk);
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
            int hashLength = HashLength(hashAlgorithmName);

            if (prk.Length < hashLength)
            {
                throw new ArgumentException(SR.Format(SR.Cryptography_Prk_TooSmall, hashLength), nameof(prk));
            }

            if (prk.Length > hashLength)
            {
                prk = prk.Slice(0, hashLength);
            }

            Extract(hashAlgorithmName, hashLength, ikm, salt, prk);
            return hashLength;
        }

        private static void Extract(HashAlgorithmName hashAlgorithmName, int hashLength, ReadOnlySpan<byte> ikm, ReadOnlySpan<byte> salt, Span<byte> prk)
        {
            Debug.Assert(HashLength(hashAlgorithmName) == hashLength);
            int written = HashOneShotHelpers.MacData(hashAlgorithmName, salt, ikm, prk);
            Debug.Assert(written == prk.Length, $"Bytes written is {written} bytes which does not match output length ({prk.Length} bytes)");
        }

        /// <summary>
        /// Performs the HKDF-Expand function
        /// See section 2.3 of <a href="https://tools.ietf.org/html/rfc5869#section-2.3">RFC5869</a>
        /// </summary>
        /// <param name="hashAlgorithmName">The hash algorithm used for HMAC operations.</param>
        /// <param name="prk">The pseudorandom key of at least <see cref="HashLength"/> bytes (usually the output from Expand step).</param>
        /// <param name="outputLength">The length of the output keying material.</param>
        /// <param name="info">The optional context and application specific information.</param>
        /// <returns>The output keying material.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="prk"/>is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="outputLength"/> is less than 1.</exception>
        public static byte[] Expand(HashAlgorithmName hashAlgorithmName, byte[] prk, int outputLength, byte[]? info = null)
        {
            ArgumentNullException.ThrowIfNull(prk);

            if (outputLength <= 0)
                throw new ArgumentOutOfRangeException(nameof(outputLength), SR.ArgumentOutOfRange_NeedPosNum);

            int hashLength = HashLength(hashAlgorithmName);

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
        /// <param name="prk">The pseudorandom key of at least <see cref="HashLength"/> bytes (usually the output from Expand step).</param>
        /// <param name="output">The destination buffer to receive the output keying material.</param>
        /// <param name="info">The context and application specific information (can be an empty span).</param>
        /// <exception cref="ArgumentException"><paramref name="output"/> is empty, or is larger than the maximum allowed length.</exception>
        public static void Expand(HashAlgorithmName hashAlgorithmName, ReadOnlySpan<byte> prk, Span<byte> output, ReadOnlySpan<byte> info)
        {
            int hashLength = HashLength(hashAlgorithmName);

            if (output.Length == 0)
                throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(output));

            // Constant comes from section 2.3 (the constraint on L in the Inputs section)
            int maxOkmLength = 255 * hashLength;
            if (output.Length > maxOkmLength)
                throw new ArgumentException(SR.Format(SR.Cryptography_Okm_TooLarge, maxOkmLength), nameof(output));

            Expand(hashAlgorithmName, hashLength, prk, output, info);
        }

        private static void Expand(HashAlgorithmName hashAlgorithmName, int hashLength, ReadOnlySpan<byte> prk, Span<byte> output, ReadOnlySpan<byte> info)
        {
            Debug.Assert(HashLength(hashAlgorithmName) == hashLength);

            if (prk.Length < hashLength)
                throw new ArgumentException(SR.Format(SR.Cryptography_Prk_TooSmall, hashLength), nameof(prk));

            Span<byte> counterSpan = stackalloc byte[1];
            ref byte counter = ref counterSpan[0];
            Span<byte> t = Span<byte>.Empty;
            Span<byte> remainingOutput = output;

            const int MaxStackInfoBuffer = 64;
            Span<byte> tempInfoBuffer = stackalloc byte[MaxStackInfoBuffer];
            ReadOnlySpan<byte> infoBuffer = stackalloc byte[0];
            byte[]? rentedTempInfoBuffer = null;

            if (output.Overlaps(info))
            {
                if (info.Length > MaxStackInfoBuffer)
                {
                    rentedTempInfoBuffer = CryptoPool.Rent(info.Length);
                    tempInfoBuffer = rentedTempInfoBuffer;
                }

                tempInfoBuffer = tempInfoBuffer.Slice(0, info.Length);
                info.CopyTo(tempInfoBuffer);
                infoBuffer = tempInfoBuffer;
            }
            else
            {
                infoBuffer = info;
            }

            using (IncrementalHash hmac = IncrementalHash.CreateHMAC(hashAlgorithmName, prk))
            {
                for (int i = 1; ; i++)
                {
                    hmac.AppendData(t);
                    hmac.AppendData(infoBuffer);
                    counter = (byte)i;
                    hmac.AppendData(counterSpan);

                    if (remainingOutput.Length >= hashLength)
                    {
                        t = remainingOutput.Slice(0, hashLength);
                        remainingOutput = remainingOutput.Slice(hashLength);
                        GetHashAndReset(hmac, t);
                    }
                    else
                    {
                        if (remainingOutput.Length > 0)
                        {
                            Debug.Assert(hashLength <= 512 / 8, "hashLength is larger than expected, consider increasing this value or using regular allocation");
                            Span<byte> lastChunk = stackalloc byte[hashLength];
                            GetHashAndReset(hmac, lastChunk);
                            lastChunk.Slice(0, remainingOutput.Length).CopyTo(remainingOutput);
                        }

                        break;
                    }
                }
            }

            if (rentedTempInfoBuffer is not null)
            {
                CryptoPool.Return(rentedTempInfoBuffer, clearSize: info.Length);
            }
        }

        /// <summary>
        /// Performs the key derivation HKDF Expand and Extract functions
        /// </summary>
        /// <param name="hashAlgorithmName">The hash algorithm used for HMAC operations.</param>
        /// <param name="ikm">The input keying material.</param>
        /// <param name="outputLength">The length of the output keying material.</param>
        /// <param name="salt">The optional salt value (a non-secret random value). If not provided it defaults to a byte array of <see cref="HashLength"/> zeros.</param>
        /// <param name="info">The optional context and application specific information.</param>
        /// <returns>The output keying material.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="ikm"/>is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="outputLength"/> is less than 1.</exception>
        public static byte[] DeriveKey(HashAlgorithmName hashAlgorithmName, byte[] ikm, int outputLength, byte[]? salt = null, byte[]? info = null)
        {
            ArgumentNullException.ThrowIfNull(ikm);

            if (outputLength <= 0)
                throw new ArgumentOutOfRangeException(nameof(outputLength), SR.ArgumentOutOfRange_NeedPosNum);

            int hashLength = HashLength(hashAlgorithmName);
            Debug.Assert(hashLength <= 512 / 8, "hashLength is larger than expected, consider increasing this value or using regular allocation");

            // Constant comes from section 2.3 (the constraint on L in the Inputs section)
            int maxOkmLength = 255 * hashLength;
            if (outputLength > maxOkmLength)
                throw new ArgumentOutOfRangeException(nameof(outputLength), SR.Format(SR.Cryptography_Okm_TooLarge, maxOkmLength));

            Span<byte> prk = stackalloc byte[hashLength];

            Extract(hashAlgorithmName, hashLength, ikm, salt, prk);

            byte[] result = new byte[outputLength];
            Expand(hashAlgorithmName, hashLength, prk, result, info);

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
        /// <exception cref="ArgumentException"><paramref name="ikm"/> is empty, or is larger than the maximum allowed length.</exception>
        public static void DeriveKey(HashAlgorithmName hashAlgorithmName, ReadOnlySpan<byte> ikm, Span<byte> output, ReadOnlySpan<byte> salt, ReadOnlySpan<byte> info)
        {
            int hashLength = HashLength(hashAlgorithmName);

            if (output.Length == 0)
                throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(output));

            // Constant comes from section 2.3 (the constraint on L in the Inputs section)
            int maxOkmLength = 255 * hashLength;
            if (output.Length > maxOkmLength)
                throw new ArgumentException(SR.Format(SR.Cryptography_Okm_TooLarge, maxOkmLength), nameof(output));

            Debug.Assert(hashLength <= 512 / 8, "hashLength is larger than expected, consider increasing this value or using regular allocation");
            Span<byte> prk = stackalloc byte[hashLength];

            Extract(hashAlgorithmName, hashLength, ikm, salt, prk);
            Expand(hashAlgorithmName, hashLength, prk, output, info);
        }

        private static void GetHashAndReset(IncrementalHash hmac, Span<byte> output)
        {
            if (!hmac.TryGetHashAndReset(output, out int bytesWritten))
            {
                Debug.Assert(false, "HMAC operation failed unexpectedly");
                throw new CryptographicException(SR.Arg_CryptographyException);
            }

            Debug.Assert(bytesWritten == output.Length, $"Bytes written is {bytesWritten} bytes which does not match output length ({output.Length} bytes)");
        }

        private static int HashLength(HashAlgorithmName hashAlgorithmName)
        {
            if (hashAlgorithmName == HashAlgorithmName.SHA1)
            {
                return HMACSHA1.HashSizeInBytes;
            }
            else if (hashAlgorithmName == HashAlgorithmName.SHA256)
            {
                return HMACSHA256.HashSizeInBytes;
            }
            else if (hashAlgorithmName == HashAlgorithmName.SHA384)
            {
                return HMACSHA384.HashSizeInBytes;
            }
            else if (hashAlgorithmName == HashAlgorithmName.SHA512)
            {
                return HMACSHA512.HashSizeInBytes;
            }
            else if (hashAlgorithmName == HashAlgorithmName.MD5)
            {
                return HMACMD5.HashSizeInBytes;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(hashAlgorithmName));
            }
        }
    }
}
