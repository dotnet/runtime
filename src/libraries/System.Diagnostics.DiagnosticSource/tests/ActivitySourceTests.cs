// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                Assert.True(ActivitySource.ActiveList.Count() > 0);
                Assert.Same(source, ActivitySource.ActiveList.FirstOrDefault(c => c.Name == source.Name));

                using (ActivitySource source1 = new ActivitySource("Source1"))
                {
                    Assert.NotSame(source, source1);
                }
            }

            Assert.Null(ActivitySource.ActiveList.FirstOrDefault(c => c.Name == "Source1"));
        }

        [Fact]
        public void CreateActivityTest()
        {
            using (ActivitySource source = new ActivitySource("Source2"))
            {
                using (Activity activity = source.CreateActivity())
                {
                    // No listener, activity should be null
                    Assert.Null(activity);
                }
            }
        }

        [Fact]
        public void SourceListenerTests()
        {
            int count = 0;

            EventHandler<ActivitySourceEventArgs> eventHandler = (o, a) =>
            {
                Assert.Equal(ActivitySourceEventOperation.SourceCreated, a.Operation);
                count++;
            };

            ActivitySource.OperationEvent += eventHandler;

            using (ActivitySource source = new ActivitySource("Source3"))
            {
                Assert.Equal(1, count);
                using (ActivitySource source1 = new ActivitySource("Source4"))
                {
                    Assert.Equal(2, count);
                    using (ActivitySource source2 = new ActivitySource("Source5"))
                    {
                        Assert.Equal(3, count);
                    }
                }
            }

            using (ActivitySource source = new ActivitySource("Source6"))
            {
                Assert.Equal(4, count);

                ActivitySource.OperationEvent -= eventHandler;

                using (ActivitySource source1 = new ActivitySource("Source7"))
                {
                    Assert.Equal(4, count);
                }
            }
        }

        [Fact]
        public void ActivityListenerTests()
        {
            int count = 0;

            EventHandler<ActivitySourceEventArgs> eventHandler = (o, a) => {
                if (a.Operation == ActivitySourceEventOperation.ActivityStarted)
                {
                    count++;
                }
                else if (a.Operation == ActivitySourceEventOperation.ActivityStopped)
                {
                    count--;
                }
                else
                {
                    Assert.True(false, "Shouldn't get Operation value different than ActivityStarted or ActivityStopped");
                }
            };

            using (ActivitySource source = new ActivitySource("Source8"))
            {
                source.ActivityEvent += eventHandler;

                using (Activity activity = source.CreateActivity())
                {
                    Assert.NotNull(activity);
                    Assert.Equal(1, count);
                    using (Activity activity1 = source.CreateActivity())
                    {
                        Assert.NotNull(activity1);
                        Assert.Equal(2, count);
                    }
                    Assert.Equal(1, count);
                }
                Assert.Equal(0, count);

                source.ActivityEvent -= eventHandler;

                using (Activity activity = source.CreateActivity())
                {
                    Assert.Null(activity);
                    Assert.Equal(0, count);
                }
            }
        }

        [Fact]
        public void CreateActivityFromContextTests()
        {
            EventHandler<ActivitySourceEventArgs> eventHandler = (o, a) => { };

            using (ActivitySource source = new ActivitySource("Source9"))
            {
                source.ActivityEvent += eventHandler; // to ensure creating non-null activity.

                ActivityContext context = new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.None, "Key=Value");
                using (Activity activity = source.CreateActivity(context))
                {
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
