// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
