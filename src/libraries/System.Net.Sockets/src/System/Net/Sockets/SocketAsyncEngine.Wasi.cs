// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using static Interop;
using static Interop.Sys;

namespace System.Net.Sockets
{
    internal sealed unsafe class SocketAsyncEngine
    {
        internal const bool InlineSocketCompletionsEnabled = true;
        private static readonly SocketAsyncEngine s_engine = new SocketAsyncEngine();

        public static bool TryRegisterSocket(IntPtr socketHandle, SocketAsyncContext context, out SocketAsyncEngine? engine, out Interop.Error error)
        {
            engine = s_engine;

            nint entryPtr = default;
            error = Interop.Sys.GetWasiSocketDescriptor(socketHandle, &entryPtr);
            if (error != Interop.Error.SUCCESS)
            {
                return false;
            }

            RegisterWasiPollHook(context, BeforePollHook, HandleSocketEvent, context.unregisterPollHook.Token);

            return true;
        }

        public static void UnregisterSocket(SocketAsyncContext context)
        {
            context.unregisterPollHook.Cancel();
        }

        // this method is invading private implementation details of wasi-libc
        // we could get rid of it when https://github.com/WebAssembly/wasi-libc/issues/542 is resolved
        // or after WASIp3 promises are implemented, whatever comes first
        public static IList<int> BeforePollHook(object? state)
        {
            var context = (SocketAsyncContext)state!;
            if (context._socket.IsClosed)
            {
                return [];
            }

            List<int> pollableHandles = new();
            // fail fast if the handle is not found in the descriptor table
            // probably because the socket was closed and the entry was removed, without unregistering the poll hook
            nint entryPtr = default;
            IntPtr socketHandle = context._socket.DangerousGetHandle();
            var error = Interop.Sys.GetWasiSocketDescriptor(socketHandle, &entryPtr);
            if (error != Interop.Error.SUCCESS)
            {
                Environment.FailFast("Can't resolve libc descriptor for socket handle " + socketHandle);
            }

            var entry = (descriptor_table_entry_t*)entryPtr;
            switch (entry->tag)
            {
                case descriptor_table_entry_tag.DESCRIPTOR_TABLE_ENTRY_TCP_SOCKET:
                {
                    tcp_socket_t* socket = &(entry->entry.tcp_socket);
                    switch (socket->state.tag)
                    {
                        case tcp_socket_state_tag.TCP_SOCKET_STATE_CONNECTING:
                        case tcp_socket_state_tag.TCP_SOCKET_STATE_LISTENING:
                            pollableHandles.Add(socket->socket_pollable.handle);
                            break;
                        case tcp_socket_state_tag.TCP_SOCKET_STATE_CONNECTED:
                            pollableHandles.Add(socket->state.state.connected.input_pollable.handle);
                            pollableHandles.Add(socket->state.state.connected.output_pollable.handle);
                            break;
                        case tcp_socket_state_tag.TCP_SOCKET_STATE_CONNECT_FAILED:
                            context.HandleEventsInline(Sys.SocketEvents.Error);
                            break;
                        case tcp_socket_state_tag.TCP_SOCKET_STATE_UNBOUND:
                        case tcp_socket_state_tag.TCP_SOCKET_STATE_BOUND:
                            break;
                        default:
                            throw new NotImplementedException("TCP:" + socket->state.tag);
                    }
                    break;
                }
                case descriptor_table_entry_tag.DESCRIPTOR_TABLE_ENTRY_UDP_SOCKET:
                {
                    udp_socket_t* socket = &(entry->entry.udp_socket);
                    switch (socket->state.tag)
                    {
                        case udp_socket_state_tag.UDP_SOCKET_STATE_UNBOUND:
                        case udp_socket_state_tag.UDP_SOCKET_STATE_BOUND_NOSTREAMS:
                            // TODO ? pollableHandles.Add(socket->socket_pollable.handle);
                            context.HandleEventsInline(Sys.SocketEvents.Read | Sys.SocketEvents.Write);
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
                            pollableHandles.Add(streams->incoming_pollable.handle);
                            pollableHandles.Add(streams->outgoing_pollable.handle);
                            break;
                        }

                        default:
                            throw new NotImplementedException("UDP" + socket->state.tag);
                    }
                    break;
                }
                default:
                    throw new NotImplementedException("TYPE" + entry->tag);
            }
            return pollableHandles;
        }

        public static void HandleSocketEvent(object? state)
        {
            SocketAsyncContext ctx = (SocketAsyncContext)state!;
            try
            {
                using (ExecutionContext.SuppressFlow())
                {
                    ctx.HandleEventsInline(Sys.SocketEvents.Write | Sys.SocketEvents.Read);
                }
            }
            catch (Exception e)
            {
                Environment.FailFast("Exception thrown from SocketAsyncEngine event loop: " + e.ToString(), e);
            }
        }

        private static void RegisterWasiPollHook(object? state, Func<object?, IList<int>> beforePollHook, Action<object?> onResolveCallback, CancellationToken cancellationToken)
        {
            CallRegisterWasiPollHook((Thread)null!, state, beforePollHook, onResolveCallback, cancellationToken);

            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "RegisterWasiPollHook")]
            static extern void CallRegisterWasiPollHook(Thread t, object? state, Func<object?, IList<int>> beforePollHook, Action<object?> onResolveCallback, CancellationToken cancellationToken);
        }
    }
}
