// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipes;
using System.Linq;
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
                Assert.Equal(content.Length, RandomAccess.Read(readHandle, buffer, fileOffset: 0)); // what is required for the above write to succeed
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
                Task<int> readToNonEmpty = RandomAccess.ReadAsync(readHandle, buffer, fileOffset: 0).AsTask(); // what is required for the above write to succeed

                await Task.WhenAll(readToNonEmpty, write);

                Assert.Equal(content.Length, readToNonEmpty.Result);
                Assert.Equal(content, buffer.AsSpan(0, readToNonEmpty.Result).ToArray());
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
                Assert.Equal(array.Length, RandomAccess.Read(handle, buffer, fileOffset: 0));
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
                Task<int> read = RandomAccess.ReadAsync(readHandle, buffer, fileOffset: 0).AsTask();

                WriteFromStackAllocatedBuffer(writeHandle, content);

                Assert.Equal(content.Length, await read);
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

                Assert.Equal(content.Length, readFromOffset456);
                Assert.Equal(content, buffer.AsSpan(0, readFromOffset456).ToArray());

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

                Assert.Equal(content.Length, readFromOffset456.Result);
                Assert.Equal(content, buffer.AsSpan(0, readFromOffset456.Result).ToArray());
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

                Assert.Equal(content.Length, readFromOffset456.Result);
                Assert.Equal(content, buffer.AsSpan(0, readFromOffset456.Result).ToArray());
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

                byte[] buffer = new byte[content.Length * 2];

                for (int i = 0; i < content.Length; i++)
                {
                    Assert.Equal(1, RandomAccess.Read(readHandle, buffer.AsSpan(i, 1), fileOffset: 0));
                }
                Assert.Equal(content, buffer.AsSpan(0, content.Length).ToArray());

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

                byte[] buffer = new byte[content.Length * 2];
                Assert.Equal(1, await RandomAccess.ReadAsync(readHandle, buffer.AsMemory(0, 1), fileOffset: 0));
                Assert.Equal(2, await RandomAccess.ReadAsync(readHandle, buffer.AsMemory(1), fileOffset: 0));
                Assert.Equal(content, buffer.AsSpan(0, 3).ToArray());
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

                int read = 0;

                do
                {
                    read += RandomAccess.Read(readHandle, buffer.AsSpan(read), fileOffset: 456);
                } while (read != VectorsByteCount);

                Assert.Equal(vectors.SelectMany(vector => vector.ToArray()), buffer.AsSpan(0, read).ToArray());

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
                long read = RandomAccess.Read(readHandle, writableVectors, fileOffset: 456);

                Assert.Equal(VectorsByteCount, read);
                Assert.Equal(readOnlyVectors.SelectMany(vector => vector.ToArray()), writableVectors.SelectMany(vector => vector.ToArray()));

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

                Assert.Equal(VectorsByteCount, await RandomAccess.ReadAsync(readHandle, writableVectors, fileOffset: 456));
                Assert.Equal(readOnlyVectors.SelectMany(vector => vector.ToArray()), writableVectors.SelectMany(vector => vector.ToArray()));

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

                Assert.Equal(VectorsByteCount, await read);
                Assert.Equal(content, writableVectors.SelectMany(vector => vector.ToArray()));
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
                    // I am opened for ideas on how we could solve it without an ugly reflection hack..
                    ThreadPoolBoundHandle threadPoolBinding = (ThreadPoolBoundHandle)typeof(PipeStream).GetField("_threadPoolBinding", Reflection.BindingFlags.NonPublic | Reflection.BindingFlags.Instance).GetValue(pipeStream);
                    typeof(SafeFileHandle).GetProperty("ThreadPoolBinding", Reflection.BindingFlags.NonPublic | Reflection.BindingFlags.Instance).GetSetMethod(true).Invoke(serverHandle, new object[] { threadPoolBinding });
                }

                return serverHandle;
            }
            finally
            {
                pipeStream.SafePipeHandle.SetHandleAsInvalid();
            }
        }
    }
}
