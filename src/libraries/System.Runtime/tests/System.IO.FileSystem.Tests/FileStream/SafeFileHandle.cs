// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.XUnitExtensions;
using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
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

        [ConditionalFact]
        [PlatformSpecific(TestPlatforms.Linux)]
        public void SafeFileHandle_PseudoFile_DoesNotThrow()
        {
            // On some Linux distributions (e.g., AzureLinux 3), pseudofiles may report CanSeek = true
            // but fail when attempting to seek. Accessing SafeFileHandle should not throw in these cases.
            string path = File.Exists("/proc/net/route")
                ? "/proc/net/route" 
                : File.Exists("/proc/version")
                    ? "/proc/version"
                    : throw new SkipTestException("Can't find a pseudofile to test.");

            using FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            // This should not throw even if the file reports CanSeek = true but doesn't support seeking
            SafeFileHandle handle = fs.SafeFileHandle;
            
            Assert.NotNull(handle);
            Assert.False(handle.IsClosed);
        }
    }
}
