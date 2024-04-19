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
    public static class CryptographicOperations
    {
        /// <summary>
        /// Determine the equality of two byte sequences in an amount of time which depends on
        /// the length of the sequences, but not the values.
        /// </summary>
        /// <param name="left">The first buffer to compare.</param>
        /// <param name="right">The second buffer to compare.</param>
        /// <returns>
        ///   <c>true</c> if <paramref name="left"/> and <paramref name="right"/> have the same
        ///   values for <see cref="ReadOnlySpan{T}.Length"/> and the same contents, <c>false</c>
        ///   otherwise.
        /// </returns>
        /// <remarks>
        ///   This method compares two buffers' contents for equality in a manner which does not
        ///   leak timing information, making it ideal for use within cryptographic routines.
        ///   This method will short-circuit and return <c>false</c> only if <paramref name="left"/>
        ///   and <paramref name="right"/> have different lengths.
        ///
        ///   Fixed-time behavior is guaranteed in all other cases, including if <paramref name="left"/>
        ///   and <paramref name="right"/> reference the same address.
        /// </remarks>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static bool FixedTimeEquals(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
        {
            // NoOptimization because we want this method to be exactly as non-short-circuiting
            // as written.
            //
            // NoInlining because the NoOptimization would get lost if the method got inlined.

            if (left.Length != right.Length)
            {
                return false;
            }

            int length = left.Length;
            int accum = 0;

            for (int i = 0; i < length; i++)
            {
                accum |= left[i] - right[i];
            }

            return accum == 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void ZeroMemory(Span<byte> buffer)
        {
            // NoOptimize to prevent the optimizer from deciding this call is unnecessary
            // NoInlining to prevent the inliner from forgetting that the method was no-optimize
            buffer.Clear();
        }

        /// <summary>
        /// Computes the hash of data.
        /// </summary>
        /// <param name="hashAlgorithm">The algorithm used to compute the hash.</param>
        /// <param name="source">The data to hash.</param>
        /// <returns>The hash of the data.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <para><paramref name="source" /> is <see langword="null" />.</para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="hashAlgorithm"/> has a <see cref="HashAlgorithmName.Name" /> that is
        ///     <see langword="null" />.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="hashAlgorithm"/> has a <see cref="HashAlgorithmName.Name" /> that is empty.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   <paramref name="hashAlgorithm"/> specifies a hash algorithm not supported by the current platform.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <paramref name="hashAlgorithm"/> specifies an unknown hash algorithm.
        /// </exception>
        public static byte[] HashData(HashAlgorithmName hashAlgorithm, byte[] source)
        {
            ArgumentNullException.ThrowIfNull(source);
            return HashData(hashAlgorithm, new ReadOnlySpan<byte>(source));
        }

        /// <summary>
        /// Computes the hash of data.
        /// </summary>
        /// <param name="hashAlgorithm">The algorithm used to compute the hash.</param>
        /// <param name="source">The data to hash.</param>
        /// <returns>The hash of the data.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="hashAlgorithm"/> has a <see cref="HashAlgorithmName.Name" /> that is
        ///   <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="hashAlgorithm"/> has a <see cref="HashAlgorithmName.Name" /> that is empty.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   <paramref name="hashAlgorithm"/> specifies a hash algorithm not supported by the current platform.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <paramref name="hashAlgorithm"/> specifies an unknown hash algorithm.
        /// </exception>
        public static byte[] HashData(HashAlgorithmName hashAlgorithm, ReadOnlySpan<byte> source)
        {
            int hashSizeInBytes = CheckHashAndGetLength(hashAlgorithm);
            Debug.Assert(hashAlgorithm.Name is not null);

            byte[] buffer = new byte[hashSizeInBytes];
            int written = HashProviderDispenser.OneShotHashProvider.HashData(hashAlgorithm.Name, source, buffer);
            Debug.Assert(written == hashSizeInBytes);
            return buffer;
        }

        /// <summary>
        /// Computes the hash of data.
        /// </summary>
        /// <param name="hashAlgorithm">The algorithm used to compute the hash.</param>
        /// <param name="source">The data to hash.</param>
        /// <param name="destination">The buffer to receive the hash value.</param>
        /// <returns>The total number of bytes written to <paramref name="destination" />.</returns>
        /// <exception cref="ArgumentException">
        ///   <para>The buffer in <paramref name="destination"/> is too small to hold the calculated hash size.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="hashAlgorithm"/> has a <see cref="HashAlgorithmName.Name" /> that is empty.</para>
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="hashAlgorithm"/> has a <see cref="HashAlgorithmName.Name" /> that is
        ///   <see langword="null" />.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   <paramref name="hashAlgorithm"/> specifies a hash algorithm not supported by the current platform.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <paramref name="hashAlgorithm"/> specifies an unknown hash algorithm.
        /// </exception>
        public static int HashData(HashAlgorithmName hashAlgorithm, ReadOnlySpan<byte> source, Span<byte> destination)
        {
            if (!TryHashData(hashAlgorithm, source, destination, out int bytesWritten))
            {
                throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));
            }

            return bytesWritten;
        }

        /// <summary>
        /// Attempts to compute the hash of data.
        /// </summary>
        /// <param name="hashAlgorithm">The algorithm used to compute the hash.</param>
        /// <param name="source">The data to hash.</param>
        /// <param name="destination">The buffer to receive the hash value.</param>
        /// <param name="bytesWritten">
        ///   When this method returns, the total number of bytes written into <paramref name="destination"/>.
        /// </param>
        /// <returns>
        ///   <see langword="false"/> if <paramref name="destination"/> is too small to hold the
        ///   calculated hash, <see langword="true"/> otherwise.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="hashAlgorithm"/> has a <see cref="HashAlgorithmName.Name" /> that is empty.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="hashAlgorithm"/> has a <see cref="HashAlgorithmName.Name" /> that is
        ///   <see langword="null" />.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   <paramref name="hashAlgorithm"/> specifies a hash algorithm not supported by the current platform.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <paramref name="hashAlgorithm"/> specifies an unknown hash algorithm.
        /// </exception>
        public static bool TryHashData(HashAlgorithmName hashAlgorithm, ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            int hashSizeInBytes = CheckHashAndGetLength(hashAlgorithm);
            Debug.Assert(hashAlgorithm.Name is not null);

            if (destination.Length < hashSizeInBytes)
            {
                bytesWritten = 0;
                return false;
            }

            bytesWritten = HashProviderDispenser.OneShotHashProvider.HashData(hashAlgorithm.Name, source, destination);
            Debug.Assert(bytesWritten == hashSizeInBytes);
            return true;
        }

        /// <summary>
        /// Computes the hash of a stream.
        /// </summary>
        /// <param name="hashAlgorithm">The algorithm used to compute the hash.</param>
        /// <param name="source">The stream to hash.</param>
        /// <returns>The hash of the data.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <para><paramref name="source" /> is <see langword="null" />.</para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="hashAlgorithm"/> has a <see cref="HashAlgorithmName.Name" /> that is
        ///     <see langword="null" />.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para><paramref name="hashAlgorithm"/> has a <see cref="HashAlgorithmName.Name" /> that is empty.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="source" /> does not support reading.</para>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   <paramref name="hashAlgorithm"/> specifies a hash algorithm not supported by the current platform.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <paramref name="hashAlgorithm"/> specifies an unknown hash algorithm.
        /// </exception>
        public static byte[] HashData(HashAlgorithmName hashAlgorithm, Stream source)
        {
            int hashSizeInBytes = CheckHashAndGetLength(hashAlgorithm);
            CheckStream(source);
            Debug.Assert(hashAlgorithm.Name is not null);
            return LiteHashProvider.HashStream(hashAlgorithm.Name, hashSizeInBytes, source);
        }

        /// <summary>
        /// Computes the hash of a stream.
        /// </summary>
        /// <param name="hashAlgorithm">The algorithm used to compute the hash.</param>
        /// <param name="source">The stream to hash.</param>
        /// <param name="destination">The buffer to receive the hash value.</param>
        /// <returns>The total number of bytes written to <paramref name="destination" />.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <para><paramref name="source" /> is <see langword="null" />.</para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="hashAlgorithm"/> has a <see cref="HashAlgorithmName.Name" /> that is
        ///     <see langword="null" />.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>The buffer in <paramref name="destination"/> is too small to hold the calculated hash size.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="hashAlgorithm"/> has a <see cref="HashAlgorithmName.Name" /> that is empty.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="source" /> does not support reading.</para>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   <paramref name="hashAlgorithm"/> specifies a hash algorithm not supported by the current platform.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <paramref name="hashAlgorithm"/> specifies an unknown hash algorithm.
        /// </exception>
        public static int HashData(HashAlgorithmName hashAlgorithm, Stream source, Span<byte> destination)
        {
            int hashSizeInBytes = CheckHashAndGetLength(hashAlgorithm);
            CheckStream(source);
            Debug.Assert(hashAlgorithm.Name is not null);
            CheckDestinationSize(hashSizeInBytes, destination.Length);

            int written = LiteHashProvider.HashStream(hashAlgorithm.Name, source, destination);
            Debug.Assert(written == hashSizeInBytes);
            return written;
        }

        /// <summary>
        /// Asynchronously computes the hash of a stream.
        /// </summary>
        /// <param name="hashAlgorithm">The algorithm used to compute the hash.</param>
        /// <param name="source">The stream to hash.</param>
        /// <param name="destination">The buffer to receive the hash value.</param>
        /// <param name="cancellationToken">
        ///   The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
        /// </param>
        /// <returns>The total number of bytes written to <paramref name="destination" />.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <para><paramref name="source" /> is <see langword="null" />.</para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="hashAlgorithm"/> has a <see cref="HashAlgorithmName.Name" /> that is
        ///     <see langword="null" />.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>The buffer in <paramref name="destination"/> is too small to hold the calculated hash size.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="hashAlgorithm"/> has a <see cref="HashAlgorithmName.Name" /> that is empty.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="source" /> does not support reading.</para>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   <paramref name="hashAlgorithm"/> specifies a hash algorithm not supported by the current platform.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <paramref name="hashAlgorithm"/> specifies an unknown hash algorithm.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        ///   <paramref name="cancellationToken"/> has been canceled.
        /// </exception>
        public static ValueTask<int> HashDataAsync(
            HashAlgorithmName hashAlgorithm,
            Stream source,
            Memory<byte> destination,
            CancellationToken cancellationToken = default)
        {
            int hashSizeInBytes = CheckHashAndGetLength(hashAlgorithm);
            CheckStream(source);
            Debug.Assert(hashAlgorithm.Name is not null);
            CheckDestinationSize(hashSizeInBytes, destination.Length);

            return LiteHashProvider.HashStreamAsync(
                hashAlgorithm.Name,
                source,
                destination,
                cancellationToken);
        }

        /// <summary>
        /// Asynchronously computes the hash of a stream.
        /// </summary>
        /// <param name="hashAlgorithm">The algorithm used to compute the hash.</param>
        /// <param name="source">The stream to hash.</param>
        /// <param name="cancellationToken">
        ///   The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
        /// </param>
        /// <returns>The hash of the data.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <para><paramref name="source" /> is <see langword="null" />.</para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="hashAlgorithm"/> has a <see cref="HashAlgorithmName.Name" /> that is
        ///     <see langword="null" />.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para><paramref name="hashAlgorithm"/> has a <see cref="HashAlgorithmName.Name" /> that is empty.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="source" /> does not support reading.</para>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   <paramref name="hashAlgorithm"/> specifies a hash algorithm not supported by the current platform.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <paramref name="hashAlgorithm"/> specifies an unknown hash algorithm.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        ///   <paramref name="cancellationToken"/> has been canceled.
        /// </exception>
        public static ValueTask<byte[]> HashDataAsync(
            HashAlgorithmName hashAlgorithm,
            Stream source,
            CancellationToken cancellationToken = default)
        {
            CheckHashAndGetLength(hashAlgorithm);
            CheckStream(source);
            Debug.Assert(hashAlgorithm.Name is not null);

            return LiteHashProvider.HashStreamAsync(hashAlgorithm.Name, source, cancellationToken);
        }

        /// <summary>
        /// Computes the HMAC of data.
        /// </summary>
        /// <param name="hashAlgorithm">The algorithm used to compute the HMAC.</param>
        /// <param name="key">The secret key. The key can be any length.</param>
        /// <param name="source">The data to compute the HMAC over.</param>
        /// <returns>The HMAC of the data.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <para><paramref name="key" /> or <paramref name="source" /> is <see langword="null" />.</para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="hashAlgorithm"/> has a <see cref="HashAlgorithmName.Name" /> that is
        ///     <see langword="null" />.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="hashAlgorithm"/> has a <see cref="HashAlgorithmName.Name" /> that is empty.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   <paramref name="hashAlgorithm"/> specifies a hash algorithm not supported by the current platform.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <paramref name="hashAlgorithm"/> specifies an unknown hash algorithm.
        /// </exception>
        public static byte[] HmacData(HashAlgorithmName hashAlgorithm, byte[] key, byte[] source)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(source);
            return HmacData(hashAlgorithm, new ReadOnlySpan<byte>(key), new ReadOnlySpan<byte>(source));
        }

        /// <summary>
        /// Computes the HMAC of data.
        /// </summary>
        /// <param name="hashAlgorithm">The algorithm used to compute the HMAC.</param>
        /// <param name="key">The secret key. The key can be any length.</param>
        /// <param name="source">The data to compute the HMAC over.</param>
        /// <returns>The HMAC of the data.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="hashAlgorithm"/> has a <see cref="HashAlgorithmName.Name" /> that is
        ///   <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="hashAlgorithm"/> has a <see cref="HashAlgorithmName.Name" /> that is empty.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   <paramref name="hashAlgorithm"/> specifies a hash algorithm not supported by the current platform.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <paramref name="hashAlgorithm"/> specifies an unknown hash algorithm.
        /// </exception>
        public static byte[] HmacData(HashAlgorithmName hashAlgorithm, ReadOnlySpan<byte> key, ReadOnlySpan<byte> source)
        {
            int hashSizeInBytes = CheckHashAndGetLength(hashAlgorithm);
            Debug.Assert(hashAlgorithm.Name is not null);
            byte[] buffer = new byte[hashSizeInBytes];

            int written = HashProviderDispenser.OneShotHashProvider.MacData(hashAlgorithm.Name, key, source, buffer);
            Debug.Assert(written == hashSizeInBytes);
            return buffer;
        }

        /// <summary>
        /// Computes the HMAC of data.
        /// </summary>
        /// <param name="hashAlgorithm">The algorithm used to compute the HMAC.</param>
        /// <param name="key">The secret key. The key can be any length.</param>
        /// <param name="source">The data to compute the HMAC over.</param>
        /// <param name="destination">The buffer to receive the HMAC value.</param>
        /// <returns>The total number of bytes written to <paramref name="destination" />.</returns>
        /// <exception cref="ArgumentException">
        ///   <para>The buffer in <paramref name="destination"/> is too small to hold the calculated hash size.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="hashAlgorithm"/> has a <see cref="HashAlgorithmName.Name" /> that is empty.</para>
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="hashAlgorithm"/> has a <see cref="HashAlgorithmName.Name" /> that is
        ///   <see langword="null" />.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   <paramref name="hashAlgorithm"/> specifies a hash algorithm not supported by the current platform.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <paramref name="hashAlgorithm"/> specifies an unknown hash algorithm.
        /// </exception>
        public static int HmacData(
            HashAlgorithmName hashAlgorithm,
            ReadOnlySpan<byte> key,
            ReadOnlySpan<byte> source,
            Span<byte> destination)
        {
            if (!TryHmacData(hashAlgorithm, key, source, destination, out int bytesWritten))
            {
                throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));
            }

            return bytesWritten;
        }

        /// <summary>
        /// Attempts to compute the HMAC of data.
        /// </summary>
        /// <param name="hashAlgorithm">The algorithm used to compute the HMAC.</param>
        /// <param name="key">The secret key. The key can be any length.</param>
        /// <param name="source">The data to compute the HMAC over.</param>
        /// <param name="destination">The buffer to receive the HMAC value.</param>
        /// <param name="bytesWritten">
        ///   When this method returns, the total number of bytes written into <paramref name="destination"/>.
        /// </param>
        /// <returns>
        ///   <see langword="false"/> if <paramref name="destination"/> is too small to hold the
        ///   calculated HMAC, <see langword="true"/> otherwise.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="hashAlgorithm"/> has a <see cref="HashAlgorithmName.Name" /> that is empty.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="hashAlgorithm"/> has a <see cref="HashAlgorithmName.Name" /> that is
        ///   <see langword="null" />.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   <paramref name="hashAlgorithm"/> specifies a hash algorithm not supported by the current platform.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <paramref name="hashAlgorithm"/> specifies an unknown hash algorithm.
        /// </exception>
        public static bool TryHmacData(
            HashAlgorithmName hashAlgorithm,
            ReadOnlySpan<byte> key,
            ReadOnlySpan<byte> source,
            Span<byte> destination,
            out int bytesWritten)
        {
            int hashSizeInBytes = CheckHashAndGetLength(hashAlgorithm);
            Debug.Assert(hashAlgorithm.Name is not null);

            if (destination.Length < hashSizeInBytes)
            {
                bytesWritten = 0;
                return false;
            }

            bytesWritten = HashProviderDispenser.OneShotHashProvider.MacData(hashAlgorithm.Name, key, source, destination);
            Debug.Assert(bytesWritten == hashSizeInBytes);
            return true;
        }

        /// <summary>
        /// Computes the HMAC of a stream.
        /// </summary>
        /// <param name="hashAlgorithm">The algorithm used to compute the HMAC.</param>
        /// <param name="key">The secret key. The key can be any length.</param>
        /// <param name="source">The data to compute the HMAC over.</param>
        /// <returns>The HMAC of the data.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <para><paramref name="key" /> or <paramref name="source" /> is <see langword="null" />.</para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="hashAlgorithm"/> has a <see cref="HashAlgorithmName.Name" /> that is
        ///     <see langword="null" />.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para><paramref name="hashAlgorithm"/> has a <see cref="HashAlgorithmName.Name" /> that is empty.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="source" /> does not support reading.</para>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   <paramref name="hashAlgorithm"/> specifies a hash algorithm not supported by the current platform.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <paramref name="hashAlgorithm"/> specifies an unknown hash algorithm.
        /// </exception>
        public static byte[] HmacData(HashAlgorithmName hashAlgorithm, byte[] key, Stream source)
        {
            ArgumentNullException.ThrowIfNull(key);
            return HmacData(hashAlgorithm, new ReadOnlySpan<byte>(key), source);
        }

        /// <summary>
        /// Computes the HMAC of a stream.
        /// </summary>
        /// <param name="hashAlgorithm">The algorithm used to compute the HMAC.</param>
        /// <param name="key">The secret key. The key can be any length.</param>
        /// <param name="source">The data to compute the HMAC over.</param>
        /// <returns>The HMAC of the data.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <para><paramref name="source" /> is <see langword="null" />.</para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="hashAlgorithm"/> has a <see cref="HashAlgorithmName.Name" /> that is
        ///     <see langword="null" />.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para><paramref name="hashAlgorithm"/> has a <see cref="HashAlgorithmName.Name" /> that is empty.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="source" /> does not support reading.</para>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   <paramref name="hashAlgorithm"/> specifies a hash algorithm not supported by the current platform.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <paramref name="hashAlgorithm"/> specifies an unknown hash algorithm.
        /// </exception>
        public static byte[] HmacData(HashAlgorithmName hashAlgorithm, ReadOnlySpan<byte> key, Stream source)
        {
            int hashSizeInBytes = CheckHashAndGetLength(hashAlgorithm);
            CheckStream(source);
            Debug.Assert(hashAlgorithm.Name is not null);

            return LiteHashProvider.HmacStream(hashAlgorithm.Name, hashSizeInBytes, key, source);
        }

        /// <summary>
        /// Computes the HMAC of a stream.
        /// </summary>
        /// <param name="hashAlgorithm">The algorithm used to compute the HMAC.</param>
        /// <param name="key">The secret key. The key can be any length.</param>
        /// <param name="source">The data to compute the HMAC over.</param>
        /// <param name="destination">The buffer to receive the HMAC value.</param>
        /// <returns>The total number of bytes written to <paramref name="destination" />.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <para><paramref name="source" /> is <see langword="null" />.</para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="hashAlgorithm"/> has a <see cref="HashAlgorithmName.Name" /> that is
        ///     <see langword="null" />.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para><paramref name="hashAlgorithm"/> has a <see cref="HashAlgorithmName.Name" /> that is empty.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="source" /> does not support reading.</para>
        ///   <para>-or-</para>
        ///   <para>The buffer in <paramref name="destination"/> is too small to hold the calculated HMAC size.</para>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   <paramref name="hashAlgorithm"/> specifies a hash algorithm not supported by the current platform.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <paramref name="hashAlgorithm"/> specifies an unknown hash algorithm.
        /// </exception>
        public static int HmacData(HashAlgorithmName hashAlgorithm, ReadOnlySpan<byte> key, Stream source, Span<byte> destination)
        {
            int hashSizeInBytes = CheckHashAndGetLength(hashAlgorithm);
            CheckStream(source);
            Debug.Assert(hashAlgorithm.Name is not null);
            CheckDestinationSize(hashSizeInBytes, destination.Length);

            return LiteHashProvider.HmacStream(hashAlgorithm.Name, key, source, destination);
        }

        /// <summary>
        /// Asynchronously computes the HMAC of a stream.
        /// </summary>
        /// <param name="hashAlgorithm">The algorithm used to compute the HMAC.</param>
        /// <param name="key">The secret key. The key can be any length.</param>
        /// <param name="source">The stream to compute the HMAC over.</param>
        /// <param name="cancellationToken">
        ///   The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
        /// </param>
        /// <returns>The HMAC of the data.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <para><paramref name="key" /> or <paramref name="source" /> is <see langword="null" />.</para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="hashAlgorithm"/> has a <see cref="HashAlgorithmName.Name" /> that is
        ///     <see langword="null" />.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para><paramref name="hashAlgorithm"/> has a <see cref="HashAlgorithmName.Name" /> that is empty.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="source" /> does not support reading.</para>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   <paramref name="hashAlgorithm"/> specifies a hash algorithm not supported by the current platform.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <paramref name="hashAlgorithm"/> specifies an unknown hash algorithm.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        ///   <paramref name="cancellationToken"/> has been canceled.
        /// </exception>
        public static ValueTask<byte[]> HmacDataAsync(
            HashAlgorithmName hashAlgorithm,
            byte[] key,
            Stream source,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(key);
            return HmacDataAsync(hashAlgorithm, new ReadOnlyMemory<byte>(key), source, cancellationToken);
        }

        /// <summary>
        /// Asynchronously computes the HMAC of a stream.
        /// </summary>
        /// <param name="hashAlgorithm">The algorithm used to compute the HMAC.</param>
        /// <param name="key">The secret key. The key can be any length.</param>
        /// <param name="source">The stream to compute the HMAC over.</param>
        /// <param name="destination">The buffer to receive the HMAC value.</param>
        /// <param name="cancellationToken">
        ///   The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
        /// </param>
        /// <returns>The total number of bytes written to <paramref name="destination" />.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <para><paramref name="source" /> is <see langword="null" />.</para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="hashAlgorithm"/> has a <see cref="HashAlgorithmName.Name" /> that is
        ///     <see langword="null" />.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>The buffer in <paramref name="destination"/> is too small to hold the calculated HMAC size.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="hashAlgorithm"/> has a <see cref="HashAlgorithmName.Name" /> that is empty.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="source" /> does not support reading.</para>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   <paramref name="hashAlgorithm"/> specifies a hash algorithm not supported by the current platform.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <paramref name="hashAlgorithm"/> specifies an unknown hash algorithm.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        ///   <paramref name="cancellationToken"/> has been canceled.
        /// </exception>
        public static ValueTask<int> HmacDataAsync(
            HashAlgorithmName hashAlgorithm,
            ReadOnlyMemory<byte> key,
            Stream source,
            Memory<byte> destination,
            CancellationToken cancellationToken = default)
        {
            int hashSizeInBytes = CheckHashAndGetLength(hashAlgorithm);
            CheckStream(source);
            Debug.Assert(hashAlgorithm.Name is not null);
            CheckDestinationSize(hashSizeInBytes, destination.Length);

            return LiteHashProvider.HmacStreamAsync(hashAlgorithm.Name, key.Span, source, destination, cancellationToken);
        }

        /// <summary>
        /// Asynchronously computes the HMAC of a stream.
        /// </summary>
        /// <param name="hashAlgorithm">The algorithm used to compute the HMAC.</param>
        /// <param name="key">The secret key. The key can be any length.</param>
        /// <param name="source">The stream to compute the HMAC over.</param>
        /// <param name="cancellationToken">
        ///   The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
        /// </param>
        /// <returns>The HMAC of the data.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <para><paramref name="source" /> is <see langword="null" />.</para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="hashAlgorithm"/> has a <see cref="HashAlgorithmName.Name" /> that is
        ///     <see langword="null" />.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para><paramref name="hashAlgorithm"/> has a <see cref="HashAlgorithmName.Name" /> that is empty.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="source" /> does not support reading.</para>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   <paramref name="hashAlgorithm"/> specifies a hash algorithm not supported by the current platform.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <paramref name="hashAlgorithm"/> specifies an unknown hash algorithm.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        ///   <paramref name="cancellationToken"/> has been canceled.
        /// </exception>
        public static ValueTask<byte[]> HmacDataAsync(
            HashAlgorithmName hashAlgorithm,
            ReadOnlyMemory<byte> key,
            Stream source,
            CancellationToken cancellationToken = default)
        {
            CheckHashAndGetLength(hashAlgorithm);
            CheckStream(source);
            Debug.Assert(hashAlgorithm.Name is not null);

            return LiteHashProvider.HmacStreamAsync(hashAlgorithm.Name, key.Span, source, cancellationToken);
        }

        private static void CheckStream([NotNull] Stream source)
        {
            ArgumentNullException.ThrowIfNull(source);

            if (!source.CanRead)
            {
                throw new ArgumentException(SR.Argument_StreamNotReadable, nameof(source));
            }
        }

        // Currently this is accurate for HMAC algorithms, too, so we can re-use it.
        // If there is ever an HMAC / hash algorithm that diverge, that needs to be handled separately.
        private static int CheckHashAndGetLength(HashAlgorithmName hashAlgorithm)
        {
            ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, nameof(hashAlgorithm));

            switch (hashAlgorithm.Name)
            {
                case HashAlgorithmNames.SHA256:
                    return SHA256.HashSizeInBytes;
                case HashAlgorithmNames.SHA1:
                    return SHA1.HashSizeInBytes;
                case HashAlgorithmNames.SHA512:
                    return SHA512.HashSizeInBytes;
                case HashAlgorithmNames.SHA384:
                    return SHA384.HashSizeInBytes;
                case HashAlgorithmNames.SHA3_256:
                    if (!HashProviderDispenser.HashSupported(HashAlgorithmNames.SHA3_256))
                    {
                        throw new PlatformNotSupportedException();
                    }

                    return SHA3_256.HashSizeInBytes;
                case HashAlgorithmNames.SHA3_384:
                    if (!HashProviderDispenser.HashSupported(HashAlgorithmNames.SHA3_384))
                    {
                        throw new PlatformNotSupportedException();
                    }

                    return SHA3_384.HashSizeInBytes;
                case HashAlgorithmNames.SHA3_512:
                    if (!HashProviderDispenser.HashSupported(HashAlgorithmNames.SHA3_512))
                    {
                        throw new PlatformNotSupportedException();
                    }

                    return SHA3_512.HashSizeInBytes;
                case HashAlgorithmNames.MD5 when Helpers.HasMD5:
                    return MD5.HashSizeInBytes;
                default:
                    throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithm.Name));
            }
        }

        private static void CheckDestinationSize(int requiredSize, int destinationSize)
        {
            if (destinationSize < requiredSize)
            {
                throw new ArgumentException(SR.Argument_DestinationTooShort, "destination");
            }
        }
    }
}
