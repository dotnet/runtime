// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Quic;
using static System.Net.Quic.MsQuicHelpers;
using static Microsoft.Quic.MsQuic;

using NEW_CONNECTION_DATA = Microsoft.Quic.QUIC_LISTENER_EVENT._Anonymous_e__Union._NEW_CONNECTION_e__Struct;
using STOP_COMPLETE_DATA = Microsoft.Quic.QUIC_LISTENER_EVENT._Anonymous_e__Union._STOP_COMPLETE_e__Struct;

namespace System.Net.Quic;

/// <summary>
/// Represents a listener that listens for incoming QUIC connections, see <see href="https://www.rfc-editor.org/rfc/rfc9000.html#name-connections">RFC 9000: Connections</see> for more details.
/// <see cref="QuicListener" /> allows accepting multiple <see cref="QuicConnection" />.
/// </summary>
/// <remarks>
/// Unlike the connection and stream, <see cref="QuicListener" /> lifetime is not linked to any of the accepted connections.
/// It can be safely disposed while keeping the accepted connection alive. The <see cref="DisposeAsync"/> will only stop listening for any other inbound connections.
/// </remarks>
public sealed partial class QuicListener : IAsyncDisposable
{
    /// <summary>
    /// Returns <c>true</c> if QUIC is supported on the current machine and can be used; otherwise, <c>false</c>.
    /// </summary>
    /// <remarks>
    /// The current implementation depends on <see href="https://github.com/microsoft/msquic">MsQuic</see> native library, this property checks its presence (Linux machines).
    /// It also checks whether TLS 1.3, requirement for QUIC protocol, is available and enabled (Windows machines).
    /// </remarks>
    public static bool IsSupported => MsQuicApi.IsQuicSupported;

    /// <summary>
    /// Creates a new <see cref="QuicListener"/> and starts listening for new connections.
    /// </summary>
    /// <param name="options">Options for the listener.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>An asynchronous task that completes with the started listener.</returns>
    public static ValueTask<QuicListener> ListenAsync(QuicListenerOptions options, CancellationToken cancellationToken = default)
    {
        if (!IsSupported)
        {
            throw new PlatformNotSupportedException(SR.Format(SR.SystemNetQuic_PlatformNotSupported, MsQuicApi.NotSupportedReason));
        }

        // Validate and fill in defaults for the options.
        options.Validate(nameof(options));

        QuicListener listener = new QuicListener(options);

        if (NetEventSource.Log.IsEnabled())
        {
            NetEventSource.Info(listener, $"{listener} Listener listens on {listener.LocalEndPoint}");
        }

        return ValueTask.FromResult(listener);
    }

    /// <summary>
    /// Handle to MsQuic listener object.
    /// </summary>
    private readonly MsQuicContextSafeHandle _handle;

    /// <summary>
    /// Set to non-zero once disposed. Prevents double and/or concurrent disposal.
    /// </summary>
    private int _disposed;

    /// <summary>
    /// Completed when SHUTDOWN_COMPLETE arrives.
    /// </summary>
    private readonly ValueTaskSource _shutdownTcs = new ValueTaskSource();

    /// <summary>
    /// Used to stop pending connections when <see cref="DisposeAsync"/> is requested.
    /// </summary>
    private readonly CancellationTokenSource _disposeCts = new CancellationTokenSource();

    /// <summary>
    /// Selects connection options for incoming connections.
    /// </summary>
    private readonly Func<QuicConnection, SslClientHelloInfo, CancellationToken, ValueTask<QuicServerConnectionOptions>> _connectionOptionsCallback;

    /// <summary>
    /// Incoming connections waiting to be accepted via AcceptAsync. The item will either be fully connected <see cref="QuicConnection"/> or <see cref="Exception"/> if the handshake failed.
    /// </summary>
    private readonly Channel<object> _acceptQueue;
    /// <summary>
    /// Allowed number of pending incoming connections.
    /// Actual value correspond to <c><see cref="QuicListenerOptions.ListenBacklog"/> - # <see cref="StartConnectionHandshake"/> in progress - <see cref="_acceptQueue"/>.Count</c> and is always <c>>= 0</c>.
    /// Starts as <see cref="QuicListenerOptions.ListenBacklog"/>, decrements with each NEW_CONNECTION, increments with <see cref="AcceptConnectionAsync" />.
    /// </summary>
    private int _pendingConnectionsCapacity;

