// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Globalization
{
    internal static partial class GlobalizationMode
    {
        private static bool GetInvariantSwitchValue() =>
            GetSwitchValue("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT");

        private static bool GetSwitchValue(string envVariable)
        {
            bool ret = false;
            string? switchValue = Environment.GetEnvironmentVariable(envVariable);
            if (switchValue != null)
            {
                ret = bool.IsTrueStringIgnoreCase(switchValue) || switchValue.Equals("1");
            }
            return ret;
        }
    }
}
