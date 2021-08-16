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

            SafeEcKeyHandle key = Interop.AndroidCrypto.EcKeyCreateByKeyParameters(
                oid,
                parameters.Q.X, parameters.Q.X?.Length ?? 0,
                parameters.Q.Y, parameters.Q.Y?.Length ?? 0,
                parameters.D, parameters.D == null ? 0 : parameters.D.Length);

            return key;
        }

        private static SafeEcKeyHandle ImportPrimeCurveParameters(ECParameters parameters)
        {
            Debug.Assert(parameters.Curve.IsPrime);
            SafeEcKeyHandle key = Interop.AndroidCrypto.EcKeyCreateByExplicitParameters(
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
            SafeEcKeyHandle key = Interop.AndroidCrypto.EcKeyCreateByExplicitParameters(
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
