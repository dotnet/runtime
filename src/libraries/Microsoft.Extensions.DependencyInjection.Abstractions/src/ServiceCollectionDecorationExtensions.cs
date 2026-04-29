// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
        public static IServiceCollection Decorate(
            this IServiceCollection services,
            Type serviceType,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type decoratorType)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(serviceType);
            ArgumentNullException.ThrowIfNull(decoratorType);

            AddDecoration(services, new ServiceDecoration(serviceType, decoratorType));
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
        public static IServiceCollection Decorate<TService>(
            this IServiceCollection services,
            Func<TService, IServiceProvider, TService> decorator)
            where TService : class
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(decorator);

            AddDecoration(services, new ServiceDecoration(typeof(TService), (sp, inner) => decorator((TService)inner, sp)!));
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
        public static IServiceCollection Decorate(
            this IServiceCollection services,
            Type serviceType,
            Func<IServiceProvider, object, object> decorator)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(serviceType);
            ArgumentNullException.ThrowIfNull(decorator);

            AddDecoration(services, new ServiceDecoration(serviceType, decorator));
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
        public static IServiceCollection DecorateKeyed(
            this IServiceCollection services,
            Type serviceType,
            object? serviceKey,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type decoratorType)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(serviceType);
            ArgumentNullException.ThrowIfNull(decoratorType);

            AddDecoration(services, new ServiceDecoration(serviceType, serviceKey, decoratorType));
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
        public static IServiceCollection DecorateKeyed<TService>(
            this IServiceCollection services,
            object? serviceKey,
            Func<TService, IServiceProvider, TService> decorator)
            where TService : class
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(decorator);

            AddDecoration(services, new ServiceDecoration(typeof(TService), serviceKey, (sp, inner) => decorator((TService)inner, sp)!));
            return services;
        }

        private static void AddDecoration(IServiceCollection services, ServiceDecoration decoration)
        {
            if (services is IDecorationServiceCollection decorationCollection)
            {
                decorationCollection.Decorations.Add(decoration);
                return;
            }

            throw new InvalidOperationException(SR.Format(SR.ServiceCollectionDoesNotSupportDecoration, services.GetType()));
        }

    }
}
