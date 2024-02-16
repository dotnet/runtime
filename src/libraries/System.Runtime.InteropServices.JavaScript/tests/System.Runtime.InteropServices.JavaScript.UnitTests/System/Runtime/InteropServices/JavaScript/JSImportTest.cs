// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading;
using Xunit;
using System.Diagnostics.CodeAnalysis;
#pragma warning disable xUnit1026 // Theory methods should use all of their parameters

namespace System.Runtime.InteropServices.JavaScript.Tests
{
    public class JSImportTest : JSInteropTestBase, IAsyncLifetime
    {
        [Fact]
        public unsafe void StructSize()
        {
            Assert.Equal(32, sizeof(JSMarshalerArgument));
        }

        [Fact]
        public async Task MultipleImportAsync()
        {
            var first = await JSHost.ImportAsync("JavaScriptTestHelper", "../JavaScriptTestHelper.mjs");
            var second = await JSHost.ImportAsync("JavaScriptTestHelper", "../JavaScriptTestHelper.mjs");
            Assert.NotNull(first);
            Assert.NotNull(second);
            Assert.Equal("object", first.GetTypeOfProperty("instance"));
            var instance1 = first.GetPropertyAsJSObject("instance");
            var instance2 = second.GetPropertyAsJSObject("instance");
            Assert.Same(instance1, instance2);
            first.Dispose();
            second.Dispose();
            instance1.Dispose();
        }

#if !FEATURE_WASM_MANAGED_THREADS // because in MT JSHost.ImportAsync is really async, it will finish before the caller could cancel it
        [Fact]
        public async Task CancelableImportAsync()
        {
            var cts = new CancellationTokenSource();
            var exTask = Assert.ThrowsAsync<JSException>(async () => await JSHost.ImportAsync("JavaScriptTestHelper", "../JavaScriptTestHelper.mjs", cts.Token));
            cts.Cancel();
            var actualEx2 = await exTask;
            Assert.Equal("Error: OperationCanceledException", actualEx2.Message);

            var actualEx = await Assert.ThrowsAsync<JSException>(async () => await JSHost.ImportAsync("JavaScriptTestHelper", "../JavaScriptTestHelper.mjs", new CancellationToken(true)));
            Assert.Equal("Error: OperationCanceledException", actualEx.Message);
        }
#endif

        [Fact]
        public unsafe void GlobalThis()
        {
            var globalThis = JSHost.GlobalThis;
            Assert.Null(globalThis.GetPropertyAsString("dummy"));
            Assert.False(globalThis.HasProperty("dummy"));
            Assert.Equal("undefined", globalThis.GetTypeOfProperty("dummy"));
            Assert.Equal("function", globalThis.GetTypeOfProperty("Array"));
            var javaScriptTestHelper = globalThis.GetPropertyAsJSObject("javaScriptTestHelper");
            Assert.NotNull(javaScriptTestHelper);
            globalThis.Dispose();
            javaScriptTestHelper.Dispose();
        }

        [Fact]
        public unsafe void DotnetInstance()
        {
            JSHost.DotnetInstance.SetProperty("testBool", true);
            Assert.Equal("boolean", JSHost.DotnetInstance.GetTypeOfProperty("testBool"));

            JSHost.DotnetInstance.SetProperty("testInt", 42);
            Assert.Equal("number", JSHost.DotnetInstance.GetTypeOfProperty("testInt"));
            Assert.Equal(42, JSHost.DotnetInstance.GetPropertyAsInt32("testInt"));

            JSHost.DotnetInstance.SetProperty("testDouble", 3.14);
            Assert.Equal("number", JSHost.DotnetInstance.GetTypeOfProperty("testDouble"));
            Assert.Equal(3.14, JSHost.DotnetInstance.GetPropertyAsDouble("testDouble"));

            JSHost.DotnetInstance.SetProperty("testString", "Yoda");
            Assert.Equal("string", JSHost.DotnetInstance.GetTypeOfProperty("testString"));
            Assert.Equal("Yoda", JSHost.DotnetInstance.GetPropertyAsString("testString"));
        }

        [Fact]
        public unsafe void BadCast()
        {
            JSException ex;
            JSHost.DotnetInstance.SetProperty("testBool", true);
            ex = Assert.Throws<JSException>(() => JSHost.DotnetInstance.GetPropertyAsInt32("testBool"));
            Assert.Contains("Value is not an integer", ex.Message);
            ex = Assert.Throws<JSException>(() => JSHost.DotnetInstance.GetPropertyAsDouble("testBool"));
            Assert.Contains("Value is not a Number", ex.Message);
            ex = Assert.Throws<JSException>(() => JSHost.DotnetInstance.GetPropertyAsString("testBool"));
            Assert.Contains("Value is not a String", ex.Message);
            ex = Assert.Throws<JSException>(() => JSHost.DotnetInstance.GetPropertyAsJSObject("testBool"));
            Assert.Contains("JSObject proxy of boolean is not supported", ex.Message);
            ex = Assert.Throws<JSException>(() => JSHost.DotnetInstance.GetPropertyAsByteArray("testBool"));
            Assert.Contains("Value is not an Array or Uint8Array", ex.Message);
            JSHost.DotnetInstance.SetProperty("testInt", 42);
            ex = Assert.Throws<JSException>(() => JSHost.DotnetInstance.GetPropertyAsBoolean("testInt"));
            Assert.Contains("Value is not a Boolean", ex.Message);
        }

        [Fact]
        public unsafe void OutOfRange()
        {
            JSException ex;
            JSHost.DotnetInstance.SetProperty("testDouble", 9007199254740991L);
            ex = Assert.Throws<JSException>(() => JSHost.DotnetInstance.GetPropertyAsInt32("testDouble"));
            Assert.Contains("Overflow: value 9007199254740991 is out of -2147483648 2147483647 range", ex.Message);
        }

        [Fact] //TODO [Fact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWasmThreadingSupported))] // this test doesn't make sense with deputy
        public unsafe void OptimizedPaths()
        {
            JavaScriptTestHelper.optimizedReached = 0;
            JavaScriptTestHelper.invoke0V();
            Assert.Equal(1, JavaScriptTestHelper.optimizedReached);
            JavaScriptTestHelper.invoke1V(42);
            Assert.Equal(43, JavaScriptTestHelper.optimizedReached);
            Assert.Equal(124, JavaScriptTestHelper.invoke1R(123));
            Assert.Equal(43 + 123, JavaScriptTestHelper.optimizedReached);
            Assert.Equal(32, JavaScriptTestHelper.invoke2R(15, 16));
            Assert.Equal(43 + 123 + 31, JavaScriptTestHelper.optimizedReached);
        }


