// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Logging.Generators.Tests
{
    /// <summary>
    /// A logger which captures the last log state logged to it.
    /// </summary>
    internal class MockLogger : ILogger
    {
        public LogLevel LastLogLevel { get; private set; }
        public EventId LastEventId { get; private set; }
        public object? LastState { get; private set; }
        public Exception? LastException { get; private set; }
        public string? LastFormattedString { get; private set; }
        public bool Enabled { get; set; }
        public int CallCount { get; private set; }

        /// <summary>
        /// Dummy disposable type, for use with BeginScope.
        /// </summary>
        private class Disposable : IDisposable
        {
            public void Dispose()
            {
                // nothing
            }
        }

        public MockLogger()
        {
            Reset();
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return new Disposable();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return Enabled;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            LastLogLevel = logLevel;
            LastEventId = eventId;
            LastState = state;
            LastException = exception;
            LastFormattedString = formatter(state, exception);
            CallCount++;
        }

        public void Reset()
        {
            LastLogLevel = (LogLevel)(-1);
            LastEventId = default;
            LastState = null;
            LastException = null;
            LastFormattedString = null;
            CallCount = 0;
            Enabled = true;
        }
    }
}
