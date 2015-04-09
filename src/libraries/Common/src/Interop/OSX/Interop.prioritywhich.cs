// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

internal static partial class Interop
{
    internal static partial class libc
    {
        internal enum PriorityWhich
        {
            PRIO_PROCESS = 0,
            PRIO_PGRP = 1,
            PRIO_USER = 2,
            PRIO_DARWIN_THREAD = 3,
            PRIO_DARWIN_PROCESS = 4
        }
    }
}
