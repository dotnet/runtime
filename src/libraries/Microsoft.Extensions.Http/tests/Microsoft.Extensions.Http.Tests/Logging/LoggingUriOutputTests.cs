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
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false, false)]
        [InlineData(false, true)]
#if NET
        [InlineData(true, false)]
        [InlineData(true, true)]
#endif
        public async Task LogsAbsoluteUrl(bool syncApi, bool logQueryString)
        {
            const string BaseUri = "http://api.example.com/search";
            const string QueryString = "term=Western%20Australia";
            const string FullUri = $"{BaseUri}?{QueryString}";

            await RemoteExecutor.Invoke(static async (syncApiStr, enableQueryStringLoggingStr) =>
            {
                bool syncApi = bool.Parse(syncApiStr);
                bool enableQueryStringLogging = bool.Parse(enableQueryStringLoggingStr);

                if (enableQueryStringLogging)
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

                using var request = new HttpRequestMessage(HttpMethod.Get, FullUri);
#if NET
                if (syncApi)
                {
                    client.Send(request);
                    await Task.Yield();
                }
                else
#endif
                {
                    _ = await client.SendAsync(request);
                }

                var pipelineStartMessage = Assert.Single(sink.Writes.Where(m =>
                {
                    return
                        m.EventId == LoggingScopeHttpMessageHandler.Log.EventIds.PipelineStart &&
                        m.LoggerName == "System.Net.Http.HttpClient.test.LogicalHandler";
                }));

                var requestStartMessage = Assert.Single(sink.Writes.Where(m =>
                {
                    return
                        m.EventId == LoggingHttpMessageHandler.Log.EventIds.RequestStart &&
                        m.LoggerName == "System.Net.Http.HttpClient.test.ClientHandler";
                }));

                string expectedUri = enableQueryStringLogging ? FullUri : BaseUri;

                Assert.Equal($"HTTP GET {expectedUri}", pipelineStartMessage.Scope.ToString());
                Assert.Equal($"Start processing HTTP request GET {expectedUri}", pipelineStartMessage.Message);
                Assert.Equal($"Sending HTTP request GET {expectedUri}", requestStartMessage.Message);

            }, syncApi.ToString(), logQueryString.ToString()).DisposeAsync();
        }
    }
}
