// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.InteropServices.JavaScript.Tests;
using System.Threading.Tasks;
using System.Security;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Tests;
using System.Globalization;
using  System.SpanTests;
namespace Sample
{
    public partial class Test
    {
        private char[] _largeBuffer = new char[4096];

        public static async Task<int> Main(string[] args)
        {
            var rand = new Random();
            Console.WriteLine("Today's lucky number is " + rand.Next(100) + " and " + Guid.NewGuid());

            return 0;
        }
        
        public static async Task JsExportTaskOfInt(int value)
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

        public static unsafe void JsExportVoidPtr(IntPtr xvalue)
        {
            void* value = (void*)xvalue;
            void* res = JavaScriptTestHelper.invoke1_VoidPtr(value, nameof(JavaScriptTestHelper.EchoVoidPtr));
            Assert.True(value == res);
        }

        public static unsafe void JsImportIntArray(int[]? expected)
        {
            var actual = JavaScriptTestHelper.echo1_Int32Array(expected);
            Assert.Equal(expected, actual);
            if (expected != null) for (int i = 0; i < expected.Length; i++)
                {
                    var actualI = JavaScriptTestHelper.store_Int32Array(expected, i);
                    Assert.Equal(expected[i], actualI);
                }
        }

        public static unsafe void JsImportArraySegmentOfInt32()
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
        
        public static unsafe void JsImportArraySegmentOfDouble()
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

        public static unsafe void JsImportSpanOfDouble()
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
        public static unsafe void JsImportObjectArray(object[]? expected)
        {
            if (expected?.Length == 1 && expected[0] is string s && s == "JSData")
            {
                expected = new object[] { new object[] { JavaScriptTestHelper.createData("test"), JavaScriptTestHelper.createException("test") } };
            }
            var actual = JavaScriptTestHelper.echo1_ObjectArray(expected);
            Console.WriteLine($"Checking two arrays for equality: expected={0}, actual={1}", 
                expected == null ? "null" : string.Join(", ", expected), 
                actual == null ? "null" : string.Join(", ", actual));

            Assert.Equal(expected, actual);

            Console.WriteLine("Checking element-wise equality of arrays.");
            if (expected != null) for (int i = 0; i < expected.Length; i++)
                {
                    var actualI = JavaScriptTestHelper.store_ObjectArray(expected, i);
                    Assert.Equal(expected[i], actualI);
                }
        }

        public static unsafe void JsImportVoidPtr(IntPtr xvalue)
        {
            void* value = (void*)xvalue;

            JavaScriptTestHelper.store1_VoidPtr(value);
            void* res = JavaScriptTestHelper.retrieve1_VoidPtr();
            Assert.True(value == res);
            res = JavaScriptTestHelper.echo1_VoidPtr(value);
            Assert.True(value == res);

            var actualJsType = JavaScriptTestHelper.getType1();
            string expectedType = IntPtr.Size == 4 ? "number" : "bigint";
            Assert.Equal(expectedType, actualJsType);
        }

        public static async Task JsImportTaskTypes()
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
        public static void JsImportInstanceMember()
        {
            var actual = JavaScriptTestHelper.MemberEcho("t-e-s-t");
            Assert.StartsWith("t-e-s-t-w-i-t-h-i-n-s-t-a-n-c-e", actual);
        }

        public static void JsImportNullableIntPtr(IntPtr? value)
        {
            string expectedType = IntPtr.Size == 4 ? "number" : "bigint";
            JsImportTest(value,
                JavaScriptTestHelper.store1_NullableIntPtr,
                JavaScriptTestHelper.retrieve1_NullableIntPtr,
                JavaScriptTestHelper.echo1_NullableIntPtr,
                JavaScriptTestHelper.throw1_NullableIntPtr,
                JavaScriptTestHelper.identity1_NullableIntPtr,
               expectedType);
        }

