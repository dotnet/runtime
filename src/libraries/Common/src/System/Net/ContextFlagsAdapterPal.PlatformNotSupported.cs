// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Versioning;

namespace System.Net
{
    [UnsupportedOSPlatform("tvos")]
    internal static class ContextFlagsAdapterPal
    {
        internal static ContextFlagsPal GetContextFlagsPalFromInterop(Interop.NetSecurityNative.GssFlags gssFlags, bool isServer)
        {
            throw new PlatformNotSupportedException();
        }

        internal static Interop.NetSecurityNative.GssFlags GetInteropFromContextFlagsPal(ContextFlagsPal flags, bool isServer)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
