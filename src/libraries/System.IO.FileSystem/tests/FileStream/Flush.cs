// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

namespace System.IO.FileSystem.Tests
{
    public partial class FileStream_Flush : FileSystemTest
    {
        [Fact]
        public void FlushThrowsForDisposedStream()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                fs.Dispose();
                Assert.Throws<ObjectDisposedException>(() => fs.Flush());
            }
        }

        [Fact]
        public void BasicFlushFunctionality()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                fs.WriteByte(0);
                fs.Flush();
            }
        }

        [Fact]
        public void FlushOnReadOnlyFileDoesNotThrow()
        {
            string fileName = GetTestFilePath();
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                fs.WriteByte(0);
            }

            using (FileStream fs = new FileStream(fileName, FileMode.Open))
            {
                fs.Flush();
            }
        }
    }
}
