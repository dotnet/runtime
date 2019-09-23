// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.Extensions.Logging.EventSource
{
    /// <summary>
    /// The provider for the <see cref="EventSourceLogger"/>.
    /// </summary>
    [ProviderAlias("EventSource")]
    internal class EventSourceLoggerProvider : ILoggerProvider
    {
        private static int _globalFactoryID;

        // A small integer that uniquely identifies the LoggerFactory associated with this LoggingProvider.
        private readonly int _factoryID;

        private LoggerFilterRule[] _rules;
        private EventSourceLogger _loggers; // Linked list of loggers that I have created
        private readonly LoggingEventSource _eventSource;
        private readonly bool _handleFilters;

        private IDisposable _filterChangeToken;

        public EventSourceLoggerProvider(LoggingEventSource eventSource) : this(eventSource, handleFilters: false)
        {

        }

        public EventSourceLoggerProvider(LoggingEventSource eventSource, bool handleFilters)
        {
            if (eventSource == null)
            {
                throw new ArgumentNullException(nameof(eventSource));
            }
            _eventSource = eventSource;
            _handleFilters = handleFilters;
            _factoryID = Interlocked.Increment(ref _globalFactoryID);
            if (_handleFilters)
            {
                OnFilterConfigurationChange();
            }
        }

        private void OnFilterConfigurationChange()
        {
            _filterChangeToken = _eventSource
                .GetFilterChangeToken()
                .RegisterChangeCallback(state => ((EventSourceLoggerProvider)state).OnFilterConfigurationChange(), this);

            SetFilterSpec(_eventSource.GetFilterRules());
        }

        /// <inheritdoc />
        public ILogger CreateLogger(string categoryName)
        {
            var newLogger = _loggers = new EventSourceLogger(categoryName, _factoryID, _eventSource, _loggers);
            newLogger.Level = GetLoggerLevel(newLogger.CategoryName);
            return newLogger;
        }

        public void Dispose()
        {
            _filterChangeToken?.Dispose();

            // Turn off any logging
            for (var logger = _loggers; logger != null; logger = logger.Next)
            {
                logger.Level = LogLevel.None;
            }
        }

        // Sets the filtering for a particular logger provider
        internal void SetFilterSpec(LoggerFilterRule[] rules)
        {
            _rules = rules;

            // Update the levels of all the loggers to match what the filter specification asks for.
            for (var logger = _loggers; logger != null; logger = logger.Next)
            {
                logger.Level = GetLoggerLevel(logger.CategoryName);
            }
        }

        private LogLevel GetLoggerLevel(string loggerCategoryName)
        {
            if (!_handleFilters)
            {
                return LogLevel.Trace;
            }

            var level = LogLevel.None;
            foreach (var rule in _rules)
            {
                Debug.Assert(rule.LogLevel.HasValue);
                Debug.Assert(rule.ProviderName == GetType().FullName);

                if (rule.CategoryName == null)
                {
                    level = rule.LogLevel.Value;
                }
                else if (loggerCategoryName.StartsWith(rule.CategoryName))
                {
                    level = rule.LogLevel.Value;
                    break;
                }
            }

            return level;
        }
    }
}
