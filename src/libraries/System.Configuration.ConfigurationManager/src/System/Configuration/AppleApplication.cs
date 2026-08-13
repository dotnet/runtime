// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace System.Configuration
{
    internal static partial class AppleApplication
    {
        internal static string GetMainBundleIdentifier()
        {
            IntPtr identifier = Interop.GetMainBundleIdentifier();
            if (identifier == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                // Apple bundle identifiers are restricted to ASCII letters, digits, periods, and hyphens.
                return Marshal.PtrToStringAnsi(identifier);
            }
            finally
            {
                Interop.Free(identifier);
            }
        }

        private static partial class Interop
        {
            [LibraryImport("libSystem.Native", EntryPoint = "SystemNative_Free")]
            internal static partial void Free(IntPtr ptr);

            [LibraryImport("libSystem.Native", EntryPoint = "SystemNative_GetMainBundleIdentifier")]
            internal static partial IntPtr GetMainBundleIdentifier();
        }
    }
}
