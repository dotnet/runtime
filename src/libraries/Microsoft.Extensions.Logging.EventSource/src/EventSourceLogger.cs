// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Threading;
using Newtonsoft.Json;

namespace Microsoft.Extensions.Logging.EventSource
{
    /// <summary>
    /// A logger that writes messages to EventSource instance.
    /// </summary>
    /// <remarks>
    /// On Windows platforms EventSource will deliver messages using Event Tracing for Windows (ETW) events.
    /// On Linux EventSource will use LTTng (http://lttng.org) to deliver messages.
    /// </remarks>
    internal class EventSourceLogger : ILogger
    {
        private static int _activityIds;
        private readonly LoggingEventSource _eventSource;
        private readonly int _factoryID;

        public EventSourceLogger(string categoryName, int factoryID, LoggingEventSource eventSource, EventSourceLogger next)
        {
            CategoryName = categoryName;

            // Default is to turn off logging
            Level = LogLevel.None;

            _factoryID = factoryID;
            _eventSource = eventSource;
            Next = next;
        }

        public string CategoryName { get; }

        public LogLevel Level { get; set; }

        // Loggers created by a single provider form a linked list
        public EventSourceLogger Next { get; }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None && logLevel >= Level;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            // See if they want the formatted message
            if (_eventSource.IsEnabled(EventLevel.Critical, LoggingEventSource.Keywords.FormattedMessage))
            {
                string message = formatter(state, exception);
                _eventSource.FormattedMessage(
                    logLevel,
                    _factoryID,
                    CategoryName,
                    eventId.ToString(),
                    message);
            }

            // See if they want the message as its component parts.
            if (_eventSource.IsEnabled(EventLevel.Critical, LoggingEventSource.Keywords.Message))
            {
                ExceptionInfo exceptionInfo = GetExceptionInfo(exception);
                IEnumerable<KeyValuePair<string, string>> arguments = GetProperties(state);

                _eventSource.Message(
                    logLevel,
                    _factoryID,
                    CategoryName,
                    eventId.ToString(),
                    exceptionInfo,
                    arguments);
            }

            // See if they want the json message
            if (_eventSource.IsEnabled(EventLevel.Critical, LoggingEventSource.Keywords.JsonMessage))
            {
                string exceptionJson = "{}";
                if (exception != null)
                {
                    var exceptionInfo = GetExceptionInfo(exception);
                    var exceptionInfoData = new KeyValuePair<string, string>[]
                    {
                        new KeyValuePair<string, string>("TypeName", exceptionInfo.TypeName),
                        new KeyValuePair<string, string>("Message", exceptionInfo.Message),
                        new KeyValuePair<string, string>("HResult", exceptionInfo.HResult.ToString()),
                        new KeyValuePair<string, string>("VerboseMessage", exceptionInfo.VerboseMessage),
                    };
                    exceptionJson = ToJson(exceptionInfoData);
                }
                IEnumerable<KeyValuePair<string, string>> arguments = GetProperties(state);
                _eventSource.MessageJson(
                    logLevel,
                    _factoryID,
                    CategoryName,
                    eventId.ToString(),
                    exceptionJson,
                    ToJson(arguments));
            }
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            if (!IsEnabled(LogLevel.Critical))
            {
                return NoopDisposable.Instance;
            }

            var id = Interlocked.Increment(ref _activityIds);

            // If JsonMessage is on, use JSON format
            if (_eventSource.IsEnabled(EventLevel.Critical, LoggingEventSource.Keywords.JsonMessage))
            {
                IEnumerable<KeyValuePair<string, string>> arguments = GetProperties(state);
                _eventSource.ActivityJsonStart(id, _factoryID, CategoryName, ToJson(arguments));
                return new ActivityScope(_eventSource, CategoryName, id, _factoryID, true);
            }
            else
            {
                IEnumerable<KeyValuePair<string, string>> arguments = GetProperties(state);
                _eventSource.ActivityStart(id, _factoryID, CategoryName, arguments);
                return new ActivityScope(_eventSource, CategoryName, id, _factoryID, false);
            }
        }

        /// <summary>
        /// ActivityScope is just a IDisposable that knows how to send the ActivityStop event when it is
        /// desposed.  It is part of the BeginScope() support.
        /// </summary>
        private class ActivityScope : IDisposable
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

        private class NoopDisposable : IDisposable
        {
            public static readonly NoopDisposable Instance = new NoopDisposable();

            public void Dispose()
            {
            }
        }

        /// <summary>
        /// 'serializes' a given exception into an ExceptionInfo (that EventSource knows how to serialize)
        /// </summary>
        /// <param name="exception"></param>
        /// <returns>ExceptionInfo object represending a .NET Exception</returns>
        /// <remarks>ETW does not support a concept of a null value. So we use an un-initialized object if there is no exception in the event data.</remarks>
        private ExceptionInfo GetExceptionInfo(Exception exception)
        {
            var exceptionInfo = new ExceptionInfo();
            if (exception != null)
            {
                exceptionInfo.TypeName = exception.GetType().FullName;
                exceptionInfo.Message = exception.Message;
                exceptionInfo.HResult = exception.HResult;
                exceptionInfo.VerboseMessage = exception.ToString();
            }
            return exceptionInfo;
        }

        /// <summary>
        /// Converts an ILogger state object into a set of key-value pairs (That can be send to a EventSource)
        /// </summary>
        private IEnumerable<KeyValuePair<string, string>> GetProperties(object state)
        {
            var arguments = new List<KeyValuePair<string, string>>();
            var asKeyValues = state as IEnumerable<KeyValuePair<string, object>>;
            if (asKeyValues != null)
            {
                foreach (var keyValue in asKeyValues)
                {
                    if (keyValue.Key != null)
                    {
                        arguments.Add(new KeyValuePair<string, string>(keyValue.Key, keyValue.Value?.ToString()));
                    }
                }
            }
            return arguments;
        }

        private string ToJson(IEnumerable<KeyValuePair<string, string>> keyValues)
        {
            var sw = new StringWriter();
            var writer = new JsonTextWriter(sw);
            writer.DateFormatString = "O"; // ISO 8601

            writer.WriteStartObject();
            foreach (var keyValue in keyValues)
            {
                writer.WritePropertyName(keyValue.Key, true);
                writer.WriteValue(keyValue.Value);
            }
            writer.WriteEndObject();
            return sw.ToString();
        }
    }
}
