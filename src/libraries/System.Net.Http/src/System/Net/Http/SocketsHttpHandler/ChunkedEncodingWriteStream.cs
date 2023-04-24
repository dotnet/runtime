// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed partial class HttpConnection : IDisposable
    {
        private sealed class ChunkedEncodingWriteStream : HttpContentWriteStream
        {
            private static readonly byte[] s_crlfBytes = "\r\n"u8.ToArray();
            private static readonly byte[] s_finalChunkBytes = "0\r\n\r\n"u8.ToArray();

            public ChunkedEncodingWriteStream(HttpConnection connection) : base(connection)
            {
            }

            public override void Write(ReadOnlySpan<byte> buffer)
            {
                BytesWritten += buffer.Length;

                HttpConnection connection = GetConnectionOrThrow();
                Debug.Assert(connection._currentRequest != null);

                if (buffer.Length == 0)
                {
                    connection.Flush();
                    return;
                }

                // Write chunk length in hex followed by \r\n
                ValueTask writeTask = connection.WriteHexInt32Async(buffer.Length, async: false);
                Debug.Assert(writeTask.IsCompleted);
                writeTask.GetAwaiter().GetResult();
                connection.Write(s_crlfBytes);

                // Write chunk contents followed by \r\n
                connection.Write(buffer);
                connection.Write(s_crlfBytes);
            }

            public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ignored)
            {
                BytesWritten += buffer.Length;

                HttpConnection connection = GetConnectionOrThrow();
                Debug.Assert(connection._currentRequest != null);

                // The token is ignored because it's coming from SendAsync and the only operations
                // here are those that are already covered by the token having been registered with
                // to close the connection.

                ValueTask task = buffer.Length == 0 ?
                    // Don't write if nothing was given, especially since we don't want to accidentally send a 0 chunk,
                    // which would indicate end of body.  Instead, just ensure no content is stuck in the buffer.
                    connection.FlushAsync(async: true) :
                    WriteChunkAsync(connection, buffer);

                return task;

                static async ValueTask WriteChunkAsync(HttpConnection connection, ReadOnlyMemory<byte> buffer)
                {
                    // Write chunk length in hex followed by \r\n
                    await connection.WriteHexInt32Async(buffer.Length, async: true).ConfigureAwait(false);
                    await connection.WriteAsync(s_crlfBytes).ConfigureAwait(false);

                    // Write chunk contents followed by \r\n
                    await connection.WriteAsync(buffer).ConfigureAwait(false);
                    await connection.WriteAsync(s_crlfBytes).ConfigureAwait(false);
                }
            }

            public override Task FinishAsync(bool async)
            {
                // Send 0 byte chunk to indicate end, then final CrLf
                HttpConnection connection = GetConnectionOrThrow();
                _connection = null;

                if (async)
                {
                    return connection.WriteAsync(s_finalChunkBytes).AsTask();
                }
                else
                {
                    connection.Write(s_finalChunkBytes);
                    return Task.CompletedTask;
                }
            }
        }
    }
}
