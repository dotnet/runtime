namespace System.Runtime.Intrinsics.Arm;

/// VectorT Summary
public abstract partial class SveF64mm : AdvSimd /// Feature: FEAT_F64MM
{

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> ConcatenateEvenInt128FromTwoInputs(Vector<T> left, Vector<T> right); // UZP1

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> ConcatenateOddInt128FromTwoInputs(Vector<T> left, Vector<T> right); // UZP2

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> InterleaveEvenInt128FromTwoInputs(Vector<T> left, Vector<T> right); // TRN1

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<T> left, Vector<T> right); // ZIP2

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<T> left, Vector<T> right); // ZIP1

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> InterleaveOddInt128FromTwoInputs(Vector<T> left, Vector<T> right); // TRN2

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> LoadVector256AndReplicateToVector(Vector<T> mask, T* address); // LD1ROW or LD1ROD or LD1ROB or LD1ROH

  public static unsafe Vector<double> MatrixMultiplyAccumulate(Vector<double> op1, Vector<double> op2, Vector<double> op3); // FMMLA // MOVPRFX

  /// total method signatures: 8

}


/// Full API
public abstract partial class SveF64mm : AdvSimd /// Feature: FEAT_F64MM
{
    /// ConcatenateEvenInt128FromTwoInputs : Concatenate even quadwords from two inputs

    /// svfloat32_t svuzp1q[_f32](svfloat32_t op1, svfloat32_t op2) : "UZP1 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<float> ConcatenateEvenInt128FromTwoInputs(Vector<float> left, Vector<float> right);

    /// svfloat64_t svuzp1q[_f64](svfloat64_t op1, svfloat64_t op2) : "UZP1 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<double> ConcatenateEvenInt128FromTwoInputs(Vector<double> left, Vector<double> right);

    /// svint8_t svuzp1q[_s8](svint8_t op1, svint8_t op2) : "UZP1 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<sbyte> ConcatenateEvenInt128FromTwoInputs(Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t svuzp1q[_s16](svint16_t op1, svint16_t op2) : "UZP1 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<short> ConcatenateEvenInt128FromTwoInputs(Vector<short> left, Vector<short> right);

    /// svint32_t svuzp1q[_s32](svint32_t op1, svint32_t op2) : "UZP1 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<int> ConcatenateEvenInt128FromTwoInputs(Vector<int> left, Vector<int> right);

    /// svint64_t svuzp1q[_s64](svint64_t op1, svint64_t op2) : "UZP1 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<long> ConcatenateEvenInt128FromTwoInputs(Vector<long> left, Vector<long> right);

    /// svuint8_t svuzp1q[_u8](svuint8_t op1, svuint8_t op2) : "UZP1 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<byte> ConcatenateEvenInt128FromTwoInputs(Vector<byte> left, Vector<byte> right);

    /// svuint16_t svuzp1q[_u16](svuint16_t op1, svuint16_t op2) : "UZP1 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<ushort> ConcatenateEvenInt128FromTwoInputs(Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svuzp1q[_u32](svuint32_t op1, svuint32_t op2) : "UZP1 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<uint> ConcatenateEvenInt128FromTwoInputs(Vector<uint> left, Vector<uint> right);

    /// svuint64_t svuzp1q[_u64](svuint64_t op1, svuint64_t op2) : "UZP1 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<ulong> ConcatenateEvenInt128FromTwoInputs(Vector<ulong> left, Vector<ulong> right);


    /// ConcatenateOddInt128FromTwoInputs : Concatenate odd quadwords from two inputs

    /// svfloat32_t svuzp2q[_f32](svfloat32_t op1, svfloat32_t op2) : "UZP2 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<float> ConcatenateOddInt128FromTwoInputs(Vector<float> left, Vector<float> right);

    /// svfloat64_t svuzp2q[_f64](svfloat64_t op1, svfloat64_t op2) : "UZP2 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<double> ConcatenateOddInt128FromTwoInputs(Vector<double> left, Vector<double> right);

    /// svint8_t svuzp2q[_s8](svint8_t op1, svint8_t op2) : "UZP2 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<sbyte> ConcatenateOddInt128FromTwoInputs(Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t svuzp2q[_s16](svint16_t op1, svint16_t op2) : "UZP2 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<short> ConcatenateOddInt128FromTwoInputs(Vector<short> left, Vector<short> right);

    /// svint32_t svuzp2q[_s32](svint32_t op1, svint32_t op2) : "UZP2 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<int> ConcatenateOddInt128FromTwoInputs(Vector<int> left, Vector<int> right);

