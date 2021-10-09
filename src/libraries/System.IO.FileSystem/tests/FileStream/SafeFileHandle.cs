// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/34582", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
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
        public void DisposingBufferedFileStreamThatWasClosedViaSafeFileHandleCloseDoesNotThrow()
        {
            FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.ReadWrite, FileShare.Read, bufferSize: 100);
            fs.SafeFileHandle.Dispose();
            fs.Dispose(); // must not throw
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

                _ = fs.SafeFileHandle;

                // expect the handle to be flushed
                Assert.Equal(TestBuffer.Length, fsr.Length);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNet5CompatFileStreamEnabled))]
        public async Task ThrowWhenHandlePositionIsChanged_sync()
        {
            await ThrowWhenHandlePositionIsChanged(useAsync: false);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported), nameof(PlatformDetection.IsNet5CompatFileStreamEnabled))]
        public async Task ThrowWhenHandlePositionIsChanged_async()
        {
            await ThrowWhenHandlePositionIsChanged(useAsync: true);
        }

        private async Task ThrowWhenHandlePositionIsChanged(bool useAsync)
        {
            string fileName = GetTestFilePath();

            using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, 0x100, useAsync))
            {
                // write some data to move the position, flush to ensure OS position is updated
                fs.Write(TestBuffer, 0, TestBuffer.Length);
                fs.Flush();

                if (fs.SafeFileHandle.IsInvalid)
                {
                    // nothing to test
                    return;
                }

                using (FileStream fsr = new FileStream(fs.SafeFileHandle, FileAccess.Read, TestBuffer.Length, useAsync))
                {
                    Assert.Equal(TestBuffer.Length, fs.Position);
                    Assert.Equal(TestBuffer.Length, fsr.Position);

                    // Operations on original filestream will fail if data is in buffer and position changes.

                    // Put data in FS write buffer and update position from FSR
                    fs.WriteByte(0);
                    fsr.Position = 0;

                    if (useAsync
                        // Async I/O behaviors differ due to kernel-based implementation on Windows
                        && OperatingSystem.IsWindows()
                        // ReadAsync which in this case (single byte written to buffer) calls FlushAsync is now 100% async
                        // so it does not complete synchronously anymore
                        && PlatformDetection.IsNet5CompatFileStreamEnabled) 
                    {
                        Assert.Throws<IOException>(() => FSAssert.CompletesSynchronously(fs.ReadAsync(new byte[1], 0, 1)));
                    }
                    else
                    {
                        await Assert.ThrowsAsync<IOException>(() => fs.ReadAsync(new byte[1], 0, 1));
                    }

                    fs.WriteByte(0);
                    fsr.Position++;
                    Assert.Throws<IOException>(() => fs.Read(new byte[1], 0, 1));

                    fs.WriteByte(0);
                    fsr.Position++;
                    await Assert.ThrowsAsync<IOException>(() => fs.ReadAsync(new byte[1], 0, 1));

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
