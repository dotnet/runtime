using System.Collections.Generic;
using System.Linq;
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

            Assert.Equal(activity, activity.SetParentId("1"));
            Assert.Equal(2, activity.Tags.ToList().Count);
            Assert.Equal("1", activity.ParentId);
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
            Assert.NotEqual(default(DateTime), activity.StartTimeUtc);

            activity.Stop();
            Assert.Equal(null, Activity.Current);
        }

        /// <summary>
        /// Tests Activity Start and Stop with timestamp
        /// </summary>
        [Fact]
        public void StartStopWithTimestamp()
        {
            var startTime = DateTime.UtcNow.AddSeconds(-1);
            var activity = new Activity("activity")
                .SetStartTime(startTime);

            activity.Start();
            Assert.Equal(startTime, activity.StartTimeUtc);

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
            Assert.True(activity.Duration.TotalSeconds >= 1);
        }

        /// <summary>
        /// Tests Activity stack: creates a parent activity and child activity
        /// Verifies 
        ///  - Activity.Parent and ParentId corectness
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
            Assert.Equal(1, childTags.Count);
            Assert.Equal(child.ParentId, childTags[0].Value);

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
            var activity = new Activity("");
            activity.Start();
            Assert.Throws<InvalidOperationException>(() => activity.Start());
        }

        /// <summary>
        /// Tests that activity that has not been started can not be stopped
        /// </summary>
        [Fact]
        public void StopNotStarted()
        {
            Assert.Throws<InvalidOperationException>(() => new Activity("").Stop());
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

                    source.StartActivity(activity, arguments);
                    Assert.Equal(activity.OperationName + ".Start", observer.EventName);
                    Assert.Equal(arguments, observer.EventObject);

                    observer.Reset();

                    source.StopActivity(activity, arguments);
                    Assert.Equal(activity.OperationName + ".Stop", observer.EventName);
                    Assert.Equal(arguments, observer.EventObject);
                } 
            }
        }

        private class TestObserver : IObserver<KeyValuePair<string, object>>
        {
            public string EventName { get; private set; }
            public object EventObject { get; private set; }

            public void OnNext(KeyValuePair<string, object> value)
            {
                EventName = value.Key;
                EventObject = value.Value;
            }

            public void Reset()
            {
                EventName = null;
                EventObject = null;
            }

            public void OnCompleted() { }

            public void OnError(Exception error) { }
        }
    }
}
