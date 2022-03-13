// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed partial class HttpConnection
    {
        internal abstract class HttpContentReadStream : HttpContentStream
        {
            private int _disposed; // 0==no, 1==yes

            public HttpContentReadStream(HttpConnection connection) : base(connection)
            {
            }

            public sealed override bool CanRead => _disposed == 0;
            public sealed override bool CanWrite => false;

            public sealed override void Write(ReadOnlySpan<byte> buffer) => throw new NotSupportedException(SR.net_http_content_readonly_stream);

            public sealed override ValueTask WriteAsync(ReadOnlyMemory<byte> destination, CancellationToken cancellationToken) => throw new NotSupportedException();

            public virtual bool NeedsDrain => false;

            protected bool IsDisposed => _disposed == 1;

            protected bool CanReadFromConnection
            {
                get
                {
                    // _connection == null typically means that we have finished reading the response.
                    // Cancellation may lead to a state where a disposed _connection is not null.
                    HttpConnection? connection = _connection;
                    return connection != null && connection._disposed != Status_Disposed;
                }
            }

            public virtual ValueTask<bool> DrainAsync(int maxDrainBytes)
            {
                Debug.Fail($"DrainAsync should not be called for this response stream: {GetType()}");
                return new ValueTask<bool>(false);
            }

            protected override void Dispose(bool disposing)
            {
                // Only attempt draining if we haven't started draining due to disposal; otherwise
                // multiple calls to Dispose (which happens frequently when someone disposes of the
                // response stream and response content) will kick off multiple concurrent draining
                // operations. Also don't delegate to the base if Dispose has already been called,
                // as doing so will end up disposing of the connection before we're done draining.
                if (Interlocked.Exchange(ref _disposed, 1) != 0)
                {
                    return;
                }

                if (disposing && NeedsDrain)
                {
                    // Start the asynchronous drain.
                    // It may complete synchronously, in which case the connection will be put back in the pool synchronously.
                    // Skip the call to base.Dispose -- it will be deferred until DrainOnDisposeAsync finishes.
                    _ = DrainOnDisposeAsync();
                    return;
                }

                base.Dispose(disposing);
            }

            private async Task DrainOnDisposeAsync()
            {
                HttpConnection? connection = _connection;        // Will be null after drain succeeds
                Debug.Assert(connection != null);
                try
                {
                    bool drained = await DrainAsync(connection._pool.Settings._maxResponseDrainSize).ConfigureAwait(false);

                    if (NetEventSource.Log.IsEnabled())
                    {
                        connection.Trace(drained ?
                            "Connection drain succeeded" :
                            $"Connection drain failed when MaxResponseDrainSize={connection._pool.Settings._maxResponseDrainSize} bytes or MaxResponseDrainTime=={connection._pool.Settings._maxResponseDrainTime} exceeded");
                    }
                }
                catch (Exception e)
                {
                    if (NetEventSource.Log.IsEnabled())
                    {
                        connection.Trace($"Connection drain failed due to exception: {e}");
                    }

                    // Eat any exceptions and just Dispose.
                }

                base.Dispose(true);
            }
        }
    }
}
