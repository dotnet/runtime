// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Configuration
{
    public static class ProtectedConfiguration
    {
        public static ProtectedConfigurationProviderCollection Providers
        {
            [RequiresUnreferencedCode(ConfigurationManager.TrimWarning)]
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
            [RequiresUnreferencedCode(ConfigurationManager.TrimWarning)]
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
