namespace System.Runtime.Intrinsics.Arm;

/// VectorT Summary
public abstract partial class Sve : AdvSimd /// Feature: FEAT_SVE  Category: stores
{

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe void Store(Vector<T> mask, T* address, Vector<T> data); // ST1W or ST1D or ST1B or ST1H

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe void Store(Vector<T> mask, T* address, (Vector<T> Value1, Vector<T> Value2) data); // ST2W or ST2D or ST2B or ST2H

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe void Store(Vector<T> mask, T* address, (Vector<T> Value1, Vector<T> Value2, Vector<T> Value3) data); // ST3W or ST3D or ST3B or ST3H

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe void Store(Vector<T> mask, T* address, (Vector<T> Value1, Vector<T> Value2, Vector<T> Value3, Vector<T> Value4) data); // ST4W or ST4D or ST4B or ST4H

  /// T: short, int, long
  public static unsafe void StoreNarrowing(Vector<T> mask, sbyte* address, Vector<T> data); // ST1B

  /// T: ushort, uint, ulong
  public static unsafe void StoreNarrowing(Vector<T> mask, byte* address, Vector<T> data); // ST1B

  /// T: int, long
  public static unsafe void StoreNarrowing(Vector<T> mask, short* address, Vector<T> data); // ST1H

  /// T: uint, ulong
  public static unsafe void StoreNarrowing(Vector<T> mask, ushort* address, Vector<T> data); // ST1H

  public static unsafe void StoreNarrowing(Vector<long> mask, int* address, Vector<long> data); // ST1W

  public static unsafe void StoreNarrowing(Vector<ulong> mask, uint* address, Vector<ulong> data); // ST1W

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe void StoreNonTemporal(Vector<T> mask, T* address, Vector<T> data); // STNT1W or STNT1D or STNT1B or STNT1H

  /// total method signatures: 11

}


/// Full API
public abstract partial class Sve : AdvSimd /// Feature: FEAT_SVE  Category: stores
{
    /// Store : Non-truncating store

    /// void svst1[_f32](svbool_t pg, float32_t *base, svfloat32_t data) : "ST1W Zdata.S, Pg, [Xarray, Xindex, LSL #2]" or "ST1W Zdata.S, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<float> mask, float* address, Vector<float> data);

    /// void svst1[_f64](svbool_t pg, float64_t *base, svfloat64_t data) : "ST1D Zdata.D, Pg, [Xarray, Xindex, LSL #3]" or "ST1D Zdata.D, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<double> mask, double* address, Vector<double> data);

    /// void svst1[_s8](svbool_t pg, int8_t *base, svint8_t data) : "ST1B Zdata.B, Pg, [Xarray, Xindex]" or "ST1B Zdata.B, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<sbyte> mask, sbyte* address, Vector<sbyte> data);

    /// void svst1[_s16](svbool_t pg, int16_t *base, svint16_t data) : "ST1H Zdata.H, Pg, [Xarray, Xindex, LSL #1]" or "ST1H Zdata.H, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<short> mask, short* address, Vector<short> data);

    /// void svst1[_s32](svbool_t pg, int32_t *base, svint32_t data) : "ST1W Zdata.S, Pg, [Xarray, Xindex, LSL #2]" or "ST1W Zdata.S, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<int> mask, int* address, Vector<int> data);

    /// void svst1[_s64](svbool_t pg, int64_t *base, svint64_t data) : "ST1D Zdata.D, Pg, [Xarray, Xindex, LSL #3]" or "ST1D Zdata.D, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<long> mask, long* address, Vector<long> data);

    /// void svst1[_u8](svbool_t pg, uint8_t *base, svuint8_t data) : "ST1B Zdata.B, Pg, [Xarray, Xindex]" or "ST1B Zdata.B, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<byte> mask, byte* address, Vector<byte> data);

    /// void svst1[_u16](svbool_t pg, uint16_t *base, svuint16_t data) : "ST1H Zdata.H, Pg, [Xarray, Xindex, LSL #1]" or "ST1H Zdata.H, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<ushort> mask, ushort* address, Vector<ushort> data);

    /// void svst1[_u32](svbool_t pg, uint32_t *base, svuint32_t data) : "ST1W Zdata.S, Pg, [Xarray, Xindex, LSL #2]" or "ST1W Zdata.S, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<uint> mask, uint* address, Vector<uint> data);

