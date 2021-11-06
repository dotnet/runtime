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

            using (ImportAsymmetricAlgorithm alg = new ImportAsymmetricAlgorithm())
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

            using (ImportAsymmetricAlgorithm alg = new ImportAsymmetricAlgorithm())
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

            using (ImportAsymmetricAlgorithm alg = new ImportAsymmetricAlgorithm())
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

            using (ImportAsymmetricAlgorithm alg = new ImportAsymmetricAlgorithm())
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

            using (ImportAsymmetricAlgorithm alg = new ImportAsymmetricAlgorithm())
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

            using (ImportAsymmetricAlgorithm alg = new ImportAsymmetricAlgorithm())
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

            using (ImportAsymmetricAlgorithm alg = new ImportAsymmetricAlgorithm())
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

            using (ImportAsymmetricAlgorithm alg = new ImportAsymmetricAlgorithm())
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

            using (ImportAsymmetricAlgorithm alg = new ImportAsymmetricAlgorithm())
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

            using (ImportAsymmetricAlgorithm alg = new ImportAsymmetricAlgorithm())
            {
                AssertExtensions.Throws<ArgumentException>("input", () => alg.ImportFromEncryptedPem(pemText, pemPassword));
            }
        }

        private class ImportAsymmetricAlgorithm : AsymmetricAlgorithm
        {
            public delegate void ImportSubjectPublicKeyInfoFunc(ReadOnlySpan<byte> source, out int bytesRead);
            public delegate void ImportPkcs8PrivateKeyFunc(ReadOnlySpan<byte> source, out int bytesRead);
            public delegate void ImportEncryptedPkcs8PrivateKeyFunc<TPass>(
                ReadOnlySpan<TPass> password,
                ReadOnlySpan<byte> source,
                out int bytesRead);

            public ImportSubjectPublicKeyInfoFunc ImportSubjectPublicKeyInfoImpl { get; set; }
            public ImportPkcs8PrivateKeyFunc ImportPkcs8PrivateKeyImpl { get; set; }
            public ImportEncryptedPkcs8PrivateKeyFunc<byte> ImportEncryptedPkcs8PrivateKeyByteFunc { get; set; }
            public ImportEncryptedPkcs8PrivateKeyFunc<char> ImportEncryptedPkcs8PrivateKeyCharFunc { get; set; }

            public override void ImportSubjectPublicKeyInfo(ReadOnlySpan<byte> source, out int bytesRead) =>
                ImportSubjectPublicKeyInfoImpl(source, out bytesRead);

            public override void ImportPkcs8PrivateKey(ReadOnlySpan<byte> source, out int bytesRead) =>
                ImportPkcs8PrivateKeyImpl(source, out bytesRead);

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
        }
    }
}
