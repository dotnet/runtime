// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace Webcil;

public class WasmVirtualDispatchBase
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public virtual int Transform(int value) => value + 1;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int DirectTransform(int value) => value + 5;
}

public sealed class WasmVirtualDispatchDerived : WasmVirtualDispatchBase
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public override int Transform(int value) => value + 2;
}

public class WasmGenericVirtualDispatchBase<T>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public virtual int Transform(int value) => value + 3;
}

public sealed class WasmGenericVirtualDispatchDerived<T> : WasmGenericVirtualDispatchBase<T>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public override int Transform(int value) => value + 4;
}

public class WasmGenericMethodDispatchBase
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public virtual T Transform<T>(T value) => value;
}

public struct WasmIndirectArgument
{
    public nint First;
    public nint Second;
}

public class WasmCallingConventionDispatchBase
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public virtual int TransformPointer(nint value) => (int)value + 6;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public virtual int TransformStruct(WasmIndirectArgument value) => (int)(value.First + value.Second);
}

public static class WasmVirtualDispatch
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Invoke(WasmVirtualDispatchBase instance, int value) => instance.Transform(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int InvokeDirect(WasmVirtualDispatchBase instance, int value) => instance.DirectTransform(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int InvokeGeneric(WasmGenericVirtualDispatchBase<string> instance, int value) => instance.Transform(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int InvokeGenericMethod(WasmGenericMethodDispatchBase instance, int value) => instance.Transform(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int InvokePointer(WasmCallingConventionDispatchBase instance, nint value) => instance.TransformPointer(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int InvokeStruct(WasmCallingConventionDispatchBase instance, WasmIndirectArgument value) => instance.TransformStruct(value);
}
