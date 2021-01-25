// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.Logging.Abstractions
{
    public class NullLoggerFactoryTest
    {
        [Fact]
        public void Create_GivesSameLogger()
        {
            // Arrange
            var factory = NullLoggerFactory.Instance;

            // Act
            var logger1 = factory.CreateLogger("Logger1");
            var logger2 = factory.CreateLogger("Logger2");

            // Assert
            Assert.Same(logger1, logger2);
        }
    }
}
