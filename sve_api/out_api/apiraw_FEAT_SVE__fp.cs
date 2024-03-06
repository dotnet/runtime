namespace System.Runtime.Intrinsics.Arm;

/// VectorT Summary
public abstract partial class Sve : AdvSimd /// Feature: FEAT_SVE  Category: fp
{

  /// T: float, double
  public static unsafe Vector<T> AddRotateComplex(Vector<T> left, Vector<T> right, [ConstantExpected] byte rotation); // FCADD // predicated, MOVPRFX

  /// T: float, double
  public static unsafe Vector<T> AddSequentialAcross(Vector<T> initial, Vector<T> value); // FADDA // predicated

  /// T: [double, float], [double, int], [double, long], [double, uint], [double, ulong]
  public static unsafe Vector<T> ConvertToDouble(Vector<T2> value); // FCVT or SCVTF or UCVTF // predicated, MOVPRFX

  /// T: [int, float], [int, double]
  public static unsafe Vector<T> ConvertToInt32(Vector<T2> value); // FCVTZS // predicated, MOVPRFX

  /// T: [long, float], [long, double]
  public static unsafe Vector<T> ConvertToInt64(Vector<T2> value); // FCVTZS // predicated, MOVPRFX

  /// T: [float, double], [float, int], [float, long], [float, uint], [float, ulong]
  public static unsafe Vector<T> ConvertToSingle(Vector<T2> value); // FCVT or SCVTF or UCVTF // predicated, MOVPRFX

  /// T: [uint, float], [uint, double]
  public static unsafe Vector<T> ConvertToUInt32(Vector<T2> value); // FCVTZU // predicated, MOVPRFX

  /// T: [ulong, float], [ulong, double]
  public static unsafe Vector<T> ConvertToUInt64(Vector<T2> value); // FCVTZU // predicated, MOVPRFX

  /// T: [float, uint], [double, ulong]
  public static unsafe Vector<T> FloatingPointExponentialAccelerator(Vector<T2> value); // FEXPA

  /// T: float, double
  public static unsafe Vector<T> MultiplyAddRotateComplex(Vector<T> addend, Vector<T> left, Vector<T> right, [ConstantExpected] byte rotation); // FCMLA // predicated, MOVPRFX

  public static unsafe Vector<float> MultiplyAddRotateComplexBySelectedScalar(Vector<float> addend, Vector<float> left, Vector<float> right, [ConstantExpected] byte rightIndex, [ConstantExpected] byte rotation); // FCMLA // MOVPRFX

  /// T: float, double
  public static unsafe Vector<T> ReciprocalEstimate(Vector<T> value); // FRECPE

  /// T: float, double
  public static unsafe Vector<T> ReciprocalExponent(Vector<T> value); // FRECPX // predicated, MOVPRFX

  /// T: float, double
  public static unsafe Vector<T> ReciprocalSqrtEstimate(Vector<T> value); // FRSQRTE

  /// T: float, double
  public static unsafe Vector<T> ReciprocalSqrtStep(Vector<T> left, Vector<T> right); // FRSQRTS

  /// T: float, double
  public static unsafe Vector<T> ReciprocalStep(Vector<T> left, Vector<T> right); // FRECPS

  /// T: float, double
  public static unsafe Vector<T> RoundAwayFromZero(Vector<T> value); // FRINTA // predicated, MOVPRFX

  /// T: float, double
  public static unsafe Vector<T> RoundToNearest(Vector<T> value); // FRINTN // predicated, MOVPRFX

  /// T: float, double
  public static unsafe Vector<T> RoundToNegativeInfinity(Vector<T> value); // FRINTM // predicated, MOVPRFX

  /// T: float, double
  public static unsafe Vector<T> RoundToPositiveInfinity(Vector<T> value); // FRINTP // predicated, MOVPRFX

  /// T: float, double
  public static unsafe Vector<T> RoundToZero(Vector<T> value); // FRINTZ // predicated, MOVPRFX

  /// T: [float, int], [double, long]
  public static unsafe Vector<T> Scale(Vector<T> left, Vector<T2> right); // FSCALE // predicated, MOVPRFX

  /// T: float, double
  public static unsafe Vector<T> Sqrt(Vector<T> value); // FSQRT // predicated, MOVPRFX

  /// T: float, double
  public static unsafe Vector<T> TrigonometricMultiplyAddCoefficient(Vector<T> left, Vector<T> right, [ConstantExpected] byte control); // FTMAD // MOVPRFX

  /// T: [float, uint], [double, ulong]
  public static unsafe Vector<T> TrigonometricSelectCoefficient(Vector<T> value, Vector<T2> selector); // FTSSEL

  /// T: [float, uint], [double, ulong]
  public static unsafe Vector<T> TrigonometricStartingValue(Vector<T> value, Vector<T2> sign); // FTSMUL

  /// total method signatures: 26

}


