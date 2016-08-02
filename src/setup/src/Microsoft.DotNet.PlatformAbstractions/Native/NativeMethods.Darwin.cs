﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.DotNet.InternalAbstractions.Native
{
    internal static partial class NativeMethods
    {
        public static class Darwin
        {
            private const int CTL_KERN = 1;
            private const int KERN_OSRELEASE = 2;

            public unsafe static string GetKernelRelease()
            {
                const uint BUFFER_LENGTH = 32;

                var name = stackalloc int[2];
                name[0] = CTL_KERN;
                name[1] = KERN_OSRELEASE;

                var buf = stackalloc byte[(int)BUFFER_LENGTH];
                var len = stackalloc uint[1];
                *len = BUFFER_LENGTH;

                try
                {
                    // If the buffer isn't big enough, it seems sysctl still returns 0 and just sets len to the
                    // necessary buffer size. This appears to be contrary to the man page, but it's easy to detect
                    // by simply checking len against the buffer length.
                    if (sysctl(name, 2, buf, len, IntPtr.Zero, 0) == 0 && *len < BUFFER_LENGTH)
                    {
                        return Marshal.PtrToStringAnsi((IntPtr)buf, (int)*len);
                    }
                }
                catch (Exception ex)
                {
                    throw new PlatformNotSupportedException("Error reading Darwin Kernel Version", ex);
                }
                throw new PlatformNotSupportedException("Unknown error reading Darwin Kernel Version");
            }

            [DllImport("libc")]
            private unsafe static extern int sysctl(
                int* name,
                uint namelen,
                byte* oldp,
                uint* oldlenp,
                IntPtr newp,
                uint newlen);
        }
    }
}
