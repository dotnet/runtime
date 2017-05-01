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
    public class HttpHandlerDiagnosticListenerTests
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
        [Fact]
        public void TestReflectInitializationViaSubscription1()
        {
            using (var eventRecords = new EventObserverAndRecorder())
            {
                // Send a random Http request to generate some events
                new HttpClient().GetAsync("http://www.bing.com").Wait();

                // Just make sure some events are written, to confirm we successfully subscribed to it. We should have 
                // at least two events, one for request send, and one for response receive
                Assert.True(eventRecords.Records.Count >= 2,
                    "Didn't get two or more events from Http Diagnostic Listener. Something is wrong.");
            }
        }

        /// <summary>
        /// A simple test to make sure the Http Diagnostic Source is initialized properly after we subscribed to it, using
        /// the subscribe overload with just the observer argument and the more compolicating enable filter function.
        /// </summary>
        [Fact]
        public void TestReflectInitializationViaSubscription2()
        {
            using (var eventRecords = new EventObserverAndRecorder(eventName => true))
            {
                // Send a random Http request to generate some events
                new HttpClient().GetAsync("http://www.bing.com").Wait();

                // Just make sure some events are written, to confirm we successfully subscribed to it. We should have 
                // at least two events, one for request send, and one for response receive
                Assert.True(eventRecords.Records.Count >= 2,
                    "Didn't get two or more events from Http Diagnostic Listener. Something is wrong.");
            }
        }

        /// <summary>
        /// A simple test to make sure the Http Diagnostic Source is initialized properly after we subscribed to it, using
        /// the subscribe overload with the observer argument and the simple predicate argument.
        /// </summary>
        [Fact]
        public void TestReflectInitializationViaSubscription3()
        {
            using (var eventRecords = new EventObserverAndRecorder((eventName, arg1, arg2) => true))
            {
                // Send a random Http request to generate some events
                new HttpClient().GetAsync("http://www.bing.com").Wait();

                // Just make sure some events are written, to confirm we successfully subscribed to it. We should have 
                // at least two events, one for request send, and one for response receive
                Assert.Equal(2, eventRecords.Records.Count);
            }
        }

        /// <summary>
        /// Test to make sure we get both request and response events.
        /// </summary>
        [Fact]
        public async Task TestBasicReceiveAndResponseEvents()
        {
            using (var eventRecords = new EventObserverAndRecorder())
            {
                // Send a random Http request to generate some events
                await new HttpClient().GetAsync("http://www.bing.com");

                // Just make sure some events are written, to confirm we successfully subscribed to it. We should have 
                // at least two events, one for request send, and one for response receive
                Assert.Equal(1, eventRecords.Records.Count(rec => rec.Key.EndsWith("Start")));
                Assert.Equal(1, eventRecords.Records.Count(rec => rec.Key.EndsWith("Stop")));

                // Check to make sure: The first record must be a request, the next record must be a response. 
                // The rest is unknown number of responses (it depends on # of redirections)

                KeyValuePair<string, object> startEvent;
                Assert.True(eventRecords.Records.TryDequeue(out startEvent));
                Assert.Equal("System.Net.Http.Desktop.HttpRequestOut.Start", startEvent.Key);
                WebRequest startRequest = ReadPublicProperty<WebRequest>(startEvent.Value, "Request");
                Assert.NotNull(startRequest.Headers["Request-Id"]);

                KeyValuePair<string, object> stopEvent;
                Assert.True(eventRecords.Records.TryDequeue(out stopEvent));
                Assert.Equal("System.Net.Http.Desktop.HttpRequestOut.Stop", stopEvent.Key);
                WebRequest stopRequest = ReadPublicProperty<WebRequest>(stopEvent.Value, "Request");
                Assert.Equal(startRequest, stopRequest);
            }
        }

        /// <summary>
        /// Test exception in request processing: exception should have expected type/status and now be swallowed by reflection hook
        /// </summary>
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

                // Just make sure some events are written, to confirm we successfully subscribed to it. We should have 
                // at least two events, one for request send, and one for response receive
                Assert.Equal(1, eventRecords.Records.Count(rec => rec.Key.EndsWith("Start")));
                Assert.Equal(0, eventRecords.Records.Count(rec => rec.Key.EndsWith("Stop")));
            }
        }

        /// <summary>
        /// Test request cancellation: reflection hook does not throw
        /// </summary>
        [Fact]
        public async Task TestCanceledRequest()
        {
            using (var eventRecords = new EventObserverAndRecorder())
            {
                CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));
                await Assert.ThrowsAsync<TaskCanceledException>(
                    () => new HttpClient().GetAsync("http://bing.com", cts.Token));

                // Just make sure some events are written, to confirm we successfully subscribed to it. We should have 
                // at least two events, one for request send, and one for response receive
                Assert.Equal(1, eventRecords.Records.Count(rec => rec.Key.EndsWith("Start")));
                Assert.Equal(0, eventRecords.Records.Count(rec => rec.Key.EndsWith("Stop")));
            }
        }

        /// <summary>
        /// Test Request-Id and Correlation-Context headers injection
        /// </summary>
        [Fact]
        public async Task TestActivityIsCreated()
        {
            var parentActivity = new Activity("parent").AddBaggage("k1", "v1").AddBaggage("k2", "v2").Start();
            using (var eventRecords = new EventObserverAndRecorder())
            {
                await new HttpClient().GetAsync("http://www.bing.com");

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
                await new HttpClient().GetAsync("http://www.bing.com");
                Assert.Equal(2, eventNumber);
            }
        }
        
        /// <summary>
        /// Tests that nothing happens if IsEnabled returns false
        /// </summary>
        [Fact]
        public async Task TestIsEnabledAllOff()
        {
            using (var eventRecords = new EventObserverAndRecorder((evnt, arg1, arg2) => false))
            {
                await new HttpClient().GetAsync("http://www.bing.com");

                Assert.Equal(0, eventRecords.Records.Count);
            }
        }

        /// <summary>
        /// Tests that if IsEnabled for request  is false, request is not instrumented
        /// </summary>
        [Fact]
        public async Task TestIsEnabledRequestOff()
        {
            bool IsEnabled(string evnt, object arg1, object arg2)
            {
                if (evnt == "System.Net.Http.Desktop.HttpRequestOut")
                {
                    return !(arg1 as WebRequest).RequestUri.ToString().Contains("bing");
                }
                return true;
            }

            using (var eventRecords = new EventObserverAndRecorder(IsEnabled))
            {
                await new HttpClient().GetAsync("http://www.bing.com");
                Assert.Equal(0, eventRecords.Records.Count);

                await new HttpClient().GetAsync("http://www.microsoft.com");
                Assert.True(eventRecords.Records.Count > 0);
            }
        }

        /// <summary>
        /// Test to make sure every event record has the right dynamic properties.
        /// </summary>
        [Fact]
        public void TestDynamicPropertiesOnReceiveAndResponseEvents()
        {
            using (var eventRecords = new EventObserverAndRecorder())
            {
                long beginTimestamp = Stopwatch.GetTimestamp();

                // Send a random Http request to generate some events
                HttpResponseMessage message = new HttpClient().GetAsync("http://www.bing.com").Result;

                // Just make sure some events are written, to confirm we successfully subscribed to it. We should have 
                // at least two events, one for request send, and one for response receive
                Assert.Equal(1, eventRecords.Records.Count(rec => rec.Key.EndsWith("Start")));
                Assert.Equal(1, eventRecords.Records.Count(rec => rec.Key.EndsWith("Stop")));


                // Check to make sure: The first record must be a request, the last record must be a response. Records in
                // the middle can be anything since it depends on # of redirections
                foreach (var pair in eventRecords.Records)
                {
                    object eventFields = pair.Value;

                    Assert.True(
                        pair.Key == "System.Net.Http.Desktop.HttpRequestOut.Start" ||
                        pair.Key == "System.Net.Http.Desktop.HttpRequestOut.Stop",
                        "An unexpected event of name " + pair.Key + "was received");

                    WebRequest request = ReadPublicProperty<WebRequest>(eventFields, "Request");
                    Assert.Equal(request.GetType().Name, "HttpWebRequest");

                    if (pair.Key == "System.Net.Http.Desktop.HttpRequestOut.Stop")
                    {
                        object response = ReadPublicProperty<WebResponse>(eventFields, "Response");
                        Assert.Equal(response.GetType().Name, "HttpWebResponse");
                    }
                }
            }
        }

        /// <summary>
        /// Test to make sure every event record has the right dynamic properties.
        /// </summary>
        [Fact]
        public void TestMultipleConcurrentRequests()
        {
            ServicePointManager.DefaultConnectionLimit = Int32.MaxValue;
            var parentActivity = new Activity("parent").Start();
            using (var eventRecords = new EventObserverAndRecorder())
            {
                long beginTimestamp = Stopwatch.GetTimestamp();

                Dictionary<string, Tuple<WebRequest, WebResponse>> requestData =
                    new Dictionary<string, Tuple<WebRequest, WebResponse>>
                    {
                        {"http://www.microsoft.com", null},
                        {"http://www.bing.com", null},
                        {"http://www.xbox.com", null},
                        {"http://www.office.com", null},
                        {"http://www.microsoftstore.com", null},
                        {"http://www.msn.com", null},
                        {"http://outlook.live.com", null},
                        {"http://build.microsoft.com", null},
                        {"http://azure.microsoft.com", null},
                        {"http://www.nuget.org", null},
                        {"http://support.microsoft.com", null},
                        {"http://www.visualstudio.com", null},
                        {"http://msdn.microsoft.com", null},
                        {"http://onedrive.live.com", null},
                        {"http://community.dynamics.com", null},
                        {"http://login.live.com", null},
                        {"http://www.skype.com", null},
                        {"http://channel9.msdn.com", null}
                    };

                // Issue all requests simultaneously
                HttpClient httpClient = new HttpClient();

                Dictionary<string, Task<HttpResponseMessage>> tasks = new Dictionary<string, Task<HttpResponseMessage>>();

                CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                foreach (string url in requestData.Keys)
                {
                    tasks.Add(url, httpClient.GetAsync(url, cts.Token));
                }

                // wait up to 10 sec for all requests and suppress exceptions
                Task.WhenAll(tasks.Select(t => t.Value).ToArray()).ContinueWith(tt => {}).Wait();

                // Examine the result. Make sure we got all successful requests.

                // Just make sure some events are written, to confirm we successfully subscribed to it. We should have 
                // exactly 1 Start event per request and exaclty 1 Stop event per response (if request succeeded)
                var successfulTasks = tasks.Where(t => t.Value.Status == TaskStatus.RanToCompletion);

                Assert.Equal(tasks.Count(), eventRecords.Records.Count(rec => rec.Key.EndsWith("Start")));
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
                        if (!requestData.TryGetValue(request.RequestUri.OriginalString, out tuple))
                        {
                            continue;
                        }

                        // all requests have Request-Id with proper parent Id
                        var requestId = request.Headers["Request-Id"];
                        Assert.True(requestId.StartsWith(parentActivity.Id));
                        // all request activities are siblings:
                        var childSuffix = requestId.Substring(0, parentActivity.Id.Length);
                        Assert.True(childSuffix.IndexOf('.') == childSuffix.Length - 1);

                        Assert.Null(requestData[request.RequestUri.OriginalString]);
                        requestData[request.RequestUri.OriginalString] =
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
                        requestData[request.RequestUri.OriginalString] =
                            new Tuple<WebRequest, WebResponse>(request, response);
                    }
                }

                // Finally, make sure we have request and response objects for every successful request
                foreach (KeyValuePair<string, Tuple<WebRequest, WebResponse>> pair in requestData)
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
        /// CallbackObserver is a adapter class that creates an observer (which you can pass
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
        /// EventObserverAndRecorder is an observer that watches all Http diagnosticlistener events flowing
        /// through, and record all of them
        /// </summary>
        private class EventObserverAndRecorder : IObserver<KeyValuePair<string, object>>, IDisposable
        {
            public EventObserverAndRecorder()
            {
                listSubscription = DiagnosticListener.AllListeners.Subscribe(new CallbackObserver<DiagnosticListener>(diagnosticListener =>
                {
                    if (diagnosticListener.Name == "System.Net.Http.Desktop")
                    {
                        httpSubscription = diagnosticListener.Subscribe(this);
                    }
                }));
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
            public void OnNext(KeyValuePair<string, object> record) { Records.Enqueue(record);  }

            private readonly IDisposable listSubscription;
            private IDisposable httpSubscription;
        }
    }
}
