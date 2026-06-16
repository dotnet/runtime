// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public class WritableMemoryStreamTests
    {
        [Fact]
        public void WriteBeyondCapacityThrows()
        {
            byte[] buffer = new byte[10];
            Stream stream = new WritableMemoryStream(new Memory<byte>(buffer));

            byte[] data = new byte[15];
            Assert.Throws<NotSupportedException>(() => stream.Write(data, 0, data.Length));
        }

        [Fact]
        public void WriteByteBeyondCapacityThrows()
        {
            byte[] buffer = new byte[3];
            Stream stream = new WritableMemoryStream(new Memory<byte>(buffer));

            stream.WriteByte(1);
            stream.WriteByte(2);
            stream.WriteByte(3);

            Assert.Throws<NotSupportedException>(() => stream.WriteByte(4));
        }

        [Fact]
        public void WriteUpToExactCapacitySucceeds()
        {
            byte[] buffer = new byte[10];
            Stream stream = new WritableMemoryStream(new Memory<byte>(buffer));

            byte[] data = new byte[10];
            for (int i = 0; i < data.Length; i++) data[i] = (byte)i;

            stream.Write(data, 0, data.Length);

            Assert.Equal(10, stream.Position);
            Assert.Equal(10, stream.Length);

            stream.Position = 0;
            byte[] readBack = new byte[10];
            int bytesRead = stream.Read(readBack, 0, 10);
            Assert.Equal(10, bytesRead);
            Assert.Equal(data, readBack);
        }

        [Fact]
        public void WritePastCapacityThrowsWithoutSideEffects()
        {
            byte[] buffer = new byte[10];
            Stream stream = new WritableMemoryStream(buffer);

            stream.Write(new byte[8], 0, 8);
            Assert.Equal(8, stream.Position);

            byte[] data = new byte[5];
            Assert.Throws<NotSupportedException>(() => stream.Write(data, 0, 5));

            Assert.Equal(8, stream.Position);
        }

        [Fact]
        public void SeekPastCapacitySucceeds()
        {
            byte[] buffer = new byte[10];
            Stream stream = new WritableMemoryStream(buffer);

            stream.Seek(100, SeekOrigin.Begin);
            Assert.Equal(100, stream.Position);

            Assert.Equal(-1, stream.ReadByte());
            Assert.Throws<NotSupportedException>(() => stream.WriteByte(42));
        }

        [Fact]
        public void WriteOverExistingDataReplacesData()
        {
            byte[] initialData = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            byte[] backing = new byte[10];
            Stream stream = new WritableMemoryStream(new Memory<byte>(backing));
            stream.Write(initialData, 0, initialData.Length);

            stream.Position = 3;
            stream.Write(new byte[] { 100, 101, 102 }, 0, 3);

            stream.Position = 0;
            byte[] result = new byte[10];
            int bytesRead = stream.Read(result, 0, 10);

            Assert.Equal(10, bytesRead);
            Assert.Equal(new byte[] { 1, 2, 3, 100, 101, 102, 7, 8, 9, 10 }, result);
        }

        [Fact]
        public async Task ReadAsyncDifferentResultSizeCreatesNewTask()
        {
            byte[] data = new byte[10];
            for (int i = 0; i < 10; i++) data[i] = (byte)i;
            byte[] backing = new byte[10];
            Stream stream = new WritableMemoryStream(backing);
            stream.Write(data, 0, data.Length);
            stream.Position = 0;

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
            byte[] backing = new byte[5];
            Stream stream = new WritableMemoryStream(backing);
            stream.Write(data, 0, data.Length);
            stream.Position = 0;

            byte[] arrayBuffer = new byte[3];
            Memory<byte> memory = arrayBuffer.AsMemory();
            int bytesRead = await stream.ReadAsync(memory);

            Assert.Equal(3, bytesRead);
            Assert.Equal(new byte[] { 10, 20, 30 }, arrayBuffer);
        }

        [Fact]
        public void WriteToUnmanagedMemory()
        {
            byte[] data = [10, 20, 30, 40, 50];

            using var manager = new NativeMemoryManager(data.Length);
            manager.GetSpan().Clear();

            using var stream = new WritableMemoryStream(manager.Memory);
            stream.Write(data);

            Assert.Equal(data, manager.GetSpan().ToArray());
            Assert.Equal(data.Length, stream.Position);
        }
    }
}
