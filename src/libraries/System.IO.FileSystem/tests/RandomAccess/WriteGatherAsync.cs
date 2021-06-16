// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/34582", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
    [SkipOnPlatform(TestPlatforms.Browser, "async file IO is not supported on browser")]
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
                AssertExtensions.Throws<ArgumentNullException>("buffers", () => RandomAccess.WriteAsync(handle, buffers: null, 0));
            }
        }

        [Fact]
        public async Task TaskAlreadyCanceledAsync()
        {
            using (SafeFileHandle handle = File.OpenHandle(GetTestFilePath(), FileMode.CreateNew, FileAccess.ReadWrite, options: FileOptions.Asynchronous))
            {
                CancellationTokenSource cts = GetCancelledTokenSource();
                CancellationToken token = cts.Token;

                Assert.True(RandomAccess.WriteAsync(handle, new ReadOnlyMemory<byte>[] { new byte[1] }, 0, token).IsCanceled);

                TaskCanceledException ex = await Assert.ThrowsAsync<TaskCanceledException>(() => RandomAccess.WriteAsync(handle, new ReadOnlyMemory<byte>[] { new byte[1] }, 0, token).AsTask());
                Assert.Equal(token, ex.CancellationToken);
            }
        }

        [Fact]
        public async Task ThrowsOnReadAccess()
        {
            using (SafeFileHandle handle = GetHandleToExistingFile(FileAccess.Read))
            {
                await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await RandomAccess.WriteAsync(handle, new ReadOnlyMemory<byte>[] { new byte[1] }, 0));
            }
        }

        [Fact]
        public async Task WriteUsingEmptyBufferReturnsZeroAsync()
        {
            using (SafeFileHandle handle = File.OpenHandle(GetTestFilePath(), FileMode.Create, FileAccess.Write, options: FileOptions.Asynchronous))
            {
                Assert.Equal(0, await RandomAccess.WriteAsync(handle, new ReadOnlyMemory<byte>[] { Array.Empty<byte>() }, fileOffset: 0));
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
                long total = 0;
                long current = 0;

                while (total != fileSize)
                {
                    int firstBufferLength = (int)Math.Min(content.Length - total, fileSize / 4);
                    Memory<byte> buffer_1 = content.AsMemory((int)total, firstBufferLength);
                    Memory<byte> buffer_2 = content.AsMemory((int)total + firstBufferLength);

                    current = await RandomAccess.WriteAsync(
                        handle,
                        new ReadOnlyMemory<byte>[]
                        {
                            buffer_1,
                            buffer_2
                        },
                        fileOffset: total);

                    Assert.InRange(current, 0, buffer_1.Length + buffer_2.Length);

                    total += current;
                }
            }

            Assert.Equal(content, File.ReadAllBytes(filePath));
        }

        [Fact]
        public async Task DuplicatedBufferDuplicatesContentAsync()
        {
            const byte value = 1;
            const int repeatCount = 2;
            string filePath = GetTestFilePath();
            ReadOnlyMemory<byte> buffer = new byte[1] { value };
            List<ReadOnlyMemory<byte>> buffers = Enumerable.Repeat(buffer, repeatCount).ToList();

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.Create, FileAccess.Write, options: FileOptions.Asynchronous))
            {
                Assert.Equal(repeatCount, await RandomAccess.WriteAsync(handle, buffers, fileOffset: 0));
            }

            byte[] actualContent = File.ReadAllBytes(filePath);
            Assert.Equal(repeatCount, actualContent.Length);
            Assert.All(actualContent, actual => Assert.Equal(value, actual));
        }
    }
}
