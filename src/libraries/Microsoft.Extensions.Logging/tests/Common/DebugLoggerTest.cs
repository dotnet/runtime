// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging.Test;
using Xunit;

namespace Microsoft.Extensions.Logging
{
    public class DebugLoggerTest
    {
        [Fact]
        public void CallingBeginScopeOnLogger_ReturnsNonNullableInstance()
        {
            // Arrange
            var logger = CreateLogger();

            // Act
            var disposable = logger.BeginScope("Scope1");

            // Assert
            Assert.NotNull(disposable);
        }

        [Fact]
        public void CallingLogWithCurlyBracesAfterFormatter_DoesNotThrow()
        {
            // Arrange
            var logger = CreateLogger();
            var message = "{test string}";

            // Act
            logger.Log(LogLevel.Debug, 0, message, null, (s, e) => s);
        }

        [Fact]
        public static void IsEnabledReturnsCorrectValue()
        {
            // Arrange
            var logger = CreateLogger();

            // Assert
            Assert.False(logger.IsEnabled(LogLevel.None));
        }

        private static ILogger CreateLogger()
        {
            return TestLoggerBuilder.Create(builder => builder.AddDebug()).CreateLogger("Test");
        }
    }
}
