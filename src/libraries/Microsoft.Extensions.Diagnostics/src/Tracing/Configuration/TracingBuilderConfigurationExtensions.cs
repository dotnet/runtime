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
        /// <para>Tracing has two key levels: <see cref="ActivitySource.Name"/> and <see cref="Activity.OperationName"/>.</para>
        /// <list type="bullet">
        ///   <item><description>Section names: <c>EnabledTracing</c> (both global and local), <c>EnabledGlobalTracing</c>, and <c>EnabledLocalTracing</c>, plus the listener-specific forms <c>{ListenerName}:...</c>.</description></item>
        ///   <item><description>Within each section, supported entries are <c>Default</c>, <c>{SourceName}</c>, <c>{SourceName}:Default</c>, and <c>{SourceName}:{OperationName}</c>. <c>Default</c> at either level is a synonym for the level above (a source-level rule when nested under a source, a global rule at the top).</description></item>
        ///   <item><description>Listener-specific rules are evaluated together with root-level rules. When both match, the most specific rule is chosen; listener-specific rules are more specific than root-level defaults.</description></item>
        ///   <item><description>Values are Boolean only: <c>true</c> enables and <c>false</c> disables.</description></item>
        /// </list>
        /// <para>Example keys: <c>EnabledTracing:Default=true</c>, <c>EnabledGlobalTracing:MyCompany.Service=false</c>, <c>EnabledTracing:MyCompany.Service:Checkout=true</c>, and <c>MyListener:EnabledLocalTracing:MyCompany.Service=true</c>.</para>
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
            builder.Services.AddSingleton(new TracingConfiguration(configuration));
            return builder;
        }
    }
}
