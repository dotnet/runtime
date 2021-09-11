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
    public abstract class DiagnosticsTest : HttpClientHandlerTestBase
    {
        private const string EnableActivityPropagationEnvironmentVariableSettingName = "DOTNET_SYSTEM_NET_HTTP_ENABLEACTIVITYPROPAGATION";
        private const string EnableActivityPropagationAppCtxSettingName = "System.Net.Http.EnableActivityPropagation";

        private static bool EnableActivityPropagationEnvironmentVariableIsNotSetAndRemoteExecutorSupported =>
            string.IsNullOrEmpty(Environment.GetEnvironmentVariable(EnableActivityPropagationEnvironmentVariableSettingName)) && RemoteExecutor.IsSupported;

        private static readonly Uri InvalidUri = new("http://nosuchhost.invalid");

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
        public void SendAsync_ExpectedDiagnosticSourceLogging()
        {
            RemoteExecutor.Invoke(async (useVersion, testAsync) =>
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
            }, UseVersion.ToString(), TestAsync.ToString()).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SendAsync_ExpectedDiagnosticSourceNoLogging()
        {
            RemoteExecutor.Invoke(async (useVersion, testAsync) =>
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
            }, UseVersion.ToString(), TestAsync.ToString()).Dispose();
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public void SendAsync_HttpTracingEnabled_Succeeds(bool useSsl)
        {
            if (useSsl && UseVersion == HttpVersion.Version20 && !PlatformDetection.SupportsAlpn)
            {
                return;
            }

            RemoteExecutor.Invoke(async (useVersion, useSsl, testAsync) =>
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
            }, UseVersion.ToString(), useSsl.ToString(), TestAsync.ToString()).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SendAsync_ExpectedDiagnosticExceptionLogging()
        {
            RemoteExecutor.Invoke(async (useVersion, testAsync) =>
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
            }, UseVersion.ToString(), TestAsync.ToString()).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SendAsync_ExpectedDiagnosticCancelledLogging()
        {
            RemoteExecutor.Invoke(async (useVersion, testAsync) =>
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
            }, UseVersion.ToString(), TestAsync.ToString()).Dispose();
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(ActivityIdFormat.Hierarchical)]
        [InlineData(ActivityIdFormat.W3C)]
        public void SendAsync_ExpectedDiagnosticSourceActivityLogging(ActivityIdFormat idFormat)
        {
            RemoteExecutor.Invoke(async (useVersion, testAsync, idFormatString) =>
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
                parentActivity.TraceStateString = "Foo";

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
            }, UseVersion.ToString(), TestAsync.ToString(), idFormat.ToString()).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SendAsync_ExpectedDiagnosticSourceActivityLogging_InvalidBaggage()
        {
            RemoteExecutor.Invoke(async (useVersion, testAsync) =>
            {
                bool exceptionLogged = false;

                TaskCompletionSource activityStopTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

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
            }, UseVersion.ToString(), TestAsync.ToString()).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SendAsync_ExpectedDiagnosticSourceActivityLoggingDoesNotOverwriteHeader()
        {
            RemoteExecutor.Invoke(async (useVersion, testAsync) =>
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
            }, UseVersion.ToString(), TestAsync.ToString()).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SendAsync_ExpectedDiagnosticSourceActivityLoggingDoesNotOverwriteW3CTraceParentHeader()
        {
            RemoteExecutor.Invoke(async (useVersion, testAsync) =>
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
            }, UseVersion.ToString(), TestAsync.ToString()).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SendAsync_ExpectedDiagnosticSourceUrlFilteredActivityLogging()
        {
            RemoteExecutor.Invoke(async (useVersion, testAsync) =>
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
            }, UseVersion.ToString(), TestAsync.ToString()).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SendAsync_ExpectedDiagnosticExceptionActivityLogging()
        {
            RemoteExecutor.Invoke(async (useVersion, testAsync) =>
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
            }, UseVersion.ToString(), TestAsync.ToString()).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SendAsync_ExpectedDiagnosticSynchronousExceptionActivityLogging()
        {
            RemoteExecutor.Invoke(async (useVersion, testAsync) =>
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

                    using (HttpClientHandler handler = CreateHttpClientHandler(useVersion))
                    using (HttpClient client = CreateHttpClient(handler, useVersion))
                    {
                        // Set a https proxy.
                        // Forces a synchronous exception for SocketsHttpHandler.
                        // SocketsHttpHandler only allow http scheme for proxies.
                        handler.Proxy = new WebProxy($"https://foo.bar", false);
                        var request = new HttpRequestMessage(HttpMethod.Get, InvalidUri)
                        {
                            Version = Version.Parse(useVersion)
                        };

                        // We cannot use Assert.Throws<Exception>(() => { SendAsync(...); }) to verify the
                        // synchronous exception here, because DiagnosticsHandler SendAsync() method has async
                        // modifier, and returns Task. If the call is not awaited, the current test method will continue
                        // run before the call is completed, thus Assert.Throws() will not capture the exception.
                        // We need to wait for the Task to complete synchronously, to validate the exception.

                        Exception exception = null;
                        if (bool.Parse(testAsync))
                        {
                            Task sendTask = client.SendAsync(request);
                            Assert.True(sendTask.IsFaulted);
                            exception = sendTask.Exception.InnerException;
                        }
                        else
                        {
                            try
                            {
                                client.Send(request);
                            }
                            catch (Exception ex)
                            {
                                exception = ex;
                            }
                            Assert.NotNull(exception);
                        }

                        await activityStopTcs.Task;

                        Assert.IsType<NotSupportedException>(exception);
                        Assert.Same(exceptionLogged, exception);
                    }
                }
            }, UseVersion.ToString(), TestAsync.ToString()).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SendAsync_ExpectedDiagnosticSourceNewAndDeprecatedEventsLogging()
        {
            RemoteExecutor.Invoke(async (useVersion, testAsync) =>
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
            }, UseVersion.ToString(), TestAsync.ToString()).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SendAsync_ExpectedDiagnosticExceptionOnlyActivityLogging()
        {
            RemoteExecutor.Invoke(async (useVersion, testAsync) =>
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
            }, UseVersion.ToString(), TestAsync.ToString()).Dispose();
        }

        public static IEnumerable<object[]> UseSocketsHttpHandler_WithIdFormat_MemberData()
        {
            yield return new object[] { true, ActivityIdFormat.Hierarchical };
            yield return new object[] { true, ActivityIdFormat.W3C };
            yield return new object[] { false, ActivityIdFormat.Hierarchical };
            yield return new object[] { false, ActivityIdFormat.W3C };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [MemberData(nameof(UseSocketsHttpHandler_WithIdFormat_MemberData))]
        public async Task SendAsync_ExpectedActivityPropagationWithoutListener(bool useSocketsHttpHandler, ActivityIdFormat idFormat)
        {
            Activity parent = new Activity("parent");
            parent.SetIdFormat(idFormat);
            parent.Start();

            await GetFactoryForVersion(UseVersion).CreateClientAndServerAsync(
                async uri =>
                {
                    await GetAsync(UseVersion.ToString(), TestAsync.ToString(), uri, useSocketsHttpHandler: useSocketsHttpHandler);
                },
                async server =>
                {
                    HttpRequestData requestData = await server.HandleRequestAsync();
                    AssertHeadersAreInjected(requestData, parent);
                });
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [InlineData(true)]
        [InlineData(false)]
        public async Task SendAsync_ExpectedActivityPropagationWithoutListenerOrParentActivity(bool useSocketsHttpHandler)
        {
            await GetFactoryForVersion(UseVersion).CreateClientAndServerAsync(
                async uri =>
                {
                    await GetAsync(UseVersion.ToString(), TestAsync.ToString(), uri, useSocketsHttpHandler: useSocketsHttpHandler);
                },
                async server =>
                {
                    HttpRequestData requestData = await server.HandleRequestAsync();
                    AssertNoHeadersAreInjected(requestData);
                });
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
        public void SendAsync_SuppressedGlobalStaticPropagationEnvVar(string envVarValue)
        {
            RemoteExecutor.Invoke(async (useVersion, testAsync, envVarValue) =>
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
            }, UseVersion.ToString(), TestAsync.ToString(), envVarValue).Dispose();
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [MemberData(nameof(UseSocketsHttpHandler_WithIdFormat_MemberData))]
        public async Task SendAsync_HeadersAreInjectedOnRedirects(bool useSocketsHttpHandler, ActivityIdFormat idFormat)
        {
            Activity parent = new Activity("parent");
            parent.SetIdFormat(idFormat);
            parent.TraceStateString = "Foo";
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
                        Assert.NotEqual(GetHeaderValue(firstRequestData, "Request-Id"), GetHeaderValue(secondRequestData, "Request-Id"));
                    }
                });
            });
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
        [InlineData(false)]
        public void SendAsync_SuppressedGlobalStaticPropagationNoListenerAppCtx(bool switchValue)
        {
            RemoteExecutor.Invoke(async (useVersion, testAsync, switchValue) =>
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
            }, UseVersion.ToString(), TestAsync.ToString(), switchValue.ToString()).Dispose();
        }

        public static IEnumerable<object[]> SocketsHttpHandlerPropagators_WithIdFormat_MemberData()
        {
            foreach (var propagator in new[] { null, DistributedContextPropagator.CreateDefaultPropagator(), DistributedContextPropagator.CreateNoOutputPropagator(), DistributedContextPropagator.CreatePassThroughPropagator() })
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
                    using var handler = new SocketsHttpHandler { ActivityHeadersPropagator = propagator };
                    handler.SslOptions.RemoteCertificateValidationCallback = delegate { return true; };
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
                        AssertHeadersAreInjected(requestData, parent, ReferenceEquals(propagator, DistributedContextPropagator.CreatePassThroughPropagator()));
                    }
                });
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

        private static void AssertHeadersAreInjected(HttpRequestData request, Activity parent, bool passthrough = false)
        {
            string requestId = GetHeaderValue(request, "Request-Id");
            string traceparent = GetHeaderValue(request, "traceparent");
            string tracestate = GetHeaderValue(request, "tracestate");

            if (parent.IdFormat == ActivityIdFormat.Hierarchical)
            {
                Assert.True(requestId != null, "Request-Id was not injected when instrumentation was enabled");
                Assert.StartsWith(parent.Id, requestId);
                Assert.Equal(passthrough, parent.Id == requestId);
                Assert.Null(traceparent);
                Assert.Null(tracestate);
            }
            else if (parent.IdFormat == ActivityIdFormat.W3C)
            {
                Assert.Null(requestId);
                Assert.True(traceparent != null, "traceparent was not injected when W3C instrumentation was enabled");
                Assert.StartsWith($"00-{parent.TraceId.ToHexString()}-", traceparent);
                Assert.Equal(passthrough, parent.Id == traceparent);
                Assert.Equal(parent.TraceStateString, tracestate);
            }

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

        private static async Task<(HttpRequestMessage, HttpResponseMessage)> GetAsync(string useVersion, string testAsync, Uri uri, CancellationToken cancellationToken = default, bool useSocketsHttpHandler = false)
        {
            HttpMessageHandler handler;
            if (useSocketsHttpHandler)
            {
                var socketsHttpHandler = new SocketsHttpHandler();
                socketsHttpHandler.SslOptions.RemoteCertificateValidationCallback = delegate { return true; };
                handler = socketsHttpHandler;
            }
            else
            {
                handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates
                };
            }

            using var client = new HttpClient(handler);
            var request = CreateRequest(HttpMethod.Get, uri, Version.Parse(useVersion), exactVersion: true);
            return (request, await client.SendAsync(bool.Parse(testAsync), request, cancellationToken));
        }
    }
}
