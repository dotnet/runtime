// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    internal static partial class ECDiffieHellmanImplementation
    {
        public sealed partial class ECDiffieHellmanCngPublicKey : ECDiffieHellmanPublicKey
        {
            private byte[] _keyBlob;
            internal string _curveName;

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
            }

            public override string ToXmlString()
            {
                throw new PlatformNotSupportedException();
            }

            public static ECDiffieHellmanCngPublicKey FromXmlString(string xml)
            {
                throw new PlatformNotSupportedException();
            }

            internal ECDiffieHellmanCngPublicKey(byte[] keyBlob, string curveName) : base(keyBlob)
            {
                Debug.Assert(keyBlob != null);

                _keyBlob = keyBlob;
                _curveName = curveName;
            }

            /// <summary>
            ///  Exports the key and explicit curve parameters used by the ECC object into an <see cref="ECParameters"/> object.
            /// </summary>
            /// <exception cref="CryptographicException">
            ///  if there was an issue obtaining the curve values.
            /// </exception>
            /// <exception cref="PlatformNotSupportedException">
            ///  if explicit export is not supported by this platform. Windows 10 or higher is required.
            /// </exception>
            /// <returns>The key and explicit curve parameters used by the ECC object.</returns>
            public override ECParameters ExportExplicitParameters()
            {
                ECParameters ecparams = new ECParameters();
                ECCng.ExportPrimeCurveParameters(ref ecparams, _keyBlob, includePrivateParameters: false);
                return ecparams;
            }

            /// <summary>
            ///  Exports the key used by the ECC object into an <see cref="ECParameters"/> object.
            ///  If the key was created as a named curve, the Curve property will contain named curve parameters
            ///  otherwise it will contain explicit parameters.
            /// </summary>
            /// <exception cref="CryptographicException">
            ///  if there was an issue obtaining the curve values.
            /// </exception>
            /// <returns>The key and named curve parameters used by the ECC object.</returns>
            public override ECParameters ExportParameters()
            {
                if (string.IsNullOrEmpty(_curveName))
                {
                    return ExportExplicitParameters();
                }
                else
                {
                    ECParameters ecparams = new ECParameters();
                    ECCng.ExportNamedCurveParameters(ref ecparams, _keyBlob, includePrivateParameters: false);
                    ecparams.Curve = ECCurve.CreateFromFriendlyName(_curveName);
                    return ecparams;
                }
            }
        }
    }
}
