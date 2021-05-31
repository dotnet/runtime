// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipes;
using System.Linq;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Tests
{
    public class RandomAccess_Read : FileSystemTest
    {
        [Fact]
        public void ThrowsArgumentNullExceptionForNullHandle()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => RandomAccess.Read(null, Array.Empty<byte>(), 0));
            Assert.Equal("handle", ex.ParamName);
        }

        [Fact]
        public void ThrowsArgumentExceptionForInvalidHandle()
        {
            SafeFileHandle handle = new SafeFileHandle(new IntPtr(-1), ownsHandle: false);

            ArgumentException ex = Assert.Throws<ArgumentException>(() => RandomAccess.Read(handle, Array.Empty<byte>(), 0));
            Assert.Equal("handle", ex.ParamName);
        }

        [Fact]
        public void ThrowsObjectDisposedExceptionForDisposedHandle()
        {
            SafeFileHandle handle = File.OpenHandle(GetTestFilePath(), FileMode.Create, FileAccess.Write);
            handle.Dispose();

            Assert.Throws<ObjectDisposedException>(() => RandomAccess.Read(handle, Array.Empty<byte>(), 0));
        }

        [Fact]
        public void ThrowsNotSupportedExceptionForUnseekableFile()
        {
            using (var server = new AnonymousPipeServerStream(PipeDirection.Out))
            using (SafeFileHandle pipeHandle = new SafeFileHandle(server.SafePipeHandle.DangerousGetHandle(), true))
            {
                Assert.Throws<NotSupportedException>(() => RandomAccess.Read(pipeHandle, Array.Empty<byte>(), 0));
            }
        }

        [Fact]
        public void ThrowsArgumentOutOfRangeExceptionForNegativeFileOffset()
        {
            using (SafeFileHandle validHandle = File.OpenHandle(GetTestFilePath(), FileMode.CreateNew, FileAccess.Write))
            {
                ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => RandomAccess.Read(validHandle, Array.Empty<byte>(), -1));
                Assert.Equal("fileOffset", ex.ParamName);
            }
        }

        [Fact]
        public void ThrowsArgumentExceptionForAsyncFileHandle()
        {
            using (SafeFileHandle validHandle = File.OpenHandle(GetTestFilePath(), FileMode.CreateNew, FileAccess.Write, options: FileOptions.Asynchronous))
            {
                ArgumentException ex = Assert.Throws<ArgumentException>(() => RandomAccess.Read(validHandle, new byte[100], 0));
                Assert.Equal("handle", ex.ParamName);
            }
        }

        [Fact]
        public void SupportsPartialReads()
        {
            const int fileSize = 4_001;
            string filePath = GetTestFilePath();
            byte[] expected = new byte[fileSize];
            new Random().NextBytes(expected);
            File.WriteAllBytes(filePath, expected);

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.Open))
            {
                byte[] actual = new byte[fileSize + 1];
                int current = 0;
                int total = 0;

                do
                {
                    current = RandomAccess.Read(
                        handle,
                        actual.AsSpan(total, Math.Min(actual.Length - total, fileSize / 4)),
                        fileOffset: total);

                    total += current;
                } while (current != 0);

                Assert.Equal(fileSize, total);
                Assert.Equal(expected, actual.Take(total).ToArray());
            }
        }
    }
}
