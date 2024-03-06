namespace System.Runtime.Intrinsics.Arm;

/// VectorT Summary
public abstract partial class Sve2 : AdvSimd /// Feature: FEAT_SVE2  Category: fp
{

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> AddRotateComplex(Vector<T> left, Vector<T> right, [ConstantExpected] byte rotation); // CADD // MOVPRFX

  public static unsafe Vector<float> DownConvertNarrowingUpper(Vector<double> value); // FCVTNT // predicated

  public static unsafe Vector<float> DownConvertRoundingOdd(Vector<double> value); // FCVTX // predicated, MOVPRFX

  public static unsafe Vector<float> DownConvertRoundingOddUpper(Vector<double> value); // FCVTXNT // predicated

  /// T: [int, float], [long, double]
  public static unsafe Vector<T> Log2(Vector<T2> value); // FLOGB // predicated, MOVPRFX

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> MultiplyAddRotateComplex(Vector<T> addend, Vector<T> left, Vector<T> right, [ConstantExpected] byte rotation); // CMLA // MOVPRFX

  /// T: short, int, ushort, uint
  public static unsafe Vector<T> MultiplyAddRotateComplexBySelectedScalar(Vector<T> addend, Vector<T> left, Vector<T> right, [ConstantExpected] byte rightIndex, [ConstantExpected] byte rotation); // CMLA // MOVPRFX

  public static unsafe Vector<uint> ReciprocalEstimate(Vector<uint> value); // URECPE // predicated, MOVPRFX

  public static unsafe Vector<uint> ReciprocalSqrtEstimate(Vector<uint> value); // URSQRTE // predicated, MOVPRFX

  /// T: sbyte, short, int, long
  public static unsafe Vector<T> SaturatingComplexAddRotate(Vector<T> op1, Vector<T> op2, [ConstantExpected] byte rotation); // SQCADD // MOVPRFX

  /// T: sbyte, short, int, long
  public static unsafe Vector<T> SaturatingRoundingDoublingComplexMultiplyAddHighRotate(Vector<T> op1, Vector<T> op2, Vector<T> op3, [ConstantExpected] byte rotation); // SQRDCMLAH // MOVPRFX

  /// T: short, int
  public static unsafe Vector<T> SaturatingRoundingDoublingComplexMultiplyAddHighRotate(Vector<T> op1, Vector<T> op2, Vector<T> op3, ulong imm_index, [ConstantExpected] byte rotation); // SQRDCMLAH // MOVPRFX

  public static unsafe Vector<double> UpConvertWideningUpper(Vector<float> value); // FCVTLT // predicated

  /// total method signatures: 13

}


/// Full API
public abstract partial class Sve2 : AdvSimd /// Feature: FEAT_SVE2  Category: fp
{
    /// AddRotateComplex : Complex add with rotate

    /// svint8_t svcadd[_s8](svint8_t op1, svint8_t op2, uint64_t imm_rotation) : "CADD Ztied1.B, Ztied1.B, Zop2.B, #imm_rotation" or "MOVPRFX Zresult, Zop1; CADD Zresult.B, Zresult.B, Zop2.B, #imm_rotation"
  public static unsafe Vector<sbyte> AddRotateComplex(Vector<sbyte> left, Vector<sbyte> right, [ConstantExpected] byte rotation);

    /// svint16_t svcadd[_s16](svint16_t op1, svint16_t op2, uint64_t imm_rotation) : "CADD Ztied1.H, Ztied1.H, Zop2.H, #imm_rotation" or "MOVPRFX Zresult, Zop1; CADD Zresult.H, Zresult.H, Zop2.H, #imm_rotation"
  public static unsafe Vector<short> AddRotateComplex(Vector<short> left, Vector<short> right, [ConstantExpected] byte rotation);

    /// svint32_t svcadd[_s32](svint32_t op1, svint32_t op2, uint64_t imm_rotation) : "CADD Ztied1.S, Ztied1.S, Zop2.S, #imm_rotation" or "MOVPRFX Zresult, Zop1; CADD Zresult.S, Zresult.S, Zop2.S, #imm_rotation"
  public static unsafe Vector<int> AddRotateComplex(Vector<int> left, Vector<int> right, [ConstantExpected] byte rotation);

