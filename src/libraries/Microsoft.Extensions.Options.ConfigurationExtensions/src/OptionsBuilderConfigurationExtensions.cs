// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for adding configuration related options services to the DI container via <see cref="OptionsBuilder{TOptions}"/>.
    /// </summary>
    public static class OptionsBuilderConfigurationExtensions
    {
        /// <summary>
        /// Registers a configuration instance which <typeparamref name="TOptions"/> will bind against.
        /// </summary>
        /// <typeparam name="TOptions">The options type to be configured.</typeparam>
        /// <param name="optionsBuilder">The options builder to add the services to.</param>
        /// <param name="config">The configuration being bound.</param>
        /// <returns>The <see cref="OptionsBuilder{TOptions}"/> so that additional calls can be chained.</returns>
        public static OptionsBuilder<TOptions> Bind<TOptions>(this OptionsBuilder<TOptions> optionsBuilder, IConfiguration config) where TOptions : class
            => optionsBuilder.Bind(config, _ => { });

        /// <summary>
        /// Registers a configuration instance which <typeparamref name="TOptions"/> will bind against.
        /// </summary>
        /// <typeparam name="TOptions">The options type to be configured.</typeparam>
        /// <param name="optionsBuilder">The options builder to add the services to.</param>
        /// <param name="config">The configuration being bound.</param>
        /// <param name="configureBinder">Used to configure the <see cref="BinderOptions"/>.</param>
        /// <returns>The <see cref="OptionsBuilder{TOptions}"/> so that additional calls can be chained.</returns>
        public static OptionsBuilder<TOptions> Bind<TOptions>(this OptionsBuilder<TOptions> optionsBuilder, IConfiguration config, Action<BinderOptions> configureBinder) where TOptions : class
        {
            if (optionsBuilder == null)
            {
                throw new ArgumentNullException(nameof(optionsBuilder));
            }

            optionsBuilder.Services.Configure<TOptions>(optionsBuilder.Name, config, configureBinder);
            return optionsBuilder;
        }

        /// <summary>
        /// Registers the dependency injection container to bind <typeparamref name="TOptions"/> against
        /// the <see cref="IConfiguration"/> obtained from the DI service provider.
        /// </summary>
        /// <typeparam name="TOptions">The options type to be configured.</typeparam>
        /// <param name="optionsBuilder">The options builder to add the services to.</param>
        /// <param name="sectionName">
        /// The name of the configuration section to bind from. If omitted, or
        /// <see langword="null"/> the name of the <paramref name="optionsBuilder"/>
        /// is used.
        /// </param>
        /// <param name="configureBinder">Optional. Used to configure the <see cref="BinderOptions"/>.</param>
        /// <returns>The <see cref="OptionsBuilder{TOptions}"/> so that additional calls can be chained.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="optionsBuilder"/> is <see langword="null"/>.
        /// </exception>
        /// <seealso cref="Bind{TOptions}(OptionsBuilder{TOptions}, IConfiguration, Action{BinderOptions})"/>
        public static OptionsBuilder<TOptions> BindConfiguration<TOptions>(
            this OptionsBuilder<TOptions> optionsBuilder,
            string sectionName = null,
            Action<BinderOptions> configureBinder = null)
            where TOptions : class
        {
            _ = optionsBuilder ?? throw new ArgumentNullException(nameof(optionsBuilder));

            optionsBuilder.Configure<IConfiguration>((opts, config) =>
            {
                IConfiguration section = sectionName switch
                {
                    string nonEmptySectionName
                    when !string.IsNullOrEmpty(nonEmptySectionName) =>
                        config?.GetSection(nonEmptySectionName),
                    null
                    when optionsBuilder.Name is string builderName &&
                    !(string.IsNullOrEmpty(builderName)) =>
                        config?.GetSection(builderName),
                    _ => config,
                };
                config?.Bind(opts, configureBinder);
            });
            return optionsBuilder;
        }
    }
}
