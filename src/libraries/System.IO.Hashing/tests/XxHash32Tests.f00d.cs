// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;
using Xunit;

namespace System.IO.Hashing.Tests
{
    public class XxHash32Tests_Seeded_f00d : NonCryptoHashTestDriver
    {
        private int Seed = unchecked((int)0xF00D_F00D);

        private static readonly byte[] s_emptyHashValue = new byte[] { 0x62, 0xB3, 0x2B, 0x9D };

        public XxHash32Tests_Seeded_f00d()
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

        protected static IEnumerable<TestCase> TestCaseDefinitions { get; } =
            new[]
            {
                // Same inputs as the main XxHash32 tests, but with the seed applied.
                new TestCase(
                    "Nobody inspects the spammish repetition",
                    Encoding.ASCII.GetBytes("Nobody inspects the spammish repetition"),
                    "E8FF660B"),
                new TestCase(
                    "The quick brown fox jumps over the lazy dog",
                    Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog"),
                    "C2B00BA1"),
                new TestCase(
                    "The quick brown fox jumps over the lazy dog.",
                    Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog."),
                    "11AC3BD7"),
                new TestCase(
                    "abc",
                    Encoding.ASCII.GetBytes("abc"),
                    "BC85BB95"),
                new TestCase(
                    "123456",
                    "313233343536",
                    "F549D3A7"),
                new TestCase(
                    "12345678901234567890",
                    "3132333435363738393031323334353637383930",
                    "E6173FEA"),
                new TestCase(
                    "123456789012345678901",
                    "313233343536373839303132333435363738393031",
                    "5F086DF1"),
                new TestCase(
                    $"{DotNetHashesThis} (x3)",
                    Encoding.ASCII.GetBytes(DotNetHashesThis3),
                    "893F4A6F"),
                new TestCase(
                    $"{DotNetNCHashing} (x3)",
                    Encoding.ASCII.GetBytes(DotNetNCHashing3),
                    "5A513E6D"),
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
