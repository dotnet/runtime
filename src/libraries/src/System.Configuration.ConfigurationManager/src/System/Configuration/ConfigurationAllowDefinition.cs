// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    public enum ConfigurationAllowDefinition
    {
        MachineOnly = 0,
        MachineToWebRoot = 100,
        MachineToApplication = 200,
        Everywhere = 300,
    }
}