// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Reflection;
using System.Security.Cryptography.Dsa.Tests;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst, "Not supported on Browser/iOS/tvOS/MacCatalyst")]
    public class DSATests
    {
        public static bool SupportsKeyGeneration => DSAFactory.SupportsKeyGeneration;

        [ConditionalFact(nameof(SupportsKeyGeneration))]
        public void TryCreateSignature_UsesCreateSignature()
        {
            var input = new byte[1024];
            new Random(42).NextBytes(input);
            int bytesWritten = 0;

            using (var wrapperDsa = new OverrideAbstractDSA(DSA.Create(1024)))
            {
                byte[] initialSig = wrapperDsa.CreateSignature(input);
                byte[] actualSig = new byte[initialSig.Length];

                Assert.False(wrapperDsa.TryCreateSignature(input, new Span<byte>(actualSig, 0, actualSig.Length - 1), out bytesWritten));
                Assert.Equal(0, bytesWritten);
                Assert.All(actualSig, b => Assert.Equal(0, b));

                Assert.True(wrapperDsa.TryCreateSignature(input, actualSig, out bytesWritten));
                Assert.Equal(initialSig.Length, bytesWritten);
                Assert.Contains(actualSig, b => b != 0);
            }
        }

        [Fact]
        public void SignData_InvalidArguments_Throws()
        {
            using (var wrapperDsa = new OverrideAbstractDSA(DSA.Create(1024)))
            {
                AssertExtensions.Throws<ArgumentNullException>("data", () => wrapperDsa.SignData((byte[])null, HashAlgorithmName.SHA1));
                AssertExtensions.Throws<ArgumentNullException>("data", () => wrapperDsa.SignData((Stream)null, HashAlgorithmName.SHA1));
                AssertExtensions.Throws<ArgumentNullException>("data", () => wrapperDsa.SignData(null, 0, 0, HashAlgorithmName.SHA1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => wrapperDsa.SignData(new byte[1], -1, 0, HashAlgorithmName.SHA1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => wrapperDsa.SignData(new byte[1], 2, 0, HashAlgorithmName.SHA1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => wrapperDsa.SignData(new byte[1], 0, -1, HashAlgorithmName.SHA1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => wrapperDsa.SignData(new byte[1], 0, 2, HashAlgorithmName.SHA1));
                AssertExtensions.Throws<ArgumentNullException>("hashAlgorithm", () => wrapperDsa.SignData(new byte[1], new HashAlgorithmName(null)));
                AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () => wrapperDsa.SignData(new byte[1], new HashAlgorithmName("")));
                AssertExtensions.Throws<ArgumentNullException>("hashAlgorithm", () => wrapperDsa.SignData(new MemoryStream(new byte[1]), new HashAlgorithmName(null)));
                AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () => wrapperDsa.SignData(new MemoryStream(new byte[1]), new HashAlgorithmName("")));
            }
        }

        [ConditionalFact(nameof(SupportsKeyGeneration))]
        public void TrySignData_UsesTryHashDataAndTryCreateSignature()
        {
            var input = new byte[1024];
            new Random(42).NextBytes(input);
            int bytesWritten = 0;

            using (var wrapperDsa = new OverrideAbstractDSA(DSA.Create(1024)))
            {
                byte[] initialSig = wrapperDsa.SignData(input, HashAlgorithmName.SHA1);
                byte[] actualSig = new byte[initialSig.Length];

                AssertExtensions.Throws<ArgumentNullException>("hashAlgorithm", () => wrapperDsa.TrySignData(new byte[1], new byte[1], new HashAlgorithmName(null), out int _));
                AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () => wrapperDsa.TrySignData(new byte[1], new byte[1], new HashAlgorithmName(""), out int _));

                Assert.False(wrapperDsa.TrySignData(input, new Span<byte>(actualSig, 0, 1), HashAlgorithmName.SHA1, out bytesWritten));
                Assert.Equal(0, bytesWritten);
                Assert.All(actualSig, b => Assert.Equal(0, b));

                Assert.True(wrapperDsa.TrySignData(input, actualSig, HashAlgorithmName.SHA1, out bytesWritten));
                Assert.Equal(initialSig.Length, bytesWritten);
                Assert.Contains(actualSig, b => b != 0);
            }
        }

        [ConditionalFact(nameof(SupportsKeyGeneration))]
        public void VerifyData_Array_UsesHashDataAndVerifySignature()
        {
            var input = new byte[1024];
            new Random(42).NextBytes(input);

            using (var wrapperDsa = new OverrideAbstractDSA(DSA.Create(1024)))
            {
                AssertExtensions.Throws<ArgumentNullException>("data", () => wrapperDsa.VerifyData((byte[])null, null, HashAlgorithmName.SHA1));
                AssertExtensions.Throws<ArgumentNullException>("data", () => wrapperDsa.VerifyData(null, 0, 0, null, HashAlgorithmName.SHA1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => wrapperDsa.VerifyData(new byte[1], -1, 0, null, HashAlgorithmName.SHA1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => wrapperDsa.VerifyData(new byte[1], 2, 0, null, HashAlgorithmName.SHA1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => wrapperDsa.VerifyData(new byte[1], 0, -1, null, HashAlgorithmName.SHA1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => wrapperDsa.VerifyData(new byte[1], 0, 2, null, HashAlgorithmName.SHA1));
                AssertExtensions.Throws<ArgumentNullException>("signature", () => wrapperDsa.VerifyData(new byte[1], 0, 1, null, HashAlgorithmName.SHA1));
                AssertExtensions.Throws<ArgumentNullException>("hashAlgorithm", () => wrapperDsa.VerifyData(new byte[1], new byte[1], new HashAlgorithmName(null)));
                AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () => wrapperDsa.VerifyData(new byte[1], new byte[1], new HashAlgorithmName("")));

                byte[] signature = wrapperDsa.SignData(input, HashAlgorithmName.SHA1);
                Assert.True(wrapperDsa.VerifyData(input.AsSpan(), signature, HashAlgorithmName.SHA1));
                Assert.False(wrapperDsa.VerifyData(input.AsSpan(), signature.AsSpan(0, signature.Length - 1), HashAlgorithmName.SHA1));
            }
        }

        [ConditionalFact(nameof(SupportsKeyGeneration))]
        public void VerifyData_Stream_UsesHashDataAndVerifySignature()
        {
            var input = new byte[1024];
            new Random(42).NextBytes(input);

            using (var wrapperDsa = new OverrideAbstractDSA(DSA.Create(1024)))
            {
                AssertExtensions.Throws<ArgumentNullException>("data", () => wrapperDsa.VerifyData((Stream)null, null, HashAlgorithmName.SHA1));
                AssertExtensions.Throws<ArgumentNullException>("signature", () => wrapperDsa.VerifyData(new MemoryStream(new byte[1]), null, HashAlgorithmName.SHA1));
                AssertExtensions.Throws<ArgumentNullException>("hashAlgorithm", () => wrapperDsa.VerifyData(new MemoryStream(new byte[1]), new byte[1], new HashAlgorithmName(null)));
                AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () => wrapperDsa.VerifyData(new MemoryStream(new byte[1]), new byte[1], new HashAlgorithmName("")));

                byte[] signature = wrapperDsa.SignData(new MemoryStream(input), HashAlgorithmName.SHA1);
                Assert.True(wrapperDsa.VerifyData(new MemoryStream(input), signature, HashAlgorithmName.SHA1));
                Assert.False(wrapperDsa.VerifyData(new MemoryStream(input), signature.AsSpan(0, signature.Length - 1).ToArray(), HashAlgorithmName.SHA1));
            }
        }

        [ConditionalFact(nameof(SupportsKeyGeneration))]
        public void VerifyData_Span_UsesTryHashDataAndVerifySignature()
        {
            var input = new byte[1024];
            new Random(42).NextBytes(input);

            using (var wrapperDsa = new OverrideAbstractDSA(DSA.Create(1024)))
            {
                AssertExtensions.Throws<ArgumentNullException>("hashAlgorithm", () => wrapperDsa.VerifyData((Span<byte>)new byte[1], new byte[1], new HashAlgorithmName(null)));
                AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () => wrapperDsa.VerifyData((Span<byte>)new byte[1], new byte[1], new HashAlgorithmName("")));

                byte[] signature = wrapperDsa.SignData(input, HashAlgorithmName.SHA1);
                Assert.True(wrapperDsa.VerifyData(input.AsSpan(), signature, HashAlgorithmName.SHA1));
                Assert.False(wrapperDsa.VerifyData(input.AsSpan(), signature.AsSpan(0, signature.Length - 1), HashAlgorithmName.SHA1));
            }
        }

        private sealed class EmptyDSA : DSA
        {
            public override byte[] CreateSignature(byte[] rgbHash) => throw new NotImplementedException();
            public override void ImportParameters(DSAParameters parameters) => throw new NotImplementedException();
            public override DSAParameters ExportParameters(bool includePrivateParameters) => throw new NotImplementedException();
            public override bool VerifySignature(byte[] rgbHash, byte[] rgbSignature) => throw new NotImplementedException();

            public new byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm) =>
                base.HashData(data, offset, count, hashAlgorithm);
            public new byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm) =>
                base.HashData(data, hashAlgorithm);
            public new bool TryHashData(ReadOnlySpan<byte> source, Span<byte> destination, HashAlgorithmName hashAlgorithm, out int bytesWritten) =>
                base.TryHashData(source, destination, hashAlgorithm, out bytesWritten);
        }

        private sealed class OverrideAbstractDSA : DSA
        {
            private readonly DSA _dsa;

            public OverrideAbstractDSA(DSA dsa) => _dsa = dsa;

            protected override void Dispose(bool disposing)
            {
                _dsa.Dispose();
                base.Dispose(disposing);
            }

            public override byte[] CreateSignature(byte[] rgbHash) => _dsa.CreateSignature(rgbHash);
            public override DSAParameters ExportParameters(bool includePrivateParameters) => _dsa.ExportParameters(includePrivateParameters);
            public override void ImportParameters(DSAParameters parameters) => _dsa.ImportParameters(parameters);
            public override bool VerifySignature(byte[] rgbHash, byte[] rgbSignature) => _dsa.VerifySignature(rgbHash, rgbSignature);
            protected override byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm) =>
                (byte[])typeof(DSA).GetMethod(
                    nameof(HashData),
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    new Type[] { typeof(Stream), typeof(HashAlgorithmName) },
                    null)
                .Invoke(_dsa, new object[] { data, hashAlgorithm });
            protected override byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm) =>
                (byte[])typeof(DSA).GetMethod(
                    nameof(HashData),
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    new Type[] { typeof(byte[]), typeof(int), typeof(int), typeof(HashAlgorithmName) },
                    null)
                .Invoke(_dsa, new object[] { data, offset, count, hashAlgorithm });
        }
    }
}
