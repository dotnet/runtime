// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Collections.Tests
{
    public class Queue_ICollection_NonGeneric_Tests : ICollection_NonGeneric_Tests
    {
        #region ICollection Helper Methods

        protected override Type ICollection_NonGeneric_CopyTo_ArrayOfEnumType_ThrowType => typeof(ArgumentException);

        protected override void AddToCollection(ICollection collection, int numberOfItemsToAdd)
        {
            int seed = numberOfItemsToAdd * 34;
            for (int i = 0; i < numberOfItemsToAdd; i++)
                ((Queue<string>)collection).Enqueue(CreateT(seed++));
        }

        protected override ICollection NonGenericICollectionFactory()
        {
            return new Queue<string>();
        }

        protected override bool Enumerator_Current_UndefinedOperation_Throws => true;

        protected override Type ICollection_NonGeneric_CopyTo_IndexLargerThanArrayCount_ThrowType => typeof(ArgumentOutOfRangeException);

        /// <summary>
        /// Returns a set of ModifyEnumerable delegates that modify the enumerable passed to them.
        /// </summary>
        protected override IEnumerable<ModifyEnumerable> ModifyEnumerables
        {
            get
            {
                yield return (IEnumerable enumerable) =>
                {
                    var casted = (Queue<string>)enumerable;
                    casted.Enqueue(CreateT(2344));
                    return true;
                };
                yield return (IEnumerable enumerable) =>
                {
                    var casted = (Queue<string>)enumerable;
                    if (casted.Count > 0)
                    {
                        casted.Dequeue();
                        return true;
                    }
                    return false;
                };
                yield return (IEnumerable enumerable) =>
                {
                    var casted = (Queue<string>)enumerable;
                    if (casted.Count > 0)
                    {
                        casted.Clear();
                        return true;
                    }
                    return false;
                };
            }
        }

        protected string CreateT(int seed)
        {
            int stringLength = seed % 10 + 5;
            Random rand = new Random(seed);
            byte[] bytes = new byte[stringLength];
            rand.NextBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        #endregion

        #region Queue Helper Methods

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
                yield return new object[] { new Queue<int>(Enumerable.Range(0, size).ToList()) };
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

        #endregion

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

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ToArray_Wrapped(bool initializeFromCollection)
        {
            // Create a queue whose head has wrapped around
            Queue<string> q = CreateQueueAtCapacity(initializeFromCollection, i => i.ToString(), size: 4);
            Assert.Equal("0", q.Dequeue());
            Assert.Equal("1", q.Dequeue());
            q.Enqueue("4");
            q.Enqueue("5");
            Assert.Equal(4, q.Count);

            string[] arr = q.ToArray();
            for (int i = 0; i < 4; i++)
            {
                Assert.Equal("" + (i + 2), arr[i]);
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
    }
}
