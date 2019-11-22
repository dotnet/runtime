// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging
{
    public class LoggerFactory : ILoggerFactory
    {
        private static readonly LoggerRuleSelector RuleSelector = new LoggerRuleSelector();

        private readonly Dictionary<string, Logger> _loggers = new Dictionary<string, Logger>(StringComparer.Ordinal);
        private readonly List<ProviderRegistration> _providerRegistrations = new List<ProviderRegistration>();
        private readonly object _sync = new object();
        private volatile bool _disposed;
        private IDisposable _changeTokenRegistration;
        private LoggerFilterOptions _filterOptions;

        internal LoggerExternalScopeProvider ScopeProvider { get; private set; }

        public LoggerFactory() : this(Enumerable.Empty<ILoggerProvider>())
        {
        }

        public LoggerFactory(IEnumerable<ILoggerProvider> providers) : this(providers, new StaticFilterOptionsMonitor(new LoggerFilterOptions()))
        {
        }

        public LoggerFactory(IEnumerable<ILoggerProvider> providers, LoggerFilterOptions filterOptions) : this(providers, new StaticFilterOptionsMonitor(filterOptions))
        {
        }

        public LoggerFactory(IEnumerable<ILoggerProvider> providers, IOptionsMonitor<LoggerFilterOptions> filterOption)
        {
            foreach (var provider in providers)
            {
                AddProviderRegistration(provider, dispose: false);
            }

            _changeTokenRegistration = filterOption.OnChange(RefreshFilters);
            RefreshFilters(filterOption.CurrentValue);
        }

        private void RefreshFilters(LoggerFilterOptions filterOptions)
        {
            lock (_sync)
            {
                _filterOptions = filterOptions;
                foreach (var logger in _loggers)
                {
                    var loggerInformation = logger.Value.Loggers;
                    var categoryName = logger.Key;

                    ApplyRules(loggerInformation, categoryName, 0, loggerInformation.Length);

                    logger.Value.Loggers = loggerInformation;
                }
            }
        }

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
                    logger = new Logger(this)
                    {
                        Loggers = CreateLoggers(categoryName)
                    };
                    _loggers[categoryName] = logger;
                }

                return logger;
            }
        }

        public void AddProvider(ILoggerProvider provider)
        {
            if (CheckDisposed())
            {
                throw new ObjectDisposedException(nameof(LoggerFactory));
            }

            lock (_sync)
            {
                AddProviderRegistration(provider, dispose: true);

                foreach (var logger in _loggers)
                {
                    var loggerInformation = logger.Value.Loggers;
                    var categoryName = logger.Key;

                    Array.Resize(ref loggerInformation, loggerInformation.Length + 1);
                    var newLoggerIndex = loggerInformation.Length - 1;

                    SetLoggerInformation(ref loggerInformation[newLoggerIndex], provider, categoryName);
                    ApplyRules(loggerInformation, categoryName, newLoggerIndex, 1);

                    logger.Value.Loggers = loggerInformation;
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
                if (ScopeProvider == null)
                {
                    ScopeProvider = new LoggerExternalScopeProvider();
                }

                supportsExternalScope.SetScopeProvider(ScopeProvider);
            }
        }

        private void SetLoggerInformation(ref LoggerInformation loggerInformation, ILoggerProvider provider,  string categoryName)
        {
            loggerInformation.Logger = provider.CreateLogger(categoryName);
            loggerInformation.ProviderType = provider.GetType();
            loggerInformation.ExternalScope = provider is ISupportExternalScope;
        }

        private LoggerInformation[] CreateLoggers(string categoryName)
        {
            var loggers = new LoggerInformation[_providerRegistrations.Count];
            for (int i = 0; i < _providerRegistrations.Count; i++)
            {
                SetLoggerInformation(ref loggers[i], _providerRegistrations[i].Provider, categoryName);
            }

            ApplyRules(loggers, categoryName, 0, loggers.Length);
            return loggers;
        }

        private void ApplyRules(LoggerInformation[] loggers, string categoryName, int start, int count)
        {
            for (var index = start; index < start + count; index++)
            {
                ref var loggerInformation = ref loggers[index];

                RuleSelector.Select(_filterOptions,
                    loggerInformation.ProviderType,
                    categoryName,
                    out var minLevel,
                    out var filter);

                loggerInformation.Category = categoryName;
                loggerInformation.MinLevel = minLevel;
                loggerInformation.Filter = filter;
            }
        }

        /// <summary>
        /// Check if the factory has been disposed.
        /// </summary>
        /// <returns>True when <see cref="Dispose()"/> as been called</returns>
        protected virtual bool CheckDisposed() => _disposed;

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
    }
}