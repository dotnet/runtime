// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

public class KnownConstrainedMethodEntry
{
    private interface IOperation
    {
        Type Get<TMethod>();
    }

    private struct Operation<TState> : IOperation
    {
        public int Calls;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Type Get<TMethod>()
        {
            Calls++;
            return typeof((TState, TMethod));
        }
    }

    private struct PlainOperation : IOperation
    {
        public int Calls;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Type Get<TMethod>()
        {
            Calls++;
            return typeof(TMethod);
        }
    }

    private interface IDefaultOperation : IOperation
    {
        Type IOperation.Get<TMethod>() => typeof(TMethod);
    }

    private struct DefaultOperation : IDefaultOperation { }

    private interface IChoice<T>
    {
        int Choose();
    }

    private struct MultipleChoices : IChoice<object>, IChoice<string>
    {
        public int Total;
        int IChoice<object>.Choose() => Total += 1;
        int IChoice<string>.Choose() => Total += 2;
    }

    private struct Comparable<T> : IComparable<T>
    {
        public int Calls;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public int CompareTo(T other)
        {
            Calls++;
            return typeof(T) == typeof(string) ? 11 : 13;
        }
    }

    private interface IStaticOperation
    {
        static abstract Type Get();
    }

    private struct StaticOperation<T> : IStaticOperation
    {
        private static readonly Type s_result = typeof(T);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Type Get() => s_result;
    }

    private class StaticClass<T> : IStaticOperation
    {
        private static readonly Type s_result = typeof(T);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Type Get() => s_result;
    }

    private class DerivedStaticClass<T> : StaticClass<List<T>> { }

    private interface IDefaultStatic<T> : IStaticOperation
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static Type IStaticOperation.Get() => typeof(T);
    }

    private interface IMarker { }

    private struct DefaultStatic<T> : IMarker, IDefaultStatic<List<T>> { }

    private class DefaultStaticClass<T> : IDefaultStatic<List<T>> { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Type InvokeStatic<TReceiver>() where TReceiver : IStaticOperation
        => TReceiver.Get();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<Type> CreateStaticDelegate<TReceiver>() where TReceiver : IStaticOperation
        => TReceiver.Get;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void TestStaticClasses<T>()
    {
        // Inlining exposes the constructed class to resolution while keeping T shared.
        Assert.Equal(typeof(T), InvokeStatic<StaticClass<T>>());
        Assert.Equal(typeof(List<T>), InvokeStatic<DerivedStaticClass<T>>());
        Assert.Equal(typeof(T), CreateStaticDelegate<StaticClass<T>>()());
        Assert.Equal(typeof(List<T>), CreateStaticDelegate<DerivedStaticClass<T>>()());
        Assert.Equal(typeof(List<T>), InvokeStatic<DefaultStaticClass<T>>());
        Assert.Equal(typeof(List<T>), CreateStaticDelegate<DefaultStaticClass<T>>()());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type Call<TReceiver, TMethod>(ref TReceiver receiver) where TReceiver : struct, IOperation
        => receiver.Get<TMethod>();

    private class TypeCaller<TReceiver, TMethod> where TReceiver : struct, IOperation
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Type Call(ref TReceiver receiver) => receiver.Get<TMethod>();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Type CallStatic(ref TReceiver receiver) => receiver.Get<TMethod>();
    }

    private class MixedCaller<TReceiver> where TReceiver : struct, IOperation
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Type Call<TMethod>(ref TReceiver receiver) => receiver.Get<TMethod>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Choose<TReceiver, T>(ref TReceiver receiver) where TReceiver : struct, IChoice<T>
        => receiver.Choose();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Compare<TReceiver, T>(ref TReceiver receiver, T other) where TReceiver : struct, IComparable<T>
        => receiver.CompareTo(other);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type CallStatic<TReceiver>() where TReceiver : struct, IStaticOperation
        => TReceiver.Get();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Func<Type> GetStaticDelegate<TReceiver>() where TReceiver : struct, IStaticOperation
        => TReceiver.Get;

    [Fact]
    public static void TestEntryPoint()
    {
        Operation<object> objectReceiver = default;
        Operation<string> stringReceiver = default;
        PlainOperation plainReceiver = default;
        DefaultOperation defaultReceiver = default;
        MultipleChoices choices = default;
        Comparable<string> stringComparable = default;
        Comparable<object> objectComparable = default;
        var objectCaller = new TypeCaller<Operation<object>, string>();
        var stringCaller = new TypeCaller<Operation<string>, object>();

        // Exercise both cold dictionary population and reuse, without boxing the receivers.
        for (int i = 1; i <= 2; i++)
        {
            Assert.Equal(typeof((object, string)), Call<Operation<object>, string>(ref objectReceiver));
            Assert.Equal(typeof((string, object)), Call<Operation<string>, object>(ref stringReceiver));
            Assert.Equal(typeof((object, int)), Call<Operation<object>, int>(ref objectReceiver));
            Assert.Equal(typeof((string, int)), Call<Operation<string>, int>(ref stringReceiver));
            Assert.Equal(typeof((object, string)), objectCaller.Call(ref objectReceiver));
            Assert.Equal(typeof((string, object)), stringCaller.Call(ref stringReceiver));
            Assert.Equal(typeof((object, string)), TypeCaller<Operation<object>, string>.CallStatic(ref objectReceiver));
            Assert.Equal(typeof((string, object)), TypeCaller<Operation<string>, object>.CallStatic(ref stringReceiver));
            Assert.Equal(typeof((object, string)), MixedCaller<Operation<object>>.Call<string>(ref objectReceiver));
            Assert.Equal(typeof((string, object)), MixedCaller<Operation<string>>.Call<object>(ref stringReceiver));
            Assert.Equal(5 * i, objectReceiver.Calls);
            Assert.Equal(5 * i, stringReceiver.Calls);

            Assert.Equal(typeof(object), Call<PlainOperation, object>(ref plainReceiver));
            Assert.Equal(typeof(string), Call<PlainOperation, string>(ref plainReceiver));
            Assert.Equal(2 * i, plainReceiver.Calls);

            Assert.Equal(typeof(object), Call<DefaultOperation, object>(ref defaultReceiver));
            Assert.Equal(typeof(string), Call<DefaultOperation, string>(ref defaultReceiver));
            Assert.Equal(3 * (i - 1) + 1, Choose<MultipleChoices, object>(ref choices));
            Assert.Equal(3 * i, Choose<MultipleChoices, string>(ref choices));

            Assert.Equal(11, Compare<Comparable<string>, string>(ref stringComparable, "value"));
            Assert.Equal(13, Compare<Comparable<object>, object>(ref objectComparable, new object()));
            Assert.Equal(i, stringComparable.Calls);
            Assert.Equal(i, objectComparable.Calls);

            Assert.Equal(typeof(object), CallStatic<StaticOperation<object>>());
            Assert.Equal(typeof(string), CallStatic<StaticOperation<string>>());
            Assert.Equal(typeof(object), GetStaticDelegate<StaticOperation<object>>()());
            Assert.Equal(typeof(string), GetStaticDelegate<StaticOperation<string>>()());
            Assert.Equal(typeof(List<object>), CallStatic<DefaultStatic<object>>());
            Assert.Equal(typeof(List<string>), CallStatic<DefaultStatic<string>>());
            Assert.Equal(typeof(List<object>), GetStaticDelegate<DefaultStatic<object>>()());
            Assert.Equal(typeof(List<string>), GetStaticDelegate<DefaultStatic<string>>()());
            TestStaticClasses<object>();
            TestStaticClasses<string>();
        }
    }
}
