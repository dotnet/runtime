// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net.Http;
using System.Text;

using Xunit;

namespace System.Net.Http.WinHttpHandlerUnitTests
{
    public class WinHttpResponseStreamTests
    {
        public WinHttpResponseStreamTests()
        {
            TestControl.ResetAll();
        }

        [Fact]
        public void CanRead_WhenCreated_ReturnsTrue()
        {
            Stream stream = MakeResponseStream();

            bool result = stream.CanRead;

            Assert.True(result);
        }

        [Fact]
        public void CanRead_WhenDisposed_ReturnsFalse()
        {
            Stream stream = MakeResponseStream();
            stream.Dispose();

            bool result = stream.CanRead;

            Assert.False(result);
        }

        [Fact]
        public void CanSeek_Always_ReturnsFalse()
        {
            Stream stream = MakeResponseStream();

            bool result = stream.CanSeek;

            Assert.False(result);
        }

        [Fact]
        public void CanWrite_Always_ReturnsFalse()
        {
            Stream stream = MakeResponseStream();

            bool result = stream.CanWrite;

            Assert.False(result);
        }

        [Fact]
        public void Length_WhenCreated_ThrowsNotSupportedException()
        {
            Stream stream = MakeResponseStream();

            Assert.Throws<NotSupportedException>(() => { long result = stream.Length; });
        }

        [Fact]
        public void Length_WhenDisposed_ThrowsObjectDisposedException()
        {
            Stream stream = MakeResponseStream();
            stream.Dispose();

            Assert.Throws<ObjectDisposedException>(() => { long result = stream.Length; });
        }

        [Fact]
        public void Position_WhenCreatedDoGet_ThrowsNotSupportedException()
        {
            Stream stream = MakeResponseStream();

            Assert.Throws<NotSupportedException>(() => { long result = stream.Position; });
        }

        [Fact]
        public void Position_WhenDisposedDoGet_ThrowsObjectDisposedException()
        {
            Stream stream = MakeResponseStream();
            stream.Dispose();

            Assert.Throws<ObjectDisposedException>(() => { long result = stream.Position; });
        }

        [Fact]
        public void Position_WhenCreatedDoSet_ThrowsNotSupportedException()
        {
            Stream stream = MakeResponseStream();

            Assert.Throws<NotSupportedException>(() => { stream.Position = 0; });
        }

        [Fact]
        public void Position_WhenDisposedDoSet_ThrowsObjectDisposedExceptionException()
        {
            Stream stream = MakeResponseStream();
            stream.Dispose();

            Assert.Throws<ObjectDisposedException>(() => { stream.Position = 0; });
        }

        [Fact]
        public void Seek_WhenCreated_ThrowsNotSupportedException()
        {
            Stream stream = MakeResponseStream();

            Assert.Throws<NotSupportedException>(() => { stream.Seek(0, SeekOrigin.Begin); });
        }

        [Fact]
        public void Seek_WhenDisposed_ThrowsObjectDisposedException()
        {
            Stream stream = MakeResponseStream();
            stream.Dispose();

            Assert.Throws<ObjectDisposedException>(() => { stream.Seek(0, SeekOrigin.Begin); });
        }

        [Fact]
        public void SetLength_WhenCreated_ThrowsNotSupportedException()
        {
            Stream stream = MakeResponseStream();

            Assert.Throws<NotSupportedException>(() => { stream.SetLength(0); });
        }

        [Fact]
        public void SetLength_WhenDisposed_ThrowsObjectDisposedException()
        {
            Stream stream = MakeResponseStream();
            stream.Dispose();

            Assert.Throws<ObjectDisposedException>(() => { stream.SetLength(0); });
        }

        [Fact]
        public void Write_WhenCreated_ThrowsNotSupportedException()
        {
            Stream stream = MakeResponseStream();

            Assert.Throws<NotSupportedException>(() => { stream.Write(new byte[1], 0, 1); });
        }

        [Fact]
        public void Write_WhenDisposed_ThrowsObjectDisposedException()
        {
            Stream stream = MakeResponseStream();
            stream.Dispose();

            Assert.Throws<ObjectDisposedException>(() => { stream.Write(new byte[1], 0, 1); });
        }

        [Fact]
        public void Read_BufferIsNull_ThrowsArgumentNullException()
        {
            Stream stream = MakeResponseStream();

            Assert.Throws<ArgumentNullException>(() => { stream.Read(null, 0, 1); });
        }

        [Fact]
        public void Read_OffsetIsNegative_ThrowsArgumentOutOfRangeException()
        {
            Stream stream = MakeResponseStream();

            Assert.Throws<ArgumentOutOfRangeException>(() => { stream.Read(new byte[1], -1, 1); });
        }

        [Fact]
        public void Read_CountIsNegative_ThrowsArgumentOutOfRangeException()
        {
            Stream stream = MakeResponseStream();

            Assert.Throws<ArgumentOutOfRangeException>(() => { stream.Read(new byte[1], 0, -1); });
        }

        [Fact]
        public void Read_OffsetPlusCountExceedsBufferLength_ThrowsArgumentException()
        {
            Stream stream = MakeResponseStream();

            Assert.Throws<ArgumentException>(() => { stream.Read(new byte[1], int.MaxValue, int.MaxValue); });
        }

        [Fact]
        public void Read_WhenDisposed_ThrowsObjectDisposedException()
        {
            Stream stream = MakeResponseStream();
            stream.Dispose();

            Assert.Throws<ObjectDisposedException>(() => { stream.Read(new byte[1], 0, 1); });
        }

        [Fact]
        public void Read_NetworkFails_ThrowsIOException()
        {
            Stream stream = MakeResponseStream();

            TestControl.Fail.WinHttpReadData = true;
            Assert.Throws<IOException>(() => { stream.Read(new byte[1], 0, 1); });
        }

        [Fact]
        public void Read_NoOffsetAndNotEndOfData_FillsBuffer()
        {
            Stream stream = MakeResponseStream();
            byte[] testData = Encoding.UTF8.GetBytes("ABCDEFGHIJKLMNOPQRSTUVWXYZ");
            TestServer.ResponseBody = testData;

            byte[] buffer = new byte[testData.Length];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);

            Assert.Equal(buffer.Length, bytesRead);
            for (int i = 0; i < buffer.Length; i++)
            {
                Assert.Equal(testData[i], buffer[i]);
            }
        }

        [Fact]
        public void Read_UsingOffsetAndNotEndOfData_FillsBufferFromOffset()
        {
            Stream stream = MakeResponseStream();
            byte[] testData = Encoding.UTF8.GetBytes("ABCDEFGHIJKLMNOPQRSTUVWXYZ");
            TestServer.ResponseBody = testData;

            byte[] buffer = new byte[testData.Length];
            int offset = 5;
            int bytesRead = stream.Read(buffer, offset, buffer.Length - offset);

            Assert.Equal(buffer.Length - offset, bytesRead);
            for (int i = 0; i < offset; i++)
            {
                Assert.Equal(0, buffer[i]);
            }

            for (int i = offset; i < buffer.Length; i++)
            {
                Assert.Equal(testData[i - offset], buffer[i]);
            }
        }

        internal Stream MakeResponseStream()
        {
            var sessionHandle = new FakeSafeWinHttpHandle(true);
            var connectHandle = new FakeSafeWinHttpHandle(true);
            var requestHandle = new FakeSafeWinHttpHandle(true);

            return new WinHttpResponseStream(sessionHandle, connectHandle, requestHandle);
        }
    }
}
