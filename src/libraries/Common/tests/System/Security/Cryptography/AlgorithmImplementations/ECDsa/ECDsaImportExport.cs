// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Security.Cryptography.Tests;
using Test.Cryptography;

namespace System.Security.Cryptography.EcDsa.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public class ECDsaImportExportTests : ECDsaTestsBase
    {
        internal static bool CanDeriveNewPublicKey { get; }
            = EcDiffieHellman.Tests.ECDiffieHellmanFactory.CanDeriveNewPublicKey;

#if NETCOREAPP
        [Fact]
        public static void DiminishedCoordsRoundtrip()
        {
            ECParameters toImport = EccTestData.GetNistP521DiminishedCoordsParameters();
            ECParameters privateParams;
            ECParameters publicParams;

            using (ECDsa ecdsa = ECDsaFactory.Create())
            {
                ecdsa.ImportParameters(toImport);
                privateParams = ecdsa.ExportParameters(true);
                publicParams = ecdsa.ExportParameters(false);
            }

            ComparePublicKey(toImport.Q, privateParams.Q);
            ComparePrivateKey(toImport, privateParams);
            ComparePublicKey(toImport.Q, publicParams.Q);
            Assert.Null(publicParams.D);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows/* "parameters.Curve.Hash doesn't round trip on Unix." */)]
        public static void ImportExplicitWithHashButNoSeed()
        {
            if (!ECDsaFactory.ExplicitCurvesSupported)
            {
                return;
            }

            using (ECDsa ec = ECDsaFactory.Create())
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

        [Theory]
        [MemberData(nameof(TestCurvesFull))]
        public static void TestNamedCurves(CurveDef curveDef)
        {
            if (!curveDef.Curve.IsNamed)
                return;

            using (ECDsa ec1 = ECDsaFactory.Create(curveDef.Curve))
            {
                ECParameters param1 = ec1.ExportParameters(curveDef.IncludePrivate);
                VerifyNamedCurve(param1, ec1, curveDef.KeySize, curveDef.IncludePrivate);

                using (ECDsa ec2 = ECDsaFactory.Create())
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
            Assert.Throws<PlatformNotSupportedException>(() => ECDsaFactory.Create(curveDef.Curve).ExportParameters(false));
        }

        [ConditionalTheory(nameof(ECExplicitCurvesSupported)), MemberData(nameof(TestCurvesFull))]
        public static void TestExplicitCurves(CurveDef curveDef)
        {
            using (ECDsa ec1 = ECDsaFactory.Create(curveDef.Curve))
            {
                ECParameters param1 = ec1.ExportExplicitParameters(curveDef.IncludePrivate);
                VerifyExplicitCurve(param1, ec1, curveDef);

                using (ECDsa ec2 = ECDsaFactory.Create())
                {
                    ec2.ImportParameters(param1);
                    ECParameters param2 = ec2.ExportExplicitParameters(curveDef.IncludePrivate);
                    VerifyExplicitCurve(param1, ec1, curveDef);

                    AssertEqual(param1, param2);
                }
            }
        }

        [ConditionalTheory(nameof(ECExplicitCurvesSupported)), MemberData(nameof(TestCurves))]
        public static void TestExplicitCurvesSignVerify(CurveDef curveDef)
        {
            using (ECDsa ec1 = ECDsaFactory.Create(curveDef.Curve))
            {
                byte[] data = new byte[0x10];
                byte[] sig1 = ec1.SignData(data, 0, data.Length, HashAlgorithmName.SHA1);

                bool verified;
                verified = ec1.VerifyData(data, sig1, HashAlgorithmName.SHA1);
                Assert.True(verified);

                using (ECDsa ec2 = ECDsaFactory.Create())
                {
                    ec2.ImportParameters(ec1.ExportExplicitParameters(true));
                    Assert.Equal(ec1.KeySize, ec2.KeySize);

                    byte[] sig2 = ec2.SignData(data, 0, data.Length, HashAlgorithmName.SHA1);
                    verified = ec2.VerifyData(data, sig2, HashAlgorithmName.SHA1);
                    Assert.True(verified);

                    // Verify key is compatible other signature
                    verified = ec2.VerifyData(data, sig1, HashAlgorithmName.SHA1);
                    Assert.True(verified);
                    verified = ec1.VerifyData(data, sig2, HashAlgorithmName.SHA1);
                    Assert.True(verified);

                    // Verify with no private key
                    using (ECDsa ec3 = ECDsaFactory.Create())
                    {
                        ec3.ImportParameters(ec2.ExportExplicitParameters(false));
                        Assert.Equal(ec2.KeySize, ec3.KeySize);
                        verified = ec3.VerifyData(data, sig1, HashAlgorithmName.SHA1);
                        Assert.True(verified);
                    }
                }

                // Ensure negative result
                unchecked
                {
                    sig1[sig1.Length - 1]++;
                }
                verified = ec1.VerifyData(data, sig1, HashAlgorithmName.SHA1);
                Assert.False(verified);
            }
        }

        [Fact]
        public static void TestNamedCurveNegative()
        {
            Assert.Throws<ArgumentNullException>(() => ECCurve.CreateFromFriendlyName(null));
            Assert.Throws<ArgumentNullException>(() => ECCurve.CreateFromValue(null));
            AssertExtensions.Throws<ArgumentException>(null, () => ECCurve.CreateFromFriendlyName(""));
            Assert.Throws<PlatformNotSupportedException>(() => ECDsaFactory.Create(ECCurve.CreateFromFriendlyName("Invalid")).ExportExplicitParameters(false));
            AssertExtensions.Throws<ArgumentException>(null, () => ECCurve.CreateFromValue(""));
            Assert.Throws<PlatformNotSupportedException>(() => ECDsaFactory.Create(ECCurve.CreateFromValue("Invalid")).ExportExplicitParameters(false));
            AssertExtensions.Throws<ArgumentException>(null, () => ECCurve.CreateFromOid(new Oid(null, null)));
            AssertExtensions.Throws<ArgumentException>(null, () => ECCurve.CreateFromOid(new Oid("", "")));
        }

        [Fact]
        public static void TestKeySizeCreateKey()
        {
            using (ECDsa ec = ECDsaFactory.Create(ECCurve.NamedCurves.nistP256))
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

        [ConditionalFact(nameof(ECExplicitCurvesSupported))]
        [SkipOnPlatform(TestPlatforms.Android, "Android does not validate curve parameters")]
        public static void TestExplicitImportValidationNegative()
        {
            unchecked
            {
                using (ECDsa ec = ECDsaFactory.Create())
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

        [ConditionalFact(nameof(ECDsa224Available))]
        public static void TestNamedImportValidationNegative()
        {
            unchecked
            {
                using(ECDsa ec = ECDsaFactory.Create())
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

        [ConditionalFact(nameof(ECExplicitCurvesSupported))]
        public static void TestGeneralExportWithExplicitParameters()
        {
            using (ECDsa ecdsa = ECDsaFactory.Create())
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

        [ConditionalFact(nameof(ECDsa224Available))]
        public static void TestNamedCurveWithExplicitKey()
        {
            using (ECDsa ec = ECDsaFactory.Create())
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

            using (ECDsa iut = ECDsaFactory.Create())
            using (ECDsa cavs = ECDsaFactory.Create())
            {
                iut.ImportParameters(iutParameters);
                cavs.ImportParameters(iut.ExportParameters(false));

                Assert.ThrowsAny<CryptographicException>(() => cavs.ExportParameters(true));

                if (ECExplicitCurvesSupported)
                {
                    Assert.ThrowsAny<CryptographicException>(() => cavs.ExportExplicitParameters(true));
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

            using (ECDsa ecdsa = ECDsaFactory.Create())
            {
                ecdsa.ImportParameters(limitedPrivateParameters);
                ECParameters exportedParameters = ecdsa.ExportParameters(true);

                Assert.Equal(expectedX, exportedParameters.Q.X);
                Assert.Equal(expectedY, exportedParameters.Q.Y);
                Assert.Equal(limitedPrivateParameters.D, exportedParameters.D);
            }
        }

        private static void VerifyNamedCurve(ECParameters parameters, ECDsa ec, int keySize, bool includePrivate)
        {
            parameters.Validate();
            Assert.True(parameters.Curve.IsNamed, "parameters.Curve.IsNamed");
            Assert.Equal(keySize, ec.KeySize);
            Assert.True(
                includePrivate && parameters.D.Length > 0 ||
                !includePrivate && parameters.D == null,
                "Private key is " + (includePrivate ? "present" : "absent"));

            if (includePrivate)
                ec.Exercise();

            // Ensure the key doesn't get regenerated after export
            ECParameters paramSecondExport = ec.ExportParameters(includePrivate);
            paramSecondExport.Validate();
            AssertEqual(parameters, paramSecondExport);
        }

        private static void VerifyExplicitCurve(ECParameters parameters, ECDsa ec, CurveDef curveDef)
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
                Assert.Equal(curve.A.Length,curve.Prime.Length);
            }

            if (curveDef.IncludePrivate)
                ec.Exercise();

            // Ensure the key doesn't get regenerated after export
            ECParameters paramSecondExport = ec.ExportExplicitParameters(curveDef.IncludePrivate);
            AssertEqual(parameters, paramSecondExport);
        }
#endif
    }
}
