// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using ThrowHelper = System.ThrowHelper;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Provides extension methods for the <see cref="ILoggingBuilder"/> and <see cref="ILoggerProviderConfiguration{ConsoleLoggerProvider}"/> classes.
    /// </summary>
    [UnsupportedOSPlatform("browser")]
    public static partial class ConsoleLoggerExtensions
    {
        internal const string RequiresDynamicCodeMessage = "Binding TOptions to configuration values may require generating dynamic code at runtime.";
        internal const string TrimmingRequiresUnreferencedCodeMessage = "TOptions's dependent types may have their members trimmed. Ensure all required members are preserved.";

        /// <summary>
        /// Adds a console logger named 'Console' to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        public static ILoggingBuilder AddConsole(this ILoggingBuilder builder)
        {
            builder.AddConfiguration();

            builder.AddConsoleFormatter<JsonConsoleFormatter, JsonConsoleFormatterOptions, ConsoleFormatterConfigureOptions>();
            builder.AddConsoleFormatter<SystemdConsoleFormatter, ConsoleFormatterOptions, ConsoleFormatterConfigureOptions>();
            builder.AddConsoleFormatter<SimpleConsoleFormatter, SimpleConsoleFormatterOptions, ConsoleFormatterConfigureOptions>();

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, ConsoleLoggerProvider>());

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<ConsoleLoggerOptions>, ConsoleLoggerConfigureOptions>());
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IOptionsChangeTokenSource<ConsoleLoggerOptions>, LoggerProviderOptionsChangeTokenSource<ConsoleLoggerOptions, ConsoleLoggerProvider>>());

            return builder;
        }

        /// <summary>
        /// Adds a console logger named 'Console' to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        /// <param name="configure">A delegate to configure the <see cref="ConsoleLogger"/>.</param>
        public static ILoggingBuilder AddConsole(this ILoggingBuilder builder, Action<ConsoleLoggerOptions> configure)
        {
            ThrowHelper.ThrowIfNull(configure);

            builder.AddConsole();
            builder.Services.Configure(configure);

            return builder;
        }

        /// <summary>
        /// Add the default console log formatter named 'simple' to the factory with default properties.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        public static ILoggingBuilder AddSimpleConsole(this ILoggingBuilder builder) =>
            builder.AddFormatterWithName(ConsoleFormatterNames.Simple);

        /// <summary>
        /// Add and configure a console log formatter named 'simple' to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        /// <param name="configure">A delegate to configure the <see cref="ConsoleLogger"/> options for the built-in default log formatter.</param>
        public static ILoggingBuilder AddSimpleConsole(this ILoggingBuilder builder, Action<SimpleConsoleFormatterOptions> configure)
        {
            return builder.AddConsoleWithFormatter<SimpleConsoleFormatterOptions>(ConsoleFormatterNames.Simple, configure);
        }

        /// <summary>
        /// Add a console log formatter named 'json' to the factory with default properties.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        public static ILoggingBuilder AddJsonConsole(this ILoggingBuilder builder) =>
            builder.AddFormatterWithName(ConsoleFormatterNames.Json);

        /// <summary>
        /// Add and configure a console log formatter named 'json' to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        /// <param name="configure">A delegate to configure the <see cref="ConsoleLogger"/> options for the built-in json log formatter.</param>
        public static ILoggingBuilder AddJsonConsole(this ILoggingBuilder builder, Action<JsonConsoleFormatterOptions> configure)
        {
            return builder.AddConsoleWithFormatter<JsonConsoleFormatterOptions>(ConsoleFormatterNames.Json, configure);
        }

        /// <summary>
        /// Add and configure a console log formatter named 'systemd' to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        /// <param name="configure">A delegate to configure the <see cref="ConsoleLogger"/> options for the built-in systemd log formatter.</param>
        public static ILoggingBuilder AddSystemdConsole(this ILoggingBuilder builder, Action<ConsoleFormatterOptions> configure)
        {
            return builder.AddConsoleWithFormatter<ConsoleFormatterOptions>(ConsoleFormatterNames.Systemd, configure);
        }

        /// <summary>
        /// Add a console log formatter named 'systemd' to the factory with default properties.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        public static ILoggingBuilder AddSystemdConsole(this ILoggingBuilder builder) =>
            builder.AddFormatterWithName(ConsoleFormatterNames.Systemd);

        internal static ILoggingBuilder AddConsoleWithFormatter<TOptions>(this ILoggingBuilder builder, string name, Action<TOptions> configure)
            where TOptions : ConsoleFormatterOptions
        {
            ThrowHelper.ThrowIfNull(configure);

            builder.AddFormatterWithName(name);
            builder.Services.Configure(configure);

            return builder;
        }

        private static ILoggingBuilder AddFormatterWithName(this ILoggingBuilder builder, string name) =>
            builder.AddConsole((ConsoleLoggerOptions options) => options.FormatterName = name);

        /// <summary>
        /// Adds a custom console logger formatter 'TFormatter' to be configured with options 'TOptions'.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        [RequiresDynamicCode(RequiresDynamicCodeMessage)]
        [RequiresUnreferencedCode(TrimmingRequiresUnreferencedCodeMessage)]
        public static ILoggingBuilder AddConsoleFormatter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TFormatter, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TOptions>(this ILoggingBuilder builder)
            where TOptions : ConsoleFormatterOptions
            where TFormatter : ConsoleFormatter
        {
            return AddConsoleFormatter<TFormatter, TOptions, ConsoleLoggerFormatterConfigureOptions<TFormatter, TOptions>>(builder);
        }

        /// <summary>
        /// Adds a custom console logger formatter 'TFormatter' to be configured with options 'TOptions'.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        /// <param name="configure">A delegate to configure options 'TOptions' for custom formatter 'TFormatter'.</param>
        [RequiresDynamicCode(RequiresDynamicCodeMessage)]
        [RequiresUnreferencedCode(TrimmingRequiresUnreferencedCodeMessage)]
        public static ILoggingBuilder AddConsoleFormatter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TFormatter, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TOptions>(this ILoggingBuilder builder, Action<TOptions> configure)
            where TOptions : ConsoleFormatterOptions
            where TFormatter : ConsoleFormatter
        {
            ThrowHelper.ThrowIfNull(configure);

            builder.AddConsoleFormatter<TFormatter, TOptions>();
            builder.Services.Configure(configure);
            return builder;
        }

        private static ILoggingBuilder AddConsoleFormatter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TFormatter, TOptions, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TConfigureOptions>(this ILoggingBuilder builder)
            where TOptions : ConsoleFormatterOptions
            where TFormatter : ConsoleFormatter
            where TConfigureOptions : class, IConfigureOptions<TOptions>
        {
            builder.AddConfiguration();

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ConsoleFormatter, TFormatter>());
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<TOptions>, TConfigureOptions>());
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IOptionsChangeTokenSource<TOptions>, ConsoleLoggerFormatterOptionsChangeTokenSource<TFormatter, TOptions>>());

            return builder;
        }

        internal static IConfiguration GetFormatterOptionsSection(this ILoggerProviderConfiguration<ConsoleLoggerProvider> providerConfiguration)
        {
            return providerConfiguration.Configuration.GetSection("FormatterOptions");
        }
    }

    [UnsupportedOSPlatform("browser")]
    internal sealed class ConsoleLoggerFormatterConfigureOptions<TFormatter, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TOptions> : ConfigureFromConfigurationOptions<TOptions>
        where TOptions : ConsoleFormatterOptions
        where TFormatter : ConsoleFormatter
    {
        [RequiresDynamicCode(ConsoleLoggerExtensions.RequiresDynamicCodeMessage)]
        [RequiresUnreferencedCode(ConsoleLoggerExtensions.TrimmingRequiresUnreferencedCodeMessage)]
        public ConsoleLoggerFormatterConfigureOptions(ILoggerProviderConfiguration<ConsoleLoggerProvider> providerConfiguration) :
            base(providerConfiguration.GetFormatterOptionsSection())
        {
        }
    }

    [UnsupportedOSPlatform("browser")]
    internal sealed class ConsoleLoggerFormatterOptionsChangeTokenSource<TFormatter, TOptions> : ConfigurationChangeTokenSource<TOptions>
        where TOptions : ConsoleFormatterOptions
        where TFormatter : ConsoleFormatter
    {
        public ConsoleLoggerFormatterOptionsChangeTokenSource(ILoggerProviderConfiguration<ConsoleLoggerProvider> providerConfiguration)
            : base(providerConfiguration.GetFormatterOptionsSection())
        {
        }
    }
}
