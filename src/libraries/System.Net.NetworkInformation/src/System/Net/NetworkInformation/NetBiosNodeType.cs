// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.NetworkInformation
{
    /// <summary>
    /// Specifies the Network Basic Input/Output System (NetBIOS) node type.
    /// </summary>
    public enum NetBiosNodeType
    {
        Unknown = 0,
        Broadcast = 1,
        Peer2Peer = 2,
        Mixed = 4,
        Hybrid = 8
    }
}
