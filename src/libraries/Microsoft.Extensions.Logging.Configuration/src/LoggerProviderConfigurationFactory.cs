// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Logging.Configuration
{
    internal class LoggerProviderConfigurationFactory : ILoggerProviderConfigurationFactory
    {
        private readonly IEnumerable<LoggingConfiguration> _configurations;

        public LoggerProviderConfigurationFactory(IEnumerable<LoggingConfiguration> configurations)
        {
            _configurations = configurations;
        }

        public IConfiguration GetConfiguration(Type providerType)
        {
            if (providerType == null)
            {
                throw new ArgumentNullException(nameof(providerType));
            }

            var fullName = providerType.FullName;
            var alias = ProviderAliasUtilities.GetAlias(providerType);
            var configurationBuilder = new ConfigurationBuilder();
            foreach (var configuration in _configurations)
            {
                var sectionFromFullName = configuration.Configuration.GetSection(fullName);
                configurationBuilder.AddConfiguration(sectionFromFullName);

                if (!string.IsNullOrWhiteSpace(alias))
                {
                    var sectionFromAlias = configuration.Configuration.GetSection(alias);
                    configurationBuilder.AddConfiguration(sectionFromAlias);
                }
            }
            return configurationBuilder.Build();
        }
    }
}
