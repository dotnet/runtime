// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.Runtime.InteropServices.JavaScript.Tests
{
    public static class JavaScriptTests
    {
        [Fact]
        public static void CoreTypes()
        {
            var arr1 = new Uint8Array(50);
            Assert.Equal(50, arr1.Length);
        }

        [Fact]
        public static void FunctionSum()
        {
            // The Difference Between call() and apply()
            // The difference is:
            //      The call() method takes arguments separately.
            //      The apply() method takes arguments as an array.
            var sum = new Function("a", "b", "return a + b");
            Assert.Equal(8, (int)sum.Call(null, 3, 5));

            Assert.Equal(13, (int)sum.Apply(null, new object[] { 6, 7 }));
        }

        [Fact]
        public static void FunctionMath()
        {
            JSObject math = (JSObject)Runtime.GetGlobalObject("Math");
            Assert.True(math != null, "math != null");

            Function mathMax = (Function)math.GetObjectProperty("max");
            Assert.True(mathMax != null, "math.max != null");

            var maxValue = (int)mathMax.Apply(null, new object[] { 5, 6, 2, 3, 7 });
            Assert.Equal(7, maxValue);

            maxValue = (int)mathMax.Call(null, 5, 6, 2, 3, 7);
            Assert.Equal(7, maxValue);

            Function mathMin = (Function)((JSObject)Runtime.GetGlobalObject("Math")).GetObjectProperty("min");
            Assert.True(mathMin != null, "math.min != null");

            var minValue = (int)mathMin.Apply(null, new object[] { 5, 6, 2, 3, 7 });
            Assert.Equal(2, minValue);

            minValue = (int)mathMin.Call(null, 5, 6, 2, 3, 7);
            Assert.Equal(2, minValue);
        }

        [Fact]
        [OuterLoop("slow")]
        public static async Task BagIterator()
        {
            await Task.Delay(1);

            var bagFn = new Function(@"
                    var same = {
                        x:1
                    };
                    return Object.entries({
                        a:1,
                        b:'two',
                        c:{fold:{}},
                        d:same,
                        e:same,
                        f:same
                    });
                ");

            for (int attempt = 0; attempt < 100_000; attempt++)
            {
                try
                {
                    using var bag = (JSObject)bagFn.Call(null);
                    using var entriesIterator = (JSObject)bag.Invoke("entries");

                    var cnt = entriesIterator.ToEnumerable().Count();
                    Assert.Equal(6, cnt);

                    // fill GC helps to repro
                    var x = new byte[100 + attempt / 100];
                    if (attempt % 1000 == 0)
                    {
                        Utils.InvokeJS("if (globalThis.gc) globalThis.gc();");// needs v8 flag --expose-gc
                        GC.Collect();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message + " At attempt=" + attempt, ex);
                }
            }
        }

        [Fact]
        public static async Task Iterator()
        {
            await Task.Delay(1);

            var makeRangeIterator = new Function("start", "end", "step", @"
                    let nextIndex = start;
                    let iterationCount = 0;

                    const rangeIterator = {
                       next: function() {
                           let result;
                           if (nextIndex < end) {
                               result = { value: {}, done: false }
                               nextIndex += step;
                               iterationCount++;
                               return result;
                           }
                           return { value: {}, done: true }
                       }
                    };
                    return rangeIterator;
                ");

            const int count = 500;
            for (int attempt = 0; attempt < 100; attempt++)
            {
                int index = 0;
                try
                {
                    var entriesIterator = (JSObject)makeRangeIterator.Call(null, 0, count, 1);
                    Assert.NotNull(entriesIterator);
                    using (entriesIterator)
                    {
                        var enumerable = entriesIterator.ToEnumerable();
                        var enumerator = enumerable.GetEnumerator();
                        Assert.NotNull(enumerator);

                        using (enumerator)
                        {
                            while (enumerator.MoveNext())
                            {
                                Assert.NotNull(enumerator.Current);
                                index++;
                            }
                        }
                    }
                    Assert.Equal(count, index);
                }
                catch (Exception ex)
                {
                    throw new Exception($"At attempt={attempt}, index={index}: {ex.Message}", ex);
                }
                await Task.Yield();
            }
        }

        public static IEnumerable<object> ToEnumerable(this JSObject iterrator)
        {
            JSObject nextResult = null;
            try
            {
                nextResult = (JSObject)iterrator.Invoke("next");
                var done = (bool)nextResult.GetObjectProperty("done");
                while (!done)
                {
                    object value = nextResult.GetObjectProperty("value");
                    nextResult.Dispose();
                    yield return value;
                    nextResult = (JSObject)iterrator.Invoke("next");
                    done = (bool)nextResult.GetObjectProperty("done");
                }
            }
            finally
            {
                nextResult?.Dispose();
            }
        }

        [Fact]
        public static void RoundtripCSDate()
        {
            var factory = new Function("dummy", @"
                return {
                    dummy:dummy,
                }");
            var date = new DateTime(2021, 01, 01, 12, 34, 45);

            var obj = (JSObject)factory.Call(null, date);
            var dummy = (DateTime)obj.GetObjectProperty("dummy");
            Assert.Equal(date, dummy);
        }

        [Fact]
        public static void RoundtripJSDate()
        {
            var factory = new Function(@"
                var dummy = new Date(2021, 00, 01, 12, 34, 45, 567);
                return {
                    dummy:dummy,
                    check:(value) => {
                        return value.valueOf()==dummy.valueOf() ? 1 : 0;
                    },
                }");
            var obj = (JSObject)factory.Call();

            var date = (DateTime)obj.GetObjectProperty("dummy");
            var check = (int)obj.Invoke("check", date);
            Assert.Equal(1, check);
        }

    }
}
