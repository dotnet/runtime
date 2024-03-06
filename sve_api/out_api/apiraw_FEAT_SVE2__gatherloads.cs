namespace System.Runtime.Intrinsics.Arm;

/// VectorT Summary
public abstract partial class Sve2 : AdvSimd /// Feature: FEAT_SVE2  Category: gatherloads
{

  /// T: [int, uint], [uint, uint], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorByteZeroExtendNonTemporal(Vector<T> mask, Vector<T2> addresses); // LDNT1B

  /// T: [int, uint], [uint, uint], [long, long], [ulong, long], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorByteZeroExtendNonTemporal(Vector<T> mask, byte* address, Vector<T2> offsets); // LDNT1B

  /// T: [int, uint], [uint, uint], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorInt16SignExtendNonTemporal(Vector<T> mask, Vector<T2> addresses); // LDNT1SH

  /// T: [long, long], [ulong, long], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorInt16SignExtendNonTemporal(Vector<T> mask, short* address, Vector<T2> indices); // LDNT1SH

  /// T: [int, uint], [uint, uint], [long, long], [ulong, long], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorInt16WithByteOffsetsSignExtendNonTemporal(Vector<T> mask, short* address, Vector<T2> offsets); // LDNT1SH

  /// T: [long, ulong], [int, uint], [ulong, ulong], [uint, uint]
  public static unsafe Vector<T> GatherVectorInt32SignExtendNonTemporal(Vector<T> mask, Vector<T2> addresses); // LDNT1SW

  /// T: [long, long], [int, int], [ulong, long], [uint, int], [long, ulong], [int, uint], [ulong, ulong], [uint, uint]
  public static unsafe Vector<T> GatherVectorInt32SignExtendNonTemporal(Vector<T> mask, int* address, Vector<T2> indices); // LDNT1SW

  /// T: [long, long], [int, int], [ulong, long], [uint, int], [long, ulong], [int, uint], [ulong, ulong], [uint, uint]
  public static unsafe Vector<T> GatherVectorInt32WithByteOffsetsSignExtendNonTemporal(Vector<T> mask, int* address, Vector<T2> offsets); // LDNT1SW

  /// T: [float, uint], [int, uint], [double, ulong], [long, ulong]
  public static unsafe Vector<T> GatherVectorNonTemporal(Vector<T> mask, Vector<T2> addresses); // LDNT1W or LDNT1D

  /// T: uint, ulong
  public static unsafe Vector<T> GatherVectorNonTemporal(Vector<T> mask, Vector<T> addresses); // LDNT1W or LDNT1D

  /// T: [float, uint], [int, uint], [double, long], [ulong, long], [double, ulong], [long, ulong]
  public static unsafe Vector<T> GatherVectorNonTemporal(Vector<T> mask, T* address, Vector<T2> offsets); // LDNT1W or LDNT1D

  /// T: uint, long, ulong
  public static unsafe Vector<T> GatherVectorNonTemporal(Vector<T> mask, T* address, Vector<T> offsets); // LDNT1W or LDNT1D

  /// T: [double, long], [ulong, long], [double, ulong], [long, ulong]
  public static unsafe Vector<T> GatherVectorNonTemporal(Vector<T> mask, T* address, Vector<T2> indices); // LDNT1D

  /// T: long, ulong
  public static unsafe Vector<T> GatherVectorNonTemporal(Vector<T> mask, T* address, Vector<T> indices); // LDNT1D

  /// T: [int, uint], [uint, uint], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorSByteSignExtendNonTemporal(Vector<T> mask, Vector<T2> addresses); // LDNT1SB

  /// T: [int, uint], [uint, uint], [long, long], [ulong, long], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorSByteSignExtendNonTemporal(Vector<T> mask, sbyte* address, Vector<T2> offsets); // LDNT1SB

  /// T: [int, uint], [uint, uint], [long, long], [ulong, long], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorUInt16WithByteOffsetsZeroExtendNonTemporal(Vector<T> mask, ushort* address, Vector<T2> offsets); // LDNT1H

  /// T: [int, uint], [uint, uint], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorUInt16ZeroExtendNonTemporal(Vector<T> mask, Vector<T2> addresses); // LDNT1H

  /// T: [long, long], [ulong, long], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorUInt16ZeroExtendNonTemporal(Vector<T> mask, ushort* address, Vector<T2> indices); // LDNT1H

  /// T: [long, long], [int, int], [ulong, long], [uint, int], [long, ulong], [int, uint], [ulong, ulong], [uint, uint]
  public static unsafe Vector<T> GatherVectorUInt32WithByteOffsetsZeroExtendNonTemporal(Vector<T> mask, uint* address, Vector<T2> offsets); // LDNT1W

  /// T: [long, ulong], [int, uint], [ulong, ulong], [uint, uint]
  public static unsafe Vector<T> GatherVectorUInt32ZeroExtendNonTemporal(Vector<T> mask, Vector<T2> addresses); // LDNT1W

  /// T: [long, long], [int, int], [ulong, long], [uint, int], [long, ulong], [int, uint], [ulong, ulong], [uint, uint]
  public static unsafe Vector<T> GatherVectorUInt32ZeroExtendNonTemporal(Vector<T> mask, uint* address, Vector<T2> indices); // LDNT1W

  /// total method signatures: 22

}


/// Full API
public abstract partial class Sve2 : AdvSimd /// Feature: FEAT_SVE2  Category: gatherloads
{
    /// GatherVectorByteZeroExtendNonTemporal : Load 8-bit data and zero-extend, non-temporal

    /// svint32_t svldnt1ub_gather[_u32base]_s32(svbool_t pg, svuint32_t bases) : "LDNT1B Zresult.S, Pg/Z, [Zbases.S, XZR]"
  public static unsafe Vector<int> GatherVectorByteZeroExtendNonTemporal(Vector<int> mask, Vector<uint> addresses);

    /// svuint32_t svldnt1ub_gather[_u32base]_u32(svbool_t pg, svuint32_t bases) : "LDNT1B Zresult.S, Pg/Z, [Zbases.S, XZR]"
  public static unsafe Vector<uint> GatherVectorByteZeroExtendNonTemporal(Vector<uint> mask, Vector<uint> addresses);

    /// svint64_t svldnt1ub_gather[_u64base]_s64(svbool_t pg, svuint64_t bases) : "LDNT1B Zresult.D, Pg/Z, [Zbases.D, XZR]"
  public static unsafe Vector<long> GatherVectorByteZeroExtendNonTemporal(Vector<long> mask, Vector<ulong> addresses);

