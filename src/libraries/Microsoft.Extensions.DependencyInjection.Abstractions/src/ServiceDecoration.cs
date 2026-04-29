// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Describes a service decoration — a transformation applied to an existing service registration.
    /// </summary>
    public sealed class ServiceDecoration
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ServiceDecoration"/> with the specified
        /// <paramref name="serviceType"/> and <paramref name="decoratorType"/>.
        /// </summary>
        /// <param name="serviceType">The type of the service to decorate.</param>
        /// <param name="decoratorType">The type of the decorator. May be an open generic type definition.</param>
        public ServiceDecoration(
            Type serviceType,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type decoratorType)
            : this(serviceType, serviceKey: null, decoratorType)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ServiceDecoration"/> with the specified
        /// <paramref name="serviceType"/>, <paramref name="serviceKey"/>, and <paramref name="decoratorType"/>.
        /// </summary>
        /// <param name="serviceType">The type of the service to decorate.</param>
        /// <param name="serviceKey">The key of the service to decorate, or <see langword="null"/> for non-keyed services.</param>
        /// <param name="decoratorType">The type of the decorator. May be an open generic type definition.</param>
        public ServiceDecoration(
            Type serviceType,
            object? serviceKey,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type decoratorType)
        {
            ArgumentNullException.ThrowIfNull(serviceType);
            ArgumentNullException.ThrowIfNull(decoratorType);

            ServiceType = serviceType;
            ServiceKey = serviceKey;
            DecoratorType = decoratorType;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ServiceDecoration"/> with the specified
        /// <paramref name="serviceType"/> and <paramref name="decoratorFactory"/>.
        /// </summary>
        /// <param name="serviceType">The type of the service to decorate.</param>
        /// <param name="decoratorFactory">A factory that creates the decorator, given the service provider and the inner service instance.</param>
        public ServiceDecoration(
            Type serviceType,
            Func<IServiceProvider, object, object> decoratorFactory)
            : this(serviceType, serviceKey: null, decoratorFactory)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ServiceDecoration"/> with the specified
        /// <paramref name="serviceType"/>, <paramref name="serviceKey"/>, and <paramref name="decoratorFactory"/>.
        /// </summary>
        /// <param name="serviceType">The type of the service to decorate.</param>
        /// <param name="serviceKey">The key of the service to decorate, or <see langword="null"/> for non-keyed services.</param>
        /// <param name="decoratorFactory">A factory that creates the decorator, given the service provider and the inner service instance.</param>
        public ServiceDecoration(
            Type serviceType,
            object? serviceKey,
            Func<IServiceProvider, object, object> decoratorFactory)
        {
            ArgumentNullException.ThrowIfNull(serviceType);
            ArgumentNullException.ThrowIfNull(decoratorFactory);

            ServiceType = serviceType;
            ServiceKey = serviceKey;
            DecoratorFactory = decoratorFactory;
        }

        /// <summary>
        /// Gets the type of the service to decorate.
        /// </summary>
        public Type ServiceType { get; }

        /// <summary>
        /// Gets the key of the service to decorate, or <see langword="null"/> for non-keyed services.
        /// </summary>
        public object? ServiceKey { get; }

        /// <summary>
        /// Gets the type of the decorator. May be an open generic type definition.
        /// <see langword="null"/> when using a factory-based decoration.
        /// </summary>
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        public Type? DecoratorType { get; }

        /// <summary>
        /// Gets the factory that creates the decorator.
        /// <see langword="null"/> when using a type-based decoration.
        /// </summary>
        public Func<IServiceProvider, object, object>? DecoratorFactory { get; }
    }
}
