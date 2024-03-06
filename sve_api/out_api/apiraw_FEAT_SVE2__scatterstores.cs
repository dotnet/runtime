namespace System.Runtime.Intrinsics.Arm;

/// VectorT Summary
public abstract partial class Sve2 : AdvSimd /// Feature: FEAT_SVE2  Category: scatterstores
{

  /// T: [int, uint], [long, ulong]
  public static unsafe void Scatter16BitNarrowing(Vector<T> mask, Vector<T2> addresses, Vector<T> data); // STNT1H

  /// T: uint, ulong
  public static unsafe void Scatter16BitNarrowing(Vector<T> mask, Vector<T> addresses, Vector<T> data); // STNT1H

  /// T: [int, uint], [long, ulong]
  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<T> mask, short* address, Vector<T2> offsets, Vector<T> data); // STNT1H

  /// T: uint, ulong
  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<T> mask, ushort* address, Vector<T> offsets, Vector<T> data); // STNT1H

  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<long> mask, short* address, Vector<long> offsets, Vector<long> data); // STNT1H

  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<ulong> mask, ushort* address, Vector<long> offsets, Vector<ulong> data); // STNT1H

  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<long> mask, short* address, Vector<long> indices, Vector<long> data); // STNT1H

  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<ulong> mask, ushort* address, Vector<long> indices, Vector<ulong> data); // STNT1H

  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<long> mask, short* address, Vector<ulong> indices, Vector<long> data); // STNT1H

  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<ulong> mask, ushort* address, Vector<ulong> indices, Vector<ulong> data); // STNT1H

  public static unsafe void Scatter32BitNarrowing(Vector<long> mask, Vector<ulong> addresses, Vector<long> data); // STNT1W

  public static unsafe void Scatter32BitNarrowing(Vector<ulong> mask, Vector<ulong> addresses, Vector<ulong> data); // STNT1W

  public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<long> mask, int* address, Vector<long> offsets, Vector<long> data); // STNT1W

  public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<ulong> mask, uint* address, Vector<long> offsets, Vector<ulong> data); // STNT1W

  public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<long> mask, int* address, Vector<ulong> offsets, Vector<long> data); // STNT1W

  public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<ulong> mask, uint* address, Vector<ulong> offsets, Vector<ulong> data); // STNT1W

  public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<long> mask, int* address, Vector<long> indices, Vector<long> data); // STNT1W

  public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<ulong> mask, uint* address, Vector<long> indices, Vector<ulong> data); // STNT1W

  public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<long> mask, int* address, Vector<ulong> indices, Vector<long> data); // STNT1W

  public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<ulong> mask, uint* address, Vector<ulong> indices, Vector<ulong> data); // STNT1W

  /// T: [int, uint], [long, ulong]
  public static unsafe void Scatter8BitNarrowing(Vector<T> mask, Vector<T2> addresses, Vector<T> data); // STNT1B

  /// T: uint, ulong
  public static unsafe void Scatter8BitNarrowing(Vector<T> mask, Vector<T> addresses, Vector<T> data); // STNT1B

  /// T: [int, uint], [long, ulong]
  public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<T> mask, sbyte* address, Vector<T2> offsets, Vector<T> data); // STNT1B

  /// T: uint, ulong
  public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<T> mask, byte* address, Vector<T> offsets, Vector<T> data); // STNT1B

  public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<long> mask, sbyte* address, Vector<long> offsets, Vector<long> data); // STNT1B

  public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<ulong> mask, byte* address, Vector<long> offsets, Vector<ulong> data); // STNT1B

  /// T: [float, uint], [int, uint], [double, ulong], [long, ulong]
  public static unsafe void ScatterNonTemporal(Vector<T> mask, Vector<T2> addresses, Vector<T> data); // STNT1W or STNT1D

  /// T: uint, ulong
  public static unsafe void ScatterNonTemporal(Vector<T> mask, Vector<T> addresses, Vector<T> data); // STNT1W or STNT1D

  /// T: [float, uint], [int, uint], [double, long], [ulong, long], [double, ulong], [long, ulong]
  public static unsafe void ScatterNonTemporal(Vector<T> mask, T* base, Vector<T2> offsets, Vector<T> data); // STNT1W or STNT1D

  /// T: uint, long, ulong
  public static unsafe void ScatterNonTemporal(Vector<T> mask, T* base, Vector<T> offsets, Vector<T> data); // STNT1W or STNT1D

  /// T: [double, long], [ulong, long], [double, ulong], [long, ulong]
  public static unsafe void ScatterNonTemporal(Vector<T> mask, T* base, Vector<T2> indices, Vector<T> data); // STNT1D

  /// T: long, ulong
  public static unsafe void ScatterNonTemporal(Vector<T> mask, T* base, Vector<T> indices, Vector<T> data); // STNT1D

  /// total method signatures: 32

}


