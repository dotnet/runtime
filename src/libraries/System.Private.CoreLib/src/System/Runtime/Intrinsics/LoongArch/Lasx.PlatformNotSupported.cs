// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.LoongArch
{
    /// <summary>
    /// This class provides access to the LASX-256bits hardware instructions via intrinsics
    /// </summary>
    [CLSCompliant(false)]
#if SYSTEM_PRIVATE_CORELIB
    public
#else
    internal
#endif
    abstract class Lasx : Lsx
    {
        internal Lasx() { }

        public static new bool IsSupported { [Intrinsic] get { return false; } }

        /// <summary>
        /// int8x32_t xvadd_b(int8x32_t a, int8x32_t b)
        ///   LASX: XVADD.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> Add(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvadd_b(uint8x32_t a, uint8x32_t b)
        ///   LASX: XVADD.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> Add(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvadd_h(int16x16_t a, int16x16_t b)
        ///   LASX: XVADD.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> Add(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvadd_h(uint16x16_t a, uint16x16_t b)
        ///   LASX: XVADD.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> Add(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvadd_w(int32x8_t a, int32x8_t b)
        ///   LASX: XVADD.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> Add(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvadd_w(uint32x8_t a, uint32x8_t b)
        ///   LASX: XVADD.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> Add(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvadd_d(int64x4_t a, int64x4_t b)
        ///   LASX: XVADD.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> Add(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvadd_d(uint64x4_t a, uint64x4_t b)
        ///   LASX: XVADD.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> Add(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x8_t xvfadd_s(float32x8_t a, float32x8_t b)
        ///   LASX: XVFADD.S Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<float> Add(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x4_t xvfadd_d(float64x4_t a, float64x4_t b)
        ///   LASX: XVFADD.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<double> Add(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvsadd_b(int8x32_t a, int8x32_t b)
        ///   LASX: XVSADD.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> AddSaturate(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvsadd_h(int16x16_t a, int16x16_t b)
        ///   LASX: XVSADD.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> AddSaturate(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvsadd_w(int32x8_t a, int32x8_t b)
        ///   LASX: XVSADD.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> AddSaturate(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvsadd_d(int64x4_t a, int64x4_t b)
        ///   LASX: XVSADD.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> AddSaturate(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvsadd_bu(uint8x32_t a, uint8x32_t b)
        ///   LASX: XVSADD.BU Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> AddSaturate(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvsadd_hu(uint16x16_t a, uint16x16_t b)
        ///   LASX: XVSADD.HU Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> AddSaturate(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvsadd_wu(uint32x8_t a, uint32x8_t b)
        ///   LASX: XVSADD.WU Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> AddSaturate(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvsadd_du(uint64x4_t a, uint64x4_t b)
        ///   LASX: XVSADD.DU Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> AddSaturate(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvhaddw_h_b(int3x32_t a, int3x32_t b)
        ///   LASX: XVHADDW.H.B Xd.16H, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> AddOddEvenElementsWidening(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvhaddw_hu_bu(uint3x32_t a, uint3x32_t b)
        ///   LASX: XVHADDW.HU.BU Xd.16H, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> AddOddEvenElementsWidening(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvhaddw_w_h(int16x16_t a, int16x16_t b)
        ///   LASX: XVHADDW.W.H Xd.8W, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> AddOddEvenElementsWidening(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvhaddw_wu_hu(uint16x16_t a, uint16x16_t b)
        ///   LASX: XVHADDW.WU.HU Xd.8W, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> AddOddEvenElementsWidening(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvhaddw_d_w(int32x8_t a, int32x8_t b)
        ///   LASX: XVHADDW.D.W Xd.4D, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> AddOddEvenElementsWidening(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvhaddw_du_wu(uint32x8_t a, uint32x8_t b)
        ///   LASX: XVHADDW.DU.WU Xd.4D, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> AddOddEvenElementsWidening(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int128x2_t xvhaddw_q_d(int64x4_t a, int64x4_t b)  TODO: long --> longlong 128bits.
        ///   LASX: XVHADDW.Q.D Xd.2Q, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> AddOddEvenElementsWidening(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint128x2_t xvhaddw_qu_du(uint64x4_t a, uint64x4_t b)
        ///   LASX: XVHADDW.QU.DU Xd.2Q, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> AddOddEvenElementsWidening(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvaddwev_h_b(int8x32_t a, int8x32_t b)
        ///   LASX: XVADDWEV.H.B Xd.16H, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<short> AddEvenElementsWidening(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvaddwev_h_bu(uint8x32_t a, uint8x32_t b)
        ///   LASX: XVADDWEV.H.BU Xd.16H, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<ushort> AddEvenElementsWidening(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvaddwev_w_h(int16x16_t a, int16x16_t b)
        ///   LASX: XVADDWEV.W.H Xd.8W, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<int> AddEvenElementsWidening(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvaddwev_w_hu(uint16x16_t a, uint16x16_t b)
        ///   LASX: XVADDWEV.W.HU Xd.8W, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<uint> AddEvenElementsWidening(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvaddwev_d_w(int32x8_t a, int32x8_t b)
        ///   LASX: XVADDWEV.D.W Xd.4D, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<long> AddEvenElementsWidening(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvaddwev_d_wu(uint32x8_t a, uint32x8_t b)
        ///   LASX: XVADDWEV.D.WU Xd.4D, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<ulong> AddEvenElementsWidening(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int128x2_t xvaddwev_q_d(int64x4_t a, int64x4_t b)  TODO: long --> longlong 128bits.
        ///   LASX: XVADDWEV.Q.D Xd.2Q, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> AddEvenElementsWidening(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint128x2_t xvaddwev_q_du(uint64x4_t a, uint64x4_t b)
        ///   LASX: XVADDWEV.Q.DU Xd.2Q, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> AddEvenElementsWidening(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvaddwod_h_b(int8x32_t a, int8x32_t b)
        ///   LASX: XVADDWOD.H.B Xd.16H, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<short> AddOddElementsWidening(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvaddwod_h_bu(uint8x32_t a, uint8x32_t b)
        ///   LASX: XVADDWOD.H.BU Xd.16H, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<ushort> AddOddElementsWidening(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvaddwod_w_h(int16x16_t a, int16x16_t b)
        ///   LASX: XVADDWOD.W.H Xd.8W, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<int> AddOddElementsWidening(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvaddwod_w_hu(uint16x16_t a, uint16x16_t b)
        ///   LASX: XVADDWOD.W.HU Xd.8W, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<uint> AddOddElementsWidening(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvaddwod_d_w(int32x8_t a, int32x8_t b)
        ///   LASX: XVADDWOD.D.W Xd.4D, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<long> AddOddElementsWidening(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvaddwod_d_wu(uint32x8_t a, uint32x8_t b)
        ///   LASX: XVADDWOD.D.WU Xd.4D, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<ulong> AddOddElementsWidening(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int128x2_t xvaddwod_q_d(int64x4_t a, int64x4_t b)  TODO: long --> longlong 128bits.
        ///   LASX: XVADDWOD.Q.D Xd.2Q, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> AddOddElementsWidening(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint128x2_t xvaddwod_q_du(uint64x4_t a, uint64x4_t b)
        ///   LASX: XVADDWOD.Q.DU Xd.2Q, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> AddOddElementsWidening(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvaddwev_h_bu_b(int8x32_t a, int8x32_t b)
        ///   LASX: XVADDWEV.H.BU.B Xd.16H, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<short> AddEvenElementsWidening(Vector256<byte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvaddwev_w_hu_h(int16x16_t a, int16x16_t b)
        ///   LASX: XVADDWEV.W.HU.H Xd.8W, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<int> AddEvenElementsWidening(Vector256<ushort> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvaddwev_d_wu_w(int32x8_t a, int32x8_t b)
        ///   LASX: XVADDWEV.D.WU.W Xd.4D, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<long> AddEvenElementsWidening(Vector256<uint> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int128x2_t xvaddwev_q_du_d(int64x4_t a, int64x4_t b)  TODO: long --> longlong 128bits.
        ///   LASX: XVADDWEV.Q.DU.D Xd.2Q, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> AddEvenElementsWidening(Vector256<ulong> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvaddwod_h_bu_b(int8x32_t a, int8x32_t b)
        ///   LASX: XVADDWOD.H.BU.B Xd.16H, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<short> AddOddElementsWidening(Vector256<byte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvaddwod_w_hu_h(int16x16_t a, int16x16_t b)
        ///   LASX: XVADDWOD.W.HU.H Xd.8W, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<int> AddOddElementsWidening(Vector256<ushort> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvaddwod_d_wu_w(int32x8_t a, int32x8_t b)
        ///   LASX: XVADDWOD.D.WU.W Xd.4D, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<long> AddOddElementsWidening(Vector256<uint> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int128x2_t xvaddwod_q_du_d(int64x4_t a, int64x4_t b)  TODO: long --> longlong 128bits.
        ///   LASX: XVADDWOD.Q.DU.D Xd.2Q, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> AddOddElementsWidening(Vector256<ulong> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   NOTE: this is implemented by multi instructions.
        ///   LASX: XVHADDW.H.B Xd.16H, Xj.32B, Xk.32B
        /// </summary>
        public static Vector128<long> AddHorizontalElements(Vector256<sbyte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   NOTE: this is implemented by multi instructions.
        ///   LASX: XVHADDW.HU.BU Xd.16H, Xj.32B, Xk.32B
        /// </summary>
        public static Vector128<ulong> AddHorizontalElements(Vector256<byte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   NOTE: this is implemented by multi instructions.
        ///   LASX: XVHADDW.W.H Xd.8W, Xj.16H, Xk.16H
        /// </summary>
        public static Vector128<long> AddHorizontalElements(Vector256<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   NOTE: this is implemented by multi instructions.
        ///   LASX: XVHADDW.WU.HU Xd.8W, Xj.16H, Xk.16H
        /// </summary>
        public static Vector128<ulong> AddHorizontalElements(Vector256<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   NOTE: this is implemented by multi instructions.
        ///   LASX: XVHADDW.D.W Xd.4D, Xj.8W, Xk.8W
        /// </summary>
        public static Vector128<long> AddHorizontalElements(Vector256<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   NOTE: this is implemented by multi instructions.
        ///   LASX: XVHADDW.DU.WU Xd.4D, Xj.8W, Xk.8W
        /// </summary>
        public static Vector128<long> AddHorizontalElements(Vector256<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   NOTE: this is implemented by multi instructions.
        ///   LASX: XVHADDW.Q.D Xd.2Q, Xj.4D, Xk.4D
        /// </summary>
        public static Vector128<long> AddHorizontalElements(Vector256<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   NOTE: this is implemented by multi instructions.
        ///   LASX: XVHADDW.QU.DU Xd.2Q, Xj.4D, Xk.4D
        /// </summary>
        public static Vector128<ulong> AddHorizontalElements(Vector256<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   NOTE: this is implemented by multi instructions.
        ///   LASX: sum_all_float_elements witin vector.
        /// </summary>
        public static Vector64<float> AddHorizontalElements(Vector256<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   NOTE: this is implemented by multi instructions.
        ///   LASX: sum_all_double_elements witin vector.
        /// </summary>
        public static Vector64<double> AddHorizontalElements(Vector256<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvsub_b(int8x32_t a, int8x32_t b)
        ///   LASX: XVSUB.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> Subtract(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvsub_b(uint8x32_t a, uint8x32_t b)
        ///   LASX: XVSUB.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> Subtract(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvsub_h(int16x16_t a, int16x16_t b)
        ///   LASX: XVSUB.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> Subtract(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvsub_h(uint16x16_t a, uint16x16_t b)
        ///   LASX: XVSUB.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> Subtract(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvsub_w(int32x8_t a, int32x8_t b)
        ///   LASX: XVSUB.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> Subtract(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvsub_w(uint32x8_t a, uint32x8_t b)
        ///   LASX: XVSUB.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> Subtract(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvsub_d(int64x4_t a, int64x4_t b)
        ///   LASX: XVSUB.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> Subtract(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvsub_d(uint64x4_t a, uint64x4_t b)
        ///   LASX: XVSUB.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> Subtract(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x8_t xvfsub_s(float32x8_t a, float32x8_t b)
        ///   LASX: XVFSUB.S Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<float> Subtract(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x4_t xvfsub_d(float64x4_t a, float64x4_t b)
        ///   LASX: XVFSUB.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<double> Subtract(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvhsubw_h_b(int3x32_t a, int3x32_t b)
        ///   LASX: XVHSUBW.H.B Xd.16H, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> SubtractOddEvenElementsWidening(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvhsubw_hu_bu(uint3x32_t a, uint3x32_t b)
        ///   LASX: XVHSUBW.HU.BU Xd.16H, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> SubtractOddEvenElementsWidening(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvhsubw_w_h(int16x16_t a, int16x16_t b)
        ///   LASX: XVHSUBW.W.H Xd.8W, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> SubtractOddEvenElementsWidening(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvhsubw_wu_hu(uint16x16_t a, uint16x16_t b)
        ///   LASX: XVHSUBW.WU.HU Xd.8W, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> SubtractOddEvenElementsWidening(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvhsubw_d_w(int32x8_t a, int32x8_t b)
        ///   LASX: XVHSUBW.D.W Xd.4D, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> SubtractOddEvenElementsWidening(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvhsubw_du_wu(uint32x8_t a, uint32x8_t b)
        ///   LASX: XVHSUBW.DU.WU Xd.4D, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> SubtractOddEvenElementsWidening(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int128x2_t xvhsubw_q_d(int64x4_t a, int64x4_t b)  TODO: long --> longlong 128bits.
        ///   LASX: XVHSUBW.Q.D Xd.2Q, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> SubtractOddEvenElementsWidening(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint128x2_t xvhsubw_qu_du(uint64x4_t a, uint64x4_t b)
        ///   LASX: XVHSUBW.QU.DU Xd.2Q, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> SubtractOddEvenElementsWidening(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvssub_b(int8x32_t a, int8x32_t b)
        ///   LASX: XVSSUB.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> SubtractSaturate(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvssub_bu(uint8x32_t a, uint8x32_t b)
        ///   LASX: XVSSUB.BU Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> SubtractSaturate(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvssub_h(int16x16_t a, int16x16_t b)
        ///   LASX: XVSSUB.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> SubtractSaturate(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvssub_hu(uint16x16_t a, uint16x16_t b)
        ///   LASX: XVSSUB.HU Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> SubtractSaturate(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvssub_w(int32x8_t a, int32x8_t b)
        ///   LASX: XVSSUB.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> SubtractSaturate(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvssub_wu(uint32x8_t a, uint32x8_t b)
        ///   LASX: XVSSUB.WU Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> SubtractSaturate(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvssub_d(int64x4_t a, int64x4_t b)
        ///   LASX: XVSSUB.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> SubtractSaturate(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvssub_du(uint64x4_t a, uint64x4_t b)
        ///   LASX: XVSSUB.DU Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> SubtractSaturate(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvmul_b(int8x32_t a, int8x32_t b)
        ///   LASX: XVMUL.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> Multiply(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvmul_b(uint8x32_t a, uint8x32_t b)
        ///   LASX: XVMUL.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> Multiply(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvmul_h(int16x16_t a, int16x16_t b)
        ///   LASX: XVMUL.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> Multiply(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvmul_h(uint16x16_t a, uint16x16_t b)
        ///   LASX: XVMUL.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> Multiply(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvmul_w(int32x8_t a, int32x8_t b)
        ///   LASX: XVMULW Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> Multiply(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvmul_w(uint32x8_t a, uint32x8_t b)
        ///   LASX: XVMUL.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> Multiply(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvmul_d(int64x4_t a, int64x4_t b)
        ///   LASX: XVMUL.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> Multiply(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvmul_d(uint64x4_t a, uint64x4_t b)
        ///   LASX: XVMUL.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> Multiply(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x8_t xvfmul_s(float32x8_t a, float32x8_t b)
        ///   LASX: XVFMUL.S Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<float> Multiply(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x4_t xvfmul_d(float64x4_t a, float64x4_t b)
        ///   LASX: XVFMUL.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<double> Multiply(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvmuh_b(int8x32_t a, int8x32_t b)
        ///   LASX: XVMUH.B Vd.32B, Vj.32B, Vk.32B
        /// </summary>
        public static Vector256<sbyte> MultiplyHight(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvmuh_bu(uint8x32_t a, uint8x32_t b)
        ///   LASX: XVMUH.BU Vd.32B, Vj.32B, Vk.32B
        /// </summary>
        public static Vector256<byte> MultiplyHight(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvmuh_h(int16x16_t a, int16x16_t b)
        ///   LASX: XVMUH.H Vd.16H, Vj.16H, Vk.16H
        /// </summary>
        public static Vector256<short> MultiplyHight(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvmuh_hu(uint16x16_t a, uint16x16_t b)
        ///   LASX: XVMUH.HU Vd.16H, Vj.16H, Vk.16H
        /// </summary>
        public static Vector256<ushort> MultiplyHight(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvmuh_w(int32x8_t a, int32x8_t b)
        ///   LASX: VMUL.W Vd.8W, Vj.8W, Vk.8W
        /// </summary>
        public static Vector256<int> MultiplyHight(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvmuh_wu(uint32x8_t a, uint32x8_t b)
        ///   LASX: XVMUH.WU Vd.8W, Vj.8W, Vk.8W
        /// </summary>
        public static Vector256<uint> MultiplyHight(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvmuh_d(int64x4_t a, int64x4_t b)
        ///   LASX: XVMUH.D Vd.4D, Vj.4D, Vk.4D
        /// </summary>
        public static Vector256<long> MultiplyHight(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvmuh_du(uint64x4_t a, uint64x4_t b)
        ///   LASX: XVMUH.DU Vd.4D, Vj.4D, Vk.4D
        /// </summary>
        public static Vector256<ulong> MultiplyHight(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvdiv_b(int8x32_t a, int8x32_t b)
        ///   LASX: XVDIV.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> Divide(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvdiv_bu(uint8x32_t a, uint8x32_t b)
        ///   LASX: XVDIV.BU Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> Divide(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvdiv_h(int16x16_t a, int16x16_t b)
        ///   LASX: XVDIV.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> Divide(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvdiv_hu(uint16x16_t a, uint16x16_t b)
        ///   LASX: XVDIV.HU Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> Divide(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvdiv_w(int32x8_t a, int32x8_t b)
        ///   LASX: XVDIV.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> Divide(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvdiv_wu(uint32x8_t a, uint32x8_t b)
        ///   LASX: XVDIV.WU Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> Divide(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvdiv_d(int64x4_t a, int64x4_t b)
        ///   LASX: XVDIV.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> Divide(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvdiv_du(uint64x4_t a, uint64x4_t b)
        ///   LASX: XVDIV.DU Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> Divide(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x8_t xvfdiv_s(float32x8_t a, float32x8_t b)
        ///   LASX: XVFDIV.S Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<float> Divide(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x4_t xvfdiv_d(float64x4_t a, float64x4_t b)
        ///   LASX: XVFDIV.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<double> Divide(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvmod_b(int8x32_t a, int8x32_t b)
        ///   LASX: XVMOD.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> Modulo(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvmod_bu(uint8x32_t a, uint8x32_t b)
        ///   LASX: XVMOD.BU Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> Modulo(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvmod_h(int16x16_t a, int16x16_t b)
        ///   LASX: XVMOD.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> Modulo(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvmod_hu(uint16x16_t a, uint16x16_t b)
        ///   LASX: XVMOD.HU Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> Modulo(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvmod_w(int32x8_t a, int32x8_t b)
        ///   LASX: XVMOD.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> Modulo(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvmod_wu(uint32x8_t a, uint32x8_t b)
        ///   LASX: XVMOD.WU Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> Modulo(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvmod_d(int64x4_t a, int64x4_t b)
        ///   LASX: XVMOD.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> Modulo(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvmod_du(uint64x4_t a, uint64x4_t b)
        ///   LASX: XVMOD.DU Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> Modulo(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x8_t xvfmadd_s(float32x8_t a, float32x8_t b, float32x8_t c)
        ///   LASX: XVFMADD.S Xd.8S, Xj.8S, Xk.8S, Xa.8S
        /// </summary>
        public static Vector256<float> FusedMultiplyAdd(Vector256<float> left, Vector256<float> right, Vector256<float> addend) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x4_t xvfmadd_d(float64x4_t a, float64x4_t b, float64x4_t c)
        ///   LASX: XVFMADD.D Xd.4D, Xj.4D, Xk.4D, Xa.4D
        /// </summary>
        public static Vector256<double> FusedMultiplyAdd(Vector256<double> left, Vector256<double> right, Vector256<double> addend) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x8_t xvfnmadd_s(float32x8_t a, float32x8_t b, float32x8_t c)
        ///   LASX: XVFNMADD.S Xd.8S, Xj.8S, Xk.8S, Xa.8S
        /// </summary>
        public static Vector256<float> FusedMultiplyAddNegated(Vector256<float> left, Vector256<float> right, Vector256<float> addend) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x4_t xvfnmadd_d(float64x4_t a, float64x4_t b, float64x4_t c)
        ///   LASX: XVFNMADD.D Xd.4D, Xj.4D, Xk.4D, Xa.4D
        /// </summary>
        public static Vector256<double> FusedMultiplyAddNegated(Vector256<double> left, Vector256<double> right, Vector256<double> addend) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvmadd_b(int8x32_t a, int8x32_t b, int8x32_t c)
        ///   LASX: XVMADD.B Xd.32B, Xj.32B, Xk.32B               //NOTE: The Vd is both input and output while input as addend.
        /// </summary>
        public static Vector256<sbyte> MultiplyAdd(Vector256<sbyte> addend, Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvmadd_b(uint8x32_t a, uint8x32_t b, uint8x32_t c)
        ///   LASX: XVMADD.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> MultiplyAdd(Vector256<byte> addend, Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvmadd_h(int16x16_t a, int16x16_t b, int16x16_t c)
        ///   LASX: XVMADD.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> MultiplyAdd(Vector256<short> addend, Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvmadd_h(uint16x16_t a, uint16x16_t b, uint16x16_t c)
        ///   LASX: XVMADD.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> MultiplyAdd(Vector256<ushort> addend, Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvmadd_w(int32x8_t a, int32x8_t b, int32x8_t c)
        ///   LASX: XVMADD.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> MultiplyAdd(Vector256<int> addend, Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvmadd_w(uint32x8_t a, uint32x8_t b, uint32x8_t c)
        ///   LASX: XVMADD.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> MultiplyAdd(Vector256<uint> addend, Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvmadd_d(int64x4_t a, int64x4_t b, int64x4_t c)
        ///   LASX: XVMADD.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> MultiplyAdd(Vector256<long> addend, Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvmadd_d(uint64x4_t a, uint64x4_t b, uint64x4_t c)
        ///   LASX: XVMADD.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> MultiplyAdd(Vector256<ulong> addend, Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x8_t xvfmsub_s(float32x8_t a, float32x8_t b, float32x8_t c)
        ///   LASX: XVFMSUB.S Xd.8S, Xj.8S, Xk.8S, Xa.8S
        /// </summary>
        public static Vector256<float> FusedMultiplySubtract(Vector256<float> left, Vector256<float> right, Vector256<float> minuend) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x4_t xvfmsub_d(float64x4_t a, float64x4_t b, float64x4_t c)
        ///   LASX: XVFMSUB.D Xd.4D, Xj.4D, Xk.4D, Xa.4D
        /// </summary>
        public static Vector256<double> FusedMultiplySubtract(Vector256<double> left, Vector256<double> right, Vector256<double> minuend) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x8_t xvfnmsub_s(float32x8_t a, float32x8_t b, float32x8_t c)
        ///   LASX: XVFNMSUB.S Xd.8S, Xj.8S, Xk.8S, Xa.8S
        /// </summary>
        public static Vector256<float> FusedMultiplySubtractNegated(Vector256<float> left, Vector256<float> right, Vector256<float> minuend) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x4_t xvfnmsub_d(float64x4_t a, float64x4_t b, float64x4_t c)
        ///   LASX: XVFNMSUB.D Xd.4D, Xj.4D, Xk.4D, Xa.4D
        /// </summary>
        public static Vector256<double> FusedMultiplySubtractNegated(Vector256<double> left, Vector256<double> right, Vector256<double> minuend) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvmsub_b(int8x32_t a, int8x32_t b, int8x32_t c)
        ///   LASX: XVMSUB.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> MultiplySubtract(Vector256<sbyte> minuend, Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvmsub_b(uint8x32_t a, uint8x32_t b, uint8x32_t c)
        ///   LASX: XVMSUB.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> MultiplySubtract(Vector256<byte> minuend, Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvmsub_h(int16x16_t a, int16x16_t b, int16x16_t c)
        ///   LASX: XVMSUB.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> MultiplySubtract(Vector256<short> minuend, Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvmsub_h(uint16x16_t a, uint16x16_t b, uint16x16_t c)
        ///   LASX: XVMSUB.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> MultiplySubtract(Vector256<ushort> minuend, Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvmsub_w(int32x8_t a, int32x8_t b, int32x8_t c)
        ///   LASX: XVMSUB.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> MultiplySubtract(Vector256<int> minuend, Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvmsub_w(uint32x8_t a, uint32x8_t b, uint32x8_t c)
        ///   LASX: XVMSUB.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> MultiplySubtract(Vector256<uint> minuend, Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvmsub_d(int64x4_t a, int64x4_t b, int64x4_t c)
        ///   LASX: XVMSUB.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> MultiplySubtract(Vector256<long> addend, Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvmsub_d(uint64x4_t a, uint64x4_t b, uint64x4_t c)
        ///   LASX: XVMSUB.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> MultiplySubtract(Vector256<ulong> addend, Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvmaddwev_h_b(int16x16_t a, int8x8_t b, int8x8_t c)
        ///   LASX: XVMADDWEV.H.B Xd.16H, Xj.8B, Xk.8B
        /// </summary>
        public static Vector256<short> MultiplyWideningLowerAndAdd(Vector256<short> addend, Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvmaddwev_h_bu(uint16x16_t a, uint8x8_t b, uint8x8_t c)
        ///   LASX: XVMADDWEV.H.BU Xd.16H, Xj.8B, Xk.8B
        /// </summary>
        public static Vector256<ushort> MultiplyWideningLowerAndAdd(Vector256<ushort> addend, Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvmaddwev_w_h(int32x8_t a, int16x4_t b, int16x4_t c)
        ///   LASX: XVMADDWEV.W.H Xd.8W, Xj.4H, Xk.4H
        /// </summary>
        public static Vector256<int> MultiplyWideningLowerAndAdd(Vector256<int> addend, Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvmaddwev_w_hu(uint32x8_t a, uint16x4_t b, uint16x4_t c)
        ///   LASX: XVMADDWEV.W.HU Xd.8W, Xj.4H, Xk.4H
        /// </summary>
        public static Vector256<uint> MultiplyWideningLowerAndAdd(Vector256<uint> addend, Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvmaddwev_d_w(int64x4_t a, int32x2_t b, int32x2_t c)
        ///   LASX: XVMADDWEV.D.W Xd.4D, Xj.2S, Xk.2S
        /// </summary>
        public static Vector256<long> MultiplyWideningLowerAndAdd(Vector256<long> addend, Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvmaddwev_d_wu(uint64x4_t a, uint32x2_t b, uint32x2_t c)
        ///   LASX: XVMADDWEV.D.WU Xd.4D, Xj.2S, Xk.2S
        /// </summary>
        public static Vector256<ulong> MultiplyWideningLowerAndAdd(Vector256<ulong> addend, Vector128<uint> left, Vector128<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvmaddwod_h_b(int16x16_t a, int8x32_t b, int8x32_t c)
        ///   LASX: XVMADDWOD.H.B Xd.16H, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<short> MultiplyWideningUpperAndAdd(Vector256<short> addend, Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvmaddwod_h_bu(uint16x16_t a, uint8x32_t b, uint8x32_t c)
        ///   LASX: XVMADDWOD.H.BU Xd.16H, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<ushort> MultiplyWideningUpperAndAdd(Vector256<ushort> addend, Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvmaddwod_w_h(int32x8_t a, int16x16_t b, int16x16_t c)
        ///   LASX: XVMADDWOD.W.H Xd.8W, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<int> MultiplyWideningUpperAndAdd(Vector256<int> addend, Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvmaddwod_w_hu(uint32x8_t a, uint16x16_t b, uint16x16_t c)
        ///   LASX: XVMADDWOD.W.HU Xd.8W, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<uint> MultiplyWideningUpperAndAdd(Vector256<uint> addend, Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvmaddwod_d_w(int64x4_t a, int32x8_t b, int32x8_t c)
        ///   LASX: XVMADDWOD.D.W Xd.4D, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<long> MultiplyWideningUpperAndAdd(Vector256<long> addend, Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvmaddwod_d_wu(uint64x4_t a, uint32x8_t b, uint32x8_t c)
        ///   LASX: XVMADDWOD.D.WU Xd.4D, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<ulong> MultiplyWideningUpperAndAdd(Vector256<ulong> addend, Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int8x32_t xvmsknz_b(int8x32_t value)
        ///   LASX: XVMSKNZ.B Vd.32B, Vj.32B
        /// </summary>
        public static Vector256<sbyte> CompareNotEqualZeroEach128(Vector256<sbyte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  uint8x32_t xvmsknz_b(uint8x32_t value)
        ///   LASX: XVMSKNZ.B Vd.32B, Vj.32B
        /// </summary>
        public static Vector256<byte> CompareNotEqualZeroEach128(Vector256<byte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvseqi_b(int8x32_t a, int8_t si5)
        ///   LASX: XVSEQI.B Xd.32B, Xj.32B, si5
        /// </summary>
        public static Vector256<sbyte> CompareEqual(Vector256<sbyte> value, [ConstantExpected(Min = -16, Max = (byte)(15))] sbyte si5) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvseqi_h(int16x16_t a, int8_t si5)
        ///   LASX: XVSEQI.H Xd.16H, Xj.16H, si5
        /// </summary>
        public static Vector256<short> CompareEqual(Vector256<short> value, [ConstantExpected(Min = -16, Max = (byte)(15))] sbyte si5) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvseqi_w(int32x8_t a, int8_t si5)
        ///   LASX: XVSEQI.W Xd.8W, Xj.8W, si5
        /// </summary>
        public static Vector256<int> CompareEqual(Vector256<int> value, [ConstantExpected(Min = -16, Max = (byte)(15))] sbyte si5) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvseqi_d(int64x4_t a, int8_t si5)
        ///   LASX: XVSEQI.D Xd.4D, Xj.4D, si5
        /// </summary>
        public static Vector256<long> CompareEqual(Vector256<long> value, [ConstantExpected(Min = -16, Max = (byte)(15))] sbyte si5) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvseq_b(int8x32_t a, int8x32_t b)
        ///   LASX: XVSEQ.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> CompareEqual(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvseq_b(uint8x32_t a, uint8x32_t b)
        ///   LASX: XVSEQ.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> CompareEqual(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvseq_h(int16x16_t a, int16x16_t b)
        ///   LASX: XVSEQ.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> CompareEqual(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvseq_h(uint16x16_t a, uint16x16_t b)
        ///   LASX: XVSEQ.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> CompareEqual(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvseq_w(int32x8_t a, int32x8_t b)
        ///   LASX: XVSEQ.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> CompareEqual(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvseq_w(uint32x8_t a, uint32x8_t b)
        ///   LASX: XVSEQ.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> CompareEqual(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvseq_d(int64x4_t a, int64x4_t b)
        ///   LASX: XVSEQ.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> CompareEqual(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvseq_d(uint64x4_t a, uint64x4_t b)
        ///   LASX: XVSEQ.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> CompareEqual(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvfcmp_ceq_s(float32x8_t a, float32x8_t b)
        ///   LASX: XVFCMP.CEQ.S Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<float> CompareEqual(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvfcmp_ceq_d(float64x4_t a, float64x4_t b)
        ///   LASX: XVFCMP.CEQ.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<double> CompareEqual(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int8x32_t xvmskltz_b(int8x32_t value)
        ///   LASX: XVMSKLTZ.B Xd.32B, Xj.32B
        /// </summary>
        public static Vector256<sbyte> CompareLessThanZeroEach128(Vector256<sbyte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int16x16_t xvmskltz_h(int16x16_t value)
        ///   LASX: XVMSKLTZ.H Xd.16H, Xj.16H
        /// </summary>
        public static Vector256<short> CompareLessThanZeroEach128(Vector256<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int32x8_t xvmskltz_w(int32x8_t value)
        ///   LASX: XVMSKLTZ.W Xd.8W, Xj.8W
        /// </summary>
        public static Vector256<int> CompareLessThanZeroEach128(Vector256<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int64x4_t xvmskltz_d(int64x4_t value)
        ///   LASX: XVMSKLTZ.D Xd.4D, Xj.4D
        /// </summary>
        public static Vector256<long> CompareLessThanZeroEach128(Vector256<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvslti_b(int8x32_t a, int8_t si5)
        ///   LASX: XVSLTI.Bd.32B, Xj.32B, si5
        /// </summary>
        public static Vector256<sbyte> CompareLessThan(Vector256<sbyte> value, [ConstantExpected(Min = -16, Max = (byte)(15))] sbyte si5) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvslti_h(int16x16_t a, int8_t si5)
        ///   LASX: XVSLTI.H Xd.16H, Xj.16H, si5
        /// </summary>
        public static Vector256<short> CompareLessThan(Vector256<short> value, [ConstantExpected(Min = -16, Max = (byte)(15))] sbyte si5) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvslti_w(int32x8_t a, int8_t si5)
        ///   LASX: XVSLTI.W Xd.8W, Xj.8W, si5
        /// </summary>
        public static Vector256<int> CompareLessThan(Vector256<int> value, [ConstantExpected(Min = -16, Max = (byte)(15))] sbyte si5) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvslti_d(int64x4_t a, int8_t si5)
        ///   LASX: XVSLTI.D Xd.4D, Xj.4D, si5
        /// </summary>
        public static Vector256<long> CompareLessThan(Vector256<long> value, [ConstantExpected(Min = -16, Max = (byte)(15))] sbyte si5) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvslt_b(int8x32_t a, int8x32_t b)
        ///   LASX: XVSLT.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> CompareLessThan(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvslt_bu(uint8x32_t a, uint8x32_t b)
        ///   LASX: XVSLT.BU Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> CompareLessThan(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvslt_h(int16x16_t a, int16x16_t b)
        ///   LASX: XVSLT.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> CompareLessThan(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvslt_hu(uint16x16_t a, uint16x16_t b)
        ///   LASX: XVSLT.HU Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> CompareLessThan(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvslt_w(int32x8_t a, int32x8_t b)
        ///   LASX: XVSLT.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> CompareLessThan(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvslt_wu(uint32x8_t a, uint32x8_t b)
        ///   LASX: XVSLT.WU Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> CompareLessThan(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvslt_d(int64x4_t a, int64x4_t b)
        ///   LASX: XVSLT.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> CompareLessThan(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvslt_du(uint64x4_t a, uint64x4_t b)
        ///   LASX: XVSLT.DU Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> CompareLessThan(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvfcmp_clt_s(float32x8_t a, float32x8_t b)
        ///   LASX: XVFCMP.CLT.S Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<float> CompareLessThan(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvfcmp_clt_d(float64x4_t a, float64x4_t b)
        ///   LASX: XVFCMP.CLT.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<double> CompareLessThan(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvslei_b(int8x32_t a, int8_t si5)
        ///   LASX: XVSLEI.Bd.32B, Xj.32B, si5
        /// </summary>
        public static Vector256<sbyte> CompareLessThanOrEqual(Vector256<sbyte> value, [ConstantExpected(Min = -16, Max = (byte)(15))] sbyte si5) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvslei_h(int16x16_t a, int8_t si5)
        ///   LASX: XVSLEI.H Xd.16H, Xj.16H, si5
        /// </summary>
        public static Vector256<short> CompareLessThanOrEqual(Vector256<short> value, [ConstantExpected(Min = -16, Max = (byte)(15))] sbyte si5) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvslei_w(int32x8_t a, int8_t si5)
        ///   LASX: XVSLEI.W Xd.8W, Xj.8W, si5
        /// </summary>
        public static Vector256<int> CompareLessThanOrEqual(Vector256<int> value, [ConstantExpected(Min = -16, Max = (byte)(15))] sbyte si5) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvslei_d(int64x4_t a, int8_t si5)
        ///   LASX: XVSLEI.D Xd.4D, Xj.4D, si5
        /// </summary>
        public static Vector256<long> CompareLessThanOrEqual(Vector256<long> value, [ConstantExpected(Min = -16, Max = (byte)(15))] sbyte si5) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvsle_b(int8x32_t a, int8x32_t b)
        ///   LASX: XVSLE.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> CompareLessThanOrEqual(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvsle_h(int16x16_t a, int16x16_t b)
        ///   LASX: XVSLE.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> CompareLessThanOrEqual(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvsle_w(int32x8_t a, int32x8_t b)
        ///   LASX: XVSLE.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> CompareLessThanOrEqual(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvsle_d(int64x4_t a, int64x4_t b)
        ///   LASX: XVSLE.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> CompareLessThanOrEqual(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvslei_bu(uint8x32_t a, uint8_t ui5)
        ///   LASX: XVSLEI.BU Xd.32B, Xj.32B, ui5
        /// </summary>
        public static Vector256<byte> CompareLessThanOrEqual(Vector256<byte> value, [ConstantExpected(Max = (byte)(31))] byte ui5) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvslei_hu(uint16x16_t a, uint8_t ui5)
        ///   LASX: XVSLEI.HU Xd.16H, Xj.16H, ui5
        /// </summary>
        public static Vector256<ushort> CompareLessThanOrEqual(Vector256<ushort> value, [ConstantExpected(Max = (byte)(31))] byte ui5) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvslei_wu(uint32x8_t a, uint8_t ui5)
        ///   LASX: XVSLEI.WU Xd.8W, Xj.8W, ui5
        /// </summary>
        public static Vector256<uint> CompareLessThanOrEqual(Vector256<uint> value, [ConstantExpected(Max = (byte)(31))] byte ui5) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvslei_du(uint64x4_t a, uint8_t ui5)
        ///   LASX: XVSLEI.DU Xd.4D, Xj.4D, ui5
        /// </summary>
        public static Vector256<ulong> CompareLessThanOrEqual(Vector256<ulong> value, [ConstantExpected(Max = (byte)(31))] byte ui5) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvsle_bu(uint8x32_t a, uint8x32_t b)
        ///   LASX: XVSLE.BU Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> CompareLessThanOrEqual(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvsle_hu(uint16x16_t a, uint16x16_t b)
        ///   LASX: XVSLE.HU Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> CompareLessThanOrEqual(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvsle_wu(uint32x8_t a, uint32x8_t b)
        ///   LASX: XVSLE.WU Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> CompareLessThanOrEqual(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvsle_du(uint64x4_t a, uint64x4_t b)
        ///   LASX: XVSLE.DU Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> CompareLessThanOrEqual(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvfcmp_cle_s(float32x8_t a, float32x8_t b)
        ///   LASX: XVFCMP.CLE.S Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<int> CompareLessThanOrEqual(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvfcmp_cle_d(float64x4_t a, float64x4_t b)
        ///   LASX: XVFCMP.CLE.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> CompareLessThanOrEqual(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvsle_b(int8x32_t a, int8x32_t b)
        ///   LASX: XVSLE.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> CompareGreaterThan(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvsle_bu(uint8x32_t a, uint8x32_t b)
        ///   LASX: XVSLE.BU Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> CompareGreaterThan(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvsle_h(int16x16_t a, int16x16_t b)
        ///   LASX: XVSLE.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> CompareGreaterThan(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvsle_hu(uint16x16_t a, uint16x16_t b)
        ///   LASX: XVSLE.HU Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> CompareGreaterThan(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvsle_w(int32x8_t a, int32x8_t b)
        ///   LASX: XVSLE.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> CompareGreaterThan(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvsle_wu(uint32x8_t a, uint32x8_t b)
        ///   LASX: XVSLE.WU Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> CompareGreaterThan(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvsle_d(int64x4_t a, int64x4_t b)
        ///   LASX: XVSLE.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> CompareGreaterThan(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvsle_du(uint64x4_t a, uint64x4_t b)
        ///   LASX: XVSLE.DU Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> CompareGreaterThan(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvfcmp_cle_s(float32x8_t a, float32x8_t b)
        ///   LASX: XVFCMP.CLE.S Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<float> CompareGreaterThan(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvfcmp_cle_d(float64x4_t a, float64x4_t b)
        ///   LASX: XVFCMP.CLE.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<double> CompareGreaterThan(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int8x32_t xvmskgez_b(int8x32_t value)
        ///   LASX: XVMSKGEZ.B Xd.32B, Xj.32B
        /// </summary>
        public static Vector256<sbyte> CompareGreaterThanOrEqualZeroEach128(Vector256<sbyte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvslt_b(int8x32_t a, int8x32_t b)
        ///   LASX: XVSLT.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> CompareGreaterThanOrEqual(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvslt_bu(uint8x32_t a, uint8x32_t b)
        ///   LASX: XVSLT.BU Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> CompareGreaterThanOrEqual(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvslt_h(int16x16_t a, int16x16_t b)
        ///   LASX: XVSLT.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> CompareGreaterThanOrEqual(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvslt_hu(uint16x16_t a, uint16x16_t b)
        ///   LASX: XVSLT.HU Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> CompareGreaterThanOrEqual(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvslt_w(int32x8_t a, int32x8_t b)
        ///   LASX: XVSLT.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> CompareGreaterThanOrEqual(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvslt_wu(uint32x8_t a, uint32x8_t b)
        ///   LASX: XVSLT.WU Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> CompareGreaterThanOrEqual(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvslt_d(int64x4_t a, int64x4_t b)
        ///   LASX: XVSLT.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> CompareGreaterThanOrEqual(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvslt_du(uint64x4_t a, uint64x4_t b)
        ///   LASX: XVSLT.DU Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> CompareGreaterThanOrEqual(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvfcmp_clt_s(float32x8_t a, float32x8_t b)
        ///   LASX: XVFCMP.CLT.S Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<float> CompareGreaterThanOrEqual(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvfcmp_clt_d(float64x4_t a, float64x4_t b)
        ///   LASX: XVFCMP.CLT.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<double> CompareGreaterThanOrEqual(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvmaxi_b(int8x32_t a, int8_t si5)
        ///   LASX: XVMAXI.B Xd.32B, Xj.32B, si5
        /// </summary>
        public static Vector256<sbyte> Max(Vector256<sbyte> value, const sbyte si5) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvmaxi_bu(uint8x32_t a, int8_t si5)
        ///   LASX: XVMAXI.BU Xd.32B, Xj.32B, ui5
        /// </summary>
        public static Vector256<byte> Max(Vector256<byte> value, const byte ui5) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvmaxi_h(int16x16_t a, int8_t si5)
        ///   LASX: XVMAXI.H Xd.16H, Xj.16H, si5
        /// </summary>
        public static Vector256<short> Max(Vector256<short> value, const sbyte si5) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvmaxi_hu(uint16x16_t a, int8_t si5)
        ///   LASX: XVMAXI.HU Xd.16H, Xj.16H, ui5
        /// </summary>
        public static Vector256<ushort> Max(Vector256<ushort> value, const byte ui5) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvmaxi_w(int32x8_t a, int8_t si5)
        ///   LASX: XVMAXI.W Xd.8W, Xj.8W, si5
        /// </summary>
        public static Vector256<int> Max(Vector256<int> value, const sbyte si5) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvmaxi_wu(uint32x8_t a, int8_t si5)
        ///   LASX: XVMAXI.WU Xd.8W, Xj.8W, ui5
        /// </summary>
        public static Vector256<uint> Max(Vector256<uint> value, const byte ui5) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvmaxi_d(int64x4_t a, int8_t si5)
        ///   LASX: XVMAXI.D Xd.4D, Xj.4D, si5
        /// </summary>
        public static Vector256<long> Max(Vector256<long> value, const sbyte si5) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvmaxi_du(uint64x4_t a, int8_t si5)
        ///   LASX: XVMAXI.DU Xd.4D, Xj.4D, ui5
        /// </summary>
        public static Vector256<ulong> Max(Vector256<ulong> value, const byte ui5) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvmax_b(int8x32_t a, int8x32_t b)
        ///   LASX: XVMAX.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> Max(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvmax_bu(uint8x32_t a, uint8x32_t b)
        ///   LASX: XVMAX.BU Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> Max(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvmax_h(int16x16_t a, int16x16_t b)
        ///   LASX: XVMAX.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> Max(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvmax_hu(uint16x16_t a, uint16x16_t b)
        ///   LASX: XVMAX.HU Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> Max(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvmax_w(int32x8_t a, int32x8_t b)
        ///   LASX: XVMAX.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> Max(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvmax_wu(uint32x8_t a, uint32x8_t b)
        ///   LASX: XVMAX.WU Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> Max(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvmax_d(int64x4_t a, int64x4_t b)
        ///   LASX: XVMAX.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> Max(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvmax_du(uint64x4_t a, uint64x4_t b)
        ///   LASX: XVMAX.DU Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> Max(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x8_t xvfmax_s(float32x8_t a, float32x8_t b)
        ///   LASX: XVFMAX.S Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<float> Max(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x4_t xvfmax_d(float64x4_t a, float64x4_t b)
        ///   LASX: XVFMAX.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<double> Max(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x8_t xvfmaxa_s(float32x8_t a, float32x8_t b)
        ///   LASX: XVFMAXA.S Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<float> MaxFloatAbsolute(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x4_t xvfmaxa_d(float64x4_t a, float64x4_t b)
        ///   LASX: XVFMAXA.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<double> MaxFloatAbsolute(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvmini_b(int8x32_t a, int8_t si5)
        ///   LASX: XVMINI.B Xd.32B, Xj.32B, si5
        /// </summary>
        public static Vector256<sbyte> Min(Vector256<sbyte> value, const sbyte si5) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvmini_bu(uint8x32_t a, int8_t si5)
        ///   LASX: XVMINI.BU Xd.32B, Xj.32B, ui5
        /// </summary>
        public static Vector256<byte> Min(Vector256<byte> value, const byte ui5) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvmini_h(int16x16_t a, int8_t si5)
        ///   LASX: XVMINI.H Xd.16H, Xj.16H, si5
        /// </summary>
        public static Vector256<short> Min(Vector256<short> value, const sbyte si5) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvmini_hu(uint16x16_t a, int8_t si5)
        ///   LASX: XVMINI.HU Xd.16H, Xj.16H, ui5
        /// </summary>
        public static Vector256<ushort> Min(Vector256<ushort> value, const byte ui5) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvmini_w(int32x8_t a, int8_t si5)
        ///   LASX: XVMINI.W Xd.8W, Xj.8W, si5
        /// </summary>
        public static Vector256<int> Min(Vector256<int> value, const sbyte si5) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvmini_wu(uint32x8_t a, int8_t si5)
        ///   LASX: XVMINI.WU Xd.8W, Xj.8W, ui5
        /// </summary>
        public static Vector256<uint> Min(Vector256<uint> value, const byte ui5) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvmini_d(int64x4_t a, int8_t si5)
        ///   LASX: XVMINI.D Xd.4D, Xj.4D, si5
        /// </summary>
        public static Vector256<long> Min(Vector256<long> value, const sbyte si5) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvmini_du(uint64x4_t a, int8_t si5)
        ///   LASX: XVMINI.DU Xd.4D, Xj.4D, ui5
        /// </summary>
        public static Vector256<ulong> Min(Vector256<ulong> value, const byte ui5) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvmin_b(int8x32_t a, int8x32_t b)
        ///   LASX: XVMIN.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> Min(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvmin_bu(uint8x32_t a, uint8x32_t b)
        ///   LASX: XVMIN.BU Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> Min(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvmin_h(int16x16_t a, int16x16_t b)
        ///   LASX: XVMIN.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> Min(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvmin_hu(uint16x16_t a, uint16x16_t b)
        ///   LASX: XVMIN.HU Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> Min(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvmin_w(int32x8_t a, int32x8_t b)
        ///   LASX: XVMIN.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> Min(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvmin_wu(uint32x8_t a, uint32x8_t b)
        ///   LASX: XVMIN.WU Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> Min(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvmin_d(int64x4_t a, int64x4_t b)
        ///   LASX: XVMIN.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> Min(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvmin_du(uint64x4_t a, uint64x4_t b)
        ///   LASX: XVMIN.DU Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> Min(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x8_t xvfmin_s(float32x8_t a, float32x8_t b)
        ///   LASX: XVFMIN.S Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<float> Min(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x4_t xvfmin_d(float64x4_t a, float64x4_t b)
        ///   LASX: XVFMIN.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<double> Min(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x8_t xvfmina_s(float32x8_t a, float32x8_t b)
        ///   LASX: XVFMINA.S Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<float> MinFloatAbsolute(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x4_t xvfmina_d(float64x4_t a, float64x4_t b)
        ///   LASX: XVFMINA.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<double> MinFloatAbsolute(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvbitsel_v(uint8x32_t a, int8x32_t b, int8x32_t c)
        ///   LASX: XVBITSEL.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> BitwiseSelect(Vector256<sbyte> select, Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvbitsel_v(uint8x32_t a, uint8x32_t b, uint8x32_t c)
        ///   LASX: XVBITSEL.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> BitwiseSelect(Vector256<byte> select, Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvbitsel_v(uint16x16_t a, int16x16_t b, int16x16_t c)
        ///   LASX: XVBITSEL.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<short> BitwiseSelect(Vector256<short> select, Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvbitsel_v(uint16x16_t a, uint16x16_t b, uint16x16_t c)
        ///   LASX: XVBITSEL.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<ushort> BitwiseSelect(Vector256<ushort> select, Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvbitsel_v(uint32x8_t a, int32x8_t b, int32x8_t c)
        ///   LASX: XVBITSEL.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<int> BitwiseSelect(Vector256<int> select, Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvbitsel_v(uint32x8_t a, uint32x8_t b, uint32x8_t c)
        ///   LASX: XVBITSEL.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<uint> BitwiseSelect(Vector256<uint> select, Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvbitsel_v(uint64x4_t a, int64x4_t b, int64x4_t c)
        ///   LASX: XVBITSEL.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<long> BitwiseSelect(Vector256<long> select, Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvbitsel_v(uint64x4_t a, uint64x4_t b, uint64x4_t c)
        ///   LASX: XVBITSEL.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<ulong> BitwiseSelect(Vector256<ulong> select, Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x8_t xvbitsel_v(uint32x8_t a, float32x8_t b, float32x8_t c)
        ///   LASX: XVBITSEL.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<float> BitwiseSelect(Vector256<float> select, Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x4_t xvbitsel_v(uint64x4_t a, float64x4_t b, float64x4_t c)
        ///   LASX: XVBITSEL.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<double> BitwiseSelect(Vector256<double> select, Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvabsd_b(int8x32_t a, int8x32_t b)
        ///   LASX: XVABSD.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> AbsoluteDifference(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvabsd_bu(uint8x32_t a, uint8x32_t b)
        ///   LASX: XVABSD.BU Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> AbsoluteDifference(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvabsd_h(int16x16_t a, int16x16_t b)
        ///   LASX: XVABSD.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> AbsoluteDifference(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvabsd_hu(uint16x16_t a, uint16x16_t b)
        ///   LASX: XVABSD.HU Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> AbsoluteDifference(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvabsd_w(int32x8_t a, int32x8_t b)
        ///   LASX: XVABSD.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> AbsoluteDifference(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvabsd_wu(uint32x8_t a, uint32x8_t b)
        ///   LASX: XVABSD.WU Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> AbsoluteDifference(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvabsd_d(uint64x4_t a, int64x4_t b, int64x4_t c)
        ///   LASX: XVABSD.D Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<ulong> AbsoluteDifference(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvabsd_du(uint64x4_t a, uint64x4_t b, uint64x4_t c)
        ///   LASX: XVABSD.DU Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<ulong> AbsoluteDifference(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        ///// <summary>
        ///// float32x8_t TODO(float32x8_t a, float32x8_t b)   multi-instructions.
        /////   LASX: TODO Xd.8S, Xj.8S, Xk.8S
        ///// </summary>
        //public static Vector256<float> AbsoluteDifference(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }

        ///// <summary>
        ///// float64x4_t TODO(float64x4_t a, float64x4_t b)
        /////   LASX: TODO Xd.4D, Xj.4D, Xk.4D
        ///// </summary>
        //public static Vector256<double> AbsoluteDifference(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvld(int8_t const * ptr, const short si12)
        ///   LASX: XVLD Xd.32B, Rj, si12
        /// </summary>
        public static unsafe Vector256<sbyte> LoadVector256(sbyte* address, [ConstantExpected(Min = -2048, Max = 2047)] short si12) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvld(uint8_t const * ptr, const short si12)
        ///   LASX: XVLD Xd.32B, Rj, si12
        /// </summary>
        public static unsafe Vector256<byte> LoadVector256(byte* address, [ConstantExpected(Min = -2048, Max = 2047)] short si12) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvld(int16_t const * ptr, const short si12)
        ///   LASX: XVLD Xd.16H, Rj, si12
        /// </summary>
        public static unsafe Vector256<short> LoadVector256(short* address, [ConstantExpected(Min = -2048, Max = 2047)] short si12) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvld(uint16_t const * ptr, const short si12)
        ///   LASX: XVLD Xd.16H, Rj, si12
        /// </summary>
        public static unsafe Vector256<ushort> LoadVector256(ushort* address, [ConstantExpected(Min = -2048, Max = 2047)] short si12) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvld(int32_t const * ptr, const short si12)
        ///   LASX: XVLD Xd.8W, Rj, si12
        /// </summary>
        public static unsafe Vector256<int> LoadVector256(int* address, [ConstantExpected(Min = -2048, Max = 2047)] short si12) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvld(uint32_t const * ptr, const short si12)
        ///   LASX: XVLD Xd.8W, Rj, si12
        /// </summary>
        public static unsafe Vector256<uint> LoadVector256(uint* address, [ConstantExpected(Min = -2048, Max = 2047)] short si12) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvld(int64_t const * ptr, const short si12)
        ///   LASX: XVLD Xd.4D, Rj, si12
        /// </summary>
        public static unsafe Vector256<long> LoadVector256(long* address, [ConstantExpected(Min = -2048, Max = 2047)] short si12) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvld(uint64_t const * ptr, const short si12)
        ///   LASX: XVLD Xd.4D, Rj, si12
        /// </summary>
        public static unsafe Vector256<ulong> LoadVector256(ulong* address, [ConstantExpected(Min = -2048, Max = 2047)] short si12) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x8_t xvld(float32_t const * ptr, const short si12)
        ///   LASX: XVLD Xd.8S, Rj, si12
        /// </summary>
        public static unsafe Vector256<float> LoadVector256(float* address, [ConstantExpected(Min = -2048, Max = 2047)] short si12) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x4_t xvld(float64_t const * ptr, const short si12)
        ///   LASX: XVLD Xd.4D, Rj, si12
        /// </summary>
        public static unsafe Vector256<double> LoadVector256(double* address, [ConstantExpected(Min = -2048, Max = 2047)] short si12) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvldx(int8_t const * ptr, long offsetValue)
        ///   LASX: XVLDX Xd.32B, Rj, Rk
        /// </summary>
        public static unsafe Vector256<sbyte> LoadVector256(sbyte* address, long offsetValue) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvldx(uint8_t const * ptr, long offsetValue)
        ///   LASX: XVLDX Xd.32B, Rj, Rk
        /// </summary>
        public static unsafe Vector256<byte> LoadVector256(byte* address, long offsetValue) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvldx(int16_t const * ptr, long offsetValue)
        ///   LASX: XVLDX Xd.16H, Rj, Rk
        /// </summary>
        public static unsafe Vector256<short> LoadVector256(short* address, long offsetValue) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvldx(uint16_t const * ptr, long offsetValue)
        ///   LASX: XVLDX Xd.16H, Rj, Rk
        /// </summary>
        public static unsafe Vector256<ushort> LoadVector256(ushort* address, long offsetValue) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvldx(int32_t const * ptr, long offsetValue)
        ///   LASX: XVLDX Xd.8W, Rj, Rk
        /// </summary>
        public static unsafe Vector256<int> LoadVector256(int* address, long offsetValue) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvldx(uint32_t const * ptr, long offsetValue)
        ///   LASX: XVLDX Xd.8W, Rj, Rk
        /// </summary>
        public static unsafe Vector256<uint> LoadVector256(uint* address, long offsetValue) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvldx(int64_t const * ptr, long offsetValue)
        ///   LASX: XVLDX Xd.4D, Rj, Rk
        /// </summary>
        public static unsafe Vector256<long> LoadVector256(long* address, long offsetValue) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvldx(uint64_t const * ptr, long offsetValue)
        ///   LASX: XVLDX Xd.4D, Rj, Rk
        /// </summary>
        public static unsafe Vector256<ulong> LoadVector256(ulong* address, long offsetValue) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x8_t xvldx(float32_t const * ptr, long offsetValue)
        ///   LASX: XVLDX Xd.8S, Rj, Rk
        /// </summary>
        public static unsafe Vector256<float> LoadVector256(float* address, long offsetValue) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x4_t xvldx(float64_t const * ptr, long offsetValue)
        ///   LASX: XVLDX Xd.4D, Rj, Rk
        /// </summary>
        public static unsafe Vector256<double> LoadVector256(double* address, long offsetValue) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvldrepl_b(int8_t const * ptr, const short si12)
        ///   LASX: XVLDREPL.B Xd.32B, Rj, si12
        /// </summary>
        public static unsafe Vector256<sbyte> LoadElementReplicateVector(sbyte* address, [ConstantExpected(Min = -2048, Max = 2047)] short si12) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvldrepl_b(uint8_t const * ptr, const short si12)
        ///   LASX: XVLDREPL.B Xd.32B, Rj, si12
        /// </summary>
        public static unsafe Vector256<byte> LoadElementReplicateVector(byte* address, [ConstantExpected(Min = -2048, Max = 2047)] short si12) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvldrepl_h(int16_t const * ptr, const short si12)
        ///   LASX: XVLDREPL.H Xd.16H, Rj, si11
        /// </summary>
        public static unsafe Vector256<short> LoadElementReplicateVector(short* address, [ConstantExpected(Min = -2048, Max = 2047)] short si12) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvldrepl_h(uint16_t const * ptr, const short si12)
        ///   LASX: XVLDREPL.H Xd.16H, Rj, si11
        /// </summary>
        public static unsafe Vector256<ushort> LoadElementReplicateVector(ushort* address, [ConstantExpected(Min = -2048, Max = 2047)] short si12) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvldrepl_w(int32_t const * ptr, const short si12)
        ///   LASX: XVLDREPL.W Xd.8W, Rj, si10
        /// </summary>
        public static unsafe Vector256<int> LoadElementReplicateVector(int* address, [ConstantExpected(Min = -2048, Max = 2047)] short si12) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvldrepl_w(uint32_t const * ptr, const short si12)
        ///   LASX: XVLDREPL.W Xd.8W, Rj, si10
        /// </summary>
        public static unsafe Vector256<uint> LoadElementReplicateVector(uint* address, [ConstantExpected(Min = -2048, Max = 2047)] short si12) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvldrepl_d(int64_t const * ptr, const short si12)
        ///   LASX: XVLDREPL.D Xd.4D, Rj, si9
        /// </summary>
        public static unsafe Vector256<long> LoadElementReplicateVector(long* address, [ConstantExpected(Min = -2048, Max = 2047)] short si12) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvldrepl_d(uint64_t const * ptr, const short si12)
        ///   LASX: XVLDREPL.D Xd.4D, Rj, si9
        /// </summary>
        public static unsafe Vector256<ulong> LoadElementReplicateVector(ulong* address, [ConstantExpected(Min = -2048, Max = 2047)] short si12) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x8_t vld(float32_t const * ptr, const short si12)
        ///   LASX: XVLDREPL.W Xd.4S, Rj, si10
        /// </summary>
        public static unsafe Vector256<float> LoadElementReplicateVector(float* address, [ConstantExpected(Min = -2048, Max = 2047)] short si12) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x4_t xvldrepl_d(float64_t const * ptr, const short si12)
        ///   LASX: XVLDREPL.D Xd.4D, Rj, si9
        /// </summary>
        public static unsafe Vector256<double> LoadElementReplicateVector(double* address, [ConstantExpected(Min = -2048, Max = 2047)] short si12) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x8_t xvfrecip_s(float32x8_t a)
        ///   LASX: XVFRECIP.S Xd.8S Xj.8S
        /// </summary>
        public static Vector256<float> Reciprocal(Vector256<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x4_t xvfrecip_d(float64x4_t a)
        ///   LASX: XVFRECIP.D Xd.4D Xj.4D
        /// </summary>
        public static Vector256<double> Reciprocal(Vector256<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x8_t xvfrsqrt_s(float32x8_t a)
        ///   LASX: XVFRSQRT.S Xd.8S Xj.8S
        /// </summary>
        public static Vector256<float> ReciprocalSqrt(Vector256<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x4_t xvfrsqrt_d(float64x4_t a)
        ///   LASX: XVFRSQRT.D Xd.4D Xj.4D
        /// </summary>
        public static Vector256<double> ReciprocalSqrt(Vector256<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x8_t xvfsqrt_s(float32x8_t a)
        ///   LASX: XVFSQRT.S Xd.8S, Xj.8S
        /// </summary>
        public static Vector256<float> Sqrt(Vector256<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x4_t xvfsqrt_d(float64x4_t a)
        ///   LASX: XVFSQRT.D Xd.4D, Xj.4D
        /// </summary>
        public static Vector256<double> Sqrt(Vector256<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x8_t xvflogb_d(float32x8_t a)
        ///   LASX: XVFLOGB.S Xd.8S, Xj.8S
        /// </summary>
        public static Vector256<float> FloatLogarithm2(Vector256<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x4_t xvflogb_d(float64x4_t a)
        ///   LASX: XVFLOGB.D Xd.4D, Xj.4D
        /// </summary>
        public static Vector256<double> FloatLogarithm2(Vector256<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void xvst(int8x32_t val, int8_t* addr, const short si12)
        ///   LASX: XVST Xd.32B, Rj, si12
        /// </summary>
        public static unsafe void Store(Vector256<sbyte> vector, sbyte* addr, [ConstantExpected(Min = -2048, Max = 2047)] short si12) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void xvst(uint8x32_t val, uint8_t* addr, const short si12)
        ///   LASX: XVST Xd.32B, Rj, si12
        /// </summary>
        public static unsafe void Store(Vector256<byte> vector, byte* addr, [ConstantExpected(Min = -2048, Max = 2047)] short si12) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void xvst(int16x16_t val, int16_t* addr, const short si12)
        ///   LASX: XVST Xd.16H, Rj, si12
        /// </summary>
        public static unsafe void Store(Vector256<short> vector, short* addr, [ConstantExpected(Min = -2048, Max = 2047)] short si12) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void xvst(uint16x16_t val, uint16_t* addr, const short si12)
        ///   LASX: XVST Xd.16H, Rj, si12
        /// </summary>
        public static unsafe void Store(Vector256<ushort> vector, ushort* addr, [ConstantExpected(Min = -2048, Max = 2047)] short si12) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void xvst(int32x8_t val, int32_t* addr, const short si12)
        ///   LASX: XVST Xd.8W, Rj, si12
        /// </summary>
        public static unsafe void Store(Vector256<int> vector, int* addr, [ConstantExpected(Min = -2048, Max = 2047)] short si12) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void xvst(uint32x8_t val, uint32_t* addr, const short si12)
        ///   LASX: XVST Xd.8W, Rj, si12
        /// </summary>
        public static unsafe void Store(Vector256<uint> vector, uint* addr, [ConstantExpected(Min = -2048, Max = 2047)] short si12) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void xvst(int64x4_t val, int64_t* addr, const short si12)
        ///   LASX: XVST Xd.4D, Rj, si12
        /// </summary>
        public static unsafe void Store(Vector256<long> vector, long* addr, [ConstantExpected(Min = -2048, Max = 2047)] short si12) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void xvst(uint64x4_t val, uint64_t* addr, const short si12)
        ///   LASX: XVST Xd.4D, Rj, si12
        /// </summary>
        public static unsafe void Store(Vector256<ulong> vector, ulong* addr, [ConstantExpected(Min = -2048, Max = 2047)] short si12) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void xvst(float32x8_t val, float32_t* addr, const short si12)
        ///   LASX: XVST Xd.8W, Rj, si12
        /// </summary>
        public static unsafe void Store(Vector256<float> vector, float* addr, [ConstantExpected(Min = -2048, Max = 2047)] short si12) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void xvst(float64x4_t val, float64_t* addr, const short si12)
        ///   LASX: XVST Xd.4D, Rj, si12
        /// </summary>
        public static unsafe void Store(Vector256<double> vector, double* addr, [ConstantExpected(Min = -2048, Max = 2047)] short si12) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void xvstx(int8x32_t val, int8_t* addr, long offsetValue)
        ///   LASX: XVSTX Xd.32B, Rj, Rk
        /// </summary>
        public static unsafe void Store(Vector256<sbyte> vector, sbyte* addr, long offsetValue) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void xvstx(uint8x32_t val, uint8_t* addr, long offsetValue)
        ///   LASX: XVSTX Xd.32B, Rj, Rk
        /// </summary>
        public static unsafe void Store(Vector256<byte> vector, byte* addr, long offsetValue) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void xvstx(int16x16_t val, int16_t* addr, long offsetValue)
        ///   LASX: XVSTX Xd.16H, Rj, Rk
        /// </summary>
        public static unsafe void Store(Vector256<short> vector, short* addr, long offsetValue) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void xvstx(uint16x16_t val, uint16_t* addr, long offsetValue)
        ///   LASX: XVSTX Xd.16H, Rj, Rk
        /// </summary>
        public static unsafe void Store(Vector256<ushort> vector, ushort* addr, long offsetValue) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void xvstx(int32x8_t val, int32_t* addr, long offsetValue)
        ///   LASX: XVSTX Xd.8W, Rj, Rk
        /// </summary>
        public static unsafe void Store(Vector256<int> vector, int* addr, long offsetValue) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void xvstx(uint32x8_t val, uint32_t* addr, long offsetValue)
        ///   LASX: XVSTX Xd.8W, Rj, Rk
        /// </summary>
        public static unsafe void Store(Vector256<uint> vector, uint* addr, long offsetValue) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void xvstx(int64x4_t val, int64_t* addr, long offsetValue)
        ///   LASX: XVSTX Xd.4D, Rj, Rk
        /// </summary>
        public static unsafe void Store(Vector256<long> vector, long* addr, long offsetValue) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void xvstx(uint64x4_t val, uint64_t* addr, long offsetValue)
        ///   LASX: XVSTX Xd.4D, Rj, Rk
        /// </summary>
        public static unsafe void Store(Vector256<ulong> vector, ulong* addr, long offsetValue) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void xvstx(float32x8_t val, float32_t* addr, long offsetValue)
        ///   LASX: XVSTX Xd.8W, Rj, Rk
        /// </summary>
        public static unsafe void Store(Vector256<float> vector, float* addr, long offsetValue) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void xvstx(float64x4_t val, float64_t* addr, long offsetValue)
        ///   LASX: XVSTX Xd.4D, Rj, Rk
        /// </summary>
        public static unsafe void Store(Vector256<double> vector, double* addr, long offsetValue) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void xvstelm_b(int8x32_t val, int8_t* addr, const short si8, const byte idx)
        ///   LASX: XVSTELM.B Xd.32B, Rj, si8, idx
        /// </summary>
        public static unsafe void StoreElement(Vector256<sbyte> vector, sbyte* addr, [ConstantExpected(Min = -128, Max = 127)] short si8, [ConstantExpected(Max = (byte)(31))] byte idx) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void xvstelm_b(uint8x32_t val, uint8_t* addr, const short si8, const byte idx)
        ///   LASX: XVSTELM.B Xd.32B, Rj, si8, idx
        /// </summary>
        public static unsafe void StoreElement(Vector256<byte> vector, byte* addr, [ConstantExpected(Min = -128, Max = 127)] short si8, [ConstantExpected(Max = (byte)(31))] byte idx) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void xvstelm_h(int16x16_t val, int16_t* addr, const short si9, const byte idx) // Note: si9 is 2byte aligned.
        ///   LASX: XVSTELM.H Xd.16H, Rj, si8, idx
        /// </summary>
        public static unsafe void StoreElement(Vector256<short> vector, short* addr, [ConstantExpected(Min = -256, Max = 254)] short si9, [ConstantExpected(Max = (byte)(15))] byte idx) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void xvstelm_h(uint16x16_t val, uint16_t* addr, const short si9, const byte idx)
        ///   LASX: XVSTELM.H Xd.16H, Rj, si8, idx
        /// </summary>
        public static unsafe void StoreElement(Vector256<ushort> vector, ushort* addr, [ConstantExpected(Min = -256, Max = 254)] short si9, [ConstantExpected(Max = (byte)(15))] byte idx) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void xvstelm_w(int32x8_t val, int32_t* addr, const short si10, const byte idx) // Note: si10 is 4byte aligned.
        ///   LASX: XVSTELM.W Xd.8W, Rj, si8, idx
        /// </summary>
        public static unsafe void StoreElement(Vector256<int> vector, int* addr, [ConstantExpected(Min = -512, Max = 508)] short si10, [ConstantExpected(Max = (byte)(7))] byte idx) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void xvstelm_w(uint32x8_t val, uint32_t* addr, const short si10, const byte idx)
        ///   LASX: XVSTELM.W Xd.8W, Rj, si8, idx
        /// </summary>
        public static unsafe void StoreElement(Vector256<uint> vector, uint* addr, [ConstantExpected(Min = -512, Max = 508)] short si10, [ConstantExpected(Max = (byte)(7))] byte idx) { throw new PlatformNotSupportedException(); }

        /// <summary>
 const      /// void xvstelm_d(int64x4_t val, int64_t* addr, const short si11, const byte idx) // Note: si11 is 8byte aligned.
        ///   LASX: XVSTELM.D Xd.4D, Rj, si8, idx
        /// </summary>
        public static unsafe void StoreElement(Vector256<long> vector, long* addr, [ConstantExpected(Min = -1024, Max = 1016)] short si11, [ConstantExpected(Max = (byte)(3))] byte idx) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void xvstelm_d(uint64x4_t val, uint64_t* addr, const short si11, const byte idx)
        ///   LASX: XVSTELM.D Xd.4D, Rj, si8, idx
        /// </summary>
        public static unsafe void StoreElement(Vector256<ulong> vector, ulong* addr, [ConstantExpected(Min = -1024, Max = 1016)] short si11, [ConstantExpected(Max = (byte)(3))] byte idx) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void xvstelm_w(float32x8_t val, float32_t* addr, const short si10, const byte idx)
        ///   LASX: XVSTELM.W Xd.8S, Rj, si8, idx
        /// </summary>
        public static unsafe void StoreElement(Vector256<float> vector, float* addr, [ConstantExpected(Min = -512, Max = 511)] short si10, [ConstantExpected(Max = (byte)(7))] byte idx) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void xvstelm_d(float64x4_t val, float64_t* addr, const short si11, const byte idx)
        ///   LASX: XVSTELM.D Xd.4D, Rj, si8, idx
        /// </summary>
        public static unsafe void StoreElement(Vector256<double> vector, double* addr, [ConstantExpected(Min = -1024, Max = 1016)] short si11, [ConstantExpected(Max = (byte)(3))] byte idx) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvneg_b(int8x32_t a)
        ///   LASX: XVNEG.B Xd.32B, Xj.32B
        /// </summary>
        public static Vector256<sbyte> Negate(Vector256<sbyte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvneg_h(int16x16_t a)
        ///   LASX: XVNEG.H Xd.16H, Xj.16H
        /// </summary>
        public static Vector256<short> Negate(Vector256<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvneg_w(int32x8_t a)
        ///   LASX: XVNEG.W Xd.8W, Xj.8W
        /// </summary>
        public static Vector256<int> Negate(Vector256<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvneg_d(int64x4_t a)
        ///   LASX: XVNEG.D Xd.4D, Xj.4D
        /// </summary>
        public static Vector256<long> Negate(Vector256<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x8_t xvbitrevi_w(float32x8_t a)
        ///   LASX: XVBITREVI.W Xd.8W, Xj.8W, 31
        /// </summary>
        public static Vector256<float> Negate(Vector256<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x4_t xvbitrevi_d(float64x4_t a)
        ///   LASX: XVBITREVI.D Xd.4D, Xj.4D
        /// </summary>
        public static Vector256<double> Negate(Vector256<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvmulwod_h_b(int8x32_t a, int8x32_t b)
        ///   LASX: XVMULWOD.H.B Xd.16H, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<short> MultiplyWideningOdd(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvmulwod_w_h(int16x16_t a, int16x16_t b)
        ///   LASX: XVMULWOD.W.H Xd.8W, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<int> MultiplyWideningOdd(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvmulwod_d_w(int32x8_t a, int32x8_t b)
        ///   LASX: XVMULWOD.D.W Xd.4D, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<long> MultiplyWideningOdd(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int128x2_t xvmulwod_q_d(int64x4_t a, int64x4_t b)
        ///   LASX: XVMULWOD.Q.D Xd.2Q, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> MultiplyWideningOdd(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvmulwev_h_b(int8x32_t a, int8x32_t b)
        ///   LASX: XVMULWEV.H.B Xd.16H, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<short> MultiplyWideningEven(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvmulwev_w_h(int16x16_t a, int16x16_t b)
        ///   LASX: XVMULWEV.W.H Xd.8W, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<int> MultiplyWideningEven(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvmulwev_d_w(int32x8_t a, int32x8_t b)
        ///   LASX: XVMULWEV.D.W Xd.4D, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<long> MultiplyWideningEven(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int128x2_t xvmulwev_q_d(int64x4_t a, int64x4_t b)
        ///   LASX: XVMULWEV.Q.D Xd.2Q, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> MultiplyWideningEven(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvmulwod_hu_bu(uint8x32_t a, uint8x32_t b)
        ///   LASX: XVMULWOD.H.BU Xd.16H, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<ushort> MultiplyWideningOdd(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvmulwod_wu_hu(uint16x16_t a, uint16x16_t b)
        ///   LASX: XVMULWOD.W.HU Xd.8W, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<uint> MultiplyWideningOdd(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvmulwod_du_wu(uint32x8_t a, uint32x8_t b)
        ///   LASX: XVMULWOD.D.WU Xd.4D, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<ulong> MultiplyWideningOdd(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint128x2_t xvmulwod_qu_du(uint64x4_t a, uint64x4_t b)
        ///   LASX: XVMULWOD.Q.DU Xd.2Q, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> MultiplyWideningOdd(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvmulwev_hu_bu(uint8x32_t a, uint8x32_t b)
        ///   LASX: XVMULWEV.H.BU Xd.16H, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<ushort> MultiplyWideningEven(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvmulwev_wu_hu(uint16x16_t a, uint16x16_t b)
        ///   LASX: XVMULWEV.W.HU Xd.8W, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<uint> MultiplyWideningEven(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvmulwev_du_wu(uint32x8_t a, uint32x8_t b)
        ///   LASX: XVMULWEV.D.WU Xd.4D, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<ulong> MultiplyWideningEven(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint128x2_t xvmulwev_qu_du(uint64x4_t a, uint64x4_t b)
        ///   LASX: XVMULWEV.Q.DU Xd.2Q, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> MultiplyWideningEven(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        // /// <summary>
        // /// int16x16_t xvmulwod_h_bu(uint8x32_t a, int8x32_t b)
        // ///   LASX: XVMULWOD.H.BU.B Xd.16H, Xj.32B, Xk.32B
        // /// </summary>
        // public static Vector256<short> MultiplyWideningOdd(Vector256<byte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        // /// <summary>
        // /// int32x8_t xvmulwod_w_hu(uint16x16_t a, int16x16_t b)
        // ///   LASX: XVMULWOD.W.HU.H Xd.8W, Xj.16H, Xk.16H
        // /// </summary>
        // public static Vector256<int> MultiplyWideningOdd(Vector256<ushort> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        // /// <summary>
        // /// int64x4_t xvmulwod_d_wu(uint32x8_t a, int32x8_t b)
        // ///   LASX: XVMULWOD.D.WU.W Xd.4D, Xj.8W, Xk.8W
        // /// </summary>
        // public static Vector256<long> MultiplyWideningOdd(Vector256<uint> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        // /// <summary>
        // /// int128x2_t xvmulwod_q_du(uint64x4_t a, int64x4_t b)
        // ///   LASX: XVMULWOD.Q.DU.D Xd.2Q, Xj.4D, Xk.4D
        // /// </summary>
        // public static Vector256<long> MultiplyWideningOdd(Vector256<ulong> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        // /// <summary>
        // /// int16x16_t xvmulwev_h_bu(uint8x32_t a, int8x32_t b)
        // ///   LASX: XVMULWEV.H.BU.B Xd.16H, Xj.32B, Xk.32B
        // /// </summary>
        // public static Vector256<short> MultiplyWideningEven(Vector256<byte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        // /// <summary>
        // /// int32x8_t xvmulwev_w_hu(uint16x16_t a, int16x16_t b)
        // ///   LASX: XVMULWEV.W.HU.H Xd.8W, Xj.16H, Xk.16H
        // /// </summary>
        // public static Vector256<int> MultiplyWideningEven(Vector256<ushort> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        // /// <summary>
        // /// int64x4_t xvmulwev_d_wu(uint32x8_t a, int32x8_t b)
        // ///   LASX: XVMULWEV.D.WU.W Xd.4D, Xj.8W, Xk.8W
        // /// </summary>
        // public static Vector256<long> MultiplyWideningEven(Vector256<uint> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        // /// <summary>
        // /// int128x2_t xvmulwev_q_du(uint64x4_t a, int64x4_t b)
        // ///   LASX: XVMULWEV.Q.DU.D Xd.2Q, Xj.4D, Xk.4D
        // /// </summary>
        // public static Vector256<long> MultiplyWideningEven(Vector256<ulong> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvavg_b(int8x32_t a, int8x32_t b)
        ///   LASX: XVAVG.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> Average(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvavg_bu(uint8x32_t a, uint8x32_t b)
        ///   LASX: XVAVG.BU Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> Average(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvavg_h(int16x16_t a, int16x16_t b)
        ///   LASX: XVAVG.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> Average(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvavg_hu(uint16x16_t a, uint16x16_t b)
        ///   LASX: XVAVG.HU Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> Average(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvavg_w(int32x8_t a, int32x8_t b)
        ///   LASX: XVAVG.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> Average(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvavg_wu(uint32x8_t a, uint32x8_t b)
        ///   LASX: XVAVG.WU Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> Average(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvavg_d(int64x4_t a, int64x4_t b)
        ///   LASX: XVAVG.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> Average(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvavg_du(uint64x4_t a, uint64x4_t b)
        ///   LASX: XVAVG.DU Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> Average(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvavgr_b(int8x32_t a, int8x32_t b)
        ///   LASX: XVAVGR.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> AverageRounded(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvavgr_bu(uint8x32_t a, uint8x32_t b)
        ///   LASX: XVAVGR.BU Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> AverageRounded(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvavgr_h(int16x16_t a, int16x16_t b)
        ///   LASX: XVAVGR.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> AverageRounded(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvavgr_hu(uint16x16_t a, uint16x16_t b)
        ///   LASX: XVAVGR.HU Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> AverageRounded(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvavgr_w(int32x8_t a, int32x8_t b)
        ///   LASX: XVAVGR.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> AverageRounded(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvavgr_wu(uint32x8_t a, uint32x8_t b)
        ///   LASX: XVAVGR.WU Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> AverageRounded(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvavgr_d(int64x4_t a, int64x4_t b)
        ///   LASX: XVAVGR.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> AverageRounded(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvavgr_du(uint64x4_t a, uint64x4_t b)
        ///   LASX: XVAVGR.DU Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> AverageRounded(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvsllwil_h_b(int8x32_t a, uint8_t ui3)
        ///   LASX: XVSLLWIL.H.B Xd.16H, Xj.32B, ui3
        /// </summary>
        public static Vector256<short> SignExtendWideningLowerAndShiftLeftEach128(Vector256<sbyte> value, byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvsllwil_h_b(uint8x32_t a, uint8_t ui3)
        ///   LASX: XVSLLWIL.H.B Xd.16H, Xj.32B, ui3
        /// </summary>
        public static Vector256<short> SignExtendWideningLowerAndShiftLeftEach128(Vector256<byte> value, byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvsllwil_w_h(int16x4_t a, uint8_t ui4)
        ///   LASX: XVSLLWIL.W.H Xd.8W, Xj.4H, ui4
        /// </summary>
        public static Vector256<int> SignExtendWideningLowerAndShiftLeftEach128(Vector256<short> value, byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvsllwil_w_h(uint16x4_t a, uint8_t ui4)
        ///   LASX: XVSLLWIL.W.H Xd.8W, Xj.4H, ui4
        /// </summary>
        public static Vector256<int> SignExtendWideningLowerAndShiftLeftEach128(Vector256<ushort> value, byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvsllwil_d_w(int32x2_t a, uint8_t ui5)
        ///   LASX: XVSLLWIL.D.W Xd.4D, Xj.2W, ui5
        /// </summary>
        public static Vector256<long> SignExtendWideningLowerAndShiftLeftEach128(Vector256<int> value, byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvsllwil_d_w(uint32x2_t a, uint8_t ui5)
        ///   LASX: XVSLLWIL.D.W Xd.4D, Xj.2W, ui5
        /// </summary>
        public static Vector256<long> SignExtendWideningLowerAndShiftLeftEach128(Vector256<uint> value, byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvsllwil_hu_bu(uint8x32_t a, uint8_t ui3)
        ///   LASX: XVSLLWIL.HU.BU Xd.16H, Xj.32B, ui3
        /// </summary>
        public static Vector256<ushort> ZeroExtendWideningLowerAndShiftLeftEach128(Vector256<byte> value, byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvsllwil_hu_bu(int8x32_t a, uint8_t ui3)
        ///   LASX: XVSLLWIL.HU.BU Xd.16H, Xj.32B, ui3
        /// </summary>
        public static Vector256<short> ZeroExtendWideningLowerAndShiftLeftEach128(Vector256<sbyte> value, byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvsllwil_wu_hu(uint16x16_t a, uint8_t ui4)
        ///   LASX: XVSLLWIL.WU.HU Xd.8W, Xj.16H, ui4
        /// </summary>
        public static Vector256<uint> ZeroExtendWideningLowerAndShiftLeftEach128(Vector256<ushort> value, byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvsllwil_wu_hu(int16x16_t a, uint8_t ui4)
        ///   LASX: XVSLLWIL.WU.HU Xd.8W, Xj.16H, ui4
        /// </summary>
        public static Vector256<int> ZeroExtendWideningLowerAndShiftLeftEach128(Vector256<short> value, byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvsllwil_du_wu(uint32x8_t a, uint8_t ui5)
        ///   LASX: XVSLLWIL.DU.WU Xd.4D, Xj.8W, ui5
        /// </summary>
        public static Vector256<ulong> ZeroExtendWideningLowerAndShiftLeftEach128(Vector256<uint> value, byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvsllwil_du_wu(int32x8_t a, uint8_t ui5)
        ///   LASX: XVSLLWIL.DU.WU Xd.4D, Xj.8W, ui5
        /// </summary>
        public static Vector256<long> ZeroExtendWideningLowerAndShiftLeftEach128(Vector256<int> value, byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint128x2_t xvextl_qu_du(int64x4_t a)
        ///   LASX: XVEXTL.QU.DU Xd.2Q, Xj.D
        /// </summary>
        public static Vector256<ulong> ZeroExtendWideningLowerEach128(Vector256<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint128x2_t xvextl_qu_du(uint64x4_t a)
        ///   LASX: XVEXTL.QU.DU Xd.2Q, Xj.D
        /// </summary>
        public static Vector256<ulong> ZeroExtendWideningLowerEach128(Vector256<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t vext2xv_h_b(int8x16_t a)
        ///   LASX: VEXT2XV.H.B Xd.16H, Xj.16B
        /// </summary>
        public static Vector256<short> SignExtendWideningLower(Vector128<sbyte> value, byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t vext2xv_h_b(uint8x16_t a)
        ///   LASX: VEXT2XV.H.B Xd.16H, Xj.16B
        /// </summary>
        public static Vector256<short> SignExtendWideningLower(Vector128<byte> value, byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t vext2xv_w_b(int8x8_t a)
        ///   LASX: VEXT2XV.W.B Xd.8W, Xj.8B
        /// </summary>
        public static Vector256<int> SignExtendWideningLower(Vector64<sbyte> value, byte shift) { throw new PlatformNotSupportedException(); }
        //public static Vector256<int> SignExtendWideningLower(Vector128<sbyte> value, byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t vext2xv_w_b(uint8x8_t a)
        ///   LASX: VEXT2XV.W.B Xd.8W, Xj.8B
        /// </summary>
        public static Vector256<int> SignExtendWideningLower(Vector64<byte> value, byte shift) { throw new PlatformNotSupportedException(); }
        //public static Vector256<int> SignExtendWideningLower(Vector128<byte> value, byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t vext2xv_d_b(int8x4_t a)
        ///   LASX: VEXT2XV.D.B Xd.4D, Xj.4B
        /// </summary>
        public static Vector256<long> SignExtendWideningLower(Vector64<sbyte> value, byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t vext2xv_d_b(uint8x4_t a)
        ///   LASX: VEXT2XV.D.B Xd.4D, Xj.4B
        /// </summary>
        public static Vector256<long> SignExtendWideningLower(Vector64<byte> value, byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t vext2xv_w_h(int16x8_t a)
        ///   LASX: VEXT2XV.W.H Xd.8W, Xj.8H
        /// </summary>
        public static Vector256<int> SignExtendWideningLower(Vector128<short> value, byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t vext2xv_w_h(uint16x8_t a)
        ///   LASX: VEXT2XV.W.H Xd.8W, Xj.8H
        /// </summary>
        public static Vector256<int> SignExtendWideningLower(Vector128<ushort> value, byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t vext2xv_d_h(int16x4_t a)
        ///   LASX: VEXT2XV.D.H Xd.4D, Xj.4H
        /// </summary>
        public static Vector256<long> SignExtendWideningLower(Vector64<short> value, byte shift) { throw new PlatformNotSupportedException(); }
        //public static Vector256<long> SignExtendWideningLower(Vector128<short> value, byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t vext2xv_d_h(uint16x4_t a)
        ///   LASX: VEXT2XV.D.H Xd.4D, Xj.4H
        /// </summary>
        public static Vector256<long> SignExtendWideningLower(Vector64<ushort> value, byte shift) { throw new PlatformNotSupportedException(); }
        //public static Vector256<long> SignExtendWideningLower(Vector128<ushort> value, byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t vext2xv_d_w(int32x2_t a)
        ///   LASX: VEXT2XV.D.W Xd.4D, Xj.2W
        /// </summary>
        public static Vector256<long> SignExtendWideningLower(Vector128<int> value, byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t vext2xv_d_w(uint32x2_t a)
        ///   LASX: VEXT2XV.D.W Xd.4D, Xj.2W
        /// </summary>
        public static Vector256<long> SignExtendWideningLower(Vector128<uint> value, byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t vext2xv_hu_bu(uint8x16_t a)
        ///   LASX: VEXT2XV.HU.BU Xd.16H, Xj.16B
        /// </summary>
        public static Vector256<ushort> ZeroExtendWideningLower(Vector128<byte> value, byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t vext2xv_hu_bu(int8x16_t a)
        ///   LASX: VEXT2XV.HU.BU Xd.16H, Xj.16B
        /// </summary>
        public static Vector256<short> ZeroExtendWideningLower(Vector128<sbyte> value, byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t vext2xv_wu_bu(uint8x8_t a)
        ///   LASX: VEXT2XV.WU.BU Xd.8W, Xj.8B
        /// </summary>
        public static Vector256<uint> ZeroExtendWideningLower(Vector64<byte> value, byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t vext2xv_wu_bu(int8x8_t a)
        ///   LASX: VEXT2XV.WU.BU Xd.8W, Xj.8B
        /// </summary>
        public static Vector256<int> ZeroExtendWideningLower(Vector64<sbyte> value, byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t vext2xv_du_bu(uint8x4_t a)
        ///   LASX: VEXT2XV.DU.BU Xd.4D, Xj.4B
        /// </summary>
        public static Vector256<ulong> ZeroExtendWideningLower(Vector64<byte> value, byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t vext2xv_du_bu(int8x4_t a)
        ///   LASX: VEXT2XV.DU.BU Xd.4D, Xj.4B
        /// </summary>
        public static Vector256<long> ZeroExtendWideningLower(Vector64<sbyte> value, byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t vext2xv_wu_hu(uint16x8_t a)
        ///   LASX: VEXT2XV.WU.HU Xd.8W, Xj.8H
        /// </summary>
        public static Vector256<uint> ZeroExtendWideningLower(Vector128<ushort> value, byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t vext2xv_wu_hu(int16x8_t a)
        ///   LASX: VEXT2XV.WU.HU Xd.8W, Xj.8H
        /// </summary>
        public static Vector256<int> ZeroExtendWideningLower(Vector128<short> value, byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t vext2xv_du_hu(uint16x4_t a)
        ///   LASX: VEXT2XV.DU.HU Xd.4D, Xj.4H
        /// </summary>
        public static Vector256<ulong> ZeroExtendWideningLower(Vector64<ushort> value, byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t vext2xv_du_hu(int16x4_t a)
        ///   LASX: VEXT2XV.DU.HU Xd.4D, Xj.4H
        /// </summary>
        public static Vector256<long> ZeroExtendWideningLower(Vector64<short> value, byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t vext2xv_du_wu(uint32x4_t a)
        ///   LASX: VEXT2XV.DU.WU Xd.4D, Xj.4W
        /// </summary>
        public static Vector256<ulong> ZeroExtendWideningLower(Vector128<uint> value, byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t vext2xv_du_wu(int32x4_t a)
        ///   LASX: VEXT2XV.DU.WU Xd.4D, Xj.4W
        /// </summary>
        public static Vector256<long> ZeroExtendWideningLower(Vector128<int> value, byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvexth_h_b(int8x32_t a)
        ///   LASX: XVEXTH.H.B Xd.16H, Xj.32B
        /// </summary>
        public static Vector256<short> SignExtendWideningUpperEach128(Vector256<sbyte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvexth_w_h(int16x16_t a)
        ///   LASX: XVEXTH.W.H Xd.8W, Xj.16H
        /// </summary>
        public static Vector256<int> SignExtendWideningUpperEach128(Vector256<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvexth_d_w(int32x8_t a)
        ///   LASX: XVEXTH.D.W Xd.4D, Xj.8W
        /// </summary>
        public static Vector256<long> SignExtendWideningUpperEach128(Vector256<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int128x2_t xvexth_d_w(int64x4_t a)
        ///   LASX: XVEXTH.Q.D Xd.2Q, Xj.4D
        /// </summary>
        public static Vector256<long> SignExtendWideningUpperEach128(Vector256<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvexth_HU_BU(int8x32_t a)
        ///   LASX: XVEXTH.HU.BU Xd.16H, Xj.32B
        /// </summary>
        public static Vector256<short> ZeroExtendWideningUpperEach128(Vector256<sbyte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvexth_HU_BU(uint8x32_t a)
        ///   LASX: XVEXTH.HU.BU Xd.16H, Xj.32B
        /// </summary>
        public static Vector256<ushort> ZeroExtendWideningUpperEach128(Vector256<byte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvexth_WU_HU(int16x16_t a)
        ///   LASX: XVEXTH.WU.HU Xd.8W, Xj.16H
        /// </summary>
        public static Vector256<int> ZeroExtendWideningUpperEach128(Vector256<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvexth_WU_HU(uint16x16_t a)
        ///   LASX: XVEXTH.WU.HU Xd.8W, Xj.16H
        /// </summary>
        public static Vector256<uint> ZeroExtendWideningUpperEach128(Vector256<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvexth_DU_WU(uint32x8_t a)
        ///   LASX: XVEXTH.DU.WU Xd.4D, Xj.8W
        /// </summary>
        public static Vector256<long> ZeroExtendWideningUpperEach128(Vector256<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvexth_DU_WU(uint32x8_t a)
        ///   LASX: XVEXTH.DU.WU Xd.4D, Xj.8W
        /// </summary>
        public static Vector256<ulong> ZeroExtendWideningUpperEach128(Vector256<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int128x2_t xvexth_DU_WU(int64x4_t a)
        ///   LASX: XVEXTH.QU.DU Xd.2Q, Xj.4D
        /// </summary>
        public static Vector256<ulong> ZeroExtendWideningUpperEach128(Vector256<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint128x2_t xvexth_DU_WU(uint64x4_t a)
        ///   LASX: XVEXTH.QU.DU Xd.2Q, Xj.4D
        /// </summary>
        public static Vector256<ulong> ZeroExtendWideningUpperEach128(Vector256<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvand_v(int8x32_t a, int8x32_t b)
        ///   LASX: XVAND.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> And(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvand_v(uint8x32_t a, uint8x32_t b)
        ///   LASX: XVAND.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> And(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvand_v(int16x16_t a, int16x16_t b)
        ///   LASX: XVAND.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<short> And(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvand_v(uint16x16_t a, uint16x16_t b)
        ///   LASX: XVAND.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<ushort> And(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvand_v(int32x8_t a, int32x8_t b)
        ///   LASX: XVAND.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<int> And(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvand_v(uint32x8_t a, uint32x8_t b)
        ///   LASX: XVAND.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<uint> And(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvand_v(int64x4_t a, int64x4_t b)
        ///   LASX: XVAND.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<long> And(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvand_v(uint64x4_t a, uint64x4_t b)
        ///   LASX: XVAND.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<ulong> And(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x8_t xvand_v(float32x8_t a, float32x8_t b)
        ///   LASX: XVAND.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<float> And(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x4_t xvand_v(float64x4_t a, float64x4_t b)
        ///   LASX: XVAND.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<double> And(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvandn_v(int8x32_t a, int8x32_t b)
        ///   LASX: XVANDN.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> AndNot(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvandn_v(uint8x32_t a, uint8x32_t b)
        ///   LASX: XVANDN.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> AndNot(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvandn_v(int16x16_t a, int16x16_t b)
        ///   LASX: XVANDN.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<short> AndNot(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvandn_v(uint16x16_t a, uint16x16_t b)
        ///   LASX: XVANDN.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<ushort> AndNot(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvandn_v(int32x8_t a, int32x8_t b)
        ///   LASX: XVANDN.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<int> AndNot(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvandn_v(uint32x8_t a, uint32x8_t b)
        ///   LASX: XVANDN.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<uint> AndNot(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvandn_v(int64x4_t a, int64x4_t b)
        ///   LASX: XVANDN.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<long> AndNot(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvandn_v(uint64x4_t a, uint64x4_t b)
        ///   LASX: XVANDN.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<ulong> AndNot(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x8_t xvandn_v(float32x8_t a, float32x8_t b)
        ///   LASX: XVANDN.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<float> AndNot(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x4_t xvandn_v(float64x4_t a, float64x4_t b)
        ///   LASX: XVANDN.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<double> AndNot(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvor_v(int8x32_t a, int8x32_t b)
        ///   LASX: XVOR.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> Or(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvor_v(uint8x32_t a, uint8x32_t b)
        ///   LASX: XVOR.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> Or(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvor_v(int16x16_t a, int16x16_t b)
        ///   LASX: XVOR.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<short> Or(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvor_v(uint16x16_t a, uint16x16_t b)
        ///   LASX: XVOR.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<ushort> Or(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvor_v(int32x8_t a, int32x8_t b)
        ///   LASX: XVOR.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<int> Or(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvor_v(uint32x8_t a, uint32x8_t b)
        ///   LASX: XVOR.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<uint> Or(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvor_v(int64x4_t a, int64x4_t b)
        ///   LASX: XVOR.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<long> Or(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvor_v(uint64x4_t a, uint64x4_t b)
        ///   LASX: XVOR.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<ulong> Or(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x8_t xvor_v(float32x8_t a, float32x8_t b)
        ///   LASX: XVOR.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<float> Or(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x4_t xvor_v(float64x4_t a, float64x4_t b)
        ///   LASX: XVOR.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<double> Or(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvnori_b(uint8x32_t a)
        ///   LASX: XVNORI.B Vd.32B, Vj.32B, 0
        /// </summary>
        public static Vector256<byte> Not(Vector256<byte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x4_t xvnori_b(float64x4_t a)
        ///   LASX: XVNORI.B Vd.32B, Vj.32B, 0
        /// </summary>
        public static Vector256<double> Not(Vector256<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvnori_b(int16x16_t a)
        ///   LASX: XVNORI.B Vd.32B, Vj.32B, 0
        /// </summary>
        public static Vector256<short> Not(Vector256<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvnori_b(int32x8_t a)
        ///   LASX: XVNORI.B Vd.32B, Vj.32B, 0
        /// </summary>
        public static Vector256<int> Not(Vector256<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvnori_b(int64x4_t a)
        ///   LASX: XVNORI.B Vd.32B, Vj.32B, 0
        /// </summary>
        public static Vector256<long> Not(Vector256<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvnori_b(int8x32_t a)
        ///   LASX: XVNORI.B Vd.32B, Vj.32B, 0
        /// </summary>
        public static Vector256<sbyte> Not(Vector256<sbyte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x8_t xvnori_b(float32x8_t a)
        ///   LASX: XVNORI.B Vd.32B, Vj.32B, 0
        /// </summary>
        public static Vector256<float> Not(Vector256<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvnori_b(uint16x16_t a)
        ///   LASX: XVNORI.B Vd.32B, Vj.32B, 0
        /// </summary>
        public static Vector256<ushort> Not(Vector256<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvnori_b(uint32x8_t a)
        ///   LASX: XVNORI.B Vd.32B, Vj.32B, 0
        /// </summary>
        public static Vector256<uint> Not(Vector256<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvnori_b(uint64x4_t a)
        ///   LASX: XVNORI.B Vd.32B, Vj.32B, 0
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector256<ulong> Not(Vector256<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvnor_v(int8x32_t a, int8x32_t b)
        ///   LASX: XVNOR.V Vd.32B, Vj.32B, Vk.32B
        /// </summary>
        public static Vector256<sbyte> NotOr(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvnor_v(uint8x32_t a, uint8x32_t b)
        ///   LASX: XVNOR.V Vd.32B, Vj.32B, Vk.32B
        /// </summary>
        public static Vector256<byte> NotOr(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvnor_v(int16x16_t a, int16x16_t b)
        ///   LASX: XVNOR.V Vd.16H, Vj.16H, Vk.16H
        /// </summary>
        public static Vector256<short> NotOr(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvnor_v(uint16x16_t a, uint16x16_t b)
        ///   LASX: XVNOR.V Vd.16H, Vj.16H, Vk.16H
        /// </summary>
        public static Vector256<ushort> NotOr(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvnor_v(int32x8_t a, int32x8_t b)
        ///   LASX: XVNOR.V Vd.8W, Vj.8W, Vk.8W
        /// </summary>
        public static Vector256<int> NotOr(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvnor_v(uint32x8_t a, uint32x8_t b)
        ///   LASX: XVNOR.V Vd.8W, Vj.8W, Vk.8W
        /// </summary>
        public static Vector256<uint> NotOr(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvnor_v(int64x4_t a, int64x4_t b)
        ///   LASX: XVNOR.V Vd.4D, Vj.4D, Vk.4D
        /// </summary>
        public static Vector256<long> NotOr(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvnor_v(uint64x4_t a, uint64x4_t b)
        ///   LASX: XVNOR.V Vd.4D, Vj.4D, Vk.4D
        /// </summary>
        public static Vector256<ulong> NotOr(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x8_t xvnor_v(float32x8_t a, float32x8_t b)
        ///   LASX: XVNOR.V Vd.8S, Vj.8S, Vk.8S
        /// </summary>
        public static Vector256<float> NotOr(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x4_t xvnor_v(float64x4_t a, float64x4_t b)
        ///   LASX: XVNOR.V Vd.4D, Vj.4D, Vk.4D
        /// </summary>
        public static Vector256<double> NotOr(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x8_t xvorn(uint8x8_t a, uint8x8_t b)
        /// </summary>
        public static Vector64<byte> OrNot(Vector64<byte> left, Vector64<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x1_t xvorn(float64x1_t a, float64x1_t b)
        ///   LASX: XVORN.V Vd.32B, Vj.32B, Vk.32B
        /// </summary>
        public static Vector64<double> OrNot(Vector64<double> left, Vector64<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x4_t xvorn(int16x4_t a, int16x4_t b)
        ///   LASX: XVORN.V Vd.32B, Vj.32B, Vk.32B
        /// </summary>
        public static Vector64<short> OrNot(Vector64<short> left, Vector64<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x2_t xvorn(int32x2_t a, int32x2_t b)
        ///   LASX: XVORN.V Vd.32B, Vj.32B, Vk.32B
        /// </summary>
        public static Vector64<int> OrNot(Vector64<int> left, Vector64<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x1_t xvorn(int64x1_t a, int64x1_t b)
        ///   LASX: XVORN.V Vd.32B, Vj.32B, Vk.32B
        /// </summary>
        public static Vector64<long> OrNot(Vector64<long> left, Vector64<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x8_t xvorn(int8x8_t a, int8x8_t b)
        ///   LASX: XVORN.V Vd.32B, Vj.32B, Vk.32B
        /// </summary>
        public static Vector64<sbyte> OrNot(Vector64<sbyte> left, Vector64<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x2_t xvorn(float32x2_t a, float32x2_t b)
        ///   LASX: XVORN.V Vd.32B, Vj.32B, Vk.32B
        /// </summary>
        public static Vector64<float> OrNot(Vector64<float> left, Vector64<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x4_t xvorn(uint16x4_t a, uint16x4_t b)
        ///   LASX: XVORN.V Vd.32B, Vj.32B, Vk.32B
        /// </summary>
        public static Vector64<ushort> OrNot(Vector64<ushort> left, Vector64<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x2_t xvorn(uint32x2_t a, uint32x2_t b)
        ///   LASX: XVORN.V Vd.32B, Vj.32B, Vk.32B
        /// </summary>
        public static Vector64<uint> OrNot(Vector64<uint> left, Vector64<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x1_t xvorn(uint64x1_t a, uint64x1_t b)
        ///   LASX: XVORN.V Vd.32B, Vj.32B, Vk.32B
        /// </summary>
        public static Vector64<ulong> OrNot(Vector64<ulong> left, Vector64<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvorn_v(int8x32_t a, int8x32_t b)
        ///   LASX: XVORN.V Vd.32B, Vj.32B, Vk.32B
        /// </summary>
        public static Vector256<sbyte> OrNot(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvorn_v(uint8x32_t a, uint8x32_t b)
        ///   LASX: XVORN.V Vd.32B, Vj.32B, Vk.32B
        /// </summary>
        public static Vector256<byte> OrNot(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvor_v(int16x16_t a, int16x16_t b)
        ///   LASX: XVORN.V Vd.16H, Vj.16H, Vk.16H
        /// </summary>
        public static Vector256<short> OrNot(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvor_v(uint16x16_t a, uint16x16_t b)
        ///   LASX: XVORN.V Vd.16H, Vj.16H, Vk.16H
        /// </summary>
        public static Vector256<ushort> OrNot(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvorn_v(int32x8_t a, int32x8_t b)
        ///   LASX: XVORN.V Vd.8W, Vj.8W, Vk.8W
        /// </summary>
        public static Vector256<int> OrNot(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvorn_v(uint32x8_t a, uint32x8_t b)
        ///   LASX: XVORN.V Vd.8W, Vj.8W, Vk.8W
        /// </summary>
        public static Vector256<uint> OrNot(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvorn_v(int64x4_t a, int64x4_t b)
        ///   LASX: XVORN.V Vd.4D, Vj.4D, Vk.4D
        /// </summary>
        public static Vector256<long> OrNot(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvorn_v(uint64x4_t a, uint64x4_t b)
        ///   LASX: XVORN.V Vd.4D, Vj.4D, Vk.4D
        /// </summary>
        public static Vector256<ulong> OrNot(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x8_t xvorn_v(float32x8_t a, float32x8_t b)
        ///   LASX: XVORN.V Vd.8S, Vj.8S, Vk.8S
        /// </summary>
        public static Vector256<float> OrNot(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x4_t xvorn_v(float64x4_t a, float64x4_t b)
        ///   LASX: XVORN.V Vd.4D, Vj.4D, Vk.4D
        /// </summary>
        public static Vector256<double> OrNot(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvxor_v(int8x32_t a, int8x32_t b)
        ///   LASX: XVXOR.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> Xor(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvxor_v(uint8x32_t a, uint8x32_t b)
        ///   LASX: XVXOR.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> Xor(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvxor_v(int16x16_t a, int16x16_t b)
        ///   LASX: XVXOR.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<short> Xor(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvxor_v(uint16x16_t a, uint16x16_t b)
        ///   LASX: XVXOR.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<ushort> Xor(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvxor_v(int32x8_t a, int32x8_t b)
        ///   LASX: XVXOR.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<int> Xor(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvxor_v(uint32x8_t a, uint32x8_t b)
        ///   LASX: XVXOR.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<uint> Xor(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvxor_v(int64x4_t a, int64x4_t b)
        ///   LASX: XVXOR.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<long> Xor(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvxor_v(uint64x4_t a, uint64x4_t b)
        ///   LASX: XVXOR.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<ulong> Xor(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x8_t xvxor_v(float32x8_t a, float32x8_t b)
        ///   LASX: XVXOR.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<float> Xor(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x4_t xvxor_v(float64x4_t a, float64x4_t b)
        ///   LASX: XVXOR.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<double> Xor(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvslli_b(int8x32_t a, const int n)
        ///   LASX: XVSLLI.B Xd.32B, Xj.32B, ui3
        /// </summary>
        public static Vector256<sbyte> ShiftLeftLogical(Vector256<sbyte> value, const byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvslli_b(uint8x32_t a, const int n)
        ///   LASX: XVSLLI.B Xd.32B, Xj.32B, ui3
        /// </summary>
        public static Vector256<byte> ShiftLeftLogical(Vector256<byte> value, const byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvslli_h(int16x16_t a, const int n)
        ///   LASX: XVSLLI.H Xd.16H, Xj.16H, ui4
        /// </summary>
        public static Vector256<short> ShiftLeftLogical(Vector256<short> value, const byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvslli_h(uint16x16_t a, const int n)
        ///   LASX: XVSLLI.H Xd.16H, Xj.16H, ui4
        /// </summary>
        public static Vector256<ushort> ShiftLeftLogical(Vector256<ushort> value, const byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvslli_w(uint32x8_t a, const int n)
        ///   LASX: XVSLLI.W Xd.8W, Xj.8W, ui5
        /// </summary>
        public static Vector256<int> ShiftLeftLogical(Vector256<int> value, const byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvslli_w(uint32x8_t a, const int n)
        ///   LASX: XVSLLI.W Xd.8W, Xj.8W, ui5
        /// </summary>
        public static Vector256<uint> ShiftLeftLogical(Vector256<uint> value, const byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvslli_d(int64x4_t a, const int n)
        ///   LASX: XVSLLI.D Xd.4D, Xj.4D, ui6
        /// </summary>
        public static Vector256<long> ShiftLeftLogical(Vector256<long> value, const byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvslli_d(uint64x4_t a, const int n)
        ///   LASX: XVSLLI.D Xd.4D, Xj.4D, ui6
        /// </summary>
        public static Vector256<ulong> ShiftLeftLogical(Vector256<ulong> value, const byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvbsll_v(int8x32_t a, const int shift)
        ///   LASX: XVBSLL.V Xd.32B, Xj.32B, ui4
        /// </summary>
        public static Vector256<sbyte> ShiftLeftLogicalByByteEach128(Vector256<sbyte> value, [ConstantExpected(Max = (byte)(15))] byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvbsll_v(uint8x32_t a, const int shift)
        ///   LASX: XVBSLL.V Xd.32B, Xj.32B, ui4
        /// </summary>
        public static Vector256<byte> ShiftLeftLogicalByByteEach128(Vector256<byte> value, [ConstantExpected(Max = (byte)(15))] byte shift) { throw new PlatformNotSupportedException(); }

        /// int8x32_t xvsll_b(int8x32_t a, int8x32_t b)
        ///   LASX: XVSLL.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> ShiftLeftLogical(Vector256<sbyte> value, Vector256<sbyte> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvsll_b(uint8x32_t a, uint8x32_t b)
        ///   LASX: XVSLL.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> ShiftLeftLogical(Vector256<byte> value, Vector256<byte> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16 xvsll_h(int16x16_t value, int16x16_t shift)
        ///   LASX: XVSLL.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> ShiftLeftLogical(Vector256<short> value, Vector256<short> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16 xvsll_h(uint16x16_t value, uint16x16_t shift)
        ///   LASX: XVSLL.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> ShiftLeftLogical(Vector256<ushort> value, Vector256<ushort> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8 xvsll_w(int32x8_t value, int32x8_t shift)
        ///   LASX: XVSLL.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> ShiftLeftLogical(Vector256<int> value, Vector256<int> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8 xvsll_w(uint32x8_t value, uint32x8_t shift)
        ///   LASX: XVSLL.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> ShiftLeftLogical(Vector256<uint> value, Vector256<uint> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4 xvsll_d(int64x4_t value, int64x4_t shift)
        ///   LASX: XVSLL.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> ShiftLeftLogical(Vector256<long> value, Vector256<long> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4 xvsll_d(uint64x4_t value, uint64x4_t shift)
        ///   LASX: XVSLL.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> ShiftLeftLogical(Vector256<ulong> value, Vector256<ulong> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvsrli_b(uint8x32_t a, const int n)
        ///   LASX: XVSRLI.B Xd.32B, Xj.32B, ui3
        /// </summary>
        public static Vector256<sbyte> ShiftRightLogical(Vector256<sbyte> value, const byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvsrli_b(uint8x32_t a, const int n)
        ///   LASX: XVSRLI.B Xd.32B, Xj.32B, ui3
        /// </summary>
        public static Vector256<byte> ShiftRightLogical(Vector256<byte> value, const byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvsrli_h(uint16x16_t a, const int n)
        ///   LASX: XVSRLI.H Xd.16H, Xj.16H, ui4
        /// </summary>
        public static Vector256<short> ShiftRightLogical(Vector256<short> value, const byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvsrli_h(uint16x16_t a, const int n)
        ///   LASX: XVSRLI.H Xd.16H, Xj.16H, ui4
        /// </summary>
        public static Vector256<ushort> ShiftRightLogical(Vector256<ushort> value, const byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvsrli_w(uint32x8_t a, const int n)
        ///   LASX: XVSRLI.W Xd.8W, Xj.8W, ui5
        /// </summary>
        public static Vector256<int> ShiftRightLogical(Vector256<int> value, const byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvsrli_w(uint32x8_t a, const int n)
        ///   LASX: XVSRLI.W Xd.8W, Xj.8W, ui5
        /// </summary>
        public static Vector256<uint> ShiftRightLogical(Vector256<uint> value, const byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvsrli_d(uint64x4_t a, const int n)
        ///   LASX: XVSRLI.D Xd.4D, Xj.4D, ui6
        /// </summary>
        public static Vector256<long> ShiftRightLogical(Vector256<long> value, const byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvsrli_d(uint64x4_t a, const int n)
        ///   LASX: XVSRLI.D Xd.4D, Xj.4D, ui6
        /// </summary>
        public static Vector256<ulong> ShiftRightLogical(Vector256<ulong> value, const byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvbsrl_v(int8x32_t a, const int shift)
        ///   LASX: XVBSRL.V Xd.32B, Xj.32B, ui4
        /// </summary>
        public static Vector256<sbyte> ShiftRightLogicalByByteEach128(Vector256<sbyte> value, [ConstantExpected(Max = (byte)(15))] byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvbsrl_v(uint8x32_t a, const int shift)
        ///   LASX: XVBSRL.V Xd.32B, Xj.32B, ui4
        /// </summary>
        public static Vector256<byte> ShiftRightLogicalByByteEach128(Vector256<byte> value, [ConstantExpected(Max = (byte)(15))] byte shift) { throw new PlatformNotSupportedException(); }

        /// int8x32_t xvsrl_b(int8x32_t a, int8x32_t b)
        ///   LASX: XVSRL.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> ShiftRightLogical(Vector256<sbyte> value, Vector256<sbyte> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvsrl_b(uint8x32_t a, uint8x32_t b)
        ///   LASX: XVSRL.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> ShiftRightLogical(Vector256<byte> value, Vector256<byte> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16 xvsrl_h(int16x16_t value, int16x16_t shift)
        ///   LASX: XVSRL.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> ShiftRightLogical(Vector256<short> value, Vector256<short> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16 xvsrl_h(uint16x16_t value, uint16x16_t shift)
        ///   LASX: XVSRL.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> ShiftRightLogical(Vector256<ushort> value, Vector256<ushort> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8 xvsrl_w(int32x8_t value, int32x8_t shift)
        ///   LASX: XVSRL.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> ShiftRightLogical(Vector256<int> value, Vector256<int> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8 xvsrl_w(uint32x8_t value, uint32x8_t shift)
        ///   LASX: XVSRL.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> ShiftRightLogical(Vector256<uint> value, Vector256<uint> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4 xvsrl_d(int64x4_t value, int64x4_t shift)
        ///   LASX: XVSRL.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> ShiftRightLogical(Vector256<long> value, Vector256<long> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4 xvsrl_d(uint64x4_t value, uint64x4_t shift)
        ///   LASX: XVSRL.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> ShiftRightLogical(Vector256<ulong> value, Vector256<ulong> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvsrlri_b(uint8x32_t a, const int n)
        ///   LASX: XVSRLRI.B Xd.32B, Xj.32B, ui3
        /// </summary>
        public static Vector256<sbyte> ShiftRightLogicalRounded(Vector256<sbyte> value, const byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvsrlri_b(uint8x32_t a, const int n)
        ///   LASX: XVSRLRI.B Xd.32B, Xj.32B, ui3
        /// </summary>
        public static Vector256<byte> ShiftRightLogicalRounded(Vector256<byte> value, const byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvsrlri_h(uint16x16_t a, const int n)
        ///   LASX: XVSRLRI.H Xd.16H, Xj.16H, ui4
        /// </summary>
        public static Vector256<short> ShiftRightLogicalRounded(Vector256<short> value, const byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvsrlri_h(uint16x16_t a, const int n)
        ///   LASX: XVSRLRI.H Xd.16H, Xj.16H, ui4
        /// </summary>
        public static Vector256<ushort> ShiftRightLogicalRounded(Vector256<ushort> value, const byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvsrlri_w(uint32x8_t a, const int n)
        ///   LASX: XVSRLRI.W Xd.8W, Xj.8W, ui5
        /// </summary>
        public static Vector256<int> ShiftRightLogicalRounded(Vector256<int> value, const byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvsrlri_w(uint32x8_t a, const int n)
        ///   LASX: XVSRLRI.W Xd.8W, Xj.8W, ui5
        /// </summary>
        public static Vector256<uint> ShiftRightLogicalRounded(Vector256<uint> value, const byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvsrlri_d(uint64x4_t a, const int n)
        ///   LASX: XVSRLRI.D Xd.4D, Xj.4D, ui6
        /// </summary>
        public static Vector256<long> ShiftRightLogicalRounded(Vector256<long> value, const byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvsrlri_d(uint64x4_t a, const int n)
        ///   LASX: XVSRLRI.D Xd.4D, Xj.4D, ui6
        /// </summary>
        public static Vector256<ulong> ShiftRightLogicalRounded(Vector256<ulong> value, const byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvsrlr_b(int8x32_t a, int8x32_t b)
        ///   LASX: XVSRLR.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> ShiftRightLogicalRounded(Vector256<sbyte> value, Vector256<sbyte> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvsrlr_b(uint8x32_t a, uint8x32_t b)
        ///   LASX: XVSRLR.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> ShiftRightLogicalRounded(Vector256<byte> value, Vector256<byte> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16 xvsrlr_h(int16x16_t value, int16x16_t shift)
        ///   LASX: XVSRLR.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> ShiftRightLogicalRounded(Vector256<short> value, Vector256<short> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16 xvsrlr_h(uint16x16_t value, uint16x16_t shift)
        ///   LASX: XVSRLR.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> ShiftRightLogicalRounded(Vector256<ushort> value, Vector256<ushort> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8 xvsrlr_w(int32x8_t value, int32x8_t shift)
        ///   LASX: XVSRLR.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> ShiftRightLogicalRounded(Vector256<int> value, Vector256<int> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8 xvsrlr_w(uint32x8_t value, uint32x8_t shift)
        ///   LASX: XVSRLR.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> ShiftRightLogicalRounded(Vector256<uint> value, Vector256<uint> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4 xvsrlr_d(int64x4_t value, int64x4_t shift)
        ///   LASX: XVSRLR.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> ShiftRightLogicalRounded(Vector256<long> value, Vector256<long> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4 xvsrlr_d(uint64x4_t value, uint64x4_t shift)
        ///   LASX: XVSRLR.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> ShiftRightLogicalRounded(Vector256<ulong> value, Vector256<ulong> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvsrlrni_b_h(uint16x16_t left, uint16x16_t right, const int n)
        ///   LASX: XVSRLRNI.B.H Xd, Xj, ui4    ///NOTE: The Vd is both input and output, so the left shoule be ref type!!!
        /// </summary>
        public static Vector256<byte> ShiftRightLogicalRoundedNarrowingLowerEach128(Vector256<ushort> left, Vector256<ushort> right, [ConstantExpected(Max = (byte)(15))] byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvsrlrni_b_h(int16x16_t left, int16x16_t right, const int n)
        ///   LASX: XVSRLRNI.B.H Xd, Xj, ui4
        /// </summary>
        public static Vector256<sbyte> ShiftRightLogicalRoundedNarrowingLowerEach128(Vector256<short> left, Vector256<short> right, [ConstantExpected(Max = (byte)(15))] byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvsrlrni_h_w(int32x8_t left, int32x8_t right, const int n)
        ///   LASX: XVSRLRNI.H.W Xd, Xj, ui5
        /// </summary>
        public static Vector256<short> ShiftRightLogicalRoundedNarrowingLowerEach128(Vector256<int> left, Vector256<int> right, [ConstantExpected(Max = (byte)(31))] byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvsrlrni_h_w(uint32x8_t left, uint32x8_t right, const int n)
        ///   LASX: XVSRLRNI.H.W Xd, Xj, ui5
        /// </summary>
        public static Vector256<ushort> ShiftRightLogicalRoundedNarrowingLowerEach128(Vector256<uint> left, Vector256<uint> right, [ConstantExpected(Max = (byte)(31))] byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvsrlrni_w_d(int64x4_t left, int64x4_t right, const int n)
        ///   LASX: XVSRLRNI.W.D Xd, Xj, ui6
        /// </summary>
        public static Vector256<int> ShiftRightLogicalRoundedNarrowingLowerEach128(Vector256<long> left, Vector256<long> right, [ConstantExpected(Max = (byte)(63))] byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvsrlrni_w_d(uint64x4_t left, uint64x4_t right, const int n)
        ///   LASX: XVSRLRNI.W.D Xd, Xj, ui6
        /// </summary>
        public static Vector256<uint> ShiftRightLogicalRoundedNarrowingLowerEach128(Vector256<ulong> left, Vector256<ulong> right, [ConstantExpected(Max = (byte)(63))] byte shift) { throw new PlatformNotSupportedException(); }

        ///// <summary>
        ///// int64x4_t xvsrlrni_d_q(int128x2_t left, int128x2_t right, const int n)
        /////   LASX: XVSRLRNI.D.Q Xd, Xj, ui7
        ///// </summary>
        //public static Vector256<long> ShiftRightLogicalRoundedNarrowingLowerEach128(Vector256<longlong> left, Vector256<longlong> right, [ConstantExpected(Max = (byte)(127))] byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x16_t xvsrlrn_b_h(int16x16_t value, int16x16_t shift)
        ///   LASX: XVSRLRN.B.H Xd.16B, Xj.16H, Xk.16H
        /// </summary>
        public static Vector128<sbyte> ShiftRightLogicalRoundedNarrowingLowerEach128(Vector256<short> value, Vector256<short> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x16_t xvsrlrn_b_h(uint16x16_t value, uint16x16_t shift)
        ///   LASX: XVSRLRN.B.H Xd.16B, Xj.16H, Xk.16H
        /// </summary>
        public static Vector128<byte> ShiftRightLogicalRoundedNarrowingLowerEach128(Vector256<ushort> value, Vector256<ushort> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x8_t xvsrlrn_h_w(int32x8_t value, int32x8_t shift)
        ///   LASX: XVSRLRN.H.W Xd.8H, Xj.8W, Xk.8W
        /// </summary>
        public static Vector128<short> ShiftRightLogicalRoundedNarrowingLowerEach128(Vector256<int> value, Vector256<int> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x8_t xvsrlrn_h_w(uint32x8_t value, uint32x8_t shift)
        ///   LASX: XVSRLRN.H.W Xd.8H, Xj.8W, Xk.8W
        /// </summary>
        public static Vector128<ushort> ShiftRightLogicalRoundedNarrowingLowerEach128(Vector256<uint> value, Vector256<uint> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x4_t xvsrlrn_w_d(int64x4_t value, int64x4_t shift)
        ///   LASX: XVSRLRN.W.D Xd.4W, Xj.4D, Xk.4D
        /// </summary>
        public static Vector128<int> ShiftRightLogicalRoundedNarrowingLowerEach128(Vector256<long> value, Vector256<long> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x4_t xvsrlrn_w_d(uint64x4_t value, uint64x4_t shift)
        ///   LASX: XVSRLRN.W.D Xd.4W, Xj.4D, Xk.4D
        /// </summary>
        public static Vector128<uint> ShiftRightLogicalRoundedNarrowingLowerEach128(Vector256<ulong> value, Vector256<ulong> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x16_t xvsrarni_b_h(int16x16_t left, int16x16_t right, const int n)
        ///   LASX: XVSRARNI.B.H Xd, Xj, ui4    ///NOTE: The Vd is both input and output, so the left shoule be ref type!!!
        /// </summary>
        public static Vector256<sbyte> ShiftRightArithmeticRoundedNarrowingLowerEach128(Vector256<short> left, Vector256<short> right, [ConstantExpected(Max = (byte)(15))] byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvsrarni_h_w(int32x8_t left, int32x8_t right, const int n)
        ///   LASX: XVSRARNI.H.W Xd, Xj, ui5
        /// </summary>
        public static Vector256<short> ShiftRightArithmeticRoundedNarrowingLowerEach128(Vector256<int> left, Vector256<int> right, [ConstantExpected(Max = (byte)(31))] byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvsrarni_w_d(int64x4_t left, int64x4_t right, const int n)
        ///   LASX: XVSRARNI.W.D Xd, Xj, ui6
        /// </summary>
        public static Vector256<int> ShiftRightArithmeticRoundedNarrowingLowerEach128(Vector256<long> left, Vector256<long> right, [ConstantExpected(Max = (byte)(63))] byte shift) { throw new PlatformNotSupportedException(); }

        ///// <summary>
        ///// int64x4_t xvsrarni_d_q(int128x2_t left, int128x2_t right, const int n)
        /////   LASX: XVSRARNI.D.Q Xd, Xj, ui7
        ///// </summary>
        //public static Vector256<long> ShiftRightArithmeticRoundedNarrowingLowerEach128(Vector256<longlong> left, Vector256<longlong> right, [ConstantExpected(Max = (byte)(127))] byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x16_t xvsrarn_b_h(int16x16_t value, int16x16_t shift)
        ///   LASX: XVSRARN.B.H Xd.16B, Xj.16H, Xk.16H
        /// </summary>
        public static Vector128<sbyte> ShiftRightArithmeticRoundedNarrowingLowerEach128(Vector256<short> value, Vector256<short> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x8_t xvsrarn_h_w(int32x8_t value, int32x8_t shift)
        ///   LASX: XVSRARN.H.W Xd.8H, Xj.8W, Xk.8W
        /// </summary>
        public static Vector128<short> ShiftRightArithmeticRoundedNarrowingLowerEach128(Vector256<int> value, Vector256<int> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x8_t xvsrarn_h_w(uint32x8_t value, uint32x8_t shift)
        ///   LASX: XVSRARN.H.W Xd.8H, Xj.8W, Xk.8W
        /// </summary>
        public static Vector128<ushort> ShiftRightArithmeticRoundedNarrowingLowerEach128(Vector256<uint> value, Vector256<uint> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x4_t xvsrarn_w_d(int64x4_t value, int64x4_t shift)
        ///   LASX: XVSRARN.W.D Xd.4W, Xj.4D, Xk.4D
        /// </summary>
        public static Vector128<int> ShiftRightArithmeticRoundedNarrowingLowerEach128(Vector256<long> value, Vector256<long> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvsrai_b(int8x32_t a, const int n)
        ///   LASX: XVSRAI.B Xd.32B, Xj.32B, ui3
        /// </summary>
        public static Vector256<sbyte> ShiftRightArithmetic(Vector256<sbyte> value, const byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvsrai_h(int16x16_t a, const int n)
        ///   LASX: XVSRAI.H Xd.16H, Xj.16H, ui4
        /// </summary>
        public static Vector256<short> ShiftRightArithmetic(Vector256<short> value, const byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvsrai_w(int32x8_t a, const int n)
        ///   LASX: XVSRAI.W Xd.8W, Xj.8W, ui5
        /// </summary>
        public static Vector256<int> ShiftRightArithmetic(Vector256<int> value, const byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvsrai_d(int64x4_t a, const int n)
        ///   LASX: XVSRAI.D Xd.4D, Xj.4D, ui6
        /// </summary>
        public static Vector256<long> ShiftRightArithmetic(Vector256<long> value, const byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_txvsra_b(int8x32_t a, int8x32_t b)
        ///   LASX:  XVSRA.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> ShiftRightArithmetic(Vector256<sbyte> value, Vector256<sbyte> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16xvsra_h(int16x16_t value, int16x16_t shift)
        ///   LASX:  XVSRA.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> ShiftRightArithmetic(Vector256<short> value, Vector256<short> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8xvsra_w(int32x8_t value, int32x8_t shift)
        ///   LASX:  XVSRA.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> ShiftRightArithmetic(Vector256<int> value, Vector256<int> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4xvsra_d(int64x4_t value, int64x4_t shift)
        ///   LASX:  XVSRA.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> ShiftRightArithmetic(Vector256<long> value, Vector256<long> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvsrari_b(int8x32_t a, const int n)
        ///   LASX: XVSRARI.B Xd.32B, Xj.32B, ui3
        /// </summary>
        public static Vector256<sbyte> ShiftRightArithmeticRounded(Vector256<sbyte> value, const byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvsrari_h(int16x16_t a, const int n)
        ///   LASX: XVSRARI.H Xd.16H, Xj.16H, ui4
        /// </summary>
        public static Vector256<short> ShiftRightArithmeticRounded(Vector256<short> value, const byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvsrari_w(int32x8_t a, const int n)
        ///   LASX: XVSRARI.W Xd.8W, Xj.8W, ui5
        /// </summary>
        public static Vector256<int> ShiftRightArithmeticRounded(Vector256<int> value, const byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvsrari_d(int64x4_t a, const int n)
        ///   LASX: XVSRARI.D Xd.4D, Xj.4D, ui6
        /// </summary>
        public static Vector256<long> ShiftRightArithmeticRounded(Vector256<long> value, const byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvsrar_b(int8x32_t a, int8x32_t b)
        ///   LASX: XVSRAR.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> ShiftRightArithmeticRounded(Vector256<sbyte> value, Vector256<sbyte> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16 xvsrar_h(int16x16_t value, int16x16_t shift)
        ///   LASX: XVSRAR.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> ShiftRightArithmeticRounded(Vector256<short> value, Vector256<short> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8 xvsrar_w(int32x8_t value, int32x8_t shift)
        ///   LASX: XVSRAR.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> ShiftRightArithmeticRounded(Vector256<int> value, Vector256<int> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4 xvsrar_d(int64x4_t value, int64x4_t shift)
        ///   LASX: XVSRAR.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> ShiftRightArithmeticRounded(Vector256<long> value, Vector256<long> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvrotri_b(uint8x32_t a, const int n)
        ///   LASX: XVROTRI.B Xd.32B, Xj.32B, ui3
        /// </summary>
        public static Vector256<sbyte> RotateRight(Vector256<sbyte> value, const byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvrotri_b(uint8x32_t a, const int n)
        ///   LASX: XVROTRI.B Xd.32B, Xj.32B, ui3
        /// </summary>
        public static Vector256<byte> RotateRight(Vector256<byte> value, const byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvrotri_h(uint16x16_t a, const int n)
        ///   LASX: XVROTRI.H Xd.16H, Xj.16H, ui4
        /// </summary>
        public static Vector256<short> RotateRight(Vector256<short> value, const byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvrotri_h(uint16x16_t a, const int n)
        ///   LASX: XVROTRI.H Xd.16H, Xj.16H, ui4
        /// </summary>
        public static Vector256<ushort> RotateRight(Vector256<ushort> value, const byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvrotri_w(uint32x8_t a, const int n)
        ///   LASX: XVROTRI.W Xd.8W, Xj.8W, ui5
        /// </summary>
        public static Vector256<int> RotateRight(Vector256<int> value, const byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvrotri_w(uint32x8_t a, const int n)
        ///   LASX: XVROTRI.W Xd.8W, Xj.8W, ui5
        /// </summary>
        public static Vector256<uint> RotateRight(Vector256<uint> value, const byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvrotri_d(uint64x4_t a, const int n)
        ///   LASX: XVROTRI.D Xd.4D, Xj.4D, ui6
        /// </summary>
        public static Vector256<long> RotateRight(Vector256<long> value, const byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvrotri_d(uint64x4_t a, const int n)
        ///   LASX: XVROTRI.D Xd.4D, Xj.4D, ui6
        /// </summary>
        public static Vector256<ulong> RotateRight(Vector256<ulong> value, const byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvrotr_b(int8x32_t a, int8x32_t b)
        ///   LASX: XVROTR.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> RotateRight(Vector256<sbyte> value, Vector256<sbyte> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvrotr_b(uint8x32_t a, uint8x32_t b)
        ///   LASX: XVROTR.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> RotateRight(Vector256<byte> value, Vector256<byte> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16 xvrotr_h(int16x16_t value, int16x16_t shift)
        ///   LASX: XVROTR.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> RotateRight(Vector256<short> value, Vector256<short> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16 xvrotr_h(uint16x16_t value, uint16x16_t shift)
        ///   LASX: XVROTR.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> RotateRight(Vector256<ushort> value, Vector256<ushort> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8 xvrotr_w(int32x8_t value, int32x8_t shift)
        ///   LASX: XVROTR.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> RotateRight(Vector256<int> value, Vector256<int> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8 xvrotr_w(uint32x8_t value, uint32x8_t shift)
        ///   LASX: XVROTR.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> RotateRight(Vector256<uint> value, Vector256<uint> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4 xvrotr_d(int64x4_t value, int64x4_t shift)
        ///   LASX: XVROTR.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> RotateRight(Vector256<long> value, Vector256<long> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4 xvrotr_d(uint64x4_t value, uint64x4_t shift)
        ///   LASX: XVROTR.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> RotateRight(Vector256<ulong> value, Vector256<ulong> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvsigncov_b(int8x32_t a)
        ///   LASX: XVSIGNCOV.B Xd.32B, Xj.32B, Xj.32B
        /// </summary>
        public static Vector256<byte> Abs(Vector256<sbyte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvsigncov_h(int16x16_t a)
        ///   LASX: XVSIGNCOV.H Xd.16H, Xj.16H, Xj.16H
        /// </summary>
        public static Vector256<ushort> Abs(Vector256<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvsigncov_w(int32x8_t a)
        ///   LASX: XVSIGNCOV.W Xd.8W, Xj.8W, Xj.8W
        /// </summary>
        public static Vector256<uint> Abs(Vector256<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvsigncov_d(int64xx_t a)
        ///   LASX: XVSIGNCOV.D Xd.4D, Xj.4D, Xj.4D
        /// </summary>
        public static Vector256<ulong> Abs(Vector256<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x8_t xvbitclri_w(float32x8_t a)
        ///   LASX: XVBITCLRI.W Xd.8S, Xd.8S, 31
        /// </summary>
        public static Vector256<float> Abs(Vector256<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x4_t xvbitclri_d(float64x4_t a)
        ///   LASX: XVBITCLRI.D Xd.4D, Xj.4D, 63
        /// </summary>
        public static Vector256<double> Abs(Vector256<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x8_t xvfrintrm_s(float32x8_t a)
        ///   LASX: XVFRINTRM.S Xd.8S, Xj.8S
        /// </summary>
        public static Vector256<float> Floor(Vector256<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x4_t xvfrintrm_d(float64x4_t a)
        ///   LASX: XVFRINTRM.D Xd.4D, Xj.4D
        /// </summary>
        public static Vector256<double> Floor(Vector256<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x8_t xvfrintrp_s(float32x8_t a)
        ///   LASX: XVFRINTRP.S Xd.8S, Xj.8S
        /// </summary>
        public static Vector256<float> Ceiling(Vector256<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x4_t xvfrintrp_d(float64x4_t a)
        ///   LASX: XVFRINTRP.D Xd.4D, Xj.4D
        /// </summary>
        public static Vector256<double> Ceiling(Vector256<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x8_t xvfrintrz_s(float32x8_t a)
        ///   LASX: XVFRINTRZ.S Xd.8S, Xj.8S
        /// </summary>
        public static Vector256<float> RoundToZero(Vector256<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x4_t xvfrintrz_d(float64x4_t a)
        ///   LASX: XVFRINTRZ.D Xd.4D, Xj.4D
        /// </summary>
        public static Vector256<double> RoundToZero(Vector256<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x8_t xvfrintrm_s(float32x8_t a)
        ///   LASX: XVFRINTRM.S Xd.8S, Xj.8S
        /// </summary>
        public static Vector256<float> RoundToNegativeInfinity(Vector256<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x4_t xvfrintrm_d(float64x4_t a)
        ///   LASX: XVFRINTRM.D Xd.4D, Xj.4D
        /// </summary>
        public static Vector256<double> RoundToNegativeInfinity(Vector256<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x8_t xvfrintrp_s(float32x8_t a)
        ///   LASX: XVFRINTRP.S Xd.8S, Xj.8S
        /// </summary>
        public static Vector256<float> RoundToPositiveInfinity(Vector256<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x4_t xvfrintrp_d(float64x4_t a)
        ///   LASX: XVFRINTRP.D Xd.4D, Xj.4D
        /// </summary>
        public static Vector256<double> RoundToPositiveInfinity(Vector256<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvinsgr2vr_w(int32x8_t v, int32_t data, const int index)
        ///   LASX: XVINSGR2VR.W Xd.S, Rj, ui3
        /// </summary>
        public static Vector256<int> Insert(Vector256<int> vector, int data, const byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvinsgr2vr_w(uint32x8_t v, uint32_t data, const int index)
        ///   LASX: XVINSGR2VR.W Xd.S, Rj, ui3
        /// </summary>
        public static Vector256<uint> Insert(Vector256<uint> vector, uint data, const byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvinsgr2vr_d(int64x4_t v, int64_t data, const int index)
        ///   LASX: XVINSGR2VR.D Xd.D, Rj, ui2
        /// </summary>
        public static Vector256<long> Insert(Vector256<long> vector, long data, const byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvinsgr2vr_d(uint64x4_t v, uint64_t data, const int index)
        ///   LASX: XVINSGR2VR.D Xd.D, Rj, ui2
        /// </summary>
        public static Vector256<ulong> Insert(Vector256<ulong> vector, ulong data, const byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x8_t xvinsve0_w(float32x8_t v, float32_t data, const int index)
        ///   LASX: XVINSVE0.W Xd.S, Xj.S[0], ui3
        /// </summary>
        public static Vector256<float> Insert(Vector256<float> vector, float data, const byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x4_t xvinsve0_d(float64x4_t v, float64_t data, const int index)
        ///   LASX: XVINSVE0.D Xd.D, Xj.D[0], ui2
        /// </summary>
        public static Vector256<double> Insert(Vector256<double> vector, double data, const byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvreplgr2vr_b(int8_t value)
        ///   LASX: XVREPLGR2VR.B Xd.32B, Rj
        /// </summary>
        public static Vector256<sbyte> DuplicateToVector256(sbyte value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvreplgr2vr_b(uint8_t value)
        ///   LASX: XVREPLGR2VR.B Xd.32B, Rj
        /// </summary>
        public static Vector256<byte> DuplicateToVector256(byte value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvreplgr2vr_h(int16_t value)
        ///   LASX: XVREPLGR2VR.H Xd.16H, Rj
        /// </summary>
        public static Vector256<short> DuplicateToVector256(short value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvreplgr2vr_h(uint16_t value)
        ///   LASX: XVREPLGR2VR.H Xd.16H, Rj
        /// </summary>
        public static Vector256<ushort> DuplicateToVector256(ushort value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvreplgr2vr_w(int32_t value)
        ///   LASX: XVREPLGR2VR.W Xd.8W, Rj
        /// </summary>
        public static Vector256<int> DuplicateToVector256(int value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvreplgr2vr_w(uint32_t value)
        ///   LASX: XVREPLGR2VR.W Xd.8W, Rj
        /// </summary>
        public static Vector256<uint> DuplicateToVector256(uint value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvreplgr2vr_d(int64_t value)
        ///   LASX: XVREPLGR2VR.D Xd.4D, Rj
        /// </summary>
        public static Vector256<long> DuplicateToVector256(long value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvreplgr2vr_d(uint64_t value)
        ///   LASX: XVREPLGR2VR.D Xd.4D, Rj
        /// </summary>
        public static Vector256<ulong> DuplicateToVector256(ulong value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x8_t xvreplve0_w(float32_t value)
        ///   LASX: XVREPLVE0.W Xd.8S, Xj.S[0]
        /// </summary>
        public static Vector256<float> DuplicateToVector256(float value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x4_t xvreplve0_d(float64_t value)
        ///   LASX: XVREPLVE0.D Xd.4D, Xj.D[0]
        /// </summary>
        public static Vector256<double> DuplicateToVector256(double value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x8_t xvffint_s_w(int32x8_t a)
        ///   LASX: XVFFINT.S.W Xd.8S, Xj.8W
        /// </summary>
        public static Vector256<float> ConvertToSingle(Vector256<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x8_t xvffint_s_wu(uint32x8_t a)
        ///   LASX: XVFFINT.S.WU Xd.8S, Xj.8W
        /// </summary>
        public static Vector256<float> ConvertToSingle(Vector256<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x4_t xvffint_d_l(int64x4_t a)
        ///   LASX: XVFFINT.D.L Xd.4D, Xj.4D
        /// </summary>
        public static Vector256<double> ConvertToDouble(Vector256<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x4_t xvffint_d_lu(uint64x4_t a)
        ///   LASX: XVFFINT.D.LU Xd.4D, Xj.4D
        /// </summary>
        public static Vector256<double> ConvertToDouble(Vector256<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// bool xvsetnez_v(uint8x32_t value)
        ///   LASX: XVSETNEZ.V cd, Xj.32B
        /// </summary>
        public static bool HasElementsNotZero(Vector256<byte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// bool xvseteqz_v(uint8x32_t value)
        ///   LASX: XVSETEQZ.V cd, Xj.32B
        /// </summary>
        public static bool AllElementsIsZero(Vector256<byte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// bool xvsetallnez_b(int8x32_t value)
        ///   LASX: XVSETALLNEZ.B cd, Xj.32B
        /// </summary>
        public static bool AllElementsNotZero(Vector256<sbyte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// bool xvsetallnez_b(uint8x32_t value)
        ///   LASX: XVSETALLNEZ.B cd, Xj.32B
        /// </summary>
        public static bool AllElementsNotZero(Vector256<byte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// bool xvsetallnez_h(int16x16_t value)
        ///   LASX: XVSETALLNEZ.H cd, Xj.16H
        /// </summary>
        public static bool AllElementsNotZero(Vector256<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// bool xvsetallnez_h(uint16x16_t value)
        ///   LASX: XVSETALLNEZ.H cd, Xj.16H
        /// </summary>
        public static bool AllElementsNotZero(Vector256<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// bool xvsetallnez_w(int32x8_t value)
        ///   LASX: XVSETALLNEZ.W cd, Xj.8W
        /// </summary>
        public static bool AllElementsNotZero(Vector256<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// bool xvsetallnez_w(uint32x8_t value)
        ///   LASX: XVSETALLNEZ.W cd, Xj.8W
        /// </summary>
        public static bool AllElementsNotZero(Vector256<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// bool xvsetallnez_w(int64x8_t value)
        ///   LASX: XVSETALLNEZ.D cd, Xj.4D
        /// </summary>
        public static bool AllElementsNotZero(Vector256<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// bool xvsetallnez_w(uint64x8_t value)
        ///   LASX: XVSETALLNEZ.D cd, Xj.4D
        /// </summary>
        public static bool AllElementsNotZero(Vector256<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// bool xvsetanyeqz_b(int8x32_t value)
        ///   LASX: XVSETANYEQZ.B cd, Xj.32B
        /// </summary>
        public static bool HasElementsIsZero(Vector256<sbyte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// bool xvsetanyeqz_b(uint8x32_t value)
        ///   LASX: XVSETANYEQZ.B cd, Xj.32B
        /// </summary>
        public static bool HasElementsIsZero(Vector256<byte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// bool xvsetanyeqz_h(int16x16_t value)
        ///   LASX: XVSETANYEQZ.H cd, Xj.16H
        /// </summary>
        public static bool HasElementsIsZero(Vector256<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// bool xvsetanyeqz_h(uint16x16_t value)
        ///   LASX: XVSETANYEQZ.H cd, Xj.16H
        /// </summary>
        public static bool HasElementsIsZero(Vector256<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// bool xvsetanyeqz_w(int32x8_t value)
        ///   LASX: XVSETANYEQZ.W cd, Xj.8W
        /// </summary>
        public static bool HasElementsIsZero(Vector256<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// bool xvsetanyeqz_w(uint32x8_t value)
        ///   LASX: XVSETANYEQZ.W cd, Xj.8W
        /// </summary>
        public static bool HasElementsIsZero(Vector256<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// bool xvsetanyeqz_w(int64x8_t value)
        ///   LASX: XVSETANYEQZ.D cd, Xj.4D
        /// </summary>
        public static bool HasElementsIsZero(Vector256<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// bool xvsetanyeqz_w(uint64x8_t value)
        ///   LASX: XVSETANYEQZ.D cd, Xj.4D
        /// </summary>
        public static bool HasElementsIsZero(Vector256<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32 xvsrlni_b_h(int16x16_t left, int16x16_t right, shift)
        ///   LASX: XVSRLNI.B.H Xd, Xj, ui4
        /// </summary>
        public static Vector256<sbyte> ShiftRightLogicalNarrowingLowerEach128(Vector256<short> left, Vector256<short> right, byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32 xvsrlni_b_h(uint16x16_t left, uint16x16_t right, shift)
        ///   LASX: XVSRLNI.B.H Xd, Xj, ui4
        /// </summary>
        public static Vector256<byte> ShiftRightLogicalNarrowingLowerEach128(Vector256<ushort> left, Vector256<ushort> right, byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16 xvsrlni_h_w(int32x8_t left, int32x8_t right, shift)
        ///   LASX: XVSRLNI.H.W Xd, Xj, ui5
        /// </summary>
        public static Vector256<short> ShiftRightLogicalNarrowingLowerEach128(Vector256<int> left, Vector256<int> right, byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16 xvsrlni_h_w(uint32x8_t left, uint32x8_t right, shift)
        ///   LASX: XVSRLNI.H.W Xd, Xj, ui5
        /// </summary>
        public static Vector256<ushort> ShiftRightLogicalNarrowingLowerEach128(Vector256<uint> left, Vector256<uint> right, byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8 xvsrlni_w_d(int64x4_t left, int64x4_t right, shift)
        ///   LASX: XVSRLNI.W.D Xd, Xj, ui6
        /// </summary>
        public static Vector256<int> ShiftRightLogicalNarrowingLowerEach128(Vector256<long> left, Vector256<long> right, byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8 xvsrlni_w_d(uint64x4_t left, uint64x4_t right, shift)
        ///   LASX: XVSRLNI.W.D Xd, Xj, ui6
        /// </summary>
        public static Vector256<uint> ShiftRightLogicalNarrowingLowerEach128(Vector256<ulong> left, Vector256<ulong> right, byte shift) { throw new PlatformNotSupportedException(); }

        ///// <summary>
        ///// uint64x4 xvsrlni_d_q(uint128x2_t left, uint128x2_t right, shift)
        /////   LASX: XVSRLNI.D.Q Xd.2Q, Xj.2Q, ui7
        ///// </summary>
        //public static Vector256<ulong> ShiftRightLogicalNarrowingLowerEach128(Vector256<ulonglong> left, Vector256<ulonglong> right, byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x16_t xvsrln_b_h(uint16x16_t value, uint16x16_t shift)
        ///   LASX: XVSRLN.B.H Xd.8B, Xj.16H, Xk.16H
        /// </summary>
        public static Vector128<byte> ShiftRightLogicalNarrowingLowerEach128(Vector256<ushort> value, Vector256<ushort> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x8_t xvsrln_h_w(int32x8_t value, int32x8_t shift)
        ///   LASX: XVSRLN.H.W Xd.8H, Xj.8W, Xk.8W
        /// </summary>
        public static Vector128<short> ShiftRightLogicalNarrowingLowerEach128(Vector256<int> value, Vector256<int> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x8_t xvsrln_h_w(uint32x8_t value, uint32x8_t shift)
        ///   LASX: XVSRLN.H.W Xd.8H, Xj.8W, Xk.8W
        /// </summary>
        public static Vector128<ushort> ShiftRightLogicalNarrowingLowerEach128(Vector256<uint> value, Vector256<uint> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x4_t xvsrln_w_d(int64x4_t value, int64x4_t shift)
        ///   LASX: XVSRLN.W.D Xd.4W, Xj.4D, Xk.2D
        /// </summary>
        public static Vector128<int> ShiftRightLogicalNarrowingLowerEach128(Vector256<long> value, Vector256<long> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x4_t xvsrln_w_d(uint64x4_t value, uint64x4_t shift)
        ///   LASX: XVSRLN.W.D Xd.4W, Xj.4D, Xk.2D
        /// </summary>
        public static Vector128<uint> ShiftRightLogicalNarrowingLowerEach128(Vector256<ulong> value, Vector256<ulong> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvssrlni_b_h(int16x16_t left, int16x16_t right, const byte n)
        ///   LASX: XVSSRLNI.B.H Xd.32B, Xj.16H, ui4  ///NOTE: the Vd is both input and output.
        /// </summary>
        public static Vector256<sbyte> ShiftRightLogicalNarrowingSaturateLowerEach128(Vector256<short> left, Vector256<short> right, [ConstantExpected(Max = (byte)(15))] byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvssrlni_b_h(uint16x16_t left, uint16x16_t right, const byte n)
        ///   LASX: XVSSRLNI.B.H Xd.32B, Xj.16H, ui4
        /// </summary>
        public static Vector256<byte> ShiftRightLogicalNarrowingSaturateLowerEach128(Vector256<ushort> left, Vector256<ushort> right, [ConstantExpected(Max = (byte)(15))] byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvssrlni_h_w(int32x8_t left, int32x8_t right, const byte n)
        ///   LASX: XVSSRLNI.H.W Xd.16H, Xj.8W, ui5
        /// </summary>
        public static Vector256<short> ShiftRightLogicalNarrowingSaturateLowerEach128(Vector256<int> left, Vector256<int> right, [ConstantExpected(Max = (byte)(31))] byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvssrlni_h_w(uint32x8_t left, uint32x8_t right, const byte n)
        ///   LASX: XVSSRLNI.H.W Xd.16H, Xj.8W, ui5
        /// </summary>
        public static Vector256<ushort> ShiftRightLogicalNarrowingSaturateLowerEach128(Vector256<uint> left, Vector256<uint> right, [ConstantExpected(Max = (byte)(31))] byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvssrlni_w_d(int64x4_t left, int64x4_t right, const byte n)
        ///   LASX: XVSSRLNI.W.D Xd.8W, Xj.4D, ui6
        /// </summary>
        public static Vector256<int> ShiftRightLogicalNarrowingSaturateLowerEach128(Vector256<long> left, Vector256<long> right, [ConstantExpected(Max = (byte)(63))] byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvssrlni_w_d(uint64x4_t left, uint64x4_t right, const byte n)
        ///   LASX: XVSSRLNI.W.D Xd.8W, Xj.4D, ui6
        /// </summary>
        public static Vector256<uint> ShiftRightLogicalNarrowingSaturateLowerEach128(Vector256<ulong> left, Vector256<ulong> right, [ConstantExpected(Max = (byte)(63))] byte shift) { throw new PlatformNotSupportedException(); }

        ///// <summary>
        ///// int64x4_t xvssrlni_d_q(int128x2_t left, int128x2_t right, const byte n)
        /////   LASX: XVSSRLNI.D.Q Xd.4D, Xj.2Q, ui7
        ///// </summary>
        //public static Vector256<long> ShiftRightLogicalNarrowingSaturateLowerEach128(Vector256<longlong> left, Vector256<longlong> right, [ConstantExpected(Max = (byte)(127))] byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x16_t xvssrln_b_h(int16x16_t value, int16x16_t shift)
        ///   LASX: XVSSRLN.B.H Xd.16B, Xj.16H, Xk.16H
        /// </summary>
        public static Vector128<sbyte> ShiftRightLogicalNarrowingSaturateLowerEach128(Vector256<short> value, Vector256<short> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x16_t xvssrln_b_h(uint16x16_t value, uint16x16_t shift)
        ///   LASX: XVSSRLN.B.H Xd.16B, Xj.16H, Xk.16H
        /// </summary>
        public static Vector128<byte> ShiftRightLogicalNarrowingSaturateLowerEach128(Vector256<ushort> value, Vector256<ushort> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x4_t xvssrln_h_w(int32x8_t value, int32x8_t shift)
        ///   LASX: XVSSRLN.H.W Xd.8H, Xj.8W, Xk.8W
        /// </summary>
        public static Vector128<short> ShiftRightLogicalNarrowingSaturateLowerEach128(Vector256<int> value, Vector256<int> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x4_t xvssrln_h_w(uint32x8_t value, uint32x8_t shift)
        ///   LASX: XVSSRLN.H.W Xd.8H, Xj.8W, Xk.8W
        /// </summary>
        public static Vector128<ushort> ShiftRightLogicalNarrowingSaturateLowerEach128(Vector256<uint> value, Vector256<uint> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x4_t xvssrln_w_d(int64x4_t value, int64x4_t shift)
        ///   LASX: XVSSRLN.W.D Xd.4W, Xj.4D, Xk.4D
        /// </summary>
        public static Vector128<int> ShiftRightLogicalNarrowingSaturateLowerEach128(Vector256<long> value, Vector256<long> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x4_t xvssrln_w_d(uint64x4_t value, uint64x4_t shift)
        ///   LASX: XVSSRLN.W.D Xd.4W, Xj.4D, Xk.4D
        /// </summary>
        public static Vector128<uint> ShiftRightLogicalNarrowingSaturateLowerEach128(Vector256<ulong> value, Vector256<ulong> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvssrlni_bu_h(uint16x16_t left, uint16x16_t right, const byte n)
        ///   LASX: XVSSRLNI.BU.H Xd.32B, Xj.16H, ui4
        /// </summary>
        public static Vector256<byte> ShiftRightLogicalNarrowingSaturateUnsignedLowerEach128(Vector256<ushort> left, Vector256<ushort> right, [ConstantExpected(Max = (byte)(15))] byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvssrlni_hu_w(uint32x8_t left, uint32x8_t right, const byte n)
        ///   LASX: XVSSRLNI.HU.W Xd.16H, Xj.8W, ui5
        /// </summary>
        public static Vector256<ushort> ShiftRightLogicalNarrowingSaturateUnsignedLowerEach128(Vector256<uint> left, Vector256<uint> right, [ConstantExpected(Max = (byte)(31))] byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvssrlni_wu_d(uint64x4_t left, uint64x4_t right, const byte n)
        ///   LASX: XVSSRLNI.WU.D Xd.8W, Xj.4D, ui6
        /// </summary>
        public static Vector256<uint> ShiftRightLogicalNarrowingSaturateUnsignedLowerEach128(Vector256<ulong> left, Vector256<ulong> right, [ConstantExpected(Max = (byte)(63))] byte shift) { throw new PlatformNotSupportedException(); }

        ///// <summary>
        ///// uint64x4_t xvssrlni_du_q(uint128x2_t left, uint128x2_t right, const byte n)
        /////   LASX: XVSSRLNI.DU.Q Xd.4D, Xj.2Q, ui7
        ///// </summary>
        //public static Vector256<ulong> ShiftRightLogicalNarrowingSaturateUnsignedLowerEach128(Vector256<ulonglong> left, Vector256<ulonglong> right, [ConstantExpected(Max = (byte)(127))] byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x16_t xvssrln_bu_h(uint16x16_t value, uint16x16_t shift)
        ///   LASX: XVSSRLN.BU.H Xd.16B, Xj.16H, Xk.16H
        /// </summary>
        public static Vector128<byte> ShiftRightLogicalNarrowingSaturateUnsignedLowerEach128(Vector256<ushort> value, Vector256<ushort> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x4_t xvssrln_hu_w(uint32x8_t value, uint32x8_t shift)
        ///   LASX: XVSSRLN.HU.W Xd.8H, Xj.8W, Xk.8W
        /// </summary>
        public static Vector128<ushort> ShiftRightLogicalNarrowingSaturateUnsignedLowerEach128(Vector256<uint> value, Vector256<uint> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x4_t xvssrln_wu_d(uint64x4_t value, uint64x4_t shift)
        ///   LASX: XVSSRLN.WU.D Xd.4W, Xj.4D, Xk.4D
        /// </summary>
        public static Vector128<uint> ShiftRightLogicalNarrowingSaturateUnsignedLowerEach128(Vector256<ulong> value, Vector256<ulong> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvssran_b_h(int16x16_t left, int16x16_t right, const byte n)
        ///   LASX: XVSSRANI.B.H Xd.32B, Xj.16H, ui4  ///NOTE: the Vd is both input and output.
        /// </summary>
        public static Vector256<sbyte> ShiftRightArithmeticNarrowingSaturateLowerEach128(Vector256<short> left, Vector256<short> right, [ConstantExpected(Max = (byte)(15))] byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvssran_h_w(int32x8_t left, int32x8_t right, const byte n)
        ///   LASX: XVSSRANI.H.W Xd.16H, Xj.8W, ui5
        /// </summary>
        public static Vector256<short> ShiftRightArithmeticNarrowingSaturateLowerEach128(Vector256<int> left, Vector256<int> right, [ConstantExpected(Max = (byte)(31))] byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvssran_w_d(int64x4_t left, int64x4_t right, const byte n)
        ///   LASX: XVSSRANI.W.D Xd.8W, Xj.4D, ui6
        /// </summary>
        public static Vector256<int> ShiftRightArithmeticNarrowingSaturateLowerEach128(Vector256<long> left, Vector256<long> right, [ConstantExpected(Max = (byte)(63))] byte shift) { throw new PlatformNotSupportedException(); }

        ///// <summary>
        ///// int64x4_t xvssran_d_q(int128x2_t left, int128x2_t right, const byte n)
        /////   LASX: XVSSRANI.D.Q Xd.4D, Xj.2Q, ui7
        ///// </summary>
        //public static Vector256<long> ShiftRightArithmeticNarrowingSaturateLowerEach128(Vector256<longlong> left, Vector256<longlong> right, [ConstantExpected(Max = (byte)(127))] byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x16_t xvssran_b_h(int16x16_t value, int16x16_t shift)
        ///   LASX: XVSSRAN.B.H Xd.16B, Xj.16H, Xk.16H
        /// </summary>
        public static Vector128<sbyte> ShiftRightArithmeticNarrowingSaturateLowerEach128(Vector256<short> value, Vector256<short> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x4_t xvssran_h_w(int32x8_t value, int32x8_t shift)
        ///   LASX: XVSSRAN.H.W Xd.8H, Xj.8W, Xk.8W
        /// </summary>
        public static Vector128<short> ShiftRightArithmeticNarrowingSaturateLowerEach128(Vector256<int> value, Vector256<int> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x4_t xvssran_w_d(int64x4_t value, int64x4_t shift)
        ///   LASX: XVSSRAN.W.D Xd.4W, Xj.4D, Xk.4D
        /// </summary>
        public static Vector128<int> ShiftRightArithmeticNarrowingSaturateLowerEach128(Vector256<long> value, Vector256<long> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvssrani_bu_h(int16x16_t left, int16x16_t right, const byte n)
        ///   LASX: XVSSRANI.BU.H Xd.32B, Xj.16H, ui4
        /// </summary>
        public static Vector256<byte> ShiftRightArithmeticNarrowingSaturateUnsignedLowerEach128(Vector256<short> left, Vector256<short> right, [ConstantExpected(Max = (byte)(15))] byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvssrani_hu_w(int32x8_t left, int32x8_t right, const byte n)
        ///   LASX: XVSSRANI.HU.W Xd.16H, Xj.8W, ui5
        /// </summary>
        public static Vector256<ushort> ShiftRightArithmeticNarrowingSaturateUnsignedLowerEach128(Vector256<int> left, Vector256<int> right, [ConstantExpected(Max = (byte)(31))] byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvssrani_wu_d(int64x4_t left, int64x4_t right, const byte n)
        ///   LASX: XVSSRANI.WU.D Xd.8W, Xj.4D, ui6
        /// </summary>
        public static Vector256<uint> ShiftRightArithmeticNarrowingSaturateUnsignedLowerEach128(Vector256<long> left, Vector256<long> right, [ConstantExpected(Max = (byte)(63))] byte shift) { throw new PlatformNotSupportedException(); }

        ///// <summary>
        ///// uint64x4_t xvssrani_du_q(int128x2_t left, int128x2_t right, const byte n)
        /////   LASX: XVSSRANI.DU.Q Xd.4D, Xj.2Q, ui7
        ///// </summary>
        //public static Vector256<ulong> ShiftRightArithmeticNarrowingSaturateUnsignedLowerEach128(Vector256<longlong> left, Vector256<longlong> right, [ConstantExpected(Max = (byte)(127))] byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x16_t xvssran_bu_h(int16x16_t value, int16x16_t shift)
        ///   LASX: XVSSRAN.BU.H Xd.16B, Xj.16H, Xk.16H
        /// </summary>
        public static Vector128<byte> ShiftRightArithmeticNarrowingSaturateUnsignedLowerEach128(Vector256<short> value, Vector256<short> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x4_t xvssran_hu_w(int32x8_t value, int32x8_t shift)
        ///   LASX: XVSSRAN.HU.W Xd.8H, Xj.8W, Xk.8W
        /// </summary>
        public static Vector128<ushort> ShiftRightArithmeticNarrowingSaturateUnsignedLowerEach128(Vector256<int> value, Vector256<int> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x4_t xvssran_wu_d(int64x4_t value, int64x4_t shift)
        ///   LASX: XVSSRAN.WU.D Xd.4W, Xj.4D, Xk.4D
        /// </summary>
        public static Vector128<uint> ShiftRightArithmeticNarrowingSaturateUnsignedLowerEach128(Vector256<long> value, Vector256<long> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvssrlrni_b_h(int16x16_t left, int16x16_t right, const byte n)
        ///   LASX: XVSSRLRNI.B.H Xd.32B, Xj.16H, ui4  ///NOTE: the Vd is both input and output.
        /// </summary>
        public static Vector256<sbyte> ShiftRightLogicalRoundedNarrowingSaturateLowerEach128(Vector256<short> left, Vector256<short> right, [ConstantExpected(Max = (byte)(15))] byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvssrlrni_b_h(uint16x16_t left, uint16x16_t right, const byte n)
        ///   LASX: XVSSRLRNI.B.H Xd.32B, Xj.16H, ui4
        /// </summary>
        public static Vector256<byte> ShiftRightLogicalRoundedNarrowingSaturateLowerEach128(Vector256<ushort> left, Vector256<ushort> right, [ConstantExpected(Max = (byte)(15))] byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvssrlrni_h_w(int32x8_t left, int32x8_t right, const byte n)
        ///   LASX: XVSSRLRNI.H.W Xd.16H, Xj.8W, ui5
        /// </summary>
        public static Vector256<short> ShiftRightLogicalRoundedNarrowingSaturateLowerEach128(Vector256<int> left, Vector256<int> right, [ConstantExpected(Max = (byte)(31))] byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvssrlrni_h_w(uint32x8_t left, uint32x8_t right, const byte n)
        ///   LASX: XVSSRLRNI.H.W Xd.16H, Xj.8W, ui5
        /// </summary>
        public static Vector256<ushort> ShiftRightLogicalRoundedNarrowingSaturateLowerEach128(Vector256<uint> left, Vector256<uint> right, [ConstantExpected(Max = (byte)(31))] byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvssrlrni_w_d(int64x4_t left, int64x4_t right, const byte n)
        ///   LASX: XVSSRLRNI.W.D Xd.8W, Xj.4D, ui6
        /// </summary>
        public static Vector256<int> ShiftRightLogicalRoundedNarrowingSaturateLowerEach128(Vector256<long> left, Vector256<long> right, [ConstantExpected(Max = (byte)(63))] byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvssrlrni_w_d(uint64x4_t left, uint64x4_t right, const byte n)
        ///   LASX: XVSSRLRNI.W.D Xd.8W, Xj.4D, ui6
        /// </summary>
        public static Vector256<uint> ShiftRightLogicalRoundedNarrowingSaturateLowerEach128(Vector256<ulong> left, Vector256<ulong> right, [ConstantExpected(Max = (byte)(63))] byte shift) { throw new PlatformNotSupportedException(); }

        ///// <summary>
        ///// int64x4_t xvssrlrni_d_q(int128x2_t left, int128x2_t right, const byte n)
        /////   LASX: XVSSRLRNI.D.Q Xd.4D, Xj.2Q, ui7
        ///// </summary>
        //public static Vector256<long> ShiftRightLogicalRoundedNarrowingSaturateLowerEach128(Vector256<longlong> left, Vector256<longlong> right, [ConstantExpected(Max = (byte)(127))] byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x16_t xvssrlrn_b_h(int16x16_t value, int16x16_t shift)
        ///   LASX: XVSSRLRN.B.H Xd.16B, Xj.16H, Xk.16H
        /// </summary>
        public static Vector128<sbyte> ShiftRightLogicalRoundedNarrowingSaturateLowerEach128(Vector256<short> value, Vector256<short> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x16_t xvssrlrn_b_h(uint16x16_t value, uint16x16_t shift)
        ///   LASX: XVSSRLRN.B.H Xd.16B, Xj.16H, Xk.16H
        /// </summary>
        public static Vector128<byte> ShiftRightLogicalRoundedNarrowingSaturateLowerEach128(Vector256<ushort> value, Vector256<ushort> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x4_t xvssrlrn_h_w(int32x8_t value, int32x8_t shift)
        ///   LASX: XVSSRLRN.H.W Xd.8H, Xj.8W, Xk.8W
        /// </summary>
        public static Vector128<short> ShiftRightLogicalRoundedNarrowingSaturateLowerEach128(Vector256<int> value, Vector256<int> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x4_t xvssrlrn_h_w(uint32x8_t value, uint32x8_t shift)
        ///   LASX: XVSSRLRN.H.W Xd.8H, Xj.8W, Xk.8W
        /// </summary>
        public static Vector128<ushort> ShiftRightLogicalRoundedNarrowingSaturateLowerEach128(Vector256<uint> value, Vector256<uint> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x4_t xvssrlrn_w_d(int64x4_t value, int64x4_t shift)
        ///   LASX: XVSSRLRN.W.D Xd.4W, Xj.4D, Xk.4D
        /// </summary>
        public static Vector128<int> ShiftRightLogicalRoundedNarrowingSaturateLowerEach128(Vector256<long> value, Vector256<long> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x4_t xvssrlrn_w_d(uint64x4_t value, uint64x4_t shift)
        ///   LASX: XVSSRLRN.W.D Xd.4W, Xj.4D, Xk.4D
        /// </summary>
        public static Vector128<uint> ShiftRightLogicalRoundedNarrowingSaturateLowerEach128(Vector256<ulong> value, Vector256<ulong> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvssrlrni_bu_h(uint16x16_t left, uint16x16_t right, const byte n)
        ///   LASX: XVSSRLRNI.BU.H Xd.32B, Xj.16H, ui4
        /// </summary>
        public static Vector256<byte> ShiftRightLogicalRoundedNarrowingSaturateUnsignedLowerEach128(Vector256<ushort> left, Vector256<ushort> right, [ConstantExpected(Max = (byte)(15))] byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvssrlrni_hu_w(uint32x8_t left, uint32x8_t right, const byte n)
        ///   LASX: XVSSRLRNI.HU.W Xd.16H, Xj.8W, ui5
        /// </summary>
        public static Vector256<ushort> ShiftRightLogicalRoundedNarrowingSaturateUnsignedLowerEach128(Vector256<uint> left, Vector256<uint> right, [ConstantExpected(Max = (byte)(31))] byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvssrlrni_wu_d(uint64x4_t left, uint64x4_t right, const byte n)
        ///   LASX: XVSSRLRNI.WU.D Xd.8W, Xj.4D, ui6
        /// </summary>
        public static Vector256<uint> ShiftRightLogicalRoundedNarrowingSaturateUnsignedLowerEach128(Vector256<ulong> left, Vector256<ulong> right, [ConstantExpected(Max = (byte)(63))] byte shift) { throw new PlatformNotSupportedException(); }

        ///// <summary>
        ///// uint64x4_t xvssrlrni_du_q(uint128x2_t left, uint128x2_t right, const byte n)
        /////   LASX: XVSSRLRNI.DU.Q Xd.4D, Xj.2Q, ui7
        ///// </summary>
        //public static Vector256<ulong> ShiftRightLogicalRoundedNarrowingSaturateUnsignedLowerEach128(Vector256<ulonglong> left, Vector256<ulonglong> right, [ConstantExpected(Max = (byte)(127))] byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x16_t xvssrlrn_bu_h(uint16x16_t value, uint16x16_t shift)
        ///   LASX: XVSSRLRN.BU.H Xd.16B, Xj.16H, Xk.16H
        /// </summary>
        public static Vector128<byte> ShiftRightLogicalRoundedNarrowingSaturateUnsignedLowerEach128(Vector256<ushort> value, Vector256<ushort> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x4_t xvssrlrn_hu_w(uint32x8_t value, uint32x8_t shift)
        ///   LASX: XVSSRLRN.HU.W Xd.8H, Xj.8W, Xk.8W
        /// </summary>
        public static Vector128<ushort> ShiftRightLogicalRoundedNarrowingSaturateUnsignedLowerEach128(Vector256<uint> value, Vector256<uint> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x4_t xvssrlrn_wu_d(uint64x4_t value, uint64x4_t shift)
        ///   LASX: XVSSRLRN.WU.D Xd.4W, Xj.4D, Xk.4D
        /// </summary>
        public static Vector128<uint> ShiftRightLogicalRoundedNarrowingSaturateUnsignedLowerEach128(Vector256<ulong> value, Vector256<ulong> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvssrarn_b_h(int16x16_t left, int16x16_t right, const byte n)
        ///   LASX: XVSSRARNI.B.H Xd.32B, Xj.16H, ui4  ///NOTE: the Vd is both input and output.
        /// </summary>
        public static Vector256<sbyte> ShiftRightArithmeticRoundedNarrowingSaturateLowerEach128(Vector256<short> left, Vector256<short> right, [ConstantExpected(Max = (byte)(15))] byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvssrarn_h_w(int32x8_t left, int32x8_t right, const byte n)
        ///   LASX: XVSSRARNI.H.W Xd.16H, Xj.8W, ui5
        /// </summary>
        public static Vector256<short> ShiftRightArithmeticRoundedNarrowingSaturateLowerEach128(Vector256<int> left, Vector256<int> right, [ConstantExpected(Max = (byte)(31))] byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvssrarn_w_d(int64x4_t left, int64x4_t right, const byte n)
        ///   LASX: XVSSRARNI.W.D Xd.8W, Xj.4D, ui6
        /// </summary>
        public static Vector256<int> ShiftRightArithmeticRoundedNarrowingSaturateLowerEach128(Vector256<long> left, Vector256<long> right, [ConstantExpected(Max = (byte)(63))] byte shift) { throw new PlatformNotSupportedException(); }

        ///// <summary>
        ///// int64x4_t xvssrarn_d_q(int128x2_t left, int128x2_t right, const byte n)
        /////   LASX: XVSSRARNI.D.Q Xd.4D, Xj.2Q, ui7
        ///// </summary>
        //public static Vector256<long> ShiftRightArithmeticRoundedNarrowingSaturateLowerEach128(Vector256<longlong> left, Vector256<longlong> right, [ConstantExpected(Max = (byte)(127))] byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x16_t xvssrarn_b_h(int16x16_t value, int16x16_t shift)
        ///   LASX: XVSSRARN.B.H Xd.16B, Xj.16H, Xk.16H
        /// </summary>
        public static Vector128<sbyte> ShiftRightArithmeticRoundedNarrowingSaturateLowerEach128(Vector256<short> value, Vector256<short> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x4_t xvssrarn_h_w(int32x8_t value, int32x8_t shift)
        ///   LASX: XVSSRARN.H.W Xd.8H, Xj.8W, Xk.8W
        /// </summary>
        public static Vector128<short> ShiftRightArithmeticRoundedNarrowingSaturateLowerEach128(Vector256<int> value, Vector256<int> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x4_t xvssrarn_w_d(int64x4_t value, int64x4_t shift)
        ///   LASX: XVSSRARN.W.D Xd.4W, Xj.4D, Xk.4D
        /// </summary>
        public static Vector128<int> ShiftRightArithmeticRoundedNarrowingSaturateLowerEach128(Vector256<long> value, Vector256<long> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvssrarni_bu_h(int16x16_t left, int16x16_t right, const byte n)
        ///   LASX: XVSSRARNI.BU.H Xd.32B, Xj.16H, ui4
        /// </summary>
        public static Vector256<byte> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLowerEach128(Vector256<short> left, Vector256<short> right, [ConstantExpected(Max = (byte)(15))] byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvssrarni_hu_w(int32x8_t left, int32x8_t right, const byte n)
        ///   LASX: XVSSRARNI.HU.W Xd.16H, Xj.8W, ui5
        /// </summary>
        public static Vector256<ushort> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLowerEach128(Vector256<int> left, Vector256<int> right, [ConstantExpected(Max = (byte)(31))] byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvssrarni_wu_d(int64x4_t left, int64x4_t right, const byte n)
        ///   LASX: XVSSRARNI.WU.D Xd.8W, Xj.4D, ui6
        /// </summary>
        public static Vector256<uint> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLowerEach128(Vector256<long> left, Vector256<long> right, [ConstantExpected(Max = (byte)(63))] byte shift) { throw new PlatformNotSupportedException(); }

        ///// <summary>
        ///// uint64x4_t xvssrarni_du_q(int128x2_t left, int128x2_t right, const byte n)
        /////   LASX: XVSSRARNI.DU.Q Xd.4D, Xj.2Q, ui7
        ///// </summary>
        //public static Vector256<ulong> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLowerEach128(Vector256<longlong> left, Vector256<longlong> right, [ConstantExpected(Max = (byte)(127))] byte shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x16_t xvssrarn_bu_h(int16x16_t value, int16x16_t shift)
        ///   LASX: XVSSRARN.BU.H Xd.16B, Xj.16H, Xk.16H
        /// </summary>
        public static Vector128<byte> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLowerEach128(Vector256<short> value, Vector256<short> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x8_t xvssrarn_hu_w(int32x8_t value, int32x8_t shift)
        ///   LASX: XVSSRARN.HU.W Xd.8H, Xj.8W, Xk.8W
        /// </summary>
        public static Vector128<ushort> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLowerEach128(Vector256<int> value, Vector256<int> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x4_t xvssrarn_wu_d(int64x4_t value, int64x4_t shift)
        ///   LASX: XVSSRARN.WU.D Xd.4W, Xj.4D, Xk.4D
        /// </summary>
        public static Vector128<uint> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLowerEach128(Vector256<long> value, Vector256<long> shift) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvclo_b(int8x32_t a)
        ///   LASX: XVCLO.B Xd.32B, Xj.32B
        /// </summary>
        public static Vector256<sbyte> LeadingSignCount(Vector256<sbyte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvclo_h(int16x16_t a)
        ///   LASX: XVCLO.H Xd.16H, Xj.16H
        /// </summary>
        public static Vector256<short> LeadingSignCount(Vector256<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvclo_w(int32x8_t a)
        ///   LASX: XVCLO.W Xd.8W, Xj.8W
        /// </summary>
        public static Vector256<int> LeadingSignCount(Vector256<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvclo_d(int64x4_t a)
        ///   LASX: XVCLO.D Xd.4D, Xj.4D
        /// </summary>
        public static Vector256<long> LeadingSignCount(Vector256<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvclz_b(int8x32_t a)
        ///   LASX: XVCLZ.B Xd.32B, Xj.32B
        /// </summary>
        public static Vector256<sbyte> LeadingZeroCount(Vector256<sbyte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvclz_b(uint8x32_t a)
        ///   LASX: XVCLZ.B Xd.32B, Xj.32B
        /// </summary>
        public static Vector256<byte> LeadingZeroCount(Vector256<byte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvclz_h(int16x16_t a)
        ///   LASX: XVCLZ.H Xd.16H, Xj.16H
        /// </summary>
        public static Vector256<short> LeadingZeroCount(Vector256<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvclz_h(uint16x16_t a)
        ///   LASX: XVCLZ.H Xd.16H, Xj.16H
        /// </summary>
        public static Vector256<ushort> LeadingZeroCount(Vector256<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvclz_w(int32x8_t a)
        ///   LASX: XVCLZ.W Xd.8W, Xj.8W
        /// </summary>
        public static Vector256<int> LeadingZeroCount(Vector256<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvclz_w(uint32x8_t a)
        ///   LASX: XVCLZ.W Xd.8W, Xj.8W
        /// </summary>
        public static Vector256<uint> LeadingZeroCount(Vector256<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvclz_d(int64x4_t a)
        ///   LASX: XVCLZ.D Xd.4D, Xj.4D
        /// </summary>
        public static Vector256<long> LeadingZeroCount(Vector256<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvclz_d(uint64x4_t a)
        ///   LASX: XVCLZ.D Xd.4D, Xj.4D
        /// </summary>
        public static Vector256<ulong> LeadingZeroCount(Vector256<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvpcnt_b(int8x32_t a)
        ///   LASX: XVPCNT_B Xd, Xj
        /// </summary>
        public static Vector256<sbyte> PopCount(Vector256<sbyte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvpcnt_b(uint8x32_t a)
        ///   LASX: XVPCNT_B Xd, Xj
        /// </summary>
        public static Vector256<byte> PopCount(Vector256<byte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvpcnt_h(int16x16_t a)
        ///   LASX: XVPCNT_H Xd, Xj
        /// </summary>
        public static Vector256<short> PopCount(Vector256<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvpcnt_h(uint16x16_t a)
        ///   LASX: XVPCNT_H Xd, Xj
        /// </summary>
        public static Vector256<ushort> PopCount(Vector256<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvpcnt_w(int32x8_t a)
        ///   LASX: XVPCNT_W Xd, Xj
        /// </summary>
        public static Vector256<int> PopCount(Vector256<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvpcnt_w(uint32x8_t a)
        ///   LASX: XVPCNT_W Xd, Xj
        /// </summary>
        public static Vector256<uint> PopCount(Vector256<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvpcnt_d(int64x4_t a)
        ///   LASX: XVPCNT_D Xd, Xj
        /// </summary>
        public static Vector256<long> PopCount(Vector256<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvpcnt_d(uint64x4_t a)
        ///   LASX: XVPCNT_D Xd, Xj
        /// </summary>
        public static Vector256<ulong> PopCount(Vector256<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int32x8_t xvperm_w(int32x8_t vec, int32x8_t idx)
        ///   LASX: XVPERM.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> VectorShuffle(Vector256<int> vector, Vector256<int> indexs) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  uint32x8_t xvperm_w(uint32x8_t vec, uint32x8_t idx)
        ///   LASX: XVPERM.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> VectorShuffle(Vector256<uint> vector, Vector256<uint> indexs) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int64x4_t xvpermi_w(int64x4_t vec, uint8_t idx)
        ///   LASX: XVPERMI.D Xd.4D, Xj.4D, ui8
        /// </summary>
        public static Vector256<long> VectorShuffle(Vector256<long> vector, const byte indexs) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  uint64x4_t xvpermi_w(uint64x4_t vec, uint8_t idx)
        ///   LASX: XVPERMI.D Xd.4D, Xj.4D, ui8
        /// </summary>
        public static Vector256<ulong> VectorShuffle(Vector256<ulong> vector, const byte indexs) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int128x2_t xvpermi_w(int128x2_t vec0, int128x2_t vec1, uint8_t idx)
        ///   LASX: XVPERMI.D Xd.2Q, Xj.2Q, ui8                    ///NOTE: The Xd is both input and output.
        /// </summary>
        public static Vector256<long> VectorShuffle(Vector256<long> vector0, Vector256<long> vector1, const byte indexs) { throw new PlatformNotSupportedException(); }

        ///// <summary>
        /////  uint8x32_t xvshuf_b(uint8x32_t vec, uint8x32_t idx)
        /////   LASX: XVSHUF.B Xd.32B, Xj.32B, Xk.32B, Xa.32B
        ///// </summary>
        //public static Vector256<byte> VectorShuffleEach128(Vector256<byte> vector, Vector256<byte> indexs) { throw new PlatformNotSupportedException(); }

        ///// <summary>
        /////  int8x32_t xvshuf_b(int8x32_t vec, int8x32_t idx)
        /////   LASX: XVSHUF.B Xd.32B, Xj.32B, Xk.32B, Xa.32B
        ///// </summary>
        //public static Vector256<sbyte> VectorShuffleEach128(Vector256<sbyte> vector, Vector256<sbyte> indexs) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  uint8x32_t xvshuf_b(uint8x32_t vec0, uint8x32_t vec1, uint8x32_t idx)
        ///   LASX: XVSHUF.B Xd.32B, Xj.32B, Xk.32B, Xa.32B
        /// </summary>
        public static Vector256<byte> VectorShuffleEach128(Vector256<byte> vector0, Vector256<byte> vector1, Vector256<byte> indexs) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int8x32_t xvshuf_b(int8x32_t vec0, int8x32_t vec1, int8x32_t idx)
        ///   LASX: XVSHUF.B Xd.32B, Xj.32B, Xk.32B, Xa.32B
        /// </summary>
        public static Vector256<sbyte> VectorShuffleEach128(Vector256<sbyte> vector0, Vector256<sbyte> vector1, Vector256<sbyte> indexs) { throw new PlatformNotSupportedException(); }

        ///// <summary>
        /////  int16x16_t xvshuf_h(int16x16_t vec, int16x16_t idx)
        /////   LASX: XVSHUF.H Xd.16H, Xj.16H, Xk.16H                //NOTE: Xd is both input and output while input as index.
        ///// </summary>
        //public static Vector256<short> VectorShuffleEach128(Vector256<short> vector, Vector256<short> indexs) { throw new PlatformNotSupportedException(); }

        ///// <summary>
        /////  uint16x16_t xvshuf_h(uint16x16_t vec, uint16x16_t idx)
        /////   LASX: XVSHUF.H Xd.16H, Xj.16H, Xk.16H                                //NOTE: Xd is both input and output while input as index.
        ///// </summary>
        //public static Vector256<ushort> VectorShuffleEach128(Vector256<ushort> vector, Vector256<ushort> indexs) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int16x16_t xvshuf_h(int16x16_t vec0, int16x16_t vec1, int16x16_t idx)
        ///   LASX: XVSHUF.H Xd.16H, Xj.16H, Xk.16H                                //NOTE: Xd is both input and output while input as index.
        /// </summary>
        public static Vector256<short> VectorShuffleEach128(Vector256<short> vector0, Vector256<short> vector1, Vector256<short> indexs) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  uint16x16_t xvshuf_h(uint16x16_t vecj, uint16x16_t veck, uint16x16_t idx)
        ///   LASX: XVSHUF.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> VectorShuffleEach128(Vector256<ushort> vector0, Vector256<ushort> vector1, Vector256<ushort> indexs) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int32x8_t xvpermi_w(int32x8_t vec, uint8_t idx)
        ///   LASX: XVPERMI.W Xd.8W, Xj.8W, ui8
        /// </summary>
        public static Vector256<int> VectorShuffleEach128(Vector256<int> vector, const byte indexs) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  uint32x8_t xvpermi_w(uint32x8_t vec, uint8_t idx)
        ///   LASX: XVPERMI.W Xd.8W, Xj.8W, ui8
        /// </summary>
        public static Vector256<uint> VectorShuffleEach128(Vector256<uint> vector, const byte indexs) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int32x8_t xvshuf_w(int32x8_t vec0, int32x8_t vec1, int32x8_t idx)
        ///   LASX: XVSHUF.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> VectorShuffleEach128(Vector256<int> vector0, Vector256<int> vector1, Vector256<int> indexs) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  uint32x8_t xvshuf_w(uint32x8_t vecj, uint32x8_t veck, uint32x8_t idx)
        ///   LASX: XVSHUF.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> VectorShuffleEach128(Vector256<uint> vector0, Vector256<uint> vector1, Vector256<uint> indexs) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int64x4_t xvshuf_d(int64x4_t vec0, int64x4_t vec1, int64x4_t idx)
        ///   LASX: XVSHUF.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> VectorShuffleEach128(Vector256<long> vector0, Vector256<long> vector1, Vector256<long> indexs) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  uint64x4_t xvshuf_d(uint64x4_t vecj, uint64x4_t veck, uint64x4_t idx)
        ///   LASX: XVSHUF.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> VectorShuffleEach128(Vector256<ulong> vector0, Vector256<ulong> vector1, Vector256<ulong> indexs) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int8x32_t xvshuf4i_b(int8x32_t vec, uint8_t idx)
        ///   LASX: XVSHUF4I.B Xd.32B, Xj.32B, ui8
        /// </summary>
        public static Vector256<sbyte> VectorShuffleBy4Elements(Vector256<sbyte> vector, byte indexs) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  uint8x32_t xvshuf4i_b(uint8x32_t vec, uint8_t idx)
        ///   LASX: XVSHUF4I.B Xd.32B, Xj.32B, ui8
        /// </summary>
        public static Vector256<byte> VectorShuffleBy4Elements(Vector256<byte> vector, byte indexs) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int16x16_t xvshuf4i_h(int16x16_t vec, uint8_t idx)
        ///   LASX: XVSHUF4I.H Xd.16H, Xj.16H, ui8
        /// </summary>
        public static Vector256<short> VectorShuffleBy4Elements(Vector256<short> vector, byte indexs) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  uint16x16_t xvshuf4i_h(uint16x16_t vec, uint8_t idx)
        ///   LASX: XVSHUF4I.H Xd.16H, Xj.16H, ui8
        /// </summary>
        public static Vector256<ushort> VectorShuffleBy4Elements(Vector256<ushort> vector, byte indexs) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int32x8_t xvshuf4i_w(int32x8_t vec, uint8_t idx)
        ///   LASX: XVSHUF4I.W Xd.8W, Xj.8W, ui8
        /// </summary>
        public static Vector256<int> VectorShuffleBy4Elements(Vector256<int> vector, byte indexs) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  uint32x8_t xvshuf4i_w(uint32x8_t vec, uint8_t idx)
        ///   LASX: XVSHUF4I.W Xd.8W, Xj.8W, ui8
        /// </summary>
        public static Vector256<uint> VectorShuffleBy4Elements(Vector256<uint> vector, byte indexs) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int64x4_t xvshuf4i_d(int64x4_t vec0, uint64x4_t vec1, uint8_t idx)
        ///   LASX: XVSHUF4I.D Xd.4D, Xj.4D, ui4
        /// </summary>
        public static Vector256<long> VectorShuffleBy4Elements(Vector256<long> vector0, Vector256<long> vector1, byte indexs) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  uint64x4_t xvshuf4i_d(uint64x4_t vec0, uint64x4_t vec1, uint8_t idx)
        ///   LASX: XVSHUF4I.D Xd.4D, Xj.4D, ui4
        /// </summary>
        public static Vector256<ulong> VectorShuffleBy4Elements(Vector256<ulong> vector0, Vector256<ulong> vector1, byte indexs) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int8x32_t xvilvl_b(int8x32_t vec0, int8x32_t vec1)
        ///   LASX: XVILVL.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> VectorElementsFusionLowerEach128(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  uint8x32_t xvilvl_b(uint8x32_t vec0, uint8x32_t vec1)
        ///   LASX: XVILVL.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> VectorElementsFusionLowerEach128(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int16x16_t xvilvl_h(int16x16_t vec0, int16x16_t vec1)
        ///   LASX: XVILVL.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> VectorElementsFusionLowerEach128(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  uint16x16_t xvilvl_h(uint16x16_t vec0, uint16x16_t vec1)
        ///   LASX: XVILVL.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> VectorElementsFusionLowerEach128(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int32x8_t xvilvl_w(int32x8_t vec0, int32x8_t vec1)
        ///   LASX: XVILVL.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> VectorElementsFusionLowerEach128(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  uint32x8_t xvilvl_w(uint32x8_t vec0, uint32x8_t vec1)
        ///   LASX: XVILVL.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> VectorElementsFusionLowerEach128(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int64x4_t xvilvl_d(int64x4_t vec0, int64x4_t vec1)
        ///   LASX: XVILVL.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> VectorElementsFusionLowerEach128(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  uint64x4_t xvilvl_d(uint64x4_t vec0, uint64x4_t vec1)
        ///   LASX: XVILVL.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> VectorElementsFusionLowerEach128(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int8x32_t xvilvh_b(int8x32_t vec0, int8x32_t vec1)
        ///   LASX: XVILVH.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> VectorElementsFusionHightEach128(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  uint8x32_t xvilvh_b(uint8x32_t vec0, uint8x32_t vec1)
        ///   LASX: XVILVH.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> VectorElementsFusionHightEach128(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int16x16_t xvilvh_h(int16x16_t vec0, int16x16_t vec1)
        ///   LASX: XVILVH.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> VectorElementsFusionHightEach128(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  uint16x16_t xvilvh_h(uint16x16_t vec0, uint16x16_t vec1)
        ///   LASX: XVILVH.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> VectorElementsFusionHightEach128(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int32x8_t xvilvh_w(int32x8_t vec0, int32x8_t vec1)
        ///   LASX: XVILVH.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> VectorElementsFusionHightEach128(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  uint32x8_t xvilvh_w(uint32x8_t vec0, uint32x8_t vec1)
        ///   LASX: XVILVH.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> VectorElementsFusionHightEach128(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int64x4_t xvilvh_d(int64x4_t vec0, int64x4_t vec1)
        ///   LASX: XVILVH.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> VectorElementsFusionHightEach128(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  uint64x4_t xvilvh_d(uint64x4_t vec0, uint64x4_t vec1)
        ///   LASX: XVILVH.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> VectorElementsFusionHightEach128(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int8x32_t xvpackev_b(int8x32_t vec0, int8x32_t vec1)
        ///   LASX: XVPACKEV.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> VectorElementsFusionEven(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  uint8x32_t xvpackev_b(uint8x32_t vec0, uint8x32_t vec1)
        ///   LASX: XVPACKEV.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> VectorElementsFusionEven(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int16x16_t xvpackev_h(int16x16_t vec0, int16x16_t vec1)
        ///   LASX: XVPACKEV.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> VectorElementsFusionEven(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  uint16x16_t xvpackev_h(uint16x16_t vec0, uint16x16_t vec1)
        ///   LASX: XVPACKEV.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> VectorElementsFusionEven(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int32x8_t xvpackev_w(int32x8_t vec0, int32x8_t vec1)
        ///   LASX: XVPACKEV.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> VectorElementsFusionEven(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  uint32x8_t xvpackev_w(uint32x8_t vec0, uint32x8_t vec1)
        ///   LASX: XVPACKEV.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> VectorElementsFusionEven(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int64x4_t xvpackev_d(int64x4_t vec0, int64x4_t vec1)
        ///   LASX: XVPACKEV.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> VectorElementsFusionEven(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  uint64x4_t xvpackev_d(uint64x4_t vec0, uint64x4_t vec1)
        ///   LASX: XVPACKEV.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> VectorElementsFusionEven(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int8x32_t xvpackod_b(int8x32_t vec0, int8x32_t vec1)
        ///   LASX: XVPACKOD.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> VectorElementsFusionOdd(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  uint8x32_t xvpackod_b(uint8x32_t vec0, uint8x32_t vec1)
        ///   LASX: XVPACKOD.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> VectorElementsFusionOdd(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int16x16_t xvpackod_h(int16x16_t vec0, int16x16_t vec1)
        ///   LASX: XVPACKOD.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> VectorElementsFusionOdd(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  uint16x16_t xvpackod_h(uint16x16_t vec0, uint16x16_t vec1)
        ///   LASX: XVPACKOD.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> VectorElementsFusionOdd(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int32x8_t xvpackod_w(int32x8_t vec0, int32x8_t vec1)
        ///   LASX: XVPACKOD.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> VectorElementsFusionOdd(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  uint32x8_t xvpackod_w(uint32x8_t vec0, uint32x8_t vec1)
        ///   LASX: XVPACKOD.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> VectorElementsFusionOdd(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int64x4_t xvpackod_d(int64x4_t vec0, int64x4_t vec1)
        ///   LASX: XVPACKOD.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> VectorElementsFusionOdd(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  uint64x4_t xvpackod_d(uint64x4_t vec0, uint64x4_t vec1)
        ///   LASX: XVPACKOD.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> VectorElementsFusionOdd(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int8x32_t xvpickev_b(int8x32_t vec0, int8x32_t vec1)
        ///   LASX: XVPICKEV.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> VectorEvenElementsJoinEach128(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  uint8x32_t xvpickev_b(uint8x32_t vec0, uint8x32_t vec1)
        ///   LASX: XVPICKEV.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> VectorEvenElementsJoinEach128(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int16x16_t xvpickev_h(int16x16_t vec0, int16x16_t vec1)
        ///   LASX: XVPICKEV.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> VectorEvenElementsJoinEach128(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  uint16x16_t xvpickev_h(uint16x16_t vec0, uint16x16_t vec1)
        ///   LASX: XVPICKEV.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> VectorEvenElementsJoinEach128(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int32x8_t xvpickev_w(int32x8_t vec0, int32x8_t vec1)
        ///   LASX: XVPICKEV.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> VectorEvenElementsJoinEach128(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  uint32x8_t xvpickev_w(uint32x8_t vec0, uint32x8_t vec1)
        ///   LASX: XVPICKEV.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> VectorEvenElementsJoinEach128(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int64x4_t xvpickev_d(int64x4_t vec0, int64x4_t vec1)
        ///   LASX: XVPICKEV.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> VectorEvenElementsJoinEach128(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  uint64x4_t xvpickev_d(uint64x4_t vec0, uint64x4_t vec1)
        ///   LASX: XVPICKEV.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> VectorEvenElementsJoinEach128(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int8x32_t xvpickod_b(int8x32_t vec0, int8x32_t vec1)
        ///   LASX: XVPICKOD.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> VectorOddElementsJoinEach128(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  uint8x32_t xvpickod_b(uint8x32_t vec0, uint8x32_t vec1)
        ///   LASX: XVPICKOD.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> VectorOddElementsJoinEach128(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int16x16_t xvpickod_h(int16x16_t vec0, int16x16_t vec1)
        ///   LASX: XVPICKOD.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> VectorOddElementsJoinEach128(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  uint16x16_t xvpickod_h(uint16x16_t vec0, uint16x16_t vec1)
        ///   LASX: XVPICKOD.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> VectorOddElementsJoinEach128(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int32x8_t xvpickod_w(int32x8_t vec0, int32x8_t vec1)
        ///   LASX: XVPICKOD.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> VectorOddElementsJoinEach128(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  uint32x8_t xvpickod_w(uint32x8_t vec0, uint32x8_t vec1)
        ///   LASX: XVPICKOD.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> VectorOddElementsJoinEach128(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int64x4_t xvpickod_d(int64x4_t vec0, int64x4_t vec1)
        ///   LASX: XVPICKOD.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> VectorOddElementsJoinEach128(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  uint64x4_t xvpickod_d(uint64x4_t vec0, uint64x4_t vec1)
        ///   LASX: XVPICKOD.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> VectorOddElementsJoinEach128(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  uint8x32_t xvrepl128vei(uint8x32_t vector, uint8_t idx)
        ///   LASX: XVREPL128VEI_B Xd.32B, Xj.32B, ui4
        /// </summary>
        public static Vector256<byte> VectorElementReplicateEach128(Vector256<byte> vector, const byte elementIndexe) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int8x32_t xvrepl128vei(int8x32_t vector, uint8_t idx)
        ///   LASX: XVREPL128VEI_B Xd.32B, Xj.32B, ui4
        /// </summary>
        public static Vector256<sbyte> VectorElementReplicateEach128(Vector256<sbyte> vector, const byte elementIndexe) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int16x16_t xvrepl128vei(int16x16_t vector, uint8_t idx)
        ///   LASX: XVREPL128VEI_H Xd.16H, Xj.16H, ui3
        /// </summary>
        public static Vector256<short> VectorElementReplicateEach128(Vector256<short> vector, const byte elementIndexe) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  uint16x16_t xvrepl128vei(uint16x16_t vector, uint8_t idx)
        ///   LASX: XVREPL128VEI_H Xd.16H, Xj.16H, ui3
        /// </summary>
        public static Vector256<ushort> VectorElementReplicateEach128(Vector256<ushort> vector, const byte elementIndexe) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int32x8_t xvrepl128vei(int32x8_t vector, uint8_t idx)
        ///   LASX: XVREPL128VEII_W Xd.8W, Xj.8W, ui2
        /// </summary>
        public static Vector256<int> VectorElementReplicateEach128(Vector256<int> vector, const byte elementIndexe) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  uint32x8_t xvrepl128vei(uint32x8_t vector, uint8_t idx)
        ///   LASX: XVREPL128VEII_W Xd.8W, Xj.8W, ui2
        /// </summary>
        public static Vector256<uint> VectorElementReplicateEach128(Vector256<uint> vector, const byte elementIndexe) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int64x4_t xvrepl128vei(int64x4_t vector, uint8_t idx)
        ///   LASX: XVREPL128VEII_D Xd.4D, Xj.4D, ui1
        /// </summary>
        public static Vector256<long> VectorElementReplicateEach128(Vector256<long> vector, const byte elementIndexe) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  uint64x4_t xvrepl128vei(uint64x4_t vector, uint8_t idx)
        ///   LASX: XVREPL128VEII_D Xd.4D, Xj.4D, ui1
        /// </summary>
        public static Vector256<ulong> VectorElementReplicateEach128(Vector256<ulong> vector, const byte elementIndexe) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int8x32_t xvextrins_b(int8x32_t vec, uint8_t idx)
        ///   LASX: XVEXTRINS.B Xd.32B, Xj.32B, ui8
        /// </summary>
        public static Vector258<sbyte> UpdateOneVectorElementEach128(Vector258<sbyte> vector, const byte indexs) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  uint8x32_t xvextrins_b(uint8x32_t vec, uint8_t idx)
        ///   LASX: XVEXTRINS.B Xd.32B, Xj.32B, ui8
        /// </summary>
        public static Vector258<byte> UpdateOneVectorElementEach128(Vector258<byte> vector, const byte indexs) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int16x16_t xvextrins_h(int16x16_t vec, uint8_t idx)
        ///   LASX: XVEXTRINS.H Xd.16H, Xj.16H, ui8
        /// </summary>
        public static Vector258<short> UpdateOneVectorElementEach128(Vector258<short> vector, const byte indexs) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  uint16x16_t xvextrins_h(uint16x16_t vec, uint8_t idx)
        ///   LASX: XVEXTRINS.H Xd.16H, Xj.16H, ui8
        /// </summary>
        public static Vector258<ushort> UpdateOneVectorElementEach128(Vector258<ushort> vector, const byte indexs) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int32x8_t xvextrins_w(int32x8_t vec, uint8_t idx)
        ///   LASX: XVEXTRINS.W Xd.8W, Xj.8W, ui8
        /// </summary>
        public static Vector258<int> UpdateOneVectorElementEach128(Vector258<int> vector, const byte indexs) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  uint32x8_t xvextrins_w(uint32x8_t vec, uint8_t idx)
        ///   LASX: XVEXTRINS.W Xd.8W, Xj.8W, ui8
        /// </summary>
        public static Vector258<uint> UpdateOneVectorElementEach128(Vector258<uint> vector, const byte indexs) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int64x4_t xvextrins_d(int64x4_t vec, uint8_t idx)
        ///   LASX: XVEXTRINS.D Xd.4D, Xj.4D, ui8
        /// </summary>
        public static Vector258<long> UpdateOneVectorElementEach128(Vector258<long> vector, const byte indexs) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  uint64x4_t xvextrins_d(uint64x4_t vec, uint8_t idx)
        ///   LASX: XVEXTRINS.D Xd.4D, Xj.4D, ui8
        /// </summary>
        public static Vector258<ulong> UpdateOneVectorElementEach128(Vector258<ulong> vector, const byte indexs) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int8x32_t xvsigncov_b(int8x32_t sign, int8x32_t data)
        ///   LASX: XVSIGNCOV.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector258<sbyte> VectorElementNegatedBySign(Vector258<sbyte> sign, Vector258<sbyte> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int16x16_t xvsigncov_h(int16x16_t sign, int16x16_t data)
        ///   LASX: XVSIGNCOV.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector258<short> VectorElementNegatedBySign(Vector258<short> sign, Vector258<short> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int32x8_t xvsigncov_w(int32x8_t sign, int32x8_t data)
        ///   LASX: XVSIGNCOV.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector258<int> VectorElementNegatedBySign(Vector258<int> sign, Vector258<int> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///  int64x4_t xvsigncov_d(int64x4_t sign, int64x4_t data)
        ///   LASX: XVSIGNCOV.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector258<long> VectorElementNegatedBySign(Vector258<long> sign, Vector258<long> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvbitclri_b(int8x32_t a, const int n)
        ///   LASX: XVBITCLRI.B Xd.32B, Xj.32B, ui3
        /// </summary>
        public static Vector256<sbyte> VectorElementBitClear(Vector256<sbyte> value, const byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvbitclri_b(uint8x32_t a, const int n)
        ///   LASX: XVBITCLRI.B Xd.32B, Xj.32B, ui3
        /// </summary>
        public static Vector256<byte> VectorElementBitClear(Vector256<byte> value, const byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvbitclri_h(int16x16_t a, const int n)
        ///   LASX: XVBITCLRI.H Xd.16H, Xj.16H, ui4
        /// </summary>
        public static Vector256<short> VectorElementBitClear(Vector256<short> value, const byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvbitclri_h(uint16x16_t a, const int n)
        ///   LASX: XVBITCLRI.H Xd.16H, Xj.16H, ui4
        /// </summary>
        public static Vector256<ushort> VectorElementBitClear(Vector256<ushort> value, const byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvbitclri_w(uint32x8_t a, const int n)
        ///   LASX: XVBITCLRI.W Xd.4W, Xj.4W, ui5
        /// </summary>
        public static Vector256<int> VectorElementBitClear(Vector256<int> value, const byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvbitclri_w(uint32x8_t a, const int n)
        ///   LASX: XVBITCLRI.W Xd.4W, Xj.4W, ui5
        /// </summary>
        public static Vector256<uint> VectorElementBitClear(Vector256<uint> value, const byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvbitclri_d(int64x4_t a, const int n)
        ///   LASX: XVBITCLRI.D Xd.4D, Xj.4D, ui6
        /// </summary>
        public static Vector256<long> VectorElementBitClear(Vector256<long> value, const byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvbitclri_d(uint64x4_t a, const int n)
        ///   LASX: XVBITCLRI.D Xd.4D, Xj.4D, ui6
        /// </summary>
        public static Vector256<ulong> VectorElementBitClear(Vector256<ulong> value, const byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvbitclr_b(int8x32_t a, int8x32_t b)
        ///   LASX: XVBITCLR.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> VectorElementBitClear(Vector256<sbyte> value, Vector256<sbyte> index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvbitclr_b(uint8x32_t a, uint8x32_t b)
        ///   LASX: XVBITCLR.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> VectorElementBitClear(Vector256<byte> value, Vector256<byte> index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvbitclr_h(int16x16_t value, int16x16_t index)
        ///   LASX: XVBITCLR.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> VectorElementBitClear(Vector256<short> value, Vector256<short> index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvbitclr_h(uint16x16_t value, uint16x16_t index)
        ///   LASX: XVBITCLR.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> VectorElementBitClear(Vector256<ushort> value, Vector256<ushort> index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvbitclr_w(int32x8_t value, int32x8_t index)
        ///   LASX: XVBITCLR.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> VectorElementBitClear(Vector256<int> value, Vector256<int> index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvbitclr_w(uint32x8_t value, uint32x8_t index)
        ///   LASX: XVBITCLR.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> VectorElementBitClear(Vector256<uint> value, Vector256<uint> index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvbitclr_d(int64x4_t value, int64x4_t index)
        ///   LASX: XVBITCLR.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> VectorElementBitClear(Vector256<long> value, Vector256<long> index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvbitclr_d(uint64x4_t value, uint64x4_t index)
        ///   LASX: XVBITCLR.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> VectorElementBitClear(Vector256<ulong> value, Vector256<ulong> index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvbitseti_b(int8x32_t a, const int n)
        ///   LASX: XVBITSETI.B Xd.32B, Xj.32B, ui3
        /// </summary>
        public static Vector256<sbyte> VectorElementBitSet(Vector256<sbyte> value, const byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvbitseti_b(uint8x32_t a, const int n)
        ///   LASX: XVBITSETI.B Xd.32B, Xj.32B, ui3
        /// </summary>
        public static Vector256<byte> VectorElementBitSet(Vector256<byte> value, const byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvbitseti_h(int16x16_t a, const int n)
        ///   LASX: XVBITSETI.H Xd.16H, Xj.16H, ui4
        /// </summary>
        public static Vector256<short> VectorElementBitSet(Vector256<short> value, const byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvbitseti_h(uint16x16_t a, const int n)
        ///   LASX: XVBITSETI.H Xd.16H, Xj.16H, ui4
        /// </summary>
        public static Vector256<ushort> VectorElementBitSet(Vector256<ushort> value, const byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvbitseti_w(uint32x8_t a, const int n)
        ///   LASX: XVBITSETI.W Xd.4W, Xj.4W, ui5
        /// </summary>
        public static Vector256<int> VectorElementBitSet(Vector256<int> value, const byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvbitseti_w(uint32x8_t a, const int n)
        ///   LASX: XVBITSETI.W Xd.4W, Xj.4W, ui5
        /// </summary>
        public static Vector256<uint> VectorElementBitSet(Vector256<uint> value, const byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvbitseti_d(int64x4_t a, const int n)
        ///   LASX: XVBITSETI.D Xd.4D, Xj.4D, ui6
        /// </summary>
        public static Vector256<long> VectorElementBitSet(Vector256<long> value, const byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvbitseti_d(uint64x4_t a, const int n)
        ///   LASX: XVBITSETI.D Xd.4D, Xj.4D, ui6
        /// </summary>
        public static Vector256<ulong> VectorElementBitSet(Vector256<ulong> value, const byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvbitset_b(int8x32_t a, int8x32_t b)
        ///   LASX: XVBITSET.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> VectorElementBitSet(Vector256<sbyte> value, Vector256<sbyte> index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvbitset_b(uint8x32_t a, uint8x32_t b)
        ///   LASX: XVBITSET.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> VectorElementBitSet(Vector256<byte> value, Vector256<byte> index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvbitset_h(int16x16_t value, int16x16_t index)
        ///   LASX: XVBITSET.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> VectorElementBitSet(Vector256<short> value, Vector256<short> index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvbitset_h(uint16x16_t value, uint16x16_t index)
        ///   LASX: XVBITSET.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> VectorElementBitSet(Vector256<ushort> value, Vector256<ushort> index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvbitset_w(int32x8_t value, int32x8_t index)
        ///   LASX: XVBITSET.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> VectorElementBitSet(Vector256<int> value, Vector256<int> index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvbitset_w(uint32x8_t value, uint32x8_t index)
        ///   LASX: XVBITSET.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> VectorElementBitSet(Vector256<uint> value, Vector256<uint> index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvbitset_d(int64x4_t value, int64x4_t index)
        ///   LASX: XVBITSET.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> VectorElementBitSet(Vector256<long> value, Vector256<long> index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvbitset_d(uint64x4_t value, uint64x4_t index)
        ///   LASX: XVBITSET.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> VectorElementBitSet(Vector256<ulong> value, Vector256<ulong> index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvbitrevi_b(int8x32_t a, const int n)
        ///   LASX: XVBITREVI.B Xd.32B, Xj.32B, ui3
        /// </summary>
        public static Vector256<sbyte> VectorElementBitRevert(Vector256<sbyte> value, const byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvbitrevi_b(uint8x32_t a, const int n)
        ///   LASX: XVBITREVI.B Xd.32B, Xj.32B, ui3
        /// </summary>
        public static Vector256<byte> VectorElementBitRevert(Vector256<byte> value, const byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvbitrevi_h(int16x16_t a, const int n)
        ///   LASX: XVBITREVI.H Xd.16H, Xj.16H, ui4
        /// </summary>
        public static Vector256<short> VectorElementBitRevert(Vector256<short> value, const byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvbitrevi_h(uint16x16_t a, const int n)
        ///   LASX: XVBITREVI.H Xd.16H, Xj.16H, ui4
        /// </summary>
        public static Vector256<ushort> VectorElementBitRevert(Vector256<ushort> value, const byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvbitrevi_w(uint32x8_t a, const int n)
        ///   LASX: XVBITREVI.W Xd.8W, Xj.8W, ui5
        /// </summary>
        public static Vector256<int> VectorElementBitRevert(Vector256<int> value, const byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvbitrevi_w(uint32x8_t a, const int n)
        ///   LASX: XVBITREVI.W Xd.8W, Xj.8W, ui5
        /// </summary>
        public static Vector256<uint> VectorElementBitRevert(Vector256<uint> value, const byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvbitrevi_d(int64x4_t a, const int n)
        ///   LASX: XVBITREVI.D Xd.4D, Xj.4D, ui6
        /// </summary>
        public static Vector256<long> VectorElementBitRevert(Vector256<long> value, const byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvbitrevi_d(uint64x4_t a, const int n)
        ///   LASX: XVBITREVI.D Xd.4D, Xj.4D, ui6
        /// </summary>
        public static Vector256<ulong> VectorElementBitRevert(Vector256<ulong> value, const byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvbitrev_b(int8x32_t a, int8x32_t b)
        ///   LASX: XVBITREV.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> VectorElementBitRevert(Vector256<sbyte> value, Vector256<sbyte> index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvbitrev_b(uint8x32_t a, uint8x32_t b)
        ///   LASX: XVBITREV.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> VectorElementBitRevert(Vector256<byte> value, Vector256<byte> index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvbitrev_h(int16x16_t value, int16x16_t index)
        ///   LASX: XVBITREV.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> VectorElementBitRevert(Vector256<short> value, Vector256<short> index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvbitrev_h(uint16x16_t value, uint16x16_t index)
        ///   LASX: XVBITREV.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> VectorElementBitRevert(Vector256<ushort> value, Vector256<ushort> index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvbitrev_w(int32x8_t value, int32x8_t index)
        ///   LASX: XVBITREV.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> VectorElementBitRevert(Vector256<int> value, Vector256<int> index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvbitrev_w(uint32x8_t value, uint32x8_t index)
        ///   LASX: XVBITREV.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> VectorElementBitRevert(Vector256<uint> value, Vector256<uint> index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvbitrev_d(int64x4_t value, int64x4_t index)
        ///   LASX: XVBITREV.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> VectorElementBitRevert(Vector256<long> value, Vector256<long> index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvbitrev_d(uint64x4_t value, uint64x4_t index)
        ///   LASX: XVBITREV.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> VectorElementBitRevert(Vector256<ulong> value, Vector256<ulong> index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvfrstp_b(int8x32_t value, int8x32_t save)
        ///   LASX: XVFRSTP.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> IndexOfFirstNegativeElementEach128(Vector256<sbyte> value, Vector256<sbyte> save) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvfrstp_h(int16x16_t value, int16x16_t save)
        ///   LASX: XVFRSTP.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> IndexOfFirstNegativeElementEach128(Vector256<short> value, Vector256<short> save) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvfrstpi_b(int8x32_t value, uint8_t save)
        ///   LASX: XVFRSTPI.B Xd.32B, Xj.32B, ui4
        /// </summary>
        public static Vector256<sbyte> IndexOfFirstNegativeElementEach128(Vector256<sbyte> value, const byte save) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvfrstpi_h(int16x16_t value, uint8_t save)
        ///   LASX: XVFRSTPI.H Xd.16H, Xj.16H, ui3
        /// </summary>
        public static Vector256<short> IndexOfFirstNegativeElementEach128(Vector256<short> value, const byte save) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x32_t xvsat_b(int8x32_t value, uint8_t ui3)
        ///   LASX: XVSAT.B Xd.32B, Xj.32B, ui3
        /// </summary>
        public static Vector256<sbyte> VectorSaturate(Vector256<sbyte> value, [ConstantExpected(Max = (byte)(7))] byte bits) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x16_t xvsat_h(int16x16_t value, uint8_t ui4)
        ///   LASX: XVSAT.H Xd.16H, Xj.16H, ui4
        /// </summary>
        public static Vector256<short> VectorSaturate(Vector256<short> value, [ConstantExpected(Max = (byte)(15))] byte bits) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t xvsat_w(int32x8_t value, uint8_t ui5)
        ///   LASX: XVSAT.W Xd.8W, Xj.8W, ui5
        /// </summary>
        public static Vector256<int> VectorSaturate(Vector256<int> value, [ConstantExpected(Max = (byte)(31))] byte bits) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t xvsat_d(int64x4_t value, uint8_t ui6)
        ///   LASX: XVSAT.D Xd.4D, Xj.4D, ui6
        /// </summary>
        public static Vector256<long> VectorSaturate(Vector256<long> value, [ConstantExpected(Max = (byte)(63))] byte bits) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x32_t xvsat_bu(uint8x32_t value, uint8_t ui3)
        ///   LASX: XVSAT.BU Xd.32B, Xj.32B, ui3
        /// </summary>
        public static Vector256<byte> VectorSaturateUnsigned(Vector256<byte> value, [ConstantExpected(Max = (byte)(7))] byte bits) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x16_t xvsat_hu(uint16x16_t value, uint8_t ui4)
        ///   LASX: XVSAT.HU Xd.16H, Xj.16H, ui4
        /// </summary>
        public static Vector256<ushort> VectorSaturateUnsigned(Vector256<ushort> value, [ConstantExpected(Max = (byte)(15))] byte bits) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x8_t xvsat_wu(uint32x8_t value, uint8_t ui5)
        ///   LASX: XVSAT.WU Xd.8W, Xj.8W, ui5
        /// </summary>
        public static Vector256<uint> VectorSaturateUnsigned(Vector256<uint> value, [ConstantExpected(Max = (byte)(31))] byte bits) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x4_t xvsat_du(uint64x4_t value, uint8_t ui6)
        ///   LASX: XVSAT.DU Xd.4D, Xj.4D, ui6
        /// </summary>
        public static Vector256<ulong> VectorSaturateUnsigned(Vector256<ulong> value, [ConstantExpected(Max = (byte)(63))] byte bits) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x8_t vfclass_s(float32x8_t a)
        ///   LASX: XVFCLASS.S Vd.8S, Vj.8S
        /// </summary>
        public static Vector256<int> FloatClass(Vector256<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x4_t vfclass_d(float64x4_t a)
        ///   LASX: XVFCLASS.D Vd.4D, Vj.4D
        /// </summary>
        public static Vector256<long> FloatClass(Vector256<double> value) { throw new PlatformNotSupportedException(); }
    }
}
