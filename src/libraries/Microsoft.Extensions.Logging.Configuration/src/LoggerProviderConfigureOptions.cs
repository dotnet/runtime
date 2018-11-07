// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging.Configuration
{
    /// <summary>
    /// Loads settings for <typeparamref name="TProvider"/> into <typeparamref name="TOptions"/> type.
    /// </summary>
    internal class LoggerProviderConfigureOptions<TOptions, TProvider> : ConfigureFromConfigurationOptions<TOptions> where TOptions : class
    {
        public LoggerProviderConfigureOptions(ILoggerProviderConfiguration<TProvider> providerConfiguration)
            : base(providerConfiguration.Configuration)
        {
        }
    }
}