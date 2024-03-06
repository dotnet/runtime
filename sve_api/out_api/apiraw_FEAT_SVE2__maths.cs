namespace System.Runtime.Intrinsics.Arm;

/// VectorT Summary
public abstract partial class Sve2 : AdvSimd /// Feature: FEAT_SVE2  Category: maths
{

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> AbsoluteDifferenceAdd(Vector<T> addend, Vector<T> left, Vector<T> right); // SABA or UABA // MOVPRFX

  /// T: [short, sbyte], [int, short], [long, int], [ushort, byte], [uint, ushort], [ulong, uint]
  public static unsafe Vector<T> AbsoluteDifferenceAddWideningLower(Vector<T> addend, Vector<T2> left, Vector<T2> right); // SABALB or UABALB // MOVPRFX

  /// T: [short, sbyte], [int, short], [long, int], [ushort, byte], [uint, ushort], [ulong, uint]
  public static unsafe Vector<T> AbsoluteDifferenceAddWideningUpper(Vector<T> addend, Vector<T2> left, Vector<T2> right); // SABALT or UABALT // MOVPRFX

  /// T: [short, sbyte], [int, short], [long, int], [ushort, byte], [uint, ushort], [ulong, uint]
  public static unsafe Vector<T> AbsoluteDifferenceWideningLower(Vector<T2> left, Vector<T2> right); // SABDLB or UABDLB

  /// T: [short, sbyte], [int, short], [long, int], [ushort, byte], [uint, ushort], [ulong, uint]
  public static unsafe Vector<T> AbsoluteDifferenceWideningUpper(Vector<T2> left, Vector<T2> right); // SABDLT or UABDLT

  /// T: uint, ulong
  public static unsafe Vector<T> AddCarryWideningLower(Vector<T> op1, Vector<T> op2, Vector<T> op3); // ADCLB // MOVPRFX

  /// T: uint, ulong
  public static unsafe Vector<T> AddCarryWideningUpper(Vector<T> op1, Vector<T> op2, Vector<T> op3); // ADCLT // MOVPRFX

  /// T: [sbyte, short], [short, int], [int, long], [byte, ushort], [ushort, uint], [uint, ulong]
  public static unsafe Vector<T> AddHighNarowingLower(Vector<T2> left, Vector<T2> right); // ADDHNB

  /// T: [sbyte, short], [short, int], [int, long], [byte, ushort], [ushort, uint], [uint, ulong]
  public static unsafe Vector<T> AddHighNarowingUpper(Vector<T> even, Vector<T2> left, Vector<T2> right); // ADDHNT

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> AddPairwise(Vector<T> left, Vector<T> right); // FADDP or ADDP // predicated, MOVPRFX

  /// T: [short, sbyte], [int, short], [long, int], [ushort, byte], [uint, ushort], [ulong, uint]
  public static unsafe Vector<T> AddPairwiseWidening(Vector<T> left, Vector<T2> right); // SADALP or UADALP // predicated, MOVPRFX

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> AddSaturate(Vector<T> left, Vector<T> right); // SQADD or UQADD // predicated, MOVPRFX

  /// T: [byte, sbyte], [ushort, short], [uint, int], [ulong, long]
  public static unsafe Vector<T> AddSaturateWithSignedAddend(Vector<T> left, Vector<T2> right); // USQADD // predicated, MOVPRFX

  /// T: [sbyte, byte], [short, ushort], [int, uint], [long, ulong]
  public static unsafe Vector<T> AddSaturateWithUnsignedAddend(Vector<T> left, Vector<T2> right); // SUQADD // predicated, MOVPRFX

  /// T: [short, sbyte], [int, short], [long, int], [ushort, byte], [uint, ushort], [ulong, uint]
  public static unsafe Vector<T> AddWideLower(Vector<T> left, Vector<T2> right); // SADDWB or UADDWB

  /// T: [short, sbyte], [int, short], [long, int], [ushort, byte], [uint, ushort], [ulong, uint]
  public static unsafe Vector<T> AddWideUpper(Vector<T> left, Vector<T2> right); // SADDWT or UADDWT

  /// T: [short, sbyte], [int, short], [long, int], [ushort, byte], [uint, ushort], [ulong, uint]
  public static unsafe Vector<T> AddWideningLower(Vector<T2> left, Vector<T2> right); // SADDLB or UADDLB

  /// T: [short, sbyte], [int, short], [long, int]
  public static unsafe Vector<T> AddWideningLowerUpper(Vector<T2> left, Vector<T2> right); // SADDLBT

  /// T: [short, sbyte], [int, short], [long, int], [ushort, byte], [uint, ushort], [ulong, uint]
  public static unsafe Vector<T> AddWideningUpper(Vector<T2> left, Vector<T2> right); // SADDLT or UADDLT

  /// T: [int, sbyte], [long, short]
  public static unsafe Vector<T> DotProductComplex(Vector<T> op1, Vector<T2> op2, Vector<T2> op3, [ConstantExpected] byte rotation); // CDOT // MOVPRFX

  /// T: [int, sbyte], [long, short]
  public static unsafe Vector<T> DotProductComplex(Vector<T> op1, Vector<T2> op2, Vector<T2> op3, ulong imm_index, [ConstantExpected] byte rotation); // CDOT // MOVPRFX

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> HalvingAdd(Vector<T> left, Vector<T> right); // SHADD or UHADD // predicated, MOVPRFX

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> HalvingSubtract(Vector<T> left, Vector<T> right); // SHSUB or UHSUB or SHSUBR or UHSUBR // predicated, MOVPRFX

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> HalvingSubtractReversed(Vector<T> left, Vector<T> right); // SHSUBR or UHSUBR or SHSUB or UHSUB // predicated, MOVPRFX

  /// T: float, double
  public static unsafe Vector<T> MaxNumberPairwise(Vector<T> left, Vector<T> right); // FMAXNMP // predicated, MOVPRFX

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> MaxPairwise(Vector<T> left, Vector<T> right); // FMAXP or SMAXP or UMAXP // predicated, MOVPRFX

  /// T: float, double
  public static unsafe Vector<T> MinNumberPairwise(Vector<T> left, Vector<T> right); // FMINNMP // predicated, MOVPRFX

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> MinPairwise(Vector<T> left, Vector<T> right); // FMINP or SMINP or UMINP // predicated, MOVPRFX

  /// T: short, int, long, ushort, uint, ulong
  public static unsafe Vector<T> MultiplyAddBySelectedScalar(Vector<T> addend, Vector<T> left, Vector<T> right, [ConstantExpected] byte rightIndex); // MLA // MOVPRFX

  /// T: [short, sbyte], [int, short], [long, int], [ushort, byte], [uint, ushort], [ulong, uint]
  public static unsafe Vector<T> MultiplyAddWideningLower(Vector<T> op1, Vector<T2> op2, Vector<T2> op3); // SMLALB or UMLALB // MOVPRFX

  /// T: [int, short], [long, int], [uint, ushort], [ulong, uint]
  public static unsafe Vector<T> MultiplyAddWideningLower(Vector<T> op1, Vector<T2> op2, Vector<T2> op3, ulong imm_index); // SMLALB or UMLALB // MOVPRFX

  /// T: [short, sbyte], [int, short], [long, int], [ushort, byte], [uint, ushort], [ulong, uint]
  public static unsafe Vector<T> MultiplyAddWideningUpper(Vector<T> op1, Vector<T2> op2, Vector<T2> op3); // SMLALT or UMLALT // MOVPRFX

  /// T: [int, short], [long, int], [uint, ushort], [ulong, uint]
  public static unsafe Vector<T> MultiplyAddWideningUpper(Vector<T> op1, Vector<T2> op2, Vector<T2> op3, ulong imm_index); // SMLALT or UMLALT // MOVPRFX

  /// T: short, int, long, ushort, uint, ulong
  public static unsafe Vector<T> MultiplyBySelectedScalar(Vector<T> left, Vector<T> right, [ConstantExpected] byte rightIndex); // MUL

  /// T: short, int, long, ushort, uint, ulong
  public static unsafe Vector<T> MultiplySubtractBySelectedScalar(Vector<T> minuend, Vector<T> left, Vector<T> right, [ConstantExpected] byte rightIndex); // MLS // MOVPRFX

  /// T: [short, sbyte], [int, short], [long, int], [ushort, byte], [uint, ushort], [ulong, uint]
  public static unsafe Vector<T> MultiplySubtractWideningLower(Vector<T> op1, Vector<T2> op2, Vector<T2> op3); // SMLSLB or UMLSLB // MOVPRFX

  /// T: [int, short], [long, int], [uint, ushort], [ulong, uint]
  public static unsafe Vector<T> MultiplySubtractWideningLower(Vector<T> op1, Vector<T2> op2, Vector<T2> op3, ulong imm_index); // SMLSLB or UMLSLB // MOVPRFX

  /// T: [short, sbyte], [int, short], [long, int], [ushort, byte], [uint, ushort], [ulong, uint]
  public static unsafe Vector<T> MultiplySubtractWideningUpper(Vector<T> op1, Vector<T2> op2, Vector<T2> op3); // SMLSLT or UMLSLT // MOVPRFX

  /// T: [int, short], [long, int], [uint, ushort], [ulong, uint]
  public static unsafe Vector<T> MultiplySubtractWideningUpper(Vector<T> op1, Vector<T2> op2, Vector<T2> op3, ulong imm_index); // SMLSLT or UMLSLT // MOVPRFX

  /// T: [short, sbyte], [int, short], [long, int], [ushort, byte], [uint, ushort], [ulong, uint]
  public static unsafe Vector<T> MultiplyWideningLower(Vector<T2> left, Vector<T2> right); // SMULLB or UMULLB

  /// T: [int, short], [long, int], [uint, ushort], [ulong, uint]
  public static unsafe Vector<T> MultiplyWideningLower(Vector<T2> op1, Vector<T2> op2, ulong imm_index); // SMULLB or UMULLB

  /// T: [short, sbyte], [int, short], [long, int], [ushort, byte], [uint, ushort], [ulong, uint]
  public static unsafe Vector<T> MultiplyWideningUpper(Vector<T2> left, Vector<T2> right); // SMULLT or UMULLT

  /// T: [int, short], [long, int], [uint, ushort], [ulong, uint]
  public static unsafe Vector<T> MultiplyWideningUpper(Vector<T2> op1, Vector<T2> op2, ulong imm_index); // SMULLT or UMULLT

  public static unsafe Vector<byte> PolynomialMultiply(Vector<byte> left, Vector<byte> right); // PMUL

  /// T: [ushort, byte], [ulong, uint]
  public static unsafe Vector<T> PolynomialMultiplyWideningLower(Vector<T2> left, Vector<T2> right); // PMULLB

  /// T: byte, uint
  public static unsafe Vector<T> PolynomialMultiplyWideningLower(Vector<T> left, Vector<T> right); // PMULLB

  /// T: [ushort, byte], [ulong, uint]
  public static unsafe Vector<T> PolynomialMultiplyWideningUpper(Vector<T2> left, Vector<T2> right); // PMULLT

  /// T: byte, uint
  public static unsafe Vector<T> PolynomialMultiplyWideningUpper(Vector<T> left, Vector<T> right); // PMULLT

  /// T: [sbyte, short], [short, int], [int, long], [byte, ushort], [ushort, uint], [uint, ulong]
  public static unsafe Vector<T> RoundingAddHighNarowingLower(Vector<T2> left, Vector<T2> right); // RADDHNB

  /// T: [sbyte, short], [short, int], [int, long], [byte, ushort], [ushort, uint], [uint, ulong]
  public static unsafe Vector<T> RoundingAddHighNarowingUpper(Vector<T> even, Vector<T2> left, Vector<T2> right); // RADDHNT

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> RoundingHalvingAdd(Vector<T> left, Vector<T> right); // SRHADD or URHADD // predicated, MOVPRFX

  /// T: [sbyte, short], [short, int], [int, long], [byte, ushort], [ushort, uint], [uint, ulong]
  public static unsafe Vector<T> RoundingSubtractHighNarowingLower(Vector<T2> left, Vector<T2> right); // RSUBHNB

  /// T: [sbyte, short], [short, int], [int, long], [byte, ushort], [ushort, uint], [uint, ulong]
  public static unsafe Vector<T> RoundingSubtractHighNarowingUpper(Vector<T> even, Vector<T2> left, Vector<T2> right); // RSUBHNT

  /// T: sbyte, short, int, long
  public static unsafe Vector<T> SaturatingAbs(Vector<T> value); // SQABS // predicated, MOVPRFX

  /// T: [short, sbyte], [int, short], [long, int]
  public static unsafe Vector<T> SaturatingDoublingMultiplyAddWideningLower(Vector<T> op1, Vector<T2> op2, Vector<T2> op3); // SQDMLALB // MOVPRFX

  /// T: [int, short], [long, int]
  public static unsafe Vector<T> SaturatingDoublingMultiplyAddWideningLower(Vector<T> op1, Vector<T2> op2, Vector<T2> op3, ulong imm_index); // SQDMLALB // MOVPRFX

  /// T: [short, sbyte], [int, short], [long, int]
  public static unsafe Vector<T> SaturatingDoublingMultiplyAddWideningLowerUpper(Vector<T> op1, Vector<T2> op2, Vector<T2> op3); // SQDMLALBT // MOVPRFX

  /// T: [short, sbyte], [int, short], [long, int]
  public static unsafe Vector<T> SaturatingDoublingMultiplyAddWideningUpper(Vector<T> op1, Vector<T2> op2, Vector<T2> op3); // SQDMLALT // MOVPRFX

  /// T: [int, short], [long, int]
  public static unsafe Vector<T> SaturatingDoublingMultiplyAddWideningUpper(Vector<T> op1, Vector<T2> op2, Vector<T2> op3, ulong imm_index); // SQDMLALT // MOVPRFX

  /// T: sbyte, short, int, long
  public static unsafe Vector<T> SaturatingDoublingMultiplyHigh(Vector<T> left, Vector<T> right); // SQDMULH

  /// T: short, int, long
  public static unsafe Vector<T> SaturatingDoublingMultiplyHigh(Vector<T> op1, Vector<T> op2, ulong imm_index); // SQDMULH

  /// T: [short, sbyte], [int, short], [long, int]
  public static unsafe Vector<T> SaturatingDoublingMultiplySubtractWideningLower(Vector<T> op1, Vector<T2> op2, Vector<T2> op3); // SQDMLSLB // MOVPRFX

  /// T: [int, short], [long, int]
  public static unsafe Vector<T> SaturatingDoublingMultiplySubtractWideningLower(Vector<T> op1, Vector<T2> op2, Vector<T2> op3, ulong imm_index); // SQDMLSLB // MOVPRFX

  /// T: [short, sbyte], [int, short], [long, int]
  public static unsafe Vector<T> SaturatingDoublingMultiplySubtractWideningLowerUpper(Vector<T> op1, Vector<T2> op2, Vector<T2> op3); // SQDMLSLBT // MOVPRFX

  /// T: [short, sbyte], [int, short], [long, int]
  public static unsafe Vector<T> SaturatingDoublingMultiplySubtractWideningUpper(Vector<T> op1, Vector<T2> op2, Vector<T2> op3); // SQDMLSLT // MOVPRFX

  /// T: [int, short], [long, int]
  public static unsafe Vector<T> SaturatingDoublingMultiplySubtractWideningUpper(Vector<T> op1, Vector<T2> op2, Vector<T2> op3, ulong imm_index); // SQDMLSLT // MOVPRFX

  /// T: [short, sbyte], [int, short], [long, int]
  public static unsafe Vector<T> SaturatingDoublingMultiplyWideningLower(Vector<T2> left, Vector<T2> right); // SQDMULLB

  /// T: [int, short], [long, int]
  public static unsafe Vector<T> SaturatingDoublingMultiplyWideningLower(Vector<T2> op1, Vector<T2> op2, ulong imm_index); // SQDMULLB

  /// T: [short, sbyte], [int, short], [long, int]
  public static unsafe Vector<T> SaturatingDoublingMultiplyWideningUpper(Vector<T2> left, Vector<T2> right); // SQDMULLT

  /// T: [int, short], [long, int]
  public static unsafe Vector<T> SaturatingDoublingMultiplyWideningUpper(Vector<T2> op1, Vector<T2> op2, ulong imm_index); // SQDMULLT

  /// T: sbyte, short, int, long
  public static unsafe Vector<T> SaturatingNegate(Vector<T> value); // SQNEG // predicated, MOVPRFX

  /// T: sbyte, short, int, long
  public static unsafe Vector<T> SaturatingRoundingDoublingMultiplyAddHigh(Vector<T> op1, Vector<T> op2, Vector<T> op3); // SQRDMLAH // MOVPRFX

  /// T: short, int, long
  public static unsafe Vector<T> SaturatingRoundingDoublingMultiplyAddHigh(Vector<T> op1, Vector<T> op2, Vector<T> op3, ulong imm_index); // SQRDMLAH // MOVPRFX

  /// T: sbyte, short, int, long
  public static unsafe Vector<T> SaturatingRoundingDoublingMultiplyHigh(Vector<T> left, Vector<T> right); // SQRDMULH

  /// T: short, int, long
  public static unsafe Vector<T> SaturatingRoundingDoublingMultiplyHigh(Vector<T> op1, Vector<T> op2, ulong imm_index); // SQRDMULH

  /// T: sbyte, short, int, long
  public static unsafe Vector<T> SaturatingRoundingDoublingMultiplySubtractHigh(Vector<T> op1, Vector<T> op2, Vector<T> op3); // SQRDMLSH // MOVPRFX

  /// T: short, int, long
  public static unsafe Vector<T> SaturatingRoundingDoublingMultiplySubtractHigh(Vector<T> op1, Vector<T> op2, Vector<T> op3, ulong imm_index); // SQRDMLSH // MOVPRFX

  /// T: [sbyte, short], [short, int], [int, long], [byte, ushort], [ushort, uint], [uint, ulong]
  public static unsafe Vector<T> SubtractHighNarowingLower(Vector<T2> left, Vector<T2> right); // SUBHNB

  /// T: [sbyte, short], [short, int], [int, long], [byte, ushort], [ushort, uint], [uint, ulong]
  public static unsafe Vector<T> SubtractHighNarowingUpper(Vector<T> even, Vector<T2> left, Vector<T2> right); // SUBHNT

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> SubtractSaturate(Vector<T> left, Vector<T> right); // SQSUB or UQSUB or SQSUBR or UQSUBR // predicated, MOVPRFX

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> SubtractSaturateReversed(Vector<T> left, Vector<T> right); // SQSUBR or UQSUBR or SQSUB or UQSUB // predicated, MOVPRFX

  /// T: [short, sbyte], [int, short], [long, int], [ushort, byte], [uint, ushort], [ulong, uint]
  public static unsafe Vector<T> SubtractWideLower(Vector<T> left, Vector<T2> right); // SSUBWB or USUBWB

  /// T: [short, sbyte], [int, short], [long, int], [ushort, byte], [uint, ushort], [ulong, uint]
  public static unsafe Vector<T> SubtractWideUpper(Vector<T> left, Vector<T2> right); // SSUBWT or USUBWT

  /// T: [short, sbyte], [int, short], [long, int], [ushort, byte], [uint, ushort], [ulong, uint]
  public static unsafe Vector<T> SubtractWideningLower(Vector<T2> left, Vector<T2> right); // SSUBLB or USUBLB

  /// T: [short, sbyte], [int, short], [long, int]
  public static unsafe Vector<T> SubtractWideningLowerUpper(Vector<T2> left, Vector<T2> right); // SSUBLBT

  /// T: [short, sbyte], [int, short], [long, int], [ushort, byte], [uint, ushort], [ulong, uint]
  public static unsafe Vector<T> SubtractWideningUpper(Vector<T2> left, Vector<T2> right); // SSUBLT or USUBLT

  /// T: [short, sbyte], [int, short], [long, int]
  public static unsafe Vector<T> SubtractWideningUpperLower(Vector<T2> left, Vector<T2> right); // SSUBLTB

  /// T: uint, ulong
  public static unsafe Vector<T> SubtractWithBorrowWideningLower(Vector<T> op1, Vector<T> op2, Vector<T> op3); // SBCLB // MOVPRFX

  /// T: uint, ulong
  public static unsafe Vector<T> SubtractWithBorrowWideningUpper(Vector<T> op1, Vector<T> op2, Vector<T> op3); // SBCLT // MOVPRFX

  /// total method signatures: 89

}


/// Full API
public abstract partial class Sve2 : AdvSimd /// Feature: FEAT_SVE2  Category: maths
{
    /// AbsoluteDifferenceAdd : Absolute difference and accumulate

