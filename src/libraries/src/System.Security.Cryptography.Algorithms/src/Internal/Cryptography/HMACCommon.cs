// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        public HMACCommon(String hashAlgorithmId, byte[] key)
        {
            _hashAlgorithmId = hashAlgorithmId;
            ChangeKey(key);
        }

        public int HashSizeInBits
        {
            get
            {
                return _hashProvider.HashSizeInBytes * 8;
            }
        }

        public void ChangeKey(byte[] key)
        {
            HashProvider oldHashProvider = _hashProvider;
            _hashProvider = null;
            if (oldHashProvider != null)
                oldHashProvider.Dispose(true);
            _hashProvider = HashProviderDispenser.CreateMacProvider(_hashAlgorithmId, key);
        }

        // Adds new data to be hashed. This can be called repeatedly in order to hash data from incontiguous sources.
        public void AppendHashData(byte[] data, int offset, int count)
        {
            _hashProvider.AppendHashData(data, offset, count);
        }

        // Compute the hash based on the appended data and resets the HashProvider for more hashing.
        public byte[] FinalizeHashAndReset()
        {
            return _hashProvider.FinalizeHashAndReset();
        }

        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_hashProvider != null)
                    _hashProvider.Dispose(true);
                _hashProvider = null;
            }
        }

        private readonly String _hashAlgorithmId;
        private HashProvider _hashProvider;
    }
}
