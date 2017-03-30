// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.EcDsa.Tests
{
    public class CurveDef
    {
#if netcoreapp
        public CurveDef() { }
        public ECCurve Curve;
        public ECCurve.ECCurveType CurveType;
        public int KeySize;
        public bool IncludePrivate;
        public bool RequiredOnPlatform;

        public bool IsCurveValidOnPlatform
        {
            get
            {
                // Assume curve is valid if required; tests will fail if not present
                return RequiredOnPlatform || ECDsaFactory.IsCurveValid(Curve.Oid);
            }
        }

        public bool IsCurveTypeEqual(ECCurve.ECCurveType actual)
        {
            if (CurveType == actual)
                return true;

            // Montgomery and Weierstrass are interchangable depending on the platform 
            if (CurveType == ECCurve.ECCurveType.PrimeMontgomery && actual == ECCurve.ECCurveType.PrimeShortWeierstrass ||
                CurveType == ECCurve.ECCurveType.PrimeShortWeierstrass && actual == ECCurve.ECCurveType.PrimeMontgomery)
            {
                return true;
            }

            return false;
        }
#endif
    }
}
