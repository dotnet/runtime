// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public partial class FileStream_FlushAsync : FileSystemTest
    {
        [Fact]
        public async Task FlushAsyncThrowsForDisposedStream()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                fs.Dispose();
                await Assert.ThrowsAsync<ObjectDisposedException>(() => fs.FlushAsync());
            }
        }

        [Fact]
        public async Task BasicFlushAsyncFunctionality()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                fs.WriteByte(0);
                await fs.FlushAsync();

                fs.WriteByte(0xFF);
                await fs.FlushAsync();
            }
        }

        [Fact]
        public async Task FlushAsyncWhenNothingToFlush()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                fs.WriteByte(0);
                await fs.FlushAsync();

                await fs.FlushAsync();
                await fs.FlushAsync();
                await fs.FlushAsync();
            }
        }

        [Fact]
        public async Task FlushAsyncOnReadOnlyFileDoesNotThrow()
        {
            string fileName = GetTestFilePath();
            File.WriteAllBytes(fileName, new byte[] { 0 });
            File.SetAttributes(fileName, FileAttributes.ReadOnly);
            try
            {
                using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    await fs.FlushAsync();
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
        public async Task FlushAfterReading(bool? flushToDisk)
        {
            string fileName = GetTestFilePath();
            File.WriteAllBytes(fileName, TestBuffer);
            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, 2))
            {
                Assert.Equal(TestBuffer[0], fs.ReadByte());
                await fs.FlushAsync();
            }
        }

        [Fact]
        public async Task FlushAsyncWriteWithOtherClient()
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
                await fs.FlushAsync();
                Assert.Equal(TestBuffer.Length, fsr.Length);

                byte[] buffer = new byte[TestBuffer.Length];
                fsr.Read(buffer, 0, buffer.Length);
                Assert.Equal(TestBuffer, buffer);
            }
        }

        [Fact]
        public void FlushAsyncWithCanceledToken()
        {
            using (FileStream fs = File.OpenWrite(GetTestFilePath()))
            {
                Assert.True(fs.FlushAsync(new CancellationToken(true)).IsCanceled);
            }
        }

    }
}
