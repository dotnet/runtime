// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading.Tasks;
using System.Xml.Serialization;

[assembly: DisableRuntimeMarshalling]

namespace System.Runtime.InteropServices.JavaScript.Tests
{
    public partial class JavaScriptTestHelper
    {
        [JSImport("globalThis.console.log")]
        public static partial void Log([JSMarshalAs<JSType.String>] string message);

        [JSImport("globalThis.window.location.toString")]
        public static partial string NativeFunctionToString();

        [JSImport("globalThis.data.echoMemberMethod")]
        public static partial string MemberEcho(string message);

        [JSImport("globalThis.rebound.echoMemberMethod")]
        public static partial string ReboundMemberEcho(string message);

        [JSExport]
        public static void ConsoleWriteLine([JSMarshalAs<JSType.String>] string message)
        {
            Console.WriteLine(message);
        }

        [JSImport("catch1toString", "JavaScriptTestHelper")]
        public static partial string catch1toString(string message, string functionName);

        [JSImport("catch1stack", "JavaScriptTestHelper")]
        public static partial string catch1stack(string message, string functionName);

        [JSExport]
        public static void ThrowFromJSExport(string message)
        {
            throw new ArgumentException(message);
        }

        [JSExport]
        [return: JSMarshalAs<JSType.Date>]
        public static DateTime Now()
        {
            return DateTime.Now;
        }

        // the methods in the region have signature for which we have optimized path in the call marshaler
        // it's the combination of number of arguments and void vs result
        // see mono_wasm_bind_js_function and mono_wasm_bind_cs_function
        #region Optimized

        public static int optimizedReached = 0;
        [JSExport]
        public static void Optimized0V()
        {
            optimizedReached++;
        }
        [JSImport("invoke0V", "JavaScriptTestHelper")]
        public static partial void invoke0V();

        [JSExport]
        public static void Optimized1V(int a1)
        {
            optimizedReached += a1;
        }
        [JSImport("invoke1V", "JavaScriptTestHelper")]
        public static partial void invoke1V(int a1);

        [JSExport]
        public static int Optimized1R(int a1)
        {
            optimizedReached += a1;
            return a1 + 1;
        }
        [JSImport("invoke1R", "JavaScriptTestHelper")]
        public static partial int invoke1R(int a1);

        [JSExport]
        public static int Optimized2R(int a1, int a2)
        {
            optimizedReached += a1 + a2;
            return a1 + a2 + 1;
        }
        [JSImport("invoke2R", "JavaScriptTestHelper")]
        public static partial int invoke2R(int a1, int a2);

        #endregion

