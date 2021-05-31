// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipes;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Tests
{
    public class RandomAccess_GetLength : FileSystemTest
    {
        [Fact]
        public void ThrowsArgumentNullExceptionForNullHandle()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => RandomAccess.GetLength(null));
            Assert.Equal("handle", ex.ParamName);
        }

        [Fact]
        public void ThrowsArgumentExceptionForInvalidHandle()
        {
            SafeFileHandle handle = new SafeFileHandle(new IntPtr(-1), ownsHandle: false);

            ArgumentException ex = Assert.Throws<ArgumentException>(() => RandomAccess.GetLength(handle));
            Assert.Equal("handle", ex.ParamName);
        }

        [Fact]
        public void ThrowsObjectDisposedExceptionForDisposedHandle()
        {
            SafeFileHandle handle = File.OpenHandle(GetTestFilePath(), FileMode.Create, FileAccess.Write);
            handle.Dispose();

            Assert.Throws<ObjectDisposedException>(() => RandomAccess.GetLength(handle));
        }

        [Fact]
        public void ThrowsNotSupportedExceptionForUnseekableFile()
        {
            using (var server = new AnonymousPipeServerStream(PipeDirection.Out))
            using (SafeFileHandle handle = new SafeFileHandle(server.SafePipeHandle.DangerousGetHandle(), true))
            {
                Assert.Throws<NotSupportedException>(() => RandomAccess.GetLength(handle));
            }   
        }

        [Fact]
        public void ReturnsZeroForEmptyFile()
        {
            using (SafeFileHandle handle = File.OpenHandle(GetTestFilePath(), FileMode.CreateNew, FileAccess.Write))
            {
                Assert.Equal(0, RandomAccess.GetLength(handle));
            }
        }

        [Fact]
        public void ReturnsExactSizeForNonEmptyFiles()
        {
            const int fileSize = 123;
            string filePath = GetTestFilePath();
            File.WriteAllBytes(filePath, new byte[fileSize]);

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.Open))
            {
                Assert.Equal(fileSize, RandomAccess.GetLength(handle));
            }
        }
    }
}
