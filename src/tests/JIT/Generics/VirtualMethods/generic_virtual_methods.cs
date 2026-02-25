// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class GenericVirtualMethodTests
{
    [Fact]
    public static void ClassBase_NonGenericDerived_AllVariants()
    {
        ValidateCaller("ClassBase_NonGenericDerived_Inlining", new ClassBaseCaller(new ClassBase_NonGenericDerived_Inlining()));
        ValidateCaller("ClassBase_NonGenericDerived_NoInlining", new ClassBaseCaller(new ClassBase_NonGenericDerived_NoInlining()));
    }

    [Fact]
    public static void ClassBase_GenericDerived_AllVariants()
    {
        ValidateCaller("ClassBase_GenericDerived_Inlining_Int", new ClassBaseCaller(new ClassBase_GenericDerived_Inlining<int>()));
        ValidateCaller("ClassBase_GenericDerived_Inlining_String", new ClassBaseCaller(new ClassBase_GenericDerived_Inlining<string>()));
        ValidateCaller("ClassBase_GenericDerived_NoInlining_Int", new ClassBaseCaller(new ClassBase_GenericDerived_NoInlining<int>()));
        ValidateCaller("ClassBase_GenericDerived_NoInlining_String", new ClassBaseCaller(new ClassBase_GenericDerived_NoInlining<string>()));
    }

    [Fact]
    public static void GenericClassBase_NonGenericDerived_AllVariants()
    {
        ValidateCaller("GenericClassBase_NonGenericDerived_Inlining_NonShared", new GenericClassBaseCaller<int>(new GenericClassBase_NonGenericDerived_Inlining_NonShared()));
        ValidateCaller("GenericClassBase_NonGenericDerived_Inlining_Shared", new GenericClassBaseCaller<string>(new GenericClassBase_NonGenericDerived_Inlining_Shared()));
        ValidateCaller("GenericClassBase_NonGenericDerived_NoInlining_NonShared", new GenericClassBaseCaller<int>(new GenericClassBase_NonGenericDerived_NoInlining_NonShared()));
        ValidateCaller("GenericClassBase_NonGenericDerived_NoInlining_Shared", new GenericClassBaseCaller<string>(new GenericClassBase_NonGenericDerived_NoInlining_Shared()));
    }

    [Fact]
    public static void GenericClassBase_GenericDerived_InliningVariants()
    {
        ValidateCaller("GenericClassBase_GenericDerived_Inlining_Int_Int", new GenericClassBaseCaller<int>(new GenericClassBase_GenericDerived_Inlining<int, int>()));
        ValidateCaller("GenericClassBase_GenericDerived_Inlining_Int_String", new GenericClassBaseCaller<int>(new GenericClassBase_GenericDerived_Inlining<int, string>()));
        ValidateCaller("GenericClassBase_GenericDerived_Inlining_String_Int", new GenericClassBaseCaller<string>(new GenericClassBase_GenericDerived_Inlining<string, int>()));
        ValidateCaller("GenericClassBase_GenericDerived_Inlining_String_String", new GenericClassBaseCaller<string>(new GenericClassBase_GenericDerived_Inlining<string, string>()));
    }

    [Fact]
    public static void GenericClassBase_GenericDerived_NoInliningVariants()
    {
        ValidateCaller("GenericClassBase_GenericDerived_NoInlining_Int_Int", new GenericClassBaseCaller<int>(new GenericClassBase_GenericDerived_NoInlining<int, int>()));
        ValidateCaller("GenericClassBase_GenericDerived_NoInlining_Int_String", new GenericClassBaseCaller<int>(new GenericClassBase_GenericDerived_NoInlining<int, string>()));
        ValidateCaller("GenericClassBase_GenericDerived_NoInlining_String_Int", new GenericClassBaseCaller<string>(new GenericClassBase_GenericDerived_NoInlining<string, int>()));
        ValidateCaller("GenericClassBase_GenericDerived_NoInlining_String_String", new GenericClassBaseCaller<string>(new GenericClassBase_GenericDerived_NoInlining<string, string>()));
    }

    [Fact]
    public static void InterfaceBase_NonGenericClassDerived_AllVariants()
    {
        ValidateCaller("InterfaceBase_NonGenericClassDerived_Inlining", new InterfaceBaseCaller(new InterfaceBase_NonGenericClassDerived_Inlining()));
        ValidateCaller("InterfaceBase_NonGenericClassDerived_NoInlining", new InterfaceBaseCaller(new InterfaceBase_NonGenericClassDerived_NoInlining()));
    }

    [Fact]
    public static void InterfaceBase_NonGenericStructDerived_AllVariants()
    {
        ValidateCaller("InterfaceBase_NonGenericStructDerived_Inlining", new InterfaceBaseCaller(new InterfaceBase_NonGenericStructDerived_Inlining()));
        ValidateCaller("InterfaceBase_NonGenericStructDerived_NoInlining", new InterfaceBaseCaller(new InterfaceBase_NonGenericStructDerived_NoInlining()));
    }

    [Fact]
    public static void InterfaceBase_GenericClassDerived_AllVariants()
    {
        ValidateCaller("InterfaceBase_GenericClassDerived_Inlining_Int", new InterfaceBaseCaller(new InterfaceBase_GenericClassDerived_Inlining<int>()));
        ValidateCaller("InterfaceBase_GenericClassDerived_Inlining_String", new InterfaceBaseCaller(new InterfaceBase_GenericClassDerived_Inlining<string>()));
        ValidateCaller("InterfaceBase_GenericClassDerived_NoInlining_Int", new InterfaceBaseCaller(new InterfaceBase_GenericClassDerived_NoInlining<int>()));
        ValidateCaller("InterfaceBase_GenericClassDerived_NoInlining_String", new InterfaceBaseCaller(new InterfaceBase_GenericClassDerived_NoInlining<string>()));
    }

    [Fact]
    public static void InterfaceBase_GenericStructDerived_AllVariants()
    {
        ValidateCaller("InterfaceBase_GenericStructDerived_Inlining_Int", new InterfaceBaseCaller(new InterfaceBase_GenericStructDerived_Inlining<int>()));
        ValidateCaller("InterfaceBase_GenericStructDerived_Inlining_String", new InterfaceBaseCaller(new InterfaceBase_GenericStructDerived_Inlining<string>()));
        ValidateCaller("InterfaceBase_GenericStructDerived_NoInlining_Int", new InterfaceBaseCaller(new InterfaceBase_GenericStructDerived_NoInlining<int>()));
        ValidateCaller("InterfaceBase_GenericStructDerived_NoInlining_String", new InterfaceBaseCaller(new InterfaceBase_GenericStructDerived_NoInlining<string>()));
    }

    [Fact]
    public static void GenericInterfaceBase_NonGenericClassDerived_AllVariants()
    {
        ValidateCaller("GenericInterfaceBase_NonGenericClassDerived_Inlining_NonShared", new GenericInterfaceBaseCaller<int>(new GenericInterfaceBase_NonGenericClassDerived_Inlining_NonShared()));
        ValidateCaller("GenericInterfaceBase_NonGenericClassDerived_Inlining_Shared", new GenericInterfaceBaseCaller<string>(new GenericInterfaceBase_NonGenericClassDerived_Inlining_Shared()));
        ValidateCaller("GenericInterfaceBase_NonGenericClassDerived_NoInlining_NonShared", new GenericInterfaceBaseCaller<int>(new GenericInterfaceBase_NonGenericClassDerived_NoInlining_NonShared()));
        ValidateCaller("GenericInterfaceBase_NonGenericClassDerived_NoInlining_Shared", new GenericInterfaceBaseCaller<string>(new GenericInterfaceBase_NonGenericClassDerived_NoInlining_Shared()));
    }

    [Fact]
    public static void GenericInterfaceBase_NonGenericStructDerived_AllVariants()
    {
        ValidateCaller("GenericInterfaceBase_NonGenericStructDerived_Inlining_NonShared", new GenericInterfaceBaseCaller<int>(new GenericInterfaceBase_NonGenericStructDerived_Inlining_NonShared()));
        ValidateCaller("GenericInterfaceBase_NonGenericStructDerived_Inlining_Shared", new GenericInterfaceBaseCaller<string>(new GenericInterfaceBase_NonGenericStructDerived_Inlining_Shared()));
        ValidateCaller("GenericInterfaceBase_NonGenericStructDerived_NoInlining_NonShared", new GenericInterfaceBaseCaller<int>(new GenericInterfaceBase_NonGenericStructDerived_NoInlining_NonShared()));
        ValidateCaller("GenericInterfaceBase_NonGenericStructDerived_NoInlining_Shared", new GenericInterfaceBaseCaller<string>(new GenericInterfaceBase_NonGenericStructDerived_NoInlining_Shared()));
    }

    [Fact]
    public static void GenericInterfaceBase_GenericClassDerived_InliningVariants()
    {
        ValidateCaller("GenericInterfaceBase_GenericClassDerived_Inlining_Int_Int", new GenericInterfaceBaseCaller<int>(new GenericInterfaceBase_GenericClassDerived_Inlining<int, int>()));
        ValidateCaller("GenericInterfaceBase_GenericClassDerived_Inlining_Int_String", new GenericInterfaceBaseCaller<int>(new GenericInterfaceBase_GenericClassDerived_Inlining<int, string>()));
        ValidateCaller("GenericInterfaceBase_GenericClassDerived_Inlining_String_Int", new GenericInterfaceBaseCaller<string>(new GenericInterfaceBase_GenericClassDerived_Inlining<string, int>()));
        ValidateCaller("GenericInterfaceBase_GenericClassDerived_Inlining_String_String", new GenericInterfaceBaseCaller<string>(new GenericInterfaceBase_GenericClassDerived_Inlining<string, string>()));
    }

    [Fact]
    public static void GenericInterfaceBase_GenericClassDerived_NoInliningVariants()
    {
        ValidateCaller("GenericInterfaceBase_GenericClassDerived_NoInlining_Int_Int", new GenericInterfaceBaseCaller<int>(new GenericInterfaceBase_GenericClassDerived_NoInlining<int, int>()));
        ValidateCaller("GenericInterfaceBase_GenericClassDerived_NoInlining_Int_String", new GenericInterfaceBaseCaller<int>(new GenericInterfaceBase_GenericClassDerived_NoInlining<int, string>()));
        ValidateCaller("GenericInterfaceBase_GenericClassDerived_NoInlining_String_Int", new GenericInterfaceBaseCaller<string>(new GenericInterfaceBase_GenericClassDerived_NoInlining<string, int>()));
        ValidateCaller("GenericInterfaceBase_GenericClassDerived_NoInlining_String_String", new GenericInterfaceBaseCaller<string>(new GenericInterfaceBase_GenericClassDerived_NoInlining<string, string>()));
    }

    [Fact]
    public static void GenericInterfaceBase_GenericStructDerived_InliningVariants()
    {
        ValidateCaller("GenericInterfaceBase_GenericStructDerived_Inlining_Int_Int", new GenericInterfaceBaseCaller<int>(new GenericInterfaceBase_GenericStructDerived_Inlining<int, int>()));
        ValidateCaller("GenericInterfaceBase_GenericStructDerived_Inlining_Int_String", new GenericInterfaceBaseCaller<int>(new GenericInterfaceBase_GenericStructDerived_Inlining<int, string>()));
        ValidateCaller("GenericInterfaceBase_GenericStructDerived_Inlining_String_Int", new GenericInterfaceBaseCaller<string>(new GenericInterfaceBase_GenericStructDerived_Inlining<string, int>()));
        ValidateCaller("GenericInterfaceBase_GenericStructDerived_Inlining_String_String", new GenericInterfaceBaseCaller<string>(new GenericInterfaceBase_GenericStructDerived_Inlining<string, string>()));
    }

    [Fact]
    public static void GenericInterfaceBase_GenericStructDerived_NoInliningVariants()
    {
        ValidateCaller("GenericInterfaceBase_GenericStructDerived_NoInlining_Int_Int", new GenericInterfaceBaseCaller<int>(new GenericInterfaceBase_GenericStructDerived_NoInlining<int, int>()));
        ValidateCaller("GenericInterfaceBase_GenericStructDerived_NoInlining_Int_String", new GenericInterfaceBaseCaller<int>(new GenericInterfaceBase_GenericStructDerived_NoInlining<int, string>()));
        ValidateCaller("GenericInterfaceBase_GenericStructDerived_NoInlining_String_Int", new GenericInterfaceBaseCaller<string>(new GenericInterfaceBase_GenericStructDerived_NoInlining<string, int>()));
        ValidateCaller("GenericInterfaceBase_GenericStructDerived_NoInlining_String_String", new GenericInterfaceBaseCaller<string>(new GenericInterfaceBase_GenericStructDerived_NoInlining<string, string>()));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ValidateCaller(string scenarioName, IBaseMethodCaller caller)
    {
        ValidateNonSharedCall(scenarioName, caller);
        ValidateContextNonSharedCall(scenarioName, caller);
        ValidateContextSharedCall(scenarioName, caller);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ValidateNonSharedCall(string scenarioName, IBaseMethodCaller caller)
    {
        // Avoid using interpolated strings to make the IL size smaller for inlining.
        // This method must be inlined to properly test GVM devirtualization.
        Console.WriteLine("Testing {0}: {1}...", nameof(ValidateNonSharedCall), scenarioName);
        var value = scenarioName.Length;
        Equal(value, caller.Invoke(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ValidateContextNonSharedCall(string scenarioName, IBaseMethodCaller caller)
    {
        Console.WriteLine("Testing {0}: {1}...", nameof(ValidateContextNonSharedCall), scenarioName);
        var value = scenarioName;
        Equal(value, caller.Invoke(value));

        Equal(value, IconContextBridgeNonShared<string>.SameMethodSameClass(caller, value));
        Equal(value, IconContextBridgeNonShared<string>.SameMethodDifferentClass(caller, value));
        Equal(value, IconContextBridgeNonShared<string>.DifferentMethodSameClass(caller, value));
        Equal(value, IconContextBridgeNonShared<string>.DifferentMethodDifferentClass(caller, value));

        Equal(value, RuntimeLookupBridgeNonShared<string>.SameClassSameMethod(caller, value));
        Equal(value, RuntimeLookupBridgeNonShared<string>.SameClassDifferentMethod(caller, value));
        Equal(value, RuntimeLookupBridgeNonShared<string>.DifferentClassSameMethod(caller, value));
        Equal(value, RuntimeLookupBridgeNonShared<string>.DifferentClassDifferentMethod(caller, value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ValidateContextSharedCall(string scenarioName, IBaseMethodCaller caller)
    {
        Console.WriteLine("Testing {0}: {1}...", nameof(ValidateContextSharedCall), scenarioName);
        var value = scenarioName;
        Equal(value, caller.Invoke(value));

        Equal(value, IconContextBridgeShared<string>.SameMethodSameClass(caller, value));
        Equal(value, IconContextBridgeShared<string>.SameMethodDifferentClass(caller, value));
        Equal(value, IconContextBridgeShared<string>.DifferentMethodSameClass(caller, value));
        Equal(value, IconContextBridgeShared<string>.DifferentMethodDifferentClass(caller, value));

        Equal(value, RuntimeLookupBridgeShared<string>.SameClassSameMethod(caller, value));
        Equal(value, RuntimeLookupBridgeShared<string>.SameClassDifferentMethod(caller, value));
        Equal(value, RuntimeLookupBridgeShared<string>.DifferentClassSameMethod(caller, value));
        Equal(value, RuntimeLookupBridgeShared<string>.DifferentClassDifferentMethod(caller, value));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Equal<T>(T expected, T actual, [CallerArgumentExpression(nameof(actual))] string testcase = "")
    {
        Console.WriteLine("Validating {0}...", testcase);
        Assert.Equal(expected, actual);
    }
}

internal static class IconContextBridgeNonShared<TMethod>
{
    public static TMethod SameMethodSameClass(IBaseMethodCaller caller, TMethod value)
    {
        return IconContextInvoker<int, TMethod>.SameMethodSameClass(caller, value);
    }

    public static TMethod SameMethodDifferentClass(IBaseMethodCaller caller, TMethod value)
    {
        return IconContextInvoker<int, TMethod>.SameMethodDifferentClass(caller, value);
    }

    public static TMethod DifferentMethodSameClass(IBaseMethodCaller caller, TMethod value)
    {
        return IconContextInvoker<int, TMethod>.DifferentMethodSameClass(caller, value);
    }

    public static TMethod DifferentMethodDifferentClass(IBaseMethodCaller caller, TMethod value)
    {
        return IconContextInvoker<int, TMethod>.DifferentMethodDifferentClass(caller, value);
    }
}

internal static class IconContextBridgeShared<TMethod>
{
    public static TMethod SameMethodSameClass(IBaseMethodCaller caller, TMethod value)
    {
        return IconContextInvoker<string, TMethod>.SameMethodSameClass(caller, value);
    }

    public static TMethod SameMethodDifferentClass(IBaseMethodCaller caller, TMethod value)
    {
        return IconContextInvoker<string, TMethod>.SameMethodDifferentClass(caller, value);
    }

    public static TMethod DifferentMethodSameClass(IBaseMethodCaller caller, TMethod value)
    {
        return IconContextInvoker<string, TMethod>.DifferentMethodSameClass(caller, value);
    }

    public static TMethod DifferentMethodDifferentClass(IBaseMethodCaller caller, TMethod value)
    {
        return IconContextInvoker<string, TMethod>.DifferentMethodDifferentClass(caller, value);
    }
}

internal static class IconContextInvoker<TContext, TMethod>
{
    public static TMethod SameMethodSameClass(IBaseMethodCaller caller, TMethod value)
    {
        return caller.Invoke(value);
    }

    public static TMethod SameMethodDifferentClass(IBaseMethodCaller caller, TMethod value)
    {
        return IconDifferentClassHelper<TContext>.SameMethod(caller, value);
    }

    public static TMethod DifferentMethodSameClass(IBaseMethodCaller caller, TMethod value)
    {
        return DifferentMethodSameClassCore(caller, value);
    }

    public static TMethod DifferentMethodDifferentClass(IBaseMethodCaller caller, TMethod value)
    {
        return IconDifferentClassHelper<TContext>.DifferentMethod(caller, value);
    }

    private static TMethod DifferentMethodSameClassCore(IBaseMethodCaller caller, TMethod value)
    {
        return caller.Invoke(value);
    }
}

internal static class IconDifferentClassHelper<TContext>
{
    public static TMethod SameMethod<TMethod>(IBaseMethodCaller caller, TMethod value)
    {
        return IconDifferentClassHost<TContext, TMethod>.InvokeSameMethod(caller, value);
    }

    public static TMethod DifferentMethod<TMethod>(IBaseMethodCaller caller, TMethod value)
    {
        return IconDifferentClassHost<TContext, TMethod>.InvokeDifferentMethod(caller, value);
    }

    private static class IconDifferentClassHost<TCtx, T>
    {
        public static T InvokeSameMethod(IBaseMethodCaller caller, T value)
        {
            return caller.Invoke(value);
        }

        public static T InvokeDifferentMethod(IBaseMethodCaller caller, T value)
        {
            return Inner.Invoke(caller, value);
        }

        private static class Inner
        {
            public static T Invoke(IBaseMethodCaller caller, T value)
            {
                return caller.Invoke(value);
            }
        }
    }
}

internal static class RuntimeLookupBridgeNonShared<TMethod>
{
    public static TMethod SameClassSameMethod(IBaseMethodCaller caller, TMethod value)
    {
        return RuntimeLookupDispatcher<int>.SameClassSameMethod(caller, value);
    }

    public static TMethod SameClassDifferentMethod(IBaseMethodCaller caller, TMethod value)
    {
        return RuntimeLookupDispatcher<int>.SameClassDifferentMethod(caller, value);
    }

    public static TMethod DifferentClassSameMethod(IBaseMethodCaller caller, TMethod value)
    {
        return RuntimeLookupDispatcher<int>.DifferentClassSameMethod(caller, value);
    }

    public static TMethod DifferentClassDifferentMethod(IBaseMethodCaller caller, TMethod value)
    {
        return RuntimeLookupDispatcher<int>.DifferentClassDifferentMethod(caller, value);
    }
}

internal static class RuntimeLookupBridgeShared<TMethod>
{
    public static TMethod SameClassSameMethod(IBaseMethodCaller caller, TMethod value)
    {
        return RuntimeLookupDispatcher<string>.SameClassSameMethod(caller, value);
    }

    public static TMethod SameClassDifferentMethod(IBaseMethodCaller caller, TMethod value)
    {
        return RuntimeLookupDispatcher<string>.SameClassDifferentMethod(caller, value);
    }

    public static TMethod DifferentClassSameMethod(IBaseMethodCaller caller, TMethod value)
    {
        return RuntimeLookupDispatcher<string>.DifferentClassSameMethod(caller, value);
    }

    public static TMethod DifferentClassDifferentMethod(IBaseMethodCaller caller, TMethod value)
    {
        return RuntimeLookupDispatcher<string>.DifferentClassDifferentMethod(caller, value);
    }
}

internal static class RuntimeLookupDispatcher<TContext>
{
    public static TMethod SameClassSameMethod<TMethod>(IBaseMethodCaller caller, TMethod value)
    {
        return RuntimeLookupThunks<TContext>.InvokeSameClassSameMethod(caller, value);
    }

    public static TMethod SameClassDifferentMethod<TMethod>(IBaseMethodCaller caller, TMethod value)
    {
        return RuntimeLookupThunks<TContext>.InvokeSameClassDifferentMethod(caller, value);
    }

    public static TMethod DifferentClassSameMethod<TMethod>(IBaseMethodCaller caller, TMethod value)
    {
        return RuntimeLookupThunks<TContext>.InvokeDifferentClassSameMethod(caller, value);
    }

    public static TMethod DifferentClassDifferentMethod<TMethod>(IBaseMethodCaller caller, TMethod value)
    {
        return RuntimeLookupThunks<TContext>.InvokeDifferentClassDifferentMethod(caller, value);
    }
}

internal static class RuntimeLookupThunks<TContext>
{
    public static T InvokeSameClassSameMethod<T>(IBaseMethodCaller caller, T value)
    {
        return RuntimeLookupHost<TContext>.SameClassSameMethod(caller, value);
    }

    public static T InvokeDifferentClassSameMethod<T>(IBaseMethodCaller caller, T value)
    {
        return RuntimeLookupHost<TContext>.DifferentClassSameMethod(caller, value);
    }

    public static T InvokeSameClassDifferentMethod<T>(IBaseMethodCaller caller, T value)
    {
        return RuntimeLookupHost<TContext>.SameClassDifferentMethod(caller, value);
    }

    public static T InvokeDifferentClassDifferentMethod<T>(IBaseMethodCaller caller, T value)
    {
        return RuntimeLookupHost<TContext>.DifferentClassDifferentMethod(caller, value);
    }
}

internal static class RuntimeLookupHost<TContext>
{
    public static T SameClassSameMethod<T>(IBaseMethodCaller caller, T value)
    {
        return caller.Invoke(value);
    }

    public static T DifferentClassSameMethod<T>(IBaseMethodCaller caller, T value)
    {
        return RuntimeLookupRemote<TContext, T>.SameMethod(caller, value);
    }

    public static T SameClassDifferentMethod<T>(IBaseMethodCaller caller, T value)
    {
        return SameClassDifferentMethodCore(caller, value);
    }

    public static T DifferentClassDifferentMethod<T>(IBaseMethodCaller caller, T value)
    {
        return RuntimeLookupRemote<TContext, T>.DifferentMethod(caller, value);
    }

    private static T SameClassDifferentMethodCore<T>(IBaseMethodCaller caller, T value)
    {
        return caller.Invoke(value);
    }
}

internal static class RuntimeLookupRemote<TContext, T>
{
    public static T SameMethod(IBaseMethodCaller caller, T value)
    {
        return caller.Invoke(value);
    }

    public static T DifferentMethod(IBaseMethodCaller caller, T value)
    {
        return RemoteInner.Invoke(caller, value);
    }

    private static class RemoteInner
    {
        public static T Invoke(IBaseMethodCaller caller, T value)
        {
            return caller.Invoke(value);
        }
    }
}

internal interface IBaseMethodCaller
{
    T Invoke<T>(T value);
}

internal sealed class ClassBaseCaller : IBaseMethodCaller
{
    private readonly NonGenericBaseClass _instance;

    public ClassBaseCaller(NonGenericBaseClass instance)
    {
        _instance = instance;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Invoke<T>(T value) => _instance.Process(value);
}

internal sealed class GenericClassBaseCaller<TBase> : IBaseMethodCaller
{
    private readonly GenericBaseClass<TBase> _instance;

    public GenericClassBaseCaller(GenericBaseClass<TBase> instance)
    {
        _instance = instance;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Invoke<T>(T value) => _instance.Process(value);
}

internal sealed class InterfaceBaseCaller : IBaseMethodCaller
{
    private readonly INonGenericBaseInterface _instance;

    public InterfaceBaseCaller(INonGenericBaseInterface instance)
    {
        _instance = instance;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Invoke<T>(T value) => _instance.Process(value);
}

internal sealed class GenericInterfaceBaseCaller<TBase> : IBaseMethodCaller
{
    private readonly IGenericBaseInterface<TBase> _instance;

    public GenericInterfaceBaseCaller(IGenericBaseInterface<TBase> instance)
    {
        _instance = instance;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Invoke<T>(T value) => _instance.Process(value);
}

internal abstract class NonGenericBaseClass
{
    public abstract T Process<T>(T value);
}

internal abstract class GenericBaseClass<TBase>
{
    public abstract T Process<T>(T value);
}

internal interface INonGenericBaseInterface
{
    T Process<T>(T value);
}

internal interface IGenericBaseInterface<TBase>
{
    T Process<T>(T value);
}

internal sealed class ClassBase_NonGenericDerived_Inlining : NonGenericBaseClass
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override T Process<T>(T value) => value;
}

internal sealed class ClassBase_NonGenericDerived_NoInlining : NonGenericBaseClass
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public override T Process<T>(T value) => value;
}

internal sealed class ClassBase_GenericDerived_Inlining<TDerived> : NonGenericBaseClass
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override T Process<T>(T value) => value;
}

internal sealed class ClassBase_GenericDerived_NoInlining<TDerived> : NonGenericBaseClass
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public override T Process<T>(T value) => value;
}

internal sealed class GenericClassBase_NonGenericDerived_Inlining_NonShared : GenericBaseClass<int>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override T Process<T>(T value) => value;
}

internal sealed class GenericClassBase_NonGenericDerived_Inlining_Shared : GenericBaseClass<string>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override T Process<T>(T value) => value;
}

internal sealed class GenericClassBase_NonGenericDerived_NoInlining_NonShared : GenericBaseClass<int>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public override T Process<T>(T value) => value;
}

internal sealed class GenericClassBase_NonGenericDerived_NoInlining_Shared : GenericBaseClass<string>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public override T Process<T>(T value) => value;
}

internal sealed class GenericClassBase_GenericDerived_Inlining<TBase, TDerived> : GenericBaseClass<TBase>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override T Process<T>(T value) => value;
}

internal sealed class GenericClassBase_GenericDerived_NoInlining<TBase, TDerived> : GenericBaseClass<TBase>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public override T Process<T>(T value) => value;
}

internal sealed class InterfaceBase_NonGenericClassDerived_Inlining : INonGenericBaseInterface
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Process<T>(T value) => value;
}

internal sealed class InterfaceBase_NonGenericClassDerived_NoInlining : INonGenericBaseInterface
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public T Process<T>(T value) => value;
}

internal readonly struct InterfaceBase_NonGenericStructDerived_Inlining : INonGenericBaseInterface
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Process<T>(T value) => value;
}

