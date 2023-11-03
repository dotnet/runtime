// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;
using Xunit;

namespace System.IO.Hashing.Tests
{
    public class Crc64Tests : NonCryptoHashTestDriver
    {
        private static readonly byte[] s_emptyHashValue = new byte[8];

        public Crc64Tests()
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
                    "0000000000000000"),
                new TestCase(
                    "One",
                    "01",
                    "42F0E1EBA9EA3693"),
                // Because CRC-64 has an initial state of 0, any input that is all
                // zero-valued bytes will always produce 0x0ul as the output, since it
                // never leaves state 0.
                new TestCase(
                    "{ 0x00 }",
                    "00",
                    "0000000000000000"),
                // Once it has left the initial state, zero-value bytes matter.
                new TestCase(
                    "{ 0x01, 0x00 }",
                    "0100",
                    "AF052A6B538EDF09"),
                new TestCase(
                    "Zero-Residue",
                    "0000000000000000",
                    "0000000000000000"),
                new TestCase(
                    "Self-test 123456789",
                    "123456789"u8.ToArray(),
                    "6C40DF5F0B497347"),
                new TestCase(
                    "Self-test residue",
                    "3132333435363738396C40DF5F0B497347",
                    "0000000000000000"),
                new TestCase(
                    "The quick brown fox jumps over the lazy dog",
                    "The quick brown fox jumps over the lazy dog"u8.ToArray(),
                    "41E05242FFA9883B"),
                // Test 256 bytes for vector optimizations
                new TestCase(
                    "Lorem ipsum 256",
                    "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Praesent non ipsum quis mauris euismod hendrerit id sed lacus. Duis quam neque, porta et volutpat nec, tempor eget nisl. Nunc quis leo quis nisi mattis molestie. Donec a diam velit. Sed a tempus nec."u8.ToArray(),
                    "DA70046E6B79DD83"),
                // Test a multiple of 128 bytes greater than 256 bytes + 16 bytes for vector optimizations
                new TestCase(
                    "Lorem ipsum 272",
                    "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Praesent non ipsum quis mauris euismod hendrerit id sed lacus. Duis quam neque, porta et volutpat nec, tempor eget nisl. Nunc quis leo quis nisi mattis molestie. Donec a diam velit. Sed a tempus nec1234567890abcdef."u8.ToArray(),
                    "A94F5E9C5557F65A"),
                // Test a multiple of 128 bytes greater than 256 bytes for vector optimizations
                new TestCase(
                    "Lorem ipsum 384",
                    "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nullam lobortis non felis et pretium. Suspendisse commodo dignissim sagittis. Etiam vestibulum luctus mollis. Ut finibus, nisl in sodales sagittis, leo mauris sollicitudin odio, id sodales nisl ante vitae quam. Nunc ut mi at lacus ultricies efficitur vitae eu ligula. Donec tincidunt, nisi suscipit facilisis auctor, metus non."u8.ToArray(),
                    "5768E3F2E9A63829"),
                // Test data that is > 256 bytes but not a multiple of 16 for vector optimizations
                new TestCase(
                     "Lorem ipsum 1001",
                     "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Integer ac urna vitae nibh sagittis porttitor et vel ante. Ut molestie sit amet velit ac mattis. Sed ullamcorper nunc non neque imperdiet, vehicula bibendum sapien efficitur. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia curae; Suspendisse potenti. Duis sem dui, malesuada non pharetra at, feugiat id mi. Nulla facilisi. Fusce a scelerisque magna. Ut leo justo, auctor quis nisi et, sollicitudin pretium odio. Sed eu nibh mollis, pretium lectus nec, posuere nulla. Morbi ac euismod purus. Morbi rhoncus leo est, at volutpat nunc pretium in. Aliquam erat volutpat. Curabitur eu lacus mollis, varius lectus ut, tincidunt eros. Nullam a velit hendrerit, euismod magna id, fringilla sem. Phasellus scelerisque hendrerit est, vel imperdiet enim auctor a. Aenean vel ultricies nunc. Suspendisse ac tincidunt urna. Nulla tempor dolor ut ligula accumsan, tempus auctor massa gravida. Aenean non odio et augue pellena."u8.ToArray(),
                     "3ECF3A363FC5BD59"),
            };

        protected override NonCryptographicHashAlgorithm CreateInstance() => new Crc64();

        protected override byte[] StaticOneShot(byte[] source) => Crc64.Hash(source);

        protected override byte[] StaticOneShot(ReadOnlySpan<byte> source) => Crc64.Hash(source);

        protected override int StaticOneShot(ReadOnlySpan<byte> source, Span<byte> destination) =>
            Crc64.Hash(source, destination);

        protected override bool TryStaticOneShot(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten) =>
            Crc64.TryHash(source, destination, out bytesWritten);

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
        public void VerifyHashToUInt64(TestCase testCase)
        {
            var alg = new Crc64();
            alg.Append(testCase.Input);
            AssertEqualHashNumber(testCase.OutputHex, alg.GetCurrentHashAsUInt64());

            AssertEqualHashNumber(testCase.OutputHex, Crc64.HashToUInt64(testCase.Input));
        }
    }
}
