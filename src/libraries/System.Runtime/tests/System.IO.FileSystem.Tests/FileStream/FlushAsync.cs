// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public partial class FileStream_FlushAsync : FileSystemTest
    {
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
    }
}
