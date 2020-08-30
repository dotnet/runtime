// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for adding options services to the DI container.
    /// </summary>
    public static class OptionsServiceCollectionExtensions
    {
        /// <summary>
        /// Adds services required for using options.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddOptions(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.TryAdd(ServiceDescriptor.Singleton(typeof(IOptions<>), typeof(OptionsManager<>)));
            services.TryAdd(ServiceDescriptor.Scoped(typeof(IOptionsSnapshot<>), typeof(OptionsManager<>)));
            services.TryAdd(ServiceDescriptor.Singleton(typeof(IOptionsMonitor<>), typeof(OptionsMonitor<>)));
            services.TryAdd(ServiceDescriptor.Transient(typeof(IOptionsFactory<>), typeof(OptionsFactory<>)));
            services.TryAdd(ServiceDescriptor.Singleton(typeof(IOptionsMonitorCache<>), typeof(OptionsCache<>)));
            return services;
        }

        /// <summary>
        /// Registers an action used to configure a particular type of options.
        /// Note: These are run before all <seealso cref="PostConfigure{TOptions}(IServiceCollection, Action{TOptions})"/>.
        /// </summary>
        /// <typeparam name="TOptions">The options type to be configured.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <param name="configureOptions">The action used to configure the options.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection Configure<TOptions>(this IServiceCollection services, Action<TOptions> configureOptions) where TOptions : class
            => services.Configure(Options.Options.DefaultName, configureOptions);

        /// <summary>
        /// Registers an action used to configure a particular type of options.
        /// Note: These are run before all <seealso cref="PostConfigure{TOptions}(IServiceCollection, Action{TOptions})"/>.
        /// </summary>
        /// <typeparam name="TOptions">The options type to be configured.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <param name="name">The name of the options instance.</param>
        /// <param name="configureOptions">The action used to configure the options.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection Configure<TOptions>(this IServiceCollection services, string name, Action<TOptions> configureOptions)
            where TOptions : class
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            services.AddOptions();
            services.AddSingleton<IConfigureOptions<TOptions>>(new ConfigureNamedOptions<TOptions>(name, configureOptions));
            return services;
        }

        /// <summary>
        /// Registers an action used to configure all instances of a particular type of options.
        /// </summary>
        /// <typeparam name="TOptions">The options type to be configured.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <param name="configureOptions">The action used to configure the options.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection ConfigureAll<TOptions>(this IServiceCollection services, Action<TOptions> configureOptions) where TOptions : class
            => services.Configure(name: null, configureOptions: configureOptions);

        /// <summary>
        /// Registers an action used to initialize a particular type of options.
        /// Note: These are run after all <seealso cref="Configure{TOptions}(IServiceCollection, Action{TOptions})"/>.
        /// </summary>
        /// <typeparam name="TOptions">The options type to be configured.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <param name="configureOptions">The action used to configure the options.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection PostConfigure<TOptions>(this IServiceCollection services, Action<TOptions> configureOptions) where TOptions : class
            => services.PostConfigure(Options.Options.DefaultName, configureOptions);

        /// <summary>
        /// Registers an action used to configure a particular type of options.
        /// Note: These are run after all <seealso cref="Configure{TOptions}(IServiceCollection, Action{TOptions})"/>.
        /// </summary>
        /// <typeparam name="TOptions">The options type to be configure.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <param name="name">The name of the options instance.</param>
        /// <param name="configureOptions">The action used to configure the options.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection PostConfigure<TOptions>(this IServiceCollection services, string name, Action<TOptions> configureOptions)
            where TOptions : class
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            services.AddOptions();
            services.AddSingleton<IPostConfigureOptions<TOptions>>(new PostConfigureOptions<TOptions>(name, configureOptions));
            return services;
        }

        /// <summary>
        /// Registers an action used to post configure all instances of a particular type of options.
        /// Note: These are run after all <seealso cref="Configure{TOptions}(IServiceCollection, Action{TOptions})"/>.
        /// </summary>
        /// <typeparam name="TOptions">The options type to be configured.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <param name="configureOptions">The action used to configure the options.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection PostConfigureAll<TOptions>(this IServiceCollection services, Action<TOptions> configureOptions) where TOptions : class
            => services.PostConfigure(name: null, configureOptions: configureOptions);

        /// <summary>
        /// Registers a type that will have all of its <see cref="IConfigureOptions{TOptions}"/>,
        /// <see cref="IPostConfigureOptions{TOptions}"/>, and <see cref="IValidateOptions{TOptions}"/>
        /// registered.
        /// </summary>
        /// <typeparam name="TConfigureOptions">The type that will configure options.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection ConfigureOptions<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TConfigureOptions>(
            this IServiceCollection services)
            where TConfigureOptions : class
                => services.ConfigureOptions(typeof(TConfigureOptions));

        private static bool IsAction(Type type)
            => (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Action<>));

        private static IEnumerable<Type> FindConfigurationServices(Type type)
        {
            IEnumerable<Type> serviceTypes = type
                .GetTypeInfo()
                .ImplementedInterfaces
                .Where(t => t.GetTypeInfo().IsGenericType)
                .Where(t =>
                    t.GetGenericTypeDefinition() == typeof(IConfigureOptions<>) ||
                    t.GetGenericTypeDefinition() == typeof(IPostConfigureOptions<>) ||
                    t.GetGenericTypeDefinition() == typeof(IValidateOptions<>));
            if (!serviceTypes.Any())
            {
                throw new InvalidOperationException(
                    IsAction(type)
                    ? SR.Error_NoConfigurationServicesAndAction
                    : SR.Error_NoConfigurationServices);
            }
            return serviceTypes;
        }

        /// <summary>
        /// Registers a type that will have all of its <see cref="IConfigureOptions{TOptions}"/>,
        /// <see cref="IPostConfigureOptions{TOptions}"/>, and <see cref="IValidateOptions{TOptions}"/>
        /// registered.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <param name="configureType">The type that will configure options.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection ConfigureOptions(
            this IServiceCollection services,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type configureType)
        {
            services.AddOptions();
            IEnumerable<Type> serviceTypes = FindConfigurationServices(configureType);
            foreach (Type serviceType in serviceTypes)
            {
                services.AddTransient(serviceType, configureType);
            }
            return services;
        }

        /// <summary>
        /// Registers an object that will have all of its <see cref="IConfigureOptions{TOptions}"/>,
        /// <see cref="IPostConfigureOptions{TOptions}"/>, and <see cref="IValidateOptions{TOptions}"/>
        /// registered.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <param name="configureInstance">The instance that will configure options.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection ConfigureOptions(this IServiceCollection services, object configureInstance)
        {
            services.AddOptions();
            IEnumerable<Type> serviceTypes = FindConfigurationServices(configureInstance.GetType());
            foreach (Type serviceType in serviceTypes)
            {
                services.AddSingleton(serviceType, configureInstance);
            }
            return services;
        }

        /// <summary>
        /// Gets an options builder that forwards Configure calls for the same <typeparamref name="TOptions"/> to the underlying service collection.
        /// </summary>
        /// <typeparam name="TOptions">The options type to be configured.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <returns>The <see cref="OptionsBuilder{TOptions}"/> so that configure calls can be chained in it.</returns>
        public static OptionsBuilder<TOptions> AddOptions<TOptions>(this IServiceCollection services) where TOptions : class
            => services.AddOptions<TOptions>(Options.Options.DefaultName);

        /// <summary>
        /// Gets an options builder that forwards Configure calls for the same named <typeparamref name="TOptions"/> to the underlying service collection.
        /// </summary>
        /// <typeparam name="TOptions">The options type to be configured.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <param name="name">The name of the options instance.</param>
        /// <returns>The <see cref="OptionsBuilder{TOptions}"/> so that configure calls can be chained in it.</returns>
        public static OptionsBuilder<TOptions> AddOptions<TOptions>(this IServiceCollection services, string name)
            where TOptions : class
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddOptions();
            return new OptionsBuilder<TOptions>(services, name);
        }
    }
}
