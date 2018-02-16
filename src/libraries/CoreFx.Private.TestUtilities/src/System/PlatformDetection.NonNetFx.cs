// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Xunit;

namespace System
{
    public static partial class PlatformDetection
    {
        public static bool TargetsNetFx452OrLower => false;
        public static bool IsNetfx462OrNewer => false;
        public static bool IsNetfx470OrNewer => false;
        public static bool IsNetfx471OrNewer => false;


        [DllImport("libc", ExactSpelling = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr gnu_get_libc_release();

        [DllImport("libc", ExactSpelling = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr gnu_get_libc_version();

        /// <summary>
        /// If gnulibc is available, returns the release, such as "stable".
        /// Otherwise (eg., Windows, musl) returns "glibc_not_found".
        /// </summary>
        public static string LibcRelease
        {
            get
            {
                try
                {
                    return Marshal.PtrToStringUTF8(gnu_get_libc_release());
                }
                catch (Exception e) when (e is DllNotFoundException || e is EntryPointNotFoundException)
                {
                    return "glibc_not_found";
                }
            }
        }

        /// <summary>
        /// If gnulibc is available, returns the version, such as "2.22".
        /// Otherwise (eg., Windows, musl) returns "glibc_not_found". (In future could run "ldd -version" for musl)
        /// </summary>
        public static string LibcVersion
        {
            get
            {
                try
                {
                    return Marshal.PtrToStringUTF8(gnu_get_libc_version());
                }
                catch (Exception e) when (e is DllNotFoundException || e is EntryPointNotFoundException)
                {
                    return "glibc_not_found";
                }
            }
        }
    }
}
