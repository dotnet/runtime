// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    internal static class ArraySegmentExtensions
    {
#if !NET
        public static ArraySegment<T> Slice<T>(this ArraySegment<T> segment, int index)
        {
            return new ArraySegment<T>(segment.Array, segment.Offset + index, segment.Count - index);
        }
#endif

        public static T At<T>(this ArraySegment<T> segment, int index)
        {
            return segment.Array![segment.Offset + index];
        }
    }
}
