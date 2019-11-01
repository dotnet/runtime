// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Pkcs.Tests.Pkcs12
{
    public static class Pkcs12BuilderTests
    {
        private static readonly Oid s_zeroOid = new Oid("0.0", "0.0");
        private static readonly ReadOnlyMemory<byte> s_derNull = new byte[] { 0x05, 0x00 };

        private static readonly PbeParameters s_pbkdf2Parameters = new PbeParameters(
            PbeEncryptionAlgorithm.Aes256Cbc,
            HashAlgorithmName.SHA384,
            0x1001);

        private static readonly PbeParameters s_win7Pbe = new PbeParameters(
            PbeEncryptionAlgorithm.TripleDes3KeyPkcs12,
            HashAlgorithmName.SHA1,
            2048);

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public static void EncryptDecryptMixBytesAndChars(bool encryptBytes, bool withSpan)
        {
            Pkcs12SafeContents contents = new Pkcs12SafeContents();
            contents.AddSecret(s_zeroOid, s_derNull);

            string password = nameof(EncryptDecryptMixBytesAndChars);
            Span<byte> passwordUtf8Bytes = stackalloc byte[password.Length];
            Encoding.UTF8.GetBytes(password, passwordUtf8Bytes);

            Pkcs12Builder builder = new Pkcs12Builder();

            if (encryptBytes)
            {
                builder.AddSafeContentsEncrypted(contents, passwordUtf8Bytes, s_pbkdf2Parameters);
            }
            else
            {
                builder.AddSafeContentsEncrypted(contents, password, s_pbkdf2Parameters);
            }

            builder.SealWithMac(password, HashAlgorithmName.SHA1, 2048);

            byte[] encoded = builder.Encode();
            Pkcs12Info info = Pkcs12Info.Decode(encoded, out _, skipCopy: true);

            Assert.True(info.VerifyMac(password));
            ReadOnlyCollection<Pkcs12SafeContents> authSafe = info.AuthenticatedSafe;
            Assert.Equal(1, authSafe.Count);

            Pkcs12SafeContents readContents = authSafe[0];

            Assert.Equal(
                Pkcs12ConfidentialityMode.Password,
                readContents.ConfidentialityMode);

            if (encryptBytes)
            {
                if (withSpan)
                {
                    readContents.Decrypt(password.AsSpan());
                }
                else
                {
                    readContents.Decrypt(password);
                }
            }
            else
            {
                if (withSpan)
                {
                    readContents.Decrypt(passwordUtf8Bytes);
                }
                else
                {
                    readContents.Decrypt(passwordUtf8Bytes.ToArray());
                }
            }

            Assert.Equal(
                Pkcs12ConfidentialityMode.None,
                readContents.ConfidentialityMode);

            List<Pkcs12SafeBag> bags = readContents.GetBags().ToList();
            Assert.Equal(1, bags.Count);
            Pkcs12SecretBag secretBag = Assert.IsType<Pkcs12SecretBag>(bags[0]);

            Assert.Equal(s_zeroOid.Value, secretBag.GetSecretType().Value);
            Assert.Equal(s_derNull.ByteArrayToHex(), secretBag.SecretValue.ByteArrayToHex());
        }

        [Fact]
        public static void EncryptPkcs12KdfWithBytes()
        {
            Pkcs12SafeContents contents = new Pkcs12SafeContents();
            contents.AddSecret(s_zeroOid, s_derNull);

            Pkcs12Builder builder = new Pkcs12Builder();

            Assert.ThrowsAny<CryptographicException>(
                () => builder.AddSafeContentsEncrypted(contents, s_derNull.Span, s_win7Pbe));
        }

        [Fact]
        public static void EncodeUnsealed()
        {
            Pkcs12Builder builder = new Pkcs12Builder();
            Assert.False(builder.IsSealed);
            Assert.Throws<InvalidOperationException>(() => builder.Encode());
            Assert.Throws<InvalidOperationException>(() => builder.TryEncode(Span<byte>.Empty, out _));
        }

        [Theory]
        [InlineData(Pkcs12IntegrityMode.Password)]
        [InlineData(Pkcs12IntegrityMode.None)]
        public static void EncodeAndTryEncode(Pkcs12IntegrityMode mode)
        {
            Pkcs12Builder builder1 = new Pkcs12Builder();
            Pkcs12Builder builder2 = new Pkcs12Builder();

            Pkcs12SafeContents contents = new Pkcs12SafeContents();
            contents.AddSecret(s_zeroOid, s_derNull);

            builder1.AddSafeContentsUnencrypted(contents);
            builder2.AddSafeContentsUnencrypted(contents);

            int macTrailerLength = 0;

            if (mode == Pkcs12IntegrityMode.Password)
            {
                builder1.SealWithMac(ReadOnlySpan<char>.Empty, HashAlgorithmName.SHA1, 2);
                builder2.SealWithMac(ReadOnlySpan<char>.Empty, HashAlgorithmName.SHA1, 2);

                // Two OCTET STRINGs of 20 bytes, and the INTEGER 2
                macTrailerLength = 2 + 20 + 2 + 20 + 2 + 3;
            }
            else if (mode == Pkcs12IntegrityMode.None)
            {
                builder1.SealWithoutIntegrity();
                builder2.SealWithoutIntegrity();
            }

            Assert.True(builder1.IsSealed, "builder1.IsSealed");
            Assert.True(builder2.IsSealed, "builder2.IsSealed");

            byte[] encoded = builder1.Encode();
            byte[] buf = new byte[encoded.Length + 40];
            Span<byte> bufSpan = buf;

            // Span too small
            Assert.False(builder2.TryEncode(buf.AsSpan(0, encoded.Length - 1), out int bytesWritten));
            Assert.Equal(0, bytesWritten);

            // Span exactly right
            bufSpan.Fill(0xCA);
            Assert.True(builder2.TryEncode(buf.AsSpan(1, encoded.Length), out bytesWritten));
            Assert.Equal(encoded.Length, bytesWritten);
            Assert.Equal(0xCA, buf[0]);
            Assert.Equal(0xCA, buf[bytesWritten + 1]);

            if (mode == Pkcs12IntegrityMode.Password)
            {
                Assert.Equal(0x02, buf[bytesWritten]);
            }

            // The same contents except the MAC (different random salt)
            Assert.Equal(
                encoded.AsSpan(0, bytesWritten - macTrailerLength).ByteArrayToHex(),
                buf.AsSpan(1, bytesWritten - macTrailerLength).ByteArrayToHex());

            if (macTrailerLength > 0)
            {
                Assert.NotEqual(
                    encoded.AsSpan(bytesWritten - macTrailerLength).ByteArrayToHex(),
                    buf.AsSpan(1 + bytesWritten - macTrailerLength, macTrailerLength).ByteArrayToHex());
            }

            // Span larger than needed
            bufSpan.Fill(0xCA);
            Assert.True(builder2.TryEncode(buf.AsSpan(2), out bytesWritten));
            Assert.Equal(encoded.Length, bytesWritten);
            Assert.Equal(0xCA, buf[0]);
            Assert.Equal(0xCA, buf[1]);
            Assert.Equal(0xCA, buf[bytesWritten + 2]);

            if (mode == Pkcs12IntegrityMode.Password)
            {
                Assert.Equal(0x02, buf[bytesWritten + 1]);
            }

            // The same contents except the MAC (different random salt)
            Assert.Equal(
                encoded.AsSpan(0, bytesWritten - macTrailerLength).ByteArrayToHex(),
                buf.AsSpan(2, bytesWritten - macTrailerLength).ByteArrayToHex());

            if (macTrailerLength > 0)
            {
                Assert.NotEqual(
                    encoded.AsSpan(bytesWritten - macTrailerLength).ByteArrayToHex(),
                    buf.AsSpan(2 + bytesWritten - macTrailerLength, macTrailerLength).ByteArrayToHex());
            }
        }

        [Fact]
        public static void SealAfterSeal()
        {
            Pkcs12Builder builder = new Pkcs12Builder();
            builder.SealWithMac(ReadOnlySpan<char>.Empty, HashAlgorithmName.SHA1, 2);

            Assert.Throws<InvalidOperationException>(() => builder.SealWithoutIntegrity());

            Assert.Throws<InvalidOperationException>(
                () => builder.SealWithMac(ReadOnlySpan<char>.Empty, HashAlgorithmName.SHA1, 2));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void CopyEncryptedSafeContents(bool withSpan)
        {
            Pkcs12Builder builder1 = new Pkcs12Builder();
            Pkcs12Builder builder2 = new Pkcs12Builder();

            Pkcs12SafeContents contents = new Pkcs12SafeContents();
            contents.AddSecret(s_zeroOid, s_derNull);

            if (withSpan)
            {
                builder1.AddSafeContentsEncrypted(contents, ReadOnlySpan<byte>.Empty, s_pbkdf2Parameters);
            }
            else
            {
                builder1.AddSafeContentsEncrypted(contents, (byte[])null, s_pbkdf2Parameters);
            }

            builder1.SealWithoutIntegrity();

            byte[] encoded1 = builder1.Encode();
            Pkcs12Info info = Pkcs12Info.Decode(encoded1, out _, skipCopy: true);
            Assert.Equal(Pkcs12IntegrityMode.None, info.IntegrityMode);
            Assert.Equal(1, info.AuthenticatedSafe.Count);

            builder2.AddSafeContentsUnencrypted(info.AuthenticatedSafe[0]);
            builder2.SealWithoutIntegrity();

            byte[] encoded2 = builder2.Encode();
            Assert.Equal(encoded1.ByteArrayToHex(), encoded2.ByteArrayToHex());
        }

        [Fact]
        public static void EncryptEncryptedSafeContents()
        {
            Pkcs12Builder builder1 = new Pkcs12Builder();
            Pkcs12Builder builder2 = new Pkcs12Builder();

            Pkcs12SafeContents contents = new Pkcs12SafeContents();
            contents.AddSecret(s_zeroOid, s_derNull);

            builder1.AddSafeContentsEncrypted(contents, ReadOnlySpan<byte>.Empty, s_pbkdf2Parameters);
            builder1.SealWithoutIntegrity();

            byte[] encoded = builder1.Encode();
            Pkcs12Info info = Pkcs12Info.Decode(encoded, out _, skipCopy: true);
            Assert.Equal(Pkcs12IntegrityMode.None, info.IntegrityMode);
            Assert.Equal(1, info.AuthenticatedSafe.Count);

            AssertExtensions.Throws<ArgumentException>(
                "safeContents",
                () => builder2.AddSafeContentsEncrypted(
                    info.AuthenticatedSafe[0],
                    "nope",
                    s_pbkdf2Parameters));

            AssertExtensions.Throws<ArgumentException>(
                "safeContents",
                () => builder2.AddSafeContentsEncrypted(
                    info.AuthenticatedSafe[0],
                    s_derNull.Span,
                    s_pbkdf2Parameters));
        }

        [Fact]
        public static void AddContentsAfterSealing()
        {
            Pkcs12SafeContents contents = new Pkcs12SafeContents();
            contents.AddSecret(s_zeroOid, s_derNull);

            Pkcs12Builder builder = new Pkcs12Builder();
            builder.SealWithoutIntegrity();

            Assert.Throws<InvalidOperationException>(
                () => builder.AddSafeContentsUnencrypted(contents));

            Assert.Throws<InvalidOperationException>(
                () => builder.AddSafeContentsEncrypted(contents, Array.Empty<byte>(), s_pbkdf2Parameters));

            Assert.Throws<InvalidOperationException>(
                () => builder.AddSafeContentsEncrypted(contents, ReadOnlySpan<byte>.Empty, s_pbkdf2Parameters));

            Assert.Throws<InvalidOperationException>(
                () => builder.AddSafeContentsEncrypted(contents, string.Empty, s_pbkdf2Parameters));

            Assert.Throws<InvalidOperationException>(
                () => builder.AddSafeContentsEncrypted(contents, ReadOnlySpan<char>.Empty, s_pbkdf2Parameters));
        }

        [Fact]
        public static void AddNullContents()
        {
            Pkcs12Builder builder = new Pkcs12Builder();

            AssertExtensions.Throws<ArgumentNullException>(
                "safeContents",
                () => builder.AddSafeContentsUnencrypted(null));

            AssertExtensions.Throws<ArgumentNullException>(
                "safeContents",
                () => builder.AddSafeContentsEncrypted(null, Array.Empty<byte>(), s_pbkdf2Parameters));

            AssertExtensions.Throws<ArgumentNullException>(
                "safeContents",
                () => builder.AddSafeContentsEncrypted(null, ReadOnlySpan<byte>.Empty, s_pbkdf2Parameters));

            AssertExtensions.Throws<ArgumentNullException>(
                "safeContents",
                () => builder.AddSafeContentsEncrypted(null, string.Empty, s_pbkdf2Parameters));

            AssertExtensions.Throws<ArgumentNullException>(
                "safeContents",
                () => builder.AddSafeContentsEncrypted(null, ReadOnlySpan<char>.Empty, s_pbkdf2Parameters));
        }

        [Fact]
        public static void AddWithNullPbeParameters()
        {
            Pkcs12Builder builder = new Pkcs12Builder();
            Pkcs12SafeContents contents = new Pkcs12SafeContents();

            AssertExtensions.Throws<ArgumentNullException>(
                "pbeParameters",
                () => builder.AddSafeContentsEncrypted(contents, Array.Empty<byte>(), null));

            AssertExtensions.Throws<ArgumentNullException>(
                "pbeParameters",
                () => builder.AddSafeContentsEncrypted(contents, ReadOnlySpan<byte>.Empty, null));

            AssertExtensions.Throws<ArgumentNullException>(
                "pbeParameters",
                () => builder.AddSafeContentsEncrypted(contents, string.Empty, null));

            AssertExtensions.Throws<ArgumentNullException>(
                "pbeParameters",
                () => builder.AddSafeContentsEncrypted(contents, ReadOnlySpan<char>.Empty, null));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void BuildEmptyContents(bool withMac)
        {
            string password;
            Pkcs12Builder builder = new Pkcs12Builder();
            Pkcs12SafeContents contents = new Pkcs12SafeContents();
            builder.AddSafeContentsUnencrypted(contents);

            if (withMac)
            {
                password = "mac";
                builder.SealWithMac(password, HashAlgorithmName.SHA1, 2);
            }
            else
            {
                password = null;
                builder.SealWithoutIntegrity();
            }

            byte[] encoded = builder.Encode();

            const string EmptyHex =
                "3029020103302406092A864886F70D010701A01704153013301106092A864886" +
                "F70D010701A00404023000";

            if (withMac)
            {
                Assert.Equal(60 + EmptyHex.Length / 2, encoded.Length);
            }
            else
            {
                Assert.Equal(EmptyHex, encoded.ByteArrayToHex());
            }

            // [ActiveIssue(11046, TestPlatforms.AnyUnix)]
            if (withMac || RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                X509Certificate2Collection collection = new X509Certificate2Collection();
                collection.Import(encoded, password, X509KeyStorageFlags.DefaultKeySet);
                Assert.Equal(0, collection.Count);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-30)]
        public static void MacRequiresPositiveIterationCount(int iterationCount)
        {
            Pkcs12Builder builder = new Pkcs12Builder();

            AssertExtensions.Throws<ArgumentOutOfRangeException>(
                "iterationCount",
                () => builder.SealWithMac("hi", HashAlgorithmName.SHA1, iterationCount));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void BuildWithoutContents(bool withMac)
        {
            string password;
            Pkcs12Builder builder = new Pkcs12Builder();

            if (withMac)
            {
                password = "mac";
                builder.SealWithMac(password, HashAlgorithmName.SHA1, 2);
            }
            else
            {
                password = null;
                builder.SealWithoutIntegrity();
            }

            byte[] encoded = builder.Encode();

            const string FullyEmptyHex =
                "3016020103301106092A864886F70D010701A00404023000";

            if (withMac)
            {
                Assert.Equal(60 + FullyEmptyHex.Length / 2, encoded.Length);
            }
            else
            {
                Assert.Equal(FullyEmptyHex, encoded.ByteArrayToHex());
            }

            // [ActiveIssue(11046, TestPlatforms.AnyUnix)]
            if (withMac || RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                X509Certificate2Collection collection = new X509Certificate2Collection();
                collection.Import(encoded, password, X509KeyStorageFlags.DefaultKeySet);
                Assert.Equal(0, collection.Count);
            }
        }

        [Fact]
        public static void MacOmitsImplicit1()
        {
            Pkcs12Builder builder1 = new Pkcs12Builder();
            Pkcs12Builder builder2 = new Pkcs12Builder();

            builder1.SealWithMac("hi", HashAlgorithmName.SHA1, 1);
            builder2.SealWithMac("hi", HashAlgorithmName.SHA1, 2);

            byte[] encode1 = builder1.Encode();
            byte[] encode2 = builder2.Encode();

            Assert.Equal(3 + encode1.Length, encode2.Length);
        }
    }
}
