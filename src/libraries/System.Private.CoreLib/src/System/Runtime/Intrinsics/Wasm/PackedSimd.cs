// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Runtime.Intrinsics.Wasm
{
    /// <summary>Provides access to the WebAssembly packed SIMD instructions via intrinsics.</summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class PackedSimd
    {
        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static bool IsSupported { [Intrinsic] get { return IsSupported; } }

        // Constructing SIMD Values

        // cut (lives somewhere else, use Vector128.Create)
        // public static Vector128<T> Constant(ImmByte[16] imm);

        /// <summary>
        ///   <para>  i8x16.splat or v128.const</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<sbyte>  Splat(sbyte  value) => Splat(value);
        /// <summary>
        ///   <para>  i8x16.splat or v128.const</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<byte>   Splat(byte   value) => Splat(value);
        /// <summary>
        ///   <para>  i16x8.splat or v128.const</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  Splat(short  value) => Splat(value);
        /// <summary>
        ///   <para>  i16x8.splat or v128.const</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> Splat(ushort value) => Splat(value);
        /// <summary>
        ///   <para>  i32x4.splat or v128.const</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    Splat(int    value) => Splat(value);
        /// <summary>
        ///   <para>  i32x4.splat or v128.const</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   Splat(uint   value) => Splat(value);
        /// <summary>
        ///   <para>  i64x2.splat or v128.const</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   Splat(long   value) => Splat(value);
        /// <summary>
        ///   <para>  i64x2.splat or v128.const</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  Splat(ulong  value) => Splat(value);
        /// <summary>
        ///   <para>  f32x4.splat or v128.const</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  Splat(float  value) => Splat(value);
        /// <summary>
        ///   <para>  f64x2.splat or v128.const</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<double> Splat(double value) => Splat(value);
        /// <summary>
        ///   <para>  i32x4.splat or v128.const</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<nint>   Splat(nint   value) => Splat(value);
        /// <summary>
        ///   <para>  i32x4.splat or v128.const</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<nuint>  Splat(nuint  value) => Splat(value);

        // Accessing lanes

        /// <summary>
        ///   <para>  i8x16.extract_lane_s</para>
        /// </summary>
        [Intrinsic]
        public static int    ExtractScalar(Vector128<sbyte>  value, [ConstantExpected(Max = (byte)(15))] byte index) => ExtractScalar(value, index);    // takes ImmLaneIdx16
        /// <summary>
        ///   <para>  i8x16.extract_lane_u</para>
        /// </summary>
        [Intrinsic]
        public static uint   ExtractScalar(Vector128<byte>   value, [ConstantExpected(Max = (byte)(15))] byte index) => ExtractScalar(value, index);    // takes ImmLaneIdx16
        /// <summary>
        ///   <para>  i16x8.extract_lane_s</para>
        /// </summary>
        [Intrinsic]
        public static int    ExtractScalar(Vector128<short>  value, [ConstantExpected(Max = (byte)(7))] byte index) => ExtractScalar(value, index);    // takes ImmLaneIdx8
        /// <summary>
        ///   <para>  i16x8.extract_lane_u</para>
        /// </summary>
        [Intrinsic]
        public static uint   ExtractScalar(Vector128<ushort> value, [ConstantExpected(Max = (byte)(7))] byte index) => ExtractScalar(value, index);    // takes ImmLaneIdx8
        /// <summary>
        ///   <para>  i32x4.extract_lane</para>
        /// </summary>
        [Intrinsic]
        public static int    ExtractScalar(Vector128<int>    value, [ConstantExpected(Max = (byte)(3))] byte index) => ExtractScalar(value, index);    // takes ImmLaneIdx4
        /// <summary>
        ///   <para>  i32x4.extract_lane</para>
        /// </summary>
        [Intrinsic]
        public static uint   ExtractScalar(Vector128<uint>   value, [ConstantExpected(Max = (byte)(3))] byte index) => ExtractScalar(value, index);    // takes ImmLaneIdx4
        /// <summary>
        ///   <para>  i64x2.extract_lane</para>
        /// </summary>
        [Intrinsic]
        public static long   ExtractScalar(Vector128<long>   value, [ConstantExpected(Max = (byte)(1))] byte index) => ExtractScalar(value, index);    // takes ImmLaneIdx2
        /// <summary>
        ///   <para>  i64x2.extract_lane</para>
        /// </summary>
        [Intrinsic]
        public static ulong  ExtractScalar(Vector128<ulong>  value, [ConstantExpected(Max = (byte)(1))] byte index) => ExtractScalar(value, index);    // takes ImmLaneIdx2
        /// <summary>
        ///   <para>  f32x4.extract_lane</para>
        /// </summary>
        [Intrinsic]
        public static float  ExtractScalar(Vector128<float>  value, [ConstantExpected(Max = (byte)(3))] byte index) => ExtractScalar(value, index);    // takes ImmLaneIdx4
        /// <summary>
        ///   <para>  f64x2.extract_lane</para>
        /// </summary>
        [Intrinsic]
        public static double ExtractScalar(Vector128<double> value, [ConstantExpected(Max = (byte)(1))] byte index) => ExtractScalar(value, index);    // takes ImmLaneIdx2
        /// <summary>
        ///   <para>  i32x4.extract_lane</para>
        /// </summary>
        [Intrinsic]
        public static nint   ExtractScalar(Vector128<nint>   value, [ConstantExpected(Max = (byte)(3))] byte index) => ExtractScalar(value, index);
        /// <summary>
        ///   <para>  i32x4.extract_lane</para>
        /// </summary>
        [Intrinsic]
        public static nuint  ExtractScalar(Vector128<nuint>  value, [ConstantExpected(Max = (byte)(3))] byte index) => ExtractScalar(value, index);

        /// <summary>
        ///   <para>  i8x16.replace_lane</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<sbyte>  ReplaceScalar(Vector128<sbyte>  vector, [ConstantExpected(Max = (byte)(15))] byte imm, int    value) => ReplaceScalar(vector, imm, value);   // takes ImmLaneIdx16
        /// <summary>
        ///   <para>  i8x16.replace_lane</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<byte>   ReplaceScalar(Vector128<byte>   vector, [ConstantExpected(Max = (byte)(15))] byte imm, uint   value) => ReplaceScalar(vector, imm, value);   // takes ImmLaneIdx16
        /// <summary>
        ///   <para>  i16x8.replace_lane</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  ReplaceScalar(Vector128<short>  vector, [ConstantExpected(Max = (byte)(7))] byte imm, int    value) => ReplaceScalar(vector, imm, value);   // takes ImmLaneIdx8
        /// <summary>
        ///   <para>  i16x8.replace_lane</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> ReplaceScalar(Vector128<ushort> vector, [ConstantExpected(Max = (byte)(7))] byte imm, uint   value) => ReplaceScalar(vector, imm, value);   // takes ImmLaneIdx8
        /// <summary>
        ///   <para>  i32x4.replace_lane</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    ReplaceScalar(Vector128<int>    vector, [ConstantExpected(Max = (byte)(3))] byte imm, int    value) => ReplaceScalar(vector, imm, value);   // takes ImmLaneIdx4
        /// <summary>
        ///   <para>  i32x4.replace_lane</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    ReplaceScalar(Vector128<uint>   vector, [ConstantExpected(Max = (byte)(3))] byte imm, uint   value) => ReplaceScalar(vector, imm, value);   // takes ImmLaneIdx4
        /// <summary>
        ///   <para>  i64x2.replace_lane</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   ReplaceScalar(Vector128<long>   vector, [ConstantExpected(Max = (byte)(1))] byte imm, long   value) => ReplaceScalar(vector, imm, value);   // takes ImmLaneIdx2
        /// <summary>
        ///   <para>  i64x2.replace_lane</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  ReplaceScalar(Vector128<ulong>  vector, [ConstantExpected(Max = (byte)(1))] byte imm, ulong  value) => ReplaceScalar(vector, imm, value);   // takes ImmLaneIdx2
        /// <summary>
        ///   <para>  f32x4.replace_lane</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  ReplaceScalar(Vector128<float>  vector, [ConstantExpected(Max = (byte)(3))] byte imm, float  value) => ReplaceScalar(vector, imm, value);   // takes ImmLaneIdx4
        /// <summary>
        ///   <para>  f64x2.replace_lane</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<double> ReplaceScalar(Vector128<double> vector, [ConstantExpected(Max = (byte)(1))] byte imm, double value) => ReplaceScalar(vector, imm, value);   // takes ImmLaneIdx2
        /// <summary>
        ///   <para>  i32x4.replace_lane</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<nint>   ReplaceScalar(Vector128<nint>   vector, [ConstantExpected(Max = (byte)(3))] byte imm, nint   value) => ReplaceScalar(vector, imm, value);
        /// <summary>
        ///   <para>  i32x4.replace_lane</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<nuint>  ReplaceScalar(Vector128<nuint>  vector, [ConstantExpected(Max = (byte)(3))] byte imm, nuint  value) => ReplaceScalar(vector, imm, value);

        /// <summary>
        ///   <para>  i8x16.shuffle</para>
        /// </summary>
        [Intrinsic]
        internal static Vector128<sbyte> Shuffle(Vector128<sbyte> lower, Vector128<sbyte> upper, Vector128<sbyte> indices) => Shuffle(lower, upper, indices);
        /// <summary>
        ///   <para>  i8x16.shuffle</para>
        /// </summary>
        [Intrinsic]
        internal static Vector128<byte>  Shuffle(Vector128<byte>  lower, Vector128<byte>  upper, Vector128<byte>  indices) => Shuffle(lower, upper, indices);

        /// <summary>
        ///   <para>  i8x16.swizzle</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<sbyte> Swizzle(Vector128<sbyte> vector, Vector128<sbyte> indices) => Swizzle(vector, indices);
        /// <summary>
        ///   <para>  i8x16.swizzle</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<byte>  Swizzle(Vector128<byte>  vector, Vector128<byte>  indices) => Swizzle(vector, indices);

        // Integer arithmetic

        /// <summary>
        ///   <para>  i8x16.add</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<sbyte>  Add(Vector128<sbyte>  left, Vector128<sbyte>  right) => Add(left, right);
        /// <summary>
        ///   <para>  i8x16.add</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<byte>   Add(Vector128<byte>   left, Vector128<byte>   right) => Add(left, right);
        /// <summary>
        ///   <para>  i16x8.add</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  Add(Vector128<short>  left, Vector128<short>  right) => Add(left, right);
        /// <summary>
        ///   <para>  i16x8.add</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> Add(Vector128<ushort> left, Vector128<ushort> right) => Add(left, right);
        /// <summary>
        ///   <para>  i32x4.add</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    Add(Vector128<int>    left, Vector128<int>    right) => Add(left, right);
        /// <summary>
        ///   <para>  i32x4.add</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   Add(Vector128<uint>   left, Vector128<uint>   right) => Add(left, right);
        /// <summary>
        ///   <para>  i64x2.add</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   Add(Vector128<long>   left, Vector128<long>   right) => Add(left, right);
        /// <summary>
        ///   <para>  i64x2.add</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  Add(Vector128<ulong>  left, Vector128<ulong>  right) => Add(left, right);
        /// <summary>
        ///   <para>  i32x4.add</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<nint>   Add(Vector128<nint>   left, Vector128<nint>   right) => Add(left, right);
        /// <summary>
        ///   <para>  i32x4.add</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<nuint>  Add(Vector128<nuint>  left, Vector128<nuint>  right) => Add(left, right);

        /// <summary>
        ///   <para>  i8x16.sub</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<sbyte>  Subtract(Vector128<sbyte>  left, Vector128<sbyte>  right) => Subtract(left, right);
        /// <summary>
        ///   <para>  i8x16.sub</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<byte>   Subtract(Vector128<byte>   left, Vector128<byte>   right) => Subtract(left, right);
        /// <summary>
        ///   <para>  i16x8.sub</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  Subtract(Vector128<short>  left, Vector128<short>  right) => Subtract(left, right);
        /// <summary>
        ///   <para>  i16x8.sub</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> Subtract(Vector128<ushort> left, Vector128<ushort> right) => Subtract(left, right);
        /// <summary>
        ///   <para>  i32x4.sub</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    Subtract(Vector128<int>    left, Vector128<int>    right) => Subtract(left, right);
        /// <summary>
        ///   <para>  i32x4.sub</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   Subtract(Vector128<uint>   left, Vector128<uint>   right) => Subtract(left, right);
        /// <summary>
        ///   <para>  i64x2.sub</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   Subtract(Vector128<long>   left, Vector128<long>   right) => Subtract(left, right);
        /// <summary>
        ///   <para>  i64x2.sub</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  Subtract(Vector128<ulong>  left, Vector128<ulong>  right) => Subtract(left, right);
        /// <summary>
        ///   <para>  i32x4.sub</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<nint>   Subtract(Vector128<nint>   left, Vector128<nint>   right) => Subtract(left, right);
        /// <summary>
        ///   <para>  i32x4.sub</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<nuint>  Subtract(Vector128<nuint>  left, Vector128<nuint>  right) => Subtract(left, right);

        /// <summary>
        ///   <para>  i16x8.mul</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  Multiply(Vector128<short>  left, Vector128<short>  right) => Multiply(left, right);
        /// <summary>
        ///   <para>  i16x8.mul</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> Multiply(Vector128<ushort> left, Vector128<ushort> right) => Multiply(left, right);
        /// <summary>
        ///   <para>  i32x4.mul</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    Multiply(Vector128<int>    left, Vector128<int>    right) => Multiply(left, right);
        /// <summary>
        ///   <para>  i32x4.mul</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   Multiply(Vector128<uint>   left, Vector128<uint>   right) => Multiply(left, right);
        /// <summary>
        ///   <para>  i64x2.mul</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   Multiply(Vector128<long>   left, Vector128<long>   right) => Multiply(left, right);
        /// <summary>
        ///   <para>  i64x2.mul</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  Multiply(Vector128<ulong>  left, Vector128<ulong>  right) => Multiply(left, right);
        /// <summary>
        ///   <para>  i32x4.mul</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<nint>   Multiply(Vector128<nint>   left, Vector128<nint>   right) => Multiply(left, right);
        /// <summary>
        ///   <para>  i32x4.mul</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<nuint>  Multiply(Vector128<nuint>  left, Vector128<nuint>  right) => Multiply(left, right);

        /// <summary>
        ///   <para>  i32x4.dot_i16x8_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<int> Dot(Vector128<short> left, Vector128<short> right) => Dot(left, right);

        /// <summary>
        ///   <para>  i8x16.neg</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<sbyte>  Negate(Vector128<sbyte>  value) => Negate(value);
        /// <summary>
        ///   <para>  i8x16.neg</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<byte>   Negate(Vector128<byte>   value) => Negate(value);
        /// <summary>
        ///   <para>  i16x8.neg</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  Negate(Vector128<short>  value) => Negate(value);
        /// <summary>
        ///   <para>  i16x8.neg</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> Negate(Vector128<ushort> value) => Negate(value);
        /// <summary>
        ///   <para>  i32x4.neg</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    Negate(Vector128<int>    value) => Negate(value);
        /// <summary>
        ///   <para>  i32x4.neg</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   Negate(Vector128<uint>   value) => Negate(value);
        /// <summary>
        ///   <para>  i64x2.neg</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   Negate(Vector128<long>   value) => Negate(value);
        /// <summary>
        ///   <para>  i64x2.neg</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  Negate(Vector128<ulong>  value) => Negate(value);
        /// <summary>
        ///   <para>  i32x4.neg</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<nint>   Negate(Vector128<nint>   value) => Negate(value);
        /// <summary>
        ///   <para>  i32x4.neg</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<nuint>  Negate(Vector128<nuint>  value) => Negate(value);

        // Extended integer arithmetic

        /// <summary>
        ///   <para>  i16x8.extmul_low_i8x16_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  MultiplyWideningLower(Vector128<sbyte>  left, Vector128<sbyte>  right) => MultiplyWideningLower(left, right);
        /// <summary>
        ///   <para>  i16x8.extmul_low_i8x16_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> MultiplyWideningLower(Vector128<byte>   left, Vector128<byte>   right) => MultiplyWideningLower(left, right);
        /// <summary>
        ///   <para>  i32x4.extmul_low_i16x8_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    MultiplyWideningLower(Vector128<short>  left, Vector128<short>  right) => MultiplyWideningLower(left, right);
        /// <summary>
        ///   <para>  i32x4.extmul_low_i16x8_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   MultiplyWideningLower(Vector128<ushort> left, Vector128<ushort> right) => MultiplyWideningLower(left, right);
        /// <summary>
        ///   <para>  i64x2.extmul_low_i32x4_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   MultiplyWideningLower(Vector128<int>    left, Vector128<int>    right) => MultiplyWideningLower(left, right);
        /// <summary>
        ///   <para>  i64x2.extmul_low_i32x4_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  MultiplyWideningLower(Vector128<uint>   left, Vector128<uint>   right) => MultiplyWideningLower(left, right);

        /// <summary>
        ///   <para>  i16x8.extmul_high_i8x16_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  MultiplyWideningUpper(Vector128<sbyte>  left, Vector128<sbyte>  right) => MultiplyWideningUpper(left, right);
        /// <summary>
        ///   <para>  i16x8.extmul_high_i8x16_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> MultiplyWideningUpper(Vector128<byte>   left, Vector128<byte>   right) => MultiplyWideningUpper(left, right);
        /// <summary>
        ///   <para>  i32x4.extmul_high_i16x8_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    MultiplyWideningUpper(Vector128<short>  left, Vector128<short>  right) => MultiplyWideningUpper(left, right);
        /// <summary>
        ///   <para>  i32x4.extmul_high_i16x8_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   MultiplyWideningUpper(Vector128<ushort> left, Vector128<ushort> right) => MultiplyWideningUpper(left, right);
        /// <summary>
        ///   <para>  i64x2.extmul_high_i32x4_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   MultiplyWideningUpper(Vector128<int>    left, Vector128<int>    right) => MultiplyWideningUpper(left, right);
        /// <summary>
        ///   <para>  i64x2.extmul_high_i32x4_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  MultiplyWideningUpper(Vector128<uint>   left, Vector128<uint>   right) => MultiplyWideningUpper(left, right);

        /// <summary>
        ///   <para>  i16x8.extadd_pairwise_i8x16_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  AddPairwiseWidening(Vector128<sbyte>  value) => AddPairwiseWidening(value);
        /// <summary>
        ///   <para>  i16x8.extadd_pairwise_i8x16_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> AddPairwiseWidening(Vector128<byte>   value) => AddPairwiseWidening(value);
        /// <summary>
        ///   <para>  i32x4.extadd_pairwise_i16x8_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    AddPairwiseWidening(Vector128<short>  value) => AddPairwiseWidening(value);
        /// <summary>
        ///   <para>  i32x4.extadd_pairwise_i16x8_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   AddPairwiseWidening(Vector128<ushort> value) => AddPairwiseWidening(value);

        // Saturating integer arithmetic

        /// <summary>
        ///   <para>  i8x16.add.sat.s</para>
        /// </summary>
        public static Vector128<sbyte>  AddSaturate(Vector128<sbyte>  left, Vector128<sbyte>  right) => AddSaturate(left, right);
        /// <summary>
        ///   <para>  i8x16.add.sat.u</para>
        /// </summary>
        public static Vector128<byte>   AddSaturate(Vector128<byte>   left, Vector128<byte>   right) => AddSaturate(left, right);
        /// <summary>
        ///   <para>  i16x8.add.sat.s</para>
        /// </summary>
        public static Vector128<short>  AddSaturate(Vector128<short>  left, Vector128<short>  right) => AddSaturate(left, right);
        /// <summary>
        ///   <para>  i16x8.add.sat.u</para>
        /// </summary>
        public static Vector128<ushort> AddSaturate(Vector128<ushort> left, Vector128<ushort> right) => AddSaturate(left, right);

        /// <summary>
        ///   <para>  i8x16.sub.sat.s</para>
        /// </summary>
        public static Vector128<sbyte>  SubtractSaturate(Vector128<sbyte>  left, Vector128<sbyte>  right) => SubtractSaturate(left, right);
        /// <summary>
        ///   <para>  i8x16.sub.sat.u</para>
        /// </summary>
        public static Vector128<byte>   SubtractSaturate(Vector128<byte>   left, Vector128<byte>   right) => SubtractSaturate(left, right);
        /// <summary>
        ///   <para>  i16x8.sub.sat.s</para>
        /// </summary>
        public static Vector128<short>  SubtractSaturate(Vector128<short>  left, Vector128<short>  right) => SubtractSaturate(left, right);
        /// <summary>
        ///   <para>  i16x8.sub.sat.u</para>
        /// </summary>
        public static Vector128<ushort> SubtractSaturate(Vector128<ushort> left, Vector128<ushort> right) => SubtractSaturate(left, right);

        /// <summary>
        ///   <para>  i16x8.q15mulr.sat.s</para>
        /// </summary>
        public static Vector128<short> MultiplyRoundedSaturateQ15(Vector128<short> left, Vector128<short> right) => MultiplyRoundedSaturateQ15(left, right);

        /// <summary>
        ///   <para>  i8x16.min.s</para>
        /// </summary>
        public static Vector128<sbyte>  Min(Vector128<sbyte>  left, Vector128<sbyte>  right) => Min(left, right);
        /// <summary>
        ///   <para>  i8x16.min.u</para>
        /// </summary>
        public static Vector128<byte>   Min(Vector128<byte>   left, Vector128<byte>   right) => Min(left, right);
        /// <summary>
        ///   <para>  i16x8.min.s</para>
        /// </summary>
        public static Vector128<short>  Min(Vector128<short>  left, Vector128<short>  right) => Min(left, right);
        /// <summary>
        ///   <para>  i16x8.min.u</para>
        /// </summary>
        public static Vector128<ushort> Min(Vector128<ushort> left, Vector128<ushort> right) => Min(left, right);
        /// <summary>
        ///   <para>  i32x4.min.s</para>
        /// </summary>
        public static Vector128<int>    Min(Vector128<int>    left, Vector128<int>    right) => Min(left, right);
        /// <summary>
        ///   <para>  i32x4.min.u</para>
        /// </summary>
        public static Vector128<uint>   Min(Vector128<uint>   left, Vector128<uint>   right) => Min(left, right);

        /// <summary>
        ///   <para>  i8x16.max.s</para>
        /// </summary>
        public static Vector128<sbyte>  Max(Vector128<sbyte>  left, Vector128<sbyte>  right) => Max(left, right);
        /// <summary>
        ///   <para>  i8x16.max.u</para>
        /// </summary>
        public static Vector128<byte>   Max(Vector128<byte>   left, Vector128<byte>   right) => Max(left, right);
        /// <summary>
        ///   <para>  i16x8.max.s</para>
        /// </summary>
        public static Vector128<short>  Max(Vector128<short>  left, Vector128<short>  right) => Max(left, right);
        /// <summary>
        ///   <para>  i16x8.max.u</para>
        /// </summary>
        public static Vector128<ushort> Max(Vector128<ushort> left, Vector128<ushort> right) => Max(left, right);
        /// <summary>
        ///   <para>  i32x4.max.s</para>
        /// </summary>
        public static Vector128<int>    Max(Vector128<int>    left, Vector128<int>    right) => Max(left, right);
        /// <summary>
        ///   <para>  i32x4.max.u</para>
        /// </summary>
        public static Vector128<uint>   Max(Vector128<uint>   left, Vector128<uint>   right) => Max(left, right);

        /// <summary>
        ///   <para>  i8x16.avgr.u</para>
        /// </summary>
        public static Vector128<byte>   AverageRounded(Vector128<byte>   left, Vector128<byte>   right) => AverageRounded(left, right);
        /// <summary>
        ///   <para>  i16x8.avgr.u</para>
        /// </summary>
        public static Vector128<ushort> AverageRounded(Vector128<ushort> left, Vector128<ushort> right) => AverageRounded(left, right);

        /// <summary>
        ///   <para>  i8x16.abs</para>
        /// </summary>
        public static Vector128<sbyte> Abs(Vector128<sbyte> value) => Abs(value);
        /// <summary>
        ///   <para>  i16x8.abs</para>
        /// </summary>
        public static Vector128<short> Abs(Vector128<short> value) => Abs(value);
        /// <summary>
        ///   <para>  i32x4.abs</para>
        /// </summary>
        public static Vector128<int>   Abs(Vector128<int>   value) => Abs(value);
        /// <summary>
        ///   <para>  i64x2.abs</para>
        /// </summary>
        public static Vector128<long>  Abs(Vector128<long>  value) => Abs(value);
        /// <summary>
        ///   <para>  i32x4.abs</para>
        /// </summary>
        public static Vector128<nint>  Abs(Vector128<nint>  value) => Abs(value);

        // Bit shifts

        /// <summary>
        ///   <para>  i8x16.shl</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<sbyte>  ShiftLeft(Vector128<sbyte>  value, int count) => ShiftLeft(value, count);
        /// <summary>
        ///   <para>  i8x16.shl</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<byte>   ShiftLeft(Vector128<byte>   value, int count) => ShiftLeft(value, count);
        /// <summary>
        ///   <para>  i16x8.shl</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  ShiftLeft(Vector128<short>  value, int count) => ShiftLeft(value, count);
        /// <summary>
        ///   <para>  i16x8.shl</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> ShiftLeft(Vector128<ushort> value, int count) => ShiftLeft(value, count);
        /// <summary>
        ///   <para>  i32x4.shl</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    ShiftLeft(Vector128<int>    value, int count) => ShiftLeft(value, count);
        /// <summary>
        ///   <para>  i32x4.shl</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   ShiftLeft(Vector128<uint>   value, int count) => ShiftLeft(value, count);
        /// <summary>
        ///   <para>  i64x2.shl</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   ShiftLeft(Vector128<long>   value, int count) => ShiftLeft(value, count);
        /// <summary>
        ///   <para>  i64x2.shl</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  ShiftLeft(Vector128<ulong>  value, int count) => ShiftLeft(value, count);
        /// <summary>
        ///   <para>  i32x4.shl</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<nint>   ShiftLeft(Vector128<nint>   value, int count) => ShiftLeft(value, count);
        /// <summary>
        ///   <para>  i32x4.shl</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<nuint>  ShiftLeft(Vector128<nuint>  value, int count) => ShiftLeft(value, count);

        /// <summary>
        ///   <para>  i8x16.shr_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<sbyte>  ShiftRightArithmetic(Vector128<sbyte>  value, int count) => ShiftRightArithmetic(value, count);
        /// <summary>
        ///   <para>  i8x16.shr_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<byte>   ShiftRightArithmetic(Vector128<byte>   value, int count) => ShiftRightArithmetic(value, count);
        /// <summary>
        ///   <para>  i16x8.shr_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  ShiftRightArithmetic(Vector128<short>  value, int count) => ShiftRightArithmetic(value, count);
        /// <summary>
        ///   <para>  i16x8.shr_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> ShiftRightArithmetic(Vector128<ushort> value, int count) => ShiftRightArithmetic(value, count);
        /// <summary>
        ///   <para>  i32x4.shr_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    ShiftRightArithmetic(Vector128<int>    value, int count) => ShiftRightArithmetic(value, count);
        /// <summary>
        ///   <para>  i32x4.shr_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   ShiftRightArithmetic(Vector128<uint>   value, int count) => ShiftRightArithmetic(value, count);
        /// <summary>
        ///   <para>  i64x2.shr_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   ShiftRightArithmetic(Vector128<long>   value, int count) => ShiftRightArithmetic(value, count);
        /// <summary>
        ///   <para>  i64x2.shr_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  ShiftRightArithmetic(Vector128<ulong>  value, int count) => ShiftRightArithmetic(value, count);
        /// <summary>
        ///   <para>  i32x4.shr_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<nint>   ShiftRightArithmetic(Vector128<nint>   value, int count) => ShiftRightArithmetic(value, count);
        /// <summary>
        ///   <para>  i32x4.shr_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<nuint>  ShiftRightArithmetic(Vector128<nuint>  value, int count) => ShiftRightArithmetic(value, count);

        /// <summary>
        ///   <para>  i8x16.shr_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<sbyte>  ShiftRightLogical(Vector128<sbyte>  value, int count) => ShiftRightLogical(value, count);
        /// <summary>
        ///   <para>  i8x16.shr_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<byte>   ShiftRightLogical(Vector128<byte>   value, int count) => ShiftRightLogical(value, count);
        /// <summary>
        ///   <para>  i16x8.shr_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  ShiftRightLogical(Vector128<short>  value, int count) => ShiftRightLogical(value, count);
        /// <summary>
        ///   <para>  i16x8.shr_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> ShiftRightLogical(Vector128<ushort> value, int count) => ShiftRightLogical(value, count);
        /// <summary>
        ///   <para>  i32x4.shr_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    ShiftRightLogical(Vector128<int>    value, int count) => ShiftRightLogical(value, count);
        /// <summary>
        ///   <para>  i32x4.shr_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   ShiftRightLogical(Vector128<uint>   value, int count) => ShiftRightLogical(value, count);
        /// <summary>
        ///   <para>  i64x2.shr_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   ShiftRightLogical(Vector128<long>   value, int count) => ShiftRightLogical(value, count);
        /// <summary>
        ///   <para>  i64x2.shr_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  ShiftRightLogical(Vector128<ulong>  value, int count) => ShiftRightLogical(value, count);
        /// <summary>
        ///   <para>  i32x4.shr_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<nint>   ShiftRightLogical(Vector128<nint>   value, int count) => ShiftRightLogical(value, count);
        /// <summary>
        ///   <para>  i32x4.shr_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<nuint>  ShiftRightLogical(Vector128<nuint>  value, int count) => ShiftRightLogical(value, count);

        // Bitwise operations

        /// <summary>
        ///   <para>  v128.and</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<sbyte>  And(Vector128<sbyte>  left, Vector128<sbyte>  right) => And(left, right);
        /// <summary>
        ///   <para>  v128.and</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<byte>   And(Vector128<byte>   left, Vector128<byte>   right) => And(left, right);
        /// <summary>
        ///   <para>  v128.and</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  And(Vector128<short>  left, Vector128<short>  right) => And(left, right);
        /// <summary>
        ///   <para>  v128.and</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> And(Vector128<ushort> left, Vector128<ushort> right) => And(left, right);
        /// <summary>
        ///   <para>  v128.and</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    And(Vector128<int>    left, Vector128<int>    right) => And(left, right);
        /// <summary>
        ///   <para>  v128.and</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   And(Vector128<uint>   left, Vector128<uint>   right) => And(left, right);
        /// <summary>
        ///   <para>  v128.and</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   And(Vector128<long>   left, Vector128<long>   right) => And(left, right);
        /// <summary>
        ///   <para>  v128.and</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  And(Vector128<ulong>  left, Vector128<ulong>  right) => And(left, right);
        /// <summary>
        ///   <para>  v128.and</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  And(Vector128<float>  left, Vector128<float>  right) => And(left, right);
        /// <summary>
        ///   <para>  v128.and</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<double> And(Vector128<double> left, Vector128<double> right) => And(left, right);
        /// <summary>
        ///   <para>  v128.and</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<nint>   And(Vector128<nint>   left, Vector128<nint>   right) => And(left, right);
        /// <summary>
        ///   <para>  v128.and</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<nuint>  And(Vector128<nuint>  left, Vector128<nuint>  right) => And(left, right);

        /// <summary>
        ///   <para>  v128.or</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<sbyte>  Or(Vector128<sbyte>  left, Vector128<sbyte>  right) => Or(left, right);
        /// <summary>
        ///   <para>  v128.or</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<byte>   Or(Vector128<byte>   left, Vector128<byte>   right) => Or(left, right);
        /// <summary>
        ///   <para>  v128.or</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  Or(Vector128<short>  left, Vector128<short>  right) => Or(left, right);
        /// <summary>
        ///   <para>  v128.or</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> Or(Vector128<ushort> left, Vector128<ushort> right) => Or(left, right);
        /// <summary>
        ///   <para>  v128.or</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    Or(Vector128<int>    left, Vector128<int>    right) => Or(left, right);
        /// <summary>
        ///   <para>  v128.or</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   Or(Vector128<uint>   left, Vector128<uint>   right) => Or(left, right);
        /// <summary>
        ///   <para>  v128.or</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   Or(Vector128<long>   left, Vector128<long>   right) => Or(left, right);
        /// <summary>
        ///   <para>  v128.or</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  Or(Vector128<ulong>  left, Vector128<ulong>  right) => Or(left, right);
        /// <summary>
        ///   <para>  v128.or</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  Or(Vector128<float>  left, Vector128<float>  right) => Or(left, right);
        /// <summary>
        ///   <para>  v128.or</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<double> Or(Vector128<double> left, Vector128<double> right) => Or(left, right);
        /// <summary>
        ///   <para>  v128.or</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<nint>   Or(Vector128<nint>   left, Vector128<nint>   right) => Or(left, right);
        /// <summary>
        ///   <para>  v128.or</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<nuint>  Or(Vector128<nuint>  left, Vector128<nuint>  right) => Or(left, right);

        /// <summary>
        ///   <para>  v128.xor</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<sbyte>  Xor(Vector128<sbyte>  left, Vector128<sbyte>  right) => Xor(left, right);
        /// <summary>
        ///   <para>  v128.xor</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<byte>   Xor(Vector128<byte>   left, Vector128<byte>   right) => Xor(left, right);
        /// <summary>
        ///   <para>  v128.xor</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  Xor(Vector128<short>  left, Vector128<short>  right) => Xor(left, right);
        /// <summary>
        ///   <para>  v128.xor</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> Xor(Vector128<ushort> left, Vector128<ushort> right) => Xor(left, right);
        /// <summary>
        ///   <para>  v128.xor</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    Xor(Vector128<int>    left, Vector128<int>    right) => Xor(left, right);
        /// <summary>
        ///   <para>  v128.xor</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   Xor(Vector128<uint>   left, Vector128<uint>   right) => Xor(left, right);
        /// <summary>
        ///   <para>  v128.xor</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   Xor(Vector128<long>   left, Vector128<long>   right) => Xor(left, right);
        /// <summary>
        ///   <para>  v128.xor</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  Xor(Vector128<ulong>  left, Vector128<ulong>  right) => Xor(left, right);
        /// <summary>
        ///   <para>  v128.xor</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  Xor(Vector128<float>  left, Vector128<float>  right) => Xor(left, right);
        /// <summary>
        ///   <para>  v128.xor</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<double> Xor(Vector128<double> left, Vector128<double> right) => Xor(left, right);
        /// <summary>
        ///   <para>  v128.xor</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<nint>   Xor(Vector128<nint>   left, Vector128<nint>   right) => Xor(left, right);
        /// <summary>
        ///   <para>  v128.xor</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<nuint>  Xor(Vector128<nuint>  left, Vector128<nuint>  right) => Xor(left, right);

        /// <summary>
        ///   <para>  v128.not</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<sbyte>  Not(Vector128<sbyte>  value) => Not(value);
        /// <summary>
        ///   <para>  v128.not</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<byte>   Not(Vector128<byte>   value) => Not(value);
        /// <summary>
        ///   <para>  v128.not</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  Not(Vector128<short>  value) => Not(value);
        /// <summary>
        ///   <para>  v128.not</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> Not(Vector128<ushort> value) => Not(value);
        /// <summary>
        ///   <para>  v128.not</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    Not(Vector128<int>    value) => Not(value);
        /// <summary>
        ///   <para>  v128.not</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   Not(Vector128<uint>   value) => Not(value);
        /// <summary>
        ///   <para>  v128.not</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   Not(Vector128<long>   value) => Not(value);
        /// <summary>
        ///   <para>  v128.not</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  Not(Vector128<ulong>  value) => Not(value);
        /// <summary>
        ///   <para>  v128.not</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  Not(Vector128<float>  value) => Not(value);
        /// <summary>
        ///   <para>  v128.not</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<double> Not(Vector128<double> value) => Not(value);
        /// <summary>
        ///   <para>  v128.not</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<nint>   Not(Vector128<nint>   value) => Not(value);
        /// <summary>
        ///   <para>  v128.not</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<nuint>  Not(Vector128<nuint>  value) => Not(value);

        /// <summary>
        ///   <para>  v128.andnot</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<sbyte>  AndNot(Vector128<sbyte>  left, Vector128<sbyte>  right) => AndNot(left, right);
        /// <summary>
        ///   <para>  v128.andnot</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<byte>   AndNot(Vector128<byte>   left, Vector128<byte>   right) => AndNot(left, right);
        /// <summary>
        ///   <para>  v128.andnot</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  AndNot(Vector128<short>  left, Vector128<short>  right) => AndNot(left, right);
        /// <summary>
        ///   <para>  v128.andnot</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> AndNot(Vector128<ushort> left, Vector128<ushort> right) => AndNot(left, right);
        /// <summary>
        ///   <para>  v128.andnot</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    AndNot(Vector128<int>    left, Vector128<int>    right) => AndNot(left, right);
        /// <summary>
        ///   <para>  v128.andnot</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   AndNot(Vector128<uint>   left, Vector128<uint>   right) => AndNot(left, right);
        /// <summary>
        ///   <para>  v128.andnot</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   AndNot(Vector128<long>   left, Vector128<long>   right) => AndNot(left, right);
        /// <summary>
        ///   <para>  v128.andnot</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  AndNot(Vector128<ulong>  left, Vector128<ulong>  right) => AndNot(left, right);
        /// <summary>
        ///   <para>  v128.andnot</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  AndNot(Vector128<float>  left, Vector128<float>  right) => AndNot(left, right);
        /// <summary>
        ///   <para>  v128.andnot</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<double> AndNot(Vector128<double> left, Vector128<double> right) => AndNot(left, right);
        /// <summary>
        ///   <para>  v128.andnot</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<nint>   AndNot(Vector128<nint>   left, Vector128<nint>   right) => AndNot(left, right);
        /// <summary>
        ///   <para>  v128.andnot</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<nuint>  AndNot(Vector128<nuint>  left, Vector128<nuint>  right) => AndNot(left, right);

        /// <summary>
        ///   <para>  v128.bitselect</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<sbyte>  BitwiseSelect(Vector128<sbyte>  left, Vector128<sbyte>  right, Vector128<sbyte>  select) => BitwiseSelect(left, right, select);
        /// <summary>
        ///   <para>  v128.bitselect</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<byte>   BitwiseSelect(Vector128<byte>   left, Vector128<byte>   right, Vector128<byte>   select) => BitwiseSelect(left, right, select);
        /// <summary>
        ///   <para>  v128.bitselect</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  BitwiseSelect(Vector128<short>  left, Vector128<short>  right, Vector128<short>  select) => BitwiseSelect(left, right, select);
        /// <summary>
        ///   <para>  v128.bitselect</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> BitwiseSelect(Vector128<ushort> left, Vector128<ushort> right, Vector128<ushort> select) => BitwiseSelect(left, right, select);
        /// <summary>
        ///   <para>  v128.bitselect</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    BitwiseSelect(Vector128<int>    left, Vector128<int>    right, Vector128<int>    select) => BitwiseSelect(left, right, select);
        /// <summary>
        ///   <para>  v128.bitselect</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   BitwiseSelect(Vector128<uint>   left, Vector128<uint>   right, Vector128<uint>   select) => BitwiseSelect(left, right, select);
        /// <summary>
        ///   <para>  v128.bitselect</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   BitwiseSelect(Vector128<long>   left, Vector128<long>   right, Vector128<long>   select) => BitwiseSelect(left, right, select);
        /// <summary>
        ///   <para>  v128.bitselect</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  BitwiseSelect(Vector128<ulong>  left, Vector128<ulong>  right, Vector128<ulong>  select) => BitwiseSelect(left, right, select);
        /// <summary>
        ///   <para>  v128.bitselect</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  BitwiseSelect(Vector128<float>  left, Vector128<float>  right, Vector128<float>  select) => BitwiseSelect(left, right, select);
        /// <summary>
        ///   <para>  v128.bitselect</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<double> BitwiseSelect(Vector128<double> left, Vector128<double> right, Vector128<double> select) => BitwiseSelect(left, right, select);
        /// <summary>
        ///   <para>  v128.bitselect</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<nint>   BitwiseSelect(Vector128<nint>   left, Vector128<nint>   right, Vector128<nint>   select) => BitwiseSelect(left, right, select);
        /// <summary>
        ///   <para>  v128.bitselect</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<nuint>  BitwiseSelect(Vector128<nuint>  left, Vector128<nuint>  right, Vector128<nuint>  select) => BitwiseSelect(left, right, select);

        /// <summary>
        ///   <para>  i8x16.popcnt</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<byte> PopCount(Vector128<byte> value) => PopCount(value);

        // Boolean horizontal reductions

        /// <summary>
        ///   <para>  v128.any_true</para>
        /// </summary>
        [Intrinsic]
        public static bool AnyTrue(Vector128<sbyte>  value) => AnyTrue(value);
        /// <summary>
        ///   <para>  v128.any_true</para>
        /// </summary>
        [Intrinsic]
        public static bool AnyTrue(Vector128<byte>   value) => AnyTrue(value);
        /// <summary>
        ///   <para>  v128.any_true</para>
        /// </summary>
        [Intrinsic]
        public static bool AnyTrue(Vector128<short>  value) => AnyTrue(value);
        /// <summary>
        ///   <para>  v128.any_true</para>
        /// </summary>
        [Intrinsic]
        public static bool AnyTrue(Vector128<ushort> value) => AnyTrue(value);
        /// <summary>
        ///   <para>  v128.any_true</para>
        /// </summary>
        [Intrinsic]
        public static bool AnyTrue(Vector128<int>    value) => AnyTrue(value);
        /// <summary>
        ///   <para>  v128.any_true</para>
        /// </summary>
        [Intrinsic]
        public static bool AnyTrue(Vector128<uint>   value) => AnyTrue(value);
        /// <summary>
        ///   <para>  v128.any_true</para>
        /// </summary>
        [Intrinsic]
        public static bool AnyTrue(Vector128<long>   value) => AnyTrue(value);
        /// <summary>
        ///   <para>  v128.any_true</para>
        /// </summary>
        [Intrinsic]
        public static bool AnyTrue(Vector128<ulong>  value) => AnyTrue(value);
        /// <summary>
        ///   <para>  v128.any_true</para>
        /// </summary>
        [Intrinsic]
        public static bool AnyTrue(Vector128<float>  value) => AnyTrue(value);
        /// <summary>
        ///   <para>  v128.any_true</para>
        /// </summary>
        [Intrinsic]
        public static bool AnyTrue(Vector128<double> value) => AnyTrue(value);
        /// <summary>
        ///   <para>  v128.any_true</para>
        /// </summary>
        [Intrinsic]
        public static bool AnyTrue(Vector128<nint>   value) => AnyTrue(value);
        /// <summary>
        ///   <para>  v128.any_true</para>
        /// </summary>
        [Intrinsic]
        public static bool AnyTrue(Vector128<nuint>  value) => AnyTrue(value);

        /// <summary>
        ///   <para>  i8x16.all_true</para>
        /// </summary>
        [Intrinsic]
        public static bool AllTrue(Vector128<sbyte>  value) => AllTrue(value);
        /// <summary>
        ///   <para>  i8x16.all_true</para>
        /// </summary>
        [Intrinsic]
        public static bool AllTrue(Vector128<byte>   value) => AllTrue(value);
        /// <summary>
        ///   <para>  i16x8.all_true</para>
        /// </summary>
        [Intrinsic]
        public static bool AllTrue(Vector128<short>  value) => AllTrue(value);
        /// <summary>
        ///   <para>  i16x8.all_true</para>
        /// </summary>
        [Intrinsic]
        public static bool AllTrue(Vector128<ushort> value) => AllTrue(value);
        /// <summary>
        ///   <para>  i32x4.all_true</para>
        /// </summary>
        [Intrinsic]
        public static bool AllTrue(Vector128<int>    value) => AllTrue(value);
        /// <summary>
        ///   <para>  i32x4.all_true</para>
        /// </summary>
        [Intrinsic]
        public static bool AllTrue(Vector128<uint>   value) => AllTrue(value);
        /// <summary>
        ///   <para>  i64x2.all_true</para>
        /// </summary>
        [Intrinsic]
        public static bool AllTrue(Vector128<long>   value) => AllTrue(value);
        /// <summary>
        ///   <para>  i64x2.all_true</para>
        /// </summary>
        [Intrinsic]
        public static bool AllTrue(Vector128<ulong>  value) => AllTrue(value);
        /// <summary>
        ///   <para>  i32x4.all_true</para>
        /// </summary>
        [Intrinsic]
        public static bool AllTrue(Vector128<nint>   value) => AllTrue(value);
        /// <summary>
        ///   <para>  i32x4.all_true</para>
        /// </summary>
        [Intrinsic]
        public static bool AllTrue(Vector128<nuint>  value) => AllTrue(value);

        // Bitmask extraction

        /// <summary>
        ///   <para>  i8x16.bitmask</para>
        /// </summary>
        [Intrinsic]
        public static int Bitmask(Vector128<sbyte>  value) => Bitmask(value);
        /// <summary>
        ///   <para>  i8x16.bitmask</para>
        /// </summary>
        [Intrinsic]
        public static int Bitmask(Vector128<byte>   value) => Bitmask(value);
        /// <summary>
        ///   <para>  i16x8.bitmask</para>
        /// </summary>
        [Intrinsic]
        public static int Bitmask(Vector128<short>  value) => Bitmask(value);
        /// <summary>
        ///   <para>  i16x8.bitmask</para>
        /// </summary>
        [Intrinsic]
        public static int Bitmask(Vector128<ushort> value) => Bitmask(value);
        /// <summary>
        ///   <para>  i32x4.bitmask</para>
        /// </summary>
        [Intrinsic]
        public static int Bitmask(Vector128<int>    value) => Bitmask(value);
        /// <summary>
        ///   <para>  i32x4.bitmask</para>
        /// </summary>
        [Intrinsic]
        public static int Bitmask(Vector128<uint>   value) => Bitmask(value);
        /// <summary>
        ///   <para>  i64x2.bitmask</para>
        /// </summary>
        [Intrinsic]
        public static int Bitmask(Vector128<long>   value) => Bitmask(value);
        /// <summary>
        ///   <para>  i64x2.bitmask</para>
        /// </summary>
        [Intrinsic]
        public static int Bitmask(Vector128<ulong>  value) => Bitmask(value);
        /// <summary>
        ///   <para>  i32x4.bitmask</para>
        /// </summary>
        [Intrinsic]
        public static int Bitmask(Vector128<nint>   value) => Bitmask(value);
        /// <summary>
        ///   <para>  i32x4.bitmask</para>
        /// </summary>
        [Intrinsic]
        public static int Bitmask(Vector128<nuint>  value) => Bitmask(value);

        // Comparisons

        /// <summary>
        ///   <para>  i8x16.eq</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<sbyte>  CompareEqual(Vector128<sbyte>  left, Vector128<sbyte>  right) => CompareEqual(left, right);
        /// <summary>
        ///   <para>  i8x16.eq</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<byte>   CompareEqual(Vector128<byte>   left, Vector128<byte>   right) => CompareEqual(left, right);
        /// <summary>
        ///   <para>  i16x8.eq</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  CompareEqual(Vector128<short>  left, Vector128<short>  right) => CompareEqual(left, right);
        /// <summary>
        ///   <para>  i16x8.eq</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> CompareEqual(Vector128<ushort> left, Vector128<ushort> right) => CompareEqual(left, right);
        /// <summary>
        ///   <para>  i32x4.eq</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    CompareEqual(Vector128<int>    left, Vector128<int>    right) => CompareEqual(left, right);
        /// <summary>
        ///   <para>  i32x4.eq</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   CompareEqual(Vector128<uint>   left, Vector128<uint>   right) => CompareEqual(left, right);
        /// <summary>
        ///   <para>  i64x2.eq</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   CompareEqual(Vector128<long>   left, Vector128<long>   right) => CompareEqual(left, right);
        /// <summary>
        ///   <para>  i64x2.eq</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  CompareEqual(Vector128<ulong>  left, Vector128<ulong>  right) => CompareEqual(left, right);
        /// <summary>
        ///   <para>  f32x4.eq</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  CompareEqual(Vector128<float>  left, Vector128<float>  right) => CompareEqual(left, right);
        /// <summary>
        ///   <para>  f64x2.eq</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<double> CompareEqual(Vector128<double> left, Vector128<double> right) => CompareEqual(left, right);
        /// <summary>
        ///   <para>  i32x4.eq</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<nint>   CompareEqual(Vector128<nint>   left, Vector128<nint>   right) => CompareEqual(left, right);
        /// <summary>
        ///   <para>  i32x4.eq</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<nuint>  CompareEqual(Vector128<nuint>  left, Vector128<nuint>  right) => CompareEqual(left, right);

        /// <summary>
        ///   <para>  i8x16.ne</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<sbyte>  CompareNotEqual(Vector128<sbyte>  left, Vector128<sbyte>  right) => CompareNotEqual(left, right);
        /// <summary>
        ///   <para>  i8x16.ne</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<byte>   CompareNotEqual(Vector128<byte>   left, Vector128<byte>   right) => CompareNotEqual(left, right);
        /// <summary>
        ///   <para>  i16x8.ne</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  CompareNotEqual(Vector128<short>  left, Vector128<short>  right) => CompareNotEqual(left, right);
        /// <summary>
        ///   <para>  i16x8.ne</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> CompareNotEqual(Vector128<ushort> left, Vector128<ushort> right) => CompareNotEqual(left, right);
        /// <summary>
        ///   <para>  i32x4.ne</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    CompareNotEqual(Vector128<int>    left, Vector128<int>    right) => CompareNotEqual(left, right);
        /// <summary>
        ///   <para>  i32x4.ne</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   CompareNotEqual(Vector128<uint>   left, Vector128<uint>   right) => CompareNotEqual(left, right);
        /// <summary>
        ///   <para>  i64x2.ne</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   CompareNotEqual(Vector128<long>   left, Vector128<long>   right) => CompareNotEqual(left, right);
        /// <summary>
        ///   <para>  i64x2.ne</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  CompareNotEqual(Vector128<ulong>  left, Vector128<ulong>  right) => CompareNotEqual(left, right);
        /// <summary>
        ///   <para>  f32x4.ne</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  CompareNotEqual(Vector128<float>  left, Vector128<float>  right) => CompareNotEqual(left, right);
        /// <summary>
        ///   <para>  f64x2.ne</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<double> CompareNotEqual(Vector128<double> left, Vector128<double> right) => CompareNotEqual(left, right);
        /// <summary>
        ///   <para>  i32x4.ne</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<nint>   CompareNotEqual(Vector128<nint>   left, Vector128<nint>   right) => CompareNotEqual(left, right);
        /// <summary>
        ///   <para>  i32x4.ne</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<nuint>  CompareNotEqual(Vector128<nuint>  left, Vector128<nuint>  right) => CompareNotEqual(left, right);

        /// <summary>
        ///   <para>  i8x16.lt_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<sbyte>  CompareLessThan(Vector128<sbyte>  left, Vector128<sbyte>  right) => CompareLessThan(left, right);
        /// <summary>
        ///   <para>  i8x16.lt_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<byte>   CompareLessThan(Vector128<byte>   left, Vector128<byte>   right) => CompareLessThan(left, right);
        /// <summary>
        ///   <para>  i16x8.lt_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  CompareLessThan(Vector128<short>  left, Vector128<short>  right) => CompareLessThan(left, right);
        /// <summary>
        ///   <para>  i16x8.lt_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> CompareLessThan(Vector128<ushort> left, Vector128<ushort> right) => CompareLessThan(left, right);
        /// <summary>
        ///   <para>  i32x4.lt_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    CompareLessThan(Vector128<int>    left, Vector128<int>    right) => CompareLessThan(left, right);
        /// <summary>
        ///   <para>  i32x4.lt_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   CompareLessThan(Vector128<uint>   left, Vector128<uint>   right) => CompareLessThan(left, right);
        /// <summary>
        ///   <para>  i64x2.lt_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   CompareLessThan(Vector128<long>   left, Vector128<long>   right) => CompareLessThan(left, right);
        /// <summary>
        ///   <para>  i64x2.lt_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  CompareLessThan(Vector128<ulong>  left, Vector128<ulong>  right) => CompareLessThan(left, right);
        /// <summary>
        ///   <para>  f32x4.lt</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  CompareLessThan(Vector128<float>  left, Vector128<float>  right) => CompareLessThan(left, right);
        /// <summary>
        ///   <para>  f64x2.lt</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<double> CompareLessThan(Vector128<double> left, Vector128<double> right) => CompareLessThan(left, right);
        /// <summary>
        ///   <para>  i32x4.lt_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<nint>   CompareLessThan(Vector128<nint>   left, Vector128<nint>   right) => CompareLessThan(left, right);
        /// <summary>
        ///   <para>  i32x4.lt_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<nuint>  CompareLessThan(Vector128<nuint>  left, Vector128<nuint>  right) => CompareLessThan(left, right);

        /// <summary>
        ///   <para>  i8x16.le_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<sbyte>  CompareLessThanOrEqual(Vector128<sbyte>  left, Vector128<sbyte>  right) => CompareLessThanOrEqual(left, right);
        /// <summary>
        ///   <para>  i8x16.le_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<byte>   CompareLessThanOrEqual(Vector128<byte>   left, Vector128<byte>   right) => CompareLessThanOrEqual(left, right);
        /// <summary>
        ///   <para>  i16x8.le_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  CompareLessThanOrEqual(Vector128<short>  left, Vector128<short>  right) => CompareLessThanOrEqual(left, right);
        /// <summary>
        ///   <para>  i16x8.le_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> CompareLessThanOrEqual(Vector128<ushort> left, Vector128<ushort> right) => CompareLessThanOrEqual(left, right);
        /// <summary>
        ///   <para>  i32x4.le_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    CompareLessThanOrEqual(Vector128<int>    left, Vector128<int>    right) => CompareLessThanOrEqual(left, right);
        /// <summary>
        ///   <para>  i32x4.le_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   CompareLessThanOrEqual(Vector128<uint>   left, Vector128<uint>   right) => CompareLessThanOrEqual(left, right);
        /// <summary>
        ///   <para>  i64x2.le_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   CompareLessThanOrEqual(Vector128<long>   left, Vector128<long>   right) => CompareLessThanOrEqual(left, right);
        /// <summary>
        ///   <para>  i64x2.le_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  CompareLessThanOrEqual(Vector128<ulong>  left, Vector128<ulong>  right) => CompareLessThanOrEqual(left, right);
        /// <summary>
        ///   <para>  f32x4.le</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  CompareLessThanOrEqual(Vector128<float>  left, Vector128<float>  right) => CompareLessThanOrEqual(left, right);
        /// <summary>
        ///   <para>  f64x2.le</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<double> CompareLessThanOrEqual(Vector128<double> left, Vector128<double> right) => CompareLessThanOrEqual(left, right);
        /// <summary>
        ///   <para>  i32x4.le_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<nint>   CompareLessThanOrEqual(Vector128<nint>   left, Vector128<nint>   right) => CompareLessThanOrEqual(left, right);
        /// <summary>
        ///   <para>  i32x4.le_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<nuint>  CompareLessThanOrEqual(Vector128<nuint>  left, Vector128<nuint>  right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        ///   <para>  i8x16.gt_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<sbyte>  CompareGreaterThan(Vector128<sbyte>  left, Vector128<sbyte>  right) => CompareGreaterThan(left, right);
        /// <summary>
        ///   <para>  i8x16.gt_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<byte>   CompareGreaterThan(Vector128<byte>   left, Vector128<byte>   right) => CompareGreaterThan(left, right);
        /// <summary>
        ///   <para>  i16x8.gt_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  CompareGreaterThan(Vector128<short>  left, Vector128<short>  right) => CompareGreaterThan(left, right);
        /// <summary>
        ///   <para>  i16x8.gt_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> CompareGreaterThan(Vector128<ushort> left, Vector128<ushort> right) => CompareGreaterThan(left, right);
        /// <summary>
        ///   <para>  i32x4.gt_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    CompareGreaterThan(Vector128<int>    left, Vector128<int>    right) => CompareGreaterThan(left, right);
        /// <summary>
        ///   <para>  i32x4.gt_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   CompareGreaterThan(Vector128<uint>   left, Vector128<uint>   right) => CompareGreaterThan(left, right);
        /// <summary>
        ///   <para>  i64x2.gt_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   CompareGreaterThan(Vector128<long>   left, Vector128<long>   right) => CompareGreaterThan(left, right);
        /// <summary>
        ///   <para>  i64x2.gt_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  CompareGreaterThan(Vector128<ulong>  left, Vector128<ulong>  right) => CompareGreaterThan(left, right);
        /// <summary>
        ///   <para>  f32x4.gt</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  CompareGreaterThan(Vector128<float>  left, Vector128<float>  right) => CompareGreaterThan(left, right);
        /// <summary>
        ///   <para>  f64x2.gt</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<double> CompareGreaterThan(Vector128<double> left, Vector128<double> right) => CompareGreaterThan(left, right);
        /// <summary>
        ///   <para>  i32x4.gt_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<nint>   CompareGreaterThan(Vector128<nint>   left, Vector128<nint>   right) => CompareGreaterThan(left, right);
        /// <summary>
        ///   <para>  i32x4.gt_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<nuint>  CompareGreaterThan(Vector128<nuint>  left, Vector128<nuint>  right) => CompareGreaterThan(left, right);

        /// <summary>
        ///   <para>  i8x16.ge_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<sbyte>  CompareGreaterThanOrEqual(Vector128<sbyte>  left, Vector128<sbyte>  right) => CompareGreaterThanOrEqual(left, right);
        /// <summary>
        ///   <para>  i8x16.ge_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<byte>   CompareGreaterThanOrEqual(Vector128<byte>   left, Vector128<byte>   right) => CompareGreaterThanOrEqual(left, right);
        /// <summary>
        ///   <para>  i16x8.ge_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  CompareGreaterThanOrEqual(Vector128<short>  left, Vector128<short>  right) => CompareGreaterThanOrEqual(left, right);
        /// <summary>
        ///   <para>  i16x8.ge_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> CompareGreaterThanOrEqual(Vector128<ushort> left, Vector128<ushort> right) => CompareGreaterThanOrEqual(left, right);
        /// <summary>
        ///   <para>  i32x4.ge_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    CompareGreaterThanOrEqual(Vector128<int>    left, Vector128<int>    right) => CompareGreaterThanOrEqual(left, right);
        /// <summary>
        ///   <para>  i32x4.ge_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   CompareGreaterThanOrEqual(Vector128<uint>   left, Vector128<uint>   right) => CompareGreaterThanOrEqual(left, right);
        /// <summary>
        ///   <para>  i32x4.ge_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   CompareGreaterThanOrEqual(Vector128<long>   left, Vector128<long>   right) => CompareGreaterThanOrEqual(left, right);
        /// <summary>
        ///   <para>  i64x2.ge_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  CompareGreaterThanOrEqual(Vector128<ulong>  left, Vector128<ulong>  right) => CompareGreaterThanOrEqual(left, right);
        /// <summary>
        ///   <para>  f32x4.ge</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  CompareGreaterThanOrEqual(Vector128<float>  left, Vector128<float>  right) => CompareGreaterThanOrEqual(left, right);
        /// <summary>
        ///   <para>  f64x2.ge</para>
        /// </summary>
        public static Vector128<double> CompareGreaterThanOrEqual(Vector128<double> left, Vector128<double> right) => CompareGreaterThanOrEqual(left, right);
        /// <summary>
        ///   <para>  i32x4.ge_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<nint>   CompareGreaterThanOrEqual(Vector128<nint>   left, Vector128<nint>   right) => CompareGreaterThanOrEqual(left, right);
        /// <summary>
        ///   <para>  i32x4.ge_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<nuint>  CompareGreaterThanOrEqual(Vector128<nuint>  left, Vector128<nuint>  right) => CompareGreaterThanOrEqual(left, right);

        // Load

        /// <summary>
        ///   <para>  v128.load</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<sbyte>  LoadVector128(sbyte*  address) => LoadVector128(address);
        /// <summary>
        ///   <para>  v128.load</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<byte>   LoadVector128(byte*   address) => LoadVector128(address);
        /// <summary>
        ///   <para>  v128.load</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<short>  LoadVector128(short*  address) => LoadVector128(address);
        /// <summary>
        ///   <para>  v128.load</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<ushort> LoadVector128(ushort* address) => LoadVector128(address);
        /// <summary>
        ///   <para>  v128.load</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<int>    LoadVector128(int*    address) => LoadVector128(address);
        /// <summary>
        ///   <para>  v128.load</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<uint>   LoadVector128(uint*   address) => LoadVector128(address);
        /// <summary>
        ///   <para>  v128.load</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<long>   LoadVector128(long*   address) => LoadVector128(address);
        /// <summary>
        ///   <para>  v128.load</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<ulong>  LoadVector128(ulong*  address) => LoadVector128(address);
        /// <summary>
        ///   <para>  v128.load</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<float>  LoadVector128(float*  address) => LoadVector128(address);
        /// <summary>
        ///   <para>  v128.load</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<double> LoadVector128(double* address) => LoadVector128(address);
        /// <summary>
        ///   <para>  v128.load</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<nint>   LoadVector128(nint*   address) => LoadVector128(address);
        /// <summary>
        ///   <para>  v128.load</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<nuint>  LoadVector128(nuint*  address) => LoadVector128(address);

        /// <summary>
        ///   <para>  v128.load32.zero</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<int>    LoadScalarVector128(int*    address) => LoadScalarVector128(address);
        /// <summary>
        ///   <para>  v128.load32.zero</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<uint>   LoadScalarVector128(uint*   address) => LoadScalarVector128(address);
        /// <summary>
        ///   <para>  v128.load64.zero</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<long>   LoadScalarVector128(long*   address) => LoadScalarVector128(address);
        /// <summary>
        ///   <para>  v128.load64.zero</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<ulong>  LoadScalarVector128(ulong*  address) => LoadScalarVector128(address);
        /// <summary>
        ///   <para>  v128.load32.zero</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<float>  LoadScalarVector128(float*  address) => LoadScalarVector128(address);
        /// <summary>
        ///   <para>  v128.load64.zero</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<double> LoadScalarVector128(double* address) => LoadScalarVector128(address);
        /// <summary>
        ///   <para>  v128.load32.zero</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<nint>   LoadScalarVector128(nint*   address) => LoadScalarVector128(address);
        /// <summary>
        ///   <para>  v128.load32.zero</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<nuint>  LoadScalarVector128(nuint*  address) => LoadScalarVector128(address);

        /// <summary>
        ///   <para>  v128.load8_splat</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<sbyte>  LoadScalarAndSplatVector128(sbyte*  address) => LoadScalarAndSplatVector128(address);
        /// <summary>
        ///   <para>  v128.load8_splat</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<byte>   LoadScalarAndSplatVector128(byte*   address) => LoadScalarAndSplatVector128(address);
        /// <summary>
        ///   <para>  v128.load16_splat</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<short>  LoadScalarAndSplatVector128(short*  address) => LoadScalarAndSplatVector128(address);
        /// <summary>
        ///   <para>  v128.load16_splat</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<ushort> LoadScalarAndSplatVector128(ushort* address) => LoadScalarAndSplatVector128(address);
        /// <summary>
        ///   <para>  v128.load32_splat</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<int>    LoadScalarAndSplatVector128(int*    address) => LoadScalarAndSplatVector128(address);
        /// <summary>
        ///   <para>  v128.load32_splat</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<uint>   LoadScalarAndSplatVector128(uint*   address) => LoadScalarAndSplatVector128(address);
        /// <summary>
        ///   <para>  v128.load64_splat</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<long>   LoadScalarAndSplatVector128(long*   address) => LoadScalarAndSplatVector128(address);
        /// <summary>
        ///   <para>  v128.load64_splat</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<ulong>  LoadScalarAndSplatVector128(ulong*  address) => LoadScalarAndSplatVector128(address);
        /// <summary>
        ///   <para>  v128.load64_splat</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<float>  LoadScalarAndSplatVector128(float*  address) => LoadScalarAndSplatVector128(address);
        /// <summary>
        ///   <para>  v128.load64_splat</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<double> LoadScalarAndSplatVector128(double* address) => LoadScalarAndSplatVector128(address);
        /// <summary>
        ///   <para>  v128.load64_splat</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<nint>   LoadScalarAndSplatVector128(nint*   address) => LoadScalarAndSplatVector128(address);
        /// <summary>
        ///   <para>  v128.load64_splat</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<nuint>  LoadScalarAndSplatVector128(nuint*  address) => LoadScalarAndSplatVector128(address);

        /// <summary>
        ///   <para>  v128.load8_lane</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<sbyte>  LoadScalarAndInsert(sbyte*  address, Vector128<sbyte>  vector, [ConstantExpected(Max = (byte)(15))] byte index) => LoadScalarAndInsert(address, vector, index); // takes ImmLaneIdx16
        /// <summary>
        ///   <para>  v128.load8_lane</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<byte>   LoadScalarAndInsert(byte*   address, Vector128<byte>   vector, [ConstantExpected(Max = (byte)(15))] byte index) => LoadScalarAndInsert(address, vector, index); // takes ImmLaneIdx16
        /// <summary>
        ///   <para>  v128.load16_lane</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<short>  LoadScalarAndInsert(short*  address, Vector128<short>  vector, [ConstantExpected(Max = (byte)(7))] byte index) => LoadScalarAndInsert(address, vector, index); // takes ImmLaneIdx8
        /// <summary>
        ///   <para>  v128.load16_lane</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<ushort> LoadScalarAndInsert(ushort* address, Vector128<ushort> vector, [ConstantExpected(Max = (byte)(7))] byte index) => LoadScalarAndInsert(address, vector, index); // takes ImmLaneIdx8
        /// <summary>
        ///   <para>  v128.load32_lane</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<int>    LoadScalarAndInsert(int*    address, Vector128<int>    vector, [ConstantExpected(Max = (byte)(3))] byte index) => LoadScalarAndInsert(address, vector, index); // takes ImmLaneIdx4
        /// <summary>
        ///   <para>  v128.load32_lane</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<uint>   LoadScalarAndInsert(uint*   address, Vector128<uint>   vector, [ConstantExpected(Max = (byte)(3))] byte index) => LoadScalarAndInsert(address, vector, index); // takes ImmLaneIdx4
        /// <summary>
        ///   <para>  v128.load64_lane</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<long>   LoadScalarAndInsert(long*   address, Vector128<long>   vector, [ConstantExpected(Max = (byte)(1))] byte index) => LoadScalarAndInsert(address, vector, index); // takes ImmLaneIdx2
        /// <summary>
        ///   <para>  v128.load64_lane</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<ulong>  LoadScalarAndInsert(ulong*  address, Vector128<ulong>  vector, [ConstantExpected(Max = (byte)(1))] byte index) => LoadScalarAndInsert(address, vector, index); // takes ImmLaneIdx2
        /// <summary>
        ///   <para>  v128.load32_lane</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<float>  LoadScalarAndInsert(float*  address, Vector128<float>  vector, [ConstantExpected(Max = (byte)(3))] byte index) => LoadScalarAndInsert(address, vector, index); // takes ImmLaneIdx4
        /// <summary>
        ///   <para>  v128.load64_lane</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<double> LoadScalarAndInsert(double* address, Vector128<double> vector, [ConstantExpected(Max = (byte)(1))] byte index) => LoadScalarAndInsert(address, vector, index); // takes ImmLaneIdx2
        /// <summary>
        ///   <para>  v128.load32_lane</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<nint>   LoadScalarAndInsert(nint*   address, Vector128<nint>   vector, [ConstantExpected(Max = (byte)(3))] byte index) => LoadScalarAndInsert(address, vector, index); // takes ImmLaneIdx4
        /// <summary>
        ///   <para>  v128.load32_lane</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<nuint>  LoadScalarAndInsert(nuint*  address, Vector128<nuint>  vector, [ConstantExpected(Max = (byte)(3))] byte index) => LoadScalarAndInsert(address, vector, index); // takes ImmLaneIdx4

        // Store

        /// <summary>
        ///   <para>  v128.store</para>
        /// </summary>
        [Intrinsic]
        public static unsafe void Store(sbyte*  address, Vector128<sbyte>  source) => Store(address, source);
        /// <summary>
        ///   <para>  v128.store</para>
        /// </summary>
        [Intrinsic]
        public static unsafe void Store(byte*   address, Vector128<byte>   source) => Store(address, source);
        /// <summary>
        ///   <para>  v128.store</para>
        /// </summary>
        [Intrinsic]
        public static unsafe void Store(short*  address, Vector128<short>  source) => Store(address, source);
        /// <summary>
        ///   <para>  v128.store</para>
        /// </summary>
        [Intrinsic]
        public static unsafe void Store(ushort* address, Vector128<ushort> source) => Store(address, source);
        /// <summary>
        ///   <para>  v128.store</para>
        /// </summary>
        [Intrinsic]
        public static unsafe void Store(int*    address, Vector128<int>    source) => Store(address, source);
        /// <summary>
        ///   <para>  v128.store</para>
        /// </summary>
        [Intrinsic]
        public static unsafe void Store(uint*   address, Vector128<uint>   source) => Store(address, source);
        /// <summary>
        ///   <para>  v128.store</para>
        /// </summary>
        [Intrinsic]
        public static unsafe void Store(long*   address, Vector128<long>   source) => Store(address, source);
        /// <summary>
        ///   <para>  v128.store</para>
        /// </summary>
        [Intrinsic]
        public static unsafe void Store(ulong*  address, Vector128<ulong>  source) => Store(address, source);
        /// <summary>
        ///   <para>  v128.store</para>
        /// </summary>
        [Intrinsic]
        public static unsafe void Store(float*  address, Vector128<float>  source) => Store(address, source);
        /// <summary>
        ///   <para>  v128.store</para>
        /// </summary>
        [Intrinsic]
        public static unsafe void Store(double* address, Vector128<double> source) => Store(address, source);
        /// <summary>
        ///   <para>  v128.store</para>
        /// </summary>
        [Intrinsic]
        public static unsafe void Store(nint*   address, Vector128<nint>   source) => Store(address, source);
        /// <summary>
        ///   <para>  v128.store</para>
        /// </summary>
        [Intrinsic]
        public static unsafe void Store(nuint*  address, Vector128<nuint>  source) => Store(address, source);

        /// <summary>
        ///   <para>  v128.store8_lane</para>
        /// </summary>
        [Intrinsic]
        public static unsafe void StoreSelectedScalar(sbyte*  address, Vector128<sbyte>  source, [ConstantExpected(Max = (byte)(15))] byte index) => StoreSelectedScalar(address, source, index); // takes ImmLaneIdx16
        /// <summary>
        ///   <para>  v128.store8_lane</para>
        /// </summary>
        [Intrinsic]
        public static unsafe void StoreSelectedScalar(byte*   address, Vector128<byte>   source, [ConstantExpected(Max = (byte)(15))] byte index) => StoreSelectedScalar(address, source, index); // takes ImmLaneIdx16
        /// <summary>
        ///   <para>  v128.store16_lane</para>
        /// </summary>
        [Intrinsic]
        public static unsafe void StoreSelectedScalar(short*  address, Vector128<short>  source, [ConstantExpected(Max = (byte)(7))] byte index) => StoreSelectedScalar(address, source, index); // takes ImmLaneIdx8
        /// <summary>
        ///   <para>  v128.store16_lane</para>
        /// </summary>
        [Intrinsic]
        public static unsafe void StoreSelectedScalar(ushort* address, Vector128<ushort> source, [ConstantExpected(Max = (byte)(7))] byte index) => StoreSelectedScalar(address, source, index); // takes ImmLaneIdx8
        /// <summary>
        ///   <para>  v128.store32_lane</para>
        /// </summary>
        [Intrinsic]
        public static unsafe void StoreSelectedScalar(int*    address, Vector128<int>    source, [ConstantExpected(Max = (byte)(3))] byte index) => StoreSelectedScalar(address, source, index); // takes ImmLaneIdx4
        /// <summary>
        ///   <para>  v128.store32_lane</para>
        /// </summary>
        [Intrinsic]
        public static unsafe void StoreSelectedScalar(uint*   address, Vector128<uint>   source, [ConstantExpected(Max = (byte)(3))] byte index) => StoreSelectedScalar(address, source, index); // takes ImmLaneIdx4
        /// <summary>
        ///   <para>  v128.store64_lane</para>
        /// </summary>
        [Intrinsic]
        public static unsafe void StoreSelectedScalar(long*   address, Vector128<long>   source, [ConstantExpected(Max = (byte)(1))] byte index) => StoreSelectedScalar(address, source, index); // takes ImmLaneIdx2
        /// <summary>
        ///   <para>  v128.store64_lane</para>
        /// </summary>
        [Intrinsic]
        public static unsafe void StoreSelectedScalar(ulong*  address, Vector128<ulong>  source, [ConstantExpected(Max = (byte)(1))] byte index) => StoreSelectedScalar(address, source, index); // takes ImmLaneIdx2
        /// <summary>
        ///   <para>  v128.store32_lane</para>
        /// </summary>
        [Intrinsic]
        public static unsafe void StoreSelectedScalar(float*  address, Vector128<float>  source, [ConstantExpected(Max = (byte)(3))] byte index) => StoreSelectedScalar(address, source, index); // takes ImmLaneIdx4
        /// <summary>
        ///   <para>  v128.store64_lane</para>
        /// </summary>
        [Intrinsic]
        public static unsafe void StoreSelectedScalar(double* address, Vector128<double> source, [ConstantExpected(Max = (byte)(1))] byte index) => StoreSelectedScalar(address, source, index); // takes ImmLaneIdx2
        /// <summary>
        ///   <para>  v128.store32_lane</para>
        /// </summary>
        [Intrinsic]
        public static unsafe void StoreSelectedScalar(nint*   address, Vector128<nint>   source, [ConstantExpected(Max = (byte)(3))] byte index) => StoreSelectedScalar(address, source, index);
        /// <summary>
        ///   <para>  v128.store32_lane</para>
        /// </summary>
        [Intrinsic]
        public static unsafe void StoreSelectedScalar(nuint*  address, Vector128<nuint>  source, [ConstantExpected(Max = (byte)(3))] byte index) => StoreSelectedScalar(address, source, index);

        /// <summary>
        ///   <para>  v128.load8x8_s</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<short>  LoadWideningVector128(sbyte*  address) => LoadWideningVector128(address);
        /// <summary>
        ///   <para>  v128.load8x8_u</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<ushort> LoadWideningVector128(byte*   address) => LoadWideningVector128(address);
        /// <summary>
        ///   <para>  v128.load16x4_s</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<int>    LoadWideningVector128(short*  address) => LoadWideningVector128(address);
        /// <summary>
        ///   <para>  v128.load16x4_u</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<uint>   LoadWideningVector128(ushort* address) => LoadWideningVector128(address);
        /// <summary>
        ///   <para>  v128.load32x2_s</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<long>   LoadWideningVector128(int*    address) => LoadWideningVector128(address);
        /// <summary>
        ///   <para>  v128.load32x2_u</para>
        /// </summary>
        [Intrinsic]
        public static unsafe Vector128<ulong>  LoadWideningVector128(uint*   address) => LoadWideningVector128(address);

        // Floating-point sign bit operations

        /// <summary>
        ///   <para>  f32x4.neg</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  Negate(Vector128<float>  value) => Negate(value);
        /// <summary>
        ///   <para>  f64x2.neg</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<double> Negate(Vector128<double> value) => Negate(value);

        /// <summary>
        ///   <para>  f32x4.abs</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  Abs(Vector128<float>  value) => Abs(value);
        /// <summary>
        ///   <para>  f64x2.abs</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<double> Abs(Vector128<double> value) => Abs(value);

        // Floating-point min and max

        /// <summary>
        ///   <para>  f32x4.min</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  Min(Vector128<float>  left, Vector128<float>  right) => Min(left, right);
        /// <summary>
        ///   <para>  f64x2.min</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<double> Min(Vector128<double> left, Vector128<double> right) => Min(left, right);

        /// <summary>
        ///   <para>  f32x4.max</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  Max(Vector128<float>  left, Vector128<float>  right) => Max(left, right);
        /// <summary>
        ///   <para>  f64x2.max</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<double> Max(Vector128<double> left, Vector128<double> right) => Max(left, right);

        /// <summary>
        ///   <para>  f32x4.pmin</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  PseudoMin(Vector128<float>  left, Vector128<float>  right) => PseudoMin(left, right);
        /// <summary>
        ///   <para>  f64x2.pmin</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<double> PseudoMin(Vector128<double> left, Vector128<double> right) => PseudoMin(left, right);

        /// <summary>
        ///   <para>  f32x4.pmax</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  PseudoMax(Vector128<float>  left, Vector128<float>  right) => PseudoMax(left, right);
        /// <summary>
        ///   <para>  f64x2.pmax</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<double> PseudoMax(Vector128<double> left, Vector128<double> right) => PseudoMax(left, right);

        // Floating-point arithmetic

        /// <summary>
        ///   <para>  f32x4.add</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  Add(Vector128<float>  left, Vector128<float>  right) => Add(left, right);
        /// <summary>
        ///   <para>  f64x2.add</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<double> Add(Vector128<double> left, Vector128<double> right) => Add(left, right);

        /// <summary>
        ///   <para>  f32x4.sub</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  Subtract(Vector128<float>  left, Vector128<float>  right) => Subtract(left, right);
        /// <summary>
        ///   <para>  f64x2.sub</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<double> Subtract(Vector128<double> left, Vector128<double> right) => Subtract(left, right);

        /// <summary>
        ///   <para>  f32x4.div</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  Divide(Vector128<float>  left, Vector128<float>  right) => Divide(left, right);
        /// <summary>
        ///   <para>  f64x2.div</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<double> Divide(Vector128<double> left, Vector128<double> right) => Divide(left, right);

        /// <summary>
        ///   <para>  f32x4.mul</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  Multiply(Vector128<float>  left, Vector128<float>  right) => Multiply(left, right);
        /// <summary>
        ///   <para>  f64x2.mul</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<double> Multiply(Vector128<double> left, Vector128<double> right) => Multiply(left, right);

        /// <summary>
        ///   <para>  f32x4.sqrt</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  Sqrt(Vector128<float>  value) => Sqrt(value);
        /// <summary>
        ///   <para>  f64x2.sqrt</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<double> Sqrt(Vector128<double> value) => Sqrt(value);

        /// <summary>
        ///   <para>  f32x4.ceil</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  Ceiling(Vector128<float>  value) => Ceiling(value);
        /// <summary>
        ///   <para>  f64x2.ceil</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<double> Ceiling(Vector128<double> value) => Ceiling(value);

        /// <summary>
        ///   <para>  f32x4.floor</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  Floor(Vector128<float>  value) => Floor(value);
        /// <summary>
        ///   <para>  f64x2.floor</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<double> Floor(Vector128<double> value) => Floor(value);

        /// <summary>
        ///   <para>  f32x4.trunc</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  Truncate(Vector128<float>  value) => Truncate(value);
        /// <summary>
        ///   <para>  f64x2.trunc</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<double> Truncate(Vector128<double> value) => Truncate(value);

        /// <summary>
        ///   <para>  f32x4.nearest</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<float>  RoundToNearest(Vector128<float>  value) => RoundToNearest(value);
        /// <summary>
        ///   <para>  f64x2.nearest</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<double> RoundToNearest(Vector128<double> value) => RoundToNearest(value);

        // Conversions

        /// <summary>
        ///   <para>  f32x4.convert_i32x4_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<float> ConvertToSingle(Vector128<int>    value) => ConvertToSingle(value);
        /// <summary>
        ///   <para>  f32x4.convert_i32x4_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<float> ConvertToSingle(Vector128<uint>   value) => ConvertToSingle(value);
        /// <summary>
        ///   <para>f32x4.demote_f64x2_zero</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<float> ConvertToSingle(Vector128<double> value) => ConvertToSingle(value);

        /// <summary>
        ///   <para>  f64x2.convert_low_i32x4_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<double> ConvertToDoubleLower(Vector128<int>   value) => ConvertToDoubleLower(value);
        /// <summary>
        ///   <para>  f64x2.convert_low_i32x4_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<double> ConvertToDoubleLower(Vector128<uint>  value) => ConvertToDoubleLower(value);
        /// <summary>
        ///   <para>  f64x2.promote_low_f32x4</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<double> ConvertToDoubleLower(Vector128<float> value) => ConvertToDoubleLower(value);

        /// <summary>
        ///   <para>  i32x4.trunc_sat_f32x4_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<int>  ConvertToInt32Saturate(Vector128<float> value) => ConvertToInt32Saturate(value);
        /// <summary>
        ///   <para>  i32x4.trunc_sat_f32x4_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<uint> ConvertToUInt32Saturate(Vector128<float> value) => ConvertToUInt32Saturate(value);

        /// <summary>
        ///   <para>  i32x4.trunc_sat_f64x2_s_zero</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<int>  ConvertToInt32Saturate(Vector128<double> value) => ConvertToInt32Saturate(value);
        /// <summary>
        ///   <para>  i32x4.trunc_sat_f64x2_u_zero</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<uint> ConvertToUInt32Saturate(Vector128<double> value) => ConvertToUInt32Saturate(value);

        /// <summary>
        ///   <para>  i8x16.narrow_i16x8_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<sbyte> ConvertNarrowingSaturateSigned(Vector128<short> lower, Vector128<short> upper) => ConvertNarrowingSaturateSigned(lower, upper);

        /// <summary>
        ///   <para>  i16x8.narrow_i32x4_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<short> ConvertNarrowingSaturateSigned(Vector128<int>   lower, Vector128<int>   upper) => ConvertNarrowingSaturateSigned(lower, upper);

        /// <summary>
        ///   <para>  i8x16.narrow_i16x8_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<byte>  ConvertNarrowingSaturateUnsigned(Vector128<short> lower, Vector128<short> upper) => ConvertNarrowingSaturateUnsigned(lower, upper);

        /// <summary>
        ///   <para>  i16x8.narrow_i32x4_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> ConvertNarrowingSaturateUnsigned(Vector128<int>  lower, Vector128<int>   upper) => ConvertNarrowingSaturateUnsigned(lower, upper);

        /// <summary>
        ///   <para>  i16x8.extend_low_i8x16_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  SignExtendWideningLower(Vector128<sbyte>  value) => SignExtendWideningLower(value);
        /// <summary>
        ///   <para>  i16x8.extend_low_i8x16_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> SignExtendWideningLower(Vector128<byte>   value) => SignExtendWideningLower(value);
        /// <summary>
        ///   <para>  i32x4.extend_low_i16x8_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    SignExtendWideningLower(Vector128<short>  value) => SignExtendWideningLower(value);
        /// <summary>
        ///   <para>  i32x4.extend_low_i16x8_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   SignExtendWideningLower(Vector128<ushort> value) => SignExtendWideningLower(value);
        /// <summary>
        ///   <para>  i64x2.extend_low_i32x4_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   SignExtendWideningLower(Vector128<int>    value) => SignExtendWideningLower(value);
        /// <summary>
        ///   <para>  i64x2.extend_low_i32x4_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  SignExtendWideningLower(Vector128<uint>   value) => SignExtendWideningLower(value);

        /// <summary>
        ///   <para>  i16x8.extend_high_i8x16_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  SignExtendWideningUpper(Vector128<sbyte>  value) => SignExtendWideningUpper(value);
        /// <summary>
        ///   <para>  i16x8.extend_high_i8x16_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> SignExtendWideningUpper(Vector128<byte>   value) => SignExtendWideningUpper(value);
        /// <summary>
        ///   <para>  i32x4.extend_high_i16x8_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    SignExtendWideningUpper(Vector128<short>  value) => SignExtendWideningUpper(value);
        /// <summary>
        ///   <para>  i32x4.extend_high_i16x8_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   SignExtendWideningUpper(Vector128<ushort> value) => SignExtendWideningUpper(value);
        /// <summary>
        ///   <para>  i64x2.extend_high_i32x4_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   SignExtendWideningUpper(Vector128<int>    value) => SignExtendWideningUpper(value);
        /// <summary>
        ///   <para>  i64x2.extend_high_i32x4_s</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  SignExtendWideningUpper(Vector128<uint>   value) => SignExtendWideningUpper(value);

        /// <summary>
        ///   <para>  i16x8.extend_low_i8x16_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  ZeroExtendWideningLower(Vector128<sbyte>  value) => ZeroExtendWideningLower(value);
        /// <summary>
        ///   <para>  i16x8.extend_low_i8x16_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> ZeroExtendWideningLower(Vector128<byte>   value) => ZeroExtendWideningLower(value);
        /// <summary>
        ///   <para>  i32x4.extend_low_i16x8_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    ZeroExtendWideningLower(Vector128<short>  value) => ZeroExtendWideningLower(value);
        /// <summary>
        ///   <para>  i32x4.extend_low_i16x8_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   ZeroExtendWideningLower(Vector128<ushort> value) => ZeroExtendWideningLower(value);
        /// <summary>
        ///   <para>  i64x2.extend_low_i32x4_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   ZeroExtendWideningLower(Vector128<int>    value) => ZeroExtendWideningLower(value);
        /// <summary>
        ///   <para>  i64x2.extend_low_i32x4_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  ZeroExtendWideningLower(Vector128<uint>   value) => ZeroExtendWideningLower(value);

        /// <summary>
        ///   <para>  i16x8.extend_high_i8x16_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<short>  ZeroExtendWideningUpper(Vector128<sbyte>  value) => ZeroExtendWideningUpper(value);
        /// <summary>
        ///   <para>  i16x8.extend_high_i8x16_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ushort> ZeroExtendWideningUpper(Vector128<byte>   value) => ZeroExtendWideningUpper(value);
        /// <summary>
        ///   <para>  i32x4.extend_high_i16x8_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<int>    ZeroExtendWideningUpper(Vector128<short>  value) => ZeroExtendWideningUpper(value);
        /// <summary>
        ///   <para>  i32x4.extend_high_i16x8_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<uint>   ZeroExtendWideningUpper(Vector128<ushort> value) => ZeroExtendWideningUpper(value);
        /// <summary>
        ///   <para>  i64x2.extend_high_i32x4_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<long>   ZeroExtendWideningUpper(Vector128<int>    value) => ZeroExtendWideningUpper(value);
        /// <summary>
        ///   <para>  i64x2.extend_high_i32x4_u</para>
        /// </summary>
        [Intrinsic]
        public static Vector128<ulong>  ZeroExtendWideningUpper(Vector128<uint>   value) => ZeroExtendWideningUpper(value);
    }
}
