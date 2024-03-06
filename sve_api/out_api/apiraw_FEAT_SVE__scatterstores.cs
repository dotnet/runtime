namespace System.Runtime.Intrinsics.Arm;

/// VectorT Summary
public abstract partial class Sve : AdvSimd /// Feature: FEAT_SVE  Category: scatterstores
{

  /// T: [float, uint], [int, uint], [uint, uint], [double, ulong], [long, ulong], [ulong, ulong]
  public static unsafe void Scatter(Vector<T> mask, Vector<T2> addresses, Vector<T> data); // ST1W or ST1D

  /// T: [float, int], [int, int], [uint, int], [float, uint], [int, uint], [uint, uint], [double, long], [long, long], [ulong, long], [double, ulong], [long, ulong], [ulong, ulong]
  public static unsafe void Scatter(Vector<T> mask, T* address, Vector<T2> indicies, Vector<T> data); // ST1W or ST1D

  /// T: [int, uint], [long, ulong]
  public static unsafe void Scatter16BitNarrowing(Vector<T> mask, Vector<T2> addresses, Vector<T> data); // ST1H

  /// T: uint, ulong
  public static unsafe void Scatter16BitNarrowing(Vector<T> mask, Vector<T> addresses, Vector<T> data); // ST1H

  /// T: int, long
  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<T> mask, short* address, Vector<T> offsets, Vector<T> data); // ST1H

  /// T: [uint, int], [ulong, long]
  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<T> mask, ushort* address, Vector<T2> offsets, Vector<T> data); // ST1H

  /// T: [int, uint], [long, ulong]
  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<T> mask, short* address, Vector<T2> offsets, Vector<T> data); // ST1H

  /// T: uint, ulong
  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<T> mask, ushort* address, Vector<T> offsets, Vector<T> data); // ST1H

  /// T: int, long
  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<T> mask, short* address, Vector<T> indices, Vector<T> data); // ST1H

  /// T: [uint, int], [ulong, long]
  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<T> mask, ushort* address, Vector<T2> indices, Vector<T> data); // ST1H

  /// T: [int, uint], [long, ulong]
  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<T> mask, short* address, Vector<T2> indices, Vector<T> data); // ST1H

  /// T: uint, ulong
  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<T> mask, ushort* address, Vector<T> indices, Vector<T> data); // ST1H

  public static unsafe void Scatter32BitNarrowing(Vector<long> mask, Vector<ulong> addresses, Vector<long> data); // ST1W

  public static unsafe void Scatter32BitNarrowing(Vector<ulong> mask, Vector<ulong> addresses, Vector<ulong> data); // ST1W

  public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<long> mask, int* address, Vector<long> offsets, Vector<long> data); // ST1W

  public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<ulong> mask, uint* address, Vector<long> offsets, Vector<ulong> data); // ST1W

  public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<long> mask, int* address, Vector<ulong> offsets, Vector<long> data); // ST1W

  public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<ulong> mask, uint* address, Vector<ulong> offsets, Vector<ulong> data); // ST1W

  public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<long> mask, int* address, Vector<long> indices, Vector<long> data); // ST1W

  public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<ulong> mask, uint* address, Vector<long> indices, Vector<ulong> data); // ST1W

  public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<long> mask, int* address, Vector<ulong> indices, Vector<long> data); // ST1W

  public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<ulong> mask, uint* address, Vector<ulong> indices, Vector<ulong> data); // ST1W

  /// T: [int, uint], [long, ulong]
  public static unsafe void Scatter8BitNarrowing(Vector<T> mask, Vector<T2> addresses, Vector<T> data); // ST1B

  /// T: uint, ulong
  public static unsafe void Scatter8BitNarrowing(Vector<T> mask, Vector<T> addresses, Vector<T> data); // ST1B

  /// T: int, long
  public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<T> mask, sbyte* address, Vector<T> offsets, Vector<T> data); // ST1B

  /// T: [uint, int], [ulong, long]
  public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<T> mask, byte* address, Vector<T2> offsets, Vector<T> data); // ST1B

  /// T: [int, uint], [long, ulong]
  public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<T> mask, sbyte* address, Vector<T2> offsets, Vector<T> data); // ST1B

  /// T: uint, ulong
  public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<T> mask, byte* address, Vector<T> offsets, Vector<T> data); // ST1B

  /// total method signatures: 28

}


