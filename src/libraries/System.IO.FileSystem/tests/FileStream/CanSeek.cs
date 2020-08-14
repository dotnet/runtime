// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.IO.Pipes;
using Xunit;

namespace System.IO.Tests
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

        [Fact]
        [PlatformSpecific(~TestPlatforms.Browser)] // IO.Pipes not supported
        public void CanSeekReturnsFalseForPipe()
        {
            using (var pipeStream = new AnonymousPipeServerStream())
            using (var clientHandle = pipeStream.ClientSafePipeHandle)
            {
                SafeFileHandle handle = new SafeFileHandle((IntPtr)int.Parse(pipeStream.GetClientHandleAsString()), false);
                using (FileStream fs = new FileStream(handle, FileAccess.Write, 1, false))
                {
                    Assert.False(fs.CanSeek);
                }
            }
        }
    }
}
