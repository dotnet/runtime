// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Globalization
{
    internal sealed partial class GlobalizationMode
    {
        internal static bool Invariant { get; } = GetGlobalizationInvariantMode();

        // GetInvariantSwitchValue calls CLRConfig first to detect if the switch is defined in the config file.
        // if the switch is defined we just use the value of this switch. otherwise, we'll try to get the switch
        // value from the environment variable if it is defined.
        internal static bool GetInvariantSwitchValue()
        {
            bool exist;
            bool ret = CLRConfig.GetBoolValue("System.Globalization.Invariant", out exist);
            if (!exist)
            {
                // Linux doesn't support environment variable names include dots
                string? switchValue = Environment.GetEnvironmentVariable("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT");
                if (switchValue != null)
                {
                    ret = bool.IsTrueStringIgnoreCase(switchValue) || switchValue.Equals("1");
                }
            }

            return ret;
        }
    }
}
