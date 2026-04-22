// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    internal sealed partial class ECOpenSsl : IDisposable
    {
        private Lazy<SafeEcKeyHandle> _key = null!; // Always initialized

        public ECOpenSsl(ECCurve curve)
        {
            GenerateKey(curve);
        }

        public ECOpenSsl(int keySizeBits)
        {
            _key = new Lazy<SafeEcKeyHandle>(() => GenerateKeyByKeySize(keySizeBits));
        }

        public ECOpenSsl(ECParameters ecParameters)
        {
            ImportParameters(ecParameters);
        }

        // Takes ownership of the key
        public ECOpenSsl(SafeEcKeyHandle key)
        {
            _key = new Lazy<SafeEcKeyHandle>(key);
        }

        internal SafeEcKeyHandle Value => _key.Value;

        public void Dispose()
        {
            FreeKey();
        }

        internal int KeySize => Interop.Crypto.EcKeyGetSize(_key.Value);

        internal SafeEvpPKeyHandle CreateEvpPKeyHandle()
        {
            SafeEcKeyHandle currentKey = _key.Value;
            Debug.Assert(currentKey != null, "key is null");

            return Interop.Crypto.CreateEvpPkeyFromEcKey(currentKey);
        }

        private void SetKey(SafeEcKeyHandle key)
        {
            Debug.Assert(key != null);
            Debug.Assert(!key.IsInvalid);
            Debug.Assert(!key.IsClosed);

            FreeKey();
            _key = new Lazy<SafeEcKeyHandle>(key);
        }

        internal int GenerateKey(ECCurve curve)
        {
            curve.Validate();
            FreeKey();

            if (curve.IsNamed)
            {
                // Use oid Value first if present, otherwise FriendlyName because Oid maintains a hard-coded
                // cache that may have different casing for FriendlyNames than OpenSsl
                string oid = !string.IsNullOrEmpty(curve.Oid.Value) ? curve.Oid.Value : curve.Oid.FriendlyName!;

                SafeEcKeyHandle? key = Interop.Crypto.EcKeyCreateByOid(oid);

                if (key == null || key.IsInvalid)
                {
                    key?.Dispose();
                    throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_CurveNotSupported, oid));
                }

                if (!Interop.Crypto.EcKeyGenerateKey(key))
                {
                    throw Interop.Crypto.CreateOpenSslCryptographicException();
                }

                SetKey(key);
            }
            else if (curve.IsExplicit)
            {
                SafeEcKeyHandle key = Interop.Crypto.EcKeyCreateByExplicitCurve(curve);

                if (!Interop.Crypto.EcKeyGenerateKey(key))
                    throw Interop.Crypto.CreateOpenSslCryptographicException();

                SetKey(key);
            }
            else
            {
                throw new PlatformNotSupportedException(
                    SR.Format(SR.Cryptography_CurveNotSupported, curve.CurveType.ToString()));
            }

            return KeySize;
        }

        private void FreeKey()
        {
            if (_key != null)
            {
                if (_key.IsValueCreated)
                {
                    _key.Value?.Dispose();
                }

                _key = null!;
            }
        }

        internal static SafeEvpPKeyHandle GenerateECKey(int keySize)
        {
            string oid = keySize switch
            {
                256 => ECOpenSsl.ECDSA_P256_OID_VALUE,
                384 => ECOpenSsl.ECDSA_P384_OID_VALUE,
                521 => ECOpenSsl.ECDSA_P521_OID_VALUE,
                _ => throw new InvalidOperationException(SR.Cryptography_InvalidKeySize),
            };

            SafeEvpPKeyHandle? pkey = Interop.Crypto.EvpPKeyGenerateByEcKeyOid(oid);

            if (pkey is not null)
            {
                return pkey;
            }

            // Fallback to legacy EC_KEY path
            SafeEvpPKeyHandle ret = ImportECKeyCore(new ECOpenSsl(keySize), out int createdKeySize);
            Debug.Assert(keySize == createdKeySize);
            return ret;
        }

        internal static SafeEvpPKeyHandle GenerateECKey(ECCurve curve, out int keySize)
        {
            if (curve.IsNamed)
            {
                string oid = !string.IsNullOrEmpty(curve.Oid.Value) ? curve.Oid.Value : curve.Oid.FriendlyName!;

                SafeEvpPKeyHandle? pkey = Interop.Crypto.EvpPKeyGenerateByEcKeyOid(oid);

                if (pkey is not null)
                {
                    keySize = Interop.Crypto.EvpPKeyGetEcFieldDegree(pkey);
                    return pkey;
                }
            }
            else if (curve.IsPrime || curve.IsCharacteristic2)
            {
                byte[] pField = curve.IsPrime ? curve.Prime! : curve.Polynomial!;

                // Pass null Q and null D to trigger key generation instead of import.
                SafeEvpPKeyHandle? pkey = Interop.Crypto.EvpPKeyCreateByEcExplicitParameters(
                    curve.CurveType,
                    null, 0,
                    null, 0,
                    null, 0,
                    pField, pField.Length,
                    curve.A!, curve.A!.Length,
                    curve.B!, curve.B!.Length,
                    curve.G.X!, curve.G.X!.Length,
                    curve.G.Y!, curve.G.Y!.Length,
                    curve.Order!, curve.Order!.Length,
                    curve.Cofactor!, curve.Cofactor!.Length,
                    curve.Seed, curve.Seed is null ? 0 : curve.Seed.Length);

                if (pkey is not null)
                {
                    keySize = Interop.Crypto.EvpPKeyGetEcFieldDegree(pkey);
                    return pkey;
                }
            }

            // Fallback to legacy EC_KEY path (explicit curves or OpenSSL < 3.0)
            return ImportECKeyCore(new ECOpenSsl(curve), out keySize);
        }

        internal static SafeEvpPKeyHandle ImportECKey(ECParameters parameters, out int keySize)
        {
            parameters.Validate();

            if (parameters.Curve.IsNamed)
            {
                string oid = !string.IsNullOrEmpty(parameters.Curve.Oid.Value) ?
                    parameters.Curve.Oid.Value : parameters.Curve.Oid.FriendlyName!;

                SafeEvpPKeyHandle? pkey = Interop.Crypto.EvpPKeyCreateByEcKeyParameters(
                    oid,
                    parameters.Q.X, parameters.Q.X?.Length ?? 0,
                    parameters.Q.Y, parameters.Q.Y?.Length ?? 0,
                    parameters.D, parameters.D is null ? 0 : parameters.D.Length);

                if (pkey is not null)
                {
                    keySize = Interop.Crypto.EvpPKeyGetEcFieldDegree(pkey);
                    return pkey;
                }
            }
            else if (parameters.Curve.IsPrime || parameters.Curve.IsCharacteristic2)
            {
                byte[] pField = parameters.Curve.IsPrime ? parameters.Curve.Prime! : parameters.Curve.Polynomial!;

                SafeEvpPKeyHandle? pkey = Interop.Crypto.EvpPKeyCreateByEcExplicitParameters(
                    parameters.Curve.CurveType,
                    parameters.Q.X, parameters.Q.X?.Length ?? 0,
                    parameters.Q.Y, parameters.Q.Y?.Length ?? 0,
                    parameters.D, parameters.D is null ? 0 : parameters.D.Length,
                    pField, pField.Length,
                    parameters.Curve.A!, parameters.Curve.A!.Length,
                    parameters.Curve.B!, parameters.Curve.B!.Length,
                    parameters.Curve.G.X!, parameters.Curve.G.X!.Length,
                    parameters.Curve.G.Y!, parameters.Curve.G.Y!.Length,
                    parameters.Curve.Order!, parameters.Curve.Order!.Length,
                    parameters.Curve.Cofactor!, parameters.Curve.Cofactor!.Length,
                    parameters.Curve.Seed, parameters.Curve.Seed is null ? 0 : parameters.Curve.Seed.Length);

                if (pkey is not null)
                {
                    keySize = Interop.Crypto.EvpPKeyGetEcFieldDegree(pkey);
                    return pkey;
                }
            }

            // Fallback to legacy EC_KEY path
            return ImportECKeyCore(new ECOpenSsl(parameters), out keySize);
        }

        // Note: This method takes ownership of ecOpenSsl and disposes it
        private static SafeEvpPKeyHandle ImportECKeyCore(ECOpenSsl ecOpenSsl, out int keySize)
        {
            using (ECOpenSsl ec = ecOpenSsl)
            {
                SafeEvpPKeyHandle handle = Interop.Crypto.CreateEvpPkeyFromEcKey(ec.Value);
                keySize = ec.KeySize;
                return handle;
            }
        }
    }
}
