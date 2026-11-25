// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class DeferredInterfaceLookup
{
    private interface IChoice<T>
    {
        int Choose();
    }

    private sealed class Choice<T> : IChoice<T>
    {
        public int Choose() => typeof(T) == typeof(string) ? 17 : 19;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Dispatch<T>(IChoice<T> receiver) => receiver.Choose();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Devirtualize<T>()
    {
        IChoice<T> receiver = new Choice<T>();
        return receiver.Choose();
    }

    private static Type Target<T>() => typeof(T);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Func<Type> Bind<T>() => new Func<Type>(Target<T>);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe delegate* managed<Type> Pointer<T>() => &Target<T>;

    private struct Receiver
    {
        public int Value;
        public Type Read<T>() => Value == 42 ? typeof(T) : throw new InvalidOperationException();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Func<Type> BindStruct<T>(Receiver receiver) => new Func<Type>(receiver.Read<T>);

    private readonly unsafe struct Holder
    {
        public readonly delegate* managed<Type> Pointer;
        public Holder(delegate* managed<Type> pointer) => Pointer = pointer;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe Holder Wrap<T>() => new Holder(&Target<T>);

    [Fact]
    public static unsafe void TestEntryPoint()
    {
        Assert.Equal(typeof(string), Bind<string>()());
        Assert.Equal(typeof(object), Bind<object>()());
        // The delegate binds a boxed receiver and uses the unboxing entry.
        Assert.Equal(typeof(string), BindStruct<string>(new Receiver { Value = 42 })());
        Assert.Equal(typeof(string), Pointer<string>()());
        Assert.Equal(typeof(object), Wrap<object>().Pointer());

        for (int i = 0; i < 2; i++)
        {
            Assert.Equal(17, Dispatch<string>(new Choice<string>()));
            Assert.Equal(19, Dispatch<object>(new Choice<object>()));
            Assert.Equal(17, Devirtualize<string>());
            Assert.Equal(19, Devirtualize<object>());
        }
    }
}
