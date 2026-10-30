// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.Diagnostics.Tracing
{
    /// <summary>
    /// Extension methods for <see cref="ITracingBuilder"/> to add or clear <see cref="ActivityListener"/> registrations.
    /// </summary>
    public static partial class TracingBuilderExtensions
    {
        /// <summary>
        /// Registers a new <see cref="ActivityListener"/> identified by <paramref name="name"/> and described by
        /// the supplied <paramref name="configure"/> callback.
        /// </summary>
        /// <param name="builder">The <see cref="ITracingBuilder"/>.</param>
        /// <param name="name">A name used by configuration-based filtering to identify this listener for rule matching.</param>
        /// <param name="configure">A callback that configures the delegate properties of the supplied <see cref="ActivityListenerBuilder"/>.</param>
        /// <returns>Returns the original <see cref="ITracingBuilder"/> for chaining.</returns>
        /// <remarks>
        /// The tracing infrastructure invokes <paramref name="configure"/> once when the underlying
        /// <see cref="ActivitySourceFactory"/> is first resolved, snapshots the delegate properties from the supplied
        /// <see cref="ActivityListenerBuilder"/>, and constructs the registered <see cref="ActivityListener"/> itself.
        /// Subscription to <see cref="ActivitySource"/> instances is driven entirely by the configuration-based
        /// <see cref="TracingRule"/> set; the builder re-evaluates listener subscriptions automatically when the bound
        /// <see cref="TracingOptions"/> change.
        /// </remarks>
        public static ITracingBuilder AddListener(this ITracingBuilder builder, string name, Action<ActivityListenerBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentException.ThrowIfNullOrEmpty(name);
            ArgumentNullException.ThrowIfNull(configure);

            builder.Services.AddSingleton(_ =>
            {
                ActivityListenerBuilder listenerBuilder = new ActivityListenerBuilder(name);
                configure(listenerBuilder);
                return listenerBuilder;
            });
            return builder;
        }

        /// <summary>
        /// Registers a new <see cref="ActivityListener"/> identified by <paramref name="name"/> and described by
        /// the supplied <paramref name="configure"/> callback, which also receives the resolved <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="builder">The <see cref="ITracingBuilder"/>.</param>
        /// <param name="name">A name used by configuration-based filtering to identify this listener for rule matching.</param>
        /// <param name="configure">A callback that configures the supplied <see cref="ActivityListenerBuilder"/>, with access to the resolved <see cref="IServiceProvider"/>.</param>
        /// <returns>Returns the original <see cref="ITracingBuilder"/> for chaining.</returns>
        /// <remarks>
        /// The tracing infrastructure invokes <paramref name="configure"/> once when the underlying
        /// <see cref="ActivitySourceFactory"/> is first resolved, snapshots the delegate properties from the supplied
        /// <see cref="ActivityListenerBuilder"/>, and constructs the registered <see cref="ActivityListener"/> itself.
        /// Subscription to <see cref="ActivitySource"/> instances is driven entirely by the configuration-based
        /// <see cref="TracingRule"/> set; the builder re-evaluates listener subscriptions automatically when the bound
        /// <see cref="TracingOptions"/> change.
        /// </remarks>
        public static ITracingBuilder AddListener(this ITracingBuilder builder, string name, Action<IServiceProvider, ActivityListenerBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentException.ThrowIfNullOrEmpty(name);
            ArgumentNullException.ThrowIfNull(configure);

            builder.Services.AddSingleton(serviceProvider =>
            {
                ActivityListenerBuilder listenerBuilder = new ActivityListenerBuilder(name);
                configure(serviceProvider, listenerBuilder);
                return listenerBuilder;
            });
            return builder;
        }

        /// <summary>
        /// Removes all <see cref="ActivityListenerBuilder"/> registrations from the dependency injection container.
        /// </summary>
        /// <param name="builder">The <see cref="ITracingBuilder"/>.</param>
        /// <returns>Returns the original <see cref="ITracingBuilder"/> for chaining.</returns>
        public static ITracingBuilder ClearListeners(this ITracingBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            builder.Services.RemoveAll<ActivityListenerBuilder>();
            return builder;
        }
    }
}
