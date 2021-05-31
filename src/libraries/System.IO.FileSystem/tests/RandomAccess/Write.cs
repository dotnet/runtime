// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipes;
using System.Linq;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Tests
{
    public class RandomAccess_Write : FileSystemTest
    {
        [Fact]
        public void ThrowsArgumentNullExceptionForNullHandle()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => RandomAccess.Write(null, Array.Empty<byte>(), 0));
            Assert.Equal("handle", ex.ParamName);
        }

        [Fact]
        public void ThrowsArgumentExceptionForInvalidHandle()
        {
            SafeFileHandle handle = new SafeFileHandle(new IntPtr(-1), ownsHandle: false);

            ArgumentException ex = Assert.Throws<ArgumentException>(() => RandomAccess.Write(handle, Array.Empty<byte>(), 0));
            Assert.Equal("handle", ex.ParamName);
        }

        [Fact]
        public void ThrowsObjectDisposedExceptionForDisposedHandle()
        {
            SafeFileHandle handle = File.OpenHandle(GetTestFilePath(), FileMode.Create, FileAccess.Write);
            handle.Dispose();

            Assert.Throws<ObjectDisposedException>(() => RandomAccess.Write(handle, Array.Empty<byte>(), 0));
        }

        [Fact]
        public void ThrowsNotSupportedExceptionForUnseekableFile()
        {
            using (var server = new AnonymousPipeServerStream(PipeDirection.Out))
            using (SafeFileHandle pipeHandle = new SafeFileHandle(server.SafePipeHandle.DangerousGetHandle(), true))
            {
                Assert.Throws<NotSupportedException>(() => RandomAccess.Write(pipeHandle, Array.Empty<byte>(), 0));
            }
        }

        [Fact]
        public void ThrowsArgumentOutOfRangeExceptionForNegativeFileOffset()
        {
            using (SafeFileHandle validHandle = File.OpenHandle(GetTestFilePath(), FileMode.CreateNew, FileAccess.Write))
            {
                ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => RandomAccess.Write(validHandle, Array.Empty<byte>(), -1));
                Assert.Equal("fileOffset", ex.ParamName);
            }
        }

        [Fact]
        public void ThrowsArgumentExceptionForAsyncFileHandle()
        {
            using (SafeFileHandle validHandle = File.OpenHandle(GetTestFilePath(), FileMode.CreateNew, FileAccess.Write, options: FileOptions.Asynchronous))
            {
                ArgumentException ex = Assert.Throws<ArgumentException>(() => RandomAccess.Write(validHandle, new byte[100], 0));
                Assert.Equal("handle", ex.ParamName);
            }
        }

        [Fact]
        public void SupportsPartialWrites()
        {
            const int fileSize = 4_001;
            string filePath = GetTestFilePath();
            byte[] content = new byte[fileSize];
            new Random().NextBytes(content);

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                int total = 0;

                while (total != fileSize)
                {
                    total += RandomAccess.Write(
                        handle,
                        content.AsSpan(total, Math.Min(content.Length - total, fileSize / 4)),
                        fileOffset: total);
                }
            }

            Assert.Equal(content, File.ReadAllBytes(filePath));
        }
    }
}
