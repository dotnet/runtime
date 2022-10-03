// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public abstract class SignatureAlgorithmHashTests<TAlgorithm> where TAlgorithm : IHashSigningAlgorithm, new()
    {
        [Theory]
        [InlineData(nameof(HashAlgorithmName.MD5))]
        [InlineData(nameof(HashAlgorithmName.SHA1))]
        [InlineData(nameof(HashAlgorithmName.SHA256))]
        [InlineData(nameof(HashAlgorithmName.SHA384))]
        [InlineData(nameof(HashAlgorithmName.SHA512))]
        public void TryHashData_UserDerived_UsesHashData(string hashAlgorithmName)
        {
            HashAlgorithmName hashAlgorithm = new HashAlgorithmName(hashAlgorithmName);
            byte[] destination = new byte[SHA512.HashSizeInBytes];

            using (TAlgorithm algorithm = new TAlgorithm())
            {
                byte[] data = new byte[1];
                bool result = algorithm.TryHashDataImpl(data, destination, hashAlgorithm, out _);
                Assert.True(result, nameof(algorithm.TryHashDataImpl));
                Assert.Equal(1, algorithm.HashDataByteCount);
            }
        }

        [Theory]
        [MemberData(nameof(HashTheoryData))]
        public void HashData_Simple(byte[] data, byte[] expectedHash, HashAlgorithmName hashAlgorithmName)
        {
            using (TAlgorithm algorithm = new TAlgorithm())
            {
                byte[] hash = algorithm.HashDataImpl(data, 0, data.Length, hashAlgorithmName);
                Assert.Equal(expectedHash, hash);
            }
        }

        [Theory]
        [MemberData(nameof(HashTheoryData))]
        public void HashData_Offsets(byte[] data, byte[] expectedHash, HashAlgorithmName hashAlgorithmName)
        {
            using (TAlgorithm algorithm = new TAlgorithm())
            {
                byte[] padded = new byte[data.Length + 2];
                data.CopyTo(padded, 1);
                byte[] hash = algorithm.HashDataImpl(padded, 1, data.Length, hashAlgorithmName);
                Assert.Equal(expectedHash, hash);
            }
        }

        [Theory]
        [MemberData(nameof(HashTheoryData))]
        public void TryHashData_Success(byte[] data, byte[] expectedHash, HashAlgorithmName hashAlgorithmName)
        {
            using (TAlgorithm algorithm = new TAlgorithm())
            {
                byte[] destination = new byte[expectedHash.Length];
                bool success = algorithm.TryHashDataImpl(data, destination, hashAlgorithmName, out int written);
                Assert.True(success, nameof(algorithm.TryHashDataImpl));
                Assert.Equal(expectedHash, destination);
                Assert.Equal(expectedHash.Length, written);
            }
        }

        public static IEnumerable<object[]> HashTheoryData
        {
            get
            {
                yield return new object[]
                {
                    new byte[] { 0x01, 0x02, 0x03, 0x04, 0xFC, 0xFD, 0xFE, 0xFF },
                    new byte[]
                    {
                        0x59, 0x85, 0xC6, 0xC0, 0x33, 0x93, 0xC1, 0xF2,
                        0x62, 0x7B, 0xBF, 0x23, 0xC4, 0x5E, 0x42, 0x8B,
                    },
                    HashAlgorithmName.MD5,
                };
                yield return new object[]
                {
                    new byte[] { 0x01, 0x02, 0x03, 0x04, 0xFC, 0xFD, 0xFE, 0xFF },
                    new byte[]
                    {
                        0xB6, 0xF2, 0x12, 0x87, 0x45, 0x32, 0xB4, 0xCA,
                        0xB2, 0xC1, 0x03, 0x3C, 0x6D, 0xE4, 0x01, 0x61,
                        0xEB, 0x7F, 0x6F, 0x11,
                    },
                    HashAlgorithmName.SHA1,
                };
                yield return new object[]
                {
                    new byte[] { 0x01, 0x02, 0x03, 0x04, 0xFC, 0xFD, 0xFE, 0xFF },
                    new byte[]
                    {
                        0x54, 0x21, 0xB4, 0x8E, 0xCD, 0xC4, 0x64, 0x29,
                        0xFB, 0x52, 0xBC, 0x35, 0x2C, 0xA7, 0xCF, 0xE6,
                        0xE5, 0x48, 0xD9, 0x67, 0x93, 0x1D, 0x84, 0xF8,
                        0xFC, 0x78, 0xCD, 0xAF, 0x30, 0x24, 0x9D, 0x2F,
                    },
                    HashAlgorithmName.SHA256,
                };
                yield return new object[]
                {
                    new byte[] { 0x01, 0x02, 0x03, 0x04, 0xFC, 0xFD, 0xFE, 0xFF },
                    new byte[]
                    {
                        0xB6, 0x30, 0x80, 0x3F, 0x9A, 0xFD, 0x60, 0x2F,
                        0xB5, 0x81, 0x65, 0x95, 0xC5, 0x14, 0xBC, 0x04,
                        0xFA, 0x8F, 0x30, 0xC1, 0x48, 0xD3, 0xEF, 0xE3,
                        0x02, 0xDA, 0x6D, 0x65, 0x41, 0x13, 0xBE, 0x71,
                        0xB2, 0xE4, 0x1A, 0xA0, 0x2B, 0xD5, 0x59, 0x02,
                        0x35, 0x64, 0x88, 0x0B, 0xC6, 0xCD, 0xDB, 0xCC,
                    },
                    HashAlgorithmName.SHA384,
                };
                yield return new object[]
                {
                    new byte[] { 0x01, 0x02, 0x03, 0x04, 0xFC, 0xFD, 0xFE, 0xFF },
                    new byte[]
                    {
                        0xD2, 0x28, 0x6E, 0x35, 0xF8, 0x57, 0xEF, 0x9E,
                        0x7E, 0xD2, 0x5D, 0xA7, 0xAB, 0xD1, 0x58, 0x1C,
                        0x33, 0xF2, 0x76, 0x0F, 0x49, 0xED, 0x60, 0x1A,
                        0x66, 0x93, 0x36, 0x67, 0x33, 0x16, 0xC0, 0x87,
                        0x35, 0xBC, 0xC7, 0xA6, 0xB3, 0xBC, 0x51, 0x7A,
                        0x42, 0xCE, 0xF4, 0x81, 0x8C, 0x51, 0x36, 0xA7,
                        0xE2, 0x51, 0x86, 0x02, 0xBA, 0x5B, 0xC5, 0xEB,
                        0x31, 0x86, 0xBD, 0x45, 0xBA, 0x05, 0x59, 0x06,
                    },
                    HashAlgorithmName.SHA512,
                };
            }
        }

    }

    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public class ECDsaAlgorithmHashTests : SignatureAlgorithmHashTests<ECDsaAlgorithmHashTests.ECDsaUserDerivedImplementation>
    {
        public sealed class ECDsaUserDerivedImplementation : ECDsa, IHashSigningAlgorithm
        {
            public int HashDataStreamCount { get; set; }
            public int HashDataByteCount { get; set; }
            public int TryHashDataCount { get; set; }

            public override byte[] SignHash(byte[] hash) => throw new NotImplementedException();
            public override bool VerifyHash(byte[] hash, byte[] signature) => throw new NotImplementedException();

            public byte[] HashDataImpl(Stream data, HashAlgorithmName hashAlgorithm) => HashData(data, hashAlgorithm);

            public byte[] HashDataImpl(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm) =>
                HashData(data, offset, count, hashAlgorithm);

            public bool TryHashDataImpl(ReadOnlySpan<byte> data, Span<byte> destination, HashAlgorithmName hashAlgorithm, out int bytesWritten) =>
                TryHashData(data, destination, hashAlgorithm, out bytesWritten);

            protected override byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm)
            {
                HashDataStreamCount++;
                return base.HashData(data, hashAlgorithm);
            }

            protected override byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
            {
                HashDataByteCount++;
                return base.HashData(data, offset, count, hashAlgorithm);
            }

            protected override bool TryHashData(ReadOnlySpan<byte> data, Span<byte> destination, HashAlgorithmName hashAlgorithm, out int bytesWritten)
            {
                TryHashDataCount++;
                return base.TryHashData(data, destination, hashAlgorithm, out bytesWritten);
            }
        }
    }

    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public class DSAAlgorithmHashTests : SignatureAlgorithmHashTests<DSAAlgorithmHashTests.DSAUserDerivedImplementation>
    {
        public sealed class DSAUserDerivedImplementation : DSA, IHashSigningAlgorithm
        {
            public int HashDataStreamCount { get; set; }
            public int HashDataByteCount { get; set; }
            public int TryHashDataCount { get; set; }

            public override DSAParameters ExportParameters(bool includePrivate) => throw new NotImplementedException();
            public override void ImportParameters(DSAParameters parameters) => throw new NotImplementedException();
            public override byte[] CreateSignature(byte[] data) => throw new NotImplementedException();
            public override bool VerifySignature(byte[] data, byte[] signature) => throw new NotImplementedException();

            public byte[] HashDataImpl(Stream data, HashAlgorithmName hashAlgorithm) => HashData(data, hashAlgorithm);

            public byte[] HashDataImpl(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm) =>
                HashData(data, offset, count, hashAlgorithm);

            public bool TryHashDataImpl(ReadOnlySpan<byte> data, Span<byte> destination, HashAlgorithmName hashAlgorithm, out int bytesWritten) =>
                TryHashData(data, destination, hashAlgorithm, out bytesWritten);

            protected override byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm)
            {
                HashDataStreamCount++;
                return base.HashData(data, hashAlgorithm);
            }

            protected override byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
            {
                HashDataByteCount++;
                return base.HashData(data, offset, count, hashAlgorithm);
            }

            protected override bool TryHashData(ReadOnlySpan<byte> data, Span<byte> destination, HashAlgorithmName hashAlgorithm, out int bytesWritten)
            {
                TryHashDataCount++;
                return base.TryHashData(data, destination, hashAlgorithm, out bytesWritten);
            }
        }
    }

    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public class RSAAlgorithmHashTests : SignatureAlgorithmHashTests<RSAAlgorithmHashTests.RSAUserDerivedImplementation>
    {
        public sealed class RSAUserDerivedImplementation : RSA, IHashSigningAlgorithm
        {
            public int HashDataStreamCount { get; set; }
            public int HashDataByteCount { get; set; }
            public int TryHashDataCount { get; set; }

            public override RSAParameters ExportParameters(bool includePrivate) => throw new NotImplementedException();
            public override void ImportParameters(RSAParameters parameters) => throw new NotImplementedException();

            public byte[] HashDataImpl(Stream data, HashAlgorithmName hashAlgorithm) => HashData(data, hashAlgorithm);

            public byte[] HashDataImpl(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm) =>
                HashData(data, offset, count, hashAlgorithm);

            public bool TryHashDataImpl(ReadOnlySpan<byte> data, Span<byte> destination, HashAlgorithmName hashAlgorithm, out int bytesWritten) =>
                TryHashData(data, destination, hashAlgorithm, out bytesWritten);

            protected override byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm)
            {
                HashDataStreamCount++;
                return base.HashData(data, hashAlgorithm);
            }

            protected override byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
            {
                HashDataByteCount++;
                return base.HashData(data, offset, count, hashAlgorithm);
            }

            protected override bool TryHashData(ReadOnlySpan<byte> data, Span<byte> destination, HashAlgorithmName hashAlgorithm, out int bytesWritten)
            {
                TryHashDataCount++;
                return base.TryHashData(data, destination, hashAlgorithm, out bytesWritten);
            }
        }
    }

    public interface IHashSigningAlgorithm : IDisposable
    {
        int HashDataStreamCount { get; set; }
        int HashDataByteCount { get; set; }
        int TryHashDataCount { get; set; }
        byte[] HashDataImpl(Stream data, HashAlgorithmName hashAlgorithm);
        byte[] HashDataImpl(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm);
        bool TryHashDataImpl(ReadOnlySpan<byte> data, Span<byte> destination, HashAlgorithmName hashAlgorithm, out int bytesWritten);
    }
}
