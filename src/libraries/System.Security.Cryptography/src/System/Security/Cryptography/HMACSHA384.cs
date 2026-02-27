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

    public class HMACSHA384 : HMAC
    {
        private sealed class HMACTrait : IHMACStatic
        {
            static int IHMACStatic.HashSizeInBytes => HashSizeInBytes;
            static string IHMACStatic.HashAlgorithmName => HashAlgorithmNames.SHA384;
            static bool IHMACStatic.IsSupported => true;
        }

        /// <summary>
        /// The hash size produced by the HMAC SHA384 algorithm, in bits.
        /// </summary>
        public const int HashSizeInBits = 384;

        /// <summary>
        /// The hash size produced by the HMAC SHA384 algorithm, in bytes.
        /// </summary>
        public const int HashSizeInBytes = HashSizeInBits / 8;

        public HMACSHA384()
            : this(RandomNumberGenerator.GetBytes(BlockSize))
        {
        }

        public HMACSHA384(byte[] key)
        {
            ArgumentNullException.ThrowIfNull(key);

            this.HashName = HashAlgorithmNames.SHA384;
            _hMacCommon = new HMACCommon(HashAlgorithmNames.SHA384, key, BlockSize);
            base.Key = _hMacCommon.ActualKey!;
            // change the default value of BlockSizeValue to 128 instead of 64
            BlockSizeValue = BlockSize;
            HashSizeValue = _hMacCommon.HashSizeInBits;
            Debug.Assert(HashSizeValue == HashSizeInBits);
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
                // We don't have a managed implementation of HMAC
                // so we can't support this
                if (value)
                {
                    throw new PlatformNotSupportedException();
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
        /// Computes the HMAC of data using the SHA384 algorithm.
        /// </summary>
        /// <param name="key">The HMAC key.</param>
        /// <param name="source">The data to HMAC.</param>
        /// <returns>The HMAC of the data.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="key" /> or <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        public static byte[] HashData(byte[] key, byte[] source)
        {
            return HMACStatic<HMACTrait>.HashData(key, source);
        }

        /// <summary>
        /// Computes the HMAC of data using the SHA384 algorithm.
        /// </summary>
        /// <param name="key">The HMAC key.</param>
        /// <param name="source">The data to HMAC.</param>
        /// <returns>The HMAC of the data.</returns>
        public static byte[] HashData(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source)
        {
            return HMACStatic<HMACTrait>.HashData(key, source);
        }

        /// <summary>
        /// Computes the HMAC of data using the SHA384 algorithm.
        /// </summary>
        /// <param name="key">The HMAC key.</param>
        /// <param name="source">The data to HMAC.</param>
        /// <param name="destination">The buffer to receive the HMAC value.</param>
        /// <returns>The total number of bytes written to <paramref name="destination" />.</returns>
        /// <exception cref="ArgumentException">
        /// The buffer in <paramref name="destination"/> is too small to hold the calculated hash
        /// size. The SHA384 algorithm always produces a 384-bit HMAC, or 48 bytes.
        /// </exception>
        public static int HashData(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source, Span<byte> destination)
        {
            return HMACStatic<HMACTrait>.HashData(key, source, destination);
        }

        /// <summary>
        /// Attempts to compute the HMAC of data using the SHA384 algorithm.
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
            return HMACStatic<HMACTrait>.TryHashData(key, source, destination, out bytesWritten);
        }

        /// <summary>
        /// Computes the HMAC of a stream using the SHA384 algorithm.
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
        ///   size. The SHA384 algorithm always produces a 384-bit HMAC, or 48 bytes.
        ///   </p>
        ///   <p>-or-</p>
        ///   <p>
        ///   <paramref name="source" /> does not support reading.
        ///   </p>
        /// </exception>
        public static int HashData(ReadOnlySpan<byte> key, Stream source, Span<byte> destination)
        {
            return HMACStatic<HMACTrait>.HashData(key, source, destination);
        }

        /// <summary>
        /// Computes the HMAC of a stream using the SHA384 algorithm.
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
            return HMACStatic<HMACTrait>.HashData(key, source);
        }

        /// <summary>
        /// Computes the HMAC of a stream using the SHA384 algorithm.
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
            return HMACStatic<HMACTrait>.HashData(key, source);
        }

        /// <summary>
        /// Asynchronously computes the HMAC of a stream using the SHA384 algorithm.
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
            return HMACStatic<HMACTrait>.HashDataAsync(key, source, cancellationToken);
        }

        /// <summary>
        /// Asynchronously computes the HMAC of a stream using the SHA384 algorithm.
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
            return HMACStatic<HMACTrait>.HashDataAsync(key, source, cancellationToken);
        }

        /// <summary>
        /// Asynchronously computes the HMAC of a stream using the SHA384 algorithm.
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
        ///   size. The SHA384 algorithm always produces a 384-bit hash, or 48 bytes.
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
            return HMACStatic<HMACTrait>.HashDataAsync(key, source, destination, cancellationToken);
        }

        /// <summary>
        ///   Verifies the HMAC of data using the SHA384 algorithm.
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
            return HMACStatic<HMACTrait>.Verify(key, source, hash);
        }

        /// <inheritdoc cref="Verify(ReadOnlySpan{byte}, ReadOnlySpan{byte}, ReadOnlySpan{byte})" />
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="key" />, <paramref name="source" />, or <paramref name="hash" /> is <see langword="null" />.
        /// </exception>
        public static bool Verify(byte[] key, byte[] source, byte[] hash)
        {
            return HMACStatic<HMACTrait>.Verify(key, source, hash);
        }

        /// <summary>
        ///   Verifies the HMAC of a stream using the SHA384 algorithm.
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
            return HMACStatic<HMACTrait>.Verify(key, source, hash);
        }

        /// <exception cref="ArgumentNullException">
        ///   <paramref name="key" />, <paramref name="source" />, or <paramref name="hash" /> is <see langword="null" />.
        /// </exception>
        /// <inheritdoc cref="Verify(ReadOnlySpan{byte}, Stream, ReadOnlySpan{byte})" />
        public static bool Verify(byte[] key, Stream source, byte[] hash)
        {
            return HMACStatic<HMACTrait>.Verify(key, source, hash);
        }

        /// <summary>
        ///   Asynchronously verifies the HMAC of a stream using the SHA384 algorithm.
        /// </summary>
        /// <param name="key">The HMAC key.</param>
        /// <param name="source">The stream to HMAC.</param>
        /// <param name="hash">The HMAC to compare against.</param>
        /// <param name="cancellationToken">
        ///   The token to monitor for cancellation requests.
        ///   The default value is <see cref="System.Threading.CancellationToken.None" />.
        /// </param>
        /// <returns>
        ///   A task that, when awaited, produces <see langword="true" /> if the computed HMAC of
        ///   <paramref name="source"/> is equal to <paramref name="hash" />; otherwise <see langword="false" />.
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
            return HMACStatic<HMACTrait>.VerifyAsync(key, source, hash, cancellationToken);
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
            return HMACStatic<HMACTrait>.VerifyAsync(key, source, hash, cancellationToken);
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
