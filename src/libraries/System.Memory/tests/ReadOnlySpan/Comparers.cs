// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        private static IEnumerable<IEqualityComparer<T>?> GetDefaultEqualityComparers<T>()
        {
            yield return null;

            yield return EqualityComparer<T>.Default;

            yield return EqualityComparer<T>.Create((i, j) => EqualityComparer<T>.Default.Equals(i, j));

            if (typeof(T) == typeof(string))
            {
                yield return (IEqualityComparer<T>)(object)StringComparer.Ordinal;
            }
        }

        private static IEnumerable<IComparer<T>?> GetDefaultComparers<T>()
        {
            yield return null;

            yield return Comparer<T>.Default;

            yield return Comparer<T>.Create((i, j) => Comparer<T>.Default.Compare(i, j));

            if (typeof(T) == typeof(string))
            {
                yield return (IComparer<T>)(object)StringComparer.Ordinal;
            }
        }

        private static IEqualityComparer<T> GetFalseEqualityComparer<T>() =>
            EqualityComparer<T>.Create((i, j) => false);
    }
}
