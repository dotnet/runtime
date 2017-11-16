// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.MemoryTests
{
    public static partial class MemoryTests
    {
        [Fact]
        public static void CtorImplicitArray()
        {
            int[] intArray = { 19, -17 };
            Cast(intArray, 19, -17);

            Memory<int> memoryInt = intArray;
            CastReadOnly(memoryInt, 19, -17);

            long[] longArray = { 1, -3, 7, -15, 31 };
            Cast<long>(longArray, 1, -3, 7, -15, 31);

            Memory<long> memoryLong = longArray;
            CastReadOnly<long>(memoryLong, 1, -3, 7, -15, 31);

            object o1 = new object();
            object o2 = new object();
            object o3 = new object();
            object o4 = new object();
            object[] objectArray = { o1, o2, o3, o4 };
            CastReference(objectArray, o1, o2, o3, o4);

            Memory<object> memoryObject = objectArray;
            CastReadOnlyReference(memoryObject, o1, o2, o3, o4);
        }

        [Fact]
        public static void CtorImplicitZeroLengthArray()
        {
            int[] emptyArray1 = Array.Empty<int>();
            Cast<int>(emptyArray1);

            Memory<int> memoryEmpty = emptyArray1;
            CastReadOnly<int>(memoryEmpty);

            int[] emptyArray2 = new int[0];
            Cast<int>(emptyArray2);

            Memory<int> memoryEmptyInt = emptyArray2;
            CastReadOnly<int>(memoryEmptyInt);

            object[] emptyObjectArray = new object[0];
            CastReference<object>(emptyObjectArray);

            Memory<object> memoryObject = emptyObjectArray;
            CastReadOnlyReference<object>(memoryObject);
        }

        [Fact]
        public static void CtorImplicitDefaultMemory()
        {
            Memory<int> memoryEmpty = default;
            CastReadOnly<int>(memoryEmpty);

            Memory<object> memoryObject = default;
            CastReadOnlyReference<object>(memoryObject);
        }

        [Fact]
        public static void CtorImplicitArraySegment()
        {
            int[] a = { 19, -17 };
            ArraySegment<int> segmentInt = new ArraySegment<int>(a, 1, 1);
            Cast(segmentInt, -17);

            Memory<int> memoryInt = segmentInt;
            CastReadOnly(memoryInt, -17);

            long[] b = { 1, -3, 7, -15, 31 };
            ArraySegment<long> segmentLong = new ArraySegment<long>(b, 1, 3);
            Cast<long>(segmentLong, -3, 7, -15);

            Memory<long> memoryLong = segmentLong;
            CastReadOnly<long>(memoryLong, -3, 7, -15);

            object o1 = new object();
            object o2 = new object();
            object o3 = new object();
            object o4 = new object();
            object[] c = { o1, o2, o3, o4 };
            ArraySegment<object> segmentObject = new ArraySegment<object>(c, 0, 2);
            CastReference(segmentObject, o1, o2);

            Memory<object> memoryObject = segmentObject;
            CastReadOnlyReference(memoryObject, o1, o2);
        }

        [Fact]
        public static void CtorImplicitZeroLengthArraySegment()
        {
            int[] empty = Array.Empty<int>();
            ArraySegment<int> emptySegment = new ArraySegment<int>(empty);
            Cast<int>(emptySegment);

            Memory<int> memoryEmpty = emptySegment;
            CastReadOnly<int>(memoryEmpty);

            int[] a = { 19, -17 };
            ArraySegment<int> segmentInt = new ArraySegment<int>(a, 1, 0);
            Cast<int>(segmentInt);

            Memory<int> memoryEmptyInt = segmentInt;
            CastReadOnly<int>(memoryEmptyInt);
        }

        private static void Cast<T>(Memory<T> memory, params T[] expected) where T : struct, IEquatable<T>
        {
            memory.Validate(expected);
        }

        private static void CastReference<T>(Memory<T> memory, params T[] expected)
        {
            memory.ValidateReferenceType(expected);
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
