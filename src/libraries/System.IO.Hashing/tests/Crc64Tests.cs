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
    }
}
