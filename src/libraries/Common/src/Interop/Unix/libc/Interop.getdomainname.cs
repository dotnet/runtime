// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class libc
    {
        [DllImport(Libraries.Libc, SetLastError = true)]
        private static extern unsafe int getdomainname(byte* name, int len);

        internal static unsafe string getdomainname()
        {
            const int HOST_NAME_MAX = 255; // man getdomainname
            const int ArrLength = HOST_NAME_MAX + 1;

            byte* name = stackalloc byte[ArrLength];
            int err = getdomainname(name, ArrLength);
            if (err != 0)
            {
                // This should never happen.  According to the man page,
                // the only possible errno for getdomainname is ENAMETOOLONG,
                // which should only happen if the buffer we supply isn't big
                // enough, and we're using a buffer size that the man page
                // says is the max for POSIX (and larger than the max for Linux).
                Debug.Fail("getdomainname failed");
                throw new InvalidOperationException(string.Format("getdomainname returned {0}", err));
            }

            // Marshal.PtrToStringAnsi uses UTF8 on Unix.
            return Marshal.PtrToStringAnsi((IntPtr)name);
        }
    }
}
