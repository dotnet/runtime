// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.XUnitExtensions;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "async file IO is not supported on browser")]
    public class RandomAccess_WriteGatherAsync : RandomAccess_Base<ValueTask>
    {
        protected override ValueTask MethodUnderTest(SafeFileHandle handle, byte[] bytes, long fileOffset)
            => RandomAccess.WriteAsync(handle, new ReadOnlyMemory<byte>[] { bytes }, fileOffset);

        [Theory]
        [MemberData(nameof(GetSyncAsyncOptions))]
        public void ThrowsArgumentNullExceptionForNullBuffers(FileOptions options)
        {
            using (SafeFileHandle handle = File.OpenHandle(GetTestFilePath(), FileMode.CreateNew, FileAccess.Write, FileShare.None, options))
            {
                AssertExtensions.Throws<ArgumentNullException>("buffers", () => RandomAccess.WriteAsync(handle, buffers: null, 0));
            }
        }

        [Theory]
        [MemberData(nameof(GetSyncAsyncOptions))]
        public async Task TaskAlreadyCanceledAsync(FileOptions options)
        {
            using (SafeFileHandle handle = File.OpenHandle(GetTestFilePath(), FileMode.CreateNew, FileAccess.ReadWrite, options: options))
            {
                CancellationTokenSource cts = GetCancelledTokenSource();
                CancellationToken token = cts.Token;

                Assert.True(RandomAccess.WriteAsync(handle, new ReadOnlyMemory<byte>[] { new byte[1] }, 0, token).IsCanceled);

                TaskCanceledException ex = await Assert.ThrowsAsync<TaskCanceledException>(() => RandomAccess.WriteAsync(handle, new ReadOnlyMemory<byte>[] { new byte[1] }, 0, token).AsTask());
                Assert.Equal(token, ex.CancellationToken);
            }
        }

        [Theory]
        [MemberData(nameof(GetSyncAsyncOptions))]
        public async Task ThrowsOnReadAccess(FileOptions options)
        {
            using (SafeFileHandle handle = GetHandleToExistingFile(FileAccess.Read, options))
            {
                await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await RandomAccess.WriteAsync(handle, new ReadOnlyMemory<byte>[] { new byte[1] }, 0));
            }
        }

        [Theory]
        [MemberData(nameof(GetSyncAsyncOptions))]
        public async Task WriteUsingEmptyBufferReturnsAsync(FileOptions options)
        {
            using (SafeFileHandle handle = File.OpenHandle(GetTestFilePath(), FileMode.Create, FileAccess.Write, options: options))
            {
                await RandomAccess.WriteAsync(handle, new ReadOnlyMemory<byte>[] { Array.Empty<byte>() }, fileOffset: 0);
            }
        }

        [Theory]
        [MemberData(nameof(GetSyncAsyncOptions))]
        public async Task WriteBeyondEndOfFileExtendsTheFileAsync(FileOptions options)
        {
            string filePath = GetTestFilePath();

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.CreateNew, FileAccess.Write, options: options))
            {
                Assert.Equal(0, RandomAccess.GetLength(handle));
                await RandomAccess.WriteAsync(handle, new ReadOnlyMemory<byte>[] { new byte[1] { 1 } }, fileOffset: 1);
                Assert.Equal(2, RandomAccess.GetLength(handle));
            }

            Assert.Equal(new byte[] { 0, 1 }, await File.ReadAllBytesAsync(filePath));
        }

        [Theory]
        [MemberData(nameof(GetSyncAsyncOptions))]
        public async Task WritesBytesFromGivenBufferToGivenFileAtGivenOffsetAsync(FileOptions options)
        {
            const int fileSize = 4_001;
            string filePath = GetTestFilePath();
            byte[] content = RandomNumberGenerator.GetBytes(fileSize);

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, options))
            {
                long total = 0;

                while (total != fileSize)
                {
                    int firstBufferLength = (int)Math.Min(content.Length - total, fileSize / 4);
                    Memory<byte> buffer_1 = content.AsMemory((int)total, firstBufferLength);
                    Memory<byte> buffer_2 = content.AsMemory((int)total + firstBufferLength);

                    await RandomAccess.WriteAsync(
                        handle,
                        new ReadOnlyMemory<byte>[]
                        {
                            buffer_1,
                            Array.Empty<byte>(),
                            buffer_2
                        },
                        fileOffset: total);

                    total += buffer_1.Length + buffer_2.Length;
                }
            }

            Assert.Equal(content, File.ReadAllBytes(filePath));
        }

        [Theory]
        [MemberData(nameof(GetSyncAsyncOptions))]
        public async Task DuplicatedBufferDuplicatesContentAsync(FileOptions options)
        {
            const byte value = 1;
            const int repeatCount = 2;
            string filePath = GetTestFilePath();
            ReadOnlyMemory<byte> buffer = new byte[1] { value };
            List<ReadOnlyMemory<byte>> buffers = Enumerable.Repeat(buffer, repeatCount).ToList();

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.Create, FileAccess.Write, options: options))
            {
                await RandomAccess.WriteAsync(handle, buffers, fileOffset: 0);
            }

            byte[] actualContent = File.ReadAllBytes(filePath);
            Assert.Equal(repeatCount, actualContent.Length);
            Assert.All(actualContent, actual => Assert.Equal(value, actual));
        }

        [OuterLoop("It consumes a lot of resources (disk space and memory).")]
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess), nameof(PlatformDetection.IsReleaseRuntime))]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        [InlineData(true, false)]
        public async Task NoInt32OverflowForLargeInputs(bool asyncFile, bool asyncMethod)
        {
            // We need to write more than Int32.MaxValue bytes to the disk to reproduce the problem.
            // To reduce the number of used memory, we allocate only one write buffer and simply repeat it multiple times.
            // For reading, we need unique buffers to ensure that all of them are getting populated with the right data.

            const int BufferCount = 1002;
            const int BufferSize = int.MaxValue / 1000;
            const long FileSize = (long)BufferCount * BufferSize;
            string filePath = GetTestFilePath();
            ReadOnlyMemory<byte> writeBuffer = RandomNumberGenerator.GetBytes(BufferSize);
            List<ReadOnlyMemory<byte>> writeBuffers = Enumerable.Repeat(writeBuffer, BufferCount).ToList();
            List<Memory<byte>> readBuffers = new List<Memory<byte>>(BufferCount);

            try
            {
                for (int i = 0; i < BufferCount; i++)
                {
                    readBuffers.Add(new byte[BufferSize]);
                }
            }
            catch (OutOfMemoryException)
            {
                throw new SkipTestException("Not enough memory.");
            }

            FileOptions options = asyncFile ? FileOptions.Asynchronous : FileOptions.None; // we need to test both code paths
            options |= FileOptions.DeleteOnClose;

            SafeFileHandle? sfh;
            try
            {
                sfh = File.OpenHandle(filePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, options, preallocationSize: FileSize);
            }
            catch (IOException)
            {
                throw new SkipTestException("Not enough disk space.");
            }

            long fileOffset = 0, bytesRead = 0;
            try
            {
                if (asyncMethod)
                {
                    await RandomAccess.WriteAsync(sfh, writeBuffers, fileOffset);
                    bytesRead = await RandomAccess.ReadAsync(sfh, readBuffers, fileOffset);
                }
                else
                {
                    RandomAccess.Write(sfh, writeBuffers, fileOffset);
                    bytesRead = RandomAccess.Read(sfh, readBuffers, fileOffset);
                }
            }
            finally
            {
                sfh.Dispose(); // delete the file ASAP to avoid running out of resources in CI
            }

            Assert.Equal(FileSize, bytesRead);
            for (int i = 0; i < BufferCount; i++)
            {
                Assert.Equal(writeBuffer, readBuffers[i]);
            }
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        [InlineData(true, false)]
        public async Task IovLimitsAreRespected(bool asyncFile, bool asyncMethod)
        {
            // We need to write and read more than IOV_MAX buffers at a time.
            // IOV_MAX typical value is 1024.
            const int BufferCount = 1026;
            const int BufferSize = 1; // the less resources we use, the better
            const int FileSize = BufferCount * BufferSize;
            
            ReadOnlyMemory<byte> writeBuffer = RandomNumberGenerator.GetBytes(BufferSize);
            ReadOnlyMemory<byte>[] writeBuffers = Enumerable.Repeat(writeBuffer, BufferCount).ToArray();
            Memory<byte>[] readBuffers = Enumerable.Range(0, BufferCount).Select(_ => new byte[BufferSize].AsMemory()).ToArray();

            FileOptions options = asyncFile ? FileOptions.Asynchronous : FileOptions.None; // we need to test both code paths
            options |= FileOptions.DeleteOnClose;

            using SafeFileHandle sfh = File.OpenHandle(GetTestFilePath(), FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, options);

            if (asyncMethod)
            {
                await RandomAccess.WriteAsync(sfh, writeBuffers, 0);
            }
            else
            {
                RandomAccess.Write(sfh, writeBuffers, 0);
            }

            Assert.Equal(FileSize, RandomAccess.GetLength(sfh));

            long fileOffset = 0;
            int bufferOffset = 0;
            while (fileOffset < FileSize)
            {
                ArraySegment<Memory<byte>> left = new ArraySegment<Memory<byte>>(readBuffers, bufferOffset, readBuffers.Length - bufferOffset);

                long bytesRead = asyncMethod
                    ? await RandomAccess.ReadAsync(sfh, left, fileOffset)
                    : RandomAccess.Read(sfh, left, fileOffset);

                fileOffset += bytesRead;
                // The following operation is correct only because the BufferSize is 1.
                bufferOffset += (int)bytesRead;
            }

            for (int i = 0; i < BufferCount; ++i)
            {
                Assert.Equal(writeBuffers[i], readBuffers[i]);
            }
        }
    }
}