/// Full API
public abstract partial class Sve : AdvSimd /// Feature: FEAT_SVE  Category: scatterstores
{
    /// Scatter : Non-truncating store

    /// void svst1_scatter[_u32base_f32](svbool_t pg, svuint32_t bases, svfloat32_t data) : "ST1W Zdata.S, Pg, [Zbases.S, #0]"
  public static unsafe void Scatter(Vector<float> mask, Vector<uint> addresses, Vector<float> data);

    /// void svst1_scatter[_u32base_s32](svbool_t pg, svuint32_t bases, svint32_t data) : "ST1W Zdata.S, Pg, [Zbases.S, #0]"
  public static unsafe void Scatter(Vector<int> mask, Vector<uint> addresses, Vector<int> data);

    /// void svst1_scatter[_u32base_u32](svbool_t pg, svuint32_t bases, svuint32_t data) : "ST1W Zdata.S, Pg, [Zbases.S, #0]"
  public static unsafe void Scatter(Vector<uint> mask, Vector<uint> addresses, Vector<uint> data);

    /// void svst1_scatter[_u64base_f64](svbool_t pg, svuint64_t bases, svfloat64_t data) : "ST1D Zdata.D, Pg, [Zbases.D, #0]"
  public static unsafe void Scatter(Vector<double> mask, Vector<ulong> addresses, Vector<double> data);

    /// void svst1_scatter[_u64base_s64](svbool_t pg, svuint64_t bases, svint64_t data) : "ST1D Zdata.D, Pg, [Zbases.D, #0]"
  public static unsafe void Scatter(Vector<long> mask, Vector<ulong> addresses, Vector<long> data);

    /// void svst1_scatter[_u64base_u64](svbool_t pg, svuint64_t bases, svuint64_t data) : "ST1D Zdata.D, Pg, [Zbases.D, #0]"
  public static unsafe void Scatter(Vector<ulong> mask, Vector<ulong> addresses, Vector<ulong> data);

    /// void svst1_scatter_[s32]offset[_f32](svbool_t pg, float32_t *base, svint32_t offsets, svfloat32_t data) : "ST1W Zdata.S, Pg, [Xbase, Zoffsets.S, SXTW]"
    /// void svst1_scatter_[s32]index[_f32](svbool_t pg, float32_t *base, svint32_t indices, svfloat32_t data) : "ST1W Zdata.S, Pg, [Xbase, Zindices.S, SXTW #2]"
  public static unsafe void Scatter(Vector<float> mask, float* address, Vector<int> indicies, Vector<float> data);

    /// void svst1_scatter_[s32]offset[_s32](svbool_t pg, int32_t *base, svint32_t offsets, svint32_t data) : "ST1W Zdata.S, Pg, [Xbase, Zoffsets.S, SXTW]"
    /// void svst1_scatter_[s32]index[_s32](svbool_t pg, int32_t *base, svint32_t indices, svint32_t data) : "ST1W Zdata.S, Pg, [Xbase, Zindices.S, SXTW #2]"
  public static unsafe void Scatter(Vector<int> mask, int* address, Vector<int> indicies, Vector<int> data);

    /// void svst1_scatter_[s32]offset[_u32](svbool_t pg, uint32_t *base, svint32_t offsets, svuint32_t data) : "ST1W Zdata.S, Pg, [Xbase, Zoffsets.S, SXTW]"
    /// void svst1_scatter_[s32]index[_u32](svbool_t pg, uint32_t *base, svint32_t indices, svuint32_t data) : "ST1W Zdata.S, Pg, [Xbase, Zindices.S, SXTW #2]"
  public static unsafe void Scatter(Vector<uint> mask, uint* address, Vector<int> indicies, Vector<uint> data);

