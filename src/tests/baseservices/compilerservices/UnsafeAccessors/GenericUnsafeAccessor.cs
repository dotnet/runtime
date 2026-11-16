// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class GenericUnsafeAccessor
{
    private struct ValueHolder<T>
    {
        public T Value { get; init; }
    }

    private static class Accessor<T>
    {
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_" + nameof(ValueHolder<T>.Value))]
        public static extern void SetValue(ref ValueHolder<T> holder, T value);
    }

    private delegate void Setter<T>(ref ValueHolder<T> holder, T value);

    [Fact]
    public static void TestEntryPoint()
    {
        var expected = new DateTimeOffset(2026, 11, 16, 12, 34, 56, TimeSpan.FromHours(2));
        Setter<DateTimeOffset> setter = typeof(Accessor<DateTimeOffset>)
            .GetMethod(nameof(Accessor<DateTimeOffset>.SetValue))
            .CreateDelegate<Setter<DateTimeOffset>>();
        ValueHolder<DateTimeOffset> holder = default;
        setter(ref holder, expected);
        Assert.Equal(expected, holder.Value);

        holder = default;
        Accessor<DateTimeOffset>.SetValue(ref holder, expected);
        Assert.Equal(expected, holder.Value);
    }
}
