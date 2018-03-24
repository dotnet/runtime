// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal partial class HttpConnection : IDisposable
    {
        private sealed class ContentLengthWriteStream : HttpContentWriteStream
        {
            public ContentLengthWriteStream(HttpConnection connection) : base(connection)
            {
            }

            public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ignored) // token ignored as it comes from SendAsync
            {
                Debug.Assert(_connection._currentRequest != null);

                // Have the connection write the data, skipping the buffer. Importantly, this will
                // force a flush of anything already in the buffer, i.e. any remaining request headers
                // that are still buffered.
                return new ValueTask(_connection.WriteAsync(buffer));
            }

            public override Task FinishAsync()
            {
                _connection = null;
                return Task.CompletedTask;
            }
        }
    }
}
