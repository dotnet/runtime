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

    private struct StructFields<T>
    {
        public T First;
        public T Second;
        public T Third;
    }

    private class ClassFields<T>
    {
        public const string ValueFieldName = nameof(_value);
        private readonly T _value;
        public readonly string Label = "initial";
        public static T StaticValue;

        public ClassFields(T value) => _value = value;
    }

    private static class FieldAccessor<T>
    {
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = nameof(StructFields<T>.Second))]
        public static extern ref T GetStructField(ref StructFields<T> holder);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = ClassFields<T>.ValueFieldName)]
        public static extern ref T GetClassField(ClassFields<T> holder);

        [UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = nameof(ClassFields<T>.StaticValue))]
        public static extern ref T GetStaticField(ClassFields<T> holder);
    }

    private delegate ref T StructFieldGetter<T>(ref StructFields<T> holder);
    private delegate ref T ClassFieldGetter<T>(ClassFields<T> holder);

    [Fact]
    public static void FieldsOnGenericTypes()
    {
        VerifyFields(42, 17, 99);
        VerifyFields("first", "second", "third");
    }

    private static void VerifyFields<T>(T first, T second, T third)
    {
        var fields = new StructFields<T> { First = first, Second = second, Third = third };
        var holder = new ClassFields<T>(first);
        ClassFields<T>.StaticValue = third;

        StructFieldGetter<T> structGetter = typeof(FieldAccessor<T>)
            .GetMethod(nameof(FieldAccessor<T>.GetStructField))
            .CreateDelegate<StructFieldGetter<T>>();
        ClassFieldGetter<T> classGetter = typeof(FieldAccessor<T>)
            .GetMethod(nameof(FieldAccessor<T>.GetClassField))
            .CreateDelegate<ClassFieldGetter<T>>();
        ClassFieldGetter<T> staticGetter = typeof(FieldAccessor<T>)
            .GetMethod(nameof(FieldAccessor<T>.GetStaticField))
            .CreateDelegate<ClassFieldGetter<T>>();

        Assert.Equal(second, structGetter(ref fields));
        Assert.Equal(first, classGetter(holder));
        Assert.Equal(third, staticGetter(null));
        Assert.Equal(second, FieldAccessor<T>.GetStructField(ref fields));
        Assert.Equal(first, FieldAccessor<T>.GetClassField(holder));
        Assert.Equal(third, FieldAccessor<T>.GetStaticField(null));
        Assert.Equal("initial", holder.Label);
    }

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