        private static void JsImportTest<T>(T value
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

        public static unsafe void BadCast()
        {
            JSException ex;
            JSHost.DotnetInstance.SetProperty("testBool", true);
            //ex = Assert.Throws<JSException>(() => JSHost.DotnetInstance.GetPropertyAsInt32("testBool"));
            //Assert.Contains("Value is not an integer", ex.Message);
            //ex = Assert.Throws<JSException>(() => JSHost.DotnetInstance.GetPropertyAsDouble("testBool"));
            //Assert.Contains("Value is not a Number", ex.Message);
            //ex = Assert.Throws<JSException>(() => JSHost.DotnetInstance.GetPropertyAsString("testBool"));
            //Assert.Contains("Value is not a String", ex.Message);
            //ex = Assert.Throws<JSException>(() => JSHost.DotnetInstance.GetPropertyAsJSObject("testBool"));
            //Assert.Contains("JSObject proxy of boolean is not supported", ex.Message);
            ex = Assert.Throws<JSException>(() => JSHost.DotnetInstance.GetPropertyAsByteArray("testBool"));
            Assert.Contains("Value is not an Array or Uint8Array", ex.Message);
            //JSHost.DotnetInstance.SetProperty("testInt", 42);
            //ex = Assert.Throws<JSException>(() => JSHost.DotnetInstance.GetPropertyAsBoolean("testInt"));
            //Assert.Contains("Value is not a Boolean", ex.Message);
        }


        private static bool IsNullOrWin32Atom(IntPtr ptr)
        {
            const long HIWORDMASK = unchecked((long)0xffffffffffff0000L);

            long lPtr = (long)ptr;
            Console.WriteLine($"IsNullOrWin32Atom: {lPtr} ({ptr})");
            return 0 == (lPtr & HIWORDMASK);
        }
        public static void TestPtrIsAtom(IntPtr ptr)
        {
            Assert.True(IsNullOrWin32Atom(ptr), $"Expected {ptr} to be <64k but it is not.");
        }

        public static unsafe string? MyPtrToStringUTF8(IntPtr ptr)
        {
            Console.WriteLine($"MyPtrToStringUTF8: {ptr}");
            if (IsNullOrWin32Atom(ptr))
            {
                Console.WriteLine($"MyPtrToStringUTF8: returning NULL");
                return null;
            }

            return "Junk";
        }

        public static void PtrToStringUTF8_Win32AtomPointer_ReturnsNull()
        {
            // Windows Marshal has specific checks that does not do
            // anything if the ptr is less than 64K.
            IntPtr testVal = 1;
            string res = Marshal.PtrToStringUTF8(testVal);
            //string res = MyPtrToStringUTF8(testVal);
            Console.WriteLine($"Marshal.PtrToStringUTF8({testVal}) = {res}");
            Assert.Null(Marshal.PtrToStringUTF8((IntPtr)1));
        }
        public static void PtrToStringUTF8_ZeroPointer_ReturnsNull()
        {
            Assert.Null(Marshal.PtrToStringUTF8(IntPtr.Zero));
        }

        public static void ZeroFreeGlobalAllocAnsi_Zero_Nop()
        {
            Marshal.ZeroFreeGlobalAllocAnsi(IntPtr.Zero);
        }

        private static SecureString ToSecureString(string data)
        {
            var str = new SecureString();
            foreach (char c in data)
            {
                str.AppendChar(c);
            }
            str.MakeReadOnly();
            return str;
        }

        public static void ZeroFreeGlobalAllocAnsi_ValidPointer_Success()
        {
            using (SecureString secureString = ToSecureString("hello"))
            {
                IntPtr ptr = Marshal.SecureStringToGlobalAllocAnsi(secureString);
                Marshal.ZeroFreeGlobalAllocAnsi(ptr);
            }
        }
        

        [JSExport]
        public static async Task DoTestMethod()
        {
            //await JavaScriptTestHelper.InitializeAsync();
            //const bool doCollect = true;
            string myTestString = "Hello from .NET";
            unsafe
            {
                fixed (char* p = myTestString)
                {
                    Console.WriteLine($"Address of x: 0x{(ulong)p:X}");
                    StringTests.PrintMyStringFromCSharp(myTestString);
                }
            }
            
            //if (doCollect) GC.Collect();

            //ZeroFreeGlobalAllocAnsi_Zero_Nop();
            //if (doCollect) GC.Collect();            
            //ZeroFreeGlobalAllocAnsi_ValidPointer_Success();
            //if (doCollect) GC.Collect();

            //System.SpanTests.TryWriteTests.AppendFormatted_ReferenceTypes_ICustomFormatter();
            //if (doCollect) GC.Collect();
            //System.SpanTests.TryWriteTests.AppendFormatted_ValueTypes_CreateProviderFlowed();
            //if (doCollect) GC.Collect();


            //GCHandleTests.Ctor_Default();
            //if (doCollect) GC.Collect();


            //int[] testData = new int[] { 1, 2, 3, int.MaxValue, int.MinValue };
            //object[] objectTestData = { new object[] { string.Intern("hello"), string.Empty } };

            //JsImportNullableIntPtr((IntPtr)42);
            //GeneralInterop.MH_SetLogVerbosity(1); // export isn't picked up!
            //PtrToStringUTF8_ZeroPointer_ReturnsNull();
            //IsNullOrWin32Atom((IntPtr)1);
            //PtrToStringUTF8_Win32AtomPointer_ReturnsNull();
            //GetFunctionPointerForDelegate_MarshalledDelegateGeneric_ReturnsExpected();
            //BadCast();
            //JsImportInstanceMember();
            //JsImportInstanceMember();
            //await JsImportTaskTypes();
            //JsImportVoidPtr((IntPtr)42);

            //JsImportObjectArray(objectTestData);
            //JsImportArraySegmentOfInt32();
            //JsImportArraySegmentOfDouble();
            //JsImportSpanOfDouble();
            //JsImportIntArray(testData);
            //JsExportVoidPtr(-9223372036854775808); //IntPtr.MinValue
            //JsExportVoidPtr(IntPtr.MinValue); 
            //await JsExportTaskOfInt(-2147483648);
            //await JsExportTaskOfInt(8);
            //await JsExportTaskOfInt(0);            
        }
    }
}