    /// svuint64_t svldnt1ub_gather[_u64base]_u64(svbool_t pg, svuint64_t bases) : "LDNT1B Zresult.D, Pg/Z, [Zbases.D, XZR]"
  public static unsafe Vector<ulong> GatherVectorByteZeroExtendNonTemporal(Vector<ulong> mask, Vector<ulong> addresses);

    /// svint32_t svldnt1ub_gather_[u32]offset_s32(svbool_t pg, const uint8_t *base, svuint32_t offsets) : "LDNT1B Zresult.S, Pg/Z, [Zoffsets.S, Xbase]"
  public static unsafe Vector<int> GatherVectorByteZeroExtendNonTemporal(Vector<int> mask, byte* address, Vector<uint> offsets);

    /// svuint32_t svldnt1ub_gather_[u32]offset_u32(svbool_t pg, const uint8_t *base, svuint32_t offsets) : "LDNT1B Zresult.S, Pg/Z, [Zoffsets.S, Xbase]"
  public static unsafe Vector<uint> GatherVectorByteZeroExtendNonTemporal(Vector<uint> mask, byte* address, Vector<uint> offsets);

    /// svint64_t svldnt1ub_gather_[s64]offset_s64(svbool_t pg, const uint8_t *base, svint64_t offsets) : "LDNT1B Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<long> GatherVectorByteZeroExtendNonTemporal(Vector<long> mask, byte* address, Vector<long> offsets);

    /// svuint64_t svldnt1ub_gather_[s64]offset_u64(svbool_t pg, const uint8_t *base, svint64_t offsets) : "LDNT1B Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<ulong> GatherVectorByteZeroExtendNonTemporal(Vector<ulong> mask, byte* address, Vector<long> offsets);

    /// svint64_t svldnt1ub_gather_[u64]offset_s64(svbool_t pg, const uint8_t *base, svuint64_t offsets) : "LDNT1B Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<long> GatherVectorByteZeroExtendNonTemporal(Vector<long> mask, byte* address, Vector<ulong> offsets);

    /// svuint64_t svldnt1ub_gather_[u64]offset_u64(svbool_t pg, const uint8_t *base, svuint64_t offsets) : "LDNT1B Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<ulong> GatherVectorByteZeroExtendNonTemporal(Vector<ulong> mask, byte* address, Vector<ulong> offsets);


    /// GatherVectorInt16SignExtendNonTemporal : Load 16-bit data and sign-extend, non-temporal

    /// svint32_t svldnt1sh_gather[_u32base]_s32(svbool_t pg, svuint32_t bases) : "LDNT1SH Zresult.S, Pg/Z, [Zbases.S, XZR]"
  public static unsafe Vector<int> GatherVectorInt16SignExtendNonTemporal(Vector<int> mask, Vector<uint> addresses);

    /// svuint32_t svldnt1sh_gather[_u32base]_u32(svbool_t pg, svuint32_t bases) : "LDNT1SH Zresult.S, Pg/Z, [Zbases.S, XZR]"
  public static unsafe Vector<uint> GatherVectorInt16SignExtendNonTemporal(Vector<uint> mask, Vector<uint> addresses);

    /// svint64_t svldnt1sh_gather[_u64base]_s64(svbool_t pg, svuint64_t bases) : "LDNT1SH Zresult.D, Pg/Z, [Zbases.D, XZR]"
  public static unsafe Vector<long> GatherVectorInt16SignExtendNonTemporal(Vector<long> mask, Vector<ulong> addresses);

    /// svuint64_t svldnt1sh_gather[_u64base]_u64(svbool_t pg, svuint64_t bases) : "LDNT1SH Zresult.D, Pg/Z, [Zbases.D, XZR]"
  public static unsafe Vector<ulong> GatherVectorInt16SignExtendNonTemporal(Vector<ulong> mask, Vector<ulong> addresses);

    /// svint64_t svldnt1sh_gather_[s64]index_s64(svbool_t pg, const int16_t *base, svint64_t indices) : "LDNT1SH Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<long> GatherVectorInt16SignExtendNonTemporal(Vector<long> mask, short* address, Vector<long> indices);

    /// svuint64_t svldnt1sh_gather_[s64]index_u64(svbool_t pg, const int16_t *base, svint64_t indices) : "LDNT1SH Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<ulong> GatherVectorInt16SignExtendNonTemporal(Vector<ulong> mask, short* address, Vector<long> indices);

    /// svint64_t svldnt1sh_gather_[u64]index_s64(svbool_t pg, const int16_t *base, svuint64_t indices) : "LDNT1SH Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<long> GatherVectorInt16SignExtendNonTemporal(Vector<long> mask, short* address, Vector<ulong> indices);

    /// svuint64_t svldnt1sh_gather_[u64]index_u64(svbool_t pg, const int16_t *base, svuint64_t indices) : "LDNT1SH Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<ulong> GatherVectorInt16SignExtendNonTemporal(Vector<ulong> mask, short* address, Vector<ulong> indices);


    /// GatherVectorInt16WithByteOffsetsSignExtendNonTemporal : Load 16-bit data and sign-extend, non-temporal

    /// svint32_t svldnt1sh_gather_[u32]offset_s32(svbool_t pg, const int16_t *base, svuint32_t offsets) : "LDNT1SH Zresult.S, Pg/Z, [Zoffsets.S, Xbase]"
  public static unsafe Vector<int> GatherVectorInt16WithByteOffsetsSignExtendNonTemporal(Vector<int> mask, short* address, Vector<uint> offsets);

    /// svuint32_t svldnt1sh_gather_[u32]offset_u32(svbool_t pg, const int16_t *base, svuint32_t offsets) : "LDNT1SH Zresult.S, Pg/Z, [Zoffsets.S, Xbase]"
  public static unsafe Vector<uint> GatherVectorInt16WithByteOffsetsSignExtendNonTemporal(Vector<uint> mask, short* address, Vector<uint> offsets);

    /// svint64_t svldnt1sh_gather_[s64]offset_s64(svbool_t pg, const int16_t *base, svint64_t offsets) : "LDNT1SH Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<long> GatherVectorInt16WithByteOffsetsSignExtendNonTemporal(Vector<long> mask, short* address, Vector<long> offsets);

    /// svuint64_t svldnt1sh_gather_[s64]offset_u64(svbool_t pg, const int16_t *base, svint64_t offsets) : "LDNT1SH Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<ulong> GatherVectorInt16WithByteOffsetsSignExtendNonTemporal(Vector<ulong> mask, short* address, Vector<long> offsets);

