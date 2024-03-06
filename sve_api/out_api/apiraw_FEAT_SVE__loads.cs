namespace System.Runtime.Intrinsics.Arm;

/// VectorT Summary
public abstract partial class Sve : AdvSimd /// Feature: FEAT_SVE  Category: loads
{

  /// T: [uint, int], [ulong, long]
  public static unsafe Vector<T> Compute16BitAddresses(Vector<T> bases, Vector<T2> indices); // ADR

  /// T: uint, ulong
  public static unsafe Vector<T> Compute16BitAddresses(Vector<T> bases, Vector<T> indices); // ADR

  /// T: [uint, int], [ulong, long]
  public static unsafe Vector<T> Compute32BitAddresses(Vector<T> bases, Vector<T2> indices); // ADR

  /// T: uint, ulong
  public static unsafe Vector<T> Compute32BitAddresses(Vector<T> bases, Vector<T> indices); // ADR

  /// T: [uint, int], [ulong, long]
  public static unsafe Vector<T> Compute64BitAddresses(Vector<T> bases, Vector<T2> indices); // ADR

  /// T: uint, ulong
  public static unsafe Vector<T> Compute64BitAddresses(Vector<T> bases, Vector<T> indices); // ADR

  /// T: [uint, int], [ulong, long]
  public static unsafe Vector<T> Compute8BitAddresses(Vector<T> bases, Vector<T2> indices); // ADR

  /// T: uint, ulong
  public static unsafe Vector<T> Compute8BitAddresses(Vector<T> bases, Vector<T> indices); // ADR

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> LoadVector(Vector<T> mask, T* address); // LD1W or LD1D or LD1B or LD1H

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> LoadVector128AndReplicateToVector(Vector<T> mask, T* address); // LD1RQW or LD1RQD or LD1RQB or LD1RQH

  public static unsafe Vector<short> LoadVectorByteNonFaultingZeroExtendToInt16(byte* address); // LDNF1B // predicated

  public static unsafe Vector<int> LoadVectorByteNonFaultingZeroExtendToInt32(byte* address); // LDNF1B // predicated

  public static unsafe Vector<long> LoadVectorByteNonFaultingZeroExtendToInt64(byte* address); // LDNF1B // predicated

  public static unsafe Vector<ushort> LoadVectorByteNonFaultingZeroExtendToUInt16(byte* address); // LDNF1B // predicated

  public static unsafe Vector<uint> LoadVectorByteNonFaultingZeroExtendToUInt32(byte* address); // LDNF1B // predicated

  public static unsafe Vector<ulong> LoadVectorByteNonFaultingZeroExtendToUInt64(byte* address); // LDNF1B // predicated

  public static unsafe Vector<short> LoadVectorByteZeroExtendToInt16(Vector<short> mask, byte* address); // LD1B

  public static unsafe Vector<int> LoadVectorByteZeroExtendToInt32(Vector<int> mask, byte* address); // LD1B

  public static unsafe Vector<long> LoadVectorByteZeroExtendToInt64(Vector<long> mask, byte* address); // LD1B

  public static unsafe Vector<ushort> LoadVectorByteZeroExtendToUInt16(Vector<ushort> mask, byte* address); // LD1B

  public static unsafe Vector<uint> LoadVectorByteZeroExtendToUInt32(Vector<uint> mask, byte* address); // LD1B

  public static unsafe Vector<ulong> LoadVectorByteZeroExtendToUInt64(Vector<ulong> mask, byte* address); // LD1B

  public static unsafe Vector<int> LoadVectorInt16NonFaultingSignExtendToInt32(short* address); // LDNF1SH // predicated

  public static unsafe Vector<long> LoadVectorInt16NonFaultingSignExtendToInt64(short* address); // LDNF1SH // predicated

  public static unsafe Vector<uint> LoadVectorInt16NonFaultingSignExtendToUInt32(short* address); // LDNF1SH // predicated

  public static unsafe Vector<ulong> LoadVectorInt16NonFaultingSignExtendToUInt64(short* address); // LDNF1SH // predicated

  public static unsafe Vector<int> LoadVectorInt16SignExtendToInt32(Vector<int> mask, short* address); // LD1SH

  public static unsafe Vector<long> LoadVectorInt16SignExtendToInt64(Vector<long> mask, short* address); // LD1SH

  public static unsafe Vector<uint> LoadVectorInt16SignExtendToUInt32(Vector<uint> mask, short* address); // LD1SH

  public static unsafe Vector<ulong> LoadVectorInt16SignExtendToUInt64(Vector<ulong> mask, short* address); // LD1SH

  public static unsafe Vector<long> LoadVectorInt32NonFaultingSignExtendToInt64(int* address); // LDNF1SW // predicated

  public static unsafe Vector<ulong> LoadVectorInt32NonFaultingSignExtendToUInt64(int* address); // LDNF1SW // predicated

  public static unsafe Vector<long> LoadVectorInt32SignExtendToInt64(Vector<long> mask, int* address); // LD1SW

  public static unsafe Vector<ulong> LoadVectorInt32SignExtendToUInt64(Vector<ulong> mask, int* address); // LD1SW

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> LoadVectorNonFaulting(T* address); // LDNF1W or LDNF1D or LDNF1B or LDNF1H // predicated

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> LoadVectorNonTemporal(Vector<T> mask, T* address); // LDNT1W or LDNT1D or LDNT1B or LDNT1H

  public static unsafe Vector<short> LoadVectorSByteNonFaultingSignExtendToInt16(sbyte* address); // LDNF1SB // predicated

  public static unsafe Vector<int> LoadVectorSByteNonFaultingSignExtendToInt32(sbyte* address); // LDNF1SB // predicated

  public static unsafe Vector<long> LoadVectorSByteNonFaultingSignExtendToInt64(sbyte* address); // LDNF1SB // predicated

  public static unsafe Vector<ushort> LoadVectorSByteNonFaultingSignExtendToUInt16(sbyte* address); // LDNF1SB // predicated

  public static unsafe Vector<uint> LoadVectorSByteNonFaultingSignExtendToUInt32(sbyte* address); // LDNF1SB // predicated

  public static unsafe Vector<ulong> LoadVectorSByteNonFaultingSignExtendToUInt64(sbyte* address); // LDNF1SB // predicated

  public static unsafe Vector<short> LoadVectorSByteSignExtendToInt16(Vector<short> mask, sbyte* address); // LD1SB

  public static unsafe Vector<int> LoadVectorSByteSignExtendToInt32(Vector<int> mask, sbyte* address); // LD1SB

  public static unsafe Vector<long> LoadVectorSByteSignExtendToInt64(Vector<long> mask, sbyte* address); // LD1SB

  public static unsafe Vector<ushort> LoadVectorSByteSignExtendToUInt16(Vector<ushort> mask, sbyte* address); // LD1SB

  public static unsafe Vector<uint> LoadVectorSByteSignExtendToUInt32(Vector<uint> mask, sbyte* address); // LD1SB

  public static unsafe Vector<ulong> LoadVectorSByteSignExtendToUInt64(Vector<ulong> mask, sbyte* address); // LD1SB

  public static unsafe Vector<int> LoadVectorUInt16NonFaultingZeroExtendToInt32(ushort* address); // LDNF1H // predicated

  public static unsafe Vector<long> LoadVectorUInt16NonFaultingZeroExtendToInt64(ushort* address); // LDNF1H // predicated

  public static unsafe Vector<uint> LoadVectorUInt16NonFaultingZeroExtendToUInt32(ushort* address); // LDNF1H // predicated

  public static unsafe Vector<ulong> LoadVectorUInt16NonFaultingZeroExtendToUInt64(ushort* address); // LDNF1H // predicated

  public static unsafe Vector<int> LoadVectorUInt16ZeroExtendToInt32(Vector<int> mask, ushort* address); // LD1H

  public static unsafe Vector<long> LoadVectorUInt16ZeroExtendToInt64(Vector<long> mask, ushort* address); // LD1H

  public static unsafe Vector<uint> LoadVectorUInt16ZeroExtendToUInt32(Vector<uint> mask, ushort* address); // LD1H

  public static unsafe Vector<ulong> LoadVectorUInt16ZeroExtendToUInt64(Vector<ulong> mask, ushort* address); // LD1H

  public static unsafe Vector<long> LoadVectorUInt32NonFaultingZeroExtendToInt64(uint* address); // LDNF1W // predicated

  public static unsafe Vector<ulong> LoadVectorUInt32NonFaultingZeroExtendToUInt64(uint* address); // LDNF1W // predicated

  public static unsafe Vector<long> LoadVectorUInt32ZeroExtendToInt64(Vector<long> mask, uint* address); // LD1W

  public static unsafe Vector<ulong> LoadVectorUInt32ZeroExtendToUInt64(Vector<ulong> mask, uint* address); // LD1W

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe (Vector<T>, Vector<T>) LoadVectorx2(Vector<T> mask, T* address); // LD2W or LD2D or LD2B or LD2H

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe (Vector<T>, Vector<T>, Vector<T>) LoadVectorx3(Vector<T> mask, T* address); // LD3W or LD3D or LD3B or LD3H

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe (Vector<T>, Vector<T>, Vector<T>, Vector<T>) LoadVectorx4(Vector<T> mask, T* address); // LD4W or LD4D or LD4B or LD4H

  public static unsafe void PrefetchBytes(Vector<byte> mask, void* address, [ConstantExpected] SvePrefetchType prefetchType); // PRFB

  public static unsafe void PrefetchInt16(Vector<ushort> mask, void* address, [ConstantExpected] SvePrefetchType prefetchType); // PRFH

  public static unsafe void PrefetchInt32(Vector<uint> mask, void* address, [ConstantExpected] SvePrefetchType prefetchType); // PRFW

