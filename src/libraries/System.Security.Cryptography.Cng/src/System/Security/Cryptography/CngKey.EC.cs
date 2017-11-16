// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Cryptography;
using System.Diagnostics;
using BCRYPT_ECC_PARAMETER_HEADER = Interop.BCrypt.BCRYPT_ECC_PARAMETER_HEADER;

namespace System.Security.Cryptography
{
    public sealed partial class CngKey : IDisposable
    {
        /// <summary>
        /// Does the key represent a named curve (Win10+)
        /// </summary>
        /// <returns></returns>
        internal bool IsECNamedCurve()
        {
            return IsECNamedCurve(Algorithm.Algorithm);
        }

        internal static bool IsECNamedCurve(string algorithm)
        {
            return (algorithm == CngAlgorithm.ECDiffieHellman.Algorithm ||
                algorithm == CngAlgorithm.ECDsa.Algorithm);
        }

        internal string GetCurveName()
        {
            if (IsECNamedCurve())
            {
                return _keyHandle.GetPropertyAsString(KeyPropertyName.ECCCurveName, CngPropertyOptions.None);
            }

            // Use hard-coded values (for use with pre-Win10 APIs)
            return GetECSpecificCurveName();
        }

        private string GetECSpecificCurveName()
        {
            string algorithm = Algorithm.Algorithm;

            if (algorithm == CngAlgorithm.ECDiffieHellmanP256.Algorithm ||
                algorithm == CngAlgorithm.ECDsaP256.Algorithm)
            {
                return "nistP256";
            }

            if (algorithm == CngAlgorithm.ECDiffieHellmanP384.Algorithm ||
                algorithm == CngAlgorithm.ECDsaP384.Algorithm)
            {
                return "nistP384";
            }

            if (algorithm == CngAlgorithm.ECDiffieHellmanP521.Algorithm ||
                algorithm == CngAlgorithm.ECDsaP521.Algorithm)
            {
                return "nistP521";
            }

            Debug.Fail(string.Format("Unknown curve {0}", algorithm));
            throw new PlatformNotSupportedException(string.Format(SR.Cryptography_CurveNotSupported, algorithm));
        }

        /// <summary>
        ///     Return a CngProperty representing a named curve.
        /// </summary>
        internal static CngProperty GetPropertyFromNamedCurve(ECCurve curve)
        {
            string curveName = curve.Oid.FriendlyName;
            unsafe
            {
                byte[] curveNameBytes = new byte[(curveName.Length + 1) * sizeof(char)]; // +1 to add trailing null
                System.Text.Encoding.Unicode.GetBytes(curveName, 0, curveName.Length, curveNameBytes, 0);
                return new CngProperty(KeyPropertyName.ECCCurveName, curveNameBytes, CngPropertyOptions.None);
            }
        }

        /// <summary>
        /// Map a curve name to algorithm. This enables curves that worked pre-Win10
        /// to work with newer APIs for import and export.
        /// </summary>
        internal static CngAlgorithm EcdsaCurveNameToAlgorithm(string name)
        {
            switch (name)
            {
                case "nistP256":
                case "ECDSA_P256":
                    return CngAlgorithm.ECDsaP256;

                case "nistP384":
                case "ECDSA_P384":
                    return CngAlgorithm.ECDsaP384;

                case "nistP521":
                case "ECDSA_P521":
                    return CngAlgorithm.ECDsaP521;
            }

            // All other curves are new in Win10 so use generic algorithm
            return CngAlgorithm.ECDsa;
        }
    }
}
