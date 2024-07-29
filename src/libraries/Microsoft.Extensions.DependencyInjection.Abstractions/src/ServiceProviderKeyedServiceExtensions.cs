// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for getting services from an <see cref="IServiceProvider" />.
    /// </summary>
    public static class ServiceProviderKeyedServiceExtensions
    {
        /// <summary>
        /// Get service of type <typeparamref name="T"/> from the <see cref="IServiceProvider"/>.
        /// </summary>
        /// <typeparam name="T">The type of service object to get.</typeparam>
        /// <param name="provider">The <see cref="IServiceProvider"/> to retrieve the service object from.</param>
        /// <param name="serviceKey">An object that specifies the key of service object to get.</param>
        /// <returns>A service object of type <typeparamref name="T"/> or null if there is no such service.</returns>
        public static T? GetKeyedService<T>(this IServiceProvider provider, object? serviceKey)
        {
            ThrowHelper.ThrowIfNull(provider);

            if (provider is IKeyedServiceProvider keyedServiceProvider)
            {
                return (T?)keyedServiceProvider.GetKeyedService(typeof(T), serviceKey);
            }

            throw new InvalidOperationException(SR.KeyedServicesNotSupported);
        }

        /// <summary>
        /// Get service of type <paramref name="serviceType"/> from the <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="provider">The <see cref="IServiceProvider"/> to retrieve the service object from.</param>
        /// <param name="serviceType">An object that specifies the type of service object to get.</param>
        /// <param name="serviceKey">An object that specifies the key of service object to get.</param>
        /// <returns>A service object of type <paramref name="serviceType"/>.</returns>
        /// <exception cref="System.InvalidOperationException">There is no service of type <paramref name="serviceType"/>.</exception>
        public static object GetRequiredKeyedService(this IServiceProvider provider, Type serviceType, object? serviceKey)
        {
            ThrowHelper.ThrowIfNull(provider);
            ThrowHelper.ThrowIfNull(serviceType);

            if (provider is IKeyedServiceProvider requiredServiceSupportingProvider)
            {
                return requiredServiceSupportingProvider.GetRequiredKeyedService(serviceType, serviceKey);
            }

            throw new InvalidOperationException(SR.KeyedServicesNotSupported);
        }

        /// <summary>
        /// Get service of type <typeparamref name="T"/> from the <see cref="IServiceProvider"/>.
        /// </summary>
        /// <typeparam name="T">The type of service object to get.</typeparam>
        /// <param name="provider">The <see cref="IServiceProvider"/> to retrieve the service object from.</param>
        /// <param name="serviceKey">An object that specifies the key of service object to get.</param>
        /// <returns>A service object of type <typeparamref name="T"/>.</returns>
        /// <exception cref="System.InvalidOperationException">There is no service of type <typeparamref name="T"/>.</exception>
        public static T GetRequiredKeyedService<T>(this IServiceProvider provider, object? serviceKey) where T : notnull
        {
            ThrowHelper.ThrowIfNull(provider);

            return (T)provider.GetRequiredKeyedService(typeof(T), serviceKey);
        }

        /// <summary>
        /// Get an enumeration of services of type <typeparamref name="T"/> from the <see cref="IServiceProvider"/>.
        /// </summary>
        /// <typeparam name="T">The type of service object to get.</typeparam>
        /// <param name="provider">The <see cref="IServiceProvider"/> to retrieve the services from.</param>
        /// <param name="serviceKey">An object that specifies the key of service object to get.</param>
        /// <returns>An enumeration of services of type <typeparamref name="T"/>.</returns>
        public static IEnumerable<T> GetKeyedServices<T>(this IServiceProvider provider, object? serviceKey)
        {
            ThrowHelper.ThrowIfNull(provider);

            return provider.GetRequiredKeyedService<IEnumerable<T>>(serviceKey);
        }

        /// <summary>
        /// Get an enumeration of services of type <paramref name="serviceType"/> from the <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="provider">The <see cref="IServiceProvider"/> to retrieve the services from.</param>
        /// <param name="serviceType">An object that specifies the type of service object to get.</param>
        /// <param name="serviceKey">An object that specifies the key of service object to get.</param>
        /// <returns>An enumeration of services of type <paramref name="serviceType"/>.</returns>
        [RequiresDynamicCode("The native code for an IEnumerable<serviceType> might not be available at runtime.")]
        public static IEnumerable<object?> GetKeyedServices(this IServiceProvider provider, Type serviceType, object? serviceKey)
        {
            ThrowHelper.ThrowIfNull(provider);
            ThrowHelper.ThrowIfNull(serviceType);

            Type? genericEnumerable = typeof(IEnumerable<>).MakeGenericType(serviceType);
            return (IEnumerable<object>)provider.GetRequiredKeyedService(genericEnumerable, serviceKey);
        }
    }
}
