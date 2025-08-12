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

namespace Sample
{
    public partial class Test
    {
        public static class Assert
        {
            private static string FormatIfArray(object? obj)
            {
                if (obj is Array arr)
                {
                    var items = new System.Text.StringBuilder();
                    items.Append('[');
                    for (int i = 0; i < arr.Length; i++)
                    {
                        if (i > 0) items.Append(", ");
                        var value = arr.GetValue(i);
                        // Recursively format nested arrays
                        items.Append(FormatIfArray(value));
                    }
                    items.Append(']');
                    return items.ToString();
                }
                // For collections (e.g., List<object>)
                if (obj is IEnumerable enumerable && !(obj is string))
                {
                    var items = new System.Text.StringBuilder();
                    items.Append('[');
                    bool first = true;
                    foreach (var value in enumerable)
                    {
                        if (!first) items.Append(", ");
                        items.Append(FormatIfArray(value));
                        first = false;
                    }
                    items.Append(']');
                    return items.ToString();
                }
                return obj?.ToString() ?? "null";
            }

            public static void NotEqual<T>(T expected, T actual)
            {
                if (object.Equals(expected, actual))
                {
                    string expectedStr = FormatIfArray(expected);
                    string actualStr = FormatIfArray(actual);
                    throw new Exception($"AssertHelper.NotEqual failed. Expected: {expectedStr}. Actual: {actualStr}.");                    
                }
            }
            public static void Equal<T>(T expected, T actual)
            {
                // Handle nulls
                if (ReferenceEquals(expected, actual))
                    return;
                if (expected is null || actual is null)
                    throw new Exception($"AssertHelper.Equal failed. Expected: {FormatIfArray(expected)}, Actual: {FormatIfArray(actual)}.");

                // Recursively compare arrays
                if (expected is Array expectedArray && actual is Array actualArray)
                {
                    if (expectedArray.Length != actualArray.Length)
                        throw new Exception($"AssertHelper.Equal failed. Array lengths differ. Expected: {FormatIfArray(expectedArray)}, Actual: {FormatIfArray(actualArray)}.");

                    for (int i = 0; i < expectedArray.Length; i++)
                    {
                        var e = expectedArray.GetValue(i);
                        var a = actualArray.GetValue(i);
                        try
                        {
                            // Recursively call Equal for nested arrays/objects
                            Equal(e, a);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"AssertHelper.Equal failed at index {i}. Expected: {FormatIfArray(expectedArray)}, Actual: {FormatIfArray(actualArray)}. Inner: {ex.Message}");
                        }
                    }
                    return;
                }

                // Fallback to default equality
                if (!object.Equals(expected, actual))
                {
                    throw new Exception($"AssertHelper.Equal failed. Expected: {FormatIfArray(expected)}, Actual: {FormatIfArray(actual)}.");
                }
            }
            public static void True(bool condition)
            {
                if (!condition)
                    throw new Exception("AssertHelper.True failed. Condition was false.");
            }
            public static void False(bool condition)
            {
                if (condition)
                    throw new Exception("AssertHelper.False failed. Condition was true.");
            }
        }
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

        [JSExport]
        public static async Task DoTestMethod()
        {
            await JavaScriptTestHelper.InitializeAsync();
            int[] testData = new int[] { 1, 2, 3, int.MaxValue, int.MinValue };
            object[] objectTestData = { new object[] { string.Intern("hello"), string.Empty } };
            JsImportObjectArray(objectTestData);
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
