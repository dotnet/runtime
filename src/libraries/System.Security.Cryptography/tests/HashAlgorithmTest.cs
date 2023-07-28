// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Test.IO.Streams;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public class HashAlgorithmTest
    {
        [Fact]
        public void SpanMethodsUsed_NotOverridden_ArrayMethodsInvoked()
        {
            byte[] input = Enumerable.Range(0, 1024).Select(i => (byte)i).ToArray();
            byte[] output;
            int bytesWritten;

            var testAlgorithm = new SummingTestHashAlgorithm();

            output = new byte[sizeof(long) - 1];
            Assert.False(testAlgorithm.TryComputeHash(input, output, out bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.Equal<byte>(new byte[sizeof(long) - 1], output);

            output = new byte[sizeof(long)];
            Assert.True(testAlgorithm.TryComputeHash(input, output, out bytesWritten));
            Assert.Equal(sizeof(long), bytesWritten);
            Assert.Equal(input.Sum(b => (long)b), BitConverter.ToInt64(output, 0));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(4096)]
        [InlineData(4097)]
        [InlineData(10000)]
        public async Task VerifyComputeHashAsync(int size)
        {
            int fullCycles = size / 256;
            int partial = size % 256;
            // SUM(0..255) is 32640
            const long CycleSum = 32640L;

            // The formula for additive sum IS n*(n+1)/2, but size is a count and the first n is 0,
            // which happens to turn it into (n-1) * n / 2, aka n * (n - 1) / 2.
            long expectedSum = CycleSum * fullCycles + (partial * (partial - 1) / 2);

            using (PositionValueStream stream = new PositionValueStream(size))
            using (HashAlgorithm hash = new SummingTestHashAlgorithm())
            {
                byte[] result = await hash.ComputeHashAsync(stream);
                byte[] expected = BitConverter.GetBytes(expectedSum);

                Assert.Equal(expected, result);
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/37669", TestPlatforms.Browser)]
        public async Task ComputeHashAsync_SupportsCancellation()
        {
            using (CancellationTokenSource cancellationSource = new CancellationTokenSource())
            using (PositionValueStream stream = new SelfCancelingStream(10000, cancellationSource))
            using (HashAlgorithm hash = new SummingTestHashAlgorithm())
            {
                // The stream has a length longer than ComputeHashAsync's read buffer,
                // so ReadAsync will get called multiple times.
                // The first call succeeds, but moves the cancellation source to canceled,
                // and the second call then fails with an OperationCanceledException, canceling the
                // whole operation.
                await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    () => hash.ComputeHashAsync(stream, cancellationSource.Token));

                Assert.True(cancellationSource.IsCancellationRequested);
            }
        }

        [Fact]
        public void ComputeHashAsync_Disposed()
        {
            using (CancellationTokenSource cancellationSource = new CancellationTokenSource())
            using (PositionValueStream stream = new SelfCancelingStream(10000, cancellationSource))
            using (HashAlgorithm hash = new SummingTestHashAlgorithm())
            {
                hash.Dispose();

                Assert.Throws<ObjectDisposedException>(
                    () =>
                    {
                        // Not returning or awaiting the Task, it never got created.
                        hash.ComputeHashAsync(stream);
                    });

                // If SelfCancelingStream.Read (or ReadAsync) was called it will trip cancellation,
                // so use that as a signal for whether or not the stream was ever read from.
                Assert.False(cancellationSource.IsCancellationRequested, "Stream.Read was invoked");
            }
        }

        [Fact]
        public void ComputeHashAsync_RequiresStream()
        {
            using (HashAlgorithm hash = new SummingTestHashAlgorithm())
            {
                AssertExtensions.Throws<ArgumentNullException>(
                    "inputStream",
                    () =>
                    {
                        // Not returning or awaiting the Task, it never got created.
                        hash.ComputeHashAsync(null);
                    });
            }
        }

        private sealed class SummingTestHashAlgorithm : HashAlgorithm
        {
            private long _sum;

            public SummingTestHashAlgorithm() => HashSizeValue = sizeof(long)*8;

            public override void Initialize() => _sum = 0;

            protected override void HashCore(byte[] array, int ibStart, int cbSize)
            {
                for (int i = ibStart; i < ibStart + cbSize; i++) _sum += array[i];
            }

            protected override byte[] HashFinal() => BitConverter.GetBytes(_sum);

            // Do not override HashCore(ReadOnlySpan) and TryHashFinal.  Consuming
            // test verifies that calling the base implementations invokes the array
            // implementations by verifying the right value is produced.
        }

        private class SelfCancelingStream : PositionValueStream
        {
            private readonly CancellationTokenSource _cancellationSource;

            public SelfCancelingStream(int totalCount, CancellationTokenSource cancellationSource)
                : base(totalCount)
            {
                _cancellationSource = cancellationSource;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                _cancellationSource.Cancel(throwOnFirstException: true);
                return base.Read(buffer, offset, count);
            }
        }
    }
}
