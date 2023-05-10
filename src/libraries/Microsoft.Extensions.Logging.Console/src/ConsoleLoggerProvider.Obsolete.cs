// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Logging.Console
{
    public partial class ConsoleLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        [Obsolete("This method is retained only for compatibility. The recommended alternative is using LoggerFactory to configure filtering and ConsoleLoggerOptions to configure logging options.")]
        public ConsoleLoggerProvider(IConsoleLoggerSettings settings)
            : this(ConsoleLoggerSettingsAdapter.GetOptionsMonitor(settings))
        { }

        [Obsolete("This method is retained only for compatibility. The recommended alternative is using LoggerFactory to configure filtering and ConsoleLoggerOptions to configure logging options.")]
        public ConsoleLoggerProvider(System.Func<string, Logging.LogLevel, bool> filter, bool includeScopes, bool disableColors)
            : this(new ConsoleLoggerSettings() { DisableColors = disableColors, IncludeScopes = includeScopes })
        { }

        [Obsolete("This method is retained only for compatibility. The recommended alternative is using LoggerFactory to configure filtering and ConsoleLoggerOptions to configure logging options.")]
        public ConsoleLoggerProvider(System.Func<string, Logging.LogLevel, bool> filter, bool includeScopes)
            : this(filter, includeScopes, false)
        { }

        [Obsolete]
        private sealed class ConsoleLoggerSettingsAdapter : IConfigureOptions<ConsoleLoggerOptions>, IOptionsChangeTokenSource<ConsoleLoggerOptions>
        {
            private IConsoleLoggerSettings _settings;
            private ConsoleLoggerSettingsAdapter(IConsoleLoggerSettings settings)
            {
                _settings = settings;
            }

            IChangeToken IOptionsChangeTokenSource<ConsoleLoggerOptions>.GetChangeToken() => _settings.ChangeToken ?? NullChangeToken.Instance;

            string IOptionsChangeTokenSource<ConsoleLoggerOptions>.Name => Microsoft.Extensions.Options.Options.DefaultName;

            void IConfigureOptions<ConsoleLoggerOptions>.Configure(ConsoleLoggerOptions options)
            {
                options.IncludeScopes = _settings.IncludeScopes;
                if (_settings is ConfigurationConsoleLoggerSettings configSettings)
                {
                    options.Configure(configSettings._configuration);
                }
                else if (_settings is ConsoleLoggerSettings consoleSettings)
                {
                    options.DisableColors = consoleSettings.DisableColors;
                }
            }

            internal static OptionsMonitor<ConsoleLoggerOptions> GetOptionsMonitor(IConsoleLoggerSettings settings)
            {
                ConsoleLoggerSettingsAdapter adapter = new(settings);
                OptionsFactory<ConsoleLoggerOptions> factory = new( new IConfigureOptions<ConsoleLoggerOptions>[] { adapter }, Array.Empty<IPostConfigureOptions<ConsoleLoggerOptions>>());
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
