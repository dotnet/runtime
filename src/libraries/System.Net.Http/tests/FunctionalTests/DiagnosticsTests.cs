// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Test.Common;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public abstract class DiagnosticsTest : HttpClientHandlerTestBase
    {
        private const string EnableActivityPropagationEnvironmentVariableSettingName = "DOTNET_SYSTEM_NET_HTTP_ENABLEACTIVITYPROPAGATION";
        private const string EnableActivityPropagationAppCtxSettingName = "System.Net.Http.EnableActivityPropagation";

        private static bool EnableActivityPropagationEnvironmentVariableIsNotSetAndRemoteExecutorSupported =>
            string.IsNullOrEmpty(Environment.GetEnvironmentVariable(EnableActivityPropagationEnvironmentVariableSettingName)) && RemoteExecutor.IsSupported;

        public DiagnosticsTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void EventSource_ExistsWithCorrectId()
        {
            Type esType = typeof(HttpClient).Assembly.GetType("System.Net.NetEventSource", throwOnError: true, ignoreCase: false);
            Assert.NotNull(esType);

            Assert.Equal("Private.InternalDiagnostics.System.Net.Http", EventSource.GetName(esType));
            Assert.Equal(Guid.Parse("a60cec70-947b-5b80-efe2-7c5547b99b3d"), EventSource.GetGuid(esType));

            Assert.NotEmpty(EventSource.GenerateManifest(esType, "assemblyPathToIncludeInManifest"));
        }

        // Diagnostic tests are each invoked in their own process as they enable/disable
        // process-wide EventSource-based tracing, and other tests in the same process
        // could interfere with the tests, as well as the enabling of tracing interfering
        // with those tests.

        /// <remarks>
        /// This test must be in the same test collection as any others testing HttpClient/WinHttpHandler
        /// DiagnosticSources, since the global logging mechanism makes them conflict inherently.
        /// </remarks>
        [OuterLoop("Uses external servers")]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SendAsync_ExpectedDiagnosticSourceLogging()
        {
            RemoteExecutor.Invoke(useVersionString =>
            {
                bool requestLogged = false;
                Guid requestGuid = Guid.Empty;
                bool responseLogged = false;
                Guid responseGuid = Guid.Empty;
                bool exceptionLogged = false;
                bool activityLogged = false;

                var diagnosticListenerObserver = new FakeDiagnosticListenerObserver(kvp =>
                {
                    if (kvp.Key.Equals("System.Net.Http.Request"))
                    {
                        Assert.NotNull(kvp.Value);
                        GetPropertyValueFromAnonymousTypeInstance<HttpRequestMessage>(kvp.Value, "Request");
                        requestGuid = GetPropertyValueFromAnonymousTypeInstance<Guid>(kvp.Value, "LoggingRequestId");
                        requestLogged = true;
                    }
                    else if (kvp.Key.Equals("System.Net.Http.Response"))
                    {
                        Assert.NotNull(kvp.Value);

                        GetPropertyValueFromAnonymousTypeInstance<HttpResponseMessage>(kvp.Value, "Response");
                        responseGuid = GetPropertyValueFromAnonymousTypeInstance<Guid>(kvp.Value, "LoggingRequestId");
                        var requestStatus =
                            GetPropertyValueFromAnonymousTypeInstance<TaskStatus>(kvp.Value, "RequestTaskStatus");
                        Assert.Equal(TaskStatus.RanToCompletion, requestStatus);

                        responseLogged = true;
                    }
                    else if (kvp.Key.Equals("System.Net.Http.Exception"))
                    {
                        exceptionLogged = true;
                    }
                    else if (kvp.Key.StartsWith("System.Net.Http.HttpRequestOut"))
                    {
                        activityLogged = true;
                    }
                });

                using (DiagnosticListener.AllListeners.Subscribe(diagnosticListenerObserver))
                {
                    diagnosticListenerObserver.Enable(s => !s.Contains("HttpRequestOut"));
                    using (HttpClient client = CreateHttpClient(useVersionString))
                    {
                        client.GetAsync(Configuration.Http.RemoteEchoServer).Result.Dispose();
                    }

                    Assert.True(requestLogged, "Request was not logged.");
                    // Poll with a timeout since logging response is not synchronized with returning a response.
                    WaitForTrue(() => responseLogged, TimeSpan.FromSeconds(1),
                        "Response was not logged within 1 second timeout.");
                    Assert.Equal(requestGuid, responseGuid);
                    Assert.False(exceptionLogged, "Exception was logged for successful request");
                    Assert.False(activityLogged, "HttpOutReq was logged while HttpOutReq logging was disabled");
                    diagnosticListenerObserver.Disable();
                }
            }, UseVersion.ToString()).Dispose();
        }

        /// <remarks>
        /// This test must be in the same test collection as any others testing HttpClient/WinHttpHandler
        /// DiagnosticSources, since the global logging mechanism makes them conflict inherently.
        /// </remarks>
        [OuterLoop("Uses external servers")]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SendAsync_ExpectedDiagnosticSourceNoLogging()
        {
            RemoteExecutor.Invoke(useVersionString =>
            {
                bool requestLogged = false;
                bool responseLogged = false;
                bool activityStartLogged = false;
                bool activityStopLogged = false;

                var diagnosticListenerObserver = new FakeDiagnosticListenerObserver(kvp =>
                {
                    if (kvp.Key.Equals("System.Net.Http.Request"))
                    {
                        requestLogged = true;
                    }
                    else if (kvp.Key.Equals("System.Net.Http.Response"))
                    {
                        responseLogged = true;
                    }
                    else if (kvp.Key.Equals("System.Net.Http.HttpRequestOut.Start"))
                    {
                        activityStartLogged = true;
                    }
                    else if (kvp.Key.Equals("System.Net.Http.HttpRequestOut.Stop"))
                    {
                        activityStopLogged = true;
                    }
                });

                using (DiagnosticListener.AllListeners.Subscribe(diagnosticListenerObserver))
                {
                    using (HttpClient client = CreateHttpClient(useVersionString))
                    {
                        LoopbackServer.CreateServerAsync(async (server, url) =>
                        {
                            Task<List<string>> requestLines = server.AcceptConnectionSendResponseAndCloseAsync();
                            Task<HttpResponseMessage> response = client.GetAsync(url);
                            await new Task[] { response, requestLines }.WhenAllOrAnyFailed();

                            AssertNoHeadersAreInjected(requestLines.Result);
                            response.Result.Dispose();
                        }).GetAwaiter().GetResult();
                    }

                    Assert.False(requestLogged, "Request was logged while logging disabled.");
                    Assert.False(activityStartLogged, "HttpRequestOut.Start was logged while logging disabled.");
                    WaitForFalse(() => responseLogged, TimeSpan.FromSeconds(1),
                        "Response was logged while logging disabled.");
                    Assert.False(activityStopLogged, "HttpRequestOut.Stop was logged while logging disabled.");
                }
            }, UseVersion.ToString()).Dispose();
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/1477", TestPlatforms.AnyUnix)]
        [OuterLoop("Uses external servers")]
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public void SendAsync_HttpTracingEnabled_Succeeds(bool useSsl)
        {
            RemoteExecutor.Invoke(async (useVersionString, useSslString) =>
            {
                using (var listener = new TestEventListener("Private.InternalDiagnostics.System.Net.Http", EventLevel.Verbose))
                {
                    var events = new ConcurrentQueue<EventWrittenEventArgs>();
                    await listener.RunWithCallbackAsync(events.Enqueue, async () =>
                    {
                        // Exercise various code paths to get coverage of tracing
                        using (HttpClient client = CreateHttpClient(useVersionString))
                        {
                            // Do a get to a loopback server
                            await LoopbackServer.CreateServerAsync(async (server, url) =>
                            {
                                await TestHelper.WhenAllCompletedOrAnyFailed(
                                    server.AcceptConnectionSendResponseAndCloseAsync(),
                                    client.GetAsync(url));
                            });

                            // Do a post to a remote server
                            byte[] expectedData = Enumerable.Range(0, 20000).Select(i => unchecked((byte)i)).ToArray();
                            Uri remoteServer = bool.Parse(useSslString)
                                ? Configuration.Http.SecureRemoteEchoServer
                                : Configuration.Http.RemoteEchoServer;
                            var content = new ByteArrayContent(expectedData);
                            content.Headers.ContentMD5 = TestHelper.ComputeMD5Hash(expectedData);
                            using (HttpResponseMessage response = await client.PostAsync(remoteServer, content))
                            {
                                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                            }
                        }
                    });

                    // We don't validate receiving specific events, but rather that we do at least
                    // receive some events, and that enabling tracing doesn't cause other failures
                    // in processing.
                    Assert.DoesNotContain(events,
                        ev => ev.EventId == 0); // make sure there are no event source error messages
                    Assert.InRange(events.Count, 1, int.MaxValue);
                }
            }, UseVersion.ToString(), useSsl.ToString()).Dispose();
        }

        [OuterLoop("Uses external servers")]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SendAsync_ExpectedDiagnosticExceptionLogging()
        {
            RemoteExecutor.Invoke(useVersionString =>
            {
                bool exceptionLogged = false;
                bool responseLogged = false;
                var diagnosticListenerObserver = new FakeDiagnosticListenerObserver(kvp =>
                {
                    if (kvp.Key.Equals("System.Net.Http.Response"))
                    {
                        Assert.NotNull(kvp.Value);
                        var requestStatus =
                            GetPropertyValueFromAnonymousTypeInstance<TaskStatus>(kvp.Value, "RequestTaskStatus");
                        Assert.Equal(TaskStatus.Faulted, requestStatus);

                        responseLogged = true;
                    }
                    else if (kvp.Key.Equals("System.Net.Http.Exception"))
                    {
                        Assert.NotNull(kvp.Value);
                        GetPropertyValueFromAnonymousTypeInstance<Exception>(kvp.Value, "Exception");

                        exceptionLogged = true;
                    }
                });

                using (DiagnosticListener.AllListeners.Subscribe(diagnosticListenerObserver))
                {
                    diagnosticListenerObserver.Enable(s => !s.Contains("HttpRequestOut"));
                    using (HttpClient client = CreateHttpClient(useVersionString))
                    {
                        Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync($"http://_{Guid.NewGuid().ToString("N")}.com"))
                            .GetAwaiter().GetResult();
                    }

                    // Poll with a timeout since logging response is not synchronized with returning a response.
                    WaitForTrue(() => responseLogged, TimeSpan.FromSeconds(1),
                        "Response with exception was not logged within 1 second timeout.");
                    Assert.True(exceptionLogged, "Exception was not logged");
                    diagnosticListenerObserver.Disable();
                }
            }, UseVersion.ToString()).Dispose();
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/23167")]
        [OuterLoop("Uses external servers")]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SendAsync_ExpectedDiagnosticCancelledLogging()
        {
            RemoteExecutor.Invoke(useVersionString =>
            {
                bool cancelLogged = false;
                var diagnosticListenerObserver = new FakeDiagnosticListenerObserver(kvp =>
                {
                    if (kvp.Key.Equals("System.Net.Http.Response"))
                    {
                        Assert.NotNull(kvp.Value);
                        var status =
                            GetPropertyValueFromAnonymousTypeInstance<TaskStatus>(kvp.Value, "RequestTaskStatus");
                        Assert.Equal(TaskStatus.Canceled, status);
                        Volatile.Write(ref cancelLogged, true);
                    }
                });

                using (DiagnosticListener.AllListeners.Subscribe(diagnosticListenerObserver))
                {
                    diagnosticListenerObserver.Enable(s => !s.Contains("HttpRequestOut"));
                    using (HttpClient client = CreateHttpClient(useVersionString))
                    {
                        LoopbackServer.CreateServerAsync(async (server, url) =>
                        {
                            CancellationTokenSource tcs = new CancellationTokenSource();
                            Task request = server.AcceptConnectionAsync(connection =>
                            {
                                tcs.Cancel();
                                return connection.ReadRequestHeaderAndSendResponseAsync();
                            });
                            Task response = client.GetAsync(url, tcs.Token);
                            await Assert.ThrowsAnyAsync<Exception>(() =>
                                TestHelper.WhenAllCompletedOrAnyFailed(response, request));
                        }).GetAwaiter().GetResult();
                    }
                }

                // Poll with a timeout since logging response is not synchronized with returning a response.
                WaitForTrue(() => Volatile.Read(ref cancelLogged), TimeSpan.FromSeconds(1),
                    "Cancellation was not logged within 1 second timeout.");
                diagnosticListenerObserver.Disable();
            }, UseVersion.ToString()).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SendAsync_ExpectedDiagnosticSourceActivityLoggingRequestId()
        {
            RemoteExecutor.Invoke(useVersionString =>
            {
                bool requestLogged = false;
                bool responseLogged = false;
                bool activityStartLogged = false;
                bool activityStopLogged = false;
                bool exceptionLogged = false;

                Activity parentActivity = new Activity("parent");
                parentActivity.AddBaggage("correlationId", Guid.NewGuid().ToString("N").ToString());
                parentActivity.AddBaggage("moreBaggage", Guid.NewGuid().ToString("N").ToString());
                parentActivity.AddTag("tag", "tag"); //add tag to ensure it is not injected into request
                parentActivity.Start();

                var diagnosticListenerObserver = new FakeDiagnosticListenerObserver(kvp =>
                {
                    if (kvp.Key.Equals("System.Net.Http.Request"))
                    {
                        requestLogged = true;
                    }
                    else if (kvp.Key.Equals("System.Net.Http.Response"))
                    {
                        responseLogged = true;
                    }
                    else if (kvp.Key.Equals("System.Net.Http.Exception"))
                    {
                        exceptionLogged = true;
                    }
                    else if (kvp.Key.Equals("System.Net.Http.HttpRequestOut.Start"))
                    {
                        Assert.NotNull(kvp.Value);
                        Assert.NotNull(Activity.Current);
                        Assert.Equal(parentActivity, Activity.Current.Parent);
                        GetPropertyValueFromAnonymousTypeInstance<HttpRequestMessage>(kvp.Value, "Request");

                        activityStartLogged = true;
                    }
                    else if (kvp.Key.Equals("System.Net.Http.HttpRequestOut.Stop"))
                    {
                        Assert.NotNull(kvp.Value);
                        Assert.NotNull(Activity.Current);
                        Assert.Equal(parentActivity, Activity.Current.Parent);
                        Assert.True(Activity.Current.Duration != TimeSpan.Zero);
                        GetPropertyValueFromAnonymousTypeInstance<HttpRequestMessage>(kvp.Value, "Request");
                        GetPropertyValueFromAnonymousTypeInstance<HttpResponseMessage>(kvp.Value, "Response");
                        var requestStatus =
                            GetPropertyValueFromAnonymousTypeInstance<TaskStatus>(kvp.Value, "RequestTaskStatus");
                        Assert.Equal(TaskStatus.RanToCompletion, requestStatus);

                        activityStopLogged = true;
                    }
                });

                using (DiagnosticListener.AllListeners.Subscribe(diagnosticListenerObserver))
                {
                    diagnosticListenerObserver.Enable(s => s.Contains("HttpRequestOut"));
                    using (HttpClient client = CreateHttpClient(useVersionString))
                    {
                        LoopbackServer.CreateServerAsync(async (server, url) =>
                        {
                            Task<List<string>> requestLines = server.AcceptConnectionSendResponseAndCloseAsync();
                            Task<HttpResponseMessage> response = client.GetAsync(url);
                            await new Task[] { response, requestLines }.WhenAllOrAnyFailed();

                            AssertHeadersAreInjected(requestLines.Result, parentActivity);
                            response.Result.Dispose();
                        }).GetAwaiter().GetResult();
                    }

                    Assert.True(activityStartLogged, "HttpRequestOut.Start was not logged.");
                    Assert.False(requestLogged, "Request was logged when Activity logging was enabled.");
                    // Poll with a timeout since logging response is not synchronized with returning a response.
                    WaitForTrue(() => activityStopLogged, TimeSpan.FromSeconds(1),
                        "HttpRequestOut.Stop was not logged within 1 second timeout.");
                    Assert.False(exceptionLogged, "Exception was logged for successful request");
                    Assert.False(responseLogged, "Response was logged when Activity logging was enabled.");
                    diagnosticListenerObserver.Disable();
                }
            }, UseVersion.ToString()).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SendAsync_ExpectedDiagnosticSourceActivityLoggingW3C()
        {
            RemoteExecutor.Invoke(useVersionString =>
            {
                bool requestLogged = false;
                bool responseLogged = false;
                bool activityStartLogged = false;
                bool activityStopLogged = false;
                bool exceptionLogged = false;

                Activity parentActivity = new Activity("parent");
                parentActivity.SetParentId(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom());
                parentActivity.AddBaggage("moreBaggage", Guid.NewGuid().ToString("N").ToString());
                parentActivity.Start();

                var diagnosticListenerObserver = new FakeDiagnosticListenerObserver(kvp =>
                {
                    if (kvp.Key.Equals("System.Net.Http.Request"))
                    {
                        requestLogged = true;
                    }
                    else if (kvp.Key.Equals("System.Net.Http.Response"))
                    {
                        responseLogged = true;
                    }
                    else if (kvp.Key.Equals("System.Net.Http.Exception"))
                    {
                        exceptionLogged = true;
                    }
                    else if (kvp.Key.Equals("System.Net.Http.HttpRequestOut.Start"))
                    {
                        Assert.NotNull(kvp.Value);
                        Assert.NotNull(Activity.Current);
                        Assert.Equal(parentActivity, Activity.Current.Parent);
                        GetPropertyValueFromAnonymousTypeInstance<HttpRequestMessage>(kvp.Value, "Request");

                        activityStartLogged = true;
                    }
                    else if (kvp.Key.Equals("System.Net.Http.HttpRequestOut.Stop"))
                    {
                        Assert.NotNull(kvp.Value);
                        Assert.NotNull(Activity.Current);
                        Assert.Equal(parentActivity, Activity.Current.Parent);
                        Assert.True(Activity.Current.Duration != TimeSpan.Zero);
                        GetPropertyValueFromAnonymousTypeInstance<HttpRequestMessage>(kvp.Value, "Request");
                        GetPropertyValueFromAnonymousTypeInstance<HttpResponseMessage>(kvp.Value, "Response");
                        var requestStatus =
                            GetPropertyValueFromAnonymousTypeInstance<TaskStatus>(kvp.Value, "RequestTaskStatus");
                        Assert.Equal(TaskStatus.RanToCompletion, requestStatus);

                        activityStopLogged = true;
                    }
                });

                using (DiagnosticListener.AllListeners.Subscribe(diagnosticListenerObserver))
                {
                    diagnosticListenerObserver.Enable(s => s.Contains("HttpRequestOut"));
                    using (HttpClient client = CreateHttpClient(useVersionString))
                    {
                        LoopbackServer.CreateServerAsync(async (server, url) =>
                        {
                            Task<List<string>> requestLines = server.AcceptConnectionSendResponseAndCloseAsync();
                            Task<HttpResponseMessage> response = client.GetAsync(url);
                            await new Task[] { response, requestLines }.WhenAllOrAnyFailed();

                            AssertHeadersAreInjected(requestLines.Result, parentActivity);
                            response.Result.Dispose();
                        }).GetAwaiter().GetResult();
                    }

                    Assert.True(activityStartLogged, "HttpRequestOut.Start was not logged.");
                    Assert.False(requestLogged, "Request was logged when Activity logging was enabled.");
                    // Poll with a timeout since logging response is not synchronized with returning a response.
                    WaitForTrue(() => activityStopLogged, TimeSpan.FromSeconds(1),
                        "HttpRequestOut.Stop was not logged within 1 second timeout.");
                    Assert.False(exceptionLogged, "Exception was logged for successful request");
                    Assert.False(responseLogged, "Response was logged when Activity logging was enabled.");
                    diagnosticListenerObserver.Disable();
                }
            }, UseVersion.ToString()).Dispose();
        }

        [OuterLoop("Uses external servers")]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SendAsync_ExpectedDiagnosticSourceActivityLogging_InvalidBaggage()
        {
            RemoteExecutor.Invoke(useVersionString =>
            {
                bool activityStopLogged = false;
                bool exceptionLogged = false;

                Activity parentActivity = new Activity("parent");
                parentActivity.SetIdFormat(ActivityIdFormat.Hierarchical);
                parentActivity.AddBaggage("bad/key", "value");
                parentActivity.AddBaggage("goodkey", "bad/value");
                parentActivity.AddBaggage("key", "value");
                parentActivity.Start();

                var diagnosticListenerObserver = new FakeDiagnosticListenerObserver(kvp =>
                {
                    if (kvp.Key.Equals("System.Net.Http.HttpRequestOut.Stop"))
                    {
                        Assert.NotNull(kvp.Value);
                        Assert.NotNull(Activity.Current);
                        Assert.Equal(parentActivity, Activity.Current.Parent);
                        Assert.True(Activity.Current.Duration != TimeSpan.Zero);
                        var request = GetPropertyValueFromAnonymousTypeInstance<HttpRequestMessage>(kvp.Value, "Request");
                        Assert.True(request.Headers.TryGetValues("Request-Id", out var requestId));
                        Assert.True(request.Headers.TryGetValues("Correlation-Context", out var correlationContext));
                        Assert.Equal(3, correlationContext.Count());
                        Assert.Contains("key=value", correlationContext);
                        Assert.Contains("bad%2Fkey=value", correlationContext);
                        Assert.Contains("goodkey=bad%2Fvalue", correlationContext);

                        var requestStatus = GetPropertyValueFromAnonymousTypeInstance<TaskStatus>(kvp.Value, "RequestTaskStatus");
                        Assert.Equal(TaskStatus.RanToCompletion, requestStatus);

                        activityStopLogged = true;
                    }
                    else if (kvp.Key.Equals("System.Net.Http.Exception"))
                    {
                        exceptionLogged = true;
                    }
                });

                using (DiagnosticListener.AllListeners.Subscribe(diagnosticListenerObserver))
                {
                    diagnosticListenerObserver.Enable(s => s.Contains("HttpRequestOut"));
                    using (HttpClient client = CreateHttpClient(useVersionString))
                    {
                        client.GetAsync(Configuration.Http.RemoteEchoServer).Result.Dispose();
                    }

                    // Poll with a timeout since logging response is not synchronized with returning a response.
                    WaitForTrue(() => activityStopLogged, TimeSpan.FromSeconds(1), "Response was not logged within 1 second timeout.");
                    Assert.False(exceptionLogged, "Exception was logged for successful request");
                    diagnosticListenerObserver.Disable();
                }
            }, UseVersion.ToString()).Dispose();
        }

        [OuterLoop("Uses external servers")]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SendAsync_ExpectedDiagnosticSourceActivityLoggingDoesNotOverwriteHeader()
        {
            RemoteExecutor.Invoke(useVersionString =>
            {
                bool activityStartLogged = false;
                bool activityStopLogged = false;

                Activity parentActivity = new Activity("parent");
                parentActivity.SetIdFormat(ActivityIdFormat.Hierarchical);
                parentActivity.AddBaggage("correlationId", Guid.NewGuid().ToString("N").ToString());
                parentActivity.Start();

                string customRequestIdHeader = "|foo.bar.";
                var diagnosticListenerObserver = new FakeDiagnosticListenerObserver(kvp =>
                {
                    if (kvp.Key.Equals("System.Net.Http.HttpRequestOut.Start"))
                    {
                        var request =
                            GetPropertyValueFromAnonymousTypeInstance<HttpRequestMessage>(kvp.Value, "Request");
                        request.Headers.Add("Request-Id", customRequestIdHeader);

                        activityStartLogged = true;
                    }
                    else if (kvp.Key.Equals("System.Net.Http.HttpRequestOut.Stop"))
                    {
                        var request =
                            GetPropertyValueFromAnonymousTypeInstance<HttpRequestMessage>(kvp.Value, "Request");
                        Assert.Single(request.Headers.GetValues("Request-Id"));
                        Assert.Equal(customRequestIdHeader, request.Headers.GetValues("Request-Id").Single());

                        Assert.False(request.Headers.TryGetValues("traceparent", out var _));
                        Assert.False(request.Headers.TryGetValues("tracestate", out var _));
                        activityStopLogged = true;
                    }
                });

                using (DiagnosticListener.AllListeners.Subscribe(diagnosticListenerObserver))
                {
                    diagnosticListenerObserver.Enable();
                    using (HttpClient client = CreateHttpClient(useVersionString))
                    {
                        client.GetAsync(Configuration.Http.RemoteEchoServer).Result.Dispose();
                    }

                    Assert.True(activityStartLogged, "HttpRequestOut.Start was not logged.");

                    // Poll with a timeout since logging response is not synchronized with returning a response.
                    WaitForTrue(() => activityStopLogged, TimeSpan.FromSeconds(1),
                        "HttpRequestOut.Stop was not logged within 1 second timeout.");
                    diagnosticListenerObserver.Disable();
                }
            }, UseVersion.ToString()).Dispose();
        }

        [OuterLoop("Uses external servers")]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SendAsync_ExpectedDiagnosticSourceActivityLoggingDoesNotOverwriteW3CTraceParentHeader()
        {
            Assert.True(UseVersion.Major < 2, "The test currently only supports HTTP/1.");
            RemoteExecutor.Invoke((useVersionString, testAsyncString) =>
            {
                bool activityStartLogged = false;
                bool activityStopLogged = false;

                Activity parentActivity = new Activity("parent");
                parentActivity.SetParentId(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom());
                parentActivity.TraceStateString = "some=state";
                parentActivity.Start();

                string customTraceParentHeader = "00-abcdef0123456789abcdef0123456789-abcdef0123456789-01";
                var diagnosticListenerObserver = new FakeDiagnosticListenerObserver(kvp =>
                {
                    if (kvp.Key.Equals("System.Net.Http.HttpRequestOut.Start"))
                    {
                        var request =
                            GetPropertyValueFromAnonymousTypeInstance<HttpRequestMessage>(kvp.Value, "Request");
                        Assert.Single(request.Headers.GetValues("traceparent"));
                        Assert.False(request.Headers.TryGetValues("tracestate", out var _));
                        Assert.Equal(customTraceParentHeader, request.Headers.GetValues("traceparent").Single());

                        Assert.False(request.Headers.TryGetValues("Request-Id", out var _));

                        activityStartLogged = true;
                    }
                    else if (kvp.Key.Equals("System.Net.Http.HttpRequestOut.Stop"))
                    {
                        activityStopLogged = true;
                    }
                });

                using (DiagnosticListener.AllListeners.Subscribe(diagnosticListenerObserver))
                {
                    diagnosticListenerObserver.Enable();
                    using (var request = new HttpRequestMessage(HttpMethod.Get, Configuration.Http.RemoteEchoServer))
                    using (HttpClient client = CreateHttpClient(useVersionString))
                    {
                        request.Headers.Add("traceparent", customTraceParentHeader);
                        client.SendAsync(bool.Parse(testAsyncString), request).Result.Dispose();
                    }

                    Assert.True(activityStartLogged, "HttpRequestOut.Start was not logged.");

                    // Poll with a timeout since logging response is not synchronized with returning a response.
                    WaitForTrue(() => activityStopLogged, TimeSpan.FromSeconds(1),
                        "HttpRequestOut.Stop was not logged within 1 second timeout.");
                    diagnosticListenerObserver.Disable();
                }
            }, UseVersion.ToString(), TestAsync.ToString()).Dispose();
        }

        [OuterLoop("Uses external servers")]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SendAsync_ExpectedDiagnosticSourceUrlFilteredActivityLogging()
        {
            RemoteExecutor.Invoke(useVersionString =>
            {
                bool activityStartLogged = false;
                bool activityStopLogged = false;

                var diagnosticListenerObserver = new FakeDiagnosticListenerObserver(kvp =>
                {
                    if (kvp.Key.Equals("System.Net.Http.HttpRequestOut.Start"))
                    {
                        activityStartLogged = true;
                    }
                    else if (kvp.Key.Equals("System.Net.Http.HttpRequestOut.Stop"))
                    {
                        activityStopLogged = true;
                    }
                });

                using (DiagnosticListener.AllListeners.Subscribe(diagnosticListenerObserver))
                {
                    diagnosticListenerObserver.Enable((s, r, _) =>
                    {
                        if (s.StartsWith("System.Net.Http.HttpRequestOut"))
                        {
                            var request = r as HttpRequestMessage;
                            if (request != null)
                                return !request.RequestUri.Equals(Configuration.Http.RemoteEchoServer);
                        }

                        return true;
                    });
                    using (HttpClient client = CreateHttpClient(useVersionString))
                    {
                        client.GetAsync(Configuration.Http.RemoteEchoServer).Result.Dispose();
                    }

                    Assert.False(activityStartLogged, "HttpRequestOut.Start was logged while URL disabled.");
                    // Poll with a timeout since logging response is not synchronized with returning a response.
                    Assert.False(activityStopLogged, "HttpRequestOut.Stop was logged while URL disabled.");
                    diagnosticListenerObserver.Disable();
                }
            }, UseVersion.ToString()).Dispose();
        }

        [OuterLoop("Uses external servers")]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SendAsync_ExpectedDiagnosticExceptionActivityLogging()
        {
            RemoteExecutor.Invoke(useVersionString =>
            {
                bool exceptionLogged = false;
                bool activityStopLogged = false;
                var diagnosticListenerObserver = new FakeDiagnosticListenerObserver(kvp =>
                {
                    if (kvp.Key.Equals("System.Net.Http.HttpRequestOut.Stop"))
                    {
                        Assert.NotNull(kvp.Value);
                        GetPropertyValueFromAnonymousTypeInstance<HttpRequestMessage>(kvp.Value, "Request");
                        var requestStatus =
                            GetPropertyValueFromAnonymousTypeInstance<TaskStatus>(kvp.Value, "RequestTaskStatus");
                        Assert.Equal(TaskStatus.Faulted, requestStatus);

                        activityStopLogged = true;
                    }
                    else if (kvp.Key.Equals("System.Net.Http.Exception"))
                    {
                        Assert.NotNull(kvp.Value);
                        GetPropertyValueFromAnonymousTypeInstance<Exception>(kvp.Value, "Exception");

                        exceptionLogged = true;
                    }
                });

                using (DiagnosticListener.AllListeners.Subscribe(diagnosticListenerObserver))
                {
                    diagnosticListenerObserver.Enable();
                    using (HttpClient client = CreateHttpClient(useVersionString))
                    {
                        Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync($"http://_{Guid.NewGuid().ToString("N")}.com"))
                            .GetAwaiter().GetResult();
                    }

                    // Poll with a timeout since logging response is not synchronized with returning a response.
                    WaitForTrue(() => activityStopLogged, TimeSpan.FromSeconds(1),
                        "Response with exception was not logged within 1 second timeout.");
                    Assert.True(exceptionLogged, "Exception was not logged");
                    diagnosticListenerObserver.Disable();
                }
            }, UseVersion.ToString()).Dispose();
        }

        [OuterLoop("Uses external servers")]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SendAsync_ExpectedDiagnosticSynchronousExceptionActivityLogging()
        {
            RemoteExecutor.Invoke((useVersionString , testAsyncString)=>
            {
                bool exceptionLogged = false;
                bool activityStopLogged = false;
                var diagnosticListenerObserver = new FakeDiagnosticListenerObserver(kvp =>
                {
                    if (kvp.Key.Equals("System.Net.Http.HttpRequestOut.Stop"))
                    {
                        Assert.NotNull(kvp.Value);
                        GetPropertyValueFromAnonymousTypeInstance<HttpRequestMessage>(kvp.Value, "Request");
                        var requestStatus =
                            GetPropertyValueFromAnonymousTypeInstance<TaskStatus>(kvp.Value, "RequestTaskStatus");
                        Assert.Equal(TaskStatus.Faulted, requestStatus);

                        activityStopLogged = true;
                    }
                    else if (kvp.Key.Equals("System.Net.Http.Exception"))
                    {
                        Assert.NotNull(kvp.Value);
                        GetPropertyValueFromAnonymousTypeInstance<Exception>(kvp.Value, "Exception");

                        exceptionLogged = true;
                    }
                });

                using (DiagnosticListener.AllListeners.Subscribe(diagnosticListenerObserver))
                {
                    diagnosticListenerObserver.Enable();
                    using (HttpClientHandler handler = CreateHttpClientHandler(useVersionString))
                    using (HttpClient client = CreateHttpClient(handler, useVersionString))
                    {
                        // Set a https proxy.
                        handler.Proxy = new WebProxy($"https://_{Guid.NewGuid().ToString("N")}.com", false);
                        HttpRequestMessage request =
                            new HttpRequestMessage(HttpMethod.Get, $"http://_{Guid.NewGuid().ToString("N")}.com");

                        // Forces a synchronous exception for SocketsHttpHandler.
                        // SocketsHttpHandler only allow http scheme for proxies.

                        // We cannot use Assert.Throws<Exception>(() => { SendAsync(...); }) to verify the
                        // synchronous exception here, because DiagnosticsHandler SendAsync() method has async
                        // modifier, and returns Task. If the call is not awaited, the current test method will continue
                        // run before the call is completed, thus Assert.Throws() will not capture the exception.
                        // We need to wait for the Task to complete synchronously, to validate the exception.
                        bool testAsync = bool.Parse(testAsyncString);
                        Task sendTask = client.SendAsync(testAsync, request);
                        if (!testAsync)
                        {
                            // In sync test case we execute client.Send(...) in separate thread to prevent deadlocks,
                            // so it will never finish immediately and we need to wait for it.
                            ((IAsyncResult)sendTask).AsyncWaitHandle.WaitOne();
                        }
                        Assert.True(sendTask.IsFaulted);
                        Assert.IsType<NotSupportedException>(sendTask.Exception.InnerException);
                    }

                    // Poll with a timeout since logging response is not synchronized with returning a response.
                    WaitForTrue(() => activityStopLogged, TimeSpan.FromSeconds(1),
                        "Response with exception was not logged within 1 second timeout.");
                    Assert.True(exceptionLogged, "Exception was not logged");
                    diagnosticListenerObserver.Disable();
                }
            }, UseVersion.ToString(), TestAsync.ToString()).Dispose();
        }

        [OuterLoop("Uses external servers")]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SendAsync_ExpectedDiagnosticSourceNewAndDeprecatedEventsLogging()
        {
            RemoteExecutor.Invoke(useVersionString =>
            {
                bool requestLogged = false;
                bool responseLogged = false;
                bool activityStartLogged = false;
                bool activityStopLogged = false;

                var diagnosticListenerObserver = new FakeDiagnosticListenerObserver(kvp =>
                {
                    if (kvp.Key.Equals("System.Net.Http.Request"))
                    {
                        requestLogged = true;
                    }
                    else if (kvp.Key.Equals("System.Net.Http.Response"))
                    {
                        responseLogged = true;
                    }
                    else if (kvp.Key.Equals("System.Net.Http.HttpRequestOut.Start"))
                    {
                        activityStartLogged = true;
                    }
                    else if (kvp.Key.Equals("System.Net.Http.HttpRequestOut.Stop"))
                    {
                        activityStopLogged = true;
                    }
                });

                using (DiagnosticListener.AllListeners.Subscribe(diagnosticListenerObserver))
                {
                    diagnosticListenerObserver.Enable();
                    using (HttpClient client = CreateHttpClient(useVersionString))
                    {
                        client.GetAsync(Configuration.Http.RemoteEchoServer).Result.Dispose();
                    }

                    Assert.True(activityStartLogged, "HttpRequestOut.Start was not logged.");
                    Assert.True(requestLogged, "Request was not logged.");
                    // Poll with a timeout since logging response is not synchronized with returning a response.
                    WaitForTrue(() => activityStopLogged, TimeSpan.FromSeconds(1),
                        "HttpRequestOut.Stop was not logged within 1 second timeout.");
                    Assert.True(responseLogged, "Response was not logged.");
                    diagnosticListenerObserver.Disable();
                }
            }, UseVersion.ToString()).Dispose();
        }

        [OuterLoop("Uses external servers")]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SendAsync_ExpectedDiagnosticExceptionOnlyActivityLogging()
        {
            RemoteExecutor.Invoke(useVersionString =>
            {
                bool exceptionLogged = false;
                bool activityLogged = false;
                var diagnosticListenerObserver = new FakeDiagnosticListenerObserver(kvp =>
                {
                    if (kvp.Key.Equals("System.Net.Http.HttpRequestOut.Stop"))
                    {
                        activityLogged = true;
                    }
                    else if (kvp.Key.Equals("System.Net.Http.Exception"))
                    {
                        Assert.NotNull(kvp.Value);
                        GetPropertyValueFromAnonymousTypeInstance<Exception>(kvp.Value, "Exception");

                        exceptionLogged = true;
                    }
                });

                using (DiagnosticListener.AllListeners.Subscribe(diagnosticListenerObserver))
                {
                    diagnosticListenerObserver.Enable(s => s.Equals("System.Net.Http.Exception"));
                    using (HttpClient client = CreateHttpClient(useVersionString))
                    {
                        Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync($"http://_{Guid.NewGuid().ToString("N")}.com"))
                            .GetAwaiter().GetResult();
                    }

                    // Poll with a timeout since logging response is not synchronized with returning a response.
                    WaitForTrue(() => exceptionLogged, TimeSpan.FromSeconds(1),
                        "Exception was not logged within 1 second timeout.");
                    Assert.False(activityLogged, "HttpOutReq was logged when logging was disabled");
                    diagnosticListenerObserver.Disable();
                }
            }, UseVersion.ToString()).Dispose();
        }

        [OuterLoop("Uses external servers")]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SendAsync_ExpectedDiagnosticStopOnlyActivityLogging()
        {
            RemoteExecutor.Invoke(useVersionString =>
            {
                bool activityStartLogged = false;
                bool activityStopLogged = false;

                var diagnosticListenerObserver = new FakeDiagnosticListenerObserver(kvp =>
                {
                    if (kvp.Key.Equals("System.Net.Http.HttpRequestOut.Start"))
                    {
                        activityStartLogged = true;
                    }
                    else if (kvp.Key.Equals("System.Net.Http.HttpRequestOut.Stop"))
                    {
                        Assert.NotNull(Activity.Current);
                        activityStopLogged = true;
                    }
                });

                using (DiagnosticListener.AllListeners.Subscribe(diagnosticListenerObserver))
                {
                    diagnosticListenerObserver.Enable(s => s.Equals("System.Net.Http.HttpRequestOut"));
                    using (HttpClient client = CreateHttpClient(useVersionString))
                    {
                        client.GetAsync(Configuration.Http.RemoteEchoServer).Result.Dispose();
                    }

                    // Poll with a timeout since logging response is not synchronized with returning a response.
                    WaitForTrue(() => activityStopLogged, TimeSpan.FromSeconds(1),
                        "HttpRequestOut.Stop was not logged within 1 second timeout.");
                    Assert.False(activityStartLogged,
                        "HttpRequestOut.Start was logged when start logging was disabled");
                    diagnosticListenerObserver.Disable();
                }
            }, UseVersion.ToString()).Dispose();
        }

        [OuterLoop("Uses external servers")]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SendAsync_ExpectedActivityPropagationWithoutListener()
        {
            RemoteExecutor.Invoke(useVersionString =>
            {
                using (HttpClient client = CreateHttpClient(useVersionString))
                {
                    Activity parent = new Activity("parent").Start();
                    using HttpResponseMessage response = client.GetAsync(Configuration.Http.RemoteEchoServer).Result;

                    Assert.True(response.RequestMessage.Headers.Contains(parent.IdFormat == ActivityIdFormat.Hierarchical ? "Request-Id" : "traceparent"));
                    parent.Stop();
                }
            }, UseVersion.ToString()).Dispose();
        }

        [OuterLoop("Uses external servers")]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SendAsync_ExpectedActivityPropagationWithoutListenerOrParentActivity()
        {
            RemoteExecutor.Invoke(useVersionString =>
            {
                using (HttpClient client = CreateHttpClient(useVersionString))
                {
                    using HttpResponseMessage response = client.GetAsync(Configuration.Http.RemoteEchoServer).Result;

                    Assert.False(response.RequestMessage.Headers.Contains("Request-Id"));
                    Assert.False(response.RequestMessage.Headers.Contains("traceparent"));
                    Assert.False(response.RequestMessage.Headers.Contains("tracestate"));
                    Assert.False(response.RequestMessage.Headers.Contains("Correlation-Context"));
                }
            }, UseVersion.ToString()).Dispose();
        }

        [OuterLoop("Uses external servers")]
        [ConditionalTheory(nameof(EnableActivityPropagationEnvironmentVariableIsNotSetAndRemoteExecutorSupported))]
        [InlineData("true", true)]
        [InlineData("1", true)]
        [InlineData("0", false)]
        [InlineData("false", false)]
        [InlineData("FALSE", false)]
        [InlineData("fAlSe", false)]
        [InlineData("helloworld", true)]
        [InlineData("", true)]
        public void SendAsync_SuppressedGlobalStaticPropagationEnvVar(string envVarValue, bool isInstrumentationEnabled)
        {
            RemoteExecutor.Invoke((innerEnvVarValue, innerIsInstrumentationEnabled) =>
            {
                Environment.SetEnvironmentVariable(EnableActivityPropagationEnvironmentVariableSettingName, innerEnvVarValue);

                string eventKey = null;
                bool anyEventLogged = false;
                var diagnosticListenerObserver = new FakeDiagnosticListenerObserver(kvp =>
                {
                    anyEventLogged = true;
                    eventKey = kvp.Key;
                });

                using (DiagnosticListener.AllListeners.Subscribe(diagnosticListenerObserver))
                {
                    diagnosticListenerObserver.Enable(s => s.Equals("System.Net.Http.HttpRequestOut"));
                    using (HttpClient client = new HttpClient())
                    {
                        Activity parent = new Activity("parent").Start();
                        using HttpResponseMessage response = client.GetAsync(Configuration.Http.RemoteEchoServer).Result;
                        parent.Stop();
                        Assert.Equal(bool.Parse(innerIsInstrumentationEnabled), response.RequestMessage.Headers.Contains(
                            parent.IdFormat == ActivityIdFormat.Hierarchical ? "Request-Id" : "traceparent"));
                    }

                    if (!bool.Parse(innerIsInstrumentationEnabled))
                    {
                        Assert.False(anyEventLogged, $"{eventKey} event logged when Activity is suppressed globally");
                    }
                    else
                    {
                        Assert.True(anyEventLogged, $"{eventKey} event was not logged logged when Activity is not suppressed");
                    }

                    diagnosticListenerObserver.Disable();
                }
            }, envVarValue, isInstrumentationEnabled.ToString()).Dispose();
        }

        [OuterLoop("Uses external servers")]
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
        [InlineData(false)]
        public void SendAsync_SuppressedGlobalStaticPropagationNoListenerAppCtx(bool switchValue)
        {
            RemoteExecutor.Invoke(innerSwitchValue =>
            {
                AppContext.SetSwitch(EnableActivityPropagationAppCtxSettingName, bool.Parse(innerSwitchValue));

                using (HttpClient client = new HttpClient())
                {
                    Activity parent = new Activity("parent").Start();
                    using HttpResponseMessage response = client.GetAsync(Configuration.Http.RemoteEchoServer).Result;
                    parent.Stop();
                    Assert.Equal(bool.Parse(innerSwitchValue), response.RequestMessage.Headers.Contains(
                        parent.IdFormat == ActivityIdFormat.Hierarchical ? "Request-Id" : "traceparent"));
                }
            }, switchValue.ToString()).Dispose();
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/23167")]
        [OuterLoop("Uses external servers")]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SendAsync_ExpectedDiagnosticCancelledActivityLogging()
        {
            RemoteExecutor.Invoke(useVersionString =>
            {
                bool cancelLogged = false;
                var diagnosticListenerObserver = new FakeDiagnosticListenerObserver(kvp =>
                {
                    if (kvp.Key == "System.Net.Http.HttpRequestOut.Stop")
                    {
                        Assert.NotNull(kvp.Value);
                        GetPropertyValueFromAnonymousTypeInstance<HttpRequestMessage>(kvp.Value, "Request");
                        var status =
                            GetPropertyValueFromAnonymousTypeInstance<TaskStatus>(kvp.Value, "RequestTaskStatus");
                        Assert.Equal(TaskStatus.Canceled, status);
                        Volatile.Write(ref cancelLogged, true);
                    }
                });

                using (DiagnosticListener.AllListeners.Subscribe(diagnosticListenerObserver))
                {
                    diagnosticListenerObserver.Enable();
                    using (HttpClient client = CreateHttpClient(useVersionString))
                    {
                        LoopbackServer.CreateServerAsync(async (server, url) =>
                        {
                            CancellationTokenSource tcs = new CancellationTokenSource();
                            Task request = server.AcceptConnectionAsync(connection =>
                            {
                                tcs.Cancel();
                                return connection.ReadRequestHeaderAndSendResponseAsync();
                            });
                            Task response = client.GetAsync(url, tcs.Token);
                            await Assert.ThrowsAnyAsync<Exception>(() =>
                                TestHelper.WhenAllCompletedOrAnyFailed(response, request));
                        }).GetAwaiter().GetResult();
                    }
                }

                // Poll with a timeout since logging response is not synchronized with returning a response.
                WaitForTrue(() => Volatile.Read(ref cancelLogged), TimeSpan.FromSeconds(1),
                    "Cancellation was not logged within 1 second timeout.");
                diagnosticListenerObserver.Disable();
            }, UseVersion.ToString()).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SendAsync_NullRequest_ThrowsArgumentNullException()
        {
            RemoteExecutor.Invoke(async () =>
            {
                var diagnosticListenerObserver = new FakeDiagnosticListenerObserver(null);
                using (DiagnosticListener.AllListeners.Subscribe(diagnosticListenerObserver))
                {
                    diagnosticListenerObserver.Enable();

                    using (MyHandler handler = new MyHandler())
                    {
                        // Getting the Task first from the .SendAsync() call also tests
                        // that the exception comes from the async Task path.
                        Task t = handler.SendAsync(null);
                        await Assert.ThrowsAsync<ArgumentNullException>(() => t);
                    }
                }

                diagnosticListenerObserver.Disable();
            }).Dispose();
        }

        private class MyHandler : HttpClientHandler
        {
            internal Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
            {
                return SendAsync(request, CancellationToken.None);
            }
        }

        private static T GetPropertyValueFromAnonymousTypeInstance<T>(object obj, string propertyName)
        {
            Type t = obj.GetType();

            PropertyInfo p = t.GetRuntimeProperty(propertyName);

            object propertyValue = p.GetValue(obj);
            Assert.NotNull(propertyValue);
            Assert.IsAssignableFrom<T>(propertyValue);

            return (T)propertyValue;
        }

        private static void WaitForTrue(Func<bool> p, TimeSpan timeout, string message)
        {
            // Assert that spin doesn't time out.
            Assert.True(SpinWait.SpinUntil(p, timeout), message);
        }

        private static void WaitForFalse(Func<bool> p, TimeSpan timeout, string message)
        {
            // Assert that spin times out.
            Assert.False(SpinWait.SpinUntil(p, timeout), message);
        }

        private static string GetHeaderValue(string name, List<string> requestLines)
        {
            string header = null;

            foreach (var line in requestLines)
            {
                if (line.StartsWith(name))
                {
                    header = line.Substring(name.Length).Trim(' ', ':');
                }
            }

            return header;
        }

        private static void AssertHeadersAreInjected(List<string> requestLines, Activity parent)
        {
            string requestId = GetHeaderValue("Request-Id", requestLines);
            string traceparent = GetHeaderValue("traceparent", requestLines);
            string tracestate = GetHeaderValue("tracestate", requestLines);

            if (parent.IdFormat == ActivityIdFormat.Hierarchical)
            {
                Assert.True(requestId != null, "Request-Id was not injected when instrumentation was enabled");
                Assert.StartsWith(parent.Id, requestId);
                Assert.NotEqual(parent.Id, requestId);
                Assert.Null(traceparent);
                Assert.Null(tracestate);
            }
            else if (parent.IdFormat == ActivityIdFormat.W3C)
            {
                Assert.Null(requestId);
                Assert.True(traceparent != null, "traceparent was not injected when W3C instrumentation was enabled");
                Assert.StartsWith($"00-{parent.TraceId.ToHexString()}-", traceparent);
                Assert.Equal(parent.TraceStateString, tracestate);
            }

            var correlationContext = new List<NameValueHeaderValue>();

            foreach (var line in requestLines)
            {
                if (line.StartsWith("Correlation-Context"))
                {
                    var corrCtxString = line.Substring("Correlation-Context".Length).Trim(' ', ':');
                    foreach (var kvp in corrCtxString.Split(','))
                    {
                        correlationContext.Add(NameValueHeaderValue.Parse(kvp));
                    }
                }
            }

            List<KeyValuePair<string, string>> baggage = parent.Baggage.ToList();
            Assert.Equal(baggage.Count, correlationContext.Count);
            foreach (var kvp in baggage)
            {
                Assert.Contains(new NameValueHeaderValue(kvp.Key, kvp.Value), correlationContext);
            }
        }

        private static void AssertNoHeadersAreInjected(List<string> requestLines)
        {
            foreach (var line in requestLines)
            {
                Assert.False(line.StartsWith("Request-Id"),
                    "Request-Id header was injected when instrumentation was disabled");

                Assert.False(line.StartsWith("traceparent"),
                    "traceparent header was injected when instrumentation was disabled");

                Assert.False(line.StartsWith("tracestate"),
                    "tracestate header was injected when instrumentation was disabled");

                Assert.False(line.StartsWith("Correlation-Context"),
                    "Correlation-Context header was injected when instrumentation was disabled");
            }
        }
    }
}
