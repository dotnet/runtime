// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libc
    {
        [DllImport(Libraries.Libc)]
        internal static extern int inotify_init();

        [DllImport(Libraries.Libc)]
        internal static extern int inotify_add_watch(int fd, string pathname, uint mask);

        [DllImport(Libraries.Libc)]
        internal static extern int inotify_rm_watch(int fd, int wd);

        [Flags]
        internal enum NotifyEvents
        {
            IN_ACCESS      = 0x00000001,
            IN_MODIFY      = 0x00000002,
            IN_ATTRIB      = 0x00000004,
            IN_MOVED_FROM  = 0x00000040,
            IN_MOVED_TO    = 0x00000080,
            IN_CREATE      = 0x00000100,
            IN_DELETE      = 0x00000200,
            IN_UNMOUNT     = 0x00002000,
            IN_Q_OVERFLOW  = 0x00004000,
            IN_IGNORED     = 0x00008000,
            IN_ONLYDIR     = 0x01000000,
            IN_EXCL_UNLINK = 0x04000000,
            IN_ISDIR       = 0x40000000,
        }
    }
}
