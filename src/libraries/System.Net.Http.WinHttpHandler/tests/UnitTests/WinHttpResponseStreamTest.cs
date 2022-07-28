// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Http.WinHttpHandlerUnitTests
{
    public class WinHttpResponseStreamTest
    {
        public WinHttpResponseStreamTest()
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

            Assert.Throws<NotSupportedException>(() => stream.Length);
        }

        [Fact]
        public void Length_WhenDisposed_ThrowsObjectDisposedException()
        {
            Stream stream = MakeResponseStream();
            stream.Dispose();

            Assert.Throws<ObjectDisposedException>(() => stream.Length);
        }

        [Fact]
        public void Position_WhenCreatedDoGet_ThrowsNotSupportedException()
        {
            Stream stream = MakeResponseStream();

            Assert.Throws<NotSupportedException>(() => stream.Position);
        }

        [Fact]
        public void Position_WhenDisposedDoGet_ThrowsObjectDisposedException()
        {
            Stream stream = MakeResponseStream();
            stream.Dispose();

            Assert.Throws<ObjectDisposedException>(() => stream.Position);
        }

        [Fact]
        public void Position_WhenCreatedDoSet_ThrowsNotSupportedException()
        {
            Stream stream = MakeResponseStream();

            Assert.Throws<NotSupportedException>(() => stream.Position = 0);
        }

        [Fact]
        public void Position_WhenDisposedDoSet_ThrowsObjectDisposedExceptionException()
        {
            Stream stream = MakeResponseStream();
            stream.Dispose();

            Assert.Throws<ObjectDisposedException>(() => stream.Position = 0);
        }

        [Fact]
        public void Seek_WhenCreated_ThrowsNotSupportedException()
        {
            Stream stream = MakeResponseStream();

            Assert.Throws<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
        }

        [Fact]
        public void Seek_WhenDisposed_ThrowsObjectDisposedException()
        {
            Stream stream = MakeResponseStream();
            stream.Dispose();

            Assert.Throws<ObjectDisposedException>(() => stream.Seek(0, SeekOrigin.Begin));
        }

        [Fact]
        public void SetLength_WhenCreated_ThrowsNotSupportedException()
        {
            Stream stream = MakeResponseStream();

            Assert.Throws<NotSupportedException>(() => stream.SetLength(0));
        }

        [Fact]
        public void SetLength_WhenDisposed_ThrowsObjectDisposedException()
        {
            Stream stream = MakeResponseStream();
            stream.Dispose();

            Assert.Throws<ObjectDisposedException>(() => stream.SetLength(0));
        }

        [Fact]
        public void Write_WhenCreated_ThrowsNotSupportedException()
        {
            Stream stream = MakeResponseStream();

            Assert.Throws<NotSupportedException>(() => stream.Write(new byte[1], 0, 1));
        }

        [Fact]
        public void Write_WhenDisposed_ThrowsObjectDisposedException()
        {
            Stream stream = MakeResponseStream();
            stream.Dispose();

            Assert.Throws<ObjectDisposedException>(() => stream.Write(new byte[1], 0, 1));
        }

        [Fact]
        public void Read_BufferIsNull_ThrowsArgumentNullException()
        {
            Stream stream = MakeResponseStream();

            Assert.Throws<ArgumentNullException>(() => stream.Read(null, 0, 1));
        }

        [Fact]
        public void Read_OffsetIsNegative_ThrowsArgumentOutOfRangeException()
        {
            Stream stream = MakeResponseStream();

            Assert.Throws<ArgumentOutOfRangeException>(() => stream.Read(new byte[1], -1, 1));
        }

        [Fact]
        public void Read_CountIsNegative_ThrowsArgumentOutOfRangeException()
        {
            Stream stream = MakeResponseStream();

            Assert.Throws<ArgumentOutOfRangeException>(() => stream.Read(new byte[1], 0, -1));
        }

        [Fact]
        public void Read_OffsetPlusCountExceedsBufferLength_ThrowsArgumentException()
        {
            Stream stream = MakeResponseStream();

            AssertExtensions.Throws<ArgumentException>("buffer", () => stream.Read(new byte[1], int.MaxValue, int.MaxValue));
        }

        [Fact]
        public void Read_WhenDisposed_ThrowsObjectDisposedException()
        {
            Stream stream = MakeResponseStream();
            stream.Dispose();

            Assert.Throws<ObjectDisposedException>(() => stream.Read(new byte[1], 0, 1));
        }

        [Fact]
        public void ReadAsync_OffsetIsNegative_ThrowsArgumentOutOfRangeException()
        {
            Stream stream = MakeResponseStream();

            Assert.Throws<ArgumentOutOfRangeException>(() => { Task t = stream.ReadAsync(new byte[1], -1, 1); });
        }

        [Fact]
        public void ReadAsync_QueryDataAvailableFailsWithApiCall_TaskIsFaultedWithIOException()
        {
            Stream stream = MakeResponseStream();

            TestControl.WinHttpQueryDataAvailable.ErrorWithApiCall = true;

            Task t = stream.ReadAsync(new byte[1], 0, 1);
            AggregateException ex = Assert.Throws<AggregateException>(() => t.Wait());
            Assert.IsType<IOException>(ex.InnerException);
        }

        [Fact]
        public void ReadAsync_QueryDataAvailableFailsOnCompletionCallback_TaskIsFaultedWithIOException()
        {
            Stream stream = MakeResponseStream();

            TestControl.WinHttpQueryDataAvailable.ErrorOnCompletion = true;

            Task t = stream.ReadAsync(new byte[1], 0, 1);
            AggregateException ex = Assert.Throws<AggregateException>(() => t.Wait());
            Assert.IsType<IOException>(ex.InnerException);
        }

        [Fact]
        public void ReadAsync_ReadDataFailsWithApiCall_TaskIsFaultedWithIOException()
        {
            Stream stream = MakeResponseStream();

            TestControl.WinHttpReadData.ErrorWithApiCall = true;

            Task t = stream.ReadAsync(new byte[1], 0, 1);
            AggregateException ex = Assert.Throws<AggregateException>(() => t.Wait());
            Assert.IsType<IOException>(ex.InnerException);
        }

        [Fact]
        public void ReadAsync_ReadDataFailsOnCompletionCallback_TaskIsFaultedWithIOException()
        {
            Stream stream = MakeResponseStream();

            TestControl.WinHttpReadData.ErrorOnCompletion = true;

            Task t = stream.ReadAsync(new byte[1], 0, 1);
            AggregateException ex = Assert.Throws<AggregateException>(() => t.Wait());
            Assert.IsType<IOException>(ex.InnerException);
        }

        [Fact]
        public void ReadAsync_TokenIsAlreadyCanceled_TaskIsCanceled()
        {
            Stream stream = MakeResponseStream();

            var cts = new CancellationTokenSource();
            cts.Cancel();
            Task t = stream.ReadAsync(new byte[1], 0, 1, cts.Token);
            Assert.True(t.IsCanceled);
        }

        [Fact]
        public void ReadAsync_WhenDisposed_ThrowsObjectDisposedException()
        {
            Stream stream = MakeResponseStream();
            stream.Dispose();

            Assert.Throws<ObjectDisposedException>(() => { Task t = stream.ReadAsync(new byte[1], 0, 1); });
        }

        [Fact]
        public void ReadAsync_PriorReadInProgress_ThrowsInvalidOperationException()
        {
            Stream stream = MakeResponseStream();

            TestControl.WinHttpReadData.Pause();
            Task t1 = stream.ReadAsync(new byte[1], 0, 1);

            Assert.Throws<InvalidOperationException>(() => { Task t2 = stream.ReadAsync(new byte[1], 0, 1); });

            TestControl.WinHttpReadData.Resume();
            t1.Wait();
        }

        [Fact]
        public void Read_QueryDataAvailableFailsWithApiCall_ThrowsIOException()
        {
            Stream stream = MakeResponseStream();

            TestControl.WinHttpQueryDataAvailable.ErrorWithApiCall = true;
            Assert.Throws<IOException>(() => { stream.Read(new byte[1], 0, 1); });
        }

        [Fact]
        public void Read_QueryDataAvailableFailsOnCompletionCallback_ThrowsIOException()
        {
            Stream stream = MakeResponseStream();

            TestControl.WinHttpQueryDataAvailable.ErrorOnCompletion = true;
            Assert.Throws<IOException>(() => { stream.Read(new byte[1], 0, 1); });
        }

        [Fact]
        public void Read_ReadDataFailsWithApiCall_ThrowsIOException()
        {
            Stream stream = MakeResponseStream();

            TestControl.WinHttpReadData.ErrorWithApiCall = true;
            Assert.Throws<IOException>(() => { stream.Read(new byte[1], 0, 1); });
        }

        [Fact]
        public void Read_ReadDataFailsOnCompletionCallback_ThrowsIOException()
        {
            Stream stream = MakeResponseStream();

            TestControl.WinHttpReadData.ErrorOnCompletion = true;
            Assert.Throws<IOException>(() => { stream.Read(new byte[1], 0, 1); });
        }

        [Fact]
        public void Read_NoOffsetAndNotEndOfData_FillsBuffer()
        {
            Stream stream = MakeResponseStream();
            byte[] testData = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"u8.ToArray();
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
            byte[] testData = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"u8.ToArray();
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
            var state = new WinHttpRequestState();
            state.Pin();
            var handle = new FakeSafeWinHttpHandle(true);
            handle.Callback = WinHttpRequestCallback.StaticCallbackDelegate;
            handle.Context = state.ToIntPtr();
            state.RequestHandle = handle;

            return new WinHttpResponseStream(handle, state, new HttpResponseMessage());
        }
    }
}
