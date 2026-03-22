// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;

#pragma warning disable CA2211

public unsafe class NonGeneric : BaseClass, IStaticInterface, IInterface
{
    public static Action? InstanceBasicClosed;
    public static Action<NonGeneric>? InstanceBasicOpen;
    public static Func<int>? InstanceReturnClosed;
    public static Func<NonGeneric, int>? InstanceReturnOpen;
    public static Func<object, object>? InstanceParamClosed;
    public static Func<NonGeneric, object, object>? InstanceParamOpen;

    public static Func<object>? VirtualClosed;
    public static Func<NonGeneric, object>? VirtualOpen;

    public static Action? StaticBasicClosed;
    public static Func<int>? StaticReturnClosed;
    public static Func<object>? StaticParamClosed;
    public static Func<object, object>? StaticParamOpen;

    public static class Cache<T>
    {
        public static Func<Type>? InstanceGenericClosed;
        public static Func<NonGeneric, Type>? InstanceGenericOpen;
        public static Func<T, Type>? InstanceGenericParamClosed;
        public static Func<NonGeneric, T, Type>? InstanceGenericParamOpen;

        public static Func<Type>? StaticGenericClosed;
        public static Func<Type>? StaticGenericParamClosed;
        public static Func<T, Type>? StaticGenericParamOpen;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Action GetStaticBasicClosed() => RuntimeHelpers.GetDelegate((nint)(delegate*<void>)&StaticBasic, ref StaticBasicClosed);
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Func<int> GetStaticReturnClosed() => RuntimeHelpers.GetDelegate((nint)(delegate*<int>)&StaticReturn, ref StaticReturnClosed);
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Func<object> GetStaticParamClosed() => RuntimeHelpers.GetDelegate((nint)(delegate*<object, object>)&StaticParam, ref StaticParamClosed);
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Func<object, object> GetStaticParamOpen() => RuntimeHelpers.GetDelegate((nint)(delegate*<object, object>)&StaticParam, ref StaticParamOpen);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Func<Type> GetStaticGenericClosed<T>() => RuntimeHelpers.GetDelegate((nint)(delegate*<Type>)&StaticGeneric<T>, ref Cache<T>.StaticGenericClosed);
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Func<Type> GetStaticGenericParamClosed<T>() => RuntimeHelpers.GetDelegate((nint)(delegate*<T, Type>)&StaticGenericParam<T>, ref Cache<T>.StaticGenericParamClosed);
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Func<T, Type> GetStaticGenericParamOpen<T>() => RuntimeHelpers.GetDelegate((nint)(delegate*<T, Type>)&StaticGenericParam<T>, ref Cache<T>.StaticGenericParamOpen);

    public void InstanceBasic() { }
    public int InstanceReturn() => Constants.TestInt;
    public object InstanceParam(object obj) => obj;
    public Type InstanceGeneric<T>() => typeof(T);
    public Type InstanceGenericParam<T>(T val) => typeof(T);

    public virtual object Virtual() => Constants.TestString;

    public static void StaticBasic() { }
    public static int StaticReturn() => Constants.TestInt;
    public static object StaticParam(object obj) => obj;
    public static Type StaticGeneric<T>() => typeof(T);
    public static Type StaticGenericParam<T>(T val) => typeof(T);

    public object InterfaceInstance() => Constants.TestString;
    public object InterfaceInstanceDimOverriden() => Constants.TestString;

    public static object InterfaceStaticAbstract() => Constants.TestString;
    public static object InterfaceStaticVirtualOverriden() => Constants.TestString;

    public override object BaseAbstract() => Constants.TestString;
    public override object BaseVirtualOverriden() => Constants.TestString;
}

public unsafe class Generic<T> : BaseClass, IStaticInterface, IInterface
{
    public static Action? InstanceBasicClosed;
    public static Action<Generic<T>>? InstanceBasicOpen;
    public static Func<int>? InstanceReturnClosed;
    public static Func<Generic<T>, int>? InstanceReturnOpen;
    public static Func<object, object>? InstanceParamClosed;
    public static Func<Generic<T>, object, object>? InstanceParamOpen;

    public static Func<object>? VirtualClosed;
    public static Func<Generic<T>, object>? VirtualOpen;

