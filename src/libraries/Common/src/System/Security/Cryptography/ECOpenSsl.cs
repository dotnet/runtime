// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    internal static partial class ECOpenSsl
    {
        internal static SafeEvpPKeyHandle GenerateECKey(int keySize)
        {
            string oid = keySize switch
            {
                256 => ECOpenSsl.ECDSA_P256_OID_VALUE,
                384 => ECOpenSsl.ECDSA_P384_OID_VALUE,
                521 => ECOpenSsl.ECDSA_P521_OID_VALUE,
                _ => throw new InvalidOperationException(SR.Cryptography_InvalidKeySize),
            };

            SafeEvpPKeyHandle pkey = Interop.Crypto.EvpPKeyGenerateByEcCurveOid(oid, out int createdKeySize);
            Debug.Assert(keySize == createdKeySize);
            return pkey;
        }

        internal static SafeEvpPKeyHandle GenerateECKey(ECCurve curve, out int keySize)
        {
            curve.Validate();

            if (curve.IsNamed)
            {
                string oid = !string.IsNullOrEmpty(curve.Oid.Value) ? curve.Oid.Value : curve.Oid.FriendlyName!;

                return Interop.Crypto.EvpPKeyGenerateByEcCurveOid(oid, out keySize);
            }
            else if (curve.IsExplicit)
            {
                // Pass null Q and null D to trigger key generation instead of import.
                return Interop.Crypto.EvpPKeyCreateByEcExplicitParameters(
                    curve,
                    null,
                    null,
                    null,
                    out keySize);
            }
            else
            {
                throw new PlatformNotSupportedException(
                    SR.Format(SR.Cryptography_CurveNotSupported, curve.CurveType.ToString()));
            }
        }

        internal static SafeEvpPKeyHandle ImportECKey(ECParameters parameters, out int keySize)
        {
            parameters.Validate();

            if (parameters.Curve.IsNamed)
            {
                string oid = !string.IsNullOrEmpty(parameters.Curve.Oid.Value) ?
                    parameters.Curve.Oid.Value : parameters.Curve.Oid.FriendlyName!;

                return Interop.Crypto.EvpPKeyCreateByEcParameters(
                    oid,
                    parameters.Q.X,
                    parameters.Q.Y,
                    parameters.D,
                    out keySize);
            }
            else if (parameters.Curve.IsExplicit)
            {
                return Interop.Crypto.EvpPKeyCreateByEcExplicitParameters(
                    parameters.Curve,
                    parameters.Q.X,
                    parameters.Q.Y,
                    parameters.D,
                    out keySize);
            }
            else
            {
                throw new PlatformNotSupportedException(
                    SR.Format(SR.Cryptography_CurveNotSupported, parameters.Curve.CurveType.ToString()));
            }
        }
    }
}
