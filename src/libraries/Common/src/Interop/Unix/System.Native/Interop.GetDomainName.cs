// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetDomainName")]
        private static unsafe partial int GetDomainName(byte* name, int len);

        internal static unsafe string GetDomainName()
        {
            const int HOST_NAME_MAX = 255; // man getdomainname
            const int ArrLength = HOST_NAME_MAX + 1;

            byte* name = stackalloc byte[ArrLength];
            int err = GetDomainName(name, ArrLength);
            if (err != 0)
            {
                // This should never happen.  According to the man page,
                // the only possible errno for getdomainname is ENAMETOOLONG,
                // which should only happen if the buffer we supply isn't big
                // enough, and we're using a buffer size that the man page
                // says is the max for POSIX (and larger than the max for Linux).
                Debug.Fail($"{nameof(GetDomainName)} failed with error {err}");
                throw new InvalidOperationException($"{nameof(GetDomainName)}: {err}");
            }

            return Marshal.PtrToStringUTF8((IntPtr)name)!;
        }
    }
}