    public static Action? StaticBasicClosed;
    public static Func<int>? StaticReturnClosed;
    public static Func<object>? StaticParamClosed;
    public static Func<object, object>? StaticParamOpen;

    public static Func<Type>? InstanceGenericClosed;
    public static Func<Generic<T>, Type>? InstanceGenericOpen;
    public static Func<T, Type>? InstanceGenericParamClosed;
    public static Func<Generic<T>, T, Type>? InstanceGenericParamOpen;

    public static Func<Type>? StaticGenericClosed;
    public static Func<Type>? StaticGenericParamClosed;
    public static Func<T, Type>? StaticGenericParamOpen;

    public static class Cache<T2>
    {
        public static Func<(Type, Type)>? InstanceDoubleGenericClosed;
        public static Func<Generic<T>, (Type, Type)>? InstanceDoubleGenericOpen;

        public static Func<(Type, Type)>? StaticDoubleGenericClosed;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Action GetStaticBasicClosed() => RuntimeHelpers.GetDelegate((nint)(delegate*<void>)&StaticBasic, ref StaticBasicClosed);
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Func<int> GetStaticReturnClosed() => RuntimeHelpers.GetDelegate((nint)(delegate*<int>)&StaticReturn, ref StaticReturnClosed);
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Func<object> GetStaticParamClosed() => RuntimeHelpers.GetDelegate((nint)(delegate*<object, object>)&StaticParam, ref StaticParamClosed);
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Func<object, object> GetStaticParamOpen() => RuntimeHelpers.GetDelegate((nint)(delegate*<object, object>)&StaticParam, ref StaticParamOpen);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Func<Type> GetStaticGenericClosed() => RuntimeHelpers.GetDelegate((nint)(delegate*<Type>)&StaticGeneric, ref StaticGenericClosed);
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Func<Type> GetStaticGenericParamClosed() => RuntimeHelpers.GetDelegate((nint)(delegate*<T, Type>)&StaticGenericParam, ref StaticGenericParamClosed);
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Func<T, Type> GetStaticGenericParamOpen() => RuntimeHelpers.GetDelegate((nint)(delegate*<T, Type>)&StaticGenericParam, ref StaticGenericParamOpen);
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Func<(Type, Type)> GetStaticDoubleGenericOpen<T2>() => RuntimeHelpers.GetDelegate((nint)(delegate*<(Type, Type)>)&StaticDoubleGeneric<T2>, ref Cache<T2>.StaticDoubleGenericClosed);

    public void InstanceBasic() { }
    public int InstanceReturn() => Constants.TestInt;
    public object InstanceParam(object obj) => obj;
    public Type InstanceGeneric() => typeof(T);
    public Type InstanceGenericParam(T val) => typeof(T);
    public (Type, Type) InstanceDoubleGeneric<T2>() => (typeof(T), typeof(T2));

    public virtual object Virtual() => Constants.TestString;

    public static void StaticBasic() { }
    public static int StaticReturn() => Constants.TestInt;
    public static object StaticParam(object obj) => obj;
    public static Type StaticGeneric() => typeof(T);
    public static Type StaticGenericParam(T val) => typeof(T);
    public static (Type, Type) StaticDoubleGeneric<T2>() => (typeof(T), typeof(T2));

    public object InterfaceInstance() => Constants.TestString;
    public object InterfaceInstanceDimOverriden() => Constants.TestString;

    public static object InterfaceStaticAbstract() => Constants.TestString;
    public static object InterfaceStaticVirtualOverriden() => Constants.TestString;

    public override object BaseAbstract() => Constants.TestString;
    public override object BaseVirtualOverriden() => Constants.TestString;
}

public unsafe struct NonGenericStruct : IStaticInterface, IInterface
{
    public static Action? InstanceBasicClosed;
    public static Action<NonGenericStruct>? InstanceBasicOpen;
    public static Func<int>? InstanceReturnClosed;
    public static Func<NonGenericStruct, int>? InstanceReturnOpen;
    public static Func<object, object>? InstanceParamClosed;
    public static Func<NonGenericStruct, object, object>? InstanceParamOpen;

    public static Action? StaticBasicClosed;
    public static Func<int>? StaticReturnClosed;
    public static Func<object>? StaticParamClosed;
    public static Func<object, object>? StaticParamOpen;

