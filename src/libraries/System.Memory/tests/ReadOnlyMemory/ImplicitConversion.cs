// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.MemoryTests
{
    public static partial class ReadOnlyMemoryTests
    {
        [Fact]
        public static void CtorImplicitArray()
        {
            int[] intArray = { 19, -17 };
            CastReadOnly(intArray, 19, -17);

            long[] longArray = { 1, -3, 7, -15, 31 };
            CastReadOnly<long>(longArray, 1, -3, 7, -15, 31);

            object o1 = new object();
            object o2 = new object();
            object o3 = new object();
            object o4 = new object();
            object[] objectArray = { o1, o2, o3, o4 };
            CastReadOnlyReference(objectArray, o1, o2, o3, o4);
        }

        [Fact]
        public static void CtorImplicitZeroLengthArray()
        {
            int[] emptyArray1 = Array.Empty<int>();
            CastReadOnly<int>(emptyArray1);

            int[] emptyArray2 = new int[0];
            CastReadOnly<int>(emptyArray2);
        }

        [Fact]
        public static void CtorImplicitArraySegment()
        {
            int[] a = { 19, -17 };
            ArraySegment<int> segmentInt = new ArraySegment<int>(a, 1, 1);
            CastReadOnly(segmentInt, -17);

            long[] b = { 1, -3, 7, -15, 31 };
            ArraySegment<long> segmentLong = new ArraySegment<long>(b, 1, 3);
            CastReadOnly<long>(segmentLong, -3, 7, -15);

            object o1 = new object();
            object o2 = new object();
            object o3 = new object();
            object o4 = new object();
            object[] c = { o1, o2, o3, o4 };
            ArraySegment<object> segmentObject = new ArraySegment<object>(c, 0, 2);
            CastReadOnlyReference(segmentObject, o1, o2);
        }

        [Fact]
        public static void CtorImplicitZeroLengthArraySegment()
        {
            int[] empty = Array.Empty<int>();
            ArraySegment<int> emptySegment = new ArraySegment<int>(empty);
            CastReadOnly<int>(emptySegment);

            int[] a = { 19, -17 };
            ArraySegment<int> segmentInt = new ArraySegment<int>(a, 1, 0);
            CastReadOnly<int>(segmentInt);
        }

        private static void CastReadOnly<T>(ReadOnlyMemory<T> memory, params T[] expected) where T : struct, IEquatable<T>
        {
            memory.Validate(expected);
        }

        private static void CastReadOnlyReference<T>(ReadOnlyMemory<T> memory, params T[] expected)
        {
            memory.ValidateReferenceType(expected);
        }
    }
}
