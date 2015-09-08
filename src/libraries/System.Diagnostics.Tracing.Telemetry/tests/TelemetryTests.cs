// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;
using System.Threading.Tasks;
using TelemData = System.Collections.Generic.KeyValuePair<string, object>;

namespace System.Diagnostics.Tracing.Telemetry.Tests
{
    /// <summary>
    /// Tests for TelemetrySource and TelemetryListener
    /// </summary>
    public class TelemetryTest
    {
        /// <summary>
        /// Trivial example of passing an integer
        /// </summary>
        [Fact]
        public void IntPayload()
        {
            var result = new List<KeyValuePair<string, object>>();
            var observer = new ObserverToList<TelemData>(result);
            using (TelemetryListener.DefaultListener.Subscribe(new ObserverToList<TelemData>(result)))
            {
                TelemetrySource.DefaultSource.WriteTelemetry("IntPayload", 5);
                Assert.Equal(1, result.Count);
                Assert.Equal("IntPayload", result[0].Key);
                Assert.Equal(5, result[0].Value);
            }   // unsubscribe

            // Make sure that after unsubscribing, we don't get more events. 
            TelemetrySource.DefaultSource.WriteTelemetry("IntPayload", 5);
            Assert.Equal(1, result.Count);
        }

        /// <summary>
        /// slightly less trivial of passing a structure with a couple of fields 
        /// </summary>
        [Fact]
        public void StructPayload()
        {
            var result = new List<KeyValuePair<string, object>>();
            using (TelemetryListener.DefaultListener.Subscribe(new ObserverToList<TelemData>(result)))
            {
                TelemetrySource.DefaultSource.WriteTelemetry("StructPayload", new Payload() { Name = "Hi", Id = 67 });
                Assert.Equal(1, result.Count);
                Assert.Equal("StructPayload", result[0].Key);
                var payload = (Payload)result[0].Value;

                Assert.Equal(67, payload.Id);
                Assert.Equal("Hi", payload.Name);
            }

            TelemetrySource.DefaultSource.WriteTelemetry("StructPayload", new Payload() { Name = "Hi", Id = 67 });
            Assert.Equal(1, result.Count);
        }

        /// <summary>
        /// Tests the IObserver OnCompleted callback. 
        /// </summary>
        [Fact]
        public void Completed()
        {
            var result = new List<KeyValuePair<string, object>>();
            var observer = new ObserverToList<TelemData>(result);
            var listener = new TelemetryListener("MyListener");
            var subscription = listener.Subscribe(observer);

            listener.WriteTelemetry("IntPayload", 5);
            Assert.Equal(1, result.Count);
            Assert.Equal("IntPayload", result[0].Key);
            Assert.Equal(5, result[0].Value);
            Assert.False(observer.Completed);

            // The listener dies
            listener.Dispose();
            Assert.True(observer.Completed);

            // confirm that we can unsubscribe without crashing 
            subscription.Dispose();

            // If we resubscribe after dispose, but it does not do anything.  
            subscription = listener.Subscribe(observer);

            listener.WriteTelemetry("IntPayload", 5);
            Assert.Equal(1, result.Count);
        }

        /// <summary>
        /// Simple tests for the IsEnabled method.
        /// </summary>
        [Fact]
        public void BasicIsEnabled()
        {
            var result = new List<KeyValuePair<string, object>>();

            bool seenUninteresting = false;
            bool seenStructPayload = false;
            Predicate<string> predicate = delegate (string telemetryName)
            {
                if (telemetryName == "Uninteresting")
                    seenUninteresting = true;
                if (telemetryName == "StructPayload")
                    seenStructPayload = true;

                return telemetryName == "StructPayload";
            };

            using (TelemetryListener.DefaultListener.Subscribe(new ObserverToList<TelemData>(result), predicate))
            {
                Assert.False(TelemetrySource.DefaultSource.IsEnabled("Uninteresting"));
                Assert.True(TelemetrySource.DefaultSource.IsEnabled("StructPayload"));

                Assert.True(seenUninteresting);
                Assert.True(seenStructPayload);
            }
        }