    /// svint64_t svldnt1sh_gather_[u64]offset_s64(svbool_t pg, const int16_t *base, svuint64_t offsets) : "LDNT1SH Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<long> GatherVectorInt16WithByteOffsetsSignExtendNonTemporal(Vector<long> mask, short* address, Vector<ulong> offsets);

    /// svuint64_t svldnt1sh_gather_[u64]offset_u64(svbool_t pg, const int16_t *base, svuint64_t offsets) : "LDNT1SH Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<ulong> GatherVectorInt16WithByteOffsetsSignExtendNonTemporal(Vector<ulong> mask, short* address, Vector<ulong> offsets);


    /// GatherVectorInt32SignExtendNonTemporal : Load 32-bit data and sign-extend, non-temporal

    /// svint64_t svldnt1sw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases) : "LDNT1SW Zresult.D, Pg/Z, [Zbases.D, XZR]"
  public static unsafe Vector<long> GatherVectorInt32SignExtendNonTemporal(Vector<long> mask, Vector<ulong> addresses);

    /// svint64_t svldnt1sw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases) : "LDNT1SW Zresult.D, Pg/Z, [Zbases.D, XZR]"
  public static unsafe Vector<int> GatherVectorInt32SignExtendNonTemporal(Vector<int> mask, Vector<uint> addresses);

    /// svuint64_t svldnt1sw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases) : "LDNT1SW Zresult.D, Pg/Z, [Zbases.D, XZR]"
  public static unsafe Vector<ulong> GatherVectorInt32SignExtendNonTemporal(Vector<ulong> mask, Vector<ulong> addresses);

    /// svuint64_t svldnt1sw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases) : "LDNT1SW Zresult.D, Pg/Z, [Zbases.D, XZR]"
  public static unsafe Vector<uint> GatherVectorInt32SignExtendNonTemporal(Vector<uint> mask, Vector<uint> addresses);

    /// svint64_t svldnt1sw_gather_[s64]index_s64(svbool_t pg, const int32_t *base, svint64_t indices) : "LDNT1SW Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<long> GatherVectorInt32SignExtendNonTemporal(Vector<long> mask, int* address, Vector<long> indices);

    /// svint64_t svldnt1sw_gather_[s64]index_s64(svbool_t pg, const int32_t *base, svint64_t indices) : "LDNT1SW Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<int> GatherVectorInt32SignExtendNonTemporal(Vector<int> mask, int* address, Vector<int> indices);

    /// svuint64_t svldnt1sw_gather_[s64]index_u64(svbool_t pg, const int32_t *base, svint64_t indices) : "LDNT1SW Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<ulong> GatherVectorInt32SignExtendNonTemporal(Vector<ulong> mask, int* address, Vector<long> indices);

    /// svuint64_t svldnt1sw_gather_[s64]index_u64(svbool_t pg, const int32_t *base, svint64_t indices) : "LDNT1SW Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<uint> GatherVectorInt32SignExtendNonTemporal(Vector<uint> mask, int* address, Vector<int> indices);

    /// svint64_t svldnt1sw_gather_[u64]index_s64(svbool_t pg, const int32_t *base, svuint64_t indices) : "LDNT1SW Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<long> GatherVectorInt32SignExtendNonTemporal(Vector<long> mask, int* address, Vector<ulong> indices);

    /// svint64_t svldnt1sw_gather_[u64]index_s64(svbool_t pg, const int32_t *base, svuint64_t indices) : "LDNT1SW Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<int> GatherVectorInt32SignExtendNonTemporal(Vector<int> mask, int* address, Vector<uint> indices);

    /// svuint64_t svldnt1sw_gather_[u64]index_u64(svbool_t pg, const int32_t *base, svuint64_t indices) : "LDNT1SW Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<ulong> GatherVectorInt32SignExtendNonTemporal(Vector<ulong> mask, int* address, Vector<ulong> indices);

    /// svuint64_t svldnt1sw_gather_[u64]index_u64(svbool_t pg, const int32_t *base, svuint64_t indices) : "LDNT1SW Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<uint> GatherVectorInt32SignExtendNonTemporal(Vector<uint> mask, int* address, Vector<uint> indices);


    /// GatherVectorInt32WithByteOffsetsSignExtendNonTemporal : Load 32-bit data and sign-extend, non-temporal

    /// svint64_t svldnt1sw_gather_[s64]offset_s64(svbool_t pg, const int32_t *base, svint64_t offsets) : "LDNT1SW Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<long> GatherVectorInt32WithByteOffsetsSignExtendNonTemporal(Vector<long> mask, int* address, Vector<long> offsets);

    /// svint64_t svldnt1sw_gather_[s64]offset_s64(svbool_t pg, const int32_t *base, svint64_t offsets) : "LDNT1SW Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<int> GatherVectorInt32WithByteOffsetsSignExtendNonTemporal(Vector<int> mask, int* address, Vector<int> offsets);

    /// svuint64_t svldnt1sw_gather_[s64]offset_u64(svbool_t pg, const int32_t *base, svint64_t offsets) : "LDNT1SW Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<ulong> GatherVectorInt32WithByteOffsetsSignExtendNonTemporal(Vector<ulong> mask, int* address, Vector<long> offsets);

    /// svuint64_t svldnt1sw_gather_[s64]offset_u64(svbool_t pg, const int32_t *base, svint64_t offsets) : "LDNT1SW Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<uint> GatherVectorInt32WithByteOffsetsSignExtendNonTemporal(Vector<uint> mask, int* address, Vector<int> offsets);

    /// svint64_t svldnt1sw_gather_[u64]offset_s64(svbool_t pg, const int32_t *base, svuint64_t offsets) : "LDNT1SW Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<long> GatherVectorInt32WithByteOffsetsSignExtendNonTemporal(Vector<long> mask, int* address, Vector<ulong> offsets);

    /// svint64_t svldnt1sw_gather_[u64]offset_s64(svbool_t pg, const int32_t *base, svuint64_t offsets) : "LDNT1SW Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<int> GatherVectorInt32WithByteOffsetsSignExtendNonTemporal(Vector<int> mask, int* address, Vector<uint> offsets);

    /// svuint64_t svldnt1sw_gather_[u64]offset_u64(svbool_t pg, const int32_t *base, svuint64_t offsets) : "LDNT1SW Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<ulong> GatherVectorInt32WithByteOffsetsSignExtendNonTemporal(Vector<ulong> mask, int* address, Vector<ulong> offsets);

    /// svuint64_t svldnt1sw_gather_[u64]offset_u64(svbool_t pg, const int32_t *base, svuint64_t offsets) : "LDNT1SW Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<uint> GatherVectorInt32WithByteOffsetsSignExtendNonTemporal(Vector<uint> mask, int* address, Vector<uint> offsets);


