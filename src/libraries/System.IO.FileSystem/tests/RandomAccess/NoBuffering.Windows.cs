// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/34582", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
    [SkipOnPlatform(TestPlatforms.Browser, "async file IO is not supported on browser")]
    public class RandomAccess_NoBuffering : FileSystemTest
    {
        private const FileOptions NoBuffering = (FileOptions)0x20000000;

        public static IEnumerable<object[]> AllAsyncSyncCombinations()
        {
            yield return new object[] { false, false };
            yield return new object[] { false, true };
            yield return new object[] { true, true };
            yield return new object[] { true, false };
        }

        [Theory]
        [MemberData(nameof(AllAsyncSyncCombinations))]
        public async Task ReadUsingSingleBuffer(bool asyncOperation, bool asyncHandle)
        {
            const int fileSize = 1_000_000; // 1 MB
            string filePath = GetTestFilePath();
            byte[] expected = RandomNumberGenerator.GetBytes(fileSize);
            File.WriteAllBytes(filePath, expected);

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.Open, options: GetFileOptions(asyncHandle)))
            using (SectorAlignedMemory<byte> buffer = SectorAlignedMemory<byte>.Allocate(Environment.SystemPageSize))
            {
                int current = 0;
                int total = 0;

                // From https://docs.microsoft.com/en-us/windows/win32/fileio/file-buffering:
                // "File access sizes, including the optional file offset in the OVERLAPPED structure,
                // if specified, must be for a number of bytes that is an integer multiple of the volume sector size."
                // So if buffer and physical sector size is 4096 and the file size is 4097:
                // the read from offset=0 reads 4096 bytes
                // the read from offset=4096 reads 1 byte
                // the read from offset=4097 THROWS (Invalid argument, offset is not a multiple of sector size!)
                // That is why we stop at the first incomplete read (the next one would throw).
                // It's possible to get 0 if we are lucky and file size is a multiple of physical sector size.
                do
                {
                    current = asyncOperation
                        ? await RandomAccess.ReadAsync(handle, buffer.Memory, fileOffset: total)
                        : RandomAccess.Read(handle, buffer.GetSpan(), fileOffset: total);

                    Assert.True(expected.AsSpan(total, current).SequenceEqual(buffer.GetSpan().Slice(0, current)));

                    total += current;
                }
                while (current == buffer.Memory.Length);

                Assert.Equal(fileSize, total);
            }
        }

        [Theory]
        [MemberData(nameof(AllAsyncSyncCombinations))]
        public async Task ReadAsyncUsingMultipleBuffers(bool asyncOperation, bool asyncHandle)
        {
            const int fileSize = 1_000_000; // 1 MB
            string filePath = GetTestFilePath();
            byte[] expected = RandomNumberGenerator.GetBytes(fileSize);
            File.WriteAllBytes(filePath, expected);

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.Open, options: GetFileOptions(asyncHandle)))
            using (SectorAlignedMemory<byte> buffer_1 = SectorAlignedMemory<byte>.Allocate(Environment.SystemPageSize))
            using (SectorAlignedMemory<byte> buffer_2 = SectorAlignedMemory<byte>.Allocate(Environment.SystemPageSize))
            {
                long current = 0;
                long total = 0;

                IReadOnlyList<Memory<byte>> buffers = new Memory<byte>[]
                {
                    buffer_1.Memory,
                    buffer_2.Memory,
                };

                do
                {
                    current = asyncOperation
                        ? await RandomAccess.ReadAsync(handle, buffers, fileOffset: total)
                        : RandomAccess.Read(handle, buffers, fileOffset: total);

                    int takeFromFirst = Math.Min(buffer_1.Memory.Length, (int)current);
                    Assert.True(expected.AsSpan((int)total, takeFromFirst).SequenceEqual(buffer_1.GetSpan().Slice(0, takeFromFirst)));
                    int takeFromSecond = (int)current - takeFromFirst;
                    Assert.True(expected.AsSpan((int)total + takeFromFirst, takeFromSecond).SequenceEqual(buffer_2.GetSpan().Slice(0, takeFromSecond)));

                    total += current;
                } while (current == buffer_1.Memory.Length + buffer_2.Memory.Length);

                Assert.Equal(fileSize, total);
            }
        }

        [Theory]
        [MemberData(nameof(AllAsyncSyncCombinations))]
        public async Task WriteUsingSingleBuffer(bool asyncOperation, bool asyncHandle)
        {
            string filePath = GetTestFilePath();
            int bufferSize = Environment.SystemPageSize;
            int fileSize = bufferSize * 10;
            byte[] content = RandomNumberGenerator.GetBytes(fileSize);

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, GetFileOptions(asyncHandle)))
            using (SectorAlignedMemory<byte> buffer = SectorAlignedMemory<byte>.Allocate(bufferSize))
            {
                int total = 0;

                while (total != fileSize)
                {
                    int take = Math.Min(content.Length - total, bufferSize);
                    content.AsSpan(total, take).CopyTo(buffer.GetSpan());

                    if (asyncOperation)
                    {
                        await RandomAccess.WriteAsync(handle, buffer.Memory, fileOffset: total);
                    }
                    else
                    {
                        RandomAccess.Write(handle, buffer.GetSpan(), fileOffset: total);
                    }

                    total += buffer.Memory.Length;
                }
            }

            Assert.Equal(content, File.ReadAllBytes(filePath));
        }

        [Theory]
        [MemberData(nameof(AllAsyncSyncCombinations))]
        public async Task WriteAsyncUsingMultipleBuffers(bool asyncOperation, bool asyncHandle)
        {
            string filePath = GetTestFilePath();
            int bufferSize = Environment.SystemPageSize;
            int fileSize = bufferSize * 10;
            byte[] content = RandomNumberGenerator.GetBytes(fileSize);

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, GetFileOptions(asyncHandle)))
            using (SectorAlignedMemory<byte> buffer_1 = SectorAlignedMemory<byte>.Allocate(bufferSize))
            using (SectorAlignedMemory<byte> buffer_2 = SectorAlignedMemory<byte>.Allocate(bufferSize))
            {
                long total = 0;

                IReadOnlyList<ReadOnlyMemory<byte>> buffers = new ReadOnlyMemory<byte>[]
                {
                    buffer_1.Memory,
                    buffer_2.Memory,
                };

                while (total != fileSize)
                {
                    content.AsSpan((int)total, bufferSize).CopyTo(buffer_1.GetSpan());
                    content.AsSpan((int)total + bufferSize, bufferSize).CopyTo(buffer_2.GetSpan());

                    if (asyncOperation)
                    {
                        await RandomAccess.WriteAsync(handle, buffers, fileOffset: total);
                    }
                    else
                    {
                        RandomAccess.Write(handle, buffers, fileOffset: total);
                    }

                    total += buffer_1.Memory.Length + buffer_2.Memory.Length;
                }
            }

            Assert.Equal(content, File.ReadAllBytes(filePath));
        }

        [Fact]
        public async Task ReadWriteAsyncUsingNonPageSizedMultipleBuffers()
        {
            string filePath = GetTestFilePath();
            // The Windows scatter/gather APIs accept segments that are exactly one page long.
            // Combined with the FILE_FLAG_NO_BUFFERING's requirements, the segments must also
            // be aligned at page size boundaries and have a size of a multiple of the page size.
            // Using segments with a length of twice the page size adheres to the second requirement
            // but not the first. The RandomAccess implementation will see it and issue sequential
            // read/write syscalls per segment, instead of one scatter/gather syscall.
            // This test verifies that fallback behavior.
            int bufferSize = Environment.SystemPageSize * 2;
            int fileSize = bufferSize * 2;
            byte[] content = RandomNumberGenerator.GetBytes(fileSize);

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, FileOptions.Asynchronous | NoBuffering))
            using (SectorAlignedMemory<byte> buffer = SectorAlignedMemory<byte>.Allocate(fileSize))
            {
                Memory<byte> firstHalf = buffer.Memory.Slice(0, bufferSize);
                Memory<byte> secondHalf = buffer.Memory.Slice(bufferSize);

                content.AsSpan().CopyTo(buffer.GetSpan());
                await RandomAccess.WriteAsync(handle, new ReadOnlyMemory<byte>[] { firstHalf, secondHalf }, 0);

                buffer.GetSpan().Clear();
                await RandomAccess.ReadAsync(handle, new Memory<byte>[] { firstHalf, secondHalf }, 0);
            }

            Assert.Equal(content, await File.ReadAllBytesAsync(filePath));
        }

        // when using FileOptions.Asynchronous we are testing Scatter&Gather APIs on Windows (FILE_FLAG_OVERLAPPED requirement)
        private static FileOptions GetFileOptions(bool asyncHandle) => (asyncHandle ? FileOptions.Asynchronous : FileOptions.None) | NoBuffering; 
    }
}
