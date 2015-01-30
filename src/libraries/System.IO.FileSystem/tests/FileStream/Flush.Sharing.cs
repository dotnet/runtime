// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

namespace System.IO.FileSystem.Tests
{
    partial class FileStream_Flush
    {
        // Requires concurrent readers/writers which is not supported on all platforms
        [Fact]
        public void FlushWriteWithOtherClient()
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
                fs.Flush();
                Assert.Equal(TestBuffer.Length, fsr.Length);

                byte[] buffer = new byte[TestBuffer.Length];
                fsr.Read(buffer, 0, buffer.Length);
                Assert.Equal(TestBuffer, buffer);
            }
        }

    }
}
