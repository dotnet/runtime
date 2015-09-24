// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

internal static partial class Interop
{
    internal static partial class libc
    {
        public struct ip_mreqn
        {
            public in_addr imr_multiaddr;
            public in_addr imr_address;
            public int imr_ifindex;
        }
    }
}
