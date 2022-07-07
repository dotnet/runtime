#pragma warning disable IDE0073
//
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
//
#pragma warning restore IDE0073

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
