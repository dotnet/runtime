// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Diagnostics.Configuration
{
    /// <summary>
    /// Extension methods for <see cref="ITracingBuilder"/> to configure tracing rules.
    /// </summary>
    public static partial class TracingBuilderExtensions
    {
        /// <summary>
        /// Sets whether all activities are enabled for the given activity source and all registered listeners.
        /// </summary>
        /// <param name="builder">The <see cref="ITracingBuilder"/>.</param>
        /// <param name="activitySourceName">The <see cref="ActivitySource.Name"/> or prefix. A null value matches all activity sources.</param>
        /// <param name="enabled"><see langword="true"/> to enable matched activities; otherwise, <see langword="false"/>.</param>
        /// <returns>The original <see cref="ITracingBuilder"/> for chaining.</returns>
        public static ITracingBuilder SetEnabled(this ITracingBuilder builder, string? activitySourceName, bool enabled)
            => builder.ConfigureRule(options => options.SetEnabled(activitySourceName, enabled));

        /// <summary>
        /// Sets whether all activities are enabled for the given source and listener.
        /// </summary>
        /// <param name="builder">The <see cref="ITracingBuilder"/>.</param>
        /// <param name="activitySourceName">The <see cref="ActivitySource.Name"/> or prefix. A null value matches all activity sources.</param>
        /// <param name="listenerName">The <see cref="IActivityListener"/>.Name. A null value matches all listeners.</param>
        /// <param name="enabled"><see langword="true"/> to enable matched activities; otherwise, <see langword="false"/>.</param>
        /// <returns>The original <see cref="ITracingBuilder"/> for chaining.</returns>
        public static ITracingBuilder SetEnabled(this ITracingBuilder builder, string? activitySourceName, string? listenerName, bool enabled)
            => builder.SetEnabled(activitySourceName, listenerName, ActivitySourceScope.Global | ActivitySourceScope.Local, enabled);

        /// <summary>
        /// Sets whether all activities are enabled for the given source, listener, and scopes.
        /// </summary>
        /// <param name="builder">The <see cref="ITracingBuilder"/>.</param>
        /// <param name="activitySourceName">The <see cref="ActivitySource.Name"/> or prefix. A null value matches all activity sources.</param>
        /// <param name="listenerName">The <see cref="IActivityListener"/>.Name. A null value matches all listeners.</param>
        /// <param name="scopes">A bitwise combination of the enumeration values that specifies the scopes to consider.</param>
        /// <param name="enabled"><see langword="true"/> to enable matched activities; otherwise, <see langword="false"/>.</param>
        /// <returns>The original <see cref="ITracingBuilder"/> for chaining.</returns>
        public static ITracingBuilder SetEnabled(this ITracingBuilder builder, string? activitySourceName, string? listenerName, ActivitySourceScope scopes, bool enabled)
            => builder.ConfigureRule(options => options.SetEnabled(activitySourceName, listenerName, scopes, enabled));

        /// <summary>
        /// Sets whether all activities are enabled for the given activity source and all listeners.
        /// </summary>
        /// <param name="options">The <see cref="TracingOptions"/>.</param>
        /// <param name="activitySourceName">The <see cref="ActivitySource.Name"/> or prefix. A null value matches all activity sources.</param>
        /// <param name="enabled"><see langword="true"/> to enable matched activities; otherwise, <see langword="false"/>.</param>
        /// <returns>The original <see cref="TracingOptions"/> for chaining.</returns>
        public static TracingOptions SetEnabled(this TracingOptions options, string? activitySourceName, bool enabled)
            => options.SetEnabled(activitySourceName, listenerName: null, enabled);

        /// <summary>
        /// Sets whether all activities are enabled for the given source and listener.
        /// </summary>
        /// <param name="options">The <see cref="TracingOptions"/>.</param>
        /// <param name="activitySourceName">The <see cref="ActivitySource.Name"/> or prefix. A null value matches all activity sources.</param>
        /// <param name="listenerName">The <see cref="IActivityListener"/>.Name. A null value matches all listeners.</param>
        /// <param name="enabled"><see langword="true"/> to enable matched activities; otherwise, <see langword="false"/>.</param>
        /// <returns>The original <see cref="TracingOptions"/> for chaining.</returns>
        public static TracingOptions SetEnabled(this TracingOptions options, string? activitySourceName, string? listenerName, bool enabled)
            => options.SetEnabled(activitySourceName, listenerName, ActivitySourceScope.Global | ActivitySourceScope.Local, enabled);

        /// <summary>
        /// Sets whether all activities are enabled for the given source, listener, and scopes.
        /// </summary>
        /// <param name="options">The <see cref="TracingOptions"/>.</param>
        /// <param name="activitySourceName">The <see cref="ActivitySource.Name"/> or prefix. A null value matches all activity sources.</param>
        /// <param name="listenerName">The <see cref="IActivityListener"/>.Name. A null value matches all listeners.</param>
        /// <param name="scopes">A bitwise combination of the enumeration values that specifies the scopes to consider.</param>
        /// <param name="enabled"><see langword="true"/> to enable matched activities; otherwise, <see langword="false"/>.</param>
        /// <returns>The original <see cref="TracingOptions"/> for chaining.</returns>
        public static TracingOptions SetEnabled(this TracingOptions options, string? activitySourceName, string? listenerName, ActivitySourceScope scopes, bool enabled)
            => options.AddRule(activitySourceName, listenerName, scopes, enabled);

        /// <summary>
        /// Enables all activities for the given activity source and all registered listeners.
        /// </summary>
        /// <param name="builder">The <see cref="ITracingBuilder"/>.</param>
        /// <param name="activitySourceName">The <see cref="ActivitySource.Name"/> or prefix. A null value matches all activity sources.</param>
        /// <returns>The original <see cref="ITracingBuilder"/> for chaining.</returns>
        public static ITracingBuilder Enable(this ITracingBuilder builder, string? activitySourceName)
            => builder.SetEnabled(activitySourceName, enabled: true);

        /// <summary>
        /// Enables all activities for the given source and listener.
        /// </summary>
        /// <param name="builder">The <see cref="ITracingBuilder"/>.</param>
        /// <param name="activitySourceName">The <see cref="ActivitySource.Name"/> or prefix. A null value matches all activity sources.</param>
        /// <param name="listenerName">The <see cref="IActivityListener"/>.Name. A null value matches all listeners.</param>
        /// <returns>The original <see cref="ITracingBuilder"/> for chaining.</returns>
        public static ITracingBuilder Enable(this ITracingBuilder builder, string? activitySourceName, string? listenerName = null)
            => builder.SetEnabled(activitySourceName, listenerName, enabled: true);

        /// <summary>
        /// Enables all activities for the given source, listener, and scopes.
        /// </summary>
        /// <param name="builder">The <see cref="ITracingBuilder"/>.</param>
        /// <param name="activitySourceName">The <see cref="ActivitySource.Name"/> or prefix. A null value matches all activity sources.</param>
        /// <param name="listenerName">The <see cref="IActivityListener"/>.Name. A null value matches all listeners.</param>
        /// <param name="scopes">A bitwise combination of the enumeration values that specifies the scopes to consider.</param>
        /// <returns>The original <see cref="ITracingBuilder"/> for chaining.</returns>
        public static ITracingBuilder Enable(this ITracingBuilder builder, string? activitySourceName, string? listenerName, ActivitySourceScope scopes)
            => builder.SetEnabled(activitySourceName, listenerName, scopes, enabled: true);

        /// <summary>
        /// Enables all activities for the given activity source and all listeners.
        /// </summary>
        /// <param name="options">The <see cref="TracingOptions"/>.</param>
        /// <param name="activitySourceName">The <see cref="ActivitySource.Name"/> or prefix. A null value matches all activity sources.</param>
        /// <returns>The original <see cref="TracingOptions"/> for chaining.</returns>
        public static TracingOptions Enable(this TracingOptions options, string? activitySourceName)
            => options.SetEnabled(activitySourceName, enabled: true);

        /// <summary>
        /// Enables all activities for the given source and listener.
        /// </summary>
        /// <param name="options">The <see cref="TracingOptions"/>.</param>
        /// <param name="activitySourceName">The <see cref="ActivitySource.Name"/> or prefix. A null value matches all activity sources.</param>
        /// <param name="listenerName">The <see cref="IActivityListener"/>.Name. A null value matches all listeners.</param>
        /// <returns>The original <see cref="TracingOptions"/> for chaining.</returns>
        public static TracingOptions Enable(this TracingOptions options, string? activitySourceName, string? listenerName = null)
            => options.SetEnabled(activitySourceName, listenerName, enabled: true);

        /// <summary>
        /// Enables all activities for the given source, listener, and scopes.
        /// </summary>
        /// <param name="options">The <see cref="TracingOptions"/>.</param>
        /// <param name="activitySourceName">The <see cref="ActivitySource.Name"/> or prefix. A null value matches all activity sources.</param>
        /// <param name="listenerName">The <see cref="IActivityListener"/>.Name. A null value matches all listeners.</param>
        /// <param name="scopes">A bitwise combination of the enumeration values that specifies the scopes to consider.</param>
        /// <returns>The original <see cref="TracingOptions"/> for chaining.</returns>
        public static TracingOptions Enable(this TracingOptions options, string? activitySourceName, string? listenerName, ActivitySourceScope scopes)
            => options.SetEnabled(activitySourceName, listenerName, scopes, enabled: true);

        /// <summary>
        /// Disables all activities for the given activity source and all registered listeners.
        /// </summary>
        /// <param name="builder">The <see cref="ITracingBuilder"/>.</param>
        /// <param name="activitySourceName">The <see cref="ActivitySource.Name"/> or prefix. A null value matches all activity sources.</param>
        /// <returns>The original <see cref="ITracingBuilder"/> for chaining.</returns>
        public static ITracingBuilder Disable(this ITracingBuilder builder, string? activitySourceName)
            => builder.SetEnabled(activitySourceName, enabled: false);

        /// <summary>
        /// Disables all activities for the given source and listener.
        /// </summary>
        /// <param name="builder">The <see cref="ITracingBuilder"/>.</param>
        /// <param name="activitySourceName">The <see cref="ActivitySource.Name"/> or prefix. A null value matches all activity sources.</param>
        /// <param name="listenerName">The <see cref="IActivityListener"/>.Name. A null value matches all listeners.</param>
        /// <returns>The original <see cref="ITracingBuilder"/> for chaining.</returns>
        public static ITracingBuilder Disable(this ITracingBuilder builder, string? activitySourceName, string? listenerName = null)
            => builder.SetEnabled(activitySourceName, listenerName, enabled: false);

        /// <summary>
        /// Disables all activities for the given source, listener, and scopes.
        /// </summary>
        /// <param name="builder">The <see cref="ITracingBuilder"/>.</param>
        /// <param name="activitySourceName">The <see cref="ActivitySource.Name"/> or prefix. A null value matches all activity sources.</param>
        /// <param name="listenerName">The <see cref="IActivityListener"/>.Name. A null value matches all listeners.</param>
        /// <param name="scopes">A bitwise combination of the enumeration values that specifies the scopes to consider.</param>
        /// <returns>The original <see cref="ITracingBuilder"/> for chaining.</returns>
        public static ITracingBuilder Disable(this ITracingBuilder builder, string? activitySourceName, string? listenerName, ActivitySourceScope scopes)
            => builder.SetEnabled(activitySourceName, listenerName, scopes, enabled: false);

        /// <summary>
        /// Disables all activities for the given activity source and all listeners.
        /// </summary>
        /// <param name="options">The <see cref="TracingOptions"/>.</param>
        /// <param name="activitySourceName">The <see cref="ActivitySource.Name"/> or prefix. A null value matches all activity sources.</param>
        /// <returns>The original <see cref="TracingOptions"/> for chaining.</returns>
        public static TracingOptions Disable(this TracingOptions options, string? activitySourceName)
            => options.SetEnabled(activitySourceName, enabled: false);

        /// <summary>
        /// Disables all activities for the given source and listener.
        /// </summary>
        /// <param name="options">The <see cref="TracingOptions"/>.</param>
        /// <param name="activitySourceName">The <see cref="ActivitySource.Name"/> or prefix. A null value matches all activity sources.</param>
        /// <param name="listenerName">The <see cref="IActivityListener"/>.Name. A null value matches all listeners.</param>
        /// <returns>The original <see cref="TracingOptions"/> for chaining.</returns>
        public static TracingOptions Disable(this TracingOptions options, string? activitySourceName, string? listenerName = null)
            => options.SetEnabled(activitySourceName, listenerName, enabled: false);

        /// <summary>
        /// Disables all activities for the given source, listener, and scopes.
        /// </summary>
        /// <param name="options">The <see cref="TracingOptions"/>.</param>
        /// <param name="activitySourceName">The <see cref="ActivitySource.Name"/> or prefix. A null value matches all activity sources.</param>
        /// <param name="listenerName">The <see cref="IActivityListener"/>.Name. A null value matches all listeners.</param>
        /// <param name="scopes">A bitwise combination of the enumeration values that specifies the scopes to consider.</param>
        /// <returns>The original <see cref="TracingOptions"/> for chaining.</returns>
        public static TracingOptions Disable(this TracingOptions options, string? activitySourceName, string? listenerName, ActivitySourceScope scopes)
            => options.SetEnabled(activitySourceName, listenerName, scopes, enabled: false);

        private static ITracingBuilder ConfigureRule(this ITracingBuilder builder, Action<TracingOptions> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(builder);
            builder.Services.Configure(configureOptions);
            return builder;
        }

        private static TracingOptions AddRule(this TracingOptions options, string? activitySourceName, string? listenerName, ActivitySourceScope scopes, bool enabled)
        {
            ArgumentNullException.ThrowIfNull(options);
            options.Rules.Add(new TracingRule(activitySourceName, listenerName, scopes, enabled));
            return options;
        }
    }
}
