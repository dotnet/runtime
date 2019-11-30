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
        private MsQuicApi _api;
        private MsQuicSecurityConfig _secConfig;
        private bool _disposed;
        private IntPtr _nativeObjPtr;
        private GCHandle _handle;
        private ListenerCallbackDelegate _listenerDelegate;
        private SslServerAuthenticationOptions _sslOptions;
        private bool _started;

        private readonly Channel<MsQuicConnection> _acceptConnectionQueue = Channel.CreateUnbounded<MsQuicConnection>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });

        internal MsQuicListener(IPEndPoint listenEndPoint, SslServerAuthenticationOptions sslServerAuthenticationOptions, MsQuicApi api, IntPtr nativeObjPtr)
        {
            _api = api;
            _sslOptions = sslServerAuthenticationOptions;
            ListenEndPoint = listenEndPoint;
            _nativeObjPtr = nativeObjPtr;
        }

        internal override IPEndPoint ListenEndPoint { get; }

        internal override async ValueTask<QuicConnectionProvider> AcceptConnectionAsync(CancellationToken cancellationToken = default)
        {
            if (!_started)
            {
                await StartAsync();
                _started = true;
            }

            if (await _acceptConnectionQueue.Reader.WaitToReadAsync())
            {
                if (_acceptConnectionQueue.Reader.TryRead(out MsQuicConnection connection))
                {
                    return connection;
                }
            }

            return null;
        }

        public async ValueTask StartAsync()
        {
            _secConfig = await _api.CreateSecurityConfig(_sslOptions.ServerCertificate);

            SetCallbackHandler();

            SOCKADDR_INET address = MsQuicNativeMethods.Convert(ListenEndPoint);

            uint status = _api.ListenerStartDelegate(
                _nativeObjPtr,
                ref address);
            MsQuicStatusException.ThrowIfFailed(status);
        }

        internal uint ListenerCallbackHandler(
            ref ListenerEvent evt)
        {
            switch (evt.Type)
            {
                case QUIC_LISTENER_EVENT.NEW_CONNECTION:
                    {
                        Console.WriteLine("Receieved new connection");
                        evt.Data.NewConnection.SecurityConfig = _secConfig.NativeObjPtr;
                        MsQuicConnection msQuicConnection = new MsQuicConnection(ListenEndPoint, _api, evt.Data.NewConnection.Connection);
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
            _api.SetCallbackHandlerDelegate(
                _nativeObjPtr,
                _listenerDelegate,
                GCHandle.ToIntPtr(_handle));
        }

        ~MsQuicListener()
        {
            Dispose(false);
        }

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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

            if (_nativeObjPtr != IntPtr.Zero)
            {
                _api.ListenerStopDelegate(_nativeObjPtr);
                _api.ListenerCloseDelegate(_nativeObjPtr);
            }

            _nativeObjPtr = IntPtr.Zero;
            _api = null;
            _disposed = true;
        }

        internal override void Close()
        {
            Dispose();
        }
    }
}