    /// svint64_t svcadd[_s64](svint64_t op1, svint64_t op2, uint64_t imm_rotation) : "CADD Ztied1.D, Ztied1.D, Zop2.D, #imm_rotation" or "MOVPRFX Zresult, Zop1; CADD Zresult.D, Zresult.D, Zop2.D, #imm_rotation"
  public static unsafe Vector<long> AddRotateComplex(Vector<long> left, Vector<long> right, [ConstantExpected] byte rotation);

    /// svuint8_t svcadd[_u8](svuint8_t op1, svuint8_t op2, uint64_t imm_rotation) : "CADD Ztied1.B, Ztied1.B, Zop2.B, #imm_rotation" or "MOVPRFX Zresult, Zop1; CADD Zresult.B, Zresult.B, Zop2.B, #imm_rotation"
  public static unsafe Vector<byte> AddRotateComplex(Vector<byte> left, Vector<byte> right, [ConstantExpected] byte rotation);

    /// svuint16_t svcadd[_u16](svuint16_t op1, svuint16_t op2, uint64_t imm_rotation) : "CADD Ztied1.H, Ztied1.H, Zop2.H, #imm_rotation" or "MOVPRFX Zresult, Zop1; CADD Zresult.H, Zresult.H, Zop2.H, #imm_rotation"
  public static unsafe Vector<ushort> AddRotateComplex(Vector<ushort> left, Vector<ushort> right, [ConstantExpected] byte rotation);

    /// svuint32_t svcadd[_u32](svuint32_t op1, svuint32_t op2, uint64_t imm_rotation) : "CADD Ztied1.S, Ztied1.S, Zop2.S, #imm_rotation" or "MOVPRFX Zresult, Zop1; CADD Zresult.S, Zresult.S, Zop2.S, #imm_rotation"
  public static unsafe Vector<uint> AddRotateComplex(Vector<uint> left, Vector<uint> right, [ConstantExpected] byte rotation);

    /// svuint64_t svcadd[_u64](svuint64_t op1, svuint64_t op2, uint64_t imm_rotation) : "CADD Ztied1.D, Ztied1.D, Zop2.D, #imm_rotation" or "MOVPRFX Zresult, Zop1; CADD Zresult.D, Zresult.D, Zop2.D, #imm_rotation"
  public static unsafe Vector<ulong> AddRotateComplex(Vector<ulong> left, Vector<ulong> right, [ConstantExpected] byte rotation);


    /// DownConvertNarrowingUpper : Down convert and narrow (top)

    /// svfloat32_t svcvtnt_f32[_f64]_m(svfloat32_t even, svbool_t pg, svfloat64_t op) : "FCVTNT Ztied.S, Pg/M, Zop.D"
    /// svfloat32_t svcvtnt_f32[_f64]_x(svfloat32_t even, svbool_t pg, svfloat64_t op) : "FCVTNT Ztied.S, Pg/M, Zop.D"
  public static unsafe Vector<float> DownConvertNarrowingUpper(Vector<double> value);


    /// DownConvertRoundingOdd : Down convert, rounding to odd

    /// svfloat32_t svcvtx_f32[_f64]_m(svfloat32_t inactive, svbool_t pg, svfloat64_t op) : "FCVTX Ztied.S, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; FCVTX Zresult.S, Pg/M, Zop.D"
    /// svfloat32_t svcvtx_f32[_f64]_x(svbool_t pg, svfloat64_t op) : "FCVTX Ztied.S, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; FCVTX Zresult.S, Pg/M, Zop.D"
    /// svfloat32_t svcvtx_f32[_f64]_z(svbool_t pg, svfloat64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; FCVTX Zresult.S, Pg/M, Zop.D"
  public static unsafe Vector<float> DownConvertRoundingOdd(Vector<double> value);


    /// DownConvertRoundingOddUpper : Down convert, rounding to odd (top)

