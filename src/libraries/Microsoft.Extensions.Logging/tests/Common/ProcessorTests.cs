// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.Extensions.Logging.Test
{
    public class ProcessorTests
    {
        [Fact]
        public void AddConsole_BuilderExtensionAddsSingleSetOfServicesWhenCalledTwice()
        {
            List<string> logMessages = new List<string>();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(builder => builder.AddProcessor((serviceProvider, processor) => new TestLogEntryProcessor(processor, m => logMessages.Add(m))));
            var loggerFactory = serviceCollection.BuildServiceProvider().GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("Test");

            logger.LogInformation("Hello {Name}", "John Doe");

            Assert.Collection(logMessages, m => Assert.Equal("Hello John Doe", m));
        }

        [Fact]
        public void sdfsdfsdf()
        {
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

            var sdf = LoggerMessage.Define<string, int>(
                LogLevel.Information,
                new EventId(1, "Test"),
                "Hello {Name}. You are {Age} years old.");

            sdf(logger, "John Doe", 10, null);

            Assert.Collection(logMessages, m => Assert.Equal("Hello John Doe. You are 10 years old.", m));
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

            public LogEntryHandler<TState, TEnrichmentProperties> GetLogEntryHandler<TState, TEnrichmentProperties>(ILogMetadata<TState>? metadata, out bool enabled, out bool dynamicEnabledCheckRequired)
            {
                var nextHandler = _nextProcessor.GetLogEntryHandler<TState, TEnrichmentProperties>(metadata, out enabled, out dynamicEnabledCheckRequired);
                return new TestHandler<TState, TEnrichmentProperties>(nextHandler, _handleLogEntryCallback);
            }

            public bool IsEnabled(LogLevel logLevel) => true;
        }

        private class TestHandler<TState, TEnrichmentProperties> : LogEntryHandler<TState, TEnrichmentProperties>
        {
            LogEntryHandler<TState, TEnrichmentProperties> _nextHandler;
            private readonly Action<string> _handleLogEntryCallback;

            public TestHandler(LogEntryHandler<TState, TEnrichmentProperties> nextHandler, Action<string> handleLogEntryCallback)
            {
                _nextHandler = nextHandler;
                _handleLogEntryCallback = handleLogEntryCallback;
            }

            public override void HandleLogEntry(ref LogEntry<TState, TEnrichmentProperties> logEntry)
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
    }
}
