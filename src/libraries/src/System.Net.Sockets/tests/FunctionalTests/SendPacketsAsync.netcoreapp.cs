// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Net.Sockets.Tests
{
    public partial class SendPacketsAsync
    {
        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        public void SendPacketsElement_FileZeroCount_OffsetLong_Success(SocketImplementationType type)
        {
            var element = new SendPacketsElement(TestFileName, 0L, 0);
            SendPackets(type, element, s_testFileSize, GetExpectedContent(element));  // Whole File
        }

        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        public void SendPacketsElement_FilePart_OffsetLong_Success(SocketImplementationType type)
        {
            var element = new SendPacketsElement(TestFileName, 10L, 20);
            SendPackets(type, element, 20, GetExpectedContent(element));
        }

        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        public void SendPacketsElement_FileMultiPart_OffsetLong_Success(SocketImplementationType type)
        {
            var elements = new[]
            {
                new SendPacketsElement(TestFileName, 10L, 20),
                new SendPacketsElement(TestFileName, 30L, 10),
                new SendPacketsElement(TestFileName, 0L, 10),
            };
            SendPackets(type, elements, SocketError.Success, 40, GetExpectedContent(elements));
        }

        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        public void SendPacketsElement_FileLargeOffset_OffsetLong_Throws(SocketImplementationType type)
        {
            // Length is validated on Send
            SendPackets(type, new SendPacketsElement(TestFileName, (long)uint.MaxValue + 11000, 1), SocketError.InvalidArgument, 0);
        }

        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        public void SendPacketsElement_FileLargeCount_OffsetLong_Throws(SocketImplementationType type)
        {
            // Length is validated on Send
            SendPackets(type, new SendPacketsElement(TestFileName, 5L, 10000), SocketError.InvalidArgument, 0);
        }

        #region FileStreams

        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        public void SendPacketsElement_FileStream_Success(SocketImplementationType type)
        {
            using (var stream = new FileStream(TestFileName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
            {
                stream.Seek(s_testFileSize / 2, SeekOrigin.Begin);
                SendPackets(type, new SendPacketsElement(stream), s_testFileSize); // Whole File
                Assert.Equal(s_testFileSize / 2, stream.Position);

                SendPackets(type, new SendPacketsElement(stream), s_testFileSize); // Whole File
                Assert.Equal(s_testFileSize / 2, stream.Position);
            }
        }

        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        public void SendPacketsElement_FileStreamZeroCount_Success(SocketImplementationType type)
        {
            using (var stream = new FileStream(TestFileName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
            {
                stream.Seek(s_testFileSize / 2, SeekOrigin.Begin);
                SendPackets(type, new SendPacketsElement(stream, 0, 0), s_testFileSize); // Whole File
                Assert.Equal(s_testFileSize / 2, stream.Position);

                SendPackets(type, new SendPacketsElement(stream, 0, 0), s_testFileSize); // Whole File
                Assert.Equal(s_testFileSize / 2, stream.Position);
            }
        }

        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        public void SendPacketsElement_FileStreamSizeCount_Success(SocketImplementationType type)
        {
            using (var stream = new FileStream(TestFileName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
            {
                stream.Seek(s_testFileSize / 2, SeekOrigin.Begin);
                SendPackets(type, new SendPacketsElement(stream, 0, s_testFileSize), s_testFileSize); // Whole File
                Assert.Equal(s_testFileSize / 2, stream.Position);

                SendPackets(type, new SendPacketsElement(stream, 0, s_testFileSize), s_testFileSize); // Whole File
                Assert.Equal(s_testFileSize / 2, stream.Position);
            }
        }

        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        public void SendPacketsElement_FileStreamPart_Success(SocketImplementationType type)
        {
            using (var stream = new FileStream(TestFileName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
            {
                stream.Seek(s_testFileSize - 10, SeekOrigin.Begin);
                SendPackets(type, new SendPacketsElement(stream, 0, 20), 20);
                Assert.Equal(s_testFileSize - 10, stream.Position);

                SendPackets(type, new SendPacketsElement(stream, 10, 20), 20);
                Assert.Equal(s_testFileSize - 10, stream.Position);

                SendPackets(type, new SendPacketsElement(stream, s_testFileSize - 20, 20), 20);
                Assert.Equal(s_testFileSize - 10, stream.Position);
            }
        }

        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        public void SendPacketsElement_FileStreamMultiPart_Success(SocketImplementationType type)
        {
            using (var stream = new FileStream(TestFileName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous))
            {
                var elements = new[]
                {
                    new SendPacketsElement(stream, 0, 20),
                    new SendPacketsElement(stream, s_testFileSize - 10, 10),
                    new SendPacketsElement(stream, 0, 10),
                    new SendPacketsElement(stream, 10, 20),
                    new SendPacketsElement(stream, 30, 10),
                };
                stream.Seek(s_testFileSize - 10, SeekOrigin.Begin);
                SendPackets(type, elements, SocketError.Success, 70, GetExpectedContent(elements));
                Assert.Equal(s_testFileSize - 10, stream.Position);

                SendPackets(type, elements, SocketError.Success, 70, GetExpectedContent(elements));
                Assert.Equal(s_testFileSize - 10, stream.Position);
            }
        }

        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        public void SendPacketsElement_FileStreamLargeOffset_Throws(SocketImplementationType type)
        {
            using (var stream = new FileStream(TestFileName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
            {
                stream.Seek(s_testFileSize / 2, SeekOrigin.Begin);
                // Length is validated on Send
                SendPackets(type, new SendPacketsElement(stream, (long)uint.MaxValue + 11000, 1), SocketError.InvalidArgument, 0);
            }
        }

        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        public void SendPacketsElement_FileStreamLargeCount_Throws(SocketImplementationType type)
        {
            using (var stream = new FileStream(TestFileName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
            {
                stream.Seek(s_testFileSize / 2, SeekOrigin.Begin);
                // Length is validated on Send
                SendPackets(type, new SendPacketsElement(stream, 5, 10000),
                    SocketError.InvalidArgument, 0);
            }
        }

        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        public void SendPacketsElement_FileStreamWithOptions_Success(SocketImplementationType type) {
            using (var stream = new FileStream(TestFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan)) {
                var element = new SendPacketsElement(stream, 0, s_testFileSize);
                SendPackets(type, element, s_testFileSize, GetExpectedContent(element)); 
            }
        }

        #endregion FileStreams

        #region Mixed Buffer, FilePath, FileStream tests

        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        public void SendPacketsElement_FileStreamMultiPartMixed_Success(SocketImplementationType type) {
            using (var stream = new FileStream(TestFileName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous)) {
                var elements = new[]
                {
                    new SendPacketsElement(new byte[] { 5, 6, 7 }, 0, 3),
                    new SendPacketsElement(stream, s_testFileSize - 10, 10),
                    new SendPacketsElement(TestFileName, 0L, 10),
                    new SendPacketsElement(stream, 10L, 20),
                    new SendPacketsElement(TestFileName, 30, 10),
                    new SendPacketsElement(new byte[] { 8, 9, 10 }, 0, 3),
                };
                byte[] expected = GetExpectedContent(elements);
                SendPackets(type, elements, SocketError.Success, expected.Length, expected);
            }
        }

        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        public void SendPacketsElement_FileStreamMultiPartMixed_MultipleFileStreams_Success(SocketImplementationType type) {
            using (var stream = new FileStream(TestFileName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous)) 
            using (var stream2 = new FileStream(TestFileName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous)) {
                var elements = new[]
                {
                    new SendPacketsElement(new byte[] { 5, 6, 7 }, 0, 0),
                    new SendPacketsElement(stream, s_testFileSize - 10, 10),
                    new SendPacketsElement(stream2, s_testFileSize - 100, 10),
                    new SendPacketsElement(TestFileName, 0L, 10),
                    new SendPacketsElement(new byte[] { 8, 9, 10 }, 0, 1),
                    new SendPacketsElement(TestFileName, 30, 10),
                };
                byte[] expected = GetExpectedContent(elements);
                SendPackets(type, elements, SocketError.Success, expected.Length, expected);
            }
        }

        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        public void SendPacketsElement_FileStreamMultiPartMixed_MultipleWholeFiles_Success(SocketImplementationType type) {
            using (var stream = new FileStream(TestFileName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous)) {
                var elements = new[]
                {
                    new SendPacketsElement(stream, 0L, 0),
                    new SendPacketsElement(TestFileName, 0L, 10),
                    new SendPacketsElement(stream, 0L, 0),
                    new SendPacketsElement(TestFileName, 0L, 10),
                };
                byte[] expected = GetExpectedContent(elements);
                SendPackets(type, elements, SocketError.Success, expected.Length, expected);
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Recreate what SendPacketsAsync should send given the <paramref name="elements"/>,
        /// directly by collating their buffers or reading from their files.
        /// </summary>
        /// <param name="elements"></param>
        /// <returns></returns>
        private byte[] GetExpectedContent(params SendPacketsElement[] elements) {

            void ReadFromFile(string filePath, long offset, long count, byte[] destination, ref long destinationOffset) {
                using (FileStream fs = new FileStream(filePath, FileMode.Open,FileAccess.Read, FileShare.Read)) {
                    // Passing a zero count to SendPacketsElement means it sends the whole file.
                    if (count == 0) {
                        count = fs.Length;
                    }
                    fs.Position = offset;
                    int actualRead = 0;
                    do {
                        actualRead += fs.Read(destination, (int) destinationOffset + actualRead, (int) count - actualRead);
                    } while (actualRead != count && fs.Position < fs.Length);
                    destinationOffset += actualRead;
                }
            }

            int FileCount(SendPacketsElement element) {
                if (element.Count != 0) return element.Count;
                if (element.FilePath != null) {
                    return (int) new FileInfo(element.FilePath).Length;
                }
                else if (element.FileStream != null) {
                    return (int) element.FileStream.Length;
                }
                throw new ArgumentException("Expected SendPacketsElement with FilePath or FileStream set.", nameof(element));
            }

            int totalCount = 0;
            foreach (var element in elements) {
                totalCount += element.Buffer != null ? element.Count : FileCount(element);
            }
            var result = new byte[totalCount];
            long resultOffset = 0L;
            foreach (var spe in elements) {
                if (spe.FilePath != null) {
                    ReadFromFile(spe.FilePath, spe.OffsetLong, spe.Count, result, ref resultOffset);
                }
                else if (spe.FileStream != null) {
                    ReadFromFile(spe.FileStream.Name, spe.OffsetLong, spe.Count, result, ref resultOffset);
                }
                else if (spe.Buffer != null && spe.Count > 0) {
                    Array.Copy(spe.Buffer, spe.OffsetLong, result, resultOffset, spe.Count);
                    resultOffset += spe.Count;
                }
            }

            Assert.Equal(totalCount, resultOffset);
            return result;
        }

        #endregion
    }
}
