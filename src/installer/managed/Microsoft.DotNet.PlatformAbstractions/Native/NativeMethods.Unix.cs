// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET45

using System;
using System.Runtime.InteropServices;

namespace Microsoft.DotNet.PlatformAbstractions.Native
{
    internal static partial class NativeMethods
    {
        public static class Unix
        {
            public unsafe static string GetUname()
            {
                // Utsname shouldn't be larger than 2K
                var buf = stackalloc byte[2048];

                try
                {
                    if (uname((IntPtr)buf) == 0)
                    {
                        return Marshal.PtrToStringAnsi((IntPtr)buf);
                    }
                }
                catch (Exception ex)
                {
                    throw new PlatformNotSupportedException("Error reading Unix name", ex);
                }
                throw new PlatformNotSupportedException("Unknown error reading Unix name");
            }

            [DllImport("libc")]
            private static extern int uname(IntPtr utsname);
        }
    }
}
#endif