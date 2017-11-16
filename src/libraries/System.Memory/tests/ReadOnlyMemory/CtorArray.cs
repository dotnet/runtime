// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using static System.TestHelpers;

namespace System.MemoryTests
{
    //
    // Tests for Memory<T>.ctor(T[])
    //
    // These tests will also exercise the matching codepaths in Memory<T>.ctor(T[], int, int). This makes it easier to ensure
    // that these parallel tests stay consistent, and avoid excess repetition in the files devoted to those specific overloads.
    //
    public static partial class ReadOnlyMemoryTests
    {
        [Fact]
        public static void CtorArrayInt()
        {
            int[] a = { 91, 92, -93, 94 };
            ReadOnlyMemory<int> memory;

            memory = new ReadOnlyMemory<int>(a);
            memory.Validate(91, 92, -93, 94);

            memory = new ReadOnlyMemory<int>(a, 0, a.Length);
            memory.Validate(91, 92, -93, 94);
        }

        [Fact]
        public static void CtorArrayLong()
        {
            long[] a = { 91, -92, 93, 94, -95 };
            ReadOnlyMemory<long> memory;

            memory = new ReadOnlyMemory<long>(a);
            memory.Validate(91, -92, 93, 94, -95);

            memory = new ReadOnlyMemory<long>(a, 0, a.Length);
            memory.Validate(91, -92, 93, 94, -95);
        }

        [Fact]
        public static void CtorArrayObject()
        {
            object o1 = new object();
            object o2 = new object();
            object[] a = { o1, o2 };
            ReadOnlyMemory<object> memory;

            memory = new ReadOnlyMemory<object>(a);
            memory.ValidateReferenceType(o1, o2);

            memory = new ReadOnlyMemory<object>(a, 0, a.Length);
            memory.ValidateReferenceType(o1, o2);
        }

        [Fact]
        public static void CtorArrayZeroLength()
        {
            int[] empty = Array.Empty<int>();
            ReadOnlyMemory<int> memory;

            memory = new ReadOnlyMemory<int>(empty);
            memory.Validate();

            memory = new ReadOnlyMemory<int>(empty, 0, empty.Length);
            memory.Validate();
        }

        [Fact]
        public static void CtorArrayNullArray()
        {
            Assert.Throws<ArgumentNullException>(() => new ReadOnlyMemory<int>(null));
            Assert.Throws<ArgumentNullException>(() => new ReadOnlyMemory<int>(null, 0, 0));
        }

        [Fact]
        public static void CtorArrayWrongValueType()
        {
            // Can pass variant array, if array type is a valuetype.

            uint[] a = { 42u, 0xffffffffu };
            int[] aAsIntArray = (int[])(object)a;
            ReadOnlyMemory<int> memory;

            memory = new ReadOnlyMemory<int>(aAsIntArray);
            memory.Validate(42, -1);

            memory = new ReadOnlyMemory<int>(aAsIntArray, 0, aAsIntArray.Length);
            memory.Validate(42, -1);
        }

        [Fact]
        public static void CtorVariantArrayType()
        {
            // For ReadOnlyMemory<T>, variant arrays are allowed for string to object
            // and reference type to object.

            ReadOnlyMemory<object> memory;

            string[] strArray = { "Hello" };
            memory = new ReadOnlyMemory<object>(strArray);
            memory.ValidateReferenceType("Hello");
            memory = new ReadOnlyMemory<object>(strArray, 0, strArray.Length);
            memory.ValidateReferenceType("Hello");

            TestClass c1 = new TestClass();
            TestClass c2 = new TestClass();
            TestClass[] clsArray = { c1, c2 };
            memory = new ReadOnlyMemory<object>(clsArray);
            memory.ValidateReferenceType(c1, c2);
            memory = new ReadOnlyMemory<object>(clsArray, 0, clsArray.Length);
            memory.ValidateReferenceType(c1, c2);
        }
    }
}

