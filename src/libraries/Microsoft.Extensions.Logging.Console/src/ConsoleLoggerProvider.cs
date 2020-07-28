// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging.Console
{
    /// <summary>
    /// A provider of <see cref="ConsoleLogger"/> instances.
    /// </summary>
    [ProviderAlias("Console")]
    public class ConsoleLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        private readonly IOptionsMonitor<ConsoleLoggerOptions> _options;
        private readonly ConcurrentDictionary<string, ConsoleLogger> _loggers;
        private ConcurrentDictionary<string, ConsoleFormatter> _formatters;
        private readonly ConsoleLoggerProcessor _messageQueue;

        private IDisposable _optionsReloadToken;
        private IExternalScopeProvider _scopeProvider = NullExternalScopeProvider.Instance;

        /// <summary>
        /// Creates an instance of <see cref="ConsoleLoggerProvider"/>.
        /// </summary>
        /// <param name="options">The options to create <see cref="ConsoleLogger"/> instances with.</param>
        public ConsoleLoggerProvider(IOptionsMonitor<ConsoleLoggerOptions> options)
            : this(options, Enumerable.Empty<ConsoleFormatter>()) { }

        /// <summary>
        /// Creates an instance of <see cref="ConsoleLoggerProvider"/>.
        /// </summary>
        /// <param name="options">The options to create <see cref="ConsoleLogger"/> instances with.</param>
        /// <param name="formatters">Log formatters added for <see cref="ConsoleLogger"/> insteaces.</param>
        public ConsoleLoggerProvider(IOptionsMonitor<ConsoleLoggerOptions> options, IEnumerable<ConsoleFormatter> formatters)
        {
            _options = options;
            _loggers = new ConcurrentDictionary<string, ConsoleLogger>();
            SetFormatters(formatters);

            ReloadLoggerOptions(options.CurrentValue);
            _optionsReloadToken = _options.OnChange(ReloadLoggerOptions);

            _messageQueue = new ConsoleLoggerProcessor();
            if (DoesConsoleSupportAnsi())
            {
                _messageQueue.Console = new AnsiLogConsole();
                _messageQueue.ErrorConsole = new AnsiLogConsole(stdErr: true);
            }
            else
            {
                _messageQueue.Console = new AnsiParsingLogConsole();
                _messageQueue.ErrorConsole = new AnsiParsingLogConsole(stdErr: true);
            }
        }

        private static bool DoesConsoleSupportAnsi()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return true;
            }
            // for Windows, check the console mode
            var stdOutHandle = Interop.Kernel32.GetStdHandle(Interop.Kernel32.STD_OUTPUT_HANDLE);
            if (!Interop.Kernel32.GetConsoleMode(stdOutHandle, out int consoleMode))
            {
                return false;
            }

            return (consoleMode & Interop.Kernel32.ENABLE_VIRTUAL_TERMINAL_PROCESSING) == Interop.Kernel32.ENABLE_VIRTUAL_TERMINAL_PROCESSING;
        }

        private void SetFormatters(IEnumerable<ConsoleFormatter> formatters = null)
        {
            _formatters = new ConcurrentDictionary<string, ConsoleFormatter>(StringComparer.OrdinalIgnoreCase);
            if (formatters == null || !formatters.Any())
            {
                var defaultMonitor = new FormatterOptionsMonitor<SimpleConsoleFormatterOptions>(new SimpleConsoleFormatterOptions());
                var systemdMonitor = new FormatterOptionsMonitor<ConsoleFormatterOptions>(new ConsoleFormatterOptions());
                var jsonMonitor = new FormatterOptionsMonitor<JsonConsoleFormatterOptions>(new JsonConsoleFormatterOptions());
                _formatters.GetOrAdd(ConsoleFormatterNames.Simple, formatterName => new SimpleConsoleFormatter(defaultMonitor));
                _formatters.GetOrAdd(ConsoleFormatterNames.Systemd, formatterName => new SystemdConsoleFormatter(systemdMonitor));
                _formatters.GetOrAdd(ConsoleFormatterNames.Json, formatterName => new JsonConsoleFormatter(jsonMonitor));
            }
            else
            {
                foreach (ConsoleFormatter formatter in formatters)
                {
                    _formatters.GetOrAdd(formatter.Name, formatterName => formatter);
                }
            }
        }

        // warning:  ReloadLoggerOptions can be called before the ctor completed,... before registering all of the state used in this method need to be initialized
        private void ReloadLoggerOptions(ConsoleLoggerOptions options)
        {
            if (options.FormatterName == null || !_formatters.TryGetValue(options.FormatterName, out ConsoleFormatter logFormatter))
            {
#pragma warning disable CS0618
                logFormatter = options.Format switch
                {
                    ConsoleLoggerFormat.Systemd => _formatters[ConsoleFormatterNames.Systemd],
                    _ => _formatters[ConsoleFormatterNames.Simple],
                };
                if (options.FormatterName == null)
                {
                    UpdateFormatterOptions(logFormatter, options);
                }
#pragma warning restore CS0618
            }

            foreach (KeyValuePair<string, ConsoleLogger> logger in _loggers)
            {
                logger.Value.Options = options;
                logger.Value.Formatter = logFormatter;
            }
        }

        /// <inheritdoc />
        public ILogger CreateLogger(string name)
        {
            if (_options.CurrentValue.FormatterName == null || !_formatters.TryGetValue(_options.CurrentValue.FormatterName, out ConsoleFormatter logFormatter))
            {
#pragma warning disable CS0618
                logFormatter = _options.CurrentValue.Format switch
                {
                    ConsoleLoggerFormat.Systemd => _formatters[ConsoleFormatterNames.Systemd],
                    _ => _formatters[ConsoleFormatterNames.Simple],
                };
                if (_options.CurrentValue.FormatterName == null)
                {
                    UpdateFormatterOptions(logFormatter, _options.CurrentValue);
                }
#pragma warning disable CS0618
            }

            return _loggers.GetOrAdd(name, loggerName => new ConsoleLogger(name, _messageQueue)
            {
                Options = _options.CurrentValue,
                ScopeProvider = _scopeProvider,
                Formatter = logFormatter,
            });
        }
