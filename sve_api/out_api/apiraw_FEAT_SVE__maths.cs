namespace System.Runtime.Intrinsics.Arm;

/// VectorT Summary
public abstract partial class Sve : AdvSimd /// Feature: FEAT_SVE  Category: maths
{

  /// T: float, double, sbyte, short, int, long
  public static unsafe Vector<T> Abs(Vector<T> value); // FABS or ABS // predicated, MOVPRFX

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> AbsoluteDifference(Vector<T> left, Vector<T> right); // FABD or SABD or UABD // predicated, MOVPRFX

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> Add(Vector<T> left, Vector<T> right); // FADD or ADD // predicated, MOVPRFX

  /// T: float, double, long, ulong
  public static unsafe Vector<T> AddAcross(Vector<T> value); // FADDV or UADDV // predicated

  /// T: [long, sbyte], [long, short], [long, int], [ulong, byte], [ulong, ushort], [ulong, uint]
  public static unsafe Vector<T> AddAcross(Vector<T2> value); // SADDV or UADDV // predicated

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> AddSaturate(Vector<T> left, Vector<T> right); // SQADD or UQADD

  /// T: float, double, int, long, uint, ulong
  public static unsafe Vector<T> Divide(Vector<T> left, Vector<T> right); // FDIV or SDIV or UDIV or FDIVR or SDIVR or UDIVR // predicated, MOVPRFX

  /// T: [int, sbyte], [long, short], [uint, byte], [ulong, ushort]
  public static unsafe Vector<T> DotProduct(Vector<T> addend, Vector<T2> left, Vector<T2> right); // SDOT or UDOT // MOVPRFX

  /// T: [int, sbyte], [long, short], [uint, byte], [ulong, ushort]
  public static unsafe Vector<T> DotProductBySelectedScalar(Vector<T> addend, Vector<T2> left, Vector<T2> right, [ConstantExpected] byte rightIndex); // SDOT or UDOT // MOVPRFX

  /// T: float, double
  public static unsafe Vector<T> FusedMultiplyAdd(Vector<T> addend, Vector<T> left, Vector<T> right); // FMLA or FMAD // predicated, MOVPRFX

  /// T: float, double
  public static unsafe Vector<T> FusedMultiplyAddBySelectedScalar(Vector<T> addend, Vector<T> left, Vector<T> right, [ConstantExpected] byte rightIndex); // FMLA // MOVPRFX

  /// T: float, double
  public static unsafe Vector<T> FusedMultiplyAddNegated(Vector<T> addend, Vector<T> left, Vector<T> right); // FNMLA or FNMAD // predicated, MOVPRFX

  /// T: float, double
  public static unsafe Vector<T> FusedMultiplySubtract(Vector<T> minuend, Vector<T> left, Vector<T> right); // FMLS or FMSB // predicated, MOVPRFX

  /// T: float, double
  public static unsafe Vector<T> FusedMultiplySubtractBySelectedScalar(Vector<T> minuend, Vector<T> left, Vector<T> right, [ConstantExpected] byte rightIndex); // FMLS // MOVPRFX

  /// T: float, double
  public static unsafe Vector<T> FusedMultiplySubtractNegated(Vector<T> minuend, Vector<T> left, Vector<T> right); // FNMLS or FNMSB // predicated, MOVPRFX

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> Max(Vector<T> left, Vector<T> right); // FMAX or SMAX or UMAX // predicated, MOVPRFX

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> MaxAcross(Vector<T> value); // FMAXV or SMAXV or UMAXV // predicated

  /// T: float, double
  public static unsafe Vector<T> MaxNumber(Vector<T> left, Vector<T> right); // FMAXNM // predicated, MOVPRFX

  /// T: float, double
  public static unsafe Vector<T> MaxNumberAcross(Vector<T> value); // FMAXNMV // predicated

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> Min(Vector<T> left, Vector<T> right); // FMIN or SMIN or UMIN // predicated, MOVPRFX

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> MinAcross(Vector<T> value); // FMINV or SMINV or UMINV // predicated

  /// T: float, double
  public static unsafe Vector<T> MinNumber(Vector<T> left, Vector<T> right); // FMINNM // predicated, MOVPRFX

  /// T: float, double
  public static unsafe Vector<T> MinNumberAcross(Vector<T> value); // FMINNMV // predicated

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> Multiply(Vector<T> left, Vector<T> right); // FMUL or MUL // predicated, MOVPRFX

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> MultiplyAdd(Vector<T> addend, Vector<T> left, Vector<T> right); // MLA or MAD // predicated, MOVPRFX

  /// T: float, double
  public static unsafe Vector<T> MultiplyBySelectedScalar(Vector<T> left, Vector<T> right, [ConstantExpected] byte rightIndex); // FMUL

  /// T: float, double
  public static unsafe Vector<T> MultiplyExtended(Vector<T> left, Vector<T> right); // FMULX // predicated, MOVPRFX

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> MultiplySubtract(Vector<T> minuend, Vector<T> left, Vector<T> right); // MLS or MSB // predicated, MOVPRFX

  /// T: float, double, sbyte, short, int, long
  public static unsafe Vector<T> Negate(Vector<T> value); // FNEG or NEG // predicated, MOVPRFX

  /// T: int, long
  public static unsafe Vector<T> SignExtend16(Vector<T> value); // SXTH // predicated, MOVPRFX

  public static unsafe Vector<long> SignExtend32(Vector<long> value); // SXTW // predicated, MOVPRFX

  /// T: short, int, long
  public static unsafe Vector<T> SignExtend8(Vector<T> value); // SXTB // predicated, MOVPRFX

  /// T: [short, sbyte], [int, short], [long, int]
  public static unsafe Vector<T> SignExtendWideningLower(Vector<T2> value); // SUNPKLO

  /// T: [short, sbyte], [int, short], [long, int]
  public static unsafe Vector<T> SignExtendWideningUpper(Vector<T2> value); // SUNPKHI

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> Subtract(Vector<T> left, Vector<T> right); // FSUB or SUB or FSUBR or SUBR // predicated, MOVPRFX

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> SubtractSaturate(Vector<T> left, Vector<T> right); // SQSUB or UQSUB

  /// T: uint, ulong
  public static unsafe Vector<T> ZeroExtend16(Vector<T> value); // UXTH or AND // predicated, MOVPRFX

  public static unsafe Vector<ulong> ZeroExtend32(Vector<ulong> value); // UXTW or AND // predicated, MOVPRFX

  /// T: ushort, uint, ulong
  public static unsafe Vector<T> ZeroExtend8(Vector<T> value); // UXTB or AND // predicated, MOVPRFX

  /// T: [ushort, byte], [uint, ushort], [ulong, uint]
  public static unsafe Vector<T> ZeroExtendWideningLower(Vector<T2> value); // UUNPKLO or PUNPKLO

  /// T: [ushort, byte], [uint, ushort], [ulong, uint]
  public static unsafe Vector<T> ZeroExtendWideningUpper(Vector<T2> value); // UUNPKHI or PUNPKHI

  /// total method signatures: 41


  /// Optional Entries:

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> MultiplyReturningHighHalf(Vector<T> left, Vector<T> right); // SMULH or UMULH // predicated, MOVPRFX

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> MultiplyReturningHighHalf(Vector<T> left, T right); // SMULH or UMULH // predicated, MOVPRFX

  /// total optional method signatures: 2

}


/// Full API
public abstract partial class Sve : AdvSimd /// Feature: FEAT_SVE  Category: maths
{
    /// Abs : Absolute value

    /// svfloat32_t svabs[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op) : "FABS Ztied.S, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; FABS Zresult.S, Pg/M, Zop.S"
    /// svfloat32_t svabs[_f32]_x(svbool_t pg, svfloat32_t op) : "FABS Ztied.S, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; FABS Zresult.S, Pg/M, Zop.S"
    /// svfloat32_t svabs[_f32]_z(svbool_t pg, svfloat32_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; FABS Zresult.S, Pg/M, Zop.S"
  public static unsafe Vector<float> Abs(Vector<float> value);

    /// svfloat64_t svabs[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op) : "FABS Ztied.D, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; FABS Zresult.D, Pg/M, Zop.D"
    /// svfloat64_t svabs[_f64]_x(svbool_t pg, svfloat64_t op) : "FABS Ztied.D, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; FABS Zresult.D, Pg/M, Zop.D"
    /// svfloat64_t svabs[_f64]_z(svbool_t pg, svfloat64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; FABS Zresult.D, Pg/M, Zop.D"
  public static unsafe Vector<double> Abs(Vector<double> value);

    /// svint8_t svabs[_s8]_m(svint8_t inactive, svbool_t pg, svint8_t op) : "ABS Ztied.B, Pg/M, Zop.B" or "MOVPRFX Zresult, Zinactive; ABS Zresult.B, Pg/M, Zop.B"
    /// svint8_t svabs[_s8]_x(svbool_t pg, svint8_t op) : "ABS Ztied.B, Pg/M, Ztied.B" or "MOVPRFX Zresult, Zop; ABS Zresult.B, Pg/M, Zop.B"
    /// svint8_t svabs[_s8]_z(svbool_t pg, svint8_t op) : "MOVPRFX Zresult.B, Pg/Z, Zop.B; ABS Zresult.B, Pg/M, Zop.B"
  public static unsafe Vector<sbyte> Abs(Vector<sbyte> value);

    /// svint16_t svabs[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op) : "ABS Ztied.H, Pg/M, Zop.H" or "MOVPRFX Zresult, Zinactive; ABS Zresult.H, Pg/M, Zop.H"
    /// svint16_t svabs[_s16]_x(svbool_t pg, svint16_t op) : "ABS Ztied.H, Pg/M, Ztied.H" or "MOVPRFX Zresult, Zop; ABS Zresult.H, Pg/M, Zop.H"
    /// svint16_t svabs[_s16]_z(svbool_t pg, svint16_t op) : "MOVPRFX Zresult.H, Pg/Z, Zop.H; ABS Zresult.H, Pg/M, Zop.H"
  public static unsafe Vector<short> Abs(Vector<short> value);

    /// svint32_t svabs[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op) : "ABS Ztied.S, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; ABS Zresult.S, Pg/M, Zop.S"
    /// svint32_t svabs[_s32]_x(svbool_t pg, svint32_t op) : "ABS Ztied.S, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; ABS Zresult.S, Pg/M, Zop.S"
    /// svint32_t svabs[_s32]_z(svbool_t pg, svint32_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; ABS Zresult.S, Pg/M, Zop.S"
  public static unsafe Vector<int> Abs(Vector<int> value);

    /// svint64_t svabs[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op) : "ABS Ztied.D, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; ABS Zresult.D, Pg/M, Zop.D"
    /// svint64_t svabs[_s64]_x(svbool_t pg, svint64_t op) : "ABS Ztied.D, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; ABS Zresult.D, Pg/M, Zop.D"
    /// svint64_t svabs[_s64]_z(svbool_t pg, svint64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; ABS Zresult.D, Pg/M, Zop.D"
  public static unsafe Vector<long> Abs(Vector<long> value);


    /// AbsoluteDifference : Absolute difference

    /// svfloat32_t svabd[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "FABD Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; FABD Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svfloat32_t svabd[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "FABD Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "FABD Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "MOVPRFX Zresult, Zop1; FABD Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svfloat32_t svabd[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; FABD Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; FABD Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<float> AbsoluteDifference(Vector<float> left, Vector<float> right);

    /// svfloat64_t svabd[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "FABD Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; FABD Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svfloat64_t svabd[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "FABD Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "FABD Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "MOVPRFX Zresult, Zop1; FABD Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svfloat64_t svabd[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; FABD Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; FABD Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<double> AbsoluteDifference(Vector<double> left, Vector<double> right);

