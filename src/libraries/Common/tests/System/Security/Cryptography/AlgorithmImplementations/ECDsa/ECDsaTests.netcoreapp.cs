// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.Tests;
using Xunit;

namespace System.Security.Cryptography.EcDsa.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/62547", TestPlatforms.Android)]
    public sealed class ECDsaTests_Span : ECDsaTests
    {
        protected override bool VerifyData(ECDsa ecdsa, byte[] data, int offset, int count, byte[] signature, HashAlgorithmName hashAlgorithm) =>
            ecdsa.VerifyData(new ReadOnlySpan<byte>(data, offset, count), signature, hashAlgorithm);

        protected override byte[] SignData(ECDsa ecdsa, byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm) =>
            TryWithOutputArray(dest => ecdsa.TrySignData(new ReadOnlySpan<byte>(data, offset, count), dest, hashAlgorithm, out int bytesWritten) ? (true, bytesWritten) : (false, 0));

        protected override void UseAfterDispose(ECDsa ecdsa, byte[] data, byte[] sig)
        {
            base.UseAfterDispose(ecdsa, data, sig);
            byte[] hash = new byte[32];

            Assert.Throws<ObjectDisposedException>(() => ecdsa.VerifyHash(hash.AsSpan(), sig.AsSpan()));
            Assert.Throws<ObjectDisposedException>(() => ecdsa.TrySignHash(hash, sig, out _));
        }

        [Theory, MemberData(nameof(RealImplementations))]
        public void SignData_InvalidArguments_Throws(ECDsa ecdsa)
        {
            AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () => ecdsa.TrySignData(ReadOnlySpan<byte>.Empty, Span<byte>.Empty, new HashAlgorithmName(null), out int bytesWritten));
            AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () => ecdsa.TrySignData(ReadOnlySpan<byte>.Empty, Span<byte>.Empty, new HashAlgorithmName(""), out int bytesWritten));
            Assert.ThrowsAny<CryptographicException>(() => ecdsa.TrySignData(ReadOnlySpan<byte>.Empty, Span<byte>.Empty, new HashAlgorithmName(Guid.NewGuid().ToString("N")), out int bytesWritten));
        }

        [Theory, MemberData(nameof(RealImplementations))]
        public void VerifyData_InvalidArguments_Throws(ECDsa ecdsa)
        {
            AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () => ecdsa.VerifyData(ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, new HashAlgorithmName(null)));
            AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () => ecdsa.VerifyData(ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, new HashAlgorithmName("")));
            Assert.ThrowsAny<CryptographicException>(() => ecdsa.VerifyData(ReadOnlySpan<byte>.Empty, Span<byte>.Empty, new HashAlgorithmName(Guid.NewGuid().ToString("N"))));
        }

        private static byte[] TryWithOutputArray(Func<byte[], (bool, int)> func)
        {
            for (int length = 1; ; length = checked(length * 2))
            {
                var result = new byte[length];
                var (success, bytesWritten) = func(result);
                if (success)
                {
                    Array.Resize(ref result, bytesWritten);
                    return result;
                }
            }
        }
    }

    public abstract partial class ECDsaTests : ECDsaTestsBase
    {
        [Fact]
        public void KeySizeProp()
        {
            using (ECDsa e = ECDsaFactory.Create())
            {
                e.KeySize = 384;
                Assert.Equal(384, e.KeySize);
                ECParameters p384 = e.ExportParameters(false);
                Assert.True(p384.Curve.IsNamed);
                p384.Validate();

                e.KeySize = 521;
                Assert.Equal(521, e.KeySize);
                ECParameters p521 = e.ExportParameters(false);
                Assert.True(p521.Curve.IsNamed);
                p521.Validate();

                // Ensure the key was regenerated
                Assert.NotEqual(p384.Curve.Oid.FriendlyName, p521.Curve.Oid.FriendlyName);
            }
        }

        [Theory, MemberData(nameof(TestNewCurves))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51332", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public void TestRegenKeyExplicit(CurveDef curveDef)
        {
            ECParameters param, param2;
            ECDsa ec, newEc;

            using (ec = ECDsaFactory.Create(curveDef.Curve))
            {
                param = ec.ExportExplicitParameters(true);
                Assert.NotNull(param.D);
                using (newEc = ECDsaFactory.Create())
                {
                    newEc.ImportParameters(param);

                    // The curve name is not flowed on explicit export\import (by design) so this excercises logic
                    // that regenerates based on current curve values
                    newEc.GenerateKey(param.Curve);
                    param2 = newEc.ExportExplicitParameters(true);

                    // Only curve should match
                    ComparePrivateKey(param, param2, false);
                    ComparePublicKey(param.Q, param2.Q, false);
                    CompareCurve(param.Curve, param2.Curve);

                    // Specify same curve name
                    newEc.GenerateKey(curveDef.Curve);
                    Assert.Equal(curveDef.KeySize, newEc.KeySize);
                    param2 = newEc.ExportExplicitParameters(true);

                    // Only curve should match
                    ComparePrivateKey(param, param2, false);
                    ComparePublicKey(param.Q, param2.Q, false);
                    CompareCurve(param.Curve, param2.Curve);

                    // Specify different curve than current
                    if (param.Curve.IsPrime)
                    {
                        if (curveDef.Curve.IsNamed &&
                            curveDef.Curve.Oid.FriendlyName != ECCurve.NamedCurves.nistP256.Oid.FriendlyName)
                        {
                            // Specify different curve (nistP256) by explicit value
                            newEc.GenerateKey(ECCurve.NamedCurves.nistP256);
                            Assert.Equal(256, newEc.KeySize);
                            param2 = newEc.ExportExplicitParameters(true);
                            // Keys should not match
                            ComparePrivateKey(param, param2, false);
                            ComparePublicKey(param.Q, param2.Q, false);
                            // P,X,Y (and others) should not match
                            Assert.True(param2.Curve.IsPrime);
                            Assert.NotEqual(param.Curve.Prime, param2.Curve.Prime);
                            Assert.NotEqual(param.Curve.G.X, param2.Curve.G.X);
                            Assert.NotEqual(param.Curve.G.Y, param2.Curve.G.Y);

                            // Reset back to original
                            newEc.GenerateKey(param.Curve);
                            Assert.Equal(curveDef.KeySize, newEc.KeySize);
                            ECParameters copyOfParam1 = newEc.ExportExplicitParameters(true);
                            // Only curve should match
                            ComparePrivateKey(param, copyOfParam1, false);
                            ComparePublicKey(param.Q, copyOfParam1.Q, false);
                            CompareCurve(param.Curve, copyOfParam1.Curve);

                            // Set back to nistP256
                            newEc.GenerateKey(param2.Curve);
                            Assert.Equal(256, newEc.KeySize);
                            param2 = newEc.ExportExplicitParameters(true);
                            // Keys should not match
                            ComparePrivateKey(param, param2, false);
                            ComparePublicKey(param.Q, param2.Q, false);
                            // P,X,Y (and others) should not match
                            Assert.True(param2.Curve.IsPrime);
                            Assert.NotEqual(param.Curve.Prime, param2.Curve.Prime);
                            Assert.NotEqual(param.Curve.G.X, param2.Curve.G.X);
                            Assert.NotEqual(param.Curve.G.Y, param2.Curve.G.Y);
                        }
                    }
                    else if (param.Curve.IsCharacteristic2)
                    {
                        if (curveDef.Curve.Oid.Value != ECDSA_Sect193r1_OID_VALUE)
                        {
                            if (ECDsaFactory.IsCurveValid(new Oid(ECDSA_Sect193r1_OID_VALUE)))
                            {
                                // Specify different curve by name
                                newEc.GenerateKey(ECCurve.CreateFromValue(ECDSA_Sect193r1_OID_VALUE));
                                Assert.Equal(193, newEc.KeySize);
                                param2 = newEc.ExportExplicitParameters(true);
                                // Keys should not match
                                ComparePrivateKey(param, param2, false);
                                ComparePublicKey(param.Q, param2.Q, false);
                                // Polynomial,X,Y (and others) should not match
                                Assert.True(param2.Curve.IsCharacteristic2);
                                Assert.NotEqual(param.Curve.Polynomial, param2.Curve.Polynomial);
                                Assert.NotEqual(param.Curve.G.X, param2.Curve.G.X);
                                Assert.NotEqual(param.Curve.G.Y, param2.Curve.G.Y);
                            }
                        }
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(TestCurves))]
        public void TestRegenKeyNamed(CurveDef curveDef)
        {
            ECParameters param, param2;
            ECDsa ec;

            using (ec = ECDsaFactory.Create(curveDef.Curve))
            {
                param = ec.ExportParameters(true);
                Assert.NotNull(param.D);
                param.Validate();

                ec.GenerateKey(param.Curve);
                param2 = ec.ExportParameters(true);
                param2.Validate();

                // Only curve should match
                ComparePrivateKey(param, param2, false);
                ComparePublicKey(param.Q, param2.Q, false);
                CompareCurve(param.Curve, param2.Curve);
            }
        }

        [ConditionalFact(nameof(ECExplicitCurvesSupported))]
        public void TestRegenKeyNistP256()
        {
            ECParameters param, param2;
            ECDsa ec;

            using (ec = ECDsaFactory.Create(256))
            {
                param = ec.ExportExplicitParameters(true);
                Assert.NotNull(param.D);

                ec.GenerateKey(param.Curve);
                param2 = ec.ExportExplicitParameters(true);

                // Only curve should match
                ComparePrivateKey(param, param2, false);
                ComparePublicKey(param.Q, param2.Q, false);
                CompareCurve(param.Curve, param2.Curve);
            }
        }

        [Theory]
        [MemberData(nameof(TestCurves))]
        public void TestChangeFromNamedCurveToKeySize(CurveDef curveDef)
        {
            if (!curveDef.Curve.IsNamed)
                return;

            using (ECDsa ec = ECDsaFactory.Create(curveDef.Curve))
            {
                ECParameters param = ec.ExportParameters(false);

                // Avoid comparing against same key as in curveDef
                if (ec.KeySize != 384 && ec.KeySize != 521)
                {
                    ec.KeySize = 384;
                    ECParameters param384 = ec.ExportParameters(false);
                    Assert.NotEqual(param.Curve.Oid.FriendlyName, param384.Curve.Oid.FriendlyName);
                    Assert.Equal(384, ec.KeySize);

                    ec.KeySize = 521;
                    ECParameters param521 = ec.ExportParameters(false);
                    Assert.NotEqual(param384.Curve.Oid.FriendlyName, param521.Curve.Oid.FriendlyName);
                    Assert.Equal(521, ec.KeySize);
                }
            }
        }

        [ConditionalFact(nameof(ECExplicitCurvesSupported))]
        public void TestPositive256WithExplicitParameters()
        {
            using (ECDsa ecdsa = ECDsaFactory.Create())
            {
                ecdsa.ImportParameters(EccTestData.GetNistP256ExplicitTestData());
                Verify256(ecdsa, true);
            }
        }

        [Fact]
        public void TestNegative256WithRandomKey()
        {
            using (ECDsa ecdsa = ECDsaFactory.Create(ECCurve.NamedCurves.nistP256))
            {
                Verify256(ecdsa, false); // will not match because of randomness
            }
        }

        [Fact]
        public void PublicKey_CannotSign()
        {
            using (ECDsa ecdsaPriv = ECDsaFactory.Create())
            using (ECDsa ecdsa = ECDsaFactory.Create())
            {
                ECParameters keyParameters = ecdsaPriv.ExportParameters(false);
                ecdsa.ImportParameters(keyParameters);

                Assert.ThrowsAny<CryptographicException>(
                    () => SignData(ecdsa, new byte[] { 1, 2, 3, 4, 5 }, HashAlgorithmName.SHA256));
            }
        }
    }
}
