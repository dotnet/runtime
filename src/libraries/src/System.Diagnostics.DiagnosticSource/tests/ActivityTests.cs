// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        public void IdFormatTests()
        {
            try
            {
                Activity activity;

                // Default format is the default (Hierarchical)
                activity = new Activity("activity1");
                activity.Start();
                Assert.Equal(ActivityIdFormat.Hierarchical, activity.IdFormat);
                activity.Stop();

                // Set the parent to something that is W3C by string
                activity = new Activity("activity2");
                activity.SetParentId("00-0123456789abcdef0123456789abcdef-0123456789abcdef-00");
                activity.Start();
                Assert.Equal(ActivityIdFormat.W3C, activity.IdFormat);
                Assert.Equal("0123456789abcdef0123456789abcdef", activity.TraceId.ToHexString());
                Assert.Equal("0123456789abcdef", activity.ParentSpanId.ToHexString());
                Assert.Equal(ActivityTraceFlags.None, activity.ActivityTraceFlags);
                Assert.False(activity.Recorded);
                Assert.True(IdIsW3CFormat(activity.Id));
                activity.Stop();

                // Set the parent to something that is W3C but using ActivityTraceId,ActivitySpanId version of SetParentId.  
                activity = new Activity("activity3");
                ActivityTraceId activityTraceId = ActivityTraceId.CreateRandom();
                activity.SetParentId(activityTraceId, ActivitySpanId.CreateRandom());
                activity.Start();
                Assert.Equal(ActivityIdFormat.W3C, activity.IdFormat);
                Assert.Equal(activityTraceId.ToHexString(), activity.TraceId.ToHexString());
                Assert.Equal(ActivityTraceFlags.None, activity.ActivityTraceFlags);
                Assert.False(activity.Recorded);
                Assert.True(IdIsW3CFormat(activity.Id));
                activity.Stop();

                // Change DefaultIdFormat to W3C, confirm I get the new format.  
                Activity.DefaultIdFormat = ActivityIdFormat.W3C;
                activity = new Activity("activity4");
                activity.Start();
                Assert.Equal(ActivityIdFormat.W3C, activity.IdFormat);
                Assert.True(IdIsW3CFormat(activity.Id));
                activity.Stop();

                // But I don't get the default format if parent is hierarchical 
                activity = new Activity("activity5");
                string parentId = "|a000b421-5d183ab6.1";
                activity.SetParentId(parentId);
                activity.Start();
                Assert.Equal(ActivityIdFormat.Hierarchical, activity.IdFormat);
                Assert.True(activity.Id.StartsWith(parentId));

                // Heirarchical Ids return null ActivityTraceId and ActivitySpanIds
                Assert.Equal("00000000000000000000000000000000", activity.TraceId.ToHexString());
                Assert.Equal("0000000000000000", activity.SpanId.ToHexString());
                activity.Stop();

                // But if I set ForceDefaultFormat I get what I asked for (W3C format)
                Activity.ForceDefaultIdFormat = true;
                activity = new Activity("activity6");
                activity.SetParentId(parentId);
                activity.Start();
                Assert.Equal(ActivityIdFormat.W3C, activity.IdFormat);
                Assert.True(IdIsW3CFormat(activity.Id));
                Assert.NotEqual("00000000000000000000000000000000", activity.TraceId.ToHexString());
                Assert.NotEqual("0000000000000000", activity.SpanId.ToHexString());

                /* TraceStateString testing */
                // Test TraceStateString (that it inherits from parent)
                Activity parent = new Activity("parent");
                string testString = "MyTestString";
                parent.TraceStateString = testString;
                parent.Start();
                Assert.Equal(testString, parent.TraceStateString);

                activity = new Activity("activity7");
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

                activity.Stop();
                parent.Stop();

                // Upper-case ids are not supported
                activity = new Activity("activity8");
                activity.SetParentId("00-0123456789ABCDEF0123456789ABCDEF-0123456789ABCDEF-01");
                activity.Start();
                Assert.Equal(ActivityIdFormat.W3C, activity.IdFormat);
                Assert.True(IdIsW3CFormat(activity.Id));
                activity.Stop();

                // non hex chars are not supported in traceId
                activity = new Activity("activity9");
                activity.SetParentId("00-xyz3456789abcdef0123456789abcdef-0123456789abcdef-01");
                activity.Start();
                Assert.Equal(ActivityIdFormat.W3C, activity.IdFormat);
                Assert.True(IdIsW3CFormat(activity.Id));
                activity.Stop();

                // non hex chars are not supported in parentSpanId
                activity = new Activity("activity10");
                activity.SetParentId("00-0123456789abcdef0123456789abcdef-x123456789abcdef-01");
                activity.Start();
                Assert.Equal(ActivityIdFormat.W3C, activity.IdFormat);
                Assert.True(IdIsW3CFormat(activity.Id));
                Assert.Equal("0000000000000000", activity.ParentSpanId.ToHexString());
                Assert.Equal("0123456789abcdef0123456789abcdef", activity.TraceId.ToHexString());
                activity.Stop();

                // ParentSpanId from parent Activity
                Activity.DefaultIdFormat = ActivityIdFormat.W3C;
                Activity.ForceDefaultIdFormat = true;

                parent = new Activity("parent").Start();
                activity = new Activity("parent").Start();
                Assert.Equal(parent.SpanId.ToHexString(), activity.ParentSpanId.ToHexString());

                activity.Stop();
                parent.Stop();
            }
            finally
            {
                // Set global settings back to the default, just to put the state back. 
                Activity.ForceDefaultIdFormat = false;
                Activity.DefaultIdFormat = ActivityIdFormat.Hierarchical;
                Activity.Current = null;
            }
        }

        [Fact]
        public void TraceIdBeforeStartTests()
        {
            try
            {
                Activity activity;

                // from traceparent header
                activity = new Activity("activity1");
                activity.SetParentId("00-0123456789abcdef0123456789abcdef-0123456789abcdef-01");
                Assert.Equal("0123456789abcdef0123456789abcdef", activity.TraceId.ToHexString());

                // from explicit TraceId and SpanId
                activity = new Activity("activity2");
                activity.SetParentId(
                    ActivityTraceId.CreateFromString("0123456789abcdef0123456789abcdef".AsSpan()),
                    ActivitySpanId.CreateFromString("0123456789abcdef".AsSpan()));

                Assert.Equal("0123456789abcdef0123456789abcdef", activity.TraceId.ToHexString());

                // from in-proc parent
                Activity parent = new Activity("parent");
                parent.SetParentId("00-0123456789abcdef0123456789abcdef-0123456789abcdef-01");
                parent.Start();

                activity = new Activity("child");
                activity.Start();
                Assert.Equal("0123456789abcdef0123456789abcdef", activity.TraceId.ToHexString());
                parent.Stop();
                activity.Stop();

                // no parent
                Activity.DefaultIdFormat = ActivityIdFormat.W3C;
                Activity.ForceDefaultIdFormat = true;

                activity = new Activity("activity3");
                Assert.Equal("00000000000000000000000000000000", activity.TraceId.ToHexString());

                // from invalid traceparent header
                activity.SetParentId("123");
                Assert.Equal("00000000000000000000000000000000", activity.TraceId.ToHexString());
            }
            finally
            {
                Activity.ForceDefaultIdFormat = false;
                Activity.DefaultIdFormat = ActivityIdFormat.Hierarchical;
                Activity.Current = null;
            }
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
        [OuterLoop] // Slighly flaky - https://github.com/dotnet/corefx/issues/23072
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

        /// <summary>
        /// Tests that Activity.Current could be set
        /// </summary>
        [Fact]
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
