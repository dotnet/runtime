// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers.Text;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal partial class HttpConnection
    {
        private sealed class ChunkedEncodingReadStream : HttpContentReadStream
        {
            /// <summary>How long a chunk indicator is allowed to be.</summary>
            /// <remarks>
            /// While most chunks indicators will contain no more than ulong.MaxValue.ToString("X").Length characters,
            /// "chunk extensions" are allowed. We place a limit on how long a line can be to avoid OOM issues if an
            /// infinite chunk length is sent.  This value is arbitrary and can be changed as needed.
            /// </remarks>
            private const int MaxChunkBytesAllowed = 16*1024;
            /// <summary>How long a trailing header can be.  This value is arbitrary and can be changed as needed.</summary>
            private const int MaxTrailingHeaderLength = 16*1024;
            /// <summary>The number of bytes remaining in the chunk.</summary>
            private ulong _chunkBytesRemaining;

            public ChunkedEncodingReadStream(HttpConnection connection) : base(connection)
            {
            }

            private async Task<bool> TryGetNextChunkAsync()
            {
                Debug.Assert(_chunkBytesRemaining == 0);

                // Read the start of the chunk line.
                _connection._allowedReadLineBytes = MaxChunkBytesAllowed;
                ArraySegment<byte> line = await _connection.ReadNextLineAsync().ConfigureAwait(false);

                // Parse the hex value.
                if (!Utf8Parser.TryParse(line.AsReadOnlySpan(), out ulong chunkSize, out int bytesConsumed, 'X'))
                {
                    throw new IOException(SR.net_http_invalid_response);
                }
                else if (bytesConsumed != line.Count)
                {
                    // There was data after the chunk size, presumably a "chunk extension".
                    // Allow tabs and spaces and then stop validating once we get to an extension.
                    int offset = line.Offset + bytesConsumed, end = line.Count - bytesConsumed + line.Offset;
                    for (int i = offset; i < end; i++)
                    {
                        char c = (char)line.Array[i];
                        if (c == ';')
                        {
                            break;
                        }
                        else if (c != ' ' && c != '\t') // not called out in the RFC, but WinHTTP allows it
                        {
                            throw new IOException(SR.net_http_invalid_response);
                        }
                    }
                }

                _chunkBytesRemaining = chunkSize;
                if (chunkSize > 0)
                {
                    return true;
                }

                // We received a chunk size of 0, which indicates end of response body. 
                // Read and discard any trailing headers, until we see an empty line.
                while (true)
                {
                    _connection._allowedReadLineBytes = MaxTrailingHeaderLength;
                    if (LineIsEmpty(await _connection.ReadNextLineAsync().ConfigureAwait(false)))
                    {
                        break;
                    }
                }

                _connection.ReturnConnectionToPool();
                _connection = null;
                return false;
            }

            private Task ConsumeChunkBytesAsync(ulong bytesConsumed)
            {
                Debug.Assert(bytesConsumed <= _chunkBytesRemaining);
                _chunkBytesRemaining -= bytesConsumed;
                return _chunkBytesRemaining != 0 ?
                    Task.CompletedTask :
                    ReadNextLineAndThrowIfNotEmptyAsync();
            }

            private async Task ReadNextLineAndThrowIfNotEmptyAsync()
            {
                _connection._allowedReadLineBytes = 2; // \r\n
                if (!LineIsEmpty(await _connection.ReadNextLineAsync().ConfigureAwait(false)))
                {
                    ThrowInvalidHttpResponse();
                }
            }

            public override async ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (_connection == null || destination.Length == 0)
                {
                    // Response body fully consumed or the caller didn't ask for any data
                    return 0;
                }

                CancellationTokenRegistration ctr = _connection.RegisterCancellation(cancellationToken);
                try
                {
                    if (_chunkBytesRemaining == 0)
                    {
                        if (!await TryGetNextChunkAsync().ConfigureAwait(false))
                        {
                            // End of response body
                            return 0;
                        }
                    }

                    if (_chunkBytesRemaining < (ulong)destination.Length)
                    {
                        destination = destination.Slice(0, (int)_chunkBytesRemaining);
                    }

                    int bytesRead = await _connection.ReadAsync(destination).ConfigureAwait(false);

                    if (bytesRead <= 0)
                    {
                        // Unexpected end of response stream
                        throw new IOException(SR.net_http_invalid_response);
                    }

                    await ConsumeChunkBytesAsync((ulong)bytesRead).ConfigureAwait(false);

                    return bytesRead;
                }
                catch (Exception exc) when (ShouldWrapInOperationCanceledException(exc, cancellationToken))
                {
                    throw new OperationCanceledException(s_cancellationMessage, exc, cancellationToken);
                }
                finally
                {
                    ctr.Dispose();
                }
            }

            public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
            {
                ValidateCopyToArgs(this, destination, bufferSize);

                return cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) :
                    _connection != null ? CopyToAsyncCore(destination, bufferSize, cancellationToken) :
                    Task.CompletedTask;
            }

            private async Task CopyToAsyncCore(Stream destination, int bufferSize, CancellationToken cancellationToken)
            {
                CancellationTokenRegistration ctr = _connection.RegisterCancellation(cancellationToken);
                try
                {
                    if (_chunkBytesRemaining > 0)
                    {
                        await _connection.CopyToAsync(destination, _chunkBytesRemaining).ConfigureAwait(false);
                        await ConsumeChunkBytesAsync(_chunkBytesRemaining).ConfigureAwait(false);
                    }

                    while (await TryGetNextChunkAsync().ConfigureAwait(false))
                    {
                        await _connection.CopyToAsync(destination, _chunkBytesRemaining).ConfigureAwait(false);
                        await ConsumeChunkBytesAsync(_chunkBytesRemaining).ConfigureAwait(false);
                    }
                }
                catch (Exception exc) when (ShouldWrapInOperationCanceledException(exc, cancellationToken))
                {
                    throw CreateOperationCanceledException(exc, cancellationToken);
                }
                finally
                {
                    ctr.Dispose();
                }
            }
        }
    }
}
