// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    internal static partial class ECOpenSsl
    {
        internal const string ECDSA_P256_OID_VALUE = "1.2.840.10045.3.1.7"; // Also called nistP256 or secP256r1
        internal const string ECDSA_P384_OID_VALUE = "1.3.132.0.34"; // Also called nistP384 or secP384r1
        internal const string ECDSA_P521_OID_VALUE = "1.3.132.0.35"; // Also called nistP521or secP521r1

        private static ECParameters ExportExplicitParameters(SafeEcKeyHandle currentKey, bool includePrivateParameters) =>
            ExportExplicitCurveParameters(currentKey, includePrivateParameters);

        private static ECParameters ExportParameters(SafeEcKeyHandle currentKey, bool includePrivateParameters)
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

            if (SafeEvpPKeyHandle.OpenSslVersion >= 0x3_00_00_00_0)
            {
                return ExportECParametersFromEvpPKeyUsingParams(pkey, includePrivateParameters);
            }

            using (SafeEcKeyHandle ecKey = Interop.Crypto.EvpPkeyGetEcKey(pkey))
            {
                return ECOpenSsl.ExportParameters(ecKey, includePrivateParameters);
            }
        }

        public static ECParameters ExportExplicitParameters(SafeEvpPKeyHandle pkey, bool includePrivateParameters)
        {
            CheckInvalidKey(pkey);

            if (SafeEvpPKeyHandle.OpenSslVersion >= 0x3_00_00_00_0)
            {
                return ExportExplicitCurveParametersFromEvpPKeyUsingParams(pkey, includePrivateParameters);
            }

            using (SafeEcKeyHandle ecKey = Interop.Crypto.EvpPkeyGetEcKey(pkey))
            {
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
            // Check encoding first — explicit-encoding keys must be exported with
            // explicit curve parameters even if OpenSSL can match a named curve.
            if (Interop.Crypto.EvpPKeyEcHasExplicitEncoding(pkey))
            {
                return ExportExplicitCurveParametersFromEvpPKeyUsingParams(pkey, includePrivateParameters);
            }

            string? curveName = Interop.Crypto.EvpPKeyGetCurveName(pkey);

            if (curveName is null)
            {
                return ExportExplicitCurveParametersFromEvpPKeyUsingParams(pkey, includePrivateParameters);
            }

            return ExportNamedCurveParametersFromEvpPKeyUsingParams(pkey, curveName, includePrivateParameters);
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
    }
}
