// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class CanonicalInterface
{
    private interface IFunc<T>
    {
        int InvokeInstance(T value);
        static abstract int Invoke(T value);
    }

    private readonly struct TupleFunc : IFunc<(object, int)>
    {
        public int InvokeInstance((object, int) value) => value.Item2;
        public static int Invoke((object, int) value) => value.Item2;
    }

    private readonly struct MultipleFunc : IFunc<(object, int)>, IFunc<(string, int)>
    {
        int IFunc<(object, int)>.InvokeInstance((object, int) value) => value.Item2 + 1;
        int IFunc<(string, int)>.InvokeInstance((string, int) value) => value.Item2 + 2;
        static int IFunc<(object, int)>.Invoke((object, int) value) => value.Item2 + 1;
        static int IFunc<(string, int)>.Invoke((string, int) value) => value.Item2 + 2;
    }

    private readonly struct SharedFunc<T> : IFunc<(T, int)>
    {
        public int InvokeInstance((T, int) value) => Invoke(value);
        public static int Invoke((T, int) value) => value.Item2 + (typeof(T) == typeof(object) ? 3 : 4);
    }

    private readonly struct FixedStateFunc<T> : IFunc<(object, int)>
    {
        public int InvokeInstance((object, int) value) => Invoke(value);
        public static int Invoke((object, int) value) => value.Item2 + (typeof(T) == typeof(object) ? 8 : 9);
    }

    private readonly struct ExactGenericFunc<T> : IFunc<(object, int)>
    {
        public int InvokeInstance((object, int) value) => Invoke(value);
        public static int Invoke((object, int) value) => value.Item2 + (typeof(T) == typeof(int) ? 5 : 6);
    }

    private interface IDefaultFunc<T> : IFunc<T>
    {
        int IFunc<T>.InvokeInstance(T value) => 7;
        static int IFunc<T>.Invoke(T value) => 7;
    }

    private readonly struct DefaultFunc : IDefaultFunc<(object, int)> { }

    private interface ICovariantFunc<out T>
    {
        static virtual Type GetArgument() => typeof(T);
    }

    private readonly struct CovariantFunc : ICovariantFunc<string> { }

    private readonly struct MultipleDefaultFunc : ICovariantFunc<string>, ICovariantFunc<object> { }

    private readonly struct GenericDefaultFunc<T> : ICovariantFunc<T> { }

    private interface IGenericFunc<T>
    {
        Type GetInstanceArgument<TArg>(T value);
        static abstract Type GetArgument<TArg>(T value);
    }

    private readonly struct GenericFunc : IGenericFunc<(object, int)>
    {
        public Type GetInstanceArgument<TArg>((object, int) value) => typeof(TArg);
        public static Type GetArgument<TArg>((object, int) value) => typeof(TArg);
    }

    private readonly struct SharedGenericFunc<T> : IGenericFunc<(T, int)>
    {
        public Type GetInstanceArgument<TArg>((T, int) value) => typeof((T, TArg));
        public static Type GetArgument<TArg>((T, int) value) => typeof((T, TArg));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type CallCovariant<TFunc, T>() where TFunc : struct, ICovariantFunc<T>
        => TFunc.GetArgument();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type CallGeneric<TFunc, T, TArg>(T value) where TFunc : struct, IGenericFunc<T>
        => TFunc.GetArgument<TArg>(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type CallGenericInstance<TFunc, T, TArg>(TFunc func, T value) where TFunc : struct, IGenericFunc<T>
        => func.GetInstanceArgument<TArg>(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Func<T, int> GetDelegate<TFunc, T>() where TFunc : struct, IFunc<T>
        => TFunc.Invoke;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Func<T, Type> GetGenericDelegate<TFunc, T, TArg>() where TFunc : struct, IGenericFunc<T>
        => TFunc.GetArgument<TArg>;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CallUnique<TFunc, T>(T value) where TFunc : struct, IFunc<T>
    {
        // ARM64-NOT: {{bl(r)?[[:space:]]}}
        // ARM64: ret
        // X64-NOT: call
        // X64: ret
        return TFunc.Invoke(value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CallExactGeneric<TFunc, T>(T value) where TFunc : struct, IFunc<T>
    {
        // ARM64-NOT: {{bl(r)?[[:space:]]}}
        // ARM64: ret
        // X64-NOT: call
        // X64: ret
        return TFunc.Invoke(value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CallDefault<TFunc, T>(T value) where TFunc : struct, IFunc<T>
    {
        // ARM64-NOT: {{bl(r)?[[:space:]]}}
        // ARM64: ret
        // X64-NOT: call
        // X64: ret
        return TFunc.Invoke(value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type CallCovariantUnique<TFunc, T>() where TFunc : struct, ICovariantFunc<T>
    {
        // ARM64-NOT: {{bl(r)?[[:space:]]}}
        // ARM64: ret
        // X64-NOT: call
        // X64: ret
        return TFunc.GetArgument();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type CallGenericExact<TFunc, T, TArg>(T value) where TFunc : struct, IGenericFunc<T>
    {
        // ARM64-NOT: {{bl(r)?[[:space:]]}}
        // ARM64: ret
        // X64-NOT: call
        // X64: ret
        return TFunc.GetArgument<TArg>(value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CallInstance<TFunc, T>(TFunc func, T value) where TFunc : struct, IFunc<T>
    {
        // ARM64-NOT: {{bl(r)?[[:space:]]}}
        // ARM64: ret
        // X64-NOT: call
        // X64: ret
        return func.InvokeInstance(value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CallOther<TFunc, T>(T value) where TFunc : struct, IFunc<T>
        => TFunc.Invoke(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CallInstanceOther<TFunc, T>(TFunc func, T value) where TFunc : struct, IFunc<T>
        => func.InvokeInstance(value);

    [Theory]
    [InlineData(0)]
    [InlineData(42)]
    [InlineData(-17)]
    public static void TestEntryPoint(int value)
    {
        (object, int) objectState = (new object(), value);
        (string, int) stringState = ("state", value);

        Assert.Equal(value, CallUnique<TupleFunc, (object, int)>(objectState));
        Assert.Equal(value, CallInstance(default(TupleFunc), objectState));
        Assert.Equal(value + 1, CallOther<MultipleFunc, (object, int)>(objectState));
        Assert.Equal(value + 2, CallOther<MultipleFunc, (string, int)>(stringState));
        Assert.Equal(value + 1, CallOther<MultipleFunc, (object, int)>(objectState));
        Assert.Equal(value + 3, CallOther<SharedFunc<object>, (object, int)>(objectState));
        Assert.Equal(value + 4, CallOther<SharedFunc<string>, (string, int)>(stringState));
        Assert.Equal(value + 5, CallExactGeneric<ExactGenericFunc<int>, (object, int)>(objectState));
        Assert.Equal(value + 6, CallOther<ExactGenericFunc<long>, (object, int)>(objectState));
        Assert.Equal(7, CallDefault<DefaultFunc, (object, int)>(objectState));
        Assert.Equal(value + 1, CallInstanceOther(default(MultipleFunc), objectState));
        Assert.Equal(value + 2, CallInstanceOther(default(MultipleFunc), stringState));
        Assert.Equal(value + 1, CallInstanceOther(default(MultipleFunc), objectState));
        Assert.Equal(value + 3, CallInstanceOther(default(SharedFunc<object>), objectState));
        Assert.Equal(value + 4, CallInstanceOther(default(SharedFunc<string>), stringState));
        Assert.Equal(value + 5, CallInstanceOther(default(ExactGenericFunc<int>), objectState));
        Assert.Equal(value + 6, CallInstanceOther(default(ExactGenericFunc<long>), objectState));
        Assert.Equal(7, CallInstanceOther(default(DefaultFunc), objectState));
        Assert.Equal(typeof(string), CallCovariant<CovariantFunc, string>());
        Assert.Equal(typeof(string), CallCovariantUnique<CovariantFunc, object>());
        Assert.Equal(typeof(string), CallCovariant<MultipleDefaultFunc, string>());
        Assert.Equal(typeof(object), CallCovariant<MultipleDefaultFunc, object>());
        Assert.Equal(typeof(string), CallCovariant<GenericDefaultFunc<string>, string>());
        Assert.Equal(typeof(object), CallCovariant<GenericDefaultFunc<object>, object>());
        Assert.Equal(typeof(object), CallGeneric<GenericFunc, (object, int), object>(objectState));
        Assert.Equal(typeof(string), CallGeneric<GenericFunc, (object, int), string>(objectState));
        Assert.Equal(typeof(int), CallGenericExact<GenericFunc, (object, int), int>(objectState));
        Assert.Equal(typeof(object), CallGenericInstance<GenericFunc, (object, int), object>(default, objectState));
        Assert.Equal(typeof(string), CallGenericInstance<GenericFunc, (object, int), string>(default, objectState));
        Assert.Equal(typeof(int), CallGenericInstance<GenericFunc, (object, int), int>(default, objectState));
        Assert.Equal(value + 8, CallOther<FixedStateFunc<object>, (object, int)>(objectState));
        Assert.Equal(value + 9, CallOther<FixedStateFunc<string>, (object, int)>(objectState));
        Assert.Equal(value + 8, CallInstanceOther(default(FixedStateFunc<object>), objectState));
        Assert.Equal(value + 9, CallInstanceOther(default(FixedStateFunc<string>), objectState));
        Assert.Equal(typeof((object, string)), CallGeneric<SharedGenericFunc<object>, (object, int), string>(objectState));
        Assert.Equal(typeof((string, object)), CallGeneric<SharedGenericFunc<string>, (string, int), object>(stringState));
        Assert.Equal(typeof((object, string)), CallGenericInstance<SharedGenericFunc<object>, (object, int), string>(default, objectState));
        Assert.Equal(typeof((string, object)), CallGenericInstance<SharedGenericFunc<string>, (string, int), object>(default, stringState));
        Assert.Equal(value, GetDelegate<TupleFunc, (object, int)>()(objectState));
        Assert.Equal(value + 8, GetDelegate<FixedStateFunc<object>, (object, int)>()(objectState));
        Assert.Equal(value + 9, GetDelegate<FixedStateFunc<string>, (object, int)>()(objectState));
        Assert.Equal(value + 1, GetDelegate<MultipleFunc, (object, int)>()(objectState));
        Assert.Equal(value + 2, GetDelegate<MultipleFunc, (string, int)>()(stringState));
        Assert.Equal(7, GetDelegate<DefaultFunc, (object, int)>()(objectState));
        Assert.Equal(typeof(object), GetGenericDelegate<GenericFunc, (object, int), object>()(objectState));
        Assert.Equal(typeof(string), GetGenericDelegate<GenericFunc, (object, int), string>()(objectState));
        Assert.Equal(typeof((object, string)), GetGenericDelegate<SharedGenericFunc<object>, (object, int), string>()(objectState));
        Assert.Equal(typeof((string, object)), GetGenericDelegate<SharedGenericFunc<string>, (string, int), object>()(stringState));
    }
}
