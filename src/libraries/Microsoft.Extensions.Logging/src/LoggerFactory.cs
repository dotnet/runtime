// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging
{

    /// <summary>
    /// Produces instances of <see cref="ILogger"/> classes based on the given providers.
    /// </summary>
    public class LoggerFactory : ILoggerFactory
    {
        private readonly Dictionary<string, Logger> _loggers = new Dictionary<string, Logger>(StringComparer.Ordinal);
        private readonly List<ProviderRegistration> _providerRegistrations = new List<ProviderRegistration>();
        private readonly object _sync = new object();
        private volatile bool _disposed;
        private readonly IDisposable? _changeTokenRegistration;
        private LoggerFilterOptions _filterOptions;
        private IExternalScopeProvider? _scopeProvider;
        private readonly LoggerFactoryOptions _factoryOptions;
        private IProcessorFactory[] _processorFactories;

        /// <summary>
        /// Creates a new <see cref="LoggerFactory"/> instance.
        /// </summary>
        public LoggerFactory() : this(Array.Empty<ILoggerProvider>())
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
        public LoggerFactory(IEnumerable<ILoggerProvider> providers, IOptionsMonitor<LoggerFilterOptions> filterOption) : this(providers, filterOption, null)
        {
        }

        /// <summary>
        /// Creates a new <see cref="LoggerFactory"/> instance.
        /// </summary>
        /// <param name="providers">The providers to use in producing <see cref="ILogger"/> instances.</param>
        /// <param name="filterOption">The filter option to use.</param>
        /// <param name="options">The <see cref="LoggerFactoryOptions"/>.</param>
        public LoggerFactory(IEnumerable<ILoggerProvider> providers, IOptionsMonitor<LoggerFilterOptions> filterOption, IOptions<LoggerFactoryOptions>? options) :
            this(providers, Array.Empty<IProcessorFactory>(), filterOption, options, null)
        {
        }

        /// <summary>
        /// Creates a new <see cref="LoggerFactory"/> instance.
        /// </summary>
        /// <param name="providers">The providers to use in producing <see cref="ILogger"/> instances.</param>
        /// <param name="processorFactories">The processor factories to use in the logging pipeline.</param>
        /// <param name="filterOption">The filter option to use.</param>
        /// <param name="options">The <see cref="LoggerFactoryOptions"/>.</param>
        /// <param name="scopeProvider">The <see cref="IExternalScopeProvider"/>.</param>
        public LoggerFactory(IEnumerable<ILoggerProvider> providers,
                             IEnumerable<IProcessorFactory> processorFactories,
                             IOptionsMonitor<LoggerFilterOptions> filterOption,
                             IOptions<LoggerFactoryOptions>? options = null,
                             IExternalScopeProvider? scopeProvider = null)
        {
            _scopeProvider = scopeProvider;

            _factoryOptions = options == null || options.Value == null ? new LoggerFactoryOptions() : options.Value;

            const ActivityTrackingOptions ActivityTrackingOptionsMask = ~(ActivityTrackingOptions.SpanId | ActivityTrackingOptions.TraceId | ActivityTrackingOptions.ParentId |
                                                                          ActivityTrackingOptions.TraceFlags | ActivityTrackingOptions.TraceState | ActivityTrackingOptions.Tags
                                                                          | ActivityTrackingOptions.Baggage);


            if ((_factoryOptions.ActivityTrackingOptions & ActivityTrackingOptionsMask) != 0)
            {
                throw new ArgumentException("placeholder");
            }

            foreach (ILoggerProvider provider in providers)
            {
                AddProviderRegistration(provider, dispose: false);
            }

            _processorFactories = processorFactories.ToArray();

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
            ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
            ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            return new DisposingLoggerFactory(loggerFactory, serviceProvider);
        }

        [MemberNotNull(nameof(_filterOptions))]
        private void RefreshFilters(LoggerFilterOptions filterOptions)
        {
            lock (_sync)
            {
                _filterOptions = filterOptions;
                foreach (KeyValuePair<string, Logger> registeredLogger in _loggers)
                {
                    Logger logger = registeredLogger.Value;
                    UpdateLogger(logger);
                }
            }
        }

        internal void OnProcessorInvalidated(Logger logger)
        {
            lock (_sync)
            {
                UpdateLogger(logger);
            }
        }

        private void UpdateLogger(Logger logger)
        {
            Debug.Assert(Monitor.IsEntered(_sync));
            LoggerInformation[] loggerInfos = logger.VersionedState.Loggers;
            for (int i = 0; i < loggerInfos.Length; i++)
            {
                LoggerInformation previousInfo = loggerInfos[i];
                loggerInfos[i] = CreateLoggerInformation(logger, previousInfo.Provider, previousInfo.Category, previousInfo.Logger, previousInfo.Processor, previousInfo.ProcessorCancelRegistration);
            }
            UpdateLogger(logger, loggerInfos);
        }

        private void UpdateLogger(Logger logger, LoggerInformation[] loggerInfos)
        {
            Debug.Assert(Monitor.IsEntered(_sync));
            VersionedLoggerState oldState = logger.VersionedState;
            // Even though we set new versioned state before disposing the old one keep in mind that
            // it is still possible for a concurrent operation to capture the versioned state before
            // it was replaced and then use it after it was disposed.
            logger.VersionedState = new VersionedLoggerState(loggerInfos, GetProcessor(loggerInfos));
            oldState.MarkNotUpToDate();
        }

        private ILogEntryProcessor GetProcessor(LoggerInformation[] loggerInfos)
        {
            ILogEntryProcessor processor = new DispatchProcessor(loggerInfos);
            for (int i = _processorFactories.Length - 1; i >= 0; i--)
            {
                processor = _processorFactories[i].GetProcessor(processor);
            }
            return processor;
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
                if (!_loggers.TryGetValue(categoryName, out Logger? logger))
                {
                    logger = new Logger(this);
                    LoggerInformation[] loggerInfos = CreateLoggers(logger, categoryName);
                    UpdateLogger(logger, loggerInfos);
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

            if (provider == null) throw new ArgumentNullException(nameof(provider));

            lock (_sync)
            {
                AddProviderRegistration(provider, dispose: true);

                foreach (KeyValuePair<string, Logger> existingLogger in _loggers)
                {
                    Logger logger = existingLogger.Value;
                    LoggerInformation[] loggerInformation = logger.VersionedState.Loggers;

                    int newLoggerIndex = loggerInformation == null ? 0 : loggerInformation.Length;
                    Array.Resize(ref loggerInformation, newLoggerIndex + 1);
                    loggerInformation[newLoggerIndex] = CreateLoggerInformation(logger, provider, existingLogger.Key);
                    UpdateLogger(logger, loggerInformation);
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
                _scopeProvider ??= new LoggerFactoryScopeProvider(_factoryOptions.ActivityTrackingOptions);

                supportsExternalScope.SetScopeProvider(_scopeProvider);
            }
        }

        private LoggerInformation[] CreateLoggers(Logger logger, string categoryName)
        {
            var loggers = new LoggerInformation[_providerRegistrations.Count];
            for (int i = 0; i < _providerRegistrations.Count; i++)
            {
                loggers[i] = CreateLoggerInformation(logger, _providerRegistrations[i].Provider, categoryName);
            }
            return loggers;
        }

        private LoggerInformation CreateLoggerInformation(
            Logger logger,
            ILoggerProvider provider,
            string category,
            ILogger? existingLogger = null,
            ILogEntryProcessor? existingProcessor = null,
            CancellationTokenRegistration? existingProcCancelRegistration = null)
        {
            LoggerRuleSelector.Select(_filterOptions,
                provider.GetType(),
                category,
                out LogLevel? minLevel,
                out Func<string?, string?, LogLevel, bool>? filter);

            ILogger loggerSink = existingLogger ?? provider.CreateLogger(category);
            minLevel ??= LogLevel.Trace;

            ILogEntryProcessor? processor = existingProcessor;
            CancellationTokenRegistration? registration = existingProcCancelRegistration;
            // TODO: CancellationTokenRegistration.Token isn't available in .NET Standard 2.0.
            //if (registration.HasValue && registration.Value.Token.IsCancellationRequested)
            //{
            //    processor = null;
            //    registration.Value.Dispose();
            //}
            if (processor == null && loggerSink is ILogEntryProcessorFactory factory)
            {
                var processorContext = factory.GetProcessor();
                if (processorContext.CancellationToken.IsCancellationRequested)
                {
                    processor = null;
                }
                else
                {
                    processor = processorContext.Processor;
                    registration = processorContext.CancellationToken.Register(logger.ProcessorInvalidated);
                }
            }
            return new LoggerInformation(provider, category, loggerSink, processor, registration, minLevel.Value, filter);
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

                foreach (ProviderRegistration registration in _providerRegistrations)
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

        private sealed class DisposingLoggerFactory : ILoggerFactory
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
