// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Security.Cryptography
{
    public sealed partial class ECDsaCng : ECDsa, IRuntimeAlgorithm
    {
        /// <summary>
        /// Create an ECDsaCng algorithm with a named curve.
        /// </summary>
        /// <param name="curve">The <see cref="ECCurve"/> representing the curve.</param>
        /// <exception cref="ArgumentNullException">if <paramref name="curve" /> is null.</exception>
        /// <exception cref="PlatformNotSupportedException">if <paramref name="curve" /> does not contain an Oid with a FriendlyName.</exception>
        [SupportedOSPlatform("windows")]
        public ECDsaCng(ECCurve curve)
        {
            // Specified curves generate the key immediately
            GenerateKey(curve);
        }

        /// <summary>
        ///     Create an ECDsaCng algorithm with a random 521 bit key pair.
        /// </summary>
        [SupportedOSPlatform("windows")]
        public ECDsaCng()
            : this(521)
        {
        }

        /// <summary>
        ///     Creates a new ECDsaCng object that will use a randomly generated key of the specified size.
        /// </summary>
        /// <param name="keySize">Size of the key to generate, in bits.</param>
        /// <exception cref="CryptographicException">if <paramref name="keySize" /> is not valid</exception>
        [SupportedOSPlatform("windows")]
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

                // Set the KeySize before DisposeKey so that an invalid value doesn't throw away the key
                base.KeySize = value;

                DisposeKey();

                // Key will be lazily re-created
            }
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
            KeySizeValue = newKeySize;
        }

        public override KeySizes[] LegalKeySizes
        {
            get
            {
                // Return the three sizes that can be explicitly set (for backwards compatibility)
                return new[] {
                    new KeySizes(minSize: 256, maxSize: 384, skipSize: 128),
                    new KeySizes(minSize: 521, maxSize: 521, skipSize: 0),
                };
            }
        }
    }
}
