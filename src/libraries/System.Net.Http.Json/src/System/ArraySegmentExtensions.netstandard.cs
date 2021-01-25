// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    internal static class ArraySegmentExtensions
    {
        public static ArraySegment<T> Slice<T>(this ArraySegment<T> arraySegment, int index)
        {
            return new ArraySegment<T>(arraySegment.Array, arraySegment.Offset + index, arraySegment.Count - index);
        }

        public static ArraySegment<T> Slice<T>(this ArraySegment<T> arraySegment, int index, int count)
        {
            return new ArraySegment<T>(arraySegment.Array, arraySegment.Offset + index, count);
        }

        public static void CopyTo<T>(this ArraySegment<T> source, ArraySegment<T> destination)
        {
            Array.Copy(source.Array, source.Offset, destination.Array, destination.Offset, source.Count);
        }
    }
}
