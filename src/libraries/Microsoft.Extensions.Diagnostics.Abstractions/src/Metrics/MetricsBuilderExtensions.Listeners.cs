// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    /// <summary>
    /// Extension methods for <see cref="IMetricsBuilder"/> to add or clear <see cref="IMetricsListener"/> registrations, and to enable or disable metrics.
    /// </summary>
    public static partial class MetricsBuilderExtensions
    {
        /// <summary>
        /// Registers a new <see cref="IMetricsListener"/> of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The implementation type of the listener.</typeparam>
        /// <param name="builder">The <see cref="IMetricsBuilder"/>.</param>
        /// <returns>Returns the original <see cref="IMetricsBuilder"/> for chaining.</returns>
        public static IMetricsBuilder AddListener<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this IMetricsBuilder builder) where T : class, IMetricsListener
        {
            ThrowHelper.ThrowIfNull(builder);
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IMetricsListener, T>());
            return builder;
        }

        /// <summary>
        /// Registers a new <see cref="IMetricsListener"/> instance.
        /// </summary>
        /// <param name="listener">The implementation type of the listener.</param>
        /// <param name="builder">The <see cref="IMetricsBuilder"/>.</param>
        /// <returns>Returns the original <see cref="IMetricsBuilder"/> for chaining.</returns>
        public static IMetricsBuilder AddListener(this IMetricsBuilder builder, IMetricsListener listener)
        {
            ThrowHelper.ThrowIfNull(builder);
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton(listener));
            return builder;
        }

        /// <summary>
        /// Removes all <see cref="IMetricsListener"/> registrations from the dependency injection container.
        /// </summary>
        /// <param name="builder">The <see cref="IMetricsBuilder"/>.</param>
        /// <returns>Returns the original <see cref="IMetricsBuilder"/> for chaining.</returns>
        public static IMetricsBuilder ClearListeners(this IMetricsBuilder builder)
        {
            ThrowHelper.ThrowIfNull(builder);
            builder.Services.RemoveAll<IMetricsListener>();
            return builder;
        }
    }
}
