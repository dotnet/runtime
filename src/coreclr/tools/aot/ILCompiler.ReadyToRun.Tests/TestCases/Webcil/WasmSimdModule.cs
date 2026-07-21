// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
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

    // Vector<T> is 16 bytes on wasm (128-bit vectors), so it uses the same v128 ABI as Vector128<T>.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector<int> EchoVectorT(Vector<int> value)
    {
        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector<int> CallEchoVectorT(Vector<int> value)
    {
        return EchoVectorT(value);
    }

    // A single-field struct wrapping a v128 is itself passed/returned as a v128, matching emscripten.
    public struct WrappedVector128
    {
        public Vector128<int> Value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static WrappedVector128 EchoWrapped(WrappedVector128 value)
    {
        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static WrappedVector128 CallEchoWrapped(WrappedVector128 value)
    {
        return EchoWrapped(value);
    }

    // The same unwrapping applies to a struct wrapping a 128-bit Vector<T>.
    public struct WrappedVectorT
    {
        public Vector<int> Value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static WrappedVectorT EchoWrappedVectorT(WrappedVectorT value)
    {
        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static WrappedVectorT CallEchoWrappedVectorT(WrappedVectorT value)
    {
        return EchoWrappedVectorT(value);
    }
}
