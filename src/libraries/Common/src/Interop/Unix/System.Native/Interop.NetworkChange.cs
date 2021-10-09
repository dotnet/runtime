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

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_CreateNetworkChangeListenerSocket")]
        public static extern Error CreateNetworkChangeListenerSocket(out int socket);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_CloseNetworkChangeListenerSocket")]
        public static extern Error CloseNetworkChangeListenerSocket(int socket);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_ReadEvents")]
        public static extern unsafe void ReadEvents(int socket, delegate* unmanaged<int, NetworkChangeKind, void> onNetworkChange);
    }
}
