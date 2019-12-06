// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Quic.Implementations.MsQuic.Internal;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using static System.Net.Quic.Implementations.MsQuic.Internal.MsQuicNativeMethods;

namespace System.Net.Quic.Implementations.MsQuic
{
    internal class MsQuicListener : QuicListenerProvider, IDisposable, IAsyncDisposable
    {
        // Functions to invoke in MsQuic
        private MsQuicApi _api;

        // Security configuration for MsQuic
        private MsQuicSecurityConfig _secConfig;

        // Pointer to the underlying listener
        private IntPtr _ptr;

        // Handle to this object for native callbacks.
        private GCHandle _handle;

        // Delegate that wraps the static function that will be called when receiving an event.
        private ListenerCallbackDelegate _listenerDelegate;

        // Ssl listening options (ALPN, cert, etc)
        private SslServerAuthenticationOptions _sslOptions;

        private volatile bool _disposed;

        private volatile bool _started;
        private IPEndPoint _listenEndpoint;

        private readonly Channel<MsQuicConnection> _acceptConnectionQueue;

        internal MsQuicListener(IPEndPoint listenEndPoint, SslServerAuthenticationOptions sslServerAuthenticationOptions, MsQuicApi api, IntPtr nativeObjPtr)
        {
            _api = api;
            _sslOptions = sslServerAuthenticationOptions;
            _listenEndpoint = listenEndPoint;
            _ptr = nativeObjPtr;
            _acceptConnectionQueue = Channel.CreateBounded<MsQuicConnection>(new BoundedChannelOptions(512) // TODO make this configurable.
            {
                SingleReader = true,
                SingleWriter = true
            });
        }

        internal override IPEndPoint ListenEndPoint {
            get
            {
                if (!_started)
                {
                    throw new InvalidOperationException("Listener must be started before getting endpoint.");
                }
                return _listenEndpoint;
            }
        }

        internal override async ValueTask<QuicConnectionProvider> AcceptConnectionAsync(CancellationToken cancellationToken = default)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            if (await _acceptConnectionQueue.Reader.WaitToReadAsync())
            {
                if (_acceptConnectionQueue.Reader.TryRead(out MsQuicConnection connection))
                {
                    if (NetEventSource.IsEnabled) NetEventSource.Exit(this);

                    return connection;
                }
            }

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);

            return null;
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

        public override ValueTask DisposeAsync()
        {
            Dispose();
            return default;
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            StopAcceptingConnections();

            if (_ptr != IntPtr.Zero)
            {
                _api._listenerStopDelegate(_ptr);
                _api._listenerCloseDelegate(_ptr);
            }

            _ptr = IntPtr.Zero;
            _api = null;
            _disposed = true;
        }

        internal override ValueTask CloseAsync()
        {
            _api._listenerStopDelegate(_ptr);
            return default;
        }

        internal override async ValueTask StartAsync()
        {
            _secConfig = await _api.CreateSecurityConfig(_sslOptions?.ServerCertificate);

            SetCallbackHandler();

            SOCKADDR_INET address = MsQuicNativeMethods.Convert(_listenEndpoint);

            uint status = _api._listenerStartDelegate(
                _ptr,
                ref address);

            MsQuicStatusException.ThrowIfFailed(status);

            // If the listen port is 0, requery the ListeneEndPoint.
            SetListenPort();

            _started = true;
        }

        private unsafe void SetListenPort()
        {
            byte* ptr = stackalloc byte[sizeof(SOCKADDR_INET)];
            QuicBuffer buffer = new QuicBuffer
            {
                Length = (uint)sizeof(SOCKADDR_INET),
                Buffer = ptr
            };

            MsQuicStatusException.ThrowIfFailed(_api.UnsafeGetParam(_ptr, (uint)QUIC_PARAM_LEVEL.LISTENER, (uint)QUIC_PARAM_LISTENER.LOCAL_ADDRESS, ref buffer));
            SOCKADDR_INET inetAddress = *(SOCKADDR_INET*)ptr;

            if (inetAddress.si_family == MsQuicNativeMethods.IPv4)
            {
                _listenEndpoint = new IPEndPoint(new IPAddress(inetAddress.Ipv4.Address), inetAddress.Ipv4.sin_port);
            }
            else
            {
                _listenEndpoint = new IPEndPoint(new IPAddress(inetAddress.Ipv6.Address), inetAddress.Ipv6._port);
            }
        }

        internal unsafe uint ListenerCallbackHandler(
            ref ListenerEvent evt)
        {
            switch (evt.Type)
            {
                case QUIC_LISTENER_EVENT.NEW_CONNECTION:
                    {
                        evt.Data.NewConnection.SecurityConfig = _secConfig.NativeObjPtr;
                        MsQuicConnection msQuicConnection = new MsQuicConnection(ListenEndPoint, ListenEndPoint, _api, evt.Data.NewConnection.Connection);
                        _acceptConnectionQueue.Writer.TryWrite(msQuicConnection);
                    }
                    break;
                default:
                    return MsQuicConstants.InternalError;
            }

            return MsQuicConstants.Success;
        }

        protected void StopAcceptingConnections()
        {
            _acceptConnectionQueue.Writer.TryComplete();
        }

        internal static uint NativeCallbackHandler(
            IntPtr listener,
            IntPtr context,
            ref ListenerEvent connectionEventStruct)
        {
            GCHandle handle = GCHandle.FromIntPtr(context);
            MsQuicListener quicListener = (MsQuicListener)handle.Target;

            return quicListener.ListenerCallbackHandler(ref connectionEventStruct);
        }

        internal void SetCallbackHandler()
        {
            _handle = GCHandle.Alloc(this);
            _listenerDelegate = new ListenerCallbackDelegate(NativeCallbackHandler);
            _api._setCallbackHandlerDelegate(
                _ptr,
                _listenerDelegate,
                GCHandle.ToIntPtr(_handle));
        }
    }
}
