// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Threading.Tasks.Dataflow.Tests
{
    public class DataflowBlockOptionsTests
    {
        [Fact]
        public void TestInvalidArguments()
        {
            Assert.Throws<ArgumentNullException>(() => { new DataflowBlockOptions().TaskScheduler = null; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { new DataflowBlockOptions().MaxMessagesPerTask = -2; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { new DataflowBlockOptions().MaxMessagesPerTask = 0; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { new DataflowBlockOptions().BoundedCapacity = -2; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { new DataflowBlockOptions().BoundedCapacity = 0; });
            Assert.Throws<ArgumentNullException>(() => { new DataflowBlockOptions().NameFormat = null; });

            Assert.Throws<ArgumentOutOfRangeException>(() => { new ExecutionDataflowBlockOptions().MaxDegreeOfParallelism = -2; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { new ExecutionDataflowBlockOptions().MaxDegreeOfParallelism = 0; });

            Assert.Throws<ArgumentOutOfRangeException>(() => { new GroupingDataflowBlockOptions().MaxNumberOfGroups = -2; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { new GroupingDataflowBlockOptions().MaxNumberOfGroups = 0; });
        }

        [Fact]
        public void TestDefaultValues()
        {
            Action<DataflowBlockOptions> verifyBaseDefaults = dbo => {
                Assert.Equal(expected: TaskScheduler.Default, actual: dbo.TaskScheduler);
                Assert.Equal(expected: DataflowBlockOptions.Unbounded, actual: dbo.MaxMessagesPerTask);
                Assert.Equal(expected: DataflowBlockOptions.Unbounded, actual: dbo.BoundedCapacity);
                Assert.Equal(expected: CancellationToken.None, actual: dbo.CancellationToken);
                Assert.Equal(expected: -1, actual: DataflowBlockOptions.Unbounded);
                Assert.Equal(expected: @"{0} Id={1}", actual: dbo.NameFormat);
                Assert.Equal(expected: true, actual: dbo.EnsureOrdered);
            };

            verifyBaseDefaults(new DataflowBlockOptions());

            var edbo = new ExecutionDataflowBlockOptions();
            verifyBaseDefaults(edbo);
            Assert.Equal(expected: 1, actual: edbo.MaxDegreeOfParallelism);

            var gdbo = new GroupingDataflowBlockOptions();
            verifyBaseDefaults(gdbo);
            Assert.Equal(expected: DataflowBlockOptions.Unbounded, actual: gdbo.MaxNumberOfGroups);
            Assert.Equal(expected: true, actual: gdbo.Greedy);
        }

        [Fact]
        public void TestPropertyRoundtripping()
        {
            var dbo = new DataflowBlockOptions();
            var edbo = new ExecutionDataflowBlockOptions();
            var gdbo = new GroupingDataflowBlockOptions();

            foreach (int value in new[] { 2, int.MaxValue, DataflowBlockOptions.Unbounded })
            {
                SetAndTest(dbo, (o,v) => o.MaxMessagesPerTask = v, o => o.MaxMessagesPerTask, value);
                SetAndTest(dbo, (o,v) => o.BoundedCapacity = v, o => o.BoundedCapacity, value);
                SetAndTest(edbo, (o,v) => o.MaxDegreeOfParallelism = v, o => o.MaxDegreeOfParallelism, value);
                SetAndTest(gdbo, (o,v) => o.MaxNumberOfGroups = v, o => o.MaxNumberOfGroups, value);
            }
            SetAndTest(gdbo, (o,v) => o.MaxNumberOfGroups = v, o => o.MaxNumberOfGroups, long.MaxValue);

            foreach (TaskScheduler value in new[] { new ConcurrentExclusiveSchedulerPair().ConcurrentScheduler, TaskScheduler.Default })
            {
                SetAndTest(dbo, (o,v) => o.TaskScheduler = v, o => o.TaskScheduler, value);
            }

            foreach (CancellationToken value in new[] { new CancellationToken(false), new CancellationToken(true), new CancellationTokenSource().Token })
            {
                SetAndTest(dbo, (o,v) => o.CancellationToken = v, o => o.CancellationToken, value);
            }

            foreach (string value in new[] { "none", "foo {0}", "foo {0} bar {1}", "kaboom {0} {1} {2}" })
            {
                SetAndTest(dbo, (o, v) => o.NameFormat = v, o => o.NameFormat, value);
            }

            foreach (bool value in DataflowTestHelpers.BooleanValues)
            {
                SetAndTest(dbo, (o, v) => o.EnsureOrdered = v, o => o.EnsureOrdered, value);
            }

            foreach (bool value in DataflowTestHelpers.BooleanValues)
            {
                SetAndTest(gdbo, (o, v) => o.Greedy = v, o => o.Greedy, value);
            }
        }

        private void SetAndTest<T, TValue>(T source, Action<T, TValue> setter, Func<T, TValue> getter, TValue value)
        {
            setter(source, value);
            Assert.Equal(expected: value, actual: getter(source));
        }
    }
}
