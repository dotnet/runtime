// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.DependencyInjection.Extensions
{
    public static partial class ServiceCollectionDescriptorExtensions
    {
        /// <summary>
        /// Adds the specified <paramref name="service"/> as a <see cref="ServiceLifetime.Transient"/> service
        /// to the <paramref name="collection"/> if the service type hasn't already been registered.
        /// </summary>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="service">The type of the service to register.</param>
        /// <param name="serviceKey">The service key.</param>
        public static void TryAddKeyedTransient(
            this IServiceCollection collection,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type service,
            object? serviceKey)
        {
            ThrowHelper.ThrowIfNull(collection);
            ThrowHelper.ThrowIfNull(service);

            var descriptor = ServiceDescriptor.KeyedTransient(service, serviceKey, service);
            ServiceCollectionDescriptorExtensions.TryAdd(collection, descriptor);
        }

        /// <summary>
        /// Adds the specified <paramref name="service"/> as a <see cref="ServiceLifetime.Transient"/> service
        /// with the <paramref name="implementationType"/> implementation
        /// to the <paramref name="collection"/> if the service type hasn't already been registered.
        /// </summary>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="service">The type of the service to register.</param>
        /// <param name="serviceKey">The service key.</param>
        /// <param name="implementationType">The implementation type of the service.</param>
        public static void TryAddKeyedTransient(
            this IServiceCollection collection,
            Type service,
            object? serviceKey,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType)
        {
            ThrowHelper.ThrowIfNull(collection);
            ThrowHelper.ThrowIfNull(service);
            ThrowHelper.ThrowIfNull(implementationType);

            var descriptor = ServiceDescriptor.KeyedTransient(service, serviceKey, implementationType);
            ServiceCollectionDescriptorExtensions.TryAdd(collection, descriptor);
        }

        /// <summary>
        /// Adds the specified <paramref name="service"/> as a <see cref="ServiceLifetime.Transient"/> service
        /// using the factory specified in <paramref name="implementationFactory"/>
        /// to the <paramref name="collection"/> if the service type hasn't already been registered.
        /// </summary>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="service">The type of the service to register.</param>
        /// <param name="serviceKey">The service key.</param>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        public static void TryAddKeyedTransient(
            this IServiceCollection collection,
            Type service,
            object? serviceKey,
            Func<IServiceProvider, object?, object> implementationFactory)
        {
            ThrowHelper.ThrowIfNull(collection);
            ThrowHelper.ThrowIfNull(service);
            ThrowHelper.ThrowIfNull(implementationFactory);

            var descriptor = ServiceDescriptor.KeyedTransient(service, serviceKey, implementationFactory);
            ServiceCollectionDescriptorExtensions.TryAdd(collection, descriptor);
        }

        /// <summary>
        /// Adds the specified <typeparamref name="TService"/> as a <see cref="ServiceLifetime.Transient"/> service
        /// to the <paramref name="collection"/> if the service type hasn't already been registered.
        /// </summary>
        /// <typeparam name="TService">The type of the service to add.</typeparam>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="serviceKey">The service key.</param>
        public static void TryAddKeyedTransient<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TService>(this IServiceCollection collection, object? serviceKey)
            where TService : class
        {
            ThrowHelper.ThrowIfNull(collection);

            TryAddKeyedTransient(collection, typeof(TService), serviceKey, typeof(TService));
        }

        /// <summary>
        /// Adds the specified <typeparamref name="TService"/> as a <see cref="ServiceLifetime.Transient"/> service
        /// implementation type specified in <typeparamref name="TImplementation"/>
        /// to the <paramref name="collection"/> if the service type hasn't already been registered.
        /// </summary>
        /// <typeparam name="TService">The type of the service to add.</typeparam>
        /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="serviceKey">The service key.</param>
        public static void TryAddKeyedTransient<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(this IServiceCollection collection, object? serviceKey)
            where TService : class
            where TImplementation : class, TService
        {
            ThrowHelper.ThrowIfNull(collection);

            TryAddKeyedTransient(collection, typeof(TService), serviceKey, typeof(TImplementation));
        }

        /// <summary>
        /// Adds the specified <typeparamref name="TService"/> as a <see cref="ServiceLifetime.Transient"/> service
        /// using the factory specified in <paramref name="implementationFactory"/>
        /// to the <paramref name="services"/> if the service type hasn't already been registered.
        /// </summary>
        /// <typeparam name="TService">The type of the service to add.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="serviceKey">The service key.</param>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        public static void TryAddKeyedTransient<TService>(
            this IServiceCollection services,
            object? serviceKey,
            Func<IServiceProvider, object?, TService> implementationFactory)
            where TService : class
        {
            services.TryAdd(ServiceDescriptor.KeyedTransient(serviceKey, implementationFactory));
        }

        /// <summary>
        /// Adds the specified <paramref name="service"/> as a <see cref="ServiceLifetime.Scoped"/> service
        /// to the <paramref name="collection"/> if the service type hasn't already been registered.
        /// </summary>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="service">The type of the service to register.</param>
        /// <param name="serviceKey">The service key.</param>
        public static void TryAddKeyedScoped(
            this IServiceCollection collection,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type service,
            object? serviceKey)
        {
            ThrowHelper.ThrowIfNull(collection);
            ThrowHelper.ThrowIfNull(service);

            var descriptor = ServiceDescriptor.KeyedScoped(service, serviceKey, service);
            ServiceCollectionDescriptorExtensions.TryAdd(collection, descriptor);
        }

        /// <summary>
        /// Adds the specified <paramref name="service"/> as a <see cref="ServiceLifetime.Scoped"/> service
        /// with the <paramref name="implementationType"/> implementation
        /// to the <paramref name="collection"/> if the service type hasn't already been registered.
        /// </summary>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="service">The type of the service to register.</param>
        /// <param name="serviceKey">The service key.</param>
        /// <param name="implementationType">The implementation type of the service.</param>
        public static void TryAddKeyedScoped(
            this IServiceCollection collection,
            Type service,
            object? serviceKey,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType)
        {
            ThrowHelper.ThrowIfNull(collection);
            ThrowHelper.ThrowIfNull(service);
            ThrowHelper.ThrowIfNull(implementationType);

            var descriptor = ServiceDescriptor.KeyedScoped(service, serviceKey, implementationType);
            ServiceCollectionDescriptorExtensions.TryAdd(collection, descriptor);
        }

        /// <summary>
        /// Adds the specified <paramref name="service"/> as a <see cref="ServiceLifetime.Scoped"/> service
        /// using the factory specified in <paramref name="implementationFactory"/>
        /// to the <paramref name="collection"/> if the service type hasn't already been registered.
        /// </summary>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="service">The type of the service to register.</param>
        /// <param name="serviceKey">The service key.</param>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        public static void TryAddKeyedScoped(
            this IServiceCollection collection,
            Type service,
            object? serviceKey,
            Func<IServiceProvider, object?, object> implementationFactory)
        {
            ThrowHelper.ThrowIfNull(collection);
            ThrowHelper.ThrowIfNull(service);
            ThrowHelper.ThrowIfNull(implementationFactory);

            var descriptor = ServiceDescriptor.KeyedScoped(service, serviceKey, implementationFactory);
            ServiceCollectionDescriptorExtensions.TryAdd(collection, descriptor);
        }

        /// <summary>
        /// Adds the specified <typeparamref name="TService"/> as a <see cref="ServiceLifetime.Scoped"/> service
        /// to the <paramref name="collection"/> if the service type hasn't already been registered.
        /// </summary>
        /// <typeparam name="TService">The type of the service to add.</typeparam>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="serviceKey">The service key.</param>
        public static void TryAddKeyedScoped<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TService>(this IServiceCollection collection, object? serviceKey)
            where TService : class
        {
            ThrowHelper.ThrowIfNull(collection);

            TryAddKeyedScoped(collection, typeof(TService), serviceKey, typeof(TService));
        }

        /// <summary>
        /// Adds the specified <typeparamref name="TService"/> as a <see cref="ServiceLifetime.Scoped"/> service
        /// implementation type specified in <typeparamref name="TImplementation"/>
        /// to the <paramref name="collection"/> if the service type hasn't already been registered.
        /// </summary>
        /// <typeparam name="TService">The type of the service to add.</typeparam>
        /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="serviceKey">The service key.</param>
        public static void TryAddKeyedScoped<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(this IServiceCollection collection, object? serviceKey)
            where TService : class
            where TImplementation : class, TService
        {
            ThrowHelper.ThrowIfNull(collection);

            TryAddKeyedScoped(collection, typeof(TService), serviceKey, typeof(TImplementation));
        }

        /// <summary>
        /// Adds the specified <typeparamref name="TService"/> as a <see cref="ServiceLifetime.Scoped"/> service
        /// using the factory specified in <paramref name="implementationFactory"/>
        /// to the <paramref name="services"/> if the service type hasn't already been registered.
        /// </summary>
        /// <typeparam name="TService">The type of the service to add.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <param name="serviceKey">The service key.</param>
        public static void TryAddKeyedScoped<TService>(
            this IServiceCollection services,
            object? serviceKey,
            Func<IServiceProvider, object?, TService> implementationFactory)
            where TService : class
        {
            services.TryAdd(ServiceDescriptor.KeyedScoped(serviceKey, implementationFactory));
        }

        /// <summary>
        /// Adds the specified <paramref name="service"/> as a <see cref="ServiceLifetime.Singleton"/> service
        /// to the <paramref name="collection"/> if the service type hasn't already been registered.
        /// </summary>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="service">The type of the service to register.</param>
        /// <param name="serviceKey">The service key.</param>
        public static void TryAddKeyedSingleton(
            this IServiceCollection collection,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type service,
            object? serviceKey)
        {
            ThrowHelper.ThrowIfNull(collection);
            ThrowHelper.ThrowIfNull(service);

            var descriptor = ServiceDescriptor.KeyedSingleton(service, serviceKey, service);
            ServiceCollectionDescriptorExtensions.TryAdd(collection, descriptor);
        }

        /// <summary>
        /// Adds the specified <paramref name="service"/> as a <see cref="ServiceLifetime.Singleton"/> service
        /// with the <paramref name="implementationType"/> implementation
        /// to the <paramref name="collection"/> if the service type hasn't already been registered.
        /// </summary>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="service">The type of the service to register.</param>
        /// <param name="serviceKey">The service key.</param>
        /// <param name="implementationType">The implementation type of the service.</param>
        public static void TryAddKeyedSingleton(
            this IServiceCollection collection,
            Type service,
            object? serviceKey,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType)
        {
            ThrowHelper.ThrowIfNull(collection);
            ThrowHelper.ThrowIfNull(service);
            ThrowHelper.ThrowIfNull(implementationType);

            var descriptor = ServiceDescriptor.KeyedSingleton(service, serviceKey, implementationType);
            ServiceCollectionDescriptorExtensions.TryAdd(collection, descriptor);
        }

        /// <summary>
        /// Adds the specified <paramref name="service"/> as a <see cref="ServiceLifetime.Singleton"/> service
        /// using the factory specified in <paramref name="implementationFactory"/>
        /// to the <paramref name="collection"/> if the service type hasn't already been registered.
        /// </summary>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="service">The type of the service to register.</param>
        /// <param name="serviceKey">The service key.</param>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        public static void TryAddKeyedSingleton(
            this IServiceCollection collection,
            Type service,
            object? serviceKey,
            Func<IServiceProvider, object?, object> implementationFactory)
        {
            ThrowHelper.ThrowIfNull(collection);
            ThrowHelper.ThrowIfNull(service);
            ThrowHelper.ThrowIfNull(implementationFactory);

            var descriptor = ServiceDescriptor.KeyedSingleton(service, serviceKey, implementationFactory);
            ServiceCollectionDescriptorExtensions.TryAdd(collection, descriptor);
        }

        /// <summary>
        /// Adds the specified <typeparamref name="TService"/> as a <see cref="ServiceLifetime.Singleton"/> service
        /// to the <paramref name="collection"/> if the service type hasn't already been registered.
        /// </summary>
        /// <typeparam name="TService">The type of the service to add.</typeparam>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="serviceKey">The service key.</param>
        public static void TryAddKeyedSingleton<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TService>(this IServiceCollection collection, object? serviceKey)
            where TService : class
        {
            ThrowHelper.ThrowIfNull(collection);

            TryAddKeyedSingleton(collection, typeof(TService), serviceKey, typeof(TService));
        }

        /// <summary>
        /// Adds the specified <typeparamref name="TService"/> as a <see cref="ServiceLifetime.Singleton"/> service
        /// implementation type specified in <typeparamref name="TImplementation"/>
        /// to the <paramref name="collection"/> if the service type hasn't already been registered.
        /// </summary>
        /// <typeparam name="TService">The type of the service to add.</typeparam>
        /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="serviceKey">The service key.</param>
        public static void TryAddKeyedSingleton<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(this IServiceCollection collection, object? serviceKey)
            where TService : class
            where TImplementation : class, TService
        {
            ThrowHelper.ThrowIfNull(collection);

            TryAddKeyedSingleton(collection, typeof(TService), serviceKey, typeof(TImplementation));
        }

        /// <summary>
        /// Adds the specified <typeparamref name="TService"/> as a <see cref="ServiceLifetime.Singleton"/> service
        /// with an instance specified in <paramref name="instance"/>
        /// to the <paramref name="collection"/> if the service type hasn't already been registered.
        /// </summary>
        /// <typeparam name="TService">The type of the service to add.</typeparam>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="serviceKey">The service key.</param>
        /// <param name="instance">The instance of the service to add.</param>
        public static void TryAddKeyedSingleton<TService>(this IServiceCollection collection, object? serviceKey, TService instance)
            where TService : class
        {
            ThrowHelper.ThrowIfNull(collection);
            ThrowHelper.ThrowIfNull(instance);

            var descriptor = ServiceDescriptor.KeyedSingleton(serviceType: typeof(TService), serviceKey, implementationInstance: instance);
            ServiceCollectionDescriptorExtensions.TryAdd(collection, descriptor);
        }

        /// <summary>
        /// Adds the specified <typeparamref name="TService"/> as a <see cref="ServiceLifetime.Singleton"/> service
        /// using the factory specified in <paramref name="implementationFactory"/>
        /// to the <paramref name="services"/> if the service type hasn't already been registered.
        /// </summary>
        /// <typeparam name="TService">The type of the service to add.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="serviceKey">The service key.</param>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        public static void TryAddKeyedSingleton<TService>(
            this IServiceCollection services,
            object? serviceKey,
            Func<IServiceProvider, object?, TService> implementationFactory)
            where TService : class
        {
            services.TryAdd(ServiceDescriptor.KeyedSingleton(serviceKey, implementationFactory));
        }

        /// <summary>
        /// Removes all services of type <typeparamref name="T"/> in <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="serviceKey">The service key.</param>
        /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
        public static IServiceCollection RemoveAllKeyed<T>(this IServiceCollection collection, object? serviceKey)
        {
            return RemoveAllKeyed(collection, typeof(T), serviceKey);
        }

        /// <summary>
        /// Removes all services of type <paramref name="serviceType"/> in <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="serviceType">The service type to remove.</param>
        /// <param name="serviceKey">The service key.</param>
        /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
        public static IServiceCollection RemoveAllKeyed(this IServiceCollection collection, Type serviceType, object? serviceKey)
        {
            ThrowHelper.ThrowIfNull(serviceType);

            for (int i = collection.Count - 1; i >= 0; i--)
            {
                ServiceDescriptor? descriptor = collection[i];
                if (descriptor.ServiceType == serviceType && object.Equals(descriptor.ServiceKey, serviceKey))
                {
                    collection.RemoveAt(i);
                }
            }

            return collection;
        }
    }
}
