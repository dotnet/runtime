// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    internal static partial class ECOpenSsl
    {
        internal const string ECDSA_P256_OID_VALUE = "1.2.840.10045.3.1.7"; // Also called nistP256 or secP256r1
        internal const string ECDSA_P384_OID_VALUE = "1.3.132.0.34"; // Also called nistP384 or secP384r1
        internal const string ECDSA_P521_OID_VALUE = "1.3.132.0.35"; // Also called nistP521or secP521r1

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
            return ExportECParametersFromEvpPKey(pkey, includePrivateParameters);
        }

        public static ECParameters ExportExplicitParameters(SafeEvpPKeyHandle pkey, bool includePrivateParameters)
        {
            CheckInvalidKey(pkey);
            return ExportExplicitCurveParametersFromEvpPKey(pkey, includePrivateParameters);
        }

        private static ECParameters ExportECParametersFromEvpPKey(SafeEvpPKeyHandle pkey, bool includePrivateParameters)
        {
            // Check encoding first — explicit-encoding keys must be exported with
            // explicit curve parameters even if OpenSSL can match a named curve.
            if (Interop.Crypto.EvpPKeyEcHasExplicitEncoding(pkey))
            {
                return ExportExplicitCurveParametersFromEvpPKey(pkey, includePrivateParameters);
            }

            string? curveName = Interop.Crypto.EvpPKeyGetCurveName(pkey);

            if (curveName is null)
            {
                // If encoding is not explicit, there should always be a named curve.
                Debug.Fail("Non-explicit key has no curve name.");
                return ExportExplicitCurveParametersFromEvpPKey(pkey, includePrivateParameters);
            }

            return ExportNamedCurveParametersFromEvpPKey(pkey, curveName, includePrivateParameters);
        }

        private static ECParameters ExportNamedCurveParametersFromEvpPKey(SafeEvpPKeyHandle pkey, string curveName, bool includePrivateParameters)
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

        private static ECParameters ExportExplicitCurveParametersFromEvpPKey(SafeEvpPKeyHandle pkey, bool includePrivateParameters)
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
