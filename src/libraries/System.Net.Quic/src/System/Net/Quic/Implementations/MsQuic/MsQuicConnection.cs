// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Net.Quic.Implementations.MsQuic.Internal;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using static System.Net.Quic.Implementations.MsQuic.Internal.MsQuicNativeMethods;

namespace System.Net.Quic.Implementations.MsQuic
{
    internal sealed class MsQuicConnection : QuicConnectionProvider
    {
        // Functions to invoke in MsQuic
        public MsQuicApi _api;

        // Pointer to the underlying connection
        private IntPtr _ptr;

        // Handle to this object for native callbacks.
        private GCHandle _handle;

        // Delegate that wraps the static function that will be called when receiving an event.
        private ConnectionCallbackDelegate _connectionDelegate;

        // Endpoint to either connect to or the endpoint already accepted.
        private IPEndPoint _localEndPoint;
        private readonly IPEndPoint _remoteEndPoint;

        private readonly ResettableCompletionSource<uint> _connectTcs = new ResettableCompletionSource<uint>();
        private readonly ResettableCompletionSource<uint> _shutdownTcs = new ResettableCompletionSource<uint>();

        private bool _disposed;
        private bool _connected;

        // Queue for accepted streams
        private readonly Channel<MsQuicStream> _acceptQueue = Channel.CreateBounded<MsQuicStream>(new BoundedChannelOptions(capacity: 512) // TODO configurable limit here.
        {
            SingleReader = true,
            SingleWriter = true,
        });

        // constructor for inbound connections
        public MsQuicConnection(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, MsQuicApi api, IntPtr nativeObjPtr)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
            _localEndPoint = localEndPoint;
            _remoteEndPoint = remoteEndPoint;
            _api = api;
            _ptr = nativeObjPtr;

            SetCallbackHandler();
            SetIdleTimeout(TimeSpan.FromSeconds(120));
            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        // constructor for outbound connections
        // TODO eventually remove the MsQuicApi and nativeObjectPtr from this constructor as people will new this up.
        public MsQuicConnection(IPEndPoint remoteEndPoint, MsQuicApi api, IntPtr nativeObjPtr, SslClientAuthenticationOptions sslClientAuthenticationOptions)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            _remoteEndPoint = remoteEndPoint;
            _ptr = nativeObjPtr;
            _api = api;

            SetCallbackHandler();
            SetIdleTimeout(TimeSpan.FromSeconds(120));

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        internal override IPEndPoint LocalEndPoint
        {
            get
            {
                if (!_connected)
                {
                    throw new InvalidOperationException("Listener must be started before getting endpoint.");
                }

                return new IPEndPoint(_localEndPoint.Address, _localEndPoint.Port);
            }
        }
        internal override IPEndPoint RemoteEndPoint => new IPEndPoint(_remoteEndPoint.Address, _remoteEndPoint.Port);

        internal override SslApplicationProtocol NegotiatedApplicationProtocol => throw new NotImplementedException();

        internal override bool Connected => _connected;

        internal uint HandleEvent(ref ConnectionEvent connectionEvent)
        {
            uint status = MsQuicConstants.Success;
            try
            {
                switch (connectionEvent.Type)
                {
                    // Connection is connected, can start to create streams.
                    case QUIC_CONNECTION_EVENT.CONNECTED:
                        {
                            status = HandleEventConnected(
                                connectionEvent);
                        }
                        break;

                    // Connection is being closed by the transport
                    case QUIC_CONNECTION_EVENT.SHUTDOWN_INITIATED_BY_TRANSPORT:
                        {
                            status = HandleEventShutdownInitiatedByTransport(
                                connectionEvent);
                        }
                        break;

                    // Connection is being closed by the peer
                    case QUIC_CONNECTION_EVENT.SHUTDOWN_INITIATED_BY_PEER:
                        {
                            status = HandleEventShutdownInitiatedByPeer(
                                connectionEvent);
                        }
                        break;

                    // Connection has been shutdown
                    case QUIC_CONNECTION_EVENT.SHUTDOWN_COMPLETE:
                        {
                            status = HandleEventShutdownComplete(
                                connectionEvent);
                        }
                        break;

                    case QUIC_CONNECTION_EVENT.PEER_STREAM_STARTED:
                        {
                            status = HandleEventNewStream(
                                connectionEvent);
                        }
                        break;

                    case QUIC_CONNECTION_EVENT.STREAMS_AVAILABLE:
                        {
                            status = HandleEventStreamsAvailable(
                                connectionEvent);
                        }
                        break;

                    default:
                        break;
                }
            }
            catch (Exception)
            {
                return MsQuicConstants.InternalError;
            }

            return status;
        }

        private uint HandleEventConnected(ConnectionEvent connectionEvent)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            SOCKADDR_INET inetAddress = MsQuicParameterHelpers.GetINetParam(_api, _ptr, (uint)QUIC_PARAM_LEVEL.CONNECTION, (uint)QUIC_PARAM_CONN.LOCAL_ADDRESS);
            _localEndPoint = MsQuicAddressHelpers.INetToIPEndPoint(inetAddress);

            _connected = true;
            _connectTcs.Complete(MsQuicConstants.Success);

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
            return MsQuicConstants.Success;
        }

        private uint HandleEventShutdownInitiatedByTransport(ConnectionEvent connectionEvent)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            if (!_connected)
            {
                _connectTcs.CompleteException(new IOException("Connection has been shutdown."));
            }

