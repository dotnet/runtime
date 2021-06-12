// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed partial class HttpConnection : IDisposable
    {
        private sealed class RawConnectionStream : HttpContentStream
        {
            public RawConnectionStream(HttpConnection connection) : base(connection)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this);
            }

            public sealed override bool CanRead => _connection != null;
            public sealed override bool CanWrite => _connection != null;

            public override int Read(Span<byte> buffer)
            {
                HttpConnection? connection = _connection;
                if (connection == null || buffer.Length == 0)
                {
                    // Response body fully consumed or the caller didn't ask for any data
                    return 0;
                }

                int bytesRead = connection.ReadBuffered(buffer);
                if (bytesRead == 0)
                {
                    // We cannot reuse this connection, so close it.
                    _connection = null;
                    connection.Dispose();
                }

                return bytesRead;
            }

            public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
            {
                CancellationHelper.ThrowIfCancellationRequested(cancellationToken);

                HttpConnection? connection = _connection;
                if (connection == null || buffer.Length == 0)
                {
                    // Response body fully consumed or the caller didn't ask for any data
                    return 0;
                }

                ValueTask<int> readTask = connection.ReadBufferedAsync(buffer);
                int bytesRead;
                if (readTask.IsCompletedSuccessfully)
                {
                    bytesRead = readTask.Result;
                }
                else
                {
                    CancellationTokenRegistration ctr = connection.RegisterCancellation(cancellationToken);
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

                if (bytesRead == 0)
                {
                    // A cancellation request may have caused the EOF.
                    CancellationHelper.ThrowIfCancellationRequested(cancellationToken);

                    // We cannot reuse this connection, so close it.
                    _connection = null;
                    connection.Dispose();
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

                HttpConnection? connection = _connection;
                if (connection == null)
                {
                    // null if response body fully consumed
                    return Task.CompletedTask;
                }

                Task copyTask = connection.CopyToUntilEofAsync(destination, async: true, bufferSize, cancellationToken);
                if (copyTask.IsCompletedSuccessfully)
                {
                    Finish(connection);
                    return Task.CompletedTask;
                }

                return CompleteCopyToAsync(copyTask, connection, cancellationToken);
            }

            private async Task CompleteCopyToAsync(Task copyTask, HttpConnection connection, CancellationToken cancellationToken)
            {
                CancellationTokenRegistration ctr = connection.RegisterCancellation(cancellationToken);
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

                // If cancellation is requested and tears down the connection, it could cause the copy
                // to end early but think it ended successfully. So we prioritize cancellation in this
                // race condition, and if we find after the copy has completed that cancellation has
                // been requested, we assume the copy completed due to cancellation and throw.
                CancellationHelper.ThrowIfCancellationRequested(cancellationToken);

                Finish(connection);
            }

            private void Finish(HttpConnection connection)
            {
                // We cannot reuse this connection, so close it.
                connection.Dispose();
                _connection = null;
            }

            public override void Write(ReadOnlySpan<byte> buffer)
            {
                HttpConnection? connection = _connection;
                if (connection == null)
                {
                    throw new IOException(SR.ObjectDisposed_StreamClosed);
                }

                if (buffer.Length != 0)
                {
                    connection.WriteWithoutBuffering(buffer);
                }
            }

            public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return ValueTask.FromCanceled(cancellationToken);
                }

                HttpConnection? connection = _connection;
                if (connection == null)
                {
                    return ValueTask.FromException(ExceptionDispatchInfo.SetCurrentStackTrace(new IOException(SR.ObjectDisposed_StreamClosed)));
                }

                if (buffer.Length == 0)
                {
                    return default;
                }

                ValueTask writeTask = connection.WriteWithoutBufferingAsync(buffer, async: true);
                return writeTask.IsCompleted ?
                    writeTask :
                    new ValueTask(WaitWithConnectionCancellationAsync(writeTask, connection, cancellationToken));
            }

            public override void Flush() => _connection?.Flush();

            public override Task FlushAsync(CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return Task.FromCanceled(cancellationToken);
                }

                HttpConnection? connection = _connection;
                if (connection == null)
                {
                    return Task.CompletedTask;
                }

                ValueTask flushTask = connection.FlushAsync(async: true);
                return flushTask.IsCompleted ?
                    flushTask.AsTask() :
                    WaitWithConnectionCancellationAsync(flushTask, connection, cancellationToken);
            }

            private static async Task WaitWithConnectionCancellationAsync(ValueTask task, HttpConnection connection, CancellationToken cancellationToken)
            {
                CancellationTokenRegistration ctr = connection.RegisterCancellation(cancellationToken);
                try
                {
                    await task.ConfigureAwait(false);
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
        }
    }
}
