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
    public class ActivityTests : IDisposable
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

            // Baggage starts off empty
            Assert.Null(activity.GetBaggageItem("some baggage"));
            Assert.Empty(activity.Baggage);

            const string Key = "some baggage";
            const string Value = "value";

            // Add items, get them individually and via an enumerable
            for (int i = 0; i < 3; i++)
            {
                Assert.Equal(activity, activity.AddBaggage(Key + i, Value + i));
                Assert.Equal(Value + i, activity.GetBaggageItem(Key + i));
                Assert.Equal(i + 1, activity.Baggage.Count());
                for (int j = 0; j < i; j++)
                {
                    Assert.Contains(new KeyValuePair<string, string>(Key + j, Value + j), activity.Baggage);
                }
            }

            // Now test baggage via child activities
            activity.Start();
            try
            {
                // Start a child
                var anotherActivity = new Activity("another activity");
                anotherActivity.Start();
                try
                {
                    // Its parent should match.
                    Assert.Same(activity, anotherActivity.Parent);

                    // All of the parent's baggage should be available via the child.
                    for (int i = 0; i < 3; i++)
                    {
                        Assert.Equal(Value + i, anotherActivity.GetBaggageItem(Key + i));
                        Assert.Contains(new KeyValuePair<string, string>(Key + i, Value + i), anotherActivity.Baggage);
                    }

                    // And we should be able to add additional baggage to the child, see that baggage, and still see the parent's.
                    Assert.Same(anotherActivity, anotherActivity.AddBaggage("hello", "world"));
                    Assert.Equal("world", anotherActivity.GetBaggageItem("hello"));
                    Assert.Equal(4, anotherActivity.Baggage.Count());
                    Assert.Equal(new KeyValuePair<string, string>("hello", "world"), anotherActivity.Baggage.First());
                    Assert.Equal(new KeyValuePair<string, string>(Key + 2, Value + 2), anotherActivity.Baggage.Skip(1).First());
                    Assert.Equal(new KeyValuePair<string, string>(Key + 1, Value + 1), anotherActivity.Baggage.Skip(2).First());
                    Assert.Equal(new KeyValuePair<string, string>(Key + 0, Value + 0), anotherActivity.Baggage.Skip(3).First());
                }
                finally
                {
                    anotherActivity.Stop();
                }
            }
            finally
            {
                activity.Stop();
            }
        }

        [Fact]
        public void TestBaggageOrderAndDuplicateKeys()
        {
            Activity a = new Activity("Baggage");
            a.AddBaggage("1", "1");
            a.AddBaggage("1", "2");
            a.AddBaggage("1", "3");
            a.AddBaggage("1", "4");

            int value = 4;

            foreach (KeyValuePair<string, string> kvp in a.Baggage)
            {
                Assert.Equal("1", kvp.Key);
                Assert.Equal(value.ToString(), kvp.Value);
                value--;
            }

            Assert.Equal("4", a.GetBaggageItem("1"));
        }

        [Fact]
        public void TestSetBaggage()
        {
            Activity a = new Activity("SetBaggage");
            Assert.Equal(0, a.Baggage.Count());

            a.SetBaggage("1", "1");
            a.SetBaggage("2", "2");
            a.SetBaggage("3", "3");
            a.SetBaggage("4", "4");
            a.SetBaggage("5", "5");

            Assert.Equal(5, a.Baggage.Count());
            Assert.Equal("1", a.GetBaggageItem("1"));
            Assert.Equal("2", a.GetBaggageItem("2"));
            Assert.Equal("3", a.GetBaggageItem("3"));
            Assert.Equal("4", a.GetBaggageItem("4"));
            Assert.Equal("5", a.GetBaggageItem("5"));

            // Check not added item
            Assert.Null(a.GetBaggageItem("6"));

            // Adding none existing key with null value is no-op
            a.SetBaggage("6", null);
            Assert.Equal(5, a.Baggage.Count());
            Assert.Null(a.GetBaggageItem("6"));

            // Check updated item
            a.SetBaggage("5", "5.1");
            Assert.Equal(5, a.Baggage.Count());
            Assert.Equal("5.1", a.GetBaggageItem("5"));

            // Add() always add new entry even we have matching key in the list.
            // Baggage always return last entered value
            a.AddBaggage("5", "5.2");
            Assert.Equal(6, a.Baggage.Count());
            Assert.Equal("5.2", a.GetBaggageItem("5"));

            // Now Remove first duplicated item
            a.SetBaggage("5", null);
            Assert.Equal(5, a.Baggage.Count());
            Assert.Equal("5.1", a.GetBaggageItem("5"));

            // Now Remove second item
            a.SetBaggage("5", null);
            Assert.Equal(4, a.Baggage.Count());
            Assert.Null(a.GetBaggageItem("5"));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestBaggageWithChainedActivities()
        {
            RemoteExecutor.Invoke(() => {
                Activity a1 = new Activity("a1");
                a1.Start();

                a1.AddBaggage("1", "1");
                a1.AddBaggage("2", "2");

                IEnumerable<KeyValuePair<string, string>> baggages = a1.Baggage;
                Assert.Equal(2, baggages.Count());
                Assert.Equal(new KeyValuePair<string, string>("2", "2"), baggages.ElementAt(0));
                Assert.Equal(new KeyValuePair<string, string>("1", "1"), baggages.ElementAt(1));

                Activity a2 = new Activity("a2");
                a2.Start();

                a2.AddBaggage("3", "3");
                baggages = a2.Baggage;
                Assert.Equal(3, baggages.Count());
                Assert.Equal(new KeyValuePair<string, string>("3", "3"), baggages.ElementAt(0));
                Assert.Equal(new KeyValuePair<string, string>("2", "2"), baggages.ElementAt(1));
                Assert.Equal(new KeyValuePair<string, string>("1", "1"), baggages.ElementAt(2));

                Activity a3 = new Activity("a3");
                a3.Start();

                a3.AddBaggage("4", "4");
                baggages = a3.Baggage;
                Assert.Equal(4, baggages.Count());
                Assert.Equal(new KeyValuePair<string, string>("4", "4"), baggages.ElementAt(0));
                Assert.Equal(new KeyValuePair<string, string>("3", "3"), baggages.ElementAt(1));
                Assert.Equal(new KeyValuePair<string, string>("2", "2"), baggages.ElementAt(2));
                Assert.Equal(new KeyValuePair<string, string>("1", "1"), baggages.ElementAt(3));

                a3.Dispose();
                a2.Dispose();
                a1.Dispose();
            }).Dispose();
        }

        /// <summary>
        /// Tests Tags operations
        /// </summary>
        [Fact]
        public void Tags()
        {
            var activity = new Activity("activity");
            Assert.Empty(activity.Tags);

            const string Key = "some tag";
            const string Value = "value";
            for (int i = 0; i < 3; i++)
            {
                Assert.Equal(activity, activity.AddTag(Key + i, Value + i));
                List<KeyValuePair<string, string>> tags = activity.Tags.ToList();
                Assert.Equal(i + 1, tags.Count);
                Assert.Equal(tags[i].Key, Key + i);
                Assert.Equal(tags[i].Value, Value + i);
            }
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
        /// Tests activity start and stop
        /// Checks Activity.Current correctness, Id generation
        /// </summary>
        [Fact]
        public void StartStop()
        {
            var activity = new Activity("activity");
            Assert.Null(Activity.Current);
            activity.Start();
            Assert.Equal(activity, Activity.Current);
            Assert.Null(activity.Parent);
            Assert.NotNull(activity.Id);
            Assert.NotNull(activity.RootId);
            Assert.NotEqual(default(DateTime), activity.StartTimeUtc);

            activity.Stop();
            Assert.Null(Activity.Current);
        }

        /// <summary>
        /// Tests Activity.IsStopped
        /// </summary>
        [Fact]
        public void IsStoppedTest()
        {
            using var activity = new Activity("activity");
            Assert.False(activity.IsStopped);
            activity.Start();
            Assert.False(activity.IsStopped);
            Assert.Equal(TimeSpan.Zero, activity.Duration);
            activity.Stop();
            Assert.NotEqual(TimeSpan.Zero, activity.Duration);
            Assert.True(activity.IsStopped);

            using var activity1 = new Activity("activity");
            Assert.False(activity1.IsStopped);
            activity1.Start();
            Assert.False(activity1.IsStopped);
            activity1.SetEndTime(DateTime.UtcNow.AddMinutes(1)); // Setting end time shouldn't mark the activity as stopped
            Assert.False(activity1.IsStopped);
            activity1.Stop();
            Assert.True(activity1.IsStopped);

            //
            // Validate when receiving Start/Stop Activity events
            //

            using ActivitySource aSource = new ActivitySource("TestActivityIsStopped");
            using ActivityListener listener = new ActivityListener();

            listener.ShouldListenTo = (activitySource) => activitySource.Name == "TestActivityIsStopped";
            listener.Sample = (ref ActivityCreationOptions<ActivityContext> activityOptions) => ActivitySamplingResult.AllData;
            listener.ActivityStarted = a => Assert.False(a.IsStopped);
            listener.ActivityStopped = a => Assert.True(a.IsStopped);
            ActivitySource.AddActivityListener(listener);
            Activity sourceActivity;
            using (sourceActivity = aSource.StartActivity("a1"))
            {
                Assert.NotNull(sourceActivity);
                Assert.False(sourceActivity.IsStopped);
            }
            Assert.True(sourceActivity.IsStopped);
        }

        /// <summary>
        /// Tests Id generation
        /// </summary>
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
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
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void IdGenerationInternalParent()
        {
            var parent = new Activity("parent");
            parent.SetIdFormat(ActivityIdFormat.Hierarchical);
            parent.Start();
            var child1 = new Activity("child1");
            var child2 = new Activity("child2");
            //start 2 children in different execution contexts
            Task.Run(() => child1.Start()).Wait();
            Task.Run(() => child2.Start()).Wait();

            // In Debug builds of System.Diagnostics.DiagnosticSource, the child operation Id will be constructed as follows
            // "|parent.RootId.<child.OperationName.Replace(., -)>-childCount.".
            // This is for debugging purposes to know which operation the child Id is comming from.
            //
            // In Release builds of System.Diagnostics.DiagnosticSource, it will not contain the operation name to keep it simple and it will be as
            // "|parent.RootId.childCount.".

            string child1DebugString = $"|{parent.RootId}.{child1.OperationName}-1.";
            string child2DebugString = $"|{parent.RootId}.{child2.OperationName}-2.";
            string child1ReleaseString = $"|{parent.RootId}.1.";
            string child2ReleaseString = $"|{parent.RootId}.2.";

            AssertExtensions.AtLeastOneEquals(child1DebugString, child1ReleaseString, child1.Id);
            AssertExtensions.AtLeastOneEquals(child2DebugString, child2ReleaseString, child2.Id);

            Assert.Equal(parent.RootId, child1.RootId);
            Assert.Equal(parent.RootId, child2.RootId);
            child1.Stop();
            child2.Stop();
            var child3 = new Activity("child3");
            child3.Start();

            string child3DebugString = $"|{parent.RootId}.{child3.OperationName}-3.";
            string child3ReleaseString = $"|{parent.RootId}.3.";

            AssertExtensions.AtLeastOneEquals(child3DebugString, child3ReleaseString, child3.Id);

            var grandChild = new Activity("grandChild");
            grandChild.Start();

            child3DebugString = $"{child3.Id}{grandChild.OperationName}-1.";
            child3ReleaseString = $"{child3.Id}1.";

            AssertExtensions.AtLeastOneEquals(child3DebugString, child3ReleaseString, grandChild.Id);
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
            var parentIds = new[]{
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

        public static bool IdIsW3CFormat(string id)
        {
            if (id.Length != 55)
                return false;
            if (id[2] != '-')
                return false;
            if (id[35] != '-')
                return false;
            if (id[52] != '-')
                return false;
            return Regex.IsMatch(id, "^[0-9a-f]{2}-[0-9a-f]{32}-[0-9a-f]{16}-[0-9a-f]{2}$");
        }

        public static bool IsLowerCaseHex(string s)
        {
            return Regex.IsMatch(s, "^[0-9a-f]*$");
        }

        /****** ActivityTraceId tests *****/
        [Fact]
        public void ActivityTraceIdTests()
        {
            Span<byte> idBytes1 = stackalloc byte[16];
            Span<byte> idBytes2 = stackalloc byte[16];

            // Empty Constructor
            string zeros = "00000000000000000000000000000000";
            ActivityTraceId emptyId = new ActivityTraceId();
            Assert.Equal(zeros, emptyId.ToHexString());
            emptyId.CopyTo(idBytes1);
            Assert.Equal(new byte[16], idBytes1.ToArray());

            Assert.True(emptyId == new ActivityTraceId());
            Assert.True(!(emptyId != new ActivityTraceId()));
            Assert.True(emptyId.Equals(new ActivityTraceId()));
            Assert.True(emptyId.Equals((object)new ActivityTraceId()));
            Assert.Equal(new ActivityTraceId().GetHashCode(), emptyId.GetHashCode());

            // NewActivityTraceId
            ActivityTraceId newId1 = ActivityTraceId.CreateRandom();
            Assert.True(IsLowerCaseHex(newId1.ToHexString()));
            Assert.Equal(32, newId1.ToHexString().Length);

            ActivityTraceId newId2 = ActivityTraceId.CreateRandom();
            Assert.Equal(32, newId1.ToHexString().Length);
            Assert.NotEqual(newId1.ToHexString(), newId2.ToHexString());

            // Test equality
            Assert.True(newId1 != newId2);
            Assert.True(!(newId1 == newId2));
            Assert.True(!(newId1.Equals(newId2)));
            Assert.True(!(newId1.Equals((object)newId2)));
            Assert.NotEqual(newId1.GetHashCode(), newId2.GetHashCode());

            ActivityTraceId newId3 = ActivityTraceId.CreateFromString("00000000000000000000000000000001".AsSpan());
            Assert.True(newId3 != emptyId);
            Assert.True(!(newId3 == emptyId));
            Assert.True(!(newId3.Equals(emptyId)));
            Assert.True(!(newId3.Equals((object)emptyId)));
            Assert.NotEqual(newId3.GetHashCode(), emptyId.GetHashCode());

            // Use in Dictionary (this does assume we have no collisions in IDs over 100 tries (very good).
            var dict = new Dictionary<ActivityTraceId, string>();
            for (int i = 0; i < 100; i++)
            {
                var newId7 = ActivityTraceId.CreateRandom();
                dict[newId7] = newId7.ToHexString();
            }
            int ctr = 0;
            foreach (string value in dict.Values)
            {
                string valueInDict;
                Assert.True(dict.TryGetValue(ActivityTraceId.CreateFromString(value.AsSpan()), out valueInDict));
                Assert.Equal(value, valueInDict);
                ctr++;
            }
            Assert.Equal(100, ctr);     // We got out what we put in.

            // AsBytes and Byte constructor.
            newId2.CopyTo(idBytes2);
            ActivityTraceId newId2Clone = ActivityTraceId.CreateFromBytes(idBytes2);
            Assert.Equal(newId2.ToHexString(), newId2Clone.ToHexString());
            newId2Clone.CopyTo(idBytes1);
            Assert.Equal(idBytes2.ToArray(), idBytes1.ToArray());

            Assert.True(newId2 == newId2Clone);
            Assert.True(newId2.Equals(newId2Clone));
            Assert.True(newId2.Equals((object)newId2Clone));
            Assert.Equal(newId2.GetHashCode(), newId2Clone.GetHashCode());

            // String constructor and ToHexString().
            string idStr = "0123456789abcdef0123456789abcdef";
            ActivityTraceId id = ActivityTraceId.CreateFromString(idStr.AsSpan());
            Assert.Equal(idStr, id.ToHexString());

            // Utf8 Constructor.
            byte[] idUtf8 = Encoding.UTF8.GetBytes(idStr);
            ActivityTraceId id1 = ActivityTraceId.CreateFromUtf8String(idUtf8);
            Assert.Equal(idStr, id1.ToHexString());

            // ToString
            Assert.Equal(idStr, id.ToString());
        }

        /****** ActivitySpanId tests *****/
        [Fact]
        public void ActivitySpanIdTests()
        {
            Span<byte> idBytes1 = stackalloc byte[8];
            Span<byte> idBytes2 = stackalloc byte[8];

            // Empty Constructor
            string zeros = "0000000000000000";
            ActivitySpanId emptyId = new ActivitySpanId();
            Assert.Equal(zeros, emptyId.ToHexString());
            emptyId.CopyTo(idBytes1);
            Assert.Equal(new byte[8], idBytes1.ToArray());

            Assert.True(emptyId == new ActivitySpanId());
            Assert.True(!(emptyId != new ActivitySpanId()));
            Assert.True(emptyId.Equals(new ActivitySpanId()));
            Assert.True(emptyId.Equals((object)new ActivitySpanId()));
            Assert.Equal(new ActivitySpanId().GetHashCode(), emptyId.GetHashCode());

            // NewActivitySpanId
            ActivitySpanId newId1 = ActivitySpanId.CreateRandom();
            Assert.True(IsLowerCaseHex(newId1.ToHexString()));
            Assert.Equal(16, newId1.ToHexString().Length);

            ActivitySpanId newId2 = ActivitySpanId.CreateRandom();
            Assert.Equal(16, newId1.ToHexString().Length);
            Assert.NotEqual(newId1.ToHexString(), newId2.ToHexString());

            // Test equality
            Assert.True(newId1 != newId2);
            Assert.True(!(newId1 == newId2));
            Assert.True(!(newId1.Equals(newId2)));
            Assert.True(!(newId1.Equals((object)newId2)));
            Assert.NotEqual(newId1.GetHashCode(), newId2.GetHashCode());

            ActivitySpanId newId3 = ActivitySpanId.CreateFromString("0000000000000001".AsSpan());
            Assert.True(newId3 != emptyId);
            Assert.True(!(newId3 == emptyId));
            Assert.True(!(newId3.Equals(emptyId)));
            Assert.True(!(newId3.Equals((object)emptyId)));
            Assert.NotEqual(newId3.GetHashCode(), emptyId.GetHashCode());

            // Use in Dictionary (this does assume we have no collisions in IDs over 100 tries (very good).
            var dict = new Dictionary<ActivitySpanId, string>();
            for (int i = 0; i < 100; i++)
            {
                var newId7 = ActivitySpanId.CreateRandom();
                dict[newId7] = newId7.ToHexString();
            }
            int ctr = 0;
            foreach (string value in dict.Values)
            {
                string valueInDict;
                Assert.True(dict.TryGetValue(ActivitySpanId.CreateFromString(value.AsSpan()), out valueInDict));
                Assert.Equal(value, valueInDict);
                ctr++;
            }
            Assert.Equal(100, ctr);     // We got out what we put in.

            // AsBytes and Byte constructor.
            newId2.CopyTo(idBytes2);
            ActivitySpanId newId2Clone = ActivitySpanId.CreateFromBytes(idBytes2);
            Assert.Equal(newId2.ToHexString(), newId2Clone.ToHexString());
            newId2Clone.CopyTo(idBytes1);
            Assert.Equal(idBytes2.ToArray(), idBytes1.ToArray());

            Assert.True(newId2 == newId2Clone);
            Assert.True(newId2.Equals(newId2Clone));
            Assert.True(newId2.Equals((object)newId2Clone));
            Assert.Equal(newId2.GetHashCode(), newId2Clone.GetHashCode());

            // String constructor and ToHexString().
            string idStr = "0123456789abcdef";
            ActivitySpanId id = ActivitySpanId.CreateFromString(idStr.AsSpan());
            Assert.Equal(idStr, id.ToHexString());

            // Utf8 Constructor.
            byte[] idUtf8 = Encoding.UTF8.GetBytes(idStr);
            ActivitySpanId id1 = ActivitySpanId.CreateFromUtf8String(idUtf8);
            Assert.Equal(idStr, id1.ToHexString());

            // ToString
            Assert.Equal(idStr, id.ToString());
        }

        /****** WC3 Format tests *****/

        [Fact]
        public void IdFormat_W3CIsDefaultForNet5()
        {
            Activity activity = new Activity("activity1");
            activity.Start();
            Assert.Equal(PlatformDetection.IsNetCore ? ActivityIdFormat.W3C : ActivityIdFormat.Hierarchical, activity.IdFormat);
        }

        [Fact]
        public void IdFormat_W3CWhenParentIdIsW3C()
        {
            Activity activity = new Activity("activity2");
            activity.SetParentId("00-0123456789abcdef0123456789abcdef-0123456789abcdef-00");
            activity.Start();
            Assert.Equal(ActivityIdFormat.W3C, activity.IdFormat);
            Assert.Equal("0123456789abcdef0123456789abcdef", activity.TraceId.ToHexString());
            Assert.Equal("0123456789abcdef", activity.ParentSpanId.ToHexString());
            Assert.Equal(ActivityTraceFlags.None, activity.ActivityTraceFlags);
            Assert.False(activity.Recorded);
            Assert.True(IdIsW3CFormat(activity.Id));
        }

        [Fact]
        public void IdFormat_W3CValidVersions()
        {
            Activity activity0 = new Activity("activity0");
            Activity activity1 = new Activity("activity1");
            Activity activity2 = new Activity("activity2");
            Activity activity3 = new Activity("activity3");
            activity0.SetParentId("01-0123456789abcdef0123456789abcdef-0123456789abcdef-00");
            activity0.Start();
            activity1.SetParentId("cc-1123456789abcdef0123456789abcdef-1123456789abcdef-00");
            activity1.Start();
            activity2.SetParentId("fe-2123456789abcdef0123456789abcdef-2123456789abcdef-00");
            activity2.Start();
            activity3.SetParentId("ef-3123456789abcdef0123456789abcdef-3123456789abcdef-00");
            activity3.Start();

            Assert.Equal(ActivityIdFormat.W3C, activity0.IdFormat);
            Assert.Equal(ActivityIdFormat.W3C, activity1.IdFormat);
            Assert.Equal(ActivityIdFormat.W3C, activity2.IdFormat);
            Assert.Equal(ActivityIdFormat.W3C, activity3.IdFormat);
            Assert.Equal("0123456789abcdef0123456789abcdef", activity0.TraceId.ToHexString());
            Assert.Equal("1123456789abcdef0123456789abcdef", activity1.TraceId.ToHexString());
            Assert.Equal("2123456789abcdef0123456789abcdef", activity2.TraceId.ToHexString());
            Assert.Equal("3123456789abcdef0123456789abcdef", activity3.TraceId.ToHexString());
            Assert.Equal("0123456789abcdef", activity0.ParentSpanId.ToHexString());
            Assert.Equal("1123456789abcdef", activity1.ParentSpanId.ToHexString());
            Assert.Equal("2123456789abcdef", activity2.ParentSpanId.ToHexString());
            Assert.Equal("3123456789abcdef", activity3.ParentSpanId.ToHexString());
        }

        [Fact]
        public void IdFormat_W3CInvalidVersionFF()
        {
            Activity activity = new Activity("activity");
            activity.SetParentId("ff-0123456789abcdef0123456789abcdef-0123456789abcdef-00");
            activity.Start();

            Assert.Equal(ActivityIdFormat.Hierarchical, activity.IdFormat);
        }

        [Fact]
        public void IdFormat_W3CWhenTraceIdAndSpanIdProvided()
        {
            Activity activity = new Activity("activity3");
            ActivityTraceId activityTraceId = ActivityTraceId.CreateRandom();
            activity.SetParentId(activityTraceId, ActivitySpanId.CreateRandom());
            activity.Start();
            Assert.Equal(ActivityIdFormat.W3C, activity.IdFormat);
            Assert.Equal(activityTraceId.ToHexString(), activity.TraceId.ToHexString());
            Assert.Equal(ActivityTraceFlags.None, activity.ActivityTraceFlags);
            Assert.False(activity.Recorded);
            Assert.True(IdIsW3CFormat(activity.Id));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void IdFormat_W3CWhenDefaultIsW3C()
        {
            RemoteExecutor.Invoke(() =>
            {
                Activity.DefaultIdFormat = ActivityIdFormat.W3C;
                Activity activity = new Activity("activity4");
                activity.Start();
                Assert.Equal(ActivityIdFormat.W3C, activity.IdFormat);
                Assert.True(IdIsW3CFormat(activity.Id));
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void IdFormat_WithTheEnvironmentSwitch()
        {
            var psi = new ProcessStartInfo();
            psi.Environment.Add("DOTNET_SYSTEM_DIAGNOSTICS_DEFAULTACTIVITYIDFORMATISHIERARCHIAL", "true");

            RemoteExecutor.Invoke(() =>
            {
                Activity activity = new Activity("activity15");
                activity.Start();
                 Assert.Equal(ActivityIdFormat.Hierarchical, activity.IdFormat);
            }, new RemoteInvokeOptions() { StartInfo = psi }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void IdFormat_HierarchicalWhenDefaultIsW3CButHierarchicalParentId()
        {
            RemoteExecutor.Invoke(() =>
            {
                Activity.DefaultIdFormat = ActivityIdFormat.W3C;
                Activity activity = new Activity("activity5");
                string parentId = "|a000b421-5d183ab6.1.";
                activity.SetParentId(parentId);
                activity.Start();
                Assert.Equal(ActivityIdFormat.Hierarchical, activity.IdFormat);
                Assert.StartsWith(parentId, activity.Id);
            }).Dispose();
        }

        [Fact]
        public void IdFormat_ZeroTraceIdAndSpanIdWithW3CFormat()
        {
            Activity activity = new Activity("activity");
            activity.Start();

            if (PlatformDetection.IsNetCore)
            {
                Assert.Equal(ActivityIdFormat.W3C, activity.IdFormat);
                Assert.NotEqual("00000000000000000000000000000000", activity.TraceId.ToHexString());
                Assert.NotEqual("0000000000000000", activity.SpanId.ToHexString());
            }
            else
            {
                Assert.Equal(ActivityIdFormat.Hierarchical, activity.IdFormat);
                Assert.Equal("00000000000000000000000000000000", activity.TraceId.ToHexString());
                Assert.Equal("0000000000000000", activity.SpanId.ToHexString());
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void IdFormat_W3CWhenForcedAndHierarchicalParentId()
        {
            RemoteExecutor.Invoke(() =>
            {
                Activity.DefaultIdFormat = ActivityIdFormat.W3C;
                Activity.ForceDefaultIdFormat = true;
                Activity activity = new Activity("activity6");
                activity.SetParentId("|a000b421-5d183ab6.1.");
                activity.Start();
                Assert.Equal(ActivityIdFormat.W3C, activity.IdFormat);
                Assert.True(IdIsW3CFormat(activity.Id));
                Assert.NotEqual("00000000000000000000000000000000", activity.TraceId.ToHexString());
                Assert.NotEqual("0000000000000000", activity.SpanId.ToHexString());
            }).Dispose();
        }

        [Fact]
        public void TraceStateString_InheritsFromParent()
        {
            Activity parent = new Activity("parent");
            parent.SetIdFormat(ActivityIdFormat.W3C);
            const string testString = "MyTestString";
            parent.TraceStateString = testString;
            parent.Start();
            Assert.Equal(testString, parent.TraceStateString);

            Activity activity = new Activity("activity7");
            activity.Start();
            Assert.Equal(ActivityIdFormat.W3C, activity.IdFormat);
            Assert.True(IdIsW3CFormat(activity.Id));
            Assert.Equal(testString, activity.TraceStateString);

            // Update child
            string childTestString = "ChildTestString";
            activity.TraceStateString = childTestString;

            // Confirm that child sees update, but parent does not
            Assert.Equal(childTestString, activity.TraceStateString);
            Assert.Equal(testString, parent.TraceStateString);

            // Update parent
            string parentTestString = "newTestString";
            parent.TraceStateString = parentTestString;

            // Confirm that parent sees update but child does not.
            Assert.Equal(childTestString, activity.TraceStateString);
            Assert.Equal(parentTestString, parent.TraceStateString);
        }

        [Fact]
        public void TraceId_W3CUppercaseCharsNotSupportedAndDoesNotThrow()
        {
            Activity activity = new Activity("activity8");
            activity.SetParentId("00-0123456789ABCDEF0123456789ABCDEF-0123456789ABCDEF-01");
            activity.Start();
            Assert.Equal(ActivityIdFormat.W3C, activity.IdFormat);
            Assert.True(IdIsW3CFormat(activity.Id));
        }

        [Fact]
        public void TraceId_W3CNonHexCharsNotSupportedAndDoesNotThrow()
        {
            Activity activity = new Activity("activity9");
            activity.SetParentId("00-xyz3456789abcdef0123456789abcdef-0123456789abcdef-01");
            activity.Start();
            Assert.Equal(ActivityIdFormat.W3C, activity.IdFormat);
            Assert.True(IdIsW3CFormat(activity.Id));
        }

        [Fact]
        public void ParentSpanId_W3CNonHexCharsNotSupportedAndDoesNotThrow()
        {
            Activity activity = new Activity("activity10");
            activity.SetParentId("00-0123456789abcdef0123456789abcdef-x123456789abcdef-01");
            activity.Start();
            Assert.Equal(ActivityIdFormat.W3C, activity.IdFormat);
            Assert.True(IdIsW3CFormat(activity.Id));
            Assert.Equal("0000000000000000", activity.ParentSpanId.ToHexString());
            Assert.Equal("0123456789abcdef0123456789abcdef", activity.TraceId.ToHexString());
        }

        [Fact]
        public void Version_W3CNonHexCharsNotSupportedAndDoesNotThrow()
        {
            Activity activity = new Activity("activity");
            activity.SetParentId("0.-0123456789abcdef0123456789abcdef-0123456789abcdef-00");
            activity.Start();

            Assert.Equal(ActivityIdFormat.Hierarchical, activity.IdFormat);
        }

        [Fact]
        public void Version_W3CNonHexCharsNotSupportedAndDoesNotThrow_ForceW3C()
        {
            Activity activity = new Activity("activity");
            activity.SetIdFormat(ActivityIdFormat.W3C);
            activity.SetParentId("0.-0123456789abcdef0123456789abcdef-0123456789abcdef-00");
            activity.Start();

            Assert.Equal(ActivityIdFormat.W3C, activity.IdFormat);
            Assert.NotEqual("0123456789abcdef0123456789abcdef", activity.TraceId.ToHexString());
            Assert.Equal(default, activity.ParentSpanId);
            Assert.True(IdIsW3CFormat(activity.Id));
        }

        [Fact]
        public void Options_W3CNonHexCharsNotSupportedAndDoesNotThrow()
        {
            Activity activity = new Activity("activity");
            activity.SetParentId("00-0123456789abcdef0123456789abcdef-0123456789abcdef-.1");
            activity.Start();

            Assert.Equal(ActivityIdFormat.W3C, activity.IdFormat);
            Assert.True(IdIsW3CFormat(activity.Id));

            Assert.Equal("0123456789abcdef0123456789abcdef", activity.TraceId.ToHexString());
            Assert.Equal("0123456789abcdef", activity.ParentSpanId.ToHexString());
            Assert.Equal(ActivityTraceFlags.None, activity.ActivityTraceFlags);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void IdFormat_W3CForcedOverridesParentActivityIdFormat()
        {
            RemoteExecutor.Invoke(() =>
            {
                Activity.DefaultIdFormat = ActivityIdFormat.W3C;
                Activity.ForceDefaultIdFormat = true;

                Activity parent = new Activity("parent").Start();
                Activity activity = new Activity("child").Start();
                Assert.Equal(parent.SpanId.ToHexString(), activity.ParentSpanId.ToHexString());
            }).Dispose();
        }

        [Fact]
        public void SetIdFormat_CanSetHierarchicalBeforeStart()
        {
            Activity activity = new Activity("activity1");
            Assert.Equal(ActivityIdFormat.Unknown, activity.IdFormat);
            activity.SetIdFormat(ActivityIdFormat.Hierarchical);
            Assert.Equal(ActivityIdFormat.Hierarchical, activity.IdFormat);
            activity.Start();
            Assert.Equal(ActivityIdFormat.Hierarchical, activity.IdFormat);

            // cannot change after activity starts
            activity.SetIdFormat(ActivityIdFormat.W3C);
            Assert.Equal(ActivityIdFormat.Hierarchical, activity.IdFormat);
        }

        [Fact]
        public void SetIdFormat_CanSetW3CBeforeStart()
        {
            Activity activity = new Activity("activity2");
            Assert.Equal(ActivityIdFormat.Unknown, activity.IdFormat);
            activity.SetIdFormat(ActivityIdFormat.W3C);
            Assert.Equal(ActivityIdFormat.W3C, activity.IdFormat);
            activity.Start();
            Assert.Equal(ActivityIdFormat.W3C, activity.IdFormat);
            Assert.True(IdIsW3CFormat(activity.Id));
        }

        [Fact]
        public void SetIdFormat_CanSetW3CAfterHierarchicalParentIsSet()
        {
            Activity activity = new Activity("activity3");
            activity.SetParentId("|foo.bar.");
            activity.SetIdFormat(ActivityIdFormat.W3C);
            Assert.Equal(ActivityIdFormat.W3C, activity.IdFormat);
            activity.Start();
            Assert.Equal(ActivityIdFormat.W3C, activity.IdFormat);
            Assert.Equal("|foo.bar.", activity.ParentId);
            Assert.True(IdIsW3CFormat(activity.Id));
        }

        [Fact]
        public void SetIdFormat_CanSetHierarchicalAfterW3CParentIsSet()
        {
            Activity activity = new Activity("activity4");
            activity.SetParentId("00-0123456789abcdef0123456789abcdef-0123456789abcdef-00");
            activity.SetIdFormat(ActivityIdFormat.Hierarchical);
            Assert.Equal(ActivityIdFormat.Hierarchical, activity.IdFormat);
            activity.Start();
            Assert.Equal(ActivityIdFormat.Hierarchical, activity.IdFormat);
            Assert.Equal("00-0123456789abcdef0123456789abcdef-0123456789abcdef-00", activity.ParentId);
            Assert.True(activity.Id[0] == '|');
        }

        [Fact]
        public void SetIdFormat_CanSetAndOverrideBeforeStart()
        {
            Activity activity = new Activity("activity5");
            Assert.Equal(ActivityIdFormat.Unknown, activity.IdFormat);
            activity.SetIdFormat(ActivityIdFormat.Hierarchical);
            Assert.Equal(ActivityIdFormat.Hierarchical, activity.IdFormat);
            activity.SetIdFormat(ActivityIdFormat.W3C);
            Assert.Equal(ActivityIdFormat.W3C, activity.IdFormat);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SetIdFormat_OverridesForcedW3C()
        {
            RemoteExecutor.Invoke(() =>
            {
                Activity.DefaultIdFormat = ActivityIdFormat.W3C;
                Activity.ForceDefaultIdFormat = true;
                Activity activity = new Activity("activity7");
                activity.SetIdFormat(ActivityIdFormat.Hierarchical);
                activity.Start();
                Assert.Equal(ActivityIdFormat.Hierarchical, activity.IdFormat);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SetIdFormat_OverridesForcedHierarchical()
        {
            RemoteExecutor.Invoke(() =>
            {
                Activity.DefaultIdFormat = ActivityIdFormat.Hierarchical;
                Activity.ForceDefaultIdFormat = true;
                Activity activity = new Activity("activity8");
                activity.SetIdFormat(ActivityIdFormat.W3C);
                activity.Start();
                Assert.Equal(ActivityIdFormat.W3C, activity.IdFormat);
            }).Dispose();
        }

        [Fact]
        public void SetIdFormat_OverridesParentHierarchicalFormat()
        {
            Activity parent = new Activity("parent")
                .SetIdFormat(ActivityIdFormat.Hierarchical)
                .Start();

            Activity child = new Activity("child")
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start();

            Assert.Equal(ActivityIdFormat.W3C, child.IdFormat);
        }

        [Fact]
        public void SetIdFormat_OverridesParentW3CFormat()
        {
            Activity parent = new Activity("parent")
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start();

            Activity child = new Activity("child")
                .SetIdFormat(ActivityIdFormat.Hierarchical)
                .Start();

            Assert.Equal(ActivityIdFormat.Hierarchical, child.IdFormat);
        }

        [Fact]
        public void TraceIdBeforeStart_FromTraceparentHeader()
        {
            Activity activity = new Activity("activity1");
            activity.SetParentId("00-0123456789abcdef0123456789abcdef-0123456789abcdef-01");
            Assert.Equal("0123456789abcdef0123456789abcdef", activity.TraceId.ToHexString());
        }

        [Fact]
        public void TraceIdBeforeStart_FromExplicitTraceId()
        {
            Activity activity = new Activity("activity2");
            activity.SetParentId(
                ActivityTraceId.CreateFromString("0123456789abcdef0123456789abcdef".AsSpan()),
                ActivitySpanId.CreateFromString("0123456789abcdef".AsSpan()));

            Assert.Equal("0123456789abcdef0123456789abcdef", activity.TraceId.ToHexString());
        }

        [Fact]
        public void TraceIdBeforeStart_FromInProcParent()
        {
            Activity parent = new Activity("parent");
            parent.SetParentId("00-0123456789abcdef0123456789abcdef-0123456789abcdef-01");
            parent.Start();

            Activity activity = new Activity("child");
            activity.Start();
            Assert.Equal("0123456789abcdef0123456789abcdef", activity.TraceId.ToHexString());
        }

        [Fact]
        public void TraceIdBeforeStart_FromInvalidTraceparentHeader()
        {
            Activity activity = new Activity("activity");
            activity.SetParentId("123");
            Assert.Equal("00000000000000000000000000000000", activity.TraceId.ToHexString());
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TraceIdBeforeStart_NoParent()
        {
            RemoteExecutor.Invoke(() =>
            {
                Activity.DefaultIdFormat = ActivityIdFormat.W3C;
                Activity.ForceDefaultIdFormat = true;

                Activity activity = new Activity("activity3");
                Assert.Equal("00000000000000000000000000000000", activity.TraceId.ToHexString());
            }).Dispose();
        }

        [Fact]
        public void RootIdBeforeStartTests()
        {
            Activity activity = new Activity("activity1");
            Assert.Null(activity.RootId);
            activity.SetParentId("|0123456789abcdef0123456789abcdef.0123456789abcdef.");
            Assert.Equal("0123456789abcdef0123456789abcdef", activity.RootId);
        }

        [Fact]
        public void ActivityTraceFlagsTests()
        {
            Activity activity;

            // Set the 'Recorded' bit by using SetParentId with a -01 flags.
            activity = new Activity("activity1");
            activity.SetParentId("00-0123456789abcdef0123456789abcdef-0123456789abcdef-01");
            activity.Start();
            Assert.Equal(ActivityIdFormat.W3C, activity.IdFormat);
            Assert.Equal("0123456789abcdef0123456789abcdef", activity.TraceId.ToHexString());
            Assert.Equal("0123456789abcdef", activity.ParentSpanId.ToHexString());
            Assert.True(IdIsW3CFormat(activity.Id));
            Assert.Equal($"00-0123456789abcdef0123456789abcdef-{activity.SpanId.ToHexString()}-01", activity.Id);
            Assert.Equal(ActivityTraceFlags.Recorded, activity.ActivityTraceFlags);
            Assert.True(activity.Recorded);
            activity.Stop();

            // Set the 'Recorded' bit by using SetParentId by using the TraceId, SpanId, ActivityTraceFlags overload
            activity = new Activity("activity2");
            ActivityTraceId activityTraceId = ActivityTraceId.CreateRandom();
            activity.SetParentId(activityTraceId, ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded);
            activity.Start();
            Assert.Equal(ActivityIdFormat.W3C, activity.IdFormat);
            Assert.Equal(activityTraceId.ToHexString(), activity.TraceId.ToHexString());
            Assert.True(IdIsW3CFormat(activity.Id));
            Assert.Equal($"00-{activity.TraceId.ToHexString()}-{activity.SpanId.ToHexString()}-01", activity.Id);
            Assert.Equal(ActivityTraceFlags.Recorded, activity.ActivityTraceFlags);
            Assert.True(activity.Recorded);
            activity.Stop();

            /****************************************************/
            // Set the 'Recorded' bit explicitly after the fact.
            activity = new Activity("activity3");
            activity.SetParentId("00-0123456789abcdef0123456789abcdef-0123456789abcdef-00");
            activity.Start();
            Assert.Equal(ActivityIdFormat.W3C, activity.IdFormat);
            Assert.Equal("0123456789abcdef0123456789abcdef", activity.TraceId.ToHexString());
            Assert.Equal("0123456789abcdef", activity.ParentSpanId.ToHexString());
            Assert.True(IdIsW3CFormat(activity.Id));
            Assert.Equal($"00-{activity.TraceId.ToHexString()}-{activity.SpanId.ToHexString()}-00", activity.Id);
            Assert.Equal(ActivityTraceFlags.None, activity.ActivityTraceFlags);
            Assert.False(activity.Recorded);

            activity.ActivityTraceFlags = ActivityTraceFlags.Recorded;
            Assert.Equal(ActivityTraceFlags.Recorded, activity.ActivityTraceFlags);
            Assert.True(activity.Recorded);
            activity.Stop();

            /****************************************************/
            // Confirm that that flags are propagated to children.
            activity = new Activity("activity4");
            activity.SetParentId("00-0123456789abcdef0123456789abcdef-0123456789abcdef-01");
            activity.Start();
            Assert.Equal(activity, Activity.Current);
            Assert.Equal(ActivityIdFormat.W3C, activity.IdFormat);
            Assert.Equal("0123456789abcdef0123456789abcdef", activity.TraceId.ToHexString());
            Assert.Equal("0123456789abcdef", activity.ParentSpanId.ToHexString());
            Assert.True(IdIsW3CFormat(activity.Id));
            Assert.Equal($"00-{activity.TraceId.ToHexString()}-{activity.SpanId.ToHexString()}-01", activity.Id);
            Assert.Equal(ActivityTraceFlags.Recorded, activity.ActivityTraceFlags);
            Assert.True(activity.Recorded);

            // create a child
            var childActivity = new Activity("activity4Child");
            childActivity.Start();
            Assert.Equal(childActivity, Activity.Current);

            Assert.Equal("0123456789abcdef0123456789abcdef", childActivity.TraceId.ToHexString());
            Assert.NotEqual(activity.SpanId.ToHexString(), childActivity.SpanId.ToHexString());
            Assert.True(IdIsW3CFormat(childActivity.Id));
            Assert.Equal($"00-{childActivity.TraceId.ToHexString()}-{childActivity.SpanId.ToHexString()}-01", childActivity.Id);
            Assert.Equal(ActivityTraceFlags.Recorded, childActivity.ActivityTraceFlags);
            Assert.True(childActivity.Recorded);

            childActivity.Stop();
            activity.Stop();
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
            // that checks duration precision on .NET Framework.
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
            Assert.Null(Activity.Current);
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
            Assert.Null(Activity.Current);
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
        [OuterLoop("https://github.com/dotnet/runtime/issues/23104")]
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

                    Assert.NotEqual(default(DateTime), activity.StartTimeUtc);
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
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
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
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
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

        /// <summary>
        /// Tests that Activity.Current could be set
        /// </summary>
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task ActivityCurrentSet()
        {
            Activity activity = new Activity("activity");

            // start Activity in the 'child' context
            await Task.Run(() => activity.Start());

            Assert.Null(Activity.Current);
            Activity.Current = activity;
            Assert.Same(activity, Activity.Current);
        }

        /// <summary>
        /// Tests that Activity.Current could be set to null
        /// </summary>
        [Fact]
        public void ActivityCurrentSetToNull()
        {
            Activity started = new Activity("started").Start();

            Activity.Current = null;
            Assert.Null(Activity.Current);
        }

        /// <summary>
        /// Tests that Activity.Current could not be set to Activity
        /// that has not been started yet
        /// </summary>
        [Fact]
        public void ActivityCurrentNotSetToNotStarted()
        {
            Activity started = new Activity("started").Start();
            Activity notStarted = new Activity("notStarted");

            Activity.Current = notStarted;
            Assert.Same(started, Activity.Current);
        }

        /// <summary>
        /// Tests that Activity.Current could not be set to stopped Activity
        /// </summary>
        [Fact]
        public void ActivityCurrentNotSetToStopped()
        {
            Activity started = new Activity("started").Start();
            Activity stopped = new Activity("stopped").Start();
            stopped.Stop();

            Activity.Current = stopped;
            Assert.Same(started, Activity.Current);
        }

        [Fact]
        public void TestDispose()
        {
            Activity current = Activity.Current;
            using (Activity activity = new Activity("Mine").Start())
            {
                Assert.Same(activity, Activity.Current);
                Assert.Same(current, activity.Parent);
            }

            Assert.Same(current, Activity.Current);
        }

        [Fact]
        public void TestCustomProperties()
        {
            Activity activity = new Activity("Custom");
            activity.SetCustomProperty("P1", "Prop1");
            activity.SetCustomProperty("P2", "Prop2");
            activity.SetCustomProperty("P3", null);

            Assert.Equal("Prop1", activity.GetCustomProperty("P1"));
            Assert.Equal("Prop2", activity.GetCustomProperty("P2"));
            Assert.Null(activity.GetCustomProperty("P3"));
            Assert.Null(activity.GetCustomProperty("P4"));

            activity.SetCustomProperty("P1", "Prop5");
            Assert.Equal("Prop5", activity.GetCustomProperty("P1"));

        }

        [Fact]
        public void TestKind()
        {
            Activity activity = new Activity("Kind");
            Assert.Equal(ActivityKind.Internal, activity.Kind);
        }

        [Fact]
        public void TestDisplayName()
        {
            Activity activity = new Activity("Op1");
            Assert.Equal("Op1", activity.OperationName);
            Assert.Equal("Op1", activity.DisplayName);

            activity.DisplayName = "Op2";
            Assert.Equal("Op1", activity.OperationName);
            Assert.Equal("Op2", activity.DisplayName);
        }

        [Fact]
        public void TestEvent()
        {
            Activity activity = new Activity("EventTest");
            Assert.Equal(0, activity.Events.Count());

            DateTimeOffset ts1 = DateTimeOffset.UtcNow;
            DateTimeOffset ts2 = ts1.AddMinutes(1);

            Assert.True(object.ReferenceEquals(activity, activity.AddEvent(new ActivityEvent("Event1", ts1))));
            Assert.True(object.ReferenceEquals(activity, activity.AddEvent(new ActivityEvent("Event2", ts2))));

            Assert.Equal(2, activity.Events.Count());
            Assert.Equal("Event1", activity.Events.ElementAt(0).Name);
            Assert.Equal(ts1, activity.Events.ElementAt(0).Timestamp);
            Assert.Equal(0, activity.Events.ElementAt(0).Tags.Count());

            Assert.Equal("Event2", activity.Events.ElementAt(1).Name);
            Assert.Equal(ts2, activity.Events.ElementAt(1).Timestamp);
            Assert.Equal(0, activity.Events.ElementAt(1).Tags.Count());
        }

        [Fact]
        public void TestIsAllDataRequested()
        {
            // Activity constructor allways set IsAllDataRequested to true for compatability.
            Activity a1 = new Activity("a1");
            Assert.True(a1.IsAllDataRequested);
            Assert.True(object.ReferenceEquals(a1, a1.AddTag("k1", "v1")));
            Assert.Equal(1, a1.Tags.Count());
        }

        [Fact]
        public void TestTagObjects()
        {
            Activity activity = new Activity("TagObjects");
            Assert.Equal(0, activity.Tags.Count());
            Assert.Equal(0, activity.TagObjects.Count());

            activity.AddTag("s1", "s1").AddTag("s2", "s2").AddTag("s3", null);
            Assert.Equal(3, activity.Tags.Count());
            Assert.Equal(3, activity.TagObjects.Count());

            KeyValuePair<string, string>[] tags = activity.Tags.ToArray();
            KeyValuePair<string, object>[] tagObjects = activity.TagObjects.ToArray();
            Assert.Equal(tags.Length, tagObjects.Length);

            for (int i = 0; i < tagObjects.Length; i++)
            {
                Assert.Equal(tags[i].Key, tagObjects[i].Key);
                Assert.Equal(tags[i].Value, tagObjects[i].Value);
            }

            activity.AddTag("s4", (object) null);
            Assert.Equal(4, activity.Tags.Count());
            Assert.Equal(4, activity.TagObjects.Count());
            tags = activity.Tags.ToArray();
            tagObjects = activity.TagObjects.ToArray();
            Assert.Equal(tags[3].Key, tagObjects[3].Key);
            Assert.Equal(tags[3].Value, tagObjects[3].Value);

            activity.AddTag("s5", 5);
            Assert.Equal(4, activity.Tags.Count());
            Assert.Equal(5, activity.TagObjects.Count());
            tagObjects = activity.TagObjects.ToArray();
            Assert.Equal(5, tagObjects[4].Value);

            activity.AddTag(null, null); // we allow that and we keeping the behavior for the compatability reason
            Assert.Equal(5, activity.Tags.Count());
            Assert.Equal(6, activity.TagObjects.Count());

            activity.SetTag("s6", "s6");
            Assert.Equal(6, activity.Tags.Count());
            Assert.Equal(7, activity.TagObjects.Count());

            activity.SetTag("s5", "s6");
            Assert.Equal(7, activity.Tags.Count());
            Assert.Equal(7, activity.TagObjects.Count());

            activity.SetTag("s3", null); // remove the tag
            Assert.Equal(6, activity.Tags.Count());
            Assert.Equal(6, activity.TagObjects.Count());

            tags = activity.Tags.ToArray();
            tagObjects = activity.TagObjects.ToArray();
            for (int i = 0; i < tagObjects.Length; i++)
            {
                Assert.Equal(tags[i].Key, tagObjects[i].Key);
                Assert.Equal(tags[i].Value, tagObjects[i].Value);
            }

            // Test Deleting last tag
            activity = new Activity("LastTagObjects");

            activity.SetTag("hello1", "1");
            activity.SetTag("hello2", "1");
            activity.SetTag("hello2", null); // last tag get deleted
            activity.SetTag("hello3", "2");
            activity.SetTag("hello4", "3");

            tagObjects = activity.TagObjects.ToArray();
            Assert.Equal(3, tagObjects.Length);
            Assert.Equal("hello1", tagObjects[0].Key);
            Assert.Equal("1", tagObjects[0].Value);
            Assert.Equal("hello3", tagObjects[1].Key);
            Assert.Equal("2", tagObjects[1].Value);
            Assert.Equal("hello4", tagObjects[2].Key);
            Assert.Equal("3", tagObjects[2].Value);

            activity = new Activity("FirstLastTagObjects");
            activity.SetTag("hello1", "1");
            activity.SetTag("hello1", null); // Delete the first and last tag
            activity.SetTag("hello2", "2");
            activity.SetTag("hello3", "3");
            tagObjects = activity.TagObjects.ToArray();
            Assert.Equal(2, tagObjects.Length);
            Assert.Equal("hello2", tagObjects[0].Key);
            Assert.Equal("2", tagObjects[0].Value);
            Assert.Equal("hello3", tagObjects[1].Key);
            Assert.Equal("3", tagObjects[1].Value);
        }

        [Fact]
        public void TestGetTagItem()
        {
            Activity a = new Activity("GetTagItem");

            // Test empty tags list
            Assert.Equal(0, a.TagObjects.Count());
            Assert.Null(a.GetTagItem("tag1"));

            // Test adding first tag
            a.AddTag("tag1", "value1");
            Assert.Equal(1, a.TagObjects.Count());
            Assert.Equal("value1", a.GetTagItem("tag1"));
            Assert.Null(a.GetTagItem("tag2"));

            // Test adding one more key
            a.AddTag("tag2", "value2");
            Assert.Equal(2, a.TagObjects.Count());
            Assert.Equal("value1", a.GetTagItem("tag1"));
            Assert.Equal("value2", a.GetTagItem("tag2"));

            // Test adding duplicate key
            a.AddTag("tag1", "value1-d");
            Assert.Equal(3, a.TagObjects.Count());
            Assert.Equal("value1", a.GetTagItem("tag1"));
            Assert.Equal("value2", a.GetTagItem("tag2"));

            // Test setting the key (overwrite the value)
            a.SetTag("tag1", "value1-O");
            Assert.Equal(3, a.TagObjects.Count());
            Assert.Equal("value1-O", a.GetTagItem("tag1"));
            Assert.Equal("value2", a.GetTagItem("tag2"));

            // Test removing the key
            a.SetTag("tag1", null);
            Assert.Equal(2, a.TagObjects.Count());
            Assert.Equal("value1-d", a.GetTagItem("tag1"));
            Assert.Equal("value2", a.GetTagItem("tag2"));

            a.SetTag("tag1", null);
            Assert.Equal(1, a.TagObjects.Count());
            Assert.Null(a.GetTagItem("tag1"));
            Assert.Equal("value2", a.GetTagItem("tag2"));

            a.SetTag("tag2", null);
            Assert.Equal(0, a.TagObjects.Count());
            Assert.Null(a.GetTagItem("tag1"));
            Assert.Null(a.GetTagItem("tag2"));
        }


        [Theory]
        [InlineData("key1", null, true,  1)]
        [InlineData("key2", null, false, 0)]
        [InlineData("key3", "v1", true,  1)]
        [InlineData("key4", "v2", false, 1)]
        public void TestInsertingFirstTag(string key, object value, bool add, int resultCount)
        {
            Activity a = new Activity("SetFirstTag");
            if (add)
            {
                a.AddTag(key, value);
            }
            else
            {
                a.SetTag(key, value);
            }

            Assert.Equal(resultCount, a.TagObjects.Count());
        }

        [Fact]
        public void StructEnumerator_TagsLinkedList()
        {
            // Note: This test verifies the presence of the struct Enumerator on TagsLinkedList used by customers dynamically to avoid allocations.

            Activity a = new Activity("TestActivity");
            a.AddTag("Tag1", true);

            IEnumerable<KeyValuePair<string, object>> enumerable = a.TagObjects;

            MethodInfo method = enumerable.GetType().GetMethod("GetEnumerator", BindingFlags.Instance | BindingFlags.Public);

            Assert.NotNull(method);
            Assert.False(method.ReturnType.IsInterface);
            Assert.True(method.ReturnType.IsValueType);
        }

        [Fact]
        public void TestParentTraceFlags()
        {
            Activity a = new Activity("ParentFlagsA");
            a.SetIdFormat(ActivityIdFormat.W3C);
            a.SetParentId(ActivityTraceId.CreateFromString("0123456789abcdef0123456789abcdef".AsSpan()), ActivitySpanId.CreateFromString("0123456789abcdef".AsSpan()), ActivityTraceFlags.Recorded);
            Assert.Equal("00-0123456789abcdef0123456789abcdef-0123456789abcdef-01", a.ParentId);

            Activity b = new Activity("ParentFlagsB");
            b.SetIdFormat(ActivityIdFormat.W3C);
            b.SetParentId(ActivityTraceId.CreateFromString("0123456789abcdef0123456789abcdef".AsSpan()), ActivitySpanId.CreateFromString("0123456789abcdef".AsSpan()), ActivityTraceFlags.None);
            b.ActivityTraceFlags = ActivityTraceFlags.Recorded; // Setting ActivityTraceFlags shouldn't affect the parent
            Assert.Equal("00-0123456789abcdef0123456789abcdef-0123456789abcdef-00", b.ParentId);

            using ActivitySource aSource = new ActivitySource("CheckParentTraceFlags");
            using ActivityListener listener = new ActivityListener();
            listener.ShouldListenTo = (activitySource) => object.ReferenceEquals(aSource, activitySource);
            listener.Sample = (ref ActivityCreationOptions<ActivityContext> activityOptions) => ActivitySamplingResult.AllDataAndRecorded;
            ActivitySource.AddActivityListener(listener);

            ActivityContext parentContext = new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded);
            a = aSource.CreateActivity("WithContext", ActivityKind.Internal, parentContext, default, default, ActivityIdFormat.W3C);
            Assert.NotNull(a);
            Assert.Equal("00-" + parentContext.TraceId + "-" + parentContext.SpanId + "-01", a.ParentId);
        }

        [Fact]
        public void TestStatus()
        {
            Activity a = new Activity("Status");
            Assert.Equal(ActivityStatusCode.Unset, a.Status);
            Assert.Null(a.StatusDescription);

            a.SetStatus(ActivityStatusCode.Ok); // Default description null parameter
            Assert.Equal(ActivityStatusCode.Ok, a.Status);
            Assert.Null(a.StatusDescription);

            a.SetStatus(ActivityStatusCode.Ok, null); // explicit description null parameter
            Assert.Equal(ActivityStatusCode.Ok, a.Status);
            Assert.Null(a.StatusDescription);

            a.SetStatus(ActivityStatusCode.Ok, "Ignored Description"); // explicit non null description
            Assert.Equal(ActivityStatusCode.Ok, a.Status);
            Assert.Null(a.StatusDescription);

            a.SetStatus(ActivityStatusCode.Error); // Default description null parameter
            Assert.Equal(ActivityStatusCode.Error, a.Status);
            Assert.Null(a.StatusDescription);

            a.SetStatus(ActivityStatusCode.Error, "Error Code"); // Default description null parameter
            Assert.Equal(ActivityStatusCode.Error, a.Status);
            Assert.Equal("Error Code", a.StatusDescription);

            a.SetStatus(ActivityStatusCode.Ok, "Description will reset to null");
            Assert.Equal(ActivityStatusCode.Ok, a.Status);
            Assert.Null(a.StatusDescription);

            a.SetStatus(ActivityStatusCode.Error, "Another Error Code Description");
            Assert.Equal(ActivityStatusCode.Error, a.Status);
            Assert.Equal("Another Error Code Description", a.StatusDescription);

            a.SetStatus((ActivityStatusCode) 100, "Another Error Code Description");
            Assert.Equal((ActivityStatusCode) 100, a.Status);
            Assert.Null(a.StatusDescription);
        }

        [Fact]
        public void StructEnumerator_GenericLinkedList()
        {
            // Note: This test verifies the presence of the struct Enumerator on DiagLinkedList<T> used by customers dynamically to avoid allocations.

            Activity a = new Activity("TestActivity");
            a.AddEvent(new ActivityEvent());

            IEnumerable<ActivityEvent> enumerable = a.Events;

            MethodInfo method = enumerable.GetType().GetMethod("GetEnumerator", BindingFlags.Instance | BindingFlags.Public);

            Assert.NotNull(method);
            Assert.False(method.ReturnType.IsInterface);
            Assert.True(method.ReturnType.IsValueType);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void RestoreOriginalParentTest()
        {
            RemoteExecutor.Invoke(() =>
            {
                Assert.Null(Activity.Current);

                Activity a = new Activity("Root");
                a.Start();

                Assert.NotNull(Activity.Current);
                Assert.Equal("Root", Activity.Current.OperationName);

                // Create Activity with the parent context to not use Activity.Current as a parent
                Activity b = new Activity("Child");
                b.SetParentId(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom());
                b.Start();

                Assert.NotNull(Activity.Current);
                Assert.Equal("Child", Activity.Current.OperationName);

                b.Stop();

                // Now the child activity stopped. We used to restore null to the Activity.Current but now we restore
                // the original parent stored in Activity.Current before we started the Activity.
                Assert.NotNull(Activity.Current);
                Assert.Equal("Root", Activity.Current.OperationName);

                a.Stop();
                Assert.Null(Activity.Current);

            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ActivityCurrentEventTest()
        {
            RemoteExecutor.Invoke(() =>
            {
                int count = 0;
                Activity? previous = null;
                Activity? current = null;

                Assert.Null(Activity.Current);

                //
                // No Event handler is registered yet.
                //

                using (Activity a1 = new Activity("a1"))
                {
                    a1.Start();
                    Assert.Equal(0, count);
                } // a1 stops here
                Assert.Equal(0, count);

                Activity.CurrentChanged += CurrentChanged1;

                //
                // One Event handler is registered.
                //

                using (Activity a1 = new Activity("a1"))
                {
                    current = a1;
                    a1.Start();
                    Assert.Equal(1, count);
                    using (Activity a2 = new Activity("a2"))
                    {
                        previous = a1;
                        current = a2;
                        a2.Start();
                        Assert.Equal(2, count);
                        previous = a2;
                        current = a1;
                    } // a2 stops here
                    Assert.Equal(3, count);

                    previous = a1;
                    current = null;
                } // a1 stops here
                Assert.Equal(4, count);

                Activity.CurrentChanged += CurrentChanged2;

                //
                // Two Event handlers are registered.
                //

                previous = null;
                using (Activity a1 = new Activity("a1"))
                {
                    current = a1;
                    a1.Start();
                    Assert.Equal(6, count);
                    using (Activity a2 = new Activity("a2"))
                    {
                        previous = a1;
                        current = a2;
                        a2.Start();
                        Assert.Equal(8, count);
                        previous = a2;
                        current = a1;
                    } // a2 stops here
                    Assert.Equal(10, count);

                    previous = a1;
                    current = null;
                } // a1 stops here
                Assert.Equal(12, count);

                Activity.CurrentChanged -= CurrentChanged1;

                //
                // One Event handler is registered after we removed the second handler.
                //

                previous = null;
                using (Activity a1 = new Activity("a1"))
                {
                    current = a1;
                    a1.Start();
                    Assert.Equal(13, count);
                    using (Activity a2 = new Activity("a2"))
                    {
                        previous = a1;
                        current = a2;
                        a2.Start();
                        Assert.Equal(14, count);
                        previous = a2;
                        current = a1;
                    } // a2 stops here
                    Assert.Equal(15, count);

                    previous = a1;
                    current = null;
                } // a1 stops here
                Assert.Equal(16, count);

                Activity.CurrentChanged -= CurrentChanged2;

                //
                // No Event handler is registered after we removed the remaining handler.
                //

                using (Activity a1 = new Activity("a1"))
                {
                    a1.Start();
                    Assert.Equal(16, count);
                    using (Activity a2 = new Activity("a2"))
                    {
                        a2.Start();
                        Assert.Equal(16, count);
                    } // a2 stops here
                    Assert.Equal(16, count);
                } // a1 stops here
                Assert.Equal(16, count);

                //
                // Event Handlers
                //

                void CurrentChanged1(object? sender, ActivityChangedEventArgs e)
                {
                    count++;
                    Assert.Null(sender);
                    Assert.Equal(e.Current, Activity.Current);
                    Assert.Equal(previous, e.Previous);
                    Assert.Equal(current, e.Current);
                }

                void CurrentChanged2(object? sender, ActivityChangedEventArgs e) => CurrentChanged1(sender, e);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TraceIdCustomGenerationTest()
        {
            RemoteExecutor.Invoke(() =>
            {
                Random random = new Random();
                byte [] traceIdBytes = new byte[16];

                Activity.TraceIdGenerator = () =>
                {
                    random.NextBytes(traceIdBytes);
                    return ActivityTraceId.CreateFromBytes(traceIdBytes);
                };
                Activity.DefaultIdFormat = ActivityIdFormat.W3C;

                for (int i = 0; i < 100; i++)
                {
                    Assert.Null(Activity.Current);
                    Activity a = new Activity("CustomTraceId");
                    a.Start();

                    Assert.Equal(ActivityTraceId.CreateFromBytes(traceIdBytes), a.TraceId);

                    a.Stop();
                }
            }).Dispose();
        }

        public void Dispose()
        {
            Activity.Current = null;
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
