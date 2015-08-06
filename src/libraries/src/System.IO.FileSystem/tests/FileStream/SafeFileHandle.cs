// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class FileStream_SafeFileHandle : FileSystemTest
    {
        [Fact]
        public void HandleNotNull()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                Assert.NotNull(fs.SafeFileHandle);
            }
        }

        [Fact]
        public void DisposeClosesHandle()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                SafeFileHandle handle = fs.SafeFileHandle;

                if (!handle.IsInvalid)
                {
                    fs.Dispose();

                    Assert.True(handle.IsClosed);
                }
            }
        }

        [Fact]
        public void AccessFlushesFileClosesHandle()
        {
            string fileName = GetTestFilePath();

            using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete))
            using (FileStream fsr = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                // write will be buffered
                fs.Write(TestBuffer, 0, TestBuffer.Length);

                // other handle doesn't yet see the change
                Assert.Equal(0, fsr.Length);

                SafeFileHandle handle = fs.SafeFileHandle;

                // expect the handle to be flushed
                Assert.Equal(TestBuffer.Length, fsr.Length);
            }
        }

        [Fact]
        public void ThrowWhenHandlePositionIsChanged()
        {
            string fileName = GetTestFilePath();

            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                // write some data to move the position, flush to ensure OS position is updated
                fs.Write(TestBuffer, 0, TestBuffer.Length);
                fs.Flush();

                if (fs.SafeFileHandle.IsInvalid)
                {
                    // nothing to test
                    return;
                }

                using (FileStream fsr = new FileStream(fs.SafeFileHandle, FileAccess.Read, TestBuffer.Length))
                {
                    Assert.Equal(TestBuffer.Length, fs.Position);
                    Assert.Equal(TestBuffer.Length, fsr.Position);

                    // Operations on original filestream will fail if data is in buffer and position changes.

                    // Put data in FS write buffer and update position from FSR
                    fs.WriteByte(0);
                    fsr.Position = 0; 
                    Assert.Throws<IOException>(() => fs.Position);

                    fs.WriteByte(0);
                    fsr.Position++; 
                    Assert.Throws<IOException>(() => fs.Read(new byte[1], 0, 1));

                    fs.WriteByte(0);
                    fsr.Position++;
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) // TODO: [ActiveIssue(812)]: Remove guard when async I/O is properly implemented on Unix
                    {
                        Assert.Throws<IOException>(() => FSAssert.CompletesSynchronously(fs.ReadAsync(new byte[1], 0, 1)));
                    }
                    else
                    {
                        Assert.ThrowsAsync<IOException>(() => fs.ReadAsync(new byte[1], 0, 1)).Wait();
                    }

                    fs.WriteByte(0);
                    fsr.Position++; 
                    Assert.Throws<IOException>(() => fs.ReadByte());

                    fs.WriteByte(0);
                    fsr.Position++; 
                    Assert.Throws<IOException>(() => fs.Seek(0, SeekOrigin.End));

                    fs.WriteByte(0);
                    fsr.Position++; 
                    Assert.Throws<IOException>(() => fs.SetLength(2));

                    fs.WriteByte(0);
                    fsr.Position++;
                    Assert.Throws<IOException>(() => fs.Flush());

                    fs.WriteByte(0);
                    fsr.Position++;
                    Assert.Throws<IOException>(() => fs.Dispose());
                }
            }
        }
    }
}
