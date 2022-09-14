// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace System.Text.Json
{
    internal static class AppContextSwitchHelper
    {
        private const string SourceGenReflectionFallbackEnabled_SwitchName = "System.Text.Json.Serialization.EnableSourceGenReflectionFallback";
        private static int s_SourceGenReflectionFallbackEnabled_CachedValue;

        public static bool IsSourceGenReflectionFallbackEnabled
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetCachedSwitchValue(SourceGenReflectionFallbackEnabled_SwitchName, ref s_SourceGenReflectionFallbackEnabled_CachedValue, defaultValue: false);
        }

        // Returns value of given switch using provided cache.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool GetCachedSwitchValue(string switchName, ref int cachedSwitchValue, bool defaultValue)
        {
            // The cached switch value has 3 states: 0 - unknown, 1 - true, -1 - false
            if (cachedSwitchValue < 0) return false;
            if (cachedSwitchValue > 0) return true;

            return GetCachedSwitchValueInternal(switchName, ref cachedSwitchValue, defaultValue);
        }

        private static bool GetCachedSwitchValueInternal(string switchName, ref int cachedSwitchValue, bool defaultValue)
        {
            if (!AppContext.TryGetSwitch(switchName, out bool isSwitchEnabled))
            {
                isSwitchEnabled = defaultValue;
            }

            cachedSwitchValue = isSwitchEnabled ? 1 /*true*/ : -1 /*false*/;
            return isSwitchEnabled;
        }
    }
}
