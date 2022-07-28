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
            private readonly long _contentLength;

            public ContentLengthWriteStream(HttpConnection connection, long contentLength)
                : base(connection)
            {
                _contentLength = contentLength;
            }

            public override void Write(ReadOnlySpan<byte> buffer)
            {
                BytesWritten += buffer.Length;

                if (BytesWritten > _contentLength)
                {
                    throw new HttpRequestException(SR.net_http_content_write_larger_than_content_length);
                }

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

                if (BytesWritten > _contentLength)
                {
                    return ValueTask.FromException(new HttpRequestException(SR.net_http_content_write_larger_than_content_length));
                }

                // Have the connection write the data, skipping the buffer. Importantly, this will
                // force a flush of anything already in the buffer, i.e. any remaining request headers
                // that are still buffered.
                HttpConnection connection = GetConnectionOrThrow();
                Debug.Assert(connection._currentRequest != null);
                return connection.WriteAsync(buffer, async: true);
            }

            public override Task FinishAsync(bool async)
            {
                if (BytesWritten != _contentLength)
                {
                    return Task.FromException(new HttpRequestException(SR.Format(SR.net_http_request_content_length_mismatch, BytesWritten, _contentLength)));
                }

                _connection = null;
                return Task.CompletedTask;
            }
        }
    }
}
