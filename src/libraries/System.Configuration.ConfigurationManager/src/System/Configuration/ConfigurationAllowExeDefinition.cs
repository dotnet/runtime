// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Configuration
{
    public enum ConfigurationAllowExeDefinition
    {
        MachineOnly = 0,
        MachineToApplication = 100,
        MachineToRoamingUser = 200,
        MachineToLocalUser = 300
    }
}
