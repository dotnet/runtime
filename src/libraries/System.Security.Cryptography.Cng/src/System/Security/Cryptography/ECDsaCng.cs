// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

using Internal.Cryptography;

namespace System.Security.Cryptography
{
    public sealed partial class ECDsaCng : ECDsa
    {
        /// <summary>
        ///     Create an ECDsaCng algorithm with a random 521 bit key pair.
        /// </summary>
        public ECDsaCng()
            : this(521)
        {
        }

        /// <summary>
        ///     Creates a new ECDsaCng object that will use a randomly generated key of the specified size.
        /// </summary>
        /// <param name="keySize">Size of the key to generate, in bits.</param>
        /// <exception cref="CryptographicException">if <paramref name="keySize" /> is not valid</exception>
        public ECDsaCng(int keySize)
        {
            KeySize = keySize;
        }

        /// <summary>
        ///     Creates a new ECDsaCng object that will use the specified key. The key's
        ///     <see cref="CngKey.AlgorithmGroup" /> must be ECDsa. This constructor
        ///     creates a copy of the key. Hence, the caller can safely dispose of the 
        ///     passed in key and continue using the ECDsaCng object. 
        /// </summary>
        /// <param name="key">Key to use for ECDsa operations</param>
        /// <exception cref="ArgumentException">if <paramref name="key" /> is not an ECDsa key</exception>
        /// <exception cref="ArgumentNullException">if <paramref name="key" /> is null.</exception>
        public ECDsaCng(CngKey key)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            if (key.AlgorithmGroup != CngAlgorithmGroup.ECDsa)
                throw new ArgumentException(SR.Cryptography_ArgECDsaRequiresECDsaKey, "key");

            Key = CngAsymmetricAlgorithmCore.Duplicate(key);
        }

        public override KeySizes[] LegalKeySizes
        {
            get
            {
                return (KeySizes[])(s_legalKeySizes.Clone());
            }
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
                new KeySizes(minSize: 256, maxSize: 384, skipSize: 128),
                new KeySizes(minSize: 521, maxSize: 521, skipSize: 0),
            };
    }
}

