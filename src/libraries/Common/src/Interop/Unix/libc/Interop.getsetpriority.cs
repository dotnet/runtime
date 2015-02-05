// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libc
    {
        [DllImport(Libraries.Libc, SetLastError = true)]
        internal static extern int getpriority(PriorityWhich which, int who);

        [DllImport(Libraries.Libc, SetLastError = true)]
        internal static extern int setpriority(PriorityWhich which, int who, int prio);

        internal enum PriorityWhich
        {
            PRIO_PROCESS = 0,
            PRIO_PGRP = 1,
            PRIO_USER = 2
        }
    }
}
