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
                    "123456789"u8.ToArray(),
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
                    "The quick brown fox jumps over the lazy dog"u8.ToArray(),
                    "39A34F41"),
                // Test a multiple of 64 bytes for vector optimizations
                new TestCase(
                    "Lorem ipsum 128",
                    "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Morbi quis iaculis nisl. Sed ornare sapien non nulla hendrerit viverra."u8.ToArray(),
                    "931A6737"),
                // Test a multiple of 64 bytes + 16 bytes for vector optimizations
                new TestCase(
                    "Lorem ipsum 144",
                    "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nulla libero est, semper in pharetra at, cursus id nulla. Class aptent taciti volutpat."u8.ToArray(),
                    "2B719549"),
                // Test data that is > 64 bytes but not a multiple of 16 for vector optimizations
                new TestCase(
                    "Lorem ipsum 1001",
                    "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Integer ac urna vitae nibh sagittis porttitor et vel ante. Ut molestie sit amet velit ac mattis. Sed ullamcorper nunc non neque imperdiet, vehicula bibendum sapien efficitur. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia curae; Suspendisse potenti. Duis sem dui, malesuada non pharetra at, feugiat id mi. Nulla facilisi. Fusce a scelerisque magna. Ut leo justo, auctor quis nisi et, sollicitudin pretium odio. Sed eu nibh mollis, pretium lectus nec, posuere nulla. Morbi ac euismod purus. Morbi rhoncus leo est, at volutpat nunc pretium in. Aliquam erat volutpat. Curabitur eu lacus mollis, varius lectus ut, tincidunt eros. Nullam a velit hendrerit, euismod magna id, fringilla sem. Phasellus scelerisque hendrerit est, vel imperdiet enim auctor a. Aenean vel ultricies nunc. Suspendisse ac tincidunt urna. Nulla tempor dolor ut ligula accumsan, tempus auctor massa gravida. Aenean non odio et augue pellena."u8.ToArray(),
                    "0464ED5F"),
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

        [Theory]
        [MemberData(nameof(TestCases))]
        public void VerifyHashToUInt32(TestCase testCase)
        {
            var alg = new Crc32();
            alg.Append(testCase.Input);
            AssertEqualHashNumber(testCase.OutputHex, alg.GetCurrentHashAsUInt32(), littleEndian: true);

            AssertEqualHashNumber(testCase.OutputHex, Crc32.HashToUInt32(testCase.Input), littleEndian: true);
        }
    }
}
