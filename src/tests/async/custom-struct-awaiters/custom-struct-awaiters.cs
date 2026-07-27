// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class CustomStructAwaiters
{
    private static int s_safeAwaiterValue;
    private static int s_unsafeAwaiterValue;

    [Fact]
    public static void TestEntryPoint()
    {
        Run().Wait();
    }

    private static async Task Run()
    {
        Volatile.Write(ref s_safeAwaiterValue, 0);
        await new SafeAwaitable(42);
        Assert.Equal(42, Volatile.Read(ref s_safeAwaiterValue));

        Volatile.Write(ref s_unsafeAwaiterValue, 0);
        await new UnsafeAwaitable(43);
        Assert.Equal(43, Volatile.Read(ref s_unsafeAwaiterValue));
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

            public void GetResult() => Assert.Equal(42, _value);
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

            public void GetResult() => Assert.Equal(43, _value);
        }
    }
}
