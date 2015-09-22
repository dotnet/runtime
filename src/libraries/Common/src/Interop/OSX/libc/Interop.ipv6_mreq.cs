// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

internal static partial class Interop
{
    internal static partial class libc
    {
        public struct ipv6_mreq
        {
            public in6_addr ipv6mr_multiaddr;
            public int ipv6mr_ifindex;
        }
    }
}
