// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;

namespace System.Net.Http.Functional.Tests
{
    /// <summary>HttpContent that traps the asynchronous invocation of SerializeToStreamAsync.</summary>
    public class AsyncGateContent : HttpContent
    {
        private readonly int? _length;

        public AsyncGateContent(int? length = 10)
        {
            _length = length;
        }

        public TaskCompletionSource<(Stream Stream, TransportContext Context)> SerializeInvokedTcs =
            new TaskCompletionSource<(Stream Stream, TransportContext Context)>(
                TaskCreationOptions.RunContinuationsAsynchronously
            );

        public TaskCompletionSource<object> SerializeTrapTcs =
            new TaskCompletionSource<object>(
                TaskCreationOptions.RunContinuationsAsynchronously
            );

        /// <inheritdoc />
        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            SerializeInvokedTcs.TrySetResult((stream, context));
            await SerializeTrapTcs.Task.ConfigureAwait(false);
        }

        protected override bool TryComputeLength(out long length)
        {
            if (_length.HasValue)
            {
                length = _length.Value;
                return true;
            }
            else
            {
                length = 0;
                return false;
            }
        }
    }
}
