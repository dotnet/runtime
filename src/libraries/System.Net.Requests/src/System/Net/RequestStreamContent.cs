// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    internal sealed class RequestStreamContent(TaskCompletionSource<Stream> getStreamTcs, TaskCompletionSource completeTcs) : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            return SerializeToStreamAsync(stream, context, default);
        }
        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
        {
            Debug.Assert(stream is not null);

            getStreamTcs.TrySetResult(stream);
            await completeTcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }
    }
}
