// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using System.Diagnostics;
using Microsoft.Xunit.Performance;

namespace System.Linq.Tests
{
    /// <summary>
    /// Classes and methods to unify performance testing logic
    /// </summary>
    public partial class Perf_LinqTestBase
    {
        public class EnumerableWrapper<T> : IEnumerable<T>
        {
            private T[] _array;
            public EnumerableWrapper(T[] array) { _array = array; }

            public IEnumerator<T> GetEnumerator() { return ((IEnumerable<T>)_array).GetEnumerator(); }
            Collections.IEnumerator Collections.IEnumerable.GetEnumerator() { return ((IEnumerable<T>)_array).GetEnumerator(); }
        }

        public class ReadOnlyCollectionWrapper<T> : IReadOnlyCollection<T>
        {
            private T[] _array;
            public ReadOnlyCollectionWrapper(T[] array) { _array = array; }

            public int Count { get { return _array.Length; } }

            public IEnumerator<T> GetEnumerator() { return ((IEnumerable<T>)_array).GetEnumerator(); }
            Collections.IEnumerator Collections.IEnumerable.GetEnumerator() { return ((IEnumerable<T>)_array).GetEnumerator(); }
        }

        public class ReadOnlyListWrapper<T> : IReadOnlyList<T>
        {
            private T[] _array;
            public ReadOnlyListWrapper(T[] array) { _array = array; }

            public int Count { get { return _array.Length; } }
            public T this[int index] { get { return _array[index]; } }

            public IEnumerator<T> GetEnumerator() { return ((IEnumerable<T>)_array).GetEnumerator(); }
            Collections.IEnumerator Collections.IEnumerable.GetEnumerator() { return ((IEnumerable<T>)_array).GetEnumerator(); }
        }

        public class CollectionWrapper<T> : ICollection<T>
        {
            private T[] _array;
            public CollectionWrapper(T[] array) { _array = array; }

            public int Count { get { return _array.Length; } }
            public bool IsReadOnly { get { return true; } }
            public bool Contains(T item)
            {
                return Array.IndexOf(_array, item) >= 0;
            }
            public void CopyTo(T[] array, int arrayIndex)
            {
                _array.CopyTo(array, arrayIndex);
            }

            public void Add(T item) {  throw new NotImplementedException(); }
            public void Clear()  {  throw new NotImplementedException(); }
            public bool Remove(T item) { throw new NotImplementedException(); }

            public IEnumerator<T> GetEnumerator() { return ((IEnumerable<T>)_array).GetEnumerator(); }
            Collections.IEnumerator Collections.IEnumerable.GetEnumerator() { return ((IEnumerable<T>)_array).GetEnumerator(); }
        }

        public class ListWrapper<T> : IList<T>
        {
            private T[] _array;
            public ListWrapper(T[] array) { _array = array; }

            public int Count { get { return _array.Length; } }
            public bool IsReadOnly { get { return true; } }
            public T this[int index]
            {
                get { return _array[index]; }
                set { throw new NotImplementedException(); }
            }
            public bool Contains(T item)
            {
                return Array.IndexOf(_array, item) >= 0;
            }
            public void CopyTo(T[] array, int arrayIndex)
            {
                _array.CopyTo(array, arrayIndex);
            }
            public int IndexOf(T item)
            {
                return Array.IndexOf(_array, item);
            }

            public void Add(T item) { throw new NotImplementedException(); }
            public void Clear() { throw new NotImplementedException(); }
            public bool Remove(T item) { throw new NotImplementedException(); }
            public void Insert(int index, T item) { throw new NotImplementedException(); }
            public void RemoveAt(int index) { throw new NotImplementedException(); }


            public IEnumerator<T> GetEnumerator() { return ((IEnumerable<T>)_array).GetEnumerator(); }
            Collections.IEnumerator Collections.IEnumerable.GetEnumerator() { return ((IEnumerable<T>)_array).GetEnumerator(); }
        }

        // =============

