// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging.Configuration
{
    /// <inheritdoc />
    public class LoggerProviderOptionsChangeTokenSource<TOptions, TProvider> : ConfigurationChangeTokenSource<TOptions>
    {
        /// <inheritdoc />
        public LoggerProviderOptionsChangeTokenSource(ILoggerProviderConfiguration<TProvider> providerConfiguration) : base(providerConfiguration.Configuration)
        {
        }
    }
}