    /// void svst1[_u64](svbool_t pg, uint64_t *base, svuint64_t data) : "ST1D Zdata.D, Pg, [Xarray, Xindex, LSL #3]" or "ST1D Zdata.D, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<ulong> mask, ulong* address, Vector<ulong> data);

    /// void svst2[_f32](svbool_t pg, float32_t *base, svfloat32x2_t data) : "ST2W {Zdata0.S, Zdata1.S}, Pg, [Xarray, Xindex, LSL #2]" or "ST2W {Zdata0.S, Zdata1.S}, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<float> mask, float* address, (Vector<float> Value1, Vector<float> Value2) data);

    /// void svst2[_f64](svbool_t pg, float64_t *base, svfloat64x2_t data) : "ST2D {Zdata0.D, Zdata1.D}, Pg, [Xarray, Xindex, LSL #3]" or "ST2D {Zdata0.D, Zdata1.D}, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<double> mask, double* address, (Vector<double> Value1, Vector<double> Value2) data);

    /// void svst2[_s8](svbool_t pg, int8_t *base, svint8x2_t data) : "ST2B {Zdata0.B, Zdata1.B}, Pg, [Xarray, Xindex]" or "ST2B {Zdata0.B, Zdata1.B}, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<sbyte> mask, sbyte* address, (Vector<sbyte> Value1, Vector<sbyte> Value2) data);

    /// void svst2[_s16](svbool_t pg, int16_t *base, svint16x2_t data) : "ST2H {Zdata0.H, Zdata1.H}, Pg, [Xarray, Xindex, LSL #1]" or "ST2H {Zdata0.H, Zdata1.H}, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<short> mask, short* address, (Vector<short> Value1, Vector<short> Value2) data);

    /// void svst2[_s32](svbool_t pg, int32_t *base, svint32x2_t data) : "ST2W {Zdata0.S, Zdata1.S}, Pg, [Xarray, Xindex, LSL #2]" or "ST2W {Zdata0.S, Zdata1.S}, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<int> mask, int* address, (Vector<int> Value1, Vector<int> Value2) data);

    /// void svst2[_s64](svbool_t pg, int64_t *base, svint64x2_t data) : "ST2D {Zdata0.D, Zdata1.D}, Pg, [Xarray, Xindex, LSL #3]" or "ST2D {Zdata0.D, Zdata1.D}, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<long> mask, long* address, (Vector<long> Value1, Vector<long> Value2) data);

    /// void svst2[_u8](svbool_t pg, uint8_t *base, svuint8x2_t data) : "ST2B {Zdata0.B, Zdata1.B}, Pg, [Xarray, Xindex]" or "ST2B {Zdata0.B, Zdata1.B}, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<byte> mask, byte* address, (Vector<byte> Value1, Vector<byte> Value2) data);

    /// void svst2[_u16](svbool_t pg, uint16_t *base, svuint16x2_t data) : "ST2H {Zdata0.H, Zdata1.H}, Pg, [Xarray, Xindex, LSL #1]" or "ST2H {Zdata0.H, Zdata1.H}, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<ushort> mask, ushort* address, (Vector<ushort> Value1, Vector<ushort> Value2) data);

    /// void svst2[_u32](svbool_t pg, uint32_t *base, svuint32x2_t data) : "ST2W {Zdata0.S, Zdata1.S}, Pg, [Xarray, Xindex, LSL #2]" or "ST2W {Zdata0.S, Zdata1.S}, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<uint> mask, uint* address, (Vector<uint> Value1, Vector<uint> Value2) data);

    /// void svst2[_u64](svbool_t pg, uint64_t *base, svuint64x2_t data) : "ST2D {Zdata0.D, Zdata1.D}, Pg, [Xarray, Xindex, LSL #3]" or "ST2D {Zdata0.D, Zdata1.D}, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<ulong> mask, ulong* address, (Vector<ulong> Value1, Vector<ulong> Value2) data);

    /// void svst3[_f32](svbool_t pg, float32_t *base, svfloat32x3_t data) : "ST3W {Zdata0.S - Zdata2.S}, Pg, [Xarray, Xindex, LSL #2]" or "ST3W {Zdata0.S - Zdata2.S}, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<float> mask, float* address, (Vector<float> Value1, Vector<float> Value2, Vector<float> Value3) data);

