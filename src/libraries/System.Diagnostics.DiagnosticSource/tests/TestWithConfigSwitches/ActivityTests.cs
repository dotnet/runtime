// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace System.Diagnostics.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/37073", TestPlatforms.Android)]
    public class ActivityTests : IDisposable
    {
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
            Assert.DoesNotContain('#', activity.Id);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void IdGenerationInternalParent()
        {
            var parent = new Activity("parent");
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

        [Fact]
        public void IdFormat_HierarchicalIsDefault()
        {
            Activity activity = new Activity("activity1");
            activity.Start();
            Assert.Equal(ActivityIdFormat.Hierarchical, activity.IdFormat);
        }

        [Fact]
        public void IdFormat_ZeroTraceIdAndSpanIdWithHierarchicalFormat()
        {
            Activity activity = new Activity("activity");
            activity.Start();
            Assert.Equal(ActivityIdFormat.Hierarchical, activity.IdFormat);
            Assert.Equal("00000000000000000000000000000000", activity.TraceId.ToHexString());
            Assert.Equal("0000000000000000", activity.SpanId.ToHexString());
        }

        public void Dispose()
        {
            Activity.Current = null;
        }
    }
}