  public static unsafe void PrefetchInt64(Vector<ulong> mask, void* address, [ConstantExpected] SvePrefetchType prefetchType); // PRFD


public enum SvePrefetchType
{
    LoadL1Temporal = 0,
    LoadL1NonTemporal = 1,
    LoadL2Temporal = 2,
    LoadL2NonTemporal = 3,
    LoadL3Temporal = 4,
    LoadL3NonTemporal = 5,
    StoreL1Temporal = 8,
    StoreL1NonTemporal = 9,
    StoreL2Temporal = 10,
    StoreL2NonTemporal = 11,
    StoreL3Temporal = 12,
    StoreL3NonTemporal = 13
};

  /// total method signatures: 67

}


/// Full API
public abstract partial class Sve : AdvSimd /// Feature: FEAT_SVE  Category: loads
{
    /// Compute16BitAddresses : Compute vector addresses for 16-bit data

    /// svuint32_t svadrh[_u32base]_[s32]index(svuint32_t bases, svint32_t indices) : "ADR Zresult.S, [Zbases.S, Zindices.S, LSL #1]"
  public static unsafe Vector<uint> Compute16BitAddresses(Vector<uint> bases, Vector<int> indices);

    /// svuint32_t svadrh[_u32base]_[u32]index(svuint32_t bases, svuint32_t indices) : "ADR Zresult.S, [Zbases.S, Zindices.S, LSL #1]"
  public static unsafe Vector<uint> Compute16BitAddresses(Vector<uint> bases, Vector<uint> indices);

    /// svuint64_t svadrh[_u64base]_[s64]index(svuint64_t bases, svint64_t indices) : "ADR Zresult.D, [Zbases.D, Zindices.D, LSL #1]"
  public static unsafe Vector<ulong> Compute16BitAddresses(Vector<ulong> bases, Vector<long> indices);

    /// svuint64_t svadrh[_u64base]_[u64]index(svuint64_t bases, svuint64_t indices) : "ADR Zresult.D, [Zbases.D, Zindices.D, LSL #1]"
  public static unsafe Vector<ulong> Compute16BitAddresses(Vector<ulong> bases, Vector<ulong> indices);


    /// Compute32BitAddresses : Compute vector addresses for 32-bit data

    /// svuint32_t svadrw[_u32base]_[s32]index(svuint32_t bases, svint32_t indices) : "ADR Zresult.S, [Zbases.S, Zindices.S, LSL #2]"
  public static unsafe Vector<uint> Compute32BitAddresses(Vector<uint> bases, Vector<int> indices);

    /// svuint32_t svadrw[_u32base]_[u32]index(svuint32_t bases, svuint32_t indices) : "ADR Zresult.S, [Zbases.S, Zindices.S, LSL #2]"
  public static unsafe Vector<uint> Compute32BitAddresses(Vector<uint> bases, Vector<uint> indices);

    /// svuint64_t svadrw[_u64base]_[s64]index(svuint64_t bases, svint64_t indices) : "ADR Zresult.D, [Zbases.D, Zindices.D, LSL #2]"
  public static unsafe Vector<ulong> Compute32BitAddresses(Vector<ulong> bases, Vector<long> indices);

    /// svuint64_t svadrw[_u64base]_[u64]index(svuint64_t bases, svuint64_t indices) : "ADR Zresult.D, [Zbases.D, Zindices.D, LSL #2]"
  public static unsafe Vector<ulong> Compute32BitAddresses(Vector<ulong> bases, Vector<ulong> indices);


    /// Compute64BitAddresses : Compute vector addresses for 64-bit data

    /// svuint32_t svadrd[_u32base]_[s32]index(svuint32_t bases, svint32_t indices) : "ADR Zresult.S, [Zbases.S, Zindices.S, LSL #3]"
  public static unsafe Vector<uint> Compute64BitAddresses(Vector<uint> bases, Vector<int> indices);

    /// svuint32_t svadrd[_u32base]_[u32]index(svuint32_t bases, svuint32_t indices) : "ADR Zresult.S, [Zbases.S, Zindices.S, LSL #3]"
  public static unsafe Vector<uint> Compute64BitAddresses(Vector<uint> bases, Vector<uint> indices);

    /// svuint64_t svadrd[_u64base]_[s64]index(svuint64_t bases, svint64_t indices) : "ADR Zresult.D, [Zbases.D, Zindices.D, LSL #3]"
  public static unsafe Vector<ulong> Compute64BitAddresses(Vector<ulong> bases, Vector<long> indices);

    /// svuint64_t svadrd[_u64base]_[u64]index(svuint64_t bases, svuint64_t indices) : "ADR Zresult.D, [Zbases.D, Zindices.D, LSL #3]"
  public static unsafe Vector<ulong> Compute64BitAddresses(Vector<ulong> bases, Vector<ulong> indices);


    /// Compute8BitAddresses : Compute vector addresses for 8-bit data

    /// svuint32_t svadrb[_u32base]_[s32]offset(svuint32_t bases, svint32_t offsets) : "ADR Zresult.S, [Zbases.S, Zoffsets.S]"
  public static unsafe Vector<uint> Compute8BitAddresses(Vector<uint> bases, Vector<int> indices);

    /// svuint32_t svadrb[_u32base]_[u32]offset(svuint32_t bases, svuint32_t offsets) : "ADR Zresult.S, [Zbases.S, Zoffsets.S]"
  public static unsafe Vector<uint> Compute8BitAddresses(Vector<uint> bases, Vector<uint> indices);

    /// svuint64_t svadrb[_u64base]_[s64]offset(svuint64_t bases, svint64_t offsets) : "ADR Zresult.D, [Zbases.D, Zoffsets.D]"
  public static unsafe Vector<ulong> Compute8BitAddresses(Vector<ulong> bases, Vector<long> indices);

    /// svuint64_t svadrb[_u64base]_[u64]offset(svuint64_t bases, svuint64_t offsets) : "ADR Zresult.D, [Zbases.D, Zoffsets.D]"
  public static unsafe Vector<ulong> Compute8BitAddresses(Vector<ulong> bases, Vector<ulong> indices);


    /// LoadVector : Unextended load

    /// svfloat32_t svld1[_f32](svbool_t pg, const float32_t *base) : "LD1W Zresult.S, Pg/Z, [Xarray, Xindex, LSL #2]" or "LD1W Zresult.S, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<float> LoadVector(Vector<float> mask, float* address);

    /// svfloat64_t svld1[_f64](svbool_t pg, const float64_t *base) : "LD1D Zresult.D, Pg/Z, [Xarray, Xindex, LSL #3]" or "LD1D Zresult.D, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<double> LoadVector(Vector<double> mask, double* address);

    /// svint8_t svld1[_s8](svbool_t pg, const int8_t *base) : "LD1B Zresult.B, Pg/Z, [Xarray, Xindex]" or "LD1B Zresult.B, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<sbyte> LoadVector(Vector<sbyte> mask, sbyte* address);

    /// svint16_t svld1[_s16](svbool_t pg, const int16_t *base) : "LD1H Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]" or "LD1H Zresult.H, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<short> LoadVector(Vector<short> mask, short* address);

    /// svint32_t svld1[_s32](svbool_t pg, const int32_t *base) : "LD1W Zresult.S, Pg/Z, [Xarray, Xindex, LSL #2]" or "LD1W Zresult.S, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<int> LoadVector(Vector<int> mask, int* address);

    /// svint64_t svld1[_s64](svbool_t pg, const int64_t *base) : "LD1D Zresult.D, Pg/Z, [Xarray, Xindex, LSL #3]" or "LD1D Zresult.D, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<long> LoadVector(Vector<long> mask, long* address);

    /// svuint8_t svld1[_u8](svbool_t pg, const uint8_t *base) : "LD1B Zresult.B, Pg/Z, [Xarray, Xindex]" or "LD1B Zresult.B, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<byte> LoadVector(Vector<byte> mask, byte* address);

    /// svuint16_t svld1[_u16](svbool_t pg, const uint16_t *base) : "LD1H Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]" or "LD1H Zresult.H, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<ushort> LoadVector(Vector<ushort> mask, ushort* address);

    /// svuint32_t svld1[_u32](svbool_t pg, const uint32_t *base) : "LD1W Zresult.S, Pg/Z, [Xarray, Xindex, LSL #2]" or "LD1W Zresult.S, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<uint> LoadVector(Vector<uint> mask, uint* address);

    /// svuint64_t svld1[_u64](svbool_t pg, const uint64_t *base) : "LD1D Zresult.D, Pg/Z, [Xarray, Xindex, LSL #3]" or "LD1D Zresult.D, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<ulong> LoadVector(Vector<ulong> mask, ulong* address);


    /// LoadVector128AndReplicateToVector : Load and replicate 128 bits of data

    /// svfloat32_t svld1rq[_f32](svbool_t pg, const float32_t *base) : "LD1RQW Zresult.S, Pg/Z, [Xarray, Xindex, LSL #2]" or "LD1RQW Zresult.S, Pg/Z, [Xarray, #index * 4]" or "LD1RQW Zresult.S, Pg/Z, [Xbase, #0]"
  public static unsafe Vector<float> LoadVector128AndReplicateToVector(Vector<float> mask, float* address);

    /// svfloat64_t svld1rq[_f64](svbool_t pg, const float64_t *base) : "LD1RQD Zresult.D, Pg/Z, [Xarray, Xindex, LSL #3]" or "LD1RQD Zresult.D, Pg/Z, [Xarray, #index * 8]" or "LD1RQD Zresult.D, Pg/Z, [Xbase, #0]"
  public static unsafe Vector<double> LoadVector128AndReplicateToVector(Vector<double> mask, double* address);

