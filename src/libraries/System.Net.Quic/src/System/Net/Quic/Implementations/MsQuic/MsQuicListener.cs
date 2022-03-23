// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Quic.Implementations.MsQuic.Internal;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using static System.Net.Quic.Implementations.MsQuic.Internal.MsQuicNativeMethods;

namespace System.Net.Quic.Implementations.MsQuic
{
    internal sealed class MsQuicListener : QuicListenerProvider, IDisposable
    {
        private static unsafe readonly ListenerCallbackDelegate s_listenerDelegate = new ListenerCallbackDelegate(NativeCallbackHandler);

        private readonly State _state;
        private GCHandle _stateHandle;
        private volatile bool _disposed;

        private readonly IPEndPoint _listenEndPoint;

        internal sealed class State
        {
            // set immediately in ctor, but we need a GCHandle to State in order to create the handle.
            public SafeMsQuicListenerHandle Handle = null!;
            public string TraceId = null!; // set in ctor.

            public readonly SafeMsQuicConfigurationHandle? ConnectionConfiguration;
            public readonly Channel<MsQuicConnection> AcceptConnectionQueue;
            // Pending connections are held back until they're ready to be used, which includes TLS negotiation.
            // If the negotiation succeeds, the connection is put into the accept queue; otherwise, it's discarded.
            public readonly ConcurrentDictionary<IntPtr, MsQuicConnection> PendingConnections;

            public QuicOptions ConnectionOptions = new QuicOptions();
            public SslServerAuthenticationOptions AuthenticationOptions = new SslServerAuthenticationOptions();

            public State(QuicListenerOptions options)
            {
                ConnectionOptions.IdleTimeout = options.IdleTimeout;
                ConnectionOptions.MaxBidirectionalStreams = options.MaxBidirectionalStreams;
                ConnectionOptions.MaxUnidirectionalStreams = options.MaxUnidirectionalStreams;

                bool delayConfiguration = false;

                if (options.ServerAuthenticationOptions != null)
                {
                    AuthenticationOptions.ClientCertificateRequired = options.ServerAuthenticationOptions.ClientCertificateRequired;
                    AuthenticationOptions.CertificateRevocationCheckMode = options.ServerAuthenticationOptions.CertificateRevocationCheckMode;
                    AuthenticationOptions.RemoteCertificateValidationCallback = options.ServerAuthenticationOptions.RemoteCertificateValidationCallback;
                    AuthenticationOptions.ServerCertificateSelectionCallback = options.ServerAuthenticationOptions.ServerCertificateSelectionCallback;
                    AuthenticationOptions.ApplicationProtocols = options.ServerAuthenticationOptions.ApplicationProtocols;

                    if (options.ServerAuthenticationOptions.ServerCertificate == null && options.ServerAuthenticationOptions.ServerCertificateContext == null &&
                        options.ServerAuthenticationOptions.ServerCertificateSelectionCallback != null)
                    {
                        // We don't have any certificate but we have selection callback so we need to wait for SNI.
                        delayConfiguration = true;
                    }
                }

                if (!delayConfiguration)
                {
                    ConnectionConfiguration = SafeMsQuicConfigurationHandle.Create(options, options.ServerAuthenticationOptions);
                }

                PendingConnections = new ConcurrentDictionary<IntPtr, MsQuicConnection>();
                AcceptConnectionQueue = Channel.CreateBounded<MsQuicConnection>(new BoundedChannelOptions(options.ListenBacklog)
                {
                    SingleReader = true,
                    SingleWriter = true
                });
            }
        }

        internal MsQuicListener(QuicListenerOptions options)
        {
            ArgumentNullException.ThrowIfNull(options.ListenEndPoint, nameof(options.ListenEndPoint));

            _state = new State(options);
            _stateHandle = GCHandle.Alloc(_state);
            try
            {
                Debug.Assert(!Monitor.IsEntered(_state), "!Monitor.IsEntered(_state)");
                uint status = MsQuicApi.Api.ListenerOpenDelegate(
                    MsQuicApi.Api.Registration,
                    s_listenerDelegate,
                    GCHandle.ToIntPtr(_stateHandle),
                    out _state.Handle);

                QuicExceptionHelpers.ThrowIfFailed(status, "ListenerOpen failed.");
            }
            catch
            {
                _stateHandle.Free();
                throw;
            }

            _state.TraceId = MsQuicTraceHelper.GetTraceId(_state.Handle);
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(_state, $"{_state.TraceId} Listener created");
            }

            _listenEndPoint = Start(options);

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(_state, $"{_state.TraceId} Listener started");
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

            Stop();
            _state?.Handle?.Dispose();

            // Note that it's safe to free the state GCHandle here, because:
            // (1) We called ListenerStop above, which will block until all listener events are processed. So we will not receive any more listener events.
            // (2) This class is finalizable, which means we will always get called even if the user doesn't explicitly Dispose us.
            // If we ever change this class to not be finalizable, and instead rely on the SafeHandle finalization, then we will need to make
            // the SafeHandle responsible for freeing this GCHandle, since it will have the only chance to do so when finalized.

            if (_stateHandle.IsAllocated) _stateHandle.Free();

            _state?.ConnectionConfiguration?.Dispose();
            _disposed = true;
        }

