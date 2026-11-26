// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class ConstrainedTypeContext
{
    private interface IOperation
    {
        int Add(int value);
        static abstract int Increment(int value);
    }

    private struct Operation<T> : IOperation
    {
        public int Bias;
        public int Add(int value) => value + Bias;
        public static int Increment(int value) => value + 1;
    }

    private interface IContext
    {
        Type GetTag();
        static abstract Type GetStaticTag();
    }

    private struct Context<T> : IContext
    {
        public int Calls;
        public Type GetTag() { Calls++; return typeof(T); }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Type GetStaticTag() => typeof(T);
    }

    private struct InitializedContext<T> : IContext
    {
        private static readonly Type s_tag;
        static InitializedContext() => s_tag = typeof(T);
        public Type GetTag() => s_tag;
        public static Type GetStaticTag() => s_tag;
    }

    private interface IEcho<T>
    {
        T GetValue();
    }

    private struct Echo<T> : IEcho<T>
    {
        public T Value;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public T GetValue() => Value;
    }

    private struct Ambiguous<T> : IEcho<object>, IEcho<string>
    {
        public int Calls;
        object IEcho<object>.GetValue() { Calls += 1; return typeof(T); }
        string IEcho<string>.GetValue() { Calls += 10; return "string implementation"; }
    }

    private interface IGeneric
    {
        Type GetTypes<T>();
        static abstract Type GetStaticTypes<T>();
    }

    private struct Generic<T> : IGeneric
    {
        public Type GetTypes<U>() => typeof((T, U));
        public static Type GetStaticTypes<U>() => typeof((T, U));
    }

    private interface IDefault<T> : IContext
    {
        Type IContext.GetTag() => typeof(T);
        static Type IContext.GetStaticTag() => typeof(T);
    }

    private struct DefaultContext<T> : IDefault<T> { }

    private class ReferenceContext<T> : IContext
    {
        public Type GetTag() => typeof(T);
        public static Type GetStaticTag() => typeof(T);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CallInstance<T>(ref T operation, int value) where T : struct, IOperation
    {
        // ARM64-NOT: {{bl(r)?[[:space:]]}}
        // ARM64: ret
        // X64-NOT: call
        // X64: ret
        return operation.Add(value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CallStatic<T>(int value) where T : struct, IOperation
    {
        // ARM64-NOT: {{bl(r)?[[:space:]]}}
        // ARM64: ret
        // X64-NOT: call
        // X64: ret
        return T.Increment(value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type CallContext<T>(ref T value) where T : IContext => value.GetTag();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type CallStaticContext<T>() where T : IContext => T.GetStaticTag();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Type InlineContext<T>(ref T value) where T : IContext => value.GetTag();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type CallConstructedContext<T>()
    {
        var context = new Context<T>();
        return InlineContext(ref context);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Func<Type> GetStaticDelegate<T>() where T : IContext => T.GetStaticTag;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static TValue CallEcho<T, TValue>(ref T value) where T : struct, IEcho<TValue> => value.GetValue();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type CallGeneric<T, U>(T value) where T : struct, IGeneric => value.GetTypes<U>();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type CallStaticGeneric<T, U>() where T : struct, IGeneric => T.GetStaticTypes<U>();

    private class Caller<T> where T : IContext
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Type Call(ref T value) => value.GetTag();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Type CallStatic() => T.GetStaticTag();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CheckContext<T>()
    {
        var context = new Context<T>();
        Assert.Equal(typeof(T), CallContext(ref context));
        Assert.Equal(1, context.Calls);
        Assert.Equal(typeof(T), CallStaticContext<Context<T>>());
        Assert.Equal(typeof(T), CallConstructedContext<T>());
        Assert.Equal(typeof(T), new Caller<Context<T>>().Call(ref context));
        Assert.Equal(2, context.Calls);
        Assert.Equal(typeof(T), Caller<Context<T>>.CallStatic());
        Assert.Equal(typeof(T), GetStaticDelegate<Context<T>>()());

        var initialized = new InitializedContext<T>();
        Assert.Equal(typeof(T), CallStaticContext<InitializedContext<T>>());
        Assert.Equal(typeof(T), CallContext(ref initialized));

        var defaultContext = new DefaultContext<T>();
        Assert.Equal(typeof(T), CallContext(ref defaultContext));
        Assert.Equal(typeof(T), CallStaticContext<DefaultContext<T>>());
        var reference = new ReferenceContext<T>();
        Assert.Equal(typeof(T), CallContext(ref reference));
        Assert.Equal(typeof(T), CallStaticContext<ReferenceContext<T>>());

        Assert.Equal(typeof((T, object)), CallGeneric<Generic<T>, object>(default));
        Assert.Equal(typeof((T, string)), CallGeneric<Generic<T>, string>(default));
        Assert.Equal(typeof((T, int)), CallGeneric<Generic<T>, int>(default));
        Assert.Equal(typeof((T, object)), CallStaticGeneric<Generic<T>, object>());
        Assert.Equal(typeof((T, string)), CallStaticGeneric<Generic<T>, string>());
        Assert.Equal(typeof((T, int)), CallStaticGeneric<Generic<T>, int>());

        var ambiguous = new Ambiguous<T>();
        Assert.Equal(typeof(T), CallEcho<Ambiguous<T>, object>(ref ambiguous));
        Assert.Equal("string implementation", CallEcho<Ambiguous<T>, string>(ref ambiguous));
        Assert.Equal(11, ambiguous.Calls);
    }

    [Fact]
    public static void TestEntryPoint()
    {
        var operation = new Operation<object> { Bias = 3 };
        Assert.Equal(7, CallInstance(ref operation, 4));
        Assert.Equal(5, CallStatic<Operation<object>>(4));

        CheckContext<object>();
        CheckContext<string>();
        CheckContext<(object, string)>();
        CheckContext<int>();

        object value = new object();
        var echo = new Echo<object> { Value = value };
        Assert.Same(value, CallEcho<Echo<object>, object>(ref echo));
        var text = new Echo<string> { Value = "echo" };
        Assert.Equal("echo", CallEcho<Echo<string>, string>(ref text));
        var pair = new Echo<(object, int)> { Value = (value, 42) };
        Assert.Equal((value, 42), CallEcho<Echo<(object, int)>, (object, int)>(ref pair));
    }
}
