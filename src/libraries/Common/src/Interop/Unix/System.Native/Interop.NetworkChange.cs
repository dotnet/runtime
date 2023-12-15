// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        public enum NetworkChangeKind
        {
            None = -1,
            AddressAdded = 0,
            AddressRemoved = 1,
            AvailabilityChanged = 2
        }

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_CreateNetworkChangeListenerSocket", SetLastError = true)]
        public static unsafe partial Error CreateNetworkChangeListenerSocket(IntPtr* socket);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_ReadEvents")]
        public static unsafe partial Error ReadEvents(SafeHandle socket, delegate* unmanaged<IntPtr, NetworkChangeKind, void> onNetworkChange);
    }
}
