// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipes;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "async file IO is not supported on browser")]
    public class RandomAccess_NonSeekable : FileSystemTest
    {
        private const int VectorCount = 10;
        private const int BufferSize = 3;
        private const int VectorsByteCount = VectorCount * BufferSize;

        protected virtual PipeOptions PipeOptions => PipeOptions.None;

        private async Task<(SafeFileHandle readHandle, SafeFileHandle writeHandle)> GetNamedPipeHandlesAsync()
        {
            string name = GetNamedPipeServerStreamName();

            var server = new NamedPipeServerStream(name, PipeDirection.In, -1, PipeTransmissionMode.Byte, PipeOptions);
            var client = new NamedPipeClientStream(".", name, PipeDirection.Out, PipeOptions);

            await Task.WhenAll(server.WaitForConnectionAsync(), client.ConnectAsync());

            bool isAsync = (PipeOptions & PipeOptions.Asynchronous) != 0;
            return (GetFileHandle(server, isAsync), GetFileHandle(client, isAsync));
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.AnyUnix, "named pipe implementation used by this test is using Sockets on Unix, which allow for both reading and writing")]
        public async Task ThrowsUnauthorizedAccessExceptionWhenOperationIsNotAllowed()
        {
            (SafeFileHandle readHandle, SafeFileHandle writeHandle) = await GetNamedPipeHandlesAsync();

            using (readHandle)
            using (writeHandle)
            {
                Assert.Throws<UnauthorizedAccessException>(() => RandomAccess.Read(writeHandle, new byte[1], 0));
                Assert.Throws<UnauthorizedAccessException>(() => RandomAccess.Write(readHandle, new byte[1], 0));

                Assert.Throws<UnauthorizedAccessException>(() => RandomAccess.Read(writeHandle, GenerateVectors(1, 1), 0));
                Assert.Throws<UnauthorizedAccessException>(() => RandomAccess.Write(readHandle, GenerateReadOnlyVectors(1, 1), 0));

                await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await RandomAccess.ReadAsync(writeHandle, new byte[1], 0));
                await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await RandomAccess.WriteAsync(readHandle, new byte[1], 0));

                await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await RandomAccess.ReadAsync(writeHandle, GenerateVectors(1, 1), 0));
                await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await RandomAccess.WriteAsync(readHandle, GenerateReadOnlyVectors(1, 1), 0));
            }
        }

        [Fact]
        public async Task ThrowsTaskAlreadyCanceledForCancelledTokenAsync()
        {
            (SafeFileHandle readHandle, SafeFileHandle writeHandle) = await GetNamedPipeHandlesAsync();

            using (readHandle)
            using (writeHandle)
            {
                CancellationTokenSource cts = RandomAccess_Base<byte>.GetCancelledTokenSource();
                CancellationToken token = cts.Token;

                Assert.True(RandomAccess.ReadAsync(readHandle, new byte[1], 0, token).IsCanceled);
                Assert.True(RandomAccess.WriteAsync(writeHandle, new byte[1], 0, token).IsCanceled);
                Assert.True(RandomAccess.ReadAsync(readHandle, GenerateVectors(1, 1), 0, token).IsCanceled);
                Assert.True(RandomAccess.WriteAsync(writeHandle, GenerateReadOnlyVectors(1, 1), 0, token).IsCanceled);

                TaskCanceledException ex = await Assert.ThrowsAsync<TaskCanceledException>(() => RandomAccess.ReadAsync(readHandle, new byte[1], 0, token).AsTask());
                Assert.Equal(token, ex.CancellationToken);
                ex = await Assert.ThrowsAsync<TaskCanceledException>(() => RandomAccess.WriteAsync(writeHandle, new byte[1], 0, token).AsTask());
                Assert.Equal(token, ex.CancellationToken);
                ex = await Assert.ThrowsAsync<TaskCanceledException>(() => RandomAccess.ReadAsync(writeHandle, GenerateVectors(1, 1), 0, token).AsTask());
                Assert.Equal(token, ex.CancellationToken);
                ex = await Assert.ThrowsAsync<TaskCanceledException>(() => RandomAccess.WriteAsync(writeHandle, GenerateReadOnlyVectors(1, 1), 0, token).AsTask());
                Assert.Equal(token, ex.CancellationToken);
            }
        }

        [Fact]
        public async Task ReadToAnEmptyBufferReturnsZeroWhenDataIsAvailable()
        {
            (SafeFileHandle readHandle, SafeFileHandle writeHandle) = await GetNamedPipeHandlesAsync();

            using (readHandle)
            using (writeHandle)
            {
                byte[] content = RandomNumberGenerator.GetBytes(BufferSize);
                ValueTask write = RandomAccess.WriteAsync(writeHandle, content, fileOffset: 0);

                Assert.Equal(0, RandomAccess.Read(readHandle, Array.Empty<byte>(), fileOffset: 0)); // what we test
                byte[] buffer = new byte[content.Length * 2];

                ReadExactly(readHandle, buffer, content.Length); // what is required for the above write to succeed

                Assert.Equal(content, buffer.AsSpan(0, content.Length).ToArray());

                await write;
            }
        }

        [Fact]
        public async Task ReadToAnEmptyBufferReturnsZeroWhenDataIsAvailableAsync()
        {
            (SafeFileHandle readHandle, SafeFileHandle writeHandle) = await GetNamedPipeHandlesAsync();

            using (readHandle)
            using (writeHandle)
            {
                byte[] content = RandomNumberGenerator.GetBytes(BufferSize);
                Task write = RandomAccess.WriteAsync(writeHandle, content, fileOffset: 0).AsTask();
                Task<int> readToEmpty = RandomAccess.ReadAsync(readHandle, Array.Empty<byte>(), fileOffset: 0).AsTask(); // what we test

                Assert.Equal(0, await readToEmpty);

                byte[] buffer = new byte[content.Length * 2];
                Task readToNonEmpty = ReadExactlyAsync(readHandle, buffer, content.Length); // what is required for the above write to succeed

                await Task.WhenAll(readToNonEmpty, write);

                Assert.Equal(content, buffer.AsSpan(0, content.Length).ToArray());
            }
        }

        [Fact]
        public async Task CanReadToStackAllocatedMemory()
        {
            (SafeFileHandle readHandle, SafeFileHandle writeHandle) = await GetNamedPipeHandlesAsync();

            using (readHandle)
            using (writeHandle)
            {
                byte[] content = RandomNumberGenerator.GetBytes(BufferSize);
                Task write = RandomAccess.WriteAsync(writeHandle, content, fileOffset: 0).AsTask();

                ReadToStackAllocatedBuffer(readHandle, content);

                await write;
            }

            void ReadToStackAllocatedBuffer(SafeFileHandle handle, byte[] array)
            {
                Span<byte> buffer = stackalloc byte[array.Length * 2];
                ReadExactly(handle, buffer, array.Length);
                Assert.Equal(array, buffer.Slice(0, array.Length).ToArray());
            }
        }

        [Fact]
        public async Task CanWriteFromStackAllocatedMemory()
        {
            (SafeFileHandle readHandle, SafeFileHandle writeHandle) = await GetNamedPipeHandlesAsync();

            using (readHandle)
            using (writeHandle)
            {
                byte[] content = RandomNumberGenerator.GetBytes(BufferSize);
                byte[] buffer = new byte[content.Length * 2];
                Task read = ReadExactlyAsync(readHandle, buffer, content.Length);

                WriteFromStackAllocatedBuffer(writeHandle, content);

                await read;
                Assert.Equal(content, buffer.AsSpan(0, content.Length).ToArray());
            }

            void WriteFromStackAllocatedBuffer(SafeFileHandle handle, byte[] array)
            {
                Span<byte> buffer = stackalloc byte[array.Length];
                array.CopyTo(buffer);
                RandomAccess.Write(handle, buffer, fileOffset: 0);
            }
        }

        [Fact]
        public async Task FileOffsetsAreIgnored_AsyncWrite_SyncRead()
        {
            (SafeFileHandle readHandle, SafeFileHandle writeHandle) = await GetNamedPipeHandlesAsync();

            using (readHandle)
            using (writeHandle)
            {
                byte[] content = RandomNumberGenerator.GetBytes(BufferSize);
                Task writeToOffset123 = RandomAccess.WriteAsync(writeHandle, content, fileOffset: 123).AsTask();
                byte[] buffer = new byte[content.Length * 2];
                int readFromOffset456 = RandomAccess.Read(readHandle, buffer, fileOffset: 456);

                Assert.InRange(readFromOffset456, 1, content.Length);
                Assert.Equal(content.Take(readFromOffset456), buffer.AsSpan(0, readFromOffset456).ToArray());

                await writeToOffset123;
            }
        }

        [Fact]
        public async Task FileOffsetsAreIgnored_AsyncRead_SyncWrite()
        {
            (SafeFileHandle readHandle, SafeFileHandle writeHandle) = await GetNamedPipeHandlesAsync();

            using (readHandle)
            using (writeHandle)
            {
                byte[] content = RandomNumberGenerator.GetBytes(BufferSize);
                byte[] buffer = new byte[content.Length * 2];
                Task<int> readFromOffset456 = RandomAccess.ReadAsync(readHandle, buffer, fileOffset: 456).AsTask();

                RandomAccess.Write(writeHandle, content, fileOffset: 123);

                int bytesRead = await readFromOffset456;
                Assert.InRange(bytesRead, 1, content.Length);
                Assert.Equal(content.Take(bytesRead), buffer.AsSpan(0, readFromOffset456.Result).ToArray());
            }
        }

        [Fact]
        public async Task FileOffsetsAreIgnoredAsync()
        {
            (SafeFileHandle readHandle, SafeFileHandle writeHandle) = await GetNamedPipeHandlesAsync();

            using (readHandle)
            using (writeHandle)
            {
                byte[] content = RandomNumberGenerator.GetBytes(BufferSize);
                Task writeToOffset123 = RandomAccess.WriteAsync(writeHandle, content, fileOffset: 123).AsTask();
                byte[] buffer = new byte[content.Length * 2];
                Task<int> readFromOffset456 = RandomAccess.ReadAsync(readHandle, buffer, fileOffset: 456).AsTask();

                await Task.WhenAll(readFromOffset456, writeToOffset123);

                Assert.InRange(readFromOffset456.Result, 1, content.Length);
                Assert.Equal(content.Take(readFromOffset456.Result), buffer.AsSpan(0, readFromOffset456.Result).ToArray());
            }
        }

        [Fact]
        public async Task PartialReadsAreSupported()
        {
            (SafeFileHandle readHandle, SafeFileHandle writeHandle) = await GetNamedPipeHandlesAsync();

            using (readHandle)
            using (writeHandle)
            {
                byte[] content = RandomNumberGenerator.GetBytes(BufferSize);
                ValueTask write = RandomAccess.WriteAsync(writeHandle, content, fileOffset: 0);

                byte[] buffer = new byte[BufferSize];

                for (int i = 0; i < BufferSize; i++)
                {
                    Assert.Equal(1, RandomAccess.Read(readHandle, buffer.AsSpan(i, 1), fileOffset: 0));
                }
                Assert.Equal(content, buffer);

                await write;
            }
        }

        [Fact]
        public async Task PartialReadsAreSupportedAsync()
        {
            (SafeFileHandle readHandle, SafeFileHandle writeHandle) = await GetNamedPipeHandlesAsync();

            using (readHandle)
            using (writeHandle)
            {
                byte[] content = RandomNumberGenerator.GetBytes(BufferSize);
                ValueTask write = RandomAccess.WriteAsync(writeHandle, content, fileOffset: 0);

                byte[] buffer = new byte[BufferSize];

                for (int i = 0; i < BufferSize; i++)
                {
                    Assert.Equal(1, await RandomAccess.ReadAsync(readHandle, buffer.AsMemory(i, 1), fileOffset: 0));
                }
                Assert.Equal(content, buffer);
            }
        }

        [Fact]
        public async Task MultipleBuffersAreSupported_AsyncWrite_SyncReads()
        {
            (SafeFileHandle readHandle, SafeFileHandle writeHandle) = await GetNamedPipeHandlesAsync();

            using (readHandle)
            using (writeHandle)
            {
                ReadOnlyMemory<byte>[] vectors = GenerateReadOnlyVectors(VectorCount, BufferSize);
                Task write = RandomAccess.WriteAsync(writeHandle, vectors, fileOffset: 123).AsTask();
                byte[] buffer = new byte[VectorsByteCount * 2];

                int bytesRead = 0;
                do
                {
                    bytesRead += RandomAccess.Read(readHandle, buffer.AsSpan(bytesRead), fileOffset: 456);
                } while (bytesRead != VectorsByteCount);

                Assert.Equal(vectors.SelectMany(vector => vector.ToArray()), buffer.AsSpan(0, bytesRead).ToArray());

                await write;
            }
        }

        [Fact]
        public async Task MultipleBuffersAreSupported_AsyncWrite_SyncRead()
        {
            (SafeFileHandle readHandle, SafeFileHandle writeHandle) = await GetNamedPipeHandlesAsync();

            using (readHandle)
            using (writeHandle)
            {
                ReadOnlyMemory<byte>[] readOnlyVectors = GenerateReadOnlyVectors(VectorCount, BufferSize);
                Task write = RandomAccess.WriteAsync(writeHandle, readOnlyVectors, fileOffset: 123).AsTask();
                byte[] buffer = new byte[VectorsByteCount * 2];

                Memory<byte>[] writableVectors = GenerateVectors(VectorCount, BufferSize);
                int bytesRead = (int)RandomAccess.Read(readHandle, writableVectors, fileOffset: 456);

                Assert.InRange(bytesRead, 1, VectorsByteCount);
                AssertEqual(readOnlyVectors, writableVectors, bytesRead);

                await write;
            }
        }

        [Fact]
        public async Task MultipleBuffersAreSupported_AsyncWrite_AsyncRead()
        {
            (SafeFileHandle readHandle, SafeFileHandle writeHandle) = await GetNamedPipeHandlesAsync();

            using (readHandle)
            using (writeHandle)
            {
                ReadOnlyMemory<byte>[] readOnlyVectors = GenerateReadOnlyVectors(VectorCount, BufferSize);
                Task write = RandomAccess.WriteAsync(writeHandle, readOnlyVectors, fileOffset: 123).AsTask();
                byte[] buffer = new byte[VectorsByteCount * 2];

                Memory<byte>[] writableVectors = GenerateVectors(VectorCount, BufferSize);

                long bytesRead = await RandomAccess.ReadAsync(readHandle, writableVectors, fileOffset: 456);
                Assert.InRange(bytesRead, 1, VectorsByteCount);
                AssertEqual(readOnlyVectors, writableVectors, (int)bytesRead);

                await write;
            }
        }

        [Fact]
        public async Task MultipleBuffersAreSupported_AsyncRead_SyncWrite()
        {
            (SafeFileHandle readHandle, SafeFileHandle writeHandle) = await GetNamedPipeHandlesAsync();

            using (readHandle)
            using (writeHandle)
            {
                Memory<byte>[] writableVectors = GenerateVectors(VectorCount, BufferSize);
                ValueTask<long> read = RandomAccess.ReadAsync(readHandle, writableVectors, fileOffset: 456);

                byte[] content = RandomNumberGenerator.GetBytes(VectorsByteCount);
                RandomAccess.Write(writeHandle, content, fileOffset: 123);

                int bytesRead = (int)await read;
                Assert.InRange(bytesRead, 1, VectorsByteCount);
                Assert.Equal(content.Take(bytesRead), writableVectors.SelectMany(vector => vector.ToArray()).Take(bytesRead));
            }
        }

        private static ReadOnlyMemory<byte>[] GenerateReadOnlyVectors(int vectorCount, int bufferSize)
            => Enumerable.Range(0, vectorCount).Select(_ => new ReadOnlyMemory<byte>(RandomNumberGenerator.GetBytes(bufferSize))).ToArray();

        private static Memory<byte>[] GenerateVectors(int vectorCount, int bufferSize)
            => Enumerable.Range(0, vectorCount).Select(_ => new Memory<byte>(RandomNumberGenerator.GetBytes(bufferSize))).ToArray();

        private static SafeFileHandle GetFileHandle(PipeStream pipeStream, bool isAsync)
        {
            var serverHandle = new SafeFileHandle(pipeStream.SafePipeHandle.DangerousGetHandle(), ownsHandle: true);

            try
            {
                if (OperatingSystem.IsWindows() && isAsync)
                {
                    // Currently it's impossible to duplicate an async safe handle that has already been bound to Thread Pool: https://github.com/dotnet/runtime/issues/28585
                    FileThreadPoolBoundHandle(serverHandle) = PipeThreadPoolBoundHandle(pipeStream);
                }

                return serverHandle;
            }
            finally
            {
                pipeStream.SafePipeHandle.SetHandleAsInvalid();
            }

            [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_threadPoolBinding")]
            extern static ref ThreadPoolBoundHandle PipeThreadPoolBoundHandle(PipeStream @this);

            [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "<ThreadPoolBinding>k__BackingField")]
            extern static ref ThreadPoolBoundHandle FileThreadPoolBoundHandle(SafeFileHandle @this);
        }

        private static void ReadExactly(SafeFileHandle readHandle, Span<byte> buffer, int expectedByteCount)
        {
            int bytesRead = 0;
            do
            {
                bytesRead += RandomAccess.Read(readHandle, buffer.Slice(bytesRead), fileOffset: 0); // fileOffset NOT set to bytesRead on purpose
            } while (bytesRead != expectedByteCount);
        }

        private static async Task ReadExactlyAsync(SafeFileHandle readHandle, byte[] buffer, int expectedByteCount)
        {
            int bytesRead = 0;
            do
            {
                bytesRead += await RandomAccess.ReadAsync(readHandle, buffer.AsMemory(bytesRead), fileOffset: 0); // fileOffset NOT set to bytesRead on purpose
            } while (bytesRead != expectedByteCount);
        }

        private static void AssertEqual(ReadOnlyMemory<byte>[] readOnlyVectors, Memory<byte>[] writableVectors, int byteCount)
            => Assert.Equal(
                readOnlyVectors.SelectMany(vector => vector.ToArray()).Take(byteCount),
                writableVectors.SelectMany(vector => vector.ToArray()).Take(byteCount));
    }
}