    /// void svst1_scatter_[u32]offset[_f32](svbool_t pg, float32_t *base, svuint32_t offsets, svfloat32_t data) : "ST1W Zdata.S, Pg, [Xbase, Zoffsets.S, UXTW]"
    /// void svst1_scatter_[u32]index[_f32](svbool_t pg, float32_t *base, svuint32_t indices, svfloat32_t data) : "ST1W Zdata.S, Pg, [Xbase, Zindices.S, UXTW #2]"
  public static unsafe void Scatter(Vector<float> mask, float* address, Vector<uint> indicies, Vector<float> data);

    /// void svst1_scatter_[u32]offset[_s32](svbool_t pg, int32_t *base, svuint32_t offsets, svint32_t data) : "ST1W Zdata.S, Pg, [Xbase, Zoffsets.S, UXTW]"
    /// void svst1_scatter_[u32]index[_s32](svbool_t pg, int32_t *base, svuint32_t indices, svint32_t data) : "ST1W Zdata.S, Pg, [Xbase, Zindices.S, UXTW #2]"
  public static unsafe void Scatter(Vector<int> mask, int* address, Vector<uint> indicies, Vector<int> data);

    /// void svst1_scatter_[u32]offset[_u32](svbool_t pg, uint32_t *base, svuint32_t offsets, svuint32_t data) : "ST1W Zdata.S, Pg, [Xbase, Zoffsets.S, UXTW]"
    /// void svst1_scatter_[u32]index[_u32](svbool_t pg, uint32_t *base, svuint32_t indices, svuint32_t data) : "ST1W Zdata.S, Pg, [Xbase, Zindices.S, UXTW #2]"
  public static unsafe void Scatter(Vector<uint> mask, uint* address, Vector<uint> indicies, Vector<uint> data);

    /// void svst1_scatter_[s64]offset[_f64](svbool_t pg, float64_t *base, svint64_t offsets, svfloat64_t data) : "ST1D Zdata.D, Pg, [Xbase, Zoffsets.D]"
    /// void svst1_scatter_[s64]index[_f64](svbool_t pg, float64_t *base, svint64_t indices, svfloat64_t data) : "ST1D Zdata.D, Pg, [Xbase, Zindices.D, LSL #3]"
  public static unsafe void Scatter(Vector<double> mask, double* address, Vector<long> indicies, Vector<double> data);

    /// void svst1_scatter_[s64]offset[_s64](svbool_t pg, int64_t *base, svint64_t offsets, svint64_t data) : "ST1D Zdata.D, Pg, [Xbase, Zoffsets.D]"
    /// void svst1_scatter_[s64]index[_s64](svbool_t pg, int64_t *base, svint64_t indices, svint64_t data) : "ST1D Zdata.D, Pg, [Xbase, Zindices.D, LSL #3]"
  public static unsafe void Scatter(Vector<long> mask, long* address, Vector<long> indicies, Vector<long> data);

    /// void svst1_scatter_[s64]offset[_u64](svbool_t pg, uint64_t *base, svint64_t offsets, svuint64_t data) : "ST1D Zdata.D, Pg, [Xbase, Zoffsets.D]"
    /// void svst1_scatter_[s64]index[_u64](svbool_t pg, uint64_t *base, svint64_t indices, svuint64_t data) : "ST1D Zdata.D, Pg, [Xbase, Zindices.D, LSL #3]"
  public static unsafe void Scatter(Vector<ulong> mask, ulong* address, Vector<long> indicies, Vector<ulong> data);

