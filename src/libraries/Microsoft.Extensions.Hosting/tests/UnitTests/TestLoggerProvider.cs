// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting.Tests
{
    internal class TestLoggerProvider : ILoggerProvider
    {
        private readonly TestLogger _logger = new();

        public List<LogEvent> Events => _logger.Events;

        public ILogger CreateLogger(string categoryName)
        {
            return _logger;
        }

        public void Dispose() { }

        private class TestLogger : ILogger
        {
            internal List<LogEvent> Events = new();

            public IDisposable BeginScope<TState>(TState state) => new Scope();

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                Events.Add(new LogEvent()
                {
                    LogLevel = logLevel,
                    EventId = eventId,
                    Message = formatter(state, exception)
                });
            }

            private class Scope : IDisposable
            {
                public void Dispose()
                {
                }
            }
        }
    }

    internal class LogEvent
    {
        public LogLevel LogLevel { get; set; }
        public EventId EventId { get; set; }
        public string Message { get; set; }
    }
}
