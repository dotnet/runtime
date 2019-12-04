// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace System.Collections.Generic
{
    internal abstract class ToArrayBase<T>
    {
        private const int maxArraySize = int.MaxValue;

        private const int initialRecursiveDepth = 16;
        private const int itemsPerRecursion = 4;

        protected const int maxSizeForNoAllocations = initialRecursiveDepth * itemsPerRecursion;

        protected T[] FinishViaAllocations(object source, ref int sourceIdx, Func<T, bool>? predicate, int count)
        {
            if (count == maxArraySize)
            {
                throw new IndexOutOfRangeException();
            }

            T[] result;
            int bufferSize = (int)Math.Min((uint)maxArraySize, (uint)count * 2) - count;
            T[] buffer = new T[bufferSize];

            var (index, moveNext) = PopulateBuffer(buffer, source, ref sourceIdx, predicate);

            if (moveNext)
            {
                result = FinishViaAllocations(source, ref sourceIdx, predicate, count + index);
            }
            else
            {
                result = new T[count + index];
            }

            Array.Copy(buffer, 0, result, count, index);
            return result;
        }

        protected abstract (int, bool) PopulateBuffer(T[] buffer, object source, ref int sourceIdx, Func<T, bool>? predicate);

        protected static T[] Allocate(int count)
        {
            if (count == 0)
                return Array.Empty<T>();

            return new T[count];
        }

        protected static T[] AllocateAndAssign(int count, T item1)
        {
            T[] result = new T[count];
            result[--count] = item1;
            return result;
        }

        protected static T[] AllocateAndAssign(int count, T item1, T item2)
        {
            T[] result = new T[count];
            result[--count] = item2;
            result[--count] = item1;
            return result;
        }

        protected static T[] AllocateAndAssign(int count, T item1, T item2, T item3)
        {
            T[] result = new T[count];
            result[--count] = item3;
            result[--count] = item2;
            result[--count] = item1;
            return result;
        }
    }

    internal class ToArrayEnumerable<T> : ToArrayBase<T>
    {
        public static readonly ToArrayEnumerable<T> Instance = new ToArrayEnumerable<T>();

        private ToArrayEnumerable() { }

        public T[] ToArray(IEnumerable<T> source)
        {
            Debug.Assert(source != null);

            if (source is ICollection<T> collection)
            {
                int count = collection.Count;
                if (count == 0)
                {
                    return Array.Empty<T>();
                }

                var result = new T[count];
                collection.CopyTo(result, arrayIndex: 0);
                return result;
            }

            using IEnumerator<T> e = source.GetEnumerator();
            return InitiallyTryWithNoAllocations(e, null, 0);
        }

        public T[] ToArray(IEnumerable<T> source, Func<T, bool> predicate)
        {
            Debug.Assert(source != null);
            using IEnumerator<T> e = source.GetEnumerator();
            return InitiallyTryWithNoAllocations(e, predicate, 0);
        }

        protected T[] InitiallyTryWithNoAllocations(IEnumerator<T> e, Func<T, bool>? predicate, int count)
        {
            T[] result;
            T item1, item2, item3, item4;

            do
            {
                if (!e.MoveNext())
                {
                    return Allocate(count);
                }
                item1 = e.Current;
            }
            while (predicate != null && !predicate(item1));

            ++count;

            do
            {
                if (!e.MoveNext())
                {
                    return AllocateAndAssign(count, item1);
                }
                item2 = e.Current;
            } while (predicate != null && !predicate(item2));

            ++count;

            do
            {
                if (!e.MoveNext())
                {
                    return AllocateAndAssign(count, item1, item2);
                }
                item3 = e.Current;
            }
            while (predicate != null && !predicate(item3));

            ++count;

            do
            {
                if (!e.MoveNext())
                {
                    return AllocateAndAssign(count, item1, item2, item3);
                }
                item4 = e.Current;
            }
            while (predicate != null && !predicate(item4));

            ++count;

            if (count >= maxSizeForNoAllocations)
            {
                if (e.MoveNext())
                    result = FinishViaAllocations(e, ref count, predicate, count);
                else
                    result = Allocate(count);
            }
            else
            {
                result = InitiallyTryWithNoAllocations(e, predicate, count);
            }

            result[--count] = item4;
            result[--count] = item3;
            result[--count] = item2;
            result[--count] = item1;

            return result;
        }

        protected override (int, bool) PopulateBuffer(T[] buffer, object source, ref int sourceIdx, Func<T, bool>? predicate)
        {
            var enumerator = (IEnumerator<T>)source;
            return predicate == null ? PopulateBuffer(buffer, enumerator) : PopulateBuffer(buffer, enumerator, predicate);
        }

        private static (int, bool) PopulateBuffer(T[] buffer, IEnumerator<T> e)
        {
            bool moveNext;
            int index;

            for (moveNext = true, index = 0; moveNext && index < buffer.Length; ++index)
            {
                buffer[index] = e.Current;
                moveNext = e.MoveNext();
            }

            return (index, moveNext);
        }

        private static (int, bool) PopulateBuffer(T[] buffer, IEnumerator<T> e, Func<T, bool> predicate)
        {
            bool moveNext;
            int index;

            for (moveNext = true, index = 0; moveNext && index < buffer.Length; moveNext = e.MoveNext())
            {
                var item = e.Current;

                if (!predicate(item))
                    continue;

                buffer[index++] = e.Current;
            }

            return (index, moveNext);
        }
    }

    internal class ToArrayArray<T> : ToArrayBase<T>
    {
        public static readonly ToArrayArray<T> Instance = new ToArrayArray<T>();

        private ToArrayArray() { }

        public T[] ToArray(T[] source, Func<T, bool> predicate)
        {
            Debug.Assert(source != null);

            return InitiallyTryWithNoAllocations(source, 0, predicate, 0);
        }

        protected T[] InitiallyTryWithNoAllocations(T[] array, int arrayIdx, Func<T, bool> predicate, int count)
        {
            T[] result;
            T item1, item2, item3, item4;

            do
            {
                if (arrayIdx >= array.Length)
                {
                    return Allocate(count);
                }

                item1 = array[arrayIdx++];
            } while (!predicate(item1));

            ++count;

            do
            {
                if (arrayIdx >= array.Length)
                {
                    return AllocateAndAssign(count, item1);
                }
                item2 = array[arrayIdx++];
            }
            while (!predicate(item2));

            ++count;

            do
            {
                if (arrayIdx >= array.Length)
                {
                    return AllocateAndAssign(count, item1, item2);
                }
                item3 = array[arrayIdx++];
            }
            while (!predicate(item3));

            ++count;

            do
            {
                if (arrayIdx >= array.Length)
                {
                    return AllocateAndAssign(count, item1, item2, item3);
                }
                item4 = array[arrayIdx++];
            }
            while (!predicate(item4));

            ++count;

            if (count >= maxSizeForNoAllocations)
            {
                if (arrayIdx < array.Length)
                {
                    result = FinishViaAllocations(array, ref arrayIdx, predicate, count);
                }
                else
                {
                    result = Allocate(count);
                }
            }
            else
            {
                result = InitiallyTryWithNoAllocations(array, arrayIdx, predicate, count);
            }

            result[--count] = item4;
            result[--count] = item3;
            result[--count] = item2;
            result[--count] = item1;

            return result;
        }

        protected override (int, bool) PopulateBuffer(T[] buffer, object source, ref int sourceIdx, Func<T, bool>? predicate)
        {
            Debug.Assert(predicate != null);

            T[] array = (T[])source;

            var bufferIdx = PopulateBuffer(buffer, array, ref sourceIdx, predicate);

            return (bufferIdx, sourceIdx < array.Length);
        }

        private int PopulateBuffer(T[] buffer, T[] source, ref int sourceIdx, Func<T, bool> predicate)
        {
            int bufferIdx;
            int arrayIdx;

            for (arrayIdx = sourceIdx, bufferIdx = 0; arrayIdx < source.Length && bufferIdx < buffer.Length; ++arrayIdx)
            {
                var item = source[arrayIdx];

                if (!predicate(item))
                    continue;

                buffer[bufferIdx++] = item;
            }

            sourceIdx = arrayIdx;
            return bufferIdx;
        }
    }

    internal class ToArrayList<T> : ToArrayBase<T>
    {
        public static readonly ToArrayList<T> Instance = new ToArrayList<T>();

        private ToArrayList() { }

        public T[] ToArray(List<T> source, Func<T, bool> predicate)
        {
            Debug.Assert(source != null);

            return InitiallyTryWithNoAllocations(source, 0, predicate, 0);
        }

        protected T[] InitiallyTryWithNoAllocations(List<T> array, int arrayIdx, Func<T, bool> predicate, int count)
        {
            T[] result;
            T item1, item2, item3, item4;

            do
            {
                if (arrayIdx >= array.Count)
                {
                    return Allocate(count);
                }

                item1 = array[arrayIdx++];
            } while (!predicate(item1));

            ++count;

            do
            {
                if (arrayIdx >= array.Count)
                {
                    return AllocateAndAssign(count, item1);
                }
                item2 = array[arrayIdx++];
            }
            while (!predicate(item2));

            ++count;

            do
            {
                if (arrayIdx >= array.Count)
                {
                    return AllocateAndAssign(count, item1, item2);
                }
                item3 = array[arrayIdx++];
            }
            while (!predicate(item3));

            ++count;

            do
            {
                if (arrayIdx >= array.Count)
                {
                    return AllocateAndAssign(count, item1, item2, item3);
                }
                item4 = array[arrayIdx++];
            }
            while (!predicate(item4));

            ++count;

            if (count >= maxSizeForNoAllocations)
            {
                if (arrayIdx < array.Count)
                {
                    result = FinishViaAllocations(array, ref arrayIdx, predicate, count);
                }
                else
                {
                    result = Allocate(count);
                }
            }
            else
            {
                result = InitiallyTryWithNoAllocations(array, arrayIdx, predicate, count);
            }

            result[--count] = item4;
            result[--count] = item3;
            result[--count] = item2;
            result[--count] = item1;

            return result;
        }

        protected override (int, bool) PopulateBuffer(T[] buffer, object source, ref int sourceIdx, Func<T, bool>? predicate)
        {
            Debug.Assert(predicate != null);

            List<T> array = (List<T>)source;

            var bufferIdx = PopulateBuffer(buffer, array, ref sourceIdx, predicate);

            return (bufferIdx, sourceIdx < array.Count);
        }

        private int PopulateBuffer(T[] buffer, List<T> source, ref int sourceIdx, Func<T, bool> predicate)
        {
            int bufferIdx;
            int arrayIdx;

            for (arrayIdx = sourceIdx, bufferIdx = 0; arrayIdx < source.Count && bufferIdx < buffer.Length; ++arrayIdx)
            {
                var item = source[arrayIdx];

                if (!predicate(item))
                    continue;

                buffer[bufferIdx++] = item;
            }

            sourceIdx = arrayIdx;
            return bufferIdx;
        }
    }
}