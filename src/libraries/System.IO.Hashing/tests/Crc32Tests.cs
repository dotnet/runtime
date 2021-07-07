// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;
using Xunit;

namespace System.IO.Hashing.Tests
{
    public class Crc32Tests : NonCryptoHashTestDriver
    {
        private static readonly byte[] s_emptyHashValue = new byte[4];

        public Crc32Tests()
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

        protected static IEnumerable<TestCase> TestCaseDefinitions { get; } =
            new[]
            {
                new TestCase(
                    "Empty",
                    "",
                    "00000000"),
                new TestCase(
                    "One",
                    "01",
                    "1BDF05A5"),
                new TestCase(
                    "Zero-Residue",
                    "00000000",
                    "1CDF4421"),
                new TestCase(
                    "Zero-InverseResidue",
                    "FFFFFFFF",
                    "FFFFFFFF"),
                new TestCase(
                    "Self-test 123456789",
                    Encoding.ASCII.GetBytes("123456789"),
                    "2639F4CB"),
                new TestCase(
                    "Self-test residue",
                    "3132333435363738392639F4CB",
                    "1CDF4421"),
                new TestCase(
                    "Self-test inverse residue",
                    "313233343536373839D9C60B34",
                    "FFFFFFFF"),
                new TestCase(
                    "The quick brown fox jumps over the lazy dog",
                    Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog"),
                    "39A34F41"),
            };

        protected override NonCryptographicHashAlgorithm CreateInstance() => new Crc32();

        protected override byte[] StaticOneShot(byte[] source) => Crc32.Hash(source);

        protected override byte[] StaticOneShot(ReadOnlySpan<byte> source) => Crc32.Hash(source);

        protected override int StaticOneShot(ReadOnlySpan<byte> source, Span<byte> destination) =>
            Crc32.Hash(source, destination);

        protected override bool TryStaticOneShot(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten) =>
            Crc32.TryHash(source, destination, out bytesWritten);

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

        [SkipOnCoreClr("https://github.com/dotnet/runtime/issues/54007", RuntimeConfiguration.Checked)]
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
