// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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