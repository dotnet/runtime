// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration.Internal
{
    internal sealed class ConfigurationManagerInternal : IConfigurationManagerInternal
    {
        internal ConfigurationManagerInternal() { }

        bool IConfigurationManagerInternal.SupportsUserConfig => ConfigurationManager.SupportsUserConfig;

        bool IConfigurationManagerInternal.SetConfigurationSystemInProgress
            => ConfigurationManager.SetConfigurationSystemInProgress;

        string IConfigurationManagerInternal.MachineConfigPath => ClientConfigurationHost.MachineConfigFilePath;

        string IConfigurationManagerInternal.ApplicationConfigUri => ClientConfigPaths.Current.ApplicationConfigUri;

        string IConfigurationManagerInternal.ExeProductName => ClientConfigPaths.Current.ProductName;

        string IConfigurationManagerInternal.ExeProductVersion => ClientConfigPaths.Current.ProductVersion;

        string IConfigurationManagerInternal.ExeRoamingConfigDirectory
            => ClientConfigPaths.Current.RoamingConfigDirectory;

        string IConfigurationManagerInternal.ExeRoamingConfigPath => ClientConfigPaths.Current.RoamingConfigFilename;

        string IConfigurationManagerInternal.ExeLocalConfigDirectory => ClientConfigPaths.Current.LocalConfigDirectory;

        string IConfigurationManagerInternal.ExeLocalConfigPath => ClientConfigPaths.Current.LocalConfigFilename;

        string IConfigurationManagerInternal.UserConfigFilename => ClientConfigPaths.UserConfigFilename;
    }
}