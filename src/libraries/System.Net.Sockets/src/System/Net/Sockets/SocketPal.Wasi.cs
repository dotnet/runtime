// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

// types here are clone of private implementation details of wasi-libc
// we could get rid of it when https://github.com/WebAssembly/wasi-libc/issues/542 is resolved
// or after WASIp3 promises are implemented, whatever comes first

namespace System.Net.Sockets
{
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
        public byte error_code;
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

    [StructLayout(LayoutKind.Sequential)]
    internal struct tcp_socket_t
    {
        public tcp_own_tcp_socket_t socket;
        public poll_own_pollable_t socket_pollable;
        public bool blocking;
        public bool fake_nodelay;
        public bool fake_reuseaddr;
        public byte family;
        public tcp_socket_state_t state;
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

    [StructLayout(LayoutKind.Sequential)]
    internal struct udp_socket_t
    {
        public udp_own_udp_socket_t socket;
        public poll_own_pollable_t socket_pollable;
        public bool blocking;
        public byte family;
        public udp_socket_state_t state;
    }

    internal enum descriptor_table_entry_tag
    {
        DESCRIPTOR_TABLE_ENTRY_TCP_SOCKET,
        DESCRIPTOR_TABLE_ENTRY_UDP_SOCKET,
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct descriptor_table_entry_union
    {
        [FieldOffset(0)] public tcp_socket_t tcp_socket;
        [FieldOffset(0)] public udp_socket_t udp_socket;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct descriptor_table_entry_t
    {
        public descriptor_table_entry_tag tag;
        public descriptor_table_entry_union entry;
    }
}
