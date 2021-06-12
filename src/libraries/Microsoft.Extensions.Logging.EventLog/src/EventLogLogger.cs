// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Extensions.Logging.EventLog
{
    /// <summary>
    /// A logger that writes messages to Windows Event Log.
    /// </summary>
    internal sealed class EventLogLogger : ILogger
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
        /// <param name="settings">The <see cref="EventLogSettings"/>.</param>
        /// <param name="externalScopeProvider">The <see cref="IExternalScopeProvider"/>.</param>
        public EventLogLogger(string name, EventLogSettings settings, IExternalScopeProvider externalScopeProvider)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));

            _externalScopeProvider = externalScopeProvider;
            EventLog = settings.EventLog;

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

            string message = formatter(state, exception);

            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            StringBuilder builder = new StringBuilder()
                            .Append("Category: ")
                            .AppendLine(_name)
                            .Append("EventId: ")
                            .Append(eventId.Id)
                            .AppendLine();

            _externalScopeProvider?.ForEachScope((scope, sb) =>
            {
                if (scope is IEnumerable<KeyValuePair<string, object>> properties)
                {
                    foreach (KeyValuePair<string, object> pair in properties)
                    {
                        sb.Append(pair.Key).Append(": ").AppendLine(pair.Value?.ToString());
                    }
                }
                else if (scope != null)
                {
                    sb.AppendLine(scope.ToString());
                }
            },
            builder);

            builder.AppendLine()
            .AppendLine(message);

            if (exception != null)
            {
                builder.AppendLine().AppendLine("Exception: ").Append(exception).AppendLine();
            }

            WriteMessage(builder.ToString(), GetEventLogEntryType(logLevel), EventLog.DefaultEventId ?? eventId.Id);
        }

        // category '0' translates to 'None' in event log
        private void WriteMessage(string message, EventLogEntryType eventLogEntryType, int eventId)
        {
            if (message.Length <= EventLog.MaxMessageSize)
            {
                EventLog.WriteEntry(message, eventLogEntryType, eventId, category: 0);
                return;
            }

            int startIndex = 0;
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
#if NETSTANDARD
            Debug.Assert(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
#endif

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
    }
}
