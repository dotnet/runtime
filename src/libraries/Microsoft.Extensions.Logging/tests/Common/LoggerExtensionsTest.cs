// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.Extensions.Logging.Test
{
    public class LoggerExtensionsTest
    {
        private const string _name = "test";
        private const string _state = "testing";
        private const string _format = "{0}, {1}";
        private static Exception _exception = new InvalidOperationException();

        private TestLogger SetUp(TestSink sink)
        {
            // Arrange
            var logger = new TestLogger(_name, sink, enabled: true);
            return logger;
        }

        [Fact]
        public void MessageOnly_LogsCorrectValues()
        {
            // Arrange
            var sink = new TestSink();
            var logger = SetUp(sink);

            // Act
            logger.LogTrace(_state);
            logger.LogInformation(_state);
            logger.LogWarning(_state);
            logger.LogError(_state);
            logger.LogCritical(_state);
            logger.LogDebug(_state);

            // Assert
            Assert.Equal(6, sink.Writes.Count());

            Assert.True(sink.Writes.TryTake(out var trace));
            Assert.Equal(LogLevel.Trace, trace.LogLevel);
            Assert.Equal(_state, trace.State.ToString());
            Assert.Equal(0, trace.EventId);
            Assert.Null(trace.Exception);

            Assert.True(sink.Writes.TryTake(out var information));
            Assert.Equal(LogLevel.Information, information.LogLevel);
            Assert.Equal(_state, information.State.ToString());
            Assert.Equal(0, information.EventId);
            Assert.Null(information.Exception);

            Assert.True(sink.Writes.TryTake(out var warning));
            Assert.Equal(LogLevel.Warning, warning.LogLevel);
            Assert.Equal(_state, warning.State.ToString());
            Assert.Equal(0, warning.EventId);
            Assert.Null(warning.Exception);

            Assert.True(sink.Writes.TryTake(out var error));
            Assert.Equal(LogLevel.Error, error.LogLevel);
            Assert.Equal(_state, error.State.ToString());
            Assert.Equal(0, error.EventId);
            Assert.Null(error.Exception);

            Assert.True(sink.Writes.TryTake(out var critical));
            Assert.Equal(LogLevel.Critical, critical.LogLevel);
            Assert.Equal(_state, critical.State.ToString());
            Assert.Equal(0, critical.EventId);
            Assert.Null(critical.Exception);

            Assert.True(sink.Writes.TryTake(out var debug));
            Assert.Equal(LogLevel.Debug, debug.LogLevel);
            Assert.Equal(_state, debug.State.ToString());
            Assert.Equal(0, debug.EventId);
            Assert.Null(debug.Exception);
        }

        [Fact]
        public void FormatMessage_LogsCorrectValues()
        {
            // Arrange
            var sink = new TestSink();
            var logger = SetUp(sink);

            // Act
            logger.LogTrace(_format, "test1", "test2");
            logger.LogInformation(_format, "test1", "test2");
            logger.LogWarning(_format, "test1", "test2");
            logger.LogError(_format, "test1", "test2");
            logger.LogCritical(_format, "test1", "test2");
            logger.LogDebug(_format, "test1", "test2");

            // Assert
            Assert.Equal(6, sink.Writes.Count());

            Assert.True(sink.Writes.TryTake(out var trace));
            Assert.Equal(LogLevel.Trace, trace.LogLevel);
            Assert.Equal(string.Format(_format, "test1", "test2"), trace.State?.ToString());
            Assert.Equal(0, trace.EventId);
            Assert.Null(trace.Exception);

            Assert.True(sink.Writes.TryTake(out var information));
            Assert.Equal(LogLevel.Information, information.LogLevel);
            Assert.Equal(string.Format(_format, "test1", "test2"), information.State?.ToString());
            Assert.Equal(0, information.EventId);
            Assert.Null(information.Exception);

            Assert.True(sink.Writes.TryTake(out var warning));
            Assert.Equal(LogLevel.Warning, warning.LogLevel);
            Assert.Equal(string.Format(_format, "test1", "test2"), warning.State?.ToString());
            Assert.Equal(0, warning.EventId);
            Assert.Null(warning.Exception);

            Assert.True(sink.Writes.TryTake(out var error));
            Assert.Equal(LogLevel.Error, error.LogLevel);
            Assert.Equal(string.Format(_format, "test1", "test2"), error.State?.ToString());
            Assert.Equal(0, error.EventId);
            Assert.Null(error.Exception);

            Assert.True(sink.Writes.TryTake(out var critical));
            Assert.Equal(LogLevel.Critical, critical.LogLevel);
            Assert.Equal(string.Format(_format, "test1", "test2"), critical.State?.ToString());
            Assert.Equal(0, critical.EventId);
            Assert.Null(critical.Exception);

            Assert.True(sink.Writes.TryTake(out var debug));
            Assert.Equal(LogLevel.Debug, debug.LogLevel);
            Assert.Equal(string.Format(_format, "test1", "test2"), debug.State?.ToString());
            Assert.Equal(0, debug.EventId);
            Assert.Null(debug.Exception);
        }

        [Fact]
        public void MessageAndEventId_LogsCorrectValues()
        {
            // Arrange
            var sink = new TestSink();
            var logger = SetUp(sink);

            // Act
            logger.LogTrace(1, _state);
            logger.LogInformation(2, _state);
            logger.LogWarning(3, _state);
            logger.LogError(4, _state);
            logger.LogCritical(5, _state);
            logger.LogDebug(6, _state);

            // Assert
            Assert.Equal(6, sink.Writes.Count());

            Assert.True(sink.Writes.TryTake(out var trace));
            Assert.Equal(LogLevel.Trace, trace.LogLevel);
            Assert.Equal(_state, trace.State.ToString());
            Assert.Equal(1, trace.EventId);
            Assert.Null(trace.Exception);

            Assert.True(sink.Writes.TryTake(out var information));
            Assert.Equal(LogLevel.Information, information.LogLevel);
            Assert.Equal(_state, information.State.ToString());
            Assert.Equal(2, information.EventId);
            Assert.Null(information.Exception);

            Assert.True(sink.Writes.TryTake(out var warning));
            Assert.Equal(LogLevel.Warning, warning.LogLevel);
            Assert.Equal(_state, warning.State.ToString());
            Assert.Equal(3, warning.EventId);
            Assert.Null(warning.Exception);

            Assert.True(sink.Writes.TryTake(out var error));
            Assert.Equal(LogLevel.Error, error.LogLevel);
            Assert.Equal(_state, error.State.ToString());
            Assert.Equal(4, error.EventId);
            Assert.Null(error.Exception);

            Assert.True(sink.Writes.TryTake(out var critical));
            Assert.Equal(LogLevel.Critical, critical.LogLevel);
            Assert.Equal(_state, critical.State.ToString());
            Assert.Equal(5, critical.EventId);
            Assert.Null(critical.Exception);

            Assert.True(sink.Writes.TryTake(out var debug));
            Assert.Equal(LogLevel.Debug, debug.LogLevel);
            Assert.Equal(_state, debug.State.ToString());
            Assert.Equal(6, debug.EventId);
            Assert.Null(debug.Exception);
        }

        [Fact]
        public void FormatMessageAndEventId_LogsCorrectValues()
        {
            // Arrange
            var sink = new TestSink();
            var logger = SetUp(sink);

            // Act
            logger.LogTrace(1, _format, "test1", "test2");
            logger.LogInformation(2, _format, "test1", "test2");
            logger.LogWarning(3, _format, "test1", "test2");
            logger.LogError(4, _format, "test1", "test2");
            logger.LogCritical(5, _format, "test1", "test2");
            logger.LogDebug(6, _format, "test1", "test2");

            // Assert
            Assert.Equal(6, sink.Writes.Count());

            Assert.True(sink.Writes.TryTake(out var trace));
            Assert.Equal(LogLevel.Trace, trace.LogLevel);
            Assert.Equal(string.Format(_format, "test1", "test2"), trace.State?.ToString());
            Assert.Equal(1, trace.EventId);
            Assert.Null(trace.Exception);

            Assert.True(sink.Writes.TryTake(out var information));
            Assert.Equal(LogLevel.Information, information.LogLevel);
            Assert.Equal(string.Format(_format, "test1", "test2"), information.State?.ToString());
            Assert.Equal(2, information.EventId);
            Assert.Null(information.Exception);

            Assert.True(sink.Writes.TryTake(out var warning));
            Assert.Equal(LogLevel.Warning, warning.LogLevel);
            Assert.Equal(string.Format(_format, "test1", "test2"), warning.State?.ToString());
            Assert.Equal(3, warning.EventId);
            Assert.Null(warning.Exception);

            Assert.True(sink.Writes.TryTake(out var error));
            Assert.Equal(LogLevel.Error, error.LogLevel);
            Assert.Equal(string.Format(_format, "test1", "test2"), error.State?.ToString());
            Assert.Equal(4, error.EventId);
            Assert.Null(error.Exception);

            Assert.True(sink.Writes.TryTake(out var critical));
            Assert.Equal(LogLevel.Critical, critical.LogLevel);
            Assert.Equal(string.Format(_format, "test1", "test2"), critical.State?.ToString());
            Assert.Equal(5, critical.EventId);
            Assert.Null(critical.Exception);

            Assert.True(sink.Writes.TryTake(out var debug));
            Assert.Equal(LogLevel.Debug, debug.LogLevel);
            Assert.Equal(string.Format(_format, "test1", "test2"), debug.State?.ToString());
            Assert.Equal(6, debug.EventId);
            Assert.Null(debug.Exception);
        }

        [Fact]
        public void MessageAndError_LogsCorrectValues()
        {
            // Arrange
            var sink = new TestSink();
            var logger = SetUp(sink);

            // Act
            logger.LogTrace(_exception, _state);
            logger.LogInformation(_exception, _state);
            logger.LogWarning(_exception, _state);
            logger.LogError(_exception, _state);
            logger.LogCritical(_exception, _state);
            logger.LogDebug(_exception, _state);

            var writeList = sink.Writes.ToList();

            // Assert
            Assert.Equal(6, sink.Writes.Count);

            Assert.True(sink.Writes.TryTake(out var trace));
            Assert.Equal(LogLevel.Trace, trace.LogLevel);
            Assert.Equal(_state, trace.State.ToString());
            Assert.Equal(0, trace.EventId);
            Assert.Equal(_exception, trace.Exception);

            Assert.True(sink.Writes.TryTake(out var information));
            Assert.Equal(LogLevel.Information, information.LogLevel);
            Assert.Equal(_state, information.State.ToString());
            Assert.Equal(0, information.EventId);
            Assert.Equal(_exception, information.Exception);

            Assert.True(sink.Writes.TryTake(out var warning));
            Assert.Equal(LogLevel.Warning, warning.LogLevel);
            Assert.Equal(_state, warning.State.ToString());
            Assert.Equal(0, warning.EventId);
            Assert.Equal(_exception, warning.Exception);

            Assert.True(sink.Writes.TryTake(out var error));
            Assert.Equal(LogLevel.Error, error.LogLevel);
            Assert.Equal(_state, error.State.ToString());
            Assert.Equal(0, error.EventId);
            Assert.Equal(_exception, error.Exception);

            Assert.True(sink.Writes.TryTake(out var critical));
            Assert.Equal(LogLevel.Critical, critical.LogLevel);
            Assert.Equal(_state, critical.State.ToString());
            Assert.Equal(0, critical.EventId);
            Assert.Equal(_exception, critical.Exception);

            Assert.True(sink.Writes.TryTake(out var debug));
            Assert.Equal(LogLevel.Debug, debug.LogLevel);
            Assert.Equal(_state, debug.State.ToString());
            Assert.Equal(0, debug.EventId);
            Assert.Equal(_exception, debug.Exception);
        }

        [Fact]
        public void MessageEventIdAndError_LogsCorrectValues()
        {
            // Arrange
            var sink = new TestSink();
            var logger = SetUp(sink);

            // Act
            logger.LogTrace(1, _exception, _state);
            logger.LogInformation(2, _exception, _state);
            logger.LogWarning(3, _exception, _state);
            logger.LogError(4, _exception, _state);
            logger.LogCritical(5, _exception, _state);
            logger.LogDebug(6, _exception, _state);

            // Assert
            Assert.Equal(6, sink.Writes.Count());

            Assert.True(sink.Writes.TryTake(out var trace));
            Assert.Equal(LogLevel.Trace, trace.LogLevel);
            Assert.Equal(_state, trace.State.ToString());
            Assert.Equal(1, trace.EventId);
            Assert.Equal(_exception, trace.Exception);

            Assert.True(sink.Writes.TryTake(out var information));
            Assert.Equal(LogLevel.Information, information.LogLevel);
            Assert.Equal(_state, information.State.ToString());
            Assert.Equal(2, information.EventId);
            Assert.Equal(_exception, information.Exception);

            Assert.True(sink.Writes.TryTake(out var warning));
            Assert.Equal(LogLevel.Warning, warning.LogLevel);
            Assert.Equal(_state, warning.State.ToString());
            Assert.Equal(3, warning.EventId);
            Assert.Equal(_exception, warning.Exception);

            Assert.True(sink.Writes.TryTake(out var error));
            Assert.Equal(LogLevel.Error, error.LogLevel);
            Assert.Equal(_state, error.State.ToString());
            Assert.Equal(4, error.EventId);
            Assert.Equal(_exception, error.Exception);

            Assert.True(sink.Writes.TryTake(out var critical));
            Assert.Equal(LogLevel.Critical, critical.LogLevel);
            Assert.Equal(_state, critical.State.ToString());
            Assert.Equal(5, critical.EventId);
            Assert.Equal(_exception, critical.Exception);

            Assert.True(sink.Writes.TryTake(out var debug));
            Assert.Equal(LogLevel.Debug, debug.LogLevel);
            Assert.Equal(_state, debug.State.ToString());
            Assert.Equal(6, debug.EventId);
            Assert.Equal(_exception, debug.Exception);
        }

        [Fact]
        public void LogValues_LogsCorrectValues()
        {
            // Arrange
            var sink = new TestSink();
            var logger = SetUp(sink);
            var testLogValues = new TestLogValues()
            {
                Value = 1
            };

            // Act
            logger.LogTrace(0, testLogValues.ToString());
            logger.LogInformation(0, testLogValues.ToString());
            logger.LogWarning(0, testLogValues.ToString());
            logger.LogError(0, testLogValues.ToString());
            logger.LogCritical(0, testLogValues.ToString());
            logger.LogDebug(0, testLogValues.ToString());

            // Assert
            Assert.Equal(6, sink.Writes.Count());

            Assert.True(sink.Writes.TryTake(out var trace));
            Assert.Equal(LogLevel.Trace, trace.LogLevel);
            Assert.Equal(0, trace.EventId);
            Assert.Null(trace.Exception);
            Assert.Equal("Test 1", trace.Formatter(trace.State, trace.Exception));

            Assert.True(sink.Writes.TryTake(out var information));
            Assert.Equal(LogLevel.Information, information.LogLevel);
            Assert.Equal(0, information.EventId);
            Assert.Null(information.Exception);
            Assert.Equal("Test 1", information.Formatter(information.State, information.Exception));

            Assert.True(sink.Writes.TryTake(out var warning));
            Assert.Equal(LogLevel.Warning, warning.LogLevel);
            Assert.Equal(0, warning.EventId);
            Assert.Null(warning.Exception);
            Assert.Equal("Test 1", warning.Formatter(warning.State, warning.Exception));

            Assert.True(sink.Writes.TryTake(out var error));
            Assert.Equal(LogLevel.Error, error.LogLevel);
            Assert.Equal(0, error.EventId);
            Assert.Null(error.Exception);
            Assert.Equal("Test 1", error.Formatter(error.State, error.Exception));

            Assert.True(sink.Writes.TryTake(out var critical));
            Assert.Equal(LogLevel.Critical, critical.LogLevel);
            Assert.Equal(0, critical.EventId);
            Assert.Null(critical.Exception);
            Assert.Equal("Test 1", critical.Formatter(critical.State, critical.Exception));

            Assert.True(sink.Writes.TryTake(out var debug));
            Assert.Equal(LogLevel.Debug, debug.LogLevel);
            Assert.Equal(0, debug.EventId);
            Assert.Null(debug.Exception);
            Assert.Equal("Test 1", debug.Formatter(debug.State, debug.Exception));
        }

        [Fact]
        public void LogValuesAndEventId_LogsCorrectValues()
        {
            // Arrange
            var sink = new TestSink();
            var logger = SetUp(sink);
            var testLogValues = new TestLogValues()
            {
                Value = 1
            };

            // Act
            logger.LogTrace(1, testLogValues.ToString());
            logger.LogInformation(2, testLogValues.ToString());
            logger.LogWarning(3, testLogValues.ToString());
            logger.LogError(4, testLogValues.ToString());
            logger.LogCritical(5, testLogValues.ToString());
            logger.LogDebug(6, testLogValues.ToString());

            // Assert
            Assert.Equal(6, sink.Writes.Count());

            Assert.True(sink.Writes.TryTake(out var trace));
            Assert.Equal(LogLevel.Trace, trace.LogLevel);
            Assert.Equal(1, trace.EventId);
            Assert.Null(trace.Exception);
            Assert.Equal("Test 1", trace.Formatter(trace.State, trace.Exception));

            Assert.True(sink.Writes.TryTake(out var information));
            Assert.Equal(LogLevel.Information, information.LogLevel);
            Assert.Equal(2, information.EventId);
            Assert.Null(information.Exception);
            Assert.Equal("Test 1", information.Formatter(information.State, information.Exception));

            Assert.True(sink.Writes.TryTake(out var warning));
            Assert.Equal(LogLevel.Warning, warning.LogLevel);
            Assert.Equal(3, warning.EventId);
            Assert.Null(warning.Exception);
            Assert.Equal("Test 1", warning.Formatter(warning.State, warning.Exception));

            Assert.True(sink.Writes.TryTake(out var error));
            Assert.Equal(LogLevel.Error, error.LogLevel);
            Assert.Equal(4, error.EventId);
            Assert.Null(error.Exception);
            Assert.Equal("Test 1", error.Formatter(error.State, error.Exception));

            Assert.True(sink.Writes.TryTake(out var critical));
            Assert.Equal(LogLevel.Critical, critical.LogLevel);
            Assert.Equal(5, critical.EventId);
            Assert.Null(critical.Exception);
            Assert.Equal("Test 1", critical.Formatter(critical.State, critical.Exception));

            Assert.True(sink.Writes.TryTake(out var debug));
            Assert.Equal(LogLevel.Debug, debug.LogLevel);
            Assert.Equal(6, debug.EventId);
            Assert.Null(debug.Exception);
            Assert.Equal("Test 1", debug.Formatter(debug.State, debug.Exception));
        }

        [Fact]
        public void LogValuesAndError_LogsCorrectValues()
        {
            // Arrange
            var sink = new TestSink();
            var logger = SetUp(sink);
            var testLogValues = new TestLogValues()
            {
                Value = 1
            };

            // Act
            logger.LogTrace(0, _exception, testLogValues.ToString());
            logger.LogInformation(0, _exception, testLogValues.ToString());
            logger.LogWarning(0, _exception, testLogValues.ToString());
            logger.LogError(0, _exception, testLogValues.ToString());
            logger.LogCritical(0, _exception, testLogValues.ToString());
            logger.LogDebug(0, _exception, testLogValues.ToString());

            // Assert
            Assert.Equal(6, sink.Writes.Count());

            Assert.True(sink.Writes.TryTake(out var trace));
            Assert.Equal(LogLevel.Trace, trace.LogLevel);
            Assert.Equal(0, trace.EventId);
            Assert.Equal(_exception, trace.Exception);
            Assert.Equal(
                "Test 1",
                trace.Formatter(trace.State, trace.Exception));

            Assert.True(sink.Writes.TryTake(out var information));
            Assert.Equal(LogLevel.Information, information.LogLevel);
            Assert.Equal(0, information.EventId);
            Assert.Equal(_exception, information.Exception);
            Assert.Equal(
                "Test 1",
                information.Formatter(information.State, information.Exception));

            Assert.True(sink.Writes.TryTake(out var warning));
            Assert.Equal(LogLevel.Warning, warning.LogLevel);
            Assert.Equal(0, warning.EventId);
            Assert.Equal(_exception, warning.Exception);
            Assert.Equal(
                "Test 1",
                warning.Formatter(warning.State, warning.Exception));

            Assert.True(sink.Writes.TryTake(out var error));
            Assert.Equal(LogLevel.Error, error.LogLevel);
            Assert.Equal(0, error.EventId);
            Assert.Equal(_exception, error.Exception);
            Assert.Equal(
                "Test 1",
                error.Formatter(error.State, error.Exception));

            Assert.True(sink.Writes.TryTake(out var critical));
            Assert.Equal(LogLevel.Critical, critical.LogLevel);
            Assert.Equal(0, critical.EventId);
            Assert.Equal(_exception, critical.Exception);
            Assert.Equal(
                "Test 1",
                critical.Formatter(critical.State, critical.Exception));

            Assert.True(sink.Writes.TryTake(out var debug));
            Assert.Equal(LogLevel.Debug, debug.LogLevel);
            Assert.Equal(0, debug.EventId);
            Assert.Equal(_exception, debug.Exception);
            Assert.Equal(
                "Test 1",
                debug.Formatter(debug.State, debug.Exception));
        }

        [Fact]
        public void LogValuesEventIdAndError_LogsCorrectValues()
        {
            // Arrange
            var sink = new TestSink();
            var logger = SetUp(sink);
            var testLogValues = new TestLogValues()
            {
                Value = 1
            };

            // Act
            logger.LogTrace(1, _exception, testLogValues.ToString());
            logger.LogInformation(2, _exception, testLogValues.ToString());
            logger.LogWarning(3, _exception, testLogValues.ToString());
            logger.LogError(4, _exception, testLogValues.ToString());
            logger.LogCritical(5, _exception, testLogValues.ToString());
            logger.LogDebug(6, _exception, testLogValues.ToString());

            // Assert
            Assert.Equal(6, sink.Writes.Count());

            Assert.True(sink.Writes.TryTake(out var trace));
            Assert.Equal(LogLevel.Trace, trace.LogLevel);
            Assert.Equal(testLogValues.ToString(), trace.State.ToString());
            Assert.Equal(1, trace.EventId);
            Assert.Equal(_exception, trace.Exception);
            Assert.Equal(
                "Test 1",
                trace.Formatter(trace.State, trace.Exception));

            Assert.True(sink.Writes.TryTake(out var information));
            Assert.Equal(LogLevel.Information, information.LogLevel);
            Assert.Equal(testLogValues.ToString(), information.State.ToString());
            Assert.Equal(2, information.EventId);
            Assert.Equal(_exception, information.Exception);
            Assert.Equal(
                "Test 1",
                information.Formatter(information.State, information.Exception));

            Assert.True(sink.Writes.TryTake(out var warning));
            Assert.Equal(LogLevel.Warning, warning.LogLevel);
            Assert.Equal(testLogValues.ToString(), warning.State.ToString());
            Assert.Equal(3, warning.EventId);
            Assert.Equal(_exception, warning.Exception);
            Assert.Equal(
                "Test 1",
                warning.Formatter(warning.State, warning.Exception));

            Assert.True(sink.Writes.TryTake(out var error));
            Assert.Equal(LogLevel.Error, error.LogLevel);
            Assert.Equal(testLogValues.ToString(), error.State.ToString());
            Assert.Equal(4, error.EventId);
            Assert.Equal(_exception, error.Exception);
            Assert.Equal(
                "Test 1",
                error.Formatter(error.State, error.Exception));

            Assert.True(sink.Writes.TryTake(out var critical));
            Assert.Equal(LogLevel.Critical, critical.LogLevel);
            Assert.Equal(testLogValues.ToString(), critical.State.ToString());
            Assert.Equal(5, critical.EventId);
            Assert.Equal(_exception, critical.Exception);
            Assert.Equal(
                "Test 1",
                critical.Formatter(critical.State, critical.Exception));

            Assert.True(sink.Writes.TryTake(out var debug));
            Assert.Equal(LogLevel.Debug, debug.LogLevel);
            Assert.Equal(testLogValues.ToString(), debug.State.ToString());
            Assert.Equal(6, debug.EventId);
            Assert.Equal(_exception, debug.Exception);
            Assert.Equal(
                "Test 1",
                debug.Formatter(debug.State, debug.Exception));
        }

        [Theory]
        [InlineData(LogLevel.Trace)]
        [InlineData(LogLevel.Information)]
        [InlineData(LogLevel.Warning)]
        [InlineData(LogLevel.Error)]
        [InlineData(LogLevel.Critical)]
        [InlineData(LogLevel.Debug)]
        public void LogLevel_MessageOnly_LogsCorrectValues(LogLevel logLevel)
        {
            // Arrange
            var sink = new TestSink();
            var logger = SetUp(sink);

            // Act
            logger.Log(logLevel, _state);

            // Assert
            Assert.True(sink.Writes.TryTake(out var write));
            Assert.Equal(logLevel, write.LogLevel);
            Assert.Equal(_state, write.State.ToString());
            Assert.Equal(0, write.EventId);
            Assert.Null(write.Exception);
        }

        [Theory]
        [InlineData(LogLevel.Trace)]
        [InlineData(LogLevel.Information)]
        [InlineData(LogLevel.Warning)]
        [InlineData(LogLevel.Error)]
        [InlineData(LogLevel.Critical)]
        [InlineData(LogLevel.Debug)]
        public void LogLevel_FormatMessage_LogsCorrectValues(LogLevel logLevel)
        {
            // Arrange
            var sink = new TestSink();
            var logger = SetUp(sink);

            // Act
            logger.Log(logLevel, _format, "test1", "test2");

            // Assert
            Assert.True(sink.Writes.TryTake(out var write));
            Assert.Equal(logLevel, write.LogLevel);
            Assert.Equal(string.Format(_format, "test1", "test2"), write.State?.ToString());
            Assert.Equal(0, write.EventId);
            Assert.Null(write.Exception);
        }

        [Theory]
        [InlineData(LogLevel.Trace, 1)]
        [InlineData(LogLevel.Information, 2)]
        [InlineData(LogLevel.Warning, 3)]
        [InlineData(LogLevel.Error, 4)]
        [InlineData(LogLevel.Critical, 5)]
        [InlineData(LogLevel.Debug, 6)]
        public void LogLevel_MessageAndEventId_LogsCorrectValues(LogLevel logLevel, int eventId)
        {
            // Arrange
            var sink = new TestSink();
            var logger = SetUp(sink);

            // Act
            logger.Log(logLevel, eventId, _state);

            // Assert
            Assert.True(sink.Writes.TryTake(out var write));
            Assert.Equal(logLevel, write.LogLevel);
            Assert.Equal(_state, write.State.ToString());
            Assert.Equal(eventId, write.EventId);
            Assert.Null(write.Exception);
        }

        [Theory]
        [InlineData(LogLevel.Trace, 1)]
        [InlineData(LogLevel.Information, 2)]
        [InlineData(LogLevel.Warning, 3)]
        [InlineData(LogLevel.Error, 4)]
        [InlineData(LogLevel.Critical, 5)]
        [InlineData(LogLevel.Debug, 6)]
        public void LogLevel_FormatMessageAndEventId_LogsCorrectValues(LogLevel logLevel, int eventId)
        {
            // Arrange
            var sink = new TestSink();
            var logger = SetUp(sink);

            // Act
            logger.Log(logLevel, eventId, _format, "test1", "test2");

            // Assert
            Assert.True(sink.Writes.TryTake(out var write));
            Assert.Equal(logLevel, write.LogLevel);
            Assert.Equal(string.Format(_format, "test1", "test2"), write.State?.ToString());
            Assert.Equal(eventId, write.EventId);
            Assert.Null(write.Exception);
        }

        [Theory]
        [InlineData(LogLevel.Trace)]
        [InlineData(LogLevel.Information)]
        [InlineData(LogLevel.Warning)]
        [InlineData(LogLevel.Error)]
        [InlineData(LogLevel.Critical)]
        [InlineData(LogLevel.Debug)]
        public void LogLevel_MessageAndError_LogsCorrectValues(LogLevel logLevel)
        {
            // Arrange
            var sink = new TestSink();
            var logger = SetUp(sink);

            // Act
            logger.Log(logLevel, _exception, _state);

            // Assert
            Assert.True(sink.Writes.TryTake(out var write));
            Assert.Equal(logLevel, write.LogLevel);
            Assert.Equal(_state, write.State.ToString());
            Assert.Equal(0, write.EventId);
            Assert.Equal(_exception, write.Exception);
        }

        [Theory]
        [InlineData(LogLevel.Trace, 1)]
        [InlineData(LogLevel.Information, 2)]
        [InlineData(LogLevel.Warning, 3)]
        [InlineData(LogLevel.Error, 4)]
        [InlineData(LogLevel.Critical, 5)]
        [InlineData(LogLevel.Debug, 6)]
        public void LogLevel_MessageEventIdAndError_LogsCorrectValues(LogLevel logLevel, int eventId)
        {
            // Arrange
            var sink = new TestSink();
            var logger = SetUp(sink);

            // Act
            logger.Log(logLevel, eventId, _exception, _state);

            // Assert
            Assert.True(sink.Writes.TryTake(out var write));
            Assert.Equal(logLevel, write.LogLevel);
            Assert.Equal(_state, write.State.ToString());
            Assert.Equal(eventId, write.EventId);
            Assert.Equal(_exception, write.Exception);
        }

        [Theory]
        [InlineData(LogLevel.Trace)]
        [InlineData(LogLevel.Information)]
        [InlineData(LogLevel.Warning)]
        [InlineData(LogLevel.Error)]
        [InlineData(LogLevel.Critical)]
        [InlineData(LogLevel.Debug)]
        public void LogLevel_LogValues_LogsCorrectValues(LogLevel logLevel)
        {
            // Arrange
            var sink = new TestSink();
            var logger = SetUp(sink);
            var testLogValues = new TestLogValues()
            {
                Value = 1
            };

            // Act
            logger.Log(logLevel, 0, testLogValues.ToString());

            // Assert
            Assert.True(sink.Writes.TryTake(out var write));
            Assert.Equal(logLevel, write.LogLevel);
            Assert.Equal(0, write.EventId);
            Assert.Null(write.Exception);
            Assert.Equal("Test 1", write.Formatter(write.State, write.Exception));
        }

        [Theory]
        [InlineData(LogLevel.Trace, 1)]
        [InlineData(LogLevel.Information, 2)]
        [InlineData(LogLevel.Warning, 3)]
        [InlineData(LogLevel.Error, 4)]
        [InlineData(LogLevel.Critical, 5)]
        [InlineData(LogLevel.Debug, 6)]
        public void LogLevel_LogValuesAndEventId_LogsCorrectValues(LogLevel logLevel, int eventId)
        {
            // Arrange
            var sink = new TestSink();
            var logger = SetUp(sink);
            var testLogValues = new TestLogValues()
            {
                Value = 1
            };

            // Act
            logger.Log(logLevel, eventId, testLogValues.ToString());

            // Assert
            Assert.True(sink.Writes.TryTake(out var write));
            Assert.Equal(logLevel, write.LogLevel);
            Assert.Equal(eventId, write.EventId);
            Assert.Null(write.Exception);
            Assert.Equal("Test 1", write.Formatter(write.State, write.Exception));
        }

        [Theory]
        [InlineData(LogLevel.Trace)]
        [InlineData(LogLevel.Information)]
        [InlineData(LogLevel.Warning)]
        [InlineData(LogLevel.Error)]
        [InlineData(LogLevel.Critical)]
        [InlineData(LogLevel.Debug)]
        public void LogLevel_LogValuesAndError_LogsCorrectValues(LogLevel logLevel)
        {
            // Arrange
            var sink = new TestSink();
            var logger = SetUp(sink);
            var testLogValues = new TestLogValues()
            {
                Value = 1
            };

            // Act
            logger.Log(logLevel, 0, _exception, testLogValues.ToString());

            // Assert
            Assert.True(sink.Writes.TryTake(out var write));
            Assert.Equal(logLevel, write.LogLevel);
            Assert.Equal(0, write.EventId);
            Assert.Equal(_exception, write.Exception);
            Assert.Equal(
                "Test 1",
                write.Formatter(write.State, write.Exception));
        }

        [Theory]
        [InlineData(LogLevel.Trace, 1)]
        [InlineData(LogLevel.Information, 2)]
        [InlineData(LogLevel.Warning, 3)]
        [InlineData(LogLevel.Error, 4)]
        [InlineData(LogLevel.Critical, 5)]
        [InlineData(LogLevel.Debug, 6)]
        public void LogLevel_LogValuesEventIdAndError_LogsCorrectValues(LogLevel logLevel, int eventId)
        {
            // Arrange
            var sink = new TestSink();
            var logger = SetUp(sink);
            var testLogValues = new TestLogValues()
            {
                Value = 1
            };

            // Act
            logger.Log(logLevel, eventId, _exception, testLogValues.ToString());

            // Assert
            Assert.True(sink.Writes.TryTake(out var write));
            Assert.Equal(logLevel, write.LogLevel);
            Assert.Equal(testLogValues.ToString(), write.State.ToString());
            Assert.Equal(eventId, write.EventId);
            Assert.Equal(_exception, write.Exception);
            Assert.Equal(
                "Test 1",
                write.Formatter(write.State, write.Exception));
        }

        [Fact]
        public void BeginScope_CreatesScope_WithFormatStringValues()
        {
            // Arrange
            var testSink = new TestSink(
                writeEnabled: (writeContext) => true,
                beginEnabled: (beginScopeContext) => true);
            var logger = new TestLogger("TestLogger", testSink, enabled: true);
            var actionName = "App.Controllers.Home.Index";
            var expectedStringMessage = "Executing action " + actionName;

            // Act
            var scope = logger.BeginScope("Executing action {ActionName}", actionName);

            // Assert
            var sinkScope = Assert.Single(testSink.Scopes);
            Assert.IsType<FormattedLogValues>(sinkScope.Scope);
            var scopeState = (FormattedLogValues)sinkScope.Scope;
            Assert.Equal(expectedStringMessage, scopeState.ToString());
            Assert.True(scopeState.Count > 0);
            Assert.Contains(scopeState, (kvp) =>
            {
                return (string.Equals(kvp.Key, "ActionName") && string.Equals(kvp.Value?.ToString(), actionName));
            });
        }

        private class TestLogValues : IReadOnlyList<KeyValuePair<string, object>>
        {
            public KeyValuePair<string, object> this[int index]
            {
                get
                {
                    if (index == 0)
                    {
                        return new KeyValuePair<string, object>(nameof(Value), Value);
                    }
                    throw new IndexOutOfRangeException(nameof(index));
                }
            }

            public int Count
            {
                get
                {
                    return 1;
                }
            }

            public int Value { get; set; }

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                yield return this[0];
            }

            public override string ToString()
            {
                return "Test " + Value;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
