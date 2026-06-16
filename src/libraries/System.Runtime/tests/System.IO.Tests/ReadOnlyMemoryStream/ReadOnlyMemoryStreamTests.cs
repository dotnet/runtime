// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public class ReadOnlyMemoryStreamTests
    {
        [Fact]
        public void ConstructorFromMemoryImplicitConversion()
        {
            byte[] buffer = { 1, 2, 3, 4, 5 };
            Memory<byte> memory = buffer;
            Stream stream = new ReadOnlyMemoryStream(memory);

            Assert.Equal(5, stream.Length);
            Assert.True(stream.CanRead);
        }

        [Fact]
        public void WorksWithSlicedMemory()
        {
            byte[] largeBuffer = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            ReadOnlyMemory<byte> slice = largeBuffer.AsMemory(3, 4);
            Stream stream = new ReadOnlyMemoryStream(slice);

            Assert.Equal(4, stream.Length);

            byte[] result = new byte[4];
            int bytesRead = stream.Read(result, 0, 4);

            Assert.Equal(4, bytesRead);
            Assert.Equal(new byte[] { 3, 4, 5, 6 }, result);
        }

        [Fact]
        public void ReadDoesNotModifyUnderlyingMemory()
        {
            byte[] originalData = { 1, 2, 3, 4, 5 };
            byte[] dataCopy = (byte[])originalData.Clone();
            Stream stream = new ReadOnlyMemoryStream(originalData);

            byte[] buffer = new byte[5];
            stream.Read(buffer, 0, 5);

            Assert.Equal(dataCopy, originalData);
        }

        [Fact]
        public async Task ReadAsyncSameResultSizeReusesCachedTask()
        {
            byte[] data = new byte[20];
            for (int i = 0; i < 20; i++) data[i] = (byte)i;
            Stream stream = new ReadOnlyMemoryStream(data);

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
            Stream stream = new ReadOnlyMemoryStream(data);

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
            byte[] data = { 10, 20, 30, 40, 50 };
            Stream stream = new ReadOnlyMemoryStream(data);

            byte[] arrayBuffer = new byte[3];
            Memory<byte> memory = arrayBuffer.AsMemory();

            int bytesRead = await stream.ReadAsync(memory);

            Assert.Equal(3, bytesRead);
            Assert.Equal(new byte[] { 10, 20, 30 }, arrayBuffer);
        }

        [Fact]
        public void ReadFromUnmanagedMemory()
        {
            byte[] expected = [1, 2, 3, 4, 5];

            using var manager = new NativeMemoryManager(expected.Length);
            expected.CopyTo(manager.GetSpan());

            using var stream = new ReadOnlyMemoryStream(manager.Memory);

            byte[] result = new byte[expected.Length];
            int bytesRead = stream.Read(result);

            Assert.Equal(expected.Length, bytesRead);
            Assert.Equal(expected, result);
        }
    }
}
