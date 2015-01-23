// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class FileStream_IsAsync : FileSystemTest
    {
        [Fact]
        public void IsAsyncConstructorArg()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.ReadWrite, FileShare.Read, 4096, true))
            {
                Assert.True(fs.IsAsync);
            }

            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.ReadWrite, FileShare.Read, 4096, false))
            {
                Assert.False(fs.IsAsync);
            }
        }

        [Fact]
        public void FileOptionsAsynchronousConstructorArg()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.ReadWrite, FileShare.Read, 4096, FileOptions.Asynchronous))
            {
                Assert.True(fs.IsAsync);
            }

            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.ReadWrite, FileShare.Read, 4096, FileOptions.None))
            {
                Assert.False(fs.IsAsync);
            }
        }

        [Fact]
        public void AsyncDiscoveredFromHandle()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.ReadWrite, FileShare.Read, 4096, true))
            using (FileStream fsh = new FileStream(fs.SafeFileHandle, FileAccess.ReadWrite))
            {
                Assert.True(fsh.IsAsync);
            }

            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.ReadWrite, FileShare.Read, 4096, false))
            using (FileStream fsh = new FileStream(fs.SafeFileHandle, FileAccess.ReadWrite))
            {
                Assert.False(fsh.IsAsync);
            }
        }
    }
}
