// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Diagnostics.Tracing
{
    /// <summary>
    /// Extension methods for <see cref="ITracingBuilder"/> to configure tracing rules.
    /// </summary>
    public static partial class TracingBuilderExtensions
    {
        /// <summary>
        /// Enables all activities for the given source, operation, listener, and scopes.
        /// </summary>
        /// <param name="builder">The <see cref="ITracingBuilder"/>.</param>
        /// <param name="sourceName">The <see cref="ActivitySource.Name"/> or prefix. A null value matches all activity sources.</param>
        /// <param name="operationName">The <see cref="Activity.OperationName"/>, exact match. A null or empty value matches all activities within the matching sources.</param>
        /// <param name="listenerName">The <see cref="ActivityListener.Name"/>. A null or empty value matches all listeners.</param>
        /// <param name="scopes">A bitwise combination of the enumeration values that specifies the scopes to consider. Defaults to all scopes.</param>
        /// <returns>The original <see cref="ITracingBuilder"/> for chaining.</returns>
        public static ITracingBuilder EnableTracing(this ITracingBuilder builder, string? sourceName = null, string? operationName = null, string? listenerName = null, ActivitySourceScopes scopes = ActivitySourceScopes.Global | ActivitySourceScopes.Local)
            => builder.ConfigureRule(options => options.EnableTracing(sourceName, operationName, listenerName, scopes));

        /// <summary>
        /// Enables all activities for the given source, operation, listener, and scopes.
        /// </summary>
        /// <param name="options">The <see cref="TracingOptions"/>.</param>
        /// <param name="sourceName">The <see cref="ActivitySource.Name"/> or prefix. A null value matches all activity sources.</param>
        /// <param name="operationName">The <see cref="Activity.OperationName"/>, exact match. A null or empty value matches all activities within the matching sources.</param>
        /// <param name="listenerName">The <see cref="ActivityListener.Name"/>. A null or empty value matches all listeners.</param>
        /// <param name="scopes">A bitwise combination of the enumeration values that specifies the scopes to consider. Defaults to all scopes.</param>
        /// <returns>The original <see cref="TracingOptions"/> for chaining.</returns>
        public static TracingOptions EnableTracing(this TracingOptions options, string? sourceName = null, string? operationName = null, string? listenerName = null, ActivitySourceScopes scopes = ActivitySourceScopes.Global | ActivitySourceScopes.Local)
            => options.AddRule(sourceName, operationName, listenerName, scopes, enable: true);

        /// <summary>
        /// Disables all activities for the given source, operation, listener, and scopes.
        /// </summary>
        /// <param name="builder">The <see cref="ITracingBuilder"/>.</param>
        /// <param name="sourceName">The <see cref="ActivitySource.Name"/> or prefix. A null value matches all activity sources.</param>
        /// <param name="operationName">The <see cref="Activity.OperationName"/>, exact match. A null or empty value matches all activities within the matching sources.</param>
        /// <param name="listenerName">The <see cref="ActivityListener.Name"/>. A null or empty value matches all listeners.</param>
        /// <param name="scopes">A bitwise combination of the enumeration values that specifies the scopes to consider. Defaults to all scopes.</param>
        /// <returns>The original <see cref="ITracingBuilder"/> for chaining.</returns>
        public static ITracingBuilder DisableTracing(this ITracingBuilder builder, string? sourceName = null, string? operationName = null, string? listenerName = null, ActivitySourceScopes scopes = ActivitySourceScopes.Global | ActivitySourceScopes.Local)
            => builder.ConfigureRule(options => options.DisableTracing(sourceName, operationName, listenerName, scopes));

        /// <summary>
        /// Disables all activities for the given source, operation, listener, and scopes.
        /// </summary>
        /// <param name="options">The <see cref="TracingOptions"/>.</param>
        /// <param name="sourceName">The <see cref="ActivitySource.Name"/> or prefix. A null value matches all activity sources.</param>
        /// <param name="operationName">The <see cref="Activity.OperationName"/>, exact match. A null or empty value matches all activities within the matching sources.</param>
        /// <param name="listenerName">The <see cref="ActivityListener.Name"/>. A null or empty value matches all listeners.</param>
        /// <param name="scopes">A bitwise combination of the enumeration values that specifies the scopes to consider. Defaults to all scopes.</param>
        /// <returns>The original <see cref="TracingOptions"/> for chaining.</returns>
        public static TracingOptions DisableTracing(this TracingOptions options, string? sourceName = null, string? operationName = null, string? listenerName = null, ActivitySourceScopes scopes = ActivitySourceScopes.Global | ActivitySourceScopes.Local)
            => options.AddRule(sourceName, operationName, listenerName, scopes, enable: false);

        private static ITracingBuilder ConfigureRule(this ITracingBuilder builder, Action<TracingOptions> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(builder);
            builder.Services.Configure(configureOptions);
            return builder;
        }

        private static TracingOptions AddRule(this TracingOptions options, string? sourceName, string? operationName, string? listenerName, ActivitySourceScopes scopes, bool enable)
        {
            ArgumentNullException.ThrowIfNull(options);
            options.Rules.Add(new TracingRule(sourceName, operationName, listenerName, scopes, enable));
            return options;
        }
    }
}
