// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.EcDsa.Tests
{
    /// <summary>
    /// Input and helper methods for ECDsa
    /// </summary>
    public abstract class ECDsaTestsBase
    {
#if netcoreapp
        internal const string ECDSA_P224_OID_VALUE = "1.3.132.0.33"; // Also called nistP224 or secP224r1
        internal const string ECDSA_P256_OID_VALUE = "1.2.840.10045.3.1.7"; // Also called nistP256, secP256r1 or prime256v1(OpenSsl)
        internal const string ECDSA_P384_OID_VALUE = "1.3.132.0.34"; // Also called nistP384 or secP384r1
        internal const string ECDSA_P521_OID_VALUE = "1.3.132.0.35"; // Also called nistP521 or secP521r1
        internal const string ECDSA_Sect193r1_OID_VALUE = "1.3.132.0.24"; //Char-2 curve

        public static IEnumerable<object[]> TestCurvesFull
        {
            get
            {
                var curveDefs =
                    from curveDef in TestCurvesRaw
                    where curveDef.IsCurveValidOnPlatform == true
                    select curveDef;

                foreach (CurveDef cd in curveDefs)
                    yield return new[] { cd };

                // return again with IncludePrivate = true
                foreach (CurveDef cd in curveDefs)
                {
                    cd.IncludePrivate = true;
                    yield return new[] { cd };
                }
            }
        }

        public static IEnumerable<object[]> TestCurves
        {
            get
            {
                var curveDefs =
                    from curveDef in TestCurvesRaw
                    where curveDef.IsCurveValidOnPlatform == true
                    select curveDef;

                foreach (CurveDef curveDef in curveDefs)
                    yield return new[] { curveDef };
            }
        }

        public static IEnumerable<object[]> TestInvalidCurves
        {
            get
            {
                var curveDefs =
                    from curveDef in TestCurvesRaw
                    where curveDef.IsCurveValidOnPlatform == false
                    select curveDef;

                foreach (CurveDef curveDef in curveDefs)
                    yield return new[] { curveDef };
            }
        }

        public static IEnumerable<object[]> TestNewCurves
        {
            get
            {
                var curveDefs =
                    from curveDef in TestCurvesRaw
                    where
                        curveDef.IsCurveValidOnPlatform == true &&
                        curveDef.RequiredOnPlatform == false
                    select curveDef;

                foreach (CurveDef curveDef in curveDefs)
                    yield return new[] { curveDef };
            }
        }

        private static IEnumerable<CurveDef> TestCurvesRaw
        {
            get
            {
                // nistP* curves
                yield return new CurveDef()
                {
                    Curve = ECCurve.NamedCurves.nistP256, // also secp256r1
                    KeySize = 256,
                    CurveType = ECCurve.ECCurveType.PrimeShortWeierstrass,
                    RequiredOnPlatform = true,
                };
                yield return new CurveDef()
                {
                    Curve = ECCurve.NamedCurves.nistP384, // also secp384r1
                    KeySize = 384,
                    CurveType = ECCurve.ECCurveType.PrimeMontgomery,
                    RequiredOnPlatform = true,
                };
                yield return new CurveDef()
                {
                    Curve = ECCurve.NamedCurves.nistP521, // also secp521r1
                    KeySize = 521,
                    CurveType = ECCurve.ECCurveType.PrimeShortWeierstrass,
                    RequiredOnPlatform = true,
                };
                yield return new CurveDef()
                {
                    Curve = ECCurve.NamedCurves.brainpoolP160r1,
                    KeySize = 160,
                    CurveType = ECCurve.ECCurveType.PrimeShortWeierstrass
                };
                yield return new CurveDef()
                {
                    Curve = ECCurve.CreateFromOid(new Oid("1.3.132.0.24", "")), // sect193r1
                    KeySize = 193,
                    CurveType = ECCurve.ECCurveType.Characteristic2,
                };
                yield return new CurveDef()
                {
                    Curve = ECCurve.CreateFromOid(new Oid("1.2.840.10045.3.0.1", "")), // c2pnb163v1
                    KeySize = 163,
                    CurveType = ECCurve.ECCurveType.Characteristic2,
                };
                yield return new CurveDef()
                {
                    Curve = ECCurve.CreateFromOid(new Oid("1.3.132.0.16", "")), // sect283k1
                    KeySize = 283,
                    CurveType = ECCurve.ECCurveType.Characteristic2,
                };
                yield return new CurveDef()
                {
                    Curve = ECCurve.CreateFromOid(new Oid("1.3.132.0.17", "")), // sect283r1
                    KeySize = 283,
                    CurveType = ECCurve.ECCurveType.Characteristic2,
                };
                yield return new CurveDef()
                {
                    Curve = ECCurve.CreateFromOid(new Oid("", "wap-wsg-idm-ecid-wtls7")),
                    KeySize = 160,
                    CurveType = ECCurve.ECCurveType.PrimeMontgomery,
                };
                yield return new CurveDef()
                {
                    Curve = ECCurve.CreateFromOid(new Oid("invalid", "invalid")),
                    KeySize = 160,
                    CurveType = ECCurve.ECCurveType.PrimeShortWeierstrass,
                };
                yield return new CurveDef
                {
                    Curve = ECDsaTestData.GetNistP256ExplicitCurve(),
                    KeySize = 256,
                    CurveType = ECCurve.ECCurveType.PrimeShortWeierstrass,
                    DisplayName = "NIST P-256",
                };
            }
        }

        internal static void AssertEqual(ECParameters p1, ECParameters p2)
        {
            ComparePrivateKey(p1, p2);
            ComparePublicKey(p1.Q, p2.Q);
            CompareCurve(p1.Curve, p2.Curve);
        }

        internal static void ComparePrivateKey(ECParameters p1, ECParameters p2, bool isEqual = true)
        {
            if (isEqual)
            {
                Assert.Equal(p1.D, p2.D);
            }
            else
            {
                Assert.NotEqual(p1.D, p2.D);
            }
        }

        internal static void ComparePublicKey(ECPoint q1, ECPoint q2, bool isEqual = true)
        {
            if (isEqual)
            {
                Assert.Equal(q1.X, q2.X);
                Assert.Equal(q1.Y, q2.Y);
            }
            else
            {
                Assert.NotEqual(q1.X, q2.X);
                Assert.NotEqual(q1.Y, q2.Y);
            }
        }

        internal static void CompareCurve(ECCurve c1, ECCurve c2)
        {
            if (c1.IsNamed)
            {
                Assert.True(c2.IsNamed);
                Assert.Equal(c1.Oid.FriendlyName, c2.Oid.FriendlyName);
            }
            else if (c1.IsExplicit)
            {
                Assert.True(c2.IsExplicit);
                Assert.Equal(c1.A, c2.A);
                Assert.Equal(c1.B, c2.B);
                Assert.Equal(c1.CurveType, c2.CurveType);
                Assert.Equal(c1.G.X, c2.G.X);
                Assert.Equal(c1.G.Y, c2.G.Y);
                Assert.Equal(c1.Cofactor, c2.Cofactor);
                Assert.Equal(c1.Order, c2.Order);
                Assert.Equal(c1.Seed, c2.Seed);
                Assert.Equal(c1.Hash, c2.Hash);

                if (c1.IsPrime)
                {
                    Assert.True(c2.IsPrime);
                    Assert.Equal(c1.Prime, c2.Prime);
                }
                else if (c1.IsCharacteristic2)
                {
                    Assert.True(c2.IsCharacteristic2);
                    Assert.Equal(c1.Polynomial, c2.Polynomial);
                }
            }
        }

        internal static void Verify256(ECDsa e, bool expected)
        {
            byte[] sig = ("998791331eb2e1f4259297f5d9cb82fa20dec98e1cb0900e6b8f014a406c3d02cbdbf5238bde471c3155fc25565524301429"
                        + "d8713dad9a67eb0a5c355e9e23dc").HexToByteArray();
            bool verified = e.VerifyHash(ECDsaTestData.s_hashSha512, sig);
            Assert.Equal(expected, verified);
        }

        // On CentOS, secp224r1 (also called nistP224) appears to be disabled. To prevent test failures on that platform, 
        // probe for this capability before depending on it. 
        internal static bool ECDsa224Available
        {
            get
            {
                return ECDsaFactory.IsCurveValid(new Oid(ECDSA_P224_OID_VALUE));
            }
        }

        internal static bool ECExplicitCurvesSupported
        {
            get
            {
                return ECDsaFactory.ExplicitCurvesSupported;
            }
        }
#endif // netcoreapp
    }

    internal static class EcDsaTestExtensions
    {
        internal static void Exercise(this ECDsa e)
        {
            // Make a few calls on this to ensure we aren't broken due to bad/prematurely released handles.

            int keySize = e.KeySize;

            byte[] data = new byte[0x10];
            byte[] sig = e.SignData(data, 0, data.Length, HashAlgorithmName.SHA1);
            bool verified = e.VerifyData(data, sig, HashAlgorithmName.SHA1);
            Assert.True(verified);

            unchecked
            {
                sig[sig.Length - 1]++;
            }
            verified = e.VerifyData(data, sig, HashAlgorithmName.SHA1);
            Assert.False(verified);
        }
    }
}
