// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.Extensions.Logging.Test
{
    public class EnrichmentTests
    {
        [Fact]
        public void LogInformation_InvokesProcessor()
        {
            // Arrange
            var sink = new TestSink();
            var provider = new TestLoggerProvider(sink, isEnabled: true);

            List<string> logMessages = new List<string>();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Trace);
                builder.AddProvider(provider);
                builder.Enrich<string>("prop1", () => "Value!");
            });
            var loggerFactory = serviceCollection.BuildServiceProvider().GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("Test");

            // Act
            logger.LogInformation("Hello {Name}", "John Doe");

            // Assert
            Assert.Equal(1, sink.Writes.Count());
            Assert.True(sink.Writes.TryTake(out var write));
            Assert.Equal(LogLevel.Information, write.LogLevel);
            Assert.Equal("Hello John Doe", write.State.ToString());
            Assert.Equal(0, write.EventId);
            Assert.Null(write.Exception);

            Assert.Collection((IReadOnlyList<KeyValuePair<string, object?>>)write.State,
                p =>
                {
                    Assert.Equal("prop1", p.Key);
                    Assert.Equal("Value!", p.Value);
                },
                p =>
                {
                    Assert.Equal("Name", p.Key);
                    Assert.Equal("John Doe", p.Value);
                },
                p =>
                {
                    Assert.Equal("{OriginalFormat}", p.Key);
                    Assert.Equal("Hello {Name}", p.Value);
                });
        }

        [Fact]
        public void DefinedLog_InvokesProcessor()
        {
            // Arrange
            var sink = new TestSink();
            var provider = new TestLoggerProvider(sink, isEnabled: true);

            List<string> logMessages = new List<string>();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Trace);
                builder.AddProvider(provider);
                builder.AddProcessor((serviceProvider, processor) => new TestLogEntryProcessor(processor, m => logMessages.Add(m)));
            });
            var loggerFactory = serviceCollection.BuildServiceProvider().GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("Test");

            var definedLog = LoggerMessage.Define<string, int>(
                LogLevel.Information,
                new EventId(1, "Test"),
                "Hello {Name}. You are {Age} years old.");

            // Act
            definedLog(logger, "John Doe", 10, null);

            // Assert
            Assert.Collection(logMessages, m => Assert.Equal("Hello John Doe. You are 10 years old.", m));

            Assert.Equal(1, sink.Writes.Count());
            Assert.True(sink.Writes.TryTake(out var write));
            Assert.Equal(LogLevel.Information, write.LogLevel);
            Assert.Equal("Hello John Doe. You are 10 years old.", write.State.ToString());
            Assert.Equal(1, write.EventId);
            Assert.Null(write.Exception);
        }

        private sealed class TestLogEntryProcessor : ILogEntryProcessor
        {
            private readonly ILogEntryProcessor _nextProcessor;
            private readonly Action<string> _handleLogEntryCallback;

            public TestLogEntryProcessor(ILogEntryProcessor nextProcessor, Action<string> handleLogEntryCallback)
            {
                _nextProcessor = nextProcessor;
                _handleLogEntryCallback = handleLogEntryCallback;
            }

            public LogEntryHandler<TState> GetLogEntryHandler<TState>(ILogMetadata<TState>? metadata, out bool enabled, out bool dynamicEnabledCheckRequired)
            {
                var nextHandler = _nextProcessor.GetLogEntryHandler<TState>(metadata, out enabled, out dynamicEnabledCheckRequired);
                return new TestLogEntryHandler<TState>(nextHandler, _handleLogEntryCallback);
            }

            public ScopeHandler<TState> GetScopeHandler<TState>(ILogMetadata<TState>? metadata, out bool enabled) where TState : notnull
            {
                var nextHandler = _nextProcessor.GetScopeHandler<TState>(metadata, out enabled);
                return new TestScopeHandler<TState>(nextHandler, _handleLogEntryCallback);
            }

            public bool IsEnabled(LogLevel logLevel) => _nextProcessor.IsEnabled(logLevel);
        }

        private sealed class TestLogEntryHandler<TState> : LogEntryHandler<TState>
        {
            private readonly LogEntryHandler<TState> _nextHandler;
            private readonly Action<string> _handleLogEntryCallback;

            public TestLogEntryHandler(LogEntryHandler<TState> nextHandler, Action<string> handleLogEntryCallback)
            {
                _nextHandler = nextHandler;
                _handleLogEntryCallback = handleLogEntryCallback;
            }

            public override void HandleLogEntry(ref LogEntry<TState> logEntry)
            {
                var message = logEntry.Formatter(logEntry.State, logEntry.Exception);
                _handleLogEntryCallback(message);

                _nextHandler.HandleLogEntry(ref logEntry);
            }

            public override bool IsEnabled(LogLevel level)
            {
                return _nextHandler.IsEnabled(level);
            }
        }

        private sealed class TestScopeHandler<TState> : ScopeHandler<TState>
        {
            private readonly ScopeHandler<TState> _nextHandler;
            private readonly Action<string> _handleLogEntryCallback;

            public TestScopeHandler(ScopeHandler<TState> nextHandler, Action<string> handleLogEntryCallback)
            {
                _nextHandler = nextHandler;
                _handleLogEntryCallback = handleLogEntryCallback;
            }

            public override IDisposable? HandleBeginScope(ref TState state)
            {
                _handleLogEntryCallback(state.ToString());

                return _nextHandler.HandleBeginScope(ref state);
            }

            public override bool IsEnabled(LogLevel level)
            {
                return _nextHandler.IsEnabled(level);
            }
        }
    }
}