    /// GatherVectorNonTemporal : Unextended load, non-temporal

    /// svfloat32_t svldnt1_gather[_u32base]_f32(svbool_t pg, svuint32_t bases) : "LDNT1W Zresult.S, Pg/Z, [Zbases.S, XZR]"
  public static unsafe Vector<float> GatherVectorNonTemporal(Vector<float> mask, Vector<uint> addresses);

    /// svint32_t svldnt1_gather[_u32base]_s32(svbool_t pg, svuint32_t bases) : "LDNT1W Zresult.S, Pg/Z, [Zbases.S, XZR]"
  public static unsafe Vector<int> GatherVectorNonTemporal(Vector<int> mask, Vector<uint> addresses);

    /// svuint32_t svldnt1_gather[_u32base]_u32(svbool_t pg, svuint32_t bases) : "LDNT1W Zresult.S, Pg/Z, [Zbases.S, XZR]"
  public static unsafe Vector<uint> GatherVectorNonTemporal(Vector<uint> mask, Vector<uint> addresses);

    /// svfloat64_t svldnt1_gather[_u64base]_f64(svbool_t pg, svuint64_t bases) : "LDNT1D Zresult.D, Pg/Z, [Zbases.D, XZR]"
  public static unsafe Vector<double> GatherVectorNonTemporal(Vector<double> mask, Vector<ulong> addresses);

    /// svint64_t svldnt1_gather[_u64base]_s64(svbool_t pg, svuint64_t bases) : "LDNT1D Zresult.D, Pg/Z, [Zbases.D, XZR]"
  public static unsafe Vector<long> GatherVectorNonTemporal(Vector<long> mask, Vector<ulong> addresses);

    /// svuint64_t svldnt1_gather[_u64base]_u64(svbool_t pg, svuint64_t bases) : "LDNT1D Zresult.D, Pg/Z, [Zbases.D, XZR]"
  public static unsafe Vector<ulong> GatherVectorNonTemporal(Vector<ulong> mask, Vector<ulong> addresses);

    /// svfloat32_t svldnt1_gather_[u32]offset[_f32](svbool_t pg, const float32_t *base, svuint32_t offsets) : "LDNT1W Zresult.S, Pg/Z, [Zoffsets.S, Xbase]"
  public static unsafe Vector<float> GatherVectorNonTemporal(Vector<float> mask, float* address, Vector<uint> offsets);

    /// svint32_t svldnt1_gather_[u32]offset[_s32](svbool_t pg, const int32_t *base, svuint32_t offsets) : "LDNT1W Zresult.S, Pg/Z, [Zoffsets.S, Xbase]"
  public static unsafe Vector<int> GatherVectorNonTemporal(Vector<int> mask, int* address, Vector<uint> offsets);

    /// svuint32_t svldnt1_gather_[u32]offset[_u32](svbool_t pg, const uint32_t *base, svuint32_t offsets) : "LDNT1W Zresult.S, Pg/Z, [Zoffsets.S, Xbase]"
  public static unsafe Vector<uint> GatherVectorNonTemporal(Vector<uint> mask, uint* address, Vector<uint> offsets);

    /// svfloat64_t svldnt1_gather_[s64]offset[_f64](svbool_t pg, const float64_t *base, svint64_t offsets) : "LDNT1D Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<double> GatherVectorNonTemporal(Vector<double> mask, double* address, Vector<long> offsets);

    /// svint64_t svldnt1_gather_[s64]offset[_s64](svbool_t pg, const int64_t *base, svint64_t offsets) : "LDNT1D Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<long> GatherVectorNonTemporal(Vector<long> mask, long* address, Vector<long> offsets);

    /// svuint64_t svldnt1_gather_[s64]offset[_u64](svbool_t pg, const uint64_t *base, svint64_t offsets) : "LDNT1D Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<ulong> GatherVectorNonTemporal(Vector<ulong> mask, ulong* address, Vector<long> offsets);

    /// svfloat64_t svldnt1_gather_[u64]offset[_f64](svbool_t pg, const float64_t *base, svuint64_t offsets) : "LDNT1D Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<double> GatherVectorNonTemporal(Vector<double> mask, double* address, Vector<ulong> offsets);

    /// svint64_t svldnt1_gather_[u64]offset[_s64](svbool_t pg, const int64_t *base, svuint64_t offsets) : "LDNT1D Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<long> GatherVectorNonTemporal(Vector<long> mask, long* address, Vector<ulong> offsets);

    /// svuint64_t svldnt1_gather_[u64]offset[_u64](svbool_t pg, const uint64_t *base, svuint64_t offsets) : "LDNT1D Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<ulong> GatherVectorNonTemporal(Vector<ulong> mask, ulong* address, Vector<ulong> offsets);

    /// svfloat64_t svldnt1_gather_[s64]index[_f64](svbool_t pg, const float64_t *base, svint64_t indices) : "LDNT1D Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<double> GatherVectorNonTemporal(Vector<double> mask, double* address, Vector<long> indices);

    /// svint64_t svldnt1_gather_[s64]index[_s64](svbool_t pg, const int64_t *base, svint64_t indices) : "LDNT1D Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<long> GatherVectorNonTemporal(Vector<long> mask, long* address, Vector<long> indices);

    /// svuint64_t svldnt1_gather_[s64]index[_u64](svbool_t pg, const uint64_t *base, svint64_t indices) : "LDNT1D Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<ulong> GatherVectorNonTemporal(Vector<ulong> mask, ulong* address, Vector<long> indices);

    /// svfloat64_t svldnt1_gather_[u64]index[_f64](svbool_t pg, const float64_t *base, svuint64_t indices) : "LDNT1D Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<double> GatherVectorNonTemporal(Vector<double> mask, double* address, Vector<ulong> indices);

    /// svint64_t svldnt1_gather_[u64]index[_s64](svbool_t pg, const int64_t *base, svuint64_t indices) : "LDNT1D Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<long> GatherVectorNonTemporal(Vector<long> mask, long* address, Vector<ulong> indices);

    /// svuint64_t svldnt1_gather_[u64]index[_u64](svbool_t pg, const uint64_t *base, svuint64_t indices) : "LDNT1D Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<ulong> GatherVectorNonTemporal(Vector<ulong> mask, ulong* address, Vector<ulong> indices);


    /// GatherVectorSByteSignExtendNonTemporal : Load 8-bit data and sign-extend, non-temporal

