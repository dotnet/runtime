// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.Wasm
{
    [CLSCompliant(false)]
    public abstract class RelaxedSimd
    {
        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static bool IsSupported { [Intrinsic] get { return false; } }

        public static Vector128<sbyte> Swizzle(Vector128<sbyte> vector, Vector128<sbyte> indices) { throw new PlatformNotSupportedException(); }
        public static Vector128<byte>  Swizzle(Vector128<byte>  vector, Vector128<byte>  indices) { throw new PlatformNotSupportedException(); }

        public static Vector128<int>  ConvertToInt32(Vector128<float>  value) { throw new PlatformNotSupportedException(); }
        public static Vector128<uint> ConvertToUInt32(Vector128<float> value) { throw new PlatformNotSupportedException(); }
        public static Vector128<int>  ConvertToInt32(Vector128<double> value) { throw new PlatformNotSupportedException(); }
        public static Vector128<uint> ConvertToUInt32(Vector128<double> value) { throw new PlatformNotSupportedException(); }

        public static Vector128<float>  MultiplyAddEstimate(Vector128<float>  a, Vector128<float>  b, Vector128<float>  c) { throw new PlatformNotSupportedException(); }
        public static Vector128<double> MultiplyAddEstimate(Vector128<double> a, Vector128<double> b, Vector128<double> c) { throw new PlatformNotSupportedException(); }

        public static Vector128<float>  MultiplyAddNegatedEstimate(Vector128<float>  a, Vector128<float>  b, Vector128<float>  c) { throw new PlatformNotSupportedException(); }
        public static Vector128<double> MultiplyAddNegatedEstimate(Vector128<double> a, Vector128<double> b, Vector128<double> c) { throw new PlatformNotSupportedException(); }

        public static Vector128<sbyte>  LaneSelect(Vector128<sbyte>  left, Vector128<sbyte>  right, Vector128<sbyte>  mask) { throw new PlatformNotSupportedException(); }
        public static Vector128<byte>   LaneSelect(Vector128<byte>   left, Vector128<byte>   right, Vector128<byte>   mask) { throw new PlatformNotSupportedException(); }
        public static Vector128<short>  LaneSelect(Vector128<short>  left, Vector128<short>  right, Vector128<short>  mask) { throw new PlatformNotSupportedException(); }
        public static Vector128<ushort> LaneSelect(Vector128<ushort> left, Vector128<ushort> right, Vector128<ushort> mask) { throw new PlatformNotSupportedException(); }
        public static Vector128<int>    LaneSelect(Vector128<int>    left, Vector128<int>    right, Vector128<int>    mask) { throw new PlatformNotSupportedException(); }
        public static Vector128<uint>   LaneSelect(Vector128<uint>   left, Vector128<uint>   right, Vector128<uint>   mask) { throw new PlatformNotSupportedException(); }
        public static Vector128<long>   LaneSelect(Vector128<long>   left, Vector128<long>   right, Vector128<long>   mask) { throw new PlatformNotSupportedException(); }
        public static Vector128<ulong>  LaneSelect(Vector128<ulong>  left, Vector128<ulong>  right, Vector128<ulong>  mask) { throw new PlatformNotSupportedException(); }

        public static Vector128<float>  Min(Vector128<float>  left, Vector128<float>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<float>  Max(Vector128<float>  left, Vector128<float>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<double> Min(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<double> Max(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        public static Vector128<short> MultiplyRoundedQ15(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }

        public static Vector128<short> DotProduct(Vector128<sbyte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<int>   DotProductAdd(Vector128<sbyte> left, Vector128<byte> right, Vector128<int> accumulator) { throw new PlatformNotSupportedException(); }
    }
}
