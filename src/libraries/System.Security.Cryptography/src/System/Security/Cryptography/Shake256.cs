// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Security.Cryptography
{
    /// <summary>
    ///   Computes the SHAKE256 hash for the input data.
    /// </summary>
    /// <remarks>
    ///   This algorithm is specified by FIPS 202. The SHAKE algorithm family is an extendable-output function (XOF)
    ///   which allows the output to be extended to any length. The size of the XOF indicates the security strength of
    ///   the algorithm, not the output size.
    /// </remarks>
    public sealed partial class Shake256 : IDisposable
    {
        // Some platforms have a mutable struct for LiteXof, do not mark this field as readonly.
        private LiteXof _hashProvider;
        private bool _disposed;

        /// <summary>
        ///   Initializes a new instance of the <see cref="Shake256" /> class.
        /// </summary>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support SHAKE256. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports SHAKE256.
        /// </exception>
        public Shake256()
        {
            CheckPlatformSupport();
            _hashProvider = LiteHashProvider.CreateXof(HashAlgorithmId);
        }

        /// <summary>
        ///   Gets a value that indicates whether the algorithm is supported on the current platform.
        /// </summary>
        /// <value>
        ///   <see langword="true" /> if the algorithm is supported; otherwise, <see langword="false" />.
        /// </value>
        public static bool IsSupported { get; } = HashProviderDispenser.HashSupported(HashAlgorithmId);

        /// <summary>
        ///   Appends the specified data to the data already processed in the hash.
        /// </summary>
        /// <param name="data">The data to process.</param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="data" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
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
        public void AppendData(ReadOnlySpan<byte> data)
        {
            CheckDisposed();

            _hashProvider.Append(data);
        }

        /// <summary>
        ///   Retrieves the hash for the data accumulated from prior calls to the <c>AppendData</c> methods,
        ///   and resets the object to its initial state.
        /// </summary>
        /// <param name="outputLength">The size of the hash to produce.</param>
        /// <returns>The computed hash.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="outputLength" /> is negative.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        /// <seealso cref="GetCurrentHash(int)" />
        public byte[] GetHashAndReset(int outputLength)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(outputLength);
            CheckDisposed();

            byte[] output = new byte[outputLength];
            _hashProvider.Finalize(output);
            _hashProvider.Reset();
            return output;
        }

        /// <summary>
        ///   Fills the buffer with the hash for the data accumulated from prior calls to the <c>AppendData</c> methods,
        ///   and resets the object to its initial state.
        /// </summary>
        /// <param name="destination">The buffer to fill with the hash.</param>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        /// <seealso cref="GetCurrentHash(Span{byte})" />
        public void GetHashAndReset(Span<byte> destination)
        {
            CheckDisposed();

            _hashProvider.Finalize(destination);
            _hashProvider.Reset();
        }

        /// <summary>
        ///   Retrieves the hash for the data accumulated from prior calls to the <c>AppendData</c> methods,
        ///   without resetting the object to its initial state.
        /// </summary>
        /// <param name="outputLength">The size of the hash to produce.</param>
        /// <returns>The computed hash.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="outputLength" /> is negative.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        /// <seealso cref="GetHashAndReset(int)" />
        public byte[] GetCurrentHash(int outputLength)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(outputLength);
            CheckDisposed();

            byte[] output = new byte[outputLength];
            _hashProvider.Current(output);
            return output;
        }

        /// <summary>
        ///   Fills the buffer with the hash for the data accumulated from prior calls to the <c>AppendData</c> methods,
        ///   without resetting the object to its initial state.
        /// </summary>
        /// <param name="destination">The buffer to fill with the hash.</param>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        /// <seealso cref="GetHashAndReset(Span{byte})" />
        public void GetCurrentHash(Span<byte> destination)
        {
            CheckDisposed();
            _hashProvider.Current(destination);
        }

        /// <summary>
        ///   Release all resources used by the current instance of the <see cref="Shake256" /> class.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _hashProvider.Dispose();
                _disposed = true;
            }
        }

        /// <summary>
        /// Computes the hash of data using the SHAKE256 algorithm.
        /// </summary>
        /// <param name="source">The data to hash.</param>
        /// <param name="outputLength">The size of the hash to produce.</param>
        /// <returns>The hash of the data.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="outputLength" /> is negative.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support SHAKE256. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports SHAKE256.
        /// </exception>
        public static byte[] HashData(byte[] source, int outputLength)
        {
            ArgumentNullException.ThrowIfNull(source);

            return HashData(new ReadOnlySpan<byte>(source), outputLength);
        }

        /// <summary>
        /// Computes the hash of data using the SHAKE256 algorithm.
        /// </summary>
        /// <param name="source">The data to hash.</param>
        /// <param name="outputLength">The size of the hash to produce.</param>
        /// <returns>The hash of the data.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="outputLength" /> is negative.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support SHAKE256. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports SHAKE256.
        /// </exception>
        public static byte[] HashData(ReadOnlySpan<byte> source, int outputLength)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(outputLength);
            CheckPlatformSupport();

            byte[] output = new byte[outputLength];
            HashDataCore(source, output);
            return output;
        }

        /// <summary>
        /// Computes the hash of data using the SHAKE256 algorithm.
        /// </summary>
        /// <param name="source">The data to hash.</param>
        /// <param name="destination">The buffer to fill with the hash.</param>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support SHAKE256. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports SHAKE256.
        /// </exception>
        public static void HashData(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            CheckPlatformSupport();
            HashDataCore(source, destination);
        }

        /// <summary>
        /// Computes the hash of a stream using the SHAKE256 algorithm.
        /// </summary>
        /// <param name="source">The stream to hash.</param>
        /// <param name="outputLength">The size of the hash to produce.</param>
        /// <returns>The hash of the data.</returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="source" /> does not support reading.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="outputLength" /> is negative.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support SHAKE256. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports SHAKE256.
        /// </exception>
        public static byte[] HashData(Stream source, int outputLength)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentOutOfRangeException.ThrowIfNegative(outputLength);

            if (!source.CanRead)
            {
                throw new ArgumentException(SR.Argument_StreamNotReadable, nameof(source));
            }

            CheckPlatformSupport();
            return LiteHashProvider.XofStream(HashAlgorithmId, outputLength, source);
        }

        /// <summary>
        /// Computes the hash of a stream using the SHAKE256 algorithm.
        /// </summary>
        /// <param name="source">The stream to hash.</param>
        /// <param name="destination">The buffer to fill with the hash.</param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="source" /> does not support reading.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support SHAKE256. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports SHAKE256.
        /// </exception>
        public static void HashData(Stream source, Span<byte> destination)
        {
            ArgumentNullException.ThrowIfNull(source);

            if (!source.CanRead)
            {
                throw new ArgumentException(SR.Argument_StreamNotReadable, nameof(source));
            }

            CheckPlatformSupport();
            LiteHashProvider.XofStream(HashAlgorithmId, source, destination);
        }

        /// <summary>
        /// Asynchronously computes the hash of a stream using the SHAKE256 algorithm.
        /// </summary>
        /// <param name="source">The stream to hash.</param>
        /// <param name="destination">The buffer to fill with the hash.</param>
        /// <param name="cancellationToken">
        ///   The token to monitor for cancellation requests.
        ///   The default value is <see cref="System.Threading.CancellationToken.None" />.
        /// </param>
        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="source" /> does not support reading.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        ///   <paramref name="cancellationToken"/> has been canceled.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support SHAKE256. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports SHAKE256.
        /// </exception>
        public static ValueTask HashDataAsync(Stream source, Memory<byte> destination, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(source);

            if (!source.CanRead)
            {
                throw new ArgumentException(SR.Argument_StreamNotReadable, nameof(source));
            }

            CheckPlatformSupport();
            return LiteHashProvider.XofStreamAsync(HashAlgorithmId, source, destination, cancellationToken);
        }

        /// <summary>
        /// Asynchronously computes the hash of a stream using the SHAKE256 algorithm.
        /// </summary>
        /// <param name="source">The stream to hash.</param>
        /// <param name="outputLength">The size of the hash to produce.</param>
        /// <param name="cancellationToken">
        ///   The token to monitor for cancellation requests.
        ///   The default value is <see cref="System.Threading.CancellationToken.None" />.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}" /> that completes with the computed hash.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="source" /> does not support reading.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="outputLength" /> is negative.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        ///   <paramref name="cancellationToken"/> has been canceled.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support SHAKE256. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports SHAKE256.
        /// </exception>
        public static ValueTask<byte[]> HashDataAsync(Stream source, int outputLength, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentOutOfRangeException.ThrowIfNegative(outputLength);

            if (!source.CanRead)
            {
                throw new ArgumentException(SR.Argument_StreamNotReadable, nameof(source));
            }

            CheckPlatformSupport();
            return LiteHashProvider.XofStreamAsync(HashAlgorithmId, outputLength, source, cancellationToken);
        }

        private static void HashDataCore(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            HashProviderDispenser.OneShotHashProvider.HashDataXof(HashAlgorithmId, source, destination);
        }

        private static void CheckPlatformSupport()
        {
            if (!IsSupported)
            {
                throw new PlatformNotSupportedException();
            }
        }

        private void CheckDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
