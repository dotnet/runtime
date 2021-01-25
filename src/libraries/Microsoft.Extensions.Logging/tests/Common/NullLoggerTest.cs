// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.Logging.Abstractions
{
    public class NullLoggerTest
    {
        [Fact]
        public void BeginScope_CanDispose()
        {
            // Arrange
            var logger = NullLogger.Instance;

            // Act & Assert
            using (logger.BeginScope("48656c6c6f20576f726c64"))
            {
            }
        }

        [Fact]
        public void IsEnabled_AlwaysFalse()
        {
            // Arrange
            var logger = NullLogger.Instance;

            // Act & Assert
            Assert.False(logger.IsEnabled(LogLevel.Debug));
            Assert.False(logger.IsEnabled(LogLevel.Trace));
            Assert.False(logger.IsEnabled(LogLevel.Information));
            Assert.False(logger.IsEnabled(LogLevel.Warning));
            Assert.False(logger.IsEnabled(LogLevel.Error));
            Assert.False(logger.IsEnabled(LogLevel.Critical));
        }

        [Fact]
        public void Write_Does_Nothing()
        {
            // Arrange
            var logger = NullLogger.Instance;
            bool isCalled = false;

            // Act
            logger.Log<object>(LogLevel.Trace, eventId: 0, state: null, exception: null, formatter: (ex, message) => { isCalled = true; return string.Empty; });

            // Assert
            Assert.False(isCalled);
        }
    }
}
