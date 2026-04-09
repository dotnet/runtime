// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System
{
    public static class TestEnvironment
    {
        /// <summary>
        /// Check if the stress mode is enabled.
        /// </summary>
        /// <value> true if the environment variable DOTNET_TEST_STRESS set to '1' or 'true'. returns false otherwise</value>
        public static bool IsStressModeEnabled
        {
            get
            {
                string value = Environment.GetEnvironmentVariable("DOTNET_TEST_STRESS");
                return value != null && (value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Removes LANG and any environment variable starting with DOTNET_SYSTEM_GLOBALIZATION from the given environment dictionary.
        /// </summary>
        public static void ClearGlobalizationEnvironmentVars(IDictionary<string,string?> environment)
        {
            var keysToRemove = new List<string>();
            foreach (var key in environment.Keys)
            {
                if (key == "LANG" || key.StartsWith("DOTNET_SYSTEM_GLOBALIZATION", StringComparison.OrdinalIgnoreCase))
                {
                    keysToRemove.Add(key);
                }
            }
            foreach (var key in keysToRemove)
            {
                environment.Remove(key);
            }
        }
    }
}