    /// svint32_t svldnt1sb_gather[_u32base]_s32(svbool_t pg, svuint32_t bases) : "LDNT1SB Zresult.S, Pg/Z, [Zbases.S, XZR]"
  public static unsafe Vector<int> GatherVectorSByteSignExtendNonTemporal(Vector<int> mask, Vector<uint> addresses);

    /// svuint32_t svldnt1sb_gather[_u32base]_u32(svbool_t pg, svuint32_t bases) : "LDNT1SB Zresult.S, Pg/Z, [Zbases.S, XZR]"
  public static unsafe Vector<uint> GatherVectorSByteSignExtendNonTemporal(Vector<uint> mask, Vector<uint> addresses);

    /// svint64_t svldnt1sb_gather[_u64base]_s64(svbool_t pg, svuint64_t bases) : "LDNT1SB Zresult.D, Pg/Z, [Zbases.D, XZR]"
  public static unsafe Vector<long> GatherVectorSByteSignExtendNonTemporal(Vector<long> mask, Vector<ulong> addresses);

    /// svuint64_t svldnt1sb_gather[_u64base]_u64(svbool_t pg, svuint64_t bases) : "LDNT1SB Zresult.D, Pg/Z, [Zbases.D, XZR]"
  public static unsafe Vector<ulong> GatherVectorSByteSignExtendNonTemporal(Vector<ulong> mask, Vector<ulong> addresses);

    /// svint32_t svldnt1sb_gather_[u32]offset_s32(svbool_t pg, const int8_t *base, svuint32_t offsets) : "LDNT1SB Zresult.S, Pg/Z, [Zoffsets.S, Xbase]"
  public static unsafe Vector<int> GatherVectorSByteSignExtendNonTemporal(Vector<int> mask, sbyte* address, Vector<uint> offsets);

    /// svuint32_t svldnt1sb_gather_[u32]offset_u32(svbool_t pg, const int8_t *base, svuint32_t offsets) : "LDNT1SB Zresult.S, Pg/Z, [Zoffsets.S, Xbase]"
  public static unsafe Vector<uint> GatherVectorSByteSignExtendNonTemporal(Vector<uint> mask, sbyte* address, Vector<uint> offsets);

    /// svint64_t svldnt1sb_gather_[s64]offset_s64(svbool_t pg, const int8_t *base, svint64_t offsets) : "LDNT1SB Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<long> GatherVectorSByteSignExtendNonTemporal(Vector<long> mask, sbyte* address, Vector<long> offsets);

    /// svuint64_t svldnt1sb_gather_[s64]offset_u64(svbool_t pg, const int8_t *base, svint64_t offsets) : "LDNT1SB Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<ulong> GatherVectorSByteSignExtendNonTemporal(Vector<ulong> mask, sbyte* address, Vector<long> offsets);

    /// svint64_t svldnt1sb_gather_[u64]offset_s64(svbool_t pg, const int8_t *base, svuint64_t offsets) : "LDNT1SB Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<long> GatherVectorSByteSignExtendNonTemporal(Vector<long> mask, sbyte* address, Vector<ulong> offsets);

    /// svuint64_t svldnt1sb_gather_[u64]offset_u64(svbool_t pg, const int8_t *base, svuint64_t offsets) : "LDNT1SB Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<ulong> GatherVectorSByteSignExtendNonTemporal(Vector<ulong> mask, sbyte* address, Vector<ulong> offsets);


    /// GatherVectorUInt16WithByteOffsetsZeroExtendNonTemporal : Load 16-bit data and zero-extend, non-temporal

    /// svint32_t svldnt1uh_gather_[u32]offset_s32(svbool_t pg, const uint16_t *base, svuint32_t offsets) : "LDNT1H Zresult.S, Pg/Z, [Zoffsets.S, Xbase]"
  public static unsafe Vector<int> GatherVectorUInt16WithByteOffsetsZeroExtendNonTemporal(Vector<int> mask, ushort* address, Vector<uint> offsets);

    /// svuint32_t svldnt1uh_gather_[u32]offset_u32(svbool_t pg, const uint16_t *base, svuint32_t offsets) : "LDNT1H Zresult.S, Pg/Z, [Zoffsets.S, Xbase]"
  public static unsafe Vector<uint> GatherVectorUInt16WithByteOffsetsZeroExtendNonTemporal(Vector<uint> mask, ushort* address, Vector<uint> offsets);

    /// svint64_t svldnt1uh_gather_[s64]offset_s64(svbool_t pg, const uint16_t *base, svint64_t offsets) : "LDNT1H Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<long> GatherVectorUInt16WithByteOffsetsZeroExtendNonTemporal(Vector<long> mask, ushort* address, Vector<long> offsets);

    /// svuint64_t svldnt1uh_gather_[s64]offset_u64(svbool_t pg, const uint16_t *base, svint64_t offsets) : "LDNT1H Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<ulong> GatherVectorUInt16WithByteOffsetsZeroExtendNonTemporal(Vector<ulong> mask, ushort* address, Vector<long> offsets);

    /// svint64_t svldnt1uh_gather_[u64]offset_s64(svbool_t pg, const uint16_t *base, svuint64_t offsets) : "LDNT1H Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<long> GatherVectorUInt16WithByteOffsetsZeroExtendNonTemporal(Vector<long> mask, ushort* address, Vector<ulong> offsets);

    /// svuint64_t svldnt1uh_gather_[u64]offset_u64(svbool_t pg, const uint16_t *base, svuint64_t offsets) : "LDNT1H Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<ulong> GatherVectorUInt16WithByteOffsetsZeroExtendNonTemporal(Vector<ulong> mask, ushort* address, Vector<ulong> offsets);


    /// GatherVectorUInt16ZeroExtendNonTemporal : Load 16-bit data and zero-extend, non-temporal

    /// svint32_t svldnt1uh_gather[_u32base]_s32(svbool_t pg, svuint32_t bases) : "LDNT1H Zresult.S, Pg/Z, [Zbases.S, XZR]"
  public static unsafe Vector<int> GatherVectorUInt16ZeroExtendNonTemporal(Vector<int> mask, Vector<uint> addresses);

    /// svuint32_t svldnt1uh_gather[_u32base]_u32(svbool_t pg, svuint32_t bases) : "LDNT1H Zresult.S, Pg/Z, [Zbases.S, XZR]"
  public static unsafe Vector<uint> GatherVectorUInt16ZeroExtendNonTemporal(Vector<uint> mask, Vector<uint> addresses);

