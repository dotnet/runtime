namespace System.Runtime.Intrinsics.Arm;

/// VectorT Summary
public abstract partial class Sve2 : AdvSimd /// Feature: FEAT_SVE2  Category: bitmanipulate
{

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> InterleavingXorLowerUpper(Vector<T> odd, Vector<T> left, Vector<T> right); // EORBT // MOVPRFX

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> InterleavingXorUpperLower(Vector<T> even, Vector<T> left, Vector<T> right); // EORTB // MOVPRFX

  /// T: [short, sbyte], [int, short], [long, int], [ushort, byte], [uint, ushort], [ulong, uint]
  public static unsafe Vector<T> MoveWideningLower(Vector<T2> value); // SSHLLB or USHLLB

  /// T: [short, sbyte], [int, short], [long, int], [ushort, byte], [uint, ushort], [ulong, uint]
  public static unsafe Vector<T> MoveWideningUpper(Vector<T2> value); // SSHLLT or USHLLT

  /// T: [float, uint], [double, ulong], [sbyte, byte], [short, ushort], [int, uint], [long, ulong]
  public static unsafe Vector<T> VectorTableLookup((Vector<T> data1, Vector<T> data2), Vector<T2> indices); // TBL

  /// T: byte, ushort, uint, ulong
  public static unsafe Vector<T> VectorTableLookup((Vector<T> data1, Vector<T> data2), Vector<T> indices); // TBL

  /// T: [float, uint], [double, ulong], [sbyte, byte], [short, ushort], [int, uint], [long, ulong]
  public static unsafe Vector<T> VectorTableLookupExtension(Vector<T> fallback, Vector<T> data, Vector<T2> indices); // TBX

  /// T: byte, ushort, uint, ulong
  public static unsafe Vector<T> VectorTableLookupExtension(Vector<T> fallback, Vector<T> data, Vector<T> indices); // TBX

  /// total method signatures: 8


  /// Optional Entries:

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> InterleavingXorLowerUpper(Vector<T> odd, Vector<T> left, T right); // EORBT // MOVPRFX

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> InterleavingXorUpperLower(Vector<T> even, Vector<T> left, T right); // EORTB // MOVPRFX

  /// total optional method signatures: 2

}


/// Full API
public abstract partial class Sve2 : AdvSimd /// Feature: FEAT_SVE2  Category: bitmanipulate
{
    /// InterleavingXorLowerUpper : Interleaving exclusive OR (bottom, top)

