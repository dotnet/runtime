// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Runtime.Intrinsics.Wasm
{
    [Intrinsic]
    internal abstract class PackedSimd
    {
        public static bool IsSupported { [Intrinsic] get { return false; } }

        [Intrinsic]
        public static Vector128<sbyte>  Splat(sbyte  value) => Splat(value);
        [Intrinsic]
        public static Vector128<byte>   Splat(byte   value) => Splat(value);
        [Intrinsic]
        public static Vector128<short>  Splat(short  value) => Splat(value);
        [Intrinsic]
        public static Vector128<ushort> Splat(ushort value) => Splat(value);
        [Intrinsic]
        public static Vector128<int>    Splat(int    value) => Splat(value);
        [Intrinsic]
        public static Vector128<uint>   Splat(uint   value) => Splat(value);
        [Intrinsic]
        public static Vector128<long>   Splat(long   value) => Splat(value);
        [Intrinsic]
        public static Vector128<ulong>  Splat(ulong  value) => Splat(value);
        [Intrinsic]
        public static Vector128<float>  Splat(float  value) => Splat(value);
        [Intrinsic]
        public static Vector128<double> Splat(double value) => Splat(value);
        [Intrinsic]
        public static Vector128<nint>   Splat(nint   value) => Splat(value);
        [Intrinsic]
        public static Vector128<nuint>  Splat(nuint  value) => Splat(value);

        [Intrinsic]
        public static int    ExtractLane(Vector128<sbyte>  value, byte index) => ExtractLane(value, index);    // takes ImmLaneIdx16
        [Intrinsic]
        public static uint   ExtractLane(Vector128<byte>   value, byte index) => ExtractLane(value, index);    // takes ImmLaneIdx16
        [Intrinsic]
        public static int    ExtractLane(Vector128<short>  value, byte index) => ExtractLane(value, index);    // takes ImmLaneIdx8
        [Intrinsic]
        public static uint   ExtractLane(Vector128<ushort> value, byte index) => ExtractLane(value, index);    // takes ImmLaneIdx8
        [Intrinsic]
        public static int    ExtractLane(Vector128<int>    value, byte index) => ExtractLane(value, index);    // takes ImmLaneIdx4
        [Intrinsic]
        public static uint   ExtractLane(Vector128<uint>   value, byte index) => ExtractLane(value, index);    // takes ImmLaneIdx4
        [Intrinsic]
        public static long   ExtractLane(Vector128<long>   value, byte index) => ExtractLane(value, index);    // takes ImmLaneIdx2
        [Intrinsic]
        public static ulong  ExtractLane(Vector128<ulong>  value, byte index) => ExtractLane(value, index);    // takes ImmLaneIdx2
        [Intrinsic]
        public static float  ExtractLane(Vector128<float>  value, byte index) => ExtractLane(value, index);    // takes ImmLaneIdx4
        [Intrinsic]
        public static double ExtractLane(Vector128<double> value, byte index) => ExtractLane(value, index);    // takes ImmLaneIdx2
        [Intrinsic]
        public static nint   ExtractLane(Vector128<nint>   value, byte index) => ExtractLane(value, index);
        [Intrinsic]
        public static nuint  ExtractLane(Vector128<nuint>  value, byte index) => ExtractLane(value, index);

        [Intrinsic]
        public static Vector128<sbyte>  ReplaceLane(Vector128<sbyte>  vector, byte imm, int    value) => ReplaceLane(vector, imm, value);   // takes ImmLaneIdx16
        [Intrinsic]
        public static Vector128<byte>   ReplaceLane(Vector128<byte>   vector, byte imm, uint   value) => ReplaceLane(vector, imm, value);   // takes ImmLaneIdx16
        [Intrinsic]
        public static Vector128<short>  ReplaceLane(Vector128<short>  vector, byte imm, int    value) => ReplaceLane(vector, imm, value);   // takes ImmLaneIdx8
        [Intrinsic]
        public static Vector128<ushort> ReplaceLane(Vector128<ushort> vector, byte imm, uint   value) => ReplaceLane(vector, imm, value);   // takes ImmLaneIdx8
        [Intrinsic]
        public static Vector128<int>    ReplaceLane(Vector128<int>    vector, byte imm, int    value) => ReplaceLane(vector, imm, value);   // takes ImmLaneIdx4
        [Intrinsic]
        public static Vector128<int>    ReplaceLane(Vector128<uint>   vector, byte imm, uint   value) => ReplaceLane(vector, imm, value);   // takes ImmLaneIdx4
        [Intrinsic]
        public static Vector128<long>   ReplaceLane(Vector128<long>   vector, byte imm, long   value) => ReplaceLane(vector, imm, value);   // takes ImmLaneIdx2
        [Intrinsic]
        public static Vector128<ulong>  ReplaceLane(Vector128<ulong>  vector, byte imm, ulong  value) => ReplaceLane(vector, imm, value);   // takes ImmLaneIdx2
        [Intrinsic]
        public static Vector128<float>  ReplaceLane(Vector128<float>  vector, byte imm, float  value) => ReplaceLane(vector, imm, value);   // takes ImmLaneIdx4
        [Intrinsic]
        public static Vector128<double> ReplaceLane(Vector128<double> vector, byte imm, double value) => ReplaceLane(vector, imm, value);   // takes ImmLaneIdx2
        [Intrinsic]
        public static Vector128<nint>   ReplaceLane(Vector128<nint>   vector, byte imm, nint   value) => ReplaceLane(vector, imm, value);
        [Intrinsic]
        public static Vector128<nuint>  ReplaceLane(Vector128<nuint>  vector, byte imm, nuint  value) => ReplaceLane(vector, imm, value);

        [Intrinsic]
        public static Vector128<sbyte> Shuffle(Vector128<sbyte> lower, Vector128<sbyte> upper, Vector128<sbyte> indices) => Shuffle(lower, upper, indices);
        [Intrinsic]
        public static Vector128<byte>  Shuffle(Vector128<byte>  lower, Vector128<byte>  upper, Vector128<byte>  indices) => Shuffle(lower, upper, indices);

        [Intrinsic]
        public static Vector128<sbyte> Swizzle(Vector128<sbyte> vector, Vector128<sbyte> indices) => Swizzle(vector, indices);
        [Intrinsic]
        public static Vector128<byte>  Swizzle(Vector128<byte>  vector, Vector128<byte>  indices) => Swizzle(vector, indices);

        [Intrinsic]
        public static Vector128<sbyte>  And(Vector128<sbyte>  left, Vector128<sbyte>  right) => And(left, right);
        [Intrinsic]
        public static Vector128<byte>   And(Vector128<byte>   left, Vector128<byte>   right) => And(left, right);
        [Intrinsic]
        public static Vector128<short>  And(Vector128<short>  left, Vector128<short>  right) => And(left, right);
        [Intrinsic]
        public static Vector128<ushort> And(Vector128<ushort> left, Vector128<ushort> right) => And(left, right);
        [Intrinsic]
        public static Vector128<int>    And(Vector128<int>    left, Vector128<int>    right) => And(left, right);
        [Intrinsic]
        public static Vector128<uint>   And(Vector128<uint>   left, Vector128<uint>   right) => And(left, right);
        [Intrinsic]
        public static Vector128<long>   And(Vector128<long>   left, Vector128<long>   right) => And(left, right);
        [Intrinsic]
        public static Vector128<ulong>  And(Vector128<ulong>  left, Vector128<ulong>  right) => And(left, right);
        [Intrinsic]
        public static Vector128<float>  And(Vector128<float>  left, Vector128<float>  right) => And(left, right);
        [Intrinsic]
        public static Vector128<double> And(Vector128<double> left, Vector128<double> right) => And(left, right);
        [Intrinsic]
        public static Vector128<nint>   And(Vector128<nint>   left, Vector128<nint>   right) => And(left, right);
        [Intrinsic]
        public static Vector128<nuint>  And(Vector128<nuint>  left, Vector128<nuint>  right) => And(left, right);

        [Intrinsic]
        public static int Bitmask(Vector128<sbyte>  value) => Bitmask(value);
        [Intrinsic]
        public static int Bitmask(Vector128<byte>   value) => Bitmask(value);
        [Intrinsic]
        public static int Bitmask(Vector128<short>  value) => Bitmask(value);
        [Intrinsic]
        public static int Bitmask(Vector128<ushort> value) => Bitmask(value);
        [Intrinsic]
        public static int Bitmask(Vector128<int>    value) => Bitmask(value);
        [Intrinsic]
        public static int Bitmask(Vector128<uint>   value) => Bitmask(value);
        [Intrinsic]
        public static int Bitmask(Vector128<long>   value) => Bitmask(value);
        [Intrinsic]
        public static int Bitmask(Vector128<ulong>  value) => Bitmask(value);
        [Intrinsic]
        public static int Bitmask(Vector128<nint>   value) => Bitmask(value);
        [Intrinsic]
        public static int Bitmask(Vector128<nuint>  value) => Bitmask(value);

        [Intrinsic]
        public static Vector128<sbyte>  CompareEqual(Vector128<sbyte>  left, Vector128<sbyte>  right) => CompareEqual(left, right);
        [Intrinsic]
        public static Vector128<byte>   CompareEqual(Vector128<byte>   left, Vector128<byte>   right) => CompareEqual(left, right);
        [Intrinsic]
        public static Vector128<short>  CompareEqual(Vector128<short>  left, Vector128<short>  right) => CompareEqual(left, right);
        [Intrinsic]
        public static Vector128<ushort> CompareEqual(Vector128<ushort> left, Vector128<ushort> right) => CompareEqual(left, right);
        [Intrinsic]
        public static Vector128<int>    CompareEqual(Vector128<int>    left, Vector128<int>    right) => CompareEqual(left, right);
        [Intrinsic]
        public static Vector128<uint>   CompareEqual(Vector128<uint>   left, Vector128<uint>   right) => CompareEqual(left, right);
        [Intrinsic]
        public static Vector128<long>   CompareEqual(Vector128<long>   left, Vector128<long>   right) => CompareEqual(left, right);
        [Intrinsic]
        public static Vector128<ulong>  CompareEqual(Vector128<ulong>  left, Vector128<ulong>  right) => CompareEqual(left, right);
        [Intrinsic]
        public static Vector128<float>  CompareEqual(Vector128<float>  left, Vector128<float>  right) => CompareEqual(left, right);
        [Intrinsic]
        public static Vector128<double> CompareEqual(Vector128<double> left, Vector128<double> right) => CompareEqual(left, right);
        [Intrinsic]
        public static Vector128<nint>   CompareEqual(Vector128<nint>   left, Vector128<nint>   right) => CompareEqual(left, right);
        [Intrinsic]
        public static Vector128<nuint>  CompareEqual(Vector128<nuint>  left, Vector128<nuint>  right) => CompareEqual(left, right);

        [Intrinsic]
        public static Vector128<sbyte>  CompareNotEqual(Vector128<sbyte>  left, Vector128<sbyte>  right) => CompareNotEqual(left, right);
        [Intrinsic]
        public static Vector128<byte>   CompareNotEqual(Vector128<byte>   left, Vector128<byte>   right) => CompareNotEqual(left, right);
        [Intrinsic]
        public static Vector128<short>  CompareNotEqual(Vector128<short>  left, Vector128<short>  right) => CompareNotEqual(left, right);
        [Intrinsic]
        public static Vector128<ushort> CompareNotEqual(Vector128<ushort> left, Vector128<ushort> right) => CompareNotEqual(left, right);
        [Intrinsic]
        public static Vector128<int>    CompareNotEqual(Vector128<int>    left, Vector128<int>    right) => CompareNotEqual(left, right);
        [Intrinsic]
        public static Vector128<uint>   CompareNotEqual(Vector128<uint>   left, Vector128<uint>   right) => CompareNotEqual(left, right);
        [Intrinsic]
        public static Vector128<long>   CompareNotEqual(Vector128<long>   left, Vector128<long>   right) => CompareNotEqual(left, right);
        [Intrinsic]
        public static Vector128<ulong>  CompareNotEqual(Vector128<ulong>  left, Vector128<ulong>  right) => CompareNotEqual(left, right);
        [Intrinsic]
        public static Vector128<float>  CompareNotEqual(Vector128<float>  left, Vector128<float>  right) => CompareNotEqual(left, right);
        [Intrinsic]
        public static Vector128<double> CompareNotEqual(Vector128<double> left, Vector128<double> right) => CompareNotEqual(left, right);
        [Intrinsic]
        public static Vector128<nint>   CompareNotEqual(Vector128<nint>   left, Vector128<nint>   right) => CompareNotEqual(left, right);
        [Intrinsic]
        public static Vector128<nuint>  CompareNotEqual(Vector128<nuint>  left, Vector128<nuint>  right) => CompareNotEqual(left, right);
    }
}
