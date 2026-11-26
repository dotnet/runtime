// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

public class ForwardedMethodTypeArgument
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Type Inner<X>() => typeof(X);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type Outer<T, U>(bool first) where T : class where U : class
        => first ? Inner<T>() : Inner<U>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Type Mixed<X>(bool array) => array ? typeof(X[]) : typeof(X);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type OuterMixed<T, U>(bool array) => Mixed<U>(array);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type OuterArray<T, U>() => Inner<U[]>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Type Middle<A, B>() => Inner<B>();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type Swapped<T, U>() => Middle<U, T>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Type Pick<A, B, C>(bool first) => first ? typeof(A) : Inner<C>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Type Repeat<A, B>(bool first) => Pick<B, B, A>(first);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type ThreeLevels<T, U>(bool first) => Repeat<U, T>(first);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type Constructed<T, U>() => Middle<U, T[]>();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type Kept<X>() => typeof(X);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Type MiddleKept<A, B>() => Kept<B>();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type NestedKept<T, U>() => MiddleKept<U, T>();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type Recursive<T, U>(int depth) => depth == 0 ? typeof(U) : Recursive<T, U>(depth - 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Type ArrayOfSecond<A, B>() => Inner<B[]>();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type NestedArray<T, U>() => ArrayOfSecond<U, T>();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type Jagged<T, U>(bool wrap) => Mixed<U[]>(wrap);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type Rectangular<T, U>() => Inner<U[,]>();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type GenericWrapper<T, U>() => Inner<List<U[]>>();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type Pair<T, U>() => ArrayOfSecond<T, Dictionary<U, T>>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Type Compose<A, B>() => Inner<KeyValuePair<B, List<A[]>>>();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type Named<T, U>() => Compose<U, T>();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type Compound<T>() => Inner<Dictionary<string, T[]>>();

    private class PairType<T, U> { }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type CrossModule<T, U>() => ForwardedArgumentBridge.Wrap<PairType<T, U>>();

    private class Owner<T>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Type Forward<U>(bool owner) => owner ? Inner<T>() : Inner<U>();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Type Both<U>() => Inner<Dictionary<T, U>>();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Type Swap<U>() => CalleeOwner<U>.Pair<T>();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Type Nested<U>() => CalleeOwner<U>.Relay<T>();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Type InstanceElement() => Inner<T>();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Type InstanceOwner() => Inner<Owner<T>>();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Type FromClassContext() => Inner<List<T[]>>();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Type KeptFromThis() => Kept<T[]>();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Type OwnerOnly() => CalleeOwner<List<T>>.Get();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Type OwnerRelay() => CalleeOwner<List<T>>.GetArray();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Type OwnerKept() => CalleeOwner<List<T>>.GetKept();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Type Initialized() => InitializedOwner<T>.Get();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int ReadCache() => CalleeOwner<List<T>>.ReadCache();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void WriteCache(int value) => CalleeOwner<List<T>>.WriteCache(value);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ref int CacheAddress() => ref CalleeOwner<List<T>>.CacheAddress();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Type CacheTag() => CalleeOwner<List<T>>.CacheTag();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static object Allocate() => CalleeOwner<List<T>>.Allocate();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Array Vector(int length) => CalleeOwner<List<T>>.Vector(length);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Array DirectVector(int length) => new List<T>[length];

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Array Jagged(int length) => CalleeOwner<T[]>.Vector(length);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool Test(object value) => CalleeOwner<Box<List<T>>>.Test(value);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TestNullable(object value) => CalleeOwner<KeyValuePair<T, int>?>.Test(value);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static object CheckedCast(object value) => CalleeOwner<List<T>>.CheckedCast(value);
    }

    private sealed class DerivedOwner<P, Q> : Owner<Q> { }

    private interface IContext<T>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        Type GetContext() => Inner<List<T[]>>();
    }

    private sealed class TwoContexts : IContext<string>, IContext<object> { }

    private struct ValueOwner<T>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Type Instance() => Inner<List<T[]>>();
    }

    private static int s_typeInitializers;

    private class InitializedOwner<T>
    {
        static InitializedOwner() => s_typeInitializers++;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type Get() => typeof(T);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type NamedMethodContext<T>() => ForwardedArgumentBridge.Named<List<T>>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Type FixedArgument<A>() => MethodOwner<TimeSpan>.Kept<KeyValuePair<A, int>>();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type FixedOwner<T>() => FixedArgument<List<T>>();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type KeptPair<A, B>() => typeof(KeyValuePair<A, B>);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Type MixedArguments<A>() => KeptPair<A, int>();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type FixedPrimitive<T>() => MixedArguments<T[]>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<Type> MethodEntry<A, B>() => new Func<Type>(MethodOwner<A>.Kept<B>);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Func<Type> NestedEntry<T, U>() => MethodEntry<U[], List<T>>();

    private class MethodOwner<A>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Type Kept<B>() => typeof(Dictionary<A, B>);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type Relay<B>() => MethodOwner<B[]>.Kept<List<A>>();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Type Outer<B>() => MethodOwner<B>.Relay<A>();
    }

    private struct MethodValueOwner<A>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Type Kept<B>() => typeof(Dictionary<A, B>);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Type Relay<B>() => Kept<B[]>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type StructMethodContext<T, U>() => default(MethodValueOwner<U>).Relay<T>();

    private static int s_cacheInitializers;

    private static class Cache<A>
    {
        public static int Value;
        public static Type Tag;

        static Cache()
        {
            s_cacheInitializers++;
            Value = 17;
            Tag = typeof(A);
        }
    }

    private class CalleeOwner<A>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadCache() => Cache<A>.Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteCache(int value) => Cache<A>.Value = value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref int CacheAddress() => ref Cache<A>.Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type CacheTag() => Cache<A>.Tag;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type Get() => typeof(A);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type GetArray() => Inner<A[]>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object Allocate() => new Box<A>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Array Vector(int length) => new A[length];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Test(object value) => value is A;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object CheckedCast(object value) => (Box<A>)value;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Type GetKept() => typeof(A);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type Pair<B>() => typeof(Dictionary<A, B>);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type Relay<B>() => CalleeOwner<List<B[]>>.Pair<A>();
    }

    private sealed class Box<T>
    {
        public Box() => s_constructions++;
    }

    private static int s_constructions;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object Make<T>() => new Box<T>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsBox<U>(object value) => value is Box<U>;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Same<T>() => IsBox<T>(Make<T>());

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Other<T, U>() => IsBox<U>(Make<T>());

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Observe(object value) => GC.KeepAlive(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Copied<T>()
    {
        object value = Make<T>();
        Observe(value);
        return IsBox<T>(value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Choose<T, U>(bool first) => IsBox<T>(first ? Make<T>() : Make<U>());

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Clear(ref object value) => value = null;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Cleared<T>()
    {
        object value = Make<T>();
        Clear(ref value);
        return IsBox<T>(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Box<T> Cast<T>(object value) => (Box<T>)value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object SiblingCast<T, U>() => Cast<U>(Make<T>());

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool RootTest<T>() => Make<T>() is Box<T>;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object RootCast<T>() => (Box<T>)Make<T>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object MakeOther<T>() { s_constructions++; return new Box<T>(); }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Agree<T>(bool first) => IsBox<T>(first ? Make<T>() : MakeOther<T>());

    private class Receiver<A, B>
    {
        internal readonly Type Constructed;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Receiver() => Constructed = typeof(KeyValuePair<A, B>);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Type Read() => Kept<List<B>>();

        public virtual Type Select<K>() => typeof(Tuple<A, B, K>);
    }

    private sealed class SwappedReceiver<A, B> : Receiver<B, B>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override Type Select<K>() => typeof(Tuple<A, B, K>);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type FromReceiver<T>(Receiver<int, T> value) => value.Read();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type FromConstructor<T>() => new Receiver<int, T>().Constructed;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type FromOverride<T>(SwappedReceiver<int, long> value) => value.Select<T[]>();

    private class Reader<T> { public virtual int Read() => 37; }
    private sealed class OtherReader<T> : Reader<T> { public override int Read() => 13; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object MakeReader<T>() => new Reader<T>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Read<T>(object value) => ((Reader<T>)value).Read();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int KnownReceiver<T>() => Read<T>(MakeReader<T>());

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int UnknownReceiver<T>(object value) => Read<T>(value);

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(typeof(List<string>), FromReceiver(new Receiver<int, string>()));
        Assert.Equal(typeof(List<object>), FromReceiver(new Receiver<int, object>()));
        Assert.Throws<NullReferenceException>(() => FromReceiver<string>(null));
        Assert.Equal(typeof(KeyValuePair<int, string>), FromConstructor<string>());
        Assert.Equal(typeof(Tuple<int, long, string[]>), FromOverride<string>(new SwappedReceiver<int, long>()));
        Assert.Equal(typeof(Tuple<int, long, object[]>), FromOverride<object>(new SwappedReceiver<int, long>()));
        var firstEntry = NestedEntry<string, object>();
        var secondEntry = NestedEntry<object, string>();
        Assert.Equal(typeof(Dictionary<object[], List<string>>), firstEntry());
        Assert.Equal(typeof(Dictionary<string[], List<object>>), secondEntry());
        Assert.Equal(typeof(Dictionary<object[], List<string>>), firstEntry());
        Assert.Equal(typeof(Dictionary<TimeSpan, KeyValuePair<List<string>, int>>), FixedOwner<string>());
        Assert.Equal(typeof(Dictionary<TimeSpan, KeyValuePair<List<object>, int>>), FixedOwner<object>());
        Assert.Equal(typeof(KeyValuePair<string[], int>), FixedPrimitive<string>());
        Assert.Equal(typeof(Action<List<string>, TimeSpan, ForwardedArgumentMode, ForwardedArgumentTag>), NamedMethodContext<string>());
        Assert.Equal(typeof(Action<List<object>, TimeSpan, ForwardedArgumentMode, ForwardedArgumentTag>), NamedMethodContext<object>());
        Assert.Equal(typeof(Dictionary<string[], List<object>>), MethodOwner<string>.Outer<object>());
        Assert.Equal(typeof(Dictionary<object[], List<string>>), MethodOwner<object>.Outer<string>());
        Assert.Equal(typeof(Dictionary<object, string[]>), StructMethodContext<string, object>());
        Assert.Equal(typeof(Dictionary<string, object[]>), StructMethodContext<object, string>());

        int before = s_constructions;
        Assert.True(Same<string>());
        Assert.True(Copied<string>());
        Assert.True(Other<string, string>());
        Assert.False(Other<string, object>());
        Assert.True(Choose<string, object>(true));
        Assert.False(Choose<string, object>(false));
        Assert.False(Cleared<string>());
        Assert.Equal(before + 7, s_constructions);

        Assert.True(RootTest<string>());
        Assert.True(RootCast<string>() is Box<string>);
        Assert.True(SiblingCast<string, string>() is Box<string>);
        Assert.Throws<InvalidCastException>(() => SiblingCast<string, object>());
        Assert.True(Agree<string>(true));
        Assert.True(Agree<string>(false));
        Assert.Equal(before + 14, s_constructions);
        Assert.Equal(37, KnownReceiver<string>());
        Assert.Equal(13, UnknownReceiver<string>(new OtherReader<string>()));

        Assert.Equal(typeof(string), Outer<string, object>(true));
        Assert.Equal(typeof(object), Outer<string, object>(false));
        Assert.Equal(typeof(object), Outer<object, string>(true));
        Assert.Equal(typeof(string), Outer<object, string>(false));
        Assert.Equal(typeof(object), OuterMixed<string, object>(false));
        Assert.Equal(typeof(object[]), OuterMixed<string, object>(true));
        Assert.Equal(typeof(int), OuterMixed<string, int>(false));
        Assert.Equal(typeof(string[]), OuterArray<object, string>());
        Assert.Equal(typeof(string), Swapped<string, object>());
        Assert.Equal(typeof(object), Swapped<object, string>());
        Assert.Equal(typeof(string), ThreeLevels<string, object>(true));
        Assert.Equal(typeof(object), ThreeLevels<string, object>(false));
        Assert.Equal(typeof(int), ThreeLevels<int, string>(true));
        Assert.Equal(typeof(string), ThreeLevels<int, string>(false));
        Assert.Equal(typeof(object[]), Constructed<object, string>());
        Assert.Equal(typeof(object), NestedKept<object, string>());
        Assert.Equal(typeof(string), NestedKept<string, object>());
        Assert.Equal(typeof(string), Recursive<object, string>(2));
        Assert.Equal(typeof(string[]), NestedArray<string, object>());
        Assert.Equal(typeof(object[]), NestedArray<object, string>());
        Assert.Equal(typeof(int[]), NestedArray<int, string>());
        Assert.Equal(typeof(string[]), Jagged<object, string>(false));
        Assert.Equal(typeof(string[][]), Jagged<object, string>(true));
        Assert.Equal(typeof(int[][]), Jagged<string, int>(true));
        Assert.Equal(typeof(string[,]), Rectangular<object, string>());
        Assert.Equal(typeof(List<string[]>), GenericWrapper<object, string>());
        Assert.Equal(typeof(Dictionary<string, object>[]), Pair<object, string>());
        Assert.Equal(typeof(KeyValuePair<string, List<object[]>>), Named<string, object>());
        Assert.Equal(typeof(KeyValuePair<object, List<string[]>>), Named<object, string>());
        Assert.Equal(typeof(Dictionary<string, int[]>), Compound<int>());
        Assert.Equal(typeof(ForwardedArgumentBox<PairType<string, object>>), CrossModule<string, object>());
        Assert.Equal(typeof(ForwardedArgumentBox<PairType<object, string>>), CrossModule<object, string>());
        object box = Owner<string>.Allocate();
        Assert.Equal(typeof(Box<List<string>>), box.GetType());
        Assert.True(Owner<string>.Test(box));
        Assert.False(Owner<object>.Test(box));
        Assert.False(Owner<string>.Test(null));
        Assert.True(ReferenceEquals(box, Owner<string>.CheckedCast(box)));
        Assert.True(Owner<string>.CheckedCast(null) == null);
        Assert.Throws<InvalidCastException>(() => Owner<object>.CheckedCast(box));
        Assert.True(Owner<string>.TestNullable(new KeyValuePair<string, int>()));
        Assert.False(Owner<object>.TestNullable(new KeyValuePair<string, int>()));
        Assert.Equal(typeof(List<string>[]), Owner<string>.Vector(2).GetType());
        Assert.Equal(typeof(object[][]), Owner<object>.Jagged(2).GetType());
        Assert.Equal(Record.Exception(() => { Owner<string>.DirectVector(-1); }).GetType(),
                     Record.Exception(() => { Owner<string>.Vector(-1); }).GetType());

        Assert.Equal(17, Owner<string>.ReadCache());
        Assert.Equal(17, Owner<object>.ReadCache());
        Owner<string>.WriteCache(53);
        Owner<object>.CacheAddress() = 92;
        Assert.Equal(53, Owner<string>.ReadCache());
        Assert.Equal(92, Owner<object>.ReadCache());
        Assert.Equal(typeof(List<string>), Owner<string>.CacheTag());
        Assert.Equal(typeof(List<object>), Owner<object>.CacheTag());
        Assert.Equal(2, s_cacheInitializers);
        Owner<string>.WriteCache(17);
        Owner<object>.WriteCache(17);

        var derived = new DerivedOwner<object, string>();
        Assert.Equal(typeof(List<string>), derived.OwnerOnly());
        Assert.Equal(typeof(List<object>), new DerivedOwner<string, object>().OwnerOnly());
        Assert.Equal(typeof(List<string>[]), Owner<string>.OwnerRelay());
        Assert.Equal(typeof(List<object>), Owner<object>.OwnerKept());
        Assert.Equal(typeof(string), Owner<string>.Initialized());
        Assert.Equal(typeof(string), Owner<string>.Initialized());
        Assert.Equal(typeof(object), Owner<object>.Initialized());
        Assert.Equal(2, s_typeInitializers);
        Assert.Equal(typeof(string), derived.InstanceElement());
        Assert.Equal(typeof(object), new DerivedOwner<string, object>().InstanceElement());
        Assert.Equal(typeof(Owner<string>), derived.InstanceOwner());
        var twoContexts = new TwoContexts();
        Assert.Equal(typeof(List<string[]>), ((IContext<string>)twoContexts).GetContext());
        Assert.Equal(typeof(List<object[]>), ((IContext<object>)twoContexts).GetContext());
        Assert.Equal(typeof(string[]), derived.KeptFromThis());
        Assert.Equal(typeof(List<string[]>), Owner<string>.FromClassContext());
        Assert.Equal(typeof(List<object[]>), default(ValueOwner<object>).Instance());
        Assert.Equal(typeof(Dictionary<object, string>), Owner<string>.Swap<object>());
        Assert.Equal(typeof(Dictionary<string, object>), Owner<object>.Swap<string>());
        Assert.Equal(typeof(Dictionary<List<string[]>, object>), Owner<string>.Nested<object>());
        Assert.Equal(typeof(Dictionary<string, object>), Owner<string>.Both<object>());
        Assert.Equal(typeof(Dictionary<object, string>), Owner<object>.Both<string>());
        Assert.Equal(typeof(string), Owner<string>.Forward<object>(true));
        Assert.Equal(typeof(object), Owner<string>.Forward<object>(false));
    }
}
