// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    internal static partial class LocalAppContextSwitches
    {
        internal static bool DisableIPv6 { get; } = InitializeDisableIPv6();

        private static bool InitializeDisableIPv6()
        {
            bool value = false;
            if (GetSwitchValue("System.Net.DisableIPv6", ref value))
            {
                return value;
            }

            return GetBooleanEnvironmentVariable("DOTNET_SYSTEM_NET_DISABLEIPV6");
        }
    }
}