    /// svint64_t svuzp2q[_s64](svint64_t op1, svint64_t op2) : "UZP2 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<long> ConcatenateOddInt128FromTwoInputs(Vector<long> left, Vector<long> right);

    /// svuint8_t svuzp2q[_u8](svuint8_t op1, svuint8_t op2) : "UZP2 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<byte> ConcatenateOddInt128FromTwoInputs(Vector<byte> left, Vector<byte> right);

    /// svuint16_t svuzp2q[_u16](svuint16_t op1, svuint16_t op2) : "UZP2 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<ushort> ConcatenateOddInt128FromTwoInputs(Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svuzp2q[_u32](svuint32_t op1, svuint32_t op2) : "UZP2 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<uint> ConcatenateOddInt128FromTwoInputs(Vector<uint> left, Vector<uint> right);

    /// svuint64_t svuzp2q[_u64](svuint64_t op1, svuint64_t op2) : "UZP2 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<ulong> ConcatenateOddInt128FromTwoInputs(Vector<ulong> left, Vector<ulong> right);


    /// InterleaveEvenInt128FromTwoInputs : Interleave even quadwords from two inputs

    /// svfloat32_t svtrn1q[_f32](svfloat32_t op1, svfloat32_t op2) : "TRN1 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<float> InterleaveEvenInt128FromTwoInputs(Vector<float> left, Vector<float> right);

    /// svfloat64_t svtrn1q[_f64](svfloat64_t op1, svfloat64_t op2) : "TRN1 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<double> InterleaveEvenInt128FromTwoInputs(Vector<double> left, Vector<double> right);

    /// svint8_t svtrn1q[_s8](svint8_t op1, svint8_t op2) : "TRN1 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<sbyte> InterleaveEvenInt128FromTwoInputs(Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t svtrn1q[_s16](svint16_t op1, svint16_t op2) : "TRN1 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<short> InterleaveEvenInt128FromTwoInputs(Vector<short> left, Vector<short> right);

    /// svint32_t svtrn1q[_s32](svint32_t op1, svint32_t op2) : "TRN1 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<int> InterleaveEvenInt128FromTwoInputs(Vector<int> left, Vector<int> right);

    /// svint64_t svtrn1q[_s64](svint64_t op1, svint64_t op2) : "TRN1 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<long> InterleaveEvenInt128FromTwoInputs(Vector<long> left, Vector<long> right);

    /// svuint8_t svtrn1q[_u8](svuint8_t op1, svuint8_t op2) : "TRN1 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<byte> InterleaveEvenInt128FromTwoInputs(Vector<byte> left, Vector<byte> right);

    /// svuint16_t svtrn1q[_u16](svuint16_t op1, svuint16_t op2) : "TRN1 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<ushort> InterleaveEvenInt128FromTwoInputs(Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svtrn1q[_u32](svuint32_t op1, svuint32_t op2) : "TRN1 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<uint> InterleaveEvenInt128FromTwoInputs(Vector<uint> left, Vector<uint> right);

    /// svuint64_t svtrn1q[_u64](svuint64_t op1, svuint64_t op2) : "TRN1 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<ulong> InterleaveEvenInt128FromTwoInputs(Vector<ulong> left, Vector<ulong> right);


    /// InterleaveInt128FromHighHalvesOfTwoInputs : Interleave quadwords from high halves of two inputs

    /// svfloat32_t svzip2q[_f32](svfloat32_t op1, svfloat32_t op2) : "ZIP2 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<float> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<float> left, Vector<float> right);

    /// svfloat64_t svzip2q[_f64](svfloat64_t op1, svfloat64_t op2) : "ZIP2 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<double> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<double> left, Vector<double> right);

    /// svint8_t svzip2q[_s8](svint8_t op1, svint8_t op2) : "ZIP2 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<sbyte> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t svzip2q[_s16](svint16_t op1, svint16_t op2) : "ZIP2 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<short> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<short> left, Vector<short> right);

    /// svint32_t svzip2q[_s32](svint32_t op1, svint32_t op2) : "ZIP2 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<int> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<int> left, Vector<int> right);

    /// svint64_t svzip2q[_s64](svint64_t op1, svint64_t op2) : "ZIP2 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<long> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<long> left, Vector<long> right);

    /// svuint8_t svzip2q[_u8](svuint8_t op1, svuint8_t op2) : "ZIP2 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<byte> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<byte> left, Vector<byte> right);

    /// svuint16_t svzip2q[_u16](svuint16_t op1, svuint16_t op2) : "ZIP2 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<ushort> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svzip2q[_u32](svuint32_t op1, svuint32_t op2) : "ZIP2 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<uint> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<uint> left, Vector<uint> right);

