// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.WebSockets.Tests
{
    public class ZLibStreamTests
    {
        [Fact]
        public async Task PoolShouldReuseTheSameInstance()
        {
            var pool = new Pool(timeoutMilliseconds: 25);

            object inflater = pool.GetInflater();
            for ( var i = 0; i < 10_000; ++i)
            {
                pool.ReturnInflater(inflater);
                Assert.Equal(1, pool.ActiveCount);

                object nextInflater = pool.GetInflater();
                Assert.Equal(0, pool.ActiveCount);

                Assert.Equal(inflater, nextInflater);
            }
            pool.ReturnInflater(inflater);

            Assert.Equal(1, pool.ActiveCount);
            await Task.Delay(200);

            // After timeout elapses we should not have any active instances
            Assert.Equal(0, pool.ActiveCount);
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.Browser)] // There is no concurrency in browser
        public async Task PoolingConcurrently()
        {
            var pool = new Pool(timeoutMilliseconds: 25);
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = 16
            };
            Parallel.For(0, 100_000, parallelOptions, x =>
            {
                if (x % 2 == 0)
                {
                    object inflater = pool.GetInflater();
                    pool.ReturnInflater(inflater);
                }
                else
                {
                    object deflater = pool.GetDeflater();
                    pool.ReturnDeflater(deflater);
                }
            });

            Assert.True(pool.ActiveCount >= 2);
            Assert.True(pool.ActiveCount <= parallelOptions.MaxDegreeOfParallelism * 2);
            await Task.Delay(200);
            Assert.Equal(0, pool.ActiveCount);
        }

        private sealed class Pool
        {
            private static Type? s_type;
            private static ConstructorInfo? s_constructor;
            private static FieldInfo? s_activeCount;
            private static MethodInfo? s_rentInflater;
            private static MethodInfo? s_returnInflater;
            private static MethodInfo? s_rentDeflater;
            private static MethodInfo? s_returnDeflater;

            private readonly object _instance;

            public Pool(int timeoutMilliseconds)
            {
                s_type ??= typeof(WebSocket).Assembly.GetType("System.Net.WebSockets.Compression.ZLibStreamPool", throwOnError: true);
                s_constructor ??= s_type.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)[0];

                _instance = s_constructor.Invoke(new object[] { /*windowBits*/9, timeoutMilliseconds });
            }

            public int ActiveCount => (int)(s_activeCount ??= s_type.GetField("_activeCount", BindingFlags.Instance | BindingFlags.NonPublic)).GetValue(_instance);

            public object GetInflater() => GetMethod(ref s_rentInflater).Invoke(_instance, null);

            public void ReturnInflater(object inflater) => GetMethod(ref s_returnInflater).Invoke(_instance, new[] { inflater });

            public object GetDeflater() => GetMethod(ref s_rentDeflater).Invoke(_instance, null);

            public void ReturnDeflater(object deflater) => GetMethod(ref s_returnDeflater).Invoke(_instance, new[] { deflater });

            private static MethodInfo GetMethod(ref MethodInfo? method, [CallerMemberName] string? name = null)
            {
                return method ??= s_type.GetMethod(name)
                    ?? throw new InvalidProgramException($"Method {name} was not found in {s_type}.");
            }
        }
    }
}
