// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using Xunit;

namespace System.IO.Tests
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
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "NetFX defaults useAsync to false for the SafeFileHandle/FileAccess constructor which leads to an ArgumentException for an async handle. In netcoreapp this constructor was fixed to check the async status of the handle and use that for the useAsync value.")]
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
