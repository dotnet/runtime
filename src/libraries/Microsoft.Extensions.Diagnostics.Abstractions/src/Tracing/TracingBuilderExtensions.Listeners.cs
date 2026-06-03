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
        /// Registers a new <see cref="ActivityListener"/> using the supplied factory to materialise the instance from the service provider.
        /// </summary>
        /// <param name="builder">The <see cref="ITracingBuilder"/>.</param>
        /// <param name="factory">A factory function that produces the listener instance.</param>
        /// <returns>Returns the original <see cref="ITracingBuilder"/> for chaining.</returns>
        public static ITracingBuilder AddListener(this ITracingBuilder builder, Func<IServiceProvider, ActivityListener> factory)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(factory);
            builder.Services.AddSingleton<ActivityListener>(factory);
            return builder;
        }

        /// <summary>
        /// Registers a new <see cref="ActivityListener"/> instance.
        /// </summary>
        /// <param name="listener">The listener instance.</param>
        /// <param name="builder">The <see cref="ITracingBuilder"/>.</param>
        /// <returns>Returns the original <see cref="ITracingBuilder"/> for chaining.</returns>
        public static ITracingBuilder AddListener(this ITracingBuilder builder, ActivityListener listener)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(listener);
            builder.Services.AddSingleton<ActivityListener>(listener);
            return builder;
        }

        /// <summary>
        /// Removes all <see cref="ActivityListener"/> registrations from the dependency injection container.
        /// </summary>
        /// <param name="builder">The <see cref="ITracingBuilder"/>.</param>
        /// <returns>Returns the original <see cref="ITracingBuilder"/> for chaining.</returns>
        public static ITracingBuilder ClearListeners(this ITracingBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            builder.Services.RemoveAll<ActivityListener>();
            return builder;
        }
    }
}
