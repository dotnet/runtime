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

        static NetSecurityNative()
        {
            GssInitializer.Initialize();
        }

        internal static class GssInitializer
        {
            private const string GssApiLibraryName = "libgssapi_krb5.so.2";
            private static readonly bool s_isInitialized = EnsureGssInitialized() == 0;

            internal static void Initialize()
            {
                if (!s_isInitialized)
                {
                    throw new DllNotFoundException(GssApiLibraryName);
                }
            }
        }
    }
}
