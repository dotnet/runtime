// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Tests
{
    public class RandomAccess_WriteGatherAsync : RandomAccess_Base<ValueTask<long>>
    {
        protected override ValueTask<long> MethodUnderTest(SafeFileHandle handle, byte[] bytes, long fileOffset)
            => RandomAccess.WriteAsync(handle, new ReadOnlyMemory<byte>[] { bytes }, fileOffset);

        protected override bool ShouldThrowForSyncHandle
            => OperatingSystem.IsWindows(); // on Windows we can NOT perform async IO using sync handle

        [Fact]
        public void ThrowsArgumentNullExceptionForNullBuffers()
        {
            using (SafeFileHandle handle = File.OpenHandle(GetTestFilePath(), FileMode.CreateNew, FileAccess.Write, FileShare.None, FileOptions.Asynchronous))
            {
                ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => RandomAccess.WriteAsync(handle, buffers: null, 0));
                Assert.Equal("buffers", ex.ParamName);
            }
        }

        [Fact]
        public async Task HappyPath()
        {
            const int fileSize = 4_001;
            string filePath = GetTestFilePath();
            byte[] content = new byte[fileSize];
            new Random().NextBytes(content);

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, FileOptions.Asynchronous))
            {
                long total = 0;

                while (total != fileSize)
                {
                    int firstBufferLength = (int)Math.Min(content.Length - total, fileSize / 4);

                    total += await RandomAccess.WriteAsync(
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
