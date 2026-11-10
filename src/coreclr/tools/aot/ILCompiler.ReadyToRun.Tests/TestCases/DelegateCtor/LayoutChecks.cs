// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Webcil;

namespace DelegateCtor;

public static class LayoutChecks
{
    public delegate int ParameterDelegate(WasmDelegateResult value);
    public delegate WasmDelegateResult ReturnDelegate();
    public delegate int ByRefDelegate(ref WasmDelegateResult value);

    private static int ParameterTarget(WasmDelegateResult value) => value.Value;
    private static WasmDelegateResult ReturnTarget() => default;
    private static int ByRefTarget(ref WasmDelegateResult value) => value.Value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ParameterDelegate CreateParameter() => new(ParameterTarget);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ReturnDelegate CreateReturn() => new(ReturnTarget);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ByRefDelegate CreateByRef() => new(ByRefTarget);
}
