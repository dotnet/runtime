namespace System.Runtime.Intrinsics.Arm;

/// VectorT Summary
public abstract partial class Sve : AdvSimd /// Feature: FEAT_SVE  Category: firstfaulting
{

  /// T: [int, uint], [uint, uint], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorByteZeroExtendFirstFaulting(Vector<T> mask, Vector<T2> addresses); // LDFF1B

  /// T: [int, int], [uint, int], [int, uint], [uint, uint], [long, long], [ulong, long], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorByteZeroExtendFirstFaulting(Vector<T> mask, byte* address, Vector<T2> offsets); // LDFF1B

  /// T: [float, uint], [int, uint], [double, ulong], [long, ulong]
  public static unsafe Vector<T> GatherVectorFirstFaulting(Vector<T> mask, Vector<T2> addresses); // LDFF1W or LDFF1D

  /// T: uint, ulong
  public static unsafe Vector<T> GatherVectorFirstFaulting(Vector<T> mask, Vector<T> addresses); // LDFF1W or LDFF1D

  /// T: [float, int], [uint, int], [float, uint], [int, uint], [double, long], [ulong, long], [double, ulong], [long, ulong]
  public static unsafe Vector<T> GatherVectorFirstFaulting(Vector<T> mask, T* address, Vector<T2> indices); // LDFF1W or LDFF1D

  /// T: int, uint, long, ulong
  public static unsafe Vector<T> GatherVectorFirstFaulting(Vector<T> mask, T* address, Vector<T> indices); // LDFF1W or LDFF1D

  /// T: [int, uint], [uint, uint], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorInt16SignExtendFirstFaulting(Vector<T> mask, Vector<T2> addresses); // LDFF1SH

  /// T: [int, int], [uint, int], [int, uint], [uint, uint], [long, long], [ulong, long], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorInt16SignExtendFirstFaulting(Vector<T> mask, short* address, Vector<T2> indices); // LDFF1SH

  /// T: [int, int], [uint, int], [int, uint], [uint, uint], [long, long], [ulong, long], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(Vector<T> mask, short* address, Vector<T2> offsets); // LDFF1SH

  /// T: [long, ulong], [int, uint], [ulong, ulong], [uint, uint]
  public static unsafe Vector<T> GatherVectorInt32SignExtendFirstFaulting(Vector<T> mask, Vector<T2> addresses); // LDFF1SW

  /// T: [long, long], [int, int], [ulong, long], [uint, int], [long, ulong], [int, uint], [ulong, ulong], [uint, uint]
  public static unsafe Vector<T> GatherVectorInt32SignExtendFirstFaulting(Vector<T> mask, int* address, Vector<T2> indices); // LDFF1SW

  /// T: [long, long], [int, int], [ulong, long], [uint, int], [long, ulong], [int, uint], [ulong, ulong], [uint, uint]
  public static unsafe Vector<T> GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(Vector<T> mask, int* address, Vector<T2> offsets); // LDFF1SW

  /// T: [int, uint], [uint, uint], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorSByteSignExtendFirstFaulting(Vector<T> mask, Vector<T2> addresses); // LDFF1SB

  /// T: [int, int], [uint, int], [int, uint], [uint, uint], [long, long], [ulong, long], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorSByteSignExtendFirstFaulting(Vector<T> mask, sbyte* address, Vector<T2> offsets); // LDFF1SB

  /// T: [int, int], [uint, int], [int, uint], [uint, uint], [long, long], [ulong, long], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(Vector<T> mask, ushort* address, Vector<T2> offsets); // LDFF1H

  /// T: [int, uint], [uint, uint], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<T> mask, Vector<T2> addresses); // LDFF1H

  /// T: [int, int], [uint, int], [int, uint], [uint, uint], [long, long], [ulong, long], [long, ulong], [ulong, ulong]
  public static unsafe Vector<T> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<T> mask, ushort* address, Vector<T2> indices); // LDFF1H

  /// T: [long, long], [int, int], [ulong, long], [uint, int], [long, ulong], [int, uint], [ulong, ulong], [uint, uint]
  public static unsafe Vector<T> GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(Vector<T> mask, uint* address, Vector<T2> offsets); // LDFF1W

  /// T: [long, ulong], [int, uint], [ulong, ulong], [uint, uint]
  public static unsafe Vector<T> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<T> mask, Vector<T2> addresses); // LDFF1W

  /// T: [long, long], [int, int], [ulong, long], [uint, int], [long, ulong], [int, uint], [ulong, ulong], [uint, uint]
  public static unsafe Vector<T> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<T> mask, uint* address, Vector<T2> indices); // LDFF1W

  /// T: [float, int], [uint, int], [float, uint], [int, uint], [double, long], [ulong, long], [double, ulong], [long, ulong]
  public static unsafe Vector<T> GatherVectorWithByteOffsetFirstFaulting(Vector<T> mask, T* address, Vector<T2> offsets); // LDFF1W or LDFF1D

  /// T: int, uint, long, ulong
  public static unsafe Vector<T> GatherVectorWithByteOffsetFirstFaulting(Vector<T> mask, T* address, Vector<T> offsets); // LDFF1W or LDFF1D

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> GetFfr(); // RDFFR // predicated

  /// T: short, int, long, ushort, uint, ulong
  public static unsafe Vector<T> LoadVectorByteZeroExtendFirstFaulting(Vector<T> mask, byte* address); // LDFF1B

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> LoadVectorFirstFaulting(Vector<T> mask, T* address); // LDFF1W or LDFF1D or LDFF1B or LDFF1H

  /// T: int, long, uint, ulong
  public static unsafe Vector<T> LoadVectorInt16SignExtendFirstFaulting(Vector<T> mask, short* address); // LDFF1SH

  /// T: long, ulong
  public static unsafe Vector<T> LoadVectorInt32SignExtendFirstFaulting(Vector<T> mask, int* address); // LDFF1SW

  /// T: short, int, long, ushort, uint, ulong
  public static unsafe Vector<T> LoadVectorSByteSignExtendFirstFaulting(Vector<T> mask, sbyte* address); // LDFF1SB

  /// T: int, long, uint, ulong
  public static unsafe Vector<T> LoadVectorUInt16ZeroExtendFirstFaulting(Vector<T> mask, ushort* address); // LDFF1H

  /// T: long, ulong
  public static unsafe Vector<T> LoadVectorUInt32ZeroExtendFirstFaulting(Vector<T> mask, uint* address); // LDFF1W

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe void SetFfr(Vector<T> value); // WRFFR

  /// total method signatures: 31

}


/// Full API
public abstract partial class Sve : AdvSimd /// Feature: FEAT_SVE  Category: firstfaulting
{
    /// GatherVectorByteZeroExtendFirstFaulting : Load 8-bit data and zero-extend, first-faulting

    /// svint32_t svldff1ub_gather[_u32base]_s32(svbool_t pg, svuint32_t bases) : "LDFF1B Zresult.S, Pg/Z, [Zbases.S, #0]"
  public static unsafe Vector<int> GatherVectorByteZeroExtendFirstFaulting(Vector<int> mask, Vector<uint> addresses);

    /// svuint32_t svldff1ub_gather[_u32base]_u32(svbool_t pg, svuint32_t bases) : "LDFF1B Zresult.S, Pg/Z, [Zbases.S, #0]"
  public static unsafe Vector<uint> GatherVectorByteZeroExtendFirstFaulting(Vector<uint> mask, Vector<uint> addresses);

    /// svint64_t svldff1ub_gather[_u64base]_s64(svbool_t pg, svuint64_t bases) : "LDFF1B Zresult.D, Pg/Z, [Zbases.D, #0]"
  public static unsafe Vector<long> GatherVectorByteZeroExtendFirstFaulting(Vector<long> mask, Vector<ulong> addresses);