    public static class Cache<T>
    {
        public static Func<Type>? InstanceGenericClosed;
        public static Func<NonGenericStruct, Type>? InstanceGenericOpen;
        public static Func<T, Type>? InstanceGenericParamClosed;
        public static Func<NonGenericStruct, T, Type>? InstanceGenericParamOpen;

        public static Func<Type>? StaticGenericClosed;
        public static Func<Type>? StaticGenericParamClosed;
        public static Func<T, Type>? StaticGenericParamOpen;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Action GetStaticBasicClosed() => RuntimeHelpers.GetDelegate((nint)(delegate*<void>)&StaticBasic, ref StaticBasicClosed);
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Func<int> GetStaticReturnClosed() => RuntimeHelpers.GetDelegate((nint)(delegate*<int>)&StaticReturn, ref StaticReturnClosed);
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Func<object> GetStaticParamClosed() => RuntimeHelpers.GetDelegate((nint)(delegate*<object, object>)&StaticParam, ref StaticParamClosed);
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Func<object, object> GetStaticParamOpen() => RuntimeHelpers.GetDelegate((nint)(delegate*<object, object>)&StaticParam, ref StaticParamOpen);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Func<Type> GetStaticGenericClosed<T>() => RuntimeHelpers.GetDelegate((nint)(delegate*<Type>)&StaticGeneric<T>, ref Cache<T>.StaticGenericClosed);
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Func<Type> GetStaticGenericParamClosed<T>() => RuntimeHelpers.GetDelegate((nint)(delegate*<T, Type>)&StaticGenericParam<T>, ref Cache<T>.StaticGenericParamClosed);
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Func<T, Type> GetStaticGenericParamOpen<T>() => RuntimeHelpers.GetDelegate((nint)(delegate*<T, Type>)&StaticGenericParam<T>, ref Cache<T>.StaticGenericParamOpen);

    public void InstanceBasic() { }
    public int InstanceReturn() => Constants.TestInt;
    public object InstanceParam(object obj) => obj;
    public Type InstanceGeneric<T>() => typeof(T);
    public Type InstanceGenericParam<T>(T val) => typeof(T);

    public static void StaticBasic() { }
    public static int StaticReturn() => Constants.TestInt;
    public static object StaticParam(object obj) => obj;
    public static Type StaticGeneric<T>() => typeof(T);
    public static Type StaticGenericParam<T>(T val) => typeof(T);

    public object InterfaceInstance() => Constants.TestString;
    public object InterfaceInstanceDimOverriden() => Constants.TestString;

    public static object InterfaceStaticAbstract() => Constants.TestString;
    public static object InterfaceStaticVirtualOverriden() => Constants.TestString;
}

public unsafe struct GenericStruct<T> : IStaticInterface, IInterface
{
    public static Action? InstanceBasicClosed;
    public static Action<GenericStruct<T>>? InstanceBasicOpen;
    public static Func<int>? InstanceReturnClosed;
    public static Func<GenericStruct<T>, int>? InstanceReturnOpen;
    public static Func<object, object>? InstanceParamClosed;
    public static Func<GenericStruct<T>, object, object>? InstanceParamOpen;

    public static Action? StaticBasicClosed;
    public static Func<int>? StaticReturnClosed;
    public static Func<object>? StaticParamClosed;
    public static Func<object, object>? StaticParamOpen;

    public static Func<Type>? InstanceGenericClosed;
    public static Func<GenericStruct<T>, Type>? InstanceGenericOpen;
    public static Func<T, Type>? InstanceGenericParamClosed;
    public static Func<GenericStruct<T>, T, Type>? InstanceGenericParamOpen;

    public static Func<Type>? StaticGenericClosed;
    public static Func<Type>? StaticGenericParamClosed;
    public static Func<T, Type>? StaticGenericParamOpen;

    public static class Cache<T2>
    {
        public static Func<(Type, Type)>? InstanceDoubleGenericClosed;
        public static Func<GenericStruct<T>, (Type, Type)>? InstanceDoubleGenericOpen;

