// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Xunit;

namespace System.IO.Tests
{
    public class FileStream_CanRead : FileSystemTest
    {
        [Fact]
        public void CanReadTrueForReadableStream()
        {
            string fileName = GetTestFilePath();
            using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite))
            {
                Assert.True(fs.CanRead);
            }

            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                Assert.True(fs.CanRead);
            }
        }

        [Fact]
        public void CanReadFalseForWriteOnlyStream()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.Write))
            {
                Assert.False(fs.CanRead);
            }
        }

        [Fact]
        public void CanReadFalseForDisposedStream()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                Assert.True(fs.CanRead);
                fs.Dispose();
                Assert.False(fs.CanRead);
            }
        }
    }
}
