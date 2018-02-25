// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography
{
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
    internal static partial class ECDiffieHellmanImplementation
    {
#endif
        public sealed partial class ECDiffieHellmanCng : ECDiffieHellman
        {
            public override void ImportParameters(ECParameters parameters)
            {
                parameters.Validate();
                ECCurve curve = parameters.Curve;
                bool includePrivateParamerters = (parameters.D != null);

                if (curve.IsPrime)
                {
                    byte[] ecExplicitBlob = ECCng.GetPrimeCurveBlob(ref parameters, ecdh: true);
                    ImportFullKeyBlob(ecExplicitBlob, includePrivateParamerters);
                }
                else if (curve.IsNamed)
                {
                    // FriendlyName is required; an attempt was already made to default it in ECCurve
                    if (string.IsNullOrEmpty(curve.Oid.FriendlyName))
                    {
                        throw new PlatformNotSupportedException(
                            string.Format(SR.Cryptography_InvalidCurveOid, curve.Oid.Value));
                    }

                    byte[] ecNamedCurveBlob = ECCng.GetNamedCurveBlob(ref parameters, ecdh: true);
                    ImportKeyBlob(ecNamedCurveBlob, curve.Oid.FriendlyName, includePrivateParamerters);
                }
                else
                {
                    throw new PlatformNotSupportedException(
                        string.Format(SR.Cryptography_CurveNotSupported, curve.CurveType.ToString()));
                }
            }

            public override ECParameters ExportExplicitParameters(bool includePrivateParameters)
            {
                byte[] blob = ExportFullKeyBlob(includePrivateParameters);

                try
                {
                    ECParameters ecparams = new ECParameters();
                    ECCng.ExportPrimeCurveParameters(ref ecparams, blob, includePrivateParameters);
                    return ecparams;
                }
                finally
                {
                    Array.Clear(blob, 0, blob.Length);
                }
            }

            public override ECParameters ExportParameters(bool includePrivateParameters)
            {
                ECParameters ecparams = new ECParameters();

                string curveName = GetCurveName();
                byte[] blob = null;

                try
                {
                    if (string.IsNullOrEmpty(curveName))
                    {
                        blob = ExportFullKeyBlob(includePrivateParameters);
                        ECCng.ExportPrimeCurveParameters(ref ecparams, blob, includePrivateParameters);
                    }
                    else
                    {
                        blob = ExportKeyBlob(includePrivateParameters);
                        ECCng.ExportNamedCurveParameters(ref ecparams, blob, includePrivateParameters);
                        ecparams.Curve = ECCurve.CreateFromFriendlyName(curveName);
                    }

                    return ecparams;
                }
                finally
                {
                    if (blob != null)
                    {
                        Array.Clear(blob, 0, blob.Length);
                    }
                }
            }
        }
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
    }
#endif
}
