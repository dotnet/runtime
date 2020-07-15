// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Runtime.CompilerServices;

namespace System
{
    // Helper method for local caching of compatibility quirks. Keep this lean and simple - this file is included into
    // every framework assembly that implements any compatibility quirks.
    internal static partial class LocalAppContextSwitches
    {
        // Returns value of given switch using provided cache.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool GetSwitchValue(string switchName, ref bool switchValue) =>
            AppContext.TryGetSwitch(switchName, out switchValue);

        // Returns value of given switch using provided cache.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool GetCachedSwitchValue(string switchName, ref int cachedSwitchValue)
        {
            // The cached switch value has 3 states: 0 - unknown, 1 - true, -1 - false
            if (cachedSwitchValue < 0) return false;
            if (cachedSwitchValue > 0) return true;

            return GetCachedSwitchValueInternal(switchName, ref cachedSwitchValue);
        }

        private static bool GetCachedSwitchValueInternal(string switchName, ref int cachedSwitchValue)
        {
            bool hasSwitch = AppContext.TryGetSwitch(switchName, out bool isSwitchEnabled);
            if (!hasSwitch)
            {
                isSwitchEnabled = GetSwitchDefaultValue(switchName);
            }

            AppContext.TryGetSwitch("TestSwitch.LocalAppContext.DisableCaching", out bool disableCaching);
            if (!disableCaching)
            {
                cachedSwitchValue = isSwitchEnabled ? 1 /*true*/ : -1 /*false*/;
            }

            return isSwitchEnabled;
        }

        // Provides default values for switches if they're not always false by default
        private static bool GetSwitchDefaultValue(string switchName)
        {
            if (switchName == "Switch.System.Runtime.Serialization.SerializationGuard")
            {
                return true;
            }

            if (switchName == "System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization")
            {
                return true;
            }

            return false;
        }
    }
}
