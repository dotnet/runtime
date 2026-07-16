// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Webcil;

// Exercises the wasm v128 calling convention (SIMD passed/returned/stored by value)
// without relying on any SIMD arithmetic intrinsics, so only the ABI/materialization
// paths are covered.
public static class WasmSimdModule
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector128<int> Echo(Vector128<int> value)
    {
        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector128<int> ThroughLocal(Vector128<int> value)
    {
        Vector128<int> local = value;
        return local;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Store(Vector128<int> value, ref Vector128<int> destination)
    {
        destination = value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector128<int> CallEcho(Vector128<int> value)
    {
        return Echo(value);
    }
}