    /// void svst3[_f64](svbool_t pg, float64_t *base, svfloat64x3_t data) : "ST3D {Zdata0.D - Zdata2.D}, Pg, [Xarray, Xindex, LSL #3]" or "ST3D {Zdata0.D - Zdata2.D}, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<double> mask, double* address, (Vector<double> Value1, Vector<double> Value2, Vector<double> Value3) data);

    /// void svst3[_s8](svbool_t pg, int8_t *base, svint8x3_t data) : "ST3B {Zdata0.B - Zdata2.B}, Pg, [Xarray, Xindex]" or "ST3B {Zdata0.B - Zdata2.B}, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<sbyte> mask, sbyte* address, (Vector<sbyte> Value1, Vector<sbyte> Value2, Vector<sbyte> Value3) data);

    /// void svst3[_s16](svbool_t pg, int16_t *base, svint16x3_t data) : "ST3H {Zdata0.H - Zdata2.H}, Pg, [Xarray, Xindex, LSL #1]" or "ST3H {Zdata0.H - Zdata2.H}, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<short> mask, short* address, (Vector<short> Value1, Vector<short> Value2, Vector<short> Value3) data);

    /// void svst3[_s32](svbool_t pg, int32_t *base, svint32x3_t data) : "ST3W {Zdata0.S - Zdata2.S}, Pg, [Xarray, Xindex, LSL #2]" or "ST3W {Zdata0.S - Zdata2.S}, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<int> mask, int* address, (Vector<int> Value1, Vector<int> Value2, Vector<int> Value3) data);

    /// void svst3[_s64](svbool_t pg, int64_t *base, svint64x3_t data) : "ST3D {Zdata0.D - Zdata2.D}, Pg, [Xarray, Xindex, LSL #3]" or "ST3D {Zdata0.D - Zdata2.D}, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<long> mask, long* address, (Vector<long> Value1, Vector<long> Value2, Vector<long> Value3) data);

    /// void svst3[_u8](svbool_t pg, uint8_t *base, svuint8x3_t data) : "ST3B {Zdata0.B - Zdata2.B}, Pg, [Xarray, Xindex]" or "ST3B {Zdata0.B - Zdata2.B}, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<byte> mask, byte* address, (Vector<byte> Value1, Vector<byte> Value2, Vector<byte> Value3) data);

    /// void svst3[_u16](svbool_t pg, uint16_t *base, svuint16x3_t data) : "ST3H {Zdata0.H - Zdata2.H}, Pg, [Xarray, Xindex, LSL #1]" or "ST3H {Zdata0.H - Zdata2.H}, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<ushort> mask, ushort* address, (Vector<ushort> Value1, Vector<ushort> Value2, Vector<ushort> Value3) data);

    /// void svst3[_u32](svbool_t pg, uint32_t *base, svuint32x3_t data) : "ST3W {Zdata0.S - Zdata2.S}, Pg, [Xarray, Xindex, LSL #2]" or "ST3W {Zdata0.S - Zdata2.S}, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<uint> mask, uint* address, (Vector<uint> Value1, Vector<uint> Value2, Vector<uint> Value3) data);

    /// void svst3[_u64](svbool_t pg, uint64_t *base, svuint64x3_t data) : "ST3D {Zdata0.D - Zdata2.D}, Pg, [Xarray, Xindex, LSL #3]" or "ST3D {Zdata0.D - Zdata2.D}, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<ulong> mask, ulong* address, (Vector<ulong> Value1, Vector<ulong> Value2, Vector<ulong> Value3) data);

    /// void svst4[_f32](svbool_t pg, float32_t *base, svfloat32x4_t data) : "ST4W {Zdata0.S - Zdata3.S}, Pg, [Xarray, Xindex, LSL #2]" or "ST4W {Zdata0.S - Zdata3.S}, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<float> mask, float* address, (Vector<float> Value1, Vector<float> Value2, Vector<float> Value3, Vector<float> Value4) data);

    /// void svst4[_f64](svbool_t pg, float64_t *base, svfloat64x4_t data) : "ST4D {Zdata0.D - Zdata3.D}, Pg, [Xarray, Xindex, LSL #3]" or "ST4D {Zdata0.D - Zdata3.D}, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<double> mask, double* address, (Vector<double> Value1, Vector<double> Value2, Vector<double> Value3, Vector<double> Value4) data);

