// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Extensions.Http.Logging
{
    public class HttpClientLoggerTest
    {
        private const string Url = "http://www.example.com";
        private const int DefaultLoggerEventsPerRequest = 8; // 2 handlers (inner & outer) x 2 levels (msg & headers) x 2 times (request & response)

        private readonly ITestOutputHelper _output;

        public HttpClientLoggerTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task RemoveAllLoggers_Success()
        {
            var sink = new TestSink();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            serviceCollection.AddSingleton<ILoggerFactory>(new TestLoggerFactory(sink, enabled: true));
            serviceCollection.AddTransient<TestMessageHandler>();

            serviceCollection.AddHttpClient("NoLoggers")
                .ConfigurePrimaryHttpMessageHandler<TestMessageHandler>()
                .RemoveAllLoggers();

            var services = serviceCollection.BuildServiceProvider();
            var factory = services.GetRequiredService<IHttpClientFactory>();

            var client = factory.CreateClient("NoLoggers");

            _ = await client.GetAsync(Url);
            Assert.Equal(0, sink.Writes.Count);
        }

        [Fact]
        public async Task RemoveAllLoggers_OnlyForSpecifiedNames()
        {
            var sink = new TestSink();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            serviceCollection.AddSingleton<ILoggerFactory>(new TestLoggerFactory(sink, enabled: true));
            serviceCollection.AddTransient<TestMessageHandler>();

            serviceCollection.AddHttpClient("NoLoggers")
                .ConfigurePrimaryHttpMessageHandler<TestMessageHandler>()
                .RemoveAllLoggers();

            serviceCollection.AddHttpClient("DefaultLogger")
                .ConfigurePrimaryHttpMessageHandler<TestMessageHandler>();

            serviceCollection.AddHttpClient("NoLoggersV2")
                .ConfigurePrimaryHttpMessageHandler<TestMessageHandler>()
                .RemoveAllLoggers();

            var services = serviceCollection.BuildServiceProvider();
            var factory = services.GetRequiredService<IHttpClientFactory>();

            var defaultLoggerClient = factory.CreateClient("DefaultLogger");
            var noLoggersClient = factory.CreateClient("NoLoggers");
            var noLoggersClientV2 = factory.CreateClient("NoLoggersV2");

            _ = await noLoggersClient.GetAsync(Url);
            Assert.Equal(0, sink.Writes.Count);

            _ = await defaultLoggerClient.GetAsync(Url);
            Assert.Equal(DefaultLoggerEventsPerRequest, sink.Writes.Count);

            var previousMessagesCount = sink.Writes.Count;

            _ = await noLoggersClientV2.GetAsync(Url);
            Assert.Equal(0, sink.Writes.Count - previousMessagesCount);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task CustomLogger_LogsCorrectEvents(bool requestSuccessful, bool async)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient(_ =>
                new TestMessageHandler(_ => requestSuccessful
                    ? new HttpResponseMessage()
                    : throw new HttpRequestException("expected")));

            TestCountingLogger testLogger = async ? new TestCountingAsyncLogger() : new TestCountingLogger();

            serviceCollection.AddHttpClient("TestCountingLogger")
                .ConfigurePrimaryHttpMessageHandler<TestMessageHandler>()
                .AddLogger(_ => testLogger);

            var services = serviceCollection.BuildServiceProvider();
            var factory = services.GetRequiredService<IHttpClientFactory>();

            var client = factory.CreateClient("TestCountingLogger");

            if (requestSuccessful)
            {
                _ = await client.GetAsync(Url);
            }
            else
            {
                _ = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(Url));
            }

            AssertCounters(testLogger, requestCount: 1, requestSuccessful, async);

            if (requestSuccessful)
            {
                _ = await client.GetAsync(Url);
            }
            else
            {
                _ = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(Url));
            }

            AssertCounters(testLogger, requestCount: 2, requestSuccessful, async);
        }

        private void AssertCounters(TestCountingLogger testLogger, int requestCount, bool requestSuccessful, bool async)
        {
            if (async)
            {
                var asyncTestLogger = (TestCountingAsyncLogger)testLogger;
                Assert.Equal(requestCount, asyncTestLogger.AsyncRequestStartLogCount);
                Assert.Equal(requestSuccessful ? requestCount : 0, asyncTestLogger.AsyncRequestStopLogCount);
                Assert.Equal(requestSuccessful ? 0 : requestCount, asyncTestLogger.AsyncRequestFailedLogCount);

                Assert.Equal(0, testLogger.RequestStartLogCount);
                Assert.Equal(0, testLogger.RequestStopLogCount);
                Assert.Equal(0, testLogger.RequestFailedLogCount);
            }
            else
            {
                Assert.Equal(requestCount, testLogger.RequestStartLogCount);
                Assert.Equal(requestSuccessful ? requestCount : 0, testLogger.RequestStopLogCount);
                Assert.Equal(requestSuccessful ? 0 : requestCount, testLogger.RequestFailedLogCount);
            }
        }

