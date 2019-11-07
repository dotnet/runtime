// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Configuration.Internal
{
    // Exposes support in System.Configuration for functions that were
    // once available in System.dll

    public interface IConfigurationManagerInternal
    {
        bool SupportsUserConfig { get; }
        bool SetConfigurationSystemInProgress { get; }
        string MachineConfigPath { get; }
        string ApplicationConfigUri { get; }
        string ExeProductName { get; }
        string ExeProductVersion { get; }
        string ExeRoamingConfigDirectory { get; }
        string ExeRoamingConfigPath { get; }
        string ExeLocalConfigDirectory { get; }
        string ExeLocalConfigPath { get; }
        string UserConfigFilename { get; }
    }
}