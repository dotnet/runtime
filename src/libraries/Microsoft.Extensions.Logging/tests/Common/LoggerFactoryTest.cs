// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Logging.Test
{
    public class LoggerFactoryTest
    {
        [Fact]
        public void AddProvider_ThrowsAfterDisposed()
        {
            var factory = new LoggerFactory();
            factory.Dispose();

            Assert.Throws<ObjectDisposedException>(() => ((ILoggerFactory) factory).AddProvider(CreateProvider()));
        }

        [Fact]
        public void CreateLogger_ThrowsAfterDisposed()
        {
            var factory = new LoggerFactory();
            factory.Dispose();
            Assert.Throws<ObjectDisposedException>(() => factory.CreateLogger("d"));
        }

        private class TestLoggerFactory : LoggerFactory
        {
            public bool Disposed => CheckDisposed();
        }

        [Fact]
        public void Dispose_MultipleCallsNoop()
        {
            var factory = new TestLoggerFactory();
            factory.Dispose();
            Assert.True(factory.Disposed);
            factory.Dispose();
        }

        [Fact]
        public void Dispose_ProvidersAreDisposed()
        {
            // Arrange
            var factory = new LoggerFactory();
            var disposableProvider1 = CreateProvider();
            var disposableProvider2 = CreateProvider();

            factory.AddProvider(disposableProvider1);
            factory.AddProvider(disposableProvider2);

            // Act
            factory.Dispose();

            // Assert
            Mock.Get<IDisposable>(disposableProvider1)
                    .Verify(p => p.Dispose(), Times.Once());
            Mock.Get<IDisposable>(disposableProvider2)
                     .Verify(p => p.Dispose(), Times.Once());
        }

        private static ILoggerProvider CreateProvider()
        {
            var disposableProvider = new Mock<ILoggerProvider>();
            disposableProvider.As<IDisposable>()
                  .Setup(p => p.Dispose());
            return disposableProvider.Object;
        }

        [Fact]
        public void Dispose_ThrowException_SwallowsException()
        {
            // Arrange
            var factory = new LoggerFactory();
            var throwingProvider = new Mock<ILoggerProvider>();
            throwingProvider.As<IDisposable>()
                .Setup(p => p.Dispose())
                .Throws<Exception>();

            factory.AddProvider(throwingProvider.Object);

            // Act
            factory.Dispose();

            // Assert
            throwingProvider.As<IDisposable>()
                .Verify(p => p.Dispose(), Times.Once());
        }

        [Fact]
        public void CallsSetScopeProvider_OnSupportedProviders()
        {
            var loggerProvider = new ExternalScopeLoggerProvider();
            var loggerFactory = new LoggerFactory(new [] { loggerProvider });

            var logger = loggerFactory.CreateLogger("Logger");

            using (logger.BeginScope("Scope"))
            {
                using (logger.BeginScope("Scope2"))
                {
                    logger.LogInformation("Message");
                }
            }
            logger.LogInformation("Message2");

            Assert.Equal(loggerProvider.LogText,
                new[]
                {
                    "Message",
                    "Scope",
                    "Scope2",
                    "Message2",
                });
            Assert.NotNull(loggerProvider.ScopeProvider);
            Assert.Equal(0, loggerProvider.BeginScopeCalledTimes);
        }

        [Fact]
        public void BeginScope_ReturnsExternalSourceTokenDirectly()
        {
            var loggerProvider = new ExternalScopeLoggerProvider();
            var loggerFactory = new LoggerFactory(new [] { loggerProvider });

            var logger = loggerFactory.CreateLogger("Logger");

            var scope = logger.BeginScope("Scope");
            Assert.StartsWith(loggerProvider.ScopeProvider.GetType().FullName, scope.GetType().FullName);
        }

        [Fact]
        public void BeginScope_ReturnsInternalSourceTokenDirectly()
        {
            var loggerProvider = new InternalScopeLoggerProvider();
            var loggerFactory = new LoggerFactory(new[] { loggerProvider });
            var logger = loggerFactory.CreateLogger("Logger");
            var scope = logger.BeginScope("Scope");
            Assert.Contains("LoggerExternalScopeProvider+Scope", scope.GetType().FullName);
        }

        [Fact]
        public void BeginScope_ReturnsCompositeToken_ForMultipleLoggers()
        {
            var loggerProvider = new ExternalScopeLoggerProvider();
            var loggerProvider2 = new InternalScopeLoggerProvider();
            var loggerFactory = new LoggerFactory(new ILoggerProvider[] { loggerProvider, loggerProvider2});

            var logger = loggerFactory.CreateLogger("Logger");

            using (logger.BeginScope("Scope"))
            {
                using (logger.BeginScope("Scope2"))
                {
                    logger.LogInformation("Message");
                }
            }
            logger.LogInformation("Message2");

            Assert.Equal(loggerProvider.LogText,
                new[]
                {
                    "Message",
                    "Scope",
                    "Scope2",
                    "Message2",
                });

            Assert.Equal(loggerProvider2.LogText,
                new[]
                {
                    "Message",
                    "Scope",
                    "Scope2",
                    "Message2",
                });
        }

        [Fact]
        public void CreateDisposeDisposesInnerServiceProvider()
        {
            var disposed = false;
            var provider = new Mock<ILoggerProvider>();
            provider.Setup(p => p.Dispose()).Callback(() => disposed = true);

            var factory = LoggerFactory.Create(builder => builder.Services.AddSingleton(_=> provider.Object));
            factory.Dispose();

            Assert.True(disposed);
        }

        private class InternalScopeLoggerProvider : ILoggerProvider, ILogger
        {
            private IExternalScopeProvider _scopeProvider = new LoggerExternalScopeProvider();
            public List<string> LogText { get; set; } = new List<string>();

            public void Dispose()
            {
            }

            public ILogger CreateLogger(string categoryName)
            {
                return this;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                LogText.Add(formatter(state, exception));
                _scopeProvider.ForEachScope((scope, builder) => builder.Add(scope.ToString()), LogText);
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                return _scopeProvider.Push(state);
            }
        }

        private class ExternalScopeLoggerProvider : ILoggerProvider, ISupportExternalScope, ILogger
        {
            public void SetScopeProvider(IExternalScopeProvider scopeProvider)
            {
                ScopeProvider = scopeProvider;
            }

            public IExternalScopeProvider ScopeProvider { get; set; }
            public int BeginScopeCalledTimes { get; set; }
            public List<string> LogText { get; set; } = new List<string>();
            public void Dispose()
            {
            }

            public ILogger CreateLogger(string categoryName)
            {
                return this;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                LogText.Add(formatter(state, exception));
                ScopeProvider.ForEachScope((scope, builder) => builder.Add(scope.ToString()), LogText);
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                BeginScopeCalledTimes++;
                return null;
            }
        }
    }
}
