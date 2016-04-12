// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Http.Tests
{
    public class HttpContentTest
    {
        [Fact]
        public void Dispose_BufferContentThenDisposeContent_BufferedStreamGetsDisposed()
        {
            MockContent content = new MockContent();
            content.LoadIntoBufferAsync().Wait();

            Type type = typeof(HttpContent);
            TypeInfo typeInfo = type.GetTypeInfo();
            FieldInfo bufferedContentField = typeof(HttpContent).GetField("_bufferedContent",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(bufferedContentField);

            MemoryStream bufferedContentStream = bufferedContentField.GetValue(content) as MemoryStream;
            Assert.NotNull(bufferedContentStream);

            content.Dispose();

            // The following line will throw an ObjectDisposedException if the buffered-stream was correctly disposed.
            Assert.Throws<ObjectDisposedException>(() => { string str = bufferedContentStream.Length.ToString(); });
        }

        [Theory]
        [InlineData(1, 100, 99, 1)]
        [InlineData(1, 100, 50, 99)]
        [InlineData(1, 100, 98, 98)]
        [InlineData(1, 100, 99, 99)]
        [InlineData(1, 100, 99, 98)]
        [InlineData(3, 50, 100, 149)]
        [InlineData(3, 50, 149, 149)]
        public async Task LoadIntoBufferAsync_ContentLengthSmallerThanActualData_ActualDataLargerThanMaxSize_ThrowsException(
            int numberOfWrites, int sizeOfEachWrite, int reportedLength, int maxSize)
        {
            Assert.InRange(maxSize, 1, (numberOfWrites * sizeOfEachWrite) - 1);

            LieAboutLengthContent c = new LieAboutLengthContent(numberOfWrites, sizeOfEachWrite, reportedLength);
            Task t = c.LoadIntoBufferAsync(maxSize);
            await Assert.ThrowsAsync<HttpRequestException>(() => t);
        }

        private sealed class LieAboutLengthContent : HttpContent
        {
            private readonly int _numberOfWrites, _sizeOfEachWrite, _reportedLength;

            public LieAboutLengthContent(int numberOfWrites, int sizeOfEachWrite, int reportedLength)
            {
                _numberOfWrites = numberOfWrites;
                _sizeOfEachWrite = sizeOfEachWrite;
                _reportedLength = reportedLength;
            }

            protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                byte[] bytes = new byte[_sizeOfEachWrite];
                for (int i = 0; i < _numberOfWrites; i++)
                {
                    await stream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
                }
            }

            protected internal override bool TryComputeLength(out long length)
            {
                length = _reportedLength;
                return true;
            }
        }
    }
}
