// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Reflection;
using System.Security.Cryptography.EcDsa.Tests;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public class ECDsaTests
    {
        [Fact]
        public void Create_InvalidArgument_Throws()
        {
#pragma warning disable SYSLIB0045 // String factory methods are obsolete
            AssertExtensions.Throws<ArgumentNullException>("algorithm", () => ECDsa.Create(null));
            Assert.Null(ECDsa.Create(Guid.NewGuid().ToString("N")));
#pragma warning restore SYSLIB0045 // String factory methods are obsolete
        }

        [Fact]
        public void Create_InvalidECCurveFriendlyName_ThrowsPlatformNotSupportedException()
        {
            ECCurve curve = ECCurve.CreateFromFriendlyName("bad potato");
            PlatformNotSupportedException pnse = Assert.Throws<PlatformNotSupportedException>(() => ECDsa.Create(curve));
            Assert.Contains("'bad potato'", pnse.Message);
        }

        [Fact]
        public void NotSupportedBaseMethods_Throw()
        {
            using (var ecdsa = new OverrideAbstractECDsa(ECDsaFactory.Create()))
            {
                Assert.Throws<NotSupportedException>(() => ecdsa.ExportParameters(false));
                Assert.Throws<NotSupportedException>(() => ecdsa.ExportExplicitParameters(false));
                Assert.Throws<NotSupportedException>(() => ecdsa.ImportParameters(default(ECParameters)));
                Assert.Throws<NotSupportedException>(() => ecdsa.GenerateKey(default(ECCurve)));

                Assert.Throws<NotImplementedException>(() => ecdsa.FromXmlString(null));
                Assert.Throws<NotImplementedException>(() => ecdsa.ToXmlString(false));
            }
        }

        [Fact]
        public void BaseProperties_ExpectedValues()
        {
            using (var ecdsa = new OverrideAbstractECDsa(ECDsaFactory.Create()))
            {
                Assert.Null(ecdsa.KeyExchangeAlgorithm);
                Assert.Equal("ECDsa", ecdsa.SignatureAlgorithm);
            }
        }

        [Fact]
        public void Array_SignData_VerifyData_UsesHashDataAndSignHashAndVerifyHash()
        {
            using (var ecdsa = new OverrideAbstractECDsa(ECDsaFactory.Create()))
            {
                AssertExtensions.Throws<ArgumentNullException>("data", () => ecdsa.SignData((byte[])null, HashAlgorithmName.SHA1));
                AssertExtensions.Throws<ArgumentNullException>("data", () => ecdsa.SignData(null, 0, 0, HashAlgorithmName.SHA1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => ecdsa.SignData(new byte[1], -1, 0, HashAlgorithmName.SHA1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => ecdsa.SignData(new byte[1], 2, 0, HashAlgorithmName.SHA1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => ecdsa.SignData(new byte[1], 0, -1, HashAlgorithmName.SHA1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => ecdsa.SignData(new byte[1], 0, 2, HashAlgorithmName.SHA1));
                AssertExtensions.Throws<ArgumentNullException>("hashAlgorithm", () => ecdsa.SignData(new byte[1], 0, 1, new HashAlgorithmName(null)));
                AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () => ecdsa.SignData(new byte[1], 0, 1, new HashAlgorithmName("")));

                AssertExtensions.Throws<ArgumentNullException>("data", () => ecdsa.VerifyData((byte[])null, null, HashAlgorithmName.SHA1));
                AssertExtensions.Throws<ArgumentNullException>("data", () => ecdsa.VerifyData(null, 0, 0, null, HashAlgorithmName.SHA1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => ecdsa.VerifyData(new byte[1], -1, 0, null, HashAlgorithmName.SHA1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => ecdsa.VerifyData(new byte[1], 2, 0, null, HashAlgorithmName.SHA1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => ecdsa.VerifyData(new byte[1], 0, -1, null, HashAlgorithmName.SHA1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => ecdsa.VerifyData(new byte[1], 0, 2, null, HashAlgorithmName.SHA1));
                AssertExtensions.Throws<ArgumentNullException>("signature", () => ecdsa.VerifyData(new byte[1], 0, 1, null, HashAlgorithmName.SHA1));
                AssertExtensions.Throws<ArgumentNullException>("hashAlgorithm", () => ecdsa.VerifyData(new byte[1], 0, 1, new byte[1], new HashAlgorithmName(null)));
                AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () => ecdsa.VerifyData(new byte[1], 0, 1, new byte[1], new HashAlgorithmName("")));

                var input = new byte[1024];
                Random.Shared.NextBytes(input);

                byte[] result = ecdsa.SignData(input, HashAlgorithmName.SHA256);
                Assert.NotNull(result);
                Assert.NotEmpty(result);

                Assert.False(ecdsa.VerifyData(input.AsSpan(1).ToArray(), result, HashAlgorithmName.SHA256));
                Assert.True(ecdsa.VerifyData(input, result, HashAlgorithmName.SHA256));
            }
        }

        [Fact]
        public void Stream_SignData_VerifyData_UsesHashDataAndSignHashAndVerifyHash()
        {
            using (var ecdsa = new OverrideAbstractECDsa(ECDsaFactory.Create()))
            {
                AssertExtensions.Throws<ArgumentNullException>("data", () => ecdsa.SignData((Stream)null, HashAlgorithmName.SHA1));
                AssertExtensions.Throws<ArgumentNullException>("hashAlgorithm", () => ecdsa.SignData(new MemoryStream(new byte[1]), new HashAlgorithmName(null)));
                AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () => ecdsa.SignData(new MemoryStream(new byte[1]), new HashAlgorithmName("")));

                AssertExtensions.Throws<ArgumentNullException>("data", () => ecdsa.VerifyData((Stream)null, null, HashAlgorithmName.SHA1));
                AssertExtensions.Throws<ArgumentNullException>("signature", () => ecdsa.VerifyData(new MemoryStream(new byte[1]), null, HashAlgorithmName.SHA1));
                AssertExtensions.Throws<ArgumentNullException>("hashAlgorithm", () => ecdsa.VerifyData(new MemoryStream(new byte[1]), new byte[1], new HashAlgorithmName(null)));
                AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () => ecdsa.VerifyData(new MemoryStream(new byte[1]), new byte[1], new HashAlgorithmName("")));

                var input = new byte[1024];
                Random.Shared.NextBytes(input);

                byte[] result = ecdsa.SignData(new MemoryStream(input), HashAlgorithmName.SHA256);
                Assert.NotNull(result);
                Assert.NotEmpty(result);

                Assert.False(ecdsa.VerifyData(new MemoryStream(input.AsSpan(1).ToArray()), result, HashAlgorithmName.SHA256));
                Assert.True(ecdsa.VerifyData(new MemoryStream(input), result, HashAlgorithmName.SHA256));
            }
        }

        [Fact]
        public void Span_TrySignData_VerifyData_UsesTryHashDataAndTrySignHashAndTryVerifyHash()
        {
            using (var ecdsa = new OverrideAbstractECDsa(ECDsaFactory.Create()))
            {
                AssertExtensions.Throws<ArgumentNullException>("hashAlgorithm", () => ecdsa.TrySignData(new byte[1], new byte[1], new HashAlgorithmName(null), out int bytesWritten));
                AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () => ecdsa.TrySignData(new byte[1], new byte[1], new HashAlgorithmName(""), out int bytesWritten));
                AssertExtensions.Throws<ArgumentNullException>("hashAlgorithm", () => ecdsa.VerifyData((ReadOnlySpan<byte>)new byte[1], new byte[1], new HashAlgorithmName(null)));
                AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () => ecdsa.VerifyData((ReadOnlySpan<byte>)new byte[1], new byte[1], new HashAlgorithmName("")));

                var input = new byte[1024];
                Random.Shared.NextBytes(input);

                byte[] output = new byte[1];
                int outputLength;
                while (!ecdsa.TrySignData(input, output, HashAlgorithmName.SHA256, out outputLength))
                {
                    output = new byte[output.Length * 2];
                }

                Assert.False(ecdsa.VerifyData((ReadOnlySpan<byte>)input, new ReadOnlySpan<byte>(output, 0, outputLength - 1), HashAlgorithmName.SHA256));
                Assert.True(ecdsa.VerifyData((ReadOnlySpan<byte>)input, new ReadOnlySpan<byte>(output, 0, outputLength), HashAlgorithmName.SHA256));
            }
        }

        private sealed class OverrideAbstractECDsa : ECDsa
        {
            private readonly ECDsa _ecdsa;

            public OverrideAbstractECDsa(ECDsa ecdsa) => _ecdsa = ecdsa;

            protected override void Dispose(bool disposing)
            {
                _ecdsa.Dispose();
                base.Dispose(disposing);
            }

            public override int KeySize
            {
                get => _ecdsa.KeySize;
                set => _ecdsa.KeySize = value;
            }

            public override byte[] SignHash(byte[] hash) => _ecdsa.SignHash(hash);

            public override bool VerifyHash(byte[] hash, byte[] signature) => _ecdsa.VerifyHash(hash, signature);

            public byte[] BaseHashData(Stream data, HashAlgorithmName hashAlgorithm) =>
                base.HashData(data, hashAlgorithm);

            public byte[] BaseHashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm) =>
                base.HashData(data, offset, count, hashAlgorithm);

            protected override byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm) =>
                (byte[])_ecdsa.GetType().GetMethod(
                    nameof(HashData),
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    new Type[] { typeof(Stream), typeof(HashAlgorithmName) },
                    null)
                .Invoke(_ecdsa, new object[] { data, hashAlgorithm });

            protected override byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm) =>
                (byte[])_ecdsa.GetType().GetMethod(
                    nameof(HashData),
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    new Type[] { typeof(byte[]), typeof(int), typeof(int), typeof(HashAlgorithmName) },
                    null)
                .Invoke(_ecdsa, new object[] { data, offset, count, hashAlgorithm });
        }
    }
}
