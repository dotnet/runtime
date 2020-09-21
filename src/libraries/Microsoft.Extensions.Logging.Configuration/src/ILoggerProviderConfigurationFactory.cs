// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Logging.Configuration
{
    /// <summary>
    /// Allows access to configuration section associated with logger provider
    /// </summary>
    public interface ILoggerProviderConfigurationFactory
    {
        /// <summary>
        /// Return configuration section associated with logger provider
        /// </summary>
        /// <param name="providerType">The logger provider type</param>
        /// <returns>The <see cref="IConfiguration"/> for the given <paramref name="providerType" />.</returns>
        IConfiguration GetConfiguration(Type providerType);
    }
}