    /// svint8_t svld1rq[_s8](svbool_t pg, const int8_t *base) : "LD1RQB Zresult.B, Pg/Z, [Xarray, Xindex]" or "LD1RQB Zresult.B, Pg/Z, [Xarray, #index]" or "LD1RQB Zresult.B, Pg/Z, [Xbase, #0]"
  public static unsafe Vector<sbyte> LoadVector128AndReplicateToVector(Vector<sbyte> mask, sbyte* address);

    /// svint16_t svld1rq[_s16](svbool_t pg, const int16_t *base) : "LD1RQH Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]" or "LD1RQH Zresult.H, Pg/Z, [Xarray, #index * 2]" or "LD1RQH Zresult.H, Pg/Z, [Xbase, #0]"
  public static unsafe Vector<short> LoadVector128AndReplicateToVector(Vector<short> mask, short* address);

    /// svint32_t svld1rq[_s32](svbool_t pg, const int32_t *base) : "LD1RQW Zresult.S, Pg/Z, [Xarray, Xindex, LSL #2]" or "LD1RQW Zresult.S, Pg/Z, [Xarray, #index * 4]" or "LD1RQW Zresult.S, Pg/Z, [Xbase, #0]"
  public static unsafe Vector<int> LoadVector128AndReplicateToVector(Vector<int> mask, int* address);

    /// svint64_t svld1rq[_s64](svbool_t pg, const int64_t *base) : "LD1RQD Zresult.D, Pg/Z, [Xarray, Xindex, LSL #3]" or "LD1RQD Zresult.D, Pg/Z, [Xarray, #index * 8]" or "LD1RQD Zresult.D, Pg/Z, [Xbase, #0]"
  public static unsafe Vector<long> LoadVector128AndReplicateToVector(Vector<long> mask, long* address);

    /// svuint8_t svld1rq[_u8](svbool_t pg, const uint8_t *base) : "LD1RQB Zresult.B, Pg/Z, [Xarray, Xindex]" or "LD1RQB Zresult.B, Pg/Z, [Xarray, #index]" or "LD1RQB Zresult.B, Pg/Z, [Xbase, #0]"
  public static unsafe Vector<byte> LoadVector128AndReplicateToVector(Vector<byte> mask, byte* address);

    /// svuint16_t svld1rq[_u16](svbool_t pg, const uint16_t *base) : "LD1RQH Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]" or "LD1RQH Zresult.H, Pg/Z, [Xarray, #index * 2]" or "LD1RQH Zresult.H, Pg/Z, [Xbase, #0]"
  public static unsafe Vector<ushort> LoadVector128AndReplicateToVector(Vector<ushort> mask, ushort* address);

    /// svuint32_t svld1rq[_u32](svbool_t pg, const uint32_t *base) : "LD1RQW Zresult.S, Pg/Z, [Xarray, Xindex, LSL #2]" or "LD1RQW Zresult.S, Pg/Z, [Xarray, #index * 4]" or "LD1RQW Zresult.S, Pg/Z, [Xbase, #0]"
  public static unsafe Vector<uint> LoadVector128AndReplicateToVector(Vector<uint> mask, uint* address);

    /// svuint64_t svld1rq[_u64](svbool_t pg, const uint64_t *base) : "LD1RQD Zresult.D, Pg/Z, [Xarray, Xindex, LSL #3]" or "LD1RQD Zresult.D, Pg/Z, [Xarray, #index * 8]" or "LD1RQD Zresult.D, Pg/Z, [Xbase, #0]"
  public static unsafe Vector<ulong> LoadVector128AndReplicateToVector(Vector<ulong> mask, ulong* address);


    /// LoadVectorByteNonFaultingZeroExtendToInt16 : Load 8-bit data and zero-extend, non-faulting

    /// svint16_t svldnf1ub_s16(svbool_t pg, const uint8_t *base) : "LDNF1B Zresult.H, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<short> LoadVectorByteNonFaultingZeroExtendToInt16(byte* address);


    /// LoadVectorByteNonFaultingZeroExtendToInt32 : Load 8-bit data and zero-extend, non-faulting

    /// svint32_t svldnf1ub_s32(svbool_t pg, const uint8_t *base) : "LDNF1B Zresult.S, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<int> LoadVectorByteNonFaultingZeroExtendToInt32(byte* address);


    /// LoadVectorByteNonFaultingZeroExtendToInt64 : Load 8-bit data and zero-extend, non-faulting

    /// svint64_t svldnf1ub_s64(svbool_t pg, const uint8_t *base) : "LDNF1B Zresult.D, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<long> LoadVectorByteNonFaultingZeroExtendToInt64(byte* address);


    /// LoadVectorByteNonFaultingZeroExtendToUInt16 : Load 8-bit data and zero-extend, non-faulting

    /// svuint16_t svldnf1ub_u16(svbool_t pg, const uint8_t *base) : "LDNF1B Zresult.H, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<ushort> LoadVectorByteNonFaultingZeroExtendToUInt16(byte* address);


    /// LoadVectorByteNonFaultingZeroExtendToUInt32 : Load 8-bit data and zero-extend, non-faulting

    /// svuint32_t svldnf1ub_u32(svbool_t pg, const uint8_t *base) : "LDNF1B Zresult.S, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<uint> LoadVectorByteNonFaultingZeroExtendToUInt32(byte* address);


    /// LoadVectorByteNonFaultingZeroExtendToUInt64 : Load 8-bit data and zero-extend, non-faulting

    /// svuint64_t svldnf1ub_u64(svbool_t pg, const uint8_t *base) : "LDNF1B Zresult.D, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<ulong> LoadVectorByteNonFaultingZeroExtendToUInt64(byte* address);


    /// LoadVectorByteZeroExtendToInt16 : Load 8-bit data and zero-extend

    /// svint16_t svld1ub_s16(svbool_t pg, const uint8_t *base) : "LD1B Zresult.H, Pg/Z, [Xarray, Xindex]" or "LD1B Zresult.H, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<short> LoadVectorByteZeroExtendToInt16(Vector<short> mask, byte* address);


    /// LoadVectorByteZeroExtendToInt32 : Load 8-bit data and zero-extend

    /// svint32_t svld1ub_s32(svbool_t pg, const uint8_t *base) : "LD1B Zresult.S, Pg/Z, [Xarray, Xindex]" or "LD1B Zresult.S, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<int> LoadVectorByteZeroExtendToInt32(Vector<int> mask, byte* address);


    /// LoadVectorByteZeroExtendToInt64 : Load 8-bit data and zero-extend

    /// svint64_t svld1ub_s64(svbool_t pg, const uint8_t *base) : "LD1B Zresult.D, Pg/Z, [Xarray, Xindex]" or "LD1B Zresult.D, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<long> LoadVectorByteZeroExtendToInt64(Vector<long> mask, byte* address);


    /// LoadVectorByteZeroExtendToUInt16 : Load 8-bit data and zero-extend

    /// svuint16_t svld1ub_u16(svbool_t pg, const uint8_t *base) : "LD1B Zresult.H, Pg/Z, [Xarray, Xindex]" or "LD1B Zresult.H, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<ushort> LoadVectorByteZeroExtendToUInt16(Vector<ushort> mask, byte* address);


    /// LoadVectorByteZeroExtendToUInt32 : Load 8-bit data and zero-extend

    /// svuint32_t svld1ub_u32(svbool_t pg, const uint8_t *base) : "LD1B Zresult.S, Pg/Z, [Xarray, Xindex]" or "LD1B Zresult.S, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<uint> LoadVectorByteZeroExtendToUInt32(Vector<uint> mask, byte* address);


    /// LoadVectorByteZeroExtendToUInt64 : Load 8-bit data and zero-extend

    /// svuint64_t svld1ub_u64(svbool_t pg, const uint8_t *base) : "LD1B Zresult.D, Pg/Z, [Xarray, Xindex]" or "LD1B Zresult.D, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<ulong> LoadVectorByteZeroExtendToUInt64(Vector<ulong> mask, byte* address);


    /// LoadVectorInt16NonFaultingSignExtendToInt32 : Load 16-bit data and sign-extend, non-faulting

    /// svint32_t svldnf1sh_s32(svbool_t pg, const int16_t *base) : "LDNF1SH Zresult.S, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<int> LoadVectorInt16NonFaultingSignExtendToInt32(short* address);


    /// LoadVectorInt16NonFaultingSignExtendToInt64 : Load 16-bit data and sign-extend, non-faulting

    /// svint64_t svldnf1sh_s64(svbool_t pg, const int16_t *base) : "LDNF1SH Zresult.D, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<long> LoadVectorInt16NonFaultingSignExtendToInt64(short* address);


    /// LoadVectorInt16NonFaultingSignExtendToUInt32 : Load 16-bit data and sign-extend, non-faulting

    /// svuint32_t svldnf1sh_u32(svbool_t pg, const int16_t *base) : "LDNF1SH Zresult.S, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<uint> LoadVectorInt16NonFaultingSignExtendToUInt32(short* address);


    /// LoadVectorInt16NonFaultingSignExtendToUInt64 : Load 16-bit data and sign-extend, non-faulting

    /// svuint64_t svldnf1sh_u64(svbool_t pg, const int16_t *base) : "LDNF1SH Zresult.D, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<ulong> LoadVectorInt16NonFaultingSignExtendToUInt64(short* address);


    /// LoadVectorInt16SignExtendToInt32 : Load 16-bit data and sign-extend

    /// svint32_t svld1sh_s32(svbool_t pg, const int16_t *base) : "LD1SH Zresult.S, Pg/Z, [Xarray, Xindex, LSL #1]" or "LD1SH Zresult.S, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<int> LoadVectorInt16SignExtendToInt32(Vector<int> mask, short* address);


    /// LoadVectorInt16SignExtendToInt64 : Load 16-bit data and sign-extend

    /// svint64_t svld1sh_s64(svbool_t pg, const int16_t *base) : "LD1SH Zresult.D, Pg/Z, [Xarray, Xindex, LSL #1]" or "LD1SH Zresult.D, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<long> LoadVectorInt16SignExtendToInt64(Vector<long> mask, short* address);


