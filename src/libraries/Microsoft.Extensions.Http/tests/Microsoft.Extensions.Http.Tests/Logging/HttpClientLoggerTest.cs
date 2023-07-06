// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Extensions.Http.Logging
{
    public class HttpClientLoggerTest
    {
        private const string Url = "http://www.example.com";

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
            serviceCollection.AddTransient(_ => new TestMessageHandler());

            serviceCollection.AddHttpClient("NoLoggers")
                .ConfigurePrimaryHttpMessageHandler<TestMessageHandler>()
                .ConfigureLogging(b => b.RemoveAllLoggers());

            var services = serviceCollection.BuildServiceProvider();
            var factory = services.GetRequiredService<IHttpClientFactory>();

            var client = factory.CreateClient("NoLoggers");

            await client.GetAsync(Url);
            Assert.Equal(0, sink.Writes.Count);
        }

        [Fact]
        public async Task RemoveAllLoggers_OnlyForSpecifiedNames()
        {
            var sink = new TestSink();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            serviceCollection.AddSingleton<ILoggerFactory>(new TestLoggerFactory(sink, enabled: true));
            serviceCollection.AddTransient(_ => new TestMessageHandler());

            serviceCollection.AddHttpClient("NoLoggers")
                .ConfigurePrimaryHttpMessageHandler<TestMessageHandler>()
                .ConfigureLogging(b => b.RemoveAllLoggers());

            serviceCollection.AddHttpClient("DefaultLogger")
                .ConfigurePrimaryHttpMessageHandler<TestMessageHandler>();

            serviceCollection.AddHttpClient("NoLoggersV2")
                .ConfigurePrimaryHttpMessageHandler<TestMessageHandler>()
                .ConfigureLogging(b => b.RemoveAllLoggers());

            var services = serviceCollection.BuildServiceProvider();
            var factory = services.GetRequiredService<IHttpClientFactory>();

            var defaultLoggerClient = factory.CreateClient("DefaultLogger");
            var noLoggersClient = factory.CreateClient("NoLoggers");
            var noLoggersClientV2 = factory.CreateClient("NoLoggersV2");

            await noLoggersClient.GetAsync(Url);
            Assert.Equal(0, sink.Writes.Count);

            await defaultLoggerClient.GetAsync(Url);
            Assert.Equal(8, sink.Writes.Count); // 2 loggers (inner & outer) x 2 levels (msg & headers) x 2 times (request & response)

            var previousMessagesCount = sink.Writes.Count;

            await noLoggersClientV2.GetAsync(Url);
            Assert.Equal(0, sink.Writes.Count - previousMessagesCount);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CustomLogger_LogsCorrectEvents(bool requestSuccessful)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient(_ =>
                new TestMessageHandler(_ => requestSuccessful
                    ? new HttpResponseMessage()
                    : throw new HttpRequestException("expected")));

            var testLogger = new TestCountingLogger();
            serviceCollection.AddHttpClient("TestCountingLogger")
                .ConfigurePrimaryHttpMessageHandler<TestMessageHandler>()
                .ConfigureLogging(b => b.AddLogger(_ => testLogger));

            var services = serviceCollection.BuildServiceProvider();
            var factory = services.GetRequiredService<IHttpClientFactory>();

            var client = factory.CreateClient("TestCountingLogger");

            try
            {
                await client.GetAsync(Url);
            }
            catch (HttpRequestException e) when (!requestSuccessful && e.Message == "expected")
            {
                // swallow expected exception
            }

            Assert.Equal(1, testLogger.RequestStartLogCount);
            Assert.Equal(requestSuccessful ? 1 : 0, testLogger.RequestStopLogCount);
            Assert.Equal(requestSuccessful ? 0 : 1, testLogger.RequestFailedLogCount);

            try
            {
                await client.GetAsync(Url);
            }
            catch (HttpRequestException e) when (!requestSuccessful && e.Message == "expected")
            {
                // swallow expected exception
            }

            Assert.Equal(2, testLogger.RequestStartLogCount);
            Assert.Equal(requestSuccessful ? 2 : 0, testLogger.RequestStopLogCount);
            Assert.Equal(requestSuccessful ? 0 : 2, testLogger.RequestFailedLogCount);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
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
                .ConfigureLogging(b => b.AddLogger(_ => testLogger));

            var services = serviceCollection.BuildServiceProvider();
            var factory = services.GetRequiredService<IHttpClientFactory>();

            var client = factory.CreateClient("TestContextCountingLogger");

            try
            {
                await client.GetAsync(Url);
            }
            catch (HttpRequestException e) when (!requestSuccessful && e.Message == "expected")
            {
                // swallow expected exception
            }

            Assert.Equal(1, testLogger.RequestStartLogged.Count);
            Assert.Equal(requestSuccessful ? 1 : 0, testLogger.RequestStopLogged.Count);
            Assert.Equal(requestSuccessful ? 0 : 1, testLogger.RequestFailedLogged.Count);

            try
            {
                await client.GetAsync(Url);
            }
            catch (HttpRequestException e) when (!requestSuccessful && e.Message == "expected")
            {
                // swallow expected exception
            }

            Assert.Equal(2, testLogger.RequestStartLogged.Count);
            Assert.Equal(requestSuccessful ? 2 : 0, testLogger.RequestStopLogged.Count);
            Assert.Equal(requestSuccessful ? 0 : 2, testLogger.RequestFailedLogged.Count);
        }

        private class TestCountingLogger : IHttpClientLogger
        {
            public int RequestStartLogCount { get; private set; }
            public int RequestStopLogCount { get; private set; }
            public int RequestFailedLogCount { get; private set; }

            public ValueTask<object?> LogRequestStartAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
            {
                RequestStartLogCount++;
                return new ValueTask<object?>((object?)null); // task from result (.NET Framework compliant)
            }

            public ValueTask LogRequestStopAsync(object? context, HttpRequestMessage request, HttpResponseMessage response, TimeSpan elapsed,
                CancellationToken cancellationToken = default)
            {
                RequestStopLogCount++;
                return new ValueTask(); // completed task (.NET Framework compliant)
            }

            public ValueTask LogRequestFailedAsync(object? context, HttpRequestMessage request, HttpResponseMessage? response, Exception exception,
                TimeSpan elapsed, CancellationToken cancellationToken = default)
            {
                RequestFailedLogCount++;
                return new ValueTask(); // completed task (.NET Framework compliant)
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

                return new ValueTask<object?>(context);
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

                return new ValueTask();
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

                return new ValueTask();
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

            public ValueTask<object?> LogRequestStartAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
            {
                _logger.LogInformation("LogRequestStartAsync " + GetObjId(request));
                return new ValueTask<object?>(null);
            }

            public ValueTask LogRequestStopAsync(object? context, HttpRequestMessage request, HttpResponseMessage response, TimeSpan elapsed,
                CancellationToken cancellationToken = default)
            {
                _logger.LogInformation("LogRequestStopAsync " + GetObjId(request));
                return new ValueTask();
            }

            public ValueTask LogRequestFailedAsync(object? context, HttpRequestMessage request, HttpResponseMessage? response, Exception exception,
                TimeSpan elapsed, CancellationToken cancellationToken = default)
            {
                _logger.LogInformation("LogRequestFailedAsync " + GetObjId(request));
                return new ValueTask();
            }
        }

        private static string GetObjId(object? obj)
        {
            if (obj is null)
            {
                return "(null)";
            }

            return obj.GetType().Name + "#" + obj.GetHashCode();
        }
    }
}