    /// void svst4[_s8](svbool_t pg, int8_t *base, svint8x4_t data) : "ST4B {Zdata0.B - Zdata3.B}, Pg, [Xarray, Xindex]" or "ST4B {Zdata0.B - Zdata3.B}, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<sbyte> mask, sbyte* address, (Vector<sbyte> Value1, Vector<sbyte> Value2, Vector<sbyte> Value3, Vector<sbyte> Value4) data);

    /// void svst4[_s16](svbool_t pg, int16_t *base, svint16x4_t data) : "ST4H {Zdata0.H - Zdata3.H}, Pg, [Xarray, Xindex, LSL #1]" or "ST4H {Zdata0.H - Zdata3.H}, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<short> mask, short* address, (Vector<short> Value1, Vector<short> Value2, Vector<short> Value3, Vector<short> Value4) data);

    /// void svst4[_s32](svbool_t pg, int32_t *base, svint32x4_t data) : "ST4W {Zdata0.S - Zdata3.S}, Pg, [Xarray, Xindex, LSL #2]" or "ST4W {Zdata0.S - Zdata3.S}, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<int> mask, int* address, (Vector<int> Value1, Vector<int> Value2, Vector<int> Value3, Vector<int> Value4) data);

    /// void svst4[_s64](svbool_t pg, int64_t *base, svint64x4_t data) : "ST4D {Zdata0.D - Zdata3.D}, Pg, [Xarray, Xindex, LSL #3]" or "ST4D {Zdata0.D - Zdata3.D}, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<long> mask, long* address, (Vector<long> Value1, Vector<long> Value2, Vector<long> Value3, Vector<long> Value4) data);

    /// void svst4[_u8](svbool_t pg, uint8_t *base, svuint8x4_t data) : "ST4B {Zdata0.B - Zdata3.B}, Pg, [Xarray, Xindex]" or "ST4B {Zdata0.B - Zdata3.B}, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<byte> mask, byte* address, (Vector<byte> Value1, Vector<byte> Value2, Vector<byte> Value3, Vector<byte> Value4) data);

    /// void svst4[_u16](svbool_t pg, uint16_t *base, svuint16x4_t data) : "ST4H {Zdata0.H - Zdata3.H}, Pg, [Xarray, Xindex, LSL #1]" or "ST4H {Zdata0.H - Zdata3.H}, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<ushort> mask, ushort* address, (Vector<ushort> Value1, Vector<ushort> Value2, Vector<ushort> Value3, Vector<ushort> Value4) data);

    /// void svst4[_u32](svbool_t pg, uint32_t *base, svuint32x4_t data) : "ST4W {Zdata0.S - Zdata3.S}, Pg, [Xarray, Xindex, LSL #2]" or "ST4W {Zdata0.S - Zdata3.S}, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<uint> mask, uint* address, (Vector<uint> Value1, Vector<uint> Value2, Vector<uint> Value3, Vector<uint> Value4) data);

    /// void svst4[_u64](svbool_t pg, uint64_t *base, svuint64x4_t data) : "ST4D {Zdata0.D - Zdata3.D}, Pg, [Xarray, Xindex, LSL #3]" or "ST4D {Zdata0.D - Zdata3.D}, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<ulong> mask, ulong* address, (Vector<ulong> Value1, Vector<ulong> Value2, Vector<ulong> Value3, Vector<ulong> Value4) data);


    /// StoreNarrowing : Truncate to 8 bits and store

    /// void svst1b[_s16](svbool_t pg, int8_t *base, svint16_t data) : "ST1B Zdata.H, Pg, [Xarray, Xindex]" or "ST1B Zdata.H, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void StoreNarrowing(Vector<short> mask, sbyte* address, Vector<short> data);

    /// void svst1b[_s32](svbool_t pg, int8_t *base, svint32_t data) : "ST1B Zdata.S, Pg, [Xarray, Xindex]" or "ST1B Zdata.S, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void StoreNarrowing(Vector<int> mask, sbyte* address, Vector<int> data);

    /// void svst1b[_s64](svbool_t pg, int8_t *base, svint64_t data) : "ST1B Zdata.D, Pg, [Xarray, Xindex]" or "ST1B Zdata.D, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void StoreNarrowing(Vector<long> mask, sbyte* address, Vector<long> data);

