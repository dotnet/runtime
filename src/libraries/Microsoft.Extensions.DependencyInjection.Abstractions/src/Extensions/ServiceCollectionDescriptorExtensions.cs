// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection.Abstractions;

namespace Microsoft.Extensions.DependencyInjection.Extensions
{
    /// <summary>
    /// Extension methods for adding and removing services to an <see cref="IServiceCollection" />.
    /// </summary>
    public static class ServiceCollectionDescriptorExtensions
    {
        /// <summary>
        /// Adds the specified <paramref name="descriptor"/> to the <paramref name="collection"/>.
        /// </summary>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="descriptor">The <see cref="ServiceDescriptor"/> to add.</param>
        /// <returns>A reference to the current instance of <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection Add(
            this IServiceCollection collection,
            ServiceDescriptor descriptor)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            collection.Add(descriptor);
            return collection;
        }

        /// <summary>
        /// Adds a sequence of <see cref="ServiceDescriptor"/> to the <paramref name="collection"/>.
        /// </summary>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="descriptors">The <see cref="ServiceDescriptor"/>s to add.</param>
        /// <returns>A reference to the current instance of <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection Add(
            this IServiceCollection collection,
            IEnumerable<ServiceDescriptor> descriptors)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if (descriptors == null)
            {
                throw new ArgumentNullException(nameof(descriptors));
            }

            foreach (var descriptor in descriptors)
            {
                collection.Add(descriptor);
            }

            return collection;
        }

        /// <summary>
        /// Adds the specified <paramref name="descriptor"/> to the <paramref name="collection"/> if the
        /// service type hasn't already been registered.
        /// </summary>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="descriptor">The <see cref="ServiceDescriptor"/> to add.</param>
        public static void TryAdd(
            this IServiceCollection collection,
            ServiceDescriptor descriptor)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (!collection.Any(d => d.ServiceType == descriptor.ServiceType))
            {
                collection.Add(descriptor);
            }
        }

        /// <summary>
        /// Adds the specified <paramref name="descriptors"/> to the <paramref name="collection"/> if the
        /// service type hasn't already been registered.
        /// </summary>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="descriptors">The <see cref="ServiceDescriptor"/>s to add.</param>
        public static void TryAdd(
            this IServiceCollection collection,
            IEnumerable<ServiceDescriptor> descriptors)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if (descriptors == null)
            {
                throw new ArgumentNullException(nameof(descriptors));
            }

            foreach (var d in descriptors)
            {
                collection.TryAdd(d);
            }
        }

        /// <summary>
        /// Adds the specified <paramref name="service"/> as a <see cref="ServiceLifetime.Transient"/> service
        /// to the <paramref name="collection"/> if the service type hasn't already been registered.
        /// </summary>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="service">The type of the service to register.</param>
        public static void TryAddTransient(
            this IServiceCollection collection,
            Type service)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            var descriptor = ServiceDescriptor.Transient(service, service);
            TryAdd(collection, descriptor);
        }

        /// <summary>
        /// Adds the specified <paramref name="service"/> as a <see cref="ServiceLifetime.Transient"/> service
        /// with the <paramref name="implementationType"/> implementation
        /// to the <paramref name="collection"/> if the service type hasn't already been registered.
        /// </summary>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="service">The type of the service to register.</param>
        /// <param name="implementationType">The implementation type of the service.</param>
        public static void TryAddTransient(
            this IServiceCollection collection,
            Type service,
            Type implementationType)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            if (implementationType == null)
            {
                throw new ArgumentNullException(nameof(implementationType));
            }

            var descriptor = ServiceDescriptor.Transient(service, implementationType);
            TryAdd(collection, descriptor);
        }

        /// <summary>
        /// Adds the specified <paramref name="service"/> as a <see cref="ServiceLifetime.Transient"/> service
        /// using the factory specified in <paramref name="implementationFactory"/>
        /// to the <paramref name="collection"/> if the service type hasn't already been registered.
        /// </summary>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="service">The type of the service to register.</param>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        public static void TryAddTransient(
            this IServiceCollection collection,
            Type service,
            Func<IServiceProvider, object> implementationFactory)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            if (implementationFactory == null)
            {
                throw new ArgumentNullException(nameof(implementationFactory));
            }

            var descriptor = ServiceDescriptor.Transient(service, implementationFactory);
            TryAdd(collection, descriptor);
        }

        /// <summary>
        /// Adds the specified <typeparamref name="TService"/> as a <see cref="ServiceLifetime.Transient"/> service
        /// to the <paramref name="collection"/> if the service type hasn't already been registered.
        /// </summary>
        /// <typeparam name="TService">The type of the service to add.</typeparam>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        public static void TryAddTransient<TService>(this IServiceCollection collection)
            where TService : class
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            TryAddTransient(collection, typeof(TService), typeof(TService));
        }

        /// <summary>
        /// Adds the specified <typeparamref name="TService"/> as a <see cref="ServiceLifetime.Transient"/> service
        /// implementation type specified in <typeparamref name="TImplementation"/>
        /// to the <paramref name="collection"/> if the service type hasn't already been registered.
        /// </summary>
        /// <typeparam name="TService">The type of the service to add.</typeparam>
        /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        public static void TryAddTransient<TService, TImplementation>(this IServiceCollection collection)
            where TService : class
            where TImplementation : class, TService
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            TryAddTransient(collection, typeof(TService), typeof(TImplementation));
        }

        /// <summary>
        /// Adds the specified <typeparamref name="TService"/> as a <see cref="ServiceLifetime.Transient"/> service
        /// using the factory specified in <paramref name="implementationFactory"/>
        /// to the <paramref name="services"/> if the service type hasn't already been registered.
        /// </summary>
        /// <typeparam name="TService">The type of the service to add.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        public static void TryAddTransient<TService>(
            this IServiceCollection services,
            Func<IServiceProvider, TService> implementationFactory)
            where TService : class
        {
            services.TryAdd(ServiceDescriptor.Transient(implementationFactory));
        }

        /// <summary>
        /// Adds the specified <paramref name="service"/> as a <see cref="ServiceLifetime.Scoped"/> service
        /// to the <paramref name="collection"/> if the service type hasn't already been registered.
        /// </summary>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="service">The type of the service to register.</param>
        public static void TryAddScoped(
            this IServiceCollection collection,
            Type service)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            var descriptor = ServiceDescriptor.Scoped(service, service);
            TryAdd(collection, descriptor);
        }

        /// <summary>
        /// Adds the specified <paramref name="service"/> as a <see cref="ServiceLifetime.Scoped"/> service
        /// with the <paramref name="implementationType"/> implementation
        /// to the <paramref name="collection"/> if the service type hasn't already been registered.
        /// </summary>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="service">The type of the service to register.</param>
        /// <param name="implementationType">The implementation type of the service.</param>
        public static void TryAddScoped(
            this IServiceCollection collection,
            Type service,
            Type implementationType)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            if (implementationType == null)
            {
                throw new ArgumentNullException(nameof(implementationType));
            }

            var descriptor = ServiceDescriptor.Scoped(service, implementationType);
            TryAdd(collection, descriptor);
        }

        /// <summary>
        /// Adds the specified <paramref name="service"/> as a <see cref="ServiceLifetime.Scoped"/> service
        /// using the factory specified in <paramref name="implementationFactory"/>
        /// to the <paramref name="collection"/> if the service type hasn't already been registered.
        /// </summary>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="service">The type of the service to register.</param>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        public static void TryAddScoped(
            this IServiceCollection collection,
            Type service,
            Func<IServiceProvider, object> implementationFactory)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            if (implementationFactory == null)
            {
                throw new ArgumentNullException(nameof(implementationFactory));
            }

            var descriptor = ServiceDescriptor.Scoped(service, implementationFactory);
            TryAdd(collection, descriptor);
        }

        /// <summary>
        /// Adds the specified <typeparamref name="TService"/> as a <see cref="ServiceLifetime.Scoped"/> service
        /// to the <paramref name="collection"/> if the service type hasn't already been registered.
        /// </summary>
        /// <typeparam name="TService">The type of the service to add.</typeparam>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        public static void TryAddScoped<TService>(this IServiceCollection collection)
            where TService : class
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            TryAddScoped(collection, typeof(TService), typeof(TService));
        }

        /// <summary>
        /// Adds the specified <typeparamref name="TService"/> as a <see cref="ServiceLifetime.Scoped"/> service
        /// implementation type specified in <typeparamref name="TImplementation"/>
        /// to the <paramref name="collection"/> if the service type hasn't already been registered.
        /// </summary>
        /// <typeparam name="TService">The type of the service to add.</typeparam>
        /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        public static void TryAddScoped<TService, TImplementation>(this IServiceCollection collection)
            where TService : class
            where TImplementation : class, TService
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            TryAddScoped(collection, typeof(TService), typeof(TImplementation));
        }

        /// <summary>
        /// Adds the specified <typeparamref name="TService"/> as a <see cref="ServiceLifetime.Scoped"/> service
        /// using the factory specified in <paramref name="implementationFactory"/>
        /// to the <paramref name="services"/> if the service type hasn't already been registered.
        /// </summary>
        /// <typeparam name="TService">The type of the service to add.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        public static void TryAddScoped<TService>(
            this IServiceCollection services,
            Func<IServiceProvider, TService> implementationFactory)
            where TService : class
        {
            services.TryAdd(ServiceDescriptor.Scoped(implementationFactory));
        }

        /// <summary>
        /// Adds the specified <paramref name="service"/> as a <see cref="ServiceLifetime.Singleton"/> service
        /// to the <paramref name="collection"/> if the service type hasn't already been registered.
        /// </summary>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="service">The type of the service to register.</param>
        public static void TryAddSingleton(
            this IServiceCollection collection,
            Type service)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            var descriptor = ServiceDescriptor.Singleton(service, service);
            TryAdd(collection, descriptor);
        }

        /// <summary>
        /// Adds the specified <paramref name="service"/> as a <see cref="ServiceLifetime.Singleton"/> service
        /// with the <paramref name="implementationType"/> implementation
        /// to the <paramref name="collection"/> if the service type hasn't already been registered.
        /// </summary>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="service">The type of the service to register.</param>
        /// <param name="implementationType">The implementation type of the service.</param>
        public static void TryAddSingleton(
            this IServiceCollection collection,
            Type service,
            Type implementationType)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            if (implementationType == null)
            {
                throw new ArgumentNullException(nameof(implementationType));
            }

            var descriptor = ServiceDescriptor.Singleton(service, implementationType);
            TryAdd(collection, descriptor);
        }

        /// <summary>
        /// Adds the specified <paramref name="service"/> as a <see cref="ServiceLifetime.Singleton"/> service
        /// using the factory specified in <paramref name="implementationFactory"/>
        /// to the <paramref name="collection"/> if the service type hasn't already been registered.
        /// </summary>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="service">The type of the service to register.</param>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        public static void TryAddSingleton(
            this IServiceCollection collection,
            Type service,
            Func<IServiceProvider, object> implementationFactory)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            if (implementationFactory == null)
            {
                throw new ArgumentNullException(nameof(implementationFactory));
            }

            var descriptor = ServiceDescriptor.Singleton(service, implementationFactory);
            TryAdd(collection, descriptor);
        }

        /// <summary>
        /// Adds the specified <typeparamref name="TService"/> as a <see cref="ServiceLifetime.Singleton"/> service
        /// to the <paramref name="collection"/> if the service type hasn't already been registered.
        /// </summary>
        /// <typeparam name="TService">The type of the service to add.</typeparam>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        public static void TryAddSingleton<TService>(this IServiceCollection collection)
            where TService : class
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            TryAddSingleton(collection, typeof(TService), typeof(TService));
        }

        /// <summary>
        /// Adds the specified <typeparamref name="TService"/> as a <see cref="ServiceLifetime.Singleton"/> service
        /// implementation type specified in <typeparamref name="TImplementation"/>
        /// to the <paramref name="collection"/> if the service type hasn't already been registered.
        /// </summary>
        /// <typeparam name="TService">The type of the service to add.</typeparam>
        /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        public static void TryAddSingleton<TService, TImplementation>(this IServiceCollection collection)
            where TService : class
            where TImplementation : class, TService
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            TryAddSingleton(collection, typeof(TService), typeof(TImplementation));
        }

        /// <summary>
        /// Adds the specified <typeparamref name="TService"/> as a <see cref="ServiceLifetime.Singleton"/> service
        /// with an instance specified in <paramref name="instance"/>
        /// to the <paramref name="collection"/> if the service type hasn't already been registered.
        /// </summary>
        /// <typeparam name="TService">The type of the service to add.</typeparam>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="instance">The instance of the service to add.</param>
        public static void TryAddSingleton<TService>(this IServiceCollection collection, TService instance)
            where TService : class
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            var descriptor = ServiceDescriptor.Singleton(typeof(TService), instance);
            TryAdd(collection, descriptor);
        }

        /// <summary>
        /// Adds the specified <typeparamref name="TService"/> as a <see cref="ServiceLifetime.Singleton"/> service
        /// using the factory specified in <paramref name="implementationFactory"/>
        /// to the <paramref name="services"/> if the service type hasn't already been registered.
        /// </summary>
        /// <typeparam name="TService">The type of the service to add.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        public static void TryAddSingleton<TService>(
            this IServiceCollection services,
            Func<IServiceProvider, TService> implementationFactory)
            where TService : class
        {
            services.TryAdd(ServiceDescriptor.Singleton(implementationFactory));
        }

        /// <summary>
        /// Adds a <see cref="ServiceDescriptor"/> if an existing descriptor with the same
        /// <see cref="ServiceDescriptor.ServiceType"/> and an implementation that does not already exist
        /// in <paramref name="services."/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="descriptor">The <see cref="ServiceDescriptor"/>.</param>
        /// <remarks>
        /// Use <see cref="TryAddEnumerable(IServiceCollection, ServiceDescriptor)"/> when registering a service implementation of a
        /// service type that
        /// supports multiple registrations of the same service type. Using
        /// <see cref="Add(IServiceCollection, ServiceDescriptor)"/> is not idempotent and can add
        /// duplicate
        /// <see cref="ServiceDescriptor"/> instances if called twice. Using
        /// <see cref="TryAddEnumerable(IServiceCollection, ServiceDescriptor)"/> will prevent registration
        /// of multiple implementation types.
        /// </remarks>
        public static void TryAddEnumerable(
            this IServiceCollection services,
            ServiceDescriptor descriptor)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            var implementationType = descriptor.GetImplementationType();

            if (implementationType == typeof(object) ||
                implementationType == descriptor.ServiceType)
            {
                throw new ArgumentException(
                    Resources.FormatTryAddIndistinguishableTypeToEnumerable(
                        implementationType,
                        descriptor.ServiceType),
                    nameof(descriptor));
            }

            if (!services.Any(d =>
                              d.ServiceType == descriptor.ServiceType &&
                              d.GetImplementationType() == implementationType))
            {
                services.Add(descriptor);
            }
        }

        /// <summary>
        /// Adds the specified <see cref="ServiceDescriptor"/>s if an existing descriptor with the same
        /// <see cref="ServiceDescriptor.ServiceType"/> and an implementation that does not already exist
        /// in <paramref name="services."/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="descriptors">The <see cref="ServiceDescriptor"/>s.</param>
        /// <remarks>
        /// Use <see cref="TryAddEnumerable(IServiceCollection, ServiceDescriptor)"/> when registering a service
        /// implementation of a service type that
        /// supports multiple registrations of the same service type. Using
        /// <see cref="Add(IServiceCollection, ServiceDescriptor)"/> is not idempotent and can add
        /// duplicate
        /// <see cref="ServiceDescriptor"/> instances if called twice. Using
        /// <see cref="TryAddEnumerable(IServiceCollection, ServiceDescriptor)"/> will prevent registration
        /// of multiple implementation types.
        /// </remarks>
        public static void TryAddEnumerable(
            this IServiceCollection services,
            IEnumerable<ServiceDescriptor> descriptors)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (descriptors == null)
            {
                throw new ArgumentNullException(nameof(descriptors));
            }

            foreach (var d in descriptors)
            {
                services.TryAddEnumerable(d);
            }
        }

        /// <summary>
        /// Removes the first service in <see cref="IServiceCollection"/> with the same service type
        /// as <paramref name="descriptor"/> and adds <paramef name="descriptor"/> to the collection.
        /// </summary>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="descriptor">The <see cref="ServiceDescriptor"/> to replace with.</param>
        /// <returns></returns>
        public static IServiceCollection Replace(
            this IServiceCollection collection,
            ServiceDescriptor descriptor)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            var registeredServiceDescriptor = collection.FirstOrDefault(s => s.ServiceType == descriptor.ServiceType);
            if (registeredServiceDescriptor != null)
            {
                collection.Remove(registeredServiceDescriptor);
            }

            collection.Add(descriptor);
            return collection;
        }

        /// <summary>
        /// Removes all services of type <typeparamef name="T"/> in <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        /// <returns></returns>
        public static IServiceCollection RemoveAll<T>(this IServiceCollection collection)
        {
            return RemoveAll(collection, typeof(T));
        }

        /// <summary>
        /// Removes all services of type <paramef name="serviceType"/> in <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="serviceType">The service type to remove.</param>
        /// <returns></returns>
        public static IServiceCollection RemoveAll(this IServiceCollection collection, Type serviceType)
        {
            if (serviceType == null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            for (var i = collection.Count - 1; i >= 0; i--)
            {
                var descriptor = collection[i];
                if (descriptor.ServiceType == serviceType)
                {
                    collection.RemoveAt(i);
                }
            }

            return collection;
        }
    }
}
