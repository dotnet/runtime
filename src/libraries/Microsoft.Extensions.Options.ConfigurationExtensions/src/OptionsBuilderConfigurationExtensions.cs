// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for adding configuration related options services to the DI container via <see cref="OptionsBuilder{TOptions}"/>.
    /// </summary>
    public static class OptionsBuilderConfigurationExtensions
    {
        internal const string TrimmingRequiredUnreferencedCodeMessage = "TOptions's dependent types may have their members trimmed. Ensure all required members are preserved.";

        /// <summary>
        /// Registers a configuration instance which <typeparamref name="TOptions"/> will bind against.
        /// </summary>
        /// <typeparam name="TOptions">The options type to be configured.</typeparam>
        /// <param name="optionsBuilder">The options builder to add the services to.</param>
        /// <param name="config">The configuration being bound.</param>
        /// <returns>The <see cref="OptionsBuilder{TOptions}"/> so that additional calls can be chained.</returns>
        [RequiresUnreferencedCode(TrimmingRequiredUnreferencedCodeMessage)]
        public static OptionsBuilder<TOptions> Bind<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TOptions>(this OptionsBuilder<TOptions> optionsBuilder, IConfiguration config) where TOptions : class
            => optionsBuilder.Bind(config, _ => { });

        /// <summary>
        /// Registers a configuration instance which <typeparamref name="TOptions"/> will bind against.
        /// </summary>
        /// <typeparam name="TOptions">The options type to be configured.</typeparam>
        /// <param name="optionsBuilder">The options builder to add the services to.</param>
        /// <param name="config">The configuration being bound.</param>
        /// <param name="configureBinder">Used to configure the <see cref="BinderOptions"/>.</param>
        /// <returns>The <see cref="OptionsBuilder{TOptions}"/> so that additional calls can be chained.</returns>
        [RequiresUnreferencedCode(TrimmingRequiredUnreferencedCodeMessage)]
        public static OptionsBuilder<TOptions> Bind<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TOptions>(this OptionsBuilder<TOptions> optionsBuilder, IConfiguration config, Action<BinderOptions> configureBinder) where TOptions : class
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
        /// <param name="configSectionPath">The name of the configuration section to bind from.</param>
        /// <param name="configureBinder">Optional. Used to configure the <see cref="BinderOptions"/>.</param>
        /// <returns>The <see cref="OptionsBuilder{TOptions}"/> so that additional calls can be chained.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="optionsBuilder"/> or <paramref name="configSectionPath" /> is <see langword="null"/>.
        /// </exception>
        /// <seealso cref="Bind{TOptions}(OptionsBuilder{TOptions}, IConfiguration, Action{BinderOptions})"/>
        [RequiresUnreferencedCode(TrimmingRequiredUnreferencedCodeMessage)]
        public static OptionsBuilder<TOptions> BindConfiguration<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TOptions>(
            this OptionsBuilder<TOptions> optionsBuilder,
            string configSectionPath,
            Action<BinderOptions> configureBinder = null)
            where TOptions : class
        {
            _ = optionsBuilder ?? throw new ArgumentNullException(nameof(optionsBuilder));
            _ = configSectionPath ?? throw new ArgumentNullException(nameof(configSectionPath));

            optionsBuilder.Configure<IConfiguration>((opts, config) => BindFromOptions<TOptions>(opts, config, configSectionPath, configureBinder));
            return optionsBuilder;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "The only call to this method is in BindConfiguration method which is already annotated as RequiresUnreferencedCode.")]
        private static void BindFromOptions<TOptions>(TOptions opts, IConfiguration config, string configSectionPath, Action<BinderOptions> configureBinder) where TOptions : class
        {
            IConfiguration section = string.Equals("", configSectionPath, StringComparison.OrdinalIgnoreCase)
                ? config
                : config.GetSection(configSectionPath);
            section.Bind(opts, configureBinder);
        }
    }
}
