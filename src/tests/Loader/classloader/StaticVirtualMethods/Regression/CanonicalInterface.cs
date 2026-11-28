// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

public class CanonicalInterface
{
    private interface IArgument<T> { }
    private sealed class Argument<T> : IArgument<T> { }

    private interface IConstrainedResult<T>
    {
        static abstract Type Get<TArg>() where TArg : IArgument<T>;
    }

    private readonly struct ConstrainedResult<T> : IConstrainedResult<T>
    {
        public static Type Get<TArg>() where TArg : IArgument<T> => typeof(TArg);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type CallWithConstraint<TFunc, T, TArg>()
        where TFunc : IConstrainedResult<T>
        where TArg : IArgument<T>
        => TFunc.Get<TArg>();

    private interface IResult<T>
    {
        static virtual int Get() => -1;
    }

    private interface IDefaultOwner<T>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static virtual Type Owner() => typeof(T);
    }

    private readonly struct TwoDefaultOwners : IDefaultOwner<object>, IDefaultOwner<string> { }

    private readonly struct OneDefaultOwner : IDefaultOwner<object>, IDefaultOwner<string>
    {
        static Type IDefaultOwner<string>.Owner() => typeof(int);
    }

    // TUnused and T canonicalize alike. The hidden argument must come from IDefaultOwner<T>.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type ReadDefaultOwner<TFunc, TUnused, T>() where TFunc : struct, IDefaultOwner<T> => TFunc.Owner();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Func<Type> GetDefaultOwner<TFunc, T>() where TFunc : struct, IDefaultOwner<T> => TFunc.Owner;

    private readonly struct SameResult : IResult<object>, IResult<string>, IResult<int>
    {
        public static int Get() => 42;
        static int IResult<int>.Get() => 43;
    }

    private readonly struct DifferentResults : IResult<object>, IResult<string>
    {
        static int IResult<object>.Get() => 1;
        static int IResult<string>.Get() => 2;
    }

    private readonly struct PartialObjectResult : IResult<object>, IResult<string>
    {
        static int IResult<object>.Get() => 3;
    }

    private readonly struct PartialStringResult : IResult<object>, IResult<string>
    {
        static int IResult<string>.Get() => 4;
    }

    private sealed class SameResultClass<T> : IResult<T[]>, IResult<List<T>>
    {
        public static int Get() => typeof(T) == typeof(object) ? 5 : 6;
    }

    private class ResultBase<T> : IResult<T[]>, IResult<List<T>>
    {
        public static int Get() => typeof(T) == typeof(object) ? 8 : 9;
    }

    private sealed class InheritedResult<TUnused, T> : ResultBase<T> { }

    private sealed class FixedInheritedResult<T> : ResultBase<string> { }

    private sealed class ReimplementedResult<T> : ResultBase<T>, IResult<List<T>>
    {
        static int IResult<List<T>>.Get() => 10;
    }

    private class ConstantResult : IResult<object>, IResult<string>
    {
        public static int Get() => 42;
    }

    private sealed class InheritedConstantResult<T> : ConstantResult { }

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

    private class CovariantBase : ICovariantFunc<object>
    {
        public static Type GetArgument() => typeof(object);
    }

    private sealed class CovariantDerived<T> : CovariantBase, ICovariantFunc<string>
    {
        static Type ICovariantFunc<string>.GetArgument() => typeof(T);
    }

    private sealed class CrossFamilyFunc<T> : ICovariantFunc<T[]>, ICovariantFunc<string>
    {
        static Type ICovariantFunc<T[]>.GetArgument() => typeof(T[]);
        static Type ICovariantFunc<string>.GetArgument() => typeof(string);
    }

    private sealed class CrossFamilyAgreeFunc<T> : ICovariantFunc<T[]>, ICovariantFunc<string>
    {
        public static Type GetArgument() => typeof(T);
    }

    private sealed class CrossFamilyDefaultFunc<T> : ICovariantFunc<T[]>, ICovariantFunc<string> { }

    private interface IContravariantFunc<in T>
    {
        static virtual Type GetInput() => typeof(T);
    }

    private sealed class ContravariantFunc<T> : IContravariantFunc<object>
    {
        public static Type GetInput() => typeof((T, object));
    }

    private sealed class ContravariantDefaultFunc<T> : IContravariantFunc<object> { }

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

