// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using SafeWinHttpHandle = Interop.WinHttp.SafeWinHttpHandle;

namespace System.Net.Http.WinHttpHandlerUnitTests
{
    public class WinHttpRequestStreamTest
    {
        public WinHttpRequestStreamTest()
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

            Assert.Throws<NotSupportedException>(() => stream.Length);
        }

        [Fact]
        public void Length_WhenDisposed_ThrowsObjectDisposedException()
        {
            Stream stream = MakeRequestStream();
            stream.Dispose();

            Assert.Throws<ObjectDisposedException>(() => stream.Length);
        }

        [Fact]
        public void Position_WhenCreatedDoGet_ThrowsNotSupportedException()
        {
            Stream stream = MakeRequestStream();

            Assert.Throws<NotSupportedException>(() => stream.Position);
        }

        [Fact]
        public void Position_WhenDisposedDoGet_ThrowsObjectDisposedException()
        {
            Stream stream = MakeRequestStream();
            stream.Dispose();

            Assert.Throws<ObjectDisposedException>(() => stream.Position);
        }

        [Fact]
        public void Position_WhenCreatedDoSet_ThrowsNotSupportedException()
        {
            Stream stream = MakeRequestStream();

            Assert.Throws<NotSupportedException>(() => stream.Position = 0);
        }

        [Fact]
        public void Position_WhenDisposedDoSet_ThrowsObjectDisposedExceptionException()
        {
            Stream stream = MakeRequestStream();
            stream.Dispose();

            Assert.Throws<ObjectDisposedException>(() => stream.Position = 0);
        }

        [Fact]
        public void Seek_WhenCreated_ThrowsNotSupportedException()
        {
            Stream stream = MakeRequestStream();

            Assert.Throws<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
        }

        [Fact]
        public void Seek_WhenDisposed_ThrowsObjectDisposedException()
        {
            Stream stream = MakeRequestStream();
            stream.Dispose();

            Assert.Throws<ObjectDisposedException>(() => stream.Seek(0, SeekOrigin.Begin));
        }

        [Fact]
        public void SetLength_WhenCreated_ThrowsNotSupportedException()
        {
            Stream stream = MakeRequestStream();

            Assert.Throws<NotSupportedException>(() => stream.SetLength(0));
        }

        [Fact]
        public void SetLength_WhenDisposed_ThrowsObjectDisposedException()
        {
            Stream stream = MakeRequestStream();
            stream.Dispose();

            Assert.Throws<ObjectDisposedException>(() => stream.SetLength(0));
        }

        [Fact]
        public void Read_WhenCreated_ThrowsNotSupportedException()
        {
            Stream stream = MakeRequestStream();

            Assert.Throws<NotSupportedException>(() => stream.Read(new byte[1], 0, 1));
        }

        [Fact]
        public void Read_WhenDisposed_ThrowsObjectDisposedException()
        {
            Stream stream = MakeRequestStream();
            stream.Dispose();

            Assert.Throws<ObjectDisposedException>(() => stream.Read(new byte[1], 0, 1));
        }

        [Fact]
        public void Write_BufferIsNull_ThrowsArgumentNullException()
        {
            Stream stream = MakeRequestStream();

            Assert.Throws<ArgumentNullException>(() => stream.Write(null, 0, 1));
        }

        [Fact]
        public void Write_OffsetIsNegative_ThrowsArgumentOutOfRangeException()
        {
            Stream stream = MakeRequestStream();

            Assert.Throws<ArgumentOutOfRangeException>(() => stream.Write(new byte[1], -1, 1));
        }

        [Fact]
        public void Write_CountIsNegative_ThrowsArgumentOutOfRangeException()
        {
            Stream stream = MakeRequestStream();

            Assert.Throws<ArgumentOutOfRangeException>(() => stream.Write(new byte[1], 0, -1));
        }

