// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    /// <summary>
    /// Additional specific tests for WritableMemoryStream beyond conformance tests.
    /// </summary>
    public class WritableMemoryStreamTests
    {
        [Fact]
        public void Constructor_EmptyMemory_CreatesZeroCapacityStream()
        {
            Memory<byte> emptyMemory = Memory<byte>.Empty;
            Stream stream = new WritableMemoryStream(emptyMemory);

            Assert.Equal(0, stream.Length);
            Assert.Equal(0, stream.Position);

            // Cannot write to zero-capacity stream
            Assert.Throws<NotSupportedException>(() => stream.WriteByte(42));
        }

        [Fact]
        public void Write_BeyondCapacity_ThrowsNotSupportedException()
        {
            byte[] buffer = new byte[10];
            Stream stream = new WritableMemoryStream(new Memory<byte>(buffer));

            byte[] data = new byte[15];

            Assert.Throws<NotSupportedException>(() => stream.Write(data, 0, data.Length));
        }

        [Fact]
        public void WriteByte_BeyondCapacity_ThrowsNotSupportedException()
        {
            byte[] buffer = new byte[3];
            Stream stream = new WritableMemoryStream(new Memory<byte>(buffer));

            stream.WriteByte(1);
            stream.WriteByte(2);
            stream.WriteByte(3);

            Assert.Throws<NotSupportedException>(() => stream.WriteByte(4));
        }

        [Fact]
        public void Write_UpToExactCapacity_Succeeds()
        {
            byte[] buffer = new byte[10];
            Stream stream = new WritableMemoryStream(new Memory<byte>(buffer));

            byte[] data = new byte[10];  // Exactly capacity
            for (int i = 0; i < data.Length; i++) data[i] = (byte)i;

            stream.Write(data, 0, data.Length);

            Assert.Equal(10, stream.Position);
            Assert.Equal(10, stream.Length);

            // Verify data was written
            stream.Position = 0;
            byte[] readBack = new byte[10];
            int bytesRead = stream.Read(readBack, 0, 10);
            Assert.Equal(10, bytesRead);
            Assert.Equal(data, readBack);
        }

        [Fact]
        public void Write_PastCapacity_ThrowsWithoutSideEffects()
        {
            byte[] buffer = new byte[10];
            Stream stream = new WritableMemoryStream(buffer);

            stream.Write(new byte[8], 0, 8);  // 8 bytes used, 2 remaining
            Assert.Equal(8, stream.Position);

            // Try to write 5 bytes (only 2 fit)
            byte[] data = new byte[5];
            Assert.Throws<NotSupportedException>(() => stream.Write(data, 0, 5));

            // Position should be unchanged after failed write
            Assert.Equal(8, stream.Position);
        }

        // Seeking beyond capacity is allowed.
        // Write will fail, but seek succeeds.
        [Fact]
        public void Seek_PastCapacity_Succeeds()
        {
            byte[] buffer = new byte[10];
            Stream stream = new WritableMemoryStream(buffer);

            // Seek beyond capacity
            stream.Seek(100, SeekOrigin.Begin);
            Assert.Equal(100, stream.Position);

            Assert.Equal(-1, stream.ReadByte());

            // Write throws (beyond capacity)
            Assert.Throws<NotSupportedException>(() => stream.WriteByte(42));
        }

        [Fact]
        public void Seek_FromEndNegativeOffset_PositionsCorrectly()
        {
            byte[] buffer = new byte[100];
            Stream stream = new WritableMemoryStream(buffer);

            // Seek to 10 bytes before end
            long newPosition = stream.Seek(-10, SeekOrigin.End);

            Assert.Equal(90, newPosition);  // 100 - 10 = 90
            Assert.Equal(90, stream.Position);
        }

        [Fact]
        public void Write_OverExistingData_ReplacesData()
        {
            byte[] buffer = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            Stream stream = new WritableMemoryStream(new Memory<byte>(buffer));

            // Overwrite positions 3-5 with new data
            stream.Position = 3;
            stream.Write(new byte[] { 100, 101, 102 }, 0, 3);

            // Verify overwrite
            stream.Position = 0;
            byte[] result = new byte[10];
            stream.Read(result, 0, 10);

            Assert.Equal(new byte[] { 1, 2, 3, 100, 101, 102, 7, 8, 9, 10 }, result);
        }

        [Fact]
        public void Position_SetToIntMaxValue_Succeeds()
        {
            byte[] buffer = new byte[100];
            Stream stream = new WritableMemoryStream(buffer);

            // WritableMemoryStream allows Position up to int.MaxValue even though it's beyond capacity.
            // Our override permits this — reads return -1, writes throw.
            stream.Position = int.MaxValue;
            Assert.Equal(int.MaxValue, stream.Position);
        }

        [Fact]
        public void Position_SetNegative_ThrowsArgumentOutOfRangeException()
        {
            Stream stream = new WritableMemoryStream(new byte[100]);
            Assert.Throws<ArgumentOutOfRangeException>(() => stream.Position = -1);
        }

        [Fact]
        public void Position_SetBeyondLongMaxValue_ThrowsArgumentOutOfRangeException()
        {
            Stream stream = new WritableMemoryStream(new byte[100]);

            // Position property accepts long, but internally casts to int
            // Setting to value > int.MaxValue should throw
            Assert.Throws<ArgumentOutOfRangeException>(() => stream.Position = (long)int.MaxValue + 1);
        }

        [Fact]
        public void Dispose_SetsCanPropertiesToFalse()
        {
            Stream stream = new WritableMemoryStream(new byte[10]);

            stream.Dispose();

            Assert.False(stream.CanRead);
            Assert.False(stream.CanSeek);
            Assert.False(stream.CanWrite);
        }

        [Fact]
        public void Operations_AfterDispose_ThrowObjectDisposedException()
        {
            byte[] buffer = new byte[10];
            Stream stream = new WritableMemoryStream(buffer);
            stream.Dispose();

            Assert.Throws<ObjectDisposedException>(() => stream.Read(new byte[5], 0, 5));
            Assert.Throws<ObjectDisposedException>(() => stream.Write(new byte[5], 0, 5));
            Assert.Throws<ObjectDisposedException>(() => stream.Seek(0, SeekOrigin.Begin));
            Assert.Throws<ObjectDisposedException>(() => _ = stream.Position);
            Assert.Throws<ObjectDisposedException>(() => stream.Position = 0);
            Assert.Throws<ObjectDisposedException>(() => _ = stream.Length);
        }

        // Zero-byte write doesn't throw and leaves state unchanged.
        [Fact]
        public void Write_ZeroBytes_Succeeds()
        {
            Stream stream = new WritableMemoryStream(new byte[10]);

            stream.Write(new byte[0], 0, 0);

            Assert.Equal(0, stream.Position);
            Assert.Equal(10, stream.Length);  // Length from initial buffer
        }

        [Fact]
        public void Read_ZeroBytes_ReturnsZero()
        {
            Stream stream = new WritableMemoryStream(new byte[10]);

            int bytesRead = stream.Read(new byte[10], 0, 0);

            Assert.Equal(0, bytesRead);
            Assert.Equal(0, stream.Position);
        }

        [Fact]
        public void SetLength_ThrowsNotSupportedException()
        {
            Stream stream = new WritableMemoryStream(new byte[10]);

            Assert.Throws<NotSupportedException>(() => stream.SetLength(20));
        }

        [Fact]
        public async Task ReadAsync_DifferentResultSize_CreatesNewTask()
        {
            byte[] data = new byte[10];
            for (int i = 0; i < 10; i++) data[i] = (byte)i;
            Stream stream = new WritableMemoryStream(data);

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
        public async Task ReadAsync_ArrayBackedMemory_UsesFastPath()
        {
            byte[] data = { 10, 20, 30, 40, 50 };
            Stream stream = new WritableMemoryStream(data);

            byte[] arrayBuffer = new byte[3];
            Memory<byte> memory = arrayBuffer.AsMemory();
            int bytesRead = await stream.ReadAsync(memory);

            Assert.Equal(3, bytesRead);
            Assert.Equal(new byte[] { 10, 20, 30 }, arrayBuffer);
        }
    }
}