        /// <summary>
        /// Test if it works when you have two subscribers active simultaneously
        /// </summary>
        [Fact]
        public void MultiSubscriber()
        {
            var subscriber1Result = new List<KeyValuePair<string, object>>();
            Predicate<string> subscriber1Predicate = telemetryName => (telemetryName == "DataForSubscriber1");
            var subscriber1Oberserver = new ObserverToList<TelemData>(subscriber1Result);

            var subscriber2Result = new List<KeyValuePair<string, object>>();
            Predicate<string> subscriber2Predicate = telemetryName => (telemetryName == "DataForSubscriber2");
            var subscriber2Oberserver = new ObserverToList<TelemData>(subscriber2Result);

            // Get two subscribers going.  
            using (var subscription1 = TelemetryListener.DefaultListener.Subscribe(subscriber1Oberserver, subscriber1Predicate))
            {
                using (var subscription2 = TelemetryListener.DefaultListener.Subscribe(subscriber2Oberserver, subscriber2Predicate))
                {
                    // Things that neither subscribe to get filtered out. 
                    if (TelemetryListener.DefaultListener.IsEnabled("DataToFilterOut"))
                        TelemetryListener.DefaultListener.WriteTelemetry("DataToFilterOut", -1);

                    Assert.Equal(0, subscriber1Result.Count);
                    Assert.Equal(0, subscriber2Result.Count);

                    /****************************************************/
                    // If a Source does not use the IsEnabled, then every subscriber gets it.  
                    subscriber1Result.Clear();
                    subscriber2Result.Clear();
                    TelemetryListener.DefaultListener.WriteTelemetry("UnfilteredData", 3);

                    Assert.Equal(1, subscriber1Result.Count);
                    Assert.Equal("UnfilteredData", subscriber1Result[0].Key);
                    Assert.Equal(3, (int)subscriber1Result[0].Value);

                    Assert.Equal(1, subscriber2Result.Count);
                    Assert.Equal("UnfilteredData", subscriber2Result[0].Key);
                    Assert.Equal(3, (int)subscriber2Result[0].Value);

                    /****************************************************/
                    // Filters not filter out everything, they are just a performance optimization.  
                    // Here you actually get more than you want even though you use a filter 
                    subscriber1Result.Clear();
                    subscriber2Result.Clear();
                    if (TelemetryListener.DefaultListener.IsEnabled("DataForSubscriber1"))
                        TelemetryListener.DefaultListener.WriteTelemetry("DataForSubscriber1", 1);

                    Assert.Equal(1, subscriber1Result.Count);
                    Assert.Equal("DataForSubscriber1", subscriber1Result[0].Key);
                    Assert.Equal(1, (int)subscriber1Result[0].Value);

                    // Subscriber 2 happens to get it 
                    Assert.Equal(1, subscriber2Result.Count);
                    Assert.Equal("DataForSubscriber1", subscriber2Result[0].Key);
                    Assert.Equal(1, (int)subscriber2Result[0].Value);

                    /****************************************************/
                    subscriber1Result.Clear();
                    subscriber2Result.Clear();
                    if (TelemetryListener.DefaultListener.IsEnabled("DataForSubscriber2"))
                        TelemetryListener.DefaultListener.WriteTelemetry("DataForSubscriber2", 2);

                    // Subscriber 1 happens to get it 
                    Assert.Equal(1, subscriber1Result.Count);
                    Assert.Equal("DataForSubscriber2", subscriber1Result[0].Key);
                    Assert.Equal(2, (int)subscriber1Result[0].Value);

                    Assert.Equal(1, subscriber2Result.Count);
                    Assert.Equal("DataForSubscriber2", subscriber2Result[0].Key);
                    Assert.Equal(2, (int)subscriber2Result[0].Value);
                }   // subscriber2 drops out

                /*********************************************************************/
                /* Only Subscriber 1 is left */
                /*********************************************************************/

                // Things that neither subscribe to get filtered out. 
                subscriber1Result.Clear();
                subscriber2Result.Clear();
                if (TelemetryListener.DefaultListener.IsEnabled("DataToFilterOut"))
                    TelemetryListener.DefaultListener.WriteTelemetry("DataToFilterOut", -1);

                Assert.Equal(0, subscriber1Result.Count);
                Assert.Equal(0, subscriber2Result.Count);

                /****************************************************/
                // If a Source does not use the IsEnabled, then every subscriber gets it.  
                subscriber1Result.Clear();
                TelemetryListener.DefaultListener.WriteTelemetry("UnfilteredData", 3);

                Assert.Equal(1, subscriber1Result.Count);
                Assert.Equal("UnfilteredData", subscriber1Result[0].Key);
                Assert.Equal(3, (int)subscriber1Result[0].Value);

                // Subscriber 2 has dropped out.
                Assert.Equal(0, subscriber2Result.Count);

                /****************************************************/
                // Filters not filter out everything, they are just a performance optimization.  
                // Here you actually get more than you want even though you use a filter 
                subscriber1Result.Clear();
                if (TelemetryListener.DefaultListener.IsEnabled("DataForSubscriber1"))
                    TelemetryListener.DefaultListener.WriteTelemetry("DataForSubscriber1", 1);

                Assert.Equal(1, subscriber1Result.Count);
                Assert.Equal("DataForSubscriber1", subscriber1Result[0].Key);
                Assert.Equal(1, (int)subscriber1Result[0].Value);

                // Subscriber 2 has dropped out.
                Assert.Equal(0, subscriber2Result.Count);

                /****************************************************/
                subscriber1Result.Clear();
                if (TelemetryListener.DefaultListener.IsEnabled("DataForSubscriber2"))
                    TelemetryListener.DefaultListener.WriteTelemetry("DataForSubscriber2", 2);

                // Subscriber 1 filters
                Assert.Equal(0, subscriber1Result.Count);
                // Subscriber 2 has dropped out
                Assert.Equal(0, subscriber2Result.Count);
            } // subscriber1 drops out  

            /*********************************************************************/
            /* No Subscribers are left */
            /*********************************************************************/

            // Things that neither subscribe to get filtered out. 
            subscriber1Result.Clear();
            subscriber2Result.Clear();
            if (TelemetryListener.DefaultListener.IsEnabled("DataToFilterOut"))
                TelemetryListener.DefaultListener.WriteTelemetry("DataToFilterOut", -1);

            Assert.Equal(0, subscriber1Result.Count);
            Assert.Equal(0, subscriber2Result.Count);

            /****************************************************/
            // If a Source does not use the IsEnabled, then every subscriber gets it.  

            TelemetryListener.DefaultListener.WriteTelemetry("UnfilteredData", 3);

            // No one subscribing
            Assert.Equal(0, subscriber1Result.Count);
            Assert.Equal(0, subscriber2Result.Count);

            /****************************************************/
            // Filters not filter out everything, they are just a performance optimization.  
            // Here you actually get more than you want even though you use a filter 
            if (TelemetryListener.DefaultListener.IsEnabled("DataForSubscriber1"))
                TelemetryListener.DefaultListener.WriteTelemetry("DataForSubscriber1", 1);

            // No one subscribing
            Assert.Equal(0, subscriber1Result.Count);
            Assert.Equal(0, subscriber2Result.Count);

            /****************************************************/
            if (TelemetryListener.DefaultListener.IsEnabled("DataForSubscriber2"))
                TelemetryListener.DefaultListener.WriteTelemetry("DataForSubscriber2", 2);

            // No one subscribing
            Assert.Equal(0, subscriber1Result.Count);
            Assert.Equal(0, subscriber2Result.Count);
        }

