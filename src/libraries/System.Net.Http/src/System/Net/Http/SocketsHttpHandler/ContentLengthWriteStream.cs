// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed partial class HttpConnection : IDisposable
    {
        private sealed class ContentLengthWriteStream : HttpContentWriteStream
        {
            public ContentLengthWriteStream(HttpConnection connection) : base(connection)
            {
            }

            public override void Write(ReadOnlySpan<byte> buffer)
            {
                BytesWritten += buffer.Length;

                // Have the connection write the data, skipping the buffer. Importantly, this will
                // force a flush of anything already in the buffer, i.e. any remaining request headers
                // that are still buffered.
                HttpConnection connection = GetConnectionOrThrow();
                Debug.Assert(connection._currentRequest != null);
                connection.Write(buffer);
            }

            public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ignored) // token ignored as it comes from SendAsync
            {
                BytesWritten += buffer.Length;

                // Have the connection write the data, skipping the buffer. Importantly, this will
                // force a flush of anything already in the buffer, i.e. any remaining request headers
                // that are still buffered.
                HttpConnection connection = GetConnectionOrThrow();
                Debug.Assert(connection._currentRequest != null);
                return connection.WriteAsync(buffer, async: true);
            }

            public override ValueTask FinishAsync(bool async)
            {
                _connection = null;
                return default;
            }
        }
    }
}
