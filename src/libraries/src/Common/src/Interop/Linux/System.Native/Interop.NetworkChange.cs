// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            LinkRemoved = 3
        }

        public delegate void NetworkChangeEvent(NetworkChangeKind kind);

        [DllImport(Libraries.SystemNative)]
        public static extern Error CreateNetworkChangeListenerSocket(out int socket);

        [DllImport(Libraries.SystemNative)]
        public static extern Error CloseNetworkChangeListenerSocket(int socket);

        [DllImport(Libraries.SystemNative)]
        public static extern NetworkChangeKind ReadSingleEvent(int socket);
    }
}
