// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    /// <summary>
    /// Computes the KMAC128 MAC for the input data.
    /// </summary>
    /// <remarks>
    /// This algorithm is specified by NIST SP 800-185.
    /// </remarks>
    public sealed class Kmac128 : IDisposable
    {
        private readonly LiteKmac _kmacProvider;
        private bool _disposed;

        /// <summary>
        ///   Initializes a new instance of the <see cref="Kmac128" /> class.
        /// </summary>
        /// <param name="key">The KMAC key.</param>
        /// <param name="customizationString">An optional customization string. The default is no customization string.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="key"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="CryptographicException">An error has occurred creating an instance of the algorithm.</exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support KMAC128. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports KMAC128.
        /// </exception>
        public Kmac128(byte[] key, byte[]? customizationString = null)
            : this(Helpers.ArrayToSpanOrThrow(key), customizationString)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="Kmac128" /> class.
        /// </summary>
        /// <param name="key">The KMAC key.</param>
        /// <param name="customizationString">An optional customization string. The default is no customization string.</param>
        /// <exception cref="CryptographicException">An error has occurred creating an instance of the algorithm.</exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support KMAC128. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports KMAC128.
        /// </exception>
        public Kmac128(ReadOnlySpan<byte> key, ReadOnlySpan<byte> customizationString = default)
        {
            CheckPlatformSupport();
            _kmacProvider = LiteHashProvider.CreateKmac(HashAlgorithmNames.KMAC128, key, customizationString, xof: false);
        }

        /// <summary>
        ///   Gets a value that indicates whether the algorithm is supported on the current platform.
        /// </summary>
        /// <value>
        ///   <see langword="true" /> if the algorithm is supported; otherwise, <see langword="false" />.
        /// </value>
        public static bool IsSupported { get; } = HashProviderDispenser.KmacSupported(HashAlgorithmNames.KMAC128);

        /// <summary>
        ///   Appends the specified data to the data already processed in the hash.
        /// </summary>
        /// <param name="data">The data to process.</param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="data" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        public void AppendData(byte[] data) => AppendData(Helpers.ArrayToSpanOrThrow(data));

        /// <summary>
        ///   Appends the specified data to the data already processed in the hash.
        /// </summary>
        /// <param name="data">The data to process.</param>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        public void AppendData(ReadOnlySpan<byte> data)
        {
            CheckDisposed();
            _kmacProvider.Append(data);
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
        /// <exception cref="CryptographicException">An error has occurred during the operation.</exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        /// <seealso cref="GetCurrentHash(int)" />
        public byte[] GetHashAndReset(int outputLength)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(outputLength);
            CheckDisposed();

            byte[] result = new byte[outputLength];
            int written = _kmacProvider.Finalize(result);
            Debug.Assert(written == outputLength);
            _kmacProvider.Reset();
            return result;
        }

        /// <summary>
        ///   Fills the buffer with the hash for the data accumulated from prior calls to the <c>AppendData</c> methods,
        ///   and resets the object to its initial state.
        /// </summary>
        /// <param name="destination">The buffer to fill with the hash.</param>
        /// <exception cref="CryptographicException">An error has occurred during the operation.</exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        /// <seealso cref="GetCurrentHash(Span{byte})" />
        public void GetHashAndReset(Span<byte> destination)
        {
            CheckDisposed();

            int written = _kmacProvider.Finalize(destination);
            Debug.Assert(written == destination.Length);
            _kmacProvider.Reset();
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
        /// <exception cref="CryptographicException">An error has occurred during the operation.</exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        /// <seealso cref="GetHashAndReset(int)" />
        public byte[] GetCurrentHash(int outputLength)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(outputLength);
            CheckDisposed();

            byte[] result = new byte[outputLength];
            int written = _kmacProvider.Current(result);
            Debug.Assert(written == outputLength);
            return result;
        }

        /// <summary>
        ///   Fills the buffer with the hash for the data accumulated from prior calls to the <c>AppendData</c> methods,
        ///   without resetting the object to its initial state.
        /// </summary>
        /// <param name="destination">The buffer to fill with the hash.</param>
        /// <exception cref="CryptographicException">An error has occurred during the operation.</exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        /// <seealso cref="GetHashAndReset(Span{byte})" />
        public void GetCurrentHash(Span<byte> destination)
        {
            CheckDisposed();

            int written = _kmacProvider.Current(destination);
            Debug.Assert(written == destination.Length);
        }

        /// <summary>
        ///   Release all resources used by the current instance of the <see cref="Kmac128" /> class.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _kmacProvider.Dispose();
        }

        /// <summary>
        /// Computes the hash of data using the KMAC128 algorithm.
        /// </summary>
        /// <param name="key">The KMAC key.</param>
        /// <param name="source">The data to hash.</param>
        /// <param name="outputLength">The size of the hash to produce.</param>
        /// <param name="customizationString">An optional customization string. The default is no customization string.</param>
        /// <returns>The hash of the data.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="outputLength" /> is negative.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="key" /> or <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="CryptographicException">An error has occurred during the operation.</exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support KMAC128. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports KMAC128.
        /// </exception>
        public static byte[] HashData(
            byte[] key,
            byte[] source,
            int outputLength,
            byte[]? customizationString = null)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(source);

            return HashData(new ReadOnlySpan<byte>(key), new ReadOnlySpan<byte>(source), outputLength, customizationString);
        }

        /// <summary>
        /// Computes the hash of data using the KMAC128 algorithm.
        /// </summary>
        /// <param name="key">The KMAC key.</param>
        /// <param name="source">The data to hash.</param>
        /// <param name="outputLength">The size of the hash to produce.</param>
        /// <param name="customizationString">An optional customization string. The default is no customization string.</param>
        /// <returns>The hash of the data.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="outputLength" /> is negative.
        /// </exception>
        /// <exception cref="CryptographicException">An error has occurred during the operation.</exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support KMAC128. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports KMAC128.
        /// </exception>
        public static byte[] HashData(
            ReadOnlySpan<byte> key,
            ReadOnlySpan<byte> source,
            int outputLength,
            ReadOnlySpan<byte> customizationString = default)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(outputLength);
            CheckPlatformSupport();

            byte[] output = new byte[outputLength];
            HashDataCore(key, source, output, customizationString);
            return output;
        }

        /// <summary>
        /// Computes the hash of data using the KMAC128 algorithm.
        /// </summary>
        /// <param name="key">The KMAC key.</param>
        /// <param name="source">The data to hash.</param>
        /// <param name="destination">The buffer to fill with the hash.</param>
        /// <param name="customizationString">An optional customization string. The default is no customization string.</param>
        /// <exception cref="CryptographicException">An error has occurred during the operation.</exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support KMAC128. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports KMAC128.
        /// </exception>
        public static void HashData(
            ReadOnlySpan<byte> key,
            ReadOnlySpan<byte> source,
            Span<byte> destination,
            ReadOnlySpan<byte> customizationString = default)
        {
            CheckPlatformSupport();
            HashDataCore(key, source, destination, customizationString);
        }

        /// <summary>
        /// Computes the hash of a stream using the KMAC128 algorithm.
        /// </summary>
        /// <param name="key">The KMAC key.</param>
        /// <param name="source">The stream to hash.</param>
        /// <param name="outputLength">The size of the hash to produce.</param>
        /// <param name="customizationString">An optional customization string. The default is no customization string.</param>
        /// <returns>The hash of the data.</returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="source" /> does not support reading.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="outputLength" /> is negative.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="key" /> or <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="CryptographicException">An error has occurred during the operation.</exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support KMAC128. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports KMAC128.
        /// </exception>
        public static byte[] HashData(
            byte[] key,
            Stream source,
            int outputLength,
            byte[]? customizationString = null)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(source);
            ArgumentOutOfRangeException.ThrowIfNegative(outputLength);

            CheckStreamCanRead(source);
            CheckPlatformSupport();
            return LiteHashProvider.KmacStream(HashAlgorithmNames.KMAC128, key, customizationString, outputLength, source, xof: false);
        }

        /// <summary>
        /// Computes the hash of a stream using the KMAC128 algorithm.
        /// </summary>
        /// <param name="key">The KMAC key.</param>
        /// <param name="source">The stream to hash.</param>
        /// <param name="outputLength">The size of the hash to produce.</param>
        /// <param name="customizationString">An optional customization string. The default is no customization string.</param>
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
        /// <exception cref="CryptographicException">An error has occurred during the operation.</exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support KMAC128. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports KMAC128.
        /// </exception>
        public static byte[] HashData(
            ReadOnlySpan<byte> key,
            Stream source,
            int outputLength,
            ReadOnlySpan<byte> customizationString = default)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentOutOfRangeException.ThrowIfNegative(outputLength);

            CheckStreamCanRead(source);
            CheckPlatformSupport();
            return LiteHashProvider.KmacStream(HashAlgorithmNames.KMAC128, key, customizationString, outputLength, source, xof: false);
        }

        /// <summary>
        /// Computes the hash of a stream using the KMAC128 algorithm.
        /// </summary>
        /// <param name="key">The KMAC key.</param>
        /// <param name="source">The stream to hash.</param>
        /// <param name="destination">The buffer to fill with the hash.</param>
        /// <param name="customizationString">An optional customization string. The default is no customization string.</param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="source" /> does not support reading.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="CryptographicException">An error has occurred during the operation.</exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support KMAC128. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports KMAC128.
        /// </exception>
        public static void HashData(
            ReadOnlySpan<byte> key,
            Stream source,
            Span<byte> destination,
            ReadOnlySpan<byte> customizationString = default)
        {
            ArgumentNullException.ThrowIfNull(source);

            CheckStreamCanRead(source);
            CheckPlatformSupport();
            LiteHashProvider.KmacStream(HashAlgorithmNames.KMAC128, key, customizationString, source, xof: false, destination);
        }

        /// <summary>
        /// Asynchronously computes the hash of a stream using the KMAC128 algorithm.
        /// </summary>
        /// <param name="key">The KMAC key.</param>
        /// <param name="source">The stream to hash.</param>
        /// <param name="outputLength">The size of the hash to produce.</param>
        /// <param name="customizationString">An optional customization string. The default is no customization string.</param>
        /// <param name="cancellationToken">
        ///   The token to monitor for cancellation requests.
        ///   The default value is <see cref="System.Threading.CancellationToken.None" />.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}" /> that completes with the computed hash.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="key" /> or <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="source" /> does not support reading.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="outputLength" /> is negative.
        /// </exception>
        /// <exception cref="CryptographicException">An error has occurred during the operation.</exception>
        /// <exception cref="OperationCanceledException">
        ///   <paramref name="cancellationToken"/> has been canceled.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support KMAC128. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports KMAC128.
        /// </exception>
        public static ValueTask<byte[]> HashDataAsync(
            byte[] key,
            Stream source,
            int outputLength,
            byte[]? customizationString = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(source);
            ArgumentOutOfRangeException.ThrowIfNegative(outputLength);

            CheckStreamCanRead(source);
            CheckPlatformSupport();
            return LiteHashProvider.KmacStreamAsync(HashAlgorithmNames.KMAC128, key, source, xof: false, outputLength, customizationString, cancellationToken);
        }

        /// <summary>
        /// Asynchronously computes the hash of a stream using the KMAC128 algorithm.
        /// </summary>
        /// <param name="key">The KMAC key.</param>
        /// <param name="source">The stream to hash.</param>
        /// <param name="outputLength">The size of the hash to produce.</param>
        /// <param name="customizationString">An optional customization string. The default is no customization string.</param>
        /// <param name="cancellationToken">
        ///   The token to monitor for cancellation requests.
        ///   The default value is <see cref="System.Threading.CancellationToken.None" />.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}" /> that completes with the computed hash.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="source" /> does not support reading.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="outputLength" /> is negative.
        /// </exception>
        /// <exception cref="CryptographicException">An error has occurred during the operation.</exception>
        /// <exception cref="OperationCanceledException">
        ///   <paramref name="cancellationToken"/> has been canceled.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support KMAC128. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports KMAC128.
        /// </exception>
        public static ValueTask<byte[]> HashDataAsync(
            ReadOnlyMemory<byte> key,
            Stream source,
            int outputLength,
            ReadOnlyMemory<byte> customizationString = default,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentOutOfRangeException.ThrowIfNegative(outputLength);

            CheckStreamCanRead(source);
            CheckPlatformSupport();
            return LiteHashProvider.KmacStreamAsync(HashAlgorithmNames.KMAC128, key.Span, source, xof: false, outputLength, customizationString.Span, cancellationToken);
        }

        /// <summary>
        /// Asynchronously computes the hash of a stream using the KMAC128 algorithm.
        /// </summary>
        /// <param name="key">The KMAC key.</param>
        /// <param name="source">The stream to hash.</param>
        /// <param name="destination">The buffer to fill with the hash.</param>
        /// <param name="customizationString">An optional customization string. The default is no customization string.</param>
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
        /// <exception cref="CryptographicException">An error has occurred during the operation.</exception>
        /// <exception cref="OperationCanceledException">
        ///   <paramref name="cancellationToken"/> has been canceled.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support KMAC128. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports KMAC128.
        /// </exception>
        public static ValueTask HashDataAsync(
            ReadOnlyMemory<byte> key,
            Stream source,
            Memory<byte> destination,
            ReadOnlyMemory<byte> customizationString = default,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(source);

            CheckStreamCanRead(source);
            CheckPlatformSupport();
            return LiteHashProvider.KmacStreamAsync(HashAlgorithmNames.KMAC128, key.Span, source, xof: false, destination, customizationString.Span, cancellationToken);
        }

        private static void HashDataCore(
            ReadOnlySpan<byte> key,
            ReadOnlySpan<byte> source,
            Span<byte> destination,
            ReadOnlySpan<byte> customizationString)
        {
            HashProviderDispenser.OneShotHashProvider.KmacData(
                HashAlgorithmNames.KMAC128,
                key,
                source,
                destination,
                customizationString,
                xof: false);
        }

        private void CheckDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

        private static void CheckPlatformSupport()
        {
            if (!IsSupported)
            {
                throw new PlatformNotSupportedException();
            }
        }

        private static void CheckStreamCanRead(Stream source)
        {
            if (!source.CanRead)
            {
                throw new ArgumentException(SR.Argument_StreamNotReadable, nameof(source));
            }
        }
    }
}
