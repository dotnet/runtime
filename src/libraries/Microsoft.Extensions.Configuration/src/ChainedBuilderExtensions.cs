// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Extension methods for adding <see cref="IConfiguration"/> to an <see cref="IConfigurationBuilder"/>.
    /// </summary>
    public static class ChainedBuilderExtensions
    {
        /// <summary>
        /// Adds an existing configuration to <paramref name="configurationBuilder"/>.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="config">The <see cref="IConfiguration"/> to add.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddConfiguration(this IConfigurationBuilder configurationBuilder, IConfiguration config)
            => AddConfiguration(configurationBuilder, config, shouldDisposeConfiguration: false);

        /// <summary>
        /// Adds an existing configuration to <paramref name="configurationBuilder"/>.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="config">The <see cref="IConfiguration"/> to add.</param>
        /// <param name="shouldDisposeConfiguration">Whether the configuration should get disposed when the configuration provider is disposed.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddConfiguration(this IConfigurationBuilder configurationBuilder, IConfiguration config, bool shouldDisposeConfiguration)
        {
            ThrowHelper.ThrowIfNull(configurationBuilder);
            ThrowHelper.ThrowIfNull(config);

            configurationBuilder.Add(new ChainedConfigurationSource
            {
                Configuration = config,
                ShouldDisposeConfiguration = shouldDisposeConfiguration,
            });
            return configurationBuilder;
        }
    }
}
