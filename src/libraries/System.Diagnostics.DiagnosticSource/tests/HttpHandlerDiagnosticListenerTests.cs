// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Diagnostics.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class HttpHandlerDiagnosticListenerTests : RemoteExecutorTestBase
    {
        /// <summary>
        /// A simple test to make sure the Http Diagnostic Source is added into the list of DiagnosticListeners.
        /// </summary>
        [Fact]
        public void TestHttpDiagnosticListenerIsRegistered()
        {
            bool listenerFound = false;
            using (DiagnosticListener.AllListeners.Subscribe(new CallbackObserver<DiagnosticListener>(diagnosticListener =>
                {
                    if (diagnosticListener.Name == "System.Net.Http.Desktop")
                    {
                        listenerFound = true;
                    }
                })))
            {

                Assert.True(listenerFound, "The Http Diagnostic Listener didn't get added to the AllListeners list.");
            }
        }

        /// <summary>
        /// A simple test to make sure the Http Diagnostic Source is initialized properly after we subscribed to it, using
        /// the subscribe overload with just the observer argument.
        /// </summary>
        [OuterLoop]
        [Fact]
        public void TestReflectInitializationViaSubscription1()
        {
            using (var eventRecords = new EventObserverAndRecorder())
            {
                // Send a random Http request to generate some events
                using (var client = new HttpClient())
                {
                    client.GetAsync(Configuration.Http.RemoteEchoServer).Result.Dispose();
                }

                // Just make sure some events are written, to confirm we successfully subscribed to it. 
                // We should have exactly one Start and one Stop event
                Assert.Equal(2, eventRecords.Records.Count);
            }
        }

        /// <summary>
        /// A simple test to make sure the Http Diagnostic Source is initialized properly after we subscribed to it, using
        /// the subscribe overload with just the observer argument and the more complicating enable filter function.
        /// </summary>
        [OuterLoop]
        [Fact]
        public void TestReflectInitializationViaSubscription2()
        {
            using (var eventRecords = new EventObserverAndRecorder(eventName => true))
            {
                // Send a random Http request to generate some events
                using (var client = new HttpClient())
                {
                    client.GetAsync(Configuration.Http.RemoteEchoServer).Result.Dispose();
                }

                // Just make sure some events are written, to confirm we successfully subscribed to it. 
                // We should have exactly one Start and one Stop event
                Assert.Equal(2, eventRecords.Records.Count);
            }
        }

        /// <summary>
        /// A simple test to make sure the Http Diagnostic Source is initialized properly after we subscribed to it, using
        /// the subscribe overload with the observer argument and the simple predicate argument.
        /// </summary>
        [OuterLoop]
        [Fact]
        public void TestReflectInitializationViaSubscription3()
        {
            using (var eventRecords = new EventObserverAndRecorder((eventName, arg1, arg2) => true))
            {
                // Send a random Http request to generate some events
                using (var client = new HttpClient())
                {
                    client.GetAsync(Configuration.Http.RemoteEchoServer).Result.Dispose();
                }

                // Just make sure some events are written, to confirm we successfully subscribed to it. 
                // We should have exactly one Start and one Stop event
                Assert.Equal(2, eventRecords.Records.Count);
            }
        }

        /// <summary>
        /// Test to make sure we get both request and response events.
        /// </summary>
        [OuterLoop]
        [Fact]
        public async Task TestBasicReceiveAndResponseEvents()
        {
            using (var eventRecords = new EventObserverAndRecorder())
            {
                // Send a random Http request to generate some events
                using (var client = new HttpClient())
                {
                    (await client.GetAsync(Configuration.Http.RemoteEchoServer)).Dispose();
                }

                // We should have exactly one Start and one Stop event
                Assert.Equal(1, eventRecords.Records.Count(rec => rec.Key.EndsWith("Start")));
                Assert.Equal(1, eventRecords.Records.Count(rec => rec.Key.EndsWith("Stop")));
                Assert.Equal(2, eventRecords.Records.Count);

                // Check to make sure: The first record must be a request, the next record must be a response. 
                KeyValuePair<string, object> startEvent;
                Assert.True(eventRecords.Records.TryDequeue(out startEvent));
                Assert.Equal("System.Net.Http.Desktop.HttpRequestOut.Start", startEvent.Key);
                HttpWebRequest startRequest = ReadPublicProperty<HttpWebRequest>(startEvent.Value, "Request");
                Assert.NotNull(startRequest);
                Assert.NotNull(startRequest.Headers["Request-Id"]);

                KeyValuePair<string, object> stopEvent;
                Assert.True(eventRecords.Records.TryDequeue(out stopEvent));
                Assert.Equal("System.Net.Http.Desktop.HttpRequestOut.Stop", stopEvent.Key);
                HttpWebRequest stopRequest = ReadPublicProperty<HttpWebRequest>(stopEvent.Value, "Request");
                Assert.Equal(startRequest, stopRequest);
                HttpWebResponse response = ReadPublicProperty<HttpWebResponse>(stopEvent.Value, "Response");
                Assert.NotNull(response);
            }
        }

        /// <summary>
        /// Test to make sure we get both request and response events.
        /// </summary>
        [OuterLoop]
        [Fact]
        public async Task TestResponseWithoutContentEvents()
        {
            using (var eventRecords = new EventObserverAndRecorder())
            {
                // Send a random Http request to generate some events
                using (var client = new HttpClient())
                {
                    (await client.GetAsync(Configuration.Http.RemoteEmptyContentServer)).Dispose();
                }

                // We should have exactly one Start and one Stop event
                Assert.Equal(1, eventRecords.Records.Count(rec => rec.Key.EndsWith("Start")));
                Assert.Equal(1, eventRecords.Records.Count(rec => rec.Key.EndsWith("Stop")));
                Assert.Equal(2, eventRecords.Records.Count);

                // Check to make sure: The first record must be a request, the next record must be a response. 
                KeyValuePair<string, object> startEvent;
                Assert.True(eventRecords.Records.TryDequeue(out startEvent));
                Assert.Equal("System.Net.Http.Desktop.HttpRequestOut.Start", startEvent.Key);
                HttpWebRequest startRequest = ReadPublicProperty<HttpWebRequest>(startEvent.Value, "Request");
                Assert.NotNull(startRequest);
                Assert.NotNull(startRequest.Headers["Request-Id"]);

                KeyValuePair<string, object> stopEvent;
                Assert.True(eventRecords.Records.TryDequeue(out stopEvent));
                Assert.Equal("System.Net.Http.Desktop.HttpRequestOut.Ex.Stop", stopEvent.Key);
                HttpWebRequest stopRequest = ReadPublicProperty<HttpWebRequest>(stopEvent.Value, "Request");
                Assert.Equal(startRequest, stopRequest);
                HttpStatusCode status = ReadPublicProperty<HttpStatusCode>(stopEvent.Value, "StatusCode");
                Assert.NotNull(status);

                WebHeaderCollection headers = ReadPublicProperty<WebHeaderCollection>(stopEvent.Value, "Headers");
                Assert.NotNull(headers);
            }
        }

        /// <summary>
        /// Test that if request is redirected, it gets only one Start and one Stop event
        /// </summary>
        [OuterLoop]
        [Fact]
        public async Task TestRedirectedRequest()
        {
            using (var eventRecords = new EventObserverAndRecorder())
            {
                using (var client = new HttpClient())
                {
                    Uri uriWithRedirect =
                        Configuration.Http.RedirectUriForDestinationUri(true, 302, Configuration.Http.RemoteEchoServer, 10);
                    (await client.GetAsync(uriWithRedirect)).Dispose();
                }

                // We should have exactly one Start and one Stop event
                Assert.Equal(1, eventRecords.Records.Count(rec => rec.Key.EndsWith("Start")));
                Assert.Equal(1, eventRecords.Records.Count(rec => rec.Key.EndsWith("Stop")));
            }
        }

        /// <summary>
        /// Test exception in request processing: exception should have expected type/status and now be swallowed by reflection hook
        /// </summary>
        [OuterLoop]
        [Fact]
        public async Task TestRequestWithException()
        {
            using (var eventRecords = new EventObserverAndRecorder())
            {
                var ex =
                    await Assert.ThrowsAsync<HttpRequestException>(
                        () => new HttpClient().GetAsync($"http://{Guid.NewGuid()}.com"));

                // check that request failed because of the wrong domain name and not because of reflection
                var webException = (WebException)ex.InnerException;
                Assert.NotNull(webException);
                Assert.True(webException.Status == WebExceptionStatus.NameResolutionFailure);

                // We should have one Start event and no stop event
                Assert.Equal(1, eventRecords.Records.Count(rec => rec.Key.EndsWith("Start")));
                Assert.Equal(0, eventRecords.Records.Count(rec => rec.Key.EndsWith("Stop")));
            }
        }

        /// <summary>
        /// Test request cancellation: reflection hook does not throw
        /// </summary>
        [OuterLoop]
        [Fact]
        public async Task TestCanceledRequest()
        {
            CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using (var eventRecords = new EventObserverAndRecorder( _ => { cts.Cancel();}))
            {
                using (var client = new HttpClient())
                {
                    var ex = await Assert.ThrowsAnyAsync<Exception>(() => client.GetAsync(Configuration.Http.RemoteEchoServer, cts.Token));
                    Assert.True(ex is TaskCanceledException || ex is WebException);
                }

                // We should have one Start event and no stop event
                Assert.Equal(1, eventRecords.Records.Count(rec => rec.Key.EndsWith("Start")));
                Assert.Equal(0, eventRecords.Records.Count(rec => rec.Key.EndsWith("Stop")));
            }
        }

        /// <summary>
        /// Test Request-Id and Correlation-Context headers injection
        /// </summary>
        [OuterLoop]
        [Fact]
        public async Task TestActivityIsCreated()
        {
            var parentActivity = new Activity("parent").AddBaggage("k1", "v1").AddBaggage("k2", "v2").Start();
            using (var eventRecords = new EventObserverAndRecorder())
            {
                using (var client = new HttpClient())
                {
                    (await client.GetAsync(Configuration.Http.RemoteEchoServer)).Dispose();
                }

                Assert.Equal(1, eventRecords.Records.Count(rec => rec.Key.EndsWith("Start")));
                Assert.Equal(1, eventRecords.Records.Count(rec => rec.Key.EndsWith("Stop")));

                WebRequest thisRequest = ReadPublicProperty<WebRequest>(eventRecords.Records.First().Value, "Request");
                var requestId = thisRequest.Headers["Request-Id"];
                var correlationContext = thisRequest.Headers["Correlation-Context"];

                Assert.NotNull(requestId);
                Assert.True(requestId.StartsWith(parentActivity.Id));

                Assert.NotNull(correlationContext);
                Assert.True(correlationContext == "k1=v1,k2=v2" || correlationContext == "k2=v2,k1=v1");
            }
            parentActivity.Stop();
        }

        /// <summary>
        /// Tests IsEnabled order and parameters
        /// </summary>
        [OuterLoop]
        [Fact]
        public async Task TestIsEnabled()
        {
            int eventNumber = 0;

            bool IsEnabled(string evnt, object arg1, object arg2)
            {
                if (eventNumber == 0)
                {
                    Assert.True(evnt == "System.Net.Http.Desktop.HttpRequestOut");
                    Assert.True(arg1 is WebRequest);
                }
                else if (eventNumber == 1)
                {
                    Assert.True(evnt == "System.Net.Http.Desktop.HttpRequestOut.Start");
                }

                eventNumber++;
                return true;
            }

            using (new EventObserverAndRecorder(IsEnabled))
            {
                using (var client = new HttpClient())
                {
                    (await client.GetAsync(Configuration.Http.RemoteEchoServer)).Dispose();
                }
                Assert.Equal(2, eventNumber);
            }
        }

        /// <summary>
        /// Tests that nothing happens if IsEnabled returns false
        /// </summary>
        [OuterLoop]
        [Fact]
        public async Task TestIsEnabledAllOff()
        {
            using (var eventRecords = new EventObserverAndRecorder((evnt, arg1, arg2) => false))
            {
                using (var client = new HttpClient())
                {
                    (await client.GetAsync(Configuration.Http.RemoteEchoServer)).Dispose();
                }

                Assert.Equal(0, eventRecords.Records.Count);
            }
        }

        /// <summary>
        /// Tests that if IsEnabled for request  is false, request is not instrumented
        /// </summary>
        [OuterLoop]
        [Fact]
        public async Task TestIsEnabledRequestOff()
        {
            bool IsEnabled(string evnt, object arg1, object arg2)
            {
                if (evnt == "System.Net.Http.Desktop.HttpRequestOut")
                {
                    return (arg1 as WebRequest).RequestUri.Scheme == "https";
                }
                return true;
            }

            using (var eventRecords = new EventObserverAndRecorder(IsEnabled))
            {
                using (var client = new HttpClient())
                {
                    (await client.GetAsync(Configuration.Http.RemoteEchoServer)).Dispose();
                    Assert.Equal(0, eventRecords.Records.Count);

                    (await client.GetAsync(Configuration.Http.SecureRemoteEchoServer)).Dispose();
                    Assert.Equal(2, eventRecords.Records.Count);
                }
            }
        }

        /// <summary>
        /// Test to make sure every event record has the right dynamic properties.
        /// </summary>
        [OuterLoop]
        [Fact]
        public void TestMultipleConcurrentRequests()
        {
            ServicePointManager.DefaultConnectionLimit = Int32.MaxValue;
            var parentActivity = new Activity("parent").Start();
            using (var eventRecords = new EventObserverAndRecorder())
            {
                Dictionary<Uri, Tuple<WebRequest, WebResponse>> requestData =
                    new Dictionary<Uri, Tuple<WebRequest, WebResponse>>();
                for (int i = 0; i < 10; i++)
                {
                    Uri uriWithRedirect =
                        Configuration.Http.RedirectUriForDestinationUri(true, 302, new Uri($"{Configuration.Http.RemoteEchoServer}?q={i}"), 3);

                    requestData[uriWithRedirect] = null;
                }

                // Issue all requests simultaneously
                HttpClient httpClient = new HttpClient();
                Dictionary<Uri, Task<HttpResponseMessage>> tasks = new Dictionary<Uri, Task<HttpResponseMessage>>();

                CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                foreach (var url in requestData.Keys)
                {
                    tasks.Add(url, httpClient.GetAsync(url, cts.Token));
                }

                // wait up to 10 sec for all requests and suppress exceptions
                Task.WhenAll(tasks.Select(t => t.Value).ToArray()).ContinueWith(tt =>
                {
                    foreach (var task in tasks)
                    {
                        task.Value.Result?.Dispose();
                    }
                }).Wait();

                // Examine the result. Make sure we got all successful requests.

                // Just make sure some events are written, to confirm we successfully subscribed to it. We should have 
                // exactly 1 Start event per request and exaclty 1 Stop event per response (if request succeeded)
                var successfulTasks = tasks.Where(t => t.Value.Status == TaskStatus.RanToCompletion);

                Assert.Equal(tasks.Count, eventRecords.Records.Count(rec => rec.Key.EndsWith("Start")));
                Assert.Equal(successfulTasks.Count(), eventRecords.Records.Count(rec => rec.Key.EndsWith("Stop")));

                // Check to make sure: We have a WebRequest and a WebResponse for each successful request
                foreach (var pair in eventRecords.Records)
                {
                    object eventFields = pair.Value;

                    Assert.True(
                        pair.Key == "System.Net.Http.Desktop.HttpRequestOut.Start" ||
                        pair.Key == "System.Net.Http.Desktop.HttpRequestOut.Stop",
                        "An unexpected event of name " + pair.Key + "was received");

                    WebRequest request = ReadPublicProperty<WebRequest>(eventFields, "Request");
                    Assert.Equal(request.GetType().Name, "HttpWebRequest");

                    if (pair.Key == "System.Net.Http.Desktop.HttpRequestOut.Start")
                    {
                        // Make sure this is an URL that we recognize. If not, just skip
                        Tuple<WebRequest, WebResponse> tuple = null;
                        if (!requestData.TryGetValue(request.RequestUri, out tuple))
                        {
                            continue;
                        }

                        // all requests have Request-Id with proper parent Id
                        var requestId = request.Headers["Request-Id"];
                        Assert.True(requestId.StartsWith(parentActivity.Id));
                        // all request activities are siblings:
                        var childSuffix = requestId.Substring(0, parentActivity.Id.Length);
                        Assert.True(childSuffix.IndexOf('.') == childSuffix.Length - 1);

                        Assert.Null(requestData[request.RequestUri]);
                        requestData[request.RequestUri] =
                            new Tuple<WebRequest, WebResponse>(request, null);
                    }
                    else
                    {
                        // This must be the response.
                        WebResponse response = ReadPublicProperty<WebResponse>(eventFields, "Response");
                        Assert.Equal(response.GetType().Name, "HttpWebResponse");

                        // By the time we see the response, the request object may already have been redirected with a different
                        // url. Hence, it's not reliable to just look up requestData by the URL/hostname. Instead, we have to look
                        // through each one and match by object reference on the request object.
                        Tuple<WebRequest, WebResponse> tuple = null;
                        foreach (Tuple<WebRequest, WebResponse> currentTuple in requestData.Values)
                        {
                            if (currentTuple != null && currentTuple.Item1 == request)
                            {
                                // Found it!
                                tuple = currentTuple;
                                break;
                            }
                        }

                        // Update the tuple with the response object
                        Assert.NotNull(tuple);
                        requestData[request.RequestUri] =
                            new Tuple<WebRequest, WebResponse>(request, response);
                    }
                }

                // Finally, make sure we have request and response objects for every successful request
                foreach (KeyValuePair<Uri, Tuple<WebRequest, WebResponse>> pair in requestData)
                {
                    if (successfulTasks.Any(t => t.Key == pair.Key))
                    {
                        Assert.NotNull(pair.Value);
                        Assert.NotNull(pair.Value.Item1);
                        Assert.NotNull(pair.Value.Item2);
                    }
                }
            }
        }

        private static T ReadPublicProperty<T>(object obj, string propertyName)
        {
            Type type = obj.GetType();
            PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            return (T)property.GetValue(obj);
        }

        /// <summary>
        /// CallbackObserver is an adapter class that creates an observer (which you can pass
        /// to IObservable.Subscribe), and calls the given callback every time the 'next' 
        /// operation on the IObserver happens. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private class CallbackObserver<T> : IObserver<T>
        {
            public CallbackObserver(Action<T> callback) { _callback = callback; }
            public void OnCompleted() { }
            public void OnError(Exception error) { }
            public void OnNext(T value) { _callback(value); }

            private Action<T> _callback;
        }

        /// <summary>
        /// EventObserverAndRecorder is an observer that watches all Http diagnostic listener events flowing
        /// through, and record all of them
        /// </summary>
        private class EventObserverAndRecorder : IObserver<KeyValuePair<string, object>>, IDisposable
        {
            private readonly Action<KeyValuePair<string, object>> onEvent;

            public EventObserverAndRecorder(Action<KeyValuePair<string, object>> onEvent = null)
            {
                listSubscription = DiagnosticListener.AllListeners.Subscribe(new CallbackObserver<DiagnosticListener>(diagnosticListener =>
                {
                    if (diagnosticListener.Name == "System.Net.Http.Desktop")
                    {
                        httpSubscription = diagnosticListener.Subscribe(this);
                    }
                }));

                this.onEvent = onEvent;
            }

            public EventObserverAndRecorder(Predicate<string> isEnabled)
            {
                listSubscription = DiagnosticListener.AllListeners.Subscribe(new CallbackObserver<DiagnosticListener>(diagnosticListener =>
                {
                    if (diagnosticListener.Name == "System.Net.Http.Desktop")
                    {
                        httpSubscription = diagnosticListener.Subscribe(this, isEnabled);
                    }
                }));
            }

            public EventObserverAndRecorder(Func<string, object, object, bool> isEnabled)
            {
                listSubscription = DiagnosticListener.AllListeners.Subscribe(new CallbackObserver<DiagnosticListener>(diagnosticListener =>
                {
                    if (diagnosticListener.Name == "System.Net.Http.Desktop")
                    {
                        httpSubscription = diagnosticListener.Subscribe(this, isEnabled);
                    }
                }));
            }

            public void Dispose()
            {
                listSubscription.Dispose();
                httpSubscription.Dispose();
            }

            public ConcurrentQueue<KeyValuePair<string, object>> Records { get; } = new ConcurrentQueue<KeyValuePair<string, object>>();

            public void OnCompleted() { }
            public void OnError(Exception error) { }

            public void OnNext(KeyValuePair<string, object> record)
            {
                Records.Enqueue(record);
                onEvent?.Invoke(record);
            }

            private readonly IDisposable listSubscription;
            private IDisposable httpSubscription;
        }
    }
}