        /// <summary>
        /// Stresses the Subscription routine by having many threads subscribe and
        /// unsubscribe concurrently
        /// </summary>
        [Fact]
        public void MultiSubscriberStress()
        {
            var random = new Random();
            //  Beat on the default listener by subscribing and unsubscribing on many threads simultaneously. 
            var factory = new TaskFactory();

            // To the whole stress test 10 times.  This keeps the task array size needed down while still
            // having lots of concurrency.   
            for (int j = 0; j < 20; j++)
            {
                // Spawn off lots of concurrent activity
                var tasks = new Task[1000];
                for (int i = 0; i < tasks.Length; i++)
                {
                    tasks[i] = factory.StartNew(delegate (object taskData)
                    {
                        int taskNum = (int)taskData;
                        var taskName = "Task" + taskNum;
                        var result = new List<KeyValuePair<string, object>>();
                        Predicate<string> predicate = (name) => name == taskName;
                        Predicate<KeyValuePair<string, object>> filter = (keyValue) => keyValue.Key == taskName;

                        // set up the observer to only see events set with the task name as the telemetry nname.  
                        var observer = new ObserverToList<TelemData>(result, filter, taskName);
                        using (TelemetryListener.DefaultListener.Subscribe(observer, predicate))
                        {
                            TelemetrySource.DefaultSource.WriteTelemetry(taskName, taskNum);

                            Assert.Equal(1, result.Count);
                            Assert.Equal(taskName, result[0].Key);
                            Assert.Equal(taskNum, result[0].Value);

                            // Spin a bit randomly.  This mixes of the lifetimes of the subscriptions and makes it 
                            // more stressful 
                            var cnt = random.Next(10, 100) * 1000;
                            while (0 < --cnt)
                                GC.KeepAlive("");
                        }   // Unsubscribe

                        // Send the telemetry again, to see if it now does NOT come through (count remains unchanged). 
                        TelemetrySource.DefaultSource.WriteTelemetry(taskName, -1);
                        Assert.Equal(1, result.Count);
                    }, i);
                }
                Task.WaitAll(tasks);
            }
        }

