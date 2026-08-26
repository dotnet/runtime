// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System
{
    internal static partial class LocalAppContextSwitches
    {
        private static int s_disableIPv6;
        internal static bool DisableIPv6
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetCachedSwitchValue("System.Net.DisableIPv6", "DOTNET_SYSTEM_NET_DISABLEIPV6", ref s_disableIPv6);
        }

        private static int s_enableSslKeyLogging;
        internal static bool EnableSslKeyLogging
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if DEBUG
            get => GetCachedSwitchValue("System.Net.EnableSslKeyLogging", ref s_enableSslKeyLogging, defaultValue: true);
#else
            get => GetCachedSwitchValue("System.Net.EnableSslKeyLogging", ref s_enableSslKeyLogging);
#endif
        }
    }
}