    /// <summary>
    /// The actual listening endpoint.
    /// </summary>
    public IPEndPoint LocalEndPoint { get; }

    /// <inheritdoc />
    public override string ToString() => _handle.ToString();

    /// <summary>
    /// Initializes and starts a new instance of a <see cref="QuicListener" />.
    /// </summary>
    /// <param name="options">Options to start the listener.</param>
    private unsafe QuicListener(QuicListenerOptions options)
    {
        GCHandle context = GCHandle.Alloc(this, GCHandleType.Weak);
        try
        {
            QUIC_HANDLE* handle;
            ThrowHelper.ThrowIfMsQuicError(MsQuicApi.Api.ListenerOpen(
                MsQuicApi.Api.Registration,
                &NativeCallback,
                (void*)GCHandle.ToIntPtr(context),
                &handle),
                "ListenerOpen failed");
            _handle = new MsQuicContextSafeHandle(handle, context, SafeHandleType.Listener);
        }
        catch
        {
            context.Free();
            throw;
        }

        // Save the connection options before starting the listener
        _connectionOptionsCallback = options.ConnectionOptionsCallback;
        _acceptQueue = Channel.CreateUnbounded<object>();
        _pendingConnectionsCapacity = options.ListenBacklog;

        // Start the listener, from now on MsQuic events will come.
        using MsQuicBuffers alpnBuffers = new MsQuicBuffers();
        alpnBuffers.Initialize(options.ApplicationProtocols, applicationProtocol => applicationProtocol.Protocol);
        QuicAddr address = options.ListenEndPoint.ToQuicAddr();
        if (options.ListenEndPoint.Address.Equals(IPAddress.IPv6Any))
        {
            // For IPv6Any, MsQuic would listen only for IPv6 connections. This would make it impossible
            // to connect the listener by using the IPv4 address (which could have been e.g. resolved by DNS).
            // Using the Unspecified family makes MsQuic handle connections from all IP addresses.
            address.Family = QUIC_ADDRESS_FAMILY_UNSPEC;
        }
        ThrowHelper.ThrowIfMsQuicError(MsQuicApi.Api.ListenerStart(
            _handle,
            alpnBuffers.Buffers,
            (uint)alpnBuffers.Count,
            &address),
            "ListenerStart failed");

        // Get the actual listening endpoint.
        address = GetMsQuicParameter<QuicAddr>(_handle, QUIC_PARAM_LISTENER_LOCAL_ADDRESS);
        LocalEndPoint = MsQuicHelpers.QuicAddrToIPEndPoint(&address, options.ListenEndPoint.AddressFamily);
    }

