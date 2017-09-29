// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed partial class HttpConnection : IDisposable
    {
        private sealed class RawConnectionStream : HttpContentDuplexStream
        {
            public RawConnectionStream(HttpConnection connection) : base(connection)
            {
            }

            public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                ValidateBufferArgs(buffer, offset, count);

                if (_connection == null || count == 0)
                {
                    // Response body fully consumed or the caller didn't ask for any data
                    return 0;
                }

                int bytesRead = await _connection.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
                if (bytesRead == 0)
                {
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

                if (_connection != null) // null if response body fully consumed
                {
                    await _connection.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);

                    // We cannot reuse this connection, so close it.
                    _connection.Dispose();
                    _connection = null;
                }
            }

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                ValidateBufferArgs(buffer, offset, count);
                return
                    _connection == null ? Task.FromException(new IOException(SR.net_http_io_write)) :
                    count > 0 ? _connection.WriteWithoutBufferingAsync(buffer, offset, count, cancellationToken) :
                    Task.CompletedTask;
            }

            public override Task FlushAsync(CancellationToken cancellationToken) =>
                _connection != null ? _connection.FlushAsync(cancellationToken) :
                Task.CompletedTask;
        }
    }
}
