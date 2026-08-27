// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace Webcil;

public class WasmVirtualDispatchBase
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public virtual int Transform(int value) => value + 1;
}

public sealed class WasmVirtualDispatchDerived : WasmVirtualDispatchBase
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public override int Transform(int value) => value + 2;
}

public static class WasmVirtualDispatch
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Invoke(WasmVirtualDispatchBase instance, int value) => instance.Transform(value);
}