    private class GenericClassBase<T> : IGenericFunc<(T, int)>
    {
        public Type GetInstanceArgument<TArg>((T, int) value) => typeof((T, TArg));
        public static Type GetArgument<TArg>((T, int) value) => typeof((T, TArg));
    }

    private sealed class InheritedGenericClass<T> : GenericClassBase<List<T>> { }

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
    private static int CallConvergent<TFunc, T>() where TFunc : struct, IResult<T>
    {
        return TFunc.Get();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CallResult<TFunc, T>() where TFunc : struct, IResult<T> => TFunc.Get();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CallClassResult<TFunc, T>() where TFunc : class, IResult<T> => TFunc.Get();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<int> GetResultDelegate<TFunc, T>() where TFunc : IResult<T> => TFunc.Get;

    // One shared generic context per instantiation flavor, covering the convergent,
    // inherited, reimplemented and constant class results plus the delegate form.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CheckInheritedResults<TUnused, T>(int convergent, int inherited, int reimplemented, int delegated)
    {
        Assert.Equal(convergent, CallClassResult<SameResultClass<T>, T[]>() + CallClassResult<SameResultClass<T>, List<T>>());
        Assert.Equal(inherited, CallClassResult<InheritedResult<TUnused, T>, T[]>() + CallClassResult<InheritedResult<TUnused, T>, List<T>>());
        Assert.Equal(9, CallClassResult<FixedInheritedResult<T>, string[]>());
        Assert.Equal(reimplemented, CallClassResult<ReimplementedResult<T>, T[]>() * 100 + CallClassResult<ReimplementedResult<T>, List<T>>());
        Assert.Equal(42, CallClassResult<InheritedConstantResult<T>, object>());
        Assert.Equal(delegated, GetResultDelegate<InheritedResult<string, T>, T[]>()());
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
    private static void CheckInterfaceGeneric<TArg>((object, int) value)
    {
        Assert.Equal(typeof(TArg), CallClassGeneric<IGenericDefaultFunc, (object, int), TArg>(value));
        Assert.Equal(typeof(TArg), GetClassGenericDelegate<IGenericDefaultFunc, (object, int), TArg>()(value));
    }

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
    private static void CheckSharedClasses<T>((T, int) state, int expected)
    {
        Assert.Equal(expected, CallClass<SharedClassFunc<T>, (T, int)>(state));
        Assert.Equal(expected, CallClass<InheritedSharedClassFunc<T>, (T, int)>(state));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CheckMultipleClasses<T>((object, int) objectState, (string, int) stringState, int objectExpected, int stringExpected)
    {
        Assert.Equal(objectExpected, CallClass<MultipleClassFunc<T>, (object, int)>(objectState));
        Assert.Equal(stringExpected, CallClass<MultipleClassFunc<T>, (string, int)>(stringState));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Func<T, int> GetClassDelegate<TFunc, T>() where TFunc : class, IFunc<T>
        => TFunc.Invoke;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Type CallClassCovariant<TFunc, T>() where TFunc : class, ICovariantFunc<T>
        => TFunc.GetArgument();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Type CallClassGeneric<TFunc, T, TArg>(T value) where TFunc : class, IGenericFunc<T>
        => TFunc.GetArgument<TArg>(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type CallInheritedGeneric<T, TArg>() =>
        CallClassGeneric<InheritedGenericClass<T>, (List<T>, int), TArg>(default);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Type CallClassContravariant<TFunc, T>() where TFunc : class, IContravariantFunc<T>
        => TFunc.GetInput();

    // One shared generic context per instantiation, covering the variant hierarchy and the
    // cross-family shapes. The disagreeing cross-family results assert consistency with the
    // exact compilation, passed in from the caller's exact context. The winner follows the
    // first-variant-match-in-interface-order rule, specified for the instance variance pass
    // and applied to statics by analogy, so nothing here encodes the order.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CheckVariantFamilies<T>(Type crossFamily, Type crossFamilyDefault)
    {
        Assert.Equal(typeof(T), CallClassCovariant<CovariantDerived<T>, object>());
        Assert.Equal(crossFamily, CallClassCovariant<CrossFamilyFunc<T>, object>());
        Assert.Equal(crossFamilyDefault, CallClassCovariant<CrossFamilyDefaultFunc<T>, object>());
        Assert.Equal(typeof(T), CallClassCovariant<CrossFamilyAgreeFunc<T>, object>());
        Assert.Equal(typeof((T, object)), CallClassContravariant<ContravariantFunc<T>, string>());
        Assert.Equal(typeof(object), CallClassContravariant<ContravariantDefaultFunc<T>, string>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(42)]
    [InlineData(-17)]
    public static void TestEntryPoint(int value)
    {
        // Bind the caller's method arguments before checking constraints on a selected target.
        Assert.Equal(typeof(Argument<short>), CallWithConstraint<ConstrainedResult<short>, short, Argument<short>>());
        Assert.Equal(typeof(Argument<string>), CallWithConstraint<ConstrainedResult<string>, string, Argument<string>>());
        (object, int) objectState = (new object(), value);
        (string, int) stringState = ("state", value);

        CheckInterfaceInstantiations<object>(objectState);
        CheckInterfaceInstantiations<string>(objectState);

        CheckInterfaceGeneric<int>(objectState);
        CheckInterfaceGeneric<object>(objectState);
        CheckInterfaceGeneric<string>(objectState);

        Assert.Equal(typeof(string), ReadDefaultOwner<TwoDefaultOwners, object, string>());
        Assert.Equal(typeof(object), ReadDefaultOwner<TwoDefaultOwners, string, object>());
        Assert.Equal(typeof(int), ReadDefaultOwner<OneDefaultOwner, object, string>());
        Assert.Equal(typeof(object), ReadDefaultOwner<OneDefaultOwner, string, object>());
        Assert.Equal(typeof(string), GetDefaultOwner<TwoDefaultOwners, string>()());
        Assert.Equal(typeof(object), GetDefaultOwner<TwoDefaultOwners, object>()());
        Assert.Equal(typeof(int), GetDefaultOwner<OneDefaultOwner, string>()());

        Assert.Equal(42, CallConvergent<SameResult, object>());
        Assert.Equal(42, CallConvergent<SameResult, string>());
        Assert.Equal(43, CallConvergent<SameResult, int>());
        Assert.Equal(1, CallResult<DifferentResults, object>());
        Assert.Equal(2, CallResult<DifferentResults, string>());
        Assert.Equal(3, CallResult<PartialObjectResult, object>());
        Assert.Equal(-1, CallResult<PartialObjectResult, string>());
        Assert.Equal(-1, CallResult<PartialStringResult, object>());
        Assert.Equal(4, CallResult<PartialStringResult, string>());

        CheckInheritedResults<string, object>(10, 16, 810, 8);
        CheckInheritedResults<object, string>(12, 18, 910, 9);
        CheckInheritedResults<string, object>(10, 16, 810, 8);
        Assert.Equal(typeof((List<object>, string)), CallInheritedGeneric<object, string>());
        Assert.Equal(typeof((List<string>, object)), CallInheritedGeneric<string, object>());
        Assert.Equal(typeof((List<string>, int)), CallInheritedGeneric<string, int>());

        CheckVariantFamilies<string>(
            CallClassCovariant<CrossFamilyFunc<string>, object>(),
            CallClassCovariant<CrossFamilyDefaultFunc<string>, object>());
        CheckVariantFamilies<Uri>(
            CallClassCovariant<CrossFamilyFunc<Uri>, object>(),
            CallClassCovariant<CrossFamilyDefaultFunc<Uri>, object>());
        CheckVariantFamilies<int>(
            CallClassCovariant<CrossFamilyFunc<int>, object>(),
            CallClassCovariant<CrossFamilyDefaultFunc<int>, object>());

        Assert.Equal(value + 10, CallClass<ClassFunc, (object, int)>(objectState));
        Assert.Equal(value + 10, CallClass<InheritedClassFunc, (object, int)>(objectState));
        Assert.Equal(value + 20, CallClass<ReimplementedClassFunc, (object, int)>(objectState));
        CheckSharedClasses(objectState, value + 30);
        CheckSharedClasses(stringState, value + 40);
        CheckMultipleClasses<object>(objectState, stringState, value + 1, value + 2);
        CheckMultipleClasses<string>(objectState, stringState, value + 3, value + 4);
        CheckMultipleClasses<object>(objectState, stringState, value + 1, value + 2);
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
