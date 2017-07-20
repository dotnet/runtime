// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed partial class HttpConnection : IDisposable
    {
        public sealed class ContentLengthWriteStream : HttpContentWriteStream
        {
            public ContentLengthWriteStream(HttpConnection connection)
                : base(connection)
            {
            }

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                ValidateBufferArgs(buffer, offset, count);
                return _connection.WriteAsync(buffer, offset, count, cancellationToken);
            }

            public override Task FlushAsync(CancellationToken cancellationToken)
            {
                return _connection.FlushAsync(cancellationToken);
            }

            public override Task FinishAsync(CancellationToken cancellationToken)
            {
                _connection = null;
                return Task.CompletedTask;
            }
        }
    }
}
