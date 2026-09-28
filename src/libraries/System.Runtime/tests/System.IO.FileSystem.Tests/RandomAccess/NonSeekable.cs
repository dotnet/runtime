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
    // This class uses SafeFileHandle.CreateAnonymousPipe to create non-seekable file handles.
    // On Windows, anonymous pipes are just named pipes.
    // By default, all named pipes are created with blocking-wait mode enabled (PIPE_WAIT).
    // With a blocking-wait handle (it's orthogonal FILE_FLAG_OVERLAPPED), the write operation
    // cannot succeed until sufficient space is created in the buffer by reading from the other end of the pipe.
    // It means that even small write operations may not complete until the corresponding
    // read operations are issued on the other end of the pipe.
    // That is why this class issues async reads before synchronous writes and async writes before synchronous reads.
    // Source: https://learn.microsoft.com/windows/win32/ipc/named-pipe-type-read-and-wait-modes
    [SkipOnPlatform(TestPlatforms.Browser, "Pipes are not supported on browser")]
    public class RandomAccess_NonSeekable : FileSystemTest
    {
        private const int VectorCount = 10;
        private const int BufferSize = 3;
        private const int VectorsByteCount = VectorCount * BufferSize;

        protected virtual bool AsyncHandles => false;

        private (SafeFileHandle readHandle, SafeFileHandle writeHandle) GetAnonymousPipeHandles()
        {
            SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle readHandle, out SafeFileHandle writeHandle,
                asyncRead: AsyncHandles, asyncWrite: AsyncHandles);
            return (readHandle, writeHandle);
        }

        [Fact]
        public void ThrowsTaskAlreadyCanceledForCancelledTokenAsync()
        {
            (SafeFileHandle readHandle, SafeFileHandle writeHandle) = GetAnonymousPipeHandles();

            using (readHandle)
            using (writeHandle)
            {
                CancellationTokenSource cts = RandomAccess_Base<byte>.GetCancelledTokenSource();
                CancellationToken token = cts.Token;

                AssertCanceled(RandomAccess.ReadAsync(readHandle, new byte[1], 0, token).AsTask(), token);
                AssertCanceled(RandomAccess.WriteAsync(writeHandle, new byte[1], 0, token).AsTask(), token);
                AssertCanceled(RandomAccess.ReadAsync(readHandle, GenerateVectors(1, 1), 0, token).AsTask(), token);
                AssertCanceled(RandomAccess.WriteAsync(writeHandle, GenerateReadOnlyVectors(1, 1), 0, token).AsTask(), token);
            }

            static void AssertCanceled(Task task, CancellationToken token)
            {
                Assert.True(task.IsCanceled);
                TaskCanceledException ex = Assert.ThrowsAsync<TaskCanceledException>(() => task).Result;
                Assert.Equal(token, ex.CancellationToken);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReadToAnEmptyBufferReturnsZeroWhenDataIsAvailable(bool asyncRead)
        {
            (SafeFileHandle readHandle, SafeFileHandle writeHandle) = GetAnonymousPipeHandles();

            using (readHandle)
            using (writeHandle)
            {
                byte[] writeBuffer = RandomNumberGenerator.GetBytes(BufferSize);
                byte[] readBuffer = new byte[writeBuffer.Length];

                ValueTask writeTask = RandomAccess.WriteAsync(writeHandle, writeBuffer, fileOffset: 0);

                Assert.Equal(0, asyncRead
                    ? await RandomAccess.ReadAsync(readHandle, Array.Empty<byte>(), fileOffset: 0)
                    : RandomAccess.Read(readHandle, Array.Empty<byte>(), fileOffset: 0));

                if (asyncRead)
                {
                    await ReadExactlyAsync(readHandle, readBuffer, writeBuffer.Length);
                }
                else
                {
                    ReadExactly(readHandle, readBuffer, writeBuffer.Length);
                }

                await writeTask;
                AssertExtensions.SequenceEqual(writeBuffer, readBuffer);
            }
        }

        [Fact]
        public async Task CanReadToStackAllocatedMemory()
        {
            (SafeFileHandle readHandle, SafeFileHandle writeHandle) = GetAnonymousPipeHandles();

            using (readHandle)
            using (writeHandle)
            {
                byte[] writeBuffer = RandomNumberGenerator.GetBytes(BufferSize);

                ValueTask writeTask = RandomAccess.WriteAsync(writeHandle, writeBuffer, fileOffset: 0);

                ReadToStackAllocatedBuffer(readHandle, writeBuffer);

                await writeTask;
            }

            void ReadToStackAllocatedBuffer(SafeFileHandle handle, byte[] writeBuffer)
            {
                Span<byte> readBuffer = stackalloc byte[writeBuffer.Length];
                ReadExactly(handle, readBuffer, writeBuffer.Length);
                AssertExtensions.SequenceEqual((ReadOnlySpan<byte>)writeBuffer, readBuffer);
            }
        }

        [Fact]
        public async Task CanWriteFromStackAllocatedMemory()
        {
            (SafeFileHandle readHandle, SafeFileHandle writeHandle) = GetAnonymousPipeHandles();

            using (readHandle)
            using (writeHandle)
            {
                byte[] writeBuffer = RandomNumberGenerator.GetBytes(BufferSize);
                byte[] readBuffer = new byte[writeBuffer.Length];

                Task readTask = ReadExactlyAsync(readHandle, readBuffer, writeBuffer.Length);

                WriteFromStackAllocatedBuffer(writeHandle, writeBuffer);

                await readTask;
                AssertExtensions.SequenceEqual(writeBuffer, readBuffer);
            }

            void WriteFromStackAllocatedBuffer(SafeFileHandle handle, byte[] array)
            {
                Span<byte> buffer = stackalloc byte[array.Length];
                array.CopyTo(buffer);
                RandomAccess.Write(handle, buffer, fileOffset: 0);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task FileOffsetsAreIgnored(bool asyncWrite)
        {
            (SafeFileHandle readHandle, SafeFileHandle writeHandle) = GetAnonymousPipeHandles();

            using (readHandle)
            using (writeHandle)
            {
                byte[] writeBuffer = RandomNumberGenerator.GetBytes(BufferSize);
                byte[] readBuffer = new byte[writeBuffer.Length];

                ValueTask<int> readTask = RandomAccess.ReadAsync(readHandle, readBuffer, fileOffset: 456);

                if (asyncWrite)
                {
                    await RandomAccess.WriteAsync(writeHandle, writeBuffer, fileOffset: 123);
                }
                else
                {
                    RandomAccess.Write(writeHandle, writeBuffer, fileOffset: 123);
                }

                int readFromOffset456 = await readTask;
                Assert.InRange(readFromOffset456, 1, writeBuffer.Length);
                AssertExtensions.SequenceEqual(writeBuffer.AsSpan(0, readFromOffset456), readBuffer.AsSpan(0, readFromOffset456));
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task PartialReadsAreSupported(bool useAsync)
        {
            (SafeFileHandle readHandle, SafeFileHandle writeHandle) = GetAnonymousPipeHandles();

            using (readHandle)
            using (writeHandle)
            {
                byte[] writeBuffer = RandomNumberGenerator.GetBytes(BufferSize);
                byte[] readBuffer = new byte[BufferSize];

                ValueTask writeTask = RandomAccess.WriteAsync(writeHandle, writeBuffer, fileOffset: 0);

                for (int i = 0; i < BufferSize; i++)
                {
                    if (useAsync)
                    {
                        Assert.Equal(1, await RandomAccess.ReadAsync(readHandle, readBuffer.AsMemory(i, 1), fileOffset: 0));
                    }
                    else
                    {
                        Assert.Equal(1, RandomAccess.Read(readHandle, readBuffer.AsSpan(i, 1), fileOffset: 0));
                    }
                }

                await writeTask;
                Assert.Equal(writeBuffer, readBuffer);
            }
        }

        [Fact]
        public async Task MultipleBuffersAreSupported_AsyncWrite_SyncReads()
        {
            (SafeFileHandle readHandle, SafeFileHandle writeHandle) = GetAnonymousPipeHandles();

            using (readHandle)
            using (writeHandle)
            {
                ReadOnlyMemory<byte>[] writeVectors = GenerateReadOnlyVectors(VectorCount, BufferSize);
                byte[] readBuffer = new byte[VectorsByteCount];

                ValueTask writeTask = RandomAccess.WriteAsync(writeHandle, writeVectors, fileOffset: 123);

                int totalBytesRead = 0;
                int bytesRead;
                do
                {
                    bytesRead = RandomAccess.Read(readHandle, readBuffer.AsSpan(totalBytesRead), fileOffset: 456);
                    Assert.InRange(bytesRead, 0, VectorsByteCount - totalBytesRead);
                    totalBytesRead += bytesRead;
                } while (totalBytesRead != VectorsByteCount && bytesRead > 0);

                await writeTask;
                AssertExtensions.SequenceEqual(
                    writeVectors.SelectMany(vector => vector.ToArray()).ToArray().AsSpan(0, totalBytesRead),
                    readBuffer.AsSpan(0, totalBytesRead));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task MultipleBuffersAreSupported_AsyncWrite_ThenRead(bool asyncRead)
        {
            (SafeFileHandle readHandle, SafeFileHandle writeHandle) = GetAnonymousPipeHandles();

            using (readHandle)
            using (writeHandle)
            {
                ReadOnlyMemory<byte>[] writeVectors = GenerateReadOnlyVectors(VectorCount, BufferSize);
                Memory<byte>[] readVectors = GenerateVectors(VectorCount, BufferSize);

                ValueTask writeTask = RandomAccess.WriteAsync(writeHandle, writeVectors, fileOffset: 123);

                long bytesRead = asyncRead
                    ? await RandomAccess.ReadAsync(readHandle, readVectors, fileOffset: 456)
                    : RandomAccess.Read(readHandle, readVectors, fileOffset: 456);

                await writeTask;
                Assert.InRange(bytesRead, 1, VectorsByteCount);
                AssertEqual(writeVectors, readVectors, (int)bytesRead);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task MultipleBuffersAreSupported_AsyncRead_ThenWrite(bool asyncWrite)
        {
            (SafeFileHandle readHandle, SafeFileHandle writeHandle) = GetAnonymousPipeHandles();

            using (readHandle)
            using (writeHandle)
            {
                Memory<byte>[] readVectors = GenerateVectors(VectorCount, BufferSize);
                byte[] writeBuffer = RandomNumberGenerator.GetBytes(VectorsByteCount);

                ValueTask<long> readTask = RandomAccess.ReadAsync(readHandle, readVectors, fileOffset: 456);

                if (asyncWrite)
                {
                    await RandomAccess.WriteAsync(writeHandle, writeBuffer, fileOffset: 123);
                }
                else
                {
                    RandomAccess.Write(writeHandle, writeBuffer, fileOffset: 123);
                }

                int bytesRead = (int)await readTask;
                Assert.InRange(bytesRead, 1, VectorsByteCount);
                Assert.Equal(writeBuffer.Take(bytesRead).ToArray(), readVectors.SelectMany(vector => vector.ToArray()).Take(bytesRead).ToArray());
            }
        }

        private static ReadOnlyMemory<byte>[] GenerateReadOnlyVectors(int vectorCount, int bufferSize)
            => Enumerable.Range(0, vectorCount).Select(_ => new ReadOnlyMemory<byte>(RandomNumberGenerator.GetBytes(bufferSize))).ToArray();

        private static Memory<byte>[] GenerateVectors(int vectorCount, int bufferSize)
            => Enumerable.Range(0, vectorCount).Select(_ => new Memory<byte>(RandomNumberGenerator.GetBytes(bufferSize))).ToArray();

        private static void ReadExactly(SafeFileHandle readHandle, Span<byte> buffer, int expectedByteCount)
        {
            int totalBytesRead = 0;
            int bytesRead;
            do
            {
                bytesRead = RandomAccess.Read(readHandle, buffer.Slice(totalBytesRead), fileOffset: 0);
                Assert.InRange(bytesRead, 0, expectedByteCount - totalBytesRead);
                totalBytesRead += bytesRead;
            } while (totalBytesRead != expectedByteCount && bytesRead > 0);
        }

        private static async Task ReadExactlyAsync(SafeFileHandle readHandle, byte[] buffer, int expectedByteCount)
        {
            int totalBytesRead = 0;
            int bytesRead;
            do
            {
                bytesRead = await RandomAccess.ReadAsync(readHandle, buffer.AsMemory(totalBytesRead), fileOffset: 0);
                Assert.InRange(bytesRead, 0, expectedByteCount - totalBytesRead);
                totalBytesRead += bytesRead;
            } while (totalBytesRead != expectedByteCount && bytesRead > 0);
        }

        private static void AssertEqual(ReadOnlyMemory<byte>[] readOnlyVectors, Memory<byte>[] writableVectors, int byteCount)
            => AssertExtensions.SequenceEqual(
                readOnlyVectors.SelectMany(vector => vector.ToArray()).Take(byteCount).ToArray(),
                writableVectors.SelectMany(vector => vector.ToArray()).Take(byteCount).ToArray());
    }
}