#if NET
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task CustomLogger_LogsCorrectEvents_Sync(bool requestSuccessful, bool asyncSecondCall)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient(_ =>
                new TestMessageHandler(_ => requestSuccessful
                    ? new HttpResponseMessage()
                    : throw new HttpRequestException("expected")));

            var testLogger = new TestCountingAsyncLogger();
            serviceCollection.AddHttpClient("TestCountingLogger")
                .ConfigurePrimaryHttpMessageHandler<TestMessageHandler>()
                .AddLogger(_ => testLogger);

            var services = serviceCollection.BuildServiceProvider();
            var factory = services.GetRequiredService<IHttpClientFactory>();

            var client = factory.CreateClient("TestCountingLogger");

            if (requestSuccessful)
            {
                _ = client.Send(new HttpRequestMessage(HttpMethod.Get, Url));
            }
            else
            {
                _ = Assert.Throws<HttpRequestException>(() => client.Send(new HttpRequestMessage(HttpMethod.Get, Url)));
            }

            AssertCounters(testLogger, requestCount: 1, requestSuccessful, async: false);

            if (asyncSecondCall)
            {
                if (requestSuccessful)
                {
                    _ = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, Url));
                }
                else
                {
                    _ = await Assert.ThrowsAsync<HttpRequestException>(() => client.SendAsync(new HttpRequestMessage(HttpMethod.Get, Url)));
                }

                Assert.Equal(1, testLogger.AsyncRequestStartLogCount);
                Assert.Equal(requestSuccessful ? 1 : 0, testLogger.AsyncRequestStopLogCount);
                Assert.Equal(requestSuccessful ? 0 : 1, testLogger.AsyncRequestFailedLogCount);

                Assert.Equal(1, testLogger.RequestStartLogCount);
                Assert.Equal(requestSuccessful ? 1 : 0, testLogger.RequestStopLogCount);
                Assert.Equal(requestSuccessful ? 0 : 1, testLogger.RequestFailedLogCount);
            }
            else
            {
                if (requestSuccessful)
                {
                    _ = client.Send(new HttpRequestMessage(HttpMethod.Get, Url));
                }
                else
                {
                    _ = Assert.Throws<HttpRequestException>(() => client.Send(new HttpRequestMessage(HttpMethod.Get, Url)));
                }

                AssertCounters(testLogger, requestCount: 2, requestSuccessful, async: false);
            }
        }