    /// svint8_t sveorbt[_s8](svint8_t odd, svint8_t op1, svint8_t op2) : "EORBT Ztied.B, Zop1.B, Zop2.B" or "MOVPRFX Zresult, Zodd; EORBT Zresult.B, Zop1.B, Zop2.B"
  public static unsafe Vector<sbyte> InterleavingXorLowerUpper(Vector<sbyte> odd, Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t sveorbt[_s16](svint16_t odd, svint16_t op1, svint16_t op2) : "EORBT Ztied.H, Zop1.H, Zop2.H" or "MOVPRFX Zresult, Zodd; EORBT Zresult.H, Zop1.H, Zop2.H"
  public static unsafe Vector<short> InterleavingXorLowerUpper(Vector<short> odd, Vector<short> left, Vector<short> right);

    /// svint32_t sveorbt[_s32](svint32_t odd, svint32_t op1, svint32_t op2) : "EORBT Ztied.S, Zop1.S, Zop2.S" or "MOVPRFX Zresult, Zodd; EORBT Zresult.S, Zop1.S, Zop2.S"
  public static unsafe Vector<int> InterleavingXorLowerUpper(Vector<int> odd, Vector<int> left, Vector<int> right);

    /// svint64_t sveorbt[_s64](svint64_t odd, svint64_t op1, svint64_t op2) : "EORBT Ztied.D, Zop1.D, Zop2.D" or "MOVPRFX Zresult, Zodd; EORBT Zresult.D, Zop1.D, Zop2.D"
  public static unsafe Vector<long> InterleavingXorLowerUpper(Vector<long> odd, Vector<long> left, Vector<long> right);

    /// svuint8_t sveorbt[_u8](svuint8_t odd, svuint8_t op1, svuint8_t op2) : "EORBT Ztied.B, Zop1.B, Zop2.B" or "MOVPRFX Zresult, Zodd; EORBT Zresult.B, Zop1.B, Zop2.B"
  public static unsafe Vector<byte> InterleavingXorLowerUpper(Vector<byte> odd, Vector<byte> left, Vector<byte> right);

    /// svuint16_t sveorbt[_u16](svuint16_t odd, svuint16_t op1, svuint16_t op2) : "EORBT Ztied.H, Zop1.H, Zop2.H" or "MOVPRFX Zresult, Zodd; EORBT Zresult.H, Zop1.H, Zop2.H"
  public static unsafe Vector<ushort> InterleavingXorLowerUpper(Vector<ushort> odd, Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t sveorbt[_u32](svuint32_t odd, svuint32_t op1, svuint32_t op2) : "EORBT Ztied.S, Zop1.S, Zop2.S" or "MOVPRFX Zresult, Zodd; EORBT Zresult.S, Zop1.S, Zop2.S"
  public static unsafe Vector<uint> InterleavingXorLowerUpper(Vector<uint> odd, Vector<uint> left, Vector<uint> right);

    /// svuint64_t sveorbt[_u64](svuint64_t odd, svuint64_t op1, svuint64_t op2) : "EORBT Ztied.D, Zop1.D, Zop2.D" or "MOVPRFX Zresult, Zodd; EORBT Zresult.D, Zop1.D, Zop2.D"
  public static unsafe Vector<ulong> InterleavingXorLowerUpper(Vector<ulong> odd, Vector<ulong> left, Vector<ulong> right);


    /// InterleavingXorUpperLower : Interleaving exclusive OR (top, bottom)

    /// svint8_t sveortb[_s8](svint8_t even, svint8_t op1, svint8_t op2) : "EORTB Ztied.B, Zop1.B, Zop2.B" or "MOVPRFX Zresult, Zeven; EORTB Zresult.B, Zop1.B, Zop2.B"
  public static unsafe Vector<sbyte> InterleavingXorUpperLower(Vector<sbyte> even, Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t sveortb[_s16](svint16_t even, svint16_t op1, svint16_t op2) : "EORTB Ztied.H, Zop1.H, Zop2.H" or "MOVPRFX Zresult, Zeven; EORTB Zresult.H, Zop1.H, Zop2.H"
  public static unsafe Vector<short> InterleavingXorUpperLower(Vector<short> even, Vector<short> left, Vector<short> right);

    /// svint32_t sveortb[_s32](svint32_t even, svint32_t op1, svint32_t op2) : "EORTB Ztied.S, Zop1.S, Zop2.S" or "MOVPRFX Zresult, Zeven; EORTB Zresult.S, Zop1.S, Zop2.S"
  public static unsafe Vector<int> InterleavingXorUpperLower(Vector<int> even, Vector<int> left, Vector<int> right);

    /// svint64_t sveortb[_s64](svint64_t even, svint64_t op1, svint64_t op2) : "EORTB Ztied.D, Zop1.D, Zop2.D" or "MOVPRFX Zresult, Zeven; EORTB Zresult.D, Zop1.D, Zop2.D"
  public static unsafe Vector<long> InterleavingXorUpperLower(Vector<long> even, Vector<long> left, Vector<long> right);

    /// svuint8_t sveortb[_u8](svuint8_t even, svuint8_t op1, svuint8_t op2) : "EORTB Ztied.B, Zop1.B, Zop2.B" or "MOVPRFX Zresult, Zeven; EORTB Zresult.B, Zop1.B, Zop2.B"
  public static unsafe Vector<byte> InterleavingXorUpperLower(Vector<byte> even, Vector<byte> left, Vector<byte> right);

    /// svuint16_t sveortb[_u16](svuint16_t even, svuint16_t op1, svuint16_t op2) : "EORTB Ztied.H, Zop1.H, Zop2.H" or "MOVPRFX Zresult, Zeven; EORTB Zresult.H, Zop1.H, Zop2.H"
  public static unsafe Vector<ushort> InterleavingXorUpperLower(Vector<ushort> even, Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t sveortb[_u32](svuint32_t even, svuint32_t op1, svuint32_t op2) : "EORTB Ztied.S, Zop1.S, Zop2.S" or "MOVPRFX Zresult, Zeven; EORTB Zresult.S, Zop1.S, Zop2.S"
  public static unsafe Vector<uint> InterleavingXorUpperLower(Vector<uint> even, Vector<uint> left, Vector<uint> right);

    /// svuint64_t sveortb[_u64](svuint64_t even, svuint64_t op1, svuint64_t op2) : "EORTB Ztied.D, Zop1.D, Zop2.D" or "MOVPRFX Zresult, Zeven; EORTB Zresult.D, Zop1.D, Zop2.D"
  public static unsafe Vector<ulong> InterleavingXorUpperLower(Vector<ulong> even, Vector<ulong> left, Vector<ulong> right);


    /// MoveWideningLower : Move long (bottom)

    /// svint16_t svmovlb[_s16](svint8_t op) : "SSHLLB Zresult.H, Zop.B, #0"
  public static unsafe Vector<short> MoveWideningLower(Vector<sbyte> value);

    /// svint32_t svmovlb[_s32](svint16_t op) : "SSHLLB Zresult.S, Zop.H, #0"
  public static unsafe Vector<int> MoveWideningLower(Vector<short> value);

    /// svint64_t svmovlb[_s64](svint32_t op) : "SSHLLB Zresult.D, Zop.S, #0"
  public static unsafe Vector<long> MoveWideningLower(Vector<int> value);

    /// svuint16_t svmovlb[_u16](svuint8_t op) : "USHLLB Zresult.H, Zop.B, #0"
  public static unsafe Vector<ushort> MoveWideningLower(Vector<byte> value);

    /// svuint32_t svmovlb[_u32](svuint16_t op) : "USHLLB Zresult.S, Zop.H, #0"
  public static unsafe Vector<uint> MoveWideningLower(Vector<ushort> value);

    /// svuint64_t svmovlb[_u64](svuint32_t op) : "USHLLB Zresult.D, Zop.S, #0"
  public static unsafe Vector<ulong> MoveWideningLower(Vector<uint> value);


    /// MoveWideningUpper : Move long (top)

    /// svint16_t svmovlt[_s16](svint8_t op) : "SSHLLT Zresult.H, Zop.B, #0"
  public static unsafe Vector<short> MoveWideningUpper(Vector<sbyte> value);

    /// svint32_t svmovlt[_s32](svint16_t op) : "SSHLLT Zresult.S, Zop.H, #0"
  public static unsafe Vector<int> MoveWideningUpper(Vector<short> value);

    /// svint64_t svmovlt[_s64](svint32_t op) : "SSHLLT Zresult.D, Zop.S, #0"
  public static unsafe Vector<long> MoveWideningUpper(Vector<int> value);

    /// svuint16_t svmovlt[_u16](svuint8_t op) : "USHLLT Zresult.H, Zop.B, #0"
  public static unsafe Vector<ushort> MoveWideningUpper(Vector<byte> value);

    /// svuint32_t svmovlt[_u32](svuint16_t op) : "USHLLT Zresult.S, Zop.H, #0"
  public static unsafe Vector<uint> MoveWideningUpper(Vector<ushort> value);

    /// svuint64_t svmovlt[_u64](svuint32_t op) : "USHLLT Zresult.D, Zop.S, #0"
  public static unsafe Vector<ulong> MoveWideningUpper(Vector<uint> value);


    /// VectorTableLookup : Table lookup in two-vector table

    /// svfloat32_t svtbl2[_f32](svfloat32x2_t data, svuint32_t indices) : "TBL Zresult.S, {Zdata0.S, Zdata1.S}, Zindices.S"
  public static unsafe Vector<float> VectorTableLookup((Vector<float> data1, Vector<float> data2), Vector<uint> indices);

    /// svfloat64_t svtbl2[_f64](svfloat64x2_t data, svuint64_t indices) : "TBL Zresult.D, {Zdata0.D, Zdata1.D}, Zindices.D"
  public static unsafe Vector<double> VectorTableLookup((Vector<double> data1, Vector<double> data2), Vector<ulong> indices);

    /// svint8_t svtbl2[_s8](svint8x2_t data, svuint8_t indices) : "TBL Zresult.B, {Zdata0.B, Zdata1.B}, Zindices.B"
  public static unsafe Vector<sbyte> VectorTableLookup((Vector<sbyte> data1, Vector<sbyte> data2), Vector<byte> indices);

    /// svint16_t svtbl2[_s16](svint16x2_t data, svuint16_t indices) : "TBL Zresult.H, {Zdata0.H, Zdata1.H}, Zindices.H"
  public static unsafe Vector<short> VectorTableLookup((Vector<short> data1, Vector<short> data2), Vector<ushort> indices);

    /// svint32_t svtbl2[_s32](svint32x2_t data, svuint32_t indices) : "TBL Zresult.S, {Zdata0.S, Zdata1.S}, Zindices.S"
  public static unsafe Vector<int> VectorTableLookup((Vector<int> data1, Vector<int> data2), Vector<uint> indices);

    /// svint64_t svtbl2[_s64](svint64x2_t data, svuint64_t indices) : "TBL Zresult.D, {Zdata0.D, Zdata1.D}, Zindices.D"
  public static unsafe Vector<long> VectorTableLookup((Vector<long> data1, Vector<long> data2), Vector<ulong> indices);

    /// svuint8_t svtbl2[_u8](svuint8x2_t data, svuint8_t indices) : "TBL Zresult.B, {Zdata0.B, Zdata1.B}, Zindices.B"
  public static unsafe Vector<byte> VectorTableLookup((Vector<byte> data1, Vector<byte> data2), Vector<byte> indices);

    /// svuint16_t svtbl2[_u16](svuint16x2_t data, svuint16_t indices) : "TBL Zresult.H, {Zdata0.H, Zdata1.H}, Zindices.H"
  public static unsafe Vector<ushort> VectorTableLookup((Vector<ushort> data1, Vector<ushort> data2), Vector<ushort> indices);

    /// svuint32_t svtbl2[_u32](svuint32x2_t data, svuint32_t indices) : "TBL Zresult.S, {Zdata0.S, Zdata1.S}, Zindices.S"
  public static unsafe Vector<uint> VectorTableLookup((Vector<uint> data1, Vector<uint> data2), Vector<uint> indices);

    /// svuint64_t svtbl2[_u64](svuint64x2_t data, svuint64_t indices) : "TBL Zresult.D, {Zdata0.D, Zdata1.D}, Zindices.D"
  public static unsafe Vector<ulong> VectorTableLookup((Vector<ulong> data1, Vector<ulong> data2), Vector<ulong> indices);


    /// VectorTableLookupExtension : Table lookup in single-vector table (merging)

    /// svfloat32_t svtbx[_f32](svfloat32_t fallback, svfloat32_t data, svuint32_t indices) : "TBX Ztied.S, Zdata.S, Zindices.S"
  public static unsafe Vector<float> VectorTableLookupExtension(Vector<float> fallback, Vector<float> data, Vector<uint> indices);

    /// svfloat64_t svtbx[_f64](svfloat64_t fallback, svfloat64_t data, svuint64_t indices) : "TBX Ztied.D, Zdata.D, Zindices.D"
  public static unsafe Vector<double> VectorTableLookupExtension(Vector<double> fallback, Vector<double> data, Vector<ulong> indices);

    /// svint8_t svtbx[_s8](svint8_t fallback, svint8_t data, svuint8_t indices) : "TBX Ztied.B, Zdata.B, Zindices.B"
  public static unsafe Vector<sbyte> VectorTableLookupExtension(Vector<sbyte> fallback, Vector<sbyte> data, Vector<byte> indices);

    /// svint16_t svtbx[_s16](svint16_t fallback, svint16_t data, svuint16_t indices) : "TBX Ztied.H, Zdata.H, Zindices.H"
  public static unsafe Vector<short> VectorTableLookupExtension(Vector<short> fallback, Vector<short> data, Vector<ushort> indices);

    /// svint32_t svtbx[_s32](svint32_t fallback, svint32_t data, svuint32_t indices) : "TBX Ztied.S, Zdata.S, Zindices.S"
  public static unsafe Vector<int> VectorTableLookupExtension(Vector<int> fallback, Vector<int> data, Vector<uint> indices);

    /// svint64_t svtbx[_s64](svint64_t fallback, svint64_t data, svuint64_t indices) : "TBX Ztied.D, Zdata.D, Zindices.D"
  public static unsafe Vector<long> VectorTableLookupExtension(Vector<long> fallback, Vector<long> data, Vector<ulong> indices);

    /// svuint8_t svtbx[_u8](svuint8_t fallback, svuint8_t data, svuint8_t indices) : "TBX Ztied.B, Zdata.B, Zindices.B"
  public static unsafe Vector<byte> VectorTableLookupExtension(Vector<byte> fallback, Vector<byte> data, Vector<byte> indices);

    /// svuint16_t svtbx[_u16](svuint16_t fallback, svuint16_t data, svuint16_t indices) : "TBX Ztied.H, Zdata.H, Zindices.H"
  public static unsafe Vector<ushort> VectorTableLookupExtension(Vector<ushort> fallback, Vector<ushort> data, Vector<ushort> indices);

    /// svuint32_t svtbx[_u32](svuint32_t fallback, svuint32_t data, svuint32_t indices) : "TBX Ztied.S, Zdata.S, Zindices.S"
  public static unsafe Vector<uint> VectorTableLookupExtension(Vector<uint> fallback, Vector<uint> data, Vector<uint> indices);

    /// svuint64_t svtbx[_u64](svuint64_t fallback, svuint64_t data, svuint64_t indices) : "TBX Ztied.D, Zdata.D, Zindices.D"
  public static unsafe Vector<ulong> VectorTableLookupExtension(Vector<ulong> fallback, Vector<ulong> data, Vector<ulong> indices);


  /// total method signatures: 48
  /// total method names:      6
}

  /// Optional Entries:
  ///   public static unsafe Vector<sbyte> InterleavingXorLowerUpper(Vector<sbyte> odd, Vector<sbyte> left, sbyte right); // sveorbt[_n_s8]
  ///   public static unsafe Vector<short> InterleavingXorLowerUpper(Vector<short> odd, Vector<short> left, short right); // sveorbt[_n_s16]
  ///   public static unsafe Vector<int> InterleavingXorLowerUpper(Vector<int> odd, Vector<int> left, int right); // sveorbt[_n_s32]
  ///   public static unsafe Vector<long> InterleavingXorLowerUpper(Vector<long> odd, Vector<long> left, long right); // sveorbt[_n_s64]
  ///   public static unsafe Vector<byte> InterleavingXorLowerUpper(Vector<byte> odd, Vector<byte> left, byte right); // sveorbt[_n_u8]
  ///   public static unsafe Vector<ushort> InterleavingXorLowerUpper(Vector<ushort> odd, Vector<ushort> left, ushort right); // sveorbt[_n_u16]
  ///   public static unsafe Vector<uint> InterleavingXorLowerUpper(Vector<uint> odd, Vector<uint> left, uint right); // sveorbt[_n_u32]
  ///   public static unsafe Vector<ulong> InterleavingXorLowerUpper(Vector<ulong> odd, Vector<ulong> left, ulong right); // sveorbt[_n_u64]
  ///   public static unsafe Vector<sbyte> InterleavingXorUpperLower(Vector<sbyte> even, Vector<sbyte> left, sbyte right); // sveortb[_n_s8]
  ///   public static unsafe Vector<short> InterleavingXorUpperLower(Vector<short> even, Vector<short> left, short right); // sveortb[_n_s16]
  ///   public static unsafe Vector<int> InterleavingXorUpperLower(Vector<int> even, Vector<int> left, int right); // sveortb[_n_s32]
  ///   public static unsafe Vector<long> InterleavingXorUpperLower(Vector<long> even, Vector<long> left, long right); // sveortb[_n_s64]
  ///   public static unsafe Vector<byte> InterleavingXorUpperLower(Vector<byte> even, Vector<byte> left, byte right); // sveortb[_n_u8]
  ///   public static unsafe Vector<ushort> InterleavingXorUpperLower(Vector<ushort> even, Vector<ushort> left, ushort right); // sveortb[_n_u16]
  ///   public static unsafe Vector<uint> InterleavingXorUpperLower(Vector<uint> even, Vector<uint> left, uint right); // sveortb[_n_u32]
  ///   public static unsafe Vector<ulong> InterleavingXorUpperLower(Vector<ulong> even, Vector<ulong> left, ulong right); // sveortb[_n_u64]
  ///   Total Maybe: 16

  /// Total ACLE covered across API:      64