/// Full API
public abstract partial class Sve2 : AdvSimd /// Feature: FEAT_SVE2  Category: scatterstores
{
    /// Scatter16BitNarrowing : Truncate to 16 bits and store, non-temporal

    /// void svstnt1h_scatter[_u32base_s32](svbool_t pg, svuint32_t bases, svint32_t data) : "STNT1H Zdata.S, Pg, [Zbases.S, XZR]"
  public static unsafe void Scatter16BitNarrowing(Vector<int> mask, Vector<uint> addresses, Vector<int> data);

    /// void svstnt1h_scatter[_u32base_u32](svbool_t pg, svuint32_t bases, svuint32_t data) : "STNT1H Zdata.S, Pg, [Zbases.S, XZR]"
  public static unsafe void Scatter16BitNarrowing(Vector<uint> mask, Vector<uint> addresses, Vector<uint> data);

    /// void svstnt1h_scatter[_u64base_s64](svbool_t pg, svuint64_t bases, svint64_t data) : "STNT1H Zdata.D, Pg, [Zbases.D, XZR]"
  public static unsafe void Scatter16BitNarrowing(Vector<long> mask, Vector<ulong> addresses, Vector<long> data);

    /// void svstnt1h_scatter[_u64base_u64](svbool_t pg, svuint64_t bases, svuint64_t data) : "STNT1H Zdata.D, Pg, [Zbases.D, XZR]"
  public static unsafe void Scatter16BitNarrowing(Vector<ulong> mask, Vector<ulong> addresses, Vector<ulong> data);


    /// Scatter16BitWithByteOffsetsNarrowing : Truncate to 16 bits and store, non-temporal

