// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression.Tests
{
    public class ZLibDeflateStreamTests : DeflateStreamTests, IDisposable
    {
        public ZLibDeflateStreamTests() { Common.SetDeflaterMode("zlib"); }
        public void Dispose() { Common.SetDeflaterMode("unknown"); }
    }

    public class ManagedDeflateStreamTests : DeflateStreamTests, IDisposable
    {
        public ManagedDeflateStreamTests() { Common.SetDeflaterMode("managed"); }
        public void Dispose() { Common.SetDeflaterMode("unknown"); }
    }

    public abstract class DeflateStreamTests
    {
        static string gzTestFile(String fileName) { return Path.Combine("GZTestData", fileName); }

        [Fact]
        public void BaseStream1()
        {
            var writeStream = new MemoryStream();
            var zip = new DeflateStream(writeStream, CompressionMode.Compress);

            Assert.Same(zip.BaseStream, writeStream);
            writeStream.Dispose();
        }

        [Fact]
        public void BaseStream2()
        {
            var ms = new MemoryStream();
            var zip = new DeflateStream(ms, CompressionMode.Decompress);

            Assert.Same(zip.BaseStream, ms);
            ms.Dispose();
        }

        [Fact]
        public async Task ModifyBaseStream()
        {
            var ms = await LocalMemoryStream.readAppFileAsync(gzTestFile("GZTestDocument.txt.gz"));
            var newMs = StripHeaderAndFooter.Strip(ms);

            var zip = new DeflateStream(newMs, CompressionMode.Decompress);
            int size = 1024;
            Byte[] bytes = new Byte[size];
            zip.BaseStream.Read(bytes, 0, size); // This will throw if the underlying stream is not writeable as expected

            zip.BaseStream.Position = 0;
            await zip.BaseStream.ReadAsync(bytes, 0, size); 
        }

        [Fact]
        public void DecompressCanRead()
        {
            var ms = new MemoryStream();
            var zip = new DeflateStream(ms, CompressionMode.Decompress);

            Assert.True(zip.CanRead);

            zip.Dispose();
            Assert.False(zip.CanRead);
        }

        [Fact]
        public void CompressCanWrite()
        {
            var ms = new MemoryStream();
            var zip = new DeflateStream(ms, CompressionMode.Compress);
            Assert.True(zip.CanWrite);

            zip.Dispose();
            Assert.False(zip.CanWrite);
        }

        [Fact]
        public void CanDisposeBaseStream()
        {
            var ms = new MemoryStream();
            var zip = new DeflateStream(ms, CompressionMode.Compress);
            ms.Dispose(); // This would throw if this was invalid
        }

        [Fact]
        public void CanDisposeDeflateStream()
        {
            var ms = new MemoryStream();
            var zip = new DeflateStream(ms, CompressionMode.Compress);
            zip.Dispose();

            // Base Stream should be null after dispose 
            Assert.Null(zip.BaseStream);

            zip.Dispose(); // Should be a no-op
        }

        [Fact]
        public async Task CanReadBaseStreamAfterDispose()
        {
            var ms = await LocalMemoryStream.readAppFileAsync(gzTestFile("GZTestDocument.txt.gz"));
            var newMs = StripHeaderAndFooter.Strip(ms);

            var zip = new DeflateStream(newMs, CompressionMode.Decompress, true);
            var baseStream = zip.BaseStream;
            zip.Dispose();

            int size = 1024;
            Byte[] bytes = new Byte[size];
            baseStream.Read(bytes, 0, size); // This will throw if the underlying stream is not writeable as expected

            baseStream.Position = 0;
            await baseStream.ReadAsync(bytes, 0, size); 
        }

        [Fact]
        public async Task DecompressWorks()
        {
            var compareStream = await LocalMemoryStream.readAppFileAsync(gzTestFile("GZTestDocument.txt"));
            var gzStream = await LocalMemoryStream.readAppFileAsync(gzTestFile("GZTestDocument.txt.gz"));

            await DecompressAsync(compareStream, gzStream);
        }

        [Fact]
        public async Task DecompressWorksWithBinaryFile()
        {
            var compareStream = await LocalMemoryStream.readAppFileAsync(gzTestFile("GZTestDocument.doc"));
            var gzStream = await LocalMemoryStream.readAppFileAsync(gzTestFile("GZTestDocument.doc.gz"));

            await DecompressAsync(compareStream, gzStream);
        }

        // Making this async since regular read/write are tested below
        private async Task DecompressAsync(MemoryStream compareStream, MemoryStream gzStream)
        {
            var strippedMs = StripHeaderAndFooter.Strip(gzStream);

            var ms = new MemoryStream();
            var zip = new DeflateStream(strippedMs, CompressionMode.Decompress);

            var deflateStream = new MemoryStream();

            int _bufferSize = 1024;
            var bytes = new Byte[_bufferSize];
            bool finished = false;
            int retCount;
            while (!finished)
            {
                retCount = await zip.ReadAsync(bytes, 0, _bufferSize);

                if (retCount != 0)
                    await deflateStream.WriteAsync(bytes, 0, retCount);
                else
                    finished = true;
            }

            deflateStream.Position = 0;
            compareStream.Position = 0;

            byte[] compareArray = compareStream.ToArray();
            byte[] writtenArray = deflateStream.ToArray();

            Assert.Equal(compareArray.Length, writtenArray.Length);
            for (int i = 0; i < compareArray.Length; i++)
            {
                Assert.Equal(compareArray[i], writtenArray[i]);
            }
        }

        [Fact]
        public async Task DecompressFailsWithRealGzStream()
        {
            String[] files = { gzTestFile("GZTestDocument.doc.gz"), gzTestFile("GZTestDocument.txt.gz") };
            foreach (String fileName in files)
            {
                var baseStream = await LocalMemoryStream.readAppFileAsync(fileName);
                var zip = new DeflateStream(baseStream, CompressionMode.Decompress);
                int _bufferSize = 2048;
                var bytes = new Byte[_bufferSize];
                Assert.Throws<InvalidDataException>(() => { zip.Read(bytes, 0, _bufferSize); });
                zip.Dispose();
            }
        }

        [Fact]
        public void NullBaseStreamThrows()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var deflate = new DeflateStream(null, CompressionMode.Decompress);
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                var deflate = new DeflateStream(null, CompressionMode.Compress);
            });
        }

        [Fact]
        public void DisposedBaseStreamThrows()
        {
            var ms = new MemoryStream();
            ms.Dispose();
            Assert.Throws<ArgumentException>(() =>
            {
                var deflate = new DeflateStream(ms, CompressionMode.Decompress);
            });

            Assert.Throws<ArgumentException>(() =>
            {
                var deflate = new DeflateStream(ms, CompressionMode.Compress);
            });
        }

        [Fact]
        public void ReadOnlyStreamThrowsOnCompress()
        {
            var ms = new LocalMemoryStream();
            ms.SetCanWrite(false);

            Assert.Throws<ArgumentException>(() =>
            {
                var gzip = new DeflateStream(ms, CompressionMode.Compress);
            });
        }

        [Fact]
        public void WriteOnlyStreamThrowsOnDecompress()
        {
            var ms = new LocalMemoryStream();
            ms.SetCanRead(false);

            Assert.Throws<ArgumentException>(() =>
            {
                var gzip = new DeflateStream(ms, CompressionMode.Decompress);
            });
        }

        [Fact]
        public void TestCtors()
        {
            CompressionLevel[] legalValues = new CompressionLevel[] { CompressionLevel.Optimal, CompressionLevel.Fastest, CompressionLevel.NoCompression };

            foreach (CompressionLevel level in legalValues)
            {
                bool[] boolValues = new bool[] { true, false };

                foreach (bool remainsOpen in boolValues)
                {
                    TestCtor(level, remainsOpen);
                }
            }
        }

        [Fact]
        public void TestLevelOptimial()
        {
            TestCtor(CompressionLevel.Optimal);
        }

        [Fact]
        public void TestLevelNoCompression()
        {
            TestCtor(CompressionLevel.NoCompression);
        }

        [Fact]
        public void TestLevelFastest()
        {
            TestCtor(CompressionLevel.Fastest);
        }

        private static void TestCtor(CompressionLevel level, bool? leaveOpen = null)
        {
            //Create the DeflateStream
            int _bufferSize = 1024;
            var bytes = new Byte[_bufferSize];
            var baseStream = new MemoryStream(bytes, true);
            DeflateStream ds;

            if (leaveOpen == null)
            {
                ds = new DeflateStream(baseStream, level);
            }
            else
            {
                ds = new DeflateStream(baseStream, level, leaveOpen ?? false);
            }

            //Write some data and Close the stream
            String strData = "Test Data";
            var encoding = Encoding.UTF8;
            Byte[] data = encoding.GetBytes(strData);
            ds.Write(data, 0, data.Length);
            ds.Flush();
            ds.Dispose();

            if (leaveOpen != true)
            {
                //Check that Close has really closed the underlying stream
                Assert.Throws<ObjectDisposedException>(() => { baseStream.Write(bytes, 0, bytes.Length); });
            }

            //Read the data
            Byte[] data2 = new Byte[_bufferSize];
            baseStream = new MemoryStream(bytes, false);
            ds = new DeflateStream(baseStream, CompressionMode.Decompress);
            int size = ds.Read(data2, 0, _bufferSize - 5);

            //Verify the data roundtripped
            for (int i = 0; i < size + 5; i++)
            {
                if (i < data.Length)
                {
                    Assert.Equal(data[i], data2[i]);
                }
                else
                {
                    Assert.Equal(data2[i], (byte)0);
                }
            }
        }

        [Fact]
        public async Task Flush()
        {
            var ms = new MemoryStream();
            var ds = new DeflateStream(ms, CompressionMode.Compress);
            ds.Flush();
            await ds.FlushAsync();

            // Just ensuring Flush doesn't throw
        }

        [Fact]
        public void FlushFailsAfterDispose()
        {
            var ms = new MemoryStream();
            var ds = new DeflateStream(ms, CompressionMode.Compress);
            ds.Dispose();
            Assert.Throws<ObjectDisposedException>(() => { ds.Flush(); });
        }

        [Fact]
        public async Task FlushAsyncFailsAfterDispose()
        {

            var ms = new MemoryStream();
            var ds = new DeflateStream(ms, CompressionMode.Compress);
            ds.Dispose();

            await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            {
                await ds.FlushAsync();
            });
        }

        [Fact]
        public void TestSeekMethodsDecompress()
        {
            var ms = new MemoryStream();
            var zip = new DeflateStream(ms, CompressionMode.Decompress);

            Assert.False(zip.CanSeek, "CanSeek should be false");

            Assert.Throws<NotSupportedException>(delegate { long value = zip.Length; });
            Assert.Throws<NotSupportedException>(delegate { long value = zip.Position; });
            Assert.Throws<NotSupportedException>(delegate { zip.Position = 100L; });
            Assert.Throws<NotSupportedException>(delegate { zip.SetLength(100L); });
            //Should we try all the enums? doesn't seem necessary
            Assert.Throws<NotSupportedException>(delegate { zip.Seek(100L, SeekOrigin.Begin); });
        }

        [Fact]
        public void TestSeekMethodsCompress()
        {
            var ms = new MemoryStream();
            var zip = new DeflateStream(ms, CompressionMode.Compress);

            Assert.False(zip.CanSeek, "CanSeek should be false");

            Assert.Throws<NotSupportedException>(delegate { long value = zip.Length; });
            Assert.Throws<NotSupportedException>(delegate { long value = zip.Position; });
            Assert.Throws<NotSupportedException>(delegate { zip.Position = 100L; });
            Assert.Throws<NotSupportedException>(delegate { zip.SetLength(100L); });
            //Should we try all the enums? doesn't seem necessary
            Assert.Throws<NotSupportedException>(delegate { zip.Seek(100L, SeekOrigin.Begin); });
        }
    }
}
