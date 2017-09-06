// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.MemoryTests
{
    //
    // Tests for Memory<T>.ctor(T[])
    //
    // These tests will also exercise the matching codepaths in Memory<T>.ctor(T[], int, int). This makes it easier to ensure
    // that these parallel tests stay consistent, and avoid excess repetition in the files devoted to those specific overloads.
    //
    public static partial class MemoryTests
    {
        [Fact]
        public static void CtorArrayInt()
        {
            int[] a = { 91, 92, -93, 94 };
            Memory<int> memory;

            memory = new Memory<int>(a);
            memory.Validate(91, 92, -93, 94);

            memory = new Memory<int>(a, 0, a.Length);
            memory.Validate(91, 92, -93, 94);
        }

        [Fact]
        public static void CtorArrayLong()
        {
            long[] a = { 91, -92, 93, 94, -95 };
            Memory<long> memory;

            memory = new Memory<long>(a);
            memory.Validate(91, -92, 93, 94, -95);

            memory = new Memory<long>(a, 0, a.Length);
            memory.Validate(91, -92, 93, 94, -95);
        }

        [Fact]
        public static void CtorArrayObject()
        {
            object o1 = new object();
            object o2 = new object();
            object[] a = { o1, o2 };
            Memory<object> memory;

            memory = new Memory<object>(a);
            memory.ValidateReferenceType(o1, o2);

            memory = new Memory<object>(a, 0, a.Length);
            memory.ValidateReferenceType(o1, o2);
        }

        [Fact]
        public static void CtorArrayZeroLength()
        {
            int[] empty = Array.Empty<int>();
            Memory<int> memory;

            memory = new Memory<int>(empty);
            memory.Validate();

            memory = new Memory<int>(empty, 0, empty.Length);
            memory.Validate();
        }

        [Fact]
        public static void CtorArrayNullArray()
        {
            Assert.Throws<ArgumentNullException>(() => new Memory<int>(null));
            Assert.Throws<ArgumentNullException>(() => new Memory<int>(null, 0, 0));
        }

        [Fact]
        public static void CtorArrayWrongArrayType()
        {
            // Cannot pass variant array, if array type is not a valuetype.
            string[] a = { "Hello" };
            Assert.Throws<ArrayTypeMismatchException>(() => new Memory<object>(a));
            Assert.Throws<ArrayTypeMismatchException>(() => new Memory<object>(a, 0, a.Length));
        }

        [Fact]
        public static void CtorArrayWrongValueType()
        {
            // Can pass variant array, if array type is a valuetype.

            uint[] a = { 42u, 0xffffffffu };
            int[] aAsIntArray = (int[])(object)a;
            Memory<int> memory;

            memory = new Memory<int>(aAsIntArray);
            memory.Validate(42, -1);

            memory = new Memory<int>(aAsIntArray, 0, aAsIntArray.Length);
            memory.Validate(42, -1);
        }
    }
}

