// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/34582", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
    [SkipOnPlatform(TestPlatforms.Browser, "async file IO is not supported on browser")]
    public class RandomAccess_WriteAsync : RandomAccess_Base<ValueTask<int>>
    {
        protected override ValueTask<int> MethodUnderTest(SafeFileHandle handle, byte[] bytes, long fileOffset)
            => RandomAccess.WriteAsync(handle, bytes, fileOffset);

        protected override bool ShouldThrowForSyncHandle
            => OperatingSystem.IsWindows(); // on Windows we can NOT perform async IO using sync handle

        [Fact]
        public async Task TaskAlreadyCanceledAsync()
        {
            using (SafeFileHandle handle = File.OpenHandle(GetTestFilePath(), FileMode.CreateNew, FileAccess.ReadWrite, options: FileOptions.Asynchronous))
            {
                CancellationTokenSource cts = GetCancelledTokenSource();
                CancellationToken token = cts.Token;

                Assert.True(RandomAccess.WriteAsync(handle, new byte[1], 0, token).IsCanceled);

                TaskCanceledException ex = await Assert.ThrowsAsync<TaskCanceledException>(() => RandomAccess.WriteAsync(handle, new byte[1], 0, token).AsTask());
                Assert.Equal(token, ex.CancellationToken);
            }
        }

        [Fact]
        public async Task ThrowsOnReadAccess()
        {
            using (SafeFileHandle handle = GetHandleToExistingFile(FileAccess.Read))
            {
                await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await RandomAccess.WriteAsync(handle, new byte[1], 0));
            }
        }

        [Fact]
        public async Task WriteUsingEmptyBufferReturnsZeroAsync()
        {
            using (SafeFileHandle handle = File.OpenHandle(GetTestFilePath(), FileMode.Create, FileAccess.Write, options: FileOptions.Asynchronous))
            {
                Assert.Equal(0, await RandomAccess.WriteAsync(handle, Array.Empty<byte>(), fileOffset: 0));
            }
        }

        [Fact]
        public async Task WritesBytesFromGivenBufferToGivenFileAtGivenOffsetAsync()
        {
            const int fileSize = 4_001;
            string filePath = GetTestFilePath();
            byte[] content = RandomNumberGenerator.GetBytes(fileSize);

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, FileOptions.Asynchronous))
            {
                int total = 0;
                int current = 0;

                while (total != fileSize)
                {
                    Memory<byte> buffer = content.AsMemory(total, Math.Min(content.Length - total, fileSize / 4));

                    current = await RandomAccess.WriteAsync(handle, buffer, fileOffset: total);

                    Assert.InRange(current, 0, buffer.Length);

                    total += current;
                }
            }

            Assert.Equal(content, File.ReadAllBytes(filePath));
        }
    }
}