    /// void svst1b[_u16](svbool_t pg, uint8_t *base, svuint16_t data) : "ST1B Zdata.H, Pg, [Xarray, Xindex]" or "ST1B Zdata.H, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void StoreNarrowing(Vector<ushort> mask, byte* address, Vector<ushort> data);

    /// void svst1b[_u32](svbool_t pg, uint8_t *base, svuint32_t data) : "ST1B Zdata.S, Pg, [Xarray, Xindex]" or "ST1B Zdata.S, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void StoreNarrowing(Vector<uint> mask, byte* address, Vector<uint> data);

    /// void svst1b[_u64](svbool_t pg, uint8_t *base, svuint64_t data) : "ST1B Zdata.D, Pg, [Xarray, Xindex]" or "ST1B Zdata.D, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void StoreNarrowing(Vector<ulong> mask, byte* address, Vector<ulong> data);

    /// void svst1h[_s32](svbool_t pg, int16_t *base, svint32_t data) : "ST1H Zdata.S, Pg, [Xarray, Xindex, LSL #1]" or "ST1H Zdata.S, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void StoreNarrowing(Vector<int> mask, short* address, Vector<int> data);

    /// void svst1h[_s64](svbool_t pg, int16_t *base, svint64_t data) : "ST1H Zdata.D, Pg, [Xarray, Xindex, LSL #1]" or "ST1H Zdata.D, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void StoreNarrowing(Vector<long> mask, short* address, Vector<long> data);

    /// void svst1h[_u32](svbool_t pg, uint16_t *base, svuint32_t data) : "ST1H Zdata.S, Pg, [Xarray, Xindex, LSL #1]" or "ST1H Zdata.S, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void StoreNarrowing(Vector<uint> mask, ushort* address, Vector<uint> data);

    /// void svst1h[_u64](svbool_t pg, uint16_t *base, svuint64_t data) : "ST1H Zdata.D, Pg, [Xarray, Xindex, LSL #1]" or "ST1H Zdata.D, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void StoreNarrowing(Vector<ulong> mask, ushort* address, Vector<ulong> data);

    /// void svst1w[_s64](svbool_t pg, int32_t *base, svint64_t data) : "ST1W Zdata.D, Pg, [Xarray, Xindex, LSL #2]" or "ST1W Zdata.D, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void StoreNarrowing(Vector<long> mask, int* address, Vector<long> data);

    /// void svst1w[_u64](svbool_t pg, uint32_t *base, svuint64_t data) : "ST1W Zdata.D, Pg, [Xarray, Xindex, LSL #2]" or "ST1W Zdata.D, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void StoreNarrowing(Vector<ulong> mask, uint* address, Vector<ulong> data);


    /// StoreNonTemporal : Non-truncating store, non-temporal

    /// void svstnt1[_f32](svbool_t pg, float32_t *base, svfloat32_t data) : "STNT1W Zdata.S, Pg, [Xarray, Xindex, LSL #2]" or "STNT1W Zdata.S, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void StoreNonTemporal(Vector<float> mask, float* address, Vector<float> data);

    /// void svstnt1[_f64](svbool_t pg, float64_t *base, svfloat64_t data) : "STNT1D Zdata.D, Pg, [Xarray, Xindex, LSL #3]" or "STNT1D Zdata.D, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void StoreNonTemporal(Vector<double> mask, double* address, Vector<double> data);

    /// void svstnt1[_s8](svbool_t pg, int8_t *base, svint8_t data) : "STNT1B Zdata.B, Pg, [Xarray, Xindex]" or "STNT1B Zdata.B, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void StoreNonTemporal(Vector<sbyte> mask, sbyte* address, Vector<sbyte> data);

    /// void svstnt1[_s16](svbool_t pg, int16_t *base, svint16_t data) : "STNT1H Zdata.H, Pg, [Xarray, Xindex, LSL #1]" or "STNT1H Zdata.H, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void StoreNonTemporal(Vector<short> mask, short* address, Vector<short> data);

    /// void svstnt1[_s32](svbool_t pg, int32_t *base, svint32_t data) : "STNT1W Zdata.S, Pg, [Xarray, Xindex, LSL #2]" or "STNT1W Zdata.S, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void StoreNonTemporal(Vector<int> mask, int* address, Vector<int> data);

