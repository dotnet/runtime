// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.IO.Hashing.Tests
{
    public abstract class Crc64Tests_Parameterized<T> : NonCryptoHashTestDriver
        where T : Crc64DriverBase, new()
    {
        private static readonly Crc64DriverBase s_driver = new T();
        private protected static readonly Crc64ParameterSet s_parameterSet = s_driver.ParameterSet;

        public Crc64Tests_Parameterized()
            : base(TestCaseBase.FromHexString(s_driver.EmptyOutput))
        {
        }

        public static IEnumerable<object[]> TestCases
        {
            get
            {
                object[] arr = new object[1];
                string residue = s_driver.Residue;

                foreach ((string Name, object Input) testCase in TestCaseDefinitions)
                {
                    string outputHex = testCase.Name switch
                    {
                        "Empty" => s_driver.EmptyOutput,
                        _ => s_driver.GetExpectedOutput(testCase.Name),
                    };

                    if (outputHex != null)
                    {
                        string inputHex;

                        if (testCase.Input is byte[] array)
                        {
                            arr[0] = new TestCase(testCase.Name, array, outputHex);
                            inputHex = TestCaseBase.ToHexString(array);
                        }
                        else
                        {
                            inputHex = (string)testCase.Input;
                            arr[0] = new TestCase(testCase.Name, inputHex, outputHex);
                        }
                        
                        yield return arr;

                        // If, in the future, refIn!=refOut is supported, then the residue and inverse residue test cases
                        // would need to be skipped, as they are only valid when refIn==refOut.
                        {
                            arr[0] = new TestCase(testCase.Name + " Residue", inputHex + outputHex, residue);
                            yield return arr;

                            if (s_parameterSet.FinalXorValue == ulong.MaxValue)
                            {
                                byte[] outputBytes = TestCaseBase.FromHexString(outputHex);

                                for (int i = 0; i < outputBytes.Length; i++)
                                {
                                    outputBytes[i] ^= 0xFF;
                                }

                                arr[0] = new TestCase(
                                    testCase.Name + " Inverse Residue",
                                    inputHex + TestCaseBase.ToHexString(outputBytes),
                                    "FFFFFFFFFFFFFFFF");

                                yield return arr;
                            }
                        }
                    }
                }
            }
        }

        private static (string Name, object Input)[] TestCaseDefinitions { get; } =
            new (string Name, object Input)[]
            {
                ("Empty", ""),
                ("One", "01"),
                ("Zero", "00000000"),
                ("{ 0x00 }", "00"),
                ("{ 0x01, 0x00 }", "0100"),
                ("Self-test 123456789", "123456789"u8.ToArray()),
                (
                    "The quick brown fox jumps over the lazy dog",
                    "The quick brown fox jumps over the lazy dog"u8.ToArray()
                ),
                // Test 256 bytes for vector optimizations
                (
                    "Lorem ipsum 256",
                    "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Praesent non ipsum quis mauris euismod hendrerit id sed lacus. Duis quam neque, porta et volutpat nec, tempor eget nisl. Nunc quis leo quis nisi mattis molestie. Donec a diam velit. Sed a tempus nec."u8.ToArray()
                ),
                // Test a multiple of 128 bytes greater than 256 bytes + 16 bytes for vector optimizations
                (
                    "Lorem ipsum 272",
                    "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Praesent non ipsum quis mauris euismod hendrerit id sed lacus. Duis quam neque, porta et volutpat nec, tempor eget nisl. Nunc quis leo quis nisi mattis molestie. Donec a diam velit. Sed a tempus nec1234567890abcdef."u8.ToArray()
                ),
                // Test a multiple of 128 bytes greater than 256 bytes for vector optimizations
                (
                    "Lorem ipsum 384",
                    "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nullam lobortis non felis et pretium. Suspendisse commodo dignissim sagittis. Etiam vestibulum luctus mollis. Ut finibus, nisl in sodales sagittis, leo mauris sollicitudin odio, id sodales nisl ante vitae quam. Nunc ut mi at lacus ultricies efficitur vitae eu ligula. Donec tincidunt, nisi suscipit facilisis auctor, metus non."u8.ToArray()
                ),
                // Test data that is > 256 bytes but not a multiple of 16 for vector optimizations
                (
                     "Lorem ipsum 1001",
                     "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Integer ac urna vitae nibh sagittis porttitor et vel ante. Ut molestie sit amet velit ac mattis. Sed ullamcorper nunc non neque imperdiet, vehicula bibendum sapien efficitur. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia curae; Suspendisse potenti. Duis sem dui, malesuada non pharetra at, feugiat id mi. Nulla facilisi. Fusce a scelerisque magna. Ut leo justo, auctor quis nisi et, sollicitudin pretium odio. Sed eu nibh mollis, pretium lectus nec, posuere nulla. Morbi ac euismod purus. Morbi rhoncus leo est, at volutpat nunc pretium in. Aliquam erat volutpat. Curabitur eu lacus mollis, varius lectus ut, tincidunt eros. Nullam a velit hendrerit, euismod magna id, fringilla sem. Phasellus scelerisque hendrerit est, vel imperdiet enim auctor a. Aenean vel ultricies nunc. Suspendisse ac tincidunt urna. Nulla tempor dolor ut ligula accumsan, tempus auctor massa gravida. Aenean non odio et augue pellena."u8.ToArray()
                ),
            };

        protected override NonCryptographicHashAlgorithm CreateInstance() => new Crc64(s_parameterSet);
        protected override NonCryptographicHashAlgorithm Clone(NonCryptographicHashAlgorithm instance) => ((Crc64)instance).Clone();

        protected override byte[] StaticOneShot(byte[] source) => Crc64.Hash(s_parameterSet, source);

        protected override byte[] StaticOneShot(ReadOnlySpan<byte> source) => Crc64.Hash(s_parameterSet, source);

        protected override int StaticOneShot(ReadOnlySpan<byte> source, Span<byte> destination) =>
            Crc64.Hash(s_parameterSet, source, destination);

        protected override bool TryStaticOneShot(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten) =>
            Crc64.TryHash(s_parameterSet, source, destination, out bytesWritten);

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
            var alg = new Crc64(s_parameterSet);
            alg.Append(testCase.Input);
            AssertEqualHashNumber(testCase.OutputHex, alg.GetCurrentHashAsUInt64(), littleEndian: s_parameterSet.ReflectValues);
            AssertEqualHashNumber(testCase.OutputHex, Crc64.HashToUInt64(s_parameterSet, testCase.Input), littleEndian: s_parameterSet.ReflectValues);
        }
    }

    public abstract class Crc64DriverBase
    {
        internal abstract Crc64ParameterSet ParameterSet { get; }

        internal abstract string EmptyOutput { get; }
        internal abstract string Residue { get; }

        internal abstract string? GetExpectedOutput(string testCaseName);
    }
}