    /// svint64_t svldnt1uh_gather[_u64base]_s64(svbool_t pg, svuint64_t bases) : "LDNT1H Zresult.D, Pg/Z, [Zbases.D, XZR]"
  public static unsafe Vector<long> GatherVectorUInt16ZeroExtendNonTemporal(Vector<long> mask, Vector<ulong> addresses);

    /// svuint64_t svldnt1uh_gather[_u64base]_u64(svbool_t pg, svuint64_t bases) : "LDNT1H Zresult.D, Pg/Z, [Zbases.D, XZR]"
  public static unsafe Vector<ulong> GatherVectorUInt16ZeroExtendNonTemporal(Vector<ulong> mask, Vector<ulong> addresses);

    /// svint64_t svldnt1uh_gather_[s64]index_s64(svbool_t pg, const uint16_t *base, svint64_t indices) : "LDNT1H Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<long> GatherVectorUInt16ZeroExtendNonTemporal(Vector<long> mask, ushort* address, Vector<long> indices);

    /// svuint64_t svldnt1uh_gather_[s64]index_u64(svbool_t pg, const uint16_t *base, svint64_t indices) : "LDNT1H Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<ulong> GatherVectorUInt16ZeroExtendNonTemporal(Vector<ulong> mask, ushort* address, Vector<long> indices);

    /// svint64_t svldnt1uh_gather_[u64]index_s64(svbool_t pg, const uint16_t *base, svuint64_t indices) : "LDNT1H Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<long> GatherVectorUInt16ZeroExtendNonTemporal(Vector<long> mask, ushort* address, Vector<ulong> indices);

    /// svuint64_t svldnt1uh_gather_[u64]index_u64(svbool_t pg, const uint16_t *base, svuint64_t indices) : "LDNT1H Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<ulong> GatherVectorUInt16ZeroExtendNonTemporal(Vector<ulong> mask, ushort* address, Vector<ulong> indices);


    /// GatherVectorUInt32WithByteOffsetsZeroExtendNonTemporal : Load 32-bit data and zero-extend, non-temporal

    /// svint64_t svldnt1uw_gather_[s64]offset_s64(svbool_t pg, const uint32_t *base, svint64_t offsets) : "LDNT1W Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<long> GatherVectorUInt32WithByteOffsetsZeroExtendNonTemporal(Vector<long> mask, uint* address, Vector<long> offsets);

    /// svint64_t svldnt1uw_gather_[s64]offset_s64(svbool_t pg, const uint32_t *base, svint64_t offsets) : "LDNT1W Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<int> GatherVectorUInt32WithByteOffsetsZeroExtendNonTemporal(Vector<int> mask, uint* address, Vector<int> offsets);

    /// svuint64_t svldnt1uw_gather_[s64]offset_u64(svbool_t pg, const uint32_t *base, svint64_t offsets) : "LDNT1W Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<ulong> GatherVectorUInt32WithByteOffsetsZeroExtendNonTemporal(Vector<ulong> mask, uint* address, Vector<long> offsets);

    /// svuint64_t svldnt1uw_gather_[s64]offset_u64(svbool_t pg, const uint32_t *base, svint64_t offsets) : "LDNT1W Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<uint> GatherVectorUInt32WithByteOffsetsZeroExtendNonTemporal(Vector<uint> mask, uint* address, Vector<int> offsets);

    /// svint64_t svldnt1uw_gather_[u64]offset_s64(svbool_t pg, const uint32_t *base, svuint64_t offsets) : "LDNT1W Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<long> GatherVectorUInt32WithByteOffsetsZeroExtendNonTemporal(Vector<long> mask, uint* address, Vector<ulong> offsets);

    /// svint64_t svldnt1uw_gather_[u64]offset_s64(svbool_t pg, const uint32_t *base, svuint64_t offsets) : "LDNT1W Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<int> GatherVectorUInt32WithByteOffsetsZeroExtendNonTemporal(Vector<int> mask, uint* address, Vector<uint> offsets);

    /// svuint64_t svldnt1uw_gather_[u64]offset_u64(svbool_t pg, const uint32_t *base, svuint64_t offsets) : "LDNT1W Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<ulong> GatherVectorUInt32WithByteOffsetsZeroExtendNonTemporal(Vector<ulong> mask, uint* address, Vector<ulong> offsets);

    /// svuint64_t svldnt1uw_gather_[u64]offset_u64(svbool_t pg, const uint32_t *base, svuint64_t offsets) : "LDNT1W Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<uint> GatherVectorUInt32WithByteOffsetsZeroExtendNonTemporal(Vector<uint> mask, uint* address, Vector<uint> offsets);


    /// GatherVectorUInt32ZeroExtendNonTemporal : Load 32-bit data and zero-extend, non-temporal

    /// svint64_t svldnt1uw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases) : "LDNT1W Zresult.D, Pg/Z, [Zbases.D, XZR]"
  public static unsafe Vector<long> GatherVectorUInt32ZeroExtendNonTemporal(Vector<long> mask, Vector<ulong> addresses);

    /// svint64_t svldnt1uw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases) : "LDNT1W Zresult.D, Pg/Z, [Zbases.D, XZR]"
  public static unsafe Vector<int> GatherVectorUInt32ZeroExtendNonTemporal(Vector<int> mask, Vector<uint> addresses);

    /// svuint64_t svldnt1uw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases) : "LDNT1W Zresult.D, Pg/Z, [Zbases.D, XZR]"
  public static unsafe Vector<ulong> GatherVectorUInt32ZeroExtendNonTemporal(Vector<ulong> mask, Vector<ulong> addresses);

    /// svuint64_t svldnt1uw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases) : "LDNT1W Zresult.D, Pg/Z, [Zbases.D, XZR]"
  public static unsafe Vector<uint> GatherVectorUInt32ZeroExtendNonTemporal(Vector<uint> mask, Vector<uint> addresses);

    /// svint64_t svldnt1uw_gather_[s64]index_s64(svbool_t pg, const uint32_t *base, svint64_t indices) : "LDNT1W Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<long> GatherVectorUInt32ZeroExtendNonTemporal(Vector<long> mask, uint* address, Vector<long> indices);

    /// svint64_t svldnt1uw_gather_[s64]index_s64(svbool_t pg, const uint32_t *base, svint64_t indices) : "LDNT1W Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<int> GatherVectorUInt32ZeroExtendNonTemporal(Vector<int> mask, uint* address, Vector<int> indices);

    /// svuint64_t svldnt1uw_gather_[s64]index_u64(svbool_t pg, const uint32_t *base, svint64_t indices) : "LDNT1W Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<ulong> GatherVectorUInt32ZeroExtendNonTemporal(Vector<ulong> mask, uint* address, Vector<long> indices);

