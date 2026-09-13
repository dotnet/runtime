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
    /// All operations exposed by this class require the WebAssembly engine to support the
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
        public static bool IsSupported { get { return IsSupported; } }

        /// <summary>Swizzles the elements in a vector using the specified indices.</summary>
        /// <param name="vector">The vector to swizzle.</param>
        /// <param name="indices">The indices used to select elements from <paramref name="vector" />.</param>
        /// <returns>The swizzled vector.</returns>
        /// <remarks>
        /// This method maps to the <c>i8x16.relaxed_swizzle</c> instruction. For indices outside
        /// the range [0, 16), the result is implementation-defined. Use the corresponding
        /// <c>PackedSimd.Swizzle</c> overload when deterministic handling of out-of-range indices is required.
        /// </remarks>
        public static Vector128<sbyte> SwizzleNative(Vector128<sbyte> vector, Vector128<sbyte> indices) => SwizzleNative(vector, indices);
        /// <inheritdoc cref="SwizzleNative(Vector128{sbyte}, Vector128{sbyte})" />
        public static Vector128<byte>  SwizzleNative(Vector128<byte>  vector, Vector128<byte>  indices) => SwizzleNative(vector, indices);

        /// <summary>Converts a vector of single-precision floating-point values to a vector of signed integers using native truncation.</summary>
        /// <param name="value">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        /// <remarks>
        /// This method maps to the <c>i32x4.relaxed_trunc_f32x4_s</c> instruction. The result for
        /// NaN or out-of-range inputs is implementation-defined. Use
        /// <see cref="PackedSimd.ConvertToInt32Saturate(Vector128{float})" /> for deterministic saturation.
        /// </remarks>
        public static Vector128<int>  ConvertToInt32Native(Vector128<float> value) => ConvertToInt32Native(value);
        /// <summary>Converts a vector of single-precision floating-point values to a vector of unsigned integers using native truncation.</summary>
        /// <param name="value">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        /// <remarks>
        /// This method maps to the <c>i32x4.relaxed_trunc_f32x4_u</c> instruction. The result for
        /// NaN or out-of-range inputs is implementation-defined. Use
        /// <see cref="PackedSimd.ConvertToUInt32Saturate(Vector128{float})" /> for deterministic saturation.
        /// </remarks>
        public static Vector128<uint> ConvertToUInt32Native(Vector128<float> value) => ConvertToUInt32Native(value);
        /// <summary>Converts a vector of double-precision floating-point values to a vector of signed integers using native truncation.</summary>
        /// <param name="value">The vector to convert.</param>
        /// <returns>The converted vector, with the upper two elements initialized to zero.</returns>
        /// <remarks>
        /// This method maps to the <c>i32x4.relaxed_trunc_f64x2_s_zero</c> instruction. The result
        /// for NaN or out-of-range inputs is implementation-defined. Use
        /// <see cref="PackedSimd.ConvertToInt32Saturate(Vector128{double})" /> for deterministic saturation.
        /// </remarks>
        public static Vector128<int>  ConvertToInt32Native(Vector128<double> value) => ConvertToInt32Native(value);
        /// <summary>Converts a vector of double-precision floating-point values to a vector of unsigned integers using native truncation.</summary>
        /// <param name="value">The vector to convert.</param>
        /// <returns>The converted vector, with the upper two elements initialized to zero.</returns>
        /// <remarks>
        /// This method maps to the <c>i32x4.relaxed_trunc_f64x2_u_zero</c> instruction. The result
        /// for NaN or out-of-range inputs is implementation-defined. Use
        /// <see cref="PackedSimd.ConvertToUInt32Saturate(Vector128{double})" /> for deterministic saturation.
        /// </remarks>
        public static Vector128<uint> ConvertToUInt32Native(Vector128<double> value) => ConvertToUInt32Native(value);

        /// <summary>Computes an implementation-dependent estimate of the product of two vectors added to a third vector.</summary>
        /// <param name="left">The first vector to multiply.</param>
        /// <param name="right">The second vector to multiply.</param>
        /// <param name="addend">The vector to add to the product.</param>
        /// <returns>The estimated multiply-add result.</returns>
        /// <remarks>
        /// This method maps to the corresponding <c>relaxed_madd</c> instruction. Whether the
        /// product is rounded before the addition, and whether the operation is fused, is
        /// implementation-defined.
        /// </remarks>
        public static Vector128<float>  MultiplyAddEstimate(Vector128<float>  left, Vector128<float>  right, Vector128<float>  addend) => MultiplyAddEstimate(left, right, addend);
        /// <inheritdoc cref="MultiplyAddEstimate(Vector128{float}, Vector128{float}, Vector128{float})" />
        public static Vector128<double> MultiplyAddEstimate(Vector128<double> left, Vector128<double> right, Vector128<double> addend) => MultiplyAddEstimate(left, right, addend);

        /// <summary>Computes an implementation-dependent estimate of the negated product of two vectors added to a third vector.</summary>
        /// <param name="left">The first vector to multiply.</param>
        /// <param name="right">The second vector to multiply.</param>
        /// <param name="addend">The vector to add to the negated product.</param>
        /// <returns>The estimated negated multiply-add result.</returns>
        /// <remarks>
        /// This method maps to the corresponding <c>relaxed_nmadd</c> instruction. Whether the
        /// product is rounded before the addition, and whether the operation is fused, is
        /// implementation-defined.
        /// </remarks>
        public static Vector128<float>  MultiplyAddNegatedEstimate(Vector128<float>  left, Vector128<float>  right, Vector128<float>  addend) => MultiplyAddNegatedEstimate(left, right, addend);
        /// <inheritdoc cref="MultiplyAddNegatedEstimate(Vector128{float}, Vector128{float}, Vector128{float})" />
        public static Vector128<double> MultiplyAddNegatedEstimate(Vector128<double> left, Vector128<double> right, Vector128<double> addend) => MultiplyAddNegatedEstimate(left, right, addend);

        /// <summary>Selects elements from two vectors using the specified mask.</summary>
        /// <param name="left">The vector selected when a mask element is all bits set.</param>
        /// <param name="right">The vector selected when a mask element is zero.</param>
        /// <param name="mask">The mask used to select elements.</param>
        /// <returns>A vector containing the selected elements.</returns>
        /// <remarks>
        /// This method maps to the corresponding <c>relaxed_laneselect</c> instruction. The result
        /// is implementation-defined for mask elements that are neither all bits set nor zero. Use
        /// <see cref="Vector128.ConditionalSelect{T}(Vector128{T}, Vector128{T}, Vector128{T})" />
        /// when deterministic bitwise selection is required.
        /// </remarks>
        public static Vector128<sbyte>  LaneSelectNative(Vector128<sbyte>  left, Vector128<sbyte>  right, Vector128<sbyte>  mask) => LaneSelectNative(left, right, mask);
        /// <inheritdoc cref="LaneSelectNative(Vector128{sbyte}, Vector128{sbyte}, Vector128{sbyte})" />
        public static Vector128<byte>   LaneSelectNative(Vector128<byte>   left, Vector128<byte>   right, Vector128<byte>   mask) => LaneSelectNative(left, right, mask);
        /// <inheritdoc cref="LaneSelectNative(Vector128{sbyte}, Vector128{sbyte}, Vector128{sbyte})" />
        public static Vector128<short>  LaneSelectNative(Vector128<short>  left, Vector128<short>  right, Vector128<short>  mask) => LaneSelectNative(left, right, mask);
        /// <inheritdoc cref="LaneSelectNative(Vector128{sbyte}, Vector128{sbyte}, Vector128{sbyte})" />
        public static Vector128<ushort> LaneSelectNative(Vector128<ushort> left, Vector128<ushort> right, Vector128<ushort> mask) => LaneSelectNative(left, right, mask);
        /// <inheritdoc cref="LaneSelectNative(Vector128{sbyte}, Vector128{sbyte}, Vector128{sbyte})" />
        public static Vector128<int>    LaneSelectNative(Vector128<int>    left, Vector128<int>    right, Vector128<int>    mask) => LaneSelectNative(left, right, mask);
        /// <inheritdoc cref="LaneSelectNative(Vector128{sbyte}, Vector128{sbyte}, Vector128{sbyte})" />
        public static Vector128<uint>   LaneSelectNative(Vector128<uint>   left, Vector128<uint>   right, Vector128<uint>   mask) => LaneSelectNative(left, right, mask);
        /// <inheritdoc cref="LaneSelectNative(Vector128{sbyte}, Vector128{sbyte}, Vector128{sbyte})" />
        public static Vector128<long>   LaneSelectNative(Vector128<long>   left, Vector128<long>   right, Vector128<long>   mask) => LaneSelectNative(left, right, mask);
        /// <inheritdoc cref="LaneSelectNative(Vector128{sbyte}, Vector128{sbyte}, Vector128{sbyte})" />
        public static Vector128<ulong>  LaneSelectNative(Vector128<ulong>  left, Vector128<ulong>  right, Vector128<ulong>  mask) => LaneSelectNative(left, right, mask);

        /// <summary>Computes the minimum of each pair of elements in two vectors.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <returns>A vector containing the minimum values.</returns>
        /// <remarks>
        /// This method maps to the corresponding <c>relaxed_min</c> instruction. NaN handling and
        /// the sign of a zero result are implementation-defined. Use <c>PackedSimd.Min</c> for
        /// IEEE-compliant behavior or <c>PackedSimd.PseudoMin</c> for pseudo-minimum behavior.
        /// </remarks>
        public static Vector128<float>  MinNative(Vector128<float>  left, Vector128<float>  right) => MinNative(left, right);
        /// <summary>Computes the maximum of each pair of elements in two vectors.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <returns>A vector containing the maximum values.</returns>
        /// <remarks>
        /// This method maps to the corresponding <c>relaxed_max</c> instruction. NaN handling and
        /// the sign of a zero result are implementation-defined. Use <c>PackedSimd.Max</c> for
        /// IEEE-compliant behavior or <c>PackedSimd.PseudoMax</c> for pseudo-maximum behavior.
        /// </remarks>
        public static Vector128<float>  MaxNative(Vector128<float>  left, Vector128<float>  right) => MaxNative(left, right);
        /// <inheritdoc cref="MinNative(Vector128{float}, Vector128{float})" />
        public static Vector128<double> MinNative(Vector128<double> left, Vector128<double> right) => MinNative(left, right);
        /// <inheritdoc cref="MaxNative(Vector128{float}, Vector128{float})" />
        public static Vector128<double> MaxNative(Vector128<double> left, Vector128<double> right) => MaxNative(left, right);

        /// <summary>Multiplies corresponding signed 16-bit fixed-point elements and returns their rounded high halves.</summary>
        /// <param name="left">The first vector to multiply.</param>
        /// <param name="right">The second vector to multiply.</param>
        /// <returns>The rounded fixed-point products.</returns>
        /// <remarks>
        /// This method maps to the <c>i16x8.relaxed_q15mulr_s</c> instruction. The result of
        /// multiplying <see cref="short.MinValue" /> by <see cref="short.MinValue" /> is
        /// implementation-defined. Use
        /// <see cref="PackedSimd.MultiplyRoundedSaturateQ15(Vector128{short}, Vector128{short})" />
        /// when deterministic saturation is required.
        /// </remarks>
        public static Vector128<short> MultiplyRoundedQ15Native(Vector128<short> left, Vector128<short> right) => MultiplyRoundedQ15Native(left, right);

        /// <summary>Multiplies adjacent signed and unsigned byte elements and sums each pair into a signed 16-bit element.</summary>
        /// <param name="left">The vector containing signed byte elements.</param>
        /// <param name="right">The vector containing unsigned 7-bit elements.</param>
        /// <returns>The pairwise dot products.</returns>
        /// <remarks>
        /// This method maps to the <c>i16x8.relaxed_dot_i8x16_i7x16_s</c> instruction. If an element
        /// in <paramref name="right" /> has its high bit set, whether it is interpreted as signed or
        /// unsigned is implementation-defined. Whether the pairwise sum saturates on overflow is
        /// also implementation-defined.
        /// </remarks>
        public static Vector128<short> DotProductNative(Vector128<sbyte> left, Vector128<byte> right) => DotProductNative(left, right);

        /// <summary>Multiplies groups of four signed and unsigned byte elements and adds each sum to a signed 32-bit accumulator.</summary>
        /// <param name="left">The vector containing signed byte elements.</param>
        /// <param name="right">The vector containing unsigned 7-bit elements.</param>
        /// <param name="accumulator">The vector added to the dot products.</param>
        /// <returns>The accumulated dot products.</returns>
        /// <remarks>
        /// This method maps to the <c>i32x4.relaxed_dot_i8x16_i7x16_add_s</c> instruction. If an
        /// element in <paramref name="right" /> has its high bit set, whether it is interpreted as
        /// signed or unsigned is implementation-defined. Whether the group sum saturates on
        /// overflow is also implementation-defined.
        /// </remarks>
        public static Vector128<int> DotProductAddNative(Vector128<sbyte> left, Vector128<byte> right, Vector128<int> accumulator) => DotProductAddNative(left, right, accumulator);
    }
}
