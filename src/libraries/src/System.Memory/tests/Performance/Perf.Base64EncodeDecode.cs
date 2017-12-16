// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;
using Xunit;

namespace System.Buffers.Text.Tests
{
    public class Base64EncodeDecodeTests
    {
        private const int InnerCount = 1000;

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(1000 * 1000)]
        private static void Base64Encode(int numberOfBytes)
        {
            Span<byte> source = new byte[numberOfBytes];
            Base64TestHelper.InitalizeBytes(source);
            Span<byte> destination = new byte[Base64.GetMaxEncodedToUtf8Length(numberOfBytes)];

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        Base64.EncodeToUtf8(source, destination, out int consumed, out int written);
                }
            }

            Span<byte> backToSource = new byte[numberOfBytes];
            Base64.DecodeFromUtf8(destination, backToSource, out _, out _);
            Assert.True(source.SequenceEqual(backToSource));
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(1000 * 1000)]
        private static void Base64EncodeDestinationTooSmall(int numberOfBytes)
        {
            Span<byte> source = new byte[numberOfBytes];
            Base64TestHelper.InitalizeBytes(source);
            Span<byte> destination = new byte[Base64.GetMaxEncodedToUtf8Length(numberOfBytes) - 1];

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        Base64.EncodeToUtf8(source, destination, out int consumed, out int written);
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(1000 * 1000)]
        private static void Base64EncodeBaseline(int numberOfBytes)
        {
            var source = new byte[numberOfBytes];
            Base64TestHelper.InitalizeBytes(source.AsSpan());
            var destination = new char[Base64.GetMaxEncodedToUtf8Length(numberOfBytes)];

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        Convert.ToBase64CharArray(source, 0, source.Length, destination, 0);
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(1000 * 1000)]
        private static void Base64Decode(int numberOfBytes)
        {
            Span<byte> source = new byte[numberOfBytes];
            Base64TestHelper.InitalizeBytes(source);
            Span<byte> encoded = new byte[Base64.GetMaxEncodedToUtf8Length(numberOfBytes)];
            Base64.EncodeToUtf8(source, encoded, out _, out _);

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        Base64.DecodeFromUtf8(encoded, source, out int bytesConsumed, out int bytesWritten);
                }
            }

            Span<byte> backToEncoded = encoded.ToArray();
            Base64.EncodeToUtf8(source, encoded, out _, out _);
            Assert.True(backToEncoded.SequenceEqual(encoded));
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(1000 * 1000)]
        private static void Base64DecodeDetinationTooSmall(int numberOfBytes)
        {
            Span<byte> source = new byte[numberOfBytes];
            Base64TestHelper.InitalizeBytes(source);
            Span<byte> encoded = new byte[Base64.GetMaxEncodedToUtf8Length(numberOfBytes)];
            Base64.EncodeToUtf8(source, encoded, out _, out _);

            source = source.Slice(0, source.Length - 1);

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        Base64.DecodeFromUtf8(encoded, source, out int bytesConsumed, out int bytesWritten);
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(1000 * 1000)]
        private static void Base64DecodeBaseline(int numberOfBytes)
        {
            Span<byte> source = new byte[numberOfBytes];
            Base64TestHelper.InitalizeBytes(source);
            ReadOnlySpan<char> encoded = Convert.ToBase64String(source.ToArray()).ToCharArray();

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        Convert.TryFromBase64Chars(encoded, source, out int bytesWritten);
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(1000 * 1000)]
        private static void Base64EncodeInPlace(int numberOfBytes)
        {
            Span<byte> source = new byte[numberOfBytes];
            Base64TestHelper.InitalizeBytes(source);
            int length = Base64.GetMaxEncodedToUtf8Length(numberOfBytes);
            Span<byte> decodedSpan = new byte[length];
            source.CopyTo(decodedSpan);
            Span<byte> backupSpan = decodedSpan.ToArray();

            int bytesWritten = 0;
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        backupSpan.CopyTo(decodedSpan);
                        Base64.EncodeToUtf8InPlace(decodedSpan, numberOfBytes, out bytesWritten);
                    }
                }
            }

            Span<byte> backToSource = new byte[numberOfBytes];
            Base64.DecodeFromUtf8(decodedSpan, backToSource, out _, out _);
            Assert.True(backupSpan.Slice(0, numberOfBytes).SequenceEqual(backToSource));
        }

        [Benchmark]
        [InlineData(1000 * 1000)]
        private static void Base64EncodeInPlaceOnce(int numberOfBytes)
        {
            Span<byte> source = new byte[numberOfBytes];
            Base64TestHelper.InitalizeBytes(source);
            int length = Base64.GetMaxEncodedToUtf8Length(numberOfBytes);
            Span<byte> decodedSpan = new byte[length];
            source.CopyTo(decodedSpan);
            Span<byte> backupSpan = decodedSpan.ToArray();

            int bytesWritten = 0;
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                backupSpan.CopyTo(decodedSpan);
                using (iteration.StartMeasurement())
                {
                    Base64.EncodeToUtf8InPlace(decodedSpan, numberOfBytes, out bytesWritten);
                }
            }

            Span<byte> backToSource = new byte[numberOfBytes];
            Base64.DecodeFromUtf8(decodedSpan, backToSource, out _, out _);
            Assert.True(backupSpan.Slice(0, numberOfBytes).SequenceEqual(backToSource));
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(1000 * 1000)]
        private static void Base64DecodeInPlace(int numberOfBytes)
        {
            Span<byte> source = new byte[numberOfBytes];
            Base64TestHelper.InitalizeBytes(source);
            int length = Base64.GetMaxEncodedToUtf8Length(numberOfBytes);
            Span<byte> encodedSpan = new byte[length];
            Base64.EncodeToUtf8(source, encodedSpan, out _, out _);

            Span<byte> backupSpan = encodedSpan.ToArray();

            int bytesWritten = 0;
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        backupSpan.CopyTo(encodedSpan);
                        Base64.DecodeFromUtf8InPlace(encodedSpan, out bytesWritten);
                    }
                }
            }

            Assert.True(source.SequenceEqual(encodedSpan.Slice(0, bytesWritten)));
        }

        [Benchmark]
        [InlineData(1000 * 1000)]
        private static void Base64DecodeInPlaceOnce(int numberOfBytes)
        {
            Span<byte> source = new byte[numberOfBytes];
            Base64TestHelper.InitalizeBytes(source);
            int length = Base64.GetMaxEncodedToUtf8Length(numberOfBytes);
            Span<byte> encodedSpan = new byte[length];

            int bytesWritten = 0;
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                Base64.EncodeToUtf8(source, encodedSpan, out _, out _);
                using (iteration.StartMeasurement())
                {
                    Base64.DecodeFromUtf8InPlace(encodedSpan, out bytesWritten);
                }
            }

            Assert.True(source.SequenceEqual(encodedSpan.Slice(0, bytesWritten)));
        }
    }
}
