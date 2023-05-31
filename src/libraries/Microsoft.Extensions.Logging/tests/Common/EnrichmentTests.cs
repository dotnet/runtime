// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.Extensions.Logging.Test
{
    public class EnrichmentTests
    {
        [Fact]
        public void LogInformation_PropertyAddedToState()
        {
            // Arrange
            var sink = new TestSink();
            var provider = new TestLoggerProvider(sink, isEnabled: true);

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
                    Assert.Equal("Name", p.Key);
                    Assert.Equal("John Doe", p.Value);
                },
                p =>
                {
                    Assert.Equal("{OriginalFormat}", p.Key);
                    Assert.Equal("Hello {Name}", p.Value);
                },
                p =>
                {
                    Assert.Equal("prop1", p.Key);
                    Assert.Equal("Value!", p.Value);
                });
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(10)]
        public void LogInformation_PropertyCount_PropertyAddedToState(int enrichPropertyCount)
        {
            // Arrange
            var sink = new TestSink();
            var provider = new TestLoggerProvider(sink, isEnabled: true);

            var beforeMetadatas = new List<LogMetadataInfo>();
            var afterMetadatas = new List<LogMetadataInfo>();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Trace);
                builder.AddProvider(provider);

                for (var i = 0; i < enrichPropertyCount; i++)
                {
                    var capturedIndex = i;
                    builder.Enrich($"prop{capturedIndex}", () => $"value{capturedIndex}");
                }
                builder.AddProcessor((serviceProvider, processor) => new TestLogMetadataLogEntryProcessor(processor, m => afterMetadatas.Add(m)));
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
            Assert.Equal(1, sink.Writes.Count());
            Assert.True(sink.Writes.TryTake(out var write));
            Assert.Equal(LogLevel.Information, write.LogLevel);
            Assert.Equal("Hello John Doe. You are 10 years old.", write.State.ToString());
            Assert.Equal(1, write.EventId);
            Assert.Null(write.Exception);

            var values = Assert.IsAssignableFrom<IReadOnlyList<KeyValuePair<string, object?>>>(write.State);
            AssertPropertyAtIndex(values, 0, "Name", "John Doe");
            AssertPropertyAtIndex(values, 1, "Age", 10);
            AssertPropertyAtIndex(values, 2, "{OriginalFormat}", "Hello {Name}. You are {Age} years old.");
            for (var i = 0; i < enrichPropertyCount; i++)
            {
                AssertPropertyAtIndex(values, i + 3, $"prop{i}", $"value{i}");
            }

            var after = Assert.Single(afterMetadatas);
            Assert.Equal(enrichPropertyCount + 2, after.PropertyCount);
            Assert.Equal(enrichPropertyCount + 2, after.PropertyInfos.Length);

            void AssertPropertyAtIndex(IReadOnlyList<KeyValuePair<string, object?>> values, int index, string key, object value)
            {
                var kvp = values[index];
                Assert.Equal(key, kvp.Key);
                Assert.Equal(value, kvp.Value);

                kvp = values.ElementAt(index);
                Assert.Equal(key, kvp.Key);
                Assert.Equal(value, kvp.Value);
            }
        }

        internal class LogMetadataInfo
        {
            public int PropertyCount { get; set; }
            public LogPropertyInfo[] PropertyInfos { get; set; }
        }

        internal sealed class TestLogMetadataLogEntryProcessor : ILogEntryProcessor
        {
            private readonly ILogEntryProcessor _nextProcessor;
            private readonly Action<LogMetadataInfo> _handleLogEntryCallback;

            public TestLogMetadataLogEntryProcessor(ILogEntryProcessor nextProcessor, Action<LogMetadataInfo> handleLogEntryCallback)
            {
                _nextProcessor = nextProcessor;
                _handleLogEntryCallback = handleLogEntryCallback;
            }

            public LogEntryHandler<TState> GetLogEntryHandler<TState>(ILogMetadata<TState>? metadata, out bool enabled, out bool dynamicEnabledCheckRequired)
            {
                var propertyInfos = new List<LogPropertyInfo>();
                for (var i = 0; i < metadata.PropertyCount; i++)
                {
                    propertyInfos.Add(metadata.GetPropertyInfo(i));
                }
                _handleLogEntryCallback(new LogMetadataInfo
                {
                    PropertyCount = metadata.PropertyCount,
                    PropertyInfos = propertyInfos.ToArray()
                });

                var nextHandler = _nextProcessor.GetLogEntryHandler<TState>(metadata, out enabled, out dynamicEnabledCheckRequired);
                return new TestLogEntryHandler<TState>(nextHandler, null);
            }

            public ScopeHandler<TState> GetScopeHandler<TState>(ILogMetadata<TState>? metadata, out bool enabled) where TState : notnull
            {
                var nextHandler = _nextProcessor.GetScopeHandler<TState>(metadata, out enabled);
                return new TestScopeHandler<TState>(nextHandler, null);
            }

            public bool IsEnabled(LogLevel logLevel) => _nextProcessor.IsEnabled(logLevel);
        }
    }
}
