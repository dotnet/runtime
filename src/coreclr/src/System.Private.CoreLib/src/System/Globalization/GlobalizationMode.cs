// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Globalization
{
    internal static partial class GlobalizationMode
    {
        // Order of these properties matter because GetUseIcuMode is dependent on Invariant.
        // So we need Invariant to be initialized first.
        internal static bool Invariant { get; } = GetGlobalizationInvariantMode();

#if TARGET_WINDOWS
        internal static bool UseIcu { get; } = GetUseIcuMode();
#else
        // We can't just call GetUseIcuMode and return false there if we want the linker to trim out the Windows code.
        internal static bool UseIcu => true;
#endif

        internal static bool GetInvariantSwitchValue() =>
            GetSwitchValue("System.Globalization.Invariant", "DOTNET_SYSTEM_GLOBALIZATION_INVARIANT");

        internal static bool GetUseNlsSwitchValue() =>
            GetSwitchValue("System.Globalization.UseNls", "DOTNET_SYSTEM_GLOBALIZATION_USENLS");

        // GetSwitchValue calls CLRConfig first to detect if the switch is defined in the config file.
        // if the switch is defined we just use the value of this switch. otherwise, we'll try to get the switch
        // value from the environment variable if it is defined.
        private static bool GetSwitchValue(string switchName, string envVariable)
        {
            bool ret = CLRConfig.GetBoolValue(switchName, out bool exist);
            if (!exist)
            {
                string? switchValue = Environment.GetEnvironmentVariable(envVariable);
                if (switchValue != null)
                {
                    ret = bool.IsTrueStringIgnoreCase(switchValue) || switchValue.Equals("1");
                }
            }

            return ret;
        }
    }
}
