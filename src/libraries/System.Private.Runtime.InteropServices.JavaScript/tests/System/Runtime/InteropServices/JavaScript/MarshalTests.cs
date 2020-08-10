// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.JavaScript;
using System.Collections.Generic;
using Xunit;

namespace System.Runtime.InteropServices.JavaScript.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/40112")]
    public static class MarshalTests
    {
        [Fact]
        public static void MarshalPrimitivesToCS()
        {
            HelperMarshal._i32Value = 0;
            Runtime.InvokeJS("App.call_test_method (\"InvokeI32\", [10, 20])");
            Assert.Equal(30, HelperMarshal._i32Value);

            HelperMarshal._f32Value = 0;
            Runtime.InvokeJS("App.call_test_method (\"InvokeFloat\", [1.5])");
            Assert.Equal(1.5f, HelperMarshal._f32Value);

            HelperMarshal._f64Value = 0;
            Runtime.InvokeJS("App.call_test_method (\"InvokeDouble\", [4.5])");
            Assert.Equal(4.5, HelperMarshal._f64Value);

            HelperMarshal._i64Value = 0;
            Runtime.InvokeJS("App.call_test_method (\"InvokeLong\", [99])");
            Assert.Equal(99, HelperMarshal._i64Value);
        }

        [Fact]
        public static void MarshalArrayBuffer()
        {
            Runtime.InvokeJS(@"
                var buffer = new ArrayBuffer(16);
                App.call_test_method (""MarshalArrayBuffer"", [ buffer ]);
            ");
            Assert.Equal(16, HelperMarshal._byteBuffer.Length);
        }

        [Fact]
        public static void MarshalArrayBuffer2Int()
        {
            Runtime.InvokeJS(@"
                var buffer = new ArrayBuffer(16);
                var int32View = new Int32Array(buffer);
                for (var i = 0; i < int32View.length; i++) {
                    int32View[i] = i * 2;
                }
                App.call_test_method (""MarshalArrayBufferToInt32Array"", [ buffer ]);
            ");

            Assert.Equal(4, HelperMarshal._intBuffer.Length);
            Assert.Equal(0, HelperMarshal._intBuffer[0]);
            Assert.Equal(2, HelperMarshal._intBuffer[1]);
            Assert.Equal(4, HelperMarshal._intBuffer[2]);
            Assert.Equal(6, HelperMarshal._intBuffer[3]);
        }

        [Fact]
        public static void MarshalArrayBuffer2Int2()
        {
            Runtime.InvokeJS(@"
                var buffer = new ArrayBuffer(16);
                var int32View = new Int32Array(buffer);
                for (var i = 0; i < int32View.length; i++) {
                    int32View[i] = i * 2;
                }
                App.call_test_method (""MarshalByteBufferToInts"", [ buffer ]);		
            ");

            Assert.Equal(4, HelperMarshal._intBuffer.Length);
            Assert.Equal(0, HelperMarshal._intBuffer[0]);
            Assert.Equal(2, HelperMarshal._intBuffer[1]);
            Assert.Equal(4, HelperMarshal._intBuffer[2]);
            Assert.Equal(6, HelperMarshal._intBuffer[3]);
        }

        [Fact]
        public static void MarshalStringToCS()
        {
            HelperMarshal._stringResource = null;
            Runtime.InvokeJS("App.call_test_method(\"InvokeString\", [\"hello\"])");
            Assert.Equal("hello", HelperMarshal._stringResource);
        }

        [Fact]
        public static void MarshalStringToJS()
        {
            HelperMarshal._marshalledString = HelperMarshal._stringResource = null;
            Runtime.InvokeJS(@"
                var str = App.call_test_method (""InvokeMarshalString"");
                App.call_test_method (""InvokeString"", [ str ]);
            ");
            Assert.NotNull(HelperMarshal._marshalledString);
            Assert.Equal(HelperMarshal._marshalledString, HelperMarshal._stringResource);
        }

        [Fact]
        public static void JSObjectKeepIdentityAcrossCalls()
        {
            HelperMarshal._object1 = HelperMarshal._object2 = null;
            Runtime.InvokeJS(@"
                var obj = { foo: 10 };
                var res = App.call_test_method (""InvokeObj1"", [ obj ]);
                App.call_test_method (""InvokeObj2"", [ res ]);
            ");

            Assert.NotNull(HelperMarshal._object1);
            Assert.Same(HelperMarshal._object1, HelperMarshal._object2);
        }

        [Fact]
        public static void CSObjectKeepIdentityAcrossCalls()
        {
            HelperMarshal._marshalledObject = HelperMarshal._object1 = HelperMarshal._object2 = null;
            Runtime.InvokeJS(@"
                var obj = App.call_test_method (""InvokeMarshalObj"");
                var res = App.call_test_method (""InvokeObj1"", [ obj ]);
                App.call_test_method (""InvokeObj2"", [ res ]);
            ");

            Assert.NotNull(HelperMarshal._object1);
            Assert.Same(HelperMarshal._marshalledObject, HelperMarshal._object1);
            Assert.Same(HelperMarshal._object1, HelperMarshal._object2);
        }

        [Fact]
        public static void JSInvokeInt()
        {
            Runtime.InvokeJS(@"
                var obj = {
                    foo: 10,
                    inc: function() {
                        var c = this.foo;
                        ++this.foo;
                        return c;
                    },
                    add: function(val){
                        return this.foo + val;
                    }
                };
                App.call_test_method (""ManipulateObject"", [ obj ]);
            ");
            Assert.Equal(10, HelperMarshal._valOne);
            Assert.Equal(31, HelperMarshal._valTwo);
        }

        [Fact]
        public static void JSInvokeTypes()
        {
            Runtime.InvokeJS(@"
                var obj = {
                    return_int: function() { return 100; },
                    return_double: function() { return 4.5; },
                    return_string: function() { return 'Hic Sunt Dracones'; },
                    return_bool: function() { return true; },
                };
                App.call_test_method (""MinipulateObjTypes"", [ obj ]);
            ");

            Assert.Equal(100, HelperMarshal._jsObjects[0]);
            Assert.Equal(4.5, HelperMarshal._jsObjects[1]);
            Assert.Equal("Hic Sunt Dracones", HelperMarshal._jsObjects[2]);
            Assert.NotEqual("HIC SVNT LEONES", HelperMarshal._jsObjects[2]);
            Assert.Equal(true, HelperMarshal._jsObjects[3]);
        }

        [Fact]
        public static void JSObjectApply()
        {
            Runtime.InvokeJS(@"
                var do_add = function(a, b) { return a + b };
                App.call_test_method (""UseFunction"", [ do_add ]);
            ");
            Assert.Equal(30, HelperMarshal._jsAddFunctionResult);
        }

        [Fact]
        public static void JSObjectAsFunction()
        {
            Runtime.InvokeJS(@"
                var do_add = function(a, b) { return a + b };
                App.call_test_method (""UseAsFunction"", [ do_add ]);
            ");
            Assert.Equal(50, HelperMarshal._jsAddAsFunctionResult);
        }

        [Fact]
        public static void MarshalDelegate()
        {
            HelperMarshal._object1 = null;
            Runtime.InvokeJS(@"
                var funcDelegate = App.call_test_method (""CreateFunctionDelegate"", [  ]);
                var res = funcDelegate (10, 20);
                App.call_test_method (""InvokeI32"", [ res, res ]);
            ");

            Assert.Equal(30, HelperMarshal._functionResultValue);
            Assert.Equal(60, HelperMarshal._i32Value);
        }
        [Fact]
        public static void BindStaticMethod()
        {
            HelperMarshal._intValue = 0;
            Runtime.InvokeJS(@$"
                var invoke_int = Module.mono_bind_static_method (""{HelperMarshal.INTEROP_CLASS}InvokeInt"");
                invoke_int (200);
            ");

            Assert.Equal(200, HelperMarshal._intValue);
        }

        [Fact]
        public static void BindIntPtrStaticMethod()
        {
            HelperMarshal._intPtrValue = IntPtr.Zero;
            Runtime.InvokeJS(@$"
                var invoke_int_ptr = Module.mono_bind_static_method (""{HelperMarshal.INTEROP_CLASS}InvokeIntPtr"");
                invoke_int_ptr (42);
            ");
            Assert.Equal(42, (int)HelperMarshal._intPtrValue);
        }

        [Fact]
        public static void MarshalIntPtrToJS()
        {
            HelperMarshal._marshaledIntPtrValue = IntPtr.Zero;
            Runtime.InvokeJS(@$"
                var invokeMarshalIntPtr = Module.mono_bind_static_method (""{HelperMarshal.INTEROP_CLASS}InvokeMarshalIntPtr"");
                var r = invokeMarshalIntPtr ();

                if (r != 42) throw `Invalid int_ptr value`;
            ");
            Assert.Equal(42, (int)HelperMarshal._marshaledIntPtrValue);
        }

        [Fact]
        public static void InvokeStaticMethod()
        {
            HelperMarshal._intValue = 0;
            Runtime.InvokeJS(@$"
                Module.mono_call_static_method (""{HelperMarshal.INTEROP_CLASS}InvokeInt"", [ 300 ]);
            ");

            Assert.Equal(300, HelperMarshal._intValue);
        }

        [Fact]
        public static void ResolveMethod()
        {
            HelperMarshal._intValue = 0;
            Runtime.InvokeJS(@$"
                var invoke_int = Module.mono_method_resolve (""{HelperMarshal.INTEROP_CLASS}InvokeInt"");
                App.call_test_method (""InvokeInt"", [ invoke_int ]);
            ");

            Assert.NotEqual(0, HelperMarshal._intValue);
        }

        [Fact]
        public static void GetObjectProperties()
        {
            Runtime.InvokeJS(@"
                var obj = {myInt: 100, myDouble: 4.5, myString: ""Hic Sunt Dracones"", myBoolean: true};
                App.call_test_method (""RetrieveObjectProperties"", [ obj ]);		
            ");

            Assert.Equal(100, HelperMarshal._jsProperties[0]);
            Assert.Equal(4.5, HelperMarshal._jsProperties[1]);
            Assert.Equal("Hic Sunt Dracones", HelperMarshal._jsProperties[2]);
            Assert.Equal(true, HelperMarshal._jsProperties[3]);
        }

        [Fact]
        public static void SetObjectProperties()
        {
            Runtime.InvokeJS(@"
                var obj = {myInt: 200, myDouble: 0, myString: ""foo"", myBoolean: false};
                App.call_test_method (""PopulateObjectProperties"", [ obj, false ]);		
                App.call_test_method (""RetrieveObjectProperties"", [ obj ]);		
            ");

            Assert.Equal(100, HelperMarshal._jsProperties[0]);
            Assert.Equal(4.5, HelperMarshal._jsProperties[1]);
            Assert.Equal("qwerty", HelperMarshal._jsProperties[2]);
            Assert.Equal(true, HelperMarshal._jsProperties[3]);
        }

        [Fact]
        public static void SetObjectPropertiesIfNotExistsFalse()
        {
            // This test will not create the properties if they do not already exist
            Runtime.InvokeJS(@"
                var obj = {myInt: 200};
                App.call_test_method (""PopulateObjectProperties"", [ obj, false ]);		
                App.call_test_method (""RetrieveObjectProperties"", [ obj ]);		
            ");

            Assert.Equal(100, HelperMarshal._jsProperties[0]);
            Assert.Null(HelperMarshal._jsProperties[1]);
            Assert.Null(HelperMarshal._jsProperties[2]);
            Assert.Null(HelperMarshal._jsProperties[3]);
        }

        [Fact]
        public static void SetObjectPropertiesIfNotExistsTrue()
        {
            // This test will set the value of the property if it exists and will create and 
            // set the value if it does not exists
            Runtime.InvokeJS(@"
                var obj = {myInt: 200};
                App.call_test_method (""PopulateObjectProperties"", [ obj, true ]);
                App.call_test_method (""RetrieveObjectProperties"", [ obj ]);
            ");

            Assert.Equal(100, HelperMarshal._jsProperties[0]);
            Assert.Equal(4.5, HelperMarshal._jsProperties[1]);
            Assert.Equal("qwerty", HelperMarshal._jsProperties[2]);
            Assert.Equal(true, HelperMarshal._jsProperties[3]);
        }

        [Fact]
        public static void MarshalTypedArray()
        {
            Runtime.InvokeJS(@"
                var buffer = new ArrayBuffer(16);
                var uint8View = new Uint8Array(buffer);
                App.call_test_method (""MarshalByteBuffer"", [ uint8View ]);		
            ");

            Assert.Equal(16, HelperMarshal._byteBuffer.Length);
        }

        [Fact]
        public static void MarshalTypedArray2Int()
        {
            Runtime.InvokeJS(@"
                var buffer = new ArrayBuffer(16);
                var int32View = new Int32Array(buffer);
                for (var i = 0; i < int32View.length; i++) {
                    int32View[i] = i * 2;
                }
                App.call_test_method (""MarshalInt32Array"", [ int32View ]);
            ");

            Assert.Equal(4, HelperMarshal._intBuffer.Length);
            Assert.Equal(0, HelperMarshal._intBuffer[0]);
            Assert.Equal(2, HelperMarshal._intBuffer[1]);
            Assert.Equal(4, HelperMarshal._intBuffer[2]);
            Assert.Equal(6, HelperMarshal._intBuffer[3]);
        }

        [Fact]
        public static void MarshalTypedArray2Float()
        {
            Runtime.InvokeJS(@"
                var typedArray = new Float32Array([1, 2.1334, 3, 4.2, 5]);
                App.call_test_method (""MarshalFloat32Array"", [ typedArray ]);		
            ");

            Assert.Equal(1, HelperMarshal._floatBuffer[0]);
            Assert.Equal(2.1334f, HelperMarshal._floatBuffer[1]);
            Assert.Equal(3, HelperMarshal._floatBuffer[2]);
            Assert.Equal(4.2f, HelperMarshal._floatBuffer[3]);
            Assert.Equal(5, HelperMarshal._floatBuffer[4]);
        }

        [Fact]
        public static void MarshalArrayBuffer2Float2()
        {
            Runtime.InvokeJS(@"
                var buffer = new ArrayBuffer(16);
                var float32View = new Float32Array(buffer);
                for (var i = 0; i < float32View.length; i++) {
                    float32View[i] = i * 2.5;
                }
                App.call_test_method (""MarshalArrayBufferToFloat32Array"", [ buffer ]);		
            ");

            Assert.Equal(4, HelperMarshal._floatBuffer.Length);
            Assert.Equal(0, HelperMarshal._floatBuffer[0]);
            Assert.Equal(2.5f, HelperMarshal._floatBuffer[1]);
            Assert.Equal(5, HelperMarshal._floatBuffer[2]);
            Assert.Equal(7.5f, HelperMarshal._floatBuffer[3]);
        }

        [Fact]
        public static void MarshalTypedArray2Double()
        {
            Runtime.InvokeJS(@"
			var typedArray = new Float64Array([1, 2.1334, 3, 4.2, 5]);
			App.call_test_method (""MarshalFloat64Array"", [ typedArray ]);		
		");

            Assert.Equal(1, HelperMarshal._doubleBuffer[0]);
            Assert.Equal(2.1334d, HelperMarshal._doubleBuffer[1]);
            Assert.Equal(3, HelperMarshal._doubleBuffer[2]);
            Assert.Equal(4.2d, HelperMarshal._doubleBuffer[3]);
            Assert.Equal(5, HelperMarshal._doubleBuffer[4]);
        }

        [Fact]
        public static void MarshalArrayBuffer2Double()
        {
            Runtime.InvokeJS(@"
                var buffer = new ArrayBuffer(32);
                var float64View = new Float64Array(buffer);
                for (var i = 0; i < float64View.length; i++) {
                    float64View[i] = i * 2.5;
                }
                App.call_test_method (""MarshalByteBufferToDoubles"", [ buffer ]);		
            ");

            Assert.Equal(4, HelperMarshal._doubleBuffer.Length);
            Assert.Equal(0, HelperMarshal._doubleBuffer[0]);
            Assert.Equal(2.5d, HelperMarshal._doubleBuffer[1]);
            Assert.Equal(5, HelperMarshal._doubleBuffer[2]);
            Assert.Equal(7.5d, HelperMarshal._doubleBuffer[3]);
        }

        [Fact]
        public static void MarshalArrayBuffer2Double2()
        {
            Runtime.InvokeJS(@"
                var buffer = new ArrayBuffer(32);
                var float64View = new Float64Array(buffer);
                for (var i = 0; i < float64View.length; i++) {
                    float64View[i] = i * 2.5;
                }
                App.call_test_method (""MarshalArrayBufferToFloat64Array"", [ buffer ]);		
            ");

            Assert.Equal(4, HelperMarshal._doubleBuffer.Length);
            Assert.Equal(0, HelperMarshal._doubleBuffer[0]);
            Assert.Equal(2.5f, HelperMarshal._doubleBuffer[1]);
            Assert.Equal(5, HelperMarshal._doubleBuffer[2]);
            Assert.Equal(7.5f, HelperMarshal._doubleBuffer[3]);
        }

        [Fact]
        public static void MarshalTypedArraySByte()
        {
            Runtime.InvokeJS(@"
                var obj = { };
                App.call_test_method (""SetTypedArraySByte"", [ obj ]);
                App.call_test_method (""GetTypedArraySByte"", [ obj ]);
            ");
            Assert.Equal(11, HelperMarshal._taSByte.Length);
            Assert.Equal(32, HelperMarshal._taSByte[0]);
            Assert.Equal(32, HelperMarshal._taSByte[HelperMarshal._taSByte.Length - 1]);
        }

        [Fact]
        public static void MarshalTypedArrayByte()
        {
            Runtime.InvokeJS(@"
                var obj = { };
                App.call_test_method (""SetTypedArrayByte"", [ obj ]);
                App.call_test_method (""GetTypedArrayByte"", [ obj ]);
            ");
            Assert.Equal(11, HelperMarshal._taSByte.Length);
            Assert.Equal(104, HelperMarshal._taByte[0]);
            Assert.Equal(115, HelperMarshal._taByte[HelperMarshal._taByte.Length - 1]);
            Assert.Equal("hic sunt dracones", System.Text.Encoding.Default.GetString(HelperMarshal._taByte));
        }

        [Fact]
        public static void MarshalTypedArrayShort()
        {
            Runtime.InvokeJS(@"
                var obj = { };
                App.call_test_method (""SetTypedArrayShort"", [ obj ]);
                App.call_test_method (""GetTypedArrayShort"", [ obj ]);
            ");
            Assert.Equal(13, HelperMarshal._taShort.Length);
            Assert.Equal(32, HelperMarshal._taShort[0]);
            Assert.Equal(32, HelperMarshal._taShort[HelperMarshal._taShort.Length - 1]);
        }

        [Fact]
        public static void MarshalTypedArrayUShort()
        {
            Runtime.InvokeJS(@"
                var obj = { };
                App.call_test_method (""SetTypedArrayUShort"", [ obj ]);
                App.call_test_method (""GetTypedArrayUShort"", [ obj ]);
            ");
            Assert.Equal(14, HelperMarshal._taUShort.Length);
            Assert.Equal(32, HelperMarshal._taUShort[0]);
            Assert.Equal(32, HelperMarshal._taUShort[HelperMarshal._taUShort.Length - 1]);
        }

        [Fact]
        public static void MarshalTypedArrayInt()
        {
            Runtime.InvokeJS(@"
                var obj = { };
                App.call_test_method (""SetTypedArrayInt"", ""o"", [ obj ]);
                App.call_test_method (""GetTypedArrayInt"", ""o"", [ obj ]);
            ");
            Assert.Equal(15, HelperMarshal._taInt.Length);
            Assert.Equal(32, HelperMarshal._taInt[0]);
            Assert.Equal(32, HelperMarshal._taInt[HelperMarshal._taInt.Length - 1]);
        }

        [Fact]
        public static void MarshalTypedArrayUInt()
        {
            Runtime.InvokeJS(@"
                var obj = { };
                App.call_test_method (""SetTypedArrayUInt"", [ obj ]);
                App.call_test_method (""GetTypedArrayUInt"", [ obj ]);
            ");
            Assert.Equal(16, HelperMarshal._taUInt.Length);
            Assert.Equal(32, (int)HelperMarshal._taUInt[0]);
            Assert.Equal(32, (int)HelperMarshal._taUInt[HelperMarshal._taUInt.Length - 1]);
        }

        [Fact]
        public static void MarshalTypedArrayFloat()
        {
            Runtime.InvokeJS(@"
                var obj = { };
                App.call_test_method (""SetTypedArrayFloat"", [ obj ]);
                App.call_test_method (""GetTypedArrayFloat"", [ obj ]);
            ");
            Assert.Equal(17, HelperMarshal._taFloat.Length);
            Assert.Equal(3.14f, HelperMarshal._taFloat[0]);
            Assert.Equal(3.14f, HelperMarshal._taFloat[HelperMarshal._taFloat.Length - 1]);
        }

        [Fact]
        public static void MarshalTypedArrayDouble()
        {
            Runtime.InvokeJS(@"
                var obj = { };
                App.call_test_method (""SetTypedArrayDouble"", ""o"", [ obj ]);
                App.call_test_method (""GetTypedArrayDouble"", ""o"", [ obj ]);
            ");
            Assert.Equal(18, HelperMarshal._taDouble.Length);
            Assert.Equal(3.14d, HelperMarshal._taDouble[0]);
            Assert.Equal(3.14d, HelperMarshal._taDouble[HelperMarshal._taDouble.Length - 1]);
        }

        [Fact]
        public static void TestFunctionSum()
        {
            HelperMarshal._sumValue = 0;
            Runtime.InvokeJS(@"
                App.call_test_method (""CreateFunctionSum"", null, [ ]);
                App.call_test_method (""CallFunctionSum"", null, [  ]);
            ");
            Assert.Equal(8, HelperMarshal._sumValue);
        }

        [Fact]
        public static void TestFunctionApply()
        {
            HelperMarshal._minValue = 0;
            Runtime.InvokeJS(@"
                App.call_test_method (""CreateFunctionApply"", null, [ ]);
                App.call_test_method (""CallFunctionApply"", null, [  ]);
            ");
            Assert.Equal(2, HelperMarshal._minValue);
        }
    }
}
