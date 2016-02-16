// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Test.Common;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public class HttpClientTest
    {
        [Fact]
        [OuterLoop]
        public void Timeout_SetTo60AndGetResponseFromServerWhichTakes40_Success()
        {
            // TODO: This server path will change once the final test infrastructure is in place (Issue #1477).
            const string SlowServer = "http://httpbin.org/drip?numbytes=1&duration=1&delay=40&code=200";
            
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(60);
                var response = client.GetAsync(SlowServer).GetAwaiter().GetResult();
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        /// <remarks>
        /// This test must be in the same test collection as any others testing HttpClient/WinHttpHandler
        /// DiagnosticSources, since the global logging mechanism makes them conflict inherently.
        /// </remarks>
        [Fact]
        public void SendAsync_ExpectedDiagnosticSourceLogging()
        {
            bool requestLogged = false;
            Guid requestGuid = Guid.Empty;
            bool responseLogged = false;
            Guid responseGuid = Guid.Empty;

            var diagnosticListenerObserver = new FakeDiagnosticListenerObserver(
                kvp =>
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
                    var response = client.GetAsync(HttpTestServers.RemoteEchoServer).Result;
                }

                Assert.True(requestLogged, "Request was not logged.");
                // Poll with a timeout since logging response is not synchronized with returning a response.
                WaitForTrue(() => responseLogged, TimeSpan.FromSeconds(1), "Response was not logged within 1 second timeout.");
                Assert.Equal(requestGuid, responseGuid);
                diagnosticListenerObserver.Disable();
            }
        }

        /// <remarks>
        /// This test must be in the same test collection as any others testing HttpClient/WinHttpHandler
        /// DiagnosticSources, since the global logging mechanism makes them conflict inherently.
        /// </remarks>
        [Fact]
        public void SendAsync_ExpectedDiagnosticSourceNoLogging()
        {
            bool requestLogged = false;
            bool responseLogged = false;

            var diagnosticListenerObserver = new FakeDiagnosticListenerObserver(
                kvp =>
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
                    var response = client.GetAsync(HttpTestServers.RemoteEchoServer).Result;
                }

                Assert.False(requestLogged, "Request was logged while logging disabled.");
                // TODO: Waiting for one second is not ideal, but how else be reasonably sure that
                // some logging message hasn't slipped through?
                WaitForFalse(() => responseLogged, TimeSpan.FromSeconds(1), "Response was logged while logging disabled.");
            }
        }

        private void WaitForTrue(Func<bool> p, TimeSpan timeout, string message)
        {
            // Assert that spin doesn't time out.
            Assert.True(System.Threading.SpinWait.SpinUntil(p, timeout), message);
        }

        private void WaitForFalse(Func<bool> p, TimeSpan timeout, string message)
        {
            // Assert that spin times out.
            Assert.False(System.Threading.SpinWait.SpinUntil(p, timeout), message);
        }

        private T GetPropertyValueFromAnonymousTypeInstance<T>(object obj, string propertyName)
        {
            Type t = obj.GetType();

            PropertyInfo p = t.GetRuntimeProperty(propertyName);

            object propertyValue = p.GetValue(obj);
            Assert.NotNull(propertyValue);
            Assert.IsAssignableFrom<T>(propertyValue);

            return (T)propertyValue;
        }
    }
}
