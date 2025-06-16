// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public abstract class DiagnosticsTest : DiagnosticsTestBase
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

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task SendAsync_ExpectedDiagnosticSourceLogging()
        {
            await RemoteExecutor.Invoke(async (useVersion, testAsync) =>
            {
                HttpRequestMessage requestLogged = null;
                HttpResponseMessage responseLogged = null;
                Guid requestGuid = Guid.Empty;
                Guid responseGuid = Guid.Empty;
                bool exceptionLogged = false;
                bool activityLogged = false;

                TaskCompletionSource responseLoggedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

                var diagnosticListenerObserver = new FakeDiagnosticListenerObserver(kvp =>
                {
                    if (kvp.Key.Equals("System.Net.Http.Request"))
                    {
                        Assert.NotNull(kvp.Value);
                        requestLogged = GetProperty<HttpRequestMessage>(kvp.Value, "Request");
                        requestGuid = GetProperty<Guid>(kvp.Value, "LoggingRequestId");
                    }
                    else if (kvp.Key.Equals("System.Net.Http.Response"))
                    {
                        Assert.NotNull(kvp.Value);
                        responseLogged = GetProperty<HttpResponseMessage>(kvp.Value, "Response");
                        responseGuid = GetProperty<Guid>(kvp.Value, "LoggingRequestId");
                        TaskStatus requestStatus = GetProperty<TaskStatus>(kvp.Value, "RequestTaskStatus");
                        Assert.Equal(TaskStatus.RanToCompletion, requestStatus);
                        responseLoggedTcs.SetResult();
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

                    await GetFactoryForVersion(useVersion).CreateClientAndServerAsync(
                        async uri =>
                        {
                            (HttpRequestMessage request, HttpResponseMessage response) = await GetAsync(useVersion, testAsync, uri);

                            await responseLoggedTcs.Task;

                            Assert.Same(request, requestLogged);
                            Assert.Same(response, responseLogged);
                        },
                        async server => await server.HandleRequestAsync());

                    Assert.Equal(requestGuid, responseGuid);
                    Assert.False(exceptionLogged, "Exception was logged for successful request");
                    Assert.False(activityLogged, "HttpOutReq was logged while HttpOutReq logging was disabled");
                }
            }, UseVersion.ToString(), TestAsync.ToString()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task SendAsync_ExpectedDiagnosticSourceNoLogging()
        {
            await RemoteExecutor.Invoke(async (useVersion, testAsync) =>
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
                    await GetFactoryForVersion(useVersion).CreateClientAndServerAsync(
                        async uri =>
                        {
                            await GetAsync(useVersion, testAsync, uri);
                        },
                        async server =>
                        {
                            HttpRequestData request = await server.AcceptConnectionSendResponseAndCloseAsync();
                            AssertNoHeadersAreInjected(request);
                        });

                    Assert.False(requestLogged, "Request was logged while logging disabled.");
                    Assert.False(activityStartLogged, "HttpRequestOut.Start was logged while logging disabled.");
                    Assert.False(responseLogged, "Response was logged while logging disabled.");
                    Assert.False(activityStopLogged, "HttpRequestOut.Stop was logged while logging disabled.");
                }
            }, UseVersion.ToString(), TestAsync.ToString()).DisposeAsync();
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task SendAsync_HttpTracingEnabled_Succeeds(bool useSsl)
        {
            if (useSsl && UseVersion == HttpVersion.Version20 && !PlatformDetection.SupportsAlpn)
            {
                return;
            }

            await RemoteExecutor.Invoke(async (useVersion, useSsl, testAsync) =>
            {
                using (var listener = new TestEventListener("Private.InternalDiagnostics.System.Net.Http", EventLevel.Verbose))
                {
                    var events = new ConcurrentQueue<EventWrittenEventArgs>();
                    await listener.RunWithCallbackAsync(events.Enqueue, async () =>
                    {
                        // Exercise various code paths to get coverage of tracing
                        await GetFactoryForVersion(useVersion).CreateClientAndServerAsync(
                            async uri => await GetAsync(useVersion, testAsync, uri),
                            async server => await server.HandleRequestAsync(),
                            options: new GenericLoopbackOptions { UseSsl = bool.Parse(useSsl) });
                    });

                    // We don't validate receiving specific events, but rather that we do at least
                    // receive some events, and that enabling tracing doesn't cause other failures
                    // in processing.
                    Assert.DoesNotContain(events,
                        ev => ev.EventId == 0); // make sure there are no event source error messages
                    Assert.InRange(events.Count, 1, int.MaxValue);
                }
            }, UseVersion.ToString(), useSsl.ToString(), TestAsync.ToString()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task SendAsync_ExpectedDiagnosticExceptionLogging()
        {
            await RemoteExecutor.Invoke(async (useVersion, testAsync) =>
            {
                Exception exceptionLogged = null;

                TaskCompletionSource responseLoggedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

                var diagnosticListenerObserver = new FakeDiagnosticListenerObserver(kvp =>
                {
                    if (kvp.Key.Equals("System.Net.Http.Response"))
                    {
                        Assert.NotNull(kvp.Value);
                        TaskStatus requestStatus = GetProperty<TaskStatus>(kvp.Value, "RequestTaskStatus");
                        Assert.Equal(TaskStatus.Faulted, requestStatus);
                        responseLoggedTcs.SetResult();
                    }
                    else if (kvp.Key.Equals("System.Net.Http.Exception"))
                    {
                        Assert.NotNull(kvp.Value);
                        exceptionLogged = GetProperty<Exception>(kvp.Value, "Exception");
                    }
                });

                using (DiagnosticListener.AllListeners.Subscribe(diagnosticListenerObserver))
                {
                    diagnosticListenerObserver.Enable();

                    Exception ex = await Assert.ThrowsAsync<HttpRequestException>(() => GetAsync(useVersion, testAsync, InvalidUri));

                    await responseLoggedTcs.Task;

                    Assert.Same(ex, exceptionLogged);
                }
            }, UseVersion.ToString(), TestAsync.ToString()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task SendAsync_ExpectedDiagnosticCancelledLogging()
        {
            await RemoteExecutor.Invoke(async (useVersion, testAsync) =>
            {
                TaskCompletionSource responseLoggedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
                TaskCompletionSource activityStopTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

                var diagnosticListenerObserver = new FakeDiagnosticListenerObserver(kvp =>
                {
                    if (kvp.Key.Equals("System.Net.Http.Response"))
                    {
                        Assert.NotNull(kvp.Value);
                        TaskStatus status = GetProperty<TaskStatus>(kvp.Value, "RequestTaskStatus");
                        Assert.Equal(TaskStatus.Canceled, status);
                        responseLoggedTcs.SetResult();
                    }
                    else if (kvp.Key == "System.Net.Http.HttpRequestOut.Stop")
                    {
                        Assert.NotNull(kvp.Value);
                        GetProperty<HttpRequestMessage>(kvp.Value, "Request");
                        TaskStatus status = GetProperty<TaskStatus>(kvp.Value, "RequestTaskStatus");
                        Assert.Equal(TaskStatus.Canceled, status);
                        activityStopTcs.SetResult();
                    }
                });

                using (DiagnosticListener.AllListeners.Subscribe(diagnosticListenerObserver))
                {
                    diagnosticListenerObserver.Enable();

                    var cts = new CancellationTokenSource();

                    await GetFactoryForVersion(useVersion).CreateClientAndServerAsync(
                        async uri =>
                        {
                            await Assert.ThrowsAsync<TaskCanceledException>(() => GetAsync(useVersion, testAsync, uri, cts.Token));
                        },
                        async server =>
                        {
                            await server.AcceptConnectionAsync(async connection =>
                            {
                                cts.Cancel();

                                await responseLoggedTcs.Task;
                                await activityStopTcs.Task;
                            });
                        });
                }
            }, UseVersion.ToString(), TestAsync.ToString()).DisposeAsync();
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(ActivityIdFormat.Hierarchical)]
        [InlineData(ActivityIdFormat.W3C)]
        public async Task SendAsync_ExpectedDiagnosticSourceActivityLogging(ActivityIdFormat idFormat)
        {
            await RemoteExecutor.Invoke(async (useVersion, testAsync, idFormatString) =>
            {
                ActivityIdFormat idFormat = Enum.Parse<ActivityIdFormat>(idFormatString);

                bool requestLogged = false;
                bool responseLogged = false;
                bool exceptionLogged = false;
                HttpRequestMessage activityStartRequestLogged = null;
                HttpRequestMessage activityStopRequestLogged = null;
                HttpResponseMessage activityStopResponseLogged = null;

                TaskCompletionSource activityStopTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

                Activity parentActivity = new Activity("parent");
                parentActivity.SetIdFormat(idFormat);
                parentActivity.AddBaggage("correlationId", Guid.NewGuid().ToString("N").ToString());
                parentActivity.AddBaggage("moreBaggage", Guid.NewGuid().ToString("N").ToString());
                parentActivity.AddTag("tag", "tag"); // add tag to ensure it is not injected into request
                parentActivity.TraceStateString = "foo=1";

                parentActivity.Start();

                Assert.Equal(idFormat, parentActivity.IdFormat);

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
                        activityStartRequestLogged = GetProperty<HttpRequestMessage>(kvp.Value, "Request");
                    }
                    else if (kvp.Key.Equals("System.Net.Http.HttpRequestOut.Stop"))
                    {
                        Assert.NotNull(kvp.Value);
                        Assert.NotNull(Activity.Current);
                        Assert.Equal(parentActivity, Activity.Current.Parent);
                        Assert.True(Activity.Current.Duration != TimeSpan.Zero);
                        activityStopRequestLogged = GetProperty<HttpRequestMessage>(kvp.Value, "Request");
                        activityStopResponseLogged = GetProperty<HttpResponseMessage>(kvp.Value, "Response");
                        TaskStatus requestStatus = GetProperty<TaskStatus>(kvp.Value, "RequestTaskStatus");
                        Assert.Equal(TaskStatus.RanToCompletion, requestStatus);
                        activityStopTcs.SetResult();
                    }
                });

                using (DiagnosticListener.AllListeners.Subscribe(diagnosticListenerObserver))
                {
                    diagnosticListenerObserver.Enable(s => s.Contains("HttpRequestOut"));

                    await GetFactoryForVersion(useVersion).CreateClientAndServerAsync(
                        async uri =>
                        {
                            (HttpRequestMessage request, HttpResponseMessage response) = await GetAsync(useVersion, testAsync, uri);

                            await activityStopTcs.Task;

                            Assert.Same(request, activityStartRequestLogged);
                            Assert.Same(request, activityStopRequestLogged);
                            Assert.Same(response, activityStopResponseLogged);
                        },
                        async server =>
                        {
                            HttpRequestData requestData = await server.AcceptConnectionSendResponseAndCloseAsync();
                            AssertHeadersAreInjected(requestData, parentActivity);
                        });

                    Assert.False(requestLogged, "Request was logged when Activity logging was enabled.");
                    Assert.False(exceptionLogged, "Exception was logged for successful request");
                    Assert.False(responseLogged, "Response was logged when Activity logging was enabled.");
                }
            }, UseVersion.ToString(), TestAsync.ToString(), idFormat.ToString()).DisposeAsync();
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(200, "GET")]
        [InlineData(404, "GET")]
        [InlineData(200, "CUSTOM")]
        public async Task SendAsync_DiagnosticListener_ExpectedTagsRecorded(int statusCode, string method)
        {
            await RemoteExecutor.Invoke(static async (useVersion, testAsync, statusCodeStr, method) =>
            {
                TaskCompletionSource activityStopTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

                Activity parentActivity = new Activity("parent");
                parentActivity.Start();

                Uri? currentUri = null;
                string? expectedUriFull = null;

                var diagnosticListenerObserver = new FakeDiagnosticListenerObserver(kvp =>
                {
                    Assert.NotNull(currentUri);

                    if (!kvp.Key.StartsWith("System.Net.Http.HttpRequestOut"))
                    {
                        return;
                    }
                    Activity activity = Activity.Current;
                    Assert.NotNull(activity);
                    Assert.Equal(parentActivity, Activity.Current.Parent);
                    IEnumerable<KeyValuePair<string, object?>> tags = activity.TagObjects;
                    Assert.Equal(method is "CUSTOM" ? "HTTP" : method, activity.DisplayName);
                    VerifyRequestTags(tags, currentUri, expectedUriFull, expectedMethodTag: method is "CUSTOM" ? "_OTHER" : method);
                    VerifyTag(tags, "http.request.method_original", method is "CUSTOM" ? method : null);

                    if (kvp.Key.EndsWith(".Stop"))
                    {
                        VerifyTag(tags, "network.protocol.version", GetVersionString(Version.Parse(useVersion)));
                        VerifyTag(tags, "http.response.status_code", int.Parse(statusCodeStr));

                        if (statusCodeStr != "200")
                        {
                            string errorType = (string)tags.Single(t => t.Key == "error.type").Value;
                            Assert.Equal(ActivityStatusCode.Error, activity.Status);
                            Assert.Equal(statusCodeStr, errorType);
                        }
                        else
                        {
                            Assert.Equal(ActivityStatusCode.Unset, activity.Status);
                            Assert.DoesNotContain(tags, t => t.Key == "error.type");
                        }

                        activityStopTcs.SetResult();
                    }
                });

                using (DiagnosticListener.AllListeners.Subscribe(diagnosticListenerObserver))
                {
                    diagnosticListenerObserver.Enable(s => s.Contains("HttpRequestOut"));

                    await GetFactoryForVersion(useVersion).CreateClientAndServerAsync(
                        async uri =>
                        {
                            uri = new Uri($"{uri.Scheme}://user:pass@{uri.Authority}/1/2/?a=1&b=2");
                            expectedUriFull = $"{uri.Scheme}://{uri.Authority}{uri.AbsolutePath}?*";
                            currentUri = uri;

                            using HttpClient client = new(CreateHttpClientHandler(allowAllCertificates: true));
                            using HttpRequestMessage request = CreateRequest(HttpMethod.Parse(method), uri, Version.Parse(useVersion), exactVersion: true);
                            await client.SendAsync(bool.Parse(testAsync), request);
                            await activityStopTcs.Task;
                        },
                        async server =>
                        {
                            HttpRequestData requestData = await server.AcceptConnectionSendResponseAndCloseAsync(
                                statusCode: (HttpStatusCode)int.Parse(statusCodeStr));
                            AssertHeadersAreInjected(requestData, parentActivity);
                        });
                }
            }, UseVersion.ToString(), TestAsync.ToString(), statusCode.ToString(), method).DisposeAsync();
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(200, "GET")]
        [InlineData(404, "GET")]
        [InlineData(200, "CUSTOM")]
        public async Task SendAsync_ActivitySource_ExpectedTagsRecorded(int statusCode, string method)
        {
            await RemoteExecutor.Invoke(static async (useVersion, testAsync, statusCodeStr, method) =>
            {
                TaskCompletionSource activityStopTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
                Activity? activity = null;
                Uri? currentUri = null;
                string? expectedUriFull = null;

                using ActivityListener listener = new ActivityListener()
                {
                    ShouldListenTo = s => s.Name is "System.Net.Http",
                    Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                    ActivityStopped = a =>
                    {
                        activity = a;
                        var tags = activity.TagObjects;
                        VerifyRequestTags(a.TagObjects, currentUri, expectedUriFull, expectedMethodTag: method is "CUSTOM" ? "_OTHER" : method);
                        VerifyTag(tags, "http.request.method_original", method is "CUSTOM" ? method : null);
                        VerifyTag(tags, "network.protocol.version", GetVersionString(Version.Parse(useVersion)));
                        VerifyTag(tags, "http.response.status_code", int.Parse(statusCodeStr));

                        if (statusCodeStr != "200")
                        {
                            string errorType = (string)tags.Single(t => t.Key == "error.type").Value;
                            Assert.Equal(ActivityStatusCode.Error, activity.Status);
                            Assert.Equal(statusCodeStr, errorType);
                        }
                        else
                        {
                            Assert.Equal(ActivityStatusCode.Unset, activity.Status);
                            Assert.DoesNotContain(tags, t => t.Key == "error.type");
                        }

                        activityStopTcs.SetResult();
                    }
                };
                ActivitySource.AddActivityListener(listener);

                await GetFactoryForVersion(useVersion).CreateClientAndServerAsync(
                        async uri =>
                        {
                            uri = new Uri($"{uri.Scheme}://user:pass@{uri.Authority}/1/2/?a=1&b=2");
                            expectedUriFull = $"{uri.Scheme}://{uri.Authority}{uri.AbsolutePath}?*";
                            currentUri = uri;

                            using HttpClient client = new(CreateHttpClientHandler(allowAllCertificates: true));
                            using HttpRequestMessage request = CreateRequest(HttpMethod.Parse(method), uri, Version.Parse(useVersion), exactVersion: true);
                            await client.SendAsync(bool.Parse(testAsync), request);
                            await activityStopTcs.Task;
                        },
                        async server =>
                        {
                            await server.AcceptConnectionSendResponseAndCloseAsync(statusCode: (HttpStatusCode)int.Parse(statusCodeStr));
                        });

                Assert.NotNull(activity);
            }, UseVersion.ToString(), TestAsync.ToString(), statusCode.ToString(), method).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task SendAsync_DoNotSampleAllData_NoTagsRecorded()
        {
            await RemoteExecutor.Invoke(static async (useVersion, testAsync) =>
            {
                TaskCompletionSource activityStopTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
                Activity? activity = null;
                using ActivityListener listener = new ActivityListener()
                {
                    ShouldListenTo = s => s.Name is "System.Net.Http",
                    Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.PropagationData,
                    ActivityStopped = a =>
                    {
                        activity = a;
                        activityStopTcs.SetResult();
                    }
                };
                ActivitySource.AddActivityListener(listener);

                await GetFactoryForVersion(useVersion).CreateClientAndServerAsync(
                            async uri =>
                            {

                                await GetAsync(useVersion, testAsync, uri);
                                await activityStopTcs.Task;
                            },
                            async server =>
                            {
                                _ = await server.AcceptConnectionSendResponseAndCloseAsync();
                            });

                Assert.NotNull(activity);
                Assert.Empty(activity.TagObjects);
            }, UseVersion.ToString(), TestAsync.ToString()).DisposeAsync();
        }

        protected static void VerifyRequestTags(IEnumerable<KeyValuePair<string, object?>> tags, Uri uri, string expectedUriFull, string expectedMethodTag = "GET")
        {
            VerifyTag(tags, "server.address", uri.Host);
            VerifyTag(tags, "server.port", uri.Port);
            VerifyTag(tags, "http.request.method", expectedMethodTag);
            VerifyTag(tags, "url.full", expectedUriFull);
        }

        protected static void VerifyTag<T>(KeyValuePair<string, object?>[] tags, string name, T value)
        {
            if (value is null)
            {
                Assert.DoesNotContain(tags, t => t.Key == name);
            }
            else
            {
                Assert.Equal(value, (T)tags.Single(t => t.Key == name).Value);
            }
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task SendAsync_Success_ConnectionSetupActivityGraphRecorded(bool useTls)
        {
            if (UseVersion == HttpVersion30 && !useTls) return;

            await RemoteExecutor.Invoke(RunTest, UseVersion.ToString(), TestAsync.ToString(), useTls.ToString()).DisposeAsync();
            static async Task RunTest(string useVersion, string testAsync, string useTlsString)
            {
                bool useTls = bool.Parse(useTlsString);

                Activity parentActivity = new Activity("parent").Start();

                using ActivityRecorder requestRecorder = new("System.Net.Http", "System.Net.Http.HttpRequestOut")
                {
                    ExpectedParent = parentActivity
                };
                using ActivityRecorder waitForConnectionRecorder = new("Experimental.System.Net.Http.Connections", "Experimental.System.Net.Http.Connections.WaitForConnection")
                {
                    VerifyParent = false
                };

                using ActivityRecorder connectionSetupRecorder = new("Experimental.System.Net.Http.Connections", "Experimental.System.Net.Http.Connections.ConnectionSetup");
                using ActivityRecorder dnsRecorder = new("Experimental.System.Net.NameResolution", "Experimental.System.Net.NameResolution.DnsLookup") { VerifyParent = false };
                using ActivityRecorder socketRecorder = new("Experimental.System.Net.Sockets", "Experimental.System.Net.Sockets.Connect") { VerifyParent = false };
                using ActivityRecorder tlsRecorder = new("Experimental.System.Net.Security", "Experimental.System.Net.Security.TlsHandshake")
                {
                    VerifyParent = false
                };

                await GetFactoryForVersion(useVersion).CreateClientAndServerAsync(
                    async uri =>
                    {
                        Version version = Version.Parse(useVersion);
                        if (version != HttpVersion30)
                        {
                            uri = new Uri($"{uri.Scheme}://localhost:{uri.Port}");
                        }

                        using HttpClient client = new HttpClient(CreateHttpClientHandler(allowAllCertificates: true));

                        await client.SendAsync(bool.Parse(testAsync), CreateRequest(HttpMethod.Get, uri, version, exactVersion: true));

                        Activity req1 = requestRecorder.VerifyActivityRecordedOnce();
                        Activity wait1 = waitForConnectionRecorder.VerifyActivityRecordedOnce();
                        Activity conn = connectionSetupRecorder.VerifyActivityRecordedOnce();

                        Activity? dns = null;
                        Activity? sock = null;
                        Activity? tls = null;

                        if (version != HttpVersion30)
                        {
                            dns = dnsRecorder.VerifyActivityRecordedOnce();
                            Assert.True(socketRecorder.Stopped is 1 or 2);
                            sock = socketRecorder.LastFinishedActivity;

                            if (useTls)
                            {
                                tls = tlsRecorder.FinishedActivities.Single(a => a.DisplayName.StartsWith("TLS client"));
                            }
                            else
                            {
                                tlsRecorder.VerifyActivityRecorded(0);
                            }
                        }

                        // Verify relationships between request and connection_setup, wait_for_connection:
                        Assert.Same(parentActivity, req1.Parent);
                        Assert.Same(req1, wait1.Parent);

                        // Verify timing relationships between request, wait_for_connection, connection_setup:
                        ActivityAssert.FinishedInOrder(conn, wait1);
                        ActivityAssert.FinishedInOrder(wait1, req1);

                        // req1->conn link:
                        req1.Links.Single(l => l.Context == conn.Context);

                        // Verify the connection_setup graph:
                        Assert.Null(conn.Parent);

                        if (version != HttpVersion30)
                        {
                            Assert.Same(conn, dns.Parent);
                            Assert.Same(conn, sock.Parent);
                            if (useTls)
                            {
                                Assert.Same(conn, tls.Parent);
                            }

                            // Verify timing relationships for connection setup:
                            ActivityAssert.FinishedInOrder(dns, sock);
                            if (useTls)
                            {
                                ActivityAssert.FinishedInOrder(sock, tls);
                                ActivityAssert.FinishedInOrder(tls, conn);
                            }
                            else
                            {
                                ActivityAssert.FinishedInOrder(sock, conn);
                            }
                        }

                        // Verify display names and attributes:
                        Assert.Equal(ActivityKind.Internal, wait1.Kind);
                        Assert.Equal(ActivityKind.Internal, conn.Kind);
                        Assert.Equal($"HTTP wait_for_connection {uri.Host}:{uri.Port}", wait1.DisplayName);
                        Assert.Equal($"HTTP connection_setup {uri.Host}:{uri.Port}", conn.DisplayName);
                        ActivityAssert.HasTag(conn, "network.peer.address",
                            (string a) => a == IPAddress.Loopback.ToString() ||
                            a == IPAddress.Loopback.MapToIPv6().ToString() ||
                            a == IPAddress.IPv6Loopback.ToString());
                        ActivityAssert.HasTag(conn, "server.address", uri.Host);
                        ActivityAssert.HasTag(conn, "server.port", uri.Port);
                        ActivityAssert.HasTag(conn, "url.scheme", useTls ? "https" : "http");

                        // The second request should reuse the first connection, connection_setup and wait_for_connection should not be recorded again.
                        await client.SendAsync(CreateRequest(HttpMethod.Get, uri, Version.Parse(useVersion), exactVersion: true));
                        requestRecorder.VerifyActivityRecorded(2);
                        Activity req2 = requestRecorder.LastFinishedActivity;
                        Assert.NotSame(req1, req2);
                        waitForConnectionRecorder.VerifyActivityRecorded(1);
                        connectionSetupRecorder.VerifyActivityRecorded(1);

                        // The second request should also have a link to the shared connection.
                        req2.Links.Single(l => l.Context == conn.Context);
                    },
                    async server =>
                    {
                        await server.AcceptConnectionAsync(async connection =>
                        {
                            await connection.ReadRequestDataAsync();
                            await connection.SendResponseAsync(HttpStatusCode.OK);
                            connection.CompleteRequestProcessing();

                            await connection.ReadRequestDataAsync();
                            await connection.SendResponseAsync(HttpStatusCode.OK);
                        });
                    }, options: new GenericLoopbackOptions()
                    {
                        UseSsl = useTls
                    });
            }
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData("dns")]
        [InlineData("socket")]
        public async Task SendAsync_ConnectionFailure_RecordsActivitiesWithCorrectErrorInfo(string failureType)
        {
            await RemoteExecutor.Invoke(RunTest, UseVersion.ToString(), TestAsync.ToString(), failureType).DisposeAsync();
            static async Task RunTest(string useVersion, string testAsync, string failureType)
            {
                Version version = Version.Parse(useVersion);

                using HttpClientHandler handler = CreateHttpClientHandler(allowAllCertificates: true);
                using HttpClient client = new HttpClient(handler);

                Activity parentActivity = new Activity("parent").Start();
                using ActivityRecorder requestRecorder = new("System.Net.Http", "System.Net.Http.HttpRequestOut")
                {
                    ExpectedParent = parentActivity
                };
                using ActivityRecorder waitForConnectionRecorder = new("Experimental.System.Net.Http.Connections", "Experimental.System.Net.Http.Connections.WaitForConnection")
                {
                    VerifyParent = false
                };
                using ActivityRecorder connectionSetupRecorder = new("Experimental.System.Net.Http.Connections", "Experimental.System.Net.Http.Connections.ConnectionSetup");
                using ActivityRecorder dnsRecorder = new("Experimental.System.Net.NameResolution", "Experimental.System.Net.NameResolution.DnsLookup") { VerifyParent = false };
                using ActivityRecorder socketRecorder = new("Experimental.System.Net.Sockets", "Experimental.System.Net.Sockets.Connect") { VerifyParent = false };

                Uri uri;
                using Socket? notListening = failureType is "socket"
                    ? (version == HttpVersion30) ? new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp) : new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                    : null;

                if (failureType is "dns")
                {
                    uri = new Uri("https://does.not.exist.sorry");
                }
                else
                {
                    Debug.Assert(notListening is not null);
                    notListening.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                    IPEndPoint ep = (IPEndPoint)notListening.LocalEndPoint;
                    uri = new Uri($"https://{ep.Address}:{ep.Port}");
                }

                using HttpRequestMessage request = CreateRequest(HttpMethod.Get, uri, version, exactVersion: true);
                await Assert.ThrowsAsync<HttpRequestException>(() => client.SendAsync(bool.Parse(testAsync), request));

                Activity req = requestRecorder.VerifyActivityRecordedOnce();
                Activity wait = waitForConnectionRecorder.VerifyActivityRecordedOnce();
                Activity conn = connectionSetupRecorder.VerifyActivityRecordedOnce();

                Assert.Same(req, wait.Parent);
                Assert.Null(conn.Parent);

                if (failureType == "dns")
                {
                    Assert.Equal(ActivityStatusCode.Error, conn.Status);
                    Assert.Equal(ActivityStatusCode.Error, wait.Status);

                    ActivityAssert.HasTag(conn, "error.type", "name_resolution_error");
                    ActivityAssert.HasTag(wait, "error.type", "name_resolution_error");

                    ActivityEvent evt = req.Events.Single(e => e.Name == "exception");
                    Dictionary<string, object?> tags = evt.Tags.ToDictionary(t => t.Key, t => t.Value);
                    Assert.Contains("exception.type", tags.Keys);
                    Assert.Contains("exception.message", tags.Keys);
                    Assert.Contains("exception.stacktrace", tags.Keys);
                    Assert.Equal(typeof(HttpRequestException).FullName, tags["exception.type"]);
                    Assert.InRange(evt.Timestamp - req.StartTimeUtc, new TimeSpan(1), req.Duration);

                    // Whether System.Net.Quic uses System.Net.Dns is an implementation detail.
                    if (version != HttpVersion30)
                    {
                        Activity dns = dnsRecorder.VerifyActivityRecordedOnce();
                        Assert.Same(conn, dns.Parent);
                        Assert.Equal(ActivityStatusCode.Error, dns.Status);
                        ActivityAssert.HasTag(dns, "error.type", (string t) => t is "host_not_found" or "timed_out");
                    }
                }
                else
                {
                    Debug.Assert(failureType is "socket");

                    Assert.Equal(ActivityStatusCode.Error, conn.Status);
                    Assert.Equal(ActivityStatusCode.Error, wait.Status);

                    ActivityAssert.HasTag(conn, "error.type", "connection_error");
                    ActivityAssert.HasTag(wait, "error.type", "connection_error");

                    ActivityEvent evt = req.Events.Single(e => e.Name == "exception");
                    Dictionary<string, object?> tags = evt.Tags.ToDictionary(t => t.Key, t => t.Value);
                    Assert.Contains("exception.type", tags.Keys);
                    Assert.Contains("exception.message", tags.Keys);
                    Assert.Contains("exception.stacktrace", tags.Keys);
                    Assert.Equal(typeof(HttpRequestException).FullName, tags["exception.type"]);
                    Assert.InRange(evt.Timestamp - req.StartTimeUtc, new TimeSpan(1), req.Duration);

                    if (version != HttpVersion30)
                    {
                        Activity sock = socketRecorder.VerifyActivityRecordedOnce();
                        Assert.Same(conn, sock.Parent);
                        Assert.Equal(ActivityStatusCode.Error, sock.Status);
                        ActivityAssert.HasTag(sock, "error.type", (string t) => t is "connection_refused" or "timed_out");
                    }
                }
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task SendAsync_OperationCanceledException_RecordsActivitiesWithCorrectErrorInfo()
        {
            await RemoteExecutor.Invoke(RunTest, UseVersion.ToString(), TestAsync.ToString()).DisposeAsync();
            static async Task RunTest(string useVersion, string testAsync)
            {
                TaskCompletionSource activityStopTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
                Activity? activity = null;

                using ActivityListener listener = new ActivityListener()
                {
                    ShouldListenTo = s => s.Name is "System.Net.Http",
                    Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                    ActivityStopped = a =>
                    {
                        activity = a;
                        Assert.Equal(ActivityStatusCode.Error, a.Status);
                        ActivityAssert.HasTag(a, "error.type", typeof(TaskCanceledException).FullName);
                        ActivityEvent evt = a.Events.Single(e => e.Name == "exception");
                        Dictionary<string, object?> tags = evt.Tags.ToDictionary(t => t.Key, t => t.Value);
                        Assert.Contains("exception.type", tags.Keys);
                        Assert.Contains("exception.message", tags.Keys);
                        Assert.Contains("exception.stacktrace", tags.Keys);
                        Assert.Equal(typeof(TaskCanceledException).FullName, tags["exception.type"]);

                        activityStopTcs.SetResult();
                    }
                };
                ActivitySource.AddActivityListener(listener);

                var cts = new CancellationTokenSource();

                await GetFactoryForVersion(useVersion).CreateClientAndServerAsync(
                    async uri =>
                    {
                        Version version = Version.Parse(useVersion);
                        if (version != HttpVersion30)
                        {
                            uri = new Uri($"{uri.Scheme}://localhost:{uri.Port}");
                        }

                        await Assert.ThrowsAsync<TaskCanceledException>(() => GetAsync(useVersion, testAsync, uri, cts.Token));
                    },
                    async server =>
                    {
                        await server.AcceptConnectionAsync(async connection =>
                        {
                            cts.Cancel();

                            await activityStopTcs.Task;
                        });
                    });

                Assert.NotNull(activity);
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task SendAsync_ExpectedDiagnosticSourceActivityLogging_InvalidBaggage()
        {
            await RemoteExecutor.Invoke(async (useVersion, testAsync) =>
            {
                bool exceptionLogged = false;

                TaskCompletionSource activityStopTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

                // To test the Hierarchical propagation format, we need to set the legacy propagator.
                DistributedContextPropagator.Current = DistributedContextPropagator.CreatePreW3CPropagator();

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
                        HttpRequestMessage request = GetProperty<HttpRequestMessage>(kvp.Value, "Request");
                        Assert.True(request.Headers.TryGetValues("Request-Id", out var requestId));
                        Assert.True(request.Headers.TryGetValues("Correlation-Context", out var correlationContext));
                        Assert.Equal("key=value, goodkey=bad%2Fvalue, bad%2Fkey=value", Assert.Single(correlationContext));
                        TaskStatus requestStatus = GetProperty<TaskStatus>(kvp.Value, "RequestTaskStatus");
                        Assert.Equal(TaskStatus.RanToCompletion, requestStatus);
                        activityStopTcs.SetResult();
                    }
                    else if (kvp.Key.Equals("System.Net.Http.Exception"))
                    {
                        exceptionLogged = true;
                    }
                });

                using (DiagnosticListener.AllListeners.Subscribe(diagnosticListenerObserver))
                {
                    diagnosticListenerObserver.Enable(s => s.Contains("HttpRequestOut"));

                    await GetFactoryForVersion(useVersion).CreateClientAndServerAsync(
                        async uri =>
                        {
                            await GetAsync(useVersion, testAsync, uri);
                        },
                        async server => await server.HandleRequestAsync());

                    await activityStopTcs.Task;

                    Assert.False(exceptionLogged, "Exception was logged for successful request");
                }
            }, UseVersion.ToString(), TestAsync.ToString()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task SendAsync_ExpectedDiagnosticSourceActivityLoggingDoesNotOverwriteHeader()
        {
            await RemoteExecutor.Invoke(async (useVersion, testAsync) =>
            {
                bool activityStartLogged = false;

                TaskCompletionSource activityStopTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

                Activity parentActivity = new Activity("parent");
                parentActivity.SetIdFormat(ActivityIdFormat.Hierarchical);
                parentActivity.AddBaggage("correlationId", Guid.NewGuid().ToString("N").ToString());
                parentActivity.Start();

                string customRequestIdHeader = "|foo.bar.";
                var diagnosticListenerObserver = new FakeDiagnosticListenerObserver(kvp =>
                {
                    if (kvp.Key.Equals("System.Net.Http.HttpRequestOut.Start"))
                    {
                        HttpRequestMessage request = GetProperty<HttpRequestMessage>(kvp.Value, "Request");
                        request.Headers.Add("Request-Id", customRequestIdHeader);

                        activityStartLogged = true;
                    }
                    else if (kvp.Key.Equals("System.Net.Http.HttpRequestOut.Stop"))
                    {
                        HttpRequestMessage request = GetProperty<HttpRequestMessage>(kvp.Value, "Request");
                        Assert.Single(request.Headers.GetValues("Request-Id"));
                        Assert.Equal(customRequestIdHeader, request.Headers.GetValues("Request-Id").Single());

                        Assert.False(request.Headers.TryGetValues("traceparent", out var _));
                        Assert.False(request.Headers.TryGetValues("tracestate", out var _));
                        activityStopTcs.SetResult();
                    }
                });

                using (DiagnosticListener.AllListeners.Subscribe(diagnosticListenerObserver))
                {
                    diagnosticListenerObserver.Enable();

                    await GetFactoryForVersion(useVersion).CreateClientAndServerAsync(
                        async uri =>
                        {
                            await GetAsync(useVersion, testAsync, uri);
                        },
                        async server => await server.HandleRequestAsync());

                    await activityStopTcs.Task;

                    Assert.True(activityStartLogged, "HttpRequestOut.Start was not logged.");
                }
            }, UseVersion.ToString(), TestAsync.ToString()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task SendAsync_ExpectedDiagnosticSourceActivityLoggingDoesNotOverwriteW3CTraceParentHeader()
        {
            await RemoteExecutor.Invoke(async (useVersion, testAsync) =>
            {
                bool activityStartLogged = false;

                TaskCompletionSource activityStopTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

                Activity parentActivity = new Activity("parent");
                parentActivity.SetParentId(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom());
                parentActivity.TraceStateString = "some=state";
                parentActivity.Start();

                string customTraceParentHeader = "00-abcdef0123456789abcdef0123456789-abcdef0123456789-01";
                var diagnosticListenerObserver = new FakeDiagnosticListenerObserver(kvp =>
                {
                    if (kvp.Key.Equals("System.Net.Http.HttpRequestOut.Start"))
                    {
                        HttpRequestMessage request = GetProperty<HttpRequestMessage>(kvp.Value, "Request");
                        Assert.Single(request.Headers.GetValues("traceparent"));
                        Assert.False(request.Headers.TryGetValues("tracestate", out var _));
                        Assert.Equal(customTraceParentHeader, request.Headers.GetValues("traceparent").Single());

                        Assert.False(request.Headers.TryGetValues("Request-Id", out var _));

                        activityStartLogged = true;
                    }
                    else if (kvp.Key.Equals("System.Net.Http.HttpRequestOut.Stop"))
                    {
                        activityStopTcs.SetResult();
                    }
                });

                using (DiagnosticListener.AllListeners.Subscribe(diagnosticListenerObserver))
                {
                    diagnosticListenerObserver.Enable();

                    await GetFactoryForVersion(useVersion).CreateClientAndServerAsync(
                        async uri =>
                        {
                            using HttpClient client = CreateHttpClient(useVersion);
                            var request = new HttpRequestMessage(HttpMethod.Get, uri)
                            {
                                Version = Version.Parse(useVersion)
                            };
                            request.Headers.Add("traceparent", customTraceParentHeader);
                            await client.SendAsync(bool.Parse(testAsync), request);
                        },
                        async server => await server.HandleRequestAsync());

                    await activityStopTcs.Task;

                    Assert.True(activityStartLogged, "HttpRequestOut.Start was not logged.");
                }
            }, UseVersion.ToString(), TestAsync.ToString()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task SendAsync_ExpectedDiagnosticSourceUrlFilteredActivityLogging()
        {
            await RemoteExecutor.Invoke(async (useVersion, testAsync) =>
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
                    await GetFactoryForVersion(useVersion).CreateClientAndServerAsync(
                        async uri =>
                        {
                            diagnosticListenerObserver.Enable((s, r, _) =>
                            {
                                if (s.StartsWith("System.Net.Http.HttpRequestOut") && r is HttpRequestMessage request)
                                {
                                    return request.RequestUri != uri;
                                }
                                return true;
                            });

                            await GetAsync(useVersion, testAsync, uri);
                        },
                        async server => await server.HandleRequestAsync());

                    Assert.False(activityStartLogged, "HttpRequestOut.Start was logged while URL disabled.");
                    Assert.False(activityStopLogged, "HttpRequestOut.Stop was logged while URL disabled.");
                }
            }, UseVersion.ToString(), TestAsync.ToString()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task SendAsync_ExpectedDiagnosticExceptionActivityLogging()
        {
            await RemoteExecutor.Invoke(async (useVersion, testAsync) =>
            {
                Exception exceptionLogged = null;

                TaskCompletionSource activityStopTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

                var diagnosticListenerObserver = new FakeDiagnosticListenerObserver(kvp =>
                {
                    if (kvp.Key.Equals("System.Net.Http.HttpRequestOut.Stop"))
                    {
                        Assert.NotNull(kvp.Value);
                        GetProperty<HttpRequestMessage>(kvp.Value, "Request");
                        TaskStatus requestStatus = GetProperty<TaskStatus>(kvp.Value, "RequestTaskStatus");
                        Assert.Equal(TaskStatus.Faulted, requestStatus);
                        activityStopTcs.SetResult();
                    }
                    else if (kvp.Key.Equals("System.Net.Http.Exception"))
                    {
                        Assert.NotNull(kvp.Value);
                        exceptionLogged = GetProperty<Exception>(kvp.Value, "Exception");
                    }
                });

                using (DiagnosticListener.AllListeners.Subscribe(diagnosticListenerObserver))
                {
                    diagnosticListenerObserver.Enable();

                    Exception ex = await Assert.ThrowsAsync<HttpRequestException>(() => GetAsync(useVersion, testAsync, InvalidUri));

                    await activityStopTcs.Task;

                    Assert.Same(ex, exceptionLogged);
                }
            }, UseVersion.ToString(), TestAsync.ToString()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task SendAsync_ExpectedDiagnosticSourceNewAndDeprecatedEventsLogging()
        {
            await RemoteExecutor.Invoke(async (useVersion, testAsync) =>
            {
                bool requestLogged = false;
                bool activityStartLogged = false;
                bool activityStopLogged = false;

                TaskCompletionSource responseLoggedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

                var diagnosticListenerObserver = new FakeDiagnosticListenerObserver(kvp =>
                {
                    if (kvp.Key.Equals("System.Net.Http.Request"))
                    {
                        requestLogged = true;
                    }
                    else if (kvp.Key.Equals("System.Net.Http.Response"))
                    {
                        responseLoggedTcs.SetResult();
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

                    await GetFactoryForVersion(useVersion).CreateClientAndServerAsync(
                        async uri =>
                        {
                            await GetAsync(useVersion, testAsync, uri);
                        },
                        async server => await server.HandleRequestAsync());

                    await responseLoggedTcs.Task;

                    Assert.True(activityStartLogged, "HttpRequestOut.Start was not logged.");
                    Assert.True(requestLogged, "Request was not logged.");
                    Assert.True(activityStopLogged, "HttpRequestOut.Stop was not logged.");
                }
            }, UseVersion.ToString(), TestAsync.ToString()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task SendAsync_ExpectedDiagnosticExceptionOnlyActivityLogging()
        {
            await RemoteExecutor.Invoke(async (useVersion, testAsync) =>
            {
                bool activityLogged = false;
                Exception exceptionLogged = null;

                TaskCompletionSource exceptionLoggedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

                var diagnosticListenerObserver = new FakeDiagnosticListenerObserver(kvp =>
                {
                    if (kvp.Key.Equals("System.Net.Http.HttpRequestOut.Stop"))
                    {
                        activityLogged = true;
                    }
                    else if (kvp.Key.Equals("System.Net.Http.Exception"))
                    {
                        Assert.NotNull(kvp.Value);
                        exceptionLogged = GetProperty<Exception>(kvp.Value, "Exception");
                        exceptionLoggedTcs.SetResult();
                    }
                });

                using (DiagnosticListener.AllListeners.Subscribe(diagnosticListenerObserver))
                {
                    diagnosticListenerObserver.Enable(s => s.Equals("System.Net.Http.Exception"));

                    Exception ex = await Assert.ThrowsAsync<HttpRequestException>(() => GetAsync(useVersion, testAsync, InvalidUri));

                    await exceptionLoggedTcs.Task;

                    Assert.Same(ex, exceptionLogged);
                    Assert.False(activityLogged, "HttpOutReq was logged when logging was disabled");
                }
            }, UseVersion.ToString(), TestAsync.ToString()).DisposeAsync();
        }

        public static IEnumerable<object[]> UseSocketsHttpHandler_WithIdFormat_MemberData()
        {
            yield return new object[] { true, ActivityIdFormat.Hierarchical };
            yield return new object[] { true, ActivityIdFormat.W3C };
            yield return new object[] { false, ActivityIdFormat.Hierarchical };
            yield return new object[] { false, ActivityIdFormat.W3C };
        }

        [ConditionalTheory(nameof(EnableActivityPropagationEnvironmentVariableIsNotSetAndRemoteExecutorSupported))]
        [InlineData("true")]
        [InlineData("1")]
        [InlineData("0")]
        [InlineData("false")]
        [InlineData("FALSE")]
        [InlineData("fAlSe")]
        [InlineData("helloworld")]
        [InlineData("")]
        public async Task SendAsync_SuppressedGlobalStaticPropagationEnvVar(string envVarValue)
        {
            await RemoteExecutor.Invoke(async (useVersion, testAsync, envVarValue) =>
            {
                Environment.SetEnvironmentVariable(EnableActivityPropagationEnvironmentVariableSettingName, envVarValue);

                bool isInstrumentationEnabled = !(envVarValue == "0" || envVarValue.Equals("false", StringComparison.OrdinalIgnoreCase));

                bool anyEventLogged = false;
                var diagnosticListenerObserver = new FakeDiagnosticListenerObserver(kvp =>
                {
                    anyEventLogged = true;
                });

                using (DiagnosticListener.AllListeners.Subscribe(diagnosticListenerObserver))
                {
                    diagnosticListenerObserver.Enable();

                    await GetFactoryForVersion(useVersion).CreateClientAndServerAsync(
                        async uri =>
                        {
                            Activity parent = new Activity("parent").Start();
                            (HttpRequestMessage request, _) = await GetAsync(useVersion, testAsync, uri);

                            string headerName = parent.IdFormat == ActivityIdFormat.Hierarchical ? "Request-Id" : "traceparent";
                            Assert.Equal(isInstrumentationEnabled, request.Headers.Contains(headerName));
                        },
                        async server => await server.HandleRequestAsync());

                    Assert.Equal(isInstrumentationEnabled, anyEventLogged);
                }
            }, UseVersion.ToString(), TestAsync.ToString(), envVarValue).DisposeAsync();
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [MemberData(nameof(UseSocketsHttpHandler_WithIdFormat_MemberData))]
        public async Task SendAsync_HeadersAreInjectedOnRedirects(bool useSocketsHttpHandler, ActivityIdFormat idFormat)
        {
            Activity parent = new Activity("parent");
            parent.SetIdFormat(idFormat);
            parent.TraceStateString = "foo=1";
            parent.Start();

            await GetFactoryForVersion(UseVersion).CreateServerAsync(async (originalServer, originalUri) =>
            {
                await GetFactoryForVersion(UseVersion).CreateServerAsync(async (redirectServer, redirectUri) =>
                {
                    Task clientTask = GetAsync(UseVersion.ToString(), TestAsync.ToString(), originalUri, useSocketsHttpHandler: useSocketsHttpHandler);

                    Task<HttpRequestData> serverTask = originalServer.HandleRequestAsync(HttpStatusCode.Redirect, new[] { new HttpHeaderData("Location", redirectUri.AbsoluteUri) });

                    await Task.WhenAny(clientTask, serverTask);
                    Assert.False(clientTask.IsCompleted, $"{clientTask.Status}: {clientTask.Exception}");
                    HttpRequestData firstRequestData = await serverTask;
                    AssertHeadersAreInjected(firstRequestData, parent);

                    serverTask = redirectServer.HandleRequestAsync();
                    await TestHelper.WhenAllCompletedOrAnyFailed(clientTask, serverTask);
                    HttpRequestData secondRequestData = await serverTask;
                    AssertHeadersAreInjected(secondRequestData, parent);

                    if (idFormat == ActivityIdFormat.W3C)
                    {
                        string firstParent = GetHeaderValue(firstRequestData, "traceparent");
                        string firstState = GetHeaderValue(firstRequestData, "tracestate");
                        Assert.True(ActivityContext.TryParse(firstParent, firstState, out ActivityContext firstContext));

                        string secondParent = GetHeaderValue(secondRequestData, "traceparent");
                        string secondState = GetHeaderValue(secondRequestData, "tracestate");
                        Assert.True(ActivityContext.TryParse(secondParent, secondState, out ActivityContext secondContext));

                        Assert.Equal(firstContext.TraceId, secondContext.TraceId);
                        Assert.Equal(firstContext.TraceFlags, secondContext.TraceFlags);
                        Assert.Equal(firstContext.TraceState, secondContext.TraceState);
                        Assert.NotEqual(firstContext.SpanId, secondContext.SpanId);
                    }
                    else
                    {
                        // Hierarchical format is not supported with the default W3C propgator. Only Legacy propagator support it.
                        Assert.Null(GetHeaderValue(firstRequestData, "Request-Id"));
                        Assert.Null(GetHeaderValue(secondRequestData, "Request-Id"));
                    }
                });
            });
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
        [InlineData(false)]
        public async Task SendAsync_SuppressedGlobalStaticPropagationNoListenerAppCtx(bool switchValue)
        {
            await RemoteExecutor.Invoke(async (useVersion, testAsync, switchValue) =>
            {
                AppContext.SetSwitch(EnableActivityPropagationAppCtxSettingName, bool.Parse(switchValue));

                await GetFactoryForVersion(useVersion).CreateClientAndServerAsync(
                    async uri =>
                    {
                        Activity parent = new Activity("parent").Start();
                        (HttpRequestMessage request, _) = await GetAsync(useVersion, testAsync, uri);

                        string headerName = parent.IdFormat == ActivityIdFormat.Hierarchical ? "Request-Id" : "traceparent";

                        Assert.Equal(bool.Parse(switchValue), request.Headers.Contains(headerName));
                    },
                    async server => await server.HandleRequestAsync());
            }, UseVersion.ToString(), TestAsync.ToString(), switchValue.ToString()).DisposeAsync();
        }

        public static IEnumerable<object[]> SocketsHttpHandlerPropagators_WithIdFormat_MemberData()
        {
            foreach (var propagator in new[]
                                        {
                                            null,
                                            DistributedContextPropagator.CreateDefaultPropagator(),
                                            DistributedContextPropagator.CreatePreW3CPropagator(),
                                            DistributedContextPropagator.CreateNoOutputPropagator(),
                                            DistributedContextPropagator.CreatePassThroughPropagator()
                                        })
            {
                foreach (ActivityIdFormat format in new[] { ActivityIdFormat.Hierarchical, ActivityIdFormat.W3C })
                {
                    yield return new object[] { propagator, format };
                }
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [MemberData(nameof(SocketsHttpHandlerPropagators_WithIdFormat_MemberData))]
        public async Task SendAsync_CustomSocketsHttpHandlerPropagator_PropagatorIsUsed(DistributedContextPropagator propagator, ActivityIdFormat idFormat)
        {
            Activity parent = new Activity("parent");
            parent.SetIdFormat(idFormat);
            parent.Start();

            await GetFactoryForVersion(UseVersion).CreateClientAndServerAsync(
                async uri =>
                {
                    using var handler = CreateSocketsHttpHandler(allowAllCertificates: true);
                    handler.ActivityHeadersPropagator = propagator;
                    using var client = new HttpClient(handler);
                    var request = CreateRequest(HttpMethod.Get, uri, UseVersion, exactVersion: true);
                    await client.SendAsync(TestAsync, request);
                },
                async server =>
                {
                    HttpRequestData requestData = await server.HandleRequestAsync();

                    if (propagator is null || ReferenceEquals(propagator, DistributedContextPropagator.CreateNoOutputPropagator()))
                    {
                        AssertNoHeadersAreInjected(requestData);
                    }
                    else
                    {
                        AssertHeadersAreInjected(requestData, parent, ReferenceEquals(propagator, DistributedContextPropagator.CreatePassThroughPropagator()), ReferenceEquals(propagator, DistributedContextPropagator.CreateDefaultPropagator()));
                    }
                });
        }

        public static IEnumerable<object[]> SocketsHttpHandler_ActivityCreation_MemberData()
        {
            foreach (var currentActivitySet in new bool[] {
                true,    // Activity was set
                false }) // No Activity is set
            {
                foreach (var diagnosticListenerActivityEnabled in new bool?[] {
                    true,   // DiagnosticListener requested an Activity
                    false,  // DiagnosticListener does not want an Activity
                    null }) // There is no DiagnosticListener
                {
                    foreach (var activitySourceCreatesActivity in new bool?[] {
                        true,   // ActivitySource created an Activity
                        false,  // ActivitySource chose not to create an Activity
                        null }) // ActivitySource had no listeners
                    {
                        yield return new object[] { currentActivitySet, diagnosticListenerActivityEnabled, activitySourceCreatesActivity };
                    }
                }
            }
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [MemberData(nameof(SocketsHttpHandler_ActivityCreation_MemberData))]
        public async Task SendAsync_ActivityIsCreatedIfRequested(bool currentActivitySet, bool? diagnosticListenerActivityEnabled, bool? activitySourceCreatesActivity)
        {
            string parameters = $"{currentActivitySet},{diagnosticListenerActivityEnabled},{activitySourceCreatesActivity}";
            await RemoteExecutor.Invoke(static async (useVersion, testAsync, parametersString) =>
            {
                bool?[] parameters = parametersString.Split(',').Select(p => p.Length == 0 ? (bool?)null : bool.Parse(p)).ToArray();
                bool currentActivitySet = parameters[0].Value;
                bool? diagnosticListenerActivityEnabled = parameters[1];
                bool? activitySourceCreatesActivity = parameters[2];

                bool madeASamplingDecision = false;
                if (activitySourceCreatesActivity.HasValue)
                {
                    ActivitySource.AddActivityListener(new ActivityListener
                    {
                        ShouldListenTo = s => s.Name is "System.Net.Http",
                        Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                        {
                            madeASamplingDecision = true;
                            return activitySourceCreatesActivity.Value ? ActivitySamplingResult.AllData : ActivitySamplingResult.None;
                        }
                    });
                }

                bool listenerCallbackWasCalled = false;
                IDisposable listenerSubscription = new MemoryStream(); // Dummy disposable
                if (diagnosticListenerActivityEnabled.HasValue)
                {
                    var diagnosticListenerObserver = new FakeDiagnosticListenerObserver(_ => listenerCallbackWasCalled = true);

                    diagnosticListenerObserver.Enable(name => !name.Contains("HttpRequestOut") || diagnosticListenerActivityEnabled.Value);

                    listenerSubscription = DiagnosticListener.AllListeners.Subscribe(diagnosticListenerObserver);
                }

                Activity parent = currentActivitySet ? new Activity("parent").Start() : null;
                Activity activity = parent;

                if (!currentActivitySet)
                {
                    // Listen to new activity creations if an Activity was created without a parent
                    // (when a DiagnosticListener forced one to be created)
                    ActivitySource.AddActivityListener(new ActivityListener
                    {
                        ShouldListenTo = s => s.Name is "System.Net.Http" or "",
                        ActivityStarted = created =>
                        {
                            Assert.Null(parent);
                            activity = created;
                        }
                    });
                }

                using (listenerSubscription)
                {
                    await GetFactoryForVersion(useVersion).CreateClientAndServerAsync(
                        async uri =>
                        {
                            await GetAsync(useVersion, testAsync, uri);
                        },
                        async server =>
                        {
                            HttpRequestData requestData = await server.AcceptConnectionSendResponseAndCloseAsync();

                            if (currentActivitySet || diagnosticListenerActivityEnabled == true || activitySourceCreatesActivity == true)
                            {
                                Assert.NotNull(activity);
                                AssertHeadersAreInjected(requestData, activity, parent is null);
                            }
                            else
                            {
                                AssertNoHeadersAreInjected(requestData);

                                if (!currentActivitySet)
                                {
                                    Assert.Null(activity);
                                }
                            }
                        });
                }

                Assert.Equal(activitySourceCreatesActivity.HasValue, madeASamplingDecision);
                Assert.Equal(diagnosticListenerActivityEnabled.HasValue, listenerCallbackWasCalled);
            }, UseVersion.ToString(), TestAsync.ToString(), parameters).DisposeAsync();
        }

        private sealed class SendMultipleTimesHandler : DelegatingHandler
        {
            private readonly Activity[] _parentActivities;

            public SendMultipleTimesHandler(HttpMessageHandler innerHandler, params Activity[] parentActivities) : base(innerHandler)
            {
                Assert.NotEmpty(parentActivities);
                _parentActivities = parentActivities;
            }

            protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
                => SendAsync(request, testAsync: false, cancellationToken).Result;

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => await SendAsync(request, testAsync: true, cancellationToken);

            private async ValueTask<HttpResponseMessage> SendAsync(HttpRequestMessage request, bool testAsync, CancellationToken cancellationToken)
            {
                HttpResponseMessage response = null;
                foreach (Activity parent in _parentActivities)
                {
                    parent.Start();
                    Assert.Equal(ActivityIdFormat.W3C, parent.IdFormat);
                    response = testAsync ? await base.SendAsync(request, cancellationToken) : base.Send(request, cancellationToken);
                    parent.Stop();
                    if (parent != _parentActivities.Last())
                    {
                        response.Dispose(); // only keep the last response
                    }
                }
                return response;
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        public async Task SendAsync_ReuseRequestInHandler_ResetsHeadersForEachReuse()
        {
            Activity parent0 = new Activity("parent0");
            Activity parent1 = new Activity("parent1") { TraceStateString = "wow=1" };
            Activity parent2 = new Activity("parent2") { TraceStateString = "wow=2" };

            const string FirstTraceParent = "00-F";
            const string FirstTraceState = "first";

            await GetFactoryForVersion(UseVersion).CreateServerAsync(async (server, uri) =>
            {
                SendMultipleTimesHandler handler = new SendMultipleTimesHandler(CreateSocketsHttpHandler(allowAllCertificates: true), parent0, parent1, parent2);
                using HttpClient client = new HttpClient(handler);
                HttpRequestMessage request = CreateRequest(HttpMethod.Get, uri, UseVersion, exactVersion: true);

                request.Headers.Add("traceparent", FirstTraceParent);
                request.Headers.Add("tracestate", FirstTraceState);

                Task clientTask = TestAsync ? client.SendAsync(request) : Task.Run(() => client.Send(request));

                HttpRequestData requestData = await server.AcceptConnectionSendResponseAndCloseAsync(statusCode: HttpStatusCode.InternalServerError);

                // On the first send DiagnosticsHandler should keep user-supplied headers.
                string traceparent = GetHeaderValue(requestData, "traceparent");
                string tracestate = GetHeaderValue(requestData, "tracestate");
                Assert.Equal(FirstTraceParent, traceparent);
                Assert.Equal(FirstTraceState, tracestate);

                requestData = await server.AcceptConnectionSendResponseAndCloseAsync(statusCode: HttpStatusCode.InternalServerError);

                // Headers should be overridden on each subsequent send.
                AssertHeadersAreInjected(requestData, parent1);
                requestData = await server.AcceptConnectionSendResponseAndCloseAsync(statusCode: HttpStatusCode.OK);
                AssertHeadersAreInjected(requestData, parent2);

                await clientTask;
            });
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        public async Task Http3_WaitForConnection_RecordedWhenWaitingForStream()
        {
            if (UseVersion != HttpVersion30 || !TestAsync)
            {
                throw new SkipTestException("This test is specific to async HTTP/3 runs.");
            }

            using Http3LoopbackServer server =  CreateHttp3LoopbackServer(new Http3Options() { MaxInboundBidirectionalStreams = 1 });

            TaskCompletionSource stream1Created = new();
            TaskCompletionSource allRequestsWaiting = new();

            Task serverTask = Task.Run(async () =>
            {
                await using Http3LoopbackConnection connection = (Http3LoopbackConnection)await server.EstablishGenericConnectionAsync();
                Http3LoopbackStream stream1 = await connection.AcceptRequestStreamAsync();
                stream1Created.SetResult();

                await allRequestsWaiting.Task;
                await stream1.HandleRequestAsync();
                await stream1.DisposeAsync();

                Http3LoopbackStream stream2 = await connection.AcceptRequestStreamAsync();
                await stream2.HandleRequestAsync();
                await stream2.DisposeAsync();

                Http3LoopbackStream stream3 = await connection.AcceptRequestStreamAsync();
                await stream3.HandleRequestAsync();
                await stream3.DisposeAsync();
            });

            Task clientTask = Task.Run(async () =>
            {
                using Activity parentActivity = new Activity("parent").Start();
                using ActivityRecorder requestRecorder = new("System.Net.Http", "System.Net.Http.HttpRequestOut")
                {
                    ExpectedParent = parentActivity
                };
                using ActivityRecorder waitForConnectionRecorder = new("Experimental.System.Net.Http.Connections", "Experimental.System.Net.Http.Connections.WaitForConnection")
                {
                    VerifyParent = false
                };
                waitForConnectionRecorder.OnStarted = a =>
                {
                    if (waitForConnectionRecorder.Started == 3)
                    {
                        allRequestsWaiting.SetResult();
                    }
                };

                SocketsHttpHandler handler = CreateSocketsHttpHandler(allowAllCertificates: true);
                using HttpClient client = new HttpClient(CreateSocketsHttpHandler(allowAllCertificates: true))
                {
                    DefaultRequestVersion = HttpVersion30,
                    DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact
                };

                Task<HttpResponseMessage> request1Task = client.GetAsync(server.Address);
                await stream1Created.Task;

                Task<HttpResponseMessage> request2Task = client.GetAsync(server.Address);
                Task<HttpResponseMessage> request3Task = client.GetAsync(server.Address);

                await new Task[] { request1Task, request2Task, request3Task }.WhenAllOrAnyFailed(30_000);
                Assert.Equal(3, waitForConnectionRecorder.Stopped);
            });

            await new Task[] { serverTask, clientTask }.WhenAllOrAnyFailed(30_000);
        }

        private static T GetProperty<T>(object obj, string propertyName)
        {
            Type t = obj.GetType();

            PropertyInfo p = t.GetRuntimeProperty(propertyName);

            object propertyValue = p.GetValue(obj);
            Assert.NotNull(propertyValue);
            Assert.IsAssignableFrom<T>(propertyValue);

            return (T)propertyValue;
        }

        private static string GetHeaderValue(HttpRequestData request, string name)
        {
            return request.Headers.SingleOrDefault(h => h.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).Value;
        }

        private static void AssertNoHeadersAreInjected(HttpRequestData request)
        {
            Assert.Null(GetHeaderValue(request, "Request-Id"));
            Assert.Null(GetHeaderValue(request, "traceparent"));
            Assert.Null(GetHeaderValue(request, "tracestate"));
            Assert.Null(GetHeaderValue(request, "Correlation-Context"));
        }

        private static void AssertHeadersAreInjected(HttpRequestData request, Activity parent, bool passthrough = false, bool isW3C = true)
        {
            string requestId = GetHeaderValue(request, "Request-Id");
            string traceparent = GetHeaderValue(request, "traceparent");
            string tracestate = GetHeaderValue(request, "tracestate");

            if (parent.IdFormat == ActivityIdFormat.Hierarchical && !isW3C)
            {
                Assert.True(requestId != null, "Request-Id was not injected when instrumentation was enabled");
                Assert.StartsWith(parent.Id, requestId);
                Assert.Equal(passthrough, parent.Id == requestId);
                Assert.Null(traceparent);
                Assert.Null(tracestate);

                List<NameValueHeaderValue> correlationContext = (GetHeaderValue(request, "Correlation-Context") ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(kvp => NameValueHeaderValue.Parse(kvp))
                    .ToList();

                List<KeyValuePair<string, string>> baggage = parent.Baggage.ToList();
                Assert.Equal(baggage.Count, correlationContext.Count);
                foreach (var kvp in baggage)
                {
                    Assert.Contains(new NameValueHeaderValue(kvp.Key, kvp.Value), correlationContext);
                }
            }
            else if (parent.IdFormat == ActivityIdFormat.W3C)
            {
                Assert.Null(requestId);
                Assert.True(traceparent != null, "traceparent was not injected when W3C instrumentation was enabled");
                Assert.StartsWith($"00-{parent.TraceId.ToHexString()}-", traceparent);
                Assert.Equal(passthrough, parent.Id == traceparent);
                Assert.Equal(parent.TraceStateString, tracestate);

                List<NameValueHeaderValue> correlationContext = (GetHeaderValue(request, "baggage") ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(kvp => NameValueHeaderValue.Parse(kvp))
                    .ToList();

                List<KeyValuePair<string, string>> baggage = parent.Baggage.ToList();
                Assert.Equal(baggage.Count, correlationContext.Count);
                foreach (var kvp in baggage)
                {
                    Assert.Contains(new NameValueHeaderValue(kvp.Key, kvp.Value), correlationContext);
                }
            }
        }

        private static async Task<(HttpRequestMessage, HttpResponseMessage)> GetAsync(string useVersion, string testAsync, Uri uri, CancellationToken cancellationToken = default, bool useSocketsHttpHandler = false)
        {
            HttpMessageHandler handler = useSocketsHttpHandler
                ? CreateSocketsHttpHandler(allowAllCertificates: true)
                : CreateHttpClientHandler(allowAllCertificates: true);

            using var client = new HttpClient(handler);
            var request = CreateRequest(HttpMethod.Get, uri, Version.Parse(useVersion), exactVersion: true);
            return (request, await client.SendAsync(bool.Parse(testAsync), request, cancellationToken));
        }
    }
}
