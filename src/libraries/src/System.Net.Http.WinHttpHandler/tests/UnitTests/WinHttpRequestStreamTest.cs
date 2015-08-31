// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;

using Xunit;

using SafeWinHttpHandle = Interop.WinHttp.SafeWinHttpHandle;

namespace System.Net.Http.WinHttpHandlerUnitTests
{
    public class WinHttpRequestStreamTests
    {
        public WinHttpRequestStreamTests()
        {
            TestControl.ResetAll();
        }

        [Fact]
        public void CanWrite_WhenCreated_ReturnsTrue()
        {
            Stream stream = MakeRequestStream();

            bool result = stream.CanWrite;

            Assert.True(result);
        }

        [Fact]
        public void CanWrite_WhenDisposed_ReturnsFalse()
        {
            Stream stream = MakeRequestStream();
            stream.Dispose();

            bool result = stream.CanWrite;

            Assert.False(result);
        }

        [Fact]
        public void CanSeek_Always_ReturnsFalse()
        {
            Stream stream = MakeRequestStream();

            bool result = stream.CanSeek;

            Assert.False(result);
        }

        [Fact]
        public void CanRead_Always_ReturnsFalse()
        {
            Stream stream = MakeRequestStream();

            bool result = stream.CanRead;

            Assert.False(result);
        }

        [Fact]
        public void Length_WhenCreated_ThrowsNotSupportedException()
        {
            Stream stream = MakeRequestStream();

            Assert.Throws<NotSupportedException>(() => { long result = stream.Length; });
        }

        [Fact]
        public void Length_WhenDisposed_ThrowsObjectDisposedException()
        {
            Stream stream = MakeRequestStream();
            stream.Dispose();

            Assert.Throws<ObjectDisposedException>(() => { long result = stream.Length; });
        }

        [Fact]
        public void Position_WhenCreatedDoGet_ThrowsNotSupportedException()
        {
            Stream stream = MakeRequestStream();

            Assert.Throws<NotSupportedException>(() => { long result = stream.Position; });
        }

        [Fact]
        public void Position_WhenDisposedDoGet_ThrowsObjectDisposedException()
        {
            Stream stream = MakeRequestStream();
            stream.Dispose();

            Assert.Throws<ObjectDisposedException>(() => { long result = stream.Position; });
        }

        [Fact]
        public void Position_WhenCreatedDoSet_ThrowsNotSupportedException()
        {
            Stream stream = MakeRequestStream();

            Assert.Throws<NotSupportedException>(() => { stream.Position = 0; });
        }

        [Fact]
        public void Position_WhenDisposedDoSet_ThrowsObjectDisposedExceptionException()
        {
            Stream stream = MakeRequestStream();
            stream.Dispose();

            Assert.Throws<ObjectDisposedException>(() => { stream.Position = 0; });
        }

        [Fact]
        public void Seek_WhenCreated_ThrowsNotSupportedException()
        {
            Stream stream = MakeRequestStream();

            Assert.Throws<NotSupportedException>(() => { stream.Seek(0, SeekOrigin.Begin); });
        }

        [Fact]
        public void Seek_WhenDisposed_ThrowsObjectDisposedException()
        {
            Stream stream = MakeRequestStream();
            stream.Dispose();

            Assert.Throws<ObjectDisposedException>(() => { stream.Seek(0, SeekOrigin.Begin); });
        }

        [Fact]
        public void SetLength_WhenCreated_ThrowsNotSupportedException()
        {
            Stream stream = MakeRequestStream();

            Assert.Throws<NotSupportedException>(() => { stream.SetLength(0); });
        }

        [Fact]
        public void SetLength_WhenDisposed_ThrowsObjectDisposedException()
        {
            Stream stream = MakeRequestStream();
            stream.Dispose();

            Assert.Throws<ObjectDisposedException>(() => { stream.SetLength(0); });
        }

        [Fact]
        public void Read_WhenCreated_ThrowsNotSupportedException()
        {
            Stream stream = MakeRequestStream();

            Assert.Throws<NotSupportedException>(() => { stream.Read(new byte[1], 0, 1); });
        }

        [Fact]
        public void Read_WhenDisposed_ThrowsObjectDisposedException()
        {
            Stream stream = MakeRequestStream();
            stream.Dispose();

            Assert.Throws<ObjectDisposedException>(() => { stream.Read(new byte[1], 0, 1); });
        }

        [Fact]
        public void Write_BufferIsNull_ThrowsArgumentNullException()
        {
            Stream stream = MakeRequestStream();

            Assert.Throws<ArgumentNullException>(() => { stream.Write(null, 0, 1); });
        }

        [Fact]
        public void Write_OffsetIsNegative_ThrowsArgumentOutOfRangeException()
        {
            Stream stream = MakeRequestStream();

            Assert.Throws<ArgumentOutOfRangeException>(() => { stream.Write(new byte[1], -1, 1); });
        }

        [Fact]
        public void Write_CountIsNegative_ThrowsArgumentOutOfRangeException()
        {
            Stream stream = MakeRequestStream();

            Assert.Throws<ArgumentOutOfRangeException>(() => { stream.Write(new byte[1], 0, -1); });
        }

        [Fact]
        public void Write_OffsetPlusCountExceedsBufferLength_ThrowsArgumentException()
        {
            Stream stream = MakeRequestStream();

            Assert.Throws<ArgumentException>(() => { stream.Write(new byte[1], 0, 3); });
        }

        [Fact]
        public void Write_OffsetPlusCountMaxValueExceedsBufferLength_ThrowsArgumentException()
        {
            Stream stream = MakeRequestStream();

            Assert.Throws<ArgumentException>(() => { stream.Write(new byte[1], int.MaxValue, int.MaxValue); });
        }

        [Fact]
        public void Write_WhenDisposed_ThrowsObjectDisposedException()
        {
            Stream stream = MakeRequestStream();
            stream.Dispose();

            Assert.Throws<ObjectDisposedException>(() => { stream.Write(new byte[1], 0, 1); });
        }

        [Fact]
        public void Write_NetworkFails_ThrowsIOException()
        {
            Stream stream = MakeRequestStream();

            TestControl.Fail.WinHttpWriteData = true;
            
            Assert.Throws<IOException>(() => { stream.Write(new byte[1], 0, 1); });
        }

        [Fact]
        public void Write_NoOffset_AllDataIsWritten()
        {
            Stream stream = MakeRequestStream();

            string data = "Test Data";
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            stream.Write(buffer, 0, buffer.Length);

            byte[] serverBytes = TestServer.RequestBody;
            Assert.True(ByteArraysEqual(buffer, 0, buffer.Length, serverBytes, 0, serverBytes.Length));
        }

        [Fact]
        public void Write_UsingOffset_DataFromOffsetIsWritten()
        {
            Stream stream = MakeRequestStream();

            string data = "Test Data";
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            int offset = 5;
            stream.Write(buffer, offset, buffer.Length - offset);

            byte[] serverBytes = TestServer.RequestBody;
            Assert.True(ByteArraysEqual(buffer, offset, buffer.Length - offset, serverBytes, 0, serverBytes.Length));
        }

        internal Stream MakeRequestStream()
        {
            SafeWinHttpHandle requestHandle = new FakeSafeWinHttpHandle(true);

            return new WinHttpRequestStream(requestHandle, false);
        }

        private bool ByteArraysEqual(byte[] array1, int offset1, int length1, byte[] array2, int offset2, int length2)
        {
            if (length1 != length2)
            {
                return false;
            }

            for (int i = 0; i < length1; i++)
            {
                if (array1[offset1 + i] != array2[offset2 + i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
