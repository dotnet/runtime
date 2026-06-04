// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.Tracing
{
    /// <summary>
    /// Extensions for <see cref="ITracingBuilder"/> for enabling tracing based on <see cref="IConfiguration"/>.
    /// </summary>
    public static class TracingBuilderConfigurationExtensions
    {
        /// <summary>
        /// Reads tracing configuration from the provided <see cref="IConfiguration"/> section and configures
        /// which <see cref="ActivitySource"/> and <see cref="Activity"/> instances are enabled.
        /// </summary>
        /// <remarks>
        /// <para>The configuration key shapes follow the metrics model, except tracing stops at the <see cref="ActivitySource.Name"/> level and has no instrument-level child keys.</para>
        /// <para>- Section names: <c>EnabledTracing</c> (both global and local), <c>EnabledGlobalTracing</c>, and <c>EnabledLocalTracing</c>, plus the listener-specific forms <c>{ListenerName}:...</c>.</para>
        /// <para>- Within each section, supported entries are <c>Default</c> and <see cref="ActivitySource.Name"/>. Unlike metrics, tracing does not support a nested <c>{ActivitySourceName}:Default</c> form because there is no level below the activity source.</para>
        /// <para>- Listener-specific rules are evaluated together with root-level rules. When both match, the most specific rule is chosen; listener-specific rules are more specific than root-level defaults.</para>
        /// <para>- Values are Boolean only: <c>true</c> enables and <c>false</c> disables.</para>
        /// <para>Example keys: <c>EnabledTracing:Default=true</c>, <c>EnabledGlobalTracing:MyCompany.Service=false</c>, and <c>MyListener:EnabledLocalTracing:MyCompany.Service=true</c>.</para>
        /// </remarks>
        /// <param name="builder">The <see cref="ITracingBuilder"/>.</param>
        /// <param name="configuration">The <see cref="IConfiguration"/> section to load.</param>
        /// <returns>The original <see cref="ITracingBuilder"/> for chaining.</returns>
        public static ITracingBuilder AddConfiguration(this ITracingBuilder builder, IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(builder);

            ArgumentNullException.ThrowIfNull(configuration);

            builder.Services.AddSingleton<IConfigureOptions<TracingOptions>>(new TracingConfigureOptions(configuration));
            builder.Services.AddSingleton<IOptionsChangeTokenSource<TracingOptions>>(new ConfigurationChangeTokenSource<TracingOptions>(configuration));
            return builder;
        }
    }
}
