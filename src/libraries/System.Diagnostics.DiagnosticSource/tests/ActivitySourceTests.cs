// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestConstruction()
        {
            RemoteExecutor.Invoke(() => {
                using ActivitySource as1 = new ActivitySource("Source1");
                Assert.Equal("Source1", as1.Name);
                Assert.Equal(String.Empty, as1.Version);
                Assert.False(as1.HasListeners());

                using ActivitySource as2 =  new ActivitySource("Source2", "1.1.1.2");
                Assert.Equal("Source2", as2.Name);
                Assert.Equal("1.1.1.2", as2.Version);
                Assert.False(as2.HasListeners());
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestStartActivityWithNoListener()
        {
            RemoteExecutor.Invoke(() => {
                using ActivitySource aSource =  new ActivitySource("SourceActivity");
                Assert.Equal("SourceActivity", aSource.Name);
                Assert.Equal(string.Empty, aSource.Version);
                Assert.False(aSource.HasListeners());

                Activity current = Activity.Current;

                // no listeners, we should get null activity.
                using Activity a1 = aSource.StartActivity("a1");
                Assert.Null(a1);
                Assert.Equal(Activity.Current, current);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestActivityWithListenerNoActivityCreate()
        {
            RemoteExecutor.Invoke(() => {
                using ActivitySource aSource =  new ActivitySource("SourceActivityListener");
                Assert.False(aSource.HasListeners());

                using ActivityListener listener = new ActivityListener();
                listener.ActivityStarted = activity => Assert.NotNull(activity);
                listener.ActivityStopped = activity => Assert.NotNull(activity);
                listener.ShouldListenTo = (activitySource) => object.ReferenceEquals(aSource, activitySource);
                listener.SampleUsingParentId = (ref ActivityCreationOptions<string> activityOptions) => ActivitySamplingResult.None;
                listener.Sample = (ref ActivityCreationOptions<ActivityContext> activityOptions) => ActivitySamplingResult.None;

                ActivitySource.AddActivityListener(listener);
                Assert.True(aSource.HasListeners());

                // The listener is not allowing to create a new Activity.
                Assert.Null(aSource.StartActivity("nullActivity"));
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestActivityWithListenerActivityCreateAndAllDataRequested()
        {
            RemoteExecutor.Invoke(() => {
                using (ActivitySource aSource = new ActivitySource("SourceActivityListener"))
                {
                    int counter = 0;
                    Assert.False(aSource.HasListeners());

                    using ActivityListener listener = new ActivityListener();
                    listener.ActivityStarted = activity => counter++;
                    listener.ActivityStopped = activity => counter--;
                    listener.ShouldListenTo = (activitySource) => object.ReferenceEquals(aSource, activitySource);
                    listener.SampleUsingParentId = (ref ActivityCreationOptions<string> activityOptions) => ActivitySamplingResult.AllDataAndRecorded;
                    listener.Sample = (ref ActivityCreationOptions<ActivityContext> activityOptions) => ActivitySamplingResult.AllDataAndRecorded;

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
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestActivitySourceAttachedObject()
        {
            RemoteExecutor.Invoke(() => {
                // All Activities created through the constructor should have same source.
                Assert.True(object.ReferenceEquals(new Activity("a1").Source, new Activity("a2").Source));
                Assert.Equal("", new Activity("a3").Source.Name);
                Assert.Equal(string.Empty, new Activity("a4").Source.Version);

                using ActivitySource aSource = new ActivitySource("SourceToTest", "1.2.3.4");

                // Ensure at least we have a listener to allow Activity creation
                using ActivityListener listener = new ActivityListener();
                listener.ActivityStarted = activity => Assert.NotNull(activity);
                listener.ActivityStopped = activity => Assert.NotNull(activity);
                listener.ShouldListenTo = (activitySource) => object.ReferenceEquals(aSource, activitySource);
                listener.SampleUsingParentId = (ref ActivityCreationOptions<string> activityOptions) => ActivitySamplingResult.AllData;
                listener.Sample = (ref ActivityCreationOptions<ActivityContext> activityOptions) => ActivitySamplingResult.AllData;

                ActivitySource.AddActivityListener(listener);

                using Activity activity = aSource.StartActivity("ActivityToTest");
                Assert.True(object.ReferenceEquals(aSource, activity.Source));
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestListeningToConstructedActivityEvents()
        {
            RemoteExecutor.Invoke(() => {
                int activityStartCount = 0;
                int activityStopCount  = 0;

                using (ActivityListener listener = new ActivityListener())
                {
                    listener.ActivityStarted = activity => activityStartCount++;
                    listener.ActivityStopped = activity => activityStopCount++;
                    listener.ShouldListenTo = (activitySource) => activitySource.Name == "" && activitySource.Version == "";
                    listener.SampleUsingParentId = (ref ActivityCreationOptions<string> activityOptions) => ActivitySamplingResult.AllData;
                    listener.Sample = (ref ActivityCreationOptions<ActivityContext> activityOptions) => ActivitySamplingResult.AllData;

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

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestExpectedListenersReturnValues()
        {
            RemoteExecutor.Invoke(() => {

                int activityStartCount = 0;
                int activityStopCount = 0;

                ActivitySource source = new ActivitySource("MultipleListenerSource");
                ActivityListener [] listeners = new ActivityListener[4];

                listeners[0] = new ActivityListener
                {
                    ActivityStarted = (activity) => { activityStartCount++; Assert.NotNull(activity); },
                    ActivityStopped = (activity) => { activityStopCount++; Assert.NotNull(activity); },
                    ShouldListenTo = (activitySource) => true,
                    SampleUsingParentId = (ref ActivityCreationOptions<string> activityOptions) => ActivitySamplingResult.None,
                    Sample = (ref ActivityCreationOptions<ActivityContext> activityOptions) => ActivitySamplingResult.None
                };
                ActivitySource.AddActivityListener(listeners[0]);

                Assert.Null(source.StartActivity("a1"));

                Assert.Equal(0, activityStartCount);
                Assert.Equal(0, activityStopCount);

                listeners[1] = new ActivityListener
                {
                    ActivityStarted = (activity) => { activityStartCount++; Assert.NotNull(activity); },
                    ActivityStopped = (activity) => { activityStopCount++; Assert.NotNull(activity); },
                    ShouldListenTo = (activitySource) => true,
                    SampleUsingParentId = (ref ActivityCreationOptions<string> activityOptions) => ActivitySamplingResult.PropagationData,
                    Sample = (ref ActivityCreationOptions<ActivityContext> activityOptions) => ActivitySamplingResult.PropagationData
                };
                ActivitySource.AddActivityListener(listeners[1]);

                using (Activity a2 = source.StartActivity("a2"))
                {
                    Assert.False(a2.IsAllDataRequested);
                    Assert.True((a2.ActivityTraceFlags & ActivityTraceFlags.Recorded) == 0);

                    Assert.Equal(2, activityStartCount);
                    Assert.Equal(0, activityStopCount);
                }

                Assert.Equal(activityStartCount, activityStopCount);
                Assert.Equal(2, activityStopCount);

                listeners[2] = new ActivityListener
                {
                    ActivityStarted = (activity) => { activityStartCount++; Assert.NotNull(activity); },
                    ActivityStopped = (activity) => { activityStopCount++; Assert.NotNull(activity); },
                    ShouldListenTo = (activitySource) => true,
                    SampleUsingParentId = (ref ActivityCreationOptions<string> activityOptions) => ActivitySamplingResult.AllData,
                    Sample = (ref ActivityCreationOptions<ActivityContext> activityOptions) => ActivitySamplingResult.AllData
                };
                ActivitySource.AddActivityListener(listeners[2]);

                using (Activity a3 = source.StartActivity("a3"))
                {
                    Assert.True(a3.IsAllDataRequested);
                    Assert.True((a3.ActivityTraceFlags & ActivityTraceFlags.Recorded) == 0);

                    Assert.Equal(5, activityStartCount);
                    Assert.Equal(2, activityStopCount);
                }

                Assert.Equal(activityStartCount, activityStopCount);
                Assert.Equal(5, activityStopCount);

                listeners[3] = new ActivityListener
                {
                    ActivityStarted = (activity) => { activityStartCount++; Assert.NotNull(activity); },
                    ActivityStopped = (activity) => { activityStopCount++; Assert.NotNull(activity); },
                    ShouldListenTo = (activitySource) => true,
                    SampleUsingParentId = (ref ActivityCreationOptions<string> activityOptions) => ActivitySamplingResult.AllDataAndRecorded,
                    Sample = (ref ActivityCreationOptions<ActivityContext> activityOptions) => ActivitySamplingResult.AllDataAndRecorded
                };
                ActivitySource.AddActivityListener(listeners[3]);

                using (Activity a4 = source.StartActivity("a4"))
                {
                    Assert.True(a4.IsAllDataRequested);
                    Assert.True((a4.ActivityTraceFlags & ActivityTraceFlags.Recorded) != 0, $"a4.ActivityTraceFlags failed: {a4.ActivityTraceFlags}");

                    Assert.Equal(9, activityStartCount);
                    Assert.Equal(5, activityStopCount);
                }

                foreach (IDisposable listener in listeners)
                {
                    listener.Dispose();
                }

                Assert.Equal(activityStartCount, activityStopCount);
                Assert.Equal(9, activityStopCount);
                Assert.Null(source.StartActivity("a5"));
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestActivityCreationProperties()
        {
            RemoteExecutor.Invoke(() => {
                ActivitySource source = new ActivitySource("MultipleListenerSource");

                using ActivityListener listener = new ActivityListener();
                listener.ActivityStarted = activity => Assert.NotNull(activity);
                listener.ActivityStopped = activity => Assert.NotNull(activity);
                listener.ShouldListenTo = (activitySource) => true;
                listener.SampleUsingParentId = (ref ActivityCreationOptions<string> activityOptions) => ActivitySamplingResult.AllData;
                listener.Sample = (ref ActivityCreationOptions<ActivityContext> activityOptions) => ActivitySamplingResult.AllData;

                ActivitySource.AddActivityListener(listener);

                ActivityContext ctx = new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded, "key0-value0");

                List<ActivityLink> links = new List<ActivityLink>();
                links.Add(new ActivityLink(new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.None, "key1-value1")));
                links.Add(new ActivityLink(new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.None, "key2-value2")));

                List<KeyValuePair<string, object>> attributes = new List<KeyValuePair<string, object>>();
                attributes.Add(new KeyValuePair<string, object>("tag1", "tagValue1"));
                attributes.Add(new KeyValuePair<string, object>("tag2", "tagValue2"));
                attributes.Add(new KeyValuePair<string, object>("tag3", "tagValue3"));

                DateTimeOffset startTime = DateTimeOffset.UtcNow;

                using (Activity activity = source.StartActivity("a1", ActivityKind.Client, ctx, attributes, links, startTime))
                {
                    Assert.NotNull(activity);
                    Assert.Equal("a1", activity.OperationName);
                    Assert.Equal("a1", activity.DisplayName);
                    Assert.Equal(ActivityKind.Client, activity.Kind);
                    Assert.Equal(startTime, activity.StartTimeUtc);

                    Assert.Equal(ctx.TraceId, activity.TraceId);
                    Assert.Equal(ctx.SpanId, activity.ParentSpanId);
                    Assert.Equal(ctx.TraceFlags, activity.ActivityTraceFlags);
                    Assert.Equal(ctx.TraceState, activity.TraceStateString);
                    Assert.Equal(ActivityIdFormat.W3C, activity.IdFormat);

                    foreach (KeyValuePair<string, object> pair in attributes)
                    {
                        Assert.NotEqual(default, activity.Tags.FirstOrDefault((p) => pair.Key == p.Key && pair.Value == pair.Value));
                    }

                    foreach (ActivityLink link in links)
                    {
                        Assert.NotEqual(default, activity.Links.FirstOrDefault((l) => link == l));
                    }
                }

                using Activity activity1 = source.StartActivity("a2", ActivityKind.Client, "NoW3CParentId", attributes, links);
                Assert.Equal(ActivityIdFormat.Hierarchical, activity1.IdFormat);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestDefaultParentContext()
        {
            RemoteExecutor.Invoke(() => {
                using ActivitySource aSource = new ActivitySource("ParentContext");
                using ActivityListener listener = new ActivityListener();

                listener.ShouldListenTo = (activitySource) => activitySource.Name == "ParentContext";
                listener.Sample = (ref ActivityCreationOptions<ActivityContext> activityOptions) =>
                {
                    Activity c = Activity.Current;
                    if (c != null)
                    {
                        Assert.Equal(c.Context, activityOptions.Parent);
                    }

                    return ActivitySamplingResult.AllData;
                };

                ActivitySource.AddActivityListener(listener);
                using Activity a = aSource.StartActivity("a", ActivityKind.Server, new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), 0));
                using Activity b = aSource.StartActivity("b");
                Assert.Equal(a.Context, b.Parent.Context);
            }).Dispose();
        }

        [Fact]
        public void TestActivityContextParsing()
        {
            const string w3cId = "00-99d43cb30a4cdb4fbeee3a19c29201b0-e82825765f051b47-01";
            Assert.True(ActivityContext.TryParse(w3cId, "k=v", out ActivityContext context));
            Assert.Equal("99d43cb30a4cdb4fbeee3a19c29201b0", context.TraceId.ToHexString());
            Assert.Equal("e82825765f051b47", context.SpanId.ToHexString());
            Assert.Equal(ActivityTraceFlags.Recorded, context.TraceFlags);
            Assert.Equal("k=v", context.TraceState);

            context = ActivityContext.Parse(w3cId, "k=v");
            Assert.Equal("99d43cb30a4cdb4fbeee3a19c29201b0", context.TraceId.ToHexString());
            Assert.Equal("e82825765f051b47", context.SpanId.ToHexString());
            Assert.Equal(ActivityTraceFlags.Recorded, context.TraceFlags);
            Assert.Equal("k=v", context.TraceState);

            context = ActivityContext.Parse(w3cId, null);
            Assert.Null(context.TraceState);

            Assert.Throws<ArgumentNullException>(() => ActivityContext.TryParse(null, "k=v", out context));
            Assert.Throws<ArgumentNullException>(() => ActivityContext.Parse(null, null));
            Assert.Throws<ArgumentException>(() => ActivityContext.Parse("BadW3C", null));

            const string invalidW3CContext = "00-Z9d43cb30a4cdb4fbeee3a19c29201b0-e82825765f051b47-01";
            Assert.False(ActivityContext.TryParse(invalidW3CContext, null, out context));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestCreatingActivityUsingDifferentParentIds()
        {
            RemoteExecutor.Invoke(() => {

                const string w3cId = "00-99d43cb30a4cdb4fbeee3a19c29201b0-e82825765f051b47-01";
                const string hierarchicalId = "SomeId";

                int callingByContext = 0;
                int callingByParentId = 0;

                using ActivitySource aSource = new ActivitySource("ParentIdsTest");
                using ActivityListener listener1 = new ActivityListener();  // will have context callback only
                using ActivityListener listener2 = new ActivityListener();  // will have parent id  callback only
                using ActivityListener listener3 = new ActivityListener();  // will have both context and parent Id callbacks

                listener1.ShouldListenTo = listener2.ShouldListenTo = listener3.ShouldListenTo = (activitySource) => activitySource.Name == "ParentIdsTest";
                listener1.Sample = listener3.Sample = (ref ActivityCreationOptions<ActivityContext> activityOptions) =>
                {
                    callingByContext++;

                    Assert.Equal(new ActivityContext(ActivityTraceId.CreateFromString(w3cId.AsSpan(3,  32)), ActivitySpanId.CreateFromString(w3cId.AsSpan(36, 16)), ActivityTraceFlags.Recorded),
                                 activityOptions.Parent);

                    return ActivitySamplingResult.AllData;
                };
                listener2.SampleUsingParentId = listener3.SampleUsingParentId = (ref ActivityCreationOptions<string> activityOptions) =>
                {
                    callingByParentId++;
                    return ActivitySamplingResult.AllData;
                };

                ActivitySource.AddActivityListener(listener1);
                ActivitySource.AddActivityListener(listener2);
                ActivitySource.AddActivityListener(listener3);


                // Create Activity using hierarchical Id, should trigger calling listener 2 and listener 3 only.
                using Activity a = aSource.StartActivity("a", ActivityKind.Client, hierarchicalId);
                Assert.Equal(2, callingByParentId);
                Assert.Equal(0, callingByContext);

                // Create Activity using W3C Id, should trigger calling all listeners.
                using Activity b = aSource.StartActivity("b", ActivityKind.Client, w3cId);
                Assert.Equal(4, callingByParentId);
                Assert.Equal(1, callingByContext);

                ActivityTraceId traceId = ActivityTraceId.CreateFromString("99d43cb30a4cdb4fbeee3a19c29201b0".AsSpan());

                Assert.NotEqual("99d43cb30a4cdb4fbeee3a19c29201b0", a.TraceId.ToHexString());
                Assert.Equal("99d43cb30a4cdb4fbeee3a19c29201b0", b.TraceId.ToHexString());
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestActivityContextIsRemote()
        {
            RemoteExecutor.Invoke(() => {
                ActivityContext ctx = new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), default);
                Assert.False(ctx.IsRemote);

                bool isRemote = false;

                using ActivitySource aSource = new ActivitySource("RemoteContext");
                using ActivityListener listener = new ActivityListener();
                listener.ShouldListenTo = (activitySource) => activitySource.Name == "RemoteContext";

                listener.Sample = (ref ActivityCreationOptions<ActivityContext> activityOptions) =>
                {
                    isRemote = activityOptions.Parent.IsRemote;
                    return ActivitySamplingResult.AllData;
                };

                ActivitySource.AddActivityListener(listener);

                foreach (bool b in new bool[] { true, false })
                {
                    ctx = new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), default, default, b);
                    Assert.Equal(b, ctx.IsRemote);

                    aSource.StartActivity("a1", default, ctx);
                    Assert.Equal(b , isRemote);
                }
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestTraceIdAutoGeneration()
        {
            RemoteExecutor.Invoke(() => {

                using ActivitySource aSource = new ActivitySource("TraceIdAutoGeneration");
                using ActivityListener listener = new ActivityListener();
                listener.ShouldListenTo = (activitySource) => activitySource.Name == "TraceIdAutoGeneration";

                ActivityContext ctx = default;

                bool forceGenerateTraceId = false;

                listener.Sample = (ref ActivityCreationOptions<ActivityContext> activityOptions) =>
                {
                    ctx = forceGenerateTraceId ? new ActivityContext(activityOptions.TraceId, default, default) : activityOptions.Parent;
                    return ActivitySamplingResult.AllData;
                };

                ActivitySource.AddActivityListener(listener);

                using (aSource.StartActivity("a1", default, ctx))
                {
                    Assert.Equal(default, ctx);
                }

                forceGenerateTraceId = true;

                Activity activity = aSource.StartActivity("a2", default, ctx);

                Assert.NotNull(activity);
                Assert.NotEqual(default, ctx);
                Assert.Equal(ctx.TraceId, activity.TraceId);
                Assert.Equal(ctx.SpanId.ToHexString(), activity.ParentSpanId.ToHexString());
                Assert.Equal(default(ActivitySpanId).ToHexString(), ctx.SpanId.ToHexString());
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestTraceIdAutoGenerationWithNullParentId()
        {
            RemoteExecutor.Invoke(() => {

                using ActivitySource aSource = new ActivitySource("TraceIdAutoGenerationWithNullParent");
                using ActivityListener listener = new ActivityListener();
                listener.ShouldListenTo = (activitySource) => activitySource.Name == "TraceIdAutoGenerationWithNullParent";

                ActivityContext ctx = default;

                listener.Sample = (ref ActivityCreationOptions<ActivityContext> activityOptions) =>
                {
                    ctx = new ActivityContext(activityOptions.TraceId, default, default);
                    return ActivitySamplingResult.AllData;
                };

                ActivitySource.AddActivityListener(listener);

                Activity activity = aSource.StartActivity("a2", default, null);

                Assert.NotNull(activity);
                Assert.NotEqual(default, ctx);
                Assert.Equal(ctx.TraceId, activity.TraceId);
                Assert.Equal(ctx.SpanId, activity.ParentSpanId);
                Assert.Equal(default(ActivitySpanId), activity.ParentSpanId);
                Assert.Equal(default(ActivitySpanId), ctx.SpanId);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestEventNotificationOrder()
        {
            RemoteExecutor.Invoke(() => {

                Activity parent = Activity.Current;
                Activity child = null;

                using ActivitySource aSource = new ActivitySource("EventNotificationOrder");
                using ActivityListener listener = new ActivityListener();

                listener.ShouldListenTo = (activitySource) => activitySource.Name == "EventNotificationOrder";
                listener.Sample = (ref ActivityCreationOptions<ActivityContext> activityOptions) => ActivitySamplingResult.AllData;
                listener.ActivityStopped = a => Assert.Equal(child, Activity.Current);

                ActivitySource.AddActivityListener(listener);

                using (child = aSource.StartActivity("a1"))
                {
                    Assert.NotNull(child);
                    // by the end of this block, the stop event notification will fire and ActivityListener.ActivityStopped will get called.
                    // assert there that the created activity is still set as Current activity.
                }

                // Now the Current should be restored back.
                Assert.Equal(parent, Activity.Current);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestAddingSamplerTags()
        {
            RemoteExecutor.Invoke(() => {

                using ActivitySource aSource = new ActivitySource("SamplerTags1");
                using ActivityListener listener = new ActivityListener();
                listener.ShouldListenTo = (activitySource) => activitySource.Name == "SamplerTags1";

                listener.Sample = (ref ActivityCreationOptions<ActivityContext> activityOptions) =>
                {
                    activityOptions.SamplingTags.Add("tag1", "value1");
                    activityOptions.SamplingTags.Add("tag2", "value2");
                    return ActivitySamplingResult.AllData;
                };

                ActivitySource.AddActivityListener(listener);

                using Activity activity = aSource.StartActivity("a1");

                Assert.NotNull(activity);
                Assert.Equal(2, activity.TagObjects.Count());
                Assert.Equal(new KeyValuePair<string, object>("tag1", "value1"), activity.TagObjects.ElementAt(0));
                Assert.Equal(new KeyValuePair<string, object>("tag2", "value2"), activity.TagObjects.ElementAt(1));
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestAddingSamplerTagsUsingParentId()
        {
            RemoteExecutor.Invoke(() => {

                using ActivitySource aSource = new ActivitySource("SamplerTags2");
                using ActivityListener listener = new ActivityListener();
                listener.ShouldListenTo = (activitySource) => activitySource.Name == "SamplerTags2";

                listener.SampleUsingParentId = (ref ActivityCreationOptions<string> activityOptions) =>
                {
                    activityOptions.SamplingTags.Add("tag1", "value1");
                    activityOptions.SamplingTags.Add("tag2", "value2");
                    return ActivitySamplingResult.AllData;
                };

                ActivitySource.AddActivityListener(listener);

                Activity activity = aSource.StartActivity("a1", ActivityKind.Client, "SomeParent");

                Assert.NotNull(activity);
                Assert.Equal(2, activity.TagObjects.Count());
                Assert.Equal(new KeyValuePair<string, object>("tag1", "value1"), activity.TagObjects.ElementAt(0));
                Assert.Equal(new KeyValuePair<string, object>("tag2", "value2"), activity.TagObjects.ElementAt(1));
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestNotAddedSamplerTags()
        {
            RemoteExecutor.Invoke(() => {

                using ActivitySource aSource = new ActivitySource("SamplerTags3");
                using ActivityListener listener = new ActivityListener();
                listener.ShouldListenTo = (activitySource) => activitySource.Name == "SamplerTags3";

                listener.Sample = (ref ActivityCreationOptions<ActivityContext> activityOptions) =>
                {
                    activityOptions.SamplingTags.Add("tag1", "value1");
                    Assert.False(true, "This callback shouldn't be called at all.");
                    return ActivitySamplingResult.AllData;
                };

                ActivitySource.AddActivityListener(listener);

                // activity should be null as no listener asked for creation.
                using Activity activity = aSource.StartActivity("a1", ActivityKind.Client, "NonW3CParentId");

                Assert.Null(activity);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestDefaultTraceIdWithHierarchicalParentId()
        {
            RemoteExecutor.Invoke(() => {

                using ActivitySource aSource = new ActivitySource("SamplerTags4");
                using ActivityListener listener = new ActivityListener();
                listener.ShouldListenTo = (activitySource) => activitySource.Name == "SamplerTags4";

                listener.SampleUsingParentId = (ref ActivityCreationOptions<string> activityOptions) =>
                {
                    Assert.Equal(default, activityOptions.TraceId);
                    return ActivitySamplingResult.AllData;
                };

                ActivitySource.AddActivityListener(listener);

                Activity activity = aSource.StartActivity("a1", ActivityKind.Client, "HierarchicalParentId");

                Assert.NotNull(activity);
                // activity has no parent. expected default trace Id.
                Assert.Equal(default, activity.TraceId);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestAddingSamplerTagsFromMultipleListeners()
        {
            RemoteExecutor.Invoke(() => {
                using ActivitySource aSource = new ActivitySource("SamplerTags5");
                using ActivityListener listener1 = new ActivityListener();
                using ActivityListener listener2 = new ActivityListener();
                listener1.ShouldListenTo = (activitySource) => activitySource.Name == "SamplerTags5";
                listener2.ShouldListenTo = (activitySource) => activitySource.Name == "SamplerTags5";

                listener1.Sample = listener2.Sample = (ref ActivityCreationOptions<ActivityContext> activityOptions) =>
                {
                    if (activityOptions.SamplingTags.Count == 0)
                        activityOptions.SamplingTags.Add("tag1", "value1");
                    else
                        activityOptions.SamplingTags.Add("tag2", "value2");

                    return ActivitySamplingResult.AllData;
                };

                ActivitySource.AddActivityListener(listener1);
                ActivitySource.AddActivityListener(listener2);

                using Activity activity = aSource.StartActivity("a1");

                Assert.NotNull(activity);
                Assert.Equal(2, activity.TagObjects.Count());
                Assert.Equal(new KeyValuePair<string, object>("tag1", "value1"), activity.TagObjects.ElementAt(0));
                Assert.Equal(new KeyValuePair<string, object>("tag2", "value2"), activity.TagObjects.ElementAt(1));
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestSamplerTagsWithMixedListenerModels()
        {
            RemoteExecutor.Invoke(() => {
                const string w3cId = "00-99d43cb30a4cdb4fbeee3a19c29201b0-e82825765f051b47-01";

                using ActivitySource aSource = new ActivitySource("SamplerTags6");
                using ActivityListener listener1 = new ActivityListener();
                using ActivityListener listener2 = new ActivityListener();
                listener1.ShouldListenTo = (activitySource) => activitySource.Name == "SamplerTags6";
                listener2.ShouldListenTo = (activitySource) => activitySource.Name == "SamplerTags6";

                // listener1 provide SampleUsingParentId callback and doesn't provide Sample
                listener1.SampleUsingParentId = (ref ActivityCreationOptions<string> activityOptions) =>
                {
                    activityOptions.SamplingTags.Add("tag1", "value1");
                    return ActivitySamplingResult.AllData;
                };

                // listener2 provide Sample callback and doesn't provide SampleUsingParentId
                // Sample should get called as we convert the w3c id to a context.
                listener2.Sample = (ref ActivityCreationOptions<ActivityContext> activityOptions) =>
                {
                    activityOptions.SamplingTags.Add("tag2", "value2");
                    return ActivitySamplingResult.AllData;
                };

                ActivitySource.AddActivityListener(listener1);
                ActivitySource.AddActivityListener(listener2);

                using Activity activity = aSource.StartActivity("a1", ActivityKind.Client, w3cId);

                Assert.NotNull(activity);
                Assert.Equal(2, activity.TagObjects.Count());
                Assert.Equal(new KeyValuePair<string, object>("tag1", "value1"), activity.TagObjects.ElementAt(0));
                Assert.Equal(new KeyValuePair<string, object>("tag2", "value2"), activity.TagObjects.ElementAt(1));
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestAddSamplerAndActivityCreationTags()
        {
            RemoteExecutor.Invoke(() => {
                using ActivitySource aSource = new ActivitySource("SamplerTags7");
                using ActivityListener listener = new ActivityListener();
                listener.ShouldListenTo = (activitySource) => activitySource.Name == "SamplerTags7";

                listener.Sample = (ref ActivityCreationOptions<ActivityContext> activityOptions) =>
                {
                    activityOptions.SamplingTags.Add("SamplerTag1", "SamplerValue1");
                    return ActivitySamplingResult.AllData;
                };

                ActivitySource.AddActivityListener(listener);

                ActivityTagsCollection tags = new ActivityTagsCollection();
                tags["tag1"] = "value1";
                tags["tag2"] = "value2";

                using Activity activity = aSource.StartActivity("a1", ActivityKind.Client, default(ActivityContext), tags);

                Assert.NotNull(activity);
                Assert.Equal(3, activity.TagObjects.Count());
                Assert.Equal(new KeyValuePair<string, object>("tag1", "value1"), activity.TagObjects.ElementAt(0));
                Assert.Equal(new KeyValuePair<string, object>("tag2", "value2"), activity.TagObjects.ElementAt(1));
                Assert.Equal(new KeyValuePair<string, object>("SamplerTag1", "SamplerValue1"), activity.TagObjects.ElementAt(2));
            }).Dispose();
        }

        public void Dispose() => Activity.Current = null;
    }
}
