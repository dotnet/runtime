// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Net.Test.Common;
using System.Reflection;
using System.Threading;

using Xunit;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class DiagnosticsTest : RemoteExecutorTestBase
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
            RemoteInvoke(() =>
            {
                bool requestLogged = false;
                Guid requestGuid = Guid.Empty;
                bool responseLogged = false;
                Guid responseGuid = Guid.Empty;

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

                        responseLogged = true;
                    }
                });

                using (DiagnosticListener.AllListeners.Subscribe(diagnosticListenerObserver))
                {
                    diagnosticListenerObserver.Enable();
                    using (var client = new HttpClient())
                    {
                        var response = client.GetAsync(Configuration.Http.RemoteEchoServer).Result;
                    }

                    Assert.True(requestLogged, "Request was not logged.");
                    // Poll with a timeout since logging response is not synchronized with returning a response.
                    WaitForTrue(() => responseLogged, TimeSpan.FromSeconds(1), "Response was not logged within 1 second timeout.");
                    Assert.Equal(requestGuid, responseGuid);
                    diagnosticListenerObserver.Disable();
                }

                return SuccessExitCode;
            }).Dispose();
        }

        /// <remarks>
        /// This test must be in the same test collection as any others testing HttpClient/WinHttpHandler
        /// DiagnosticSources, since the global logging mechanism makes them conflict inherently.
        /// </remarks>
        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void SendAsync_ExpectedDiagnosticSourceNoLogging()
        {
            RemoteInvoke(() =>
            {
                bool requestLogged = false;
                bool responseLogged = false;

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
                });

                using (DiagnosticListener.AllListeners.Subscribe(diagnosticListenerObserver))
                {
                    using (var client = new HttpClient())
                    {
                        var response = client.GetAsync(Configuration.Http.RemoteEchoServer).Result;
                    }

                    Assert.False(requestLogged, "Request was logged while logging disabled.");
                    WaitForFalse(() => responseLogged, TimeSpan.FromSeconds(1), "Response was logged while logging disabled.");
                }
                return SuccessExitCode;
            }).Dispose();
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void SendAsync_HttpTracingEnabled_Succeeds()
        {
            RemoteInvoke(async () =>
            {
                using (var listener = new TestEventListener("Microsoft-System-Net-Http", EventLevel.Verbose))
                {
                    var events = new ConcurrentQueue<EventWrittenEventArgs>();
                    await listener.RunWithCallbackAsync(events.Enqueue, async () =>
                    {
                        // Exercise various code paths to get coverage of tracing
                        using (var client = new HttpClient())
                        {
                            // Do a get to a loopback server
                            await LoopbackServer.CreateServerAsync(async (server, url) =>
                            {
                                await TestHelper.WhenAllCompletedOrAnyFailed(
                                    LoopbackServer.ReadRequestAndSendResponseAsync(server),
                                    client.GetAsync(url));
                            });

                            // Do a post to a remote server
                            byte[] expectedData = Enumerable.Range(0, 20000).Select(i => (byte)i).ToArray();
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
            }).Dispose();
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
    }
}