    /// svint8_t svabd[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2) : "SABD Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; SABD Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svint8_t svabd[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2) : "SABD Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "SABD Ztied2.B, Pg/M, Ztied2.B, Zop1.B" or "MOVPRFX Zresult, Zop1; SABD Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svint8_t svabd[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; SABD Zresult.B, Pg/M, Zresult.B, Zop2.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; SABD Zresult.B, Pg/M, Zresult.B, Zop1.B"
  public static unsafe Vector<sbyte> AbsoluteDifference(Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t svabd[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2) : "SABD Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; SABD Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svint16_t svabd[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2) : "SABD Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "SABD Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "MOVPRFX Zresult, Zop1; SABD Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svint16_t svabd[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; SABD Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; SABD Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<short> AbsoluteDifference(Vector<short> left, Vector<short> right);

    /// svint32_t svabd[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2) : "SABD Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; SABD Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svint32_t svabd[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2) : "SABD Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "SABD Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "MOVPRFX Zresult, Zop1; SABD Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svint32_t svabd[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; SABD Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; SABD Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<int> AbsoluteDifference(Vector<int> left, Vector<int> right);

    /// svint64_t svabd[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2) : "SABD Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; SABD Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svint64_t svabd[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2) : "SABD Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "SABD Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "MOVPRFX Zresult, Zop1; SABD Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svint64_t svabd[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; SABD Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; SABD Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<long> AbsoluteDifference(Vector<long> left, Vector<long> right);

    /// svuint8_t svabd[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2) : "UABD Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; UABD Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svuint8_t svabd[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2) : "UABD Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "UABD Ztied2.B, Pg/M, Ztied2.B, Zop1.B" or "MOVPRFX Zresult, Zop1; UABD Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svuint8_t svabd[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; UABD Zresult.B, Pg/M, Zresult.B, Zop2.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; UABD Zresult.B, Pg/M, Zresult.B, Zop1.B"
  public static unsafe Vector<byte> AbsoluteDifference(Vector<byte> left, Vector<byte> right);

    /// svuint16_t svabd[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2) : "UABD Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; UABD Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svuint16_t svabd[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2) : "UABD Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "UABD Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "MOVPRFX Zresult, Zop1; UABD Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svuint16_t svabd[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; UABD Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; UABD Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<ushort> AbsoluteDifference(Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svabd[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2) : "UABD Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; UABD Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svuint32_t svabd[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2) : "UABD Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "UABD Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "MOVPRFX Zresult, Zop1; UABD Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svuint32_t svabd[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; UABD Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; UABD Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<uint> AbsoluteDifference(Vector<uint> left, Vector<uint> right);

    /// svuint64_t svabd[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2) : "UABD Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; UABD Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svuint64_t svabd[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2) : "UABD Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "UABD Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "MOVPRFX Zresult, Zop1; UABD Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svuint64_t svabd[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; UABD Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; UABD Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<ulong> AbsoluteDifference(Vector<ulong> left, Vector<ulong> right);


    /// Add : Add

    /// svfloat32_t svadd[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "FADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; FADD Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svfloat32_t svadd[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "FADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "FADD Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "FADD Zresult.S, Zop1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; FADD Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svfloat32_t svadd[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; FADD Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; FADD Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<float> Add(Vector<float> left, Vector<float> right);

    /// svfloat64_t svadd[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "FADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; FADD Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svfloat64_t svadd[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "FADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "FADD Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "FADD Zresult.D, Zop1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; FADD Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svfloat64_t svadd[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; FADD Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; FADD Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<double> Add(Vector<double> left, Vector<double> right);

    /// svint8_t svadd[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2) : "ADD Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; ADD Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svint8_t svadd[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2) : "ADD Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "ADD Ztied2.B, Pg/M, Ztied2.B, Zop1.B" or "ADD Zresult.B, Zop1.B, Zop2.B"
    /// svint8_t svadd[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; ADD Zresult.B, Pg/M, Zresult.B, Zop2.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; ADD Zresult.B, Pg/M, Zresult.B, Zop1.B"
  public static unsafe Vector<sbyte> Add(Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t svadd[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2) : "ADD Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; ADD Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svint16_t svadd[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2) : "ADD Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "ADD Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "ADD Zresult.H, Zop1.H, Zop2.H"
    /// svint16_t svadd[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; ADD Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; ADD Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<short> Add(Vector<short> left, Vector<short> right);

    /// svint32_t svadd[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2) : "ADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; ADD Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svint32_t svadd[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2) : "ADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "ADD Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "ADD Zresult.S, Zop1.S, Zop2.S"
    /// svint32_t svadd[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; ADD Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; ADD Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<int> Add(Vector<int> left, Vector<int> right);

    /// svint64_t svadd[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2) : "ADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; ADD Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svint64_t svadd[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2) : "ADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "ADD Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "ADD Zresult.D, Zop1.D, Zop2.D"
    /// svint64_t svadd[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; ADD Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; ADD Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<long> Add(Vector<long> left, Vector<long> right);

    /// svuint8_t svadd[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2) : "ADD Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; ADD Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svuint8_t svadd[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2) : "ADD Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "ADD Ztied2.B, Pg/M, Ztied2.B, Zop1.B" or "ADD Zresult.B, Zop1.B, Zop2.B"
    /// svuint8_t svadd[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; ADD Zresult.B, Pg/M, Zresult.B, Zop2.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; ADD Zresult.B, Pg/M, Zresult.B, Zop1.B"
  public static unsafe Vector<byte> Add(Vector<byte> left, Vector<byte> right);

    /// svuint16_t svadd[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2) : "ADD Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; ADD Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svuint16_t svadd[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2) : "ADD Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "ADD Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "ADD Zresult.H, Zop1.H, Zop2.H"
    /// svuint16_t svadd[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; ADD Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; ADD Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<ushort> Add(Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svadd[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2) : "ADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; ADD Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svuint32_t svadd[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2) : "ADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "ADD Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "ADD Zresult.S, Zop1.S, Zop2.S"
    /// svuint32_t svadd[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; ADD Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; ADD Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<uint> Add(Vector<uint> left, Vector<uint> right);

    /// svuint64_t svadd[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2) : "ADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; ADD Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svuint64_t svadd[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2) : "ADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "ADD Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "ADD Zresult.D, Zop1.D, Zop2.D"
    /// svuint64_t svadd[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; ADD Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; ADD Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<ulong> Add(Vector<ulong> left, Vector<ulong> right);


    /// AddAcross : Add reduction

    /// float32_t svaddv[_f32](svbool_t pg, svfloat32_t op) : "FADDV Sresult, Pg, Zop.S"
  public static unsafe Vector<float> AddAcross(Vector<float> value);

    /// float64_t svaddv[_f64](svbool_t pg, svfloat64_t op) : "FADDV Dresult, Pg, Zop.D"
  public static unsafe Vector<double> AddAcross(Vector<double> value);

    /// int64_t svaddv[_s8](svbool_t pg, svint8_t op) : "SADDV Dresult, Pg, Zop.B"
  public static unsafe Vector<long> AddAcross(Vector<sbyte> value);

    /// int64_t svaddv[_s16](svbool_t pg, svint16_t op) : "SADDV Dresult, Pg, Zop.H"
  public static unsafe Vector<long> AddAcross(Vector<short> value);

    /// int64_t svaddv[_s32](svbool_t pg, svint32_t op) : "SADDV Dresult, Pg, Zop.S"
  public static unsafe Vector<long> AddAcross(Vector<int> value);

    /// int64_t svaddv[_s64](svbool_t pg, svint64_t op) : "UADDV Dresult, Pg, Zop.D"
  public static unsafe Vector<long> AddAcross(Vector<long> value);

    /// uint64_t svaddv[_u8](svbool_t pg, svuint8_t op) : "UADDV Dresult, Pg, Zop.B"
  public static unsafe Vector<ulong> AddAcross(Vector<byte> value);

    /// uint64_t svaddv[_u16](svbool_t pg, svuint16_t op) : "UADDV Dresult, Pg, Zop.H"
  public static unsafe Vector<ulong> AddAcross(Vector<ushort> value);

    /// uint64_t svaddv[_u32](svbool_t pg, svuint32_t op) : "UADDV Dresult, Pg, Zop.S"
  public static unsafe Vector<ulong> AddAcross(Vector<uint> value);

    /// uint64_t svaddv[_u64](svbool_t pg, svuint64_t op) : "UADDV Dresult, Pg, Zop.D"
  public static unsafe Vector<ulong> AddAcross(Vector<ulong> value);


    /// AddSaturate : Saturating add

    /// svint8_t svqadd[_s8](svint8_t op1, svint8_t op2) : "SQADD Zresult.B, Zop1.B, Zop2.B"
  public static unsafe Vector<sbyte> AddSaturate(Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t svqadd[_s16](svint16_t op1, svint16_t op2) : "SQADD Zresult.H, Zop1.H, Zop2.H"
  public static unsafe Vector<short> AddSaturate(Vector<short> left, Vector<short> right);

    /// svint32_t svqadd[_s32](svint32_t op1, svint32_t op2) : "SQADD Zresult.S, Zop1.S, Zop2.S"
  public static unsafe Vector<int> AddSaturate(Vector<int> left, Vector<int> right);

    /// svint64_t svqadd[_s64](svint64_t op1, svint64_t op2) : "SQADD Zresult.D, Zop1.D, Zop2.D"
  public static unsafe Vector<long> AddSaturate(Vector<long> left, Vector<long> right);

    /// svuint8_t svqadd[_u8](svuint8_t op1, svuint8_t op2) : "UQADD Zresult.B, Zop1.B, Zop2.B"
  public static unsafe Vector<byte> AddSaturate(Vector<byte> left, Vector<byte> right);

    /// svuint16_t svqadd[_u16](svuint16_t op1, svuint16_t op2) : "UQADD Zresult.H, Zop1.H, Zop2.H"
  public static unsafe Vector<ushort> AddSaturate(Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svqadd[_u32](svuint32_t op1, svuint32_t op2) : "UQADD Zresult.S, Zop1.S, Zop2.S"
  public static unsafe Vector<uint> AddSaturate(Vector<uint> left, Vector<uint> right);

    /// svuint64_t svqadd[_u64](svuint64_t op1, svuint64_t op2) : "UQADD Zresult.D, Zop1.D, Zop2.D"
  public static unsafe Vector<ulong> AddSaturate(Vector<ulong> left, Vector<ulong> right);


    /// Divide : Divide

    /// svfloat32_t svdiv[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "FDIV Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; FDIV Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svfloat32_t svdiv[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "FDIV Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "FDIVR Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "MOVPRFX Zresult, Zop1; FDIV Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svfloat32_t svdiv[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; FDIV Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; FDIVR Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<float> Divide(Vector<float> left, Vector<float> right);

    /// svfloat64_t svdiv[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "FDIV Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; FDIV Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svfloat64_t svdiv[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "FDIV Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "FDIVR Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "MOVPRFX Zresult, Zop1; FDIV Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svfloat64_t svdiv[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; FDIV Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; FDIVR Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<double> Divide(Vector<double> left, Vector<double> right);

    /// svint32_t svdiv[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2) : "SDIV Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; SDIV Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svint32_t svdiv[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2) : "SDIV Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "SDIVR Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "MOVPRFX Zresult, Zop1; SDIV Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svint32_t svdiv[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; SDIV Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; SDIVR Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<int> Divide(Vector<int> left, Vector<int> right);

    /// svint64_t svdiv[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2) : "SDIV Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; SDIV Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svint64_t svdiv[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2) : "SDIV Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "SDIVR Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "MOVPRFX Zresult, Zop1; SDIV Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svint64_t svdiv[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; SDIV Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; SDIVR Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<long> Divide(Vector<long> left, Vector<long> right);

    /// svuint32_t svdiv[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2) : "UDIV Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; UDIV Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svuint32_t svdiv[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2) : "UDIV Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "UDIVR Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "MOVPRFX Zresult, Zop1; UDIV Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svuint32_t svdiv[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; UDIV Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; UDIVR Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<uint> Divide(Vector<uint> left, Vector<uint> right);

    /// svuint64_t svdiv[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2) : "UDIV Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; UDIV Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svuint64_t svdiv[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2) : "UDIV Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "UDIVR Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "MOVPRFX Zresult, Zop1; UDIV Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svuint64_t svdiv[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; UDIV Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; UDIVR Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<ulong> Divide(Vector<ulong> left, Vector<ulong> right);


    /// DotProduct : Dot product

    /// svint32_t svdot[_s32](svint32_t op1, svint8_t op2, svint8_t op3) : "SDOT Ztied1.S, Zop2.B, Zop3.B" or "MOVPRFX Zresult, Zop1; SDOT Zresult.S, Zop2.B, Zop3.B"
  public static unsafe Vector<int> DotProduct(Vector<int> addend, Vector<sbyte> left, Vector<sbyte> right);

    /// svint64_t svdot[_s64](svint64_t op1, svint16_t op2, svint16_t op3) : "SDOT Ztied1.D, Zop2.H, Zop3.H" or "MOVPRFX Zresult, Zop1; SDOT Zresult.D, Zop2.H, Zop3.H"
  public static unsafe Vector<long> DotProduct(Vector<long> addend, Vector<short> left, Vector<short> right);

    /// svuint32_t svdot[_u32](svuint32_t op1, svuint8_t op2, svuint8_t op3) : "UDOT Ztied1.S, Zop2.B, Zop3.B" or "MOVPRFX Zresult, Zop1; UDOT Zresult.S, Zop2.B, Zop3.B"
  public static unsafe Vector<uint> DotProduct(Vector<uint> addend, Vector<byte> left, Vector<byte> right);

    /// svuint64_t svdot[_u64](svuint64_t op1, svuint16_t op2, svuint16_t op3) : "UDOT Ztied1.D, Zop2.H, Zop3.H" or "MOVPRFX Zresult, Zop1; UDOT Zresult.D, Zop2.H, Zop3.H"
  public static unsafe Vector<ulong> DotProduct(Vector<ulong> addend, Vector<ushort> left, Vector<ushort> right);


    /// DotProductBySelectedScalar : Dot product

    /// svint32_t svdot_lane[_s32](svint32_t op1, svint8_t op2, svint8_t op3, uint64_t imm_index) : "SDOT Ztied1.S, Zop2.B, Zop3.B[imm_index]" or "MOVPRFX Zresult, Zop1; SDOT Zresult.S, Zop2.B, Zop3.B[imm_index]"
  public static unsafe Vector<int> DotProductBySelectedScalar(Vector<int> addend, Vector<sbyte> left, Vector<sbyte> right, [ConstantExpected] byte rightIndex);

    /// svint64_t svdot_lane[_s64](svint64_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index) : "SDOT Ztied1.D, Zop2.H, Zop3.H[imm_index]" or "MOVPRFX Zresult, Zop1; SDOT Zresult.D, Zop2.H, Zop3.H[imm_index]"
  public static unsafe Vector<long> DotProductBySelectedScalar(Vector<long> addend, Vector<short> left, Vector<short> right, [ConstantExpected] byte rightIndex);

    /// svuint32_t svdot_lane[_u32](svuint32_t op1, svuint8_t op2, svuint8_t op3, uint64_t imm_index) : "UDOT Ztied1.S, Zop2.B, Zop3.B[imm_index]" or "MOVPRFX Zresult, Zop1; UDOT Zresult.S, Zop2.B, Zop3.B[imm_index]"
  public static unsafe Vector<uint> DotProductBySelectedScalar(Vector<uint> addend, Vector<byte> left, Vector<byte> right, [ConstantExpected] byte rightIndex);

    /// svuint64_t svdot_lane[_u64](svuint64_t op1, svuint16_t op2, svuint16_t op3, uint64_t imm_index) : "UDOT Ztied1.D, Zop2.H, Zop3.H[imm_index]" or "MOVPRFX Zresult, Zop1; UDOT Zresult.D, Zop2.H, Zop3.H[imm_index]"
  public static unsafe Vector<ulong> DotProductBySelectedScalar(Vector<ulong> addend, Vector<ushort> left, Vector<ushort> right, [ConstantExpected] byte rightIndex);


    /// FusedMultiplyAdd : Multiply-add, addend first

    /// svfloat32_t svmla[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3) : "FMLA Ztied1.S, Pg/M, Zop2.S, Zop3.S" or "MOVPRFX Zresult, Zop1; FMLA Zresult.S, Pg/M, Zop2.S, Zop3.S"
    /// svfloat32_t svmla[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3) : "FMLA Ztied1.S, Pg/M, Zop2.S, Zop3.S" or "FMAD Ztied2.S, Pg/M, Zop3.S, Zop1.S" or "FMAD Ztied3.S, Pg/M, Zop2.S, Zop1.S" or "MOVPRFX Zresult, Zop1; FMLA Zresult.S, Pg/M, Zop2.S, Zop3.S"
    /// svfloat32_t svmla[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; FMLA Zresult.S, Pg/M, Zop2.S, Zop3.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; FMAD Zresult.S, Pg/M, Zop3.S, Zop1.S" or "MOVPRFX Zresult.S, Pg/Z, Zop3.S; FMAD Zresult.S, Pg/M, Zop2.S, Zop1.S"
  public static unsafe Vector<float> FusedMultiplyAdd(Vector<float> addend, Vector<float> left, Vector<float> right);

    /// svfloat64_t svmla[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3) : "FMLA Ztied1.D, Pg/M, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; FMLA Zresult.D, Pg/M, Zop2.D, Zop3.D"
    /// svfloat64_t svmla[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3) : "FMLA Ztied1.D, Pg/M, Zop2.D, Zop3.D" or "FMAD Ztied2.D, Pg/M, Zop3.D, Zop1.D" or "FMAD Ztied3.D, Pg/M, Zop2.D, Zop1.D" or "MOVPRFX Zresult, Zop1; FMLA Zresult.D, Pg/M, Zop2.D, Zop3.D"
    /// svfloat64_t svmla[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; FMLA Zresult.D, Pg/M, Zop2.D, Zop3.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; FMAD Zresult.D, Pg/M, Zop3.D, Zop1.D" or "MOVPRFX Zresult.D, Pg/Z, Zop3.D; FMAD Zresult.D, Pg/M, Zop2.D, Zop1.D"
  public static unsafe Vector<double> FusedMultiplyAdd(Vector<double> addend, Vector<double> left, Vector<double> right);


    /// FusedMultiplyAddBySelectedScalar : Multiply-add, addend first

    /// svfloat32_t svmla_lane[_f32](svfloat32_t op1, svfloat32_t op2, svfloat32_t op3, uint64_t imm_index) : "FMLA Ztied1.S, Zop2.S, Zop3.S[imm_index]" or "MOVPRFX Zresult, Zop1; FMLA Zresult.S, Zop2.S, Zop3.S[imm_index]"
  public static unsafe Vector<float> FusedMultiplyAddBySelectedScalar(Vector<float> addend, Vector<float> left, Vector<float> right, [ConstantExpected] byte rightIndex);

    /// svfloat64_t svmla_lane[_f64](svfloat64_t op1, svfloat64_t op2, svfloat64_t op3, uint64_t imm_index) : "FMLA Ztied1.D, Zop2.D, Zop3.D[imm_index]" or "MOVPRFX Zresult, Zop1; FMLA Zresult.D, Zop2.D, Zop3.D[imm_index]"
  public static unsafe Vector<double> FusedMultiplyAddBySelectedScalar(Vector<double> addend, Vector<double> left, Vector<double> right, [ConstantExpected] byte rightIndex);


    /// FusedMultiplyAddNegated : Negated multiply-add, addend first

    /// svfloat32_t svnmla[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3) : "FNMLA Ztied1.S, Pg/M, Zop2.S, Zop3.S" or "MOVPRFX Zresult, Zop1; FNMLA Zresult.S, Pg/M, Zop2.S, Zop3.S"
    /// svfloat32_t svnmla[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3) : "FNMLA Ztied1.S, Pg/M, Zop2.S, Zop3.S" or "FNMAD Ztied2.S, Pg/M, Zop3.S, Zop1.S" or "FNMAD Ztied3.S, Pg/M, Zop2.S, Zop1.S" or "MOVPRFX Zresult, Zop1; FNMLA Zresult.S, Pg/M, Zop2.S, Zop3.S"
    /// svfloat32_t svnmla[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; FNMLA Zresult.S, Pg/M, Zop2.S, Zop3.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; FNMAD Zresult.S, Pg/M, Zop3.S, Zop1.S" or "MOVPRFX Zresult.S, Pg/Z, Zop3.S; FNMAD Zresult.S, Pg/M, Zop2.S, Zop1.S"
  public static unsafe Vector<float> FusedMultiplyAddNegated(Vector<float> addend, Vector<float> left, Vector<float> right);

    /// svfloat64_t svnmla[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3) : "FNMLA Ztied1.D, Pg/M, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; FNMLA Zresult.D, Pg/M, Zop2.D, Zop3.D"
    /// svfloat64_t svnmla[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3) : "FNMLA Ztied1.D, Pg/M, Zop2.D, Zop3.D" or "FNMAD Ztied2.D, Pg/M, Zop3.D, Zop1.D" or "FNMAD Ztied3.D, Pg/M, Zop2.D, Zop1.D" or "MOVPRFX Zresult, Zop1; FNMLA Zresult.D, Pg/M, Zop2.D, Zop3.D"
    /// svfloat64_t svnmla[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; FNMLA Zresult.D, Pg/M, Zop2.D, Zop3.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; FNMAD Zresult.D, Pg/M, Zop3.D, Zop1.D" or "MOVPRFX Zresult.D, Pg/Z, Zop3.D; FNMAD Zresult.D, Pg/M, Zop2.D, Zop1.D"
  public static unsafe Vector<double> FusedMultiplyAddNegated(Vector<double> addend, Vector<double> left, Vector<double> right);


    /// FusedMultiplySubtract : Multiply-subtract, minuend first

    /// svfloat32_t svmls[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3) : "FMLS Ztied1.S, Pg/M, Zop2.S, Zop3.S" or "MOVPRFX Zresult, Zop1; FMLS Zresult.S, Pg/M, Zop2.S, Zop3.S"
    /// svfloat32_t svmls[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3) : "FMLS Ztied1.S, Pg/M, Zop2.S, Zop3.S" or "FMSB Ztied2.S, Pg/M, Zop3.S, Zop1.S" or "FMSB Ztied3.S, Pg/M, Zop2.S, Zop1.S" or "MOVPRFX Zresult, Zop1; FMLS Zresult.S, Pg/M, Zop2.S, Zop3.S"
    /// svfloat32_t svmls[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; FMLS Zresult.S, Pg/M, Zop2.S, Zop3.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; FMSB Zresult.S, Pg/M, Zop3.S, Zop1.S" or "MOVPRFX Zresult.S, Pg/Z, Zop3.S; FMSB Zresult.S, Pg/M, Zop2.S, Zop1.S"
  public static unsafe Vector<float> FusedMultiplySubtract(Vector<float> minuend, Vector<float> left, Vector<float> right);

    /// svfloat64_t svmls[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3) : "FMLS Ztied1.D, Pg/M, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; FMLS Zresult.D, Pg/M, Zop2.D, Zop3.D"
    /// svfloat64_t svmls[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3) : "FMLS Ztied1.D, Pg/M, Zop2.D, Zop3.D" or "FMSB Ztied2.D, Pg/M, Zop3.D, Zop1.D" or "FMSB Ztied3.D, Pg/M, Zop2.D, Zop1.D" or "MOVPRFX Zresult, Zop1; FMLS Zresult.D, Pg/M, Zop2.D, Zop3.D"
    /// svfloat64_t svmls[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; FMLS Zresult.D, Pg/M, Zop2.D, Zop3.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; FMSB Zresult.D, Pg/M, Zop3.D, Zop1.D" or "MOVPRFX Zresult.D, Pg/Z, Zop3.D; FMSB Zresult.D, Pg/M, Zop2.D, Zop1.D"
  public static unsafe Vector<double> FusedMultiplySubtract(Vector<double> minuend, Vector<double> left, Vector<double> right);


    /// FusedMultiplySubtractBySelectedScalar : Multiply-subtract, minuend first

    /// svfloat32_t svmls_lane[_f32](svfloat32_t op1, svfloat32_t op2, svfloat32_t op3, uint64_t imm_index) : "FMLS Ztied1.S, Zop2.S, Zop3.S[imm_index]" or "MOVPRFX Zresult, Zop1; FMLS Zresult.S, Zop2.S, Zop3.S[imm_index]"
  public static unsafe Vector<float> FusedMultiplySubtractBySelectedScalar(Vector<float> minuend, Vector<float> left, Vector<float> right, [ConstantExpected] byte rightIndex);

    /// svfloat64_t svmls_lane[_f64](svfloat64_t op1, svfloat64_t op2, svfloat64_t op3, uint64_t imm_index) : "FMLS Ztied1.D, Zop2.D, Zop3.D[imm_index]" or "MOVPRFX Zresult, Zop1; FMLS Zresult.D, Zop2.D, Zop3.D[imm_index]"
  public static unsafe Vector<double> FusedMultiplySubtractBySelectedScalar(Vector<double> minuend, Vector<double> left, Vector<double> right, [ConstantExpected] byte rightIndex);


    /// FusedMultiplySubtractNegated : Negated multiply-subtract, minuend first

    /// svfloat32_t svnmls[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3) : "FNMLS Ztied1.S, Pg/M, Zop2.S, Zop3.S" or "MOVPRFX Zresult, Zop1; FNMLS Zresult.S, Pg/M, Zop2.S, Zop3.S"
    /// svfloat32_t svnmls[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3) : "FNMLS Ztied1.S, Pg/M, Zop2.S, Zop3.S" or "FNMSB Ztied2.S, Pg/M, Zop3.S, Zop1.S" or "FNMSB Ztied3.S, Pg/M, Zop2.S, Zop1.S" or "MOVPRFX Zresult, Zop1; FNMLS Zresult.S, Pg/M, Zop2.S, Zop3.S"
    /// svfloat32_t svnmls[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; FNMLS Zresult.S, Pg/M, Zop2.S, Zop3.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; FNMSB Zresult.S, Pg/M, Zop3.S, Zop1.S" or "MOVPRFX Zresult.S, Pg/Z, Zop3.S; FNMSB Zresult.S, Pg/M, Zop2.S, Zop1.S"
  public static unsafe Vector<float> FusedMultiplySubtractNegated(Vector<float> minuend, Vector<float> left, Vector<float> right);

    /// svfloat64_t svnmls[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3) : "FNMLS Ztied1.D, Pg/M, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; FNMLS Zresult.D, Pg/M, Zop2.D, Zop3.D"
    /// svfloat64_t svnmls[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3) : "FNMLS Ztied1.D, Pg/M, Zop2.D, Zop3.D" or "FNMSB Ztied2.D, Pg/M, Zop3.D, Zop1.D" or "FNMSB Ztied3.D, Pg/M, Zop2.D, Zop1.D" or "MOVPRFX Zresult, Zop1; FNMLS Zresult.D, Pg/M, Zop2.D, Zop3.D"
    /// svfloat64_t svnmls[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; FNMLS Zresult.D, Pg/M, Zop2.D, Zop3.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; FNMSB Zresult.D, Pg/M, Zop3.D, Zop1.D" or "MOVPRFX Zresult.D, Pg/Z, Zop3.D; FNMSB Zresult.D, Pg/M, Zop2.D, Zop1.D"
  public static unsafe Vector<double> FusedMultiplySubtractNegated(Vector<double> minuend, Vector<double> left, Vector<double> right);


    /// Max : Maximum

    /// svfloat32_t svmax[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "FMAX Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; FMAX Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svfloat32_t svmax[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "FMAX Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "FMAX Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "MOVPRFX Zresult, Zop1; FMAX Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svfloat32_t svmax[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; FMAX Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; FMAX Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<float> Max(Vector<float> left, Vector<float> right);

    /// svfloat64_t svmax[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "FMAX Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; FMAX Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svfloat64_t svmax[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "FMAX Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "FMAX Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "MOVPRFX Zresult, Zop1; FMAX Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svfloat64_t svmax[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; FMAX Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; FMAX Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<double> Max(Vector<double> left, Vector<double> right);

    /// svint8_t svmax[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2) : "SMAX Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; SMAX Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svint8_t svmax[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2) : "SMAX Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "SMAX Ztied2.B, Pg/M, Ztied2.B, Zop1.B" or "MOVPRFX Zresult, Zop1; SMAX Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svint8_t svmax[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; SMAX Zresult.B, Pg/M, Zresult.B, Zop2.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; SMAX Zresult.B, Pg/M, Zresult.B, Zop1.B"
  public static unsafe Vector<sbyte> Max(Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t svmax[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2) : "SMAX Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; SMAX Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svint16_t svmax[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2) : "SMAX Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "SMAX Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "MOVPRFX Zresult, Zop1; SMAX Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svint16_t svmax[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; SMAX Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; SMAX Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<short> Max(Vector<short> left, Vector<short> right);

    /// svint32_t svmax[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2) : "SMAX Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; SMAX Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svint32_t svmax[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2) : "SMAX Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "SMAX Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "MOVPRFX Zresult, Zop1; SMAX Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svint32_t svmax[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; SMAX Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; SMAX Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<int> Max(Vector<int> left, Vector<int> right);

    /// svint64_t svmax[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2) : "SMAX Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; SMAX Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svint64_t svmax[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2) : "SMAX Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "SMAX Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "MOVPRFX Zresult, Zop1; SMAX Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svint64_t svmax[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; SMAX Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; SMAX Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<long> Max(Vector<long> left, Vector<long> right);

    /// svuint8_t svmax[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2) : "UMAX Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; UMAX Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svuint8_t svmax[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2) : "UMAX Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "UMAX Ztied2.B, Pg/M, Ztied2.B, Zop1.B" or "MOVPRFX Zresult, Zop1; UMAX Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svuint8_t svmax[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; UMAX Zresult.B, Pg/M, Zresult.B, Zop2.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; UMAX Zresult.B, Pg/M, Zresult.B, Zop1.B"
  public static unsafe Vector<byte> Max(Vector<byte> left, Vector<byte> right);

    /// svuint16_t svmax[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2) : "UMAX Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; UMAX Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svuint16_t svmax[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2) : "UMAX Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "UMAX Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "MOVPRFX Zresult, Zop1; UMAX Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svuint16_t svmax[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; UMAX Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; UMAX Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<ushort> Max(Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svmax[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2) : "UMAX Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; UMAX Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svuint32_t svmax[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2) : "UMAX Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "UMAX Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "MOVPRFX Zresult, Zop1; UMAX Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svuint32_t svmax[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; UMAX Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; UMAX Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<uint> Max(Vector<uint> left, Vector<uint> right);

    /// svuint64_t svmax[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2) : "UMAX Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; UMAX Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svuint64_t svmax[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2) : "UMAX Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "UMAX Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "MOVPRFX Zresult, Zop1; UMAX Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svuint64_t svmax[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; UMAX Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; UMAX Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<ulong> Max(Vector<ulong> left, Vector<ulong> right);


    /// MaxAcross : Maximum reduction to scalar

    /// float32_t svmaxv[_f32](svbool_t pg, svfloat32_t op) : "FMAXV Sresult, Pg, Zop.S"
  public static unsafe Vector<float> MaxAcross(Vector<float> value);

    /// float64_t svmaxv[_f64](svbool_t pg, svfloat64_t op) : "FMAXV Dresult, Pg, Zop.D"
  public static unsafe Vector<double> MaxAcross(Vector<double> value);

    /// int8_t svmaxv[_s8](svbool_t pg, svint8_t op) : "SMAXV Bresult, Pg, Zop.B"
  public static unsafe Vector<sbyte> MaxAcross(Vector<sbyte> value);

    /// int16_t svmaxv[_s16](svbool_t pg, svint16_t op) : "SMAXV Hresult, Pg, Zop.H"
  public static unsafe Vector<short> MaxAcross(Vector<short> value);

    /// int32_t svmaxv[_s32](svbool_t pg, svint32_t op) : "SMAXV Sresult, Pg, Zop.S"
  public static unsafe Vector<int> MaxAcross(Vector<int> value);

    /// int64_t svmaxv[_s64](svbool_t pg, svint64_t op) : "SMAXV Dresult, Pg, Zop.D"
  public static unsafe Vector<long> MaxAcross(Vector<long> value);

    /// uint8_t svmaxv[_u8](svbool_t pg, svuint8_t op) : "UMAXV Bresult, Pg, Zop.B"
  public static unsafe Vector<byte> MaxAcross(Vector<byte> value);

    /// uint16_t svmaxv[_u16](svbool_t pg, svuint16_t op) : "UMAXV Hresult, Pg, Zop.H"
  public static unsafe Vector<ushort> MaxAcross(Vector<ushort> value);

    /// uint32_t svmaxv[_u32](svbool_t pg, svuint32_t op) : "UMAXV Sresult, Pg, Zop.S"
  public static unsafe Vector<uint> MaxAcross(Vector<uint> value);

    /// uint64_t svmaxv[_u64](svbool_t pg, svuint64_t op) : "UMAXV Dresult, Pg, Zop.D"
  public static unsafe Vector<ulong> MaxAcross(Vector<ulong> value);


    /// MaxNumber : Maximum number

    /// svfloat32_t svmaxnm[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "FMAXNM Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; FMAXNM Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svfloat32_t svmaxnm[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "FMAXNM Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "FMAXNM Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "MOVPRFX Zresult, Zop1; FMAXNM Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svfloat32_t svmaxnm[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; FMAXNM Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; FMAXNM Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<float> MaxNumber(Vector<float> left, Vector<float> right);

    /// svfloat64_t svmaxnm[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "FMAXNM Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; FMAXNM Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svfloat64_t svmaxnm[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "FMAXNM Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "FMAXNM Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "MOVPRFX Zresult, Zop1; FMAXNM Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svfloat64_t svmaxnm[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; FMAXNM Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; FMAXNM Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<double> MaxNumber(Vector<double> left, Vector<double> right);


    /// MaxNumberAcross : Maximum number reduction to scalar

    /// float32_t svmaxnmv[_f32](svbool_t pg, svfloat32_t op) : "FMAXNMV Sresult, Pg, Zop.S"
  public static unsafe Vector<float> MaxNumberAcross(Vector<float> value);

    /// float64_t svmaxnmv[_f64](svbool_t pg, svfloat64_t op) : "FMAXNMV Dresult, Pg, Zop.D"
  public static unsafe Vector<double> MaxNumberAcross(Vector<double> value);


    /// Min : Minimum

    /// svfloat32_t svmin[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "FMIN Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; FMIN Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svfloat32_t svmin[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "FMIN Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "FMIN Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "MOVPRFX Zresult, Zop1; FMIN Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svfloat32_t svmin[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; FMIN Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; FMIN Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<float> Min(Vector<float> left, Vector<float> right);

    /// svfloat64_t svmin[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "FMIN Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; FMIN Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svfloat64_t svmin[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "FMIN Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "FMIN Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "MOVPRFX Zresult, Zop1; FMIN Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svfloat64_t svmin[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; FMIN Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; FMIN Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<double> Min(Vector<double> left, Vector<double> right);

    /// svint8_t svmin[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2) : "SMIN Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; SMIN Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svint8_t svmin[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2) : "SMIN Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "SMIN Ztied2.B, Pg/M, Ztied2.B, Zop1.B" or "MOVPRFX Zresult, Zop1; SMIN Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svint8_t svmin[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; SMIN Zresult.B, Pg/M, Zresult.B, Zop2.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; SMIN Zresult.B, Pg/M, Zresult.B, Zop1.B"
  public static unsafe Vector<sbyte> Min(Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t svmin[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2) : "SMIN Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; SMIN Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svint16_t svmin[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2) : "SMIN Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "SMIN Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "MOVPRFX Zresult, Zop1; SMIN Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svint16_t svmin[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; SMIN Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; SMIN Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<short> Min(Vector<short> left, Vector<short> right);

    /// svint32_t svmin[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2) : "SMIN Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; SMIN Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svint32_t svmin[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2) : "SMIN Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "SMIN Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "MOVPRFX Zresult, Zop1; SMIN Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svint32_t svmin[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; SMIN Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; SMIN Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<int> Min(Vector<int> left, Vector<int> right);

    /// svint64_t svmin[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2) : "SMIN Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; SMIN Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svint64_t svmin[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2) : "SMIN Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "SMIN Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "MOVPRFX Zresult, Zop1; SMIN Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svint64_t svmin[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; SMIN Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; SMIN Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<long> Min(Vector<long> left, Vector<long> right);

    /// svuint8_t svmin[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2) : "UMIN Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; UMIN Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svuint8_t svmin[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2) : "UMIN Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "UMIN Ztied2.B, Pg/M, Ztied2.B, Zop1.B" or "MOVPRFX Zresult, Zop1; UMIN Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svuint8_t svmin[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; UMIN Zresult.B, Pg/M, Zresult.B, Zop2.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; UMIN Zresult.B, Pg/M, Zresult.B, Zop1.B"
  public static unsafe Vector<byte> Min(Vector<byte> left, Vector<byte> right);

    /// svuint16_t svmin[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2) : "UMIN Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; UMIN Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svuint16_t svmin[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2) : "UMIN Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "UMIN Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "MOVPRFX Zresult, Zop1; UMIN Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svuint16_t svmin[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; UMIN Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; UMIN Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<ushort> Min(Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svmin[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2) : "UMIN Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; UMIN Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svuint32_t svmin[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2) : "UMIN Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "UMIN Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "MOVPRFX Zresult, Zop1; UMIN Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svuint32_t svmin[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; UMIN Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; UMIN Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<uint> Min(Vector<uint> left, Vector<uint> right);

    /// svuint64_t svmin[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2) : "UMIN Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; UMIN Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svuint64_t svmin[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2) : "UMIN Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "UMIN Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "MOVPRFX Zresult, Zop1; UMIN Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svuint64_t svmin[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; UMIN Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; UMIN Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<ulong> Min(Vector<ulong> left, Vector<ulong> right);


    /// MinAcross : Minimum reduction to scalar

    /// float32_t svminv[_f32](svbool_t pg, svfloat32_t op) : "FMINV Sresult, Pg, Zop.S"
  public static unsafe Vector<float> MinAcross(Vector<float> value);

    /// float64_t svminv[_f64](svbool_t pg, svfloat64_t op) : "FMINV Dresult, Pg, Zop.D"
  public static unsafe Vector<double> MinAcross(Vector<double> value);

    /// int8_t svminv[_s8](svbool_t pg, svint8_t op) : "SMINV Bresult, Pg, Zop.B"
  public static unsafe Vector<sbyte> MinAcross(Vector<sbyte> value);

    /// int16_t svminv[_s16](svbool_t pg, svint16_t op) : "SMINV Hresult, Pg, Zop.H"
  public static unsafe Vector<short> MinAcross(Vector<short> value);

    /// int32_t svminv[_s32](svbool_t pg, svint32_t op) : "SMINV Sresult, Pg, Zop.S"
  public static unsafe Vector<int> MinAcross(Vector<int> value);

    /// int64_t svminv[_s64](svbool_t pg, svint64_t op) : "SMINV Dresult, Pg, Zop.D"
  public static unsafe Vector<long> MinAcross(Vector<long> value);

    /// uint8_t svminv[_u8](svbool_t pg, svuint8_t op) : "UMINV Bresult, Pg, Zop.B"
  public static unsafe Vector<byte> MinAcross(Vector<byte> value);

    /// uint16_t svminv[_u16](svbool_t pg, svuint16_t op) : "UMINV Hresult, Pg, Zop.H"
  public static unsafe Vector<ushort> MinAcross(Vector<ushort> value);

    /// uint32_t svminv[_u32](svbool_t pg, svuint32_t op) : "UMINV Sresult, Pg, Zop.S"
  public static unsafe Vector<uint> MinAcross(Vector<uint> value);

    /// uint64_t svminv[_u64](svbool_t pg, svuint64_t op) : "UMINV Dresult, Pg, Zop.D"
  public static unsafe Vector<ulong> MinAcross(Vector<ulong> value);


    /// MinNumber : Minimum number

    /// svfloat32_t svminnm[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "FMINNM Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; FMINNM Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svfloat32_t svminnm[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "FMINNM Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "FMINNM Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "MOVPRFX Zresult, Zop1; FMINNM Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svfloat32_t svminnm[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; FMINNM Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; FMINNM Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<float> MinNumber(Vector<float> left, Vector<float> right);

    /// svfloat64_t svminnm[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "FMINNM Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; FMINNM Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svfloat64_t svminnm[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "FMINNM Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "FMINNM Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "MOVPRFX Zresult, Zop1; FMINNM Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svfloat64_t svminnm[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; FMINNM Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; FMINNM Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<double> MinNumber(Vector<double> left, Vector<double> right);


    /// MinNumberAcross : Minimum number reduction to scalar

    /// float32_t svminnmv[_f32](svbool_t pg, svfloat32_t op) : "FMINNMV Sresult, Pg, Zop.S"
  public static unsafe Vector<float> MinNumberAcross(Vector<float> value);

    /// float64_t svminnmv[_f64](svbool_t pg, svfloat64_t op) : "FMINNMV Dresult, Pg, Zop.D"
  public static unsafe Vector<double> MinNumberAcross(Vector<double> value);


    /// Multiply : Multiply

    /// svfloat32_t svmul[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "FMUL Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; FMUL Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svfloat32_t svmul[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "FMUL Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "FMUL Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "FMUL Zresult.S, Zop1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; FMUL Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svfloat32_t svmul[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; FMUL Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; FMUL Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<float> Multiply(Vector<float> left, Vector<float> right);

    /// svfloat64_t svmul[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "FMUL Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; FMUL Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svfloat64_t svmul[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "FMUL Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "FMUL Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "FMUL Zresult.D, Zop1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; FMUL Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svfloat64_t svmul[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; FMUL Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; FMUL Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<double> Multiply(Vector<double> left, Vector<double> right);

    /// svint8_t svmul[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2) : "MUL Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; MUL Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svint8_t svmul[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2) : "MUL Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MUL Ztied2.B, Pg/M, Ztied2.B, Zop1.B" or "MOVPRFX Zresult, Zop1; MUL Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svint8_t svmul[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; MUL Zresult.B, Pg/M, Zresult.B, Zop2.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; MUL Zresult.B, Pg/M, Zresult.B, Zop1.B"
  public static unsafe Vector<sbyte> Multiply(Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t svmul[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2) : "MUL Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; MUL Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svint16_t svmul[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2) : "MUL Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MUL Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "MOVPRFX Zresult, Zop1; MUL Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svint16_t svmul[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; MUL Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; MUL Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<short> Multiply(Vector<short> left, Vector<short> right);

    /// svint32_t svmul[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2) : "MUL Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; MUL Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svint32_t svmul[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2) : "MUL Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MUL Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "MOVPRFX Zresult, Zop1; MUL Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svint32_t svmul[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; MUL Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; MUL Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<int> Multiply(Vector<int> left, Vector<int> right);

    /// svint64_t svmul[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2) : "MUL Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; MUL Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svint64_t svmul[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2) : "MUL Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MUL Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "MOVPRFX Zresult, Zop1; MUL Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svint64_t svmul[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; MUL Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; MUL Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<long> Multiply(Vector<long> left, Vector<long> right);

    /// svuint8_t svmul[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2) : "MUL Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; MUL Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svuint8_t svmul[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2) : "MUL Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MUL Ztied2.B, Pg/M, Ztied2.B, Zop1.B" or "MOVPRFX Zresult, Zop1; MUL Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svuint8_t svmul[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; MUL Zresult.B, Pg/M, Zresult.B, Zop2.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; MUL Zresult.B, Pg/M, Zresult.B, Zop1.B"
  public static unsafe Vector<byte> Multiply(Vector<byte> left, Vector<byte> right);

    /// svuint16_t svmul[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2) : "MUL Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; MUL Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svuint16_t svmul[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2) : "MUL Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MUL Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "MOVPRFX Zresult, Zop1; MUL Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svuint16_t svmul[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; MUL Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; MUL Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<ushort> Multiply(Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svmul[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2) : "MUL Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; MUL Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svuint32_t svmul[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2) : "MUL Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MUL Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "MOVPRFX Zresult, Zop1; MUL Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svuint32_t svmul[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; MUL Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; MUL Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<uint> Multiply(Vector<uint> left, Vector<uint> right);

    /// svuint64_t svmul[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2) : "MUL Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; MUL Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svuint64_t svmul[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2) : "MUL Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MUL Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "MOVPRFX Zresult, Zop1; MUL Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svuint64_t svmul[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; MUL Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; MUL Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<ulong> Multiply(Vector<ulong> left, Vector<ulong> right);


    /// MultiplyAdd : Multiply-add, addend first

    /// svint8_t svmla[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2, svint8_t op3) : "MLA Ztied1.B, Pg/M, Zop2.B, Zop3.B" or "MOVPRFX Zresult, Zop1; MLA Zresult.B, Pg/M, Zop2.B, Zop3.B"
    /// svint8_t svmla[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2, svint8_t op3) : "MLA Ztied1.B, Pg/M, Zop2.B, Zop3.B" or "MAD Ztied2.B, Pg/M, Zop3.B, Zop1.B" or "MAD Ztied3.B, Pg/M, Zop2.B, Zop1.B" or "MOVPRFX Zresult, Zop1; MLA Zresult.B, Pg/M, Zop2.B, Zop3.B"
    /// svint8_t svmla[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2, svint8_t op3) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; MLA Zresult.B, Pg/M, Zop2.B, Zop3.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; MAD Zresult.B, Pg/M, Zop3.B, Zop1.B" or "MOVPRFX Zresult.B, Pg/Z, Zop3.B; MAD Zresult.B, Pg/M, Zop2.B, Zop1.B"
  public static unsafe Vector<sbyte> MultiplyAdd(Vector<sbyte> addend, Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t svmla[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2, svint16_t op3) : "MLA Ztied1.H, Pg/M, Zop2.H, Zop3.H" or "MOVPRFX Zresult, Zop1; MLA Zresult.H, Pg/M, Zop2.H, Zop3.H"
    /// svint16_t svmla[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2, svint16_t op3) : "MLA Ztied1.H, Pg/M, Zop2.H, Zop3.H" or "MAD Ztied2.H, Pg/M, Zop3.H, Zop1.H" or "MAD Ztied3.H, Pg/M, Zop2.H, Zop1.H" or "MOVPRFX Zresult, Zop1; MLA Zresult.H, Pg/M, Zop2.H, Zop3.H"
    /// svint16_t svmla[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2, svint16_t op3) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; MLA Zresult.H, Pg/M, Zop2.H, Zop3.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; MAD Zresult.H, Pg/M, Zop3.H, Zop1.H" or "MOVPRFX Zresult.H, Pg/Z, Zop3.H; MAD Zresult.H, Pg/M, Zop2.H, Zop1.H"
  public static unsafe Vector<short> MultiplyAdd(Vector<short> addend, Vector<short> left, Vector<short> right);

    /// svint32_t svmla[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2, svint32_t op3) : "MLA Ztied1.S, Pg/M, Zop2.S, Zop3.S" or "MOVPRFX Zresult, Zop1; MLA Zresult.S, Pg/M, Zop2.S, Zop3.S"
    /// svint32_t svmla[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2, svint32_t op3) : "MLA Ztied1.S, Pg/M, Zop2.S, Zop3.S" or "MAD Ztied2.S, Pg/M, Zop3.S, Zop1.S" or "MAD Ztied3.S, Pg/M, Zop2.S, Zop1.S" or "MOVPRFX Zresult, Zop1; MLA Zresult.S, Pg/M, Zop2.S, Zop3.S"
    /// svint32_t svmla[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2, svint32_t op3) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; MLA Zresult.S, Pg/M, Zop2.S, Zop3.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; MAD Zresult.S, Pg/M, Zop3.S, Zop1.S" or "MOVPRFX Zresult.S, Pg/Z, Zop3.S; MAD Zresult.S, Pg/M, Zop2.S, Zop1.S"
  public static unsafe Vector<int> MultiplyAdd(Vector<int> addend, Vector<int> left, Vector<int> right);

    /// svint64_t svmla[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2, svint64_t op3) : "MLA Ztied1.D, Pg/M, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; MLA Zresult.D, Pg/M, Zop2.D, Zop3.D"
    /// svint64_t svmla[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2, svint64_t op3) : "MLA Ztied1.D, Pg/M, Zop2.D, Zop3.D" or "MAD Ztied2.D, Pg/M, Zop3.D, Zop1.D" or "MAD Ztied3.D, Pg/M, Zop2.D, Zop1.D" or "MOVPRFX Zresult, Zop1; MLA Zresult.D, Pg/M, Zop2.D, Zop3.D"
    /// svint64_t svmla[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2, svint64_t op3) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; MLA Zresult.D, Pg/M, Zop2.D, Zop3.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; MAD Zresult.D, Pg/M, Zop3.D, Zop1.D" or "MOVPRFX Zresult.D, Pg/Z, Zop3.D; MAD Zresult.D, Pg/M, Zop2.D, Zop1.D"
  public static unsafe Vector<long> MultiplyAdd(Vector<long> addend, Vector<long> left, Vector<long> right);

    /// svuint8_t svmla[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2, svuint8_t op3) : "MLA Ztied1.B, Pg/M, Zop2.B, Zop3.B" or "MOVPRFX Zresult, Zop1; MLA Zresult.B, Pg/M, Zop2.B, Zop3.B"
    /// svuint8_t svmla[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2, svuint8_t op3) : "MLA Ztied1.B, Pg/M, Zop2.B, Zop3.B" or "MAD Ztied2.B, Pg/M, Zop3.B, Zop1.B" or "MAD Ztied3.B, Pg/M, Zop2.B, Zop1.B" or "MOVPRFX Zresult, Zop1; MLA Zresult.B, Pg/M, Zop2.B, Zop3.B"
    /// svuint8_t svmla[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2, svuint8_t op3) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; MLA Zresult.B, Pg/M, Zop2.B, Zop3.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; MAD Zresult.B, Pg/M, Zop3.B, Zop1.B" or "MOVPRFX Zresult.B, Pg/Z, Zop3.B; MAD Zresult.B, Pg/M, Zop2.B, Zop1.B"
  public static unsafe Vector<byte> MultiplyAdd(Vector<byte> addend, Vector<byte> left, Vector<byte> right);

    /// svuint16_t svmla[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2, svuint16_t op3) : "MLA Ztied1.H, Pg/M, Zop2.H, Zop3.H" or "MOVPRFX Zresult, Zop1; MLA Zresult.H, Pg/M, Zop2.H, Zop3.H"
    /// svuint16_t svmla[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2, svuint16_t op3) : "MLA Ztied1.H, Pg/M, Zop2.H, Zop3.H" or "MAD Ztied2.H, Pg/M, Zop3.H, Zop1.H" or "MAD Ztied3.H, Pg/M, Zop2.H, Zop1.H" or "MOVPRFX Zresult, Zop1; MLA Zresult.H, Pg/M, Zop2.H, Zop3.H"
    /// svuint16_t svmla[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2, svuint16_t op3) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; MLA Zresult.H, Pg/M, Zop2.H, Zop3.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; MAD Zresult.H, Pg/M, Zop3.H, Zop1.H" or "MOVPRFX Zresult.H, Pg/Z, Zop3.H; MAD Zresult.H, Pg/M, Zop2.H, Zop1.H"
  public static unsafe Vector<ushort> MultiplyAdd(Vector<ushort> addend, Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svmla[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2, svuint32_t op3) : "MLA Ztied1.S, Pg/M, Zop2.S, Zop3.S" or "MOVPRFX Zresult, Zop1; MLA Zresult.S, Pg/M, Zop2.S, Zop3.S"
    /// svuint32_t svmla[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2, svuint32_t op3) : "MLA Ztied1.S, Pg/M, Zop2.S, Zop3.S" or "MAD Ztied2.S, Pg/M, Zop3.S, Zop1.S" or "MAD Ztied3.S, Pg/M, Zop2.S, Zop1.S" or "MOVPRFX Zresult, Zop1; MLA Zresult.S, Pg/M, Zop2.S, Zop3.S"
    /// svuint32_t svmla[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2, svuint32_t op3) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; MLA Zresult.S, Pg/M, Zop2.S, Zop3.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; MAD Zresult.S, Pg/M, Zop3.S, Zop1.S" or "MOVPRFX Zresult.S, Pg/Z, Zop3.S; MAD Zresult.S, Pg/M, Zop2.S, Zop1.S"
  public static unsafe Vector<uint> MultiplyAdd(Vector<uint> addend, Vector<uint> left, Vector<uint> right);

    /// svuint64_t svmla[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2, svuint64_t op3) : "MLA Ztied1.D, Pg/M, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; MLA Zresult.D, Pg/M, Zop2.D, Zop3.D"
    /// svuint64_t svmla[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2, svuint64_t op3) : "MLA Ztied1.D, Pg/M, Zop2.D, Zop3.D" or "MAD Ztied2.D, Pg/M, Zop3.D, Zop1.D" or "MAD Ztied3.D, Pg/M, Zop2.D, Zop1.D" or "MOVPRFX Zresult, Zop1; MLA Zresult.D, Pg/M, Zop2.D, Zop3.D"
    /// svuint64_t svmla[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2, svuint64_t op3) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; MLA Zresult.D, Pg/M, Zop2.D, Zop3.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; MAD Zresult.D, Pg/M, Zop3.D, Zop1.D" or "MOVPRFX Zresult.D, Pg/Z, Zop3.D; MAD Zresult.D, Pg/M, Zop2.D, Zop1.D"
  public static unsafe Vector<ulong> MultiplyAdd(Vector<ulong> addend, Vector<ulong> left, Vector<ulong> right);


    /// MultiplyBySelectedScalar : Multiply

    /// svfloat32_t svmul_lane[_f32](svfloat32_t op1, svfloat32_t op2, uint64_t imm_index) : "FMUL Zresult.S, Zop1.S, Zop2.S[imm_index]"
  public static unsafe Vector<float> MultiplyBySelectedScalar(Vector<float> left, Vector<float> right, [ConstantExpected] byte rightIndex);

    /// svfloat64_t svmul_lane[_f64](svfloat64_t op1, svfloat64_t op2, uint64_t imm_index) : "FMUL Zresult.D, Zop1.D, Zop2.D[imm_index]"
  public static unsafe Vector<double> MultiplyBySelectedScalar(Vector<double> left, Vector<double> right, [ConstantExpected] byte rightIndex);


    /// MultiplyExtended : Multiply extended (∞×0=2)

    /// svfloat32_t svmulx[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "FMULX Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; FMULX Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svfloat32_t svmulx[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "FMULX Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "FMULX Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "MOVPRFX Zresult, Zop1; FMULX Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svfloat32_t svmulx[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; FMULX Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; FMULX Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<float> MultiplyExtended(Vector<float> left, Vector<float> right);

    /// svfloat64_t svmulx[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "FMULX Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; FMULX Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svfloat64_t svmulx[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "FMULX Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "FMULX Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "MOVPRFX Zresult, Zop1; FMULX Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svfloat64_t svmulx[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; FMULX Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; FMULX Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<double> MultiplyExtended(Vector<double> left, Vector<double> right);


    /// MultiplySubtract : Multiply-subtract, minuend first

    /// svint8_t svmls[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2, svint8_t op3) : "MLS Ztied1.B, Pg/M, Zop2.B, Zop3.B" or "MOVPRFX Zresult, Zop1; MLS Zresult.B, Pg/M, Zop2.B, Zop3.B"
    /// svint8_t svmls[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2, svint8_t op3) : "MLS Ztied1.B, Pg/M, Zop2.B, Zop3.B" or "MSB Ztied2.B, Pg/M, Zop3.B, Zop1.B" or "MSB Ztied3.B, Pg/M, Zop2.B, Zop1.B" or "MOVPRFX Zresult, Zop1; MLS Zresult.B, Pg/M, Zop2.B, Zop3.B"
    /// svint8_t svmls[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2, svint8_t op3) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; MLS Zresult.B, Pg/M, Zop2.B, Zop3.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; MSB Zresult.B, Pg/M, Zop3.B, Zop1.B" or "MOVPRFX Zresult.B, Pg/Z, Zop3.B; MSB Zresult.B, Pg/M, Zop2.B, Zop1.B"
  public static unsafe Vector<sbyte> MultiplySubtract(Vector<sbyte> minuend, Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t svmls[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2, svint16_t op3) : "MLS Ztied1.H, Pg/M, Zop2.H, Zop3.H" or "MOVPRFX Zresult, Zop1; MLS Zresult.H, Pg/M, Zop2.H, Zop3.H"
    /// svint16_t svmls[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2, svint16_t op3) : "MLS Ztied1.H, Pg/M, Zop2.H, Zop3.H" or "MSB Ztied2.H, Pg/M, Zop3.H, Zop1.H" or "MSB Ztied3.H, Pg/M, Zop2.H, Zop1.H" or "MOVPRFX Zresult, Zop1; MLS Zresult.H, Pg/M, Zop2.H, Zop3.H"
    /// svint16_t svmls[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2, svint16_t op3) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; MLS Zresult.H, Pg/M, Zop2.H, Zop3.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; MSB Zresult.H, Pg/M, Zop3.H, Zop1.H" or "MOVPRFX Zresult.H, Pg/Z, Zop3.H; MSB Zresult.H, Pg/M, Zop2.H, Zop1.H"
  public static unsafe Vector<short> MultiplySubtract(Vector<short> minuend, Vector<short> left, Vector<short> right);

    /// svint32_t svmls[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2, svint32_t op3) : "MLS Ztied1.S, Pg/M, Zop2.S, Zop3.S" or "MOVPRFX Zresult, Zop1; MLS Zresult.S, Pg/M, Zop2.S, Zop3.S"
    /// svint32_t svmls[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2, svint32_t op3) : "MLS Ztied1.S, Pg/M, Zop2.S, Zop3.S" or "MSB Ztied2.S, Pg/M, Zop3.S, Zop1.S" or "MSB Ztied3.S, Pg/M, Zop2.S, Zop1.S" or "MOVPRFX Zresult, Zop1; MLS Zresult.S, Pg/M, Zop2.S, Zop3.S"
    /// svint32_t svmls[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2, svint32_t op3) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; MLS Zresult.S, Pg/M, Zop2.S, Zop3.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; MSB Zresult.S, Pg/M, Zop3.S, Zop1.S" or "MOVPRFX Zresult.S, Pg/Z, Zop3.S; MSB Zresult.S, Pg/M, Zop2.S, Zop1.S"
  public static unsafe Vector<int> MultiplySubtract(Vector<int> minuend, Vector<int> left, Vector<int> right);

    /// svint64_t svmls[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2, svint64_t op3) : "MLS Ztied1.D, Pg/M, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; MLS Zresult.D, Pg/M, Zop2.D, Zop3.D"
    /// svint64_t svmls[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2, svint64_t op3) : "MLS Ztied1.D, Pg/M, Zop2.D, Zop3.D" or "MSB Ztied2.D, Pg/M, Zop3.D, Zop1.D" or "MSB Ztied3.D, Pg/M, Zop2.D, Zop1.D" or "MOVPRFX Zresult, Zop1; MLS Zresult.D, Pg/M, Zop2.D, Zop3.D"
    /// svint64_t svmls[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2, svint64_t op3) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; MLS Zresult.D, Pg/M, Zop2.D, Zop3.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; MSB Zresult.D, Pg/M, Zop3.D, Zop1.D" or "MOVPRFX Zresult.D, Pg/Z, Zop3.D; MSB Zresult.D, Pg/M, Zop2.D, Zop1.D"
  public static unsafe Vector<long> MultiplySubtract(Vector<long> minuend, Vector<long> left, Vector<long> right);

    /// svuint8_t svmls[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2, svuint8_t op3) : "MLS Ztied1.B, Pg/M, Zop2.B, Zop3.B" or "MOVPRFX Zresult, Zop1; MLS Zresult.B, Pg/M, Zop2.B, Zop3.B"
    /// svuint8_t svmls[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2, svuint8_t op3) : "MLS Ztied1.B, Pg/M, Zop2.B, Zop3.B" or "MSB Ztied2.B, Pg/M, Zop3.B, Zop1.B" or "MSB Ztied3.B, Pg/M, Zop2.B, Zop1.B" or "MOVPRFX Zresult, Zop1; MLS Zresult.B, Pg/M, Zop2.B, Zop3.B"
    /// svuint8_t svmls[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2, svuint8_t op3) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; MLS Zresult.B, Pg/M, Zop2.B, Zop3.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; MSB Zresult.B, Pg/M, Zop3.B, Zop1.B" or "MOVPRFX Zresult.B, Pg/Z, Zop3.B; MSB Zresult.B, Pg/M, Zop2.B, Zop1.B"
  public static unsafe Vector<byte> MultiplySubtract(Vector<byte> minuend, Vector<byte> left, Vector<byte> right);

    /// svuint16_t svmls[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2, svuint16_t op3) : "MLS Ztied1.H, Pg/M, Zop2.H, Zop3.H" or "MOVPRFX Zresult, Zop1; MLS Zresult.H, Pg/M, Zop2.H, Zop3.H"
    /// svuint16_t svmls[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2, svuint16_t op3) : "MLS Ztied1.H, Pg/M, Zop2.H, Zop3.H" or "MSB Ztied2.H, Pg/M, Zop3.H, Zop1.H" or "MSB Ztied3.H, Pg/M, Zop2.H, Zop1.H" or "MOVPRFX Zresult, Zop1; MLS Zresult.H, Pg/M, Zop2.H, Zop3.H"
    /// svuint16_t svmls[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2, svuint16_t op3) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; MLS Zresult.H, Pg/M, Zop2.H, Zop3.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; MSB Zresult.H, Pg/M, Zop3.H, Zop1.H" or "MOVPRFX Zresult.H, Pg/Z, Zop3.H; MSB Zresult.H, Pg/M, Zop2.H, Zop1.H"
  public static unsafe Vector<ushort> MultiplySubtract(Vector<ushort> minuend, Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svmls[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2, svuint32_t op3) : "MLS Ztied1.S, Pg/M, Zop2.S, Zop3.S" or "MOVPRFX Zresult, Zop1; MLS Zresult.S, Pg/M, Zop2.S, Zop3.S"
    /// svuint32_t svmls[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2, svuint32_t op3) : "MLS Ztied1.S, Pg/M, Zop2.S, Zop3.S" or "MSB Ztied2.S, Pg/M, Zop3.S, Zop1.S" or "MSB Ztied3.S, Pg/M, Zop2.S, Zop1.S" or "MOVPRFX Zresult, Zop1; MLS Zresult.S, Pg/M, Zop2.S, Zop3.S"
    /// svuint32_t svmls[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2, svuint32_t op3) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; MLS Zresult.S, Pg/M, Zop2.S, Zop3.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; MSB Zresult.S, Pg/M, Zop3.S, Zop1.S" or "MOVPRFX Zresult.S, Pg/Z, Zop3.S; MSB Zresult.S, Pg/M, Zop2.S, Zop1.S"
  public static unsafe Vector<uint> MultiplySubtract(Vector<uint> minuend, Vector<uint> left, Vector<uint> right);

    /// svuint64_t svmls[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2, svuint64_t op3) : "MLS Ztied1.D, Pg/M, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; MLS Zresult.D, Pg/M, Zop2.D, Zop3.D"
    /// svuint64_t svmls[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2, svuint64_t op3) : "MLS Ztied1.D, Pg/M, Zop2.D, Zop3.D" or "MSB Ztied2.D, Pg/M, Zop3.D, Zop1.D" or "MSB Ztied3.D, Pg/M, Zop2.D, Zop1.D" or "MOVPRFX Zresult, Zop1; MLS Zresult.D, Pg/M, Zop2.D, Zop3.D"
    /// svuint64_t svmls[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2, svuint64_t op3) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; MLS Zresult.D, Pg/M, Zop2.D, Zop3.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; MSB Zresult.D, Pg/M, Zop3.D, Zop1.D" or "MOVPRFX Zresult.D, Pg/Z, Zop3.D; MSB Zresult.D, Pg/M, Zop2.D, Zop1.D"
  public static unsafe Vector<ulong> MultiplySubtract(Vector<ulong> minuend, Vector<ulong> left, Vector<ulong> right);


    /// Negate : Negate

    /// svfloat32_t svneg[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op) : "FNEG Ztied.S, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; FNEG Zresult.S, Pg/M, Zop.S"
    /// svfloat32_t svneg[_f32]_x(svbool_t pg, svfloat32_t op) : "FNEG Ztied.S, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; FNEG Zresult.S, Pg/M, Zop.S"
    /// svfloat32_t svneg[_f32]_z(svbool_t pg, svfloat32_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; FNEG Zresult.S, Pg/M, Zop.S"
  public static unsafe Vector<float> Negate(Vector<float> value);

    /// svfloat64_t svneg[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op) : "FNEG Ztied.D, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; FNEG Zresult.D, Pg/M, Zop.D"
    /// svfloat64_t svneg[_f64]_x(svbool_t pg, svfloat64_t op) : "FNEG Ztied.D, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; FNEG Zresult.D, Pg/M, Zop.D"
    /// svfloat64_t svneg[_f64]_z(svbool_t pg, svfloat64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; FNEG Zresult.D, Pg/M, Zop.D"
  public static unsafe Vector<double> Negate(Vector<double> value);

    /// svint8_t svneg[_s8]_m(svint8_t inactive, svbool_t pg, svint8_t op) : "NEG Ztied.B, Pg/M, Zop.B" or "MOVPRFX Zresult, Zinactive; NEG Zresult.B, Pg/M, Zop.B"
    /// svint8_t svneg[_s8]_x(svbool_t pg, svint8_t op) : "NEG Ztied.B, Pg/M, Ztied.B" or "MOVPRFX Zresult, Zop; NEG Zresult.B, Pg/M, Zop.B"
    /// svint8_t svneg[_s8]_z(svbool_t pg, svint8_t op) : "MOVPRFX Zresult.B, Pg/Z, Zop.B; NEG Zresult.B, Pg/M, Zop.B"
  public static unsafe Vector<sbyte> Negate(Vector<sbyte> value);

    /// svint16_t svneg[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op) : "NEG Ztied.H, Pg/M, Zop.H" or "MOVPRFX Zresult, Zinactive; NEG Zresult.H, Pg/M, Zop.H"
    /// svint16_t svneg[_s16]_x(svbool_t pg, svint16_t op) : "NEG Ztied.H, Pg/M, Ztied.H" or "MOVPRFX Zresult, Zop; NEG Zresult.H, Pg/M, Zop.H"
    /// svint16_t svneg[_s16]_z(svbool_t pg, svint16_t op) : "MOVPRFX Zresult.H, Pg/Z, Zop.H; NEG Zresult.H, Pg/M, Zop.H"
  public static unsafe Vector<short> Negate(Vector<short> value);

    /// svint32_t svneg[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op) : "NEG Ztied.S, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; NEG Zresult.S, Pg/M, Zop.S"
    /// svint32_t svneg[_s32]_x(svbool_t pg, svint32_t op) : "NEG Ztied.S, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; NEG Zresult.S, Pg/M, Zop.S"
    /// svint32_t svneg[_s32]_z(svbool_t pg, svint32_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; NEG Zresult.S, Pg/M, Zop.S"
  public static unsafe Vector<int> Negate(Vector<int> value);

    /// svint64_t svneg[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op) : "NEG Ztied.D, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; NEG Zresult.D, Pg/M, Zop.D"
    /// svint64_t svneg[_s64]_x(svbool_t pg, svint64_t op) : "NEG Ztied.D, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; NEG Zresult.D, Pg/M, Zop.D"
    /// svint64_t svneg[_s64]_z(svbool_t pg, svint64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; NEG Zresult.D, Pg/M, Zop.D"
  public static unsafe Vector<long> Negate(Vector<long> value);


    /// SignExtend16 : Sign-extend the low 16 bits

    /// svint32_t svexth[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op) : "SXTH Ztied.S, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; SXTH Zresult.S, Pg/M, Zop.S"
    /// svint32_t svexth[_s32]_x(svbool_t pg, svint32_t op) : "SXTH Ztied.S, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; SXTH Zresult.S, Pg/M, Zop.S"
    /// svint32_t svexth[_s32]_z(svbool_t pg, svint32_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; SXTH Zresult.S, Pg/M, Zop.S"
  public static unsafe Vector<int> SignExtend16(Vector<int> value);

    /// svint64_t svexth[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op) : "SXTH Ztied.D, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; SXTH Zresult.D, Pg/M, Zop.D"
    /// svint64_t svexth[_s64]_x(svbool_t pg, svint64_t op) : "SXTH Ztied.D, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; SXTH Zresult.D, Pg/M, Zop.D"
    /// svint64_t svexth[_s64]_z(svbool_t pg, svint64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; SXTH Zresult.D, Pg/M, Zop.D"
  public static unsafe Vector<long> SignExtend16(Vector<long> value);


    /// SignExtend32 : Sign-extend the low 32 bits

    /// svint64_t svextw[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op) : "SXTW Ztied.D, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; SXTW Zresult.D, Pg/M, Zop.D"
    /// svint64_t svextw[_s64]_x(svbool_t pg, svint64_t op) : "SXTW Ztied.D, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; SXTW Zresult.D, Pg/M, Zop.D"
    /// svint64_t svextw[_s64]_z(svbool_t pg, svint64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; SXTW Zresult.D, Pg/M, Zop.D"
  public static unsafe Vector<long> SignExtend32(Vector<long> value);


    /// SignExtend8 : Sign-extend the low 8 bits

    /// svint16_t svextb[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op) : "SXTB Ztied.H, Pg/M, Zop.H" or "MOVPRFX Zresult, Zinactive; SXTB Zresult.H, Pg/M, Zop.H"
    /// svint16_t svextb[_s16]_x(svbool_t pg, svint16_t op) : "SXTB Ztied.H, Pg/M, Ztied.H" or "MOVPRFX Zresult, Zop; SXTB Zresult.H, Pg/M, Zop.H"
    /// svint16_t svextb[_s16]_z(svbool_t pg, svint16_t op) : "MOVPRFX Zresult.H, Pg/Z, Zop.H; SXTB Zresult.H, Pg/M, Zop.H"
  public static unsafe Vector<short> SignExtend8(Vector<short> value);

    /// svint32_t svextb[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op) : "SXTB Ztied.S, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; SXTB Zresult.S, Pg/M, Zop.S"
    /// svint32_t svextb[_s32]_x(svbool_t pg, svint32_t op) : "SXTB Ztied.S, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; SXTB Zresult.S, Pg/M, Zop.S"
    /// svint32_t svextb[_s32]_z(svbool_t pg, svint32_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; SXTB Zresult.S, Pg/M, Zop.S"
  public static unsafe Vector<int> SignExtend8(Vector<int> value);

    /// svint64_t svextb[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op) : "SXTB Ztied.D, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; SXTB Zresult.D, Pg/M, Zop.D"
    /// svint64_t svextb[_s64]_x(svbool_t pg, svint64_t op) : "SXTB Ztied.D, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; SXTB Zresult.D, Pg/M, Zop.D"
    /// svint64_t svextb[_s64]_z(svbool_t pg, svint64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; SXTB Zresult.D, Pg/M, Zop.D"
  public static unsafe Vector<long> SignExtend8(Vector<long> value);


    /// SignExtendWideningLower : Unpack and extend low half

    /// svint16_t svunpklo[_s16](svint8_t op) : "SUNPKLO Zresult.H, Zop.B"
  public static unsafe Vector<short> SignExtendWideningLower(Vector<sbyte> value);

    /// svint32_t svunpklo[_s32](svint16_t op) : "SUNPKLO Zresult.S, Zop.H"
  public static unsafe Vector<int> SignExtendWideningLower(Vector<short> value);

    /// svint64_t svunpklo[_s64](svint32_t op) : "SUNPKLO Zresult.D, Zop.S"
  public static unsafe Vector<long> SignExtendWideningLower(Vector<int> value);


    /// SignExtendWideningUpper : Unpack and extend high half

    /// svint16_t svunpkhi[_s16](svint8_t op) : "SUNPKHI Zresult.H, Zop.B"
  public static unsafe Vector<short> SignExtendWideningUpper(Vector<sbyte> value);

    /// svint32_t svunpkhi[_s32](svint16_t op) : "SUNPKHI Zresult.S, Zop.H"
  public static unsafe Vector<int> SignExtendWideningUpper(Vector<short> value);

    /// svint64_t svunpkhi[_s64](svint32_t op) : "SUNPKHI Zresult.D, Zop.S"
  public static unsafe Vector<long> SignExtendWideningUpper(Vector<int> value);


    /// Subtract : Subtract

    /// svfloat32_t svsub[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "FSUB Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; FSUB Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svfloat32_t svsub[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "FSUB Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "FSUBR Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "FSUB Zresult.S, Zop1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; FSUB Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svfloat32_t svsub[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; FSUB Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; FSUBR Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<float> Subtract(Vector<float> left, Vector<float> right);

    /// svfloat64_t svsub[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "FSUB Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; FSUB Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svfloat64_t svsub[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "FSUB Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "FSUBR Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "FSUB Zresult.D, Zop1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; FSUB Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svfloat64_t svsub[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; FSUB Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; FSUBR Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<double> Subtract(Vector<double> left, Vector<double> right);

    /// svint8_t svsub[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2) : "SUB Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; SUB Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svint8_t svsub[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2) : "SUB Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "SUBR Ztied2.B, Pg/M, Ztied2.B, Zop1.B" or "SUB Zresult.B, Zop1.B, Zop2.B"
    /// svint8_t svsub[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; SUB Zresult.B, Pg/M, Zresult.B, Zop2.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; SUBR Zresult.B, Pg/M, Zresult.B, Zop1.B"
  public static unsafe Vector<sbyte> Subtract(Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t svsub[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2) : "SUB Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; SUB Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svint16_t svsub[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2) : "SUB Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "SUBR Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "SUB Zresult.H, Zop1.H, Zop2.H"
    /// svint16_t svsub[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; SUB Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; SUBR Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<short> Subtract(Vector<short> left, Vector<short> right);

    /// svint32_t svsub[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2) : "SUB Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; SUB Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svint32_t svsub[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2) : "SUB Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "SUBR Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "SUB Zresult.S, Zop1.S, Zop2.S"
    /// svint32_t svsub[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; SUB Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; SUBR Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<int> Subtract(Vector<int> left, Vector<int> right);

    /// svint64_t svsub[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2) : "SUB Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; SUB Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svint64_t svsub[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2) : "SUB Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "SUBR Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "SUB Zresult.D, Zop1.D, Zop2.D"
    /// svint64_t svsub[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; SUB Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; SUBR Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<long> Subtract(Vector<long> left, Vector<long> right);

    /// svuint8_t svsub[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2) : "SUB Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; SUB Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svuint8_t svsub[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2) : "SUB Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "SUBR Ztied2.B, Pg/M, Ztied2.B, Zop1.B" or "SUB Zresult.B, Zop1.B, Zop2.B"
    /// svuint8_t svsub[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; SUB Zresult.B, Pg/M, Zresult.B, Zop2.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; SUBR Zresult.B, Pg/M, Zresult.B, Zop1.B"
  public static unsafe Vector<byte> Subtract(Vector<byte> left, Vector<byte> right);

    /// svuint16_t svsub[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2) : "SUB Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; SUB Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svuint16_t svsub[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2) : "SUB Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "SUBR Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "SUB Zresult.H, Zop1.H, Zop2.H"
    /// svuint16_t svsub[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; SUB Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; SUBR Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<ushort> Subtract(Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svsub[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2) : "SUB Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; SUB Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svuint32_t svsub[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2) : "SUB Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "SUBR Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "SUB Zresult.S, Zop1.S, Zop2.S"
    /// svuint32_t svsub[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; SUB Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; SUBR Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<uint> Subtract(Vector<uint> left, Vector<uint> right);

    /// svuint64_t svsub[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2) : "SUB Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; SUB Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svuint64_t svsub[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2) : "SUB Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "SUBR Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "SUB Zresult.D, Zop1.D, Zop2.D"
    /// svuint64_t svsub[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; SUB Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; SUBR Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<ulong> Subtract(Vector<ulong> left, Vector<ulong> right);


    /// SubtractSaturate : Saturating subtract

    /// svint8_t svqsub[_s8](svint8_t op1, svint8_t op2) : "SQSUB Zresult.B, Zop1.B, Zop2.B"
  public static unsafe Vector<sbyte> SubtractSaturate(Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t svqsub[_s16](svint16_t op1, svint16_t op2) : "SQSUB Zresult.H, Zop1.H, Zop2.H"
  public static unsafe Vector<short> SubtractSaturate(Vector<short> left, Vector<short> right);

    /// svint32_t svqsub[_s32](svint32_t op1, svint32_t op2) : "SQSUB Zresult.S, Zop1.S, Zop2.S"
  public static unsafe Vector<int> SubtractSaturate(Vector<int> left, Vector<int> right);

    /// svint64_t svqsub[_s64](svint64_t op1, svint64_t op2) : "SQSUB Zresult.D, Zop1.D, Zop2.D"
  public static unsafe Vector<long> SubtractSaturate(Vector<long> left, Vector<long> right);

    /// svuint8_t svqsub[_u8](svuint8_t op1, svuint8_t op2) : "UQSUB Zresult.B, Zop1.B, Zop2.B"
  public static unsafe Vector<byte> SubtractSaturate(Vector<byte> left, Vector<byte> right);

    /// svuint16_t svqsub[_u16](svuint16_t op1, svuint16_t op2) : "UQSUB Zresult.H, Zop1.H, Zop2.H"
  public static unsafe Vector<ushort> SubtractSaturate(Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svqsub[_u32](svuint32_t op1, svuint32_t op2) : "UQSUB Zresult.S, Zop1.S, Zop2.S"
  public static unsafe Vector<uint> SubtractSaturate(Vector<uint> left, Vector<uint> right);

    /// svuint64_t svqsub[_u64](svuint64_t op1, svuint64_t op2) : "UQSUB Zresult.D, Zop1.D, Zop2.D"
  public static unsafe Vector<ulong> SubtractSaturate(Vector<ulong> left, Vector<ulong> right);


    /// ZeroExtend16 : Zero-extend the low 16 bits

    /// svuint32_t svexth[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op) : "UXTH Ztied.S, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; UXTH Zresult.S, Pg/M, Zop.S"
    /// svuint32_t svexth[_u32]_x(svbool_t pg, svuint32_t op) : "UXTH Ztied.S, Pg/M, Ztied.S" or "AND Ztied.S, Ztied.S, #65535"
    /// svuint32_t svexth[_u32]_z(svbool_t pg, svuint32_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; UXTH Zresult.S, Pg/M, Zop.S"
  public static unsafe Vector<uint> ZeroExtend16(Vector<uint> value);

    /// svuint64_t svexth[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op) : "UXTH Ztied.D, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; UXTH Zresult.D, Pg/M, Zop.D"
    /// svuint64_t svexth[_u64]_x(svbool_t pg, svuint64_t op) : "UXTH Ztied.D, Pg/M, Ztied.D" or "AND Ztied.D, Ztied.D, #65535"
    /// svuint64_t svexth[_u64]_z(svbool_t pg, svuint64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; UXTH Zresult.D, Pg/M, Zop.D"
  public static unsafe Vector<ulong> ZeroExtend16(Vector<ulong> value);


    /// ZeroExtend32 : Zero-extend the low 32 bits

    /// svuint64_t svextw[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op) : "UXTW Ztied.D, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; UXTW Zresult.D, Pg/M, Zop.D"
    /// svuint64_t svextw[_u64]_x(svbool_t pg, svuint64_t op) : "UXTW Ztied.D, Pg/M, Ztied.D" or "AND Ztied.D, Ztied.D, #4294967295"
    /// svuint64_t svextw[_u64]_z(svbool_t pg, svuint64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; UXTW Zresult.D, Pg/M, Zop.D"
  public static unsafe Vector<ulong> ZeroExtend32(Vector<ulong> value);


    /// ZeroExtend8 : Zero-extend the low 8 bits

    /// svuint16_t svextb[_u16]_m(svuint16_t inactive, svbool_t pg, svuint16_t op) : "UXTB Ztied.H, Pg/M, Zop.H" or "MOVPRFX Zresult, Zinactive; UXTB Zresult.H, Pg/M, Zop.H"
    /// svuint16_t svextb[_u16]_x(svbool_t pg, svuint16_t op) : "UXTB Ztied.H, Pg/M, Ztied.H" or "AND Ztied.H, Ztied.H, #255"
    /// svuint16_t svextb[_u16]_z(svbool_t pg, svuint16_t op) : "MOVPRFX Zresult.H, Pg/Z, Zop.H; UXTB Zresult.H, Pg/M, Zop.H"
  public static unsafe Vector<ushort> ZeroExtend8(Vector<ushort> value);

    /// svuint32_t svextb[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op) : "UXTB Ztied.S, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; UXTB Zresult.S, Pg/M, Zop.S"
    /// svuint32_t svextb[_u32]_x(svbool_t pg, svuint32_t op) : "UXTB Ztied.S, Pg/M, Ztied.S" or "AND Ztied.S, Ztied.S, #255"
    /// svuint32_t svextb[_u32]_z(svbool_t pg, svuint32_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; UXTB Zresult.S, Pg/M, Zop.S"
  public static unsafe Vector<uint> ZeroExtend8(Vector<uint> value);

    /// svuint64_t svextb[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op) : "UXTB Ztied.D, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; UXTB Zresult.D, Pg/M, Zop.D"
    /// svuint64_t svextb[_u64]_x(svbool_t pg, svuint64_t op) : "UXTB Ztied.D, Pg/M, Ztied.D" or "AND Ztied.D, Ztied.D, #255"
    /// svuint64_t svextb[_u64]_z(svbool_t pg, svuint64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; UXTB Zresult.D, Pg/M, Zop.D"
  public static unsafe Vector<ulong> ZeroExtend8(Vector<ulong> value);


    /// ZeroExtendWideningLower : Unpack and extend low half

    /// svuint16_t svunpklo[_u16](svuint8_t op) : "UUNPKLO Zresult.H, Zop.B"
    /// svbool_t svunpklo[_b](svbool_t op) : "PUNPKLO Presult.H, Pop.B"
  public static unsafe Vector<ushort> ZeroExtendWideningLower(Vector<byte> value);

    /// svuint32_t svunpklo[_u32](svuint16_t op) : "UUNPKLO Zresult.S, Zop.H"
    /// svbool_t svunpklo[_b](svbool_t op) : "PUNPKLO Presult.H, Pop.B"
  public static unsafe Vector<uint> ZeroExtendWideningLower(Vector<ushort> value);

    /// svuint64_t svunpklo[_u64](svuint32_t op) : "UUNPKLO Zresult.D, Zop.S"
    /// svbool_t svunpklo[_b](svbool_t op) : "PUNPKLO Presult.H, Pop.B"
  public static unsafe Vector<ulong> ZeroExtendWideningLower(Vector<uint> value);


    /// ZeroExtendWideningUpper : Unpack and extend high half

    /// svuint16_t svunpkhi[_u16](svuint8_t op) : "UUNPKHI Zresult.H, Zop.B"
    /// svbool_t svunpkhi[_b](svbool_t op) : "PUNPKHI Presult.H, Pop.B"
  public static unsafe Vector<ushort> ZeroExtendWideningUpper(Vector<byte> value);

    /// svuint32_t svunpkhi[_u32](svuint16_t op) : "UUNPKHI Zresult.S, Zop.H"
    /// svbool_t svunpkhi[_b](svbool_t op) : "PUNPKHI Presult.H, Pop.B"
  public static unsafe Vector<uint> ZeroExtendWideningUpper(Vector<ushort> value);

    /// svuint64_t svunpkhi[_u64](svuint32_t op) : "UUNPKHI Zresult.D, Zop.S"
    /// svbool_t svunpkhi[_b](svbool_t op) : "PUNPKHI Presult.H, Pop.B"
  public static unsafe Vector<ulong> ZeroExtendWideningUpper(Vector<uint> value);


  /// total method signatures: 196
  /// total method names:      49
}

  /// Optional Entries:
  ///   public static unsafe Vector<sbyte> MultiplyReturningHighHalf(Vector<sbyte> left, Vector<sbyte> right); // svmulh[_s8]_m or svmulh[_s8]_x or svmulh[_s8]_z
  ///   public static unsafe Vector<short> MultiplyReturningHighHalf(Vector<short> left, Vector<short> right); // svmulh[_s16]_m or svmulh[_s16]_x or svmulh[_s16]_z
  ///   public static unsafe Vector<int> MultiplyReturningHighHalf(Vector<int> left, Vector<int> right); // svmulh[_s32]_m or svmulh[_s32]_x or svmulh[_s32]_z
  ///   public static unsafe Vector<long> MultiplyReturningHighHalf(Vector<long> left, Vector<long> right); // svmulh[_s64]_m or svmulh[_s64]_x or svmulh[_s64]_z
  ///   public static unsafe Vector<byte> MultiplyReturningHighHalf(Vector<byte> left, Vector<byte> right); // svmulh[_u8]_m or svmulh[_u8]_x or svmulh[_u8]_z
  ///   public static unsafe Vector<ushort> MultiplyReturningHighHalf(Vector<ushort> left, Vector<ushort> right); // svmulh[_u16]_m or svmulh[_u16]_x or svmulh[_u16]_z
  ///   public static unsafe Vector<uint> MultiplyReturningHighHalf(Vector<uint> left, Vector<uint> right); // svmulh[_u32]_m or svmulh[_u32]_x or svmulh[_u32]_z
  ///   public static unsafe Vector<ulong> MultiplyReturningHighHalf(Vector<ulong> left, Vector<ulong> right); // svmulh[_u64]_m or svmulh[_u64]_x or svmulh[_u64]_z
  ///   public static unsafe Vector<sbyte> MultiplyReturningHighHalf(Vector<sbyte> left, sbyte right); // svmulh[_n_s8]_m or svmulh[_n_s8]_x or svmulh[_n_s8]_z
  ///   public static unsafe Vector<short> MultiplyReturningHighHalf(Vector<short> left, short right); // svmulh[_n_s16]_m or svmulh[_n_s16]_x or svmulh[_n_s16]_z
  ///   public static unsafe Vector<int> MultiplyReturningHighHalf(Vector<int> left, int right); // svmulh[_n_s32]_m or svmulh[_n_s32]_x or svmulh[_n_s32]_z
  ///   public static unsafe Vector<long> MultiplyReturningHighHalf(Vector<long> left, long right); // svmulh[_n_s64]_m or svmulh[_n_s64]_x or svmulh[_n_s64]_z
  ///   public static unsafe Vector<byte> MultiplyReturningHighHalf(Vector<byte> left, byte right); // svmulh[_n_u8]_m or svmulh[_n_u8]_x or svmulh[_n_u8]_z
  ///   public static unsafe Vector<ushort> MultiplyReturningHighHalf(Vector<ushort> left, ushort right); // svmulh[_n_u16]_m or svmulh[_n_u16]_x or svmulh[_n_u16]_z
  ///   public static unsafe Vector<uint> MultiplyReturningHighHalf(Vector<uint> left, uint right); // svmulh[_n_u32]_m or svmulh[_n_u32]_x or svmulh[_n_u32]_z
  ///   public static unsafe Vector<ulong> MultiplyReturningHighHalf(Vector<ulong> left, ulong right); // svmulh[_n_u64]_m or svmulh[_n_u64]_x or svmulh[_n_u64]_z
  ///   Total Maybe: 16

  /// Rejected:
  ///   public static unsafe Vector<float> AbsoluteDifference(Vector<float> left, float right); // svabd[_n_f32]_m or svabd[_n_f32]_x or svabd[_n_f32]_z
  ///   public static unsafe Vector<double> AbsoluteDifference(Vector<double> left, double right); // svabd[_n_f64]_m or svabd[_n_f64]_x or svabd[_n_f64]_z
  ///   public static unsafe Vector<sbyte> AbsoluteDifference(Vector<sbyte> left, sbyte right); // svabd[_n_s8]_m or svabd[_n_s8]_x or svabd[_n_s8]_z
  ///   public static unsafe Vector<short> AbsoluteDifference(Vector<short> left, short right); // svabd[_n_s16]_m or svabd[_n_s16]_x or svabd[_n_s16]_z
  ///   public static unsafe Vector<int> AbsoluteDifference(Vector<int> left, int right); // svabd[_n_s32]_m or svabd[_n_s32]_x or svabd[_n_s32]_z
  ///   public static unsafe Vector<long> AbsoluteDifference(Vector<long> left, long right); // svabd[_n_s64]_m or svabd[_n_s64]_x or svabd[_n_s64]_z
  ///   public static unsafe Vector<byte> AbsoluteDifference(Vector<byte> left, byte right); // svabd[_n_u8]_m or svabd[_n_u8]_x or svabd[_n_u8]_z
  ///   public static unsafe Vector<ushort> AbsoluteDifference(Vector<ushort> left, ushort right); // svabd[_n_u16]_m or svabd[_n_u16]_x or svabd[_n_u16]_z
  ///   public static unsafe Vector<uint> AbsoluteDifference(Vector<uint> left, uint right); // svabd[_n_u32]_m or svabd[_n_u32]_x or svabd[_n_u32]_z
  ///   public static unsafe Vector<ulong> AbsoluteDifference(Vector<ulong> left, ulong right); // svabd[_n_u64]_m or svabd[_n_u64]_x or svabd[_n_u64]_z
  ///   public static unsafe Vector<float> Add(Vector<float> left, float right); // svadd[_n_f32]_m or svadd[_n_f32]_x or svadd[_n_f32]_z
  ///   public static unsafe Vector<double> Add(Vector<double> left, double right); // svadd[_n_f64]_m or svadd[_n_f64]_x or svadd[_n_f64]_z
  ///   public static unsafe Vector<sbyte> Add(Vector<sbyte> left, sbyte right); // svadd[_n_s8]_m or svadd[_n_s8]_x or svadd[_n_s8]_z
  ///   public static unsafe Vector<short> Add(Vector<short> left, short right); // svadd[_n_s16]_m or svadd[_n_s16]_x or svadd[_n_s16]_z
  ///   public static unsafe Vector<int> Add(Vector<int> left, int right); // svadd[_n_s32]_m or svadd[_n_s32]_x or svadd[_n_s32]_z
  ///   public static unsafe Vector<long> Add(Vector<long> left, long right); // svadd[_n_s64]_m or svadd[_n_s64]_x or svadd[_n_s64]_z
  ///   public static unsafe Vector<byte> Add(Vector<byte> left, byte right); // svadd[_n_u8]_m or svadd[_n_u8]_x or svadd[_n_u8]_z
  ///   public static unsafe Vector<ushort> Add(Vector<ushort> left, ushort right); // svadd[_n_u16]_m or svadd[_n_u16]_x or svadd[_n_u16]_z
  ///   public static unsafe Vector<uint> Add(Vector<uint> left, uint right); // svadd[_n_u32]_m or svadd[_n_u32]_x or svadd[_n_u32]_z
  ///   public static unsafe Vector<ulong> Add(Vector<ulong> left, ulong right); // svadd[_n_u64]_m or svadd[_n_u64]_x or svadd[_n_u64]_z
  ///   public static unsafe Vector<sbyte> AddSaturate(Vector<sbyte> left, sbyte right); // svqadd[_n_s8]
  ///   public static unsafe Vector<short> AddSaturate(Vector<short> left, short right); // svqadd[_n_s16]
  ///   public static unsafe Vector<int> AddSaturate(Vector<int> left, int right); // svqadd[_n_s32]
  ///   public static unsafe Vector<long> AddSaturate(Vector<long> left, long right); // svqadd[_n_s64]
  ///   public static unsafe Vector<byte> AddSaturate(Vector<byte> left, byte right); // svqadd[_n_u8]
  ///   public static unsafe Vector<ushort> AddSaturate(Vector<ushort> left, ushort right); // svqadd[_n_u16]
  ///   public static unsafe Vector<uint> AddSaturate(Vector<uint> left, uint right); // svqadd[_n_u32]
  ///   public static unsafe Vector<ulong> AddSaturate(Vector<ulong> left, ulong right); // svqadd[_n_u64]
  ///   public static unsafe Vector<float> Divide(Vector<float> left, float right); // svdiv[_n_f32]_m or svdiv[_n_f32]_x or svdiv[_n_f32]_z
  ///   public static unsafe Vector<double> Divide(Vector<double> left, double right); // svdiv[_n_f64]_m or svdiv[_n_f64]_x or svdiv[_n_f64]_z
  ///   public static unsafe Vector<int> Divide(Vector<int> left, int right); // svdiv[_n_s32]_m or svdiv[_n_s32]_x or svdiv[_n_s32]_z
  ///   public static unsafe Vector<long> Divide(Vector<long> left, long right); // svdiv[_n_s64]_m or svdiv[_n_s64]_x or svdiv[_n_s64]_z
  ///   public static unsafe Vector<uint> Divide(Vector<uint> left, uint right); // svdiv[_n_u32]_m or svdiv[_n_u32]_x or svdiv[_n_u32]_z
  ///   public static unsafe Vector<ulong> Divide(Vector<ulong> left, ulong right); // svdiv[_n_u64]_m or svdiv[_n_u64]_x or svdiv[_n_u64]_z
  ///   public static unsafe Vector<float> DivideReversed(Vector<float> left, Vector<float> right); // svdivr[_f32]_m or svdivr[_f32]_x or svdivr[_f32]_z
  ///   public static unsafe Vector<double> DivideReversed(Vector<double> left, Vector<double> right); // svdivr[_f64]_m or svdivr[_f64]_x or svdivr[_f64]_z
  ///   public static unsafe Vector<int> DivideReversed(Vector<int> left, Vector<int> right); // svdivr[_s32]_m or svdivr[_s32]_x or svdivr[_s32]_z
  ///   public static unsafe Vector<long> DivideReversed(Vector<long> left, Vector<long> right); // svdivr[_s64]_m or svdivr[_s64]_x or svdivr[_s64]_z
  ///   public static unsafe Vector<uint> DivideReversed(Vector<uint> left, Vector<uint> right); // svdivr[_u32]_m or svdivr[_u32]_x or svdivr[_u32]_z
  ///   public static unsafe Vector<ulong> DivideReversed(Vector<ulong> left, Vector<ulong> right); // svdivr[_u64]_m or svdivr[_u64]_x or svdivr[_u64]_z
  ///   public static unsafe Vector<float> DivideReversed(Vector<float> left, float right); // svdivr[_n_f32]_m or svdivr[_n_f32]_x or svdivr[_n_f32]_z
  ///   public static unsafe Vector<double> DivideReversed(Vector<double> left, double right); // svdivr[_n_f64]_m or svdivr[_n_f64]_x or svdivr[_n_f64]_z
  ///   public static unsafe Vector<int> DivideReversed(Vector<int> left, int right); // svdivr[_n_s32]_m or svdivr[_n_s32]_x or svdivr[_n_s32]_z
  ///   public static unsafe Vector<long> DivideReversed(Vector<long> left, long right); // svdivr[_n_s64]_m or svdivr[_n_s64]_x or svdivr[_n_s64]_z
  ///   public static unsafe Vector<uint> DivideReversed(Vector<uint> left, uint right); // svdivr[_n_u32]_m or svdivr[_n_u32]_x or svdivr[_n_u32]_z
  ///   public static unsafe Vector<ulong> DivideReversed(Vector<ulong> left, ulong right); // svdivr[_n_u64]_m or svdivr[_n_u64]_x or svdivr[_n_u64]_z
  ///   public static unsafe Vector<int> DotProduct(Vector<int> addend, Vector<sbyte> left, sbyte right); // svdot[_n_s32]
  ///   public static unsafe Vector<long> DotProduct(Vector<long> addend, Vector<short> left, short right); // svdot[_n_s64]
  ///   public static unsafe Vector<uint> DotProduct(Vector<uint> addend, Vector<byte> left, byte right); // svdot[_n_u32]
  ///   public static unsafe Vector<ulong> DotProduct(Vector<ulong> addend, Vector<ushort> left, ushort right); // svdot[_n_u64]
  ///   public static unsafe Vector<float> Max(Vector<float> left, float right); // svmax[_n_f32]_m or svmax[_n_f32]_x or svmax[_n_f32]_z
  ///   public static unsafe Vector<double> Max(Vector<double> left, double right); // svmax[_n_f64]_m or svmax[_n_f64]_x or svmax[_n_f64]_z
  ///   public static unsafe Vector<sbyte> Max(Vector<sbyte> left, sbyte right); // svmax[_n_s8]_m or svmax[_n_s8]_x or svmax[_n_s8]_z
  ///   public static unsafe Vector<short> Max(Vector<short> left, short right); // svmax[_n_s16]_m or svmax[_n_s16]_x or svmax[_n_s16]_z
  ///   public static unsafe Vector<int> Max(Vector<int> left, int right); // svmax[_n_s32]_m or svmax[_n_s32]_x or svmax[_n_s32]_z
  ///   public static unsafe Vector<long> Max(Vector<long> left, long right); // svmax[_n_s64]_m or svmax[_n_s64]_x or svmax[_n_s64]_z
  ///   public static unsafe Vector<byte> Max(Vector<byte> left, byte right); // svmax[_n_u8]_m or svmax[_n_u8]_x or svmax[_n_u8]_z
  ///   public static unsafe Vector<ushort> Max(Vector<ushort> left, ushort right); // svmax[_n_u16]_m or svmax[_n_u16]_x or svmax[_n_u16]_z
  ///   public static unsafe Vector<uint> Max(Vector<uint> left, uint right); // svmax[_n_u32]_m or svmax[_n_u32]_x or svmax[_n_u32]_z
  ///   public static unsafe Vector<ulong> Max(Vector<ulong> left, ulong right); // svmax[_n_u64]_m or svmax[_n_u64]_x or svmax[_n_u64]_z
  ///   public static unsafe Vector<float> MaxNumber(Vector<float> left, float right); // svmaxnm[_n_f32]_m or svmaxnm[_n_f32]_x or svmaxnm[_n_f32]_z
  ///   public static unsafe Vector<double> MaxNumber(Vector<double> left, double right); // svmaxnm[_n_f64]_m or svmaxnm[_n_f64]_x or svmaxnm[_n_f64]_z
  ///   public static unsafe Vector<float> Min(Vector<float> left, float right); // svmin[_n_f32]_m or svmin[_n_f32]_x or svmin[_n_f32]_z
  ///   public static unsafe Vector<double> Min(Vector<double> left, double right); // svmin[_n_f64]_m or svmin[_n_f64]_x or svmin[_n_f64]_z
  ///   public static unsafe Vector<sbyte> Min(Vector<sbyte> left, sbyte right); // svmin[_n_s8]_m or svmin[_n_s8]_x or svmin[_n_s8]_z
  ///   public static unsafe Vector<short> Min(Vector<short> left, short right); // svmin[_n_s16]_m or svmin[_n_s16]_x or svmin[_n_s16]_z
  ///   public static unsafe Vector<int> Min(Vector<int> left, int right); // svmin[_n_s32]_m or svmin[_n_s32]_x or svmin[_n_s32]_z
  ///   public static unsafe Vector<long> Min(Vector<long> left, long right); // svmin[_n_s64]_m or svmin[_n_s64]_x or svmin[_n_s64]_z
  ///   public static unsafe Vector<byte> Min(Vector<byte> left, byte right); // svmin[_n_u8]_m or svmin[_n_u8]_x or svmin[_n_u8]_z
  ///   public static unsafe Vector<ushort> Min(Vector<ushort> left, ushort right); // svmin[_n_u16]_m or svmin[_n_u16]_x or svmin[_n_u16]_z
  ///   public static unsafe Vector<uint> Min(Vector<uint> left, uint right); // svmin[_n_u32]_m or svmin[_n_u32]_x or svmin[_n_u32]_z
  ///   public static unsafe Vector<ulong> Min(Vector<ulong> left, ulong right); // svmin[_n_u64]_m or svmin[_n_u64]_x or svmin[_n_u64]_z
  ///   public static unsafe Vector<float> MinNumber(Vector<float> left, float right); // svminnm[_n_f32]_m or svminnm[_n_f32]_x or svminnm[_n_f32]_z
  ///   public static unsafe Vector<double> MinNumber(Vector<double> left, double right); // svminnm[_n_f64]_m or svminnm[_n_f64]_x or svminnm[_n_f64]_z
  ///   public static unsafe Vector<float> Multiply(Vector<float> left, float right); // svmul[_n_f32]_m or svmul[_n_f32]_x or svmul[_n_f32]_z
  ///   public static unsafe Vector<double> Multiply(Vector<double> left, double right); // svmul[_n_f64]_m or svmul[_n_f64]_x or svmul[_n_f64]_z
  ///   public static unsafe Vector<sbyte> Multiply(Vector<sbyte> left, sbyte right); // svmul[_n_s8]_m or svmul[_n_s8]_x or svmul[_n_s8]_z
  ///   public static unsafe Vector<short> Multiply(Vector<short> left, short right); // svmul[_n_s16]_m or svmul[_n_s16]_x or svmul[_n_s16]_z
  ///   public static unsafe Vector<int> Multiply(Vector<int> left, int right); // svmul[_n_s32]_m or svmul[_n_s32]_x or svmul[_n_s32]_z
  ///   public static unsafe Vector<long> Multiply(Vector<long> left, long right); // svmul[_n_s64]_m or svmul[_n_s64]_x or svmul[_n_s64]_z
  ///   public static unsafe Vector<byte> Multiply(Vector<byte> left, byte right); // svmul[_n_u8]_m or svmul[_n_u8]_x or svmul[_n_u8]_z
  ///   public static unsafe Vector<ushort> Multiply(Vector<ushort> left, ushort right); // svmul[_n_u16]_m or svmul[_n_u16]_x or svmul[_n_u16]_z
  ///   public static unsafe Vector<uint> Multiply(Vector<uint> left, uint right); // svmul[_n_u32]_m or svmul[_n_u32]_x or svmul[_n_u32]_z
  ///   public static unsafe Vector<ulong> Multiply(Vector<ulong> left, ulong right); // svmul[_n_u64]_m or svmul[_n_u64]_x or svmul[_n_u64]_z
  ///   public static unsafe Vector<float> MultiplyAdd(Vector<float> addend, Vector<float> left, float right); // svmla[_n_f32]_m or svmla[_n_f32]_x or svmla[_n_f32]_z
  ///   public static unsafe Vector<double> MultiplyAdd(Vector<double> addend, Vector<double> left, double right); // svmla[_n_f64]_m or svmla[_n_f64]_x or svmla[_n_f64]_z
  ///   public static unsafe Vector<sbyte> MultiplyAdd(Vector<sbyte> addend, Vector<sbyte> left, sbyte right); // svmla[_n_s8]_m or svmla[_n_s8]_x or svmla[_n_s8]_z
  ///   public static unsafe Vector<short> MultiplyAdd(Vector<short> addend, Vector<short> left, short right); // svmla[_n_s16]_m or svmla[_n_s16]_x or svmla[_n_s16]_z
  ///   public static unsafe Vector<int> MultiplyAdd(Vector<int> addend, Vector<int> left, int right); // svmla[_n_s32]_m or svmla[_n_s32]_x or svmla[_n_s32]_z
  ///   public static unsafe Vector<long> MultiplyAdd(Vector<long> addend, Vector<long> left, long right); // svmla[_n_s64]_m or svmla[_n_s64]_x or svmla[_n_s64]_z
  ///   public static unsafe Vector<byte> MultiplyAdd(Vector<byte> addend, Vector<byte> left, byte right); // svmla[_n_u8]_m or svmla[_n_u8]_x or svmla[_n_u8]_z
  ///   public static unsafe Vector<ushort> MultiplyAdd(Vector<ushort> addend, Vector<ushort> left, ushort right); // svmla[_n_u16]_m or svmla[_n_u16]_x or svmla[_n_u16]_z
  ///   public static unsafe Vector<uint> MultiplyAdd(Vector<uint> addend, Vector<uint> left, uint right); // svmla[_n_u32]_m or svmla[_n_u32]_x or svmla[_n_u32]_z
  ///   public static unsafe Vector<ulong> MultiplyAdd(Vector<ulong> addend, Vector<ulong> left, ulong right); // svmla[_n_u64]_m or svmla[_n_u64]_x or svmla[_n_u64]_z
  ///   public static unsafe Vector<float> MultiplyAddMultiplicandFirst(Vector<float> op1, Vector<float> op2, Vector<float> op3); // svmad[_f32]_m or svmad[_f32]_x or svmad[_f32]_z
  ///   public static unsafe Vector<double> MultiplyAddMultiplicandFirst(Vector<double> op1, Vector<double> op2, Vector<double> op3); // svmad[_f64]_m or svmad[_f64]_x or svmad[_f64]_z
  ///   public static unsafe Vector<sbyte> MultiplyAddMultiplicandFirst(Vector<sbyte> op1, Vector<sbyte> op2, Vector<sbyte> op3); // svmad[_s8]_m or svmad[_s8]_x or svmad[_s8]_z
  ///   public static unsafe Vector<short> MultiplyAddMultiplicandFirst(Vector<short> op1, Vector<short> op2, Vector<short> op3); // svmad[_s16]_m or svmad[_s16]_x or svmad[_s16]_z
  ///   public static unsafe Vector<int> MultiplyAddMultiplicandFirst(Vector<int> op1, Vector<int> op2, Vector<int> op3); // svmad[_s32]_m or svmad[_s32]_x or svmad[_s32]_z
  ///   public static unsafe Vector<long> MultiplyAddMultiplicandFirst(Vector<long> op1, Vector<long> op2, Vector<long> op3); // svmad[_s64]_m or svmad[_s64]_x or svmad[_s64]_z
  ///   public static unsafe Vector<byte> MultiplyAddMultiplicandFirst(Vector<byte> op1, Vector<byte> op2, Vector<byte> op3); // svmad[_u8]_m or svmad[_u8]_x or svmad[_u8]_z
  ///   public static unsafe Vector<ushort> MultiplyAddMultiplicandFirst(Vector<ushort> op1, Vector<ushort> op2, Vector<ushort> op3); // svmad[_u16]_m or svmad[_u16]_x or svmad[_u16]_z
  ///   public static unsafe Vector<uint> MultiplyAddMultiplicandFirst(Vector<uint> op1, Vector<uint> op2, Vector<uint> op3); // svmad[_u32]_m or svmad[_u32]_x or svmad[_u32]_z
  ///   public static unsafe Vector<ulong> MultiplyAddMultiplicandFirst(Vector<ulong> op1, Vector<ulong> op2, Vector<ulong> op3); // svmad[_u64]_m or svmad[_u64]_x or svmad[_u64]_z
  ///   public static unsafe Vector<float> MultiplyAddMultiplicandFirst(Vector<float> op1, Vector<float> op2, float op3); // svmad[_n_f32]_m or svmad[_n_f32]_x or svmad[_n_f32]_z
  ///   public static unsafe Vector<double> MultiplyAddMultiplicandFirst(Vector<double> op1, Vector<double> op2, double op3); // svmad[_n_f64]_m or svmad[_n_f64]_x or svmad[_n_f64]_z
  ///   public static unsafe Vector<sbyte> MultiplyAddMultiplicandFirst(Vector<sbyte> op1, Vector<sbyte> op2, sbyte op3); // svmad[_n_s8]_m or svmad[_n_s8]_x or svmad[_n_s8]_z
  ///   public static unsafe Vector<short> MultiplyAddMultiplicandFirst(Vector<short> op1, Vector<short> op2, short op3); // svmad[_n_s16]_m or svmad[_n_s16]_x or svmad[_n_s16]_z
  ///   public static unsafe Vector<int> MultiplyAddMultiplicandFirst(Vector<int> op1, Vector<int> op2, int op3); // svmad[_n_s32]_m or svmad[_n_s32]_x or svmad[_n_s32]_z
  ///   public static unsafe Vector<long> MultiplyAddMultiplicandFirst(Vector<long> op1, Vector<long> op2, long op3); // svmad[_n_s64]_m or svmad[_n_s64]_x or svmad[_n_s64]_z
  ///   public static unsafe Vector<byte> MultiplyAddMultiplicandFirst(Vector<byte> op1, Vector<byte> op2, byte op3); // svmad[_n_u8]_m or svmad[_n_u8]_x or svmad[_n_u8]_z
  ///   public static unsafe Vector<ushort> MultiplyAddMultiplicandFirst(Vector<ushort> op1, Vector<ushort> op2, ushort op3); // svmad[_n_u16]_m or svmad[_n_u16]_x or svmad[_n_u16]_z
  ///   public static unsafe Vector<uint> MultiplyAddMultiplicandFirst(Vector<uint> op1, Vector<uint> op2, uint op3); // svmad[_n_u32]_m or svmad[_n_u32]_x or svmad[_n_u32]_z
  ///   public static unsafe Vector<ulong> MultiplyAddMultiplicandFirst(Vector<ulong> op1, Vector<ulong> op2, ulong op3); // svmad[_n_u64]_m or svmad[_n_u64]_x or svmad[_n_u64]_z
  ///   public static unsafe Vector<float> MultiplyAddNegated(Vector<float> addend, Vector<float> left, float right); // svnmla[_n_f32]_m or svnmla[_n_f32]_x or svnmla[_n_f32]_z
  ///   public static unsafe Vector<double> MultiplyAddNegated(Vector<double> addend, Vector<double> left, double right); // svnmla[_n_f64]_m or svnmla[_n_f64]_x or svnmla[_n_f64]_z
  ///   public static unsafe Vector<float> MultiplyExtended(Vector<float> left, float right); // svmulx[_n_f32]_m or svmulx[_n_f32]_x or svmulx[_n_f32]_z
  ///   public static unsafe Vector<double> MultiplyExtended(Vector<double> left, double right); // svmulx[_n_f64]_m or svmulx[_n_f64]_x or svmulx[_n_f64]_z
  ///   public static unsafe Vector<float> MultiplySubtract(Vector<float> minuend, Vector<float> left, float right); // svmls[_n_f32]_m or svmls[_n_f32]_x or svmls[_n_f32]_z
  ///   public static unsafe Vector<double> MultiplySubtract(Vector<double> minuend, Vector<double> left, double right); // svmls[_n_f64]_m or svmls[_n_f64]_x or svmls[_n_f64]_z
  ///   public static unsafe Vector<sbyte> MultiplySubtract(Vector<sbyte> minuend, Vector<sbyte> left, sbyte right); // svmls[_n_s8]_m or svmls[_n_s8]_x or svmls[_n_s8]_z
  ///   public static unsafe Vector<short> MultiplySubtract(Vector<short> minuend, Vector<short> left, short right); // svmls[_n_s16]_m or svmls[_n_s16]_x or svmls[_n_s16]_z
  ///   public static unsafe Vector<int> MultiplySubtract(Vector<int> minuend, Vector<int> left, int right); // svmls[_n_s32]_m or svmls[_n_s32]_x or svmls[_n_s32]_z
  ///   public static unsafe Vector<long> MultiplySubtract(Vector<long> minuend, Vector<long> left, long right); // svmls[_n_s64]_m or svmls[_n_s64]_x or svmls[_n_s64]_z
  ///   public static unsafe Vector<byte> MultiplySubtract(Vector<byte> minuend, Vector<byte> left, byte right); // svmls[_n_u8]_m or svmls[_n_u8]_x or svmls[_n_u8]_z
  ///   public static unsafe Vector<ushort> MultiplySubtract(Vector<ushort> minuend, Vector<ushort> left, ushort right); // svmls[_n_u16]_m or svmls[_n_u16]_x or svmls[_n_u16]_z
  ///   public static unsafe Vector<uint> MultiplySubtract(Vector<uint> minuend, Vector<uint> left, uint right); // svmls[_n_u32]_m or svmls[_n_u32]_x or svmls[_n_u32]_z
  ///   public static unsafe Vector<ulong> MultiplySubtract(Vector<ulong> minuend, Vector<ulong> left, ulong right); // svmls[_n_u64]_m or svmls[_n_u64]_x or svmls[_n_u64]_z
  ///   public static unsafe Vector<float> MultiplySubtractMultiplicandFirst(Vector<float> op1, Vector<float> op2, Vector<float> op3); // svmsb[_f32]_m or svmsb[_f32]_x or svmsb[_f32]_z
  ///   public static unsafe Vector<double> MultiplySubtractMultiplicandFirst(Vector<double> op1, Vector<double> op2, Vector<double> op3); // svmsb[_f64]_m or svmsb[_f64]_x or svmsb[_f64]_z
  ///   public static unsafe Vector<sbyte> MultiplySubtractMultiplicandFirst(Vector<sbyte> op1, Vector<sbyte> op2, Vector<sbyte> op3); // svmsb[_s8]_m or svmsb[_s8]_x or svmsb[_s8]_z
  ///   public static unsafe Vector<short> MultiplySubtractMultiplicandFirst(Vector<short> op1, Vector<short> op2, Vector<short> op3); // svmsb[_s16]_m or svmsb[_s16]_x or svmsb[_s16]_z
  ///   public static unsafe Vector<int> MultiplySubtractMultiplicandFirst(Vector<int> op1, Vector<int> op2, Vector<int> op3); // svmsb[_s32]_m or svmsb[_s32]_x or svmsb[_s32]_z
  ///   public static unsafe Vector<long> MultiplySubtractMultiplicandFirst(Vector<long> op1, Vector<long> op2, Vector<long> op3); // svmsb[_s64]_m or svmsb[_s64]_x or svmsb[_s64]_z
  ///   public static unsafe Vector<byte> MultiplySubtractMultiplicandFirst(Vector<byte> op1, Vector<byte> op2, Vector<byte> op3); // svmsb[_u8]_m or svmsb[_u8]_x or svmsb[_u8]_z
  ///   public static unsafe Vector<ushort> MultiplySubtractMultiplicandFirst(Vector<ushort> op1, Vector<ushort> op2, Vector<ushort> op3); // svmsb[_u16]_m or svmsb[_u16]_x or svmsb[_u16]_z
  ///   public static unsafe Vector<uint> MultiplySubtractMultiplicandFirst(Vector<uint> op1, Vector<uint> op2, Vector<uint> op3); // svmsb[_u32]_m or svmsb[_u32]_x or svmsb[_u32]_z
  ///   public static unsafe Vector<ulong> MultiplySubtractMultiplicandFirst(Vector<ulong> op1, Vector<ulong> op2, Vector<ulong> op3); // svmsb[_u64]_m or svmsb[_u64]_x or svmsb[_u64]_z
  ///   public static unsafe Vector<float> MultiplySubtractMultiplicandFirst(Vector<float> op1, Vector<float> op2, float op3); // svmsb[_n_f32]_m or svmsb[_n_f32]_x or svmsb[_n_f32]_z
  ///   public static unsafe Vector<double> MultiplySubtractMultiplicandFirst(Vector<double> op1, Vector<double> op2, double op3); // svmsb[_n_f64]_m or svmsb[_n_f64]_x or svmsb[_n_f64]_z
  ///   public static unsafe Vector<sbyte> MultiplySubtractMultiplicandFirst(Vector<sbyte> op1, Vector<sbyte> op2, sbyte op3); // svmsb[_n_s8]_m or svmsb[_n_s8]_x or svmsb[_n_s8]_z
  ///   public static unsafe Vector<short> MultiplySubtractMultiplicandFirst(Vector<short> op1, Vector<short> op2, short op3); // svmsb[_n_s16]_m or svmsb[_n_s16]_x or svmsb[_n_s16]_z
  ///   public static unsafe Vector<int> MultiplySubtractMultiplicandFirst(Vector<int> op1, Vector<int> op2, int op3); // svmsb[_n_s32]_m or svmsb[_n_s32]_x or svmsb[_n_s32]_z
  ///   public static unsafe Vector<long> MultiplySubtractMultiplicandFirst(Vector<long> op1, Vector<long> op2, long op3); // svmsb[_n_s64]_m or svmsb[_n_s64]_x or svmsb[_n_s64]_z
  ///   public static unsafe Vector<byte> MultiplySubtractMultiplicandFirst(Vector<byte> op1, Vector<byte> op2, byte op3); // svmsb[_n_u8]_m or svmsb[_n_u8]_x or svmsb[_n_u8]_z
  ///   public static unsafe Vector<ushort> MultiplySubtractMultiplicandFirst(Vector<ushort> op1, Vector<ushort> op2, ushort op3); // svmsb[_n_u16]_m or svmsb[_n_u16]_x or svmsb[_n_u16]_z
  ///   public static unsafe Vector<uint> MultiplySubtractMultiplicandFirst(Vector<uint> op1, Vector<uint> op2, uint op3); // svmsb[_n_u32]_m or svmsb[_n_u32]_x or svmsb[_n_u32]_z
  ///   public static unsafe Vector<ulong> MultiplySubtractMultiplicandFirst(Vector<ulong> op1, Vector<ulong> op2, ulong op3); // svmsb[_n_u64]_m or svmsb[_n_u64]_x or svmsb[_n_u64]_z
  ///   public static unsafe Vector<float> MultiplySubtractNegated(Vector<float> minuend, Vector<float> left, float right); // svnmls[_n_f32]_m or svnmls[_n_f32]_x or svnmls[_n_f32]_z
  ///   public static unsafe Vector<double> MultiplySubtractNegated(Vector<double> minuend, Vector<double> left, double right); // svnmls[_n_f64]_m or svnmls[_n_f64]_x or svnmls[_n_f64]_z
  ///   public static unsafe Vector<float> NegateMultiplyAddMultiplicandFirst(Vector<float> op1, Vector<float> op2, Vector<float> op3); // svnmad[_f32]_m or svnmad[_f32]_x or svnmad[_f32]_z
  ///   public static unsafe Vector<double> NegateMultiplyAddMultiplicandFirst(Vector<double> op1, Vector<double> op2, Vector<double> op3); // svnmad[_f64]_m or svnmad[_f64]_x or svnmad[_f64]_z
  ///   public static unsafe Vector<float> NegateMultiplyAddMultiplicandFirst(Vector<float> op1, Vector<float> op2, float op3); // svnmad[_n_f32]_m or svnmad[_n_f32]_x or svnmad[_n_f32]_z
  ///   public static unsafe Vector<double> NegateMultiplyAddMultiplicandFirst(Vector<double> op1, Vector<double> op2, double op3); // svnmad[_n_f64]_m or svnmad[_n_f64]_x or svnmad[_n_f64]_z
  ///   public static unsafe Vector<float> NegateMultiplySubtractMultiplicandFirst(Vector<float> op1, Vector<float> op2, Vector<float> op3); // svnmsb[_f32]_m or svnmsb[_f32]_x or svnmsb[_f32]_z
  ///   public static unsafe Vector<double> NegateMultiplySubtractMultiplicandFirst(Vector<double> op1, Vector<double> op2, Vector<double> op3); // svnmsb[_f64]_m or svnmsb[_f64]_x or svnmsb[_f64]_z
  ///   public static unsafe Vector<float> NegateMultiplySubtractMultiplicandFirst(Vector<float> op1, Vector<float> op2, float op3); // svnmsb[_n_f32]_m or svnmsb[_n_f32]_x or svnmsb[_n_f32]_z
  ///   public static unsafe Vector<double> NegateMultiplySubtractMultiplicandFirst(Vector<double> op1, Vector<double> op2, double op3); // svnmsb[_n_f64]_m or svnmsb[_n_f64]_x or svnmsb[_n_f64]_z
  ///   public static unsafe Vector<float> Subtract(Vector<float> left, float right); // svsub[_n_f32]_m or svsub[_n_f32]_x or svsub[_n_f32]_z
  ///   public static unsafe Vector<double> Subtract(Vector<double> left, double right); // svsub[_n_f64]_m or svsub[_n_f64]_x or svsub[_n_f64]_z
  ///   public static unsafe Vector<sbyte> Subtract(Vector<sbyte> left, sbyte right); // svsub[_n_s8]_m or svsub[_n_s8]_x or svsub[_n_s8]_z
  ///   public static unsafe Vector<short> Subtract(Vector<short> left, short right); // svsub[_n_s16]_m or svsub[_n_s16]_x or svsub[_n_s16]_z
  ///   public static unsafe Vector<int> Subtract(Vector<int> left, int right); // svsub[_n_s32]_m or svsub[_n_s32]_x or svsub[_n_s32]_z
  ///   public static unsafe Vector<long> Subtract(Vector<long> left, long right); // svsub[_n_s64]_m or svsub[_n_s64]_x or svsub[_n_s64]_z
  ///   public static unsafe Vector<byte> Subtract(Vector<byte> left, byte right); // svsub[_n_u8]_m or svsub[_n_u8]_x or svsub[_n_u8]_z
  ///   public static unsafe Vector<ushort> Subtract(Vector<ushort> left, ushort right); // svsub[_n_u16]_m or svsub[_n_u16]_x or svsub[_n_u16]_z
  ///   public static unsafe Vector<uint> Subtract(Vector<uint> left, uint right); // svsub[_n_u32]_m or svsub[_n_u32]_x or svsub[_n_u32]_z
  ///   public static unsafe Vector<ulong> Subtract(Vector<ulong> left, ulong right); // svsub[_n_u64]_m or svsub[_n_u64]_x or svsub[_n_u64]_z
  ///   public static unsafe Vector<float> SubtractReversed(Vector<float> left, Vector<float> right); // svsubr[_f32]_m or svsubr[_f32]_x or svsubr[_f32]_z
  ///   public static unsafe Vector<double> SubtractReversed(Vector<double> left, Vector<double> right); // svsubr[_f64]_m or svsubr[_f64]_x or svsubr[_f64]_z
  ///   public static unsafe Vector<sbyte> SubtractReversed(Vector<sbyte> left, Vector<sbyte> right); // svsubr[_s8]_m or svsubr[_s8]_x or svsubr[_s8]_z
  ///   public static unsafe Vector<short> SubtractReversed(Vector<short> left, Vector<short> right); // svsubr[_s16]_m or svsubr[_s16]_x or svsubr[_s16]_z
  ///   public static unsafe Vector<int> SubtractReversed(Vector<int> left, Vector<int> right); // svsubr[_s32]_m or svsubr[_s32]_x or svsubr[_s32]_z
  ///   public static unsafe Vector<long> SubtractReversed(Vector<long> left, Vector<long> right); // svsubr[_s64]_m or svsubr[_s64]_x or svsubr[_s64]_z
  ///   public static unsafe Vector<byte> SubtractReversed(Vector<byte> left, Vector<byte> right); // svsubr[_u8]_m or svsubr[_u8]_x or svsubr[_u8]_z
  ///   public static unsafe Vector<ushort> SubtractReversed(Vector<ushort> left, Vector<ushort> right); // svsubr[_u16]_m or svsubr[_u16]_x or svsubr[_u16]_z
  ///   public static unsafe Vector<uint> SubtractReversed(Vector<uint> left, Vector<uint> right); // svsubr[_u32]_m or svsubr[_u32]_x or svsubr[_u32]_z
  ///   public static unsafe Vector<ulong> SubtractReversed(Vector<ulong> left, Vector<ulong> right); // svsubr[_u64]_m or svsubr[_u64]_x or svsubr[_u64]_z
  ///   public static unsafe Vector<float> SubtractReversed(Vector<float> left, float right); // svsubr[_n_f32]_m or svsubr[_n_f32]_x or svsubr[_n_f32]_z
  ///   public static unsafe Vector<double> SubtractReversed(Vector<double> left, double right); // svsubr[_n_f64]_m or svsubr[_n_f64]_x or svsubr[_n_f64]_z
  ///   public static unsafe Vector<sbyte> SubtractReversed(Vector<sbyte> left, sbyte right); // svsubr[_n_s8]_m or svsubr[_n_s8]_x or svsubr[_n_s8]_z
  ///   public static unsafe Vector<short> SubtractReversed(Vector<short> left, short right); // svsubr[_n_s16]_m or svsubr[_n_s16]_x or svsubr[_n_s16]_z
  ///   public static unsafe Vector<int> SubtractReversed(Vector<int> left, int right); // svsubr[_n_s32]_m or svsubr[_n_s32]_x or svsubr[_n_s32]_z
  ///   public static unsafe Vector<long> SubtractReversed(Vector<long> left, long right); // svsubr[_n_s64]_m or svsubr[_n_s64]_x or svsubr[_n_s64]_z
  ///   public static unsafe Vector<byte> SubtractReversed(Vector<byte> left, byte right); // svsubr[_n_u8]_m or svsubr[_n_u8]_x or svsubr[_n_u8]_z
  ///   public static unsafe Vector<ushort> SubtractReversed(Vector<ushort> left, ushort right); // svsubr[_n_u16]_m or svsubr[_n_u16]_x or svsubr[_n_u16]_z
  ///   public static unsafe Vector<uint> SubtractReversed(Vector<uint> left, uint right); // svsubr[_n_u32]_m or svsubr[_n_u32]_x or svsubr[_n_u32]_z
  ///   public static unsafe Vector<ulong> SubtractReversed(Vector<ulong> left, ulong right); // svsubr[_n_u64]_m or svsubr[_n_u64]_x or svsubr[_n_u64]_z
  ///   public static unsafe Vector<sbyte> SubtractSaturate(Vector<sbyte> left, sbyte right); // svqsub[_n_s8]
  ///   public static unsafe Vector<short> SubtractSaturate(Vector<short> left, short right); // svqsub[_n_s16]
  ///   public static unsafe Vector<int> SubtractSaturate(Vector<int> left, int right); // svqsub[_n_s32]
  ///   public static unsafe Vector<long> SubtractSaturate(Vector<long> left, long right); // svqsub[_n_s64]
  ///   public static unsafe Vector<byte> SubtractSaturate(Vector<byte> left, byte right); // svqsub[_n_u8]
  ///   public static unsafe Vector<ushort> SubtractSaturate(Vector<ushort> left, ushort right); // svqsub[_n_u16]
  ///   public static unsafe Vector<uint> SubtractSaturate(Vector<uint> left, uint right); // svqsub[_n_u32]
  ///   public static unsafe Vector<ulong> SubtractSaturate(Vector<ulong> left, ulong right); // svqsub[_n_u64]
  ///   Total Rejected: 196

  /// Total ACLE covered across API:      1038

