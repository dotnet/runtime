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

        // Reference type instantiations share code, so the awaiters here are
        // runtime determined and require runtime lookups.
        await RunGeneric("shared generic");
        await RunGeneric(new object());
        // Value type instantiations are exact.
        await RunGeneric(44);

        // The canonical helper requires an instantiation argument, but the
        // exact instantiation is known statically rather than runtime determined.
        await RunKnownReferenceType();
    }

    private static async Task RunKnownReferenceType()
    {
        string value = "known reference type";

        string safeResult =
            await new GenericSafeAwaitable<string>(value);
        Assert.Equal(value, safeResult);

        string unsafeResult =
            await new GenericUnsafeAwaitable<string>(value);
        Assert.Equal(value, unsafeResult);
    }

    private static async Task RunGeneric<T>(T value)
    {
        Tracker<T>.Value = default;
        Tracker<T>.Completions = 0;

        T safeResult = await new GenericSafeAwaitable<T>(value);
        Assert.Equal(value, safeResult);
        Assert.Equal(value, Tracker<T>.Value);
        Assert.Equal(1, Volatile.Read(ref Tracker<T>.Completions));

        Tracker<T>.Value = default;

        T unsafeResult = await new GenericUnsafeAwaitable<T>(value);
        Assert.Equal(value, unsafeResult);
        Assert.Equal(value, Tracker<T>.Value);
        Assert.Equal(2, Volatile.Read(ref Tracker<T>.Completions));
    }

    private static class Tracker<T>
    {
        public static T? Value;
        public static int Completions;
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

    private readonly struct GenericSafeAwaitable<T>
    {
        private readonly T _value;

        public GenericSafeAwaitable(T value) => _value = value;

        public GenericSafeAwaiter<T> GetAwaiter() => new GenericSafeAwaiter<T>(_value);
    }

    private readonly struct GenericSafeAwaiter<T> : INotifyCompletion
    {
        private readonly T _value;

        public GenericSafeAwaiter(T value) => _value = value;

        public bool IsCompleted => false;

        public void OnCompleted(Action continuation)
        {
            Tracker<T>.Value = _value;
            Interlocked.Increment(ref Tracker<T>.Completions);
            ThreadPool.QueueUserWorkItem(static state => ((Action)state!).Invoke(), continuation);
        }

        public T GetResult() => _value;
    }

    private readonly struct GenericUnsafeAwaitable<T>
    {
        private readonly T _value;

        public GenericUnsafeAwaitable(T value) => _value = value;

        public GenericUnsafeAwaiter<T> GetAwaiter() => new GenericUnsafeAwaiter<T>(_value);
    }

    private readonly struct GenericUnsafeAwaiter<T> : ICriticalNotifyCompletion
    {
        private readonly T _value;

        public GenericUnsafeAwaiter(T value) => _value = value;

        public bool IsCompleted => false;

        public void OnCompleted(Action continuation) => throw new InvalidOperationException();

        public void UnsafeOnCompleted(Action continuation)
        {
            Tracker<T>.Value = _value;
            Interlocked.Increment(ref Tracker<T>.Completions);
            ThreadPool.UnsafeQueueUserWorkItem(static state => ((Action)state!).Invoke(), continuation);
        }

        public T GetResult() => _value;
    }
}
