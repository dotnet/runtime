// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;

namespace System.Security.Cryptography.Algorithms.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public static class ECPemExportTests
    {
        [Fact]
        public static void ExportPem_ECDsa_ExportECPrivateKey()
        {
            string expectedPem =
                "-----BEGIN EC PRIVATE KEY-----\n" +
                "cGVubnk=\n" +
                "-----END EC PRIVATE KEY-----";

            static byte[] ExportECPrivateKey()
            {
                return new byte[] { 0x70, 0x65, 0x6e, 0x6e, 0x79 };
            }

            using (DelegateECDsa ec = new DelegateECDsa())
            {
                ec.ExportECPrivateKeyDelegate = ExportECPrivateKey;
                Assert.Equal(expectedPem, ec.ExportECPrivateKeyPem());
            }
        }

        [Fact]
        public static void ExportPem_ECDsa_TryExportECPrivateKey()
        {
            string expectedPem =
                "-----BEGIN EC PRIVATE KEY-----\n" +
                "cGVubnk=\n" +
                "-----END EC PRIVATE KEY-----";

            static bool TryExportECPrivateKey(Span<byte> destination, out int bytesWritten)
            {
                ReadOnlySpan<byte> result = new byte[] { 0x70, 0x65, 0x6e, 0x6e, 0x79 };
                bytesWritten = result.Length;
                result.CopyTo(destination);
                return true;
            }

            using (DelegateECDsa ec = new DelegateECDsa())
            {
                ec.TryExportECPrivateKeyDelegate = TryExportECPrivateKey;

                int written;
                bool result;
                char[] buffer;

                // buffer not enough
                buffer = new char[expectedPem.Length - 1];
                result = ec.TryExportECPrivateKeyPem(buffer, out written);
                Assert.False(result, nameof(ec.TryExportECPrivateKeyPem));
                Assert.Equal(0, written);

                // buffer just enough
                buffer = new char[expectedPem.Length];
                result = ec.TryExportECPrivateKeyPem(buffer, out written);
                Assert.True(result, nameof(ec.TryExportECPrivateKeyPem));
                Assert.Equal(expectedPem.Length, written);
                Assert.Equal(expectedPem, new string(buffer));

                // buffer more than enough
                buffer = new char[expectedPem.Length + 20];
                buffer.AsSpan().Fill('!');
                Span<char> bufferSpan = buffer.AsSpan(10);
                result = ec.TryExportECPrivateKeyPem(bufferSpan, out written);
                Assert.True(result, nameof(ec.TryExportECPrivateKeyPem));
                Assert.Equal(expectedPem.Length, written);
                Assert.Equal(expectedPem, new string(bufferSpan.Slice(0, written)));

                // Ensure padding has not been touched.
                AssertExtensions.FilledWith('!', buffer[0..10]);
                AssertExtensions.FilledWith('!', buffer[^10..]);
            }
        }
        [Fact]
        public static void ExportPem_ECDiffieHellman_ExportECPrivateKey()
        {
            string expectedPem =
                "-----BEGIN EC PRIVATE KEY-----\n" +
                "cGVubnk=\n" +
                "-----END EC PRIVATE KEY-----";

            static byte[] ExportECPrivateKey()
            {
                return new byte[] { 0x70, 0x65, 0x6e, 0x6e, 0x79 };
            }

            using (DelegateECDiffieHellman ec = new DelegateECDiffieHellman())
            {
                ec.ExportECPrivateKeyDelegate = ExportECPrivateKey;
                Assert.Equal(expectedPem, ec.ExportECPrivateKeyPem());
            }
        }

        [Fact]
        public static void ExportPem_ECDiffieHellman_TryExportECPrivateKey()
        {
            string expectedPem =
                "-----BEGIN EC PRIVATE KEY-----\n" +
                "cGVubnk=\n" +
                "-----END EC PRIVATE KEY-----";

            static bool TryExportECPrivateKey(Span<byte> destination, out int bytesWritten)
            {
                ReadOnlySpan<byte> result = new byte[] { 0x70, 0x65, 0x6e, 0x6e, 0x79 };
                bytesWritten = result.Length;
                result.CopyTo(destination);
                return true;
            }

            using (DelegateECDiffieHellman ec = new DelegateECDiffieHellman())
            {
                ec.TryExportECPrivateKeyDelegate = TryExportECPrivateKey;

                int written;
                bool result;
                char[] buffer;

                // buffer not enough
                buffer = new char[expectedPem.Length - 1];
                result = ec.TryExportECPrivateKeyPem(buffer, out written);
                Assert.False(result, nameof(ec.TryExportECPrivateKeyPem));
                Assert.Equal(0, written);

                // buffer just enough
                buffer = new char[expectedPem.Length];
                result = ec.TryExportECPrivateKeyPem(buffer, out written);
                Assert.True(result, nameof(ec.TryExportECPrivateKeyPem));
                Assert.Equal(expectedPem.Length, written);
                Assert.Equal(expectedPem, new string(buffer));

                // buffer more than enough
                buffer = new char[expectedPem.Length + 20];
                buffer.AsSpan().Fill('!');
                Span<char> bufferSpan = buffer.AsSpan(10);
                result = ec.TryExportECPrivateKeyPem(bufferSpan, out written);
                Assert.True(result, nameof(ec.TryExportECPrivateKeyPem));
                Assert.Equal(expectedPem.Length, written);
                Assert.Equal(expectedPem, new string(bufferSpan.Slice(0, written)));

                // Ensure padding has not been touched.
                AssertExtensions.FilledWith('!', buffer[0..10]);
                AssertExtensions.FilledWith('!', buffer[^10..]);
            }
        }

        private class DelegateECDsa : ECDsa
        {
            public delegate bool TryExportFunc(Span<byte> destination, out int bytesWritten);

            public Func<byte[]> ExportECPrivateKeyDelegate = null;
            public TryExportFunc TryExportECPrivateKeyDelegate = null;


            public DelegateECDsa()
            {
            }

            public override byte[] ExportECPrivateKey() => ExportECPrivateKeyDelegate();

            public override bool TryExportECPrivateKey(Span<byte> destination, out int bytesWritten) =>
                TryExportECPrivateKeyDelegate(destination, out bytesWritten);

            public override byte[] SignHash(byte[] hash) => throw new NotImplementedException();
            public override bool VerifyHash(byte[] hash, byte[] signature) => throw new NotImplementedException();
            protected override byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm) => throw new NotImplementedException();
            protected override byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm) => throw new NotImplementedException();
            public override void ImportParameters(ECParameters parameters) => throw new NotImplementedException();
            public override ECParameters ExportParameters(bool includePrivateParameters) => throw new NotImplementedException();
            public override ECParameters ExportExplicitParameters(bool includePrivateParameters) => throw new NotImplementedException();
            public override void GenerateKey(ECCurve curve) => throw new NotImplementedException();
        }

        private class DelegateECDiffieHellman : ECDiffieHellman
        {
            public delegate bool TryExportFunc(Span<byte> destination, out int bytesWritten);

            public Func<byte[]> ExportECPrivateKeyDelegate = null;
            public TryExportFunc TryExportECPrivateKeyDelegate = null;


            public DelegateECDiffieHellman()
            {
            }

            public override byte[] ExportECPrivateKey() => ExportECPrivateKeyDelegate();

            public override bool TryExportECPrivateKey(Span<byte> destination, out int bytesWritten) =>
                TryExportECPrivateKeyDelegate(destination, out bytesWritten);

            public override ECDiffieHellmanPublicKey PublicKey => throw new NotImplementedException();
        }
    }
}