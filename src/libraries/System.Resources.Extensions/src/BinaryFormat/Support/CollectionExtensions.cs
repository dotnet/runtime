// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Collections.Generic;

internal static class CollectionExtensions
{
    /// <summary>
    ///  Creates a list trimmed to the given count.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   This is an optimized implementation that avoids iterating over the entire list when possible.
    ///  </para>
    /// </remarks>
    internal static List<T> CreateTrimmedList<T>(this IReadOnlyList<T> readOnlyList, int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(readOnlyList.Count, count, nameof(count));

        // List<T> will use ICollection<T>.CopyTo if it's available, which is faster than iterating over the list.
        // If we just have an array this can be done easily with ArraySegment<T>.
        if (readOnlyList is T[] array)
        {
            return new List<T>(new ArraySegment<T>(array, 0, count));
        }

        // Fall back to just setting the count (by removing).
        List<T> list = new(readOnlyList);
        list.RemoveRange(count, list.Count - count);
        return list;
    }
}
