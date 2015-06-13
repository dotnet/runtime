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
            O_RDONLY    = 0x0000,
            O_WRONLY    = 0x0001,
            O_RDWR      = 0x0002,
            O_CREAT     = 0x0200,
            O_TRUNC     = 0x0400,
            O_EXCL      = 0x0800,
            O_SYNC      = 0x0080,
            O_ASYNC     = 0x0040,
            O_CLOEXEC   = 0x00100000,
        }
    }
}
