// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography
{
    public sealed partial class ECDsaCng : ECDsa
    {
        /// <summary>
        ///         ImportParameters will replace the existing key that ECDsaCng is working with by creating a
        ///         new CngKey. If the parameters contains only Q, then only a public key will be imported.
        ///         If the parameters also contains D, then a full key pair will be imported. 
        ///         The parameters Curve value specifies the type of the curve to import.
        /// </summary>
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
            parameters.Validate();
            bool includePrivateParameters = (parameters.D != null);
            ECCurve curve = parameters.Curve;
            if (curve.IsPrime)
            {
                Key = ECCng.ImportPrimeCurveParameters(parameters, includePrivateParameters);
            }
            else if (curve.IsNamed)
            {
                // FriendlyName is required; an attempt was already made to default it in ECCurve
                if (string.IsNullOrEmpty(curve.Oid.FriendlyName))
                    throw new PlatformNotSupportedException(string.Format(SR.Cryptography_InvalidCurveOid, curve.Oid.Value.ToString()));

                Key = ECCng.ImportNamedCurveParameters(parameters, includePrivateParameters);
            }
            else
            {
                throw new PlatformNotSupportedException(string.Format(SR.Cryptography_CurveNotSupported, curve.CurveType.ToString()));
            }
        }

        /// <summary>
        ///     Exports the key and explicit curve parameters used by the ECC object into an <see cref="ECParameters"/> object.
        /// </summary>
        /// <exception cref="CryptographicException">
        ///     if there was an issue obtaining the curve values.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///     if explicit export is not supported by this platform. Windows 10 or higher is required.
        /// </exception>
        /// <returns>The key and explicit curve parameters used by the ECC object.</returns>
        public override ECParameters ExportExplicitParameters(bool includePrivateParameters)
        {
            return ECCng.ExportPrimeCurveParameters(Key, includePrivateParameters);
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
            ECParameters ecparams;
            if (string.IsNullOrEmpty(Key.GetCurveName()))
            {
                ecparams = ECCng.ExportPrimeCurveParameters(Key, includePrivateParameters);
            }
            else
            { 
                ecparams = ECCng.ExportNamedCurveParameters(Key, includePrivateParameters);
            }
            return ecparams;
        }
    }
}
