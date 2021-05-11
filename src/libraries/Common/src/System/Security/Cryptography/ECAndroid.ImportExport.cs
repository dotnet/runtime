// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;

namespace System.Security.Cryptography
{
    internal sealed partial class ECAndroid
    {
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
                throw new CryptographicException();
            }

            FreeKey();
            _key = new Lazy<SafeEcKeyHandle>(key);
            return KeySize;
        }

        public static ECParameters ExportExplicitParameters(SafeEcKeyHandle currentKey, bool includePrivateParameters) =>
            ExportExplicitCurveParameters(currentKey, includePrivateParameters);

        public static ECParameters ExportParameters(SafeEcKeyHandle currentKey, bool includePrivateParameters)
        {
            ECParameters ecparams;
            string? curveName = Interop.AndroidCrypto.EcKeyGetCurveName(currentKey);
            if (curveName is not null)
            {
                ecparams = ExportNamedCurveParameters(currentKey, curveName, includePrivateParameters);
            }
            else
            {
                ecparams = ExportExplicitCurveParameters(currentKey, includePrivateParameters);
            }
            return ecparams;
        }

        private static ECParameters ExportNamedCurveParameters(SafeEcKeyHandle key, string curveName, bool includePrivateParameters)
        {
            CheckInvalidKey(key);

            ECParameters parameters = Interop.AndroidCrypto.GetECKeyParameters(key, includePrivateParameters);

            bool hasPrivateKey = (parameters.D != null);

            if (hasPrivateKey != includePrivateParameters)
            {
                throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
            }

            // Assign Curve
            parameters.Curve = ECCurve.CreateFromFriendlyName(curveName);

            return parameters;
        }

        private static ECParameters ExportExplicitCurveParameters(SafeEcKeyHandle key, bool includePrivateParameters)
        {
            CheckInvalidKey(key);

            ECParameters parameters = Interop.AndroidCrypto.GetECCurveParameters(key, includePrivateParameters);

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

            var androidParameters = new Interop.AndroidCrypto.AndroidECKeyArrayParameters
            {
                qx = parameters.Q.X,
                qy = parameters.Q.Y,
                d = parameters.D,
                qx_length = parameters.Q.X?.Length ?? 0,
                qy_length = parameters.Q.Y?.Length ?? 0,
                d_length = parameters.D == null ? 0 : parameters.D.Length,
            };

            SafeEcKeyHandle key = Interop.AndroidCrypto.EcKeyCreateByKeyParameters(oid, ref androidParameters);

            return key;
        }

        private static SafeEcKeyHandle ImportCurveParametersCommon (ECParameters parameters, byte[]? p, int pLength)
        {
            var androidParameters = new Interop.AndroidCrypto.AndroidECKeyExplicitParameters
            {
                qx = parameters.Q.X, qx_length = parameters.Q.X?.Length ?? 0,
                qy = parameters.Q.Y, qy_length = parameters.Q.Y?.Length ?? 0,
                d = parameters.D, d_length = parameters.D == null ? 0 : parameters.D.Length,
                p = p, p_length = pLength,
                a = parameters.Curve.A!, a_length = parameters.Curve.A!.Length,
                b = parameters.Curve.B!, b_length = parameters.Curve.B!.Length,
                gx = parameters.Curve.G.X!, gx_length = parameters.Curve.G.X!.Length,
                gy = parameters.Curve.G.Y!, gy_length = parameters.Curve.G.Y!.Length,
                order = parameters.Curve.Order!, order_length = parameters.Curve.Order!.Length,
                cofactor = parameters.Curve.Cofactor, cofactor_length = parameters.Curve.Cofactor!.Length,
                seed = parameters.Curve.Seed, seed_length = parameters.Curve.Seed == null ? 0 : parameters.Curve.Seed.Length
            };

            SafeEcKeyHandle key = Interop.AndroidCrypto.EcKeyCreateByExplicitParameters(parameters.Curve.CurveType, ref androidParameters);

            return key;
        }

        private static SafeEcKeyHandle ImportPrimeCurveParameters(ECParameters parameters)
        {
            if (!parameters.Curve.IsPrime)
                throw new ArgumentException ("Curve must be Prime", nameof(parameters));
            return ImportCurveParametersCommon (parameters, parameters.Curve.Prime!, parameters.Curve.Prime!.Length);
        }

        private static SafeEcKeyHandle ImportCharacteristic2CurveParameters(ECParameters parameters)
        {
            if (!parameters.Curve.IsCharacteristic2)
                throw new ArgumentException ("Curve must be Characteristic2", nameof(parameters));
            return ImportCurveParametersCommon (parameters, parameters.Curve.Polynomial!, parameters.Curve.Polynomial!.Length);
        }

        private static void CheckInvalidKey(SafeEcKeyHandle key)
        {
            if (key == null || key.IsInvalid)
            {
                throw new CryptographicException(SR.Cryptography_OpenInvalidHandle);
            }
        }

        public static SafeEcKeyHandle GenerateKeyByKeySize(int keySize)
        {
            string oid;
            switch (keySize)
            {
                case 256: oid = Oids.secp256r1; break;
                case 384: oid = Oids.secp384r1; break;
                case 521: oid = Oids.secp521r1; break;
                default:
                    // Only above three sizes supported for backwards compatibility; named curves should be used instead
                    throw new InvalidOperationException(SR.Cryptography_InvalidKeySize);
            }

            SafeEcKeyHandle? key = Interop.AndroidCrypto.EcKeyCreateByOid(oid);

            if (key == null || key.IsInvalid)
                throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_CurveNotSupported, oid));

            return key;
        }
    }
}
