// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.Extensions.Http.Tests.Logging
{
    public class LoggingUriOutputTests
    {
        public static TheoryData<bool, bool, bool, bool> Handlers_LogAbsoluteUri_Data()
        {
            TheoryData<bool, bool, bool, bool> result = new();
            bool[] booleans = { true, false };
            bool[] syncApiVals =
#if NET
                booleans;
#else
                { false };
#endif
            foreach (bool syncApi in syncApiVals)
            {
                foreach (bool logQueryString in booleans)
                {
                    foreach (bool absoluteUri in booleans)
                    {
                        foreach (bool scopeHandler in booleans)
                        {
                            result.Add(syncApi, logQueryString, absoluteUri, scopeHandler);
                        }
                    }
                }
            }
            return result;
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [MemberData(nameof(Handlers_LogAbsoluteUri_Data))]
        public async Task Handlers_LogExpectedUri(bool syncApi, bool logQueryString, bool absoluteUri, bool scopeHandler)
        {
            await RemoteExecutor.Invoke(static async (syncApiStr, logQueryStringStr, absoluteUriStr, scopeHandlerStr) =>
            {
                bool syncApi = bool.Parse(syncApiStr);
                bool logQueryString = bool.Parse(logQueryStringStr);
                bool absoluteUri = bool.Parse(absoluteUriStr);
                bool scopeHandler = bool.Parse(scopeHandlerStr);

                string baseUri = absoluteUri ? "http://api.example.com/search" : "/search";
                const string queryString = "term=Western%20Australia";
                string destinationUri = $"{baseUri}?{queryString}";

                if (logQueryString)
                {
                    AppContext.SetSwitch("Microsoft.Extensions.Http.LogQueryString", true);
                }

                var sink = new TestSink();
                var logger = new TestLogger("test", sink, enabled: true);

                DelegatingHandler handler = scopeHandler ? new LoggingScopeHttpMessageHandler(logger) : new LoggingHttpMessageHandler(logger);
                handler.InnerHandler = new TestMessageHandler();

                using HttpMessageInvoker invoker = new HttpMessageInvoker(handler);
                using var request = new HttpRequestMessage(HttpMethod.Get, destinationUri);

#if NET
                if (syncApi)
                {
                    _ = invoker.Send(request, default);
                    await Task.Yield();
                }
                else
#endif
                {
                    _ = await invoker.SendAsync(request, default);
                }

                string expectedUri = logQueryString ? destinationUri : baseUri;

                if (scopeHandler)
                {
                    var pipelineStartMessage = Assert.Single(sink.Writes.Where(m => m.EventId == LoggingScopeHttpMessageHandler.Log.EventIds.PipelineStart));
                    Assert.Equal($"HTTP GET {expectedUri}", pipelineStartMessage.Scope.ToString());
                    Assert.Equal($"Start processing HTTP request GET {expectedUri}", pipelineStartMessage.Message);
                }
                else
                {
                    var requestStartMessage = Assert.Single(sink.Writes.Where(m => m.EventId == LoggingHttpMessageHandler.Log.EventIds.RequestStart));
                    Assert.Equal($"Sending HTTP request GET {expectedUri}", requestStartMessage.Message);
                }
            }, syncApi.ToString(), logQueryString.ToString(), absoluteUri.ToString(), scopeHandler.ToString()).DisposeAsync();
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false, false)]
        [InlineData(false, true)]
#if NET
        [InlineData(true, false)]
        [InlineData(true, true)]
#endif
        public async Task Integration_LogsExpectedAbsoluteUri(bool syncApi, bool logQueryString)
        {
            const string baseUri = "http://api.example.com/search";
            const string queryString = "term=Western%20Australia";
            const string destinationUri = $"{baseUri}?{queryString}";

            await RemoteExecutor.Invoke(static async (syncApiStr, logQueryStringStr) =>
            {
                bool syncApi = bool.Parse(syncApiStr);
                bool logQueryString = bool.Parse(logQueryStringStr);

                if (logQueryString)
                {
                    AppContext.SetSwitch("Microsoft.Extensions.Http.LogQueryString", true);
                }

                var sink = new TestSink();
                var serviceCollection = new ServiceCollection();
                serviceCollection.AddLogging();
                serviceCollection.AddSingleton<ILoggerFactory>(new TestLoggerFactory(sink, enabled: true));
                serviceCollection.AddTransient<TestMessageHandler>();
                serviceCollection.AddHttpClient("test").ConfigurePrimaryHttpMessageHandler<TestMessageHandler>();

                var services = serviceCollection.BuildServiceProvider();
                var factory = services.GetRequiredService<IHttpClientFactory>();
                var client = factory.CreateClient("test");

                using var request = new HttpRequestMessage(HttpMethod.Get, destinationUri);
#if NET
                if (syncApi)
                {
                    _ = client.Send(request);
                    await Task.Yield();
                }
                else
#endif
                {
                    _ = await client.SendAsync(request);
                }

                var pipelineStartMessage = Assert.Single(sink.Writes.Where(m =>
                        m.EventId == LoggingScopeHttpMessageHandler.Log.EventIds.PipelineStart &&
                        m.LoggerName == "System.Net.Http.HttpClient.test.LogicalHandler"));

                var requestStartMessage = Assert.Single(sink.Writes.Where(m =>
                        m.EventId == LoggingHttpMessageHandler.Log.EventIds.RequestStart &&
                        m.LoggerName == "System.Net.Http.HttpClient.test.ClientHandler"));

                string expectedUri = logQueryString ? destinationUri : baseUri;

                Assert.Equal($"HTTP GET {expectedUri}", pipelineStartMessage.Scope.ToString());
                Assert.Equal($"Start processing HTTP request GET {expectedUri}", pipelineStartMessage.Message);
                Assert.Equal($"Sending HTTP request GET {expectedUri}", requestStartMessage.Message);

            }, syncApi.ToString(), logQueryString.ToString()).DisposeAsync();
        }
    }
}