    /// LoadVectorInt16SignExtendToUInt32 : Load 16-bit data and sign-extend

    /// svuint32_t svld1sh_u32(svbool_t pg, const int16_t *base) : "LD1SH Zresult.S, Pg/Z, [Xarray, Xindex, LSL #1]" or "LD1SH Zresult.S, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<uint> LoadVectorInt16SignExtendToUInt32(Vector<uint> mask, short* address);


    /// LoadVectorInt16SignExtendToUInt64 : Load 16-bit data and sign-extend

    /// svuint64_t svld1sh_u64(svbool_t pg, const int16_t *base) : "LD1SH Zresult.D, Pg/Z, [Xarray, Xindex, LSL #1]" or "LD1SH Zresult.D, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<ulong> LoadVectorInt16SignExtendToUInt64(Vector<ulong> mask, short* address);


    /// LoadVectorInt32NonFaultingSignExtendToInt64 : Load 32-bit data and sign-extend, non-faulting

    /// svint64_t svldnf1sw_s64(svbool_t pg, const int32_t *base) : "LDNF1SW Zresult.D, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<long> LoadVectorInt32NonFaultingSignExtendToInt64(int* address);


    /// LoadVectorInt32NonFaultingSignExtendToUInt64 : Load 32-bit data and sign-extend, non-faulting

    /// svuint64_t svldnf1sw_u64(svbool_t pg, const int32_t *base) : "LDNF1SW Zresult.D, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<ulong> LoadVectorInt32NonFaultingSignExtendToUInt64(int* address);


    /// LoadVectorInt32SignExtendToInt64 : Load 32-bit data and sign-extend

    /// svint64_t svld1sw_s64(svbool_t pg, const int32_t *base) : "LD1SW Zresult.D, Pg/Z, [Xarray, Xindex, LSL #2]" or "LD1SW Zresult.D, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<long> LoadVectorInt32SignExtendToInt64(Vector<long> mask, int* address);


    /// LoadVectorInt32SignExtendToUInt64 : Load 32-bit data and sign-extend

    /// svuint64_t svld1sw_u64(svbool_t pg, const int32_t *base) : "LD1SW Zresult.D, Pg/Z, [Xarray, Xindex, LSL #2]" or "LD1SW Zresult.D, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<ulong> LoadVectorInt32SignExtendToUInt64(Vector<ulong> mask, int* address);


    /// LoadVectorNonFaulting : Unextended load, non-faulting

    /// svfloat32_t svldnf1[_f32](svbool_t pg, const float32_t *base) : "LDNF1W Zresult.S, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<float> LoadVectorNonFaulting(float* address);

    /// svfloat64_t svldnf1[_f64](svbool_t pg, const float64_t *base) : "LDNF1D Zresult.D, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<double> LoadVectorNonFaulting(double* address);

    /// svint8_t svldnf1[_s8](svbool_t pg, const int8_t *base) : "LDNF1B Zresult.B, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<sbyte> LoadVectorNonFaulting(sbyte* address);

    /// svint16_t svldnf1[_s16](svbool_t pg, const int16_t *base) : "LDNF1H Zresult.H, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<short> LoadVectorNonFaulting(short* address);

    /// svint32_t svldnf1[_s32](svbool_t pg, const int32_t *base) : "LDNF1W Zresult.S, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<int> LoadVectorNonFaulting(int* address);

    /// svint64_t svldnf1[_s64](svbool_t pg, const int64_t *base) : "LDNF1D Zresult.D, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<long> LoadVectorNonFaulting(long* address);

    /// svuint8_t svldnf1[_u8](svbool_t pg, const uint8_t *base) : "LDNF1B Zresult.B, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<byte> LoadVectorNonFaulting(byte* address);

    /// svuint16_t svldnf1[_u16](svbool_t pg, const uint16_t *base) : "LDNF1H Zresult.H, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<ushort> LoadVectorNonFaulting(ushort* address);

    /// svuint32_t svldnf1[_u32](svbool_t pg, const uint32_t *base) : "LDNF1W Zresult.S, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<uint> LoadVectorNonFaulting(uint* address);

    /// svuint64_t svldnf1[_u64](svbool_t pg, const uint64_t *base) : "LDNF1D Zresult.D, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<ulong> LoadVectorNonFaulting(ulong* address);


    /// LoadVectorNonTemporal : Unextended load, non-temporal

    /// svfloat32_t svldnt1[_f32](svbool_t pg, const float32_t *base) : "LDNT1W Zresult.S, Pg/Z, [Xarray, Xindex, LSL #2]" or "LDNT1W Zresult.S, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<float> LoadVectorNonTemporal(Vector<float> mask, float* address);

    /// svfloat64_t svldnt1[_f64](svbool_t pg, const float64_t *base) : "LDNT1D Zresult.D, Pg/Z, [Xarray, Xindex, LSL #3]" or "LDNT1D Zresult.D, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<double> LoadVectorNonTemporal(Vector<double> mask, double* address);

    /// svint8_t svldnt1[_s8](svbool_t pg, const int8_t *base) : "LDNT1B Zresult.B, Pg/Z, [Xarray, Xindex]" or "LDNT1B Zresult.B, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<sbyte> LoadVectorNonTemporal(Vector<sbyte> mask, sbyte* address);

    /// svint16_t svldnt1[_s16](svbool_t pg, const int16_t *base) : "LDNT1H Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]" or "LDNT1H Zresult.H, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<short> LoadVectorNonTemporal(Vector<short> mask, short* address);

    /// svint32_t svldnt1[_s32](svbool_t pg, const int32_t *base) : "LDNT1W Zresult.S, Pg/Z, [Xarray, Xindex, LSL #2]" or "LDNT1W Zresult.S, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<int> LoadVectorNonTemporal(Vector<int> mask, int* address);

    /// svint64_t svldnt1[_s64](svbool_t pg, const int64_t *base) : "LDNT1D Zresult.D, Pg/Z, [Xarray, Xindex, LSL #3]" or "LDNT1D Zresult.D, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<long> LoadVectorNonTemporal(Vector<long> mask, long* address);

    /// svuint8_t svldnt1[_u8](svbool_t pg, const uint8_t *base) : "LDNT1B Zresult.B, Pg/Z, [Xarray, Xindex]" or "LDNT1B Zresult.B, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<byte> LoadVectorNonTemporal(Vector<byte> mask, byte* address);

    /// svuint16_t svldnt1[_u16](svbool_t pg, const uint16_t *base) : "LDNT1H Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]" or "LDNT1H Zresult.H, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<ushort> LoadVectorNonTemporal(Vector<ushort> mask, ushort* address);

    /// svuint32_t svldnt1[_u32](svbool_t pg, const uint32_t *base) : "LDNT1W Zresult.S, Pg/Z, [Xarray, Xindex, LSL #2]" or "LDNT1W Zresult.S, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<uint> LoadVectorNonTemporal(Vector<uint> mask, uint* address);

    /// svuint64_t svldnt1[_u64](svbool_t pg, const uint64_t *base) : "LDNT1D Zresult.D, Pg/Z, [Xarray, Xindex, LSL #3]" or "LDNT1D Zresult.D, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<ulong> LoadVectorNonTemporal(Vector<ulong> mask, ulong* address);


    /// LoadVectorSByteNonFaultingSignExtendToInt16 : Load 8-bit data and sign-extend, non-faulting

    /// svint16_t svldnf1sb_s16(svbool_t pg, const int8_t *base) : "LDNF1SB Zresult.H, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<short> LoadVectorSByteNonFaultingSignExtendToInt16(sbyte* address);


    /// LoadVectorSByteNonFaultingSignExtendToInt32 : Load 8-bit data and sign-extend, non-faulting

    /// svint32_t svldnf1sb_s32(svbool_t pg, const int8_t *base) : "LDNF1SB Zresult.S, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<int> LoadVectorSByteNonFaultingSignExtendToInt32(sbyte* address);


    /// LoadVectorSByteNonFaultingSignExtendToInt64 : Load 8-bit data and sign-extend, non-faulting

    /// svint64_t svldnf1sb_s64(svbool_t pg, const int8_t *base) : "LDNF1SB Zresult.D, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<long> LoadVectorSByteNonFaultingSignExtendToInt64(sbyte* address);


    /// LoadVectorSByteNonFaultingSignExtendToUInt16 : Load 8-bit data and sign-extend, non-faulting

    /// svuint16_t svldnf1sb_u16(svbool_t pg, const int8_t *base) : "LDNF1SB Zresult.H, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<ushort> LoadVectorSByteNonFaultingSignExtendToUInt16(sbyte* address);


    /// LoadVectorSByteNonFaultingSignExtendToUInt32 : Load 8-bit data and sign-extend, non-faulting

    /// svuint32_t svldnf1sb_u32(svbool_t pg, const int8_t *base) : "LDNF1SB Zresult.S, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<uint> LoadVectorSByteNonFaultingSignExtendToUInt32(sbyte* address);


    /// LoadVectorSByteNonFaultingSignExtendToUInt64 : Load 8-bit data and sign-extend, non-faulting

    /// svuint64_t svldnf1sb_u64(svbool_t pg, const int8_t *base) : "LDNF1SB Zresult.D, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<ulong> LoadVectorSByteNonFaultingSignExtendToUInt64(sbyte* address);


    /// LoadVectorSByteSignExtendToInt16 : Load 8-bit data and sign-extend

    /// svint16_t svld1sb_s16(svbool_t pg, const int8_t *base) : "LD1SB Zresult.H, Pg/Z, [Xarray, Xindex]" or "LD1SB Zresult.H, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<short> LoadVectorSByteSignExtendToInt16(Vector<short> mask, sbyte* address);


    /// LoadVectorSByteSignExtendToInt32 : Load 8-bit data and sign-extend

