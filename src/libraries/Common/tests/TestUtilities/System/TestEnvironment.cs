// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        public static void ClearGlobalizationEnvironmentVars(System.Collections.IDictionary environment)
        {
            var keysToRemove = new List<string>();
            foreach (System.Collections.DictionaryEntry entry in environment)
            {
                string key = entry.Key as string;
                if (key == "LANG" || (key != null && key.StartsWith("DOTNET_SYSTEM_GLOBALIZATION", StringComparison.OrdinalIgnoreCase)))
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
