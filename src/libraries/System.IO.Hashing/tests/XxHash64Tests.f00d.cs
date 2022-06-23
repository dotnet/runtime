// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;
using Xunit;

namespace System.IO.Hashing.Tests
{
    public class XxHash64Tests_Seeded_f00d : NonCryptoHashTestDriver
    {
        private long Seed = unchecked((long)0xF00D_F00D_F00D_F00D);

        private static readonly byte[] s_emptyHashValue =
            new byte[] { 0x01, 0x3F, 0x6A, 0x1B, 0xB3, 0x9C, 0x10, 0x87 };

        public XxHash64Tests_Seeded_f00d()
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
                    "76E6275980CF4E30"),
                new TestCase(
                    "The quick brown fox jumps over the lazy dog",
                    "The quick brown fox jumps over the lazy dog"u8.ToArray(),
                    "5CC25b2B8248DF76"),
                new TestCase(
                    "The quick brown fox jumps over the lazy dog.",
                    "The quick brown fox jumps over the lazy dog."u8.ToArray(),
                    "10E8D9E4DA841407"),
                new TestCase(
                    "abc",
                    "abc"u8.ToArray(),
                    "AE9DA0E407940A85"),
                new TestCase(
                    "123456",
                    "313233343536",
                    "99E249386C1EB47D"),
                new TestCase(
                    "1234567890123456789012345678901234567890",
                    "31323334353637383930313233343536373839303132333435363738393031323334353637383930",
                    "0DA1BD86FAAA1BC8"),
                new TestCase(
                    "12345678901234567890123456789012345678901",
                    "3132333435363738393031323334353637383930313233343536373839303132333435363738393031",
                    "913DFB6C43C01EB3"),
                new TestCase(
                    "12345678901234567890123456789012345678901234",
                    "3132333435363738393031323334353637383930313233343536373839303132333435363738393031323334",
                    "F15334D3BCFC5841"),
                new TestCase(
                    DotNetNCHashing,
                    Encoding.ASCII.GetBytes(DotNetNCHashing),
                    "C5551996D9F3737F"),
                new TestCase(
                    $"{ThirtyThreeBytes} (x3)",
                    Encoding.ASCII.GetBytes(ThirtyThreeBytes3),
                    "6BD086C0CC425D9F"),
                new TestCase(
                    $"{SixtyThreeBytes} (x3)",
                    Encoding.ASCII.GetBytes(SixtyThreeBytes3),
                    "6F1C62EB48EA2FEC"),
                // stripe size
                new TestCase(
                    $"{ThirtyTwoBytes} (x3)",
                    Encoding.ASCII.GetBytes(ThirtyTwoBytes3),
                    "B358EB96B8E3E7AD")
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