        #region Get/Set Property

        [Fact]
        public unsafe void JSObjectGetSet()
        {
            Func<double, JSObject> createObject = Utils.CreateFunctionDoubleJSObject("a", @"
                var x = {a, x:42 };
                return x;
                ");
            JSObject obj = createObject(1);
            Assert.NotNull(obj);
            double? a = obj.GetPropertyAsDouble("a");
            Assert.Equal(1, a);

            double? x = obj.GetPropertyAsDouble("x");
            Assert.Equal(42, x);

            int? xi = obj.GetPropertyAsInt32("x");
            Assert.Equal(42, xi);

            /*
            obj.GetProperty("x", out string? xs);
            Assert.Equal("42", xs);
            */

            obj.SetProperty("b", 3);
            double? b = obj.GetPropertyAsDouble("b");
            Assert.Equal(3, b);

            obj.SetProperty("c", "test");
            string? c = obj.GetPropertyAsString("c");
            Assert.Equal("test", c);

            obj.SetProperty("c", (string)null);
            string? d = obj.GetPropertyAsString("c");
            Assert.Null(d);
        }

        #endregion

        #region CreateFunction

        [Fact]
        public unsafe void CreateFunctionDouble()
        {
            Func<double, double, double> doublePlus = Utils.CreateFunctionDoubleDoubleDouble("a", "b", "return a+b");
            Assert.Equal(3, doublePlus(1, 2));
            Assert.Equal(Math.PI * 2, doublePlus(Math.PI, Math.PI));
        }

        [Fact]
        public unsafe void CreateFunctionDoubleThrow()
        {
            Func<double, double, double> doubleThrows = Utils.CreateFunctionDoubleDoubleDouble("a", "b", "throw Error('test '+a+' '+b);");
            var ex = Assert.Throws<JSException>(() => doubleThrows(1, 2));
            Assert.Equal("Error: test 1 2", ex.Message);

#if !FEATURE_WASM_MANAGED_THREADS
            Assert.Contains("create_function", ex.StackTrace);
#else
            Assert.Contains("omitted JavaScript stack trace", ex.StackTrace);
#endif
        }

        [Fact]
        public unsafe void CreateFunctionString()
        {
            Func<string, string, string> stringPlus = Utils.CreateFunctionStringStringString("a", "b", "return a+b");
            Assert.Equal("hello world", stringPlus("hello ", "world"));
            Assert.Equal("hellonull", stringPlus("hello", null));
        }

        [Fact]
        public unsafe void CreateFunctionInternal()
        {
            Func<bool> internals = Utils.CreateFunctionBool("return INTERNAL.mono_wasm_runtime_is_ready");
            Assert.True(internals());
        }

        #endregion

        #region Arrays

        [Theory]
        [MemberData(nameof(MarshalByteArrayCases))]
        public unsafe void JsImportByteArray(byte[]? expected)
        {
            var actual = JavaScriptTestHelper.echo1_ByteArray(expected);
            Assert.Equal(expected, actual);
            if (expected != null) for (int i = 0; i < expected.Length; i++)
                {
                    var actualI = JavaScriptTestHelper.store_ByteArray(expected, i);
                    Assert.Equal(expected[i], actualI);
                }
        }

        [Theory]
        [MemberData(nameof(MarshalIntArrayCases))]
        public unsafe void JsImportIntArray(int[]? expected)
        {
            var actual = JavaScriptTestHelper.echo1_Int32Array(expected);
            Assert.Equal(expected, actual);
            if (expected != null) for (int i = 0; i < expected.Length; i++)
                {
                    var actualI = JavaScriptTestHelper.store_Int32Array(expected, i);
                    Assert.Equal(expected[i], actualI);
                }
        }

        [Theory]
        [MemberData(nameof(MarshalDoubleArrayCases))]
        public unsafe void JsImportDoubleArray(double[]? expected)
        {
            var actual = JavaScriptTestHelper.echo1_DoubleArray(expected);
            Assert.Equal(expected, actual);
            if (expected != null) for (int i = 0; i < expected.Length; i++)
                {
                    var actualI = JavaScriptTestHelper.store_DoubleArray(expected, i);
                    Assert.Equal(expected[i], actualI);
                }
        }

        [Theory]
        [MemberData(nameof(MarshalStringArrayCases))]
        public unsafe void JsImportStringArray(string[]? expected)
        {
            var actual = JavaScriptTestHelper.echo1_StringArray(expected);
            Assert.Equal(expected, actual);

            if (expected != null) for (int i = 0; i < expected.Length; i++)
                {
                    var actualI = JavaScriptTestHelper.store_StringArray(expected, i);
                    Assert.Equal(expected[i], actualI);
                }
        }

        [Theory]
        [MemberData(nameof(MarshalObjectArrayCases))]
        public unsafe void JsImportObjectArray(object[]? expected)
        {
            if (expected?.Length == 1 && expected[0] is string s && s == "JSData")
            {
                expected = new object[] { new object[] { JavaScriptTestHelper.createData("test"), JavaScriptTestHelper.createException("test") } };
            }
            var actual = JavaScriptTestHelper.echo1_ObjectArray(expected);
            Assert.Equal(expected, actual);

            if (expected != null) for (int i = 0; i < expected.Length; i++)
                {
                    var actualI = JavaScriptTestHelper.store_ObjectArray(expected, i);
                    Assert.Equal(expected[i], actualI);
                }
        }

        [Theory]
        [MemberData(nameof(MarshalObjectArrayCasesToDouble))]
        public unsafe void JsImportObjectArrayToDouble(object[]? expected)
        {
            if (expected != null) for (int i = 0; i < expected.Length; i++)
                {
                    var actualI = JavaScriptTestHelper.store_ObjectArray(expected, i);
                    if (expected[i].GetType() == typeof(char))
                    {
                        Assert.Equal((double)(int)(char)expected[i], actualI);
                    }
                    else
                    {
                        Assert.Equal(Convert.ToDouble(expected[i]), actualI);
                    }
                }
        }

        [Theory]
        [MemberData(nameof(MarshalObjectArrayCasesThrow))]
        public void JsImportObjectArrayThrows(object[]? expected)
        {
            Assert.Throws<NotSupportedException>(() => JavaScriptTestHelper.echo1_ObjectArray(expected));
        }

        [Fact]
        public async Task JsImportObjectArrayTask()
        {
            object[] expected = new object[] { Task.CompletedTask };
            var actual = JavaScriptTestHelper.echo1_ObjectArray(expected);
            Assert.True(typeof(Task).IsAssignableFrom(actual[0].GetType()));
            await Task.Delay(100);
            await Task.Yield();
            var actual0 = actual[0] as Task;
            Assert.True(actual0.IsCompleted);
            Assert.True(actual0.IsCompletedSuccessfully);
            var actualT = JavaScriptTestHelper.store_ObjectArray(expected, 0);
            await Task.Delay(100);
            await Task.Yield();
            var actualT0 = actualT as Task;
            Assert.True(actualT0.IsCompletedSuccessfully);
        }

        [Fact]
        public async Task JsImportObjectArrayTaskObject()
        {
            object[] expected = new object[] { Task.FromResult((object)42) };
            var actual = JavaScriptTestHelper.echo1_ObjectArray(expected);
            var actual0 = Assert.IsType<Task<object>>(actual[0]);
            await Task.Delay(100);
            await Task.Yield();
            Assert.True(actual0.IsCompleted);
            Assert.True(actual0.IsCompletedSuccessfully);
            Assert.Equal(42.0d, actual0.Result);
        }

        [Fact]
        public async Task JsImportObjectArrayTaskObjectFail()
        {
            var exex = new Exception("test");
            object[] expected = new object[] { Task.FromException(exex) };
            var actual = JavaScriptTestHelper.echo1_ObjectArray(expected);
            var actual0 = Assert.IsType<Task<object>>(actual[0]);
            await Task.Delay(100);
            await Task.Yield();
            Assert.True(actual0.IsCompleted);
            Assert.True(actual0.IsFaulted);
            var actualEx = await Assert.ThrowsAsync<Exception>(async () => await actual0);
            Assert.Same(exex, actualEx);
        }

        #endregion

        #region Views

        [Fact]
        public unsafe void JsImportSpanOfByte()
        {
            var expectedBytes = stackalloc byte[] { 1, 2, 42, 0, 127, 255 };
            Span<byte> expected = new Span<byte>(expectedBytes, 6);
            Assert.True(Unsafe.AsPointer(ref expected.GetPinnableReference()) == expectedBytes);
            Span<byte> actual = JavaScriptTestHelper.echo1_SpanOfByte(expected, false);
            Assert.Equal(expected.Length, actual.Length);
            Assert.NotEqual(expected[0], expected[1]);
            Assert.Equal(expected.GetPinnableReference(), actual.GetPinnableReference());
            Assert.True(actual.SequenceCompareTo(expected) == 0);
            Assert.Equal(expected.ToArray(), actual.ToArray());
            actual = JavaScriptTestHelper.echo1_SpanOfByte(expected, true);
            Assert.Equal(expected[0], expected[1]);
            Assert.Equal(actual[0], actual[1]);
        }

        [Fact]
        public unsafe void JsImportSpanOfInt32()
        {
            var expectedBytes = stackalloc int[] { 0, 1, -2, 42, int.MaxValue, int.MinValue };
            Span<int> expected = new Span<int>(expectedBytes, 6);
            Assert.True(Unsafe.AsPointer(ref expected.GetPinnableReference()) == expectedBytes);
            Span<int> actual = JavaScriptTestHelper.echo1_SpanOfInt32(expected, false);
            Assert.Equal(expected.Length, actual.Length);
            Assert.NotEqual(expected[0], expected[1]);
            Assert.Equal(expected.GetPinnableReference(), actual.GetPinnableReference());
            Assert.True(actual.SequenceCompareTo(expected) == 0);
            Assert.Equal(expected.ToArray(), actual.ToArray());
            actual = JavaScriptTestHelper.echo1_SpanOfInt32(expected, true);
            Assert.Equal(expected[0], expected[1]);
            Assert.Equal(actual[0], actual[1]);
        }

        [Fact]
        public unsafe void JsImportSpanOfDouble()
        {
            var expectedBytes = stackalloc double[] { 0, 1, -1, double.Pi, 42, double.MaxValue, double.MinValue, double.NaN, double.PositiveInfinity, double.NegativeInfinity };
            Span<double> expected = new Span<double>(expectedBytes, 10);
            Assert.True(Unsafe.AsPointer(ref expected.GetPinnableReference()) == expectedBytes);
            Span<double> actual = JavaScriptTestHelper.echo1_SpanOfDouble(expected, false);
            Assert.Equal(expected.Length, actual.Length);
            Assert.NotEqual(expected[0], expected[1]);
            Assert.Equal(expected.GetPinnableReference(), actual.GetPinnableReference());
            Assert.True(actual.SequenceCompareTo(expected) == 0);
            Assert.Equal(expected.ToArray(), actual.ToArray());
            actual = JavaScriptTestHelper.echo1_SpanOfDouble(expected, true);
            Assert.Equal(expected[0], expected[1]);
            Assert.Equal(actual[0], actual[1]);
        }

        [Fact]
        public unsafe void JsImportArraySegmentOfByte()
        {
            var expectedBytes = new byte[] { 88, 1, 2, 42, 0, 127, 255 };
            ArraySegment<byte> expected = new ArraySegment<byte>(expectedBytes, 1, 6);
            ArraySegment<byte> actual = JavaScriptTestHelper.echo1_ArraySegmentOfByte(expected, false);
            Assert.Equal(expected.Count, actual.Count);
            Assert.NotEqual(expected[0], expected[1]);
            Assert.Equal(expected.Array, actual.Array);
            actual = JavaScriptTestHelper.echo1_ArraySegmentOfByte(expected, true);
            Assert.Equal(expected[0], expected[1]);
            Assert.Equal(actual[0], actual[1]);
        }

        [Fact]
        public unsafe void JsImportArraySegmentOfInt32()
        {
            var expectedBytes = new int[] { 88, 0, 1, -2, 42, int.MaxValue, int.MinValue };
            ArraySegment<int> expected = new ArraySegment<int>(expectedBytes, 1, 6);
            ArraySegment<int> actual = JavaScriptTestHelper.echo1_ArraySegmentOfInt32(expected, false);
            Assert.Equal(expected.Count, actual.Count);
            Assert.NotEqual(expected[0], expected[1]);
            Assert.Equal(expected.Array, actual.Array);
            actual = JavaScriptTestHelper.echo1_ArraySegmentOfInt32(expected, true);
            Assert.Equal(expected[0], expected[1]);
            Assert.Equal(actual[0], actual[1]);
        }

        [Fact]
        public unsafe void JsImportArraySegmentOfDouble()
        {
            var expectedBytes = new double[] { 88.88, 0, 1, -1, double.Pi, 42, double.MaxValue, double.MinValue, double.NaN, double.PositiveInfinity, double.NegativeInfinity };
            ArraySegment<double> expected = new ArraySegment<double>(expectedBytes, 1, 10);
            ArraySegment<double> actual = JavaScriptTestHelper.echo1_ArraySegmentOfDouble(expected, false);
            Assert.Equal(expected.Count, actual.Count);
            Assert.NotEqual(expected[0], expected[1]);
            Assert.Equal(expected.Array, actual.Array);
            actual = JavaScriptTestHelper.echo1_ArraySegmentOfDouble(expected, true);
            Assert.Equal(expected[0], expected[1]);
            Assert.Equal(actual[0], actual[1]);
        }

        #endregion

        #region Boolean

        [Theory]
        [MemberData(nameof(MarshalBooleanCases))]
        public void JsImportBoolean(bool value)
        {
            JsImportTest(value,
                JavaScriptTestHelper.store1_Boolean,
                JavaScriptTestHelper.retrieve1_Boolean,
                JavaScriptTestHelper.echo1_Boolean,
                JavaScriptTestHelper.throw1_Boolean,
                JavaScriptTestHelper.identity1_Boolean,
                "boolean");
        }

        #endregion Boolean

        #region Char
        [Theory]
        [MemberData(nameof(MarshalCharCases))]
        public void JsImportChar(char value)
        {
            JsImportTest(value,
                JavaScriptTestHelper.store1_Char,
                JavaScriptTestHelper.retrieve1_Char,
                JavaScriptTestHelper.echo1_Char,
                JavaScriptTestHelper.throw1_Char,
                JavaScriptTestHelper.identity1_Char,
                "number");
        }

        #endregion Char

        #region Byte
        [Theory]
        [MemberData(nameof(MarshalByteCases))]
        public void JsImportByte(byte value)
        {
            JsImportTest(value,
                JavaScriptTestHelper.store1_Byte,
                JavaScriptTestHelper.retrieve1_Byte,
                JavaScriptTestHelper.echo1_Byte,
                JavaScriptTestHelper.throw1_Byte,
                JavaScriptTestHelper.identity1_Byte,
                "number");
        }

        [Theory]
        [MemberData(nameof(OutOfRangeCases))]
        public void ByteOutOfRange(double value, string message)
        {
            JavaScriptTestHelper.store1_Double(value);
            var ex = Assert.Throws<JSException>(() => JavaScriptTestHelper.retrieve1_Byte());
            Assert.Contains(message, ex.Message);
        }

        #endregion Byte

        #region Int16
        [Theory]
        [MemberData(nameof(MarshalInt16Cases))]
        public void JsImportInt16(short value)
        {
            JsImportTest(value,
                JavaScriptTestHelper.store1_Int16,
                JavaScriptTestHelper.retrieve1_Int16,
                JavaScriptTestHelper.echo1_Int16,
                JavaScriptTestHelper.throw1_Int16,
                JavaScriptTestHelper.identity1_Int16,
                "number");
        }

        #endregion Int16

        #region Int32
        [Theory]
        [MemberData(nameof(MarshalInt32Cases))]
        public void JsImportInt32(int value)
        {
            JsImportTest(value,
                JavaScriptTestHelper.store1_Int32,
                JavaScriptTestHelper.retrieve1_Int32,
                JavaScriptTestHelper.echo1_Int32,
                JavaScriptTestHelper.throw1_Int32,
                JavaScriptTestHelper.identity1_Int32,
                "number");
        }

        [Theory]
        [MemberData(nameof(OutOfRangeCases))]
        public void Int32OutOfRange(double value, string message)
        {
            JavaScriptTestHelper.store1_Double(value);
            var ex = Assert.Throws<JSException>(() => JavaScriptTestHelper.retrieve1_Int32());
            Assert.Contains(message, ex.Message);
        }

        #endregion Int32

        #region Int52

        [Theory]
        [MemberData(nameof(MarshalInt52Cases))]
        public void JsImportInt52(long value)
        {
            JsImportTest(value,
                JavaScriptTestHelper.store1_Int52,
                JavaScriptTestHelper.retrieve1_Int52,
                JavaScriptTestHelper.echo1_Int52,
                JavaScriptTestHelper.throw1_Int52,
                JavaScriptTestHelper.identity1_Int52,
                "number");
        }

        #endregion Int52

        #region BigInt64
        [Theory]
        [MemberData(nameof(MarshalBigInt64Cases))]
        public void JsImportBigInt64(long value)
        {
            JsImportTest(value,
                JavaScriptTestHelper.store1_BigInt64,
                JavaScriptTestHelper.retrieve1_BigInt64,
                JavaScriptTestHelper.echo1_BigInt64,
                JavaScriptTestHelper.throw1_BigInt64,
                JavaScriptTestHelper.identity1_BigInt64,
                "bigint");
        }

        #endregion BigInt64

        #region Double
        [Theory]
        [MemberData(nameof(MarshalDoubleCases))]
        public void JsImportDouble(double value)
        {
            JsImportTest(value,
                JavaScriptTestHelper.store1_Double,
                JavaScriptTestHelper.retrieve1_Double,
                JavaScriptTestHelper.echo1_Double,
                JavaScriptTestHelper.throw1_Double,
                JavaScriptTestHelper.identity1_Double,
                "number");
        }

        #endregion Double

        #region Single
        [Theory]
        [MemberData(nameof(MarshalSingleCases))]
        public void JsImportSingle(float value)
        {
            JsImportTest(value,
                JavaScriptTestHelper.store1_Single,
                JavaScriptTestHelper.retrieve1_Single,
                JavaScriptTestHelper.echo1_Single,
                JavaScriptTestHelper.throw1_Single,
                JavaScriptTestHelper.identity1_Single,
                "number");
        }

        #endregion Single

        #region IntPtr
        [Theory]
        [MemberData(nameof(MarshalIntPtrCases))]
        public void JsImportIntPtr(IntPtr value)
        {
            JsImportTest(value,
                JavaScriptTestHelper.store1_IntPtr,
                JavaScriptTestHelper.retrieve1_IntPtr,
                JavaScriptTestHelper.echo1_IntPtr,
                JavaScriptTestHelper.throw1_IntPtr,
                JavaScriptTestHelper.identity1_IntPtr,
                "number");
        }

        #endregion IntPtr

        #region VoidPtr

        [Theory]
        [MemberData(nameof(MarshalIntPtrCases))]
        public unsafe void JsImportVoidPtr(IntPtr xvalue)
        {
            void* value = (void*)xvalue;

            JavaScriptTestHelper.store1_VoidPtr(value);
            void* res = JavaScriptTestHelper.retrieve1_VoidPtr();
            Assert.True(value == res);
            res = JavaScriptTestHelper.echo1_VoidPtr(value);
            Assert.True(value == res);

            var actualJsType = JavaScriptTestHelper.getType1();
            Assert.Equal("number", actualJsType);
        }

        #endregion VoidPtr

        #region Datetime
        [Theory]
        [MemberData(nameof(MarshalDateTimeCases))]
        public void JSImportDateTime(DateTime value)
        {
            JsImportTest(value,
                JavaScriptTestHelper.store1_DateTime,
                JavaScriptTestHelper.retrieve1_DateTime,
                JavaScriptTestHelper.echo1_DateTime,
                JavaScriptTestHelper.throw1_DateTime,
                JavaScriptTestHelper.identity1_DateTime,
                "object", "Date");
        }

        #endregion Datetime

        #region DateTimeOffset
        [Theory]
        [MemberData(nameof(MarshalDateTimeOffsetCases))]
        public void JSImportDateTimeOffset(DateTimeOffset value)
        {
            JsImportTest(value,
                JavaScriptTestHelper.store1_DateTimeOffset,
                JavaScriptTestHelper.retrieve1_DateTimeOffset,
                JavaScriptTestHelper.echo1_DateTimeOffset,
                JavaScriptTestHelper.throw1_DateTimeOffset,
                JavaScriptTestHelper.identity1_DateTimeOffset,
                "object", "Date");
        }

        #endregion DateTimeOffset

        #region NullableBoolean
        [Theory]
        [MemberData(nameof(MarshalNullableBooleanCases))]
        public void JsImportNullableBoolean(bool? value)
        {
            JsImportTest(value,
                JavaScriptTestHelper.store1_NullableBoolean,
                JavaScriptTestHelper.retrieve1_NullableBoolean,
                JavaScriptTestHelper.echo1_NullableBoolean,
                JavaScriptTestHelper.throw1_NullableBoolean,
                JavaScriptTestHelper.identity1_NullableBoolean,
                "boolean");
        }

        #endregion NullableBoolean

        #region NullableInt32
        [Theory]
        [MemberData(nameof(MarshalNullableInt32Cases))]
        public void JsImportNullableInt32(int? value)
        {
            JsImportTest(value,
                JavaScriptTestHelper.store1_NullableInt32,
                JavaScriptTestHelper.retrieve1_NullableInt32,
                JavaScriptTestHelper.echo1_NullableInt32,
                JavaScriptTestHelper.throw1_NullableInt32,
                JavaScriptTestHelper.identity1_NullableInt32,
                "number");
        }

        #endregion NullableInt32

        #region NullableBigInt64
        [Theory]
        [MemberData(nameof(MarshalNullableBigInt64Cases))]
        public void JsImportNullableBigInt64(long? value)
        {
            JsImportTest(value,
                JavaScriptTestHelper.store1_NullableBigInt64,
                JavaScriptTestHelper.retrieve1_NullableBigInt64,
                JavaScriptTestHelper.echo1_NullableBigInt64,
                JavaScriptTestHelper.throw1_NullableBigInt64,
                JavaScriptTestHelper.identity1_NullableBigInt64,
                "bigint");
        }

        #endregion NullableBigInt64

        #region NullableIntPtr
        [Theory]
        [MemberData(nameof(MarshalNullableIntPtrCases))]
        public void JsImportNullableIntPtr(IntPtr? value)
        {
            JsImportTest(value,
                JavaScriptTestHelper.store1_NullableIntPtr,
                JavaScriptTestHelper.retrieve1_NullableIntPtr,
                JavaScriptTestHelper.echo1_NullableIntPtr,
                JavaScriptTestHelper.throw1_NullableIntPtr,
                JavaScriptTestHelper.identity1_NullableIntPtr,
                "number");
        }

        #endregion NullableIntPtr

        #region NullableDouble
        [Theory]
        [MemberData(nameof(MarshalNullableDoubleCases))]
        public void JsImportNullableDouble(double? value)
        {
            JsImportTest(value,
                JavaScriptTestHelper.store1_NullableDouble,
                JavaScriptTestHelper.retrieve1_NullableDouble,
                JavaScriptTestHelper.echo1_NullableDouble,
                JavaScriptTestHelper.throw1_NullableDouble,
                JavaScriptTestHelper.identity1_NullableDouble,
                "number");
        }

        #endregion NullableDouble

        #region NullableDateTime
        [Theory]
        [MemberData(nameof(MarshalNullableDateTimeCases))]
        public void JsImportNullableDateTime(DateTime? value)
        {
            JsImportTest(value,
                JavaScriptTestHelper.store1_NullableDateTime,
                JavaScriptTestHelper.retrieve1_NullableDateTime,
                JavaScriptTestHelper.echo1_NullableDateTime,
                JavaScriptTestHelper.throw1_NullableDateTime,
                JavaScriptTestHelper.identity1_NullableDateTime,
                "object");
        }

        #endregion NullableDateTime

        #region String
        [Theory]
        [MemberData(nameof(MarshalStringCases))]
        public void JsImportString(string value)
        {
            JsImportTest(value,
                JavaScriptTestHelper.store1_String,
                JavaScriptTestHelper.retrieve1_String,
                JavaScriptTestHelper.echo1_String,
                JavaScriptTestHelper.throw1_String,
                JavaScriptTestHelper.identity1_String
                , "string");
        }

        [Fact]
        public void JsImportNative()
        {
            if (JSHost.GlobalThis.HasProperty("window"))
            {
                var actual = JavaScriptTestHelper.NativeFunctionToString();
                Assert.StartsWith("http", actual);
            }
        }

        [Fact]
        public void JsImportInstanceMember()
        {
            var actual = JavaScriptTestHelper.MemberEcho("t-e-s-t");
            Assert.StartsWith("t-e-s-t-w-i-t-h-i-n-s-t-a-n-c-e", actual);
        }

        [Fact]
        public void JsImportReboundInstanceMember()
        {
            var actual = JavaScriptTestHelper.ReboundMemberEcho("t-e-s-t");
            Assert.StartsWith("t-e-s-t-w-i-t-h-i-n-s-t-a-n-c-e", actual);
        }

        #endregion String

        #region Object
        [Theory]
        [MemberData(nameof(MarshalObjectCases))]
        public void JSImportObject(object value, string clazz)
        {
            JsImportTest(value,
                JavaScriptTestHelper.store1_Object,
                JavaScriptTestHelper.retrieve1_Object,
                JavaScriptTestHelper.echo1_Object,
                JavaScriptTestHelper.throw1_Object,
                JavaScriptTestHelper.identity1_Object,
                "object", clazz);
        }

        #endregion Object

        #region Exception
        [Theory]
        [MemberData(nameof(MarshalExceptionCases))]
        public void JSImportException(Exception value, string clazz)
        {
            if (clazz == "JSTestError")
            {
                value = JavaScriptTestHelper.createException("!CreateEx!");
            }

            JsImportTest(value,
                JavaScriptTestHelper.store1_Exception,
                JavaScriptTestHelper.retrieve1_Exception,
                JavaScriptTestHelper.echo1_Exception,
                JavaScriptTestHelper.throw1_Exception,
                JavaScriptTestHelper.identity1_Exception,
                "object", clazz);
        }

        [Fact]
        public void JSImportReturnError()
        {
            var err = JavaScriptTestHelper.returnError() as Exception;
            Assert.NotNull(err);
            Assert.Contains("this-is-error", err.Message);
        }

        #endregion Exception

        #region JSObject
        [Theory]
        [MemberData(nameof(MarshalIJSObjectCases))]
        public void JSImportIJSObject(JSObject value, string clazz)
        {
            if (clazz == "JSData")
            {
                value = JavaScriptTestHelper.createData("!CreateJS!");
            }

            JsImportTest(value,
                JavaScriptTestHelper.store1_JSObject,
                JavaScriptTestHelper.retrieve1_JSObject,
                JavaScriptTestHelper.echo1_JSObject,
                JavaScriptTestHelper.throw1_JSObject,
                JavaScriptTestHelper.identity1_JSObject,
                "object", clazz);
        }

        #endregion JSObject

        #region ProxyOfProxy
        [Fact]
        public void ProxyOfProxyThrows()
        {
            // proxy of proxy should throw
            JavaScriptTestHelper.store1_Object(new object());
            Assert.Throws<JSException>(() => JavaScriptTestHelper.retrieve1_JSObject());
        }


        [Fact]
        public void ProxyOfIntThrows()
        {
            // JSObject proxy of int should throw
            JavaScriptTestHelper.store1_Int32(13);
            Assert.Throws<JSException>(() => JavaScriptTestHelper.retrieve1_JSObject());
        }
        #endregion

        #region Task

        [Fact]
        public async Task JsImportSleep()
        {
            await JavaScriptTestHelper.sleep(100);
        }

        [Fact]
        public async Task JsImportTaskTypes()
        {
            for (int i = 0; i < 100; i++)
            {
                object a = new object();
                Exception e = new Exception();
                JSObject j = JSHost.GlobalThis;
                Assert.Equal("test", await JavaScriptTestHelper.echopromise_String("test"));
                Assert.Same(a, await JavaScriptTestHelper.echopromise_Object(a));
                Assert.Same(e, await JavaScriptTestHelper.echopromise_Exception(e));
                Assert.Same(j, await JavaScriptTestHelper.echopromise_JSObject(j));
                GC.Collect();
                await Task.Delay(10);
            }
        }

        [Fact]
        public async Task JsImportThenVoid()
        {
            TaskCompletionSource tcs = new TaskCompletionSource();
            JavaScriptTestHelper.thenvoid(tcs.Task);
            GC.Collect();
            tcs.SetResult();

            GC.Collect();

            await Task.Yield();
        }

        [Fact]
        [OuterLoop]
        public async Task JsImportForeverMany()
        {
            for (int i = 0; i < 1000; i++)
            {
                if (i % 100 == 0)
                {
                    GC.Collect();
                    await Task.Yield();
                }
                var forever = JavaScriptTestHelper.forever();
                Assert.False(forever.IsCompleted);
            }
        }

        [Fact]
        public async Task JsImportVoidTaskPending()
        {
            GC.Collect();
            var pending = Task.Delay(1000);
            var res = JavaScriptTestHelper.await2(pending);
            GC.Collect();
            Assert.False(res.IsCompleted);
            await Task.Yield();
            GC.Collect();
            await res;
            GC.Collect();
            Assert.True(res.IsCompleted);
            GC.Collect();
        }

        [Fact]
        public async Task JsImportVoidTaskComplete()
        {
            GC.Collect();
            var resComplete = JavaScriptTestHelper.await2(Task.CompletedTask);
            GC.Collect();
            await Task.Delay(100);
            GC.Collect();
            await Task.Yield();
            GC.Collect();
            Assert.True(resComplete.IsCompleted);
            GC.Collect();
            await resComplete;
            GC.Collect();
        }

        [Fact]
        public async Task JsImportSleep2()
        {
            int ms = await JavaScriptTestHelper.sleep_Int(100);
            Assert.Equal(100, ms);
        }


        [Fact]
        public async Task JsImportTaskEchoComplete()
        {
            var task = JavaScriptTestHelper.echo1_Task(Task.CompletedTask);
            Assert.NotEqual(Task.CompletedTask, task);
            // yield to main loop, because "the respective handler function will be called asynchronously"
            // https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Promise/then#return_value
            await Task.Delay(100);
            Assert.True(task.IsCompleted);
        }

        [Fact]
        public async Task JsImportTaskEchoPendingResult()
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            var task = JavaScriptTestHelper.echo1_Task(tcs.Task);
            Assert.NotEqual(tcs.Task, task);
            Assert.False(task.IsCompleted);

            tcs.SetResult("test");
            // yield to main loop, because "the respective handler function will be called asynchronously"
            await Task.Delay(100);
            Assert.True(task.IsCompleted);
            Assert.Equal(typeof(Task), task.GetType());
        }

