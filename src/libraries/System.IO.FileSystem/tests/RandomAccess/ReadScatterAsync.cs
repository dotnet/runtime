// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    public class RandomAccess_ReadScatterAsync : RandomAccess_Base<ValueTask<long>>
    {
        protected override ValueTask<long> MethodUnderTest(SafeFileHandle handle, byte[] bytes, long fileOffset)
            => RandomAccess.ReadAsync(handle, new Memory<byte>[] { bytes }, fileOffset);

        protected override bool ShouldThrowForSyncHandle
            => OperatingSystem.IsWindows(); // on Windows we can NOT perform async IO using sync handle

        [Fact]
        public void ThrowsArgumentNullExceptionForNullBuffers()
        {
            using (SafeFileHandle handle = File.OpenHandle(GetTestFilePath(), FileMode.CreateNew, FileAccess.Write, options: FileOptions.Asynchronous))
            {
                AssertExtensions.Throws<ArgumentNullException>("buffers", () => RandomAccess.ReadAsync(handle, buffers: null, 0));
            }
        }

        [Fact]
        public async Task TaskAlreadyCanceledAsync()
        {
            using (SafeFileHandle handle = File.OpenHandle(GetTestFilePath(), FileMode.CreateNew, FileAccess.ReadWrite, options: FileOptions.Asynchronous))
            {
                CancellationTokenSource cts = GetCancelledTokenSource();
                CancellationToken token = cts.Token;

                Assert.True(RandomAccess.ReadAsync(handle, new Memory<byte>[] { new byte[1] }, 0, token).IsCanceled);

                TaskCanceledException ex = await Assert.ThrowsAsync<TaskCanceledException>(() => RandomAccess.ReadAsync(handle, new Memory<byte>[] { new byte[1] }, 0, token).AsTask());
                Assert.Equal(token, ex.CancellationToken);
            }
        }

        [Fact]
        public async Task ThrowsOnWriteAccess()
        {
            using (SafeFileHandle handle = GetHandleToExistingFile(FileAccess.Write))
            {
                await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await RandomAccess.ReadAsync(handle, new Memory<byte>[] { new byte[1] }, 0));
            }
        }

        [Fact]
        public async Task ReadToAnEmptyBufferReturnsZeroAsync()
        {
            string filePath = GetTestFilePath();
            File.WriteAllBytes(filePath, new byte[1]);

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.Open, options: FileOptions.Asynchronous))
            {
                Assert.Equal(0, await RandomAccess.ReadAsync(handle, new Memory<byte>[] { Array.Empty<byte>() }, fileOffset: 0));
            }
        }

        [Fact]
        public async Task ReadsBytesFromGivenFileAtGivenOffsetAsync()
        {
            const int fileSize = 4_001;
            string filePath = GetTestFilePath();
            byte[] expected = RandomNumberGenerator.GetBytes(fileSize);
            File.WriteAllBytes(filePath, expected);

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.Open, options: FileOptions.Asynchronous))
            {
                byte[] actual = new byte[fileSize + 1];
                long current = 0;
                long total = 0;

                do
                {
                    int firstBufferLength = (int)Math.Min(actual.Length - total, fileSize / 4);
                    Memory<byte> buffer_1 = actual.AsMemory((int)total, firstBufferLength);
                    Memory<byte> buffer_2 = actual.AsMemory((int)total + firstBufferLength);

                    current = await RandomAccess.ReadAsync(
                        handle,
                        new Memory<byte>[]
                        {
                            buffer_1,
                            buffer_2
                        },
                        fileOffset: total);

                    Assert.InRange(current, 0, buffer_1.Length + buffer_2.Length);

                    total += current;
                } while (current != 0);

                Assert.Equal(fileSize, total);
                Assert.Equal(expected, actual.Take((int)total).ToArray());
            }
        }

        [Fact]
        public async Task ReadToTheSameBufferOverwritesContent()
        {
            string filePath = GetTestFilePath();
            File.WriteAllBytes(filePath, new byte[3] { 1, 2, 3 });

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.Open, options: FileOptions.Asynchronous))
            {
                byte[] buffer = new byte[1];
                Assert.Equal(buffer.Length + buffer.Length, await RandomAccess.ReadAsync(handle, Enumerable.Repeat(buffer.AsMemory(), 2).ToList(), fileOffset: 0));
                Assert.Equal(2, buffer[0]);
            }
        }
    }
}
