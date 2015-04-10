// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libc
    {
        [DllImport(Libraries.Libc, SetLastError = true)]
        internal static extern int closedir(IntPtr dirp);

        internal sealed class SafeDirHandle : SafeHandle
        {
            private SafeDirHandle() : base(IntPtr.Zero, true)
            {
            }

            protected override bool ReleaseHandle()
            {
                return closedir(handle) == 0;
            }

            public override bool IsInvalid
            {
                get { return handle == IntPtr.Zero; }
            }
        }
    }
}
