// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Collections.Tests
{
    public abstract partial class Queue_Generic_Tests<T> : IGenericSharedAPI_Tests<T>
    {
        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void Queue_Generic_TryDequeue_AllElements(int count)
        {
            Queue<T> queue = GenericQueueFactory(count);
            List<T> elements = queue.ToList();
            foreach (T element in elements)
            {
                T result;
                Assert.True(queue.TryDequeue(out result));
                Assert.Equal(element, result);
            }
        }

        [Fact]
        public void Queue_Generic_TryDequeue_EmptyQueue_ReturnsFalse()
        {
            T result;
            Assert.False(new Queue<T>().TryDequeue(out result));
            Assert.Equal(default(T), result);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void Queue_Generic_TryPeek_AllElements(int count)
        {
            Queue<T> queue = GenericQueueFactory(count);
            List<T> elements = queue.ToList();
            foreach (T element in elements)
            {
                T result;
                Assert.True(queue.TryPeek(out result));
                Assert.Equal(element, result);

                queue.Dequeue();
            }
        }

        [Fact]
        public void Queue_Generic_TryPeek_EmptyQueue_ReturnsFalse()
        {
            T result;
            Assert.False(new Queue<T>().TryPeek(out result));
            Assert.Equal(default(T), result);
        }
    }
}
