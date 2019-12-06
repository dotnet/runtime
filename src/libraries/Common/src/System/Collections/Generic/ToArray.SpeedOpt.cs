// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace System.Collections.Generic
{
    internal abstract class ToArrayBase<T>
    {
        // from https://docs.microsoft.com/en-us/dotnet/framework/configure-apps/file-schema/runtime/gcallowverylargeobjects-element
        private const int maxByteElementsArraySize  = 0X7FFFFFC7;
        private const int maxOtherElementsArraySize = 0X7FEFFFFF;

        protected const int maxArraySize = maxByteElementsArraySize;

        private const int initialRecursiveDepth = 16;
        private const int itemsPerRecursion = 4;

        protected const int maxSizeForNoAllocations = initialRecursiveDepth * itemsPerRecursion;

        private int GetBufferSize(int count, int sourceSize)
        {
            if (count >= maxByteElementsArraySize)
            {
                // We fail here "early" as we could be creating more buffers (which would be smaller thaat the max)
                // but would eventually lead to an excessive allocation which would throw an OutOfMemory exception
                throw new OutOfMemoryException();
            }

            var maxSize =
                count < sourceSize
                ? sourceSize
                :   count < maxOtherElementsArraySize
                    ? maxOtherElementsArraySize
                    : maxByteElementsArraySize;

            return (int)Math.Min((uint)maxSize, (uint)count * 2) - count; ;
        }

        protected T[] FinishViaAllocations(object source, int sourceSize, ref int sourceIdx, Func<T, bool>? predicate, int count)
        {
            T[] result;
            int bufferSize = GetBufferSize(count, sourceSize);
            T[] buffer = new T[bufferSize];

            var (index, moveNext) = PopulateBuffer(buffer, source, ref sourceIdx, predicate);

            if (moveNext)
            {
                result = FinishViaAllocations(source, sourceSize, ref sourceIdx, predicate, count + index);
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

        protected static T[] Assign(T[] result, ref (T, T, T, T) items, int count)
        {
            result[--count] = items.Item4;
            result[--count] = items.Item3;
            result[--count] = items.Item2;
            result[--count] = items.Item1;

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
            return InitiallyTryWithNoAllocations(e, 0);
        }

        public T[] ToArray(IEnumerable<T> source, Func<T, bool> predicate)
        {
            Debug.Assert(source != null);
            using IEnumerator<T> e = source.GetEnumerator();
            return InitiallyTryWithNoAllocations(e, predicate, 0);
        }

        private T[] InitiallyTryWithNoAllocations(IEnumerator<T> source, int count)
        {
            (T, T, T, T) items;

            if (!source.MoveNext())
            {
                return Allocate(count);
            }
            items.Item1 = source.Current;

            ++count;

            if (!source.MoveNext())
            {
                return AllocateAndAssign(count, items.Item1);
            }
            items.Item2 = source.Current;

            ++count;

            if (!source.MoveNext())
            {
                return AllocateAndAssign(count, items.Item1, items.Item2);
            }
            items.Item3 = source.Current;

            ++count;

            if (!source.MoveNext())
            {
                return AllocateAndAssign(count, items.Item1, items.Item2, items.Item3);
            }
            items.Item4 = source.Current;

            ++count;

            T[] result;
            if (count >= maxSizeForNoAllocations)
            {
                if (source.MoveNext())
                    result = FinishViaAllocations(source, count);
                else
                    result = Allocate(count);
            }
            else
            {
                result = InitiallyTryWithNoAllocations(source, count);
            }

            return Assign(result, ref items, count);

            T[] FinishViaAllocations(IEnumerator<T> source, int count)
            {
                int dummyIdx = 0;
                return base.FinishViaAllocations(source, maxArraySize, ref dummyIdx, null, count);
            }
        }

        private T[] InitiallyTryWithNoAllocations(IEnumerator<T> source, Func<T, bool> predicate, int count)
        {
            (T, T, T, T) items;

            do
            {
                if (!source.MoveNext())
                {
                    return Allocate(count);
                }
                items.Item1 = source.Current;
            }
            while (!predicate(items.Item1));

            ++count;

            do
            {
                if (!source.MoveNext())
                {
                    return AllocateAndAssign(count, items.Item1);
                }
                items.Item2 = source.Current;
            } while (!predicate(items.Item2));

            ++count;

            do
            {
                if (!source.MoveNext())
                {
                    return AllocateAndAssign(count, items.Item1, items.Item2);
                }
                items.Item3 = source.Current;
            }
            while (!predicate(items.Item3));

            ++count;

            do
            {
                if (!source.MoveNext())
                {
                    return AllocateAndAssign(count, items.Item1, items.Item2, items.Item3);
                }
                items.Item4 = source.Current;
            }
            while (!predicate(items.Item4));

            ++count;

            T[] result;
            if (count >= maxSizeForNoAllocations)
            {
                if (source.MoveNext())
                    result = FinishViaAllocations(source, predicate, count);
                else
                    result = Allocate(count);
            }
            else
            {
                result = InitiallyTryWithNoAllocations(source, predicate, count);
            }

            return Assign(result, ref items, count);

            T[] FinishViaAllocations(IEnumerator<T> source, Func<T, bool>? predicate, int count)
            {
                int dummyIdx = 0;
                return base.FinishViaAllocations(source, maxArraySize, ref dummyIdx, predicate, count);
            }
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

            for (moveNext = true, index = 0; moveNext && index < buffer.Length; moveNext = e.MoveNext(), ++index)
            {
                buffer[index] = e.Current;
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

                buffer[index++] = item;
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

        private T[] InitiallyTryWithNoAllocations(T[] source, int sourceIdx, Func<T, bool> predicate, int count)
        {
            (T, T, T, T) items;

            do
            {
                if (sourceIdx >= source.Length)
                {
                    return Allocate(count);
                }

                items.Item1 = source[sourceIdx++];
            } while (!predicate(items.Item1));

            ++count;

            do
            {
                if (sourceIdx >= source.Length)
                {
                    return AllocateAndAssign(count, items.Item1);
                }
                items.Item2 = source[sourceIdx++];
            }
            while (!predicate(items.Item2));

            ++count;

            do
            {
                if (sourceIdx >= source.Length)
                {
                    return AllocateAndAssign(count, items.Item1, items.Item2);
                }
                items.Item3 = source[sourceIdx++];
            }
            while (!predicate(items.Item3));

            ++count;

            do
            {
                if (sourceIdx >= source.Length)
                {
                    return AllocateAndAssign(count, items.Item1, items.Item2, items.Item3);
                }
                items.Item4 = source[sourceIdx++];
            }
            while (!predicate(items.Item4));

            ++count;

            T[] result;
            if (count >= maxSizeForNoAllocations)
            {
                if (sourceIdx < source.Length)
                {
                    result = FinishViaAllocations(source, source.Length, ref sourceIdx, predicate, count);
                }
                else
                {
                    result = Allocate(count);
                }
            }
            else
            {
                result = InitiallyTryWithNoAllocations(source, sourceIdx, predicate, count);
            }

            return Assign(result, ref items, count);
        }

        protected override (int, bool) PopulateBuffer(T[] buffer, object source, ref int sourceIdx, Func<T, bool>? predicate)
        {
            Debug.Assert(predicate != null);

            T[] array = (T[])source;

            var bufferIdx = PopulateBuffer(buffer, array, ref sourceIdx, predicate);

            return (bufferIdx, sourceIdx < array.Length);
        }

        private static int PopulateBuffer(T[] buffer, T[] source, ref int sourceIdx, Func<T, bool> predicate)
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

        private T[] InitiallyTryWithNoAllocations(List<T> source, int sourceIdx, Func<T, bool> predicate, int count)
        {
            (T, T, T, T) items;

            do
            {
                if (sourceIdx >= source.Count)
                {
                    return Allocate(count);
                }

                items.Item1 = source[sourceIdx++];
            } while (!predicate(items.Item1));

            ++count;

            do
            {
                if (sourceIdx >= source.Count)
                {
                    return AllocateAndAssign(count, items.Item1);
                }
                items.Item2 = source[sourceIdx++];
            }
            while (!predicate(items.Item2));

            ++count;

            do
            {
                if (sourceIdx >= source.Count)
                {
                    return AllocateAndAssign(count, items.Item1, items.Item2);
                }
                items.Item3 = source[sourceIdx++];
            }
            while (!predicate(items.Item3));

            ++count;

            do
            {
                if (sourceIdx >= source.Count)
                {
                    return AllocateAndAssign(count, items.Item1, items.Item2, items.Item3);
                }
                items.Item4 = source[sourceIdx++];
            }
            while (!predicate(items.Item4));

            ++count;

            T[] result;
            if (count >= maxSizeForNoAllocations)
            {
                if (sourceIdx < source.Count)
                {
                    result = FinishViaAllocations(source, source.Count, ref sourceIdx, predicate, count);
                }
                else
                {
                    result = Allocate(count);
                }
            }
            else
            {
                result = InitiallyTryWithNoAllocations(source, sourceIdx, predicate, count);
            }

            return Assign(result, ref items, count);
        }

        protected override (int, bool) PopulateBuffer(T[] buffer, object source, ref int sourceIdx, Func<T, bool>? predicate)
        {
            Debug.Assert(predicate != null);

            List<T> array = (List<T>)source;

            var bufferIdx = PopulateBuffer(buffer, array, ref sourceIdx, predicate);

            return (bufferIdx, sourceIdx < array.Count);
        }

        private static int PopulateBuffer(T[] buffer, List<T> source, ref int sourceIdx, Func<T, bool> predicate)
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