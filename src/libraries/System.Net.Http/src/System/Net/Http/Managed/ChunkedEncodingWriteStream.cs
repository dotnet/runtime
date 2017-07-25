// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed partial class HttpConnection : IDisposable
    {
        private sealed class ChunkedEncodingWriteStream : HttpContentWriteStream
        {
            private static readonly byte[] s_finalChunkBytes = { (byte)'0', (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n' };

            public ChunkedEncodingWriteStream(HttpConnection connection)
                : base(connection)
            {
            }

            public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                ValidateBufferArgs(buffer, offset, count);

                // Don't write if nothing was given
                // Especially since we don't want to accidentally send a 0 chunk, which would indicate end of body
                if (count == 0)
                {
                    return;
                }

                // Write chunk length -- hex representation of count
                bool digitWritten = false;
                for (int i = 7; i >= 0; i--)
                {
                    int shift = i * 4;
                    int mask = 0xF << shift;
                    int digit = (count & mask) >> shift;
                    if (digitWritten || digit != 0)
                    {
                        await _connection.WriteByteAsync((byte)(digit < 10 ? '0' + digit : 'A' + digit - 10), cancellationToken).ConfigureAwait(false);
                        digitWritten = true;
                    }
                }

                await _connection.WriteTwoBytesAsync((byte)'\r', (byte)'\n', cancellationToken).ConfigureAwait(false);

                // Write chunk contents
                await _connection.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
                await _connection.WriteTwoBytesAsync((byte)'\r', (byte)'\n', cancellationToken).ConfigureAwait(false);
            }

            public override Task FlushAsync(CancellationToken cancellationToken)
            {
                return _connection.FlushAsync(cancellationToken);
            }
            
            public override async Task FinishAsync(CancellationToken cancellationToken)
            {
                // Send 0 byte chunk to indicate end, then final CrLf
                await _connection.WriteBytesAsync(s_finalChunkBytes, cancellationToken).ConfigureAwait(false);
                _connection = null;
            }
        }
    }
}