#endif

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CustomLogger_WithContext_LogsCorrectEvents(bool requestSuccessful)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient(_ =>
                new TestMessageHandler(_ => requestSuccessful
                    ? new HttpResponseMessage()
                    : throw new HttpRequestException("expected")));

            var testLogger = new TestContextCountingLogger();
            serviceCollection.AddHttpClient("TestContextCountingLogger")
                .ConfigurePrimaryHttpMessageHandler<TestMessageHandler>()
                .AddLogger(_ => testLogger);

            var services = serviceCollection.BuildServiceProvider();
            var factory = services.GetRequiredService<IHttpClientFactory>();

            var client = factory.CreateClient("TestContextCountingLogger");

            if (requestSuccessful)
            {
                _ = await client.GetAsync(Url);
            }
            else
            {
                _ = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(Url));
            }

            Assert.Equal(1, testLogger.RequestStartLogged.Count);
            Assert.Equal(requestSuccessful ? 1 : 0, testLogger.RequestStopLogged.Count);
            Assert.Equal(requestSuccessful ? 0 : 1, testLogger.RequestFailedLogged.Count);

            if (requestSuccessful)
            {
                _ = await client.GetAsync(Url);
            }
            else
            {
                _ = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(Url));
            }

            Assert.Equal(2, testLogger.RequestStartLogged.Count);
            Assert.Equal(requestSuccessful ? 2 : 0, testLogger.RequestStopLogged.Count);
            Assert.Equal(requestSuccessful ? 0 : 2, testLogger.RequestFailedLogged.Count);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CustomLogger_ResolveFromDI_Success(bool removeDefaultLoggers)
        {
            var sink = new TestSink();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            serviceCollection.AddSingleton<ILoggerFactory>(new TestLoggerFactory(sink, enabled: true));
            serviceCollection.AddTransient<TestMessageHandler>();
            serviceCollection.AddSingleton<TestILoggerCustomLogger>();

            var builder = serviceCollection.AddHttpClient("TestILoggerCustomLogger");
            ConfigureHttpClient(builder);

            var builderV2 = serviceCollection.AddHttpClient("TestILoggerCustomLoggerV2");
            ConfigureHttpClient(builderV2);

            void ConfigureHttpClient(IHttpClientBuilder b)
            {
                b.ConfigurePrimaryHttpMessageHandler<TestMessageHandler>();
                if (removeDefaultLoggers)
                {
                    b.RemoveAllLoggers();
                }
                b.AddLogger<TestILoggerCustomLogger>();
            }

            var services = serviceCollection.BuildServiceProvider();
            var factory = services.GetRequiredService<IHttpClientFactory>();

            var client = factory.CreateClient("TestILoggerCustomLogger");
            var client2 = factory.CreateClient("TestILoggerCustomLoggerV2");

            var customLoggerName = typeof(TestILoggerCustomLogger).FullName.Replace('+', '.');
            int customLoggerEventsPerRequest = 2;
            int expectedEventsPerRequest = customLoggerEventsPerRequest + (removeDefaultLoggers ? 0 : DefaultLoggerEventsPerRequest);

            _ = await client.GetAsync(Url);
            Assert.Equal(expectedEventsPerRequest, sink.Writes.Count);
            Assert.Equal(customLoggerEventsPerRequest, sink.Writes.Count(w => w.LoggerName == customLoggerName));

            _ = await client.GetAsync(Url);
            Assert.Equal(2 * expectedEventsPerRequest, sink.Writes.Count);
            Assert.Equal(2 * customLoggerEventsPerRequest, sink.Writes.Count(w => w.LoggerName == customLoggerName));

            _ = await client2.GetAsync(Url);
            Assert.Equal(3 * expectedEventsPerRequest, sink.Writes.Count);
            Assert.Equal(3 * customLoggerEventsPerRequest, sink.Writes.Count(w => w.LoggerName == customLoggerName));
        }

        [Fact]
        public async Task WrapHandlerPipeline_LogCorrectNumberOfEvents()
        {
            var serviceCollection = new ServiceCollection();

            int counter = 1;
            serviceCollection.AddTransient(_ =>
                new TestMessageHandler(_ => ((counter++) % 3 == 0) // every 3rd request is successful
                    ? new HttpResponseMessage()
                    : throw new HttpRequestException("expected")));
            serviceCollection.AddTransient<TestRetryingHandler>();

            var innerLogger = new TestCountingLogger();
            var outerLogger = new TestCountingLogger();
            serviceCollection.AddHttpClient("WrapHandlerPipeline")
                .ConfigurePrimaryHttpMessageHandler<TestMessageHandler>()
                .AddHttpMessageHandler<TestRetryingHandler>()
                .AddLogger(_ => innerLogger, wrapHandlersPipeline: false)
                .AddLogger(_ => outerLogger, wrapHandlersPipeline: true);

            var services = serviceCollection.BuildServiceProvider();
            var factory = services.GetRequiredService<IHttpClientFactory>();

            var client = factory.CreateClient("WrapHandlerPipeline");

            _ = await client.GetAsync(Url);

            Assert.Equal(1, outerLogger.RequestStartLogCount);
            Assert.Equal(1, outerLogger.RequestStopLogCount);
            Assert.Equal(0, outerLogger.RequestFailedLogCount);

            Assert.Equal(3, innerLogger.RequestStartLogCount);
            Assert.Equal(1, innerLogger.RequestStopLogCount);
            Assert.Equal(2, innerLogger.RequestFailedLogCount);

            _ = await client.GetAsync(Url);

            Assert.Equal(2, outerLogger.RequestStartLogCount);
            Assert.Equal(2, outerLogger.RequestStopLogCount);
            Assert.Equal(0, outerLogger.RequestFailedLogCount);

            Assert.Equal(6, innerLogger.RequestStartLogCount);
            Assert.Equal(2, innerLogger.RequestStopLogCount);
            Assert.Equal(4, innerLogger.RequestFailedLogCount);
        }

        [Fact]
        public async Task LoggerFactoryWithHttpClientFactory_NoCircularDependency_PublicLogging()
        {
            var sink = new TestSink();
            var services = new ServiceCollection();
            services.AddTransient<TestMessageHandler>();
            services.AddSingleton<TestSink>(sink);
            services.AddSingleton<TestLoggerProvider>();

            services.AddLogging(b => b.SetMinimumLevel(LogLevel.Trace));
            services.AddSingleton<ILoggerProvider>(sp => sp.GetRequiredService<TestLoggerProvider>());
            services.AddHttpClient("TestLoggerProvider")
                .ConfigurePrimaryHttpMessageHandler<TestMessageHandler>()
                .RemoveAllLoggers();

            services.AddHttpClient("Production")
                .ConfigurePrimaryHttpMessageHandler<TestMessageHandler>();

            var serviceProvider = services.BuildServiceProvider();

            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            Assert.NotNull(loggerFactory);

            var prodClient = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("Production");

            _ = await prodClient.GetAsync(Url);

            Assert.Equal(DefaultLoggerEventsPerRequest, sink.Writes.Count(w => w.LoggerName.StartsWith("System.Net.Http.HttpClient.Production")));
            Assert.Equal(0, sink.Writes.Count(w => w.LoggerName.StartsWith("System.Net.Http.HttpClient.TestLoggerProvider")));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported), nameof(PlatformDetection.IsPreciseGcSupported))]
        public async Task LoggerFactoryWithHttpClientFactory_NoCircularDependency_DebugLogging()
        {
            var sink = new TestSink();
            var services = new ServiceCollection();
            services.AddTransient<TestMessageHandler>();
            services.AddSingleton<TestSink>(sink);
            services.AddSingleton<TestLoggerProvider>();

            services.AddLogging(b => b.SetMinimumLevel(LogLevel.Trace));
            services.AddSingleton<ILoggerProvider>(sp => sp.GetRequiredService<TestLoggerProvider>());
            services.AddHttpClient("TestLoggerProvider")
                .ConfigurePrimaryHttpMessageHandler<TestMessageHandler>()
                .RemoveAllLoggers();

            services.AddHttpClient("Production")
                .ConfigurePrimaryHttpMessageHandler<TestMessageHandler>();

            var serviceProvider = services.BuildServiceProvider();

            var httpClientFactory = (DefaultHttpClientFactory)serviceProvider.GetRequiredService<IHttpClientFactory>();
            var prodClient = httpClientFactory.CreateClient("Production");

            _ = await prodClient.GetAsync(Url);

            httpClientFactory.StartCleanupTimer(); // we need to create a timer instance before triggering cleanup; normally it happens after the first expiry
            httpClientFactory.CleanupTimer_Tick(); // trigger cleanup to write debug logs

            Assert.Equal(2, sink.Writes.Count(w => w.LoggerName == typeof(DefaultHttpClientFactory).FullName));
        }

        private sealed class TestLoggerProvider : ILoggerProvider
        {
            private readonly HttpClient _httpClient;
            private readonly TestSink _testSink;

            public TestLoggerProvider(IHttpClientFactory httpClientFactory, TestSink testSink)
            {
                _httpClient = httpClientFactory.CreateClient("TestLoggerProvider");
                _testSink = testSink;
                _testSink.MessageLogged += _ => _httpClient.GetAsync(Url).GetAwaiter().GetResult(); // simulating sending logs on the wire
            }

            public ILogger CreateLogger(string categoryName)
            {
                var logger = new TestLogger(categoryName, _testSink, enabled: true);
                return logger;
            }

            public void Dispose() => _httpClient.Dispose();
        }

        private class TestCountingLogger : IHttpClientLogger
        {
            public int RequestStartLogCount { get; private set; }
            public int RequestStopLogCount { get; private set; }
            public int RequestFailedLogCount { get; private set; }

            public object? LogRequestStart(HttpRequestMessage request)
            {
                RequestStartLogCount++;
                return null;
            }

            public void LogRequestStop(object? context, HttpRequestMessage request, HttpResponseMessage response, TimeSpan elapsed)
            {
                RequestStopLogCount++;
            }

            public void LogRequestFailed(object? context, HttpRequestMessage request, HttpResponseMessage? response, Exception exception, TimeSpan elapsed)
            {
                RequestFailedLogCount++;
            }
        }

        private class TestCountingAsyncLogger : TestCountingLogger, IHttpClientAsyncLogger
        {
            public int AsyncRequestStartLogCount { get; private set; }
            public int AsyncRequestStopLogCount { get; private set; }
            public int AsyncRequestFailedLogCount { get; private set; }

            public ValueTask<object?> LogRequestStartAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
            {
                AsyncRequestStartLogCount++;
                return new ValueTask<object?>((object?)null);
            }

            public ValueTask LogRequestStopAsync(object? context, HttpRequestMessage request, HttpResponseMessage response, TimeSpan elapsed,
                CancellationToken cancellationToken = default)
            {
                AsyncRequestStopLogCount++;
                return new ValueTask();
            }

            public ValueTask LogRequestFailedAsync(object? context, HttpRequestMessage request, HttpResponseMessage? response, Exception exception,
                TimeSpan elapsed, CancellationToken cancellationToken = default)
            {
                AsyncRequestFailedLogCount++;
                return new ValueTask();
            }
        }

        private class TestContextCountingLogger : IHttpClientLogger
        {
            private static int s_nextId;
            public HashSet<object> RequestStartLogged { get; } = new HashSet<object>();
            public HashSet<object> RequestStopLogged { get; } = new HashSet<object>();
            public HashSet<object> RequestFailedLogged { get; } = new HashSet<object>();
            private Dictionary<int, Context> _idToContext = new Dictionary<int, Context>();

            public object? LogRequestStart(HttpRequestMessage request)
            {
                var context = new Context(){ Id = Interlocked.Increment(ref s_nextId) };
                _idToContext[context.Id] = context;

                Assert.False(RequestStartLogged.Contains(context));
                RequestStartLogged.Add(context);

                return context;
            }

            public void LogRequestStop(object? context, HttpRequestMessage request, HttpResponseMessage response, TimeSpan elapsed)
            {
                Assert.NotNull(context);
                var c = Assert.IsType<Context>(context);
                Assert.True(_idToContext.ContainsKey(c.Id));
                Assert.Same(_idToContext[c.Id], c);

                Assert.False(RequestStopLogged.Contains(context));
                RequestStopLogged.Add(context);
            }

            public void LogRequestFailed(object? context, HttpRequestMessage request, HttpResponseMessage? response, Exception exception, TimeSpan elapsed)
            {
                Assert.NotNull(context);
                var c = Assert.IsType<Context>(context);
                Assert.True(_idToContext.ContainsKey(c.Id));
                Assert.Same(_idToContext[c.Id], c);

                Assert.False(RequestFailedLogged.Contains(context));
                RequestFailedLogged.Add(context);
            }

            private class Context
            {
                public int Id { get; set; }
            }
        }

        private class TestILoggerCustomLogger : IHttpClientLogger
        {
            private readonly ILogger _logger;
            public TestILoggerCustomLogger(ILoggerFactory loggerFactory)
            {
                _logger = loggerFactory.CreateLogger<TestILoggerCustomLogger>();
            }

            public object? LogRequestStart(HttpRequestMessage request)
            {
                _logger.LogInformation("LogRequestStart");
                return null;
            }

            public void LogRequestStop(object? context, HttpRequestMessage request, HttpResponseMessage response, TimeSpan elapsed)
                => _logger.LogInformation("LogRequestStop");

            public void LogRequestFailed(object? context, HttpRequestMessage request, HttpResponseMessage? response, Exception exception,TimeSpan elapsed)
                => _logger.LogInformation("LogRequestFailed");
        }

        private class TestRetryingHandler : DelegatingHandler
        {
            private const int MaxRetries = 5;

            private async Task<HttpResponseMessage> SendAsyncCore(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
            {
                for (int i = 1; i <= MaxRetries; ++i)
                {
                    try
                    {
                        if (async)
                        {
                            return await base.SendAsync(request, cancellationToken);
                        }
                        else
                        {
#if NET
                            return base.Send(request, cancellationToken);
#else
                            throw new NotImplementedException("unreachable");
#endif
                        }
                    }
                    catch (HttpRequestException)
                    {
                        if (i == MaxRetries)
                        {
                            throw;
                        }
                    }
                }
                throw new NotImplementedException("unreachable");
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => SendAsyncCore(request, async: true, cancellationToken);

#if NET
            protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
                => SendAsyncCore(request, async: false, cancellationToken).GetAwaiter().GetResult();
#endif
        }
    }
}
