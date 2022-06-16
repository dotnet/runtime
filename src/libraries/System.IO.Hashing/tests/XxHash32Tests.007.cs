// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;
using Xunit;

namespace System.IO.Hashing.Tests
{
    public class XxHash32Tests_Seeded_007 : NonCryptoHashTestDriver
    {
        private int Seed = 0x007_007_00;

        private static readonly byte[] s_emptyHashValue = new byte[] { 0xC4, 0xD2, 0x6D, 0x9A };

        public XxHash32Tests_Seeded_007()
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

        private const string DotNetHashesThis = ".NET Hashes This!";
        private const string DotNetHashesThis3 = DotNetHashesThis + DotNetHashesThis + DotNetHashesThis;
        private const string DotNetNCHashing = ".NET now has non-crypto hashing";
        private const string DotNetNCHashing3 = DotNetNCHashing + DotNetNCHashing + DotNetNCHashing;
        private const string SixteenBytes = ".NET Hashes This";
        private const string SixteenBytes3 = SixteenBytes + SixteenBytes + SixteenBytes;

        protected static IEnumerable<TestCase> TestCaseDefinitions { get; } =
            new[]
            {
                // Same inputs as the main XxHash32 tests, but with the seed applied.
                new TestCase(
                    "Nobody inspects the spammish repetition",
                    "Nobody inspects the spammish repetition"u8.ToArray(),
                    "21C69E3A"),
                new TestCase(
                    "The quick brown fox jumps over the lazy dog",
                    "The quick brown fox jumps over the lazy dog"u8.ToArray(),
                    "558D024f"),
                new TestCase(
                    "The quick brown fox jumps over the lazy dog.",
                    "The quick brown fox jumps over the lazy dog."u8.ToArray(),
                    "E7D2C0A7"),
                new TestCase(
                    "abc",
                    "abc"u8.ToArray(),
                    "D46070BB"),
                new TestCase(
                    "123456",
                    "313233343536",
                    "4256C97B"),
                new TestCase(
                    "12345678901234567890",
                    "3132333435363738393031323334353637383930",
                    "A57B8FEE"),
                new TestCase(
                    "123456789012345678901",
                    "313233343536373839303132333435363738393031",
                    "6789AD70"),
                new TestCase(
                    $"{DotNetHashesThis} (x3)",
                    Encoding.ASCII.GetBytes(DotNetHashesThis3),
                    "C5231E05"),
                new TestCase(
                    $"{DotNetNCHashing} (x3)",
                    Encoding.ASCII.GetBytes(DotNetNCHashing3),
                    "CABC8ABD"),
                // stripe size
                new TestCase(
                    $"{SixteenBytes} (x3)",
                    Encoding.ASCII.GetBytes(SixteenBytes3),
                    "AD98EBD3")
            };

        protected override NonCryptographicHashAlgorithm CreateInstance() => new XxHash32(Seed);

        protected override byte[] StaticOneShot(byte[] source) => XxHash32.Hash(source, Seed);

        protected override byte[] StaticOneShot(ReadOnlySpan<byte> source) => XxHash32.Hash(source, Seed);

        protected override int StaticOneShot(ReadOnlySpan<byte> source, Span<byte> destination) =>
            XxHash32.Hash(source, destination, Seed);

        protected override bool TryStaticOneShot(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten) =>
            XxHash32.TryHash(source, destination, out bytesWritten, Seed);

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
