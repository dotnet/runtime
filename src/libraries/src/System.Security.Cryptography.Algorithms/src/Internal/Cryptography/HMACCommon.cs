// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Security.Cryptography;

using Internal.Cryptography;

namespace Internal.Cryptography
{
    //
    // This class provides the common functionality for HMACSHA1, HMACSHA256, HMACMD5, etc.
    // Ideally, this would be encapsulated in a common base class but the preexisting contract
    // locks these public classes into deriving directly from HMAC so we have to use encapsulation
    // and delegation to HMACCommon instead.
    //
    // This wrapper adds the ability to change the Key on the fly for compat with the desktop.
    //
    internal sealed class HMACCommon
    {
        public HMACCommon(string hashAlgorithmId, byte[] key, int blockSize)
        {
            Debug.Assert(!string.IsNullOrEmpty(hashAlgorithmId));
            Debug.Assert(blockSize > 0 || blockSize == -1);

            _hashAlgorithmId = hashAlgorithmId;
            _blockSize = blockSize;
            ChangeKey(key);
        }

        public int HashSizeInBits
        {
            get
            {
                return _hMacProvider.HashSizeInBytes * 8;
            }
        }

        public void ChangeKey(byte[] key)
        {
            // If _blockSize is -1 the key isn't going to be extractable by the object holder,
            // so there's no point in recalculating it in managed code.
            if (key.Length > _blockSize && _blockSize > 0)
            {
                // Perform RFC 2104, section 2 key adjustment.
                if (_lazyHashProvider == null)
                    _lazyHashProvider = HashProviderDispenser.CreateHashProvider(_hashAlgorithmId);
                _lazyHashProvider.AppendHashData(key, 0, key.Length);
                key = _lazyHashProvider.FinalizeHashAndReset();
            }

            HashProvider oldHashProvider = _hMacProvider;
            _hMacProvider = null;
            if (oldHashProvider != null)
                oldHashProvider.Dispose(true);
            _hMacProvider = HashProviderDispenser.CreateMacProvider(_hashAlgorithmId, key);

            ActualKey = key;
        }

        // The actual key used for hashing. This will not be the same as the original key passed to ChangeKey() if the original key exceeded the
        // hash algorithm's block size. (See RFC 2104, section 2)
        public byte[] ActualKey { get; private set; }

        // Adds new data to be hashed. This can be called repeatedly in order to hash data from noncontiguous sources.
        public void AppendHashData(byte[] data, int offset, int count)
        {
            _hMacProvider.AppendHashData(data, offset, count);
        }

        // Compute the hash based on the appended data and resets the HashProvider for more hashing.
        public byte[] FinalizeHashAndReset()
        {
            return _hMacProvider.FinalizeHashAndReset();
        }

        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_hMacProvider != null)
                    _hMacProvider.Dispose(true);
                _hMacProvider = null;
            }
        }

        private readonly String _hashAlgorithmId;
        private HashProvider _hMacProvider;
        private volatile HashProvider _lazyHashProvider;

        private readonly int _blockSize;
    }
}
