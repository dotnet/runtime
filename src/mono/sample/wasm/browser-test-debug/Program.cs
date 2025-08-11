// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Runtime.InteropServices.JavaScript.Tests;

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
                        items.Append(arr.GetValue(i)?.ToString());
                    }
                    items.Append(']');
                    return items.ToString();
                }
                return obj?.ToString() ?? "null";
            }

            public static void Equal<T>(T expected, T actual)
            {
                if (expected is Array expectedArray && actual is Array actualArray)
                {
                    if (expectedArray.Length != actualArray.Length)
                    {
                        throw new Exception($"AssertHelper.Equal failed. Array lengths differ. Expected: {expectedArray.Length}, Actual: {actualArray.Length}. Expected: {FormatIfArray(expectedArray)}, Actual: {FormatIfArray(actualArray)}.");
                    }
                    for (int i = 0; i < expectedArray.Length; i++)
                    {
                        var e = expectedArray.GetValue(i);
                        var a = actualArray.GetValue(i);
                        if (!object.Equals(e, a))
                        {
                            throw new Exception($"AssertHelper.Equal failed at index {i}. Expected: {FormatIfArray(expectedArray)}, Actual: {FormatIfArray(actualArray)}.");
                        }
                    }
                    return;
                }
                if (!object.Equals(expected, actual))
                {
                    string expectedStr = FormatIfArray(expected);
                    string actualStr = FormatIfArray(actual);
                    throw new Exception($"AssertHelper.Equal failed. Expected: {expectedStr}. Actual: {actualStr}.");
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

        [JSExport]
        public static async Task DoTestMethod()
        {
            await JavaScriptTestHelper.InitializeAsync();
            int[] testData = new int[] { 1, 2, 3, int.MaxValue, int.MinValue };

            JsImportIntArray(testData);
            //JsExportVoidPtr(-9223372036854775808); //IntPtr.MinValue
            //JsExportVoidPtr(IntPtr.MinValue); 
            //await JsExportTaskOfInt(-2147483648);
            //await JsExportTaskOfInt(8);
            //await JsExportTaskOfInt(0);            
        }
    }
}
