// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace Microsoft.Extensions.Logging.EventSource
{
    /// <summary>
    /// A logger that writes messages to EventSource instance.
    /// </summary>
    /// <remarks>
    /// On Windows platforms EventSource will deliver messages using Event Tracing for Windows (ETW) events.
    /// On Linux EventSource will use LTTng (http://lttng.org) to deliver messages.
    /// </remarks>
    internal sealed class EventSourceLogger : ILogger
    {
        private static int _activityIds;
        private readonly LoggingEventSource _eventSource;
        private readonly int _factoryID;

        public EventSourceLogger(string categoryName, int factoryID, LoggingEventSource eventSource, EventSourceLogger? next)
        {
            CategoryName = categoryName;

            // Default is to turn on all the logging
            Level = LogLevel.Trace;

            _factoryID = factoryID;
            _eventSource = eventSource;
            Next = next;
        }

        public string CategoryName { get; }

        public LogLevel Level { get; set; }

        // Loggers created by a single provider form a linked list
        public EventSourceLogger? Next { get; }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None && logLevel >= Level;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }
            string? message = null;

            // See if they want the formatted message
            if (_eventSource.IsEnabled(EventLevel.Critical, LoggingEventSource.Keywords.FormattedMessage))
            {
                message = formatter(state, exception);
                _eventSource.FormattedMessage(
                    logLevel,
                    _factoryID,
                    CategoryName,
                    eventId.Id,
                    eventId.Name,
                    message);
            }

            // See if they want the message as its component parts.
            if (_eventSource.IsEnabled(EventLevel.Critical, LoggingEventSource.Keywords.Message))
            {
                ExceptionInfo exceptionInfo = GetExceptionInfo(exception);
                IReadOnlyList<KeyValuePair<string, string?>> arguments = GetProperties(state);

                _eventSource.Message(
                    logLevel,
                    _factoryID,
                    CategoryName,
                    eventId.Id,
                    eventId.Name,
                    exceptionInfo,
                    arguments);
            }

            // See if they want the json message
            if (_eventSource.IsEnabled(EventLevel.Critical, LoggingEventSource.Keywords.JsonMessage))
            {
                string exceptionJson = "{}";
                if (exception != null)
                {
                    ExceptionInfo exceptionInfo = GetExceptionInfo(exception);
                    KeyValuePair<string, string?>[] exceptionInfoData = new[]
                    {
                        new KeyValuePair<string, string?>("TypeName", exceptionInfo.TypeName),
                        new KeyValuePair<string, string?>("Message", exceptionInfo.Message),
                        new KeyValuePair<string, string?>("HResult", exceptionInfo.HResult.ToString()),
                        new KeyValuePair<string, string?>("VerboseMessage", exceptionInfo.VerboseMessage),
                    };
                    exceptionJson = ToJson(exceptionInfoData);
                }
                IReadOnlyList<KeyValuePair<string, string?>> arguments = GetProperties(state);
                message ??= formatter(state, exception);
                _eventSource.MessageJson(
                    logLevel,
                    _factoryID,
                    CategoryName,
                    eventId.Id,
                    eventId.Name,
                    exceptionJson,
                    ToJson(arguments),
                    message);
            }
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            if (!IsEnabled(LogLevel.Critical))
            {
                return NullScope.Instance;
            }

            int id = Interlocked.Increment(ref _activityIds);

            // If JsonMessage is on, use JSON format
            if (_eventSource.IsEnabled(EventLevel.Critical, LoggingEventSource.Keywords.JsonMessage))
            {
                IReadOnlyList<KeyValuePair<string, string?>> arguments = GetProperties(state);
                _eventSource.ActivityJsonStart(id, _factoryID, CategoryName, ToJson(arguments));
                return new ActivityScope(_eventSource, CategoryName, id, _factoryID, true);
            }

            if (_eventSource.IsEnabled(EventLevel.Critical, LoggingEventSource.Keywords.Message) ||
                _eventSource.IsEnabled(EventLevel.Critical, LoggingEventSource.Keywords.FormattedMessage))
            {
                IReadOnlyList<KeyValuePair<string, string?>> arguments = GetProperties(state);
                _eventSource.ActivityStart(id, _factoryID, CategoryName, arguments);
                return new ActivityScope(_eventSource, CategoryName, id, _factoryID, false);
            }

            return NullScope.Instance;
        }

        /// <summary>
        /// ActivityScope is just a IDisposable that knows how to send the ActivityStop event when it is
        /// desposed.  It is part of the BeginScope() support.
        /// </summary>
        private sealed class ActivityScope : IDisposable
        {
            private readonly string _categoryName;
            private readonly int _activityID;
            private readonly int _factoryID;
            private readonly bool _isJsonStop;
            private readonly LoggingEventSource _eventSource;

            public ActivityScope(LoggingEventSource eventSource, string categoryName, int activityID, int factoryID, bool isJsonStop)
            {
                _categoryName = categoryName;
                _activityID = activityID;
                _factoryID = factoryID;
                _isJsonStop = isJsonStop;
                _eventSource = eventSource;
            }

            public void Dispose()
            {
                if (_isJsonStop)
                {
                    _eventSource.ActivityJsonStop(_activityID, _factoryID, _categoryName);
                }
                else
                {
                    _eventSource.ActivityStop(_activityID, _factoryID, _categoryName);
                }
            }
        }

        /// <summary>
        /// 'serializes' a given exception into an ExceptionInfo (that EventSource knows how to serialize)
        /// </summary>
        /// <param name="exception">The exception to get information for.</param>
        /// <returns>ExceptionInfo object represending a .NET Exception</returns>
        /// <remarks>ETW does not support a concept of a null value. So we use an un-initialized object if there is no exception in the event data.</remarks>
        private static ExceptionInfo GetExceptionInfo(Exception? exception)
        {
            return exception != null ? new ExceptionInfo(exception) : ExceptionInfo.Empty;
        }

        /// <summary>
        /// Converts an ILogger state object into a set of key-value pairs (That can be send to a EventSource)
        /// </summary>
        private static IReadOnlyList<KeyValuePair<string, string?>> GetProperties(object? state)
        {
            if (state is IReadOnlyList<KeyValuePair<string, object?>> keyValuePairs)
            {
                var arguments = new KeyValuePair<string, string?>[keyValuePairs.Count];
                for (int i = 0; i < keyValuePairs.Count; i++)
                {
                    KeyValuePair<string, object?> keyValuePair = keyValuePairs[i];
                    arguments[i] = new KeyValuePair<string, string?>(keyValuePair.Key, keyValuePair.Value?.ToString());
                }
                return arguments;
            }

            return Array.Empty<KeyValuePair<string, string?>>();
        }

        private static string ToJson(IReadOnlyList<KeyValuePair<string, string?>> keyValues)
        {
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream);

            writer.WriteStartObject();
            foreach (KeyValuePair<string, string?> keyValue in keyValues)
            {
                writer.WriteString(keyValue.Key, keyValue.Value);
            }
            writer.WriteEndObject();

            writer.Flush();

            if (!stream.TryGetBuffer(out ArraySegment<byte> buffer))
            {
                buffer = new ArraySegment<byte>(stream.ToArray());
            }

            return Encoding.UTF8.GetString(buffer.Array!, buffer.Offset, buffer.Count);
        }
    }
}
