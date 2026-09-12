// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography
{
    /// <summary>
    ///   Represents an HPKE recipient context for decrypting multiple messages and exporting secrets.
    /// </summary>
    [Experimental(Experimentals.HpkeExperimentalDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public abstract class HpkeRecipient : IDisposable
    {
        private bool _disposed;

        /// <summary>
        ///   Gets the cipher suite associated with this recipient.
        /// </summary>
        /// <value>
        ///   The cipher suite associated with this recipient.
        /// </value>
        public HpkeSuite Suite { get; }

        /// <summary>
        ///   Initializes a new instance of the <see cref="HpkeRecipient" /> class with the specified cipher suite.
        /// </summary>
        /// <param name="suite">
        ///   The cipher suite associated with this recipient.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="suite" /> is <see langword="null" />.
        /// </exception>
        protected HpkeRecipient(HpkeSuite suite)
        {
            ArgumentNullException.ThrowIfNull(suite);
            Suite = suite;
        }

        /// <summary>
        ///   Decrypts and authenticates a message using this recipient context.
        /// </summary>
        /// <param name="ciphertext">
        ///   The ciphertext, including its trailing authentication tag.
        /// </param>
        /// <param name="associatedData">
        ///   The additional authenticated data, which must match the value used by the sender.
        /// </param>
        /// <returns>
        ///   A new byte array containing the authenticated plaintext.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="ciphertext" /> is shorter than <see cref="HpkeSuite.AeadTagSizeInBytes" /> bytes.
        /// </exception>
        /// <exception cref="AuthenticationTagMismatchException">
        ///   The authentication tag could not be verified.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   The recipient's message limit has been reached, or an error occurred during decryption.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The object has already been disposed.
        /// </exception>
        /// <remarks>
        ///   Messages must be supplied in the same order in which the corresponding sender context encrypted them.
        /// </remarks>
        public byte[] Open(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> associatedData = default)
        {
            int plaintextLength = GetPlaintextLength(ciphertext);
            ThrowIfDisposed();
            byte[] plaintext = new byte[plaintextLength];

            try
            {
                OpenCore(ciphertext, plaintext, associatedData);
                return plaintext;
            }
            catch
            {
                CryptographicOperations.ZeroMemory(plaintext);
                throw;
            }
        }

        /// <summary>
        ///   Decrypts and authenticates a message using this recipient context.
        /// </summary>
        /// <param name="ciphertext">
        ///   The ciphertext, including its trailing authentication tag.
        /// </param>
        /// <param name="associatedData">
        ///   The additional authenticated data, which must match the value used by the sender,
        ///   or <see langword="null" /> to use no additional authenticated data.
        /// </param>
        /// <returns>
        ///   A new byte array containing the authenticated plaintext.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="ciphertext" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="ciphertext" /> is shorter than <see cref="HpkeSuite.AeadTagSizeInBytes" /> bytes.
        /// </exception>
        /// <exception cref="AuthenticationTagMismatchException">
        ///   The authentication tag could not be verified.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   The recipient's message limit has been reached, or an error occurred during decryption.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The object has already been disposed.
        /// </exception>
        /// <remarks>
        ///   Messages must be supplied in the same order in which the corresponding sender context encrypted them.
        /// </remarks>
        public byte[] Open(byte[] ciphertext, byte[]? associatedData = null)
        {
            ArgumentNullException.ThrowIfNull(ciphertext);
            return Open(new ReadOnlySpan<byte>(ciphertext), new ReadOnlySpan<byte>(associatedData));
        }

        /// <summary>
        ///   Decrypts and authenticates a message into the provided buffer using this recipient context.
        /// </summary>
        /// <param name="ciphertext">
        ///   The ciphertext, including its trailing authentication tag.
        /// </param>
        /// <param name="plaintext">
        ///   The buffer to receive the authenticated plaintext.
        /// </param>
        /// <param name="associatedData">
        ///   The additional authenticated data, which must match the value used by the sender.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///     <paramref name="ciphertext" /> is shorter than <see cref="HpkeSuite.AeadTagSizeInBytes" /> bytes.
        ///   </para>
        ///   <para> -or- </para>
        ///   <para>
        ///     The length of <paramref name="plaintext" /> is not exactly the length of
        ///     <paramref name="ciphertext" /> minus <see cref="HpkeSuite.AeadTagSizeInBytes" />.
        ///   </para>
        /// </exception>
        /// <exception cref="AuthenticationTagMismatchException">
        ///   The authentication tag could not be verified.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     One or more provided buffers overlap.
        ///   </para>
        ///   <para> -or- </para>
        ///   <para>
        ///     The recipient's message limit has been reached, or an error occurred during decryption.
        ///   </para>
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The object has already been disposed.
        /// </exception>
        /// <remarks>
        ///   Messages must be supplied in the same order in which the corresponding sender context encrypted them.
        /// </remarks>
        public void Open(
            ReadOnlySpan<byte> ciphertext,
            Span<byte> plaintext,
            ReadOnlySpan<byte> associatedData = default)
        {
            int plaintextLength = GetPlaintextLength(ciphertext);

            if (plaintext.Length != plaintextLength)
            {
                throw new ArgumentException(
                    SR.Format(SR.Argument_DestinationImprecise, plaintextLength),
                    nameof(plaintext));
            }

            if (ciphertext.Overlaps(plaintext) || associatedData.Overlaps(plaintext))
            {
                throw new CryptographicException(SR.Cryptography_OverlappingBuffers);
            }

            ThrowIfDisposed();
            OpenCore(ciphertext, plaintext, associatedData);
        }

        /// <summary>
        ///   When overridden in a derived class, decrypts and authenticates a message using this recipient context.
        /// </summary>
        /// <param name="ciphertext">
        ///   The ciphertext, including its trailing authentication tag.
        /// </param>
        /// <param name="plaintext">
        ///   The buffer to receive the authenticated plaintext.
        /// </param>
        /// <param name="associatedData">
        ///   The additional authenticated data.
        /// </param>
        /// <exception cref="AuthenticationTagMismatchException">
        ///   The authentication tag could not be verified.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   The recipient's message limit has been reached, or an error occurred during decryption.
        /// </exception>
        protected abstract void OpenCore(
            ReadOnlySpan<byte> ciphertext,
            Span<byte> plaintext,
            ReadOnlySpan<byte> associatedData);

        /// <summary>
        ///   Derives an exported secret from this recipient context.
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
        public byte[] Export(ReadOnlySpan<byte> exporterContext, int length)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(length);
            int maximumLength = Suite.KdfMetadata.MaximumExportLength;

            if (length > maximumLength)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(length),
                    SR.Format(SR.Argument_HpkeExportLengthTooLarge, maximumLength));
            }

            ThrowIfDisposed();
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
        ///   Derives an exported secret from this recipient context.
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
        public byte[] Export(byte[] exporterContext, int length)
        {
            ArgumentNullException.ThrowIfNull(exporterContext);
            return Export(new ReadOnlySpan<byte>(exporterContext), length);
        }

        /// <summary>
        ///   Derives an exported secret from this recipient context into the provided buffer.
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
        ///   <para>
        ///     One or more provided buffers overlap.
        ///   </para>
        ///   <para> -or- </para>
        ///   <para>
        ///     An error occurred while deriving the exported secret.
        ///   </para>
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The object has already been disposed.
        /// </exception>
        public void Export(ReadOnlySpan<byte> exporterContext, Span<byte> destination)
        {
            int maximumLength = Suite.KdfMetadata.MaximumExportLength;

            if (destination.Length > maximumLength)
            {
                throw new ArgumentException(
                    SR.Format(SR.Argument_HpkeExportLengthTooLarge, maximumLength),
                    nameof(destination));
            }

            if (exporterContext.Overlaps(destination))
            {
                throw new CryptographicException(SR.Cryptography_OverlappingBuffers);
            }

            ThrowIfDisposed();
            ExportCore(exporterContext, destination);
        }

        /// <summary>
        ///   When overridden in a derived class, derives an exported secret from this recipient context.
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
        protected abstract void ExportCore(ReadOnlySpan<byte> exporterContext, Span<byte> destination);

        /// <summary>
        ///   Releases all resources used by the <see cref="HpkeRecipient" /> class.
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
        ///   Releases the unmanaged resources used by this recipient and optionally releases its managed resources.
        /// </summary>
        /// <param name="disposing">
        ///   <see langword="true" /> to release both managed and unmanaged resources;
        ///   <see langword="false" /> to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
        }

        private int GetPlaintextLength(ReadOnlySpan<byte> ciphertext)
        {
            int tagSize = Suite.AeadTagSizeInBytes;

            if (ciphertext.Length < tagSize)
            {
                throw new ArgumentException(
                    SR.Format(SR.Argument_HpkeCiphertextTooShort, tagSize),
                    nameof(ciphertext));
            }

            return ciphertext.Length - tagSize;
        }

        private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
