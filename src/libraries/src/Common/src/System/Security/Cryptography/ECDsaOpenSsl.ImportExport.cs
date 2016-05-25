// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;

namespace System.Security.Cryptography
{
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
    internal static partial class ECDsaImplementation
    {
#endif
        public sealed partial class ECDsaOpenSsl : ECDsa
        {
            /// <summary>
            ///         ImportParameters will replace the existing key that ECDsaOpenSsl is working with by creating a
            ///         new key. If the parameters contains only Q, then only a public key will be imported.
            ///         If the parameters also contains D, then a full key pair will be imported. 
            ///         The parameters Curve value specifies the type of the curve to import.
            /// </summary>
            /// <param name="parameters">The curve parameters.</param>
            /// <exception cref="CryptographicException">
            ///     if <paramref name="parameters" /> does not contain valid values.
            /// </exception>
            /// <exception cref="NotSupportedException">
            ///     if <paramref name="parameters" /> references a curve that cannot be imported.
            /// </exception>
            /// <exception cref="PlatformNotSupportedException">
            ///     if <paramref name="parameters" /> references a curve that is not supported by this platform.
            /// </exception>
            public override void ImportParameters(ECParameters parameters)
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
                    throw new PlatformNotSupportedException(string.Format(SR.Cryptography_CurveNotSupported, parameters.Curve.CurveType.ToString()));
                }

                if (key == null || key.IsInvalid)
                    throw Interop.Crypto.CreateOpenSslCryptographicException();

                SetKey(key);
            }

            /// <summary>
            ///     Exports the key and explicit curve parameters used by the ECC object into an <see cref="ECParameters"/> object.
            /// </summary>
            /// <exception cref="CryptographicException">
            ///     if there was an issue obtaining the curve values.
            /// </exception>
            /// <returns>The key and explicit curve parameters used by the ECC object.</returns>
            public override ECParameters ExportExplicitParameters(bool includePrivateParameters)
            {
                // It's entirely possible that this line will cause the key to be generated in the first place.
                SafeEcKeyHandle currentKey = _key.Value;

                ECParameters ecparams = ExportExplicitCurveParameters(currentKey, includePrivateParameters);
                return ecparams;
            }

            /// <summary>
            ///     Exports the key used by the ECC object into an <see cref="ECParameters"/> object.
            ///     If the curve has a name, the Curve property will contain named curve parameters otherwise it will contain explicit parameters.
            /// </summary>
            /// <exception cref="CryptographicException">
            ///     if there was an issue obtaining the curve values.
            /// </exception>
            /// <returns>The key and named curve parameters used by the ECC object.</returns>
            public override ECParameters ExportParameters(bool includePrivateParameters)
            {
                // It's entirely possible that this line will cause the key to be generated in the first place.
                SafeEcKeyHandle currentKey = _key.Value;

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
                    parameters.Curve.Oid.Value : parameters.Curve.Oid.FriendlyName;

                SafeEcKeyHandle key = Interop.Crypto.EcKeyCreateByKeyParameters(
                    oid,
                    parameters.Q.X, parameters.Q.X.Length,
                    parameters.Q.Y, parameters.Q.Y.Length,
                    parameters.D, parameters.D == null ? 0 : parameters.D.Length);

                return key;
            }

            private static SafeEcKeyHandle ImportPrimeCurveParameters(ECParameters parameters)
            {
                Debug.Assert(parameters.Curve.IsPrime);
                SafeEcKeyHandle key = Interop.Crypto.EcKeyCreateByExplicitParameters(
                    parameters.Curve.CurveType,
                    parameters.Q.X, parameters.Q.X.Length,
                    parameters.Q.Y, parameters.Q.Y.Length,
                    parameters.D, parameters.D == null ? 0 : parameters.D.Length,
                    parameters.Curve.Prime, parameters.Curve.Prime.Length,
                    parameters.Curve.A, parameters.Curve.A.Length,
                    parameters.Curve.B, parameters.Curve.B.Length,
                    parameters.Curve.G.X, parameters.Curve.G.X.Length,
                    parameters.Curve.G.Y, parameters.Curve.G.Y.Length,
                    parameters.Curve.Order, parameters.Curve.Order.Length,
                    parameters.Curve.Cofactor, parameters.Curve.Cofactor.Length,
                    parameters.Curve.Seed, parameters.Curve.Seed == null ? 0 : parameters.Curve.Seed.Length);

                return key;
            }

            private static SafeEcKeyHandle ImportCharacteristic2CurveParameters(ECParameters parameters)
            {
                Debug.Assert(parameters.Curve.IsCharacteristic2);
                SafeEcKeyHandle key = Interop.Crypto.EcKeyCreateByExplicitParameters(
                    parameters.Curve.CurveType,
                    parameters.Q.X, parameters.Q.X.Length,
                    parameters.Q.Y, parameters.Q.Y.Length,
                    parameters.D, parameters.D == null ? 0 : parameters.D.Length,
                    parameters.Curve.Polynomial, parameters.Curve.Polynomial.Length,
                    parameters.Curve.A, parameters.Curve.A.Length,
                    parameters.Curve.B, parameters.Curve.B.Length,
                    parameters.Curve.G.X, parameters.Curve.G.X.Length,
                    parameters.Curve.G.Y, parameters.Curve.G.Y.Length,
                    parameters.Curve.Order, parameters.Curve.Order.Length,
                    parameters.Curve.Cofactor, parameters.Curve.Cofactor.Length,
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
        }
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
    }
#endif
}
