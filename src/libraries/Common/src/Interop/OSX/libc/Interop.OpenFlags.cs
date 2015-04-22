// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

internal static partial class Interop
{
    internal static partial class libc
    {
        [Flags]
        internal enum OpenFlags
        {
            O_RDONLY    = 0x0,
            O_WRONLY    = 0x1,
            O_RDWR      = 0x2,
            O_CREAT     = 0x200,
            O_EXCL      = 0x800,
            O_TRUNC     = 0x400,
            O_SYNC      = 0x80,
            O_ASYNC     = 0x40,
            O_CLOEXEC   = 0x1000000,
        }
    }
}
