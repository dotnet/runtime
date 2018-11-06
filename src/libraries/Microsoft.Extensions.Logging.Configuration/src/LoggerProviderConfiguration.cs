// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Logging.Configuration
{
    internal class LoggerProviderConfiguration<T> : ILoggerProviderConfiguration<T>
    {
        public LoggerProviderConfiguration(ILoggerProviderConfigurationFactory providerConfigurationFactory)
        {
            Configuration = providerConfigurationFactory.GetConfiguration(typeof(T));
        }

        public IConfiguration Configuration { get; }
    }
}