        [JSImport("create_function", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Function<JSType.Number, JSType.Number, JSType.Number>>]
        public static partial Func<int, int, int> createMath([JSMarshalAs<JSType.String>] string a, [JSMarshalAs<JSType.String>] string b, [JSMarshalAs<JSType.String>] string code);

        [JSImport("getType1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.String>]
        internal static partial string getType1();

        [JSImport("getClass1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.String>]
        internal static partial string getClass1();

        [JSImport("throw0fn", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Discard>]
        internal static partial void throw0();

        [JSImport("returnError", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Any>]
        internal static partial object returnError();

        [JSImport("echo1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Promise<JSType.Void>>]
        internal static partial Task echo1_Task([JSMarshalAs<JSType.Promise<JSType.Void>>] Task arg1);

        [JSImport("createException", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Error>]
        internal static partial Exception createException([JSMarshalAs<JSType.String>] string name);

        [JSImport("createData", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Object>]
        internal static partial JSObject createData([JSMarshalAs<JSType.String>] string name);

        #region relaxed
        [JSImport("dummy", "JavaScriptTestHelper")]
        internal static partial void Relaxed(string a1, Exception ex,
            bool ab, double a6, byte a2, char a3, short a4, float a5, IntPtr a7,
            bool? nab, double? na6, byte? na2, char? na3, short? na4, float? na5, IntPtr? na7,
            Task<string> ta1, Task<Exception> tex,
            Task<bool> tab, Task<double> ta6, Task<byte> ta2, Task<char> ta3, Task<short> ta4, Task<float> ta5, Task<IntPtr> ta7,
            string[] aa1, byte[] aab, double[] aad, int[] aai
            );

        [JSImport("dummy", "JavaScriptTestHelper")]
        internal static partial double RelaxedDouble();

        [JSImport("dummy", "JavaScriptTestHelper")]
        internal static partial string RelaxedString();

        [JSImport("dummy", "JavaScriptTestHelper")]
        internal static partial string[] RelaxedStringArray();

        [JSImport("dummy", "JavaScriptTestHelper")]
        internal static partial Exception RelaxedException();

        [JSImport("dummy", "JavaScriptTestHelper")]
        internal static partial bool RelaxedBool();

        [JSImport("dummy", "JavaScriptTestHelper")]
        internal static partial double? RelaxedNullableDouble();

        [JSImport("dummy", "JavaScriptTestHelper")]
        internal static partial bool? RelaxedNullableBool();

        [JSImport("dummy", "JavaScriptTestHelper")]
        internal static partial Task RelaxedTask();

        [JSImport("dummy", "JavaScriptTestHelper")]
        internal static partial Task<double> RelaxedTaskDouble();

        [JSImport("dummy", "JavaScriptTestHelper")]
        internal static partial Task<string> RelaxedTaskString();

        [JSImport("dummy", "JavaScriptTestHelper")]
        internal static partial Task<Exception> RelaxedTaskException();

        [JSImport("dummy", "JavaScriptTestHelper")]
        internal static partial Task<bool> RelaxedTaskBool();


        #endregion

        #region Arrays

        [JSImport("echo1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Array<JSType.Number>>]
        internal static partial byte[]? echo1_ByteArray([JSMarshalAs<JSType.Array<JSType.Number>>] byte[]? value);

        [JSImport("storeAt", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal static partial byte? store_ByteArray([JSMarshalAs<JSType.Array<JSType.Number>>] byte[]? value, [JSMarshalAs<JSType.Number>] int index);

        [JSImport("echo1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Array<JSType.Number>>]
        internal static partial int[]? echo1_Int32Array([JSMarshalAs<JSType.Array<JSType.Number>>] int[]? value);

        [JSImport("storeAt", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal static partial int? store_Int32Array([JSMarshalAs<JSType.Array<JSType.Number>>] int[]? value, [JSMarshalAs<JSType.Number>] int index);

        [JSImport("echo1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Array<JSType.Number>>]
        internal static partial double[]? echo1_DoubleArray([JSMarshalAs<JSType.Array<JSType.Number>>] double[]? value);

        [JSImport("storeAt", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal static partial double? store_DoubleArray([JSMarshalAs<JSType.Array<JSType.Number>>] double[]? value, [JSMarshalAs<JSType.Number>] int index);

        [JSImport("echo1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Array<JSType.String>>]
        internal static partial string[]? echo1_StringArray([JSMarshalAs<JSType.Array<JSType.String>>] string[]? value);

        [JSImport("storeAt", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.String>]
        internal static partial string? store_StringArray([JSMarshalAs<JSType.Array<JSType.String>>] string[]? value, [JSMarshalAs<JSType.Number>] int index);

        [JSImport("echo1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Array<JSType.Any>>]
        internal static partial object[]? echo1_ObjectArray([JSMarshalAs<JSType.Array<JSType.Any>>] object[]? value);

        [JSImport("storeAt", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Any>]
        internal static partial object? store_ObjectArray([JSMarshalAs<JSType.Array<JSType.Any>>] object[]? value, [JSMarshalAs<JSType.Number>] int index);

        [JSImport("echo1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Array<JSType.Object>>]
        internal static partial JSObject[]? echo1_JSObjectArray([JSMarshalAs<JSType.Array<JSType.Object>>] JSObject[]? value);

        [JSImport("storeAt", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Object>]
        internal static partial JSObject? store_JSObjectArray([JSMarshalAs<JSType.Array<JSType.Object>>] JSObject[]? value, [JSMarshalAs<JSType.Number>] int index);

        #endregion

        #region Views

        [JSImport("echo1view", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.MemoryView>]
        internal static partial Span<byte> echo1_SpanOfByte([JSMarshalAs<JSType.MemoryView>] Span<byte> value, [JSMarshalAs<JSType.Boolean>] bool edit);

        [JSImport("echo1view", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.MemoryView>]
        internal static partial Span<int> echo1_SpanOfInt32([JSMarshalAs<JSType.MemoryView>] Span<int> value, [JSMarshalAs<JSType.Boolean>] bool edit);

        [JSImport("echo1view", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.MemoryView>]
        internal static partial Span<double> echo1_SpanOfDouble([JSMarshalAs<JSType.MemoryView>] Span<double> value, [JSMarshalAs<JSType.Boolean>] bool edit);

        [JSImport("echo1view", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.MemoryView>]
        internal static partial ArraySegment<byte> echo1_ArraySegmentOfByte([JSMarshalAs<JSType.MemoryView>] ArraySegment<byte> value, [JSMarshalAs<JSType.Boolean>] bool edit);

        [JSImport("echo1view", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.MemoryView>]
        internal static partial ArraySegment<int> echo1_ArraySegmentOfInt32([JSMarshalAs<JSType.MemoryView>] ArraySegment<int> value, [JSMarshalAs<JSType.Boolean>] bool edit);

        [JSImport("echo1view", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.MemoryView>]
        internal static partial ArraySegment<double> echo1_ArraySegmentOfDouble([JSMarshalAs<JSType.MemoryView>] ArraySegment<double> value, [JSMarshalAs<JSType.Boolean>] bool edit);

        #endregion

        #region  Int32
        [JSImport("echo1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal static partial int echo1_Int32([JSMarshalAs<JSType.Number>] int value);
        [JSImport("store1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Void>]
        internal static partial void store1_Int32([JSMarshalAs<JSType.Number>] int value);
        [JSImport("retrieve1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal static partial int retrieve1_Int32();
        [JSImport("identity1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Boolean>]
        internal static partial bool identity1_Int32([JSMarshalAs<JSType.Number>] int value);
        [JSImport("throw1fn", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal static partial int throw1_Int32([JSMarshalAs<JSType.Number>] int value);
        [JSImport("invoke1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal static partial int invoke1_Int32([JSMarshalAs<JSType.Number>] int value, [JSMarshalAs<JSType.String>] string name);
        [JSExport]
        [return: JSMarshalAs<JSType.Number>]
        public static int EchoInt32([JSMarshalAs<JSType.Number>] int arg1)
        {
            return arg1;
        }
        #endregion Int32

        #region String
        [JSImport("echo1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.String>]
        internal static partial string echo1_String([JSMarshalAs<JSType.String>] string value);
        [JSImport("store1", "JavaScriptTestHelper")]
        internal static partial void store1_String([JSMarshalAs<JSType.String>] string value);
        [JSImport("retrieve1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.String>]
        internal static partial string retrieve1_String();
        [JSImport("identity1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Boolean>]
        internal static partial bool identity1_String([JSMarshalAs<JSType.String>] string value);
        [JSImport("throw1fn", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.String>]
        internal static partial string throw1_String([JSMarshalAs<JSType.String>] string value);
        [JSImport("invoke1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.String>]
        internal static partial string invoke1_String([JSMarshalAs<JSType.String>] string value, [JSMarshalAs<JSType.String>] string name);
        [JSImport("invoke2", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.String>]
        internal static partial string invoke2_String([JSMarshalAs<JSType.String>] string value, [JSMarshalAs<JSType.String>] string name);
        [JSImport("invokeStructClassRecords", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Array<JSType.String>>]
        internal static partial string[] invokeStructClassRecords([JSMarshalAs<JSType.String>] string value);
        [JSExport]
        [return: JSMarshalAs<JSType.String>]
        public static string EchoString([JSMarshalAs<JSType.String>] string arg1)
        {
            return arg1;
        }

        [JSImport("echopromise", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Promise<JSType.String>>]
        internal static partial Task<string> echopromise_String([JSMarshalAs<JSType.String>] string value);
        #endregion String

        #region Object
        [JSImport("echo1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Any>]
        internal static partial object echo1_Object([JSMarshalAs<JSType.Any>] object value);
        [JSImport("store1", "JavaScriptTestHelper")]
        internal static partial void store1_Object([JSMarshalAs<JSType.Any>] object value);
        [JSImport("retrieve1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Any>]
        internal static partial object retrieve1_Object();
        [JSImport("identity1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Boolean>]
        internal static partial bool identity1_Object([JSMarshalAs<JSType.Any>] object value);
        [JSImport("throw1fn", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Any>]
        internal static partial object throw1_Object([JSMarshalAs<JSType.Any>] object value);
        [JSImport("invoke1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Any>]
        internal static partial object invoke1_Object([JSMarshalAs<JSType.Any>] object value, [JSMarshalAs<JSType.String>] string name);
        [JSExport]
        [return: JSMarshalAs<JSType.Any>]
        public static object EchoObject([JSMarshalAs<JSType.Any>] object arg1)
        {
            return arg1;
        }

        [JSImport("echopromise", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Promise<JSType.Any>>]
        internal static partial Task<object> echopromise_Object([JSMarshalAs<JSType.Any>] object value);
        #endregion Object

        #region Exception
        [JSImport("echo1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Error>]
        internal static partial Exception echo1_Exception([JSMarshalAs<JSType.Error>] Exception value);
        [JSImport("store1", "JavaScriptTestHelper")]
        internal static partial void store1_Exception([JSMarshalAs<JSType.Error>] Exception value);
        [JSImport("retrieve1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Error>]
        internal static partial Exception retrieve1_Exception();
        [JSImport("identity1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Boolean>]
        internal static partial bool identity1_Exception([JSMarshalAs<JSType.Error>] Exception value);
        [JSImport("throw1fn", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Error>]
        internal static partial Exception throw1_Exception([JSMarshalAs<JSType.Error>] Exception value);
        [JSImport("invoke1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Error>]
        internal static partial Exception invoke1_Exception([JSMarshalAs<JSType.Error>] Exception value, [JSMarshalAs<JSType.String>] string name);
        [JSExport]
        [return: JSMarshalAs<JSType.Error>]
        public static Exception EchoException([JSMarshalAs<JSType.Error>] Exception arg1)
        {
            return arg1;
        }
        [JSImport("echopromise", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Promise<JSType.Error>>]
        internal static partial Task<Exception> echopromise_Exception([JSMarshalAs<JSType.Error>] Exception value);
        #endregion Exception

        #region Task
        [JSImport("awaitvoid", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Promise<JSType.Void>>]
        internal static partial Task awaitvoid([JSMarshalAs<JSType.Promise<JSType.Void>>] Task arg1);
        [JSImport("sleep", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Promise<JSType.Void>>]
        internal static partial Task sleep([JSMarshalAs<JSType.Number>] int ms);
        [JSImport("forever", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Promise<JSType.Void>>]
        internal static partial Task forever();
        [JSImport("sleep", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Promise<JSType.Number>>]
        internal static partial Task<int> sleep_Int([JSMarshalAs<JSType.Number>] int ms);

        [JSImport("sleep", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Promise<JSType.Number>>]
        internal static partial Task<int>? sleepMaybe_Int([JSMarshalAs<JSType.Number>] int ms);

        [JSImport("await2", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Promise<JSType.Void>>]
        internal static partial Task await2([JSMarshalAs<JSType.Promise<JSType.Void>>] Task arg1);

        [JSImport("thenvoid", "JavaScriptTestHelper")]
        internal static partial void thenvoid([JSMarshalAs<JSType.Promise<JSType.Void>>] Task arg1);

        [JSImport("await1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Promise<JSType.Any>>]
        internal static partial Task<object> await1([JSMarshalAs<JSType.Promise<JSType.Any>>] Task<object> arg1);
        [JSImport("await1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Promise<JSType.Error>>]
        internal static partial Task<Exception> await1_TaskOfException([JSMarshalAs<JSType.Promise<JSType.Error>>] Task<Exception> arg1);
        [JSImport("invoke1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Promise<JSType.Any>>]
        internal static partial Task<object> invoke1_TaskOfObject([JSMarshalAs<JSType.Promise<JSType.Any>>] Task<object> value, [JSMarshalAs<JSType.String>] string name);
        [JSImport("invoke1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Promise<JSType.Number>>]
        internal static partial Task<int> invoke1_TaskOfInt([JSMarshalAs<JSType.Promise<JSType.Number>>] Task<int> value, [JSMarshalAs<JSType.String>] string name);

        [JSExport]
        [return: JSMarshalAs<JSType.Promise<JSType.Any>>]
        public static async Task<object> AwaitTaskOfObject([JSMarshalAs<JSType.Promise<JSType.Any>>] Task<object> arg1)
        {
            var res = await arg1;
            return res;
        }

        #endregion

        #region Action + Func

        [JSImport("back3", "JavaScriptTestHelper")]
        internal static partial void back3_Action([JSMarshalAs<JSType.Function>] Action action);

        [JSImport("echo1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Function>]
        internal static partial Action echo1_ActionAction([JSMarshalAs<JSType.Function>] Action action);

        [JSImport("echo1large", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Function>]
        internal static partial Action echo1large_ActionAction([JSMarshalAs<JSType.Function>] Action action);

        [JSImport("echo1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Function<JSType.Number>>]
        internal static partial Action<int> echo1_ActionIntActionInt([JSMarshalAs<JSType.Function<JSType.Number>>] Action<int> action);

        [JSImport("backback", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Function<JSType.Number, JSType.Number>>]
        internal static partial Func<int, int> backback_FuncIntFuncInt([JSMarshalAs<JSType.Function<JSType.Number, JSType.Number>>] Func<int, int> fun, [JSMarshalAs<JSType.Number>] int a);

        [JSImport("backback", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Function<JSType.Number, JSType.Number, JSType.Number>>]
        internal static partial Func<int, int, int> backback_FuncIntIntFuncIntInt([JSMarshalAs<JSType.Function<JSType.Number, JSType.Number, JSType.Number>>] Func<int, int, int> fun, [JSMarshalAs<JSType.Number>] int a, [JSMarshalAs<JSType.Number>] int b);

        [JSImport("back3", "JavaScriptTestHelper")]
        internal static partial void back3_ActionInt([JSMarshalAs<JSType.Function<JSType.Number>>] Action<int>? action, [JSMarshalAs<JSType.Number>] int a);

        [JSImport("back3", "JavaScriptTestHelper")]
        internal static partial void back3_ActionIntInt([JSMarshalAs<JSType.Function<JSType.Number, JSType.Number>>] Action<int, int>? action, [JSMarshalAs<JSType.Number>] int a, [JSMarshalAs<JSType.Number>] int b);

        [JSImport("back3", "JavaScriptTestHelper")]
        internal static partial void back3_ActionLongLong([JSMarshalAs<JSType.Function<JSType.Number, JSType.Number>>] Action<long, long>? action, [JSMarshalAs<JSType.Number>] long a, [JSMarshalAs<JSType.Number>] long b);

        [JSImport("back3", "JavaScriptTestHelper")]
        internal static partial void back3_ActionIntLong([JSMarshalAs<JSType.Function<JSType.Number, JSType.Number>>] Action<int, long>? action, [JSMarshalAs<JSType.Number>] int a, [JSMarshalAs<JSType.Number>] long b);

        [JSImport("back3", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal static partial int back3_FunctionIntInt([JSMarshalAs<JSType.Function<JSType.Number, JSType.Number>>] Func<int, int>? fun, [JSMarshalAs<JSType.Number>] int a);


        [JSImport("invoke1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Function<JSType.Number, JSType.Number>>]
        internal static partial Func<int, int> invoke1_FuncOfIntInt([JSMarshalAs<JSType.Function<JSType.Number, JSType.Number>>] Func<int, int> value, [JSMarshalAs<JSType.String>] string name);

        [JSExport]
        [return: JSMarshalAs<JSType.Function<JSType.Number, JSType.Number>>]
        public static Func<int, int> BackFuncOfIntInt([JSMarshalAs<JSType.Function<JSType.Number, JSType.Number>>] Func<int, int> arg1)
        {
            return (int a) =>
            {
                return arg1(a);
            };
        }

        #endregion

        #region Boolean
        [JSImport("echo1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Boolean>]
        internal static partial bool echo1_Boolean([JSMarshalAs<JSType.Boolean>] bool value);
        [JSImport("store1", "JavaScriptTestHelper")]
        internal static partial void store1_Boolean([JSMarshalAs<JSType.Boolean>] bool value);
        [JSImport("retrieve1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Boolean>]
        internal static partial bool retrieve1_Boolean();
        [JSImport("identity1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Boolean>]
        internal static partial bool identity1_Boolean([JSMarshalAs<JSType.Boolean>] bool value);
        [JSImport("throw1fn", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Boolean>]
        internal static partial bool throw1_Boolean([JSMarshalAs<JSType.Boolean>] bool value);
        [JSImport("invoke1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Boolean>]
        internal static partial bool invoke1_Boolean([JSMarshalAs<JSType.Boolean>] bool value, [JSMarshalAs<JSType.String>] string name);
        [JSExport]
        [return: JSMarshalAs<JSType.Boolean>]
        public static bool EchoBoolean([JSMarshalAs<JSType.Boolean>] bool arg1)
        {
            return arg1;
        }
        #endregion Boolean

        #region Char
        [JSImport("echo1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.String>]
        internal static partial char echo1_Char([JSMarshalAs<JSType.String>] char value);
        [JSImport("store1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Discard>]
        internal static partial void store1_Char([JSMarshalAs<JSType.String>] char value);
        [JSImport("retrieve1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.String>]
        internal static partial char retrieve1_Char();
        [JSImport("identity1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Boolean>]
        internal static partial bool identity1_Char([JSMarshalAs<JSType.String>] char value);
        [JSImport("throw1fn", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.String>]
        internal static partial char throw1_Char([JSMarshalAs<JSType.String>] char value);
        [JSImport("invoke1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.String>]
        internal static partial char invoke1_Char([JSMarshalAs<JSType.String>] char value, [JSMarshalAs<JSType.String>] string name);
        [JSExport]
        [return: JSMarshalAs<JSType.String>]
        public static char EchoChar([JSMarshalAs<JSType.String>] char arg1)
        {
            return arg1;
        }
        #endregion Byte

        #region Byte
        [JSImport("echo1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal static partial byte echo1_Byte([JSMarshalAs<JSType.Number>] byte value);
        [JSImport("store1", "JavaScriptTestHelper")]
        internal static partial void store1_Byte([JSMarshalAs<JSType.Number>] byte value);
        [JSImport("retrieve1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal static partial byte retrieve1_Byte();
        [JSImport("identity1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Boolean>]
        internal static partial bool identity1_Byte([JSMarshalAs<JSType.Number>] byte value);
        [JSImport("throw1fn", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal static partial byte throw1_Byte([JSMarshalAs<JSType.Number>] byte value);
        [JSImport("invoke1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal static partial byte invoke1_Byte([JSMarshalAs<JSType.Number>] byte value, [JSMarshalAs<JSType.String>] string name);
        [JSExport]
        [return: JSMarshalAs<JSType.Number>]
        public static byte EchoByte([JSMarshalAs<JSType.Number>] byte arg1)
        {
            return arg1;
        }
        #endregion Byte

        #region Int16
        [JSImport("echo1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal static partial short echo1_Int16([JSMarshalAs<JSType.Number>] short value);
        [JSImport("store1", "JavaScriptTestHelper")]
        internal static partial void store1_Int16([JSMarshalAs<JSType.Number>] short value);
        [JSImport("retrieve1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal static partial short retrieve1_Int16();
        [JSImport("identity1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Boolean>]
        internal static partial bool identity1_Int16([JSMarshalAs<JSType.Number>] short value);
        [JSImport("throw1fn", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal static partial short throw1_Int16([JSMarshalAs<JSType.Number>] short value);
        [JSImport("invoke1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal static partial short invoke1_Int16([JSMarshalAs<JSType.Number>] short value, [JSMarshalAs<JSType.String>] string name);
        [JSExport]
        [return: JSMarshalAs<JSType.Number>]
        public static short EchoInt16([JSMarshalAs<JSType.Number>] short arg1)
        {
            return arg1;
        }
        #endregion Int16

        #region Int52
        [JSImport("echo1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal static partial long echo1_Int52([JSMarshalAs<JSType.Number>] long value);
        [JSImport("store1", "JavaScriptTestHelper")]
        internal static partial void store1_Int52([JSMarshalAs<JSType.Number>] long value);
        [JSImport("retrieve1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal static partial long retrieve1_Int52();
        [JSImport("identity1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Boolean>]
        internal static partial bool identity1_Int52([JSMarshalAs<JSType.Number>] long value);
        [JSImport("throw1fn", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal static partial long throw1_Int52([JSMarshalAs<JSType.Number>] long value);
        [JSImport("invoke1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal static partial long invoke1_Int52([JSMarshalAs<JSType.Number>] long value, [JSMarshalAs<JSType.String>] string name);
        [JSExport]
        [return: JSMarshalAs<JSType.Number>]
        public static long EchoInt52([JSMarshalAs<JSType.Number>] long arg1)
        {
            return arg1;
        }
        #endregion Int52

        #region BigInt64
        [JSImport("echo1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.BigInt>]
        internal static partial long echo1_BigInt64([JSMarshalAs<JSType.BigInt>] long value);
        [JSImport("store1", "JavaScriptTestHelper")]
        internal static partial void store1_BigInt64([JSMarshalAs<JSType.BigInt>] long value);
        [JSImport("retrieve1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.BigInt>]
        internal static partial long retrieve1_BigInt64();
        [JSImport("identity1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Boolean>]
        internal static partial bool identity1_BigInt64([JSMarshalAs<JSType.BigInt>] long value);
        [JSImport("throw1fn", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.BigInt>]
        internal static partial long throw1_BigInt64([JSMarshalAs<JSType.BigInt>] long value);
        [JSImport("invoke1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.BigInt>]
        internal static partial long invoke1_BigInt64([JSMarshalAs<JSType.BigInt>] long value, [JSMarshalAs<JSType.String>] string name);
        [JSExport]
        [return: JSMarshalAs<JSType.BigInt>]
        public static long EchoBigInt64([JSMarshalAs<JSType.BigInt>] long arg1)
        {
            return arg1;
        }
        #endregion BigInt64

        #region Double
        [JSImport("echo1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal static partial double echo1_Double([JSMarshalAs<JSType.Number>] double value);
        [JSImport("store1", "JavaScriptTestHelper")]
        internal static partial void store1_Double([JSMarshalAs<JSType.Number>] double value);
        [JSImport("retrieve1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal static partial double retrieve1_Double();
        [JSImport("identity1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Boolean>]
        internal static partial bool identity1_Double([JSMarshalAs<JSType.Number>] double value);
        [JSImport("throw1fn", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal static partial double throw1_Double([JSMarshalAs<JSType.Number>] double value);
        [JSImport("invoke1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal static partial double invoke1_Double([JSMarshalAs<JSType.Number>] double value, [JSMarshalAs<JSType.String>] string name);
        [JSExport]
        [return: JSMarshalAs<JSType.Number>]
        public static double EchoDouble([JSMarshalAs<JSType.Number>] double arg1)
        {
            return arg1;
        }
        #endregion Double

        #region Single
        [JSImport("echo1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal static partial float echo1_Single([JSMarshalAs<JSType.Number>] float value);
        [JSImport("store1", "JavaScriptTestHelper")]
        internal static partial void store1_Single([JSMarshalAs<JSType.Number>] float value);
        [JSImport("retrieve1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal static partial float retrieve1_Single();
        [JSImport("identity1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Boolean>]
        internal static partial bool identity1_Single([JSMarshalAs<JSType.Number>] float value);
        [JSImport("throw1fn", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal static partial float throw1_Single([JSMarshalAs<JSType.Number>] float value);
        [JSImport("invoke1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal static partial float invoke1_Single([JSMarshalAs<JSType.Number>] float value, [JSMarshalAs<JSType.String>] string name);
        [JSExport]
        [return: JSMarshalAs<JSType.Number>]
        public static float EchoSingle([JSMarshalAs<JSType.Number>] float arg1)
        {
            return arg1;
        }
        #endregion Single

        #region IntPtr
        [JSImport("echo1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal static partial IntPtr echo1_IntPtr([JSMarshalAs<JSType.Number>] IntPtr value);
        [JSImport("store1", "JavaScriptTestHelper")]
        internal static partial void store1_IntPtr([JSMarshalAs<JSType.Number>] IntPtr value);
        [JSImport("retrieve1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal static partial IntPtr retrieve1_IntPtr();
        [JSImport("identity1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Boolean>]
        internal static partial bool identity1_IntPtr([JSMarshalAs<JSType.Number>] IntPtr value);
        [JSImport("throw1fn", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal static partial IntPtr throw1_IntPtr([JSMarshalAs<JSType.Number>] IntPtr value);
        [JSImport("invoke1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal static partial IntPtr invoke1_IntPtr([JSMarshalAs<JSType.Number>] IntPtr value, [JSMarshalAs<JSType.String>] string name);
        [JSExport]
        [return: JSMarshalAs<JSType.Number>]
        public static IntPtr EchoIntPtr([JSMarshalAs<JSType.Number>] IntPtr arg1)
        {
            return arg1;
        }
        #endregion IntPtr

        #region VoidPtr

        [JSImport("echo1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal unsafe static partial void* echo1_VoidPtr([JSMarshalAs<JSType.Number>] void* value);
        [JSImport("store1", "JavaScriptTestHelper")]
        internal unsafe static partial void store1_VoidPtr([JSMarshalAs<JSType.Number>] void* value);
        [JSImport("retrieve1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal unsafe static partial void* retrieve1_VoidPtr();
        [JSImport("identity1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Boolean>]
        internal unsafe static partial bool identity1_VoidPtr([JSMarshalAs<JSType.Number>] void* value);
        [JSImport("throw1fn", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal unsafe static partial void* throw1_VoidPtr([JSMarshalAs<JSType.Number>] void* value);
        [JSImport("invoke1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal unsafe static partial void* invoke1_VoidPtr([JSMarshalAs<JSType.Number>] void* value, [JSMarshalAs<JSType.String>] string name);
        [JSExport]
        [return: JSMarshalAs<JSType.Number>]
        public unsafe static void* EchoVoidPtr([JSMarshalAs<JSType.Number>] void* arg1)
        {
            return arg1;
        }
        #endregion VoidPtr

        #region DateTime
        [JSImport("echo1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Date>]
        internal static partial DateTime echo1_DateTime([JSMarshalAs<JSType.Date>] DateTime value);
        [JSImport("store1", "JavaScriptTestHelper")]
        internal static partial void store1_DateTime([JSMarshalAs<JSType.Date>] DateTime value);
        [JSImport("retrieve1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Date>]
        internal static partial DateTime retrieve1_DateTime();
        [JSImport("identity1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Boolean>]
        internal static partial bool identity1_DateTime([JSMarshalAs<JSType.Date>] DateTime value);
        [JSImport("throw1fn", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Date>]
        internal static partial DateTime throw1_DateTime([JSMarshalAs<JSType.Date>] DateTime value);
        [JSImport("invoke1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Date>]
        internal static partial DateTime invoke1_DateTime([JSMarshalAs<JSType.Date>] DateTime value, [JSMarshalAs<JSType.String>] string name);
        [JSExport]
        [return: JSMarshalAs<JSType.Date>]
        public static DateTime EchoDateTime([JSMarshalAs<JSType.Date>] DateTime arg1)
        {
            return arg1;
        }
        #endregion DateTime

        #region DateTimeOffset
        [JSImport("echo1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Date>]
        internal static partial DateTimeOffset echo1_DateTimeOffset([JSMarshalAs<JSType.Date>] DateTimeOffset value);
        [JSImport("store1", "JavaScriptTestHelper")]
        internal static partial void store1_DateTimeOffset([JSMarshalAs<JSType.Date>] DateTimeOffset value);
        [JSImport("retrieve1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Date>]
        internal static partial DateTimeOffset retrieve1_DateTimeOffset();
        [JSImport("identity1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Boolean>]
        internal static partial bool identity1_DateTimeOffset([JSMarshalAs<JSType.Date>] DateTimeOffset value);
        [JSImport("throw1fn", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Date>]
        internal static partial DateTimeOffset throw1_DateTimeOffset([JSMarshalAs<JSType.Date>] DateTimeOffset value);
        [JSImport("invoke1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Date>]
        internal static partial DateTimeOffset invoke1_DateTimeOffset([JSMarshalAs<JSType.Date>] DateTimeOffset value, [JSMarshalAs<JSType.String>] string name);
        [JSExport]
        [return: JSMarshalAs<JSType.Date>]
        public static DateTimeOffset EchoDateTimeOffset([JSMarshalAs<JSType.Date>] DateTimeOffset arg1)
        {
            return arg1;
        }
        #endregion DateTimeOffset

        #region NullableBoolean

        [JSImport("echo1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Boolean>]
        internal static partial bool? echo1_NullableBoolean([JSMarshalAs<JSType.Boolean>] bool? value);
        [JSImport("store1", "JavaScriptTestHelper")]
        internal static partial void store1_NullableBoolean([JSMarshalAs<JSType.Boolean>] bool? value);
        [JSImport("retrieve1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Boolean>]
        internal static partial bool? retrieve1_NullableBoolean();
        [JSImport("identity1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Boolean>]
        internal static partial bool identity1_NullableBoolean([JSMarshalAs<JSType.Boolean>] bool? value);
        [JSImport("throw1fn", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Boolean>]
        internal static partial bool? throw1_NullableBoolean([JSMarshalAs<JSType.Boolean>] bool? value);
        [JSImport("invoke1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Boolean>]
        internal static partial bool? invoke1_NullableBoolean([JSMarshalAs<JSType.Boolean>] bool? value, [JSMarshalAs<JSType.String>] string name);
        [JSExport]
        [return: JSMarshalAs<JSType.Boolean>]
        public static bool? EchoNullableBoolean([JSMarshalAs<JSType.Boolean>] bool? arg1)
        {
            return arg1;
        }
        #endregion NullableBoolean

        #region NullableInt32

        [JSImport("echo1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal static partial int? echo1_NullableInt32([JSMarshalAs<JSType.Number>] int? value);
        [JSImport("store1", "JavaScriptTestHelper")]
        internal static partial void store1_NullableInt32([JSMarshalAs<JSType.Number>] int? value);
        [JSImport("retrieve1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal static partial int? retrieve1_NullableInt32();
        [JSImport("identity1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Boolean>]
        internal static partial bool identity1_NullableInt32([JSMarshalAs<JSType.Number>] int? value);
        [JSImport("throw1fn", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal static partial int? throw1_NullableInt32([JSMarshalAs<JSType.Number>] int? value);
        [JSImport("invoke1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal static partial int? invoke1_NullableInt32([JSMarshalAs<JSType.Number>] int? value, [JSMarshalAs<JSType.String>] string name);
        [JSExport]
        [return: JSMarshalAs<JSType.Number>]
        public static int? EchoNullableInt32([JSMarshalAs<JSType.Number>] int? arg1)
        {
            return arg1;
        }
        #endregion NullableInt32

        #region NullableBigInt64

        [JSImport("echo1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.BigInt>]
        internal static partial long? echo1_NullableBigInt64([JSMarshalAs<JSType.BigInt>] long? value);
        [JSImport("store1", "JavaScriptTestHelper")]
        internal static partial void store1_NullableBigInt64([JSMarshalAs<JSType.BigInt>] long? value);
        [JSImport("retrieve1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.BigInt>]
        internal static partial long? retrieve1_NullableBigInt64();
        [JSImport("identity1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Boolean>]
        internal static partial bool identity1_NullableBigInt64([JSMarshalAs<JSType.BigInt>] long? value);
        [JSImport("throw1fn", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.BigInt>]
        internal static partial long? throw1_NullableBigInt64([JSMarshalAs<JSType.BigInt>] long? value);
        [JSImport("invoke1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.BigInt>]
        internal static partial long? invoke1_NullableBigInt64([JSMarshalAs<JSType.BigInt>] long? value, [JSMarshalAs<JSType.String>] string name);
        [JSExport]
        [return: JSMarshalAs<JSType.BigInt>]
        public static long? EchoNullableBigInt64([JSMarshalAs<JSType.BigInt>] long? arg1)
        {
            return arg1;
        }
        #endregion NullableBigInt64

        #region NullableIntPtr

        [JSImport("echo1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal static partial IntPtr? echo1_NullableIntPtr([JSMarshalAs<JSType.Number>] IntPtr? value);
        [JSImport("store1", "JavaScriptTestHelper")]
        internal static partial void store1_NullableIntPtr([JSMarshalAs<JSType.Number>] IntPtr? value);
        [JSImport("retrieve1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal static partial IntPtr? retrieve1_NullableIntPtr();
        [JSImport("identity1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Boolean>]
        internal static partial bool identity1_NullableIntPtr([JSMarshalAs<JSType.Number>] IntPtr? value);
        [JSImport("throw1fn", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal static partial IntPtr? throw1_NullableIntPtr([JSMarshalAs<JSType.Number>] IntPtr? value);
        [JSImport("invoke1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal static partial IntPtr? invoke1_NullableIntPtr([JSMarshalAs<JSType.Number>] IntPtr? value, [JSMarshalAs<JSType.String>] string name);
        [JSExport]
        [return: JSMarshalAs<JSType.Number>]
        public static IntPtr? EchoNullableIntPtr([JSMarshalAs<JSType.Number>] IntPtr? arg1)
        {
            return arg1;
        }
        #endregion NullableIntPtr

        #region NullableDouble

        [JSImport("echo1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal static partial double? echo1_NullableDouble([JSMarshalAs<JSType.Number>] double? value);
        [JSImport("store1", "JavaScriptTestHelper")]
        internal static partial void store1_NullableDouble([JSMarshalAs<JSType.Number>] double? value);
        [JSImport("retrieve1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal static partial double? retrieve1_NullableDouble();
        [JSImport("identity1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Boolean>]
        internal static partial bool identity1_NullableDouble([JSMarshalAs<JSType.Number>] double? value);
        [JSImport("throw1fn", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal static partial double? throw1_NullableDouble([JSMarshalAs<JSType.Number>] double? value);
        [JSImport("invoke1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Number>]
        internal static partial double? invoke1_NullableDouble([JSMarshalAs<JSType.Number>] double? value, [JSMarshalAs<JSType.String>] string name);
        [JSExport]
        [return: JSMarshalAs<JSType.Number>]
        public static double? EchoNullableDouble([JSMarshalAs<JSType.Number>] double? arg1)
        {
            return arg1;
        }
        #endregion NullableDouble

        #region NullableDateTime

        [JSImport("echo1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Date>]
        internal static partial DateTime? echo1_NullableDateTime([JSMarshalAs<JSType.Date>] DateTime? value);
        [JSImport("store1", "JavaScriptTestHelper")]
        internal static partial void store1_NullableDateTime([JSMarshalAs<JSType.Date>] DateTime? value);
        [JSImport("retrieve1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Date>]
        internal static partial DateTime? retrieve1_NullableDateTime();
        [JSImport("identity1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Boolean>]
        internal static partial bool identity1_NullableDateTime([JSMarshalAs<JSType.Date>] DateTime? value);
        [JSImport("throw1fn", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Date>]
        internal static partial DateTime? throw1_NullableDateTime([JSMarshalAs<JSType.Date>] DateTime? value);
        [JSImport("invoke1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Date>]
        internal static partial DateTime? invoke1_NullableDateTime([JSMarshalAs<JSType.Date>] DateTime? value, [JSMarshalAs<JSType.String>] string name);
        [JSExport]
        [return: JSMarshalAs<JSType.Date>]
        public static DateTime? EchoNullableDateTime([JSMarshalAs<JSType.Date>] DateTime? arg1)
        {
            return arg1;
        }
        #endregion NullableDateTime

        #region JSObject
        [JSImport("echo1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Object>]
        internal static partial JSObject echo1_JSObject([JSMarshalAs<JSType.Object>] JSObject value);
        [JSImport("store1", "JavaScriptTestHelper")]
        internal static partial void store1_JSObject([JSMarshalAs<JSType.Object>] JSObject value);
        [JSImport("retrieve1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Object>]
        internal static partial JSObject retrieve1_JSObject();
        [JSImport("identity1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Boolean>]
        internal static partial bool identity1_JSObject([JSMarshalAs<JSType.Object>] JSObject value);
        [JSImport("throw1fn", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Object>]
        internal static partial JSObject throw1_JSObject([JSMarshalAs<JSType.Object>] JSObject value);
        [JSImport("invoke1", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Object>]
        internal static partial JSObject invoke1_JSObject([JSMarshalAs<JSType.Object>] JSObject value, [JSMarshalAs<JSType.String>] string name);
        [JSExport]
        [return: JSMarshalAs<JSType.Object>]
        public static JSObject EchoIJSObject([JSMarshalAs<JSType.Object>] JSObject arg1)
        {
            return arg1;
        }

        [JSImport("echopromise", "JavaScriptTestHelper")]
        [return: JSMarshalAs<JSType.Promise<JSType.Object>>]
        internal static partial Task<JSObject> echopromise_JSObject([JSMarshalAs<JSType.Object>] JSObject value);
        #endregion JSObject

        [JSImport("setup", "JavaScriptTestHelper")]
        internal static partial Task Setup();

        [JSImport("INTERNAL.forceDisposeProxies")]
        internal static partial void ForceDisposeProxies(bool disposeMethods, bool verbose);

        static JSObject _module;
        public static async Task InitializeAsync()
        {
            if (_module == null)
            {
                _module = await JSHost.ImportAsync("JavaScriptTestHelper", "../JavaScriptTestHelper.mjs"); ;
                await Setup();
            }

#if FEATURE_WASM_MANAGED_THREADS
            // are we in the UI thread ?
            if (Environment.CurrentManagedThreadId == 1)
#endif
            {
                // this gives browser chance to serve UI thread event loop before every test
                await Task.Yield();
            }
        }

        public static Task DisposeAsync()
        {
            _module?.Dispose();
            _module = null;
            return Task.CompletedTask;
        }
    }
}

namespace JavaScriptTestHelperNamespace
{
    public partial class JavaScriptTestHelper
    {
        [System.Runtime.InteropServices.JavaScript.JSExport]
        public static string EchoString(string message)
        {
            return message + "11";
        }

        private partial class NestedClass
        {
            [System.Runtime.InteropServices.JavaScript.JSExport]
            public static string EchoString(string message) => message + "12";

            private partial class DoubleNestedClass
            {
                [System.Runtime.InteropServices.JavaScript.JSExport]
                public static string EchoString(string message) => message + "13";
            }
        }

        public partial record class NestedRecordClass
        {
            [System.Runtime.InteropServices.JavaScript.JSExport]
            public static string EchoString(string message) => message + "14";

            public partial record class DoubleNestedRecordClass
            {
                [System.Runtime.InteropServices.JavaScript.JSExport]
                public static string EchoString(string message) => message + "15";
            }
        }

        public partial struct NestedStruct
        {
            [System.Runtime.InteropServices.JavaScript.JSExport]
            public static string EchoString(string message) => message + "16";

            public partial struct DoubleNestedStruct
            {
                [System.Runtime.InteropServices.JavaScript.JSExport]
                public static string EchoString(string message) => message + "17";
            }
        }

        public partial record struct NestedRecordStruct
        {
            [System.Runtime.InteropServices.JavaScript.JSExport]
            public static string EchoString(string message) => message + "18";

            public partial record struct DoubleNestedRecordStruct
            {
                [System.Runtime.InteropServices.JavaScript.JSExport]
                public static string EchoString(string message) => message + "19";
            }
        }
    }

    public partial class JavaScriptTestHelperStruct
    {
        [System.Runtime.InteropServices.JavaScript.JSExport]
        public static string EchoString(string message) => message + "21";

        public partial class NestedClass
        {
            [System.Runtime.InteropServices.JavaScript.JSExport]
            public static string EchoString(string message) => message + "22";
        }

        public partial record class NestedRecordClass
        {
            [System.Runtime.InteropServices.JavaScript.JSExport]
            public static string EchoString(string message) => message + "23";
        }

        public partial struct NestedStruct
        {
            [System.Runtime.InteropServices.JavaScript.JSExport]
            public static string EchoString(string message)
            {
                return message + "24";
            }
        }

        public partial record struct NestedRecordStruct
        {
            [System.Runtime.InteropServices.JavaScript.JSExport]
            public static string EchoString(string message) => message + "25";
        }
    }

    public partial record class JavaScriptTestHelperRecordClass
    {
        [System.Runtime.InteropServices.JavaScript.JSExport]
        public static string EchoString(string message) => message + "31";

        public partial class NestedClass
        {
            [System.Runtime.InteropServices.JavaScript.JSExport]
            public static string EchoString(string message) => message + "32";
        }

        public partial record class NestedRecordClass
        {
            [System.Runtime.InteropServices.JavaScript.JSExport]
            public static string EchoString(string message) => message + "33";
        }

        public partial struct NestedStruct
        {
            [System.Runtime.InteropServices.JavaScript.JSExport]
            public static string EchoString(string message) => message + "34";
        }

        public partial record struct NestedRecordStruct
        {
            [System.Runtime.InteropServices.JavaScript.JSExport]
            public static string EchoString(string message) => message + "35";
        }
    }

    public partial record struct JavaScriptTestHelperRecordStruct
    {
        [System.Runtime.InteropServices.JavaScript.JSExport]
        public static string EchoString(string message) => message + "41";

        public partial class NestedClass
        {
            [System.Runtime.InteropServices.JavaScript.JSExport]
            public static string EchoString(string message) => message + "42";
        }

        public partial record class NestedRecordClass
        {
            [System.Runtime.InteropServices.JavaScript.JSExport]
            public static string EchoString(string message) => message + "43";
        }

        public partial struct NestedStruct
        {
            [System.Runtime.InteropServices.JavaScript.JSExport]
            public static string EchoString(string message) => message + "44";
        }

        public partial record struct NestedRecordStruct
        {
            [System.Runtime.InteropServices.JavaScript.JSExport]
            public static string EchoString(string message)
            {
                return message + "45";
            }
        }
    }
}

public partial class JavaScriptTestHelperNoNamespace
{
    [System.Runtime.InteropServices.JavaScript.JSExport]
    public static string EchoString(string message) => message + "51";

    public partial class NestedClass
    {
        [System.Runtime.InteropServices.JavaScript.JSExport]
        public static string EchoString(string message) => message + "52";

        public partial class DoubleNestedClass
        {
            [System.Runtime.InteropServices.JavaScript.JSExport]
            public static string EchoString(string message) => message + "53";
        }
    }

    public partial record class NestedRecordClass
    {
        [System.Runtime.InteropServices.JavaScript.JSExport]
        public static string EchoString(string message) => message + "54";

        public partial record class DoubleNestedRecordClass
        {
            [System.Runtime.InteropServices.JavaScript.JSExport]
            public static string EchoString(string message) => message + "55";
        }
    }

    public partial struct NestedStruct
    {
        [System.Runtime.InteropServices.JavaScript.JSExport]
        public static string EchoString(string message) => message + "56";

        public partial struct DoubleNestedStruct
        {
            [System.Runtime.InteropServices.JavaScript.JSExport]
            public static string EchoString(string message) => message + "57";
        }
    }

    public partial record struct NestedRecordStruct
    {
        [System.Runtime.InteropServices.JavaScript.JSExport]
        public static string EchoString(string message) => message + "58";

        public partial record struct DoubleNestedRecordStruct
        {
            [System.Runtime.InteropServices.JavaScript.JSExport]
            public static string EchoString(string message) => message + "59";
        }
    }
}

public partial class JavaScriptTestHelperStructNoNamespace
{
    [System.Runtime.InteropServices.JavaScript.JSExport]
    public static string EchoString(string message) => message + "61";

    public partial class NestedClass
    {
        [System.Runtime.InteropServices.JavaScript.JSExport]
        public static string EchoString(string message) => message + "62";
    }

    public partial record class NestedRecordClass
    {
        [System.Runtime.InteropServices.JavaScript.JSExport]
        public static string EchoString(string message) => message + "63";
    }

    public partial struct NestedStruct
    {
        [System.Runtime.InteropServices.JavaScript.JSExport]
        public static string EchoString(string message) => message + "64";
    }

    public partial record struct NestedRecordStruct
    {
        [System.Runtime.InteropServices.JavaScript.JSExport]
        public static string EchoString(string message) => message + "65";
    }
}

public partial record class JavaScriptTestHelperRecordClassNoNamespace
{
    [System.Runtime.InteropServices.JavaScript.JSExport]
    public static string EchoString(string message) => message + "71";

    public partial class NestedClass
    {
        [System.Runtime.InteropServices.JavaScript.JSExport]
        public static string EchoString(string message) => message + "72";
    }

    public partial record class NestedRecordClass
    {
        [System.Runtime.InteropServices.JavaScript.JSExport]
        public static string EchoString(string message) => message + "73";
    }

    public partial struct NestedStruct
    {
        [System.Runtime.InteropServices.JavaScript.JSExport]
        public static string EchoString(string message) => message + "74";
    }

    public partial record struct NestedRecordStruct
    {
        [System.Runtime.InteropServices.JavaScript.JSExport]
        public static string EchoString(string message) => message + "75";
    }
}

public partial record struct JavaScriptTestHelperRecordStructNoNamespace
{
    [System.Runtime.InteropServices.JavaScript.JSExport]
    public static string EchoString(string message) => message + "81";

    public partial class NestedClass
    {
        [System.Runtime.InteropServices.JavaScript.JSExport]
        public static string EchoString(string message) => message + "82";
    }

    public partial record class NestedRecordClass
    {
        [System.Runtime.InteropServices.JavaScript.JSExport]
        public static string EchoString(string message) => message + "83";
    }

    public partial struct NestedStruct
    {
        [System.Runtime.InteropServices.JavaScript.JSExport]
        public static string EchoString(string message) => message + "84";
    }

    public partial record struct NestedRecordStruct
    {
        [System.Runtime.InteropServices.JavaScript.JSExport]
        public static string EchoString(string message) => message + "85";
    }
}
