// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for adding service decorations to an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionDecorationExtensions
    {

        /// <summary>
        /// Decorates all registrations of the type specified in <typeparamref name="TService"/> with
        /// the decorator type specified in <typeparamref name="TDecorator"/>.
        /// </summary>
        /// <typeparam name="TService">The type of the service to decorate.</typeparam>
        /// <typeparam name="TDecorator">The type of the decorator.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the decoration to.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        /// <exception cref="InvalidOperationException">No service of type <typeparamref name="TService"/> has been registered.</exception>
        public static IServiceCollection Decorate<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TDecorator>(
            this IServiceCollection services)
            where TDecorator : TService
        {
            return Decorate(services, typeof(TService), typeof(TDecorator));
        }

        /// <summary>
        /// Decorates all registrations of the specified <paramref name="serviceType"/> with
        /// the specified <paramref name="decoratorType"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the decoration to.</param>
        /// <param name="serviceType">The type of the service to decorate.</param>
        /// <param name="decoratorType">The type of the decorator.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        /// <exception cref="InvalidOperationException">No service of type <paramref name="serviceType"/> has been registered.</exception>
        public static IServiceCollection Decorate(
            this IServiceCollection services,
            Type serviceType,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type decoratorType)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(serviceType);
            ArgumentNullException.ThrowIfNull(decoratorType);

            if (!TryDecorateCore(services, serviceType, serviceKey: null, decoratorType, decoratorFactory: null))
            {
                throw new InvalidOperationException(SR.Format(SR.NoServiceRegistered, serviceType));
            }

            return services;
        }

        /// <summary>
        /// Decorates all registrations of the type specified in <typeparamref name="TService"/> using
        /// the specified factory.
        /// </summary>
        /// <typeparam name="TService">The type of the service to decorate.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the decoration to.</param>
        /// <param name="decorator">A factory that creates the decorator, given the inner service and service provider.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        /// <exception cref="InvalidOperationException">No service of type <typeparamref name="TService"/> has been registered.</exception>
        public static IServiceCollection Decorate<TService>(
            this IServiceCollection services,
            Func<TService, IServiceProvider, TService> decorator)
            where TService : class
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(decorator);

            if (!TryDecorateCore(services, typeof(TService), serviceKey: null, decoratorType: null, (sp, inner) => decorator((TService)inner, sp)!))
            {
                throw new InvalidOperationException(SR.Format(SR.NoServiceRegistered, typeof(TService)));
            }

            return services;
        }

        /// <summary>
        /// Decorates all registrations of the specified <paramref name="serviceType"/> using
        /// the specified factory.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the decoration to.</param>
        /// <param name="serviceType">The type of the service to decorate.</param>
        /// <param name="decorator">A factory that creates the decorator, given the service provider and the inner service instance.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        /// <exception cref="InvalidOperationException">No service of type <paramref name="serviceType"/> has been registered.</exception>
        public static IServiceCollection Decorate(
            this IServiceCollection services,
            Type serviceType,
            Func<IServiceProvider, object, object> decorator)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(serviceType);
            ArgumentNullException.ThrowIfNull(decorator);

            if (!TryDecorateCore(services, serviceType, serviceKey: null, decoratorType: null, decorator))
            {
                throw new InvalidOperationException(SR.Format(SR.NoServiceRegistered, serviceType));
            }

            return services;
        }

        /// <summary>
        /// Decorates all registrations of the type specified in <typeparamref name="TService"/> with
        /// the decorator type specified in <typeparamref name="TDecorator"/>, if any exist.
        /// </summary>
        /// <typeparam name="TService">The type of the service to decorate.</typeparam>
        /// <typeparam name="TDecorator">The type of the decorator.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the decoration to.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection TryDecorate<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TDecorator>(
            this IServiceCollection services)
            where TDecorator : TService
        {
            return TryDecorate(services, typeof(TService), typeof(TDecorator));
        }

        /// <summary>
        /// Decorates all registrations of the specified <paramref name="serviceType"/> with
        /// the specified <paramref name="decoratorType"/>, if any exist.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the decoration to.</param>
        /// <param name="serviceType">The type of the service to decorate.</param>
        /// <param name="decoratorType">The type of the decorator.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection TryDecorate(
            this IServiceCollection services,
            Type serviceType,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type decoratorType)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(serviceType);
            ArgumentNullException.ThrowIfNull(decoratorType);

            TryDecorateCore(services, serviceType, serviceKey: null, decoratorType, decoratorFactory: null);
            return services;
        }

        /// <summary>
        /// Decorates all registrations of the type specified in <typeparamref name="TService"/> using
        /// the specified factory, if any exist.
        /// </summary>
        /// <typeparam name="TService">The type of the service to decorate.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the decoration to.</param>
        /// <param name="decorator">A factory that creates the decorator, given the inner service and service provider.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection TryDecorate<TService>(
            this IServiceCollection services,
            Func<TService, IServiceProvider, TService> decorator)
            where TService : class
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(decorator);

            TryDecorateCore(services, typeof(TService), serviceKey: null, decoratorType: null, (sp, inner) => decorator((TService)inner, sp)!);
            return services;
        }

        /// <summary>
        /// Decorates all registrations of the specified <paramref name="serviceType"/> using
        /// the specified factory, if any exist.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the decoration to.</param>
        /// <param name="serviceType">The type of the service to decorate.</param>
        /// <param name="decorator">A factory that creates the decorator, given the service provider and the inner service instance.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection TryDecorate(
            this IServiceCollection services,
            Type serviceType,
            Func<IServiceProvider, object, object> decorator)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(serviceType);
            ArgumentNullException.ThrowIfNull(decorator);

            TryDecorateCore(services, serviceType, serviceKey: null, decoratorType: null, decorator);
            return services;
        }

        // --- Keyed variants ---

        /// <summary>
        /// Decorates all registrations of the type specified in <typeparamref name="TService"/> with
        /// the specified <paramref name="serviceKey"/> using the decorator type specified in
        /// <typeparamref name="TDecorator"/>.
        /// </summary>
        /// <typeparam name="TService">The type of the service to decorate.</typeparam>
        /// <typeparam name="TDecorator">The type of the decorator.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the decoration to.</param>
        /// <param name="serviceKey">The key of the service to decorate.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        /// <exception cref="InvalidOperationException">No keyed service of type <typeparamref name="TService"/> has been registered with the specified key.</exception>
        public static IServiceCollection DecorateKeyed<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TDecorator>(
            this IServiceCollection services,
            object? serviceKey)
            where TDecorator : TService
        {
            return DecorateKeyed(services, typeof(TService), serviceKey, typeof(TDecorator));
        }

        /// <summary>
        /// Decorates all registrations of the specified <paramref name="serviceType"/> with
        /// the specified <paramref name="serviceKey"/> using the specified <paramref name="decoratorType"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the decoration to.</param>
        /// <param name="serviceType">The type of the service to decorate.</param>
        /// <param name="serviceKey">The key of the service to decorate.</param>
        /// <param name="decoratorType">The type of the decorator.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        /// <exception cref="InvalidOperationException">No keyed service of type <paramref name="serviceType"/> has been registered with the specified key.</exception>
        public static IServiceCollection DecorateKeyed(
            this IServiceCollection services,
            Type serviceType,
            object? serviceKey,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type decoratorType)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(serviceType);
            ArgumentNullException.ThrowIfNull(decoratorType);

            if (!TryDecorateCore(services, serviceType, serviceKey, decoratorType, decoratorFactory: null))
            {
                throw new InvalidOperationException(SR.Format(SR.NoServiceRegistered, serviceType));
            }

            return services;
        }

        /// <summary>
        /// Decorates all registrations of the type specified in <typeparamref name="TService"/> with
        /// the specified <paramref name="serviceKey"/> using the specified factory.
        /// </summary>
        /// <typeparam name="TService">The type of the service to decorate.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the decoration to.</param>
        /// <param name="serviceKey">The key of the service to decorate.</param>
        /// <param name="decorator">A factory that creates the decorator, given the inner service and service provider.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        /// <exception cref="InvalidOperationException">No keyed service of type <typeparamref name="TService"/> has been registered with the specified key.</exception>
        public static IServiceCollection DecorateKeyed<TService>(
            this IServiceCollection services,
            object? serviceKey,
            Func<TService, IServiceProvider, TService> decorator)
            where TService : class
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(decorator);

            if (!TryDecorateCore(services, typeof(TService), serviceKey, decoratorType: null, (sp, inner) => decorator((TService)inner, sp)!))
            {
                throw new InvalidOperationException(SR.Format(SR.NoServiceRegistered, typeof(TService)));
            }

            return services;
        }

        /// <summary>
        /// Decorates all registrations of the type specified in <typeparamref name="TService"/> with
        /// the specified <paramref name="serviceKey"/> using the decorator type specified in
        /// <typeparamref name="TDecorator"/>, if any exist.
        /// </summary>
        /// <typeparam name="TService">The type of the service to decorate.</typeparam>
        /// <typeparam name="TDecorator">The type of the decorator.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the decoration to.</param>
        /// <param name="serviceKey">The key of the service to decorate.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection TryDecorateKeyed<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TDecorator>(
            this IServiceCollection services,
            object? serviceKey)
            where TDecorator : TService
        {
            return TryDecorateKeyed(services, typeof(TService), serviceKey, typeof(TDecorator));
        }

        /// <summary>
        /// Decorates all registrations of the specified <paramref name="serviceType"/> with
        /// the specified <paramref name="serviceKey"/> using the specified <paramref name="decoratorType"/>,
        /// if any exist.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the decoration to.</param>
        /// <param name="serviceType">The type of the service to decorate.</param>
        /// <param name="serviceKey">The key of the service to decorate.</param>
        /// <param name="decoratorType">The type of the decorator.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection TryDecorateKeyed(
            this IServiceCollection services,
            Type serviceType,
            object? serviceKey,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type decoratorType)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(serviceType);
            ArgumentNullException.ThrowIfNull(decoratorType);

            TryDecorateCore(services, serviceType, serviceKey, decoratorType, decoratorFactory: null);
            return services;
        }

        /// <summary>
        /// Decorates all registrations of the type specified in <typeparamref name="TService"/> with
        /// the specified <paramref name="serviceKey"/> using the specified factory, if any exist.
        /// </summary>
        /// <typeparam name="TService">The type of the service to decorate.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the decoration to.</param>
        /// <param name="serviceKey">The key of the service to decorate.</param>
        /// <param name="decorator">A factory that creates the decorator, given the inner service and service provider.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection TryDecorateKeyed<TService>(
            this IServiceCollection services,
            object? serviceKey,
            Func<TService, IServiceProvider, TService> decorator)
            where TService : class
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(decorator);

            TryDecorateCore(services, typeof(TService), serviceKey, decoratorType: null, (sp, inner) => decorator((TService)inner, sp)!);
            return services;
        }

        // --- Helpers ---

        /// <summary>
        /// Finds all <see cref="ServiceDescriptor"/> entries in the collection that match the
        /// specified <paramref name="decoration"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to search.</param>
        /// <param name="decoration">The decoration to match against.</param>
        /// <returns>An enumerable of matching <see cref="ServiceDescriptor"/> entries.</returns>
        public static IEnumerable<ServiceDescriptor> FindMatchingDescriptors(
            this IServiceCollection services,
            ServiceDecoration decoration)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(decoration);

            for (int i = 0; i < services.Count; i++)
            {
                ServiceDescriptor descriptor = services[i];

                if (IsMatch(descriptor, decoration))
                {
                    yield return descriptor;
                }
            }
        }

        private static bool TryDecorateCore(
            IServiceCollection services,
            Type serviceType,
            object? serviceKey,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type? decoratorType,
            Func<IServiceProvider, object, object>? decoratorFactory)
        {
            // For non-open-generic decorations, check that at least one matching descriptor exists
            if (!serviceType.IsGenericTypeDefinition)
            {
                bool hasMatch = false;
                for (int i = 0; i < services.Count; i++)
                {
                    ServiceDescriptor descriptor = services[i];
                    if (descriptor.ServiceType == serviceType && object.Equals(descriptor.ServiceKey, serviceKey))
                    {
                        hasMatch = true;
                        break;
                    }
                }

                if (!hasMatch)
                {
                    return false;
                }
            }

            ServiceDecoration decoration = decoratorType is not null
                ? new ServiceDecoration(serviceType, serviceKey, decoratorType)
                : new ServiceDecoration(serviceType, serviceKey, decoratorFactory!);

            GetDecorations(services).Add(decoration);

            return true;
        }

        internal static IList<ServiceDecoration> GetDecorations(IServiceCollection services)
        {
            if (services is IDecorationServiceCollection decorationCollection)
            {
                return decorationCollection.Decorations;
            }

            throw new InvalidOperationException(SR.Format(SR.ServiceCollectionDoesNotSupportDecoration, services.GetType()));
        }

        private static bool IsMatch(ServiceDescriptor descriptor, ServiceDecoration decoration)
        {
            if (!object.Equals(descriptor.ServiceKey, decoration.ServiceKey))
            {
                return false;
            }

            if (decoration.ServiceType.IsGenericTypeDefinition)
            {
                return descriptor.ServiceType.IsGenericType
                    && descriptor.ServiceType.GetGenericTypeDefinition() == decoration.ServiceType;
            }

            return descriptor.ServiceType == decoration.ServiceType;
        }
    }
}