        [Fact]
        public async Task JsImportTaskEchoPendingException()
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            var task = JavaScriptTestHelper.echo1_Task(tcs.Task);
            Assert.NotEqual(tcs.Task, task);
            Assert.False(task.IsCompleted);

            tcs.SetException(new Exception("Test"));
            // yield to main loop, because "the respective handler function will be called asynchronously"
            await Task.Delay(100);
            Assert.True(task.IsFaulted);
            await Assert.ThrowsAsync<Exception>(async () => await task);
        }

        [Theory]
        [MemberData(nameof(TaskCases))]
        public async Task JsImportTaskAwaitPendingResult(object result)
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            var task = JavaScriptTestHelper.await1(tcs.Task);
            Assert.NotEqual(tcs.Task, task);
            Assert.False(task.IsCompleted);

            tcs.SetResult(result);
            // yield to main loop, because "the respective handler function will be called asynchronously"
            await Task.Delay(100);
            Assert.True(task.IsCompleted);
            var res = await task;
            if (result != null && result.GetType() == typeof(int))
            {
                Assert.Equal(result, Convert.ToInt32(res));
            }
            else
            {
                Assert.Equal(result, res);
            }
        }

        [Fact]
        public async Task JsImportTaskAwaitPendingException()
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            var task = JavaScriptTestHelper.await1(tcs.Task);
            Assert.NotEqual(tcs.Task, task);
            Assert.False(task.IsCompleted);

            tcs.SetException(new Exception("Test"));
            // yield to main loop, because "the respective handler function will be called asynchronously"
            await Task.Delay(100);
            Assert.True(task.IsFaulted);
            await Assert.ThrowsAsync<Exception>(async () => await task);
        }

        [Fact]
        public async Task JsImportTaskAwaitPendingExceptionValue()
        {
            TaskCompletionSource<Exception> tcs = new TaskCompletionSource<Exception>();
            var task = JavaScriptTestHelper.await1_TaskOfException(tcs.Task);
            Assert.NotEqual(tcs.Task, task);
            Assert.False(task.IsCompleted);

            tcs.SetResult(new Exception("Test"));
            // yield to main loop, because "the respective handler function will be called asynchronously"
            await Task.Delay(100);
            Assert.True(task.IsCompletedSuccessfully);
            Assert.Equal("Test", task.Result.Message);
        }


        [Fact]
        public async Task JsImportTaskAwait()
        {
            var task = JavaScriptTestHelper.awaitvoid(Task.CompletedTask);
            await Task.Delay(100);
            Assert.True(task.IsCompleted);
            await task;
        }

        #endregion

        #region Action

        [Fact] //TODO [Fact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWasmThreadingSupported))] // this test doesn't make sense with deputy
        public void JsImportCallback_EchoAction()
        {
            bool called = false;
            Action expected = () =>
            {
                called = true;
            };
            var actual = JavaScriptTestHelper.echo1_ActionAction(expected);
            Assert.NotEqual(expected, actual);
            Assert.False(called);
            actual();
            Assert.True(called);
        }

        /* TODO deputy
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsWasmThreadingSupported))]
        public void JsImportCallback_EchoActionThrows_MT()
        {
            bool called = false;
            Action expected = () =>
            {
                called = true;
            };
            var actual = JavaScriptTestHelper.echo1_ActionAction(expected);
            Assert.NotEqual(expected, actual);
            Assert.False(called);
            // with deputy thread, call back to C# from synchronous JS function is not allowed
            Assert.Throws<JSException>(()=>actual());
            Assert.False(called);
        }
        */

        [Fact]
        public async Task JsImportCallback_Async()
        {
            bool called = false;
            var promise = JavaScriptTestHelper.backback_FuncIntIntFuncIntIntAsync((a,b) =>
            {
                called = true;
                return a + b;
            }, 123, 321);
            Assert.False(called);
            var actual = await promise;
            Assert.True(called);
            Assert.Equal(444, actual);
        }


        [Fact] //TODO [Fact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWasmThreadingSupported))] // this test doesn't make sense with deputy
        [OuterLoop]
        public async Task JsImportCallback_EchoActionMany()
        {
            int a = 1;
            for (int i = 0; i < 1000; i++)
            {
                Action expected = () =>
                {
                    a += i;
                };
                var actual = JavaScriptTestHelper.echo1large_ActionAction(expected);
                Assert.NotEqual(expected, actual);
                if (i % 100 == 0)
                {
                    await Task.Yield();
                    GC.Collect();
                }
            }
        }

        [Fact] //TODO [Fact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWasmThreadingSupported))] // this test doesn't make sense with deputy
        public void JsImportCallback_Action()
        {
            bool called = false;
            JavaScriptTestHelper.back3_Action(() =>
            {
                called = true;
            });
            Assert.True(called);
        }

        [Fact] //TODO [Fact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWasmThreadingSupported))] // this test doesn't make sense with deputy
        public void JsImportEcho_ActionAction()
        {
            bool called = false;
            Action res = JavaScriptTestHelper.echo1_ActionAction(() =>
            {
                called = true;
            });
            Assert.False(called);
            res.Invoke();
            Assert.True(called);
        }

        [Fact] //TODO [Fact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWasmThreadingSupported))] // this test doesn't make sense with deputy
        public void JsImportEcho_ActionIntActionInt()
        {
            int calledA = -1;
            Action<int> res = JavaScriptTestHelper.echo1_ActionIntActionInt((a) =>
            {
                calledA = a;
            });
            Assert.Equal(-1, calledA);
            res.Invoke(42);
            Assert.Equal(42, calledA);
        }

        [Fact] //TODO [Fact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWasmThreadingSupported))] // this test doesn't make sense with deputy
        public void JsImportCallback_ActionInt()
        {
            int called = -1;
            JavaScriptTestHelper.back3_ActionInt((a) =>
            {
                called = a;
            }, 42);
            Assert.Equal(42, called);
        }

        [Fact] //TODO [Fact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWasmThreadingSupported))] // this test doesn't make sense with deputy
        public void JsImportCallback_FunctionIntInt()
        {
            int called = -1;
            int res = JavaScriptTestHelper.back3_FunctionIntInt((a) =>
            {
                called = a;
                return a;
            }, 42);
            Assert.Equal(42, called);
            Assert.Equal(42, res);
        }

        [Fact] //TODO [Fact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWasmThreadingSupported))] // this test doesn't make sense with deputy
        public void JsImportBackCallback_FunctionIntInt()
        {
            int called = -1;
            Func<int, int> res = JavaScriptTestHelper.backback_FuncIntFuncInt((a) =>
            {
                called = a;
                return a;
            }, 42);
            Assert.Equal(-1, called);
            int actual = res.Invoke(42);
            Assert.Equal(84, actual);
            Assert.Equal(84, called);
        }

        [Fact] //TODO [Fact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWasmThreadingSupported))] // this test doesn't make sense with deputy
        public void JsImportBackCallback_FunctionIntIntIntInt()
        {
            int calledA = -1;
            int calledB = -1;
            Func<int, int, int> res = JavaScriptTestHelper.backback_FuncIntIntFuncIntInt((a, b) =>
            {
                calledA = a;
                calledB = b;
                return a + b;
            }, 42, 43);
            Assert.Equal(-1, calledA);
            Assert.Equal(-1, calledB);
            int actual = res.Invoke(40, 41);
            Assert.Equal(166, actual);
            Assert.Equal(82, calledA);
            Assert.Equal(84, calledB);
        }

        [Fact] //TODO [Fact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWasmThreadingSupported))] // this test doesn't make sense with deputy
        public void JsImportCallback_ActionIntInt()
        {
            int calledA = -1;
            int calledB = -1;
            JavaScriptTestHelper.back3_ActionIntInt((a, b) =>
            {
                calledA = a;
                calledB = b;
            }, 42, 43);
            Assert.Equal(42, calledA);
            Assert.Equal(43, calledB);
        }

        [Fact] //TODO [Fact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWasmThreadingSupported))] // this test doesn't make sense with deputy
        public void JsImportCallback_ActionLongLong()
        {
            long calledA = -1;
            long calledB = -1;
            JavaScriptTestHelper.back3_ActionLongLong((a, b) =>
            {
                calledA = a;
                calledB = b;
            }, 42, 43);
            Assert.Equal(42, calledA);
            Assert.Equal(43, calledB);
        }

        [Fact] //TODO [Fact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWasmThreadingSupported))] // this test doesn't make sense with deputy
        public void JsImportCallback_ActionIntLong()
        {
            int calledA = -1;
            long calledB = -1;
            JavaScriptTestHelper.back3_ActionIntLong((a, b) =>
            {
                calledA = a;
                calledB = b;
            }, 42, 43);
            Assert.Equal(42, calledA);
            Assert.Equal(43, calledB);
        }

        [Fact] //TODO [Fact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWasmThreadingSupported))] // this test doesn't make sense with deputy
        public void JsImportCallback_ActionIntThrow()
        {
            int called = -1;
            Exception expected = new Exception("test!!");
            Exception actual = Assert.Throws<Exception>(() => JavaScriptTestHelper.back3_ActionInt((a) =>
            {
                called = a;
                throw expected;
            }, 42));
            Assert.Equal(42, called);
            Assert.Same(expected, actual);
        }

        [Fact]
        public void JsImportMath()
        {
            Func<int, int, int> plus = Utils.CreateFunctionIntIntInt("a", "b", @"return a+b");
            Assert.Equal(3, plus(1, 2));
        }

        #endregion

        private void JsImportTest<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>(T value
            , Action<T> store1
            , Func<T> retrieve1
            , Func<T, T> echo1
            , Func<T, T> throw1
            , Func<T, bool> identity1
            , string jsType, string? jsClass = null)
        {
            if (value == null)
            {
                jsClass = null;
                jsType = "object";
            }

            // invoke 
            store1(value);
            var res = retrieve1();
            Assert.Equal(value, res);
            res = echo1(value);
            Assert.Equal(value, res);
            var equals = identity1(value);
            Assert.True(equals, "value not equals");

            var actualJsType = JavaScriptTestHelper.getType1();
            Assert.Equal(jsType, actualJsType);

            if (jsClass != null)
            {
                var actualJsClass = JavaScriptTestHelper.getClass1();
                Assert.Equal(jsClass, actualJsClass);
            }
            var exThrow0 = Assert.Throws<JSException>(() => JavaScriptTestHelper.throw0());
            Assert.Contains("throw-0-msg", exThrow0.Message);
            Assert.DoesNotContain(" at ", exThrow0.Message);
#if !FEATURE_WASM_MANAGED_THREADS
            Assert.Contains("throw0fn", exThrow0.StackTrace);
#else
            Assert.Contains("omitted JavaScript stack trace", exThrow0.StackTrace);
#endif

            var exThrow1 = Assert.Throws<JSException>(() => throw1(value));
            Assert.Contains("throw1-msg", exThrow1.Message);
            Assert.DoesNotContain(" at ", exThrow1.Message);
#if !FEATURE_WASM_MANAGED_THREADS
            Assert.Contains("throw1fn", exThrow1.StackTrace);
#else
            Assert.Contains("omitted JavaScript stack trace", exThrow0.StackTrace);
#endif

            // anything is a system.object, sometimes it would be JSObject wrapper
            if (typeof(T).IsPrimitive)
            {
                if (typeof(T) != typeof(long))
                {

                    object resBoxed = JavaScriptTestHelper.echo1_Object(value);
                    // js Number always boxes as double
                    if (typeof(T) == typeof(IntPtr))
                    {
                        //TODO Assert.Equal((IntPtr)(object)value, (IntPtr)(int)(double)resBoxed);
                    }
                    else if (typeof(T) == typeof(bool))
                    {
                        Assert.Equal((bool)(object)value, (bool)resBoxed);
                    }
                    else if (typeof(T) == typeof(char))
                    {
                        Assert.Equal((char)(object)value, (char)(double)resBoxed);
                    }
                    else
                    {
                        Assert.Equal(Convert.ToDouble(value), resBoxed);
                    }
                }

                //TODO var task = JavaScriptTestHelper.await1(Task.FromResult((object)value));
            }
            else if (typeof(T) == typeof(DateTime))
            {
                var resBoxed = JavaScriptTestHelper.echo1_Object(value);
                Assert.Equal(value, resBoxed);
            }
            else if (typeof(T) == typeof(DateTimeOffset))
            {
                var resBoxed = JavaScriptTestHelper.echo1_Object(value);
                Assert.Equal(((DateTimeOffset)(object)value).UtcDateTime, resBoxed);
            }
            else if (Nullable.GetUnderlyingType(typeof(T)) != null)
            {
                var vt = Nullable.GetUnderlyingType(typeof(T));
                if (vt != typeof(long))
                {
                    var resBoxed = JavaScriptTestHelper.echo1_Object(value);
                    if (resBoxed != null)
                    {
                        if (vt == typeof(bool))
                        {
                            Assert.Equal(((bool?)(object)value).Value, (bool)resBoxed);
                        }
                        else if (vt == typeof(char))
                        {
                            Assert.Equal(((char?)(object)value).Value, (char)resBoxed);
                        }
                        else if (vt == typeof(DateTime))
                        {
                            Assert.Equal(((DateTime?)(object)value).Value, resBoxed);
                        }
                        else if (vt == typeof(DateTimeOffset))
                        {
                            Assert.Equal(((DateTimeOffset?)(object)value).Value.UtcDateTime, resBoxed);
                        }
                        else if (vt == typeof(IntPtr))
                        {
                            // TODO Assert.Equal((double)((IntPtr?)(object)value).Value, resBoxed);
                        }
                        else
                        {
                            Assert.Equal(Convert.ToDouble(value), resBoxed);
                        }
                    }
                    else
                    {
                        Assert.Equal(value, default(T));
                    }
                }
            }
            else
            {
                var resObj = JavaScriptTestHelper.retrieve1_Object();
                if (resObj == null || resObj.GetType() != typeof(JSObject))
                {
                    Assert.Equal(value, resObj);
                }
            }

            if (typeof(Exception).IsAssignableFrom(typeof(T)))
            {
                // all exceptions are Exception
                var resEx = JavaScriptTestHelper.retrieve1_Exception();
                Assert.Equal((Exception)(object)value, resEx);
            }
        }
    }
}