    /// svuint64_t svldff1ub_gather[_u64base]_u64(svbool_t pg, svuint64_t bases) : "LDFF1B Zresult.D, Pg/Z, [Zbases.D, #0]"
  public static unsafe Vector<ulong> GatherVectorByteZeroExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> addresses);

    /// svint32_t svldff1ub_gather_[s32]offset_s32(svbool_t pg, const uint8_t *base, svint32_t offsets) : "LDFF1B Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]"
  public static unsafe Vector<int> GatherVectorByteZeroExtendFirstFaulting(Vector<int> mask, byte* address, Vector<int> offsets);

    /// svuint32_t svldff1ub_gather_[s32]offset_u32(svbool_t pg, const uint8_t *base, svint32_t offsets) : "LDFF1B Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]"
  public static unsafe Vector<uint> GatherVectorByteZeroExtendFirstFaulting(Vector<uint> mask, byte* address, Vector<int> offsets);

    /// svint32_t svldff1ub_gather_[u32]offset_s32(svbool_t pg, const uint8_t *base, svuint32_t offsets) : "LDFF1B Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]"
  public static unsafe Vector<int> GatherVectorByteZeroExtendFirstFaulting(Vector<int> mask, byte* address, Vector<uint> offsets);

    /// svuint32_t svldff1ub_gather_[u32]offset_u32(svbool_t pg, const uint8_t *base, svuint32_t offsets) : "LDFF1B Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]"
  public static unsafe Vector<uint> GatherVectorByteZeroExtendFirstFaulting(Vector<uint> mask, byte* address, Vector<uint> offsets);

    /// svint64_t svldff1ub_gather_[s64]offset_s64(svbool_t pg, const uint8_t *base, svint64_t offsets) : "LDFF1B Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<long> GatherVectorByteZeroExtendFirstFaulting(Vector<long> mask, byte* address, Vector<long> offsets);

    /// svuint64_t svldff1ub_gather_[s64]offset_u64(svbool_t pg, const uint8_t *base, svint64_t offsets) : "LDFF1B Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<ulong> GatherVectorByteZeroExtendFirstFaulting(Vector<ulong> mask, byte* address, Vector<long> offsets);

    /// svint64_t svldff1ub_gather_[u64]offset_s64(svbool_t pg, const uint8_t *base, svuint64_t offsets) : "LDFF1B Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<long> GatherVectorByteZeroExtendFirstFaulting(Vector<long> mask, byte* address, Vector<ulong> offsets);

    /// svuint64_t svldff1ub_gather_[u64]offset_u64(svbool_t pg, const uint8_t *base, svuint64_t offsets) : "LDFF1B Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<ulong> GatherVectorByteZeroExtendFirstFaulting(Vector<ulong> mask, byte* address, Vector<ulong> offsets);


    /// GatherVectorFirstFaulting : Unextended load, first-faulting

    /// svfloat32_t svldff1_gather[_u32base]_f32(svbool_t pg, svuint32_t bases) : "LDFF1W Zresult.S, Pg/Z, [Zbases.S, #0]"
  public static unsafe Vector<float> GatherVectorFirstFaulting(Vector<float> mask, Vector<uint> addresses);

    /// svint32_t svldff1_gather[_u32base]_s32(svbool_t pg, svuint32_t bases) : "LDFF1W Zresult.S, Pg/Z, [Zbases.S, #0]"
  public static unsafe Vector<int> GatherVectorFirstFaulting(Vector<int> mask, Vector<uint> addresses);

    /// svuint32_t svldff1_gather[_u32base]_u32(svbool_t pg, svuint32_t bases) : "LDFF1W Zresult.S, Pg/Z, [Zbases.S, #0]"
  public static unsafe Vector<uint> GatherVectorFirstFaulting(Vector<uint> mask, Vector<uint> addresses);

    /// svfloat64_t svldff1_gather[_u64base]_f64(svbool_t pg, svuint64_t bases) : "LDFF1D Zresult.D, Pg/Z, [Zbases.D, #0]"
  public static unsafe Vector<double> GatherVectorFirstFaulting(Vector<double> mask, Vector<ulong> addresses);

    /// svint64_t svldff1_gather[_u64base]_s64(svbool_t pg, svuint64_t bases) : "LDFF1D Zresult.D, Pg/Z, [Zbases.D, #0]"
  public static unsafe Vector<long> GatherVectorFirstFaulting(Vector<long> mask, Vector<ulong> addresses);

    /// svuint64_t svldff1_gather[_u64base]_u64(svbool_t pg, svuint64_t bases) : "LDFF1D Zresult.D, Pg/Z, [Zbases.D, #0]"
  public static unsafe Vector<ulong> GatherVectorFirstFaulting(Vector<ulong> mask, Vector<ulong> addresses);

    /// svfloat32_t svldff1_gather_[s32]index[_f32](svbool_t pg, const float32_t *base, svint32_t indices) : "LDFF1W Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #2]"
  public static unsafe Vector<float> GatherVectorFirstFaulting(Vector<float> mask, float* address, Vector<int> indices);

    /// svint32_t svldff1_gather_[s32]index[_s32](svbool_t pg, const int32_t *base, svint32_t indices) : "LDFF1W Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #2]"
  public static unsafe Vector<int> GatherVectorFirstFaulting(Vector<int> mask, int* address, Vector<int> indices);

    /// svuint32_t svldff1_gather_[s32]index[_u32](svbool_t pg, const uint32_t *base, svint32_t indices) : "LDFF1W Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #2]"
  public static unsafe Vector<uint> GatherVectorFirstFaulting(Vector<uint> mask, uint* address, Vector<int> indices);

    /// svfloat32_t svldff1_gather_[u32]index[_f32](svbool_t pg, const float32_t *base, svuint32_t indices) : "LDFF1W Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #2]"
  public static unsafe Vector<float> GatherVectorFirstFaulting(Vector<float> mask, float* address, Vector<uint> indices);

    /// svint32_t svldff1_gather_[u32]index[_s32](svbool_t pg, const int32_t *base, svuint32_t indices) : "LDFF1W Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #2]"
  public static unsafe Vector<int> GatherVectorFirstFaulting(Vector<int> mask, int* address, Vector<uint> indices);

    /// svuint32_t svldff1_gather_[u32]index[_u32](svbool_t pg, const uint32_t *base, svuint32_t indices) : "LDFF1W Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #2]"
  public static unsafe Vector<uint> GatherVectorFirstFaulting(Vector<uint> mask, uint* address, Vector<uint> indices);

    /// svfloat64_t svldff1_gather_[s64]index[_f64](svbool_t pg, const float64_t *base, svint64_t indices) : "LDFF1D Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #3]"
  public static unsafe Vector<double> GatherVectorFirstFaulting(Vector<double> mask, double* address, Vector<long> indices);

    /// svint64_t svldff1_gather_[s64]index[_s64](svbool_t pg, const int64_t *base, svint64_t indices) : "LDFF1D Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #3]"
  public static unsafe Vector<long> GatherVectorFirstFaulting(Vector<long> mask, long* address, Vector<long> indices);

    /// svuint64_t svldff1_gather_[s64]index[_u64](svbool_t pg, const uint64_t *base, svint64_t indices) : "LDFF1D Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #3]"
  public static unsafe Vector<ulong> GatherVectorFirstFaulting(Vector<ulong> mask, ulong* address, Vector<long> indices);

    /// svfloat64_t svldff1_gather_[u64]index[_f64](svbool_t pg, const float64_t *base, svuint64_t indices) : "LDFF1D Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #3]"
  public static unsafe Vector<double> GatherVectorFirstFaulting(Vector<double> mask, double* address, Vector<ulong> indices);

    /// svint64_t svldff1_gather_[u64]index[_s64](svbool_t pg, const int64_t *base, svuint64_t indices) : "LDFF1D Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #3]"
  public static unsafe Vector<long> GatherVectorFirstFaulting(Vector<long> mask, long* address, Vector<ulong> indices);

    /// svuint64_t svldff1_gather_[u64]index[_u64](svbool_t pg, const uint64_t *base, svuint64_t indices) : "LDFF1D Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #3]"
  public static unsafe Vector<ulong> GatherVectorFirstFaulting(Vector<ulong> mask, ulong* address, Vector<ulong> indices);


    /// GatherVectorInt16SignExtendFirstFaulting : Load 16-bit data and sign-extend, first-faulting

    /// svint32_t svldff1sh_gather[_u32base]_s32(svbool_t pg, svuint32_t bases) : "LDFF1SH Zresult.S, Pg/Z, [Zbases.S, #0]"
  public static unsafe Vector<int> GatherVectorInt16SignExtendFirstFaulting(Vector<int> mask, Vector<uint> addresses);

    /// svuint32_t svldff1sh_gather[_u32base]_u32(svbool_t pg, svuint32_t bases) : "LDFF1SH Zresult.S, Pg/Z, [Zbases.S, #0]"
  public static unsafe Vector<uint> GatherVectorInt16SignExtendFirstFaulting(Vector<uint> mask, Vector<uint> addresses);

    /// svint64_t svldff1sh_gather[_u64base]_s64(svbool_t pg, svuint64_t bases) : "LDFF1SH Zresult.D, Pg/Z, [Zbases.D, #0]"
  public static unsafe Vector<long> GatherVectorInt16SignExtendFirstFaulting(Vector<long> mask, Vector<ulong> addresses);

    /// svuint64_t svldff1sh_gather[_u64base]_u64(svbool_t pg, svuint64_t bases) : "LDFF1SH Zresult.D, Pg/Z, [Zbases.D, #0]"
  public static unsafe Vector<ulong> GatherVectorInt16SignExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> addresses);

    /// svint32_t svldff1sh_gather_[s32]index_s32(svbool_t pg, const int16_t *base, svint32_t indices) : "LDFF1SH Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #1]"
  public static unsafe Vector<int> GatherVectorInt16SignExtendFirstFaulting(Vector<int> mask, short* address, Vector<int> indices);

    /// svuint32_t svldff1sh_gather_[s32]index_u32(svbool_t pg, const int16_t *base, svint32_t indices) : "LDFF1SH Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #1]"
  public static unsafe Vector<uint> GatherVectorInt16SignExtendFirstFaulting(Vector<uint> mask, short* address, Vector<int> indices);

    /// svint32_t svldff1sh_gather_[u32]index_s32(svbool_t pg, const int16_t *base, svuint32_t indices) : "LDFF1SH Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #1]"
  public static unsafe Vector<int> GatherVectorInt16SignExtendFirstFaulting(Vector<int> mask, short* address, Vector<uint> indices);

    /// svuint32_t svldff1sh_gather_[u32]index_u32(svbool_t pg, const int16_t *base, svuint32_t indices) : "LDFF1SH Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #1]"
  public static unsafe Vector<uint> GatherVectorInt16SignExtendFirstFaulting(Vector<uint> mask, short* address, Vector<uint> indices);

    /// svint64_t svldff1sh_gather_[s64]index_s64(svbool_t pg, const int16_t *base, svint64_t indices) : "LDFF1SH Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]"
  public static unsafe Vector<long> GatherVectorInt16SignExtendFirstFaulting(Vector<long> mask, short* address, Vector<long> indices);

    /// svuint64_t svldff1sh_gather_[s64]index_u64(svbool_t pg, const int16_t *base, svint64_t indices) : "LDFF1SH Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]"
  public static unsafe Vector<ulong> GatherVectorInt16SignExtendFirstFaulting(Vector<ulong> mask, short* address, Vector<long> indices);

    /// svint64_t svldff1sh_gather_[u64]index_s64(svbool_t pg, const int16_t *base, svuint64_t indices) : "LDFF1SH Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]"
  public static unsafe Vector<long> GatherVectorInt16SignExtendFirstFaulting(Vector<long> mask, short* address, Vector<ulong> indices);

    /// svuint64_t svldff1sh_gather_[u64]index_u64(svbool_t pg, const int16_t *base, svuint64_t indices) : "LDFF1SH Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]"
  public static unsafe Vector<ulong> GatherVectorInt16SignExtendFirstFaulting(Vector<ulong> mask, short* address, Vector<ulong> indices);


    /// GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting : Load 16-bit data and sign-extend, first-faulting

    /// svint32_t svldff1sh_gather_[s32]offset_s32(svbool_t pg, const int16_t *base, svint32_t offsets) : "LDFF1SH Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]"
  public static unsafe Vector<int> GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(Vector<int> mask, short* address, Vector<int> offsets);

    /// svuint32_t svldff1sh_gather_[s32]offset_u32(svbool_t pg, const int16_t *base, svint32_t offsets) : "LDFF1SH Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]"
  public static unsafe Vector<uint> GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(Vector<uint> mask, short* address, Vector<int> offsets);

    /// svint32_t svldff1sh_gather_[u32]offset_s32(svbool_t pg, const int16_t *base, svuint32_t offsets) : "LDFF1SH Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]"
  public static unsafe Vector<int> GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(Vector<int> mask, short* address, Vector<uint> offsets);

    /// svuint32_t svldff1sh_gather_[u32]offset_u32(svbool_t pg, const int16_t *base, svuint32_t offsets) : "LDFF1SH Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]"
  public static unsafe Vector<uint> GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(Vector<uint> mask, short* address, Vector<uint> offsets);

    /// svint64_t svldff1sh_gather_[s64]offset_s64(svbool_t pg, const int16_t *base, svint64_t offsets) : "LDFF1SH Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<long> GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(Vector<long> mask, short* address, Vector<long> offsets);

    /// svuint64_t svldff1sh_gather_[s64]offset_u64(svbool_t pg, const int16_t *base, svint64_t offsets) : "LDFF1SH Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<ulong> GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(Vector<ulong> mask, short* address, Vector<long> offsets);

    /// svint64_t svldff1sh_gather_[u64]offset_s64(svbool_t pg, const int16_t *base, svuint64_t offsets) : "LDFF1SH Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<long> GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(Vector<long> mask, short* address, Vector<ulong> offsets);

    /// svuint64_t svldff1sh_gather_[u64]offset_u64(svbool_t pg, const int16_t *base, svuint64_t offsets) : "LDFF1SH Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<ulong> GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(Vector<ulong> mask, short* address, Vector<ulong> offsets);


    /// GatherVectorInt32SignExtendFirstFaulting : Load 32-bit data and sign-extend, first-faulting

    /// svint64_t svldff1sw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases) : "LDFF1SW Zresult.D, Pg/Z, [Zbases.D, #0]"
  public static unsafe Vector<long> GatherVectorInt32SignExtendFirstFaulting(Vector<long> mask, Vector<ulong> addresses);

    /// svint64_t svldff1sw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases) : "LDFF1SW Zresult.D, Pg/Z, [Zbases.D, #0]"
  public static unsafe Vector<int> GatherVectorInt32SignExtendFirstFaulting(Vector<int> mask, Vector<uint> addresses);

    /// svuint64_t svldff1sw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases) : "LDFF1SW Zresult.D, Pg/Z, [Zbases.D, #0]"
  public static unsafe Vector<ulong> GatherVectorInt32SignExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> addresses);

    /// svuint64_t svldff1sw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases) : "LDFF1SW Zresult.D, Pg/Z, [Zbases.D, #0]"
  public static unsafe Vector<uint> GatherVectorInt32SignExtendFirstFaulting(Vector<uint> mask, Vector<uint> addresses);

    /// svint64_t svldff1sw_gather_[s64]index_s64(svbool_t pg, const int32_t *base, svint64_t indices) : "LDFF1SW Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]"
  public static unsafe Vector<long> GatherVectorInt32SignExtendFirstFaulting(Vector<long> mask, int* address, Vector<long> indices);

    /// svint64_t svldff1sw_gather_[s64]index_s64(svbool_t pg, const int32_t *base, svint64_t indices) : "LDFF1SW Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]"
  public static unsafe Vector<int> GatherVectorInt32SignExtendFirstFaulting(Vector<int> mask, int* address, Vector<int> indices);

    /// svuint64_t svldff1sw_gather_[s64]index_u64(svbool_t pg, const int32_t *base, svint64_t indices) : "LDFF1SW Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]"
  public static unsafe Vector<ulong> GatherVectorInt32SignExtendFirstFaulting(Vector<ulong> mask, int* address, Vector<long> indices);

    /// svuint64_t svldff1sw_gather_[s64]index_u64(svbool_t pg, const int32_t *base, svint64_t indices) : "LDFF1SW Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]"
  public static unsafe Vector<uint> GatherVectorInt32SignExtendFirstFaulting(Vector<uint> mask, int* address, Vector<int> indices);

    /// svint64_t svldff1sw_gather_[u64]index_s64(svbool_t pg, const int32_t *base, svuint64_t indices) : "LDFF1SW Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]"
  public static unsafe Vector<long> GatherVectorInt32SignExtendFirstFaulting(Vector<long> mask, int* address, Vector<ulong> indices);

    /// svint64_t svldff1sw_gather_[u64]index_s64(svbool_t pg, const int32_t *base, svuint64_t indices) : "LDFF1SW Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]"
  public static unsafe Vector<int> GatherVectorInt32SignExtendFirstFaulting(Vector<int> mask, int* address, Vector<uint> indices);

    /// svuint64_t svldff1sw_gather_[u64]index_u64(svbool_t pg, const int32_t *base, svuint64_t indices) : "LDFF1SW Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]"
  public static unsafe Vector<ulong> GatherVectorInt32SignExtendFirstFaulting(Vector<ulong> mask, int* address, Vector<ulong> indices);

    /// svuint64_t svldff1sw_gather_[u64]index_u64(svbool_t pg, const int32_t *base, svuint64_t indices) : "LDFF1SW Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]"
  public static unsafe Vector<uint> GatherVectorInt32SignExtendFirstFaulting(Vector<uint> mask, int* address, Vector<uint> indices);


    /// GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting : Load 32-bit data and sign-extend, first-faulting

    /// svint64_t svldff1sw_gather_[s64]offset_s64(svbool_t pg, const int32_t *base, svint64_t offsets) : "LDFF1SW Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<long> GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(Vector<long> mask, int* address, Vector<long> offsets);

    /// svint64_t svldff1sw_gather_[s64]offset_s64(svbool_t pg, const int32_t *base, svint64_t offsets) : "LDFF1SW Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<int> GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(Vector<int> mask, int* address, Vector<int> offsets);

    /// svuint64_t svldff1sw_gather_[s64]offset_u64(svbool_t pg, const int32_t *base, svint64_t offsets) : "LDFF1SW Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<ulong> GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(Vector<ulong> mask, int* address, Vector<long> offsets);

    /// svuint64_t svldff1sw_gather_[s64]offset_u64(svbool_t pg, const int32_t *base, svint64_t offsets) : "LDFF1SW Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<uint> GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(Vector<uint> mask, int* address, Vector<int> offsets);

    /// svint64_t svldff1sw_gather_[u64]offset_s64(svbool_t pg, const int32_t *base, svuint64_t offsets) : "LDFF1SW Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<long> GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(Vector<long> mask, int* address, Vector<ulong> offsets);

    /// svint64_t svldff1sw_gather_[u64]offset_s64(svbool_t pg, const int32_t *base, svuint64_t offsets) : "LDFF1SW Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<int> GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(Vector<int> mask, int* address, Vector<uint> offsets);

    /// svuint64_t svldff1sw_gather_[u64]offset_u64(svbool_t pg, const int32_t *base, svuint64_t offsets) : "LDFF1SW Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<ulong> GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(Vector<ulong> mask, int* address, Vector<ulong> offsets);

    /// svuint64_t svldff1sw_gather_[u64]offset_u64(svbool_t pg, const int32_t *base, svuint64_t offsets) : "LDFF1SW Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<uint> GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(Vector<uint> mask, int* address, Vector<uint> offsets);


    /// GatherVectorSByteSignExtendFirstFaulting : Load 8-bit data and sign-extend, first-faulting

    /// svint32_t svldff1sb_gather[_u32base]_s32(svbool_t pg, svuint32_t bases) : "LDFF1SB Zresult.S, Pg/Z, [Zbases.S, #0]"
  public static unsafe Vector<int> GatherVectorSByteSignExtendFirstFaulting(Vector<int> mask, Vector<uint> addresses);

    /// svuint32_t svldff1sb_gather[_u32base]_u32(svbool_t pg, svuint32_t bases) : "LDFF1SB Zresult.S, Pg/Z, [Zbases.S, #0]"
  public static unsafe Vector<uint> GatherVectorSByteSignExtendFirstFaulting(Vector<uint> mask, Vector<uint> addresses);

    /// svint64_t svldff1sb_gather[_u64base]_s64(svbool_t pg, svuint64_t bases) : "LDFF1SB Zresult.D, Pg/Z, [Zbases.D, #0]"
  public static unsafe Vector<long> GatherVectorSByteSignExtendFirstFaulting(Vector<long> mask, Vector<ulong> addresses);

    /// svuint64_t svldff1sb_gather[_u64base]_u64(svbool_t pg, svuint64_t bases) : "LDFF1SB Zresult.D, Pg/Z, [Zbases.D, #0]"
  public static unsafe Vector<ulong> GatherVectorSByteSignExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> addresses);

    /// svint32_t svldff1sb_gather_[s32]offset_s32(svbool_t pg, const int8_t *base, svint32_t offsets) : "LDFF1SB Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]"
  public static unsafe Vector<int> GatherVectorSByteSignExtendFirstFaulting(Vector<int> mask, sbyte* address, Vector<int> offsets);

    /// svuint32_t svldff1sb_gather_[s32]offset_u32(svbool_t pg, const int8_t *base, svint32_t offsets) : "LDFF1SB Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]"
  public static unsafe Vector<uint> GatherVectorSByteSignExtendFirstFaulting(Vector<uint> mask, sbyte* address, Vector<int> offsets);

    /// svint32_t svldff1sb_gather_[u32]offset_s32(svbool_t pg, const int8_t *base, svuint32_t offsets) : "LDFF1SB Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]"
  public static unsafe Vector<int> GatherVectorSByteSignExtendFirstFaulting(Vector<int> mask, sbyte* address, Vector<uint> offsets);

    /// svuint32_t svldff1sb_gather_[u32]offset_u32(svbool_t pg, const int8_t *base, svuint32_t offsets) : "LDFF1SB Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]"
  public static unsafe Vector<uint> GatherVectorSByteSignExtendFirstFaulting(Vector<uint> mask, sbyte* address, Vector<uint> offsets);

    /// svint64_t svldff1sb_gather_[s64]offset_s64(svbool_t pg, const int8_t *base, svint64_t offsets) : "LDFF1SB Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<long> GatherVectorSByteSignExtendFirstFaulting(Vector<long> mask, sbyte* address, Vector<long> offsets);

    /// svuint64_t svldff1sb_gather_[s64]offset_u64(svbool_t pg, const int8_t *base, svint64_t offsets) : "LDFF1SB Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<ulong> GatherVectorSByteSignExtendFirstFaulting(Vector<ulong> mask, sbyte* address, Vector<long> offsets);

    /// svint64_t svldff1sb_gather_[u64]offset_s64(svbool_t pg, const int8_t *base, svuint64_t offsets) : "LDFF1SB Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<long> GatherVectorSByteSignExtendFirstFaulting(Vector<long> mask, sbyte* address, Vector<ulong> offsets);

    /// svuint64_t svldff1sb_gather_[u64]offset_u64(svbool_t pg, const int8_t *base, svuint64_t offsets) : "LDFF1SB Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<ulong> GatherVectorSByteSignExtendFirstFaulting(Vector<ulong> mask, sbyte* address, Vector<ulong> offsets);


    /// GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting : Load 16-bit data and zero-extend, first-faulting

    /// svint32_t svldff1uh_gather_[s32]offset_s32(svbool_t pg, const uint16_t *base, svint32_t offsets) : "LDFF1H Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]"
  public static unsafe Vector<int> GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(Vector<int> mask, ushort* address, Vector<int> offsets);

    /// svuint32_t svldff1uh_gather_[s32]offset_u32(svbool_t pg, const uint16_t *base, svint32_t offsets) : "LDFF1H Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]"
  public static unsafe Vector<uint> GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(Vector<uint> mask, ushort* address, Vector<int> offsets);

    /// svint32_t svldff1uh_gather_[u32]offset_s32(svbool_t pg, const uint16_t *base, svuint32_t offsets) : "LDFF1H Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]"
  public static unsafe Vector<int> GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(Vector<int> mask, ushort* address, Vector<uint> offsets);

    /// svuint32_t svldff1uh_gather_[u32]offset_u32(svbool_t pg, const uint16_t *base, svuint32_t offsets) : "LDFF1H Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]"
  public static unsafe Vector<uint> GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(Vector<uint> mask, ushort* address, Vector<uint> offsets);

    /// svint64_t svldff1uh_gather_[s64]offset_s64(svbool_t pg, const uint16_t *base, svint64_t offsets) : "LDFF1H Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<long> GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(Vector<long> mask, ushort* address, Vector<long> offsets);

    /// svuint64_t svldff1uh_gather_[s64]offset_u64(svbool_t pg, const uint16_t *base, svint64_t offsets) : "LDFF1H Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<ulong> GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(Vector<ulong> mask, ushort* address, Vector<long> offsets);

    /// svint64_t svldff1uh_gather_[u64]offset_s64(svbool_t pg, const uint16_t *base, svuint64_t offsets) : "LDFF1H Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<long> GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(Vector<long> mask, ushort* address, Vector<ulong> offsets);

    /// svuint64_t svldff1uh_gather_[u64]offset_u64(svbool_t pg, const uint16_t *base, svuint64_t offsets) : "LDFF1H Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<ulong> GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(Vector<ulong> mask, ushort* address, Vector<ulong> offsets);


    /// GatherVectorUInt16ZeroExtendFirstFaulting : Load 16-bit data and zero-extend, first-faulting

    /// svint32_t svldff1uh_gather[_u32base]_s32(svbool_t pg, svuint32_t bases) : "LDFF1H Zresult.S, Pg/Z, [Zbases.S, #0]"
  public static unsafe Vector<int> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<int> mask, Vector<uint> addresses);

    /// svuint32_t svldff1uh_gather[_u32base]_u32(svbool_t pg, svuint32_t bases) : "LDFF1H Zresult.S, Pg/Z, [Zbases.S, #0]"
  public static unsafe Vector<uint> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<uint> mask, Vector<uint> addresses);

    /// svint64_t svldff1uh_gather[_u64base]_s64(svbool_t pg, svuint64_t bases) : "LDFF1H Zresult.D, Pg/Z, [Zbases.D, #0]"
  public static unsafe Vector<long> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<long> mask, Vector<ulong> addresses);

    /// svuint64_t svldff1uh_gather[_u64base]_u64(svbool_t pg, svuint64_t bases) : "LDFF1H Zresult.D, Pg/Z, [Zbases.D, #0]"
  public static unsafe Vector<ulong> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> addresses);

    /// svint32_t svldff1uh_gather_[s32]index_s32(svbool_t pg, const uint16_t *base, svint32_t indices) : "LDFF1H Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #1]"
  public static unsafe Vector<int> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<int> mask, ushort* address, Vector<int> indices);

    /// svuint32_t svldff1uh_gather_[s32]index_u32(svbool_t pg, const uint16_t *base, svint32_t indices) : "LDFF1H Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #1]"
  public static unsafe Vector<uint> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<uint> mask, ushort* address, Vector<int> indices);

    /// svint32_t svldff1uh_gather_[u32]index_s32(svbool_t pg, const uint16_t *base, svuint32_t indices) : "LDFF1H Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #1]"
  public static unsafe Vector<int> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<int> mask, ushort* address, Vector<uint> indices);

    /// svuint32_t svldff1uh_gather_[u32]index_u32(svbool_t pg, const uint16_t *base, svuint32_t indices) : "LDFF1H Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #1]"
  public static unsafe Vector<uint> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<uint> mask, ushort* address, Vector<uint> indices);

    /// svint64_t svldff1uh_gather_[s64]index_s64(svbool_t pg, const uint16_t *base, svint64_t indices) : "LDFF1H Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]"
  public static unsafe Vector<long> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<long> mask, ushort* address, Vector<long> indices);

    /// svuint64_t svldff1uh_gather_[s64]index_u64(svbool_t pg, const uint16_t *base, svint64_t indices) : "LDFF1H Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]"
  public static unsafe Vector<ulong> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<ulong> mask, ushort* address, Vector<long> indices);

    /// svint64_t svldff1uh_gather_[u64]index_s64(svbool_t pg, const uint16_t *base, svuint64_t indices) : "LDFF1H Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]"
  public static unsafe Vector<long> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<long> mask, ushort* address, Vector<ulong> indices);

    /// svuint64_t svldff1uh_gather_[u64]index_u64(svbool_t pg, const uint16_t *base, svuint64_t indices) : "LDFF1H Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]"
  public static unsafe Vector<ulong> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<ulong> mask, ushort* address, Vector<ulong> indices);


    /// GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting : Load 32-bit data and zero-extend, first-faulting

    /// svint64_t svldff1uw_gather_[s64]offset_s64(svbool_t pg, const uint32_t *base, svint64_t offsets) : "LDFF1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<long> GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(Vector<long> mask, uint* address, Vector<long> offsets);

    /// svint64_t svldff1uw_gather_[s64]offset_s64(svbool_t pg, const uint32_t *base, svint64_t offsets) : "LDFF1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<int> GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(Vector<int> mask, uint* address, Vector<int> offsets);

    /// svuint64_t svldff1uw_gather_[s64]offset_u64(svbool_t pg, const uint32_t *base, svint64_t offsets) : "LDFF1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<ulong> GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(Vector<ulong> mask, uint* address, Vector<long> offsets);

    /// svuint64_t svldff1uw_gather_[s64]offset_u64(svbool_t pg, const uint32_t *base, svint64_t offsets) : "LDFF1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<uint> GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(Vector<uint> mask, uint* address, Vector<int> offsets);

    /// svint64_t svldff1uw_gather_[u64]offset_s64(svbool_t pg, const uint32_t *base, svuint64_t offsets) : "LDFF1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<long> GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(Vector<long> mask, uint* address, Vector<ulong> offsets);

    /// svint64_t svldff1uw_gather_[u64]offset_s64(svbool_t pg, const uint32_t *base, svuint64_t offsets) : "LDFF1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<int> GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(Vector<int> mask, uint* address, Vector<uint> offsets);

    /// svuint64_t svldff1uw_gather_[u64]offset_u64(svbool_t pg, const uint32_t *base, svuint64_t offsets) : "LDFF1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<ulong> GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(Vector<ulong> mask, uint* address, Vector<ulong> offsets);

    /// svuint64_t svldff1uw_gather_[u64]offset_u64(svbool_t pg, const uint32_t *base, svuint64_t offsets) : "LDFF1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<uint> GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(Vector<uint> mask, uint* address, Vector<uint> offsets);


    /// GatherVectorUInt32ZeroExtendFirstFaulting : Load 32-bit data and zero-extend, first-faulting

    /// svint64_t svldff1uw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases) : "LDFF1W Zresult.D, Pg/Z, [Zbases.D, #0]"
  public static unsafe Vector<long> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<long> mask, Vector<ulong> addresses);

    /// svint64_t svldff1uw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases) : "LDFF1W Zresult.D, Pg/Z, [Zbases.D, #0]"
  public static unsafe Vector<int> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<int> mask, Vector<uint> addresses);

    /// svuint64_t svldff1uw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases) : "LDFF1W Zresult.D, Pg/Z, [Zbases.D, #0]"
  public static unsafe Vector<ulong> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> addresses);

    /// svuint64_t svldff1uw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases) : "LDFF1W Zresult.D, Pg/Z, [Zbases.D, #0]"
  public static unsafe Vector<uint> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<uint> mask, Vector<uint> addresses);

    /// svint64_t svldff1uw_gather_[s64]index_s64(svbool_t pg, const uint32_t *base, svint64_t indices) : "LDFF1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]"
  public static unsafe Vector<long> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<long> mask, uint* address, Vector<long> indices);

    /// svint64_t svldff1uw_gather_[s64]index_s64(svbool_t pg, const uint32_t *base, svint64_t indices) : "LDFF1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]"
  public static unsafe Vector<int> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<int> mask, uint* address, Vector<int> indices);

    /// svuint64_t svldff1uw_gather_[s64]index_u64(svbool_t pg, const uint32_t *base, svint64_t indices) : "LDFF1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]"
  public static unsafe Vector<ulong> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<ulong> mask, uint* address, Vector<long> indices);

    /// svuint64_t svldff1uw_gather_[s64]index_u64(svbool_t pg, const uint32_t *base, svint64_t indices) : "LDFF1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]"
  public static unsafe Vector<uint> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<uint> mask, uint* address, Vector<int> indices);

    /// svint64_t svldff1uw_gather_[u64]index_s64(svbool_t pg, const uint32_t *base, svuint64_t indices) : "LDFF1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]"
  public static unsafe Vector<long> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<long> mask, uint* address, Vector<ulong> indices);

    /// svint64_t svldff1uw_gather_[u64]index_s64(svbool_t pg, const uint32_t *base, svuint64_t indices) : "LDFF1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]"
  public static unsafe Vector<int> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<int> mask, uint* address, Vector<uint> indices);

    /// svuint64_t svldff1uw_gather_[u64]index_u64(svbool_t pg, const uint32_t *base, svuint64_t indices) : "LDFF1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]"
  public static unsafe Vector<ulong> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<ulong> mask, uint* address, Vector<ulong> indices);

    /// svuint64_t svldff1uw_gather_[u64]index_u64(svbool_t pg, const uint32_t *base, svuint64_t indices) : "LDFF1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]"
  public static unsafe Vector<uint> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<uint> mask, uint* address, Vector<uint> indices);


    /// GatherVectorWithByteOffsetFirstFaulting : Unextended load, first-faulting

    /// svfloat32_t svldff1_gather_[s32]offset[_f32](svbool_t pg, const float32_t *base, svint32_t offsets) : "LDFF1W Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]"
  public static unsafe Vector<float> GatherVectorWithByteOffsetFirstFaulting(Vector<float> mask, float* address, Vector<int> offsets);

    /// svint32_t svldff1_gather_[s32]offset[_s32](svbool_t pg, const int32_t *base, svint32_t offsets) : "LDFF1W Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]"
  public static unsafe Vector<int> GatherVectorWithByteOffsetFirstFaulting(Vector<int> mask, int* address, Vector<int> offsets);

    /// svuint32_t svldff1_gather_[s32]offset[_u32](svbool_t pg, const uint32_t *base, svint32_t offsets) : "LDFF1W Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]"
  public static unsafe Vector<uint> GatherVectorWithByteOffsetFirstFaulting(Vector<uint> mask, uint* address, Vector<int> offsets);

    /// svfloat32_t svldff1_gather_[u32]offset[_f32](svbool_t pg, const float32_t *base, svuint32_t offsets) : "LDFF1W Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]"
  public static unsafe Vector<float> GatherVectorWithByteOffsetFirstFaulting(Vector<float> mask, float* address, Vector<uint> offsets);

    /// svint32_t svldff1_gather_[u32]offset[_s32](svbool_t pg, const int32_t *base, svuint32_t offsets) : "LDFF1W Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]"
  public static unsafe Vector<int> GatherVectorWithByteOffsetFirstFaulting(Vector<int> mask, int* address, Vector<uint> offsets);

    /// svuint32_t svldff1_gather_[u32]offset[_u32](svbool_t pg, const uint32_t *base, svuint32_t offsets) : "LDFF1W Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]"
  public static unsafe Vector<uint> GatherVectorWithByteOffsetFirstFaulting(Vector<uint> mask, uint* address, Vector<uint> offsets);

    /// svfloat64_t svldff1_gather_[s64]offset[_f64](svbool_t pg, const float64_t *base, svint64_t offsets) : "LDFF1D Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<double> GatherVectorWithByteOffsetFirstFaulting(Vector<double> mask, double* address, Vector<long> offsets);

    /// svint64_t svldff1_gather_[s64]offset[_s64](svbool_t pg, const int64_t *base, svint64_t offsets) : "LDFF1D Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<long> GatherVectorWithByteOffsetFirstFaulting(Vector<long> mask, long* address, Vector<long> offsets);

    /// svuint64_t svldff1_gather_[s64]offset[_u64](svbool_t pg, const uint64_t *base, svint64_t offsets) : "LDFF1D Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<ulong> GatherVectorWithByteOffsetFirstFaulting(Vector<ulong> mask, ulong* address, Vector<long> offsets);

    /// svfloat64_t svldff1_gather_[u64]offset[_f64](svbool_t pg, const float64_t *base, svuint64_t offsets) : "LDFF1D Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<double> GatherVectorWithByteOffsetFirstFaulting(Vector<double> mask, double* address, Vector<ulong> offsets);

    /// svint64_t svldff1_gather_[u64]offset[_s64](svbool_t pg, const int64_t *base, svuint64_t offsets) : "LDFF1D Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<long> GatherVectorWithByteOffsetFirstFaulting(Vector<long> mask, long* address, Vector<ulong> offsets);

    /// svuint64_t svldff1_gather_[u64]offset[_u64](svbool_t pg, const uint64_t *base, svuint64_t offsets) : "LDFF1D Zresult.D, Pg/Z, [Xbase, Zoffsets.D]"
  public static unsafe Vector<ulong> GatherVectorWithByteOffsetFirstFaulting(Vector<ulong> mask, ulong* address, Vector<ulong> offsets);


    /// GetFfr : Read FFR, returning predicate of succesfully loaded elements

    /// svbool_t svrdffr() : "RDFFR Presult.B"
    /// svbool_t svrdffr_z(svbool_t pg) : "RDFFR Presult.B, Pg/Z"
  public static unsafe Vector<sbyte> GetFfr();

    /// svbool_t svrdffr() : "RDFFR Presult.B"
    /// svbool_t svrdffr_z(svbool_t pg) : "RDFFR Presult.B, Pg/Z"
  public static unsafe Vector<short> GetFfr();

    /// svbool_t svrdffr() : "RDFFR Presult.B"
    /// svbool_t svrdffr_z(svbool_t pg) : "RDFFR Presult.B, Pg/Z"
  public static unsafe Vector<int> GetFfr();

    /// svbool_t svrdffr() : "RDFFR Presult.B"
    /// svbool_t svrdffr_z(svbool_t pg) : "RDFFR Presult.B, Pg/Z"
  public static unsafe Vector<long> GetFfr();

    /// svbool_t svrdffr() : "RDFFR Presult.B"
    /// svbool_t svrdffr_z(svbool_t pg) : "RDFFR Presult.B, Pg/Z"
  public static unsafe Vector<byte> GetFfr();

    /// svbool_t svrdffr() : "RDFFR Presult.B"
    /// svbool_t svrdffr_z(svbool_t pg) : "RDFFR Presult.B, Pg/Z"
  public static unsafe Vector<ushort> GetFfr();

    /// svbool_t svrdffr() : "RDFFR Presult.B"
    /// svbool_t svrdffr_z(svbool_t pg) : "RDFFR Presult.B, Pg/Z"
  public static unsafe Vector<uint> GetFfr();

    /// svbool_t svrdffr() : "RDFFR Presult.B"
    /// svbool_t svrdffr_z(svbool_t pg) : "RDFFR Presult.B, Pg/Z"
  public static unsafe Vector<ulong> GetFfr();


    /// LoadVectorByteZeroExtendFirstFaulting : Load 8-bit data and zero-extend, first-faulting

    /// svint16_t svldff1ub_s16(svbool_t pg, const uint8_t *base) : "LDFF1B Zresult.H, Pg/Z, [Xarray, Xindex]" or "LDFF1B Zresult.H, Pg/Z, [Xbase, XZR]"
  public static unsafe Vector<short> LoadVectorByteZeroExtendFirstFaulting(Vector<short> mask, byte* address);

    /// svint32_t svldff1ub_s32(svbool_t pg, const uint8_t *base) : "LDFF1B Zresult.S, Pg/Z, [Xarray, Xindex]" or "LDFF1B Zresult.S, Pg/Z, [Xbase, XZR]"
  public static unsafe Vector<int> LoadVectorByteZeroExtendFirstFaulting(Vector<int> mask, byte* address);

    /// svint64_t svldff1ub_s64(svbool_t pg, const uint8_t *base) : "LDFF1B Zresult.D, Pg/Z, [Xarray, Xindex]" or "LDFF1B Zresult.D, Pg/Z, [Xbase, XZR]"
  public static unsafe Vector<long> LoadVectorByteZeroExtendFirstFaulting(Vector<long> mask, byte* address);

    /// svuint16_t svldff1ub_u16(svbool_t pg, const uint8_t *base) : "LDFF1B Zresult.H, Pg/Z, [Xarray, Xindex]" or "LDFF1B Zresult.H, Pg/Z, [Xbase, XZR]"
  public static unsafe Vector<ushort> LoadVectorByteZeroExtendFirstFaulting(Vector<ushort> mask, byte* address);

    /// svuint32_t svldff1ub_u32(svbool_t pg, const uint8_t *base) : "LDFF1B Zresult.S, Pg/Z, [Xarray, Xindex]" or "LDFF1B Zresult.S, Pg/Z, [Xbase, XZR]"
  public static unsafe Vector<uint> LoadVectorByteZeroExtendFirstFaulting(Vector<uint> mask, byte* address);

    /// svuint64_t svldff1ub_u64(svbool_t pg, const uint8_t *base) : "LDFF1B Zresult.D, Pg/Z, [Xarray, Xindex]" or "LDFF1B Zresult.D, Pg/Z, [Xbase, XZR]"
  public static unsafe Vector<ulong> LoadVectorByteZeroExtendFirstFaulting(Vector<ulong> mask, byte* address);


    /// LoadVectorFirstFaulting : Unextended load, first-faulting

    /// svfloat32_t svldff1[_f32](svbool_t pg, const float32_t *base) : "LDFF1W Zresult.S, Pg/Z, [Xarray, Xindex, LSL #2]" or "LDFF1W Zresult.S, Pg/Z, [Xbase, XZR, LSL #2]"
  public static unsafe Vector<float> LoadVectorFirstFaulting(Vector<float> mask, float* address);

    /// svfloat64_t svldff1[_f64](svbool_t pg, const float64_t *base) : "LDFF1D Zresult.D, Pg/Z, [Xarray, Xindex, LSL #3]" or "LDFF1D Zresult.D, Pg/Z, [Xbase, XZR, LSL #3]"
  public static unsafe Vector<double> LoadVectorFirstFaulting(Vector<double> mask, double* address);

    /// svint8_t svldff1[_s8](svbool_t pg, const int8_t *base) : "LDFF1B Zresult.B, Pg/Z, [Xarray, Xindex]" or "LDFF1B Zresult.B, Pg/Z, [Xbase, XZR]"
  public static unsafe Vector<sbyte> LoadVectorFirstFaulting(Vector<sbyte> mask, sbyte* address);

    /// svint16_t svldff1[_s16](svbool_t pg, const int16_t *base) : "LDFF1H Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]" or "LDFF1H Zresult.H, Pg/Z, [Xbase, XZR, LSL #1]"
  public static unsafe Vector<short> LoadVectorFirstFaulting(Vector<short> mask, short* address);

    /// svint32_t svldff1[_s32](svbool_t pg, const int32_t *base) : "LDFF1W Zresult.S, Pg/Z, [Xarray, Xindex, LSL #2]" or "LDFF1W Zresult.S, Pg/Z, [Xbase, XZR, LSL #2]"
  public static unsafe Vector<int> LoadVectorFirstFaulting(Vector<int> mask, int* address);

    /// svint64_t svldff1[_s64](svbool_t pg, const int64_t *base) : "LDFF1D Zresult.D, Pg/Z, [Xarray, Xindex, LSL #3]" or "LDFF1D Zresult.D, Pg/Z, [Xbase, XZR, LSL #3]"
  public static unsafe Vector<long> LoadVectorFirstFaulting(Vector<long> mask, long* address);

    /// svuint8_t svldff1[_u8](svbool_t pg, const uint8_t *base) : "LDFF1B Zresult.B, Pg/Z, [Xarray, Xindex]" or "LDFF1B Zresult.B, Pg/Z, [Xbase, XZR]"
  public static unsafe Vector<byte> LoadVectorFirstFaulting(Vector<byte> mask, byte* address);

    /// svuint16_t svldff1[_u16](svbool_t pg, const uint16_t *base) : "LDFF1H Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]" or "LDFF1H Zresult.H, Pg/Z, [Xbase, XZR, LSL #1]"
  public static unsafe Vector<ushort> LoadVectorFirstFaulting(Vector<ushort> mask, ushort* address);

    /// svuint32_t svldff1[_u32](svbool_t pg, const uint32_t *base) : "LDFF1W Zresult.S, Pg/Z, [Xarray, Xindex, LSL #2]" or "LDFF1W Zresult.S, Pg/Z, [Xbase, XZR, LSL #2]"
  public static unsafe Vector<uint> LoadVectorFirstFaulting(Vector<uint> mask, uint* address);

    /// svuint64_t svldff1[_u64](svbool_t pg, const uint64_t *base) : "LDFF1D Zresult.D, Pg/Z, [Xarray, Xindex, LSL #3]" or "LDFF1D Zresult.D, Pg/Z, [Xbase, XZR, LSL #3]"
  public static unsafe Vector<ulong> LoadVectorFirstFaulting(Vector<ulong> mask, ulong* address);


    /// LoadVectorInt16SignExtendFirstFaulting : Load 16-bit data and sign-extend, first-faulting

    /// svint32_t svldff1sh_s32(svbool_t pg, const int16_t *base) : "LDFF1SH Zresult.S, Pg/Z, [Xarray, Xindex, LSL #1]" or "LDFF1SH Zresult.S, Pg/Z, [Xbase, XZR, LSL #1]"
  public static unsafe Vector<int> LoadVectorInt16SignExtendFirstFaulting(Vector<int> mask, short* address);

    /// svint64_t svldff1sh_s64(svbool_t pg, const int16_t *base) : "LDFF1SH Zresult.D, Pg/Z, [Xarray, Xindex, LSL #1]" or "LDFF1SH Zresult.D, Pg/Z, [Xbase, XZR, LSL #1]"
  public static unsafe Vector<long> LoadVectorInt16SignExtendFirstFaulting(Vector<long> mask, short* address);

    /// svuint32_t svldff1sh_u32(svbool_t pg, const int16_t *base) : "LDFF1SH Zresult.S, Pg/Z, [Xarray, Xindex, LSL #1]" or "LDFF1SH Zresult.S, Pg/Z, [Xbase, XZR, LSL #1]"
  public static unsafe Vector<uint> LoadVectorInt16SignExtendFirstFaulting(Vector<uint> mask, short* address);

    /// svuint64_t svldff1sh_u64(svbool_t pg, const int16_t *base) : "LDFF1SH Zresult.D, Pg/Z, [Xarray, Xindex, LSL #1]" or "LDFF1SH Zresult.D, Pg/Z, [Xbase, XZR, LSL #1]"
  public static unsafe Vector<ulong> LoadVectorInt16SignExtendFirstFaulting(Vector<ulong> mask, short* address);


    /// LoadVectorInt32SignExtendFirstFaulting : Load 32-bit data and sign-extend, first-faulting

    /// svint64_t svldff1sw_s64(svbool_t pg, const int32_t *base) : "LDFF1SW Zresult.D, Pg/Z, [Xarray, Xindex, LSL #2]" or "LDFF1SW Zresult.D, Pg/Z, [Xbase, XZR, LSL #2]"
  public static unsafe Vector<long> LoadVectorInt32SignExtendFirstFaulting(Vector<long> mask, int* address);

    /// svuint64_t svldff1sw_u64(svbool_t pg, const int32_t *base) : "LDFF1SW Zresult.D, Pg/Z, [Xarray, Xindex, LSL #2]" or "LDFF1SW Zresult.D, Pg/Z, [Xbase, XZR, LSL #2]"
  public static unsafe Vector<ulong> LoadVectorInt32SignExtendFirstFaulting(Vector<ulong> mask, int* address);


    /// LoadVectorSByteSignExtendFirstFaulting : Load 8-bit data and sign-extend, first-faulting

    /// svint16_t svldff1sb_s16(svbool_t pg, const int8_t *base) : "LDFF1SB Zresult.H, Pg/Z, [Xarray, Xindex]" or "LDFF1SB Zresult.H, Pg/Z, [Xbase, XZR]"
  public static unsafe Vector<short> LoadVectorSByteSignExtendFirstFaulting(Vector<short> mask, sbyte* address);

    /// svint32_t svldff1sb_s32(svbool_t pg, const int8_t *base) : "LDFF1SB Zresult.S, Pg/Z, [Xarray, Xindex]" or "LDFF1SB Zresult.S, Pg/Z, [Xbase, XZR]"
  public static unsafe Vector<int> LoadVectorSByteSignExtendFirstFaulting(Vector<int> mask, sbyte* address);

    /// svint64_t svldff1sb_s64(svbool_t pg, const int8_t *base) : "LDFF1SB Zresult.D, Pg/Z, [Xarray, Xindex]" or "LDFF1SB Zresult.D, Pg/Z, [Xbase, XZR]"
  public static unsafe Vector<long> LoadVectorSByteSignExtendFirstFaulting(Vector<long> mask, sbyte* address);

    /// svuint16_t svldff1sb_u16(svbool_t pg, const int8_t *base) : "LDFF1SB Zresult.H, Pg/Z, [Xarray, Xindex]" or "LDFF1SB Zresult.H, Pg/Z, [Xbase, XZR]"
  public static unsafe Vector<ushort> LoadVectorSByteSignExtendFirstFaulting(Vector<ushort> mask, sbyte* address);

    /// svuint32_t svldff1sb_u32(svbool_t pg, const int8_t *base) : "LDFF1SB Zresult.S, Pg/Z, [Xarray, Xindex]" or "LDFF1SB Zresult.S, Pg/Z, [Xbase, XZR]"
  public static unsafe Vector<uint> LoadVectorSByteSignExtendFirstFaulting(Vector<uint> mask, sbyte* address);

    /// svuint64_t svldff1sb_u64(svbool_t pg, const int8_t *base) : "LDFF1SB Zresult.D, Pg/Z, [Xarray, Xindex]" or "LDFF1SB Zresult.D, Pg/Z, [Xbase, XZR]"
  public static unsafe Vector<ulong> LoadVectorSByteSignExtendFirstFaulting(Vector<ulong> mask, sbyte* address);


    /// LoadVectorUInt16ZeroExtendFirstFaulting : Load 16-bit data and zero-extend, first-faulting

    /// svint32_t svldff1uh_s32(svbool_t pg, const uint16_t *base) : "LDFF1H Zresult.S, Pg/Z, [Xarray, Xindex, LSL #1]" or "LDFF1H Zresult.S, Pg/Z, [Xbase, XZR, LSL #1]"
  public static unsafe Vector<int> LoadVectorUInt16ZeroExtendFirstFaulting(Vector<int> mask, ushort* address);

    /// svint64_t svldff1uh_s64(svbool_t pg, const uint16_t *base) : "LDFF1H Zresult.D, Pg/Z, [Xarray, Xindex, LSL #1]" or "LDFF1H Zresult.D, Pg/Z, [Xbase, XZR, LSL #1]"
  public static unsafe Vector<long> LoadVectorUInt16ZeroExtendFirstFaulting(Vector<long> mask, ushort* address);

    /// svuint32_t svldff1uh_u32(svbool_t pg, const uint16_t *base) : "LDFF1H Zresult.S, Pg/Z, [Xarray, Xindex, LSL #1]" or "LDFF1H Zresult.S, Pg/Z, [Xbase, XZR, LSL #1]"
  public static unsafe Vector<uint> LoadVectorUInt16ZeroExtendFirstFaulting(Vector<uint> mask, ushort* address);

    /// svuint64_t svldff1uh_u64(svbool_t pg, const uint16_t *base) : "LDFF1H Zresult.D, Pg/Z, [Xarray, Xindex, LSL #1]" or "LDFF1H Zresult.D, Pg/Z, [Xbase, XZR, LSL #1]"
  public static unsafe Vector<ulong> LoadVectorUInt16ZeroExtendFirstFaulting(Vector<ulong> mask, ushort* address);


    /// LoadVectorUInt32ZeroExtendFirstFaulting : Load 32-bit data and zero-extend, first-faulting

    /// svint64_t svldff1uw_s64(svbool_t pg, const uint32_t *base) : "LDFF1W Zresult.D, Pg/Z, [Xarray, Xindex, LSL #2]" or "LDFF1W Zresult.D, Pg/Z, [Xbase, XZR, LSL #2]"
  public static unsafe Vector<long> LoadVectorUInt32ZeroExtendFirstFaulting(Vector<long> mask, uint* address);

    /// svuint64_t svldff1uw_u64(svbool_t pg, const uint32_t *base) : "LDFF1W Zresult.D, Pg/Z, [Xarray, Xindex, LSL #2]" or "LDFF1W Zresult.D, Pg/Z, [Xbase, XZR, LSL #2]"
  public static unsafe Vector<ulong> LoadVectorUInt32ZeroExtendFirstFaulting(Vector<ulong> mask, uint* address);


    /// SetFfr : Write to the first-fault register

    /// void svwrffr(svbool_t op) : "WRFFR Pop.B"
  public static unsafe void SetFfr(Vector<sbyte> value);

    /// void svwrffr(svbool_t op) : "WRFFR Pop.B"
  public static unsafe void SetFfr(Vector<short> value);

    /// void svwrffr(svbool_t op) : "WRFFR Pop.B"
  public static unsafe void SetFfr(Vector<int> value);

    /// void svwrffr(svbool_t op) : "WRFFR Pop.B"
  public static unsafe void SetFfr(Vector<long> value);

    /// void svwrffr(svbool_t op) : "WRFFR Pop.B"
  public static unsafe void SetFfr(Vector<byte> value);

    /// void svwrffr(svbool_t op) : "WRFFR Pop.B"
  public static unsafe void SetFfr(Vector<ushort> value);

    /// void svwrffr(svbool_t op) : "WRFFR Pop.B"
  public static unsafe void SetFfr(Vector<uint> value);

    /// void svwrffr(svbool_t op) : "WRFFR Pop.B"
  public static unsafe void SetFfr(Vector<ulong> value);


  /// total method signatures: 184
  /// total method names:      22
}


  /// Rejected:
  ///   public static unsafe void ClearFfr(); // svsetffr
  ///   public static unsafe Vector<int> GatherVectorByteZeroExtendFirstFaulting(Vector<int> mask, Vector<uint> addresses, long offset); // svldff1ub_gather[_u32base]_offset_s32
  ///   public static unsafe Vector<uint> GatherVectorByteZeroExtendFirstFaulting(Vector<uint> mask, Vector<uint> addresses, long offset); // svldff1ub_gather[_u32base]_offset_u32
  ///   public static unsafe Vector<long> GatherVectorByteZeroExtendFirstFaulting(Vector<long> mask, Vector<ulong> addresses, long offset); // svldff1ub_gather[_u64base]_offset_s64
  ///   public static unsafe Vector<ulong> GatherVectorByteZeroExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> addresses, long offset); // svldff1ub_gather[_u64base]_offset_u64
  ///   public static unsafe Vector<float> GatherVectorFirstFaulting(Vector<float> mask, Vector<uint> addresses, long index); // svldff1_gather[_u32base]_index_f32
  ///   public static unsafe Vector<int> GatherVectorFirstFaulting(Vector<int> mask, Vector<uint> addresses, long index); // svldff1_gather[_u32base]_index_s32
  ///   public static unsafe Vector<uint> GatherVectorFirstFaulting(Vector<uint> mask, Vector<uint> addresses, long index); // svldff1_gather[_u32base]_index_u32
  ///   public static unsafe Vector<double> GatherVectorFirstFaulting(Vector<double> mask, Vector<ulong> addresses, long index); // svldff1_gather[_u64base]_index_f64
  ///   public static unsafe Vector<long> GatherVectorFirstFaulting(Vector<long> mask, Vector<ulong> addresses, long index); // svldff1_gather[_u64base]_index_s64
  ///   public static unsafe Vector<ulong> GatherVectorFirstFaulting(Vector<ulong> mask, Vector<ulong> addresses, long index); // svldff1_gather[_u64base]_index_u64
  ///   public static unsafe Vector<int> GatherVectorInt16SignExtendFirstFaulting(Vector<int> mask, Vector<uint> addresses, long index); // svldff1sh_gather[_u32base]_index_s32
  ///   public static unsafe Vector<uint> GatherVectorInt16SignExtendFirstFaulting(Vector<uint> mask, Vector<uint> addresses, long index); // svldff1sh_gather[_u32base]_index_u32
  ///   public static unsafe Vector<long> GatherVectorInt16SignExtendFirstFaulting(Vector<long> mask, Vector<ulong> addresses, long index); // svldff1sh_gather[_u64base]_index_s64
  ///   public static unsafe Vector<ulong> GatherVectorInt16SignExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> addresses, long index); // svldff1sh_gather[_u64base]_index_u64
  ///   public static unsafe Vector<int> GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(Vector<int> mask, Vector<uint> addresses, long offset); // svldff1sh_gather[_u32base]_offset_s32
  ///   public static unsafe Vector<uint> GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(Vector<uint> mask, Vector<uint> addresses, long offset); // svldff1sh_gather[_u32base]_offset_u32
  ///   public static unsafe Vector<long> GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(Vector<long> mask, Vector<ulong> addresses, long offset); // svldff1sh_gather[_u64base]_offset_s64
  ///   public static unsafe Vector<ulong> GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> addresses, long offset); // svldff1sh_gather[_u64base]_offset_u64
  ///   public static unsafe Vector<long> GatherVectorInt32SignExtendFirstFaulting(Vector<long> mask, Vector<ulong> addresses, long index); // svldff1sw_gather[_u64base]_index_s64
  ///   public static unsafe Vector<int> GatherVectorInt32SignExtendFirstFaulting(Vector<int> mask, Vector<uint> addresses, long index); // svldff1sw_gather[_u64base]_index_s64
  ///   public static unsafe Vector<ulong> GatherVectorInt32SignExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> addresses, long index); // svldff1sw_gather[_u64base]_index_u64
  ///   public static unsafe Vector<uint> GatherVectorInt32SignExtendFirstFaulting(Vector<uint> mask, Vector<uint> addresses, long index); // svldff1sw_gather[_u64base]_index_u64
  ///   public static unsafe Vector<long> GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(Vector<long> mask, Vector<ulong> addresses, long offset); // svldff1sw_gather[_u64base]_offset_s64
  ///   public static unsafe Vector<int> GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(Vector<int> mask, Vector<uint> addresses, long offset); // svldff1sw_gather[_u64base]_offset_s64
  ///   public static unsafe Vector<ulong> GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> addresses, long offset); // svldff1sw_gather[_u64base]_offset_u64
  ///   public static unsafe Vector<uint> GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(Vector<uint> mask, Vector<uint> addresses, long offset); // svldff1sw_gather[_u64base]_offset_u64
  ///   public static unsafe Vector<int> GatherVectorSByteSignExtendFirstFaulting(Vector<int> mask, Vector<uint> addresses, long offset); // svldff1sb_gather[_u32base]_offset_s32
  ///   public static unsafe Vector<uint> GatherVectorSByteSignExtendFirstFaulting(Vector<uint> mask, Vector<uint> addresses, long offset); // svldff1sb_gather[_u32base]_offset_u32
  ///   public static unsafe Vector<long> GatherVectorSByteSignExtendFirstFaulting(Vector<long> mask, Vector<ulong> addresses, long offset); // svldff1sb_gather[_u64base]_offset_s64
  ///   public static unsafe Vector<ulong> GatherVectorSByteSignExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> addresses, long offset); // svldff1sb_gather[_u64base]_offset_u64
  ///   public static unsafe Vector<int> GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(Vector<int> mask, Vector<uint> addresses, long offset); // svldff1uh_gather[_u32base]_offset_s32
  ///   public static unsafe Vector<uint> GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(Vector<uint> mask, Vector<uint> addresses, long offset); // svldff1uh_gather[_u32base]_offset_u32
  ///   public static unsafe Vector<long> GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(Vector<long> mask, Vector<ulong> addresses, long offset); // svldff1uh_gather[_u64base]_offset_s64
  ///   public static unsafe Vector<ulong> GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> addresses, long offset); // svldff1uh_gather[_u64base]_offset_u64
  ///   public static unsafe Vector<int> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<int> mask, Vector<uint> addresses, long index); // svldff1uh_gather[_u32base]_index_s32
  ///   public static unsafe Vector<uint> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<uint> mask, Vector<uint> addresses, long index); // svldff1uh_gather[_u32base]_index_u32
  ///   public static unsafe Vector<long> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<long> mask, Vector<ulong> addresses, long index); // svldff1uh_gather[_u64base]_index_s64
  ///   public static unsafe Vector<ulong> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> addresses, long index); // svldff1uh_gather[_u64base]_index_u64
  ///   public static unsafe Vector<long> GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(Vector<long> mask, Vector<ulong> addresses, long offset); // svldff1uw_gather[_u64base]_offset_s64
  ///   public static unsafe Vector<int> GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(Vector<int> mask, Vector<uint> addresses, long offset); // svldff1uw_gather[_u64base]_offset_s64
  ///   public static unsafe Vector<ulong> GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> addresses, long offset); // svldff1uw_gather[_u64base]_offset_u64
  ///   public static unsafe Vector<uint> GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(Vector<uint> mask, Vector<uint> addresses, long offset); // svldff1uw_gather[_u64base]_offset_u64
  ///   public static unsafe Vector<long> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<long> mask, Vector<ulong> addresses, long index); // svldff1uw_gather[_u64base]_index_s64
  ///   public static unsafe Vector<int> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<int> mask, Vector<uint> addresses, long index); // svldff1uw_gather[_u64base]_index_s64
  ///   public static unsafe Vector<ulong> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> addresses, long index); // svldff1uw_gather[_u64base]_index_u64
  ///   public static unsafe Vector<uint> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<uint> mask, Vector<uint> addresses, long index); // svldff1uw_gather[_u64base]_index_u64
  ///   public static unsafe Vector<float> GatherVectorWithByteOffsetFirstFaulting(Vector<float> mask, Vector<uint> addresses, long offset); // svldff1_gather[_u32base]_offset_f32
  ///   public static unsafe Vector<int> GatherVectorWithByteOffsetFirstFaulting(Vector<int> mask, Vector<uint> addresses, long offset); // svldff1_gather[_u32base]_offset_s32
  ///   public static unsafe Vector<uint> GatherVectorWithByteOffsetFirstFaulting(Vector<uint> mask, Vector<uint> addresses, long offset); // svldff1_gather[_u32base]_offset_u32
  ///   public static unsafe Vector<double> GatherVectorWithByteOffsetFirstFaulting(Vector<double> mask, Vector<ulong> addresses, long offset); // svldff1_gather[_u64base]_offset_f64
  ///   public static unsafe Vector<long> GatherVectorWithByteOffsetFirstFaulting(Vector<long> mask, Vector<ulong> addresses, long offset); // svldff1_gather[_u64base]_offset_s64
  ///   public static unsafe Vector<ulong> GatherVectorWithByteOffsetFirstFaulting(Vector<ulong> mask, Vector<ulong> addresses, long offset); // svldff1_gather[_u64base]_offset_u64
  ///   public static unsafe Vector<short> LoadVectorByteZeroExtendFirstFaulting(Vector<short> mask, byte* address, long vnum); // svldff1ub_vnum_s16
  ///   public static unsafe Vector<int> LoadVectorByteZeroExtendFirstFaulting(Vector<int> mask, byte* address, long vnum); // svldff1ub_vnum_s32
  ///   public static unsafe Vector<long> LoadVectorByteZeroExtendFirstFaulting(Vector<long> mask, byte* address, long vnum); // svldff1ub_vnum_s64
  ///   public static unsafe Vector<ushort> LoadVectorByteZeroExtendFirstFaulting(Vector<ushort> mask, byte* address, long vnum); // svldff1ub_vnum_u16
  ///   public static unsafe Vector<uint> LoadVectorByteZeroExtendFirstFaulting(Vector<uint> mask, byte* address, long vnum); // svldff1ub_vnum_u32
  ///   public static unsafe Vector<ulong> LoadVectorByteZeroExtendFirstFaulting(Vector<ulong> mask, byte* address, long vnum); // svldff1ub_vnum_u64
  ///   public static unsafe Vector<float> LoadVectorFirstFaulting(Vector<float> mask, float* address, long vnum); // svldff1_vnum[_f32]
  ///   public static unsafe Vector<double> LoadVectorFirstFaulting(Vector<double> mask, double* address, long vnum); // svldff1_vnum[_f64]
  ///   public static unsafe Vector<sbyte> LoadVectorFirstFaulting(Vector<sbyte> mask, sbyte* address, long vnum); // svldff1_vnum[_s8]
  ///   public static unsafe Vector<short> LoadVectorFirstFaulting(Vector<short> mask, short* address, long vnum); // svldff1_vnum[_s16]
  ///   public static unsafe Vector<int> LoadVectorFirstFaulting(Vector<int> mask, int* address, long vnum); // svldff1_vnum[_s32]
  ///   public static unsafe Vector<long> LoadVectorFirstFaulting(Vector<long> mask, long* address, long vnum); // svldff1_vnum[_s64]
  ///   public static unsafe Vector<byte> LoadVectorFirstFaulting(Vector<byte> mask, byte* address, long vnum); // svldff1_vnum[_u8]
  ///   public static unsafe Vector<ushort> LoadVectorFirstFaulting(Vector<ushort> mask, ushort* address, long vnum); // svldff1_vnum[_u16]
  ///   public static unsafe Vector<uint> LoadVectorFirstFaulting(Vector<uint> mask, uint* address, long vnum); // svldff1_vnum[_u32]
  ///   public static unsafe Vector<ulong> LoadVectorFirstFaulting(Vector<ulong> mask, ulong* address, long vnum); // svldff1_vnum[_u64]
  ///   public static unsafe Vector<int> LoadVectorInt16SignExtendFirstFaulting(Vector<int> mask, short* address, long vnum); // svldff1sh_vnum_s32
  ///   public static unsafe Vector<long> LoadVectorInt16SignExtendFirstFaulting(Vector<long> mask, short* address, long vnum); // svldff1sh_vnum_s64
  ///   public static unsafe Vector<uint> LoadVectorInt16SignExtendFirstFaulting(Vector<uint> mask, short* address, long vnum); // svldff1sh_vnum_u32
  ///   public static unsafe Vector<ulong> LoadVectorInt16SignExtendFirstFaulting(Vector<ulong> mask, short* address, long vnum); // svldff1sh_vnum_u64
  ///   public static unsafe Vector<long> LoadVectorInt32SignExtendFirstFaulting(Vector<long> mask, int* address, long vnum); // svldff1sw_vnum_s64
  ///   public static unsafe Vector<ulong> LoadVectorInt32SignExtendFirstFaulting(Vector<ulong> mask, int* address, long vnum); // svldff1sw_vnum_u64
  ///   public static unsafe Vector<short> LoadVectorSByteSignExtendFirstFaulting(Vector<short> mask, sbyte* address, long vnum); // svldff1sb_vnum_s16
  ///   public static unsafe Vector<int> LoadVectorSByteSignExtendFirstFaulting(Vector<int> mask, sbyte* address, long vnum); // svldff1sb_vnum_s32
  ///   public static unsafe Vector<long> LoadVectorSByteSignExtendFirstFaulting(Vector<long> mask, sbyte* address, long vnum); // svldff1sb_vnum_s64
  ///   public static unsafe Vector<ushort> LoadVectorSByteSignExtendFirstFaulting(Vector<ushort> mask, sbyte* address, long vnum); // svldff1sb_vnum_u16
  ///   public static unsafe Vector<uint> LoadVectorSByteSignExtendFirstFaulting(Vector<uint> mask, sbyte* address, long vnum); // svldff1sb_vnum_u32
  ///   public static unsafe Vector<ulong> LoadVectorSByteSignExtendFirstFaulting(Vector<ulong> mask, sbyte* address, long vnum); // svldff1sb_vnum_u64
  ///   public static unsafe Vector<int> LoadVectorUInt16ZeroExtendFirstFaulting(Vector<int> mask, ushort* address, long vnum); // svldff1uh_vnum_s32
  ///   public static unsafe Vector<long> LoadVectorUInt16ZeroExtendFirstFaulting(Vector<long> mask, ushort* address, long vnum); // svldff1uh_vnum_s64
  ///   public static unsafe Vector<uint> LoadVectorUInt16ZeroExtendFirstFaulting(Vector<uint> mask, ushort* address, long vnum); // svldff1uh_vnum_u32
  ///   public static unsafe Vector<ulong> LoadVectorUInt16ZeroExtendFirstFaulting(Vector<ulong> mask, ushort* address, long vnum); // svldff1uh_vnum_u64
  ///   public static unsafe Vector<long> LoadVectorUInt32ZeroExtendFirstFaulting(Vector<long> mask, uint* address, long vnum); // svldff1uw_vnum_s64
  ///   public static unsafe Vector<ulong> LoadVectorUInt32ZeroExtendFirstFaulting(Vector<ulong> mask, uint* address, long vnum); // svldff1uw_vnum_u64
  ///   Total Rejected: 87

  /// Total ACLE covered across API:      279