            _acceptQueue.Writer.Complete();

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);

            return MsQuicConstants.Success;
        }

        private uint HandleEventShutdownInitiatedByPeer(ConnectionEvent connectionEvent)
        {
            _acceptQueue.Writer.Complete();
            return MsQuicConstants.Success;
        }

        private uint HandleEventShutdownComplete(ConnectionEvent connectionEvent)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            _shutdownTcs.Complete(MsQuicConstants.Success);

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
            return MsQuicConstants.Success;
        }

        private uint HandleEventNewStream(ConnectionEvent connectionEvent)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            MsQuicStream msQuicStream = new MsQuicStream(_api, this, connectionEvent.StreamFlags, connectionEvent.Data.NewStream.Stream, inbound: true);

            if (_acceptQueue.Writer.TryWrite(msQuicStream))
            {
                if (NetEventSource.IsEnabled) NetEventSource.Exit(this);

                return MsQuicConstants.Success;
            }
            else
            {
                // Backlog too large, can't accept connections.
                return MsQuicConstants.InternalError;
            }
        }

        private uint HandleEventStreamsAvailable(ConnectionEvent connectionEvent)
        {
            return MsQuicConstants.Success;
        }

        internal override async ValueTask<QuicStreamProvider> AcceptStreamAsync(CancellationToken cancellationToken = default)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            if (await _acceptQueue.Reader.WaitToReadAsync(cancellationToken))
            {
                if (_acceptQueue.Reader.TryRead(out MsQuicStream stream))
                {
                    if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
                    return stream;
                }
            }

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);

            return null;
        }

        internal override QuicStreamProvider OpenUnidirectionalStream()
        {
            return StreamOpen(QUIC_STREAM_OPEN_FLAG.UNIDIRECTIONAL);
        }

        internal override QuicStreamProvider OpenBidirectionalStream()
        {
            return StreamOpen(QUIC_STREAM_OPEN_FLAG.NONE);
        }

        private unsafe void SetIdleTimeout(TimeSpan timeout)
        {
            MsQuicParameterHelpers.SetULongParam(_api, _ptr, (uint)QUIC_PARAM_LEVEL.CONNECTION, (uint)QUIC_PARAM_CONN.IDLE_TIMEOUT, (ulong)timeout.TotalMilliseconds);
        }

        internal override ValueTask ConnectAsync(CancellationToken cancellationToken = default)
        {
            // TODO move idle timeout setting from this class
            uint status = _api._connectionStartDelegate(
                _ptr,
                (ushort)_remoteEndPoint.AddressFamily,
                _remoteEndPoint.Address.ToString(),
                (ushort)_remoteEndPoint.Port);

            MsQuicStatusException.ThrowIfFailed(status);

            return _connectTcs.GetTypelessValueTask();
        }

        private MsQuicStream StreamOpen(
            QUIC_STREAM_OPEN_FLAG flags)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            IntPtr streamPtr = IntPtr.Zero;
            uint status = _api._streamOpenDelegate(
                _ptr,
                (uint)flags,
                MsQuicStream.NativeCallbackHandler,
                IntPtr.Zero,
                out streamPtr);
            // TODO this call is failing right now
            MsQuicStatusException.ThrowIfFailed(status);

            MsQuicStream stream = new MsQuicStream(_api, this, flags, streamPtr, inbound: false);

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
            return stream;
        }

        private void SetCallbackHandler()
        {
            _handle = GCHandle.Alloc(this);
            _connectionDelegate = new ConnectionCallbackDelegate(NativeCallbackHandler);
            _api._setCallbackHandlerDelegate(
                _ptr,
                _connectionDelegate,
                GCHandle.ToIntPtr(_handle));
        }

        private ValueTask ShutdownAsync(
            QUIC_CONNECTION_SHUTDOWN_FLAG Flags,
            ushort ErrorCode)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            uint status = _api._connectionShutdownDelegate(
                _ptr,
                (uint)Flags,
                ErrorCode);
            MsQuicStatusException.ThrowIfFailed(status);

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
            return _shutdownTcs.GetTypelessValueTask();
        }

        internal static uint NativeCallbackHandler(
            IntPtr connection,
            IntPtr context,
            ref ConnectionEvent connectionEventStruct)
        {
            GCHandle handle = GCHandle.FromIntPtr(context);
            MsQuicConnection quicConnection = (MsQuicConnection)handle.Target;
            // TODO quicConnection can be null
            return quicConnection.HandleEvent(ref connectionEventStruct);
        }

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~MsQuicConnection()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (_ptr != IntPtr.Zero)
            {
                _api._connectionCloseDelegate?.Invoke(_ptr);
            }

            _ptr = IntPtr.Zero;
            _api = null;

            _handle.Free();
            _disposed = true;
        }

        internal override ValueTask CloseAsync(CancellationToken cancellationToken = default)
        {
            // TODO make this async
            return ShutdownAsync(QUIC_CONNECTION_SHUTDOWN_FLAG.NONE, 0);
        }

        public override ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return default;
            }

            if (_ptr != IntPtr.Zero)
            {
                _api._connectionCloseDelegate?.Invoke(_ptr);
            }

            _ptr = IntPtr.Zero;
            _api = null;

            _handle.Free();
            _disposed = true;
            return default;
        }
    }
}
