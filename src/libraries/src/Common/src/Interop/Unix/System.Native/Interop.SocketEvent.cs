// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_CreateSocketEventPort")]
        internal static extern unsafe Error CreateSocketEventPort(out int port);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_CloseSocketEventPort")]
        internal static extern Error CloseSocketEventPort(int port);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_CreateSocketEventBuffer")]
        internal static extern unsafe Error CreateSocketEventBuffer(int count, out SocketEvent* buffer);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_FreeSocketEventBuffer")]
        internal static extern unsafe Error FreeSocketEventBuffer(SocketEvent* buffer);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_TryChangeSocketEventRegistration")]
        internal static extern Error DangerousTryChangeSocketEventRegistration(int port, int socket, SocketEvents currentEvents, SocketEvents newEvents, IntPtr data);

        internal static Error TryChangeSocketEventRegistration(int port, SafeHandle socket, SocketEvents currentEvents, SocketEvents newEvents, IntPtr data)
        {
            bool release = false;
            try
            {
                socket.DangerousAddRef(ref release);
                return DangerousTryChangeSocketEventRegistration(port, (int)socket.DangerousGetHandle(), currentEvents, newEvents, data);
            }
            finally
            {
                if (release)
                {
                    socket.DangerousRelease();
                }
            }
        }

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_WaitForSocketEvents")]
        internal static extern unsafe Error WaitForSocketEvents(int port, SocketEvent* buffer, int* count);
    }
}
