// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
using Xunit;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    [ActiveIssue(20470, TargetFrameworkMonikers.UapAot)]
    [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "NetEventSource is only part of .NET Core.")]
    public class DiagnosticsTest : HttpClientTestBase
    {
        [Fact]
        public static void EventSource_ExistsWithCorrectId()
        {
            Type esType = typeof(HttpClient).GetTypeInfo().Assembly.GetType("System.Net.NetEventSource", throwOnError: true, ignoreCase: false);
            Assert.NotNull(esType);

            Assert.Equal("Microsoft-System-Net-Http", EventSource.GetName(esType));
            Assert.Equal(Guid.Parse("bdd9a83e-1929-5482-0d73-2fe5e1c0e16d"), EventSource.GetGuid(esType));

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
        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void SendAsync_ExpectedDiagnosticSourceLogging()
        {
            RemoteInvoke(useManagedHandlerString =>
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
                        var requestStatus = GetPropertyValueFromAnonymousTypeInstance<TaskStatus>(kvp.Value, "RequestTaskStatus");
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
                    diagnosticListenerObserver.Enable( s => !s.Contains("HttpRequestOut"));
                    using (HttpClient client = CreateHttpClient(useManagedHandlerString))
                    {
                        client.GetAsync(Configuration.Http.RemoteEchoServer).Result.Dispose();
                    }

                    Assert.True(requestLogged, "Request was not logged.");
                    // Poll with a timeout since logging response is not synchronized with returning a response.
                    WaitForTrue(() => responseLogged, TimeSpan.FromSeconds(1), "Response was not logged within 1 second timeout.");
                    Assert.Equal(requestGuid, responseGuid);
                    Assert.False(exceptionLogged, "Exception was logged for successful request");
                    Assert.False(activityLogged, "HttpOutReq was logged while HttpOutReq logging was disabled");
                    diagnosticListenerObserver.Disable();
                }

                return SuccessExitCode;
            }, UseManagedHandler.ToString()).Dispose();
        }

        /// <remarks>
        /// This test must be in the same test collection as any others testing HttpClient/WinHttpHandler
        /// DiagnosticSources, since the global logging mechanism makes them conflict inherently.
        /// </remarks>
        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void SendAsync_ExpectedDiagnosticSourceNoLogging()
        {
            RemoteInvoke(useManagedHandlerString =>
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
                    using (HttpClient client = CreateHttpClient(useManagedHandlerString))
                    {
                        LoopbackServer.CreateServerAsync(async (server, url) =>
                        {
                            Task<List<string>> requestLines = LoopbackServer.AcceptSocketAsync(server,
                                    (s, stream, reader, writer) => LoopbackServer.ReadWriteAcceptedAsync(s, reader, writer));
                            Task<HttpResponseMessage> response = client.GetAsync(url);
                            await Task.WhenAll(response, requestLines);

                            AssertNoHeadersAreInjected(requestLines.Result);
                            response.Result.Dispose();
                        }).Wait();
                    }

                    Assert.False(requestLogged, "Request was logged while logging disabled.");
                    Assert.False(activityStartLogged, "HttpRequestOut.Start was logged while logging disabled.");
                    WaitForFalse(() => responseLogged, TimeSpan.FromSeconds(1), "Response was logged while logging disabled.");
                    Assert.False(activityStopLogged, "HttpRequestOut.Stop was logged while logging disabled.");
                }
                return SuccessExitCode;
            }, UseManagedHandler.ToString()).Dispose();
        }

        [ActiveIssue(23771, TestPlatforms.AnyUnix)]
        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void SendAsync_HttpTracingEnabled_Succeeds()
        {
            RemoteInvoke(async useManagedHandlerString =>
            {
                using (var listener = new TestEventListener("Microsoft-System-Net-Http", EventLevel.Verbose))
                {
                    var events = new ConcurrentQueue<EventWrittenEventArgs>();
                    await listener.RunWithCallbackAsync(events.Enqueue, async () =>
                    {
                        // Exercise various code paths to get coverage of tracing
                        using (HttpClient client = CreateHttpClient(useManagedHandlerString))
                        {
                            // Do a get to a loopback server
                            await LoopbackServer.CreateServerAsync(async (server, url) =>
                            {
                                await TestHelper.WhenAllCompletedOrAnyFailed(
                                    LoopbackServer.ReadRequestAndSendResponseAsync(server),
                                    client.GetAsync(url));
                            });

                            // Do a post to a remote server
                            byte[] expectedData = Enumerable.Range(0, 20000).Select(i => unchecked((byte)i)).ToArray();
                            HttpContent content = new ByteArrayContent(expectedData);
                            content.Headers.ContentMD5 = TestHelper.ComputeMD5Hash(expectedData);
                            using (HttpResponseMessage response = await client.PostAsync(Configuration.Http.RemoteEchoServer, content))
                            {
                                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                            }
                        }
                    });

                    // We don't validate receiving specific events, but rather that we do at least
                    // receive some events, and that enabling tracing doesn't cause other failures
                    // in processing.
                    Assert.DoesNotContain(events, ev => ev.EventId == 0); // make sure there are no event source error messages
                    Assert.InRange(events.Count, 1, int.MaxValue);
                }

                return SuccessExitCode;
            }, UseManagedHandler.ToString()).Dispose();
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void SendAsync_ExpectedDiagnosticExceptionLogging()
        {
            RemoteInvoke(useManagedHandlerString =>
            {
                bool exceptionLogged = false;
                bool responseLogged = false;
                var diagnosticListenerObserver = new FakeDiagnosticListenerObserver(kvp =>
                {
                    if (kvp.Key.Equals("System.Net.Http.Response"))
                    {
                        Assert.NotNull(kvp.Value);
                        var requestStatus = GetPropertyValueFromAnonymousTypeInstance<TaskStatus>(kvp.Value, "RequestTaskStatus");
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
                    using (HttpClient client = CreateHttpClient(useManagedHandlerString))
                    {
                        Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync($"http://{Guid.NewGuid()}.com")).Wait();
                    }
                    // Poll with a timeout since logging response is not synchronized with returning a response.
                    WaitForTrue(() => responseLogged, TimeSpan.FromSeconds(1),
                        "Response with exception was not logged within 1 second timeout.");
                    Assert.True(exceptionLogged, "Exception was not logged");
                    diagnosticListenerObserver.Disable();
                }

                return SuccessExitCode;
            }, UseManagedHandler.ToString()).Dispose();
        }

        [ActiveIssue(23209)]
        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void SendAsync_ExpectedDiagnosticCancelledLogging()
        {
            RemoteInvoke(useManagedHandlerString =>
            {
                bool cancelLogged = false;
                var diagnosticListenerObserver = new FakeDiagnosticListenerObserver(kvp =>
                {
                    if (kvp.Key.Equals("System.Net.Http.Response"))
                    {
                        Assert.NotNull(kvp.Value);
                        var status = GetPropertyValueFromAnonymousTypeInstance<TaskStatus>(kvp.Value, "RequestTaskStatus");
                        Assert.Equal(TaskStatus.Canceled, status);
                        Volatile.Write(ref cancelLogged, true);
                    }
                });

                using (DiagnosticListener.AllListeners.Subscribe(diagnosticListenerObserver))
                {
                    diagnosticListenerObserver.Enable(s => !s.Contains("HttpRequestOut"));
                    using (HttpClient client = CreateHttpClient(useManagedHandlerString))
                    {
                        LoopbackServer.CreateServerAsync(async (server, url) =>
                        {
                            CancellationTokenSource tcs = new CancellationTokenSource();
                            Task request = LoopbackServer.AcceptSocketAsync(server,
                                (s, stream, reader, writer) =>
                                {
                                    tcs.Cancel();
                                    return LoopbackServer.ReadWriteAcceptedAsync(s, reader, writer);
                                });
                            Task response = client.GetAsync(url, tcs.Token);
                            await Assert.ThrowsAnyAsync<Exception>(() => TestHelper.WhenAllCompletedOrAnyFailed(response, request));
                        }).Wait();
                    }
                }
                // Poll with a timeout since logging response is not synchronized with returning a response.
                WaitForTrue(() => Volatile.Read(ref cancelLogged), TimeSpan.FromSeconds(1),
                    "Cancellation was not logged within 1 second timeout.");
                diagnosticListenerObserver.Disable();

                return SuccessExitCode;
            }, UseManagedHandler.ToString()).Dispose();
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void SendAsync_ExpectedDiagnosticSourceActivityLogging()
        {
            RemoteInvoke(useManagedHandlerString =>
            {
                bool requestLogged = false;
                bool responseLogged = false;
                bool activityStartLogged = false;
                bool activityStopLogged = false;
                bool exceptionLogged = false;

                Activity parentActivity = new Activity("parent");
                parentActivity.AddBaggage("correlationId", Guid.NewGuid().ToString());
                parentActivity.AddBaggage("moreBaggage", Guid.NewGuid().ToString());
                parentActivity.AddTag("tag", "tag"); //add tag to ensure it is not injected into request
                parentActivity.Start();

                var diagnosticListenerObserver = new FakeDiagnosticListenerObserver(kvp =>
                {
                    if (kvp.Key.Equals("System.Net.Http.Request")) { requestLogged = true; }
                    else if (kvp.Key.Equals("System.Net.Http.Response")) { responseLogged = true;}
                    else if (kvp.Key.Equals("System.Net.Http.Exception")) { exceptionLogged = true; }
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
                        var requestStatus = GetPropertyValueFromAnonymousTypeInstance<TaskStatus>(kvp.Value, "RequestTaskStatus");
                        Assert.Equal(TaskStatus.RanToCompletion, requestStatus);

                        activityStopLogged = true;
                    }
                });

                using (DiagnosticListener.AllListeners.Subscribe(diagnosticListenerObserver))
                {
                    diagnosticListenerObserver.Enable(s => s.Contains("HttpRequestOut"));
                    using (HttpClient client = CreateHttpClient(useManagedHandlerString))
                    {
                        LoopbackServer.CreateServerAsync(async (server, url) =>
                        {
                            Task<List<string>> requestLines = LoopbackServer.AcceptSocketAsync(server,
                                (s, stream, reader, writer) => LoopbackServer.ReadWriteAcceptedAsync(s, reader, writer));
                            Task<HttpResponseMessage> response = client.GetAsync(url);
                            await Task.WhenAll(response, requestLines);

                            AssertHeadersAreInjected(requestLines.Result, parentActivity);
                            response.Result.Dispose();
                        }).Wait();
                    }

                    Assert.True(activityStartLogged, "HttpRequestOut.Start was not logged.");
                    Assert.False(requestLogged, "Request was logged when Activity logging was enabled.");
                    // Poll with a timeout since logging response is not synchronized with returning a response.
                    WaitForTrue(() => activityStopLogged, TimeSpan.FromSeconds(1), "HttpRequestOut.Stop was not logged within 1 second timeout.");
                    Assert.False(exceptionLogged, "Exception was logged for successful request");
                    Assert.False(responseLogged, "Response was logged when Activity logging was enabled.");
                    diagnosticListenerObserver.Disable();
                }

                return SuccessExitCode;
            }, UseManagedHandler.ToString()).Dispose();
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void SendAsync_ExpectedDiagnosticSourceUrlFilteredActivityLogging()
        {
            RemoteInvoke(useManagedHandlerString =>
            {
                bool activityStartLogged = false;
                bool activityStopLogged = false;

                var diagnosticListenerObserver = new FakeDiagnosticListenerObserver(kvp =>
                {
                    if (kvp.Key.Equals("System.Net.Http.HttpRequestOut.Start")){activityStartLogged = true;}
                    else if (kvp.Key.Equals("System.Net.Http.HttpRequestOut.Stop")) {activityStopLogged = true;}
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
                    using (HttpClient client = CreateHttpClient(useManagedHandlerString))
                    {
                        client.GetAsync(Configuration.Http.RemoteEchoServer).Result.Dispose();
                    }
                    Assert.False(activityStartLogged, "HttpRequestOut.Start was logged while URL disabled.");
                    // Poll with a timeout since logging response is not synchronized with returning a response.
                    Assert.False(activityStopLogged, "HttpRequestOut.Stop was logged while URL disabled.");
                    diagnosticListenerObserver.Disable();
                }

                return SuccessExitCode;
            }, UseManagedHandler.ToString()).Dispose();
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void SendAsync_ExpectedDiagnosticExceptionActivityLogging()
        {
            RemoteInvoke(useManagedHandlerString =>
            {
                bool exceptionLogged = false;
                bool activityStopLogged = false;
                var diagnosticListenerObserver = new FakeDiagnosticListenerObserver(kvp =>
                {
                    if (kvp.Key.Equals("System.Net.Http.HttpRequestOut.Stop"))
                    {
                        Assert.NotNull(kvp.Value);
                        GetPropertyValueFromAnonymousTypeInstance<HttpRequestMessage>(kvp.Value, "Request");
                        var requestStatus = GetPropertyValueFromAnonymousTypeInstance<TaskStatus>(kvp.Value, "RequestTaskStatus");
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
                    using (HttpClient client = CreateHttpClient(useManagedHandlerString))
                    {
                        Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync($"http://{Guid.NewGuid()}.com")).Wait();
                    }
                    // Poll with a timeout since logging response is not synchronized with returning a response.
                    WaitForTrue(() => activityStopLogged, TimeSpan.FromSeconds(1),
                        "Response with exception was not logged within 1 second timeout.");
                    Assert.True(exceptionLogged, "Exception was not logged");
                    diagnosticListenerObserver.Disable();
                }

                return SuccessExitCode;
            }, UseManagedHandler.ToString()).Dispose();
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void SendAsync_ExpectedDiagnosticSourceNewAndDeprecatedEventsLogging()
        {
            RemoteInvoke(useManagedHandlerString =>
            {
                bool requestLogged = false;
                bool responseLogged = false;
                bool activityStartLogged = false;
                bool activityStopLogged = false;

                var diagnosticListenerObserver = new FakeDiagnosticListenerObserver(kvp =>
                {
                    if (kvp.Key.Equals("System.Net.Http.Request")) { requestLogged = true; }
                    else if (kvp.Key.Equals("System.Net.Http.Response")) { responseLogged = true; }
                    else if (kvp.Key.Equals("System.Net.Http.HttpRequestOut.Start")) { activityStartLogged = true;}
                    else if (kvp.Key.Equals("System.Net.Http.HttpRequestOut.Stop")) { activityStopLogged = true; }
                });

                using (DiagnosticListener.AllListeners.Subscribe(diagnosticListenerObserver))
                {
                    diagnosticListenerObserver.Enable();
                    using (HttpClient client = CreateHttpClient(useManagedHandlerString))
                    {
                        client.GetAsync(Configuration.Http.RemoteEchoServer).Result.Dispose();
                    }

                    Assert.True(activityStartLogged, "HttpRequestOut.Start was not logged.");
                    Assert.True(requestLogged, "Request was not logged.");
                    // Poll with a timeout since logging response is not synchronized with returning a response.
                    WaitForTrue(() => activityStopLogged, TimeSpan.FromSeconds(1), "HttpRequestOut.Stop was not logged within 1 second timeout.");
                    Assert.True(responseLogged, "Response was not logged.");
                    diagnosticListenerObserver.Disable();
                }

                return SuccessExitCode;
            }, UseManagedHandler.ToString()).Dispose();
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void SendAsync_ExpectedDiagnosticExceptionOnlyActivityLogging()
        {
            RemoteInvoke(useManagedHandlerString =>
            {
                bool exceptionLogged = false;
                bool activityLogged = false;
                var diagnosticListenerObserver = new FakeDiagnosticListenerObserver(kvp =>
                {
                    if (kvp.Key.Equals("System.Net.Http.HttpRequestOut.Stop")) { activityLogged = true; }
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
                    using (HttpClient client = CreateHttpClient(useManagedHandlerString))
                    {
                        Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync($"http://{Guid.NewGuid()}.com")).Wait();
                    }
                    // Poll with a timeout since logging response is not synchronized with returning a response.
                    WaitForTrue(() => exceptionLogged, TimeSpan.FromSeconds(1),
                        "Exception was not logged within 1 second timeout.");
                    Assert.False(activityLogged, "HttpOutReq was logged when logging was disabled");
                    diagnosticListenerObserver.Disable();
                }

                return SuccessExitCode;
            }, UseManagedHandler.ToString()).Dispose();
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void SendAsync_ExpectedDiagnosticStopOnlyActivityLogging()
        {
            RemoteInvoke(useManagedHandlerString =>
            {
                bool activityStartLogged = false;
                bool activityStopLogged = false;

                var diagnosticListenerObserver = new FakeDiagnosticListenerObserver(kvp =>
                {
                    if (kvp.Key.Equals("System.Net.Http.HttpRequestOut.Start")) { activityStartLogged = true; }
                    else if (kvp.Key.Equals("System.Net.Http.HttpRequestOut.Stop"))
                    {
                        Assert.NotNull(Activity.Current);
                        activityStopLogged = true;
                    }
                });

                using (DiagnosticListener.AllListeners.Subscribe(diagnosticListenerObserver))
                {
                    diagnosticListenerObserver.Enable(s => s.Equals("System.Net.Http.HttpRequestOut"));
                    using (HttpClient client = CreateHttpClient(useManagedHandlerString))
                    {
                        client.GetAsync(Configuration.Http.RemoteEchoServer).Result.Dispose();
                    }
                    // Poll with a timeout since logging response is not synchronized with returning a response.
                    WaitForTrue(() => activityStopLogged, TimeSpan.FromSeconds(1),
                        "HttpRequestOut.Stop was not logged within 1 second timeout.");
                    Assert.False(activityStartLogged, "HttpRequestOut.Start was logged when start logging was disabled");
                    diagnosticListenerObserver.Disable();
                }

                return SuccessExitCode;
            }, UseManagedHandler.ToString()).Dispose();
        }

        [ActiveIssue(23209)]
        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void SendAsync_ExpectedDiagnosticCancelledActivityLogging()
        {
            RemoteInvoke(useManagedHandlerString =>
            {
                bool cancelLogged = false;
                var diagnosticListenerObserver = new FakeDiagnosticListenerObserver(kvp =>
                {
                    if (kvp.Key == "System.Net.Http.HttpRequestOut.Stop")
                    {
                        Assert.NotNull(kvp.Value);
                        GetPropertyValueFromAnonymousTypeInstance<HttpRequestMessage>(kvp.Value, "Request");
                        var status = GetPropertyValueFromAnonymousTypeInstance<TaskStatus>(kvp.Value, "RequestTaskStatus");
                        Assert.Equal(TaskStatus.Canceled, status);
                        Volatile.Write(ref cancelLogged, true);
                    }
                });

                using (DiagnosticListener.AllListeners.Subscribe(diagnosticListenerObserver))
                {
                    diagnosticListenerObserver.Enable();
                    using (HttpClient client = CreateHttpClient(useManagedHandlerString))
                    {
                        LoopbackServer.CreateServerAsync(async (server, url) =>
                        {
                            CancellationTokenSource tcs = new CancellationTokenSource();
                            Task request = LoopbackServer.AcceptSocketAsync(server,
                                (s, stream, reader, writer) =>
                                {
                                    tcs.Cancel();
                                    return LoopbackServer.ReadWriteAcceptedAsync(s, reader, writer);
                                });
                            Task response = client.GetAsync(url, tcs.Token);
                            await Assert.ThrowsAnyAsync<Exception>(() => TestHelper.WhenAllCompletedOrAnyFailed(response, request));
                        }).Wait();
                    }
                }
                // Poll with a timeout since logging response is not synchronized with returning a response.
                WaitForTrue(() => Volatile.Read(ref cancelLogged), TimeSpan.FromSeconds(1),
                    "Cancellation was not logged within 1 second timeout.");
                diagnosticListenerObserver.Disable();

                return SuccessExitCode;
            }, UseManagedHandler.ToString()).Dispose();
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

        private void AssertHeadersAreInjected(List<string> requestLines, Activity parent)
        {
            string requestId = null;
            var correlationContext = new List<NameValueHeaderValue>();

            foreach (var line in requestLines)
            {
                if (line.StartsWith("Request-Id"))
                {
                    requestId = line.Substring("Request-Id".Length).Trim(' ', ':');
                }
                if (line.StartsWith("Correlation-Context"))
                {
                    var corrCtxString = line.Substring("Correlation-Context".Length).Trim(' ', ':');
                    foreach (var kvp in corrCtxString.Split(','))
                    {
                        correlationContext.Add(NameValueHeaderValue.Parse(kvp));
                    }
                }
            }
            Assert.True(requestId != null, "Request-Id was not injected when instrumentation was enabled");
            Assert.True(requestId.StartsWith(parent.Id));
            Assert.NotEqual(parent.Id, requestId);

            List<KeyValuePair<string, string>> baggage = parent.Baggage.ToList();
            Assert.Equal(baggage.Count, correlationContext.Count);
            foreach (var kvp in baggage)
            {
                Assert.Contains(new NameValueHeaderValue(kvp.Key, kvp.Value), correlationContext);
            }
        }

        private void AssertNoHeadersAreInjected(List<string> requestLines)
        {
            foreach (var line in requestLines)
            {
                Assert.False(line.StartsWith("Request-Id"),
                    "Request-Id header was injected when instrumentation was disabled");
                Assert.False(line.StartsWith("Correlation-Context"),
                    "Correlation-Context header was injected when instrumentation was disabled");
            }
        }
    }
}
