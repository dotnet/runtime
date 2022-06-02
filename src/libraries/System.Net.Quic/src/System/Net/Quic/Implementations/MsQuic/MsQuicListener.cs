// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
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
using Microsoft.Quic;
using System.Runtime.CompilerServices;
using static Microsoft.Quic.MsQuic;

namespace System.Net.Quic.Implementations.MsQuic
{
    internal sealed class MsQuicListener : QuicListenerProvider, IDisposable
    {
        private readonly State _state;
        private GCHandle _stateHandle;
        private volatile bool _disposed;

        private readonly IPEndPoint _listenEndPoint;

        internal sealed class State
        {
            // set immediately in ctor, but we need a GCHandle to State in order to create the handle.
            public SafeMsQuicListenerHandle Handle = null!;

            public TaskCompletionSource StopCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

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
                    AuthenticationOptions.CipherSuitesPolicy = options.ServerAuthenticationOptions.CipherSuitesPolicy;

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

        internal unsafe MsQuicListener(QuicListenerOptions options)
        {
            ArgumentNullException.ThrowIfNull(options.ListenEndPoint, nameof(options.ListenEndPoint));

            _state = new State(options);
            _stateHandle = GCHandle.Alloc(_state);
            try
            {
                QUIC_HANDLE* handle;
                Debug.Assert(!Monitor.IsEntered(_state), "!Monitor.IsEntered(_state)");
                ThrowIfFailure(MsQuicApi.Api.ApiTable->ListenerOpen(
                    MsQuicApi.Api.Registration.QuicHandle,
                    &NativeCallback,
                    (void*)GCHandle.ToIntPtr(_stateHandle),
                    &handle), "ListenerOpen failed");
                _state.Handle = new SafeMsQuicListenerHandle(handle);
            }
            catch
            {
                _stateHandle.Free();
                throw;
            }

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(_state, $"{_state.Handle} Listener created");
            }

            _listenEndPoint = Start(options);

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(_state, $"{_state.Handle} Listener started");
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

            // TODO: solve listener stopping in better way now that it receives STOP_COMPLETED event.
            StopAsync().GetAwaiter().GetResult();
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

            Debug.Assert(_stateHandle.IsAllocated);
            try
            {
                Debug.Assert(!Monitor.IsEntered(_state), "!Monitor.IsEntered(_state)");
                using var msquicBuffers = new MsQuicBuffers();
                msquicBuffers.Initialize(applicationProtocols, applicationProtocol => applicationProtocol.Protocol);

                QuicAddr address = listenEndPoint.ToQuicAddr();

                if (listenEndPoint.Address == IPAddress.IPv6Any)
                {
                    // For IPv6Any, MsQuic would listen only for IPv6 connections. To mimic the behavior of TCP sockets,
                    // we leave the address family unspecified and let MsQuic handle connections from all IP addresses.
                    address.Family = QUIC_ADDRESS_FAMILY_UNSPEC;
                }

                ThrowIfFailure(MsQuicApi.Api.ApiTable->ListenerStart(
                    _state.Handle.QuicHandle,
                    msquicBuffers.Buffers,
                    (uint)applicationProtocols.Count,
                    &address), "ListenerStart failed");
            }
            catch
            {
                _stateHandle.Free();
                throw;
            }

            Debug.Assert(!Monitor.IsEntered(_state), "!Monitor.IsEntered(_state)");
            // override the address family to the original value in case we had to use UNSPEC
            return MsQuicParameterHelpers.GetIPEndPointParam(MsQuicApi.Api, _state.Handle, QUIC_PARAM_LISTENER_LOCAL_ADDRESS, listenEndPoint.AddressFamily);
        }