/// Full API
public abstract partial class Sve : AdvSimd /// Feature: FEAT_SVE  Category: fp
{
    /// AddRotateComplex : Complex add with rotate

    /// svfloat32_t svcadd[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2, uint64_t imm_rotation) : "FCADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S, #imm_rotation" or "MOVPRFX Zresult, Zop1; FCADD Zresult.S, Pg/M, Zresult.S, Zop2.S, #imm_rotation"
    /// svfloat32_t svcadd[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2, uint64_t imm_rotation) : "FCADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S, #imm_rotation" or "MOVPRFX Zresult, Zop1; FCADD Zresult.S, Pg/M, Zresult.S, Zop2.S, #imm_rotation"
    /// svfloat32_t svcadd[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2, uint64_t imm_rotation) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; FCADD Zresult.S, Pg/M, Zresult.S, Zop2.S, #imm_rotation"
  public static unsafe Vector<float> AddRotateComplex(Vector<float> left, Vector<float> right, [ConstantExpected] byte rotation);

    /// svfloat64_t svcadd[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2, uint64_t imm_rotation) : "FCADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D, #imm_rotation" or "MOVPRFX Zresult, Zop1; FCADD Zresult.D, Pg/M, Zresult.D, Zop2.D, #imm_rotation"
    /// svfloat64_t svcadd[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2, uint64_t imm_rotation) : "FCADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D, #imm_rotation" or "MOVPRFX Zresult, Zop1; FCADD Zresult.D, Pg/M, Zresult.D, Zop2.D, #imm_rotation"
    /// svfloat64_t svcadd[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2, uint64_t imm_rotation) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; FCADD Zresult.D, Pg/M, Zresult.D, Zop2.D, #imm_rotation"
  public static unsafe Vector<double> AddRotateComplex(Vector<double> left, Vector<double> right, [ConstantExpected] byte rotation);


    /// AddSequentialAcross : Add reduction (strictly-ordered)

    /// float32_t svadda[_f32](svbool_t pg, float32_t initial, svfloat32_t op) : "FADDA Stied, Pg, Stied, Zop.S"
  public static unsafe Vector<float> AddSequentialAcross(Vector<float> initial, Vector<float> value);

    /// float64_t svadda[_f64](svbool_t pg, float64_t initial, svfloat64_t op) : "FADDA Dtied, Pg, Dtied, Zop.D"
  public static unsafe Vector<double> AddSequentialAcross(Vector<double> initial, Vector<double> value);


    /// ConvertToDouble : Floating-point convert

    /// svfloat64_t svcvt_f64[_f32]_m(svfloat64_t inactive, svbool_t pg, svfloat32_t op) : "FCVT Ztied.D, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; FCVT Zresult.D, Pg/M, Zop.S"
    /// svfloat64_t svcvt_f64[_f32]_x(svbool_t pg, svfloat32_t op) : "FCVT Ztied.D, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; FCVT Zresult.D, Pg/M, Zop.S"
    /// svfloat64_t svcvt_f64[_f32]_z(svbool_t pg, svfloat32_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; FCVT Zresult.D, Pg/M, Zop.S"
  public static unsafe Vector<double> ConvertToDouble(Vector<float> value);

    /// svfloat64_t svcvt_f64[_s32]_m(svfloat64_t inactive, svbool_t pg, svint32_t op) : "SCVTF Ztied.D, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; SCVTF Zresult.D, Pg/M, Zop.S"
    /// svfloat64_t svcvt_f64[_s32]_x(svbool_t pg, svint32_t op) : "SCVTF Ztied.D, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; SCVTF Zresult.D, Pg/M, Zop.S"
    /// svfloat64_t svcvt_f64[_s32]_z(svbool_t pg, svint32_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; SCVTF Zresult.D, Pg/M, Zop.S"
  public static unsafe Vector<double> ConvertToDouble(Vector<int> value);

    /// svfloat64_t svcvt_f64[_s64]_m(svfloat64_t inactive, svbool_t pg, svint64_t op) : "SCVTF Ztied.D, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; SCVTF Zresult.D, Pg/M, Zop.D"
    /// svfloat64_t svcvt_f64[_s64]_x(svbool_t pg, svint64_t op) : "SCVTF Ztied.D, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; SCVTF Zresult.D, Pg/M, Zop.D"
    /// svfloat64_t svcvt_f64[_s64]_z(svbool_t pg, svint64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; SCVTF Zresult.D, Pg/M, Zop.D"
  public static unsafe Vector<double> ConvertToDouble(Vector<long> value);

    /// svfloat64_t svcvt_f64[_u32]_m(svfloat64_t inactive, svbool_t pg, svuint32_t op) : "UCVTF Ztied.D, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; UCVTF Zresult.D, Pg/M, Zop.S"
    /// svfloat64_t svcvt_f64[_u32]_x(svbool_t pg, svuint32_t op) : "UCVTF Ztied.D, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; UCVTF Zresult.D, Pg/M, Zop.S"
    /// svfloat64_t svcvt_f64[_u32]_z(svbool_t pg, svuint32_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; UCVTF Zresult.D, Pg/M, Zop.S"
  public static unsafe Vector<double> ConvertToDouble(Vector<uint> value);

