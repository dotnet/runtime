// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TelemData = System.Collections.Generic.KeyValuePair<string, object>;
using Xunit;

namespace System.Diagnostics.Tests
{
    /// <summary>
    /// Tests for DiagnosticSource and DiagnosticListener
    /// </summary>
    public class DiagnosticSourceTest
    {
        /// <summary>
        /// Trivial example of passing an integer
        /// </summary>
        [Fact]
        public void IntPayload()
        {
            using (DiagnosticListener listener = new DiagnosticListener("TestingIntPayload"))
            {
                DiagnosticSource source = listener;
                var result = new List<KeyValuePair<string, object>>();
                var observer = new ObserverToList<TelemData>(result);

                using (listener.Subscribe(new ObserverToList<TelemData>(result)))
                {
                    listener.Write("IntPayload", 5);
                    Assert.Equal(1, result.Count);
                    Assert.Equal("IntPayload", result[0].Key);
                    Assert.Equal(5, result[0].Value);
                }   // unsubscribe

                // Make sure that after unsubscribing, we don't get more events.
                source.Write("IntPayload", 5);
                Assert.Equal(1, result.Count);
            }
        }

        /// <summary>
        /// Trivial example of passing an object
        /// </summary>
        [Fact]
        public void ObjectPayload()
        {
            using (DiagnosticListener listener = new DiagnosticListener("TestingObjectPayload"))
            {
                DiagnosticSource source = listener;
                var result = new List<KeyValuePair<string, object>>();
                var observer = new ObserverToList<TelemData>(result);

                using (listener.Subscribe(new ObserverToList<TelemData>(result)))
                {
                    object o = new object();

                    listener.Write("ObjectPayload", o);
                    Assert.Equal(1, result.Count);
                    Assert.Equal("ObjectPayload", result[0].Key);
                    Assert.Same(o, result[0].Value);
                }   // unsubscribe

                // Make sure that after unsubscribing, we don't get more events.
                source.Write("ObjectPayload", new object());
                Assert.Equal(1, result.Count);
            }
        }

        /// <summary>
        /// slightly less trivial of passing a structure with a couple of fields
        /// </summary>
        [Fact]
        public void StructPayload()
        {
            using (DiagnosticListener listener = new DiagnosticListener("TestingStructPayload"))
            {
                DiagnosticSource source = listener;
                var result = new List<KeyValuePair<string, object>>();
                using (listener.Subscribe(new ObserverToList<TelemData>(result)))
                {
                    source.Write("StructPayload", new Payload() { Name = "Hi", Id = 67 });
                    Assert.Equal(1, result.Count);
                    Assert.Equal("StructPayload", result[0].Key);
                    var payload = (Payload)result[0].Value;

                    Assert.Equal(67, payload.Id);
                    Assert.Equal("Hi", payload.Name);
                }

                source.Write("StructPayload", new Payload() { Name = "Hi", Id = 67 });
                Assert.Equal(1, result.Count);
            }
        }

        /// <summary>
        /// Tests the IObserver OnCompleted callback.
        /// </summary>
        [Fact]
        public void Completed()
        {
            var result = new List<KeyValuePair<string, object>>();
            var observer = new ObserverToList<TelemData>(result);
            var listener = new DiagnosticListener("TestingCompleted");
            var subscription = listener.Subscribe(observer);

            listener.Write("IntPayload", 5);
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

            listener.Write("IntPayload", 5);
            Assert.Equal(1, result.Count);
        }

        /// <summary>
        /// Simple tests for the IsEnabled method.
        /// </summary>
        [Fact]
        public void BasicIsEnabled()
        {
            using (DiagnosticListener listener = new DiagnosticListener("TestingBasicIsEnabled"))
            {
                DiagnosticSource source = listener;
                var result = new List<KeyValuePair<string, object>>();

                bool seenUninteresting = false;
                bool seenStructPayload = false;
                Predicate<string> predicate = delegate (string name)
                {
                    if (name == "Uninteresting")
                        seenUninteresting = true;
                    if (name == "StructPayload")
                        seenStructPayload = true;

                    return name == "StructPayload";
                };

                // Assert.False(listener.IsEnabled());  Since other things might turn on all DiagnosticSources, we can't ever test that it is not enabled.
                using (listener.Subscribe(new ObserverToList<TelemData>(result), predicate))
                {
                    Assert.False(source.IsEnabled("Uninteresting"));
                    Assert.False(source.IsEnabled("Uninteresting", "arg1", "arg2"));
                    Assert.True(source.IsEnabled("StructPayload"));
                    Assert.True(source.IsEnabled("StructPayload", "arg1", "arg2"));
                    Assert.True(seenUninteresting);
                    Assert.True(seenStructPayload);
                    Assert.True(listener.IsEnabled());
                }
            }
        }

