// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.Extensions.Logging.Test
{
    public class ProcessorTests
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
                builder.AddProcessor((serviceProvider, processor) => new TestLogEntryProcessor(processor, m => logMessages.Add(m)));
            });
            var loggerFactory = serviceCollection.BuildServiceProvider().GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("Test");

            // Act
            logger.LogInformation("Hello {Name}", "John Doe");

            // Assert
            Assert.Collection(logMessages, m => Assert.Equal("Hello John Doe", m));

            Assert.Equal(1, sink.Writes.Count());
            Assert.True(sink.Writes.TryTake(out var write));
            Assert.Equal(LogLevel.Information, write.LogLevel);
            Assert.Equal("Hello John Doe", write.State.ToString());
            Assert.Equal(0, write.EventId);
            Assert.Null(write.Exception);
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
    }
}
