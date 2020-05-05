// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class ActivitySourceTests : IDisposable
    {
        [Fact]
        public void TestConstruction()
        {
            RemoteExecutor.Invoke(() => {
                using (ActivitySource as1 = new ActivitySource("Source1"))
                {
                    Assert.Equal("Source1", as1.Name);
                    Assert.Equal(String.Empty, as1.Version);
                    Assert.False(as1.HasListeners());
                    using (ActivitySource as2 =  new ActivitySource("Source2", "1.1.1.2"))
                    {
                        Assert.Equal("Source2", as2.Name);
                        Assert.Equal("1.1.1.2", as2.Version);
                        Assert.False(as2.HasListeners());
                    }
                }
            }).Dispose();
        }

        [Fact]
        public void TestStartActivityWithNoListener()
        {
            RemoteExecutor.Invoke(() => {
                using (ActivitySource aSource =  new ActivitySource("SourceActivity"))
                {
                    Assert.Equal("SourceActivity", aSource.Name);
                    Assert.Equal(string.Empty, aSource.Version);
                    Assert.False(aSource.HasListeners());

                    Activity current = Activity.Current;
                    using (Activity a1 = aSource.StartActivity("a1"))
                    {
                        // no listeners, we should get null activity.
                        Assert.Null(a1);
                        Assert.Equal(Activity.Current, current);
                    }
                }
            }).Dispose();
        }

        [Fact]
        public void TestActivityWithListenerNoActivityCreate()
        {
            RemoteExecutor.Invoke(() => {
                using (ActivitySource aSource =  new ActivitySource("SourceActivityListener"))
                {
                    Assert.False(aSource.HasListeners());

                    using (ActivityListener listener = new ActivityListener
                        {
                            ActivityStarted = activity => Assert.NotNull(activity),
                            ActivityStopped = activity => Assert.NotNull(activity),
                            ShouldListenTo = (activitySource) => object.ReferenceEquals(aSource, activitySource),
                            GetRequestedDataUsingParentId = (ref ActivityCreationOptions<string> activityOptions) => ActivityDataRequest.None,
                            GetRequestedDataUsingContext = (ref ActivityCreationOptions<ActivityContext> activityOptions) => ActivityDataRequest.None
                        }
                    )
                    {
                        ActivitySource.AddActivityListener(listener);
                        Assert.True(aSource.HasListeners());

                        // The listener is not allowing to create a new Activity.
                        Assert.Null(aSource.StartActivity("nullActivity"));
                    }
                }
            }).Dispose();
        }

        [Fact]
        public void TestActivityWithListenerActivityCreateAndAllDataRequested()
        {
            RemoteExecutor.Invoke(() => {
                using (ActivitySource aSource = new ActivitySource("SourceActivityListener"))
                {
                    int counter = 0;
                    Assert.False(aSource.HasListeners());

                    using (ActivityListener listener = new ActivityListener
                        {
                            ActivityStarted = activity => counter++,
                            ActivityStopped = activity => counter--,
                            ShouldListenTo = (activitySource) => object.ReferenceEquals(aSource, activitySource),
                            GetRequestedDataUsingParentId = (ref ActivityCreationOptions<string> activityOptions) => ActivityDataRequest.AllDataAndRecorded,
                            GetRequestedDataUsingContext = (ref ActivityCreationOptions<ActivityContext> activityOptions) => ActivityDataRequest.AllDataAndRecorded
                        }
                    )
                    {
                        ActivitySource.AddActivityListener(listener);

                        Assert.True(aSource.HasListeners());

                        using (Activity activity = aSource.StartActivity("AllDataRequestedActivity"))
                        {
                            Assert.NotNull(activity);
                            Assert.True(activity.IsAllDataRequested);
                            Assert.Equal(1, counter);

                            Assert.Equal(0, activity.Tags.Count());
                            Assert.Equal(0, activity.Baggage.Count());

                            Assert.True(object.ReferenceEquals(activity, activity.AddTag("key", "value")));
                            Assert.True(object.ReferenceEquals(activity, activity.AddBaggage("key", "value")));

                            Assert.Equal(1, activity.Tags.Count());
                            Assert.Equal(1, activity.Baggage.Count());

                            using (Activity activity1 = aSource.StartActivity("AllDataRequestedActivity1"))
                            {
                                Assert.NotNull(activity1);
                                Assert.True(activity1.IsAllDataRequested);
                                Assert.Equal(2, counter);

                                Assert.Equal(0, activity1.Links.Count());
                                Assert.Equal(0, activity1.Events.Count());
                                Assert.True(object.ReferenceEquals(activity1, activity1.AddEvent(new ActivityEvent("e1"))));
                                Assert.Equal(1, activity1.Events.Count());
                            }
                            Assert.Equal(1, counter);
                        }

                        Assert.Equal(0, counter);
                    }
                }
            }).Dispose();
        }

        [Fact]
        public void TestActivitySourceAttachedObject()
        {
            RemoteExecutor.Invoke(() => {
                // All Activities created through the constructor should have same source.
                Assert.True(object.ReferenceEquals(new Activity("a1").Source, new Activity("a2").Source));
                Assert.Equal("", new Activity("a3").Source.Name);
                Assert.Equal(string.Empty, new Activity("a4").Source.Version);

                using (ActivitySource aSource = new ActivitySource("SourceToTest", "1.2.3.4"))
                {
                    //Ensure at least we have a listener to allow Activity creation
                    using (ActivityListener listener = new ActivityListener
                        {
                            ActivityStarted = activity => Assert.NotNull(activity),
                            ActivityStopped = activity => Assert.NotNull(activity),
                            ShouldListenTo = (activitySource) => object.ReferenceEquals(aSource, activitySource),
                            GetRequestedDataUsingParentId = (ref ActivityCreationOptions<string> activityOptions) => ActivityDataRequest.AllData,
                            GetRequestedDataUsingContext = (ref ActivityCreationOptions<ActivityContext> activityOptions) => ActivityDataRequest.AllData
                        }
                    )
                    {
                        ActivitySource.AddActivityListener(listener);

                        using (Activity activity = aSource.StartActivity("ActivityToTest"))
                        {
                            Assert.True(object.ReferenceEquals(aSource, activity.Source));
                        }
                    }
                }
            }).Dispose();
        }

        [Fact]
        public void TestListeningToConstructedActivityEvents()
        {
            RemoteExecutor.Invoke(() => {
                int activityStartCount = 0;
                int activityStopCount  = 0;

                using (ActivityListener listener = new ActivityListener
                    {
                        ActivityStarted = activity => activityStartCount++,
                        ActivityStopped = activity => activityStopCount++,
                        ShouldListenTo = (activitySource) => activitySource.Name == "" && activitySource.Version == "",
                        GetRequestedDataUsingParentId = (ref ActivityCreationOptions<string> activityOptions) => ActivityDataRequest.AllData,
                        GetRequestedDataUsingContext = (ref ActivityCreationOptions<ActivityContext> activityOptions) => ActivityDataRequest.AllData
                    }
                )
                {
                    ActivitySource.AddActivityListener(listener);

                    Assert.Equal(0, activityStartCount);
                    Assert.Equal(0, activityStopCount);

                    using (Activity a1 = new Activity("a1"))
                    {
                        Assert.Equal(0, activityStartCount);
                        Assert.Equal(0, activityStopCount);

                        a1.Start();

                        Assert.Equal(1, activityStartCount);
                        Assert.Equal(0, activityStopCount);
                    }

                    Assert.Equal(1, activityStartCount);
                    Assert.Equal(1, activityStopCount);
                }

                // Ensure the listener is disposed
                using (Activity a2 = new Activity("a2"))
                {
                    a2.Start();

                    Assert.Equal(1, activityStartCount);
                    Assert.Equal(1, activityStopCount);
                }
            }).Dispose();
        }

        [Fact]
        public void TestExpectedListenersReturnValues()
        {
            RemoteExecutor.Invoke(() => {

                ActivitySource source = new ActivitySource("MultipleListenerSource");
                ActivityListener [] listeners = new ActivityListener[4];

                listeners[0] = new ActivityListener
                {
                    ActivityStarted = activity => Assert.NotNull(activity),
                    ActivityStopped = activity => Assert.NotNull(activity),
                    ShouldListenTo = (activitySource) => true,
                    GetRequestedDataUsingParentId = (ref ActivityCreationOptions<string> activityOptions) => ActivityDataRequest.None,
                    GetRequestedDataUsingContext = (ref ActivityCreationOptions<ActivityContext> activityOptions) => ActivityDataRequest.None
                };
                ActivitySource.AddActivityListener(listeners[0]);

                Assert.Null(source.StartActivity("a1"));

                listeners[1] = new ActivityListener
                {
                    ActivityStarted = activity => Assert.NotNull(activity),
                    ActivityStopped = activity => Assert.NotNull(activity),
                    ShouldListenTo = (activitySource) => true,
                    GetRequestedDataUsingParentId = (ref ActivityCreationOptions<string> activityOptions) => ActivityDataRequest.PropagationData,
                    GetRequestedDataUsingContext = (ref ActivityCreationOptions<ActivityContext> activityOptions) => ActivityDataRequest.PropagationData
                };
                ActivitySource.AddActivityListener(listeners[1]);

                using (Activity a2 = source.StartActivity("a2"))
                {
                    Assert.False(a2.IsAllDataRequested);
                    Assert.True((a2.ActivityTraceFlags & ActivityTraceFlags.Recorded) == 0);
                }

                listeners[2] = new ActivityListener
                {
                    ActivityStarted = activity => Assert.NotNull(activity),
                    ActivityStopped = activity => Assert.NotNull(activity),
                    ShouldListenTo = (activitySource) => true,
                    GetRequestedDataUsingParentId = (ref ActivityCreationOptions<string> activityOptions) => ActivityDataRequest.AllData,
                    GetRequestedDataUsingContext = (ref ActivityCreationOptions<ActivityContext> activityOptions) => ActivityDataRequest.AllData
                };
                ActivitySource.AddActivityListener(listeners[2]);

                using (Activity a3 = source.StartActivity("a3"))
                {
                    Assert.True(a3.IsAllDataRequested);
                    Assert.True((a3.ActivityTraceFlags & ActivityTraceFlags.Recorded) == 0);
                }

                listeners[3] = new ActivityListener
                {
                    ActivityStarted = activity => Assert.NotNull(activity),
                    ActivityStopped = activity => Assert.NotNull(activity),
                    ShouldListenTo = (activitySource) => true,
                    GetRequestedDataUsingParentId = (ref ActivityCreationOptions<string> activityOptions) => ActivityDataRequest.AllDataAndRecorded,
                    GetRequestedDataUsingContext = (ref ActivityCreationOptions<ActivityContext> activityOptions) => ActivityDataRequest.AllDataAndRecorded
                };
                ActivitySource.AddActivityListener(listeners[3]);

                using (Activity a4 = source.StartActivity("a4"))
                {
                    Assert.True(a4.IsAllDataRequested);
                    Assert.True((a4.ActivityTraceFlags & ActivityTraceFlags.Recorded) != 0, $"a4.ActivityTraceFlags failed: {a4.ActivityTraceFlags}");
                }

                foreach (IDisposable listener in listeners)
                {
                    listener.Dispose();
                }

                Assert.Null(source.StartActivity("a5"));
            }).Dispose();
        }

        [Fact]
        public void TestActivityCreationProperties()
        {
            RemoteExecutor.Invoke(() => {
                ActivitySource source = new ActivitySource("MultipleListenerSource");

                using (ActivityListener listener = new ActivityListener
                    {
                        ActivityStarted = activity => Assert.NotNull(activity),
                        ActivityStopped = activity => Assert.NotNull(activity),
                        ShouldListenTo = (activitySource) => true,
                        GetRequestedDataUsingParentId = (ref ActivityCreationOptions<string> activityOptions) => ActivityDataRequest.AllData,
                        GetRequestedDataUsingContext = (ref ActivityCreationOptions<ActivityContext> activityOptions) => ActivityDataRequest.AllData
                    }
                )
                {
                    ActivitySource.AddActivityListener(listener);

                    ActivityContext ctx = new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded, "key0-value0");

                    List<ActivityLink> links = new List<ActivityLink>();
                    links.Add(new ActivityLink(new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.None, "key1-value1")));
                    links.Add(new ActivityLink(new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.None, "key2-value2")));

                    List<KeyValuePair<string, string>> attributes = new List<KeyValuePair<string, string>>();
                    attributes.Add(new KeyValuePair<string, string>("tag1", "tagValue1"));
                    attributes.Add(new KeyValuePair<string, string>("tag2", "tagValue2"));
                    attributes.Add(new KeyValuePair<string, string>("tag3", "tagValue3"));

                    using (Activity activity = source.StartActivity("a1", ActivityKind.Client, ctx, attributes, links))
                    {
                        Assert.NotNull(activity);
                        Assert.Equal("a1", activity.OperationName);
                        Assert.Equal("a1", activity.DisplayName);
                        Assert.Equal(ActivityKind.Client, activity.Kind);

                        Assert.Equal(ctx.TraceId, activity.TraceId);
                        Assert.Equal(ctx.SpanId, activity.ParentSpanId);
                        Assert.Equal(ctx.TraceFlags, activity.ActivityTraceFlags);
                        Assert.Equal(ctx.TraceState, activity.TraceStateString);
                        Assert.Equal(ActivityIdFormat.W3C, activity.IdFormat);

                        foreach (KeyValuePair<string, string> pair in attributes)
                        {
                            Assert.NotEqual(default, activity.Tags.FirstOrDefault((p) => pair.Key == p.Key && pair.Value == pair.Value));
                        }

                        foreach (ActivityLink link in links)
                        {
                            Assert.NotEqual(default, activity.Links.FirstOrDefault((l) => link == l));
                        }
                    }

                    using (Activity activity = source.StartActivity("a2", ActivityKind.Client, "NoW3CParentId", attributes, links))
                    {
                        Assert.Equal(ActivityIdFormat.Hierarchical, activity.IdFormat);
                    }
                }
            }).Dispose();
        }
        public void Dispose() => Activity.Current = null;
    }
}
