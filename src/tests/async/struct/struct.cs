// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable 1998

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class Async2Struct
{
    private static int s_safeAwaiterValue;
    private static int s_unsafeAwaiterValue;

    [Fact]
    public static void TestEntryPoint()
    {
        Async().Wait();
        Async2().Wait();
        CustomAwaiters().Wait();
    }

    [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(false)]
    private static async Task Async()
    {
        S s = new S(100);
        await s.Test();
        AssertEqual(100, s.Value);
    }

    private static async Task Async2()
    {
        S s = new S(100);
        await s.Test();
        AssertEqual(100, s.Value);
    }

    private static async Task CustomAwaiters()
    {
        Volatile.Write(ref s_safeAwaiterValue, 0);
        await new SafeAwaitable(42);
        AssertEqual(42, Volatile.Read(ref s_safeAwaiterValue));

        Volatile.Write(ref s_unsafeAwaiterValue, 0);
        await new UnsafeAwaitable(43);
        AssertEqual(43, Volatile.Read(ref s_unsafeAwaiterValue));
    }

    private readonly struct SafeAwaitable
    {
        private readonly int _value;

        public SafeAwaitable(int value) => _value = value;

        public SafeAwaiter GetAwaiter() => new SafeAwaiter(_value);

        public readonly struct SafeAwaiter : INotifyCompletion
        {
            private readonly int _value;

            public SafeAwaiter(int value) => _value = value;

            public bool IsCompleted => false;

            public void OnCompleted(Action continuation)
            {
                Volatile.Write(ref s_safeAwaiterValue, _value);
                ThreadPool.QueueUserWorkItem(static state => ((Action)state!).Invoke(), continuation);
            }

            public void GetResult() => AssertEqual(42, _value);
        }
    }

    private readonly struct UnsafeAwaitable
    {
        private readonly int _value;

        public UnsafeAwaitable(int value) => _value = value;

        public UnsafeAwaiter GetAwaiter() => new UnsafeAwaiter(_value);

        public readonly struct UnsafeAwaiter : ICriticalNotifyCompletion
        {
            private readonly int _value;

            public UnsafeAwaiter(int value) => _value = value;

            public bool IsCompleted => false;

            public void OnCompleted(Action continuation) => throw new InvalidOperationException();

            public void UnsafeOnCompleted(Action continuation)
            {
                Volatile.Write(ref s_unsafeAwaiterValue, _value);
                ThreadPool.UnsafeQueueUserWorkItem(static state => ((Action)state!).Invoke(), continuation);
            }

            public void GetResult() => AssertEqual(43, _value);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AssertEqual(int expected, int val)
    {
        Assert.Equal(expected, val);
    }

    private struct S
    {
        public int Value;

        public S(int value) => Value = value;

        public async Task Test()
        {
            AssertEqual(100, this.Value);
            this.Value++;
            await this.InstanceCall();
            AssertEqual(101, this.Value);

            await this.TaskButNotAsync();
            AssertEqual(102, this.Value);
        }

        private async Task InstanceCall()
        {
            AssertEqual(101, this.Value);
            this.Value++;
            AssertEqual(102, this.Value);
            await Task.Yield();
            AssertEqual(102, this.Value);
        }

        private Task TaskButNotAsync()
        {
            AssertEqual(101, Value);
            Value++;
            return Task.CompletedTask;
        }
    }
}