    /// svuint64_t svzip2q[_u64](svuint64_t op1, svuint64_t op2) : "ZIP2 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<ulong> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<ulong> left, Vector<ulong> right);


    /// InterleaveInt128FromLowHalvesOfTwoInputs : Interleave quadwords from low halves of two inputs

    /// svfloat32_t svzip1q[_f32](svfloat32_t op1, svfloat32_t op2) : "ZIP1 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<float> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<float> left, Vector<float> right);

    /// svfloat64_t svzip1q[_f64](svfloat64_t op1, svfloat64_t op2) : "ZIP1 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<double> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<double> left, Vector<double> right);

    /// svint8_t svzip1q[_s8](svint8_t op1, svint8_t op2) : "ZIP1 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<sbyte> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t svzip1q[_s16](svint16_t op1, svint16_t op2) : "ZIP1 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<short> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<short> left, Vector<short> right);

    /// svint32_t svzip1q[_s32](svint32_t op1, svint32_t op2) : "ZIP1 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<int> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<int> left, Vector<int> right);

    /// svint64_t svzip1q[_s64](svint64_t op1, svint64_t op2) : "ZIP1 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<long> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<long> left, Vector<long> right);

    /// svuint8_t svzip1q[_u8](svuint8_t op1, svuint8_t op2) : "ZIP1 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<byte> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<byte> left, Vector<byte> right);

    /// svuint16_t svzip1q[_u16](svuint16_t op1, svuint16_t op2) : "ZIP1 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<ushort> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svzip1q[_u32](svuint32_t op1, svuint32_t op2) : "ZIP1 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<uint> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<uint> left, Vector<uint> right);

    /// svuint64_t svzip1q[_u64](svuint64_t op1, svuint64_t op2) : "ZIP1 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<ulong> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<ulong> left, Vector<ulong> right);


    /// InterleaveOddInt128FromTwoInputs : Interleave odd quadwords from two inputs

    /// svfloat32_t svtrn2q[_f32](svfloat32_t op1, svfloat32_t op2) : "TRN2 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<float> InterleaveOddInt128FromTwoInputs(Vector<float> left, Vector<float> right);

    /// svfloat64_t svtrn2q[_f64](svfloat64_t op1, svfloat64_t op2) : "TRN2 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<double> InterleaveOddInt128FromTwoInputs(Vector<double> left, Vector<double> right);

    /// svint8_t svtrn2q[_s8](svint8_t op1, svint8_t op2) : "TRN2 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<sbyte> InterleaveOddInt128FromTwoInputs(Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t svtrn2q[_s16](svint16_t op1, svint16_t op2) : "TRN2 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<short> InterleaveOddInt128FromTwoInputs(Vector<short> left, Vector<short> right);

    /// svint32_t svtrn2q[_s32](svint32_t op1, svint32_t op2) : "TRN2 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<int> InterleaveOddInt128FromTwoInputs(Vector<int> left, Vector<int> right);

    /// svint64_t svtrn2q[_s64](svint64_t op1, svint64_t op2) : "TRN2 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<long> InterleaveOddInt128FromTwoInputs(Vector<long> left, Vector<long> right);

    /// svuint8_t svtrn2q[_u8](svuint8_t op1, svuint8_t op2) : "TRN2 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<byte> InterleaveOddInt128FromTwoInputs(Vector<byte> left, Vector<byte> right);

    /// svuint16_t svtrn2q[_u16](svuint16_t op1, svuint16_t op2) : "TRN2 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<ushort> InterleaveOddInt128FromTwoInputs(Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svtrn2q[_u32](svuint32_t op1, svuint32_t op2) : "TRN2 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<uint> InterleaveOddInt128FromTwoInputs(Vector<uint> left, Vector<uint> right);

    /// svuint64_t svtrn2q[_u64](svuint64_t op1, svuint64_t op2) : "TRN2 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<ulong> InterleaveOddInt128FromTwoInputs(Vector<ulong> left, Vector<ulong> right);


    /// LoadVector256AndReplicateToVector : Load and replicate 256 bits of data

    /// svfloat32_t svld1ro[_f32](svbool_t pg, const float32_t *base) : "LD1ROW Zresult.S, Pg/Z, [Xarray, Xindex, LSL #2]" or "LD1ROW Zresult.S, Pg/Z, [Xarray, #index * 4]" or "LD1ROW Zresult.S, Pg/Z, [Xbase, #0]"
  public static unsafe Vector<float> LoadVector256AndReplicateToVector(Vector<float> mask, float* address);

