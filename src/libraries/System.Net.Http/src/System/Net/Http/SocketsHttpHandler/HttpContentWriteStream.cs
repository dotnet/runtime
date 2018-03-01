// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal partial class HttpConnection : IDisposable
    {
        private abstract class HttpContentWriteStream : HttpContentStream
        {
            public HttpContentWriteStream(HttpConnection connection) : base(connection) =>
                Debug.Assert(connection != null);

            public sealed override bool CanRead => false;
            public sealed override bool CanWrite => true;

            public sealed override void Flush() => FlushAsync().GetAwaiter().GetResult();

            public sealed override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

            public sealed override void Write(byte[] buffer, int offset, int count) =>
                WriteAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();

            // The token here is ignored because it's coming from SendAsync and the only operations
            // here are those that are already covered by the token having been registered with
            // to close the connection.

            public sealed override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken ignored)
            {
                ValidateBufferArgs(buffer, offset, count);
                return WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), ignored).AsTask();
            }

            public sealed override Task FlushAsync(CancellationToken ignored) =>
                _connection.FlushAsync().AsTask();

            public abstract Task FinishAsync();
        }
    }
}
