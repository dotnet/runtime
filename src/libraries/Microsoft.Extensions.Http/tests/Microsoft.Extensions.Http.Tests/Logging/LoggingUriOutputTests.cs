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
            { "http://q.app/foo?", "http://q.app/foo?" },
            { "http://q.app/foo?XXX", "http://q.app/foo?*" },
            { "http://q.app/a/b/c?a=b%20c&x=1", "http://q.app/a/b/c?*" },
            { "/cat/1/2", "/cat/1/2" },
            { "/cat/1/2?", "/cat/1/2?" },
            { "/cat/1/2?X", "/cat/1/2?*" },
            { "/cat/1/2?a=b%20c&x=1", "/cat/1/2?*" },
        };

        [Theory]
        [MemberData(nameof(GetRedactedUriString_Data))]
        public void GetRedactedUriString(string original, string expected)
        {
            Uri? uri = original != null ? new Uri(original, UriKind.RelativeOrAbsolute) : null;
            string? actual = LogHelper.GetRedactedUriString(uri);

            Assert.Equal(expected, actual);
        }

        public static TheoryData<bool, bool, string> Handlers_LogExpectedUri_Data()
        {
            TheoryData<bool, bool, string> result = new();
            bool[] booleans = { true, false };
            bool[] syncApiVals =
#if NET
                booleans;
#else
                { false };
#endif
            foreach (bool syncApi in syncApiVals)
            {
                foreach (bool scopeHandler in booleans)
                {
                    // valid values for logQueryStringEnabler:
                    // ""           - Do not enable query string logging.
                    // "AppCtx"     - Enable via AppContext switch.
                    // "EnvVarTrue" - Enable by setting the environment *_DISABLEURIQUERYREDACTION variable to 'true'.
                    // "EnvVar1"    - Enable by setting the environment *DISABLEURIQUERYREDACTION variable to '1'.
                    string[] lqs = ["", "AppCtx"];
                    foreach (string queryRedactionDisabler in lqs)
                    {
                        result.Add(syncApi, scopeHandler, queryRedactionDisabler);
                    }
                }
            }

            result.Add(false, false, "EnvVarTrue");
            result.Add(false, false, "EnvVar1");
            result.Add(false, true, "EnvVarTrue");
            result.Add(false, true, "EnvVar1");

            return result;
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [MemberData(nameof(Handlers_LogExpectedUri_Data))]
        public async Task Handlers_LogExpectedUri(bool syncApi, bool scopeHandler, string queryRedactionDisabler)
        {
            await RemoteExecutor.Invoke(static async (syncApiStr, scopeHandlerStr, queryRedactionDisabler) =>
            {
                bool syncApi = bool.Parse(syncApiStr);
                bool scopeHandler = bool.Parse(scopeHandlerStr);

                string baseUri = "http://api.example.com/search";
                const string queryString = "term=Western%20Australia";
                string destinationUri = $"{baseUri}?{queryString}";

                switch (queryRedactionDisabler)
                {
                    case "AppCtx":
                        AppContext.SetSwitch("Microsoft.Extensions.Http.DisableUriQueryRedaction", true);
                        break;
                    case "EnvVarTrue":
                        Environment.SetEnvironmentVariable("DOTNET_MICROSOFT_EXTENSIONS_HTTP_DISABLEURIQUERYREDACTION", "True");
                        break;
                    case "EnvVar1":
                        Environment.SetEnvironmentVariable("DOTNET_MICROSOFT_EXTENSIONS_HTTP_DISABLEURIQUERYREDACTION", "1");
                        break;
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

                string expectedUri = !string.IsNullOrEmpty(queryRedactionDisabler) ? destinationUri : $"{baseUri}?*";

                if (scopeHandler)
                {
                    var pipelineStartMessage = Assert.Single(sink.Writes.Where(m => m.EventId == EventIds.PipelineStart));
                    Assert.Equal($"HTTP GET {expectedUri}", pipelineStartMessage.Scope.ToString());
                    Assert.Equal($"Start processing HTTP request GET {expectedUri}", pipelineStartMessage.Message);
                }
                else
                {
                    var requestStartMessage = Assert.Single(sink.Writes.Where(m => m.EventId == EventIds.RequestStart));
                    Assert.Equal($"Sending HTTP request GET {expectedUri}", requestStartMessage.Message);
                }
            }, syncApi.ToString(), scopeHandler.ToString(), queryRedactionDisabler).DisposeAsync();
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

            await RemoteExecutor.Invoke(static async (syncApiStr, logQueryStringStr) =>
            {
                bool syncApi = bool.Parse(syncApiStr);
                bool logQueryString = bool.Parse(logQueryStringStr);

                if (logQueryString)
                {
                    AppContext.SetSwitch("Microsoft.Extensions.Http.DisableUriQueryRedaction", true);
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

                string expectedUri = logQueryString ? destinationUri : $"{baseUri}?*";

                Assert.Equal($"HTTP GET {expectedUri}", pipelineStartMessage.Scope.ToString());
                Assert.Equal($"Start processing HTTP request GET {expectedUri}", pipelineStartMessage.Message);
                Assert.Equal($"Sending HTTP request GET {expectedUri}", requestStartMessage.Message);

            }, syncApi.ToString(), logQueryString.ToString()).DisposeAsync();
        }
    }
}