    /// void svst1_scatter_[u64]offset[_f64](svbool_t pg, float64_t *base, svuint64_t offsets, svfloat64_t data) : "ST1D Zdata.D, Pg, [Xbase, Zoffsets.D]"
    /// void svst1_scatter_[u64]index[_f64](svbool_t pg, float64_t *base, svuint64_t indices, svfloat64_t data) : "ST1D Zdata.D, Pg, [Xbase, Zindices.D, LSL #3]"
  public static unsafe void Scatter(Vector<double> mask, double* address, Vector<ulong> indicies, Vector<double> data);

    /// void svst1_scatter_[u64]offset[_s64](svbool_t pg, int64_t *base, svuint64_t offsets, svint64_t data) : "ST1D Zdata.D, Pg, [Xbase, Zoffsets.D]"
    /// void svst1_scatter_[u64]index[_s64](svbool_t pg, int64_t *base, svuint64_t indices, svint64_t data) : "ST1D Zdata.D, Pg, [Xbase, Zindices.D, LSL #3]"
  public static unsafe void Scatter(Vector<long> mask, long* address, Vector<ulong> indicies, Vector<long> data);

    /// void svst1_scatter_[u64]offset[_u64](svbool_t pg, uint64_t *base, svuint64_t offsets, svuint64_t data) : "ST1D Zdata.D, Pg, [Xbase, Zoffsets.D]"
    /// void svst1_scatter_[u64]index[_u64](svbool_t pg, uint64_t *base, svuint64_t indices, svuint64_t data) : "ST1D Zdata.D, Pg, [Xbase, Zindices.D, LSL #3]"
  public static unsafe void Scatter(Vector<ulong> mask, ulong* address, Vector<ulong> indicies, Vector<ulong> data);


    /// Scatter16BitNarrowing : Truncate to 16 bits and store

    /// void svst1h_scatter[_u32base_s32](svbool_t pg, svuint32_t bases, svint32_t data) : "ST1H Zdata.S, Pg, [Zbases.S, #0]"
  public static unsafe void Scatter16BitNarrowing(Vector<int> mask, Vector<uint> addresses, Vector<int> data);

    /// void svst1h_scatter[_u32base_u32](svbool_t pg, svuint32_t bases, svuint32_t data) : "ST1H Zdata.S, Pg, [Zbases.S, #0]"
  public static unsafe void Scatter16BitNarrowing(Vector<uint> mask, Vector<uint> addresses, Vector<uint> data);

    /// void svst1h_scatter[_u64base_s64](svbool_t pg, svuint64_t bases, svint64_t data) : "ST1H Zdata.D, Pg, [Zbases.D, #0]"
  public static unsafe void Scatter16BitNarrowing(Vector<long> mask, Vector<ulong> addresses, Vector<long> data);

    /// void svst1h_scatter[_u64base_u64](svbool_t pg, svuint64_t bases, svuint64_t data) : "ST1H Zdata.D, Pg, [Zbases.D, #0]"
  public static unsafe void Scatter16BitNarrowing(Vector<ulong> mask, Vector<ulong> addresses, Vector<ulong> data);


    /// Scatter16BitWithByteOffsetsNarrowing : Truncate to 16 bits and store

