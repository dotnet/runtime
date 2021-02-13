// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Collections.Tests
{
    public class PriorityQueue_NonGeneric_Tests : TestBase
    {
        protected PriorityQueue<string, int> SmallPriorityQueueFactory(out HashSet<(string, int)> items)
        {
            items = new HashSet<(string, int)>
            {
                ("one", 1),
                ("two", 2),
                ("three", 3)
            };
            var queue = new PriorityQueue<string, int>(items);

            return queue;
        }

        [Fact]
        public void PriorityQueue_Generic_EnqueueDequeue_SmallerThanMin()
        {
            PriorityQueue<string, int> queue = SmallPriorityQueueFactory(out HashSet<(string, int)> enqueuedItems);

            string actualElement = queue.EnqueueDequeue("zero", 0);

            Assert.Equal("zero", actualElement);
            Assert.True(enqueuedItems.SetEquals(queue.UnorderedItems));
        }

        [Fact]
        public void PriorityQueue_Generic_EnqueueDequeue_LargerThanMin()
        {
            PriorityQueue<string, int> queue = SmallPriorityQueueFactory(out HashSet<(string, int)> enqueuedItems);

            string actualElement = queue.EnqueueDequeue("four", 4);

            Assert.Equal("one", actualElement);
            Assert.Equal("two", queue.Dequeue());
            Assert.Equal("three", queue.Dequeue());
            Assert.Equal("four", queue.Dequeue());
        }

        [Fact]
        public void PriorityQueue_Generic_EnqueueDequeue_EqualToMin()
        {
            PriorityQueue<string, int> queue = SmallPriorityQueueFactory(out HashSet<(string, int)> enqueuedItems);

            string actualElement = queue.EnqueueDequeue("one-not-to-enqueue", 1);

            Assert.Equal("one-not-to-enqueue", actualElement);
            Assert.True(enqueuedItems.SetEquals(queue.UnorderedItems));
        }
    }
}
