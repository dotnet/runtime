// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        public void SetLengthBeyondCapacityThrows()
        {
            byte[] buffer = new byte[8];
            Stream stream = new WritableMemoryStream(new Memory<byte>(buffer));

            Assert.Throws<NotSupportedException>(() => stream.SetLength(9));
            Assert.Equal(8, stream.Length);
        }

        [Fact]
        public void WritePastShrunkenLengthExtendsAndZeroesGap()
        {
            byte[] buffer = { 1, 2, 3, 4, 5, 6, 7, 8 };
            Stream stream = new WritableMemoryStream(new Memory<byte>(buffer));

            stream.SetLength(2);
            Assert.Equal(2, stream.Length);

            stream.Position = 5;
            stream.WriteByte(42);

            Assert.Equal(6, stream.Length);
            Assert.Equal(6, stream.Position);

            stream.Position = 0;
            byte[] readBack = new byte[6];
            Assert.Equal(6, stream.Read(readBack, 0, readBack.Length));
            Assert.Equal(new byte[] { 1, 2, 0, 0, 0, 42 }, readBack);
        }
    }
}