    /// svfloat32_t svcvtxnt_f32[_f64]_m(svfloat32_t even, svbool_t pg, svfloat64_t op) : "FCVTXNT Ztied.S, Pg/M, Zop.D"
    /// svfloat32_t svcvtxnt_f32[_f64]_x(svfloat32_t even, svbool_t pg, svfloat64_t op) : "FCVTXNT Ztied.S, Pg/M, Zop.D"
  public static unsafe Vector<float> DownConvertRoundingOddUpper(Vector<double> value);


    /// Log2 : Base 2 logarithm as integer

    /// svint32_t svlogb[_f32]_m(svint32_t inactive, svbool_t pg, svfloat32_t op) : "FLOGB Ztied.S, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; FLOGB Zresult.S, Pg/M, Zop.S"
    /// svint32_t svlogb[_f32]_x(svbool_t pg, svfloat32_t op) : "FLOGB Ztied.S, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; FLOGB Zresult.S, Pg/M, Zop.S"
    /// svint32_t svlogb[_f32]_z(svbool_t pg, svfloat32_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; FLOGB Zresult.S, Pg/M, Zop.S"
  public static unsafe Vector<int> Log2(Vector<float> value);

    /// svint64_t svlogb[_f64]_m(svint64_t inactive, svbool_t pg, svfloat64_t op) : "FLOGB Ztied.D, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; FLOGB Zresult.D, Pg/M, Zop.D"
    /// svint64_t svlogb[_f64]_x(svbool_t pg, svfloat64_t op) : "FLOGB Ztied.D, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; FLOGB Zresult.D, Pg/M, Zop.D"
    /// svint64_t svlogb[_f64]_z(svbool_t pg, svfloat64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; FLOGB Zresult.D, Pg/M, Zop.D"
  public static unsafe Vector<long> Log2(Vector<double> value);


    /// MultiplyAddRotateComplex : Complex multiply-add with rotate

    /// svint8_t svcmla[_s8](svint8_t op1, svint8_t op2, svint8_t op3, uint64_t imm_rotation) : "CMLA Ztied1.B, Zop2.B, Zop3.B, #imm_rotation" or "MOVPRFX Zresult, Zop1; CMLA Zresult.B, Zop2.B, Zop3.B, #imm_rotation"
  public static unsafe Vector<sbyte> MultiplyAddRotateComplex(Vector<sbyte> addend, Vector<sbyte> left, Vector<sbyte> right, [ConstantExpected] byte rotation);

    /// svint16_t svcmla[_s16](svint16_t op1, svint16_t op2, svint16_t op3, uint64_t imm_rotation) : "CMLA Ztied1.H, Zop2.H, Zop3.H, #imm_rotation" or "MOVPRFX Zresult, Zop1; CMLA Zresult.H, Zop2.H, Zop3.H, #imm_rotation"
  public static unsafe Vector<short> MultiplyAddRotateComplex(Vector<short> addend, Vector<short> left, Vector<short> right, [ConstantExpected] byte rotation);

    /// svint32_t svcmla[_s32](svint32_t op1, svint32_t op2, svint32_t op3, uint64_t imm_rotation) : "CMLA Ztied1.S, Zop2.S, Zop3.S, #imm_rotation" or "MOVPRFX Zresult, Zop1; CMLA Zresult.S, Zop2.S, Zop3.S, #imm_rotation"
  public static unsafe Vector<int> MultiplyAddRotateComplex(Vector<int> addend, Vector<int> left, Vector<int> right, [ConstantExpected] byte rotation);

    /// svint64_t svcmla[_s64](svint64_t op1, svint64_t op2, svint64_t op3, uint64_t imm_rotation) : "CMLA Ztied1.D, Zop2.D, Zop3.D, #imm_rotation" or "MOVPRFX Zresult, Zop1; CMLA Zresult.D, Zop2.D, Zop3.D, #imm_rotation"
  public static unsafe Vector<long> MultiplyAddRotateComplex(Vector<long> addend, Vector<long> left, Vector<long> right, [ConstantExpected] byte rotation);

    /// svuint8_t svcmla[_u8](svuint8_t op1, svuint8_t op2, svuint8_t op3, uint64_t imm_rotation) : "CMLA Ztied1.B, Zop2.B, Zop3.B, #imm_rotation" or "MOVPRFX Zresult, Zop1; CMLA Zresult.B, Zop2.B, Zop3.B, #imm_rotation"
  public static unsafe Vector<byte> MultiplyAddRotateComplex(Vector<byte> addend, Vector<byte> left, Vector<byte> right, [ConstantExpected] byte rotation);

