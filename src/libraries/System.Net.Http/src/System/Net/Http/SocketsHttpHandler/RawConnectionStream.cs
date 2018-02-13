// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal partial class HttpConnection : IDisposable
    {
        private sealed class RawConnectionStream : HttpContentDuplexStream
        {
            public RawConnectionStream(HttpConnection connection) : base(connection)
            {
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                ValidateBufferArgs(buffer, offset, count);
                return ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
            }

            public override async ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (_connection == null || destination.Length == 0)
                {
                    // Response body fully consumed or the caller didn't ask for any data
                    return 0;
                }

                ValueTask<int> readTask = _connection.ReadAsync(destination);
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
                    catch (Exception exc) when (ShouldWrapInOperationCanceledException(exc, cancellationToken))
                    {
                        throw CreateOperationCanceledException(exc, cancellationToken);
                    }
                    finally
                    {
                        ctr.Dispose();
                    }
                }

                if (bytesRead == 0)
                {
                    // A cancellation request may have caused the EOF.
                    cancellationToken.ThrowIfCancellationRequested();

                    // We cannot reuse this connection, so close it.
                    _connection.Dispose();
                    _connection = null;
                    return 0;
                }

                return bytesRead;
            }

            public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
            {
                if (destination == null)
                {
                    throw new ArgumentNullException(nameof(destination));
                }
                if (bufferSize <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(bufferSize));
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (_connection == null)
                {
                    // Response body fully consumed
                    return;
                }

                Task copyTask = _connection.CopyToAsync(destination);
                if (!copyTask.IsCompletedSuccessfully)
                {
                    CancellationTokenRegistration ctr = _connection.RegisterCancellation(cancellationToken);
                    try
                    {
                        await copyTask.ConfigureAwait(false);
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

                // We cannot reuse this connection, so close it.
                _connection.Dispose();
                _connection = null;
            }

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                ValidateBufferArgs(buffer, offset, count);
                return WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken);
            }

            public override Task WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return Task.FromCanceled(cancellationToken);
                }

                if (_connection == null)
                {
                    return Task.FromException(new IOException(SR.net_http_io_write));
                }

                if (source.Length == 0)
                {
                    return Task.CompletedTask;
                }

                Task writeTask = _connection.WriteWithoutBufferingAsync(source);
                return writeTask.IsCompleted ?
                    writeTask :
                    WaitWithConnectionCancellationAsync(writeTask, cancellationToken);
            }

            public override Task FlushAsync(CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return Task.FromCanceled(cancellationToken);
                }

                if (_connection == null)
                {
                    return Task.CompletedTask;
                }

                Task flushTask = _connection.FlushAsync();
                return flushTask.IsCompleted ?
                    flushTask :
                    WaitWithConnectionCancellationAsync(flushTask, cancellationToken);
            }

            private async Task WaitWithConnectionCancellationAsync(Task task, CancellationToken cancellationToken)
            {
                CancellationTokenRegistration ctr = _connection.RegisterCancellation(cancellationToken);
                try
                {
                    await task.ConfigureAwait(false);
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
