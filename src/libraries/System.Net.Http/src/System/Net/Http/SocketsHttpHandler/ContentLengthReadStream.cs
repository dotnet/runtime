// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed partial class HttpConnection : IDisposable
    {
        private sealed class ContentLengthReadStream : HttpContentReadStream
        {
            private ulong _contentBytesRemaining;

            public ContentLengthReadStream(HttpConnection connection, ulong contentLength) : base(connection)
            {
                Debug.Assert(contentLength > 0, "Caller should have checked for 0.");
                _contentBytesRemaining = contentLength;
            }

            public override int Read(Span<byte> buffer)
            {
                if (_connection == null || buffer.Length == 0)
                {
                    // Response body fully consumed or the caller didn't ask for any data.
                    return 0;
                }

                Debug.Assert(_contentBytesRemaining > 0);
                if ((ulong)buffer.Length > _contentBytesRemaining)
                {
                    buffer = buffer.Slice(0, (int)_contentBytesRemaining);
                }

                int bytesRead = _connection.Read(buffer);
                if (bytesRead <= 0)
                {
                    // Unexpected end of response stream.
                    throw new IOException(SR.Format(SR.net_http_invalid_response_premature_eof_bytecount, _contentBytesRemaining));
                }

                Debug.Assert((ulong)bytesRead <= _contentBytesRemaining);
                _contentBytesRemaining -= (ulong)bytesRead;

                if (_contentBytesRemaining == 0)
                {
                    // End of response body
                    _connection.CompleteResponse();
                    _connection = null;
                }

                return bytesRead;
            }

            public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
            {
                CancellationHelper.ThrowIfCancellationRequested(cancellationToken);

                if (_connection == null || buffer.Length == 0)
                {
                    // Response body fully consumed or the caller didn't ask for any data
                    return 0;
                }

                Debug.Assert(_contentBytesRemaining > 0);

                if ((ulong)buffer.Length > _contentBytesRemaining)
                {
                    buffer = buffer.Slice(0, (int)_contentBytesRemaining);
                }

                ValueTask<int> readTask = _connection.ReadAsync(buffer);
                int bytesRead;
                if (readTask.IsCompletedSuccessfully)
                {
                    bytesRead = readTask.Result;
                }
                else
                {
                    CancellationTokenRegistration ctr = _connection.RegisterCancellation(cancellationToken);
                    try
                    {
                        bytesRead = await readTask.ConfigureAwait(false);
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

                if (bytesRead <= 0)
                {
                    // A cancellation request may have caused the EOF.
                    CancellationHelper.ThrowIfCancellationRequested(cancellationToken);

                    // Unexpected end of response stream.
                    throw new IOException(SR.Format(SR.net_http_invalid_response_premature_eof_bytecount, _contentBytesRemaining));
                }

                Debug.Assert((ulong)bytesRead <= _contentBytesRemaining);
                _contentBytesRemaining -= (ulong)bytesRead;

                if (_contentBytesRemaining == 0)
                {
                    // End of response body
                    _connection.CompleteResponse();
                    _connection = null;
                }

                return bytesRead;
            }

            public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
            {
                ValidateCopyToArguments(destination, bufferSize);

                if (cancellationToken.IsCancellationRequested)
                {
                    return Task.FromCanceled(cancellationToken);
                }

                if (_connection == null)
                {
                    // null if response body fully consumed
                    return Task.CompletedTask;
                }

                Task copyTask = _connection.CopyToContentLengthAsync(destination, async: true, _contentBytesRemaining, bufferSize, cancellationToken);
                if (copyTask.IsCompletedSuccessfully)
                {
                    Finish();
                    return Task.CompletedTask;
                }

                return CompleteCopyToAsync(copyTask, cancellationToken);
            }

            private async Task CompleteCopyToAsync(Task copyTask, CancellationToken cancellationToken)
            {
                Debug.Assert(_connection != null);
                CancellationTokenRegistration ctr = _connection.RegisterCancellation(cancellationToken);
                try
                {
                    await copyTask.ConfigureAwait(false);
                }
                catch (Exception exc) when (CancellationHelper.ShouldWrapInOperationCanceledException(exc, cancellationToken))
                {
                    throw CancellationHelper.CreateOperationCanceledException(exc, cancellationToken);
                }
                finally
                {
                    ctr.Dispose();
                }

                Finish();
            }

            private void Finish()
            {
                _contentBytesRemaining = 0;
                _connection!.CompleteResponse();
                _connection = null;
            }

            // Based on ReadChunkFromConnectionBuffer; perhaps we should refactor into a common routine.
            private ReadOnlyMemory<byte> ReadFromConnectionBuffer(int maxBytesToRead)
            {
                Debug.Assert(maxBytesToRead > 0);
                Debug.Assert(_contentBytesRemaining > 0);
                Debug.Assert(_connection != null);

                ReadOnlyMemory<byte> connectionBuffer = _connection.RemainingBuffer;
                if (connectionBuffer.Length == 0)
                {
                    return default;
                }

                int bytesToConsume = Math.Min(maxBytesToRead, (int)Math.Min((ulong)connectionBuffer.Length, _contentBytesRemaining));
                Debug.Assert(bytesToConsume > 0);

                _connection.ConsumeFromRemainingBuffer(bytesToConsume);
                _contentBytesRemaining -= (ulong)bytesToConsume;

                return connectionBuffer.Slice(0, bytesToConsume);
            }

            public override bool NeedsDrain => (_connection != null);

            public override async ValueTask<bool> DrainAsync(int maxDrainBytes)
            {
                Debug.Assert(_connection != null);
                Debug.Assert(_contentBytesRemaining > 0);

                ReadFromConnectionBuffer(int.MaxValue);
                if (_contentBytesRemaining == 0)
                {
                    Finish();
                    return true;
                }

                if (_contentBytesRemaining > (ulong)maxDrainBytes)
                {
                    return false;
                }

                CancellationTokenSource? cts = null;
                CancellationTokenRegistration ctr = default;
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

                try
                {
                    while (true)
                    {
                        await _connection.FillAsync(async: true).ConfigureAwait(false);
                        ReadFromConnectionBuffer(int.MaxValue);
                        if (_contentBytesRemaining == 0)
                        {
                            // Dispose of the registration and then check whether cancellation has been
                            // requested. This is necessary to make determinstic a race condition between
                            // cancellation being requested and unregistering from the token.  Otherwise,
                            // it's possible cancellation could be requested just before we unregister and
                            // we then return a connection to the pool that has been or will be disposed
                            // (e.g. if a timer is used and has already queued its callback but the
                            // callback hasn't yet run).
                            ctr.Dispose();
                            CancellationHelper.ThrowIfCancellationRequested(ctr.Token);

                            Finish();
                            return true;
                        }
                    }
                }
                finally
                {
                    ctr.Dispose();
                    cts?.Dispose();
                }
            }
        }
    }
}
