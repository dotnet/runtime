// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal partial class HttpConnection : IDisposable
    {
        private sealed class ContentLengthReadStream : HttpContentReadStream
        {
            private ulong _contentBytesRemaining;

            public ContentLengthReadStream(HttpConnection connection, ulong contentLength) : base(connection)
            {
                Debug.Assert(contentLength > 0, "Caller should have checked for 0.");
                _contentBytesRemaining = contentLength;
            }

            public override async ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (_connection == null || destination.Length == 0)
                {
                    // Response body fully consumed or the caller didn't ask for any data
                    return 0;
                }

                Debug.Assert(_contentBytesRemaining > 0);

                if ((ulong)destination.Length > _contentBytesRemaining)
                {
                    destination = destination.Slice(0, (int)_contentBytesRemaining);
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

                if (bytesRead <= 0)
                {
                    // A cancellation request may have caused the EOF.
                    cancellationToken.ThrowIfCancellationRequested();

                    // Unexpected end of response stream.
                    throw new IOException(SR.net_http_invalid_response);
                }

                Debug.Assert((ulong)bytesRead <= _contentBytesRemaining);
                _contentBytesRemaining -= (ulong)bytesRead;

                if (_contentBytesRemaining == 0)
                {
                    // End of response body
                    _connection.ReturnConnectionToPool();
                    _connection = null;
                }

                return bytesRead;
            }

            public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
            {
                ValidateCopyToArgs(this, destination, bufferSize);

                return
                    cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) :
                    _connection != null ? CopyToAsyncCore(destination, bufferSize, cancellationToken) :
                    Task.CompletedTask; // null if response body fully consumed
            }

            private async Task CopyToAsyncCore(Stream destination, int bufferSize, CancellationToken cancellationToken)
            {
                Task copyTask = _connection.CopyToAsync(destination, _contentBytesRemaining);
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

                _contentBytesRemaining = 0;
                _connection.ReturnConnectionToPool();
                _connection = null;
            }
        }
    }
}
