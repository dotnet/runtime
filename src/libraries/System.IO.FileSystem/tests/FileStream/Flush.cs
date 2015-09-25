// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.IO.Tests
{
    public partial class FileStream_Flush : FileSystemTest
    {
        [Theory]
        [InlineData(null)]
        [InlineData(false)]
        [InlineData(true)]
        public void FlushThrowsForDisposedStream(bool? flushToDisk)
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                fs.Dispose();
                Assert.Throws<ObjectDisposedException>(() => Flush(fs, flushToDisk));
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData(false)]
        [InlineData(true)]
        public void BasicFlushFunctionality(bool? flushToDisk)
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                fs.WriteByte(0);
                Flush(fs, flushToDisk);

                fs.WriteByte(0xFF);
                Flush(fs, flushToDisk);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData(false)]
        [InlineData(true)]
        public void FlushWhenNothingToFlush(bool? flushToDisk)
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                fs.WriteByte(0);
                Flush(fs, flushToDisk);

                Flush(fs, flushToDisk);
                Flush(fs, flushToDisk);
                Flush(fs, flushToDisk);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData(false)]
        [InlineData(true)]
        public void FlushOnReadOnlyStreamDoesNotThrow(bool? flushToDisk)
        {
            string fileName = GetTestFilePath();
            File.WriteAllBytes(fileName, new byte[] { 0 });
            File.SetAttributes(fileName, FileAttributes.ReadOnly);
            try
            {
                using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    Flush(fs, flushToDisk);
                }
            }
            finally
            {
                File.SetAttributes(fileName, FileAttributes.Normal);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData(false)]
        [InlineData(true)]
        public void FlushAfterReading(bool? flushToDisk)
        {
            string fileName = GetTestFilePath();
            File.WriteAllBytes(fileName, TestBuffer);
            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, 2))
            {
                Assert.Equal(TestBuffer[0], fs.ReadByte());
                Flush(fs, flushToDisk);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData(false)]
        [InlineData(true)]
        public void FlushWriteWithOtherClient(bool? flushToDisk)
        {
            string fileName = GetTestFilePath();

            // ensure that we'll be using a buffer larger than our test data
            using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, TestBuffer.Length * 2))
            using (FileStream fsr = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fs.Write(TestBuffer, 0, TestBuffer.Length);
                Assert.Equal(TestBuffer.Length, fs.Length);

                // Make sure that we've actually buffered it, read handle won't see any changes
                Assert.Equal(0, fsr.Length);

                // This should cause a write, after it completes the two handles should be in sync
                Flush(fs, flushToDisk);
                Assert.Equal(TestBuffer.Length, fsr.Length);

                byte[] buffer = new byte[TestBuffer.Length];
                fsr.Read(buffer, 0, buffer.Length);
                Assert.Equal(TestBuffer, buffer);
            }
        }

        [Fact]
        public void FlushCallsFlush_flushToDisk_False()
        {
            using (StoreFlushArgFileStream fs = new StoreFlushArgFileStream(GetTestFilePath(), FileMode.Create))
            {
                fs.Flush();
                Assert.True(fs.LastFlushArg.HasValue);
                Assert.False(fs.LastFlushArg.Value);
            }
        }

        private static void Flush(FileStream fs, bool? flushArg)
        {
            if (!flushArg.HasValue)
                fs.Flush();
            else
                fs.Flush(flushArg.Value);
        }

        private sealed class StoreFlushArgFileStream : FileStream
        {
            public StoreFlushArgFileStream(string path, FileMode mode) : base(path, mode)
            {
            }

            public bool? LastFlushArg;

            public override void Flush(bool flushToDisk)
            {
                LastFlushArg = flushToDisk;
                base.Flush(flushToDisk);
            }
        }

    }
}
