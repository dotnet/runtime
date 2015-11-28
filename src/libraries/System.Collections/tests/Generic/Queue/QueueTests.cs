// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Xunit;

namespace System.Collections.Tests
{
    public class QueueTests
    {
        [Fact]
        public void Ctors()
        {
            // Construct queues using the various constructors and make sure the count is correct
            Assert.Equal(0, new Queue<int>().Count);
            Assert.Equal(0, new Queue<int>(5).Count);
            Assert.Equal(0, new Queue<int>(Array.Empty<int>()).Count);
            Assert.Equal(5, new Queue<int>(Enumerable.Range(0, 5)).Count);

            // Make sure a queue constructed from an enumerated contains the right elements in the right order
            int count = 0;
            foreach (int i in new Queue<int>(Enumerable.Range(0, 5)))
            {
                Assert.Equal(count++, i);
            }

            // Verify arguments
            Assert.Throws<ArgumentOutOfRangeException>("capacity", () => new Queue<int>(-1));
            Assert.Throws<ArgumentNullException>("collection", () => new Queue<int>(null));
        }

        [Theory]
        [MemberData("Collections")]
        public void Ctor_IEnumerable(IEnumerable<int> collection)
        {
            var q = new Queue<int>(collection);

            Assert.Equal(collection, q);

            foreach (int item in collection)
            {
                Assert.Equal(item, q.Dequeue());
            }
            Assert.Equal(0, q.Count);
        }

        [Theory]
        [MemberData("Collections")]
        public void Ctor_IEnumerable_EnqueueDequeue(IEnumerable<int> collection)
        {
            var q = new Queue<int>(collection);
            q.Enqueue(int.MaxValue);
            q.Enqueue(int.MinValue);

            foreach (int item in collection)
            {
                Assert.Equal(item, q.Dequeue());
            }
            Assert.Equal(int.MaxValue, q.Dequeue());
            Assert.Equal(int.MinValue, q.Dequeue());
            Assert.Equal(0, q.Count);
        }

        [Theory]
        [MemberData("NonEmptyCollections")]
        public void Ctor_IEnumerable_Dequeue(IEnumerable<int> collection)
        {
            var q = new Queue<int>(collection);
            IEnumerator<int> e = collection.GetEnumerator();

            Assert.True(e.MoveNext());
            Assert.Equal(e.Current, q.Dequeue());

            while (e.MoveNext())
            {
                Assert.Equal(e.Current, q.Dequeue());
            }
            Assert.Equal(0, q.Count);
        }

        [Theory]
        [MemberData("NonEmptyCollections")]
        public void Ctor_IEnumerable_DequeueEnqueue(IEnumerable<int> collection)
        {
            var q = new Queue<int>(collection);
            IEnumerator<int> e = collection.GetEnumerator();

            Assert.True(e.MoveNext());
            Assert.Equal(e.Current, q.Dequeue());

            q.Enqueue(int.MaxValue);

            while (e.MoveNext())
            {
                Assert.Equal(e.Current, q.Dequeue());
            }
            Assert.Equal(int.MaxValue, q.Dequeue());
            Assert.Equal(0, q.Count);
        }

        public static IEnumerable<object[]> Collections
        {
            get { return GenerateCollections(includeEmptyCollections: true); }
        }

        public static IEnumerable<object[]> NonEmptyCollections
        {
            get { return GenerateCollections(includeEmptyCollections: false); }
        }

        private static IEnumerable<object[]> GenerateCollections(bool includeEmptyCollections)
        {
            var sizes = new List<int> { 65, 64, 5, 4, 3, 2, 1 };

            if (includeEmptyCollections)
                sizes.Add(0);

            foreach (int size in sizes)
            {
                yield return new object[] { Enumerable.Range(0, size).ToArray() };
                yield return new object[] { Enumerable.Range(0, size).ToList() };
                yield return new object[] { new Collection<int>(Enumerable.Range(0, size).ToList()) };
                yield return new object[] { new ReadOnlyCollection<int>(Enumerable.Range(0, size).ToList()) };
                yield return new object[] { CreateIteratorCollection(size) };
            }
        }

        private static IEnumerable<int> CreateIteratorCollection(int size)
        {
            for (int i = 0; i < size; i++)
            {
                yield return i;
            }
        }

