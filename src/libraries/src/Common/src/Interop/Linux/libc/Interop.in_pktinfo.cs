// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

internal static partial class Interop
{
    internal static partial class libc
    {
#pragma warning disable 169, 649
        public unsafe struct in_pktinfo
        {
            public int ipi_ifindex;
            public in_addr ipi_spec_dst;
            public in_addr ipi_addr;
        }
#pragma warning restore 169, 649
    }
}
