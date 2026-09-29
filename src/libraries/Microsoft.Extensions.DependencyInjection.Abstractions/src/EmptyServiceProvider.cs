// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides an <see cref="IServiceProvider"/> that contains no application services.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Like the <see cref="IServiceProvider"/> returned by <c>new ServiceCollection().BuildServiceProvider()</c>,
    /// this provider still exposes the built-in services (<see cref="IServiceProvider"/>,
    /// <see cref="IServiceScopeFactory"/>, <see cref="IServiceProviderIsService"/> and
    /// <see cref="IServiceProviderIsKeyedService"/>) and resolves requests for
    /// <see cref="IEnumerable{T}"/> to an empty sequence.
    /// </para>
    /// <para>
    /// Use <see cref="Instance"/> to access the shared singleton instead of writing a custom empty provider.
    /// </para>
    /// </remarks>
    public sealed class EmptyServiceProvider : IKeyedServiceProvider, IServiceProviderIsKeyedService
    {
        private EmptyServiceProvider()
        {
        }

        /// <summary>
        /// Gets the shared <see cref="EmptyServiceProvider"/> instance.
        /// </summary>
        public static EmptyServiceProvider Instance { get; } = new EmptyServiceProvider();

        /// <inheritdoc />
        object? IServiceProvider.GetService(Type serviceType) => GetService(serviceType);

        /// <inheritdoc />
        object? IKeyedServiceProvider.GetKeyedService(Type serviceType, object? serviceKey) => GetKeyedService(serviceType, serviceKey);

        /// <inheritdoc />
        object IKeyedServiceProvider.GetRequiredKeyedService(Type serviceType, object? serviceKey)
        {
            object? service = GetKeyedService(serviceType, serviceKey);
            if (service is null)
            {
                ThrowNoServiceRegistered(serviceType, serviceKey);
            }

            return service;
        }

        /// <inheritdoc />
        bool IServiceProviderIsService.IsService(Type serviceType)
        {
            ArgumentNullException.ThrowIfNull(serviceType);

            return !serviceType.IsGenericTypeDefinition && (IsEnumerable(serviceType) || IsBuiltInService(serviceType));
        }

        /// <inheritdoc />
        bool IServiceProviderIsKeyedService.IsKeyedService(Type serviceType, object? serviceKey)
        {
            ArgumentNullException.ThrowIfNull(serviceType);

            if (serviceType.IsGenericTypeDefinition)
            {
                return false;
            }

            return IsEnumerable(serviceType) || (serviceKey is null && IsBuiltInService(serviceType));
        }

        private object? GetService(Type serviceType)
        {
            ArgumentNullException.ThrowIfNull(serviceType);

            if (serviceType == typeof(IServiceProvider) ||
                serviceType == typeof(IServiceProviderIsService) ||
                serviceType == typeof(IServiceProviderIsKeyedService))
            {
                return this;
            }

            if (serviceType == typeof(IServiceScopeFactory))
            {
                return EmptyServiceScope.Instance;
            }

            return IsEnumerable(serviceType) ? CreateEmptyEnumerable(serviceType) : null;
        }

        private object? GetKeyedService(Type serviceType, object? serviceKey)
        {
            ArgumentNullException.ThrowIfNull(serviceType);

            if (ReferenceEquals(serviceKey, KeyedService.AnyKey) &&
                (!serviceType.IsGenericType || serviceType.GetGenericTypeDefinition() != typeof(IEnumerable<>)))
            {
                throw new InvalidOperationException(SR.Format(SR.KeyedServiceAnyKeyUsedToResolveService, nameof(IServiceProvider), nameof(IServiceScopeFactory)));
            }

            if (serviceKey is null)
            {
                return GetService(serviceType);
            }

            return IsEnumerable(serviceType) ? CreateEmptyEnumerable(serviceType) : null;
        }

        private static bool IsBuiltInService(Type serviceType) =>
            serviceType == typeof(IServiceProvider) ||
            serviceType == typeof(IServiceScopeFactory) ||
            serviceType == typeof(IServiceProviderIsService) ||
            serviceType == typeof(IServiceProviderIsKeyedService);

        private static bool IsEnumerable(Type serviceType) =>
            serviceType.IsConstructedGenericType && serviceType.GetGenericTypeDefinition() == typeof(IEnumerable<>);

        // Wraps RuntimeFeature.IsDynamicCodeSupported across target frameworks.
        private static bool IsDynamicCodeSupported =>
#if NETFRAMEWORK || NETSTANDARD2_0
            true;
#else
            RuntimeFeature.IsDynamicCodeSupported;
#endif

        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "The element type is guaranteed not to be a ValueType when dynamic code isn't supported.")]
        private static Array CreateEmptyEnumerable(Type serviceType)
        {
            Type itemType = serviceType.GenericTypeArguments[0];
            if (!IsDynamicCodeSupported && itemType.IsValueType)
            {
                // Native AOT apps are not able to make an Enumerable of a ValueType service
                // since there is no guarantee the ValueType[] code has been generated.
                throw new InvalidOperationException(SR.Format(SR.AotCannotCreateEnumerableValueType, itemType));
            }

            return Array.CreateInstance(itemType, 0);
        }

        [DoesNotReturn]
        private static void ThrowNoServiceRegistered(Type serviceType, object? serviceKey)
        {
            throw serviceKey is null
                ? new InvalidOperationException(SR.Format(SR.NoServiceRegistered, serviceType))
                : new InvalidOperationException(SR.Format(SR.NoKeyedServiceRegistered, serviceType, serviceKey.GetType()));
        }

        private sealed class EmptyServiceScope : IServiceScopeFactory, IServiceScope
        {
            public static EmptyServiceScope Instance { get; } = new EmptyServiceScope();

            public IServiceProvider ServiceProvider => EmptyServiceProvider.Instance;

            public IServiceScope CreateScope() => this;

            public void Dispose()
            {
            }
        }
    }
}
