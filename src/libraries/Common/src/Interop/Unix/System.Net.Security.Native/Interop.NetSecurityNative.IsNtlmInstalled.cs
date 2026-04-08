// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class NetSecurityNative
    {
        [LibraryImport(Interop.Libraries.NetSecurityNative, EntryPoint = "NetSecurityNative_IsNtlmInstalled")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool IsNtlmInstalled();

        [LibraryImport(Interop.Libraries.NetSecurityNative, EntryPoint = "NetSecurityNative_EnsureGssInitialized")]
        private static partial int EnsureGssInitialized();

        private static readonly int s_GssInitResult;
        private const string GssApiLibraryName = "libgssapi_krb5.so.2";

        static NetSecurityNative()
        {
            GssInitializer.Initialize();

            if (s_GssInitResult != 0)
            {
                throw new DllNotFoundException(GssApiLibraryName);
            }
        }

        internal static class GssInitializer
        {
            static GssInitializer()
            {
                s_GssInitResult = EnsureGssInitialized();
            }

            internal static void Initialize()
            {
                // No-op that exists to provide a hook for other static constructors.
            }
        }
    }
}
