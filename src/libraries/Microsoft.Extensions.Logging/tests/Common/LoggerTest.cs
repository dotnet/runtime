// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Logging.Test
{
    public class LoggerTest
    {
        [Fact]
        public void Log_IgnoresExceptionInIntermediateLoggersAndThrowsAggregateException()
        {
            // Arrange
            var store = new List<string>();
            var loggerFactory = TestLoggerBuilder.Create(builder => builder
                .AddProvider(new CustomLoggerProvider("provider1", ThrowExceptionAt.None, store))
                .AddProvider(new CustomLoggerProvider("provider2", ThrowExceptionAt.Log, store))
                .AddProvider(new CustomLoggerProvider("provider3", ThrowExceptionAt.None, store)));

            var logger = loggerFactory.CreateLogger("Test");

            // Act
            var aggregateException = Assert.Throws<AggregateException>(() => logger.LogInformation("Hello!"));

            // Assert
            Assert.Equal(new[] { "provider1.Test-Hello!", "provider3.Test-Hello!" }, store);
            Assert.NotNull(aggregateException);
            Assert.StartsWith("An error occurred while writing to logger(s).", aggregateException.Message);
            Assert.Single(aggregateException.InnerExceptions);
            var exception = aggregateException.InnerExceptions[0];
            Assert.Equal("provider2.Test-Error occurred while logging data.", exception.Message);
        }

        [Fact]
        public void BeginScope_IgnoresExceptionInIntermediateLoggersAndThrowsAggregateException()
        {
            // Arrange
            var store = new List<string>();
            var loggerFactory = TestLoggerBuilder.Create(builder => builder
                .AddProvider(new CustomLoggerProvider("provider1", ThrowExceptionAt.None, store))
                .AddProvider(new CustomLoggerProvider("provider2", ThrowExceptionAt.BeginScope, store))
                .AddProvider(new CustomLoggerProvider("provider3", ThrowExceptionAt.None, store)));

            var logger = loggerFactory.CreateLogger("Test");

            // Act
            var aggregateException = Assert.Throws<AggregateException>(() => logger.BeginScope("Scope1"));

            // Assert
            Assert.Equal(new[] { "provider1.Test-Scope1", "provider3.Test-Scope1" }, store);
            Assert.NotNull(aggregateException);
            Assert.StartsWith("An error occurred while writing to logger(s).", aggregateException.Message);
            Assert.Single(aggregateException.InnerExceptions);
            var exception = aggregateException.InnerExceptions[0];
            Assert.Equal("provider2.Test-Error occurred while creating scope.", exception.Message);
        }

        [Fact]
        public void IsEnabled_IgnoresExceptionInIntermediateLoggers()
        {
            // Arrange
            var store = new List<string>();
            var loggerFactory = TestLoggerBuilder.Create(builder => builder
                .AddProvider(new CustomLoggerProvider("provider1", ThrowExceptionAt.None, store))
                .AddProvider(new CustomLoggerProvider("provider2", ThrowExceptionAt.IsEnabled, store))
                .AddProvider(new CustomLoggerProvider("provider3", ThrowExceptionAt.None, store)));

            var logger = loggerFactory.CreateLogger("Test");

            // Act
            var aggregateException = Assert.Throws<AggregateException>(() => logger.LogInformation("Hello!"));

            // Assert
            Assert.Equal(new[] { "provider1.Test-Hello!", "provider3.Test-Hello!" }, store);
            Assert.NotNull(aggregateException);
            Assert.StartsWith("An error occurred while writing to logger(s).", aggregateException.Message);
            Assert.Single(aggregateException.InnerExceptions);
            var exception = aggregateException.InnerExceptions[0];
            Assert.Equal("provider2.Test-Error occurred while checking if logger is enabled.", exception.Message);
        }

        [Fact]
        public void Log_AggregatesExceptionsFromMultipleLoggers()
        {
            // Arrange
            var store = new List<string>();
            var loggerFactory = TestLoggerBuilder.Create(builder => builder
                .AddProvider(new CustomLoggerProvider("provider1", ThrowExceptionAt.Log, store))
                .AddProvider(new CustomLoggerProvider("provider2", ThrowExceptionAt.Log, store)));

            var logger = loggerFactory.CreateLogger("Test");

            // Act
            var aggregateException = Assert.Throws<AggregateException>(() => logger.LogInformation("Hello!"));

            // Assert
            Assert.Empty(store);
            Assert.NotNull(aggregateException);
            Assert.StartsWith("An error occurred while writing to logger(s).", aggregateException.Message);
            var exceptions = aggregateException.InnerExceptions;
            Assert.Equal(2, exceptions.Count);
            Assert.Equal("provider1.Test-Error occurred while logging data.", exceptions[0].Message);
            Assert.Equal("provider2.Test-Error occurred while logging data.", exceptions[1].Message);
        }

        [Fact]
        public void LoggerCanGetProviderAfterItIsCreated()
        {
            // Arrange
            var store = new List<string>();
            var loggerFactory = new LoggerFactory();
            var logger = loggerFactory.CreateLogger("Test");

            loggerFactory.AddProvider(new CustomLoggerProvider("provider1", ThrowExceptionAt.None, store));

            // Act
            logger.LogInformation("Hello");

            // Assert
            Assert.Equal(new[] { "provider1.Test-Hello" }, store);
        }

        [Fact]
        public void ScopesAreNotCreatedForDisabledLoggers()
        {
            var provider = new Mock<ILoggerProvider>();
            var logger = new Mock<ILogger>();

            provider.Setup(loggerProvider => loggerProvider.CreateLogger(It.IsAny<string>()))
                .Returns(logger.Object);

            var factory = TestLoggerBuilder.Create(
                builder => {
                    builder.AddProvider(provider.Object);
                    // Disable all logs
                    builder.AddFilter(null, LogLevel.None);
                });

            var newLogger = factory.CreateLogger("Logger");
            using (newLogger.BeginScope("Scope"))
            {
            }

            provider.Verify(p => p.CreateLogger("Logger"), Times.Once);
            logger.Verify(l => l.BeginScope(It.IsAny<object>()), Times.Never);
        }

        [Fact]
        public void ScopesAreNotCreatedWhenScopesAreDisabled()
        {
            var provider = new Mock<ILoggerProvider>();
            var logger = new Mock<ILogger>();

            provider.Setup(loggerProvider => loggerProvider.CreateLogger(It.IsAny<string>()))
                .Returns(logger.Object);

            var factory = TestLoggerBuilder.Create(
                builder => {
                    builder.AddProvider(provider.Object);
                    builder.Services.Configure<LoggerFilterOptions>(options => options.CaptureScopes = false);
                });

            var newLogger = factory.CreateLogger("Logger");
            using (newLogger.BeginScope("Scope"))
            {
            }

            provider.Verify(p => p.CreateLogger("Logger"), Times.Once);
            logger.Verify(l => l.BeginScope(It.IsAny<object>()), Times.Never);
        }

        [Fact]
        public void ScopesAreNotCreatedInIScopeProviderWhenScopesAreDisabled()
        {
            var provider = new Mock<ILoggerProvider>();
            var logger = new Mock<ILogger>();

            IExternalScopeProvider externalScopeProvider = null;

            provider.Setup(loggerProvider => loggerProvider.CreateLogger(It.IsAny<string>()))
                .Returns(logger.Object);
            provider.As<ISupportExternalScope>().Setup(scope => scope.SetScopeProvider(It.IsAny<IExternalScopeProvider>()))
                .Callback((IExternalScopeProvider scopeProvider) => externalScopeProvider = scopeProvider);

            var factory = TestLoggerBuilder.Create(
                builder => {
                    builder.AddProvider(provider.Object);
                    builder.Services.Configure<LoggerFilterOptions>(options => options.CaptureScopes = false);
                });

            var newLogger = factory.CreateLogger("Logger");
            int scopeCount = 0;

            using (newLogger.BeginScope("Scope"))
            {
                externalScopeProvider.ForEachScope<object>((_, __) => scopeCount ++, null);
            }

            provider.Verify(p => p.CreateLogger("Logger"), Times.Once);
            logger.Verify(l => l.BeginScope(It.IsAny<object>()), Times.Never);
            Assert.Equal(0, scopeCount);
        }

        [Fact]
        public void CaptureScopesIsReadFromConfiguration()
        {
            var provider = new Mock<ILoggerProvider>();
            var logger = new Mock<ILogger>();
            var json = @"{ ""CaptureScopes"": ""false"" }";

            var config = TestConfiguration.Create(() => json);
            IExternalScopeProvider externalScopeProvider = null;

            provider.Setup(loggerProvider => loggerProvider.CreateLogger(It.IsAny<string>()))
                .Returns(logger.Object);
            provider.As<ISupportExternalScope>().Setup(scope => scope.SetScopeProvider(It.IsAny<IExternalScopeProvider>()))
                .Callback((IExternalScopeProvider scopeProvider) => externalScopeProvider = scopeProvider);

            var factory = TestLoggerBuilder.Create(
                builder => {
                    builder.AddProvider(provider.Object);
                    builder.AddConfiguration(config);
                });

            var newLogger = factory.CreateLogger("Logger");
            int scopeCount = 0;

            using (newLogger.BeginScope("Scope"))
            {
                externalScopeProvider.ForEachScope<object>((_, __) => scopeCount ++, null);
                Assert.Equal(0, scopeCount);
            }

            json = @"{ ""CaptureScopes"": ""true"" }";
            config.Reload();

            scopeCount = 0;
            using (newLogger.BeginScope("Scope"))
            {
                externalScopeProvider.ForEachScope<object>((_, __) => scopeCount ++, null);
                Assert.Equal(1, scopeCount);
            }
        }

        private class CustomLoggerProvider : ILoggerProvider
        {
            private readonly string _providerName;
            private readonly ThrowExceptionAt _throwExceptionAt;
            private readonly List<string> _store;

            public CustomLoggerProvider(string providerName, ThrowExceptionAt throwExceptionAt, List<string> store)
            {
                _providerName = providerName;
                _throwExceptionAt = throwExceptionAt;
                _store = store;
            }

            public ILogger CreateLogger(string name)
            {
                return new CustomLogger($"{_providerName}.{name}", _throwExceptionAt, _store);
            }

            public void Dispose()
            {
            }
        }

        private class CustomLogger : ILogger
        {
            private readonly string _name;
            private readonly ThrowExceptionAt _throwExceptionAt;
            private readonly List<string> _store;

            public CustomLogger(string name, ThrowExceptionAt throwExceptionAt, List<string> store)
            {
                _name = name;
                _throwExceptionAt = throwExceptionAt;
                _store = store;
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                if (_throwExceptionAt == ThrowExceptionAt.BeginScope)
                {
                    throw new InvalidOperationException($"{_name}-Error occurred while creating scope.");
                }
                _store.Add($"{_name}-{state}");

                return null;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                if (_throwExceptionAt == ThrowExceptionAt.IsEnabled)
                {
                    throw new InvalidOperationException($"{_name}-Error occurred while checking if logger is enabled.");
                }

                return true;
            }

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception exception,
                Func<TState, Exception, string> formatter)
            {
                if (!IsEnabled(logLevel))
                {
                    return;
                }

                if (_throwExceptionAt == ThrowExceptionAt.Log)
                {
                    throw new InvalidOperationException($"{_name}-Error occurred while logging data.");
                }
                _store.Add($"{_name}-{state}");
            }
        }

        private enum ThrowExceptionAt
        {
            None,
            BeginScope,
            Log,
            IsEnabled
        }
    }
}
