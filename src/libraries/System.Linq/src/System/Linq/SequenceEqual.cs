// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static bool SequenceEqual<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second) =>
            SequenceEqual(first, second, null);

        public static bool SequenceEqual<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource>? comparer)
        {
            if (first == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.first);
            }

            if (second == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.second);
            }

            if (comparer == null)
            {
                // It's relatively common to see code (especially in tests and testing frameworks) that ends up
                // using Enumerable.SequenceEqual to compare two byte arrays.  Using ReadOnlySpan.SequenceEqual
                // is significantly faster than accessing each byte via the array's IList<byte> interface
                // implementation.  So, we special-case byte[] here.  It would be nice to be able to delegate
                // to ReadOnlySpan.SequenceEqual for all TSource[] arrays where TSource is a value type and
                // implements IEquatable<TSource>, but there's no good way without reflection to convince
                // the C# compiler to let us delegate, as ReadOnlySpan.SequenceEqual requires an IEquatable<T>
                // constraint on its type parameter, and Enumerable.SequenceEqual lacks one on its type parameter.
                if (typeof(TSource) == typeof(byte) && first is byte[] firstArr && second is byte[] secondArr)
                {
                    return ((ReadOnlySpan<byte>)firstArr).SequenceEqual(secondArr);
                }

                comparer = EqualityComparer<TSource>.Default;
            }

            if (first is ICollection<TSource> firstCol && second is ICollection<TSource> secondCol)
            {
                if (firstCol.Count != secondCol.Count)
                {
                    return false;
                }

                if (firstCol is IList<TSource> firstList && secondCol is IList<TSource> secondList)
                {
                    int count = firstCol.Count;
                    for (int i = 0; i < count; i++)
                    {
                        if (!comparer.Equals(firstList[i], secondList[i]))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            using (IEnumerator<TSource> e1 = first.GetEnumerator())
            using (IEnumerator<TSource> e2 = second.GetEnumerator())
            {
                while (e1.MoveNext())
                {
                    if (!(e2.MoveNext() && comparer.Equals(e1.Current, e2.Current)))
                    {
                        return false;
                    }
                }

                return !e2.MoveNext();
            }
        }
    }
}
