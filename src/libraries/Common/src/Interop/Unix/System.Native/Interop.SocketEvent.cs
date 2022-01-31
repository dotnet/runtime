// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [Flags]
        internal enum SocketEvents : int
        {
            None = 0x00,
            Read = 0x01,
            Write = 0x02,
            ReadClose = 0x04,
            Close = 0x08,
            Error = 0x10
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SocketEvent
        {
            public IntPtr Data;
            public SocketEvents Events;
            private int _padding;
        }

        [GeneratedDllImport(Libraries.SystemNative, EntryPoint = "SystemNative_CreateSocketEventPort")]
        internal static unsafe partial Error CreateSocketEventPort(IntPtr* port);

        [GeneratedDllImport(Libraries.SystemNative, EntryPoint = "SystemNative_CloseSocketEventPort")]
        internal static partial Error CloseSocketEventPort(IntPtr port);

        [GeneratedDllImport(Libraries.SystemNative, EntryPoint = "SystemNative_CreateSocketEventBuffer")]
        internal static unsafe partial Error CreateSocketEventBuffer(int count, SocketEvent** buffer);

        [GeneratedDllImport(Libraries.SystemNative, EntryPoint = "SystemNative_FreeSocketEventBuffer")]
        internal static unsafe partial Error FreeSocketEventBuffer(SocketEvent* buffer);

        [GeneratedDllImport(Libraries.SystemNative, EntryPoint = "SystemNative_TryChangeSocketEventRegistration")]
        internal static partial Error TryChangeSocketEventRegistration(IntPtr port, SafeHandle socket, SocketEvents currentEvents, SocketEvents newEvents, IntPtr data);

        [GeneratedDllImport(Libraries.SystemNative, EntryPoint = "SystemNative_TryChangeSocketEventRegistration")]
        internal static partial Error TryChangeSocketEventRegistration(IntPtr port, IntPtr socket, SocketEvents currentEvents, SocketEvents newEvents, IntPtr data);

        [GeneratedDllImport(Libraries.SystemNative, EntryPoint = "SystemNative_WaitForSocketEvents")]
        internal static unsafe partial Error WaitForSocketEvents(IntPtr port, SocketEvent* buffer, int* count);
    }
}
