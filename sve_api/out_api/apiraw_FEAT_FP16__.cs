namespace System.Runtime.Intrinsics.Arm;

/// VectorT Summary
public abstract partial class SveFp16 : AdvSimd /// Feature: FEAT_FP16
{

  public static unsafe Vector<half> Abs(Vector<half> value); // FABS // predicated, MOVPRFX

  public static unsafe Vector<half> AbsoluteCompareGreaterThan(Vector<half> left, Vector<half> right); // FACGT // predicated

  public static unsafe Vector<half> AbsoluteCompareGreaterThanOrEqual(Vector<half> left, Vector<half> right); // FACGE // predicated

  public static unsafe Vector<half> AbsoluteCompareLessThan(Vector<half> left, Vector<half> right); // FACGT // predicated

  public static unsafe Vector<half> AbsoluteCompareLessThanOrEqual(Vector<half> left, Vector<half> right); // FACGE // predicated

  public static unsafe Vector<half> AbsoluteDifference(Vector<half> left, Vector<half> right); // FABD // predicated, MOVPRFX

  public static unsafe Vector<half> Add(Vector<half> left, Vector<half> right); // FADD // predicated, MOVPRFX

  public static unsafe Vector<half> AddAcross(Vector<half> value); // FADDV // predicated

  public static unsafe Vector<half> AddPairwise(Vector<half> left, Vector<half> right); // FADDP // predicated, MOVPRFX

  public static unsafe Vector<half> AddRotateComplex(Vector<half> left, Vector<half> right, [ConstantExpected] byte rotation); // FCADD // predicated, MOVPRFX

  public static unsafe Vector<half> AddSequentialAcross(Vector<half> initial, Vector<half> value); // FADDA // predicated

  public static unsafe Vector<half> CompareEqual(Vector<half> left, Vector<half> right); // FCMEQ // predicated

  public static unsafe Vector<half> CompareGreaterThan(Vector<half> left, Vector<half> right); // FCMGT // predicated

  public static unsafe Vector<half> CompareGreaterThanOrEqual(Vector<half> left, Vector<half> right); // FCMGE // predicated

  public static unsafe Vector<half> CompareLessThan(Vector<half> left, Vector<half> right); // FCMGT // predicated

  public static unsafe Vector<half> CompareLessThanOrEqual(Vector<half> left, Vector<half> right); // FCMGE // predicated

  public static unsafe Vector<half> CompareNotEqualTo(Vector<half> left, Vector<half> right); // FCMNE // predicated

  public static unsafe Vector<half> CompareUnordered(Vector<half> left, Vector<half> right); // FCMUO // predicated

  public static unsafe Vector<half> ConcatenateEvenInt128FromTwoInputs(Vector<half> left, Vector<half> right); // UZP1

  public static unsafe Vector<half> ConcatenateOddInt128FromTwoInputs(Vector<half> left, Vector<half> right); // UZP2

  public static unsafe Vector<half> ConditionalExtractAfterLastActiveElement(Vector<half> mask, Vector<half> defaultValue, Vector<half> data); // CLASTA // MOVPRFX

  public static unsafe half ConditionalExtractAfterLastActiveElement(Vector<half> mask, half defaultValues, Vector<half> data); // CLASTA // MOVPRFX

