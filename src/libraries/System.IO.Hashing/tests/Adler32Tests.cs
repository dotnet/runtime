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
        [InlineData(5553, 0xAA40476Bu)]
        [InlineData(11104, 0xA2778E87u)]
        public void LargeInput_ExceedsNMax(int length, uint expected)
        {
            byte[] data = new byte[length];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (byte)('a' + (i % 26));
            }

            Assert.Equal(expected, Adler32.HashToUInt32(data));

            var alg = new Adler32();
            alg.Append(data);
            Assert.Equal(expected, alg.GetCurrentHashAsUInt32());
        }

        /// <summary>
        /// Tests a wide variety of lengths to exercise scalar, Vector128, Vector256, and Vector512
        /// code paths as well as their transitions and tail handling.
        /// </summary>
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(7)]
        [InlineData(15)]
        [InlineData(16)]
        [InlineData(17)]
        [InlineData(31)]
        [InlineData(32)]
        [InlineData(33)]
        [InlineData(47)]
        [InlineData(48)]
        [InlineData(63)]
        [InlineData(64)]
        [InlineData(65)]
        [InlineData(95)]
        [InlineData(96)]
        [InlineData(127)]
        [InlineData(128)]
        [InlineData(129)]
        [InlineData(255)]
        [InlineData(256)]
        [InlineData(512)]
        [InlineData(1000)]
        [InlineData(1023)]
        [InlineData(1024)]
        [InlineData(4096)]
        [InlineData(5551)]
        [InlineData(5552)]
        [InlineData(5553)]
        [InlineData(5600)]
        [InlineData(8192)]
        [InlineData(11104)]
        [InlineData(16384)]
        public void VariousLengths_MatchesReference(int length)
        {
            byte[] data = new byte[length];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (byte)(i % 251);
            }

            uint expected = ReferenceAdler32(data);
            Assert.Equal(expected, Adler32.HashToUInt32(data));

            var alg = new Adler32();
            alg.Append(data);
            Assert.Equal(expected, alg.GetCurrentHashAsUInt32());
        }

        /// <summary>
        /// Tests with all-0xFF bytes, which maximizes accumulator values and stresses
        /// overflow-safe behavior in the vectorized paths.
        /// </summary>
        [Theory]
        [InlineData(32)]
        [InlineData(64)]
        [InlineData(128)]
        [InlineData(256)]
        [InlineData(5552)]
        [InlineData(5553)]
        public void AllMaxBytes_MatchesReference(int length)
        {
            byte[] data = new byte[length];
            data.AsSpan().Fill(0xFF);

            Assert.Equal(ReferenceAdler32(data), Adler32.HashToUInt32(data));
        }

        /// <summary>
        /// Tests incremental appending with various chunk sizes to verify that the
        /// vectorized paths produce the same result regardless of how data is fed in.
        /// </summary>
        [Theory]
        [InlineData(1)]
        [InlineData(7)]
        [InlineData(16)]
        [InlineData(32)]
        [InlineData(64)]
        [InlineData(100)]
        public void IncrementalAppend_MatchesOneShot(int chunkSize)
        {
            byte[] data = new byte[1000];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (byte)(i * 7 + 13);
            }

            uint oneShot = Adler32.HashToUInt32(data);

            var alg = new Adler32();
            int offset = 0;
            while (offset < data.Length)
            {
                int len = Math.Min(chunkSize, data.Length - offset);
                alg.Append(data.AsSpan(offset, len));
                offset += len;
            }

            Assert.Equal(oneShot, alg.GetCurrentHashAsUInt32());
        }

        /// <summary>
        /// Computes a reference Adler32 result using the simplest possible scalar implementation.
        /// </summary>
        private static uint ReferenceAdler32(ReadOnlySpan<byte> data, uint adler = 1)
        {
            const uint Base = 65521;

            uint s1 = adler & 0xFFFF;
            uint s2 = (adler >> 16) & 0xFFFF;

            foreach (byte b in data)
            {
                s1 = (s1 + b) % Base;
                s2 = (s2 + s1) % Base;
            }

            return (s2 << 16) | s1;
        }
    }
}
