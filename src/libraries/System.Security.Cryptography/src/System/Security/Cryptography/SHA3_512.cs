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
    /// Computes the SHA3-512 hash for the input data.
    /// </summary>
    /// <remarks>
    /// This algorithm is specified by FIPS 202.
    /// </remarks>
    public abstract class SHA3_512 : HashAlgorithm
    {
        private sealed class HashTrait : IHashStatic
        {
            static int IHashStatic.HashSizeInBytes => HashSizeInBytes;
            static string IHashStatic.HashAlgorithmName => HashAlgorithmNames.SHA3_512;
            static bool IHashStatic.IsSupported => IsSupported;
        }

        /// <summary>
        /// The hash size produced by the SHA3-512 algorithm, in bits.
        /// </summary>
        public const int HashSizeInBits = 512;

        /// <summary>
        /// The hash size produced by the SHA3-512 algorithm, in bytes.
        /// </summary>
        public const int HashSizeInBytes = HashSizeInBits / 8;

        /// <summary>
        /// Initializes a new instance of <see cref="SHA3_512" />.
        /// </summary>
        protected SHA3_512()
        {
            HashSizeValue = HashSizeInBits;
        }

        /// <summary>
        /// Gets a value that indicates whether the algorithm is supported on the current platform.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the algorithm is supported; otherwise, <see langword="false" />.
        /// </value>
        public static bool IsSupported { get; } = HashProviderDispenser.HashSupported(HashAlgorithmNames.SHA3_512);

        /// <summary>
        /// Creates an instance of the default implementation of <see cref="SHA3_512" />.
        /// </summary>
        /// <returns>
        /// A new instance of <see cref="SHA3_512" />.
        /// </returns>
        /// <exception cref="PlatformNotSupportedException">
        /// The platform does not support SHA3-512.
        /// </exception>
        public static new SHA3_512 Create()
        {
            HashStatic<HashTrait>.CheckPlatformSupport();
            return new Implementation();
        }

        /// <summary>
        /// Computes the hash of data using the SHA3-512 algorithm.
        /// </summary>
        /// <param name="source">The data to hash.</param>
        /// <returns>The hash of the data.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        /// The platform does not support SHA3-512.
        /// </exception>
        public static byte[] HashData(byte[] source) => HashStatic<HashTrait>.HashData(source);

        /// <summary>
        /// Computes the hash of data using the SHA3-512 algorithm.
        /// </summary>
        /// <param name="source">The data to hash.</param>
        /// <returns>The hash of the data.</returns>
        /// <exception cref="PlatformNotSupportedException">
        /// The platform does not support SHA3-512.
        /// </exception>
        public static byte[] HashData(ReadOnlySpan<byte> source) => HashStatic<HashTrait>.HashData(source);

        /// <summary>
        /// Computes the hash of data using the SHA3-512 algorithm.
        /// </summary>
        /// <param name="source">The data to hash.</param>
        /// <param name="destination">The buffer to receive the hash value.</param>
        /// <returns>The total number of bytes written to <paramref name="destination" />.</returns>
        /// <exception cref="ArgumentException">
        /// The buffer in <paramref name="destination"/> is too small to hold the calculated hash
        /// size. The SHA3-512 algorithm always produces a 512-bit hash, or 64 bytes.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        /// The platform does not support SHA3-512.
        /// </exception>
        public static int HashData(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            return HashStatic<HashTrait>.HashData(source, destination);
        }

        /// <summary>
        /// Attempts to compute the hash of data using the SHA3-512 algorithm.
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
        /// The platform does not support SHA3-512.
        /// </exception>
        public static bool TryHashData(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            return HashStatic<HashTrait>.TryHashData(source, destination, out bytesWritten);
        }

        /// <summary>
        /// Computes the hash of a stream using the SHA3-512 algorithm.
        /// </summary>
        /// <param name="source">The stream to hash.</param>
        /// <param name="destination">The buffer to receive the hash value.</param>
        /// <returns>The total number of bytes written to <paramref name="destination" />.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   The buffer in <paramref name="destination"/> is too small to hold the calculated hash
        ///   size. The SHA3-512 algorithm always produces a 512-bit hash, or 64 bytes.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///   <paramref name="source" /> does not support reading.
        ///   </para>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        /// The platform does not support SHA3-512.
        /// </exception>
        public static int HashData(Stream source, Span<byte> destination)
        {
            return HashStatic<HashTrait>.HashData(source, destination);
        }

        /// <summary>
        /// Computes the hash of a stream using the SHA3-512 algorithm.
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
        /// The platform does not support SHA3-512.
        /// </exception>
        public static byte[] HashData(Stream source) => HashStatic<HashTrait>.HashData(source);

        /// <summary>
        /// Asynchronously computes the hash of a stream using the SHA3-512 algorithm.
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
        /// The platform does not support SHA3-512.
        /// </exception>
        public static ValueTask<byte[]> HashDataAsync(Stream source, CancellationToken cancellationToken = default)
        {
            return HashStatic<HashTrait>.HashDataAsync(source, cancellationToken);
        }

        /// <summary>
        /// Asynchronously computes the hash of a stream using the SHA3-512 algorithm.
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
        ///   <para>
        ///   The buffer in <paramref name="destination"/> is too small to hold the calculated hash
        ///   size. The SHA3-512 algorithm always produces a 512-bit hash, or 64 bytes.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///   <paramref name="source" /> does not support reading.
        ///   </para>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        /// The platform does not support SHA3-512.
        /// </exception>
        public static ValueTask<int> HashDataAsync(
            Stream source,
            Memory<byte> destination,
            CancellationToken cancellationToken = default)
        {
            return HashStatic<HashTrait>.HashDataAsync(source, destination, cancellationToken);
        }

        private sealed class Implementation : SHA3_512
        {
            private readonly HashProvider _hashProvider;

            public Implementation()
            {
                _hashProvider = HashProviderDispenser.CreateHashProvider(HashAlgorithmNames.SHA3_512);
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
