// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging.Testing;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Logging.Test
{
    public class LoggerFactoryExtensionsTest
    {
        [Fact]
        public void LoggerFactoryCreateOfT_CallsCreateWithCorrectName()
        {
            // Arrange
            var expected = typeof(TestType).FullName;

            var factory = new Mock<ILoggerFactory>();
            factory.Setup(f => f.CreateLogger(
                It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);

            // Act
            factory.Object.CreateLogger<TestType>();

            // Assert
            factory.Verify(f => f.CreateLogger(expected));
        }

        [Fact]
        public void LoggerFactoryCreateOfT_SingleGeneric_CallsCreateWithCorrectName()
        {
            // Arrange
            var factory = new Mock<ILoggerFactory>();
            factory.Setup(f => f.CreateLogger(It.Is<string>(
                x => x.Equals("Microsoft.Extensions.Logging.Test.GenericClass<Microsoft.Extensions.Logging.Test.TestType>"))))
            .Returns(new Mock<ILogger>().Object);

            var logger = factory.Object.CreateLogger<GenericClass<TestType>>();

            // Assert
            Assert.NotNull(logger);
        }

        [Fact]
        public void LoggerFactoryCreateOfT_TwoGenerics_CallsCreateWithCorrectName()
        {
            // Arrange
            var factory = new Mock<ILoggerFactory>();
            factory.Setup(f => f.CreateLogger(It.Is<string>(
                x => x.Equals("Microsoft.Extensions.Logging.Test.GenericClass<Microsoft.Extensions.Logging.Test.TestType, Microsoft.Extensions.Logging.Test.SecondTestType>"))))
            .Returns(new Mock<ILogger>().Object);

            var logger = factory.Object.CreateLogger<GenericClass<TestType, SecondTestType>>();

            // Assert
            Assert.NotNull(logger);
        }

        [Fact]
        public void CreatesLoggerName_WithoutGenericTypeArgumentsInformation()
        {
            // Arrange
            var fullName = typeof(GenericClass<string>).GetGenericTypeDefinition().FullName;
            var fullNameWithoutBacktick = fullName.Substring(0, fullName.IndexOf('`'));
            var testSink = new TestSink();
            var factory = new TestLoggerFactory(testSink, enabled: true);

            // Act
            var logger = factory.CreateLogger<GenericClass<string>>();
            logger.LogInformation("test message");

            // Assert
            var sinkWrite = Assert.Single(testSink.Writes);
            Assert.Equal(fullNameWithoutBacktick, sinkWrite.LoggerName);
        }

        [Fact]
        public void CreatesLoggerName_OnNestedGenericType_CreatesWithoutGenericTypeArgumentsInformation()
        {
            // Arrange
            var fullName = typeof(GenericClass<GenericClass<string>>).GetGenericTypeDefinition().FullName;
            var fullNameWithoutBacktick = fullName.Substring(0, fullName.IndexOf('`'));
            var testSink = new TestSink();
            var factory = new TestLoggerFactory(testSink, enabled: true);

            // Act
            var logger = factory.CreateLogger<GenericClass<GenericClass<string>>>();
            logger.LogInformation("test message");

            // Assert
            var sinkWrite = Assert.Single(testSink.Writes);
            Assert.Equal(fullNameWithoutBacktick, sinkWrite.LoggerName);
        }

        [Fact]
        public void CreatesLoggerName_OnMultipleTypeArgumentGenericType_CreatesWithoutGenericTypeArgumentsInformation()
        {
            // Arrange
            var fullName = typeof(GenericClass<string, string>).GetGenericTypeDefinition().FullName;
            var fullNameWithoutBacktick = fullName.Substring(0, fullName.IndexOf('`'));
            var testSink = new TestSink();
            var factory = new TestLoggerFactory(testSink, enabled: true);

            // Act
            var logger = factory.CreateLogger<GenericClass<string, string>>();
            logger.LogInformation("test message");

            // Assert
            var sinkWrite = Assert.Single(testSink.Writes);
            Assert.Equal(fullNameWithoutBacktick, sinkWrite.LoggerName);
        }


        [Fact]
        public void LoggerFactoryCreate_CallsCreateWithCorrectName()
        {
            // Arrange
            var expected = typeof(TestType).FullName;

            var factory = new Mock<ILoggerFactory>();
            factory.Setup(f => f.CreateLogger(
                It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);

            // Act
            factory.Object.CreateLogger(typeof(TestType));

            // Assert
            factory.Verify(f => f.CreateLogger(expected));
        }

        [Fact]
        public void LoggerFactoryCreate_SingleGeneric_CallsCreateWithCorrectName()
        {
            // Arrange
            var factory = new Mock<ILoggerFactory>();
            factory.Setup(f => f.CreateLogger(It.Is<string>(
                x => x.Equals("Microsoft.Extensions.Logging.Test.GenericClass"))))
            .Returns(new Mock<ILogger>().Object);

            var logger = factory.Object.CreateLogger(typeof(GenericClass<TestType>));

            // Assert
            Assert.NotNull(logger);
        }

        [Fact]
        public void LoggerFactoryCreate_TwoGenerics_CallsCreateWithCorrectName()
        {
            // Arrange
            var factory = new Mock<ILoggerFactory>();
            factory.Setup(f => f.CreateLogger(It.Is<string>(
                x => x.Equals("Microsoft.Extensions.Logging.Test.GenericClass"))))
            .Returns(new Mock<ILogger>().Object);

            var logger = factory.Object.CreateLogger(typeof(GenericClass<TestType, SecondTestType>));

            // Assert
            Assert.NotNull(logger);
        }
    }

    internal class TestType
    {
        // intentionally holds nothing
    }

    internal class SecondTestType
    {
        // intentionally holds nothing
    }

    internal class GenericClass<X, Y> where X : class where Y : class
    {
        // intentionally holds nothing
    }

    internal class GenericClass<X> where X : class
    {
        // intentionally holds nothing
    }
}
