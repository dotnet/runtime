// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.LoongArch
{
    /// <summary>
    /// This class provides access to the LSX-128bits hardware instructions via intrinsics
    /// </summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class Lsx : LoongArchBase
    {
        internal Lsx() { }

        public static new bool IsSupported { get => IsSupported; }

        /// <summary>
        /// int8x8_t vadd_b(int8x8_t a, int8x8_t b)
        ///   LSX: VADD.B Vd.8B, Vj.8B, Vk.8B
        /// </summary>
        public static Vector64<sbyte> Add(Vector64<sbyte> left, Vector64<sbyte> right) => Add(left, right);

        /// <summary>
        /// uint8x8_t vadd_b(uint8x8_t a, uint8x8_t b)
        ///   LSX: VADD.B Vd.8B, Vj.8B, Vk.8B
        /// </summary>
        public static Vector64<byte> Add(Vector64<byte> left, Vector64<byte> right) => Add(left, right);

        /// <summary>
        /// int16x4_t vadd_h(int16x4_t a, int16x4_t b)
        ///   LSX: VADD.H Vd.4H, Vj.4H, Vk.4H
        /// </summary>
        public static Vector64<short> Add(Vector64<short> left, Vector64<short> right) => Add(left, right);

        /// <summary>
        /// uint16x4_t vadd_h(uint16x4_t a, uint16x4_t b)
        ///   LSX: VADD.H Vd.4H, Vj.4H, Vk.4H
        /// </summary>
        public static Vector64<ushort> Add(Vector64<ushort> left, Vector64<ushort> right) => Add(left, right);

        /// <summary>
        /// int32x2_t vadd_w(int32x2_t a, int32x2_t b)
        ///   LSX: VADD.W Vd.2W, Vj.2W, Vk.2W
        /// </summary>
        public static Vector64<int> Add(Vector64<int> left, Vector64<int> right) => Add(left, right);

        /// <summary>
        /// uint32x2_t vadd_w(uint32x2_t a, uint32x2_t b)
        ///   LSX: VADD.W Vd.2W, Vj.2W, Vk.2W
        /// </summary>
        public static Vector64<uint> Add(Vector64<uint> left, Vector64<uint> right) => Add(left, right);

        /// <summary>
        /// int64x1_t vadd_d(int64x1_t a, int64x1_t b)
        ///   LSX: VADD.D Vd.D, Vj.D, Vk.D
        /// </summary>
        public static Vector64<long> Add(Vector64<long> left, Vector64<long> right) => Add(left, right);

        /// <summary>
        /// uint64x1_t vadd_d(uint64x1_t a, uint64x1_t b)
        ///   LSX: VADD.D Vd.D, Vj.D, Vk.D
        /// </summary>
        public static Vector64<ulong> Add(Vector64<ulong> left, Vector64<ulong> right) => Add(left, right);

        /// <summary>
        /// float32x2_t vfadd_s(float32x2_t a, float32x2_t b)
        ///   LSX: VFADD.S Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector64<float> Add(Vector64<float> left, Vector64<float> right) => Add(left, right);

        /// <summary>
        /// float64x1_t vfadd_d(float64x1_t a, float64x1_t b)
        ///   LSX: VFADD.D Vd.D, Vj.D, Vk.D
        /// </summary>
        public static Vector64<double> Add(Vector64<double> left, Vector64<double> right) => Add(left, right);

        /// <summary>
        /// int8x16_t vadd_b(int8x16_t a, int8x16_t b)
        ///   LSX: VADD.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> Add(Vector128<sbyte> left, Vector128<sbyte> right) => Add(left, right);

        /// <summary>
        /// uint8x16_t vadd_b(uint8x16_t a, uint8x16_t b)
        ///   LSX: VADD.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> Add(Vector128<byte> left, Vector128<byte> right) => Add(left, right);

        /// <summary>
        /// int16x8_t vadd_h(int16x8_t a, int16x8_t b)
        ///   LSX: VADD.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> Add(Vector128<short> left, Vector128<short> right) => Add(left, right);

        /// <summary>
        /// uint16x8_t vadd_h(uint16x8_t a, uint16x8_t b)
        ///   LSX: VADD.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> Add(Vector128<ushort> left, Vector128<ushort> right) => Add(left, right);

        /// <summary>
        /// int32x4_t vadd_w(int32x4_t a, int32x4_t b)
        ///   LSX: VADD.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<int> Add(Vector128<int> left, Vector128<int> right) => Add(left, right);

        /// <summary>
        /// uint32x4_t vadd_w(uint32x4_t a, uint32x4_t b)
        ///   LSX: VADD.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<uint> Add(Vector128<uint> left, Vector128<uint> right) => Add(left, right);

        /// <summary>
        /// int64x2_t vadd_d(int64x2_t a, int64x2_t b)
        ///   LSX: VADD.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> Add(Vector128<long> left, Vector128<long> right) => Add(left, right);

        /// <summary>
        /// uint64x2_t vadd_d(uint64x2_t a, uint64x2_t b)
        ///   LSX: VADD.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> Add(Vector128<ulong> left, Vector128<ulong> right) => Add(left, right);

        /// <summary>
        /// float32x4_t vfadd_s(float32x4_t a, float32x4_t b)
        ///   LSX: VFADD.S Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<float> Add(Vector128<float> left, Vector128<float> right) => Add(left, right);

        /// <summary>
        /// float64x2_t vfadd_d(float64x2_t a, float64x2_t b)
        ///   LSX: VFADD.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<double> Add(Vector128<double> left, Vector128<double> right) => Add(left, right);

        /// <summary>
        /// int8x8_t vsadd_b(int8x8_t a, uint8x8_t b)
        ///   LSX: VSADD.B Vd.8B, Vj.8B
        /// </summary>
        public static Vector64<sbyte> AddSaturate(Vector64<sbyte> left, Vector64<byte> right) => AddSaturate(left, right);

        /// <summary>
        /// uint8x8_t vsadd_bu(uint8x8_t a, int8x8_t b)
        ///   LSX: VSADD.BU Vd.8B, Vj.8B
        /// </summary>
        public static Vector64<byte> AddSaturate(Vector64<byte> left, Vector64<sbyte> right) => AddSaturate(left, right);

        /// <summary>
        /// int16x4_t vsadd_h(int16x4_t a, uint16x4_t b)
        ///   LSX: VSADD.H Vd.4H, Vj.4H
        /// </summary>
        public static Vector64<short> AddSaturate(Vector64<short> left, Vector64<ushort> right) => AddSaturate(left, right);

        /// <summary>
        /// uint16x4_t vsadd_hu(uint16x4_t a, int16x4_t b)
        ///   LSX: VSADD.HU Vd.4H, Vj.4H
        /// </summary>
        public static Vector64<ushort> AddSaturate(Vector64<ushort> left, Vector64<short> right) => AddSaturate(left, right);

        /// <summary>
        /// int32x2_t vsadd_w(int32x2_t a, uint32x2_t b)
        ///   LSX: VSADD.W Vd.2W, Vj.2W
        /// </summary>
        public static Vector64<int> AddSaturate(Vector64<int> left, Vector64<uint> right) => AddSaturate(left, right);

        /// <summary>
        /// uint32x2_t vsadd_wu(uint32x2_t a, int32x2_t b)
        ///   LSX: VSADD.WU Vd.2W, Vj.2W
        /// </summary>
        public static Vector64<uint> AddSaturate(Vector64<uint> left, Vector64<int> right) => AddSaturate(left, right);

        /// <summary>
        /// int64x1_t vsadd_d(int64x1_t a, uint64x1_t b)
        ///   LSX: VSADD.D Vd.D, Vj.D
        /// </summary>
        public static Vector64<long> AddSaturate(Vector64<long> left, Vector64<ulong> right) => AddSaturate(left, right);

        /// <summary>
        /// uint64x1_t vsadd_du(uint64x1_t a, int64x1_t b)
        ///   LSX: VSADD.DU Vd.D, Vj.D
        /// </summary>
        public static Vector64<ulong> AddSaturate(Vector64<ulong> left, Vector64<long> right) => AddSaturate(left, right);

        /// <summary>
        /// int8x16_t vsadd_b(int8x16_t a, int8x16_t b)
        ///   LSX: VSADD.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> AddSaturate(Vector128<sbyte> left, Vector128<sbyte> right) => AddSaturate(left, right);

        /// <summary>
        /// int16x8_t vsadd_h(int16x8_t a, int16x8_t b)
        ///   LSX: VSADD.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> AddSaturate(Vector128<short> left, Vector128<short> right) => AddSaturate(left, right);

        /// <summary>
        /// int32x4_t vsadd_w(int32x4_t a, int32x4_t b)
        ///   LSX: VSADD.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<int> AddSaturate(Vector128<int> left, Vector128<int> right) => AddSaturate(left, right);

        /// <summary>
        /// int64x2_t vsadd_d(int64x2_t a, int64x2_t b)
        ///   LSX: VSADD.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> AddSaturate(Vector128<long> left, Vector128<long> right) => AddSaturate(left, right);

        /// <summary>
        /// uint8x16_t vsadd_bu(uint8x16_t a, uint8x16_t b)
        ///   LSX: VSADD.BU Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> AddSaturate(Vector128<byte> left, Vector128<byte> right) => AddSaturate(left, right);

        /// <summary>
        /// uint16x8_t vsadd_hu(uint16x8_t a, uint16x8_t b)
        ///   LSX: VSADD.HU Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> AddSaturate(Vector128<ushort> left, Vector128<ushort> right) => AddSaturate(left, right);

        /// <summary>
        /// uint32x4_t vsadd_wu(uint32x4_t a, uint32x4_t b)
        ///   LSX: VSADD.WU Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<uint> AddSaturate(Vector128<uint> left, Vector128<uint> right) => AddSaturate(left, right);

        /// <summary>
        /// uint64x2_t vsadd_du(uint64x2_t a, uint64x2_t b)
        ///   LSX: VSADD.DU Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> AddSaturate(Vector128<ulong> left, Vector128<ulong> right) => AddSaturate(left, right);

        /// <summary>
        /// int16x8_t vhaddw_h_b(int8x16_t a, int8x16_t b)
        ///   LSX: VHADDW.H.B Vd.8H, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<short> AddOddEvenElementsWidening(Vector128<sbyte> left, Vector128<sbyte> right) => AddOddEvenElementsWidening(left, right);

        /// <summary>
        /// uint16x8_t vhaddw_hu_bu(uint8x16_t a, uint8x16_t b)
        ///   LSX: VHADDW.HU.BU Vd.8H, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<ushort> AddOddEvenElementsWidening(Vector128<byte> left, Vector128<byte> right) => AddOddEvenElementsWidening(left, right);

        /// <summary>
        /// int32x4_t vhaddw_w_h(int16x8_t a, int16x8_t b)
        ///   LSX: VHADDW.W.H Vd.4W, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<int> AddOddEvenElementsWidening(Vector128<short> left, Vector128<short> right) => AddOddEvenElementsWidening(left, right);

        /// <summary>
        /// uint32x4_t vhaddw_wu_hu(uint16x8_t a, uint16x8_t b)
        ///   LSX: VHADDW.WU.HU Vd.4W, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<uint> AddOddEvenElementsWidening(Vector128<ushort> left, Vector128<ushort> right) => AddOddEvenElementsWidening(left, right);

        /// <summary>
        /// int64x2_t vhaddw_d_w(int32x4_t a, int32x4_t b)
        ///   LSX: VHADDW.D.W Vd.2D, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<long> AddOddEvenElementsWidening(Vector128<int> left, Vector128<int> right) => AddOddEvenElementsWidening(left, right);

        /// <summary>
        /// uint64x2_t vhaddw_du_wu(uint32x4_t a, uint32x4_t b)
        ///   LSX: VHADDW.DU.WU Vd.2D, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<ulong> AddOddEvenElementsWidening(Vector128<uint> left, Vector128<uint> right) => AddOddEvenElementsWidening(left, right);

        /// <summary>
        /// int128x1_t vhaddw_q_d(int64x2_t a, int64x2_t b)  TODO: long --> longlong 128bits.
        ///   LSX: VHADDW.Q.D Vd.Q, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> AddOddEvenElementsWidening(Vector128<long> left, Vector128<long> right) => AddOddEvenElementsWidening(left, right);

        /// <summary>
        /// uint128x1_t vhaddw_qu_du(uint64x2_t a, uint64x2_t b)
        ///   LSX: VHADDW.QU.DU Vd.Q, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> AddOddEvenElementsWidening(Vector128<ulong> left, Vector128<ulong> right) => AddOddEvenElementsWidening(left, right);

        /// <summary>
        /// int16x8_t vaddwev_h_b(int8x16_t a, int8x16_t b)
        ///   LSX: VADDWEV.H.B Vd.8H, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<short> AddEvenElementsWidening(Vector128<sbyte> left, Vector128<sbyte> right) => AddEvenElementsWidening(left, right);

        /// <summary>
        /// uint16x8_t vaddwev_h_bu(uint8x16_t a, uint8x16_t b)
        ///   LSX: VADDWEV.H.BU Vd.8H, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<ushort> AddEvenElementsWidening(Vector128<byte> left, Vector128<byte> right) => AddEvenElementsWidening(left, right);

        /// <summary>
        /// int32x4_t vaddwev_w_h(int16x8_t a, int16x8_t b)
        ///   LSX: VADDWEV.W.H Vd.4W, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<int> AddEvenElementsWidening(Vector128<short> left, Vector128<short> right) => AddEvenElementsWidening(left, right);

        /// <summary>
        /// uint32x4_t vaddwev_w_hu(uint16x8_t a, uint16x8_t b)
        ///   LSX: VADDWEV.W.HU Vd.4W, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<uint> AddEvenElementsWidening(Vector128<ushort> left, Vector128<ushort> right) => AddEvenElementsWidening(left, right);

        /// <summary>
        /// int64x2_t vaddwev_d_w(int32x4_t a, int32x4_t b)
        ///   LSX: VADDWEV.D.W Vd.2D, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<long> AddEvenElementsWidening(Vector128<int> left, Vector128<int> right) => AddEvenElementsWidening(left, right);

        /// <summary>
        /// uint64x2_t vaddwev_d_wu(uint32x4_t a, uint32x4_t b)
        ///   LSX: VADDWEV.D.WU Vd.2D, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<ulong> AddEvenElementsWidening(Vector128<uint> left, Vector128<uint> right) => AddEvenElementsWidening(left, right);

        /// <summary>
        /// int128x1_t vaddwev_q_d(int64x2_t a, int64x2_t b)  TODO: long --> longlong 128bits.
        ///   LSX: VADDWEV.Q.D Vd.Q, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> AddEvenElementsWidening(Vector128<long> left, Vector128<long> right) => AddEvenElementsWidening(left, right);

        /// <summary>
        /// uint128x1_t vaddwev_q_du(uint64x2_t a, uint64x2_t b)
        ///   LSX: VADDWEV.Q.DU Vd.Q, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> AddEvenElementsWidening(Vector128<ulong> left, Vector128<ulong> right) => AddEvenElementsWidening(left, right);

        /// <summary>
        /// int16x8_t vaddwod_h_b(int8x16_t a, int8x16_t b)
        ///   LSX: VADDWOD.H.B Vd.8H, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<short> AddOddElementsWidening(Vector128<sbyte> left, Vector128<sbyte> right) => AddOddElementsWidening(left, right);

        /// <summary>
        /// uint16x8_t vaddwod_h_bu(uint8x16_t a, uint8x16_t b)
        ///   LSX: VADDWOD.H.BU Vd.8H, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<ushort> AddOddElementsWidening(Vector128<byte> left, Vector128<byte> right) => AddOddElementsWidening(left, right);

        /// <summary>
        /// int32x4_t vaddwod_w_h(int16x8_t a, int16x8_t b)
        ///   LSX: VADDWOD.W.H Vd.4W, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<int> AddOddElementsWidening(Vector128<short> left, Vector128<short> right) => AddOddElementsWidening(left, right);

        /// <summary>
        /// uint32x4_t vaddwod_w_hu(uint16x8_t a, uint16x8_t b)
        ///   LSX: VADDWOD.W.HU Vd.4W, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<uint> AddOddElementsWidening(Vector128<ushort> left, Vector128<ushort> right) => AddOddElementsWidening(left, right);

        /// <summary>
        /// int64x2_t vaddwod_d_w(int32x4_t a, int32x4_t b)
        ///   LSX: VADDWOD.D.W Vd.2D, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<long> AddOddElementsWidening(Vector128<int> left, Vector128<int> right) => AddOddElementsWidening(left, right);

        /// <summary>
        /// uint64x2_t vaddwod_d_wu(uint32x4_t a, uint32x4_t b)
        ///   LSX: VADDWOD.D.WU Vd.2D, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<ulong> AddOddElementsWidening(Vector128<uint> left, Vector128<uint> right) => AddOddElementsWidening(left, right);

        /// <summary>
        /// int128x1_t vaddwod_q_d(int64x2_t a, int64x2_t b)  TODO: long --> longlong 128bits.
        ///   LSX: VADDWOD.Q.D Vd.Q, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> AddOddElementsWidening(Vector128<long> left, Vector128<long> right) => AddOddElementsWidening(left, right);

        /// <summary>
        /// uint128x1_t vaddwod_q_du(uint64x2_t a, uint64x2_t b)
        ///   LSX: VADDWOD.Q.DU Vd.Q, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> AddOddElementsWidening(Vector128<ulong> left, Vector128<ulong> right) => AddOddElementsWidening(left, right);

        /// <summary>
        /// int16x8_t vaddwev_h_bu_b(int8x16_t a, int8x16_t b)
        ///   LSX: VADDWEV.H.BU.B Vd.8H, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<short> AddEvenElementsWidening(Vector128<byte> left, Vector128<sbyte> right) => AddEvenElementsWidening(left, right);

        /// <summary>
        /// int32x4_t vaddwev_w_hu_h(int16x8_t a, int16x8_t b)
        ///   LSX: VADDWEV.W.HU.H Vd.4W, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<int> AddEvenElementsWidening(Vector128<ushort> left, Vector128<short> right) => AddEvenElementsWidening(left, right);

        /// <summary>
        /// int64x2_t vaddwev_d_wu_w(int32x4_t a, int32x4_t b)
        ///   LSX: VADDWEV.D.WU.W Vd.2D, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<long> AddEvenElementsWidening(Vector128<uint> left, Vector128<int> right) => AddEvenElementsWidening(left, right);

        /// <summary>
        /// int128x1_t vaddwev_q_du_d(int64x2_t a, int64x2_t b)  TODO: long --> longlong 128bits.
        ///   LSX: VADDWEV.Q.DU.D Vd.Q, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> AddEvenElementsWidening(Vector128<ulong> left, Vector128<long> right) => AddEvenElementsWidening(left, right);

        /// <summary>
        /// int16x8_t vaddwod_h_bu_b(int8x16_t a, int8x16_t b)
        ///   LSX: VADDWOD.H.BU.B Vd.8H, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<short> AddOddElementsWidening(Vector128<byte> left, Vector128<sbyte> right) => AddOddElementsWidening(left, right);

        /// <summary>
        /// int32x4_t vaddwod_w_hu_h(int16x8_t a, int16x8_t b)
        ///   LSX: VADDWOD.W.HU.H Vd.4W, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<int> AddOddElementsWidening(Vector128<ushort> left, Vector128<short> right) => AddOddElementsWidening(left, right);

        /// <summary>
        /// int64x2_t vaddwod_d_wu_w(int32x4_t a, int32x4_t b)
        ///   LSX: VADDWOD.D.WU.W Vd.2D, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<long> AddOddElementsWidening(Vector128<uint> left, Vector128<int> right) => AddOddElementsWidening(left, right);

        /// <summary>
        /// int128x1_t vaddwod_q_du_d(int64x2_t a, int64x2_t b)  TODO: long --> longlong 128bits.
        ///   LSX: VADDWOD.Q.DU.D Vd.Q, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> AddOddElementsWidening(Vector128<ulong> left, Vector128<long> right) => AddOddElementsWidening(left, right);

        //// TODO: LA-SIMD: add HorizontalSubtract for LA64.

        /// <summary>
        ///   NOTE: this is implemented by multi instructions.
        ///   LSX: VHADDW.H.B Vd.8H, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<long> AddHorizontalElements(Vector128<sbyte> value) => AddHorizontalElements(value);

        /// <summary>
        ///   NOTE: this is implemented by multi instructions.
        ///   LSX: VHADDW.HU.BU Vd.8H, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<ulong> AddHorizontalElements(Vector128<byte> value) => AddHorizontalElements(value);

        /// <summary>
        ///   NOTE: this is implemented by multi instructions.
        ///   LSX: VHADDW.W.H Vd.4W, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<long> AddHorizontalElements(Vector128<short> value) => AddHorizontalElements(value);

        /// <summary>
        ///   NOTE: this is implemented by multi instructions.
        ///   LSX: VHADDW.WU.HU Vd.4W, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ulong> AddHorizontalElements(Vector128<ushort> value) => AddHorizontalElements(value);

        /// <summary>
        ///   NOTE: this is implemented by multi instructions.
        ///   LSX: VHADDW.D.W Vd.2D, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<long> AddHorizontalElements(Vector128<int> value) => AddHorizontalElements(value);

        /// <summary>
        ///   NOTE: this is implemented by multi instructions.
        ///   LSX: VHADDW.DU.WU Vd.2D, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<long> AddHorizontalElements(Vector128<uint> value) => AddHorizontalElements(value);

        /// <summary>
        ///   NOTE: this is implemented by multi instructions.
        ///   LSX: VHADDW.Q.D Vd.Q, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> AddHorizontalElements(Vector128<long> value) => AddHorizontalElements(value);

        /// <summary>
        ///   NOTE: this is implemented by multi instructions.
        ///   LSX: VHADDW.QU.DU Vd.Q, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> AddHorizontalElements(Vector128<ulong> value) => AddHorizontalElements(value);

        /// <summary>
        ///   NOTE: this is implemented by multi instructions.
        ///   LSX: sum_all_float_elements witin vector.
        /// </summary>
        public static Vector64<float> AddHorizontalElements(Vector128<float> value) => AddHorizontalElements(value);

        /// <summary>
        ///   NOTE: this is implemented by multi instructions.
        ///   LSX: sum_all_double_elements witin vector.
        /// </summary>
        public static Vector64<double> AddHorizontalElements(Vector128<double> value) => AddHorizontalElements(value);

        /// <summary>
        /// int8x8_t vsub_b(int8x8_t a, int8x8_t b)
        ///   LSX: VSUB.B Vd.8B, Vj.8B, Vk.8B
        /// </summary>
        public static Vector64<sbyte> Subtract(Vector64<sbyte> left, Vector64<sbyte> right) => Subtract(left, right);

        /// <summary>
        /// uint8x8_t vsub_b(uint8x8_t a, uint8x8_t b)
        ///   LSX: VSUB.B Vd.8B, Vj.8B, Vk.8B
        /// </summary>
        public static Vector64<byte> Subtract(Vector64<byte> left, Vector64<byte> right) => Subtract(left, right);

        /// <summary>
        /// int16x4_t vsub_h(int16x4_t a, int16x4_t b)
        ///   LSX: VSUB.H Vd.4H, Vj.4H, Vk.4H
        /// </summary>
        public static Vector64<short> Subtract(Vector64<short> left, Vector64<short> right) => Subtract(left, right);

        /// <summary>
        /// uint16x4_t vsub_h(uint16x4_t a, uint16x4_t b)
        ///   LSX: VSUB.H Vd.4H, Vj.4H, Vk.4H
        /// </summary>
        public static Vector64<ushort> Subtract(Vector64<ushort> left, Vector64<ushort> right) => Subtract(left, right);

        /// <summary>
        /// int32x2_t vsub_w(int32x2_t a, int32x2_t b)
        ///   LSX: VSUB.W Vd.2W, Vj.2W, Vk.2W
        /// </summary>
        public static Vector64<int> Subtract(Vector64<int> left, Vector64<int> right) => Subtract(left, right);

        /// <summary>
        /// uint32x2_t vsub_w(uint32x2_t a, uint32x2_t b)
        ///   LSX: VSUB.W Vd.2W, Vj.2W, Vk.2W
        /// </summary>
        public static Vector64<uint> Subtract(Vector64<uint> left, Vector64<uint> right) => Subtract(left, right);

        /// <summary>
        /// int64x1_t vsub_d(int64x1_t a, int64x1_t b)
        ///   LSX: VSUB.D Vd.D, Vj.D, Vk.D
        /// </summary>
        public static Vector64<long> Subtract(Vector64<long> left, Vector64<long> right) => Subtract(left, right);

        /// <summary>
        /// uint64x1_t vsub_d(uint64x1_t a, uint64x1_t b)
        ///   LSX: VSUB.D Vd.D, Vj.D, Vk.D
        /// </summary>
        public static Vector64<ulong> Subtract(Vector64<ulong> left, Vector64<ulong> right) => Subtract(left, right);

        /// <summary>
        /// float32x2_t vfsub_s(float32x2_t a, float32x2_t b)
        ///   LSX: VFSUB.S Vd.2S, Vj.2S, Vk.2S
        /// </summary>
        public static Vector64<float> Subtract(Vector64<float> left, Vector64<float> right) => Subtract(left, right);

        /// <summary>
        /// float64x1_t vfsub_d(float64x1_t a, float64x1_t b)
        ///   LSX: VFSUB.D Vd.D, Vj.D, Vk.D
        /// </summary>
        public static Vector64<double> Subtract(Vector64<double> left, Vector64<double> right) => Subtract(left, right);

        /// <summary>
        /// int8x16_t vsub_b(int8x16_t a, int8x16_t b)
        ///   LSX: VSUB.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> Subtract(Vector128<sbyte> left, Vector128<sbyte> right) => Subtract(left, right);

        /// <summary>
        /// uint8x16_t vsub_b(uint8x16_t a, uint8x16_t b)
        ///   LSX: VSUB.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> Subtract(Vector128<byte> left, Vector128<byte> right) => Subtract(left, right);

        /// <summary>
        /// int16x8_t vsub_h(int16x8_t a, int16x8_t b)
        ///   LSX: VSUB.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> Subtract(Vector128<short> left, Vector128<short> right) => Subtract(left, right);

        /// <summary>
        /// uint16x8_t vsub_h(uint16x8_t a, uint16x8_t b)
        ///   LSX: VSUB.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> Subtract(Vector128<ushort> left, Vector128<ushort> right) => Subtract(left, right);

        /// <summary>
        /// int32x4_t vsub_w(int32x4_t a, int32x4_t b)
        ///   LSX: VSUB.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<int> Subtract(Vector128<int> left, Vector128<int> right) => Subtract(left, right);

        /// <summary>
        /// uint32x4_t vsub_w(uint32x4_t a, uint32x4_t b)
        ///   LSX: VSUB.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<uint> Subtract(Vector128<uint> left, Vector128<uint> right) => Subtract(left, right);

        /// <summary>
        /// int64x2_t vsub_d(int64x2_t a, int64x2_t b)
        ///   LSX: VSUB.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> Subtract(Vector128<long> left, Vector128<long> right) => Subtract(left, right);

        /// <summary>
        /// uint64x2_t vsub_d(uint64x2_t a, uint64x2_t b)
        ///   LSX: VSUB.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> Subtract(Vector128<ulong> left, Vector128<ulong> right) => Subtract(left, right);

        /// <summary>
        /// float32x4_t vfsub_s(float32x4_t a, float32x4_t b)
        ///   LSX: VFSUB.S Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<float> Subtract(Vector128<float> left, Vector128<float> right) => Subtract(left, right);

        /// <summary>
        /// float64x2_t vfsub_d(float64x2_t a, float64x2_t b)
        ///   LSX: VFSUB.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<double> Subtract(Vector128<double> left, Vector128<double> right) => Subtract(left, right);

        /// <summary>
        /// int16x8_t vhsubw_h_b(int8x16_t a, int8x16_t b)
        ///   LSX: VHSUBW.H.B Vd.8H, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> SubtractOddEvenElementsWidening(Vector128<sbyte> left, Vector128<sbyte> right) => SubtractOddEvenElementsWidening(left, right);

        /// <summary>
        /// uint16x8_t vhsubw_hu_bu(uint8x16_t a, uint8x16_t b)
        ///   LSX: VHSUBW.HU.BU Vd.8H, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> SubtractOddEvenElementsWidening(Vector128<byte> left, Vector128<byte> right) => SubtractOddEvenElementsWidening(left, right);

        /// <summary>
        /// int32x4_t vhsubw_w_h(int16x8_t a, int16x8_t b)
        ///   LSX: VHSUBW.W.H Vd.4W, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> SubtractOddEvenElementsWidening(Vector128<short> left, Vector128<short> right) => SubtractOddEvenElementsWidening(left, right);

        /// <summary>
        /// uint32x4_t vhsubw_wu_hu(uint16x8_t a, uint16x8_t b)
        ///   LSX: VHSUBW.WU.HU Vd.4W, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> SubtractOddEvenElementsWidening(Vector128<ushort> left, Vector128<ushort> right) => SubtractOddEvenElementsWidening(left, right);

        /// <summary>
        /// int64x2_t vhsubw_d_w(int32x4_t a, int32x4_t b)
        ///   LSX: VHSUBW.D.W Vd.2D, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<int> SubtractOddEvenElementsWidening(Vector128<int> left, Vector128<int> right) => SubtractOddEvenElementsWidening(left, right);

        /// <summary>
        /// uint64x2_t vhsubw_du_wu(uint32x4_t a, uint32x4_t b)
        ///   LSX: VHSUBW.DU.WU Vd.2D, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<uint> SubtractOddEvenElementsWidening(Vector128<uint> left, Vector128<uint> right) => SubtractOddEvenElementsWidening(left, right);

        /// <summary>
        /// int128x1_t vhsubw_q_d(int64x2_t a, int64x2_t b)  TODO: long --> longlong 128bits.
        ///   LSX: VHSUBW.Q.D Vd.Q, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> SubtractOddEvenElementsWidening(Vector128<long> left, Vector128<long> right) => SubtractOddEvenElementsWidening(left, right);

        /// <summary>
        /// uint128x1_t vhsubw_qu_du(uint64x2_t a, uint64x2_t b)
        ///   LSX: VHSUBW.QU.DU Vd.Q, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> SubtractOddEvenElementsWidening(Vector128<ulong> left, Vector128<ulong> right) => SubtractOddEvenElementsWidening(left, right);

        /// <summary>
        /// int8x16_t vssub_b(int8x16_t a, int8x16_t b)
        ///   LSX: VSSUB.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> SubtractSaturate(Vector128<sbyte> left, Vector128<sbyte> right) => SubtractSaturate(left, right);

        /// <summary>
        /// uint8x16_t vssub_bu(uint8x16_t a, uint8x16_t b)
        ///   LSX: VSSUB.BU Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> SubtractSaturate(Vector128<byte> left, Vector128<byte> right) => SubtractSaturate(left, right);

        /// <summary>
        /// int16x8_t vssub_h(int16x8_t a, int16x8_t b)
        ///   LSX: VSSUB.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> SubtractSaturate(Vector128<short> left, Vector128<short> right) => SubtractSaturate(left, right);

        /// <summary>
        /// uint16x8_t vssub_hu(uint16x8_t a, uint16x8_t b)
        ///   LSX: VSSUB.HU Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> SubtractSaturate(Vector128<ushort> left, Vector128<ushort> right) => SubtractSaturate(left, right);

        /// <summary>
        /// int32x4_t vssub_w(int32x4_t a, int32x4_t b)
        ///   LSX: VSSUB.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<int> SubtractSaturate(Vector128<int> left, Vector128<int> right) => SubtractSaturate(left, right);

        /// <summary>
        /// uint32x4_t vssub_wu(uint32x4_t a, uint32x4_t b)
        ///   LSX: VSSUB.WU Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<uint> SubtractSaturate(Vector128<uint> left, Vector128<uint> right) => SubtractSaturate(left, right);

        /// <summary>
        /// int64x2_t vssub_d(int64x2_t a, int64x2_t b)
        ///   LSX: VSSUB.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> SubtractSaturate(Vector128<long> left, Vector128<long> right) => SubtractSaturate(left, right);

        /// <summary>
        /// uint64x2_t vssub_du(uint64x2_t a, uint64x2_t b)
        ///   LSX: VSSUB.DU Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> SubtractSaturate(Vector128<ulong> left, Vector128<ulong> right) => SubtractSaturate(left, right);

        /// <summary>
        /// int8x16_t vmul_b(int8x16_t a, int8x16_t b)
        ///   LSX: VMUL.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> Multiply(Vector128<sbyte> left, Vector128<sbyte> right) => Multiply(left, right);

        /// <summary>
        /// uint8x16_t vmul_b(uint8x16_t a, uint8x16_t b)
        ///   LSX: VMUL.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> Multiply(Vector128<byte> left, Vector128<byte> right) => Multiply(left, right);

        /// <summary>
        /// int16x8_t vmul_h(int16x8_t a, int16x8_t b)
        ///   LSX: VMUL.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> Multiply(Vector128<short> left, Vector128<short> right) => Multiply(left, right);

        /// <summary>
        /// uint16x8_t vmul_h(uint16x8_t a, uint16x8_t b)
        ///   LSX: VMUL.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> Multiply(Vector128<ushort> left, Vector128<ushort> right) => Multiply(left, right);

        /// <summary>
        /// int32x4_t vmul_w(int32x4_t a, int32x4_t b)
        ///   LSX: VMUL.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<int> Multiply(Vector128<int> left, Vector128<int> right) => Multiply(left, right);

        /// <summary>
        /// uint32x4_t vmul(uint32x4_t a, uint32x4_t b)
        ///   LSX: VMUL.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<uint> Multiply(Vector128<uint> left, Vector128<uint> right) => Multiply(left, right);

        /// <summary>
        /// int64x2_t vmul_d(int64x2_t a, int64x2_t b)
        ///   LSX: VMUL.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> Multiply(Vector128<long> left, Vector128<long> right) => Multiply(left, right);

        /// <summary>
        /// uint64x2_t vmul(uint64x2_t a, uint64x2_t b)
        ///   LSX: VMUL.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> Multiply(Vector128<ulong> left, Vector128<ulong> right) => Multiply(left, right);

        /// <summary>
        /// float32x4_t vfmul_s(float32x4_t a, float32x4_t b)
        ///   LSX: VFMUL.S Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<float> Multiply(Vector128<float> left, Vector128<float> right) => Multiply(left, right);

        /// <summary>
        /// float64x2_t vfmul_d(float64x2_t a, float64x2_t b)
        ///   LSX: VFMUL.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<double> Multiply(Vector128<double> left, Vector128<double> right) => Multiply(left, right);

        /// <summary>
        /// int8x16_t vmuh_b(int8x16_t a, int8x16_t b)
        ///   LSX: VMUH.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> MultiplyHight(Vector128<sbyte> left, Vector128<sbyte> right) => MultiplyHight(left, right);

        /// <summary>
        /// uint8x16_t vmuh_b(uint8x16_t a, uint8x16_t b)
        ///   LSX: VMUH.BU Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> MultiplyHight(Vector128<byte> left, Vector128<byte> right) => MultiplyHight(left, right);

        /// <summary>
        /// int16x8_t vmuh_h(int16x8_t a, int16x8_t b)
        ///   LSX: VMUH.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> MultiplyHight(Vector128<short> left, Vector128<short> right) => MultiplyHight(left, right);

        /// <summary>
        /// uint16x8_t vmuh_hu(uint16x8_t a, uint16x8_t b)
        ///   LSX: VMUH.HU Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> MultiplyHight(Vector128<ushort> left, Vector128<ushort> right) => MultiplyHight(left, right);

        /// <summary>
        /// int32x4_t vmuh_w(int32x4_t a, int32x4_t b)
        ///   LSX: VMUL.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<int> MultiplyHight(Vector128<int> left, Vector128<int> right) => MultiplyHight(left, right);

        /// <summary>
        /// uint32x4_t vmuh_wu(uint32x4_t a, uint32x4_t b)
        ///   LSX: VMUH.WU Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<uint> MultiplyHight(Vector128<uint> left, Vector128<uint> right) => MultiplyHight(left, right);

        /// <summary>
        /// int64x2_t vmuh_d(int64x2_t a, int64x2_t b)
        ///   LSX: VMUH.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> MultiplyHight(Vector128<long> left, Vector128<long> right) => MultiplyHight(left, right);

        /// <summary>
        /// uint64x2_t vmuh_du(uint64x2_t a, uint64x2_t b)
        ///   LSX: VMUH.DU Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> MultiplyHight(Vector128<ulong> left, Vector128<ulong> right) => MultiplyHight(left, right);

        /// <summary>
        /// int8x16_t vdiv_b(int8x16_t a, int8x16_t b)
        ///   LSX: VDIV.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> Divide(Vector128<sbyte> left, Vector128<sbyte> right) => Divide(left, right);

        /// <summary>
        /// uint8x16_t vdiv_bu(uint8x16_t a, uint8x16_t b)
        ///   LSX: VDIV.BU Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> Divide(Vector128<byte> left, Vector128<byte> right) => Divide(left, right);

        /// <summary>
        /// int16x8_t vdiv_h(int16x8_t a, int16x8_t b)
        ///   LSX: VDIV.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> Divide(Vector128<short> left, Vector128<short> right) => Divide(left, right);

        /// <summary>
        /// uint16x8_t vdiv_hu(uint16x8_t a, uint16x8_t b)
        ///   LSX: VDIV.HU Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> Divide(Vector128<ushort> left, Vector128<ushort> right) => Divide(left, right);

        /// <summary>
        /// int32x4_t vdiv_w(int32x4_t a, int32x4_t b)
        ///   LSX: VDIV.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<int> Divide(Vector128<int> left, Vector128<int> right) => Divide(left, right);

        /// <summary>
        /// uint32x4_t vdiv_wu(uint32x4_t a, uint32x4_t b)
        ///   LSX: VDIV.WU Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<uint> Divide(Vector128<uint> left, Vector128<uint> right) => Divide(left, right);

        /// <summary>
        /// int64x2_t vdiv_d(int64x2_t a, int64x2_t b)
        ///   LSX: VDIV.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> Divide(Vector128<long> left, Vector128<long> right) => Divide(left, right);

        /// <summary>
        /// uint64x2_t vdiv_du(uint64x2_t a, uint64x2_t b)
        ///   LSX: VDIV.DU Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> Divide(Vector128<ulong> left, Vector128<ulong> right) => Divide(left, right);

        /// <summary>
        /// float32x4_t vfdiv_s(float32x4_t a, float32x4_t b)
        ///   LSX: VFDIV.S Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<float> Divide(Vector128<float> left, Vector128<float> right) => Divide(left, right);

        /// <summary>
        /// float64x2_t vfdiv_d(float64x2_t a, float64x2_t b)
        ///   LSX: VFDIV.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<double> Divide(Vector128<double> left, Vector128<double> right) => Divide(left, right);

        /// <summary>
        /// int8x16_t vmod_b(int8x16_t a, int8x16_t b)
        ///   LSX: VMOD.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> Modulo(Vector128<sbyte> left, Vector128<sbyte> right) => Modulo(left, right);

        /// <summary>
        /// uint8x16_t vmod_bu(uint8x16_t a, uint8x16_t b)
        ///   LSX: VMOD.BU Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> Modulo(Vector128<byte> left, Vector128<byte> right) => Modulo(left, right);

        /// <summary>
        /// int16x8_t vmod_h(int16x8_t a, int16x8_t b)
        ///   LSX: VMOD.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> Modulo(Vector128<short> left, Vector128<short> right) => Modulo(left, right);

        /// <summary>
        /// uint16x8_t vmod_hu(uint16x8_t a, uint16x8_t b)
        ///   LSX: VMOD.HU Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> Modulo(Vector128<ushort> left, Vector128<ushort> right) => Modulo(left, right);

        /// <summary>
        /// int32x4_t vmod_w(int32x4_t a, int32x4_t b)
        ///   LSX: VMOD.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<int> Modulo(Vector128<int> left, Vector128<int> right) => Modulo(left, right);

        /// <summary>
        /// uint32x4_t vmod_wu(uint32x4_t a, uint32x4_t b)
        ///   LSX: VMOD.WU Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<uint> Modulo(Vector128<uint> left, Vector128<uint> right) => Modulo(left, right);

        /// <summary>
        /// int64x2_t vmod_d(int64x2_t a, int64x2_t b)
        ///   LSX: VMOD.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> Modulo(Vector128<long> left, Vector128<long> right) => Modulo(left, right);

        /// <summary>
        /// uint64x2_t vmod_du(uint64x2_t a, uint64x2_t b)
        ///   LSX: VMOD.DU Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> Modulo(Vector128<ulong> left, Vector128<ulong> right) => Modulo(left, right);

        /// <summary>
        /// float32x4_t vfmadd_s(float32x4_t a, float32x4_t b, float32x4_t c)
        ///   LSX: VFMADD.S Vd.4S, Vj.4S, Vk.4S, Va.4S
        /// </summary>
        public static Vector128<float> FusedMultiplyAdd(Vector128<float> left, Vector128<float> right, Vector128<float> addend) => FusedMultiplyAdd(left, right, addend);

        /// <summary>
        /// float64x2_t vfmadd_d(float64x2_t a, float64x2_t b, float64x2_t c)
        ///   LSX: VFMADD.D Vd.2D, Vj.2D, Vk.2D, Va.2D
        /// </summary>
        public static Vector128<double> FusedMultiplyAdd(Vector128<double> left, Vector128<double> right, Vector128<double> addend) => FusedMultiplyAdd(left, right, addend);

        /// <summary>
        /// float32x4_t vfnmadd_s(float32x4_t a, float32x4_t b, float32x4_t c)
        ///   LSX: VFNMADD.S Vd.4S, Vj.4S, Vk.4S, Va.4S
        /// </summary>
        public static Vector128<float> FusedMultiplyAddNegated(Vector128<float> left, Vector128<float> right, Vector128<float> addend) => FusedMultiplyAddNegated(left, right, addend);

        /// <summary>
        /// float64x2_t vfnmadd_d(float64x2_t a, float64x2_t b, float64x2_t c)
        ///   LSX: VFNMADD.D Vd.2D, Vj.2D, Vk.2D, Va.2D
        /// </summary>
        public static Vector128<double> FusedMultiplyAddNegated(Vector128<double> left, Vector128<double> right, Vector128<double> addend) => FusedMultiplyAddNegated(left, right, addend);

        /// <summary>
        /// int8x16_t vmadd_b(int8x16_t a, int8x16_t b, int8x16_t c)
        ///   LSX: VMADD.B Vd.16B, Vj.16B, Vk.16B               //NOTE: The Vd is both input and output while input as addend.
        /// </summary>
        public static Vector128<sbyte> MultiplyAdd(Vector128<sbyte> addend, Vector128<sbyte> left, Vector128<sbyte> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        /// uint8x16_t vmadd_b(uint8x16_t a, uint8x16_t b, uint8x16_t c)
        ///   LSX: VMADD.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> MultiplyAdd(Vector128<byte> addend, Vector128<byte> left, Vector128<byte> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        /// int16x8_t vmadd_h(int16x8_t a, int16x8_t b, int16x8_t c)
        ///   LSX: VMADD.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> MultiplyAdd(Vector128<short> addend, Vector128<short> left, Vector128<short> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        /// uint16x8_t vmadd_h(uint16x8_t a, uint16x8_t b, uint16x8_t c)
        ///   LSX: VMADD.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> MultiplyAdd(Vector128<ushort> addend, Vector128<ushort> left, Vector128<ushort> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        /// int32x4_t vmadd_w(int32x4_t a, int32x4_t b, int32x4_t c)
        ///   LSX: VMADD.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<int> MultiplyAdd(Vector128<int> addend, Vector128<int> left, Vector128<int> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        /// uint32x4_t vmadd_w(uint32x4_t a, uint32x4_t b, uint32x4_t c)
        ///   LSX: VMADD.W  Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<uint> MultiplyAdd(Vector128<uint> addend, Vector128<uint> left, Vector128<uint> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        /// int64x2_t vmadd_d(int64x2_t a, int64x2_t b, int64x2_t c)
        ///   LSX: VMADD.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> MultiplyAdd(Vector128<long> minuend, Vector128<long> left, Vector128<long> right) => MultiplyAdd(minuend, left, right);

        /// <summary>
        /// uint64x2_t vmadd_d(uint64x2_t a, uint64x2_t b, uint64x2_t c)
        ///   LSX: VMADD.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> MultiplyAdd(Vector128<ulong> minuend, Vector128<ulong> left, Vector128<ulong> right) => MultiplyAdd(minuend, left, right);

        /// <summary>
        /// float32x4_t vfmsub_s(float32x4_t a, float32x4_t b, float32x4_t c)
        ///   LSX: VFMSUB.S Vd.4S, Vj.4S, Vk.4S, Va,4S
        /// </summary>
        public static Vector128<float> FusedMultiplySubtract(Vector128<float> left, Vector128<float> right, Vector128<float> minuend) => FusedMultiplySubtract(left, right, minuend);

        /// <summary>
        /// float64x2_t vfmsub_d(float64x2_t a, float64x2_t b, float64x2_t c)
        ///   LSX: VFMSUB.D Vd.2D, Vj.2D, Vk.2D, Va.2D
        /// </summary>
        public static Vector128<double> FusedMultiplySubtract(Vector128<double> left, Vector128<double> right, Vector128<double> minuend) => FusedMultiplySubtract(left, right, minuend);

        /// <summary>
        /// float32x4_t vfnmsub_s(float32x4_t a, float32x4_t b, float32x4_t c)
        ///   LSX: VFNMSUB.S Vd.4S, Vj.4S, Vk.4S, Va,4S
        /// </summary>
        public static Vector128<float> FusedMultiplySubtractNegated(Vector128<float> left, Vector128<float> right, Vector128<float> minuend) => FusedMultiplySubtractNegated(left, right, minuend);

        /// <summary>
        /// float64x2_t vfnmsub_d(float64x2_t a, float64x2_t b, float64x2_t c)
        ///   LSX: VFNMSUB.D Vd.2D, Vj.2D, Vk.2D, Va.2D
        /// </summary>
        public static Vector128<double> FusedMultiplySubtractNegated(Vector128<double> left, Vector128<double> right, Vector128<double> minuend) => FusedMultiplySubtractNegated(left, right, minuend);

        /// <summary>
        /// int8x16_t vmsub_b(int8x16_t a, int8x16_t b, int8x16_t c)
        ///   LSX: VMSUB.B Vd.16B, Vj.16B, Vk.16B               //NOTE: The Vd is both input and output while input as minuend.
        /// </summary>
        public static Vector128<sbyte> MultiplySubtract(Vector128<sbyte> minuend, Vector128<sbyte> left, Vector128<sbyte> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        /// uint8x16_t vmsub_b(uint8x16_t a, uint8x16_t b, uint8x16_t c)
        ///   LSX: VMSUB.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> MultiplySubtract(Vector128<byte> minuend, Vector128<byte> left, Vector128<byte> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        /// int16x8_t vmsub_h(int16x8_t a, int16x8_t b, int16x8_t c)
        ///   LSX: VMSUB.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> MultiplySubtract(Vector128<short> minuend, Vector128<short> left, Vector128<short> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        /// uint16x8_t vmsub_h(uint16x8_t a, uint16x8_t b, uint16x8_t c)
        ///   LSX: VMSUB.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> MultiplySubtract(Vector128<ushort> minuend, Vector128<ushort> left, Vector128<ushort> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        /// int32x4_t vmsub_w(int32x4_t a, int32x4_t b, int32x4_t c)
        ///   LSX: VMSUB.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<int> MultiplySubtract(Vector128<int> minuend, Vector128<int> left, Vector128<int> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        /// uint32x4_t vmsub_w(uint32x4_t a, uint32x4_t b, uint32x4_t c)
        ///   LSX: VMSUB.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<uint> MultiplySubtract(Vector128<uint> minuend, Vector128<uint> left, Vector128<uint> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        /// int64x2_t vmsub_d(int64x2_t a, int64x2_t b, int64x2_t c)
        ///   LSX: VMSUB.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> MultiplySubtract(Vector128<long> minuend, Vector128<long> left, Vector128<long> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        /// uint64x2_t vmsub_d(uint64x2_t a, uint64x2_t b, uint64x2_t c)
        ///   LSX: VMSUB.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> MultiplySubtract(Vector128<ulong> minuend, Vector128<ulong> left, Vector128<ulong> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        /// int16x8_t vmaddwev_h_b(int16x8_t a, int8x8_t b, int8x8_t c)
        ///   LSX: VMADDWEV.H.B Vd.8H, Vj.8B, Vk.8B
        /// </summary>
        public static Vector128<short> MultiplyWideningLowerAndAdd(Vector128<short> addend, Vector64<sbyte> left, Vector64<sbyte> right) => MultiplyWideningLowerAndAdd(addend, left, right);

        /// <summary>
        /// uint16x8_t vmaddwev_h_bu(uint16x8_t a, uint8x8_t b, uint8x8_t c)
        ///   LSX: VMADDWEV.H.BU Vd.8H, Vj.8B, Vk.8B
        /// </summary>
        public static Vector128<ushort> MultiplyWideningLowerAndAdd(Vector128<ushort> addend, Vector64<byte> left, Vector64<byte> right) => MultiplyWideningLowerAndAdd(addend, left, right);

        /// <summary>
        /// int32x4_t vmaddwev_w_h(int32x4_t a, int16x4_t b, int16x4_t c)
        ///   LSX: VMADDWEV.W.H Vd.4W, Vj.4H, Vk.4H
        /// </summary>
        public static Vector128<int> MultiplyWideningLowerAndAdd(Vector128<int> addend, Vector64<short> left, Vector64<short> right) => MultiplyWideningLowerAndAdd(addend, left, right);

        /// <summary>
        /// uint32x4_t vmaddwev_w_hu(uint32x4_t a, uint16x4_t b, uint16x4_t c)
        ///   LSX: VMADDWEV.W.HU Vd.4W, Vj.4H, Vk.4H
        /// </summary>
        public static Vector128<uint> MultiplyWideningLowerAndAdd(Vector128<uint> addend, Vector64<ushort> left, Vector64<ushort> right) => MultiplyWideningLowerAndAdd(addend, left, right);

        /// <summary>
        /// int64x2_t vmaddwev_d_w(int64x2_t a, int32x2_t b, int32x2_t c)
        ///   LSX: VMADDWEV.D.W Vd.2D, Vj.2S, Vk.2S
        /// </summary>
        public static Vector128<long> MultiplyWideningLowerAndAdd(Vector128<long> addend, Vector64<int> left, Vector64<int> right) => MultiplyWideningLowerAndAdd(addend, left, right);

        /// <summary>
        /// uint64x2_t vmaddwev_d_wu(uint64x2_t a, uint32x2_t b, uint32x2_t c)
        ///   LSX: VMADDWEV.D.WU Vd.2D, Vj.2S, Vk.2S
        /// </summary>
        public static Vector128<ulong> MultiplyWideningLowerAndAdd(Vector128<ulong> addend, Vector64<uint> left, Vector64<uint> right) => MultiplyWideningLowerAndAdd(addend, left, right);

        /// <summary>
        /// int16x8_t vmaddwod_h_b(int16x8_t a, int8x16_t b, int8x16_t c)
        ///   LSX: VMADDWOD.H.B Vd.8H, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<short> MultiplyWideningUpperAndAdd(Vector128<short> addend, Vector128<sbyte> left, Vector128<sbyte> right) => MultiplyWideningUpperAndAdd(addend, left, right);

        /// <summary>
        /// uint16x8_t vmaddwod_h_bu(uint16x8_t a, uint8x16_t b, uint8x16_t c)
        ///   LSX: VMADDWOD.H.BU Vd.8H, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<ushort> MultiplyWideningUpperAndAdd(Vector128<ushort> addend, Vector128<byte> left, Vector128<byte> right) => MultiplyWideningUpperAndAdd(addend, left, right);

        /// <summary>
        /// int32x4_t vmaddwod_w_h(int32x4_t a, int16x8_t b, int16x8_t c)
        ///   LSX: VMADDWOD.W.H Vd.4W, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<int> MultiplyWideningUpperAndAdd(Vector128<int> addend, Vector128<short> left, Vector128<short> right) => MultiplyWideningUpperAndAdd(addend, left, right);

        /// <summary>
        /// uint32x4_t vmaddwod_w_hu(uint32x4_t a, uint16x8_t b, uint16x8_t c)
        ///   LSX: VMADDWOD.W.HU Vd.4W, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<uint> MultiplyWideningUpperAndAdd(Vector128<uint> addend, Vector128<ushort> left, Vector128<ushort> right) => MultiplyWideningUpperAndAdd(addend, left, right);

        /// <summary>
        /// int64x2_t vmaddwod_d_w(int64x2_t a, int32x4_t b, int32x4_t c)
        ///   LSX: VMADDWOD.D.W Vd.2D, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<long> MultiplyWideningUpperAndAdd(Vector128<long> addend, Vector128<int> left, Vector128<int> right) => MultiplyWideningUpperAndAdd(addend, left, right);

        /// <summary>
        /// uint64x2_t vmaddwod_d_wu(uint64x2_t a, uint32x4_t b, uint32x4_t c)
        ///   LSX: VMADDWOD.D.WU Vd.2D, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<ulong> MultiplyWideningUpperAndAdd(Vector128<ulong> addend, Vector128<uint> left, Vector128<uint> right) => MultiplyWideningUpperAndAdd(addend, left, right);

        /// <summary>
        ///  int8x16_t vmsknz_b(int8x16_t value)
        ///   LSX: VMSKNZ.B Vd.16B, Vj.16B
        /// </summary>
        public static Vector128<sbyte> CompareNotEqualZero(Vector128<sbyte> value) => CompareNotEqualZero(value);

        /// <summary>
        ///  uint8x16_t vmsknz_b(uint8x16_t value)
        ///   LSX: VMSKNZ.B Vd.16B, Vj.16B
        /// </summary>
        public static Vector128<byte> CompareNotEqualZero(Vector128<byte> value) => CompareNotEqualZero(value);

        /// <summary>
        /// int8x16_t vseqi_b(int8x16_t a, int8_t si5)
        ///   LSX: VSEQI.B Vd.16B, Vj.16B, si5
        /// </summary>
        public static Vector128<sbyte> CompareEqual(Vector128<sbyte> value,  [ConstantExpected(Min = -16, Max = (byte)(15))] const sbyte si5) => CompareEqual(value, si5);

        /// <summary>
        /// int16x8_t vseqi_h(int16x8_t a, int8_t si5)
        ///   LSX: VSEQI.H Vd.8H, Vj.8H, si5
        /// </summary>
        public static Vector128<short> CompareEqual(Vector128<short> value,  [ConstantExpected(Min = -16, Max = (byte)(15))] const sbyte si5) => CompareEqual(value, si5);

        /// <summary>
        /// int32x4_t vseqi_w(int32x4_t a, int8_t si5)
        ///   LSX: VSEQI.W Vd.4W, Vj.4W, si5
        /// </summary>
        public static Vector128<int> CompareEqual(Vector128<int> value,  [ConstantExpected(Min = -16, Max = (byte)(15))] const sbyte si5) => CompareEqual(value, si5);

        /// <summary>
        /// int64x2_t vseqi_d(int64x2_t a, int8_t si5)
        ///   LSX: VSEQI.D Vd.2D, Vj.2D, si5
        /// </summary>
        public static Vector128<long> CompareEqual(Vector128<long> value,  [ConstantExpected(Min = -16, Max = (byte)(15))] const sbyte si5) => CompareEqual(value, si5);

        /// <summary>
        /// int8x16_t vseq_b(int8x16_t a, int8x16_t b)
        ///   LSX: VSEQ.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> CompareEqual(Vector128<sbyte> left, Vector128<sbyte> right) => CompareEqual(left, right);

        /// <summary>
        /// uint8x16_t vseq_b(uint8x16_t a, uint8x16_t b)
        ///   LSX: VSEQ.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> CompareEqual(Vector128<byte> left, Vector128<byte> right) => CompareEqual(left, right);

        /// <summary>
        /// int16x8_t vseq_h(int16x8_t a, int16x8_t b)
        ///   LSX: VSEQ.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> CompareEqual(Vector128<short> left, Vector128<short> right) => CompareEqual(left, right);

        /// <summary>
        /// uint16x8_t vseq_h(uint16x8_t a, uint16x8_t b)
        ///   LSX: VSEQ.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> CompareEqual(Vector128<ushort> left, Vector128<ushort> right) => CompareEqual(left, right);

        /// <summary>
        /// int32x4_t vseq_w(int32x4_t a, int32x4_t b)
        ///   LSX: VSEQ.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<int> CompareEqual(Vector128<int> left, Vector128<int> right) => CompareEqual(left, right);

        /// <summary>
        /// uint32x4_t vseq_w(uint32x4_t a, uint32x4_t b)
        ///   LSX: VSEQ.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<uint> CompareEqual(Vector128<uint> left, Vector128<uint> right) => CompareEqual(left, right);

        /// <summary>
        /// int64x2_t vseq_d(int64x2_t a, int64x2_t b)
        ///   LSX: VSEQ.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> CompareEqual(Vector128<long> left, Vector128<long> right) => CompareEqual(left, right);

        /// <summary>
        /// uint64x2_t vseq_d(uint64x2_t a, uint64x2_t b)
        ///   LSX: VSEQ.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> CompareEqual(Vector128<ulong> left, Vector128<ulong> right) => CompareEqual(left, right);

        /// <summary>
        /// int32x4_t vfcmp_ceq_s(float32x4_t a, float32x4_t b)
        ///   LSX: VFCMP.CEQ.S Vd.4W, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<int> CompareEqual(Vector128<float> left, Vector128<float> right) => CompareEqual(left, right);

        /// <summary>
        /// int64x2_t vfcmp_ceq_d(float64x2_t a, float64x2_t b)
        ///   LSX: VFCMP.CEQ.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> CompareEqual(Vector128<double> left, Vector128<double> right) => CompareEqual(left, right);

        /// <summary>
        ///  int8x16_t vmskltz_b(int8x16_t value)
        ///   LSX: VMSKLTZ.B Vd.16B, Vj.16B
        /// </summary>
        public static Vector128<sbyte> CompareLessThanZero(Vector128<sbyte> value) => CompareLessThanZero(value);

        /// <summary>
        ///  int16x8_t vmskltz_h(int16x8_t value)
        ///   LSX: VMSKLTZ.H Vd.8H, Vj.8H
        /// </summary>
        public static Vector128<short> CompareLessThanZero(Vector128<short> value) => CompareLessThanZero(value);

        /// <summary>
        ///  int32x4_t vmskltz_w(int32x4_t value)
        ///   LSX: VMSKLTZ.W Vd.4W, Vj.4W
        /// </summary>
        public static Vector128<int> CompareLessThanZero(Vector128<int> value) => CompareLessThanZero(value);

        /// <summary>
        ///  int64x2_t vmskltz_d(int64x2_t value)
        ///   LSX: VMSKLTZ.D Vd.2D, Vj.2D
        /// </summary>
        public static Vector128<long> CompareLessThanZero(Vector128<long> value) => CompareLessThanZero(value);

        /// <summary>
        /// int8x16_t vslti_b(int8x16_t a, int8_t si5)
        ///   LSX: VSLTI.B Vd.16B, Vj.16B, si5
        /// </summary>
        public static Vector128<sbyte> CompareLessThan(Vector128<sbyte> value,  [ConstantExpected(Min = -16, Max = (byte)(15))] const sbyte si5) => CompareLessThan(value, si5);

        /// <summary>
        /// int16x8_t vslti_h(int16x8_t a, int8_t si5)
        ///   LSX: VSLTI.H Vd.8H, Vj.8H, si5
        /// </summary>
        public static Vector128<short> CompareLessThan(Vector128<short> value,  [ConstantExpected(Min = -16, Max = (byte)(15))] const sbyte si5) => CompareLessThan(value, si5);

        /// <summary>
        /// int32x4_t vslti_w(int32x4_t a, int8_t si5)
        ///   LSX: VSLTI.W Vd.4W, Vj.4W, si5
        /// </summary>
        public static Vector128<int> CompareLessThan(Vector128<int> value,  [ConstantExpected(Min = -16, Max = (byte)(15))] const sbyte si5) => CompareLessThan(value, si5);

        /// <summary>
        /// int64x2_t vslti_d(int64x2_t a, int8_t si5)
        ///   LSX: VSLTI.D Vd.2D, Vj.2D, si5
        /// </summary>
        public static Vector128<long> CompareLessThan(Vector128<long> value,  [ConstantExpected(Min = -16, Max = (byte)(15))] const sbyte si5) => CompareLessThan(value, si5);

        /// <summary>
        /// uint8x16_t vslt_b(int8x16_t a, int8x16_t b)
        ///   LSX: VSLT.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> CompareLessThan(Vector128<sbyte> left, Vector128<sbyte> right) => CompareLessThan(left, right);

        /// <summary>
        /// uint8x16_t vslt_bu(uint8x16_t a, uint8x16_t b)
        ///   LSX: VSLT.BU Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> CompareLessThan(Vector128<byte> left, Vector128<byte> right) => CompareLessThan(left, right);

        /// <summary>
        /// uint16x8_t vslt_h(int16x8_t a, int16x8_t b)
        ///   LSX: VSLT.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> CompareLessThan(Vector128<short> left, Vector128<short> right) => CompareLessThan(left, right);

        /// <summary>
        /// uint16x8_t vslt_hu(uint16x8_t a, uint16x8_t b)
        ///   LSX: VSLT.HU Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> CompareLessThan(Vector128<ushort> left, Vector128<ushort> right) => CompareLessThan(left, right);

        /// <summary>
        /// uint32x4_t vslt_w(int32x4_t a, int32x4_t b)
        ///   LSX: VSLT.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<int> CompareLessThan(Vector128<int> left, Vector128<int> right) => CompareLessThan(left, right);

        /// <summary>
        /// uint32x4_t vslt_wu(uint32x4_t a, uint32x4_t b)
        ///   LSX: VSLT.WU Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<uint> CompareLessThan(Vector128<uint> left, Vector128<uint> right) => CompareLessThan(left, right);

        /// <summary>
        /// uint64x2_t vslt_d(int64x2_t a, int64x2_t b)
        ///   LSX: VSLT.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> CompareLessThan(Vector128<long> left, Vector128<long> right) => CompareLessThan(left, right);

        /// <summary>
        /// uint64x2_t vslt_du(uint64x2_t a, uint64x2_t b)
        ///   LSX: VSLT.DU Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> CompareLessThan(Vector128<ulong> left, Vector128<ulong> right) => CompareLessThan(left, right);

        /// <summary>
        /// int32x4_t vfcmp_clt_s(float32x4_t a, float32x4_t b)
        ///   LSX: VFCMP.CLT.S Vd.4W, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<int> CompareLessThan(Vector128<float> left, Vector128<float> right) => CompareLessThan(left, right);

        /// <summary>
        /// int64x2_t vfcmp_clt_d(float64x2_t a, float64x2_t b)
        ///   LSX: VFCMP.CLT.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> CompareLessThan(Vector128<double> left, Vector128<double> right) => CompareLessThan(left, right);

        /// <summary>
        /// int8x16_t vslei_b(int8x16_t a, int8_t si5)
        ///   LSX: VSLEI.B Vd.16B, Vj.16B, si5
        /// </summary>
        public static Vector128<sbyte> CompareLessThanOrEqual(Vector128<sbyte> value,  [ConstantExpected(Min = -16, Max = (byte)(15))] const sbyte si5) => CompareLessThanOrEqual(value, si5);

        /// <summary>
        /// int16x8_t vslei_h(int16x8_t a, int8_t si5)
        ///   LSX: VSLEI.H Vd.8H, Vj.8H, si5
        /// </summary>
        public static Vector128<short> CompareLessThanOrEqual(Vector128<short> value,  [ConstantExpected(Min = -16, Max = (byte)(15))] const sbyte si5) => CompareLessThanOrEqual(value, si5);

        /// <summary>
        /// int32x4_t vslei_w(int32x4_t a, int8_t si5)
        ///   LSX: VSLEI.W Vd.4W, Vj.4W, si5
        /// </summary>
        public static Vector128<int> CompareLessThanOrEqual(Vector128<int> value,  [ConstantExpected(Min = -16, Max = (byte)(15))] const sbyte si5) => CompareLessThanOrEqual(value, si5);

        /// <summary>
        /// int64x2_t vslei_d(int64x2_t a, int8_t si5)
        ///   LSX: VSLEI.D Vd.2D, Vj.2D, si5
        /// </summary>
        public static Vector128<long> CompareLessThanOrEqual(Vector128<long> value,  [ConstantExpected(Min = -16, Max = (byte)(15))] const sbyte si5) => CompareLessThanOrEqual(value, si5);

        /// <summary>
        /// uint8x16_t vsle_b(int8x16_t a, int8x16_t b)
        ///   LSX: VSLE.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> CompareLessThanOrEqual(Vector128<sbyte> left, Vector128<sbyte> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// uint16x8_t vsle_h(int16x8_t a, int16x8_t b)
        ///   LSX: VSLE.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> CompareLessThanOrEqual(Vector128<short> left, Vector128<short> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// uint32x4_t vsle_w(int32x4_t a, int32x4_t b)
        ///   LSX: VSLE.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<int> CompareLessThanOrEqual(Vector128<int> left, Vector128<int> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// uint64x2_t vsle_d(int64x2_t a, int64x2_t b)
        ///   LSX: VSLE.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> CompareLessThanOrEqual(Vector128<long> left, Vector128<long> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// uint8x16_t vslei_bu(uint8x16_t a, uint8_t ui5)
        ///   LSX: VSLEI.BU Vd.16B, Vj.16B, ui5
        /// </summary>
        public static Vector128<byte> CompareLessThanOrEqual(Vector128<byte> value,  [ConstantExpected(Min = 0, Max = (byte)(31))] const byte ui5) => CompareLessThanOrEqual(value, ui5);

        /// <summary>
        /// uint16x8_t vslei_hu(uint16x8_t a, uint8_t ui5)
        ///   LSX: VSLEI.HU Vd.8H, Vj.8H, ui5
        /// </summary>
        public static Vector128<ushort> CompareLessThanOrEqual(Vector128<ushort> value,  [ConstantExpected(Min = 0, Max = (byte)(31))] const byte ui5) => CompareLessThanOrEqual(value, ui5);

        /// <summary>
        /// uint32x4_t vslei_wu(uint32x4_t a, uint8_t ui5)
        ///   LSX: VSLEI.WU Vd.4W, Vj.4W, ui5
        /// </summary>
        public static Vector128<uint> CompareLessThanOrEqual(Vector128<uint> value,  [ConstantExpected(Min = 0, Max = (byte)(31))] const byte ui5) => CompareLessThanOrEqual(value, ui5);

        /// <summary>
        /// uint64x2_t vslei_du(uint64x2_t a, uint8_t ui5)
        ///   LSX: VSLEI.DU Vd.2D, Vj.2D, ui5
        /// </summary>
        public static Vector128<long> CompareLessThanOrEqual(Vector128<long> value,  [ConstantExpected(Min = 0, Max = (byte)(31))] const byte ui5) => CompareLessThanOrEqual(value, ui5);

        /// <summary>
        /// uint8x16_t vsle_bu(uint8x16_t a, uint8x16_t b)
        ///   LSX: VSLE.BU Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> CompareLessThanOrEqual(Vector128<byte> left, Vector128<byte> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// uint16x8_t vsle_hu(uint16x8_t a, uint16x8_t b)
        ///   LSX: VSLE.HU Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> CompareLessThanOrEqual(Vector128<ushort> left, Vector128<ushort> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// uint32x4_t vsle_wu(uint32x4_t a, uint32x4_t b)
        ///   LSX: VSLE.WU Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<uint> CompareLessThanOrEqual(Vector128<uint> left, Vector128<uint> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// uint64x2_t vsle_du(uint64x2_t a, uint64x2_t b)
        ///   LSX: VSLE.DU Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> CompareLessThanOrEqual(Vector128<ulong> left, Vector128<ulong> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// int32x4_t vfcmp_cle_s(float32x4_t a, float32x4_t b)
        ///   LSX: VFCMP.CLE.S Vd.4W, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<int> CompareLessThanOrEqual(Vector128<float> left, Vector128<float> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// int64x2_t vfcmp_cle_d(float64x2_t a, float64x2_t b)
        ///   LSX: VFCMP.CLE.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> CompareLessThanOrEqual(Vector128<double> left, Vector128<double> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// uint8x16_t vsle_b(int8x16_t a, int8x16_t b)
        ///   LSX: VSLE.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> CompareGreaterThan(Vector128<sbyte> left, Vector128<sbyte> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// uint16x8_t vsle_h(int16x8_t a, int16x8_t b)
        ///   LSX: VSLE.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> CompareGreaterThan(Vector128<short> left, Vector128<short> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// uint32x4_t vsle_w(int32x4_t a, int32x4_t b)
        ///   LSX: VSLE.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<int> CompareGreaterThan(Vector128<int> left, Vector128<int> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// uint64x2_t vsle_d(int64x2_t a, int64x2_t b)
        ///   LSX: VSLE.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> CompareGreaterThan(Vector128<long> left, Vector128<long> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// uint8x16_t vsle_bu(uint8x16_t a, uint8x16_t b)
        ///   LSX: VSLE.BU Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> CompareGreaterThan(Vector128<byte> left, Vector128<byte> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// uint16x8_t vsle_hu(uint16x8_t a, uint16x8_t b)
        ///   LSX: VSLE.HU Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> CompareGreaterThan(Vector128<ushort> left, Vector128<ushort> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// uint32x4_t vsle_wu(uint32x4_t a, uint32x4_t b)
        ///   LSX: VSLE.WU Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<uint> CompareGreaterThan(Vector128<uint> left, Vector128<uint> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// uint64x2_t vsle_du(uint64x2_t a, uint64x2_t b)
        ///   LSX: VSLE.DU Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> CompareGreaterThan(Vector128<ulong> left, Vector128<ulong> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// int32x4_t vfcmp_cle_s(float32x4_t a, float32x4_t b)
        ///   LSX: VFCMP.CLE.S Vd.4W, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<int> CompareGreaterThan(Vector128<float> left, Vector128<float> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// int64x2_t vfcmp_cle_d(float64x2_t a, float64x2_t b)
        ///   LSX: VFCMP.CLE.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> CompareGreaterThan(Vector128<double> left, Vector128<double> right) => CompareGreaterThan(left, right);

        /// <summary>
        ///  int8x16_t vmskgez_b(int8x16_t value)
        ///   LSX: VMSKGEZ.B Vd.16B, Vj.16B
        /// </summary>
        public static Vector128<sbyte> CompareGreaterThanOrEqualZero(Vector128<sbyte> value) => CompareGreaterThanOrEqualZero(value);

        /// <summary>
        /// uint8x16_t vslt_b(int8x16_t a, int8x16_t b)
        ///   LSX: VSLT.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> CompareGreaterThanOrEqual(Vector128<sbyte> left, Vector128<sbyte> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// uint8x16_t vslt_bu(uint8x16_t a, uint8x16_t b)
        ///   LSX: VSLT.BU Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> CompareGreaterThanOrEqual(Vector128<byte> left, Vector128<byte> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// uint16x8_t vslt_h(int16x8_t a, int16x8_t b)
        ///   LSX: VSLT.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> CompareGreaterThanOrEqual(Vector128<short> left, Vector128<short> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// uint16x8_t vslt_hu(uint16x8_t a, uint16x8_t b)
        ///   LSX: VSLT.HU Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> CompareGreaterThanOrEqual(Vector128<ushort> left, Vector128<ushort> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// uint32x4_t vslt_w(int32x4_t a, int32x4_t b)
        ///   LSX: VSLT.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<int> CompareGreaterThanOrEqual(Vector128<int> left, Vector128<int> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// uint32x4_t vslt_wu(uint32x4_t a, uint32x4_t b)
        ///   LSX: VSLT.WU Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<uint> CompareGreaterThanOrEqual(Vector128<uint> left, Vector128<uint> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// uint64x2_t vslt_d(int64x2_t a, int64x2_t b)
        ///   LSX: VSLT.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> CompareGreaterThanOrEqual(Vector128<long> left, Vector128<long> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// uint64x2_t vslt_du(uint64x2_t a, uint64x2_t b)
        ///   LSX: VSLT.DU Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> CompareGreaterThanOrEqual(Vector128<ulong> left, Vector128<ulong> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// int32x4_t vfcmp_clt_s(float32x4_t a, float32x4_t b)
        ///   LSX: VFCMP.CLT.S Vd.4W, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<int> CompareGreaterThanOrEqual(Vector128<float> left, Vector128<float> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// int64x2_t vfcmp_clt_d(float64x2_t a, float64x2_t b)
        ///   LSX: VFCMP.CLT.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> CompareGreaterThanOrEqual(Vector128<double> left, Vector128<double> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// int8x16_t vmaxi_b(int8x16_t a, int8_t si5)
        ///   LSX: VMAXI.B Vd.16B, Vj.16B, si5
        /// </summary>
        public static Vector128<sbyte> Max(Vector128<sbyte> value, const sbyte si5) => Max(value, si5);

        /// <summary>
        /// uint8x16_t vmaxi_bu(uint8x16_t a, int8_t si5)
        ///   LSX: VMAXI.BU Vd.16B, Vj.16B, ui5
        /// </summary>
        public static Vector128<byte> Max(Vector128<byte> value, const byte ui5) => Max(value, ui5);

        /// <summary>
        /// int16x8_t vmaxi_h(int16x8_t a, int8_t si5)
        ///   LSX: VMAXI.H Vd.8H, Vj.8H, si5
        /// </summary>
        public static Vector128<short> Max(Vector128<short> value, const sbyte si5) => Max(value, si5);

        /// <summary>
        /// uint16x8_t vmaxi_hu(uint16x8_t a, int8_t si5)
        ///   LSX: VMAXI.HU Vd.8H, Vj.8H, ui5
        /// </summary>
        public static Vector128<ushort> Max(Vector128<ushort> value, const byte ui5) => Max(value, ui5);

        /// <summary>
        /// int32x4_t vmaxi_w(int32x4_t a, int8_t si5)
        ///   LSX: VMAXI.W Vd.4W, Vj.4W, si5
        /// </summary>
        public static Vector128<int> Max(Vector128<int> value, const sbyte si5) => Max(value, si5);

        /// <summary>
        /// uint32x4_t vmaxi_wu(uint32x4_t a, int8_t si5)
        ///   LSX: VMAXI.WU Vd.4W, Vj.4W, ui5
        /// </summary>
        public static Vector128<uint> Max(Vector128<uint> value, const byte ui5) => Max(value, ui5);

        /// <summary>
        /// int64x2_t vmaxi_d(int64x2_t a, int8_t si5)
        ///   LSX: VMAXI.D Vd.2D, Vj.2D, si5
        /// </summary>
        public static Vector128<long> Max(Vector128<long> value, const sbyte si5) => Max(value, si5);

        /// <summary>
        /// uint64x2_t vmaxi_du(uint64x2_t a, int8_t si5)
        ///   LSX: VMAXI.DU Vd.2D, Vj.2D, ui5
        /// </summary>
        public static Vector128<ulong> Max(Vector128<ulong> value, const byte ui5) => Max(value, ui5);

        /// <summary>
        /// int8x16_t vmax_b(int8x16_t a, int8x16_t b)
        ///   LSX: VMAX.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> Max(Vector128<sbyte> left, Vector128<sbyte> right) => Max(left, right);

        /// <summary>
        /// uint8x16_t vmax_bu(uint8x16_t a, uint8x16_t b)
        ///   LSX: VMAX.BU Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> Max(Vector128<byte> left, Vector128<byte> right) => Max(left, right);

        /// <summary>
        /// int16x8_t vmax_h(int16x8_t a, int16x8_t b)
        ///   LSX: VMAX.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> Max(Vector128<short> left, Vector128<short> right) => Max(left, right);

        /// <summary>
        /// uint16x8_t vmax_hu(uint16x8_t a, uint16x8_t b)
        ///   LSX: VMAX.HU Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> Max(Vector128<ushort> left, Vector128<ushort> right) => Max(left, right);

        /// <summary>
        /// int32x4_t vmax_w(int32x4_t a, int32x4_t b)
        ///   LSX: VMAX.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<int> Max(Vector128<int> left, Vector128<int> right) => Max(left, right);

        /// <summary>
        /// uint32x4_t vmax_wu(uint32x4_t a, uint32x4_t b)
        ///   LSX: VMAX.WU Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<uint> Max(Vector128<uint> left, Vector128<uint> right) => Max(left, right);

        /// <summary>
        /// int64x2_t vmax_d(int64x2_t a, int64x2_t b)
        ///   LSX: VMAX.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> Max(Vector128<long> left, Vector128<long> right) => Max(left, right);

        /// <summary>
        /// uint64x2_t vmax_du(uint64x2_t a, uint64x2_t b)
        ///   LSX: VMAX.DU Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> Max(Vector128<ulong> left, Vector128<ulong> right) => Max(left, right);

        /// <summary>
        /// float32x4_t vfmax_s(float32x4_t a, float32x4_t b)
        ///   LSX: VFMAX.S Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<float> Max(Vector128<float> left, Vector128<float> right) => Max(left, right);

        /// <summary>
        /// float64x2_t vfmax_d(float64x2_t a, float64x2_t b)
        ///   LSX: VFMAX.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<double> Max(Vector128<double> left, Vector128<double> right) => Max(left, right);

        /// <summary>
        /// float32x4_t vfmaxa_s(float32x4_t a, float32x4_t b)
        ///   LSX: VFMAXA.S Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<float> MaxFloatAbsolute(Vector128<float> left, Vector128<float> right) => MaxFloatAbsolute(left, right);

        /// <summary>
        /// float64x2_t vfmaxa_d(float64x2_t a, float64x2_t b)
        ///   LSX: VFMAXA.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<double> MaxFloatAbsolute(Vector128<double> left, Vector128<double> right) => MaxFloatAbsolute(left, right);

        /// <summary>
        /// int8x16_t vmini_b(int8x16_t a, int8_t si5)
        ///   LSX: VMINI.B Vd.16B, Vj.16B, si5
        /// </summary>
        public static Vector128<sbyte> Min(Vector128<sbyte> value, const sbyte si5) => Min(value, si5);

        /// <summary>
        /// uint8x16_t vmini_bu(uint8x16_t a, int8_t si5)
        ///   LSX: VMINI.BU Vd.16B, Vj.16B, ui5
        /// </summary>
        public static Vector128<byte> Min(Vector128<byte> value, const byte ui5) => Min(value, ui5);

        /// <summary>
        /// int16x8_t vmini_h(int16x8_t a, int8_t si5)
        ///   LSX: VMINI.H Vd.8H, Vj.8H, si5
        /// </summary>
        public static Vector128<short> Min(Vector128<short> value, const sbyte si5) => Min(value, si5);

        /// <summary>
        /// uint16x8_t vmini_hu(uint16x8_t a, int8_t si5)
        ///   LSX: VMINI.HU Vd.8H, Vj.8H, ui5
        /// </summary>
        public static Vector128<ushort> Min(Vector128<ushort> value, const byte ui5) => Min(value, ui5);

        /// <summary>
        /// int32x4_t vmini_w(int32x4_t a, int8_t si5)
        ///   LSX: VMINI.W Vd.4W, Vj.4W, si5
        /// </summary>
        public static Vector128<int> Min(Vector128<int> value, const sbyte si5) => Min(value, si5);

        /// <summary>
        /// uint32x4_t vmini_wu(uint32x4_t a, int8_t si5)
        ///   LSX: VMINI.WU Vd.4W, Vj.4W, ui5
        /// </summary>
        public static Vector128<uint> Min(Vector128<uint> value, const byte ui5) => Min(value, ui5);

        /// <summary>
        /// int64x2_t vmini_d(int64x2_t a, int8_t si5)
        ///   LSX: VMINI.D Vd.2D, Vj.2D, si5
        /// </summary>
        public static Vector128<long> Min(Vector128<long> value, const sbyte si5) => Min(value, si5);

        /// <summary>
        /// uint64x2_t vmini_du(uint64x2_t a, int8_t si5)
        ///   LSX: VMINI.DU Vd.2D, Vj.2D, ui5
        /// </summary>
        public static Vector128<ulong> Min(Vector128<ulong> value, const byte ui5) => Min(value, ui5);

        /// <summary>
        /// int8x16_t vmin_b(int8x16_t a, int8x16_t b)
        ///   LSX: VMIN.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> Min(Vector128<sbyte> left, Vector128<sbyte> right) => Min(left, right);

        /// <summary>
        /// uint8x16_t vmin_bu(uint8x16_t a, uint8x16_t b)
        ///   LSX: VMIN.BU Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> Min(Vector128<byte> left, Vector128<byte> right) => Min(left, right);

        /// <summary>
        /// int16x8_t vmin_h(int16x8_t a, int16x8_t b)
        ///   LSX: VMIN.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> Min(Vector128<short> left, Vector128<short> right) => Min(left, right);

        /// <summary>
        /// uint16x8_t vmin_hu(uint16x8_t a, uint16x8_t b)
        ///   LSX: VMIN.HU Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> Min(Vector128<ushort> left, Vector128<ushort> right) => Min(left, right);

        /// <summary>
        /// int32x4_t vmin_w(int32x4_t a, int32x4_t b)
        ///   LSX: VMIN.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<int> Min(Vector128<int> left, Vector128<int> right) => Min(left, right);

        /// <summary>
        /// uint32x4_t vmin_wu(uint32x4_t a, uint32x4_t b)
        ///   LSX: VMIN.WU Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<uint> Min(Vector128<uint> left, Vector128<uint> right) => Min(left, right);

        /// <summary>
        /// int64x2_t vmin_d(int64x2_t a, int64x2_t b)
        ///   LSX: VMIN.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> Min(Vector128<long> left, Vector128<long> right) => Min(left, right);

        /// <summary>
        /// uint64x2_t vmin_du(uint64x2_t a, uint64x2_t b)
        ///   LSX: VMIN.DU Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> Min(Vector128<ulong> left, Vector128<ulong> right) => Min(left, right);

        /// <summary>
        /// float32x4_t vfmin_s(float32x4_t a, float32x4_t b)
        ///   LSX: VFMIN.S Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<float> Min(Vector128<float> left, Vector128<float> right) => Min(left, right);

        /// <summary>
        /// float64x2_t vfmin_d(float64x2_t a, float64x2_t b)
        ///   LSX: VFMIN.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<double> Min(Vector128<double> left, Vector128<double> right) => Min(left, right);

        /// <summary>
        /// float32x4_t vfmina_s(float32x4_t a, float32x4_t b)
        ///   LSX: VFMINA.S Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<float> MinFloatAbsolute(Vector128<float> left, Vector128<float> right) => MinFloatAbsolute(left, right);

        /// <summary>
        /// float64x2_t vfmina_d(float64x2_t a, float64x2_t b)
        ///   LSX: VFMINA.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<double> MinFloatAbsolute(Vector128<double> left, Vector128<double> right) => MinFloatAbsolute(left, right);

        /// <summary>
        /// int8x16_t vbitsel_v(uint8x16_t a, int8x16_t b, int8x16_t c)
        ///   LSX: VBITSEL.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> BitwiseSelect(Vector128<sbyte> select, Vector128<sbyte> left, Vector128<sbyte> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// uint8x16_t vbitsel_v(uint8x16_t a, uint8x16_t b, uint8x16_t c)
        ///   LSX: VBITSEL.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> BitwiseSelect(Vector128<byte> select, Vector128<byte> left, Vector128<byte> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// int16x8_t vbitsel_v(uint16x8_t a, int16x8_t b, int16x8_t c)
        ///   LSX: VBITSEL.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<short> BitwiseSelect(Vector128<short> select, Vector128<short> left, Vector128<short> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// uint16x8_t vbitsel_v(uint16x8_t a, uint16x8_t b, uint16x8_t c)
        ///   LSX: VBITSEL.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<ushort> BitwiseSelect(Vector128<ushort> select, Vector128<ushort> left, Vector128<ushort> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// int32x4_t vbitsel_v(uint32x4_t a, int32x4_t b, int32x4_t c)
        ///   LSX: VBITSEL.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<int> BitwiseSelect(Vector128<int> select, Vector128<int> left, Vector128<int> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// uint32x4_t vbitsel_v(uint32x4_t a, uint32x4_t b, uint32x4_t c)
        ///   LSX: VBITSEL.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<uint> BitwiseSelect(Vector128<uint> select, Vector128<uint> left, Vector128<uint> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// int64x2_t vbitsel_v(uint64x2_t a, int64x2_t b, int64x2_t c)
        ///   LSX: VBITSEL.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<long> BitwiseSelect(Vector128<long> select, Vector128<long> left, Vector128<long> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// uint64x2_t vbitsel_v(uint64x2_t a, uint64x2_t b, uint64x2_t c)
        ///   LSX: VBITSEL.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<ulong> BitwiseSelect(Vector128<ulong> select, Vector128<ulong> left, Vector128<ulong> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// float32x4_t vbitsel_v(uint32x4_t a, float32x4_t b, float32x4_t c)
        ///   LSX: VBITSEL.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<float> BitwiseSelect(Vector128<float> select, Vector128<float> left, Vector128<float> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// float64x2_t vbitsel_v(uint64x2_t a, float64x2_t b, float64x2_t c)
        ///   LSX: VBITSEL.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<double> BitwiseSelect(Vector128<double> select, Vector128<double> left, Vector128<double> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// int8x16_t vabsd_b(int8x16_t a, int8x16_t b)
        ///   LSX: VABSD.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> AbsoluteDifference(Vector128<sbyte> left, Vector128<sbyte> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// uint8x16_t vabsd_bu(uint8x16_t a, uint8x16_t b)
        ///   LSX: VABSD.BU Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> AbsoluteDifference(Vector128<byte> left, Vector128<byte> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// int16x8_t vabsd_h(int16x8_t a, int16x8_t b)
        ///   LSX: VABSD.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> AbsoluteDifference(Vector128<short> left, Vector128<short> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// uint16x8_t vabsd_hu(uint16x8_t a, uint16x8_t b)
        ///   LSX: VABSD.HU Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> AbsoluteDifference(Vector128<ushort> left, Vector128<ushort> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// int32x4_t vabsd_w(int32x4_t a, int32x4_t b)
        ///   LSX: VABSD.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<uint> AbsoluteDifference(Vector128<int> left, Vector128<int> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// uint32x4_t vabsd_wu(uint32x4_t a, uint32x4_t b)
        ///   LSX: VABSD.WU Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<uint> AbsoluteDifference(Vector128<uint> left, Vector128<uint> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// int64x2_t vabsd_d(uint64x2_t a, int64x2_t b, int64x2_t c)
        ///   LSX: VABSD.D Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<ulong> AbsoluteDifference(Vector128<long> left, Vector128<long> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// uint64x2_t vabsd_du(uint64x2_t a, uint64x2_t b, uint64x2_t c)
        ///   LSX: VABSD.DU Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<ulong> AbsoluteDifference(Vector128<ulong> left, Vector128<ulong> right) => AbsoluteDifference(left, right);

        ///// <summary>
        ///// float32x4_t TODO(float32x4_t a, float32x4_t b)   multi-instructions.
        /////   LSX: TODO Vd.4S, Vj.4S, Vk.4S
        ///// </summary>
        //public static Vector128<float> AbsoluteDifference(Vector128<float> left, Vector128<float> right) => AbsoluteDifference(left, right);

        ///// <summary>
        ///// float64x2_t TODO(float64x2_t a, float64x2_t b)
        /////   LSX: TODO Vd.2D, Vj.2D, Vk.2D
        ///// </summary>
        //public static Vector128<double> AbsoluteDifference(Vector128<double> left, Vector128<double> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// int8x16_t vld(int8_t const * ptr, const short si12)
        ///   LSX: VLD Vd.16B, Rj, si12
        /// </summary>
        public static unsafe Vector128<sbyte> LoadVector128(sbyte* address, [ConstantExpected(Min = -2048, Max = 2047)] const short si12) => LoadVector128(address, si12);

        /// <summary>
        /// uint8x16_t vld(uint8_t const * ptr, const short si12)
        ///   LSX: VLD Vd.16B, Rj, si12
        /// </summary>
        public static unsafe Vector128<byte> LoadVector128(byte* address, [ConstantExpected(Min = -2048, Max = 2047)] const short si12) => LoadVector128(address, si12);

        /// <summary>
        /// int16x8_t vld(int16_t const * ptr, const short si12)
        ///   LSX: VLD Vd.8H, Rj, si12
        /// </summary>
        public static unsafe Vector128<short> LoadVector128(short* address, [ConstantExpected(Min = -2048, Max = 2047)] const short si12) => LoadVector128(address, si12);

        /// <summary>
        /// uint16x8_t vld(uint16_t const * ptr, const short si12)
        ///   LSX: VLD Vd.8H, Rj, si12
        /// </summary>
        public static unsafe Vector128<ushort> LoadVector128(ushort* address, [ConstantExpected(Min = -2048, Max = 2047)] const short si12) => LoadVector128(address, si12);

        /// <summary>
        /// int32x4_t vld(int32_t const * ptr, const short si12)
        ///   LSX: VLD Vd.4W, Rj, si12
        /// </summary>
        public static unsafe Vector128<int> LoadVector128(int* address, [ConstantExpected(Min = -2048, Max = 2047)] const short si12) => LoadVector128(address, si12);

        /// <summary>
        /// uint32x4_t vld(uint32_t const * ptr, const short si12)
        ///   LSX: VLD Vd.4W, Rj, si12
        /// </summary>
        public static unsafe Vector128<uint> LoadVector128(uint* address, [ConstantExpected(Min = -2048, Max = 2047)] const short si12) => LoadVector128(address, si12);

        /// <summary>
        /// int64x2_t vld(int64_t const * ptr, const short si12)
        ///   LSX: VLD Vd.2D, Rj, si12
        /// </summary>
        public static unsafe Vector128<long> LoadVector128(long* address, [ConstantExpected(Min = -2048, Max = 2047)] const short si12) => LoadVector128(address, si12);

        /// <summary>
        /// uint64x2_t vld(uint64_t const * ptr, const short si12)
        ///   LSX: VLD Vd.2D, Rj, si12
        /// </summary>
        public static unsafe Vector128<ulong> LoadVector128(ulong* address, [ConstantExpected(Min = -2048, Max = 2047)] const short si12) => LoadVector128(address, si12);

        /// <summary>
        /// float32x4_t vld(float32_t const * ptr, const short si12)
        ///   LSX: VLD Vd.4S, Rj, si12
        /// </summary>
        public static unsafe Vector128<float> LoadVector128(float* address, [ConstantExpected(Min = -2048, Max = 2047)] const short si12) => LoadVector128(address, si12);

        /// <summary>
        /// float64x2_t vld(float64_t const * ptr, const short si12)
        ///   LSX: VLD Vd.2D, Rj, si12
        /// </summary>
        public static unsafe Vector128<double> LoadVector128(double* address, [ConstantExpected(Min = -2048, Max = 2047)] const short si12) => LoadVector128(address, si12);

        /// <summary>
        /// int8x16_t vldx(int8_t const * ptr, long offsetValue)
        ///   LSX: VLDX Vd.16B, Rj, rk
        /// </summary>
        public static unsafe Vector128<sbyte> LoadVector128(sbyte* address, long offsetValue) => LoadVector128(address, offsetValue);

        /// <summary>
        /// uint8x16_t vldx(uint8_t const * ptr, long offsetValue)
        ///   LSX: VLDX Vd.16B, Rj, rk
        /// </summary>
        public static unsafe Vector128<byte> LoadVector128(byte* address, long offsetValue) => LoadVector128(address, offsetValue);

        /// <summary>
        /// int16x8_t vldx(int16_t const * ptr, long offsetValue)
        ///   LSX: VLDX Vd.8H, Rj, rk
        /// </summary>
        public static unsafe Vector128<short> LoadVector128(short* address, long offsetValue) => LoadVector128(address, offsetValue);

        /// <summary>
        /// uint16x8_t vldx(uint16_t const * ptr, long offsetValue)
        ///   LSX: VLDX Vd.8H, Rj, rk
        /// </summary>
        public static unsafe Vector128<ushort> LoadVector128(ushort* address, long offsetValue) => LoadVector128(address, offsetValue);

        /// <summary>
        /// int32x4_t vldx(int32_t const * ptr, long offsetValue)
        ///   LSX: VLDX Vd.4W, Rj, rk
        /// </summary>
        public static unsafe Vector128<int> LoadVector128(int* address, long offsetValue) => LoadVector128(address, offsetValue);

        /// <summary>
        /// uint32x4_t vldx(uint32_t const * ptr, long offsetValue)
        ///   LSX: VLDX Vd.4W, Rj, rk
        /// </summary>
        public static unsafe Vector128<uint> LoadVector128(uint* address, long offsetValue) => LoadVector128(address, offsetValue);

        /// <summary>
        /// int64x2_t vldx(int64_t const * ptr, long offsetValue)
        ///   LSX: VLDX Vd.2D, Rj, rk
        /// </summary>
        public static unsafe Vector128<long> LoadVector128(long* address, long offsetValue) => LoadVector128(address, offsetValue);

        /// <summary>
        /// uint64x2_t vldx(uint64_t const * ptr, long offsetValue)
        ///   LSX: VLDX Vd.2D, Rj, rk
        /// </summary>
        public static unsafe Vector128<ulong> LoadVector128(ulong* address, long offsetValue) => LoadVector128(address, offsetValue);

        /// <summary>
        /// float32x4_t vldx(float32_t const * ptr, long offsetValue)
        ///   LSX: VLDX Vd.4S, Rj, rk
        /// </summary>
        public static unsafe Vector128<float> LoadVector128(float* address, long offsetValue) => LoadVector128(address, offsetValue);

        /// <summary>
        /// float64x2_t vldx(float64_t const * ptr, long offsetValue)
        ///   LSX: VLDX Vd.2D, Rj, rk
        /// </summary>
        public static unsafe Vector128<double> LoadVector128(double* address, long offsetValue) => LoadVector128(address, offsetValue);

        /// <summary>
        /// int8x16_t vldrepl_b(int8_t const * ptr, const short si12)
        ///   LSX: VLDREPL.B Vd.16B, Rj, si12
        /// </summary>
        public static unsafe Vector128<sbyte> LoadElementReplicateVector(sbyte* address, [ConstantExpected(Min = -2048, Max = 2047)] const short si12) => LoadElementReplicateVector(address, si12);

        /// <summary>
        /// uint8x16_t vldrepl_b(uint8_t const * ptr, const short si12)
        ///   LSX: VLDREPL.B Vd.16B, Rj, si12
        /// </summary>
        public static unsafe Vector128<byte> LoadElementReplicateVector(byte* address, [ConstantExpected(Min = -2048, Max = 2047)] const short si12) => LoadElementReplicateVector(address, si12);

        /// <summary>
        /// int16x8_t vldrepl_h(int16_t const * ptr, const short si12)
        ///   LSX: VLDREPL.H Vd.8H, Rj, si11
        /// </summary>
        public static unsafe Vector128<short> LoadElementReplicateVector(short* address, [ConstantExpected(Min = -2048, Max = 2047)] const short si12) => LoadElementReplicateVector(address, si12);

        /// <summary>
        /// uint16x8_t vldrepl_h(uint16_t const * ptr, const short si12)
        ///   LSX: VLDREPL.H Vd.8H, Rj, si11
        /// </summary>
        public static unsafe Vector128<ushort> LoadElementReplicateVector(ushort* address, [ConstantExpected(Min = -2048, Max = 2047)] const short si12) => LoadElementReplicateVector(address, si12);

        /// <summary>
        /// int32x4_t vldrepl_w(int32_t const * ptr, const short si12)
        ///   LSX: VLDREPL.W Vd.4W, Rj, si10
        /// </summary>
        public static unsafe Vector128<int> LoadElementReplicateVector(int* address, [ConstantExpected(Min = -2048, Max = 2047)] const short si12) => LoadElementReplicateVector(address, si12);

        /// <summary>
        /// uint32x4_t vldrepl_w(uint32_t const * ptr, const short si12)
        ///   LSX: VLDREPL.W Vd.4W, Rj, si10
        /// </summary>
        public static unsafe Vector128<uint> LoadElementReplicateVector(uint* address, [ConstantExpected(Min = -2048, Max = 2047)] const short si12) => LoadElementReplicateVector(address, si12);

        /// <summary>
        /// int64x2_t vldrepl_d(int64_t const * ptr, const short si12)
        ///   LSX: VLDREPL.D Vd.2D, Rj, si9
        /// </summary>
        public static unsafe Vector128<long> LoadElementReplicateVector(long* address, [ConstantExpected(Min = -2048, Max = 2047)] const short si12) => LoadElementReplicateVector(address, si12);

        /// <summary>
        /// uint64x2_t vldrepl_d(uint64_t const * ptr, const short si12)
        ///   LSX: VLDREPL.D Vd.2D, Rj, si9
        /// </summary>
        public static unsafe Vector128<ulong> LoadElementReplicateVector(ulong* address, [ConstantExpected(Min = -2048, Max = 2047)] const short si12) => LoadElementReplicateVector(address, si12);

        /// <summary>
        /// float32x4_t vld(float32_t const * ptr, const short si12)
        ///   LSX: VLDREPL.W Vd.4S, Rj, si10
        /// </summary>
        public static unsafe Vector128<float> LoadElementReplicateVector(float* address, [ConstantExpected(Min = -2048, Max = 2047)] const short si12) => LoadElementReplicateVector(address, si12);

        /// <summary>
        /// float64x2_t vldrepl_d(float64_t const * ptr, const short si12)
        ///   LSX: VLDREPL.D Vd.2D, Rj, si9
        /// </summary>
        public static unsafe Vector128<double> LoadElementReplicateVector(double* address, [ConstantExpected(Min = -2048, Max = 2047)] const short si12) => LoadElementReplicateVector(address, si12);

        /// <summary>
        /// float32x4_t vfrecip_s(float32x4_t a)
        ///   LSX: VFRECIP.S Vd.4S Vj.4S
        /// </summary>
        public static Vector128<float> Reciprocal(Vector128<float> value) => Reciprocal(value);

        /// <summary>
        /// float64x2_t vfrecip_d(float64x2_t a)
        ///   LSX: VFRECIP.D Vd.2D Vj.2D
        /// </summary>
        public static Vector128<double> Reciprocal(Vector128<double> value) => Reciprocal(value);

        /// <summary>
        /// float32x4_t vfrsqrt_s(float32x4_t a)
        ///   LSX: VFRSQRT.S Vd.4S Vj.4S
        /// </summary>
        public static Vector128<float> ReciprocalSqrt(Vector128<float> value) => ReciprocalSqrt(value);

        /// <summary>
        /// float64x2_t vfrsqrt_d(float64x2_t a)
        ///   LSX: VFRSQRT.D Vd.2D Vj.2D
        /// </summary>
        public static Vector128<double> ReciprocalSqrt(Vector128<double> value) => ReciprocalSqrt(value);

        /// <summary>
        /// float32x4_t vfsqrt_s(float32x4_t a)
        ///   LSX: VFSQRT.S Vd.4S, Vj.4S
        /// </summary>
        public static Vector128<float> Sqrt(Vector128<float> value) => Sqrt(value);

        /// <summary>
        /// float64x2_t vfsqrt_d(float64x2_t a)
        ///   LSX: VFSQRT.D Vd.2D, Vj.2D
        /// </summary>
        public static Vector128<double> Sqrt(Vector128<double> value) => Sqrt(value);

        /// <summary>
        /// float32x4_t vflogb_s(float32x4_t a)
        ///   LSX: VFLOGB.S Vd.4S, Vj.4S
        /// </summary>
        public static Vector128<float> Logarithm2(Vector128<float> value) => Logarithm2(value);

        /// <summary>
        /// float64x2_t vflogb_d(float64x2_t a)
        ///   LSX: VFLOGB.D Vd.2D, Vj.2D
        /// </summary>
        public static Vector128<double> Logarithm2(Vector128<double> value) => Logarithm2(value);

        /// <summary>
        /// void vst(int8x16_t val, int8_t* addr, const short si12)
        ///   LSX: VST Vd.16B, Rj, si12
        /// </summary>
        public static unsafe void Store(Vector128<sbyte> vector, sbyte* addr, [ConstantExpected(Min = -2048, Max = 2047)] short si12) => Store(vector, addr, si12);

        /// <summary>
        /// void vst(uint8x16_t val, uint8_t* addr, const short si12)
        ///   LSX: VST Vd.16B, Rj, si12
        /// </summary>
        public static unsafe void Store(Vector128<byte> vector, byte* addr, [ConstantExpected(Min = -2048, Max = 2047)] short si12) => Store(vector, addr, si12);

        /// <summary>
        /// void vst(int16x8_t val, int16_t* addr, const short si12)
        ///   LSX: VST Vd.8H, Rj, si12
        /// </summary>
        public static unsafe void Store(Vector128<short> vector, short* addr, [ConstantExpected(Min = -2048, Max = 2047)] short si12) => Store(vector, addr, si12);

        /// <summary>
        /// void vst(uint16x8_t val, uint16_t* addr, const short si12)
        ///   LSX: VST Vd.8H, Rj, si12
        /// </summary>
        public static unsafe void Store(Vector128<ushort> vector, ushort* addr, [ConstantExpected(Min = -2048, Max = 2047)] short si12) => Store(vector, addr, si12);

        /// <summary>
        /// void vst(int32x4_t val, int32_t* addr, const short si12)
        ///   LSX: VST Vd.4W, Rj, si12
        /// </summary>
        public static unsafe void Store(Vector128<int> vector, int* addr, [ConstantExpected(Min = -2048, Max = 2047)] short si12) => Store(vector, addr, si12);

        /// <summary>
        /// void vst(uint32x4_t val, uint32_t* addr, const short si12)
        ///   LSX: VST Vd.4W, Rj, si12
        /// </summary>
        public static unsafe void Store(Vector128<uint> vector, uint* addr, [ConstantExpected(Min = -2048, Max = 2047)] short si12) => Store(vector, addr, si12);

        /// <summary>
        /// void vst(int64x2_t val, int64_t* addr, const short si12)
        ///   LSX: VST Vd.2D, Rj, si12
        /// </summary>
        public static unsafe void Store(Vector128<long> vector, long* addr, [ConstantExpected(Min = -2048, Max = 2047)] short si12) => Store(vector, addr, si12);

        /// <summary>
        /// void vst(uint64x2_t val, uint64_t* addr, const short si12)
        ///   LSX: VST Vd.2D, Rj, si12
        /// </summary>
        public static unsafe void Store(Vector128<ulong> vector, ulong* addr, [ConstantExpected(Min = -2048, Max = 2047)] short si12) => Store(vector, addr, si12);

        /// <summary>
        /// void vst(float32x4_t val, float32_t* addr, const short si12)
        ///   LSX: VST Vd.4S, Rj, si12
        /// </summary>
        public static unsafe void Store(Vector128<float> vector, float* addr, [ConstantExpected(Min = -2048, Max = 2047)] short si12) => Store(vector, addr, si12);

        /// <summary>
        /// void vst(float64x2_t val, float64_t* addr, const short si12)
        ///   LSX: VST Vd.2D, Rj, si12
        /// </summary>
        public static unsafe void Store(Vector128<double> vector, double* addr, [ConstantExpected(Min = -2048, Max = 2047)] short si12) => Store(vector, addr, si12);

        /// <summary>
        /// void vstelm_b(int8x16_t val, int8_t* ptr, const short si8, const byte index)
        ///   LSX: VSTELM.B Vd.16B, Rj, si8, idx
        /// </summary>
        public static unsafe void StoreElement(Vector128<sbyte> vector, sbyte* addr, [ConstantExpected(Min = -128, Max = 127)] short si8, [ConstantExpected(Max = (byte)(15))] byte idx) => StoreElement(vector, addr, si8, idx);

        /// <summary>
        /// void vstelm_b(uint8x16_t val, uint8_t* ptr, const short si8, const byte index)
        ///   LSX: VSTELM.B Vd.16B, Rj, si8, idx
        /// </summary>
        public static unsafe void StoreElement(Vector128<byte> vector, byte* addr, [ConstantExpected(Min = -128, Max = 127)] short si8, [ConstantExpected(Max = (byte)(15))] byte idx) => StoreElement(vector, addr, si8, idx);

        /// <summary>
        /// void vstelm_h(int16x8_t val, int16_t* ptr, const short si9, const byte index) // si9 is 2byte aligned.
        ///   LSX: VSTELM.H Vd.8H, Rj, si8, idx
        /// </summary>
        public static unsafe void StoreElement(Vector128<short> vector, short* addr, [ConstantExpected(Min = -256, Max = 254)] short si9, [ConstantExpected(Max = (byte)(7))] byte idx) => StoreElement(vector, addr, si9, idx);

        /// <summary>
        /// void vstelm_h(uint16x8_t val, uint16_t* ptr, const short si9, const byte index)
        ///   LSX: VSTELM.H Vd.8H, Rj, si8, idx
        /// </summary>
        public static unsafe void StoreElement(Vector128<ushort> vector, ushort* addr, [ConstantExpected(Min = -256, Max = 254)] short si9, [ConstantExpected(Max = (byte)(7))] byte idx) => StoreElement(vector, addr, si9, idx);

        /// <summary>
        /// void vstelm_w(int32x4_t val, int* ptr, const short si10, const byte index) // si10 is 4byte aligned.
        ///   LSX: VSTELM.W Vd.4W, Rj, si8, idx
        /// </summary>
        public static unsafe void StoreElement(Vector128<int> vector, int* addr, [ConstantExpected(Min = -512, Max = 508)] short si10, [ConstantExpected(Max = (byte)(3))] byte idx) => StoreElement(vector, addr, si10, idx);

        /// <summary>
        /// void vstelm_w(uint32x4_t val, uint* ptr, const short si10, const byte index)
        ///   LSX: VSTELM.W Vd.4W, Rj, si8, idx
        /// </summary>
        public static unsafe void StoreElement(Vector128<uint> vector, uint* addr, [ConstantExpected(Min = -512, Max = 508)] short si10, [ConstantExpected(Max = (byte)(3))] byte idx) => StoreElement(vector, addr, si10, idx);

        /// <summary>
        /// void vstelm_d(int64x2_t val, int64_t* ptr, const short si11, const byte index) // si11 is 8byte aligned.
        ///   LSX: VSTELM.D Vd.2D, Rj, si8, idx
        /// </summary>
        public static unsafe void StoreElement(Vector128<long> vector, long* addr, [ConstantExpected(Min = -1024, Max = 1016)] short si11, [ConstantExpected(Max = (byte)(1))] byte idx) => StoreElement(vector, addr, si11, idx);

        /// <summary>
        /// void vstelm_d(uint64x2_t val, uint64_t* ptr, const short si11, const byte index)
        ///   LSX: VSTELM.D Vd.2D, Rj, si8, idx
        /// </summary>
        public static unsafe void StoreElement(Vector128<ulong> vector, ulong* addr, [ConstantExpected(Min = -1024, Max = 1016)] short si11, [ConstantExpected(Max = (byte)(1))] byte idx) => StoreElement(vector, addr, si11, idx);

        /// <summary>
        /// void vstelm_w(float32x4_t val, float32_t* ptr, const short si10, const byte index)
        ///   LSX: VSTELM.W Vd.16B, Rj, si8, idx
        /// </summary>
        public static unsafe void StoreElement(Vector128<float> vector, float* addr, [ConstantExpected(Min = -512, Max = 511)] short si10, [ConstantExpected(Max = (byte)(3))] byte idx) => StoreElement(vector, addr, si10, idx);

        /// <summary>
        /// void vstelm_d(float64x2_t val, float64_t* ptr, const short si11, const byte index)
        ///   LSX: VSTELM.D Vd.16B, Rj, si8, idx
        /// </summary>
        public static unsafe void StoreElement(Vector128<double> vector, double* addr, [ConstantExpected(Min = -1024, Max = 1016)] short si11, [ConstantExpected(Max = (byte)(1))] byte idx) => StoreElement(vector, addr, si11, idx);

        /// <summary>
        /// int8x16_t vneg_b(int8x16_t a)
        ///   LSX: VNEG.B Vd.16B, Vj.16B
        /// </summary>
        public static Vector128<sbyte> Negate(Vector128<sbyte> value) => Negate(value);

        /// <summary>
        /// int16x8_t vneg_h(int16x8_t a)
        ///   LSX: VNEG.H Vd.8H, Vj.8H
        /// </summary>
        public static Vector128<short> Negate(Vector128<short> value) => Negate(value);

        /// <summary>
        /// int32x4_t vneg_w(int32x4_t a)
        ///   LSX: VNEG.W Vd.4W, Vj.4W
        /// </summary>
        public static Vector128<int> Negate(Vector128<int> value) => Negate(value);

        /// <summary>
        /// int64x2_t vneg_d(int64x2_t a)
        ///   LSX: VNEG.D Vd.2D, Vj.2D
        /// </summary>
        public static Vector128<long> Negate(Vector128<long> value) => Negate(value);

        /// <summary>
        /// float32x4_t vbitrevi_w(float32x4_t a)
        ///   LSX: VBITREVI.W Vd.4W, Vj.4W, 31
        /// </summary>
        public static Vector128<float> Negate(Vector128<float> value) => Negate(value);

        /// <summary>
        /// float64x2_t vbitrevi_d(float64x2_t a)
        ///   LSX: VBITREVI.D Vd.2D, Vj.2D, 63
        /// </summary>
        public static Vector128<double> Negate(Vector128<double> value) => Negate(value);

        /// <summary>
        /// float64x1_t fneg_d(float64x1_t a)
        ///   LSX: FNEG.D Fd, Fj
        /// </summary>
        public static Vector64<double> NegateScalar(Vector64<double> value) => NegateScalar(value);

        /// <summary>
        /// float32_t fneg_s(float32_t a)
        ///   LSX: FNEG.S Fd, Fj
        /// </summary>
        public static Vector64<float> NegateScalar(Vector64<float> value) => NegateScalar(value);

        /// <summary>
        /// int16x8_t vmulwod_h_b(int8x16_t a, int8x16_t b)
        ///   LSX: VMULWOD.H.B Vd.8H, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<short> MultiplyWideningOdd(Vector128<sbyte> left, Vector128<sbyte> right) => MultiplyWideningOdd(left, right);

        /// <summary>
        /// int32x4_t vmulwod_w_h(int16x8_t a, int16x8_t b)
        ///   LSX: VMULWOD.W.H Vd.4W, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<int> MultiplyWideningOdd(Vector128<short> left, Vector128<short> right) => MultiplyWideningOdd(left, right);

        /// <summary>
        /// int64x2_t vmulwod_d_w(int32x4_t a, int32x4_t b)
        ///   LSX: VMULWOD.D.W Vd.2D, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<long> MultiplyWideningOdd(Vector128<int> left, Vector128<int> right) => MultiplyWideningOdd(left, right);

        /// <summary>
        /// int128x1_t vmulwod_q_d(int64x2_t a, int64x2_t b)
        ///   LSX: VMULWOD.Q.D Vd.Q, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> MultiplyWideningOdd(Vector128<long> left, Vector128<long> right) => MultiplyWideningOdd(left, right);

        /// <summary>
        /// int16x8_t vmulwev_h_b(int8x16_t a, int8x16_t b)
        ///   LSX: VMULWEV.H.B Vd.8H, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<short> MultiplyWideningEven(Vector128<sbyte> left, Vector128<sbyte> right) => MultiplyWideningEven(left, right);

        /// <summary>
        /// int32x4_t vmulwev_w_h(int16x8_t a, int16x8_t b)
        ///   LSX: VMULWEV.W.H Vd.4W, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<int> MultiplyWideningEven(Vector128<short> left, Vector128<short> right) => MultiplyWideningEven(left, right);

        /// <summary>
        /// int64x2_t vmulwev_d_w(int32x4_t a, int32x4_t b)
        ///   LSX: VMULWEV.D.W Vd.2D, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<long> MultiplyWideningEven(Vector128<int> left, Vector128<int> right) => MultiplyWideningEven(left, right);

        /// <summary>
        /// int128x1_t vmulwev_q_d(int64x2_t a, int64x2_t b)
        ///   LSX: VMULWEV.Q.D Vd.Q, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> MultiplyWideningEven(Vector128<long> left, Vector128<long> right) => MultiplyWideningEven(left, right);

        /// <summary>
        /// uint16x8_t vmulwod_hu_bu(uint8x16_t a, uint8x16_t b)
        ///   LSX: VMULWOD.H.BU Vd.8H, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<ushort> MultiplyWideningOdd(Vector128<byte> left, Vector128<byte> right) => MultiplyWideningOdd(left, right);

        /// <summary>
        /// uint32x4_t vmulwod_wu_hu(uint16x8_t a, uint16x8_t b)
        ///   LSX: VMULWOD.W.HU Vd.4W, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<uint> MultiplyWideningOdd(Vector128<ushort> left, Vector128<ushort> right) => MultiplyWideningOdd(left, right);

        /// <summary>
        /// uint64x2_t vmulwod_du_wu(uint32x4_t a, uint32x4_t b)
        ///   LSX: VMULWOD.D.WU Vd.2D, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<ulong> MultiplyWideningOdd(Vector128<uint> left, Vector128<uint> right) => MultiplyWideningOdd(left, right);

        /// <summary>
        /// uint128x1_t vmulwod_qu_du(uint64x2_t a, uint64x2_t b)
        ///   LSX: VMULWOD.Q.DU Vd.Q, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> MultiplyWideningOdd(Vector128<ulong> left, Vector128<ulong> right) => MultiplyWideningOdd(left, right);

        /// <summary>
        /// uint16x8_t vmulwev_hu_bu(uint8x16_t a, uint8x16_t b)
        ///   LSX: VMULWEV.H.BU Vd.8H, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<ushort> MultiplyWideningEven(Vector128<byte> left, Vector128<byte> right) => MultiplyWideningEven(left, right);

        /// <summary>
        /// uint32x4_t vmulwev_wu_hu(uint16x8_t a, uint16x8_t b)
        ///   LSX: VMULWEV.W.HU Vd.4W, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<uint> MultiplyWideningEven(Vector128<ushort> left, Vector128<ushort> right) => MultiplyWideningEven(left, right);

        /// <summary>
        /// uint64x2_t vmulwev_du_wu(uint32x4_t a, uint32x4_t b)
        ///   LSX: VMULWEV.D.WU Vd.2D, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<ulong> MultiplyWideningEven(Vector128<uint> left, Vector128<uint> right) => MultiplyWideningEven(left, right);

        /// <summary>
        /// uint128x1_t vmulwev_qu_du(uint64x2_t a, uint64x2_t b)
        ///   LSX: VMULWEV.Q.DU Vd.Q, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> MultiplyWideningEven(Vector128<ulong> left, Vector128<ulong> right) => MultiplyWideningEven(left, right);

        /// <summary>
        /// int16x8_t vmulwod_h_bu(uint8x16_t a, int8x16_t b)
        ///   LSX: VMULWOD.H.BU.B Vd.8H, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<short> MultiplyWideningOdd(Vector128<byte> left, Vector128<sbyte> right) => MultiplyWideningOdd(left, right);

        /// <summary>
        /// int32x4_t vmulwod_w_hu(uint16x8_t a, int16x8_t b)
        ///   LSX: VMULWOD.W.HU.H Vd.4W, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<int> MultiplyWideningOdd(Vector128<ushort> left, Vector128<short> right) => MultiplyWideningOdd(left, right);

        /// <summary>
        /// int64x2_t vmulwod_d_wu(uint32x4_t a, int32x4_t b)
        ///   LSX: VMULWOD.D.WU.W Vd.2D, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<long> MultiplyWideningOdd(Vector128<uint> left, Vector128<int> right) => MultiplyWideningOdd(left, right);

        /// <summary>
        /// int128x1_t vmulwod_q_du(uint64x2_t a, int64x2_t b)
        ///   LSX: VMULWOD.Q.DU.D Vd.Q, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> MultiplyWideningOdd(Vector128<ulong> left, Vector128<long> right) => MultiplyWideningOdd(left, right);

        /// <summary>
        /// int16x8_t vmulwev_h_bu(uint8x16_t a, int8x16_t b)
        ///   LSX: VMULWEV.H.BU.B Vd.8H, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<short> MultiplyWideningEven(Vector128<byte> left, Vector128<sbyte> right) => MultiplyWideningEven(left, right);

        /// <summary>
        /// int32x4_t vmulwev_w_hu(uint16x8_t a, int16x8_t b)
        ///   LSX: VMULWEV.W.HU.H Vd.4W, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<int> MultiplyWideningEven(Vector128<ushort> left, Vector128<short> right) => MultiplyWideningEven(left, right);

        /// <summary>
        /// int64x2_t vmulwev_d_wu(uint32x4_t a, int32x4_t b)
        ///   LSX: VMULWEV.D.WU.W Vd.2D, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<long> MultiplyWideningEven(Vector128<uint> left, Vector128<int> right) => MultiplyWideningEven(left, right);

        /// <summary>
        /// int128x1_t vmulwev_q_du(uint64x2_t a, int64x2_t b)
        ///   LSX: VMULWEV.Q.DU.D Vd.Q, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> MultiplyWideningEven(Vector128<ulong> left, Vector128<long> right) => MultiplyWideningEven(left, right);

        /// <summary>
        /// int8x16_t vavg_b(int8x16_t a, int8x16_t b)
        ///   LSX: VAVG.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> Average(Vector128<sbyte> left, Vector128<sbyte> right) => Average(left, right);

        /// <summary>
        /// uint8x16_t vavg_bu(uint8x16_t a, uint8x16_t b)
        ///   LSX: VAVG.BU Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> Average(Vector128<byte> left, Vector128<byte> right) => Average(left, right);

        /// <summary>
        /// int16x8_t vavg_h(int16x8_t a, int16x8_t b)
        ///   LSX: VAVG.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> Average(Vector128<short> left, Vector128<short> right) => Average(left, right);

        /// <summary>
        /// uint16x8_t vavg_hu(uint16x8_t a, uint16x8_t b)
        ///   LSX: VAVG.HU Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> Average(Vector128<ushort> left, Vector128<ushort> right) => Average(left, right);

        /// <summary>
        /// int32x4_t vavg_w(int32x4_t a, int32x4_t b)
        ///   LSX: VAVG.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<int> Average(Vector128<int> left, Vector128<int> right) => Average(left, right);

        /// <summary>
        /// uint32x4_t vavg_wu(uint32x4_t a, uint32x4_t b)
        ///   LSX: VAVG.WU Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<uint> Average(Vector128<uint> left, Vector128<uint> right) => Average(left, right);

        /// <summary>
        /// int64x2_t vavg_d(int64x2_t a, int64x2_t b)
        ///   LSX: VAVG.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> Average(Vector128<long> left, Vector128<long> right) => Average(left, right);

        /// <summary>
        /// uint64x2_t vavg_du(uint64x2_t a, uint64x2_t b)
        ///   LSX: VAVG.DU Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> Average(Vector128<ulong> left, Vector128<ulong> right) => Average(left, right);

        /// <summary>
        /// int8x16_t vavgr_b(int8x16_t a, int8x16_t b)
        ///   LSX: VAVGR.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> AverageRounded(Vector128<sbyte> left, Vector128<sbyte> right) => AverageRounded(left, right);

        /// <summary>
        /// uint8x16_t vavgr_bu(uint8x16_t a, uint8x16_t b)
        ///   LSX: VAVGR.BU Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> AverageRounded(Vector128<byte> left, Vector128<byte> right) => AverageRounded(left, right);

        /// <summary>
        /// int16x8_t vavgr_h(int16x8_t a, int16x8_t b)
        ///   LSX: VAVGR.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> AverageRounded(Vector128<short> left, Vector128<short> right) => AverageRounded(left, right);

        /// <summary>
        /// uint16x8_t vavgr_hu(uint16x8_t a, uint16x8_t b)
        ///   LSX: VAVGR.HU Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> AverageRounded(Vector128<ushort> left, Vector128<ushort> right) => AverageRounded(left, right);

        /// <summary>
        /// int32x4_t vavgr_w(int32x4_t a, int32x4_t b)
        ///   LSX: VAVGR.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<int> AverageRounded(Vector128<int> left, Vector128<int> right) => AverageRounded(left, right);

        /// <summary>
        /// uint32x4_t vavgr_wu(uint32x4_t a, uint32x4_t b)
        ///   LSX: VAVGR.WU Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<uint> AverageRounded(Vector128<uint> left, Vector128<uint> right) => AverageRounded(left, right);

        /// <summary>
        /// int64x2_t vavgr_d(int64x2_t a, int64x2_t b)
        ///   LSX: VAVGR.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> AverageRounded(Vector128<long> left, Vector128<long> right) => AverageRounded(left, right);

        /// <summary>
        /// uint64x2_t vavgr_du(uint64x2_t a, uint64x2_t b)
        ///   LSX: VAVGR.DU Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> AverageRounded(Vector128<ulong> left, Vector128<ulong> right) => AverageRounded(left, right);

        /// <summary>
        /// int16x8_t vsllwil_h_b(int8x16_t a, uint8_t ui3)
        ///   LSX: VSLLWIL.H.B Vd.8H, Vj.16B, ui3
        /// </summary>
        public static Vector128<short> SignExtendWideningLowerAndShiftLeft(Vector64<sbyte> value, byte shift) => SignExtendWideningLowerAndShiftLeft(value, shift);

        /// <summary>
        /// int32x4_t vsllwil_w_h(int16x4_t a, uint8_t ui4)
        ///   LSX: VSLLWIL.W.H Vd.4W, Vj.4H, ui4
        /// </summary>
        public static Vector128<int> SignExtendWideningLowerAndShiftLeft(Vector64<short> value, byte shift) => SignExtendWideningLowerAndShiftLeft(value, shift);

        /// <summary>
        /// int64x2_t vsllwil_d_w(int32x2_t a, uint8_t ui5)
        ///   LSX: VSLLWIL.D.W Vd.2D, Vj.2W, ui5
        /// </summary>
        public static Vector128<long> SignExtendWideningLowerAndShiftLeft(Vector64<int> value, byte shift) => SignExtendWideningLowerAndShiftLeft(value, shift);

        /// <summary>
        /// uint16x8_t vsllwil_hu_bu(uint8x16_t a, uint8_t ui3)
        ///   LSX: VSLLWIL.HU.BU Vd.8H, Vj.16B, ui3
        /// </summary>
        public static Vector128<ushort> ZeroExtendWideningLowerAndShiftLeft(Vector64<byte> value, byte shift) => ZeroExtendWideningLowerAndShiftLeft(value, shift);

        /// <summary>
        /// int16x8_t vsllwil_hu_bu(int8x16_t a, uint8_t ui3)
        ///   LSX: VSLLWIL.HU.BU Vd.8H, Vj.16B, ui3
        /// </summary>
        public static Vector128<short> ZeroExtendWideningLowerAndShiftLeft(Vector64<sbyte> value, byte shift) => ZeroExtendWideningLowerAndShiftLeft(value, shift);

        /// <summary>
        /// uint32x4_t vsllwil_wu_hu(uint16x8_t a, uint8_t ui4)
        ///   LSX: VSLLWIL.WU.HU Vd.4W, Vj.8H, ui4
        /// </summary>
        public static Vector128<uint> ZeroExtendWideningLowerAndShiftLeft(Vector64<ushort> value, byte shift) => ZeroExtendWideningLowerAndShiftLeft(value, shift);

        /// <summary>
        /// int32x4_t vsllwil_wu_hu(int16x8_t a, uint8_t ui4)
        ///   LSX: VSLLWIL.WU.HU Vd.4W, Vj.8H, ui4
        /// </summary>
        public static Vector128<int> ZeroExtendWideningLowerAndShiftLeft(Vector64<short> value, byte shift) => ZeroExtendWideningLowerAndShiftLeft(value, shift);

        /// <summary>
        /// uint64x2_t vsllwil_du_wu(uint32x4_t a, uint8_t ui5)
        ///   LSX: VSLLWIL.DU.WU Vd.2D, Vj.4W, ui5
        /// </summary>
        public static Vector128<ulong> ZeroExtendWideningLowerAndShiftLeft(Vector64<uint> value, byte shift) => ZeroExtendWideningLowerAndShiftLeft(value, shift);

        /// <summary>
        /// int64x2_t vsllwil_du_wu(int32x4_t a, uint8_t ui5)
        ///   LSX: VSLLWIL.DU.WU Vd.2D, Vj.4W, ui5
        /// </summary>
        public static Vector128<long> ZeroExtendWideningLowerAndShiftLeft(Vector64<int> value, byte shift) => ZeroExtendWideningLowerAndShiftLeft(value, shift);

        /// <summary>
        /// uint128x1_t vextl_qu_du(int64_t a)
        ///   LSX: VEXTL.QU.DU Vd.Q, Vj.D
        /// </summary>
        public static Vector128<ulong> ZeroExtendWideningLower(Vector64<long> value) => ZeroExtendWideningLower(value);

        /// <summary>
        /// uint128x1_t vextl_qu_du(uint64_t a)
        ///   LSX: VEXTL.QU.DU Vd.Q, Vj.D
        /// </summary>
        public static Vector128<ulong> ZeroExtendWideningLower(Vector64<ulong> value) => ZeroExtendWideningLower(value);

        /// <summary>
        /// int16x8_t vexth_h_b(int8x16_t a)
        ///   LSX: VEXTH.H.B Vd.8H, Vj.16B
        /// </summary>
        public static Vector128<short> SignExtendWideningUpper(Vector128<sbyte> value) => SignExtendWideningUpper(value);

        /// <summary>
        /// int32x4_t vexth_w_h(int16x8_t a)
        ///   LSX: VEXTH.W.H Vd.4W, Vj.8H
        /// </summary>
        public static Vector128<int> SignExtendWideningUpper(Vector128<short> value) => SignExtendWideningUpper(value);

        /// <summary>
        /// int64x2_t vexth_d_w(int32x4_t a)
        ///   LSX: VEXTH.D.W Vd.2D, Vj.4W
        /// </summary>
        public static Vector128<long> SignExtendWideningUpper(Vector128<int> value) => SignExtendWideningUpper(value);

        /// <summary>
        /// int128x1_t vexth_d_w(int64x2_t a)
        ///   LSX: VEXTH.Q.D Vd.Q, Vj.2D
        /// </summary>
        public static Vector128<long> SignExtendWideningUpper(Vector128<long> value) => SignExtendWideningUpper(value);

        /// <summary>
        /// int16x8_t vexth_HU_BU(int8x16_t a)
        ///   LSX: VEXTH.HU.BU Vd.8H, Vj.16B
        /// </summary>
        public static Vector128<short> ZeroExtendWideningUpper(Vector128<sbyte> value) => ZeroExtendWideningUpper(value);

        /// <summary>
        /// uint16x8_t vexth_HU_BU(uint8x16_t a)
        ///   LSX: VEXTH.HU.BU Vd.8H, Vj.16B
        /// </summary>
        public static Vector128<ushort> ZeroExtendWideningUpper(Vector128<byte> value) => ZeroExtendWideningUpper(value);

        /// <summary>
        /// int32x4_t vexth_WU_HU(int16x8_t a)
        ///   LSX: VEXTH.WU.HU Vd.4W, Vj.8H
        /// </summary>
        public static Vector128<int> ZeroExtendWideningUpper(Vector128<short> value) => ZeroExtendWideningUpper(value);

        /// <summary>
        /// uint32x4_t vexth_WU_HU(uint16x8_t a)
        ///   LSX: VEXTH.WU.HU Vd.4W, Vj.8H
        /// </summary>
        public static Vector128<uint> ZeroExtendWideningUpper(Vector128<ushort> value) => ZeroExtendWideningUpper(value);

        /// <summary>
        /// int64x2_t vexth_DU_WU(uint32x4_t a)
        ///   LSX: VEXTH.DU.WU Vd.2D, Vj.4W
        /// </summary>
        public static Vector128<long> ZeroExtendWideningUpper(Vector128<int> value) => ZeroExtendWideningUpper(value);

        /// <summary>
        /// uint64x2_t vexth_DU_WU(uint32x4_t a)
        ///   LSX: VEXTH.DU.WU Vd.2D, Vj.4W
        /// </summary>
        public static Vector128<ulong> ZeroExtendWideningUpper(Vector128<uint> value) => ZeroExtendWideningUpper(value);

        /// <summary>
        /// int128x1_t vexth_DU_WU(int64x2_t a)
        ///   LSX: VEXTH.QU.DU Vd.Q, Vj.2D
        /// </summary>
        public static Vector128<ulong> ZeroExtendWideningUpper(Vector128<long> value) => ZeroExtendWideningUpper(value);

        /// <summary>
        /// uint128x1_t vexth_DU_WU(uint64x2_t a)
        ///   LSX: VEXTH.QU.DU Vd.Q, Vj.2D
        /// </summary>
        public static Vector128<ulong> ZeroExtendWideningUpper(Vector128<ulong> value) => ZeroExtendWideningUpper(value);

        /// <summary>
        /// int8x16_t vand_v(int8x16_t a, int8x16_t b)
        ///   LSX: VAND.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> And(Vector128<sbyte> left, Vector128<sbyte> right) => And(left, right);

        /// <summary>
        /// uint8x16_t vand_v(uint8x16_t a, uint8x16_t b)
        ///   LSX: VAND.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> And(Vector128<byte> left, Vector128<byte> right) => And(left, right);

        /// <summary>
        /// int16x8_t vand_v(int16x8_t a, int16x8_t b)
        ///   LSX: VAND.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<short> And(Vector128<short> left, Vector128<short> right) => And(left, right);

        /// <summary>
        /// uint16x8_t vand_v(uint16x8_t a, uint16x8_t b)
        ///   LSX: VAND.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<ushort> And(Vector128<ushort> left, Vector128<ushort> right) => And(left, right);

        /// <summary>
        /// int32x4_t vand_v(int32x4_t a, int32x4_t b)
        ///   LSX: VAND.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<int> And(Vector128<int> left, Vector128<int> right) => And(left, right);

        /// <summary>
        /// uint32x4_t vand_v(uint32x4_t a, uint32x4_t b)
        ///   LSX: VAND.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<uint> And(Vector128<uint> left, Vector128<uint> right) => And(left, right);

        /// <summary>
        /// int64x2_t vand_v(int64x2_t a, int64x2_t b)
        ///   LSX: VAND.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<long> And(Vector128<long> left, Vector128<long> right) => And(left, right);

        /// <summary>
        /// uint64x2_t vand_v(uint64x2_t a, uint64x2_t b)
        ///   LSX: VAND.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<ulong> And(Vector128<ulong> left, Vector128<ulong> right) => And(left, right);

        /// <summary>
        /// float32x4_t vand_v(float32x4_t a, float32x4_t b)
        ///   LSX: VAND.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<float> And(Vector128<float> left, Vector128<float> right) => And(left, right);

        /// <summary>
        /// float64x2_t vand_v(float64x2_t a, float64x2_t b)
        ///   LSX: VAND.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<double> And(Vector128<double> left, Vector128<double> right) => And(left, right);

        /// <summary>
        /// int8x16_t vandn_v(int8x16_t a, int8x16_t b)
        ///   LSX: VANDN.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> AndNot(Vector128<sbyte> left, Vector128<sbyte> right) => AndNot(left, right);

        /// <summary>
        /// uint8x16_t vandn_v(uint8x16_t a, uint8x16_t b)
        ///   LSX: VANDN.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> AndNot(Vector128<byte> left, Vector128<byte> right) => AndNot(left, right);

        /// <summary>
        /// int16x8_t vandn_v(int16x8_t a, int16x8_t b)
        ///   LSX: VANDN.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<short> AndNot(Vector128<short> left, Vector128<short> right) => AndNot(left, right);

        /// <summary>
        /// uint16x8_t vandn_v(uint16x8_t a, uint16x8_t b)
        ///   LSX: VANDN.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<ushort> AndNot(Vector128<ushort> left, Vector128<ushort> right) => AndNot(left, right);

        /// <summary>
        /// int32x4_t vandn_v(int32x4_t a, int32x4_t b)
        ///   LSX: VANDN.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<int> AndNot(Vector128<int> left, Vector128<int> right) => AndNot(left, right);

        /// <summary>
        /// uint32x4_t vandn_v(uint32x4_t a, uint32x4_t b)
        ///   LSX: VANDN.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<uint> AndNot(Vector128<uint> left, Vector128<uint> right) => AndNot(left, right);

        /// <summary>
        /// int64x2_t vandn_v(int64x2_t a, int64x2_t b)
        ///   LSX: VANDN.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<long> AndNot(Vector128<long> left, Vector128<long> right) => AndNot(left, right);

        /// <summary>
        /// uint64x2_t vandn_v(uint64x2_t a, uint64x2_t b)
        ///   LSX: VANDN.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<ulong> AndNot(Vector128<ulong> left, Vector128<ulong> right) => AndNot(left, right);

        /// <summary>
        /// float32x4_t vandn_v(float32x4_t a, float32x4_t b)
        ///   LSX: VANDN.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<float> AndNot(Vector128<float> left, Vector128<float> right) => AndNot(left, right);

        /// <summary>
        /// float64x2_t vandn_v(float64x2_t a, float64x2_t b)
        ///   LSX: VANDN.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<double> AndNot(Vector128<double> left, Vector128<double> right) => AndNot(left, right);

        /// <summary>
        /// uint8x8_t vor(uint8x8_t a, uint8x8_t b)
        ///   LSX: VOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector64<byte> Or(Vector64<byte> left, Vector64<byte> right) => Or(left, right);

        /// <summary>
        /// float64x1_t vor(float64x1_t a, float64x1_t b)
        ///   LSX: VOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector64<double> Or(Vector64<double> left, Vector64<double> right) => Or(left, right);

        /// <summary>
        /// int16x4_t vor(int16x4_t a, int16x4_t b)
        ///   LSX: VOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector64<short> Or(Vector64<short> left, Vector64<short> right) => Or(left, right);

        /// <summary>
        /// int32x2_t vor(int32x2_t a, int32x2_t b)
        ///   LSX: VOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector64<int> Or(Vector64<int> left, Vector64<int> right) => Or(left, right);

        /// <summary>
        /// int64x1_t vor(int64x1_t a, int64x1_t b)
        ///   LSX: VOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector64<long> Or(Vector64<long> left, Vector64<long> right) => Or(left, right);

        /// <summary>
        /// int8x8_t vor(int8x8_t a, int8x8_t b)
        ///   LSX: VOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector64<sbyte> Or(Vector64<sbyte> left, Vector64<sbyte> right) => Or(left, right);

        /// <summary>
        /// float32x2_t vor(float32x2_t a, float32x2_t b)
        ///   LSX: VOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector64<float> Or(Vector64<float> left, Vector64<float> right) => Or(left, right);

        /// <summary>
        /// uint16x4_t vor(uint16x4_t a, uint16x4_t b)
        ///   LSX: VOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector64<ushort> Or(Vector64<ushort> left, Vector64<ushort> right) => Or(left, right);

        /// <summary>
        /// uint32x2_t vor(uint32x2_t a, uint32x2_t b)
        ///   LSX: VOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector64<uint> Or(Vector64<uint> left, Vector64<uint> right) => Or(left, right);

        /// <summary>
        /// uint64x1_t vor(uint64x1_t a, uint64x1_t b)
        ///   LSX: VOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector64<ulong> Or(Vector64<ulong> left, Vector64<ulong> right) => Or(left, right);

        /// <summary>
        /// int8x16_t vor_v(int8x16_t a, int8x16_t b)
        ///   LSX: VOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> Or(Vector128<sbyte> left, Vector128<sbyte> right) => Or(left, right);

        /// <summary>
        /// uint8x16_t vor_v(uint8x16_t a, uint8x16_t b)
        ///   LSX: VOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> Or(Vector128<byte> left, Vector128<byte> right) => Or(left, right);

        /// <summary>
        /// int16x8_t vor_v(int16x8_t a, int16x8_t b)
        ///   LSX: VOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<short> Or(Vector128<short> left, Vector128<short> right) => Or(left, right);

        /// <summary>
        /// uint16x8_t vor_v(uint16x8_t a, uint16x8_t b)
        ///   LSX: VOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<ushort> Or(Vector128<ushort> left, Vector128<ushort> right) => Or(left, right);

        /// <summary>
        /// int32x4_t vor_v(int32x4_t a, int32x4_t b)
        ///   LSX: VOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<int> Or(Vector128<int> left, Vector128<int> right) => Or(left, right);

        /// <summary>
        /// uint32x4_t vor_v(uint32x4_t a, uint32x4_t b)
        ///   LSX: VOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<uint> Or(Vector128<uint> left, Vector128<uint> right) => Or(left, right);

        /// <summary>
        /// int64x2_t vor_v(int64x2_t a, int64x2_t b)
        ///   LSX: VOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<long> Or(Vector128<long> left, Vector128<long> right) => Or(left, right);

        /// <summary>
        /// uint64x2_t vor_v(uint64x2_t a, uint64x2_t b)
        ///   LSX: VOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<ulong> Or(Vector128<ulong> left, Vector128<ulong> right) => Or(left, right);

        /// <summary>
        /// float32x4_t vor_v(float32x4_t a, float32x4_t b)
        ///   LSX: VOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<float> Or(Vector128<float> left, Vector128<float> right) => Or(left, right);

        /// <summary>
        /// float64x2_t vor_v(float64x2_t a, float64x2_t b)
        ///   LSX: VOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<double> Or(Vector128<double> left, Vector128<double> right) => Or(left, right);

        /// <summary>
        /// uint8x8_t vnori_b(uint8x8_t a)
        ///   LSX: VNORI.B Vd.16B, Vj.16B, 0
        /// </summary>
        public static Vector64<byte> Not(Vector64<byte> value) => Not(value);

        /// <summary>
        /// float64x1_t vnori_b(float64x1_t a)
        ///   LSX: VNORI.B Vd.16B, Vj.16B, 0
        /// </summary>
        public static Vector64<double> Not(Vector64<double> value) => Not(value);

        /// <summary>
        /// int16x4_t vnori_b(int16x4_t a)
        ///   LSX: VNORI.B Vd.16B, Vj.16B, 0
        /// </summary>
        public static Vector64<short> Not(Vector64<short> value) => Not(value);

        /// <summary>
        /// int32x2_t vnori_b(int32x2_t a)
        ///   LSX: VNORI.B Vd.16B, Vj.16B, 0
        /// </summary>
        public static Vector64<int> Not(Vector64<int> value) => Not(value);

        /// <summary>
        /// int64x1_t vnori_b(int64x1_t a)
        ///   LSX: VNORI.B Vd.16B, Vj.16B, 0
        /// </summary>
        public static Vector64<long> Not(Vector64<long> value) => Not(value);

        /// <summary>
        /// int8x8_t vnori_b(int8x8_t a)
        ///   LSX: VNORI.B Vd.16B, Vj.16B, 0
        /// </summary>
        public static Vector64<sbyte> Not(Vector64<sbyte> value) => Not(value);

        /// <summary>
        /// float32x2_t vnori_b(float32x2_t a)
        ///   LSX: VNORI.B Vd.16B, Vj.16B, 0
        /// </summary>
        public static Vector64<float> Not(Vector64<float> value) => Not(value);

        /// <summary>
        /// uint16x4_t vnori_b(uint16x4_t a)
        ///   LSX: VNORI.B Vd.16B, Vj.16B, 0
        /// </summary>
        public static Vector64<ushort> Not(Vector64<ushort> value) => Not(value);

        /// <summary>
        /// uint32x2_t vnori_b(uint32x2_t a)
        ///   LSX: VNORI.B Vd.16B, Vj.16B, 0
        /// </summary>
        public static Vector64<uint> Not(Vector64<uint> value) => Not(value);

        /// <summary>
        /// uint64x1_t vnori_b(uint64x1_t a)
        ///   LSX: VNORI.B Vd.16B, Vj.16B, 0
        /// </summary>
        public static Vector64<ulong> Not(Vector64<ulong> value) => Not(value);

        /// <summary>
        /// uint8x16_t vnori_b(uint8x16_t a)
        ///   LSX: VNORI.B Vd.16B, Vj.16B, 0
        /// </summary>
        public static Vector128<byte> Not(Vector128<byte> value) => Not(value);

        /// <summary>
        /// float64x2_t vnori_b(float64x2_t a)
        ///   LSX: VNORI.B Vd.16B, Vj.16B, 0
        /// </summary>
        public static Vector128<double> Not(Vector128<double> value) => Not(value);

        /// <summary>
        /// int16x8_t vnori_b(int16x8_t a)
        ///   LSX: VNORI.B Vd.16B, Vj.16B, 0
        /// </summary>
        public static Vector128<short> Not(Vector128<short> value) => Not(value);

        /// <summary>
        /// int32x4_t vnori_b(int32x4_t a)
        ///   LSX: VNORI.B Vd.16B, Vj.16B, 0
        /// </summary>
        public static Vector128<int> Not(Vector128<int> value) => Not(value);

        /// <summary>
        /// int64x2_t vnori_b(int64x2_t a)
        ///   LSX: VNORI.B Vd.16B, Vj.16B, 0
        /// </summary>
        public static Vector128<long> Not(Vector128<long> value) => Not(value);

        /// <summary>
        /// int8x16_t vnori_b(int8x16_t a)
        ///   LSX: VNORI.B Vd.16B, Vj.16B, 0
        /// </summary>
        public static Vector128<sbyte> Not(Vector128<sbyte> value) => Not(value);

        /// <summary>
        /// float32x4_t vnori_b(float32x4_t a)
        ///   LSX: VNORI.B Vd.16B, Vj.16B, 0
        /// </summary>
        public static Vector128<float> Not(Vector128<float> value) => Not(value);

        /// <summary>
        /// uint16x8_t vnori_b(uint16x8_t a)
        ///   LSX: VNORI.B Vd.16B, Vj.16B, 0
        /// </summary>
        public static Vector128<ushort> Not(Vector128<ushort> value) => Not(value);

        /// <summary>
        /// uint32x4_t vnori_b(uint32x4_t a)
        ///   LSX: VNORI.B Vd.16B, Vj.16B, 0
        /// </summary>
        public static Vector128<uint> Not(Vector128<uint> value) => Not(value);

        /// <summary>
        /// uint64x2_t vnori_b(uint64x2_t a)
        ///   LSX: VNORI.B Vd.16B, Vj.16B, 0
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<ulong> Not(Vector128<ulong> value) => Not(value);

        /// <summary>
        /// int8x16_t vnor_v(int8x16_t a, int8x16_t b)
        ///   LSX: VNOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> NotOr(Vector128<sbyte> left, Vector128<sbyte> right) => NotOr(left, right);

        /// <summary>
        /// uint8x16_t vnor_v(uint8x16_t a, uint8x16_t b)
        ///   LSX: VNOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> NotOr(Vector128<byte> left, Vector128<byte> right) => NotOr(left, right);

        /// <summary>
        /// int16x8_t vnor_v(int16x8_t a, int16x8_t b)
        ///   LSX: VNOR.V Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> NotOr(Vector128<short> left, Vector128<short> right) => NotOr(left, right);

        /// <summary>
        /// uint16x8_t vnor_v(uint16x8_t a, uint16x8_t b)
        ///   LSX: VNOR.V Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> NotOr(Vector128<ushort> left, Vector128<ushort> right) => NotOr(left, right);

        /// <summary>
        /// int32x4_t vnor_v(int32x4_t a, int32x4_t b)
        ///   LSX: VNOR.V Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<int> NotOr(Vector128<int> left, Vector128<int> right) => NotOr(left, right);

        /// <summary>
        /// uint32x4_t vnor_v(uint32x4_t a, uint32x4_t b)
        ///   LSX: VNOR.V Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<uint> NotOr(Vector128<uint> left, Vector128<uint> right) => NotOr(left, right);

        /// <summary>
        /// int64x2_t vnor_v(int64x2_t a, int64x2_t b)
        ///   LSX: VNOR.V Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> NotOr(Vector128<long> left, Vector128<long> right) => NotOr(left, right);

        /// <summary>
        /// uint64x2_t vnor_v(uint64x2_t a, uint64x2_t b)
        ///   LSX: VNOR.V Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> NotOr(Vector128<ulong> left, Vector128<ulong> right) => NotOr(left, right);

        /// <summary>
        /// float32x4_t vnor_v(float32x4_t a, float32x4_t b)
        ///   LSX: VNOR.V Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<float> NotOr(Vector128<float> left, Vector128<float> right) => NotOr(left, right);

        /// <summary>
        /// float64x2_t vnor_v(float64x2_t a, float64x2_t b)
        ///   LSX: VNOR.V Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<double> NotOr(Vector128<double> left, Vector128<double> right) => NotOr(left, right);

        /// <summary>
        /// uint8x8_t vorn(uint8x8_t a, uint8x8_t b)
        /// </summary>
        public static Vector64<byte> OrNot(Vector64<byte> left, Vector64<byte> right) => OrNot(left, right);

        /// <summary>
        /// float64x1_t vorn(float64x1_t a, float64x1_t b)
        ///   LSX: VORN.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector64<double> OrNot(Vector64<double> left, Vector64<double> right) => OrNot(left, right);

        /// <summary>
        /// int16x4_t vorn(int16x4_t a, int16x4_t b)
        ///   LSX: VORN.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector64<short> OrNot(Vector64<short> left, Vector64<short> right) => OrNot(left, right);

        /// <summary>
        /// int32x2_t vorn(int32x2_t a, int32x2_t b)
        ///   LSX: VORN.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector64<int> OrNot(Vector64<int> left, Vector64<int> right) => OrNot(left, right);

        /// <summary>
        /// int64x1_t vorn(int64x1_t a, int64x1_t b)
        ///   LSX: VORN.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector64<long> OrNot(Vector64<long> left, Vector64<long> right) => OrNot(left, right);

        /// <summary>
        /// int8x8_t vorn(int8x8_t a, int8x8_t b)
        ///   LSX: VORN.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector64<sbyte> OrNot(Vector64<sbyte> left, Vector64<sbyte> right) => OrNot(left, right);

        /// <summary>
        /// float32x2_t vorn(float32x2_t a, float32x2_t b)
        ///   LSX: VORN.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector64<float> OrNot(Vector64<float> left, Vector64<float> right) => OrNot(left, right);

        /// <summary>
        /// uint16x4_t vorn(uint16x4_t a, uint16x4_t b)
        ///   LSX: VORN.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector64<ushort> OrNot(Vector64<ushort> left, Vector64<ushort> right) => OrNot(left, right);

        /// <summary>
        /// uint32x2_t vorn(uint32x2_t a, uint32x2_t b)
        ///   LSX: VORN.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector64<uint> OrNot(Vector64<uint> left, Vector64<uint> right) => OrNot(left, right);

        /// <summary>
        /// uint64x1_t vorn(uint64x1_t a, uint64x1_t b)
        ///   LSX: VORN.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector64<ulong> OrNot(Vector64<ulong> left, Vector64<ulong> right) => OrNot(left, right);

        /// <summary>
        /// int8x16_t vorn_v(int8x16_t a, int8x16_t b)
        ///   LSX: VORN.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> OrNot(Vector128<sbyte> left, Vector128<sbyte> right) => OrNot(left, right);

        /// <summary>
        /// uint8x16_t vorn_v(uint8x16_t a, uint8x16_t b)
        ///   LSX: VORN.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> OrNot(Vector128<byte> left, Vector128<byte> right) => OrNot(left, right);

        /// <summary>
        /// int16x8_t vor_v(int16x8_t a, int16x8_t b)
        ///   LSX: VORN.V Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> OrNot(Vector128<short> left, Vector128<short> right) => OrNot(left, right);

        /// <summary>
        /// uint16x8_t vor_v(uint16x8_t a, uint16x8_t b)
        ///   LSX: VORN.V Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> OrNot(Vector128<ushort> left, Vector128<ushort> right) => OrNot(left, right);

        /// <summary>
        /// int32x4_t vorn_v(int32x4_t a, int32x4_t b)
        ///   LSX: VORN.V Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<int> OrNot(Vector128<int> left, Vector128<int> right) => OrNot(left, right);

        /// <summary>
        /// uint32x4_t vorn_v(uint32x4_t a, uint32x4_t b)
        ///   LSX: VORN.V Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<uint> OrNot(Vector128<uint> left, Vector128<uint> right) => OrNot(left, right);

        /// <summary>
        /// int64x2_t vorn_v(int64x2_t a, int64x2_t b)
        ///   LSX: VORN.V Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> OrNot(Vector128<long> left, Vector128<long> right) => OrNot(left, right);

        /// <summary>
        /// uint64x2_t vorn_v(uint64x2_t a, uint64x2_t b)
        ///   LSX: VORN.V Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> OrNot(Vector128<ulong> left, Vector128<ulong> right) => OrNot(left, right);

        /// <summary>
        /// float32x4_t vorn_v(float32x4_t a, float32x4_t b)
        ///   LSX: VORN.V Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<float> OrNot(Vector128<float> left, Vector128<float> right) => OrNot(left, right);

        /// <summary>
        /// float64x2_t vorn_v(float64x2_t a, float64x2_t b)
        ///   LSX: VORN.V Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<double> OrNot(Vector128<double> left, Vector128<double> right) => OrNot(left, right);

        /// <summary>
        /// uint8x8_t vxor(uint8x8_t a, uint8x8_t b)
        ///   LSX: VXOR.V Vd.8B, Vj.8B, Vk.8B
        /// </summary>
        public static Vector64<byte> Xor(Vector64<byte> left, Vector64<byte> right) => Xor(left, right);

        /// <summary>
        /// float64x1_t vxor(float64x1_t a, float64x1_t b)
        ///   LSX: VXOR.V Vd.8B, Vj.8B, Vk.8B
        /// </summary>
        public static Vector64<double> Xor(Vector64<double> left, Vector64<double> right) => Xor(left, right);

        /// <summary>
        /// int16x4_t vxor(int16x4_t a, int16x4_t b)
        ///   LSX: VXOR.V Vd.8B, Vj.8B, Vk.8B
        /// </summary>
        public static Vector64<short> Xor(Vector64<short> left, Vector64<short> right) => Xor(left, right);

        /// <summary>
        /// int32x2_t vxor(int32x2_t a, int32x2_t b)
        ///   LSX: VXOR.V Vd.8B, Vj.8B, Vk.8B
        /// </summary>
        public static Vector64<int> Xor(Vector64<int> left, Vector64<int> right) => Xor(left, right);

        /// <summary>
        /// int64x1_t vxor(int64x1_t a, int64x1_t b)
        ///   LSX: VXOR.V Vd.8B, Vj.8B, Vk.8B
        /// </summary>
        public static Vector64<long> Xor(Vector64<long> left, Vector64<long> right) => Xor(left, right);

        /// <summary>
        /// int8x8_t vxor(int8x8_t a, int8x8_t b)
        ///   LSX: VXOR.V Vd.8B, Vj.8B, Vk.8B
        /// </summary>
        public static Vector64<sbyte> Xor(Vector64<sbyte> left, Vector64<sbyte> right) => Xor(left, right);

        /// <summary>
        /// float32x2_t vxor(float32x2_t a, float32x2_t b)
        ///   LSX: VXOR.V Vd.8B, Vj.8B, Vk.8B
        /// </summary>
        public static Vector64<float> Xor(Vector64<float> left, Vector64<float> right) => Xor(left, right);

        /// <summary>
        /// uint16x4_t vxor(uint16x4_t a, uint16x4_t b)
        ///   LSX: VXOR.V Vd.8B, Vj.8B, Vk.8B
        /// </summary>
        public static Vector64<ushort> Xor(Vector64<ushort> left, Vector64<ushort> right) => Xor(left, right);

        /// <summary>
        /// uint32x2_t vxor(uint32x2_t a, uint32x2_t b)
        ///   LSX: VXOR.V Vd.8B, Vj.8B, Vk.8B
        /// </summary>
        public static Vector64<uint> Xor(Vector64<uint> left, Vector64<uint> right) => Xor(left, right);

        /// <summary>
        /// uint64x1_t vxor(uint64x1_t a, uint64x1_t b)
        ///   LSX: VXOR.V Vd.8B, Vj.8B, Vk.8B
        /// </summary>
        public static Vector64<ulong> Xor(Vector64<ulong> left, Vector64<ulong> right) => Xor(left, right);

        /// <summary>
        /// int8x16_t vxor_v(int8x16_t a, int8x16_t b)
        ///   LSX: VXOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> Xor(Vector128<sbyte> left, Vector128<sbyte> right) => Xor(left, right);

        /// <summary>
        /// uint8x16_t vxor_v(uint8x16_t a, uint8x16_t b)
        ///   LSX: VXOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> Xor(Vector128<byte> left, Vector128<byte> right) => Xor(left, right);

        /// <summary>
        /// int16x8_t vxor_v(int16x8_t a, int16x8_t b)
        ///   LSX: VXOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<short> Xor(Vector128<short> left, Vector128<short> right) => Xor(left, right);

        /// <summary>
        /// uint16x8_t vxor_v(uint16x8_t a, uint16x8_t b)
        ///   LSX: VXOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<ushort> Xor(Vector128<ushort> left, Vector128<ushort> right) => Xor(left, right);

        /// <summary>
        /// int32x4_t vxor_v(int32x4_t a, int32x4_t b)
        ///   LSX: VXOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<int> Xor(Vector128<int> left, Vector128<int> right) => Xor(left, right);

        /// <summary>
        /// uint32x4_t vxor_v(uint32x4_t a, uint32x4_t b)
        ///   LSX: VXOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<uint> Xor(Vector128<uint> left, Vector128<uint> right) => Xor(left, right);

        /// <summary>
        /// int64x2_t vxor_v(int64x2_t a, int64x2_t b)
        ///   LSX: VXOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<long> Xor(Vector128<long> left, Vector128<long> right) => Xor(left, right);

        /// <summary>
        /// uint64x2_t vxor_v(uint64x2_t a, uint64x2_t b)
        ///   LSX: VXOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<ulong> Xor(Vector128<ulong> left, Vector128<ulong> right) => Xor(left, right);

        /// <summary>
        /// float32x4_t vxor_v(float32x4_t a, float32x4_t b)
        ///   LSX: VXOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<float> Xor(Vector128<float> left, Vector128<float> right) => Xor(left, right);

        /// <summary>
        /// float64x2_t vxor_v(float64x2_t a, float64x2_t b)
        ///   LSX: VXOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<double> Xor(Vector128<double> left, Vector128<double> right) => Xor(left, right);

        /// <summary>
        /// int8x16_t vslli_b(int8x16_t a, const int n)
        ///   LSX: VSLLI.B Vd.16B, Vj.16B, ui3
        /// </summary>
        public static Vector128<sbyte> ShiftLeftLogical(Vector128<sbyte> value, const byte shift) => ShiftLeftLogical(value, shift);

        /// <summary>
        /// uint8x16_t vslli_b(uint8x16_t a, const int n)
        ///   LSX: VSLLI.B Vd.16B, Vj.16B, ui3
        /// </summary>
        public static Vector128<byte> ShiftLeftLogical(Vector128<byte> value, const byte shift) => ShiftLeftLogical(value, shift);

        /// <summary>
        /// int16x8_t vslli_h(int16x8_t a, const int n)
        ///   LSX: VSLLI.H Vd.8H, Vj.8H, ui4
        /// </summary>
        public static Vector128<short> ShiftLeftLogical(Vector128<short> value, const byte shift) => ShiftLeftLogical(value, shift);

        /// <summary>
        /// uint16x8_t vslli_h(uint16x8_t a, const int n)
        ///   LSX: VSLLI.H Vd.8H, Vj.8H, ui4
        /// </summary>
        public static Vector128<ushort> ShiftLeftLogical(Vector128<ushort> value, const byte shift) => ShiftLeftLogical(value, shift);

        /// <summary>
        /// uint32x4_t vslli_w(uint32x4_t a, const int n)
        ///   LSX: VSLLI.W Vd.4W, Vj.4W, ui5
        /// </summary>
        public static Vector128<int> ShiftLeftLogical(Vector128<int> value, const byte shift) => ShiftLeftLogical(value, shift);

        /// <summary>
        /// uint32x4_t vslli_w(uint32x4_t a, const int n)
        ///   LSX: VSLLI.W Vd.4W, Vj.4W, ui5
        /// </summary>
        public static Vector128<uint> ShiftLeftLogical(Vector128<uint> value, const byte shift) => ShiftLeftLogical(value, shift);

        /// <summary>
        /// int64x2_t vslli_d(int64x2_t a, const int n)
        ///   LSX: VSLLI.D Vd.2D, Vj.2D, ui6
        /// </summary>
        public static Vector128<long> ShiftLeftLogical(Vector128<long> value, const byte shift) => ShiftLeftLogical(value, shift);

        /// <summary>
        /// uint64x2_t vslli_d(uint64x2_t a, const int n)
        ///   LSX: VSLLI.D Vd.2D, Vj.2D, ui6
        /// </summary>
        public static Vector128<ulong> ShiftLeftLogical(Vector128<ulong> value, const byte shift) => ShiftLeftLogical(value, shift);

        /// <summary>
        /// int8x16_t vbsll_v(int8x16_t a, const int shift)
        ///   LSX: VBSLL.V Vd.16B, Vj.16B, ui4
        /// </summary>
        public static Vector128<sbyte> ShiftLeftLogicalByByte(Vector128<sbyte> value, [ConstantExpected(Max = (byte)(15))] byte shift) => ShiftLeftLogicalByByte(value, shift);

        /// <summary>
        /// uint8x16_t vbsll_v(uint8x16_t a, const int shift)
        ///   LSX: VBSLL.V Vd.16B, Vj.16B, ui4
        /// </summary>
        public static Vector128<byte> ShiftLeftLogicalByByte(Vector128<byte> value, [ConstantExpected(Max = (byte)(15))] byte shift) => ShiftLeftLogicalByByte(value, shift);

        /// <summary>
        /// int8x16_t vsll_b(int8x16_t a, int8x16_t b)
        ///   LSX: VSLL.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> ShiftLeftLogical(Vector128<sbyte> value, Vector128<sbyte> shift) => ShiftLeftLogical(value, shift);

        /// <summary>
        /// uint8x16_t vsll_b(uint8x16_t a, uint8x16_t b)
        ///   LSX: VSLL.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> ShiftLeftLogical(Vector128<byte> value, Vector128<byte> shift) => ShiftLeftLogical(value, shift);

        /// <summary>
        /// int16x8_t vsll_h(int16x8_t value, int16x8_t shift)
        ///   LSX: VSLL.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> ShiftLeftLogical(Vector128<short> value, Vector128<short> shift) => ShiftLeftLogical(value, shift);

        /// <summary>
        /// uint16x8_t vsll_h(uint16x8_t value, uint16x8_t shift)
        ///   LSX: VSLL.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> ShiftLeftLogical(Vector128<ushort> value, Vector128<ushort> shift) => ShiftLeftLogical(value, shift);

        /// <summary>
        /// int32x4_t vsll_w(int32x4_t value, int32x4_t shift)
        ///   LSX: VSLL.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<int> ShiftLeftLogical(Vector128<int> value, Vector128<int> shift) => ShiftLeftLogical(value, shift);

        /// <summary>
        /// uint32x4_t vsll_w(uint32x4_t value, uint32x4_t shift)
        ///   LSX: VSLL.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<uint> ShiftLeftLogical(Vector128<uint> value, Vector128<uint> shift) => ShiftLeftLogical(value, shift);

        /// <summary>
        /// int64x2_t vsll_d(int64x2_t value, int64x2_t shift)
        ///   LSX: VSLL.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> ShiftLeftLogical(Vector128<long> value, Vector128<long> shift) => ShiftLeftLogical(value, shift);

        /// <summary>
        /// uint64x2_t vsll_d(uint64x2_t value, uint64x2_t shift)
        ///   LSX: VSLL.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> ShiftLeftLogical(Vector128<ulong> value, Vector128<ulong> shift) => ShiftLeftLogical(value, shift);

        /// <summary>
        /// uint8x16_t vsrli_b(uint8x16_t a, const int n)
        ///   LSX: VSRLI.B Vd.16B, Vj.16B, ui3
        /// </summary>
        public static Vector128<sbyte> ShiftRightLogical(Vector128<sbyte> value, const byte shift) => ShiftRightLogical(value, shift);

        /// <summary>
        /// uint8x16_t vsrli_b(uint8x16_t a, const int n)
        ///   LSX: VSRLI.B Vd.16B, Vj.16B, ui3
        /// </summary>
        public static Vector128<byte> ShiftRightLogical(Vector128<byte> value, const byte shift) => ShiftRightLogical(value, shift);

        /// <summary>
        /// uint16x8_t vsrli_h(uint16x8_t a, const int n)
        ///   LSX: VSRLI.H Vd.8H, Vj.8H, ui4
        /// </summary>
        public static Vector128<short> ShiftRightLogical(Vector128<short> value, const byte shift) => ShiftRightLogical(value, shift);

        /// <summary>
        /// uint16x8_t vsrli_h(uint16x8_t a, const int n)
        ///   LSX: VSRLI.H Vd.8H, Vj.8H, ui4
        /// </summary>
        public static Vector128<ushort> ShiftRightLogical(Vector128<ushort> value, const byte shift) => ShiftRightLogical(value, shift);

        /// <summary>
        /// uint32x4_t vsrli_w(uint32x4_t a, const int n)
        ///   LSX: VSRLI.W Vd.4W, Vj.4W, ui5
        /// </summary>
        public static Vector128<int> ShiftRightLogical(Vector128<int> value, const byte shift) => ShiftRightLogical(value, shift);

        /// <summary>
        /// uint32x4_t vsrli_w(uint32x4_t a, const int n)
        ///   LSX: VSRLI.W Vd.4W, Vj.4W, ui5
        /// </summary>
        public static Vector128<uint> ShiftRightLogical(Vector128<uint> value, const byte shift) => ShiftRightLogical(value, shift);

        /// <summary>
        /// uint64x2_t vsrli_d(uint64x2_t a, const int n)
        ///   LSX: VSRLI.D Vd.2D, Vj.2D, ui6
        /// </summary>
        public static Vector128<long> ShiftRightLogical(Vector128<long> value, const byte shift) => ShiftRightLogical(value, shift);

        /// <summary>
        /// uint64x2_t vsrli_d(uint64x2_t a, const int n)
        ///   LSX: VSRLI.D Vd.2D, Vj.2D, ui6
        /// </summary>
        public static Vector128<ulong> ShiftRightLogical(Vector128<ulong> value, const byte shift) => ShiftRightLogical(value, shift);

        /// <summary>
        /// int8x16_t vbsrl_v(int8x16_t a, const int shift)
        ///   LSX: VBSRL.V Vd.16B, Vj.16B, ui4
        /// </summary>
        public static Vector128<sbyte> ShiftRightLogicalByByte(Vector128<sbyte> value, [ConstantExpected(Max = (byte)(15))] byte shift) => ShiftRightLogicalByByte(value, shift);

        /// <summary>
        /// uint8x16_t vbsrl_v(uint8x16_t a, const int shift)
        ///   LSX: VBSRL.V Vd.16B, Vj.16B, ui4
        /// </summary>
        public static Vector128<byte> ShiftRightLogicalByByte(Vector128<byte> value, [ConstantExpected(Max = (byte)(15))] byte shift) => ShiftRightLogicalByByte(value, shift);

        /// <summary>
        /// int8x16_t vsrl_b(int8x16_t a, int8x16_t b)
        ///   LSX: VSRL.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> ShiftRightLogical(Vector128<sbyte> value, Vector128<sbyte> shift) => ShiftRightLogical(value, shift);

        /// <summary>
        /// uint8x16_t vsrl_b(uint8x16_t a, uint8x16_t b)
        ///   LSX: VSRL.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> ShiftRightLogical(Vector128<byte> value, Vector128<byte> shift) => ShiftRightLogical(value, shift);

        /// <summary>
        /// int16x8_t vsrl_h(int16x8_t value, int16x8_t shift)
        ///   LSX: VSRL.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> ShiftRightLogical(Vector128<short> value, Vector128<short> shift) => ShiftRightLogical(value, shift);

        /// <summary>
        /// uint16x8_t vsrl_h(uint16x8_t value, uint16x8_t shift)
        ///   LSX: VSRL.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> ShiftRightLogical(Vector128<ushort> value, Vector128<ushort> shift) => ShiftRightLogical(value, shift);

        /// <summary>
        /// int32x4_t vsrl_w(int32x4_t value, int32x4_t shift)
        ///   LSX: VSRL.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<int> ShiftRightLogical(Vector128<int> value, Vector128<int> shift) => ShiftRightLogical(value, shift);

        /// <summary>
        /// uint32x4_t vsrl_w(uint32x4_t value, uint32x4_t shift)
        ///   LSX: VSRL.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<uint> ShiftRightLogical(Vector128<uint> value, Vector128<uint> shift) => ShiftRightLogical(value, shift);

        /// <summary>
        /// int64x2_t vsrl_d(int64x2_t value, int64x2_t shift)
        ///   LSX: VSRL.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> ShiftRightLogical(Vector128<long> value, Vector128<long> shift) => ShiftRightLogical(value, shift);

        /// <summary>
        /// uint64x2_t vsrl_d(uint64x2_t value, uint64x2_t shift)
        ///   LSX: VSRL.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> ShiftRightLogical(Vector128<ulong> value, Vector128<ulong> shift) => ShiftRightLogical(value, shift);

        /// <summary>
        /// uint8x16_t vsrlri_b(uint8x16_t a, const int n)
        ///   LSX: VSRLRI.B Vd.16B, Vj.16B, ui3
        /// </summary>
        public static Vector128<sbyte> ShiftRightLogicalRounded(Vector128<sbyte> value, const byte shift) => ShiftRightLogicalRounded(value, shift);

        /// <summary>
        /// uint8x16_t vsrlri_b(uint8x16_t a, const int n)
        ///   LSX: VSRLRI.B Vd.16B, Vj.16B, ui3
        /// </summary>
        public static Vector128<byte> ShiftRightLogicalRounded(Vector128<byte> value, const byte shift) => ShiftRightLogicalRounded(value, shift);

        /// <summary>
        /// uint16x8_t vsrlri_h(uint16x8_t a, const int n)
        ///   LSX: VSRLRI.H Vd.8H, Vj.8H, ui4
        /// </summary>
        public static Vector128<short> ShiftRightLogicalRounded(Vector128<short> value, const byte shift) => ShiftRightLogicalRounded(value, shift);

        /// <summary>
        /// uint16x8_t vsrlri_h(uint16x8_t a, const int n)
        ///   LSX: VSRLRI.H Vd.8H, Vj.8H, ui4
        /// </summary>
        public static Vector128<ushort> ShiftRightLogicalRounded(Vector128<ushort> value, const byte shift) => ShiftRightLogicalRounded(value, shift);

        /// <summary>
        /// uint32x4_t vsrlri_w(uint32x4_t a, const int n)
        ///   LSX: VSRLRI.W Vd.4W, Vj.4W, ui5
        /// </summary>
        public static Vector128<int> ShiftRightLogicalRounded(Vector128<int> value, const byte shift) => ShiftRightLogicalRounded(value, shift);

        /// <summary>
        /// uint32x4_t vsrlri_w(uint32x4_t a, const int n)
        ///   LSX: VSRLRI.W Vd.4W, Vj.4W, ui5
        /// </summary>
        public static Vector128<uint> ShiftRightLogicalRounded(Vector128<uint> value, const byte shift) => ShiftRightLogicalRounded(value, shift);

        /// <summary>
        /// uint64x2_t vsrlri_d(uint64x2_t a, const int n)
        ///   LSX: VSRLRI.D Vd.2D, Vj.2D, ui6
        /// </summary>
        public static Vector128<long> ShiftRightLogicalRounded(Vector128<long> value, const byte shift) => ShiftRightLogicalRounded(value, shift);

        /// <summary>
        /// uint64x2_t vsrlri_d(uint64x2_t a, const int n)
        ///   LSX: VSRLRI.D Vd.2D, Vj.2D, ui6
        /// </summary>
        public static Vector128<ulong> ShiftRightLogicalRounded(Vector128<ulong> value, const byte shift) => ShiftRightLogicalRounded(value, shift);

        /// <summary>
        /// int8x16_t vsrlr_b(int8x16_t a, int8x16_t b)
        ///   LSX: VSRLR.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> ShiftRightLogicalRounded(Vector128<sbyte> value, Vector128<sbyte> shift) => ShiftRightLogicalRounded(value, shift);

        /// <summary>
        /// uint8x16_t vsrlr_b(uint8x16_t a, uint8x16_t b)
        ///   LSX: VSRLR.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> ShiftRightLogicalRounded(Vector128<byte> value, Vector128<byte> shift) => ShiftRightLogicalRounded(value, shift);

        /// <summary>
        /// int16x8_t vsrlr_h(int16x8_t value, int16x8_t shift)
        ///   LSX: VSRLR.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> ShiftRightLogicalRounded(Vector128<short> value, Vector128<short> shift) => ShiftRightLogicalRounded(value, shift);

        /// <summary>
        /// uint16x8_t vsrlr_h(uint16x8_t value, uint16x8_t shift)
        ///   LSX: VSRLR.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> ShiftRightLogicalRounded(Vector128<ushort> value, Vector128<ushort> shift) => ShiftRightLogicalRounded(value, shift);

        /// <summary>
        /// int32x4_t vsrlr_w(int32x4_t value, int32x4_t shift)
        ///   LSX: VSRLR.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<int> ShiftRightLogicalRounded(Vector128<int> value, Vector128<int> shift) => ShiftRightLogicalRounded(value, shift);

        /// <summary>
        /// uint32x4_t vsrlr_w(uint32x4_t value, uint32x4_t shift)
        ///   LSX: VSRLR.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<uint> ShiftRightLogicalRounded(Vector128<uint> value, Vector128<uint> shift) => ShiftRightLogicalRounded(value, shift);

        /// <summary>
        /// int64x2_t vsrlr_d(int64x2_t value, int64x2_t shift)
        ///   LSX: VSRLR.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> ShiftRightLogicalRounded(Vector128<long> value, Vector128<long> shift) => ShiftRightLogicalRounded(value, shift);

        /// <summary>
        /// uint64x2_t vsrlr_d(uint64x2_t value, uint64x2_t shift)
        ///   LSX: VSRLR.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> ShiftRightLogicalRounded(Vector128<ulong> value, Vector128<ulong> shift) => ShiftRightLogicalRounded(value, shift);

        /// <summary>
        /// uint8x16_t vsrlrni_b_h(uint16x8_t left, uint16x8_t right, const int n)
        ///   LSX: VSRLRNI.B.H Vd, Vj, ui4    ///NOTE: The Vd is both input and output, so the left shoule be ref type!!!
        /// </summary>
        public static Vector128<byte> ShiftRightLogicalRoundedNarrowingLower(Vector128<ushort> left, Vector128<ushort> right, [ConstantExpected(Min = 0, Max = (byte)(15))] const byte shift) => ShiftRightLogicalRoundedNarrowingLower(left, right, shift);

        /// <summary>
        /// int8x16_t vsrlrni_b_h(int16x8_t left, int16x8_t right, const int n)
        ///   LSX: VSRLRNI.B.H Vd, Vj, ui4
        /// </summary>
        public static Vector128<sbyte> ShiftRightLogicalRoundedNarrowingLower(Vector128<short> left, Vector128<short> right, [ConstantExpected(Min = 0, Max = (byte)(15))] const byte shift) => ShiftRightLogicalRoundedNarrowingLower(left, right, shift);

        /// <summary>
        /// int16x8_t vsrlrni_h_w(int32x4_t left, int32x4_t right, const int n)
        ///   LSX: VSRLRNI.H.W Vd, Vj, ui5
        /// </summary>
        public static Vector128<short> ShiftRightLogicalRoundedNarrowingLower(Vector128<int> left, Vector128<int> right, [ConstantExpected(Min = 0, Max = (byte)(31))] const byte shift) => ShiftRightLogicalRoundedNarrowingLower(left, right, shift);

        /// <summary>
        /// uint16x8_t vsrlrni_h_w(uint32x4_t left, uint32x4_t right, const int n)
        ///   LSX: VSRLRNI.H.W Vd, Vj, ui5
        /// </summary>
        public static Vector128<ushort> ShiftRightLogicalRoundedNarrowingLower(Vector128<uint> left, Vector128<uint> right, [ConstantExpected(Min = 0, Max = (byte)(31))] const byte shift) => ShiftRightLogicalRoundedNarrowingLower(left, right, shift);

        /// <summary>
        /// int32x4_t vsrlrni_w_d(int64x2_t left, int64x2_t right, const int n)
        ///   LSX: VSRLRNI.W.D Vd, Vj, ui6
        /// </summary>
        public static Vector128<int> ShiftRightLogicalRoundedNarrowingLower(Vector128<long> left, Vector128<long> right, [ConstantExpected(Min = 0, Max = (byte)(63))] const byte shift) => ShiftRightLogicalRoundedNarrowingLower(left, right, shift);

        /// <summary>
        /// uint32x4_t vsrlrni_w_d(uint64x2_t left, uint64x2_t right, const int n)
        ///   LSX: VSRLRNI.W.D Vd, Vj, ui6
        /// </summary>
        public static Vector128<uint> ShiftRightLogicalRoundedNarrowingLower(Vector128<ulong> left, Vector128<ulong> right, [ConstantExpected(Min = 0, Max = (byte)(63))] const byte shift) => ShiftRightLogicalRoundedNarrowingLower(left, right, shift);

        ///// <summary>
        ///// int64x2_t vsrlrni_d_q(int128x1_t left, int128x1_t right, const int n)
        /////   LSX: VSRLRNI.D.Q Vd, Vj, ui7
        ///// </summary>
        //public static Vector128<long> ShiftRightLogicalRoundedNarrowingLower(Vector128<longlong> left, Vector128<longlong> right, [ConstantExpected(Min = 0, Max = (byte)(127))] const byte shift) => ShiftRightLogicalRoundedNarrowingLower(left, right, shift);

        /// <summary>
        /// int8x8_t vsrlrn_b_h(int16x8_t value, int16x8_t shift)
        ///   LSX: VSRLRN.B.H Vd.8B, Vj.8H, Vk.8H
        /// </summary>
        public static Vector64<sbyte> ShiftRightLogicalRoundedNarrowingLower(Vector128<short> value, Vector128<short> shift) => ShiftRightLogicalRoundedNarrowingLower(value, shift);

        /// <summary>
        /// uint8x8_t vsrlrn_b_h(uint16x8_t value, uint16x8_t shift)
        ///   LSX: VSRLRN.B.H Vd.8B, Vj.8H, Vk.8H
        /// </summary>
        public static Vector64<byte> ShiftRightLogicalRoundedNarrowingLower(Vector128<ushort> value, Vector128<ushort> shift) => ShiftRightLogicalRoundedNarrowingLower(value, shift);

        /// <summary>
        /// int16x4_t vsrlrn_h_w(int32x4_t value, int32x4_t shift)
        ///   LSX: VSRLRN.H.W Vd.4H, Vj.4W, Vk.4W
        /// </summary>
        public static Vector64<short> ShiftRightLogicalRoundedNarrowingLower(Vector128<int> value, Vector128<int> shift) => ShiftRightLogicalRoundedNarrowingLower(value, shift);

        /// <summary>
        /// uint16x4_t vsrlrn_h_w(uint32x4_t value, uint32x4_t shift)
        ///   LSX: VSRLRN.H.W Vd.4H, Vj.4W, Vk.4W
        /// </summary>
        public static Vector64<ushort> ShiftRightLogicalRoundedNarrowingLower(Vector128<uint> value, Vector128<uint> shift) => ShiftRightLogicalRoundedNarrowingLower(value, shift);

        /// <summary>
        /// int32x2_t vsrlrn_w_d(int64x2_t value, int64x2_t shift)
        ///   LSX: VSRLRN.W.D Vd.2W, Vj.2D, Vk.2D
        /// </summary>
        public static Vector64<int> ShiftRightLogicalRoundedNarrowingLower(Vector128<long> value, Vector128<long> shift) => ShiftRightLogicalRoundedNarrowingLower(value, shift);

        /// <summary>
        /// uint32x2_t vsrlrn_w_d(uint64x2_t value, uint64x2_t shift)
        ///   LSX: VSRLRN.W.D Vd.2W, Vj.2D, Vk.2D
        /// </summary>
        public static Vector64<uint> ShiftRightLogicalRoundedNarrowingLower(Vector128<ulong> value, Vector128<ulong> shift) => ShiftRightLogicalRoundedNarrowingLower(value, shift);

        /// <summary>
        /// int8x16_t vsrai_b(int8x16_t a, const int n)
        ///   LSX: VSRAI.B Vd.16B, Vj.16B, ui3
        /// </summary>
        public static Vector128<sbyte> ShiftRightArithmetic(Vector128<sbyte> value, const byte shift) => ShiftRightArithmetic(value, shift);

        /// <summary>
        /// int16x8_t vsrai_h(int16x8_t a, const int n)
        ///   LSX: VSRAI.H Vd.8H, Vj.8H, ui4
        /// </summary>
        public static Vector128<short> ShiftRightArithmetic(Vector128<short> value, const byte shift) => ShiftRightArithmetic(value, shift);

        /// <summary>
        /// int32x4_t vsrai_w(int32x4_t a, const int n)
        ///   LSX: VSRAI.W Vd.4W, Vj.4W, ui5
        /// </summary>
        public static Vector128<int> ShiftRightArithmetic(Vector128<int> value, const byte shift) => ShiftRightArithmetic(value, shift);

        /// <summary>
        /// int64x2_t vsrai_d(int64x2_t a, const int n)
        ///   LSX: VSRAI.D Vd.2D, Vj.2D, ui6
        /// </summary>
        public static Vector128<long> ShiftRightArithmetic(Vector128<long> value, const byte shift) => ShiftRightArithmetic(value, shift);

        /// <summary>
        /// uint8x16_t vsra_b(uint8x16_t a, uint8x16_t b)
        ///   LSX: VSRA.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> ShiftRightArithmetic(Vector128<byte> value, Vector128<byte> shift) => ShiftRightArithmetic(value, shift);

        /// <summary>
        /// int16x8_t vsra_h(int16x8_t value, int16x8_t shift)
        ///   LSX: VSRA.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> ShiftRightArithmetic(Vector128<short> value, Vector128<short> shift) => ShiftRightArithmetic(value, shift);

        /// <summary>
        /// int32x4_t vsra_w(int32x4_t value, int32x4_t shift)
        ///   LSX: VSRA.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<int> ShiftRightArithmetic(Vector128<int> value, Vector128<int> shift) => ShiftRightArithmetic(value, shift);

        /// <summary>
        /// int64x2_t vsra_d(int64x2_t value, int64x2_t shift)
        ///   LSX: VSRA.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> ShiftRightArithmetic(Vector128<long> value, Vector128<long> shift) => ShiftRightArithmetic(value, shift);

        /// <summary>
        /// int8x16_t vsrari_b(int8x16_t a, const int n)
        ///   LSX: VSRARI.B Vd.16B, Vj.16B, ui3
        /// </summary>
        public static Vector128<sbyte> ShiftRightArithmeticRounded(Vector128<sbyte> value, const byte shift) => ShiftRightArithmeticRounded(value, shift);

        /// <summary>
        /// int16x8_t vsrari_h(int16x8_t a, const int n)
        ///   LSX: VSRARI.H Vd.8H, Vj.8H, ui4
        /// </summary>
        public static Vector128<short> ShiftRightArithmeticRounded(Vector128<short> value, const byte shift) => ShiftRightArithmeticRounded(value, shift);

        /// <summary>
        /// int32x4_t vsrari_w(int32x4_t a, const int n)
        ///   LSX: VSRARI.W Vd.4W, Vj.4W, ui5
        /// </summary>
        public static Vector128<int> ShiftRightArithmeticRounded(Vector128<int> value, const byte shift) => ShiftRightArithmeticRounded(value, shift);

        /// <summary>
        /// int64x2_t vsrari_d(int64x2_t a, const int n)
        ///   LSX: VSRARI.D Vd.2D, Vj.2D, ui6
        /// </summary>
        public static Vector128<long> ShiftRightArithmeticRounded(Vector128<long> value, const byte shift) => ShiftRightArithmeticRounded(value, shift);

        /// <summary>
        /// int8x16_t vsrar_b(int8x16_t a, int8x16_t b)
        ///   LSX: VSRAR.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> ShiftRightArithmeticRounded(Vector128<sbyte> value, Vector128<sbyte> shift) => ShiftRightArithmeticRounded(value, shift);

        /// <summary>
        /// int16x8_t vsrar_h(int16x8_t value, int16x8_t shift)
        ///   LSX: VSRAR.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> ShiftRightArithmeticRounded(Vector128<short> value, Vector128<short> shift) => ShiftRightArithmeticRounded(value, shift);

        /// <summary>
        /// int32x4_t vsrar_w(int32x4_t value, int32x4_t shift)
        ///   LSX: VSRAR.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<int> ShiftRightArithmeticRounded(Vector128<int> value, Vector128<int> shift) => ShiftRightArithmeticRounded(value, shift);

        /// <summary>
        /// int64x2_t vsrar_d(int64x2_t value, int64x2_t shift)
        ///   LSX: VSRAR.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> ShiftRightArithmeticRounded(Vector128<long> value, Vector128<long> shift) => ShiftRightArithmeticRounded(value, shift);

        /// <summary>
        /// int8x16_t vsrarni_b_h(int16x8_t left, int16x8_t right, const int n)
        ///   LSX: VSRARNI.B.H Vd, Vj, ui4    ///NOTE: The Vd is both input and output, so the left shoule be ref type!!!
        /// </summary>
        public static Vector128<sbyte> ShiftRightArithmeticRoundedNarrowingLower(Vector128<short> left, Vector128<short> right, [ConstantExpected(Min = 0, Max = (byte)(15))] const byte shift) => ShiftRightArithmeticRoundedNarrowingLower(left, right, shift);

        /// <summary>
        /// int16x8_t vsrarni_h_w(int32x4_t left, int32x4_t right, const int n)
        ///   LSX: VSRARNI.H.W Vd, Vj, ui5
        /// </summary>
        public static Vector128<short> ShiftRightArithmeticRoundedNarrowingLower(Vector128<int> left, Vector128<int> right, [ConstantExpected(Min = 0, Max = (byte)(31))] const byte shift) => ShiftRightArithmeticRoundedNarrowingLower(left, right, shift);

        /// <summary>
        /// int32x4_t vsrarni_w_d(int64x2_t left, int64x2_t right, const int n)
        ///   LSX: VSRARNI.W.D Vd, Vj, ui6
        /// </summary>
        public static Vector128<int> ShiftRightArithmeticRoundedNarrowingLower(Vector128<long> left, Vector128<long> right, [ConstantExpected(Min = 0, Max = (byte)(63))] const byte shift) => ShiftRightArithmeticRoundedNarrowingLower(left, right, shift);

        ///// <summary>
        ///// int64x2_t vsrarni_d_q(int128x1_t left, int128x1_t right, const int n)
        /////   LSX: VSRARNI.D.Q Vd, Vj, ui7
        ///// </summary>
        //public static Vector128<long> ShiftRightArithmeticRoundedNarrowingLower(Vector128<longlong> left, Vector128<longlong> right, [ConstantExpected(Min = 0, Max = (byte)(127))] const byte shift) => ShiftRightArithmeticRoundedNarrowingLower(left, right, shift);

        /// <summary>
        /// int8x8_t vsrarn_b_h(int16x8_t value, int16x8_t shift)
        ///   LSX: VSRARN.B.H Vd.8B, Vj.8H, Vk.8H
        /// </summary>
        public static Vector64<sbyte> ShiftRightArithmeticRoundedNarrowingLower(Vector128<short> value, Vector128<short> shift) => ShiftRightArithmeticRoundedNarrowingLower(value, shift);

        /// <summary>
        /// int16x4_t vsrarn_h_w(int32x4_t value, int32x4_t shift)
        ///   LSX: VSRARN.H.W Vd.4H, Vj.4W, Vk.4W
        /// </summary>
        public static Vector64<short> ShiftRightArithmeticRoundedNarrowingLower(Vector128<int> value, Vector128<int> shift) => ShiftRightArithmeticRoundedNarrowingLower(value, shift);

        /// <summary>
        /// uint16x4_t vsrarn_h_w(uint32x4_t value, uint32x4_t shift)
        ///   LSX: VSRARN.H.W Vd.4H, Vj.4W, Vk.4W
        /// </summary>
        public static Vector64<ushort> ShiftRightArithmeticRoundedNarrowingLower(Vector128<uint> value, Vector128<uint> shift) => ShiftRightArithmeticRoundedNarrowingLower(value, shift);

        /// <summary>
        /// int32x2_t vsrarn_w_d(int64x2_t value, int64x2_t shift)
        ///   LSX: VSRARN.W.D Vd.2W, Vj.2D, Vk.2D
        /// </summary>
        public static Vector64<int> ShiftRightArithmeticRoundedNarrowingLower(Vector128<long> value, Vector128<long> shift) => ShiftRightArithmeticRoundedNarrowingLower(value, shift);

        /// <summary>
        /// uint8x16_t vrotri_b(uint8x16_t a, const int n)
        ///   LSX: VROTRI.B Vd.16B, Vj.16B, ui3
        /// </summary>
        public static Vector128<sbyte> RotateRight(Vector128<sbyte> value, const byte shift) => RotateRight(value, shift);

        /// <summary>
        /// uint8x16_t vrotri_b(uint8x16_t a, const int n)
        ///   LSX: VROTRI.B Vd.16B, Vj.16B, ui3
        /// </summary>
        public static Vector128<byte> RotateRight(Vector128<byte> value, const byte shift) => RotateRight(value, shift);

        /// <summary>
        /// uint16x8_t vrotri_h(uint16x8_t a, const int n)
        ///   LSX: VROTRI.H Vd.8H, Vj.8H, ui4
        /// </summary>
        public static Vector128<short> RotateRight(Vector128<short> value, const byte shift) => RotateRight(value, shift);

        /// <summary>
        /// uint16x8_t vrotri_h(uint16x8_t a, const int n)
        ///   LSX: VROTRI.H Vd.8H, Vj.8H, ui4
        /// </summary>
        public static Vector128<ushort> RotateRight(Vector128<ushort> value, const byte shift) => RotateRight(value, shift);

        /// <summary>
        /// uint32x4_t vrotri_w(uint32x4_t a, const int n)
        ///   LSX: VROTRI.W Vd.4W, Vj.4W, ui5
        /// </summary>
        public static Vector128<int> RotateRight(Vector128<int> value, const byte shift) => RotateRight(value, shift);

        /// <summary>
        /// uint32x4_t vrotri_w(uint32x4_t a, const int n)
        ///   LSX: VROTRI.W Vd.4W, Vj.4W, ui5
        /// </summary>
        public static Vector128<uint> RotateRight(Vector128<uint> value, const byte shift) => RotateRight(value, shift);

        /// <summary>
        /// uint64x2_t vrotri_d(uint64x2_t a, const int n)
        ///   LSX: VROTRI.D Vd.2D, Vj.2D, ui6
        /// </summary>
        public static Vector128<long> RotateRight(Vector128<long> value, const byte shift) => RotateRight(value, shift);

        /// <summary>
        /// uint64x2_t vrotri_d(uint64x2_t a, const int n)
        ///   LSX: VROTRI.D Vd.2D, Vj.2D, ui6
        /// </summary>
        public static Vector128<ulong> RotateRight(Vector128<ulong> value, const byte shift) => RotateRight(value, shift);

        /// <summary>
        /// int8x16_t vrotr_b(int8x16_t a, int8x16_t b)
        ///   LSX: VROTR.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> RotateRight(Vector128<sbyte> value, Vector128<sbyte> shift) => RotateRight(value, shift);

        /// <summary>
        /// uint8x16_t vrotr_b(uint8x16_t a, uint8x16_t b)
        ///   LSX: VROTR.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> RotateRight(Vector128<byte> value, Vector128<byte> shift) => RotateRight(value, shift);

        /// <summary>
        /// int16x8_t vrotr_h(int16x8_t value, int16x8_t shift)
        ///   LSX: VROTR.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> RotateRight(Vector128<short> value, Vector128<short> shift) => RotateRight(value, shift);

        /// <summary>
        /// uint16x8_t vrotr_h(uint16x8_t value, uint16x8_t shift)
        ///   LSX: VROTR.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> RotateRight(Vector128<ushort> value, Vector128<ushort> shift) => RotateRight(value, shift);

        /// <summary>
        /// int32x4_t vrotr_w(int32x4_t value, int32x4_t shift)
        ///   LSX: VROTR.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<int> RotateRight(Vector128<int> value, Vector128<int> shift) => RotateRight(value, shift);

        /// <summary>
        /// uint32x4_t vrotr_w(uint32x4_t value, uint32x4_t shift)
        ///   LSX: VROTR.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<uint> RotateRight(Vector128<uint> value, Vector128<uint> shift) => RotateRight(value, shift);

        /// <summary>
        /// int64x2_t vrotr_d(int64x2_t value, int64x2_t shift)
        ///   LSX: VROTR.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> RotateRight(Vector128<long> value, Vector128<long> shift) => RotateRight(value, shift);

        /// <summary>
        /// uint64x2_t vrotr_d(uint64x2_t value, uint64x2_t shift)
        ///   LSX: VROTR.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> RotateRight(Vector128<ulong> value, Vector128<ulong> shift) => RotateRight(value, shift);

        /// <summary>
        /// int8x16_t vsigncov_b(int8x16_t a)
        ///   LSX: VSIGNCOV.B Vd.16B, Vj.16B, Vj.16B
        /// </summary>
        public static Vector128<byte> Abs(Vector128<sbyte> value) => Abs(value);

        /// <summary>
        /// int16x8_t vsigncov_h(int16x8_t a)
        ///   LSX: VSIGNCOV.H Vd.8H, Vj.8H, Vj.8H
        /// </summary>
        public static Vector128<ushort> Abs(Vector128<short> value) => Abs(value);

        /// <summary>
        /// int32x4_t vsigncov_w(int32x4_t a)
        ///   LSX: VSIGNCOV.W Vd.4W, Vj.4W, Vj.4W
        /// </summary>
        public static Vector128<uint> Abs(Vector128<int> value) => Abs(value);

        /// <summary>
        /// int64x2_t vsigncov_d(int64x2_t a)
        ///   LSX: VSIGNCOV.D Vd.2D, Vj.2D, Vj.2D
        /// </summary>
        public static Vector128<ulong> Abs(Vector128<long> value) => Abs(value);

        /// <summary>
        /// float32x4_t vbitclri_w(float32x4_t a)
        ///   LSX: VBITCLRI.W Vd.4S, Vj.4S, 31
        /// </summary>
        public static Vector128<float> Abs(Vector128<float> value) => Abs(value);

        /// <summary>
        /// float64x2_t vbitclri_d(float64x2_t a)
        ///   LSX: VBITCLRI.D Vd.2D, Vj.2D, 63
        /// </summary>
        public static Vector128<double> Abs(Vector128<double> value) => Abs(value);

        /// <summary>
        /// float32x4_t vfrintrm_s(float32x4_t a)
        ///   LSX: VFRINTRM.S Vd.4S, Vj.4S
        /// </summary>
        public static Vector128<float> Floor(Vector128<float> value) => Floor(value);

        /// <summary>
        /// float64x2_t vfrintrm_d(float64x2_t a)
        ///   LSX: VFRINTRM.D Vd.2D, Vj.2D
        /// </summary>
        public static Vector128<double> Floor(Vector128<double> value) => Floor(value);

        /// <summary>
        /// float32x4_t vfrintrp_s(float32x4_t a)
        ///   LSX: VFRINTRP.S Vd.4S, Vj.4S
        /// </summary>
        public static Vector128<float> Ceiling(Vector128<float> value) => Ceiling(value);

        /// <summary>
        /// float64x2_t vfrintrp_d(float64x2_t a)
        ///   LSX: VFRINTRP.D Vd.2D, Vj.2D
        /// </summary>
        public static Vector128<double> Ceiling(Vector128<double> value) => Ceiling(value);

        /// <summary>
        /// float32x4_t vfrintrz_s(float32x4_t a)
        ///   LSX: VFRINTRZ.S Vd.4S, Vj.4S
        /// </summary>
        public static Vector128<float> RoundToZero(Vector128<float> value) => RoundToZero(value);

        /// <summary>
        /// float64x2_t vfrintrz_d(float64x2_t a)
        ///   LSX: VFRINTRZ.D Vd.2D, Vj.2D
        /// </summary>
        public static Vector128<double> RoundToZero(Vector128<double> value) => RoundToZero(value);

        /// <summary>
        /// float32x4_t vfrintrm_s(float32x4_t a)
        ///   LSX: VFRINTRM.S Vd.4S, Vj.4S
        /// </summary>
        public static Vector128<float> RoundToNegativeInfinity(Vector128<float> value) => RoundToNegativeInfinity(value);

        /// <summary>
        /// float64x2_t vfrintrm_d(float64x2_t a)
        ///   LSX: VFRINTRM.D Vd.2D, Vj.2D
        /// </summary>
        public static Vector128<double> RoundToNegativeInfinity(Vector128<double> value) => RoundToNegativeInfinity(value);

        /// <summary>
        /// float32x4_t vfrintrp_s(float32x4_t a)
        ///   LSX: VFRINTRP.S Vd.4S, Vj.4S
        /// </summary>
        public static Vector128<float> RoundToPositiveInfinity(Vector128<float> value) => RoundToPositiveInfinity(value);

        /// <summary>
        /// float64x2_t vfrintrp_d(float64x2_t a)
        ///   LSX: VFRINTRP.D Vd.2D, Vj.2D
        /// </summary>
        public static Vector128<double> RoundToPositiveInfinity(Vector128<double> value) => RoundToPositiveInfinity(value);

        /// <summary>
        /// int8x16_t vinsgr2vr_b(int8x16_t v, int8_t data, const int index)
        ///   LSX: VINSGR2VR.B Vd.B, Rj, ui4
        /// </summary>
        public static Vector128<sbyte> Insert(Vector128<sbyte> vector, sbyte data, const byte index) => Insert(vector, data, index);

        /// <summary>
        /// uint8x16_t vinsgr2vr_b(uint8x16_t v, uint8_t data, const int index)
        ///   LSX: VINSGR2VR.B Vd.B, Rj, ui4
        /// </summary>
        public static Vector128<byte> Insert(Vector128<byte> vector, byte data, const byte index) => Insert(vector, data, index);

        /// <summary>
        /// int16x8_t vinsgr2vr_h(int16x8_t v, int16_t data, const int index)
        ///   LSX: VINSGR2VR.H Vd.H, Rj, ui3
        /// </summary>
        public static Vector128<short> Insert(Vector128<short> vector, short data, const byte index) => Insert(vector, data, index);

        /// <summary>
        /// uint16x8_t vinsgr2vr_h(uint16x8_t v, uint16_t data, const int index)
        ///   LSX: VINSGR2VR.H Vd.H, Rj, ui3
        /// </summary>
        public static Vector128<ushort> Insert(Vector128<ushort> vector, ushort data, const byte index) => Insert(vector, data, index);

        /// <summary>
        /// int32x4_t vinsgr2vr_w(int32x4_t v, int32_t data, const int index)
        ///   LSX: VINSGR2VR.W Vd.S, Rj, ui2
        /// </summary>
        public static Vector128<int> Insert(Vector128<int> vector, int data, const byte index) => Insert(vector, data, index);

        /// <summary>
        /// uint32x4_t vinsgr2vr_w(uint32x4_t v, uint32_t data, const int index)
        ///   LSX: VINSGR2VR.W Vd.S, Rj, ui2
        /// </summary>
        public static Vector128<uint> Insert(Vector128<uint> vector, uint data, const byte index) => Insert(vector, data, index);

        /// <summary>
        /// int64x2_t vinsgr2vr_d(int64x2_t v, int64_t data, const int index)
        ///   LSX: VINSGR2VR.D Vd.D, Rj, ui1
        /// </summary>
        public static Vector128<long> Insert(Vector128<long> vector, long data, const byte index) => Insert(vector, data, index);

        /// <summary>
        /// uint64x2_t vinsgr2vr_d(uint64x2_t v, uint64_t data, const int index)
        ///   LSX: VINSGR2VR.D Vd.D, Rj, ui1
        /// </summary>
        public static Vector128<ulong> Insert(Vector128<ulong> vector, ulong data, const byte index) => Insert(vector, data, index);

        /// <summary>
        /// float32x4_t xvinsve0_w(float32x4_t v, float32_t data, const int index)
        ///   LSX: VEXTRINS.W  Vd.S, Vj.S, ui8
        /// </summary>
        public static Vector128<float> Insert(Vector128<float> vector, float data, const byte index) => Insert(vector, data, index);

        /// <summary>
        /// float64x2_t xvinsve0_d(float64x2_t v, float64_t data, const int index)
        ///   LSX: VEXTRINS.D  Vd.D, Vj.D, ui8
        /// </summary>
        public static Vector128<double> Insert(Vector128<double> vector, double data, const byte index) => Insert(vector, data, index);

        /// <summary>
        /// int8x16_t vreplgr2vr_b(int8_t value)
        ///   LSX: VREPLGR2VR.B Vd.16B, Rj
        /// </summary>
        public static Vector128<sbyte> DuplicateToVector128(sbyte value) => DuplicateToVector128(value);

        /// <summary>
        /// uint8x16_t vreplgr2vr_b(uint8_t value)
        ///   LSX: VREPLGR2VR.B Vd.16B, Rj
        /// </summary>
        public static Vector128<byte> DuplicateToVector128(byte value) => DuplicateToVector128(value);

        /// <summary>
        /// int16x8_t vreplgr2vr_h(int16_t value)
        ///   LSX: VREPLGR2VR.H Vd.8H, Rj
        /// </summary>
        public static Vector128<short> DuplicateToVector128(short value) => DuplicateToVector128(value);

        /// <summary>
        /// uint16x8_t vreplgr2vr_h(uint16_t value)
        ///   LSX: VREPLGR2VR.H Vd.8H, Rj
        /// </summary>
        public static Vector128<ushort> DuplicateToVector128(ushort value) => DuplicateToVector128(value);

        /// <summary>
        /// int32x4_t vreplgr2vr_w(int32_t value)
        ///   LSX: VREPLGR2VR.W Vd.4W, Rj
        /// </summary>
        public static Vector128<int> DuplicateToVector128(int value) => DuplicateToVector128(value);

        /// <summary>
        /// uint32x4_t vreplgr2vr_w(uint32_t value)
        ///   LSX: VREPLGR2VR.W Vd.4W, Rj
        /// </summary>
        public static Vector128<uint> DuplicateToVector128(uint value) => DuplicateToVector128(value);

        /// <summary>
        /// int64x2_t vreplgr2vr_d(int64_t value)
        ///   LSX: VREPLGR2VR.D Vd.2D, Rj
        /// </summary>
        public static Vector128<long> DuplicateToVector128(long value) => DuplicateToVector128(value);

        /// <summary>
        /// uint64x2_t vreplgr2vr_d(uint64_t value)
        ///   LSX: VREPLGR2VR.D Vd.2D, Rj
        /// </summary>
        public static Vector128<ulong> DuplicateToVector128(ulong value) => DuplicateToVector128(value);

        /// <summary>
        /// float32x4_t xvreplve0_w(float32_t value)
        ///   LSX: XVREPLVE0.W Vd.4S, Vj.S[0]
        /// </summary>
        public static Vector128<float> DuplicateToVector128(float value) => DuplicateToVector128(value);

        /// <summary>
        /// float64x2_t xvreplve0_d(float64_t value)
        ///   LSX: XVREPLVE0.D Vd.2D, Vj.D[0]
        /// </summary>
        public static Vector128<double> DuplicateToVector128(double value) => DuplicateToVector128(value);

        /// <summary>
        /// float32x4_t vffint_s_w(int32x4_t a)
        ///   LSX: VFFINT.S.W Vd.4S, Vj.4W
        /// </summary>
        public static Vector128<float> ConvertToSingle(Vector128<int> value) => ConvertToSingle(value);

        /// <summary>
        /// float32x4_t vffint_s_wu(uint32x4_t a)
        ///   LSX: VFFINT.S.WU Vd.4S, Vj.4W
        /// </summary>
        public static Vector128<float> ConvertToSingle(Vector128<uint> value) => ConvertToSingle(value);

        /// <summary>
        /// float64x2_t vffint_d_l(int64x2_t a)
        ///   LSX: VFFINT.D.L Vd.2D, Vj.2D
        /// </summary>
        public static Vector128<double> ConvertToDouble(Vector128<long> value) => ConvertToDouble(value);

        /// <summary>
        /// float64x2_t vffint_d_lu(uint64x2_t a)
        ///   LSX: VFFINT.D.LU Vd.2D, Vj.2D
        /// </summary>
        public static Vector128<double> ConvertToDouble(Vector128<ulong> value) => ConvertToDouble(value);

        /// <summary>
        /// int8_t vfsrtpi_b(uint8x16_t value)
        ///   LSX: VFSRTPI.B Vd.16B, Vj.16B, 0
        /// </summary>
        public static byte FirstNegativeInteger(Vector128<byte> value) => FirstNegativeInteger(value);

        /// <summary>
        /// int16_t vfsrtpi_h(uint16x8_t value)
        ///   LSX: VFSRTPI.H Vd.8H, Vj.8H, 0
        /// </summary>
        public static ushort FirstNegativeInteger(Vector128<ushort> value) => FirstNegativeInteger(value);

        /// <summary>
        /// bool vsetnez_v(uint8x16_t value)
        ///   LSX: VSETNEZ.V cd, Vj.16B
        /// </summary>
        public static bool HasElementsNotZero(Vector128<byte> value) => HasElementsNotZero(value);

        /// <summary>
        /// bool vseteqz_v(uint8x16_t value)
        ///   LSX: VSETEQZ.V cd, Vj.16B
        /// </summary>
        public static bool AllElementsIsZero(Vector128<byte> value) => AllElementsIsZero(value);

        /// <summary>
        /// bool vsetallnez_b(int8x16_t value)
        ///   LSX: VSETALLNEZ.B cd, Vj.16B
        /// </summary>
        public static bool AllElementsNotZero(Vector128<sbyte> value) => AllElementsNotZero(value);

        /// <summary>
        /// bool vsetallnez_b(uint8x16_t value)
        ///   LSX: VSETALLNEZ.B cd, Vj.16B
        /// </summary>
        public static bool AllElementsNotZero(Vector128<byte> value) => AllElementsNotZero(value);

        /// <summary>
        /// bool vsetallnez_h(int16x8_t value)
        ///   LSX: VSETALLNEZ.H cd, Vj.8H
        /// </summary>
        public static bool AllElementsNotZero(Vector128<short> value) => AllElementsNotZero(value);

        /// <summary>
        /// bool vsetallnez_h(uint16x8_t value)
        ///   LSX: VSETALLNEZ.H cd, Vj.8H
        /// </summary>
        public static bool AllElementsNotZero(Vector128<ushort> value) => AllElementsNotZero(value);

        /// <summary>
        /// bool vsetallnez_w(int32x4_t value)
        ///   LSX: VSETALLNEZ.W cd, Vj.4W
        /// </summary>
        public static bool AllElementsNotZero(Vector128<int> value) => AllElementsNotZero(value);

        /// <summary>
        /// bool vsetallnez_w(uint32x4_t value)
        ///   LSX: VSETALLNEZ.W cd, Vj.4W
        /// </summary>
        public static bool AllElementsNotZero(Vector128<uint> value) => AllElementsNotZero(value);

        /// <summary>
        /// bool vsetallnez_w(int64x2_t value)
        ///   LSX: VSETALLNEZ.D cd, Vj.2D
        /// </summary>
        public static bool AllElementsNotZero(Vector128<long> value) => AllElementsNotZero(value);

        /// <summary>
        /// bool vsetallnez_w(uint64x2_t value)
        ///   LSX: VSETALLNEZ.D cd, Vj.2D
        /// </summary>
        public static bool AllElementsNotZero(Vector128<ulong> value) => AllElementsNotZero(value);

        /// <summary>
        /// bool vsetanyeqz_b(int8x16_t value)
        ///   LSX: VSETANYEQZ.B cd, Vj.16B
        /// </summary>
        public static bool HasElementsIsZero(Vector128<sbyte> value) => HasElementsIsZero(value);

        /// <summary>
        /// bool vsetanyeqz_b(uint8x16_t value)
        ///   LSX: VSETANYEQZ.B cd, Vj.16B
        /// </summary>
        public static bool HasElementsIsZero(Vector128<byte> value) => HasElementsIsZero(value);

        /// <summary>
        /// bool vsetanyeqz_h(int16x8_t value)
        ///   LSX: VSETANYEQZ.H cd, Vj.8H
        /// </summary>
        public static bool HasElementsIsZero(Vector128<short> value) => HasElementsIsZero(value);

        /// <summary>
        /// bool vsetanyeqz_h(uint16x8_t value)
        ///   LSX: VSETANYEQZ.H cd, Vj.8H
        /// </summary>
        public static bool HasElementsIsZero(Vector128<ushort> value) => HasElementsIsZero(value);

        /// <summary>
        /// bool vsetanyeqz_w(int32x4_t value)
        ///   LSX: VSETANYEQZ.W cd, Vj.4W
        /// </summary>
        public static bool HasElementsIsZero(Vector128<int> value) => HasElementsIsZero(value);

        /// <summary>
        /// bool vsetanyeqz_w(uint32x4_t value)
        ///   LSX: VSETANYEQZ.W cd, Vj.4W
        /// </summary>
        public static bool HasElementsIsZero(Vector128<uint> value) => HasElementsIsZero(value);

        /// <summary>
        /// bool vsetanyeqz_w(int64x2_t value)
        ///   LSX: VSETANYEQZ.D cd, Vj.2D
        /// </summary>
        public static bool HasElementsIsZero(Vector128<long> value) => HasElementsIsZero(value);

        /// <summary>
        /// bool vsetanyeqz_w(uint64x2_t value)
        ///   LSX: VSETANYEQZ.D cd, Vj.2D
        /// </summary>
        public static bool HasElementsIsZero(Vector128<ulong> value) => HasElementsIsZero(value);

        /// <summary>
        /// uint8x16_t vsrlni_b_h(uint16x8_t left, uint16x8_t right, shift)
        ///   LSX: VSRLNI.B.H Vd, Vj, ui4
        /// </summary>
        public static Vector128<byte> ShiftRightLogicalNarrowingLower(Vector128<ushort> left, Vector128<ushort> right, byte shift) => ShiftRightLogicalNarrowingLower(left, right, shift);

        /// <summary>
        /// int16x8_t vsrlni_h_w(int32x4_t left, int32x4_t right, shift)
        ///   LSX: VSRLNI.H.W Vd, Vj, ui5
        /// </summary>
        public static Vector128<short> ShiftRightLogicalNarrowingLower(Vector128<int> left, Vector128<int> right, byte shift) => ShiftRightLogicalNarrowingLower(left, right, shift);

        /// <summary>
        /// uint16x8_t vsrlni_h_w(uint32x4_t left, uint32x4_t right, shift)
        ///   LSX: VSRLNI.H.W Vd, Vj, ui5
        /// </summary>
        public static Vector128<ushort> ShiftRightLogicalNarrowingLower(Vector128<uint> left, Vector128<uint> right, byte shift) => ShiftRightLogicalNarrowingLower(left, right, shift);

        /// <summary>
        /// int32x4_t vsrlni_w_d(int64x2_t left, int64x2_t right, shift)
        ///   LSX: VSRLNI.W.D Vd, Vj, ui6
        /// </summary>
        public static Vector128<int> ShiftRightLogicalNarrowingLower(Vector128<long> left, Vector128<long> right, byte shift) => ShiftRightLogicalNarrowingLower(left, right, shift);

        /// <summary>
        /// uint32x4_t vsrlni_w_d(uint64x2_t left, uint64x2_t right, shift)
        ///   LSX: VSRLNI.W.D Vd, Vj, ui6
        /// </summary>
        public static Vector128<uint> ShiftRightLogicalNarrowingLower(Vector128<ulong> left, Vector128<ulong> right, byte shift) => ShiftRightLogicalNarrowingLower(left, right, shift);

        ///// <summary>
        ///// uint64x2_t vsrlni_d_q(uint128x1_t left, uint128x1_t right, shift)
        /////   LSX: VSRLNI.D.Q Vd.Q, Vj.Q, ui7
        ///// </summary>
        //public static Vector128<ulong> ShiftRightLogicalNarrowingLower(Vector128<ulonglong> left, Vector128<ulonglong> right, byte shift) => ShiftRightLogicalNarrowingLower(left, right, shift);

        /// <summary>
        /// int8x8_t vsrln_b_h(int16x8_t value, int16x8_t shift)
        ///   LSX: VSRLN.B.H Vd.8B, Vj.8H, Vk.8H
        /// </summary>
        public static Vector64<sbyte> ShiftRightLogicalNarrowingLower(Vector128<short> value, Vector128<short> shift) => ShiftRightLogicalNarrowingLower(value, shift);

        /// <summary>
        /// uint8x8_t vsrln_b_h(uint16x8_t value, uint16x8_t shift)
        ///   LSX: VSRLN.B.H Vd.8B, Vj.8H, Vk.8H
        /// </summary>
        public static Vector64<byte> ShiftRightLogicalNarrowingLower(Vector128<ushort> value, Vector128<ushort> shift) => ShiftRightLogicalNarrowingLower(value, shift);

        /// <summary>
        /// int16x4_t vsrln_h_w(int32x4_t value, int32x4_t shift)
        ///   LSX: VSRLN.H.W Vd.4H, Vj.4W, Vk.4W
        /// </summary>
        public static Vector64<short> ShiftRightLogicalNarrowingLower(Vector128<int> value, Vector128<int> shift) => ShiftRightLogicalNarrowingLower(value, shift);

        /// <summary>
        /// uint16x4_t vsrln_h_w(uint32x4_t value, uint32x4_t shift)
        ///   LSX: VSRLN.H.W Vd.4H, Vj.4W, Vk.4W
        /// </summary>
        public static Vector64<ushort> ShiftRightLogicalNarrowingLower(Vector128<uint> value, Vector128<uint> shift) => ShiftRightLogicalNarrowingLower(value, shift);

        /// <summary>
        /// int32x2_t vsrln_w_d(int64x2_t value, int64x2_t shift)
        ///   LSX: VSRLN.W.D Vd.2W, Vj.2D, Vk.2D
        /// </summary>
        public static Vector64<int> ShiftRightLogicalNarrowingLower(Vector128<long> value, Vector128<long> shift) => ShiftRightLogicalNarrowingLower(value, shift);

        /// <summary>
        /// uint32x2_t vsrln_w_d(uint64x2_t value, uint64x2_t shift)
        ///   LSX: VSRLN.W.D Vd.2W, Vj.2D, Vk.2D
        /// </summary>
        public static Vector64<uint> ShiftRightLogicalNarrowingLower(Vector128<ulong> value, Vector128<ulong> shift) => ShiftRightLogicalNarrowingLower(value, shift);

        /// <summary>
        /// int16x8_t vssrlni_b_h(int16x8_t left, int16x8_t right, const byte n)
        ///   LSX: VSSRLNI.B.H Vd.16B, Vj.8H, ui4  ///NOTE: the Vd is both input and output.
        /// </summary>
        public static Vector128<sbyte> ShiftRightLogicalNarrowingSaturateLower(Vector128<short> left, Vector128<short> right, [ConstantExpected(Min = 0, Max = (byte)(15))] byte shift) => ShiftRightLogicalNarrowingSaturateLower(left, right, shift);

        /// <summary>
        /// uint16x8_t vssrlni_b_h(uint16x8_t left, uint16x8_t right, const byte n)
        ///   LSX: VSSRLNI.B.H Vd.16B, Vj.8H, ui4
        /// </summary>
        public static Vector128<byte> ShiftRightLogicalNarrowingSaturateLower(Vector128<ushort> left, Vector128<ushort> right, [ConstantExpected(Min = 0, Max = (byte)(15))] byte shift) => ShiftRightLogicalNarrowingSaturateLower(left, right, shift);

        /// <summary>
        /// int16x8_t vssrlni_h_w(int32x4_t left, int32x4_t right, const byte n)
        ///   LSX: VSSRLNI.H.W Vd.8H, Vj.4W, ui5
        /// </summary>
        public static Vector128<short> ShiftRightLogicalNarrowingSaturateLower(Vector128<int> left, Vector128<int> right, [ConstantExpected(Min = 0, Max = (byte)(31))] byte shift) => ShiftRightLogicalNarrowingSaturateLower(left, right, shift);

        /// <summary>
        /// uint16x8_t vssrlni_h_w(uint32x4_t left, uint32x4_t right, const byte n)
        ///   LSX: VSSRLNI.H.W Vd.8H, Vj.4W, ui5
        /// </summary>
        public static Vector128<ushort> ShiftRightLogicalNarrowingSaturateLower(Vector128<uint> left, Vector128<uint> right, [ConstantExpected(Min = 0, Max = (byte)(31))] byte shift) => ShiftRightLogicalNarrowingSaturateLower(left, right, shift);

        /// <summary>
        /// int32x4_t vssrlni_w_d(int64x2_t left, int64x2_t right, const byte n)
        ///   LSX: VSSRLNI.W.D Vd.4W, Vj.2D, ui6
        /// </summary>
        public static Vector128<int> ShiftRightLogicalNarrowingSaturateLower(Vector128<long> left, Vector128<long> right, [ConstantExpected(Min = 0, Max = (byte)(63))] byte shift) => ShiftRightLogicalNarrowingSaturateLower(left, right, shift);

        /// <summary>
        /// uint32x4_t vssrlni_w_d(uint64x2_t left, uint64x2_t right, const byte n)
        ///   LSX: VSSRLNI.W.D Vd.4W, Vj.2D, ui6
        /// </summary>
        public static Vector128<uint> ShiftRightLogicalNarrowingSaturateLower(Vector128<ulong> left, Vector128<ulong> right, [ConstantExpected(Min = 0, Max = (byte)(63))] byte shift) => ShiftRightLogicalNarrowingSaturateLower(left, right, shift);

        ///// <summary>
        ///// int64x2_t vssrlni_d_q(int128x1_t left, int128x1_t right, const byte n)
        /////   LSX: VSSRLNI.D.Q Vd.2D, Vj.Q, ui7
        ///// </summary>
        //public static Vector128<long> ShiftRightLogicalNarrowingSaturateLower(Vector128<longlong> left, Vector128<longlong> right, [ConstantExpected(Min = 0, Max = (byte)(127))] byte shift) => ShiftRightLogicalNarrowingSaturateLower(left, right, shift);

        /// <summary>
        /// int8x8_t vssrln_b_h(int16x8_t value, int16x8_t shift)
        ///   LSX: VSSRLN.B.H Vd.8B, Vj.8H, Vk.8H
        /// </summary>
        public static Vector64<sbyte> ShiftRightLogicalNarrowingSaturateLower(Vector128<short> value, Vector128<short> shift) => ShiftRightLogicalNarrowingSaturateLower(value, shift);

        /// <summary>
        /// uint8x8_t vssrln_b_h(uint16x8_t value, uint16x8_t shift)
        ///   LSX: VSSRLN.B.H Vd.8B, Vj.8H, Vk.8H
        /// </summary>
        public static Vector64<byte> ShiftRightLogicalNarrowingSaturateLower(Vector128<ushort> value, Vector128<ushort> shift) => ShiftRightLogicalNarrowingSaturateLower(value, shift);

        /// <summary>
        /// int16x4_t vssrln_h_w(int32x4_t value, int32x4_t shift)
        ///   LSX: VSSRLN.H.W Vd.4H, Vj.4W, Vk.4W
        /// </summary>
        public static Vector64<short> ShiftRightLogicalNarrowingSaturateLower(Vector128<int> value, Vector128<int> shift) => ShiftRightLogicalNarrowingSaturateLower(value, shift);

        /// <summary>
        /// uint16x4_t vssrln_h_w(uint32x4_t value, uint32x4_t shift)
        ///   LSX: VSSRLN.H.W Vd.4H, Vj.4W, Vk.4W
        /// </summary>
        public static Vector64<ushort> ShiftRightLogicalNarrowingSaturateLower(Vector128<uint> value, Vector128<uint> shift) => ShiftRightLogicalNarrowingSaturateLower(value, shift);

        /// <summary>
        /// int32x2_t vssrln_w_d(int64x2_t value, int64x2_t shift)
        ///   LSX: VSSRLN.W.D Vd.2W, Vj.2D, Vk.2D
        /// </summary>
        public static Vector64<int> ShiftRightLogicalNarrowingSaturateLower(Vector128<long> value, Vector128<long> shift) => ShiftRightLogicalNarrowingSaturateLower(value, shift);

        /// <summary>
        /// uint32x2_t vssrln_w_d(uint64x2_t value, uint64x2_t shift)
        ///   LSX: VSSRLN.W.D Vd.2W, Vj.2D, Vk.2D
        /// </summary>
        public static Vector64<uint> ShiftRightLogicalNarrowingSaturateLower(Vector128<ulong> value, Vector128<ulong> shift) => ShiftRightLogicalNarrowingSaturateLower(value, shift);

        /// <summary>
        /// uint16x8_t vssrlni_bu_h(uint16x8_t left, uint16x8_t right, const byte n)
        ///   LSX: VSSRLNI.BU.H Vd.16B, Vj.8H, ui4
        /// </summary>
        public static Vector128<byte> ShiftRightLogicalNarrowingSaturateUnsignedLower(Vector128<ushort> left, Vector128<ushort> right, [ConstantExpected(Min = 0, Max = (byte)(15))] byte shift) => ShiftRightLogicalNarrowingSaturateUnsignedLower(left, right, shift);

        /// <summary>
        /// uint16x8_t vssrlni_hu_w(uint32x4_t left, uint32x4_t right, const byte n)
        ///   LSX: VSSRLNI.HU.W Vd.8H, Vj.4W, ui5
        /// </summary>
        public static Vector128<ushort> ShiftRightLogicalNarrowingSaturateUnsignedLower(Vector128<uint> left, Vector128<uint> right, [ConstantExpected(Min = 0, Max = (byte)(31))] byte shift) => ShiftRightLogicalNarrowingSaturateUnsignedLower(left, right, shift);

        /// <summary>
        /// uint32x4_t vssrlni_wu_d(uint64x2_t left, uint64x2_t right, const byte n)
        ///   LSX: VSSRLNI.WU.D Vd.4W, Vj.2D, ui6
        /// </summary>
        public static Vector128<uint> ShiftRightLogicalNarrowingSaturateUnsignedLower(Vector128<ulong> left, Vector128<ulong> right, [ConstantExpected(Min = 0, Max = (byte)(63))] byte shift) => ShiftRightLogicalNarrowingSaturateUnsignedLower(left, right, shift);

        ///// <summary>
        ///// uint64x2_t vssrlni_du_q(uint128x1_t left, uint128x1_t right, const byte n)
        /////   LSX: VSSRLNI.DU.Q Vd.2D, Vj.Q, ui7
        ///// </summary>
        //public static Vector128<ulong> ShiftRightLogicalNarrowingSaturateUnsignedLower(Vector128<ulonglong> left, Vector128<ulonglong> right, [ConstantExpected(Min = 0, Max = (byte)(127))] byte shift) => ShiftRightLogicalNarrowingSaturateUnsignedLower(left, right, shift);

        /// <summary>
        /// uint8x8_t vssrln_bu_h(uint16x8_t value, uint16x8_t shift)
        ///   LSX: VSSRLN.BU.H Vd.8B, Vj.8H, Vk.8H
        /// </summary>
        public static Vector64<byte> ShiftRightLogicalNarrowingSaturateUnsignedLower(Vector128<ushort> value, Vector128<ushort> shift) => ShiftRightLogicalNarrowingSaturateUnsignedLower(value, shift);

        /// <summary>
        /// uint16x4_t vssrln_hu_w(uint32x4_t value, uint32x4_t shift)
        ///   LSX: VSSRLN.HU.W Vd.4H, Vj.4W, Vk.4W
        /// </summary>
        public static Vector64<ushort> ShiftRightLogicalNarrowingSaturateUnsignedLower(Vector128<uint> value, Vector128<uint> shift) => ShiftRightLogicalNarrowingSaturateUnsignedLower(value, shift);

        /// <summary>
        /// uint32x2_t vssrln_wu_d(uint64x2_t value, uint64x2_t shift)
        ///   LSX: VSSRLN.WU.D Vd.2W, Vj.2D, Vk.2D
        /// </summary>
        public static Vector64<uint> ShiftRightLogicalNarrowingSaturateUnsignedLower(Vector128<ulong> value, Vector128<ulong> shift) => ShiftRightLogicalNarrowingSaturateUnsignedLower(value, shift);

        /// <summary>
        /// int16x8_t vssran_b_h(int16x8_t left, int16x8_t right, const byte n)
        ///   LSX: VSSRANI.B.H Vd.16B, Vj.8H, ui4  ///NOTE: the Vd is both input and output.
        /// </summary>
        public static Vector128<sbyte> ShiftRightArithmeticNarrowingSaturateLower(Vector128<short> left, Vector128<short> right, [ConstantExpected(Min = 0, Max = (byte)(15))] byte shift) => ShiftRightArithmeticNarrowingSaturateLower(left, right, shift);

        /// <summary>
        /// int16x8_t vssran_h_w(int32x4_t left, int32x4_t right, const byte n)
        ///   LSX: VSSRANI.H.W Vd.8H, Vj.4W, ui5
        /// </summary>
        public static Vector128<short> ShiftRightArithmeticNarrowingSaturateLower(Vector128<int> left, Vector128<int> right, [ConstantExpected(Min = 0, Max = (byte)(31))] byte shift) => ShiftRightArithmeticNarrowingSaturateLower(left, right, shift);

        /// <summary>
        /// int32x4_t vssran_w_d(int64x2_t left, int64x2_t right, const byte n)
        ///   LSX: VSSRANI.W.D Vd.4W, Vj.2D, ui6
        /// </summary>
        public static Vector128<int> ShiftRightArithmeticNarrowingSaturateLower(Vector128<long> left, Vector128<long> right, [ConstantExpected(Min = 0, Max = (byte)(63))] byte shift) => ShiftRightArithmeticNarrowingSaturateLower(left, right, shift);

        ///// <summary>
        ///// int64x2_t vssran_d_q(int128x1_t left, int128x1_t right, const byte n)
        /////   LSX: VSSRANI.D.Q Vd.2D, Vj.Q, ui7
        ///// </summary>
        //public static Vector128<long> ShiftRightArithmeticNarrowingSaturateLower(Vector128<longlong> left, Vector128<longlong> right, [ConstantExpected(Min = 0, Max = (byte)(127))] byte shift) => ShiftRightArithmeticNarrowingSaturateLower(left, right, shift);

        /// <summary>
        /// int8x8_t vssran_b_h(int16x8_t value, int16x8_t shift)
        ///   LSX: VSSRAN.B.H Vd.8B, Vj.8H, Vk.8H
        /// </summary>
        public static Vector64<sbyte> ShiftRightArithmeticNarrowingSaturateLower(Vector128<short> value, Vector128<short> shift) => ShiftRightArithmeticNarrowingSaturateLower(value, shift);

        /// <summary>
        /// int16x4_t vssran_h_w(int32x4_t value, int32x4_t shift)
        ///   LSX: VSSRAN.H.W Vd.4H, Vj.4W, Vk.4W
        /// </summary>
        public static Vector64<short> ShiftRightArithmeticNarrowingSaturateLower(Vector128<int> value, Vector128<int> shift) => ShiftRightArithmeticNarrowingSaturateLower(value, shift);

        /// <summary>
        /// int32x2_t vssran_w_d(int64x2_t value, int64x2_t shift)
        ///   LSX: VSSRAN.W.D Vd.2W, Vj.2D, Vk.2D
        /// </summary>
        public static Vector64<int> ShiftRightArithmeticNarrowingSaturateLower(Vector128<long> value, Vector128<long> shift) => ShiftRightArithmeticNarrowingSaturateLower(value, shift);

        /// <summary>
        /// uint16x8_t vssrani_bu_h(int16x8_t left, int16x8_t right, const byte n)
        ///   LSX: VSSRANI.BU.H Vd.16B, Vj.8H, ui4
        /// </summary>
        public static Vector128<byte> ShiftRightArithmeticNarrowingSaturateUnsignedLower(Vector128<short> left, Vector128<short> right, [ConstantExpected(Min = 0, Max = (byte)(15))] byte shift) => ShiftRightArithmeticNarrowingSaturateUnsignedLower(left, right, shift);

        /// <summary>
        /// uint16x8_t vssrani_hu_w(int32x4_t left, int32x4_t right, const byte n)
        ///   LSX: VSSRANI.HU.W Vd.8H, Vj.4W, ui5
        /// </summary>
        public static Vector128<ushort> ShiftRightArithmeticNarrowingSaturateUnsignedLower(Vector128<int> left, Vector128<int> right, [ConstantExpected(Min = 0, Max = (byte)(31))] byte shift) => ShiftRightArithmeticNarrowingSaturateUnsignedLower(left, right, shift);

        /// <summary>
        /// uint32x4_t vssrani_wu_d(int64x2_t left, int64x2_t right, const byte n)
        ///   LSX: VSSRANI.WU.D Vd.4W, Vj.2D, ui6
        /// </summary>
        public static Vector128<uint> ShiftRightArithmeticNarrowingSaturateUnsignedLower(Vector128<long> left, Vector128<long> right, [ConstantExpected(Min = 0, Max = (byte)(63))] byte shift) => ShiftRightArithmeticNarrowingSaturateUnsignedLower(left, right, shift);

        ///// <summary>
        ///// uint64x2_t vssrani_du_q(int128x1_t left, int128x1_t right, const byte n)
        /////   LSX: VSSRANI.DU.Q Vd.2D, Vj.Q, ui7
        ///// </summary>
        //public static Vector128<ulong> ShiftRightArithmeticNarrowingSaturateUnsignedLower(Vector128<longlong> left, Vector128<longlong> right, [ConstantExpected(Min = 0, Max = (byte)(127))] byte shift) => ShiftRightArithmeticNarrowingSaturateUnsignedLower(left, right, shift);

        /// <summary>
        /// uint8x8_t vssran_bu_h(int16x8_t value, int16x8_t shift)
        ///   LSX: VSSRAN.BU.H Vd.8B, Vj.8H, Vk.8H
        /// </summary>
        public static Vector64<byte> ShiftRightArithmeticNarrowingSaturateUnsignedLower(Vector128<short> value, Vector128<short> shift) => ShiftRightArithmeticNarrowingSaturateUnsignedLower(value, shift);

        /// <summary>
        /// uint16x4_t vssran_hu_w(int32x4_t value, int32x4_t shift)
        ///   LSX: VSSRAN.HU.W Vd.4H, Vj.4W, Vk.4W
        /// </summary>
        public static Vector64<ushort> ShiftRightArithmeticNarrowingSaturateUnsignedLower(Vector128<int> value, Vector128<int> shift) => ShiftRightArithmeticNarrowingSaturateUnsignedLower(value, shift);

        /// <summary>
        /// uint32x2_t vssran_wu_d(int64x2_t value, int64x2_t shift)
        ///   LSX: VSSRAN.WU.D Vd.2W, Vj.2D, Vk.2D
        /// </summary>
        public static Vector64<uint> ShiftRightArithmeticNarrowingSaturateUnsignedLower(Vector128<long> value, Vector128<long> shift) => ShiftRightArithmeticNarrowingSaturateUnsignedLower(value, shift);

        /// <summary>
        /// int16x8_t vssrlrni_b_h(int16x8_t left, int16x8_t right, const byte n)
        ///   LSX: VSSRLRNI.B.H Vd.16B, Vj.8H, ui4  ///NOTE: the Vd is both input and output.
        /// </summary>
        public static Vector128<sbyte> ShiftRightLogicalRoundedNarrowingSaturateLower(Vector128<short> left, Vector128<short> right, [ConstantExpected(Min = 0, Max = (byte)(15))] byte shift) => ShiftRightLogicalRoundedNarrowingSaturateLower(left, right, shift);

        /// <summary>
        /// uint16x8_t vssrlrni_b_h(uint16x8_t left, uint16x8_t right, const byte n)
        ///   LSX: VSSRLRNI.B.H Vd.16B, Vj.8H, ui4
        /// </summary>
        public static Vector128<byte> ShiftRightLogicalRoundedNarrowingSaturateLower(Vector128<ushort> left, Vector128<ushort> right, [ConstantExpected(Min = 0, Max = (byte)(15))] byte shift) => ShiftRightLogicalRoundedNarrowingSaturateLower(left, right, shift);

        /// <summary>
        /// int16x8_t vssrlrni_h_w(int32x4_t left, int32x4_t right, const byte n)
        ///   LSX: VSSRLRNI.H.W Vd.8H, Vj.4W, ui5
        /// </summary>
        public static Vector128<short> ShiftRightLogicalRoundedNarrowingSaturateLower(Vector128<int> left, Vector128<int> right, [ConstantExpected(Min = 0, Max = (byte)(31))] byte shift) => ShiftRightLogicalRoundedNarrowingSaturateLower(left, right, shift);

        /// <summary>
        /// uint16x8_t vssrlrni_h_w(uint32x4_t left, uint32x4_t right, const byte n)
        ///   LSX: VSSRLRNI.H.W Vd.8H, Vj.4W, ui5
        /// </summary>
        public static Vector128<ushort> ShiftRightLogicalRoundedNarrowingSaturateLower(Vector128<uint> left, Vector128<uint> right, [ConstantExpected(Min = 0, Max = (byte)(31))] byte shift) => ShiftRightLogicalRoundedNarrowingSaturateLower(left, right, shift);

        /// <summary>
        /// int32x4_t vssrlrni_w_d(int64x2_t left, int64x2_t right, const byte n)
        ///   LSX: VSSRLRNI.W.D Vd.4W, Vj.2D, ui6
        /// </summary>
        public static Vector128<int> ShiftRightLogicalRoundedNarrowingSaturateLower(Vector128<long> left, Vector128<long> right, [ConstantExpected(Min = 0, Max = (byte)(63))] byte shift) => ShiftRightLogicalRoundedNarrowingSaturateLower(left, right, shift);

        /// <summary>
        /// uint32x4_t vssrlrni_w_d(uint64x2_t left, uint64x2_t right, const byte n)
        ///   LSX: VSSRLRNI.W.D Vd.4W, Vj.2D, ui6
        /// </summary>
        public static Vector128<uint> ShiftRightLogicalRoundedNarrowingSaturateLower(Vector128<ulong> left, Vector128<ulong> right, [ConstantExpected(Min = 0, Max = (byte)(63))] byte shift) => ShiftRightLogicalRoundedNarrowingSaturateLower(left, right, shift);

        ///// <summary>
        ///// int64x2_t vssrlrni_d_q(int128x1_t left, int128x1_t right, const byte n)
        /////   LSX: VSSRLRNI.D.Q Vd.2D, Vj.Q, ui7
        ///// </summary>
        //public static Vector128<long> ShiftRightLogicalRoundedNarrowingSaturateLower(Vector128<longlong> left, Vector128<longlong> right, [ConstantExpected(Min = 0, Max = (byte)(127))] byte shift) => ShiftRightLogicalRoundedNarrowingSaturateLower(left, right, shift);

        /// <summary>
        /// int8x8_t vssrlrn_b_h(int16x8_t value, int16x8_t shift)
        ///   LSX: VSSRLRN.B.H Vd.8B, Vj.8H, Vk.8H
        /// </summary>
        public static Vector64<sbyte> ShiftRightLogicalRoundedNarrowingSaturateLower(Vector128<short> value, Vector128<short> shift) => ShiftRightLogicalRoundedNarrowingSaturateLower(value, shift);

        /// <summary>
        /// uint8x8_t vssrlrn_b_h(uint16x8_t value, uint16x8_t shift)
        ///   LSX: VSSRLRN.B.H Vd.8B, Vj.8H, Vk.8H
        /// </summary>
        public static Vector64<byte> ShiftRightLogicalRoundedNarrowingSaturateLower(Vector128<ushort> value, Vector128<ushort> shift) => ShiftRightLogicalRoundedNarrowingSaturateLower(value, shift);

        /// <summary>
        /// int16x4_t vssrlrn_h_w(int32x4_t value, int32x4_t shift)
        ///   LSX: VSSRLRN.H.W Vd.4H, Vj.4W, Vk.4W
        /// </summary>
        public static Vector64<short> ShiftRightLogicalRoundedNarrowingSaturateLower(Vector128<int> value, Vector128<int> shift) => ShiftRightLogicalRoundedNarrowingSaturateLower(value, shift);

        /// <summary>
        /// uint16x4_t vssrlrn_h_w(uint32x4_t value, uint32x4_t shift)
        ///   LSX: VSSRLRN.H.W Vd.4H, Vj.4W, Vk.4W
        /// </summary>
        public static Vector64<ushort> ShiftRightLogicalRoundedNarrowingSaturateLower(Vector128<uint> value, Vector128<uint> shift) => ShiftRightLogicalRoundedNarrowingSaturateLower(value, shift);

        /// <summary>
        /// int32x2_t vssrlrn_w_d(int64x2_t value, int64x2_t shift)
        ///   LSX: VSSRLRN.W.D Vd.2W, Vj.2D, Vk.2D
        /// </summary>
        public static Vector64<int> ShiftRightLogicalRoundedNarrowingSaturateLower(Vector128<long> value, Vector128<long> shift) => ShiftRightLogicalRoundedNarrowingSaturateLower(value, shift);

        /// <summary>
        /// uint32x2_t vssrlrn_w_d(uint64x2_t value, uint64x2_t shift)
        ///   LSX: VSSRLRN.W.D Vd.2W, Vj.2D, Vk.2D
        /// </summary>
        public static Vector64<uint> ShiftRightLogicalRoundedNarrowingSaturateLower(Vector128<ulong> value, Vector128<ulong> shift) => ShiftRightLogicalRoundedNarrowingSaturateLower(value, shift);

        /// <summary>
        /// uint16x8_t vssrlrni_bu_h(uint16x8_t left, uint16x8_t right, const byte n)
        ///   LSX: VSSRLRNI.BU.H Vd.16B, Vj.8H, ui4
        /// </summary>
        public static Vector128<byte> ShiftRightLogicalRoundedNarrowingSaturateUnsignedLower(Vector128<ushort> left, Vector128<ushort> right, [ConstantExpected(Min = 0, Max = (byte)(15))] byte shift) => ShiftRightLogicalRoundedNarrowingSaturateUnsignedLower(left, right, shift);

        /// <summary>
        /// uint16x8_t vssrlrni_hu_w(uint32x4_t left, uint32x4_t right, const byte n)
        ///   LSX: VSSRLRNI.HU.W Vd.8H, Vj.4W, ui5
        /// </summary>
        public static Vector128<ushort> ShiftRightLogicalRoundedNarrowingSaturateUnsignedLower(Vector128<uint> left, Vector128<uint> right, [ConstantExpected(Min = 0, Max = (byte)(31))] byte shift) => ShiftRightLogicalRoundedNarrowingSaturateUnsignedLower(left, right, shift);

        /// <summary>
        /// uint32x4_t vssrlrni_wu_d(uint64x2_t left, uint64x2_t right, const byte n)
        ///   LSX: VSSRLRNI.WU.D Vd.4W, Vj.2D, ui6
        /// </summary>
        public static Vector128<uint> ShiftRightLogicalRoundedNarrowingSaturateUnsignedLower(Vector128<ulong> left, Vector128<ulong> right, [ConstantExpected(Min = 0, Max = (byte)(63))] byte shift) => ShiftRightLogicalRoundedNarrowingSaturateUnsignedLower(left, right, shift);

        ///// <summary>
        ///// uint64x2_t vssrlrni_du_q(uint128x1_t left, uint128x1_t right, const byte n)
        /////   LSX: VSSRLRNI.DU.Q Vd.2D, Vj.Q, ui7
        ///// </summary>
        //public static Vector128<ulong> ShiftRightLogicalRoundedNarrowingSaturateUnsignedLower(Vector128<ulonglong> left, Vector128<ulonglong> right, [ConstantExpected(Min = 0, Max = (byte)(127))] byte shift) => ShiftRightLogicalRoundedNarrowingSaturateUnsignedLower(left, right, shift);

        /// <summary>
        /// uint8x8_t vssrlrn_bu_h(uint16x8_t value, uint16x8_t shift)
        ///   LSX: VSSRLRN.BU.H Vd.8B, Vj.8H, Vk.8H
        /// </summary>
        public static Vector64<byte> ShiftRightLogicalRoundedNarrowingSaturateUnsignedLower(Vector128<ushort> value, Vector128<ushort> shift) => ShiftRightLogicalRoundedNarrowingSaturateUnsignedLower(value, shift);

        /// <summary>
        /// uint16x4_t vssrlrn_hu_w(uint32x4_t value, uint32x4_t shift)
        ///   LSX: VSSRLRN.HU.W Vd.4H, Vj.4W, Vk.4W
        /// </summary>
        public static Vector64<ushort> ShiftRightLogicalRoundedNarrowingSaturateUnsignedLower(Vector128<uint> value, Vector128<uint> shift) => ShiftRightLogicalRoundedNarrowingSaturateUnsignedLower(value, shift);

        /// <summary>
        /// uint32x2_t vssrlrn_wu_d(uint64x2_t value, uint64x2_t shift)
        ///   LSX: VSSRLRN.WU.D Vd.2W, Vj.2D, Vk.2D
        /// </summary>
        public static Vector64<uint> ShiftRightLogicalRoundedNarrowingSaturateUnsignedLower(Vector128<ulong> value, Vector128<ulong> shift) => ShiftRightLogicalRoundedNarrowingSaturateUnsignedLower(value, shift);

        /// <summary>
        /// int16x8_t vssrarn_b_h(int16x8_t left, int16x8_t right, const byte n)
        ///   LSX: VSSRARNI.B.H Vd.16B, Vj.8H, ui4  ///NOTE: the Vd is both input and output.
        /// </summary>
        public static Vector128<sbyte> ShiftRightArithmeticRoundedNarrowingSaturateLower(Vector128<short> left, Vector128<short> right, [ConstantExpected(Min = 0, Max = (byte)(15))] byte shift) => ShiftRightArithmeticRoundedNarrowingSaturateLower(left, right, shift);

        /// <summary>
        /// int16x8_t vssrarn_h_w(int32x4_t left, int32x4_t right, const byte n)
        ///   LSX: VSSRARNI.H.W Vd.8H, Vj.4W, ui5
        /// </summary>
        public static Vector128<short> ShiftRightArithmeticRoundedNarrowingSaturateLower(Vector128<int> left, Vector128<int> right, [ConstantExpected(Min = 0, Max = (byte)(31))] byte shift) => ShiftRightArithmeticRoundedNarrowingSaturateLower(left, right, shift);

        /// <summary>
        /// int32x4_t vssrarn_w_d(int64x2_t left, int64x2_t right, const byte n)
        ///   LSX: VSSRARNI.W.D Vd.4W, Vj.2D, ui6
        /// </summary>
        public static Vector128<int> ShiftRightArithmeticRoundedNarrowingSaturateLower(Vector128<long> left, Vector128<long> right, [ConstantExpected(Min = 0, Max = (byte)(63))] byte shift) => ShiftRightArithmeticRoundedNarrowingSaturateLower(left, right, shift);

        ///// <summary>
        ///// int64x2_t vssrarn_d_q(int128x1_t left, int128x1_t right, const byte n)
        /////   LSX: VSSRARNI.D.Q Vd.2D, Vj.Q, ui7
        ///// </summary>
        //public static Vector128<long> ShiftRightArithmeticRoundedNarrowingSaturateLower(Vector128<longlong> left, Vector128<longlong> right, [ConstantExpected(Min = 0, Max = (byte)(127))] byte shift) => ShiftRightArithmeticRoundedNarrowingSaturateLower(left, right, shift);

        /// <summary>
        /// int8x8_t vssrarn_b_h(int16x8_t value, int16x8_t shift)
        ///   LSX: VSSRARN.B.H Vd.8B, Vj.8H, Vk.8H
        /// </summary>
        public static Vector64<sbyte> ShiftRightArithmeticRoundedNarrowingSaturateLower(Vector128<short> value, Vector128<short> shift) => ShiftRightArithmeticRoundedNarrowingSaturateLower(value, shift);

        /// <summary>
        /// int16x4_t vssrarn_h_w(int32x4_t value, int32x4_t shift)
        ///   LSX: VSSRARN.H.W Vd.4H, Vj.4W, Vk.4W
        /// </summary>
        public static Vector64<short> ShiftRightArithmeticRoundedNarrowingSaturateLower(Vector128<int> value, Vector128<int> shift) => ShiftRightArithmeticRoundedNarrowingSaturateLower(value, shift);

        /// <summary>
        /// int32x2_t vssrarn_w_d(int64x2_t value, int64x2_t shift)
        ///   LSX: VSSRARN.W.D Vd.2W, Vj.2D, Vk.2D
        /// </summary>
        public static Vector64<int> ShiftRightArithmeticRoundedNarrowingSaturateLower(Vector128<long> value, Vector128<long> shift) => ShiftRightArithmeticRoundedNarrowingSaturateLower(value, shift);

        /// <summary>
        /// uint16x8_t vssrarni_bu_h(int16x8_t left, int16x8_t right, const byte n)
        ///   LSX: VSSRARNI.BU.H Vd.16B, Vj.8H, ui4
        /// </summary>
        public static Vector128<byte> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLower(Vector128<short> left, Vector128<short> right, [ConstantExpected(Min = 0, Max = (byte)(15))] byte shift) => ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLower(left, right, shift);

        /// <summary>
        /// uint16x8_t vssrarni_hu_w(int32x4_t left, int32x4_t right, const byte n)
        ///   LSX: VSSRARNI.HU.W Vd.8H, Vj.4W, ui5
        /// </summary>
        public static Vector128<ushort> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLower(Vector128<int> left, Vector128<int> right, [ConstantExpected(Min = 0, Max = (byte)(31))] byte shift) => ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLower(left, right, shift);

        /// <summary>
        /// uint32x4_t vssrarni_wu_d(int64x2_t left, int64x2_t right, const byte n)
        ///   LSX: VSSRARNI.WU.D Vd.4W, Vj.2D, ui6
        /// </summary>
        public static Vector128<uint> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLower(Vector128<long> left, Vector128<long> right, [ConstantExpected(Min = 0, Max = (byte)(63))] byte shift) => ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLower(left, right, shift);

        ///// <summary>
        ///// uint64x2_t vssrarni_du_q(int128x1_t left, int128x1_t right, const byte n)
        /////   LSX: VSSRARNI.DU.Q Vd.2D, Vj.Q, ui7
        ///// </summary>
        //public static Vector128<ulong> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLower(Vector128<longlong> left, Vector128<longlong> right, [ConstantExpected(Min = 0, Max = (byte)(127))] byte shift) => ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLower(left, right, shift);

        /// <summary>
        /// uint8x8_t vssrarn_bu_h(int16x8_t value, int16x8_t shift)
        ///   LSX: VSSRARN.BU.H Vd.8B, Vj.8H, Vk.8H
        /// </summary>
        public static Vector64<byte> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLower(Vector128<short> value, Vector128<short> shift) => ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLower(value, shift);

        /// <summary>
        /// uint16x4_t vssrarn_hu_w(int32x4_t value, int32x4_t shift)
        ///   LSX: VSSRARN.HU.W Vd.4H, Vj.4W, Vk.4W
        /// </summary>
        public static Vector64<ushort> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLower(Vector128<int> value, Vector128<int> shift) => ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLower(value, shift);

        /// <summary>
        /// uint32x2_t vssrarn_wu_d(int64x2_t value, int64x2_t shift)
        ///   LSX: VSSRARN.WU.D Vd.2W, Vj.2D, Vk.2D
        /// </summary>
        public static Vector64<uint> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLower(Vector128<long> value, Vector128<long> shift) => ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLower(value, shift);

        /// <summary>
        /// int8x8_t vclo_b(int8x8_t a)
        ///   LSX: VCLO.B Vd.8B, Vj.8B
        /// </summary>
        public static Vector64<sbyte> LeadingSignCount(Vector64<sbyte> value) => LeadingSignCount(value);

        /// <summary>
        /// int16x4_t vclo_h(int16x4_t a)
        ///   LSX: VCLO.H Vd.4H, Vj.4H
        /// </summary>
        public static Vector64<short> LeadingSignCount(Vector64<short> value) => LeadingSignCount(value);

        /// <summary>
        /// int32x2_t vclo_w(int32x2_t a)
        ///   LSX: VCLO.W Vd.2W, Vj.2W
        /// </summary>
        public static Vector64<int> LeadingSignCount(Vector64<int> value) => LeadingSignCount(value);

        /// <summary>
        /// int64x1_t vclo_d(int64x1_t a)
        ///   LSX: VCLO.D Vd.D, Vj.D
        /// </summary>
        public static Vector64<long> LeadingSignCount(Vector64<long> value) => LeadingSignCount(value);

        /// <summary>
        /// int8x16_t vclo_b(int8x16_t a)
        ///   LSX: VCLO.B Vd.16B, Vj.16B
        /// </summary>
        public static Vector128<sbyte> LeadingSignCount(Vector128<sbyte> value) => LeadingSignCount(value);

        /// <summary>
        /// int16x8_t vclo_h(int16x8_t a)
        ///   LSX: VCLO.H Vd.8H, Vj.8H
        /// </summary>
        public static Vector128<short> LeadingSignCount(Vector128<short> value) => LeadingSignCount(value);

        /// <summary>
        /// int32x4_t vclo_w(int32x4_t a)
        ///   LSX: VCLO.W Vd.4W, Vj.4W
        /// </summary>
        public static Vector128<int> LeadingSignCount(Vector128<int> value) => LeadingSignCount(value);

        /// <summary>
        /// int64x2_t vclo_d(int64x2_t a)
        ///   LSX: VCLO.D Vd.2D, Vj.2D
        /// </summary>
        public static Vector128<long> LeadingSignCount(Vector128<long> value) => LeadingSignCount(value);

        /// <summary>
        /// uint8x8_t vclz_b(uint8x8_t a)
        ///   LSX: VCLZ.B Vd.8B, Vj.8B
        /// </summary>
        public static Vector64<byte> LeadingZeroCount(Vector64<byte> value) => LeadingZeroCount(value);

        /// <summary>
        /// int8x8_t vclz_b(int8x8_t a)
        ///   LSX: VCLZ.B Vd.8B, Vj.8B
        /// </summary>
        public static Vector64<sbyte> LeadingZeroCount(Vector64<sbyte> value) => LeadingZeroCount(value);

        /// <summary>
        /// int16x4_t vclz_h(int16x4_t a)
        ///   LSX: VCLZ.H Vd.4H, Vj.4H
        /// </summary>
        public static Vector64<short> LeadingZeroCount(Vector64<short> value) => LeadingZeroCount(value);

        /// <summary>
        /// uint16x4_t vclz_h(uint16x4_t a)
        ///   LSX: VCLZ.H Vd.4H, Vj.4H
        /// </summary>
        public static Vector64<ushort> LeadingZeroCount(Vector64<ushort> value) => LeadingZeroCount(value);

        /// <summary>
        /// int32x2_t vclz_w(int32x2_t a)
        ///   LSX: VCLZ.W Vd.2W, Vj.2W
        /// </summary>
        public static Vector64<int> LeadingZeroCount(Vector64<int> value) => LeadingZeroCount(value);

        /// <summary>
        /// uint32x2_t vclz_w(uint32x2_t a)
        ///   LSX: VCLZ.W Vd.2W, Vj.2W
        /// </summary>
        public static Vector64<uint> LeadingZeroCount(Vector64<uint> value) => LeadingZeroCount(value);

        /// <summary>
        /// int64x1_t vclz_d(int64x1_t a)
        ///   LSX: VCLZ.D Vd.D, Vj.D
        /// </summary>
        public static Vector64<long> LeadingZeroCount(Vector64<long> value) => LeadingZeroCount(value);

        /// <summary>
        /// uint64x1_t vclz_d(uint64x1_t a)
        ///   LSX: VCLZ.D Vd.D, Vj.D
        /// </summary>
        public static Vector64<ulong> LeadingZeroCount(Vector64<ulong> value) => LeadingZeroCount(value);

        /// <summary>
        /// int8x16_t vclz_b(int8x16_t a)
        ///   LSX: VCLZ.B Vd.16B, Vj.16B
        /// </summary>
        public static Vector128<sbyte> LeadingZeroCount(Vector128<sbyte> value) => LeadingZeroCount(value);

        /// <summary>
        /// uint8x16_t vclz_b(uint8x16_t a)
        ///   LSX: VCLZ.B Vd.16B, Vj.16B
        /// </summary>
        public static Vector128<byte> LeadingZeroCount(Vector128<byte> value) => LeadingZeroCount(value);

        /// <summary>
        /// int16x8_t vclz_h(int16x8_t a)
        ///   LSX: VCLZ.H Vd.8H, Vj.8H
        /// </summary>
        public static Vector128<short> LeadingZeroCount(Vector128<short> value) => LeadingZeroCount(value);

        /// <summary>
        /// uint16x8_t vclz_h(uint16x8_t a)
        ///   LSX: VCLZ.H Vd.8H, Vj.8H
        /// </summary>
        public static Vector128<ushort> LeadingZeroCount(Vector128<ushort> value) => LeadingZeroCount(value);

        /// <summary>
        /// int32x4_t vclz_w(int32x4_t a)
        ///   LSX: VCLZ.W Vd.4W, Vj.4W
        /// </summary>
        public static Vector128<int> LeadingZeroCount(Vector128<int> value) => LeadingZeroCount(value);

        /// <summary>
        /// uint32x4_t vclz_w(uint32x4_t a)
        ///   LSX: VCLZ.W Vd.4W, Vj.4W
        /// </summary>
        public static Vector128<uint> LeadingZeroCount(Vector128<uint> value) => LeadingZeroCount(value);

        /// <summary>
        /// int64x2_t vclz_d(int64x2_t a)
        ///   LSX: VCLZ.D Vd.2D, Vj.2D
        /// </summary>
        public static Vector128<long> LeadingZeroCount(Vector128<long> value) => LeadingZeroCount(value);

        /// <summary>
        /// uint64x2_t vclz_d(uint64x2_t a)
        ///   LSX: VCLZ.D Vd.2D, Vj.2D
        /// </summary>
        public static Vector128<ulong> LeadingZeroCount(Vector128<ulong> value) => LeadingZeroCount(value);

        /// <summary>
        /// int8x16_t vpcnt_b(int8x16_t a)
        ///   LSX: VPCNT_B Vd, Vj
        /// </summary>
        public static Vector128<sbyte> PopCount(Vector128<sbyte> value) => PopCount(value);

        /// <summary>
        /// uint8x16_t vpcnt_b(uint8x16_t a)
        ///   LSX: VPCNT_B Vd, Vj
        /// </summary>
        public static Vector128<byte> PopCount(Vector128<byte> value) => PopCount(value);

        /// <summary>
        /// int16x8_t vpcnt_h(int16x8_t a)
        ///   LSX: VPCNT_H Vd, Vj
        /// </summary>
        public static Vector128<short> PopCount(Vector128<short> value) => PopCount(value);

        /// <summary>
        /// uint16x8_t vpcnt_h(uint16x8_t a)
        ///   LSX: VPCNT_H Vd, Vj
        /// </summary>
        public static Vector128<ushort> PopCount(Vector128<ushort> value) => PopCount(value);

        /// <summary>
        /// int32x4_t vpcnt_w(int32x4_t a)
        ///   LSX: VPCNT_W Vd, Vj
        /// </summary>
        public static Vector128<int> PopCount(Vector128<int> value) => PopCount(value);

        /// <summary>
        /// uint32x4_t vpcnt_w(uint32x4_t a)
        ///   LSX: VPCNT_W Vd, Vj
        /// </summary>
        public static Vector128<uint> PopCount(Vector128<uint> value) => PopCount(value);

        /// <summary>
        /// int64x2_t vpcnt_d(int64x2_t a)
        ///   LSX: VPCNT_D Vd, Vj
        /// </summary>
        public static Vector128<long> PopCount(Vector128<long> value) => PopCount(value);

        /// <summary>
        /// uint64x2_t vpcnt_d(uint64x2_t a)
        ///   LSX: VPCNT_D Vd, Vj
        /// </summary>
        public static Vector128<ulong> PopCount(Vector128<ulong> value) => PopCount(value);

        /// <summary>
        ///  uint8x16_t vshuf_b(uint8x16_t vec, uint8x16_t idx)
        ///   LSX: VSHUF.B Vd.16B, Vj.16B, Vk.16B, Va.16B
        /// </summary>
        public static Vector128<byte> VectorShuffle(Vector128<byte> vector, Vector128<byte> indexs) => VectorShuffle(vector, indexs);

        /// <summary>
        ///  int8x16_t vshuf_b(int8x16_t vec, int8x16_t idx)
        ///   LSX: VSHUF.B Vd.16B, Vj.16B, Vk.16B, Va.16B
        /// </summary>
        public static Vector128<sbyte> VectorShuffle(Vector128<sbyte> vector, Vector128<sbyte> indexs) => VectorShuffle(vector, indexs);

        /// <summary>
        ///  uint8x16_t vshuf_b(uint8x16_t vec0, uint8x16_t vec1, uint8x16_t idx)
        ///   LSX: VSHUF.B Vd.16B, Vj.16B, Vk.16B, Va.16B
        /// </summary>
        public static Vector128<byte> VectorShuffle(Vector128<byte> vector0, Vector128<byte> vector1, Vector128<byte> indexs) => VectorShuffle(vector0, vector1, indexs);

        /// <summary>
        ///  int8x16_t vshuf_b(int8x16_t vec0, int8x16_t vec1, int8x16_t idx)
        ///   LSX: VSHUF.B Vd.16B, Vj.16B, Vk.16B, Va.16B
        /// </summary>
        public static Vector128<sbyte> VectorShuffle(Vector128<sbyte> vector0, Vector128<sbyte> vector1, Vector128<sbyte> indexs) => VectorShuffle(vector0, vector1, indexs);

        /// <summary>
        ///  int16x8_t vshuf_h(int16x8_t vec0, int16x8_t vec1, int16x8_t idx)
        ///   LSX: VSHUF.H Vd.8H, Vj.8H, Vk.8H                                //NOTE: Vd is both input and output while input as index.
        /// </summary>
        public static Vector128<short> VectorShuffle(Vector128<short> vector0, Vector128<short> vector1, Vector128<short> indexs) => VectorShuffle(vector0, vector1, indexs);

        /// <summary>
        ///  uint16x8_t vshuf_h(uint16x8_t vecj, uint16x8_t veck, uint16x8_t idx)
        ///   LSX: VSHUF.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> VectorShuffle(Vector128<ushort> vector0, Vector128<ushort> vector1, Vector128<ushort> indexs) => VectorShuffle(vector0, vector1, indexs);

        /// <summary>
        ///  int32x4_t vpermi_w(int32x4_t vec, uint8_t idx)
        ///   LSX: VPERMI.W Vd.4W, Vj.4W, ui8
        /// </summary>
        public static Vector128<int> VectorShuffle(Vector128<int> vector, const byte indexs) => VectorShuffle(vector, indexs);

        /// <summary>
        ///  uint32x4_t vpermi_w(uint32x4_t vec, uint8_t idx)
        ///   LSX: VPERMI.W Vd.4W, Vj.4W, ui8
        /// </summary>
        public static Vector128<uint> VectorShuffle(Vector128<uint> vector, const byte indexs) => VectorShuffle(vector, indexs);

        /// <summary>
        ///  int32x4_t vshuf_w(int32x4_t vec0, int32x4_t vec1, int32x4_t idx)
        ///   LSX: VSHUF.W Vd.4W, Vj.4W, Vk.4W                                //NOTE: Vd is both input and output while input as index.
        /// </summary>
        public static Vector128<int> VectorShuffle(Vector128<int> vector0, Vector128<int> vector1, Vector128<int> indexs) => VectorShuffle(vector0, vector1, indexs);

        /// <summary>
        ///  uint32x4_t vshuf_w(uint32x4_t vecj, uint32x4_t veck, uint32x4_t idx)
        ///   LSX: VSHUF.W Vd.4W, Vj.4W, Vk.4W                                //NOTE: Vd is both input and output while input as index.
        /// </summary>
        public static Vector128<uint> VectorShuffle(Vector128<uint> vector0, Vector128<uint> vector1, Vector128<uint> indexs) => VectorShuffle(vector0, vector1, indexs);

        /// <summary>
        ///  int64x2_t vshuf_d(int64x2_t vec0, int64x2_t vec1, int64x2_t idx)
        ///   LSX: VSHUF.D Vd.2D, Vj.2D, Vk.2D                                //NOTE: Vd is both input and output while input as index.
        /// </summary>
        public static Vector128<long> VectorShuffle(Vector128<long> vector0, Vector128<long> vector1, Vector128<long> indexs) => VectorShuffle(vector0, vector1, indexs);

        /// <summary>
        ///  uint64x2_t vshuf_d(uint64x2_t vecj, uint64x2_t veck, uint64x2_t idx)
        ///   LSX: VSHUF.D Vd.2D, Vj.2D, Vk.2D                                //NOTE: Vd is both input and output while input as index.
        /// </summary>
        public static Vector128<ulong> VectorShuffle(Vector128<ulong> vector0, Vector128<ulong> vector1, Vector128<ulong> indexs) => VectorShuffle(vector0, vector1, indexs);

        /// <summary>
        ///  int8x16_t vshuf4i_b(int8x16_t vec, uint8_t idx)
        ///   LSX: VSHUF4I.B Vd.16B, Vj.16B, ui8
        /// </summary>
        public static Vector128<sbyte> VectorShuffleBy4Elements(Vector128<sbyte> vector, byte indexs) => VectorShuffleBy4Elements(vector, indexs);

        /// <summary>
        ///  uint8x16_t vshuf4i_b(uint8x16_t vec, uint8_t idx)
        ///   LSX: VSHUF4I.B Vd.16B, Vj.16B, ui8
        /// </summary>
        public static Vector128<byte> VectorShuffleBy4Elements(Vector128<byte> vector, byte indexs) => VectorShuffleBy4Elements(vector, indexs);

        /// <summary>
        ///  int16x8_t vshuf4i_h(int16x8_t vec, uint8_t idx)
        ///   LSX: VSHUF4I.H Vd.8H, Vj.8H, ui8
        /// </summary>
        public static Vector128<short> VectorShuffleBy4Elements(Vector128<short> vector, byte indexs) => VectorShuffleBy4Elements(vector, indexs);

        /// <summary>
        ///  uint16x8_t vshuf4i_h(uint16x8_t vec, uint8_t idx)
        ///   LSX: VSHUF4I.H Vd.8H, Vj.8H, ui8
        /// </summary>
        public static Vector128<ushort> VectorShuffleBy4Elements(Vector128<ushort> vector, byte indexs) => VectorShuffleBy4Elements(vector, indexs);

        /// <summary>
        ///  int32x4_t vshuf4i_w(int32x4_t vec, uint8_t idx)
        ///   LSX: VSHUF4I.W Vd.4W, Vj.4W, ui8
        /// </summary>
        public static Vector128<int> VectorShuffleBy4Elements(Vector128<int> vector, byte indexs) => VectorShuffleBy4Elements(vector, indexs);

        /// <summary>
        ///  uint32x4_t vshuf4i_w(uint32x4_t vec, uint8_t idx)
        ///   LSX: VSHUF4I.W Vd.4W, Vj.4W, ui8
        /// </summary>
        public static Vector128<uint> VectorShuffleBy4Elements(Vector128<uint> vector, byte indexs) => VectorShuffleBy4Elements(vector, indexs);

        /// <summary>
        ///  int64x2_t vshuf4i_d(int64x2_t vec0, uint64x2_t vec1, uint8_t idx)
        ///   LSX: VSHUF4I.D Vd.2D, Vj.2D, ui4
        /// </summary>
        public static Vector128<long> VectorShuffleBy4Elements(Vector128<long> vector0, Vector128<long> vector1, byte indexs) => VectorShuffleBy4Elements(vector0, vector1, indexs);

        /// <summary>
        ///  uint64x2_t vshuf4i_d(uint64x2_t vec0, uint64x2_t vec1, uint8_t idx)
        ///   LSX: VSHUF4I.D Vd.2D, Vj.2D, ui4
        /// </summary>
        public static Vector128<ulong> VectorShuffleBy4Elements(Vector128<ulong> vector0, Vector128<ulong> vector1, byte indexs) => VectorShuffleBy4Elements(vector0, vector1, indexs);

        /// <summary>
        ///  int8x16_t vilvl_b(int8x16_t vec0, int8x16_t vec1)
        ///   LSX: VILVL.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> VectorElementsFusionLower(Vector128<sbyte> left, Vector128<sbyte> right) => VectorElementsFusionLower(left, right);

        /// <summary>
        ///  uint8x16_t vilvl_b(uint8x16_t vec0, uint8x16_t vec1)
        ///   LSX: VILVL.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> VectorElementsFusionLower(Vector128<byte> left, Vector128<byte> right) => VectorElementsFusionLower(left, right);

        /// <summary>
        ///  int16x8_t vilvl_h(int16x8_t vec0, int16x8_t vec1)
        ///   LSX: VILVL.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> VectorElementsFusionLower(Vector128<short> left, Vector128<short> right) => VectorElementsFusionLower(left, right);

        /// <summary>
        ///  uint16x8_t vilvl_h(uint16x8_t vec0, uint16x8_t vec1)
        ///   LSX: VILVL.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> VectorElementsFusionLower(Vector128<ushort> left, Vector128<ushort> right) => VectorElementsFusionLower(left, right);

        /// <summary>
        ///  int32x4_t vilvl_w(int32x4_t vec0, int32x4_t vec1)
        ///   LSX: VILVL.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<int> VectorElementsFusionLower(Vector128<int> left, Vector128<int> right) => VectorElementsFusionLower(left, right);

        /// <summary>
        ///  uint32x4_t vilvl_w(uint32x4_t vec0, uint32x4_t vec1)
        ///   LSX: VILVL.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<uint> VectorElementsFusionLower(Vector128<uint> left, Vector128<uint> right) => VectorElementsFusionLower(left, right);

        /// <summary>
        ///  int64x2_t vilvl_d(int64x2_t vec0, int64x2_t vec1)
        ///   LSX: VILVL.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> VectorElementsFusionLower(Vector128<long> left, Vector128<long> right) => VectorElementsFusionLower(left, right);

        /// <summary>
        ///  uint64x2_t vilvl_d(uint64x2_t vec0, uint64x2_t vec1)
        ///   LSX: VILVL.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> VectorElementsFusionLower(Vector128<ulong> left, Vector128<ulong> right) => VectorElementsFusionLower(left, right);

        /// <summary>
        ///  int8x16_t vilvh_b(int8x16_t vec0, int8x16_t vec1)
        ///   LSX: VILVH.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> VectorElementsFusionHight(Vector128<sbyte> left, Vector128<sbyte> right) => VectorElementsFusionHight(left, right);

        /// <summary>
        ///  uint8x16_t vilvh_b(uint8x16_t vec0, uint8x16_t vec1)
        ///   LSX: VILVH.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> VectorElementsFusionHight(Vector128<byte> left, Vector128<byte> right) => VectorElementsFusionHight(left, right);

        /// <summary>
        ///  int16x8_t vilvh_h(int16x8_t vec0, int16x8_t vec1)
        ///   LSX: VILVH.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> VectorElementsFusionHight(Vector128<short> left, Vector128<short> right) => VectorElementsFusionHight(left, right);

        /// <summary>
        ///  uint16x8_t vilvh_h(uint16x8_t vec0, uint16x8_t vec1)
        ///   LSX: VILVH.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> VectorElementsFusionHight(Vector128<ushort> left, Vector128<ushort> right) => VectorElementsFusionHight(left, right);

        /// <summary>
        ///  int32x4_t vilvh_w(int32x4_t vec0, int32x4_t vec1)
        ///   LSX: VILVH.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<int> VectorElementsFusionHight(Vector128<int> left, Vector128<int> right) => VectorElementsFusionHight(left, right);

        /// <summary>
        ///  uint32x4_t vilvh_w(uint32x4_t vec0, uint32x4_t vec1)
        ///   LSX: VILVH.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<uint> VectorElementsFusionHight(Vector128<uint> left, Vector128<uint> right) => VectorElementsFusionHight(left, right);

        /// <summary>
        ///  int64x2_t vilvh_d(int64x2_t vec0, int64x2_t vec1)
        ///   LSX: VILVH.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> VectorElementsFusionHight(Vector128<long> left, Vector128<long> right) => VectorElementsFusionHight(left, right);

        /// <summary>
        ///  uint64x2_t vilvh_d(uint64x2_t vec0, uint64x2_t vec1)
        ///   LSX: VILVH.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> VectorElementsFusionHight(Vector128<ulong> left, Vector128<ulong> right) => VectorElementsFusionHight(left, right);

        /// <summary>
        ///  int8x16_t vpackev_b(int8x16_t vec0, int8x16_t vec1)
        ///   LSX: VPACKEV.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> VectorElementsFusionEven(Vector128<sbyte> left, Vector128<sbyte> right) => VectorElementsFusionEven(left, right);

        /// <summary>
        ///  uint8x16_t vpackev_b(uint8x16_t vec0, uint8x16_t vec1)
        ///   LSX: VPACKEV.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> VectorElementsFusionEven(Vector128<byte> left, Vector128<byte> right) => VectorElementsFusionEven(left, right);

        /// <summary>
        ///  int16x8_t vpackev_h(int16x8_t vec0, int16x8_t vec1)
        ///   LSX: VPACKEV.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> VectorElementsFusionEven(Vector128<short> left, Vector128<short> right) => VectorElementsFusionEven(left, right);

        /// <summary>
        ///  uint16x8_t vpackev_h(uint16x8_t vec0, uint16x8_t vec1)
        ///   LSX: VPACKEV.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> VectorElementsFusionEven(Vector128<ushort> left, Vector128<ushort> right) => VectorElementsFusionEven(left, right);

        /// <summary>
        ///  int32x4_t vpackev_w(int32x4_t vec0, int32x4_t vec1)
        ///   LSX: VPACKEV.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<int> VectorElementsFusionEven(Vector128<int> left, Vector128<int> right) => VectorElementsFusionEven(left, right);

        /// <summary>
        ///  uint32x4_t vpackev_w(uint32x4_t vec0, uint32x4_t vec1)
        ///   LSX: VPACKEV.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<uint> VectorElementsFusionEven(Vector128<uint> left, Vector128<uint> right) => VectorElementsFusionEven(left, right);

        /// <summary>
        ///  int64x2_t vpackev_d(int64x2_t vec0, int64x2_t vec1)
        ///   LSX: VPACKEV.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> VectorElementsFusionEven(Vector128<long> left, Vector128<long> right) => VectorElementsFusionEven(left, right);

        /// <summary>
        ///  uint64x2_t vpackev_d(uint64x2_t vec0, uint64x2_t vec1)
        ///   LSX: VPACKEV.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> VectorElementsFusionEven(Vector128<ulong> left, Vector128<ulong> right) => VectorElementsFusionEven(left, right);

        /// <summary>
        ///  int8x16_t vpackod_b(int8x16_t vec0, int8x16_t vec1)
        ///   LSX: VPACKOD.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> VectorElementsFusionOdd(Vector128<sbyte> left, Vector128<sbyte> right) => VectorElementsFusionOdd(left, right);

        /// <summary>
        ///  uint8x16_t vpackod_b(uint8x16_t vec0, uint8x16_t vec1)
        ///   LSX: VPACKOD.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> VectorElementsFusionOdd(Vector128<byte> left, Vector128<byte> right) => VectorElementsFusionOdd(left, right);

        /// <summary>
        ///  int16x8_t vpackod_h(int16x8_t vec0, int16x8_t vec1)
        ///   LSX: VPACKOD.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> VectorElementsFusionOdd(Vector128<short> left, Vector128<short> right) => VectorElementsFusionOdd(left, right);

        /// <summary>
        ///  uint16x8_t vpackod_h(uint16x8_t vec0, uint16x8_t vec1)
        ///   LSX: VPACKOD.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> VectorElementsFusionOdd(Vector128<ushort> left, Vector128<ushort> right) => VectorElementsFusionOdd(left, right);

        /// <summary>
        ///  int32x4_t vpackod_w(int32x4_t vec0, int32x4_t vec1)
        ///   LSX: VPACKOD.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<int> VectorElementsFusionOdd(Vector128<int> left, Vector128<int> right) => VectorElementsFusionOdd(left, right);

        /// <summary>
        ///  uint32x4_t vpackod_w(uint32x4_t vec0, uint32x4_t vec1)
        ///   LSX: VPACKOD.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<uint> VectorElementsFusionOdd(Vector128<uint> left, Vector128<uint> right) => VectorElementsFusionOdd(left, right);

        /// <summary>
        ///  int64x2_t vpackod_d(int64x2_t vec0, int64x2_t vec1)
        ///   LSX: VPACKOD.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> VectorElementsFusionOdd(Vector128<long> left, Vector128<long> right) => VectorElementsFusionOdd(left, right);

        /// <summary>
        ///  uint64x2_t vpackod_d(uint64x2_t vec0, uint64x2_t vec1)
        ///   LSX: VPACKOD.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> VectorElementsFusionOdd(Vector128<ulong> left, Vector128<ulong> right) => VectorElementsFusionOdd(left, right);

        /// <summary>
        ///  int8x16_t vpickev_b(int8x16_t vec0, int8x16_t vec1)
        ///   LSX: VPICKEV.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> VectorEvenElementsJoin(Vector128<sbyte> left, Vector128<sbyte> right) => VectorEvenElementsJoin(left, right);

        /// <summary>
        ///  uint8x16_t vpickev_b(uint8x16_t vec0, uint8x16_t vec1)
        ///   LSX: VPICKEV.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> VectorEvenElementsJoin(Vector128<byte> left, Vector128<byte> right) => VectorEvenElementsJoin(left, right);

        /// <summary>
        ///  int16x8_t vpickev_h(int16x8_t vec0, int16x8_t vec1)
        ///   LSX: VPICKEV.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> VectorEvenElementsJoin(Vector128<short> left, Vector128<short> right) => VectorEvenElementsJoin(left, right);

        /// <summary>
        ///  uint16x8_t vpickev_h(uint16x8_t vec0, uint16x8_t vec1)
        ///   LSX: VPICKEV.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> VectorEvenElementsJoin(Vector128<ushort> left, Vector128<ushort> right) => VectorEvenElementsJoin(left, right);

        /// <summary>
        ///  int32x4_t vpickev_w(int32x4_t vec0, int32x4_t vec1)
        ///   LSX: VPICKEV.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<int> VectorEvenElementsJoin(Vector128<int> left, Vector128<int> right) => VectorEvenElementsJoin(left, right);

        /// <summary>
        ///  uint32x4_t vpickev_w(uint32x4_t vec0, uint32x4_t vec1)
        ///   LSX: VPICKEV.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<uint> VectorEvenElementsJoin(Vector128<uint> left, Vector128<uint> right) => VectorEvenElementsJoin(left, right);

        /// <summary>
        ///  int64x2_t vpickev_d(int64x2_t vec0, int64x2_t vec1)
        ///   LSX: VPICKEV.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> VectorEvenElementsJoin(Vector128<long> left, Vector128<long> right) => VectorEvenElementsJoin(left, right);

        /// <summary>
        ///  uint64x2_t vpickev_d(uint64x2_t vec0, uint64x2_t vec1)
        ///   LSX: VPICKEV.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> VectorEvenElementsJoin(Vector128<ulong> left, Vector128<ulong> right) => VectorEvenElementsJoin(left, right);

        /// <summary>
        ///  int8x16_t vpickod_b(int8x16_t vec0, int8x16_t vec1)
        ///   LSX: VPICKOD.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> VectorOddElementsJoin(Vector128<sbyte> left, Vector128<sbyte> right) => VectorOddElementsJoin(left, right);

        /// <summary>
        ///  uint8x16_t vpickod_b(uint8x16_t vec0, uint8x16_t vec1)
        ///   LSX: VPICKOD.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> VectorOddElementsJoin(Vector128<byte> left, Vector128<byte> right) => VectorOddElementsJoin(left, right);

        /// <summary>
        ///  int16x8_t vpickod_h(int16x8_t vec0, int16x8_t vec1)
        ///   LSX: VPICKOD.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> VectorOddElementsJoin(Vector128<short> left, Vector128<short> right) => VectorOddElementsJoin(left, right);

        /// <summary>
        ///  uint16x8_t vpickod_h(uint16x8_t vec0, uint16x8_t vec1)
        ///   LSX: VPICKOD.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> VectorOddElementsJoin(Vector128<ushort> left, Vector128<ushort> right) => VectorOddElementsJoin(left, right);

        /// <summary>
        ///  int32x4_t vpickod_w(int32x4_t vec0, int32x4_t vec1)
        ///   LSX: VPICKOD.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<int> VectorOddElementsJoin(Vector128<int> left, Vector128<int> right) => VectorOddElementsJoin(left, right);

        /// <summary>
        ///  uint32x4_t vpickod_w(uint32x4_t vec0, uint32x4_t vec1)
        ///   LSX: VPICKOD.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<uint> VectorOddElementsJoin(Vector128<uint> left, Vector128<uint> right) => VectorOddElementsJoin(left, right);

        /// <summary>
        ///  int64x2_t vpickod_d(int64x2_t vec0, int64x2_t vec1)
        ///   LSX: VPICKOD.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> VectorOddElementsJoin(Vector128<long> left, Vector128<long> right) => VectorOddElementsJoin(left, right);

        /// <summary>
        ///  uint64x2_t vpickod_d(uint64x2_t vec0, uint64x2_t vec1)
        ///   LSX: VPICKOD.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> VectorOddElementsJoin(Vector128<ulong> left, Vector128<ulong> right) => VectorOddElementsJoin(left, right);

        /// <summary>
        ///  uint8x16_t vreplve_b(uint8x16_t vector, uint8_t idx)
        ///   LSX: VREPLVE.B Vd.16B, Vj.16B, rk
        ///   LSX: VREPLVEI.B Vd.16B, Vj.16B, ui4
        /// </summary>
        public static Vector128<byte> VectorElementReplicate(Vector128<byte> vector, byte elementIndexe) => VectorElementReplicate(vector, elementIndexe);

        /// <summary>
        ///  int8x16_t vreplve_b(int8x16_t vector, uint8_t idx)
        ///   LSX: VREPLVE.B Vd.16B, Vj.16B, rk
        ///   LSX: VREPLVEI.B Vd.16B, Vj.16B, ui4
        /// </summary>
        public static Vector128<sbyte> VectorElementReplicate(Vector128<sbyte> vector, byte elementIndexe) => VectorElementReplicate(vector, elementIndexe);

        /// <summary>
        ///  int16x8_t vreplve_h(int16x8_t vector, uint8_t idx)
        ///   LSX: VREPLVE.H Vd.8H, Vj.8H, rk
        ///   LSX: VREPLVEI.H Vd.8H, Vj.8H, ui3
        /// </summary>
        public static Vector128<short> VectorElementReplicate(Vector128<short> vector, byte elementIndexe) => VectorElementReplicate(vector, elementIndexe);

        /// <summary>
        ///  uint16x8_t vreplve_h(uint16x8_t vector, uint8_t idx)
        ///   LSX: VREPLVE.H Vd.8H, Vj.8H, rk
        ///   LSX: VREPLVEI.H Vd.8H, Vj.8H, ui3
        /// </summary>
        public static Vector128<ushort> VectorElementReplicate(Vector128<ushort> vector, byte elementIndexe) => VectorElementReplicate(vector, elementIndexe);

        /// <summary>
        ///  int32x4_t vreplve_w(int32x4_t vector, uint8_t idx)
        ///   LSX: VREPLVE.W Vd.4W, Vj.4W, rk
        ///   LSX: VREPLVEI.W Vd.4W, Vj.4W, ui2
        /// </summary>
        public static Vector128<int> VectorElementReplicate(Vector128<int> vector, byte elementIndexe) => VectorElementReplicate(vector, elementIndexe);

        /// <summary>
        ///  uint32x4_t vreplve_w(uint32x4_t vector, uint8_t idx)
        ///   LSX: VREPLVE.W Vd.4W, Vj.4W, rk
        ///   LSX: VREPLVEI.W Vd.4W, Vj.4W, ui2
        /// </summary>
        public static Vector128<uint> VectorElementReplicate(Vector128<uint> vector, byte elementIndexe) => VectorElementReplicate(vector, elementIndexe);

        /// <summary>
        ///  int64x2_t vreplve_d(int64x2_t vector, uint8_t idx)
        ///   LSX: VREPLVE.D Vd.2D, Vj.2D, rk
        ///   LSX: VREPLVEI.D Vd.2D, Vj.2D, ui1
        /// </summary>
        public static Vector128<long> VectorElementReplicate(Vector128<long> vector, byte elementIndexe) => VectorElementReplicate(vector, elementIndexe);

        /// <summary>
        ///  uint64x2_t vreplve_d(uint64x2_t vector, uint8_t idx)
        ///   LSX: VREPLVE.D Vd.2D, Vj.2D, rk
        ///   LSX: VREPLVEI.D Vd.2D, Vj.2D, ui1
        /// </summary>
        public static Vector128<ulong> VectorElementReplicate(Vector128<ulong> vector, byte elementIndexe) => VectorElementReplicate(vector, elementIndexe);

        /// <summary>
        ///  int8x16_t vextrins_b(int8x16_t vec, uint8_t idx)
        ///   LSX: VEXTRINS.B Vd.16B, Vj.16B, ui8
        /// </summary>
        public static Vector128<sbyte> UpdateOneVectorElement(Vector128<sbyte> vector, const byte indexs) => UpdateOneVectorElement(vector, indexs);

        /// <summary>
        ///  uint8x16_t vextrins_b(uint8x16_t vec, uint8_t idx)
        ///   LSX: VEXTRINS.B Vd.16B, Vj.16B, ui8
        /// </summary>
        public static Vector128<byte> UpdateOneVectorElement(Vector128<byte> vector, const byte indexs) => UpdateOneVectorElement(vector, indexs);

        /// <summary>
        ///  int16x8_t vextrins_h(int16x8_t vec, uint8_t idx)
        ///   LSX: VEXTRINS.H Vd.8H, Vj.8H, ui8
        /// </summary>
        public static Vector128<short> UpdateOneVectorElement(Vector128<short> vector, const byte indexs) => UpdateOneVectorElement(vector, indexs);

        /// <summary>
        ///  uint16x8_t vextrins_h(uint16x8_t vec, uint8_t idx)
        ///   LSX: VEXTRINS.H Vd.8H, Vj.8H, ui8
        /// </summary>
        public static Vector128<ushort> UpdateOneVectorElement(Vector128<ushort> vector, const byte indexs) => UpdateOneVectorElement(vector, indexs);

        /// <summary>
        ///  int32x4_t vextrins_w(int32x4_t vec, uint8_t idx)
        ///   LSX: VEXTRINS.W Vd.4W, Vj.4W, ui8
        /// </summary>
        public static Vector128<int> UpdateOneVectorElement(Vector128<int> vector, const byte indexs) => UpdateOneVectorElement(vector, indexs);

        /// <summary>
        ///  uint32x4_t vextrins_w(uint32x4_t vec, uint8_t idx)
        ///   LSX: VEXTRINS.W Vd.4W, Vj.4W, ui8
        /// </summary>
        public static Vector128<uint> UpdateOneVectorElement(Vector128<uint> vector, const byte indexs) => UpdateOneVectorElement(vector, indexs);

        /// <summary>
        ///  int64x2_t vextrins_d(int64x2_t vec, uint8_t idx)
        ///   LSX: VEXTRINS.D Vd.2D, Vj.2D, ui8
        /// </summary>
        public static Vector128<long> UpdateOneVectorElement(Vector128<long> vector, const byte indexs) => UpdateOneVectorElement(vector, indexs);

        /// <summary>
        ///  uint64x2_t vextrins_d(uint64x2_t vec, uint8_t idx)
        ///   LSX: VEXTRINS.D Vd.2D, Vj.2D, ui8
        /// </summary>
        public static Vector128<ulong> UpdateOneVectorElement(Vector128<ulong> vector, const byte indexs) => UpdateOneVectorElement(vector, indexs);

        /// <summary>
        ///  int8x16_t vsigncov_b(int8x16_t sign, int8x16_t data)
        ///   LSX: VSIGNCOV.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> VectorElementNegatedBySign(Vector128<sbyte> sign, Vector128<sbyte> data) => VectorElementNegatedBySign(sign, data);

        /// <summary>
        ///  int16x8_t vsigncov_h(int16x8_t sign, int16x8_t data)
        ///   LSX: VSIGNCOV.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> VectorElementNegatedBySign(Vector128<short> sign, Vector128<short> data) => VectorElementNegatedBySign(sign, data);

        /// <summary>
        ///  int32x4_t vsigncov_w(int32x4_t sign, int32x4_t data)
        ///   LSX: VSIGNCOV.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<int> VectorElementNegatedBySign(Vector128<int> sign, Vector128<int> data) => VectorElementNegatedBySign(sign, data);

        /// <summary>
        ///  int64x2_t vsigncov_d(int64x2_t sign, int64x2_t data)
        ///   LSX: VSIGNCOV.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> VectorElementNegatedBySign(Vector128<long> sign, Vector128<long> data) => VectorElementNegatedBySign(sign, data);

        /// <summary>
        /// int8x16_t vbitclri_b(int8x16_t a, const int n)
        ///   LSX: VBITCLRI.B Vd.16B, Vj.16B, ui3
        /// </summary>
        public static Vector128<sbyte> VectorElementBitClear(Vector128<sbyte> value, const byte index) => VectorElementBitClear(value, index);

        /// <summary>
        /// uint8x16_t vbitclri_b(uint8x16_t a, const int n)
        ///   LSX: VBITCLRI.B Vd.16B, Vj.16B, ui3
        /// </summary>
        public static Vector128<byte> VectorElementBitClear(Vector128<byte> value, const byte index) => VectorElementBitClear(value, index);

        /// <summary>
        /// int16x8_t vbitclri_h(int16x8_t a, const int n)
        ///   LSX: VBITCLRI.H Vd.8H, Vj.8H, ui4
        /// </summary>
        public static Vector128<short> VectorElementBitClear(Vector128<short> value, const byte index) => VectorElementBitClear(value, index);

        /// <summary>
        /// uint16x8_t vbitclri_h(uint16x8_t a, const int n)
        ///   LSX: VBITCLRI.H Vd.8H, Vj.8H, ui4
        /// </summary>
        public static Vector128<ushort> VectorElementBitClear(Vector128<ushort> value, const byte index) => VectorElementBitClear(value, index);

        /// <summary>
        /// uint32x4_t vbitclri_w(uint32x4_t a, const int n)
        ///   LSX: VBITCLRI.W Vd.4W, Vj.4W, ui5
        /// </summary>
        public static Vector128<int> VectorElementBitClear(Vector128<int> value, const byte index) => VectorElementBitClear(value, index);

        /// <summary>
        /// uint32x4_t vbitclri_w(uint32x4_t a, const int n)
        ///   LSX: VBITCLRI.W Vd.4W, Vj.4W, ui5
        /// </summary>
        public static Vector128<uint> VectorElementBitClear(Vector128<uint> value, const byte index) => VectorElementBitClear(value, index);

        /// <summary>
        /// int64x2_t vbitclri_d(int64x2_t a, const int n)
        ///   LSX: VBITCLRI.D Vd.2D, Vj.2D, ui6
        /// </summary>
        public static Vector128<long> VectorElementBitClear(Vector128<long> value, const byte index) => VectorElementBitClear(value, index);

        /// <summary>
        /// uint64x2_t vbitclri_d(uint64x2_t a, const int n)
        ///   LSX: VBITCLRI.D Vd.2D, Vj.2D, ui6
        /// </summary>
        public static Vector128<ulong> VectorElementBitClear(Vector128<ulong> value, const byte index) => VectorElementBitClear(value, index);

        /// <summary>
        /// int8x16_t vbitclr_b(int8x16_t a, int8x16_t b)
        ///   LSX: VBITCLR.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> VectorElementBitClear(Vector128<sbyte> value, Vector128<sbyte> index) => VectorElementBitClear(value, index);

        /// <summary>
        /// uint8x16_t vbitclr_b(uint8x16_t a, uint8x16_t b)
        ///   LSX: VBITCLR.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> VectorElementBitClear(Vector128<byte> value, Vector128<byte> index) => VectorElementBitClear(value, index);

        /// <summary>
        /// int16x8_t vbitclr_h(int16x8_t value, int16x8_t index)
        ///   LSX: VBITCLR.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> VectorElementBitClear(Vector128<short> value, Vector128<short> index) => VectorElementBitClear(value, index);

        /// <summary>
        /// uint16x8_t vbitclr_h(uint16x8_t value, uint16x8_t index)
        ///   LSX: VBITCLR.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> VectorElementBitClear(Vector128<ushort> value, Vector128<ushort> index) => VectorElementBitClear(value, index);

        /// <summary>
        /// int32x4_t vbitclr_w(int32x4_t value, int32x4_t index)
        ///   LSX: VBITCLR.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<int> VectorElementBitClear(Vector128<int> value, Vector128<int> index) => VectorElementBitClear(value, index);

        /// <summary>
        /// uint32x4_t vbitclr_w(uint32x4_t value, uint32x4_t index)
        ///   LSX: VBITCLR.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<uint> VectorElementBitClear(Vector128<uint> value, Vector128<uint> index) => VectorElementBitClear(value, index);

        /// <summary>
        /// int64x2_t vbitclr_d(int64x2_t value, int64x2_t index)
        ///   LSX: VBITCLR.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> VectorElementBitClear(Vector128<long> value, Vector128<long> index) => VectorElementBitClear(value, index);

        /// <summary>
        /// uint64x2_t vbitclr_d(uint64x2_t value, uint64x2_t index)
        ///   LSX: VBITCLR.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> VectorElementBitClear(Vector128<ulong> value, Vector128<ulong> index) => VectorElementBitClear(value, index);

        /// <summary>
        /// int8x16_t vbitseti_b(int8x16_t a, const int n)
        ///   LSX: VBITSETI.B Vd.16B, Vj.16B, ui3
        /// </summary>
        public static Vector128<sbyte> VectorElementBitSet(Vector128<sbyte> value, const byte index) => VectorElementBitSet(value, index);

        /// <summary>
        /// uint8x16_t vbitseti_b(uint8x16_t a, const int n)
        ///   LSX: VBITSETI.B Vd.16B, Vj.16B, ui3
        /// </summary>
        public static Vector128<byte> VectorElementBitSet(Vector128<byte> value, const byte index) => VectorElementBitSet(value, index);

        /// <summary>
        /// int16x8_t vbitseti_h(int16x8_t a, const int n)
        ///   LSX: VBITSETI.H Vd.8H, Vj.8H, ui4
        /// </summary>
        public static Vector128<short> VectorElementBitSet(Vector128<short> value, const byte index) => VectorElementBitSet(value, index);

        /// <summary>
        /// uint16x8_t vbitseti_h(uint16x8_t a, const int n)
        ///   LSX: VBITSETI.H Vd.8H, Vj.8H, ui4
        /// </summary>
        public static Vector128<ushort> VectorElementBitSet(Vector128<ushort> value, const byte index) => VectorElementBitSet(value, index);

        /// <summary>
        /// uint32x4_t vbitseti_w(uint32x4_t a, const int n)
        ///   LSX: VBITSETI.W Vd.4W, Vj.4W, ui5
        /// </summary>
        public static Vector128<int> VectorElementBitSet(Vector128<int> value, const byte index) => VectorElementBitSet(value, index);

        /// <summary>
        /// uint32x4_t vbitseti_w(uint32x4_t a, const int n)
        ///   LSX: VBITSETI.W Vd.4W, Vj.4W, ui5
        /// </summary>
        public static Vector128<uint> VectorElementBitSet(Vector128<uint> value, const byte index) => VectorElementBitSet(value, index);

        /// <summary>
        /// int64x2_t vbitseti_d(int64x2_t a, const int n)
        ///   LSX: VBITSETI.D Vd.2D, Vj.2D, ui6
        /// </summary>
        public static Vector128<long> VectorElementBitSet(Vector128<long> value, const byte index) => VectorElementBitSet(value, index);

        /// <summary>
        /// uint64x2_t vbitseti_d(uint64x2_t a, const int n)
        ///   LSX: VBITSETI.D Vd.2D, Vj.2D, ui6
        /// </summary>
        public static Vector128<ulong> VectorElementBitSet(Vector128<ulong> value, const byte index) => VectorElementBitSet(value, index);

        /// <summary>
        /// int8x16_t vbitset_b(int8x16_t a, int8x16_t b)
        ///   LSX: VBITSET.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> VectorElementBitSet(Vector128<sbyte> value, Vector128<sbyte> index) => VectorElementBitSet(value, index);

        /// <summary>
        /// uint8x16_t vbitset_b(uint8x16_t a, uint8x16_t b)
        ///   LSX: VBITSET.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> VectorElementBitSet(Vector128<byte> value, Vector128<byte> index) => VectorElementBitSet(value, index);

        /// <summary>
        /// int16x8_t vbitset_h(int16x8_t value, int16x8_t index)
        ///   LSX: VBITSET.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> VectorElementBitSet(Vector128<short> value, Vector128<short> index) => VectorElementBitSet(value, index);

        /// <summary>
        /// uint16x8_t vbitset_h(uint16x8_t value, uint16x8_t index)
        ///   LSX: VBITSET.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> VectorElementBitSet(Vector128<ushort> value, Vector128<ushort> index) => VectorElementBitSet(value, index);

        /// <summary>
        /// int32x4_t vbitset_w(int32x4_t value, int32x4_t index)
        ///   LSX: VBITSET.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<int> VectorElementBitSet(Vector128<int> value, Vector128<int> index) => VectorElementBitSet(value, index);

        /// <summary>
        /// uint32x4_t vbitset_w(uint32x4_t value, uint32x4_t index)
        ///   LSX: VBITSET.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<uint> VectorElementBitSet(Vector128<uint> value, Vector128<uint> index) => VectorElementBitSet(value, index);

        /// <summary>
        /// int64x2_t vbitset_d(int64x2_t value, int64x2_t index)
        ///   LSX: VBITSET.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> VectorElementBitSet(Vector128<long> value, Vector128<long> index) => VectorElementBitSet(value, index);

        /// <summary>
        /// uint64x2_t vbitset_d(uint64x2_t value, uint64x2_t index)
        ///   LSX: VBITSET.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> VectorElementBitSet(Vector128<ulong> value, Vector128<ulong> index) => VectorElementBitSet(value, index);

        /// <summary>
        /// int8x16_t vbitrevi_b(int8x16_t a, const int n)
        ///   LSX: VBITREVI.B Vd.16B, Vj.16B, ui3
        /// </summary>
        public static Vector128<sbyte> VectorElementBitRevert(Vector128<sbyte> value, const byte index) => VectorElementBitRevert(value, index);

        /// <summary>
        /// uint8x16_t vbitrevi_b(uint8x16_t a, const int n)
        ///   LSX: VBITREVI.B Vd.16B, Vj.16B, ui3
        /// </summary>
        public static Vector128<byte> VectorElementBitRevert(Vector128<byte> value, const byte index) => VectorElementBitRevert(value, index);

        /// <summary>
        /// int16x8_t vbitrevi_h(int16x8_t a, const int n)
        ///   LSX: VBITREVI.H Vd.8H, Vj.8H, ui4
        /// </summary>
        public static Vector128<short> VectorElementBitRevert(Vector128<short> value, const byte index) => VectorElementBitRevert(value, index);

        /// <summary>
        /// uint16x8_t vbitrevi_h(uint16x8_t a, const int n)
        ///   LSX: VBITREVI.H Vd.8H, Vj.8H, ui4
        /// </summary>
        public static Vector128<ushort> VectorElementBitRevert(Vector128<ushort> value, const byte index) => VectorElementBitRevert(value, index);

        /// <summary>
        /// uint32x4_t vbitrevi_w(uint32x4_t a, const int n)
        ///   LSX: VBITREVI.W Vd.4W, Vj.4W, ui5
        /// </summary>
        public static Vector128<int> VectorElementBitRevert(Vector128<int> value, const byte index) => VectorElementBitRevert(value, index);

        /// <summary>
        /// uint32x4_t vbitrevi_w(uint32x4_t a, const int n)
        ///   LSX: VBITREVI.W Vd.4W, Vj.4W, ui5
        /// </summary>
        public static Vector128<uint> VectorElementBitRevert(Vector128<uint> value, const byte index) => VectorElementBitRevert(value, index);

        /// <summary>
        /// int64x2_t vbitrevi_d(int64x2_t a, const int n)
        ///   LSX: VBITREVI.D Vd.2D, Vj.2D, ui6
        /// </summary>
        public static Vector128<long> VectorElementBitRevert(Vector128<long> value, const byte index) => VectorElementBitRevert(value, index);

        /// <summary>
        /// uint64x2_t vbitrevi_d(uint64x2_t a, const int n)
        ///   LSX: VBITREVI.D Vd.2D, Vj.2D, ui6
        /// </summary>
        public static Vector128<ulong> VectorElementBitRevert(Vector128<ulong> value, const byte index) => VectorElementBitRevert(value, index);

        /// <summary>
        /// int8x16_t vbitrev_b(int8x16_t a, int8x16_t b)
        ///   LSX: VBITREV.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> VectorElementBitRevert(Vector128<sbyte> value, Vector128<sbyte> index) => VectorElementBitRevert(value, index);

        /// <summary>
        /// uint8x16_t vbitrev_b(uint8x16_t a, uint8x16_t b)
        ///   LSX: VBITREV.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> VectorElementBitRevert(Vector128<byte> value, Vector128<byte> index) => VectorElementBitRevert(value, index);

        /// <summary>
        /// int16x8_t vbitrev_h(int16x8_t value, int16x8_t index)
        ///   LSX: VBITREV.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> VectorElementBitRevert(Vector128<short> value, Vector128<short> index) => VectorElementBitRevert(value, index);

        /// <summary>
        /// uint16x8_t vbitrev_h(uint16x8_t value, uint16x8_t index)
        ///   LSX: VBITREV.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> VectorElementBitRevert(Vector128<ushort> value, Vector128<ushort> index) => VectorElementBitRevert(value, index);

        /// <summary>
        /// int32x4_t vbitrev_w(int32x4_t value, int32x4_t index)
        ///   LSX: VBITREV.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<int> VectorElementBitRevert(Vector128<int> value, Vector128<int> index) => VectorElementBitRevert(value, index);

        /// <summary>
        /// uint32x4_t vbitrev_w(uint32x4_t value, uint32x4_t index)
        ///   LSX: VBITREV.W Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<uint> VectorElementBitRevert(Vector128<uint> value, Vector128<uint> index) => VectorElementBitRevert(value, index);

        /// <summary>
        /// int64x2_t vbitrev_d(int64x2_t value, int64x2_t index)
        ///   LSX: VBITREV.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> VectorElementBitRevert(Vector128<long> value, Vector128<long> index) => VectorElementBitRevert(value, index);

        /// <summary>
        /// uint64x2_t vbitrev_d(uint64x2_t value, uint64x2_t index)
        ///   LSX: VBITREV.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> VectorElementBitRevert(Vector128<ulong> value, Vector128<ulong> index) => VectorElementBitRevert(value, index);

        /// <summary>
        /// int8x16_t vfrstp_b(int8x16_t value, int8x16_t save)
        ///   LSX: VFRSTP.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> IndexOfFirstNegativeElement(Vector128<sbyte> value, Vector128<sbyte> save) => IndexOfFirstNegativeElement(value, save);

        /// <summary>
        /// int16x8_t vfrstp_h(int16x8_t value, int16x8_t save)
        ///   LSX: VFRSTP.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> IndexOfFirstNegativeElement(Vector128<short> value, Vector128<short> save) => IndexOfFirstNegativeElement(value, save);

        /// <summary>
        /// int8x16_t vfrstpi_b(int8x16_t value, uint8_t save)
        ///   LSX: VFRSTPI.B Vd.16B, Vj.16B, ui4
        /// </summary>
        public static Vector128<sbyte> IndexOfFirstNegativeElement(Vector128<sbyte> value, const byte save) => IndexOfFirstNegativeElement(value, save);

        /// <summary>
        /// int16x8_t vfrstpi_h(int16x8_t value, uint8_t save)
        ///   LSX: VFRSTPI.H Vd.8H, Vj.8H, ui3
        /// </summary>
        public static Vector128<short> IndexOfFirstNegativeElement(Vector128<short> value, const byte save) => IndexOfFirstNegativeElement(value, save);

        /// <summary>
        /// int32x4_t vfclass_s(float32x4_t a)
        ///   LSX: VFCLASS.S Vd.4S, Vj.4S
        /// </summary>
        public static Vector128<int> FloatClass(Vector128<float> value) => FloatClass(value);

        /// <summary>
        /// int64x2_t vfclass_d(float64x2_t a)
        ///   LSX: VFCLASS.D Vd.2D, Vj.2D
        /// </summary>
        public static Vector128<long> FloatClass(Vector128<double> value) => FloatClass(value);
    }
}
