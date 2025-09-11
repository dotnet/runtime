// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    //
    // If you change anything in this class, you must make the same change in the other HMAC* classes. This is a pain but given that the
    // preexisting contract from the .NET Framework locks all of these into deriving directly from HMAC, it can't be helped.
    //

    public class HMACSHA512 : HMAC, IHMACShared
    {
        /// <summary>
        /// The hash size produced by the HMAC SHA512 algorithm, in bits.
        /// </summary>
        public const int HashSizeInBits = 512;

        /// <summary>
        /// The hash size produced by the HMAC SHA512 algorithm, in bytes.
        /// </summary>
        public const int HashSizeInBytes = HashSizeInBits / 8;

        public HMACSHA512()
            : this(RandomNumberGenerator.GetBytes(BlockSize))
        {
        }

        public HMACSHA512(byte[] key)
        {
            ArgumentNullException.ThrowIfNull(key);

            this.HashName = HashAlgorithmNames.SHA512;
            _hMacCommon = new HMACCommon(HashAlgorithmNames.SHA512, key, BlockSize);
            base.Key = _hMacCommon.ActualKey!;
            // change the default value of BlockSizeValue to 128 instead of 64
            BlockSizeValue = BlockSize;
            HashSizeValue = _hMacCommon.HashSizeInBits;
        }

        [Obsolete(Obsoletions.ProduceLegacyHmacValuesMessage, DiagnosticId = Obsoletions.ProduceLegacyHmacValuesDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public bool ProduceLegacyHmacValues
        {
            get
            {
                return false;
            }
            set
            {
                if (value)
                {
                    throw new PlatformNotSupportedException(); // This relates to a quirk in the Desktop managed implementation; ours is native.
                }
            }
        }

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

        static int IHMACShared.HashSizeInBytes => HashSizeInBytes;
        static string IHMACShared.HashAlgorithmName => HashAlgorithmNames.SHA512;

        protected override void HashCore(byte[] rgb, int ib, int cb) =>
            _hMacCommon.AppendHashData(rgb, ib, cb);

        protected override void HashCore(ReadOnlySpan<byte> source) =>
            _hMacCommon.AppendHashData(source);

        protected override byte[] HashFinal() =>
            _hMacCommon.FinalizeHashAndReset();

        protected override bool TryHashFinal(Span<byte> destination, out int bytesWritten) =>
            _hMacCommon.TryFinalizeHashAndReset(destination, out bytesWritten);

        public override void Initialize() => _hMacCommon.Reset();

        /// <summary>
        /// Computes the HMAC of data using the SHA512 algorithm.
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
        /// Computes the HMAC of data using the SHA512 algorithm.
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
        /// Computes the HMAC of data using the SHA512 algorithm.
        /// </summary>
        /// <param name="key">The HMAC key.</param>
        /// <param name="source">The data to HMAC.</param>
        /// <param name="destination">The buffer to receive the HMAC value.</param>
        /// <returns>The total number of bytes written to <paramref name="destination" />.</returns>
        /// <exception cref="ArgumentException">
        /// The buffer in <paramref name="destination"/> is too small to hold the calculated hash
        /// size. The SHA512 algorithm always produces a 512-bit HMAC, or 64 bytes.
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
        /// Attempts to compute the HMAC of data using the SHA512 algorithm.
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
            if (destination.Length < HashSizeInBytes)
            {
                bytesWritten = 0;
                return false;
            }

            bytesWritten = HashProviderDispenser.OneShotHashProvider.MacData(HashAlgorithmNames.SHA512, key, source, destination);
            Debug.Assert(bytesWritten == HashSizeInBytes);

            return true;
        }

        /// <summary>
        /// Computes the HMAC of a stream using the SHA512 algorithm.
        /// </summary>
        /// <param name="key">The HMAC key.</param>
        /// <param name="source">The stream to HMAC.</param>
        /// <param name="destination">The buffer to receive the HMAC value.</param>
        /// <returns>The total number of bytes written to <paramref name="destination" />.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <p>
        ///   The buffer in <paramref name="destination"/> is too small to hold the calculated HMAC
        ///   size. The SHA512 algorithm always produces a 512-bit HMAC, or 64 bytes.
        ///   </p>
        ///   <p>-or-</p>
        ///   <p>
        ///   <paramref name="source" /> does not support reading.
        ///   </p>
        /// </exception>
        public static int HashData(ReadOnlySpan<byte> key, Stream source, Span<byte> destination)
        {
            ArgumentNullException.ThrowIfNull(source);

            if (destination.Length < HashSizeInBytes)
                throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));

            if (!source.CanRead)
                throw new ArgumentException(SR.Argument_StreamNotReadable, nameof(source));

            return LiteHashProvider.HmacStream(HashAlgorithmNames.SHA512, key, source, destination);
        }

        /// <summary>
        /// Computes the HMAC of a stream using the SHA512 algorithm.
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

            return LiteHashProvider.HmacStream(HashAlgorithmNames.SHA512, HashSizeInBytes, key, source);
        }

        /// <summary>
        /// Computes the HMAC of a stream using the SHA512 algorithm.
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
        /// Asynchronously computes the HMAC of a stream using the SHA512 algorithm.
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

            return LiteHashProvider.HmacStreamAsync(HashAlgorithmNames.SHA512, key.Span, source, cancellationToken);
        }

        /// <summary>
        /// Asynchronously computes the HMAC of a stream using the SHA512 algorithm.
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
        /// Asynchronously computes the HMAC of a stream using the SHA512 algorithm.
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
        ///   <p>
        ///   The buffer in <paramref name="destination"/> is too small to hold the calculated hash
        ///   size. The SHA512 algorithm always produces a 512-bit hash, or 64 bytes.
        ///   </p>
        ///   <p>-or-</p>
        ///   <p>
        ///   <paramref name="source" /> does not support reading.
        ///   </p>
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

            return LiteHashProvider.HmacStreamAsync(
                HashAlgorithmNames.SHA512,
                key.Span,
                source,
                destination,
                cancellationToken);
        }

        /// <summary>
        ///   Verifies the HMAC of data using the SHA512 algorithm.
        /// </summary>
        /// <param name="key">The HMAC key.</param>
        /// <param name="source">The data to HMAC.</param>
        /// <param name="hash">The HMAC to compare against.</param>
        /// <returns>
        ///   <see langword="true" /> if the computed HMAC of <paramref name="source"/> is equal to
        ///   <paramref name="hash" />; otherwise <see langword="false" />.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="hash"/> has a length not equal to <see cref="HashSizeInBytes" />.
        /// </exception>
        /// <remarks>
        ///   This API performs a fixed-time comparison of the derived HMAC against a known HMAC to prevent leaking
        ///   timing information.
        /// </remarks>
        public static bool Verify(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source, ReadOnlySpan<byte> hash)
        {
            return HMACShared<HMACSHA512>.Verify(key, source, hash);
        }

        /// <inheritdoc cref="Verify(ReadOnlySpan{byte}, ReadOnlySpan{byte}, ReadOnlySpan{byte})" />
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="key" />, <paramref name="source" />, or <paramref name="hash" /> is <see langword="null" />.
        /// </exception>
        public static bool Verify(byte[] key, byte[] source, byte[] hash)
        {
            return HMACShared<HMACSHA512>.Verify(key, source, hash);
        }

        /// <summary>
        /// Verifies the HMAC of a stream using the SHA512 algorithm.
        /// </summary>
        /// <param name="key">The HMAC key.</param>
        /// <param name="source">The stream to HMAC.</param>
        /// <param name="hash">The HMAC to compare against.</param>
        /// <returns>
        ///   <see langword="true" /> if the computed HMAC of <paramref name="source"/> is equal to
        ///   <paramref name="hash" />; otherwise <see langword="false" />.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <para><paramref name="hash"/> has a length not equal to <see cref="HashSizeInBytes" />.</para>
        ///   <para> -or- </para>
        ///   <para><paramref name="source" /> does not support reading.</para>
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <remarks>
        ///   This API performs a fixed-time comparison of the derived HMAC against a known HMAC to prevent leaking
        ///   timing information.
        /// </remarks>
        public static bool Verify(ReadOnlySpan<byte> key, Stream source, ReadOnlySpan<byte> hash)
        {
            return HMACShared<HMACSHA512>.Verify(key, source, hash);
        }

        /// <exception cref="ArgumentNullException">
        ///   <paramref name="key" />, <paramref name="source" />, or <paramref name="hash" /> is <see langword="null" />.
        /// </exception>
        /// <inheritdoc cref="Verify(ReadOnlySpan{byte}, Stream, ReadOnlySpan{byte})" />
        public static bool Verify(byte[] key, System.IO.Stream source, byte[] hash)
        {
            return HMACShared<HMACSHA512>.Verify(key, source, hash);
        }

        /// <summary>
        /// Asynchronously verifies the HMAC of a stream using the SHA512 algorithm.
        /// </summary>
        /// <param name="key">The HMAC key.</param>
        /// <param name="source">The stream to HMAC.</param>
        /// <param name="hash">The HMAC to compare against.</param>
        /// <param name="cancellationToken">
        ///   The token to monitor for cancellation requests.
        ///   The default value is <see cref="System.Threading.CancellationToken.None" />.
        /// </param>
        /// <returns>
        ///   <see langword="true" /> if the computed HMAC of <paramref name="source"/> is equal to
        ///   <paramref name="hash" />; otherwise <see langword="false" />.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <para><paramref name="hash"/> has a length not equal to <see cref="HashSizeInBytes" />.</para>
        ///   <para> -or- </para>
        ///   <para><paramref name="source" /> does not support reading.</para>
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <remarks>
        ///   This API performs a fixed-time comparison of the derived HMAC against a known HMAC to prevent leaking
        ///   timing information.
        /// </remarks>
        public static ValueTask<bool> VerifyAsync(
            ReadOnlyMemory<byte> key,
            Stream source,
            ReadOnlyMemory<byte> hash,
            CancellationToken cancellationToken = default)
        {
            return HMACShared<HMACSHA512>.VerifyAsync(key, source, hash, cancellationToken);
        }

        /// <exception cref="ArgumentNullException">
        ///   <paramref name="key" />, <paramref name="source" />, or <paramref name="hash" /> is <see langword="null" />.
        /// </exception>
        /// <inheritdoc cref="VerifyAsync(ReadOnlyMemory{byte}, Stream, ReadOnlyMemory{byte}, CancellationToken)" />
        public static ValueTask<bool> VerifyAsync(
            byte[] key,
            Stream source,
            byte[] hash,
            CancellationToken cancellationToken = default)
        {
            return HMACShared<HMACSHA512>.VerifyAsync(key, source, hash, cancellationToken);
        }

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

        private HMACCommon _hMacCommon;
        private const int BlockSize = 128;
    }
}