    /// svfloat64_t svld1ro[_f64](svbool_t pg, const float64_t *base) : "LD1ROD Zresult.D, Pg/Z, [Xarray, Xindex, LSL #3]" or "LD1ROD Zresult.D, Pg/Z, [Xarray, #index * 8]" or "LD1ROD Zresult.D, Pg/Z, [Xbase, #0]"
  public static unsafe Vector<double> LoadVector256AndReplicateToVector(Vector<double> mask, double* address);

    /// svint8_t svld1ro[_s8](svbool_t pg, const int8_t *base) : "LD1ROB Zresult.B, Pg/Z, [Xarray, Xindex]" or "LD1ROB Zresult.B, Pg/Z, [Xarray, #index]" or "LD1ROB Zresult.B, Pg/Z, [Xbase, #0]"
  public static unsafe Vector<sbyte> LoadVector256AndReplicateToVector(Vector<sbyte> mask, sbyte* address);

    /// svint16_t svld1ro[_s16](svbool_t pg, const int16_t *base) : "LD1ROH Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]" or "LD1ROH Zresult.H, Pg/Z, [Xarray, #index * 2]" or "LD1ROH Zresult.H, Pg/Z, [Xbase, #0]"
  public static unsafe Vector<short> LoadVector256AndReplicateToVector(Vector<short> mask, short* address);

    /// svint32_t svld1ro[_s32](svbool_t pg, const int32_t *base) : "LD1ROW Zresult.S, Pg/Z, [Xarray, Xindex, LSL #2]" or "LD1ROW Zresult.S, Pg/Z, [Xarray, #index * 4]" or "LD1ROW Zresult.S, Pg/Z, [Xbase, #0]"
  public static unsafe Vector<int> LoadVector256AndReplicateToVector(Vector<int> mask, int* address);

    /// svint64_t svld1ro[_s64](svbool_t pg, const int64_t *base) : "LD1ROD Zresult.D, Pg/Z, [Xarray, Xindex, LSL #3]" or "LD1ROD Zresult.D, Pg/Z, [Xarray, #index * 8]" or "LD1ROD Zresult.D, Pg/Z, [Xbase, #0]"
  public static unsafe Vector<long> LoadVector256AndReplicateToVector(Vector<long> mask, long* address);

    /// svuint8_t svld1ro[_u8](svbool_t pg, const uint8_t *base) : "LD1ROB Zresult.B, Pg/Z, [Xarray, Xindex]" or "LD1ROB Zresult.B, Pg/Z, [Xarray, #index]" or "LD1ROB Zresult.B, Pg/Z, [Xbase, #0]"
  public static unsafe Vector<byte> LoadVector256AndReplicateToVector(Vector<byte> mask, byte* address);

    /// svuint16_t svld1ro[_u16](svbool_t pg, const uint16_t *base) : "LD1ROH Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]" or "LD1ROH Zresult.H, Pg/Z, [Xarray, #index * 2]" or "LD1ROH Zresult.H, Pg/Z, [Xbase, #0]"
  public static unsafe Vector<ushort> LoadVector256AndReplicateToVector(Vector<ushort> mask, ushort* address);

    /// svuint32_t svld1ro[_u32](svbool_t pg, const uint32_t *base) : "LD1ROW Zresult.S, Pg/Z, [Xarray, Xindex, LSL #2]" or "LD1ROW Zresult.S, Pg/Z, [Xarray, #index * 4]" or "LD1ROW Zresult.S, Pg/Z, [Xbase, #0]"
  public static unsafe Vector<uint> LoadVector256AndReplicateToVector(Vector<uint> mask, uint* address);

    /// svuint64_t svld1ro[_u64](svbool_t pg, const uint64_t *base) : "LD1ROD Zresult.D, Pg/Z, [Xarray, Xindex, LSL #3]" or "LD1ROD Zresult.D, Pg/Z, [Xarray, #index * 8]" or "LD1ROD Zresult.D, Pg/Z, [Xbase, #0]"
  public static unsafe Vector<ulong> LoadVector256AndReplicateToVector(Vector<ulong> mask, ulong* address);


    /// MatrixMultiplyAccumulate : Matrix multiply-accumulate

    /// svfloat64_t svmmla[_f64](svfloat64_t op1, svfloat64_t op2, svfloat64_t op3) : "FMMLA Ztied1.D, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; FMMLA Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<double> MatrixMultiplyAccumulate(Vector<double> op1, Vector<double> op2, Vector<double> op3);


  /// total method signatures: 71
  /// total method names:      8
}


  /// Total ACLE covered across API:      71

