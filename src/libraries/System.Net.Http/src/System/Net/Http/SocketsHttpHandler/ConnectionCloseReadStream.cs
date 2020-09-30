// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal partial class HttpConnection : IDisposable
    {
        private sealed class ConnectionCloseReadStream : HttpContentReadStream
        {
            public ConnectionCloseReadStream(HttpConnection connection) : base(connection)
            {
            }

            public override int Read(Span<byte> buffer)
            {
                HttpConnection? connection = _connection;
                if (connection == null || buffer.Length == 0)
                {
                    // Response body fully consumed or the caller didn't ask for any data
                    return 0;
                }

                int bytesRead = connection.Read(buffer);
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

                ValueTask<int> readTask = connection.ReadAsync(buffer);
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
                    // If cancellation is requested and tears down the connection, it could cause the read
                    // to return 0, which would otherwise signal the end of the data, but that would lead
                    // the caller to think that it actually received all of the data, rather than it ending
                    // early due to cancellation.  So we prioritize cancellation in this race condition, and
                    // if we read 0 bytes and then find that cancellation has requested, we assume cancellation
                    // was the cause and throw.
                    CancellationHelper.ThrowIfCancellationRequested(cancellationToken);

                    // We cannot reuse this connection, so close it.
                    _connection = null;
                    connection.Dispose();
                }

                return bytesRead;
            }

            public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
            {
                ValidateCopyToArgs(this, destination, bufferSize);

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
                _connection = null;
                connection.Dispose();
            }
        }
    }
}