    /// svfloat64_t svcvt_f64[_u64]_m(svfloat64_t inactive, svbool_t pg, svuint64_t op) : "UCVTF Ztied.D, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; UCVTF Zresult.D, Pg/M, Zop.D"
    /// svfloat64_t svcvt_f64[_u64]_x(svbool_t pg, svuint64_t op) : "UCVTF Ztied.D, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; UCVTF Zresult.D, Pg/M, Zop.D"
    /// svfloat64_t svcvt_f64[_u64]_z(svbool_t pg, svuint64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; UCVTF Zresult.D, Pg/M, Zop.D"
  public static unsafe Vector<double> ConvertToDouble(Vector<ulong> value);


    /// ConvertToInt32 : Floating-point convert

    /// svint32_t svcvt_s32[_f32]_m(svint32_t inactive, svbool_t pg, svfloat32_t op) : "FCVTZS Ztied.S, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; FCVTZS Zresult.S, Pg/M, Zop.S"
    /// svint32_t svcvt_s32[_f32]_x(svbool_t pg, svfloat32_t op) : "FCVTZS Ztied.S, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; FCVTZS Zresult.S, Pg/M, Zop.S"
    /// svint32_t svcvt_s32[_f32]_z(svbool_t pg, svfloat32_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; FCVTZS Zresult.S, Pg/M, Zop.S"
  public static unsafe Vector<int> ConvertToInt32(Vector<float> value);

    /// svint32_t svcvt_s32[_f64]_m(svint32_t inactive, svbool_t pg, svfloat64_t op) : "FCVTZS Ztied.S, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; FCVTZS Zresult.S, Pg/M, Zop.D"
    /// svint32_t svcvt_s32[_f64]_x(svbool_t pg, svfloat64_t op) : "FCVTZS Ztied.S, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; FCVTZS Zresult.S, Pg/M, Zop.D"
    /// svint32_t svcvt_s32[_f64]_z(svbool_t pg, svfloat64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; FCVTZS Zresult.S, Pg/M, Zop.D"
  public static unsafe Vector<int> ConvertToInt32(Vector<double> value);


    /// ConvertToInt64 : Floating-point convert

    /// svint64_t svcvt_s64[_f32]_m(svint64_t inactive, svbool_t pg, svfloat32_t op) : "FCVTZS Ztied.D, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; FCVTZS Zresult.D, Pg/M, Zop.S"
    /// svint64_t svcvt_s64[_f32]_x(svbool_t pg, svfloat32_t op) : "FCVTZS Ztied.D, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; FCVTZS Zresult.D, Pg/M, Zop.S"
    /// svint64_t svcvt_s64[_f32]_z(svbool_t pg, svfloat32_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; FCVTZS Zresult.D, Pg/M, Zop.S"
  public static unsafe Vector<long> ConvertToInt64(Vector<float> value);

    /// svint64_t svcvt_s64[_f64]_m(svint64_t inactive, svbool_t pg, svfloat64_t op) : "FCVTZS Ztied.D, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; FCVTZS Zresult.D, Pg/M, Zop.D"
    /// svint64_t svcvt_s64[_f64]_x(svbool_t pg, svfloat64_t op) : "FCVTZS Ztied.D, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; FCVTZS Zresult.D, Pg/M, Zop.D"
    /// svint64_t svcvt_s64[_f64]_z(svbool_t pg, svfloat64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; FCVTZS Zresult.D, Pg/M, Zop.D"
  public static unsafe Vector<long> ConvertToInt64(Vector<double> value);


    /// ConvertToSingle : Floating-point convert

    /// svfloat32_t svcvt_f32[_f64]_m(svfloat32_t inactive, svbool_t pg, svfloat64_t op) : "FCVT Ztied.S, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; FCVT Zresult.S, Pg/M, Zop.D"
    /// svfloat32_t svcvt_f32[_f64]_x(svbool_t pg, svfloat64_t op) : "FCVT Ztied.S, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; FCVT Zresult.S, Pg/M, Zop.D"
    /// svfloat32_t svcvt_f32[_f64]_z(svbool_t pg, svfloat64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; FCVT Zresult.S, Pg/M, Zop.D"
  public static unsafe Vector<float> ConvertToSingle(Vector<double> value);

    /// svfloat32_t svcvt_f32[_s32]_m(svfloat32_t inactive, svbool_t pg, svint32_t op) : "SCVTF Ztied.S, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; SCVTF Zresult.S, Pg/M, Zop.S"
    /// svfloat32_t svcvt_f32[_s32]_x(svbool_t pg, svint32_t op) : "SCVTF Ztied.S, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; SCVTF Zresult.S, Pg/M, Zop.S"
    /// svfloat32_t svcvt_f32[_s32]_z(svbool_t pg, svint32_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; SCVTF Zresult.S, Pg/M, Zop.S"
  public static unsafe Vector<float> ConvertToSingle(Vector<int> value);