        public static Func<(Type, Type)>? StaticDoubleGenericClosed;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Action GetStaticBasicClosed() => RuntimeHelpers.GetDelegate((nint)(delegate*<void>)&StaticBasic, ref StaticBasicClosed);
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Func<int> GetStaticReturnClosed() => RuntimeHelpers.GetDelegate((nint)(delegate*<int>)&StaticReturn, ref StaticReturnClosed);
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Func<object> GetStaticParamClosed() => RuntimeHelpers.GetDelegate((nint)(delegate*<object, object>)&StaticParam, ref StaticParamClosed);
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Func<object, object> GetStaticParamOpen() => RuntimeHelpers.GetDelegate((nint)(delegate*<object, object>)&StaticParam, ref StaticParamOpen);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Func<Type> GetStaticGenericClosed() => RuntimeHelpers.GetDelegate((nint)(delegate*<Type>)&StaticGeneric, ref StaticGenericClosed);
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Func<Type> GetStaticGenericParamClosed() => RuntimeHelpers.GetDelegate((nint)(delegate*<T, Type>)&StaticGenericParam, ref StaticGenericParamClosed);
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Func<T, Type> GetStaticGenericParamOpen() => RuntimeHelpers.GetDelegate((nint)(delegate*<T, Type>)&StaticGenericParam, ref StaticGenericParamOpen);
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Func<(Type, Type)> GetStaticDoubleGenericOpen<T2>() => RuntimeHelpers.GetDelegate((nint)(delegate*<(Type, Type)>)&StaticDoubleGeneric<T2>, ref Cache<T2>.StaticDoubleGenericClosed);

    public void InstanceBasic() { }
    public int InstanceReturn() => Constants.TestInt;
    public object InstanceParam(object obj) => obj;
    public Type InstanceGeneric() => typeof(T);
    public Type InstanceGenericParam(T val) => typeof(T);
    public (Type, Type) InstanceDoubleGeneric<T2>() => (typeof(T), typeof(T2));

    public static void StaticBasic() { }
    public static int StaticReturn() => Constants.TestInt;
    public static object StaticParam(object obj) => obj;
    public static Type StaticGeneric() => typeof(T);
    public static Type StaticGenericParam(T val) => typeof(T);
    public static (Type, Type) StaticDoubleGeneric<T2>() => (typeof(T), typeof(T2));

    public object InterfaceInstance() => Constants.TestString;
    public object InterfaceInstanceDimOverriden() => Constants.TestString;

    public static object InterfaceStaticAbstract() => Constants.TestString;
    public static object InterfaceStaticVirtualOverriden() => Constants.TestString;
}

public static unsafe class StaticResolver<T> where T : IStaticInterface
{
    public static Func<object>? InterfaceStaticAbstract;
    public static Func<object>? InterfaceStaticVirtual;
    public static Func<object>? InterfaceStaticVirtualOverriden;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Func<object> GetInterfaceStaticAbstract() => RuntimeHelpers.GetDelegate((nint)(delegate*<object>)&T.InterfaceStaticAbstract, ref InterfaceStaticAbstract);
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Func<object> GetInterfaceStaticVirtual() => RuntimeHelpers.GetDelegate((nint)(delegate*<object>)&T.InterfaceStaticVirtual, ref InterfaceStaticVirtual);
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Func<object> GetInterfaceStaticVirtualOverriden() => RuntimeHelpers.GetDelegate((nint)(delegate*<object>)&T.InterfaceStaticVirtualOverriden, ref InterfaceStaticVirtualOverriden);
}

public interface IStaticInterface
{
    public static abstract object InterfaceStaticAbstract();
    public static virtual object InterfaceStaticVirtual() => Constants.TestString;
    public static virtual object InterfaceStaticVirtualOverriden() => null!;
}

public interface IInterface
{
    public object InterfaceInstance();
    public object InterfaceInstanceDim() => Constants.TestString;
    public object InterfaceInstanceDimOverriden() => null!;
}

public abstract class BaseClass
{
    public abstract object BaseAbstract();
    public virtual object BaseVirtual() => Constants.TestString;
    public virtual object BaseVirtualOverriden() => null!;
}

public static class VirtualCache
{
    public static Func<IInterface, object>? InterfaceInstance;
    public static Func<IInterface, object>? InterfaceInstanceDim;
    public static Func<IInterface, object>? InterfaceInstanceDimOverriden;

    public static Func<BaseClass, object>? BaseAbstract;
    public static Func<BaseClass, object>? BaseVirtual;
    public static Func<BaseClass, object>? BaseVirtualOverriden;
}

public static class Constants
{
    public const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public;
    public const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.Public;
    public const int TestInt = 5;
    public const string TestString = "test";
}
