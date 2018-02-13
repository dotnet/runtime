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
        private sealed class ChunkedEncodingWriteStream : HttpContentWriteStream
        {
            private static readonly byte[] s_finalChunkBytes = { (byte)'0', (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n' };

            public ChunkedEncodingWriteStream(HttpConnection connection) : base(connection)
            {
            }

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken ignored)
            {
                ValidateBufferArgs(buffer, offset, count);
                return WriteAsync(new Memory<byte>(buffer, offset, count), ignored);
            }

            public override Task WriteAsync(ReadOnlyMemory<byte> source, CancellationToken ignored)
            {
                // The token is ignored because it's coming from SendAsync and the only operations
                // here are those that are already covered by the token having been registered with
                // to close the connection.

                if (source.Length == 0)
                {
                    // Don't write if nothing was given, especially since we don't want to accidentally send a 0 chunk,
                    // which would indicate end of body.  Instead, just ensure no content is stuck in the buffer.
                    return _connection.FlushAsync();
                }

                if (_connection._currentRequest == null)
                {
                    // Avoid sending anything if the response has already completed, in which case there's no point
                    // sending further data (this might happen, for example, on a redirect.)
                    return Task.CompletedTask;
                }

                return WriteChunkAsync(source);
            }

            private async Task WriteChunkAsync(ReadOnlyMemory<byte> source)
            {
                // Write chunk length -- hex representation of count
                bool digitWritten = false;
                for (int i = 7; i >= 0; i--)
                {
                    int shift = i * 4;
                    int mask = 0xF << shift;
                    int digit = (source.Length & mask) >> shift;
                    if (digitWritten || digit != 0)
                    {
                        await _connection.WriteByteAsync((byte)(digit < 10 ? '0' + digit : 'A' + digit - 10)).ConfigureAwait(false);
                        digitWritten = true;
                    }
                }

                // End chunk length
                await _connection.WriteTwoBytesAsync((byte)'\r', (byte)'\n').ConfigureAwait(false);

                // Write chunk contents
                await _connection.WriteAsync(source).ConfigureAwait(false);
                await _connection.WriteTwoBytesAsync((byte)'\r', (byte)'\n').ConfigureAwait(false);

                // Flush the chunk.  This is reasonable from the standpoint of having just written a standalone piece
                // of data, but is also necessary to support duplex communication, where a CopyToAsync is taking the
                // data from content and writing it here; if there was no flush, we might not send the data until the
                // source was empty, and it might be kept open to enable subsequent communication.  And it's necessary
                // in general for at least the first write, as we need to ensure if it's the entirety of the content
                // and if all of the headers and content fit in the write buffer that we've actually sent the request.
                await _connection.FlushAsync().ConfigureAwait(false);
            }

            public override Task FlushAsync(CancellationToken ignored) => // see comment on WriteAsync about "ignored"
                _connection.FlushAsync();

            public override async Task FinishAsync()
            {
                // Send 0 byte chunk to indicate end, then final CrLf
                await _connection.WriteBytesAsync(s_finalChunkBytes).ConfigureAwait(false);
                _connection = null;
            }
        }
    }
}