  public static unsafe Vector<half> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<half> mask, Vector<half> defaultScalar, Vector<half> data); // CLASTA // MOVPRFX

  public static unsafe Vector<half> ConditionalExtractLastActiveElement(Vector<half> mask, Vector<half> defaultValue, Vector<half> data); // CLASTB // MOVPRFX

  public static unsafe half ConditionalExtractLastActiveElement(Vector<half> mask, half defaultValues, Vector<half> data); // CLASTB // MOVPRFX

  public static unsafe Vector<half> ConditionalExtractLastActiveElementAndReplicate(Vector<half> mask, Vector<half> fallback, Vector<half> data); // CLASTB // MOVPRFX

  public static unsafe Vector<half> ConditionalSelect(Vector<half> mask, Vector<half> left, Vector<half> right); // SEL

  public static unsafe Vector<double> ConvertToDouble(Vector<half> value); // FCVT // predicated, MOVPRFX

  /// T: [half, float], [half, double], [half, short], [half, int], [half, long], [half, ushort], [half, uint], [half, ulong]
  public static unsafe Vector<T> ConvertToHalf(Vector<T2> value); // FCVT or SCVTF or UCVTF // predicated, MOVPRFX

  public static unsafe Vector<short> ConvertToInt16(Vector<half> value); // FCVTZS // predicated, MOVPRFX

  public static unsafe Vector<int> ConvertToInt32(Vector<half> value); // FCVTZS // predicated, MOVPRFX

  public static unsafe Vector<long> ConvertToInt64(Vector<half> value); // FCVTZS // predicated, MOVPRFX

  public static unsafe Vector<float> ConvertToSingle(Vector<half> value); // FCVT // predicated, MOVPRFX

  public static unsafe Vector<ushort> ConvertToUInt16(Vector<half> value); // FCVTZU // predicated, MOVPRFX

  public static unsafe Vector<uint> ConvertToUInt32(Vector<half> value); // FCVTZU // predicated, MOVPRFX

  public static unsafe Vector<ulong> ConvertToUInt64(Vector<half> value); // FCVTZU // predicated, MOVPRFX

  public static unsafe Vector<half> CreateFalseMaskHalf(); // PFALSE

  public static unsafe Vector<half> CreateTrueMaskHalf([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // PTRUE

  public static unsafe Vector<half> CreateWhileReadAfterWriteMask(half* left, half* right); // WHILERW

  public static unsafe Vector<half> CreateWhileWriteAfterReadMask(half* left, half* right); // WHILEWR

  public static unsafe Vector<half> Divide(Vector<half> left, Vector<half> right); // FDIV or FDIVR // predicated, MOVPRFX

  public static unsafe Vector<half> DownConvertNarrowingUpper(Vector<float> value); // FCVTNT // predicated

  public static unsafe Vector<half> DuplicateSelectedScalarToVector(Vector<half> data, [ConstantExpected] byte index); // DUP or TBL

  public static unsafe half ExtractAfterLastScalar(Vector<half> value); // LASTA // predicated

  public static unsafe Vector<half> ExtractAfterLastVector(Vector<half> value); // LASTA // predicated

  public static unsafe half ExtractLastScalar(Vector<half> value); // LASTB // predicated

  public static unsafe Vector<half> ExtractLastVector(Vector<half> value); // LASTB // predicated

  public static unsafe Vector<half> ExtractVector(Vector<half> upper, Vector<half> lower, [ConstantExpected] byte index); // EXT // MOVPRFX

  public static unsafe Vector<half> FloatingPointExponentialAccelerator(Vector<ushort> value); // FEXPA

  public static unsafe Vector<half> FusedMultiplyAdd(Vector<half> addend, Vector<half> left, Vector<half> right); // FMLA or FMAD // predicated, MOVPRFX

  public static unsafe Vector<half> FusedMultiplyAddBySelectedScalar(Vector<half> addend, Vector<half> left, Vector<half> right, [ConstantExpected] byte rightIndex); // FMLA // MOVPRFX

  public static unsafe Vector<half> FusedMultiplyAddNegated(Vector<half> addend, Vector<half> left, Vector<half> right); // FNMLA or FNMAD // predicated, MOVPRFX

  public static unsafe Vector<half> FusedMultiplySubtract(Vector<half> minuend, Vector<half> left, Vector<half> right); // FMLS or FMSB // predicated, MOVPRFX

  public static unsafe Vector<half> FusedMultiplySubtractBySelectedScalar(Vector<half> minuend, Vector<half> left, Vector<half> right, [ConstantExpected] byte rightIndex); // FMLS // MOVPRFX

  public static unsafe Vector<half> FusedMultiplySubtractNegated(Vector<half> minuend, Vector<half> left, Vector<half> right); // FNMLS or FNMSB // predicated, MOVPRFX

  public static unsafe ulong GetActiveElementCount(Vector<half> mask, Vector<half> from); // CNTP

  public static unsafe Vector<half> InsertIntoShiftedVector(Vector<half> left, half right); // INSR

  public static unsafe Vector<half> InterleaveEvenInt128FromTwoInputs(Vector<half> left, Vector<half> right); // TRN1

  public static unsafe Vector<half> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<half> left, Vector<half> right); // ZIP2

  public static unsafe Vector<half> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<half> left, Vector<half> right); // ZIP1

  public static unsafe Vector<half> InterleaveOddInt128FromTwoInputs(Vector<half> left, Vector<half> right); // TRN2

  public static unsafe Vector<half> LoadVector(Vector<half> mask, half* address); // LD1H

  public static unsafe Vector<half> LoadVector128AndReplicateToVector(Vector<half> mask, half* address); // LD1RQH

  public static unsafe Vector<half> LoadVector256AndReplicateToVector(Vector<half> mask, half* address); // LD1ROH

  public static unsafe Vector<half> LoadVectorFirstFaulting(Vector<half> mask, half* address); // LDFF1H

  public static unsafe Vector<half> LoadVectorNonFaulting(half* address); // LDNF1H // predicated

  public static unsafe Vector<half> LoadVectorNonTemporal(Vector<half> mask, half* address); // LDNT1H

  public static unsafe (Vector<half>, Vector<half>) LoadVectorx2(Vector<half> mask, half* address); // LD2H

  public static unsafe (Vector<half>, Vector<half>, Vector<half>) LoadVectorx3(Vector<half> mask, half* address); // LD3H

  public static unsafe (Vector<half>, Vector<half>, Vector<half>, Vector<half>) LoadVectorx4(Vector<half> mask, half* address); // LD4H

  public static unsafe Vector<short> Log2(Vector<half> value); // FLOGB // predicated, MOVPRFX

  public static unsafe Vector<half> Max(Vector<half> left, Vector<half> right); // FMAX // predicated, MOVPRFX

  public static unsafe Vector<half> MaxAcross(Vector<half> value); // FMAXV // predicated

  public static unsafe Vector<half> MaxNumber(Vector<half> left, Vector<half> right); // FMAXNM // predicated, MOVPRFX

  public static unsafe Vector<half> MaxNumberAcross(Vector<half> value); // FMAXNMV // predicated

  public static unsafe Vector<half> MaxNumberPairwise(Vector<half> left, Vector<half> right); // FMAXNMP // predicated, MOVPRFX

  public static unsafe Vector<half> MaxPairwise(Vector<half> left, Vector<half> right); // FMAXP // predicated, MOVPRFX

  public static unsafe Vector<half> Min(Vector<half> left, Vector<half> right); // FMIN // predicated, MOVPRFX

  public static unsafe Vector<half> MinAcross(Vector<half> value); // FMINV // predicated

  public static unsafe Vector<half> MinNumber(Vector<half> left, Vector<half> right); // FMINNM // predicated, MOVPRFX

  public static unsafe Vector<half> MinNumberAcross(Vector<half> value); // FMINNMV // predicated

  public static unsafe Vector<half> MinNumberPairwise(Vector<half> left, Vector<half> right); // FMINNMP // predicated, MOVPRFX

  public static unsafe Vector<half> MinPairwise(Vector<half> left, Vector<half> right); // FMINP // predicated, MOVPRFX

  public static unsafe Vector<half> Multiply(Vector<half> left, Vector<half> right); // FMUL // predicated, MOVPRFX

  public static unsafe Vector<half> MultiplyAddRotateComplex(Vector<half> addend, Vector<half> left, Vector<half> right, [ConstantExpected] byte rotation); // FCMLA // predicated, MOVPRFX

  public static unsafe Vector<half> MultiplyAddRotateComplexBySelectedScalar(Vector<half> addend, Vector<half> left, Vector<half> right, [ConstantExpected] byte rightIndex, [ConstantExpected] byte rotation); // FCMLA // MOVPRFX

  public static unsafe Vector<float> MultiplyAddWideningLower(Vector<float> op1, Vector<half> op2, Vector<half> op3); // FMLALB // MOVPRFX

  public static unsafe Vector<float> MultiplyAddWideningLower(Vector<float> op1, Vector<half> op2, Vector<half> op3, ulong imm_index); // FMLALB // MOVPRFX

  public static unsafe Vector<float> MultiplyAddWideningUpper(Vector<float> op1, Vector<half> op2, Vector<half> op3); // FMLALT // MOVPRFX

  public static unsafe Vector<float> MultiplyAddWideningUpper(Vector<float> op1, Vector<half> op2, Vector<half> op3, ulong imm_index); // FMLALT // MOVPRFX

  public static unsafe Vector<half> MultiplyBySelectedScalar(Vector<half> left, Vector<half> right, [ConstantExpected] byte rightIndex); // FMUL

  public static unsafe Vector<half> MultiplyExtended(Vector<half> left, Vector<half> right); // FMULX // predicated, MOVPRFX

  public static unsafe Vector<float> MultiplySubtractWideningLower(Vector<float> op1, Vector<half> op2, Vector<half> op3); // FMLSLB // MOVPRFX

  public static unsafe Vector<float> MultiplySubtractWideningLower(Vector<float> op1, Vector<half> op2, Vector<half> op3, ulong imm_index); // FMLSLB // MOVPRFX

  public static unsafe Vector<float> MultiplySubtractWideningUpper(Vector<float> op1, Vector<half> op2, Vector<half> op3); // FMLSLT // MOVPRFX

  public static unsafe Vector<float> MultiplySubtractWideningUpper(Vector<float> op1, Vector<half> op2, Vector<half> op3, ulong imm_index); // FMLSLT // MOVPRFX

  public static unsafe Vector<half> Negate(Vector<half> value); // FNEG // predicated, MOVPRFX

  public static unsafe Vector<ushort> PopCount(Vector<half> value); // CNT // predicated, MOVPRFX

  public static unsafe Vector<half> ReciprocalEstimate(Vector<half> value); // FRECPE

  public static unsafe Vector<half> ReciprocalExponent(Vector<half> value); // FRECPX // predicated, MOVPRFX

  public static unsafe Vector<half> ReciprocalSqrtEstimate(Vector<half> value); // FRSQRTE

  public static unsafe Vector<half> ReciprocalSqrtStep(Vector<half> left, Vector<half> right); // FRSQRTS

  public static unsafe Vector<half> ReciprocalStep(Vector<half> left, Vector<half> right); // FRECPS

  public static unsafe Vector<half> ReverseElement(Vector<half> value); // REV

  public static unsafe Vector<half> RoundAwayFromZero(Vector<half> value); // FRINTA // predicated, MOVPRFX

  public static unsafe Vector<half> RoundToNearest(Vector<half> value); // FRINTN // predicated, MOVPRFX

  public static unsafe Vector<half> RoundToNegativeInfinity(Vector<half> value); // FRINTM // predicated, MOVPRFX

  public static unsafe Vector<half> RoundToPositiveInfinity(Vector<half> value); // FRINTP // predicated, MOVPRFX

  public static unsafe Vector<half> RoundToZero(Vector<half> value); // FRINTZ // predicated, MOVPRFX

  public static unsafe Vector<half> Scale(Vector<half> left, Vector<short> right); // FSCALE // predicated, MOVPRFX

  public static unsafe Vector<half> Splice(Vector<half> mask, Vector<half> left, Vector<half> right); // SPLICE // MOVPRFX

  public static unsafe Vector<half> Sqrt(Vector<half> value); // FSQRT // predicated, MOVPRFX

  public static unsafe void Store(Vector<half> mask, half* address, Vector<half> data); // ST1H

  public static unsafe void Store(Vector<half> mask, half* address, (Vector<half> Value1, Vector<half> Value2) data); // ST2H

  public static unsafe void Store(Vector<half> mask, half* address, (Vector<half> Value1, Vector<half> Value2, Vector<half> Value3) data); // ST3H

  public static unsafe void Store(Vector<half> mask, half* address, (Vector<half> Value1, Vector<half> Value2, Vector<half> Value3, Vector<half> Value4) data); // ST4H

  public static unsafe void StoreNonTemporal(Vector<half> mask, half* address, Vector<half> data); // STNT1H

  public static unsafe Vector<half> Subtract(Vector<half> left, Vector<half> right); // FSUB or FSUBR // predicated, MOVPRFX

  public static unsafe Vector<half> TransposeEven(Vector<half> left, Vector<half> right); // TRN1

  public static unsafe Vector<half> TransposeOdd(Vector<half> left, Vector<half> right); // TRN2

  public static unsafe Vector<half> TrigonometricMultiplyAddCoefficient(Vector<half> left, Vector<half> right, [ConstantExpected] byte control); // FTMAD // MOVPRFX

  public static unsafe Vector<half> TrigonometricSelectCoefficient(Vector<half> value, Vector<ushort> selector); // FTSSEL

  public static unsafe Vector<half> TrigonometricStartingValue(Vector<half> value, Vector<ushort> sign); // FTSMUL

  public static unsafe Vector<half> UnzipEven(Vector<half> left, Vector<half> right); // UZP1

  public static unsafe Vector<half> UnzipOdd(Vector<half> left, Vector<half> right); // UZP2

  public static unsafe Vector<float> UpConvertWideningUpper(Vector<half> value); // FCVTLT // predicated

  public static unsafe Vector<half> VectorTableLookup(Vector<half> data, Vector<ushort> indices); // TBL

  public static unsafe Vector<half> VectorTableLookup((Vector<half> data1, Vector<half> data2), Vector<ushort> indices); // TBL

  public static unsafe Vector<half> VectorTableLookupExtension(Vector<half> fallback, Vector<half> data, Vector<ushort> indices); // TBX

  public static unsafe Vector<half> ZipHigh(Vector<half> left, Vector<half> right); // ZIP2

  public static unsafe Vector<half> ZipLow(Vector<half> left, Vector<half> right); // ZIP1


  // All patterns used by PTRUE.
  public enum SveMaskPattern : byte
  {
    LargestPowerOf2 = 0,   // The largest power of 2.
    VectorCount1 = 1,    // 1 element.
    VectorCount2 = 2,    // 2 elements.
    VectorCount3 = 3,    // 3 elements.
    VectorCount4 = 4,    // 4 elements.
    VectorCount5 = 5,    // 5 elements.
    VectorCount6 = 6,    // 6 elements.
    VectorCount7 = 7,    // 7 elements.
    VectorCount8 = 8,    // 8 elements.
    VectorCount16 = 9,   // 16 elements.
    VectorCount32 = 10,  // 32 elements.
    VectorCount64 = 11,  // 64 elements.
    VectorCount128 = 12, // 128 elements.
    VectorCount256 = 13, // 256 elements.
    LargestMultipleOf4 = 29,  // The largest multiple of 4.
    LargestMultipleOf3 = 30,  // The largest multiple of 3.
    All  = 31    // All available (implicitly a multiple of two).
  };

  /// total method signatures: 131


  /// Optional Entries:

  public static unsafe Vector<half> AbsoluteCompareGreaterThan(Vector<half> left, half right); // FACGT // predicated

  public static unsafe Vector<half> AbsoluteCompareGreaterThanOrEqual(Vector<half> left, half right); // FACGE // predicated

  public static unsafe Vector<half> AbsoluteCompareLessThan(Vector<half> left, half right); // FACGT // predicated

  public static unsafe Vector<half> AbsoluteCompareLessThanOrEqual(Vector<half> left, half right); // FACGE // predicated

  public static unsafe Vector<half> AbsoluteDifference(Vector<half> left, half right); // FABD // predicated, MOVPRFX

  public static unsafe Vector<half> Add(Vector<half> left, half right); // FADD or FSUB // predicated, MOVPRFX

  public static unsafe Vector<half> CompareEqual(Vector<half> left, half right); // FCMEQ // predicated

  public static unsafe Vector<half> CompareGreaterThan(Vector<half> left, half right); // FCMGT // predicated

  public static unsafe Vector<half> CompareGreaterThanOrEqual(Vector<half> left, half right); // FCMGE // predicated

  public static unsafe Vector<half> CompareLessThan(Vector<half> left, half right); // FCMLT or FCMGT // predicated

  public static unsafe Vector<half> CompareLessThanOrEqual(Vector<half> left, half right); // FCMLE or FCMGE // predicated

  public static unsafe Vector<half> CompareNotEqualTo(Vector<half> left, half right); // FCMNE // predicated

  public static unsafe Vector<half> CompareUnordered(Vector<half> left, half right); // FCMUO // predicated

  public static unsafe half ConditionalExtractAfterLastActiveElement(Vector<half> mask, half defaultValue, Vector<half> data); // CLASTA

  public static unsafe half ConditionalExtractAfterLastActiveElementAndReplicate(Vector<half> mask, half defaultScalar, Vector<half> data); // CLASTA

  public static unsafe half ConditionalExtractLastActiveElement(Vector<half> mask, half defaultValue, Vector<half> data); // CLASTB

  public static unsafe half ConditionalExtractLastActiveElementAndReplicate(Vector<half> mask, half fallback, Vector<half> data); // CLASTB

  public static unsafe Vector<half> Divide(Vector<half> left, half right); // FDIV or FDIVR // predicated, MOVPRFX

  public static unsafe Vector<half> Max(Vector<half> left, half right); // FMAX // predicated, MOVPRFX

  public static unsafe Vector<half> MaxNumber(Vector<half> left, half right); // FMAXNM // predicated, MOVPRFX

  public static unsafe Vector<half> Min(Vector<half> left, half right); // FMIN // predicated, MOVPRFX

  public static unsafe Vector<half> MinNumber(Vector<half> left, half right); // FMINNM // predicated, MOVPRFX

  public static unsafe Vector<half> Multiply(Vector<half> left, half right); // FMUL // predicated, MOVPRFX

  public static unsafe Vector<half> MultiplyAdd(Vector<half> addend, Vector<half> left, half right); // FMLA or FMAD // predicated, MOVPRFX

  public static unsafe Vector<half> MultiplyAddNegated(Vector<half> addend, Vector<half> left, half right); // FNMLA or FNMAD // predicated, MOVPRFX

  public static unsafe Vector<float> MultiplyAddWideningLower(Vector<float> op1, Vector<half> op2, half op3); // FMLALB // MOVPRFX

  public static unsafe Vector<float> MultiplyAddWideningUpper(Vector<float> op1, Vector<half> op2, half op3); // FMLALT // MOVPRFX

  public static unsafe Vector<half> MultiplyExtended(Vector<half> left, half right); // FMULX // predicated, MOVPRFX

  public static unsafe Vector<half> MultiplySubtract(Vector<half> minuend, Vector<half> left, half right); // FMLS or FMSB // predicated, MOVPRFX

  public static unsafe Vector<half> MultiplySubtractNegated(Vector<half> minuend, Vector<half> left, half right); // FNMLS or FNMSB // predicated, MOVPRFX

  public static unsafe Vector<float> MultiplySubtractWideningLower(Vector<float> op1, Vector<half> op2, half op3); // FMLSLB // MOVPRFX

  public static unsafe Vector<float> MultiplySubtractWideningUpper(Vector<float> op1, Vector<half> op2, half op3); // FMLSLT // MOVPRFX

  public static unsafe Vector<half> Subtract(Vector<half> left, half right); // FSUB or FADD or FSUBR // predicated, MOVPRFX

  /// total optional method signatures: 33

}


/// Full API
public abstract partial class SveFp16 : AdvSimd /// Feature: FEAT_FP16
{
    /// Abs : Absolute value

    /// svfloat16_t svabs[_f16]_m(svfloat16_t inactive, svbool_t pg, svfloat16_t op) : "FABS Ztied.H, Pg/M, Zop.H" or "MOVPRFX Zresult, Zinactive; FABS Zresult.H, Pg/M, Zop.H"
    /// svfloat16_t svabs[_f16]_x(svbool_t pg, svfloat16_t op) : "FABS Ztied.H, Pg/M, Ztied.H" or "MOVPRFX Zresult, Zop; FABS Zresult.H, Pg/M, Zop.H"
    /// svfloat16_t svabs[_f16]_z(svbool_t pg, svfloat16_t op) : "MOVPRFX Zresult.H, Pg/Z, Zop.H; FABS Zresult.H, Pg/M, Zop.H"
  public static unsafe Vector<half> Abs(Vector<half> value);


    /// AbsoluteCompareGreaterThan : Absolute compare greater than

    /// svbool_t svacgt[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "FACGT Presult.H, Pg/Z, Zop1.H, Zop2.H"
  public static unsafe Vector<half> AbsoluteCompareGreaterThan(Vector<half> left, Vector<half> right);


    /// AbsoluteCompareGreaterThanOrEqual : Absolute compare greater than or equal to

    /// svbool_t svacge[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "FACGE Presult.H, Pg/Z, Zop1.H, Zop2.H"
  public static unsafe Vector<half> AbsoluteCompareGreaterThanOrEqual(Vector<half> left, Vector<half> right);


    /// AbsoluteCompareLessThan : Absolute compare less than

    /// svbool_t svaclt[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "FACGT Presult.H, Pg/Z, Zop2.H, Zop1.H"
  public static unsafe Vector<half> AbsoluteCompareLessThan(Vector<half> left, Vector<half> right);


    /// AbsoluteCompareLessThanOrEqual : Absolute compare less than or equal to

    /// svbool_t svacle[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "FACGE Presult.H, Pg/Z, Zop2.H, Zop1.H"
  public static unsafe Vector<half> AbsoluteCompareLessThanOrEqual(Vector<half> left, Vector<half> right);


    /// AbsoluteDifference : Absolute difference

    /// svfloat16_t svabd[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "FABD Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; FABD Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svfloat16_t svabd[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "FABD Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "FABD Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "MOVPRFX Zresult, Zop1; FABD Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svfloat16_t svabd[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; FABD Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; FABD Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<half> AbsoluteDifference(Vector<half> left, Vector<half> right);


    /// Add : Add

    /// svfloat16_t svadd[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "FADD Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; FADD Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svfloat16_t svadd[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "FADD Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "FADD Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "FADD Zresult.H, Zop1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; FADD Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svfloat16_t svadd[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; FADD Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; FADD Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<half> Add(Vector<half> left, Vector<half> right);


    /// AddAcross : Add reduction

