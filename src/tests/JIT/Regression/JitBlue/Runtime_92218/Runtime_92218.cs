// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

public struct MutableStruct
{
    private long _internalValue;

    public long InternalValue
    {
        get => Volatile.Read(ref _internalValue);
        private set => Volatile.Write(ref _internalValue, value);
    }

    public void Add(long value) => AddInternal(value);
    private void AddInternal(long value) => InternalValue += value;
    public MutableStruct(long value) => InternalValue = value;
}

public static class Runtime_92218
{
    [Fact]
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void Problem()
    {
        var test = new MutableStruct(420);
        var from = new MutableStruct(42);

        var wrapper = -new TimeSpan(3);

        while (test.InternalValue >= from.InternalValue)
        {
            test.Add(wrapper.Ticks);
        }
    }
}