// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Text;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed partial class HttpConnection
    {
        private sealed class ChunkedEncodingReadStream : HttpContentReadStream
        {
            /// <summary>The number of bytes remaining in the chunk.</summary>
            private ulong _chunkBytesRemaining;
            /// <summary>The current state of the parsing state machine for the chunked response.</summary>
            private ParsingState _state = ParsingState.ExpectChunkHeader;
            private readonly HttpResponseMessage _response;

            public ChunkedEncodingReadStream(HttpConnection connection, HttpResponseMessage response) : base(connection)
            {
                Debug.Assert(response != null, "The HttpResponseMessage cannot be null.");
                _response = response;
            }

            public override int Read(Span<byte> buffer)
            {
                if (_connection == null)
                {
                    // Response body fully consumed
                    return 0;
                }

                if (buffer.Length == 0)
                {
                    if (PeekChunkFromConnectionBuffer())
                    {
                        return 0;
                    }
                }
                else
                {
                    // Try to consume from data we already have in the buffer.
                    int bytesRead = ReadChunksFromConnectionBuffer(buffer, cancellationRegistration: default);
                    if (bytesRead > 0)
                    {
                        return bytesRead;
                    }
                }

                // Nothing available to consume.  Fall back to I/O.
                while (true)
                {
                    if (_connection == null)
                    {
                        // Fully consumed the response in ReadChunksFromConnectionBuffer.
                        return 0;
                    }

                    if (_state == ParsingState.ExpectChunkData &&
                        buffer.Length >= _connection.ReadBufferSize &&
                        _chunkBytesRemaining >= (ulong)_connection.ReadBufferSize)
                    {
                        // As an optimization, we skip going through the connection's read buffer if both
                        // the remaining chunk data and the buffer are both at least as large
                        // as the connection buffer.  That avoids an unnecessary copy while still reading
                        // the maximum amount we'd otherwise read at a time.
                        Debug.Assert(_connection.RemainingBuffer.Length == 0);
                        Debug.Assert(buffer.Length != 0);
                        int bytesRead = _connection.Read(buffer.Slice(0, (int)Math.Min((ulong)buffer.Length, _chunkBytesRemaining)));
                        if (bytesRead == 0)
                        {
                            throw new HttpIOException(HttpRequestError.ResponseEnded, SR.Format(SR.net_http_invalid_response_premature_eof_bytecount, _chunkBytesRemaining));
                        }
                        _chunkBytesRemaining -= (ulong)bytesRead;
                        if (_chunkBytesRemaining == 0)
                        {
                            _state = ParsingState.ExpectChunkTerminator;
                        }
                        return bytesRead;
                    }

                    if (buffer.Length == 0)
                    {
                        // User requested a zero-byte read, and we have no data available in the buffer for processing.
                        // This zero-byte read indicates their desire to trade off the extra cost of a zero-byte read
                        // for reduced memory consumption when data is not immediately available.
                        // So, we will issue our own zero-byte read against the underlying stream to allow it to make use of
                        // optimizations, such as deferring buffer allocation until data is actually available.
                        _connection.Read(buffer);
                    }

                    // We're only here if we need more data to make forward progress.
                    Fill();

                    // Now that we have more, see if we can get any response data, and if
                    // we can we're done.
                    if (buffer.Length == 0)
                    {
                        if (PeekChunkFromConnectionBuffer())
                        {
                            return 0;
                        }
                    }
                    else
                    {
                        int bytesCopied = ReadChunksFromConnectionBuffer(buffer, cancellationRegistration: default);
                        if (bytesCopied > 0)
                        {
                            return bytesCopied;
                        }
                    }
                }
            }

            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    // Cancellation requested.
                    return ValueTask.FromCanceled<int>(cancellationToken);
                }

                if (_connection == null)
                {
                    // Response body fully consumed
                    return new ValueTask<int>(0);
                }

                if (buffer.Length == 0)
                {
                    if (PeekChunkFromConnectionBuffer())
                    {
                        return new ValueTask<int>(0);
                    }
                }
                else
                {
                    // Try to consume from data we already have in the buffer.
                    int bytesRead = ReadChunksFromConnectionBuffer(buffer.Span, cancellationRegistration: default);
                    if (bytesRead > 0)
                    {
                        return new ValueTask<int>(bytesRead);
                    }
                }

                // We may have just consumed the remainder of the response (with no actual data
                // available), so check again.
                if (_connection == null)
                {
                    Debug.Assert(_state == ParsingState.Done);
                    return new ValueTask<int>(0);
                }

                // Nothing available to consume.  Fall back to I/O.
                return ReadAsyncCore(buffer, cancellationToken);
            }

            private async ValueTask<int> ReadAsyncCore(Memory<byte> buffer, CancellationToken cancellationToken)
            {
                // Should only be called if ReadChunksFromConnectionBuffer returned 0.

                Debug.Assert(_connection != null);

                CancellationTokenRegistration ctr = _connection.RegisterCancellation(cancellationToken);
                try
                {
                    while (true)
                    {
                        if (_connection == null)
                        {
                            // Fully consumed the response in ReadChunksFromConnectionBuffer.
                            return 0;
                        }

                        if (_state == ParsingState.ExpectChunkData &&
                            buffer.Length >= _connection.ReadBufferSize &&
                            _chunkBytesRemaining >= (ulong)_connection.ReadBufferSize)
                        {
                            // As an optimization, we skip going through the connection's read buffer if both
                            // the remaining chunk data and the buffer are both at least as large
                            // as the connection buffer.  That avoids an unnecessary copy while still reading
                            // the maximum amount we'd otherwise read at a time.
                            Debug.Assert(_connection.RemainingBuffer.Length == 0);
                            Debug.Assert(buffer.Length != 0);
                            int bytesRead = await _connection.ReadAsync(buffer.Slice(0, (int)Math.Min((ulong)buffer.Length, _chunkBytesRemaining))).ConfigureAwait(false);
                            if (bytesRead == 0)
                            {
                                throw new HttpIOException(HttpRequestError.ResponseEnded, SR.Format(SR.net_http_invalid_response_premature_eof_bytecount, _chunkBytesRemaining));
                            }
                            _chunkBytesRemaining -= (ulong)bytesRead;
                            if (_chunkBytesRemaining == 0)
                            {
                                _state = ParsingState.ExpectChunkTerminator;
                            }
                            return bytesRead;
                        }

                        if (buffer.Length == 0)
                        {
                            // User requested a zero-byte read, and we have no data available in the buffer for processing.
                            // This zero-byte read indicates their desire to trade off the extra cost of a zero-byte read
                            // for reduced memory consumption when data is not immediately available.
                            // So, we will issue our own zero-byte read against the underlying stream to allow it to make use of
                            // optimizations, such as deferring buffer allocation until data is actually available.
                            await _connection.ReadAsync(buffer).ConfigureAwait(false);
                        }

                        // We're only here if we need more data to make forward progress.
                        await FillAsync().ConfigureAwait(false);

                        // Now that we have more, see if we can get any response data, and if
                        // we can we're done.
                        if (buffer.Length == 0)
                        {
                            if (PeekChunkFromConnectionBuffer())
                            {
                                return 0;
                            }
                        }
                        else
                        {
                            int bytesCopied = ReadChunksFromConnectionBuffer(buffer.Span, ctr);
                            if (bytesCopied > 0)
                            {
                                return bytesCopied;
                            }
                        }
                    }
                }
                catch (Exception exc) when (CancellationHelper.ShouldWrapInOperationCanceledException(exc, cancellationToken))
                {
                    throw CancellationHelper.CreateOperationCanceledException(exc, cancellationToken);
                }
                finally
                {
                    ctr.Dispose();
                }
            }

            public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
            {
                ValidateCopyToArguments(destination, bufferSize);

                return
                    cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) :
                    _connection == null ? Task.CompletedTask :
                    CopyToAsyncCore(destination, cancellationToken);
            }

            private async Task CopyToAsyncCore(Stream destination, CancellationToken cancellationToken)
            {
                CancellationTokenRegistration ctr = _connection!.RegisterCancellation(cancellationToken);
                try
                {
                    while (true)
                    {
                        while (true)
                        {
                            if (ReadChunkFromConnectionBuffer(int.MaxValue, ctr) is not ReadOnlyMemory<byte> bytesRead || bytesRead.Length == 0)
                            {
                                break;
                            }
                            await destination.WriteAsync(bytesRead, cancellationToken).ConfigureAwait(false);
                        }

                        if (_connection == null)
                        {
                            // Fully consumed the response.
                            return;
                        }

                        await FillAsync().ConfigureAwait(false);
                    }
                }
                catch (Exception exc) when (CancellationHelper.ShouldWrapInOperationCanceledException(exc, cancellationToken))
                {
                    throw CancellationHelper.CreateOperationCanceledException(exc, cancellationToken);
                }
                finally
                {
                    ctr.Dispose();
                }
            }

            private bool PeekChunkFromConnectionBuffer()
            {
                return ReadChunkFromConnectionBuffer(maxBytesToRead: 0, cancellationRegistration: default).HasValue;
            }

            private int ReadChunksFromConnectionBuffer(Span<byte> buffer, CancellationTokenRegistration cancellationRegistration)
            {
                Debug.Assert(buffer.Length > 0);
                int totalBytesRead = 0;
                while (buffer.Length > 0)
                {
                    if (ReadChunkFromConnectionBuffer(buffer.Length, cancellationRegistration) is not ReadOnlyMemory<byte> bytesRead || bytesRead.Length == 0)
                    {
                        break;
                    }

                    Debug.Assert(bytesRead.Length <= buffer.Length);
                    totalBytesRead += bytesRead.Length;
                    bytesRead.Span.CopyTo(buffer);
                    buffer = buffer.Slice(bytesRead.Length);
                }
                return totalBytesRead;
            }

            private ReadOnlyMemory<byte>? ReadChunkFromConnectionBuffer(int maxBytesToRead, CancellationTokenRegistration cancellationRegistration)
            {
                Debug.Assert(_connection != null);

                try
                {
                    ReadOnlySpan<byte> currentLine;
                    switch (_state)
                    {
                        case ParsingState.ExpectChunkHeader:
                            Debug.Assert(_chunkBytesRemaining == 0, $"Expected {nameof(_chunkBytesRemaining)} == 0, got {_chunkBytesRemaining}");

                            // Read the chunk header line.
                            if (!_connection.TryReadNextChunkedLine(out currentLine))
                            {
                                // Could not get a whole line, so we can't parse the chunk header.
                                return default;
                            }

                            // Parse the hex value from it.
                            if (!Utf8Parser.TryParse(currentLine, out ulong chunkSize, out int bytesConsumed, 'X'))
                            {
                                throw new HttpIOException(HttpRequestError.InvalidResponse, SR.Format(SR.net_http_invalid_response_chunk_header_invalid, BitConverter.ToString(currentLine.ToArray())));
                            }
                            _chunkBytesRemaining = chunkSize;

                            // If there's a chunk extension after the chunk size, validate it.
                            if (bytesConsumed != currentLine.Length)
                            {
                                ValidateChunkExtension(currentLine.Slice(bytesConsumed));
                            }

                            // Proceed to handle the chunk.  If there's data in it, go read it.
                            // Otherwise, finish handling the response.
                            if (chunkSize > 0)
                            {
                                _state = ParsingState.ExpectChunkData;
                                goto case ParsingState.ExpectChunkData;
                            }
                            else
                            {
                                _state = ParsingState.ConsumeTrailers;
                                goto case ParsingState.ConsumeTrailers;
                            }

                        case ParsingState.ExpectChunkData:
                            Debug.Assert(_chunkBytesRemaining > 0);

                            ReadOnlyMemory<byte> connectionBuffer = _connection.RemainingBuffer;
                            if (connectionBuffer.Length == 0)
                            {
                                return default;
                            }

                            int bytesToConsume = Math.Min(maxBytesToRead, (int)Math.Min((ulong)connectionBuffer.Length, _chunkBytesRemaining));
                            Debug.Assert(bytesToConsume > 0 || maxBytesToRead == 0);

                            _connection.ConsumeFromRemainingBuffer(bytesToConsume);
                            _chunkBytesRemaining -= (ulong)bytesToConsume;
                            if (_chunkBytesRemaining == 0)
                            {
                                _state = ParsingState.ExpectChunkTerminator;
                            }

                            return connectionBuffer.Slice(0, bytesToConsume);

                        case ParsingState.ExpectChunkTerminator:
                            Debug.Assert(_chunkBytesRemaining == 0, $"Expected {nameof(_chunkBytesRemaining)} == 0, got {_chunkBytesRemaining}");

                            if (!_connection.TryReadNextChunkedLine(out currentLine))
                            {
                                return default;
                            }

                            if (currentLine.Length != 0)
                            {
                                throw new HttpIOException(HttpRequestError.InvalidResponse, SR.Format(SR.net_http_invalid_response_chunk_terminator_invalid, Encoding.ASCII.GetString(currentLine)));
                            }

                            _state = ParsingState.ExpectChunkHeader;
                            goto case ParsingState.ExpectChunkHeader;

                        case ParsingState.ConsumeTrailers:
                            Debug.Assert(_chunkBytesRemaining == 0, $"Expected {nameof(_chunkBytesRemaining)} == 0, got {_chunkBytesRemaining}");

                            // Consume the receive buffer. If the stream is disposed, pass a null response to avoid
                            // processing headers for a connection returned to the pool.
                            if (_connection.ParseHeaders(IsDisposed ? null : _response, isFromTrailer: true))
                            {
                                // Dispose of the registration and then check whether cancellation has been
                                // requested. This is necessary to make deterministic a race condition between
                                // cancellation being requested and unregistering from the token.  Otherwise,
                                // it's possible cancellation could be requested just before we unregister and
                                // we then return a connection to the pool that has been or will be disposed
                                // (e.g. if a timer is used and has already queued its callback but the
                                // callback hasn't yet run).
                                cancellationRegistration.Dispose();
                                CancellationHelper.ThrowIfCancellationRequested(cancellationRegistration.Token);

                                _state = ParsingState.Done;
                                _connection.CompleteResponse();
                                _connection = null;
                            }

                            return default;

                        default:
                        case ParsingState.Done: // shouldn't be called once we're done
                            Debug.Fail($"Unexpected state: {_state}");
                            if (NetEventSource.Log.IsEnabled())
                            {
                                NetEventSource.Error(this, $"Unexpected state: {_state}");
                            }

                            return default;
                    }
                }
                catch (Exception)
                {
                    // Ensure we don't try to read from the connection again (in particular, for draining)
                    _connection!.Dispose();
                    _connection = null;
                    throw;
                }
            }

            private static void ValidateChunkExtension(ReadOnlySpan<byte> lineAfterChunkSize)
            {
                // Until we see the ';' denoting the extension, the line after the chunk size
                // must contain only tabs and spaces.  After the ';', anything goes.
                for (int i = 0; i < lineAfterChunkSize.Length; i++)
                {
                    byte c = lineAfterChunkSize[i];
                    if (c == ';')
                    {
                        break;
                    }
                    else if (c != ' ' && c != '\t') // not called out in the RFC, but WinHTTP allows it
                    {
                        throw new HttpIOException(HttpRequestError.InvalidResponse, SR.Format(SR.net_http_invalid_response_chunk_extension_invalid, BitConverter.ToString(lineAfterChunkSize.ToArray())));
                    }
                }
            }

            private enum ParsingState : byte
            {
                ExpectChunkHeader,
                ExpectChunkData,
                ExpectChunkTerminator,
                ConsumeTrailers,
                Done
            }

            public override bool NeedsDrain => CanReadFromConnection;

            public override async ValueTask<bool> DrainAsync(int maxDrainBytes)
            {
                Debug.Assert(_connection != null);

                CancellationTokenSource? cts = null;
                CancellationTokenRegistration ctr = default;
                try
                {
                    int drainedBytes = 0;
                    while (true)
                    {
                        drainedBytes += _connection.RemainingBuffer.Length;
                        while (true)
                        {
                            if (ReadChunkFromConnectionBuffer(int.MaxValue, ctr) is not ReadOnlyMemory<byte> bytesRead || bytesRead.Length == 0)
                            {
                                break;
                            }
                        }

                        // When ReadChunkFromConnectionBuffer reads the final chunk, it will clear out _connection
                        // and return the connection to the pool.
                        if (_connection == null)
                        {
                            return true;
                        }

                        if (drainedBytes >= maxDrainBytes)
                        {
                            return false;
                        }

                        if (cts == null) // only create the drain timer if we have to go async
                        {
                            TimeSpan drainTime = _connection._pool.Settings._maxResponseDrainTime;

                            if (drainTime == TimeSpan.Zero)
                            {
                                return false;
                            }

                            if (drainTime != Timeout.InfiniteTimeSpan)
                            {
                                cts = new CancellationTokenSource((int)drainTime.TotalMilliseconds);
                                ctr = cts.Token.Register(static s => ((HttpConnection)s!).Dispose(), _connection);
                            }
                        }

                        await FillAsync().ConfigureAwait(false);
                    }
                }
                finally
                {
                    ctr.Dispose();
                    cts?.Dispose();
                }
            }

            private void Fill()
            {
                Debug.Assert(_connection is not null);
                ValueTask fillTask = _state == ParsingState.ConsumeTrailers
                    ? _connection.FillForHeadersAsync(async: false)
                    : _connection.FillAsync(async: false);
                Debug.Assert(fillTask.IsCompleted);
                fillTask.GetAwaiter().GetResult();
            }

            private ValueTask FillAsync()
            {
                Debug.Assert(_connection is not null);
                return _state == ParsingState.ConsumeTrailers
                    ? _connection.FillForHeadersAsync(async: true)
                    : _connection.FillAsync(async: true);
            }
        }
    }
}
