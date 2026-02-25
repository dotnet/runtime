// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.IO.Hashing.Tests
{
    public class Adler32Tests : NonCryptoHashTestDriver
    {
        private static readonly byte[] s_emptyHashValue = [00, 00, 00, 01];

        public Adler32Tests()
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
                    "00000001"),
                new TestCase(
                    "One",
                    "01",
                    "00020002"),
                new TestCase(
                    "Self-test 123456789",
                    "123456789"u8.ToArray(),
                    "091E01DE"),
                new TestCase(
                    "The quick brown fox jumps over the lazy dog",
                    "The quick brown fox jumps over the lazy dog"u8.ToArray(),
                    "5BDC0FDA"),
                // Test a multiple of 64 bytes for vector optimizations
                new TestCase(
                    "Lorem ipsum 128",
                    "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Morbi quis iaculis nisl. Sed ornare sapien non nulla hendrerit viverra."u8.ToArray(),
                    "ED662F5C"),
                // Test a multiple of 64 bytes + 16 bytes for vector optimizations
                new TestCase(
                    "Lorem ipsum 144",
                    "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nulla libero est, semper in pharetra at, cursus id nulla. Class aptent taciti volutpat."u8.ToArray(),
                    "FCB734CC"),
                // Test data that is > 64 bytes but not a multiple of 16 for vector optimizations
                new TestCase(
                    "Lorem ipsum 1001",
                    "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Integer ac urna vitae nibh sagittis porttitor et vel ante. Ut molestie sit amet velit ac mattis. Sed ullamcorper nunc non neque imperdiet, vehicula bibendum sapien efficitur. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia curae; Suspendisse potenti. Duis sem dui, malesuada non pharetra at, feugiat id mi. Nulla facilisi. Fusce a scelerisque magna. Ut leo justo, auctor quis nisi et, sollicitudin pretium odio. Sed eu nibh mollis, pretium lectus nec, posuere nulla. Morbi ac euismod purus. Morbi rhoncus leo est, at volutpat nunc pretium in. Aliquam erat volutpat. Curabitur eu lacus mollis, varius lectus ut, tincidunt eros. Nullam a velit hendrerit, euismod magna id, fringilla sem. Phasellus scelerisque hendrerit est, vel imperdiet enim auctor a. Aenean vel ultricies nunc. Suspendisse ac tincidunt urna. Nulla tempor dolor ut ligula accumsan, tempus auctor massa gravida. Aenean non odio et augue pellena."u8.ToArray(),
                    "8A836E53"),
            };

        protected override NonCryptographicHashAlgorithm CreateInstance() => new Adler32();
        protected override NonCryptographicHashAlgorithm Clone(NonCryptographicHashAlgorithm instance) => ((Adler32)instance).Clone();

        protected override byte[] StaticOneShot(byte[] source) => Adler32.Hash(source);

        protected override byte[] StaticOneShot(ReadOnlySpan<byte> source) => Adler32.Hash(source);

        protected override int StaticOneShot(ReadOnlySpan<byte> source, Span<byte> destination) =>
            Adler32.Hash(source, destination);

        protected override bool TryStaticOneShot(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten) =>
            Adler32.TryHash(source, destination, out bytesWritten);

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
            var alg = new Adler32();
            alg.Append(testCase.Input);
            AssertEqualHashNumber(testCase.OutputHex, alg.GetCurrentHashAsUInt32(), littleEndian: false);
            AssertEqualHashNumber(testCase.OutputHex, Adler32.HashToUInt32(testCase.Input), littleEndian: false);
        }

        [Theory]
        [InlineData(5553, 0x62C69C89u)]
        [InlineData(11104, 0xA8AE3724u)]
        public void LargeInput_ExceedsNMax(int length, uint expected)
        {
            // This test ensures that Adler32 optimizations involving delayed modulo
            // do not overflow a 32-bit intermediate at any point.

            var alg = new Adler32();

            // The maximum possible value of an Adler32 checksum is 0xFFF0FFF0,
            // which has both components just below the modulo value (0xFFF0 == 65520).
            // A sequence of 65519 ones will generate this value.

            byte[] primer = new byte[65519];
            primer.AsSpan().Fill(1);

            alg.Append(primer);
            Assert.Equal(0xFFF0FFF0, alg.GetCurrentHashAsUInt32());

            // Starting from an already-maxed checksum, a stream of 5553 max value
            // bytes will overflow if not reduced by mod 65521 before the last byte.

            byte[] data = new byte[length];
            data.AsSpan().Fill(byte.MaxValue);

            alg.Append(data);
            Assert.Equal(expected, alg.GetCurrentHashAsUInt32());
        }
    }
}