    /// svint32_t svld1sb_s32(svbool_t pg, const int8_t *base) : "LD1SB Zresult.S, Pg/Z, [Xarray, Xindex]" or "LD1SB Zresult.S, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<int> LoadVectorSByteSignExtendToInt32(Vector<int> mask, sbyte* address);


    /// LoadVectorSByteSignExtendToInt64 : Load 8-bit data and sign-extend

    /// svint64_t svld1sb_s64(svbool_t pg, const int8_t *base) : "LD1SB Zresult.D, Pg/Z, [Xarray, Xindex]" or "LD1SB Zresult.D, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<long> LoadVectorSByteSignExtendToInt64(Vector<long> mask, sbyte* address);


    /// LoadVectorSByteSignExtendToUInt16 : Load 8-bit data and sign-extend

    /// svuint16_t svld1sb_u16(svbool_t pg, const int8_t *base) : "LD1SB Zresult.H, Pg/Z, [Xarray, Xindex]" or "LD1SB Zresult.H, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<ushort> LoadVectorSByteSignExtendToUInt16(Vector<ushort> mask, sbyte* address);


    /// LoadVectorSByteSignExtendToUInt32 : Load 8-bit data and sign-extend

    /// svuint32_t svld1sb_u32(svbool_t pg, const int8_t *base) : "LD1SB Zresult.S, Pg/Z, [Xarray, Xindex]" or "LD1SB Zresult.S, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<uint> LoadVectorSByteSignExtendToUInt32(Vector<uint> mask, sbyte* address);


    /// LoadVectorSByteSignExtendToUInt64 : Load 8-bit data and sign-extend

    /// svuint64_t svld1sb_u64(svbool_t pg, const int8_t *base) : "LD1SB Zresult.D, Pg/Z, [Xarray, Xindex]" or "LD1SB Zresult.D, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<ulong> LoadVectorSByteSignExtendToUInt64(Vector<ulong> mask, sbyte* address);


    /// LoadVectorUInt16NonFaultingZeroExtendToInt32 : Load 16-bit data and zero-extend, non-faulting

    /// svint32_t svldnf1uh_s32(svbool_t pg, const uint16_t *base) : "LDNF1H Zresult.S, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<int> LoadVectorUInt16NonFaultingZeroExtendToInt32(ushort* address);


    /// LoadVectorUInt16NonFaultingZeroExtendToInt64 : Load 16-bit data and zero-extend, non-faulting

    /// svint64_t svldnf1uh_s64(svbool_t pg, const uint16_t *base) : "LDNF1H Zresult.D, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<long> LoadVectorUInt16NonFaultingZeroExtendToInt64(ushort* address);


    /// LoadVectorUInt16NonFaultingZeroExtendToUInt32 : Load 16-bit data and zero-extend, non-faulting

    /// svuint32_t svldnf1uh_u32(svbool_t pg, const uint16_t *base) : "LDNF1H Zresult.S, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<uint> LoadVectorUInt16NonFaultingZeroExtendToUInt32(ushort* address);


    /// LoadVectorUInt16NonFaultingZeroExtendToUInt64 : Load 16-bit data and zero-extend, non-faulting

    /// svuint64_t svldnf1uh_u64(svbool_t pg, const uint16_t *base) : "LDNF1H Zresult.D, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<ulong> LoadVectorUInt16NonFaultingZeroExtendToUInt64(ushort* address);


    /// LoadVectorUInt16ZeroExtendToInt32 : Load 16-bit data and zero-extend

    /// svint32_t svld1uh_s32(svbool_t pg, const uint16_t *base) : "LD1H Zresult.S, Pg/Z, [Xarray, Xindex, LSL #1]" or "LD1H Zresult.S, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<int> LoadVectorUInt16ZeroExtendToInt32(Vector<int> mask, ushort* address);


    /// LoadVectorUInt16ZeroExtendToInt64 : Load 16-bit data and zero-extend

    /// svint64_t svld1uh_s64(svbool_t pg, const uint16_t *base) : "LD1H Zresult.D, Pg/Z, [Xarray, Xindex, LSL #1]" or "LD1H Zresult.D, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<long> LoadVectorUInt16ZeroExtendToInt64(Vector<long> mask, ushort* address);


    /// LoadVectorUInt16ZeroExtendToUInt32 : Load 16-bit data and zero-extend

    /// svuint32_t svld1uh_u32(svbool_t pg, const uint16_t *base) : "LD1H Zresult.S, Pg/Z, [Xarray, Xindex, LSL #1]" or "LD1H Zresult.S, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<uint> LoadVectorUInt16ZeroExtendToUInt32(Vector<uint> mask, ushort* address);


    /// LoadVectorUInt16ZeroExtendToUInt64 : Load 16-bit data and zero-extend

    /// svuint64_t svld1uh_u64(svbool_t pg, const uint16_t *base) : "LD1H Zresult.D, Pg/Z, [Xarray, Xindex, LSL #1]" or "LD1H Zresult.D, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<ulong> LoadVectorUInt16ZeroExtendToUInt64(Vector<ulong> mask, ushort* address);


    /// LoadVectorUInt32NonFaultingZeroExtendToInt64 : Load 32-bit data and zero-extend, non-faulting

    /// svint64_t svldnf1uw_s64(svbool_t pg, const uint32_t *base) : "LDNF1W Zresult.D, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<long> LoadVectorUInt32NonFaultingZeroExtendToInt64(uint* address);


    /// LoadVectorUInt32NonFaultingZeroExtendToUInt64 : Load 32-bit data and zero-extend, non-faulting

    /// svuint64_t svldnf1uw_u64(svbool_t pg, const uint32_t *base) : "LDNF1W Zresult.D, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<ulong> LoadVectorUInt32NonFaultingZeroExtendToUInt64(uint* address);


    /// LoadVectorUInt32ZeroExtendToInt64 : Load 32-bit data and zero-extend

    /// svint64_t svld1uw_s64(svbool_t pg, const uint32_t *base) : "LD1W Zresult.D, Pg/Z, [Xarray, Xindex, LSL #2]" or "LD1W Zresult.D, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<long> LoadVectorUInt32ZeroExtendToInt64(Vector<long> mask, uint* address);


    /// LoadVectorUInt32ZeroExtendToUInt64 : Load 32-bit data and zero-extend

    /// svuint64_t svld1uw_u64(svbool_t pg, const uint32_t *base) : "LD1W Zresult.D, Pg/Z, [Xarray, Xindex, LSL #2]" or "LD1W Zresult.D, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<ulong> LoadVectorUInt32ZeroExtendToUInt64(Vector<ulong> mask, uint* address);


    /// LoadVectorx2 : Load two-element tuples into two vectors

    /// svfloat32x2_t svld2[_f32](svbool_t pg, const float32_t *base) : "LD2W {Zresult0.S, Zresult1.S}, Pg/Z, [Xarray, Xindex, LSL #2]" or "LD2W {Zresult0.S, Zresult1.S}, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe (Vector<float>, Vector<float>) LoadVectorx2(Vector<float> mask, float* address);

    /// svfloat64x2_t svld2[_f64](svbool_t pg, const float64_t *base) : "LD2D {Zresult0.D, Zresult1.D}, Pg/Z, [Xarray, Xindex, LSL #3]" or "LD2D {Zresult0.D, Zresult1.D}, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe (Vector<double>, Vector<double>) LoadVectorx2(Vector<double> mask, double* address);

    /// svint8x2_t svld2[_s8](svbool_t pg, const int8_t *base) : "LD2B {Zresult0.B, Zresult1.B}, Pg/Z, [Xarray, Xindex]" or "LD2B {Zresult0.B, Zresult1.B}, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe (Vector<sbyte>, Vector<sbyte>) LoadVectorx2(Vector<sbyte> mask, sbyte* address);

    /// svint16x2_t svld2[_s16](svbool_t pg, const int16_t *base) : "LD2H {Zresult0.H, Zresult1.H}, Pg/Z, [Xarray, Xindex, LSL #1]" or "LD2H {Zresult0.H, Zresult1.H}, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe (Vector<short>, Vector<short>) LoadVectorx2(Vector<short> mask, short* address);

    /// svint32x2_t svld2[_s32](svbool_t pg, const int32_t *base) : "LD2W {Zresult0.S, Zresult1.S}, Pg/Z, [Xarray, Xindex, LSL #2]" or "LD2W {Zresult0.S, Zresult1.S}, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe (Vector<int>, Vector<int>) LoadVectorx2(Vector<int> mask, int* address);

    /// svint64x2_t svld2[_s64](svbool_t pg, const int64_t *base) : "LD2D {Zresult0.D, Zresult1.D}, Pg/Z, [Xarray, Xindex, LSL #3]" or "LD2D {Zresult0.D, Zresult1.D}, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe (Vector<long>, Vector<long>) LoadVectorx2(Vector<long> mask, long* address);

    /// svuint8x2_t svld2[_u8](svbool_t pg, const uint8_t *base) : "LD2B {Zresult0.B, Zresult1.B}, Pg/Z, [Xarray, Xindex]" or "LD2B {Zresult0.B, Zresult1.B}, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe (Vector<byte>, Vector<byte>) LoadVectorx2(Vector<byte> mask, byte* address);

    /// svuint16x2_t svld2[_u16](svbool_t pg, const uint16_t *base) : "LD2H {Zresult0.H, Zresult1.H}, Pg/Z, [Xarray, Xindex, LSL #1]" or "LD2H {Zresult0.H, Zresult1.H}, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe (Vector<ushort>, Vector<ushort>) LoadVectorx2(Vector<ushort> mask, ushort* address);

    /// svuint32x2_t svld2[_u32](svbool_t pg, const uint32_t *base) : "LD2W {Zresult0.S, Zresult1.S}, Pg/Z, [Xarray, Xindex, LSL #2]" or "LD2W {Zresult0.S, Zresult1.S}, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe (Vector<uint>, Vector<uint>) LoadVectorx2(Vector<uint> mask, uint* address);