internal readonly struct InterfaceBase_NonGenericStructDerived_NoInlining : INonGenericBaseInterface
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public T Process<T>(T value) => value;
}

internal sealed class InterfaceBase_GenericClassDerived_Inlining<TDerived> : INonGenericBaseInterface
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Process<T>(T value) => value;
}

internal sealed class InterfaceBase_GenericClassDerived_NoInlining<TDerived> : INonGenericBaseInterface
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public T Process<T>(T value) => value;
}

internal readonly struct InterfaceBase_GenericStructDerived_Inlining<TDerived> : INonGenericBaseInterface
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Process<T>(T value) => value;
}

internal readonly struct InterfaceBase_GenericStructDerived_NoInlining<TDerived> : INonGenericBaseInterface
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public T Process<T>(T value) => value;
}

internal sealed class GenericInterfaceBase_NonGenericClassDerived_Inlining_NonShared : IGenericBaseInterface<int>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Process<T>(T value) => value;
}

internal sealed class GenericInterfaceBase_NonGenericClassDerived_Inlining_Shared : IGenericBaseInterface<string>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Process<T>(T value) => value;
}

internal sealed class GenericInterfaceBase_NonGenericClassDerived_NoInlining_NonShared : IGenericBaseInterface<int>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public T Process<T>(T value) => value;
}