    /// svuint16_t svcmla[_u16](svuint16_t op1, svuint16_t op2, svuint16_t op3, uint64_t imm_rotation) : "CMLA Ztied1.H, Zop2.H, Zop3.H, #imm_rotation" or "MOVPRFX Zresult, Zop1; CMLA Zresult.H, Zop2.H, Zop3.H, #imm_rotation"
  public static unsafe Vector<ushort> MultiplyAddRotateComplex(Vector<ushort> addend, Vector<ushort> left, Vector<ushort> right, [ConstantExpected] byte rotation);

    /// svuint32_t svcmla[_u32](svuint32_t op1, svuint32_t op2, svuint32_t op3, uint64_t imm_rotation) : "CMLA Ztied1.S, Zop2.S, Zop3.S, #imm_rotation" or "MOVPRFX Zresult, Zop1; CMLA Zresult.S, Zop2.S, Zop3.S, #imm_rotation"
  public static unsafe Vector<uint> MultiplyAddRotateComplex(Vector<uint> addend, Vector<uint> left, Vector<uint> right, [ConstantExpected] byte rotation);

    /// svuint64_t svcmla[_u64](svuint64_t op1, svuint64_t op2, svuint64_t op3, uint64_t imm_rotation) : "CMLA Ztied1.D, Zop2.D, Zop3.D, #imm_rotation" or "MOVPRFX Zresult, Zop1; CMLA Zresult.D, Zop2.D, Zop3.D, #imm_rotation"
  public static unsafe Vector<ulong> MultiplyAddRotateComplex(Vector<ulong> addend, Vector<ulong> left, Vector<ulong> right, [ConstantExpected] byte rotation);


    /// MultiplyAddRotateComplexBySelectedScalar : Complex multiply-add with rotate

    /// svint16_t svcmla_lane[_s16](svint16_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index, uint64_t imm_rotation) : "CMLA Ztied1.H, Zop2.H, Zop3.H[imm_index], #imm_rotation" or "MOVPRFX Zresult, Zop1; CMLA Zresult.H, Zop2.H, Zop3.H[imm_index], #imm_rotation"
  public static unsafe Vector<short> MultiplyAddRotateComplexBySelectedScalar(Vector<short> addend, Vector<short> left, Vector<short> right, [ConstantExpected] byte rightIndex, [ConstantExpected] byte rotation);

    /// svint32_t svcmla_lane[_s32](svint32_t op1, svint32_t op2, svint32_t op3, uint64_t imm_index, uint64_t imm_rotation) : "CMLA Ztied1.S, Zop2.S, Zop3.S[imm_index], #imm_rotation" or "MOVPRFX Zresult, Zop1; CMLA Zresult.S, Zop2.S, Zop3.S[imm_index], #imm_rotation"
  public static unsafe Vector<int> MultiplyAddRotateComplexBySelectedScalar(Vector<int> addend, Vector<int> left, Vector<int> right, [ConstantExpected] byte rightIndex, [ConstantExpected] byte rotation);

    /// svuint16_t svcmla_lane[_u16](svuint16_t op1, svuint16_t op2, svuint16_t op3, uint64_t imm_index, uint64_t imm_rotation) : "CMLA Ztied1.H, Zop2.H, Zop3.H[imm_index], #imm_rotation" or "MOVPRFX Zresult, Zop1; CMLA Zresult.H, Zop2.H, Zop3.H[imm_index], #imm_rotation"
  public static unsafe Vector<ushort> MultiplyAddRotateComplexBySelectedScalar(Vector<ushort> addend, Vector<ushort> left, Vector<ushort> right, [ConstantExpected] byte rightIndex, [ConstantExpected] byte rotation);

