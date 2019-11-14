// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            LinkAdded = 2,
            LinkRemoved = 3,
            AvailabilityChanged = 4
        }

        public delegate void NetworkChangeEvent(int socket, NetworkChangeKind kind);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_CreateNetworkChangeListenerSocket")]
        public static extern Error CreateNetworkChangeListenerSocket(out int socket);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_CloseNetworkChangeListenerSocket")]
        public static extern Error CloseNetworkChangeListenerSocket(int socket);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_ReadEvents")]
        public static extern void ReadEvents(int socket, NetworkChangeEvent onNetworkChange);
    }
}
