// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_INotifyInit", SetLastError = true)]
        internal static extern SafeFileHandle INotifyInit();

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_INotifyAddWatch", SetLastError = true)]
        internal static extern int INotifyAddWatch(SafeFileHandle fd, string pathName, uint mask);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_INotifyRemoveWatch", SetLastError = true)]
        private static extern int INotifyRemoveWatch_private(SafeFileHandle fd, int wd);

        internal static int INotifyRemoveWatch(SafeFileHandle fd, int wd)
        {
            int result = INotifyRemoveWatch_private(fd, wd);
            if (result < 0)
            {
                Error hr = GetLastError();
                if (hr == Error.EINVAL)
                {
                    // This specific case means that there was a deleted event in the queue that was not processed
                    // so this call is expected to fail since the WatchDescriptor is no longer valid and was cleaned
                    // up automatically by the OS.
                    result = 0;
                }
                else
                {
                    Debug.Fail("inotify_rm_watch failed with " + hr);
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
            IN_Q_OVERFLOW  = 0x00004000,
            IN_IGNORED     = 0x00008000,
            IN_ONLYDIR     = 0x01000000,
            IN_DONT_FOLLOW = 0x02000000,
            IN_EXCL_UNLINK = 0x04000000,
            IN_ISDIR       = 0x40000000,
        }
    }
}
