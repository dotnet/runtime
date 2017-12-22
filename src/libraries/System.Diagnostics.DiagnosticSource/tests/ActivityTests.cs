// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class ActivityTests
    {
        /// <summary>
        /// Tests Activity constructor
        /// </summary>
        [Fact]
        public void DefaultActivity()
        {
            string activityName = "activity";
            var activity = new Activity(activityName);
            Assert.Equal(activityName, activity.OperationName);
            Assert.Null(activity.Id);
            Assert.Null(activity.RootId);
            Assert.Equal(TimeSpan.Zero, activity.Duration);            
            Assert.Null(activity.Parent);
            Assert.Null(activity.ParentId);
            Assert.Equal(0, activity.Baggage.ToList().Count);
            Assert.Equal(0, activity.Tags.ToList().Count);
        }

        /// <summary>
        /// Tests baggage operations
        /// </summary>
        [Fact]
        public void Baggage()
        {
            var activity = new Activity("activity");
            Assert.Null(activity.GetBaggageItem("some baggage"));

            Assert.Equal(activity, activity.AddBaggage("some baggage", "value"));
            Assert.Equal("value", activity.GetBaggageItem("some baggage"));

            var baggage = activity.Baggage.ToList();
            Assert.Equal(1, baggage.Count);
        }

        /// <summary>
        /// Tests Tags operations
        /// </summary>
        [Fact]
        public void Tags()
        {
            var activity = new Activity("activity");

            Assert.Equal(activity, activity.AddTag("some tag", "value"));

            var tags = activity.Tags.ToList();
            Assert.Equal(1, tags.Count);
            Assert.Equal(tags[0].Key, "some tag");
            Assert.Equal(tags[0].Value, "value");
        }

        /// <summary>
        /// Tests activity SetParentId
        /// </summary>
        [Fact]
        public void SetParentId()
        {
            var parent = new Activity("parent");
            parent.SetParentId(null);  // Error does nothing
            Assert.Null(parent.ParentId);

            parent.SetParentId("");  // Error does nothing
            Assert.Null(parent.ParentId);

            parent.SetParentId("1");
            Assert.Equal("1", parent.ParentId);

            parent.SetParentId("2"); // Error does nothing
            Assert.Equal("1", parent.ParentId);

            Assert.Equal(parent.ParentId, parent.RootId);
            parent.Start();

            var child = new Activity("child");
            child.Start();

            Assert.Equal(parent.Id, child.ParentId);
            child.SetParentId("3");  // Error does nothing;
            Assert.Equal(parent.Id, child.ParentId);
        }

        /// <summary>
        /// Tests activity SetParentId
        /// </summary>
        [Fact]
        public void ActivityIdOverflow()
        {
            //check parentId |abc.1.1...1.1.1.1.1. (1023 bytes) and check last .1.1.1.1 is replaced with #overflow_suffix 8 bytes long
            var parentId = new StringBuilder("|abc.");
            while (parentId.Length < 1022)
                parentId.Append("1.");

            var activity = new Activity("activity")
                .SetParentId(parentId.ToString())
                .Start();

            Assert.Equal(
                parentId.ToString().Substring(0, parentId.Length - 8),
                activity.Id.Substring(0, activity.Id.Length - 9));
            Assert.Equal('#', activity.Id[activity.Id.Length - 1]);

            //check parentId |abc.1.1...1.012345678 (128 bytes) and check last .012345678 is replaced with #overflow_suffix 8 bytes long
            parentId = new StringBuilder("|abc.");
            while (parentId.Length < 1013)
                parentId.Append("1.");
            parentId.Append("012345678.");

            activity = new Activity("activity")
                .SetParentId(parentId.ToString())
                .Start();

            //last .012345678 will be replaced with #overflow_suffix 8 bytes long
            Assert.Equal(
                parentId.ToString().Substring(0, parentId.Length - 10),
                activity.Id.Substring(0, activity.Id.Length - 9));
            Assert.Equal('#', activity.Id[activity.Id.Length - 1]);
        }

        /// <summary>
        /// Tests overflow in Id generation when parentId has a single (root) node
        /// </summary>
        [Fact]
        public void ActivityIdNonHierarchicalOverflow()
        {
            // find out Activity Id length on this platform in this AppDomain
            Activity testActivity = new Activity("activity")
                .Start();
            var expectedIdLength = testActivity.Id.Length;
            testActivity.Stop();

            // check that if parentId '|aaa...a' 1024 bytes long is set with single node (no dots or underscores in the Id)
            // it causes overflow during Id generation, and new root Id is generated for the new Activity
            var parentId = '|' + new string('a', 1022) + '.';

            var activity = new Activity("activity")
                .SetParentId(parentId)
                .Start();

            Assert.Equal(parentId, activity.ParentId);

            // With probability 1/MaxLong, Activity.Id length may be expectedIdLength + 1
            Assert.InRange(activity.Id.Length, expectedIdLength, expectedIdLength + 1);
            Assert.False(activity.Id.Contains('#'));
        }

        /// <summary>
        /// Tests activity start and stop 
        /// Checks Activity.Current correctness, Id generation
        /// </summary>
        [Fact]
        public void StartStop()
        {
            var activity = new Activity("activity");
            Assert.Equal(null, Activity.Current);
            activity.Start();
            Assert.Equal(activity, Activity.Current);
            Assert.Null(activity.Parent);
            Assert.NotNull(activity.Id);
            Assert.NotNull(activity.RootId);
            Assert.NotEqual(default(DateTime), activity.StartTimeUtc);

            activity.Stop();
            Assert.Equal(null, Activity.Current);
        }

        /// <summary>
        /// Tests Id generation
        /// </summary>
        [Fact]
        public void IdGenerationNoParent()
        {
            var orphan1 = new Activity("orphan1");
            var orphan2 = new Activity("orphan2");

            Task.Run(() => orphan1.Start()).Wait();
            Task.Run(() => orphan2.Start()).Wait();

            Assert.NotEqual(orphan2.Id, orphan1.Id);
            Assert.NotEqual(orphan2.RootId, orphan1.RootId);
        }

        /// <summary>
        /// Tests Id generation
        /// </summary>
        [Fact]
        public void IdGenerationInternalParent()
        {
            var parent = new Activity("parent");
            parent.Start();
            var child1 = new Activity("child1");
            var child2 = new Activity("child2");
            //start 2 children in different execution contexts
            Task.Run(() => child1.Start()).Wait();
            Task.Run(() => child2.Start()).Wait();
#if DEBUG
            Assert.Equal($"|{parent.RootId}.{child1.OperationName}-1.", child1.Id);
            Assert.Equal($"|{parent.RootId}.{child2.OperationName}-2.", child2.Id);
#else
            Assert.Equal($"|{parent.RootId}.1.", child1.Id);
            Assert.Equal($"|{parent.RootId}.2.", child2.Id);
#endif
            Assert.Equal(parent.RootId, child1.RootId);
            Assert.Equal(parent.RootId, child2.RootId);
            child1.Stop();
            child2.Stop();
            var child3 = new Activity("child3");
            child3.Start();
#if DEBUG
            Assert.Equal($"|{parent.RootId}.{child3.OperationName}-3.", child3.Id);
#else
            Assert.Equal($"|{parent.RootId}.3.", child3.Id);
#endif

            var grandChild = new Activity("grandChild");
            grandChild.Start();
#if DEBUG
            Assert.Equal($"{child3.Id}{grandChild.OperationName}-1.", grandChild.Id);
#else
            Assert.Equal($"{child3.Id}1.", grandChild.Id);
#endif

        }

        /// <summary>
        /// Tests Id generation
        /// </summary>
        [Fact]
        public void IdGenerationExternalParentId()
        {
            var child1 = new Activity("child1");
            child1.SetParentId("123");
            child1.Start();
            Assert.Equal("123", child1.RootId);
            Assert.Equal('|', child1.Id[0]);
            Assert.Equal('_', child1.Id[child1.Id.Length - 1]);
            child1.Stop();

            var child2 = new Activity("child2");
            child2.SetParentId("123");
            child2.Start();
            Assert.NotEqual(child2.Id, child1.Id);
            Assert.Equal("123", child2.RootId);
        }

        /// <summary>
        /// Tests Id generation
        /// </summary>
        [Fact]
        public void RootId()
        {

            var parentIds = new []{
                "123",   //Parent does not start with '|' and does not contain '.'
                "123.1", //Parent does not start with '|' but contains '.'
                "|123",  //Parent starts with '|' and does not contain '.'
                "|123.1.1", //Parent starts with '|' and contains '.'
            };
            foreach (var parentId in parentIds)
            {
                var activity = new Activity("activity");
                activity.SetParentId(parentId);
                Assert.Equal("123", activity.RootId);
            }
        }

        /// <summary>
        /// Tests Activity Start and Stop with timestamp
        /// </summary>
        [Fact]
        public void StartStopWithTimestamp()
        {
            var activity = new Activity("activity");
            Assert.Equal(default(DateTime), activity.StartTimeUtc);

            activity.SetStartTime(DateTime.Now);    // Error Does nothing because it is not UTC
            Assert.Equal(default(DateTime), activity.StartTimeUtc);

            var startTime = DateTime.UtcNow.AddSeconds(-1); // A valid time in the past that we want to be our offical start time.  
            activity.SetStartTime(startTime);

            activity.Start();
            Assert.Equal(startTime, activity.StartTimeUtc); // we use our offical start time not the time now.  
            Assert.Equal(TimeSpan.Zero, activity.Duration);

            Thread.Sleep(35);

            activity.SetEndTime(DateTime.Now);      // Error does nothing because it is not UTC    
            Assert.Equal(TimeSpan.Zero, activity.Duration);

            var stopTime = DateTime.UtcNow;
            activity.SetEndTime(stopTime);
            Assert.Equal(stopTime - startTime, activity.Duration);
        }

        /// <summary>
        /// Tests Activity Stop without timestamp
        /// </summary>
        [Fact]
        public void StopWithoutTimestamp()
        {
            var startTime = DateTime.UtcNow.AddSeconds(-1);
            var activity = new Activity("activity")
                .SetStartTime(startTime);

            activity.Start();
            Assert.Equal(startTime, activity.StartTimeUtc);

            activity.Stop();

            // DateTime.UtcNow is not precise on some platforms, but Activity stop time is precise
            // in this test we set start time, but not stop time and check duration.
            //
            // Let's check that duration is 1sec - 2 * maximum DateTime.UtcNow error or bigger.
            // As both start and stop timestamps may have error.
            // There is another test (ActivityDateTimeTests.StartStopReturnsPreciseDuration) 
            // that checks duration precision on netfx.
            Assert.InRange(activity.Duration.TotalMilliseconds, 1000 - 2 * MaxClockErrorMSec, double.MaxValue);
        }

        /// <summary>
        /// Tests Activity stack: creates a parent activity and child activity
        /// Verifies 
        ///  - Activity.Parent and ParentId correctness
        ///  - Baggage propagated from parent
        ///  - Tags are not propagated
        /// Stops child and checks Activity,Current is set to parent
        /// </summary>
        [Fact]
        public void ParentChild()
        {
            var parent = new Activity("parent")
                .AddBaggage("id1", "baggage from parent")
                .AddTag("tag1", "tag from parent");

            parent.Start();

            Assert.Equal(parent, Activity.Current);

            var child = new Activity("child");
            child.Start();
            Assert.Equal(parent, child.Parent);
            Assert.Equal(parent.Id, child.ParentId);

            //baggage from parent
            Assert.Equal("baggage from parent", child.GetBaggageItem("id1"));

            //no tags from parent
            var childTags = child.Tags.ToList();
            Assert.Equal(0, childTags.Count);

            child.Stop();
            Assert.Equal(parent, Activity.Current);

            parent.Stop();
            Assert.Equal(null, Activity.Current);
        }

        /// <summary>
        /// Tests wrong stop order, when parent is stopped before child
        /// </summary>
        [Fact]
        public void StopParent()
        {
            var parent = new Activity("parent");
            parent.Start();
            var child = new Activity("child");
            child.Start();

            parent.Stop();
            Assert.Equal(null, Activity.Current);
        }

        /// <summary>
        /// Tests that activity can not be stated twice
        /// </summary>
        [Fact]
        public void StartTwice()
        {
            var activity = new Activity("activity");
            activity.Start();
            var id = activity.Id;

            activity.Start();       // Error already started.  Does nothing.  
            Assert.Equal(id, activity.Id);
        }

        /// <summary>
        /// Tests that activity that has not been started can not be stopped
        /// </summary>
        [Fact]
        public void StopNotStarted()
        {
            var activity = new Activity("activity");
            activity.Stop();        // Error Does Nothing
            Assert.Equal(TimeSpan.Zero, activity.Duration);
        }

        /// <summary>
        /// Tests that second activity stop does not update Activity.Current
        /// </summary>
        [Fact]
        public void StopTwice()
        {
            var parent = new Activity("parent");
            parent.Start();

            var child1 = new Activity("child1");
            child1.Start();
            child1.Stop();

            var child2 = new Activity("child2");
            child2.Start();

            child1.Stop();

            Assert.Equal(child2, Activity.Current);
        }

        [Fact]
        public void DiagnosticSourceStartStop()
        {
            using (DiagnosticListener listener = new DiagnosticListener("Testing"))
            {
                DiagnosticSource source = listener;
                var observer = new TestObserver();

                using (listener.Subscribe(observer))
                {
                    var arguments = new { args = "arguments" };

                    var activity = new Activity("activity");

                    var stopWatch = Stopwatch.StartNew();
                    // Test Activity.Start
                    source.StartActivity(activity, arguments);

                    Assert.Equal(activity.OperationName + ".Start", observer.EventName);
                    Assert.Equal(arguments, observer.EventObject);
                    Assert.NotNull(observer.Activity);

                    Assert.NotEqual(activity.StartTimeUtc, default(DateTime));
                    Assert.Equal(TimeSpan.Zero, observer.Activity.Duration);

                    observer.Reset();

                    Thread.Sleep(100);

                    // Test Activity.Stop
                    source.StopActivity(activity, arguments);
                    stopWatch.Stop();
                    Assert.Equal(activity.OperationName + ".Stop", observer.EventName);
                    Assert.Equal(arguments, observer.EventObject);

                    // Confirm that duration is set. 
                    Assert.NotNull(observer.Activity);
                    Assert.InRange(observer.Activity.Duration, TimeSpan.FromTicks(1), TimeSpan.MaxValue);

                    // let's only check that Duration is set in StopActivity, we do not intend to check precision here
                    Assert.InRange(observer.Activity.Duration, TimeSpan.FromTicks(1), stopWatch.Elapsed.Add(TimeSpan.FromMilliseconds(2 * MaxClockErrorMSec)));
                } 
            }
        }

        /// <summary>
        /// Tests that Activity.Current flows correctly within async methods
        /// </summary>
        [Fact]
        public async Task ActivityCurrentFlowsWithAsyncSimple()
        {
            Activity activity = new Activity("activity").Start();
            Assert.Same(activity, Activity.Current);

            await Task.Run(() =>
            {
                Assert.Same(activity, Activity.Current);
            });

            Assert.Same(activity, Activity.Current);
        }

        /// <summary>
        /// Tests that Activity.Current flows correctly within async methods
        /// </summary>
        [Fact]
        public async Task ActivityCurrentFlowsWithAsyncComplex()
        {
            Activity originalActivity = Activity.Current;

            // Start an activity which spawns a task, but don't await it.
            // While that's running, start another, nested activity.
            Activity activity1 = new Activity("activity1").Start();
            Assert.Same(activity1, Activity.Current);

            SemaphoreSlim semaphore = new SemaphoreSlim(initialCount: 0);
            Task task = Task.Run(async () =>
            {
                // Wait until the semaphore is signaled.
                await semaphore.WaitAsync();
                Assert.Same(activity1, Activity.Current);
            });

            Activity activity2 = new Activity("activity2").Start();
            Assert.Same(activity2, Activity.Current);

            // Let task1 complete.
            semaphore.Release();
            await task;

            Assert.Same(activity2, Activity.Current);

            activity2.Stop();

            Assert.Same(activity1, Activity.Current);

            activity1.Stop();

            Assert.Same(originalActivity, Activity.Current);
        }

        private class TestObserver : IObserver<KeyValuePair<string, object>>
        {
            public string EventName { get; private set; }
            public object EventObject { get; private set; }

            public Activity Activity { get; private set; }

            public void OnNext(KeyValuePair<string, object> value)
            {
                EventName = value.Key;
                EventObject = value.Value;
                Activity = Activity.Current;
            }

            public void Reset()
            {
                EventName = null;
                EventObject = null;
                Activity = null;
            }

            public void OnCompleted() { }

            public void OnError(Exception error) { }
        }

        private const int MaxClockErrorMSec = 20;
    }
}
