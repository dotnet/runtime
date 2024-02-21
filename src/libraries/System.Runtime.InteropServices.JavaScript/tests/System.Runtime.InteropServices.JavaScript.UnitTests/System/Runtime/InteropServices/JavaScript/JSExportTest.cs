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
    public class JSExportAsyncTest : JSInteropTestBase, IAsyncLifetime
    {
        [Theory]
        [MemberData(nameof(MarshalBooleanCases))]
        public async Task JsExportBooleanAsync(bool value)
        {
            await JsExportTestAsync(value,
                JavaScriptTestHelper.invoke1_BooleanAsync,
                nameof(JavaScriptTestHelper.EchoBoolean),
                "boolean");
        }

        private async Task JsExportTestAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>(T value
        , Func<T, string, Task<T>> invoke, string echoName, string jsType, string? jsClass = null)
        {
            T res;
            res = await invoke(value, echoName);
            Assert.Equal<T>(value, res);
        }
    }

    //TODO [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWasmThreadingSupported))] // this test doesn't make sense with deputy
    public class JSExportTest : JSInteropTestBase, IAsyncLifetime
    {
        [Theory]
        [MemberData(nameof(MarshalBooleanCases))]
        public void JsExportBoolean(bool value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_Boolean,
                nameof(JavaScriptTestHelper.EchoBoolean),
                "boolean");
        }

        [Theory]
        [MemberData(nameof(MarshalCharCases))]
        public void JsExportChar(char value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_Char,
                nameof(JavaScriptTestHelper.EchoChar),
                "number");
        }

        [Theory]
        [MemberData(nameof(MarshalByteCases))]
        public void JsExportByte(byte value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_Byte,
                nameof(JavaScriptTestHelper.EchoByte),
                "number");
        }

        [Theory]
        [MemberData(nameof(MarshalInt16Cases))]
        public void JsExportInt16(short value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_Int16,
                nameof(JavaScriptTestHelper.EchoInt16),
                "number");
        }

        [Theory]
        [MemberData(nameof(MarshalInt32Cases))]
        public void JsExportInt32(int value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_Int32,
                nameof(JavaScriptTestHelper.EchoInt32),
                "number");
        }

        [Theory]
        [MemberData(nameof(MarshalInt52Cases))]
        public void JsExportInt52(long value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_Int52,
                nameof(JavaScriptTestHelper.EchoInt52),
                "number");
        }

        [Theory]
        [MemberData(nameof(MarshalBigInt64Cases))]
        public void JsExportBigInt64(long value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_BigInt64,
                nameof(JavaScriptTestHelper.EchoBigInt64),
                "bigint");
        }

        [Theory]
        [MemberData(nameof(MarshalDoubleCases))]
        public void JsExportDouble(double value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_Double,
                nameof(JavaScriptTestHelper.EchoDouble),
                "number");
        }

        [Theory]
        [MemberData(nameof(MarshalSingleCases))]
        public void JsExportSingle(float value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_Single,
                nameof(JavaScriptTestHelper.EchoSingle),
                "number");
        }

        [Theory]
        [MemberData(nameof(MarshalIntPtrCases))]
        public void JsExportIntPtr(IntPtr value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_IntPtr,
                nameof(JavaScriptTestHelper.EchoIntPtr),
                "number");
        }

        [Theory]
        [MemberData(nameof(MarshalIntPtrCases))]
        public unsafe void JsExportVoidPtr(IntPtr xvalue)
        {
            void* value = (void*)xvalue;
            void* res = JavaScriptTestHelper.invoke1_VoidPtr(value, nameof(JavaScriptTestHelper.EchoVoidPtr));
            Assert.True(value == res);
        }

        [Theory]
        [MemberData(nameof(MarshalDateTimeCases))]
        public void JsExportDateTime(DateTime value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_DateTime,
                nameof(JavaScriptTestHelper.EchoDateTime),
                "object", "Date");
        }


        [Theory]
        [MemberData(nameof(MarshalDateTimeOffsetCases))]
        public void JsExportDateTimeOffset(DateTimeOffset value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_DateTimeOffset,
                nameof(JavaScriptTestHelper.EchoDateTimeOffset),
                "object", "Date");
        }

        [Theory]
        [MemberData(nameof(MarshalNullableBooleanCases))]
        public void JsExportNullableBoolean(bool? value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_NullableBoolean,
                nameof(JavaScriptTestHelper.EchoNullableBoolean),
                "boolean");
        }

        [Theory]
        [MemberData(nameof(MarshalNullableInt32Cases))]
        public void JsExportNullableInt32(int? value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_NullableInt32,
                nameof(JavaScriptTestHelper.EchoNullableInt32),
                "number");
        }

        [Theory]
        [MemberData(nameof(MarshalNullableBigInt64Cases))]
        public void JsExportNullableBigInt64(long? value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_NullableBigInt64,
                nameof(JavaScriptTestHelper.EchoNullableBigInt64),
                "bigint");
        }

        [Theory]
        [MemberData(nameof(MarshalNullableIntPtrCases))]
        public void JsExportNullableIntPtr(IntPtr? value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_NullableIntPtr,
                nameof(JavaScriptTestHelper.EchoNullableIntPtr),
                "number");
        }

        [Theory]
        [MemberData(nameof(MarshalNullableDoubleCases))]
        public void JsExportNullableDouble(double? value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_NullableDouble,
                nameof(JavaScriptTestHelper.EchoNullableDouble),
                "number");
        }

        [Theory]
        [MemberData(nameof(MarshalNullableDateTimeCases))]
        public void JsExportNullableDateTime(DateTime? value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_NullableDateTime,
                nameof(JavaScriptTestHelper.EchoNullableDateTime),
                "object");
        }

        [Theory]
        [MemberData(nameof(MarshalStringCases))]
        public void JsExportString(string value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_String,
                nameof(JavaScriptTestHelper.EchoString),
                "string");
        }

        [Fact]
        public void JsExportStringNoNs()
        {
            var actual = JavaScriptTestHelper.invoke2_String("test", nameof(JavaScriptTestHelperNoNamespace.EchoString));
            Assert.Equal("test51", actual);
        }

        [Fact]
        public void JsExportStructClassRecords()
        {
            var actual = JavaScriptTestHelper.invokeStructClassRecords("test");
            Assert.Equal(48, actual.Length);
            Assert.Equal("test11", actual[0]);
            Assert.Equal("test12", actual[1]);
            Assert.Equal("test13", actual[2]);
            Assert.Equal("test14", actual[3]);
            Assert.Equal("test15", actual[4]);
            Assert.Equal("test16", actual[5]);
            Assert.Equal("test17", actual[6]);
            Assert.Equal("test18", actual[7]);
            Assert.Equal("test19", actual[8]);
            Assert.Equal("test21", actual[9]);
            Assert.Equal("test22", actual[10]);
            Assert.Equal("test23", actual[11]);
            Assert.Equal("test24", actual[12]);
            Assert.Equal("test25", actual[13]);
            Assert.Equal("test31", actual[14]);
            Assert.Equal("test32", actual[15]);
            Assert.Equal("test33", actual[16]);
            Assert.Equal("test34", actual[17]);
            Assert.Equal("test35", actual[18]);
            Assert.Equal("test41", actual[19]);
            Assert.Equal("test42", actual[20]);
            Assert.Equal("test43", actual[21]);
            Assert.Equal("test44", actual[22]);
            Assert.Equal("test45", actual[23]);
            Assert.Equal("test51", actual[24]);
            Assert.Equal("test52", actual[25]);
            Assert.Equal("test53", actual[26]);
            Assert.Equal("test54", actual[27]);
            Assert.Equal("test55", actual[28]);
            Assert.Equal("test56", actual[29]);
            Assert.Equal("test57", actual[30]);
            Assert.Equal("test58", actual[31]);
            Assert.Equal("test59", actual[32]);
            Assert.Equal("test61", actual[33]);
            Assert.Equal("test62", actual[34]);
            Assert.Equal("test63", actual[35]);
            Assert.Equal("test64", actual[36]);
            Assert.Equal("test65", actual[37]);
            Assert.Equal("test71", actual[38]);
            Assert.Equal("test72", actual[39]);
            Assert.Equal("test73", actual[40]);
            Assert.Equal("test74", actual[41]);
            Assert.Equal("test75", actual[42]);
            Assert.Equal("test81", actual[43]);
            Assert.Equal("test82", actual[44]);
            Assert.Equal("test83", actual[45]);
            Assert.Equal("test84", actual[46]);
            Assert.Equal("test85", actual[47]);
        }

        [Theory]
        [MemberData(nameof(MarshalObjectCases))]
        public void JsExportObject(object value, string clazz)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_Object,
                nameof(JavaScriptTestHelper.EchoObject),
                "object", clazz);
        }

        [Theory]
        [MemberData(nameof(MarshalExceptionCases))]
        public void JsExportException(Exception value, string clazz)
        {
            if (clazz == "JSTestError")
            {
                value = JavaScriptTestHelper.createException("!CreateEx!");
            }

            JsExportTest(value,
                JavaScriptTestHelper.invoke1_Exception,
                nameof(JavaScriptTestHelper.EchoException),
                "object", clazz);
        }

        [Fact]
        public void JsExportCatchToString()
        {
            var toString = JavaScriptTestHelper.catch1toString("-t-e-s-t-", nameof(JavaScriptTestHelper.ThrowFromJSExport));
            Assert.DoesNotContain("Unexpected error", toString);
            Assert.Contains("-t-e-s-t-", toString);
            Assert.DoesNotContain(nameof(JavaScriptTestHelper.ThrowFromJSExport), toString);
        }

        [Fact]
        public void JsExportCatchStack()
        {
            var stack = JavaScriptTestHelper.catch1stack("-t-e-s-t-", nameof(JavaScriptTestHelper.ThrowFromJSExport));
            Assert.Contains(nameof(JavaScriptTestHelper.ThrowFromJSExport), stack);
            if (PlatformDetection.IsBrowserDomSupportedOrNodeJS)
            {
                Assert.Contains("catch1stack", stack);
            }
        }

        [Theory]
        [MemberData(nameof(MarshalIJSObjectCases))]
        public void JsExportIJSObject(JSObject value, string clazz)
        {
            if (clazz == "JSData")
            {
                value = JavaScriptTestHelper.createData("!CreateJS!");
            }

            JsExportTest(value,
                JavaScriptTestHelper.invoke1_JSObject,
                nameof(JavaScriptTestHelper.EchoIJSObject),
                "object", clazz);
        }

        [Theory]
        [MemberData(nameof(MarshalInt32Cases))]
        public async Task JsExportTaskOfInt(int value)
        {
            TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();

            var res = JavaScriptTestHelper.invoke1_TaskOfInt(tcs.Task, nameof(JavaScriptTestHelper.AwaitTaskOfObject));
            tcs.SetResult(value);
            await Task.Yield();
            var rr = await res;
            await Task.Yield();
            Assert.Equal(value, rr);
            //GC.Collect();
        }

        [Fact]
        public void JsExportCallback_FunctionIntInt()
        {
            int called = -1;
            var chain = JavaScriptTestHelper.invoke1_FuncOfIntInt((int a) =>
            {
                called = a;
                return a;
            }, nameof(JavaScriptTestHelper.BackFuncOfIntInt));

            Assert.Equal(-1, called);
            var actual = chain(42);
            Assert.Equal(42, actual);
            Assert.Equal(42, called);
        }

        [Fact]
        public void JsExportCallback_FunctionIntIntThrow()
        {
            int called = -1;
            var expected = new Exception("test!!");
            var chain = JavaScriptTestHelper.invoke1_FuncOfIntInt((int a) =>
            {
                called = a;
                throw expected;
            }, nameof(JavaScriptTestHelper.BackFuncOfIntInt));

            Assert.Equal(-1, called);
            var actual = Assert.Throws<Exception>(() => chain(42));
            Assert.Equal(42, called);
            Assert.Same(expected, actual);
        }

        private void JsExportTest<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>(T value
        , Func<T, string, T> invoke, string echoName, string jsType, string? jsClass = null)
        {
            T res;
            res = invoke(value, echoName);
            Assert.Equal<T>(value, res);
        }
    }
}
