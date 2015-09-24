// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using socklen_t = System.UInt32;

internal static partial class Interop
{
    internal static partial class libc
    {
#pragma warning disable 169, 649
        public unsafe struct cmsghdr
        {
            public static int Size { get { return sizeof(cmsghdr) - 1; } }

            public socklen_t cmsg_len;
            public int cmsg_level;
            public int cmsg_type;
            public byte cmsg_data; // Actually variably-sized
        }
#pragma warning restore 169, 649
    }
}
