// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Runtime.Intrinsics.Wasm
{
    /// <summary>
    /// This class provides access to the WebAssembly packed SIMD instructions via intrinsics
    /// </summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class PackedSimd
    {
        public static bool IsSupported { [Intrinsic] get { return false; } }

        // Constructing SIMD Values

        // cut (lives somewhere else, use Vector128.Create)
        // public static Vector128<T> Constant(ImmByte[16] imm);

        /// <summary>
        ///   i8x16.splat or v128.const
        /// </summary>
        [Intrinsic]
        public static Vector128<sbyte>  Splat(sbyte  value) => Splat(value);
        /// <summary>
        ///   i8x16.splat or v128.const
        /// </summary>
        [Intrinsic]
        public static Vector128<byte>   Splat(byte   value) => Splat(value);
        /// <summary>
        ///   i16x8.splat or v128.const
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  Splat(short  value) => Splat(value);
        /// <summary>
        ///   i16x8.splat or v128.const
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> Splat(ushort value) => Splat(value);
        /// <summary>
        ///   i32x4.splat or v128.const
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    Splat(int    value) => Splat(value);
        /// <summary>
        ///   i32x4.splat or v128.const
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   Splat(uint   value) => Splat(value);
        /// <summary>
        ///   i64x2.splat or v128.const
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   Splat(long   value) => Splat(value);
        /// <summary>
        ///   i64x2.splat or v128.const
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  Splat(ulong  value) => Splat(value);
        /// <summary>
        ///   f32x4.splat or v128.const
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  Splat(float  value) => Splat(value);
        /// <summary>
        ///   f64x2.splat or v128.const
        /// </summary>
        [Intrinsic]
        public static Vector128<double> Splat(double value) => Splat(value);
        /// <summary>
        ///   i32x4.splat or v128.const
        /// </summary>
        [Intrinsic]
        public static Vector128<nint>   Splat(nint   value) => Splat(value);
        /// <summary>
        ///   i32x4.splat or v128.const
        /// </summary>
        [Intrinsic]
        public static Vector128<nuint>  Splat(nuint  value) => Splat(value);

        // Accessing lanes

        /// <summary>
        ///   i8x16.extract_lane_s
        /// </summary>
        [Intrinsic]
        public static int    ExtractLane(Vector128<sbyte>  value, [ConstantExpected(Max = (byte)(15))] byte index) => ExtractLane(value, index);    // takes ImmLaneIdx16
        /// <summary>
        ///   i8x16.extract_lane_u
        /// </summary>
        [Intrinsic]
        public static uint   ExtractLane(Vector128<byte>   value, [ConstantExpected(Max = (byte)(15))] byte index) => ExtractLane(value, index);    // takes ImmLaneIdx16
        /// <summary>
        ///   i16x8.extract_lane_s
        /// </summary>
        [Intrinsic]
        public static int    ExtractLane(Vector128<short>  value, [ConstantExpected(Max = (byte)(7))] byte index) => ExtractLane(value, index);    // takes ImmLaneIdx8
        /// <summary>
        ///   i16x8.extract_lane_u
        /// </summary>
        [Intrinsic]
        public static uint   ExtractLane(Vector128<ushort> value, [ConstantExpected(Max = (byte)(7))] byte index) => ExtractLane(value, index);    // takes ImmLaneIdx8
        /// <summary>
        ///   i32x4.extract_lane
        /// </summary>
        [Intrinsic]
        public static int    ExtractLane(Vector128<int>    value, [ConstantExpected(Max = (byte)(3))] byte index) => ExtractLane(value, index);    // takes ImmLaneIdx4
        /// <summary>
        ///   i32x4.extract_lane
        /// </summary>
        [Intrinsic]
        public static uint   ExtractLane(Vector128<uint>   value, [ConstantExpected(Max = (byte)(3))] byte index) => ExtractLane(value, index);    // takes ImmLaneIdx4
        /// <summary>
        ///   i64x2.extract_lane
        /// </summary>
        [Intrinsic]
        public static long   ExtractLane(Vector128<long>   value, [ConstantExpected(Max = (byte)(1))] byte index) => ExtractLane(value, index);    // takes ImmLaneIdx2
        /// <summary>
        ///   i64x2.extract_lane
        /// </summary>
        [Intrinsic]
        public static ulong  ExtractLane(Vector128<ulong>  value, [ConstantExpected(Max = (byte)(1))] byte index) => ExtractLane(value, index);    // takes ImmLaneIdx2
        /// <summary>
        ///   f32x4.extract_lane
        /// </summary>
        [Intrinsic]
        public static float  ExtractLane(Vector128<float>  value, [ConstantExpected(Max = (byte)(3))] byte index) => ExtractLane(value, index);    // takes ImmLaneIdx4
        /// <summary>
        ///   f64x2.extract_lane
        /// </summary>
        [Intrinsic]
        public static double ExtractLane(Vector128<double> value, [ConstantExpected(Max = (byte)(1))] byte index) => ExtractLane(value, index);    // takes ImmLaneIdx2
        /// <summary>
        ///   i32x4.extract_lane
        /// </summary>
        [Intrinsic]
        public static nint   ExtractLane(Vector128<nint>   value, [ConstantExpected(Max = (byte)(3))] byte index) => ExtractLane(value, index);
        /// <summary>
        ///   i32x4.extract_lane
        /// </summary>
        [Intrinsic]
        public static nuint  ExtractLane(Vector128<nuint>  value, [ConstantExpected(Max = (byte)(3))] byte index) => ExtractLane(value, index);

        /// <summary>
        ///   i8x16.replace_lane
        /// </summary>
        [Intrinsic]
        public static Vector128<sbyte>  ReplaceLane(Vector128<sbyte>  vector, [ConstantExpected(Max = (byte)(15))] byte imm, int    value) => ReplaceLane(vector, imm, value);   // takes ImmLaneIdx16
        /// <summary>
        ///   i8x16.replace_lane
        /// </summary>
        [Intrinsic]
        public static Vector128<byte>   ReplaceLane(Vector128<byte>   vector, [ConstantExpected(Max = (byte)(15))] byte imm, uint   value) => ReplaceLane(vector, imm, value);   // takes ImmLaneIdx16
        /// <summary>
        ///   i16x8.replace_lane
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  ReplaceLane(Vector128<short>  vector, [ConstantExpected(Max = (byte)(7))] byte imm, int    value) => ReplaceLane(vector, imm, value);   // takes ImmLaneIdx8
        /// <summary>
        ///   i16x8.replace_lane
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> ReplaceLane(Vector128<ushort> vector, [ConstantExpected(Max = (byte)(7))] byte imm, uint   value) => ReplaceLane(vector, imm, value);   // takes ImmLaneIdx8
        /// <summary>
        ///   i32x4.replace_lane
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    ReplaceLane(Vector128<int>    vector, [ConstantExpected(Max = (byte)(3))] byte imm, int    value) => ReplaceLane(vector, imm, value);   // takes ImmLaneIdx4
        /// <summary>
        ///   i32x4.replace_lane
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    ReplaceLane(Vector128<uint>   vector, [ConstantExpected(Max = (byte)(3))] byte imm, uint   value) => ReplaceLane(vector, imm, value);   // takes ImmLaneIdx4
        /// <summary>
        ///   i64x2.replace_lane
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   ReplaceLane(Vector128<long>   vector, [ConstantExpected(Max = (byte)(1))] byte imm, long   value) => ReplaceLane(vector, imm, value);   // takes ImmLaneIdx2
        /// <summary>
        ///   i64x2.replace_lane
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  ReplaceLane(Vector128<ulong>  vector, [ConstantExpected(Max = (byte)(1))] byte imm, ulong  value) => ReplaceLane(vector, imm, value);   // takes ImmLaneIdx2
        /// <summary>
        ///   f32x4.replace_lane
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  ReplaceLane(Vector128<float>  vector, [ConstantExpected(Max = (byte)(3))] byte imm, float  value) => ReplaceLane(vector, imm, value);   // takes ImmLaneIdx4
        /// <summary>
        ///   f64x2.replace_lane
        /// </summary>
        [Intrinsic]
        public static Vector128<double> ReplaceLane(Vector128<double> vector, [ConstantExpected(Max = (byte)(1))] byte imm, double value) => ReplaceLane(vector, imm, value);   // takes ImmLaneIdx2
        /// <summary>
        ///   i32x4.replace_lane
        /// </summary>
        [Intrinsic]
        public static Vector128<nint>   ReplaceLane(Vector128<nint>   vector, [ConstantExpected(Max = (byte)(3))] byte imm, nint   value) => ReplaceLane(vector, imm, value);
        /// <summary>
        ///   i32x4.replace_lane
        /// </summary>
        [Intrinsic]
        public static Vector128<nuint>  ReplaceLane(Vector128<nuint>  vector, [ConstantExpected(Max = (byte)(3))] byte imm, nuint  value) => ReplaceLane(vector, imm, value);

        /// <summary>
        ///   i8x16.shuffle
        /// </summary>
        [Intrinsic]
        public static Vector128<sbyte> Shuffle(Vector128<sbyte> lower, Vector128<sbyte> upper, Vector128<sbyte> indices) => Shuffle(lower, upper, indices);
        /// <summary>
        ///   i8x16.shuffle
        /// </summary>
        [Intrinsic]
        public static Vector128<byte>  Shuffle(Vector128<byte>  lower, Vector128<byte>  upper, Vector128<byte>  indices) => Shuffle(lower, upper, indices);

        /// <summary>
        ///   i8x16.swizzle
        /// </summary>
        [Intrinsic]
        public static Vector128<sbyte> Swizzle(Vector128<sbyte> vector, Vector128<sbyte> indices) => Swizzle(vector, indices);
        /// <summary>
        ///   i8x16.swizzle
        /// </summary>
        [Intrinsic]
        public static Vector128<byte>  Swizzle(Vector128<byte>  vector, Vector128<byte>  indices) => Swizzle(vector, indices);

        // Integer arithmetic

        /// <summary>
        ///   i8x16.add
        /// </summary>
        [Intrinsic]
        public static Vector128<sbyte>  Add(Vector128<sbyte>  left, Vector128<sbyte>  right) => Add(left, right);
        /// <summary>
        ///   i8x16.add
        /// </summary>
        [Intrinsic]
        public static Vector128<byte>   Add(Vector128<byte>   left, Vector128<byte>   right) => Add(left, right);
        /// <summary>
        ///   i16x8.add
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  Add(Vector128<short>  left, Vector128<short>  right) => Add(left, right);
        /// <summary>
        ///   i16x8.add
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> Add(Vector128<ushort> left, Vector128<ushort> right) => Add(left, right);
        /// <summary>
        ///   i32x4.add
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    Add(Vector128<int>    left, Vector128<int>    right) => Add(left, right);
        /// <summary>
        ///   i32x4.add
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   Add(Vector128<uint>   left, Vector128<uint>   right) => Add(left, right);
        /// <summary>
        ///   i64x2.add
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   Add(Vector128<long>   left, Vector128<long>   right) => Add(left, right);
        /// <summary>
        ///   i64x2.add
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  Add(Vector128<ulong>  left, Vector128<ulong>  right) => Add(left, right);
        /// <summary>
        ///   i32x4.add
        /// </summary>
        [Intrinsic]
        public static Vector128<nint>   Add(Vector128<nint>   left, Vector128<nint>   right) => Add(left, right);
        /// <summary>
        ///   i32x4.add
        /// </summary>
        [Intrinsic]
        public static Vector128<nuint>  Add(Vector128<nuint>  left, Vector128<nuint>  right) => Add(left, right);

        /// <summary>
        ///   i8x16.sub
        /// </summary>
        [Intrinsic]
        public static Vector128<sbyte>  Subtract(Vector128<sbyte>  left, Vector128<sbyte>  right) => Subtract(left, right);
        /// <summary>
        ///   i8x16.sub
        /// </summary>
        [Intrinsic]
        public static Vector128<byte>   Subtract(Vector128<byte>   left, Vector128<byte>   right) => Subtract(left, right);
        /// <summary>
        ///   i16x8.sub
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  Subtract(Vector128<short>  left, Vector128<short>  right) => Subtract(left, right);
        /// <summary>
        ///   i16x8.sub
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> Subtract(Vector128<ushort> left, Vector128<ushort> right) => Subtract(left, right);
        /// <summary>
        ///   i32x4.sub
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    Subtract(Vector128<int>    left, Vector128<int>    right) => Subtract(left, right);
        /// <summary>
        ///   i32x4.sub
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   Subtract(Vector128<uint>   left, Vector128<uint>   right) => Subtract(left, right);
        /// <summary>
        ///   i64x2.sub
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   Subtract(Vector128<long>   left, Vector128<long>   right) => Subtract(left, right);
        /// <summary>
        ///   i64x2.sub
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  Subtract(Vector128<ulong>  left, Vector128<ulong>  right) => Subtract(left, right);
        /// <summary>
        ///   i32x4.sub
        /// </summary>
        [Intrinsic]
        public static Vector128<nint>   Subtract(Vector128<nint>   left, Vector128<nint>   right) => Subtract(left, right);
        /// <summary>
        ///   i32x4.sub
        /// </summary>
        [Intrinsic]
        public static Vector128<nuint>  Subtract(Vector128<nuint>  left, Vector128<nuint>  right) => Subtract(left, right);

        /// <summary>
        ///   i16x8.mul
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  Multiply(Vector128<short>  left, Vector128<short>  right) => Multiply(left, right);
        /// <summary>
        ///   i16x8.mul
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> Multiply(Vector128<ushort> left, Vector128<ushort> right) => Multiply(left, right);
        /// <summary>
        ///   i32x4.mul
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    Multiply(Vector128<int>    left, Vector128<int>    right) => Multiply(left, right);
        /// <summary>
        ///   i32x4.mul
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   Multiply(Vector128<uint>   left, Vector128<uint>   right) => Multiply(left, right);
        /// <summary>
        ///   i64x2.mul
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   Multiply(Vector128<long>   left, Vector128<long>   right) => Multiply(left, right);
        /// <summary>
        ///   i64x2.mul
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  Multiply(Vector128<ulong>  left, Vector128<ulong>  right) => Multiply(left, right);
        /// <summary>
        ///   i32x4.mul
        /// </summary>
        [Intrinsic]
        public static Vector128<nint>   Multiply(Vector128<nint>   left, Vector128<nint>   right) => Multiply(left, right);
        /// <summary>
        ///   i32x4.mul
        /// </summary>
        [Intrinsic]
        public static Vector128<nuint>  Multiply(Vector128<nuint>  left, Vector128<nuint>  right) => Multiply(left, right);

        /// <summary>
        ///   i32x4.dot_i16x8_s
        /// </summary>
        [Intrinsic]
        public static Vector128<int> Dot(Vector128<short> left, Vector128<short> right) => Dot(left, right);

        /// <summary>
        ///   i8x16.neg
        /// </summary>
        [Intrinsic]
        public static Vector128<sbyte>  Negate(Vector128<sbyte>  value) => Negate(value);
        /// <summary>
        ///   i8x16.neg
        /// </summary>
        [Intrinsic]
        public static Vector128<byte>   Negate(Vector128<byte>   value) => Negate(value);
        /// <summary>
        ///   i16x8.neg
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  Negate(Vector128<short>  value) => Negate(value);
        /// <summary>
        ///   i16x8.neg
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> Negate(Vector128<ushort> value) => Negate(value);
        /// <summary>
        ///   i32x4.neg
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    Negate(Vector128<int>    value) => Negate(value);
        /// <summary>
        ///   i32x4.neg
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   Negate(Vector128<uint>   value) => Negate(value);
        /// <summary>
        ///   i64x2.neg
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   Negate(Vector128<long>   value) => Negate(value);
        /// <summary>
        ///   i64x2.neg
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  Negate(Vector128<ulong>  value) => Negate(value);
        /// <summary>
        ///   i32x4.neg
        /// </summary>
        [Intrinsic]
        public static Vector128<nint>   Negate(Vector128<nint>   value) => Negate(value);
        /// <summary>
        ///   i32x4.neg
        /// </summary>
        [Intrinsic]
        public static Vector128<nuint>  Negate(Vector128<nuint>  value) => Negate(value);

        // Extended integer arithmetic

        /// <summary>
        ///   i16x8.extmul_low_i8x16_s
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  MultiplyWideningLower(Vector128<sbyte>  left, Vector128<sbyte>  right) => MultiplyWideningLower(left, right);
        /// <summary>
        ///   i16x8.extmul_low_i8x16_u
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> MultiplyWideningLower(Vector128<byte>   left, Vector128<byte>   right) => MultiplyWideningLower(left, right);
        /// <summary>
        ///   i32x4.extmul_low_i16x8_s
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    MultiplyWideningLower(Vector128<short>  left, Vector128<short>  right) => MultiplyWideningLower(left, right);
        /// <summary>
        ///   i32x4.extmul_low_i16x8_u
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   MultiplyWideningLower(Vector128<ushort> left, Vector128<ushort> right) => MultiplyWideningLower(left, right);
        /// <summary>
        ///   i64x2.extmul_low_i32x4_s
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   MultiplyWideningLower(Vector128<int>    left, Vector128<int>    right) => MultiplyWideningLower(left, right);
        /// <summary>
        ///   i64x2.extmul_low_i32x4_u
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  MultiplyWideningLower(Vector128<uint>   left, Vector128<uint>   right) => MultiplyWideningLower(left, right);

        /// <summary>
        ///   i16x8.extmul_high_i8x16_s
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  MultiplyWideningUpper(Vector128<sbyte>  left, Vector128<sbyte>  right) => MultiplyWideningUpper(left, right);
        /// <summary>
        ///   i16x8.extmul_high_i8x16_u
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> MultiplyWideningUpper(Vector128<byte>   left, Vector128<byte>   right) => MultiplyWideningUpper(left, right);
        /// <summary>
        ///   i32x4.extmul_high_i16x8_s
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    MultiplyWideningUpper(Vector128<short>  left, Vector128<short>  right) => MultiplyWideningUpper(left, right);
        /// <summary>
        ///   i32x4.extmul_high_i16x8_u
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   MultiplyWideningUpper(Vector128<ushort> left, Vector128<ushort> right) => MultiplyWideningUpper(left, right);
        /// <summary>
        ///   i64x2.extmul_high_i32x4_s
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   MultiplyWideningUpper(Vector128<int>    left, Vector128<int>    right) => MultiplyWideningUpper(left, right);
        /// <summary>
        ///   i64x2.extmul_high_i32x4_u
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  MultiplyWideningUpper(Vector128<uint>   left, Vector128<uint>   right) => MultiplyWideningUpper(left, right);

        /// <summary>
        ///   i16x8.extadd_pairwise_i8x16_s
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  AddPairwiseWidening(Vector128<sbyte>  value) => AddPairwiseWidening(value);
        /// <summary>
        ///   i16x8.extadd_pairwise_i8x16_u
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> AddPairwiseWidening(Vector128<byte>   value) => AddPairwiseWidening(value);
        /// <summary>
        ///   i32x4.extadd_pairwise_i16x8_s
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    AddPairwiseWidening(Vector128<short>  value) => AddPairwiseWidening(value);
        /// <summary>
        ///   i32x4.extadd_pairwise_i16x8_u
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   AddPairwiseWidening(Vector128<ushort> value) => AddPairwiseWidening(value);

        // Saturating integer arithmetic

        /// <summary>
        ///   i8x16.add.sat.s
        /// </summary>
        public static Vector128<sbyte>  AddSaturate(Vector128<sbyte>  left, Vector128<sbyte>  right) => AddSaturate(left, right);
        /// <summary>
        ///   i8x16.add.sat.u
        /// </summary>
        public static Vector128<byte>   AddSaturate(Vector128<byte>   left, Vector128<byte>   right) => AddSaturate(left, right);
        /// <summary>
        ///   i16x8.add.sat.s
        /// </summary>
        public static Vector128<short>  AddSaturate(Vector128<short>  left, Vector128<short>  right) => AddSaturate(left, right);
        /// <summary>
        ///   i16x8.add.sat.u
        /// </summary>
        public static Vector128<ushort> AddSaturate(Vector128<ushort> left, Vector128<ushort> right) => AddSaturate(left, right);

        /// <summary>
        ///   i8x16.sub.sat.s
        /// </summary>
        public static Vector128<sbyte>  SubtractSaturate(Vector128<sbyte>  left, Vector128<sbyte>  right) => SubtractSaturate(left, right);
        /// <summary>
        ///   i8x16.sub.sat.u
        /// </summary>
        public static Vector128<byte>   SubtractSaturate(Vector128<byte>   left, Vector128<byte>   right) => SubtractSaturate(left, right);
        /// <summary>
        ///   i16x8.sub.sat.s
        /// </summary>
        public static Vector128<short>  SubtractSaturate(Vector128<short>  left, Vector128<short>  right) => SubtractSaturate(left, right);
        /// <summary>
        ///   i16x8.sub.sat.u
        /// </summary>
        public static Vector128<ushort> SubtractSaturate(Vector128<ushort> left, Vector128<ushort> right) => SubtractSaturate(left, right);

        /// <summary>
        ///   i16x8.q15mulr.sat.s
        /// </summary>
        public static Vector128<short> MultiplyRoundedSaturateQ15(Vector128<short> left, Vector128<short> right) => MultiplyRoundedSaturateQ15(left, right);

        /// <summary>
        ///   i8x16.min.s
        /// </summary>
        public static Vector128<sbyte>  Min(Vector128<sbyte>  left, Vector128<sbyte>  right) => Min(left, right);
        /// <summary>
        ///   i8x16.min.u
        /// </summary>
        public static Vector128<byte>   Min(Vector128<byte>   left, Vector128<byte>   right) => Min(left, right);
        /// <summary>
        ///   i16x8.min.s
        /// </summary>
        public static Vector128<short>  Min(Vector128<short>  left, Vector128<short>  right) => Min(left, right);
        /// <summary>
        ///   i16x8.min.u
        /// </summary>
        public static Vector128<ushort> Min(Vector128<ushort> left, Vector128<ushort> right) => Min(left, right);
        /// <summary>
        ///   i32x4.min.s
        /// </summary>
        public static Vector128<int>    Min(Vector128<int>    left, Vector128<int>    right) => Min(left, right);
        /// <summary>
        ///   i32x4.min.u
        /// </summary>
        public static Vector128<uint>   Min(Vector128<uint>   left, Vector128<uint>   right) => Min(left, right);

        /// <summary>
        ///   i8x16.max.s
        /// </summary>
        public static Vector128<sbyte>  Max(Vector128<sbyte>  left, Vector128<sbyte>  right) => Max(left, right);
        /// <summary>
        ///   i8x16.max.u
        /// </summary>
        public static Vector128<byte>   Max(Vector128<byte>   left, Vector128<byte>   right) => Max(left, right);
        /// <summary>
        ///   i16x8.max.s
        /// </summary>
        public static Vector128<short>  Max(Vector128<short>  left, Vector128<short>  right) => Max(left, right);
        /// <summary>
        ///   i16x8.max.u
        /// </summary>
        public static Vector128<ushort> Max(Vector128<ushort> left, Vector128<ushort> right) => Max(left, right);
        /// <summary>
        ///   i32x4.max.s
        /// </summary>
        public static Vector128<int>    Max(Vector128<int>    left, Vector128<int>    right) => Max(left, right);
        /// <summary>
        ///   i32x4.max.u
        /// </summary>
        public static Vector128<uint>   Max(Vector128<uint>   left, Vector128<uint>   right) => Max(left, right);

        /// <summary>
        ///   i8x16.avgr.u
        /// </summary>
        public static Vector128<byte>   AverageRounded(Vector128<byte>   left, Vector128<byte>   right) => AverageRounded(left, right);
        /// <summary>
        ///   i16x8.avgr.u
        /// </summary>
        public static Vector128<ushort> AverageRounded(Vector128<ushort> left, Vector128<ushort> right) => AverageRounded(left, right);

        /// <summary>
        ///   i8x16.abs
        /// </summary>
        public static Vector128<sbyte> Abs(Vector128<sbyte> value) => Abs(value);
        /// <summary>
        ///   i16x8.abs
        /// </summary>
        public static Vector128<short> Abs(Vector128<short> value) => Abs(value);
        /// <summary>
        ///   i32x4.abs
        /// </summary>
        public static Vector128<int>   Abs(Vector128<int>   value) => Abs(value);
        /// <summary>
        ///   i64x2.abs
        /// </summary>
        public static Vector128<long>  Abs(Vector128<long>  value) => Abs(value);
        /// <summary>
        ///   i32x4.abs
        /// </summary>
        public static Vector128<nint>  Abs(Vector128<nint>  value) => Abs(value);

        // Bit shifts

        /// <summary>
        ///   i8x16.shl
        /// </summary>
        [Intrinsic]
        public static Vector128<sbyte>  ShiftLeft(Vector128<sbyte>  value, int count) => ShiftLeft(value, count);
        /// <summary>
        ///   i8x16.shl
        /// </summary>
        [Intrinsic]
        public static Vector128<byte>   ShiftLeft(Vector128<byte>   value, int count) => ShiftLeft(value, count);
        /// <summary>
        ///   i16x8.shl
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  ShiftLeft(Vector128<short>  value, int count) => ShiftLeft(value, count);
        /// <summary>
        ///   i16x8.shl
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> ShiftLeft(Vector128<ushort> value, int count) => ShiftLeft(value, count);
        /// <summary>
        ///   i32x4.shl
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    ShiftLeft(Vector128<int>    value, int count) => ShiftLeft(value, count);
        /// <summary>
        ///   i32x4.shl
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   ShiftLeft(Vector128<uint>   value, int count) => ShiftLeft(value, count);
        /// <summary>
        ///   i64x2.shl
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   ShiftLeft(Vector128<long>   value, int count) => ShiftLeft(value, count);
        /// <summary>
        ///   i64x2.shl
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  ShiftLeft(Vector128<ulong>  value, int count) => ShiftLeft(value, count);
        /// <summary>
        ///   i32x4.shl
        /// </summary>
        [Intrinsic]
        public static Vector128<nint>   ShiftLeft(Vector128<nint>   value, int count) => ShiftLeft(value, count);
        /// <summary>
        ///   i32x4.shl
        /// </summary>
        [Intrinsic]
        public static Vector128<nuint>  ShiftLeft(Vector128<nuint>  value, int count) => ShiftLeft(value, count);

        /// <summary>
        ///   i8x16.shr_s
        /// </summary>
        [Intrinsic]
        public static Vector128<sbyte>  ShiftRightArithmetic(Vector128<sbyte>  value, int count) => ShiftRightArithmetic(value, count);
        /// <summary>
        ///   i8x16.shr_s
        /// </summary>
        [Intrinsic]
        public static Vector128<byte>   ShiftRightArithmetic(Vector128<byte>   value, int count) => ShiftRightArithmetic(value, count);
        /// <summary>
        ///   i16x8.shr_s
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  ShiftRightArithmetic(Vector128<short>  value, int count) => ShiftRightArithmetic(value, count);
        /// <summary>
        ///   i16x8.shr_s
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> ShiftRightArithmetic(Vector128<ushort> value, int count) => ShiftRightArithmetic(value, count);
        /// <summary>
        ///   i32x4.shr_s
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    ShiftRightArithmetic(Vector128<int>    value, int count) => ShiftRightArithmetic(value, count);
        /// <summary>
        ///   i32x4.shr_s
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   ShiftRightArithmetic(Vector128<uint>   value, int count) => ShiftRightArithmetic(value, count);
        /// <summary>
        ///   i64x2.shr_s
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   ShiftRightArithmetic(Vector128<long>   value, int count) => ShiftRightArithmetic(value, count);
        /// <summary>
        ///   i64x2.shr_s
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  ShiftRightArithmetic(Vector128<ulong>  value, int count) => ShiftRightArithmetic(value, count);
        /// <summary>
        ///   i32x4.shr_s
        /// </summary>
        [Intrinsic]
        public static Vector128<nint>   ShiftRightArithmetic(Vector128<nint>   value, int count) => ShiftRightArithmetic(value, count);
        /// <summary>
        ///   i32x4.shr_s
        /// </summary>
        [Intrinsic]
        public static Vector128<nuint>  ShiftRightArithmetic(Vector128<nuint>  value, int count) => ShiftRightArithmetic(value, count);

        /// <summary>
        ///   i8x16.shr_u
        /// </summary>
        [Intrinsic]
        public static Vector128<sbyte>  ShiftRightLogical(Vector128<sbyte>  value, int count) => ShiftRightLogical(value, count);
        /// <summary>
        ///   i8x16.shr_u
        /// </summary>
        [Intrinsic]
        public static Vector128<byte>   ShiftRightLogical(Vector128<byte>   value, int count) => ShiftRightLogical(value, count);
        /// <summary>
        ///   i16x8.shr_u
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  ShiftRightLogical(Vector128<short>  value, int count) => ShiftRightLogical(value, count);
        /// <summary>
        ///   i16x8.shr_u
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> ShiftRightLogical(Vector128<ushort> value, int count) => ShiftRightLogical(value, count);
        /// <summary>
        ///   i32x4.shr_u
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    ShiftRightLogical(Vector128<int>    value, int count) => ShiftRightLogical(value, count);
        /// <summary>
        ///   i32x4.shr_u
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   ShiftRightLogical(Vector128<uint>   value, int count) => ShiftRightLogical(value, count);
        /// <summary>
        ///   i64x2.shr_u
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   ShiftRightLogical(Vector128<long>   value, int count) => ShiftRightLogical(value, count);
        /// <summary>
        ///   i64x2.shr_u
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  ShiftRightLogical(Vector128<ulong>  value, int count) => ShiftRightLogical(value, count);
        /// <summary>
        ///   i32x4.shr_u
        /// </summary>
        [Intrinsic]
        public static Vector128<nint>   ShiftRightLogical(Vector128<nint>   value, int count) => ShiftRightLogical(value, count);
        /// <summary>
        ///   i32x4.shr_u
        /// </summary>
        [Intrinsic]
        public static Vector128<nuint>  ShiftRightLogical(Vector128<nuint>  value, int count) => ShiftRightLogical(value, count);

        // Bitwise operations

        /// <summary>
        ///   v128.and
        /// </summary>
        [Intrinsic]
        public static Vector128<sbyte>  And(Vector128<sbyte>  left, Vector128<sbyte>  right) => And(left, right);
        /// <summary>
        ///   v128.and
        /// </summary>
        [Intrinsic]
        public static Vector128<byte>   And(Vector128<byte>   left, Vector128<byte>   right) => And(left, right);
        /// <summary>
        ///   v128.and
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  And(Vector128<short>  left, Vector128<short>  right) => And(left, right);
        /// <summary>
        ///   v128.and
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> And(Vector128<ushort> left, Vector128<ushort> right) => And(left, right);
        /// <summary>
        ///   v128.and
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    And(Vector128<int>    left, Vector128<int>    right) => And(left, right);
        /// <summary>
        ///   v128.and
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   And(Vector128<uint>   left, Vector128<uint>   right) => And(left, right);
        /// <summary>
        ///   v128.and
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   And(Vector128<long>   left, Vector128<long>   right) => And(left, right);
        /// <summary>
        ///   v128.and
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  And(Vector128<ulong>  left, Vector128<ulong>  right) => And(left, right);
        /// <summary>
        ///   v128.and
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  And(Vector128<float>  left, Vector128<float>  right) => And(left, right);
        /// <summary>
        ///   v128.and
        /// </summary>
        [Intrinsic]
        public static Vector128<double> And(Vector128<double> left, Vector128<double> right) => And(left, right);
        /// <summary>
        ///   v128.and
        /// </summary>
        [Intrinsic]
        public static Vector128<nint>   And(Vector128<nint>   left, Vector128<nint>   right) => And(left, right);
        /// <summary>
        ///   v128.and
        /// </summary>
        [Intrinsic]
        public static Vector128<nuint>  And(Vector128<nuint>  left, Vector128<nuint>  right) => And(left, right);

        /// <summary>
        ///   v128.or
        /// </summary>
        [Intrinsic]
        public static Vector128<sbyte>  Or(Vector128<sbyte>  left, Vector128<sbyte>  right) => Or(left, right);
        /// <summary>
        ///   v128.or
        /// </summary>
        [Intrinsic]
        public static Vector128<byte>   Or(Vector128<byte>   left, Vector128<byte>   right) => Or(left, right);
        /// <summary>
        ///   v128.or
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  Or(Vector128<short>  left, Vector128<short>  right) => Or(left, right);
        /// <summary>
        ///   v128.or
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> Or(Vector128<ushort> left, Vector128<ushort> right) => Or(left, right);
        /// <summary>
        ///   v128.or
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    Or(Vector128<int>    left, Vector128<int>    right) => Or(left, right);
        /// <summary>
        ///   v128.or
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   Or(Vector128<uint>   left, Vector128<uint>   right) => Or(left, right);
        /// <summary>
        ///   v128.or
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   Or(Vector128<long>   left, Vector128<long>   right) => Or(left, right);
        /// <summary>
        ///   v128.or
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  Or(Vector128<ulong>  left, Vector128<ulong>  right) => Or(left, right);
        /// <summary>
        ///   v128.or
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  Or(Vector128<float>  left, Vector128<float>  right) => Or(left, right);
        /// <summary>
        ///   v128.or
        /// </summary>
        [Intrinsic]
        public static Vector128<double> Or(Vector128<double> left, Vector128<double> right) => Or(left, right);
        /// <summary>
        ///   v128.or
        /// </summary>
        [Intrinsic]
        public static Vector128<nint>   Or(Vector128<nint>   left, Vector128<nint>   right) => Or(left, right);
        /// <summary>
        ///   v128.or
        /// </summary>
        [Intrinsic]
        public static Vector128<nuint>  Or(Vector128<nuint>  left, Vector128<nuint>  right) => Or(left, right);

        /// <summary>
        ///   v128.xor
        /// </summary>
        [Intrinsic]
        public static Vector128<sbyte>  Xor(Vector128<sbyte>  left, Vector128<sbyte>  right) => Xor(left, right);
        /// <summary>
        ///   v128.xor
        /// </summary>
        [Intrinsic]
        public static Vector128<byte>   Xor(Vector128<byte>   left, Vector128<byte>   right) => Xor(left, right);
        /// <summary>
        ///   v128.xor
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  Xor(Vector128<short>  left, Vector128<short>  right) => Xor(left, right);
        /// <summary>
        ///   v128.xor
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> Xor(Vector128<ushort> left, Vector128<ushort> right) => Xor(left, right);
        /// <summary>
        ///   v128.xor
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    Xor(Vector128<int>    left, Vector128<int>    right) => Xor(left, right);
        /// <summary>
        ///   v128.xor
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   Xor(Vector128<uint>   left, Vector128<uint>   right) => Xor(left, right);
        /// <summary>
        ///   v128.xor
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   Xor(Vector128<long>   left, Vector128<long>   right) => Xor(left, right);
        /// <summary>
        ///   v128.xor
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  Xor(Vector128<ulong>  left, Vector128<ulong>  right) => Xor(left, right);
        /// <summary>
        ///   v128.xor
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  Xor(Vector128<float>  left, Vector128<float>  right) => Xor(left, right);
        /// <summary>
        ///   v128.xor
        /// </summary>
        [Intrinsic]
        public static Vector128<double> Xor(Vector128<double> left, Vector128<double> right) => Xor(left, right);
        /// <summary>
        ///   v128.xor
        /// </summary>
        [Intrinsic]
        public static Vector128<nint>   Xor(Vector128<nint>   left, Vector128<nint>   right) => Xor(left, right);
        /// <summary>
        ///   v128.xor
        /// </summary>
        [Intrinsic]
        public static Vector128<nuint>  Xor(Vector128<nuint>  left, Vector128<nuint>  right) => Xor(left, right);

        /// <summary>
        ///   v128.not
        /// </summary>
        [Intrinsic]
        public static Vector128<sbyte>  Not(Vector128<sbyte>  value) => Not(value);
        /// <summary>
        ///   v128.not
        /// </summary>
        [Intrinsic]
        public static Vector128<byte>   Not(Vector128<byte>   value) => Not(value);
        /// <summary>
        ///   v128.not
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  Not(Vector128<short>  value) => Not(value);
        /// <summary>
        ///   v128.not
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> Not(Vector128<ushort> value) => Not(value);
        /// <summary>
        ///   v128.not
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    Not(Vector128<int>    value) => Not(value);
        /// <summary>
        ///   v128.not
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   Not(Vector128<uint>   value) => Not(value);
        /// <summary>
        ///   v128.not
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   Not(Vector128<long>   value) => Not(value);
        /// <summary>
        ///   v128.not
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  Not(Vector128<ulong>  value) => Not(value);
        /// <summary>
        ///   v128.not
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  Not(Vector128<float>  value) => Not(value);
        /// <summary>
        ///   v128.not
        /// </summary>
        [Intrinsic]
        public static Vector128<double> Not(Vector128<double> value) => Not(value);
        /// <summary>
        ///   v128.not
        /// </summary>
        [Intrinsic]
        public static Vector128<nint>   Not(Vector128<nint>   value) => Not(value);
        /// <summary>
        ///   v128.not
        /// </summary>
        [Intrinsic]
        public static Vector128<nuint>  Not(Vector128<nuint>  value) => Not(value);

        /// <summary>
        ///   v128.andnot
        /// </summary>
        [Intrinsic]
        public static Vector128<sbyte>  AndNot(Vector128<sbyte>  left, Vector128<sbyte>  right) => AndNot(left, right);
        /// <summary>
        ///   v128.andnot
        /// </summary>
        [Intrinsic]
        public static Vector128<byte>   AndNot(Vector128<byte>   left, Vector128<byte>   right) => AndNot(left, right);
        /// <summary>
        ///   v128.andnot
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  AndNot(Vector128<short>  left, Vector128<short>  right) => AndNot(left, right);
        /// <summary>
        ///   v128.andnot
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> AndNot(Vector128<ushort> left, Vector128<ushort> right) => AndNot(left, right);
        /// <summary>
        ///   v128.andnot
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    AndNot(Vector128<int>    left, Vector128<int>    right) => AndNot(left, right);
        /// <summary>
        ///   v128.andnot
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   AndNot(Vector128<uint>   left, Vector128<uint>   right) => AndNot(left, right);
        /// <summary>
        ///   v128.andnot
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   AndNot(Vector128<long>   left, Vector128<long>   right) => AndNot(left, right);
        /// <summary>
        ///   v128.andnot
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  AndNot(Vector128<ulong>  left, Vector128<ulong>  right) => AndNot(left, right);
        /// <summary>
        ///   v128.andnot
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  AndNot(Vector128<float>  left, Vector128<float>  right) => AndNot(left, right);
        /// <summary>
        ///   v128.andnot
        /// </summary>
        [Intrinsic]
        public static Vector128<double> AndNot(Vector128<double> left, Vector128<double> right) => AndNot(left, right);
        /// <summary>
        ///   v128.andnot
        /// </summary>
        [Intrinsic]
        public static Vector128<nint>   AndNot(Vector128<nint>   left, Vector128<nint>   right) => AndNot(left, right);
        /// <summary>
        ///   v128.andnot
        /// </summary>
        [Intrinsic]
        public static Vector128<nuint>  AndNot(Vector128<nuint>  left, Vector128<nuint>  right) => AndNot(left, right);

        /// <summary>
        ///   v128.bitselect
        /// </summary>
        [Intrinsic]
        public static Vector128<sbyte>  BitwiseSelect(Vector128<sbyte>  left, Vector128<sbyte>  right, Vector128<sbyte>  select) => BitwiseSelect(left, right, select);
        /// <summary>
        ///   v128.bitselect
        /// </summary>
        [Intrinsic]
        public static Vector128<byte>   BitwiseSelect(Vector128<byte>   left, Vector128<byte>   right, Vector128<byte>   select) => BitwiseSelect(left, right, select);
        /// <summary>
        ///   v128.bitselect
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  BitwiseSelect(Vector128<short>  left, Vector128<short>  right, Vector128<short>  select) => BitwiseSelect(left, right, select);
        /// <summary>
        ///   v128.bitselect
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> BitwiseSelect(Vector128<ushort> left, Vector128<ushort> right, Vector128<ushort> select) => BitwiseSelect(left, right, select);
        /// <summary>
        ///   v128.bitselect
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    BitwiseSelect(Vector128<int>    left, Vector128<int>    right, Vector128<int>    select) => BitwiseSelect(left, right, select);
        /// <summary>
        ///   v128.bitselect
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   BitwiseSelect(Vector128<uint>   left, Vector128<uint>   right, Vector128<uint>   select) => BitwiseSelect(left, right, select);
        /// <summary>
        ///   v128.bitselect
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   BitwiseSelect(Vector128<long>   left, Vector128<long>   right, Vector128<long>   select) => BitwiseSelect(left, right, select);
        /// <summary>
        ///   v128.bitselect
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  BitwiseSelect(Vector128<ulong>  left, Vector128<ulong>  right, Vector128<ulong>  select) => BitwiseSelect(left, right, select);
        /// <summary>
        ///   v128.bitselect
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  BitwiseSelect(Vector128<float>  left, Vector128<float>  right, Vector128<float>  select) => BitwiseSelect(left, right, select);
        /// <summary>
        ///   v128.bitselect
        /// </summary>
        [Intrinsic]
        public static Vector128<double> BitwiseSelect(Vector128<double> left, Vector128<double> right, Vector128<double> select) => BitwiseSelect(left, right, select);
        /// <summary>
        ///   v128.bitselect
        /// </summary>
        [Intrinsic]
        public static Vector128<nint>   BitwiseSelect(Vector128<nint>   left, Vector128<nint>   right, Vector128<nint>   select) => BitwiseSelect(left, right, select);
        /// <summary>
        ///   v128.bitselect
        /// </summary>
        [Intrinsic]
        public static Vector128<nuint>  BitwiseSelect(Vector128<nuint>  left, Vector128<nuint>  right, Vector128<nuint>  select) => BitwiseSelect(left, right, select);

        /// <summary>
        ///   i8x16.popcnt
        /// </summary>
        [Intrinsic]
        public static Vector128<byte> PopCount(Vector128<byte> value) => PopCount(value);

        // Boolean horizontal reductions

        /// <summary>
        ///   v128.any_true
        /// </summary>
        [Intrinsic]
        public static bool AnyTrue(Vector128<sbyte>  value) => AnyTrue(value);
        /// <summary>
        ///   v128.any_true
        /// </summary>
        [Intrinsic]
        public static bool AnyTrue(Vector128<byte>   value) => AnyTrue(value);
        /// <summary>
        ///   v128.any_true
        /// </summary>
        [Intrinsic]
        public static bool AnyTrue(Vector128<short>  value) => AnyTrue(value);
        /// <summary>
        ///   v128.any_true
        /// </summary>
        [Intrinsic]
        public static bool AnyTrue(Vector128<ushort> value) => AnyTrue(value);
        /// <summary>
        ///   v128.any_true
        /// </summary>
        [Intrinsic]
        public static bool AnyTrue(Vector128<int>    value) => AnyTrue(value);
        /// <summary>
        ///   v128.any_true
        /// </summary>
        [Intrinsic]
        public static bool AnyTrue(Vector128<uint>   value) => AnyTrue(value);
        /// <summary>
        ///   v128.any_true
        /// </summary>
        [Intrinsic]
        public static bool AnyTrue(Vector128<long>   value) => AnyTrue(value);
        /// <summary>
        ///   v128.any_true
        /// </summary>
        [Intrinsic]
        public static bool AnyTrue(Vector128<ulong>  value) => AnyTrue(value);
        /// <summary>
        ///   v128.any_true
        /// </summary>
        [Intrinsic]
        public static bool AnyTrue(Vector128<float>  value) => AnyTrue(value);
        /// <summary>
        ///   v128.any_true
        /// </summary>
        [Intrinsic]
        public static bool AnyTrue(Vector128<double> value) => AnyTrue(value);
        /// <summary>
        ///   v128.any_true
        /// </summary>
        [Intrinsic]
        public static bool AnyTrue(Vector128<nint>   value) => AnyTrue(value);
        /// <summary>
        ///   v128.any_true
        /// </summary>
        [Intrinsic]
        public static bool AnyTrue(Vector128<nuint>  value) => AnyTrue(value);

        /// <summary>
        ///   i8x16.all_true
        /// </summary>
        [Intrinsic]
        public static bool AllTrue(Vector128<sbyte>  value) => AllTrue(value);
        /// <summary>
        ///   i8x16.all_true
        /// </summary>
        [Intrinsic]
        public static bool AllTrue(Vector128<byte>   value) => AllTrue(value);
        /// <summary>
        ///   i16x8.all_true
        /// </summary>
        [Intrinsic]
        public static bool AllTrue(Vector128<short>  value) => AllTrue(value);
        /// <summary>
        ///   i16x8.all_true
        /// </summary>
        [Intrinsic]
        public static bool AllTrue(Vector128<ushort> value) => AllTrue(value);
        /// <summary>
        ///   i32x4.all_true
        /// </summary>
        [Intrinsic]
        public static bool AllTrue(Vector128<int>    value) => AllTrue(value);
        /// <summary>
        ///   i32x4.all_true
        /// </summary>
        [Intrinsic]
        public static bool AllTrue(Vector128<uint>   value) => AllTrue(value);
        /// <summary>
        ///   i64x2.all_true
        /// </summary>
        [Intrinsic]
        public static bool AllTrue(Vector128<long>   value) => AllTrue(value);
        /// <summary>
        ///   i64x2.all_true
        /// </summary>
        [Intrinsic]
        public static bool AllTrue(Vector128<ulong>  value) => AllTrue(value);
        /// <summary>
        ///   i32x4.all_true
        /// </summary>
        [Intrinsic]
        public static bool AllTrue(Vector128<nint>   value) => AllTrue(value);
        /// <summary>
        ///   i32x4.all_true
        /// </summary>
        [Intrinsic]
        public static bool AllTrue(Vector128<nuint>  value) => AllTrue(value);

        // Bitmask extraction

        /// <summary>
        ///   i8x16.bitmask
        /// </summary>
        [Intrinsic]
        public static int Bitmask(Vector128<sbyte>  value) => Bitmask(value);
        /// <summary>
        ///   i8x16.bitmask
        /// </summary>
        [Intrinsic]
        public static int Bitmask(Vector128<byte>   value) => Bitmask(value);
        /// <summary>
        ///   i16x8.bitmask
        /// </summary>
        [Intrinsic]
        public static int Bitmask(Vector128<short>  value) => Bitmask(value);
        /// <summary>
        ///   i16x8.bitmask
        /// </summary>
        [Intrinsic]
        public static int Bitmask(Vector128<ushort> value) => Bitmask(value);
        /// <summary>
        ///   i32x4.bitmask
        /// </summary>
        [Intrinsic]
        public static int Bitmask(Vector128<int>    value) => Bitmask(value);
        /// <summary>
        ///   i32x4.bitmask
        /// </summary>
        [Intrinsic]
        public static int Bitmask(Vector128<uint>   value) => Bitmask(value);
        /// <summary>
        ///   i64x2.bitmask
        /// </summary>
        [Intrinsic]
        public static int Bitmask(Vector128<long>   value) => Bitmask(value);
        /// <summary>
        ///   i64x2.bitmask
        /// </summary>
        [Intrinsic]
        public static int Bitmask(Vector128<ulong>  value) => Bitmask(value);
        /// <summary>
        ///   i32x4.bitmask
        /// </summary>
        [Intrinsic]
        public static int Bitmask(Vector128<nint>   value) => Bitmask(value);
        /// <summary>
        ///   i32x4.bitmask
        /// </summary>
        [Intrinsic]
        public static int Bitmask(Vector128<nuint>  value) => Bitmask(value);

        // Comparisons

        /// <summary>
        ///   i8x16.eq
        /// </summary>
        [Intrinsic]
        public static Vector128<sbyte>  CompareEqual(Vector128<sbyte>  left, Vector128<sbyte>  right) => CompareEqual(left, right);
        /// <summary>
        ///   i8x16.eq
        /// </summary>
        [Intrinsic]
        public static Vector128<byte>   CompareEqual(Vector128<byte>   left, Vector128<byte>   right) => CompareEqual(left, right);
        /// <summary>
        ///   i16x8.eq
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  CompareEqual(Vector128<short>  left, Vector128<short>  right) => CompareEqual(left, right);
        /// <summary>
        ///   i16x8.eq
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> CompareEqual(Vector128<ushort> left, Vector128<ushort> right) => CompareEqual(left, right);
        /// <summary>
        ///   i32x4.eq
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    CompareEqual(Vector128<int>    left, Vector128<int>    right) => CompareEqual(left, right);
        /// <summary>
        ///   i32x4.eq
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   CompareEqual(Vector128<uint>   left, Vector128<uint>   right) => CompareEqual(left, right);
        /// <summary>
        ///   i64x2.eq
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   CompareEqual(Vector128<long>   left, Vector128<long>   right) => CompareEqual(left, right);
        /// <summary>
        ///   i64x2.eq
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  CompareEqual(Vector128<ulong>  left, Vector128<ulong>  right) => CompareEqual(left, right);
        /// <summary>
        ///   f32x4.eq
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  CompareEqual(Vector128<float>  left, Vector128<float>  right) => CompareEqual(left, right);
        /// <summary>
        ///   f64x2.eq
        /// </summary>
        [Intrinsic]
        public static Vector128<double> CompareEqual(Vector128<double> left, Vector128<double> right) => CompareEqual(left, right);
        /// <summary>
        ///   i32x4.eq
        /// </summary>
        [Intrinsic]
        public static Vector128<nint>   CompareEqual(Vector128<nint>   left, Vector128<nint>   right) => CompareEqual(left, right);
        /// <summary>
        ///   i32x4.eq
        /// </summary>
        [Intrinsic]
        public static Vector128<nuint>  CompareEqual(Vector128<nuint>  left, Vector128<nuint>  right) => CompareEqual(left, right);

        /// <summary>
        ///   i8x16.ne
        /// </summary>
        [Intrinsic]
        public static Vector128<sbyte>  CompareNotEqual(Vector128<sbyte>  left, Vector128<sbyte>  right) => CompareNotEqual(left, right);
        /// <summary>
        ///   i8x16.ne
        /// </summary>
        [Intrinsic]
        public static Vector128<byte>   CompareNotEqual(Vector128<byte>   left, Vector128<byte>   right) => CompareNotEqual(left, right);
        /// <summary>
        ///   i16x8.ne
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  CompareNotEqual(Vector128<short>  left, Vector128<short>  right) => CompareNotEqual(left, right);
        /// <summary>
        ///   i16x8.ne
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> CompareNotEqual(Vector128<ushort> left, Vector128<ushort> right) => CompareNotEqual(left, right);
        /// <summary>
        ///   i32x4.ne
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    CompareNotEqual(Vector128<int>    left, Vector128<int>    right) => CompareNotEqual(left, right);
        /// <summary>
        ///   i32x4.ne
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   CompareNotEqual(Vector128<uint>   left, Vector128<uint>   right) => CompareNotEqual(left, right);
        /// <summary>
        ///   i64x2.ne
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   CompareNotEqual(Vector128<long>   left, Vector128<long>   right) => CompareNotEqual(left, right);
        /// <summary>
        ///   i64x2.ne
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  CompareNotEqual(Vector128<ulong>  left, Vector128<ulong>  right) => CompareNotEqual(left, right);
        /// <summary>
        ///   f32x4.ne
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  CompareNotEqual(Vector128<float>  left, Vector128<float>  right) => CompareNotEqual(left, right);
        /// <summary>
        ///   f64x2.ne
        /// </summary>
        [Intrinsic]
        public static Vector128<double> CompareNotEqual(Vector128<double> left, Vector128<double> right) => CompareNotEqual(left, right);
        /// <summary>
        ///   i32x4.ne
        /// </summary>
        [Intrinsic]
        public static Vector128<nint>   CompareNotEqual(Vector128<nint>   left, Vector128<nint>   right) => CompareNotEqual(left, right);
        /// <summary>
        ///   i32x4.ne
        /// </summary>
        [Intrinsic]
        public static Vector128<nuint>  CompareNotEqual(Vector128<nuint>  left, Vector128<nuint>  right) => CompareNotEqual(left, right);

        /// <summary>
        ///   i8x16.lt_s
        /// </summary>
        [Intrinsic]
        public static Vector128<sbyte>  CompareLessThan(Vector128<sbyte>  left, Vector128<sbyte>  right) => CompareLessThan(left, right);
        /// <summary>
        ///   i8x16.lt_u
        /// </summary>
        [Intrinsic]
        public static Vector128<byte>   CompareLessThan(Vector128<byte>   left, Vector128<byte>   right) => CompareLessThan(left, right);
        /// <summary>
        ///   i16x8.lt_s
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  CompareLessThan(Vector128<short>  left, Vector128<short>  right) => CompareLessThan(left, right);
        /// <summary>
        ///   i16x8.lt_u
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> CompareLessThan(Vector128<ushort> left, Vector128<ushort> right) => CompareLessThan(left, right);
        /// <summary>
        ///   i32x4.lt_s
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    CompareLessThan(Vector128<int>    left, Vector128<int>    right) => CompareLessThan(left, right);
        /// <summary>
        ///   i32x4.lt_u
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   CompareLessThan(Vector128<uint>   left, Vector128<uint>   right) => CompareLessThan(left, right);
        /// <summary>
        ///   i64x2.lt_s
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   CompareLessThan(Vector128<long>   left, Vector128<long>   right) => CompareLessThan(left, right);
        /// <summary>
        ///   i64x2.lt_u
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  CompareLessThan(Vector128<ulong>  left, Vector128<ulong>  right) => CompareLessThan(left, right);
        /// <summary>
        ///   f32x4.lt
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  CompareLessThan(Vector128<float>  left, Vector128<float>  right) => CompareLessThan(left, right);
        /// <summary>
        ///   f64x2.lt
        /// </summary>
        [Intrinsic]
        public static Vector128<double> CompareLessThan(Vector128<double> left, Vector128<double> right) => CompareLessThan(left, right);
        /// <summary>
        ///   i32x4.lt_s
        /// </summary>
        [Intrinsic]
        public static Vector128<nint>   CompareLessThan(Vector128<nint>   left, Vector128<nint>   right) => CompareLessThan(left, right);
        /// <summary>
        ///   i32x4.lt_u
        /// </summary>
        [Intrinsic]
        public static Vector128<nuint>  CompareLessThan(Vector128<nuint>  left, Vector128<nuint>  right) => CompareLessThan(left, right);

        /// <summary>
        ///   i8x16.le_s
        /// </summary>
        [Intrinsic]
        public static Vector128<sbyte>  CompareLessThanOrEqual(Vector128<sbyte>  left, Vector128<sbyte>  right) => CompareLessThanOrEqual(left, right);
        /// <summary>
        ///   i8x16.le_u
        /// </summary>
        [Intrinsic]
        public static Vector128<byte>   CompareLessThanOrEqual(Vector128<byte>   left, Vector128<byte>   right) => CompareLessThanOrEqual(left, right);
        /// <summary>
        ///   i16x8.le_s
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  CompareLessThanOrEqual(Vector128<short>  left, Vector128<short>  right) => CompareLessThanOrEqual(left, right);
        /// <summary>
        ///   i16x8.le_u
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> CompareLessThanOrEqual(Vector128<ushort> left, Vector128<ushort> right) => CompareLessThanOrEqual(left, right);
        /// <summary>
        ///   i32x4.le_s
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    CompareLessThanOrEqual(Vector128<int>    left, Vector128<int>    right) => CompareLessThanOrEqual(left, right);
        /// <summary>
        ///   i32x4.le_u
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   CompareLessThanOrEqual(Vector128<uint>   left, Vector128<uint>   right) => CompareLessThanOrEqual(left, right);
        /// <summary>
        ///   i64x2.le_s
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   CompareLessThanOrEqual(Vector128<long>   left, Vector128<long>   right) => CompareLessThanOrEqual(left, right);
        /// <summary>
        ///   i64x2.le_u
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  CompareLessThanOrEqual(Vector128<ulong>  left, Vector128<ulong>  right) => CompareLessThanOrEqual(left, right);
        /// <summary>
        ///   f32x4.le
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  CompareLessThanOrEqual(Vector128<float>  left, Vector128<float>  right) => CompareLessThanOrEqual(left, right);
        /// <summary>
        ///   f64x2.le
        /// </summary>
        [Intrinsic]
        public static Vector128<double> CompareLessThanOrEqual(Vector128<double> left, Vector128<double> right) => CompareLessThanOrEqual(left, right);
        /// <summary>
        ///   i32x4.le_s
        /// </summary>
        [Intrinsic]
        public static Vector128<nint>   CompareLessThanOrEqual(Vector128<nint>   left, Vector128<nint>   right) => CompareLessThanOrEqual(left, right);
        /// <summary>
        ///   i32x4.le_u
        /// </summary>
        [Intrinsic]
        public static Vector128<nuint>  CompareLessThanOrEqual(Vector128<nuint>  left, Vector128<nuint>  right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        ///   i8x16.gt_s
        /// </summary>
        [Intrinsic]
        public static Vector128<sbyte>  CompareGreaterThan(Vector128<sbyte>  left, Vector128<sbyte>  right) => CompareGreaterThan(left, right);
        /// <summary>
        ///   i8x16.gt_u
        /// </summary>
        [Intrinsic]
        public static Vector128<byte>   CompareGreaterThan(Vector128<byte>   left, Vector128<byte>   right) => CompareGreaterThan(left, right);
        /// <summary>
        ///   i16x8.gt_s
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  CompareGreaterThan(Vector128<short>  left, Vector128<short>  right) => CompareGreaterThan(left, right);
        /// <summary>
        ///   i16x8.gt_u
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> CompareGreaterThan(Vector128<ushort> left, Vector128<ushort> right) => CompareGreaterThan(left, right);
        /// <summary>
        ///   i32x4.gt_s
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    CompareGreaterThan(Vector128<int>    left, Vector128<int>    right) => CompareGreaterThan(left, right);
        /// <summary>
        ///   i32x4.gt_u
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   CompareGreaterThan(Vector128<uint>   left, Vector128<uint>   right) => CompareGreaterThan(left, right);
        /// <summary>
        ///   i64x2.gt_s
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   CompareGreaterThan(Vector128<long>   left, Vector128<long>   right) => CompareGreaterThan(left, right);
        /// <summary>
        ///   i64x2.gt_u
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  CompareGreaterThan(Vector128<ulong>  left, Vector128<ulong>  right) => CompareGreaterThan(left, right);
        /// <summary>
        ///   f32x4.gt
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  CompareGreaterThan(Vector128<float>  left, Vector128<float>  right) => CompareGreaterThan(left, right);
        /// <summary>
        ///   f64x2.gt
        /// </summary>
        [Intrinsic]
        public static Vector128<double> CompareGreaterThan(Vector128<double> left, Vector128<double> right) => CompareGreaterThan(left, right);
        /// <summary>
        ///   i32x4.gt_s
        /// </summary>
        [Intrinsic]
        public static Vector128<nint>   CompareGreaterThan(Vector128<nint>   left, Vector128<nint>   right) => CompareGreaterThan(left, right);
        /// <summary>
        ///   i32x4.gt_u
        /// </summary>
        [Intrinsic]
        public static Vector128<nuint>  CompareGreaterThan(Vector128<nuint>  left, Vector128<nuint>  right) => CompareGreaterThan(left, right);

        /// <summary>
        ///   i8x16.ge_s
        /// </summary>
        [Intrinsic]
        public static Vector128<sbyte>  CompareGreaterThanOrEqual(Vector128<sbyte>  left, Vector128<sbyte>  right) => CompareGreaterThanOrEqual(left, right);
        /// <summary>
        ///   i8x16.ge_u
        /// </summary>
        [Intrinsic]
        public static Vector128<byte>   CompareGreaterThanOrEqual(Vector128<byte>   left, Vector128<byte>   right) => CompareGreaterThanOrEqual(left, right);
        /// <summary>
        ///   i16x8.ge_s
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  CompareGreaterThanOrEqual(Vector128<short>  left, Vector128<short>  right) => CompareGreaterThanOrEqual(left, right);
        /// <summary>
        ///   i16x8.ge_u
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> CompareGreaterThanOrEqual(Vector128<ushort> left, Vector128<ushort> right) => CompareGreaterThanOrEqual(left, right);
        /// <summary>
        ///   i32x4.ge_s
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    CompareGreaterThanOrEqual(Vector128<int>    left, Vector128<int>    right) => CompareGreaterThanOrEqual(left, right);
        /// <summary>
        ///   i32x4.ge_u
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   CompareGreaterThanOrEqual(Vector128<uint>   left, Vector128<uint>   right) => CompareGreaterThanOrEqual(left, right);
        /// <summary>
        ///   i32x4.ge_s
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   CompareGreaterThanOrEqual(Vector128<long>   left, Vector128<long>   right) => CompareGreaterThanOrEqual(left, right);
        /// <summary>
        ///   i64x2.ge_s
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  CompareGreaterThanOrEqual(Vector128<ulong>  left, Vector128<ulong>  right) => CompareGreaterThanOrEqual(left, right);
        /// <summary>
        ///   f32x4.ge
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  CompareGreaterThanOrEqual(Vector128<float>  left, Vector128<float>  right) => CompareGreaterThanOrEqual(left, right);
        /// <summary>
        ///   f64x2.ge
        /// </summary>
        public static Vector128<double> CompareGreaterThanOrEqual(Vector128<double> left, Vector128<double> right) => CompareGreaterThanOrEqual(left, right);
        /// <summary>
        ///   i32x4.ge_s
        /// </summary>
        [Intrinsic]
        public static Vector128<nint>   CompareGreaterThanOrEqual(Vector128<nint>   left, Vector128<nint>   right) => CompareGreaterThanOrEqual(left, right);
        /// <summary>
        ///   i32x4.ge_u
        /// </summary>
        [Intrinsic]
        public static Vector128<nuint>  CompareGreaterThanOrEqual(Vector128<nuint>  left, Vector128<nuint>  right) => CompareGreaterThanOrEqual(left, right);

       // Floating-point sign bit operations

        /// <summary>
        ///   f32x4.neg
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  Negate(Vector128<float>  value) => Negate(value);
        /// <summary>
        ///   f64x2.neg
        /// </summary>
        [Intrinsic]
        public static Vector128<double> Negate(Vector128<double> value) => Negate(value);

        /// <summary>
        ///   f32x4.abs
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  Abs(Vector128<float>  value) => Abs(value);
        /// <summary>
        ///   f64x2.abs
        /// </summary>
        [Intrinsic]
        public static Vector128<double> Abs(Vector128<double> value) => Abs(value);

        // Floating-point min and max

        /// <summary>
        ///   f32x4.min
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  Min(Vector128<float>  left, Vector128<float>  right) => Min(left, right);
        /// <summary>
        ///   f64x2.min
        /// </summary>
        [Intrinsic]
        public static Vector128<double> Min(Vector128<double> left, Vector128<double> right) => Min(left, right);

        /// <summary>
        ///   f32x4.max
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  Max(Vector128<float>  left, Vector128<float>  right) => Max(left, right);
        /// <summary>
        ///   f64x2.max
        /// </summary>
        [Intrinsic]
        public static Vector128<double> Max(Vector128<double> left, Vector128<double> right) => Max(left, right);

        /// <summary>
        ///   f32x4.pmin
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  PseudoMin(Vector128<float>  left, Vector128<float>  right) => PseudoMin(left, right);
        /// <summary>
        ///   f64x2.pmin
        /// </summary>
        [Intrinsic]
        public static Vector128<double> PseudoMin(Vector128<double> left, Vector128<double> right) => PseudoMin(left, right);

        /// <summary>
        ///   f32x4.pmax
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  PseudoMax(Vector128<float>  left, Vector128<float>  right) => PseudoMax(left, right);
        /// <summary>
        ///   f64x2.pmax
        /// </summary>
        [Intrinsic]
        public static Vector128<double> PseudoMax(Vector128<double> left, Vector128<double> right) => PseudoMax(left, right);

        // Floating-point arithmetic

        /// <summary>
        ///   f32x4.add
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  Add(Vector128<float>  left, Vector128<float>  right) => Add(left, right);
        /// <summary>
        ///   f64x2.add
        /// </summary>
        [Intrinsic]
        public static Vector128<double> Add(Vector128<double> left, Vector128<double> right) => Add(left, right);

        /// <summary>
        ///   f32x4.sub
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  Subtract(Vector128<float>  left, Vector128<float>  right) => Subtract(left, right);
        /// <summary>
        ///   f64x2.sub
        /// </summary>
        [Intrinsic]
        public static Vector128<double> Subtract(Vector128<double> left, Vector128<double> right) => Subtract(left, right);

        /// <summary>
        ///   f32x4.div
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  Divide(Vector128<float>  left, Vector128<float>  right) => Divide(left, right);
        /// <summary>
        ///   f64x2.div
        /// </summary>
        [Intrinsic]
        public static Vector128<double> Divide(Vector128<double> left, Vector128<double> right) => Divide(left, right);

        /// <summary>
        ///   f32x4.mul
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  Multiply(Vector128<float>  left, Vector128<float>  right) => Multiply(left, right);
        /// <summary>
        ///   f64x2.mul
        /// </summary>
        [Intrinsic]
        public static Vector128<double> Multiply(Vector128<double> left, Vector128<double> right) => Multiply(left, right);

        /// <summary>
        ///   f32x4.sqrt
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  Sqrt(Vector128<float>  value) => Sqrt(value);
        /// <summary>
        ///   f64x2.sqrt
        /// </summary>
        [Intrinsic]
        public static Vector128<double> Sqrt(Vector128<double> value) => Sqrt(value);

        /// <summary>
        ///   f32x4.ceil
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  Ceiling(Vector128<float>  value) => Ceiling(value);
        /// <summary>
        ///   f64x2.ceil
        /// </summary>
        [Intrinsic]
        public static Vector128<double> Ceiling(Vector128<double> value) => Ceiling(value);

        /// <summary>
        ///   f32x4.floor
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  Floor(Vector128<float>  value) => Floor(value);
        /// <summary>
        ///   f64x2.floor
        /// </summary>
        [Intrinsic]
        public static Vector128<double> Floor(Vector128<double> value) => Floor(value);

        /// <summary>
        ///   f32x4.trunc
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  Truncate(Vector128<float>  value) => Truncate(value);
        /// <summary>
        ///   f64x2.trunc
        /// </summary>
        [Intrinsic]
        public static Vector128<double> Truncate(Vector128<double> value) => Truncate(value);

        /// <summary>
        ///   f32x4.nearest
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  RoundToNearest(Vector128<float>  value) => RoundToNearest(value);
        /// <summary>
        ///   f64x2.nearest
        /// </summary>
        [Intrinsic]
        public static Vector128<double> RoundToNearest(Vector128<double> value) => RoundToNearest(value);

        // Conversions

        /// <summary>
        ///   f32x4.convert_i32x4_s
        /// </summary>
        [Intrinsic]
        public static Vector128<float> ConvertToSingle(Vector128<int>    value) => ConvertToSingle(value);
        /// <summary>
        ///   f32x4.convert_i32x4_u
        /// </summary>
        [Intrinsic]
        public static Vector128<float> ConvertToSingle(Vector128<uint>   value) => ConvertToSingle(value);
        /// <summary>
        /// f32x4.demote_f64x2_zero
        /// </summary>
        [Intrinsic]
        public static Vector128<float> ConvertToSingle(Vector128<double> value) => ConvertToSingle(value);

        /// <summary>
        ///   f64x2.convert_low_i32x4_s
        /// </summary>
        [Intrinsic]
        public static Vector128<double> ConvertToDoubleLower(Vector128<int>   value) => ConvertToDoubleLower(value);
        /// <summary>
        ///   f64x2.convert_low_i32x4_u
        /// </summary>
        [Intrinsic]
        public static Vector128<double> ConvertToDoubleLower(Vector128<uint>  value) => ConvertToDoubleLower(value);
        /// <summary>
        ///   f64x2.promote_low_f32x4
        /// </summary>
        [Intrinsic]
        public static Vector128<double> ConvertToDoubleLower(Vector128<float> value) => ConvertToDoubleLower(value);

        /// <summary>
        ///   i32x4.trunc_sat_f32x4_s
        /// </summary>
        [Intrinsic]
        public static Vector128<int>  ConvertToInt32Saturate(Vector128<float> value) => ConvertToInt32Saturate(value);
        /// <summary>
        ///   i32x4.trunc_sat_f32x4_u
        /// </summary>
        [Intrinsic]
        internal static Vector128<uint> ConvertToUnsignedInt32Saturate(Vector128<float> value) => ConvertToUnsignedInt32Saturate(value);

        /// <summary>
        ///   i32x4.trunc_sat_f64x2_s_zero
        /// </summary>
        [Intrinsic]
        public static Vector128<int>  ConvertToInt32Saturate(Vector128<double> value) => ConvertToInt32Saturate(value);
        /// <summary>
        ///   i32x4.trunc_sat_f64x2_u_zero
        /// </summary>
        [Intrinsic]
        internal static Vector128<uint> ConvertToUnsignedInt32Saturate(Vector128<double> value) => ConvertToUnsignedInt32Saturate(value);

        /// <summary>
        ///   i8x16.narrow_i16x8_s
        /// </summary>
        [Intrinsic]
        internal static Vector128<sbyte> ConvertNarrowingSignedSaturate(Vector128<short> lower, Vector128<short> upper) => ConvertNarrowingSignedSaturate(lower, upper);

        /// <summary>
        ///   i16x8.narrow_i32x4_s
        /// </summary>
        [Intrinsic]
        internal static Vector128<short> ConvertNarrowingSignedSaturate(Vector128<int>   lower, Vector128<int>   upper) => ConvertNarrowingSignedSaturate(lower, upper);

        /// <summary>
        ///   i8x16.narrow_i16x8_u
        /// </summary>
        [Intrinsic]
        internal static Vector128<byte>  ConvertNarrowingUnsignedSaturate(Vector128<short> lower, Vector128<short> upper) => ConvertNarrowingUnsignedSaturate(lower, upper);

        /// <summary>
        ///   i16x8.narrow_i32x4_u
        /// </summary>
        [Intrinsic]
        internal static Vector128<ushort> ConvertNarrowingUnsignedSaturate(Vector128<int>  lower, Vector128<int>   upper) => ConvertNarrowingUnsignedSaturate(lower, upper);

        /// <summary>
        ///   i16x8.extend_low_i8x16_s
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  SignExtendWideningLower(Vector128<sbyte>  value) => SignExtendWideningLower(value);
        /// <summary>
        ///   i16x8.extend_low_i8x16_s
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> SignExtendWideningLower(Vector128<byte>   value) => SignExtendWideningLower(value);
        /// <summary>
        ///   i32x4.extend_low_i16x8_s
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    SignExtendWideningLower(Vector128<short>  value) => SignExtendWideningLower(value);
        /// <summary>
        ///   i32x4.extend_low_i16x8_s
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   SignExtendWideningLower(Vector128<ushort> value) => SignExtendWideningLower(value);
        /// <summary>
        ///   i64x2.extend_low_i32x4_s
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   SignExtendWideningLower(Vector128<int>    value) => SignExtendWideningLower(value);
        /// <summary>
        ///   i64x2.extend_low_i32x4_s
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  SignExtendWideningLower(Vector128<uint>   value) => SignExtendWideningLower(value);

        /// <summary>
        ///   i16x8.extend_high_i8x16_s
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  SignExtendWideningUpper(Vector128<sbyte>  value) => SignExtendWideningUpper(value);
        /// <summary>
        ///   i16x8.extend_high_i8x16_s
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> SignExtendWideningUpper(Vector128<byte>   value) => SignExtendWideningUpper(value);
        /// <summary>
        ///   i32x4.extend_high_i16x8_s
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    SignExtendWideningUpper(Vector128<short>  value) => SignExtendWideningUpper(value);
        /// <summary>
        ///   i32x4.extend_high_i16x8_s
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   SignExtendWideningUpper(Vector128<ushort> value) => SignExtendWideningUpper(value);
        /// <summary>
        ///   i64x2.extend_high_i32x4_s
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   SignExtendWideningUpper(Vector128<int>    value) => SignExtendWideningUpper(value);
        /// <summary>
        ///   i64x2.extend_high_i32x4_s
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  SignExtendWideningUpper(Vector128<uint>   value) => SignExtendWideningUpper(value);

        /// <summary>
        ///   i16x8.extend_low_i8x16_u
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  ZeroExtendWideningLower(Vector128<sbyte>  value) => ZeroExtendWideningLower(value);
        /// <summary>
        ///   i16x8.extend_low_i8x16_u
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> ZeroExtendWideningLower(Vector128<byte>   value) => ZeroExtendWideningLower(value);
        /// <summary>
        ///   i32x4.extend_low_i16x8_u
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    ZeroExtendWideningLower(Vector128<short>  value) => ZeroExtendWideningLower(value);
        /// <summary>
        ///   i32x4.extend_low_i16x8_u
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   ZeroExtendWideningLower(Vector128<ushort> value) => ZeroExtendWideningLower(value);
        /// <summary>
        ///   i64x2.extend_low_i32x4_u
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   ZeroExtendWideningLower(Vector128<int>    value) => ZeroExtendWideningLower(value);
        /// <summary>
        ///   i64x2.extend_low_i32x4_u
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  ZeroExtendWideningLower(Vector128<uint>   value) => ZeroExtendWideningLower(value);

        /// <summary>
        ///   i16x8.extend_high_i8x16_u
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  ZeroExtendWideningUpper(Vector128<sbyte>  value) => ZeroExtendWideningUpper(value);
        /// <summary>
        ///   i16x8.extend_high_i8x16_u
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> ZeroExtendWideningUpper(Vector128<byte>   value) => ZeroExtendWideningUpper(value);
        /// <summary>
        ///   i32x4.extend_high_i16x8_u
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    ZeroExtendWideningUpper(Vector128<short>  value) => ZeroExtendWideningUpper(value);
        /// <summary>
        ///   i32x4.extend_high_i16x8_u
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   ZeroExtendWideningUpper(Vector128<ushort> value) => ZeroExtendWideningUpper(value);
        /// <summary>
        ///   i64x2.extend_high_i32x4_u
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   ZeroExtendWideningUpper(Vector128<int>    value) => ZeroExtendWideningUpper(value);
        /// <summary>
        ///   i64x2.extend_high_i32x4_u
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  ZeroExtendWideningUpper(Vector128<uint>   value) => ZeroExtendWideningUpper(value);
    }
}