    /// svfloat32_t svcvt_f32[_s64]_m(svfloat32_t inactive, svbool_t pg, svint64_t op) : "SCVTF Ztied.S, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; SCVTF Zresult.S, Pg/M, Zop.D"
    /// svfloat32_t svcvt_f32[_s64]_x(svbool_t pg, svint64_t op) : "SCVTF Ztied.S, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; SCVTF Zresult.S, Pg/M, Zop.D"
    /// svfloat32_t svcvt_f32[_s64]_z(svbool_t pg, svint64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; SCVTF Zresult.S, Pg/M, Zop.D"
  public static unsafe Vector<float> ConvertToSingle(Vector<long> value);

    /// svfloat32_t svcvt_f32[_u32]_m(svfloat32_t inactive, svbool_t pg, svuint32_t op) : "UCVTF Ztied.S, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; UCVTF Zresult.S, Pg/M, Zop.S"
    /// svfloat32_t svcvt_f32[_u32]_x(svbool_t pg, svuint32_t op) : "UCVTF Ztied.S, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; UCVTF Zresult.S, Pg/M, Zop.S"
    /// svfloat32_t svcvt_f32[_u32]_z(svbool_t pg, svuint32_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; UCVTF Zresult.S, Pg/M, Zop.S"
  public static unsafe Vector<float> ConvertToSingle(Vector<uint> value);

    /// svfloat32_t svcvt_f32[_u64]_m(svfloat32_t inactive, svbool_t pg, svuint64_t op) : "UCVTF Ztied.S, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; UCVTF Zresult.S, Pg/M, Zop.D"
    /// svfloat32_t svcvt_f32[_u64]_x(svbool_t pg, svuint64_t op) : "UCVTF Ztied.S, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; UCVTF Zresult.S, Pg/M, Zop.D"
    /// svfloat32_t svcvt_f32[_u64]_z(svbool_t pg, svuint64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; UCVTF Zresult.S, Pg/M, Zop.D"
  public static unsafe Vector<float> ConvertToSingle(Vector<ulong> value);


    /// ConvertToUInt32 : Floating-point convert

    /// svuint32_t svcvt_u32[_f32]_m(svuint32_t inactive, svbool_t pg, svfloat32_t op) : "FCVTZU Ztied.S, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; FCVTZU Zresult.S, Pg/M, Zop.S"
    /// svuint32_t svcvt_u32[_f32]_x(svbool_t pg, svfloat32_t op) : "FCVTZU Ztied.S, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; FCVTZU Zresult.S, Pg/M, Zop.S"
    /// svuint32_t svcvt_u32[_f32]_z(svbool_t pg, svfloat32_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; FCVTZU Zresult.S, Pg/M, Zop.S"
  public static unsafe Vector<uint> ConvertToUInt32(Vector<float> value);

    /// svuint32_t svcvt_u32[_f64]_m(svuint32_t inactive, svbool_t pg, svfloat64_t op) : "FCVTZU Ztied.S, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; FCVTZU Zresult.S, Pg/M, Zop.D"
    /// svuint32_t svcvt_u32[_f64]_x(svbool_t pg, svfloat64_t op) : "FCVTZU Ztied.S, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; FCVTZU Zresult.S, Pg/M, Zop.D"
    /// svuint32_t svcvt_u32[_f64]_z(svbool_t pg, svfloat64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; FCVTZU Zresult.S, Pg/M, Zop.D"
  public static unsafe Vector<uint> ConvertToUInt32(Vector<double> value);


    /// ConvertToUInt64 : Floating-point convert

    /// svuint64_t svcvt_u64[_f32]_m(svuint64_t inactive, svbool_t pg, svfloat32_t op) : "FCVTZU Ztied.D, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; FCVTZU Zresult.D, Pg/M, Zop.S"
    /// svuint64_t svcvt_u64[_f32]_x(svbool_t pg, svfloat32_t op) : "FCVTZU Ztied.D, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; FCVTZU Zresult.D, Pg/M, Zop.S"
    /// svuint64_t svcvt_u64[_f32]_z(svbool_t pg, svfloat32_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; FCVTZU Zresult.D, Pg/M, Zop.S"
  public static unsafe Vector<ulong> ConvertToUInt64(Vector<float> value);

