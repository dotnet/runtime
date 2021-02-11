// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Logging.Configuration
{
    /// <summary>
    /// Allows access to configuration section associated with logger provider
    /// </summary>
    /// <typeparam name="T">Type of logger provider to get configuration for</typeparam>
    public interface ILoggerProviderConfiguration<T>
    {
        /// <summary>
        /// Configuration section for requested logger provider
        /// </summary>
        IConfiguration Configuration { get; }
    }
}
