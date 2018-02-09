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
        private sealed class ConnectionCloseReadStream : HttpContentReadStream
        {
            public ConnectionCloseReadStream(HttpConnection connection) : base(connection)
            {
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                ValidateBufferArgs(buffer, offset, count);
                return ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
            }

            public override async ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default)
            {
                if (_connection == null || destination.Length == 0)
                {
                    // Response body fully consumed or the caller didn't ask for any data
                    return 0;
                }

                int bytesRead = await _connection.ReadAsync(destination, cancellationToken).ConfigureAwait(false);
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
        }
    }
}
