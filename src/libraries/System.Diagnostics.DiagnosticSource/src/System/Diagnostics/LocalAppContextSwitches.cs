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
                    defaultActivityIdFormatIsHierarchial = switchValue.Equals("true", StringComparison.OrdinalIgnoreCase) || switchValue.Equals("1");
                }
            }

            return defaultActivityIdFormatIsHierarchial;
        }
    }
}
