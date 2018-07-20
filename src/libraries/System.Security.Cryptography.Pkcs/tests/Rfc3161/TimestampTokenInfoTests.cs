// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Pkcs.Tests
{
    public static class TimestampTokenInfoTests
    {
        [Theory]
        [InlineData(nameof(TimestampTokenTestData.FreeTsaDotOrg1))]
        [InlineData(nameof(TimestampTokenTestData.Symantec1))]
        [InlineData(nameof(TimestampTokenTestData.DigiCert1))]
        public static void CreateFromParameters(string testDataName)
        {
            TimestampTokenTestData testData = TimestampTokenTestData.GetTestData(testDataName);

            Oid policyId = new Oid(testData.PolicyId, testData.PolicyId);
            Oid hashAlgorithmOid = new Oid(testData.HashAlgorithmId);
            byte[] messageHash = testData.HashBytes.ToArray();
            byte[] serial = testData.SerialNumberBytes.ToArray();
            DateTimeOffset nonUtcTimestamp = testData.Timestamp.ToOffset(TimeSpan.FromHours(-8));
            long? accuracyMicrosec = testData.AccuracyInMicroseconds;
            byte[] nonce = testData.NonceBytes?.ToArray();
            byte[] tsaNameBytes = testData.TsaNameBytes?.ToArray();

            ReadOnlyMemory<byte>? nonceMemory = null;
            ReadOnlyMemory<byte>? tsaMemory = null;

            if (nonce != null)
            {
                nonceMemory = nonce;
            }

            if (tsaNameBytes != null)
            {
                tsaMemory = tsaNameBytes;
            }

            var tokenInfo = new Rfc3161TimestampTokenInfo(
                policyId,
                hashAlgorithmOid,
                messageHash,
                serial,
                nonUtcTimestamp,
                accuracyMicrosec,
                testData.IsOrdering,
                nonceMemory,
                tsaMemory);

            // Since AssertEqual will check all the fields the remaining checks in this method are about
            // input/output value/reference associations.
            AssertEqual(testData, tokenInfo);

            Assert.NotSame(policyId, tokenInfo.PolicyId);
            Assert.NotSame(hashAlgorithmOid, tokenInfo.HashAlgorithmId);

            Assert.Equal(nonUtcTimestamp, tokenInfo.Timestamp);
            Assert.Equal(TimeSpan.Zero, tokenInfo.Timestamp.Offset);

            Assert.Equal(messageHash.ByteArrayToHex(), tokenInfo.GetMessageHash().ByteArrayToHex());
            // Detached from the original data
            messageHash[0] ^= 0xFF;
            Assert.NotEqual(messageHash.ByteArrayToHex(), tokenInfo.GetMessageHash().ByteArrayToHex());

            Assert.Equal(serial.ByteArrayToHex(), tokenInfo.GetSerialNumber().ByteArrayToHex());
            // Detached from the original data
            serial[1] ^= 0xFF;
            Assert.NotEqual(serial.ByteArrayToHex(), tokenInfo.GetSerialNumber().ByteArrayToHex());


            if (nonce != null)
            {
                ReadOnlyMemory<byte>? tokenNonce = tokenInfo.GetNonce();
                Assert.True(tokenNonce.HasValue, "tokenInfo.GetNonce().HasValue");

                Assert.Equal(nonce.ByteArrayToHex(), tokenNonce.Value.ByteArrayToHex());
                // Detached from the original data
                nonce[0] ^= 0xFF;
                Assert.NotEqual(nonce.ByteArrayToHex(), tokenNonce.Value.ByteArrayToHex());
            }

            ReadOnlyMemory<byte>? nameFromToken = tokenInfo.GetTimestampAuthorityName();

            if (tsaNameBytes != null)
            {
                Assert.True(nameFromToken.HasValue, "nameFromToken.HasValue");
                Assert.Equal(tsaNameBytes.ByteArrayToHex(), nameFromToken.Value.ByteArrayToHex());
                // Detached from the original data
                tsaNameBytes[5] ^= 0xFF;
                Assert.NotEqual(tsaNameBytes.ByteArrayToHex(), nameFromToken.Value.ByteArrayToHex());
            }

            if (testData.ExtensionsBytes == null)
            {
                Assert.False(tokenInfo.HasExtensions, "tokenInfo.HasExtensions");
                Assert.NotNull(tokenInfo.GetExtensions());
                Assert.Equal(0, tokenInfo.GetExtensions().Count);

                // GetExtensions always returns a new collection.
                Assert.NotSame(tokenInfo.GetExtensions(), tokenInfo.GetExtensions());
            }
            else
            {
                Assert.True(tokenInfo.HasExtensions, "tokenInfo.HasExtensions");
                Assert.NotNull(tokenInfo.GetExtensions());

                Assert.True(false, "A test handler has been written for extensions...");

                // GetExtensions always returns a new collection.
                Assert.NotSame(tokenInfo.GetExtensions(), tokenInfo.GetExtensions());
            }

            // Because the token is DER encoded, we should produce byte-for-byte the same value.
            Assert.Equal(testData.TokenInfoBytes.ByteArrayToHex(), tokenInfo.Encode().ByteArrayToHex());
        }

        [Theory]
        [InlineData(nameof(TimestampTokenTestData.FreeTsaDotOrg1), false)]
        [InlineData(nameof(TimestampTokenTestData.FreeTsaDotOrg1), true)]
        [InlineData(nameof(TimestampTokenTestData.Symantec1), false)]
        [InlineData(nameof(TimestampTokenTestData.Symantec1), true)]
        [InlineData(nameof(TimestampTokenTestData.DigiCert1), false)]
        [InlineData(nameof(TimestampTokenTestData.DigiCert1), true)]
        public static void CreateFromValue(string testDataName, bool viaTry)
        {
            TimestampTokenTestData testData = TimestampTokenTestData.GetTestData(testDataName);

            ValidateTokenInfo(
                testData.TokenInfoBytes,
                testData,
                viaTry ? testData.TokenInfoBytes.Length : (int?)null);
        }

        private static void ValidateTokenInfo(
            ReadOnlyMemory<byte> tokenInfoBytes,
            TimestampTokenTestData testData,
            int? lengthFromTry)
        {
            Rfc3161TimestampTokenInfo tokenInfo;

            Assert.True(
                Rfc3161TimestampTokenInfo.TryDecode(tokenInfoBytes, out tokenInfo, out int bytesRead),
                "Rfc3161TimestampTokenInfo.TryDecode");

            Assert.NotNull(tokenInfo);

            if (lengthFromTry != null)
            {
                Assert.Equal(lengthFromTry.Value, bytesRead);
            }
            
            AssertEqual(testData, tokenInfo);
        }

        [Fact]
        public static void TryDecode_LongerThanNeeded()
        {
            const int ExtraBytes = 11;
            ReadOnlyMemory<byte> inputTokenData = TimestampTokenTestData.Symantec1.TokenInfoBytes;
            int len = inputTokenData.Length + ExtraBytes;
            byte[] inputData = new byte[len];

            for (int i = inputTokenData.Length; i < len; i++)
            {
                inputData[i] = unchecked((byte)i);
            }

            inputTokenData.Span.CopyTo(inputData);

            ValidateTokenInfo(inputData, TimestampTokenTestData.Symantec1, inputTokenData.Length);
        }

        [Fact]
        public static void TryDecode_Invalid()
        {
            ReadOnlyMemory<byte> inputData = TimestampTokenTestData.Symantec1.TokenInfoBytes;

            Assert.False(
                Rfc3161TimestampTokenInfo.TryDecode(
                    inputData.Slice(0, inputData.Length - 1),
                    out Rfc3161TimestampTokenInfo tokenInfo,
                    out int bytesRead));

            Assert.Equal(0, bytesRead);
            Assert.Null(tokenInfo);
        }

        [Fact]
        public static void BuilderCtor_PolicyIdRequired()
        {
            AssertExtensions.Throws<ArgumentNullException>(
                "policyId",
                () => new Rfc3161TimestampTokenInfo(null, null, default, default, default));
        }

        [Fact]
        public static void BuilderCtor_HashAlgorithmIdRequired()
        {
            Oid policyId = new Oid("0.0", "0.0");

            AssertExtensions.Throws<ArgumentNullException>(
                "hashAlgorithmId",
                () => new Rfc3161TimestampTokenInfo(policyId, null, default, default, default));
        }

        [Fact]
        public static void BuilderCtor_TsaNameOptional()
        {
            Oid policyId = new Oid("0.0", "0.0");
            Oid hashAlgorithmId = new Oid(Oids.Sha256);

            var tokenInfo = new Rfc3161TimestampTokenInfo(
                policyId,
                hashAlgorithmId,
                new byte[256 / 8],
                new byte[] { 1 },
                DateTimeOffset.UtcNow);

            Assert.False(tokenInfo.GetTimestampAuthorityName().HasValue);

            Assert.True(Rfc3161TimestampTokenInfo.TryDecode(tokenInfo.Encode(), out tokenInfo, out _));
            Assert.False(tokenInfo.GetTimestampAuthorityName().HasValue);
        }

        [Fact]
        public static void BuilderCtor_AccuracyOptional()
        {
            Oid policyId = new Oid("0.0", "0.0");
            Oid hashAlgorithmId = new Oid(Oids.Sha256);

            var tokenInfo = new Rfc3161TimestampTokenInfo(
                policyId,
                hashAlgorithmId,
                new byte[256 / 8],
                new byte[] { 2 },
                DateTimeOffset.UtcNow);

            Assert.False(tokenInfo.AccuracyInMicroseconds.HasValue);

            Assert.True(Rfc3161TimestampTokenInfo.TryDecode(tokenInfo.Encode(), out tokenInfo, out _));
            Assert.False(tokenInfo.AccuracyInMicroseconds.HasValue);
        }

        [Fact]
        public static void TsaName_SameDataSecondInvocation()
        {
            const string InputHex =
                "3081F8020101060B6086480186F845010717033031300D060960864801650304" +
                "020105000420315F5BDB76D078C43B8AC0064E4A0164612B1FCE77C869345BFC" +
                "94C75894EDD302146C77B12D5FCF9F6DC1D4A481E935F446FBA376C4180F3230" +
                "3137313031303232303835325A300302011EA08186A48183308180310B300906" +
                "0355040613025553311D301B060355040A131453796D616E74656320436F7270" +
                "6F726174696F6E311F301D060355040B131653796D616E746563205472757374" +
                "204E6574776F726B3131302F0603550403132853796D616E7465632053484132" +
                "35362054696D655374616D70696E67205369676E6572202D204732";

            Rfc3161TimestampTokenInfo tokenInfo;
            Assert.True(Rfc3161TimestampTokenInfo.TryDecode(InputHex.HexToByteArray(), out tokenInfo, out _));

            ReadOnlyMemory<byte>? tsaName = tokenInfo.GetTimestampAuthorityName();
            Assert.True(tsaName.HasValue, "tsaName.HasValue");
            ReadOnlyMemory<byte> tsaName1 = tsaName.Value;
            ReadOnlyMemory<byte> tsaName2 = tokenInfo.GetTimestampAuthorityName().Value;

            Assert.Equal(tsaName1.Length, tsaName2.Length);

            Assert.True(
                Unsafe.AreSame(
                    ref MemoryMarshal.GetReference(tsaName1.Span),
                    ref MemoryMarshal.GetReference(tsaName2.Span)),
                "Second call to GetTimestampAuthorityName is over the same memory");
        }

        [Fact]
        public static void ExtensionsRoundtrips()
        {
            Oid policyId = new Oid("0.0", "0.0");
            Oid hashAlgorithmId = new Oid(Oids.Sha256);

            byte[] extensionValue = { 3, 1, 4, 1, 5, 9, 2, 7, 5, 8 };
            
            var tokenInfo = new Rfc3161TimestampTokenInfo(
                policyId,
                hashAlgorithmId,
                new byte[256 / 8],
                new byte[] { 3 },
                DateTimeOffset.UtcNow,
                extensions: new X509ExtensionCollection
                {
                    new X509Extension(new Oid("0.0.0", "0.0.0"), extensionValue, true),
                });

            Assert.True(tokenInfo.HasExtensions);

            Assert.True(Rfc3161TimestampTokenInfo.TryDecode(tokenInfo.Encode(), out tokenInfo, out _));

            Assert.True(tokenInfo.HasExtensions);
            X509ExtensionCollection extensions = tokenInfo.GetExtensions();

            Assert.Equal(1, extensions.Count);
            X509Extension extension = extensions[0];
            Assert.NotNull(extension);
            Assert.Equal("0.0.0", extension.Oid.Value);
            Assert.True(extension.Critical, "extension.Critical");
            Assert.Equal(extensionValue.ByteArrayToHex(), extension.RawData.ByteArrayToHex());
        }

        [Fact]
        public static void BuilderCtor_IsOrdering_Roundtrips()
        {
            Oid policyId = new Oid("0.0", "0.0");
            Oid hashAlgorithmId = new Oid(Oids.Sha256);

            var tokenInfo = new Rfc3161TimestampTokenInfo(
                policyId,
                hashAlgorithmId,
                new byte[256 / 8],
                new byte[] { 7 },
                DateTimeOffset.UtcNow,
                isOrdering: true);

            Assert.True(tokenInfo.IsOrdering, "tokenInfo.IsOrdering");

            Assert.True(Rfc3161TimestampTokenInfo.TryDecode(tokenInfo.Encode(), out tokenInfo, out _));
            Assert.True(tokenInfo.IsOrdering, "tokenInfo.IsOrdering");
        }

        [Fact]
        public static void BuilderCtor_Timestamp_KeepsSubSeconds()
        {
            // RFC 3161 says that the genTime value should omit fractions "when there is no need"
            //
            // We leave the trimming up to the caller, because there are multiple positions for
            // the accuracy+precision position.
            DateTimeOffset marker = new DateTimeOffset(2017, 12, 18, 17, 5, 34, TimeSpan.Zero);
            DateTimeOffset experiment = marker + TimeSpan.FromMilliseconds(17);

            Assert.NotEqual(marker, experiment);

            Oid policyId1 = new Oid("0.0", "0.0");
            Oid hashAlgorithmId1 = new Oid(Oids.Sha256);

            var tokenInfo = new Rfc3161TimestampTokenInfo(
                policyId1,
                hashAlgorithmId1,
                new byte[256 / 8],
                new byte[] { 6 },
                experiment);

            Assert.Equal(experiment, tokenInfo.Timestamp);
        }

        [Theory]
        [InlineData("No accuracy", "", true, null)]
        [InlineData("MicroSeconds = 0", "3003810100", false, null)]
        [InlineData("MicroSeconds = 1", "3003810101", true, 1L)]
        [InlineData("MicroSeconds = 999", "3004810203E7", true, 999L)]
        [InlineData("MicroSeconds = 1000", "3004810203E8", false, null)]
        [InlineData("MilliSeconds = 0", "3003800100", false, null)]
        [InlineData("MilliSeconds = 1", "3003800101", true, 1000L)]
        [InlineData("MilliSeconds = 999", "3004800203E7", true, 999000L)]
        [InlineData("MilliSeconds = 1000", "3004800203E8", false, null)]
        [InlineData("Seconds = 0", "3003020100", true, 0L)]
        [InlineData("Seconds = -1", "30030201FF", false, null)]
        public static void Accuracy_Bounds_ParsesAsExpected(
            string description,
            string accuracyHex,
            bool shouldParse,
            long? expectedTotalMicroseconds)
        {
            string inputHex =
                "305A0201010601003031300D0609608648016503040201050004200000000000" +
                "0000000000000000000000000000000000000000000000000000000201081817" +
                "32303137313231383138313235342E373438363336345A" + accuracyHex;

            byte[] inputData = inputHex.HexToByteArray();
            inputData[1] = checked((byte)(0x55 + accuracyHex.Length / 2));

            if (shouldParse)
            {
                int bytesRead;
                Rfc3161TimestampTokenInfo tokenInfo;

                Assert.True(Rfc3161TimestampTokenInfo.TryDecode(inputData, out tokenInfo, out bytesRead));
                Assert.Equal(inputData.Length, bytesRead);
                Assert.NotNull(tokenInfo);
                Assert.Equal(expectedTotalMicroseconds, tokenInfo.AccuracyInMicroseconds);
            }
            else
            {
                Assert.False(Rfc3161TimestampTokenInfo.TryDecode(inputData, out _, out _));
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(999)]
        [InlineData(1000)]
        [InlineData(1001)]
        [InlineData(1999)]
        [InlineData(999000)]
        [InlineData(999001)]
        [InlineData(999999)]
        [InlineData(1000000)]
        [InlineData(1000001)]
        [InlineData(1000999)]
        [InlineData(1001000)]
        [InlineData(1001001)]
        [InlineData(1999999)]
        public static void AccuracyRoundtrips(long totalMicroseconds)
        {
            Rfc3161TimestampTokenInfo info = new Rfc3161TimestampTokenInfo(
                new Oid("0.0", "0.0"),
                new Oid(Oids.Sha256),
                new byte[256 / 8],
                new byte[] { 1 },
                DateTimeOffset.UtcNow,
                accuracyInMicroseconds: totalMicroseconds);

            Assert.True(info.AccuracyInMicroseconds.HasValue);
            Assert.Equal(totalMicroseconds, info.AccuracyInMicroseconds.Value);

            byte[] encoded = info.Encode();

            Rfc3161TimestampTokenInfo info2;
            int bytesConsumed;

            Assert.True(
                Rfc3161TimestampTokenInfo.TryDecode(encoded, out info2, out bytesConsumed));

            Assert.Equal(encoded.Length, bytesConsumed);
            Assert.NotNull(info2);
            Assert.True(info2.AccuracyInMicroseconds.HasValue);
            Assert.Equal(totalMicroseconds, info2.AccuracyInMicroseconds.Value);
        }

        [Fact]
        public static void NegativeAccuracyThrows()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>(
                "accuracyInMicroseconds",
                () => new Rfc3161TimestampTokenInfo(
                    new Oid("0.0", "0.0"),
                    new Oid(Oids.Sha256),
                    new byte[256 / 8],
                    new byte[] { 2 },
                    DateTimeOffset.UtcNow,
                    accuracyInMicroseconds: -1));
        }

        internal static void AssertEqual(TimestampTokenTestData testData, Rfc3161TimestampTokenInfo tokenInfo)
        {
            Assert.Equal(testData.Version, tokenInfo.Version);
            Assert.Equal(testData.PolicyId, tokenInfo.PolicyId.Value);
            Assert.Equal(testData.HashAlgorithmId, tokenInfo.HashAlgorithmId.Value);
            // FriendlyName should be set for known digest algorithms
            Assert.NotEqual(tokenInfo.HashAlgorithmId.Value, tokenInfo.HashAlgorithmId.FriendlyName);
            Assert.Equal(testData.HashBytes.ByteArrayToHex(), tokenInfo.GetMessageHash().ByteArrayToHex());
            Assert.Equal(testData.SerialNumberBytes.ByteArrayToHex(), tokenInfo.GetSerialNumber().ByteArrayToHex());
            Assert.Equal(testData.Timestamp, tokenInfo.Timestamp);
            Assert.Equal(TimeSpan.Zero, tokenInfo.Timestamp.Offset);
            Assert.Equal(testData.AccuracyInMicroseconds, tokenInfo.AccuracyInMicroseconds);

            if (testData.IsOrdering)
            {
                Assert.True(tokenInfo.IsOrdering, "tokenInfo.IsOrdering");
            }
            else
            {
                Assert.False(tokenInfo.IsOrdering, "tokenInfo.IsOrdering");
            }

            Assert.Equal(testData.NonceBytes?.ByteArrayToHex(), tokenInfo.GetNonce()?.ByteArrayToHex());
            Assert.Equal(testData.TsaNameBytes?.ByteArrayToHex(), tokenInfo.GetTimestampAuthorityName()?.ByteArrayToHex());

            if (testData.ExtensionsBytes == null)
            {
                Assert.False(tokenInfo.HasExtensions, "tokenInfo.HasExtensions");
                Assert.NotNull(tokenInfo.GetExtensions());
                Assert.Equal(0, tokenInfo.GetExtensions().Count);

                // GetExtensions always returns a new collection.
                Assert.NotSame(tokenInfo.GetExtensions(), tokenInfo.GetExtensions());
            }
            else
            {
                Assert.True(tokenInfo.HasExtensions, "tokenInfo.HasExtensions");
                Assert.NotNull(tokenInfo.GetExtensions());

                Assert.True(false, "A test handler has been written for extensions...");

                // GetExtensions always returns a new collection.
                Assert.NotSame(tokenInfo.GetExtensions(), tokenInfo.GetExtensions());
            }
        }
    }
}
