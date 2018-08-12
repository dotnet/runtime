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

            public sealed override Task FlushAsync(CancellationToken ignored) =>
                _connection.FlushAsync().AsTask();

            public abstract Task FinishAsync();

            public sealed override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken) => throw new NotSupportedException();
        }
    }
}
