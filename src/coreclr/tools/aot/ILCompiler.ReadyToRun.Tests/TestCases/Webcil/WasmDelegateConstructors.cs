// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace Webcil;

public sealed class WasmDelegateTarget
{
    private readonly int _state;

    public WasmDelegateTarget(int state)
    {
        _state = state;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int StaticTarget(int value) => value + 1;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int InstanceTarget(int value) => value + _state;
}

public struct WasmDelegateResult
{
    public int Value;
    public object Target;
}

public static class WasmDelegateConstructors
{
    public delegate WasmDelegateResult ReturnsStruct(int value);
    public delegate int Transform(int value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Transform CreateOpenStatic() => new(WasmDelegateTarget.StaticTarget);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Transform CreateClosedInstance(WasmDelegateTarget target) => new(target.InstanceTarget);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Transform CreateClosedStatic(WasmDelegateTarget target) => new(target.ClosedStaticTarget);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ReturnsStruct CreateClosedStaticRetBuf(WasmDelegateTarget target) =>
        new(target.ClosedStaticRetBufTarget);

}

public static class WasmDelegateTargetExtensions
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int ClosedStaticTarget(this WasmDelegateTarget target, int value) =>
        target.InstanceTarget(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static WasmDelegateResult ClosedStaticRetBufTarget(this WasmDelegateTarget target, int value) =>
        new WasmDelegateResult { Value = value, Target = target };
}
