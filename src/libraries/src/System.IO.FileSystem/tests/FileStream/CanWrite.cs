// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Xunit;

namespace System.IO.Tests
{
    public class FileStream_CanWrite : FileSystemTest
    {
        [Fact]
        public void CanWriteTrueForWriteableStream()
        {
            string fileName = GetTestFilePath();
            using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite))
            {
                Assert.True(fs.CanWrite);
            }

            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Write))
            {
                Assert.True(fs.CanWrite);
            }
        }

        [Fact]
        public void CanWriteFalseForReadOnlyStream()
        {
            string fileName = GetTestFilePath();
            using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite))
            { }

            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                Assert.False(fs.CanWrite);
            }
        }

        [Fact]
        public void CanWriteFalseForDisposedStream()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                Assert.True(fs.CanWrite);
                fs.Dispose();
                Assert.False(fs.CanWrite);
            }
        }
    }
}
