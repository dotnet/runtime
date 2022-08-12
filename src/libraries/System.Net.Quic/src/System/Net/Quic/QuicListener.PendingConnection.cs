// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Quic;

public sealed partial class QuicListener
{
    /// <summary>
    /// Represents a connection that's been received via NEW_CONNECTION but not accepted yet.
    /// </summary>
    /// <remarks>
    /// When a new connection is being received, the handshake process needs to get started.
    /// More specifically, the server-side connection options, including server certificate, need to selected and provided back to MsQuic.
    /// Finally, after the handshake completes and the connection is established, the result needs to be stored and subsequently retrieved from within <see cref="AcceptConnectionAsync" />.
    /// </remarks>
    private sealed class PendingConnection : IAsyncDisposable
    {
        /// <summary>
        /// Our own imposed timeout in the handshake process, since in certain cases MsQuic will not impose theirs, see <see href="https://github.com/microsoft/msquic/discussions/2705"/>.
        /// </summary>
        /// <returns></returns>
        private static readonly TimeSpan s_handshakeTimeout = TimeSpan.FromSeconds(10);

        /// <summary>
        /// It will contain the established <see cref="QuicConnection" /> in case of a successful handshake; otherwise, <c>null</c>.
        /// </summary>
        private readonly TaskCompletionSource<QuicConnection> _finishHandshakeTask;
        /// <summary>
        /// Use to impose the handshake timeout.
        /// </summary>
        private readonly CancellationTokenSource _cancellationTokenSource;

        public PendingConnection()
        {
            _finishHandshakeTask = new TaskCompletionSource<QuicConnection>(TaskCreationOptions.RunContinuationsAsynchronously);
            _cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Kicks off the handshake process. It doesn't propagate the result outside directly but rather stores it in a task available via <see cref="FinishHandshakeAsync"/>.
        /// </summary>
        /// <remarks>
        /// The method is <c>async void</c> on purpose so it starts an operation but doesn't wait for the result from the caller's perspective.
        /// It does await <see cref="QuicConnection.FinishHandshakeAsync"/> but that never gets propagated to the caller for which the method ends with the first asynchronously processed <c>await</c>.
        /// Once the asynchronous processing finishes, the result is stored in the task field that gets exposed via <see cref="FinishHandshakeAsync"/>.
        /// </remarks>
        /// <param name="connection">The new connection.</param>
        /// <param name="clientHello">The TLS ClientHello data.</param>
        /// <param name="connectionOptionsCallback">The connection options selection callback.</param>
        public async void StartHandshake(QuicConnection connection, SslClientHelloInfo clientHello, Func<QuicConnection, SslClientHelloInfo, CancellationToken, ValueTask<QuicServerConnectionOptions>> connectionOptionsCallback)
        {
            try
            {
                _cancellationTokenSource.CancelAfter(s_handshakeTimeout);
                QuicServerConnectionOptions options = await connectionOptionsCallback(connection, clientHello, _cancellationTokenSource.Token).ConfigureAwait(false);
                options.Validate(nameof(options)); // Validate and fill in defaults for the options.
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
                    NetEventSource.Error(connection, $"{connection} Connection handshake failed: {ex}");
                }

                await connection.CloseAsync(default).ConfigureAwait(false);
                await connection.DisposeAsync().ConfigureAwait(false);
                _finishHandshakeTask.SetException(ex);
            }
        }

        /// <summary>
        /// Provides access to the result of the handshake started with <see cref="StartHandshake(QuicConnection, SslClientHelloInfo, Func{QuicConnection, SslClientHelloInfo, CancellationToken, ValueTask{QuicServerConnectionOptions}})"/>.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>An asynchronous task that completes with the established connection if it succeeded or <c>null</c> if it failed.</returns>
        public ValueTask<QuicConnection> FinishHandshakeAsync(CancellationToken cancellationToken = default)
            => new(_finishHandshakeTask.Task.WaitAsync(cancellationToken));


        /// <summary>
        /// Cancels the handshake in progress and awaits for it so that the connection can be safely cleaned from the listener queue.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            _cancellationTokenSource.Cancel();
            try
            {
                await _finishHandshakeTask.Task.ConfigureAwait(false);
            }
            catch
            {
                // Just swallow the exception, we don't want it to propagate outside of dispose and it has already been logged in StartHandshake catch block.
            }
        }
    }
}
