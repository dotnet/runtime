// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class ActivitySourceTests
    {
        [Fact]
        public void CreateTest()
        {
            using (ActivitySource source = new ActivitySource("Source1"))
            {
                Assert.Equal("Source1", source.Name);
            }
        }

        [Fact]
        public void StartActivityTest()
        {
            using (ActivitySource source = new ActivitySource("Source2"))
            {
                using (Activity activity = source.StartActivity())
                {
                    // No listener, activity should be null
                    Assert.Null(activity);
                }
            }
        }

        [Fact]
        public void SourceListenerTests()
        {
            Listener listener;

            using (listener = new Listener(enableListening: false, createActivities: false))
            {
                ActivitySource.AddListener(listener);
                using (ActivitySource source = new ActivitySource("Source3"))
                {
                    using (Activity activity = source.StartActivity())
                    {
                        // There is a listener which will not intereseted to listen to any source.
                        Assert.Null(activity);
                        Assert.Equal(0, listener.Count);
                        Assert.Equal(0, listener.SourceNames.Count());
                    }
                }
            }

            using (listener = new Listener(enableListening: true, createActivities: false))
            {
                ActivitySource.AddListener(listener);
                using (ActivitySource source = new ActivitySource("Source4"))
                {
                    using (Activity activity = source.StartActivity())
                    {
                        // There is a listener which is listening but not allowing to create any activity.
                        Assert.Null(activity);
                        Assert.Equal(0, listener.Count);
                        Assert.Equal(1, listener.SourceNames.Count());
                    }
                }
            }

            using (listener = new Listener(enableListening: true, createActivities: true))
            {
                ActivitySource.AddListener(listener);
                using (ActivitySource source = new ActivitySource("Source5"))
                {
                    Assert.Equal(0, listener.Count);
                    Assert.Equal(1, listener.SourceNames.Count());

                    using (Activity activity = source.StartActivity())
                    {
                        Assert.NotNull(activity);

                        // We should already got Activity start event
                        Assert.Equal(1, listener.Count);
                    }

                    // We should already got Activity stop event
                    Assert.Equal(0, listener.Count);

                    using (ActivitySource source1 = new ActivitySource("Source5"))
                    {
                        Assert.Equal(2, listener.SourceNames.Count());
                        foreach (string s in listener.SourceNames)
                        {
                            // We are listening to 2 sources with the same name.
                            Assert.Equal("Source5", s);
                        }
                    }

                }
            }
        }

        [Fact]
        public void CreateActivityFromContextTests()
        {
            using (Listener listener = new Listener(enableListening: true, createActivities: true))
            {
                ActivitySource.AddListener(listener);

                using (ActivitySource source = new ActivitySource("Source6"))
                {
                    Assert.Equal(0, listener.Count);

                    ActivityContext context = new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.None, "Key=Value");
                    using (Activity activity = source.StartActivity(context))
                    {
                        Assert.Equal(1, listener.Count);

                        Assert.NotNull(activity);
                        Assert.Equal(context.TraceId, activity.TraceId);
                        Assert.Equal(context.SpanId, activity.ParentSpanId);
                        Assert.Equal(context.TraceFlags, activity.ActivityTraceFlags);
                        Assert.Equal(context.TraceState, activity.TraceStateString);
                    }
                }
            }
        }
    }

    public class Listener : ActivityListener
    {
        private bool _enableListening;
        private bool _createActivities;

        private List<string> _sourceNames = new List<string>();

        public Listener(bool enableListening, bool createActivities)
        {
            _enableListening = enableListening;
            _createActivities = createActivities;
        }

        public int Count { get; set; }

        public IEnumerable<string> SourceNames => _sourceNames;

        public override bool EnableListening(string activitySourceName)
        {
            if (_enableListening)
                _sourceNames.Add(activitySourceName);

            return _enableListening;
        }

        public override bool ShouldCreateActivity(string activitySourceName, ActivityContext context, IEnumerable<ActivityLink> links) => _createActivities;

        public override void OnActivityStarted(Activity a) { Count++; }

        public override void OnActivityStopped(Activity a) { Count--;}
   }
}
