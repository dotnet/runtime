// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.IO.Hashing.Tests
{
    public abstract class NonCryptoHashTestDriver
    {
        private readonly int _hashLengthInBytes;
        private readonly byte[] _emptyHash;
        private string _emptyHashHex;

        protected NonCryptoHashTestDriver(byte[] emptyHash)
        {
            _hashLengthInBytes = emptyHash.Length;
            _emptyHash = emptyHash;
        }

        protected abstract NonCryptographicHashAlgorithm CreateInstance();

        protected abstract byte[] StaticOneShot(byte[] source);
        protected abstract byte[] StaticOneShot(ReadOnlySpan<byte> source);
        protected abstract int StaticOneShot(ReadOnlySpan<byte> source, Span<byte> destination);

        protected abstract bool TryStaticOneShot(
            ReadOnlySpan<byte> source,
            Span<byte> destination,
            out int bytesWritten);

        [Fact]
        public void TestsDefined()
        {
            const string DriverSuffix = "Driver";
            Type implType = GetType();
            Type defType = typeof(NonCryptoHashTestDriver);
            List<string>? missingMethods = null;

            foreach (MethodInfo info in defType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic))
            {
                if (info.IsFamily && info.Name.EndsWith(DriverSuffix, StringComparison.Ordinal))
                {
                    string targetMethodName = info.Name.Substring(0, info.Name.Length - DriverSuffix.Length);

                    MethodInfo info2 = implType.GetMethod(
                        targetMethodName,
                        BindingFlags.Instance | BindingFlags.Public);

                    if (info2 is null)
                    {
                        missingMethods ??= new List<string>();
                        missingMethods.Add(targetMethodName);
                    }
                }
            }

            if (missingMethods is not null)
            {
                Assert.Empty(missingMethods);
            }
        }

        [Fact]
        public void VerifyLengthProperty()
        {
            NonCryptographicHashAlgorithm hash = CreateInstance();
            Assert.Equal(_hashLengthInBytes, hash.HashLengthInBytes);
        }

        protected void InstanceAppendAllocateDriver(TestCase testCase)
        {
            NonCryptographicHashAlgorithm hash = CreateInstance();
            hash.Append(testCase.Input);
            byte[] output = hash.GetCurrentHash();

            Assert.Equal(testCase.OutputHex, TestCase.ToHexString(output));
        }

        protected void InstanceAppendAllocateAndResetDriver(TestCase testCase)
        {
            NonCryptographicHashAlgorithm hash = CreateInstance();
            hash.Append(testCase.Input);
            byte[] output = hash.GetHashAndReset();

            Assert.Equal(testCase.OutputHex, TestCase.ToHexString(output));

            int written = hash.GetHashAndReset(output);
            Assert.Equal(output.Length, written);
            VerifyEmptyResult(output);
        }

        protected void InstanceMultiAppendGetCurrentHashDriver(TestCase testCase)
        {
            ReadOnlySpan<byte> source = testCase.Input;
            int div3 = source.Length / 3;
            NonCryptographicHashAlgorithm hash = CreateInstance();
            hash.Append(source.Slice(0, div3));
            source = source.Slice(div3);
            hash.Append(source.Slice(0, div3));
            source = source.Slice(div3);
            hash.Append(source);

            Span<byte> buf = stackalloc byte[256];

            // May as well check unaligned writes.
            Span<byte> destination = buf.Slice(1);
            int written = hash.GetCurrentHash(destination);
            ReadOnlySpan<byte> answer = destination.Slice(0, written);

            testCase.VerifyResponse(answer);

            destination.Clear();
            hash.GetCurrentHash(destination);
            testCase.VerifyResponse(answer);
        }

        protected void InstanceVerifyEmptyStateDriver(TestCase testCase)
        {
            Span<byte> buf = stackalloc byte[256];
            NonCryptographicHashAlgorithm hash = CreateInstance();
            int written = hash.GetCurrentHash(buf);
            VerifyEmptyResult(buf.Slice(0, written));

            written = hash.GetHashAndReset(buf);
            VerifyEmptyResult(buf.Slice(0, written));
        }

        protected void InstanceVerifyResetStateDriver(TestCase testCase)
        {
            NonCryptographicHashAlgorithm hash = CreateInstance();
            Span<byte> buf = stackalloc byte[233];
            int written = hash.GetHashAndReset(buf);
            ReadOnlySpan<byte> ret = buf.Slice(0, written);
            VerifyEmptyResult(ret);

            hash.Append(testCase.Input);
            hash.Reset();
            hash.GetCurrentHash(buf);
            VerifyEmptyResult(ret);

            // Manual call to Reset while already in a pristine state.
            hash.Reset();
            hash.GetCurrentHash(buf);
            VerifyEmptyResult(ret);

            hash.Append(testCase.Input);
            hash.GetHashAndReset(buf);
            testCase.VerifyResponse(ret);

            hash.GetHashAndReset(buf);
            VerifyEmptyResult(ret);
        }

        [Fact]
        public void StaticOneShotNullArrayThrows()
        {
            AssertExtensions.Throws<ArgumentNullException>(
                "source",
                () => StaticOneShot((byte[])null));
        }

        protected void StaticVerifyOneShotArrayDriver(TestCase testCase)
        {
            byte[] answer = StaticOneShot(testCase.Input.ToArray());
            testCase.VerifyResponse(answer);
        }

        protected void StaticVerifyOneShotSpanToArrayDriver(TestCase testCase)
        {
            byte[] answer = StaticOneShot(testCase.Input);
            testCase.VerifyResponse(answer);
        }

        protected void StaticVerifyOneShotSpanToSpanDriver(TestCase testCase)
        {
            Span<byte> destination = stackalloc byte[256];

            int written = StaticOneShot(testCase.Input, destination);
            testCase.VerifyResponse(destination.Slice(0, written));
        }

        protected void StaticVerifyTryOneShotDriver(TestCase testCase)
        {
            Span<byte> destination = stackalloc byte[256];

            Assert.True(TryStaticOneShot(testCase.Input, destination, out int written));
            testCase.VerifyResponse(destination.Slice(0, written));
        }

        [Fact]
        public void StaticVerifyOneShotSpanTooShortThrows()
        {
            byte[] destination = new byte[256];

            for (int i = 0; i < _hashLengthInBytes; i++)
            {
                byte fill = (byte)~i;
                destination.AsSpan().Fill(fill);

                AssertExtensions.Throws<ArgumentException>(
                    "destination",
                    () => StaticOneShot(ReadOnlySpan<byte>.Empty, destination.AsSpan(0, i)));

                for (int j = 0; j < destination.Length; j++)
                {
                    Assert.Equal(fill, destination[j]);
                }
            }
        }

        [Fact]
        public void StaticVerifyOneShotSpanTooLongNoOverwrite()
        {
            Span<byte> buf = stackalloc byte[256];

            for (int i = 10; i < 40; i++)
            {
                buf.Fill((byte)i);

                int written = StaticOneShot(buf.Slice(0, i), buf.Slice(i));
                Assert.Equal(_hashLengthInBytes, written);

                for (int j = i + written; j < buf.Length; j++)
                {
                    Assert.Equal(i, buf[j]);
                }
            }
        }

        [Fact]
        public void StaticVerifyTryOneShotSpanTooLongNoOverwrite()
        {
            Span<byte> buf = stackalloc byte[256];

            for (int i = 10; i < 40; i++)
            {
                buf.Fill((byte)i);

                Assert.True(TryStaticOneShot(buf.Slice(0, i), buf.Slice(i), out int written));
                Assert.Equal(_hashLengthInBytes, written);

                for (int j = i + written; j < buf.Length; j++)
                {
                    Assert.Equal(i, buf[j]);
                }
            }
        }

        [Fact]
        public void StaticVerifyTryOneShotSpanTooShortNoWrites()
        {
            Span<byte> buf = stackalloc byte[256];

            for (int i = 0; i < _hashLengthInBytes; i++)
            {
                byte fill = (byte)~i;
                buf.Fill(fill);

                Assert.False(TryStaticOneShot(ReadOnlySpan<byte>.Empty, buf.Slice(0, i), out int written));
                Assert.Equal(0, written);

                for (int j = 0; j < buf.Length; j++)
                {
                    Assert.Equal(fill, buf[j]);
                }
            }
        }

        private void VerifyEmptyResult(ReadOnlySpan<byte> result)
        {
            if (!result.SequenceEqual(_emptyHash))
            {
                // We know this will fail, but it gives a nice presentation.

                Assert.Equal(
                    _emptyHashHex ??= TestCase.ToHexString(_emptyHash),
                    TestCase.ToHexString(result));
            }
        }

        public sealed class TestCase
        {
            private byte[] _input;
            private byte[] _output;

            public string Name { get; }
            public ReadOnlySpan<byte> Input => new ReadOnlySpan<byte>(_input);
            public string OutputHex { get; }

            public TestCase(string name, byte[] input, byte[] output)
            {
                Name = name;
                _input = input;
                OutputHex = ToHexString(output);
                _output = FromHexString(OutputHex);
            }

            public TestCase(string name, byte[] input, string outputHex)
            {
                Name = name;
                _input = input;
                OutputHex = outputHex.ToUpperInvariant();
                _output = FromHexString(OutputHex);
            }

            public TestCase(string name, string inputHex, string outputHex)
            {
                Name = name;
                _input = FromHexString(inputHex);
                OutputHex = outputHex.ToUpperInvariant();
                _output = FromHexString(OutputHex);
            }

            internal void VerifyResponse(ReadOnlySpan<byte> response)
            {
                if (!response.SequenceEqual(_output))
                {
                    // We know this will fail, but it gives a nice presentation.
                    Assert.Equal(OutputHex, ToHexString(response));
                }
            }

            internal static string ToHexString(ReadOnlySpan<byte> input)
            {
#if NET5_0_OR_GREATER
                return Convert.ToHexString(input);
#else
                var builder = new global::System.Text.StringBuilder(input.Length * 2);

                foreach (byte b in input)
                {
                    builder.Append(b.ToString("X2"));
                }

                return builder.ToString();
#endif
            }

            internal static byte[] FromHexString(string hexString)
            {
#if NET5_0_OR_GREATER
                return Convert.FromHexString(hexString);
#else
                byte[] bytes = new byte[hexString.Length / 2];

                for (int i = 0; i < hexString.Length; i += 2)
                {
                    string s = hexString.Substring(i, 2);
                    bytes[i / 2] = byte.Parse(s, global::System.Globalization.NumberStyles.HexNumber, null);
                }

                return bytes;
#endif
            }

            public override string ToString() => Name;
        }
    }
}
