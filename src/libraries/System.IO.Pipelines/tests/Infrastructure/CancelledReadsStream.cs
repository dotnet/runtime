// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Pipelines.Tests
{
    public class CancelledReadsStream : ReadOnlyStream
    {
        public TaskCompletionSource<object> WaitForReadTask = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await WaitForReadTask.Task;

            cancellationToken.ThrowIfCancellationRequested();

            return 0;
        }

#if NET
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await WaitForReadTask.Task;

            cancellationToken.ThrowIfCancellationRequested();

            return 0;
        }
#endif
    }
}
