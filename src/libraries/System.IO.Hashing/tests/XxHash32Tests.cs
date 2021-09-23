// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;
using Xunit;

namespace System.IO.Hashing.Tests
{
    public class XxHash32Tests : NonCryptoHashTestDriver
    {
        private static readonly byte[] s_emptyHashValue = new byte[] { 0x02, 0xCC, 0x5D, 0x05 };

        public XxHash32Tests()
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
                //https://asecuritysite.com/encryption/xxHash, Example 1
                new TestCase(
                    "Nobody inspects the spammish repetition",
                    Encoding.ASCII.GetBytes("Nobody inspects the spammish repetition"),
                    "E2293B2F"),
                //https://asecuritysite.com/encryption/xxHash, Example 2
                new TestCase(
                    "The quick brown fox jumps over the lazy dog",
                    Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog"),
                    "E85EA4DE"),
                //https://asecuritysite.com/encryption/xxHash, Example 3
                new TestCase(
                    "The quick brown fox jumps over the lazy dog.",
                    Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog."),
                    "68D039C8"),

                // Manual exploration to force boundary conditions in code coverage.
                // Output values produced by existing tools.

                // The "in three pieces" test causes this to build up in the accumulator every time.
                new TestCase(
                    "abc",
                    Encoding.ASCII.GetBytes("abc"),
                    "32D153FF"),
                // Accumulates every time.
                new TestCase(
                    "123456",
                    "313233343536",
                    "B7014066"),
                // In the 3 pieces test, the third call to Append fills and process the holdback,
                // then stores the remainder.
                new TestCase(
                    "12345678901234567890",
                    "3132333435363738393031323334353637383930",
                    "2D0C3D1B"),
                // Same as above, but ends up with a byte that doesn't align to uints.
                new TestCase(
                    "123456789012345678901",
                    "313233343536373839303132333435363738393031",
                    "8ED1B04E"),
                // 17 * 3 bytes long, in the "in three pieces" test each call to Append processes
                // a lane and holds one byte.
                new TestCase(
                    $"{DotNetHashesThis} (x3)",
                    Encoding.ASCII.GetBytes(DotNetHashesThis3),
                    "1FE08A04"),
                // 31 * 3 bytes long.  In the "in three pieces" test the later calls to Append call
                // into ProcessStripe with unaligned starts.
                new TestCase(
                    $"{DotNetNCHashing} (x3)",
                    Encoding.ASCII.GetBytes(DotNetNCHashing3),
                    "65242024"),
            };

        protected override NonCryptographicHashAlgorithm CreateInstance() => new XxHash32();

        protected override byte[] StaticOneShot(byte[] source) => XxHash32.Hash(source);

        protected override byte[] StaticOneShot(ReadOnlySpan<byte> source) => XxHash32.Hash(source);

        protected override int StaticOneShot(ReadOnlySpan<byte> source, Span<byte> destination) =>
            XxHash32.Hash(source, destination);

        protected override bool TryStaticOneShot(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten) =>
            XxHash32.TryHash(source, destination, out bytesWritten);

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
