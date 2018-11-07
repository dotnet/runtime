// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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