    /// svuint64_t svcvt_u64[_f64]_m(svuint64_t inactive, svbool_t pg, svfloat64_t op) : "FCVTZU Ztied.D, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; FCVTZU Zresult.D, Pg/M, Zop.D"
    /// svuint64_t svcvt_u64[_f64]_x(svbool_t pg, svfloat64_t op) : "FCVTZU Ztied.D, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; FCVTZU Zresult.D, Pg/M, Zop.D"
    /// svuint64_t svcvt_u64[_f64]_z(svbool_t pg, svfloat64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; FCVTZU Zresult.D, Pg/M, Zop.D"
  public static unsafe Vector<ulong> ConvertToUInt64(Vector<double> value);


    /// FloatingPointExponentialAccelerator : Floating-point exponential accelerator

    /// svfloat32_t svexpa[_f32](svuint32_t op) : "FEXPA Zresult.S, Zop.S"
  public static unsafe Vector<float> FloatingPointExponentialAccelerator(Vector<uint> value);

    /// svfloat64_t svexpa[_f64](svuint64_t op) : "FEXPA Zresult.D, Zop.D"
  public static unsafe Vector<double> FloatingPointExponentialAccelerator(Vector<ulong> value);


    /// MultiplyAddRotateComplex : Complex multiply-add with rotate

    /// svfloat32_t svcmla[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3, uint64_t imm_rotation) : "FCMLA Ztied1.S, Pg/M, Zop2.S, Zop3.S, #imm_rotation" or "MOVPRFX Zresult, Zop1; FCMLA Zresult.S, Pg/M, Zop2.S, Zop3.S, #imm_rotation"
    /// svfloat32_t svcmla[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3, uint64_t imm_rotation) : "FCMLA Ztied1.S, Pg/M, Zop2.S, Zop3.S, #imm_rotation" or "MOVPRFX Zresult, Zop1; FCMLA Zresult.S, Pg/M, Zop2.S, Zop3.S, #imm_rotation"
    /// svfloat32_t svcmla[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3, uint64_t imm_rotation) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; FCMLA Zresult.S, Pg/M, Zop2.S, Zop3.S, #imm_rotation"
  public static unsafe Vector<float> MultiplyAddRotateComplex(Vector<float> addend, Vector<float> left, Vector<float> right, [ConstantExpected] byte rotation);

    /// svfloat64_t svcmla[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3, uint64_t imm_rotation) : "FCMLA Ztied1.D, Pg/M, Zop2.D, Zop3.D, #imm_rotation" or "MOVPRFX Zresult, Zop1; FCMLA Zresult.D, Pg/M, Zop2.D, Zop3.D, #imm_rotation"
    /// svfloat64_t svcmla[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3, uint64_t imm_rotation) : "FCMLA Ztied1.D, Pg/M, Zop2.D, Zop3.D, #imm_rotation" or "MOVPRFX Zresult, Zop1; FCMLA Zresult.D, Pg/M, Zop2.D, Zop3.D, #imm_rotation"
    /// svfloat64_t svcmla[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3, uint64_t imm_rotation) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; FCMLA Zresult.D, Pg/M, Zop2.D, Zop3.D, #imm_rotation"
  public static unsafe Vector<double> MultiplyAddRotateComplex(Vector<double> addend, Vector<double> left, Vector<double> right, [ConstantExpected] byte rotation);


    /// MultiplyAddRotateComplexBySelectedScalar : Complex multiply-add with rotate

    /// svfloat32_t svcmla_lane[_f32](svfloat32_t op1, svfloat32_t op2, svfloat32_t op3, uint64_t imm_index, uint64_t imm_rotation) : "FCMLA Ztied1.S, Zop2.S, Zop3.S[imm_index], #imm_rotation" or "MOVPRFX Zresult, Zop1; FCMLA Zresult.S, Zop2.S, Zop3.S[imm_index], #imm_rotation"
  public static unsafe Vector<float> MultiplyAddRotateComplexBySelectedScalar(Vector<float> addend, Vector<float> left, Vector<float> right, [ConstantExpected] byte rightIndex, [ConstantExpected] byte rotation);


    /// ReciprocalEstimate : Reciprocal estimate

    /// svfloat32_t svrecpe[_f32](svfloat32_t op) : "FRECPE Zresult.S, Zop.S"
  public static unsafe Vector<float> ReciprocalEstimate(Vector<float> value);

    /// svfloat64_t svrecpe[_f64](svfloat64_t op) : "FRECPE Zresult.D, Zop.D"
  public static unsafe Vector<double> ReciprocalEstimate(Vector<double> value);


    /// ReciprocalExponent : Reciprocal exponent

    /// svfloat32_t svrecpx[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op) : "FRECPX Ztied.S, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; FRECPX Zresult.S, Pg/M, Zop.S"
    /// svfloat32_t svrecpx[_f32]_x(svbool_t pg, svfloat32_t op) : "FRECPX Ztied.S, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; FRECPX Zresult.S, Pg/M, Zop.S"
    /// svfloat32_t svrecpx[_f32]_z(svbool_t pg, svfloat32_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; FRECPX Zresult.S, Pg/M, Zop.S"
  public static unsafe Vector<float> ReciprocalExponent(Vector<float> value);