    /// <summary>
    /// Accepts an inbound <see cref="QuicConnection" />.
    /// </summary>
    /// <remarks>
    /// Propagates exceptions from <see cref="QuicListenerOptions.ConnectionOptionsCallback"/>, including validation errors from misconfigured <see cref="QuicServerConnectionOptions"/>, e.g. <see cref="ArgumentException"/>.
    /// Also propagates exceptions from failed connection handshake, e.g. <see cref="AuthenticationException"/>, <see cref="QuicException"/>.
    /// </remarks>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that will contain a fully connected <see cref="QuicConnection" /> which successfully finished the handshake and is ready to be used.</returns>
    public async ValueTask<QuicConnection> AcceptConnectionAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed == 1, this);

        GCHandle keepObject = GCHandle.Alloc(this);
        try
        {
            object item = await _acceptQueue.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            Interlocked.Increment(ref _pendingConnectionsCapacity);

            if (item is QuicConnection connection)
            {
                return connection;
            }
            ExceptionDispatchInfo.Throw((Exception)item);
            throw null; // Never reached.
        }
        catch (ChannelClosedException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Throw(ex.InnerException);
            throw;
        }
        finally
        {
            keepObject.Free();
        }
    }

    /// <summary>
    /// Kicks off the handshake process. It doesn't propagate the result outside directly but rather stores it in <c>_acceptQueue</c> for <see cref="AcceptConnectionAsync" />.
    /// </summary>
    /// <remarks>
    /// The method is <c>async void</c> on purpose so it starts an operation but doesn't wait for the result from the caller's perspective.
    /// It does await <see cref="QuicConnection.FinishHandshakeAsync"/> but that never gets propagated to the caller for which the method ends with the first asynchronously processed <c>await</c>.
    /// Once the asynchronous processing finishes, the result is stored in <c>_acceptQueue</c>.
    /// </remarks>
    /// <param name="connection">The new connection.</param>
    /// <param name="clientHello">The TLS ClientHello data.</param>
    private async Task StartConnectionHandshake(QuicConnection connection, SslClientHelloInfo clientHello)
    {
        bool wrapException = false;
        CancellationToken cancellationToken = default;

        // In certain cases MsQuic will not impose the handshake idle timeout on their side, see
        // https://github.com/microsoft/msquic/discussions/2705.
        // This will be assigned to before the linked CTS is cancelled
        TimeSpan handshakeTimeout = QuicDefaults.HandshakeTimeout;
        try
        {
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_disposeCts.Token, connection.ConnectionShutdownToken);
            cancellationToken = linkedCts.Token;
            // Initial timeout for retrieving connection options.
            linkedCts.CancelAfter(handshakeTimeout);

            wrapException = true;
            QuicServerConnectionOptions options = await _connectionOptionsCallback(connection, clientHello, cancellationToken).ConfigureAwait(false);
            wrapException = false;

            options.Validate(nameof(options));

            // Update handshake timeout based on the returned value.
            handshakeTimeout = options.HandshakeTimeout;
            linkedCts.CancelAfter(handshakeTimeout);

            await connection.FinishHandshakeAsync(options, clientHello.ServerName, cancellationToken).ConfigureAwait(false);
            if (!_acceptQueue.Writer.TryWrite(connection))
            {
                // Channel has been closed, dispose the connection as it'll never be handed out.
                await connection.DisposeAsync().ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (connection.ConnectionShutdownToken.IsCancellationRequested)
        {
            // Connection closed by peer
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(connection, $"{connection} Connection closed by remote peer");
            }

            // Retrieve the exception which failed the handshake, the parameters are not going to be
            // validated because the inner _connectedTcs is already transitioned to faulted state.
            ValueTask task = connection.FinishHandshakeAsync(null!, null!, default);
            Debug.Assert(task.IsFaulted);

            // Unwrap AggregateException and propagate it to the accept queue.
            Exception ex = task.AsTask().Exception!.InnerException!;

            await connection.DisposeAsync().ConfigureAwait(false);
            if (!_acceptQueue.Writer.TryWrite(ex))
            {
                // Channel has been closed, connection is already disposed, do nothing.
            }
        }
        catch (OperationCanceledException) when (_disposeCts.IsCancellationRequested)
        {
            // Handshake stopped by QuicListener.DisposeAsync:
            // 1. Dispose the connection and by that shut it down --> application error code doesn't matter here as this is a transport error.
            // 2. Connection won't be handed out since listener has stopped --> do not propagate anything.

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(connection, $"{connection} Connection handshake stopped by listener");
            }

            await connection.DisposeAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException oce) when (cancellationToken.IsCancellationRequested)
        {
            // Handshake cancelled by options.HandshakeTimeout, probably stalled:
            // 1. Connection must be killed so dispose it and by that shut it down --> application error code doesn't matter here as this is a transport error.
            // 2. Connection won't be handed out since it's useless --> propagate appropriate exception, listener will pass it to the caller.

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Error(connection, $"{connection} Connection handshake timed out: {oce}");
            }

            Exception ex = ExceptionDispatchInfo.SetCurrentStackTrace(new QuicException(QuicError.ConnectionTimeout, null, SR.Format(SR.net_quic_handshake_timeout, handshakeTimeout), oce));
            await connection.DisposeAsync().ConfigureAwait(false);
            if (!_acceptQueue.Writer.TryWrite(ex))
            {
                // Channel has been closed, connection is already disposed, do nothing.
            }
        }
        catch (Exception ex)
        {
            // Handshake failed:
            // 1. Dispose the connection and by that shut it down --> application error code doesn't matter here as this is a transport error.
            // 2. Connection cannot be handed out since it's useless --> propagate the exception as-is, listener will pass it to the caller.

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Error(connection, $"{connection} Connection handshake failed: {ex}");
            }

            await connection.DisposeAsync().ConfigureAwait(false);
            if (!_acceptQueue.Writer.TryWrite(
                    wrapException ?
                        ExceptionDispatchInfo.SetCurrentStackTrace(new QuicException(QuicError.CallbackError, null, SR.net_quic_callback_error, ex)) :
                        ex))
            {
                // Channel has been closed, connection is already disposed, do nothing.
            }
        }
    }

    private unsafe int HandleEventNewConnection(ref NEW_CONNECTION_DATA data)
    {
        // Check if there's capacity to have another connection waiting to be accepted.
        if (Interlocked.Decrement(ref _pendingConnectionsCapacity) < 0)
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(this, $"{this} Refusing connection from {MsQuicHelpers.QuicAddrToIPEndPoint(data.Info->RemoteAddress)} due to backlog limit");
            }

            Interlocked.Increment(ref _pendingConnectionsCapacity);
            return QUIC_STATUS_CONNECTION_REFUSED;
        }

        QuicConnection connection = new QuicConnection(data.Connection, data.Info);
        SslClientHelloInfo clientHello = new SslClientHelloInfo(data.Info->ServerNameLength > 0 ? Marshal.PtrToStringUTF8((IntPtr)data.Info->ServerName, data.Info->ServerNameLength) : "", SslProtocols.Tls13);

        // Kicks off the rest of the handshake in the background, the process itself will enqueue the result in the accept queue.
        // This also makes sure the connection options callback provided by the user is not invoked
        // from the MsQuic thread and cannot delay acks or other operations on other connections.
        _ = Task.Run(() => StartConnectionHandshake(connection, clientHello));

        return QUIC_STATUS_SUCCESS;

    }
    private unsafe int HandleEventStopComplete()
    {
        _shutdownTcs.TrySetResult();
        return QUIC_STATUS_SUCCESS;
    }

    private unsafe int HandleListenerEvent(ref QUIC_LISTENER_EVENT listenerEvent)
        => listenerEvent.Type switch
        {
            QUIC_LISTENER_EVENT_TYPE.NEW_CONNECTION => HandleEventNewConnection(ref listenerEvent.NEW_CONNECTION),
            QUIC_LISTENER_EVENT_TYPE.STOP_COMPLETE => HandleEventStopComplete(),
            _ => QUIC_STATUS_SUCCESS
        };

