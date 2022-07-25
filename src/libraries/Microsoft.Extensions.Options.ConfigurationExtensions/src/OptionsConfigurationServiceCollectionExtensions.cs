// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for adding configuration related options services to the DI container.
    /// </summary>
    public static class OptionsConfigurationServiceCollectionExtensions
    {
        /// <summary>
        /// Registers a configuration instance which TOptions will bind against.
        /// </summary>
        /// <typeparam name="TOptions">The type of options being configured.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <param name="config">The configuration being bound.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        [RequiresUnreferencedCode(OptionsBuilderConfigurationExtensions.TrimmingRequiredUnreferencedCodeMessage)]
        public static IServiceCollection Configure<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TOptions>(this IServiceCollection services, IConfiguration config) where TOptions : class
            => services.Configure<TOptions>(Options.Options.DefaultName, config);

        /// <summary>
        /// Registers a configuration instance which TOptions will bind against.
        /// </summary>
        /// <typeparam name="TOptions">The type of options being configured.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <param name="name">The name of the options instance.</param>
        /// <param name="config">The configuration being bound.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        [RequiresUnreferencedCode(OptionsBuilderConfigurationExtensions.TrimmingRequiredUnreferencedCodeMessage)]
        public static IServiceCollection Configure<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TOptions>(this IServiceCollection services, string? name, IConfiguration config) where TOptions : class
            => services.Configure<TOptions>(name, config, _ => { });

        /// <summary>
        /// Registers a configuration instance which TOptions will bind against.
        /// </summary>
        /// <typeparam name="TOptions">The type of options being configured.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <param name="config">The configuration being bound.</param>
        /// <param name="configureBinder">Used to configure the <see cref="BinderOptions"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        [RequiresUnreferencedCode(OptionsBuilderConfigurationExtensions.TrimmingRequiredUnreferencedCodeMessage)]
        public static IServiceCollection Configure<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TOptions>(this IServiceCollection services, IConfiguration config, Action<BinderOptions>? configureBinder)
            where TOptions : class
            => services.Configure<TOptions>(Options.Options.DefaultName, config, configureBinder);

        /// <summary>
        /// Registers a configuration instance which TOptions will bind against.
        /// </summary>
        /// <typeparam name="TOptions">The type of options being configured.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <param name="name">The name of the options instance.</param>
        /// <param name="config">The configuration being bound.</param>
        /// <param name="configureBinder">Used to configure the <see cref="BinderOptions"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        [RequiresUnreferencedCode(OptionsBuilderConfigurationExtensions.TrimmingRequiredUnreferencedCodeMessage)]
        public static IServiceCollection Configure<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TOptions>(this IServiceCollection services, string? name, IConfiguration config, Action<BinderOptions>? configureBinder)
            where TOptions : class
        {
            ThrowHelper.ThrowIfNull(services);
            ThrowHelper.ThrowIfNull(config);

            services.AddOptions();
            services.AddSingleton<IOptionsChangeTokenSource<TOptions>>(new ConfigurationChangeTokenSource<TOptions>(name, config));
            return services.AddSingleton<IConfigureOptions<TOptions>>(new NamedConfigureFromConfigurationOptions<TOptions>(name, config, configureBinder));
        }
    }
}