#pragma warning disable CS0618
        private void UpdateFormatterOptions(ConsoleFormatter formatter, ConsoleLoggerOptions deprecatedFromOptions)
        {
            // kept for deprecated apis:
            if (formatter is SimpleConsoleFormatter defaultFormatter)
            {
                defaultFormatter.FormatterOptions = new SimpleConsoleFormatterOptions()
                {
                    DisableColors = deprecatedFromOptions.DisableColors,
                    IncludeScopes = deprecatedFromOptions.IncludeScopes,
                    TimestampFormat = deprecatedFromOptions.TimestampFormat,
                    UseUtcTimestamp = deprecatedFromOptions.UseUtcTimestamp,
                };
            }
            else
            if (formatter is SystemdConsoleFormatter systemdFormatter)
            {
                systemdFormatter.FormatterOptions = new ConsoleFormatterOptions()
                {
                    IncludeScopes = deprecatedFromOptions.IncludeScopes,
                    TimestampFormat = deprecatedFromOptions.TimestampFormat,
                    UseUtcTimestamp = deprecatedFromOptions.UseUtcTimestamp,
                };
            }
        }
#pragma warning restore CS0618

        /// <inheritdoc />
        public void Dispose()
        {
            _optionsReloadToken?.Dispose();
            _messageQueue.Dispose();
        }

        /// <inheritdoc />
        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider;

            foreach (System.Collections.Generic.KeyValuePair<string, ConsoleLogger> logger in _loggers)
            {
                logger.Value.ScopeProvider = _scopeProvider;
            }
        }
    }
}
