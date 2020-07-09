// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices.JavaScript;
using Xunit;

namespace System.Runtime.InteropServices.JavaScript.Tests
{
    public static class MarshalTests
    {
        [Fact]
        public static void MarshalPrimitivesToCS()
        {
            HelperMarshal._i32Value = 0;
            Runtime.InvokeJS("App.call_test_method(\"InvokeI32\", [10, 20])");
            Assert.Equal(30, HelperMarshal._i32Value);

            HelperMarshal._f32Value = 0;
            Runtime.InvokeJS("App.call_test_method(\"InvokeFloat\", [1.5])");
            Assert.Equal(1.5f, HelperMarshal._f32Value);

            HelperMarshal._f64Value = 0;
            Runtime.InvokeJS("App.call_test_method(\"InvokeDouble\", [4.5])");
            Assert.Equal(4.5, HelperMarshal._f64Value);

            HelperMarshal._i64Value = 0;
            Runtime.InvokeJS("App.call_test_method(\"InvokeLong\", [99])");
            Assert.Equal(99, HelperMarshal._i64Value);
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
                var do_add = function(a, b) { return a + b};
                App.call_test_method (""UseFunction"", [ do_add ]);
            ");
            Assert.Equal(30, HelperMarshal._jsAddFunctionResult);
        }

        [Fact]
        public static void JSObjectAsFunction()
        {
            Runtime.InvokeJS(@"
                var do_add = function(a, b) { return a + b};
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
    }
}