        /// <summary>
        /// Tests if as we create new TelemetryListerns, we get callbacks for them 
        /// </summary>
        [Fact]
        public void AllListenersAddRemove()
        {
            // This callback will return the listener that happens on the callback  
            TelemetryListener returnedListener = null;
            Action<TelemetryListener> onNewListener = delegate (TelemetryListener listener)
            {
                Assert.Null(returnedListener);
                Assert.NotNull(listener);
                returnedListener = listener;
            };

            // Subscribe, which delivers catch-up event for the Default listener 
            using (var allListenerSubscription = TelemetryListener.AllListeners.Subscribe(MakeObserver(onNewListener)))
            {
                Assert.Equal(TelemetryListener.DefaultListener, returnedListener);
                returnedListener = null;
            }   // Now we unsubscribe

            // Create an dispose a listener, but we won't get a callback for it.  
            using (new TelemetryListener("TestListen"))
            { }

            Assert.Null(returnedListener);          // No callback was made 

            // Resubscribe  
            using (var allListenerSubscription = TelemetryListener.AllListeners.Subscribe(MakeObserver(onNewListener)))
            {

                Assert.Equal(TelemetryListener.DefaultListener, returnedListener);
                returnedListener = null;

                // add two new subscribers
                using (var listener1 = new TelemetryListener("TestListen1"))
                {
                    Assert.Equal(listener1.Name, "TestListen1");
                    Assert.Equal(listener1, returnedListener);
                    returnedListener = null;

                    using (var listener2 = new TelemetryListener("TestListen2"))
                    {
                        Assert.Equal(listener2.Name, "TestListen2");
                        Assert.Equal(listener2, returnedListener);
                        returnedListener = null;
                    }   // Dispose of listener2
                }   // Dispose of listener1

            } // Unsubscribe 

            // Check that we are back to just the DefaultListener. 
            using (var allListenerSubscription = TelemetryListener.AllListeners.Subscribe(MakeObserver(onNewListener)))
            {
                Assert.Equal(TelemetryListener.DefaultListener, returnedListener);
                returnedListener = null;
            }   // cleanup 
        }

