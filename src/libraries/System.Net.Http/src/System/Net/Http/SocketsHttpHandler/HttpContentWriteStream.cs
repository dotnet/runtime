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
        private abstract class HttpContentWriteStream : HttpContentStream
        {
            public HttpContentWriteStream(HttpConnection connection) : base(connection) =>
                Debug.Assert(connection != null);

            public sealed override bool CanRead => false;
            public sealed override bool CanWrite => true;

            public sealed override void Flush() =>
                _connection.Flush();

            public sealed override Task FlushAsync(CancellationToken ignored) =>
                _connection.FlushAsync().AsTask();

            public sealed override int Read(Span<byte> buffer) => throw new NotSupportedException();

            public sealed override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken) => throw new NotSupportedException();

            public sealed override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) => throw new NotSupportedException();

            public abstract Task FinishAsync();
        }
    }
}
