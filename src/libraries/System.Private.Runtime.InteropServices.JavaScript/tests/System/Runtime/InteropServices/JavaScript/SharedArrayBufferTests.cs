// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using Xunit;

namespace System.Runtime.InteropServices.JavaScript.Tests
{
    public static class SharedArrayBufferTests
    {
        private static Function _objectPrototype;

        public static IEnumerable<object[]> Object_Prototype()
        {
            _objectPrototype ??= new Function("return Object.prototype.toString;");
            yield return new object[] { _objectPrototype.Call() };
        }

        [Theory]
        [MemberData(nameof(Object_Prototype))]
        public static void SharedArrayBufferLength(Function objectPrototype)
        {
            SharedArrayBuffer d = new SharedArrayBuffer(0);
            Assert.Equal("[object SharedArrayBuffer]", objectPrototype.Call(d));
            Assert.Equal(0, d.Length);
        }

        [Theory]
        [MemberData(nameof(Object_Prototype))]
        public static void SharedArrayBufferLength1(Function objectPrototype)
        {
            SharedArrayBuffer d = new SharedArrayBuffer(50);
            Assert.Equal("[object SharedArrayBuffer]", objectPrototype.Call(d));
            Assert.Equal(0, d.Length);
        }

        [Fact]
        public static void SharedArrayBufferLength2()
        {
            SharedArrayBuffer d = new SharedArrayBuffer(50);
            Assert.Equal(50, d.ByteLength);
        }

        [Fact]
        public static void SharedArrayBufferSlice()
        {
            SharedArrayBuffer d = new SharedArrayBuffer(50);
            Assert.Equal(50, d.Slice().ByteLength);
        }

        [Fact]
        public static void SharedArrayBufferSlice1()
        {
            SharedArrayBuffer d = new SharedArrayBuffer(50);
            Assert.Equal(50, d.Slice(0, 50).ByteLength);
        }

        [Fact]
        public static void SharedArrayBufferSlice2()
        {
            SharedArrayBuffer d = new SharedArrayBuffer(50);
            Assert.Equal(50, d.Slice(0).ByteLength);
        }

        [Fact]
        public static void SharedArrayBufferSlice3()
        {
            SharedArrayBuffer d = new SharedArrayBuffer(50);
            Assert.Equal(3, d.Slice(-3).ByteLength);
        }

        [Fact]
        public static void SharedArrayBufferSlice4()
        {
            SharedArrayBuffer d = new SharedArrayBuffer(50);
            Assert.Equal(3, d.Slice(1, 4).ByteLength);
        }

        [Fact]
        public static void SharedArrayBufferSliceAndDice()
        {
            // create a SharedArrayBuffer with a size in bytes
            SharedArrayBuffer buffer = new SharedArrayBuffer(16);
            Int32Array int32View = new Int32Array(buffer);  // create view
            // produces Int32Array [0, 0, 0, 0]

            int32View[1] = 42;

            Assert.Equal(4, int32View.Length);
            Assert.Equal(42, int32View[1]);

            Int32Array sliced = new Int32Array(buffer.Slice(4,12));
            // expected output: Int32Array [42, 0]

            Assert.Equal(2, sliced.Length);
            Assert.Equal(42, sliced[0]);
            Assert.Equal(0, sliced[1]);
        }

        [Fact]
        public static void SharedArrayBufferSliceAndDice1()
        {
            // create a SharedArrayBuffer with a size in bytes
            SharedArrayBuffer buffer = new SharedArrayBuffer(16);
            Int32Array int32View = new Int32Array(buffer);  // create view
            // produces Int32Array [0, 0, 0, 0]

            int32View[1] = 42;

            Assert.Equal(4, int32View.Length);
            Assert.Equal(42, int32View[1]);

            Int32Array sliced = new Int32Array(buffer.Slice(4,12));
            // expected output: Int32Array [42, 0]

            Span<int> nativeArray = sliced; // error

            int sum = 0;
            for (int i = 0; i < nativeArray.Length; i++)
            {
                sum += nativeArray[i];
            }

            Assert.Equal(42, sum);
        }
    }
}
