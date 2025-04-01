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
        private static class EventIds
        {
            public static readonly EventId RequestStart = new EventId(100, "RequestStart");
            public static readonly EventId RequestEnd = new EventId(101, "RequestEnd");

            public static readonly EventId PipelineStart = new EventId(100, "RequestPipelineStart");
            public static readonly EventId PipelineEnd = new EventId(101, "RequestPipelineEnd");
        }

        public static readonly TheoryData<string, string> GetRedactedUriString_Data = new TheoryData<string, string>()
        {
            { null, null },
            { "http://q.app/foo", "http://q.app/foo" },
            { "http://q.app:123/foo", "http://q.app:123/foo" },
            { "http://user:xxx@q.app/foo", "http://q.app/foo" }, // has user info
            { "http://q.app/foo?", "http://q.app/foo?" },
            { "http://q.app/foo?XXX", "http://q.app/foo?*" },
            { "http://q.app/a/b/c?a=b%20c&x=1", "http://q.app/a/b/c?*" },
            { "http://q.app:4242/a/b/c?a=b%20c&x=1", "http://q.app:4242/a/b/c?*" },
            { "/cat/1/2", "*" }, // Relative Uris are fully redacted.
            { "/cat/1/2?a=b%20c&x=1", "*" },
        };

        [Theory]
        [MemberData(nameof(GetRedactedUriString_Data))]
        public void GetRedactedUriString_RedactsUriByDefault(string original, string expected)
        {
            Uri? uri = original != null ? new Uri(original, UriKind.RelativeOrAbsolute) : null;
            string? actual = LogHelper.GetRedactedUriString(uri);

            Assert.Equal(expected, actual);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData("AppCtx")]     // AppContext switch System.Net.Http.DisableUriRedaction = true
        [InlineData("EnvVar1")]    // Env. var DOTNET_SYSTEM_NET_DISABLEURIREDACTION = "1"
        [InlineData("EnvVarTrue")] // Env. var DOTNET_SYSTEM_NET_DISABLEURIREDACTION = "true"
        public void GetRedactedUriString_DisableUriRedaction_DoesNotRedactUri(string queryRedactionDisabler)
        {
            RemoteExecutor.Invoke(static queryRedactionDisabler =>
            {
                switch (queryRedactionDisabler)
                {
                    case "AppCtx":
                        AppContext.SetSwitch("System.Net.Http.DisableUriRedaction", true);
                        break;
                    case "EnvVarTrue":
                        Environment.SetEnvironmentVariable("DOTNET_SYSTEM_NET_HTTP_DISABLEURIREDACTION", "true");
                        break;
                    case "EnvVar1":
                        Environment.SetEnvironmentVariable("DOTNET_SYSTEM_NET_HTTP_DISABLEURIREDACTION", "1");
                        break;
                }

                Uri[] uris = GetRedactedUriString_Data.Select(a => a[0] == null ? null : new Uri((string)a[0], UriKind.RelativeOrAbsolute)).ToArray();

                foreach (Uri uri in uris)
                {
                    string? expected = uri != null ? uri.IsAbsoluteUri ? uri.AbsoluteUri : uri.ToString() : null;
                    string? actual = LogHelper.GetRedactedUriString(uri);
                    Assert.Equal(expected, actual);
                }
            }, queryRedactionDisabler).Dispose();
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false, false)]
        [InlineData(false, true)]
#if NET
        [InlineData(true, false)]
        [InlineData(true, true)]
#endif
        public async Task Handlers_LogExpectedUri(bool syncApi, bool scopeHandler)
        {
            await RemoteExecutor.Invoke(static async (syncApiStr, scopeHandlerStr) =>
            {
                bool syncApi = bool.Parse(syncApiStr);
                bool scopeHandler = bool.Parse(scopeHandlerStr);

                string baseUri = "http://api.example.com/search";
                const string queryString = "term=Western%20Australia";
                string destinationUri = $"{baseUri}?{queryString}";

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

                if (scopeHandler)
                {
                    var pipelineStartMessage = Assert.Single(sink.Writes.Where(m => m.EventId == EventIds.PipelineStart));
                    Assert.Equal($"HTTP GET {baseUri}?*", pipelineStartMessage.Scope.ToString());
                    Assert.Equal($"Start processing HTTP request GET {baseUri}?*", pipelineStartMessage.Message);
                }
                else
                {
                    var requestStartMessage = Assert.Single(sink.Writes.Where(m => m.EventId == EventIds.RequestStart));
                    Assert.Equal($"Sending HTTP request GET {baseUri}?*", requestStartMessage.Message);
                }
            }, syncApi.ToString(), scopeHandler.ToString()).DisposeAsync();
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false, false)]
        [InlineData(false, true)]
#if NET
        [InlineData(true, false)]
        [InlineData(true, true)]
#endif
        public async Task Integration_LogsExpectedAbsoluteUri(bool syncApi, bool disableUriQueryRedaction)
        {
            const string baseUri = "http://api.example.com/search";
            const string queryString = "term=Western%20Australia";
            const string destinationUri = $"{baseUri}?{queryString}";

            await RemoteExecutor.Invoke(static async (syncApiStr, disableUriQueryRedactionStr) =>
            {
                bool syncApi = bool.Parse(syncApiStr);
                bool disableUriQueryRedaction = bool.Parse(disableUriQueryRedactionStr);

                if (disableUriQueryRedaction)
                {
                    AppContext.SetSwitch("System.Net.Http.DisableUriRedaction", true);
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
                        m.EventId == EventIds.PipelineStart &&
                        m.LoggerName == "System.Net.Http.HttpClient.test.LogicalHandler"));

                var requestStartMessage = Assert.Single(sink.Writes.Where(m =>
                        m.EventId == EventIds.RequestStart &&
                        m.LoggerName == "System.Net.Http.HttpClient.test.ClientHandler"));

                string expectedUri = disableUriQueryRedaction ? destinationUri : $"{baseUri}?*";

                Assert.Equal($"HTTP GET {expectedUri}", pipelineStartMessage.Scope.ToString());
                Assert.Equal($"Start processing HTTP request GET {expectedUri}", pipelineStartMessage.Message);
                Assert.Equal($"Sending HTTP request GET {expectedUri}", requestStartMessage.Message);

            }, syncApi.ToString(), disableUriQueryRedaction.ToString()).DisposeAsync();
        }
    }
}
