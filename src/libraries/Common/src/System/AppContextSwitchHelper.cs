// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace System
{
    internal static class AppContextSwitchHelper
    {
        internal static bool GetBooleanConfig(string switchName, bool defaultValue) =>
            AppContext.TryGetSwitch(switchName, out bool value) ? value : defaultValue;

        internal static bool GetBooleanConfig(string switchName, string envVariable, bool defaultValue = false)
        {
            if (Environment.GetEnvironmentVariable(envVariable) is string str)
            {
                if (str == "1" || string.Equals(str, bool.TrueString, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                if (str == "1" || string.Equals(str, bool.FalseString, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return GetBooleanConfig(switchName, defaultValue);
        }
    }
}
