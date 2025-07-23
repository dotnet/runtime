// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using System.IO;
using System.Security.Cryptography.Asn1;
using System.Threading;
using System.Threading.Tasks;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    /// <summary>
    ///  Represents a stateful hash algorithm for computing the mu (&#x3BC;) value for an ML-DSA signature
    ///  associated with a specific key.
    /// </summary>
    [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public abstract class MLDsaMuHash : IDisposable
    {
        private bool _disposed;

        /// <summary>
        ///   Gets the ML-DSA key associated with this signature mu (&#x3BC;) computation.
        /// </summary>
        /// <value>The ML-DSA key associated with this signature mu (&#x3BC;) computation.</value>
        protected MLDsa Key { get; private set; }

        /// <summary>
        ///   Gets the output size, in bytes, of this hash algorithm.
        /// </summary>
        /// <value>
        ///   The output size, in bytes, of this hash algorithm.
        /// </value>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        public int HashLengthInBytes
        {
            get
            {
                ThrowIfDisposed();

                return Key.Algorithm.MuSizeInBytes;
            }
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="MLDsaMuHash"/> class for use with the specified key.
        /// </summary>
        /// <param name="key">The ML-DSA key associated with this hash state.</param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="key"/> is <see langword="null" />.
        /// </exception>
        protected MLDsaMuHash(MLDsa key)
        {
            ArgumentNullException.ThrowIfNull(key);
            Key = key;
        }

        /// <summary>
        ///   Releases all resources used by the <see cref="MLDsaMuHash"/> class.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
                _disposed = true;
            }
        }

        /// <summary>
        ///   Called by the <c>Dispose()</c> and <c>Finalize()</c> methods to release the managed and unmanaged
        ///   resources used by the current instance of the <see cref="MLDsa"/> class.
        /// </summary>
        /// <param name="disposing">
        ///   <see langword="true" /> to release managed and unmanaged resources;
        ///   <see langword="false" /> to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Key = null!;
            }
        }

        /// <summary>
        ///   Appends the specified data to the data already processed in the hash.
        /// </summary>
        /// <param name="data">The data to process.</param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="data" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        /// <exception cref="CryptographicException">An error occurred processing the data.</exception>
        public void AppendData(byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);

            AppendData(new ReadOnlySpan<byte>(data));
        }

        /// <summary>
        ///   Appends the specified data to the data already processed in the hash.
        /// </summary>
        /// <param name="data">The data to process.</param>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        /// <exception cref="CryptographicException">An error occurred processing the data.</exception>
        public void AppendData(ReadOnlySpan<byte> data)
        {
            ThrowIfDisposed();

            // No-op, no data to append.
            if (data.IsEmpty)
            {
                return;
            }

            AppendDataCore(data);
        }

        /// <summary>
        ///   Appends the data from the provided stream into the hash.
        /// </summary>
        /// <param name="stream">The stream with data to process.</param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="stream" /> does not support reading.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="stream" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="CryptographicException">An error occurred processing the data.</exception>
        public void AppendData(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);

            if (!stream.CanRead)
            {
                throw new ArgumentException(SR.Argument_StreamNotReadable, nameof(stream));
            }

            ThrowIfDisposed();

            // Don't use the crypto pool, because the array is passed outside of the cryptography module.
            byte[] buffer = ArrayPool<byte>.Shared.Rent(4096);
            int bytesRead;

            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                AppendDataCore(buffer.AsSpan(0, bytesRead));
            }

            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }

        /// <summary>
        ///   Asynchronously appends the data from a stream into the hash.
        /// </summary>
        /// <param name="stream">The stream to hash.</param>
        /// <param name="cancellationToken">
        ///   The token to monitor for cancellation requests.
        ///   The default value is <see cref="CancellationToken.None" />.
        /// </param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="stream" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="stream" /> does not support reading.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        ///   <paramref name="cancellationToken"/> has been canceled.
        /// </exception>
        /// <exception cref="CryptographicException">An error occurred processing the data.</exception>
        public Task AppendDataAsync(
            Stream stream,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(stream);

            if (!stream.CanRead)
            {
                throw new ArgumentException(SR.Argument_StreamNotReadable, nameof(stream));
            }

            ThrowIfDisposed();

            return AppendDataAsyncCore(this, stream, cancellationToken);

            static async Task AppendDataAsyncCore(
                MLDsaMuHash instance,
                Stream stream,
                CancellationToken cancellationToken)
            {
                // Don't use the crypto pool, because the array is passed outside of the cryptography module.
                byte[] buffer = ArrayPool<byte>.Shared.Rent(4096);
                Memory<byte> buf = buffer.AsMemory();
                int bytesRead;

                while ((bytesRead = await stream.ReadAsync(buf, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    instance.AppendDataCore(new ReadOnlySpan<byte>(buffer, 0, bytesRead));
                }

                ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
            }
        }

        /// <summary>
        ///   Retrieves the hash for the data accumulated from prior calls to the <c>AppendData</c> methods,
        ///   and resets the object to its initial state.
        /// </summary>
        /// <returns>The computed hash.</returns>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        /// <exception cref="CryptographicException">An error occurred processing the data.</exception>
        /// <seealso cref="GetCurrentHash()" />
        public byte[] GetHashAndReset()
        {
            ThrowIfDisposed();

            byte[] ret = new byte[HashLengthInBytes];
            GetHashAndResetCore(ret.AsSpan());
            return ret;
        }

        /// <summary>
        ///   Fills the buffer with the hash for the data accumulated from prior calls to the <c>AppendData</c> methods,
        ///   and resets the object to its initial state.
        /// </summary>
        /// <param name="destination">The buffer to fill with the hash.</param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="destination"/> has a length that is not equal to the output size of this hash algorithm.
        /// </exception>"
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        /// <exception cref="CryptographicException">An error occurred processing the data.</exception>
        /// <seealso cref="GetCurrentHash(Span{byte})" />
        /// <seealso cref="HashLengthInBytes"/>
        public void GetHashAndReset(Span<byte> destination)
        {
            Helpers.ThrowIfWrongLength(destination, HashLengthInBytes);
            ThrowIfDisposed();

            GetHashAndResetCore(destination);
        }

        /// <summary>
        ///   Retrieves the hash for the data accumulated from prior calls to the <c>AppendData</c> methods,
        ///   without resetting the object to its initial state.
        /// </summary>
        /// <returns>The computed hash.</returns>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        /// <seealso cref="GetHashAndReset()" />
        public byte[] GetCurrentHash()
        {
            ThrowIfDisposed();

            byte[] ret = new byte[HashLengthInBytes];
            GetCurrentHashCore(ret.AsSpan());
            return ret;
        }

        /// <summary>
        ///   Fills the buffer with the hash for the data accumulated from prior calls to the <c>AppendData</c> methods,
        ///   without resetting the object to its initial state.
        /// </summary>
        /// <param name="destination">The buffer to fill with the hash.</param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="destination"/> has a length that is not equal to the output size of this hash algorithm.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        /// <exception cref="CryptographicException">An error occurred processing the data.</exception>
        /// <seealso cref="GetHashAndReset(Span{byte})" />
        public void GetCurrentHash(Span<byte> destination)
        {
            Helpers.ThrowIfWrongLength(destination, HashLengthInBytes);
            ThrowIfDisposed();

            GetCurrentHashCore(destination);
        }

        /// <summary>
        ///   Creates a new instance of <see cref="MLDsaMuHash" /> with the existing appended data preserved.
        /// </summary>
        /// <returns>A clone of the current instance.</returns>
        /// <exception cref="CryptographicException">An error has occurred during the operation.</exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        public MLDsaMuHash Clone()
        {
            ThrowIfDisposed();

            MLDsaMuHash clone = CloneCore();

            if (ReferenceEquals(this, clone))
            {
                throw new CryptographicException();
            }

            return clone;
        }

        /// <summary>
        ///   Resets the instance back to its initial state.
        /// </summary>
        /// <exception cref="CryptographicException">An error has occurred during the operation.</exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        public void Reset()
        {
            ThrowIfDisposed();

            ResetCore();
        }

        /// <summary>
        ///   Completes the current mu (&#x3BC;) computation, signs it with the key that created this instance,
        ///   and resets to a fresh mu calculation state.
        /// </summary>
        /// <returns>The signature of the completed mu computation.</returns>
        /// <exception cref="ObjectDisposedException">The object, or original key, has already been disposed.</exception>
        /// <exception cref="CryptographicException">An error has occurred during the operation.</exception>
        public byte[] SignAndReset()
        {
            ThrowIfDisposed();

            int hashLength = HashLengthInBytes;
            Debug.Assert(hashLength == 64);

            Span<byte> mu = stackalloc byte[hashLength];
            GetHashAndResetCore(mu);
            return Key.SignExternalMu(mu);
        }

        /// <summary>
        ///   Completes the current mu (&#x3BC;) computation, signs it with the key that created this instance,
        ///   writing the result to the provided buffer; then resets this instance to a fresh mu calculation state.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the signature.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="destination"/> has a length that is not equal to the output size of this hash algorithm.
        /// </exception>"
        /// <exception cref="ObjectDisposedException">The object, or original key, has already been disposed.</exception>
        /// <exception cref="CryptographicException">An error has occurred during the operation.</exception>
        public void SignAndReset(Span<byte> destination)
        {
            // Normally we do parameter validation first, but if we're disposed
            // we can't check _key.Algorithm.
            // Rather than keeping the key instance alive, we'll just be backwards here.
            ThrowIfDisposed();

            Helpers.ThrowIfWrongLength(destination, Key.Algorithm.SignatureSizeInBytes);

            int hashLength = HashLengthInBytes;
            Debug.Assert(hashLength == 64);

            Span<byte> mu = stackalloc byte[hashLength];
            GetHashAndResetCore(mu);
            Key.SignExternalMu(mu, destination);
        }

        /// <inheritdoc cref="VerifyAndReset(ReadOnlySpan{byte})"/>
        /// <exception cref="ArgumentNullException"><paramref name="signature"/> is <see langword="null"/>.</exception>
        public bool VerifyAndReset(byte[] signature)
        {
            ArgumentNullException.ThrowIfNull(signature);

            return VerifyAndReset(new ReadOnlySpan<byte>(signature));
        }

        /// <summary>
        ///   Completes the current mu (&#x3BC;) computation, and verifies it with the key that created this instance.
        /// </summary>
        /// <param name="signature">
        ///   The signature to verify against the computed mu value.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the mu value is valid for the key and signature; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="CryptographicException">An error has occurred during the operation.</exception>
        public bool VerifyAndReset(ReadOnlySpan<byte> signature)
        {
            ThrowIfDisposed();

            int hashLength = HashLengthInBytes;
            Debug.Assert(hashLength == 64);

            Span<byte> mu = stackalloc byte[hashLength];
            GetHashAndResetCore(mu);
            return Key.VerifyExternalMu(mu, signature);
        }

        /// <summary>
        ///   When overridden in a derived type, appends the specified data to the data already processed in the hash.
        /// </summary>
        /// <param name="data">The data to process.</param>
        /// <exception cref="CryptographicException">An error occurred processing the data.</exception>
        protected abstract void AppendDataCore(ReadOnlySpan<byte> data);

        /// <summary>
        ///   When overridden in a derived type, retrieves the hash for the data accumulated from prior calls to the
        ///   <c>AppendData</c> methods, without resetting.
        /// </summary>
        /// <param name="destination">The buffer to fill with the hash, always <see cref="HashLengthInBytes" /> in length.</param>
        protected abstract void GetCurrentHashCore(Span<byte> destination);

        /// <summary>
        ///   When overridden in a derived type, retrieves the hash for the data accumulated from prior calls to the
        ///   <c>AppendData</c> methods, and resets to the initial state.
        /// </summary>
        /// <param name="destination">The buffer to fill with the hash, always <see cref="HashLengthInBytes" /> in length.</param>
        protected abstract void GetHashAndResetCore(Span<byte> destination);

        /// <summary>
        ///   When overridden in a derived type, creates a new instance of <see cref="MLDsaMuHash" /> with the existing
        ///   data as accumulated from prior calls to <c>AppendData</c>, but which does not continue to share appended data
        ///   with this instance.
        /// </summary>
        /// <returns>A new instance of <see cref="MLDsaMuHash"/>.</returns>
        protected abstract MLDsaMuHash CloneCore();

        /// <summary>
        ///   Resets the instance back to its initial state without returning the hash.
        /// </summary>
        /// <remarks>
        ///   The default implementation of this method is to call <see cref="GetHashAndResetCore(Span{byte})"/>,
        ///   it is only necessary to override this method when a better implementation is available.
        /// </remarks>
        protected virtual void ResetCore()
        {
            Span<byte> discard = stackalloc byte[HashLengthInBytes];
            GetHashAndResetCore(discard);
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, typeof(MLDsaMuHash));
        }
    }
}
