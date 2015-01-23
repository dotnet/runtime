// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

namespace System.IO.FileSystem.Tests
{
    public partial class FileStream_CanSeek : FileSystemTest
    {
        [Fact]
        public void CanSeekTrueForSeekableStream()
        {
            string fileName = GetTestFilePath();
            using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite))
            {
                Assert.True(fs.CanSeek);
            }

            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                Assert.True(fs.CanSeek);
            }

            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Write))
            {
                Assert.True(fs.CanSeek);
            }
        }

        [Fact]
        public void CanSeekFalseForDisposedStream()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                Assert.True(fs.CanSeek);
                fs.Dispose();
                Assert.False(fs.CanSeek);
            }
        }
    }
}
