// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography
{
    /// <summary>
    ///   Represents an HPKE sender context for encrypting multiple messages and exporting secrets.
    /// </summary>
    [Experimental(Experimentals.HpkeExperimentalDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public abstract class HpkeSender : IDisposable
    {
        private bool _disposed;

        /// <summary>
        ///   Gets the cipher suite associated with this sender.
        /// </summary>
        /// <value>
        ///   The cipher suite associated with this sender.
        /// </value>
        public HpkeSuite Suite { get; }

        /// <summary>
        ///   Initializes a new instance of the <see cref="HpkeSender" /> class with the specified cipher suite.
        /// </summary>
        /// <param name="suite">
        ///   The cipher suite associated with this sender.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="suite" /> is <see langword="null" />.
        /// </exception>
        protected HpkeSender(HpkeSuite suite)
        {
            ArgumentNullException.ThrowIfNull(suite);
            Suite = suite;
        }

        /// <summary>
        ///   Encrypts and authenticates a message using this sender context.
        /// </summary>
        /// <param name="plaintext">
        ///   The message to encrypt.
        /// </param>
        /// <param name="associatedData">
        ///   The additional data to authenticate without encrypting.
        /// </param>
        /// <returns>
        ///   A new byte array containing the ciphertext followed by its authentication tag.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The ciphertext length would exceed <see cref="int.MaxValue" />.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   The sender's message limit has been reached, or an error occurred during encryption.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The object has already been disposed.
        /// </exception>
        /// <remarks>
        ///   Messages must be decrypted by the corresponding recipient context in the same order
        ///   in which they were encrypted.
        /// </remarks>
        public byte[] Seal(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> associatedData = default)
        {
            ThrowIfDisposed();
            byte[] ciphertext = new byte[Suite.GetCiphertextLength(plaintext.Length)];
            SealCore(plaintext, ciphertext, associatedData);
            return ciphertext;
        }

        /// <summary>
        ///   Encrypts and authenticates a message using this sender context.
        /// </summary>
        /// <param name="plaintext">
        ///   The message to encrypt.
        /// </param>
        /// <param name="associatedData">
        ///   The additional data to authenticate without encrypting,
        ///   or <see langword="null" /> to use no additional authenticated data.
        /// </param>
        /// <returns>
        ///   A new byte array containing the ciphertext followed by its authentication tag.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="plaintext" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The ciphertext length would exceed <see cref="int.MaxValue" />.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   The sender's message limit has been reached, or an error occurred during encryption.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The object has already been disposed.
        /// </exception>
        public byte[] Seal(byte[] plaintext, byte[]? associatedData = null)
        {
            ArgumentNullException.ThrowIfNull(plaintext);
            return Seal(new ReadOnlySpan<byte>(plaintext), new ReadOnlySpan<byte>(associatedData));
        }

        /// <summary>
        ///   Encrypts and authenticates a message into the provided buffer using this sender context.
        /// </summary>
        /// <param name="plaintext">
        ///   The message to encrypt.
        /// </param>
        /// <param name="ciphertext">
        ///   The buffer to receive the ciphertext followed by its authentication tag.
        /// </param>
        /// <param name="associatedData">
        ///   The additional data to authenticate without encrypting.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="ciphertext" /> is not exactly the length returned by
        ///   <see cref="HpkeSuite.GetCiphertextLength" /> for <paramref name="plaintext" />.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The ciphertext length would exceed <see cref="int.MaxValue" />.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     One or more provided buffers overlap.
        ///   </para>
        ///   <para> -or- </para>
        ///   <para>
        ///     The sender's message limit has been reached, or an error occurred during encryption.
        ///   </para>
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The object has already been disposed.
        /// </exception>
        public void Seal(
            ReadOnlySpan<byte> plaintext,
            Span<byte> ciphertext,
            ReadOnlySpan<byte> associatedData = default)
        {
            ThrowIfDisposed();
            int ciphertextLength = Suite.GetCiphertextLength(plaintext.Length);

            if (ciphertext.Length != ciphertextLength)
            {
                throw new ArgumentException(
                    SR.Format(SR.Argument_DestinationImprecise, ciphertextLength),
                    nameof(ciphertext));
            }

            if (plaintext.Overlaps(ciphertext) || associatedData.Overlaps(ciphertext))
            {
                throw new CryptographicException(SR.Cryptography_OverlappingBuffers);
            }

            SealCore(plaintext, ciphertext, associatedData);
        }

        /// <summary>
        ///   When overridden in a derived class, encrypts and authenticates a message using this sender context.
        /// </summary>
        /// <param name="plaintext">
        ///   The message to encrypt.
        /// </param>
        /// <param name="ciphertext">
        ///   The buffer to receive the ciphertext followed by its authentication tag.
        /// </param>
        /// <param name="associatedData">
        ///   The additional data to authenticate without encrypting.
        /// </param>
        /// <exception cref="CryptographicException">
        ///   The sender's message limit has been reached, or an error occurred during encryption.
        /// </exception>
        /// <remarks>
        ///   The calling method has verified that this instance is not disposed and the ciphertext buffer
        ///   has the exact required length. Implementations must maintain the sender's message sequence,
        ///   reject encryption when the message limit is reached, and fill the entire ciphertext buffer on success.
        /// </remarks>
        protected abstract void SealCore(
            ReadOnlySpan<byte> plaintext,
            Span<byte> ciphertext,
            ReadOnlySpan<byte> associatedData);

        /// <summary>
        ///   Derives an exported secret from this sender context.
        /// </summary>
        /// <param name="exporterContext">
        ///   The application context used to derive the exported secret.
        /// </param>
        /// <param name="length">
        ///   The length, in bytes, of the exported secret.
        /// </param>
        /// <returns>
        ///   A new byte array containing the exported secret.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="length" /> is negative or exceeds the maximum export length supported by the cipher suite's KDF.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while deriving the exported secret.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The object has already been disposed.
        /// </exception>
        /// <remarks>
        ///   The maximum export length is 255 times the hash output length for HKDF, or 65,535 bytes for SHAKE.
        ///   Exporting a secret does not advance the sender's message sequence.
        ///   The caller is responsible for protecting the returned secret and clearing it when no longer needed.
        /// </remarks>
        public byte[] Export(ReadOnlySpan<byte> exporterContext, int length)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(length);
            ThrowIfDisposed();
            int maximumLength = Suite.KdfMetadata.MaximumExportLength;

            if (length > maximumLength)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(length),
                    SR.Format(SR.Argument_HpkeExportLengthTooLarge, maximumLength));
            }

            byte[] secret = new byte[length];

            try
            {
                ExportCore(exporterContext, secret);
                return secret;
            }
            catch
            {
                CryptographicOperations.ZeroMemory(secret);
                throw;
            }
        }

        /// <summary>
        ///   Derives an exported secret from this sender context.
        /// </summary>
        /// <param name="exporterContext">
        ///   The application context used to derive the exported secret.
        /// </param>
        /// <param name="length">
        ///   The length, in bytes, of the exported secret.
        /// </param>
        /// <returns>
        ///   A new byte array containing the exported secret.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="exporterContext" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="length" /> is negative or exceeds the maximum export length supported by the cipher suite's KDF.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while deriving the exported secret.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The object has already been disposed.
        /// </exception>
        /// <remarks>
        ///   The maximum export length is 255 times the hash output length for HKDF, or 65,535 bytes for SHAKE.
        ///   The caller is responsible for protecting the returned secret and clearing it when no longer needed.
        /// </remarks>
        public byte[] Export(byte[] exporterContext, int length)
        {
            ArgumentNullException.ThrowIfNull(exporterContext);
            return Export(new ReadOnlySpan<byte>(exporterContext), length);
        }

        /// <summary>
        ///   Derives an exported secret from this sender context into the provided buffer.
        /// </summary>
        /// <param name="exporterContext">
        ///   The application context used to derive the exported secret.
        /// </param>
        /// <param name="destination">
        ///   The buffer to receive the exported secret.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   The length of <paramref name="destination" /> exceeds the maximum export length supported by the cipher suite's KDF.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while deriving the exported secret.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The object has already been disposed.
        /// </exception>
        /// <remarks>
        ///   The maximum export length is 255 times the hash output length for HKDF, or 65,535 bytes for SHAKE.
        ///   Exporting a secret does not advance the sender's message sequence.
        ///   The caller is responsible for protecting the secret and clearing the buffer when no longer needed.
        /// </remarks>
        public void Export(ReadOnlySpan<byte> exporterContext, Span<byte> destination)
        {
            ThrowIfDisposed();
            int maximumLength = Suite.KdfMetadata.MaximumExportLength;

            if (destination.Length > maximumLength)
            {
                throw new ArgumentException(
                    SR.Format(SR.Argument_HpkeExportLengthTooLarge, maximumLength),
                    nameof(destination));
            }

            ExportCore(exporterContext, destination);
        }

        /// <summary>
        ///   When overridden in a derived class, derives an exported secret from this sender context.
        /// </summary>
        /// <param name="exporterContext">
        ///   The application context used to derive the exported secret.
        /// </param>
        /// <param name="destination">
        ///   The buffer to receive the exported secret.
        /// </param>
        /// <exception cref="CryptographicException">
        ///   An error occurred while deriving the exported secret.
        /// </exception>
        /// <remarks>
        ///   The calling method has verified that this instance is not disposed and the destination length
        ///   does not exceed the KDF's maximum export length. The destination may be empty.
        ///   Implementations must fill the entire destination on success without advancing the sender's message sequence.
        /// </remarks>
        protected abstract void ExportCore(ReadOnlySpan<byte> exporterContext, Span<byte> destination);

        /// <summary>
        ///   Releases all resources used by the <see cref="HpkeSender" /> class.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        ///   Releases the unmanaged resources used by this sender and optionally releases its managed resources.
        /// </summary>
        /// <param name="disposing">
        ///   <see langword="true" /> to release both managed and unmanaged resources;
        ///   <see langword="false" /> to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
        }

        private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
