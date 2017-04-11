using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Diagnostics.Tests
{
    [SkipOnTargetFramework(
        TargetFrameworkMonikers.Net45|
        TargetFrameworkMonikers.Net451 |
        TargetFrameworkMonikers.Net452 |
        TargetFrameworkMonikers.Netcore50 |
        TargetFrameworkMonikers.Netcore50aot |
        TargetFrameworkMonikers.Netcoreapp |
        TargetFrameworkMonikers.Netcoreapp1_0 |
        TargetFrameworkMonikers.Netcoreapp1_1 |
        TargetFrameworkMonikers.NetcoreCoreRT |
        TargetFrameworkMonikers.Uap |
        TargetFrameworkMonikers.UapAot)]
    public class HttpHandlerDiagnosticListenerTests
    {
        /// <summary>
        /// A simple test to make sure the Http Diagnostic Source is added into the list of DiagnosticListeners.
        /// </summary>
        [Fact]
        public void TestHttpDiagnosticListenerIsRegistered()
        {
            bool listenerFound = false;
            DiagnosticListener.AllListeners.Subscribe(new CallbackObserver<DiagnosticListener>(diagnosticListener =>
            {
                if (diagnosticListener.Name == "System.Net.Http.Desktop")
                {
                    listenerFound = true;
                }
            }));

            Assert.True(listenerFound, "The Http Diagnostic Listener didn't get added to the AllListeners list.");
        }

        /// <summary>
        /// A simple test to make sure the Http Diagnostic Source is initialized properly after we subscribed to it, using
        /// the subscribe overload with just the observer argument.
        /// </summary>
        [Fact]
        public void TestReflectInitializationViaSubscription1()
        {
            EventObserverAndRecorder eventRecords = CreateEventRecorder();

            // Send a random Http request to generate some events
            HttpResponseMessage message = new HttpClient().GetAsync("www.bing.com").Result;

            // Just make sure some events are written, to confirm we successfully subscribed to it. We should have 
            // at least two events, one for request send, and one for response receive
            Assert.True(eventRecords.Records.Count >= 2, "Didn't get two or more events from Http Diagnostic Listener. Something is wrong.");
        }

        /// <summary>
        /// A simple test to make sure the Http Diagnostic Source is initialized properly after we subscribed to it, using
        /// the subscribe overload with the observer argument and the simple predicate argument.
        /// </summary>
        [Fact]
        public void TestReflectInitializationViaSubscription2()
        {
            EventObserverAndRecorder eventRecords = new EventObserverAndRecorder();
            DiagnosticListener.AllListeners.Subscribe(new CallbackObserver<DiagnosticListener>(diagnosticListener =>
            {
                if (diagnosticListener.Name == "System.Net.Http.Desktop")
                {
                    diagnosticListener.Subscribe(eventRecords, (eventName) => { return true; });
                }
            }));

            // Send a random Http request to generate some events
            HttpResponseMessage message = new HttpClient().GetAsync("www.bing.com").Result;

            // Just make sure some events are written, to confirm we successfully subscribed to it. We should have 
            // at least two events, one for request send, and one for response receive
            Assert.True(eventRecords.Records.Count >= 2, "Didn't get two or more events from Http Diagnostic Listener. Something is wrong.");
        }

        /// <summary>
        /// A simple test to make sure the Http Diagnostic Source is initialized properly after we subscribed to it, using
        /// the subscribe overload with just the observer argument and the more compolicating enable filter function.
        /// </summary>
        [Fact]
        public void TestReflectInitializationViaSubscription3()
        {
            EventObserverAndRecorder eventRecords = new EventObserverAndRecorder();
            DiagnosticListener.AllListeners.Subscribe(new CallbackObserver<DiagnosticListener>(diagnosticListener =>
            {
                if (diagnosticListener.Name == "System.Net.Http.Desktop")
                {
                    diagnosticListener.Subscribe(eventRecords, (eventName, obj1, obj2) => { return true; });
                }
            }));

            // Send a random Http request to generate some events
            HttpResponseMessage message = new HttpClient().GetAsync("www.bing.com").Result;

            // Just make sure some events are written, to confirm we successfully subscribed to it. We should have 
            // at least two events, one for request send, and one for response receive
            Assert.True(eventRecords.Records.Count >= 2, "Didn't get two or more events from Http Diagnostic Listener. Something is wrong.");
        }

        /// <summary>
        /// Test to make sure we get both request and response events.
        /// </summary>
        [Fact]
        public void TestBasicReceiveAndResponseEvents()
        {
            EventObserverAndRecorder eventRecords = CreateEventRecorder();

            // Send a random Http request to generate some events
            HttpResponseMessage message = new HttpClient().GetAsync("www.bing.com").Result;

            // Just make sure some events are written, to confirm we successfully subscribed to it. We should have 
            // at least two events, one for request send, and one for response receive
            Assert.True(eventRecords.Records.Count >= 2, "Didn't get two or more events from Http Diagnostic Listener. Something is wrong.");

            // I can't use the HttpWebRequest type here since it doesn't exist in .net core, so just use "object"
            WebRequest firstRequest = null;

            // Check to make sure: The first record must be a request, the last record must be a response. Records in
            // the middle can be anything since it depends on # of redirections
            for (int i = 0; i < eventRecords.Records.Count; i++)
            {
                var pair = eventRecords.Records[i];
                dynamic eventFields = pair.Value;
                WebRequest thisRequest = eventFields.Request;

                if (i == 0)
                {
                    Assert.Equal("System.Net.Http.Request", pair.Key);                    
                    firstRequest = eventFields.Request;
                }
                else if (i == eventRecords.Records.Count - 1)
                {
                    Assert.Equal("System.Net.Http.Response", pair.Key);
                    Assert.ReferenceEquals(firstRequest, eventFields.Request);
                }
                else
                {
                    Assert.True(pair.Key == "System.Net.Http.Response" || pair.Key == "System.Net.Http.Request", "An unexpected event of name " + pair.Key + "was received");
                    Assert.ReferenceEquals(firstRequest, eventFields.Request);
                }
            }
        }

        /// <summary>
        /// Test to make sure every event record has the right dynamic properties.
        /// </summary>
        [Fact]
        public void TestDynamicPropertiesOnReceiveAndResponseEvents()
        {
            EventObserverAndRecorder eventRecords = CreateEventRecorder();
            long beginTimestamp = Stopwatch.GetTimestamp();

            // Send a random Http request to generate some events
            HttpResponseMessage message = new HttpClient().GetAsync("www.bing.com").Result;

            // Just make sure some events are written, to confirm we successfully subscribed to it. We should have 
            // at least two events, one for request send, and one for response receive
            Assert.True(eventRecords.Records.Count >= 2, "Didn't get two or more events from Http Diagnostic Listener. Something is wrong.");

            // Check to make sure: The first record must be a request, the last record must be a response. Records in
            // the middle can be anything since it depends on # of redirections
            for (int i = 0; i < eventRecords.Records.Count; i++)
            {
                var pair = eventRecords.Records[i];
                dynamic eventFields = pair.Value;

                Assert.True(pair.Key == "System.Net.Http.Response" || pair.Key == "System.Net.Http.Request", "An unexpected event of name " + pair.Key + "was received");

                WebRequest request = eventFields.Request;
                long timestamp = eventFields.Timestamp;

                Assert.Equal(request.GetType().Name, "HttpWebRequest");

                // Just compare the timestamp to make sure it's reasonable. In an poorman experiment, we
                // found that 10 secs is roughly 30,000,000 ticks
                Assert.True(timestamp - beginTimestamp > 0 && timestamp - beginTimestamp < 30 * 1000 * 1000, "The timestamp sent with the event doesn't look correct");

                if (pair.Key == "System.Net.Http.Response")
                {
                    object response = eventFields.Response;
                    Assert.Equal(response.GetType().Name, "HttpWebResponse");
                }
            }
        }

        /// <summary>
        /// Test to make sure every event record has the right dynamic properties.
        /// </summary>
        [Fact]
        public void TestMultipleConcurrentRequests()
        {
            EventObserverAndRecorder eventRecords = CreateEventRecorder();
            long beginTimestamp = Stopwatch.GetTimestamp();

            Dictionary<string, Tuple<WebRequest, WebResponse>> requestData = new Dictionary<string, Tuple<WebRequest, WebResponse>>();
            requestData.Add("www.microsoft.com", null);
            requestData.Add("www.bing.com", null);
            requestData.Add("www.xbox.com", null);
            requestData.Add("www.office.com", null);
            requestData.Add("www.microsoftstore.com", null);
            requestData.Add("www.msn.com", null);
            requestData.Add("outlook.live.com", null);
            requestData.Add("build.microsoft.com", null);
            requestData.Add("azure.microsoft.com", null);
            requestData.Add("www.nuget.org", null);
            requestData.Add("support.microsoft.com", null);
            requestData.Add("www.visualstudio.com", null);
            requestData.Add("msdn.microsoft.com", null);
            requestData.Add("onedrive.live.com", null);
            requestData.Add("community.dynamics.com", null);
            requestData.Add("login.live.com", null);
            requestData.Add("www.skype.com", null);
            requestData.Add("channel9.msdn.com", null);

            // Issue all requests simultaneously
            HttpClient httpClient = new HttpClient();
            List<Task<HttpResponseMessage>> tasks = new List<Task<HttpResponseMessage>>();
            foreach (string url in requestData.Keys)
            {
                tasks.Add(httpClient.GetAsync(url));
            }

            Task.WaitAll(tasks.ToArray());

            // Examine the result. Make sure we got them all.

            // Just make sure some events are written, to confirm we successfully subscribed to it. We should have 
            // at least two events, one for request send, and one for response receive
            Assert.True(eventRecords.Records.Count >= requestData.Count * 2, "Didn't get two or more events on average from each request. Something is wrong.");

            // Check to make sure: We have a WebRequest and a WebResponse for each URL
            for (int i = 0; i < eventRecords.Records.Count; i++)
            {
                var pair = eventRecords.Records[i];
                dynamic eventFields = pair.Value;

                Assert.True(pair.Key == "System.Net.Http.Response" || pair.Key == "System.Net.Http.Request", "An unexpected event of name " + pair.Key + "was received");

                WebRequest request = eventFields.Request;
                long timestamp = eventFields.Timestamp;

                Assert.Equal(request.GetType().Name, "HttpWebRequest");

                // Just compare the timestamp to make sure it's reasonable. In an poorman experiment, we
                // found that 10 secs is roughly 30,000,000 ticks
                Assert.True(timestamp - beginTimestamp > 0 && timestamp - beginTimestamp < 30 * 1000 * 1000, "The timestamp sent with the event doesn't look correct");

                if (pair.Key == "System.Net.Http.Request")
                {
                    // Make sure this is an URL that we recognize. If not, just skip
                    Tuple<WebRequest, WebResponse> tuple = null;
                    if (!requestData.TryGetValue(request.RequestUri.Host, out tuple))
                    {
                        continue;
                    }

                    WebRequest previousSeenRequest = tuple?.Item1;
                    WebResponse previousSeenResponse = tuple?.Item2;

                    // We see have seen an HttpWebRequest before for this URL/host, make sure it's the same one,
                    // Then update the tuple with the request object, if we didn't have one
                    Assert.True(previousSeenRequest == null || previousSeenRequest == request, "Didn't expect to see a different WebRequest object going to the same url host for: " + request.RequestUri.Host);
                    requestData[request.RequestUri.Host] = new Tuple<WebRequest, WebResponse>(previousSeenRequest ?? request, previousSeenResponse);
                }
                else
                {
                    // This must be the response.
                    WebResponse response = eventFields.Response;
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
                    requestData[request.RequestUri.Host] = new Tuple<WebRequest, WebResponse>(request, response);
                }
            }

            // Finally, make sure we have request and response objects for every entry
            foreach (KeyValuePair<string, Tuple<WebRequest, WebResponse>> pair in requestData)
            {
                Assert.NotNull(pair.Value);
                Assert.NotNull(pair.Value.Item1);
                Assert.NotNull(pair.Value.Item2);
            }
        }

        private EventObserverAndRecorder CreateEventRecorder()
        {
            EventObserverAndRecorder eventRecords = new EventObserverAndRecorder();
            DiagnosticListener.AllListeners.Subscribe(new CallbackObserver<DiagnosticListener>(diagnosticListener =>
            {
                if (diagnosticListener.Name == "System.Net.Http.Desktop")
                {
                    diagnosticListener.Subscribe(eventRecords);
                }
            }));
            return eventRecords;
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
        private class EventObserverAndRecorder : IObserver<KeyValuePair<string, object>>
        {
            public List<KeyValuePair<string, object>> Records { get; private set; }

            public void OnCompleted() { }
            public void OnError(Exception error) { }
            public void OnNext(KeyValuePair<string, object> record) { Records.Add(record); }
        }
    }
}