    /// svfloat64_t svrecpx[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op) : "FRECPX Ztied.D, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; FRECPX Zresult.D, Pg/M, Zop.D"
    /// svfloat64_t svrecpx[_f64]_x(svbool_t pg, svfloat64_t op) : "FRECPX Ztied.D, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; FRECPX Zresult.D, Pg/M, Zop.D"
    /// svfloat64_t svrecpx[_f64]_z(svbool_t pg, svfloat64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; FRECPX Zresult.D, Pg/M, Zop.D"
  public static unsafe Vector<double> ReciprocalExponent(Vector<double> value);


    /// ReciprocalSqrtEstimate : Reciprocal square root estimate

    /// svfloat32_t svrsqrte[_f32](svfloat32_t op) : "FRSQRTE Zresult.S, Zop.S"
  public static unsafe Vector<float> ReciprocalSqrtEstimate(Vector<float> value);

    /// svfloat64_t svrsqrte[_f64](svfloat64_t op) : "FRSQRTE Zresult.D, Zop.D"
  public static unsafe Vector<double> ReciprocalSqrtEstimate(Vector<double> value);


    /// ReciprocalSqrtStep : Reciprocal square root step

    /// svfloat32_t svrsqrts[_f32](svfloat32_t op1, svfloat32_t op2) : "FRSQRTS Zresult.S, Zop1.S, Zop2.S"
  public static unsafe Vector<float> ReciprocalSqrtStep(Vector<float> left, Vector<float> right);

    /// svfloat64_t svrsqrts[_f64](svfloat64_t op1, svfloat64_t op2) : "FRSQRTS Zresult.D, Zop1.D, Zop2.D"
  public static unsafe Vector<double> ReciprocalSqrtStep(Vector<double> left, Vector<double> right);


    /// ReciprocalStep : Reciprocal step

    /// svfloat32_t svrecps[_f32](svfloat32_t op1, svfloat32_t op2) : "FRECPS Zresult.S, Zop1.S, Zop2.S"
  public static unsafe Vector<float> ReciprocalStep(Vector<float> left, Vector<float> right);

    /// svfloat64_t svrecps[_f64](svfloat64_t op1, svfloat64_t op2) : "FRECPS Zresult.D, Zop1.D, Zop2.D"
  public static unsafe Vector<double> ReciprocalStep(Vector<double> left, Vector<double> right);


    /// RoundAwayFromZero : Round to nearest, ties away from zero

    /// svfloat32_t svrinta[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op) : "FRINTA Ztied.S, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; FRINTA Zresult.S, Pg/M, Zop.S"
    /// svfloat32_t svrinta[_f32]_x(svbool_t pg, svfloat32_t op) : "FRINTA Ztied.S, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; FRINTA Zresult.S, Pg/M, Zop.S"
    /// svfloat32_t svrinta[_f32]_z(svbool_t pg, svfloat32_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; FRINTA Zresult.S, Pg/M, Zop.S"
  public static unsafe Vector<float> RoundAwayFromZero(Vector<float> value);

    /// svfloat64_t svrinta[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op) : "FRINTA Ztied.D, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; FRINTA Zresult.D, Pg/M, Zop.D"
    /// svfloat64_t svrinta[_f64]_x(svbool_t pg, svfloat64_t op) : "FRINTA Ztied.D, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; FRINTA Zresult.D, Pg/M, Zop.D"
    /// svfloat64_t svrinta[_f64]_z(svbool_t pg, svfloat64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; FRINTA Zresult.D, Pg/M, Zop.D"
  public static unsafe Vector<double> RoundAwayFromZero(Vector<double> value);


    /// RoundToNearest : Round to nearest, ties to even

    /// svfloat32_t svrintn[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op) : "FRINTN Ztied.S, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; FRINTN Zresult.S, Pg/M, Zop.S"
    /// svfloat32_t svrintn[_f32]_x(svbool_t pg, svfloat32_t op) : "FRINTN Ztied.S, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; FRINTN Zresult.S, Pg/M, Zop.S"
    /// svfloat32_t svrintn[_f32]_z(svbool_t pg, svfloat32_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; FRINTN Zresult.S, Pg/M, Zop.S"
  public static unsafe Vector<float> RoundToNearest(Vector<float> value);

    /// svfloat64_t svrintn[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op) : "FRINTN Ztied.D, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; FRINTN Zresult.D, Pg/M, Zop.D"
    /// svfloat64_t svrintn[_f64]_x(svbool_t pg, svfloat64_t op) : "FRINTN Ztied.D, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; FRINTN Zresult.D, Pg/M, Zop.D"
    /// svfloat64_t svrintn[_f64]_z(svbool_t pg, svfloat64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; FRINTN Zresult.D, Pg/M, Zop.D"
  public static unsafe Vector<double> RoundToNearest(Vector<double> value);


    /// RoundToNegativeInfinity : Round towards -∞

