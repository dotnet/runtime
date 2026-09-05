// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Runtime.Intrinsics.Wasm
{
    /// <summary>Provides access to the WebAssembly relaxed SIMD instructions via intrinsics.</summary>
    /// <remarks>
    /// <para>
    /// Operations exposed on this class behave "relaxedly": for inputs outside a well-defined range
    /// the result is implementation-defined and may differ between WebAssembly engines and host
    /// architectures. Callers that require deterministic semantics across engines should use the
    /// corresponding <see cref="PackedSimd"/> operation (where available) instead.
    /// </para>
    /// <para>
    /// All members of this class require the runtime to support the
    /// <see href="https://github.com/WebAssembly/relaxed-simd">relaxed SIMD</see> WebAssembly proposal.
    /// </para>
    /// </remarks>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class RelaxedSimd
    {
        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static bool IsSupported { [Intrinsic] get { return IsSupported; } }

        // Relaxed swizzle: like PackedSimd.Swizzle, but for index lanes outside [0, 16) the
        // result is implementation-defined (often the index modulo 16 on x86, zero on ARM).

        /// <summary>  i8x16.relaxed_swizzle</summary>
        [Intrinsic]
        public static Vector128<sbyte> SwizzleNative(Vector128<sbyte> vector, Vector128<sbyte> indices) => SwizzleNative(vector, indices);
        /// <summary>  i8x16.relaxed_swizzle</summary>
        [Intrinsic]
        public static Vector128<byte>  SwizzleNative(Vector128<byte>  vector, Vector128<byte>  indices) => SwizzleNative(vector, indices);

        // Relaxed truncating float-to-int conversions. For NaN or out-of-range inputs the result is
        // implementation-defined; the saturating PackedSimd.ConvertToInt32Saturate / ConvertToUInt32Saturate
        // overloads provide deterministic semantics.

        /// <summary>  i32x4.relaxed_trunc_f32x4_s</summary>
        [Intrinsic]
        public static Vector128<int>  ConvertToInt32Native(Vector128<float> value) => ConvertToInt32Native(value);
        /// <summary>  i32x4.relaxed_trunc_f32x4_u</summary>
        [Intrinsic]
        public static Vector128<uint> ConvertToUInt32Native(Vector128<float> value) => ConvertToUInt32Native(value);
        /// <summary>  i32x4.relaxed_trunc_f64x2_s_zero</summary>
        [Intrinsic]
        public static Vector128<int>  ConvertToInt32Native(Vector128<double> value) => ConvertToInt32Native(value);
        /// <summary>  i32x4.relaxed_trunc_f64x2_u_zero</summary>
        [Intrinsic]
        public static Vector128<uint> ConvertToUInt32Native(Vector128<double> value) => ConvertToUInt32Native(value);

        // Relaxed fused multiply-add. Whether the intermediate product is rounded before the add
        // (and whether the underlying instruction is a true fused FMA) is implementation-defined.

        /// <summary>  f32x4.relaxed_madd</summary>
        [Intrinsic]
        public static Vector128<float>  MultiplyAddEstimate(Vector128<float>  left, Vector128<float>  right, Vector128<float>  addend) => MultiplyAddEstimate(left, right, addend);
        /// <summary>  f64x2.relaxed_madd</summary>
        [Intrinsic]
        public static Vector128<double> MultiplyAddEstimate(Vector128<double> left, Vector128<double> right, Vector128<double> addend) => MultiplyAddEstimate(left, right, addend);

        /// <summary>  f32x4.relaxed_nmadd</summary>
        [Intrinsic]
        public static Vector128<float>  MultiplyAddNegatedEstimate(Vector128<float>  left, Vector128<float>  right, Vector128<float>  addend) => MultiplyAddNegatedEstimate(left, right, addend);
        /// <summary>  f64x2.relaxed_nmadd</summary>
        [Intrinsic]
        public static Vector128<double> MultiplyAddNegatedEstimate(Vector128<double> left, Vector128<double> right, Vector128<double> addend) => MultiplyAddNegatedEstimate(left, right, addend);

        // Relaxed lane select. The mask is interpreted per-byte/word; lanes where the mask bit is
        // neither all-ones nor all-zeros produce implementation-defined results. For deterministic
        // selection use Vector128.ConditionalSelect.

        /// <summary>  i8x16.relaxed_laneselect</summary>
        [Intrinsic]
        public static Vector128<sbyte>  LaneSelectNative(Vector128<sbyte>  left, Vector128<sbyte>  right, Vector128<sbyte>  mask) => LaneSelectNative(left, right, mask);
        /// <summary>  i8x16.relaxed_laneselect</summary>
        [Intrinsic]
        public static Vector128<byte>   LaneSelectNative(Vector128<byte>   left, Vector128<byte>   right, Vector128<byte>   mask) => LaneSelectNative(left, right, mask);
        /// <summary>  i16x8.relaxed_laneselect</summary>
        [Intrinsic]
        public static Vector128<short>  LaneSelectNative(Vector128<short>  left, Vector128<short>  right, Vector128<short>  mask) => LaneSelectNative(left, right, mask);
        /// <summary>  i16x8.relaxed_laneselect</summary>
        [Intrinsic]
        public static Vector128<ushort> LaneSelectNative(Vector128<ushort> left, Vector128<ushort> right, Vector128<ushort> mask) => LaneSelectNative(left, right, mask);
        /// <summary>  i32x4.relaxed_laneselect</summary>
        [Intrinsic]
        public static Vector128<int>    LaneSelectNative(Vector128<int>    left, Vector128<int>    right, Vector128<int>    mask) => LaneSelectNative(left, right, mask);
        /// <summary>  i32x4.relaxed_laneselect</summary>
        [Intrinsic]
        public static Vector128<uint>   LaneSelectNative(Vector128<uint>   left, Vector128<uint>   right, Vector128<uint>   mask) => LaneSelectNative(left, right, mask);
        /// <summary>  i64x2.relaxed_laneselect</summary>
        [Intrinsic]
        public static Vector128<long>   LaneSelectNative(Vector128<long>   left, Vector128<long>   right, Vector128<long>   mask) => LaneSelectNative(left, right, mask);
        /// <summary>  i64x2.relaxed_laneselect</summary>
        [Intrinsic]
        public static Vector128<ulong>  LaneSelectNative(Vector128<ulong>  left, Vector128<ulong>  right, Vector128<ulong>  mask) => LaneSelectNative(left, right, mask);

        // Relaxed min/max. NaN handling and sign-of-zero handling are implementation-defined.
        // For IEEE-compliant min/max use PackedSimd.Min/Max; for pseudo-min/max (one-sided NaN
        // propagation) use PackedSimd.PseudoMin/PseudoMax.

        /// <summary>  f32x4.relaxed_min</summary>
        [Intrinsic]
        public static Vector128<float>  MinNative(Vector128<float>  left, Vector128<float>  right) => MinNative(left, right);
        /// <summary>  f32x4.relaxed_max</summary>
        [Intrinsic]
        public static Vector128<float>  MaxNative(Vector128<float>  left, Vector128<float>  right) => MaxNative(left, right);
        /// <summary>  f64x2.relaxed_min</summary>
        [Intrinsic]
        public static Vector128<double> MinNative(Vector128<double> left, Vector128<double> right) => MinNative(left, right);
        /// <summary>  f64x2.relaxed_max</summary>
        [Intrinsic]
        public static Vector128<double> MaxNative(Vector128<double> left, Vector128<double> right) => MaxNative(left, right);

        // Relaxed Q15 multiply with rounding. Differs from PackedSimd.MultiplyRoundedSaturateQ15
        // (i16x8.q15mulr_sat_s) in that the multiplication of INT16_MIN by INT16_MIN produces an
        // implementation-defined value (typically INT16_MIN unsaturated on x86).

        /// <summary>  i16x8.relaxed_q15mulr_s</summary>
        [Intrinsic]
        public static Vector128<short> MultiplyRoundedQ15Native(Vector128<short> left, Vector128<short> right) => MultiplyRoundedQ15Native(left, right);

        // Relaxed dot products for signed-by-unsigned bytes. Per the finished spec pseudocode,
        // operand `a` is signed and operand `b` is unsigned 7-bit; when any lane of `b` has the
        // high bit set that lane's product is implementation-defined (may be interpreted as
        // signed or unsigned). The pairwise/adjacent summation is also implementation-defined
        // saturating (may or may not saturate on overflow).

        /// <summary>  i16x8.relaxed_dot_i8x16_i7x16_s — multiplies adjacent (sbyte, byte) pairs and sums each pair into a signed 16-bit lane.</summary>
        [Intrinsic]
        public static Vector128<short> DotProductNative(Vector128<sbyte> left, Vector128<byte> right) => DotProductNative(left, right);

        /// <summary>  i32x4.relaxed_dot_i8x16_i7x16_add_s — multiplies four adjacent (sbyte, byte) pairs, sums them with a signed 32-bit accumulator, and returns the result.</summary>
        [Intrinsic]
        public static Vector128<int> DotProductAddNative(Vector128<sbyte> left, Vector128<byte> right, Vector128<int> accumulator) => DotProductAddNative(left, right, accumulator);
    }
}