#pragma warning disable CS3016
    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvCdecl) })]
#pragma warning restore CS3016
    private static unsafe int NativeCallback(QUIC_HANDLE* listener, void* context, QUIC_LISTENER_EVENT* listenerEvent)
    {
        GCHandle stateHandle = GCHandle.FromIntPtr((IntPtr)context);

        // Check if the instance hasn't been collected.
        if (!stateHandle.IsAllocated || stateHandle.Target is not QuicListener instance)
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Error(null, $"Received event {listenerEvent->Type} for [list][{(nint)listener:X11}] while listener is already disposed");
            }
            return QUIC_STATUS_INVALID_STATE;
        }

        try
        {
            // Process the event.
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(instance, $"{instance} Received event {listenerEvent->Type} {listenerEvent->ToString()}");
            }
            return instance.HandleListenerEvent(ref *listenerEvent);
        }
        catch (Exception ex)
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Error(instance, $"{instance} Exception while processing event {listenerEvent->Type}: {ex}");
            }
            return QUIC_STATUS_INTERNAL_ERROR;
        }
    }

    /// <summary>
    /// Stops listening for new connections and releases all resources associated with the listener.
    /// </summary>
    /// <returns>A task that represents the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        // Check if the listener has been shut down and if not, shut it down.
        if (_shutdownTcs.TryInitialize(out ValueTask valueTask, this))
        {
            unsafe
            {
                MsQuicApi.Api.ListenerStop(_handle);
            }
        }

        // Wait for STOP_COMPLETE, the last event, so that all resources can be safely released.
        await valueTask.ConfigureAwait(false);
        _handle.Dispose();

        // Flush the queue and dispose all remaining connections.
        await _disposeCts.CancelAsync().ConfigureAwait(false);
        _acceptQueue.Writer.TryComplete(ExceptionDispatchInfo.SetCurrentStackTrace(new ObjectDisposedException(GetType().FullName)));
        while (_acceptQueue.Reader.TryRead(out object? item))
        {
            if (item is QuicConnection connection)
            {
                await connection.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
