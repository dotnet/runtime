// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration.Memory;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// IConfigurationBuilder extension methods for the MemoryConfigurationProvider.
    /// </summary>
    public static class MemoryConfigurationBuilderExtensions
    {
        /// <summary>
        /// Adds the memory configuration provider to <paramref name="configurationBuilder"/>.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddInMemoryCollection(this IConfigurationBuilder configurationBuilder)
        {
            ThrowHelper.ThrowIfNull(configurationBuilder);

            configurationBuilder.Add(new MemoryConfigurationSource());
            return configurationBuilder;
        }

        /// <summary>
        /// Adds the memory configuration provider to <paramref name="configurationBuilder"/>.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="initialData">The data to add to memory configuration provider.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddInMemoryCollection(
            this IConfigurationBuilder configurationBuilder,
            IEnumerable<KeyValuePair<string, string?>>? initialData)
        {
            ThrowHelper.ThrowIfNull(configurationBuilder);

            configurationBuilder.Add(new MemoryConfigurationSource { InitialData = initialData });
            return configurationBuilder;
        }
    }
}
