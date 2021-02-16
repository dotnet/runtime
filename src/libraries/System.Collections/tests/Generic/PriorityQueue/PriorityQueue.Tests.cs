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
        public void PriorityQueue_Generic_EnqueueDequeue_Empty()
        {
            PriorityQueue<string, int> queue = new PriorityQueue<string, int>();

            Assert.Equal("hello", queue.EnqueueDequeue("hello", 42));
            Assert.Equal(0, queue.Count);
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

        [Fact]
        public void PriorityQueue_Generic_Constructor_IEnumerable_Null()
        {
            (string, int)[] itemsToEnqueue = new(string, int)[] { (null, 0), ("one", 1) } ;
            PriorityQueue<string, int> queue = new PriorityQueue<string, int>(itemsToEnqueue);
            Assert.Null(queue.Dequeue());
            Assert.Equal("one", queue.Dequeue());
        }

        [Fact]
        public void PriorityQueue_Generic_Enqueue_Null()
        {
            PriorityQueue<string, int> queue = new PriorityQueue<string, int>();

            queue.Enqueue(element: null, 1);
            queue.Enqueue(element: "zero", 0);
            queue.Enqueue(element: "two", 2);

            Assert.Equal("zero", queue.Dequeue());
            Assert.Null(queue.Dequeue());
            Assert.Equal("two", queue.Dequeue());
        }

        [Fact]
        public void PriorityQueue_Generic_EnqueueRange_Null()
        {
            PriorityQueue<string, int> queue = new PriorityQueue<string, int>();

            queue.EnqueueRange(new string[] { null, null, null }, 0);
            queue.EnqueueRange(new string[] { "not null" }, 1);
            queue.EnqueueRange(new string[] { null, null, null }, 0);

            for (int i = 0; i < 6; ++i)
            {
                Assert.Null(queue.Dequeue());
            }

            Assert.Equal("not null", queue.Dequeue());
        }
    }
}