        /// <summary>
        /// Measures the time of iteration over IEnumerable sequence
        /// </summary>
        /// <typeparam name="T">Elements type</typeparam>
        /// <param name="source">Sequence</param>
        /// <param name="iterationCount">Number of passes</param>
        public static void MeasureIteration<T>(IEnumerable<T> source, int iterationCount)
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < iterationCount; i++)
                    {
                        foreach (var item in source)
                        { }
                    }
                }
            }
        }

        /// <summary>
        /// Measures the time of materialization of IEnumerable sequence to Array
        /// </summary>
        /// <typeparam name="T">Elements type</typeparam>
        /// <param name="source">Sequence</param>
        /// <param name="iterationCount">Number of passes</param>
        public static void MeasureMaterializationToArray<T>(IEnumerable<T> source, int iterationCount)
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < iterationCount; i++)
                    {
                        source.ToArray();
                    }
                }
            }
        }

        /// <summary>
        /// Measures the time of materialization of IEnumerable sequence to List
        /// </summary>
        /// <typeparam name="T">Elements type</typeparam>
        /// <param name="source">Sequence</param>
        /// <param name="iterationCount">Number of passes</param>
        /// <returns>Measured time</returns>
        public static void MeasureMaterializationToList<T>(IEnumerable<T> source, int iterationCount)
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < iterationCount; i++)
                    {
                        source.ToList();
                    }
                }
            }
        }

        /// <summary>
        /// Measures the time of materialization of IEnumerable sequence to Dictionary
        /// </summary>
        /// <typeparam name="T">Elements type</typeparam>
        /// <param name="source">Sequence</param>
        /// <param name="iterationCount">Number of passes</param>
        public static void MeasureMaterializationToDictionary<T>(IEnumerable<T> source, int iterationCount)
        {
            int count = 0;
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < iterationCount; i++)
                    {
                        source.ToDictionary(key => count++);
                    }
                }
            }
        }

        // ===============

        public enum WrapperType
        {
            NoWrap,
            IEnumerable,
            IReadOnlyCollection,
            IReadOnlyList,
            ICollection,
            IList
        }

        /// <summary>
        /// Wrap array with one of wrapper types
        /// </summary>
        public static IEnumerable<T> Wrap<T>(T[] source, WrapperType wrapperKind)
        {
            switch (wrapperKind)
            {
                case WrapperType.NoWrap:
                    return source;
                case WrapperType.IEnumerable:
                    return new EnumerableWrapper<T>(source);
                case WrapperType.ICollection:
                    return new CollectionWrapper<T>(source);
                case WrapperType.IReadOnlyCollection:
                    return new ReadOnlyCollectionWrapper<T>(source);
                case WrapperType.IReadOnlyList:
                    return new ReadOnlyListWrapper<T>(source);
                case WrapperType.IList:
                    return new ListWrapper<T>(source);
            }

            return source;
        }

        /// <summary>
        /// Main method to measure performance.
        /// Creates array of Int32 with length 'elementCount', wraps it by one of the wrapper, applies LINQ and measures materialization to Array
        /// </summary>
        public static void Measure<TElement>(int elementCount, int iterationCount, WrapperType wrapperKind, Func<IEnumerable<int>, IEnumerable<TElement>> applyLINQ)
        {
            int[] data = Enumerable.Range(0, elementCount).ToArray();
            IEnumerable<int> wrapper = Wrap(data, wrapperKind);

            IEnumerable<TElement> linqExpr = applyLINQ(wrapper);
            MeasureMaterializationToArray(linqExpr, iterationCount);
        }

        /// <summary>
        /// Main method to measure performance.
        /// Creates array of TSource with length 'elementCount', wraps it by one of the wrapper, applies LINQ and measures materialization to Array
        /// </summary>
        public static void Measure<TSource, TElement>(int elementCount, int iterationCount, TSource defaultValue, WrapperType wrapperKind, Func<IEnumerable<TSource>, IEnumerable<TElement>> applyLINQ)
        {
            TSource[] data = Enumerable.Repeat(defaultValue, elementCount).ToArray();
            IEnumerable<TSource> wrapper = Wrap(data, wrapperKind);

            IEnumerable<TElement> linqExpr = applyLINQ(wrapper);
            MeasureMaterializationToArray(linqExpr, iterationCount);
        }

        // ===========
    }
}
