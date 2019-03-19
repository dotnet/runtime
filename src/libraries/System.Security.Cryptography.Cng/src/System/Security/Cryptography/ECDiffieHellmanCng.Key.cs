// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    public sealed partial class ECDiffieHellmanCng : ECDiffieHellman
    {
        /// <summary>
        ///     Public key used to generate key material with the second party
        /// </summary>
        public override ECDiffieHellmanPublicKey PublicKey
        {
            get
            {
                return ECDiffieHellmanCngPublicKey.FromKey(Key);
            }
        }

        /// <summary>
        ///     Gets the key that will be used by the ECDH object for any cryptographic operation that it uses.
        ///     This key object will be disposed if the key is reset, for instance by changing the KeySize
        ///     property, using ImportParamers to create a new key, or by Disposing of the parent ECDH object.
        ///     Therefore, you should make sure that the key object is no longer used in these scenarios. This
        ///     object will not be the same object as the CngKey passed to the ECDHCng constructor if that
        ///     constructor was used, however it will point at the same CNG key.
        /// </summary>
        public CngKey Key
        {
            get
            {
                return GetKey();
            }

            private set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                if (value.AlgorithmGroup != CngAlgorithmGroup.ECDiffieHellman)
                    throw new ArgumentException(SR.Cryptography_ArgECDHRequiresECDHKey, nameof(value));

                _core.SetKey(value);

                // LegalKeySizes stores the values for either the current named curve or for the three 
                // curves that use size instead of name
                ForceSetKeySize(value.KeySize);
            }
        }

        public override void GenerateKey(ECCurve curve)
        {
            curve.Validate();
            _core.DisposeKey();

            if (curve.IsNamed)
            {
                if (string.IsNullOrEmpty(curve.Oid.FriendlyName))
                    throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_InvalidCurveOid, curve.Oid.Value));

                // Map curve name to algorithm to support pre-Win10 curves
                CngAlgorithm alg = CngKey.EcdhCurveNameToAlgorithm(curve.Oid.FriendlyName);
                if (CngKey.IsECNamedCurve(alg.Algorithm))
                {
                    CngKey key = _core.GetOrGenerateKey(curve);
                    ForceSetKeySize(key.KeySize);
                }
                else
                {
                    int keySize = 0;
                    // Get the proper KeySize from algorithm name
                    if (alg == CngAlgorithm.ECDiffieHellmanP256)
                        keySize = 256;
                    else if (alg == CngAlgorithm.ECDiffieHellmanP384)
                        keySize = 384;
                    else if (alg == CngAlgorithm.ECDiffieHellmanP521)
                        keySize = 521;
                    else
                    {
                        Debug.Fail($"Unknown algorithm {alg}");
                        throw new ArgumentException(SR.Cryptography_InvalidKeySize);
                    }
                    CngKey key = _core.GetOrGenerateKey(keySize, alg);
                    ForceSetKeySize(keySize);
                }
            }
            else if (curve.IsExplicit)
            {
                CngKey key = _core.GetOrGenerateKey(curve);
                ForceSetKeySize(key.KeySize);
            }
            else
            {
                throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_CurveNotSupported, curve.CurveType.ToString()));
            }
        }

        private CngKey GetKey()
        {
            CngKey key = null;

            if (_core.IsKeyGeneratedNamedCurve())
            {
                // Curve was previously created, so use that
                key = _core.GetOrGenerateKey(null);
            }
            else
            {
                CngAlgorithm algorithm = null;
                int keySize = 0;

                // Map the current key size to a CNG algorithm name
                keySize = KeySize;
                switch (keySize)
                {
                    case 256:
                        algorithm = CngAlgorithm.ECDiffieHellmanP256;
                        break;
                    case 384:
                        algorithm = CngAlgorithm.ECDiffieHellmanP384;
                        break;
                    case 521:
                        algorithm = CngAlgorithm.ECDiffieHellmanP521;
                        break;
                    default:
                        Debug.Fail("Should not have invalid key size");
                        throw new ArgumentException(SR.Cryptography_InvalidKeySize);
                }
                key = _core.GetOrGenerateKey(keySize, algorithm);
            }

            return key;
        }
    }
}