internal sealed class GenericInterfaceBase_NonGenericClassDerived_NoInlining_Shared : IGenericBaseInterface<string>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public T Process<T>(T value) => value;
}

internal readonly struct GenericInterfaceBase_NonGenericStructDerived_Inlining_NonShared : IGenericBaseInterface<int>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Process<T>(T value) => value;
}

internal readonly struct GenericInterfaceBase_NonGenericStructDerived_Inlining_Shared : IGenericBaseInterface<string>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Process<T>(T value) => value;
}

internal readonly struct GenericInterfaceBase_NonGenericStructDerived_NoInlining_NonShared : IGenericBaseInterface<int>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public T Process<T>(T value) => value;
}

internal readonly struct GenericInterfaceBase_NonGenericStructDerived_NoInlining_Shared : IGenericBaseInterface<string>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public T Process<T>(T value) => value;
}

internal sealed class GenericInterfaceBase_GenericClassDerived_Inlining<TBase, TDerived> : IGenericBaseInterface<TBase>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Process<T>(T value) => value;
}

internal sealed class GenericInterfaceBase_GenericClassDerived_NoInlining<TBase, TDerived> : IGenericBaseInterface<TBase>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public T Process<T>(T value) => value;
}

internal readonly struct GenericInterfaceBase_GenericStructDerived_Inlining<TBase, TDerived> : IGenericBaseInterface<TBase>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Process<T>(T value) => value;
}

internal readonly struct GenericInterfaceBase_GenericStructDerived_NoInlining<TBase, TDerived> : IGenericBaseInterface<TBase>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public T Process<T>(T value) => value;
}