    /// float16_t svaddv[_f16](svbool_t pg, svfloat16_t op) : "FADDV Hresult, Pg, Zop.H"
  public static unsafe Vector<half> AddAcross(Vector<half> value);


    /// AddPairwise : Add pairwise

    /// svfloat16_t svaddp[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "FADDP Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; FADDP Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svfloat16_t svaddp[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "FADDP Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; FADDP Zresult.H, Pg/M, Zresult.H, Zop2.H"
  public static unsafe Vector<half> AddPairwise(Vector<half> left, Vector<half> right);


    /// AddRotateComplex : Complex add with rotate

    /// svfloat16_t svcadd[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2, uint64_t imm_rotation) : "FCADD Ztied1.H, Pg/M, Ztied1.H, Zop2.H, #imm_rotation" or "MOVPRFX Zresult, Zop1; FCADD Zresult.H, Pg/M, Zresult.H, Zop2.H, #imm_rotation"
    /// svfloat16_t svcadd[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2, uint64_t imm_rotation) : "FCADD Ztied1.H, Pg/M, Ztied1.H, Zop2.H, #imm_rotation" or "MOVPRFX Zresult, Zop1; FCADD Zresult.H, Pg/M, Zresult.H, Zop2.H, #imm_rotation"
    /// svfloat16_t svcadd[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2, uint64_t imm_rotation) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; FCADD Zresult.H, Pg/M, Zresult.H, Zop2.H, #imm_rotation"
  public static unsafe Vector<half> AddRotateComplex(Vector<half> left, Vector<half> right, [ConstantExpected] byte rotation);


    /// AddSequentialAcross : Add reduction (strictly-ordered)

    /// float16_t svadda[_f16](svbool_t pg, float16_t initial, svfloat16_t op) : "FADDA Htied, Pg, Htied, Zop.H"
  public static unsafe Vector<half> AddSequentialAcross(Vector<half> initial, Vector<half> value);


    /// CompareEqual : Compare equal to

    /// svbool_t svcmpeq[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "FCMEQ Presult.H, Pg/Z, Zop1.H, Zop2.H"
  public static unsafe Vector<half> CompareEqual(Vector<half> left, Vector<half> right);


    /// CompareGreaterThan : Compare greater than

    /// svbool_t svcmpgt[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "FCMGT Presult.H, Pg/Z, Zop1.H, Zop2.H"
  public static unsafe Vector<half> CompareGreaterThan(Vector<half> left, Vector<half> right);


    /// CompareGreaterThanOrEqual : Compare greater than or equal to

    /// svbool_t svcmpge[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "FCMGE Presult.H, Pg/Z, Zop1.H, Zop2.H"
  public static unsafe Vector<half> CompareGreaterThanOrEqual(Vector<half> left, Vector<half> right);


    /// CompareLessThan : Compare less than

    /// svbool_t svcmplt[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "FCMGT Presult.H, Pg/Z, Zop2.H, Zop1.H"
  public static unsafe Vector<half> CompareLessThan(Vector<half> left, Vector<half> right);


    /// CompareLessThanOrEqual : Compare less than or equal to

    /// svbool_t svcmple[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "FCMGE Presult.H, Pg/Z, Zop2.H, Zop1.H"
  public static unsafe Vector<half> CompareLessThanOrEqual(Vector<half> left, Vector<half> right);


    /// CompareNotEqualTo : Compare not equal to

    /// svbool_t svcmpne[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "FCMNE Presult.H, Pg/Z, Zop1.H, Zop2.H"
  public static unsafe Vector<half> CompareNotEqualTo(Vector<half> left, Vector<half> right);


    /// CompareUnordered : Compare unordered with

    /// svbool_t svcmpuo[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "FCMUO Presult.H, Pg/Z, Zop1.H, Zop2.H"
  public static unsafe Vector<half> CompareUnordered(Vector<half> left, Vector<half> right);


    /// ConcatenateEvenInt128FromTwoInputs : Concatenate even quadwords from two inputs

    /// svfloat16_t svuzp1q[_f16](svfloat16_t op1, svfloat16_t op2) : "UZP1 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<half> ConcatenateEvenInt128FromTwoInputs(Vector<half> left, Vector<half> right);


    /// ConcatenateOddInt128FromTwoInputs : Concatenate odd quadwords from two inputs

    /// svfloat16_t svuzp2q[_f16](svfloat16_t op1, svfloat16_t op2) : "UZP2 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<half> ConcatenateOddInt128FromTwoInputs(Vector<half> left, Vector<half> right);


    /// ConditionalExtractAfterLastActiveElement : Conditionally extract element after last

    /// svfloat16_t svclasta[_f16](svbool_t pg, svfloat16_t fallback, svfloat16_t data) : "CLASTA Ztied.H, Pg, Ztied.H, Zdata.H" or "MOVPRFX Zresult, Zfallback; CLASTA Zresult.H, Pg, Zresult.H, Zdata.H"
  public static unsafe Vector<half> ConditionalExtractAfterLastActiveElement(Vector<half> mask, Vector<half> defaultValue, Vector<half> data);

    /// svfloat16_t svclasta[_f16](svbool_t pg, svfloat16_t fallback, svfloat16_t data) : "CLASTA Ztied.H, Pg, Ztied.H, Zdata.H" or "MOVPRFX Zresult, Zfallback; CLASTA Zresult.H, Pg, Zresult.H, Zdata.H"
    /// float16_t svclasta[_n_f16](svbool_t pg, float16_t fallback, svfloat16_t data) : "CLASTA Wtied, Pg, Wtied, Zdata.H" or "CLASTA Htied, Pg, Htied, Zdata.H"
  public static unsafe half ConditionalExtractAfterLastActiveElement(Vector<half> mask, half defaultValues, Vector<half> data);


    /// ConditionalExtractAfterLastActiveElementAndReplicate : Conditionally extract element after last

