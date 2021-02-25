// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Collections.Tests
{
    public static class PriorityQueue_PropertyTests
    {
        const int MaxTest = 100;
        const int Seed = 42;

        private readonly static IComparer<string> s_stringComparer = StringComparer.Ordinal;

        [Theory]
        [MemberData(nameof(GetRandomStringArrays))]
        public static void HeapSort_Heapify_String(string[] elements)
        {
            IEnumerable<string> expected = elements.OrderBy(e => e, s_stringComparer);
            IEnumerable<string> actual = HeapSort_Heapify(elements, s_stringComparer);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(GetRandomIntArrays))]
        public static void HeapSort_Heapify_Int(int[] elements)
        {
            IEnumerable<int> expected = elements.OrderBy(e => e);
            IEnumerable<int> actual = HeapSort_Heapify(elements);
            Assert.Equal(expected, actual);
        }

        private static IEnumerable<TElement> HeapSort_Heapify<TElement>(IEnumerable<TElement> inputs, IComparer<TElement>? comparer = null)
        {
            var queue = new PriorityQueue<TElement, TElement>(inputs.Select(e => (e, e)), comparer);
            foreach ((TElement element, TElement priority) in DrainHeap(queue))
            {
                Assert.Equal(element, priority);
                yield return element;
            }
        }

        [Theory]
        [MemberData(nameof(GetRandomStringArrays))]
        public static void HeapSort_EnqueueRange_String(string[] elements)
        {
            IEnumerable<string> expected = elements.OrderBy(e => e, s_stringComparer);
            IEnumerable<string> actual = HeapSort_EnqueueRange(elements, s_stringComparer);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(GetRandomIntArrays))]
        public static void HeapSort_EnqueueRange_Int(int[] elements)
        {
            IEnumerable<int> expected = elements.OrderBy(e => e);
            IEnumerable<int> actual = HeapSort_EnqueueRange(elements);
            Assert.Equal(expected, actual);
        }

        private static IEnumerable<TElement> HeapSort_EnqueueRange<TElement>(IEnumerable<TElement> inputs, IComparer<TElement>? comparer = null)
        {
            var queue = new PriorityQueue<TElement, TElement>(comparer);
            queue.EnqueueRange(inputs.Select(e => (e, e)));
            foreach ((TElement element, TElement priority) in DrainHeap(queue))
            {
                Assert.Equal(element, priority);
                yield return element;
            }
        }

        [Theory]
        [MemberData(nameof(GetRandomStringArrays))]
        public static void HeapSort_Enqueue_String(string[] elements)
        {
            IEnumerable<string> expected = elements.OrderBy(e => e, s_stringComparer);
            IEnumerable<string> actual = HeapSort_Enqueue(elements, s_stringComparer);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(GetRandomIntArrays))]
        public static void HeapSort_Enqueue_Int(int[] elements)
        {
            IEnumerable<int> expected = elements.OrderBy(e => e);
            IEnumerable<int> actual = HeapSort_Enqueue(elements);
            Assert.Equal(expected, actual);
        }

        private static IEnumerable<TElement> HeapSort_Enqueue<TElement>(IEnumerable<TElement> inputs, IComparer<TElement>? comparer = null)
        {
            var queue = new PriorityQueue<TElement, TElement>(comparer);

            foreach (TElement input in inputs)
            {
                queue.Enqueue(input, input);
            }

            foreach ((TElement element, TElement priority) in DrainHeap(queue))
            {
                Assert.Equal(element, priority);
                yield return element;
            }
        }

        [Theory]
        [MemberData(nameof(GetRandomStringArrays))]
        public static void KMaxElements_String(string[] elements)
        {
            const int k = 5;
            IEnumerable<string> expected = elements.OrderByDescending(e => e, s_stringComparer).Take(k);
            IEnumerable<string> actual = KMaxElements(elements, k, s_stringComparer);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(GetRandomIntArrays))]
        public static void KMaxElements_Int(int[] elements)
        {
            const int k = 5;
            IEnumerable<int> expected = elements.OrderByDescending(e => e).Take(k);
            IEnumerable<int> actual = KMaxElements(elements, k);
            Assert.Equal(expected, actual);
        }

        private static IEnumerable<TElement> KMaxElements<TElement>(TElement[] elements, int k, IComparer<TElement>? comparer = null)
        {
            var queue = new PriorityQueue<TElement, TElement>(comparer);
            comparer = queue.Comparer;

            int heapSize = Math.Min(k, elements.Length);
            for (int i = 0; i < heapSize; i++)
            {
                TElement element = elements[i];
                queue.Enqueue(element, element);
            }

            for (int i = k; i < elements.Length; i++)
            {
                TElement element = elements[i];
                TElement dequeued = queue.EnqueueDequeue(element, element);
                Assert.True(comparer.Compare(dequeued, element) <= 0);
                Assert.Equal(k, queue.Count);
            }

            foreach ((TElement element, TElement priority) in DrainHeap(queue).Reverse())
            {
                Assert.Equal(element, priority);
                yield return element;
            }
        }

        private static IEnumerable<(TElement Element, TPriority Priority)> DrainHeap<TElement, TPriority>(PriorityQueue<TElement, TPriority> queue)
        {
            while (queue.Count > 0)
            {
                Assert.True(queue.TryPeek(out TElement element, out TPriority priority));
                Assert.True(queue.TryDequeue(out TElement element2, out TPriority priority2));
                Assert.Equal(element, element2);
                Assert.Equal(priority, priority2);
                yield return (element, priority);
            }

            Assert.False(queue.TryPeek(out _, out _));
        }

        public static IEnumerable<object[]> GetRandomStringArrays() => GenerateMemberData(random => GenArray(GenString, random));
        public static IEnumerable<object[]> GetRandomIntArrays() => GenerateMemberData(random => GenArray(GenInt, random));

        private static IEnumerable<object[]> GenerateMemberData<T>(Func<Random, T> genElement)
        {
            var random = new Random(Seed);
            for (int i = 0; i < MaxTest; i++)
            {
                yield return new object[] { genElement(random) };
            };
        }

        private static T[] GenArray<T>(Func<Random, T> genElement, Random random)
        {
            const int MaxArraySize = 100;
            int arraySize = random.Next(MaxArraySize);
            var array = new T[arraySize];
            for (int i = 0; i < arraySize; i++)
            {
                array[i] = genElement(random);
            }

            return array;
        }

        private static int GenInt(Random random) => random.Next();

        private static string GenString(Random random)
        {
            const int MaxSize = 50;
            int size = random.Next(MaxSize);
            var buffer = new byte[size];
            random.NextBytes(buffer);
            return Convert.ToBase64String(buffer);
        }
    }
}
