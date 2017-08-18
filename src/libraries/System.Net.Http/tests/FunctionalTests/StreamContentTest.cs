// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public class StreamContentTest
    {
        private readonly ITestOutputHelper _output;
        
        public StreamContentTest(ITestOutputHelper output)
        {
            _output = output;
        }
        
        [Fact]
        public void Ctor_NullStream_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new StreamContent(null));
        }

        [Fact]
        public void Ctor_ZeroBufferSize_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new StreamContent(new MemoryStream(), 0));
        }

        [Fact]
        public void ContentLength_SetStreamSupportingSeeking_StreamLengthMatchesHeaderValue()
        {
            var source = new MockStream(new byte[10], true, true); // Supports seeking.
            var content = new StreamContent(source);

            Assert.Equal(source.Length, content.Headers.ContentLength);;
        }

        [Fact]
        public void ContentLength_SetStreamSupportingSeekingPartiallyConsumed_StreamLengthMatchesHeaderValueMinusConsumed()
        {
            int consumed = 4;
            var source = new MockStream(new byte[10], true, true); // Supports seeking.
            source.Read(new byte[consumed], 0, consumed);
            var content = new StreamContent(source);

            Assert.Equal(source.Length - consumed, content.Headers.ContentLength);
        }

        [Fact]
        public void ContentLength_SetStreamNotSupportingSeeking_NullReturned()
        {
            var source = new MockStream(new byte[10], false, true); // Doesn't support seeking.
            var content = new StreamContent(source);

            Assert.Null(content.Headers.ContentLength);
        }

        [Fact]
        public void Dispose_UseMockStreamSourceAndDisposeContent_MockStreamGotDisposed()
        {
            var source = new MockStream(new byte[10]);
            var content = new StreamContent(source);
            content.Dispose();

            Assert.Equal(1, source.DisposeCount);
        }

        [Fact]
        public void CopyToAsync_NullDestination_ThrowsArgumentnullException()
        {
            var source = new MockStream(new byte[10]);
            var content = new StreamContent(source);
            Assert.Throws<ArgumentNullException>(() => { Task t = content.CopyToAsync(null); });
        }

        [Fact]
        public async Task CopyToAsync_CallMultipleTimesWithStreamSupportingSeeking_ContentIsSerializedMultipleTimes()
        {
            var source = new MockStream(new byte[10], true, true); // Supports seeking.
            var content = new StreamContent(source);

            var destination1 = new MemoryStream();
            await content.CopyToAsync(destination1);
            Assert.Equal(source.Length, destination1.Length);

            var destination2 = new MemoryStream();
            await content.CopyToAsync(destination2);
            Assert.Equal(source.Length, destination2.Length);
        }

        [Fact]
        public async Task CopyToAsync_CallMultipleTimesWithStreamSupportingSeekingPartiallyConsumed_ContentIsSerializedMultipleTimesFromInitialPoint()
        {
            int consumed = 4;
            var source = new MockStream(new byte[10], true, true); // supports seeking.
            source.Read(new byte[consumed], 0, consumed);
            var content = new StreamContent(source);

            var destination1 = new MemoryStream();
            await content.CopyToAsync(destination1);
            Assert.Equal(source.Length - consumed, destination1.Length);

            var destination2 = new MemoryStream();
            await content.CopyToAsync(destination2);
            Assert.Equal(source.Length - consumed, destination2.Length);
        }

        [Fact]
        public async Task CopyToAsync_CallMultipleTimesWithStreamNotSupportingSeeking_ThrowsInvalidOperationException()
        {
            var source = new MockStream(new byte[10], false, true); // doesn't support seeking.
            var content = new StreamContent(source);

            var destination1 = new MemoryStream();
            await content.CopyToAsync(destination1);
            // Use hardcoded expected length, since source.Length would throw (source stream gets disposed if non-seekable).
            Assert.Equal(10, destination1.Length);

            // Note that the InvalidOperationException is thrown in CopyToAsync(). It is not thrown inside the task.
            var destination2 = new MemoryStream();
            Assert.Throws<InvalidOperationException>(() => { Task t = content.CopyToAsync(destination2); });
        }

        [Fact]
        public async Task CopyToAsync_CallMultipleTimesWithStreamNotSupportingSeekingButBufferedStream_ContentSerializedOnceToBuffer()
        {
            var source = new MockStream(new byte[10], false, true); // doesn't support seeking.
            var content = new StreamContent(source);

            // After loading the content into a buffer, we should be able to copy the content to a destination stream
            // multiple times, even though the stream doesn't support seeking.
            await content.LoadIntoBufferAsync();

            var destination1 = new MemoryStream();
            await content.CopyToAsync(destination1);
            // Use hardcoded expected length, since source.Length would throw (source stream gets disposed if non-seekable)
            Assert.Equal(10, destination1.Length);

            var destination2 = new MemoryStream();
            await content.CopyToAsync(destination2);
            Assert.Equal(10, destination2.Length);
        }

        [Fact]
        public async Task CopyToAsync_CallMultipleTimesWithStreamNotSupportingSeekingButBufferedStreamPartiallyConsumed_ContentSerializedOnceToBuffer()
        {
            int consumed = 4;
            var source = new MockStream(new byte[10], false, true); // doesn't support seeking.
            source.Read(new byte[consumed], 0, consumed);
            var content = new StreamContent(source);

            // After loading the content into a buffer, we should be able to copy the content to a destination stream
            // multiple times, even though the stream doesn't support seeking.
            await content.LoadIntoBufferAsync();

            var destination1 = new MemoryStream();
            await content.CopyToAsync(destination1);
            // Use hardcoded expected length, since source.Length would throw (source stream gets disposed if non-seekable).
            Assert.Equal(10 - consumed, destination1.Length);

            var destination2 = new MemoryStream();
            await content.CopyToAsync(destination2);
            Assert.Equal(10 - consumed, destination2.Length);
        }

        [Fact]
        public async Task ContentReadStream_GetProperty_ReturnOriginalStream()
        {
            var source = new MockStream(new byte[10]);
            var content = new StreamContent(source);

            Stream stream = await content.ReadAsStreamAsync();
            Assert.False(stream.CanWrite);
            Assert.Equal(source.Length, stream.Length);
            Assert.Equal(0, source.ReadCount);
            Assert.NotSame(source, stream);
        }

        [Fact]
        public async Task ContentReadStream_GetPropertyPartiallyConsumed_ReturnOriginalStream()
        {
            int consumed = 4;
            var source = new MockStream(new byte[10]);
            source.Read(new byte[consumed], 0, consumed);
            var content = new StreamContent(source);

            Stream stream = await content.ReadAsStreamAsync();
            Assert.False(stream.CanWrite);
            Assert.Equal(source.Length, stream.Length);
            Assert.Equal(1, source.ReadCount);
            Assert.Equal(consumed, stream.Position);
            Assert.NotSame(source, stream);
        }

        [Fact]
        public async Task ContentReadStream_CheckResultProperties_ValuesRepresentReadOnlyStream()
        {
            byte[] data = new byte[10];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (byte)i;
            }

            var source = new MockStream(data);

            var content = new StreamContent(source);
            Stream contentReadStream = await content.ReadAsStreamAsync();

            // The following checks verify that the stream returned passes all read-related properties to the 
            // underlying MockStream and throws when using write-related members.

            Assert.False(contentReadStream.CanWrite);
            Assert.True(contentReadStream.CanRead);
            Assert.Equal(source.Length, contentReadStream.Length);

            Assert.Equal(1, source.CanSeekCount);
            _output.WriteLine(contentReadStream.CanSeek.ToString());
            Assert.Equal(2, source.CanSeekCount);

            contentReadStream.Position = 3; // No exception.
            Assert.Equal(3, contentReadStream.Position);
          
            byte byteOnIndex3 = (byte)contentReadStream.ReadByte();
            Assert.Equal(data[3], byteOnIndex3);

            byte[] byteOnIndex4 = new byte[1];
            int result = await contentReadStream.ReadAsync(byteOnIndex4, 0, 1);
            Assert.Equal(1, result);
                        
            Assert.Equal(data[4], byteOnIndex4[0]);

            byte[] byteOnIndex5 = new byte[1];
            Assert.Equal(1, contentReadStream.Read(byteOnIndex5, 0, 1));
            Assert.Equal(data[5], byteOnIndex5[0]);

            byte[] byteOnIndex6 = new byte[1];
            Assert.Equal(1, contentReadStream.Read(new Span<byte>(byteOnIndex6, 0, 1)));
            Assert.Equal(data[6], byteOnIndex6[0]);

            contentReadStream.ReadTimeout = 123;
            Assert.Equal(123, source.ReadTimeout);
            Assert.Equal(123, contentReadStream.ReadTimeout);

            Assert.Equal(0, source.CanTimeoutCount);
            _output.WriteLine(contentReadStream.CanTimeout.ToString());
            Assert.Equal(1, source.CanTimeoutCount);

            Assert.Equal(0, source.SeekCount);
            contentReadStream.Seek(0, SeekOrigin.Begin);
            Assert.Equal(1, source.SeekCount);

            Assert.Throws<NotSupportedException>(() => { contentReadStream.WriteTimeout = 5; });
            Assert.Throws<NotSupportedException>(() => contentReadStream.WriteTimeout.ToString());
            Assert.Throws<NotSupportedException>(() => contentReadStream.Flush());
            Assert.Throws<NotSupportedException>(() => contentReadStream.SetLength(1));
            Assert.Throws<NotSupportedException>(() => contentReadStream.Write(null, 0, 0));
            Assert.Throws<NotSupportedException>(() => contentReadStream.Write(new Span<byte>(Array.Empty<byte>())));
            Assert.Throws<NotSupportedException>(() => contentReadStream.WriteByte(1));

            Assert.Equal(0, source.DisposeCount);
            contentReadStream.Dispose();
            Assert.Equal(1, source.DisposeCount);
        }

        #region Helper methods

        private class MockStream : MemoryStream
        {
            private bool _canSeek;
            private bool _canRead;
            private int _readTimeout;

            public int DisposeCount { get; private set; }
            public int BufferSize { get; private set; }
            public int ReadCount { get; private set; }
            public int CanSeekCount { get; private set; }
            public int CanTimeoutCount { get; private set; }
            public int SeekCount { get; private set; }

            public override bool CanSeek
            {
                get
                {
                    CanSeekCount++;
                    return _canSeek;
                }
            }

            public override bool CanRead
            {
                get { return _canRead; }
            }

            public override int ReadTimeout
            {
                get { return _readTimeout; }
                set { _readTimeout = value; }
            }

            public override bool CanTimeout
            {
                get
                {
                    CanTimeoutCount++;
                    return base.CanTimeout;
                }
            }

            public MockStream(byte[] data)
                : this(data, true, true)
            {
            }

            public MockStream(byte[] data, bool canSeek, bool canRead)
                : base(data)
            {
                _canSeek = canSeek;
                _canRead = canRead;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                ReadCount++;
                SetBufferSize(count);
                return base.Read(buffer, offset, count);
            }

            public override long Seek(long offset, SeekOrigin loc)
            {
                SeekCount++;
                return base.Seek(offset, loc);
            }

            protected override void Dispose(bool disposing)
            {
                DisposeCount++;
                base.Dispose(disposing);
            }

            private void SetBufferSize(int count)
            {
                if (BufferSize == 0)
                {
                    BufferSize = count;
                }
            }
        }

        #endregion
    }
}
