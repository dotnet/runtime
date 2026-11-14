// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "async file IO is not supported on browser")]
    public class RandomAccess_NonSeekable_AsyncHandles : RandomAccess_NonSeekable
    {
        protected override bool AsyncHandles => true;

        [Theory]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [InlineData(FileAccess.Read, false, false)]
        [InlineData(FileAccess.Read, true, false)]
        [InlineData(FileAccess.Write, false, false)]
        [InlineData(FileAccess.Write, true, false)]
        [InlineData(FileAccess.Read, false, true)]
        [InlineData(FileAccess.Read, true, true)]
        [InlineData(FileAccess.Write, false, true)]
        [InlineData(FileAccess.Write, true, true)]
        public async Task QueuedOperationsFlowExecutionContext(FileAccess access, bool multipleBuffers, bool suppressFlow)
        {
            SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle readHandle, out SafeFileHandle writeHandle,
                asyncRead: true, asyncWrite: true);

            using (readHandle)
            using (writeHandle)
            {
                byte[] buffer = new byte[access == FileAccess.Read ? 1 : 1024 * 1024];
                Task first = access == FileAccess.Read
                    ? RandomAccess.ReadAsync(readHandle, buffer, 0).AsTask()
                    : RandomAccess.WriteAsync(writeHandle, buffer, 0).AsTask();
                Assert.False(first.IsCompleted);

                AsyncLocal<int> context = new();
                using ContextMemoryManager firstManager = new(context, suppressFlow ? 0 : 1);
                using ContextMemoryManager secondManager = new(context, suppressFlow ? 0 : 2);
                Task[] queued = new Task[2];
                for (int i = 0; i < queued.Length; i++)
                {
                    context.Value = i + 1;
                    Memory<byte> memory = i == 0 ? firstManager.Buffer : secondManager.Buffer;
                    AsyncFlowControl flow = suppressFlow ? ExecutionContext.SuppressFlow() : default;
                    try
                    {
                        queued[i] = access == FileAccess.Read
                            ? multipleBuffers
                                ? RandomAccess.ReadAsync(readHandle, new Memory<byte>[] { memory }, 0).AsTask()
                                : RandomAccess.ReadAsync(readHandle, memory, 0).AsTask()
                            : multipleBuffers
                                ? RandomAccess.WriteAsync(writeHandle, new ReadOnlyMemory<byte>[] { memory }, 0).AsTask()
                                : RandomAccess.WriteAsync(writeHandle, memory, 0).AsTask();
                    }
                    finally
                    {
                        if (suppressFlow)
                        {
                            flow.Undo();
                        }
                    }
                    Assert.False(queued[i].IsCompleted);
                }

                context.Value = 3;
                if (access == FileAccess.Read)
                {
                    await RandomAccess.WriteAsync(writeHandle, new byte[3], 0);
                }
                else
                {
                    int remaining = buffer.Length;
                    while (remaining > 0)
                    {
                        int count = await RandomAccess.ReadAsync(readHandle, buffer.AsMemory(0, remaining), 0).AsTask().WaitAsync(TimeSpan.FromSeconds(3));
                        Assert.True(count > 0);
                        remaining -= count;
                    }
                }

                await first.WaitAsync(TimeSpan.FromSeconds(3));
                await Task.WhenAll(queued).WaitAsync(TimeSpan.FromSeconds(3));
                Assert.Equal(3, context.Value);
            }
        }

        private sealed class ContextMemoryManager(AsyncLocal<int> context, int expected) : MemoryManager<byte>
        {
            private readonly byte[] _buffer = new byte[1];

            public Memory<byte> Buffer => CreateMemory(1);
            public override Span<byte> GetSpan()
            {
                Assert.Equal(expected, context.Value);
                return _buffer;
            }
            public override MemoryHandle Pin(int elementIndex = 0)
            {
                Assert.Equal(expected, context.Value);
                return _buffer.AsMemory(elementIndex).Pin();
            }
            public override void Unpin() { }
            protected override void Dispose(bool disposing) { }
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [InlineData(FileAccess.Read, false)]
        [InlineData(FileAccess.Read, true)]
        [InlineData(FileAccess.Write, false)]
        [InlineData(FileAccess.Write, true)]
        public async Task QueuedOperationReportsBufferException(FileAccess access, bool multipleBuffers)
        {
            SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle readHandle, out SafeFileHandle writeHandle,
                asyncRead: true, asyncWrite: true);

            using (readHandle)
            using (writeHandle)
            {
                using ThrowingMemoryManager manager = new();
                Memory<byte> memory = manager.Buffer;
                byte[] buffer = new byte[access == FileAccess.Read ? 1 : 1024 * 1024];
                Task first = access == FileAccess.Read
                    ? RandomAccess.ReadAsync(readHandle, buffer, 0).AsTask()
                    : RandomAccess.WriteAsync(writeHandle, buffer, 0).AsTask();
                Assert.False(first.IsCompleted);

                Task failing = access == FileAccess.Read
                    ? multipleBuffers
                        ? RandomAccess.ReadAsync(readHandle, new Memory<byte>[] { memory }, 0).AsTask()
                        : RandomAccess.ReadAsync(readHandle, memory, 0).AsTask()
                    : multipleBuffers
                        ? RandomAccess.WriteAsync(writeHandle, new ReadOnlyMemory<byte>[] { memory }, 0).AsTask()
                        : RandomAccess.WriteAsync(writeHandle, memory, 0).AsTask();
                Assert.False(failing.IsCompleted);

                if (access == FileAccess.Read)
                {
                    await RandomAccess.WriteAsync(writeHandle, new byte[] { 42 }, 0);
                }
                else
                {
                    int remaining = buffer.Length;
                    while (remaining > 0)
                    {
                        int count = await RandomAccess.ReadAsync(readHandle, buffer.AsMemory(0, remaining), 0).AsTask().WaitAsync(TimeSpan.FromSeconds(3));
                        Assert.True(count > 0);
                        remaining -= count;
                    }
                }

                await first.WaitAsync(TimeSpan.FromSeconds(3));
                await Assert.ThrowsAsync<InvalidOperationException>(() => failing.WaitAsync(TimeSpan.FromSeconds(3)));

                Task<int> nextRead = RandomAccess.ReadAsync(readHandle, buffer.AsMemory(0, 1), 0).AsTask();
                await RandomAccess.WriteAsync(writeHandle, new byte[] { 43 }, 0);
                Assert.Equal(1, await nextRead.WaitAsync(TimeSpan.FromSeconds(3)));
                Assert.Equal(43, buffer[0]);
            }
        }

        [Fact]
        public async Task QueuedEmptyReadWaitsForData()
        {
            SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle readHandle, out SafeFileHandle writeHandle,
                asyncRead: true, asyncWrite: true);

            using (readHandle)
            using (writeHandle)
            {
                byte[] buffer = new byte[1];
                Task<int> read = RandomAccess.ReadAsync(readHandle, buffer, 0).AsTask();
                Task<int> emptyRead = RandomAccess.ReadAsync(readHandle, Memory<byte>.Empty, 0).AsTask();

                await RandomAccess.WriteAsync(writeHandle, new byte[] { 42 }, 0);
                Assert.Equal(1, await read.WaitAsync(TimeSpan.FromSeconds(3)));
                Assert.Equal(42, buffer[0]);

                await Task.Delay(250);
                Assert.False(emptyRead.IsCompleted);

                await RandomAccess.WriteAsync(writeHandle, new byte[] { 43 }, 0);
                Assert.Equal(0, await emptyRead.WaitAsync(TimeSpan.FromSeconds(3)));
                Assert.Equal(1, await RandomAccess.ReadAsync(readHandle, buffer, 0).AsTask().WaitAsync(TimeSpan.FromSeconds(3)));
                Assert.Equal(43, buffer[0]);
            }
        }

        [Fact]
        public async Task EmptyReadCanBeCanceled()
        {
            SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle readHandle, out SafeFileHandle writeHandle,
                asyncRead: true, asyncWrite: true);

            using (readHandle)
            using (writeHandle)
            using (CancellationTokenSource cancellation = new())
            {
                Task<int> read = RandomAccess.ReadAsync(readHandle, Memory<byte>.Empty, 0, cancellation.Token).AsTask();
                Assert.False(read.IsCompleted);
                cancellation.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => read.WaitAsync(TimeSpan.FromSeconds(3)));

                Task<int> nextRead = RandomAccess.ReadAsync(readHandle, Memory<byte>.Empty, 0).AsTask();
                await RandomAccess.WriteAsync(writeHandle, new byte[] { 42 }, 0);
                Assert.Equal(0, await nextRead.WaitAsync(TimeSpan.FromSeconds(3)));
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public async Task EmptyReadOnWriteOnlyPipeThrows()
        {
            SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle readHandle, out SafeFileHandle writeHandle,
                asyncRead: true, asyncWrite: true);

            using (readHandle)
            using (writeHandle)
            {
                await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
                    await RandomAccess.ReadAsync(writeHandle, Memory<byte>.Empty, 0).AsTask().WaitAsync(TimeSpan.FromSeconds(3)));

                readHandle.Dispose();
                Assert.Throws<UnauthorizedAccessException>(() => RandomAccess.Read(writeHandle, Span<byte>.Empty, 0));
                await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
                    await RandomAccess.ReadAsync(writeHandle, Memory<byte>.Empty, 0));
            }
        }

        [Theory]
        [InlineData(FileAccess.Read, false)]
        [InlineData(FileAccess.Read, true)]
        [InlineData(FileAccess.Write, false)]
        [InlineData(FileAccess.Write, true)]
        public void CanceledOperationDoesNotRetainBuffer(FileAccess access, bool multipleBuffers)
        {
            SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle readHandle, out SafeFileHandle writeHandle,
                asyncRead: true, asyncWrite: true);

            using (readHandle)
            using (writeHandle)
            {
                WeakReference buffer = StartAndCancel(access == FileAccess.Read ? readHandle : writeHandle, access, multipleBuffers);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                Assert.False(buffer.IsAlive);
                GC.KeepAlive(readHandle);
                GC.KeepAlive(writeHandle);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static WeakReference StartAndCancel(SafeFileHandle handle, FileAccess access, bool multipleBuffers)
            {
                byte[] buffer = new byte[1024 * 1024]; // Large enough to keep writes pending on an empty pipe.
                WeakReference reference = new(buffer);
                using CancellationTokenSource cancellation = new();
                Task task = access switch
                {
                    FileAccess.Read when multipleBuffers => RandomAccess.ReadAsync(handle, new Memory<byte>[] { buffer }, 0, cancellation.Token).AsTask(),
                    FileAccess.Read => RandomAccess.ReadAsync(handle, buffer, 0, cancellation.Token).AsTask(),
                    FileAccess.Write when multipleBuffers => RandomAccess.WriteAsync(handle, new ReadOnlyMemory<byte>[] { buffer }, 0, cancellation.Token).AsTask(),
                    _ => RandomAccess.WriteAsync(handle, buffer, 0, cancellation.Token).AsTask()
                };

                Assert.False(task.IsCompleted);
                cancellation.Cancel();
                Assert.ThrowsAny<OperationCanceledException>(() => task.WaitAsync(TimeSpan.FromSeconds(3)).GetAwaiter().GetResult());
                return reference;
            }
        }

        [Theory]
        [InlineData(FileAccess.Read)]
        [InlineData(FileAccess.Write)]
        public async Task CancellationIsSupported(FileAccess access)
        {
            SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle readHandle, out SafeFileHandle writeHandle,
                asyncRead: true, asyncWrite: true);

            using (readHandle)
            using (writeHandle)
            {
                SafeFileHandle handle = access == FileAccess.Read ? readHandle : writeHandle;

                Assert.True(handle.IsAsync);

                CancellationTokenSource cts = new(TimeSpan.FromMilliseconds(250));
                CancellationToken token = cts.Token;
                byte[] buffer = new byte[1024 * 1024]; // use a large buffer to ensure the async pipe write is pending

                OperationCanceledException ex = await Assert.ThrowsAsync<OperationCanceledException>(
                    () => access == FileAccess.Write
                        ? RandomAccess.WriteAsync(handle, buffer, 0, token).AsTask()
                        : RandomAccess.ReadAsync(handle, buffer, 0, token).AsTask());

                Assert.Equal(token, ex.CancellationToken);
            }
        }
    }
}
