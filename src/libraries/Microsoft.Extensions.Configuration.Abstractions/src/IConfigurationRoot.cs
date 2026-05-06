// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Represents the root of an <see cref="IConfiguration"/> hierarchy.
    /// </summary>
    public interface IConfigurationRoot : IConfiguration
    {
        /// <summary>
        /// Forces the configuration values to be reloaded from the underlying <see cref="IConfigurationProvider"/> providers.
        /// </summary>
        void Reload();

        /// <summary>
        /// Gets the <see cref="IConfigurationProvider"/> providers for this configuration.
        /// </summary>
        IEnumerable<IConfigurationProvider> Providers { get; }
    }
}