        private unsafe IPEndPoint Start(QuicListenerOptions options)
        {
            List<SslApplicationProtocol> applicationProtocols = options.ServerAuthenticationOptions!.ApplicationProtocols!;
            IPEndPoint listenEndPoint = options.ListenEndPoint!;

            SOCKADDR_INET address = MsQuicAddressHelpers.IPEndPointToINet(listenEndPoint);

            uint status;

            Debug.Assert(_stateHandle.IsAllocated);

            MemoryHandle[]? handles = null;
            QuicBuffer[]? buffers = null;
            try
            {
                Debug.Assert(!Monitor.IsEntered(_state), "!Monitor.IsEntered(_state)");
                MsQuicAlpnHelper.Prepare(applicationProtocols, out handles, out buffers);
                status = MsQuicApi.Api.ListenerStartDelegate(_state.Handle, (QuicBuffer*)Marshal.UnsafeAddrOfPinnedArrayElement(buffers, 0), (uint)applicationProtocols.Count, ref address);
            }
            catch
            {
                _stateHandle.Free();
                throw;
            }
            finally
            {
                MsQuicAlpnHelper.Return(ref handles, ref buffers);
            }

            QuicExceptionHelpers.ThrowIfFailed(status, "ListenerStart failed.");

            Debug.Assert(!Monitor.IsEntered(_state), "!Monitor.IsEntered(_state)");
            SOCKADDR_INET inetAddress = MsQuicParameterHelpers.GetINetParam(MsQuicApi.Api, _state.Handle, QUIC_PARAM_LEVEL.LISTENER, (uint)QUIC_PARAM_LISTENER.LOCAL_ADDRESS);
            return MsQuicAddressHelpers.INetToIPEndPoint(ref inetAddress);
        }

        private void Stop()
        {
            // TODO finalizers are called even if the object construction fails.
            if (_state == null)
            {
                return;
            }

            _state.AcceptConnectionQueue?.Writer.TryComplete();

            if (_state.Handle != null)
            {
                Debug.Assert(!Monitor.IsEntered(_state), "!Monitor.IsEntered(_state)");
                MsQuicApi.Api.ListenerStopDelegate(_state.Handle);
            }
        }

        private static unsafe uint NativeCallbackHandler(
            IntPtr listener,
            IntPtr context,
            ListenerEvent* evt)
        {
            GCHandle gcHandle = GCHandle.FromIntPtr(context);
            Debug.Assert(gcHandle.IsAllocated);
            Debug.Assert(gcHandle.Target is not null);
            var state = (State)gcHandle.Target;
            if (evt->Type != QUIC_LISTENER_EVENT.NEW_CONNECTION)
            {
                return MsQuicStatusCodes.InternalError;
            }

            SafeMsQuicConnectionHandle? connectionHandle = null;
            MsQuicConnection? msQuicConnection = null;
            try
            {
                ref NewConnectionInfo connectionInfo = ref *evt->Data.NewConnection.Info;

                IPEndPoint localEndPoint = MsQuicAddressHelpers.INetToIPEndPoint(ref *(SOCKADDR_INET*)connectionInfo.LocalAddress);
                IPEndPoint remoteEndPoint = MsQuicAddressHelpers.INetToIPEndPoint(ref *(SOCKADDR_INET*)connectionInfo.RemoteAddress);
                string targetHost = string.Empty;   // compat with SslStream
                if (connectionInfo.ServerNameLength > 0 && connectionInfo.ServerName != IntPtr.Zero)
                {
                    // TBD We should figure out what to do with international names.
                    targetHost = Marshal.PtrToStringAnsi(connectionInfo.ServerName, connectionInfo.ServerNameLength);
                }

                SafeMsQuicConfigurationHandle? connectionConfiguration = state.ConnectionConfiguration;

                if (connectionConfiguration == null)
                {
                    Debug.Assert(state.AuthenticationOptions.ServerCertificateSelectionCallback != null);
                    try
                    {
                        // ServerCertificateSelectionCallback is synchronous. We will call it as needed when building configuration
                        connectionConfiguration = SafeMsQuicConfigurationHandle.Create(state.ConnectionOptions, state.AuthenticationOptions, targetHost);
                    }
                    catch (Exception ex)
                    {
                        if (NetEventSource.Log.IsEnabled())
                        {
                            NetEventSource.Error(state, $"[Listener#{state.GetHashCode()}] Exception occurred during creating configuration in connection callback: {ex}");
                        }
                    }

                    if (connectionConfiguration == null)
                    {
                        // We don't have safe handle yet so MsQuic will cleanup new connection.
                        return MsQuicStatusCodes.InternalError;
                    }
                }

                connectionHandle = new SafeMsQuicConnectionHandle(evt->Data.NewConnection.Connection);

                Debug.Assert(!Monitor.IsEntered(state), "!Monitor.IsEntered(state)");
                uint status = MsQuicApi.Api.ConnectionSetConfigurationDelegate(connectionHandle, connectionConfiguration);
                if (MsQuicStatusHelper.SuccessfulStatusCode(status))
                {
                    msQuicConnection = new MsQuicConnection(localEndPoint, remoteEndPoint, state, connectionHandle, state.AuthenticationOptions.ClientCertificateRequired, state.AuthenticationOptions.CertificateRevocationCheckMode, state.AuthenticationOptions.RemoteCertificateValidationCallback);
                    msQuicConnection.SetNegotiatedAlpn(connectionInfo.NegotiatedAlpn, connectionInfo.NegotiatedAlpnLength);

                    if (!state.PendingConnections.TryAdd(connectionHandle.DangerousGetHandle(), msQuicConnection))
                    {
                        msQuicConnection.Dispose();
                    }

                    return MsQuicStatusCodes.Success;
                }

                // If we fall-through here something wrong happened.
            }
            catch (Exception ex)
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Error(state, $"[Listener#{state.GetHashCode()}] Exception occurred during handling {(QUIC_LISTENER_EVENT)evt->Type} connection callback: {ex}");
                }
            }

            // This handle will be cleaned up by MsQuic by returning InternalError.
            connectionHandle?.SetHandleAsInvalid();
            msQuicConnection?.Dispose();
            return MsQuicStatusCodes.InternalError;
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
