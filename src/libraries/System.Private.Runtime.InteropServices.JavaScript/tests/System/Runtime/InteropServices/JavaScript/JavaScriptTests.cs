// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Xunit;

namespace System.Runtime.InteropServices.JavaScript.Tests
{
    public static class JavaScriptTests
    {
        [Fact]
        public static void CoreTypes()
        {
            var arr = new Uint8ClampedArray(50);
            Assert.Equal(50, arr.Length);
            Assert.Equal(TypedArrayTypeCode.Uint8ClampedArray, arr.GetTypedArrayType());

            var arr1 = new Uint8Array(50);
            Assert.Equal(50, arr1.Length);
            Assert.Equal(TypedArrayTypeCode.Uint8Array, arr1.GetTypedArrayType());

            var arr2 = new Uint16Array(50);
            Assert.Equal(50, arr2.Length);
            Assert.Equal(TypedArrayTypeCode.Uint16Array, arr2.GetTypedArrayType());

            var arr3 = new Uint32Array(50);
            Assert.Equal(50, arr3.Length);
            Assert.Equal(TypedArrayTypeCode.Uint32Array, arr3.GetTypedArrayType());

            var arr4 = new Int8Array(50);
            Assert.Equal(50, arr4.Length);
            Assert.Equal(TypedArrayTypeCode.Int8Array, arr4.GetTypedArrayType());

            var arr5 = new Int16Array(50);
            Assert.Equal(50, arr5.Length);
            Assert.Equal(TypedArrayTypeCode.Int16Array, arr5.GetTypedArrayType());

            var arr6 = new Int32Array(50);
            Assert.Equal(50, arr6.Length);
            Assert.Equal(TypedArrayTypeCode.Int32Array, arr6.GetTypedArrayType());

            var arr7 = new Float32Array(50);
            Assert.Equal(50, arr7.Length);
            Assert.Equal(TypedArrayTypeCode.Float32Array, arr7.GetTypedArrayType());

            var arr8 = new Float64Array(50);
            Assert.Equal(50, arr8.Length);
            Assert.Equal(TypedArrayTypeCode.Float64Array, arr8.GetTypedArrayType());

            var sharedArr40 = new SharedArrayBuffer(40);
            var sharedArr50 = new SharedArrayBuffer(50);

            var arr9 = new Uint8ClampedArray(sharedArr50);
            Assert.Equal(50, arr9.Length);

            var arr10 = new Uint8Array(sharedArr50);
            Assert.Equal(50, arr10.Length);

            var arr11 = new Uint16Array(sharedArr50);
            Assert.Equal(25, arr11.Length);

            var arr12 = new Uint32Array(sharedArr40);
            Assert.Equal(10, arr12.Length);

            var arr13 = new Int8Array(sharedArr50);
            Assert.Equal(50, arr13.Length);

            var arr14 = new Int16Array(sharedArr40);
            Assert.Equal(20, arr14.Length);

            var arr15 = new Int32Array(sharedArr40);
            Assert.Equal(10, arr15.Length);

            var arr16 = new Float32Array(sharedArr40);
            Assert.Equal(10, arr16.Length);

            var arr17 = new Float64Array(sharedArr40);
            Assert.Equal(5, arr17.Length);
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

            for (int attempt = 0; attempt < 100_000; attempt++)
            {
                try
                {
                    using (var entriesIterator = (JSObject)makeRangeIterator.Call(null, 0, 500))
                    {
                        var cnt = entriesIterator.ToEnumerable().Count();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message + " At attempt=" + attempt, ex);
                }
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

        private static JSObject SetupListenerTest (string prefix) {
            Runtime.InvokeJS($"globalThis.{prefix} = {{" + @"
    listeners: [],
    addEventListener: function (name, listener, options) {
        if (name === 'throwError')
            throw new Error('throwError throwing');
        var capture = !options ? false : !!options.capture;
        for (var i = 0; i < this.listeners.length; i++) {
            var item = this.listeners[i];
            if (item[0] !== name)
                continue;
            var itemCapture = !item[2] ? false : !!item[2].capture;
            if (itemCapture !== capture)
                continue;
            if (item[1] === listener)
                return;
        }        
        this.listeners.push([name, listener, options || null]);
    },
    removeEventListener: function (name, listener, capture) {
        for (var i = 0; i < this.listeners.length; i++) {
            var item = this.listeners[i];
            if (item[0] !== name)
                continue;
            if (item[1] !== listener)
                continue;
            var itemCapture = !item[2] ? false : !!item[2].capture;
            if (itemCapture !== !!capture)
                continue;
            this.listeners.splice(i, 1);
            return;
        }
    },
    fireEvent: function (name, evt) {
        this._fireEventImpl(name, true, evt);
        this._fireEventImpl(name, false, evt);
    },
    _fireEventImpl: function (name, capture, evt) {
        for (var i = 0; i < this.listeners.length; i++) {
            var item = this.listeners[i];
            if (item[0] !== name)
                continue;
            var itemCapture = !item[2] ? false : (item[2].capture || false);
            if (itemCapture !== capture)
                continue;
            item[1].call(this, evt);
        }
    },
};
");
            return (JSObject)Runtime.GetGlobalObject(prefix);
        }

        [Fact]
        public static void AddEventListenerWorks () {
            var temp = new bool[1];
            var obj = SetupListenerTest("addEventListenerWorks");
            obj.AddEventListener("test", () => {
                temp[0] = true;
            });
            obj.Invoke("fireEvent", "test");
            Assert.True(temp[0]);
        }

        [Fact]
        public static void AddEventListenerPassesOptions () {
            var log = new List<string>();
            var obj = SetupListenerTest("addEventListenerPassesOptions");
            obj.AddEventListener("test", () => {
                log.Add("Capture");
            }, new JSObject.EventListenerOptions { Capture = true });
            obj.AddEventListener("test", () => {
                log.Add("Non-capture");
            }, new JSObject.EventListenerOptions { Capture = false });
            obj.Invoke("fireEvent", "test");
            Assert.Equal("Capture", log[0]);
            Assert.Equal("Non-capture", log[1]);
        }

        [Fact]
        public static void AddEventListenerForwardsExceptions () {
            var obj = SetupListenerTest("addEventListenerForwardsExceptions");
            obj.AddEventListener("test", () => {
                throw new Exception("Test exception");
            });
            var exc = Assert.Throws<JSException>(() => {
                obj.Invoke("fireEvent", "test");
            });
            Assert.Contains("Test exception", exc.Message);

            exc = Assert.Throws<JSException>(() => {
                obj.AddEventListener("throwError", () => {
                    throw new Exception("Should not be called");
                });
            });
            Assert.Contains("throwError throwing", exc.Message);
            obj.Invoke("fireEvent", "throwError");
        }

        [Fact]
        public static void RemovedEventListenerIsNotCalled () {
            var obj = SetupListenerTest("removedEventListenerIsNotCalled");
            Action del = () => {
                throw new Exception("Should not be called");
            };
            obj.AddEventListener("test", del);
            Assert.Throws<JSException>(() => {
                obj.Invoke("fireEvent", "test");
            });

            obj.RemoveEventListener("test", del);
            obj.Invoke("fireEvent", "test");
        }

        [Fact]
        public static void RegisterSameEventListener () {
            var counter = new int[1];
            var obj = SetupListenerTest("registerSameDelegateTwice");
            Action del = () => {
                counter[0]++;
            };

            obj.AddEventListener("test1", del);
            obj.AddEventListener("test2", del);

            obj.Invoke("fireEvent", "test1");
            Assert.Equal(1, counter[0]);
            obj.Invoke("fireEvent", "test2");
            Assert.Equal(2, counter[0]);

            obj.RemoveEventListener("test1", del);
            obj.Invoke("fireEvent", "test1");
            obj.Invoke("fireEvent", "test2");
            Assert.Equal(3, counter[0]);

            obj.RemoveEventListener("test2", del);
            obj.Invoke("fireEvent", "test1");
            obj.Invoke("fireEvent", "test2");
            Assert.Equal(3, counter[0]);
        }

        [Fact]
        public static void UseAddEventListenerResultToRemove () {
            var obj = SetupListenerTest("useAddEventListenerResultToRemove");
            Action del = () => {
                throw new Exception("Should not be called");
            };
            var handle = obj.AddEventListener("test", del);
            Assert.Throws<JSException>(() => {
                obj.Invoke("fireEvent", "test");
            });

            obj.RemoveEventListener("test", handle);
            obj.Invoke("fireEvent", "test");
        }

        [Fact]
        public static void RegisterSameEventListenerToMultipleSources () {
            var counter = new int[1];
            var a = SetupListenerTest("registerSameEventListenerToMultipleSourcesA");
            var b = SetupListenerTest("registerSameEventListenerToMultipleSourcesB");
            Action del = () => {
                counter[0]++;
            };

            a.AddEventListener("test", del);
            b.AddEventListener("test", del);

            a.Invoke("fireEvent", "test");
            Assert.Equal(1, counter[0]);
            b.Invoke("fireEvent", "test");
            Assert.Equal(2, counter[0]);

            a.RemoveEventListener("test", del);
            a.Invoke("fireEvent", "test");
            b.Invoke("fireEvent", "test");
            Assert.Equal(3, counter[0]);

            b.RemoveEventListener("test", del);
            a.Invoke("fireEvent", "test");
            b.Invoke("fireEvent", "test");
            Assert.Equal(3, counter[0]);
        }
    }
}