    /// svuint64x2_t svld2[_u64](svbool_t pg, const uint64_t *base) : "LD2D {Zresult0.D, Zresult1.D}, Pg/Z, [Xarray, Xindex, LSL #3]" or "LD2D {Zresult0.D, Zresult1.D}, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe (Vector<ulong>, Vector<ulong>) LoadVectorx2(Vector<ulong> mask, ulong* address);


    /// LoadVectorx3 : Load three-element tuples into three vectors

    /// svfloat32x3_t svld3[_f32](svbool_t pg, const float32_t *base) : "LD3W {Zresult0.S - Zresult2.S}, Pg/Z, [Xarray, Xindex, LSL #2]" or "LD3W {Zresult0.S - Zresult2.S}, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe (Vector<float>, Vector<float>, Vector<float>) LoadVectorx3(Vector<float> mask, float* address);

    /// svfloat64x3_t svld3[_f64](svbool_t pg, const float64_t *base) : "LD3D {Zresult0.D - Zresult2.D}, Pg/Z, [Xarray, Xindex, LSL #3]" or "LD3D {Zresult0.D - Zresult2.D}, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe (Vector<double>, Vector<double>, Vector<double>) LoadVectorx3(Vector<double> mask, double* address);

    /// svint8x3_t svld3[_s8](svbool_t pg, const int8_t *base) : "LD3B {Zresult0.B - Zresult2.B}, Pg/Z, [Xarray, Xindex]" or "LD3B {Zresult0.B - Zresult2.B}, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe (Vector<sbyte>, Vector<sbyte>, Vector<sbyte>) LoadVectorx3(Vector<sbyte> mask, sbyte* address);

    /// svint16x3_t svld3[_s16](svbool_t pg, const int16_t *base) : "LD3H {Zresult0.H - Zresult2.H}, Pg/Z, [Xarray, Xindex, LSL #1]" or "LD3H {Zresult0.H - Zresult2.H}, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe (Vector<short>, Vector<short>, Vector<short>) LoadVectorx3(Vector<short> mask, short* address);

    /// svint32x3_t svld3[_s32](svbool_t pg, const int32_t *base) : "LD3W {Zresult0.S - Zresult2.S}, Pg/Z, [Xarray, Xindex, LSL #2]" or "LD3W {Zresult0.S - Zresult2.S}, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe (Vector<int>, Vector<int>, Vector<int>) LoadVectorx3(Vector<int> mask, int* address);

    /// svint64x3_t svld3[_s64](svbool_t pg, const int64_t *base) : "LD3D {Zresult0.D - Zresult2.D}, Pg/Z, [Xarray, Xindex, LSL #3]" or "LD3D {Zresult0.D - Zresult2.D}, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe (Vector<long>, Vector<long>, Vector<long>) LoadVectorx3(Vector<long> mask, long* address);

    /// svuint8x3_t svld3[_u8](svbool_t pg, const uint8_t *base) : "LD3B {Zresult0.B - Zresult2.B}, Pg/Z, [Xarray, Xindex]" or "LD3B {Zresult0.B - Zresult2.B}, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe (Vector<byte>, Vector<byte>, Vector<byte>) LoadVectorx3(Vector<byte> mask, byte* address);

    /// svuint16x3_t svld3[_u16](svbool_t pg, const uint16_t *base) : "LD3H {Zresult0.H - Zresult2.H}, Pg/Z, [Xarray, Xindex, LSL #1]" or "LD3H {Zresult0.H - Zresult2.H}, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe (Vector<ushort>, Vector<ushort>, Vector<ushort>) LoadVectorx3(Vector<ushort> mask, ushort* address);

    /// svuint32x3_t svld3[_u32](svbool_t pg, const uint32_t *base) : "LD3W {Zresult0.S - Zresult2.S}, Pg/Z, [Xarray, Xindex, LSL #2]" or "LD3W {Zresult0.S - Zresult2.S}, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe (Vector<uint>, Vector<uint>, Vector<uint>) LoadVectorx3(Vector<uint> mask, uint* address);

    /// svuint64x3_t svld3[_u64](svbool_t pg, const uint64_t *base) : "LD3D {Zresult0.D - Zresult2.D}, Pg/Z, [Xarray, Xindex, LSL #3]" or "LD3D {Zresult0.D - Zresult2.D}, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe (Vector<ulong>, Vector<ulong>, Vector<ulong>) LoadVectorx3(Vector<ulong> mask, ulong* address);


    /// LoadVectorx4 : Load four-element tuples into four vectors

    /// svfloat32x4_t svld4[_f32](svbool_t pg, const float32_t *base) : "LD4W {Zresult0.S - Zresult3.S}, Pg/Z, [Xarray, Xindex, LSL #2]" or "LD4W {Zresult0.S - Zresult3.S}, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe (Vector<float>, Vector<float>, Vector<float>, Vector<float>) LoadVectorx4(Vector<float> mask, float* address);

    /// svfloat64x4_t svld4[_f64](svbool_t pg, const float64_t *base) : "LD4D {Zresult0.D - Zresult3.D}, Pg/Z, [Xarray, Xindex, LSL #3]" or "LD4D {Zresult0.D - Zresult3.D}, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe (Vector<double>, Vector<double>, Vector<double>, Vector<double>) LoadVectorx4(Vector<double> mask, double* address);

    /// svint8x4_t svld4[_s8](svbool_t pg, const int8_t *base) : "LD4B {Zresult0.B - Zresult3.B}, Pg/Z, [Xarray, Xindex]" or "LD4B {Zresult0.B - Zresult3.B}, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe (Vector<sbyte>, Vector<sbyte>, Vector<sbyte>, Vector<sbyte>) LoadVectorx4(Vector<sbyte> mask, sbyte* address);

    /// svint16x4_t svld4[_s16](svbool_t pg, const int16_t *base) : "LD4H {Zresult0.H - Zresult3.H}, Pg/Z, [Xarray, Xindex, LSL #1]" or "LD4H {Zresult0.H - Zresult3.H}, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe (Vector<short>, Vector<short>, Vector<short>, Vector<short>) LoadVectorx4(Vector<short> mask, short* address);

    /// svint32x4_t svld4[_s32](svbool_t pg, const int32_t *base) : "LD4W {Zresult0.S - Zresult3.S}, Pg/Z, [Xarray, Xindex, LSL #2]" or "LD4W {Zresult0.S - Zresult3.S}, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe (Vector<int>, Vector<int>, Vector<int>, Vector<int>) LoadVectorx4(Vector<int> mask, int* address);

    /// svint64x4_t svld4[_s64](svbool_t pg, const int64_t *base) : "LD4D {Zresult0.D - Zresult3.D}, Pg/Z, [Xarray, Xindex, LSL #3]" or "LD4D {Zresult0.D - Zresult3.D}, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe (Vector<long>, Vector<long>, Vector<long>, Vector<long>) LoadVectorx4(Vector<long> mask, long* address);

    /// svuint8x4_t svld4[_u8](svbool_t pg, const uint8_t *base) : "LD4B {Zresult0.B - Zresult3.B}, Pg/Z, [Xarray, Xindex]" or "LD4B {Zresult0.B - Zresult3.B}, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe (Vector<byte>, Vector<byte>, Vector<byte>, Vector<byte>) LoadVectorx4(Vector<byte> mask, byte* address);

    /// svuint16x4_t svld4[_u16](svbool_t pg, const uint16_t *base) : "LD4H {Zresult0.H - Zresult3.H}, Pg/Z, [Xarray, Xindex, LSL #1]" or "LD4H {Zresult0.H - Zresult3.H}, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe (Vector<ushort>, Vector<ushort>, Vector<ushort>, Vector<ushort>) LoadVectorx4(Vector<ushort> mask, ushort* address);

    /// svuint32x4_t svld4[_u32](svbool_t pg, const uint32_t *base) : "LD4W {Zresult0.S - Zresult3.S}, Pg/Z, [Xarray, Xindex, LSL #2]" or "LD4W {Zresult0.S - Zresult3.S}, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe (Vector<uint>, Vector<uint>, Vector<uint>, Vector<uint>) LoadVectorx4(Vector<uint> mask, uint* address);

    /// svuint64x4_t svld4[_u64](svbool_t pg, const uint64_t *base) : "LD4D {Zresult0.D - Zresult3.D}, Pg/Z, [Xarray, Xindex, LSL #3]" or "LD4D {Zresult0.D - Zresult3.D}, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe (Vector<ulong>, Vector<ulong>, Vector<ulong>, Vector<ulong>) LoadVectorx4(Vector<ulong> mask, ulong* address);


    /// PrefetchBytes : Prefetch bytes

    /// void svprfb(svbool_t pg, const void *base, enum svprfop op) : "PRFB op, Pg, [Xarray, Xindex]" or "PRFB op, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void PrefetchBytes(Vector<byte> mask, void* address, [ConstantExpected] SvePrefetchType prefetchType);


    /// PrefetchInt16 : Prefetch halfwords

    /// void svprfh(svbool_t pg, const void *base, enum svprfop op) : "PRFH op, Pg, [Xarray, Xindex, LSL #1]" or "PRFH op, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void PrefetchInt16(Vector<ushort> mask, void* address, [ConstantExpected] SvePrefetchType prefetchType);


    /// PrefetchInt32 : Prefetch words

    /// void svprfw(svbool_t pg, const void *base, enum svprfop op) : "PRFW op, Pg, [Xarray, Xindex, LSL #2]" or "PRFW op, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void PrefetchInt32(Vector<uint> mask, void* address, [ConstantExpected] SvePrefetchType prefetchType);


    /// PrefetchInt64 : Prefetch doublewords

