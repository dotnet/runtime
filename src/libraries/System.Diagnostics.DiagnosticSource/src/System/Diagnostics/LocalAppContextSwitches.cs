// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System
{
    internal static partial class LocalAppContextSwitches
    {
        public static bool DefaultActivityIdFormatIsHierarchial { get; } = InitializeDefaultActivityIdFormat();

        private static bool InitializeDefaultActivityIdFormat()
        {
            bool defaultActivityIdFormatIsHierarchial = false;

            if (!LocalAppContextSwitches.GetSwitchValue("System.Diagnostics.DefaultActivityIdFormatIsHierarchial", ref defaultActivityIdFormatIsHierarchial))
            {
                string? switchValue = Environment.GetEnvironmentVariable("DOTNET_SYSTEM_DIAGNOSTICS_DEFAULTACTIVITYIDFORMATISHIERARCHIAL");
                if (switchValue != null)
                {
                    defaultActivityIdFormatIsHierarchial = IsTrueStringIgnoreCase(switchValue) || switchValue.Equals("1");
                }
            }

            return defaultActivityIdFormatIsHierarchial;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsTrueStringIgnoreCase(string value)
        {
            return value.Length == 4 &&
                    (value[0] == 't' || value[0] == 'T') &&
                    (value[1] == 'r' || value[1] == 'R') &&
                    (value[2] == 'u' || value[2] == 'U') &&
                    (value[3] == 'e' || value[3] == 'E');
        }
    }
}
