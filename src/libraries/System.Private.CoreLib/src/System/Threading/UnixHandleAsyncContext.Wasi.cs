// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static Interop;
using static Interop.Sys;

namespace System.Threading
{
    // types here are clone of private implementation details of wasi-libc
    // we could get rid of it when https://github.com/WebAssembly/wasi-libc/issues/542 is resolved
    // or after WASIp3 promises are implemented, whatever comes first

    [StructLayout(LayoutKind.Sequential)]
    internal struct tcp_own_tcp_socket_t
    {
        public int handle;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct udp_own_udp_socket_t
    {
        public int handle;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct udp_own_incoming_datagram_stream_t
    {
        public int handle;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct udp_own_outgoing_datagram_stream_t
    {
        public int handle;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct streams_own_input_stream_t
    {
        public int handle;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct poll_own_pollable_t
    {
        public int handle;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct streams_own_output_stream_t
    {
        public int handle;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct tcp_socket_state_unbound_t
    {
        public int dummy;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct tcp_socket_state_bound_t
    {
        public int dummy;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct tcp_socket_state_connecting_t
    {
        public int dummy;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct tcp_socket_state_listening_t
    {
        public int dummy;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct tcp_socket_state_connected_t
    {
        public streams_own_input_stream_t input;
        public poll_own_pollable_t input_pollable;
        public streams_own_output_stream_t output;
        public poll_own_pollable_t output_pollable;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct tcp_socket_state_connect_failed_t
    {
        public int error_code;
    }

    internal enum tcp_socket_state_tag
    {
        TCP_SOCKET_STATE_UNBOUND,
        TCP_SOCKET_STATE_BOUND,
        TCP_SOCKET_STATE_CONNECTING,
        TCP_SOCKET_STATE_CONNECTED,
        TCP_SOCKET_STATE_CONNECT_FAILED,
        TCP_SOCKET_STATE_LISTENING,
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct tcp_socket_state_union
    {
        [FieldOffset(0)] public tcp_socket_state_unbound_t unbound;
        [FieldOffset(0)] public tcp_socket_state_bound_t bound;
        [FieldOffset(0)] public tcp_socket_state_connecting_t connecting;
        [FieldOffset(0)] public tcp_socket_state_connected_t connected;
        [FieldOffset(0)] public tcp_socket_state_connect_failed_t connect_failed;
        [FieldOffset(0)] public tcp_socket_state_listening_t listening;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct tcp_socket_state_t
    {
        public tcp_socket_state_tag tag;
        public tcp_socket_state_union state;
    }

    // Layout mirrors wasi-libc (WASIp2) tcp_socket_t from
    // https://github.com/WebAssembly/wasi-libc/blob/161b3195fc25/libc-bottom-half/headers/private/wasi/tcp.h
    // Only `socket`, `state` and `socket_pollable` are read; trailing fields are modelled for completeness.
    [StructLayout(LayoutKind.Sequential)]
    internal struct tcp_socket_t
    {
        public tcp_own_tcp_socket_t socket;
        public tcp_socket_state_t state;
        public poll_own_pollable_t socket_pollable;
        public byte blocking;
        public byte fake_nodelay;
        public byte fake_reuseaddr;
        public byte family;
        public ulong send_timeout;
        public ulong recv_timeout;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct udp_socket_streams_t
    {
        public udp_own_incoming_datagram_stream_t incoming;
        public poll_own_pollable_t incoming_pollable;
        public udp_own_outgoing_datagram_stream_t outgoing;
        public poll_own_pollable_t outgoing_pollable;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct udp_socket_state_unbound_t
    {
        public int dummy;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct udp_socket_state_bound_nostreams_t
    {
        public int dummy;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct udp_socket_state_bound_streaming_t
    {
        public udp_socket_streams_t streams;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct udp_socket_state_connected_t
    {
        public udp_socket_streams_t streams;
    }

    internal enum udp_socket_state_tag
    {
        UDP_SOCKET_STATE_UNBOUND,
        UDP_SOCKET_STATE_BOUND_NOSTREAMS,
        UDP_SOCKET_STATE_BOUND_STREAMING,
        UDP_SOCKET_STATE_CONNECTED,
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct udp_socket_state_union
    {
        [FieldOffset(0)] public udp_socket_state_unbound_t unbound;
        [FieldOffset(0)] public udp_socket_state_bound_nostreams_t bound_nostreams;
        [FieldOffset(0)] public udp_socket_state_bound_streaming_t bound_streaming;
        [FieldOffset(0)] public udp_socket_state_connected_t connected;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct udp_socket_state_t
    {
        public udp_socket_state_tag tag;
        public udp_socket_state_union state;
    }

    // Layout mirrors wasi-libc (WASIp2) udp_socket_t from
    // https://github.com/WebAssembly/wasi-libc/blob/161b3195fc25/libc-bottom-half/sources/udp.c
    [StructLayout(LayoutKind.Sequential)]
    internal struct udp_socket_t
    {
        public udp_own_udp_socket_t socket;
        public poll_own_pollable_t socket_pollable;
        public byte blocking;
        public byte family;
        public udp_socket_state_t state;
    }

    public sealed partial class UnixHandleAsyncContext
    {
        public static bool IsSupported => true;

        internal bool IsRegistered
        {
            get;
            private set;
        }

        private CancellationTokenSource? _unregisterPollHook;

        private bool TryRegisterWithPollThread(out Interop.Error error)
        {
            // Multiple callers may try to register concurrently.
            using (_writeQueue.Lock()) // Lock is used for IsDisposed.
            {
                if (IsDisposed || IsRegistered)
                {
                    // Already registered/disposed.
                    error = Interop.Error.SUCCESS;
                    return true;
                }

                nint entryPtr = default;
                int socketType = 0;
                IntPtr socketHandle = Handle.DangerousGetHandle();
                unsafe { error = Interop.Sys.GetWasiSocketDescriptor(socketHandle, &entryPtr, &socketType); }
                if (error != Interop.Error.SUCCESS)
                {
                    return false; // Registration failed.
                }

                _unregisterPollHook = new CancellationTokenSource();
                Thread.RegisterWasiPollHook(this, BeforePollHook, HandlePollEvent, _unregisterPollHook.Token);

                IsRegistered = true;
                return true; // Successfully registered.
            }
        }

        private bool Register(Operation _)
        {
            if (TryRegisterWithPollThread(out Interop.Error error))
            {
                // Registration was a success. Operation will be triggered by poll thread.
                return true;
            }

            if (error == Interop.Error.ENOMEM || error == Interop.Error.ENOSPC)
            {
                throw new OutOfMemoryException();
            }

            throw new InvalidOperationException($"Unexpected error: {error}");
        }

        private void Unregister()
        {
            Debug.Assert(IsDisposed);
            if (IsRegistered)
            {
                _unregisterPollHook?.Cancel();
            }
        }

        // Kinds understood by SystemNative_WasiSubscribeSocketPollable.
        private const int SubscribeKindInputStream = 0;
        private const int SubscribeKindOutputStream = 1;
        private const int SubscribeKindTcpSocket = 2;
        private const int SubscribeKindIncomingDatagramStream = 4;
        private const int SubscribeKindOutgoingDatagramStream = 5;

        // In the new wasi-libc, the pollables embedded in the socket state are created lazily:
        // their handle stays 0 until the matching `subscribe` import is called. Subscribe on demand
        // and store the handle back into the socket state so wasi-libc owns and later drops it.
        private static int EnsurePollable(int kind, int streamOrSocketHandle, ref int pollableHandle)
        {
            if (pollableHandle == 0)
            {
                pollableHandle = Interop.Sys.WasiSubscribeSocketPollable(kind, streamOrSocketHandle);
            }
            if (pollableHandle == 0)
            {
                Environment.FailFast("Failed to subscribe WASI pollable for kind " + kind + " and handle " + streamOrSocketHandle);
            }
            return pollableHandle;
        }

        private static unsafe IList<int> BeforePollHook(object? state)
        {
            var asyncContext = (UnixHandleAsyncContext)state!;
            if (asyncContext.Handle.IsClosed)
            {
                return [];
            }

            List<int> pollableHandles = new();
            nint entryPtr = default;
            int socketType = 0;
            IntPtr socketHandle = asyncContext.Handle.DangerousGetHandle();
            var error = Interop.Sys.GetWasiSocketDescriptor(socketHandle, &entryPtr, &socketType);
            if (error != Interop.Error.SUCCESS)
            {
                Environment.FailFast("Can't resolve libc descriptor for socket handle " + socketHandle);
            }

            // wasi-libc no longer exposes a tagged union for descriptor entries; entryPtr points
            // directly at the socket struct and socketType (1 = TCP/stream, 2 = UDP/datagram)
            // is derived natively via SO_TYPE.
            switch (socketType)
            {
                case 1:
                {
                    tcp_socket_t* socket = (tcp_socket_t*)entryPtr;
                    switch (socket->state.tag)
                    {
                        case tcp_socket_state_tag.TCP_SOCKET_STATE_CONNECTING:
                        case tcp_socket_state_tag.TCP_SOCKET_STATE_LISTENING:
                            pollableHandles.Add(EnsurePollable(SubscribeKindTcpSocket, socket->socket.handle, ref socket->socket_pollable.handle));
                            break;
                        case tcp_socket_state_tag.TCP_SOCKET_STATE_CONNECTED:
                            pollableHandles.Add(EnsurePollable(SubscribeKindInputStream, socket->state.state.connected.input.handle, ref socket->state.state.connected.input_pollable.handle));
                            pollableHandles.Add(EnsurePollable(SubscribeKindOutputStream, socket->state.state.connected.output.handle, ref socket->state.state.connected.output_pollable.handle));
                            break;
                        case tcp_socket_state_tag.TCP_SOCKET_STATE_CONNECT_FAILED:
                            asyncContext.HandleEventsInline(Sys.HandleEvents.Error);
                            break;
                        case tcp_socket_state_tag.TCP_SOCKET_STATE_UNBOUND:
                        case tcp_socket_state_tag.TCP_SOCKET_STATE_BOUND:
                            break;
                        default:
                            throw new NotImplementedException("TCP:" + socket->state.tag);
                    }
                    break;
                }
                case 2:
                {
                    udp_socket_t* socket = (udp_socket_t*)entryPtr;
                    switch (socket->state.tag)
                    {
                        case udp_socket_state_tag.UDP_SOCKET_STATE_UNBOUND:
                        case udp_socket_state_tag.UDP_SOCKET_STATE_BOUND_NOSTREAMS:
                            asyncContext.HandleEventsInline(Sys.HandleEvents.Read | Sys.HandleEvents.Write);
                            break;
                        case udp_socket_state_tag.UDP_SOCKET_STATE_BOUND_STREAMING:
                        case udp_socket_state_tag.UDP_SOCKET_STATE_CONNECTED:
                        {
                            udp_socket_streams_t* streams;
                            if (socket->state.tag == udp_socket_state_tag.UDP_SOCKET_STATE_BOUND_STREAMING)
                            {
                                streams = &(socket->state.state.bound_streaming.streams);
                            }
                            else
                            {
                                streams = &(socket->state.state.connected.streams);
                            }
                            pollableHandles.Add(EnsurePollable(SubscribeKindIncomingDatagramStream, streams->incoming.handle, ref streams->incoming_pollable.handle));
                            pollableHandles.Add(EnsurePollable(SubscribeKindOutgoingDatagramStream, streams->outgoing.handle, ref streams->outgoing_pollable.handle));
                            break;
                        }

                        default:
                            throw new NotImplementedException("UDP" + socket->state.tag);
                    }
                    break;
                }
                default:
                    throw new NotImplementedException("TYPE" + socketType);
            }
            return pollableHandles;
        }

        private static void HandlePollEvent(object? state)
        {
            UnixHandleAsyncContext asyncContext = (UnixHandleAsyncContext)state!;
            try
            {
                using (ExecutionContext.SuppressFlow())
                {
                    asyncContext.HandleEventsInline(Sys.HandleEvents.Write | Sys.HandleEvents.Read);
                }
            }
            catch (Exception e)
            {
                Environment.FailFast("Exception thrown from UnixHandleAsyncContext event handler: " + e.ToString(), e);
            }
        }

    }
}
