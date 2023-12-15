// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Describes a service with its service type, implementation, and lifetime.
    /// </summary>
    [DebuggerDisplay("{DebuggerToString(),nq}")]
    public class ServiceDescriptor
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ServiceDescriptor"/> with the specified <paramref name="implementationType"/>.
        /// </summary>
        /// <param name="serviceType">The <see cref="Type"/> of the service.</param>
        /// <param name="implementationType">The <see cref="Type"/> implementing the service.</param>
        /// <param name="lifetime">The <see cref="ServiceLifetime"/> of the service.</param>
        public ServiceDescriptor(
            Type serviceType,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType,
            ServiceLifetime lifetime)
            : this(serviceType, null, implementationType, lifetime)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ServiceDescriptor"/> with the specified <paramref name="implementationType"/>.
        /// </summary>
        /// <param name="serviceType">The <see cref="Type"/> of the service.</param>
        /// <param name="serviceKey">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
        /// <param name="implementationType">The <see cref="Type"/> implementing the service.</param>
        /// <param name="lifetime">The <see cref="ServiceLifetime"/> of the service.</param>
        public ServiceDescriptor(
            Type serviceType,
            object? serviceKey,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType,
            ServiceLifetime lifetime)
            : this(serviceType, serviceKey, lifetime)
        {
            ThrowHelper.ThrowIfNull(serviceType);
            ThrowHelper.ThrowIfNull(implementationType);

            _implementationType = implementationType;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ServiceDescriptor"/> with the specified <paramref name="instance"/>
        /// as a <see cref="ServiceLifetime.Singleton"/>.
        /// </summary>
        /// <param name="serviceType">The <see cref="Type"/> of the service.</param>
        /// <param name="instance">The instance implementing the service.</param>
        public ServiceDescriptor(
            Type serviceType,
            object instance)
            : this(serviceType, null, instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ServiceDescriptor"/> with the specified <paramref name="instance"/>
        /// as a <see cref="ServiceLifetime.Singleton"/>.
        /// </summary>
        /// <param name="serviceType">The <see cref="Type"/> of the service.</param>
        /// <param name="serviceKey">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
        /// <param name="instance">The instance implementing the service.</param>
        public ServiceDescriptor(
            Type serviceType,
            object? serviceKey,
            object instance)
            : this(serviceType, serviceKey, ServiceLifetime.Singleton)
        {
            ThrowHelper.ThrowIfNull(serviceType);
            ThrowHelper.ThrowIfNull(instance);

            _implementationInstance = instance;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ServiceDescriptor"/> with the specified <paramref name="factory"/>.
        /// </summary>
        /// <param name="serviceType">The <see cref="Type"/> of the service.</param>
        /// <param name="factory">A factory used for creating service instances.</param>
        /// <param name="lifetime">The <see cref="ServiceLifetime"/> of the service.</param>
        public ServiceDescriptor(
            Type serviceType,
            Func<IServiceProvider, object> factory,
            ServiceLifetime lifetime)
            : this(serviceType, serviceKey: null, lifetime)
        {
            ThrowHelper.ThrowIfNull(serviceType);
            ThrowHelper.ThrowIfNull(factory);

            _implementationFactory = factory;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ServiceDescriptor"/> with the specified <paramref name="factory"/>.
        /// </summary>
        /// <param name="serviceType">The <see cref="Type"/> of the service.</param>
        /// <param name="serviceKey">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
        /// <param name="factory">A factory used for creating service instances.</param>
        /// <param name="lifetime">The <see cref="ServiceLifetime"/> of the service.</param>
        public ServiceDescriptor(
            Type serviceType,
            object? serviceKey,
            Func<IServiceProvider, object?, object> factory,
            ServiceLifetime lifetime)
            : this(serviceType, serviceKey, lifetime)
        {
            ThrowHelper.ThrowIfNull(serviceType);
            ThrowHelper.ThrowIfNull(factory);

            if (serviceKey is null)
            {
                // If the key is null, use the same factory signature as non-keyed descriptor
                Func<IServiceProvider, object> nullKeyedFactory = sp => factory(sp, null);
                _implementationFactory = nullKeyedFactory;
            }
            else
            {
                _implementationFactory = factory;
            }
        }

        private ServiceDescriptor(Type serviceType, object? serviceKey, ServiceLifetime lifetime)
        {
            Lifetime = lifetime;
            ServiceType = serviceType;
            ServiceKey = serviceKey;
        }

        /// <summary>
        /// Gets the <see cref="ServiceLifetime"/> of the service.
        /// </summary>
        public ServiceLifetime Lifetime { get; }

        /// <summary>
        /// Get the key of the service, if applicable.
        /// </summary>
        public object? ServiceKey { get; }

        /// <summary>
        /// Gets the <see cref="Type"/> of the service.
        /// </summary>
        public Type ServiceType { get; }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        private Type? _implementationType;

        /// <summary>
        /// Gets the <see cref="Type"/> that implements the service.
        /// </summary>
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        public Type? ImplementationType
        {
            get
            {
                if (IsKeyedService)
                {
                    ThrowKeyedDescriptor();
                }
                return _implementationType;
            }
        }

        /// <summary>
        /// Gets the <see cref="Type"/> that implements the service.
        /// </summary>
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        public Type? KeyedImplementationType
        {
            get
            {
                if (!IsKeyedService)
                {
                    ThrowNonKeyedDescriptor();
                }
                return _implementationType;
            }
        }

        private object? _implementationInstance;

        /// <summary>
        /// Gets the instance that implements the service.
        /// </summary>
        public object? ImplementationInstance
        {
            get
            {
                if (IsKeyedService)
                {
                    ThrowKeyedDescriptor();
                }
                return _implementationInstance;
            }
        }

        /// <summary>
        /// Gets the instance that implements the service.
        /// </summary>
        public object? KeyedImplementationInstance
        {
            get
            {
                if (!IsKeyedService)
                {
                    ThrowNonKeyedDescriptor();
                }
                return _implementationInstance;
            }
        }

        private object? _implementationFactory;

        /// <summary>
        /// Gets the factory used for creating service instances.
        /// </summary>
        public Func<IServiceProvider, object>? ImplementationFactory
        {
            get
            {
                if (IsKeyedService)
                {
                    ThrowKeyedDescriptor();
                }
                return (Func<IServiceProvider, object>?)_implementationFactory;
            }
        }

        /// <summary>
        /// Gets the factory used for creating Keyed service instances.
        /// </summary>
        public Func<IServiceProvider, object?, object>? KeyedImplementationFactory
        {
            get
            {
                if (!IsKeyedService)
                {
                    ThrowNonKeyedDescriptor();
                }
                return (Func<IServiceProvider, object?, object>?)_implementationFactory;
            }
        }

        /// <summary>
        /// Indicates whether the service is a keyed service.
        /// </summary>
        public bool IsKeyedService => ServiceKey != null;

        /// <inheritdoc />
        public override string ToString()
        {
            string? lifetime = $"{nameof(ServiceType)}: {ServiceType} {nameof(Lifetime)}: {Lifetime} ";

            if (IsKeyedService)
            {
                lifetime += $"{nameof(ServiceKey)}: {ServiceKey} ";

                if (KeyedImplementationType != null)
                {
                    return lifetime + $"{nameof(KeyedImplementationType)}: {KeyedImplementationType}";
                }

                if (KeyedImplementationFactory != null)
                {
                    return lifetime + $"{nameof(KeyedImplementationFactory)}: {KeyedImplementationFactory.Method}";
                }

                return lifetime + $"{nameof(KeyedImplementationInstance)}: {KeyedImplementationInstance}";
            }
            else
            {
                if (ImplementationType != null)
                {
                    return lifetime + $"{nameof(ImplementationType)}: {ImplementationType}";
                }

                if (ImplementationFactory != null)
                {
                    return lifetime + $"{nameof(ImplementationFactory)}: {ImplementationFactory.Method}";
                }

                return lifetime + $"{nameof(ImplementationInstance)}: {ImplementationInstance}";
            }
        }

        internal Type GetImplementationType()
        {
            if (ServiceKey == null)
            {
                if (ImplementationType != null)
                {
                    return ImplementationType;
                }
                else if (ImplementationInstance != null)
                {
                    return ImplementationInstance.GetType();
                }
                else if (ImplementationFactory != null)
                {
                    Type[]? typeArguments = ImplementationFactory.GetType().GenericTypeArguments;

                    Debug.Assert(typeArguments.Length == 2);

                    return typeArguments[1];
                }
            }
            else
            {
                if (KeyedImplementationType != null)
                {
                    return KeyedImplementationType;
                }
                else if (KeyedImplementationInstance != null)
                {
                    return KeyedImplementationInstance.GetType();
                }
                else if (KeyedImplementationFactory != null)
                {
                    Type[]? typeArguments = KeyedImplementationFactory.GetType().GenericTypeArguments;

                    Debug.Assert(typeArguments.Length == 3);

                    return typeArguments[2];
                }
            }

            Debug.Assert(false, "ImplementationType, ImplementationInstance, ImplementationFactory or KeyedImplementationFactory must be non null");
            return null;
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor"/> with the specified
        /// <typeparamref name="TService"/>, <typeparamref name="TImplementation"/>,
        /// and the <see cref="ServiceLifetime.Transient"/> lifetime.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <typeparam name="TImplementation">The type of the implementation.</typeparam>
        /// <returns>A new instance of <see cref="ServiceDescriptor"/>.</returns>
        public static ServiceDescriptor Transient<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            return DescribeKeyed<TService, TImplementation>(null, ServiceLifetime.Transient);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor"/> with the specified
        /// <typeparamref name="TService"/>, <typeparamref name="TImplementation"/>,
        /// and the <see cref="ServiceLifetime.Transient"/> lifetime.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <typeparam name="TImplementation">The type of the implementation.</typeparam>
        /// <param name="serviceKey">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor"/>.</returns>
        public static ServiceDescriptor KeyedTransient<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(object? serviceKey)
            where TService : class
            where TImplementation : class, TService
        {
            return DescribeKeyed<TService, TImplementation>(serviceKey, ServiceLifetime.Transient);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor"/> with the specified
        /// <paramref name="service"/> and <paramref name="implementationType"/>
        /// and the <see cref="ServiceLifetime.Transient"/> lifetime.
        /// </summary>
        /// <param name="service">The type of the service.</param>
        /// <param name="implementationType">The type of the implementation.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor"/>.</returns>
        public static ServiceDescriptor Transient(
            Type service,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType)
        {
            ThrowHelper.ThrowIfNull(service);
            ThrowHelper.ThrowIfNull(implementationType);

            return Describe(service, implementationType, ServiceLifetime.Transient);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor"/> with the specified
        /// <paramref name="service"/> and <paramref name="implementationType"/>
        /// and the <see cref="ServiceLifetime.Transient"/> lifetime.
        /// </summary>
        /// <param name="service">The type of the service.</param>
        /// <param name="serviceKey">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
        /// <param name="implementationType">The type of the implementation.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor"/>.</returns>
        public static ServiceDescriptor KeyedTransient(
            Type service,
            object? serviceKey,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType)
        {
            ThrowHelper.ThrowIfNull(service);
            ThrowHelper.ThrowIfNull(implementationType);

            return DescribeKeyed(service, serviceKey, implementationType, ServiceLifetime.Transient);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor"/> with the specified
        /// <typeparamref name="TService"/>, <typeparamref name="TImplementation"/>,
        /// <paramref name="implementationFactory"/>,
        /// and the <see cref="ServiceLifetime.Transient"/> lifetime.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <typeparam name="TImplementation">The type of the implementation.</typeparam>
        /// <param name="implementationFactory">A factory to create new instances of the service implementation.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor"/>.</returns>
        public static ServiceDescriptor Transient<TService, TImplementation>(
            Func<IServiceProvider, TImplementation> implementationFactory)
            where TService : class
            where TImplementation : class, TService
        {
            ThrowHelper.ThrowIfNull(implementationFactory);

            return Describe(typeof(TService), implementationFactory, ServiceLifetime.Transient);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor"/> with the specified
        /// <typeparamref name="TService"/>, <typeparamref name="TImplementation"/>,
        /// <paramref name="implementationFactory"/>,
        /// and the <see cref="ServiceLifetime.Transient"/> lifetime.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <typeparam name="TImplementation">The type of the implementation.</typeparam>
        /// <param name="serviceKey">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
        /// <param name="implementationFactory">A factory to create new instances of the service implementation.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor"/>.</returns>
        public static ServiceDescriptor KeyedTransient<TService, TImplementation>(
            object? serviceKey,
            Func<IServiceProvider, object?, TImplementation> implementationFactory)
            where TService : class
            where TImplementation : class, TService
        {
            ThrowHelper.ThrowIfNull(implementationFactory);

            return DescribeKeyed(typeof(TService), serviceKey, implementationFactory, ServiceLifetime.Transient);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor"/> with the specified
        /// <typeparamref name="TService"/>, <paramref name="implementationFactory"/>,
        /// and the <see cref="ServiceLifetime.Transient"/> lifetime.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <param name="implementationFactory">A factory to create new instances of the service implementation.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor"/>.</returns>
        public static ServiceDescriptor Transient<TService>(Func<IServiceProvider, TService> implementationFactory)
            where TService : class
        {
            ThrowHelper.ThrowIfNull(implementationFactory);

            return Describe(typeof(TService), implementationFactory, ServiceLifetime.Transient);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor"/> with the specified
        /// <typeparamref name="TService"/>, <paramref name="implementationFactory"/>,
        /// and the <see cref="ServiceLifetime.Transient"/> lifetime.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <param name="serviceKey">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
        /// <param name="implementationFactory">A factory to create new instances of the service implementation.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor"/>.</returns>
        public static ServiceDescriptor KeyedTransient<TService>(object? serviceKey, Func<IServiceProvider, object?, TService> implementationFactory)
            where TService : class
        {
            ThrowHelper.ThrowIfNull(implementationFactory);

            return DescribeKeyed(typeof(TService), serviceKey, implementationFactory, ServiceLifetime.Transient);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor"/> with the specified
        /// <paramref name="service"/>, <paramref name="implementationFactory"/>,
        /// and the <see cref="ServiceLifetime.Transient"/> lifetime.
        /// </summary>
        /// <param name="service">The type of the service.</param>
        /// <param name="implementationFactory">A factory to create new instances of the service implementation.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor"/>.</returns>
        public static ServiceDescriptor Transient(Type service, Func<IServiceProvider, object> implementationFactory)
        {
            ThrowHelper.ThrowIfNull(service);
            ThrowHelper.ThrowIfNull(implementationFactory);

            return Describe(service, implementationFactory, ServiceLifetime.Transient);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor"/> with the specified
        /// <paramref name="service"/>, <paramref name="implementationFactory"/>,
        /// and the <see cref="ServiceLifetime.Transient"/> lifetime.
        /// </summary>
        /// <param name="service">The type of the service.</param>
        /// <param name="serviceKey">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
        /// <param name="implementationFactory">A factory to create new instances of the service implementation.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor"/>.</returns>
        public static ServiceDescriptor KeyedTransient(Type service, object? serviceKey, Func<IServiceProvider, object?, object> implementationFactory)
        {
            ThrowHelper.ThrowIfNull(service);
            ThrowHelper.ThrowIfNull(implementationFactory);

            return DescribeKeyed(service, serviceKey, implementationFactory, ServiceLifetime.Transient);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor"/> with the specified
        /// <typeparamref name="TService"/>, <typeparamref name="TImplementation"/>,
        /// and the <see cref="ServiceLifetime.Scoped"/> lifetime.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <typeparam name="TImplementation">The type of the implementation.</typeparam>
        /// <returns>A new instance of <see cref="ServiceDescriptor"/>.</returns>
        public static ServiceDescriptor Scoped<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            return DescribeKeyed<TService, TImplementation>(null, ServiceLifetime.Scoped);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor"/> with the specified
        /// <typeparamref name="TService"/>, <typeparamref name="TImplementation"/>,
        /// and the <see cref="ServiceLifetime.Scoped"/> lifetime.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <typeparam name="TImplementation">The type of the implementation.</typeparam>
        /// <param name="serviceKey">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor"/>.</returns>
        public static ServiceDescriptor KeyedScoped<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(object? serviceKey)
            where TService : class
            where TImplementation : class, TService
        {
            return DescribeKeyed<TService, TImplementation>(serviceKey, ServiceLifetime.Scoped);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor"/> with the specified
        /// <paramref name="service"/> and <paramref name="implementationType"/>
        /// and the <see cref="ServiceLifetime.Scoped"/> lifetime.
        /// </summary>
        /// <param name="service">The type of the service.</param>
        /// <param name="implementationType">The type of the implementation.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor"/>.</returns>
        public static ServiceDescriptor Scoped(
            Type service,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType)
        {
            return Describe(service, implementationType, ServiceLifetime.Scoped);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor"/> with the specified
        /// <paramref name="service"/> and <paramref name="implementationType"/>
        /// and the <see cref="ServiceLifetime.Scoped"/> lifetime.
        /// </summary>
        /// <param name="service">The type of the service.</param>
        /// <param name="serviceKey">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
        /// <param name="implementationType">The type of the implementation.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor"/>.</returns>
        public static ServiceDescriptor KeyedScoped(
            Type service,
            object? serviceKey,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType)
        {
            return DescribeKeyed(service, serviceKey, implementationType, ServiceLifetime.Scoped);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor"/> with the specified
        /// <typeparamref name="TService"/>, <typeparamref name="TImplementation"/>,
        /// <paramref name="implementationFactory"/>,
        /// and the <see cref="ServiceLifetime.Scoped"/> lifetime.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <typeparam name="TImplementation">The type of the implementation.</typeparam>
        /// <param name="implementationFactory">A factory to create new instances of the service implementation.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor"/>.</returns>
        public static ServiceDescriptor Scoped<TService, TImplementation>(
            Func<IServiceProvider, TImplementation> implementationFactory)
            where TService : class
            where TImplementation : class, TService
        {
            ThrowHelper.ThrowIfNull(implementationFactory);

            return Describe(typeof(TService), implementationFactory, ServiceLifetime.Scoped);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor"/> with the specified
        /// <typeparamref name="TService"/>, <typeparamref name="TImplementation"/>,
        /// <paramref name="implementationFactory"/>,
        /// and the <see cref="ServiceLifetime.Scoped"/> lifetime.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <typeparam name="TImplementation">The type of the implementation.</typeparam>
        /// <param name="serviceKey">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
        /// <param name="implementationFactory">A factory to create new instances of the service implementation.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor"/>.</returns>
        public static ServiceDescriptor KeyedScoped<TService, TImplementation>(
            object? serviceKey,
            Func<IServiceProvider, object?, TImplementation> implementationFactory)
            where TService : class
            where TImplementation : class, TService
        {
            ThrowHelper.ThrowIfNull(implementationFactory);

            return DescribeKeyed(typeof(TService), serviceKey, implementationFactory, ServiceLifetime.Scoped);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor"/> with the specified
        /// <typeparamref name="TService"/>, <paramref name="implementationFactory"/>,
        /// and the <see cref="ServiceLifetime.Scoped"/> lifetime.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <param name="implementationFactory">A factory to create new instances of the service implementation.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor"/>.</returns>
        public static ServiceDescriptor Scoped<TService>(Func<IServiceProvider, TService> implementationFactory)
            where TService : class
        {
            ThrowHelper.ThrowIfNull(implementationFactory);

            return Describe(typeof(TService), implementationFactory, ServiceLifetime.Scoped);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor"/> with the specified
        /// <typeparamref name="TService"/>, <paramref name="implementationFactory"/>,
        /// and the <see cref="ServiceLifetime.Scoped"/> lifetime.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <param name="serviceKey">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
        /// <param name="implementationFactory">A factory to create new instances of the service implementation.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor"/>.</returns>
        public static ServiceDescriptor KeyedScoped<TService>(object? serviceKey, Func<IServiceProvider, object?, TService> implementationFactory)
            where TService : class
        {
            ThrowHelper.ThrowIfNull(implementationFactory);

            return DescribeKeyed(typeof(TService), serviceKey, implementationFactory, ServiceLifetime.Scoped);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor"/> with the specified
        /// <paramref name="service"/>, <paramref name="implementationFactory"/>,
        /// and the <see cref="ServiceLifetime.Scoped"/> lifetime.
        /// </summary>
        /// <param name="service">The type of the service.</param>
        /// <param name="implementationFactory">A factory to create new instances of the service implementation.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor"/>.</returns>
        public static ServiceDescriptor Scoped(Type service, Func<IServiceProvider, object> implementationFactory)
        {
            ThrowHelper.ThrowIfNull(service);
            ThrowHelper.ThrowIfNull(implementationFactory);

            return Describe(service, implementationFactory, ServiceLifetime.Scoped);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor"/> with the specified
        /// <paramref name="service"/>, <paramref name="implementationFactory"/>,
        /// and the <see cref="ServiceLifetime.Scoped"/> lifetime.
        /// </summary>
        /// <param name="service">The type of the service.</param>
        /// <param name="serviceKey">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
        /// <param name="implementationFactory">A factory to create new instances of the service implementation.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor"/>.</returns>
        public static ServiceDescriptor KeyedScoped(Type service, object? serviceKey, Func<IServiceProvider, object?, object> implementationFactory)
        {
            ThrowHelper.ThrowIfNull(service);
            ThrowHelper.ThrowIfNull(implementationFactory);

            return DescribeKeyed(service, serviceKey, implementationFactory, ServiceLifetime.Scoped);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor"/> with the specified
        /// <typeparamref name="TService"/>, <typeparamref name="TImplementation"/>,
        /// and the <see cref="ServiceLifetime.Singleton"/> lifetime.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <typeparam name="TImplementation">The type of the implementation.</typeparam>
        /// <returns>A new instance of <see cref="ServiceDescriptor"/>.</returns>
        public static ServiceDescriptor Singleton<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            return DescribeKeyed<TService, TImplementation>(null, ServiceLifetime.Singleton);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor"/> with the specified
        /// <typeparamref name="TService"/>, <typeparamref name="TImplementation"/>,
        /// and the <see cref="ServiceLifetime.Singleton"/> lifetime.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <typeparam name="TImplementation">The type of the implementation.</typeparam>
        /// <param name="serviceKey">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor"/>.</returns>
        public static ServiceDescriptor KeyedSingleton<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(
            object? serviceKey)
            where TService : class
            where TImplementation : class, TService
        {
            return DescribeKeyed<TService, TImplementation>(serviceKey, ServiceLifetime.Singleton);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor"/> with the specified
        /// <paramref name="service"/> and <paramref name="implementationType"/>
        /// and the <see cref="ServiceLifetime.Singleton"/> lifetime.
        /// </summary>
        /// <param name="service">The type of the service.</param>
        /// <param name="implementationType">The type of the implementation.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor"/>.</returns>
        public static ServiceDescriptor Singleton(
            Type service,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType)
        {
            ThrowHelper.ThrowIfNull(service);
            ThrowHelper.ThrowIfNull(implementationType);

            return Describe(service, implementationType, ServiceLifetime.Singleton);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor"/> with the specified
        /// <paramref name="service"/> and <paramref name="implementationType"/>
        /// and the <see cref="ServiceLifetime.Singleton"/> lifetime.
        /// </summary>
        /// <param name="service">The type of the service.</param>
        /// <param name="serviceKey">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
        /// <param name="implementationType">The type of the implementation.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor"/>.</returns>
        public static ServiceDescriptor KeyedSingleton(
            Type service,
            object? serviceKey,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType)
        {
            ThrowHelper.ThrowIfNull(service);
            ThrowHelper.ThrowIfNull(implementationType);

            return DescribeKeyed(service, serviceKey, implementationType, ServiceLifetime.Singleton);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor"/> with the specified
        /// <typeparamref name="TService"/>, <typeparamref name="TImplementation"/>,
        /// <paramref name="implementationFactory"/>,
        /// and the <see cref="ServiceLifetime.Singleton"/> lifetime.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <typeparam name="TImplementation">The type of the implementation.</typeparam>
        /// <param name="implementationFactory">A factory to create new instances of the service implementation.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor"/>.</returns>
        public static ServiceDescriptor Singleton<TService, TImplementation>(
            Func<IServiceProvider, TImplementation> implementationFactory)
            where TService : class
            where TImplementation : class, TService
        {
            ThrowHelper.ThrowIfNull(implementationFactory);

            return Describe(typeof(TService), implementationFactory, ServiceLifetime.Singleton);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor"/> with the specified
        /// <typeparamref name="TService"/>, <typeparamref name="TImplementation"/>,
        /// <paramref name="implementationFactory"/>,
        /// and the <see cref="ServiceLifetime.Singleton"/> lifetime.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <typeparam name="TImplementation">The type of the implementation.</typeparam>
        /// <param name="serviceKey">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
        /// <param name="implementationFactory">A factory to create new instances of the service implementation.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor"/>.</returns>
        public static ServiceDescriptor KeyedSingleton<TService, TImplementation>(
            object? serviceKey,
            Func<IServiceProvider, object?, TImplementation> implementationFactory)
            where TService : class
            where TImplementation : class, TService
        {
            ThrowHelper.ThrowIfNull(implementationFactory);

            return DescribeKeyed(typeof(TService), serviceKey, implementationFactory, ServiceLifetime.Singleton);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor"/> with the specified
        /// <typeparamref name="TService"/>, <paramref name="implementationFactory"/>,
        /// and the <see cref="ServiceLifetime.Singleton"/> lifetime.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <param name="implementationFactory">A factory to create new instances of the service implementation.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor"/>.</returns>
        public static ServiceDescriptor Singleton<TService>(Func<IServiceProvider, TService> implementationFactory)
            where TService : class
        {
            ThrowHelper.ThrowIfNull(implementationFactory);

            return Describe(typeof(TService), implementationFactory, ServiceLifetime.Singleton);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor"/> with the specified
        /// <typeparamref name="TService"/>, <paramref name="implementationFactory"/>,
        /// and the <see cref="ServiceLifetime.Singleton"/> lifetime.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <param name="serviceKey">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
        /// <param name="implementationFactory">A factory to create new instances of the service implementation.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor"/>.</returns>
        public static ServiceDescriptor KeyedSingleton<TService>(
            object? serviceKey,
            Func<IServiceProvider, object?, TService> implementationFactory)
            where TService : class
        {
            ThrowHelper.ThrowIfNull(implementationFactory);

            return DescribeKeyed(typeof(TService), serviceKey, implementationFactory, ServiceLifetime.Singleton);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor"/> with the specified
        /// <paramref name="serviceType"/>, <paramref name="implementationFactory"/>,
        /// and the <see cref="ServiceLifetime.Singleton"/> lifetime.
        /// </summary>
        /// <param name="serviceType">The type of the service.</param>
        /// <param name="implementationFactory">A factory to create new instances of the service implementation.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor"/>.</returns>
        public static ServiceDescriptor Singleton(
            Type serviceType,
            Func<IServiceProvider, object> implementationFactory)
        {
            ThrowHelper.ThrowIfNull(serviceType);
            ThrowHelper.ThrowIfNull(implementationFactory);

            return Describe(serviceType, implementationFactory, ServiceLifetime.Singleton);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor"/> with the specified
        /// <paramref name="serviceType"/>, <paramref name="implementationFactory"/>,
        /// and the <see cref="ServiceLifetime.Singleton"/> lifetime.
        /// </summary>
        /// <param name="serviceType">The type of the service.</param>
        /// <param name="serviceKey">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
        /// <param name="implementationFactory">A factory to create new instances of the service implementation.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor"/>.</returns>
        public static ServiceDescriptor KeyedSingleton(
            Type serviceType,
            object? serviceKey,
            Func<IServiceProvider, object?, object> implementationFactory)
        {
            ThrowHelper.ThrowIfNull(serviceType);
            ThrowHelper.ThrowIfNull(implementationFactory);

            return DescribeKeyed(serviceType, serviceKey, implementationFactory, ServiceLifetime.Singleton);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor"/> with the specified
        /// <typeparamref name="TService"/>, <paramref name="implementationInstance"/>,
        /// and the <see cref="ServiceLifetime.Singleton"/> lifetime.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <param name="implementationInstance">The instance of the implementation.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor"/>.</returns>
        public static ServiceDescriptor Singleton<TService>(TService implementationInstance)
            where TService : class
        {
            ThrowHelper.ThrowIfNull(implementationInstance);

            return Singleton(serviceType: typeof(TService), implementationInstance: implementationInstance);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor"/> with the specified
        /// <typeparamref name="TService"/>, <paramref name="implementationInstance"/>,
        /// and the <see cref="ServiceLifetime.Singleton"/> lifetime.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <param name="serviceKey">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
        /// <param name="implementationInstance">The instance of the implementation.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor"/>.</returns>
        public static ServiceDescriptor KeyedSingleton<TService>(
            object? serviceKey,
            TService implementationInstance)
            where TService : class
        {
            ThrowHelper.ThrowIfNull(implementationInstance);

            return KeyedSingleton(typeof(TService), serviceKey, implementationInstance);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor"/> with the specified
        /// <paramref name="serviceType"/>, <paramref name="implementationInstance"/>,
        /// and the <see cref="ServiceLifetime.Singleton"/> lifetime.
        /// </summary>
        /// <param name="serviceType">The type of the service.</param>
        /// <param name="implementationInstance">The instance of the implementation.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor"/>.</returns>
        public static ServiceDescriptor Singleton(
            Type serviceType,
            object implementationInstance)
        {
            ThrowHelper.ThrowIfNull(serviceType);
            ThrowHelper.ThrowIfNull(implementationInstance);

            return new ServiceDescriptor(serviceType, implementationInstance);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor"/> with the specified
        /// <paramref name="serviceType"/>, <paramref name="implementationInstance"/>,
        /// and the <see cref="ServiceLifetime.Singleton"/> lifetime.
        /// </summary>
        /// <param name="serviceType">The type of the service.</param>
        /// <param name="serviceKey">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
        /// <param name="implementationInstance">The instance of the implementation.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor"/>.</returns>
        public static ServiceDescriptor KeyedSingleton(
            Type serviceType,
            object? serviceKey,
            object implementationInstance)
        {
            ThrowHelper.ThrowIfNull(serviceType);
            ThrowHelper.ThrowIfNull(implementationInstance);

            return new ServiceDescriptor(serviceType, serviceKey, implementationInstance);
        }

        private static ServiceDescriptor DescribeKeyed<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(
            object? serviceKey,
            ServiceLifetime lifetime)
            where TService : class
            where TImplementation : class, TService
        {
            return DescribeKeyed(
                typeof(TService),
                serviceKey,
                typeof(TImplementation),
                lifetime: lifetime);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor"/> with the specified
        /// <paramref name="serviceType"/>, <paramref name="implementationType"/>,
        /// and <paramref name="lifetime"/>.
        /// </summary>
        /// <param name="serviceType">The type of the service.</param>
        /// <param name="implementationType">The type of the implementation.</param>
        /// <param name="lifetime">The lifetime of the service.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor"/>.</returns>
        public static ServiceDescriptor Describe(
            Type serviceType,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType,
            ServiceLifetime lifetime)
        {
            return new ServiceDescriptor(serviceType, implementationType, lifetime);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor"/> with the specified
        /// <paramref name="serviceType"/>, <paramref name="implementationType"/>,
        /// and <paramref name="lifetime"/>.
        /// </summary>
        /// <param name="serviceType">The type of the service.</param>
        /// <param name="serviceKey">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
        /// <param name="implementationType">The type of the implementation.</param>
        /// <param name="lifetime">The lifetime of the service.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor"/>.</returns>
        public static ServiceDescriptor DescribeKeyed(
            Type serviceType,
            object? serviceKey,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType,
            ServiceLifetime lifetime)
        {
            return new ServiceDescriptor(serviceType, serviceKey, implementationType, lifetime);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor"/> with the specified
        /// <paramref name="serviceType"/>, <paramref name="implementationFactory"/>,
        /// and <paramref name="lifetime"/>.
        /// </summary>
        /// <param name="serviceType">The type of the service.</param>
        /// <param name="implementationFactory">A factory to create new instances of the service implementation.</param>
        /// <param name="lifetime">The lifetime of the service.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor"/>.</returns>
        public static ServiceDescriptor Describe(Type serviceType, Func<IServiceProvider, object> implementationFactory, ServiceLifetime lifetime)
        {
            return new ServiceDescriptor(serviceType, implementationFactory, lifetime);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor"/> with the specified
        /// <paramref name="serviceType"/>, <paramref name="implementationFactory"/>,
        /// and <paramref name="lifetime"/>.
        /// </summary>
        /// <param name="serviceType">The type of the service.</param>
        /// <param name="serviceKey">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
        /// <param name="implementationFactory">A factory to create new instances of the service implementation.</param>
        /// <param name="lifetime">The lifetime of the service.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor"/>.</returns>
        public static ServiceDescriptor DescribeKeyed(Type serviceType, object? serviceKey, Func<IServiceProvider, object?, object> implementationFactory, ServiceLifetime lifetime)
        {
            return new ServiceDescriptor(serviceType, serviceKey, implementationFactory, lifetime);
        }

        private string DebuggerToString()
        {
            string debugText = $@"Lifetime = {Lifetime}, ServiceType = ""{ServiceType.FullName}""";

            // Either implementation type, factory or instance is set.
            if (IsKeyedService)
            {
                debugText += $@", ServiceKey = ""{ServiceKey}""";
                if (KeyedImplementationType != null)
                {
                    debugText += $@", KeyedImplementationType = ""{KeyedImplementationType.FullName}""";
                }
                else if (KeyedImplementationFactory != null)
                {
                    debugText += $@", KeyedImplementationFactory = {KeyedImplementationFactory.Method}";
                }
                else
                {
                    debugText += $@", KeyedImplementationInstance = {KeyedImplementationInstance}";
                }
            }
            else
            {
                if (ImplementationType != null)
                {
                    debugText += $@", ImplementationType = ""{ImplementationType.FullName}""";
                }
                else if (ImplementationFactory != null)
                {
                    debugText += $@", ImplementationFactory = {ImplementationFactory.Method}";
                }
                else
                {
                    debugText += $@", ImplementationInstance = {ImplementationInstance}";
                }
            }

            return debugText;
        }

        private static void ThrowKeyedDescriptor() => throw new InvalidOperationException(SR.KeyedDescriptorMisuse);

        private static void ThrowNonKeyedDescriptor() => throw new InvalidOperationException(SR.NonKeyedDescriptorMisuse);
    }
}