    /// svfloat16_t svclasta[_f16](svbool_t pg, svfloat16_t fallback, svfloat16_t data) : "CLASTA Ztied.H, Pg, Ztied.H, Zdata.H" or "MOVPRFX Zresult, Zfallback; CLASTA Zresult.H, Pg, Zresult.H, Zdata.H"
  public static unsafe Vector<half> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<half> mask, Vector<half> defaultScalar, Vector<half> data);


    /// ConditionalExtractLastActiveElement : Conditionally extract last element

    /// svfloat16_t svclastb[_f16](svbool_t pg, svfloat16_t fallback, svfloat16_t data) : "CLASTB Ztied.H, Pg, Ztied.H, Zdata.H" or "MOVPRFX Zresult, Zfallback; CLASTB Zresult.H, Pg, Zresult.H, Zdata.H"
  public static unsafe Vector<half> ConditionalExtractLastActiveElement(Vector<half> mask, Vector<half> defaultValue, Vector<half> data);

    /// svfloat16_t svclastb[_f16](svbool_t pg, svfloat16_t fallback, svfloat16_t data) : "CLASTB Ztied.H, Pg, Ztied.H, Zdata.H" or "MOVPRFX Zresult, Zfallback; CLASTB Zresult.H, Pg, Zresult.H, Zdata.H"
    /// float16_t svclastb[_n_f16](svbool_t pg, float16_t fallback, svfloat16_t data) : "CLASTB Wtied, Pg, Wtied, Zdata.H" or "CLASTB Htied, Pg, Htied, Zdata.H"
  public static unsafe half ConditionalExtractLastActiveElement(Vector<half> mask, half defaultValues, Vector<half> data);


    /// ConditionalExtractLastActiveElementAndReplicate : Conditionally extract last element

    /// svfloat16_t svclastb[_f16](svbool_t pg, svfloat16_t fallback, svfloat16_t data) : "CLASTB Ztied.H, Pg, Ztied.H, Zdata.H" or "MOVPRFX Zresult, Zfallback; CLASTB Zresult.H, Pg, Zresult.H, Zdata.H"
  public static unsafe Vector<half> ConditionalExtractLastActiveElementAndReplicate(Vector<half> mask, Vector<half> fallback, Vector<half> data);


    /// ConditionalSelect : Conditionally select elements

    /// svfloat16_t svsel[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "SEL Zresult.H, Pg, Zop1.H, Zop2.H"
  public static unsafe Vector<half> ConditionalSelect(Vector<half> mask, Vector<half> left, Vector<half> right);


    /// ConvertToDouble : Floating-point convert

    /// svfloat64_t svcvt_f64[_f16]_m(svfloat64_t inactive, svbool_t pg, svfloat16_t op) : "FCVT Ztied.D, Pg/M, Zop.H" or "MOVPRFX Zresult, Zinactive; FCVT Zresult.D, Pg/M, Zop.H"
    /// svfloat64_t svcvt_f64[_f16]_x(svbool_t pg, svfloat16_t op) : "FCVT Ztied.D, Pg/M, Ztied.H" or "MOVPRFX Zresult, Zop; FCVT Zresult.D, Pg/M, Zop.H"
    /// svfloat64_t svcvt_f64[_f16]_z(svbool_t pg, svfloat16_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; FCVT Zresult.D, Pg/M, Zop.H"
  public static unsafe Vector<double> ConvertToDouble(Vector<half> value);


    /// ConvertToHalf : Floating-point convert

    /// svfloat16_t svcvt_f16[_f32]_m(svfloat16_t inactive, svbool_t pg, svfloat32_t op) : "FCVT Ztied.H, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; FCVT Zresult.H, Pg/M, Zop.S"
    /// svfloat16_t svcvt_f16[_f32]_x(svbool_t pg, svfloat32_t op) : "FCVT Ztied.H, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; FCVT Zresult.H, Pg/M, Zop.S"
    /// svfloat16_t svcvt_f16[_f32]_z(svbool_t pg, svfloat32_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; FCVT Zresult.H, Pg/M, Zop.S"
  public static unsafe Vector<half> ConvertToHalf(Vector<float> value);

    /// svfloat16_t svcvt_f16[_f64]_m(svfloat16_t inactive, svbool_t pg, svfloat64_t op) : "FCVT Ztied.H, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; FCVT Zresult.H, Pg/M, Zop.D"
    /// svfloat16_t svcvt_f16[_f64]_x(svbool_t pg, svfloat64_t op) : "FCVT Ztied.H, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; FCVT Zresult.H, Pg/M, Zop.D"
    /// svfloat16_t svcvt_f16[_f64]_z(svbool_t pg, svfloat64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; FCVT Zresult.H, Pg/M, Zop.D"
  public static unsafe Vector<half> ConvertToHalf(Vector<double> value);

    /// svfloat16_t svcvt_f16[_s16]_m(svfloat16_t inactive, svbool_t pg, svint16_t op) : "SCVTF Ztied.H, Pg/M, Zop.H" or "MOVPRFX Zresult, Zinactive; SCVTF Zresult.H, Pg/M, Zop.H"
    /// svfloat16_t svcvt_f16[_s16]_x(svbool_t pg, svint16_t op) : "SCVTF Ztied.H, Pg/M, Ztied.H" or "MOVPRFX Zresult, Zop; SCVTF Zresult.H, Pg/M, Zop.H"
    /// svfloat16_t svcvt_f16[_s16]_z(svbool_t pg, svint16_t op) : "MOVPRFX Zresult.H, Pg/Z, Zop.H; SCVTF Zresult.H, Pg/M, Zop.H"
  public static unsafe Vector<half> ConvertToHalf(Vector<short> value);

    /// svfloat16_t svcvt_f16[_s32]_m(svfloat16_t inactive, svbool_t pg, svint32_t op) : "SCVTF Ztied.H, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; SCVTF Zresult.H, Pg/M, Zop.S"
    /// svfloat16_t svcvt_f16[_s32]_x(svbool_t pg, svint32_t op) : "SCVTF Ztied.H, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; SCVTF Zresult.H, Pg/M, Zop.S"
    /// svfloat16_t svcvt_f16[_s32]_z(svbool_t pg, svint32_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; SCVTF Zresult.H, Pg/M, Zop.S"
  public static unsafe Vector<half> ConvertToHalf(Vector<int> value);

    /// svfloat16_t svcvt_f16[_s64]_m(svfloat16_t inactive, svbool_t pg, svint64_t op) : "SCVTF Ztied.H, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; SCVTF Zresult.H, Pg/M, Zop.D"
    /// svfloat16_t svcvt_f16[_s64]_x(svbool_t pg, svint64_t op) : "SCVTF Ztied.H, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; SCVTF Zresult.H, Pg/M, Zop.D"
    /// svfloat16_t svcvt_f16[_s64]_z(svbool_t pg, svint64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; SCVTF Zresult.H, Pg/M, Zop.D"
  public static unsafe Vector<half> ConvertToHalf(Vector<long> value);

    /// svfloat16_t svcvt_f16[_u16]_m(svfloat16_t inactive, svbool_t pg, svuint16_t op) : "UCVTF Ztied.H, Pg/M, Zop.H" or "MOVPRFX Zresult, Zinactive; UCVTF Zresult.H, Pg/M, Zop.H"
    /// svfloat16_t svcvt_f16[_u16]_x(svbool_t pg, svuint16_t op) : "UCVTF Ztied.H, Pg/M, Ztied.H" or "MOVPRFX Zresult, Zop; UCVTF Zresult.H, Pg/M, Zop.H"
    /// svfloat16_t svcvt_f16[_u16]_z(svbool_t pg, svuint16_t op) : "MOVPRFX Zresult.H, Pg/Z, Zop.H; UCVTF Zresult.H, Pg/M, Zop.H"
  public static unsafe Vector<half> ConvertToHalf(Vector<ushort> value);

    /// svfloat16_t svcvt_f16[_u32]_m(svfloat16_t inactive, svbool_t pg, svuint32_t op) : "UCVTF Ztied.H, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; UCVTF Zresult.H, Pg/M, Zop.S"
    /// svfloat16_t svcvt_f16[_u32]_x(svbool_t pg, svuint32_t op) : "UCVTF Ztied.H, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; UCVTF Zresult.H, Pg/M, Zop.S"
    /// svfloat16_t svcvt_f16[_u32]_z(svbool_t pg, svuint32_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; UCVTF Zresult.H, Pg/M, Zop.S"
  public static unsafe Vector<half> ConvertToHalf(Vector<uint> value);

    /// svfloat16_t svcvt_f16[_u64]_m(svfloat16_t inactive, svbool_t pg, svuint64_t op) : "UCVTF Ztied.H, Pg/M, Zop.D" or "MOVPRFX Zresult, Zinactive; UCVTF Zresult.H, Pg/M, Zop.D"
    /// svfloat16_t svcvt_f16[_u64]_x(svbool_t pg, svuint64_t op) : "UCVTF Ztied.H, Pg/M, Ztied.D" or "MOVPRFX Zresult, Zop; UCVTF Zresult.H, Pg/M, Zop.D"
    /// svfloat16_t svcvt_f16[_u64]_z(svbool_t pg, svuint64_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; UCVTF Zresult.H, Pg/M, Zop.D"
  public static unsafe Vector<half> ConvertToHalf(Vector<ulong> value);


    /// ConvertToInt16 : Floating-point convert

    /// svint16_t svcvt_s16[_f16]_m(svint16_t inactive, svbool_t pg, svfloat16_t op) : "FCVTZS Ztied.H, Pg/M, Zop.H" or "MOVPRFX Zresult, Zinactive; FCVTZS Zresult.H, Pg/M, Zop.H"
    /// svint16_t svcvt_s16[_f16]_x(svbool_t pg, svfloat16_t op) : "FCVTZS Ztied.H, Pg/M, Ztied.H" or "MOVPRFX Zresult, Zop; FCVTZS Zresult.H, Pg/M, Zop.H"
    /// svint16_t svcvt_s16[_f16]_z(svbool_t pg, svfloat16_t op) : "MOVPRFX Zresult.H, Pg/Z, Zop.H; FCVTZS Zresult.H, Pg/M, Zop.H"
  public static unsafe Vector<short> ConvertToInt16(Vector<half> value);


    /// ConvertToInt32 : Floating-point convert

    /// svint32_t svcvt_s32[_f16]_m(svint32_t inactive, svbool_t pg, svfloat16_t op) : "FCVTZS Ztied.S, Pg/M, Zop.H" or "MOVPRFX Zresult, Zinactive; FCVTZS Zresult.S, Pg/M, Zop.H"
    /// svint32_t svcvt_s32[_f16]_x(svbool_t pg, svfloat16_t op) : "FCVTZS Ztied.S, Pg/M, Ztied.H" or "MOVPRFX Zresult, Zop; FCVTZS Zresult.S, Pg/M, Zop.H"
    /// svint32_t svcvt_s32[_f16]_z(svbool_t pg, svfloat16_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; FCVTZS Zresult.S, Pg/M, Zop.H"
  public static unsafe Vector<int> ConvertToInt32(Vector<half> value);


    /// ConvertToInt64 : Floating-point convert

    /// svint64_t svcvt_s64[_f16]_m(svint64_t inactive, svbool_t pg, svfloat16_t op) : "FCVTZS Ztied.D, Pg/M, Zop.H" or "MOVPRFX Zresult, Zinactive; FCVTZS Zresult.D, Pg/M, Zop.H"
    /// svint64_t svcvt_s64[_f16]_x(svbool_t pg, svfloat16_t op) : "FCVTZS Ztied.D, Pg/M, Ztied.H" or "MOVPRFX Zresult, Zop; FCVTZS Zresult.D, Pg/M, Zop.H"
    /// svint64_t svcvt_s64[_f16]_z(svbool_t pg, svfloat16_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; FCVTZS Zresult.D, Pg/M, Zop.H"
  public static unsafe Vector<long> ConvertToInt64(Vector<half> value);


    /// ConvertToSingle : Floating-point convert

    /// svfloat32_t svcvt_f32[_f16]_m(svfloat32_t inactive, svbool_t pg, svfloat16_t op) : "FCVT Ztied.S, Pg/M, Zop.H" or "MOVPRFX Zresult, Zinactive; FCVT Zresult.S, Pg/M, Zop.H"
    /// svfloat32_t svcvt_f32[_f16]_x(svbool_t pg, svfloat16_t op) : "FCVT Ztied.S, Pg/M, Ztied.H" or "MOVPRFX Zresult, Zop; FCVT Zresult.S, Pg/M, Zop.H"
    /// svfloat32_t svcvt_f32[_f16]_z(svbool_t pg, svfloat16_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; FCVT Zresult.S, Pg/M, Zop.H"
  public static unsafe Vector<float> ConvertToSingle(Vector<half> value);


    /// ConvertToUInt16 : Floating-point convert

    /// svuint16_t svcvt_u16[_f16]_m(svuint16_t inactive, svbool_t pg, svfloat16_t op) : "FCVTZU Ztied.H, Pg/M, Zop.H" or "MOVPRFX Zresult, Zinactive; FCVTZU Zresult.H, Pg/M, Zop.H"
    /// svuint16_t svcvt_u16[_f16]_x(svbool_t pg, svfloat16_t op) : "FCVTZU Ztied.H, Pg/M, Ztied.H" or "MOVPRFX Zresult, Zop; FCVTZU Zresult.H, Pg/M, Zop.H"
    /// svuint16_t svcvt_u16[_f16]_z(svbool_t pg, svfloat16_t op) : "MOVPRFX Zresult.H, Pg/Z, Zop.H; FCVTZU Zresult.H, Pg/M, Zop.H"
  public static unsafe Vector<ushort> ConvertToUInt16(Vector<half> value);


    /// ConvertToUInt32 : Floating-point convert

    /// svuint32_t svcvt_u32[_f16]_m(svuint32_t inactive, svbool_t pg, svfloat16_t op) : "FCVTZU Ztied.S, Pg/M, Zop.H" or "MOVPRFX Zresult, Zinactive; FCVTZU Zresult.S, Pg/M, Zop.H"
    /// svuint32_t svcvt_u32[_f16]_x(svbool_t pg, svfloat16_t op) : "FCVTZU Ztied.S, Pg/M, Ztied.H" or "MOVPRFX Zresult, Zop; FCVTZU Zresult.S, Pg/M, Zop.H"
    /// svuint32_t svcvt_u32[_f16]_z(svbool_t pg, svfloat16_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; FCVTZU Zresult.S, Pg/M, Zop.H"
  public static unsafe Vector<uint> ConvertToUInt32(Vector<half> value);


    /// ConvertToUInt64 : Floating-point convert

    /// svuint64_t svcvt_u64[_f16]_m(svuint64_t inactive, svbool_t pg, svfloat16_t op) : "FCVTZU Ztied.D, Pg/M, Zop.H" or "MOVPRFX Zresult, Zinactive; FCVTZU Zresult.D, Pg/M, Zop.H"
    /// svuint64_t svcvt_u64[_f16]_x(svbool_t pg, svfloat16_t op) : "FCVTZU Ztied.D, Pg/M, Ztied.H" or "MOVPRFX Zresult, Zop; FCVTZU Zresult.D, Pg/M, Zop.H"
    /// svuint64_t svcvt_u64[_f16]_z(svbool_t pg, svfloat16_t op) : "MOVPRFX Zresult.D, Pg/Z, Zop.D; FCVTZU Zresult.D, Pg/M, Zop.H"
  public static unsafe Vector<ulong> ConvertToUInt64(Vector<half> value);


    /// CreateFalseMaskHalf : Set all predicate elements to false

    /// svbool_t svpfalse[_b]() : "PFALSE Presult.B"
  public static unsafe Vector<half> CreateFalseMaskHalf();


    /// CreateTrueMaskHalf : Set predicate elements to true

    /// svbool_t svptrue_pat_b8(enum svpattern pattern) : "PTRUE Presult.B, pattern"
  public static unsafe Vector<half> CreateTrueMaskHalf([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);


    /// CreateWhileReadAfterWriteMask : While free of read-after-write conflicts

    /// svbool_t svwhilerw[_f16](const float16_t *op1, const float16_t *op2) : "WHILERW Presult.H, Xop1, Xop2"
  public static unsafe Vector<half> CreateWhileReadAfterWriteMask(half* left, half* right);


    /// CreateWhileWriteAfterReadMask : While free of write-after-read conflicts

    /// svbool_t svwhilewr[_f16](const float16_t *op1, const float16_t *op2) : "WHILEWR Presult.H, Xop1, Xop2"
  public static unsafe Vector<half> CreateWhileWriteAfterReadMask(half* left, half* right);


    /// Divide : Divide

    /// svfloat16_t svdiv[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "FDIV Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; FDIV Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svfloat16_t svdiv[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "FDIV Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "FDIVR Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "MOVPRFX Zresult, Zop1; FDIV Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svfloat16_t svdiv[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; FDIV Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; FDIVR Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<half> Divide(Vector<half> left, Vector<half> right);


    /// DownConvertNarrowingUpper : Down convert and narrow (top)

    /// svfloat16_t svcvtnt_f16[_f32]_m(svfloat16_t even, svbool_t pg, svfloat32_t op) : "FCVTNT Ztied.H, Pg/M, Zop.S"
    /// svfloat16_t svcvtnt_f16[_f32]_x(svfloat16_t even, svbool_t pg, svfloat32_t op) : "FCVTNT Ztied.H, Pg/M, Zop.S"
  public static unsafe Vector<half> DownConvertNarrowingUpper(Vector<float> value);


    /// DuplicateSelectedScalarToVector : Broadcast a scalar value

    /// svfloat16_t svdup_lane[_f16](svfloat16_t data, uint16_t index) : "DUP Zresult.H, Zdata.H[index]" or "TBL Zresult.H, Zdata.H, Zindex.H"
    /// svfloat16_t svdupq_lane[_f16](svfloat16_t data, uint64_t index) : "DUP Zresult.Q, Zdata.Q[index]" or "TBL Zresult.D, Zdata.D, Zindices_d.D"
  public static unsafe Vector<half> DuplicateSelectedScalarToVector(Vector<half> data, [ConstantExpected] byte index);


    /// ExtractAfterLastScalar : Extract element after last

    /// float16_t svlasta[_f16](svbool_t pg, svfloat16_t op) : "LASTA Wresult, Pg, Zop.H" or "LASTA Hresult, Pg, Zop.H"
  public static unsafe half ExtractAfterLastScalar(Vector<half> value);


    /// ExtractAfterLastVector : Extract element after last

    /// float16_t svlasta[_f16](svbool_t pg, svfloat16_t op) : "LASTA Wresult, Pg, Zop.H" or "LASTA Hresult, Pg, Zop.H"
  public static unsafe Vector<half> ExtractAfterLastVector(Vector<half> value);


    /// ExtractLastScalar : Extract last element

    /// float16_t svlastb[_f16](svbool_t pg, svfloat16_t op) : "LASTB Wresult, Pg, Zop.H" or "LASTB Hresult, Pg, Zop.H"
  public static unsafe half ExtractLastScalar(Vector<half> value);


    /// ExtractLastVector : Extract last element

    /// float16_t svlastb[_f16](svbool_t pg, svfloat16_t op) : "LASTB Wresult, Pg, Zop.H" or "LASTB Hresult, Pg, Zop.H"
  public static unsafe Vector<half> ExtractLastVector(Vector<half> value);


    /// ExtractVector : Extract vector from pair of vectors

    /// svfloat16_t svext[_f16](svfloat16_t op1, svfloat16_t op2, uint64_t imm3) : "EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3 * 2" or "MOVPRFX Zresult, Zop1; EXT Zresult.B, Zresult.B, Zop2.B, #imm3 * 2"
  public static unsafe Vector<half> ExtractVector(Vector<half> upper, Vector<half> lower, [ConstantExpected] byte index);


    /// FloatingPointExponentialAccelerator : Floating-point exponential accelerator

    /// svfloat16_t svexpa[_f16](svuint16_t op) : "FEXPA Zresult.H, Zop.H"
  public static unsafe Vector<half> FloatingPointExponentialAccelerator(Vector<ushort> value);


    /// FusedMultiplyAdd : Multiply-add, addend first

    /// svfloat16_t svmla[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3) : "FMLA Ztied1.H, Pg/M, Zop2.H, Zop3.H" or "MOVPRFX Zresult, Zop1; FMLA Zresult.H, Pg/M, Zop2.H, Zop3.H"
    /// svfloat16_t svmla[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3) : "FMLA Ztied1.H, Pg/M, Zop2.H, Zop3.H" or "FMAD Ztied2.H, Pg/M, Zop3.H, Zop1.H" or "FMAD Ztied3.H, Pg/M, Zop2.H, Zop1.H" or "MOVPRFX Zresult, Zop1; FMLA Zresult.H, Pg/M, Zop2.H, Zop3.H"
    /// svfloat16_t svmla[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; FMLA Zresult.H, Pg/M, Zop2.H, Zop3.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; FMAD Zresult.H, Pg/M, Zop3.H, Zop1.H" or "MOVPRFX Zresult.H, Pg/Z, Zop3.H; FMAD Zresult.H, Pg/M, Zop2.H, Zop1.H"
  public static unsafe Vector<half> FusedMultiplyAdd(Vector<half> addend, Vector<half> left, Vector<half> right);


    /// FusedMultiplyAddBySelectedScalar : Multiply-add, addend first

    /// svfloat16_t svmla_lane[_f16](svfloat16_t op1, svfloat16_t op2, svfloat16_t op3, uint64_t imm_index) : "FMLA Ztied1.H, Zop2.H, Zop3.H[imm_index]" or "MOVPRFX Zresult, Zop1; FMLA Zresult.H, Zop2.H, Zop3.H[imm_index]"
  public static unsafe Vector<half> FusedMultiplyAddBySelectedScalar(Vector<half> addend, Vector<half> left, Vector<half> right, [ConstantExpected] byte rightIndex);


    /// FusedMultiplyAddNegated : Negated multiply-add, addend first

    /// svfloat16_t svnmla[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3) : "FNMLA Ztied1.H, Pg/M, Zop2.H, Zop3.H" or "MOVPRFX Zresult, Zop1; FNMLA Zresult.H, Pg/M, Zop2.H, Zop3.H"
    /// svfloat16_t svnmla[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3) : "FNMLA Ztied1.H, Pg/M, Zop2.H, Zop3.H" or "FNMAD Ztied2.H, Pg/M, Zop3.H, Zop1.H" or "FNMAD Ztied3.H, Pg/M, Zop2.H, Zop1.H" or "MOVPRFX Zresult, Zop1; FNMLA Zresult.H, Pg/M, Zop2.H, Zop3.H"
    /// svfloat16_t svnmla[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; FNMLA Zresult.H, Pg/M, Zop2.H, Zop3.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; FNMAD Zresult.H, Pg/M, Zop3.H, Zop1.H" or "MOVPRFX Zresult.H, Pg/Z, Zop3.H; FNMAD Zresult.H, Pg/M, Zop2.H, Zop1.H"
  public static unsafe Vector<half> FusedMultiplyAddNegated(Vector<half> addend, Vector<half> left, Vector<half> right);


    /// FusedMultiplySubtract : Multiply-subtract, minuend first

    /// svfloat16_t svmls[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3) : "FMLS Ztied1.H, Pg/M, Zop2.H, Zop3.H" or "MOVPRFX Zresult, Zop1; FMLS Zresult.H, Pg/M, Zop2.H, Zop3.H"
    /// svfloat16_t svmls[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3) : "FMLS Ztied1.H, Pg/M, Zop2.H, Zop3.H" or "FMSB Ztied2.H, Pg/M, Zop3.H, Zop1.H" or "FMSB Ztied3.H, Pg/M, Zop2.H, Zop1.H" or "MOVPRFX Zresult, Zop1; FMLS Zresult.H, Pg/M, Zop2.H, Zop3.H"
    /// svfloat16_t svmls[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; FMLS Zresult.H, Pg/M, Zop2.H, Zop3.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; FMSB Zresult.H, Pg/M, Zop3.H, Zop1.H" or "MOVPRFX Zresult.H, Pg/Z, Zop3.H; FMSB Zresult.H, Pg/M, Zop2.H, Zop1.H"
  public static unsafe Vector<half> FusedMultiplySubtract(Vector<half> minuend, Vector<half> left, Vector<half> right);


    /// FusedMultiplySubtractBySelectedScalar : Multiply-subtract, minuend first

    /// svfloat16_t svmls_lane[_f16](svfloat16_t op1, svfloat16_t op2, svfloat16_t op3, uint64_t imm_index) : "FMLS Ztied1.H, Zop2.H, Zop3.H[imm_index]" or "MOVPRFX Zresult, Zop1; FMLS Zresult.H, Zop2.H, Zop3.H[imm_index]"
  public static unsafe Vector<half> FusedMultiplySubtractBySelectedScalar(Vector<half> minuend, Vector<half> left, Vector<half> right, [ConstantExpected] byte rightIndex);


    /// FusedMultiplySubtractNegated : Negated multiply-subtract, minuend first

    /// svfloat16_t svnmls[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3) : "FNMLS Ztied1.H, Pg/M, Zop2.H, Zop3.H" or "MOVPRFX Zresult, Zop1; FNMLS Zresult.H, Pg/M, Zop2.H, Zop3.H"
    /// svfloat16_t svnmls[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3) : "FNMLS Ztied1.H, Pg/M, Zop2.H, Zop3.H" or "FNMSB Ztied2.H, Pg/M, Zop3.H, Zop1.H" or "FNMSB Ztied3.H, Pg/M, Zop2.H, Zop1.H" or "MOVPRFX Zresult, Zop1; FNMLS Zresult.H, Pg/M, Zop2.H, Zop3.H"
    /// svfloat16_t svnmls[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; FNMLS Zresult.H, Pg/M, Zop2.H, Zop3.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; FNMSB Zresult.H, Pg/M, Zop3.H, Zop1.H" or "MOVPRFX Zresult.H, Pg/Z, Zop3.H; FNMSB Zresult.H, Pg/M, Zop2.H, Zop1.H"
  public static unsafe Vector<half> FusedMultiplySubtractNegated(Vector<half> minuend, Vector<half> left, Vector<half> right);


    /// GetActiveElementCount : Count set predicate bits

    /// uint64_t svcntp_b8(svbool_t pg, svbool_t op) : "CNTP Xresult, Pg, Pop.B"
  public static unsafe ulong GetActiveElementCount(Vector<half> mask, Vector<half> from);


    /// InsertIntoShiftedVector : Insert scalar into shifted vector

    /// svfloat16_t svinsr[_n_f16](svfloat16_t op1, float16_t op2) : "INSR Ztied1.H, Wop2" or "INSR Ztied1.H, Hop2"
  public static unsafe Vector<half> InsertIntoShiftedVector(Vector<half> left, half right);


    /// InterleaveEvenInt128FromTwoInputs : Interleave even quadwords from two inputs

    /// svfloat16_t svtrn1q[_f16](svfloat16_t op1, svfloat16_t op2) : "TRN1 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<half> InterleaveEvenInt128FromTwoInputs(Vector<half> left, Vector<half> right);


    /// InterleaveInt128FromHighHalvesOfTwoInputs : Interleave quadwords from high halves of two inputs

    /// svfloat16_t svzip2q[_f16](svfloat16_t op1, svfloat16_t op2) : "ZIP2 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<half> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<half> left, Vector<half> right);


    /// InterleaveInt128FromLowHalvesOfTwoInputs : Interleave quadwords from low halves of two inputs

    /// svfloat16_t svzip1q[_f16](svfloat16_t op1, svfloat16_t op2) : "ZIP1 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<half> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<half> left, Vector<half> right);


    /// InterleaveOddInt128FromTwoInputs : Interleave odd quadwords from two inputs

    /// svfloat16_t svtrn2q[_f16](svfloat16_t op1, svfloat16_t op2) : "TRN2 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<half> InterleaveOddInt128FromTwoInputs(Vector<half> left, Vector<half> right);


    /// LoadVector : Unextended load

    /// svfloat16_t svld1[_f16](svbool_t pg, const float16_t *base) : "LD1H Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]" or "LD1H Zresult.H, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<half> LoadVector(Vector<half> mask, half* address);


    /// LoadVector128AndReplicateToVector : Load and replicate 128 bits of data

    /// svfloat16_t svld1rq[_f16](svbool_t pg, const float16_t *base) : "LD1RQH Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]" or "LD1RQH Zresult.H, Pg/Z, [Xarray, #index * 2]" or "LD1RQH Zresult.H, Pg/Z, [Xbase, #0]"
  public static unsafe Vector<half> LoadVector128AndReplicateToVector(Vector<half> mask, half* address);


    /// LoadVector256AndReplicateToVector : Load and replicate 256 bits of data

    /// svfloat16_t svld1ro[_f16](svbool_t pg, const float16_t *base) : "LD1ROH Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]" or "LD1ROH Zresult.H, Pg/Z, [Xarray, #index * 2]" or "LD1ROH Zresult.H, Pg/Z, [Xbase, #0]"
  public static unsafe Vector<half> LoadVector256AndReplicateToVector(Vector<half> mask, half* address);


    /// LoadVectorFirstFaulting : Unextended load, first-faulting

    /// svfloat16_t svldff1[_f16](svbool_t pg, const float16_t *base) : "LDFF1H Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]" or "LDFF1H Zresult.H, Pg/Z, [Xbase, XZR, LSL #1]"
  public static unsafe Vector<half> LoadVectorFirstFaulting(Vector<half> mask, half* address);


    /// LoadVectorNonFaulting : Unextended load, non-faulting

    /// svfloat16_t svldnf1[_f16](svbool_t pg, const float16_t *base) : "LDNF1H Zresult.H, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<half> LoadVectorNonFaulting(half* address);


    /// LoadVectorNonTemporal : Unextended load, non-temporal

    /// svfloat16_t svldnt1[_f16](svbool_t pg, const float16_t *base) : "LDNT1H Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]" or "LDNT1H Zresult.H, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<half> LoadVectorNonTemporal(Vector<half> mask, half* address);


    /// LoadVectorx2 : Load two-element tuples into two vectors

    /// svfloat16x2_t svld2[_f16](svbool_t pg, const float16_t *base) : "LD2H {Zresult0.H, Zresult1.H}, Pg/Z, [Xarray, Xindex, LSL #1]" or "LD2H {Zresult0.H, Zresult1.H}, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe (Vector<half>, Vector<half>) LoadVectorx2(Vector<half> mask, half* address);


    /// LoadVectorx3 : Load three-element tuples into three vectors

    /// svfloat16x3_t svld3[_f16](svbool_t pg, const float16_t *base) : "LD3H {Zresult0.H - Zresult2.H}, Pg/Z, [Xarray, Xindex, LSL #1]" or "LD3H {Zresult0.H - Zresult2.H}, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe (Vector<half>, Vector<half>, Vector<half>) LoadVectorx3(Vector<half> mask, half* address);


    /// LoadVectorx4 : Load four-element tuples into four vectors

    /// svfloat16x4_t svld4[_f16](svbool_t pg, const float16_t *base) : "LD4H {Zresult0.H - Zresult3.H}, Pg/Z, [Xarray, Xindex, LSL #1]" or "LD4H {Zresult0.H - Zresult3.H}, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe (Vector<half>, Vector<half>, Vector<half>, Vector<half>) LoadVectorx4(Vector<half> mask, half* address);


    /// Log2 : Base 2 logarithm as integer

    /// svint16_t svlogb[_f16]_m(svint16_t inactive, svbool_t pg, svfloat16_t op) : "FLOGB Ztied.H, Pg/M, Zop.H" or "MOVPRFX Zresult, Zinactive; FLOGB Zresult.H, Pg/M, Zop.H"
    /// svint16_t svlogb[_f16]_x(svbool_t pg, svfloat16_t op) : "FLOGB Ztied.H, Pg/M, Ztied.H" or "MOVPRFX Zresult, Zop; FLOGB Zresult.H, Pg/M, Zop.H"
    /// svint16_t svlogb[_f16]_z(svbool_t pg, svfloat16_t op) : "MOVPRFX Zresult.H, Pg/Z, Zop.H; FLOGB Zresult.H, Pg/M, Zop.H"
  public static unsafe Vector<short> Log2(Vector<half> value);


    /// Max : Maximum

    /// svfloat16_t svmax[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "FMAX Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; FMAX Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svfloat16_t svmax[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "FMAX Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "FMAX Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "MOVPRFX Zresult, Zop1; FMAX Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svfloat16_t svmax[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; FMAX Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; FMAX Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<half> Max(Vector<half> left, Vector<half> right);


    /// MaxAcross : Maximum reduction to scalar

    /// float16_t svmaxv[_f16](svbool_t pg, svfloat16_t op) : "FMAXV Hresult, Pg, Zop.H"
  public static unsafe Vector<half> MaxAcross(Vector<half> value);


    /// MaxNumber : Maximum number

    /// svfloat16_t svmaxnm[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "FMAXNM Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; FMAXNM Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svfloat16_t svmaxnm[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "FMAXNM Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "FMAXNM Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "MOVPRFX Zresult, Zop1; FMAXNM Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svfloat16_t svmaxnm[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; FMAXNM Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; FMAXNM Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<half> MaxNumber(Vector<half> left, Vector<half> right);


    /// MaxNumberAcross : Maximum number reduction to scalar

    /// float16_t svmaxnmv[_f16](svbool_t pg, svfloat16_t op) : "FMAXNMV Hresult, Pg, Zop.H"
  public static unsafe Vector<half> MaxNumberAcross(Vector<half> value);


    /// MaxNumberPairwise : Maximum number pairwise

    /// svfloat16_t svmaxnmp[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "FMAXNMP Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; FMAXNMP Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svfloat16_t svmaxnmp[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "FMAXNMP Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; FMAXNMP Zresult.H, Pg/M, Zresult.H, Zop2.H"
  public static unsafe Vector<half> MaxNumberPairwise(Vector<half> left, Vector<half> right);


    /// MaxPairwise : Maximum pairwise

    /// svfloat16_t svmaxp[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "FMAXP Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; FMAXP Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svfloat16_t svmaxp[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "FMAXP Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; FMAXP Zresult.H, Pg/M, Zresult.H, Zop2.H"
  public static unsafe Vector<half> MaxPairwise(Vector<half> left, Vector<half> right);


    /// Min : Minimum

    /// svfloat16_t svmin[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "FMIN Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; FMIN Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svfloat16_t svmin[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "FMIN Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "FMIN Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "MOVPRFX Zresult, Zop1; FMIN Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svfloat16_t svmin[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; FMIN Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; FMIN Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<half> Min(Vector<half> left, Vector<half> right);


    /// MinAcross : Minimum reduction to scalar

    /// float16_t svminv[_f16](svbool_t pg, svfloat16_t op) : "FMINV Hresult, Pg, Zop.H"
  public static unsafe Vector<half> MinAcross(Vector<half> value);


    /// MinNumber : Minimum number

    /// svfloat16_t svminnm[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "FMINNM Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; FMINNM Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svfloat16_t svminnm[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "FMINNM Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "FMINNM Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "MOVPRFX Zresult, Zop1; FMINNM Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svfloat16_t svminnm[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; FMINNM Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; FMINNM Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<half> MinNumber(Vector<half> left, Vector<half> right);


    /// MinNumberAcross : Minimum number reduction to scalar

    /// float16_t svminnmv[_f16](svbool_t pg, svfloat16_t op) : "FMINNMV Hresult, Pg, Zop.H"
  public static unsafe Vector<half> MinNumberAcross(Vector<half> value);


    /// MinNumberPairwise : Minimum number pairwise

    /// svfloat16_t svminnmp[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "FMINNMP Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; FMINNMP Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svfloat16_t svminnmp[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "FMINNMP Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; FMINNMP Zresult.H, Pg/M, Zresult.H, Zop2.H"
  public static unsafe Vector<half> MinNumberPairwise(Vector<half> left, Vector<half> right);


    /// MinPairwise : Minimum pairwise

    /// svfloat16_t svminp[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "FMINP Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; FMINP Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svfloat16_t svminp[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "FMINP Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; FMINP Zresult.H, Pg/M, Zresult.H, Zop2.H"
  public static unsafe Vector<half> MinPairwise(Vector<half> left, Vector<half> right);


    /// Multiply : Multiply

    /// svfloat16_t svmul[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "FMUL Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; FMUL Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svfloat16_t svmul[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "FMUL Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "FMUL Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "FMUL Zresult.H, Zop1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; FMUL Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svfloat16_t svmul[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; FMUL Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; FMUL Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<half> Multiply(Vector<half> left, Vector<half> right);


    /// MultiplyAddRotateComplex : Complex multiply-add with rotate

    /// svfloat16_t svcmla[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3, uint64_t imm_rotation) : "FCMLA Ztied1.H, Pg/M, Zop2.H, Zop3.H, #imm_rotation" or "MOVPRFX Zresult, Zop1; FCMLA Zresult.H, Pg/M, Zop2.H, Zop3.H, #imm_rotation"
    /// svfloat16_t svcmla[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3, uint64_t imm_rotation) : "FCMLA Ztied1.H, Pg/M, Zop2.H, Zop3.H, #imm_rotation" or "MOVPRFX Zresult, Zop1; FCMLA Zresult.H, Pg/M, Zop2.H, Zop3.H, #imm_rotation"
    /// svfloat16_t svcmla[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3, uint64_t imm_rotation) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; FCMLA Zresult.H, Pg/M, Zop2.H, Zop3.H, #imm_rotation"
  public static unsafe Vector<half> MultiplyAddRotateComplex(Vector<half> addend, Vector<half> left, Vector<half> right, [ConstantExpected] byte rotation);


    /// MultiplyAddRotateComplexBySelectedScalar : Complex multiply-add with rotate

    /// svfloat16_t svcmla_lane[_f16](svfloat16_t op1, svfloat16_t op2, svfloat16_t op3, uint64_t imm_index, uint64_t imm_rotation) : "FCMLA Ztied1.H, Zop2.H, Zop3.H[imm_index], #imm_rotation" or "MOVPRFX Zresult, Zop1; FCMLA Zresult.H, Zop2.H, Zop3.H[imm_index], #imm_rotation"
  public static unsafe Vector<half> MultiplyAddRotateComplexBySelectedScalar(Vector<half> addend, Vector<half> left, Vector<half> right, [ConstantExpected] byte rightIndex, [ConstantExpected] byte rotation);


    /// MultiplyAddWideningLower : Multiply-add long (bottom)

    /// svfloat32_t svmlalb[_f32](svfloat32_t op1, svfloat16_t op2, svfloat16_t op3) : "FMLALB Ztied1.S, Zop2.H, Zop3.H" or "MOVPRFX Zresult, Zop1; FMLALB Zresult.S, Zop2.H, Zop3.H"
  public static unsafe Vector<float> MultiplyAddWideningLower(Vector<float> op1, Vector<half> op2, Vector<half> op3);

    /// svfloat32_t svmlalb_lane[_f32](svfloat32_t op1, svfloat16_t op2, svfloat16_t op3, uint64_t imm_index) : "FMLALB Ztied1.S, Zop2.H, Zop3.H[imm_index]" or "MOVPRFX Zresult, Zop1; FMLALB Zresult.S, Zop2.H, Zop3.H[imm_index]"
  public static unsafe Vector<float> MultiplyAddWideningLower(Vector<float> op1, Vector<half> op2, Vector<half> op3, ulong imm_index);


    /// MultiplyAddWideningUpper : Multiply-add long (top)

    /// svfloat32_t svmlalt[_f32](svfloat32_t op1, svfloat16_t op2, svfloat16_t op3) : "FMLALT Ztied1.S, Zop2.H, Zop3.H" or "MOVPRFX Zresult, Zop1; FMLALT Zresult.S, Zop2.H, Zop3.H"
  public static unsafe Vector<float> MultiplyAddWideningUpper(Vector<float> op1, Vector<half> op2, Vector<half> op3);

    /// svfloat32_t svmlalt_lane[_f32](svfloat32_t op1, svfloat16_t op2, svfloat16_t op3, uint64_t imm_index) : "FMLALT Ztied1.S, Zop2.H, Zop3.H[imm_index]" or "MOVPRFX Zresult, Zop1; FMLALT Zresult.S, Zop2.H, Zop3.H[imm_index]"
  public static unsafe Vector<float> MultiplyAddWideningUpper(Vector<float> op1, Vector<half> op2, Vector<half> op3, ulong imm_index);


    /// MultiplyBySelectedScalar : Multiply

    /// svfloat16_t svmul_lane[_f16](svfloat16_t op1, svfloat16_t op2, uint64_t imm_index) : "FMUL Zresult.H, Zop1.H, Zop2.H[imm_index]"
  public static unsafe Vector<half> MultiplyBySelectedScalar(Vector<half> left, Vector<half> right, [ConstantExpected] byte rightIndex);


    /// MultiplyExtended : Multiply extended (∞×0=2)

    /// svfloat16_t svmulx[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "FMULX Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; FMULX Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svfloat16_t svmulx[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "FMULX Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "FMULX Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "MOVPRFX Zresult, Zop1; FMULX Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svfloat16_t svmulx[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; FMULX Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; FMULX Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<half> MultiplyExtended(Vector<half> left, Vector<half> right);


    /// MultiplySubtractWideningLower : Multiply-subtract long (bottom)

    /// svfloat32_t svmlslb[_f32](svfloat32_t op1, svfloat16_t op2, svfloat16_t op3) : "FMLSLB Ztied1.S, Zop2.H, Zop3.H" or "MOVPRFX Zresult, Zop1; FMLSLB Zresult.S, Zop2.H, Zop3.H"
  public static unsafe Vector<float> MultiplySubtractWideningLower(Vector<float> op1, Vector<half> op2, Vector<half> op3);

    /// svfloat32_t svmlslb_lane[_f32](svfloat32_t op1, svfloat16_t op2, svfloat16_t op3, uint64_t imm_index) : "FMLSLB Ztied1.S, Zop2.H, Zop3.H[imm_index]" or "MOVPRFX Zresult, Zop1; FMLSLB Zresult.S, Zop2.H, Zop3.H[imm_index]"
  public static unsafe Vector<float> MultiplySubtractWideningLower(Vector<float> op1, Vector<half> op2, Vector<half> op3, ulong imm_index);


    /// MultiplySubtractWideningUpper : Multiply-subtract long (top)

    /// svfloat32_t svmlslt[_f32](svfloat32_t op1, svfloat16_t op2, svfloat16_t op3) : "FMLSLT Ztied1.S, Zop2.H, Zop3.H" or "MOVPRFX Zresult, Zop1; FMLSLT Zresult.S, Zop2.H, Zop3.H"
  public static unsafe Vector<float> MultiplySubtractWideningUpper(Vector<float> op1, Vector<half> op2, Vector<half> op3);

    /// svfloat32_t svmlslt_lane[_f32](svfloat32_t op1, svfloat16_t op2, svfloat16_t op3, uint64_t imm_index) : "FMLSLT Ztied1.S, Zop2.H, Zop3.H[imm_index]" or "MOVPRFX Zresult, Zop1; FMLSLT Zresult.S, Zop2.H, Zop3.H[imm_index]"
  public static unsafe Vector<float> MultiplySubtractWideningUpper(Vector<float> op1, Vector<half> op2, Vector<half> op3, ulong imm_index);


    /// Negate : Negate

    /// svfloat16_t svneg[_f16]_m(svfloat16_t inactive, svbool_t pg, svfloat16_t op) : "FNEG Ztied.H, Pg/M, Zop.H" or "MOVPRFX Zresult, Zinactive; FNEG Zresult.H, Pg/M, Zop.H"
    /// svfloat16_t svneg[_f16]_x(svbool_t pg, svfloat16_t op) : "FNEG Ztied.H, Pg/M, Ztied.H" or "MOVPRFX Zresult, Zop; FNEG Zresult.H, Pg/M, Zop.H"
    /// svfloat16_t svneg[_f16]_z(svbool_t pg, svfloat16_t op) : "MOVPRFX Zresult.H, Pg/Z, Zop.H; FNEG Zresult.H, Pg/M, Zop.H"
  public static unsafe Vector<half> Negate(Vector<half> value);


    /// PopCount : Count nonzero bits

    /// svuint16_t svcnt[_f16]_m(svuint16_t inactive, svbool_t pg, svfloat16_t op) : "CNT Ztied.H, Pg/M, Zop.H" or "MOVPRFX Zresult, Zinactive; CNT Zresult.H, Pg/M, Zop.H"
    /// svuint16_t svcnt[_f16]_x(svbool_t pg, svfloat16_t op) : "CNT Ztied.H, Pg/M, Ztied.H" or "MOVPRFX Zresult, Zop; CNT Zresult.H, Pg/M, Zop.H"
    /// svuint16_t svcnt[_f16]_z(svbool_t pg, svfloat16_t op) : "MOVPRFX Zresult.H, Pg/Z, Zop.H; CNT Zresult.H, Pg/M, Zop.H"
  public static unsafe Vector<ushort> PopCount(Vector<half> value);


    /// ReciprocalEstimate : Reciprocal estimate

    /// svfloat16_t svrecpe[_f16](svfloat16_t op) : "FRECPE Zresult.H, Zop.H"
  public static unsafe Vector<half> ReciprocalEstimate(Vector<half> value);


    /// ReciprocalExponent : Reciprocal exponent

    /// svfloat16_t svrecpx[_f16]_m(svfloat16_t inactive, svbool_t pg, svfloat16_t op) : "FRECPX Ztied.H, Pg/M, Zop.H" or "MOVPRFX Zresult, Zinactive; FRECPX Zresult.H, Pg/M, Zop.H"
    /// svfloat16_t svrecpx[_f16]_x(svbool_t pg, svfloat16_t op) : "FRECPX Ztied.H, Pg/M, Ztied.H" or "MOVPRFX Zresult, Zop; FRECPX Zresult.H, Pg/M, Zop.H"
    /// svfloat16_t svrecpx[_f16]_z(svbool_t pg, svfloat16_t op) : "MOVPRFX Zresult.H, Pg/Z, Zop.H; FRECPX Zresult.H, Pg/M, Zop.H"
  public static unsafe Vector<half> ReciprocalExponent(Vector<half> value);


    /// ReciprocalSqrtEstimate : Reciprocal square root estimate

    /// svfloat16_t svrsqrte[_f16](svfloat16_t op) : "FRSQRTE Zresult.H, Zop.H"
  public static unsafe Vector<half> ReciprocalSqrtEstimate(Vector<half> value);


    /// ReciprocalSqrtStep : Reciprocal square root step

    /// svfloat16_t svrsqrts[_f16](svfloat16_t op1, svfloat16_t op2) : "FRSQRTS Zresult.H, Zop1.H, Zop2.H"
  public static unsafe Vector<half> ReciprocalSqrtStep(Vector<half> left, Vector<half> right);


    /// ReciprocalStep : Reciprocal step

    /// svfloat16_t svrecps[_f16](svfloat16_t op1, svfloat16_t op2) : "FRECPS Zresult.H, Zop1.H, Zop2.H"
  public static unsafe Vector<half> ReciprocalStep(Vector<half> left, Vector<half> right);


    /// ReverseElement : Reverse all elements

    /// svfloat16_t svrev[_f16](svfloat16_t op) : "REV Zresult.H, Zop.H"
  public static unsafe Vector<half> ReverseElement(Vector<half> value);


    /// RoundAwayFromZero : Round to nearest, ties away from zero

    /// svfloat16_t svrinta[_f16]_m(svfloat16_t inactive, svbool_t pg, svfloat16_t op) : "FRINTA Ztied.H, Pg/M, Zop.H" or "MOVPRFX Zresult, Zinactive; FRINTA Zresult.H, Pg/M, Zop.H"
    /// svfloat16_t svrinta[_f16]_x(svbool_t pg, svfloat16_t op) : "FRINTA Ztied.H, Pg/M, Ztied.H" or "MOVPRFX Zresult, Zop; FRINTA Zresult.H, Pg/M, Zop.H"
    /// svfloat16_t svrinta[_f16]_z(svbool_t pg, svfloat16_t op) : "MOVPRFX Zresult.H, Pg/Z, Zop.H; FRINTA Zresult.H, Pg/M, Zop.H"
  public static unsafe Vector<half> RoundAwayFromZero(Vector<half> value);


    /// RoundToNearest : Round to nearest, ties to even

    /// svfloat16_t svrintn[_f16]_m(svfloat16_t inactive, svbool_t pg, svfloat16_t op) : "FRINTN Ztied.H, Pg/M, Zop.H" or "MOVPRFX Zresult, Zinactive; FRINTN Zresult.H, Pg/M, Zop.H"
    /// svfloat16_t svrintn[_f16]_x(svbool_t pg, svfloat16_t op) : "FRINTN Ztied.H, Pg/M, Ztied.H" or "MOVPRFX Zresult, Zop; FRINTN Zresult.H, Pg/M, Zop.H"
    /// svfloat16_t svrintn[_f16]_z(svbool_t pg, svfloat16_t op) : "MOVPRFX Zresult.H, Pg/Z, Zop.H; FRINTN Zresult.H, Pg/M, Zop.H"
  public static unsafe Vector<half> RoundToNearest(Vector<half> value);


    /// RoundToNegativeInfinity : Round towards -∞

    /// svfloat16_t svrintm[_f16]_m(svfloat16_t inactive, svbool_t pg, svfloat16_t op) : "FRINTM Ztied.H, Pg/M, Zop.H" or "MOVPRFX Zresult, Zinactive; FRINTM Zresult.H, Pg/M, Zop.H"
    /// svfloat16_t svrintm[_f16]_x(svbool_t pg, svfloat16_t op) : "FRINTM Ztied.H, Pg/M, Ztied.H" or "MOVPRFX Zresult, Zop; FRINTM Zresult.H, Pg/M, Zop.H"
    /// svfloat16_t svrintm[_f16]_z(svbool_t pg, svfloat16_t op) : "MOVPRFX Zresult.H, Pg/Z, Zop.H; FRINTM Zresult.H, Pg/M, Zop.H"
  public static unsafe Vector<half> RoundToNegativeInfinity(Vector<half> value);


    /// RoundToPositiveInfinity : Round towards +∞

    /// svfloat16_t svrintp[_f16]_m(svfloat16_t inactive, svbool_t pg, svfloat16_t op) : "FRINTP Ztied.H, Pg/M, Zop.H" or "MOVPRFX Zresult, Zinactive; FRINTP Zresult.H, Pg/M, Zop.H"
    /// svfloat16_t svrintp[_f16]_x(svbool_t pg, svfloat16_t op) : "FRINTP Ztied.H, Pg/M, Ztied.H" or "MOVPRFX Zresult, Zop; FRINTP Zresult.H, Pg/M, Zop.H"
    /// svfloat16_t svrintp[_f16]_z(svbool_t pg, svfloat16_t op) : "MOVPRFX Zresult.H, Pg/Z, Zop.H; FRINTP Zresult.H, Pg/M, Zop.H"
  public static unsafe Vector<half> RoundToPositiveInfinity(Vector<half> value);


    /// RoundToZero : Round towards zero

    /// svfloat16_t svrintz[_f16]_m(svfloat16_t inactive, svbool_t pg, svfloat16_t op) : "FRINTZ Ztied.H, Pg/M, Zop.H" or "MOVPRFX Zresult, Zinactive; FRINTZ Zresult.H, Pg/M, Zop.H"
    /// svfloat16_t svrintz[_f16]_x(svbool_t pg, svfloat16_t op) : "FRINTZ Ztied.H, Pg/M, Ztied.H" or "MOVPRFX Zresult, Zop; FRINTZ Zresult.H, Pg/M, Zop.H"
    /// svfloat16_t svrintz[_f16]_z(svbool_t pg, svfloat16_t op) : "MOVPRFX Zresult.H, Pg/Z, Zop.H; FRINTZ Zresult.H, Pg/M, Zop.H"
  public static unsafe Vector<half> RoundToZero(Vector<half> value);


    /// Scale : Adjust exponent

    /// svfloat16_t svscale[_f16]_m(svbool_t pg, svfloat16_t op1, svint16_t op2) : "FSCALE Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; FSCALE Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svfloat16_t svscale[_f16]_x(svbool_t pg, svfloat16_t op1, svint16_t op2) : "FSCALE Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; FSCALE Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svfloat16_t svscale[_f16]_z(svbool_t pg, svfloat16_t op1, svint16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; FSCALE Zresult.H, Pg/M, Zresult.H, Zop2.H"
  public static unsafe Vector<half> Scale(Vector<half> left, Vector<short> right);


    /// Splice : Splice two vectors under predicate control

    /// svfloat16_t svsplice[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "SPLICE Ztied1.H, Pg, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; SPLICE Zresult.H, Pg, Zresult.H, Zop2.H"
  public static unsafe Vector<half> Splice(Vector<half> mask, Vector<half> left, Vector<half> right);


    /// Sqrt : Square root

    /// svfloat16_t svsqrt[_f16]_m(svfloat16_t inactive, svbool_t pg, svfloat16_t op) : "FSQRT Ztied.H, Pg/M, Zop.H" or "MOVPRFX Zresult, Zinactive; FSQRT Zresult.H, Pg/M, Zop.H"
    /// svfloat16_t svsqrt[_f16]_x(svbool_t pg, svfloat16_t op) : "FSQRT Ztied.H, Pg/M, Ztied.H" or "MOVPRFX Zresult, Zop; FSQRT Zresult.H, Pg/M, Zop.H"
    /// svfloat16_t svsqrt[_f16]_z(svbool_t pg, svfloat16_t op) : "MOVPRFX Zresult.H, Pg/Z, Zop.H; FSQRT Zresult.H, Pg/M, Zop.H"
  public static unsafe Vector<half> Sqrt(Vector<half> value);


    /// Store : Non-truncating store

    /// void svst1[_f16](svbool_t pg, float16_t *base, svfloat16_t data) : "ST1H Zdata.H, Pg, [Xarray, Xindex, LSL #1]" or "ST1H Zdata.H, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<half> mask, half* address, Vector<half> data);

    /// void svst2[_f16](svbool_t pg, float16_t *base, svfloat16x2_t data) : "ST2H {Zdata0.H, Zdata1.H}, Pg, [Xarray, Xindex, LSL #1]" or "ST2H {Zdata0.H, Zdata1.H}, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<half> mask, half* address, (Vector<half> Value1, Vector<half> Value2) data);

    /// void svst3[_f16](svbool_t pg, float16_t *base, svfloat16x3_t data) : "ST3H {Zdata0.H - Zdata2.H}, Pg, [Xarray, Xindex, LSL #1]" or "ST3H {Zdata0.H - Zdata2.H}, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<half> mask, half* address, (Vector<half> Value1, Vector<half> Value2, Vector<half> Value3) data);

    /// void svst4[_f16](svbool_t pg, float16_t *base, svfloat16x4_t data) : "ST4H {Zdata0.H - Zdata3.H}, Pg, [Xarray, Xindex, LSL #1]" or "ST4H {Zdata0.H - Zdata3.H}, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<half> mask, half* address, (Vector<half> Value1, Vector<half> Value2, Vector<half> Value3, Vector<half> Value4) data);


    /// StoreNonTemporal : Non-truncating store, non-temporal

    /// void svstnt1[_f16](svbool_t pg, float16_t *base, svfloat16_t data) : "STNT1H Zdata.H, Pg, [Xarray, Xindex, LSL #1]" or "STNT1H Zdata.H, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void StoreNonTemporal(Vector<half> mask, half* address, Vector<half> data);


    /// Subtract : Subtract

    /// svfloat16_t svsub[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "FSUB Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; FSUB Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svfloat16_t svsub[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "FSUB Ztied1.H, Pg/M, Ztied1.H, Zop2.H" or "FSUBR Ztied2.H, Pg/M, Ztied2.H, Zop1.H" or "FSUB Zresult.H, Zop1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; FSUB Zresult.H, Pg/M, Zresult.H, Zop2.H"
    /// svfloat16_t svsub[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2) : "MOVPRFX Zresult.H, Pg/Z, Zop1.H; FSUB Zresult.H, Pg/M, Zresult.H, Zop2.H" or "MOVPRFX Zresult.H, Pg/Z, Zop2.H; FSUBR Zresult.H, Pg/M, Zresult.H, Zop1.H"
  public static unsafe Vector<half> Subtract(Vector<half> left, Vector<half> right);


    /// TransposeEven : Interleave even elements from two inputs

    /// svfloat16_t svtrn1[_f16](svfloat16_t op1, svfloat16_t op2) : "TRN1 Zresult.H, Zop1.H, Zop2.H"
  public static unsafe Vector<half> TransposeEven(Vector<half> left, Vector<half> right);


    /// TransposeOdd : Interleave odd elements from two inputs

    /// svfloat16_t svtrn2[_f16](svfloat16_t op1, svfloat16_t op2) : "TRN2 Zresult.H, Zop1.H, Zop2.H"
  public static unsafe Vector<half> TransposeOdd(Vector<half> left, Vector<half> right);


    /// TrigonometricMultiplyAddCoefficient : Trigonometric multiply-add coefficient

    /// svfloat16_t svtmad[_f16](svfloat16_t op1, svfloat16_t op2, uint64_t imm3) : "FTMAD Ztied1.H, Ztied1.H, Zop2.H, #imm3" or "MOVPRFX Zresult, Zop1; FTMAD Zresult.H, Zresult.H, Zop2.H, #imm3"
  public static unsafe Vector<half> TrigonometricMultiplyAddCoefficient(Vector<half> left, Vector<half> right, [ConstantExpected] byte control);


    /// TrigonometricSelectCoefficient : Trigonometric select coefficient

    /// svfloat16_t svtssel[_f16](svfloat16_t op1, svuint16_t op2) : "FTSSEL Zresult.H, Zop1.H, Zop2.H"
  public static unsafe Vector<half> TrigonometricSelectCoefficient(Vector<half> value, Vector<ushort> selector);


    /// TrigonometricStartingValue : Trigonometric starting value

    /// svfloat16_t svtsmul[_f16](svfloat16_t op1, svuint16_t op2) : "FTSMUL Zresult.H, Zop1.H, Zop2.H"
  public static unsafe Vector<half> TrigonometricStartingValue(Vector<half> value, Vector<ushort> sign);


    /// UnzipEven : Concatenate even elements from two inputs

    /// svfloat16_t svuzp1[_f16](svfloat16_t op1, svfloat16_t op2) : "UZP1 Zresult.H, Zop1.H, Zop2.H"
  public static unsafe Vector<half> UnzipEven(Vector<half> left, Vector<half> right);


    /// UnzipOdd : Concatenate odd elements from two inputs

    /// svfloat16_t svuzp2[_f16](svfloat16_t op1, svfloat16_t op2) : "UZP2 Zresult.H, Zop1.H, Zop2.H"
  public static unsafe Vector<half> UnzipOdd(Vector<half> left, Vector<half> right);


    /// UpConvertWideningUpper : Up convert long (top)

    /// svfloat32_t svcvtlt_f32[_f16]_m(svfloat32_t inactive, svbool_t pg, svfloat16_t op) : "FCVTLT Ztied.S, Pg/M, Zop.H"
    /// svfloat32_t svcvtlt_f32[_f16]_x(svbool_t pg, svfloat16_t op) : "FCVTLT Ztied.S, Pg/M, Ztied.H"
  public static unsafe Vector<float> UpConvertWideningUpper(Vector<half> value);


    /// VectorTableLookup : Table lookup in single-vector table

    /// svfloat16_t svtbl[_f16](svfloat16_t data, svuint16_t indices) : "TBL Zresult.H, Zdata.H, Zindices.H"
  public static unsafe Vector<half> VectorTableLookup(Vector<half> data, Vector<ushort> indices);

    /// svfloat16_t svtbl2[_f16](svfloat16x2_t data, svuint16_t indices) : "TBL Zresult.H, {Zdata0.H, Zdata1.H}, Zindices.H"
  public static unsafe Vector<half> VectorTableLookup((Vector<half> data1, Vector<half> data2), Vector<ushort> indices);


    /// VectorTableLookupExtension : Table lookup in single-vector table (merging)

    /// svfloat16_t svtbx[_f16](svfloat16_t fallback, svfloat16_t data, svuint16_t indices) : "TBX Ztied.H, Zdata.H, Zindices.H"
  public static unsafe Vector<half> VectorTableLookupExtension(Vector<half> fallback, Vector<half> data, Vector<ushort> indices);


    /// ZipHigh : Interleave elements from high halves of two inputs

    /// svfloat16_t svzip2[_f16](svfloat16_t op1, svfloat16_t op2) : "ZIP2 Zresult.H, Zop1.H, Zop2.H"
  public static unsafe Vector<half> ZipHigh(Vector<half> left, Vector<half> right);


    /// ZipLow : Interleave elements from low halves of two inputs

    /// svfloat16_t svzip1[_f16](svfloat16_t op1, svfloat16_t op2) : "ZIP1 Zresult.H, Zop1.H, Zop2.H"
  public static unsafe Vector<half> ZipLow(Vector<half> left, Vector<half> right);


  /// total method signatures: 138
  /// total method names:      134
}

  /// Optional Entries:
  ///   public static unsafe Vector<half> AbsoluteCompareGreaterThan(Vector<half> left, half right); // svacgt[_n_f16]
  ///   public static unsafe Vector<half> AbsoluteCompareGreaterThanOrEqual(Vector<half> left, half right); // svacge[_n_f16]
  ///   public static unsafe Vector<half> AbsoluteCompareLessThan(Vector<half> left, half right); // svaclt[_n_f16]
  ///   public static unsafe Vector<half> AbsoluteCompareLessThanOrEqual(Vector<half> left, half right); // svacle[_n_f16]
  ///   public static unsafe Vector<half> AbsoluteDifference(Vector<half> left, half right); // svabd[_n_f16]_m or svabd[_n_f16]_x or svabd[_n_f16]_z
  ///   public static unsafe Vector<half> Add(Vector<half> left, half right); // svadd[_n_f16]_m or svadd[_n_f16]_x or svadd[_n_f16]_z
  ///   public static unsafe Vector<half> CompareEqual(Vector<half> left, half right); // svcmpeq[_n_f16]
  ///   public static unsafe Vector<half> CompareGreaterThan(Vector<half> left, half right); // svcmpgt[_n_f16]
  ///   public static unsafe Vector<half> CompareGreaterThanOrEqual(Vector<half> left, half right); // svcmpge[_n_f16]
  ///   public static unsafe Vector<half> CompareLessThan(Vector<half> left, half right); // svcmplt[_n_f16]
  ///   public static unsafe Vector<half> CompareLessThanOrEqual(Vector<half> left, half right); // svcmple[_n_f16]
  ///   public static unsafe Vector<half> CompareNotEqualTo(Vector<half> left, half right); // svcmpne[_n_f16]
  ///   public static unsafe Vector<half> CompareUnordered(Vector<half> left, half right); // svcmpuo[_n_f16]
  ///   public static unsafe half ConditionalExtractAfterLastActiveElement(Vector<half> mask, half defaultValue, Vector<half> data); // svclasta[_n_f16]
  ///   public static unsafe half ConditionalExtractAfterLastActiveElementAndReplicate(Vector<half> mask, half defaultScalar, Vector<half> data); // svclasta[_n_f16]
  ///   public static unsafe half ConditionalExtractLastActiveElement(Vector<half> mask, half defaultValue, Vector<half> data); // svclastb[_n_f16]
  ///   public static unsafe half ConditionalExtractLastActiveElementAndReplicate(Vector<half> mask, half fallback, Vector<half> data); // svclastb[_n_f16]
  ///   public static unsafe Vector<half> Divide(Vector<half> left, half right); // svdiv[_n_f16]_m or svdiv[_n_f16]_x or svdiv[_n_f16]_z
  ///   public static unsafe Vector<half> Max(Vector<half> left, half right); // svmax[_n_f16]_m or svmax[_n_f16]_x or svmax[_n_f16]_z
  ///   public static unsafe Vector<half> MaxNumber(Vector<half> left, half right); // svmaxnm[_n_f16]_m or svmaxnm[_n_f16]_x or svmaxnm[_n_f16]_z
  ///   public static unsafe Vector<half> Min(Vector<half> left, half right); // svmin[_n_f16]_m or svmin[_n_f16]_x or svmin[_n_f16]_z
  ///   public static unsafe Vector<half> MinNumber(Vector<half> left, half right); // svminnm[_n_f16]_m or svminnm[_n_f16]_x or svminnm[_n_f16]_z
  ///   public static unsafe Vector<half> Multiply(Vector<half> left, half right); // svmul[_n_f16]_m or svmul[_n_f16]_x or svmul[_n_f16]_z
  ///   public static unsafe Vector<half> MultiplyAdd(Vector<half> addend, Vector<half> left, half right); // svmla[_n_f16]_m or svmla[_n_f16]_x or svmla[_n_f16]_z
  ///   public static unsafe Vector<half> MultiplyAddNegated(Vector<half> addend, Vector<half> left, half right); // svnmla[_n_f16]_m or svnmla[_n_f16]_x or svnmla[_n_f16]_z
  ///   public static unsafe Vector<float> MultiplyAddWideningLower(Vector<float> op1, Vector<half> op2, half op3); // svmlalb[_n_f32]
  ///   public static unsafe Vector<float> MultiplyAddWideningUpper(Vector<float> op1, Vector<half> op2, half op3); // svmlalt[_n_f32]
  ///   public static unsafe Vector<half> MultiplyExtended(Vector<half> left, half right); // svmulx[_n_f16]_m or svmulx[_n_f16]_x or svmulx[_n_f16]_z
  ///   public static unsafe Vector<half> MultiplySubtract(Vector<half> minuend, Vector<half> left, half right); // svmls[_n_f16]_m or svmls[_n_f16]_x or svmls[_n_f16]_z
  ///   public static unsafe Vector<half> MultiplySubtractNegated(Vector<half> minuend, Vector<half> left, half right); // svnmls[_n_f16]_m or svnmls[_n_f16]_x or svnmls[_n_f16]_z
  ///   public static unsafe Vector<float> MultiplySubtractWideningLower(Vector<float> op1, Vector<half> op2, half op3); // svmlslb[_n_f32]
  ///   public static unsafe Vector<float> MultiplySubtractWideningUpper(Vector<float> op1, Vector<half> op2, half op3); // svmlslt[_n_f32]
  ///   public static unsafe Vector<half> Subtract(Vector<half> left, half right); // svsub[_n_f16]_m or svsub[_n_f16]_x or svsub[_n_f16]_z
  ///   Total Maybe: 33

  /// Rejected:
  ///   public static unsafe ulong CountElementsInAFullVector(Vector<half> value); // svlen[_f16]
  ///   public static unsafe Vector<half> CreateTrueMaskHalf(); // svptrue_b8
  ///   public static unsafe Vector<half> DivideReversed(Vector<half> left, Vector<half> right); // svdivr[_f16]_m or svdivr[_f16]_x or svdivr[_f16]_z
  ///   public static unsafe Vector<half> DivideReversed(Vector<half> left, half right); // svdivr[_n_f16]_m or svdivr[_n_f16]_x or svdivr[_n_f16]_z
  ///   public static unsafe Vector<half> DuplicateSelectedScalarToVector(half value); // svdup[_n]_f16 or svdup[_n]_f16_m or svdup[_n]_f16_x or svdup[_n]_f16_z
  ///   public static unsafe Vector<half> LoadVector(Vector<half> mask, half* address, long vnum); // svld1_vnum[_f16]
  ///   public static unsafe Vector<half> LoadVectorFirstFaulting(Vector<half> mask, half* address, long vnum); // svldff1_vnum[_f16]
  ///   public static unsafe Vector<half> LoadVectorNonFaulting(half* address, long vnum); // svldnf1_vnum[_f16]
  ///   public static unsafe Vector<half> LoadVectorNonTemporal(Vector<half> mask, half* address, long vnum); // svldnt1_vnum[_f16]
  ///   public static unsafe (Vector<half>, Vector<half>) LoadVectorx2(Vector<half> mask, half* address, long vnum); // svld2_vnum[_f16]
  ///   public static unsafe (Vector<half>, Vector<half>, Vector<half>) LoadVectorx3(Vector<half> mask, half* address, long vnum); // svld3_vnum[_f16]
  ///   public static unsafe (Vector<half>, Vector<half>, Vector<half>, Vector<half>) LoadVectorx4(Vector<half> mask, half* address, long vnum); // svld4_vnum[_f16]
  ///   public static unsafe Vector<half> MultiplyAddMultiplicandFirst(Vector<half> op1, Vector<half> op2, Vector<half> op3); // svmad[_f16]_m or svmad[_f16]_x or svmad[_f16]_z
  ///   public static unsafe Vector<half> MultiplyAddMultiplicandFirst(Vector<half> op1, Vector<half> op2, half op3); // svmad[_n_f16]_m or svmad[_n_f16]_x or svmad[_n_f16]_z
  ///   public static unsafe Vector<half> MultiplySubtractMultiplicandFirst(Vector<half> op1, Vector<half> op2, Vector<half> op3); // svmsb[_f16]_m or svmsb[_f16]_x or svmsb[_f16]_z
  ///   public static unsafe Vector<half> MultiplySubtractMultiplicandFirst(Vector<half> op1, Vector<half> op2, half op3); // svmsb[_n_f16]_m or svmsb[_n_f16]_x or svmsb[_n_f16]_z
  ///   public static unsafe Vector<half> NegateMultiplyAddMultiplicandFirst(Vector<half> op1, Vector<half> op2, Vector<half> op3); // svnmad[_f16]_m or svnmad[_f16]_x or svnmad[_f16]_z
  ///   public static unsafe Vector<half> NegateMultiplyAddMultiplicandFirst(Vector<half> op1, Vector<half> op2, half op3); // svnmad[_n_f16]_m or svnmad[_n_f16]_x or svnmad[_n_f16]_z
  ///   public static unsafe Vector<half> NegateMultiplySubtractMultiplicandFirst(Vector<half> op1, Vector<half> op2, Vector<half> op3); // svnmsb[_f16]_m or svnmsb[_f16]_x or svnmsb[_f16]_z
  ///   public static unsafe Vector<half> NegateMultiplySubtractMultiplicandFirst(Vector<half> op1, Vector<half> op2, half op3); // svnmsb[_n_f16]_m or svnmsb[_n_f16]_x or svnmsb[_n_f16]_z
  ///   public static unsafe Vector<half> RoundUsingCurrentRoundingModeExact(Vector<half> value); // svrintx[_f16]_m or svrintx[_f16]_x or svrintx[_f16]_z
  ///   public static unsafe Vector<half> RoundUsingCurrentRoundingModeInexact(Vector<half> value); // svrinti[_f16]_m or svrinti[_f16]_x or svrinti[_f16]_z
  ///   public static unsafe Vector<half> Scale(Vector<half> left, short right); // svscale[_n_f16]_m or svscale[_n_f16]_x or svscale[_n_f16]_z
  ///   public static unsafe void Store(Vector<half> mask, half* base, long vnum, Vector<half> data); // svst1_vnum[_f16]
  ///   public static unsafe void Store(Vector<half> mask, half* base, long vnum, (Vector<half> data1, Vector<half> data2)); // svst2_vnum[_f16]
  ///   public static unsafe void Store(Vector<half> mask, half* base, long vnum, (Vector<half> data1, Vector<half> data2, Vector<half> data3)); // svst3_vnum[_f16]
  ///   public static unsafe void Store(Vector<half> mask, half* base, long vnum, (Vector<half> data1, Vector<half> data2, Vector<half> data3, Vector<half> data4)); // svst4_vnum[_f16]
  ///   public static unsafe void StoreNonTemporal(Vector<half> mask, half* base, long vnum, Vector<half> data); // svstnt1_vnum[_f16]
  ///   public static unsafe Vector<half> SubtractReversed(Vector<half> left, Vector<half> right); // svsubr[_f16]_m or svsubr[_f16]_x or svsubr[_f16]_z
  ///   public static unsafe Vector<half> SubtractReversed(Vector<half> left, half right); // svsubr[_n_f16]_m or svsubr[_n_f16]_x or svsubr[_n_f16]_z
  ///   Total Rejected: 30

  /// Total ACLE covered across API:      360

