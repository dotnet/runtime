using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
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
            HttpResponseMessage message = new HttpClient().GetAsync("http://www.bing.com").Result;

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
            HttpResponseMessage message = new HttpClient().GetAsync("http://www.bing.com").Result;

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
            HttpResponseMessage message = new HttpClient().GetAsync("http://www.bing.com").Result;

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
            HttpResponseMessage message = new HttpClient().GetAsync("http://www.bing.com").Result;

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
                object eventFields = pair.Value;
                WebRequest thisRequest = ReadPublicProperty<WebRequest>(eventFields, "Request");

                if (i == 0)
                {
                    Assert.Equal("System.Net.Http.Request", pair.Key);
                    firstRequest = thisRequest;
                }
                else if (i == eventRecords.Records.Count - 1)
                {
                    Assert.Equal("System.Net.Http.Response", pair.Key);
                    Assert.Equal(firstRequest, thisRequest);
                }
                else
                {
                    Assert.True(pair.Key == "System.Net.Http.Response" || pair.Key == "System.Net.Http.Request", "An unexpected event of name " + pair.Key + "was received");
                    Assert.Equal(firstRequest, thisRequest);
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
            HttpResponseMessage message = new HttpClient().GetAsync("http://www.bing.com").Result;

            // Just make sure some events are written, to confirm we successfully subscribed to it. We should have 
            // at least two events, one for request send, and one for response receive
            Assert.True(eventRecords.Records.Count >= 2, "Didn't get two or more events from Http Diagnostic Listener. Something is wrong.");

            // Check to make sure: The first record must be a request, the last record must be a response. Records in
            // the middle can be anything since it depends on # of redirections
            for (int i = 0; i < eventRecords.Records.Count; i++)
            {
                var pair = eventRecords.Records[i];
                object eventFields = pair.Value;

                Assert.True(pair.Key == "System.Net.Http.Response" || pair.Key == "System.Net.Http.Request", "An unexpected event of name " + pair.Key + "was received");

                WebRequest request = ReadPublicProperty<WebRequest>(eventFields, "Request");
                long timestamp = ReadPublicProperty<long>(eventFields, "Timestamp");

                Assert.Equal(request.GetType().Name, "HttpWebRequest");

                // Just compare the timestamp to make sure it's reasonable. In an poorman experiment, we
                // found that 10 secs is roughly 30,000,000 ticks
                Assert.True(timestamp - beginTimestamp > 0 && timestamp - beginTimestamp < 30 * 1000 * 1000, $"The timestamp sent with the event doesn't look correct. Begin {beginTimestamp} End {timestamp} Diff {timestamp - beginTimestamp} Expected < {30 * 1000 * 1000}");

                if (pair.Key == "System.Net.Http.Response")
                {
                    object response = ReadPublicProperty<WebResponse>(eventFields, "Response");
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
            requestData.Add("http://www.microsoft.com", null);
            requestData.Add("http://www.bing.com", null);
            requestData.Add("http://www.xbox.com", null);
            requestData.Add("http://www.office.com", null);
            requestData.Add("http://www.microsoftstore.com", null);
            requestData.Add("http://www.msn.com", null);
            requestData.Add("http://outlook.live.com", null);
            requestData.Add("http://build.microsoft.com", null);
            requestData.Add("http://azure.microsoft.com", null);
            requestData.Add("http://www.nuget.org", null);
            requestData.Add("http://support.microsoft.com", null);
            requestData.Add("http://www.visualstudio.com", null);
            requestData.Add("http://msdn.microsoft.com", null);
            requestData.Add("http://onedrive.live.com", null);
            requestData.Add("http://community.dynamics.com", null);
            requestData.Add("http://login.live.com", null);
            requestData.Add("http://www.skype.com", null);
            requestData.Add("http://channel9.msdn.com", null);

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
                object eventFields = pair.Value;                

                Assert.True(pair.Key == "System.Net.Http.Response" || pair.Key == "System.Net.Http.Request", "An unexpected event of name " + pair.Key + "was received");

                WebRequest request = ReadPublicProperty<WebRequest>(eventFields, "Request");
                long timestamp = ReadPublicProperty<long>(eventFields, "Timestamp");

                Assert.Equal(request.GetType().Name, "HttpWebRequest");

                // Just compare the timestamp to make sure it's reasonable. In an poorman experiment, we
                // found that 10 secs is roughly 30,000,000 ticks
                Assert.True(timestamp - beginTimestamp > 0 && timestamp - beginTimestamp < 30 * 1000 * 1000, $"The timestamp sent with the event doesn't look correct. Begin {beginTimestamp} End {timestamp} Diff {timestamp - beginTimestamp} Expected < {30 * 1000 * 1000}");

                if (pair.Key == "System.Net.Http.Request")
                {
                    // Make sure this is an URL that we recognize. If not, just skip
                    Tuple<WebRequest, WebResponse> tuple = null;
                    if (!requestData.TryGetValue(request.RequestUri.OriginalString, out tuple))
                    {
                        continue;
                    }

                    WebRequest previousSeenRequest = tuple?.Item1;
                    WebResponse previousSeenResponse = tuple?.Item2;

                    // We see have seen an HttpWebRequest before for this URL/host, make sure it's the same one,
                    // Then update the tuple with the request object, if we didn't have one
                    Assert.True(previousSeenRequest == null || previousSeenRequest == request, "Didn't expect to see a different WebRequest object going to the same url host for: " + request.RequestUri.OriginalString);
                    requestData[request.RequestUri.OriginalString] = new Tuple<WebRequest, WebResponse>(previousSeenRequest ?? request, previousSeenResponse);
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
                    requestData[request.RequestUri.OriginalString] = new Tuple<WebRequest, WebResponse>(request, response);
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

        private static T ReadPublicProperty<T>(object obj, string propertyName)
        {
            Type type = obj.GetType();
            PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            return (T)property.GetValue(obj);
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
            public List<KeyValuePair<string, object>> Records { get; } = new List<KeyValuePair<string, object>>();

            public void OnCompleted() { }
            public void OnError(Exception error) { }
            public void OnNext(KeyValuePair<string, object> record) { Records.Add(record);  }
        }
    }
}
