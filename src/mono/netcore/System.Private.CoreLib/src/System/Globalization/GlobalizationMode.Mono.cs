// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace System.Globalization
{
    internal static partial class GlobalizationMode
    {
        private static bool GetSwitchValue(string switchName, string envVariable)
        {
            bool ret = false;
            string? switchValue = Environment.GetEnvironmentVariable(envVariable);
            if (switchValue != null)
            {
                ret = bool.IsTrueStringIgnoreCase(switchValue) || switchValue.Equals("1");
            }
            return ret;
        }

        private static bool TryGetStringValue(string switchName, string envVariable, [NotNullWhen(true)] out string? value)
        {
            value = Environment.GetEnvironmentVariable(envVariable);
            return !string.IsNullOrEmpty(value);
        }
    }
}
