// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

internal static partial class Interop
{
    internal static partial class libc
    {
#pragma warning disable 169, 649
        public unsafe struct in6_pktinfo
        {
            public in6_addr ipi6_addr;
            public int ipi6_ifindex;
        }
#pragma warning restore 169, 649
    }
}
