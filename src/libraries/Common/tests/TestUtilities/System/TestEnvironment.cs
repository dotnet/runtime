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
    }
}
