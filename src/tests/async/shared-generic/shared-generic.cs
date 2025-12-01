// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;
public class Async2SharedGeneric
{
    public struct S0
    {
        public object _o;
        public S0(object o) => _o = o;
    }

    public struct S1<T>
    {
        public T t;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        // simple cases
        Async1EntryPoint<int>(typeof(int), 42).Wait();
        Async1EntryPoint<string>(typeof(string), "abc").Wait();
        Async1EntryPoint<object>(typeof(object), "def").Wait();

        // struct with an obj and its nullable
        Async1EntryPoint<S0>(typeof(S0), new S0(42)).Wait();
        Async1EntryPoint<S0?>(typeof(S0?), new S0(42)).Wait();
        Async1EntryPoint<S0?>(typeof(S0?), null).Wait();

        // generic struct with an obj and its nullable
        Async1EntryPoint<S1<string>>(typeof(S1<string>), new S1<string> { t = "ghj" }).Wait();
        Async1EntryPoint<S1<string>?>(typeof(S1<string>?), new S1<string> { t = "qwe" }).Wait();
        Async1EntryPoint<S1<string>?>(typeof(S1<string>?), null).Wait();

        // simple cases
        Async2EntryPoint<int>(typeof(int), 142).Wait();
        Async2EntryPoint<string>(typeof(string), "ghi").Wait();
        Async2EntryPoint<object>(typeof(object), "jkl").Wait();

        // struct with an obj and its nullable
        Async2EntryPoint<S0>(typeof(S0), new S0(4242)).Wait();
        Async2EntryPoint<S0?>(typeof(S0?), new S0(424242)).Wait();
        Async2EntryPoint<S0?>(typeof(S0?), null).Wait();

        // generic struct with an obj and its nullable
        Async2EntryPoint<S1<string>>(typeof(S1<string>), new S1<string> { t = "kl" }).Wait();
        Async2EntryPoint<S1<string>?>(typeof(S1<string>?), new S1<string> { t = "zx" }).Wait();
        Async2EntryPoint<S1<string>?>(typeof(S1<string>?), null).Wait();
    }

    [RuntimeAsyncMethodGeneration(false)]
    private static async Task Async1EntryPoint<T>(Type t, T value)
    {
        await new GenericClass<T>().InstanceMethod(t);
        await GenericClass<T>.StaticMethod(t);
        await GenericClass<T>.StaticMethod<T>(t, t);
        await GenericClass<T>.StaticMethodAsync1(t);
        await GenericClass<T>.StaticMethodAsync1<T>(t, t);
        Assert.Equal(value, value); // make sure we can compare value
        Assert.Equal(value, await GenericClass<T>.StaticReturnClassType(value));
        Assert.Equal(value, await GenericClass<T>.StaticReturnMethodType<T>(value));
        Assert.Equal(value, await GenericClass<T>.StaticReturnClassTypeAsync1(value));
        Assert.Equal(value, await GenericClass<T>.StaticReturnMethodTypeAsync1<T>(value));
    }

    private static async Task Async2EntryPoint<T>(Type t, T value)
    {
        await new GenericClass<T>().InstanceMethod(t);
        await GenericClass<T>.StaticMethod(t);
        await GenericClass<T>.StaticMethod<T>(t, t);
        await GenericClass<T>.StaticMethodAsync1(t);
        await GenericClass<T>.StaticMethodAsync1<T>(t, t);
        Assert.Equal(value, value); // make sure we can compare value
        Assert.Equal(value, await GenericClass<T>.StaticReturnClassType(value));
        Assert.Equal(value, await GenericClass<T>.StaticReturnMethodType<T>(value));
        Assert.Equal(value, await GenericClass<T>.StaticReturnClassTypeAsync1(value));
        Assert.Equal(value, await GenericClass<T>.StaticReturnMethodTypeAsync1<T>(value));
    }

    [Fact]
    public static void TestInterface()
    {
        TestInterfaceAsync(new JsonDeserializer<ArrayReader>()).GetAwaiter().GetResult();
    }

    private static async Task TestInterfaceAsync(ITypeDeserializer deserializer)
    {
        Assert.Equal("abc", await deserializer.ReadString());
    }

    private struct ArrayReader
    {
    }

    private interface ITypeDeserializer
    {
        Task<string> ReadString();
    }

    private class JsonDeserializer<TReader> : ITypeDeserializer
    {
        Task<string> ITypeDeserializer.ReadString() => Task.FromResult("abc");
    }
}

public class GenericClass<T>
{
    // 'this' is context
    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task InstanceMethod(Type t)
    {
        Assert.Equal(typeof(T), t);
        await Task.Yield();
        Assert.Equal(typeof(T), t);
    }

    // Class context
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task StaticMethod(Type t)
    {
        Assert.Equal(typeof(T), t);
        await Task.Yield();
        Assert.Equal(typeof(T), t);
    }

    // Method context
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task StaticMethod<TM>(Type t, Type tm)
    {
        Assert.Equal(typeof(T), t);
        Assert.Equal(typeof(TM), tm);
        await Task.Yield();
        Assert.Equal(typeof(T), t);
        Assert.Equal(typeof(TM), tm);
    }

    // Class context
    [MethodImpl(MethodImplOptions.NoInlining)]
    [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(false)]
    public static async Task StaticMethodAsync1(Type t)
    {
        Assert.Equal(typeof(T), t);
        await Task.Yield();
        Assert.Equal(typeof(T), t);
    }

    // Method context
    [MethodImpl(MethodImplOptions.NoInlining)]
    [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(false)]
    public static async Task StaticMethodAsync1<TM>(Type t, Type tm)
    {
        Assert.Equal(typeof(T), t);
        Assert.Equal(typeof(TM), tm);
        await Task.Yield();
        Assert.Equal(typeof(T), t);
        Assert.Equal(typeof(TM), tm);
    }

    public static async Task<T> StaticReturnClassType(T value)
    {
        await Task.Yield();
        return value;
    }

    public static async Task<TM> StaticReturnMethodType<TM>(TM value)
    {
        await Task.Yield();
        return value;
    }

    [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(false)]
    public static async Task<T> StaticReturnClassTypeAsync1(T value)
    {
        await Task.Yield();
        return value;
    }

    [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(false)]
    public static async Task<TM> StaticReturnMethodTypeAsync1<TM>(TM value)
    {
        await Task.Yield();
        return value;
    }
}