    /// svfloat32_t svrintm[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op) : "FRINTM Ztied.S, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; FRINTM Zresult.S, Pg/M, Zop.S"
    /// svfloat32_t svrintm[_f32]_x(svbool_t pg, svfloat32_t op) : "FRINTM Ztied.S, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; FRINTM Zresult.S, Pg/M, Zop.S"
    /// svfloat32_t svrintm[_f32]_z(svbool_t pg, svfloat32_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; FRINTM Zresult.S, Pg/M, Zop.S"
  public static unsafe Vector<float> RoundToNegativeInfinity(Vector<float> value);

    /// svfloat64_t svrintm[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op) : "FRINTM Ztied.D, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; FRINTM Zresult.D, Pg/M, Zop.D"
    /// svfloat64_t svrintm[_f64]_x(svbool_t pg, svfloat64_t op) : "FRINTM Ztied.D, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; FRINTM Zresult.D, Pg/M, Zop.D"
    /// svfloat64_t svrintm[_f64]_z(svbool_t pg, svfloat64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; FRINTM Zresult.D, Pg/M, Zop.D"
  public static unsafe Vector<double> RoundToNegativeInfinity(Vector<double> value);


    /// RoundToPositiveInfinity : Round towards +∞

    /// svfloat32_t svrintp[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op) : "FRINTP Ztied.S, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; FRINTP Zresult.S, Pg/M, Zop.S"
    /// svfloat32_t svrintp[_f32]_x(svbool_t pg, svfloat32_t op) : "FRINTP Ztied.S, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; FRINTP Zresult.S, Pg/M, Zop.S"
    /// svfloat32_t svrintp[_f32]_z(svbool_t pg, svfloat32_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; FRINTP Zresult.S, Pg/M, Zop.S"
  public static unsafe Vector<float> RoundToPositiveInfinity(Vector<float> value);

    /// svfloat64_t svrintp[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op) : "FRINTP Ztied.D, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; FRINTP Zresult.D, Pg/M, Zop.D"
    /// svfloat64_t svrintp[_f64]_x(svbool_t pg, svfloat64_t op) : "FRINTP Ztied.D, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; FRINTP Zresult.D, Pg/M, Zop.D"
    /// svfloat64_t svrintp[_f64]_z(svbool_t pg, svfloat64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; FRINTP Zresult.D, Pg/M, Zop.D"
  public static unsafe Vector<double> RoundToPositiveInfinity(Vector<double> value);


    /// RoundToZero : Round towards zero

    /// svfloat32_t svrintz[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op) : "FRINTZ Ztied.S, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; FRINTZ Zresult.S, Pg/M, Zop.S"
    /// svfloat32_t svrintz[_f32]_x(svbool_t pg, svfloat32_t op) : "FRINTZ Ztied.S, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; FRINTZ Zresult.S, Pg/M, Zop.S"
    /// svfloat32_t svrintz[_f32]_z(svbool_t pg, svfloat32_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; FRINTZ Zresult.S, Pg/M, Zop.S"
  public static unsafe Vector<float> RoundToZero(Vector<float> value);

    /// svfloat64_t svrintz[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op) : "FRINTZ Ztied.D, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; FRINTZ Zresult.D, Pg/M, Zop.D"
    /// svfloat64_t svrintz[_f64]_x(svbool_t pg, svfloat64_t op) : "FRINTZ Ztied.D, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; FRINTZ Zresult.D, Pg/M, Zop.D"
    /// svfloat64_t svrintz[_f64]_z(svbool_t pg, svfloat64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; FRINTZ Zresult.D, Pg/M, Zop.D"
  public static unsafe Vector<double> RoundToZero(Vector<double> value);


    /// Scale : Adjust exponent

    /// svfloat32_t svscale[_f32]_m(svbool_t pg, svfloat32_t op1, svint32_t op2) : "FSCALE Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; FSCALE Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svfloat32_t svscale[_f32]_x(svbool_t pg, svfloat32_t op1, svint32_t op2) : "FSCALE Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; FSCALE Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svfloat32_t svscale[_f32]_z(svbool_t pg, svfloat32_t op1, svint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; FSCALE Zresult.S, Pg/M, Zresult.S, Zop2.S"
  public static unsafe Vector<float> Scale(Vector<float> left, Vector<int> right);

    /// svfloat64_t svscale[_f64]_m(svbool_t pg, svfloat64_t op1, svint64_t op2) : "FSCALE Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; FSCALE Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svfloat64_t svscale[_f64]_x(svbool_t pg, svfloat64_t op1, svint64_t op2) : "FSCALE Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; FSCALE Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svfloat64_t svscale[_f64]_z(svbool_t pg, svfloat64_t op1, svint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; FSCALE Zresult.D, Pg/M, Zresult.D, Zop2.D"
  public static unsafe Vector<double> Scale(Vector<double> left, Vector<long> right);


