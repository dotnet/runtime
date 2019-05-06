// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// IConfigurationBuilder extension methods for the chaind configuration provider.
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
            if (configurationBuilder == null)
            {
                throw new ArgumentNullException(nameof(configurationBuilder));
            }
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            configurationBuilder.Add(new ChainedConfigurationSource
            {
                Configuration = config,
                ShouldDisposeConfiguration = shouldDisposeConfiguration,
            });
            return configurationBuilder;
        }
    }
}
