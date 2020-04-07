// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Globalization
{
    internal static partial class GlobalizationMode
    {
        internal static bool Invariant { get; } = GetGlobalizationInvariantMode();

        private static bool GetInvariantSwitchValue()
        {
            string? val = Environment.GetEnvironmentVariable("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT");
            if (val != null)
                return bool.IsTrueStringIgnoreCase(val) || val.Equals("1");
            return false;
        }
    }
}
