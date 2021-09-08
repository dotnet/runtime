// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class NetSecurityNative
    {
        [DllImport(Interop.Libraries.NetSecurityNative, EntryPoint="NetSecurityNative_IsNtlmInstalled")]
        internal static extern bool IsNtlmInstalled();

        [DllImport(Interop.Libraries.NetSecurityNative, EntryPoint = "NetSecurityNative_EnsureGssInitialized")]
        private static extern int EnsureGssInitialized();

        static NetSecurityNative()
        {
            GssInitializer.Initialize();
        }

        internal static class GssInitializer
        {
            static GssInitializer()
            {
                if (EnsureGssInitialized() != 0)
                {
                    throw new InvalidOperationException();
                }
            }

            internal static void Initialize()
            {
                // No-op that exists to provide a hook for other static constructors.
            }
        }
    }
}
