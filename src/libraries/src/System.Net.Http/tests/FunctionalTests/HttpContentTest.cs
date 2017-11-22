// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public class HttpContentTest
    {
        private readonly ITestOutputHelper _output;

        public HttpContentTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task CopyToAsync_CallWithMockContent_MockContentMethodCalled()
        {
            var content = new MockContent(MockOptions.CanCalculateLength);
            var m = new MemoryStream();

            await content.CopyToAsync(m);

            Assert.Equal(1, content.SerializeToStreamAsyncCount);
            Assert.Equal(content.GetMockData(), m.ToArray());
        }

        [Fact]
        public async Task CopyToAsync_ThrowCustomExceptionInOverriddenMethod_ThrowsMockException()
        {
            var content = new MockContent(new MockException(), MockOptions.ThrowInSerializeMethods);

            Task t = content.CopyToAsync(new MemoryStream());
            await Assert.ThrowsAsync<MockException>(() => t);
        }

        [Fact]
        public async Task CopyToAsync_ThrowObjectDisposedExceptionInOverriddenMethod_ThrowsWrappedHttpRequestException()
        {
            var content = new MockContent(new ObjectDisposedException(""), MockOptions.ThrowInSerializeMethods);

            Task t = content.CopyToAsync(new MemoryStream());
            HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => t);
            Assert.IsType<ObjectDisposedException>(ex.InnerException);
        }

        [Fact]
        public async Task CopyToAsync_ThrowIOExceptionInOverriddenMethod_ThrowsWrappedHttpRequestException()
        {
            var content = new MockContent(new IOException(), MockOptions.ThrowInSerializeMethods);

            Task t = content.CopyToAsync(new MemoryStream());
            HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => t);
            Assert.IsType<IOException>(ex.InnerException);
        }

        [Fact]
        public void CopyToAsync_ThrowCustomExceptionInOverriddenAsyncMethod_ExceptionBubblesUp()
        {
            var content = new MockContent(new MockException(), MockOptions.ThrowInAsyncSerializeMethods);

            var m = new MemoryStream();
            Assert.Throws<MockException>(() => { content.CopyToAsync(m); });
        }

        [Fact]
        public async Task CopyToAsync_ThrowObjectDisposedExceptionInOverriddenAsyncMethod_ThrowsWrappedHttpRequestException()
        {
            var content = new MockContent(new ObjectDisposedException(""), MockOptions.ThrowInAsyncSerializeMethods);

            Task t = content.CopyToAsync(new MemoryStream());
            HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => t);
            Assert.IsType<ObjectDisposedException>(ex.InnerException);
        }

        [Fact]
        public async Task CopyToAsync_ThrowIOExceptionInOverriddenAsyncMethod_ThrowsWrappedHttpRequestException()
        {
            var content = new MockContent(new IOException(), MockOptions.ThrowInAsyncSerializeMethods);

            Task t = content.CopyToAsync(new MemoryStream());
            HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => t);
            Assert.IsType<IOException>(ex.InnerException);
        }

        [Fact]
        public void CopyToAsync_MockContentReturnsNull_ThrowsInvalidOperationException()
        {
            // return 'null' when CopyToAsync() is called.
            var content = new MockContent(MockOptions.ReturnNullInCopyToAsync); 
            var m = new MemoryStream();
            
            // The HttpContent derived class (MockContent in our case) must return a Task object when WriteToAsync()
            // is called. If not, HttpContent will throw.
            Assert.Throws<InvalidOperationException>(() => { content.CopyToAsync(m); });
        }

        [Fact]
        public async Task CopyToAsync_BufferContentFirst_UseBufferedStreamAsSource()
        {
            var data = new byte[10];
            var content = new MockContent(data);
            await content.LoadIntoBufferAsync();

            Assert.Equal(1, content.SerializeToStreamAsyncCount);
            var destination = new MemoryStream();
            await content.CopyToAsync(destination);

            // Our MockContent should not be called for the CopyTo() operation since the buffered stream should be 
            // used.
            Assert.Equal(1, content.SerializeToStreamAsyncCount);
            Assert.Equal(data.Length, destination.Length);
        }

        [Fact]
        public void TryComputeLength_RetrieveContentLength_ComputeLengthShouldBeCalled()
        {
            var content = new MockContent(MockOptions.CanCalculateLength);

            Assert.Equal(content.GetMockData().Length, content.Headers.ContentLength);
            Assert.Equal(1, content.TryComputeLengthCount);
        }

        [Fact]
        public async Task TryComputeLength_RetrieveContentLengthFromBufferedContent_ComputeLengthIsNotCalled()
        {
            var content = new MockContent();
            await content.LoadIntoBufferAsync();

            Assert.Equal(content.GetMockData().Length, content.Headers.ContentLength);
            
            // Called once to determine the size of the buffer.
            Assert.Equal(1, content.TryComputeLengthCount); 
        }

        [Fact]
        public void TryComputeLength_ThrowCustomExceptionInOverriddenMethod_ExceptionBubblesUpToCaller()
        {
            var content = new MockContent(MockOptions.ThrowInTryComputeLength); 

            var m = new MemoryStream();
            Assert.Throws<MockException>(() => content.Headers.ContentLength);
        }

        [Fact]
        public async Task ReadAsStreamAsync_GetFromUnbufferedContent_CreateContentReadStreamCalledOnce()
        {
            var content = new MockContent(MockOptions.CanCalculateLength);

            // Call multiple times: CreateContentReadStreamAsync() should be called only once.
            Stream stream = await content.ReadAsStreamAsync();
            stream = await content.ReadAsStreamAsync();
            stream = await content.ReadAsStreamAsync();

            Assert.Equal(1, content.CreateContentReadStreamCount);
            Assert.Equal(content.GetMockData().Length, stream.Length);
            Stream stream2 = await content.ReadAsStreamAsync();
            Assert.Same(stream, stream2);
        }

        [Fact]
        public async Task ReadAsStreamAsync_GetFromBufferedContent_CreateContentReadStreamCalled()
        {
            var content = new MockContent(MockOptions.CanCalculateLength);
            await content.LoadIntoBufferAsync();

            Stream stream = await content.ReadAsStreamAsync();

            Assert.Equal(0, content.CreateContentReadStreamCount);
            Assert.Equal(content.GetMockData().Length, stream.Length);
            Stream stream2 = await content.ReadAsStreamAsync();
            Assert.Same(stream, stream2);
            Assert.Equal(0, stream.Position);
            Assert.Equal((byte)'d', stream.ReadByte());
        }

        [Fact]
        public async Task ReadAsStreamAsync_FirstGetFromUnbufferedContentThenGetFromBufferedContent_SameStream()
        {
            var content = new MockContent(MockOptions.CanCalculateLength);

            Stream before = await content.ReadAsStreamAsync();
            Assert.Equal(1, content.CreateContentReadStreamCount);

            await content.LoadIntoBufferAsync();

            Stream after = await content.ReadAsStreamAsync();
            Assert.Equal(1, content.CreateContentReadStreamCount);

            // Note that ContentReadStream returns always the same stream. If the user gets the stream, buffers content,
            // and gets the stream again, the same instance is returned. Returning a different instance could be 
            // confusing, even though there shouldn't be any real world scenario for retrieving the read stream both
            // before and after buffering content.
            Assert.Equal(before, after);
        }

        [Fact]
        public async Task ReadAsStreamAsync_UseBaseImplementation_ContentGetsBufferedThenMemoryStreamReturned()
        {
            var content = new MockContent(MockOptions.DontOverrideCreateContentReadStream);
            Stream stream = await content.ReadAsStreamAsync();

            Assert.NotNull(stream);
            Assert.Equal(1, content.SerializeToStreamAsyncCount);
            Stream stream2 = await content.ReadAsStreamAsync();
            Assert.Same(stream, stream2);
            Assert.Equal(0, stream.Position);
            Assert.Equal((byte)'d', stream.ReadByte());
        }

        [Fact]
        public async Task LoadIntoBufferAsync_BufferSizeSmallerThanContentSizeWithCalculatedContentLength_ThrowsHttpRequestException()
        {
            var content = new MockContent(MockOptions.CanCalculateLength);
            Task t = content.LoadIntoBufferAsync(content.GetMockData().Length - 1);
            await Assert.ThrowsAsync<HttpRequestException>(() => t);
        }

        [Fact]
        public async Task LoadIntoBufferAsync_BufferSizeSmallerThanContentSizeWithNullContentLength_ThrowsHttpRequestException()
        {
            var content = new MockContent();
            Task t = content.LoadIntoBufferAsync(content.GetMockData().Length - 1);
            await Assert.ThrowsAsync<HttpRequestException>(() => t);
        }

        [Fact]
        public async Task LoadIntoBufferAsync_CallOnMockContentWithCalculatedContentLength_CopyToAsyncMemoryStreamCalled()
        {
            var content = new MockContent(MockOptions.CanCalculateLength);
            Assert.NotNull(content.Headers.ContentLength);
            await content.LoadIntoBufferAsync();

            Assert.Equal(1, content.SerializeToStreamAsyncCount);
            Stream stream = await content.ReadAsStreamAsync();
            Assert.False(stream.CanWrite);
        }

        [Fact]
        public async Task LoadIntoBufferAsync_CallOnMockContentWithNullContentLength_CopyToAsyncMemoryStreamCalled()
        {
            var content = new MockContent();
            Assert.Null(content.Headers.ContentLength);
            await content.LoadIntoBufferAsync();
            Assert.NotNull(content.Headers.ContentLength);
            Assert.Equal(content.MockData.Length, content.Headers.ContentLength);

            Assert.Equal(1, content.SerializeToStreamAsyncCount);
            Stream stream = await content.ReadAsStreamAsync();
            Assert.False(stream.CanWrite);
        }

        [Fact]
        public async Task LoadIntoBufferAsync_CallOnMockContentWithLessLengthThanContentLengthHeader_BufferedStreamLengthMatchesActualLengthNotContentLengthHeaderValue()
        {
            byte[] data = Encoding.UTF8.GetBytes("16 bytes of data");
            var content = new MockContent(data);
            content.Headers.ContentLength = 32; // Set the Content-Length header to a value > actual data length.
            Assert.Equal(32, content.Headers.ContentLength);

            await content.LoadIntoBufferAsync();

            Assert.Equal(1, content.SerializeToStreamAsyncCount);
            Assert.NotNull(content.Headers.ContentLength);
            Assert.Equal(32, content.Headers.ContentLength);
            Stream stream = await content.ReadAsStreamAsync();
            Assert.Equal(data.Length, stream.Length);
        }

        [Fact]
        public async Task LoadIntoBufferAsync_CallMultipleTimesWithCalculatedContentLength_CopyToAsyncMemoryStreamCalledOnce()
        {
            var content = new MockContent(MockOptions.CanCalculateLength);
            await content.LoadIntoBufferAsync();
            await content.LoadIntoBufferAsync();

            Assert.Equal(1, content.SerializeToStreamAsyncCount);
            Stream stream = await content.ReadAsStreamAsync();
            Assert.False(stream.CanWrite);
        }

        [Fact]
        public async Task LoadIntoBufferAsync_CallMultipleTimesWithNullContentLength_CopyToAsyncMemoryStreamCalledOnce()
        {
            var content = new MockContent();
            await content.LoadIntoBufferAsync();
            await content.LoadIntoBufferAsync();

            Assert.Equal(1, content.SerializeToStreamAsyncCount);
            Stream stream = await content.ReadAsStreamAsync();
            Assert.False(stream.CanWrite);
        }

        [Fact]
        public async Task LoadIntoBufferAsync_ThrowCustomExceptionInOverriddenMethod_ThrowsMockException()
        {
            var content = new MockContent(new MockException(), MockOptions.ThrowInSerializeMethods);

            Task t = content.LoadIntoBufferAsync();
            await Assert.ThrowsAsync<MockException>(() => t);
        }

        [Fact]
        public async Task LoadIntoBufferAsync_ThrowObjectDisposedExceptionInOverriddenMethod_ThrowsWrappedHttpRequestException()
        {
            var content = new MockContent(new ObjectDisposedException(""), MockOptions.ThrowInSerializeMethods);

            Task t = content.LoadIntoBufferAsync();
            HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => t);
            Assert.IsType<ObjectDisposedException>(ex.InnerException);
        }

        [Fact]
        public async Task LoadIntoBufferAsync_ThrowIOExceptionInOverriddenMethod_ThrowsWrappedHttpRequestException()
        {
            MockContent content = new MockContent(new IOException(), MockOptions.ThrowInSerializeMethods);

            Task t = content.LoadIntoBufferAsync();
            HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => t);
            Assert.IsType<IOException>(ex.InnerException);
        }

        [Fact]
        public void LoadIntoBufferAsync_ThrowCustomExceptionInOverriddenAsyncMethod_ExceptionBubblesUpToCaller()
        {
            var content = new MockContent(new MockException(), MockOptions.ThrowInAsyncSerializeMethods);

            Assert.Throws<MockException>(() => { content.LoadIntoBufferAsync(); });
        }

        [Fact]
        public async Task LoadIntoBufferAsync_ThrowObjectDisposedExceptionInOverriddenAsyncMethod_ThrowsHttpRequestException()
        {
            var content = new MockContent(new ObjectDisposedException(""), MockOptions.ThrowInAsyncSerializeMethods);

            Task t = content.LoadIntoBufferAsync();
            HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => t);
            Assert.IsType<ObjectDisposedException>(ex.InnerException);
        }

        [Fact]
        public async Task LoadIntoBufferAsync_ThrowIOExceptionInOverriddenAsyncMethod_ThrowsHttpRequestException()
        {
            var content = new MockContent(new IOException(), MockOptions.ThrowInAsyncSerializeMethods);

            Task t = content.LoadIntoBufferAsync();
            HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => t);
            Assert.IsType<IOException>(ex.InnerException);
        }

        [Fact]
        public async Task Dispose_GetReadStreamThenDispose_ReadStreamGetsDisposed()
        {
            var content = new MockContent();
            MockMemoryStream s = (MockMemoryStream) await content.ReadAsStreamAsync();
            Assert.Equal(1, content.CreateContentReadStreamCount);

            Assert.Equal(0, s.DisposeCount);
            content.Dispose();
            Assert.Equal(1, s.DisposeCount);
        }

        [Fact]
        public void Dispose_DisposeContentThenAccessContentLength_Throw()
        {
            var content = new MockContent();

            // This is not really typical usage of the type, but let's make sure we consider also this case: The user
            // keeps a reference to the Headers property before disposing the content. Then after disposing, the user
            // accesses the ContentLength property.
            var headers = content.Headers;
            content.Dispose();
            Assert.Throws<ObjectDisposedException>(() => headers.ContentLength.ToString());
        }

        [Fact]
        public async Task CopyToAsync_UseStreamWriteByteWithBufferSizeSmallerThanContentSize_ThrowsHttpRequestException()
        {
            // MockContent uses stream.WriteByte() rather than stream.Write(): Verify that the max. buffer size
            // is also checked when using WriteByte().
            var content = new MockContent(MockOptions.UseWriteByteInCopyTo);
            Task t = content.LoadIntoBufferAsync(content.GetMockData().Length - 1);
            await Assert.ThrowsAsync<HttpRequestException>(() => t);
        }
        
        [Fact]
        public async Task ReadAsStringAsync_EmptyContent_EmptyString()
        {
            var content = new MockContent(new byte[0]);
            string actualContent = await content.ReadAsStringAsync();
            Assert.Equal(string.Empty, actualContent);
        }

        [Fact]
        public async Task ReadAsStringAsync_SetInvalidCharset_ThrowsInvalidOperationException()
        {
            string sourceString = "some string";
            byte[] contentBytes = Encoding.UTF8.GetBytes(sourceString);

            var content = new MockContent(contentBytes);
            content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
            content.Headers.ContentType.CharSet = "invalid";

            // This will throw because we have an invalid charset.
            Task t = content.ReadAsStringAsync();
            await Assert.ThrowsAsync<InvalidOperationException>(() => t);
        }

        [Fact]
        public async Task ReadAsStringAsync_SetNoCharset_DefaultCharsetUsed()
        {
            // Use content with umlaut characters.
            string sourceString = "ÄäüÜ"; // c4 e4 fc dc
            Encoding defaultEncoding = Encoding.GetEncoding("utf-8");
            byte[] contentBytes = defaultEncoding.GetBytes(sourceString);

            var content = new MockContent(contentBytes);

            // Reading the string should consider the charset of the 'Content-Type' header.
            string result = await content.ReadAsStringAsync();

            Assert.Equal(sourceString, result);
        }

        [Fact]
        public async Task ReadAsByteArrayAsync_EmptyContent_EmptyArray()
        {
            var content = new MockContent(new byte[0]);
            byte[] bytes = await content.ReadAsByteArrayAsync();
            Assert.Equal(0, bytes.Length);
        }

        [Fact]
        public void Dispose_DisposedObjectThenAccessMembers_ThrowsObjectDisposedException()
        {
            var content = new MockContent();
            content.Dispose();

            var m = new MemoryStream();

            Assert.Throws<ObjectDisposedException>(() => { content.CopyToAsync(m); });
            Assert.Throws<ObjectDisposedException>(() => { content.ReadAsByteArrayAsync(); });
            Assert.Throws<ObjectDisposedException>(() => { content.ReadAsStringAsync(); });
            Assert.Throws<ObjectDisposedException>(() => { content.ReadAsStreamAsync(); });
            Assert.Throws<ObjectDisposedException>(() => { content.LoadIntoBufferAsync(); });

            // Note that we don't throw when users access the Headers property. This is useful e.g. to be able to 
            // read the headers of a content, even though the content is already disposed. Note that the .NET guidelines
            // only require members to throw ObjectDisposedExcpetion for members "that cannot be used after the object 
            // has been disposed of".
            _output.WriteLine(content.Headers.ToString());
        }

        #region Helper methods

        private byte[] EncodeStringWithBOM(Encoding encoding, string str)
        {
            byte[] rawBytes = encoding.GetBytes(str);
            byte[] preamble = encoding.GetPreamble(); // Get the correct BOM characters
            byte[] contentBytes = new byte[preamble.Length + rawBytes.Length];
            Array.Copy(preamble, 0, contentBytes, 0, preamble.Length);
            Array.Copy(rawBytes, 0, contentBytes, preamble.Length, rawBytes.Length);
            return contentBytes;
        }

        public class MockException : Exception
        {
            public MockException() { }
            public MockException(string message) : base(message) { }
            public MockException(string message, Exception inner) : base(message, inner) { }
        }

        [Flags]
        private enum MockOptions
        {
            None = 0x0,
            ThrowInSerializeMethods = 0x1,
            ReturnNullInCopyToAsync = 0x2,
            UseWriteByteInCopyTo = 0x4,
            DontOverrideCreateContentReadStream = 0x8,
            CanCalculateLength = 0x10,
            ThrowInTryComputeLength = 0x20,
            ThrowInAsyncSerializeMethods = 0x40
        }

        private class MockContent : HttpContent
        {
            private byte[] _mockData;
            private MockOptions _options;
            private Exception _customException;
            
            public int TryComputeLengthCount { get; private set; }
            public int SerializeToStreamAsyncCount { get; private set; }
            public int CreateContentReadStreamCount { get; private set; }
            public int DisposeCount { get; private set; }

            public byte[] MockData
            {
                get { return _mockData; }
            }

            public MockContent()
                : this((byte[])null, MockOptions.None)
            { 
            }

            public MockContent(byte[] mockData)
                : this(mockData, MockOptions.None)
            {
            }

            public MockContent(MockOptions options)
                : this((byte[])null, options)
            { 
            }

            public MockContent(Exception customException, MockOptions options)
                : this((byte[])null, options)
            {
                _customException = customException;
            }

            public MockContent(byte[] mockData, MockOptions options)
            {
                _options = options;

                if (mockData == null)
                {
                    _mockData = Encoding.UTF8.GetBytes("data");
                }
                else
                {
                    _mockData = mockData;
                }
            }

            public byte[] GetMockData()
            {
                return _mockData;
            }

            protected override bool TryComputeLength(out long length)
            {
                TryComputeLengthCount++;

                if ((_options & MockOptions.ThrowInTryComputeLength) != 0)
                {
                    throw new MockException();
                }

                if ((_options & MockOptions.CanCalculateLength) != 0)
                {
                    length = _mockData.Length;
                    return true;
                }
                else
                {
                    length = 0;
                    return false;
                }
            }

            protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                SerializeToStreamAsyncCount++;

                if ((_options & MockOptions.ReturnNullInCopyToAsync) != 0)
                {
                    return null;
                }

                if ((_options & MockOptions.ThrowInAsyncSerializeMethods) != 0)
                {
                    throw _customException;
                }

                return Task.Run(() =>
                {
                    CheckThrow();
                    return stream.WriteAsync(_mockData, 0, _mockData.Length);
                });
            }

            protected override Task<Stream> CreateContentReadStreamAsync()
            {
                CreateContentReadStreamCount++;

                if ((_options & MockOptions.DontOverrideCreateContentReadStream) != 0)
                {
                    return base.CreateContentReadStreamAsync();
                }
                else
                {
                    return Task.FromResult<Stream>(new MockMemoryStream(_mockData, 0, _mockData.Length, false));
                }
            }

            protected override void Dispose(bool disposing)
            {
                DisposeCount++;
                base.Dispose(disposing);
            }

            private void CheckThrow()
            {
                if ((_options & MockOptions.ThrowInSerializeMethods) != 0)
                {
                    throw _customException;
                }
            }
        }

        private class MockMemoryStream : MemoryStream
        {
            public int DisposeCount { get; private set; }

            public MockMemoryStream(byte[] buffer, int index, int count, bool writable)
                : base(buffer, index, count, writable)
            {
            }

            protected override void Dispose(bool disposing)
            {
                DisposeCount++;
                base.Dispose(disposing);
            }
        }
        
        #endregion
    }
}
