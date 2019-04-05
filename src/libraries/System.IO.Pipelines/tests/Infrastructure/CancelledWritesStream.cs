// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Pipelines.Tests
{
    public class CancelledWritesStream : WriteOnlyStream
    {
        public TaskCompletionSource<object> WaitForWriteTask = new TaskCompletionSource<object>(TaskContinuationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<object> WaitForFlushTask = new TaskCompletionSource<object>(TaskContinuationOptions.RunContinuationsAsynchronously);

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await WaitForWriteTask.Task;

            cancellationToken.ThrowIfCancellationRequested();
        }

#if !netstandard
        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await WaitForWriteTask.Task;

            cancellationToken.ThrowIfCancellationRequested();
        }
#endif

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            await WaitForFlushTask.Task;

            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
