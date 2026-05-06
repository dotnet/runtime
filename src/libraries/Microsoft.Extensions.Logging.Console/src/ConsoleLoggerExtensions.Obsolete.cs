// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Logging
{
    public static partial class ConsoleLoggerExtensions
    {
        /// <summary>
        /// This method is retained only for compatibility.
        /// </summary>
        /// <param name="factory">This method is retained only for compatibility.</param>
        /// <param name="configuration">This method is retained only for compatibility.</param>
        /// <returns>This method is retained only for compatibility.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method is retained only for compatibility. The recommended alternative is AddConsole(this ILoggingBuilder builder).", error: true)]
        public static Logging.ILoggerFactory AddConsole(this Logging.ILoggerFactory factory, Extensions.Configuration.IConfiguration configuration)
        {
            var settings = new ConfigurationConsoleLoggerSettings(configuration);
            return factory.AddConsole(settings);
        }

        /// <summary>
        /// This method is retained only for compatibility.
        /// </summary>
        /// <param name="factory">This method is retained only for compatibility.</param>
        /// <param name="settings">This method is retained only for compatibility.</param>
        /// <returns>This method is retained only for compatibility.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method is retained only for compatibility. The recommended alternative is AddConsole(this ILoggingBuilder builder).", error: true)]
        public static Logging.ILoggerFactory AddConsole(this Logging.ILoggerFactory factory, Console.IConsoleLoggerSettings settings)
        {
            factory.AddProvider(new ConsoleLoggerProvider(ConsoleLoggerSettingsAdapter.GetOptionsMonitor(settings)));
            return factory;
        }

        /// <summary>
        /// This method is retained only for compatibility.
        /// </summary>
        /// <param name="factory">This method is retained only for compatibility.</param>
        /// <param name="minLevel">This method is retained only for compatibility.</param>
        /// <param name="includeScopes">This method is retained only for compatibility.</param>
        /// <returns>This method is retained only for compatibility.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method is retained only for compatibility. The recommended alternative is AddConsole(this ILoggingBuilder builder).", error: true)]
        public static Logging.ILoggerFactory AddConsole(this Logging.ILoggerFactory factory, Logging.LogLevel minLevel, bool includeScopes)
        {
            factory.AddConsole((n, l) => l >= LogLevel.Information, includeScopes);
            return factory;
        }

        /// <summary>
        /// This method is retained only for compatibility.
        /// </summary>
        /// <param name="factory">This method is retained only for compatibility.</param>
        /// <param name="minLevel">This method is retained only for compatibility.</param>
        /// <returns>This method is retained only for compatibility.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method is retained only for compatibility. The recommended alternative is AddConsole(this ILoggingBuilder builder).", error: true)]
        public static Logging.ILoggerFactory AddConsole(this Logging.ILoggerFactory factory, Logging.LogLevel minLevel)
        {
            factory.AddConsole(minLevel, includeScopes: false);
            return factory;
        }

        /// <summary>
        /// This method is retained only for compatibility.
        /// </summary>
        /// <param name="factory">This method is retained only for compatibility.</param>
        /// <param name="includeScopes">This method is retained only for compatibility.</param>
        /// <returns>This method is retained only for compatibility.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method is retained only for compatibility. The recommended alternative is AddConsole(this ILoggingBuilder builder).", error: true)]
        public static Logging.ILoggerFactory AddConsole(this Logging.ILoggerFactory factory, bool includeScopes)
        {
            factory.AddConsole((n, l) => l >= LogLevel.Information, includeScopes);
            return factory;
        }

        /// <summary>
        /// This method is retained only for compatibility.
        /// </summary>
        /// <param name="factory">This method is retained only for compatibility.</param>
        /// <param name="filter">This method is retained only for compatibility.</param>
        /// <param name="includeScopes">This method is retained only for compatibility.</param>
        /// <returns>This method is retained only for compatibility.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method is retained only for compatibility. The recommended alternative is AddConsole(this ILoggingBuilder builder).", error: true)]
        public static Logging.ILoggerFactory AddConsole(this Logging.ILoggerFactory factory, System.Func<string, Logging.LogLevel, bool> filter, bool includeScopes)
        {
            factory.AddConsole(new ConsoleLoggerSettings() { IncludeScopes = includeScopes });
            return factory;
        }

        /// <summary>
        /// This method is retained only for compatibility.
        /// </summary>
        /// <param name="factory">This method is retained only for compatibility.</param>
        /// <param name="filter">This method is retained only for compatibility.</param>
        /// <returns>This method is retained only for compatibility.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method is retained only for compatibility. The recommended alternative is AddConsole(this ILoggingBuilder builder).", error: true)]
        public static Logging.ILoggerFactory AddConsole(this Logging.ILoggerFactory factory, System.Func<string, Logging.LogLevel, bool> filter)
        {
            factory.AddConsole(filter, includeScopes: false);
            return factory;
        }

        /// <summary>
        /// This method is retained only for compatibility.
        /// </summary>
        /// <param name="factory">This method is retained only for compatibility.</param>
        /// <returns>This method is retained only for compatibility.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method is retained only for compatibility. The recommended alternative is AddConsole(this ILoggingBuilder builder).", error: true)]
        public static Logging.ILoggerFactory AddConsole(this Logging.ILoggerFactory factory)
        {
            return factory.AddConsole(includeScopes: false);
        }

        [Obsolete]
        private sealed class ConsoleLoggerSettingsAdapter : IConfigureOptions<ConsoleLoggerOptions>, IOptionsChangeTokenSource<ConsoleLoggerOptions>
        {
            private IConsoleLoggerSettings _settings;
            private ConsoleLoggerSettingsAdapter(IConsoleLoggerSettings settings)
            {
                _settings = settings;
            }

            IChangeToken IOptionsChangeTokenSource<ConsoleLoggerOptions>.GetChangeToken() => _settings.ChangeToken ?? NullChangeToken.Instance;

            string IOptionsChangeTokenSource<ConsoleLoggerOptions>.Name => Options.Options.DefaultName;

            void IConfigureOptions<ConsoleLoggerOptions>.Configure(ConsoleLoggerOptions options)
            {
                options.IncludeScopes = _settings.IncludeScopes;
                if (_settings is ConfigurationConsoleLoggerSettings configSettings)
                {
                    configSettings._configuration.Bind(options);
                }
                else if (_settings is ConsoleLoggerSettings consoleSettings)
                {
                    options.DisableColors = consoleSettings.DisableColors;
                }
            }

            internal static OptionsMonitor<ConsoleLoggerOptions> GetOptionsMonitor(IConsoleLoggerSettings settings)
            {
                ConsoleLoggerSettingsAdapter adapter = new(settings);
                OptionsFactory<ConsoleLoggerOptions> factory = new(new IConfigureOptions<ConsoleLoggerOptions>[] { adapter }, Array.Empty<IPostConfigureOptions<ConsoleLoggerOptions>>());
                IOptionsChangeTokenSource<ConsoleLoggerOptions>[] sources = new IOptionsChangeTokenSource<ConsoleLoggerOptions>[] { adapter };
                OptionsCache<ConsoleLoggerOptions> cache = new();

                return new OptionsMonitor<ConsoleLoggerOptions>(factory, sources, cache);
            }
        }

        private sealed class NullChangeToken : IChangeToken, IDisposable
        {
            internal static NullChangeToken Instance { get; } = new NullChangeToken();
            private NullChangeToken() { }
            public bool HasChanged => false;
            public bool ActiveChangeCallbacks => false;
            public IDisposable RegisterChangeCallback(Action<object?> callback, object? state) => this;
            public void Dispose() { }
        }
    }
}
