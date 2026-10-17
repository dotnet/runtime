// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [Flags]
        internal enum HandleEvents : int
        {
            None = 0x00,
            Read = 0x01,
            Write = 0x02,
            ReadClose = 0x04,
            Close = 0x08,
            Error = 0x10
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct HandleEvent
        {
            public IntPtr Data;
            public HandleEvents Events;
            private int _padding;
        }

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_CreateHandleEventPort")]
        internal static unsafe partial Error CreateHandleEventPort(IntPtr* port);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_CloseHandleEventPort")]
        internal static partial Error CloseHandleEventPort(IntPtr port);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_CreateHandleEventBuffer")]
        internal static unsafe partial Error CreateHandleEventBuffer(int count, HandleEvent** buffer);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_FreeHandleEventBuffer")]
        internal static unsafe partial Error FreeHandleEventBuffer(HandleEvent* buffer);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_TryChangeHandleEventRegistration")]
        internal static partial Error TryChangeHandleEventRegistration(IntPtr port, SafeHandle socket, HandleEvents currentEvents, HandleEvents newEvents, IntPtr data);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_TryChangeHandleEventRegistration")]
        internal static partial Error TryChangeHandleEventRegistration(IntPtr port, IntPtr socket, HandleEvents currentEvents, HandleEvents newEvents, IntPtr data);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_WaitForHandleEvents")]
        internal static unsafe partial Error WaitForHandleEvents(IntPtr port, HandleEvent* buffer, int* count);
    }
}
