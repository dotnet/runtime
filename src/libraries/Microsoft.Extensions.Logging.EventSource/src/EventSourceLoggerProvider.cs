// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.Tracing;

namespace Microsoft.Extensions.Logging.EventSource
{
    /// <summary>
    /// The provider for the <see cref="EventSourceLogger"/>.
    /// </summary>
    [ProviderAlias("EventSource")]
    internal class EventSourceLoggerProvider : ILoggerProvider
    {
        // A small integer that uniquely identifies the LoggerFactory assoicated with this LoggingProvider.
        // Zero is illegal (it means we are uninitialized), and have to be added to the factory.
        private int _factoryID;

        private LogLevel _defaultLevel;
        private string _filterSpec;
        private EventSourceLogger _loggers; // Linked list of loggers that I have created
        private readonly LoggingEventSource _eventSource;

        public EventSourceLoggerProvider(LoggingEventSource eventSource, EventSourceLoggerProvider next = null)
        {
            if (eventSource == null)
            {
                throw new ArgumentNullException(nameof(eventSource));
            }
            _eventSource = eventSource;
            Next = next;
        }

        public EventSourceLoggerProvider Next { get; }

        /// <inheritdoc />
        public ILogger CreateLogger(string categoryName)
        {
            // need to check if the filter spec and internal event source level has changed
            // and update the _defaultLevel if it has
            _eventSource.ApplyFilterSpec();
            var newLogger = _loggers = new EventSourceLogger(categoryName, _factoryID, _eventSource, _loggers);
            newLogger.Level = ParseLevelSpecs(_filterSpec, _defaultLevel, newLogger.CategoryName);
            return newLogger;
        }

        public void Dispose()
        {
            SetFilterSpec(null); // Turn off any logging
        }

        // Sets the filtering for a particular logger provider
        internal void SetFilterSpec(string filterSpec)
        {
            _filterSpec = filterSpec;
            _defaultLevel = GetDefaultLevel();

            // Update the levels of all the loggers to match what the filter specification asks for.
            for (var logger = _loggers; logger != null; logger = logger.Next)
            {
                logger.Level = ParseLevelSpecs(filterSpec, _defaultLevel, logger.CategoryName);
            }

            if (_factoryID == 0)
            {
                // Compute an ID for the Factory.  It is its position in the list (starting at 1, we reserve 0 to mean unstarted).
                _factoryID = 1;
                for (var cur = Next; cur != null; cur = cur.Next)
                {
                    _factoryID++;
                }
            }
        }

        private LogLevel GetDefaultLevel()
        {
            var allMessageKeywords = LoggingEventSource.Keywords.Message | LoggingEventSource.Keywords.FormattedMessage | LoggingEventSource.Keywords.JsonMessage;

            if (_eventSource.IsEnabled(EventLevel.Informational, allMessageKeywords))
            {
                if (_eventSource.IsEnabled(EventLevel.Verbose, allMessageKeywords))
                {
                    return LogLevel.Debug;
                }
                else
                {
                    return LogLevel.Information;
                }
            }
            else
            {
                if (_eventSource.IsEnabled(EventLevel.Warning, allMessageKeywords))
                {
                    return LogLevel.Warning;
                }
                else
                {
                    if (_eventSource.IsEnabled(EventLevel.Error, allMessageKeywords))
                    {
                        return LogLevel.Error;
                    }
                    else
                    {
                        return LogLevel.Critical;
                    }
                }
            }
        }

        /// <summary>
        /// Given a set of specifications  Pat1:Level1;Pat1;Level2 ... Where
        /// Pat is a string pattern (a logger Name with a optional trailing wildcard * char)
        /// and Level is a number 0 (Trace) through 5 (Critical).
        ///
        /// The :Level can be omitted (thus Pat1;Pat2 ...) in which case the level is 1 (Debug).
        ///
        /// A completely emtry sting act like * (all loggers set to Debug level).
        ///
        /// The first speciciation that 'loggers' Name matches is used.
        /// </summary>
        private LogLevel ParseLevelSpecs(string filterSpec, LogLevel defaultLevel, string loggerName)
        {
            if (filterSpec == null)
            {
                return LoggingEventSource.LoggingDisabled;      // Null means disable.
            }
            if (filterSpec == string.Empty)
            {
                return defaultLevel;
            }

            var level = LoggingEventSource.LoggingDisabled;   // If the logger does not match something, it is off.

            // See if logger.Name  matches a _filterSpec pattern.
            var namePos = 0;
            var specPos = 0;
            for (;;)
            {
                if (namePos < loggerName.Length)
                {
                    if (filterSpec.Length <= specPos)
                    {
                        break;
                    }
                    var specChar = filterSpec[specPos++];
                    var nameChar = loggerName[namePos++];
                    if (specChar == nameChar)
                    {
                        continue;
                    }

                    // We allow wildcards at the end.
                    if (specChar == '*' && ParseLevel(defaultLevel, filterSpec, specPos, ref level))
                    {
                        return level;
                    }
                }
                else if (ParseLevel(defaultLevel, filterSpec, specPos, ref level))
                {
                    return level;
                }

                // Skip to the next spec in the ; separated list.
                specPos = filterSpec.IndexOf(';', specPos) + 1;
                if (specPos <= 0) // No ; done.
                {
                    break;
                }
                namePos = 0;    // Reset where we are searching in the name.
            }

            return level;
        }

        /// <summary>
        /// Parses the level specification (which should look like :N where n is a  number 0 (Trace)
        /// through 5 (Critical).   It can also be an empty string (which means 1 (Debug) and ';' marks
        /// the end of the specifcation This specification should start at spec[curPos]
        /// It returns the value in 'ret' and returns true if successful.  If false is returned ret is left unchanged.
        /// </summary>
        private bool ParseLevel(LogLevel defaultLevel, string spec, int specPos, ref LogLevel ret)
        {
            var endPos = spec.IndexOf(';', specPos);
            if (endPos < 0)
            {
                endPos = spec.Length;
            }

            if (specPos == endPos)
            {
                // No :Num spec means Debug
                ret = defaultLevel;
                return true;
            }
            if (spec[specPos++] != ':')
            {
                return false;
            }

            string levelStr = spec.Substring(specPos, endPos - specPos);
            int level;
            switch (levelStr)
            {
                case "Trace":
                    ret = LogLevel.Trace;
                    break;
                case "Debug":
                    ret = LogLevel.Debug;
                    break;
                case "Information":
                    ret = LogLevel.Information;
                    break;
                case "Warning":
                    ret = LogLevel.Warning;
                    break;
                case "Error":
                    ret = LogLevel.Error;
                    break;
                case "Critical":
                    ret = LogLevel.Critical;
                    break;
                default:
                    if (!int.TryParse(levelStr, out level))
                    {
                        return false;
                    }
                    if (!(LogLevel.Trace <= (LogLevel)level && (LogLevel)level <= LogLevel.None))
                    {
                        return false;
                    }
                    ret = (LogLevel)level;
                    break;
            }
            return true;
        }
    }
}