    /// void svprfd(svbool_t pg, const void *base, enum svprfop op) : "PRFD op, Pg, [Xarray, Xindex, LSL #3]" or "PRFD op, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void PrefetchInt64(Vector<ulong> mask, void* address, [ConstantExpected] SvePrefetchType prefetchType);


  /// total method signatures: 138
  /// total method names:      63
}


  /// Rejected:
  ///   public static unsafe Vector<float> LoadVector(Vector<float> mask, float* address, long vnum); // svld1_vnum[_f32]
  ///   public static unsafe Vector<double> LoadVector(Vector<double> mask, double* address, long vnum); // svld1_vnum[_f64]
  ///   public static unsafe Vector<sbyte> LoadVector(Vector<sbyte> mask, sbyte* address, long vnum); // svld1_vnum[_s8]
  ///   public static unsafe Vector<short> LoadVector(Vector<short> mask, short* address, long vnum); // svld1_vnum[_s16]
  ///   public static unsafe Vector<int> LoadVector(Vector<int> mask, int* address, long vnum); // svld1_vnum[_s32]
  ///   public static unsafe Vector<long> LoadVector(Vector<long> mask, long* address, long vnum); // svld1_vnum[_s64]
  ///   public static unsafe Vector<byte> LoadVector(Vector<byte> mask, byte* address, long vnum); // svld1_vnum[_u8]
  ///   public static unsafe Vector<ushort> LoadVector(Vector<ushort> mask, ushort* address, long vnum); // svld1_vnum[_u16]
  ///   public static unsafe Vector<uint> LoadVector(Vector<uint> mask, uint* address, long vnum); // svld1_vnum[_u32]
  ///   public static unsafe Vector<ulong> LoadVector(Vector<ulong> mask, ulong* address, long vnum); // svld1_vnum[_u64]
  ///   public static unsafe Vector<short> LoadVectorByteNonFaultingZeroExtendToInt16(byte* address, long vnum); // svldnf1ub_vnum_s16
  ///   public static unsafe Vector<int> LoadVectorByteNonFaultingZeroExtendToInt32(byte* address, long vnum); // svldnf1ub_vnum_s32
  ///   public static unsafe Vector<long> LoadVectorByteNonFaultingZeroExtendToInt64(byte* address, long vnum); // svldnf1ub_vnum_s64
  ///   public static unsafe Vector<ushort> LoadVectorByteNonFaultingZeroExtendToUInt16(byte* address, long vnum); // svldnf1ub_vnum_u16
  ///   public static unsafe Vector<uint> LoadVectorByteNonFaultingZeroExtendToUInt32(byte* address, long vnum); // svldnf1ub_vnum_u32
  ///   public static unsafe Vector<ulong> LoadVectorByteNonFaultingZeroExtendToUInt64(byte* address, long vnum); // svldnf1ub_vnum_u64
  ///   public static unsafe Vector<short> LoadVectorByteZeroExtendToInt16(Vector<short> mask, byte* address, long vnum); // svld1ub_vnum_s16
  ///   public static unsafe Vector<int> LoadVectorByteZeroExtendToInt32(Vector<int> mask, byte* address, long vnum); // svld1ub_vnum_s32
  ///   public static unsafe Vector<long> LoadVectorByteZeroExtendToInt64(Vector<long> mask, byte* address, long vnum); // svld1ub_vnum_s64
  ///   public static unsafe Vector<ushort> LoadVectorByteZeroExtendToUInt16(Vector<ushort> mask, byte* address, long vnum); // svld1ub_vnum_u16
  ///   public static unsafe Vector<uint> LoadVectorByteZeroExtendToUInt32(Vector<uint> mask, byte* address, long vnum); // svld1ub_vnum_u32
  ///   public static unsafe Vector<ulong> LoadVectorByteZeroExtendToUInt64(Vector<ulong> mask, byte* address, long vnum); // svld1ub_vnum_u64
  ///   public static unsafe Vector<int> LoadVectorInt16NonFaultingSignExtendToInt32(short* address, long vnum); // svldnf1sh_vnum_s32
  ///   public static unsafe Vector<long> LoadVectorInt16NonFaultingSignExtendToInt64(short* address, long vnum); // svldnf1sh_vnum_s64
  ///   public static unsafe Vector<uint> LoadVectorInt16NonFaultingSignExtendToUInt32(short* address, long vnum); // svldnf1sh_vnum_u32
  ///   public static unsafe Vector<ulong> LoadVectorInt16NonFaultingSignExtendToUInt64(short* address, long vnum); // svldnf1sh_vnum_u64
  ///   public static unsafe Vector<int> LoadVectorInt16SignExtendToInt32(Vector<int> mask, short* address, long vnum); // svld1sh_vnum_s32
  ///   public static unsafe Vector<long> LoadVectorInt16SignExtendToInt64(Vector<long> mask, short* address, long vnum); // svld1sh_vnum_s64
  ///   public static unsafe Vector<uint> LoadVectorInt16SignExtendToUInt32(Vector<uint> mask, short* address, long vnum); // svld1sh_vnum_u32
  ///   public static unsafe Vector<ulong> LoadVectorInt16SignExtendToUInt64(Vector<ulong> mask, short* address, long vnum); // svld1sh_vnum_u64
  ///   public static unsafe Vector<long> LoadVectorInt32NonFaultingSignExtendToInt64(int* address, long vnum); // svldnf1sw_vnum_s64
  ///   public static unsafe Vector<ulong> LoadVectorInt32NonFaultingSignExtendToUInt64(int* address, long vnum); // svldnf1sw_vnum_u64
  ///   public static unsafe Vector<long> LoadVectorInt32SignExtendToInt64(Vector<long> mask, int* address, long vnum); // svld1sw_vnum_s64
  ///   public static unsafe Vector<ulong> LoadVectorInt32SignExtendToUInt64(Vector<ulong> mask, int* address, long vnum); // svld1sw_vnum_u64
  ///   public static unsafe Vector<float> LoadVectorNonFaulting(float* address, long vnum); // svldnf1_vnum[_f32]
  ///   public static unsafe Vector<double> LoadVectorNonFaulting(double* address, long vnum); // svldnf1_vnum[_f64]
  ///   public static unsafe Vector<sbyte> LoadVectorNonFaulting(sbyte* address, long vnum); // svldnf1_vnum[_s8]
  ///   public static unsafe Vector<short> LoadVectorNonFaulting(short* address, long vnum); // svldnf1_vnum[_s16]
  ///   public static unsafe Vector<int> LoadVectorNonFaulting(int* address, long vnum); // svldnf1_vnum[_s32]
  ///   public static unsafe Vector<long> LoadVectorNonFaulting(long* address, long vnum); // svldnf1_vnum[_s64]
  ///   public static unsafe Vector<byte> LoadVectorNonFaulting(byte* address, long vnum); // svldnf1_vnum[_u8]
  ///   public static unsafe Vector<ushort> LoadVectorNonFaulting(ushort* address, long vnum); // svldnf1_vnum[_u16]
  ///   public static unsafe Vector<uint> LoadVectorNonFaulting(uint* address, long vnum); // svldnf1_vnum[_u32]
  ///   public static unsafe Vector<ulong> LoadVectorNonFaulting(ulong* address, long vnum); // svldnf1_vnum[_u64]
  ///   public static unsafe Vector<float> LoadVectorNonTemporal(Vector<float> mask, float* address, long vnum); // svldnt1_vnum[_f32]
  ///   public static unsafe Vector<double> LoadVectorNonTemporal(Vector<double> mask, double* address, long vnum); // svldnt1_vnum[_f64]
  ///   public static unsafe Vector<sbyte> LoadVectorNonTemporal(Vector<sbyte> mask, sbyte* address, long vnum); // svldnt1_vnum[_s8]
  ///   public static unsafe Vector<short> LoadVectorNonTemporal(Vector<short> mask, short* address, long vnum); // svldnt1_vnum[_s16]
  ///   public static unsafe Vector<int> LoadVectorNonTemporal(Vector<int> mask, int* address, long vnum); // svldnt1_vnum[_s32]
  ///   public static unsafe Vector<long> LoadVectorNonTemporal(Vector<long> mask, long* address, long vnum); // svldnt1_vnum[_s64]
  ///   public static unsafe Vector<byte> LoadVectorNonTemporal(Vector<byte> mask, byte* address, long vnum); // svldnt1_vnum[_u8]
  ///   public static unsafe Vector<ushort> LoadVectorNonTemporal(Vector<ushort> mask, ushort* address, long vnum); // svldnt1_vnum[_u16]
  ///   public static unsafe Vector<uint> LoadVectorNonTemporal(Vector<uint> mask, uint* address, long vnum); // svldnt1_vnum[_u32]
  ///   public static unsafe Vector<ulong> LoadVectorNonTemporal(Vector<ulong> mask, ulong* address, long vnum); // svldnt1_vnum[_u64]
  ///   public static unsafe Vector<short> LoadVectorSByteNonFaultingSignExtendToInt16(sbyte* address, long vnum); // svldnf1sb_vnum_s16
  ///   public static unsafe Vector<int> LoadVectorSByteNonFaultingSignExtendToInt32(sbyte* address, long vnum); // svldnf1sb_vnum_s32
  ///   public static unsafe Vector<long> LoadVectorSByteNonFaultingSignExtendToInt64(sbyte* address, long vnum); // svldnf1sb_vnum_s64
  ///   public static unsafe Vector<ushort> LoadVectorSByteNonFaultingSignExtendToUInt16(sbyte* address, long vnum); // svldnf1sb_vnum_u16
  ///   public static unsafe Vector<uint> LoadVectorSByteNonFaultingSignExtendToUInt32(sbyte* address, long vnum); // svldnf1sb_vnum_u32
  ///   public static unsafe Vector<ulong> LoadVectorSByteNonFaultingSignExtendToUInt64(sbyte* address, long vnum); // svldnf1sb_vnum_u64
  ///   public static unsafe Vector<short> LoadVectorSByteSignExtendToInt16(Vector<short> mask, sbyte* address, long vnum); // svld1sb_vnum_s16
  ///   public static unsafe Vector<int> LoadVectorSByteSignExtendToInt32(Vector<int> mask, sbyte* address, long vnum); // svld1sb_vnum_s32
  ///   public static unsafe Vector<long> LoadVectorSByteSignExtendToInt64(Vector<long> mask, sbyte* address, long vnum); // svld1sb_vnum_s64
  ///   public static unsafe Vector<ushort> LoadVectorSByteSignExtendToUInt16(Vector<ushort> mask, sbyte* address, long vnum); // svld1sb_vnum_u16
  ///   public static unsafe Vector<uint> LoadVectorSByteSignExtendToUInt32(Vector<uint> mask, sbyte* address, long vnum); // svld1sb_vnum_u32
  ///   public static unsafe Vector<ulong> LoadVectorSByteSignExtendToUInt64(Vector<ulong> mask, sbyte* address, long vnum); // svld1sb_vnum_u64
  ///   public static unsafe Vector<int> LoadVectorUInt16NonFaultingZeroExtendToInt32(ushort* address, long vnum); // svldnf1uh_vnum_s32
  ///   public static unsafe Vector<long> LoadVectorUInt16NonFaultingZeroExtendToInt64(ushort* address, long vnum); // svldnf1uh_vnum_s64
  ///   public static unsafe Vector<uint> LoadVectorUInt16NonFaultingZeroExtendToUInt32(ushort* address, long vnum); // svldnf1uh_vnum_u32
  ///   public static unsafe Vector<ulong> LoadVectorUInt16NonFaultingZeroExtendToUInt64(ushort* address, long vnum); // svldnf1uh_vnum_u64
  ///   public static unsafe Vector<int> LoadVectorUInt16ZeroExtendToInt32(Vector<int> mask, ushort* address, long vnum); // svld1uh_vnum_s32
  ///   public static unsafe Vector<long> LoadVectorUInt16ZeroExtendToInt64(Vector<long> mask, ushort* address, long vnum); // svld1uh_vnum_s64
  ///   public static unsafe Vector<uint> LoadVectorUInt16ZeroExtendToUInt32(Vector<uint> mask, ushort* address, long vnum); // svld1uh_vnum_u32
  ///   public static unsafe Vector<ulong> LoadVectorUInt16ZeroExtendToUInt64(Vector<ulong> mask, ushort* address, long vnum); // svld1uh_vnum_u64
  ///   public static unsafe Vector<long> LoadVectorUInt32NonFaultingZeroExtendToInt64(uint* address, long vnum); // svldnf1uw_vnum_s64
  ///   public static unsafe Vector<ulong> LoadVectorUInt32NonFaultingZeroExtendToUInt64(uint* address, long vnum); // svldnf1uw_vnum_u64
  ///   public static unsafe Vector<long> LoadVectorUInt32ZeroExtendToInt64(Vector<long> mask, uint* address, long vnum); // svld1uw_vnum_s64
  ///   public static unsafe Vector<ulong> LoadVectorUInt32ZeroExtendToUInt64(Vector<ulong> mask, uint* address, long vnum); // svld1uw_vnum_u64
  ///   public static unsafe (Vector<float>, Vector<float>) LoadVectorx2(Vector<float> mask, float* address, long vnum); // svld2_vnum[_f32]
  ///   public static unsafe (Vector<double>, Vector<double>) LoadVectorx2(Vector<double> mask, double* address, long vnum); // svld2_vnum[_f64]
  ///   public static unsafe (Vector<sbyte>, Vector<sbyte>) LoadVectorx2(Vector<sbyte> mask, sbyte* address, long vnum); // svld2_vnum[_s8]
  ///   public static unsafe (Vector<short>, Vector<short>) LoadVectorx2(Vector<short> mask, short* address, long vnum); // svld2_vnum[_s16]
  ///   public static unsafe (Vector<int>, Vector<int>) LoadVectorx2(Vector<int> mask, int* address, long vnum); // svld2_vnum[_s32]
  ///   public static unsafe (Vector<long>, Vector<long>) LoadVectorx2(Vector<long> mask, long* address, long vnum); // svld2_vnum[_s64]
  ///   public static unsafe (Vector<byte>, Vector<byte>) LoadVectorx2(Vector<byte> mask, byte* address, long vnum); // svld2_vnum[_u8]
  ///   public static unsafe (Vector<ushort>, Vector<ushort>) LoadVectorx2(Vector<ushort> mask, ushort* address, long vnum); // svld2_vnum[_u16]
  ///   public static unsafe (Vector<uint>, Vector<uint>) LoadVectorx2(Vector<uint> mask, uint* address, long vnum); // svld2_vnum[_u32]
  ///   public static unsafe (Vector<ulong>, Vector<ulong>) LoadVectorx2(Vector<ulong> mask, ulong* address, long vnum); // svld2_vnum[_u64]
  ///   public static unsafe (Vector<float>, Vector<float>, Vector<float>) LoadVectorx3(Vector<float> mask, float* address, long vnum); // svld3_vnum[_f32]
  ///   public static unsafe (Vector<double>, Vector<double>, Vector<double>) LoadVectorx3(Vector<double> mask, double* address, long vnum); // svld3_vnum[_f64]
  ///   public static unsafe (Vector<sbyte>, Vector<sbyte>, Vector<sbyte>) LoadVectorx3(Vector<sbyte> mask, sbyte* address, long vnum); // svld3_vnum[_s8]
  ///   public static unsafe (Vector<short>, Vector<short>, Vector<short>) LoadVectorx3(Vector<short> mask, short* address, long vnum); // svld3_vnum[_s16]
  ///   public static unsafe (Vector<int>, Vector<int>, Vector<int>) LoadVectorx3(Vector<int> mask, int* address, long vnum); // svld3_vnum[_s32]
  ///   public static unsafe (Vector<long>, Vector<long>, Vector<long>) LoadVectorx3(Vector<long> mask, long* address, long vnum); // svld3_vnum[_s64]
  ///   public static unsafe (Vector<byte>, Vector<byte>, Vector<byte>) LoadVectorx3(Vector<byte> mask, byte* address, long vnum); // svld3_vnum[_u8]
  ///   public static unsafe (Vector<ushort>, Vector<ushort>, Vector<ushort>) LoadVectorx3(Vector<ushort> mask, ushort* address, long vnum); // svld3_vnum[_u16]
  ///   public static unsafe (Vector<uint>, Vector<uint>, Vector<uint>) LoadVectorx3(Vector<uint> mask, uint* address, long vnum); // svld3_vnum[_u32]
  ///   public static unsafe (Vector<ulong>, Vector<ulong>, Vector<ulong>) LoadVectorx3(Vector<ulong> mask, ulong* address, long vnum); // svld3_vnum[_u64]
  ///   public static unsafe (Vector<float>, Vector<float>, Vector<float>, Vector<float>) LoadVectorx4(Vector<float> mask, float* address, long vnum); // svld4_vnum[_f32]
  ///   public static unsafe (Vector<double>, Vector<double>, Vector<double>, Vector<double>) LoadVectorx4(Vector<double> mask, double* address, long vnum); // svld4_vnum[_f64]
  ///   public static unsafe (Vector<sbyte>, Vector<sbyte>, Vector<sbyte>, Vector<sbyte>) LoadVectorx4(Vector<sbyte> mask, sbyte* address, long vnum); // svld4_vnum[_s8]
  ///   public static unsafe (Vector<short>, Vector<short>, Vector<short>, Vector<short>) LoadVectorx4(Vector<short> mask, short* address, long vnum); // svld4_vnum[_s16]
  ///   public static unsafe (Vector<int>, Vector<int>, Vector<int>, Vector<int>) LoadVectorx4(Vector<int> mask, int* address, long vnum); // svld4_vnum[_s32]
  ///   public static unsafe (Vector<long>, Vector<long>, Vector<long>, Vector<long>) LoadVectorx4(Vector<long> mask, long* address, long vnum); // svld4_vnum[_s64]
  ///   public static unsafe (Vector<byte>, Vector<byte>, Vector<byte>, Vector<byte>) LoadVectorx4(Vector<byte> mask, byte* address, long vnum); // svld4_vnum[_u8]
  ///   public static unsafe (Vector<ushort>, Vector<ushort>, Vector<ushort>, Vector<ushort>) LoadVectorx4(Vector<ushort> mask, ushort* address, long vnum); // svld4_vnum[_u16]
  ///   public static unsafe (Vector<uint>, Vector<uint>, Vector<uint>, Vector<uint>) LoadVectorx4(Vector<uint> mask, uint* address, long vnum); // svld4_vnum[_u32]
  ///   public static unsafe (Vector<ulong>, Vector<ulong>, Vector<ulong>, Vector<ulong>) LoadVectorx4(Vector<ulong> mask, ulong* address, long vnum); // svld4_vnum[_u64]
  ///   public static unsafe void PrefetchBytes(Vector<byte> mask, void* address, long vnum, [ConstantExpected] SvePrefetchType prefetchType); // svprfb_vnum
  ///   public static unsafe void PrefetchInt16(Vector<ushort> mask, void* address, long vnum, [ConstantExpected] SvePrefetchType prefetchType); // svprfh_vnum
  ///   public static unsafe void PrefetchInt32(Vector<uint> mask, void* address, long vnum, [ConstantExpected] SvePrefetchType prefetchType); // svprfw_vnum
  ///   public static unsafe void PrefetchInt64(Vector<ulong> mask, void* address, long vnum, [ConstantExpected] SvePrefetchType prefetchType); // svprfd_vnum
  ///   Total Rejected: 112

  /// Total ACLE covered across API:      250

