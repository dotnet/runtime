// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class libc
    {
        [DllImport(Libraries.Libc, SetLastError = true)]
        private static extern unsafe int gethostname(byte* name, int len);

        internal static unsafe string gethostname()
        {
            const int HOST_NAME_MAX = 255; // man gethostname
            const int ArrLength = HOST_NAME_MAX + 1;

            byte* name = stackalloc byte[ArrLength];
            int errno = gethostname(name, ArrLength);
            if (errno == 0)
            {
                int nullPos = 0;
                for (; nullPos < ArrLength && name[nullPos] != '\0'; nullPos++) ;
                return Encoding.UTF8.GetString(name, nullPos);
            }

            // This should never happen.  According to the man page,
            // the only possible errno for gethostname is ENAMETOOLONG,
            // which should only happen if the buffer we supply isn't big
            // enough, and we're using a buffer size that the man page
            // says is the max for POSIX (and larger than the max for Linux).
            throw new InvalidOperationException();
        }
    }
}
