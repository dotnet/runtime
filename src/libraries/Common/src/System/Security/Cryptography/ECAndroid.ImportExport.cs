// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    internal sealed partial class ECAndroid
    {
        private static readonly string[] s_validOids = [Oids.EcPublicKey];

        public int ImportParameters(ECParameters parameters)
        {
            parameters.Validate();
            SafeEcKeyHandle key = ImportParametersCore(parameters);

            if (key is null || key.IsInvalid)
            {
                key?.Dispose();
                throw new CryptographicException();
            }

            if (parameters.D is not null && parameters.Q.X is null)
            {
                if (!TryRecoverPublicKey(key, out ECPoint publicKey))
                {
                    key.Dispose();
                    throw new CryptographicException();
                }

                ECParameters completeParameters = parameters;
                completeParameters.Q = publicKey;
                SafeEcKeyHandle? completeKey = null;

                try
                {
                    completeKey = ImportParametersCore(completeParameters);

                    if (completeKey is null || completeKey.IsInvalid)
                    {
                        throw new CryptographicException();
                    }
                }
                catch
                {
                    completeKey?.Dispose();
                    key.Dispose();
                    throw;
                }

                key.Dispose();
                key = completeKey;
            }

            FreeKey();
            _key = new Lazy<SafeEcKeyHandle>(key);
            return KeySize;
        }

        private static SafeEcKeyHandle ImportParametersCore(ECParameters parameters)
        {
            if (parameters.Curve.IsPrime)
            {
                return ImportPrimeCurveParameters(parameters);
            }

            if (parameters.Curve.IsCharacteristic2)
            {
                return ImportCharacteristic2CurveParameters(parameters);
            }

            if (parameters.Curve.IsNamed)
            {
                return ImportNamedCurveParameters(parameters);
            }

            throw new PlatformNotSupportedException(
                SR.Format(SR.Cryptography_CurveNotSupported, parameters.Curve.CurveType.ToString()));
        }

        private static bool TryRecoverPublicKey(SafeEcKeyHandle key, out ECPoint publicKey)
        {
            publicKey = default;

            if (!Interop.AndroidCrypto.TryExportEcKeyPkcs8PrivateKey(key, out ArraySegment<byte> pkcs8))
            {
                return false;
            }

            ECParameters recoveredParameters = default;

            try
            {
                KeyFormatHelper.ReadPkcs8<ECParameters>(
                    s_validOids,
                    pkcs8.AsSpan(),
                    EccKeyFormatHelper.FromECPrivateKey,
                    out int bytesRead,
                    out recoveredParameters);

                if (bytesRead != pkcs8.Count || recoveredParameters.Q.X is null || recoveredParameters.Q.Y is null)
                {
                    return false;
                }

                publicKey = recoveredParameters.Q;
                return true;
            }
            catch (CryptographicException)
            {
                return false;
            }
            finally
            {
                CryptoPool.Return(pkcs8);

                if (recoveredParameters.D is not null)
                {
                    CryptographicOperations.ZeroMemory(recoveredParameters.D);
                }
            }
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
            {
                key?.Dispose();
                throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_CurveNotSupported, oid));
            }

            return key;
        }
    }
}
