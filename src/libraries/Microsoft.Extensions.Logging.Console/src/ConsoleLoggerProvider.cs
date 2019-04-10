// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging.Console
{
    [ProviderAlias("Console")]
    public class ConsoleLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        private readonly IOptionsMonitor<ConsoleLoggerOptions> _options;
        private readonly ConcurrentDictionary<string, ConsoleLogger> _loggers;
        private readonly ConsoleLoggerProcessor _messageQueue;

        private IDisposable _optionsReloadToken;
        private IExternalScopeProvider _scopeProvider = NullExternalScopeProvider.Instance;

        public ConsoleLoggerProvider(IOptionsMonitor<ConsoleLoggerOptions> options)
        {
            _options = options;
            _loggers = new ConcurrentDictionary<string, ConsoleLogger>();

            ReloadLoggerOptions(options.CurrentValue);

            _messageQueue = new ConsoleLoggerProcessor();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _messageQueue.Console = new WindowsLogConsole();
                _messageQueue.ErrorConsole = new WindowsLogConsole(stdErr: true);
            }
            else
            {
                _messageQueue.Console = new AnsiLogConsole(new AnsiSystemConsole());
                _messageQueue.ErrorConsole = new AnsiLogConsole(new AnsiSystemConsole(stdErr: true));
            }
        }

        private void ReloadLoggerOptions(ConsoleLoggerOptions options)
        {
            foreach (var logger in _loggers)
            {
                logger.Value.Options = options;
            }

            _optionsReloadToken = _options.OnChange(ReloadLoggerOptions);
        }

        /// <inheritdoc />
        public ILogger CreateLogger(string name)
        {
            return _loggers.GetOrAdd(name, loggerName => new ConsoleLogger(name, _messageQueue)
            {
                Options = _options.CurrentValue,
                ScopeProvider = _scopeProvider
            });
        }

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

            foreach (var logger in _loggers)
            {
                logger.Value.ScopeProvider = _scopeProvider;
            }

        }
    }
}