    /// svuint64_t svldnt1uw_gather_[s64]index_u64(svbool_t pg, const uint32_t *base, svint64_t indices) : "LDNT1W Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<uint> GatherVectorUInt32ZeroExtendNonTemporal(Vector<uint> mask, uint* address, Vector<int> indices);

    /// svint64_t svldnt1uw_gather_[u64]index_s64(svbool_t pg, const uint32_t *base, svuint64_t indices) : "LDNT1W Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<long> GatherVectorUInt32ZeroExtendNonTemporal(Vector<long> mask, uint* address, Vector<ulong> indices);

    /// svint64_t svldnt1uw_gather_[u64]index_s64(svbool_t pg, const uint32_t *base, svuint64_t indices) : "LDNT1W Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<int> GatherVectorUInt32ZeroExtendNonTemporal(Vector<int> mask, uint* address, Vector<uint> indices);

    /// svuint64_t svldnt1uw_gather_[u64]index_u64(svbool_t pg, const uint32_t *base, svuint64_t indices) : "LDNT1W Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<ulong> GatherVectorUInt32ZeroExtendNonTemporal(Vector<ulong> mask, uint* address, Vector<ulong> indices);

    /// svuint64_t svldnt1uw_gather_[u64]index_u64(svbool_t pg, const uint32_t *base, svuint64_t indices) : "LDNT1W Zresult.D, Pg/Z, [Zoffsets.D, Xbase]"
  public static unsafe Vector<uint> GatherVectorUInt32ZeroExtendNonTemporal(Vector<uint> mask, uint* address, Vector<uint> indices);


