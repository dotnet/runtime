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

    private class ClassFunc : IFunc<(object, int)>
    {
        public int InvokeInstance((object, int) value) => value.Item2 + 10;
        public static int Invoke((object, int) value) => value.Item2 + 10;
    }

    private sealed class InheritedClassFunc : ClassFunc { }

    private sealed class ReimplementedClassFunc : ClassFunc, IFunc<(object, int)>
    {
        int IFunc<(object, int)>.InvokeInstance((object, int) value) => value.Item2 + 20;
        static int IFunc<(object, int)>.Invoke((object, int) value) => value.Item2 + 20;
    }

    private class SharedClassFunc<T> : IFunc<(T, int)>
    {
        public int InvokeInstance((T, int) value) => Invoke(value);
        public static int Invoke((T, int) value) => value.Item2 + (typeof(T) == typeof(object) ? 30 : 40);
    }

    private sealed class InheritedSharedClassFunc<T> : SharedClassFunc<T> { }

    private sealed class MultipleClassFunc<T> : IFunc<(object, int)>, IFunc<(string, int)>
    {
        int IFunc<(object, int)>.InvokeInstance((object, int) value) => value.Item2 + 1;
        int IFunc<(string, int)>.InvokeInstance((string, int) value) => value.Item2 + 2;
        static int IFunc<(object, int)>.Invoke((object, int) value) => value.Item2 + (typeof(T) == typeof(object) ? 1 : 3);
        static int IFunc<(string, int)>.Invoke((string, int) value) => value.Item2 + (typeof(T) == typeof(object) ? 2 : 4);
    }

    private sealed class DefaultClassFunc : IDefaultFunc<(object, int)> { }

    private interface ICovariantFunc<out T>
    {
        static virtual Type GetArgument() => typeof(T);
    }

    private readonly struct CovariantFunc : ICovariantFunc<string> { }

    private sealed class CovariantClassFunc : ICovariantFunc<string> { }
    private sealed class MultipleDefaultClassFunc : ICovariantFunc<string>, ICovariantFunc<object> { }

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

    private interface IGenericDefaultFunc : IGenericFunc<(object, int)>
    {
        Type IGenericFunc<(object, int)>.GetInstanceArgument<TArg>((object, int) value) => typeof(TArg);
        static Type IGenericFunc<(object, int)>.GetArgument<TArg>((object, int) value) => typeof(TArg);
    }

    private interface IStatefulGenericDefaultFunc<T> : IGenericFunc<(object, int)>
    {
        Type IGenericFunc<(object, int)>.GetInstanceArgument<TArg>((object, int) value) => typeof((T, TArg));
        static Type IGenericFunc<(object, int)>.GetArgument<TArg>((object, int) value) => typeof((T, TArg));
    }

    private interface IInheritedGenericDefault<T, U> : IStatefulGenericDefaultFunc<U> { }
    private interface IClosedGenericDefault : IInheritedGenericDefault<object, string> { }

    private interface IReimplementedGenericDefault<T> : IStatefulGenericDefaultFunc<T>
    {
        static Type IGenericFunc<(object, int)>.GetArgument<TArg>((object, int) value) => typeof((TArg, T));
    }

    private sealed class GenericClassFunc : IGenericFunc<(object, int)>
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
        return TFunc.Invoke(value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CallExactGeneric<TFunc, T>(T value) where TFunc : struct, IFunc<T>
    {
        return TFunc.Invoke(value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CallDefault<TFunc, T>(T value) where TFunc : struct, IFunc<T>
    {
        return TFunc.Invoke(value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type CallCovariantUnique<TFunc, T>() where TFunc : struct, ICovariantFunc<T>
    {
        return TFunc.GetArgument();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type CallGenericExact<TFunc, T, TArg>(T value) where TFunc : struct, IGenericFunc<T>
    {
        return TFunc.GetArgument<TArg>(value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CallInstance<TFunc, T>(TFunc func, T value) where TFunc : struct, IFunc<T>
    {
        return func.InvokeInstance(value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CallOther<TFunc, T>(T value) where TFunc : struct, IFunc<T>
        => TFunc.Invoke(value);

    // Inlining exposes an interface, rather than a class or struct, as the constrained type.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type CallInterfaceGeneric<TArg>((object, int) value)
        => CallClassGeneric<IGenericDefaultFunc, (object, int), TArg>(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Func<(object, int), Type> GetInterfaceGenericDelegate<TArg>()
        => GetClassGenericDelegate<IGenericDefaultFunc, (object, int), TArg>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<T, Type> GetClassGenericDelegate<TFunc, T, TArg>() where TFunc : class, IGenericFunc<T>
        => TFunc.GetArgument<TArg>;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CheckInterfaceInstantiations<T>((object, int) value)
    {
        Assert.Equal(typeof((string, int)), CallClassGeneric<IClosedGenericDefault, (object, int), int>(value));
        Assert.Equal(typeof((string, string)), GetClassGenericDelegate<IClosedGenericDefault, (object, int), string>()(value));
        Assert.Equal(typeof((int, T)), CallClassGeneric<IReimplementedGenericDefault<T>, (object, int), int>(value));
        Assert.Equal(typeof((string, T)), CallClassGeneric<IReimplementedGenericDefault<T>, (object, int), string>(value));
        Assert.Equal(typeof((int, T)), GetClassGenericDelegate<IReimplementedGenericDefault<T>, (object, int), int>()(value));
        Assert.Equal(typeof((string, T)), GetClassGenericDelegate<IReimplementedGenericDefault<T>, (object, int), string>()(value));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CallInstanceOther<TFunc, T>(TFunc func, T value) where TFunc : struct, IFunc<T>
        => func.InvokeInstance(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CallClass<TFunc, T>(T value) where TFunc : class, IFunc<T>
        => TFunc.Invoke(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CallSharedClass<T>((T, int) value)
        => CallClass<SharedClassFunc<T>, (T, int)>(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CallInheritedSharedClass<T>((T, int) value)
        => CallClass<InheritedSharedClassFunc<T>, (T, int)>(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CallMultipleClassObject<T>((object, int) value)
        => CallClass<MultipleClassFunc<T>, (object, int)>(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CallMultipleClassString<T>((string, int) value)
        => CallClass<MultipleClassFunc<T>, (string, int)>(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Func<T, int> GetClassDelegate<TFunc, T>() where TFunc : class, IFunc<T>
        => TFunc.Invoke;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Type CallClassCovariant<TFunc, T>() where TFunc : class, ICovariantFunc<T>
        => TFunc.GetArgument();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Type CallClassGeneric<TFunc, T, TArg>(T value) where TFunc : class, IGenericFunc<T>
        => TFunc.GetArgument<TArg>(value);

    [Theory]
    [InlineData(0)]
    [InlineData(42)]
    [InlineData(-17)]
    public static void TestEntryPoint(int value)
    {
        (object, int) objectState = (new object(), value);
        (string, int) stringState = ("state", value);

        CheckInterfaceInstantiations<object>(objectState);
        CheckInterfaceInstantiations<string>(objectState);

        Assert.Equal(typeof(int), CallInterfaceGeneric<int>(objectState));
        Assert.Equal(typeof(object), CallInterfaceGeneric<object>(objectState));
        Assert.Equal(typeof(string), CallInterfaceGeneric<string>(objectState));
        Assert.Equal(typeof(int), GetInterfaceGenericDelegate<int>()(objectState));
        Assert.Equal(typeof(object), GetInterfaceGenericDelegate<object>()(objectState));
        Assert.Equal(typeof(string), GetInterfaceGenericDelegate<string>()(objectState));

        Assert.Equal(value + 10, CallClass<ClassFunc, (object, int)>(objectState));
        Assert.Equal(value + 10, CallClass<InheritedClassFunc, (object, int)>(objectState));
        Assert.Equal(value + 20, CallClass<ReimplementedClassFunc, (object, int)>(objectState));
        Assert.Equal(value + 30, CallSharedClass(objectState));
        Assert.Equal(value + 40, CallSharedClass(stringState));
        Assert.Equal(value + 30, CallInheritedSharedClass(objectState));
        Assert.Equal(value + 40, CallInheritedSharedClass(stringState));
        Assert.Equal(value + 1, CallMultipleClassObject<object>(objectState));
        Assert.Equal(value + 2, CallMultipleClassString<object>(stringState));
        Assert.Equal(value + 3, CallMultipleClassObject<string>(objectState));
        Assert.Equal(value + 4, CallMultipleClassString<string>(stringState));
        Assert.Equal(value + 1, CallMultipleClassObject<object>(objectState));
        Assert.Equal(7, CallClass<DefaultClassFunc, (object, int)>(objectState));
        Assert.Equal(value + 10, GetClassDelegate<InheritedClassFunc, (object, int)>()(objectState));
        Assert.Equal(value + 20, GetClassDelegate<ReimplementedClassFunc, (object, int)>()(objectState));
        Assert.Equal(value + 30, GetClassDelegate<SharedClassFunc<object>, (object, int)>()(objectState));
        Assert.Equal(value + 40, GetClassDelegate<SharedClassFunc<string>, (string, int)>()(stringState));
        Assert.Equal(7, GetClassDelegate<DefaultClassFunc, (object, int)>()(objectState));
        Assert.Equal(typeof(string), CallClassCovariant<CovariantClassFunc, object>());
        Assert.Equal(typeof(string), CallClassCovariant<MultipleDefaultClassFunc, string>());
        Assert.Equal(typeof(object), CallClassCovariant<MultipleDefaultClassFunc, object>());
        Assert.Equal(typeof(string), CallClassGeneric<GenericClassFunc, (object, int), string>(objectState));
        Assert.Equal(typeof(object), CallClassGeneric<GenericClassFunc, (object, int), object>(objectState));

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
