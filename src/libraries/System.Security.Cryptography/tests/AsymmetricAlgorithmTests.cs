// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public static class AsymmetricAlgorithmTests
    {
        [Fact]
        public static void ImportFromPem_AcceptsSubjectPublicKeyInfo()
        {
            string pemText = @"
                Test that this PEM block is skipped since it is not an understood PEM label.
                -----BEGIN SLEEPING-----
                zzzz
                -----END SLEEPING-----
                -----BEGIN PUBLIC KEY-----
                c2xlZXA=
                -----END PUBLIC KEY-----";

            static void ImportSubjectPublicKeyInfo(ReadOnlySpan<byte> source, out int bytesRead)
            {
                ReadOnlySpan<byte> expected = new byte[] { 0x73, 0x6c, 0x65, 0x65, 0x70 };
                AssertExtensions.SequenceEqual(expected, source);
                bytesRead = expected.Length;
            }

            using (StubAsymmetricAlgorithm alg = new StubAsymmetricAlgorithm())
            {
                alg.ImportSubjectPublicKeyInfoImpl = ImportSubjectPublicKeyInfo;
                alg.ImportFromPem(pemText);
            }
        }

        [Fact]
        public static void ImportFromPem_AcceptsPkcs8PrivateKey()
        {
            string pemText = @"
                Test that this PEM block is skipped since it is not an understood PEM label.
                -----BEGIN SLEEPING-----
                zzzz
                -----END SLEEPING-----
                -----BEGIN PRIVATE KEY-----
                c2xlZXA=
                -----END PRIVATE KEY-----";

            static void ImportPkcs8PrivateKey(ReadOnlySpan<byte> source, out int bytesRead)
            {
                ReadOnlySpan<byte> expected = new byte[] { 0x73, 0x6c, 0x65, 0x65, 0x70 };
                AssertExtensions.SequenceEqual(expected, source);
                bytesRead = expected.Length;
            }

            using (StubAsymmetricAlgorithm alg = new StubAsymmetricAlgorithm())
            {
                alg.ImportPkcs8PrivateKeyImpl = ImportPkcs8PrivateKey;
                alg.ImportFromPem(pemText);
            }
        }

        [Fact]
        public static void ImportFromPem_AcceptsEncryptedPkcs8PrivateKey_PasswordBytes()
        {
            string pemText = @"
                Test that this PEM block is skipped since it is not an understood PEM label.
                -----BEGIN SLEEPING-----
                zzzz
                -----END SLEEPING-----
                -----BEGIN ENCRYPTED PRIVATE KEY-----
                c2xlZXA=
                -----END ENCRYPTED PRIVATE KEY-----";
            byte[] pemPassword = new byte[] { 1, 2, 3, 4, 5 };

            void ImportEncryptedPkcs8PrivateKey(
                ReadOnlySpan<byte> passwordBytes,
                ReadOnlySpan<byte> source,
                out int bytesRead)
            {
                ReadOnlySpan<byte> expected = new byte[] { 0x73, 0x6c, 0x65, 0x65, 0x70 };
                AssertExtensions.SequenceEqual(expected, source);
                AssertExtensions.SequenceEqual(pemPassword, passwordBytes);
                bytesRead = expected.Length;
            }

            using (StubAsymmetricAlgorithm alg = new StubAsymmetricAlgorithm())
            {
                alg.ImportEncryptedPkcs8PrivateKeyByteFunc = ImportEncryptedPkcs8PrivateKey;
                alg.ImportFromEncryptedPem(pemText, pemPassword);
            }
        }

        [Fact]
        public static void ImportFromPem_AcceptsEncryptedPkcs8PrivateKey_PasswordChars()
        {
            string pemText = @"
                Test that this PEM block is skipped since it is not an understood PEM label.
                -----BEGIN SLEEPING-----
                zzzz
                -----END SLEEPING-----
                -----BEGIN ENCRYPTED PRIVATE KEY-----
                c2xlZXA=
                -----END ENCRYPTED PRIVATE KEY-----";
            string pemPassword = "PLACEHOLDER";

            void ImportEncryptedPkcs8PrivateKey(
                ReadOnlySpan<char> password,
                ReadOnlySpan<byte> source,
                out int bytesRead)
            {
                ReadOnlySpan<byte> expected = new byte[] { 0x73, 0x6c, 0x65, 0x65, 0x70 };
                AssertExtensions.SequenceEqual(expected, source);
                AssertExtensions.SequenceEqual(pemPassword, password);
                bytesRead = expected.Length;
            }

            using (StubAsymmetricAlgorithm alg = new StubAsymmetricAlgorithm())
            {
                alg.ImportEncryptedPkcs8PrivateKeyCharFunc = ImportEncryptedPkcs8PrivateKey;
                alg.ImportFromEncryptedPem(pemText, pemPassword);
            }
        }

        [Fact]
        public static void ImportFromPem_AmbiguousKey()
        {
            string pemText = @"
                -----BEGIN PUBLIC KEY-----
                c2xlZXA=
                -----END PUBLIC KEY-----
                -----BEGIN PUBLIC KEY-----
                Y29mZmVl
                -----END PUBLIC KEY-----";

            using (StubAsymmetricAlgorithm alg = new StubAsymmetricAlgorithm())
            {
                AssertExtensions.Throws<ArgumentException>("input", () => alg.ImportFromPem(pemText));
            }
        }

        [Fact]
        public static void ImportFromPem_Encrypted_AmbiguousKey()
        {
            string pemText = @"
                -----BEGIN ENCRYPTED PRIVATE KEY-----
                c2xlZXA=
                -----END ENCRYPTED PRIVATE KEY-----
                -----BEGIN ENCRYPTED PRIVATE KEY-----
                Y29mZmVl
                -----END ENCRYPTED PRIVATE KEY-----";
            string pemPassword = "PLACEHOLDER";

            using (StubAsymmetricAlgorithm alg = new StubAsymmetricAlgorithm())
            {
                AssertExtensions.Throws<ArgumentException>("input", () => alg.ImportFromEncryptedPem(pemText, pemPassword));
            }
        }

        [Fact]
        public static void ImportFromPem_NoUnderstoodPemLabel()
        {
            string pemText = @"
                -----BEGIN SLEEPING-----
                zzzz
                -----END SLEEPING-----";

            using (StubAsymmetricAlgorithm alg = new StubAsymmetricAlgorithm())
            {
                AssertExtensions.Throws<ArgumentException>("input", () => alg.ImportFromPem(pemText));
            }
        }

        [Fact]
        public static void ImportFromPem_Encrypted_NoUnderstoodPemLabel()
        {
            string pemText = @"
                -----BEGIN SLEEPING-----
                zzzz
                -----END SLEEPING-----";
            string pemPassword = "PLACEHOLDER";

            using (StubAsymmetricAlgorithm alg = new StubAsymmetricAlgorithm())
            {
                AssertExtensions.Throws<ArgumentException>("input", () => alg.ImportFromEncryptedPem(pemText, pemPassword));
            }
        }

        [Fact]
        public static void ImportFromPem_EncryptedPemWithoutPassword()
        {
            string pemText = @"
                -----BEGIN ENCRYPTED PRIVATE KEY-----
                c2xlZXA=
                -----END ENCRYPTED PRIVATE KEY-----";

            using (StubAsymmetricAlgorithm alg = new StubAsymmetricAlgorithm())
            {
                AssertExtensions.Throws<ArgumentException>("input", () => alg.ImportFromPem(pemText));
            }
        }

        [Fact]
        public static void ImportFromPem_NotEncryptedWithPassword()
        {
            string pemText = @"
                -----BEGIN PRIVATE KEY-----
                c2xlZXA=
                -----END PRIVATE KEY-----";
            string pemPassword = "PLACEHOLDER";

            using (StubAsymmetricAlgorithm alg = new StubAsymmetricAlgorithm())
            {
                AssertExtensions.Throws<ArgumentException>("input", () => alg.ImportFromEncryptedPem(pemText, pemPassword));
            }
        }

        [Fact]
        public static void ExportPem_ExportSubjectPublicKeyInfoPem()
        {
            string expectedPem =
                "-----BEGIN PUBLIC KEY-----\n" +
                "cGVubnk=\n" +
                "-----END PUBLIC KEY-----";
            
            using (StubAsymmetricAlgorithm alg = new StubAsymmetricAlgorithm())
            {
                alg.ExportSubjectPublicKeyInfoImpl = static () => new byte[] { 0x70, 0x65, 0x6e, 0x6e, 0x79 };
                string pem = alg.ExportSubjectPublicKeyInfoPem();
                Assert.Equal(expectedPem, pem);
            }
        }

        [Fact]
        public static void ExportPem_TryExportSubjectPublicKeyInfoPem()
        {
            string expectedPem =
                "-----BEGIN PUBLIC KEY-----\n" +
                "cGVubnk=\n" +
                "-----END PUBLIC KEY-----";

            static bool TryExportSubjectPublicKeyInfo(Span<byte> destination, out int bytesWritten)
            {
                ReadOnlySpan<byte> result = new byte[] { 0x70, 0x65, 0x6e, 0x6e, 0x79 };
                bytesWritten = result.Length;
                result.CopyTo(destination);
                return true;
            }

            using (StubAsymmetricAlgorithm alg = new StubAsymmetricAlgorithm())
            {
                alg.TryExportSubjectPublicKeyInfoImpl = TryExportSubjectPublicKeyInfo;
                int written;
                bool result;
                char[] buffer;

                // buffer not enough
                buffer = new char[expectedPem.Length - 1];
                result = alg.TryExportSubjectPublicKeyInfoPem(buffer, out written);
                Assert.False(result, nameof(alg.TryExportSubjectPublicKeyInfoPem));
                Assert.Equal(0, written);

                // buffer just enough
                buffer = new char[expectedPem.Length];
                result = alg.TryExportSubjectPublicKeyInfoPem(buffer, out written);
                Assert.True(result, nameof(alg.TryExportSubjectPublicKeyInfoPem));
                Assert.Equal(expectedPem.Length, written);
                Assert.Equal(expectedPem, new string(buffer));

                // buffer more than enough
                buffer = new char[expectedPem.Length + 20];
                buffer.AsSpan().Fill('!');
                Span<char> bufferSpan = buffer.AsSpan(10);
                result = alg.TryExportSubjectPublicKeyInfoPem(bufferSpan, out written);
                Assert.True(result, nameof(alg.TryExportSubjectPublicKeyInfoPem));
                Assert.Equal(expectedPem.Length, written);
                Assert.Equal(expectedPem, new string(bufferSpan.Slice(0, written)));

                // Ensure padding has not been touched.
                AssertExtensions.FilledWith('!', buffer[0..10]);
                AssertExtensions.FilledWith('!', buffer[^10..]);
            }
        }

        [Fact]
        public static void ExportPem_TryExportPkcs8PrivateKeyPem()
        {
            string expectedPem =
                "-----BEGIN PRIVATE KEY-----\n" +
                "cGVubnk=\n" +
                "-----END PRIVATE KEY-----";

            static bool TryExportPkcs8PrivateKey(Span<byte> destination, out int bytesWritten)
            {
                ReadOnlySpan<byte> result = new byte[] { 0x70, 0x65, 0x6e, 0x6e, 0x79 };
                bytesWritten = result.Length;
                result.CopyTo(destination);
                return true;
            }

            using (StubAsymmetricAlgorithm alg = new StubAsymmetricAlgorithm())
            {
                alg.TryExportPkcs8PrivateKeyImpl = TryExportPkcs8PrivateKey;
                int written;
                bool result;
                char[] buffer;

                // buffer not enough
                buffer = new char[expectedPem.Length - 1];
                result = alg.TryExportPkcs8PrivateKeyPem(buffer, out written);
                Assert.False(result, nameof(alg.TryExportPkcs8PrivateKeyPem));
                Assert.Equal(0, written);

                // buffer just enough
                buffer = new char[expectedPem.Length];
                result = alg.TryExportPkcs8PrivateKeyPem(buffer, out written);
                Assert.True(result, nameof(alg.TryExportPkcs8PrivateKeyPem));
                Assert.Equal(expectedPem.Length, written);
                Assert.Equal(expectedPem, new string(buffer));

                // buffer more than enough
                buffer = new char[expectedPem.Length + 20];
                buffer.AsSpan().Fill('!');
                Span<char> bufferSpan = buffer.AsSpan(10);
                result = alg.TryExportPkcs8PrivateKeyPem(bufferSpan, out written);
                Assert.True(result, nameof(alg.TryExportPkcs8PrivateKeyPem));
                Assert.Equal(expectedPem.Length, written);
                Assert.Equal(expectedPem, new string(bufferSpan.Slice(0, written)));

                // Ensure padding has not been touched.
                AssertExtensions.FilledWith('!', buffer[0..10]);
                AssertExtensions.FilledWith('!', buffer[^10..]);
            }
        }

        [Fact]
        public static void ExportPem_ExportPkcs8PrivateKeyPem()
        {
            string expectedPem =
                "-----BEGIN PRIVATE KEY-----\n" +
                "cGVubnk=\n" +
                "-----END PRIVATE KEY-----";

            byte[] exportedBytes = new byte[] { 0x70, 0x65, 0x6e, 0x6e, 0x79 };
            
            using (StubAsymmetricAlgorithm alg = new StubAsymmetricAlgorithm())
            {
                alg.ExportPkcs8PrivateKeyPemImpl = () => exportedBytes;
                string pem = alg.ExportPkcs8PrivateKeyPem();
                Assert.Equal(expectedPem, pem);

                // Test that the PEM export cleared the PKCS8 bytes from memory
                // that were returned from ExportPkcs8PrivateKey.
                AssertExtensions.FilledWith((byte)0, exportedBytes);
            }
        }

        [Fact]
        public static void ExportPem_ExportEncryptedPkcs8PrivateKeyPem()
        {
            string expectedPem =
                "-----BEGIN ENCRYPTED PRIVATE KEY-----\n" +
                "cGVubnk=\n" +
                "-----END ENCRYPTED PRIVATE KEY-----";

            byte[] exportedBytes = new byte[] { 0x70, 0x65, 0x6e, 0x6e, 0x79 };
            string expectedPassword = "PLACEHOLDER";
            PbeParameters expectedPbeParameters = new PbeParameters(
                PbeEncryptionAlgorithm.Aes256Cbc,
                HashAlgorithmName.SHA384,
                RandomNumberGenerator.GetInt32(0, 100_000));

            byte[] ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, PbeParameters pbeParameters)
            {
                Assert.Equal(expectedPbeParameters.EncryptionAlgorithm, pbeParameters.EncryptionAlgorithm);
                Assert.Equal(expectedPbeParameters.HashAlgorithm, pbeParameters.HashAlgorithm);
                Assert.Equal(expectedPbeParameters.IterationCount, pbeParameters.IterationCount);
                Assert.Equal(expectedPassword, new string(password));

                return exportedBytes;
            }
            
            using (StubAsymmetricAlgorithm alg = new StubAsymmetricAlgorithm())
            {
                alg.ExportEncryptedPkcs8PrivateKeyImpl = ExportEncryptedPkcs8PrivateKey;
                string pem = alg.ExportEncryptedPkcs8PrivateKeyPem(expectedPassword, expectedPbeParameters);
                Assert.Equal(expectedPem, pem);

                // Test that the PEM export cleared the PKCS8 bytes from memory
                // that were returned from ExportEncryptedPkcs8PrivateKey.
                AssertExtensions.FilledWith((byte)0, exportedBytes);
            }
        }

        [Fact]
        public static void ExportPem_TryExportEncryptedPkcs8PrivateKeyPem()
        {
            string expectedPem =
                "-----BEGIN ENCRYPTED PRIVATE KEY-----\n" +
                "cGVubnk=\n" +
                "-----END ENCRYPTED PRIVATE KEY-----";

            byte[] exportedBytes = new byte[] { 0x70, 0x65, 0x6e, 0x6e, 0x79 };
            string expectedPassword = "PLACEHOLDER";
            PbeParameters expectedPbeParameters = new PbeParameters(
                PbeEncryptionAlgorithm.Aes256Cbc,
                HashAlgorithmName.SHA384,
                RandomNumberGenerator.GetInt32(0, 100_000));

            bool TryExportEncryptedPkcs8PrivateKey(
                ReadOnlySpan<char> password,
                PbeParameters pbeParameters,
                Span<byte> destination,
                out int bytesWritten)
            {
                Assert.Equal(expectedPbeParameters.EncryptionAlgorithm, pbeParameters.EncryptionAlgorithm);
                Assert.Equal(expectedPbeParameters.HashAlgorithm, pbeParameters.HashAlgorithm);
                Assert.Equal(expectedPbeParameters.IterationCount, pbeParameters.IterationCount);
                Assert.Equal(expectedPassword, new string(password));

                exportedBytes.AsSpan().CopyTo(destination);
                bytesWritten = exportedBytes.Length;
                return true;
            }

            using (StubAsymmetricAlgorithm alg = new StubAsymmetricAlgorithm())
            {
                alg.TryExportEncryptedPkcs8PrivateKeyImpl = TryExportEncryptedPkcs8PrivateKey;
                int written;
                bool result;
                char[] buffer;

                // buffer not enough
                buffer = new char[expectedPem.Length - 1];
                result = alg.TryExportEncryptedPkcs8PrivateKeyPem(expectedPassword, expectedPbeParameters, buffer, out written);
                Assert.False(result, nameof(alg.TryExportEncryptedPkcs8PrivateKeyPem));
                Assert.Equal(0, written);

                // buffer just enough
                buffer = new char[expectedPem.Length];
                result = alg.TryExportEncryptedPkcs8PrivateKeyPem(expectedPassword, expectedPbeParameters, buffer, out written);
                Assert.True(result, nameof(alg.TryExportEncryptedPkcs8PrivateKeyPem));
                Assert.Equal(expectedPem.Length, written);
                Assert.Equal(expectedPem, new string(buffer));

                // buffer more than enough
                buffer = new char[expectedPem.Length + 20];
                buffer.AsSpan().Fill('!');
                Span<char> bufferSpan = buffer.AsSpan(10);
                result = alg.TryExportEncryptedPkcs8PrivateKeyPem(expectedPassword, expectedPbeParameters, bufferSpan, out written);
                Assert.True(result, nameof(alg.TryExportEncryptedPkcs8PrivateKeyPem));
                Assert.Equal(expectedPem.Length, written);
                Assert.Equal(expectedPem, new string(bufferSpan.Slice(0, written)));

                // Ensure padding has not been touched.
                AssertExtensions.FilledWith('!', buffer[0..10]);
                AssertExtensions.FilledWith('!', buffer[^10..]);
            }
        }

        private class StubAsymmetricAlgorithm : AsymmetricAlgorithm
        {
            public delegate byte[] ExportSubjectPublicKeyInfoFunc();
            public delegate byte[] ExportPkcs8PrivateKeyPemFunc();
            public delegate byte[] ExportEncryptedPkcs8PrivateKeyFunc(ReadOnlySpan<char> password, PbeParameters pbeParameters);
            public delegate bool TryExportSubjectPublicKeyInfoFunc(Span<byte> destination, out int bytesWritten);
            public delegate bool TryExportPkcs8PrivateKeyFunc(Span<byte> destination, out int bytesWritten);
            public delegate void ImportSubjectPublicKeyInfoFunc(ReadOnlySpan<byte> source, out int bytesRead);
            public delegate void ImportPkcs8PrivateKeyFunc(ReadOnlySpan<byte> source, out int bytesRead);
            public delegate void ImportEncryptedPkcs8PrivateKeyFunc<TPass>(
                ReadOnlySpan<TPass> password,
                ReadOnlySpan<byte> source,
                out int bytesRead);
            public delegate bool TryExportEncryptedPkcs8PrivateKeyFunc(
                ReadOnlySpan<char> password,
                PbeParameters pbeParameters,
                Span<byte> destination,
                out int bytesWritten);

            public ImportSubjectPublicKeyInfoFunc ImportSubjectPublicKeyInfoImpl { get; set; }
            public ImportPkcs8PrivateKeyFunc ImportPkcs8PrivateKeyImpl { get; set; }
            public ImportEncryptedPkcs8PrivateKeyFunc<byte> ImportEncryptedPkcs8PrivateKeyByteFunc { get; set; }
            public ImportEncryptedPkcs8PrivateKeyFunc<char> ImportEncryptedPkcs8PrivateKeyCharFunc { get; set; }
            public ExportSubjectPublicKeyInfoFunc ExportSubjectPublicKeyInfoImpl {get; set; }
            public TryExportSubjectPublicKeyInfoFunc TryExportSubjectPublicKeyInfoImpl { get; set; }
            public ExportPkcs8PrivateKeyPemFunc ExportPkcs8PrivateKeyPemImpl { get; set; }
            public TryExportPkcs8PrivateKeyFunc TryExportPkcs8PrivateKeyImpl { get; set; }
            public ExportEncryptedPkcs8PrivateKeyFunc ExportEncryptedPkcs8PrivateKeyImpl { get; set; }
            public TryExportEncryptedPkcs8PrivateKeyFunc TryExportEncryptedPkcs8PrivateKeyImpl { get; set; }

            public override void ImportSubjectPublicKeyInfo(ReadOnlySpan<byte> source, out int bytesRead) =>
                ImportSubjectPublicKeyInfoImpl(source, out bytesRead);

            public override void ImportPkcs8PrivateKey(ReadOnlySpan<byte> source, out int bytesRead) =>
                ImportPkcs8PrivateKeyImpl(source, out bytesRead);

            public override byte[] ExportSubjectPublicKeyInfo() => ExportSubjectPublicKeyInfoImpl();
            public override byte[] ExportPkcs8PrivateKey() => ExportPkcs8PrivateKeyPemImpl();

            public override byte[] ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, PbeParameters pbeParameters)
            {
                return ExportEncryptedPkcs8PrivateKeyImpl(password, pbeParameters);
            }

            public override void ImportEncryptedPkcs8PrivateKey(
                ReadOnlySpan<byte> passwordBytes,
                ReadOnlySpan<byte> source,
                out int bytesRead)
            {
                ImportEncryptedPkcs8PrivateKeyByteFunc(passwordBytes, source, out bytesRead);
            }

            public override void ImportEncryptedPkcs8PrivateKey(
                ReadOnlySpan<char> password,
                ReadOnlySpan<byte> source,
                out int bytesRead)
            {
                ImportEncryptedPkcs8PrivateKeyCharFunc(password, source, out bytesRead);
            }

            public override bool TryExportSubjectPublicKeyInfo(Span<byte> destination, out int bytesWritten)
            {
                return TryExportSubjectPublicKeyInfoImpl(destination, out bytesWritten);
            }

            public override bool TryExportPkcs8PrivateKey(Span<byte> destination, out int bytesWritten)
            {
                return TryExportPkcs8PrivateKeyImpl(destination, out bytesWritten);
            }

            public override bool TryExportEncryptedPkcs8PrivateKey(
                ReadOnlySpan<char> password,
                PbeParameters pbeParameters,
                Span<byte> destination,
                out int bytesWritten)
            {
                return TryExportEncryptedPkcs8PrivateKeyImpl(password, pbeParameters, destination, out bytesWritten);
            }
        }
    }
}
