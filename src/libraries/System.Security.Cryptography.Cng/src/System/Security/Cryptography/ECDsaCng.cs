// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

            if (!IsEccAlgorithmGroup(key.AlgorithmGroup))
                throw new ArgumentException(SR.Cryptography_ArgECDsaRequiresECDsaKey, "key");

            Key = CngAlgorithmCore.Duplicate(key);
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

        private static bool IsEccAlgorithmGroup(CngAlgorithmGroup algorithmGroup)
        {
            // Sometimes, when reading from certificates, ECDSA keys get identified as ECDH.
            // Windows allows the ECDH keys to perform both key exchange (ECDH) and signing (ECDSA),
            // so either value is acceptable for the ECDSA wrapper object.
            //
            // It is worth noting, however, that ECDSA-identified keys cannot be used for key exchange (ECDH) in CNG.
            return algorithmGroup == CngAlgorithmGroup.ECDsa || algorithmGroup == CngAlgorithmGroup.ECDiffieHellman;
        }

        private CngAlgorithmCore _core;

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

