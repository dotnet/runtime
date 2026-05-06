// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Configuration
{
    // Represents which user.config files are included in the configuration.
    public enum ConfigurationUserLevel
    {
        None = 0,
        PerUserRoaming = 10,
        PerUserRoamingAndLocal = 20,
    }
}