        /// <summary>
        /// Simple tests for the IsEnabled method.
        /// </summary>
        [Fact]
        public void IsEnabledMultipleArgs()
        {
            using (DiagnosticListener listener = new DiagnosticListener("TestingIsEnabledMultipleArgs"))
            {
                DiagnosticSource source = listener;
                var result = new List<KeyValuePair<string, object>>();
                Func<string, object, object, bool> isEnabled = (name, arg1, arg2) =>
                {
                    if (arg1 != null)
                        return (bool) arg1;
                    if (arg2 != null)
                        return (bool) arg2;
                    return true;
                };

                using (listener.Subscribe(new ObserverToList<TelemData>(result), isEnabled))
                {
                    Assert.True(source.IsEnabled("event"));
                    Assert.True(source.IsEnabled("event", null, null));
                    Assert.True(source.IsEnabled("event", null, true));

                    Assert.False(source.IsEnabled("event", false, false));
                    Assert.False(source.IsEnabled("event", false, null));
                    Assert.False(source.IsEnabled("event", null, false));
                }
            }
        }

        /// <summary>
        /// Test if it works when you have two subscribers active simultaneously
        /// </summary>
        [Fact]
        public void MultiSubscriber()
        {
            using (DiagnosticListener listener = new DiagnosticListener("TestingMultiSubscriber"))
            {
                DiagnosticSource source = listener;
                var subscriber1Result = new List<KeyValuePair<string, object>>();
                Predicate<string> subscriber1Predicate = name => (name == "DataForSubscriber1");
                var subscriber1Observer = new ObserverToList<TelemData>(subscriber1Result);

                var subscriber2Result = new List<KeyValuePair<string, object>>();
                Predicate<string> subscriber2Predicate = name => (name == "DataForSubscriber2");
                var subscriber2Observer = new ObserverToList<TelemData>(subscriber2Result);

                // Get two subscribers going.
                using (var subscription1 = listener.Subscribe(subscriber1Observer, subscriber1Predicate))
                {
                    using (var subscription2 = listener.Subscribe(subscriber2Observer, subscriber2Predicate))
                    {
                        // Things that neither subscribe to get filtered out.
                        if (listener.IsEnabled("DataToFilterOut"))
                            listener.Write("DataToFilterOut", -1);

                        Assert.Equal(0, subscriber1Result.Count);
                        Assert.Equal(0, subscriber2Result.Count);

                        /****************************************************/
                        // If a Source does not use the IsEnabled, then every subscriber gets it.
                        subscriber1Result.Clear();
                        subscriber2Result.Clear();
                        listener.Write("UnfilteredData", 3);

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
                        if (listener.IsEnabled("DataForSubscriber1"))
                            listener.Write("DataForSubscriber1", 1);

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
                        if (listener.IsEnabled("DataForSubscriber2"))
                            listener.Write("DataForSubscriber2", 2);

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
                    if (listener.IsEnabled("DataToFilterOut"))
                        listener.Write("DataToFilterOut", -1);

                    Assert.Equal(0, subscriber1Result.Count);
                    Assert.Equal(0, subscriber2Result.Count);

                    /****************************************************/
                    // If a Source does not use the IsEnabled, then every subscriber gets it.
                    subscriber1Result.Clear();
                    listener.Write("UnfilteredData", 3);

                    Assert.Equal(1, subscriber1Result.Count);
                    Assert.Equal("UnfilteredData", subscriber1Result[0].Key);
                    Assert.Equal(3, (int)subscriber1Result[0].Value);

                    // Subscriber 2 has dropped out.
                    Assert.Equal(0, subscriber2Result.Count);

                    /****************************************************/
                    // Filters not filter out everything, they are just a performance optimization.
                    // Here you actually get more than you want even though you use a filter
                    subscriber1Result.Clear();
                    if (listener.IsEnabled("DataForSubscriber1"))
                        listener.Write("DataForSubscriber1", 1);

                    Assert.Equal(1, subscriber1Result.Count);
                    Assert.Equal("DataForSubscriber1", subscriber1Result[0].Key);
                    Assert.Equal(1, (int)subscriber1Result[0].Value);

                    // Subscriber 2 has dropped out.
                    Assert.Equal(0, subscriber2Result.Count);

                    /****************************************************/
                    subscriber1Result.Clear();
                    if (listener.IsEnabled("DataForSubscriber2"))
                        listener.Write("DataForSubscriber2", 2);

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
                if (listener.IsEnabled("DataToFilterOut"))
                    listener.Write("DataToFilterOut", -1);

                Assert.Equal(0, subscriber1Result.Count);
                Assert.Equal(0, subscriber2Result.Count);

                /****************************************************/
                // If a Source does not use the IsEnabled, then every subscriber gets it.

                listener.Write("UnfilteredData", 3);

                // No one subscribing
                Assert.Equal(0, subscriber1Result.Count);
                Assert.Equal(0, subscriber2Result.Count);

                /****************************************************/
                // Filters not filter out everything, they are just a performance optimization.
                // Here you actually get more than you want even though you use a filter
                if (listener.IsEnabled("DataForSubscriber1"))
                    listener.Write("DataForSubscriber1", 1);

                // No one subscribing
                Assert.Equal(0, subscriber1Result.Count);
                Assert.Equal(0, subscriber2Result.Count);

                /****************************************************/
                if (listener.IsEnabled("DataForSubscriber2"))
                    listener.Write("DataForSubscriber2", 2);

                // No one subscribing
                Assert.Equal(0, subscriber1Result.Count);
                Assert.Equal(0, subscriber2Result.Count);
            }
        }

        /// <summary>
        /// Stresses the Subscription routine by having many threads subscribe and
        /// unsubscribe concurrently
        /// </summary>
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void MultiSubscriberStress()
        {
            using (DiagnosticListener listener = new DiagnosticListener("MultiSubscriberStressTest"))
            {
                DiagnosticSource source = listener;

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

                            // set up the observer to only see events set with the task name as the name.
                            var observer = new ObserverToList<TelemData>(result, filter, taskName);
                            using (listener.Subscribe(observer, predicate))
                            {
                                source.Write(taskName, taskNum);

                                Assert.Equal(1, result.Count);
                                Assert.Equal(taskName, result[0].Key);
                                Assert.Equal(taskNum, result[0].Value);

                                // Spin a bit randomly.  This mixes of the lifetimes of the subscriptions and makes it
                                // more stressful
                                var cnt = random.Next(10, 100) * 1000;
                                while (0 < --cnt)
                                    GC.KeepAlive("");
                            }   // Unsubscribe

                            // Send the notification again, to see if it now does NOT come through (count remains unchanged).
                            source.Write(taskName, -1);
                            Assert.Equal(1, result.Count);
                        }, i);
                    }
                    Task.WaitAll(tasks);
                }
            }
        }

        /// <summary>
        /// Tests if as we create new DiagnosticListerns, we get callbacks for them
        /// </summary>
        [Fact]
        public void AllListenersAddRemove()
        {
            using (DiagnosticListener listener = new DiagnosticListener("TestListen0"))
            {
                DiagnosticSource source = listener;

                // This callback will return the listener that happens on the callback
                DiagnosticListener returnedListener = null;
                Action<DiagnosticListener> onNewListener = delegate (DiagnosticListener listen)
                {
                    // Other tests can be running concurrently with this test, which will make
                    // this callback fire for those listeners as well.   We only care about
                    // the Listeners we generate here so ignore any that d
                    if (!listen.Name.StartsWith("TestListen"))
                        return;

                    Assert.Null(returnedListener);
                    Assert.NotNull(listen);
                    returnedListener = listen;
                };

                // Subscribe, which delivers catch-up event for the Default listener
                using (var allListenerSubscription = DiagnosticListener.AllListeners.Subscribe(MakeObserver(onNewListener)))
                {
                    Assert.Equal(listener, returnedListener);
                    returnedListener = null;
                }   // Now we unsubscribe

                // Create an dispose a listener, but we won't get a callback for it.
                using (new DiagnosticListener("TestListen"))
                { }

                Assert.Null(returnedListener);          // No callback was made

                // Resubscribe
                using (var allListenerSubscription = DiagnosticListener.AllListeners.Subscribe(MakeObserver(onNewListener)))
                {

                    Assert.Equal(listener, returnedListener);
                    returnedListener = null;

                    // add two new subscribers
                    using (var listener1 = new DiagnosticListener("TestListen1"))
                    {
                        Assert.Equal("TestListen1", listener1.Name);
                        Assert.Equal(listener1, returnedListener);
                        returnedListener = null;

                        using (var listener2 = new DiagnosticListener("TestListen2"))
                        {
                            Assert.Equal("TestListen2", listener2.Name);
                            Assert.Equal(listener2, returnedListener);
                            returnedListener = null;
                        }   // Dispose of listener2
                    }   // Dispose of listener1

                } // Unsubscribe

                // Check that we are back to just the DefaultListener.
                using (var allListenerSubscription = DiagnosticListener.AllListeners.Subscribe(MakeObserver(onNewListener)))
                {
                    Assert.Equal(listener, returnedListener);
                    returnedListener = null;
                }   // cleanup
            }
        }

        /// <summary>
        /// Tests that the 'catchupList' of active listeners is accurate even as we
        /// add and remove DiagnosticListeners randomly.
        /// </summary>
        [Fact]
        public void AllListenersCheckCatchupList()
        {
            var expected = new List<DiagnosticListener>();
            var list = GetActiveListenersWithPrefix("TestListener");
            Assert.Equal(list, expected);

            for (int i = 0; i < 50; i++)
            {
                expected.Insert(0, (new DiagnosticListener("TestListener" + i)));
                list = GetActiveListenersWithPrefix("TestListener");
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
                list = GetActiveListenersWithPrefix("TestListener");
                Assert.Equal(list.Count, expected.Count);
                Assert.Equal(list, expected);
            }
        }

        /// <summary>
        /// Stresses the AllListeners by having many threads be adding and removing.
        /// </summary>
        [OuterLoop]
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(100, 100)] // run multiple times to stress it further
        [InlineData(100, 101)]
        [InlineData(100, 102)]
        [InlineData(100, 103)]
        [InlineData(100, 104)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/79906", TestRuntimes.Mono)]
        public void AllSubscriberStress(int numThreads, int numListenersPerThread)
        {
            // No listeners have been created yet
            Assert.Equal(0, GetActiveListenersWithPrefix(nameof(AllSubscriberStress)).Count);

            // Run lots of threads to add/remove listeners
            Task.WaitAll(Enumerable.Range(0, numThreads).Select(i => Task.Factory.StartNew(delegate
            {
                // Create a set of DiagnosticListeners, which add themselves to the AllListeners list.
                var listeners = new List<DiagnosticListener>(numListenersPerThread);
                for (int j = 0; j < numListenersPerThread; j++)
                {
                    var listener = new DiagnosticListener($"{nameof(AllSubscriberStress)}_Task {i} TestListener{j}");
                    listeners.Add(listener);
                }

                // They are all in the list.
                List<DiagnosticListener> list = GetActiveListenersWithPrefix(nameof(AllSubscriberStress));
                Assert.All(listeners, listener => Assert.Contains(listener, list));

                // Dispose them all, first the even then the odd, just to mix it up and be more stressful.
                for (int j = 0; j < listeners.Count; j += 2) // even
                    listeners[j].Dispose();
                for (int j = 1; j < listeners.Count; j += 2) // odd
                    listeners[j].Dispose();

                // None should be left in the list
                list = GetActiveListenersWithPrefix(nameof(AllSubscriberStress));
                Assert.All(listeners, listener => Assert.DoesNotContain(listener, list));
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray());

            // None of the created listeners should remain
            Assert.Equal(0, GetActiveListenersWithPrefix(nameof(AllSubscriberStress)).Count);
        }

        [Fact]
        public void DoubleDisposeOfListener()
        {
            var listener = new DiagnosticListener("TestingDoubleDisposeOfListener");
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
            using (var listener = new DiagnosticListener(name))
            {
                Assert.Equal(name, listener.ToString());
            }
        }

        [Fact]
        public void DisposeAllListenerSubscriptionInSameOrderSubscribed()
        {
            int count1 = 0, count2 = 0, count3 = 0;
            IDisposable sub1 = DiagnosticListener.AllListeners.Subscribe(MakeObserver<DiagnosticListener>(onCompleted: () => count1++));
            IDisposable sub2 = DiagnosticListener.AllListeners.Subscribe(MakeObserver<DiagnosticListener>(onCompleted: () => count2++));
            IDisposable sub3 = DiagnosticListener.AllListeners.Subscribe(MakeObserver<DiagnosticListener>(onCompleted: () => count3++));

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

        [Fact]
        public void SubscribeWithNullPredicate()
        {
            using (DiagnosticListener listener = new DiagnosticListener("TestingSubscribeWithNullPredicate"))
            {
                Predicate<string> predicate = null;
                using (listener.Subscribe(new ObserverToList<TelemData>(new List<KeyValuePair<string, object>>()), predicate))
                {
                    Assert.True(listener.IsEnabled("event"));
                    Assert.True(listener.IsEnabled("event", null));
                    Assert.True(listener.IsEnabled("event", "arg1"));
                    Assert.True(listener.IsEnabled("event", "arg1", "arg2"));
                }
            }

            using (DiagnosticListener listener = new DiagnosticListener("TestingSubscribeWithNullPredicate"))
            {
                DiagnosticSource source = listener;
                Func<string, object, object, bool> predicate = null;
                using (listener.Subscribe(new ObserverToList<TelemData>(new List<KeyValuePair<string, object>>()), predicate))
                {
                    Assert.True(source.IsEnabled("event"));
                    Assert.True(source.IsEnabled("event", null));
                    Assert.True(source.IsEnabled("event", "arg1"));
                    Assert.True(source.IsEnabled("event", "arg1", "arg2"));
                }
            }
        }

        [Fact]
        public void ActivityImportExport()
        {
            using (DiagnosticListener listener = new DiagnosticListener("TestingBasicIsEnabled"))
            {
                DiagnosticSource source = listener;
                var result = new List<KeyValuePair<string, object>>();

                bool seenPredicate = false;
                Func<string, object, object, bool> predicate = delegate (string name, object obj1, object obj2)
                {
                    seenPredicate = true;
                    return true;
                };

                Activity importerActivity = new Activity("activityImporter");
                object importer = "MyImporterObject";
                bool seenActivityImport = false;
                Action<Activity, object> activityImport = delegate (Activity activity, object payload)
                {
                    Assert.Equal(activity.GetHashCode(), importerActivity.GetHashCode());
                    Assert.Equal(importer, payload);
                    seenActivityImport = true;

                };

                Activity exporterActivity = new Activity("activityExporter");
                object exporter = "MyExporterObject";
                bool seenActivityExport = false;
                Action<Activity, object> activityExport = delegate (Activity activity, object payload)
                {
                    Assert.Equal(activity.GetHashCode(), exporterActivity.GetHashCode());
                    Assert.Equal(exporter, payload);
                    seenActivityExport = true;
                };

                // Use the Subscribe that allows you to hook the OnActivityImport and OnActivityExport calls.
                using (listener.Subscribe(new ObserverToList<TelemData>(result), predicate, activityImport, activityExport))
                {
                    if (listener.IsEnabled("IntPayload"))
                        listener.Write("IntPayload", 5);

                    Assert.True(seenPredicate);
                    Assert.Equal(1, result.Count);
                    Assert.Equal("IntPayload", result[0].Key);
                    Assert.Equal(5, result[0].Value);

                    listener.OnActivityImport(importerActivity, importer);
                    Assert.True(seenActivityImport);

                    listener.OnActivityExport(exporterActivity, exporter);
                    Assert.True(seenActivityExport);
                }
            }
        }

        #region Helpers
        /// <summary>
        /// Returns the list of active diagnostic listeners.
        /// </summary>
        /// <returns></returns>
        private static List<DiagnosticListener> GetActiveListenersWithPrefix(string prefix)
        {
            var ret = new List<DiagnosticListener>();
            Action<DiagnosticListener> onNewListener = delegate (DiagnosticListener listen)
            {
                if (listen.Name.StartsWith(prefix))
                    ret.Add(listen);
            };

            // Subscribe, which gives you the list
            using (var allListenerSubscription = DiagnosticListener.AllListeners.Subscribe(MakeObserver(onNewListener)))
            {
            } // Unsubscribe to remove side effects.
            return ret;
        }

        /// <summary>
        /// Used to make an observer out of an action delegate.
        /// </summary>
        public static IObserver<T> MakeObserver<T>(
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
            Assert.Fail("Error happened on IObserver");
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