    /// Sqrt : Square root

    /// svfloat32_t svsqrt[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op) : "FSQRT Ztied.S, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; FSQRT Zresult.S, Pg/M, Zop.S"
    /// svfloat32_t svsqrt[_f32]_x(svbool_t pg, svfloat32_t op) : "FSQRT Ztied.S, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; FSQRT Zresult.S, Pg/M, Zop.S"
    /// svfloat32_t svsqrt[_f32]_z(svbool_t pg, svfloat32_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; FSQRT Zresult.S, Pg/M, Zop.S"
  public static unsafe Vector<float> Sqrt(Vector<float> value);

    /// svfloat64_t svsqrt[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op) : "FSQRT Ztied.D, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; FSQRT Zresult.D, Pg/M, Zop.D"
    /// svfloat64_t svsqrt[_f64]_x(svbool_t pg, svfloat64_t op) : "FSQRT Ztied.D, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; FSQRT Zresult.D, Pg/M, Zop.D"
    /// svfloat64_t svsqrt[_f64]_z(svbool_t pg, svfloat64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; FSQRT Zresult.D, Pg/M, Zop.D"
  public static unsafe Vector<double> Sqrt(Vector<double> value);


    /// TrigonometricMultiplyAddCoefficient : Trigonometric multiply-add coefficient

    /// svfloat32_t svtmad[_f32](svfloat32_t op1, svfloat32_t op2, uint64_t imm3) : "FTMAD Ztied1.S, Ztied1.S, Zop2.S, #imm3" or "MOVPRFX Zresult, Zop1; FTMAD Zresult.S, Zresult.S, Zop2.S, #imm3"
  public static unsafe Vector<float> TrigonometricMultiplyAddCoefficient(Vector<float> left, Vector<float> right, [ConstantExpected] byte control);

    /// svfloat64_t svtmad[_f64](svfloat64_t op1, svfloat64_t op2, uint64_t imm3) : "FTMAD Ztied1.D, Ztied1.D, Zop2.D, #imm3" or "MOVPRFX Zresult, Zop1; FTMAD Zresult.D, Zresult.D, Zop2.D, #imm3"
  public static unsafe Vector<double> TrigonometricMultiplyAddCoefficient(Vector<double> left, Vector<double> right, [ConstantExpected] byte control);


    /// TrigonometricSelectCoefficient : Trigonometric select coefficient

    /// svfloat32_t svtssel[_f32](svfloat32_t op1, svuint32_t op2) : "FTSSEL Zresult.S, Zop1.S, Zop2.S"
  public static unsafe Vector<float> TrigonometricSelectCoefficient(Vector<float> value, Vector<uint> selector);

    /// svfloat64_t svtssel[_f64](svfloat64_t op1, svuint64_t op2) : "FTSSEL Zresult.D, Zop1.D, Zop2.D"
  public static unsafe Vector<double> TrigonometricSelectCoefficient(Vector<double> value, Vector<ulong> selector);


    /// TrigonometricStartingValue : Trigonometric starting value

    /// svfloat32_t svtsmul[_f32](svfloat32_t op1, svuint32_t op2) : "FTSMUL Zresult.S, Zop1.S, Zop2.S"
  public static unsafe Vector<float> TrigonometricStartingValue(Vector<float> value, Vector<uint> sign);

    /// svfloat64_t svtsmul[_f64](svfloat64_t op1, svuint64_t op2) : "FTSMUL Zresult.D, Zop1.D, Zop2.D"
  public static unsafe Vector<double> TrigonometricStartingValue(Vector<double> value, Vector<ulong> sign);


  /// total method signatures: 57
  /// total method names:      28
}


  /// Rejected:
  ///   public static unsafe Vector<float> RoundUsingCurrentRoundingModeExact(Vector<float> value); // svrintx[_f32]_m or svrintx[_f32]_x or svrintx[_f32]_z
  ///   public static unsafe Vector<double> RoundUsingCurrentRoundingModeExact(Vector<double> value); // svrintx[_f64]_m or svrintx[_f64]_x or svrintx[_f64]_z
  ///   public static unsafe Vector<float> RoundUsingCurrentRoundingModeInexact(Vector<float> value); // svrinti[_f32]_m or svrinti[_f32]_x or svrinti[_f32]_z
  ///   public static unsafe Vector<double> RoundUsingCurrentRoundingModeInexact(Vector<double> value); // svrinti[_f64]_m or svrinti[_f64]_x or svrinti[_f64]_z
  ///   public static unsafe Vector<float> Scale(Vector<float> left, int right); // svscale[_n_f32]_m or svscale[_n_f32]_x or svscale[_n_f32]_z
  ///   public static unsafe Vector<double> Scale(Vector<double> left, long right); // svscale[_n_f64]_m or svscale[_n_f64]_x or svscale[_n_f64]_z
  ///   Total Rejected: 6

  /// Total ACLE covered across API:      151

