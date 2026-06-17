// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Security.Cryptography.Tests;
using Microsoft.DotNet.XUnitExtensions;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.EcDiffieHellman.Tests
{
    public partial class ECDiffieHellmanTests
    {
        // On CentOS, secp224r1 (also called nistP224) appears to be disabled. To prevent test failures on that platform,
        // probe for this capability before depending on it.
        internal bool ECDsa224Available =>
            ECDiffieHellmanFactory.IsCurveValid(new Oid(ECDSA_P224_OID_VALUE));

        internal bool CanDeriveNewPublicKey =>
            ECDiffieHellmanFactory.CanDeriveNewPublicKey;

        [ConditionalTheory, MemberData(nameof(AllTestCurves))]
        public void TestNamedCurves(CurveDef curveDef)
        {
            SkipTestException.ThrowUnless(curveDef.Curve.IsNamed);
            SkipTestException.ThrowUnless(curveDef.IsCurveValidOnPlatform(ECDiffieHellmanFactory));

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

        [ConditionalTheory, MemberData(nameof(PublicTestCurves))]
        public void TestNamedCurvesNegative(CurveDef curveDef)
        {
            SkipTestException.ThrowUnless(curveDef.Curve.IsNamed);
            SkipTestException.ThrowWhen(curveDef.IsCurveValidOnPlatform(ECDiffieHellmanFactory));

            // An exception may be thrown during Create() if the Oid is bad, or later during native calls
            Assert.Throws<PlatformNotSupportedException>(() =>
            {
                using ECDiffieHellman ecdh = ECDiffieHellmanFactory.Create(curveDef.Curve);
                ecdh.ExportParameters(false);
            });
        }

        [ConditionalTheory, MemberData(nameof(AllTestCurves))]
        public void TestExplicitCurves(CurveDef curveDef)
        {
            SkipTestException.ThrowUnless(ECDiffieHellmanFactory.ExplicitCurvesSupported);
            SkipTestException.ThrowUnless(curveDef.IsCurveValidOnPlatform(ECDiffieHellmanFactory));

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

        [ConditionalTheory, MemberData(nameof(PublicTestCurves))]
        public void TestExplicitCurvesKeyAgree(CurveDef curveDef)
        {
            SkipTestException.ThrowUnless(ECDiffieHellmanFactory.ExplicitCurvesSupported);
            SkipTestException.ThrowUnless(curveDef.IsCurveValidOnPlatform(ECDiffieHellmanFactory));

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
        public void TestNamedCurveNegative()
        {
            Assert.Throws<PlatformNotSupportedException>(
                () => ECDiffieHellmanFactory.Create(ECCurve.CreateFromFriendlyName("Invalid")).ExportExplicitParameters(false));

            Assert.Throws<PlatformNotSupportedException>(
                () => ECDiffieHellmanFactory.Create(ECCurve.CreateFromValue("Invalid")).ExportExplicitParameters(false));
        }

        [Fact]
        public void TestKeySizeCreateKey()
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

        [ConditionalFact]
        [SkipOnPlatform(TestPlatforms.Android, "Android does not validate curve parameters")]
        public void TestExplicitImportValidationNegative()
        {
            SkipTestException.ThrowUnless(ECDiffieHellmanFactory.ExplicitCurvesSupported);

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

        [ConditionalFact]
        public void ImportExplicitWithSeedButNoHash()
        {
            SkipTestException.ThrowUnless(ECDiffieHellmanFactory.ExplicitCurvesSupported);

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

        [ConditionalFact]
        [PlatformSpecific(TestPlatforms.Windows/* "parameters.Curve.Hash doesn't round trip on Unix." */)]
        public void ImportExplicitWithHashButNoSeed()
        {
            SkipTestException.ThrowUnless(ECDiffieHellmanFactory.ExplicitCurvesSupported);

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

        [ConditionalFact]
        public void TestNamedImportValidationNegative()
        {
            SkipTestException.ThrowUnless(ECDsa224Available);
            SkipTestException.ThrowUnless(ECDiffieHellmanFactory.ExplicitCurvesSupported);

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

        [ConditionalFact]
        public void TestGeneralExportWithExplicitParameters()
        {
            SkipTestException.ThrowUnless(ECDiffieHellmanFactory.ExplicitCurvesSupported);

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

        [ConditionalFact]
        public void TestExplicitCurveImportOnUnsupportedPlatform()
        {
            SkipTestException.ThrowWhen(
                ECDiffieHellmanFactory.ExplicitCurvesSupported || ECDiffieHellmanFactory.ExplicitCurvesSupportFailOnUseOnly);

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

        [ConditionalFact]
        public void TestNamedCurveWithExplicitKey()
        {
            SkipTestException.ThrowUnless(ECDsa224Available);
            SkipTestException.ThrowUnless(ECDiffieHellmanFactory.ExplicitCurvesSupported);

            using (ECDiffieHellman ec = ECDiffieHellmanFactory.Create())
            {
                ECParameters parameters = EccTestData.GetNistP224KeyTestData();
                ec.ImportParameters(parameters);
                VerifyNamedCurve(parameters, ec, 224, true);
            }
        }

        [Fact]
        public void ExportIncludingPrivateOnPublicOnlyKey()
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

        [ConditionalFact]
        public void ImportFromPrivateOnlyKey()
        {
            SkipTestException.ThrowUnless(ECDiffieHellmanFactory.CanDeriveNewPublicKey);

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

        [ConditionalTheory]
        [MemberData(nameof(NamedCurves))]
        public void OidPresentOnCurveMiscased(ECCurve curve, bool checkCurveValidity)
        {
            if (checkCurveValidity)
                SkipTestException.ThrowUnless(ECDiffieHellmanFactory.IsCurveValid(curve.Oid));

            ECCurve miscasedCurve = ECCurve.CreateFromFriendlyName(InvertStringCase(curve.Oid.FriendlyName));
            Assert.NotEqual(miscasedCurve.Oid.FriendlyName, curve.Oid.FriendlyName);
            Assert.Equal(miscasedCurve.Oid.FriendlyName, curve.Oid.FriendlyName, ignoreCase: true);

            using (ECDiffieHellman ecdh = ECDiffieHellmanFactory.Create())
            {
                ecdh.GenerateKey(miscasedCurve);
                ECParameters exportedParameters = ecdh.ExportParameters(false);
                Assert.Equal(curve.Oid.Value, exportedParameters.Curve.Oid.Value);

                exportedParameters.Curve = miscasedCurve;

                // Assert.NoThrow. Make sure we can import the mis-cased curve.
                ecdh.ImportParameters(exportedParameters);
            }
        }

        public static IEnumerable<object[]> NamedCurves
        {
            get
            {
                yield return new object[] { ECCurve.NamedCurves.nistP256, false };
                yield return new object[] { ECCurve.NamedCurves.nistP384, false };
                yield return new object[] { ECCurve.NamedCurves.nistP521, false };
                yield return new object[] { ECCurve.CreateFromFriendlyName("ECDH_P256"), false };
                yield return new object[] { ECCurve.CreateFromFriendlyName("ECDH_P384"), false };
                yield return new object[] { ECCurve.CreateFromFriendlyName("ECDH_P521"), false };

                // Curves may not be valid for all platforms, so validity must be checked at runtime
                yield return new object[] { ECCurve.NamedCurves.brainpoolP160r1, true };
                yield return new object[] { ECCurve.NamedCurves.brainpoolP160t1, true };
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

        [Theory]
        [MemberData(nameof(NistEccCdhPrimeCurveVectors))]
        public void EcdhKeyAgreement_PrimeCurve_MatchesKnownSharedSecret(
            string curveName, string curveOid,
            string qCavsXHex, string qCavsYHex,
            string dIutHex, string qIutXHex, string qIutYHex,
            string expectedZHex)
        {
            _ = curveName;
            ECCurve curve = ECCurve.CreateFromValue(curveOid);

            using (ECDiffieHellman iut = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellman cavs = ECDiffieHellmanFactory.Create())
            {
                iut.ImportParameters(new ECParameters
                {
                    Curve = curve,
                    D = dIutHex.HexToByteArray(),
                    Q = new ECPoint
                    {
                        X = qIutXHex.HexToByteArray(),
                        Y = qIutYHex.HexToByteArray(),
                    },
                });

                cavs.ImportParameters(new ECParameters
                {
                    Curve = curve,
                    Q = new ECPoint
                    {
                        X = qCavsXHex.HexToByteArray(),
                        Y = qCavsYHex.HexToByteArray(),
                    },
                });

                byte[] derivedZ = iut.DeriveRawSecretAgreement(cavs.PublicKey);
                Assert.Equal(expectedZHex.HexToByteArray(), derivedZ);
            }
        }

        // NIST SP 800-56A ECCCDH vectors (CAVS 14.1), P-256
        // Source: https://csrc.nist.gov/projects/cryptographic-algorithm-validation-program
        public static TheoryData<string, string, string, string, string, string, string, string> NistEccCdhPrimeCurveVectors => new()
        {
            // P-256, COUNT=0
            {
                "P-256", "1.2.840.10045.3.1.7",
                "700c48f77f56584c5cc632ca65640db91b6bacce3a4df6b42ce7cc838833d287",
                "db71e509e3fd9b060ddb20ba5c51dcc5948d46fbf640dfe0441782cab85fa4ac",
                "7d7dc5f71eb29ddaf80d6214632eeae03d9058af1fb6d22ed80badb62bc1a534",
                "ead218590119e8876b29146ff89ca61770c4edbbf97d38ce385ed281d8a6b230",
                "28af61281fd35e2fa7002523acc85a429cb06ee6648325389f59edfce1405141",
                "46fc62106420ff012e54a434fbdd2d25ccc5852060561e68040dd7778997bd7b"
            },
            // P-256, COUNT=1
            {
                "P-256", "1.2.840.10045.3.1.7",
                "809f04289c64348c01515eb03d5ce7ac1a8cb9498f5caa50197e58d43a86a7ae",
                "b29d84e811197f25eba8f5194092cb6ff440e26d4421011372461f579271cda3",
                "38f65d6dce47676044d58ce5139582d568f64bb16098d179dbab07741dd5caf5",
                "119f2f047902782ab0c9e27a54aff5eb9b964829ca99c06b02ddba95b0a3f6d0",
                "8f52b726664cac366fc98ac7a012b2682cbd962e5acb544671d41b9445704d1d",
                "057d636096cb80b67a8c038c890e887d1adfa4195e9b3ce241c8a778c59cda67"
            },
        };
    }
}
