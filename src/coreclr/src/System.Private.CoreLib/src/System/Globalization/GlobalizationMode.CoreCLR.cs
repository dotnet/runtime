// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace System.Globalization
{
    internal static partial class GlobalizationMode
    {
        // GetSwitchValue calls CLRConfig first to detect if the switch is defined in the config file.
        // if the switch is defined we just use the value of this switch. otherwise, we'll try to get the switch
        // value from the environment variable if it is defined.
        private static bool GetSwitchValue(string switchName, string envVariable)
        {
            if (!AppContext.TryGetSwitch(switchName, out bool ret))
            {
                string? switchValue = Environment.GetEnvironmentVariable(envVariable);
                if (switchValue != null)
                {
                    ret = bool.IsTrueStringIgnoreCase(switchValue) || switchValue.Equals("1");
                }
            }

            return ret;
        }

        private static bool TryGetStringValue(string switchName, string envVariable, [NotNullWhen(true)] out string? value)
        {
            value = AppContext.GetData(switchName) as string;
            if (string.IsNullOrEmpty(value))
            {
                value = Environment.GetEnvironmentVariable(envVariable);
                if (string.IsNullOrEmpty(value))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
