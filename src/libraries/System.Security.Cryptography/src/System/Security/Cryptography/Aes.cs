// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    public abstract class Aes : SymmetricAlgorithm
    {
        protected Aes()
        {
            LegalBlockSizesValue = s_legalBlockSizes.CloneKeySizesArray();
            LegalKeySizesValue = s_legalKeySizes.CloneKeySizesArray();

            BlockSizeValue = 128;
            FeedbackSizeValue = 8;
            KeySizeValue = 256;
            ModeValue = CipherMode.CBC;
        }

        [UnsupportedOSPlatform("browser")]
        public static new Aes Create()
        {
            return new AesImplementation();
        }

        [Obsolete(Obsoletions.CryptoStringFactoryMessage, DiagnosticId = Obsoletions.CryptoStringFactoryDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [RequiresUnreferencedCode(CryptoConfig.CreateFromNameUnreferencedCodeMessage)]
        public static new Aes? Create(string algorithmName)
        {
            return (Aes?)CryptoConfig.CreateFromName(algorithmName);
        }

        /// <summary>
        ///   Computes the output length of the IETF RFC 5649 AES Key Wrap with Padding
        ///   Algorithm for the specified plaintext length.
        /// </summary>
        /// <param name="plaintextLengthInBytes">
        ///   The length of the plaintext to be wrapped, in bytes.
        /// </param>
        /// <returns>
        ///   The padded length of the key wrap for the specified plaintext.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <para>
        ///     <paramref name="plaintextLengthInBytes"/> is less than or equal to zero.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="plaintextLengthInBytes"/> represents a plaintext length
        ///     that, when wrapped, has a length that cannot be represented as a signed
        ///     32-bit integer.
        ///   </para>
        /// </exception>
        public static int GetKeyWrapPaddedLength(int plaintextLengthInBytes)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(plaintextLengthInBytes);

            const int MaxSupportedValue = 0x7FFF_FFF0;

            if (plaintextLengthInBytes > MaxSupportedValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(plaintextLengthInBytes),
                    SR.Cryptography_PlaintextTooLarge);
            }

            checked
            {
                int blocks = (plaintextLengthInBytes + 7) / 8;
                return (blocks + 1) * 8;
            }
        }

        /// <summary>
        ///   Wraps a key using the IETF RFC 5649 AES Key Wrap with Padding algorithm.
        /// </summary>
        /// <param name="plaintext">The data to wrap.</param>
        /// <returns>The wrapped data.</returns>
        /// <exception cref="ArgumentException"><paramref name="plaintext"/> is <see langword="null" /> or empty.</exception>
        /// <exception cref="CryptographicException">An error occurred during the cryptographic operation.</exception>
        public byte[] EncryptKeyWrapPadded(byte[] plaintext)
        {
            if (plaintext is null || plaintext.Length == 0)
                throw new ArgumentException(SR.Arg_EmptyOrNullArray, nameof(plaintext));

            return EncryptKeyWrapPadded(new ReadOnlySpan<byte>(plaintext));
        }

        /// <summary>
        ///   Wraps a key using the IETF RFC 5649 AES Key Wrap with Padding algorithm.
        /// </summary>
        /// <param name="plaintext">The data to wrap.</param>
        /// <returns>The wrapped data.</returns>
        /// <exception cref="ArgumentException"><paramref name="plaintext"/> is empty.</exception>
        /// <exception cref="CryptographicException">An error occurred during the cryptographic operation.</exception>
        public byte[] EncryptKeyWrapPadded(ReadOnlySpan<byte> plaintext)
        {
            if (plaintext.IsEmpty)
                throw new ArgumentException(SR.Arg_EmptySpan, nameof(plaintext));

            int outputLength = GetKeyWrapPaddedLength(plaintext.Length);
            byte[] output = new byte[outputLength];
            EncryptKeyWrapPaddedCore(plaintext, output);
            return output;
        }

        /// <summary>
        ///   Wraps a key using the IETF RFC 5649 AES Key Wrap with Padding algorithm,
        ///   writing the result to a specified buffer.
        /// </summary>
        /// <param name="plaintext">The data to wrap.</param>
        /// <param name="destination">The buffer to receive the wrapped data.</param>
        /// <exception cref="ArgumentException">
        ///   <para><paramref name="plaintext"/> is empty.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="destination"/> is not precisely sized.</para>
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para><paramref name="plaintext"/> and <paramref name="destination"/> overlap.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred during the cryptographic operation.</para>
        /// </exception>
        /// <seealso cref="GetKeyWrapPaddedLength"/>
        public void EncryptKeyWrapPadded(ReadOnlySpan<byte> plaintext, Span<byte> destination)
        {
            if (plaintext.IsEmpty)
                throw new ArgumentException(SR.Arg_EmptySpan, nameof(plaintext));

            int requiredLength = GetKeyWrapPaddedLength(plaintext.Length);

            if (destination.Length != requiredLength)
            {
                throw new ArgumentException(
                    SR.Format(SR.Argument_DestinationImprecise, requiredLength),
                    nameof(destination));
            }

            if (plaintext.Overlaps(destination))
            {
                throw new CryptographicException(SR.Cryptography_OverlappingBuffers);
            }

            EncryptKeyWrapPaddedCore(plaintext, destination);
        }

        /// <summary>
        ///   Unwraps a key that was wrapped using the IETF RFC 5649 AES Key Wrap with Padding algorithm.
        /// </summary>
        /// <param name="ciphertext">The data to unwrap.</param>
        /// <returns>The unwrapped key.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="ciphertext"/> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="ciphertext"/> has a <see cref="Array.Length"/> that does not correspond
        ///   to the output of the Key Wrap with Padding algorithm.
        /// </exception>
        public byte[] DecryptKeyWrapPadded(byte[] ciphertext)
        {
            ArgumentNullException.ThrowIfNull(ciphertext);

            return DecryptKeyWrapPadded(new ReadOnlySpan<byte>(ciphertext));
        }

        /// <summary>
        ///   Unwraps a key that was wrapped using the IETF RFC 5649 AES Key Wrap with Padding algorithm.
        /// </summary>
        /// <param name="ciphertext">The data to unwrap.</param>
        /// <returns>The unwrapped key.</returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="ciphertext"/> has a <see cref="ReadOnlySpan{T}.Length"/> that does not correspond
        ///   to the output of the Key Wrap with Padding algorithm.
        /// </exception>
        public byte[] DecryptKeyWrapPadded(ReadOnlySpan<byte> ciphertext)
        {
            if (ciphertext.Length < 16 || ciphertext.Length % 8 != 0)
                throw new ArgumentException(SR.Cryptography_KeyWrap_InvalidLength, nameof(ciphertext));

            using (CryptoPoolLease lease = CryptoPoolLease.Rent(ciphertext.Length - 8, skipClear: true))
            {
                int written = DecryptKeyWrapPadded(ciphertext, lease.Span);
                return lease.Span.Slice(0, written).ToArray();
            }
        }

        /// <summary>
        ///   Unwraps a key that was wrapped using the IETF RFC 5649 AES Key Wrap with Padding algorithm.
        /// </summary>
        /// <param name="ciphertext">The data to unwrap.</param>
        /// <param name="destination">The buffer to receive the unwrapped key.</param>
        /// <returns>The number of bytes in the unwrapped key.</returns>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///     <paramref name="ciphertext"/> has a <see cref="ReadOnlySpan{T}.Length"/> that does not correspond
        ///     to the output of the Key Wrap with Padding algorithm.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="destination"/> has a <see cref="Span{T}.Length"/> that is
        ///     more than 16 bytes shorter than <paramref name="ciphertext"/>, thus guaranteed
        ///     too short to hold the unwrapped key.
        ///   </para>
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para><paramref name="ciphertext"/> and <paramref name="destination"/> overlap.</para>
        ///   <para>-or-</para>
        ///   <para>The unwrap algorithm failed to unwrap the ciphertext.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred during the cryptographic operation.</para>
        /// </exception>
        public int DecryptKeyWrapPadded(ReadOnlySpan<byte> ciphertext, Span<byte> destination)
        {
            if (ciphertext.Length < 16 || ciphertext.Length % 8 != 0)
                throw new ArgumentException(SR.Cryptography_KeyWrap_InvalidLength, nameof(ciphertext));

            if (TryDecryptKeyWrapPadded(ciphertext, destination, out int bytesWritten))
            {
                return bytesWritten;
            }

            throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));
        }

        /// <summary>
        ///   Attempts to unwrap a key that was wrapped using the IETF RFC 5649
        ///   AES Key Wrap with Padding algorithm.
        /// </summary>
        /// <param name="ciphertext">The data to unwrap.</param>
        /// <param name="destination">The buffer to receive the unwrapped key.</param>
        /// <param name="bytesWritten">
        ///   When this method returns, contains the number of bytes written to <paramref name="destination"/>.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <returns>
        ///   <see langword="true" /> if <paramref name="destination"/> is long enough to receive the unwrapped key;
        ///   otherwise, <see langword="false" />.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="ciphertext"/> has a <see cref="ReadOnlySpan{T}.Length"/> that does not correspond
        ///   to the output of the Key Wrap with Padding algorithm.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para><paramref name="ciphertext"/> and <paramref name="destination"/> overlap.</para>
        ///   <para>-or-</para>
        ///   <para>The unwrap algorithm failed to unwrap the ciphertext.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred during the cryptographic operation.</para>
        /// </exception>
        public bool TryDecryptKeyWrapPadded(ReadOnlySpan<byte> ciphertext, Span<byte> destination, out int bytesWritten)
        {
            if (ciphertext.Length < 16 || ciphertext.Length % 8 != 0)
                throw new ArgumentException(SR.Cryptography_KeyWrap_InvalidLength, nameof(ciphertext));

            int maxOutput = ciphertext.Length - 8;
            int minOutput = maxOutput - 7;

            if (destination.Length < minOutput)
            {
                bytesWritten = 0;
                return false;
            }

            if (destination.Length > maxOutput)
            {
                destination = destination.Slice(0, maxOutput);
            }

            if (ciphertext.Overlaps(destination))
            {
                throw new CryptographicException(SR.Cryptography_OverlappingBuffers);
            }

            CryptoPoolLease lease = CryptoPoolLease.RentConditionally(
                maxOutput,
                destination,
                out bool rented,
                skipClearIfNotRented: true);

            try
            {
                int written = DecryptKeyWrapPaddedCore(ciphertext, lease.Span);

                if (written < minOutput || written > maxOutput)
                {
                    // The override has violated the rules of the algorithm.
                    throw new CryptographicException();
                }

                if (written > destination.Length)
                {
                    bytesWritten = 0;
                    return false;
                }

                if (rented)
                {
                    lease.Span.Slice(0, written).CopyTo(destination);
                }

                // If destination was long enough, and we didn't rent,
                // our software implementation will guarantee that
                // destination is cleared beyond the written length
                // (because that had to be empty, or zeros for padding).
                //
                // An override might only copy 0..written into destination,
                // so unconditionally clear the remainder of destination so
                // it is consistent across rented/unrented base/derived.
                destination.Slice(written).Clear();

                bytesWritten = written;
                return true;
            }
            catch
            {
                // It's only important to clear destination if it was not rented...
                // but rather than have some exceptions clear it, and some not, always clear.
                CryptographicOperations.ZeroMemory(destination);
                throw;
            }
            finally
            {
                lease.Dispose();
            }
        }

        /// <summary>
        ///   Unwraps a key that was wrapped using the IETF RFC 5649 AES Key Wrap with Padding algorithm.
        /// </summary>
        /// <param name="source">The data to unwrap.</param>
        /// <param name="destination">
        ///   The buffer to receive the unwrapped key.
        ///   </param>
        /// <returns>The number of bytes in the unwrapped key.</returns>
        /// <exception cref="CryptographicException">
        ///   <para>The unwrap algorithm failed to unwrap the ciphertext.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred during the cryptographic operation.</para>
        /// </exception>
        /// <remarks>
        ///   <para>
        ///     When called by the base class,
        ///     <paramref name="source"/> is pre-validated to be at least 16 bytes long and a multiple of 8 bytes.
        ///   </para>
        ///   <para>
        ///     When called by the base class,
        ///     <paramref name="destination"/> will always be exactly 8 bytes shorter than <paramref name="source"/>,
        ///     so any valid value will always fit.
        ///   </para>
        /// </remarks>
        protected virtual int DecryptKeyWrapPaddedCore(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            ulong iv;

            if (source.Length == 16)
            {
                Span<byte> decrypt = stackalloc byte[16];
                DecryptEcb(source, decrypt, PaddingMode.None);
                iv = BinaryPrimitives.ReadUInt64BigEndian(decrypt);
                decrypt.Slice(8).CopyTo(destination);
            }
            else
            {
                iv = Rfc3394Unwrap(source, destination);
            }

            uint len = (uint)iv;
            uint header = (uint)(iv >> 32);
            int slen = (int)len;

            // Only 0..7 padding bytes are allowed.
            // If len > maxOutput, that was "negative" padding, which is a large positive uint, so "more than 7".
            // If len == maxOutput, pad is 0, which is valid.
            // If len < maxOutput by less than 8, then pad is in the range 0..7, which is valid.
            // If len is any lower than that, then pad is more than 7, which is invalid.
            int maxOutput = source.Length - 8;
            uint pad = (uint)maxOutput - len;

            if (header != 0xA65959A6 || pad > 7 || destination.Slice(slen).IndexOfAnyExcept((byte)0) >= 0)
            {
                throw new CryptographicException(SR.Cryptography_KeyWrap_DecryptFailed);
            }

            return slen;
        }

        /// <summary>
        ///   Wraps a key using the IETF RFC 5649 AES Key Wrap with Padding algorithm,
        ///   writing the result to a specified buffer.
        /// </summary>
        /// <param name="source">The data to wrap.</param>
        /// <param name="destination">The buffer to receive the wrapped data.</param>
        /// <exception cref="CryptographicException">An error occurred during the cryptographic operation.</exception>
        /// <remarks>
        ///   <para>
        ///     When called by the base class,
        ///     <paramref name="source"/> is pre-validated to not be empty.
        ///   </para>
        ///   <para>
        ///     When called by the base class,
        ///     <paramref name="destination"/> is pre-validated to be exactly the length returned by
        ///     <see cref="GetKeyWrapPaddedLength"/> for the given input.
        ///   </para>
        /// </remarks>
        protected virtual void EncryptKeyWrapPaddedCore(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            Debug.Assert(destination.Length == GetKeyWrapPaddedLength(source.Length));

            const ulong AIV = 0xA65959A6;
            ulong iv = (AIV << 32) | (uint)source.Length;

            if (source.Length <= 8)
            {
                Span<byte> buf = stackalloc byte[16];

                BinaryPrimitives.WriteUInt64BigEndian(buf, iv);
                Span<byte> keyPart = buf.Slice(8);
                // Fill keyPart with zeros, then copy the key in to the beginning.
                keyPart.Clear();
                source.CopyTo(keyPart);

                EncryptEcb(buf, destination, PaddingMode.None);

                // Clear out the copy we made of the key.
                CryptographicOperations.ZeroMemory(keyPart);
            }
            else if (source.Length % 8 == 0)
            {
                Rfc3394Wrap(iv, source, destination);
            }
            else
            {
                int n = checked((source.Length + 7) / 8);
                int len = n * 8;

                using (CryptoPoolLease lease = CryptoPoolLease.Rent(len))
                {
                    source.CopyTo(lease.Span);
                    lease.Span.Slice(source.Length).Clear();

                    Rfc3394Wrap(iv, lease.Span, destination);
                }
            }
        }

        private void Rfc3394Wrap(ulong iv, ReadOnlySpan<byte> source, Span<byte> destination)
        {
            Debug.Assert(source.Length % 8 == 0);
            Debug.Assert(source.Length >= 16);
            Debug.Assert(destination.Length == GetKeyWrapPaddedLength(source.Length));

            Span<byte> B = stackalloc byte[16];
            Span<byte> A = B.Slice(0, 8);
            Span<byte> ALo = A.Slice(4, 4);
            uint t = 1;

            source.CopyTo(destination.Slice(8));
            BinaryPrimitives.WriteUInt64BigEndian(A, iv);

            for (uint j = 0; j < 6; j++)
            {
                Span<byte> R = destination.Slice(8);

                for (uint i = 0; i < source.Length; i += 8, t++, R = R.Slice(8))
                {
                    R.Slice(0, 8).CopyTo(B.Slice(8));
                    EncryptEcb(B, B, PaddingMode.None);

                    uint al = BinaryPrimitives.ReadUInt32BigEndian(ALo);
                    al ^= t;
                    BinaryPrimitives.WriteUInt32BigEndian(ALo, al);

                    B.Slice(8, 8).CopyTo(R);
                }
            }

            A.CopyTo(destination);
        }

        private ulong Rfc3394Unwrap(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            Span<byte> B = stackalloc byte[16];
            Span<byte> A = B.Slice(0, 8);
            Span<byte> ALo = A.Slice(4, 4);
            int inlen = source.Length - 8;
            uint t = 6U * (uint)(source.Length / 8) - 6;

            source.Slice(0, 8).CopyTo(A);
            source.Slice(8).CopyTo(destination);

            for (uint j = 0; j < 6; j++)
            {
                for (int rOffset = source.Length - 16; rOffset >= 0; t--, rOffset -= 8)
                {
                    Span<byte> R = destination.Slice(rOffset);

                    uint al = BinaryPrimitives.ReadUInt32BigEndian(ALo);
                    al ^= t;
                    BinaryPrimitives.WriteUInt32BigEndian(ALo, al);

                    R.Slice(0, 8).CopyTo(B.Slice(8));
                    DecryptEcb(B, B, PaddingMode.None);
                    B.Slice(8).CopyTo(R);
                }
            }

            return BinaryPrimitives.ReadUInt64BigEndian(A);
        }

        private static readonly KeySizes[] s_legalBlockSizes = { new KeySizes(128, 128, 0) };
        private static readonly KeySizes[] s_legalKeySizes = { new KeySizes(128, 256, 64) };
    }
}
