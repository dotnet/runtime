// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Quic
{
    internal static unsafe class MsQuicExtensions
    {
        public static void SetConnectionCallback(this ref QUIC_API_TABLE Table, QUIC_HANDLE* Handle, delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void*, QUIC_CONNECTION_EVENT*, int> Callback, void* Context)
        {
            Table.SetCallbackHandler(Handle, Callback, Context);
        }

        public static void SetStreamCallback(this ref QUIC_API_TABLE Table, QUIC_HANDLE* Handle, delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void*, QUIC_STREAM_EVENT*, int> Callback, void* Context)
        {
            Table.SetCallbackHandler(Handle, Callback, Context);
        }

        public static void SetListenerCallback(this ref QUIC_API_TABLE Table, QUIC_HANDLE* Handle, delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void*, QUIC_LISTENER_EVENT*, int> Callback, void* Context)
        {
            Table.SetCallbackHandler(Handle, Callback, Context);
        }
    }
}
