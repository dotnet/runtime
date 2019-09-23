// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging.EventLog.Internal;

namespace Microsoft.Extensions.Logging.EventLog
{
    /// <summary>
    /// A logger that writes messages to Windows Event Log.
    /// </summary>
    [Obsolete("This type is obsolete and will be removed in a future version. The recommended alternative is using EventLogLoggerProvider to construct loggers.")]
    public class EventLogLogger : ILogger
    {
        private readonly string _name;
        private readonly EventLogSettings _settings;
        private readonly IExternalScopeProvider _externalScopeProvider;

        private const string ContinuationString = "...";
        private readonly int _beginOrEndMessageSegmentSize;
        private readonly int _intermediateMessageSegmentSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLogLogger"/> class.
        /// </summary>
        /// <param name="name">The name of the logger.</param>
        public EventLogLogger(string name)
            : this(name, settings: new EventLogSettings())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLogLogger"/> class.
        /// </summary>
        /// <param name="name">The name of the logger.</param>
        /// <param name="settings">The <see cref="EventLogSettings"/>.</param>
        public EventLogLogger(string name, EventLogSettings settings)
            : this(name, settings, new LoggerExternalScopeProvider())
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLogLogger"/> class.
        /// </summary>
        /// <param name="name">The name of the logger.</param>
        /// <param name="settings">The <see cref="EventLogSettings"/>.</param>
        /// <param name="externalScopeProvider">The <see cref="IExternalScopeProvider"/>.</param>
        public EventLogLogger(string name, EventLogSettings settings, IExternalScopeProvider externalScopeProvider)
        {
            _name = string.IsNullOrEmpty(name) ? nameof(EventLogLogger) : name;
            _settings = settings;
            _externalScopeProvider = externalScopeProvider;

            var logName = string.IsNullOrEmpty(settings.LogName) ? "Application" : settings.LogName;
            var sourceName = string.IsNullOrEmpty(settings.SourceName) ? "Application" : settings.SourceName;
            var machineName = string.IsNullOrEmpty(settings.MachineName) ? "." : settings.MachineName;

            // Due to the following reasons, we cannot have these checks either here or in IsEnabled method:
            // 1. Log name & source name existence check only works on local computer.
            // 2. Source name existence check requires Administrative privileges.

            EventLog = settings.EventLog ?? new WindowsEventLog(logName, machineName, sourceName);

            // Examples:
            // 1. An error occu...
            // 2. ...esponse stream
            _beginOrEndMessageSegmentSize = EventLog.MaxMessageSize - ContinuationString.Length;

            // Example:
            // ...rred while writ...
            _intermediateMessageSegmentSize = EventLog.MaxMessageSize - 2 * ContinuationString.Length;
        }

        public IEventLog EventLog { get; }

        /// <inheritdoc />
        public IDisposable BeginScope<TState>(TState state)
        {
            return _externalScopeProvider?.Push(state);
        }

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None &&
                (_settings.Filter == null || _settings.Filter(_name, logLevel));
        }

        /// <inheritdoc />
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            var message = formatter(state, exception);

            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            message = _name + Environment.NewLine + message;

            if (exception != null)
            {
                message += Environment.NewLine + Environment.NewLine + exception;
            }

            _externalScopeProvider?.ForEachScope<object>((scope, _) => message += Environment.NewLine + scope, null);

            WriteMessage(message, GetEventLogEntryType(logLevel), eventId.Id);
        }

        // category '0' translates to 'None' in event log
        private void WriteMessage(string message, EventLogEntryType eventLogEntryType, int eventId)
        {
            if (message.Length <= EventLog.MaxMessageSize)
            {
                EventLog.WriteEntry(message, eventLogEntryType, eventId, category: 0);
                return;
            }

            var startIndex = 0;
            string messageSegment = null;
            while (true)
            {
                // Begin segment
                // Example: An error occu...
                if (startIndex == 0)
                {
                    messageSegment = message.Substring(startIndex, _beginOrEndMessageSegmentSize) + ContinuationString;
                    startIndex += _beginOrEndMessageSegmentSize;
                }
                else
                {
                    // Check if rest of the message can fit within the maximum message size
                    // Example: ...esponse stream
                    if ((message.Length - (startIndex + 1)) <= _beginOrEndMessageSegmentSize)
                    {
                        messageSegment = ContinuationString + message.Substring(startIndex);
                        EventLog.WriteEntry(messageSegment, eventLogEntryType, eventId, category: 0);
                        break;
                    }
                    else
                    {
                        // Example: ...rred while writ...
                        messageSegment =
                            ContinuationString
                            + message.Substring(startIndex, _intermediateMessageSegmentSize)
                            + ContinuationString;
                        startIndex += _intermediateMessageSegmentSize;
                    }
                }

                EventLog.WriteEntry(messageSegment, eventLogEntryType, eventId, category: 0);
            }
        }

        private EventLogEntryType GetEventLogEntryType(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Information:
                case LogLevel.Debug:
                case LogLevel.Trace:
                    return EventLogEntryType.Information;
                case LogLevel.Warning:
                    return EventLogEntryType.Warning;
                case LogLevel.Critical:
                case LogLevel.Error:
                    return EventLogEntryType.Error;
                default:
                    return EventLogEntryType.Information;
            }
        }

        private class NoopDisposable : IDisposable
        {
            public static NoopDisposable Instance = new NoopDisposable();

            public void Dispose()
            {
            }
        }
    }
}