    /// void svstnt1[_s64](svbool_t pg, int64_t *base, svint64_t data) : "STNT1D Zdata.D, Pg, [Xarray, Xindex, LSL #3]" or "STNT1D Zdata.D, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void StoreNonTemporal(Vector<long> mask, long* address, Vector<long> data);

    /// void svstnt1[_u8](svbool_t pg, uint8_t *base, svuint8_t data) : "STNT1B Zdata.B, Pg, [Xarray, Xindex]" or "STNT1B Zdata.B, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void StoreNonTemporal(Vector<byte> mask, byte* address, Vector<byte> data);

    /// void svstnt1[_u16](svbool_t pg, uint16_t *base, svuint16_t data) : "STNT1H Zdata.H, Pg, [Xarray, Xindex, LSL #1]" or "STNT1H Zdata.H, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void StoreNonTemporal(Vector<ushort> mask, ushort* address, Vector<ushort> data);

    /// void svstnt1[_u32](svbool_t pg, uint32_t *base, svuint32_t data) : "STNT1W Zdata.S, Pg, [Xarray, Xindex, LSL #2]" or "STNT1W Zdata.S, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void StoreNonTemporal(Vector<uint> mask, uint* address, Vector<uint> data);

    /// void svstnt1[_u64](svbool_t pg, uint64_t *base, svuint64_t data) : "STNT1D Zdata.D, Pg, [Xarray, Xindex, LSL #3]" or "STNT1D Zdata.D, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void StoreNonTemporal(Vector<ulong> mask, ulong* address, Vector<ulong> data);


