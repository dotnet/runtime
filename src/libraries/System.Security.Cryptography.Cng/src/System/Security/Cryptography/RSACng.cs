// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

using Internal.Cryptography;

namespace System.Security.Cryptography
{
    public sealed partial class RSACng : RSA
    {
        /// <summary>
        ///     Create an RSACng algorithm with a random 2048 bit key pair.
        /// </summary>
        public RSACng()
            : this(2048)
        {
        }

        /// <summary>
        ///     Creates a new RSACng object that will use a randomly generated key of the specified size.
        ///     Valid key sizes range from 512 to 16384 bits, in increments of 64 bits. It is suggested that a
        ///     minimum size of 2048 bits be used for all keys.
        /// </summary>
        /// <param name="keySize">Size of the key to generate, in bits.</param>
        /// <exception cref="CryptographicException">if <paramref name="keySize" /> is not valid</exception>
        public RSACng(int keySize)
        {
            _legalKeySizesValue = s_legalKeySizes;
            KeySize = keySize;
        }

        /// <summary>
        ///     Creates a new RSACng object that will use the specified key. The key's
        ///     <see cref="CngKey.AlgorithmGroup" /> must be Rsa. This constructor
        ///     creates a copy of the key. Hence, the caller can safely dispose of the 
        ///     passed in key and continue using the RSACng object. 
        /// </summary>
        /// <param name="key">Key to use for RSA operations</param>
        /// <exception cref="ArgumentException">if <paramref name="key" /> is not an RSA key</exception>
        /// <exception cref="ArgumentNullException">if <paramref name="key" /> is null.</exception>
        public RSACng(CngKey key)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            if (key.AlgorithmGroup != CngAlgorithmGroup.Rsa)
                throw new ArgumentException(SR.Cryptography_ArgRSAaRequiresRSAKey, "key");

            _legalKeySizesValue = s_legalKeySizes;
            Key = CngAsymmetricAlgorithmCore.Duplicate(key);
        }

        protected override void Dispose(bool disposing)
        {
            _core.Dispose();
        }

        private CngAsymmetricAlgorithmCore _core;

        // See https://msdn.microsoft.com/en-us/library/windows/desktop/bb931354(v=vs.85).aspx
        private static readonly KeySizes[] s_legalKeySizes = 
            new KeySizes[] 
            {
                // All values are in bits.
                new KeySizes(minSize: 512, maxSize: 16384, skipSize: 64), 
            }; 
    }
}

