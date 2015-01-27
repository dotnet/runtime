// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libc
    {
        [DllImport(Libraries.Libc, SetLastError = true)]
        internal static extern unsafe IntPtr getmntent_r(IntPtr fp, ref mntent mntbuf, byte* buf, int buflen);

        #pragma warning disable 0649 // set via P/Invoke
        internal unsafe struct mntent
        {
            internal byte* mnt_fsname;
            internal byte* mnt_dir;
            internal byte* mnt_type;
            internal byte* mnt_opts;
            internal int mnt_freq;
            internal int mnt_passno;
        }
        #pragma warning restore 0649
    }
}
