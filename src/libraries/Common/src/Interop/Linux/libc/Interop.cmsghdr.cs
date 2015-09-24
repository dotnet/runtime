// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

internal static partial class Interop
{
    internal static partial class libc
    {
#pragma warning disable 169, 649
        public unsafe struct cmsghdr
        {
            public IntPtr cmsg_len;
            public int cmsg_level;
            public int cmsg_type;
        }
#pragma warning restore 169, 649
    }
}
