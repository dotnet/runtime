using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            Assert.Throws<ArgumentException>(() => parent.SetParentId(null));
            Assert.Throws<ArgumentException>(() => parent.SetParentId(""));
            parent.SetParentId("1");
            Assert.Equal("1", parent.ParentId);
            Assert.Throws<InvalidOperationException>(() => parent.SetParentId("2"));

            parent.Start();
            var child = new Activity("child");
            child.Start();
            Assert.Throws<InvalidOperationException>(() => child.SetParentId("3"));
        }

        /// <summary>
        /// Tests activity SetParentId
        /// </summary>
        [Fact]
        public void ActivityIdOverflow()
        {
            //check parentId /abc.1.1...1.1.1.1.1 (126 bytes) and check last .1.1.1.1 is replaced with #overflow_suffix 8 bytes long
            var parentId = new StringBuilder("/abc");
            while (parentId.Length < 126)
                parentId.Append(".1");

            var activity = new Activity("activity")
                .SetParentId(parentId.ToString())
                .Start();

            Assert.Equal(
                parentId.ToString().Substring(0, parentId.Length - 8),
                activity.Id.Substring(0, activity.Id.Length - 9));
            Assert.Equal('#', activity.Id[activity.Id.Length - 9]);

            //check parentId /abc.1.1...1.012345678 (128 bytes) and check last .012345678 is replaced with #overflow_suffix 8 bytes long
            parentId = new StringBuilder("/abc");
            while (parentId.Length < 118)
                parentId.Append(".1");
            parentId.Append(".012345678");

            activity = new Activity("activity")
                .SetParentId(parentId.ToString())
                .Start();

            //last .012345678 will be replaced with #overflow_suffix 8 bytes long
            Assert.Equal(
                parentId.ToString().Substring(0, parentId.Length - 10),
                activity.Id.Substring(0, activity.Id.Length - 9));
            Assert.Equal('#', activity.Id[activity.Id.Length - 9]);
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
            var t1 = Task.Run(() => child1.Start());
            var t2 = Task.Run(() => child2.Start());
            Task.WhenAll(t1, t2).Wait();
#if DEBUG
            Assert.Equal($"{parent.Id}.{child1.OperationName}_1", child1.Id);
            Assert.Equal($"{parent.Id}.{child2.OperationName}_2", child2.Id);
#else
            Assert.Equal(parent.Id + ".1", child1.Id);
            Assert.Equal(parent.Id + ".2", child2.Id);
#endif
            child1.Stop();
            child2.Stop();
            var child3 = new Activity("child3");
            child3.Start();
#if DEBUG
            Assert.Equal($"{parent.Id}.{child3.OperationName}_3", child3.Id);
#else
            Assert.Equal(parent.Id + ".3", child3.Id);
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
            child1.Stop();
            var child2 = new Activity("child2");
            child2.SetParentId("123");
            child2.Start();
            Assert.NotEqual(child2.Id, child1.Id);
        }

        /// <summary>
        /// Tests Activity Start and Stop with timestamp
        /// </summary>
        [Fact]
        public void StartStopWithTimestamp()
        {
            var activity = new Activity("activity");
            Assert.Throws<InvalidOperationException>(() => activity.SetStartTime(DateTime.Now));

            var startTime = DateTime.UtcNow.AddSeconds(-1);
            activity.SetStartTime(startTime);

            activity.Start();
            Assert.Equal(startTime, activity.StartTimeUtc);

            Assert.Throws<InvalidOperationException>(() => activity.SetEndTime(DateTime.Now));
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
            Assert.Throws<InvalidOperationException>(() => activity.Start());
        }

        /// <summary>
        /// Tests that activity that has not been started can not be stopped
        /// </summary>
        [Fact]
        public void StopNotStarted()
        {
            Assert.Throws<InvalidOperationException>(() => new Activity("activity").Stop());
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
