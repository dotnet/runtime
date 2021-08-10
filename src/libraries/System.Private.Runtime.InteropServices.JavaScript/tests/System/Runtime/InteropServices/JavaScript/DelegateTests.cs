// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
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
        public static void InvokeFunctionAcceptingArrayTypes(Function objectPrototype, string creator, JSObject arrayType)
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

        [Fact]
        public static void DispatchToDelegate()
        {
            var factory = new Function(@"return {
                callback: null,
                eventFactory:function(data){
                    return {
                        data:data
                    };
                },
                fireEvent: function (evt) {
                    this.callback(evt);
                }
            };");
            var dispatcher = (JSObject)factory.Call();
            var temp = new bool[2];
            Action<JSObject> cb = (JSObject envt) =>
            {
                var data = (int)envt.GetObjectProperty("data");
                temp[data] = true;
            };
            dispatcher.SetObjectProperty("callback", cb);
            var evnt0 = dispatcher.Invoke("eventFactory", 0);
            var evnt1 = dispatcher.Invoke("eventFactory", 1);
            dispatcher.Invoke("fireEvent", evnt0);
            dispatcher.Invoke("fireEvent", evnt0);
            dispatcher.Invoke("fireEvent", evnt1);
            Assert.True(temp[0]);
            Assert.True(temp[1]);
        }

        [Fact]
        public static void EventsAreNotCollected()
        {
            const int attempts = 100; // we fire 100 events in a loop, to try that it's GC same
            var factory = new Function(@"return {
                callback: null,
                eventFactory:function(data){
                    return {
                        data:data
                    };
                },
                fireEvent: function (evt) {
                    this.callback(evt);
                }
            };");
            var dispatcher = (JSObject)factory.Call();
            var temp = new bool[attempts];
            Action<JSObject> cb = (JSObject envt) =>
            {
#if DEBUG
                envt.AssertNotDisposed();
                envt.AssertInFlight(0);
#endif
                var data = (int)envt.GetObjectProperty("data");
                temp[data] = true;
            };
            dispatcher.SetObjectProperty("callback", cb);

            var evnt = dispatcher.Invoke("eventFactory", 0);
            for (int i = 0; i < attempts; i++)
            {
                var evnti = dispatcher.Invoke("eventFactory", i);
                dispatcher.Invoke("fireEvent", evnti);
                dispatcher.Invoke("fireEvent", evnt);
                Runtime.InvokeJS("if (globalThis.gc) globalThis.gc();");// needs v8 flag --expose-gc
            }
        }


        [Fact]
        public static void NullDelegate()
        {
            var factory = new Function("delegate", "callback", @"
                callback(delegate);
            ");

            Delegate check = null;
            Action<Delegate> callback = (Delegate data) =>
            {
                check = data;
            };
            factory.Call(null, null, callback);
            Assert.Null(check);
        }

        [Fact]
        public static async Task ResolveStringPromise()
        {
            var factory = new Function(@"
                return new Promise((resolve, reject) => {
                  setTimeout(() => {
                    resolve('foo');
                  }, 10);
                });");

            var promise = (Task<object>)factory.Call();
            var value = await promise;

            Assert.Equal("foo", (string)value);
            
        }

        [Fact]
        public static async Task ResolveJSObjectPromise()
        {
            for (int i = 0; i < 10; i++)
            {
                var factory = new Function(@"
                return new Promise((resolve, reject) => {
                  setTimeout(() => {
                    resolve({foo:'bar'});
                  }, 10);
                });");

                var promise = (Task<object>)factory.Call();
                var value = (JSObject)await promise;

                Assert.Equal("bar", value.GetObjectProperty("foo"));
                Runtime.InvokeJS("if (globalThis.gc) globalThis.gc();");// needs v8 flag --expose-gc
            }
        }

        [Fact]
        public static async Task RejectPromise()
        {
            var factory = new Function(@"
                return new Promise((resolve, reject) => {
                  setTimeout(() => {
                    reject('fail');
                  }, 10);
                });");

            var promise = (Task<object>)factory.Call();

            var ex = await Assert.ThrowsAsync<JSException>(async () => await promise);
            Assert.Equal("fail", ex.Message);
        }

        [Fact]
        public static async Task RejectPromiseError()
        {
            var factory = new Function(@"
                return new Promise((resolve, reject) => {
                  setTimeout(() => {
                    reject(new Error('fail'));
                  }, 10);
                });");

            var promise = (Task<object>)factory.Call();

            var ex = await Assert.ThrowsAsync<JSException>(async () => await promise);
            Assert.Equal("Error: fail", ex.Message);
        }


        [ActiveIssue("https://github.com/dotnet/runtime/issues/56963")]
        [Fact]
        public static void RoundtripPromise()
        {
            var factory = new Function(@"
                var dummy=new Promise((resolve, reject) => {});
                return {
                    dummy:dummy,
                    check:(promise)=>{
                        return promise===dummy ? 1:0;
                    }
                }");

            var obj = (JSObject)factory.Call();
            var dummy = obj.GetObjectProperty("dummy");
            Assert.IsType<Task<object>>(dummy);
            var check = obj.Invoke("check", dummy);
            Assert.Equal(1, check);
        }


        [Fact]
        public static async Task ResolveTask()
        {
            var tcs = new TaskCompletionSource<int>();
            var factory = new Function("task", "callback", @"
                return task.then((data)=>{
                    callback(data);
                })
            ");

            int check = 0;
            Action<int> callback = (int data) =>
            {
                check = data;
            };
            Task<int> task = tcs.Task;
            // we are testing that Task is marshaled as thenable
            var promise = (Task<object>)factory.Call(null, task, callback);
            tcs.SetResult(1);
            // the result value is not applied until we await the promise
            Assert.Equal(0, check);
            await promise;
            // but it's set after we do
            Assert.Equal(1, check);
        }

        [Fact]
        public static async Task RejectTask()
        {
            var tcs = new TaskCompletionSource<int>();
            var factory = new Function("task", "callback", @"
                return task.catch((reason)=>{
                    callback(reason);
                })
            ");

            string check = null;
            Action<string> callback = (string data) =>
            {
                check = data;
            };
            var promise = (Task<object>)factory.Call(null, tcs.Task, callback);
            Assert.Null(check);
            tcs.SetException(new Exception("test"));
            Assert.Null(check);
            await promise;
            Assert.Contains("System.Exception: test", check);
        }

        [Fact]
        public static void NullTask()
        {
            var tcs = new TaskCompletionSource<int>();
            var factory = new Function("task", "callback", @"
                callback(task);
            ");

            Task check = Task.FromResult(1);
            Action<Task> callback = (Task data) =>
            {
                check = data;
            };
            factory.Call(null, null, callback);
            Assert.Null(check);
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/56963")]
        [Fact]
        public static void RoundtripTask()
        {
            var tcs = new TaskCompletionSource<int>();
            var factory = new Function("dummy", @"
                return {
                    dummy:dummy,
                }");

            var obj = (JSObject)factory.Call(null, tcs.Task);
            var dummy = obj.GetObjectProperty("dummy");
            Assert.IsType<Task<int>>(dummy);
        }
    }
}