        /// <summary>
        /// Tests that the 'catchupList' of active listeners is accurate even as we 
        /// add and remove TelemetryListeners randomly.  
        /// </summary>
        [Fact]
        public void AllListenersCheckCatchupList()
        {
            var expected = new List<TelemetryListener>();
            var list = GetActiveNonDefaultListeners();
            Assert.Equal(list, expected);

            for (int i = 0; i < 50; i++)
            {
                expected.Insert(0, (new TelemetryListener("TestListener" + i)));
                list = GetActiveNonDefaultListeners();
                Assert.Equal(list, expected);
            }

            // Remove the element randomly.  
            var random = new Random(0);
            while (0 < expected.Count)
            {
                var toRemoveIdx = random.Next(0, expected.Count - 1);     // Always leave the Default listener.  
                var toRemoveListener = expected[toRemoveIdx];
                toRemoveListener.Dispose();     // Kill it (which removes it from the list)
                expected.RemoveAt(toRemoveIdx);
                list = GetActiveNonDefaultListeners();
                Assert.Equal(list.Count, expected.Count);
                Assert.Equal(list, expected);
            }
        }

        /// <summary>
        /// Stresses the AllListeners by having many threads be added and removed.
        /// </summary>
        [Fact]
        public void AllSubscriberStress()
        {
            var list = GetActiveNonDefaultListeners();
            Assert.Equal(0, list.Count);

            var factory = new TaskFactory();

            // To the whole stress test 10 times.  This keeps the task array size needed down while still
            // having lots of concurrency.   
            for (int k = 0; k < 10; k++)
            {
                // TODO FIX NOW:  Task[1] should be Task[100] but it fails.  
                var tasks = new Task[1];
                for (int i = 0; i < tasks.Length; i++)
                {
                    tasks[i] = (factory.StartNew(delegate ()
                    {
                        // Create a set of TelemetryListeners (which add themselves to the AllListeners list. 
                        var listeners = new List<TelemetryListener>();
                        for (int j = 0; j < 100; j++)
                            listeners.Insert(0, (new TelemetryListener("Task " + i + " TestListener" + j)));

                        // They are all in the list
                        list = GetActiveNonDefaultListeners();
                        foreach (var listener in listeners)
                            Assert.Contains(listener, list);

                        // Dispose them all, first the even then the odd, just to mix it up and be more stressful.  
                        for (int j = 0; j < listeners.Count; j += 2)      // Even
                            listeners[j].Dispose();
                        for (int j = 1; j < listeners.Count; j += 2)      // odd
                            listeners[j].Dispose();


                        // And now they are not in the list.  
                        list = GetActiveNonDefaultListeners();
                        foreach (var listener in listeners)
                            Assert.DoesNotContain(listener, list);
                    }));
                }
                // Wait for all the tasks to finish.  
                Task.WaitAll(tasks);
            }

            // There should be no listeners left.  
            list = GetActiveNonDefaultListeners();
            Assert.Equal(0, list.Count);
        }

        [Fact]
        public void DoubleDisposeOfListener()
        {
            var listener = new TelemetryListener("MyListener");
            int completionCount = 0;

            IDisposable subscription = listener.Subscribe(MakeObserver<KeyValuePair<string, object>>(_ => { }, () => completionCount++));

            listener.Dispose();
            listener.Dispose();

            subscription.Dispose();
            subscription.Dispose();

            Assert.Equal(1, completionCount);
        }

        [Fact]
        public void ListenerToString()
        {
            string name = Guid.NewGuid().ToString();
            using (var listener = new TelemetryListener(name))
            {
                Assert.Equal(name, listener.ToString());
            }
        }

