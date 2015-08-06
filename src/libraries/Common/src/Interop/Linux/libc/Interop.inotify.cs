// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libc
    {
        [DllImport(Libraries.Libc, SetLastError = true)]
        internal static extern int inotify_init();

        [DllImport(Libraries.Libc, SetLastError = true)]
        internal static extern int inotify_add_watch(int fd, string pathname, uint mask);

        [DllImport(Libraries.Libc, SetLastError = true, EntryPoint = "inotify_rm_watch")]
        private static extern int inotify_rm_watch_extern(int fd, int wd);

        internal static int inotify_rm_watch(int fd, int wd)
        {
            int result = Interop.libc.inotify_rm_watch_extern(fd, wd);
            if (result < 0)
            {
                Error hr = Interop.Sys.GetLastError();
                if (hr == Interop.Error.EINVAL)
                {
                    // This specific case means that there was a deleted event in the queue that was not processed
                    // so this call is expected to fail since the WatchDescriptor is no longer valid and was cleaned
                    // up automatically by the OS.
                    result = 0;
                }
                else
                {
                    System.Diagnostics.Debug.Fail("inotify_rm_watch failed with " + hr);
                }
            }

            return result;
        }

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
            IN_DONT_FOLLOW = 0x02000000,
            IN_EXCL_UNLINK = 0x04000000,
            IN_ISDIR       = 0x40000000,
        }
    }
}
