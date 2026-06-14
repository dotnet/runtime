// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    /// <summary>
    /// Additional specific tests for ReadOnlyMemoryStream beyond conformance tests.
    /// </summary>
    public class ReadOnlyMemoryStreamTests
    {
        [Fact]
        public void Constructor_CreatesReadOnlySeekableStream()
        {
            byte[] buffer = new byte[100];
            Stream stream = new ReadOnlyMemoryStream(new ReadOnlyMemory<byte>(buffer));

            Assert.True(stream.CanRead);
            Assert.False(stream.CanWrite);
            Assert.True(stream.CanSeek);
            Assert.Equal(100, stream.Length);
            Assert.Equal(0, stream.Position);
        }

        // Empty ReadOnlyMemory<byte> creates valid zero-length stream.
        [Fact]
        public void Constructor_EmptyMemory_CreatesZeroLengthStream()
        {
            ReadOnlyMemory<byte> emptyMemory = ReadOnlyMemory<byte>.Empty;
            Stream stream = new ReadOnlyMemoryStream(emptyMemory);

            Assert.Equal(0, stream.Length);
            Assert.Equal(0, stream.Position);
            Assert.True(stream.CanRead);
            Assert.False(stream.CanWrite);
        }

        [Fact]
        public void Constructor_FromMemory_WorksCorrectly()
        {
            byte[] buffer = { 1, 2, 3, 4, 5 };
            Memory<byte> memory = buffer;
            Stream stream = new ReadOnlyMemoryStream(memory);  // Implicit conversion

            Assert.Equal(5, stream.Length);
            Assert.True(stream.CanRead);
        }

        // Not covered in conformance tests: ReadOnlyMemory slices stream handling
        [Fact]
        public void Stream_WorksWithSlicedMemory()
        {
            byte[] largeBuffer = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            ReadOnlyMemory<byte> slice = largeBuffer.AsMemory(3, 4);  // [3, 4, 5, 6]
            Stream stream = new ReadOnlyMemoryStream(slice);

            Assert.Equal(4, stream.Length);

            byte[] result = new byte[4];
            int bytesRead = stream.Read(result, 0, 4);

            Assert.Equal(4, bytesRead);
            Assert.Equal(new byte[] { 3, 4, 5, 6 }, result);
        }

        [Fact]
        public void Position_AdvancesDuringRead()
        {
            byte[] buffer = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            Stream stream = new ReadOnlyMemoryStream(buffer);
            byte[] readBuffer = new byte[3];

            Assert.Equal(0, stream.Position);

            stream.Read(readBuffer, 0, 3);
            Assert.Equal(3, stream.Position);

            stream.Read(readBuffer, 0, 3);
            Assert.Equal(6, stream.Position);

            stream.Read(readBuffer, 0, 3);
            Assert.Equal(9, stream.Position);
        }

        [Fact]
        public void Seek_FromCurrent_RelativeOffset()
        {
            Stream stream = new ReadOnlyMemoryStream(new byte[100]);
            stream.Position = 50;

            // Seek forward 10 bytes
            long newPosition = stream.Seek(10, SeekOrigin.Current);
            Assert.Equal(60, newPosition);

            // Seek backward 20 bytes
            newPosition = stream.Seek(-20, SeekOrigin.Current);
            Assert.Equal(40, newPosition);
        }

        [Fact]
        public void Seek_InvalidOrigin_ThrowsArgumentException()
        {
            Stream stream = new ReadOnlyMemoryStream(new byte[100]);

            Assert.Throws<ArgumentException>(() => stream.Seek(0, (SeekOrigin)999));
        }

        [Fact]
        public void Read_ReturnsCorrectData()
        {
            byte[] data = { 10, 20, 30, 40, 50 };
            Stream stream = new ReadOnlyMemoryStream(data);
            byte[] buffer = new byte[3];

            int bytesRead = stream.Read(buffer, 0, 3);

            Assert.Equal(3, bytesRead);
            Assert.Equal(new byte[] { 10, 20, 30 }, buffer);
            Assert.Equal(3, stream.Position);
        }

        [Fact]
        public void Read_LargerThanAvailable_ReturnsPartialData()
        {
            byte[] data = { 1, 2, 3 };
            Stream stream = new ReadOnlyMemoryStream(data);
            byte[] buffer = new byte[10];

            int bytesRead = stream.Read(buffer, 0, 10);

            Assert.Equal(3, bytesRead);
            Assert.Equal(new byte[] { 1, 2, 3 }, buffer[..3]);
        }

        [Fact]
        public void Read_AfterSeek_ReturnsCorrectData()
        {
            byte[] data = { 10, 20, 30, 40, 50 };
            Stream stream = new ReadOnlyMemoryStream(data);

            stream.Seek(2, SeekOrigin.Begin);
            byte[] buffer = new byte[2];
            int bytesRead = stream.Read(buffer, 0, 2);

            Assert.Equal(2, bytesRead);
            Assert.Equal(new byte[] { 30, 40 }, buffer);
        }

        [Fact]
        public void Read_DoesNotModifyUnderlyingMemory()
        {
            byte[] originalData = { 1, 2, 3, 4, 5 };
            byte[] dataCopy = (byte[])originalData.Clone();
            Stream stream = new ReadOnlyMemoryStream(originalData);

            byte[] buffer = new byte[5];
            stream.Read(buffer, 0, 5);

            // Original data should be unchanged
            Assert.Equal(dataCopy, originalData);
        }

        [Fact]
        public void Write_ThrowsNotSupportedException()
        {
            Stream stream = new ReadOnlyMemoryStream(new ReadOnlyMemory<byte>(new byte[10]));
            byte[] data = { 1, 2, 3 };

            Assert.Throws<NotSupportedException>(() => stream.Write(data, 0, 3));
        }

        [Fact]
        public void SetLength_ThrowsNotSupportedException()
        {
            Stream stream = new ReadOnlyMemoryStream(new byte[10]);
            Assert.Throws<NotSupportedException>(() => stream.SetLength(20));
        }

        [Fact]
        public void Dispose_SetsCanPropertiesToFalse()
        {
            Stream stream = new ReadOnlyMemoryStream(new byte[10]);

            stream.Dispose();

            Assert.False(stream.CanRead);
            Assert.False(stream.CanSeek);
            Assert.False(stream.CanWrite);
        }

        [Fact]
        public void Operations_AfterDispose_ThrowObjectDisposedException()
        {
            byte[] buffer = new byte[10];
            Stream stream = new ReadOnlyMemoryStream(buffer);
            stream.Dispose();

            Assert.Throws<ObjectDisposedException>(() => stream.Read(new byte[5], 0, 5));
            Assert.Throws<ObjectDisposedException>(() => stream.ReadByte());
            Assert.Throws<ObjectDisposedException>(() => stream.Seek(0, SeekOrigin.Begin));
            Assert.Throws<ObjectDisposedException>(() => _ = stream.Position);
            Assert.Throws<ObjectDisposedException>(() => stream.Position = 0);
            Assert.Throws<ObjectDisposedException>(() => _ = stream.Length);
        }

        // Standard IDisposable pattern - Dispose() should be idempotent.
        [Fact]
        public void Dispose_MultipleCalls_DoesNotThrow()
        {
            Stream stream = new ReadOnlyMemoryStream(new byte[10]);

            stream.Dispose();
            stream.Dispose();  // Should not throw
            stream.Dispose();  // Should not throw
        }

        [Fact]
        public void Read_NullBuffer_ThrowsArgumentNullException()
        {
            Stream stream = new ReadOnlyMemoryStream(new byte[10]);

            Assert.Throws<ArgumentNullException>(() => stream.Read(null!, 0, 5));
        }

        [Fact]
        public void EmptyBuffer_BehavesCorrectly()
        {
            Stream stream = new ReadOnlyMemoryStream(ReadOnlyMemory<byte>.Empty);

            Assert.Equal(0, stream.Length);
            Assert.Equal(0, stream.Position);

            byte[] buffer = new byte[10];
            Assert.Equal(0, stream.Read(buffer, 0, 10));

            stream.Seek(0, SeekOrigin.Begin);
            Assert.Equal(0, stream.Position);

            // Seeking beyond empty buffer is allowed
            long newPosition = stream.Seek(1, SeekOrigin.Begin);
            Assert.Equal(1, newPosition);
            Assert.Equal(1, stream.Position);
        }

        [Fact]
        public async Task ReadAsync_SameResultSize_ReusesCachedTask()
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
        public async Task ReadAsync_DifferentResultSize_CreatesNewTask()
        {
            byte[] data = new byte[10];
            for (int i = 0; i < 10; i++) data[i] = (byte)i;
            Stream stream = new ReadOnlyMemoryStream(data);

            byte[] buffer1 = new byte[5];
            byte[] buffer2 = new byte[3];
            byte[] buffer3 = new byte[2];

            Task<int> task1 = stream.ReadAsync(buffer1, 0, 5);  // Returns 5
            Task<int> task2 = stream.ReadAsync(buffer2, 0, 3);  // Returns 3
            Task<int> task3 = stream.ReadAsync(buffer3, 0, 2);  // Returns 2

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
            Stream stream = new ReadOnlyMemoryStream(data);

            byte[] arrayBuffer = new byte[3];
            Memory<byte> memory = arrayBuffer.AsMemory();

            int bytesRead = await stream.ReadAsync(memory);

            Assert.Equal(3, bytesRead);
            Assert.Equal(new byte[] { 10, 20, 30 }, arrayBuffer);
        }
    }
}