    /// void svstnt1h_scatter_[u32]offset[_s32](svbool_t pg, int16_t *base, svuint32_t offsets, svint32_t data) : "STNT1H Zdata.S, Pg, [Zoffsets.S, Xbase]"
  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<int> mask, short* address, Vector<uint> offsets, Vector<int> data);

    /// void svstnt1h_scatter_[u32]offset[_u32](svbool_t pg, uint16_t *base, svuint32_t offsets, svuint32_t data) : "STNT1H Zdata.S, Pg, [Zoffsets.S, Xbase]"
  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<uint> mask, ushort* address, Vector<uint> offsets, Vector<uint> data);

    /// void svstnt1h_scatter_[s64]offset[_s64](svbool_t pg, int16_t *base, svint64_t offsets, svint64_t data) : "STNT1H Zdata.D, Pg, [Zoffsets.D, Xbase]"
  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<long> mask, short* address, Vector<long> offsets, Vector<long> data);

    /// void svstnt1h_scatter_[s64]offset[_u64](svbool_t pg, uint16_t *base, svint64_t offsets, svuint64_t data) : "STNT1H Zdata.D, Pg, [Zoffsets.D, Xbase]"
  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<ulong> mask, ushort* address, Vector<long> offsets, Vector<ulong> data);

    /// void svstnt1h_scatter_[u64]offset[_s64](svbool_t pg, int16_t *base, svuint64_t offsets, svint64_t data) : "STNT1H Zdata.D, Pg, [Zoffsets.D, Xbase]"
  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<long> mask, short* address, Vector<ulong> offsets, Vector<long> data);

    /// void svstnt1h_scatter_[u64]offset[_u64](svbool_t pg, uint16_t *base, svuint64_t offsets, svuint64_t data) : "STNT1H Zdata.D, Pg, [Zoffsets.D, Xbase]"
  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<ulong> mask, ushort* address, Vector<ulong> offsets, Vector<ulong> data);

    /// void svstnt1h_scatter_[s64]index[_s64](svbool_t pg, int16_t *base, svint64_t indices, svint64_t data) : "STNT1H Zdata.D, Pg, [Zoffsets.D, Xbase]"
  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<long> mask, short* address, Vector<long> indices, Vector<long> data);

    /// void svstnt1h_scatter_[s64]index[_u64](svbool_t pg, uint16_t *base, svint64_t indices, svuint64_t data) : "STNT1H Zdata.D, Pg, [Zoffsets.D, Xbase]"
  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<ulong> mask, ushort* address, Vector<long> indices, Vector<ulong> data);

    /// void svstnt1h_scatter_[u64]index[_s64](svbool_t pg, int16_t *base, svuint64_t indices, svint64_t data) : "STNT1H Zdata.D, Pg, [Zoffsets.D, Xbase]"
  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<long> mask, short* address, Vector<ulong> indices, Vector<long> data);

    /// void svstnt1h_scatter_[u64]index[_u64](svbool_t pg, uint16_t *base, svuint64_t indices, svuint64_t data) : "STNT1H Zdata.D, Pg, [Zoffsets.D, Xbase]"
  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<ulong> mask, ushort* address, Vector<ulong> indices, Vector<ulong> data);


    /// Scatter32BitNarrowing : Truncate to 32 bits and store, non-temporal

    /// void svstnt1w_scatter[_u64base_s64](svbool_t pg, svuint64_t bases, svint64_t data) : "STNT1W Zdata.D, Pg, [Zbases.D, XZR]"
  public static unsafe void Scatter32BitNarrowing(Vector<long> mask, Vector<ulong> addresses, Vector<long> data);

    /// void svstnt1w_scatter[_u64base_u64](svbool_t pg, svuint64_t bases, svuint64_t data) : "STNT1W Zdata.D, Pg, [Zbases.D, XZR]"
  public static unsafe void Scatter32BitNarrowing(Vector<ulong> mask, Vector<ulong> addresses, Vector<ulong> data);


    /// Scatter32BitWithByteOffsetsNarrowing : Truncate to 32 bits and store, non-temporal

    /// void svstnt1w_scatter_[s64]offset[_s64](svbool_t pg, int32_t *base, svint64_t offsets, svint64_t data) : "STNT1W Zdata.D, Pg, [Zoffsets.D, Xbase]"
  public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<long> mask, int* address, Vector<long> offsets, Vector<long> data);

    /// void svstnt1w_scatter_[s64]offset[_u64](svbool_t pg, uint32_t *base, svint64_t offsets, svuint64_t data) : "STNT1W Zdata.D, Pg, [Zoffsets.D, Xbase]"
  public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<ulong> mask, uint* address, Vector<long> offsets, Vector<ulong> data);

    /// void svstnt1w_scatter_[u64]offset[_s64](svbool_t pg, int32_t *base, svuint64_t offsets, svint64_t data) : "STNT1W Zdata.D, Pg, [Zoffsets.D, Xbase]"
  public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<long> mask, int* address, Vector<ulong> offsets, Vector<long> data);

    /// void svstnt1w_scatter_[u64]offset[_u64](svbool_t pg, uint32_t *base, svuint64_t offsets, svuint64_t data) : "STNT1W Zdata.D, Pg, [Zoffsets.D, Xbase]"
  public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<ulong> mask, uint* address, Vector<ulong> offsets, Vector<ulong> data);

    /// void svstnt1w_scatter_[s64]index[_s64](svbool_t pg, int32_t *base, svint64_t indices, svint64_t data) : "STNT1W Zdata.D, Pg, [Zoffsets.D, Xbase]"
  public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<long> mask, int* address, Vector<long> indices, Vector<long> data);

    /// void svstnt1w_scatter_[s64]index[_u64](svbool_t pg, uint32_t *base, svint64_t indices, svuint64_t data) : "STNT1W Zdata.D, Pg, [Zoffsets.D, Xbase]"
  public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<ulong> mask, uint* address, Vector<long> indices, Vector<ulong> data);

    /// void svstnt1w_scatter_[u64]index[_s64](svbool_t pg, int32_t *base, svuint64_t indices, svint64_t data) : "STNT1W Zdata.D, Pg, [Zoffsets.D, Xbase]"
  public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<long> mask, int* address, Vector<ulong> indices, Vector<long> data);

    /// void svstnt1w_scatter_[u64]index[_u64](svbool_t pg, uint32_t *base, svuint64_t indices, svuint64_t data) : "STNT1W Zdata.D, Pg, [Zoffsets.D, Xbase]"
  public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<ulong> mask, uint* address, Vector<ulong> indices, Vector<ulong> data);


    /// Scatter8BitNarrowing : Truncate to 8 bits and store, non-temporal

    /// void svstnt1b_scatter[_u32base_s32](svbool_t pg, svuint32_t bases, svint32_t data) : "STNT1B Zdata.S, Pg, [Zbases.S, XZR]"
  public static unsafe void Scatter8BitNarrowing(Vector<int> mask, Vector<uint> addresses, Vector<int> data);

    /// void svstnt1b_scatter[_u32base_u32](svbool_t pg, svuint32_t bases, svuint32_t data) : "STNT1B Zdata.S, Pg, [Zbases.S, XZR]"
  public static unsafe void Scatter8BitNarrowing(Vector<uint> mask, Vector<uint> addresses, Vector<uint> data);

    /// void svstnt1b_scatter[_u64base_s64](svbool_t pg, svuint64_t bases, svint64_t data) : "STNT1B Zdata.D, Pg, [Zbases.D, XZR]"
  public static unsafe void Scatter8BitNarrowing(Vector<long> mask, Vector<ulong> addresses, Vector<long> data);

    /// void svstnt1b_scatter[_u64base_u64](svbool_t pg, svuint64_t bases, svuint64_t data) : "STNT1B Zdata.D, Pg, [Zbases.D, XZR]"
  public static unsafe void Scatter8BitNarrowing(Vector<ulong> mask, Vector<ulong> addresses, Vector<ulong> data);


    /// Scatter8BitWithByteOffsetsNarrowing : Truncate to 8 bits and store, non-temporal

    /// void svstnt1b_scatter_[u32]offset[_s32](svbool_t pg, int8_t *base, svuint32_t offsets, svint32_t data) : "STNT1B Zdata.S, Pg, [Zoffsets.S, Xbase]"
  public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<int> mask, sbyte* address, Vector<uint> offsets, Vector<int> data);

    /// void svstnt1b_scatter_[u32]offset[_u32](svbool_t pg, uint8_t *base, svuint32_t offsets, svuint32_t data) : "STNT1B Zdata.S, Pg, [Zoffsets.S, Xbase]"
  public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<uint> mask, byte* address, Vector<uint> offsets, Vector<uint> data);

    /// void svstnt1b_scatter_[s64]offset[_s64](svbool_t pg, int8_t *base, svint64_t offsets, svint64_t data) : "STNT1B Zdata.D, Pg, [Zoffsets.D, Xbase]"
  public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<long> mask, sbyte* address, Vector<long> offsets, Vector<long> data);

    /// void svstnt1b_scatter_[s64]offset[_u64](svbool_t pg, uint8_t *base, svint64_t offsets, svuint64_t data) : "STNT1B Zdata.D, Pg, [Zoffsets.D, Xbase]"
  public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<ulong> mask, byte* address, Vector<long> offsets, Vector<ulong> data);

    /// void svstnt1b_scatter_[u64]offset[_s64](svbool_t pg, int8_t *base, svuint64_t offsets, svint64_t data) : "STNT1B Zdata.D, Pg, [Zoffsets.D, Xbase]"
  public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<long> mask, sbyte* address, Vector<ulong> offsets, Vector<long> data);

    /// void svstnt1b_scatter_[u64]offset[_u64](svbool_t pg, uint8_t *base, svuint64_t offsets, svuint64_t data) : "STNT1B Zdata.D, Pg, [Zoffsets.D, Xbase]"
  public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<ulong> mask, byte* address, Vector<ulong> offsets, Vector<ulong> data);


    /// ScatterNonTemporal : Non-truncating store, non-temporal

    /// void svstnt1_scatter[_u32base_f32](svbool_t pg, svuint32_t bases, svfloat32_t data) : "STNT1W Zdata.S, Pg, [Zbases.S, XZR]"
  public static unsafe void ScatterNonTemporal(Vector<float> mask, Vector<uint> addresses, Vector<float> data);

    /// void svstnt1_scatter[_u32base_s32](svbool_t pg, svuint32_t bases, svint32_t data) : "STNT1W Zdata.S, Pg, [Zbases.S, XZR]"
  public static unsafe void ScatterNonTemporal(Vector<int> mask, Vector<uint> addresses, Vector<int> data);

    /// void svstnt1_scatter[_u32base_u32](svbool_t pg, svuint32_t bases, svuint32_t data) : "STNT1W Zdata.S, Pg, [Zbases.S, XZR]"
  public static unsafe void ScatterNonTemporal(Vector<uint> mask, Vector<uint> addresses, Vector<uint> data);

    /// void svstnt1_scatter[_u64base_f64](svbool_t pg, svuint64_t bases, svfloat64_t data) : "STNT1D Zdata.D, Pg, [Zbases.D, XZR]"
  public static unsafe void ScatterNonTemporal(Vector<double> mask, Vector<ulong> addresses, Vector<double> data);

    /// void svstnt1_scatter[_u64base_s64](svbool_t pg, svuint64_t bases, svint64_t data) : "STNT1D Zdata.D, Pg, [Zbases.D, XZR]"
  public static unsafe void ScatterNonTemporal(Vector<long> mask, Vector<ulong> addresses, Vector<long> data);

    /// void svstnt1_scatter[_u64base_u64](svbool_t pg, svuint64_t bases, svuint64_t data) : "STNT1D Zdata.D, Pg, [Zbases.D, XZR]"
  public static unsafe void ScatterNonTemporal(Vector<ulong> mask, Vector<ulong> addresses, Vector<ulong> data);

    /// void svstnt1_scatter_[u32]offset[_f32](svbool_t pg, float32_t *base, svuint32_t offsets, svfloat32_t data) : "STNT1W Zdata.S, Pg, [Zoffsets.S, Xbase]"
  public static unsafe void ScatterNonTemporal(Vector<float> mask, float* base, Vector<uint> offsets, Vector<float> data);

    /// void svstnt1_scatter_[u32]offset[_s32](svbool_t pg, int32_t *base, svuint32_t offsets, svint32_t data) : "STNT1W Zdata.S, Pg, [Zoffsets.S, Xbase]"
  public static unsafe void ScatterNonTemporal(Vector<int> mask, int* base, Vector<uint> offsets, Vector<int> data);

    /// void svstnt1_scatter_[u32]offset[_u32](svbool_t pg, uint32_t *base, svuint32_t offsets, svuint32_t data) : "STNT1W Zdata.S, Pg, [Zoffsets.S, Xbase]"
  public static unsafe void ScatterNonTemporal(Vector<uint> mask, uint* base, Vector<uint> offsets, Vector<uint> data);

    /// void svstnt1_scatter_[s64]offset[_f64](svbool_t pg, float64_t *base, svint64_t offsets, svfloat64_t data) : "STNT1D Zdata.D, Pg, [Zoffsets.D, Xbase]"
  public static unsafe void ScatterNonTemporal(Vector<double> mask, double* base, Vector<long> offsets, Vector<double> data);

    /// void svstnt1_scatter_[s64]offset[_s64](svbool_t pg, int64_t *base, svint64_t offsets, svint64_t data) : "STNT1D Zdata.D, Pg, [Zoffsets.D, Xbase]"
  public static unsafe void ScatterNonTemporal(Vector<long> mask, long* base, Vector<long> offsets, Vector<long> data);

    /// void svstnt1_scatter_[s64]offset[_u64](svbool_t pg, uint64_t *base, svint64_t offsets, svuint64_t data) : "STNT1D Zdata.D, Pg, [Zoffsets.D, Xbase]"
  public static unsafe void ScatterNonTemporal(Vector<ulong> mask, ulong* base, Vector<long> offsets, Vector<ulong> data);

    /// void svstnt1_scatter_[u64]offset[_f64](svbool_t pg, float64_t *base, svuint64_t offsets, svfloat64_t data) : "STNT1D Zdata.D, Pg, [Zoffsets.D, Xbase]"
  public static unsafe void ScatterNonTemporal(Vector<double> mask, double* base, Vector<ulong> offsets, Vector<double> data);

    /// void svstnt1_scatter_[u64]offset[_s64](svbool_t pg, int64_t *base, svuint64_t offsets, svint64_t data) : "STNT1D Zdata.D, Pg, [Zoffsets.D, Xbase]"
  public static unsafe void ScatterNonTemporal(Vector<long> mask, long* base, Vector<ulong> offsets, Vector<long> data);

    /// void svstnt1_scatter_[u64]offset[_u64](svbool_t pg, uint64_t *base, svuint64_t offsets, svuint64_t data) : "STNT1D Zdata.D, Pg, [Zoffsets.D, Xbase]"
  public static unsafe void ScatterNonTemporal(Vector<ulong> mask, ulong* base, Vector<ulong> offsets, Vector<ulong> data);

    /// void svstnt1_scatter_[s64]index[_f64](svbool_t pg, float64_t *base, svint64_t indices, svfloat64_t data) : "STNT1D Zdata.D, Pg, [Zoffsets.D, Xbase]"
  public static unsafe void ScatterNonTemporal(Vector<double> mask, double* base, Vector<long> indices, Vector<double> data);

    /// void svstnt1_scatter_[s64]index[_s64](svbool_t pg, int64_t *base, svint64_t indices, svint64_t data) : "STNT1D Zdata.D, Pg, [Zoffsets.D, Xbase]"
  public static unsafe void ScatterNonTemporal(Vector<long> mask, long* base, Vector<long> indices, Vector<long> data);

    /// void svstnt1_scatter_[s64]index[_u64](svbool_t pg, uint64_t *base, svint64_t indices, svuint64_t data) : "STNT1D Zdata.D, Pg, [Zoffsets.D, Xbase]"
  public static unsafe void ScatterNonTemporal(Vector<ulong> mask, ulong* base, Vector<long> indices, Vector<ulong> data);

    /// void svstnt1_scatter_[u64]index[_f64](svbool_t pg, float64_t *base, svuint64_t indices, svfloat64_t data) : "STNT1D Zdata.D, Pg, [Zoffsets.D, Xbase]"
  public static unsafe void ScatterNonTemporal(Vector<double> mask, double* base, Vector<ulong> indices, Vector<double> data);

    /// void svstnt1_scatter_[u64]index[_s64](svbool_t pg, int64_t *base, svuint64_t indices, svint64_t data) : "STNT1D Zdata.D, Pg, [Zoffsets.D, Xbase]"
  public static unsafe void ScatterNonTemporal(Vector<long> mask, long* base, Vector<ulong> indices, Vector<long> data);

    /// void svstnt1_scatter_[u64]index[_u64](svbool_t pg, uint64_t *base, svuint64_t indices, svuint64_t data) : "STNT1D Zdata.D, Pg, [Zoffsets.D, Xbase]"
  public static unsafe void ScatterNonTemporal(Vector<ulong> mask, ulong* base, Vector<ulong> indices, Vector<ulong> data);


  /// total method signatures: 55
  /// total method names:      7
}


  /// Rejected:
  ///   public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<int> mask, Vector<uint> address, long offset, Vector<int> data); // svstnt1h_scatter[_u32base]_offset[_s32]
  ///   public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<uint> mask, Vector<uint> address, long offset, Vector<uint> data); // svstnt1h_scatter[_u32base]_offset[_u32]
  ///   public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<long> mask, Vector<ulong> address, long offset, Vector<long> data); // svstnt1h_scatter[_u64base]_offset[_s64]
  ///   public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<ulong> mask, Vector<ulong> address, long offset, Vector<ulong> data); // svstnt1h_scatter[_u64base]_offset[_u64]
  ///   public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<int> mask, Vector<uint> address, long index, Vector<int> data); // svstnt1h_scatter[_u32base]_index[_s32]
  ///   public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<uint> mask, Vector<uint> address, long index, Vector<uint> data); // svstnt1h_scatter[_u32base]_index[_u32]
  ///   public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<long> mask, Vector<ulong> address, long index, Vector<long> data); // svstnt1h_scatter[_u64base]_index[_s64]
  ///   public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<ulong> mask, Vector<ulong> address, long index, Vector<ulong> data); // svstnt1h_scatter[_u64base]_index[_u64]
  ///   public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<long> mask, Vector<ulong> address, long offset, Vector<long> data); // svstnt1w_scatter[_u64base]_offset[_s64]
  ///   public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<ulong> mask, Vector<ulong> address, long offset, Vector<ulong> data); // svstnt1w_scatter[_u64base]_offset[_u64]
  ///   public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<long> mask, Vector<ulong> address, long index, Vector<long> data); // svstnt1w_scatter[_u64base]_index[_s64]
  ///   public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<ulong> mask, Vector<ulong> address, long index, Vector<ulong> data); // svstnt1w_scatter[_u64base]_index[_u64]
  ///   public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<int> mask, Vector<uint> address, long offset, Vector<int> data); // svstnt1b_scatter[_u32base]_offset[_s32]
  ///   public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<uint> mask, Vector<uint> address, long offset, Vector<uint> data); // svstnt1b_scatter[_u32base]_offset[_u32]
  ///   public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<long> mask, Vector<ulong> address, long offset, Vector<long> data); // svstnt1b_scatter[_u64base]_offset[_s64]
  ///   public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<ulong> mask, Vector<ulong> address, long offset, Vector<ulong> data); // svstnt1b_scatter[_u64base]_offset[_u64]
  ///   public static unsafe void ScatterNonTemporal(Vector<float> mask, Vector<uint> bases, long offset, Vector<float> data); // svstnt1_scatter[_u32base]_offset[_f32]
  ///   public static unsafe void ScatterNonTemporal(Vector<int> mask, Vector<uint> bases, long offset, Vector<int> data); // svstnt1_scatter[_u32base]_offset[_s32]
  ///   public static unsafe void ScatterNonTemporal(Vector<uint> mask, Vector<uint> bases, long offset, Vector<uint> data); // svstnt1_scatter[_u32base]_offset[_u32]
  ///   public static unsafe void ScatterNonTemporal(Vector<double> mask, Vector<ulong> bases, long offset, Vector<double> data); // svstnt1_scatter[_u64base]_offset[_f64]
  ///   public static unsafe void ScatterNonTemporal(Vector<long> mask, Vector<ulong> bases, long offset, Vector<long> data); // svstnt1_scatter[_u64base]_offset[_s64]
  ///   public static unsafe void ScatterNonTemporal(Vector<ulong> mask, Vector<ulong> bases, long offset, Vector<ulong> data); // svstnt1_scatter[_u64base]_offset[_u64]
  ///   public static unsafe void ScatterNonTemporal(Vector<float> mask, Vector<uint> bases, long index, Vector<float> data); // svstnt1_scatter[_u32base]_index[_f32]
  ///   public static unsafe void ScatterNonTemporal(Vector<int> mask, Vector<uint> bases, long index, Vector<int> data); // svstnt1_scatter[_u32base]_index[_s32]
  ///   public static unsafe void ScatterNonTemporal(Vector<uint> mask, Vector<uint> bases, long index, Vector<uint> data); // svstnt1_scatter[_u32base]_index[_u32]
  ///   public static unsafe void ScatterNonTemporal(Vector<double> mask, Vector<ulong> bases, long index, Vector<double> data); // svstnt1_scatter[_u64base]_index[_f64]
  ///   public static unsafe void ScatterNonTemporal(Vector<long> mask, Vector<ulong> bases, long index, Vector<long> data); // svstnt1_scatter[_u64base]_index[_s64]
  ///   public static unsafe void ScatterNonTemporal(Vector<ulong> mask, Vector<ulong> bases, long index, Vector<ulong> data); // svstnt1_scatter[_u64base]_index[_u64]
  ///   Total Rejected: 28

  /// Total ACLE covered across API:      83

