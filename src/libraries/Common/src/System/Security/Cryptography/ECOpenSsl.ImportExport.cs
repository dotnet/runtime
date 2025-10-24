// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    internal sealed partial class ECOpenSsl
    {
        internal const string ECDSA_P256_OID_VALUE = "1.2.840.10045.3.1.7"; // Also called nistP256 or secP256r1
        internal const string ECDSA_P384_OID_VALUE = "1.3.132.0.34"; // Also called nistP384 or secP384r1
        internal const string ECDSA_P521_OID_VALUE = "1.3.132.0.35"; // Also called nistP521or secP521r1

        public int ImportParameters(ECParameters parameters)
        {
            SafeEcKeyHandle key;

            parameters.Validate();

            if (parameters.Curve.IsPrime)
            {
                key = ImportPrimeCurveParameters(parameters);
            }
            else if (parameters.Curve.IsCharacteristic2)
            {
                key = ImportCharacteristic2CurveParameters(parameters);
            }
            else if (parameters.Curve.IsNamed)
            {
                key = ImportNamedCurveParameters(parameters);
            }
            else
            {
                throw new PlatformNotSupportedException(
                    SR.Format(SR.Cryptography_CurveNotSupported, parameters.Curve.CurveType.ToString()));
            }

            if (key == null || key.IsInvalid)
            {
                Exception e = Interop.Crypto.CreateOpenSslCryptographicException();
                key?.Dispose();
                throw e;
            }

            // The Import* methods above may have polluted the error queue even if in the end they succeeded.
            // Clean up the error queue.
            Interop.Crypto.ErrClearError();

            FreeKey();
            _key = new Lazy<SafeEcKeyHandle>(key);
            return KeySize;
        }

        public static ECParameters ExportExplicitParameters(SafeEcKeyHandle currentKey, bool includePrivateParameters) =>
            ExportExplicitCurveParameters(currentKey, includePrivateParameters);

        public static ECParameters ExportParameters(SafeEcKeyHandle currentKey, bool includePrivateParameters)
        {
            ECParameters ecparams;
            if (Interop.Crypto.EcKeyHasCurveName(currentKey))
            {
                ecparams = ExportNamedCurveParameters(currentKey, includePrivateParameters);
            }
            else
            {
                ecparams = ExportExplicitCurveParameters(currentKey, includePrivateParameters);
            }
            return ecparams;
        }

        private static ECParameters ExportNamedCurveParameters(SafeEcKeyHandle key, bool includePrivateParameters)
        {
            CheckInvalidKey(key);

            ECParameters parameters = Interop.Crypto.GetECKeyParameters(key, includePrivateParameters);

            bool hasPrivateKey = (parameters.D != null);

            if (hasPrivateKey != includePrivateParameters)
            {
                throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
            }

            // Assign Curve
            string keyOidValueName = Interop.Crypto.EcKeyGetCurveName(key);
            parameters.Curve = ECCurve.CreateFromValue(keyOidValueName);

            return parameters;
        }

        private static ECParameters ExportExplicitCurveParameters(SafeEcKeyHandle key, bool includePrivateParameters)
        {
            CheckInvalidKey(key);

            ECParameters parameters = Interop.Crypto.GetECCurveParameters(key, includePrivateParameters);

            bool hasPrivateKey = (parameters.D != null);
            if (hasPrivateKey != includePrivateParameters)
            {
                throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
            }

            return parameters;
        }

        private static SafeEcKeyHandle ImportNamedCurveParameters(ECParameters parameters)
        {
            Debug.Assert(parameters.Curve.IsNamed);

            // Use oid Value first if present, otherwise FriendlyName
            string oid = !string.IsNullOrEmpty(parameters.Curve.Oid.Value) ?
                parameters.Curve.Oid.Value : parameters.Curve.Oid.FriendlyName!;

            SafeEcKeyHandle key = Interop.Crypto.EcKeyCreateByKeyParameters(
                oid,
                parameters.Q.X, parameters.Q.X?.Length ?? 0,
                parameters.Q.Y, parameters.Q.Y?.Length ?? 0,
                parameters.D, parameters.D == null ? 0 : parameters.D.Length);

            return key;
        }

        private static SafeEcKeyHandle ImportPrimeCurveParameters(ECParameters parameters)
        {
            Debug.Assert(parameters.Curve.IsPrime);
            SafeEcKeyHandle key = Interop.Crypto.EcKeyCreateByExplicitParameters(
                parameters.Curve.CurveType,
                parameters.Q.X, parameters.Q.X?.Length ?? 0,
                parameters.Q.Y, parameters.Q.Y?.Length ?? 0,
                parameters.D, parameters.D == null ? 0 : parameters.D.Length,
                parameters.Curve.Prime!, parameters.Curve.Prime!.Length,
                parameters.Curve.A!, parameters.Curve.A!.Length,
                parameters.Curve.B!, parameters.Curve.B!.Length,
                parameters.Curve.G.X!, parameters.Curve.G.X!.Length,
                parameters.Curve.G.Y!, parameters.Curve.G.Y!.Length,
                parameters.Curve.Order!, parameters.Curve.Order!.Length,
                parameters.Curve.Cofactor, parameters.Curve.Cofactor!.Length,
                parameters.Curve.Seed, parameters.Curve.Seed == null ? 0 : parameters.Curve.Seed.Length);

            return key;
        }

        private static SafeEcKeyHandle ImportCharacteristic2CurveParameters(ECParameters parameters)
        {
            Debug.Assert(parameters.Curve.IsCharacteristic2);
            SafeEcKeyHandle key = Interop.Crypto.EcKeyCreateByExplicitParameters(
                parameters.Curve.CurveType,
                parameters.Q.X, parameters.Q.X?.Length ?? 0,
                parameters.Q.Y, parameters.Q.Y?.Length ?? 0,
                parameters.D, parameters.D == null ? 0 : parameters.D.Length,
                parameters.Curve.Polynomial!, parameters.Curve.Polynomial!.Length,
                parameters.Curve.A!, parameters.Curve.A!.Length,
                parameters.Curve.B!, parameters.Curve.B!.Length,
                parameters.Curve.G.X!, parameters.Curve.G.X!.Length,
                parameters.Curve.G.Y!, parameters.Curve.G.Y!.Length,
                parameters.Curve.Order!, parameters.Curve.Order!.Length,
                parameters.Curve.Cofactor, parameters.Curve.Cofactor!.Length,
                parameters.Curve.Seed, parameters.Curve.Seed == null ? 0 : parameters.Curve.Seed.Length);

            return key;
        }

        private static void CheckInvalidKey(SafeEcKeyHandle key)
        {
            if (key == null || key.IsInvalid)
            {
                throw new CryptographicException(SR.Cryptography_OpenInvalidHandle);
            }
        }

        private static void CheckInvalidKey(SafeEvpPKeyHandle key)
        {
            if (key == null || key.IsInvalid)
            {
                throw new CryptographicException(SR.Cryptography_OpenInvalidHandle);
            }
        }

        public static ECParameters ExportParameters(SafeEvpPKeyHandle pkey, bool includePrivateParameters)
        {
            CheckInvalidKey(pkey);

            using (SafeEcKeyHandle ecKey = Interop.Crypto.EvpPkeyGetEcKey(pkey))
            {
                if (ecKey == null || ecKey.IsInvalid)
                {
                    // This may happen when EVP_PKEY was created by provider and getting EC_KEY is not possible.
                    return ExportECParametersFromEvpPKeyUsingParams(pkey, includePrivateParameters);
                }

                return ECOpenSsl.ExportParameters(ecKey, includePrivateParameters);
            }
        }

        public static ECParameters ExportExplicitParameters(SafeEvpPKeyHandle pkey, bool includePrivateParameters)
        {
            CheckInvalidKey(pkey);

            using (SafeEcKeyHandle ecKey = Interop.Crypto.EvpPkeyGetEcKey(pkey))
            {
                if (ecKey == null || ecKey.IsInvalid)
                {
                    // This may happen when EVP_PKEY was created by provider and getting EC_KEY is not possible.
                    return ExportExplicitCurveParametersFromEvpPKeyUsingParams(pkey, includePrivateParameters);
                }

                return ECOpenSsl.ExportExplicitParameters(ecKey, includePrivateParameters);
            }
        }

        /// <summary>
        /// Extracts ECParameters from an EVP_PKEY* using OpenSSL 3 params API.
        /// This is needed in case EVP_PKEY* was created by provider and getting EC_KEY is not possible.
        /// For keys created with EC_KEY, ECOpenSsl.ExportParameters should be used.
        /// </summary>
        private static ECParameters ExportECParametersFromEvpPKeyUsingParams(SafeEvpPKeyHandle pkey, bool includePrivateParameters)
        {
            string? curveName = Interop.Crypto.EvpPKeyGetCurveName(pkey);
            if (curveName == null)
            {
                return ExportExplicitCurveParametersFromEvpPKeyUsingParams(pkey, includePrivateParameters);
            }
            else
            {
                return ExportNamedCurveParametersFromEvpPKeyUsingParams(pkey, curveName, includePrivateParameters);
            }
        }

        private static ECParameters ExportNamedCurveParametersFromEvpPKeyUsingParams(SafeEvpPKeyHandle pkey, string curveName, bool includePrivateParameters)
        {
            Debug.Assert(curveName != null);
            ECParameters parameters = Interop.Crypto.EvpPKeyGetEcKeyParameters(pkey, includePrivateParameters);

            bool hasPrivateKey = (parameters.D != null);

            if (hasPrivateKey != includePrivateParameters)
            {
                throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
            }

            // Assign Curve
            parameters.Curve = ECCurve.CreateFromValue(curveName);

            return parameters;
        }

        private static ECParameters ExportExplicitCurveParametersFromEvpPKeyUsingParams(SafeEvpPKeyHandle pkey, bool includePrivateParameters)
        {
            ECParameters parameters = Interop.Crypto.EvpPKeyGetEcCurveParameters(pkey, includePrivateParameters);

            bool hasPrivateKey = (parameters.D != null);
            if (hasPrivateKey != includePrivateParameters)
            {
                throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
            }

            return parameters;
        }

        public static SafeEcKeyHandle GenerateKeyByKeySize(int keySize)
        {
            string oid;
            switch (keySize)
            {
                case 256: oid = ECDSA_P256_OID_VALUE; break;
                case 384: oid = ECDSA_P384_OID_VALUE; break;
                case 521: oid = ECDSA_P521_OID_VALUE; break;
                default:
                    // Only above three sizes supported for backwards compatibility; named curves should be used instead
                    throw new InvalidOperationException(SR.Cryptography_InvalidKeySize);
            }

            SafeEcKeyHandle? key = Interop.Crypto.EcKeyCreateByOid(oid);

            if (key == null || key.IsInvalid)
            {
                key?.Dispose();
                throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_CurveNotSupported, oid));
            }

            if (!Interop.Crypto.EcKeyGenerateKey(key))
            {
                key.Dispose();
                throw Interop.Crypto.CreateOpenSslCryptographicException();
            }

            return key;
        }
    }
}
