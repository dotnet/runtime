// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Runtime.Intrinsics.Wasm
{
    [CLSCompliant(false)]
    [Intrinsic]
    public abstract class WasmBase
    {
        public bool IsSupported { get; }

        // Constructing SIMD Values

        //[Intrinsic]
        //public static Vector128<byte> Constant(ImmByte16 imm);

        [Intrinsic]
        public static Vector128<byte> Constant(ulong p1, ulong p2) => Constant(p1, p2);

        // public static Vector128<sbyte>  SplatByte(int    x) => SplatByte(x);
        // public static Vector128<byte>   SplatByte(uint   x) => SplatByte(x);
        // public static Vector128<short>  SplatShort(int    x) => SplatShort(x);
        // public static Vector128<ushort> SplatShort(uint   x) => SplatShort(x);

        // public static Vector128<int>    Splat(int    x);
        // public static Vector128<uint>   Splat(uint   x);
        // public static Vector128<long>   Splat(long   x);
        // public static Vector128<ulong>  Splat(ulong  x);
        // public static Vector128<float>  Splat(float  x);
        // public static Vector128<double> Splat(double x);
        // public static Vector128<nint>   Splat(nint   x);
        // public static Vector128<nuint>  Splat(nuint  x);
    }

    [CLSCompliant(false)]
    public unsafe struct ImmByte16
    {
        public fixed byte bytes[16];
    }
}
