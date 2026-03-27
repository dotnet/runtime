// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.Diagnostics.Configuration
{
    /// <summary>
    /// Extension methods for <see cref="ITracingBuilder"/> to add or clear <see cref="IActivityListener"/> registrations.
    /// </summary>
    public static partial class ActivityBuilderExtensions
    {
        /// <summary>
        /// Registers a new <see cref="IActivityListener"/> of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The implementation type of the listener.</typeparam>
        /// <param name="builder">The <see cref="ITracingBuilder"/>.</param>
        /// <returns>Returns the original <see cref="ITracingBuilder"/> for chaining.</returns>
        public static ITracingBuilder AddListener<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this ITracingBuilder builder) where T : class, IActivityListener
        {
            ArgumentNullException.ThrowIfNull(builder);
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IActivityListener, T>());
            return builder;
        }

        /// <summary>
        /// Registers a new <see cref="IActivityListener"/> instance.
        /// </summary>
        /// <param name="listener">The listener instance.</param>
        /// <param name="builder">The <see cref="ITracingBuilder"/>.</param>
        /// <returns>Returns the original <see cref="ITracingBuilder"/> for chaining.</returns>
        public static ITracingBuilder AddListener(this ITracingBuilder builder, IActivityListener listener)
        {
            ArgumentNullException.ThrowIfNull(builder);
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton(listener));
            return builder;
        }

        /// <summary>
        /// Removes all <see cref="IActivityListener"/> registrations from the dependency injection container.
        /// </summary>
        /// <param name="builder">The <see cref="ITracingBuilder"/>.</param>
        /// <returns>Returns the original <see cref="ITracingBuilder"/> for chaining.</returns>
        public static ITracingBuilder ClearListeners(this ITracingBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            builder.Services.RemoveAll<IActivityListener>();
            return builder;
        }
    }
}
