// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Globalization;
using System.Diagnostics;
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

        private static string GetActivityLogString(ActivityTrackingOptions options)
        {
            Activity activity = Activity.Current;
            if (activity == null)
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();
            if ((options & ActivityTrackingOptions.SpanId) != 0)
            {
                sb.Append($"SpanId:{activity.GetSpanId()}");
            }

            if ((options & ActivityTrackingOptions.TraceId) != 0)
            {
                sb.Append(sb.Length > 0 ? $", TraceId:{activity.GetTraceId()}" : $"TraceId:{activity.GetTraceId()}");
            }

            if ((options & ActivityTrackingOptions.ParentId) != 0)
            {
                sb.Append(sb.Length > 0 ? $", ParentId:{activity.GetParentId()}" : $"ParentId:{activity.GetParentId()}");
            }

            if ((options & ActivityTrackingOptions.TraceState) != 0)
            {
                sb.Append(sb.Length > 0 ? $", TraceState:{activity.TraceStateString}" : $"TraceState:{activity.TraceStateString}");
            }

            if ((options & ActivityTrackingOptions.TraceFlags) != 0)
            {
                sb.Append(sb.Length > 0 ? $", TraceFlags:{activity.ActivityTraceFlags}" : $"TraceFlags:{activity.ActivityTraceFlags}");
            }

            return sb.ToString();
        }

        [Theory]
        [InlineData(ActivityTrackingOptions.SpanId)]
        [InlineData(ActivityTrackingOptions.TraceId)]
        [InlineData(ActivityTrackingOptions.ParentId)]
        [InlineData(ActivityTrackingOptions.TraceState)]
        [InlineData(ActivityTrackingOptions.TraceFlags)]
        [InlineData(ActivityTrackingOptions.SpanId | ActivityTrackingOptions.TraceId)]
        [InlineData(ActivityTrackingOptions.SpanId | ActivityTrackingOptions.ParentId)]
        [InlineData(ActivityTrackingOptions.SpanId | ActivityTrackingOptions.TraceState)]
        [InlineData(ActivityTrackingOptions.SpanId | ActivityTrackingOptions.TraceFlags)]
        [InlineData(ActivityTrackingOptions.TraceId | ActivityTrackingOptions.ParentId)]
        [InlineData(ActivityTrackingOptions.TraceId | ActivityTrackingOptions.TraceState)]
        [InlineData(ActivityTrackingOptions.TraceId | ActivityTrackingOptions.TraceFlags)]
        [InlineData(ActivityTrackingOptions.ParentId | ActivityTrackingOptions.TraceState)]
        [InlineData(ActivityTrackingOptions.ParentId | ActivityTrackingOptions.TraceFlags)]
        [InlineData(ActivityTrackingOptions.TraceState | ActivityTrackingOptions.TraceFlags)]
        [InlineData(ActivityTrackingOptions.SpanId | ActivityTrackingOptions.TraceId | ActivityTrackingOptions.ParentId)]
        [InlineData(ActivityTrackingOptions.SpanId | ActivityTrackingOptions.TraceId | ActivityTrackingOptions.TraceState)]
        [InlineData(ActivityTrackingOptions.SpanId | ActivityTrackingOptions.TraceId | ActivityTrackingOptions.TraceFlags)]
        [InlineData(ActivityTrackingOptions.SpanId | ActivityTrackingOptions.ParentId | ActivityTrackingOptions.TraceState)]
        [InlineData(ActivityTrackingOptions.SpanId | ActivityTrackingOptions.ParentId | ActivityTrackingOptions.TraceFlags)]
        [InlineData(ActivityTrackingOptions.SpanId | ActivityTrackingOptions.TraceState | ActivityTrackingOptions.TraceFlags)]
        [InlineData(ActivityTrackingOptions.TraceId | ActivityTrackingOptions.ParentId | ActivityTrackingOptions.TraceState)]
        [InlineData(ActivityTrackingOptions.TraceId | ActivityTrackingOptions.ParentId | ActivityTrackingOptions.TraceFlags)]
        [InlineData(ActivityTrackingOptions.TraceId | ActivityTrackingOptions.TraceState | ActivityTrackingOptions.TraceFlags)]
        [InlineData(ActivityTrackingOptions.SpanId | ActivityTrackingOptions.TraceId | ActivityTrackingOptions.ParentId | ActivityTrackingOptions.TraceState)]
        [InlineData(ActivityTrackingOptions.SpanId | ActivityTrackingOptions.TraceId | ActivityTrackingOptions.ParentId | ActivityTrackingOptions.TraceFlags)]
        [InlineData(ActivityTrackingOptions.TraceId | ActivityTrackingOptions.ParentId | ActivityTrackingOptions.TraceState | ActivityTrackingOptions.TraceFlags)]
        [InlineData(ActivityTrackingOptions.SpanId | ActivityTrackingOptions.TraceId | ActivityTrackingOptions.ParentId | ActivityTrackingOptions.TraceState | ActivityTrackingOptions.TraceFlags)]
        public void TestActivityIds(ActivityTrackingOptions options)
        {
            var loggerProvider = new ExternalScopeLoggerProvider();

            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                .Configure(o => o.ActivityTrackingOptions = options)
                .AddProvider(loggerProvider);
            });

            var logger = loggerFactory.CreateLogger("Logger");

            Activity a = new Activity("ScopeActivity");
            a.Start();
            string activity1String = GetActivityLogString(options);
            string activity2String;

            using (logger.BeginScope("Scope 1"))
            {
                logger.LogInformation("Message 1");
                Activity b = new Activity("ScopeActivity");
                b.Start();
                activity2String = GetActivityLogString(options);

                using (logger.BeginScope("Scope 2"))
                {
                    logger.LogInformation("Message 2");
                }
                b.Stop();
            }
            a.Stop();

            Assert.Equal(activity1String, loggerProvider.LogText[1]);
            Assert.Equal(activity2String, loggerProvider.LogText[4]);
        }

        [Fact]
        public void TestInvalidActivityTrackingOptions()
        {
            Assert.Throws<ArgumentException>(() =>
                LoggerFactory.Create(builder => { builder.Configure(o => o.ActivityTrackingOptions = (ActivityTrackingOptions) 0xFF00);})
            );
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

    internal static class ActivityExtensions
    {
        public static string GetSpanId(this Activity activity)
        {
            return activity.IdFormat switch
            {
                ActivityIdFormat.Hierarchical => activity.Id,
                ActivityIdFormat.W3C => activity.SpanId.ToHexString(),
                _ => null,
            } ?? string.Empty;
        }

        public static string GetTraceId(this Activity activity)
        {
            return activity.IdFormat switch
            {
                ActivityIdFormat.Hierarchical => activity.RootId,
                ActivityIdFormat.W3C => activity.TraceId.ToHexString(),
                _ => null,
            } ?? string.Empty;
        }

        public static string GetParentId(this Activity activity)
        {
            return activity.IdFormat switch
            {
                ActivityIdFormat.Hierarchical => activity.ParentId,
                ActivityIdFormat.W3C => activity.ParentSpanId.ToHexString(),
                _ => null,
            } ?? string.Empty;
        }
    }
}
