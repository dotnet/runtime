// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Xunit;

namespace System.Threading.Tasks.Dataflow.Tests
{
    public partial class DataflowBlockTests : DataflowBlockTestBase
    {
        [Fact]
        [OuterLoop]
        public void RunDataflowBlockOptionsTests()
        {
            // Test base DataflowBlockOptions
            {
                // Test invalid property values
                {
                    Assert.Throws<ArgumentNullException>(() => { new DataflowBlockOptions().TaskScheduler = null; });
                    Assert.Throws<ArgumentOutOfRangeException>(() => { new DataflowBlockOptions().MaxMessagesPerTask = -2; });
                    Assert.Throws<ArgumentOutOfRangeException>(() => { new DataflowBlockOptions().MaxMessagesPerTask = 0; });
                    Assert.Throws<ArgumentOutOfRangeException>(() => { new DataflowBlockOptions().BoundedCapacity = -2; });
                    Assert.Throws<ArgumentOutOfRangeException>(() => { new DataflowBlockOptions().BoundedCapacity = 0; });
                    Assert.Throws<ArgumentNullException>(() => { new DataflowBlockOptions().NameFormat = null; });
                }

                // Test default values
                {
                    var db = new DataflowBlockOptions();
                    Assert.True(db.TaskScheduler == TaskScheduler.Default, "TaskScheduler should be Default");
                    Assert.True(db.MaxMessagesPerTask == DataflowBlockOptions.Unbounded, "Max messages should be unbounded.");
                    Assert.True(db.BoundedCapacity == DataflowBlockOptions.Unbounded, "Bounded capacity should be unbounded.");
                    Assert.True(
                        !db.CancellationToken.CanBeCanceled && !db.CancellationToken.IsCancellationRequested,
                        "The cancellation token should be None.");
                    Assert.True(DataflowBlockOptions.Unbounded == -1, "Unbounded should be the value -1");
                    Assert.True(db.NameFormat == @"{0} Id={1}", @"NameFormat should be the value '{0} Id={1}'");
                }

                // Test that set values are retrievable
                {
                    var db = new DataflowBlockOptions();

                    db.MaxMessagesPerTask = 2;
                    Assert.True(db.MaxMessagesPerTask == 2, "Expected max messages to be the set value 2");
                    db.MaxMessagesPerTask = Int32.MaxValue;
                    Assert.True(db.MaxMessagesPerTask == Int32.MaxValue, "Expected max messages to be the set value Int32.MaxValue");
                    db.MaxMessagesPerTask = DataflowBlockOptions.Unbounded;
                    Assert.True(db.MaxMessagesPerTask == DataflowBlockOptions.Unbounded, "Expected max messages to be unbounded.");

                    db.BoundedCapacity = 2;
                    Assert.True(db.BoundedCapacity == 2, "Expected bounded capacity to be the set value 2");
                    db.BoundedCapacity = Int32.MaxValue;
                    Assert.True(db.BoundedCapacity == Int32.MaxValue, "Expected bounded capacity to be the set value Int32.MaxValue");
                    db.BoundedCapacity = DataflowBlockOptions.Unbounded;
                    Assert.True(db.BoundedCapacity == DataflowBlockOptions.Unbounded, "Expected bounded capacity to be unbounded.");

                    var dummyScheduler = new DummyScheduler();
                    db.TaskScheduler = dummyScheduler;
                    Assert.True(db.TaskScheduler == dummyScheduler, "Expected task scheduler to be the dummy scheduler");
                    db.TaskScheduler = TaskScheduler.Default;
                    Assert.True(db.TaskScheduler == TaskScheduler.Default, "Expected task scheduler to be the default scheduler");

                    var cts = new CancellationTokenSource();
                    db.CancellationToken = cts.Token;
                    Assert.True(db.CancellationToken == cts.Token, "Expected the token to be the one just set");
                    db.CancellationToken = CancellationToken.None;
                    Assert.True(db.CancellationToken == CancellationToken.None, "Expected the token to be none");

                    db.NameFormat = "none";
                    Assert.True(db.NameFormat.Equals("none"), "Expected name format to be the set value 'none'");
                    db.NameFormat = "foo {0}";
                    Assert.True(db.NameFormat.Equals("foo {0}"), @"Expected name format to be the set value 'foo {0}'");
                    db.NameFormat = "foo {0} bar {1}";
                    Assert.True(db.NameFormat.Equals("foo {0} bar {1}"), @"Expected name format to be the set value 'foo {0} bar {1}'");
                    db.NameFormat = "kaboom {0} {1} {2}";
                    Assert.True(db.NameFormat.Equals("kaboom {0} {1} {2}"), @"Expected name format to be the set value 'kaboom {0} {1} {2}'");
                }
            }

            // Test base ExecutionDataflowBlockOptions
            {
                // Test invalid property values
                {
                    Assert.Throws<ArgumentOutOfRangeException>(() => { new ExecutionDataflowBlockOptions().MaxDegreeOfParallelism = -2; });
                    Assert.Throws<ArgumentOutOfRangeException>(() => { new ExecutionDataflowBlockOptions().MaxDegreeOfParallelism = 0; });
                }

                // Test default values
                {
                    var db = new ExecutionDataflowBlockOptions();
                    Assert.True(db.TaskScheduler == TaskScheduler.Default, "Expected task scheduler to have default value");
                    Assert.True(db.MaxMessagesPerTask == DataflowBlockOptions.Unbounded, "Expected max messages to have default value");
                    Assert.True(db.BoundedCapacity == DataflowBlockOptions.Unbounded, "Expected bounded capacity to have default value");
                    Assert.True(
                        !db.CancellationToken.CanBeCanceled && !db.CancellationToken.IsCancellationRequested, "Expected cancellation token to have default value");
                    Assert.True(db.MaxDegreeOfParallelism == 1, "Expected max dop to have default value");
                }

                // Test that set values are retrievable
                {
                    var db = new ExecutionDataflowBlockOptions();

                    db.MaxDegreeOfParallelism = 2;
                    Assert.True(db.MaxDegreeOfParallelism == 2, "Expected max dop to be 2");
                    db.MaxDegreeOfParallelism = Int32.MaxValue;
                    Assert.True(db.MaxDegreeOfParallelism == Int32.MaxValue, "Expected max dop to be Int32.MaxValue");
                    db.MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded;
                    Assert.True(db.MaxDegreeOfParallelism == DataflowBlockOptions.Unbounded, "Expected max dop to be unbounded");
                }
            }

            // Test base GroupingDataflowBlockOptions
            {
                // Test invalid property values
                {
                    Assert.Throws<ArgumentOutOfRangeException>(() => { new GroupingDataflowBlockOptions().MaxNumberOfGroups = -2; });
                    Assert.Throws<ArgumentOutOfRangeException>(() => { new GroupingDataflowBlockOptions().MaxNumberOfGroups = 0; });
                }

                // Test default values
                {
                    var db = new GroupingDataflowBlockOptions();
                    Assert.True(db.TaskScheduler == TaskScheduler.Default, "Expected task scheduler to have default value");
                    Assert.True(db.MaxMessagesPerTask == DataflowBlockOptions.Unbounded, "Expected max messages to have default value");
                    Assert.True(db.BoundedCapacity == DataflowBlockOptions.Unbounded, "Expected bounded capacity to have default value");
                    Assert.True(
                        !db.CancellationToken.CanBeCanceled && !db.CancellationToken.IsCancellationRequested, "Expected cancellation token to have default value");
                    Assert.True(db.MaxNumberOfGroups == DataflowBlockOptions.Unbounded, "Expected max groups to have default value");
                    Assert.True(db.Greedy == true, "Expected greedy to have default value");
                }

                // Test that set values are retrievable
                {
                    var db = new GroupingDataflowBlockOptions();

                    db.MaxNumberOfGroups = 2;
                    Assert.True(db.MaxNumberOfGroups == 2, "Expected max groups to be 2");
                    db.MaxNumberOfGroups = Int32.MaxValue;
                    Assert.True(db.MaxNumberOfGroups == Int32.MaxValue, "Expected max groups to be Int32.MaxValue");
                    db.MaxNumberOfGroups = Int64.MaxValue;
                    Assert.True(db.MaxNumberOfGroups == Int64.MaxValue, "Expected max groups to be Int64.MaxValue");
                    db.MaxNumberOfGroups = DataflowBlockOptions.Unbounded;
                    Assert.True(db.MaxMessagesPerTask == DataflowBlockOptions.Unbounded, "Expected max groups to unbounded");

                    db.Greedy = true;
                    Assert.True(db.Greedy == true, "Expected greedy to be true");
                    db.Greedy = false;
                    Assert.True(db.Greedy == false, "Expected greedy to be false");
                    db.Greedy = true;
                    Assert.True(db.Greedy == true, "Expected greedy to be true");
                }
            }
        }

        class DummyScheduler : TaskScheduler
        {
            protected override System.Collections.Generic.IEnumerable<Task> GetScheduledTasks()
            {
                throw new NotImplementedException();
            }

            protected override void QueueTask(Task task)
            {
                throw new NotImplementedException();
            }

            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            {
                throw new NotImplementedException();
            }
        }
    }
}