        private unsafe Task StopAsync()
        {
            // TODO finalizers are called even if the object construction fails.
            if (_state == null)
            {
                return Task.CompletedTask;
            }

            _state.AcceptConnectionQueue?.Writer.TryComplete();

            if (_state.Handle != null)
            {
                Debug.Assert(!Monitor.IsEntered(_state), "!Monitor.IsEntered(_state)");
                MsQuicApi.Api.ApiTable->ListenerStop(_state.Handle.QuicHandle);
            }
            return _state.StopCompletion.Task;
        }

#pragma warning disable CS3016
        [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvCdecl) })]
#pragma warning restore CS3016
        private static unsafe int NativeCallback(QUIC_HANDLE* listener, void* context, QUIC_LISTENER_EVENT* listenerEvent)
        {
            GCHandle gcHandle = GCHandle.FromIntPtr((IntPtr)context);
            Debug.Assert(gcHandle.IsAllocated);
            Debug.Assert(gcHandle.Target is not null);
            var state = (State)gcHandle.Target;


            if (listenerEvent->Type == QUIC_LISTENER_EVENT_TYPE.STOP_COMPLETE)
            {
                state.StopCompletion.TrySetResult();
                return QUIC_STATUS_SUCCESS;
            }

            if (listenerEvent->Type != QUIC_LISTENER_EVENT_TYPE.NEW_CONNECTION)
            {
                return QUIC_STATUS_INTERNAL_ERROR;
            }

            SafeMsQuicConnectionHandle? connectionHandle = null;
            MsQuicConnection? msQuicConnection = null;
            try
            {
                ref QUIC_NEW_CONNECTION_INFO connectionInfo = ref *listenerEvent->NEW_CONNECTION.Info;

                IPEndPoint localEndPoint = MsQuicAddressHelpers.INetToIPEndPoint((IntPtr)connectionInfo.LocalAddress);
                IPEndPoint remoteEndPoint = MsQuicAddressHelpers.INetToIPEndPoint((IntPtr)connectionInfo.RemoteAddress);

                string targetHost = string.Empty;   // compat with SslStream
                if (connectionInfo.ServerNameLength > 0 && (IntPtr)connectionInfo.ServerName != IntPtr.Zero)
                {
                    // TBD We should figure out what to do with international names.
                    targetHost = Marshal.PtrToStringAnsi((IntPtr)connectionInfo.ServerName, connectionInfo.ServerNameLength);
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
                        return QUIC_STATUS_INTERNAL_ERROR;
                    }
                }

                connectionHandle = new SafeMsQuicConnectionHandle(listenerEvent->NEW_CONNECTION.Connection);

                Debug.Assert(!Monitor.IsEntered(state), "!Monitor.IsEntered(state)");
                int status = MsQuicApi.Api.ApiTable->ConnectionSetConfiguration(connectionHandle.QuicHandle, connectionConfiguration.QuicHandle);
                if (StatusSucceeded(status))
                {
                    msQuicConnection = new MsQuicConnection(localEndPoint, remoteEndPoint, state, connectionHandle, state.AuthenticationOptions.ClientCertificateRequired, state.AuthenticationOptions.CertificateRevocationCheckMode, state.AuthenticationOptions.RemoteCertificateValidationCallback);
                    msQuicConnection.SetNegotiatedAlpn((IntPtr)connectionInfo.NegotiatedAlpn, connectionInfo.NegotiatedAlpnLength);

                    if (!state.PendingConnections.TryAdd(connectionHandle.DangerousGetHandle(), msQuicConnection))
                    {
                        msQuicConnection.Dispose();
                    }

                    return QUIC_STATUS_SUCCESS;
                }

                // If we fall-through here something wrong happened.
            }
            catch (Exception ex)
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Error(state, $"[Listener#{state.GetHashCode()}] Exception occurred during handling {listenerEvent->Type} connection callback: {ex}");
                }
            }

            // This handle will be cleaned up by MsQuic by returning InternalError.
            connectionHandle?.SetHandleAsInvalid();
            msQuicConnection?.Dispose();
            return QUIC_STATUS_INTERNAL_ERROR;
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
