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

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task CustomLogger_LogsCorrectEvents(bool requestSuccessful, bool forceAsyncLogging)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient(_ =>
                new TestMessageHandler(_ => requestSuccessful
                    ? new HttpResponseMessage()
                    : throw new HttpRequestException("expected")));

            var testLogger = new TestCountingLogger(forceAsyncLogging);
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

            Assert.Equal(1, testLogger.RequestStartLogCount);
            Assert.Equal(requestSuccessful ? 1 : 0, testLogger.RequestStopLogCount);
            Assert.Equal(requestSuccessful ? 0 : 1, testLogger.RequestFailedLogCount);

            if (requestSuccessful)
            {
                _ = await client.GetAsync(Url);
            }
            else
            {
                _ = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(Url));
            }

            Assert.Equal(2, testLogger.RequestStartLogCount);
            Assert.Equal(requestSuccessful ? 2 : 0, testLogger.RequestStopLogCount);
            Assert.Equal(requestSuccessful ? 0 : 2, testLogger.RequestFailedLogCount);
        }

#if NET5_0_OR_GREATER
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        [InlineData(false)]
        [InlineData(true)]
        public void CustomLogger_LogsCorrectEvents_Sync(bool requestSuccessful)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient(_ =>
                new TestMessageHandler(_ => requestSuccessful
                    ? new HttpResponseMessage()
                    : throw new HttpRequestException("expected")));

            var testLogger = new TestCountingLogger();
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

            Assert.Equal(1, testLogger.RequestStartLogCount);
            Assert.Equal(requestSuccessful ? 1 : 0, testLogger.RequestStopLogCount);
            Assert.Equal(requestSuccessful ? 0 : 1, testLogger.RequestFailedLogCount);

            if (requestSuccessful)
            {
                _ = client.Send(new HttpRequestMessage(HttpMethod.Get, Url));
            }
            else
            {
                _ = Assert.Throws<HttpRequestException>(() => client.Send(new HttpRequestMessage(HttpMethod.Get, Url)));
            }

            Assert.Equal(2, testLogger.RequestStartLogCount);
            Assert.Equal(requestSuccessful ? 2 : 0, testLogger.RequestStopLogCount);
            Assert.Equal(requestSuccessful ? 0 : 2, testLogger.RequestFailedLogCount);
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

        private static ValueTask<object?> GetSuccessfulTaskWithResult(bool async = false, object? result = null)
        {
            if (async)
            {
                return DelayWithResult(result);
            }
            return new ValueTask<object?>(result);
        }

        private static async ValueTask<object?> DelayWithResult(object? result)
        {
            await Task.Delay(1).ConfigureAwait(false);
            return result;
        }

        private static ValueTask GetSuccessfulTask(bool async = false)
        {
            if (async)
            {
                return new ValueTask(Task.Delay(1));
            }
            return new ValueTask();
        }

        private class TestCountingLogger : IHttpClientLogger
        {
            private bool _async;
            public int RequestStartLogCount { get; private set; }
            public int RequestStopLogCount { get; private set; }
            public int RequestFailedLogCount { get; private set; }

            public TestCountingLogger(bool async = false)
            {
                _async = async;
            }

            public ValueTask<object?> LogRequestStartAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
            {
                RequestStartLogCount++;
                return GetSuccessfulTaskWithResult(_async);
            }

            public ValueTask LogRequestStopAsync(object? context, HttpRequestMessage request, HttpResponseMessage response, TimeSpan elapsed,
                CancellationToken cancellationToken = default)
            {
                RequestStopLogCount++;
                return GetSuccessfulTask(_async);
            }

            public ValueTask LogRequestFailedAsync(object? context, HttpRequestMessage request, HttpResponseMessage? response, Exception exception,
                TimeSpan elapsed, CancellationToken cancellationToken = default)
            {
                RequestFailedLogCount++;
                return GetSuccessfulTask(_async);
            }
        }

        private class TestContextCountingLogger : IHttpClientLogger
        {
            private static int s_nextId;
            public HashSet<object> RequestStartLogged { get; } = new HashSet<object>();
            public HashSet<object> RequestStopLogged { get; } = new HashSet<object>();
            public HashSet<object> RequestFailedLogged { get; } = new HashSet<object>();
            private Dictionary<int, Context> _idToContext = new Dictionary<int, Context>();

            public ValueTask<object?> LogRequestStartAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
            {
                var context = new Context(){ Id = Interlocked.Increment(ref s_nextId) };
                _idToContext[context.Id] = context;

                Assert.False(RequestStartLogged.Contains(context));
                RequestStartLogged.Add(context);

                return GetSuccessfulTaskWithResult(result: context);
            }

            public ValueTask LogRequestStopAsync(object? context, HttpRequestMessage request, HttpResponseMessage response, TimeSpan elapsed,
                CancellationToken cancellationToken = default)
            {
                Assert.NotNull(context);
                var c = Assert.IsType<Context>(context);
                Assert.True(_idToContext.ContainsKey(c.Id));
                Assert.Same(_idToContext[c.Id], c);

                Assert.False(RequestStopLogged.Contains(context));
                RequestStopLogged.Add(context);

                return GetSuccessfulTask();
            }

            public ValueTask LogRequestFailedAsync(object? context, HttpRequestMessage request, HttpResponseMessage? response, Exception exception,
                TimeSpan elapsed, CancellationToken cancellationToken = default)
            {
                Assert.NotNull(context);
                var c = Assert.IsType<Context>(context);
                Assert.True(_idToContext.ContainsKey(c.Id));
                Assert.Same(_idToContext[c.Id], c);

                Assert.False(RequestFailedLogged.Contains(context));
                RequestFailedLogged.Add(context);

                return GetSuccessfulTask();
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

            public TestILoggerCustomLogger(ILoggerFactory loggerFactory, string loggerName)
            {
                _logger = loggerFactory.CreateLogger(loggerName);
            }

            public ValueTask<object?> LogRequestStartAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
            {
                _logger.LogInformation("LogRequestStartAsync");
                return GetSuccessfulTaskWithResult();
            }

            public ValueTask LogRequestStopAsync(object? context, HttpRequestMessage request, HttpResponseMessage response, TimeSpan elapsed,
                CancellationToken cancellationToken = default)
            {
                _logger.LogInformation("LogRequestStopAsync");
                return GetSuccessfulTask();
            }

            public ValueTask LogRequestFailedAsync(object? context, HttpRequestMessage request, HttpResponseMessage? response, Exception exception,
                TimeSpan elapsed, CancellationToken cancellationToken = default)
            {
                _logger.LogInformation("LogRequestFailedAsync");
                return GetSuccessfulTask();
            }
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
#if NET5_0_OR_GREATER
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

#if NET5_0_OR_GREATER
            protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
                => SendAsyncCore(request, async: false, cancellationToken).GetAwaiter().GetResult();
#endif
        }
    }
}