    /// svuint32_t svcmla_lane[_u32](svuint32_t op1, svuint32_t op2, svuint32_t op3, uint64_t imm_index, uint64_t imm_rotation) : "CMLA Ztied1.S, Zop2.S, Zop3.S[imm_index], #imm_rotation" or "MOVPRFX Zresult, Zop1; CMLA Zresult.S, Zop2.S, Zop3.S[imm_index], #imm_rotation"
  public static unsafe Vector<uint> MultiplyAddRotateComplexBySelectedScalar(Vector<uint> addend, Vector<uint> left, Vector<uint> right, [ConstantExpected] byte rightIndex, [ConstantExpected] byte rotation);


    /// ReciprocalEstimate : Reciprocal estimate

    /// svuint32_t svrecpe[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op) : "URECPE Ztied.S, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; URECPE Zresult.S, Pg/M, Zop.S"
    /// svuint32_t svrecpe[_u32]_x(svbool_t pg, svuint32_t op) : "URECPE Ztied.S, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; URECPE Zresult.S, Pg/M, Zop.S"
    /// svuint32_t svrecpe[_u32]_z(svbool_t pg, svuint32_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; URECPE Zresult.S, Pg/M, Zop.S"
  public static unsafe Vector<uint> ReciprocalEstimate(Vector<uint> value);


    /// ReciprocalSqrtEstimate : Reciprocal square root estimate

    /// svuint32_t svrsqrte[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op) : "URSQRTE Ztied.S, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; URSQRTE Zresult.S, Pg/M, Zop.S"
    /// svuint32_t svrsqrte[_u32]_x(svbool_t pg, svuint32_t op) : "URSQRTE Ztied.S, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; URSQRTE Zresult.S, Pg/M, Zop.S"
    /// svuint32_t svrsqrte[_u32]_z(svbool_t pg, svuint32_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; URSQRTE Zresult.S, Pg/M, Zop.S"
  public static unsafe Vector<uint> ReciprocalSqrtEstimate(Vector<uint> value);


    /// SaturatingComplexAddRotate : Saturating complex add with rotate

    /// svint8_t svqcadd[_s8](svint8_t op1, svint8_t op2, uint64_t imm_rotation) : "SQCADD Ztied1.B, Ztied1.B, Zop2.B, #imm_rotation" or "MOVPRFX Zresult, Zop1; SQCADD Zresult.B, Zresult.B, Zop2.B, #imm_rotation"
  public static unsafe Vector<sbyte> SaturatingComplexAddRotate(Vector<sbyte> op1, Vector<sbyte> op2, [ConstantExpected] byte rotation);

    /// svint16_t svqcadd[_s16](svint16_t op1, svint16_t op2, uint64_t imm_rotation) : "SQCADD Ztied1.H, Ztied1.H, Zop2.H, #imm_rotation" or "MOVPRFX Zresult, Zop1; SQCADD Zresult.H, Zresult.H, Zop2.H, #imm_rotation"
  public static unsafe Vector<short> SaturatingComplexAddRotate(Vector<short> op1, Vector<short> op2, [ConstantExpected] byte rotation);

    /// svint32_t svqcadd[_s32](svint32_t op1, svint32_t op2, uint64_t imm_rotation) : "SQCADD Ztied1.S, Ztied1.S, Zop2.S, #imm_rotation" or "MOVPRFX Zresult, Zop1; SQCADD Zresult.S, Zresult.S, Zop2.S, #imm_rotation"
  public static unsafe Vector<int> SaturatingComplexAddRotate(Vector<int> op1, Vector<int> op2, [ConstantExpected] byte rotation);

    /// svint64_t svqcadd[_s64](svint64_t op1, svint64_t op2, uint64_t imm_rotation) : "SQCADD Ztied1.D, Ztied1.D, Zop2.D, #imm_rotation" or "MOVPRFX Zresult, Zop1; SQCADD Zresult.D, Zresult.D, Zop2.D, #imm_rotation"
  public static unsafe Vector<long> SaturatingComplexAddRotate(Vector<long> op1, Vector<long> op2, [ConstantExpected] byte rotation);


    /// SaturatingRoundingDoublingComplexMultiplyAddHighRotate : Saturating rounding doubling complex multiply-add high with rotate