        [Fact]
        public void DisposeAllListenerSubscriptionInSameOrderSubscribed()
        {
            int count1 = 0, count2 = 0, count3 = 0;
            IDisposable sub1 = TelemetryListener.AllListeners.Subscribe(MakeObserver<TelemetryListener>(onCompleted: () => count1++));
            IDisposable sub2 = TelemetryListener.AllListeners.Subscribe(MakeObserver<TelemetryListener>(onCompleted: () => count2++));
            IDisposable sub3 = TelemetryListener.AllListeners.Subscribe(MakeObserver<TelemetryListener>(onCompleted: () => count3++));

            Assert.Equal(0, count1);
            Assert.Equal(0, count2);
            Assert.Equal(0, count3);

            sub1.Dispose();
            Assert.Equal(1, count1);
            Assert.Equal(0, count2);
            Assert.Equal(0, count3);
            sub1.Dispose(); // dispose again just to make sure nothing bad happens

            sub2.Dispose();
            Assert.Equal(1, count1);
            Assert.Equal(1, count2);
            Assert.Equal(0, count3);
            sub2.Dispose();

            sub3.Dispose();
            Assert.Equal(1, count1);
            Assert.Equal(1, count2);
            Assert.Equal(1, count3);
            sub3.Dispose();
        }

        #region Helpers 
        /// <summary>
        /// Returns the list of active telemetry listeners.  
        /// </summary>
        /// <returns></returns>
        private static List<TelemetryListener> GetActiveNonDefaultListeners()
        {
            var ret = new List<TelemetryListener>();
            var seenDefault = false;
            Action<TelemetryListener> onNewListener = delegate (TelemetryListener listener)
            {
                if (listener == TelemetryListener.DefaultListener)
                {
                    Assert.False(seenDefault);
                    seenDefault = true;
                }
                else
                    ret.Add(listener);
            };

            // Subscribe, which gives you the list
            using (var allListenerSubscription = TelemetryListener.AllListeners.Subscribe(MakeObserver(onNewListener)))
            {
                Assert.True(seenDefault);
            } // Unsubscribe to remove side effects.  
            return ret;
        }

        /// <summary>
        /// Used to make an observer out of a action delegate. 
        /// </summary>
        private static IObserver<T> MakeObserver<T>(
            Action<T> onNext = null, Action onCompleted = null)
        {
            return new Observer<T>(onNext, onCompleted);
        }

        /// <summary>
        /// Used in the implementation of MakeObserver.  
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private class Observer<T> : IObserver<T>
        {
            public Observer(Action<T> onNext, Action onCompleted)
            {
                _onNext = onNext ?? new Action<T>(_ => { });
                _onCompleted = onCompleted ?? new Action(() => { });
            }

            public void OnCompleted() { _onCompleted(); }
            public void OnError(Exception error) { }
            public void OnNext(T value) { _onNext(value); }

            private Action<T> _onNext;
            private Action _onCompleted;
        }
        #endregion 
    }

    // Takes an IObserver and returns a List<T> that are the elements observed.
    // Will assert on error and 'Completed' is set if the 'OnCompleted' callback
    // is issued.  
    internal class ObserverToList<T> : IObserver<T>
    {
        public ObserverToList(List<T> output, Predicate<T> filter = null, string name = null)
        {
            _output = output;
            _output.Clear();
            _filter = filter;
            _name = name;
        }

        public bool Completed { get; private set; }

        #region private

        public void OnCompleted()
        {
            Completed = true;
        }

        public void OnError(Exception error)
        {
            Assert.True(false, "Error happened on IObserver");
        }

        public void OnNext(T value)
        {
            Assert.False(Completed);
            if (_filter == null || _filter(value))
                _output.Add(value);
        }

        private List<T> _output;
        private Predicate<T> _filter;
        private string _name;  // for debugging 
        #endregion
    }

    /// <summary>
    /// Trivial class used for payloads.  (Usually anonymous types are used.  
    /// </summary>
    internal class Payload
    {
        public string Name { get; set; }
        public int Id { get; set; }
    }
}