  /// total method signatures: 62
  /// total method names:      3
}


  /// Rejected:
  ///   public static unsafe void Store(Vector<float> mask, float* base, long vnum, Vector<float> data); // svst1_vnum[_f32]
  ///   public static unsafe void Store(Vector<double> mask, double* base, long vnum, Vector<double> data); // svst1_vnum[_f64]
  ///   public static unsafe void Store(Vector<sbyte> mask, sbyte* base, long vnum, Vector<sbyte> data); // svst1_vnum[_s8]
  ///   public static unsafe void Store(Vector<short> mask, short* base, long vnum, Vector<short> data); // svst1_vnum[_s16]
  ///   public static unsafe void Store(Vector<int> mask, int* base, long vnum, Vector<int> data); // svst1_vnum[_s32]
  ///   public static unsafe void Store(Vector<long> mask, long* base, long vnum, Vector<long> data); // svst1_vnum[_s64]
  ///   public static unsafe void Store(Vector<byte> mask, byte* base, long vnum, Vector<byte> data); // svst1_vnum[_u8]
  ///   public static unsafe void Store(Vector<ushort> mask, ushort* base, long vnum, Vector<ushort> data); // svst1_vnum[_u16]
  ///   public static unsafe void Store(Vector<uint> mask, uint* base, long vnum, Vector<uint> data); // svst1_vnum[_u32]
  ///   public static unsafe void Store(Vector<ulong> mask, ulong* base, long vnum, Vector<ulong> data); // svst1_vnum[_u64]
  ///   public static unsafe void Store(Vector<float> mask, float* base, long vnum, (Vector<float> data1, Vector<float> data2)); // svst2_vnum[_f32]
  ///   public static unsafe void Store(Vector<double> mask, double* base, long vnum, (Vector<double> data1, Vector<double> data2)); // svst2_vnum[_f64]
  ///   public static unsafe void Store(Vector<sbyte> mask, sbyte* base, long vnum, (Vector<sbyte> data1, Vector<sbyte> data2)); // svst2_vnum[_s8]
  ///   public static unsafe void Store(Vector<short> mask, short* base, long vnum, (Vector<short> data1, Vector<short> data2)); // svst2_vnum[_s16]
  ///   public static unsafe void Store(Vector<int> mask, int* base, long vnum, (Vector<int> data1, Vector<int> data2)); // svst2_vnum[_s32]
  ///   public static unsafe void Store(Vector<long> mask, long* base, long vnum, (Vector<long> data1, Vector<long> data2)); // svst2_vnum[_s64]
  ///   public static unsafe void Store(Vector<byte> mask, byte* base, long vnum, (Vector<byte> data1, Vector<byte> data2)); // svst2_vnum[_u8]
  ///   public static unsafe void Store(Vector<ushort> mask, ushort* base, long vnum, (Vector<ushort> data1, Vector<ushort> data2)); // svst2_vnum[_u16]
  ///   public static unsafe void Store(Vector<uint> mask, uint* base, long vnum, (Vector<uint> data1, Vector<uint> data2)); // svst2_vnum[_u32]
  ///   public static unsafe void Store(Vector<ulong> mask, ulong* base, long vnum, (Vector<ulong> data1, Vector<ulong> data2)); // svst2_vnum[_u64]
  ///   public static unsafe void Store(Vector<float> mask, float* base, long vnum, (Vector<float> data1, Vector<float> data2, Vector<float> data3)); // svst3_vnum[_f32]
  ///   public static unsafe void Store(Vector<double> mask, double* base, long vnum, (Vector<double> data1, Vector<double> data2, Vector<double> data3)); // svst3_vnum[_f64]
  ///   public static unsafe void Store(Vector<sbyte> mask, sbyte* base, long vnum, (Vector<sbyte> data1, Vector<sbyte> data2, Vector<sbyte> data3)); // svst3_vnum[_s8]
  ///   public static unsafe void Store(Vector<short> mask, short* base, long vnum, (Vector<short> data1, Vector<short> data2, Vector<short> data3)); // svst3_vnum[_s16]
  ///   public static unsafe void Store(Vector<int> mask, int* base, long vnum, (Vector<int> data1, Vector<int> data2, Vector<int> data3)); // svst3_vnum[_s32]
  ///   public static unsafe void Store(Vector<long> mask, long* base, long vnum, (Vector<long> data1, Vector<long> data2, Vector<long> data3)); // svst3_vnum[_s64]
  ///   public static unsafe void Store(Vector<byte> mask, byte* base, long vnum, (Vector<byte> data1, Vector<byte> data2, Vector<byte> data3)); // svst3_vnum[_u8]
  ///   public static unsafe void Store(Vector<ushort> mask, ushort* base, long vnum, (Vector<ushort> data1, Vector<ushort> data2, Vector<ushort> data3)); // svst3_vnum[_u16]
  ///   public static unsafe void Store(Vector<uint> mask, uint* base, long vnum, (Vector<uint> data1, Vector<uint> data2, Vector<uint> data3)); // svst3_vnum[_u32]
  ///   public static unsafe void Store(Vector<ulong> mask, ulong* base, long vnum, (Vector<ulong> data1, Vector<ulong> data2, Vector<ulong> data3)); // svst3_vnum[_u64]
  ///   public static unsafe void Store(Vector<float> mask, float* base, long vnum, (Vector<float> data1, Vector<float> data2, Vector<float> data3, Vector<float> data4)); // svst4_vnum[_f32]
  ///   public static unsafe void Store(Vector<double> mask, double* base, long vnum, (Vector<double> data1, Vector<double> data2, Vector<double> data3, Vector<double> data4)); // svst4_vnum[_f64]
  ///   public static unsafe void Store(Vector<sbyte> mask, sbyte* base, long vnum, (Vector<sbyte> data1, Vector<sbyte> data2, Vector<sbyte> data3, Vector<sbyte> data4)); // svst4_vnum[_s8]
  ///   public static unsafe void Store(Vector<short> mask, short* base, long vnum, (Vector<short> data1, Vector<short> data2, Vector<short> data3, Vector<short> data4)); // svst4_vnum[_s16]
  ///   public static unsafe void Store(Vector<int> mask, int* base, long vnum, (Vector<int> data1, Vector<int> data2, Vector<int> data3, Vector<int> data4)); // svst4_vnum[_s32]
  ///   public static unsafe void Store(Vector<long> mask, long* base, long vnum, (Vector<long> data1, Vector<long> data2, Vector<long> data3, Vector<long> data4)); // svst4_vnum[_s64]
  ///   public static unsafe void Store(Vector<byte> mask, byte* base, long vnum, (Vector<byte> data1, Vector<byte> data2, Vector<byte> data3, Vector<byte> data4)); // svst4_vnum[_u8]
  ///   public static unsafe void Store(Vector<ushort> mask, ushort* base, long vnum, (Vector<ushort> data1, Vector<ushort> data2, Vector<ushort> data3, Vector<ushort> data4)); // svst4_vnum[_u16]
  ///   public static unsafe void Store(Vector<uint> mask, uint* base, long vnum, (Vector<uint> data1, Vector<uint> data2, Vector<uint> data3, Vector<uint> data4)); // svst4_vnum[_u32]
  ///   public static unsafe void Store(Vector<ulong> mask, ulong* base, long vnum, (Vector<ulong> data1, Vector<ulong> data2, Vector<ulong> data3, Vector<ulong> data4)); // svst4_vnum[_u64]
  ///   public static unsafe void StoreNarrowing(Vector<short> mask, sbyte* base, long vnum, Vector<short> data); // svst1b_vnum[_s16]
  ///   public static unsafe void StoreNarrowing(Vector<int> mask, sbyte* base, long vnum, Vector<int> data); // svst1b_vnum[_s32]
  ///   public static unsafe void StoreNarrowing(Vector<long> mask, sbyte* base, long vnum, Vector<long> data); // svst1b_vnum[_s64]
  ///   public static unsafe void StoreNarrowing(Vector<ushort> mask, byte* base, long vnum, Vector<ushort> data); // svst1b_vnum[_u16]
  ///   public static unsafe void StoreNarrowing(Vector<uint> mask, byte* base, long vnum, Vector<uint> data); // svst1b_vnum[_u32]
  ///   public static unsafe void StoreNarrowing(Vector<ulong> mask, byte* base, long vnum, Vector<ulong> data); // svst1b_vnum[_u64]
  ///   public static unsafe void StoreNarrowing(Vector<int> mask, short* base, long vnum, Vector<int> data); // svst1h_vnum[_s32]
  ///   public static unsafe void StoreNarrowing(Vector<long> mask, short* base, long vnum, Vector<long> data); // svst1h_vnum[_s64]
  ///   public static unsafe void StoreNarrowing(Vector<uint> mask, ushort* base, long vnum, Vector<uint> data); // svst1h_vnum[_u32]
  ///   public static unsafe void StoreNarrowing(Vector<ulong> mask, ushort* base, long vnum, Vector<ulong> data); // svst1h_vnum[_u64]
  ///   public static unsafe void StoreNarrowing(Vector<long> mask, int* base, long vnum, Vector<long> data); // svst1w_vnum[_s64]
  ///   public static unsafe void StoreNarrowing(Vector<ulong> mask, uint* base, long vnum, Vector<ulong> data); // svst1w_vnum[_u64]
  ///   public static unsafe void StoreNonTemporal(Vector<float> mask, float* base, long vnum, Vector<float> data); // svstnt1_vnum[_f32]
  ///   public static unsafe void StoreNonTemporal(Vector<double> mask, double* base, long vnum, Vector<double> data); // svstnt1_vnum[_f64]
  ///   public static unsafe void StoreNonTemporal(Vector<sbyte> mask, sbyte* base, long vnum, Vector<sbyte> data); // svstnt1_vnum[_s8]
  ///   public static unsafe void StoreNonTemporal(Vector<short> mask, short* base, long vnum, Vector<short> data); // svstnt1_vnum[_s16]
  ///   public static unsafe void StoreNonTemporal(Vector<int> mask, int* base, long vnum, Vector<int> data); // svstnt1_vnum[_s32]
  ///   public static unsafe void StoreNonTemporal(Vector<long> mask, long* base, long vnum, Vector<long> data); // svstnt1_vnum[_s64]
  ///   public static unsafe void StoreNonTemporal(Vector<byte> mask, byte* base, long vnum, Vector<byte> data); // svstnt1_vnum[_u8]
  ///   public static unsafe void StoreNonTemporal(Vector<ushort> mask, ushort* base, long vnum, Vector<ushort> data); // svstnt1_vnum[_u16]
  ///   public static unsafe void StoreNonTemporal(Vector<uint> mask, uint* base, long vnum, Vector<uint> data); // svstnt1_vnum[_u32]
  ///   public static unsafe void StoreNonTemporal(Vector<ulong> mask, ulong* base, long vnum, Vector<ulong> data); // svstnt1_vnum[_u64]
  ///   Total Rejected: 62

  /// Total ACLE covered across API:      124

