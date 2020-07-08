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
            HelperMarshal.i32_res = 0;
            Runtime.InvokeJS("App.call_test_method(\"InvokeI32\", \"ii\", [10, 20])");
            Assert.Equal(30, HelperMarshal.i32_res);

            HelperMarshal.f32_res = 0;
            Runtime.InvokeJS("App.call_test_method(\"InvokeFloat\", \"f\", [1.5])");
            Assert.Equal(1.5f, HelperMarshal.f32_res);

            HelperMarshal.f64_res = 0;
            Runtime.InvokeJS("App.call_test_method(\"InvokeDouble\", \"d\", [4.5])");
            Assert.Equal(4.5, HelperMarshal.f64_res);

            HelperMarshal.i64_res = 0;
            Runtime.InvokeJS("App.call_test_method(\"InvokeLong\", \"l\", [99])");
            Assert.Equal(99, HelperMarshal.i64_res);
        }

        [Fact]
        public static void MarshalTypedArray()
        {
            Runtime.InvokeJS(@"
                var buffer = new ArrayBuffer(16);
                var uint8View = new Uint8Array(buffer);
                App.call_test_method (""MarshalByteBuffer"", ""o"", [ uint8View ]);		
            ");

            Assert.Equal(16, HelperMarshal.byteBuffer.Length);
        }

        [Fact]
        public static void MarshalArrayBuffer()
        {
            Runtime.InvokeJS(@"
                var buffer = new ArrayBuffer(16);
                App.call_test_method (""MarshalArrayBuffer"", ""o"", [ buffer ]);		
            ");

            Assert.Equal(16, HelperMarshal.byteBuffer.Length);
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
                App.call_test_method (""MarshalArrayBufferToInt32Array"", ""o"", [ buffer ]);		
            ");

            Assert.Equal(4, HelperMarshal.intBuffer.Length);
            Assert.Equal(0, HelperMarshal.intBuffer[0]);
            Assert.Equal(2, HelperMarshal.intBuffer[1]);
            Assert.Equal(4, HelperMarshal.intBuffer[2]);
            Assert.Equal(6, HelperMarshal.intBuffer[3]);
        }

        [Fact]
        public static void MarshalStringToCS()
        {
            HelperMarshal._stringResource = null;
            Runtime.InvokeJS("App.call_test_method(\"InvokeString\", \"s\", [\"hello\"])");
            Assert.Equal("hello", HelperMarshal._stringResource);
        }

        [Fact]
        public static void MarshalStringToJS()
        {
            HelperMarshal._marshalledString = HelperMarshal._stringResource = null;
            Runtime.InvokeJS(@"
                var str = App.call_test_method (""InvokeMarshalString"", ""o"", [ ]);
                App.call_test_method (""InvokeString"", ""s"", [ str ]);
            ");
            Assert.NotNull(HelperMarshal._marshalledString);
            Assert.Equal(HelperMarshal._marshalledString, HelperMarshal._stringResource);
        }
    }
}
