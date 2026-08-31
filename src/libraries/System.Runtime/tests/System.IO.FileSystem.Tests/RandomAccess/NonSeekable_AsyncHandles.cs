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

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
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
