// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Net.Quic.Implementations.MsQuic.Internal;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using static System.Net.Quic.Implementations.MsQuic.Internal.MsQuicNativeMethods;

namespace System.Net.Quic.Implementations.MsQuic
{
    internal sealed class MsQuicListener : QuicListenerProvider, IDisposable
    {
        private static readonly ListenerCallbackDelegate s_listenerDelegate = new ListenerCallbackDelegate(NativeCallbackHandler);

        private readonly State _state;
        private GCHandle _stateHandle;
        private volatile bool _disposed;

        private IPEndPoint _listenEndPoint;
        private readonly List<SslApplicationProtocol> _applicationProtocols;

        private sealed class State
        {
            // set immediately in ctor, but we need a GCHandle to State in order to create the handle.
            public SafeMsQuicListenerHandle Handle = null!;

            public readonly SafeMsQuicConfigurationHandle ConnectionConfiguration;
            public readonly Channel<MsQuicConnection> AcceptConnectionQueue;

            public State(QuicListenerOptions options)
            {
                ConnectionConfiguration = SafeMsQuicConfigurationHandle.Create(options);

                AcceptConnectionQueue = Channel.CreateBounded<MsQuicConnection>(new BoundedChannelOptions(options.ListenBacklog)
                {
                    SingleReader = true,
                    SingleWriter = true
                });
            }
        }

        internal MsQuicListener(QuicListenerOptions options)
        {
            _applicationProtocols = options.ServerAuthenticationOptions!.ApplicationProtocols!;
            _listenEndPoint = options.ListenEndPoint!;

            _state = new State(options);
            _stateHandle = GCHandle.Alloc(_state);
            try
            {
                uint status = MsQuicApi.Api.ListenerOpenDelegate(
                    MsQuicApi.Api.Registration,
                    s_listenerDelegate,
                    GCHandle.ToIntPtr(_stateHandle),
                    out _state.Handle);

                QuicExceptionHelpers.ThrowIfFailed(status, "ListenerOpen failed.");
            }
            catch
            {
                _state.Handle?.Dispose();
                _stateHandle.Free();
                throw;
            }
        }

        internal override IPEndPoint ListenEndPoint
        {
            get
            {
                return new IPEndPoint(_listenEndPoint.Address, _listenEndPoint.Port);
            }
        }

        internal override async ValueTask<QuicConnectionProvider> AcceptConnectionAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            try
            {
                return await _state.AcceptConnectionQueue.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (ChannelClosedException)
            {
                throw new QuicOperationAbortedException();
            }
        }

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~MsQuicListener()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            StopAcceptingConnections();
            _state.Handle.Dispose();
            if (_stateHandle.IsAllocated) _stateHandle.Free();
            _state.ConnectionConfiguration.Dispose();
            _disposed = true;
        }

        internal override unsafe void Start()
        {
            ThrowIfDisposed();

            SOCKADDR_INET address = MsQuicAddressHelpers.IPEndPointToINet(_listenEndPoint);

            uint status;

            MemoryHandle[]? handles = null;
            QuicBuffer[]? buffers = null;
            try
            {
                MsQuicAlpnHelper.Prepare(_applicationProtocols, out handles, out buffers);
                status = MsQuicApi.Api.ListenerStartDelegate(_state.Handle, ref MemoryMarshal.GetReference(buffers.AsSpan()), (uint)_applicationProtocols.Count, ref address);
            }
            finally
            {
                MsQuicAlpnHelper.Destroy(ref handles, ref buffers);
            }

            QuicExceptionHelpers.ThrowIfFailed(status, "ListenerStart failed.");

            SOCKADDR_INET inetAddress = MsQuicParameterHelpers.GetINetParam(MsQuicApi.Api, _state.Handle, (uint)QUIC_PARAM_LEVEL.LISTENER, (uint)QUIC_PARAM_LISTENER.LOCAL_ADDRESS);
            _listenEndPoint = MsQuicAddressHelpers.INetToIPEndPoint(ref inetAddress);
        }

        internal override void Close()
        {
            ThrowIfDisposed();
            MsQuicApi.Api.ListenerStopDelegate(_state.Handle);
        }

        private void StopAcceptingConnections()
        {
            _state.AcceptConnectionQueue.Writer.TryComplete();
        }

        private static unsafe uint NativeCallbackHandler(
            IntPtr listener,
            IntPtr context,
            ref ListenerEvent evt)
        {
            if ((QUIC_LISTENER_EVENT)evt.Type != QUIC_LISTENER_EVENT.NEW_CONNECTION)
            {
                return MsQuicStatusCodes.InternalError;
            }

            State state = (State)GCHandle.FromIntPtr(context).Target!;
            SafeMsQuicConnectionHandle? connectionHandle = null;

            try
            {
                ref NewConnectionInfo connectionInfo = ref *(NewConnectionInfo*)evt.Data.NewConnection.Info;

                IPEndPoint localEndPoint = MsQuicAddressHelpers.INetToIPEndPoint(ref *(SOCKADDR_INET*)connectionInfo.LocalAddress);
                IPEndPoint remoteEndPoint = MsQuicAddressHelpers.INetToIPEndPoint(ref *(SOCKADDR_INET*)connectionInfo.RemoteAddress);

                connectionHandle = new SafeMsQuicConnectionHandle(evt.Data.NewConnection.Connection);

                uint status = MsQuicApi.Api.ConnectionSetConfigurationDelegate(connectionHandle, state.ConnectionConfiguration);
                QuicExceptionHelpers.ThrowIfFailed(status, "ConnectionSetConfiguration failed.");

                var msQuicConnection = new MsQuicConnection(localEndPoint, remoteEndPoint, connectionHandle);
                msQuicConnection.SetNegotiatedAlpn(connectionInfo.NegotiatedAlpn, connectionInfo.NegotiatedAlpnLength);

                if (!state.AcceptConnectionQueue.Writer.TryWrite(msQuicConnection))
                {
                    // This handle will be cleaned up by MsQuic.
                    connectionHandle.SetHandleAsInvalid();
                    msQuicConnection.Dispose();
                    return MsQuicStatusCodes.InternalError;
                }

                return MsQuicStatusCodes.Success;
            }
            catch (Exception ex)
            {
                // This handle will be cleaned up by MsQuic by returning InternalError.
                connectionHandle?.SetHandleAsInvalid();
                state.AcceptConnectionQueue.Writer.TryComplete(ex);
                return MsQuicStatusCodes.InternalError;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(MsQuicStream));
            }
        }
    }
}
