// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        internal static bool GetCachedSwitchValue(string switchName, ref int cachedSwitchValue, bool defaultValue = false)
        {
            // The cached switch value has 3 states: 0 - unknown, 1 - true, -1 - false
            if (cachedSwitchValue < 0) return false;
            if (cachedSwitchValue > 0) return true;

            return GetCachedSwitchValueInternal(switchName, null, ref cachedSwitchValue, defaultValue);
        }

        // Returns value of given switch or environment variable using provided cache.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool GetCachedSwitchValue(string switchName, string? envVariable, ref int cachedSwitchValue, bool defaultValue = false)
        {
            // The cached switch value has 3 states: 0 - unknown, 1 - true, -1 - false
            if (cachedSwitchValue < 0) return false;
            if (cachedSwitchValue > 0) return true;

            return GetCachedSwitchValueInternal(switchName, envVariable, ref cachedSwitchValue, defaultValue);
        }

        private static bool GetCachedSwitchValueInternal(string switchName, string? envVariable, ref int cachedSwitchValue, bool defaultValue)
        {
            if (!AppContext.TryGetSwitch(switchName, out bool isSwitchEnabled))
            {
                if (envVariable == null || !TryGetBooleanEnvironmentVariable(envVariable, out isSwitchEnabled))
                {
                    isSwitchEnabled = defaultValue;
                }
            }

            AppContext.TryGetSwitch("TestSwitch.LocalAppContext.DisableCaching", out bool disableCaching);
            if (!disableCaching)
            {
                cachedSwitchValue = isSwitchEnabled ? 1 /*true*/ : -1 /*false*/;
            }

            return isSwitchEnabled;
        }

        private static bool TryGetBooleanEnvironmentVariable(string envVariable, out bool value)
        {
            if (Environment.GetEnvironmentVariable(envVariable) is string str)
            {
                if (str == "1" || string.Equals(str, bool.TrueString, StringComparison.OrdinalIgnoreCase))
                {
                    value = true;
                    return true;
                }

                if (str == "0" || string.Equals(str, bool.FalseString, StringComparison.OrdinalIgnoreCase))
                {
                    value = false;
                    return true;
                }

                value = false;
                return false;
            }

            value = false;
            return false;
        }
    }
}
