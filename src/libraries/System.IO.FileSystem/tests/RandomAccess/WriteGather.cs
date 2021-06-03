// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Tests
{
    public class RandomAccess_WriteGather : RandomAccess_Base<long>
    {
        protected override long MethodUnderTest(SafeFileHandle handle, byte[] bytes, long fileOffset)
            => RandomAccess.Write(handle, new ReadOnlyMemory<byte>[] { bytes }, fileOffset);

        protected override bool ShouldThrowForAsyncHandle
            => OperatingSystem.IsWindows(); // on Windows we can NOT perform sync IO using async handle

        [Fact]
        public void ThrowsArgumentNullExceptionForNullBuffers()
        {
            using (SafeFileHandle handle = File.OpenHandle(GetTestFilePath(), FileMode.CreateNew, FileAccess.Write))
            {
                ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => RandomAccess.Write(handle, buffers: null, 0));
                Assert.Equal("buffers", ex.ParamName);
            }
        }

        [Fact]
        public void ThrowsOnReadAccess()
        {
            using (SafeFileHandle handle = GetHandleToExistingFile(FileAccess.Read))
            {
                Assert.Throws<UnauthorizedAccessException>(() => RandomAccess.Write(handle, new ReadOnlyMemory<byte>[] { new byte[1] }, 0));
            }
        }

        [Fact]
        public void HappyPath()
        {
            const int fileSize = 4_001;
            string filePath = GetTestFilePath();
            byte[] content = new byte[fileSize];
            new Random().NextBytes(content);

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                long total = 0;

                while (total != fileSize)
                {
                    int firstBufferLength = (int)Math.Min(content.Length - total, fileSize / 4);

                    total += RandomAccess.Write(
                        handle,
                        new ReadOnlyMemory<byte>[]
                        {
                            content.AsMemory((int)total, firstBufferLength),
                            content.AsMemory((int)total + firstBufferLength)
                        },
                        fileOffset: total);
                }
            }

            Assert.Equal(content, File.ReadAllBytes(filePath));
        }
    }
}
