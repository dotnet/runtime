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
        public static Vector128<sbyte> Swizzle(Vector128<sbyte> vector, Vector128<sbyte> indices) => Swizzle(vector, indices);
        /// <summary>  i8x16.relaxed_swizzle</summary>
        [Intrinsic]
        public static Vector128<byte>  Swizzle(Vector128<byte>  vector, Vector128<byte>  indices) => Swizzle(vector, indices);

        // Relaxed truncating float-to-int conversions. For NaN or out-of-range inputs the result is
        // implementation-defined; the saturating PackedSimd.ConvertToInt32Saturate / ConvertToUInt32Saturate
        // overloads provide deterministic semantics.

        /// <summary>  i32x4.relaxed_trunc_f32x4_s</summary>
        [Intrinsic]
        public static Vector128<int>  ConvertToInt32(Vector128<float> value) => ConvertToInt32(value);
        /// <summary>  i32x4.relaxed_trunc_f32x4_u</summary>
        [Intrinsic]
        public static Vector128<uint> ConvertToUInt32(Vector128<float> value) => ConvertToUInt32(value);
        /// <summary>  i32x4.relaxed_trunc_f64x2_s_zero</summary>
        [Intrinsic]
        public static Vector128<int>  ConvertToInt32(Vector128<double> value) => ConvertToInt32(value);
        /// <summary>  i32x4.relaxed_trunc_f64x2_u_zero</summary>
        [Intrinsic]
        public static Vector128<uint> ConvertToUInt32(Vector128<double> value) => ConvertToUInt32(value);

        // Relaxed fused multiply-add. Whether the intermediate product is rounded before the add
        // (and whether the underlying instruction is a true fused FMA) is implementation-defined.

        /// <summary>  f32x4.relaxed_madd</summary>
        [Intrinsic]
        public static Vector128<float>  MultiplyAdd(Vector128<float>  a, Vector128<float>  b, Vector128<float>  c) => MultiplyAdd(a, b, c);
        /// <summary>  f64x2.relaxed_madd</summary>
        [Intrinsic]
        public static Vector128<double> MultiplyAdd(Vector128<double> a, Vector128<double> b, Vector128<double> c) => MultiplyAdd(a, b, c);

        /// <summary>  f32x4.relaxed_nmadd</summary>
        [Intrinsic]
        public static Vector128<float>  MultiplyAddNegated(Vector128<float>  a, Vector128<float>  b, Vector128<float>  c) => MultiplyAddNegated(a, b, c);
        /// <summary>  f64x2.relaxed_nmadd</summary>
        [Intrinsic]
        public static Vector128<double> MultiplyAddNegated(Vector128<double> a, Vector128<double> b, Vector128<double> c) => MultiplyAddNegated(a, b, c);

        // Relaxed lane select. The mask is interpreted per-byte/word; lanes where the mask bit is
        // neither all-ones nor all-zeros produce implementation-defined results. For deterministic
        // selection use Vector128.ConditionalSelect.

        /// <summary>  i8x16.relaxed_laneselect</summary>
        [Intrinsic]
        public static Vector128<sbyte>  LaneSelect(Vector128<sbyte>  left, Vector128<sbyte>  right, Vector128<sbyte>  mask) => LaneSelect(left, right, mask);
        /// <summary>  i8x16.relaxed_laneselect</summary>
        [Intrinsic]
        public static Vector128<byte>   LaneSelect(Vector128<byte>   left, Vector128<byte>   right, Vector128<byte>   mask) => LaneSelect(left, right, mask);
        /// <summary>  i16x8.relaxed_laneselect</summary>
        [Intrinsic]
        public static Vector128<short>  LaneSelect(Vector128<short>  left, Vector128<short>  right, Vector128<short>  mask) => LaneSelect(left, right, mask);
        /// <summary>  i16x8.relaxed_laneselect</summary>
        [Intrinsic]
        public static Vector128<ushort> LaneSelect(Vector128<ushort> left, Vector128<ushort> right, Vector128<ushort> mask) => LaneSelect(left, right, mask);
        /// <summary>  i32x4.relaxed_laneselect</summary>
        [Intrinsic]
        public static Vector128<int>    LaneSelect(Vector128<int>    left, Vector128<int>    right, Vector128<int>    mask) => LaneSelect(left, right, mask);
        /// <summary>  i32x4.relaxed_laneselect</summary>
        [Intrinsic]
        public static Vector128<uint>   LaneSelect(Vector128<uint>   left, Vector128<uint>   right, Vector128<uint>   mask) => LaneSelect(left, right, mask);
        /// <summary>  i64x2.relaxed_laneselect</summary>
        [Intrinsic]
        public static Vector128<long>   LaneSelect(Vector128<long>   left, Vector128<long>   right, Vector128<long>   mask) => LaneSelect(left, right, mask);
        /// <summary>  i64x2.relaxed_laneselect</summary>
        [Intrinsic]
        public static Vector128<ulong>  LaneSelect(Vector128<ulong>  left, Vector128<ulong>  right, Vector128<ulong>  mask) => LaneSelect(left, right, mask);

        // Relaxed min/max. NaN handling and sign-of-zero handling are implementation-defined.
        // For IEEE-compliant min/max use PackedSimd.Min/Max; for pseudo-min/max (one-sided NaN
        // propagation) use PackedSimd.PseudoMin/PseudoMax.

        /// <summary>  f32x4.relaxed_min</summary>
        [Intrinsic]
        public static Vector128<float>  Min(Vector128<float>  left, Vector128<float>  right) => Min(left, right);
        /// <summary>  f32x4.relaxed_max</summary>
        [Intrinsic]
        public static Vector128<float>  Max(Vector128<float>  left, Vector128<float>  right) => Max(left, right);
        /// <summary>  f64x2.relaxed_min</summary>
        [Intrinsic]
        public static Vector128<double> Min(Vector128<double> left, Vector128<double> right) => Min(left, right);
        /// <summary>  f64x2.relaxed_max</summary>
        [Intrinsic]
        public static Vector128<double> Max(Vector128<double> left, Vector128<double> right) => Max(left, right);

        // Relaxed Q15 multiply with rounding. Differs from PackedSimd.MultiplyRoundedSaturateQ15
        // (i16x8.q15mulr_sat_s) in that the multiplication of INT16_MIN by INT16_MIN produces an
        // implementation-defined value (typically INT16_MIN unsaturated on x86).

        /// <summary>  i16x8.relaxed_q15mulr_s</summary>
        [Intrinsic]
        public static Vector128<short> MultiplyRoundedQ15(Vector128<short> left, Vector128<short> right) => MultiplyRoundedQ15(left, right);

        // Relaxed dot products for mixed unsigned-by-signed bytes. The second operand is interpreted
        // as signed but its lanes must be in the range [-64, 127] (the "i7" constraint); lanes whose
        // value falls outside this range produce implementation-defined results.

        /// <summary>  i16x8.relaxed_dot_i8x16_i7x16_s — multiplies adjacent (byte, sbyte) pairs and sums each pair into a signed 16-bit lane.</summary>
        [Intrinsic]
        public static Vector128<short> DotProduct(Vector128<byte> left, Vector128<sbyte> right) => DotProduct(left, right);

        /// <summary>  i32x4.relaxed_dot_i8x16_i7x16_add_s — multiplies four adjacent (byte, sbyte) pairs, sums them with a signed 32-bit accumulator, and returns the result.</summary>
        [Intrinsic]
        public static Vector128<int> DotProductAdd(Vector128<byte> left, Vector128<sbyte> right, Vector128<int> accumulator) => DotProductAdd(left, right, accumulator);
    }
}
