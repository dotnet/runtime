// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.Tests;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.EcDiffieHellman.Tests
{
#if NETCOREAPP
    public partial class ECDiffieHellmanTests
    {
        // On CentOS, secp224r1 (also called nistP224) appears to be disabled. To prevent test failures on that platform,
        // probe for this capability before depending on it.
        internal static bool ECDsa224Available =>
            ECDiffieHellmanFactory.IsCurveValid(new Oid(ECDSA_P224_OID_VALUE));
        
        internal static bool CanDeriveNewPublicKey { get; }
            = EcDiffieHellman.Tests.ECDiffieHellmanFactory.CanDeriveNewPublicKey;

        [Theory, MemberData(nameof(TestCurvesFull))]
        public static void TestNamedCurves(CurveDef curveDef)
        {
            if (!curveDef.Curve.IsNamed)
                return;

            using (ECDiffieHellman ec1 = ECDiffieHellmanFactory.Create(curveDef.Curve))
            {
                ECParameters param1 = ec1.ExportParameters(curveDef.IncludePrivate);
                VerifyNamedCurve(param1, ec1, curveDef.KeySize, curveDef.IncludePrivate);

                using (ECDiffieHellman ec2 = ECDiffieHellmanFactory.Create())
                {
                    ec2.ImportParameters(param1);
                    ECParameters param2 = ec2.ExportParameters(curveDef.IncludePrivate);
                    VerifyNamedCurve(param2, ec2, curveDef.KeySize, curveDef.IncludePrivate);

                    AssertEqual(param1, param2);
                }
            }
        }

        [Theory, MemberData(nameof(TestInvalidCurves))]
        public static void TestNamedCurvesNegative(CurveDef curveDef)
        {
            if (!curveDef.Curve.IsNamed)
                return;

            // An exception may be thrown during Create() if the Oid is bad, or later during native calls
            Assert.Throws<PlatformNotSupportedException>(() =>
            {
                using ECDiffieHellman ecdh = ECDiffieHellmanFactory.Create(curveDef.Curve);
                ecdh.ExportParameters(false);
            });
        }

        [Theory, MemberData(nameof(TestCurvesFull))]
        public static void TestExplicitCurves(CurveDef curveDef)
        {
            if (!ECDiffieHellmanFactory.ExplicitCurvesSupported)
            {
                return;
            }

            using (ECDiffieHellman ec1 = ECDiffieHellmanFactory.Create(curveDef.Curve))
            {
                ECParameters param1 = ec1.ExportExplicitParameters(curveDef.IncludePrivate);
                VerifyExplicitCurve(param1, ec1, curveDef);

                using (ECDiffieHellman ec2 = ECDiffieHellmanFactory.Create())
                {
                    ec2.ImportParameters(param1);
                    ECParameters param2 = ec2.ExportExplicitParameters(curveDef.IncludePrivate);
                    VerifyExplicitCurve(param1, ec1, curveDef);

                    AssertEqual(param1, param2);
                }
            }
        }

        [Theory, MemberData(nameof(TestCurves))]
        public static void TestExplicitCurvesKeyAgree(CurveDef curveDef)
        {
            if (!ECDiffieHellmanFactory.ExplicitCurvesSupported)
            {
                return;
            }

            using (ECDiffieHellman ecdh1Named = ECDiffieHellmanFactory.Create(curveDef.Curve))
            {
                ECParameters ecdh1ExplicitParameters = ecdh1Named.ExportExplicitParameters(true);

                using (ECDiffieHellman ecdh1Explicit = ECDiffieHellmanFactory.Create())
                using (ECDiffieHellman ecdh2 = ECDiffieHellmanFactory.Create(ecdh1ExplicitParameters.Curve))
                {
                    ecdh1Explicit.ImportParameters(ecdh1ExplicitParameters);

                    using (ECDiffieHellmanPublicKey ecdh1NamedPub = ecdh1Named.PublicKey)
                    using (ECDiffieHellmanPublicKey ecdh1ExplicitPub = ecdh1Explicit.PublicKey)
                    using (ECDiffieHellmanPublicKey ecdh2Pub = ecdh2.PublicKey)
                    {
                        HashAlgorithmName hash = HashAlgorithmName.SHA256;

                        byte[] ech1Named_ecdh1Named = ecdh1Named.DeriveKeyFromHash(ecdh1NamedPub, hash);
                        byte[] ech1Named_ecdh1Named2 = ecdh1Named.DeriveKeyFromHash(ecdh1NamedPub, hash);
                        byte[] ech1Named_ecdh1Explicit = ecdh1Named.DeriveKeyFromHash(ecdh1ExplicitPub, hash);
                        byte[] ech1Named_ecdh2Explicit = ecdh1Named.DeriveKeyFromHash(ecdh2Pub, hash);

                        byte[] ecdh1Explicit_ecdh1Named = ecdh1Explicit.DeriveKeyFromHash(ecdh1NamedPub, hash);
                        byte[] ecdh1Explicit_ecdh1Explicit = ecdh1Explicit.DeriveKeyFromHash(ecdh1ExplicitPub, hash);
                        byte[] ecdh1Explicit_ecdh1Explicit2 = ecdh1Explicit.DeriveKeyFromHash(ecdh1ExplicitPub, hash);
                        byte[] ecdh1Explicit_ecdh2Explicit = ecdh1Explicit.DeriveKeyFromHash(ecdh2Pub, hash);

                        byte[] ecdh2_ecdh1Named = ecdh2.DeriveKeyFromHash(ecdh1NamedPub, hash);
                        byte[] ecdh2_ecdh1Explicit = ecdh2.DeriveKeyFromHash(ecdh1ExplicitPub, hash);
                        byte[] ecdh2_ecdh2Explicit = ecdh2.DeriveKeyFromHash(ecdh2Pub, hash);
                        byte[] ecdh2_ecdh2Explicit2 = ecdh2.DeriveKeyFromHash(ecdh2Pub, hash);

                        Assert.Equal(ech1Named_ecdh1Named, ech1Named_ecdh1Named2);
                        Assert.Equal(ech1Named_ecdh1Explicit, ecdh1Explicit_ecdh1Named);
                        Assert.Equal(ech1Named_ecdh2Explicit, ecdh2_ecdh1Named);

                        Assert.Equal(ecdh1Explicit_ecdh1Explicit, ecdh1Explicit_ecdh1Explicit2);
                        Assert.Equal(ecdh1Explicit_ecdh2Explicit, ecdh2_ecdh1Explicit);

                        Assert.Equal(ecdh2_ecdh2Explicit, ecdh2_ecdh2Explicit2);
                    }
                }
            }
        }

        [Fact]
        public static void TestNamedCurveNegative()
        {
            Assert.Throws<PlatformNotSupportedException>(
                () => ECDiffieHellmanFactory.Create(ECCurve.CreateFromFriendlyName("Invalid")).ExportExplicitParameters(false));

            Assert.Throws<PlatformNotSupportedException>(
                () => ECDiffieHellmanFactory.Create(ECCurve.CreateFromValue("Invalid")).ExportExplicitParameters(false));
        }

        [Fact]
        public static void TestKeySizeCreateKey()
        {
            using (ECDiffieHellman ec = ECDiffieHellmanFactory.Create(ECCurve.NamedCurves.nistP256))
            {
                // Ensure the handle is created
                Assert.Equal(256, ec.KeySize);
                ec.Exercise();

                ec.KeySize = 521; //nistP521
                Assert.Equal(521, ec.KeySize);
                ec.Exercise();

                Assert.ThrowsAny<CryptographicException>(() => ec.KeySize = 9999);
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Android, "Android does not validate curve parameters")]
        public static void TestExplicitImportValidationNegative()
        {
            if (!ECDiffieHellmanFactory.ExplicitCurvesSupported)
            {
                return;
            }

            unchecked
            {
                using (ECDiffieHellman ec = ECDiffieHellmanFactory.Create())
                {
                    ECParameters p = EccTestData.GetNistP256ExplicitTestData();
                    Assert.True(p.Curve.IsPrime);
                    ec.ImportParameters(p);

                    ECParameters temp = p;
                    temp.Q.X = null; Assert.ThrowsAny<CryptographicException>(() => ec.ImportParameters(temp));
                    temp.Q.X = new byte[] { }; Assert.ThrowsAny<CryptographicException>(() => ec.ImportParameters(temp));
                    temp.Q.X = new byte[1] { 0x10 }; Assert.ThrowsAny<CryptographicException>(() => ec.ImportParameters(temp));
                    temp.Q.X = (byte[])p.Q.X.Clone(); --temp.Q.X[0]; Assert.ThrowsAny<CryptographicException>(() => ec.ImportParameters(temp));

                    temp = p;
                    temp.Q.Y = null; Assert.ThrowsAny<CryptographicException>(() => ec.ImportParameters(temp));
                    temp.Q.Y = new byte[] { }; Assert.ThrowsAny<CryptographicException>(() => ec.ImportParameters(temp));
                    temp.Q.Y = new byte[1] { 0x10 }; Assert.ThrowsAny<CryptographicException>(() => ec.ImportParameters(temp));
                    temp.Q.Y = (byte[])p.Q.Y.Clone(); --temp.Q.Y[0]; Assert.ThrowsAny<CryptographicException>(() => ec.ImportParameters(temp));

                    temp = p;
                    temp.Curve.A = null; Assert.ThrowsAny<CryptographicException>(() => ec.ImportParameters(temp));
                    temp.Curve.A = new byte[] { }; Assert.ThrowsAny<CryptographicException>(() => ec.ImportParameters(temp));
                    temp.Curve.A = new byte[1] { 0x10 }; Assert.ThrowsAny<CryptographicException>(() => ec.ImportParameters(temp));
                    temp.Curve.A = (byte[])p.Curve.A.Clone(); --temp.Curve.A[0]; Assert.ThrowsAny<CryptographicException>(() => ec.ImportParameters(temp));

                    temp = p;
                    temp.Curve.B = null; Assert.ThrowsAny<CryptographicException>(() => ec.ImportParameters(temp));
                    temp.Curve.B = new byte[] { }; Assert.ThrowsAny<CryptographicException>(() => ec.ImportParameters(temp));
                    temp.Curve.B = new byte[1] { 0x10 }; Assert.ThrowsAny<CryptographicException>(() => ec.ImportParameters(temp));
                    temp.Curve.B = (byte[])p.Curve.B.Clone(); --temp.Curve.B[0]; Assert.ThrowsAny<CryptographicException>(() => ec.ImportParameters(temp));

                    temp = p;
                    temp.Curve.Order = null; Assert.ThrowsAny<CryptographicException>(() => ec.ImportParameters(temp));
                    temp.Curve.Order = new byte[] { }; Assert.ThrowsAny<CryptographicException>(() => ec.ImportParameters(temp));

                    temp = p;
                    temp.Curve.Prime = null; Assert.ThrowsAny<CryptographicException>(() => ec.ImportParameters(temp));
                    temp.Curve.Prime = new byte[] { }; Assert.ThrowsAny<CryptographicException>(() => ec.ImportParameters(temp));
                    temp.Curve.Prime = new byte[1] { 0x10 }; Assert.ThrowsAny<CryptographicException>(() => ec.ImportParameters(temp));
                    temp.Curve.Prime = (byte[])p.Curve.Prime.Clone(); --temp.Curve.Prime[0]; Assert.ThrowsAny<CryptographicException>(() => ec.ImportParameters(temp));
                }
            }
        }

        [Fact]
        public static void ImportExplicitWithSeedButNoHash()
        {
            if (!ECDiffieHellmanFactory.ExplicitCurvesSupported)
            {
                return;
            }

            using (ECDiffieHellman ec = ECDiffieHellmanFactory.Create())
            {
                ECCurve curve = EccTestData.GetNistP256ExplicitCurve();
                Assert.NotNull(curve.Hash);
                ec.GenerateKey(curve);

                ECParameters parameters = ec.ExportExplicitParameters(true);
                Assert.NotNull(parameters.Curve.Seed);
                parameters.Curve.Hash = null;

                ec.ImportParameters(parameters);
                ec.Exercise();
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows/* "parameters.Curve.Hash doesn't round trip on Unix." */)]
        public static void ImportExplicitWithHashButNoSeed()
        {
            if (!ECDiffieHellmanFactory.ExplicitCurvesSupported)
            {
                return;
            }

            using (ECDiffieHellman ec = ECDiffieHellmanFactory.Create())
            {
                ECCurve curve = EccTestData.GetNistP256ExplicitCurve();
                Assert.NotNull(curve.Hash);
                ec.GenerateKey(curve);

                ECParameters parameters = ec.ExportExplicitParameters(true);
                Assert.NotNull(parameters.Curve.Hash);
                parameters.Curve.Seed = null;

                ec.ImportParameters(parameters);
                ec.Exercise();
            }
        }

        [ConditionalFact(nameof(ECDsa224Available))]
        public static void TestNamedImportValidationNegative()
        {
            if (!ECDiffieHellmanFactory.ExplicitCurvesSupported)
            {
                return;
            }

            unchecked
            {
                using (ECDiffieHellman ec = ECDiffieHellmanFactory.Create())
                {
                    ECParameters p = EccTestData.GetNistP224KeyTestData();
                    Assert.True(p.Curve.IsNamed);
                    var q = p.Q;
                    var c = p.Curve;
                    ec.ImportParameters(p);

                    ECParameters temp = p;
                    temp.Q.X = null; Assert.ThrowsAny<CryptographicException>(() => ec.ImportParameters(temp));
                    temp.Q.X = new byte[] { }; Assert.ThrowsAny<CryptographicException>(() => ec.ImportParameters(temp));
                    temp.Q.X = new byte[1] { 0x10 }; Assert.ThrowsAny<CryptographicException>(() => ec.ImportParameters(temp));
                    temp.Q.X = (byte[])p.Q.X.Clone(); temp.Q.X[0]--; Assert.ThrowsAny<CryptographicException>(() => ec.ImportParameters(temp));

                    temp = p;
                    temp.Q.Y = null; Assert.ThrowsAny<CryptographicException>(() => ec.ImportParameters(temp));
                    temp.Q.Y = new byte[] { }; Assert.ThrowsAny<CryptographicException>(() => ec.ImportParameters(temp));
                    temp.Q.Y = new byte[1] { 0x10 }; Assert.ThrowsAny<CryptographicException>(() => ec.ImportParameters(temp));
                    temp.Q.Y = (byte[])p.Q.Y.Clone(); temp.Q.Y[0]--; Assert.ThrowsAny<CryptographicException>(() => ec.ImportParameters(temp));

                    temp = p; temp.Curve = ECCurve.CreateFromOid(new Oid("Invalid", "Invalid")); Assert.ThrowsAny<PlatformNotSupportedException>(() => ec.ImportParameters(temp));
                }
            }
        }

        [Fact]
        public static void TestGeneralExportWithExplicitParameters()
        {
            if (!ECDiffieHellmanFactory.ExplicitCurvesSupported)
            {
                return;
            }

            using (ECDiffieHellman ecdsa = ECDiffieHellmanFactory.Create())
            {
                ECParameters param = EccTestData.GetNistP256ExplicitTestData();
                param.Validate();
                ecdsa.ImportParameters(param);
                Assert.True(param.Curve.IsExplicit);

                param = ecdsa.ExportParameters(false);
                param.Validate();

                // We should have explicit values, not named, as this curve has no name.
                Assert.True(param.Curve.IsExplicit);
            }
        }

        [Fact]
        public static void TestExplicitCurveImportOnUnsupportedPlatform()
        {
            if (ECDiffieHellmanFactory.ExplicitCurvesSupported)
            {
                return;
            }

            using (ECDiffieHellman ecdh = ECDiffieHellmanFactory.Create())
            {
                ECParameters param = EccTestData.GetNistP256ExplicitTestData();

                Assert.Throws<PlatformNotSupportedException>(
                    () =>
                    {
                        try
                        {
                            ecdh.ImportParameters(param);
                        }
                        catch (CryptographicException e)
                        {
                            throw new PlatformNotSupportedException("Converting exception", e);
                        }
                    });
            }
        }

        [ConditionalFact(nameof(ECDsa224Available))]
        public static void TestNamedCurveWithExplicitKey()
        {
            if (!ECDiffieHellmanFactory.ExplicitCurvesSupported)
            {
                return;
            }

            using (ECDiffieHellman ec = ECDiffieHellmanFactory.Create())
            {
                ECParameters parameters = EccTestData.GetNistP224KeyTestData();
                ec.ImportParameters(parameters);
                VerifyNamedCurve(parameters, ec, 224, true);
            }
        }

        [Fact]
        public static void ExportIncludingPrivateOnPublicOnlyKey()
        {
            ECParameters iutParameters = new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP521,
                Q =
                {
                    X = "00d45615ed5d37fde699610a62cd43ba76bedd8f85ed31005fe00d6450fbbd101291abd96d4945a8b57bc73b3fe9f4671105309ec9b6879d0551d930dac8ba45d255".HexToByteArray(),
                    Y = "01425332844e592b440c0027972ad1526431c06732df19cd46a242172d4dd67c2c8c99dfc22e49949a56cf90c6473635ce82f25b33682fb19bc33bd910ed8ce3a7fa".HexToByteArray(),
                },
                D = "00816f19c1fb10ef94d4a1d81c156ec3d1de08b66761f03f06ee4bb9dcebbbfe1eaa1ed49a6a990838d8ed318c14d74cc872f95d05d07ad50f621ceb620cd905cfb8".HexToByteArray(),
            };

            using (ECDiffieHellman iut = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellman cavs = ECDiffieHellmanFactory.Create())
            {
                iut.ImportParameters(iutParameters);
                cavs.ImportParameters(iut.ExportParameters(false));

                Assert.ThrowsAny<CryptographicException>(() => cavs.ExportParameters(true));

                if (ECDiffieHellmanFactory.ExplicitCurvesSupported)
                {
                    Assert.ThrowsAny<CryptographicException>(() => cavs.ExportExplicitParameters(true));
                }

                using (ECDiffieHellmanPublicKey iutPublic = iut.PublicKey)
                {
                    Assert.ThrowsAny<CryptographicException>(() => cavs.DeriveKeyFromHash(iutPublic, HashAlgorithmName.SHA256));
                }
            }
        }

        [ConditionalFact(nameof(CanDeriveNewPublicKey))]
        public static void ImportFromPrivateOnlyKey()
        {
            byte[] expectedX = "00d45615ed5d37fde699610a62cd43ba76bedd8f85ed31005fe00d6450fbbd101291abd96d4945a8b57bc73b3fe9f4671105309ec9b6879d0551d930dac8ba45d255".HexToByteArray();
            byte[] expectedY = "01425332844e592b440c0027972ad1526431c06732df19cd46a242172d4dd67c2c8c99dfc22e49949a56cf90c6473635ce82f25b33682fb19bc33bd910ed8ce3a7fa".HexToByteArray();

            ECParameters limitedPrivateParameters = new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP521,
                Q = default,
                D = "00816f19c1fb10ef94d4a1d81c156ec3d1de08b66761f03f06ee4bb9dcebbbfe1eaa1ed49a6a990838d8ed318c14d74cc872f95d05d07ad50f621ceb620cd905cfb8".HexToByteArray(),
            };

            using (ECDiffieHellman ecdh = ECDiffieHellmanFactory.Create())
            {
                ecdh.ImportParameters(limitedPrivateParameters);
                ECParameters exportedParameters = ecdh.ExportParameters(true);

                Assert.Equal(expectedX, exportedParameters.Q.X);
                Assert.Equal(expectedY, exportedParameters.Q.Y);
                Assert.Equal(limitedPrivateParameters.D, exportedParameters.D);
            }
        }

        private static void VerifyNamedCurve(ECParameters parameters, ECDiffieHellman ec, int keySize, bool includePrivate)
        {
            parameters.Validate();
            Assert.True(parameters.Curve.IsNamed);
            Assert.Equal(keySize, ec.KeySize);
            Assert.True(
                includePrivate && parameters.D.Length > 0 ||
                !includePrivate && parameters.D == null);

            if (includePrivate)
                ec.Exercise();

            // Ensure the key doesn't get regenerated after export
            ECParameters paramSecondExport = ec.ExportParameters(includePrivate);
            paramSecondExport.Validate();
            AssertEqual(parameters, paramSecondExport);
        }

        private static void VerifyExplicitCurve(ECParameters parameters, ECDiffieHellman ec, CurveDef curveDef)
        {
            Assert.True(parameters.Curve.IsExplicit);
            ECCurve curve = parameters.Curve;


            Assert.True(curveDef.IsCurveTypeEqual(curve.CurveType));
            Assert.True(
                curveDef.IncludePrivate && parameters.D.Length > 0 ||
                !curveDef.IncludePrivate && parameters.D == null);
            Assert.Equal(curveDef.KeySize, ec.KeySize);

            Assert.Equal(curve.A.Length, parameters.Q.X.Length);
            Assert.Equal(curve.A.Length, parameters.Q.Y.Length);
            Assert.Equal(curve.A.Length, curve.B.Length);
            Assert.Equal(curve.A.Length, curve.G.X.Length);
            Assert.Equal(curve.A.Length, curve.G.Y.Length);
            Assert.True(curve.Seed == null || curve.Seed.Length > 0);
            Assert.True(curve.Order == null || curve.Order.Length > 0);
            if (curve.IsPrime)
            {
                Assert.Equal(curve.A.Length, curve.Prime.Length);
            }

            if (curveDef.IncludePrivate)
                ec.Exercise();

            // Ensure the key doesn't get regenerated after export
            ECParameters paramSecondExport = ec.ExportExplicitParameters(curveDef.IncludePrivate);
            AssertEqual(parameters, paramSecondExport);
        }
    }
#endif
}
