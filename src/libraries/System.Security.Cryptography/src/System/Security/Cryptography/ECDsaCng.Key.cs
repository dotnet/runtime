// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    public sealed partial class ECDsaCng : ECDsa
    {
        /// <summary>
        ///     Gets the key that will be used by the ECDsa object for any cryptographic operation that it uses.
        ///     This key object will be disposed if the key is reset, for instance by changing the KeySize
        ///     property, using ImportParamers to create a new key, or by Disposing of the parent ECDsa object.
        ///     Therefore, you should make sure that the key object is no longer used in these scenarios. This
        ///     object will not be the same object as the CngKey passed to the ECDsaCng constructor if that
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
                ArgumentNullException.ThrowIfNull(value);

                if (!IsEccAlgorithmGroup(value.AlgorithmGroup))
                    throw new ArgumentException(SR.Cryptography_ArgECDsaRequiresECDsaKey, nameof(value));
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
                CngAlgorithm alg = CngKey.EcdsaCurveNameToAlgorithm(curve.Oid.FriendlyName);
                if (CngKey.IsECNamedCurve(alg.Algorithm))
                {
                    CngKey key = _core.GetOrGenerateKey(curve);
                    ForceSetKeySize(key.KeySize);
                }
                else
                {
                    int keySize;
                    // Get the proper KeySize from algorithm name
                    if (alg == CngAlgorithm.ECDsaP256)
                        keySize = 256;
                    else if (alg == CngAlgorithm.ECDsaP384)
                        keySize = 384;
                    else if (alg == CngAlgorithm.ECDsaP521)
                        keySize = 521;
                    else
                    {
                        Debug.Fail($"Unknown algorithm {alg}");
                        throw new ArgumentException(SR.Cryptography_InvalidKeySize);
                    }
                    _core.GetOrGenerateKey(keySize, alg);
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
            CngKey key;

            if (_core.IsKeyGeneratedNamedCurve())
            {
                // Curve was previously created, so use that
                key = _core.GetOrGenerateKey(null);
            }
            else
            {
                CngAlgorithm algorithm;

                // Map the current key size to a CNG algorithm name
                int keySize = KeySize;
                switch (keySize)
                {
                    case 256: algorithm = CngAlgorithm.ECDsaP256; break;
                    case 384: algorithm = CngAlgorithm.ECDsaP384; break;
                    case 521: algorithm = CngAlgorithm.ECDsaP521; break;
                    default:
                        Debug.Fail("Should not have invalid key size");
                        throw new ArgumentException(SR.Cryptography_InvalidKeySize);
                }
                key = _core.GetOrGenerateKey(keySize, algorithm);
            }

            return key;
        }

        private SafeNCryptKeyHandle GetDuplicatedKeyHandle()
        {
            return Key.Handle;
        }
    }
}
