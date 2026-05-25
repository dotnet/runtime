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
        /// Enables all activities for the given source, activity, listener, and scopes.
        /// </summary>
        /// <param name="builder">The <see cref="ITracingBuilder"/>.</param>
        /// <param name="activitySourceName">The <see cref="ActivitySource.Name"/> or prefix. A null value matches all activity sources.</param>
        /// <param name="activityName">The <see cref="Activity.OperationName"/>, exact match. A null or empty value matches all activities within the matching sources.</param>
        /// <param name="listenerName">The <see cref="IActivityListener.Name"/>. A null or empty value matches all listeners.</param>
        /// <param name="scopes">A bitwise combination of the enumeration values that specifies the scopes to consider. Defaults to all scopes.</param>
        /// <returns>The original <see cref="ITracingBuilder"/> for chaining.</returns>
        public static ITracingBuilder EnableTracing(this ITracingBuilder builder, string? activitySourceName = null, string? activityName = null, string? listenerName = null, ActivitySourceScope scopes = ActivitySourceScope.Global | ActivitySourceScope.Local)
            => builder.ConfigureRule(options => options.EnableTracing(activitySourceName, activityName, listenerName, scopes));

        /// <summary>
        /// Enables all activities for the given source, activity, listener, and scopes.
        /// </summary>
        /// <param name="options">The <see cref="TracingOptions"/>.</param>
        /// <param name="activitySourceName">The <see cref="ActivitySource.Name"/> or prefix. A null value matches all activity sources.</param>
        /// <param name="activityName">The <see cref="Activity.OperationName"/>, exact match. A null or empty value matches all activities within the matching sources.</param>
        /// <param name="listenerName">The <see cref="IActivityListener.Name"/>. A null or empty value matches all listeners.</param>
        /// <param name="scopes">A bitwise combination of the enumeration values that specifies the scopes to consider. Defaults to all scopes.</param>
        /// <returns>The original <see cref="TracingOptions"/> for chaining.</returns>
        public static TracingOptions EnableTracing(this TracingOptions options, string? activitySourceName = null, string? activityName = null, string? listenerName = null, ActivitySourceScope scopes = ActivitySourceScope.Global | ActivitySourceScope.Local)
            => options.AddRule(activitySourceName, activityName, listenerName, scopes, enabled: true);

        /// <summary>
        /// Disables all activities for the given source, activity, listener, and scopes.
        /// </summary>
        /// <param name="builder">The <see cref="ITracingBuilder"/>.</param>
        /// <param name="activitySourceName">The <see cref="ActivitySource.Name"/> or prefix. A null value matches all activity sources.</param>
        /// <param name="activityName">The <see cref="Activity.OperationName"/>, exact match. A null or empty value matches all activities within the matching sources.</param>
        /// <param name="listenerName">The <see cref="IActivityListener.Name"/>. A null or empty value matches all listeners.</param>
        /// <param name="scopes">A bitwise combination of the enumeration values that specifies the scopes to consider. Defaults to all scopes.</param>
        /// <returns>The original <see cref="ITracingBuilder"/> for chaining.</returns>
        public static ITracingBuilder DisableTracing(this ITracingBuilder builder, string? activitySourceName = null, string? activityName = null, string? listenerName = null, ActivitySourceScope scopes = ActivitySourceScope.Global | ActivitySourceScope.Local)
            => builder.ConfigureRule(options => options.DisableTracing(activitySourceName, activityName, listenerName, scopes));

        /// <summary>
        /// Disables all activities for the given source, activity, listener, and scopes.
        /// </summary>
        /// <param name="options">The <see cref="TracingOptions"/>.</param>
        /// <param name="activitySourceName">The <see cref="ActivitySource.Name"/> or prefix. A null value matches all activity sources.</param>
        /// <param name="activityName">The <see cref="Activity.OperationName"/>, exact match. A null or empty value matches all activities within the matching sources.</param>
        /// <param name="listenerName">The <see cref="IActivityListener.Name"/>. A null or empty value matches all listeners.</param>
        /// <param name="scopes">A bitwise combination of the enumeration values that specifies the scopes to consider. Defaults to all scopes.</param>
        /// <returns>The original <see cref="TracingOptions"/> for chaining.</returns>
        public static TracingOptions DisableTracing(this TracingOptions options, string? activitySourceName = null, string? activityName = null, string? listenerName = null, ActivitySourceScope scopes = ActivitySourceScope.Global | ActivitySourceScope.Local)
            => options.AddRule(activitySourceName, activityName, listenerName, scopes, enabled: false);

        private static ITracingBuilder ConfigureRule(this ITracingBuilder builder, Action<TracingOptions> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(builder);
            builder.Services.Configure(configureOptions);
            return builder;
        }

        private static TracingOptions AddRule(this TracingOptions options, string? activitySourceName, string? activityName, string? listenerName, ActivitySourceScope scopes, bool enabled)
        {
            ArgumentNullException.ThrowIfNull(options);
            options.Rules.Add(new TracingRule(activitySourceName, activityName, listenerName, scopes, enabled));
            return options;
        }
    }
}
