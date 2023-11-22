// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public class Stream_ReadExactly
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task DelegatesToRead_Success(bool async)
        {
            int readInvokedCount = 0;
            var s = new DelegateStream(
                canReadFunc: () => true,
                readFunc: (array, offset, count) =>
                {
                    readInvokedCount++;

                    int byteCount = Math.Min(count, 10);
                    for (int i = 0; i < byteCount; i++) array[offset + i] = (byte)i;
                    return byteCount;
                });

            byte[] buffer = new byte[30];

            if (async)
            {
                await s.ReadExactlyAsync(buffer);
            }
            else
            {
                s.ReadExactly(buffer);
            }

            Assert.Equal(3, readInvokedCount);
            for (int i = 0; i < 10; i++) Assert.Equal(i, buffer[i]);
            for (int i = 10; i < 20; i++) Assert.Equal(i - 10, buffer[i]);
            for (int i = 20; i < 30; i++) Assert.Equal(i - 20, buffer[i]);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task DelegatesToRead_Success_OffsetCount(bool async)
        {
            int readInvokedCount = 0;
            var s = new DelegateStream(
                canReadFunc: () => true,
                readFunc: (array, offset, count) =>
                {
                    readInvokedCount++;

                    int byteCount = Math.Min(count, 10);
                    for (int i = 0; i < byteCount; i++) array[offset + i] = (byte)i;
                    return byteCount;
                });

            byte[] buffer = new byte[30];

            if (async)
            {
                await s.ReadExactlyAsync(buffer, 0, 10);
            }
            else
            {
                s.ReadExactly(buffer, 0, 10);
            }

            Assert.Equal(1, readInvokedCount);
            for (int i = 0; i < 10; i++) Assert.Equal(i, buffer[i]);
            for (int i = 10; i < 30; i++) Assert.Equal(0, buffer[i]);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReadPartialPageCorrectly(bool async)
        {
            int readInvokedCount = 0;
            var s = new DelegateStream(
                canReadFunc: () => true,
                readFunc: (array, offset, count) =>
                {
                    readInvokedCount++;

                    int byteCount = Math.Min(count, 10);
                    for (int i = 0; i < byteCount; i++) array[offset + i] = (byte)i;
                    return byteCount;
                });

            byte[] buffer = new byte[25];

            if (async)
            {
                await s.ReadExactlyAsync(buffer);
            }
            else
            {
                s.ReadExactly(buffer);
            }

            Assert.Equal(3, readInvokedCount);
            for (int i = 0; i < 10; i++) Assert.Equal(i, buffer[i]);
            for (int i = 10; i < 20; i++) Assert.Equal(i - 10, buffer[i]);
            for (int i = 20; i < 25; i++) Assert.Equal(i - 20, buffer[i]);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReadPartialPageCorrectly_OffsetCount(bool async)
        {
            int readInvokedCount = 0;
            var s = new DelegateStream(
                canReadFunc: () => true,
                readFunc: (array, offset, count) =>
                {
                    readInvokedCount++;

                    int byteCount = Math.Min(count, 10);
                    for (int i = 0; i < byteCount; i++) array[offset + i] = (byte)i;
                    return byteCount;
                });

            byte[] buffer = new byte[25];

            if (async)
            {
                await s.ReadExactlyAsync(buffer, 5, 15);
            }
            else
            {
                s.ReadExactly(buffer, 5, 15);
            }

            Assert.Equal(2, readInvokedCount);
            for (int i = 0; i < 5; i++) Assert.Equal(0, buffer[i]);
            for (int i = 5; i < 15; i++) Assert.Equal(i - 5, buffer[i]);
            for (int i = 15; i < 20; i++) Assert.Equal(i - 15, buffer[i]);
            for (int i = 20; i < 25; i++) Assert.Equal(0, buffer[i]);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReadEmptyBuffer(bool async)
        {
            int readInvokedCount = 0;
            var s = new DelegateStream(
                canReadFunc: () => true,
                readFunc: (array, offset, count) =>
                {
                    readInvokedCount++;

                    int byteCount = Math.Min(count, 10);
                    for (int i = 0; i < byteCount; i++) array[offset + i] = (byte)i;
                    return byteCount;
                });

            byte[] emptyBuffer = Array.Empty<byte>();

            // ReadExactly on an empty buffer is a no-op
            if (async)
            {
                await s.ReadExactlyAsync(emptyBuffer);
                await s.ReadExactlyAsync(emptyBuffer, 0, 0);
            }
            else
            {
                s.ReadExactly(emptyBuffer);
                s.ReadExactly(emptyBuffer, 0, 0);
            }

            Assert.Equal(0, readInvokedCount);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ThrowOnEndOfStream(bool async)
        {
            int readInvokedCount = 0;
            var s = new DelegateStream(
                canReadFunc: () => true,
                readFunc: (array, offset, count) =>
                {
                    readInvokedCount++;

                    if (readInvokedCount == 1)
                    {
                        int byteCount = Math.Min(count, 10);
                        for (int i = 0; i < byteCount; i++) array[offset + i] = (byte)i;
                        return byteCount;
                    }
                    else
                    {
                        return 0;
                    }
                });

            byte[] buffer = new byte[11];
            if (async)
            {
                await Assert.ThrowsAsync<EndOfStreamException>(async () => await s.ReadExactlyAsync(buffer));
            }
            else
            {
                Assert.Throws<EndOfStreamException>(() => s.ReadExactly(buffer));
            }
            Assert.Equal(2, readInvokedCount);

            readInvokedCount = 0;
            if (async)
            {
                await Assert.ThrowsAsync<EndOfStreamException>(async () => await s.ReadExactlyAsync(buffer, 0, buffer.Length));
            }
            else
            {
                Assert.Throws<EndOfStreamException>(() => s.ReadExactly(buffer, 0, buffer.Length));
            }
            Assert.Equal(2, readInvokedCount);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task OffsetCount_ArgumentChecking(bool async)
        {
            int readInvokedCount = 0;
            var s = new DelegateStream(
                canReadFunc: () => true,
                readFunc: (array, offset, count) =>
                {
                    readInvokedCount++;

                    int byteCount = Math.Min(count, 10);
                    for (int i = 0; i < byteCount; i++) array[offset + i] = (byte)i;
                    return byteCount;
                });

            byte[] buffer = new byte[30];

            if (async)
            {
                await Assert.ThrowsAsync<ArgumentNullException>(async () => await s.ReadExactlyAsync(null, 0, 1));
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await s.ReadExactlyAsync(buffer, 0, -1));
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await s.ReadExactlyAsync(buffer, -1, buffer.Length));
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await s.ReadExactlyAsync(buffer, buffer.Length, 1));
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await s.ReadExactlyAsync(buffer, 0, buffer.Length + 1));
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await s.ReadExactlyAsync(buffer, buffer.Length - 1, 2));
            }
            else
            {
                Assert.Throws<ArgumentNullException>(() => s.ReadExactly(null, 0, 1));
                Assert.Throws<ArgumentOutOfRangeException>(() => s.ReadExactly(buffer, 0, -1));
                Assert.Throws<ArgumentOutOfRangeException>(() => s.ReadExactly(buffer, -1, buffer.Length));
                Assert.Throws<ArgumentOutOfRangeException>(() => s.ReadExactly(buffer, buffer.Length, 1));
                Assert.Throws<ArgumentOutOfRangeException>(() => s.ReadExactly(buffer, 0, buffer.Length + 1));
                Assert.Throws<ArgumentOutOfRangeException>(() => s.ReadExactly(buffer, buffer.Length - 1, 2));
            }

            Assert.Equal(0, readInvokedCount);
        }

        [Fact]
        public async Task CancellationTokenIsPassedThrough()
        {
            int readInvokedCount = 0;
            var s = new DelegateStream(
                canReadFunc: () => true,
                readAsyncFunc: (array, offset, count, cancellationToken) =>
                {
                    readInvokedCount++;
                    cancellationToken.ThrowIfCancellationRequested();

                    int byteCount = Math.Min(count, 10);
                    for (int i = 0; i < byteCount; i++) array[offset + i] = (byte)i;
                    return Task.FromResult(10);
                });

            byte[] buffer = new byte[20];

            using CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;
            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(async () => await s.ReadExactlyAsync(buffer, cancellationToken: token));
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await s.ReadExactlyAsync(buffer, 0, buffer.Length, cancellationToken: token));
            Assert.Equal(2, readInvokedCount);
        }
    }
}
