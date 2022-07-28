// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;
using Xunit;

namespace System.IO.Hashing.Tests
{
    public class XxHash64Tests_Seeded_007 : NonCryptoHashTestDriver
    {
        private long Seed = 0x007_007_007_007_007;

        private static readonly byte[] s_emptyHashValue =
            new byte[] { 0x62, 0xAD, 0xCA, 0xD4, 0xEC, 0x80, 0x84, 0x0E };

        public XxHash64Tests_Seeded_007()
            : base(s_emptyHashValue)
        {
        }

        public static IEnumerable<object[]> TestCases
        {
            get
            {
                object[] arr = new object[1];

                foreach (TestCase testCase in TestCaseDefinitions)
                {
                    arr[0] = testCase;
                    yield return arr;
                }
            }
        }

        private const string ThirtyThreeBytes = "This string has 33 ASCII bytes...";
        private const string ThirtyThreeBytes3 = ThirtyThreeBytes + ThirtyThreeBytes + ThirtyThreeBytes;
        private const string DotNetNCHashing = ".NET now has non-crypto hashing";
        private const string SixtyThreeBytes = "A sixty-three byte test input requires substantial forethought!";
        private const string SixtyThreeBytes3 = SixtyThreeBytes + SixtyThreeBytes + SixtyThreeBytes;
        private const string ThirtyTwoBytes = "This string has 32 ASCII bytes..";
        private const string ThirtyTwoBytes3 = ThirtyTwoBytes + ThirtyTwoBytes + ThirtyTwoBytes;

        protected static IEnumerable<TestCase> TestCaseDefinitions { get; } =
            new[]
            {
                // Same inputs as the main XxHash64 tests, but with the seed applied.
                new TestCase(
                    "Nobody inspects the spammish repetition",
                    "Nobody inspects the spammish repetition"u8.ToArray(),
                    "C86A41E2F34280A0"),
                new TestCase(
                    "The quick brown fox jumps over the lazy dog",
                    "The quick brown fox jumps over the lazy dog"u8.ToArray(),
                    "BB05857F11B054EB"),
                new TestCase(
                    "The quick brown fox jumps over the lazy dog.",
                    "The quick brown fox jumps over the lazy dog."u8.ToArray(),
                    "618682461CB28F83"),
                new TestCase(
                    "abc",
                    "abc"u8.ToArray(),
                    "6BF4B26E3CA10C20"),
                new TestCase(
                    "123456",
                    "313233343536",
                    "CA35E96DF53D4962"),
                new TestCase(
                    "1234567890123456789012345678901234567890",
                    "31323334353637383930313233343536373839303132333435363738393031323334353637383930",
                    "FA3195B38205C088"),
                new TestCase(
                    "12345678901234567890123456789012345678901",
                    "3132333435363738393031323334353637383930313233343536373839303132333435363738393031",
                    "1980150281DF51A9"),
                new TestCase(
                    "12345678901234567890123456789012345678901234",
                    "3132333435363738393031323334353637383930313233343536373839303132333435363738393031323334",
                    "4AE47735FD53BF97"),
                new TestCase(
                    DotNetNCHashing,
                    Encoding.ASCII.GetBytes(DotNetNCHashing),
                    "D3ECF5BFE5F49B6F"),
                new TestCase(
                    $"{ThirtyThreeBytes} (x3)",
                    Encoding.ASCII.GetBytes(ThirtyThreeBytes3),
                    "27C38ACA51CD2684"),
                new TestCase(
                    $"{SixtyThreeBytes} (x3)",
                    Encoding.ASCII.GetBytes(SixtyThreeBytes3),
                    "D6095B93EB10BEDA"),
                // stripe size
                new TestCase(
                    $"{ThirtyTwoBytes} (x3)",
                    Encoding.ASCII.GetBytes(ThirtyTwoBytes3),
                    "45116421CF932B1F")
            };

        protected override NonCryptographicHashAlgorithm CreateInstance() => new XxHash64(Seed);

        protected override byte[] StaticOneShot(byte[] source) => XxHash64.Hash(source, Seed);

        protected override byte[] StaticOneShot(ReadOnlySpan<byte> source) => XxHash64.Hash(source, Seed);

        protected override int StaticOneShot(ReadOnlySpan<byte> source, Span<byte> destination) =>
            XxHash64.Hash(source, destination, Seed);

        protected override bool TryStaticOneShot(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten) =>
            XxHash64.TryHash(source, destination, out bytesWritten, Seed);

        [Theory]
        [MemberData(nameof(TestCases))]
        public void InstanceAppendAllocate(TestCase testCase)
        {
            InstanceAppendAllocateDriver(testCase);
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void InstanceAppendAllocateAndReset(TestCase testCase)
        {
            InstanceAppendAllocateAndResetDriver(testCase);
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void InstanceMultiAppendGetCurrentHash(TestCase testCase)
        {
            InstanceMultiAppendGetCurrentHashDriver(testCase);
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void InstanceVerifyEmptyState(TestCase testCase)
        {
            InstanceVerifyEmptyStateDriver(testCase);
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void InstanceVerifyResetState(TestCase testCase)
        {
            InstanceVerifyResetStateDriver(testCase);
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void StaticVerifyOneShotArray(TestCase testCase)
        {
            StaticVerifyOneShotArrayDriver(testCase);
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void StaticVerifyOneShotSpanToArray(TestCase testCase)
        {
            StaticVerifyOneShotSpanToArrayDriver(testCase);
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void StaticVerifyOneShotSpanToSpan(TestCase testCase)
        {
            StaticVerifyOneShotSpanToSpanDriver(testCase);
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void StaticVerifyTryOneShot(TestCase testCase)
        {
            StaticVerifyTryOneShotDriver(testCase);
        }
    }
}
