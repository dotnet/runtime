// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    /// <summary>
    /// Provides support for computing a hash or HMAC value incrementally across several segments.
    /// </summary>
    public sealed class IncrementalHash : IDisposable
    {
        private const int NTE_BAD_ALGID = unchecked((int)0x80090008);

        private readonly HashAlgorithmName _algorithmName;
        private HashAlgorithm _hash;
        private bool _disposed;
        private bool _resetPending;

        private IncrementalHash(HashAlgorithmName name, HashAlgorithm hash)
        {
            Debug.Assert(name != null);
            Debug.Assert(!string.IsNullOrEmpty(name.Name));
            Debug.Assert(hash != null);

            _algorithmName = name;
            _hash = hash;
        }
        
        /// <summary>
        /// Get the name of the algorithm being performed.
        /// </summary>
        public HashAlgorithmName AlgorithmName
        {
            get { return _algorithmName; }
        }

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

            Debug.Assert(_hash != null);

            if (_resetPending)
            {
                _hash.Initialize();
                _resetPending = false;
            }

            _hash.TransformBlock(data, offset, count, null, 0);
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
                throw new ObjectDisposedException(typeof(IncrementalHash).Name);

            Debug.Assert(_hash != null);

            if (_resetPending)
            {
                // No point in setting _resetPending to false, we're about to set it to true.
                _hash.Initialize();
            }

            _hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

            byte[] hashValue = _hash.Hash;
            _resetPending = true;

            return hashValue;
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

            return new IncrementalHash(hashAlgorithm, GetHashAlgorithm(hashAlgorithm));
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

            return new IncrementalHash(hashAlgorithm, GetHMAC(hashAlgorithm, key));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5351", Justification = "MD5 is used when the user asks for it.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350", Justification = "SHA1 is used when the user asks for it.")]
        private static HashAlgorithm GetHashAlgorithm(HashAlgorithmName hashAlgorithm)
        {
            if (hashAlgorithm == HashAlgorithmName.MD5)
                return new MD5CryptoServiceProvider();
            if (hashAlgorithm == HashAlgorithmName.SHA1)
                return new SHA1CryptoServiceProvider();
            if (hashAlgorithm == HashAlgorithmName.SHA256)
                return new SHA256CryptoServiceProvider();
            if (hashAlgorithm == HashAlgorithmName.SHA384)
                return new SHA384CryptoServiceProvider();
            if (hashAlgorithm == HashAlgorithmName.SHA512)
                return new SHA512CryptoServiceProvider();

            throw new CryptographicException(NTE_BAD_ALGID);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5351", Justification = "MD5 is used when the user asks for it.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350", Justification = "SHA1 is used when the user asks for it.")]
        private static HashAlgorithm GetHMAC(HashAlgorithmName hashAlgorithm, byte[] key)
        {
            if (hashAlgorithm == HashAlgorithmName.MD5)
                return new HMACMD5(key);
            if (hashAlgorithm == HashAlgorithmName.SHA1)
                return new HMACSHA1(key);
            if (hashAlgorithm == HashAlgorithmName.SHA256)
                return new HMACSHA256(key);
            if (hashAlgorithm == HashAlgorithmName.SHA384)
                return new HMACSHA384(key);
            if (hashAlgorithm == HashAlgorithmName.SHA512)
                return new HMACSHA512(key);

            throw new CryptographicException(NTE_BAD_ALGID);
        }
    }
}
