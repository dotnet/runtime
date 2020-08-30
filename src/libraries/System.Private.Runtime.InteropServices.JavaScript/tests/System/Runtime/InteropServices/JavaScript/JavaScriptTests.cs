// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.JavaScript;
using Xunit;

namespace System.Runtime.InteropServices.JavaScript.Tests
{
    public static class JavaScriptTests
    {
        [Fact]
        public static void CoreTypes()
        {
            var arr = new Uint8ClampedArray(50);
            Assert.Equal(50, arr.Length);
            Assert.Equal(TypedArrayTypeCode.Uint8ClampedArray, arr.GetTypedArrayType());

            var arr1 = new Uint8Array(50);
            Assert.Equal(50, arr1.Length);
            Assert.Equal(TypedArrayTypeCode.Uint8Array, arr1.GetTypedArrayType());

            var arr2 = new Uint16Array(50);
            Assert.Equal(50, arr2.Length);
            Assert.Equal(TypedArrayTypeCode.Uint16Array, arr2.GetTypedArrayType());

            var arr3 = new Uint32Array(50);
            Assert.Equal(50, arr3.Length);
            Assert.Equal(TypedArrayTypeCode.Uint32Array, arr3.GetTypedArrayType());

            var arr4 = new Int8Array(50);
            Assert.Equal(50, arr4.Length);
            Assert.Equal(TypedArrayTypeCode.Int8Array, arr4.GetTypedArrayType());

            var arr5 = new Int16Array(50);
            Assert.Equal(50, arr5.Length);
            Assert.Equal(TypedArrayTypeCode.Int16Array, arr5.GetTypedArrayType());

            var arr6 = new Int32Array(50);
            Assert.Equal(50, arr6.Length);
            Assert.Equal(TypedArrayTypeCode.Int32Array, arr6.GetTypedArrayType());

            var arr7 = new Float32Array(50);
            Assert.Equal(50, arr7.Length);
            Assert.Equal(TypedArrayTypeCode.Float32Array, arr7.GetTypedArrayType());

            var arr8 = new Float64Array(50);
            Assert.Equal(50, arr8.Length);
            Assert.Equal(TypedArrayTypeCode.Float64Array, arr8.GetTypedArrayType());

            var sharedArr40 = new SharedArrayBuffer(40);
            var sharedArr50 = new SharedArrayBuffer(50);

            var arr9 = new Uint8ClampedArray(sharedArr50);
            Assert.Equal(50, arr9.Length);

            var arr10 = new Uint8Array(sharedArr50);
            Assert.Equal(50, arr10.Length);

            var arr11 = new Uint16Array(sharedArr50);
            Assert.Equal(25, arr11.Length);

            var arr12 = new Uint32Array(sharedArr40);
            Assert.Equal(10, arr12.Length);

            var arr13 = new Int8Array(sharedArr50);
            Assert.Equal(50, arr13.Length);

            var arr14 = new Int16Array(sharedArr40);
            Assert.Equal(20, arr14.Length);

            var arr15 = new Int32Array(sharedArr40);
            Assert.Equal(10, arr15.Length);

            var arr16 = new Float32Array(sharedArr40);
            Assert.Equal(10, arr16.Length);

            var arr17 = new Float64Array(sharedArr40);
            Assert.Equal(5, arr17.Length);
        }

        [Fact]
        public static void FunctionSum()
        {
            // The Difference Between call() and apply()
            // The difference is:
            //      The call() method takes arguments separately.
            //      The apply() method takes arguments as an array.
            var sum = new Function("a", "b", "return a + b");
            Assert.Equal(8, (int)sum.Call(null, 3, 5));

            Assert.Equal(13, (int)sum.Apply(null, new object[] { 6, 7 }));
        }

        [Fact]
        public static void FunctionMath()
        {
            JSObject math = (JSObject)Runtime.GetGlobalObject("Math");
            Assert.NotNull(math);

            Function mathMax = (Function)math.GetObjectProperty("max");
            Assert.NotNull(mathMax);

            var maxValue = (int)mathMax.Apply(null, new object[] { 5, 6, 2, 3, 7 });
            Assert.Equal(7, maxValue);

            maxValue = (int)mathMax.Call(null, 5, 6, 2, 3, 7);
            Assert.Equal(7, maxValue);

            Function mathMin = (Function)((JSObject)Runtime.GetGlobalObject("Math")).GetObjectProperty("min");
            Assert.NotNull(mathMin);

            var minValue = (int)mathMin.Apply(null, new object[] { 5, 6, 2, 3, 7 });
            Assert.Equal(2, minValue);

            minValue = (int)mathMin.Call(null, 5, 6, 2, 3, 7);
            Assert.Equal(2, minValue);
        }
    }
}
