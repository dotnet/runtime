// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal abstract class HttpContentWriteStream : HttpContentStream
    {
        public HttpContentWriteStream(HttpConnection connection) : base(connection) =>
            Debug.Assert(connection != null);

        public sealed override bool CanRead => false;
        public sealed override bool CanWrite => true;

        public sealed override void Flush() => FlushAsync().GetAwaiter().GetResult();

        public sealed override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public sealed override void Write(byte[] buffer, int offset, int count) =>
            WriteAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();

        public abstract Task FinishAsync();
    }
}
