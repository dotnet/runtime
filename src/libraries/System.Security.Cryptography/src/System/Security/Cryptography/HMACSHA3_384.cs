// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    /// <summary>
    /// Computes a Hash-based Message Authentication Code (HMAC) by using the SHA3-384 hash function.
    /// </summary>
    public class HMACSHA3_384 : HMAC
    {
        private HMACCommon _hMacCommon;
        internal const int BlockSize = 104; // FIPS 202 Table 3.

        /// <summary>
        /// The hash size produced by the HMAC SHA3-384 algorithm, in bits.
        /// </summary>
        public const int HashSizeInBits = 384;

        /// <summary>
        /// The hash size produced by the HMAC SHA3-384 algorithm, in bytes.
        /// </summary>
        public const int HashSizeInBytes = HashSizeInBits / 8;

        /// <summary>
        /// Initializes a new instance of the <see cref="HMACSHA3_384" /> class with a randomly generated key.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <see cref="HMACSHA3_384" /> is a type of keyed hash algorithm that is constructed from the SHA3-384 hash
        /// function and used as a Hash-based Message Authentication Code (HMAC). The HMAC process mixes a secret key
        /// with the message data, hashes the result with the hash function, mixes that hash value with the secret key
        /// again, and then applies the hash function a second time. The output hash is 384 bits in length.
        /// </para>
        /// <para>
        /// This constructor uses a 104-byte, randomly generated key.
        /// </para>
        /// </remarks>
        public HMACSHA3_384()
            : this(RandomNumberGenerator.GetBytes(BlockSize))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HMACSHA3_384" /> class with the specified key data.
        /// </summary>
        /// <param name="key">
        /// The secret key for <see cref="HMACSHA3_384" />. The key can be any length.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="key" /> is <see langword="null" />.
        /// </exception>
        public HMACSHA3_384(byte[] key)
        {
            ArgumentNullException.ThrowIfNull(key);
            CheckSha3Support();

            this.HashName = HashAlgorithmNames.SHA3_384;
            _hMacCommon = new HMACCommon(HashAlgorithmNames.SHA3_384, key, BlockSize);
            base.Key = _hMacCommon.ActualKey!;
            BlockSizeValue = BlockSize;
            HashSizeValue = HashSizeInBits;
            Debug.Assert(HashSizeValue == _hMacCommon.HashSizeInBits);
        }

        /// <summary>
        /// Gets a value that indicates whether the algorithm is supported on the current platform.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the algorithm is supported; otherwise, <see langword="false" />.
        /// </value>
        public static bool IsSupported { get; } = HashProviderDispenser.MacSupported(HashAlgorithmNames.SHA3_384);

        /// <inheritdoc />
        public override byte[] Key
        {
            get
            {
                return base.Key;
            }
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                _hMacCommon.ChangeKey(value);
                base.Key = _hMacCommon.ActualKey!;
            }
        }

        /// <inheritdoc />
        protected override void HashCore(byte[] rgb, int ib, int cb) =>
            _hMacCommon.AppendHashData(rgb, ib, cb);

        /// <inheritdoc />
        protected override void HashCore(ReadOnlySpan<byte> source) =>
            _hMacCommon.AppendHashData(source);

        /// <inheritdoc />
        protected override byte[] HashFinal() =>
            _hMacCommon.FinalizeHashAndReset();

        /// <inheritdoc />
        protected override bool TryHashFinal(Span<byte> destination, out int bytesWritten) =>
            _hMacCommon.TryFinalizeHashAndReset(destination, out bytesWritten);

        /// <inheritdoc />
        public override void Initialize() => _hMacCommon.Reset();

        /// <summary>
        /// Computes the HMAC of data using the SHA3-384 algorithm.
        /// </summary>
        /// <param name="key">The HMAC key.</param>
        /// <param name="source">The data to HMAC.</param>
        /// <returns>The HMAC of the data.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="key" /> or <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        public static byte[] HashData(byte[] key, byte[] source)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(source);

            return HashData(new ReadOnlySpan<byte>(key), new ReadOnlySpan<byte>(source));
        }

        /// <summary>
        /// Computes the HMAC of data using the SHA3-384 algorithm.
        /// </summary>
        /// <param name="key">The HMAC key.</param>
        /// <param name="source">The data to HMAC.</param>
        /// <returns>The HMAC of the data.</returns>
        public static byte[] HashData(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source)
        {
            byte[] buffer = new byte[HashSizeInBytes];

            int written = HashData(key, source, buffer.AsSpan());
            Debug.Assert(written == buffer.Length);

            return buffer;
        }

        /// <summary>
        /// Computes the HMAC of data using the SHA3-384 algorithm.
        /// </summary>
        /// <param name="key">The HMAC key.</param>
        /// <param name="source">The data to HMAC.</param>
        /// <param name="destination">The buffer to receive the HMAC value.</param>
        /// <returns>The total number of bytes written to <paramref name="destination" />.</returns>
        /// <exception cref="ArgumentException">
        /// The buffer in <paramref name="destination"/> is too small to hold the calculated hash
        /// size. The SHA3-384 algorithm always produces a 384-bit HMAC, or 48 bytes.
        /// </exception>
        public static int HashData(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source, Span<byte> destination)
        {
            if (!TryHashData(key, source, destination, out int bytesWritten))
            {
                throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));
            }

            return bytesWritten;
        }

        /// <summary>
        /// Attempts to compute the HMAC of data using the SHA3-384 algorithm.
        /// </summary>
        /// <param name="key">The HMAC key.</param>
        /// <param name="source">The data to HMAC.</param>
        /// <param name="destination">The buffer to receive the HMAC value.</param>
        /// <param name="bytesWritten">
        /// When this method returns, the total number of bytes written into <paramref name="destination"/>.
        /// </param>
        /// <returns>
        /// <see langword="false"/> if <paramref name="destination"/> is too small to hold the
        /// calculated hash, <see langword="true"/> otherwise.
        /// </returns>
        public static bool TryHashData(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            CheckSha3Support();

            if (destination.Length < HashSizeInBytes)
            {
                bytesWritten = 0;
                return false;
            }

            bytesWritten = HashProviderDispenser.OneShotHashProvider.MacData(HashAlgorithmNames.SHA3_384, key, source, destination);
            Debug.Assert(bytesWritten == HashSizeInBytes);

            return true;
        }

        /// <summary>
        /// Computes the HMAC of a stream using the SHA3-384 algorithm.
        /// </summary>
        /// <param name="key">The HMAC key.</param>
        /// <param name="source">The stream to HMAC.</param>
        /// <param name="destination">The buffer to receive the HMAC value.</param>
        /// <returns>The total number of bytes written to <paramref name="destination" />.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   The buffer in <paramref name="destination"/> is too small to hold the calculated HMAC
        ///   size. The SHA3-384 algorithm always produces a 384-bit HMAC, or 48 bytes.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///   <paramref name="source" /> does not support reading.
        ///   </para>
        /// </exception>
        public static int HashData(ReadOnlySpan<byte> key, Stream source, Span<byte> destination)
        {
            ArgumentNullException.ThrowIfNull(source);

            if (destination.Length < HashSizeInBytes)
                throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));

            if (!source.CanRead)
                throw new ArgumentException(SR.Argument_StreamNotReadable, nameof(source));

            CheckSha3Support();
            return LiteHashProvider.HmacStream(HashAlgorithmNames.SHA3_384, key, source, destination);
        }

        /// <summary>
        /// Computes the HMAC of a stream using the SHA3-384 algorithm.
        /// </summary>
        /// <param name="key">The HMAC key.</param>
        /// <param name="source">The stream to HMAC.</param>
        /// <returns>The HMAC of the data.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="source" /> does not support reading.
        /// </exception>
        public static byte[] HashData(ReadOnlySpan<byte> key, Stream source)
        {
            ArgumentNullException.ThrowIfNull(source);

            if (!source.CanRead)
                throw new ArgumentException(SR.Argument_StreamNotReadable, nameof(source));

            CheckSha3Support();
            return LiteHashProvider.HmacStream(HashAlgorithmNames.SHA3_384, HashSizeInBytes, key, source);
        }

        /// <summary>
        /// Computes the HMAC of a stream using the SHA3-384 algorithm.
        /// </summary>
        /// <param name="key">The HMAC key.</param>
        /// <param name="source">The stream to HMAC.</param>
        /// <returns>The HMAC of the data.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="key" /> or <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="source" /> does not support reading.
        /// </exception>
        public static byte[] HashData(byte[] key, Stream source)
        {
            ArgumentNullException.ThrowIfNull(key);

            return HashData(new ReadOnlySpan<byte>(key), source);
        }

        /// <summary>
        /// Asynchronously computes the HMAC of a stream using the SHA3-384 algorithm.
        /// </summary>
        /// <param name="key">The HMAC key.</param>
        /// <param name="source">The stream to HMAC.</param>
        /// <param name="cancellationToken">
        ///   The token to monitor for cancellation requests.
        ///   The default value is <see cref="System.Threading.CancellationToken.None" />.
        /// </param>
        /// <returns>The HMAC of the data.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="source" /> does not support reading.
        /// </exception>
        public static ValueTask<byte[]> HashDataAsync(ReadOnlyMemory<byte> key, Stream source, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(source);

            if (!source.CanRead)
                throw new ArgumentException(SR.Argument_StreamNotReadable, nameof(source));

            CheckSha3Support();
            return LiteHashProvider.HmacStreamAsync(HashAlgorithmNames.SHA3_384, key.Span, source, cancellationToken);
        }

        /// <summary>
        /// Asynchronously computes the HMAC of a stream using the SHA3-384 algorithm.
        /// </summary>
        /// <param name="key">The HMAC key.</param>
        /// <param name="source">The stream to HMAC.</param>
        /// <param name="cancellationToken">
        ///   The token to monitor for cancellation requests.
        ///   The default value is <see cref="System.Threading.CancellationToken.None" />.
        /// </param>
        /// <returns>The HMAC of the data.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="key" /> or <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="source" /> does not support reading.
        /// </exception>
        public static ValueTask<byte[]> HashDataAsync(byte[] key, Stream source, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(key);

            return HashDataAsync(new ReadOnlyMemory<byte>(key), source, cancellationToken);
        }

        /// <summary>
        /// Asynchronously computes the HMAC of a stream using the SHA3-384 algorithm.
        /// </summary>
        /// <param name="key">The HMAC key.</param>
        /// <param name="source">The stream to HMAC.</param>
        /// <param name="destination">The buffer to receive the HMAC value.</param>
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
        ///   size. The SHA3-384 algorithm always produces a 384-bit hash, or 48 bytes.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///   <paramref name="source" /> does not support reading.
        ///   </para>
        /// </exception>
        public static ValueTask<int> HashDataAsync(
            ReadOnlyMemory<byte> key,
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
            return LiteHashProvider.HmacStreamAsync(
                HashAlgorithmNames.SHA3_384,
                key.Span,
                source,
                destination,
                cancellationToken);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                HMACCommon hMacCommon = _hMacCommon;
                if (hMacCommon != null)
                {
                    _hMacCommon = null!;
                    hMacCommon.Dispose(disposing);
                }
            }
            base.Dispose(disposing);
        }

        private static void CheckSha3Support()
        {
            if (!IsSupported)
                throw new PlatformNotSupportedException();
        }
    }
}
