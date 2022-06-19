// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Runtime.InteropServices.JavaScript.Tests
{
    public static class TypedArrayTests
    {
        private static Function _objectPrototype;

        public static IEnumerable<object[]> Object_Prototype()
        {
            _objectPrototype ??= new Function("return Object.prototype.toString;");
            yield return new object[] { _objectPrototype.Call() };
        }

        [Theory]
        [MemberData(nameof(Object_Prototype))]
        public static void Uint8ArrayFrom(Function objectPrototype)
        {
            var array = new byte[50];
            Uint8Array from = Uint8Array.From(array);
            Assert.Equal(50, from.Length);
            Assert.Equal("[object Uint8Array]", objectPrototype.Call(from));
        }

        [Theory]
        [MemberData(nameof(Object_Prototype))]
        public static void Uint8ArrayFromArrayBuffer(Function objectPrototype)
        {
            Uint8Array from = new Uint8Array(new ArrayBuffer(50));
            Assert.True(from.Length == 50);
            Assert.Equal("[object Uint8Array]", objectPrototype.Call(from));
        }
        
        [Fact]
        public async static Task Uint8ArrayToArray()
        {
            var factory = new Function("size", "return new Uint8Array(new ArrayBuffer(size));");
            var rnd = new Random();

            int iterations = 10;

            List<Task> tasks=new List<Task>();

            for (int i = 0; i < iterations; i++)
            {
                tasks.Add(Task.Run(() => {
                    int bufferSize = 1024 * 1024 * rnd.Next(10);
                    var proxy = (Uint8Array)factory.Call(null, bufferSize);
                    proxy[0]=42;
                    proxy[bufferSize-1]=42;
                    Assert.Equal(bufferSize, proxy.Length);
                    Assert.Equal(0, proxy[1]);
                    Assert.Equal(0, proxy[bufferSize-2]);
                    Assert.Equal(42, proxy[0]);
                    Assert.Equal(42, proxy[bufferSize-1]);

                    Thread.Sleep(rnd.Next(100));

                    Assert.Equal(bufferSize, proxy.Length);
                    Assert.Equal(0, proxy[1]);
                    Assert.Equal(0, proxy[bufferSize-2]);
                    Assert.Equal(42, proxy[0]);
                    Assert.Equal(42, proxy[bufferSize-1]);

                    var data = proxy.ToArray();

                    Assert.Equal(bufferSize, data.Length);
                    Assert.Equal(0, data[1]);
                    Assert.Equal(0, data[bufferSize-2]);
                    Assert.Equal(42, data[0]);
                    Assert.Equal(42, data[bufferSize-1]);

                    proxy.Dispose();
                }));
            }

            await Task.WaitAll(tasks.ToArray());
        }
    }
}
