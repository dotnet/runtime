// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed partial class HttpConnection
    {
        private sealed class ChunkedEncodingReadStream : HttpContentReadStream
        {
            private ulong _chunkBytesRemaining;

            public ChunkedEncodingReadStream(HttpConnection connection)
                : base(connection)
            {
                _chunkBytesRemaining = 0;
            }

            private async Task<bool> TryGetNextChunk(CancellationToken cancellationToken)
            {
                Debug.Assert(_chunkBytesRemaining == 0);

                // Start of chunk, read chunk size.
                ulong chunkSize = ParseHexSize(await _connection.ReadNextLineAsync(cancellationToken).ConfigureAwait(false));
                _chunkBytesRemaining = chunkSize;

                if (chunkSize > 0)
                {
                    return true;
                }

                // Indicates end of response body. We expect final CRLF after this.
                await _connection.ReadCrLfAsync(cancellationToken).ConfigureAwait(false);
                _connection.ReturnConnectionToPool();
                _connection = null;
                return false;
            }

            private ulong ParseHexSize(ArraySegment<byte> line)
            {
                ulong size = 0;
                try
                {
                    for (int i = 0; i < line.Count; i++)
                    {
                        char c = (char)line[i];
                        if ((uint)(c - '0') <= '9' - '0')
                        {
                            size = checked(size * 16 + ((ulong)c - '0'));
                        }
                        else if ((uint)(c - 'a') <= ('f' - 'a'))
                        {
                            size = checked(size * 16 + ((ulong)c - 'a' + 10));
                        }
                        else if ((uint)(c - 'A') <= ('F' - 'A'))
                        {
                            size = checked(size * 16 + ((ulong)c - 'A' + 10));
                        }
                        else
                        {
                            if (c == '\r' && i > 0)
                            {
                                break;
                            }
                            throw new IOException(SR.net_http_invalid_response);
                        }
                    }
                }
                catch (OverflowException e)
                {
                    throw new IOException(SR.net_http_invalid_response, e);
                }
                return size;
            }

            private async Task ConsumeChunkBytes(ulong bytesConsumed, CancellationToken cancellationToken)
            {
                Debug.Assert(bytesConsumed <= _chunkBytesRemaining);
                _chunkBytesRemaining -= bytesConsumed;
                if (_chunkBytesRemaining == 0)
                {
                    await _connection.ReadCrLfAsync(cancellationToken).ConfigureAwait(false);
                }
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                ValidateBufferArgs(buffer, offset, count);
                return ReadAsync(new Memory<byte>(buffer, offset, count)).AsTask();
            }

            public override async ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default)
            {
                if (_connection == null || destination.Length == 0)
                {
                    // Response body fully consumed or the caller didn't ask for any data
                    return 0;
                }

                if (_chunkBytesRemaining == 0)
                {
                    if (!await TryGetNextChunk(cancellationToken).ConfigureAwait(false))
                    {
                        // End of response body
                        return 0;
                    }
                }

                if (_chunkBytesRemaining < (ulong)destination.Length)
                {
                    destination = destination.Slice(0, (int)_chunkBytesRemaining);
                }

                int bytesRead = await _connection.ReadAsync(destination, cancellationToken).ConfigureAwait(false);

                if (bytesRead <= 0)
                {
                    // Unexpected end of response stream
                    throw new IOException(SR.net_http_invalid_response);
                }

                await ConsumeChunkBytes((ulong)bytesRead, cancellationToken).ConfigureAwait(false);

                return bytesRead;
            }

            public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
            {
                if (destination == null)
                {
                    throw new ArgumentNullException(nameof(destination));
                }

                if (_connection == null)
                {
                    // Response body fully consumed
                    return;
                }

                if (_chunkBytesRemaining > 0)
                {
                    await _connection.CopyToAsync(destination, _chunkBytesRemaining, cancellationToken).ConfigureAwait(false);
                    await ConsumeChunkBytes(_chunkBytesRemaining, cancellationToken).ConfigureAwait(false);
                }

                while (await TryGetNextChunk(cancellationToken).ConfigureAwait(false))
                {
                    await _connection.CopyToAsync(destination, _chunkBytesRemaining, cancellationToken).ConfigureAwait(false);
                    await ConsumeChunkBytes(_chunkBytesRemaining, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}
