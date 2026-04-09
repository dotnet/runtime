// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for adding hosted services to an <see cref="IServiceCollection" />.
    /// </summary>
    public static class ServiceCollectionHostedServiceExtensions
    {
        /// <summary>
        /// Add an <see cref="IHostedService"/> registration for the given type.
        /// </summary>
        /// <remarks>
        /// Note that this creates registration for <see cref="IHostedService"/> specifically. Not for the actual <c>THostedService</c> type.
        /// If you want to register the actual type, you must do so separately.
        /// </remarks>
        /// <example>
        /// <para>
        /// The following code shows how to register a hosted service while also registering the actual <c>THostedService</c> type.
        /// </para>
        /// <code language="csharp">
        /// services.AddSingleton&lt;SomeService&gt;();
        /// services.AddHostedService(sp => sp.GetRequiredService&lt;SomeService&gt;());
        /// </code>
        /// </example>
        /// <typeparam name="THostedService">An <see cref="IHostedService"/> to register.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to register with.</param>
        /// <returns>The original <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddHostedService<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THostedService>(this IServiceCollection services)
            where THostedService : class, IHostedService
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, THostedService>());

            return services;
        }

        /// <summary>
        /// Add an <see cref="IHostedService"/> registration for the given type.
        /// </summary>
        /// <remarks>
        /// Note that this creates registration for <see cref="IHostedService"/> specifically. Not for the actual <c>THostedService</c> type.
        /// If you want to register the actual type, you must do so separately.
        /// </remarks>
        /// <example>
        /// <para>
        /// The following code shows how to register a hosted service while also registering the actual <c>THostedService</c> type.
        /// </para>
        /// <code language="csharp">
        /// services.AddSingleton&lt;SomeService&gt;(implementationFactory);
        /// services.AddHostedService(sp => sp.GetRequiredService&lt;SomeService&gt;());
        /// </code>
        /// </example>
        /// <typeparam name="THostedService">An <see cref="IHostedService"/> to register.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to register with.</param>
        /// <param name="implementationFactory">A factory to create new instances of the service implementation.</param>
        /// <returns>The original <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddHostedService<THostedService>(this IServiceCollection services, Func<IServiceProvider, THostedService> implementationFactory)
            where THostedService : class, IHostedService
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService>(implementationFactory));

            return services;
        }
    }
}
