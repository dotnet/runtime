// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    /// <summary>
    /// Provides support for computing a hash or HMAC value incrementally across several segments.
    /// </summary>
    public sealed class IncrementalHash : IDisposable
    {
        private readonly HashAlgorithmName _algorithmName;
        private HashProvider _hash;
        private HMACCommon _hmac;
        private bool _disposed;

        private IncrementalHash(HashAlgorithmName name, HashProvider hash)
        {
            Debug.Assert(name != null);
            Debug.Assert(!string.IsNullOrEmpty(name.Name));
            Debug.Assert(hash != null);

            _algorithmName = name;
            _hash = hash;
        }

        private IncrementalHash(HashAlgorithmName name, HMACCommon hmac)
        {
            Debug.Assert(name != null);
            Debug.Assert(!string.IsNullOrEmpty(name.Name));
            Debug.Assert(hmac != null);

            _algorithmName = new HashAlgorithmName("HMAC" + name.Name);
            _hmac = hmac;
        }
        
        /// <summary>
        /// Get the name of the algorithm being performed.
        /// </summary>
        public HashAlgorithmName AlgorithmName => _algorithmName;

        /// <summary>
        /// Append the entire contents of <paramref name="data"/> to the data already processed in the hash or HMAC.
        /// </summary>
        /// <param name="data">The data to process.</param>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> is <c>null</c>.</exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        public void AppendData(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            AppendData(data, 0, data.Length);
        }

        /// <summary>
        /// Append <paramref name="count"/> bytes of <paramref name="data"/>, starting at <paramref name="offset"/>,
        /// to the data already processed in the hash or HMAC.
        /// </summary>
        /// <param name="data">The data to process.</param>
        /// <param name="offset">The offset into the byte array from which to begin using data.</param>
        /// <param name="count">The number of bytes in the array to use as data.</param>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="offset"/> is out of range. This parameter requires a non-negative number.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="count"/> is out of range. This parameter requires a non-negative number less than
        ///     the <see cref="Array.Length"/> value of <paramref name="data"/>.
        ///     </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="count"/> is greater than
        ///     <paramref name="data"/>.<see cref="Array.Length"/> - <paramref name="offset"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        public void AppendData(byte[] data, int offset, int count)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (count < 0 || (count > data.Length))
                throw new ArgumentOutOfRangeException(nameof(count));
            if ((data.Length - count) < offset)
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            if (_disposed)
                throw new ObjectDisposedException(typeof(IncrementalHash).Name);

            AppendData(new ReadOnlySpan<byte>(data, offset, count));
        }

        public void AppendData(ReadOnlySpan<byte> data)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(typeof(IncrementalHash).Name);
            }

            Debug.Assert((_hash != null) ^ (_hmac != null));
            if (_hash != null)
            {
                _hash.AppendHashData(data);
            }
            else
            {
                _hmac.AppendHashData(data);
            }
        }

        /// <summary>
        /// Retrieve the hash or HMAC for the data accumulated from prior calls to
        /// <see cref="AppendData(byte[])"/>, and return to the state the object
        /// was in at construction.
        /// </summary>
        /// <returns>The computed hash or HMAC.</returns>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        public byte[] GetHashAndReset()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(typeof(IncrementalHash).Name);
            }

            Debug.Assert((_hash != null) ^ (_hmac != null));
            return _hash != null ?
                _hash.FinalizeHashAndReset() :
                _hmac.FinalizeHashAndReset();
        }

        public bool TryGetHashAndReset(Span<byte> destination, out int bytesWritten)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(typeof(IncrementalHash).Name);
            }

            Debug.Assert((_hash != null) ^ (_hmac != null));
            return _hash != null ?
                _hash.TryFinalizeHashAndReset(destination, out bytesWritten) :
                _hmac.TryFinalizeHashAndReset(destination, out bytesWritten);
        }

        /// <summary>
        /// Release all resources used by the current instance of the
        /// <see cref="IncrementalHash"/> class.
        /// </summary>
        public void Dispose()
        {
            _disposed = true;

            if (_hash != null)
            {
                _hash.Dispose();
                _hash = null;
            }

            if (_hmac != null)
            {
                _hmac.Dispose(true);
                _hmac = null;
            }
        }

        /// <summary>
        /// Create an <see cref="IncrementalHash"/> for the algorithm specified by <paramref name="hashAlgorithm"/>.
        /// </summary>
        /// <param name="hashAlgorithm">The name of the hash algorithm to perform.</param>
        /// <returns>
        /// An <see cref="IncrementalHash"/> instance ready to compute the hash algorithm specified
        /// by <paramref name="hashAlgorithm"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///     <paramref name="hashAlgorithm"/>.<see cref="HashAlgorithmName.Name"/> is <c>null</c>, or
        ///     the empty string.
        /// </exception>
        /// <exception cref="CryptographicException"><paramref name="hashAlgorithm"/> is not a known hash algorithm.</exception>
        public static IncrementalHash CreateHash(HashAlgorithmName hashAlgorithm)
        {
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw new ArgumentException(SR.Cryptography_HashAlgorithmNameNullOrEmpty, nameof(hashAlgorithm));

            return new IncrementalHash(hashAlgorithm, HashProviderDispenser.CreateHashProvider(hashAlgorithm.Name));
        }

        /// <summary>
        /// Create an <see cref="IncrementalHash"/> for the Hash-based Message Authentication Code (HMAC)
        /// algorithm utilizing the hash algorithm specified by <paramref name="hashAlgorithm"/>, and a
        /// key specified by <paramref name="key"/>.
        /// </summary>
        /// <param name="hashAlgorithm">The name of the hash algorithm to perform within the HMAC.</param>
        /// <param name="key">
        ///     The secret key for the HMAC. The key can be any length, but a key longer than the output size
        ///     of the hash algorithm specified by <paramref name="hashAlgorithm"/> will be hashed (using the
        ///     algorithm specified by <paramref name="hashAlgorithm"/>) to derive a correctly-sized key. Therefore,
        ///     the recommended size of the secret key is the output size of the hash specified by
        ///     <paramref name="hashAlgorithm"/>.
        /// </param>
        /// <returns>
        /// An <see cref="IncrementalHash"/> instance ready to compute the hash algorithm specified
        /// by <paramref name="hashAlgorithm"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///     <paramref name="hashAlgorithm"/>.<see cref="HashAlgorithmName.Name"/> is <c>null</c>, or
        ///     the empty string.
        /// </exception>
        /// <exception cref="CryptographicException"><paramref name="hashAlgorithm"/> is not a known hash algorithm.</exception>
        public static IncrementalHash CreateHMAC(HashAlgorithmName hashAlgorithm, byte[] key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw new ArgumentException(SR.Cryptography_HashAlgorithmNameNullOrEmpty, nameof(hashAlgorithm));

            return new IncrementalHash(hashAlgorithm, new HMACCommon(hashAlgorithm.Name, key, -1));
        }
    }
}
