// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Produces instances of <see cref="ILogger"/> classes based on the given providers.
    /// </summary>
    public class LoggerFactory : ILoggerFactory
    {
        private static readonly LoggerRuleSelector RuleSelector = new LoggerRuleSelector();

        private readonly Dictionary<string, Logger> _loggers = new Dictionary<string, Logger>(StringComparer.Ordinal);
        private readonly List<ProviderRegistration> _providerRegistrations = new List<ProviderRegistration>();
        private readonly object _sync = new object();
        private volatile bool _disposed;
        private IDisposable _changeTokenRegistration;
        private LoggerFilterOptions _filterOptions;
        private LoggerExternalScopeProvider _scopeProvider;

        /// <summary>
        /// Creates a new <see cref="LoggerFactory"/> instance.
        /// </summary>
        public LoggerFactory() : this(Enumerable.Empty<ILoggerProvider>())
        {
        }

        /// <summary>
        /// Creates a new <see cref="LoggerFactory"/> instance.
        /// </summary>
        /// <param name="providers">The providers to use in producing <see cref="ILogger"/> instances.</param>
        public LoggerFactory(IEnumerable<ILoggerProvider> providers) : this(providers, new StaticFilterOptionsMonitor(new LoggerFilterOptions()))
        {
        }

        /// <summary>
        /// Creates a new <see cref="LoggerFactory"/> instance.
        /// </summary>
        /// <param name="providers">The providers to use in producing <see cref="ILogger"/> instances.</param>
        /// <param name="filterOptions">The filter options to use.</param>
        public LoggerFactory(IEnumerable<ILoggerProvider> providers, LoggerFilterOptions filterOptions) : this(providers, new StaticFilterOptionsMonitor(filterOptions))
        {
        }

        /// <summary>
        /// Creates a new <see cref="LoggerFactory"/> instance.
        /// </summary>
        /// <param name="providers">The providers to use in producing <see cref="ILogger"/> instances.</param>
        /// <param name="filterOption">The filter option to use.</param>
        public LoggerFactory(IEnumerable<ILoggerProvider> providers, IOptionsMonitor<LoggerFilterOptions> filterOption)
        {
            foreach (var provider in providers)
            {
                AddProviderRegistration(provider, dispose: false);
            }

            _changeTokenRegistration = filterOption.OnChange(RefreshFilters);
            RefreshFilters(filterOption.CurrentValue);
        }

        /// <summary>
        /// Creates new instance of <see cref="ILoggerFactory"/> configured using provided <paramref name="configure"/> delegate.
        /// </summary>
        /// <param name="configure">A delegate to configure the <see cref="ILoggingBuilder"/>.</param>
        /// <returns>The <see cref="ILoggerFactory"/> that was created.</returns>
        public static ILoggerFactory Create(Action<ILoggingBuilder> configure)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(configure);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            return new DisposingLoggerFactory(loggerFactory, serviceProvider);
        }

        private void RefreshFilters(LoggerFilterOptions filterOptions)
        {
            lock (_sync)
            {
                _filterOptions = filterOptions;
                foreach (var registeredLogger in _loggers)
                {
                    var logger = registeredLogger.Value;
                    (logger.MessageLoggers, logger.ScopeLoggers) = ApplyFilters(logger.Loggers);
                }
            }
        }

        /// <summary>
        /// Creates an <see cref="ILogger"/> with the given <paramref name="categoryName"/>.
        /// </summary>
        /// <param name="categoryName">The category name for messages produced by the logger.</param>
        /// <returns>The <see cref="ILogger"/> that was created.</returns>
        public ILogger CreateLogger(string categoryName)
        {
            if (CheckDisposed())
            {
                throw new ObjectDisposedException(nameof(LoggerFactory));
            }

            lock (_sync)
            {
                if (!_loggers.TryGetValue(categoryName, out var logger))
                {
                    logger = new Logger
                    {
                        Loggers = CreateLoggers(categoryName),
                    };

                    (logger.MessageLoggers, logger.ScopeLoggers) = ApplyFilters(logger.Loggers);

                    _loggers[categoryName] = logger;
                }

                return logger;
            }
        }

        /// <summary>
        /// Adds the given provider to those used in creating <see cref="ILogger"/> instances.
        /// </summary>
        /// <param name="provider">The <see cref="ILoggerProvider"/> to add.</param>
        public void AddProvider(ILoggerProvider provider)
        {
            if (CheckDisposed())
            {
                throw new ObjectDisposedException(nameof(LoggerFactory));
            }

            lock (_sync)
            {
                AddProviderRegistration(provider, dispose: true);

                foreach (var existingLogger in _loggers)
                {
                    var logger = existingLogger.Value;
                    var loggerInformation = logger.Loggers;

                    var newLoggerIndex = loggerInformation.Length;
                    Array.Resize(ref loggerInformation, loggerInformation.Length + 1);
                    loggerInformation[newLoggerIndex] = new LoggerInformation(provider, existingLogger.Key);

                    logger.Loggers = loggerInformation;
                    (logger.MessageLoggers, logger.ScopeLoggers) = ApplyFilters(logger.Loggers);
                }
            }
        }

        private void AddProviderRegistration(ILoggerProvider provider, bool dispose)
        {
            _providerRegistrations.Add(new ProviderRegistration
            {
                Provider = provider,
                ShouldDispose = dispose
            });

            if (provider is ISupportExternalScope supportsExternalScope)
            {
                if (_scopeProvider == null)
                {
                    _scopeProvider = new LoggerExternalScopeProvider();
                }

                supportsExternalScope.SetScopeProvider(_scopeProvider);
            }
        }

        private LoggerInformation[] CreateLoggers(string categoryName)
        {
            var loggers = new LoggerInformation[_providerRegistrations.Count];
            for (var i = 0; i < _providerRegistrations.Count; i++)
            {
                loggers[i] = new LoggerInformation(_providerRegistrations[i].Provider, categoryName);
            }
            return loggers;
        }

        private (MessageLogger[] MessageLoggers, ScopeLogger[] ScopeLoggers) ApplyFilters(LoggerInformation[] loggers)
        {
            var messageLoggers = new List<MessageLogger>();
            var scopeLoggers = _filterOptions.CaptureScopes ? new List<ScopeLogger>() : null;

            foreach (var loggerInformation in loggers)
            {
                RuleSelector.Select(_filterOptions,
                    loggerInformation.ProviderType,
                    loggerInformation.Category,
                    out var minLevel,
                    out var filter);

                if (minLevel != null && minLevel > LogLevel.Critical)
                {
                    continue;
                }

                messageLoggers.Add(new MessageLogger(loggerInformation.Logger, loggerInformation.Category, loggerInformation.ProviderType.FullName, minLevel, filter));

                if (!loggerInformation.ExternalScope)
                {
                    scopeLoggers?.Add(new ScopeLogger(logger: loggerInformation.Logger, externalScopeProvider: null));
                }
            }

            if (_scopeProvider != null)
            {
                scopeLoggers?.Add(new ScopeLogger(logger: null, externalScopeProvider: _scopeProvider));
            }

            return (messageLoggers.ToArray(), scopeLoggers?.ToArray());
        }

        /// <summary>
        /// Check if the factory has been disposed.
        /// </summary>
        /// <returns>True when <see cref="Dispose()"/> as been called</returns>
        protected virtual bool CheckDisposed() => _disposed;

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                _changeTokenRegistration?.Dispose();

                foreach (var registration in _providerRegistrations)
                {
                    try
                    {
                        if (registration.ShouldDispose)
                        {
                            registration.Provider.Dispose();
                        }
                    }
                    catch
                    {
                        // Swallow exceptions on dispose
                    }
                }
            }
        }

        private struct ProviderRegistration
        {
            public ILoggerProvider Provider;
            public bool ShouldDispose;
        }

        private class DisposingLoggerFactory : ILoggerFactory
        {
            private readonly ILoggerFactory _loggerFactory;

            private readonly ServiceProvider _serviceProvider;

            public DisposingLoggerFactory(ILoggerFactory loggerFactory, ServiceProvider serviceProvider)
            {
                _loggerFactory = loggerFactory;
                _serviceProvider = serviceProvider;
            }

            public void Dispose()
            {
                _serviceProvider.Dispose();
            }

            public ILogger CreateLogger(string categoryName)
            {
                return _loggerFactory.CreateLogger(categoryName);
            }

            public void AddProvider(ILoggerProvider provider)
            {
                _loggerFactory.AddProvider(provider);
            }
        }
    }
}
