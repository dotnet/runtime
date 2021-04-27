// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.JavaScript;
using System.Collections.Generic;
using Xunit;

namespace System.Runtime.InteropServices.JavaScript.Tests
{
    public static class DelegateTests
    {
        private static Function _objectPrototype;

        [Fact]
        public static void InvokeFunction()
        {
            HelperMarshal._functionResultValue = 0;
            HelperMarshal._i32Value = 0;

            Runtime.InvokeJS(@"
                var funcDelegate = App.call_test_method (""CreateFunctionDelegate"", [  ]);
                var res = funcDelegate (10, 20);
                App.call_test_method (""InvokeI32"", [ res, res ]);
            ");

            Assert.Equal(30, HelperMarshal._functionResultValue);
            Assert.Equal(60, HelperMarshal._i32Value);
        }

        [Fact]
        public static void InvokeFunctionInLoopUsingConstanceValues()
        {
            HelperMarshal._functionResultValue = 0;
            HelperMarshal._i32Value = 0;

            Runtime.InvokeJS(@"
                var funcDelegate = App.call_test_method (""CreateFunctionDelegate"", [  ]);
                var res = funcDelegate (10, 20);
                for (x = 0; x < 1000; x++)
                {
                    res = funcDelegate (10, 20);
                }
                App.call_test_method (""InvokeI32"", [ res, res ]);
            ");

            Assert.Equal(30, HelperMarshal._functionResultValue);
            Assert.Equal(60, HelperMarshal._i32Value);
        }

        [Fact]
        public static void InvokeFunctionInLoopUsingIncrementedValues()
        {
            HelperMarshal._functionResultValue = 0;
            HelperMarshal._i32Value = 0;
            Runtime.InvokeJS(@"
                var funcDelegate = App.call_test_method (""CreateFunctionDelegate"", [  ]);
                var res = funcDelegate (10, 20);
                for (x = 0; x < 1000; x++)
                {
                    res = funcDelegate (x, x);
                }
                App.call_test_method (""InvokeI32"", [ res, res ]);
            ");

            Assert.Equal(1998, HelperMarshal._functionResultValue);
            Assert.Equal(3996, HelperMarshal._i32Value);
        }

        [Fact]
        public static void InvokeActionTReturnedByInvokingFuncT()
        {
            HelperMarshal._functionActionResultValue = 0;
            HelperMarshal._functionActionResultValueOfAction = 0;

            Runtime.InvokeJS(@"
                var funcDelegate = App.call_test_method (""CreateFunctionDelegateWithAction"", [  ]);
                var actionDelegate = funcDelegate (10, 20);
                actionDelegate(30,40);
            ");

            Assert.Equal(30, HelperMarshal._functionActionResultValue);
            Assert.Equal(70, HelperMarshal._functionActionResultValueOfAction);
        }

        [Fact]
        public static void InvokeActionIntInt()
        {
            HelperMarshal._actionResultValue = 0;

            Runtime.InvokeJS(@"
                var actionDelegate = App.call_test_method (""CreateActionDelegate"", [  ]);
                actionDelegate(30,40);
            ");

            Assert.Equal(70, HelperMarshal._actionResultValue);
        }

        [Fact]
        public static void InvokeActionFloatIntToIntInt()
        {
            HelperMarshal._actionResultValue = 0;
            Runtime.InvokeJS(@"
                var actionDelegate = App.call_test_method (""CreateActionDelegate"", [  ]);
                actionDelegate(3.14,40);
            ");

            Assert.Equal(43, HelperMarshal._actionResultValue);
        }

        [Fact]
        public static void InvokeDelegateMethod()
        {
            HelperMarshal._delMethodResultValue = string.Empty;
            Runtime.InvokeJS(@"
                var del = App.call_test_method (""CreateDelegateMethod"", [  ]);
                del(""Hic sunt dracones"");
            ");

            Assert.Equal("Hic sunt dracones", HelperMarshal._delMethodResultValue);
        }

        [Fact]
        public static void InvokeDelegateMethodReturnString()
        {
            HelperMarshal._delMethodStringResultValue = string.Empty;
            Runtime.InvokeJS(@"
                var del = App.call_test_method (""CreateDelegateMethodReturnString"", [  ]);
                var res = del(""Hic sunt dracones"");
                App.call_test_method (""SetTestString1"", [ res ]);
            ");

            Assert.Equal("Received: Hic sunt dracones", HelperMarshal._delMethodStringResultValue);
        }

        [Theory]
        [InlineData("CreateCustomMultiCastDelegate_VoidString", "Moin")]
        [InlineData("CreateMultiCastAction_VoidString", "MoinMoin")]
        public static void InvokeMultiCastDelegate_VoidString(string creator, string testStr)
        {
            HelperMarshal._delegateCallResult = string.Empty;
            Runtime.InvokeJS($@"
                var del = App.call_test_method (""{creator}"", [  ]);
                del(""{testStr}"");
            ");
            Assert.Equal($"  Hello, {testStr}!  GoodMorning, {testStr}!", HelperMarshal._delegateCallResult);
        }
        
        [Theory]
        [InlineData("CreateDelegateFromAnonymousMethod_VoidString")]
        [InlineData("CreateDelegateFromLambda_VoidString")]
        [InlineData("CreateDelegateFromMethod_VoidString")]
        [InlineData("CreateActionT_VoidString")]
        public static void InvokeDelegate_VoidString(string creator)
        {
            HelperMarshal._delegateCallResult = string.Empty;
            var s = Runtime.InvokeJS($@"
                var del = App.call_test_method (""{creator}"", [  ]);
                del(""Hic sunt dracones"");
            ");

            Assert.Equal("Notification received for: Hic sunt dracones", HelperMarshal._delegateCallResult);
        }

        public static IEnumerable<object[]> ArrayType_TestData()
        {
            _objectPrototype ??= new Function("return Object.prototype.toString;");
            yield return new object[] { _objectPrototype.Call(), "Uint8Array", Uint8Array.From(new byte[10]) };
            yield return new object[] { _objectPrototype.Call(), "Uint8ClampedArray", Uint8ClampedArray.From(new byte[10]) };
            yield return new object[] { _objectPrototype.Call(), "Int8Array", Int8Array.From(new sbyte[10]) };
            yield return new object[] { _objectPrototype.Call(), "Uint16Array", Uint16Array.From(new ushort[10]) };
            yield return new object[] { _objectPrototype.Call(), "Int16Array", Int16Array.From(new short[10]) };
            yield return new object[] { _objectPrototype.Call(), "Uint32Array", Uint32Array.From(new uint[10]) };
            yield return new object[] { _objectPrototype.Call(), "Int32Array", Int32Array.From(new int[10]) };
            yield return new object[] { _objectPrototype.Call(), "Float32Array", Float32Array.From(new float[10]) };
            yield return new object[] { _objectPrototype.Call(), "Float64Array", Float64Array.From(new double[10]) };
            yield return new object[] { _objectPrototype.Call(), "Array", new Array(10) };
        }

        [Theory]
        [MemberData(nameof(ArrayType_TestData))]
        public static void InvokeFunctionAcceptingArrayTypes(Function objectPrototype, string creator, JSObject arrayType )
        {
            HelperMarshal._funcActionBufferObjectResultValue = arrayType;
            Assert.Equal(10, HelperMarshal._funcActionBufferObjectResultValue.Length);
            Assert.Equal($"[object {creator}]", objectPrototype.Call(HelperMarshal._funcActionBufferObjectResultValue));

            Runtime.InvokeJS($@"
                var buffer = new {creator}(50);
                var del = App.call_test_method (""CreateFunctionAccepting{creator}"", [  ]);
                var setAction = del(buffer);
                setAction(buffer);
            ");

            Assert.Equal(50, HelperMarshal._funcActionBufferObjectResultValue.Length);
            Assert.Equal(HelperMarshal._funcActionBufferObjectResultValue.Length, HelperMarshal._funcActionBufferResultLengthValue);
            Assert.Equal($"[object {creator}]", objectPrototype.Call(HelperMarshal._funcActionBufferObjectResultValue));
        }
    }
}