        [Fact]
        public void Write_OffsetPlusCountExceedsBufferLength_ThrowsArgumentException()
        {
            Stream stream = MakeRequestStream();

            AssertExtensions.Throws<ArgumentException>("buffer", () => stream.Write(new byte[1], 0, 3));
        }

        [Fact]
        public void Write_OffsetPlusCountMaxValueExceedsBufferLength_ThrowsArgumentException()
        {
            Stream stream = MakeRequestStream();

            AssertExtensions.Throws<ArgumentException>("buffer", () => stream.Write(new byte[1], int.MaxValue, int.MaxValue));
        }

        [Fact]
        public void Write_WhenDisposed_ThrowsObjectDisposedException()
        {
            Stream stream = MakeRequestStream();
            stream.Dispose();

            Assert.Throws<ObjectDisposedException>(() => stream.Write(new byte[1], 0, 1));
        }

        [Fact]
        public void Write_NetworkFails_ThrowsIOException()
        {
            Stream stream = MakeRequestStream();

            TestControl.WinHttpWriteData.ErrorOnCompletion = true;

            Assert.Throws<IOException>(() => stream.Write(new byte[1], 0, 1));
        }

        [Fact]
        public void Write_NoOffset_AllDataIsWritten()
        {
            Stream stream = MakeRequestStream();

            string data = "Test Data";
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            stream.Write(buffer, 0, buffer.Length);

            byte[] serverBytes = TestServer.RequestBody;
            Assert.Equal(buffer, serverBytes);
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
            Assert.Equal(
                new ArraySegment<byte>(buffer, offset, buffer.Length - offset),
                new ArraySegment<byte>(serverBytes, 0, serverBytes.Length));
        }

        [Fact]
        public void WriteAsync_OffsetIsNegative_ThrowsArgumentOutOfRangeException()
        {
            Stream stream = MakeRequestStream();

            Assert.Throws<ArgumentOutOfRangeException>(() => { Task t = stream.WriteAsync(new byte[1], -1, 1); });
        }

        [Fact]
        public async Task WriteAsync_NetworkFails_TaskIsFaultedWithIOException()
        {
            Stream stream = MakeRequestStream();

            TestControl.WinHttpWriteData.ErrorOnCompletion = true;

            await Assert.ThrowsAsync<IOException>(() => stream.WriteAsync(new byte[1], 0, 1));
        }

        [Fact]
        public void WriteAsync_TokenIsAlreadyCanceled_TaskIsCanceled()
        {
            Stream stream = MakeRequestStream();

            var cts = new CancellationTokenSource();
            cts.Cancel();
            Task t = stream.WriteAsync(new byte[1], 0, 1, cts.Token);
            Assert.True(t.IsCanceled);
        }

        [Fact]
        public void WtiteAsync_WhenDisposed_ThrowsObjectDisposedException()
        {
            Stream stream = MakeRequestStream();
            stream.Dispose();

            Assert.Throws<ObjectDisposedException>(() => { Task t = stream.WriteAsync(new byte[1], 0, 1); });
        }

        [Fact]
        public void WriteAsync_PriorWriteInProgress_ThrowsInvalidOperationException()
        {
            Stream stream = MakeRequestStream();

            TestControl.WinHttpWriteData.Pause();
            Task t1 = stream.WriteAsync(new byte[1], 0, 1);

            Assert.Throws<InvalidOperationException>(() => { Task t2 = stream.WriteAsync(new byte[1], 0, 1); });

            TestControl.WinHttpWriteData.Resume();
            t1.Wait();
        }

        internal Stream MakeRequestStream()
        {
            var state = new WinHttpRequestState();
            state.Pin();
            var handle = new FakeSafeWinHttpHandle(true);
            handle.Callback = WinHttpRequestCallback.StaticCallbackDelegate;
            handle.Context = state.ToIntPtr();
            state.RequestHandle = handle;

            return new WinHttpRequestStream(state, WinHttpChunkMode.None);
        }
    }
}
