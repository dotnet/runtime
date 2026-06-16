// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Buffers;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.Memory.Tests
{
    public class ReadOnlySequenceStreamTests
    {
        [Fact]
        public void ReadZeroBytesReturnsZero()
        {
            byte[] data = [1, 2, 3];
            var stream = new ReadOnlySequenceStream(new ReadOnlySequence<byte>(data));
            byte[] buffer = new byte[10];

            int bytesRead = stream.Read(buffer, 0, 0);

            Assert.Equal(0, bytesRead);
            Assert.Equal(0, stream.Position);
        }

        [Fact]
        public void SeekingBeyondEmptyBufferIsAllowed()
        {
            var stream = new ReadOnlySequenceStream(ReadOnlySequence<byte>.Empty);

            Assert.Equal(0, stream.Length);
            Assert.Equal(0, stream.Position);

            byte[] buffer = new byte[10];
            int bytesRead = stream.Read(buffer, 0, 10);
            Assert.Equal(0, bytesRead);

            stream.Seek(0, SeekOrigin.Begin);
            Assert.Equal(0, stream.Position);

            long newPosition = stream.Seek(1, SeekOrigin.Begin);
            Assert.Equal(1, newPosition);
            Assert.Equal(1, stream.Position);
        }

        [Fact]
        public async Task ReadAsyncSameResultSizeReusesCachedTask()
        {
            byte[] data = new byte[20];
            for (int i = 0; i < 20; i++) data[i] = (byte)i;
            var stream = new ReadOnlySequenceStream(new ReadOnlySequence<byte>(data));

            byte[] buffer1 = new byte[5];
            byte[] buffer2 = new byte[5];
            byte[] buffer3 = new byte[5];

            Task<int> task1 = stream.ReadAsync(buffer1, 0, 5);
            Task<int> task2 = stream.ReadAsync(buffer2, 0, 5);
            Task<int> task3 = stream.ReadAsync(buffer3, 0, 5);

            await task1;
            await task2;
            await task3;

            Assert.Same(task1, task2);
            Assert.Same(task2, task3);

            Assert.Equal(new byte[] { 0, 1, 2, 3, 4 }, buffer1);
            Assert.Equal(new byte[] { 5, 6, 7, 8, 9 }, buffer2);
            Assert.Equal(new byte[] { 10, 11, 12, 13, 14 }, buffer3);
        }

        [Fact]
        public async Task ReadAsyncDifferentResultSizeCreatesNewTask()
        {
            byte[] data = new byte[10];
            for (int i = 0; i < 10; i++) data[i] = (byte)i;
            var stream = new ReadOnlySequenceStream(new ReadOnlySequence<byte>(data));

            byte[] buffer1 = new byte[5];
            byte[] buffer2 = new byte[3];
            byte[] buffer3 = new byte[2];

            Task<int> task1 = stream.ReadAsync(buffer1, 0, 5);
            Task<int> task2 = stream.ReadAsync(buffer2, 0, 3);
            Task<int> task3 = stream.ReadAsync(buffer3, 0, 2);

            await task1;
            await task2;
            await task3;

            Assert.NotSame(task1, task2);
            Assert.NotSame(task2, task3);
        }

        [Fact]
        public async Task ReadAsyncArrayBackedMemoryUsesFastPath()
        {
            byte[] data = [10, 20, 30, 40, 50];
            var stream = new ReadOnlySequenceStream(new ReadOnlySequence<byte>(data));

            byte[] arrayBuffer = new byte[3];
            Memory<byte> memory = arrayBuffer.AsMemory();
            int bytesRead = await stream.ReadAsync(memory);

            Assert.Equal(3, bytesRead);
            Assert.Equal(new byte[] { 10, 20, 30 }, arrayBuffer);
        }
    }
}