    /// svint8_t svaba[_s8](svint8_t op1, svint8_t op2, svint8_t op3) : "SABA Ztied1.B, Zop2.B, Zop3.B" or "MOVPRFX Zresult, Zop1; SABA Zresult.B, Zop2.B, Zop3.B"
  public static unsafe Vector<sbyte> AbsoluteDifferenceAdd(Vector<sbyte> addend, Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t svaba[_s16](svint16_t op1, svint16_t op2, svint16_t op3) : "SABA Ztied1.H, Zop2.H, Zop3.H" or "MOVPRFX Zresult, Zop1; SABA Zresult.H, Zop2.H, Zop3.H"
  public static unsafe Vector<short> AbsoluteDifferenceAdd(Vector<short> addend, Vector<short> left, Vector<short> right);

    /// svint32_t svaba[_s32](svint32_t op1, svint32_t op2, svint32_t op3) : "SABA Ztied1.S, Zop2.S, Zop3.S" or "MOVPRFX Zresult, Zop1; SABA Zresult.S, Zop2.S, Zop3.S"
  public static unsafe Vector<int> AbsoluteDifferenceAdd(Vector<int> addend, Vector<int> left, Vector<int> right);

    /// svint64_t svaba[_s64](svint64_t op1, svint64_t op2, svint64_t op3) : "SABA Ztied1.D, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; SABA Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<long> AbsoluteDifferenceAdd(Vector<long> addend, Vector<long> left, Vector<long> right);

    /// svuint8_t svaba[_u8](svuint8_t op1, svuint8_t op2, svuint8_t op3) : "UABA Ztied1.B, Zop2.B, Zop3.B" or "MOVPRFX Zresult, Zop1; UABA Zresult.B, Zop2.B, Zop3.B"
  public static unsafe Vector<byte> AbsoluteDifferenceAdd(Vector<byte> addend, Vector<byte> left, Vector<byte> right);

    /// svuint16_t svaba[_u16](svuint16_t op1, svuint16_t op2, svuint16_t op3) : "UABA Ztied1.H, Zop2.H, Zop3.H" or "MOVPRFX Zresult, Zop1; UABA Zresult.H, Zop2.H, Zop3.H"
  public static unsafe Vector<ushort> AbsoluteDifferenceAdd(Vector<ushort> addend, Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svaba[_u32](svuint32_t op1, svuint32_t op2, svuint32_t op3) : "UABA Ztied1.S, Zop2.S, Zop3.S" or "MOVPRFX Zresult, Zop1; UABA Zresult.S, Zop2.S, Zop3.S"
  public static unsafe Vector<uint> AbsoluteDifferenceAdd(Vector<uint> addend, Vector<uint> left, Vector<uint> right);

    /// svuint64_t svaba[_u64](svuint64_t op1, svuint64_t op2, svuint64_t op3) : "UABA Ztied1.D, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; UABA Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<ulong> AbsoluteDifferenceAdd(Vector<ulong> addend, Vector<ulong> left, Vector<ulong> right);


    /// AbsoluteDifferenceAddWideningLower : Absolute difference and accumulate long (bottom)

    /// svint16_t svabalb[_s16](svint16_t op1, svint8_t op2, svint8_t op3) : "SABALB Ztied1.H, Zop2.B, Zop3.B" or "MOVPRFX Zresult, Zop1; SABALB Zresult.H, Zop2.B, Zop3.B"
  public static unsafe Vector<short> AbsoluteDifferenceAddWideningLower(Vector<short> addend, Vector<sbyte> left, Vector<sbyte> right);

    /// svint32_t svabalb[_s32](svint32_t op1, svint16_t op2, svint16_t op3) : "SABALB Ztied1.S, Zop2.H, Zop3.H" or "MOVPRFX Zresult, Zop1; SABALB Zresult.S, Zop2.H, Zop3.H"
  public static unsafe Vector<int> AbsoluteDifferenceAddWideningLower(Vector<int> addend, Vector<short> left, Vector<short> right);

    /// svint64_t svabalb[_s64](svint64_t op1, svint32_t op2, svint32_t op3) : "SABALB Ztied1.D, Zop2.S, Zop3.S" or "MOVPRFX Zresult, Zop1; SABALB Zresult.D, Zop2.S, Zop3.S"
  public static unsafe Vector<long> AbsoluteDifferenceAddWideningLower(Vector<long> addend, Vector<int> left, Vector<int> right);

    /// svuint16_t svabalb[_u16](svuint16_t op1, svuint8_t op2, svuint8_t op3) : "UABALB Ztied1.H, Zop2.B, Zop3.B" or "MOVPRFX Zresult, Zop1; UABALB Zresult.H, Zop2.B, Zop3.B"
  public static unsafe Vector<ushort> AbsoluteDifferenceAddWideningLower(Vector<ushort> addend, Vector<byte> left, Vector<byte> right);

    /// svuint32_t svabalb[_u32](svuint32_t op1, svuint16_t op2, svuint16_t op3) : "UABALB Ztied1.S, Zop2.H, Zop3.H" or "MOVPRFX Zresult, Zop1; UABALB Zresult.S, Zop2.H, Zop3.H"
  public static unsafe Vector<uint> AbsoluteDifferenceAddWideningLower(Vector<uint> addend, Vector<ushort> left, Vector<ushort> right);

    /// svuint64_t svabalb[_u64](svuint64_t op1, svuint32_t op2, svuint32_t op3) : "UABALB Ztied1.D, Zop2.S, Zop3.S" or "MOVPRFX Zresult, Zop1; UABALB Zresult.D, Zop2.S, Zop3.S"
  public static unsafe Vector<ulong> AbsoluteDifferenceAddWideningLower(Vector<ulong> addend, Vector<uint> left, Vector<uint> right);


    /// AbsoluteDifferenceAddWideningUpper : Absolute difference and accumulate long (top)

    /// svint16_t svabalt[_s16](svint16_t op1, svint8_t op2, svint8_t op3) : "SABALT Ztied1.H, Zop2.B, Zop3.B" or "MOVPRFX Zresult, Zop1; SABALT Zresult.H, Zop2.B, Zop3.B"
  public static unsafe Vector<short> AbsoluteDifferenceAddWideningUpper(Vector<short> addend, Vector<sbyte> left, Vector<sbyte> right);

    /// svint32_t svabalt[_s32](svint32_t op1, svint16_t op2, svint16_t op3) : "SABALT Ztied1.S, Zop2.H, Zop3.H" or "MOVPRFX Zresult, Zop1; SABALT Zresult.S, Zop2.H, Zop3.H"
  public static unsafe Vector<int> AbsoluteDifferenceAddWideningUpper(Vector<int> addend, Vector<short> left, Vector<short> right);

    /// svint64_t svabalt[_s64](svint64_t op1, svint32_t op2, svint32_t op3) : "SABALT Ztied1.D, Zop2.S, Zop3.S" or "MOVPRFX Zresult, Zop1; SABALT Zresult.D, Zop2.S, Zop3.S"
  public static unsafe Vector<long> AbsoluteDifferenceAddWideningUpper(Vector<long> addend, Vector<int> left, Vector<int> right);

    /// svuint16_t svabalt[_u16](svuint16_t op1, svuint8_t op2, svuint8_t op3) : "UABALT Ztied1.H, Zop2.B, Zop3.B" or "MOVPRFX Zresult, Zop1; UABALT Zresult.H, Zop2.B, Zop3.B"
  public static unsafe Vector<ushort> AbsoluteDifferenceAddWideningUpper(Vector<ushort> addend, Vector<byte> left, Vector<byte> right);

    /// svuint32_t svabalt[_u32](svuint32_t op1, svuint16_t op2, svuint16_t op3) : "UABALT Ztied1.S, Zop2.H, Zop3.H" or "MOVPRFX Zresult, Zop1; UABALT Zresult.S, Zop2.H, Zop3.H"
  public static unsafe Vector<uint> AbsoluteDifferenceAddWideningUpper(Vector<uint> addend, Vector<ushort> left, Vector<ushort> right);

    /// svuint64_t svabalt[_u64](svuint64_t op1, svuint32_t op2, svuint32_t op3) : "UABALT Ztied1.D, Zop2.S, Zop3.S" or "MOVPRFX Zresult, Zop1; UABALT Zresult.D, Zop2.S, Zop3.S"
  public static unsafe Vector<ulong> AbsoluteDifferenceAddWideningUpper(Vector<ulong> addend, Vector<uint> left, Vector<uint> right);


    /// AbsoluteDifferenceWideningLower : Absolute difference long (bottom)

    /// svint16_t svabdlb[_s16](svint8_t op1, svint8_t op2) : "SABDLB Zresult.H, Zop1.B, Zop2.B"
  public static unsafe Vector<short> AbsoluteDifferenceWideningLower(Vector<sbyte> left, Vector<sbyte> right);

    /// svint32_t svabdlb[_s32](svint16_t op1, svint16_t op2) : "SABDLB Zresult.S, Zop1.H, Zop2.H"
  public static unsafe Vector<int> AbsoluteDifferenceWideningLower(Vector<short> left, Vector<short> right);

    /// svint64_t svabdlb[_s64](svint32_t op1, svint32_t op2) : "SABDLB Zresult.D, Zop1.S, Zop2.S"
  public static unsafe Vector<long> AbsoluteDifferenceWideningLower(Vector<int> left, Vector<int> right);

    /// svuint16_t svabdlb[_u16](svuint8_t op1, svuint8_t op2) : "UABDLB Zresult.H, Zop1.B, Zop2.B"
  public static unsafe Vector<ushort> AbsoluteDifferenceWideningLower(Vector<byte> left, Vector<byte> right);

    /// svuint32_t svabdlb[_u32](svuint16_t op1, svuint16_t op2) : "UABDLB Zresult.S, Zop1.H, Zop2.H"
  public static unsafe Vector<uint> AbsoluteDifferenceWideningLower(Vector<ushort> left, Vector<ushort> right);

    /// svuint64_t svabdlb[_u64](svuint32_t op1, svuint32_t op2) : "UABDLB Zresult.D, Zop1.S, Zop2.S"
  public static unsafe Vector<ulong> AbsoluteDifferenceWideningLower(Vector<uint> left, Vector<uint> right);


    /// AbsoluteDifferenceWideningUpper : Absolute difference long (top)

    /// svint16_t svabdlt[_s16](svint8_t op1, svint8_t op2) : "SABDLT Zresult.H, Zop1.B, Zop2.B"
  public static unsafe Vector<short> AbsoluteDifferenceWideningUpper(Vector<sbyte> left, Vector<sbyte> right);

    /// svint32_t svabdlt[_s32](svint16_t op1, svint16_t op2) : "SABDLT Zresult.S, Zop1.H, Zop2.H"
  public static unsafe Vector<int> AbsoluteDifferenceWideningUpper(Vector<short> left, Vector<short> right);

    /// svint64_t svabdlt[_s64](svint32_t op1, svint32_t op2) : "SABDLT Zresult.D, Zop1.S, Zop2.S"
  public static unsafe Vector<long> AbsoluteDifferenceWideningUpper(Vector<int> left, Vector<int> right);

    /// svuint16_t svabdlt[_u16](svuint8_t op1, svuint8_t op2) : "UABDLT Zresult.H, Zop1.B, Zop2.B"
  public static unsafe Vector<ushort> AbsoluteDifferenceWideningUpper(Vector<byte> left, Vector<byte> right);

    /// svuint32_t svabdlt[_u32](svuint16_t op1, svuint16_t op2) : "UABDLT Zresult.S, Zop1.H, Zop2.H"
  public static unsafe Vector<uint> AbsoluteDifferenceWideningUpper(Vector<ushort> left, Vector<ushort> right);

    /// svuint64_t svabdlt[_u64](svuint32_t op1, svuint32_t op2) : "UABDLT Zresult.D, Zop1.S, Zop2.S"
  public static unsafe Vector<ulong> AbsoluteDifferenceWideningUpper(Vector<uint> left, Vector<uint> right);


    /// AddCarryWideningLower : Add with carry long (bottom)

    /// svuint32_t svadclb[_u32](svuint32_t op1, svuint32_t op2, svuint32_t op3) : "ADCLB Ztied1.S, Zop2.S, Zop3.S" or "MOVPRFX Zresult, Zop1; ADCLB Zresult.S, Zop2.S, Zop3.S"
  public static unsafe Vector<uint> AddCarryWideningLower(Vector<uint> op1, Vector<uint> op2, Vector<uint> op3);

    /// svuint64_t svadclb[_u64](svuint64_t op1, svuint64_t op2, svuint64_t op3) : "ADCLB Ztied1.D, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; ADCLB Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<ulong> AddCarryWideningLower(Vector<ulong> op1, Vector<ulong> op2, Vector<ulong> op3);


    /// AddCarryWideningUpper : Add with carry long (top)

    /// svuint32_t svadclt[_u32](svuint32_t op1, svuint32_t op2, svuint32_t op3) : "ADCLT Ztied1.S, Zop2.S, Zop3.S" or "MOVPRFX Zresult, Zop1; ADCLT Zresult.S, Zop2.S, Zop3.S"
  public static unsafe Vector<uint> AddCarryWideningUpper(Vector<uint> op1, Vector<uint> op2, Vector<uint> op3);

    /// svuint64_t svadclt[_u64](svuint64_t op1, svuint64_t op2, svuint64_t op3) : "ADCLT Ztied1.D, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; ADCLT Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<ulong> AddCarryWideningUpper(Vector<ulong> op1, Vector<ulong> op2, Vector<ulong> op3);


    /// AddHighNarowingLower : Add narrow high part (bottom)

    /// svint8_t svaddhnb[_s16](svint16_t op1, svint16_t op2) : "ADDHNB Zresult.B, Zop1.H, Zop2.H"
  public static unsafe Vector<sbyte> AddHighNarowingLower(Vector<short> left, Vector<short> right);

    /// svint16_t svaddhnb[_s32](svint32_t op1, svint32_t op2) : "ADDHNB Zresult.H, Zop1.S, Zop2.S"
  public static unsafe Vector<short> AddHighNarowingLower(Vector<int> left, Vector<int> right);

    /// svint32_t svaddhnb[_s64](svint64_t op1, svint64_t op2) : "ADDHNB Zresult.S, Zop1.D, Zop2.D"
  public static unsafe Vector<int> AddHighNarowingLower(Vector<long> left, Vector<long> right);

    /// svuint8_t svaddhnb[_u16](svuint16_t op1, svuint16_t op2) : "ADDHNB Zresult.B, Zop1.H, Zop2.H"
  public static unsafe Vector<byte> AddHighNarowingLower(Vector<ushort> left, Vector<ushort> right);

    /// svuint16_t svaddhnb[_u32](svuint32_t op1, svuint32_t op2) : "ADDHNB Zresult.H, Zop1.S, Zop2.S"
  public static unsafe Vector<ushort> AddHighNarowingLower(Vector<uint> left, Vector<uint> right);

    /// svuint32_t svaddhnb[_u64](svuint64_t op1, svuint64_t op2) : "ADDHNB Zresult.S, Zop1.D, Zop2.D"
  public static unsafe Vector<uint> AddHighNarowingLower(Vector<ulong> left, Vector<ulong> right);


    /// AddHighNarowingUpper : Add narrow high part (top)

    /// svint8_t svaddhnt[_s16](svint8_t even, svint16_t op1, svint16_t op2) : "ADDHNT Ztied.B, Zop1.H, Zop2.H"
  public static unsafe Vector<sbyte> AddHighNarowingUpper(Vector<sbyte> even, Vector<short> left, Vector<short> right);

    /// svint16_t svaddhnt[_s32](svint16_t even, svint32_t op1, svint32_t op2) : "ADDHNT Ztied.H, Zop1.S, Zop2.S"
  public static unsafe Vector<short> AddHighNarowingUpper(Vector<short> even, Vector<int> left, Vector<int> right);

    /// svint32_t svaddhnt[_s64](svint32_t even, svint64_t op1, svint64_t op2) : "ADDHNT Ztied.S, Zop1.D, Zop2.D"
  public static unsafe Vector<int> AddHighNarowingUpper(Vector<int> even, Vector<long> left, Vector<long> right);

    /// svuint8_t svaddhnt[_u16](svuint8_t even, svuint16_t op1, svuint16_t op2) : "ADDHNT Ztied.B, Zop1.H, Zop2.H"
  public static unsafe Vector<byte> AddHighNarowingUpper(Vector<byte> even, Vector<ushort> left, Vector<ushort> right);

    /// svuint16_t svaddhnt[_u32](svuint16_t even, svuint32_t op1, svuint32_t op2) : "ADDHNT Ztied.H, Zop1.S, Zop2.S"
  public static unsafe Vector<ushort> AddHighNarowingUpper(Vector<ushort> even, Vector<uint> left, Vector<uint> right);

    /// svuint32_t svaddhnt[_u64](svuint32_t even, svuint64_t op1, svuint64_t op2) : "ADDHNT Ztied.S, Zop1.D, Zop2.D"
  public static unsafe Vector<uint> AddHighNarowingUpper(Vector<uint> even, Vector<ulong> left, Vector<ulong> right);


    /// AddPairwise : Add pairwise

    /// svfloat32_t svaddp[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "FADDP Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; FADDP Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svfloat32_t svaddp[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "FADDP Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; FADDP Zresult.S, Pg/M, Zresult.S, Zop2.S"
  public static unsafe Vector<float> AddPairwise(Vector<float> left, Vector<float> right);

    /// svfloat64_t svaddp[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "FADDP Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; FADDP Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svfloat64_t svaddp[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "FADDP Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; FADDP Zresult.D, Pg/M, Zresult.D, Zop2.D"
  public static unsafe Vector<double> AddPairwise(Vector<double> left, Vector<double> right);

    /// svint8_t svaddp[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2) : "ADDP Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; ADDP Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svint8_t svaddp[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2) : "ADDP Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; ADDP Zresult.B, Pg/M, Zresult.B, Zop2.B"
  public static unsafe Vector<sbyte> AddPairwise(Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t svaddp[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2) : "ADDP Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; ADDP Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svint16_t svaddp[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2) : "ADDP Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; ADDP Zresult.H, Pg/M, Zresult.H, Zop2.H"
  public static unsafe Vector<short> AddPairwise(Vector<short> left, Vector<short> right);

    /// svint32_t svaddp[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2) : "ADDP Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; ADDP Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svint32_t svaddp[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2) : "ADDP Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; ADDP Zresult.S, Pg/M, Zresult.S, Zop2.S"
  public static unsafe Vector<int> AddPairwise(Vector<int> left, Vector<int> right);

    /// svint64_t svaddp[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2) : "ADDP Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; ADDP Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svint64_t svaddp[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2) : "ADDP Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; ADDP Zresult.D, Pg/M, Zresult.D, Zop2.D"
  public static unsafe Vector<long> AddPairwise(Vector<long> left, Vector<long> right);

    /// svuint8_t svaddp[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2) : "ADDP Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; ADDP Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svuint8_t svaddp[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2) : "ADDP Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; ADDP Zresult.B, Pg/M, Zresult.B, Zop2.B"
  public static unsafe Vector<byte> AddPairwise(Vector<byte> left, Vector<byte> right);

    /// svuint16_t svaddp[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2) : "ADDP Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; ADDP Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svuint16_t svaddp[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2) : "ADDP Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; ADDP Zresult.H, Pg/M, Zresult.H, Zop2.H"
  public static unsafe Vector<ushort> AddPairwise(Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svaddp[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2) : "ADDP Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; ADDP Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svuint32_t svaddp[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2) : "ADDP Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; ADDP Zresult.S, Pg/M, Zresult.S, Zop2.S"
  public static unsafe Vector<uint> AddPairwise(Vector<uint> left, Vector<uint> right);

    /// svuint64_t svaddp[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2) : "ADDP Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; ADDP Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svuint64_t svaddp[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2) : "ADDP Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; ADDP Zresult.D, Pg/M, Zresult.D, Zop2.D"
  public static unsafe Vector<ulong> AddPairwise(Vector<ulong> left, Vector<ulong> right);


    /// AddPairwiseWidening : Add and accumulate long pairwise

    /// svint16_t svadalp[_s16]_m(svbool_t pg, svint16_t op1, svint8_t op2) : "SADALP Ztied1.H, Pg/M, Zop2.B" or "MOVPRFX Zresult, Zop1; SADALP Zresult.H, Pg/M, Zop2.B"
    /// svint16_t svadalp[_s16]_x(svbool_t pg, svint16_t op1, svint8_t op2) : "SADALP Ztied1.H, Pg/M, Zop2.B" or "MOVPRFX Zresult, Zop1; SADALP Zresult.H, Pg/M, Zop2.B"
    /// svint16_t svadalp[_s16]_z(svbool_t pg, svint16_t op1, svint8_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; SADALP Zresult.H, Pg/M, Zop2.B"
  public static unsafe Vector<short> AddPairwiseWidening(Vector<short> left, Vector<sbyte> right);

    /// svint32_t svadalp[_s32]_m(svbool_t pg, svint32_t op1, svint16_t op2) : "SADALP Ztied1.S, Pg/M, Zop2.H" or "MOVPRFX Zresult, Zop1; SADALP Zresult.S, Pg/M, Zop2.H"
    /// svint32_t svadalp[_s32]_x(svbool_t pg, svint32_t op1, svint16_t op2) : "SADALP Ztied1.S, Pg/M, Zop2.H" or "MOVPRFX Zresult, Zop1; SADALP Zresult.S, Pg/M, Zop2.H"
    /// svint32_t svadalp[_s32]_z(svbool_t pg, svint32_t op1, svint16_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; SADALP Zresult.S, Pg/M, Zop2.H"
  public static unsafe Vector<int> AddPairwiseWidening(Vector<int> left, Vector<short> right);

    /// svint64_t svadalp[_s64]_m(svbool_t pg, svint64_t op1, svint32_t op2) : "SADALP Ztied1.D, Pg/M, Zop2.S" or "MOVPRFX Zresult, Zop1; SADALP Zresult.D, Pg/M, Zop2.S"
    /// svint64_t svadalp[_s64]_x(svbool_t pg, svint64_t op1, svint32_t op2) : "SADALP Ztied1.D, Pg/M, Zop2.S" or "MOVPRFX Zresult, Zop1; SADALP Zresult.D, Pg/M, Zop2.S"
    /// svint64_t svadalp[_s64]_z(svbool_t pg, svint64_t op1, svint32_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; SADALP Zresult.D, Pg/M, Zop2.S"
  public static unsafe Vector<long> AddPairwiseWidening(Vector<long> left, Vector<int> right);

    /// svuint16_t svadalp[_u16]_m(svbool_t pg, svuint16_t op1, svuint8_t op2) : "UADALP Ztied1.H, Pg/M, Zop2.B" or "MOVPRFX Zresult, Zop1; UADALP Zresult.H, Pg/M, Zop2.B"
    /// svuint16_t svadalp[_u16]_x(svbool_t pg, svuint16_t op1, svuint8_t op2) : "UADALP Ztied1.H, Pg/M, Zop2.B" or "MOVPRFX Zresult, Zop1; UADALP Zresult.H, Pg/M, Zop2.B"
    /// svuint16_t svadalp[_u16]_z(svbool_t pg, svuint16_t op1, svuint8_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; UADALP Zresult.H, Pg/M, Zop2.B"
  public static unsafe Vector<ushort> AddPairwiseWidening(Vector<ushort> left, Vector<byte> right);

    /// svuint32_t svadalp[_u32]_m(svbool_t pg, svuint32_t op1, svuint16_t op2) : "UADALP Ztied1.S, Pg/M, Zop2.H" or "MOVPRFX Zresult, Zop1; UADALP Zresult.S, Pg/M, Zop2.H"
    /// svuint32_t svadalp[_u32]_x(svbool_t pg, svuint32_t op1, svuint16_t op2) : "UADALP Ztied1.S, Pg/M, Zop2.H" or "MOVPRFX Zresult, Zop1; UADALP Zresult.S, Pg/M, Zop2.H"
    /// svuint32_t svadalp[_u32]_z(svbool_t pg, svuint32_t op1, svuint16_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; UADALP Zresult.S, Pg/M, Zop2.H"
  public static unsafe Vector<uint> AddPairwiseWidening(Vector<uint> left, Vector<ushort> right);

    /// svuint64_t svadalp[_u64]_m(svbool_t pg, svuint64_t op1, svuint32_t op2) : "UADALP Ztied1.D, Pg/M, Zop2.S" or "MOVPRFX Zresult, Zop1; UADALP Zresult.D, Pg/M, Zop2.S"
    /// svuint64_t svadalp[_u64]_x(svbool_t pg, svuint64_t op1, svuint32_t op2) : "UADALP Ztied1.D, Pg/M, Zop2.S" or "MOVPRFX Zresult, Zop1; UADALP Zresult.D, Pg/M, Zop2.S"
    /// svuint64_t svadalp[_u64]_z(svbool_t pg, svuint64_t op1, svuint32_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; UADALP Zresult.D, Pg/M, Zop2.S"
  public static unsafe Vector<ulong> AddPairwiseWidening(Vector<ulong> left, Vector<uint> right);


    /// AddSaturate : Saturating add

    /// svint8_t svqadd[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2) : "SQADD Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; SQADD Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svint8_t svqadd[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2) : "SQADD Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "SQADD Ztied2.B, Pg/M, Ztied2.B, Zop1.B" or "SQADD Zresult.B, Zop1.B, Zop2.B"
    /// svint8_t svqadd[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; SQADD Zresult.B, Pg/M, Zresult.B, Zop2.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; SQADD Zresult.B, Pg/M, Zresult.B, Zop1.B"
  public static unsafe Vector<sbyte> AddSaturate(Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t svqadd[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2) : "SQADD Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; SQADD Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svint16_t svqadd[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2) : "SQADD Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "SQADD Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "SQADD Zresult.H, Zop1.H, Zop2.H"
    /// svint16_t svqadd[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; SQADD Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; SQADD Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<short> AddSaturate(Vector<short> left, Vector<short> right);

    /// svint32_t svqadd[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2) : "SQADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; SQADD Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svint32_t svqadd[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2) : "SQADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "SQADD Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "SQADD Zresult.S, Zop1.S, Zop2.S"
    /// svint32_t svqadd[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; SQADD Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; SQADD Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<int> AddSaturate(Vector<int> left, Vector<int> right);

    /// svint64_t svqadd[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2) : "SQADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; SQADD Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svint64_t svqadd[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2) : "SQADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "SQADD Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "SQADD Zresult.D, Zop1.D, Zop2.D"
    /// svint64_t svqadd[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; SQADD Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; SQADD Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<long> AddSaturate(Vector<long> left, Vector<long> right);

    /// svuint8_t svqadd[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2) : "UQADD Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; UQADD Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svuint8_t svqadd[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2) : "UQADD Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "UQADD Ztied2.B, Pg/M, Ztied2.B, Zop1.B" or "UQADD Zresult.B, Zop1.B, Zop2.B"
    /// svuint8_t svqadd[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; UQADD Zresult.B, Pg/M, Zresult.B, Zop2.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; UQADD Zresult.B, Pg/M, Zresult.B, Zop1.B"
  public static unsafe Vector<byte> AddSaturate(Vector<byte> left, Vector<byte> right);

    /// svuint16_t svqadd[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2) : "UQADD Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; UQADD Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svuint16_t svqadd[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2) : "UQADD Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "UQADD Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "UQADD Zresult.H, Zop1.H, Zop2.H"
    /// svuint16_t svqadd[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; UQADD Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; UQADD Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<ushort> AddSaturate(Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svqadd[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2) : "UQADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; UQADD Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svuint32_t svqadd[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2) : "UQADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "UQADD Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "UQADD Zresult.S, Zop1.S, Zop2.S"
    /// svuint32_t svqadd[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; UQADD Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; UQADD Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<uint> AddSaturate(Vector<uint> left, Vector<uint> right);

    /// svuint64_t svqadd[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2) : "UQADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; UQADD Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svuint64_t svqadd[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2) : "UQADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "UQADD Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "UQADD Zresult.D, Zop1.D, Zop2.D"
    /// svuint64_t svqadd[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; UQADD Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; UQADD Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<ulong> AddSaturate(Vector<ulong> left, Vector<ulong> right);


    /// AddSaturateWithSignedAddend : Saturating add with signed addend

    /// svuint8_t svsqadd[_u8]_m(svbool_t pg, svuint8_t op1, svint8_t op2) : "USQADD Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; USQADD Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svuint8_t svsqadd[_u8]_x(svbool_t pg, svuint8_t op1, svint8_t op2) : "USQADD Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; USQADD Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svuint8_t svsqadd[_u8]_z(svbool_t pg, svuint8_t op1, svint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; USQADD Zresult.B, Pg/M, Zresult.B, Zop2.B"
  public static unsafe Vector<byte> AddSaturateWithSignedAddend(Vector<byte> left, Vector<sbyte> right);

    /// svuint16_t svsqadd[_u16]_m(svbool_t pg, svuint16_t op1, svint16_t op2) : "USQADD Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; USQADD Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svuint16_t svsqadd[_u16]_x(svbool_t pg, svuint16_t op1, svint16_t op2) : "USQADD Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; USQADD Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svuint16_t svsqadd[_u16]_z(svbool_t pg, svuint16_t op1, svint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; USQADD Zresult.H, Pg/M, Zresult.H, Zop2.H"
  public static unsafe Vector<ushort> AddSaturateWithSignedAddend(Vector<ushort> left, Vector<short> right);

    /// svuint32_t svsqadd[_u32]_m(svbool_t pg, svuint32_t op1, svint32_t op2) : "USQADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; USQADD Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svuint32_t svsqadd[_u32]_x(svbool_t pg, svuint32_t op1, svint32_t op2) : "USQADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; USQADD Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svuint32_t svsqadd[_u32]_z(svbool_t pg, svuint32_t op1, svint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; USQADD Zresult.S, Pg/M, Zresult.S, Zop2.S"
  public static unsafe Vector<uint> AddSaturateWithSignedAddend(Vector<uint> left, Vector<int> right);

    /// svuint64_t svsqadd[_u64]_m(svbool_t pg, svuint64_t op1, svint64_t op2) : "USQADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; USQADD Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svuint64_t svsqadd[_u64]_x(svbool_t pg, svuint64_t op1, svint64_t op2) : "USQADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; USQADD Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svuint64_t svsqadd[_u64]_z(svbool_t pg, svuint64_t op1, svint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; USQADD Zresult.D, Pg/M, Zresult.D, Zop2.D"
  public static unsafe Vector<ulong> AddSaturateWithSignedAddend(Vector<ulong> left, Vector<long> right);


    /// AddSaturateWithUnsignedAddend : Saturating add with unsigned addend

    /// svint8_t svuqadd[_s8]_m(svbool_t pg, svint8_t op1, svuint8_t op2) : "SUQADD Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; SUQADD Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svint8_t svuqadd[_s8]_x(svbool_t pg, svint8_t op1, svuint8_t op2) : "SUQADD Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; SUQADD Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svint8_t svuqadd[_s8]_z(svbool_t pg, svint8_t op1, svuint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; SUQADD Zresult.B, Pg/M, Zresult.B, Zop2.B"
  public static unsafe Vector<sbyte> AddSaturateWithUnsignedAddend(Vector<sbyte> left, Vector<byte> right);

    /// svint16_t svuqadd[_s16]_m(svbool_t pg, svint16_t op1, svuint16_t op2) : "SUQADD Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; SUQADD Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svint16_t svuqadd[_s16]_x(svbool_t pg, svint16_t op1, svuint16_t op2) : "SUQADD Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; SUQADD Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svint16_t svuqadd[_s16]_z(svbool_t pg, svint16_t op1, svuint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; SUQADD Zresult.H, Pg/M, Zresult.H, Zop2.H"
  public static unsafe Vector<short> AddSaturateWithUnsignedAddend(Vector<short> left, Vector<ushort> right);

    /// svint32_t svuqadd[_s32]_m(svbool_t pg, svint32_t op1, svuint32_t op2) : "SUQADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; SUQADD Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svint32_t svuqadd[_s32]_x(svbool_t pg, svint32_t op1, svuint32_t op2) : "SUQADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; SUQADD Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svint32_t svuqadd[_s32]_z(svbool_t pg, svint32_t op1, svuint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; SUQADD Zresult.S, Pg/M, Zresult.S, Zop2.S"
  public static unsafe Vector<int> AddSaturateWithUnsignedAddend(Vector<int> left, Vector<uint> right);

    /// svint64_t svuqadd[_s64]_m(svbool_t pg, svint64_t op1, svuint64_t op2) : "SUQADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; SUQADD Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svint64_t svuqadd[_s64]_x(svbool_t pg, svint64_t op1, svuint64_t op2) : "SUQADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; SUQADD Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svint64_t svuqadd[_s64]_z(svbool_t pg, svint64_t op1, svuint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; SUQADD Zresult.D, Pg/M, Zresult.D, Zop2.D"
  public static unsafe Vector<long> AddSaturateWithUnsignedAddend(Vector<long> left, Vector<ulong> right);


    /// AddWideLower : Add wide (bottom)

    /// svint16_t svaddwb[_s16](svint16_t op1, svint8_t op2) : "SADDWB Zresult.H, Zop1.H, Zop2.B"
  public static unsafe Vector<short> AddWideLower(Vector<short> left, Vector<sbyte> right);

    /// svint32_t svaddwb[_s32](svint32_t op1, svint16_t op2) : "SADDWB Zresult.S, Zop1.S, Zop2.H"
  public static unsafe Vector<int> AddWideLower(Vector<int> left, Vector<short> right);

    /// svint64_t svaddwb[_s64](svint64_t op1, svint32_t op2) : "SADDWB Zresult.D, Zop1.D, Zop2.S"
  public static unsafe Vector<long> AddWideLower(Vector<long> left, Vector<int> right);

    /// svuint16_t svaddwb[_u16](svuint16_t op1, svuint8_t op2) : "UADDWB Zresult.H, Zop1.H, Zop2.B"
  public static unsafe Vector<ushort> AddWideLower(Vector<ushort> left, Vector<byte> right);

    /// svuint32_t svaddwb[_u32](svuint32_t op1, svuint16_t op2) : "UADDWB Zresult.S, Zop1.S, Zop2.H"
  public static unsafe Vector<uint> AddWideLower(Vector<uint> left, Vector<ushort> right);

    /// svuint64_t svaddwb[_u64](svuint64_t op1, svuint32_t op2) : "UADDWB Zresult.D, Zop1.D, Zop2.S"
  public static unsafe Vector<ulong> AddWideLower(Vector<ulong> left, Vector<uint> right);


    /// AddWideUpper : Add wide (top)

    /// svint16_t svaddwt[_s16](svint16_t op1, svint8_t op2) : "SADDWT Zresult.H, Zop1.H, Zop2.B"
  public static unsafe Vector<short> AddWideUpper(Vector<short> left, Vector<sbyte> right);

    /// svint32_t svaddwt[_s32](svint32_t op1, svint16_t op2) : "SADDWT Zresult.S, Zop1.S, Zop2.H"
  public static unsafe Vector<int> AddWideUpper(Vector<int> left, Vector<short> right);

    /// svint64_t svaddwt[_s64](svint64_t op1, svint32_t op2) : "SADDWT Zresult.D, Zop1.D, Zop2.S"
  public static unsafe Vector<long> AddWideUpper(Vector<long> left, Vector<int> right);

    /// svuint16_t svaddwt[_u16](svuint16_t op1, svuint8_t op2) : "UADDWT Zresult.H, Zop1.H, Zop2.B"
  public static unsafe Vector<ushort> AddWideUpper(Vector<ushort> left, Vector<byte> right);

    /// svuint32_t svaddwt[_u32](svuint32_t op1, svuint16_t op2) : "UADDWT Zresult.S, Zop1.S, Zop2.H"
  public static unsafe Vector<uint> AddWideUpper(Vector<uint> left, Vector<ushort> right);

    /// svuint64_t svaddwt[_u64](svuint64_t op1, svuint32_t op2) : "UADDWT Zresult.D, Zop1.D, Zop2.S"
  public static unsafe Vector<ulong> AddWideUpper(Vector<ulong> left, Vector<uint> right);


    /// AddWideningLower : Add long (bottom)

    /// svint16_t svaddlb[_s16](svint8_t op1, svint8_t op2) : "SADDLB Zresult.H, Zop1.B, Zop2.B"
  public static unsafe Vector<short> AddWideningLower(Vector<sbyte> left, Vector<sbyte> right);

    /// svint32_t svaddlb[_s32](svint16_t op1, svint16_t op2) : "SADDLB Zresult.S, Zop1.H, Zop2.H"
  public static unsafe Vector<int> AddWideningLower(Vector<short> left, Vector<short> right);

    /// svint64_t svaddlb[_s64](svint32_t op1, svint32_t op2) : "SADDLB Zresult.D, Zop1.S, Zop2.S"
  public static unsafe Vector<long> AddWideningLower(Vector<int> left, Vector<int> right);

    /// svuint16_t svaddlb[_u16](svuint8_t op1, svuint8_t op2) : "UADDLB Zresult.H, Zop1.B, Zop2.B"
  public static unsafe Vector<ushort> AddWideningLower(Vector<byte> left, Vector<byte> right);

    /// svuint32_t svaddlb[_u32](svuint16_t op1, svuint16_t op2) : "UADDLB Zresult.S, Zop1.H, Zop2.H"
  public static unsafe Vector<uint> AddWideningLower(Vector<ushort> left, Vector<ushort> right);

    /// svuint64_t svaddlb[_u64](svuint32_t op1, svuint32_t op2) : "UADDLB Zresult.D, Zop1.S, Zop2.S"
  public static unsafe Vector<ulong> AddWideningLower(Vector<uint> left, Vector<uint> right);


    /// AddWideningLowerUpper : Add long (bottom + top)

    /// svint16_t svaddlbt[_s16](svint8_t op1, svint8_t op2) : "SADDLBT Zresult.H, Zop1.B, Zop2.B"
  public static unsafe Vector<short> AddWideningLowerUpper(Vector<sbyte> left, Vector<sbyte> right);

    /// svint32_t svaddlbt[_s32](svint16_t op1, svint16_t op2) : "SADDLBT Zresult.S, Zop1.H, Zop2.H"
  public static unsafe Vector<int> AddWideningLowerUpper(Vector<short> left, Vector<short> right);

    /// svint64_t svaddlbt[_s64](svint32_t op1, svint32_t op2) : "SADDLBT Zresult.D, Zop1.S, Zop2.S"
  public static unsafe Vector<long> AddWideningLowerUpper(Vector<int> left, Vector<int> right);


    /// AddWideningUpper : Add long (top)

    /// svint16_t svaddlt[_s16](svint8_t op1, svint8_t op2) : "SADDLT Zresult.H, Zop1.B, Zop2.B"
  public static unsafe Vector<short> AddWideningUpper(Vector<sbyte> left, Vector<sbyte> right);

    /// svint32_t svaddlt[_s32](svint16_t op1, svint16_t op2) : "SADDLT Zresult.S, Zop1.H, Zop2.H"
  public static unsafe Vector<int> AddWideningUpper(Vector<short> left, Vector<short> right);

    /// svint64_t svaddlt[_s64](svint32_t op1, svint32_t op2) : "SADDLT Zresult.D, Zop1.S, Zop2.S"
  public static unsafe Vector<long> AddWideningUpper(Vector<int> left, Vector<int> right);

    /// svuint16_t svaddlt[_u16](svuint8_t op1, svuint8_t op2) : "UADDLT Zresult.H, Zop1.B, Zop2.B"
  public static unsafe Vector<ushort> AddWideningUpper(Vector<byte> left, Vector<byte> right);

    /// svuint32_t svaddlt[_u32](svuint16_t op1, svuint16_t op2) : "UADDLT Zresult.S, Zop1.H, Zop2.H"
  public static unsafe Vector<uint> AddWideningUpper(Vector<ushort> left, Vector<ushort> right);

    /// svuint64_t svaddlt[_u64](svuint32_t op1, svuint32_t op2) : "UADDLT Zresult.D, Zop1.S, Zop2.S"
  public static unsafe Vector<ulong> AddWideningUpper(Vector<uint> left, Vector<uint> right);


    /// DotProductComplex : Complex dot product

    /// svint32_t svcdot[_s32](svint32_t op1, svint8_t op2, svint8_t op3, uint64_t imm_rotation) : "CDOT Ztied1.S, Zop2.B, Zop3.B, #imm_rotation" or "MOVPRFX Zresult, Zop1; CDOT Zresult.S, Zop2.B, Zop3.B, #imm_rotation"
  public static unsafe Vector<int> DotProductComplex(Vector<int> op1, Vector<sbyte> op2, Vector<sbyte> op3, [ConstantExpected] byte rotation);

    /// svint64_t svcdot[_s64](svint64_t op1, svint16_t op2, svint16_t op3, uint64_t imm_rotation) : "CDOT Ztied1.D, Zop2.H, Zop3.H, #imm_rotation" or "MOVPRFX Zresult, Zop1; CDOT Zresult.D, Zop2.H, Zop3.H, #imm_rotation"
  public static unsafe Vector<long> DotProductComplex(Vector<long> op1, Vector<short> op2, Vector<short> op3, [ConstantExpected] byte rotation);

    /// svint32_t svcdot_lane[_s32](svint32_t op1, svint8_t op2, svint8_t op3, uint64_t imm_index, uint64_t imm_rotation) : "CDOT Ztied1.S, Zop2.B, Zop3.B[imm_index], #imm_rotation" or "MOVPRFX Zresult, Zop1; CDOT Zresult.S, Zop2.B, Zop3.B[imm_index], #imm_rotation"
  public static unsafe Vector<int> DotProductComplex(Vector<int> op1, Vector<sbyte> op2, Vector<sbyte> op3, ulong imm_index, [ConstantExpected] byte rotation);

    /// svint64_t svcdot_lane[_s64](svint64_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index, uint64_t imm_rotation) : "CDOT Ztied1.D, Zop2.H, Zop3.H[imm_index], #imm_rotation" or "MOVPRFX Zresult, Zop1; CDOT Zresult.D, Zop2.H, Zop3.H[imm_index], #imm_rotation"
  public static unsafe Vector<long> DotProductComplex(Vector<long> op1, Vector<short> op2, Vector<short> op3, ulong imm_index, [ConstantExpected] byte rotation);


    /// HalvingAdd : Halving add

    /// svint8_t svhadd[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2) : "SHADD Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; SHADD Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svint8_t svhadd[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2) : "SHADD Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "SHADD Ztied2.B, Pg/M, Ztied2.B, Zop1.B" or "MOVPRFX Zresult, Zop1; SHADD Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svint8_t svhadd[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; SHADD Zresult.B, Pg/M, Zresult.B, Zop2.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; SHADD Zresult.B, Pg/M, Zresult.B, Zop1.B"
  public static unsafe Vector<sbyte> HalvingAdd(Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t svhadd[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2) : "SHADD Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; SHADD Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svint16_t svhadd[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2) : "SHADD Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "SHADD Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "MOVPRFX Zresult, Zop1; SHADD Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svint16_t svhadd[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; SHADD Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; SHADD Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<short> HalvingAdd(Vector<short> left, Vector<short> right);

    /// svint32_t svhadd[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2) : "SHADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; SHADD Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svint32_t svhadd[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2) : "SHADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "SHADD Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "MOVPRFX Zresult, Zop1; SHADD Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svint32_t svhadd[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; SHADD Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; SHADD Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<int> HalvingAdd(Vector<int> left, Vector<int> right);

    /// svint64_t svhadd[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2) : "SHADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; SHADD Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svint64_t svhadd[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2) : "SHADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "SHADD Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "MOVPRFX Zresult, Zop1; SHADD Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svint64_t svhadd[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; SHADD Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; SHADD Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<long> HalvingAdd(Vector<long> left, Vector<long> right);

    /// svuint8_t svhadd[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2) : "UHADD Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; UHADD Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svuint8_t svhadd[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2) : "UHADD Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "UHADD Ztied2.B, Pg/M, Ztied2.B, Zop1.B" or "MOVPRFX Zresult, Zop1; UHADD Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svuint8_t svhadd[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; UHADD Zresult.B, Pg/M, Zresult.B, Zop2.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; UHADD Zresult.B, Pg/M, Zresult.B, Zop1.B"
  public static unsafe Vector<byte> HalvingAdd(Vector<byte> left, Vector<byte> right);

    /// svuint16_t svhadd[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2) : "UHADD Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; UHADD Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svuint16_t svhadd[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2) : "UHADD Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "UHADD Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "MOVPRFX Zresult, Zop1; UHADD Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svuint16_t svhadd[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; UHADD Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; UHADD Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<ushort> HalvingAdd(Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svhadd[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2) : "UHADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; UHADD Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svuint32_t svhadd[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2) : "UHADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "UHADD Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "MOVPRFX Zresult, Zop1; UHADD Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svuint32_t svhadd[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; UHADD Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; UHADD Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<uint> HalvingAdd(Vector<uint> left, Vector<uint> right);

    /// svuint64_t svhadd[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2) : "UHADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; UHADD Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svuint64_t svhadd[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2) : "UHADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "UHADD Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "MOVPRFX Zresult, Zop1; UHADD Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svuint64_t svhadd[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; UHADD Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; UHADD Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<ulong> HalvingAdd(Vector<ulong> left, Vector<ulong> right);


    /// HalvingSubtract : Halving subtract

    /// svint8_t svhsub[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2) : "SHSUB Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; SHSUB Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svint8_t svhsub[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2) : "SHSUB Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "SHSUBR Ztied2.B, Pg/M, Ztied2.B, Zop1.B" or "MOVPRFX Zresult, Zop1; SHSUB Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svint8_t svhsub[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; SHSUB Zresult.B, Pg/M, Zresult.B, Zop2.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; SHSUBR Zresult.B, Pg/M, Zresult.B, Zop1.B"
  public static unsafe Vector<sbyte> HalvingSubtract(Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t svhsub[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2) : "SHSUB Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; SHSUB Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svint16_t svhsub[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2) : "SHSUB Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "SHSUBR Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "MOVPRFX Zresult, Zop1; SHSUB Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svint16_t svhsub[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; SHSUB Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; SHSUBR Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<short> HalvingSubtract(Vector<short> left, Vector<short> right);

    /// svint32_t svhsub[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2) : "SHSUB Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; SHSUB Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svint32_t svhsub[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2) : "SHSUB Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "SHSUBR Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "MOVPRFX Zresult, Zop1; SHSUB Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svint32_t svhsub[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; SHSUB Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; SHSUBR Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<int> HalvingSubtract(Vector<int> left, Vector<int> right);

    /// svint64_t svhsub[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2) : "SHSUB Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; SHSUB Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svint64_t svhsub[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2) : "SHSUB Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "SHSUBR Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "MOVPRFX Zresult, Zop1; SHSUB Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svint64_t svhsub[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; SHSUB Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; SHSUBR Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<long> HalvingSubtract(Vector<long> left, Vector<long> right);

    /// svuint8_t svhsub[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2) : "UHSUB Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; UHSUB Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svuint8_t svhsub[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2) : "UHSUB Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "UHSUBR Ztied2.B, Pg/M, Ztied2.B, Zop1.B" or "MOVPRFX Zresult, Zop1; UHSUB Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svuint8_t svhsub[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; UHSUB Zresult.B, Pg/M, Zresult.B, Zop2.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; UHSUBR Zresult.B, Pg/M, Zresult.B, Zop1.B"
  public static unsafe Vector<byte> HalvingSubtract(Vector<byte> left, Vector<byte> right);

    /// svuint16_t svhsub[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2) : "UHSUB Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; UHSUB Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svuint16_t svhsub[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2) : "UHSUB Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "UHSUBR Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "MOVPRFX Zresult, Zop1; UHSUB Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svuint16_t svhsub[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; UHSUB Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; UHSUBR Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<ushort> HalvingSubtract(Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svhsub[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2) : "UHSUB Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; UHSUB Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svuint32_t svhsub[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2) : "UHSUB Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "UHSUBR Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "MOVPRFX Zresult, Zop1; UHSUB Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svuint32_t svhsub[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; UHSUB Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; UHSUBR Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<uint> HalvingSubtract(Vector<uint> left, Vector<uint> right);

    /// svuint64_t svhsub[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2) : "UHSUB Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; UHSUB Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svuint64_t svhsub[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2) : "UHSUB Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "UHSUBR Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "MOVPRFX Zresult, Zop1; UHSUB Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svuint64_t svhsub[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; UHSUB Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; UHSUBR Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<ulong> HalvingSubtract(Vector<ulong> left, Vector<ulong> right);


    /// HalvingSubtractReversed : Halving subtract reversed

    /// svint8_t svhsubr[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2) : "SHSUBR Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; SHSUBR Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svint8_t svhsubr[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2) : "SHSUBR Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "SHSUB Ztied2.B, Pg/M, Ztied2.B, Zop1.B" or "MOVPRFX Zresult, Zop1; SHSUBR Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svint8_t svhsubr[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; SHSUBR Zresult.B, Pg/M, Zresult.B, Zop2.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; SHSUB Zresult.B, Pg/M, Zresult.B, Zop1.B"
  public static unsafe Vector<sbyte> HalvingSubtractReversed(Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t svhsubr[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2) : "SHSUBR Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; SHSUBR Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svint16_t svhsubr[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2) : "SHSUBR Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "SHSUB Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "MOVPRFX Zresult, Zop1; SHSUBR Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svint16_t svhsubr[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; SHSUBR Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; SHSUB Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<short> HalvingSubtractReversed(Vector<short> left, Vector<short> right);

    /// svint32_t svhsubr[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2) : "SHSUBR Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; SHSUBR Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svint32_t svhsubr[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2) : "SHSUBR Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "SHSUB Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "MOVPRFX Zresult, Zop1; SHSUBR Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svint32_t svhsubr[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; SHSUBR Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; SHSUB Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<int> HalvingSubtractReversed(Vector<int> left, Vector<int> right);

    /// svint64_t svhsubr[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2) : "SHSUBR Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; SHSUBR Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svint64_t svhsubr[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2) : "SHSUBR Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "SHSUB Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "MOVPRFX Zresult, Zop1; SHSUBR Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svint64_t svhsubr[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; SHSUBR Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; SHSUB Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<long> HalvingSubtractReversed(Vector<long> left, Vector<long> right);

    /// svuint8_t svhsubr[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2) : "UHSUBR Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; UHSUBR Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svuint8_t svhsubr[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2) : "UHSUBR Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "UHSUB Ztied2.B, Pg/M, Ztied2.B, Zop1.B" or "MOVPRFX Zresult, Zop1; UHSUBR Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svuint8_t svhsubr[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; UHSUBR Zresult.B, Pg/M, Zresult.B, Zop2.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; UHSUB Zresult.B, Pg/M, Zresult.B, Zop1.B"
  public static unsafe Vector<byte> HalvingSubtractReversed(Vector<byte> left, Vector<byte> right);

    /// svuint16_t svhsubr[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2) : "UHSUBR Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; UHSUBR Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svuint16_t svhsubr[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2) : "UHSUBR Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "UHSUB Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "MOVPRFX Zresult, Zop1; UHSUBR Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svuint16_t svhsubr[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; UHSUBR Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; UHSUB Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<ushort> HalvingSubtractReversed(Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svhsubr[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2) : "UHSUBR Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; UHSUBR Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svuint32_t svhsubr[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2) : "UHSUBR Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "UHSUB Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "MOVPRFX Zresult, Zop1; UHSUBR Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svuint32_t svhsubr[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; UHSUBR Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; UHSUB Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<uint> HalvingSubtractReversed(Vector<uint> left, Vector<uint> right);

    /// svuint64_t svhsubr[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2) : "UHSUBR Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; UHSUBR Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svuint64_t svhsubr[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2) : "UHSUBR Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "UHSUB Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "MOVPRFX Zresult, Zop1; UHSUBR Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svuint64_t svhsubr[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; UHSUBR Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; UHSUB Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<ulong> HalvingSubtractReversed(Vector<ulong> left, Vector<ulong> right);


    /// MaxNumberPairwise : Maximum number pairwise

    /// svfloat32_t svmaxnmp[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "FMAXNMP Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; FMAXNMP Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svfloat32_t svmaxnmp[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "FMAXNMP Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; FMAXNMP Zresult.S, Pg/M, Zresult.S, Zop2.S"
  public static unsafe Vector<float> MaxNumberPairwise(Vector<float> left, Vector<float> right);

    /// svfloat64_t svmaxnmp[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "FMAXNMP Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; FMAXNMP Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svfloat64_t svmaxnmp[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "FMAXNMP Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; FMAXNMP Zresult.D, Pg/M, Zresult.D, Zop2.D"
  public static unsafe Vector<double> MaxNumberPairwise(Vector<double> left, Vector<double> right);


    /// MaxPairwise : Maximum pairwise

    /// svfloat32_t svmaxp[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "FMAXP Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; FMAXP Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svfloat32_t svmaxp[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "FMAXP Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; FMAXP Zresult.S, Pg/M, Zresult.S, Zop2.S"
  public static unsafe Vector<float> MaxPairwise(Vector<float> left, Vector<float> right);

    /// svfloat64_t svmaxp[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "FMAXP Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; FMAXP Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svfloat64_t svmaxp[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "FMAXP Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; FMAXP Zresult.D, Pg/M, Zresult.D, Zop2.D"
  public static unsafe Vector<double> MaxPairwise(Vector<double> left, Vector<double> right);

    /// svint8_t svmaxp[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2) : "SMAXP Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; SMAXP Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svint8_t svmaxp[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2) : "SMAXP Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; SMAXP Zresult.B, Pg/M, Zresult.B, Zop2.B"
  public static unsafe Vector<sbyte> MaxPairwise(Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t svmaxp[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2) : "SMAXP Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; SMAXP Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svint16_t svmaxp[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2) : "SMAXP Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; SMAXP Zresult.H, Pg/M, Zresult.H, Zop2.H"
  public static unsafe Vector<short> MaxPairwise(Vector<short> left, Vector<short> right);

    /// svint32_t svmaxp[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2) : "SMAXP Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; SMAXP Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svint32_t svmaxp[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2) : "SMAXP Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; SMAXP Zresult.S, Pg/M, Zresult.S, Zop2.S"
  public static unsafe Vector<int> MaxPairwise(Vector<int> left, Vector<int> right);

    /// svint64_t svmaxp[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2) : "SMAXP Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; SMAXP Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svint64_t svmaxp[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2) : "SMAXP Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; SMAXP Zresult.D, Pg/M, Zresult.D, Zop2.D"
  public static unsafe Vector<long> MaxPairwise(Vector<long> left, Vector<long> right);

    /// svuint8_t svmaxp[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2) : "UMAXP Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; UMAXP Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svuint8_t svmaxp[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2) : "UMAXP Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; UMAXP Zresult.B, Pg/M, Zresult.B, Zop2.B"
  public static unsafe Vector<byte> MaxPairwise(Vector<byte> left, Vector<byte> right);

    /// svuint16_t svmaxp[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2) : "UMAXP Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; UMAXP Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svuint16_t svmaxp[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2) : "UMAXP Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; UMAXP Zresult.H, Pg/M, Zresult.H, Zop2.H"
  public static unsafe Vector<ushort> MaxPairwise(Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svmaxp[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2) : "UMAXP Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; UMAXP Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svuint32_t svmaxp[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2) : "UMAXP Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; UMAXP Zresult.S, Pg/M, Zresult.S, Zop2.S"
  public static unsafe Vector<uint> MaxPairwise(Vector<uint> left, Vector<uint> right);

    /// svuint64_t svmaxp[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2) : "UMAXP Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; UMAXP Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svuint64_t svmaxp[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2) : "UMAXP Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; UMAXP Zresult.D, Pg/M, Zresult.D, Zop2.D"
  public static unsafe Vector<ulong> MaxPairwise(Vector<ulong> left, Vector<ulong> right);


    /// MinNumberPairwise : Minimum number pairwise

    /// svfloat32_t svminnmp[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "FMINNMP Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; FMINNMP Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svfloat32_t svminnmp[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "FMINNMP Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; FMINNMP Zresult.S, Pg/M, Zresult.S, Zop2.S"
  public static unsafe Vector<float> MinNumberPairwise(Vector<float> left, Vector<float> right);

    /// svfloat64_t svminnmp[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "FMINNMP Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; FMINNMP Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svfloat64_t svminnmp[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "FMINNMP Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; FMINNMP Zresult.D, Pg/M, Zresult.D, Zop2.D"
  public static unsafe Vector<double> MinNumberPairwise(Vector<double> left, Vector<double> right);


    /// MinPairwise : Minimum pairwise

    /// svfloat32_t svminp[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "FMINP Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; FMINP Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svfloat32_t svminp[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2) : "FMINP Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; FMINP Zresult.S, Pg/M, Zresult.S, Zop2.S"
  public static unsafe Vector<float> MinPairwise(Vector<float> left, Vector<float> right);

    /// svfloat64_t svminp[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "FMINP Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; FMINP Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svfloat64_t svminp[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2) : "FMINP Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; FMINP Zresult.D, Pg/M, Zresult.D, Zop2.D"
  public static unsafe Vector<double> MinPairwise(Vector<double> left, Vector<double> right);

    /// svint8_t svminp[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2) : "SMINP Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; SMINP Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svint8_t svminp[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2) : "SMINP Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; SMINP Zresult.B, Pg/M, Zresult.B, Zop2.B"
  public static unsafe Vector<sbyte> MinPairwise(Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t svminp[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2) : "SMINP Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; SMINP Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svint16_t svminp[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2) : "SMINP Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; SMINP Zresult.H, Pg/M, Zresult.H, Zop2.H"
  public static unsafe Vector<short> MinPairwise(Vector<short> left, Vector<short> right);

    /// svint32_t svminp[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2) : "SMINP Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; SMINP Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svint32_t svminp[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2) : "SMINP Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; SMINP Zresult.S, Pg/M, Zresult.S, Zop2.S"
  public static unsafe Vector<int> MinPairwise(Vector<int> left, Vector<int> right);

    /// svint64_t svminp[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2) : "SMINP Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; SMINP Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svint64_t svminp[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2) : "SMINP Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; SMINP Zresult.D, Pg/M, Zresult.D, Zop2.D"
  public static unsafe Vector<long> MinPairwise(Vector<long> left, Vector<long> right);

    /// svuint8_t svminp[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2) : "UMINP Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; UMINP Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svuint8_t svminp[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2) : "UMINP Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; UMINP Zresult.B, Pg/M, Zresult.B, Zop2.B"
  public static unsafe Vector<byte> MinPairwise(Vector<byte> left, Vector<byte> right);

    /// svuint16_t svminp[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2) : "UMINP Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; UMINP Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svuint16_t svminp[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2) : "UMINP Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; UMINP Zresult.H, Pg/M, Zresult.H, Zop2.H"
  public static unsafe Vector<ushort> MinPairwise(Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svminp[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2) : "UMINP Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; UMINP Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svuint32_t svminp[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2) : "UMINP Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; UMINP Zresult.S, Pg/M, Zresult.S, Zop2.S"
  public static unsafe Vector<uint> MinPairwise(Vector<uint> left, Vector<uint> right);

    /// svuint64_t svminp[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2) : "UMINP Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; UMINP Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svuint64_t svminp[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2) : "UMINP Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; UMINP Zresult.D, Pg/M, Zresult.D, Zop2.D"
  public static unsafe Vector<ulong> MinPairwise(Vector<ulong> left, Vector<ulong> right);


    /// MultiplyAddBySelectedScalar : Multiply-add, addend first

    /// svint16_t svmla_lane[_s16](svint16_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index) : "MLA Ztied1.H, Zop2.H, Zop3.H[imm_index]" or "MOVPRFX Zresult, Zop1; MLA Zresult.H, Zop2.H, Zop3.H[imm_index]"
  public static unsafe Vector<short> MultiplyAddBySelectedScalar(Vector<short> addend, Vector<short> left, Vector<short> right, [ConstantExpected] byte rightIndex);

    /// svint32_t svmla_lane[_s32](svint32_t op1, svint32_t op2, svint32_t op3, uint64_t imm_index) : "MLA Ztied1.S, Zop2.S, Zop3.S[imm_index]" or "MOVPRFX Zresult, Zop1; MLA Zresult.S, Zop2.S, Zop3.S[imm_index]"
  public static unsafe Vector<int> MultiplyAddBySelectedScalar(Vector<int> addend, Vector<int> left, Vector<int> right, [ConstantExpected] byte rightIndex);

    /// svint64_t svmla_lane[_s64](svint64_t op1, svint64_t op2, svint64_t op3, uint64_t imm_index) : "MLA Ztied1.D, Zop2.D, Zop3.D[imm_index]" or "MOVPRFX Zresult, Zop1; MLA Zresult.D, Zop2.D, Zop3.D[imm_index]"
  public static unsafe Vector<long> MultiplyAddBySelectedScalar(Vector<long> addend, Vector<long> left, Vector<long> right, [ConstantExpected] byte rightIndex);

    /// svuint16_t svmla_lane[_u16](svuint16_t op1, svuint16_t op2, svuint16_t op3, uint64_t imm_index) : "MLA Ztied1.H, Zop2.H, Zop3.H[imm_index]" or "MOVPRFX Zresult, Zop1; MLA Zresult.H, Zop2.H, Zop3.H[imm_index]"
  public static unsafe Vector<ushort> MultiplyAddBySelectedScalar(Vector<ushort> addend, Vector<ushort> left, Vector<ushort> right, [ConstantExpected] byte rightIndex);

    /// svuint32_t svmla_lane[_u32](svuint32_t op1, svuint32_t op2, svuint32_t op3, uint64_t imm_index) : "MLA Ztied1.S, Zop2.S, Zop3.S[imm_index]" or "MOVPRFX Zresult, Zop1; MLA Zresult.S, Zop2.S, Zop3.S[imm_index]"
  public static unsafe Vector<uint> MultiplyAddBySelectedScalar(Vector<uint> addend, Vector<uint> left, Vector<uint> right, [ConstantExpected] byte rightIndex);

    /// svuint64_t svmla_lane[_u64](svuint64_t op1, svuint64_t op2, svuint64_t op3, uint64_t imm_index) : "MLA Ztied1.D, Zop2.D, Zop3.D[imm_index]" or "MOVPRFX Zresult, Zop1; MLA Zresult.D, Zop2.D, Zop3.D[imm_index]"
  public static unsafe Vector<ulong> MultiplyAddBySelectedScalar(Vector<ulong> addend, Vector<ulong> left, Vector<ulong> right, [ConstantExpected] byte rightIndex);


    /// MultiplyAddWideningLower : Multiply-add long (bottom)

    /// svint16_t svmlalb[_s16](svint16_t op1, svint8_t op2, svint8_t op3) : "SMLALB Ztied1.H, Zop2.B, Zop3.B" or "MOVPRFX Zresult, Zop1; SMLALB Zresult.H, Zop2.B, Zop3.B"
  public static unsafe Vector<short> MultiplyAddWideningLower(Vector<short> op1, Vector<sbyte> op2, Vector<sbyte> op3);

    /// svint32_t svmlalb[_s32](svint32_t op1, svint16_t op2, svint16_t op3) : "SMLALB Ztied1.S, Zop2.H, Zop3.H" or "MOVPRFX Zresult, Zop1; SMLALB Zresult.S, Zop2.H, Zop3.H"
  public static unsafe Vector<int> MultiplyAddWideningLower(Vector<int> op1, Vector<short> op2, Vector<short> op3);

    /// svint64_t svmlalb[_s64](svint64_t op1, svint32_t op2, svint32_t op3) : "SMLALB Ztied1.D, Zop2.S, Zop3.S" or "MOVPRFX Zresult, Zop1; SMLALB Zresult.D, Zop2.S, Zop3.S"
  public static unsafe Vector<long> MultiplyAddWideningLower(Vector<long> op1, Vector<int> op2, Vector<int> op3);

    /// svuint16_t svmlalb[_u16](svuint16_t op1, svuint8_t op2, svuint8_t op3) : "UMLALB Ztied1.H, Zop2.B, Zop3.B" or "MOVPRFX Zresult, Zop1; UMLALB Zresult.H, Zop2.B, Zop3.B"
  public static unsafe Vector<ushort> MultiplyAddWideningLower(Vector<ushort> op1, Vector<byte> op2, Vector<byte> op3);

    /// svuint32_t svmlalb[_u32](svuint32_t op1, svuint16_t op2, svuint16_t op3) : "UMLALB Ztied1.S, Zop2.H, Zop3.H" or "MOVPRFX Zresult, Zop1; UMLALB Zresult.S, Zop2.H, Zop3.H"
  public static unsafe Vector<uint> MultiplyAddWideningLower(Vector<uint> op1, Vector<ushort> op2, Vector<ushort> op3);

    /// svuint64_t svmlalb[_u64](svuint64_t op1, svuint32_t op2, svuint32_t op3) : "UMLALB Ztied1.D, Zop2.S, Zop3.S" or "MOVPRFX Zresult, Zop1; UMLALB Zresult.D, Zop2.S, Zop3.S"
  public static unsafe Vector<ulong> MultiplyAddWideningLower(Vector<ulong> op1, Vector<uint> op2, Vector<uint> op3);

    /// svint32_t svmlalb_lane[_s32](svint32_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index) : "SMLALB Ztied1.S, Zop2.H, Zop3.H[imm_index]" or "MOVPRFX Zresult, Zop1; SMLALB Zresult.S, Zop2.H, Zop3.H[imm_index]"
  public static unsafe Vector<int> MultiplyAddWideningLower(Vector<int> op1, Vector<short> op2, Vector<short> op3, ulong imm_index);

    /// svint64_t svmlalb_lane[_s64](svint64_t op1, svint32_t op2, svint32_t op3, uint64_t imm_index) : "SMLALB Ztied1.D, Zop2.S, Zop3.S[imm_index]" or "MOVPRFX Zresult, Zop1; SMLALB Zresult.D, Zop2.S, Zop3.S[imm_index]"
  public static unsafe Vector<long> MultiplyAddWideningLower(Vector<long> op1, Vector<int> op2, Vector<int> op3, ulong imm_index);

    /// svuint32_t svmlalb_lane[_u32](svuint32_t op1, svuint16_t op2, svuint16_t op3, uint64_t imm_index) : "UMLALB Ztied1.S, Zop2.H, Zop3.H[imm_index]" or "MOVPRFX Zresult, Zop1; UMLALB Zresult.S, Zop2.H, Zop3.H[imm_index]"
  public static unsafe Vector<uint> MultiplyAddWideningLower(Vector<uint> op1, Vector<ushort> op2, Vector<ushort> op3, ulong imm_index);

    /// svuint64_t svmlalb_lane[_u64](svuint64_t op1, svuint32_t op2, svuint32_t op3, uint64_t imm_index) : "UMLALB Ztied1.D, Zop2.S, Zop3.S[imm_index]" or "MOVPRFX Zresult, Zop1; UMLALB Zresult.D, Zop2.S, Zop3.S[imm_index]"
  public static unsafe Vector<ulong> MultiplyAddWideningLower(Vector<ulong> op1, Vector<uint> op2, Vector<uint> op3, ulong imm_index);


    /// MultiplyAddWideningUpper : Multiply-add long (top)

    /// svint16_t svmlalt[_s16](svint16_t op1, svint8_t op2, svint8_t op3) : "SMLALT Ztied1.H, Zop2.B, Zop3.B" or "MOVPRFX Zresult, Zop1; SMLALT Zresult.H, Zop2.B, Zop3.B"
  public static unsafe Vector<short> MultiplyAddWideningUpper(Vector<short> op1, Vector<sbyte> op2, Vector<sbyte> op3);

    /// svint32_t svmlalt[_s32](svint32_t op1, svint16_t op2, svint16_t op3) : "SMLALT Ztied1.S, Zop2.H, Zop3.H" or "MOVPRFX Zresult, Zop1; SMLALT Zresult.S, Zop2.H, Zop3.H"
  public static unsafe Vector<int> MultiplyAddWideningUpper(Vector<int> op1, Vector<short> op2, Vector<short> op3);

    /// svint64_t svmlalt[_s64](svint64_t op1, svint32_t op2, svint32_t op3) : "SMLALT Ztied1.D, Zop2.S, Zop3.S" or "MOVPRFX Zresult, Zop1; SMLALT Zresult.D, Zop2.S, Zop3.S"
  public static unsafe Vector<long> MultiplyAddWideningUpper(Vector<long> op1, Vector<int> op2, Vector<int> op3);

    /// svuint16_t svmlalt[_u16](svuint16_t op1, svuint8_t op2, svuint8_t op3) : "UMLALT Ztied1.H, Zop2.B, Zop3.B" or "MOVPRFX Zresult, Zop1; UMLALT Zresult.H, Zop2.B, Zop3.B"
  public static unsafe Vector<ushort> MultiplyAddWideningUpper(Vector<ushort> op1, Vector<byte> op2, Vector<byte> op3);

    /// svuint32_t svmlalt[_u32](svuint32_t op1, svuint16_t op2, svuint16_t op3) : "UMLALT Ztied1.S, Zop2.H, Zop3.H" or "MOVPRFX Zresult, Zop1; UMLALT Zresult.S, Zop2.H, Zop3.H"
  public static unsafe Vector<uint> MultiplyAddWideningUpper(Vector<uint> op1, Vector<ushort> op2, Vector<ushort> op3);

    /// svuint64_t svmlalt[_u64](svuint64_t op1, svuint32_t op2, svuint32_t op3) : "UMLALT Ztied1.D, Zop2.S, Zop3.S" or "MOVPRFX Zresult, Zop1; UMLALT Zresult.D, Zop2.S, Zop3.S"
  public static unsafe Vector<ulong> MultiplyAddWideningUpper(Vector<ulong> op1, Vector<uint> op2, Vector<uint> op3);

    /// svint32_t svmlalt_lane[_s32](svint32_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index) : "SMLALT Ztied1.S, Zop2.H, Zop3.H[imm_index]" or "MOVPRFX Zresult, Zop1; SMLALT Zresult.S, Zop2.H, Zop3.H[imm_index]"
  public static unsafe Vector<int> MultiplyAddWideningUpper(Vector<int> op1, Vector<short> op2, Vector<short> op3, ulong imm_index);

    /// svint64_t svmlalt_lane[_s64](svint64_t op1, svint32_t op2, svint32_t op3, uint64_t imm_index) : "SMLALT Ztied1.D, Zop2.S, Zop3.S[imm_index]" or "MOVPRFX Zresult, Zop1; SMLALT Zresult.D, Zop2.S, Zop3.S[imm_index]"
  public static unsafe Vector<long> MultiplyAddWideningUpper(Vector<long> op1, Vector<int> op2, Vector<int> op3, ulong imm_index);

    /// svuint32_t svmlalt_lane[_u32](svuint32_t op1, svuint16_t op2, svuint16_t op3, uint64_t imm_index) : "UMLALT Ztied1.S, Zop2.H, Zop3.H[imm_index]" or "MOVPRFX Zresult, Zop1; UMLALT Zresult.S, Zop2.H, Zop3.H[imm_index]"
  public static unsafe Vector<uint> MultiplyAddWideningUpper(Vector<uint> op1, Vector<ushort> op2, Vector<ushort> op3, ulong imm_index);

    /// svuint64_t svmlalt_lane[_u64](svuint64_t op1, svuint32_t op2, svuint32_t op3, uint64_t imm_index) : "UMLALT Ztied1.D, Zop2.S, Zop3.S[imm_index]" or "MOVPRFX Zresult, Zop1; UMLALT Zresult.D, Zop2.S, Zop3.S[imm_index]"
  public static unsafe Vector<ulong> MultiplyAddWideningUpper(Vector<ulong> op1, Vector<uint> op2, Vector<uint> op3, ulong imm_index);


    /// MultiplyBySelectedScalar : Multiply

    /// svint16_t svmul_lane[_s16](svint16_t op1, svint16_t op2, uint64_t imm_index) : "MUL Zresult.H, Zop1.H, Zop2.H[imm_index]"
  public static unsafe Vector<short> MultiplyBySelectedScalar(Vector<short> left, Vector<short> right, [ConstantExpected] byte rightIndex);

    /// svint32_t svmul_lane[_s32](svint32_t op1, svint32_t op2, uint64_t imm_index) : "MUL Zresult.S, Zop1.S, Zop2.S[imm_index]"
  public static unsafe Vector<int> MultiplyBySelectedScalar(Vector<int> left, Vector<int> right, [ConstantExpected] byte rightIndex);

    /// svint64_t svmul_lane[_s64](svint64_t op1, svint64_t op2, uint64_t imm_index) : "MUL Zresult.D, Zop1.D, Zop2.D[imm_index]"
  public static unsafe Vector<long> MultiplyBySelectedScalar(Vector<long> left, Vector<long> right, [ConstantExpected] byte rightIndex);

    /// svuint16_t svmul_lane[_u16](svuint16_t op1, svuint16_t op2, uint64_t imm_index) : "MUL Zresult.H, Zop1.H, Zop2.H[imm_index]"
  public static unsafe Vector<ushort> MultiplyBySelectedScalar(Vector<ushort> left, Vector<ushort> right, [ConstantExpected] byte rightIndex);

    /// svuint32_t svmul_lane[_u32](svuint32_t op1, svuint32_t op2, uint64_t imm_index) : "MUL Zresult.S, Zop1.S, Zop2.S[imm_index]"
  public static unsafe Vector<uint> MultiplyBySelectedScalar(Vector<uint> left, Vector<uint> right, [ConstantExpected] byte rightIndex);

    /// svuint64_t svmul_lane[_u64](svuint64_t op1, svuint64_t op2, uint64_t imm_index) : "MUL Zresult.D, Zop1.D, Zop2.D[imm_index]"
  public static unsafe Vector<ulong> MultiplyBySelectedScalar(Vector<ulong> left, Vector<ulong> right, [ConstantExpected] byte rightIndex);


    /// MultiplySubtractBySelectedScalar : Multiply-subtract, minuend first

    /// svint16_t svmls_lane[_s16](svint16_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index) : "MLS Ztied1.H, Zop2.H, Zop3.H[imm_index]" or "MOVPRFX Zresult, Zop1; MLS Zresult.H, Zop2.H, Zop3.H[imm_index]"
  public static unsafe Vector<short> MultiplySubtractBySelectedScalar(Vector<short> minuend, Vector<short> left, Vector<short> right, [ConstantExpected] byte rightIndex);

    /// svint32_t svmls_lane[_s32](svint32_t op1, svint32_t op2, svint32_t op3, uint64_t imm_index) : "MLS Ztied1.S, Zop2.S, Zop3.S[imm_index]" or "MOVPRFX Zresult, Zop1; MLS Zresult.S, Zop2.S, Zop3.S[imm_index]"
  public static unsafe Vector<int> MultiplySubtractBySelectedScalar(Vector<int> minuend, Vector<int> left, Vector<int> right, [ConstantExpected] byte rightIndex);

    /// svint64_t svmls_lane[_s64](svint64_t op1, svint64_t op2, svint64_t op3, uint64_t imm_index) : "MLS Ztied1.D, Zop2.D, Zop3.D[imm_index]" or "MOVPRFX Zresult, Zop1; MLS Zresult.D, Zop2.D, Zop3.D[imm_index]"
  public static unsafe Vector<long> MultiplySubtractBySelectedScalar(Vector<long> minuend, Vector<long> left, Vector<long> right, [ConstantExpected] byte rightIndex);

    /// svuint16_t svmls_lane[_u16](svuint16_t op1, svuint16_t op2, svuint16_t op3, uint64_t imm_index) : "MLS Ztied1.H, Zop2.H, Zop3.H[imm_index]" or "MOVPRFX Zresult, Zop1; MLS Zresult.H, Zop2.H, Zop3.H[imm_index]"
  public static unsafe Vector<ushort> MultiplySubtractBySelectedScalar(Vector<ushort> minuend, Vector<ushort> left, Vector<ushort> right, [ConstantExpected] byte rightIndex);

    /// svuint32_t svmls_lane[_u32](svuint32_t op1, svuint32_t op2, svuint32_t op3, uint64_t imm_index) : "MLS Ztied1.S, Zop2.S, Zop3.S[imm_index]" or "MOVPRFX Zresult, Zop1; MLS Zresult.S, Zop2.S, Zop3.S[imm_index]"
  public static unsafe Vector<uint> MultiplySubtractBySelectedScalar(Vector<uint> minuend, Vector<uint> left, Vector<uint> right, [ConstantExpected] byte rightIndex);

    /// svuint64_t svmls_lane[_u64](svuint64_t op1, svuint64_t op2, svuint64_t op3, uint64_t imm_index) : "MLS Ztied1.D, Zop2.D, Zop3.D[imm_index]" or "MOVPRFX Zresult, Zop1; MLS Zresult.D, Zop2.D, Zop3.D[imm_index]"
  public static unsafe Vector<ulong> MultiplySubtractBySelectedScalar(Vector<ulong> minuend, Vector<ulong> left, Vector<ulong> right, [ConstantExpected] byte rightIndex);


    /// MultiplySubtractWideningLower : Multiply-subtract long (bottom)

    /// svint16_t svmlslb[_s16](svint16_t op1, svint8_t op2, svint8_t op3) : "SMLSLB Ztied1.H, Zop2.B, Zop3.B" or "MOVPRFX Zresult, Zop1; SMLSLB Zresult.H, Zop2.B, Zop3.B"
  public static unsafe Vector<short> MultiplySubtractWideningLower(Vector<short> op1, Vector<sbyte> op2, Vector<sbyte> op3);

    /// svint32_t svmlslb[_s32](svint32_t op1, svint16_t op2, svint16_t op3) : "SMLSLB Ztied1.S, Zop2.H, Zop3.H" or "MOVPRFX Zresult, Zop1; SMLSLB Zresult.S, Zop2.H, Zop3.H"
  public static unsafe Vector<int> MultiplySubtractWideningLower(Vector<int> op1, Vector<short> op2, Vector<short> op3);

    /// svint64_t svmlslb[_s64](svint64_t op1, svint32_t op2, svint32_t op3) : "SMLSLB Ztied1.D, Zop2.S, Zop3.S" or "MOVPRFX Zresult, Zop1; SMLSLB Zresult.D, Zop2.S, Zop3.S"
  public static unsafe Vector<long> MultiplySubtractWideningLower(Vector<long> op1, Vector<int> op2, Vector<int> op3);

    /// svuint16_t svmlslb[_u16](svuint16_t op1, svuint8_t op2, svuint8_t op3) : "UMLSLB Ztied1.H, Zop2.B, Zop3.B" or "MOVPRFX Zresult, Zop1; UMLSLB Zresult.H, Zop2.B, Zop3.B"
  public static unsafe Vector<ushort> MultiplySubtractWideningLower(Vector<ushort> op1, Vector<byte> op2, Vector<byte> op3);

    /// svuint32_t svmlslb[_u32](svuint32_t op1, svuint16_t op2, svuint16_t op3) : "UMLSLB Ztied1.S, Zop2.H, Zop3.H" or "MOVPRFX Zresult, Zop1; UMLSLB Zresult.S, Zop2.H, Zop3.H"
  public static unsafe Vector<uint> MultiplySubtractWideningLower(Vector<uint> op1, Vector<ushort> op2, Vector<ushort> op3);

    /// svuint64_t svmlslb[_u64](svuint64_t op1, svuint32_t op2, svuint32_t op3) : "UMLSLB Ztied1.D, Zop2.S, Zop3.S" or "MOVPRFX Zresult, Zop1; UMLSLB Zresult.D, Zop2.S, Zop3.S"
  public static unsafe Vector<ulong> MultiplySubtractWideningLower(Vector<ulong> op1, Vector<uint> op2, Vector<uint> op3);

    /// svint32_t svmlslb_lane[_s32](svint32_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index) : "SMLSLB Ztied1.S, Zop2.H, Zop3.H[imm_index]" or "MOVPRFX Zresult, Zop1; SMLSLB Zresult.S, Zop2.H, Zop3.H[imm_index]"
  public static unsafe Vector<int> MultiplySubtractWideningLower(Vector<int> op1, Vector<short> op2, Vector<short> op3, ulong imm_index);

    /// svint64_t svmlslb_lane[_s64](svint64_t op1, svint32_t op2, svint32_t op3, uint64_t imm_index) : "SMLSLB Ztied1.D, Zop2.S, Zop3.S[imm_index]" or "MOVPRFX Zresult, Zop1; SMLSLB Zresult.D, Zop2.S, Zop3.S[imm_index]"
  public static unsafe Vector<long> MultiplySubtractWideningLower(Vector<long> op1, Vector<int> op2, Vector<int> op3, ulong imm_index);

    /// svuint32_t svmlslb_lane[_u32](svuint32_t op1, svuint16_t op2, svuint16_t op3, uint64_t imm_index) : "UMLSLB Ztied1.S, Zop2.H, Zop3.H[imm_index]" or "MOVPRFX Zresult, Zop1; UMLSLB Zresult.S, Zop2.H, Zop3.H[imm_index]"
  public static unsafe Vector<uint> MultiplySubtractWideningLower(Vector<uint> op1, Vector<ushort> op2, Vector<ushort> op3, ulong imm_index);

    /// svuint64_t svmlslb_lane[_u64](svuint64_t op1, svuint32_t op2, svuint32_t op3, uint64_t imm_index) : "UMLSLB Ztied1.D, Zop2.S, Zop3.S[imm_index]" or "MOVPRFX Zresult, Zop1; UMLSLB Zresult.D, Zop2.S, Zop3.S[imm_index]"
  public static unsafe Vector<ulong> MultiplySubtractWideningLower(Vector<ulong> op1, Vector<uint> op2, Vector<uint> op3, ulong imm_index);


    /// MultiplySubtractWideningUpper : Multiply-subtract long (top)

    /// svint16_t svmlslt[_s16](svint16_t op1, svint8_t op2, svint8_t op3) : "SMLSLT Ztied1.H, Zop2.B, Zop3.B" or "MOVPRFX Zresult, Zop1; SMLSLT Zresult.H, Zop2.B, Zop3.B"
  public static unsafe Vector<short> MultiplySubtractWideningUpper(Vector<short> op1, Vector<sbyte> op2, Vector<sbyte> op3);

    /// svint32_t svmlslt[_s32](svint32_t op1, svint16_t op2, svint16_t op3) : "SMLSLT Ztied1.S, Zop2.H, Zop3.H" or "MOVPRFX Zresult, Zop1; SMLSLT Zresult.S, Zop2.H, Zop3.H"
  public static unsafe Vector<int> MultiplySubtractWideningUpper(Vector<int> op1, Vector<short> op2, Vector<short> op3);

    /// svint64_t svmlslt[_s64](svint64_t op1, svint32_t op2, svint32_t op3) : "SMLSLT Ztied1.D, Zop2.S, Zop3.S" or "MOVPRFX Zresult, Zop1; SMLSLT Zresult.D, Zop2.S, Zop3.S"
  public static unsafe Vector<long> MultiplySubtractWideningUpper(Vector<long> op1, Vector<int> op2, Vector<int> op3);

    /// svuint16_t svmlslt[_u16](svuint16_t op1, svuint8_t op2, svuint8_t op3) : "UMLSLT Ztied1.H, Zop2.B, Zop3.B" or "MOVPRFX Zresult, Zop1; UMLSLT Zresult.H, Zop2.B, Zop3.B"
  public static unsafe Vector<ushort> MultiplySubtractWideningUpper(Vector<ushort> op1, Vector<byte> op2, Vector<byte> op3);

    /// svuint32_t svmlslt[_u32](svuint32_t op1, svuint16_t op2, svuint16_t op3) : "UMLSLT Ztied1.S, Zop2.H, Zop3.H" or "MOVPRFX Zresult, Zop1; UMLSLT Zresult.S, Zop2.H, Zop3.H"
  public static unsafe Vector<uint> MultiplySubtractWideningUpper(Vector<uint> op1, Vector<ushort> op2, Vector<ushort> op3);

    /// svuint64_t svmlslt[_u64](svuint64_t op1, svuint32_t op2, svuint32_t op3) : "UMLSLT Ztied1.D, Zop2.S, Zop3.S" or "MOVPRFX Zresult, Zop1; UMLSLT Zresult.D, Zop2.S, Zop3.S"
  public static unsafe Vector<ulong> MultiplySubtractWideningUpper(Vector<ulong> op1, Vector<uint> op2, Vector<uint> op3);

    /// svint32_t svmlslt_lane[_s32](svint32_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index) : "SMLSLT Ztied1.S, Zop2.H, Zop3.H[imm_index]" or "MOVPRFX Zresult, Zop1; SMLSLT Zresult.S, Zop2.H, Zop3.H[imm_index]"
  public static unsafe Vector<int> MultiplySubtractWideningUpper(Vector<int> op1, Vector<short> op2, Vector<short> op3, ulong imm_index);

    /// svint64_t svmlslt_lane[_s64](svint64_t op1, svint32_t op2, svint32_t op3, uint64_t imm_index) : "SMLSLT Ztied1.D, Zop2.S, Zop3.S[imm_index]" or "MOVPRFX Zresult, Zop1; SMLSLT Zresult.D, Zop2.S, Zop3.S[imm_index]"
  public static unsafe Vector<long> MultiplySubtractWideningUpper(Vector<long> op1, Vector<int> op2, Vector<int> op3, ulong imm_index);

    /// svuint32_t svmlslt_lane[_u32](svuint32_t op1, svuint16_t op2, svuint16_t op3, uint64_t imm_index) : "UMLSLT Ztied1.S, Zop2.H, Zop3.H[imm_index]" or "MOVPRFX Zresult, Zop1; UMLSLT Zresult.S, Zop2.H, Zop3.H[imm_index]"
  public static unsafe Vector<uint> MultiplySubtractWideningUpper(Vector<uint> op1, Vector<ushort> op2, Vector<ushort> op3, ulong imm_index);

    /// svuint64_t svmlslt_lane[_u64](svuint64_t op1, svuint32_t op2, svuint32_t op3, uint64_t imm_index) : "UMLSLT Ztied1.D, Zop2.S, Zop3.S[imm_index]" or "MOVPRFX Zresult, Zop1; UMLSLT Zresult.D, Zop2.S, Zop3.S[imm_index]"
  public static unsafe Vector<ulong> MultiplySubtractWideningUpper(Vector<ulong> op1, Vector<uint> op2, Vector<uint> op3, ulong imm_index);


    /// MultiplyWideningLower : Multiply long (bottom)

    /// svint16_t svmullb[_s16](svint8_t op1, svint8_t op2) : "SMULLB Zresult.H, Zop1.B, Zop2.B"
  public static unsafe Vector<short> MultiplyWideningLower(Vector<sbyte> left, Vector<sbyte> right);

    /// svint32_t svmullb[_s32](svint16_t op1, svint16_t op2) : "SMULLB Zresult.S, Zop1.H, Zop2.H"
  public static unsafe Vector<int> MultiplyWideningLower(Vector<short> left, Vector<short> right);

    /// svint64_t svmullb[_s64](svint32_t op1, svint32_t op2) : "SMULLB Zresult.D, Zop1.S, Zop2.S"
  public static unsafe Vector<long> MultiplyWideningLower(Vector<int> left, Vector<int> right);

    /// svuint16_t svmullb[_u16](svuint8_t op1, svuint8_t op2) : "UMULLB Zresult.H, Zop1.B, Zop2.B"
  public static unsafe Vector<ushort> MultiplyWideningLower(Vector<byte> left, Vector<byte> right);

    /// svuint32_t svmullb[_u32](svuint16_t op1, svuint16_t op2) : "UMULLB Zresult.S, Zop1.H, Zop2.H"
  public static unsafe Vector<uint> MultiplyWideningLower(Vector<ushort> left, Vector<ushort> right);

    /// svuint64_t svmullb[_u64](svuint32_t op1, svuint32_t op2) : "UMULLB Zresult.D, Zop1.S, Zop2.S"
  public static unsafe Vector<ulong> MultiplyWideningLower(Vector<uint> left, Vector<uint> right);

    /// svint32_t svmullb_lane[_s32](svint16_t op1, svint16_t op2, uint64_t imm_index) : "SMULLB Zresult.S, Zop1.H, Zop2.H[imm_index]"
  public static unsafe Vector<int> MultiplyWideningLower(Vector<short> op1, Vector<short> op2, ulong imm_index);

    /// svint64_t svmullb_lane[_s64](svint32_t op1, svint32_t op2, uint64_t imm_index) : "SMULLB Zresult.D, Zop1.S, Zop2.S[imm_index]"
  public static unsafe Vector<long> MultiplyWideningLower(Vector<int> op1, Vector<int> op2, ulong imm_index);

    /// svuint32_t svmullb_lane[_u32](svuint16_t op1, svuint16_t op2, uint64_t imm_index) : "UMULLB Zresult.S, Zop1.H, Zop2.H[imm_index]"
  public static unsafe Vector<uint> MultiplyWideningLower(Vector<ushort> op1, Vector<ushort> op2, ulong imm_index);

    /// svuint64_t svmullb_lane[_u64](svuint32_t op1, svuint32_t op2, uint64_t imm_index) : "UMULLB Zresult.D, Zop1.S, Zop2.S[imm_index]"
  public static unsafe Vector<ulong> MultiplyWideningLower(Vector<uint> op1, Vector<uint> op2, ulong imm_index);


    /// MultiplyWideningUpper : Multiply long (top)

    /// svint16_t svmullt[_s16](svint8_t op1, svint8_t op2) : "SMULLT Zresult.H, Zop1.B, Zop2.B"
  public static unsafe Vector<short> MultiplyWideningUpper(Vector<sbyte> left, Vector<sbyte> right);

    /// svint32_t svmullt[_s32](svint16_t op1, svint16_t op2) : "SMULLT Zresult.S, Zop1.H, Zop2.H"
  public static unsafe Vector<int> MultiplyWideningUpper(Vector<short> left, Vector<short> right);

    /// svint64_t svmullt[_s64](svint32_t op1, svint32_t op2) : "SMULLT Zresult.D, Zop1.S, Zop2.S"
  public static unsafe Vector<long> MultiplyWideningUpper(Vector<int> left, Vector<int> right);

    /// svuint16_t svmullt[_u16](svuint8_t op1, svuint8_t op2) : "UMULLT Zresult.H, Zop1.B, Zop2.B"
  public static unsafe Vector<ushort> MultiplyWideningUpper(Vector<byte> left, Vector<byte> right);

    /// svuint32_t svmullt[_u32](svuint16_t op1, svuint16_t op2) : "UMULLT Zresult.S, Zop1.H, Zop2.H"
  public static unsafe Vector<uint> MultiplyWideningUpper(Vector<ushort> left, Vector<ushort> right);

    /// svuint64_t svmullt[_u64](svuint32_t op1, svuint32_t op2) : "UMULLT Zresult.D, Zop1.S, Zop2.S"
  public static unsafe Vector<ulong> MultiplyWideningUpper(Vector<uint> left, Vector<uint> right);

    /// svint32_t svmullt_lane[_s32](svint16_t op1, svint16_t op2, uint64_t imm_index) : "SMULLT Zresult.S, Zop1.H, Zop2.H[imm_index]"
  public static unsafe Vector<int> MultiplyWideningUpper(Vector<short> op1, Vector<short> op2, ulong imm_index);

    /// svint64_t svmullt_lane[_s64](svint32_t op1, svint32_t op2, uint64_t imm_index) : "SMULLT Zresult.D, Zop1.S, Zop2.S[imm_index]"
  public static unsafe Vector<long> MultiplyWideningUpper(Vector<int> op1, Vector<int> op2, ulong imm_index);

    /// svuint32_t svmullt_lane[_u32](svuint16_t op1, svuint16_t op2, uint64_t imm_index) : "UMULLT Zresult.S, Zop1.H, Zop2.H[imm_index]"
  public static unsafe Vector<uint> MultiplyWideningUpper(Vector<ushort> op1, Vector<ushort> op2, ulong imm_index);

    /// svuint64_t svmullt_lane[_u64](svuint32_t op1, svuint32_t op2, uint64_t imm_index) : "UMULLT Zresult.D, Zop1.S, Zop2.S[imm_index]"
  public static unsafe Vector<ulong> MultiplyWideningUpper(Vector<uint> op1, Vector<uint> op2, ulong imm_index);


    /// PolynomialMultiply : Polynomial multiply

    /// svuint8_t svpmul[_u8](svuint8_t op1, svuint8_t op2) : "PMUL Zresult.B, Zop1.B, Zop2.B"
  public static unsafe Vector<byte> PolynomialMultiply(Vector<byte> left, Vector<byte> right);


    /// PolynomialMultiplyWideningLower : Polynomial multiply long (bottom)

    /// svuint16_t svpmullb[_u16](svuint8_t op1, svuint8_t op2) : "PMULLB Zresult.H, Zop1.B, Zop2.B"
  public static unsafe Vector<ushort> PolynomialMultiplyWideningLower(Vector<byte> left, Vector<byte> right);

    /// svuint64_t svpmullb[_u64](svuint32_t op1, svuint32_t op2) : "PMULLB Zresult.D, Zop1.S, Zop2.S"
  public static unsafe Vector<ulong> PolynomialMultiplyWideningLower(Vector<uint> left, Vector<uint> right);

    /// svuint8_t svpmullb_pair[_u8](svuint8_t op1, svuint8_t op2) : "PMULLB Zresult.H, Zop1.B, Zop2.B"
  public static unsafe Vector<byte> PolynomialMultiplyWideningLower(Vector<byte> left, Vector<byte> right);

    /// svuint32_t svpmullb_pair[_u32](svuint32_t op1, svuint32_t op2) : "PMULLB Zresult.D, Zop1.S, Zop2.S"
  public static unsafe Vector<uint> PolynomialMultiplyWideningLower(Vector<uint> left, Vector<uint> right);


    /// PolynomialMultiplyWideningUpper : Polynomial multiply long (top)

    /// svuint16_t svpmullt[_u16](svuint8_t op1, svuint8_t op2) : "PMULLT Zresult.H, Zop1.B, Zop2.B"
  public static unsafe Vector<ushort> PolynomialMultiplyWideningUpper(Vector<byte> left, Vector<byte> right);

    /// svuint64_t svpmullt[_u64](svuint32_t op1, svuint32_t op2) : "PMULLT Zresult.D, Zop1.S, Zop2.S"
  public static unsafe Vector<ulong> PolynomialMultiplyWideningUpper(Vector<uint> left, Vector<uint> right);

    /// svuint8_t svpmullt_pair[_u8](svuint8_t op1, svuint8_t op2) : "PMULLT Zresult.H, Zop1.B, Zop2.B"
  public static unsafe Vector<byte> PolynomialMultiplyWideningUpper(Vector<byte> left, Vector<byte> right);

    /// svuint32_t svpmullt_pair[_u32](svuint32_t op1, svuint32_t op2) : "PMULLT Zresult.D, Zop1.S, Zop2.S"
  public static unsafe Vector<uint> PolynomialMultiplyWideningUpper(Vector<uint> left, Vector<uint> right);


    /// RoundingAddHighNarowingLower : Rounding add narrow high part (bottom)

    /// svint8_t svraddhnb[_s16](svint16_t op1, svint16_t op2) : "RADDHNB Zresult.B, Zop1.H, Zop2.H"
  public static unsafe Vector<sbyte> RoundingAddHighNarowingLower(Vector<short> left, Vector<short> right);

    /// svint16_t svraddhnb[_s32](svint32_t op1, svint32_t op2) : "RADDHNB Zresult.H, Zop1.S, Zop2.S"
  public static unsafe Vector<short> RoundingAddHighNarowingLower(Vector<int> left, Vector<int> right);

    /// svint32_t svraddhnb[_s64](svint64_t op1, svint64_t op2) : "RADDHNB Zresult.S, Zop1.D, Zop2.D"
  public static unsafe Vector<int> RoundingAddHighNarowingLower(Vector<long> left, Vector<long> right);

    /// svuint8_t svraddhnb[_u16](svuint16_t op1, svuint16_t op2) : "RADDHNB Zresult.B, Zop1.H, Zop2.H"
  public static unsafe Vector<byte> RoundingAddHighNarowingLower(Vector<ushort> left, Vector<ushort> right);

    /// svuint16_t svraddhnb[_u32](svuint32_t op1, svuint32_t op2) : "RADDHNB Zresult.H, Zop1.S, Zop2.S"
  public static unsafe Vector<ushort> RoundingAddHighNarowingLower(Vector<uint> left, Vector<uint> right);

    /// svuint32_t svraddhnb[_u64](svuint64_t op1, svuint64_t op2) : "RADDHNB Zresult.S, Zop1.D, Zop2.D"
  public static unsafe Vector<uint> RoundingAddHighNarowingLower(Vector<ulong> left, Vector<ulong> right);


    /// RoundingAddHighNarowingUpper : Rounding add narrow high part (top)

    /// svint8_t svraddhnt[_s16](svint8_t even, svint16_t op1, svint16_t op2) : "RADDHNT Ztied.B, Zop1.H, Zop2.H"
  public static unsafe Vector<sbyte> RoundingAddHighNarowingUpper(Vector<sbyte> even, Vector<short> left, Vector<short> right);

    /// svint16_t svraddhnt[_s32](svint16_t even, svint32_t op1, svint32_t op2) : "RADDHNT Ztied.H, Zop1.S, Zop2.S"
  public static unsafe Vector<short> RoundingAddHighNarowingUpper(Vector<short> even, Vector<int> left, Vector<int> right);

    /// svint32_t svraddhnt[_s64](svint32_t even, svint64_t op1, svint64_t op2) : "RADDHNT Ztied.S, Zop1.D, Zop2.D"
  public static unsafe Vector<int> RoundingAddHighNarowingUpper(Vector<int> even, Vector<long> left, Vector<long> right);

    /// svuint8_t svraddhnt[_u16](svuint8_t even, svuint16_t op1, svuint16_t op2) : "RADDHNT Ztied.B, Zop1.H, Zop2.H"
  public static unsafe Vector<byte> RoundingAddHighNarowingUpper(Vector<byte> even, Vector<ushort> left, Vector<ushort> right);

    /// svuint16_t svraddhnt[_u32](svuint16_t even, svuint32_t op1, svuint32_t op2) : "RADDHNT Ztied.H, Zop1.S, Zop2.S"
  public static unsafe Vector<ushort> RoundingAddHighNarowingUpper(Vector<ushort> even, Vector<uint> left, Vector<uint> right);

    /// svuint32_t svraddhnt[_u64](svuint32_t even, svuint64_t op1, svuint64_t op2) : "RADDHNT Ztied.S, Zop1.D, Zop2.D"
  public static unsafe Vector<uint> RoundingAddHighNarowingUpper(Vector<uint> even, Vector<ulong> left, Vector<ulong> right);


    /// RoundingHalvingAdd : Rounding halving add

    /// svint8_t svrhadd[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2) : "SRHADD Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; SRHADD Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svint8_t svrhadd[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2) : "SRHADD Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "SRHADD Ztied2.B, Pg/M, Ztied2.B, Zop1.B" or "MOVPRFX Zresult, Zop1; SRHADD Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svint8_t svrhadd[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; SRHADD Zresult.B, Pg/M, Zresult.B, Zop2.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; SRHADD Zresult.B, Pg/M, Zresult.B, Zop1.B"
  public static unsafe Vector<sbyte> RoundingHalvingAdd(Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t svrhadd[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2) : "SRHADD Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; SRHADD Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svint16_t svrhadd[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2) : "SRHADD Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "SRHADD Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "MOVPRFX Zresult, Zop1; SRHADD Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svint16_t svrhadd[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; SRHADD Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; SRHADD Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<short> RoundingHalvingAdd(Vector<short> left, Vector<short> right);

    /// svint32_t svrhadd[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2) : "SRHADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; SRHADD Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svint32_t svrhadd[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2) : "SRHADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "SRHADD Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "MOVPRFX Zresult, Zop1; SRHADD Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svint32_t svrhadd[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; SRHADD Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; SRHADD Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<int> RoundingHalvingAdd(Vector<int> left, Vector<int> right);

    /// svint64_t svrhadd[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2) : "SRHADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; SRHADD Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svint64_t svrhadd[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2) : "SRHADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "SRHADD Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "MOVPRFX Zresult, Zop1; SRHADD Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svint64_t svrhadd[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; SRHADD Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; SRHADD Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<long> RoundingHalvingAdd(Vector<long> left, Vector<long> right);

    /// svuint8_t svrhadd[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2) : "URHADD Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; URHADD Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svuint8_t svrhadd[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2) : "URHADD Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "URHADD Ztied2.B, Pg/M, Ztied2.B, Zop1.B" or "MOVPRFX Zresult, Zop1; URHADD Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svuint8_t svrhadd[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; URHADD Zresult.B, Pg/M, Zresult.B, Zop2.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; URHADD Zresult.B, Pg/M, Zresult.B, Zop1.B"
  public static unsafe Vector<byte> RoundingHalvingAdd(Vector<byte> left, Vector<byte> right);

    /// svuint16_t svrhadd[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2) : "URHADD Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; URHADD Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svuint16_t svrhadd[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2) : "URHADD Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "URHADD Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "MOVPRFX Zresult, Zop1; URHADD Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svuint16_t svrhadd[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; URHADD Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; URHADD Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<ushort> RoundingHalvingAdd(Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svrhadd[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2) : "URHADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; URHADD Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svuint32_t svrhadd[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2) : "URHADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "URHADD Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "MOVPRFX Zresult, Zop1; URHADD Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svuint32_t svrhadd[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; URHADD Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; URHADD Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<uint> RoundingHalvingAdd(Vector<uint> left, Vector<uint> right);

    /// svuint64_t svrhadd[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2) : "URHADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; URHADD Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svuint64_t svrhadd[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2) : "URHADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "URHADD Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "MOVPRFX Zresult, Zop1; URHADD Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svuint64_t svrhadd[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; URHADD Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; URHADD Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<ulong> RoundingHalvingAdd(Vector<ulong> left, Vector<ulong> right);


    /// RoundingSubtractHighNarowingLower : Rounding subtract narrow high part (bottom)

    /// svint8_t svrsubhnb[_s16](svint16_t op1, svint16_t op2) : "RSUBHNB Zresult.B, Zop1.H, Zop2.H"
  public static unsafe Vector<sbyte> RoundingSubtractHighNarowingLower(Vector<short> left, Vector<short> right);

    /// svint16_t svrsubhnb[_s32](svint32_t op1, svint32_t op2) : "RSUBHNB Zresult.H, Zop1.S, Zop2.S"
  public static unsafe Vector<short> RoundingSubtractHighNarowingLower(Vector<int> left, Vector<int> right);

    /// svint32_t svrsubhnb[_s64](svint64_t op1, svint64_t op2) : "RSUBHNB Zresult.S, Zop1.D, Zop2.D"
  public static unsafe Vector<int> RoundingSubtractHighNarowingLower(Vector<long> left, Vector<long> right);

    /// svuint8_t svrsubhnb[_u16](svuint16_t op1, svuint16_t op2) : "RSUBHNB Zresult.B, Zop1.H, Zop2.H"
  public static unsafe Vector<byte> RoundingSubtractHighNarowingLower(Vector<ushort> left, Vector<ushort> right);

    /// svuint16_t svrsubhnb[_u32](svuint32_t op1, svuint32_t op2) : "RSUBHNB Zresult.H, Zop1.S, Zop2.S"
  public static unsafe Vector<ushort> RoundingSubtractHighNarowingLower(Vector<uint> left, Vector<uint> right);

    /// svuint32_t svrsubhnb[_u64](svuint64_t op1, svuint64_t op2) : "RSUBHNB Zresult.S, Zop1.D, Zop2.D"
  public static unsafe Vector<uint> RoundingSubtractHighNarowingLower(Vector<ulong> left, Vector<ulong> right);


    /// RoundingSubtractHighNarowingUpper : Rounding subtract narrow high part (top)

    /// svint8_t svrsubhnt[_s16](svint8_t even, svint16_t op1, svint16_t op2) : "RSUBHNT Ztied.B, Zop1.H, Zop2.H"
  public static unsafe Vector<sbyte> RoundingSubtractHighNarowingUpper(Vector<sbyte> even, Vector<short> left, Vector<short> right);

    /// svint16_t svrsubhnt[_s32](svint16_t even, svint32_t op1, svint32_t op2) : "RSUBHNT Ztied.H, Zop1.S, Zop2.S"
  public static unsafe Vector<short> RoundingSubtractHighNarowingUpper(Vector<short> even, Vector<int> left, Vector<int> right);

    /// svint32_t svrsubhnt[_s64](svint32_t even, svint64_t op1, svint64_t op2) : "RSUBHNT Ztied.S, Zop1.D, Zop2.D"
  public static unsafe Vector<int> RoundingSubtractHighNarowingUpper(Vector<int> even, Vector<long> left, Vector<long> right);

    /// svuint8_t svrsubhnt[_u16](svuint8_t even, svuint16_t op1, svuint16_t op2) : "RSUBHNT Ztied.B, Zop1.H, Zop2.H"
  public static unsafe Vector<byte> RoundingSubtractHighNarowingUpper(Vector<byte> even, Vector<ushort> left, Vector<ushort> right);

    /// svuint16_t svrsubhnt[_u32](svuint16_t even, svuint32_t op1, svuint32_t op2) : "RSUBHNT Ztied.H, Zop1.S, Zop2.S"
  public static unsafe Vector<ushort> RoundingSubtractHighNarowingUpper(Vector<ushort> even, Vector<uint> left, Vector<uint> right);

    /// svuint32_t svrsubhnt[_u64](svuint32_t even, svuint64_t op1, svuint64_t op2) : "RSUBHNT Ztied.S, Zop1.D, Zop2.D"
  public static unsafe Vector<uint> RoundingSubtractHighNarowingUpper(Vector<uint> even, Vector<ulong> left, Vector<ulong> right);


    /// SaturatingAbs : Saturating absolute value

    /// svint8_t svqabs[_s8]_m(svint8_t inactive, svbool_t pg, svint8_t op) : "SQABS Ztied.B, Pg/M, Zop.B" or "MOVPRFX Zresult, Zinactive; SQABS Zresult.B, Pg/M, Zop.B"
    /// svint8_t svqabs[_s8]_x(svbool_t pg, svint8_t op) : "SQABS Ztied.B, Pg/M, Ztied.B" or "MOVPRFX Zresult, Zop; SQABS Zresult.B, Pg/M, Zop.B"
    /// svint8_t svqabs[_s8]_z(svbool_t pg, svint8_t op) : "MOVPRFX Zresult.B, Pg/Z, Zop.B; SQABS Zresult.B, Pg/M, Zop.B"
  public static unsafe Vector<sbyte> SaturatingAbs(Vector<sbyte> value);

    /// svint16_t svqabs[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op) : "SQABS Ztied.H, Pg/M, Zop.H" or "MOVPRFX Zresult, Zinactive; SQABS Zresult.H, Pg/M, Zop.H"
    /// svint16_t svqabs[_s16]_x(svbool_t pg, svint16_t op) : "SQABS Ztied.H, Pg/M, Ztied.H" or "MOVPRFX Zresult, Zop; SQABS Zresult.H, Pg/M, Zop.H"
    /// svint16_t svqabs[_s16]_z(svbool_t pg, svint16_t op) : "MOVPRFX Zresult.H, Pg/Z, Zop.H; SQABS Zresult.H, Pg/M, Zop.H"
  public static unsafe Vector<short> SaturatingAbs(Vector<short> value);

    /// svint32_t svqabs[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op) : "SQABS Ztied.S, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; SQABS Zresult.S, Pg/M, Zop.S"
    /// svint32_t svqabs[_s32]_x(svbool_t pg, svint32_t op) : "SQABS Ztied.S, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; SQABS Zresult.S, Pg/M, Zop.S"
    /// svint32_t svqabs[_s32]_z(svbool_t pg, svint32_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; SQABS Zresult.S, Pg/M, Zop.S"
  public static unsafe Vector<int> SaturatingAbs(Vector<int> value);

    /// svint64_t svqabs[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op) : "SQABS Ztied.D, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; SQABS Zresult.D, Pg/M, Zop.D"
    /// svint64_t svqabs[_s64]_x(svbool_t pg, svint64_t op) : "SQABS Ztied.D, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; SQABS Zresult.D, Pg/M, Zop.D"
    /// svint64_t svqabs[_s64]_z(svbool_t pg, svint64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; SQABS Zresult.D, Pg/M, Zop.D"
  public static unsafe Vector<long> SaturatingAbs(Vector<long> value);


    /// SaturatingDoublingMultiplyAddWideningLower : Saturating doubling multiply-add long (bottom)

    /// svint16_t svqdmlalb[_s16](svint16_t op1, svint8_t op2, svint8_t op3) : "SQDMLALB Ztied1.H, Zop2.B, Zop3.B" or "MOVPRFX Zresult, Zop1; SQDMLALB Zresult.H, Zop2.B, Zop3.B"
  public static unsafe Vector<short> SaturatingDoublingMultiplyAddWideningLower(Vector<short> op1, Vector<sbyte> op2, Vector<sbyte> op3);

    /// svint32_t svqdmlalb[_s32](svint32_t op1, svint16_t op2, svint16_t op3) : "SQDMLALB Ztied1.S, Zop2.H, Zop3.H" or "MOVPRFX Zresult, Zop1; SQDMLALB Zresult.S, Zop2.H, Zop3.H"
  public static unsafe Vector<int> SaturatingDoublingMultiplyAddWideningLower(Vector<int> op1, Vector<short> op2, Vector<short> op3);

    /// svint64_t svqdmlalb[_s64](svint64_t op1, svint32_t op2, svint32_t op3) : "SQDMLALB Ztied1.D, Zop2.S, Zop3.S" or "MOVPRFX Zresult, Zop1; SQDMLALB Zresult.D, Zop2.S, Zop3.S"
  public static unsafe Vector<long> SaturatingDoublingMultiplyAddWideningLower(Vector<long> op1, Vector<int> op2, Vector<int> op3);

    /// svint32_t svqdmlalb_lane[_s32](svint32_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index) : "SQDMLALB Ztied1.S, Zop2.H, Zop3.H[imm_index]" or "MOVPRFX Zresult, Zop1; SQDMLALB Zresult.S, Zop2.H, Zop3.H[imm_index]"
  public static unsafe Vector<int> SaturatingDoublingMultiplyAddWideningLower(Vector<int> op1, Vector<short> op2, Vector<short> op3, ulong imm_index);

    /// svint64_t svqdmlalb_lane[_s64](svint64_t op1, svint32_t op2, svint32_t op3, uint64_t imm_index) : "SQDMLALB Ztied1.D, Zop2.S, Zop3.S[imm_index]" or "MOVPRFX Zresult, Zop1; SQDMLALB Zresult.D, Zop2.S, Zop3.S[imm_index]"
  public static unsafe Vector<long> SaturatingDoublingMultiplyAddWideningLower(Vector<long> op1, Vector<int> op2, Vector<int> op3, ulong imm_index);


    /// SaturatingDoublingMultiplyAddWideningLowerUpper : Saturating doubling multiply-add long (bottom × top)

    /// svint16_t svqdmlalbt[_s16](svint16_t op1, svint8_t op2, svint8_t op3) : "SQDMLALBT Ztied1.H, Zop2.B, Zop3.B" or "MOVPRFX Zresult, Zop1; SQDMLALBT Zresult.H, Zop2.B, Zop3.B"
  public static unsafe Vector<short> SaturatingDoublingMultiplyAddWideningLowerUpper(Vector<short> op1, Vector<sbyte> op2, Vector<sbyte> op3);

    /// svint32_t svqdmlalbt[_s32](svint32_t op1, svint16_t op2, svint16_t op3) : "SQDMLALBT Ztied1.S, Zop2.H, Zop3.H" or "MOVPRFX Zresult, Zop1; SQDMLALBT Zresult.S, Zop2.H, Zop3.H"
  public static unsafe Vector<int> SaturatingDoublingMultiplyAddWideningLowerUpper(Vector<int> op1, Vector<short> op2, Vector<short> op3);

    /// svint64_t svqdmlalbt[_s64](svint64_t op1, svint32_t op2, svint32_t op3) : "SQDMLALBT Ztied1.D, Zop2.S, Zop3.S" or "MOVPRFX Zresult, Zop1; SQDMLALBT Zresult.D, Zop2.S, Zop3.S"
  public static unsafe Vector<long> SaturatingDoublingMultiplyAddWideningLowerUpper(Vector<long> op1, Vector<int> op2, Vector<int> op3);


    /// SaturatingDoublingMultiplyAddWideningUpper : Saturating doubling multiply-add long (top)

    /// svint16_t svqdmlalt[_s16](svint16_t op1, svint8_t op2, svint8_t op3) : "SQDMLALT Ztied1.H, Zop2.B, Zop3.B" or "MOVPRFX Zresult, Zop1; SQDMLALT Zresult.H, Zop2.B, Zop3.B"
  public static unsafe Vector<short> SaturatingDoublingMultiplyAddWideningUpper(Vector<short> op1, Vector<sbyte> op2, Vector<sbyte> op3);

    /// svint32_t svqdmlalt[_s32](svint32_t op1, svint16_t op2, svint16_t op3) : "SQDMLALT Ztied1.S, Zop2.H, Zop3.H" or "MOVPRFX Zresult, Zop1; SQDMLALT Zresult.S, Zop2.H, Zop3.H"
  public static unsafe Vector<int> SaturatingDoublingMultiplyAddWideningUpper(Vector<int> op1, Vector<short> op2, Vector<short> op3);

    /// svint64_t svqdmlalt[_s64](svint64_t op1, svint32_t op2, svint32_t op3) : "SQDMLALT Ztied1.D, Zop2.S, Zop3.S" or "MOVPRFX Zresult, Zop1; SQDMLALT Zresult.D, Zop2.S, Zop3.S"
  public static unsafe Vector<long> SaturatingDoublingMultiplyAddWideningUpper(Vector<long> op1, Vector<int> op2, Vector<int> op3);

    /// svint32_t svqdmlalt_lane[_s32](svint32_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index) : "SQDMLALT Ztied1.S, Zop2.H, Zop3.H[imm_index]" or "MOVPRFX Zresult, Zop1; SQDMLALT Zresult.S, Zop2.H, Zop3.H[imm_index]"
  public static unsafe Vector<int> SaturatingDoublingMultiplyAddWideningUpper(Vector<int> op1, Vector<short> op2, Vector<short> op3, ulong imm_index);

    /// svint64_t svqdmlalt_lane[_s64](svint64_t op1, svint32_t op2, svint32_t op3, uint64_t imm_index) : "SQDMLALT Ztied1.D, Zop2.S, Zop3.S[imm_index]" or "MOVPRFX Zresult, Zop1; SQDMLALT Zresult.D, Zop2.S, Zop3.S[imm_index]"
  public static unsafe Vector<long> SaturatingDoublingMultiplyAddWideningUpper(Vector<long> op1, Vector<int> op2, Vector<int> op3, ulong imm_index);


    /// SaturatingDoublingMultiplyHigh : Saturating doubling multiply high

    /// svint8_t svqdmulh[_s8](svint8_t op1, svint8_t op2) : "SQDMULH Zresult.B, Zop1.B, Zop2.B"
  public static unsafe Vector<sbyte> SaturatingDoublingMultiplyHigh(Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t svqdmulh[_s16](svint16_t op1, svint16_t op2) : "SQDMULH Zresult.H, Zop1.H, Zop2.H"
  public static unsafe Vector<short> SaturatingDoublingMultiplyHigh(Vector<short> left, Vector<short> right);

    /// svint32_t svqdmulh[_s32](svint32_t op1, svint32_t op2) : "SQDMULH Zresult.S, Zop1.S, Zop2.S"
  public static unsafe Vector<int> SaturatingDoublingMultiplyHigh(Vector<int> left, Vector<int> right);

    /// svint64_t svqdmulh[_s64](svint64_t op1, svint64_t op2) : "SQDMULH Zresult.D, Zop1.D, Zop2.D"
  public static unsafe Vector<long> SaturatingDoublingMultiplyHigh(Vector<long> left, Vector<long> right);

    /// svint16_t svqdmulh_lane[_s16](svint16_t op1, svint16_t op2, uint64_t imm_index) : "SQDMULH Zresult.H, Zop1.H, Zop2.H[imm_index]"
  public static unsafe Vector<short> SaturatingDoublingMultiplyHigh(Vector<short> op1, Vector<short> op2, ulong imm_index);

    /// svint32_t svqdmulh_lane[_s32](svint32_t op1, svint32_t op2, uint64_t imm_index) : "SQDMULH Zresult.S, Zop1.S, Zop2.S[imm_index]"
  public static unsafe Vector<int> SaturatingDoublingMultiplyHigh(Vector<int> op1, Vector<int> op2, ulong imm_index);

    /// svint64_t svqdmulh_lane[_s64](svint64_t op1, svint64_t op2, uint64_t imm_index) : "SQDMULH Zresult.D, Zop1.D, Zop2.D[imm_index]"
  public static unsafe Vector<long> SaturatingDoublingMultiplyHigh(Vector<long> op1, Vector<long> op2, ulong imm_index);


    /// SaturatingDoublingMultiplySubtractWideningLower : Saturating doubling multiply-subtract long (bottom)

    /// svint16_t svqdmlslb[_s16](svint16_t op1, svint8_t op2, svint8_t op3) : "SQDMLSLB Ztied1.H, Zop2.B, Zop3.B" or "MOVPRFX Zresult, Zop1; SQDMLSLB Zresult.H, Zop2.B, Zop3.B"
  public static unsafe Vector<short> SaturatingDoublingMultiplySubtractWideningLower(Vector<short> op1, Vector<sbyte> op2, Vector<sbyte> op3);

    /// svint32_t svqdmlslb[_s32](svint32_t op1, svint16_t op2, svint16_t op3) : "SQDMLSLB Ztied1.S, Zop2.H, Zop3.H" or "MOVPRFX Zresult, Zop1; SQDMLSLB Zresult.S, Zop2.H, Zop3.H"
  public static unsafe Vector<int> SaturatingDoublingMultiplySubtractWideningLower(Vector<int> op1, Vector<short> op2, Vector<short> op3);

    /// svint64_t svqdmlslb[_s64](svint64_t op1, svint32_t op2, svint32_t op3) : "SQDMLSLB Ztied1.D, Zop2.S, Zop3.S" or "MOVPRFX Zresult, Zop1; SQDMLSLB Zresult.D, Zop2.S, Zop3.S"
  public static unsafe Vector<long> SaturatingDoublingMultiplySubtractWideningLower(Vector<long> op1, Vector<int> op2, Vector<int> op3);

    /// svint32_t svqdmlslb_lane[_s32](svint32_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index) : "SQDMLSLB Ztied1.S, Zop2.H, Zop3.H[imm_index]" or "MOVPRFX Zresult, Zop1; SQDMLSLB Zresult.S, Zop2.H, Zop3.H[imm_index]"
  public static unsafe Vector<int> SaturatingDoublingMultiplySubtractWideningLower(Vector<int> op1, Vector<short> op2, Vector<short> op3, ulong imm_index);

    /// svint64_t svqdmlslb_lane[_s64](svint64_t op1, svint32_t op2, svint32_t op3, uint64_t imm_index) : "SQDMLSLB Ztied1.D, Zop2.S, Zop3.S[imm_index]" or "MOVPRFX Zresult, Zop1; SQDMLSLB Zresult.D, Zop2.S, Zop3.S[imm_index]"
  public static unsafe Vector<long> SaturatingDoublingMultiplySubtractWideningLower(Vector<long> op1, Vector<int> op2, Vector<int> op3, ulong imm_index);


    /// SaturatingDoublingMultiplySubtractWideningLowerUpper : Saturating doubling multiply-subtract long (bottom × top)

    /// svint16_t svqdmlslbt[_s16](svint16_t op1, svint8_t op2, svint8_t op3) : "SQDMLSLBT Ztied1.H, Zop2.B, Zop3.B" or "MOVPRFX Zresult, Zop1; SQDMLSLBT Zresult.H, Zop2.B, Zop3.B"
  public static unsafe Vector<short> SaturatingDoublingMultiplySubtractWideningLowerUpper(Vector<short> op1, Vector<sbyte> op2, Vector<sbyte> op3);

    /// svint32_t svqdmlslbt[_s32](svint32_t op1, svint16_t op2, svint16_t op3) : "SQDMLSLBT Ztied1.S, Zop2.H, Zop3.H" or "MOVPRFX Zresult, Zop1; SQDMLSLBT Zresult.S, Zop2.H, Zop3.H"
  public static unsafe Vector<int> SaturatingDoublingMultiplySubtractWideningLowerUpper(Vector<int> op1, Vector<short> op2, Vector<short> op3);

    /// svint64_t svqdmlslbt[_s64](svint64_t op1, svint32_t op2, svint32_t op3) : "SQDMLSLBT Ztied1.D, Zop2.S, Zop3.S" or "MOVPRFX Zresult, Zop1; SQDMLSLBT Zresult.D, Zop2.S, Zop3.S"
  public static unsafe Vector<long> SaturatingDoublingMultiplySubtractWideningLowerUpper(Vector<long> op1, Vector<int> op2, Vector<int> op3);


    /// SaturatingDoublingMultiplySubtractWideningUpper : Saturating doubling multiply-subtract long (top)

    /// svint16_t svqdmlslt[_s16](svint16_t op1, svint8_t op2, svint8_t op3) : "SQDMLSLT Ztied1.H, Zop2.B, Zop3.B" or "MOVPRFX Zresult, Zop1; SQDMLSLT Zresult.H, Zop2.B, Zop3.B"
  public static unsafe Vector<short> SaturatingDoublingMultiplySubtractWideningUpper(Vector<short> op1, Vector<sbyte> op2, Vector<sbyte> op3);

    /// svint32_t svqdmlslt[_s32](svint32_t op1, svint16_t op2, svint16_t op3) : "SQDMLSLT Ztied1.S, Zop2.H, Zop3.H" or "MOVPRFX Zresult, Zop1; SQDMLSLT Zresult.S, Zop2.H, Zop3.H"
  public static unsafe Vector<int> SaturatingDoublingMultiplySubtractWideningUpper(Vector<int> op1, Vector<short> op2, Vector<short> op3);

    /// svint64_t svqdmlslt[_s64](svint64_t op1, svint32_t op2, svint32_t op3) : "SQDMLSLT Ztied1.D, Zop2.S, Zop3.S" or "MOVPRFX Zresult, Zop1; SQDMLSLT Zresult.D, Zop2.S, Zop3.S"
  public static unsafe Vector<long> SaturatingDoublingMultiplySubtractWideningUpper(Vector<long> op1, Vector<int> op2, Vector<int> op3);

    /// svint32_t svqdmlslt_lane[_s32](svint32_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index) : "SQDMLSLT Ztied1.S, Zop2.H, Zop3.H[imm_index]" or "MOVPRFX Zresult, Zop1; SQDMLSLT Zresult.S, Zop2.H, Zop3.H[imm_index]"
  public static unsafe Vector<int> SaturatingDoublingMultiplySubtractWideningUpper(Vector<int> op1, Vector<short> op2, Vector<short> op3, ulong imm_index);

    /// svint64_t svqdmlslt_lane[_s64](svint64_t op1, svint32_t op2, svint32_t op3, uint64_t imm_index) : "SQDMLSLT Ztied1.D, Zop2.S, Zop3.S[imm_index]" or "MOVPRFX Zresult, Zop1; SQDMLSLT Zresult.D, Zop2.S, Zop3.S[imm_index]"
  public static unsafe Vector<long> SaturatingDoublingMultiplySubtractWideningUpper(Vector<long> op1, Vector<int> op2, Vector<int> op3, ulong imm_index);


    /// SaturatingDoublingMultiplyWideningLower : Saturating doubling multiply long (bottom)

    /// svint16_t svqdmullb[_s16](svint8_t op1, svint8_t op2) : "SQDMULLB Zresult.H, Zop1.B, Zop2.B"
  public static unsafe Vector<short> SaturatingDoublingMultiplyWideningLower(Vector<sbyte> left, Vector<sbyte> right);

    /// svint32_t svqdmullb[_s32](svint16_t op1, svint16_t op2) : "SQDMULLB Zresult.S, Zop1.H, Zop2.H"
  public static unsafe Vector<int> SaturatingDoublingMultiplyWideningLower(Vector<short> left, Vector<short> right);

    /// svint64_t svqdmullb[_s64](svint32_t op1, svint32_t op2) : "SQDMULLB Zresult.D, Zop1.S, Zop2.S"
  public static unsafe Vector<long> SaturatingDoublingMultiplyWideningLower(Vector<int> left, Vector<int> right);

    /// svint32_t svqdmullb_lane[_s32](svint16_t op1, svint16_t op2, uint64_t imm_index) : "SQDMULLB Zresult.S, Zop1.H, Zop2.H[imm_index]"
  public static unsafe Vector<int> SaturatingDoublingMultiplyWideningLower(Vector<short> op1, Vector<short> op2, ulong imm_index);

    /// svint64_t svqdmullb_lane[_s64](svint32_t op1, svint32_t op2, uint64_t imm_index) : "SQDMULLB Zresult.D, Zop1.S, Zop2.S[imm_index]"
  public static unsafe Vector<long> SaturatingDoublingMultiplyWideningLower(Vector<int> op1, Vector<int> op2, ulong imm_index);


    /// SaturatingDoublingMultiplyWideningUpper : Saturating doubling multiply long (top)

    /// svint16_t svqdmullt[_s16](svint8_t op1, svint8_t op2) : "SQDMULLT Zresult.H, Zop1.B, Zop2.B"
  public static unsafe Vector<short> SaturatingDoublingMultiplyWideningUpper(Vector<sbyte> left, Vector<sbyte> right);

    /// svint32_t svqdmullt[_s32](svint16_t op1, svint16_t op2) : "SQDMULLT Zresult.S, Zop1.H, Zop2.H"
  public static unsafe Vector<int> SaturatingDoublingMultiplyWideningUpper(Vector<short> left, Vector<short> right);

    /// svint64_t svqdmullt[_s64](svint32_t op1, svint32_t op2) : "SQDMULLT Zresult.D, Zop1.S, Zop2.S"
  public static unsafe Vector<long> SaturatingDoublingMultiplyWideningUpper(Vector<int> left, Vector<int> right);

    /// svint32_t svqdmullt_lane[_s32](svint16_t op1, svint16_t op2, uint64_t imm_index) : "SQDMULLT Zresult.S, Zop1.H, Zop2.H[imm_index]"
  public static unsafe Vector<int> SaturatingDoublingMultiplyWideningUpper(Vector<short> op1, Vector<short> op2, ulong imm_index);

    /// svint64_t svqdmullt_lane[_s64](svint32_t op1, svint32_t op2, uint64_t imm_index) : "SQDMULLT Zresult.D, Zop1.S, Zop2.S[imm_index]"
  public static unsafe Vector<long> SaturatingDoublingMultiplyWideningUpper(Vector<int> op1, Vector<int> op2, ulong imm_index);


    /// SaturatingNegate : Saturating negate

    /// svint8_t svqneg[_s8]_m(svint8_t inactive, svbool_t pg, svint8_t op) : "SQNEG Ztied.B, Pg/M, Zop.B" or "MOVPRFX Zresult, Zinactive; SQNEG Zresult.B, Pg/M, Zop.B"
    /// svint8_t svqneg[_s8]_x(svbool_t pg, svint8_t op) : "SQNEG Ztied.B, Pg/M, Ztied.B" or "MOVPRFX Zresult, Zop; SQNEG Zresult.B, Pg/M, Zop.B"
    /// svint8_t svqneg[_s8]_z(svbool_t pg, svint8_t op) : "MOVPRFX Zresult.B, Pg/Z, Zop.B; SQNEG Zresult.B, Pg/M, Zop.B"
  public static unsafe Vector<sbyte> SaturatingNegate(Vector<sbyte> value);

    /// svint16_t svqneg[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op) : "SQNEG Ztied.H, Pg/M, Zop.H" or "MOVPRFX Zresult, Zinactive; SQNEG Zresult.H, Pg/M, Zop.H"
    /// svint16_t svqneg[_s16]_x(svbool_t pg, svint16_t op) : "SQNEG Ztied.H, Pg/M, Ztied.H" or "MOVPRFX Zresult, Zop; SQNEG Zresult.H, Pg/M, Zop.H"
    /// svint16_t svqneg[_s16]_z(svbool_t pg, svint16_t op) : "MOVPRFX Zresult.H, Pg/Z, Zop.H; SQNEG Zresult.H, Pg/M, Zop.H"
  public static unsafe Vector<short> SaturatingNegate(Vector<short> value);

    /// svint32_t svqneg[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op) : "SQNEG Ztied.S, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; SQNEG Zresult.S, Pg/M, Zop.S"
    /// svint32_t svqneg[_s32]_x(svbool_t pg, svint32_t op) : "SQNEG Ztied.S, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; SQNEG Zresult.S, Pg/M, Zop.S"
    /// svint32_t svqneg[_s32]_z(svbool_t pg, svint32_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; SQNEG Zresult.S, Pg/M, Zop.S"
  public static unsafe Vector<int> SaturatingNegate(Vector<int> value);

    /// svint64_t svqneg[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op) : "SQNEG Ztied.D, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; SQNEG Zresult.D, Pg/M, Zop.D"
    /// svint64_t svqneg[_s64]_x(svbool_t pg, svint64_t op) : "SQNEG Ztied.D, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; SQNEG Zresult.D, Pg/M, Zop.D"
    /// svint64_t svqneg[_s64]_z(svbool_t pg, svint64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; SQNEG Zresult.D, Pg/M, Zop.D"
  public static unsafe Vector<long> SaturatingNegate(Vector<long> value);


    /// SaturatingRoundingDoublingMultiplyAddHigh : Saturating rounding doubling multiply-add high

    /// svint8_t svqrdmlah[_s8](svint8_t op1, svint8_t op2, svint8_t op3) : "SQRDMLAH Ztied1.B, Zop2.B, Zop3.B" or "MOVPRFX Zresult, Zop1; SQRDMLAH Zresult.B, Zop2.B, Zop3.B"
  public static unsafe Vector<sbyte> SaturatingRoundingDoublingMultiplyAddHigh(Vector<sbyte> op1, Vector<sbyte> op2, Vector<sbyte> op3);

    /// svint16_t svqrdmlah[_s16](svint16_t op1, svint16_t op2, svint16_t op3) : "SQRDMLAH Ztied1.H, Zop2.H, Zop3.H" or "MOVPRFX Zresult, Zop1; SQRDMLAH Zresult.H, Zop2.H, Zop3.H"
  public static unsafe Vector<short> SaturatingRoundingDoublingMultiplyAddHigh(Vector<short> op1, Vector<short> op2, Vector<short> op3);

    /// svint32_t svqrdmlah[_s32](svint32_t op1, svint32_t op2, svint32_t op3) : "SQRDMLAH Ztied1.S, Zop2.S, Zop3.S" or "MOVPRFX Zresult, Zop1; SQRDMLAH Zresult.S, Zop2.S, Zop3.S"
  public static unsafe Vector<int> SaturatingRoundingDoublingMultiplyAddHigh(Vector<int> op1, Vector<int> op2, Vector<int> op3);

    /// svint64_t svqrdmlah[_s64](svint64_t op1, svint64_t op2, svint64_t op3) : "SQRDMLAH Ztied1.D, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; SQRDMLAH Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<long> SaturatingRoundingDoublingMultiplyAddHigh(Vector<long> op1, Vector<long> op2, Vector<long> op3);

    /// svint16_t svqrdmlah_lane[_s16](svint16_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index) : "SQRDMLAH Ztied1.H, Zop2.H, Zop3.H[imm_index]" or "MOVPRFX Zresult, Zop1; SQRDMLAH Zresult.H, Zop2.H, Zop3.H[imm_index]"
  public static unsafe Vector<short> SaturatingRoundingDoublingMultiplyAddHigh(Vector<short> op1, Vector<short> op2, Vector<short> op3, ulong imm_index);

    /// svint32_t svqrdmlah_lane[_s32](svint32_t op1, svint32_t op2, svint32_t op3, uint64_t imm_index) : "SQRDMLAH Ztied1.S, Zop2.S, Zop3.S[imm_index]" or "MOVPRFX Zresult, Zop1; SQRDMLAH Zresult.S, Zop2.S, Zop3.S[imm_index]"
  public static unsafe Vector<int> SaturatingRoundingDoublingMultiplyAddHigh(Vector<int> op1, Vector<int> op2, Vector<int> op3, ulong imm_index);

    /// svint64_t svqrdmlah_lane[_s64](svint64_t op1, svint64_t op2, svint64_t op3, uint64_t imm_index) : "SQRDMLAH Ztied1.D, Zop2.D, Zop3.D[imm_index]" or "MOVPRFX Zresult, Zop1; SQRDMLAH Zresult.D, Zop2.D, Zop3.D[imm_index]"
  public static unsafe Vector<long> SaturatingRoundingDoublingMultiplyAddHigh(Vector<long> op1, Vector<long> op2, Vector<long> op3, ulong imm_index);


    /// SaturatingRoundingDoublingMultiplyHigh : Saturating rounding doubling multiply high

    /// svint8_t svqrdmulh[_s8](svint8_t op1, svint8_t op2) : "SQRDMULH Zresult.B, Zop1.B, Zop2.B"
  public static unsafe Vector<sbyte> SaturatingRoundingDoublingMultiplyHigh(Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t svqrdmulh[_s16](svint16_t op1, svint16_t op2) : "SQRDMULH Zresult.H, Zop1.H, Zop2.H"
  public static unsafe Vector<short> SaturatingRoundingDoublingMultiplyHigh(Vector<short> left, Vector<short> right);

    /// svint32_t svqrdmulh[_s32](svint32_t op1, svint32_t op2) : "SQRDMULH Zresult.S, Zop1.S, Zop2.S"
  public static unsafe Vector<int> SaturatingRoundingDoublingMultiplyHigh(Vector<int> left, Vector<int> right);

    /// svint64_t svqrdmulh[_s64](svint64_t op1, svint64_t op2) : "SQRDMULH Zresult.D, Zop1.D, Zop2.D"
  public static unsafe Vector<long> SaturatingRoundingDoublingMultiplyHigh(Vector<long> left, Vector<long> right);

    /// svint16_t svqrdmulh_lane[_s16](svint16_t op1, svint16_t op2, uint64_t imm_index) : "SQRDMULH Zresult.H, Zop1.H, Zop2.H[imm_index]"
  public static unsafe Vector<short> SaturatingRoundingDoublingMultiplyHigh(Vector<short> op1, Vector<short> op2, ulong imm_index);

    /// svint32_t svqrdmulh_lane[_s32](svint32_t op1, svint32_t op2, uint64_t imm_index) : "SQRDMULH Zresult.S, Zop1.S, Zop2.S[imm_index]"
  public static unsafe Vector<int> SaturatingRoundingDoublingMultiplyHigh(Vector<int> op1, Vector<int> op2, ulong imm_index);

    /// svint64_t svqrdmulh_lane[_s64](svint64_t op1, svint64_t op2, uint64_t imm_index) : "SQRDMULH Zresult.D, Zop1.D, Zop2.D[imm_index]"
  public static unsafe Vector<long> SaturatingRoundingDoublingMultiplyHigh(Vector<long> op1, Vector<long> op2, ulong imm_index);


    /// SaturatingRoundingDoublingMultiplySubtractHigh : Saturating rounding doubling multiply-subtract high

    /// svint8_t svqrdmlsh[_s8](svint8_t op1, svint8_t op2, svint8_t op3) : "SQRDMLSH Ztied1.B, Zop2.B, Zop3.B" or "MOVPRFX Zresult, Zop1; SQRDMLSH Zresult.B, Zop2.B, Zop3.B"
  public static unsafe Vector<sbyte> SaturatingRoundingDoublingMultiplySubtractHigh(Vector<sbyte> op1, Vector<sbyte> op2, Vector<sbyte> op3);

    /// svint16_t svqrdmlsh[_s16](svint16_t op1, svint16_t op2, svint16_t op3) : "SQRDMLSH Ztied1.H, Zop2.H, Zop3.H" or "MOVPRFX Zresult, Zop1; SQRDMLSH Zresult.H, Zop2.H, Zop3.H"
  public static unsafe Vector<short> SaturatingRoundingDoublingMultiplySubtractHigh(Vector<short> op1, Vector<short> op2, Vector<short> op3);

    /// svint32_t svqrdmlsh[_s32](svint32_t op1, svint32_t op2, svint32_t op3) : "SQRDMLSH Ztied1.S, Zop2.S, Zop3.S" or "MOVPRFX Zresult, Zop1; SQRDMLSH Zresult.S, Zop2.S, Zop3.S"
  public static unsafe Vector<int> SaturatingRoundingDoublingMultiplySubtractHigh(Vector<int> op1, Vector<int> op2, Vector<int> op3);

    /// svint64_t svqrdmlsh[_s64](svint64_t op1, svint64_t op2, svint64_t op3) : "SQRDMLSH Ztied1.D, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; SQRDMLSH Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<long> SaturatingRoundingDoublingMultiplySubtractHigh(Vector<long> op1, Vector<long> op2, Vector<long> op3);

    /// svint16_t svqrdmlsh_lane[_s16](svint16_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index) : "SQRDMLSH Ztied1.H, Zop2.H, Zop3.H[imm_index]" or "MOVPRFX Zresult, Zop1; SQRDMLSH Zresult.H, Zop2.H, Zop3.H[imm_index]"
  public static unsafe Vector<short> SaturatingRoundingDoublingMultiplySubtractHigh(Vector<short> op1, Vector<short> op2, Vector<short> op3, ulong imm_index);

    /// svint32_t svqrdmlsh_lane[_s32](svint32_t op1, svint32_t op2, svint32_t op3, uint64_t imm_index) : "SQRDMLSH Ztied1.S, Zop2.S, Zop3.S[imm_index]" or "MOVPRFX Zresult, Zop1; SQRDMLSH Zresult.S, Zop2.S, Zop3.S[imm_index]"
  public static unsafe Vector<int> SaturatingRoundingDoublingMultiplySubtractHigh(Vector<int> op1, Vector<int> op2, Vector<int> op3, ulong imm_index);

    /// svint64_t svqrdmlsh_lane[_s64](svint64_t op1, svint64_t op2, svint64_t op3, uint64_t imm_index) : "SQRDMLSH Ztied1.D, Zop2.D, Zop3.D[imm_index]" or "MOVPRFX Zresult, Zop1; SQRDMLSH Zresult.D, Zop2.D, Zop3.D[imm_index]"
  public static unsafe Vector<long> SaturatingRoundingDoublingMultiplySubtractHigh(Vector<long> op1, Vector<long> op2, Vector<long> op3, ulong imm_index);


    /// SubtractHighNarowingLower : Subtract narrow high part (bottom)

    /// svint8_t svsubhnb[_s16](svint16_t op1, svint16_t op2) : "SUBHNB Zresult.B, Zop1.H, Zop2.H"
  public static unsafe Vector<sbyte> SubtractHighNarowingLower(Vector<short> left, Vector<short> right);

    /// svint16_t svsubhnb[_s32](svint32_t op1, svint32_t op2) : "SUBHNB Zresult.H, Zop1.S, Zop2.S"
  public static unsafe Vector<short> SubtractHighNarowingLower(Vector<int> left, Vector<int> right);

    /// svint32_t svsubhnb[_s64](svint64_t op1, svint64_t op2) : "SUBHNB Zresult.S, Zop1.D, Zop2.D"
  public static unsafe Vector<int> SubtractHighNarowingLower(Vector<long> left, Vector<long> right);

    /// svuint8_t svsubhnb[_u16](svuint16_t op1, svuint16_t op2) : "SUBHNB Zresult.B, Zop1.H, Zop2.H"
  public static unsafe Vector<byte> SubtractHighNarowingLower(Vector<ushort> left, Vector<ushort> right);

    /// svuint16_t svsubhnb[_u32](svuint32_t op1, svuint32_t op2) : "SUBHNB Zresult.H, Zop1.S, Zop2.S"
  public static unsafe Vector<ushort> SubtractHighNarowingLower(Vector<uint> left, Vector<uint> right);

    /// svuint32_t svsubhnb[_u64](svuint64_t op1, svuint64_t op2) : "SUBHNB Zresult.S, Zop1.D, Zop2.D"
  public static unsafe Vector<uint> SubtractHighNarowingLower(Vector<ulong> left, Vector<ulong> right);


    /// SubtractHighNarowingUpper : Subtract narrow high part (top)

    /// svint8_t svsubhnt[_s16](svint8_t even, svint16_t op1, svint16_t op2) : "SUBHNT Ztied.B, Zop1.H, Zop2.H"
  public static unsafe Vector<sbyte> SubtractHighNarowingUpper(Vector<sbyte> even, Vector<short> left, Vector<short> right);

    /// svint16_t svsubhnt[_s32](svint16_t even, svint32_t op1, svint32_t op2) : "SUBHNT Ztied.H, Zop1.S, Zop2.S"
  public static unsafe Vector<short> SubtractHighNarowingUpper(Vector<short> even, Vector<int> left, Vector<int> right);

    /// svint32_t svsubhnt[_s64](svint32_t even, svint64_t op1, svint64_t op2) : "SUBHNT Ztied.S, Zop1.D, Zop2.D"
  public static unsafe Vector<int> SubtractHighNarowingUpper(Vector<int> even, Vector<long> left, Vector<long> right);

    /// svuint8_t svsubhnt[_u16](svuint8_t even, svuint16_t op1, svuint16_t op2) : "SUBHNT Ztied.B, Zop1.H, Zop2.H"
  public static unsafe Vector<byte> SubtractHighNarowingUpper(Vector<byte> even, Vector<ushort> left, Vector<ushort> right);

    /// svuint16_t svsubhnt[_u32](svuint16_t even, svuint32_t op1, svuint32_t op2) : "SUBHNT Ztied.H, Zop1.S, Zop2.S"
  public static unsafe Vector<ushort> SubtractHighNarowingUpper(Vector<ushort> even, Vector<uint> left, Vector<uint> right);

    /// svuint32_t svsubhnt[_u64](svuint32_t even, svuint64_t op1, svuint64_t op2) : "SUBHNT Ztied.S, Zop1.D, Zop2.D"
  public static unsafe Vector<uint> SubtractHighNarowingUpper(Vector<uint> even, Vector<ulong> left, Vector<ulong> right);


    /// SubtractSaturate : Saturating subtract

    /// svint8_t svqsub[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2) : "SQSUB Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; SQSUB Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svint8_t svqsub[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2) : "SQSUB Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "SQSUBR Ztied2.B, Pg/M, Ztied2.B, Zop1.B" or "SQSUB Zresult.B, Zop1.B, Zop2.B"
    /// svint8_t svqsub[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; SQSUB Zresult.B, Pg/M, Zresult.B, Zop2.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; SQSUBR Zresult.B, Pg/M, Zresult.B, Zop1.B"
  public static unsafe Vector<sbyte> SubtractSaturate(Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t svqsub[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2) : "SQSUB Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; SQSUB Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svint16_t svqsub[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2) : "SQSUB Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "SQSUBR Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "SQSUB Zresult.H, Zop1.H, Zop2.H"
    /// svint16_t svqsub[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; SQSUB Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; SQSUBR Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<short> SubtractSaturate(Vector<short> left, Vector<short> right);

    /// svint32_t svqsub[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2) : "SQSUB Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; SQSUB Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svint32_t svqsub[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2) : "SQSUB Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "SQSUBR Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "SQSUB Zresult.S, Zop1.S, Zop2.S"
    /// svint32_t svqsub[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; SQSUB Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; SQSUBR Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<int> SubtractSaturate(Vector<int> left, Vector<int> right);

    /// svint64_t svqsub[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2) : "SQSUB Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; SQSUB Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svint64_t svqsub[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2) : "SQSUB Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "SQSUBR Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "SQSUB Zresult.D, Zop1.D, Zop2.D"
    /// svint64_t svqsub[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; SQSUB Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; SQSUBR Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<long> SubtractSaturate(Vector<long> left, Vector<long> right);

    /// svuint8_t svqsub[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2) : "UQSUB Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; UQSUB Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svuint8_t svqsub[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2) : "UQSUB Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "UQSUBR Ztied2.B, Pg/M, Ztied2.B, Zop1.B" or "UQSUB Zresult.B, Zop1.B, Zop2.B"
    /// svuint8_t svqsub[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; UQSUB Zresult.B, Pg/M, Zresult.B, Zop2.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; UQSUBR Zresult.B, Pg/M, Zresult.B, Zop1.B"
  public static unsafe Vector<byte> SubtractSaturate(Vector<byte> left, Vector<byte> right);

    /// svuint16_t svqsub[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2) : "UQSUB Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; UQSUB Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svuint16_t svqsub[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2) : "UQSUB Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "UQSUBR Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "UQSUB Zresult.H, Zop1.H, Zop2.H"
    /// svuint16_t svqsub[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; UQSUB Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; UQSUBR Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<ushort> SubtractSaturate(Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svqsub[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2) : "UQSUB Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; UQSUB Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svuint32_t svqsub[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2) : "UQSUB Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "UQSUBR Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "UQSUB Zresult.S, Zop1.S, Zop2.S"
    /// svuint32_t svqsub[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; UQSUB Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; UQSUBR Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<uint> SubtractSaturate(Vector<uint> left, Vector<uint> right);

    /// svuint64_t svqsub[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2) : "UQSUB Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; UQSUB Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svuint64_t svqsub[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2) : "UQSUB Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "UQSUBR Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "UQSUB Zresult.D, Zop1.D, Zop2.D"
    /// svuint64_t svqsub[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; UQSUB Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; UQSUBR Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<ulong> SubtractSaturate(Vector<ulong> left, Vector<ulong> right);


    /// SubtractSaturateReversed : Saturating subtract reversed

    /// svint8_t svqsubr[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2) : "SQSUBR Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; SQSUBR Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svint8_t svqsubr[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2) : "SQSUBR Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "SQSUB Ztied2.B, Pg/M, Ztied2.B, Zop1.B" or "SQSUB Zresult.B, Zop2.B, Zop1.B"
    /// svint8_t svqsubr[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; SQSUBR Zresult.B, Pg/M, Zresult.B, Zop2.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; SQSUB Zresult.B, Pg/M, Zresult.B, Zop1.B"
  public static unsafe Vector<sbyte> SubtractSaturateReversed(Vector<sbyte> left, Vector<sbyte> right);

    /// svint16_t svqsubr[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2) : "SQSUBR Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; SQSUBR Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svint16_t svqsubr[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2) : "SQSUBR Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "SQSUB Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "SQSUB Zresult.H, Zop2.H, Zop1.H"
    /// svint16_t svqsubr[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; SQSUBR Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; SQSUB Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<short> SubtractSaturateReversed(Vector<short> left, Vector<short> right);

    /// svint32_t svqsubr[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2) : "SQSUBR Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; SQSUBR Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svint32_t svqsubr[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2) : "SQSUBR Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "SQSUB Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "SQSUB Zresult.S, Zop2.S, Zop1.S"
    /// svint32_t svqsubr[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; SQSUBR Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; SQSUB Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<int> SubtractSaturateReversed(Vector<int> left, Vector<int> right);

    /// svint64_t svqsubr[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2) : "SQSUBR Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; SQSUBR Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svint64_t svqsubr[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2) : "SQSUBR Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "SQSUB Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "SQSUB Zresult.D, Zop2.D, Zop1.D"
    /// svint64_t svqsubr[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; SQSUBR Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; SQSUB Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<long> SubtractSaturateReversed(Vector<long> left, Vector<long> right);

    /// svuint8_t svqsubr[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2) : "UQSUBR Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "MOVPRFX Zresult, Zop1; UQSUBR Zresult.B, Pg/M, Zresult.B, Zop2.B"
    /// svuint8_t svqsubr[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2) : "UQSUBR Ztied1.B, Pg/M, Ztied1.B, Zop2.B" or "UQSUB Ztied2.B, Pg/M, Ztied2.B, Zop1.B" or "UQSUB Zresult.B, Zop2.B, Zop1.B"
    /// svuint8_t svqsubr[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2) : "MOVPRFX Zresult.B, Pg/Z, Zop1.B; UQSUBR Zresult.B, Pg/M, Zresult.B, Zop2.B" or "MOVPRFX Zresult.B, Pg/Z, Zop2.B; UQSUB Zresult.B, Pg/M, Zresult.B, Zop1.B"
  public static unsafe Vector<byte> SubtractSaturateReversed(Vector<byte> left, Vector<byte> right);

    /// svuint16_t svqsubr[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2) : "UQSUBR Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; UQSUBR Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svuint16_t svqsubr[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2) : "UQSUBR Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "UQSUB Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "UQSUB Zresult.H, Zop2.H, Zop1.H"
    /// svuint16_t svqsubr[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; UQSUBR Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; UQSUB Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<ushort> SubtractSaturateReversed(Vector<ushort> left, Vector<ushort> right);

    /// svuint32_t svqsubr[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2) : "UQSUBR Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "MOVPRFX Zresult, Zop1; UQSUBR Zresult.S, Pg/M, Zresult.S, Zop2.S"
    /// svuint32_t svqsubr[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2) : "UQSUBR Ztied1.S, Pg/M, Ztied1.S, Zop2.S" or "UQSUB Ztied2.S, Pg/M, Ztied2.S, Zop1.S" or "UQSUB Zresult.S, Zop2.S, Zop1.S"
    /// svuint32_t svqsubr[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2) : "MOVPRFX Zresult.S, Pg/Z, Zop1.S; UQSUBR Zresult.S, Pg/M, Zresult.S, Zop2.S" or "MOVPRFX Zresult.S, Pg/Z, Zop2.S; UQSUB Zresult.S, Pg/M, Zresult.S, Zop1.S"
  public static unsafe Vector<uint> SubtractSaturateReversed(Vector<uint> left, Vector<uint> right);

    /// svuint64_t svqsubr[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2) : "UQSUBR Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "MOVPRFX Zresult, Zop1; UQSUBR Zresult.D, Pg/M, Zresult.D, Zop2.D"
    /// svuint64_t svqsubr[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2) : "UQSUBR Ztied1.D, Pg/M, Ztied1.D, Zop2.D" or "UQSUB Ztied2.D, Pg/M, Ztied2.D, Zop1.D" or "UQSUB Zresult.D, Zop2.D, Zop1.D"
    /// svuint64_t svqsubr[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2) : "MOVPRFX Zresult.D, Pg/Z, Zop1.D; UQSUBR Zresult.D, Pg/M, Zresult.D, Zop2.D" or "MOVPRFX Zresult.D, Pg/Z, Zop2.D; UQSUB Zresult.D, Pg/M, Zresult.D, Zop1.D"
  public static unsafe Vector<ulong> SubtractSaturateReversed(Vector<ulong> left, Vector<ulong> right);


    /// SubtractWideLower : Subtract wide (bottom)

    /// svint16_t svsubwb[_s16](svint16_t op1, svint8_t op2) : "SSUBWB Zresult.H, Zop1.H, Zop2.B"
  public static unsafe Vector<short> SubtractWideLower(Vector<short> left, Vector<sbyte> right);

    /// svint32_t svsubwb[_s32](svint32_t op1, svint16_t op2) : "SSUBWB Zresult.S, Zop1.S, Zop2.H"
  public static unsafe Vector<int> SubtractWideLower(Vector<int> left, Vector<short> right);

    /// svint64_t svsubwb[_s64](svint64_t op1, svint32_t op2) : "SSUBWB Zresult.D, Zop1.D, Zop2.S"
  public static unsafe Vector<long> SubtractWideLower(Vector<long> left, Vector<int> right);

    /// svuint16_t svsubwb[_u16](svuint16_t op1, svuint8_t op2) : "USUBWB Zresult.H, Zop1.H, Zop2.B"
  public static unsafe Vector<ushort> SubtractWideLower(Vector<ushort> left, Vector<byte> right);

    /// svuint32_t svsubwb[_u32](svuint32_t op1, svuint16_t op2) : "USUBWB Zresult.S, Zop1.S, Zop2.H"
  public static unsafe Vector<uint> SubtractWideLower(Vector<uint> left, Vector<ushort> right);

    /// svuint64_t svsubwb[_u64](svuint64_t op1, svuint32_t op2) : "USUBWB Zresult.D, Zop1.D, Zop2.S"
  public static unsafe Vector<ulong> SubtractWideLower(Vector<ulong> left, Vector<uint> right);


    /// SubtractWideUpper : Subtract wide (top)

    /// svint16_t svsubwt[_s16](svint16_t op1, svint8_t op2) : "SSUBWT Zresult.H, Zop1.H, Zop2.B"
  public static unsafe Vector<short> SubtractWideUpper(Vector<short> left, Vector<sbyte> right);

    /// svint32_t svsubwt[_s32](svint32_t op1, svint16_t op2) : "SSUBWT Zresult.S, Zop1.S, Zop2.H"
  public static unsafe Vector<int> SubtractWideUpper(Vector<int> left, Vector<short> right);

    /// svint64_t svsubwt[_s64](svint64_t op1, svint32_t op2) : "SSUBWT Zresult.D, Zop1.D, Zop2.S"
  public static unsafe Vector<long> SubtractWideUpper(Vector<long> left, Vector<int> right);

    /// svuint16_t svsubwt[_u16](svuint16_t op1, svuint8_t op2) : "USUBWT Zresult.H, Zop1.H, Zop2.B"
  public static unsafe Vector<ushort> SubtractWideUpper(Vector<ushort> left, Vector<byte> right);

    /// svuint32_t svsubwt[_u32](svuint32_t op1, svuint16_t op2) : "USUBWT Zresult.S, Zop1.S, Zop2.H"
  public static unsafe Vector<uint> SubtractWideUpper(Vector<uint> left, Vector<ushort> right);

    /// svuint64_t svsubwt[_u64](svuint64_t op1, svuint32_t op2) : "USUBWT Zresult.D, Zop1.D, Zop2.S"
  public static unsafe Vector<ulong> SubtractWideUpper(Vector<ulong> left, Vector<uint> right);


    /// SubtractWideningLower : Subtract long (bottom)

    /// svint16_t svsublb[_s16](svint8_t op1, svint8_t op2) : "SSUBLB Zresult.H, Zop1.B, Zop2.B"
  public static unsafe Vector<short> SubtractWideningLower(Vector<sbyte> left, Vector<sbyte> right);

    /// svint32_t svsublb[_s32](svint16_t op1, svint16_t op2) : "SSUBLB Zresult.S, Zop1.H, Zop2.H"
  public static unsafe Vector<int> SubtractWideningLower(Vector<short> left, Vector<short> right);

    /// svint64_t svsublb[_s64](svint32_t op1, svint32_t op2) : "SSUBLB Zresult.D, Zop1.S, Zop2.S"
  public static unsafe Vector<long> SubtractWideningLower(Vector<int> left, Vector<int> right);

    /// svuint16_t svsublb[_u16](svuint8_t op1, svuint8_t op2) : "USUBLB Zresult.H, Zop1.B, Zop2.B"
  public static unsafe Vector<ushort> SubtractWideningLower(Vector<byte> left, Vector<byte> right);

    /// svuint32_t svsublb[_u32](svuint16_t op1, svuint16_t op2) : "USUBLB Zresult.S, Zop1.H, Zop2.H"
  public static unsafe Vector<uint> SubtractWideningLower(Vector<ushort> left, Vector<ushort> right);

    /// svuint64_t svsublb[_u64](svuint32_t op1, svuint32_t op2) : "USUBLB Zresult.D, Zop1.S, Zop2.S"
  public static unsafe Vector<ulong> SubtractWideningLower(Vector<uint> left, Vector<uint> right);


    /// SubtractWideningLowerUpper : Subtract long (bottom - top)

    /// svint16_t svsublbt[_s16](svint8_t op1, svint8_t op2) : "SSUBLBT Zresult.H, Zop1.B, Zop2.B"
  public static unsafe Vector<short> SubtractWideningLowerUpper(Vector<sbyte> left, Vector<sbyte> right);

    /// svint32_t svsublbt[_s32](svint16_t op1, svint16_t op2) : "SSUBLBT Zresult.S, Zop1.H, Zop2.H"
  public static unsafe Vector<int> SubtractWideningLowerUpper(Vector<short> left, Vector<short> right);

    /// svint64_t svsublbt[_s64](svint32_t op1, svint32_t op2) : "SSUBLBT Zresult.D, Zop1.S, Zop2.S"
  public static unsafe Vector<long> SubtractWideningLowerUpper(Vector<int> left, Vector<int> right);


    /// SubtractWideningUpper : Subtract long (top)

    /// svint16_t svsublt[_s16](svint8_t op1, svint8_t op2) : "SSUBLT Zresult.H, Zop1.B, Zop2.B"
  public static unsafe Vector<short> SubtractWideningUpper(Vector<sbyte> left, Vector<sbyte> right);

    /// svint32_t svsublt[_s32](svint16_t op1, svint16_t op2) : "SSUBLT Zresult.S, Zop1.H, Zop2.H"
  public static unsafe Vector<int> SubtractWideningUpper(Vector<short> left, Vector<short> right);

    /// svint64_t svsublt[_s64](svint32_t op1, svint32_t op2) : "SSUBLT Zresult.D, Zop1.S, Zop2.S"
  public static unsafe Vector<long> SubtractWideningUpper(Vector<int> left, Vector<int> right);

    /// svuint16_t svsublt[_u16](svuint8_t op1, svuint8_t op2) : "USUBLT Zresult.H, Zop1.B, Zop2.B"
  public static unsafe Vector<ushort> SubtractWideningUpper(Vector<byte> left, Vector<byte> right);

    /// svuint32_t svsublt[_u32](svuint16_t op1, svuint16_t op2) : "USUBLT Zresult.S, Zop1.H, Zop2.H"
  public static unsafe Vector<uint> SubtractWideningUpper(Vector<ushort> left, Vector<ushort> right);

    /// svuint64_t svsublt[_u64](svuint32_t op1, svuint32_t op2) : "USUBLT Zresult.D, Zop1.S, Zop2.S"
  public static unsafe Vector<ulong> SubtractWideningUpper(Vector<uint> left, Vector<uint> right);


    /// SubtractWideningUpperLower : Subtract long (top - bottom)

    /// svint16_t svsubltb[_s16](svint8_t op1, svint8_t op2) : "SSUBLTB Zresult.H, Zop1.B, Zop2.B"
  public static unsafe Vector<short> SubtractWideningUpperLower(Vector<sbyte> left, Vector<sbyte> right);

    /// svint32_t svsubltb[_s32](svint16_t op1, svint16_t op2) : "SSUBLTB Zresult.S, Zop1.H, Zop2.H"
  public static unsafe Vector<int> SubtractWideningUpperLower(Vector<short> left, Vector<short> right);

    /// svint64_t svsubltb[_s64](svint32_t op1, svint32_t op2) : "SSUBLTB Zresult.D, Zop1.S, Zop2.S"
  public static unsafe Vector<long> SubtractWideningUpperLower(Vector<int> left, Vector<int> right);


    /// SubtractWithBorrowWideningLower : Subtract with borrow long (bottom)

    /// svuint32_t svsbclb[_u32](svuint32_t op1, svuint32_t op2, svuint32_t op3) : "SBCLB Ztied1.S, Zop2.S, Zop3.S" or "MOVPRFX Zresult, Zop1; SBCLB Zresult.S, Zop2.S, Zop3.S"
  public static unsafe Vector<uint> SubtractWithBorrowWideningLower(Vector<uint> op1, Vector<uint> op2, Vector<uint> op3);

    /// svuint64_t svsbclb[_u64](svuint64_t op1, svuint64_t op2, svuint64_t op3) : "SBCLB Ztied1.D, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; SBCLB Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<ulong> SubtractWithBorrowWideningLower(Vector<ulong> op1, Vector<ulong> op2, Vector<ulong> op3);


    /// SubtractWithBorrowWideningUpper : Subtract with borrow long (top)

    /// svuint32_t svsbclt[_u32](svuint32_t op1, svuint32_t op2, svuint32_t op3) : "SBCLT Ztied1.S, Zop2.S, Zop3.S" or "MOVPRFX Zresult, Zop1; SBCLT Zresult.S, Zop2.S, Zop3.S"
  public static unsafe Vector<uint> SubtractWithBorrowWideningUpper(Vector<uint> op1, Vector<uint> op2, Vector<uint> op3);

    /// svuint64_t svsbclt[_u64](svuint64_t op1, svuint64_t op2, svuint64_t op3) : "SBCLT Ztied1.D, Zop2.D, Zop3.D" or "MOVPRFX Zresult, Zop1; SBCLT Zresult.D, Zop2.D, Zop3.D"
  public static unsafe Vector<ulong> SubtractWithBorrowWideningUpper(Vector<ulong> op1, Vector<ulong> op2, Vector<ulong> op3);


  /// total method signatures: 412
  /// total method names:      70
}


  /// Rejected:
  ///   public static unsafe Vector<sbyte> AbsoluteDifferenceAdd(Vector<sbyte> addend, Vector<sbyte> left, sbyte right); // svaba[_n_s8]
  ///   public static unsafe Vector<short> AbsoluteDifferenceAdd(Vector<short> addend, Vector<short> left, short right); // svaba[_n_s16]
  ///   public static unsafe Vector<int> AbsoluteDifferenceAdd(Vector<int> addend, Vector<int> left, int right); // svaba[_n_s32]
  ///   public static unsafe Vector<long> AbsoluteDifferenceAdd(Vector<long> addend, Vector<long> left, long right); // svaba[_n_s64]
  ///   public static unsafe Vector<byte> AbsoluteDifferenceAdd(Vector<byte> addend, Vector<byte> left, byte right); // svaba[_n_u8]
  ///   public static unsafe Vector<ushort> AbsoluteDifferenceAdd(Vector<ushort> addend, Vector<ushort> left, ushort right); // svaba[_n_u16]
  ///   public static unsafe Vector<uint> AbsoluteDifferenceAdd(Vector<uint> addend, Vector<uint> left, uint right); // svaba[_n_u32]
  ///   public static unsafe Vector<ulong> AbsoluteDifferenceAdd(Vector<ulong> addend, Vector<ulong> left, ulong right); // svaba[_n_u64]
  ///   public static unsafe Vector<short> AbsoluteDifferenceAddWideningLower(Vector<short> addend, Vector<sbyte> left, sbyte right); // svabalb[_n_s16]
  ///   public static unsafe Vector<int> AbsoluteDifferenceAddWideningLower(Vector<int> addend, Vector<short> left, short right); // svabalb[_n_s32]
  ///   public static unsafe Vector<long> AbsoluteDifferenceAddWideningLower(Vector<long> addend, Vector<int> left, int right); // svabalb[_n_s64]
  ///   public static unsafe Vector<ushort> AbsoluteDifferenceAddWideningLower(Vector<ushort> addend, Vector<byte> left, byte right); // svabalb[_n_u16]
  ///   public static unsafe Vector<uint> AbsoluteDifferenceAddWideningLower(Vector<uint> addend, Vector<ushort> left, ushort right); // svabalb[_n_u32]
  ///   public static unsafe Vector<ulong> AbsoluteDifferenceAddWideningLower(Vector<ulong> addend, Vector<uint> left, uint right); // svabalb[_n_u64]
  ///   public static unsafe Vector<short> AbsoluteDifferenceAddWideningUpper(Vector<short> addend, Vector<sbyte> left, sbyte right); // svabalt[_n_s16]
  ///   public static unsafe Vector<int> AbsoluteDifferenceAddWideningUpper(Vector<int> addend, Vector<short> left, short right); // svabalt[_n_s32]
  ///   public static unsafe Vector<long> AbsoluteDifferenceAddWideningUpper(Vector<long> addend, Vector<int> left, int right); // svabalt[_n_s64]
  ///   public static unsafe Vector<ushort> AbsoluteDifferenceAddWideningUpper(Vector<ushort> addend, Vector<byte> left, byte right); // svabalt[_n_u16]
  ///   public static unsafe Vector<uint> AbsoluteDifferenceAddWideningUpper(Vector<uint> addend, Vector<ushort> left, ushort right); // svabalt[_n_u32]
  ///   public static unsafe Vector<ulong> AbsoluteDifferenceAddWideningUpper(Vector<ulong> addend, Vector<uint> left, uint right); // svabalt[_n_u64]
  ///   public static unsafe Vector<short> AbsoluteDifferenceWideningLower(Vector<sbyte> left, sbyte right); // svabdlb[_n_s16]
  ///   public static unsafe Vector<int> AbsoluteDifferenceWideningLower(Vector<short> left, short right); // svabdlb[_n_s32]
  ///   public static unsafe Vector<long> AbsoluteDifferenceWideningLower(Vector<int> left, int right); // svabdlb[_n_s64]
  ///   public static unsafe Vector<ushort> AbsoluteDifferenceWideningLower(Vector<byte> left, byte right); // svabdlb[_n_u16]
  ///   public static unsafe Vector<uint> AbsoluteDifferenceWideningLower(Vector<ushort> left, ushort right); // svabdlb[_n_u32]
  ///   public static unsafe Vector<ulong> AbsoluteDifferenceWideningLower(Vector<uint> left, uint right); // svabdlb[_n_u64]
  ///   public static unsafe Vector<short> AbsoluteDifferenceWideningUpper(Vector<sbyte> left, sbyte right); // svabdlt[_n_s16]
  ///   public static unsafe Vector<int> AbsoluteDifferenceWideningUpper(Vector<short> left, short right); // svabdlt[_n_s32]
  ///   public static unsafe Vector<long> AbsoluteDifferenceWideningUpper(Vector<int> left, int right); // svabdlt[_n_s64]
  ///   public static unsafe Vector<ushort> AbsoluteDifferenceWideningUpper(Vector<byte> left, byte right); // svabdlt[_n_u16]
  ///   public static unsafe Vector<uint> AbsoluteDifferenceWideningUpper(Vector<ushort> left, ushort right); // svabdlt[_n_u32]
  ///   public static unsafe Vector<ulong> AbsoluteDifferenceWideningUpper(Vector<uint> left, uint right); // svabdlt[_n_u64]
  ///   public static unsafe Vector<uint> AddCarryWideningLower(Vector<uint> op1, Vector<uint> op2, uint op3); // svadclb[_n_u32]
  ///   public static unsafe Vector<ulong> AddCarryWideningLower(Vector<ulong> op1, Vector<ulong> op2, ulong op3); // svadclb[_n_u64]
  ///   public static unsafe Vector<uint> AddCarryWideningUpper(Vector<uint> op1, Vector<uint> op2, uint op3); // svadclt[_n_u32]
  ///   public static unsafe Vector<ulong> AddCarryWideningUpper(Vector<ulong> op1, Vector<ulong> op2, ulong op3); // svadclt[_n_u64]
  ///   public static unsafe Vector<sbyte> AddHighNarowingLower(Vector<short> left, short right); // svaddhnb[_n_s16]
  ///   public static unsafe Vector<short> AddHighNarowingLower(Vector<int> left, int right); // svaddhnb[_n_s32]
  ///   public static unsafe Vector<int> AddHighNarowingLower(Vector<long> left, long right); // svaddhnb[_n_s64]
  ///   public static unsafe Vector<byte> AddHighNarowingLower(Vector<ushort> left, ushort right); // svaddhnb[_n_u16]
  ///   public static unsafe Vector<ushort> AddHighNarowingLower(Vector<uint> left, uint right); // svaddhnb[_n_u32]
  ///   public static unsafe Vector<uint> AddHighNarowingLower(Vector<ulong> left, ulong right); // svaddhnb[_n_u64]
  ///   public static unsafe Vector<sbyte> AddHighNarowingUpper(Vector<sbyte> even, Vector<short> left, short right); // svaddhnt[_n_s16]
  ///   public static unsafe Vector<short> AddHighNarowingUpper(Vector<short> even, Vector<int> left, int right); // svaddhnt[_n_s32]
  ///   public static unsafe Vector<int> AddHighNarowingUpper(Vector<int> even, Vector<long> left, long right); // svaddhnt[_n_s64]
  ///   public static unsafe Vector<byte> AddHighNarowingUpper(Vector<byte> even, Vector<ushort> left, ushort right); // svaddhnt[_n_u16]
  ///   public static unsafe Vector<ushort> AddHighNarowingUpper(Vector<ushort> even, Vector<uint> left, uint right); // svaddhnt[_n_u32]
  ///   public static unsafe Vector<uint> AddHighNarowingUpper(Vector<uint> even, Vector<ulong> left, ulong right); // svaddhnt[_n_u64]
  ///   public static unsafe Vector<sbyte> AddSaturate(Vector<sbyte> left, sbyte right); // svqadd[_n_s8]_m or svqadd[_n_s8]_x or svqadd[_n_s8]_z
  ///   public static unsafe Vector<short> AddSaturate(Vector<short> left, short right); // svqadd[_n_s16]_m or svqadd[_n_s16]_x or svqadd[_n_s16]_z
  ///   public static unsafe Vector<int> AddSaturate(Vector<int> left, int right); // svqadd[_n_s32]_m or svqadd[_n_s32]_x or svqadd[_n_s32]_z
  ///   public static unsafe Vector<long> AddSaturate(Vector<long> left, long right); // svqadd[_n_s64]_m or svqadd[_n_s64]_x or svqadd[_n_s64]_z
  ///   public static unsafe Vector<byte> AddSaturate(Vector<byte> left, byte right); // svqadd[_n_u8]_m or svqadd[_n_u8]_x or svqadd[_n_u8]_z
  ///   public static unsafe Vector<ushort> AddSaturate(Vector<ushort> left, ushort right); // svqadd[_n_u16]_m or svqadd[_n_u16]_x or svqadd[_n_u16]_z
  ///   public static unsafe Vector<uint> AddSaturate(Vector<uint> left, uint right); // svqadd[_n_u32]_m or svqadd[_n_u32]_x or svqadd[_n_u32]_z
  ///   public static unsafe Vector<ulong> AddSaturate(Vector<ulong> left, ulong right); // svqadd[_n_u64]_m or svqadd[_n_u64]_x or svqadd[_n_u64]_z
  ///   public static unsafe Vector<byte> AddSaturateWithSignedAddend(Vector<byte> left, sbyte right); // svsqadd[_n_u8]_m or svsqadd[_n_u8]_x or svsqadd[_n_u8]_z
  ///   public static unsafe Vector<ushort> AddSaturateWithSignedAddend(Vector<ushort> left, short right); // svsqadd[_n_u16]_m or svsqadd[_n_u16]_x or svsqadd[_n_u16]_z
  ///   public static unsafe Vector<uint> AddSaturateWithSignedAddend(Vector<uint> left, int right); // svsqadd[_n_u32]_m or svsqadd[_n_u32]_x or svsqadd[_n_u32]_z
  ///   public static unsafe Vector<ulong> AddSaturateWithSignedAddend(Vector<ulong> left, long right); // svsqadd[_n_u64]_m or svsqadd[_n_u64]_x or svsqadd[_n_u64]_z
  ///   public static unsafe Vector<sbyte> AddSaturateWithUnsignedAddend(Vector<sbyte> left, byte right); // svuqadd[_n_s8]_m or svuqadd[_n_s8]_x or svuqadd[_n_s8]_z
  ///   public static unsafe Vector<short> AddSaturateWithUnsignedAddend(Vector<short> left, ushort right); // svuqadd[_n_s16]_m or svuqadd[_n_s16]_x or svuqadd[_n_s16]_z
  ///   public static unsafe Vector<int> AddSaturateWithUnsignedAddend(Vector<int> left, uint right); // svuqadd[_n_s32]_m or svuqadd[_n_s32]_x or svuqadd[_n_s32]_z
  ///   public static unsafe Vector<long> AddSaturateWithUnsignedAddend(Vector<long> left, ulong right); // svuqadd[_n_s64]_m or svuqadd[_n_s64]_x or svuqadd[_n_s64]_z
  ///   public static unsafe Vector<short> AddWideLower(Vector<short> left, sbyte right); // svaddwb[_n_s16]
  ///   public static unsafe Vector<int> AddWideLower(Vector<int> left, short right); // svaddwb[_n_s32]
  ///   public static unsafe Vector<long> AddWideLower(Vector<long> left, int right); // svaddwb[_n_s64]
  ///   public static unsafe Vector<ushort> AddWideLower(Vector<ushort> left, byte right); // svaddwb[_n_u16]
  ///   public static unsafe Vector<uint> AddWideLower(Vector<uint> left, ushort right); // svaddwb[_n_u32]
  ///   public static unsafe Vector<ulong> AddWideLower(Vector<ulong> left, uint right); // svaddwb[_n_u64]
  ///   public static unsafe Vector<short> AddWideUpper(Vector<short> left, sbyte right); // svaddwt[_n_s16]
  ///   public static unsafe Vector<int> AddWideUpper(Vector<int> left, short right); // svaddwt[_n_s32]
  ///   public static unsafe Vector<long> AddWideUpper(Vector<long> left, int right); // svaddwt[_n_s64]
  ///   public static unsafe Vector<ushort> AddWideUpper(Vector<ushort> left, byte right); // svaddwt[_n_u16]
  ///   public static unsafe Vector<uint> AddWideUpper(Vector<uint> left, ushort right); // svaddwt[_n_u32]
  ///   public static unsafe Vector<ulong> AddWideUpper(Vector<ulong> left, uint right); // svaddwt[_n_u64]
  ///   public static unsafe Vector<short> AddWideningLower(Vector<sbyte> left, sbyte right); // svaddlb[_n_s16]
  ///   public static unsafe Vector<int> AddWideningLower(Vector<short> left, short right); // svaddlb[_n_s32]
  ///   public static unsafe Vector<long> AddWideningLower(Vector<int> left, int right); // svaddlb[_n_s64]
  ///   public static unsafe Vector<ushort> AddWideningLower(Vector<byte> left, byte right); // svaddlb[_n_u16]
  ///   public static unsafe Vector<uint> AddWideningLower(Vector<ushort> left, ushort right); // svaddlb[_n_u32]
  ///   public static unsafe Vector<ulong> AddWideningLower(Vector<uint> left, uint right); // svaddlb[_n_u64]
  ///   public static unsafe Vector<short> AddWideningLowerUpper(Vector<sbyte> left, sbyte right); // svaddlbt[_n_s16]
  ///   public static unsafe Vector<int> AddWideningLowerUpper(Vector<short> left, short right); // svaddlbt[_n_s32]
  ///   public static unsafe Vector<long> AddWideningLowerUpper(Vector<int> left, int right); // svaddlbt[_n_s64]
  ///   public static unsafe Vector<short> AddWideningUpper(Vector<sbyte> left, sbyte right); // svaddlt[_n_s16]
  ///   public static unsafe Vector<int> AddWideningUpper(Vector<short> left, short right); // svaddlt[_n_s32]
  ///   public static unsafe Vector<long> AddWideningUpper(Vector<int> left, int right); // svaddlt[_n_s64]
  ///   public static unsafe Vector<ushort> AddWideningUpper(Vector<byte> left, byte right); // svaddlt[_n_u16]
  ///   public static unsafe Vector<uint> AddWideningUpper(Vector<ushort> left, ushort right); // svaddlt[_n_u32]
  ///   public static unsafe Vector<ulong> AddWideningUpper(Vector<uint> left, uint right); // svaddlt[_n_u64]
  ///   public static unsafe Vector<sbyte> HalvingAdd(Vector<sbyte> left, sbyte right); // svhadd[_n_s8]_m or svhadd[_n_s8]_x or svhadd[_n_s8]_z
  ///   public static unsafe Vector<short> HalvingAdd(Vector<short> left, short right); // svhadd[_n_s16]_m or svhadd[_n_s16]_x or svhadd[_n_s16]_z
  ///   public static unsafe Vector<int> HalvingAdd(Vector<int> left, int right); // svhadd[_n_s32]_m or svhadd[_n_s32]_x or svhadd[_n_s32]_z
  ///   public static unsafe Vector<long> HalvingAdd(Vector<long> left, long right); // svhadd[_n_s64]_m or svhadd[_n_s64]_x or svhadd[_n_s64]_z
  ///   public static unsafe Vector<byte> HalvingAdd(Vector<byte> left, byte right); // svhadd[_n_u8]_m or svhadd[_n_u8]_x or svhadd[_n_u8]_z
  ///   public static unsafe Vector<ushort> HalvingAdd(Vector<ushort> left, ushort right); // svhadd[_n_u16]_m or svhadd[_n_u16]_x or svhadd[_n_u16]_z
  ///   public static unsafe Vector<uint> HalvingAdd(Vector<uint> left, uint right); // svhadd[_n_u32]_m or svhadd[_n_u32]_x or svhadd[_n_u32]_z
  ///   public static unsafe Vector<ulong> HalvingAdd(Vector<ulong> left, ulong right); // svhadd[_n_u64]_m or svhadd[_n_u64]_x or svhadd[_n_u64]_z
  ///   public static unsafe Vector<sbyte> HalvingSubtract(Vector<sbyte> left, sbyte right); // svhsub[_n_s8]_m or svhsub[_n_s8]_x or svhsub[_n_s8]_z
  ///   public static unsafe Vector<short> HalvingSubtract(Vector<short> left, short right); // svhsub[_n_s16]_m or svhsub[_n_s16]_x or svhsub[_n_s16]_z
  ///   public static unsafe Vector<int> HalvingSubtract(Vector<int> left, int right); // svhsub[_n_s32]_m or svhsub[_n_s32]_x or svhsub[_n_s32]_z
  ///   public static unsafe Vector<long> HalvingSubtract(Vector<long> left, long right); // svhsub[_n_s64]_m or svhsub[_n_s64]_x or svhsub[_n_s64]_z
  ///   public static unsafe Vector<byte> HalvingSubtract(Vector<byte> left, byte right); // svhsub[_n_u8]_m or svhsub[_n_u8]_x or svhsub[_n_u8]_z
  ///   public static unsafe Vector<ushort> HalvingSubtract(Vector<ushort> left, ushort right); // svhsub[_n_u16]_m or svhsub[_n_u16]_x or svhsub[_n_u16]_z
  ///   public static unsafe Vector<uint> HalvingSubtract(Vector<uint> left, uint right); // svhsub[_n_u32]_m or svhsub[_n_u32]_x or svhsub[_n_u32]_z
  ///   public static unsafe Vector<ulong> HalvingSubtract(Vector<ulong> left, ulong right); // svhsub[_n_u64]_m or svhsub[_n_u64]_x or svhsub[_n_u64]_z
  ///   public static unsafe Vector<sbyte> HalvingSubtractReversed(Vector<sbyte> left, sbyte right); // svhsubr[_n_s8]_m or svhsubr[_n_s8]_x or svhsubr[_n_s8]_z
  ///   public static unsafe Vector<short> HalvingSubtractReversed(Vector<short> left, short right); // svhsubr[_n_s16]_m or svhsubr[_n_s16]_x or svhsubr[_n_s16]_z
  ///   public static unsafe Vector<int> HalvingSubtractReversed(Vector<int> left, int right); // svhsubr[_n_s32]_m or svhsubr[_n_s32]_x or svhsubr[_n_s32]_z
  ///   public static unsafe Vector<long> HalvingSubtractReversed(Vector<long> left, long right); // svhsubr[_n_s64]_m or svhsubr[_n_s64]_x or svhsubr[_n_s64]_z
  ///   public static unsafe Vector<byte> HalvingSubtractReversed(Vector<byte> left, byte right); // svhsubr[_n_u8]_m or svhsubr[_n_u8]_x or svhsubr[_n_u8]_z
  ///   public static unsafe Vector<ushort> HalvingSubtractReversed(Vector<ushort> left, ushort right); // svhsubr[_n_u16]_m or svhsubr[_n_u16]_x or svhsubr[_n_u16]_z
  ///   public static unsafe Vector<uint> HalvingSubtractReversed(Vector<uint> left, uint right); // svhsubr[_n_u32]_m or svhsubr[_n_u32]_x or svhsubr[_n_u32]_z
  ///   public static unsafe Vector<ulong> HalvingSubtractReversed(Vector<ulong> left, ulong right); // svhsubr[_n_u64]_m or svhsubr[_n_u64]_x or svhsubr[_n_u64]_z
  ///   public static unsafe Vector<short> MultiplyAddWideningLower(Vector<short> op1, Vector<sbyte> op2, sbyte op3); // svmlalb[_n_s16]
  ///   public static unsafe Vector<int> MultiplyAddWideningLower(Vector<int> op1, Vector<short> op2, short op3); // svmlalb[_n_s32]
  ///   public static unsafe Vector<long> MultiplyAddWideningLower(Vector<long> op1, Vector<int> op2, int op3); // svmlalb[_n_s64]
  ///   public static unsafe Vector<ushort> MultiplyAddWideningLower(Vector<ushort> op1, Vector<byte> op2, byte op3); // svmlalb[_n_u16]
  ///   public static unsafe Vector<uint> MultiplyAddWideningLower(Vector<uint> op1, Vector<ushort> op2, ushort op3); // svmlalb[_n_u32]
  ///   public static unsafe Vector<ulong> MultiplyAddWideningLower(Vector<ulong> op1, Vector<uint> op2, uint op3); // svmlalb[_n_u64]
  ///   public static unsafe Vector<short> MultiplyAddWideningUpper(Vector<short> op1, Vector<sbyte> op2, sbyte op3); // svmlalt[_n_s16]
  ///   public static unsafe Vector<int> MultiplyAddWideningUpper(Vector<int> op1, Vector<short> op2, short op3); // svmlalt[_n_s32]
  ///   public static unsafe Vector<long> MultiplyAddWideningUpper(Vector<long> op1, Vector<int> op2, int op3); // svmlalt[_n_s64]
  ///   public static unsafe Vector<ushort> MultiplyAddWideningUpper(Vector<ushort> op1, Vector<byte> op2, byte op3); // svmlalt[_n_u16]
  ///   public static unsafe Vector<uint> MultiplyAddWideningUpper(Vector<uint> op1, Vector<ushort> op2, ushort op3); // svmlalt[_n_u32]
  ///   public static unsafe Vector<ulong> MultiplyAddWideningUpper(Vector<ulong> op1, Vector<uint> op2, uint op3); // svmlalt[_n_u64]
  ///   public static unsafe Vector<short> MultiplySubtractWideningLower(Vector<short> op1, Vector<sbyte> op2, sbyte op3); // svmlslb[_n_s16]
  ///   public static unsafe Vector<int> MultiplySubtractWideningLower(Vector<int> op1, Vector<short> op2, short op3); // svmlslb[_n_s32]
  ///   public static unsafe Vector<long> MultiplySubtractWideningLower(Vector<long> op1, Vector<int> op2, int op3); // svmlslb[_n_s64]
  ///   public static unsafe Vector<ushort> MultiplySubtractWideningLower(Vector<ushort> op1, Vector<byte> op2, byte op3); // svmlslb[_n_u16]
  ///   public static unsafe Vector<uint> MultiplySubtractWideningLower(Vector<uint> op1, Vector<ushort> op2, ushort op3); // svmlslb[_n_u32]
  ///   public static unsafe Vector<ulong> MultiplySubtractWideningLower(Vector<ulong> op1, Vector<uint> op2, uint op3); // svmlslb[_n_u64]
  ///   public static unsafe Vector<short> MultiplySubtractWideningUpper(Vector<short> op1, Vector<sbyte> op2, sbyte op3); // svmlslt[_n_s16]
  ///   public static unsafe Vector<int> MultiplySubtractWideningUpper(Vector<int> op1, Vector<short> op2, short op3); // svmlslt[_n_s32]
  ///   public static unsafe Vector<long> MultiplySubtractWideningUpper(Vector<long> op1, Vector<int> op2, int op3); // svmlslt[_n_s64]
  ///   public static unsafe Vector<ushort> MultiplySubtractWideningUpper(Vector<ushort> op1, Vector<byte> op2, byte op3); // svmlslt[_n_u16]
  ///   public static unsafe Vector<uint> MultiplySubtractWideningUpper(Vector<uint> op1, Vector<ushort> op2, ushort op3); // svmlslt[_n_u32]
  ///   public static unsafe Vector<ulong> MultiplySubtractWideningUpper(Vector<ulong> op1, Vector<uint> op2, uint op3); // svmlslt[_n_u64]
  ///   public static unsafe Vector<short> MultiplyWideningLower(Vector<sbyte> left, sbyte right); // svmullb[_n_s16]
  ///   public static unsafe Vector<int> MultiplyWideningLower(Vector<short> left, short right); // svmullb[_n_s32]
  ///   public static unsafe Vector<long> MultiplyWideningLower(Vector<int> left, int right); // svmullb[_n_s64]
  ///   public static unsafe Vector<ushort> MultiplyWideningLower(Vector<byte> left, byte right); // svmullb[_n_u16]
  ///   public static unsafe Vector<uint> MultiplyWideningLower(Vector<ushort> left, ushort right); // svmullb[_n_u32]
  ///   public static unsafe Vector<ulong> MultiplyWideningLower(Vector<uint> left, uint right); // svmullb[_n_u64]
  ///   public static unsafe Vector<short> MultiplyWideningUpper(Vector<sbyte> left, sbyte right); // svmullt[_n_s16]
  ///   public static unsafe Vector<int> MultiplyWideningUpper(Vector<short> left, short right); // svmullt[_n_s32]
  ///   public static unsafe Vector<long> MultiplyWideningUpper(Vector<int> left, int right); // svmullt[_n_s64]
  ///   public static unsafe Vector<ushort> MultiplyWideningUpper(Vector<byte> left, byte right); // svmullt[_n_u16]
  ///   public static unsafe Vector<uint> MultiplyWideningUpper(Vector<ushort> left, ushort right); // svmullt[_n_u32]
  ///   public static unsafe Vector<ulong> MultiplyWideningUpper(Vector<uint> left, uint right); // svmullt[_n_u64]
  ///   public static unsafe Vector<byte> PolynomialMultiply(Vector<byte> left, byte right); // svpmul[_n_u8]
  ///   public static unsafe Vector<ushort> PolynomialMultiplyWideningLower(Vector<byte> left, byte right); // svpmullb[_n_u16]
  ///   public static unsafe Vector<ulong> PolynomialMultiplyWideningLower(Vector<uint> left, uint right); // svpmullb[_n_u64]
  ///   public static unsafe Vector<byte> PolynomialMultiplyWideningLower(Vector<byte> left, byte right); // svpmullb_pair[_n_u8]
  ///   public static unsafe Vector<uint> PolynomialMultiplyWideningLower(Vector<uint> left, uint right); // svpmullb_pair[_n_u32]
  ///   public static unsafe Vector<ushort> PolynomialMultiplyWideningUpper(Vector<byte> left, byte right); // svpmullt[_n_u16]
  ///   public static unsafe Vector<ulong> PolynomialMultiplyWideningUpper(Vector<uint> left, uint right); // svpmullt[_n_u64]
  ///   public static unsafe Vector<byte> PolynomialMultiplyWideningUpper(Vector<byte> left, byte right); // svpmullt_pair[_n_u8]
  ///   public static unsafe Vector<uint> PolynomialMultiplyWideningUpper(Vector<uint> left, uint right); // svpmullt_pair[_n_u32]
  ///   public static unsafe Vector<sbyte> RoundingAddHighNarowingLower(Vector<short> left, short right); // svraddhnb[_n_s16]
  ///   public static unsafe Vector<short> RoundingAddHighNarowingLower(Vector<int> left, int right); // svraddhnb[_n_s32]
  ///   public static unsafe Vector<int> RoundingAddHighNarowingLower(Vector<long> left, long right); // svraddhnb[_n_s64]
  ///   public static unsafe Vector<byte> RoundingAddHighNarowingLower(Vector<ushort> left, ushort right); // svraddhnb[_n_u16]
  ///   public static unsafe Vector<ushort> RoundingAddHighNarowingLower(Vector<uint> left, uint right); // svraddhnb[_n_u32]
  ///   public static unsafe Vector<uint> RoundingAddHighNarowingLower(Vector<ulong> left, ulong right); // svraddhnb[_n_u64]
  ///   public static unsafe Vector<sbyte> RoundingAddHighNarowingUpper(Vector<sbyte> even, Vector<short> left, short right); // svraddhnt[_n_s16]
  ///   public static unsafe Vector<short> RoundingAddHighNarowingUpper(Vector<short> even, Vector<int> left, int right); // svraddhnt[_n_s32]
  ///   public static unsafe Vector<int> RoundingAddHighNarowingUpper(Vector<int> even, Vector<long> left, long right); // svraddhnt[_n_s64]
  ///   public static unsafe Vector<byte> RoundingAddHighNarowingUpper(Vector<byte> even, Vector<ushort> left, ushort right); // svraddhnt[_n_u16]
  ///   public static unsafe Vector<ushort> RoundingAddHighNarowingUpper(Vector<ushort> even, Vector<uint> left, uint right); // svraddhnt[_n_u32]
  ///   public static unsafe Vector<uint> RoundingAddHighNarowingUpper(Vector<uint> even, Vector<ulong> left, ulong right); // svraddhnt[_n_u64]
  ///   public static unsafe Vector<sbyte> RoundingHalvingAdd(Vector<sbyte> left, sbyte right); // svrhadd[_n_s8]_m or svrhadd[_n_s8]_x or svrhadd[_n_s8]_z
  ///   public static unsafe Vector<short> RoundingHalvingAdd(Vector<short> left, short right); // svrhadd[_n_s16]_m or svrhadd[_n_s16]_x or svrhadd[_n_s16]_z
  ///   public static unsafe Vector<int> RoundingHalvingAdd(Vector<int> left, int right); // svrhadd[_n_s32]_m or svrhadd[_n_s32]_x or svrhadd[_n_s32]_z
  ///   public static unsafe Vector<long> RoundingHalvingAdd(Vector<long> left, long right); // svrhadd[_n_s64]_m or svrhadd[_n_s64]_x or svrhadd[_n_s64]_z
  ///   public static unsafe Vector<byte> RoundingHalvingAdd(Vector<byte> left, byte right); // svrhadd[_n_u8]_m or svrhadd[_n_u8]_x or svrhadd[_n_u8]_z
  ///   public static unsafe Vector<ushort> RoundingHalvingAdd(Vector<ushort> left, ushort right); // svrhadd[_n_u16]_m or svrhadd[_n_u16]_x or svrhadd[_n_u16]_z
  ///   public static unsafe Vector<uint> RoundingHalvingAdd(Vector<uint> left, uint right); // svrhadd[_n_u32]_m or svrhadd[_n_u32]_x or svrhadd[_n_u32]_z
  ///   public static unsafe Vector<ulong> RoundingHalvingAdd(Vector<ulong> left, ulong right); // svrhadd[_n_u64]_m or svrhadd[_n_u64]_x or svrhadd[_n_u64]_z
  ///   public static unsafe Vector<sbyte> RoundingSubtractHighNarowingLower(Vector<short> left, short right); // svrsubhnb[_n_s16]
  ///   public static unsafe Vector<short> RoundingSubtractHighNarowingLower(Vector<int> left, int right); // svrsubhnb[_n_s32]
  ///   public static unsafe Vector<int> RoundingSubtractHighNarowingLower(Vector<long> left, long right); // svrsubhnb[_n_s64]
  ///   public static unsafe Vector<byte> RoundingSubtractHighNarowingLower(Vector<ushort> left, ushort right); // svrsubhnb[_n_u16]
  ///   public static unsafe Vector<ushort> RoundingSubtractHighNarowingLower(Vector<uint> left, uint right); // svrsubhnb[_n_u32]
  ///   public static unsafe Vector<uint> RoundingSubtractHighNarowingLower(Vector<ulong> left, ulong right); // svrsubhnb[_n_u64]
  ///   public static unsafe Vector<sbyte> RoundingSubtractHighNarowingUpper(Vector<sbyte> even, Vector<short> left, short right); // svrsubhnt[_n_s16]
  ///   public static unsafe Vector<short> RoundingSubtractHighNarowingUpper(Vector<short> even, Vector<int> left, int right); // svrsubhnt[_n_s32]
  ///   public static unsafe Vector<int> RoundingSubtractHighNarowingUpper(Vector<int> even, Vector<long> left, long right); // svrsubhnt[_n_s64]
  ///   public static unsafe Vector<byte> RoundingSubtractHighNarowingUpper(Vector<byte> even, Vector<ushort> left, ushort right); // svrsubhnt[_n_u16]
  ///   public static unsafe Vector<ushort> RoundingSubtractHighNarowingUpper(Vector<ushort> even, Vector<uint> left, uint right); // svrsubhnt[_n_u32]
  ///   public static unsafe Vector<uint> RoundingSubtractHighNarowingUpper(Vector<uint> even, Vector<ulong> left, ulong right); // svrsubhnt[_n_u64]
  ///   public static unsafe Vector<short> SaturatingDoublingMultiplyAddWideningLower(Vector<short> op1, Vector<sbyte> op2, sbyte op3); // svqdmlalb[_n_s16]
  ///   public static unsafe Vector<int> SaturatingDoublingMultiplyAddWideningLower(Vector<int> op1, Vector<short> op2, short op3); // svqdmlalb[_n_s32]
  ///   public static unsafe Vector<long> SaturatingDoublingMultiplyAddWideningLower(Vector<long> op1, Vector<int> op2, int op3); // svqdmlalb[_n_s64]
  ///   public static unsafe Vector<short> SaturatingDoublingMultiplyAddWideningLowerUpper(Vector<short> op1, Vector<sbyte> op2, sbyte op3); // svqdmlalbt[_n_s16]
  ///   public static unsafe Vector<int> SaturatingDoublingMultiplyAddWideningLowerUpper(Vector<int> op1, Vector<short> op2, short op3); // svqdmlalbt[_n_s32]
  ///   public static unsafe Vector<long> SaturatingDoublingMultiplyAddWideningLowerUpper(Vector<long> op1, Vector<int> op2, int op3); // svqdmlalbt[_n_s64]
  ///   public static unsafe Vector<short> SaturatingDoublingMultiplyAddWideningUpper(Vector<short> op1, Vector<sbyte> op2, sbyte op3); // svqdmlalt[_n_s16]
  ///   public static unsafe Vector<int> SaturatingDoublingMultiplyAddWideningUpper(Vector<int> op1, Vector<short> op2, short op3); // svqdmlalt[_n_s32]
  ///   public static unsafe Vector<long> SaturatingDoublingMultiplyAddWideningUpper(Vector<long> op1, Vector<int> op2, int op3); // svqdmlalt[_n_s64]
  ///   public static unsafe Vector<sbyte> SaturatingDoublingMultiplyHigh(Vector<sbyte> left, sbyte right); // svqdmulh[_n_s8]
  ///   public static unsafe Vector<short> SaturatingDoublingMultiplyHigh(Vector<short> left, short right); // svqdmulh[_n_s16]
  ///   public static unsafe Vector<int> SaturatingDoublingMultiplyHigh(Vector<int> left, int right); // svqdmulh[_n_s32]
  ///   public static unsafe Vector<long> SaturatingDoublingMultiplyHigh(Vector<long> left, long right); // svqdmulh[_n_s64]
  ///   public static unsafe Vector<short> SaturatingDoublingMultiplySubtractWideningLower(Vector<short> op1, Vector<sbyte> op2, sbyte op3); // svqdmlslb[_n_s16]
  ///   public static unsafe Vector<int> SaturatingDoublingMultiplySubtractWideningLower(Vector<int> op1, Vector<short> op2, short op3); // svqdmlslb[_n_s32]
  ///   public static unsafe Vector<long> SaturatingDoublingMultiplySubtractWideningLower(Vector<long> op1, Vector<int> op2, int op3); // svqdmlslb[_n_s64]
  ///   public static unsafe Vector<short> SaturatingDoublingMultiplySubtractWideningLowerUpper(Vector<short> op1, Vector<sbyte> op2, sbyte op3); // svqdmlslbt[_n_s16]
  ///   public static unsafe Vector<int> SaturatingDoublingMultiplySubtractWideningLowerUpper(Vector<int> op1, Vector<short> op2, short op3); // svqdmlslbt[_n_s32]
  ///   public static unsafe Vector<long> SaturatingDoublingMultiplySubtractWideningLowerUpper(Vector<long> op1, Vector<int> op2, int op3); // svqdmlslbt[_n_s64]
  ///   public static unsafe Vector<short> SaturatingDoublingMultiplySubtractWideningUpper(Vector<short> op1, Vector<sbyte> op2, sbyte op3); // svqdmlslt[_n_s16]
  ///   public static unsafe Vector<int> SaturatingDoublingMultiplySubtractWideningUpper(Vector<int> op1, Vector<short> op2, short op3); // svqdmlslt[_n_s32]
  ///   public static unsafe Vector<long> SaturatingDoublingMultiplySubtractWideningUpper(Vector<long> op1, Vector<int> op2, int op3); // svqdmlslt[_n_s64]
  ///   public static unsafe Vector<short> SaturatingDoublingMultiplyWideningLower(Vector<sbyte> left, sbyte right); // svqdmullb[_n_s16]
  ///   public static unsafe Vector<int> SaturatingDoublingMultiplyWideningLower(Vector<short> left, short right); // svqdmullb[_n_s32]
  ///   public static unsafe Vector<long> SaturatingDoublingMultiplyWideningLower(Vector<int> left, int right); // svqdmullb[_n_s64]
  ///   public static unsafe Vector<short> SaturatingDoublingMultiplyWideningUpper(Vector<sbyte> left, sbyte right); // svqdmullt[_n_s16]
  ///   public static unsafe Vector<int> SaturatingDoublingMultiplyWideningUpper(Vector<short> left, short right); // svqdmullt[_n_s32]
  ///   public static unsafe Vector<long> SaturatingDoublingMultiplyWideningUpper(Vector<int> left, int right); // svqdmullt[_n_s64]
  ///   public static unsafe Vector<sbyte> SaturatingRoundingDoublingMultiplyAddHigh(Vector<sbyte> op1, Vector<sbyte> op2, sbyte op3); // svqrdmlah[_n_s8]
  ///   public static unsafe Vector<short> SaturatingRoundingDoublingMultiplyAddHigh(Vector<short> op1, Vector<short> op2, short op3); // svqrdmlah[_n_s16]
  ///   public static unsafe Vector<int> SaturatingRoundingDoublingMultiplyAddHigh(Vector<int> op1, Vector<int> op2, int op3); // svqrdmlah[_n_s32]
  ///   public static unsafe Vector<long> SaturatingRoundingDoublingMultiplyAddHigh(Vector<long> op1, Vector<long> op2, long op3); // svqrdmlah[_n_s64]
  ///   public static unsafe Vector<sbyte> SaturatingRoundingDoublingMultiplyHigh(Vector<sbyte> left, sbyte right); // svqrdmulh[_n_s8]
  ///   public static unsafe Vector<short> SaturatingRoundingDoublingMultiplyHigh(Vector<short> left, short right); // svqrdmulh[_n_s16]
  ///   public static unsafe Vector<int> SaturatingRoundingDoublingMultiplyHigh(Vector<int> left, int right); // svqrdmulh[_n_s32]
  ///   public static unsafe Vector<long> SaturatingRoundingDoublingMultiplyHigh(Vector<long> left, long right); // svqrdmulh[_n_s64]
  ///   public static unsafe Vector<sbyte> SaturatingRoundingDoublingMultiplySubtractHigh(Vector<sbyte> op1, Vector<sbyte> op2, sbyte op3); // svqrdmlsh[_n_s8]
  ///   public static unsafe Vector<short> SaturatingRoundingDoublingMultiplySubtractHigh(Vector<short> op1, Vector<short> op2, short op3); // svqrdmlsh[_n_s16]
  ///   public static unsafe Vector<int> SaturatingRoundingDoublingMultiplySubtractHigh(Vector<int> op1, Vector<int> op2, int op3); // svqrdmlsh[_n_s32]
  ///   public static unsafe Vector<long> SaturatingRoundingDoublingMultiplySubtractHigh(Vector<long> op1, Vector<long> op2, long op3); // svqrdmlsh[_n_s64]
  ///   public static unsafe Vector<sbyte> SubtractHighNarowingLower(Vector<short> left, short right); // svsubhnb[_n_s16]
  ///   public static unsafe Vector<short> SubtractHighNarowingLower(Vector<int> left, int right); // svsubhnb[_n_s32]
  ///   public static unsafe Vector<int> SubtractHighNarowingLower(Vector<long> left, long right); // svsubhnb[_n_s64]
  ///   public static unsafe Vector<byte> SubtractHighNarowingLower(Vector<ushort> left, ushort right); // svsubhnb[_n_u16]
  ///   public static unsafe Vector<ushort> SubtractHighNarowingLower(Vector<uint> left, uint right); // svsubhnb[_n_u32]
  ///   public static unsafe Vector<uint> SubtractHighNarowingLower(Vector<ulong> left, ulong right); // svsubhnb[_n_u64]
  ///   public static unsafe Vector<sbyte> SubtractHighNarowingUpper(Vector<sbyte> even, Vector<short> left, short right); // svsubhnt[_n_s16]
  ///   public static unsafe Vector<short> SubtractHighNarowingUpper(Vector<short> even, Vector<int> left, int right); // svsubhnt[_n_s32]
  ///   public static unsafe Vector<int> SubtractHighNarowingUpper(Vector<int> even, Vector<long> left, long right); // svsubhnt[_n_s64]
  ///   public static unsafe Vector<byte> SubtractHighNarowingUpper(Vector<byte> even, Vector<ushort> left, ushort right); // svsubhnt[_n_u16]
  ///   public static unsafe Vector<ushort> SubtractHighNarowingUpper(Vector<ushort> even, Vector<uint> left, uint right); // svsubhnt[_n_u32]
  ///   public static unsafe Vector<uint> SubtractHighNarowingUpper(Vector<uint> even, Vector<ulong> left, ulong right); // svsubhnt[_n_u64]
  ///   public static unsafe Vector<sbyte> SubtractSaturate(Vector<sbyte> left, sbyte right); // svqsub[_n_s8]_m or svqsub[_n_s8]_x or svqsub[_n_s8]_z
  ///   public static unsafe Vector<short> SubtractSaturate(Vector<short> left, short right); // svqsub[_n_s16]_m or svqsub[_n_s16]_x or svqsub[_n_s16]_z
  ///   public static unsafe Vector<int> SubtractSaturate(Vector<int> left, int right); // svqsub[_n_s32]_m or svqsub[_n_s32]_x or svqsub[_n_s32]_z
  ///   public static unsafe Vector<long> SubtractSaturate(Vector<long> left, long right); // svqsub[_n_s64]_m or svqsub[_n_s64]_x or svqsub[_n_s64]_z
  ///   public static unsafe Vector<byte> SubtractSaturate(Vector<byte> left, byte right); // svqsub[_n_u8]_m or svqsub[_n_u8]_x or svqsub[_n_u8]_z
  ///   public static unsafe Vector<ushort> SubtractSaturate(Vector<ushort> left, ushort right); // svqsub[_n_u16]_m or svqsub[_n_u16]_x or svqsub[_n_u16]_z
  ///   public static unsafe Vector<uint> SubtractSaturate(Vector<uint> left, uint right); // svqsub[_n_u32]_m or svqsub[_n_u32]_x or svqsub[_n_u32]_z
  ///   public static unsafe Vector<ulong> SubtractSaturate(Vector<ulong> left, ulong right); // svqsub[_n_u64]_m or svqsub[_n_u64]_x or svqsub[_n_u64]_z
  ///   public static unsafe Vector<sbyte> SubtractSaturateReversed(Vector<sbyte> left, sbyte right); // svqsubr[_n_s8]_m or svqsubr[_n_s8]_x or svqsubr[_n_s8]_z
  ///   public static unsafe Vector<short> SubtractSaturateReversed(Vector<short> left, short right); // svqsubr[_n_s16]_m or svqsubr[_n_s16]_x or svqsubr[_n_s16]_z
  ///   public static unsafe Vector<int> SubtractSaturateReversed(Vector<int> left, int right); // svqsubr[_n_s32]_m or svqsubr[_n_s32]_x or svqsubr[_n_s32]_z
  ///   public static unsafe Vector<long> SubtractSaturateReversed(Vector<long> left, long right); // svqsubr[_n_s64]_m or svqsubr[_n_s64]_x or svqsubr[_n_s64]_z
  ///   public static unsafe Vector<byte> SubtractSaturateReversed(Vector<byte> left, byte right); // svqsubr[_n_u8]_m or svqsubr[_n_u8]_x or svqsubr[_n_u8]_z
  ///   public static unsafe Vector<ushort> SubtractSaturateReversed(Vector<ushort> left, ushort right); // svqsubr[_n_u16]_m or svqsubr[_n_u16]_x or svqsubr[_n_u16]_z
  ///   public static unsafe Vector<uint> SubtractSaturateReversed(Vector<uint> left, uint right); // svqsubr[_n_u32]_m or svqsubr[_n_u32]_x or svqsubr[_n_u32]_z
  ///   public static unsafe Vector<ulong> SubtractSaturateReversed(Vector<ulong> left, ulong right); // svqsubr[_n_u64]_m or svqsubr[_n_u64]_x or svqsubr[_n_u64]_z
  ///   public static unsafe Vector<short> SubtractWideLower(Vector<short> left, sbyte right); // svsubwb[_n_s16]
  ///   public static unsafe Vector<int> SubtractWideLower(Vector<int> left, short right); // svsubwb[_n_s32]
  ///   public static unsafe Vector<long> SubtractWideLower(Vector<long> left, int right); // svsubwb[_n_s64]
  ///   public static unsafe Vector<ushort> SubtractWideLower(Vector<ushort> left, byte right); // svsubwb[_n_u16]
  ///   public static unsafe Vector<uint> SubtractWideLower(Vector<uint> left, ushort right); // svsubwb[_n_u32]
  ///   public static unsafe Vector<ulong> SubtractWideLower(Vector<ulong> left, uint right); // svsubwb[_n_u64]
  ///   public static unsafe Vector<short> SubtractWideUpper(Vector<short> left, sbyte right); // svsubwt[_n_s16]
  ///   public static unsafe Vector<int> SubtractWideUpper(Vector<int> left, short right); // svsubwt[_n_s32]
  ///   public static unsafe Vector<long> SubtractWideUpper(Vector<long> left, int right); // svsubwt[_n_s64]
  ///   public static unsafe Vector<ushort> SubtractWideUpper(Vector<ushort> left, byte right); // svsubwt[_n_u16]
  ///   public static unsafe Vector<uint> SubtractWideUpper(Vector<uint> left, ushort right); // svsubwt[_n_u32]
  ///   public static unsafe Vector<ulong> SubtractWideUpper(Vector<ulong> left, uint right); // svsubwt[_n_u64]
  ///   public static unsafe Vector<short> SubtractWideningLower(Vector<sbyte> left, sbyte right); // svsublb[_n_s16]
  ///   public static unsafe Vector<int> SubtractWideningLower(Vector<short> left, short right); // svsublb[_n_s32]
  ///   public static unsafe Vector<long> SubtractWideningLower(Vector<int> left, int right); // svsublb[_n_s64]
  ///   public static unsafe Vector<ushort> SubtractWideningLower(Vector<byte> left, byte right); // svsublb[_n_u16]
  ///   public static unsafe Vector<uint> SubtractWideningLower(Vector<ushort> left, ushort right); // svsublb[_n_u32]
  ///   public static unsafe Vector<ulong> SubtractWideningLower(Vector<uint> left, uint right); // svsublb[_n_u64]
  ///   public static unsafe Vector<short> SubtractWideningLowerUpper(Vector<sbyte> left, sbyte right); // svsublbt[_n_s16]
  ///   public static unsafe Vector<int> SubtractWideningLowerUpper(Vector<short> left, short right); // svsublbt[_n_s32]
  ///   public static unsafe Vector<long> SubtractWideningLowerUpper(Vector<int> left, int right); // svsublbt[_n_s64]
  ///   public static unsafe Vector<short> SubtractWideningUpper(Vector<sbyte> left, sbyte right); // svsublt[_n_s16]
  ///   public static unsafe Vector<int> SubtractWideningUpper(Vector<short> left, short right); // svsublt[_n_s32]
  ///   public static unsafe Vector<long> SubtractWideningUpper(Vector<int> left, int right); // svsublt[_n_s64]
  ///   public static unsafe Vector<ushort> SubtractWideningUpper(Vector<byte> left, byte right); // svsublt[_n_u16]
  ///   public static unsafe Vector<uint> SubtractWideningUpper(Vector<ushort> left, ushort right); // svsublt[_n_u32]
  ///   public static unsafe Vector<ulong> SubtractWideningUpper(Vector<uint> left, uint right); // svsublt[_n_u64]
  ///   public static unsafe Vector<short> SubtractWideningUpperLower(Vector<sbyte> left, sbyte right); // svsubltb[_n_s16]
  ///   public static unsafe Vector<int> SubtractWideningUpperLower(Vector<short> left, short right); // svsubltb[_n_s32]
  ///   public static unsafe Vector<long> SubtractWideningUpperLower(Vector<int> left, int right); // svsubltb[_n_s64]
  ///   public static unsafe Vector<uint> SubtractWithBorrowWideningLower(Vector<uint> op1, Vector<uint> op2, uint op3); // svsbclb[_n_u32]
  ///   public static unsafe Vector<ulong> SubtractWithBorrowWideningLower(Vector<ulong> op1, Vector<ulong> op2, ulong op3); // svsbclb[_n_u64]
  ///   public static unsafe Vector<uint> SubtractWithBorrowWideningUpper(Vector<uint> op1, Vector<uint> op2, uint op3); // svsbclt[_n_u32]
  ///   public static unsafe Vector<ulong> SubtractWithBorrowWideningUpper(Vector<ulong> op1, Vector<ulong> op2, ulong op3); // svsbclt[_n_u64]
  ///   Total Rejected: 294

  /// Total ACLE covered across API:      1024

