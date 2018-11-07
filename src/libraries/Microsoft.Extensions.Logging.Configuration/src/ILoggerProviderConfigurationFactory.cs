// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
        IConfiguration GetConfiguration(Type providerType);
    }
}