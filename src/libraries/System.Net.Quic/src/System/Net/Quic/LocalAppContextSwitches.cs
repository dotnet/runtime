// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System
{
    internal static partial class LocalAppContextSwitches
    {
        private static int s_disableConfigurationCache;
        internal static bool DisableConfigurationCache
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetCachedSwitchValue("System.Net.Quic.DisableConfigurationCache", "DOTNET_SYSTEM_NET_QUIC_DISABLE_CONFIGURATION_CACHE", ref s_disableConfigurationCache);
        }

        private static int s_appLocalMsQuic;
        internal static bool AppLocalMsQuic
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetCachedSwitchValue("System.Net.Quic.AppLocalMsQuic", ref s_appLocalMsQuic);
        }
    }
}
