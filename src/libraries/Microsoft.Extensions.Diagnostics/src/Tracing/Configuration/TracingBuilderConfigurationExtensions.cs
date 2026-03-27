// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.Configuration
{
    /// <summary>
    /// Extensions for <see cref="ITracingBuilder"/> for enabling tracing based on <see cref="IConfiguration"/>.
    /// </summary>
    public static class ActivityBuilderExtensions
    {
        /// <summary>
        /// Reads tracing configuration from the provided <see cref="IConfiguration"/> section and configures
        /// which <see cref="ActivitySource"/> and <see cref="Activity"/> instances are enabled.
        /// </summary>
        /// <param name="builder">The <see cref="ITracingBuilder"/>.</param>
        /// <param name="configuration">The <see cref="IConfiguration"/> section to load.</param>
        /// <returns>The original <see cref="ITracingBuilder"/> for chaining.</returns>
        public static ITracingBuilder AddConfiguration(this ITracingBuilder builder, IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(builder);

            ArgumentNullException.ThrowIfNull(configuration);

            builder.Services.AddSingleton<IConfigureOptions<TracingOptions>>(new TracingConfigureOptions(configuration));
            builder.Services.AddSingleton<IOptionsChangeTokenSource<TracingOptions>>(new ConfigurationChangeTokenSource<TracingOptions>(configuration));
            builder.Services.AddSingleton(new TracingConfiguration(configuration));
            return builder;
        }
    }
}
