// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libc
    {
        internal static bool IsGnu { get { return s_isGnu.Value; } }

        internal static string gnu_get_libc_version()
        {
            return Marshal.PtrToStringAnsi(gnu_get_libc_version_native());
        }

        [DllImport(Libraries.Libc, EntryPoint = "gnu_get_libc_version")]
        private static extern IntPtr gnu_get_libc_version_native();

        private static readonly Lazy<bool> s_isGnu = new Lazy<bool>(() =>
        {
            try
            {
                // Just try to call the P/Invoke.  If it succeeds, this is GNU libc.
                gnu_get_libc_version_native();
                return true;
            }
            catch
            {
                // Otherwise, it's not.
                return false;
            }
        });
    }
}
