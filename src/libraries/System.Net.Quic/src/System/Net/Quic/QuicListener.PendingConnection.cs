// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Quic;

public sealed partial class QuicListener
{
    private sealed class PendingConnection : IAsyncDisposable
    {
        private static readonly TimeSpan _handshakeTimeout = TimeSpan.FromSeconds(10);

        private readonly TaskCompletionSource<QuicConnection?> _finishHandshakeTask;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public PendingConnection()
        {
            _finishHandshakeTask = new TaskCompletionSource<QuicConnection?>();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async void StartHandshake(QuicConnection connection, SslClientHelloInfo clientHello, Func<QuicConnection, SslClientHelloInfo, CancellationToken, ValueTask<QuicServerConnectionOptions>> connectionOptionsCallback)
        {
            try
            {
                _cancellationTokenSource.CancelAfter(_handshakeTimeout);
                var options = await connectionOptionsCallback(connection, clientHello, _cancellationTokenSource.Token).ConfigureAwait(false);
                await connection.FinishHandshakeAsync(options, clientHello.ServerName, _cancellationTokenSource.Token).ConfigureAwait(false);
                _finishHandshakeTask.SetResult(connection);
            }
            catch (Exception ex)
            {
                // Handshake failed:
                // 1. Connection cannot be handed out since it's useless --> return null, listener will wait for another one.
                // 2. Shutdown the connection to send a transport error to the peer --> application error code doesn't matter here, use default.

                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Error(connection, $"Connection handshake failed: {ex}");
                }

                await connection.CloseAsync(default).ConfigureAwait(false);
                connection.Dispose();
                _finishHandshakeTask.SetResult(null);
            }
        }

        public ValueTask<QuicConnection?> FinishHandshakeAsync(CancellationToken cancellationToken = default)
            => new(_finishHandshakeTask.Task.WaitAsync(cancellationToken));

        public ValueTask DisposeAsync()
        {
            _cancellationTokenSource.Cancel();
            return new ValueTask(_finishHandshakeTask.Task);
        }
    }
}
