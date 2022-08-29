// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Moq;

using Xunit;

namespace Microsoft.Extensions.Logging.Test
{
    public class TraceSourceLoggerTest
    {
        [Fact]
        public static void IsEnabledReturnsCorrectValue()
        {
            // Arrange
            var testSwitch = new SourceSwitch("TestSwitch", "Level will be set to warning for this test");
            testSwitch.Level = SourceLevels.Warning;

            var factory = TestLoggerBuilder.Create(builder => builder.AddTraceSource(testSwitch));

            // Act
            var logger = factory.CreateLogger("Test");

            // Assert
            Assert.False(logger.IsEnabled(LogLevel.None));
            Assert.True(logger.IsEnabled(LogLevel.Critical));
            Assert.True(logger.IsEnabled(LogLevel.Error));
            Assert.True(logger.IsEnabled(LogLevel.Warning));
            Assert.False(logger.IsEnabled(LogLevel.Information));
            Assert.False(logger.IsEnabled(LogLevel.Debug));
            Assert.False(logger.IsEnabled(LogLevel.Trace));
        }

        [Theory]
        [InlineData(SourceLevels.Warning, SourceLevels.Information, true)]
        [InlineData(SourceLevels.Information, SourceLevels.Information, true)]
        [InlineData(SourceLevels.Information, SourceLevels.Warning, true)]
        [InlineData(SourceLevels.Warning, SourceLevels.Warning, false)]
        public static void MultipleLoggers_IsEnabledReturnsCorrectValue(SourceLevels first, SourceLevels second, bool expected)
        {
            // Arrange
            var firstSwitch = new SourceSwitch("FirstSwitch", "First Test Switch");
            firstSwitch.Level = first;

            var secondSwitch = new SourceSwitch("SecondSwitch", "Second Test Switch");
            secondSwitch.Level = second;

            // Act
            var factory = TestLoggerBuilder.Create(builder => builder
                .AddTraceSource(firstSwitch)
                .AddTraceSource(secondSwitch));

            var logger = factory.CreateLogger("Test");

            // Assert
            Assert.Equal(expected, logger.IsEnabled(LogLevel.Information));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        [InlineData(true)]
        [InlineData(false)]
        public static void Log_Should_Add_Exception_To_Message_Whether_Formatter_Is_Null_Or_Not(bool shouldFormatterBeNull)
        {
            // Arrange
            Mock<TraceListener> traceListener = new Mock<TraceListener>();
            SourceSwitch sourceSwitch = new SourceSwitch("TestSwitch") {Level = SourceLevels.All};

            ILoggerFactory factory = TestLoggerBuilder.Create(builder => builder.AddTraceSource(sourceSwitch, traceListener.Object));
            ILogger logger = factory.CreateLogger("Test");

            const LogLevel logLevel = LogLevel.Information;
            EventId eventId = new EventId(1);
            const string message = "some log message";
            Exception exception = new Exception("Some error occurred");
            Func<string, Exception, string> formatter = shouldFormatterBeNull ? (Func<string, Exception, string>)null : (value, passedException) => value;

            string expectedMessage = $"{message} {exception}";

            // Act
            logger.Log(logLevel, eventId, message, exception, formatter);

            // Assert
            traceListener.Verify(listener => listener.TraceEvent(It.IsAny<TraceEventCache>(), It.IsAny<string>(), It.IsAny<TraceEventType>(), It.IsAny<int>(), expectedMessage), Times.Once);
        }
    }
}

