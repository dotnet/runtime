// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System;

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

        [GeneratedDllImport(Libraries.SystemNative, EntryPoint = "SystemNative_CreateNetworkChangeListenerSocket")]
        public static unsafe partial Error CreateNetworkChangeListenerSocket(int* socket);

        [GeneratedDllImport(Libraries.SystemNative, EntryPoint = "SystemNative_CloseNetworkChangeListenerSocket")]
        public static partial Error CloseNetworkChangeListenerSocket(int socket);

        [GeneratedDllImport(Libraries.SystemNative, EntryPoint = "SystemNative_ReadEvents")]
        public static unsafe partial void ReadEvents(int socket, delegate* unmanaged<int, NetworkChangeKind, void> onNetworkChange);
    }
}
