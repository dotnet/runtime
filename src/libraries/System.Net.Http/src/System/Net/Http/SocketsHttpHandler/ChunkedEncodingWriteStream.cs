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
            private static readonly byte[] s_finalChunkBytes = { (byte)'0', (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n' };

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
                connection.WriteHexInt32Async(buffer.Length, async: false).GetAwaiter().GetResult();
                connection.WriteTwoBytesAsync((byte)'\r', (byte)'\n', async: false).GetAwaiter().GetResult();

                // Write chunk contents followed by \r\n
                connection.Write(buffer);
                connection.WriteTwoBytesAsync((byte)'\r', (byte)'\n', async: false).GetAwaiter().GetResult();
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
                    await connection.WriteTwoBytesAsync((byte)'\r', (byte)'\n', async: true).ConfigureAwait(false);

                    // Write chunk contents followed by \r\n
                    await connection.WriteAsync(buffer, async: true).ConfigureAwait(false);
                    await connection.WriteTwoBytesAsync((byte)'\r', (byte)'\n', async: true).ConfigureAwait(false);
                }
            }

            public override async ValueTask FinishAsync(bool async)
            {
                // Send 0 byte chunk to indicate end, then final CrLf
                HttpConnection connection = GetConnectionOrThrow();
                _connection = null;
                await connection.WriteBytesAsync(s_finalChunkBytes, async).ConfigureAwait(false);
            }
        }
    }
}
