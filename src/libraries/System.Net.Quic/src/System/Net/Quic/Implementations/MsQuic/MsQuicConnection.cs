using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Quic.Implementations.MsQuic.Internal;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using static System.Net.Quic.Implementations.MsQuic.Internal.MsQuicNativeMethods;

namespace System.Net.Quic.Implementations.MsQuic
{
    internal sealed class MsQuicConnection : QuicConnectionProvider
    {
        public MsQuicApi _api;
        private bool _disposed;
        private IntPtr _nativeObjPtr;
        private GCHandle _handle;
        private ConnectionCallbackDelegate _connectionDelegate;
        private IPEndPoint _remoteEndPoint;
        private TaskCompletionSource<object> _connectTcs = new TaskCompletionSource<object>();
        private TaskCompletionSource<object> _shutdownTcs = new TaskCompletionSource<object>();
        private string _logBaseString;

        private readonly Channel<MsQuicStream> _acceptQueue = Channel.CreateUnbounded<MsQuicStream>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });

        // constructor for inbound connections
        public MsQuicConnection(IPEndPoint remoteEndPoint, MsQuicApi api, IntPtr nativeObjPtr)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
            _remoteEndPoint = remoteEndPoint;
            _logBaseString = "[Connection Server]";

            _api = api;
            _nativeObjPtr = nativeObjPtr;

            SetCallbackHandler();
            SetIdleTimeout(TimeSpan.FromSeconds(120));
            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        // constructor for outbound connections
        public MsQuicConnection(IPEndPoint remoteEndPoint, MsQuicApi api, IntPtr nativeObjPtr, SslClientAuthenticationOptions sslClientAuthenticationOptions)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
            _logBaseString = "[Connection Client]";

            _remoteEndPoint = remoteEndPoint;
            _nativeObjPtr = nativeObjPtr;
            _api = api;
            SetCallbackHandler();

            Log("Created connection");
            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        internal override IPEndPoint LocalEndPoint => throw new NotImplementedException();

        internal override IPEndPoint RemoteEndPoint => new IPEndPoint(_remoteEndPoint.Address, _remoteEndPoint.Port);

        internal override SslApplicationProtocol NegotiatedApplicationProtocol => throw new NotImplementedException();

        internal override bool Connected => throw new NotImplementedException();

        internal uint HandleEvent(ref ConnectionEvent connectionEvent)
        {
            uint status = MsQuicConstants.Success;
            try
            {
                switch (connectionEvent.Type)
                {
                    case QUIC_CONNECTION_EVENT.CONNECTED:
                        {
                            status = HandleEventConnected(
                                connectionEvent);
                        }
                        break;

                    case QUIC_CONNECTION_EVENT.SHUTDOWN_BEGIN:
                        {
                            status = HandleEventShutdownBegin(
                                connectionEvent);
                        }
                        break;

                    case QUIC_CONNECTION_EVENT.SHUTDOWN_BEGIN_PEER:
                        {
                            status = HandleEventShutdownBeginPeer(
                                connectionEvent);
                        }
                        break;

                    case QUIC_CONNECTION_EVENT.SHUTDOWN_COMPLETE:
                        {
                            status = HandleEventShutdownComplete(
                                connectionEvent);
                        }
                        break;

                    case QUIC_CONNECTION_EVENT.NEW_STREAM:
                        {
                            // never receiving new stream event.
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
                        Log($"Unexpected event {(connectionEvent.Type).ToString()}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Log(ex.Message);
                return MsQuicConstants.InternalError;
            }

            return status;
        }

        private uint HandleEventConnected(ConnectionEvent connectionEvent)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
            // TODO don't use tcs here.
            _connectTcs?.SetResult(null);
            _connectTcs = null;
            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
            return MsQuicConstants.Success;
        }

        private void Log(string log)
        {
            Console.WriteLine($"{_logBaseString} {log}");
        }

        private uint HandleEventShutdownBegin(ConnectionEvent connectionEvent)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"Shutdown begin {MsQuicConstants.ErrorTypeFromErrorCode(connectionEvent.ShutdownBeginStatus)}");

            _connectTcs?.SetResult(null);
            _connectTcs = null;

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);

            return MsQuicConstants.Success;
        }

        private uint HandleEventShutdownBeginPeer(ConnectionEvent connectionEvent)
        {
            return MsQuicConstants.Success;
        }

        private uint HandleEventShutdownComplete(ConnectionEvent connectionEvent)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"Shutdown Complete");

            _shutdownTcs?.SetResult(null);

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
            return MsQuicConstants.Success;
        }

        private uint HandleEventNewStream(ConnectionEvent connectionEvent)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            MsQuicStream msQuicStream = new MsQuicStream(_api, this, connectionEvent.Data.NewStream.Stream);

            _acceptQueue.Writer.TryWrite(msQuicStream);

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);

            return MsQuicConstants.Success;
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

        public unsafe void SetIdleTimeout(TimeSpan timeout)
        {
            ulong msTime = (ulong)timeout.TotalMilliseconds;
            var buffer = new QuicBuffer()
            {
                Length = sizeof(ulong),
                Buffer = (byte*)&msTime
            };
            SetParam(QUIC_PARAM_CONN.IDLE_TIMEOUT, buffer);
        }

        public void SetPeerBiDirectionalStreamCount(ushort count)
        {
            SetUshortParamter(QUIC_PARAM_CONN.PEER_BIDI_STREAM_COUNT, count);
        }

        public void SetPeerUnidirectionalStreamCount(ushort count)
        {
            SetUshortParamter(QUIC_PARAM_CONN.PEER_UNIDI_STREAM_COUNT, count);
        }

        public void SetLocalBidirectionalStreamCount(ushort count)
        {
            SetUshortParamter(QUIC_PARAM_CONN.LOCAL_BIDI_STREAM_COUNT, count);
        }

        public void SetLocalUnidirectionalStreamCount(ushort count)
        {
            SetUshortParamter(QUIC_PARAM_CONN.LOCAL_UNIDI_STREAM_COUNT, count);
        }

        public unsafe void EnableBuffering()
        {
            bool val = true;
            var buffer = new QuicBuffer()
            {
                Length = sizeof(bool),
                Buffer = (byte*)&val
            };
            SetParam(QUIC_PARAM_CONN.USE_SEND_BUFFER, buffer);
        }

        public unsafe void DisableBuffering()
        {
            bool val = false;
            var buffer = new QuicBuffer()
            {
                Length = sizeof(bool),
                Buffer = (byte*)&val
            };
            SetParam(QUIC_PARAM_CONN.USE_SEND_BUFFER, buffer);
        }

        internal override ValueTask ConnectAsync(CancellationToken cancellationToken = default)
        {
            SetIdleTimeout(TimeSpan.FromSeconds(120));
            uint status = _api.ConnectionStartDelegate(
                _nativeObjPtr,
                (ushort)_remoteEndPoint.AddressFamily,
                _remoteEndPoint.Address.ToString(),
                (ushort)_remoteEndPoint.Port);

            MsQuicStatusException.ThrowIfFailed(status);

            return new ValueTask(_connectTcs.Task);
        }

        public MsQuicStream StreamOpen(
            QUIC_STREAM_OPEN_FLAG flags)
        {
            IntPtr streamPtr = IntPtr.Zero;
            uint status = _api.StreamOpenDelegate(
                _nativeObjPtr,
                (uint)flags,
                MsQuicStream.NativeCallbackHandler,
                IntPtr.Zero,
                out streamPtr);
            MsQuicStatusException.ThrowIfFailed(status);

            return new MsQuicStream(_api, this, flags, streamPtr);
        }

        public void SetCallbackHandler()
        {
            _handle = GCHandle.Alloc(this);
            Log("Callback handler");
            _connectionDelegate = new ConnectionCallbackDelegate(NativeCallbackHandler);
            _api.SetCallbackHandlerDelegate(
                _nativeObjPtr,
                _connectionDelegate,
                GCHandle.ToIntPtr(_handle));
        }

        public Task ShutdownAsync(
            QUIC_CONNECTION_SHUTDOWN_FLAG Flags,
            ushort ErrorCode)
        {
            uint status = _api.ConnectionShutdownDelegate(
                _nativeObjPtr,
                (uint)Flags,
                ErrorCode);
            MsQuicStatusException.ThrowIfFailed(status);
            return _shutdownTcs.Task;
        }

        internal static uint NativeCallbackHandler(
            IntPtr connection,
            IntPtr context,
            ref ConnectionEvent connectionEventStruct)
        {
            var handle = GCHandle.FromIntPtr(context);
            var quicConnection = (MsQuicConnection)handle.Target;
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

            if (_nativeObjPtr != IntPtr.Zero)
            {
                ShutdownAsync(QUIC_CONNECTION_SHUTDOWN_FLAG.NONE, 0).GetAwaiter().GetResult();
                _api.ConnectionCloseDelegate?.Invoke(_nativeObjPtr);
            }

            _nativeObjPtr = IntPtr.Zero;
            _api = null;

            _handle.Free();
            _disposed = true;
        }

        private unsafe void SetUshortParamter(QUIC_PARAM_CONN param, ushort count)
        {
            QuicBuffer buffer = new QuicBuffer()
            {
                Length = sizeof(ushort),
                Buffer = (byte*)&count
            };
            SetParam(param, buffer);
        }

        private void SetParam(
            QUIC_PARAM_CONN param,
            QuicBuffer buf)
        {
            MsQuicStatusException.ThrowIfFailed(_api.UnsafeSetParam(
                _nativeObjPtr,
                (uint)QUIC_PARAM_LEVEL.CONNECTION,
                (uint)param,
                buf));
        }

        internal override void Close()
        {
            Dispose(false);
        }

        public override async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            if (_nativeObjPtr != IntPtr.Zero)
            {
                await ShutdownAsync(QUIC_CONNECTION_SHUTDOWN_FLAG.NONE, 0).ConfigureAwait(false);
                _api.ConnectionCloseDelegate?.Invoke(_nativeObjPtr);
            }

            _nativeObjPtr = IntPtr.Zero;
            _api = null;

            _handle.Free();
            _disposed = true;
        }
    }
}