        [Theory]
        [InlineData(0, 5)]
        [InlineData(1, 1)]
        [InlineData(3, 100)]
        public void EnqueueAndDequeue(int capacity, int items)
        {
            var q = new Queue<int>(capacity);
            Assert.Equal(0, q.Count);

            // Enqueue some values and make sure the count is correct
            for (int i = 0; i < items; i++)
            {
                q.Enqueue(i);
            }
            Assert.Equal(items, q.Count);

            // Iterate to make sure the values are all there in the right order
            int count = 0;
            foreach (int i in q)
            {
                Assert.Equal(count++, i);
            }

            // Dequeue to make sure the values are removed in the right order and the count is updated
            count = 0;
            for (int i = 0; i < items; i++)
            {
                Assert.Equal(count++, q.Dequeue());
                Assert.Equal(items - i - 1, q.Count);
            }

            // Can't dequeue when empty
            Assert.Throws<InvalidOperationException>(() => q.Dequeue());

            // But can still be used after a failure and after bouncing at empty
            q.Enqueue(42);
            Assert.Equal(42, q.Dequeue());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void Clear_Empty(int capacity)
        {
            var q = new Queue<int>(capacity);
            Assert.Equal(0, q.Count);
            q.Clear();
            Assert.Equal(0, q.Count);
        }

        [Fact]
        public void Clear_Normal()
        {
            var q = new Queue<string>();
            Assert.Equal(0, q.Count);

            for (int repeat = 0; repeat < 2; repeat++) // repeat to ensure we can grow after emptying
            {
                // Add some elements
                for (int i = 0; i < 5; i++)
                {
                    q.Enqueue(i.ToString());
                }
                Assert.Equal(5, q.Count);
                Assert.True(q.GetEnumerator().MoveNext());

                // Clear them and make sure they're gone
                q.Clear();
                Assert.Equal(0, q.Count);
                Assert.False(q.GetEnumerator().MoveNext());
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Clear_Wrapped(bool initializeFromCollection)
        {
            // Try to exercise special case of clearing when we've wrapped around
            Queue<string> q = CreateQueueAtCapacity(initializeFromCollection, i => i.ToString(), size: 4);
            Assert.Equal("0", q.Dequeue());
            Assert.Equal("1", q.Dequeue());
            q.Enqueue("5");
            q.Enqueue("6");
            Assert.Equal(4, q.Count);
            q.Clear();
            Assert.Equal(0, q.Count);
            Assert.False(q.GetEnumerator().MoveNext());
        }

        [Fact]
        public void Peek()
        {
            var q = new Queue<int>();
            Assert.Throws<InvalidOperationException>(() => q.Peek());
            q.Enqueue(1);
            Assert.Equal(1, q.Peek());
            q.Enqueue(2);
            Assert.Equal(1, q.Peek());
            Assert.Equal(1, q.Dequeue());
            Assert.Equal(2, q.Peek());
            Assert.Equal(2, q.Dequeue());
        }

        [Fact]
        public void Contains_ValueType()
        {
            var q = new Queue<int>();

            // Add some elements
            for (int i = 0; i < 10; i++)
            {
                if (i % 2 == 0)
                {
                    q.Enqueue(i);
                }
            }

            // Look them up using contains, making sure those and only those that should be there are there
            for (int i = 0; i < 10; i++)
            {
                Assert.Equal(i % 2 == 0, q.Contains(i));
            }
        }

        [Fact]
        public void Contains_ReferenceType()
        {
            var q = new Queue<string>();

            // Add some elements
            for (int i = 0; i < 10; i++)
            {
                if (i % 2 == 0)
                {
                    q.Enqueue(i.ToString());
                }
                else
                {
                    q.Enqueue(null);
                }
            }

            // Look them up using contains, making sure those and only those that should be there are there
            for (int i = 0; i < 10; i++)
            {
                Assert.Equal(i % 2 == 0, q.Contains(i.ToString()));
            }

            // Make sure we can find a null value we enqueued
            Assert.True(q.Contains(null));
        }

        [Fact]
        public void CopyTo_Normal()
        {
            var q = new Queue<int>();

            // Copy an empty queue
            var arr = new int[0];
            q.CopyTo(arr, 0);

            // Fill the queue
            q = new Queue<int>(Enumerable.Range(0, 10));
            arr = new int[q.Count];

            // Copy the queue's elements to an array
            q.CopyTo(arr, 0);
            for (int i = 0; i < 10; i++)
            {
                Assert.Equal(i, arr[i]);
            }

            // Dequeue some elements
            for (int i = 0; i < 3; i++)
            {
                Assert.Equal(i, q.Dequeue());
            }

            // Copy the remaining ones to a different location
            q.CopyTo(arr, 1);
            Assert.Equal(0, arr[0]);
            for (int i = 1; i < 8; i++)
            {
                Assert.Equal(i + 2, arr[i]);
            }
            for (int i = 8; i < 10; i++)
            {
                Assert.Equal(i, arr[i]);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CopyTo_Wrapped(bool initializeFromCollection)
        {
            // Create a queue whose head has wrapped around
            Queue<int> q = CreateQueueAtCapacity(initializeFromCollection, i => i, size: 4);
            Assert.Equal(0, q.Dequeue());
            Assert.Equal(1, q.Dequeue());
            Assert.Equal(2, q.Count);
            q.Enqueue(4);
            q.Enqueue(5);
            Assert.Equal(4, q.Count);

            // Now copy; should require two copies under the covers
            int[] arr = new int[4];
            q.CopyTo(arr, 0);
            for (int i = 0; i < 4; i++)
            {
                Assert.Equal(i + 2, arr[i]);
            }
        }

        [Fact]
        public void CopyTo_ArgumentValidation()
        {
            // Argument validation
            var q = new Queue<int>(Enumerable.Range(0, 4));
            Assert.Throws<ArgumentNullException>("array", () => q.CopyTo(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>("arrayIndex", () => q.CopyTo(new int[4], -1));
            Assert.Throws<ArgumentOutOfRangeException>("arrayIndex", () => q.CopyTo(new int[4], 5));
            Assert.Throws<ArgumentException>(() => q.CopyTo(new int[4], 1));
        }

        [Theory]
        [InlineData(0, 0)] 
        [InlineData(0, 10)]
        [InlineData(1, 10)]    
        [InlineData(10, 10)]
        [InlineData(10, 100)]
        [InlineData(90, 100)]
        [InlineData(100, 100)]
        public void TrimExcess(int items, int capacity)
        {
            var q = new Queue<int>(capacity);
            for (int i = 0; i < items; i++)
            {
                q.Enqueue(i);
            }
            q.TrimExcess();
            Assert.Equal(items, q.Count);
            Assert.Equal(Enumerable.Range(0, items), q);
        }

        [Fact]
        public void ICollection_IsSynchronized()
        {
            ICollection ic = new Queue<int>();
            Assert.False(ic.IsSynchronized);
        }

        [Fact]
        public void ICollection_SyncRoot()
        {
            ICollection ic = new Queue<int>();
            Assert.Same(ic.SyncRoot, ic.SyncRoot);
            Assert.NotNull(ic.SyncRoot);
        }

        [Fact]
        public void ICollection_Count()
        {
            ICollection ic = new Queue<int>(Enumerable.Range(0, 5));
            Assert.Equal(5, ic.Count);
        }

        [Fact]
        public void ICollection_CopyTo_FastPath()
        {
            int[] arr = new int[1] { 1 };
            ((ICollection)new Queue<int>(0)).CopyTo(arr, 0);
            Assert.Equal(1, arr[0]);
        }

        [Fact]
        public void ICollection_CopyTo_ArgumentExceptions()
        {
            ICollection ic = new Queue<int>(Enumerable.Range(0, 5));

            Assert.Throws<ArgumentNullException>("array", () => ic.CopyTo(null, 0));
            Assert.Throws<ArgumentException>(() => ic.CopyTo(new int[5, 5], 0));
            Assert.Throws<ArgumentException>(() => ic.CopyTo(Array.CreateInstance(typeof(int), new[] { 10 }, new[] { 3 }), 0));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => ic.CopyTo(new int[10], -1));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => ic.CopyTo(new int[10], 11));
            Assert.Throws<ArgumentException>(() => ic.CopyTo(new int[10], 7));
            Assert.Throws<ArgumentException>(() => ic.CopyTo(new string[10], 0));
        }

        [Fact]
        public void ICollection_CopyTo_Normal()
        {
            var q = new Queue<int>();
            var arr = new int[1] { 1 };

            // Copy an empty queue
            ((ICollection)q).CopyTo(arr, 0);
            Assert.Equal(1, arr[0]);

            q = new Queue<int>(Enumerable.Range(0, 10));
            arr = new int[q.Count];

            // Copy the queue's elements to an array
            ((ICollection)q).CopyTo(arr, 0);
            for (int i = 0; i < 10; i++)
            {
                Assert.Equal(i, arr[i]);
            }

            // Dequeue some elements
            for (int i = 0; i < 3; i++)
            {
                Assert.Equal(i, q.Dequeue());
            }

            // Copy the remaining ones to a different location
            ((ICollection)q).CopyTo(arr, 1);
            Assert.Equal(0, arr[0]);
            for (int i = 1; i < 8; i++)
            {
                Assert.Equal(i + 2, arr[i]);
            }
            for (int i = 8; i < 10; i++)
            {
                Assert.Equal(i, arr[i]);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ICollection_CopyTo_Wrapped(bool initializeFromCollection)
        {
            // Create a queue whose head has wrapped around
            Queue<int> q = CreateQueueAtCapacity(initializeFromCollection, i => i, size: 4);
            Assert.Equal(0, q.Dequeue());
            Assert.Equal(1, q.Dequeue());
            Assert.Equal(2, q.Count);
            q.Enqueue(4);
            q.Enqueue(5);
            Assert.Equal(4, q.Count);

            // Now copy; should require two copies under the covers
            int[] arr = new int[4];
            ((ICollection)q).CopyTo(arr, 0);
            for (int i = 0; i < 4; i++)
            {
                Assert.Equal(i + 2, arr[i]);
            }
        }

        [Fact]
        public void IEnumerable()
        {
            var src = Enumerable.Range(10, 15);
            var q = new Queue<int>(src);

            // Get enumerators from the source and then for both IEnumerable and IEnumerable<T> for the queue
            IEnumerator<int> srcEnumerator = src.GetEnumerator();
            IEnumerator nonGeneric = ((IEnumerable)q).GetEnumerator();
            IEnumerator<int> generic = ((IEnumerable<int>)q).GetEnumerator();

            // Current shouldn't yet be usable
            Assert.Throws<InvalidOperationException>(() => nonGeneric.Current);
            Assert.Throws<InvalidOperationException>(() => generic.Current);

            // Run through the sequences twice, the second time to help validate Reset
            for (int repeat = 0; repeat < 2; repeat++)
            {
                // Make sure every element in the source is in each of the queue's enumerators
                while (srcEnumerator.MoveNext())
                {
                    Assert.True(nonGeneric.MoveNext());
                    Assert.Equal(srcEnumerator.Current, nonGeneric.Current);

                    Assert.True(generic.MoveNext());
                    Assert.Equal(srcEnumerator.Current, generic.Current);
                }

                // The queue's enumerators should now be done
                Assert.False(nonGeneric.MoveNext());
                Assert.False(generic.MoveNext());
                Assert.Throws<InvalidOperationException>(() => nonGeneric.Current);
                Assert.Throws<InvalidOperationException>(() => generic.Current);

                // Reset
                nonGeneric.Reset();
                generic.Reset();
                srcEnumerator = src.GetEnumerator(); // RangeIterator doesn't support reset
            }

            // Dispose the enumerators
            ((IDisposable)nonGeneric).Dispose();
            generic.Dispose();
            Assert.False(nonGeneric.MoveNext());
            Assert.False(generic.MoveNext());
        }

        [Fact]
        public void ToArray()
        {
            var q = new Queue<int>(4);

            // Verify behavior with an empty queue
            Assert.NotNull(q.ToArray());
            Assert.NotSame(q.ToArray(), q.ToArray());

            // Verify behavior with some elements
            for (int i = 0; i < 4; i++)
            {
                q.Enqueue(i);
                var arr = q.ToArray();
                Assert.Equal(i + 1, arr.Length);
                for (int j = 0; j < arr.Length; j++)
                {
                    Assert.Equal(j, arr[j]);
                }
            }

            // Verify behavior when we wrap around
            {
                Assert.Equal(0, q.Dequeue());
                Assert.Equal(1, q.Dequeue());
                q.Enqueue(4);
                q.Enqueue(5);
                var arr = q.ToArray();
                for (int i = 0; i < arr.Length; i++)
                {
                    Assert.Equal(i + 2, arr[i]);
                }
            }
        }

        private static Queue<T> CreateQueueAtCapacity<T>(bool initializeFromCollection, Func<int, T> selector, int size)
        {
            Queue<T> q;

            if (initializeFromCollection)
            {
                q = new Queue<T>(Enumerable.Range(0, size).Select(selector).ToArray());
            }
            else
            {
                q = new Queue<T>(size);
                for (int i = 0; i < size; i++)
                {
                    q.Enqueue(selector(i));
                }
            }

            return q;
        }
    }
}
