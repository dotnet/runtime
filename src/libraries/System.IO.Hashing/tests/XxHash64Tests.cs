// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;
using Xunit;

namespace System.IO.Hashing.Tests
{
    public class XxHash64Tests : NonCryptoHashTestDriver
    {
        private static readonly byte[] s_emptyHashValue =
            new byte[] { 0xEF, 0x46, 0xDB, 0x37, 0x51, 0xD8, 0xE9, 0x99 };

        public XxHash64Tests()
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
        private const string SixteenBytes = "0123456789ABCDEF";
        private const string SixteenBytes3 = SixteenBytes + SixteenBytes + SixteenBytes;

        protected static IEnumerable<TestCase> TestCaseDefinitions { get; } =
            new[]
            {
                //https://asecuritysite.com/encryption/xxHash, Example 1
                new TestCase(
                    "Nobody inspects the spammish repetition",
                    "Nobody inspects the spammish repetition"u8.ToArray(),
                    "FBCEA83C8A378BF1"),
                //https://asecuritysite.com/encryption/xxHash, Example 2
                new TestCase(
                    "The quick brown fox jumps over the lazy dog",
                    "The quick brown fox jumps over the lazy dog"u8.ToArray(),
                    "0B242D361FDA71BC"),
                //https://asecuritysite.com/encryption/xxHash, Example 3
                new TestCase(
                    "The quick brown fox jumps over the lazy dog.",
                    "The quick brown fox jumps over the lazy dog."u8.ToArray(),
                    "44AD33705751AD73"),

                // Manual exploration to force boundary conditions in code coverage.
                // Output values produced by existing tools.

                // The "in three pieces" test causes this to build up in the accumulator every time.
                new TestCase(
                    "abc",
                    "abc"u8.ToArray(),
                    "44BC2CF5AD770999"),
                // Accumulates every time.
                new TestCase(
                    "123456",
                    "313233343536",
                    "2B2DC38AAA53C322"),
                // In the 3 pieces test, the third call to Append fills and process the holdback,
                // then stores the remainder.
                // The remainder is 40-32 = 8 bytes, so the Complete phase hits 1/0/0.
                new TestCase(
                    "1234567890123456789012345678901234567890",
                    "31323334353637383930313233343536373839303132333435363738393031323334353637383930",
                    "5F3AF5E23EEB431D"),
                // Same as above, but ends up with a byte that doesn't align to ulongs (1/0/1)
                new TestCase(
                    "12345678901234567890123456789012345678901",
                    "3132333435363738393031323334353637383930313233343536373839303132333435363738393031",
                    "45A03ED59AB5CAD6"),
                // Same as above, but ends up with a spare uint (1/1/0)
                new TestCase(
                    "12345678901234567890123456789012345678901234",
                    "3132333435363738393031323334353637383930313233343536373839303132333435363738393031323334",
                    "CA5C1B5B9061279F"),
                // The maximum amount of remainder work, 31 bytes (3/1/3)
                new TestCase(
                    DotNetNCHashing,
                    Encoding.ASCII.GetBytes(DotNetNCHashing),
                    "D8444D7806DFDE0E"),
                // 33 * 3 bytes long, in the "in three pieces" test each call to Append processes
                // a lane and holds one byte.
                new TestCase(
                    $"{ThirtyThreeBytes} (x3)",
                    Encoding.ASCII.GetBytes(ThirtyThreeBytes3),
                    "488DF4E623587E10"),
                // 63 * 3 bytes long.  In the "in three pieces" test the later calls to Append call
                // into ProcessStripe with unaligned starts.
                new TestCase(
                    $"{SixtyThreeBytes} (x3)",
                    Encoding.ASCII.GetBytes(SixtyThreeBytes3),
                    "239C7B3A85BD22B3"),
                // stripe size
                new TestCase(
                    $"{ThirtyTwoBytes} (x3)",
                    Encoding.ASCII.GetBytes(ThirtyTwoBytes3),
                    "975E3E6FE7E67FBC"),
                // 16 * 3 bytes, filling the holdback buffer exactly on the second Append call.
                new TestCase(
                    $"{SixteenBytes} (x3)",
                    Encoding.ASCII.GetBytes(SixteenBytes3),
                    "BDD40F0FAC166EAA"),
            };

        public static IEnumerable<object[]> LargeTestCases
        {
            get
            {
                object[] arr = new object[1];

                foreach (LargeTestCase testCase in LargeTestCaseDefinitions)
                {
                    arr[0] = testCase;
                    yield return arr;
                }
            }
        }

        protected static IEnumerable<LargeTestCase> LargeTestCaseDefinitions { get; } =
            new[]
            {
                // Manually run against the xxHash64 reference implementation.
                new LargeTestCase(
                    "EEEEE... (10GB)",
                    (byte)'E',
                    10L * 1024 * 1024 * 1024, // 10 GB
                    "F3CB8D45A8B695EF"),
            };

        protected override NonCryptographicHashAlgorithm CreateInstance() => new XxHash64();

        protected override byte[] StaticOneShot(byte[] source) => XxHash64.Hash(source);

        protected override byte[] StaticOneShot(ReadOnlySpan<byte> source) => XxHash64.Hash(source);

        protected override int StaticOneShot(ReadOnlySpan<byte> source, Span<byte> destination) =>
            XxHash64.Hash(source, destination);

        protected override bool TryStaticOneShot(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten) =>
            XxHash64.TryHash(source, destination, out bytesWritten);

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
        [MemberData(nameof(LargeTestCases))]
        [OuterLoop]
        public void InstanceMultiAppendLargeInput(LargeTestCase testCase)
        {
            InstanceMultiAppendLargeInputDriver(testCase);
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
