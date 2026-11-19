// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using CoreFXTestLibrary;

public static class StaticInterfaceGvms
{
    public interface IOperation { static abstract Type M<TArg>(); }
    public interface IDefault<T> : IOperation
    {
        static Type IOperation.M<TArg>() => typeof((T, TArg));
    }
    public interface IInherited<A, B> : IDefault<B> { }
    public interface IClosed : IDefault<string> { }
    public interface IPlain : IOperation
    {
        static Type IOperation.M<TArg>() => typeof((int, TArg));
    }
    public interface IOverride<T> : IDefault<T>
    {
        static Type IOperation.M<TArg>() => typeof((TArg, T));
    }
    public interface ILeft<T> : IDefault<T> { }
    public interface IRight<T> : IDefault<T> { }
    public interface IDiamond<T> : ILeft<T>, IRight<T> { }
    public class DefaultClass<T> : IDefault<T> { }
    public interface ISelf<T>
    {
        static virtual Type M<TArg>() => typeof((T, TArg));
    }
    public interface ICovariant<out T>
    {
        static virtual Type M<TArg>() => typeof((T, TArg));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Type Call<TImpl, TArg>() where TImpl : class, IOperation where TArg : class
        => TImpl.M<TArg>();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Type CallDelegate<TImpl, TArg>() where TImpl : class, IOperation where TArg : class
        => ((Func<Type>)TImpl.M<TArg>)();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Type CallSelf<TImpl, T, TArg>() where TImpl : class, ISelf<T> where T : class where TArg : class
        => TImpl.M<TArg>();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Type CallVariant<TImpl, TArg>() where TImpl : class, ICovariant<object> where TArg : class
        => TImpl.M<TArg>();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type Hide(Type type) => type;

    private static void Check(Type implementation, Type expected)
    {
        Type argument = Hide(typeof(Uri));
        var call = typeof(StaticInterfaceGvms).GetMethod(nameof(Call)).MakeGenericMethod(implementation, argument);
        var callback = typeof(StaticInterfaceGvms).GetMethod(nameof(CallDelegate)).MakeGenericMethod(implementation, argument);
        Assert.AreEqual(expected, (Type)call.Invoke(null, null));
        Assert.AreEqual(expected, (Type)callback.Invoke(null, null));
    }

    [TestMethod]
    public static void TestReflection()
    {
        // Root the shared callers and default body, but create the Uri dictionaries at runtime.
        Assert.AreEqual(typeof((string, object)), Call<IDefault<string>, object>());
        Assert.AreEqual(typeof((string, object)), CallDelegate<IDefault<string>, object>());
        Check(Hide(typeof(IDefault<string>)), typeof((string, Uri)));
        Check(Hide(typeof(IInherited<object, string>)), typeof((string, Uri)));
        Check(Hide(typeof(IInherited<string, object>)), typeof((object, Uri)));
        Check(Hide(typeof(IClosed)), typeof((string, Uri)));
        Check(Hide(typeof(IPlain)), typeof((int, Uri)));
        Check(Hide(typeof(IOverride<string>)), typeof((Uri, string)));
        Check(Hide(typeof(IDiamond<string>)), typeof((string, Uri)));
        Check(Hide(typeof(DefaultClass<string>)), typeof((string, Uri)));

        Assert.AreEqual(typeof((string, object)), CallSelf<ISelf<string>, string, object>());
        var self = typeof(StaticInterfaceGvms).GetMethod(nameof(CallSelf)).MakeGenericMethod(
            Hide(typeof(ISelf<string>)), Hide(typeof(string)), Hide(typeof(Uri)));
        Assert.AreEqual(typeof((string, Uri)), (Type)self.Invoke(null, null));

        Assert.AreEqual(typeof((string, object)), CallVariant<ICovariant<string>, object>());
        var variant = typeof(StaticInterfaceGvms).GetMethod(nameof(CallVariant)).MakeGenericMethod(
            Hide(typeof(ICovariant<string>)), Hide(typeof(Uri)));
        Assert.AreEqual(typeof((string, Uri)), (Type)variant.Invoke(null, null));
    }
}
