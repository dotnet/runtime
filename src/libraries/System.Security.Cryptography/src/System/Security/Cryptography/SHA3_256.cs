// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    /// <summary>
    /// Computes the SHA3-256 hash for the input data.
    /// </summary>
    /// <remarks>
    /// This algorithm is specified by FIPS 202.
    /// </remarks>
    public abstract class SHA3_256 : HashAlgorithm
    {
        /// <summary>
        /// The hash size produced by the SHA3-256 algorithm, in bits.
        /// </summary>
        public const int HashSizeInBits = 256;

        /// <summary>
        /// The hash size produced by the SHA3-256 algorithm, in bytes.
        /// </summary>
        public const int HashSizeInBytes = HashSizeInBits / 8;

        /// <summary>
        /// Initializes a new instance of <see cref="SHA3_256" />.
        /// </summary>
        protected SHA3_256()
        {
            HashSizeValue = HashSizeInBits;
        }

        /// <summary>
        /// Gets a value that indicates whether the algorithm is supported on the current platform.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the algorithm is supported; otherwise, <see langword="false" />.
        /// </value>
        public static bool IsSupported { get; } = HashProviderDispenser.HashSupported(HashAlgorithmNames.SHA3_256);

        /// <summary>
        /// Creates an instance of the default implementation of <see cref="SHA3_256" />.
        /// </summary>
        /// <returns>
        /// A new instance of <see cref="SHA3_256" />.
        /// </returns>
        /// <exception cref="PlatformNotSupportedException">
        /// The platform does not support SHA3-256.
        /// </exception>
        public static new SHA3_256 Create()
        {
            CheckSha3Support();
            return new Implementation();
        }

        /// <summary>
        /// Computes the hash of data using the SHA3-256 algorithm.
        /// </summary>
        /// <param name="source">The data to hash.</param>
        /// <returns>The hash of the data.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        /// The platform does not support SHA3-256.
        /// </exception>
        public static byte[] HashData(byte[] source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return HashData(new ReadOnlySpan<byte>(source));
        }

        /// <summary>
        /// Computes the hash of data using the SHA3-256 algorithm.
        /// </summary>
        /// <param name="source">The data to hash.</param>
        /// <returns>The hash of the data.</returns>
        /// <exception cref="PlatformNotSupportedException">
        /// The platform does not support SHA3-256.
        /// </exception>
        public static byte[] HashData(ReadOnlySpan<byte> source)
        {
            byte[] buffer = new byte[HashSizeInBytes];

            int written = HashData(source, buffer.AsSpan());
            Debug.Assert(written == buffer.Length);

            return buffer;
        }

        /// <summary>
        /// Computes the hash of data using the SHA3-256 algorithm.
        /// </summary>
        /// <param name="source">The data to hash.</param>
        /// <param name="destination">The buffer to receive the hash value.</param>
        /// <returns>The total number of bytes written to <paramref name="destination" />.</returns>
        /// <exception cref="ArgumentException">
        /// The buffer in <paramref name="destination"/> is too small to hold the calculated hash
        /// size. The SHA3-256 algorithm always produces a 256-bit hash, or 32 bytes.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        /// The platform does not support SHA3-256.
        /// </exception>
        public static int HashData(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            if (!TryHashData(source, destination, out int bytesWritten))
                throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));

            return bytesWritten;
        }

        /// <summary>
        /// Attempts to compute the hash of data using the SHA3-256 algorithm.
        /// </summary>
        /// <param name="source">The data to hash.</param>
        /// <param name="destination">The buffer to receive the hash value.</param>
        /// <param name="bytesWritten">
        /// When this method returns, the total number of bytes written into <paramref name="destination"/>.
        /// </param>
        /// <returns>
        /// <see langword="false"/> if <paramref name="destination"/> is too small to hold the
        /// calculated hash, <see langword="true"/> otherwise.
        /// </returns>
        /// <exception cref="PlatformNotSupportedException">
        /// The platform does not support SHA3-256.
        /// </exception>
        public static bool TryHashData(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            CheckSha3Support();

            if (destination.Length < HashSizeInBytes)
            {
                bytesWritten = 0;
                return false;
            }

            bytesWritten = HashProviderDispenser.OneShotHashProvider.HashData(HashAlgorithmNames.SHA3_256, source, destination);
            Debug.Assert(bytesWritten == HashSizeInBytes);

            return true;
        }

        /// <summary>
        /// Computes the hash of a stream using the SHA3-256 algorithm.
        /// </summary>
        /// <param name="source">The stream to hash.</param>
        /// <param name="destination">The buffer to receive the hash value.</param>
        /// <returns>The total number of bytes written to <paramref name="destination" />.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <p>
        ///   The buffer in <paramref name="destination"/> is too small to hold the calculated hash
        ///   size. The SHA3-256 algorithm always produces a 256-bit hash, or 32 bytes.
        ///   </p>
        ///   <p>-or-</p>
        ///   <p>
        ///   <paramref name="source" /> does not support reading.
        ///   </p>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        /// The platform does not support SHA3-256.
        /// </exception>
        public static int HashData(Stream source, Span<byte> destination)
        {
            ArgumentNullException.ThrowIfNull(source);

            if (destination.Length < HashSizeInBytes)
                throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));

            if (!source.CanRead)
                throw new ArgumentException(SR.Argument_StreamNotReadable, nameof(source));

            CheckSha3Support();
            return LiteHashProvider.HashStream(HashAlgorithmNames.SHA3_256, source, destination);
        }

        /// <summary>
        /// Computes the hash of a stream using the SHA3-256 algorithm.
        /// </summary>
        /// <param name="source">The stream to hash.</param>
        /// <returns>The hash of the data.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="source" /> does not support reading.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        /// The platform does not support SHA3-256.
        /// </exception>
        public static byte[] HashData(Stream source)
        {
            ArgumentNullException.ThrowIfNull(source);

            if (!source.CanRead)
                throw new ArgumentException(SR.Argument_StreamNotReadable, nameof(source));

            CheckSha3Support();
            return LiteHashProvider.HashStream(HashAlgorithmNames.SHA3_256, HashSizeInBytes, source);
        }

        /// <summary>
        /// Asynchronously computes the hash of a stream using the SHA3-256 algorithm.
        /// </summary>
        /// <param name="source">The stream to hash.</param>
        /// <param name="cancellationToken">
        ///   The token to monitor for cancellation requests.
        ///   The default value is <see cref="System.Threading.CancellationToken.None" />.
        /// </param>
        /// <returns>The hash of the data.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="source" /> does not support reading.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        /// The platform does not support SHA3-256.
        /// </exception>
        public static ValueTask<byte[]> HashDataAsync(Stream source, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(source);

            if (!source.CanRead)
                throw new ArgumentException(SR.Argument_StreamNotReadable, nameof(source));

            CheckSha3Support();
            return LiteHashProvider.HashStreamAsync(HashAlgorithmNames.SHA3_256, source, cancellationToken);
        }

        /// <summary>
        /// Asynchronously computes the hash of a stream using the SHA3-256 algorithm.
        /// </summary>
        /// <param name="source">The stream to hash.</param>
        /// <param name="destination">The buffer to receive the hash value.</param>
        /// <param name="cancellationToken">
        ///   The token to monitor for cancellation requests.
        ///   The default value is <see cref="System.Threading.CancellationToken.None" />.
        /// </param>
        /// <returns>The total number of bytes written to <paramref name="destination" />.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <p>
        ///   The buffer in <paramref name="destination"/> is too small to hold the calculated hash
        ///   size. The SHA3-256 algorithm always produces a 256-bit hash, or 32 bytes.
        ///   </p>
        ///   <p>-or-</p>
        ///   <p>
        ///   <paramref name="source" /> does not support reading.
        ///   </p>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        /// The platform does not support SHA3-256.
        /// </exception>
        public static ValueTask<int> HashDataAsync(
            Stream source,
            Memory<byte> destination,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(source);

            if (destination.Length < HashSizeInBytes)
                throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));

            if (!source.CanRead)
                throw new ArgumentException(SR.Argument_StreamNotReadable, nameof(source));

            CheckSha3Support();
            return LiteHashProvider.HashStreamAsync(
                HashAlgorithmNames.SHA3_256,
                source,
                destination,
                cancellationToken);
        }

        private static void CheckSha3Support()
        {
            if (!IsSupported)
                throw new PlatformNotSupportedException();
        }

        private sealed class Implementation : SHA3_256
        {
            private readonly HashProvider _hashProvider;

            public Implementation()
            {
                _hashProvider = HashProviderDispenser.CreateHashProvider(HashAlgorithmNames.SHA3_256);
                HashSizeValue = _hashProvider.HashSizeInBytes * 8;
            }

            protected sealed override void HashCore(byte[] array, int ibStart, int cbSize) =>
                _hashProvider.AppendHashData(array, ibStart, cbSize);

            protected sealed override void HashCore(ReadOnlySpan<byte> source) =>
                _hashProvider.AppendHashData(source);

            protected sealed override byte[] HashFinal() =>
                _hashProvider.FinalizeHashAndReset();

            protected sealed override bool TryHashFinal(Span<byte> destination, out int bytesWritten) =>
                _hashProvider.TryFinalizeHashAndReset(destination, out bytesWritten);

            public sealed override void Initialize() => _hashProvider.Reset();

            protected sealed override void Dispose(bool disposing)
            {
                _hashProvider.Dispose(disposing);
                base.Dispose(disposing);
            }
        }
    }
}
