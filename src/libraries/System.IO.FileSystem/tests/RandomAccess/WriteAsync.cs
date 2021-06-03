// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Tests
{
    public class RandomAccess_WriteAsync : RandomAccess_Base<ValueTask<int>>
    {
        protected override ValueTask<int> MethodUnderTest(SafeFileHandle handle, byte[] bytes, long fileOffset)
            => RandomAccess.WriteAsync(handle, bytes, fileOffset);

        protected override bool ShouldThrowForSyncHandle
            => OperatingSystem.IsWindows(); // on Windows we can NOT perform async IO using sync handle

        [Fact]
        public Task TaskAlreadyCanceledAsync()
        {
            using (SafeFileHandle handle = File.OpenHandle(GetTestFilePath(), FileMode.CreateNew, FileAccess.ReadWrite, options: FileOptions.Asynchronous))
            {
                CancellationToken token = GetCancelledToken();
                Assert.True(RandomAccess.WriteAsync(handle, new byte[1], 0, token).IsCanceled);
                return Assert.ThrowsAsync<TaskCanceledException>(async () => await RandomAccess.WriteAsync(handle, new byte[1], 0, token));
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
                int total = 0;

                while (total != fileSize)
                {
                    total += await RandomAccess.WriteAsync(
                        handle,
                        content.AsMemory(total, Math.Min(content.Length - total, fileSize / 4)),
                        fileOffset: total);
                }
            }

            Assert.Equal(content, File.ReadAllBytes(filePath));
        }
    }
}
