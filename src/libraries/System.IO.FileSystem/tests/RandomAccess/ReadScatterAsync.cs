// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Tests
{
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
                ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => RandomAccess.ReadAsync(handle, buffers: null, 0));
                Assert.Equal("buffers", ex.ParamName);
            }
        }

        [Fact]
        public Task TaskAlreadyCanceledAsync()
        {
            using (SafeFileHandle handle = File.OpenHandle(GetTestFilePath(), FileMode.CreateNew, FileAccess.ReadWrite, options: FileOptions.Asynchronous))
            {
                CancellationToken token = GetCancelledToken();
                Assert.True(RandomAccess.ReadAsync(handle, new Memory<byte>[] { new byte[1] }, 0, token).IsCanceled);
                return Assert.ThrowsAsync<TaskCanceledException>(async () => await RandomAccess.ReadAsync(handle, new Memory<byte>[] { new byte[1] }, 0, token));
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
        public async Task HappyPath()
        {
            const int fileSize = 4_001;
            string filePath = GetTestFilePath();
            byte[] expected = new byte[fileSize];
            new Random().NextBytes(expected);
            File.WriteAllBytes(filePath, expected);

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.Open, options: FileOptions.Asynchronous))
            {
                byte[] actual = new byte[fileSize + 1];
                long current = 0;
                long total = 0;

                do
                {
                    int firstBufferLength = (int)Math.Min(actual.Length - total, fileSize / 4);

                    current = await RandomAccess.ReadAsync(
                        handle,
                        new Memory<byte>[]
                        {
                            actual.AsMemory((int)total, firstBufferLength),
                            actual.AsMemory((int)total + firstBufferLength)
                        },
                        fileOffset: total);

                    total += current;
                } while (current != 0);

                Assert.Equal(fileSize, total);
                Assert.Equal(expected, actual.Take((int)total).ToArray());
            }
        }
    }
}