  /// total method signatures: 109
  /// total method names:      11
}


  /// Rejected:
  ///   public static unsafe Vector<int> GatherVectorByteZeroExtendNonTemporal(Vector<int> mask, Vector<uint> addresses, long offset); // svldnt1ub_gather[_u32base]_offset_s32
  ///   public static unsafe Vector<uint> GatherVectorByteZeroExtendNonTemporal(Vector<uint> mask, Vector<uint> addresses, long offset); // svldnt1ub_gather[_u32base]_offset_u32
  ///   public static unsafe Vector<long> GatherVectorByteZeroExtendNonTemporal(Vector<long> mask, Vector<ulong> addresses, long offset); // svldnt1ub_gather[_u64base]_offset_s64
  ///   public static unsafe Vector<ulong> GatherVectorByteZeroExtendNonTemporal(Vector<ulong> mask, Vector<ulong> addresses, long offset); // svldnt1ub_gather[_u64base]_offset_u64
  ///   public static unsafe Vector<int> GatherVectorInt16SignExtendNonTemporal(Vector<int> mask, Vector<uint> addresses, long index); // svldnt1sh_gather[_u32base]_index_s32
  ///   public static unsafe Vector<uint> GatherVectorInt16SignExtendNonTemporal(Vector<uint> mask, Vector<uint> addresses, long index); // svldnt1sh_gather[_u32base]_index_u32
  ///   public static unsafe Vector<long> GatherVectorInt16SignExtendNonTemporal(Vector<long> mask, Vector<ulong> addresses, long index); // svldnt1sh_gather[_u64base]_index_s64
  ///   public static unsafe Vector<ulong> GatherVectorInt16SignExtendNonTemporal(Vector<ulong> mask, Vector<ulong> addresses, long index); // svldnt1sh_gather[_u64base]_index_u64
  ///   public static unsafe Vector<int> GatherVectorInt16WithByteOffsetsSignExtendNonTemporal(Vector<int> mask, Vector<uint> addresses, long offset); // svldnt1sh_gather[_u32base]_offset_s32
  ///   public static unsafe Vector<uint> GatherVectorInt16WithByteOffsetsSignExtendNonTemporal(Vector<uint> mask, Vector<uint> addresses, long offset); // svldnt1sh_gather[_u32base]_offset_u32
  ///   public static unsafe Vector<long> GatherVectorInt16WithByteOffsetsSignExtendNonTemporal(Vector<long> mask, Vector<ulong> addresses, long offset); // svldnt1sh_gather[_u64base]_offset_s64
  ///   public static unsafe Vector<ulong> GatherVectorInt16WithByteOffsetsSignExtendNonTemporal(Vector<ulong> mask, Vector<ulong> addresses, long offset); // svldnt1sh_gather[_u64base]_offset_u64
  ///   public static unsafe Vector<long> GatherVectorInt32SignExtendNonTemporal(Vector<long> mask, Vector<ulong> addresses, long index); // svldnt1sw_gather[_u64base]_index_s64
  ///   public static unsafe Vector<int> GatherVectorInt32SignExtendNonTemporal(Vector<int> mask, Vector<uint> addresses, long index); // svldnt1sw_gather[_u64base]_index_s64
  ///   public static unsafe Vector<ulong> GatherVectorInt32SignExtendNonTemporal(Vector<ulong> mask, Vector<ulong> addresses, long index); // svldnt1sw_gather[_u64base]_index_u64
  ///   public static unsafe Vector<uint> GatherVectorInt32SignExtendNonTemporal(Vector<uint> mask, Vector<uint> addresses, long index); // svldnt1sw_gather[_u64base]_index_u64
  ///   public static unsafe Vector<long> GatherVectorInt32WithByteOffsetsSignExtendNonTemporal(Vector<long> mask, Vector<ulong> addresses, long offset); // svldnt1sw_gather[_u64base]_offset_s64
  ///   public static unsafe Vector<int> GatherVectorInt32WithByteOffsetsSignExtendNonTemporal(Vector<int> mask, Vector<uint> addresses, long offset); // svldnt1sw_gather[_u64base]_offset_s64
  ///   public static unsafe Vector<ulong> GatherVectorInt32WithByteOffsetsSignExtendNonTemporal(Vector<ulong> mask, Vector<ulong> addresses, long offset); // svldnt1sw_gather[_u64base]_offset_u64
  ///   public static unsafe Vector<uint> GatherVectorInt32WithByteOffsetsSignExtendNonTemporal(Vector<uint> mask, Vector<uint> addresses, long offset); // svldnt1sw_gather[_u64base]_offset_u64
  ///   public static unsafe Vector<float> GatherVectorNonTemporal(Vector<float> mask, Vector<uint> addresses, long offset); // svldnt1_gather[_u32base]_offset_f32
  ///   public static unsafe Vector<int> GatherVectorNonTemporal(Vector<int> mask, Vector<uint> addresses, long offset); // svldnt1_gather[_u32base]_offset_s32
  ///   public static unsafe Vector<uint> GatherVectorNonTemporal(Vector<uint> mask, Vector<uint> addresses, long offset); // svldnt1_gather[_u32base]_offset_u32
  ///   public static unsafe Vector<double> GatherVectorNonTemporal(Vector<double> mask, Vector<ulong> addresses, long offset); // svldnt1_gather[_u64base]_offset_f64
  ///   public static unsafe Vector<long> GatherVectorNonTemporal(Vector<long> mask, Vector<ulong> addresses, long offset); // svldnt1_gather[_u64base]_offset_s64
  ///   public static unsafe Vector<ulong> GatherVectorNonTemporal(Vector<ulong> mask, Vector<ulong> addresses, long offset); // svldnt1_gather[_u64base]_offset_u64
  ///   public static unsafe Vector<float> GatherVectorNonTemporal(Vector<float> mask, Vector<uint> addresses, long index); // svldnt1_gather[_u32base]_index_f32
  ///   public static unsafe Vector<int> GatherVectorNonTemporal(Vector<int> mask, Vector<uint> addresses, long index); // svldnt1_gather[_u32base]_index_s32
  ///   public static unsafe Vector<uint> GatherVectorNonTemporal(Vector<uint> mask, Vector<uint> addresses, long index); // svldnt1_gather[_u32base]_index_u32
  ///   public static unsafe Vector<double> GatherVectorNonTemporal(Vector<double> mask, Vector<ulong> addresses, long index); // svldnt1_gather[_u64base]_index_f64
  ///   public static unsafe Vector<long> GatherVectorNonTemporal(Vector<long> mask, Vector<ulong> addresses, long index); // svldnt1_gather[_u64base]_index_s64
  ///   public static unsafe Vector<ulong> GatherVectorNonTemporal(Vector<ulong> mask, Vector<ulong> addresses, long index); // svldnt1_gather[_u64base]_index_u64
  ///   public static unsafe Vector<int> GatherVectorSByteSignExtendNonTemporal(Vector<int> mask, Vector<uint> addresses, long offset); // svldnt1sb_gather[_u32base]_offset_s32
  ///   public static unsafe Vector<uint> GatherVectorSByteSignExtendNonTemporal(Vector<uint> mask, Vector<uint> addresses, long offset); // svldnt1sb_gather[_u32base]_offset_u32
  ///   public static unsafe Vector<long> GatherVectorSByteSignExtendNonTemporal(Vector<long> mask, Vector<ulong> addresses, long offset); // svldnt1sb_gather[_u64base]_offset_s64
  ///   public static unsafe Vector<ulong> GatherVectorSByteSignExtendNonTemporal(Vector<ulong> mask, Vector<ulong> addresses, long offset); // svldnt1sb_gather[_u64base]_offset_u64
  ///   public static unsafe Vector<int> GatherVectorUInt16WithByteOffsetsZeroExtendNonTemporal(Vector<int> mask, Vector<uint> addresses, long offset); // svldnt1uh_gather[_u32base]_offset_s32
  ///   public static unsafe Vector<uint> GatherVectorUInt16WithByteOffsetsZeroExtendNonTemporal(Vector<uint> mask, Vector<uint> addresses, long offset); // svldnt1uh_gather[_u32base]_offset_u32
  ///   public static unsafe Vector<long> GatherVectorUInt16WithByteOffsetsZeroExtendNonTemporal(Vector<long> mask, Vector<ulong> addresses, long offset); // svldnt1uh_gather[_u64base]_offset_s64
  ///   public static unsafe Vector<ulong> GatherVectorUInt16WithByteOffsetsZeroExtendNonTemporal(Vector<ulong> mask, Vector<ulong> addresses, long offset); // svldnt1uh_gather[_u64base]_offset_u64
  ///   public static unsafe Vector<int> GatherVectorUInt16ZeroExtendNonTemporal(Vector<int> mask, Vector<uint> addresses, long index); // svldnt1uh_gather[_u32base]_index_s32
  ///   public static unsafe Vector<uint> GatherVectorUInt16ZeroExtendNonTemporal(Vector<uint> mask, Vector<uint> addresses, long index); // svldnt1uh_gather[_u32base]_index_u32
  ///   public static unsafe Vector<long> GatherVectorUInt16ZeroExtendNonTemporal(Vector<long> mask, Vector<ulong> addresses, long index); // svldnt1uh_gather[_u64base]_index_s64
  ///   public static unsafe Vector<ulong> GatherVectorUInt16ZeroExtendNonTemporal(Vector<ulong> mask, Vector<ulong> addresses, long index); // svldnt1uh_gather[_u64base]_index_u64
  ///   public static unsafe Vector<long> GatherVectorUInt32WithByteOffsetsZeroExtendNonTemporal(Vector<long> mask, Vector<ulong> addresses, long offset); // svldnt1uw_gather[_u64base]_offset_s64
  ///   public static unsafe Vector<int> GatherVectorUInt32WithByteOffsetsZeroExtendNonTemporal(Vector<int> mask, Vector<uint> addresses, long offset); // svldnt1uw_gather[_u64base]_offset_s64
  ///   public static unsafe Vector<ulong> GatherVectorUInt32WithByteOffsetsZeroExtendNonTemporal(Vector<ulong> mask, Vector<ulong> addresses, long offset); // svldnt1uw_gather[_u64base]_offset_u64
  ///   public static unsafe Vector<uint> GatherVectorUInt32WithByteOffsetsZeroExtendNonTemporal(Vector<uint> mask, Vector<uint> addresses, long offset); // svldnt1uw_gather[_u64base]_offset_u64
  ///   public static unsafe Vector<long> GatherVectorUInt32ZeroExtendNonTemporal(Vector<long> mask, Vector<ulong> addresses, long index); // svldnt1uw_gather[_u64base]_index_s64
  ///   public static unsafe Vector<int> GatherVectorUInt32ZeroExtendNonTemporal(Vector<int> mask, Vector<uint> addresses, long index); // svldnt1uw_gather[_u64base]_index_s64
  ///   public static unsafe Vector<ulong> GatherVectorUInt32ZeroExtendNonTemporal(Vector<ulong> mask, Vector<ulong> addresses, long index); // svldnt1uw_gather[_u64base]_index_u64
  ///   public static unsafe Vector<uint> GatherVectorUInt32ZeroExtendNonTemporal(Vector<uint> mask, Vector<uint> addresses, long index); // svldnt1uw_gather[_u64base]_index_u64
  ///   Total Rejected: 52

  /// Total ACLE covered across API:      161

