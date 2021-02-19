// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.JavaScript;
using System.Collections.Generic;
using Xunit;

namespace System.Runtime.InteropServices.JavaScript.Tests
{
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
        public static void MarshalNullStringToCS()
        {
            HelperMarshal._stringResource = null;
            Runtime.InvokeJS("App.call_test_method(\"InvokeString\", [ null ])");
            Assert.Null(HelperMarshal._stringResource);
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

        [Theory]
        [InlineData(byte.MinValue)]
        [InlineData(byte.MaxValue)]
        [InlineData(SByte.MinValue)]
        [InlineData(SByte.MaxValue)]
        [InlineData(uint.MaxValue)]
        [InlineData(uint.MinValue)]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        [InlineData(double.MaxValue)]
        [InlineData(double.MinValue)]
        public static void InvokeUnboxNumberString(object o)
        {
            HelperMarshal._marshalledObject = o;
            HelperMarshal._object1 = HelperMarshal._object2 = null;
            var value = Runtime.InvokeJS(@"
                var obj = App.call_test_method (""InvokeReturnMarshalObj"");
                var res = App.call_test_method (""InvokeObj1"", [ obj.toString() ]);
            ");

            Assert.Equal(o.ToString().ToLower(), HelperMarshal._object1);
        }

        [Theory]
        [InlineData(byte.MinValue, 0)]
        [InlineData(byte.MaxValue, 255)]
        [InlineData(SByte.MinValue, -128)]
        [InlineData(SByte.MaxValue, 127)]
        [InlineData(uint.MaxValue)]
        [InlineData(uint.MinValue, 0)]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        [InlineData(double.MaxValue)]
        [InlineData(double.MinValue)]
        public static void InvokeUnboxNumber(object o, object expected = null)
        {
            HelperMarshal._marshalledObject = o;
            HelperMarshal._object1 = HelperMarshal._object2 = null;
            Runtime.InvokeJS(@"
                var obj = App.call_test_method (""InvokeReturnMarshalObj"");
                var res = App.call_test_method (""InvokeObj1"", [ obj ]);
            ");

            Assert.Equal(expected ?? o, HelperMarshal._object1);
        }

        [Theory]
        [InlineData(byte.MinValue, 0)]
        [InlineData(byte.MaxValue, 255)]
        [InlineData(SByte.MinValue, -128)]
        [InlineData(SByte.MaxValue, 127)]
        [InlineData(uint.MaxValue)]
        [InlineData(uint.MinValue, 0)]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        [InlineData(double.MaxValue)]
        [InlineData(double.MinValue)]
        public static void InvokeUnboxStringNumber(object o, object expected = null)
        {
            HelperMarshal._marshalledObject = HelperMarshal._object1 = HelperMarshal._object2 = null;
            Runtime.InvokeJS(String.Format (@"
                var res = App.call_test_method (""InvokeObj1"", [ {0} ]);
            ", o));

            Assert.Equal (expected ?? o, HelperMarshal._object1);
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

        private static void RunMarshalTypedArrayJS(string type) {
            Runtime.InvokeJS(@"
                var obj = { };
                App.call_test_method (""SetTypedArray" + type + @""", [ obj ]);
                App.call_test_method (""GetTypedArray" + type + @""", [ obj ]);
            ");
        }

        [Fact]
        public static void MarshalTypedArraySByte()
        {
            RunMarshalTypedArrayJS("SByte");
            Assert.Equal(11, HelperMarshal._taSByte.Length);
            Assert.Equal(32, HelperMarshal._taSByte[0]);
            Assert.Equal(32, HelperMarshal._taSByte[HelperMarshal._taSByte.Length - 1]);
        }

        [Fact]
        public static void MarshalTypedArrayByte()
        {
            RunMarshalTypedArrayJS("Byte");
            Assert.Equal(17, HelperMarshal._taByte.Length);
            Assert.Equal(104, HelperMarshal._taByte[0]);
            Assert.Equal(115, HelperMarshal._taByte[HelperMarshal._taByte.Length - 1]);
            Assert.Equal("hic sunt dracones", System.Text.Encoding.Default.GetString(HelperMarshal._taByte));
        }

        [Fact]
        public static void MarshalTypedArrayShort()
        {
            RunMarshalTypedArrayJS("Short");
            Assert.Equal(13, HelperMarshal._taShort.Length);
            Assert.Equal(32, HelperMarshal._taShort[0]);
            Assert.Equal(32, HelperMarshal._taShort[HelperMarshal._taShort.Length - 1]);
        }

        [Fact]
        public static void MarshalTypedArrayUShort()
        {
            RunMarshalTypedArrayJS("UShort");
            Assert.Equal(14, HelperMarshal._taUShort.Length);
            Assert.Equal(32, HelperMarshal._taUShort[0]);
            Assert.Equal(32, HelperMarshal._taUShort[HelperMarshal._taUShort.Length - 1]);
        }

        [Fact]
        public static void MarshalTypedArrayInt()
        {
            RunMarshalTypedArrayJS("Int");
            Assert.Equal(15, HelperMarshal._taInt.Length);
            Assert.Equal(32, HelperMarshal._taInt[0]);
            Assert.Equal(32, HelperMarshal._taInt[HelperMarshal._taInt.Length - 1]);
        }

        [Fact]
        public static void MarshalTypedArrayUInt()
        {
            RunMarshalTypedArrayJS("UInt");
            Assert.Equal(16, HelperMarshal._taUInt.Length);
            Assert.Equal(32, (int)HelperMarshal._taUInt[0]);
            Assert.Equal(32, (int)HelperMarshal._taUInt[HelperMarshal._taUInt.Length - 1]);
        }

        [Fact]
        public static void MarshalTypedArrayFloat()
        {
            RunMarshalTypedArrayJS("Float");
            Assert.Equal(17, HelperMarshal._taFloat.Length);
            Assert.Equal(3.14f, HelperMarshal._taFloat[0]);
            Assert.Equal(3.14f, HelperMarshal._taFloat[HelperMarshal._taFloat.Length - 1]);
        }

        [Fact]
        public static void MarshalTypedArrayDouble()
        {
            RunMarshalTypedArrayJS("Double");
            Assert.Equal(18, HelperMarshal._taDouble.Length);
            Assert.Equal(3.14d, HelperMarshal._taDouble[0]);
            Assert.Equal(3.14d, HelperMarshal._taDouble[HelperMarshal._taDouble.Length - 1]);
        }

        [Fact]
        public static void TestFunctionSum()
        {
            HelperMarshal._sumValue = 0;
            Runtime.InvokeJS(@"
                App.call_test_method (""CreateFunctionSum"", []);
                App.call_test_method (""CallFunctionSum"", []);
            ");
            Assert.Equal(8, HelperMarshal._sumValue);
        }

        [Fact]
        public static void TestFunctionApply()
        {
            HelperMarshal._minValue = 0;
            Runtime.InvokeJS(@"
                App.call_test_method (""CreateFunctionApply"", []);
                App.call_test_method (""CallFunctionApply"", []);
            ");
            Assert.Equal(2, HelperMarshal._minValue);
        }
        
        [Fact]
        public static void BoundStaticMethodMissingArgs()
        {
            // TODO: We currently have code that relies on this behavior (missing args default to 0) but
            //  it would be better if it threw an exception about the missing arguments. This test is here
            //  to ensure we do not break things by accidentally changing this behavior -kg

            HelperMarshal._intValue = 1;
            Runtime.InvokeJS(@$"
                var invoke_int = Module.mono_bind_static_method (""{HelperMarshal.INTEROP_CLASS}InvokeInt"");
                invoke_int ();
            ");
            Assert.Equal(0, HelperMarshal._intValue);
        }
        
        [Fact]
        public static void BoundStaticMethodExtraArgs()
        {
            HelperMarshal._intValue = 0;
            Runtime.InvokeJS(@$"
                var invoke_int = Module.mono_bind_static_method (""{HelperMarshal.INTEROP_CLASS}InvokeInt"");
                invoke_int (200, 400);
            ");
            Assert.Equal(200, HelperMarshal._intValue);
        }
        
        [Fact]
        public static void BoundStaticMethodArgumentTypeCoercion()
        {
            // TODO: As above, the type coercion behavior on display in this test is not ideal, but
            //  changing it risks breakage in existing code so for now it is verified by a test -kg

            HelperMarshal._intValue = 0;
            Runtime.InvokeJS(@$"
                var invoke_int = Module.mono_bind_static_method (""{HelperMarshal.INTEROP_CLASS}InvokeInt"");
                invoke_int (""200"");
            ");
            Assert.Equal(200, HelperMarshal._intValue);

            Runtime.InvokeJS(@$"
                var invoke_int = Module.mono_bind_static_method (""{HelperMarshal.INTEROP_CLASS}InvokeInt"");
                invoke_int (400.5);
            ");
            Assert.Equal(400, HelperMarshal._intValue);
        }
        
        [Fact]
        public static void BoundStaticMethodUnpleasantArgumentTypeCoercion()
        {
            HelperMarshal._intValue = 100;
            Runtime.InvokeJS(@$"
                var invoke_int = Module.mono_bind_static_method (""{HelperMarshal.INTEROP_CLASS}InvokeInt"");
                invoke_int (""hello"");
            ");
            Assert.Equal(0, HelperMarshal._intValue);

            // In this case at the very least, the leading "7" is not turned into the number 7
            Runtime.InvokeJS(@$"
                var invoke_int = Module.mono_bind_static_method (""{HelperMarshal.INTEROP_CLASS}InvokeInt"");
                invoke_int (""7apples"");
            ");
            Assert.Equal(0, HelperMarshal._intValue);
        }

        [Fact]
        public static void PassUintArgument()
        {
            HelperMarshal._uintValue = 0;
            Runtime.InvokeJS(@$"
                var invoke_uint = Module.mono_bind_static_method (""{HelperMarshal.INTEROP_CLASS}InvokeUInt"");
                invoke_uint (0xFFFFFFFE);
            ");

            Assert.Equal(0xFFFFFFFEu, HelperMarshal._uintValue);
        }
        
        [Fact]
        public static void ReturnUintEnum ()
        {
            HelperMarshal._uintValue = 0;
            HelperMarshal._enumValue = TestEnum.BigValue;
            Runtime.InvokeJS(@$"
                var get_value = Module.mono_bind_static_method (""{HelperMarshal.INTEROP_CLASS}GetEnumValue"");
                var e = get_value ();
                var invoke_uint = Module.mono_bind_static_method (""{HelperMarshal.INTEROP_CLASS}InvokeUInt"");
                invoke_uint (e);
            ");
            Assert.Equal((uint)TestEnum.BigValue, HelperMarshal._uintValue);
        }
        
        [Fact]
        public static void PassUintEnumByValue ()
        {
            HelperMarshal._enumValue = TestEnum.Zero;
            Runtime.InvokeJS(@$"
                var set_enum = Module.mono_bind_static_method (""{HelperMarshal.INTEROP_CLASS}SetEnumValue"", ""j"");
                set_enum (0xFFFFFFFE);
            ");
            Assert.Equal(TestEnum.BigValue, HelperMarshal._enumValue);
        }
        
        [Fact]
        public static void PassUintEnumByValueMasqueradingAsInt ()
        {
            HelperMarshal._enumValue = TestEnum.Zero;
            // HACK: We're explicitly telling the bindings layer to pass an int here, not an enum
            // Because we know the enum is : uint, this is compatible, so it works.
            Runtime.InvokeJS(@$"
                var set_enum = Module.mono_bind_static_method (""{HelperMarshal.INTEROP_CLASS}SetEnumValue"", ""i"");
                set_enum (0xFFFFFFFE);
            ");
            Assert.Equal(TestEnum.BigValue, HelperMarshal._enumValue);
        }
        
        [Fact]
        public static void PassUintEnumByNameIsNotImplemented ()
        {
            HelperMarshal._enumValue = TestEnum.Zero;
            var exc = Assert.Throws<JSException>( () => 
                Runtime.InvokeJS(@$"
                    var set_enum = Module.mono_bind_static_method (""{HelperMarshal.INTEROP_CLASS}SetEnumValue"", ""j"");
                    set_enum (""BigValue"");
                ")
            );
            Assert.StartsWith("Error: Expected numeric value for enum argument, got 'BigValue'", exc.Message);
        }
        
        [Fact]
        public static void CannotUnboxUint64 ()
        {
            var exc = Assert.Throws<JSException>( () => 
                Runtime.InvokeJS(@$"
                    var get_u64 = Module.mono_bind_static_method (""{HelperMarshal.INTEROP_CLASS}GetUInt64"", """");
                    var u64 = get_u64();
                ")
            );
            Assert.StartsWith("Error: int64 not available", exc.Message);
        }

        [Fact]
        public static void BareStringArgumentsAreNotInterned()
        {
            HelperMarshal._stringResource = HelperMarshal._stringResource2 = null;
            Runtime.InvokeJS(@"
                var jsLiteral = ""hello world"";
                App.call_test_method (""InvokeString"", [ jsLiteral ]);
                App.call_test_method (""InvokeString2"", [ jsLiteral ]);
            ");
            Assert.Equal("hello world", HelperMarshal._stringResource);
            Assert.Equal(HelperMarshal._stringResource, HelperMarshal._stringResource2);
            Assert.False(Object.ReferenceEquals(HelperMarshal._stringResource, HelperMarshal._stringResource2));
        }

        [Fact]
        public static void InternedStringSignaturesAreInternedOnJavascriptSide()
        {
            HelperMarshal._stringResource = HelperMarshal._stringResource2 = null;
            Runtime.InvokeJS(@"
                var sym = ""interned string"";
                App.call_test_method (""InvokeString"", [ sym ], ""S"");
                App.call_test_method (""InvokeString2"", [ sym ], ""S"");
            ");
            Assert.Equal("interned string", HelperMarshal._stringResource);
            Assert.Equal(HelperMarshal._stringResource, HelperMarshal._stringResource2);
            Assert.True(Object.ReferenceEquals(HelperMarshal._stringResource, HelperMarshal._stringResource2));
        }

        [Fact]
        public static void OnceAJSStringIsInternedItIsAlwaysUsedIfPossible()
        {
            HelperMarshal._stringResource = HelperMarshal._stringResource2 = null;
            Runtime.InvokeJS(@"
                var sym = ""interned string 2"";
                App.call_test_method (""InvokeString"", [ sym ], ""S"");
                App.call_test_method (""InvokeString2"", [ sym ], ""s"");
            ");
            Assert.Equal("interned string 2", HelperMarshal._stringResource);
            Assert.Equal(HelperMarshal._stringResource, HelperMarshal._stringResource2);
            Assert.True(Object.ReferenceEquals(HelperMarshal._stringResource, HelperMarshal._stringResource2));
        }

        [Fact]
        public static void ManuallyInternString()
        {
            HelperMarshal._stringResource = HelperMarshal._stringResource2 = null;
            Runtime.InvokeJS(@"
                var sym = BINDING.mono_intern_string(""interned string 3"");
                App.call_test_method (""InvokeString"", [ sym ], ""s"");
                App.call_test_method (""InvokeString2"", [ sym ], ""s"");
            ");
            Assert.Equal("interned string 3", HelperMarshal._stringResource);
            Assert.Equal(HelperMarshal._stringResource, HelperMarshal._stringResource2);
            Assert.True(Object.ReferenceEquals(HelperMarshal._stringResource, HelperMarshal._stringResource2));
        }

        [Fact]
        public static void LargeStringsAreNotAutomaticallyLocatedInInternTable()
        {
            HelperMarshal._stringResource = HelperMarshal._stringResource2 = null;
            Runtime.InvokeJS(@"
                var s = ""long interned string"";
                for (var i = 0; i < 1024; i++)
                    s += String(i % 10);
                var sym = BINDING.mono_intern_string(s);
                App.call_test_method (""InvokeString"", [ sym ], ""S"");
                App.call_test_method (""InvokeString2"", [ sym ], ""s"");
            ");
            Assert.Equal(HelperMarshal._stringResource, HelperMarshal._stringResource2);
            Assert.False(Object.ReferenceEquals(HelperMarshal._stringResource, HelperMarshal._stringResource2));
        }

        [Fact]
        public static void CanInternVeryManyStrings()
        {
            HelperMarshal._stringResource = null;
            Runtime.InvokeJS(@"
                for (var i = 0; i < 10240; i++)
                    BINDING.mono_intern_string('s' + i);
                App.call_test_method (""InvokeString"", [ 's5000' ], ""S"");
            ");
            Assert.Equal("s5000", HelperMarshal._stringResource);
            Assert.Equal(HelperMarshal._stringResource, string.IsInterned(HelperMarshal._stringResource));
        }

        [Fact]
        public static void SymbolsAreMarshaledAsStrings()
        {
            HelperMarshal._stringResource = HelperMarshal._stringResource2 = null;
            Runtime.InvokeJS(@"
                var jsLiteral = Symbol(""custom symbol"");
                App.call_test_method (""InvokeString"", [ jsLiteral ]);
                App.call_test_method (""InvokeString2"", [ jsLiteral ]);
            ");
            Assert.Equal("custom symbol", HelperMarshal._stringResource);
            Assert.Equal(HelperMarshal._stringResource, HelperMarshal._stringResource2);
            Assert.True(Object.ReferenceEquals(HelperMarshal._stringResource, HelperMarshal._stringResource2));
        }

        [Fact]
        public static void InternedStringReturnValuesWork()
        {
            HelperMarshal._stringResource = HelperMarshal._stringResource2 = null;
            var fqn = "[System.Private.Runtime.InteropServices.JavaScript.Tests]System.Runtime.InteropServices.JavaScript.Tests.HelperMarshal:StoreArgumentAndReturnLiteral";
            Runtime.InvokeJS(
                $"var a = BINDING.bind_static_method('{fqn}')('test');\r\n" +
                $"var b = BINDING.bind_static_method('{fqn}')(a);\r\n" +
                "App.call_test_method ('InvokeString2', [ b ]);"
            );
            Assert.Equal("s: 1 length: 1", HelperMarshal._stringResource);
            Assert.Equal("1", HelperMarshal._stringResource2);
        }
    }
}