    /// svint8_t svqrdcmlah[_s8](svint8_t op1, svint8_t op2, svint8_t op3, uint64_t imm_rotation) : "SQRDCMLAH Ztied1.B, Zop2.B, Zop3.B, #imm_rotation" or "MOVPRFX Zresult, Zop1; SQRDCMLAH Zresult.B, Zop2.B, Zop3.B, #imm_rotation"
  public static unsafe Vector<sbyte> SaturatingRoundingDoublingComplexMultiplyAddHighRotate(Vector<sbyte> op1, Vector<sbyte> op2, Vector<sbyte> op3, [ConstantExpected] byte rotation);

    /// svint16_t svqrdcmlah[_s16](svint16_t op1, svint16_t op2, svint16_t op3, uint64_t imm_rotation) : "SQRDCMLAH Ztied1.H, Zop2.H, Zop3.H, #imm_rotation" or "MOVPRFX Zresult, Zop1; SQRDCMLAH Zresult.H, Zop2.H, Zop3.H, #imm_rotation"
  public static unsafe Vector<short> SaturatingRoundingDoublingComplexMultiplyAddHighRotate(Vector<short> op1, Vector<short> op2, Vector<short> op3, [ConstantExpected] byte rotation);

    /// svint32_t svqrdcmlah[_s32](svint32_t op1, svint32_t op2, svint32_t op3, uint64_t imm_rotation) : "SQRDCMLAH Ztied1.S, Zop2.S, Zop3.S, #imm_rotation" or "MOVPRFX Zresult, Zop1; SQRDCMLAH Zresult.S, Zop2.S, Zop3.S, #imm_rotation"
  public static unsafe Vector<int> SaturatingRoundingDoublingComplexMultiplyAddHighRotate(Vector<int> op1, Vector<int> op2, Vector<int> op3, [ConstantExpected] byte rotation);

    /// svint64_t svqrdcmlah[_s64](svint64_t op1, svint64_t op2, svint64_t op3, uint64_t imm_rotation) : "SQRDCMLAH Ztied1.D, Zop2.D, Zop3.D, #imm_rotation" or "MOVPRFX Zresult, Zop1; SQRDCMLAH Zresult.D, Zop2.D, Zop3.D, #imm_rotation"
  public static unsafe Vector<long> SaturatingRoundingDoublingComplexMultiplyAddHighRotate(Vector<long> op1, Vector<long> op2, Vector<long> op3, [ConstantExpected] byte rotation);

    /// svint16_t svqrdcmlah_lane[_s16](svint16_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index, uint64_t imm_rotation) : "SQRDCMLAH Ztied1.H, Zop2.H, Zop3.H[imm_index], #imm_rotation" or "MOVPRFX Zresult, Zop1; SQRDCMLAH Zresult.H, Zop2.H, Zop3.H[imm_index], #imm_rotation"
  public static unsafe Vector<short> SaturatingRoundingDoublingComplexMultiplyAddHighRotate(Vector<short> op1, Vector<short> op2, Vector<short> op3, ulong imm_index, [ConstantExpected] byte rotation);

    /// svint32_t svqrdcmlah_lane[_s32](svint32_t op1, svint32_t op2, svint32_t op3, uint64_t imm_index, uint64_t imm_rotation) : "SQRDCMLAH Ztied1.S, Zop2.S, Zop3.S[imm_index], #imm_rotation" or "MOVPRFX Zresult, Zop1; SQRDCMLAH Zresult.S, Zop2.S, Zop3.S[imm_index], #imm_rotation"
  public static unsafe Vector<int> SaturatingRoundingDoublingComplexMultiplyAddHighRotate(Vector<int> op1, Vector<int> op2, Vector<int> op3, ulong imm_index, [ConstantExpected] byte rotation);


    /// UpConvertWideningUpper : Up convert long (top)

    /// svfloat64_t svcvtlt_f64[_f32]_m(svfloat64_t inactive, svbool_t pg, svfloat32_t op) : "FCVTLT Ztied.D, Pg/M, Zop.S"
    /// svfloat64_t svcvtlt_f64[_f32]_x(svbool_t pg, svfloat32_t op) : "FCVTLT Ztied.D, Pg/M, Ztied.S"
  public static unsafe Vector<double> UpConvertWideningUpper(Vector<float> value);


  /// total method signatures: 38
  /// total method names:      12
}


  /// Total ACLE covered across API:      51

