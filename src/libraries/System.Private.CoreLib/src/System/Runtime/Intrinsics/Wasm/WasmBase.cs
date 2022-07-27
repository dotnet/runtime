// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Runtime.Intrinsics.Wasm
{
    [Intrinsic]
    internal abstract class WasmBase
    {
        public static bool IsSupported { get => IsSupported; }

        // Constructing SIMD Values

        [Intrinsic]
        public static Vector128<byte> Constant(Vector128<byte> imm) => Constant(imm);

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

        [Intrinsic]
        public static Vector128<sbyte>  ReplaceLane(Vector128<sbyte>  a, byte imm, int    x) => ReplaceLane(a, imm, x);   // takes ImmLaneIdx16
        [Intrinsic]
        public static Vector128<byte>   ReplaceLane(Vector128<byte>   a, byte imm, uint   x) => ReplaceLane(a, imm, x);   // takes ImmLaneIdx16
        [Intrinsic]
        public static Vector128<short>  ReplaceLane(Vector128<short>  a, byte imm, int    x) => ReplaceLane(a, imm, x);   // takes ImmLaneIdx8
        [Intrinsic]
        public static Vector128<ushort> ReplaceLane(Vector128<ushort> a, byte imm, uint   x) => ReplaceLane(a, imm, x);   // takes ImmLaneIdx8
        [Intrinsic]
        public static Vector128<int>    ReplaceLane(Vector128<int>    a, byte imm, int    x) => ReplaceLane(a, imm, x);   // takes ImmLaneIdx4
        [Intrinsic]
        public static Vector128<int>    ReplaceLane(Vector128<uint>   a, byte imm, uint   x) => ReplaceLane(a, imm, x);   // takes ImmLaneIdx4
        [Intrinsic]
        public static Vector128<long>   ReplaceLane(Vector128<long>   a, byte imm, long   x) => ReplaceLane(a, imm, x);   // takes ImmLaneIdx2
        [Intrinsic]
        public static Vector128<ulong>  ReplaceLane(Vector128<ulong>  a, byte imm, ulong  x) => ReplaceLane(a, imm, x);   // takes ImmLaneIdx2
        [Intrinsic]
        public static Vector128<float>  ReplaceLane(Vector128<float>  a, byte imm, float  x) => ReplaceLane(a, imm, x);   // takes ImmLaneIdx4
        [Intrinsic]
        public static Vector128<double> ReplaceLane(Vector128<double> a, byte imm, double x) => ReplaceLane(a, imm, x);   // takes ImmLaneIdx2
        [Intrinsic]
        public static Vector128<nint>   ReplaceLane(Vector128<nint>   a, byte imm, nint   x) => ReplaceLane(a, imm, x);
        [Intrinsic]
        public static Vector128<nuint>  ReplaceLane(Vector128<nuint>  a, byte imm, nuint  x) => ReplaceLane(a, imm, x);

        [Intrinsic]
        public static Vector128<sbyte> Shuffle(Vector128<sbyte> a, Vector128<sbyte> b, Vector128<sbyte> imm) => Shuffle(a, b, imm);
        [Intrinsic]
        public static Vector128<byte>  Shuffle(Vector128<byte>  a, Vector128<byte>  b, Vector128<byte> imm) => Shuffle(a, b, imm);

        [Intrinsic]
        public static Vector128<sbyte> Swizzle(Vector128<sbyte> a, Vector128<sbyte> s) => Swizzle(a, s);
        [Intrinsic]
        public static Vector128<byte>  Swizzle(Vector128<byte>  a, Vector128<byte>  s) => Swizzle(a, s);

        [Intrinsic]
        public static Vector128<sbyte> And(Vector128<sbyte> left, Vector128<sbyte> right) => And(left, right);
        [Intrinsic]
        public static Vector128<byte> And(Vector128<byte> left, Vector128<byte> right) => And(left, right);
        [Intrinsic]
        public static Vector128<short> And(Vector128<short> left, Vector128<short> right) => And(left, right);
        [Intrinsic]
        public static Vector128<ushort> And(Vector128<ushort> left, Vector128<ushort> right) => And(left, right);
        [Intrinsic]
        public static Vector128<int> And(Vector128<int> left, Vector128<int> right) => And(left, right);
        [Intrinsic]
        public static Vector128<uint> And(Vector128<uint> left, Vector128<uint> right) => And(left, right);
        [Intrinsic]
        public static Vector128<long> And(Vector128<long> left, Vector128<long> right) => And(left, right);
        [Intrinsic]
        public static Vector128<ulong> And(Vector128<ulong> left, Vector128<ulong> right) => And(left, right);
        [Intrinsic]
        public static Vector128<float> And(Vector128<float> left, Vector128<float> right) => And(left, right);
        [Intrinsic]
        public static Vector128<double> And(Vector128<double> left, Vector128<double> right) => And(left, right);
        [Intrinsic]
        public static Vector128<nint> And(Vector128<nint> left, Vector128<nint> right) => And(left, right);
        [Intrinsic]
        public static Vector128<nuint> And(Vector128<nuint> left, Vector128<nuint> right) => And(left, right);
    }
}
