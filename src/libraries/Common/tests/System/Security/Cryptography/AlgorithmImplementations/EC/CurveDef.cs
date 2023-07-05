// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Tests
{
    public class CurveDef
    {
#if NETCOREAPP
        public CurveDef() { }
        public ECCurve Curve;
        public ECCurve.ECCurveType CurveType;
        public int KeySize;
        public bool IncludePrivate;
        public bool RequiredOnPlatform;
        public string DisplayName;

        public bool IsCurveValidOnPlatform
        {
            get
            {
                // Assume curve is valid if required; tests will fail if not present
                return RequiredOnPlatform ||
                    (Curve.IsNamed && (EcDsa.Tests.ECDsaFactory.IsCurveValid(Curve.Oid) || EcDiffieHellman.Tests.ECDiffieHellmanFactory.IsCurveValid(Curve.Oid))) ||
                    (Curve.IsExplicit && (EcDsa.Tests.ECDsaFactory.ExplicitCurvesSupported || EcDiffieHellman.Tests.ECDiffieHellmanFactory.ExplicitCurvesSupported));
            }
        }

        public bool IsCurveTypeEqual(ECCurve.ECCurveType actual)
        {
            if (CurveType == actual)
                return true;

            // Montgomery and Weierstrass are interchangeable depending on the platform
            if (CurveType == ECCurve.ECCurveType.PrimeMontgomery && actual == ECCurve.ECCurveType.PrimeShortWeierstrass ||
                CurveType == ECCurve.ECCurveType.PrimeShortWeierstrass && actual == ECCurve.ECCurveType.PrimeMontgomery)
            {
                return true;
            }

            return false;
        }

        public override string ToString()
        {
            if (Curve.IsNamed)
                return $"CurveDef Named:({Curve.Oid.Value ?? "(null)"}, {Curve.Oid.FriendlyName ?? "(null)"})";

            if (Curve.IsExplicit)
            {
                if (string.IsNullOrEmpty(DisplayName))
                {
                    return $"CurveDef Explicit:{Curve.CurveType} - {(Curve.Prime ?? Curve.Polynomial)?.Length}-byte descriptor";
                }

                return $"CurveDef Explicit:{Curve.CurveType} - {DisplayName}";
            }

            return "CurveDef (Unknown, edit ToString)";
        }
#endif
    }
}
