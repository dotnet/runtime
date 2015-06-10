// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

internal static partial class Interop
{
    internal static partial class libc
    {
        internal enum FcntlCommands
        {
            F_LINUX_SPECIFIC_BASE = 1024,
            F_SETPIPE_SZ = F_LINUX_SPECIFIC_BASE + 7,
            F_GETPIPE_SZ = F_LINUX_SPECIFIC_BASE + 8
        }
    }
}
