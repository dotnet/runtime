// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public class Stream_ReadAtLeast
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task DelegatesToRead_Success(bool async)
        {
            bool readInvoked = false;
            var s = new DelegateStream(
                canReadFunc: () => true,
                readFunc: (array, offset, count) =>
                {
                    readInvoked = true;
                    Assert.NotNull(array);
                    Assert.Equal(0, offset);
                    Assert.Equal(30, count);

                    for (int i = 0; i < 10; i++) array[offset + i] = (byte)i;
                    return 10;
                });

            byte[] buffer = new byte[30];

            Assert.Equal(10, async ? await s.ReadAtLeastAsync(buffer, 10) : s.ReadAtLeast(buffer, 10));
            Assert.True(readInvoked);
            for (int i = 0; i < 10; i++) Assert.Equal(i, buffer[i]);
            for (int i = 10; i < 30; i++) Assert.Equal(0, buffer[i]);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReadMoreThanOnePage(bool async)
        {
            int readInvokedCount = 0;
            var s = new DelegateStream(
                canReadFunc: () => true,
                readFunc: (array, offset, count) =>
                {
                    readInvokedCount++;

                    for (int i = 0; i < 10; i++) array[offset + i] = (byte)i;
                    return 10;
                });

            byte[] buffer = new byte[30];

            Assert.Equal(20, async ? await s.ReadAtLeastAsync(buffer, 20) : s.ReadAtLeast(buffer, 20));
            Assert.Equal(2, readInvokedCount);
            for (int i = 0; i < 10; i++) Assert.Equal(i, buffer[i]);
            for (int i = 10; i < 20; i++) Assert.Equal(i - 10, buffer[i]);
            for (int i = 20; i < 30; i++) Assert.Equal(0, buffer[i]);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReadMoreThanMinimumBytes(bool async)
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

            // first try with a buffer that doesn't fill 3 full pages
            byte[] buffer = new byte[28];

            Assert.Equal(28, async ? await s.ReadAtLeastAsync(buffer, 22) : s.ReadAtLeast(buffer, 22));
            Assert.Equal(3, readInvokedCount);
            for (int i = 0; i < 10; i++) Assert.Equal(i, buffer[i]);
            for (int i = 10; i < 20; i++) Assert.Equal(i - 10, buffer[i]);
            for (int i = 20; i < 28; i++) Assert.Equal(i - 20, buffer[i]);

            // now try with a buffer that is bigger than 3 pages
            readInvokedCount = 0;
            buffer = new byte[32];

            Assert.Equal(30, async ? await s.ReadAtLeastAsync(buffer, 22) : s.ReadAtLeast(buffer, 22));
            Assert.Equal(3, readInvokedCount);
            for (int i = 0; i < 10; i++) Assert.Equal(i, buffer[i]);
            for (int i = 10; i < 20; i++) Assert.Equal(i - 10, buffer[i]);
            for (int i = 20; i < 30; i++) Assert.Equal(i - 20, buffer[i]);
            for (int i = 30; i < 32; i++) Assert.Equal(0, buffer[i]);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReadAtLeastZero(bool async)
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

            byte[] buffer = new byte[20];

            // ReadAtLeast minimumBytes=0 is a no-op
            Assert.Equal(0, async ? await s.ReadAtLeastAsync(buffer, 0) : s.ReadAtLeast(buffer, 0));
            Assert.Equal(0, readInvokedCount);

            // now try with an empty buffer
            byte[] emptyBuffer = Array.Empty<byte>();

            Assert.Equal(0, async ? await s.ReadAtLeastAsync(emptyBuffer, 0) : s.ReadAtLeast(emptyBuffer, 0));
            Assert.Equal(0, readInvokedCount);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task NegativeMinimumBytes(bool async)
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

            byte[] buffer = new byte[10];
            if (async)
            {
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await s.ReadAtLeastAsync(buffer, -1));
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await s.ReadAtLeastAsync(buffer, -10));
            }
            else
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => s.ReadAtLeast(buffer, -1));
                Assert.Throws<ArgumentOutOfRangeException>(() => s.ReadAtLeast(buffer, -10));
            }
            Assert.Equal(0, readInvokedCount);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task BufferSmallerThanMinimumBytes(bool async)
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

            byte[] buffer = new byte[20];
            byte[] emptyBuffer = Array.Empty<byte>();
            if (async)
            {
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await s.ReadAtLeastAsync(buffer, 21));
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await s.ReadAtLeastAsync(emptyBuffer, 1));
            }
            else
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => s.ReadAtLeast(buffer, 21));
                Assert.Throws<ArgumentOutOfRangeException>(() => s.ReadAtLeast(emptyBuffer, 1));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task HandleEndOfStream(bool async)
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

            byte[] buffer = new byte[20];
            if (async)
            {
                await Assert.ThrowsAsync<EndOfStreamException>(async () => await s.ReadAtLeastAsync(buffer, 11));
            }
            else
            {
                Assert.Throws<EndOfStreamException>(() => s.ReadAtLeast(buffer, 11));
            }
            Assert.Equal(2, readInvokedCount);

            readInvokedCount = 0;

            Assert.Equal(10, async ? await s.ReadAtLeastAsync(buffer, 11, throwOnEndOfStream: false) : s.ReadAtLeast(buffer, 11, throwOnEndOfStream: false));
            for (int i = 0; i < 10; i++) Assert.Equal(i, buffer[i]);
            for (int i = 10; i < 20; i++) Assert.Equal(0, buffer[i]);
            Assert.Equal(2, readInvokedCount);
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

            await Assert.ThrowsAsync<OperationCanceledException>(async () => await s.ReadAtLeastAsync(buffer, 10, cancellationToken: token));
            Assert.Equal(1, readInvokedCount);
        }
    }
}
