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

public static class WasmDelegateConstructors
{
    public delegate int Transform(int value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Transform CreateOpenStatic() => new(WasmDelegateTarget.StaticTarget);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Transform CreateClosedInstance(WasmDelegateTarget target) => new(target.InstanceTarget);
}