    /// void svst1h_scatter_[s32]offset[_s32](svbool_t pg, int16_t *base, svint32_t offsets, svint32_t data) : "ST1H Zdata.S, Pg, [Xbase, Zoffsets.S, SXTW]"
  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<int> mask, short* address, Vector<int> offsets, Vector<int> data);

    /// void svst1h_scatter_[s32]offset[_u32](svbool_t pg, uint16_t *base, svint32_t offsets, svuint32_t data) : "ST1H Zdata.S, Pg, [Xbase, Zoffsets.S, SXTW]"
  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<uint> mask, ushort* address, Vector<int> offsets, Vector<uint> data);

    /// void svst1h_scatter_[u32]offset[_s32](svbool_t pg, int16_t *base, svuint32_t offsets, svint32_t data) : "ST1H Zdata.S, Pg, [Xbase, Zoffsets.S, UXTW]"
  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<int> mask, short* address, Vector<uint> offsets, Vector<int> data);

    /// void svst1h_scatter_[u32]offset[_u32](svbool_t pg, uint16_t *base, svuint32_t offsets, svuint32_t data) : "ST1H Zdata.S, Pg, [Xbase, Zoffsets.S, UXTW]"
  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<uint> mask, ushort* address, Vector<uint> offsets, Vector<uint> data);

    /// void svst1h_scatter_[s64]offset[_s64](svbool_t pg, int16_t *base, svint64_t offsets, svint64_t data) : "ST1H Zdata.D, Pg, [Xbase, Zoffsets.D]"
  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<long> mask, short* address, Vector<long> offsets, Vector<long> data);

    /// void svst1h_scatter_[s64]offset[_u64](svbool_t pg, uint16_t *base, svint64_t offsets, svuint64_t data) : "ST1H Zdata.D, Pg, [Xbase, Zoffsets.D]"
  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<ulong> mask, ushort* address, Vector<long> offsets, Vector<ulong> data);

    /// void svst1h_scatter_[u64]offset[_s64](svbool_t pg, int16_t *base, svuint64_t offsets, svint64_t data) : "ST1H Zdata.D, Pg, [Xbase, Zoffsets.D]"
  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<long> mask, short* address, Vector<ulong> offsets, Vector<long> data);

    /// void svst1h_scatter_[u64]offset[_u64](svbool_t pg, uint16_t *base, svuint64_t offsets, svuint64_t data) : "ST1H Zdata.D, Pg, [Xbase, Zoffsets.D]"
  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<ulong> mask, ushort* address, Vector<ulong> offsets, Vector<ulong> data);

    /// void svst1h_scatter_[s32]index[_s32](svbool_t pg, int16_t *base, svint32_t indices, svint32_t data) : "ST1H Zdata.S, Pg, [Xbase, Zindices.S, SXTW #1]"
  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<int> mask, short* address, Vector<int> indices, Vector<int> data);

    /// void svst1h_scatter_[s32]index[_u32](svbool_t pg, uint16_t *base, svint32_t indices, svuint32_t data) : "ST1H Zdata.S, Pg, [Xbase, Zindices.S, SXTW #1]"
  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<uint> mask, ushort* address, Vector<int> indices, Vector<uint> data);

    /// void svst1h_scatter_[u32]index[_s32](svbool_t pg, int16_t *base, svuint32_t indices, svint32_t data) : "ST1H Zdata.S, Pg, [Xbase, Zindices.S, UXTW #1]"
  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<int> mask, short* address, Vector<uint> indices, Vector<int> data);

    /// void svst1h_scatter_[u32]index[_u32](svbool_t pg, uint16_t *base, svuint32_t indices, svuint32_t data) : "ST1H Zdata.S, Pg, [Xbase, Zindices.S, UXTW #1]"
  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<uint> mask, ushort* address, Vector<uint> indices, Vector<uint> data);

    /// void svst1h_scatter_[s64]index[_s64](svbool_t pg, int16_t *base, svint64_t indices, svint64_t data) : "ST1H Zdata.D, Pg, [Xbase, Zindices.D, LSL #1]"
  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<long> mask, short* address, Vector<long> indices, Vector<long> data);

    /// void svst1h_scatter_[s64]index[_u64](svbool_t pg, uint16_t *base, svint64_t indices, svuint64_t data) : "ST1H Zdata.D, Pg, [Xbase, Zindices.D, LSL #1]"
  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<ulong> mask, ushort* address, Vector<long> indices, Vector<ulong> data);

    /// void svst1h_scatter_[u64]index[_s64](svbool_t pg, int16_t *base, svuint64_t indices, svint64_t data) : "ST1H Zdata.D, Pg, [Xbase, Zindices.D, LSL #1]"
  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<long> mask, short* address, Vector<ulong> indices, Vector<long> data);

    /// void svst1h_scatter_[u64]index[_u64](svbool_t pg, uint16_t *base, svuint64_t indices, svuint64_t data) : "ST1H Zdata.D, Pg, [Xbase, Zindices.D, LSL #1]"
  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<ulong> mask, ushort* address, Vector<ulong> indices, Vector<ulong> data);


    /// Scatter32BitNarrowing : Truncate to 32 bits and store

    /// void svst1w_scatter[_u64base_s64](svbool_t pg, svuint64_t bases, svint64_t data) : "ST1W Zdata.D, Pg, [Zbases.D, #0]"
  public static unsafe void Scatter32BitNarrowing(Vector<long> mask, Vector<ulong> addresses, Vector<long> data);

    /// void svst1w_scatter[_u64base_u64](svbool_t pg, svuint64_t bases, svuint64_t data) : "ST1W Zdata.D, Pg, [Zbases.D, #0]"
  public static unsafe void Scatter32BitNarrowing(Vector<ulong> mask, Vector<ulong> addresses, Vector<ulong> data);


    /// Scatter32BitWithByteOffsetsNarrowing : Truncate to 32 bits and store

    /// void svst1w_scatter_[s64]offset[_s64](svbool_t pg, int32_t *base, svint64_t offsets, svint64_t data) : "ST1W Zdata.D, Pg, [Xbase, Zoffsets.D]"
  public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<long> mask, int* address, Vector<long> offsets, Vector<long> data);

    /// void svst1w_scatter_[s64]offset[_u64](svbool_t pg, uint32_t *base, svint64_t offsets, svuint64_t data) : "ST1W Zdata.D, Pg, [Xbase, Zoffsets.D]"
  public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<ulong> mask, uint* address, Vector<long> offsets, Vector<ulong> data);

    /// void svst1w_scatter_[u64]offset[_s64](svbool_t pg, int32_t *base, svuint64_t offsets, svint64_t data) : "ST1W Zdata.D, Pg, [Xbase, Zoffsets.D]"
  public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<long> mask, int* address, Vector<ulong> offsets, Vector<long> data);

    /// void svst1w_scatter_[u64]offset[_u64](svbool_t pg, uint32_t *base, svuint64_t offsets, svuint64_t data) : "ST1W Zdata.D, Pg, [Xbase, Zoffsets.D]"
  public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<ulong> mask, uint* address, Vector<ulong> offsets, Vector<ulong> data);

    /// void svst1w_scatter_[s64]index[_s64](svbool_t pg, int32_t *base, svint64_t indices, svint64_t data) : "ST1W Zdata.D, Pg, [Xbase, Zindices.D, LSL #2]"
  public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<long> mask, int* address, Vector<long> indices, Vector<long> data);

    /// void svst1w_scatter_[s64]index[_u64](svbool_t pg, uint32_t *base, svint64_t indices, svuint64_t data) : "ST1W Zdata.D, Pg, [Xbase, Zindices.D, LSL #2]"
  public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<ulong> mask, uint* address, Vector<long> indices, Vector<ulong> data);

    /// void svst1w_scatter_[u64]index[_s64](svbool_t pg, int32_t *base, svuint64_t indices, svint64_t data) : "ST1W Zdata.D, Pg, [Xbase, Zindices.D, LSL #2]"
  public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<long> mask, int* address, Vector<ulong> indices, Vector<long> data);

    /// void svst1w_scatter_[u64]index[_u64](svbool_t pg, uint32_t *base, svuint64_t indices, svuint64_t data) : "ST1W Zdata.D, Pg, [Xbase, Zindices.D, LSL #2]"
  public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<ulong> mask, uint* address, Vector<ulong> indices, Vector<ulong> data);


    /// Scatter8BitNarrowing : Truncate to 8 bits and store

    /// void svst1b_scatter[_u32base_s32](svbool_t pg, svuint32_t bases, svint32_t data) : "ST1B Zdata.S, Pg, [Zbases.S, #0]"
  public static unsafe void Scatter8BitNarrowing(Vector<int> mask, Vector<uint> addresses, Vector<int> data);

    /// void svst1b_scatter[_u32base_u32](svbool_t pg, svuint32_t bases, svuint32_t data) : "ST1B Zdata.S, Pg, [Zbases.S, #0]"
  public static unsafe void Scatter8BitNarrowing(Vector<uint> mask, Vector<uint> addresses, Vector<uint> data);

    /// void svst1b_scatter[_u64base_s64](svbool_t pg, svuint64_t bases, svint64_t data) : "ST1B Zdata.D, Pg, [Zbases.D, #0]"
  public static unsafe void Scatter8BitNarrowing(Vector<long> mask, Vector<ulong> addresses, Vector<long> data);

    /// void svst1b_scatter[_u64base_u64](svbool_t pg, svuint64_t bases, svuint64_t data) : "ST1B Zdata.D, Pg, [Zbases.D, #0]"
  public static unsafe void Scatter8BitNarrowing(Vector<ulong> mask, Vector<ulong> addresses, Vector<ulong> data);


    /// Scatter8BitWithByteOffsetsNarrowing : Truncate to 8 bits and store

    /// void svst1b_scatter_[s32]offset[_s32](svbool_t pg, int8_t *base, svint32_t offsets, svint32_t data) : "ST1B Zdata.S, Pg, [Xbase, Zoffsets.S, SXTW]"
  public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<int> mask, sbyte* address, Vector<int> offsets, Vector<int> data);

    /// void svst1b_scatter_[s32]offset[_u32](svbool_t pg, uint8_t *base, svint32_t offsets, svuint32_t data) : "ST1B Zdata.S, Pg, [Xbase, Zoffsets.S, SXTW]"
  public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<uint> mask, byte* address, Vector<int> offsets, Vector<uint> data);

    /// void svst1b_scatter_[u32]offset[_s32](svbool_t pg, int8_t *base, svuint32_t offsets, svint32_t data) : "ST1B Zdata.S, Pg, [Xbase, Zoffsets.S, UXTW]"
  public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<int> mask, sbyte* address, Vector<uint> offsets, Vector<int> data);

    /// void svst1b_scatter_[u32]offset[_u32](svbool_t pg, uint8_t *base, svuint32_t offsets, svuint32_t data) : "ST1B Zdata.S, Pg, [Xbase, Zoffsets.S, UXTW]"
  public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<uint> mask, byte* address, Vector<uint> offsets, Vector<uint> data);

    /// void svst1b_scatter_[s64]offset[_s64](svbool_t pg, int8_t *base, svint64_t offsets, svint64_t data) : "ST1B Zdata.D, Pg, [Xbase, Zoffsets.D]"
  public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<long> mask, sbyte* address, Vector<long> offsets, Vector<long> data);

    /// void svst1b_scatter_[s64]offset[_u64](svbool_t pg, uint8_t *base, svint64_t offsets, svuint64_t data) : "ST1B Zdata.D, Pg, [Xbase, Zoffsets.D]"
  public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<ulong> mask, byte* address, Vector<long> offsets, Vector<ulong> data);

    /// void svst1b_scatter_[u64]offset[_s64](svbool_t pg, int8_t *base, svuint64_t offsets, svint64_t data) : "ST1B Zdata.D, Pg, [Xbase, Zoffsets.D]"
  public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<long> mask, sbyte* address, Vector<ulong> offsets, Vector<long> data);

    /// void svst1b_scatter_[u64]offset[_u64](svbool_t pg, uint8_t *base, svuint64_t offsets, svuint64_t data) : "ST1B Zdata.D, Pg, [Xbase, Zoffsets.D]"
  public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<ulong> mask, byte* address, Vector<ulong> offsets, Vector<ulong> data);


  /// total method signatures: 60
  /// total method names:      7
}


  /// Rejected:
  ///   public static unsafe void Scatter(Vector<float> mask, Vector<uint> address, long indicies, Vector<float> data); // svst1_scatter[_u32base]_offset[_f32] or svst1_scatter[_u32base]_index[_f32]
  ///   public static unsafe void Scatter(Vector<int> mask, Vector<uint> address, long indicies, Vector<int> data); // svst1_scatter[_u32base]_offset[_s32] or svst1_scatter[_u32base]_index[_s32]
  ///   public static unsafe void Scatter(Vector<uint> mask, Vector<uint> address, long indicies, Vector<uint> data); // svst1_scatter[_u32base]_offset[_u32] or svst1_scatter[_u32base]_index[_u32]
  ///   public static unsafe void Scatter(Vector<double> mask, Vector<ulong> address, long indicies, Vector<double> data); // svst1_scatter[_u64base]_offset[_f64] or svst1_scatter[_u64base]_index[_f64]
  ///   public static unsafe void Scatter(Vector<long> mask, Vector<ulong> address, long indicies, Vector<long> data); // svst1_scatter[_u64base]_offset[_s64] or svst1_scatter[_u64base]_index[_s64]
  ///   public static unsafe void Scatter(Vector<ulong> mask, Vector<ulong> address, long indicies, Vector<ulong> data); // svst1_scatter[_u64base]_offset[_u64] or svst1_scatter[_u64base]_index[_u64]
  ///   public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<int> mask, Vector<uint> address, long offset, Vector<int> data); // svst1h_scatter[_u32base]_offset[_s32]
  ///   public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<uint> mask, Vector<uint> address, long offset, Vector<uint> data); // svst1h_scatter[_u32base]_offset[_u32]
  ///   public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<long> mask, Vector<ulong> address, long offset, Vector<long> data); // svst1h_scatter[_u64base]_offset[_s64]
  ///   public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<ulong> mask, Vector<ulong> address, long offset, Vector<ulong> data); // svst1h_scatter[_u64base]_offset[_u64]
  ///   public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<int> mask, Vector<uint> address, long index, Vector<int> data); // svst1h_scatter[_u32base]_index[_s32]
  ///   public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<uint> mask, Vector<uint> address, long index, Vector<uint> data); // svst1h_scatter[_u32base]_index[_u32]
  ///   public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<long> mask, Vector<ulong> address, long index, Vector<long> data); // svst1h_scatter[_u64base]_index[_s64]
  ///   public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<ulong> mask, Vector<ulong> address, long index, Vector<ulong> data); // svst1h_scatter[_u64base]_index[_u64]
  ///   public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<long> mask, Vector<ulong> address, long offset, Vector<long> data); // svst1w_scatter[_u64base]_offset[_s64]
  ///   public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<ulong> mask, Vector<ulong> address, long offset, Vector<ulong> data); // svst1w_scatter[_u64base]_offset[_u64]
  ///   public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<long> mask, Vector<ulong> address, long index, Vector<long> data); // svst1w_scatter[_u64base]_index[_s64]
  ///   public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<ulong> mask, Vector<ulong> address, long index, Vector<ulong> data); // svst1w_scatter[_u64base]_index[_u64]
  ///   public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<int> mask, Vector<uint> address, long offset, Vector<int> data); // svst1b_scatter[_u32base]_offset[_s32]
  ///   public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<uint> mask, Vector<uint> address, long offset, Vector<uint> data); // svst1b_scatter[_u32base]_offset[_u32]
  ///   public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<long> mask, Vector<ulong> address, long offset, Vector<long> data); // svst1b_scatter[_u64base]_offset[_s64]
  ///   public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<ulong> mask, Vector<ulong> address, long offset, Vector<ulong> data); // svst1b_scatter[_u64base]_offset[_u64]
  ///   Total Rejected: 22

  /// Total ACLE covered across API:      100

