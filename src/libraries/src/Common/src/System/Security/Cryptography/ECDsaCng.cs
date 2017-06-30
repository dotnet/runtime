// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using static Internal.NativeCrypto.BCryptNative;

namespace System.Security.Cryptography
{
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
    internal static partial class ECDsaImplementation
    {
#endif
        public sealed partial class ECDsaCng : ECDsa
        {
            /// <summary>
            /// Create an ECDsaCng algorithm with a named curve.
            /// </summary>
            /// <param name="curve">The <see cref="ECCurve"/> representing the curve.</param>
            /// <exception cref="ArgumentNullException">if <paramref name="curve" /> is null.</exception>
            /// <exception cref="PlatformNotSupportedException">if <paramref name="curve" /> does not contain an Oid with a FriendlyName.</exception>
            public ECDsaCng(ECCurve curve)
            {
                // Specified curves generate the key immediately
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

            /// <summary>
            /// Is the curve named, or once of the special nist curves
            /// </summary>
            internal static bool IsECNamedCurve(string algorithm)
            {
                return (algorithm == AlgorithmName.ECDH ||
                    algorithm == AlgorithmName.ECDsa);
            }

            /// <summary>
            /// Maps algorithm to curve name accounting for the special nist curves
            /// </summary>
            internal static string SpecialNistAlgorithmToCurveName(string algorithm)
            {
                if (algorithm == AlgorithmName.ECDHP256 ||
                    algorithm == AlgorithmName.ECDsaP256)
                {
                    return "nistP256";
                }

                if (algorithm == AlgorithmName.ECDHP384 ||
                    algorithm == AlgorithmName.ECDsaP384)
                {
                    return "nistP384";
                }

                if (algorithm == AlgorithmName.ECDHP521 ||
                    algorithm == AlgorithmName.ECDsaP521)
                {
                    return "nistP521";
                }

                Debug.Fail(string.Format("Unknown curve {0}", algorithm));
                throw new PlatformNotSupportedException(string.Format(SR.Cryptography_CurveNotSupported, algorithm));
            }
        }
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
    }
#endif
}
