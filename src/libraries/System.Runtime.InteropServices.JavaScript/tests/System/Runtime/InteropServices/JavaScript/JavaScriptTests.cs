// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        public static void FunctionSumCall()
        {
            var sum = new Function("a", "b", "return a + b");
            Assert.Equal(8, (int)sum.Call(null, 3, 5));
        }

        // public static double FunctionSumCallD (double a, double b) 
        // {
        //     var sum = new Function("a", "b", "return a + b");
        //     return Math.Round((double)sum.Call(null, a, b), 2);
        // }
        // public static int FunctionSumApply (int a, int b) 
        // {
        //     var sum = new Function("a", "b", "return a + b");
        //     return (int)sum.Apply(null, new object[] { a, b });
        // }

        // public static double FunctionSumApplyD (double a, double b) 
        // {
        //     var sum = new Function("a", "b", "return a + b");
        //     return Math.Round((double)sum.Apply(null, new object[] { a, b }), 2);
        // }

        // public static object FunctionMathMin (WebAssembly.Core.Array array) 
        // {
        //     object[] parms = new object[array.Length];
        //     for (int x = 0; x < array.Length; x++)
        //         parms[x] = array[x];

        //     var math = (JSObject)Runtime.GetGlobalObject("Math");
        //     var min = (Function)math.GetObjectProperty("min");
        //     return min.Apply(null, parms);
        // }

        // public static DataView DataViewConstructor () 
        // {
        //     // create an ArrayBuffer with a size in bytes
        //     var buffer = new ArrayBuffer(16);

        //     // Create a couple of views
        //     var view1 = new DataView(buffer);
        //     var view2 = new DataView(buffer,12,4); //from byte 12 for the next 4 bytes
        //     view1.SetInt8(12, 42); // put 42 in slot 12            
        //     return view2;
        // }
        // public static DataView DataViewArrayBuffer (ArrayBuffer buffer) 
        // {
        //     var view1 = new DataView(buffer);
        //     return view1;
        // }
        // public static DataView DataViewByteLength (ArrayBuffer buffer) 
        // {
        //     var x = new DataView(buffer, 4, 2);
        //     return x;
        // }
        // public static DataView DataViewByteOffset (ArrayBuffer buffer) 
        // {
        //     var x = new DataView(buffer, 4, 2);
        //     return x;
        // }
        // public static float DataViewGetFloat32 (DataView view) 
        // {
        //     return view.GetFloat32(1);
        // }
        // public static double DataViewGetFloat64 (DataView view) 
        // {
        //     return view.GetFloat64(1);
        // }

        // public static short DataViewGetInt16 (DataView view) 
        // {
        //     return view.GetInt16(1);
        // }

        // public static int DataViewGetInt32 (DataView view) 
        // {
        //     return view.GetInt32(1);
        // }

        // public static sbyte DataViewGetInt8 (DataView view) 
        // {
        //     return view.GetInt8(1);
        // }

        // public static ushort DataViewGetUint16 (DataView view) 
        // {
        //     return view.GetUint16(1);
        // }

        // public static uint DataViewGetUint32 (DataView view) 
        // {
        //     return view.GetUint32(1);
        // }

        // public static byte DataViewGetUint8 (DataView view) 
        // {
        //     return view.GetUint8(1);
        // }

        // public static DataView DataViewSetFloat32 () 
        // {
        //     // create an ArrayBuffer with a size in bytes
        //     var buffer = new ArrayBuffer(16);

        //     var view = new DataView(buffer);
        //     view.SetFloat32(1, (float)Math.PI);
        //     return view;
        // }

        // public static DataView DataViewSetFloat64 () 
        // {
        //     var x = new DataView(new ArrayBuffer(12), 0);
        //     x.SetFloat64(1, Math.PI);        
        //     return x;
        // }
        
        // public static DataView DataViewSetInt16 () 
        // {
        //     var x = new DataView(new ArrayBuffer(12), 0);
        //     x.SetInt16(1, 1234);
        //     return x;
        // }
        
        // public static DataView DataViewSetInt32 () 
        // {
        //     var x = new DataView(new ArrayBuffer(12), 0);
        //     x.SetInt32(1, 1234);
        //     return x;
        // }
        
        // public static DataView DataViewSetInt8 () 
        // {
        //     var x = new DataView(new ArrayBuffer(12), 0);
        //     x.SetInt8(1, 123);
        //     return x;
        // }
        
        // public static DataView DataViewSetUint16 () 
        // {
        //     var x = new DataView(new ArrayBuffer(12), 0);
        //     x.SetUint16(1, 1234);
        //     return x;
        // }
        
        // public static DataView DataViewSetUint32 () 
        // {
        //     var x = new DataView(new ArrayBuffer(12), 0);
        //     x.SetUint32(1, 1234);
        //     return x;
        // }
        
        // public static DataView DataViewSetUint8 () 
        // {
        //     var x = new DataView(new ArrayBuffer(12), 0);
        //     x.SetUint8(1, 123);
        //     return x;
        // }

        // public static object ArrayPop () 
        // {
        //     var arr = new WebAssembly.Core.Array();
        //     return arr.Pop();
        // }

        // public static int ParameterTest () 
        // { 
        //     return -1;
        // }

        // public static int ParameterTest2 (string param1) 
        // { 
        //     return -1;
        // }
        // public static bool StringIsNull (string param1) 
        // { 
        //     return param1 == null;
        // }
        // public static bool StringIsNullOrEmpty (string param1) 
        // { 
        //     return string.IsNullOrEmpty(param1);
        // }
        // public static bool StringArrayIsNull (string[] param1) 
        // { 
        //     return param1 == null;
        // }        
        // public static Uri StringToUri (string uri) 
        // { 
        //     return new Uri(uri);
        // }
        // public unsafe void* PassReturnPtr (void *ptr)
        // {
        //     return ptr;
        // }
    }
}
