// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Tests
{
    public class RandomAccess_NonSeekable_AsyncHandles : RandomAccess_NonSeekable
    {
        protected override PipeOptions PipeOptions => PipeOptions.Asynchronous;

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))] // cancellable file IO is supported only on Windows
        [InlineData(FileAccess.Read)]
        [InlineData(FileAccess.Write)]
        public async Task CancellationIsSupported(FileAccess access)
        {
            string pipeName = FileSystemTest.GetNamedPipeServerStreamName();
            string pipePath = Path.GetFullPath($@"\\.\pipe\{pipeName}");

            using (var server = new NamedPipeServerStream(pipeName, PipeDirection.InOut))
            using (SafeFileHandle clientHandle = File.OpenHandle(pipePath, FileMode.Open, access, FileShare.None, FileOptions.Asynchronous))
            {
                await server.WaitForConnectionAsync();

                Assert.True(clientHandle.IsAsync);

                CancellationTokenSource cts = new(TimeSpan.FromMilliseconds(250));
                CancellationToken token = cts.Token;
                byte[] buffer = new byte[1];

                OperationCanceledException ex = await Assert.ThrowsAsync<OperationCanceledException>(
                    () => access == FileAccess.Write
                        ? RandomAccess.WriteAsync(clientHandle, buffer, 0, token).AsTask()
                        : RandomAccess.ReadAsync(clientHandle, buffer, 0, token).AsTask());

                Assert.Equal(token, ex.CancellationToken);
            }
        }
    }
}
