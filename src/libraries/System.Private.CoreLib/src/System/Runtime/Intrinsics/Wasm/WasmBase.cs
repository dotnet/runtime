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

        [Intrinsic]
        public static Vector128<sbyte>  Splat(sbyte  x) => Splat(x);
        [Intrinsic]
        public static Vector128<byte>   Splat(byte   x) => Splat(x);
        [Intrinsic]
        public static Vector128<short>  Splat(short  x) => Splat(x);
        [Intrinsic]
        public static Vector128<ushort> Splat(ushort x) => Splat(x);
        [Intrinsic]
        public static Vector128<int>    Splat(int    x) => Splat(x);
        [Intrinsic]
        public static Vector128<uint>   Splat(uint   x) => Splat(x);
        [Intrinsic]
        public static Vector128<long>   Splat(long   x) => Splat(x);
        [Intrinsic]
        public static Vector128<ulong>  Splat(ulong  x) => Splat(x);
        [Intrinsic]
        public static Vector128<float>  Splat(float  x) => Splat(x);
        [Intrinsic]
        public static Vector128<double> Splat(double x) => Splat(x);
        [Intrinsic]
        public static Vector128<nint>   Splat(nint   x) => Splat(x);
        [Intrinsic]
        public static Vector128<nuint>  Splat(nuint  x) => Splat(x);

        [Intrinsic]
        public static int    ExtractLane(Vector128<sbyte>  a, byte imm) => ExtractLane(a, imm);    // takes ImmLaneIdx16
        [Intrinsic]
        public static uint   ExtractLane(Vector128<byte>   a, byte imm) => ExtractLane(a, imm);    // takes ImmLaneIdx16
        [Intrinsic]
        public static int    ExtractLane(Vector128<short>  a, byte imm) => ExtractLane(a, imm);    // takes ImmLaneIdx8
        [Intrinsic]
        public static uint   ExtractLane(Vector128<ushort> a, byte imm) => ExtractLane(a, imm);    // takes ImmLaneIdx8
        [Intrinsic]
        public static int    ExtractLane(Vector128<int>    a, byte imm) => ExtractLane(a, imm);    // takes ImmLaneIdx4
        [Intrinsic]
        public static uint   ExtractLane(Vector128<uint>   a, byte imm) => ExtractLane(a, imm);    // takes ImmLaneIdx4
        [Intrinsic]
        public static long   ExtractLane(Vector128<long>   a, byte imm) => ExtractLane(a, imm);    // takes ImmLaneIdx2
        [Intrinsic]
        public static ulong  ExtractLane(Vector128<ulong>  a, byte imm) => ExtractLane(a, imm);    // takes ImmLaneIdx2
        [Intrinsic]
        public static float  ExtractLane(Vector128<float>  a, byte imm) => ExtractLane(a, imm);    // takes ImmLaneIdx4
        [Intrinsic]
        public static double ExtractLane(Vector128<double> a, byte imm) => ExtractLane(a, imm);    // takes ImmLaneIdx2
        [Intrinsic]
        public static nint   ExtractLane(Vector128<nint>   a, byte imm) => ExtractLane(a, imm);
        [Intrinsic]
        public static nuint  ExtractLane(Vector128<nuint>  a, byte imm) => ExtractLane(a, imm);
    }

    [CLSCompliant(false)]
    public unsafe struct ImmByte16
    {
        public fixed byte bytes[16];
    }
}
