// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    public static class ProtectedConfiguration
    {
        public static ProtectedConfigurationProviderCollection Providers
        {
            get
            {
                ProtectedConfigurationSection config =
                    PrivilegedConfigurationManager.GetSection(
                            BaseConfigurationRecord.ReservedSectionProtectedConfiguration) as
                        ProtectedConfigurationSection;
                return config == null ? new ProtectedConfigurationProviderCollection() : config.GetAllProviders();
            }
        }

        public const string RsaProviderName = "RsaProtectedConfigurationProvider";
        public const string DataProtectionProviderName = "DataProtectionConfigurationProvider";
        public const string ProtectedDataSectionName = BaseConfigurationRecord.ReservedSectionProtectedConfiguration;

        public static string DefaultProvider
        {
            get
            {
                ProtectedConfigurationSection config =
                    PrivilegedConfigurationManager.GetSection(
                            BaseConfigurationRecord.ReservedSectionProtectedConfiguration) as
                        ProtectedConfigurationSection;
                return config != null ? config.DefaultProvider : "";
            }
        }
    }
}