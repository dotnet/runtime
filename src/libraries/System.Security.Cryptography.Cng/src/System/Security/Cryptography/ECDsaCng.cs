// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Cryptography;
using System.Diagnostics;

namespace System.Security.Cryptography
{
    public sealed partial class ECDsaCng : ECDsa
    {
        private CngAlgorithmCore _core;
        private bool _skipKeySizeCheck;

        /// <summary>
        /// Create an ECDsaCng algorithm with a named curve.
        /// </summary>
        /// <param name="curve">The <see cref="ECCurve"/> representing the curve.</param>
        /// <exception cref="ArgumentNullException">if <paramref name="curve" /> is null.</exception>
        /// <exception cref="PlatformNotSupportedException">if <paramref name="curve" /> does not contain an Oid with a FriendlyName.</exception>
        public ECDsaCng(ECCurve curve)
        {
            // FriendlyName is required; an attempt was already made to default it in ECCurve
            if (string.IsNullOrEmpty(curve.Oid.FriendlyName))
                throw new PlatformNotSupportedException(string.Format(SR.Cryptography_InvalidCurveOid, curve.Oid.Value.ToString()));

            // Named curves generate the key immediately
            GenerateKey(curve);
        }

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

        public override int KeySize
        {
            get
            {
                return base.KeySize;
            }
            set
            {
                if (KeySize == value)
                {
                    return;
                }

                base.KeySize = value;

                // Key will be lazily re-created
            }
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
                throw new ArgumentNullException(nameof(key));

            if (!IsEccAlgorithmGroup(key.AlgorithmGroup))
                throw new ArgumentException(SR.Cryptography_ArgECDsaRequiresECDsaKey, nameof(key));

            Key = CngAlgorithmCore.Duplicate(key);
        }

        /// <summary>
        /// Set the KeySize without validating against LegalKeySizes.
        /// </summary>
        /// <param name="newKeySize">The value to set the KeySize to.</param>
        private void ForceSetKeySize(int newKeySize)
        {
            // In the event that a key was loaded via ImportParameters, curve name, or an IntPtr/SafeHandle
            // it could be outside of the bounds that we currently represent as "legal key sizes".
            // Since that is our view into the underlying component it can be detached from the
            // component's understanding.  If it said it has opened a key, and this is the size, trust it.
            _skipKeySizeCheck = true;

            try
            {
                // Set base.KeySize directly, since we don't want to free the key
                // (which we would do if the keysize changed on import)
                base.KeySize = newKeySize;
            }
            finally
            {
                _skipKeySizeCheck = false;
            }
        }

        public override KeySizes[] LegalKeySizes
        {
            get
            {
                if (_skipKeySizeCheck)
                {
                    // When size limitations are in bypass, accept any positive integer.
                    // Many of them may not make sense (like 1), but we're just assigning
                    // the field to whatever value was provided by the native component.
                    return new[] { new KeySizes(minSize: 1, maxSize: int.MaxValue, skipSize: 1) };
                }

                // Return the three sizes that can be explicitly set (for backwards compatibility)
                return new[] {
                    new KeySizes(minSize: 256, maxSize: 384, skipSize: 128),
                    new KeySizes(minSize: 521, maxSize: 521, skipSize: 0),
                };
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
    }
}
