namespace System.Runtime.Intrinsics.Arm;

/// VectorT Summary
public abstract partial class SveBf16 : AdvSimd /// Feature: FEAT_BF16
{

  public static unsafe Vector<float> Bfloat16DotProduct(Vector<float> addend, Vector<bfloat16> left, Vector<bfloat16> right); // BFDOT // MOVPRFX

  public static unsafe Vector<float> Bfloat16MatrixMultiplyAccumulate(Vector<float> op1, Vector<bfloat16> op2, Vector<bfloat16> op3); // BFMMLA // MOVPRFX

  public static unsafe Vector<float> Bfloat16MultiplyAddWideningToSinglePrecisionLower(Vector<float> op1, Vector<bfloat16> op2, Vector<bfloat16> op3); // BFMLALB // MOVPRFX

  public static unsafe Vector<float> Bfloat16MultiplyAddWideningToSinglePrecisionLower(Vector<float> op1, Vector<bfloat16> op2, Vector<bfloat16> op3, ulong imm_index); // BFMLALB // MOVPRFX

  public static unsafe Vector<float> Bfloat16MultiplyAddWideningToSinglePrecisionUpper(Vector<float> op1, Vector<bfloat16> op2, Vector<bfloat16> op3); // BFMLALT // MOVPRFX

  public static unsafe Vector<float> Bfloat16MultiplyAddWideningToSinglePrecisionUpper(Vector<float> op1, Vector<bfloat16> op2, Vector<bfloat16> op3, ulong imm_index); // BFMLALT // MOVPRFX

  public static unsafe Vector<bfloat16> ConcatenateEvenInt128FromTwoInputs(Vector<bfloat16> left, Vector<bfloat16> right); // UZP1

  public static unsafe Vector<bfloat16> ConcatenateOddInt128FromTwoInputs(Vector<bfloat16> left, Vector<bfloat16> right); // UZP2

  public static unsafe Vector<bfloat16> ConditionalExtractAfterLastActiveElement(Vector<bfloat16> mask, Vector<bfloat16> defaultValue, Vector<bfloat16> data); // CLASTA // MOVPRFX

  public static unsafe bfloat16 ConditionalExtractAfterLastActiveElement(Vector<bfloat16> mask, bfloat16 defaultValues, Vector<bfloat16> data); // CLASTA // MOVPRFX

  public static unsafe Vector<bfloat16> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<bfloat16> mask, Vector<bfloat16> defaultScalar, Vector<bfloat16> data); // CLASTA // MOVPRFX

  public static unsafe Vector<bfloat16> ConditionalExtractLastActiveElement(Vector<bfloat16> mask, Vector<bfloat16> defaultValue, Vector<bfloat16> data); // CLASTB // MOVPRFX

  public static unsafe bfloat16 ConditionalExtractLastActiveElement(Vector<bfloat16> mask, bfloat16 defaultValues, Vector<bfloat16> data); // CLASTB // MOVPRFX

  public static unsafe Vector<bfloat16> ConditionalExtractLastActiveElementAndReplicate(Vector<bfloat16> mask, Vector<bfloat16> fallback, Vector<bfloat16> data); // CLASTB // MOVPRFX

  public static unsafe Vector<bfloat16> ConditionalSelect(Vector<bfloat16> mask, Vector<bfloat16> left, Vector<bfloat16> right); // SEL

  public static unsafe Vector<bfloat16> ConvertToBFloat16(Vector<float> value); // BFCVT // predicated, MOVPRFX

  public static unsafe Vector<bfloat16> CreateFalseMaskBFloat16(); // PFALSE

  public static unsafe Vector<bfloat16> CreateTrueMaskBFloat16([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // PTRUE

  public static unsafe Vector<bfloat16> CreateWhileReadAfterWriteMask(bfloat16* left, bfloat16* right); // WHILERW

  public static unsafe Vector<bfloat16> CreateWhileWriteAfterReadMask(bfloat16* left, bfloat16* right); // WHILEWR

  public static unsafe Vector<float> DotProductBySelectedScalar(Vector<float> addend, Vector<bfloat16> left, Vector<bfloat16> right, [ConstantExpected] byte rightIndex); // BFDOT // MOVPRFX

  public static unsafe Vector<bfloat16> DownConvertNarrowingUpper(Vector<float> value); // BFCVTNT // predicated

  public static unsafe Vector<bfloat16> DuplicateSelectedScalarToVector(Vector<bfloat16> data, [ConstantExpected] byte index); // DUP or TBL

  public static unsafe bfloat16 ExtractAfterLastScalar(Vector<bfloat16> value); // LASTA // predicated

  public static unsafe Vector<bfloat16> ExtractAfterLastVector(Vector<bfloat16> value); // LASTA // predicated

  public static unsafe bfloat16 ExtractLastScalar(Vector<bfloat16> value); // LASTB // predicated

  public static unsafe Vector<bfloat16> ExtractLastVector(Vector<bfloat16> value); // LASTB // predicated

  public static unsafe Vector<bfloat16> ExtractVector(Vector<bfloat16> upper, Vector<bfloat16> lower, [ConstantExpected] byte index); // EXT // MOVPRFX

  public static unsafe ulong GetActiveElementCount(Vector<bfloat16> mask, Vector<bfloat16> from); // CNTP

  public static unsafe Vector<bfloat16> InsertIntoShiftedVector(Vector<bfloat16> left, bfloat16 right); // INSR

  public static unsafe Vector<bfloat16> InterleaveEvenInt128FromTwoInputs(Vector<bfloat16> left, Vector<bfloat16> right); // TRN1

  public static unsafe Vector<bfloat16> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<bfloat16> left, Vector<bfloat16> right); // ZIP2

  public static unsafe Vector<bfloat16> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<bfloat16> left, Vector<bfloat16> right); // ZIP1

  public static unsafe Vector<bfloat16> InterleaveOddInt128FromTwoInputs(Vector<bfloat16> left, Vector<bfloat16> right); // TRN2

  public static unsafe Vector<bfloat16> LoadVector(Vector<bfloat16> mask, bfloat16* address); // LD1H

  public static unsafe Vector<bfloat16> LoadVector128AndReplicateToVector(Vector<bfloat16> mask, bfloat16* address); // LD1RQH

  public static unsafe Vector<bfloat16> LoadVector256AndReplicateToVector(Vector<bfloat16> mask, bfloat16* address); // LD1ROH

  public static unsafe Vector<bfloat16> LoadVectorFirstFaulting(Vector<bfloat16> mask, bfloat16* address); // LDFF1H

  public static unsafe Vector<bfloat16> LoadVectorNonFaulting(bfloat16* address); // LDNF1H // predicated

  public static unsafe Vector<bfloat16> LoadVectorNonTemporal(Vector<bfloat16> mask, bfloat16* address); // LDNT1H

  public static unsafe (Vector<bfloat16>, Vector<bfloat16>) LoadVectorx2(Vector<bfloat16> mask, bfloat16* address); // LD2H

  public static unsafe (Vector<bfloat16>, Vector<bfloat16>, Vector<bfloat16>) LoadVectorx3(Vector<bfloat16> mask, bfloat16* address); // LD3H

  public static unsafe (Vector<bfloat16>, Vector<bfloat16>, Vector<bfloat16>, Vector<bfloat16>) LoadVectorx4(Vector<bfloat16> mask, bfloat16* address); // LD4H

  public static unsafe Vector<ushort> PopCount(Vector<bfloat16> value); // CNT // predicated, MOVPRFX

  public static unsafe Vector<bfloat16> ReverseElement(Vector<bfloat16> value); // REV

  public static unsafe Vector<bfloat16> Splice(Vector<bfloat16> mask, Vector<bfloat16> left, Vector<bfloat16> right); // SPLICE // MOVPRFX

  public static unsafe void Store(Vector<bfloat16> mask, bfloat16* address, Vector<bfloat16> data); // ST1H

  public static unsafe void Store(Vector<bfloat16> mask, bfloat16* address, (Vector<bfloat16> Value1, Vector<bfloat16> Value2) data); // ST2H

  public static unsafe void Store(Vector<bfloat16> mask, bfloat16* address, (Vector<bfloat16> Value1, Vector<bfloat16> Value2, Vector<bfloat16> Value3) data); // ST3H

  public static unsafe void Store(Vector<bfloat16> mask, bfloat16* address, (Vector<bfloat16> Value1, Vector<bfloat16> Value2, Vector<bfloat16> Value3, Vector<bfloat16> Value4) data); // ST4H

  public static unsafe void StoreNonTemporal(Vector<bfloat16> mask, bfloat16* address, Vector<bfloat16> data); // STNT1H

  public static unsafe Vector<bfloat16> TransposeEven(Vector<bfloat16> left, Vector<bfloat16> right); // TRN1

  public static unsafe Vector<bfloat16> TransposeOdd(Vector<bfloat16> left, Vector<bfloat16> right); // TRN2

  public static unsafe Vector<bfloat16> UnzipEven(Vector<bfloat16> left, Vector<bfloat16> right); // UZP1

  public static unsafe Vector<bfloat16> UnzipOdd(Vector<bfloat16> left, Vector<bfloat16> right); // UZP2

  public static unsafe Vector<bfloat16> VectorTableLookup(Vector<bfloat16> data, Vector<ushort> indices); // TBL

  public static unsafe Vector<bfloat16> VectorTableLookup((Vector<bfloat16> data1, Vector<bfloat16> data2), Vector<ushort> indices); // TBL

  public static unsafe Vector<bfloat16> VectorTableLookupExtension(Vector<bfloat16> fallback, Vector<bfloat16> data, Vector<ushort> indices); // TBX

  public static unsafe Vector<bfloat16> ZipHigh(Vector<bfloat16> left, Vector<bfloat16> right); // ZIP2

  public static unsafe Vector<bfloat16> ZipLow(Vector<bfloat16> left, Vector<bfloat16> right); // ZIP1


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

  /// total method signatures: 60


  /// Optional Entries:

  public static unsafe Vector<float> Bfloat16DotProduct(Vector<float> addend, Vector<bfloat16> left, bfloat16 right); // BFDOT // MOVPRFX

  public static unsafe Vector<float> Bfloat16MultiplyAddWideningToSinglePrecisionLower(Vector<float> op1, Vector<bfloat16> op2, bfloat16 op3); // BFMLALB // MOVPRFX

  public static unsafe Vector<float> Bfloat16MultiplyAddWideningToSinglePrecisionUpper(Vector<float> op1, Vector<bfloat16> op2, bfloat16 op3); // BFMLALT // MOVPRFX

  public static unsafe bfloat16 ConditionalExtractAfterLastActiveElement(Vector<bfloat16> mask, bfloat16 defaultValue, Vector<bfloat16> data); // CLASTA

  public static unsafe bfloat16 ConditionalExtractAfterLastActiveElementAndReplicate(Vector<bfloat16> mask, bfloat16 defaultScalar, Vector<bfloat16> data); // CLASTA

  public static unsafe bfloat16 ConditionalExtractLastActiveElement(Vector<bfloat16> mask, bfloat16 defaultValue, Vector<bfloat16> data); // CLASTB

  public static unsafe bfloat16 ConditionalExtractLastActiveElementAndReplicate(Vector<bfloat16> mask, bfloat16 fallback, Vector<bfloat16> data); // CLASTB

  /// total optional method signatures: 7

}


/// Full API
public abstract partial class SveBf16 : AdvSimd /// Feature: FEAT_BF16
{
    /// Bfloat16DotProduct : BFloat16 dot product

    /// svfloat32_t svbfdot[_f32](svfloat32_t op1, svbfloat16_t op2, svbfloat16_t op3) : "BFDOT Ztied1.S, Zop2.H, Zop3.H" or "MOVPRFX Zresult, Zop1; BFDOT Zresult.S, Zop2.H, Zop3.H"
  public static unsafe Vector<float> Bfloat16DotProduct(Vector<float> addend, Vector<bfloat16> left, Vector<bfloat16> right);


    /// Bfloat16MatrixMultiplyAccumulate : BFloat16 matrix multiply-accumulate

    /// svfloat32_t svbfmmla[_f32](svfloat32_t op1, svbfloat16_t op2, svbfloat16_t op3) : "BFMMLA Ztied1.S, Zop2.H, Zop3.H" or "MOVPRFX Zresult, Zop1; BFMMLA Zresult.S, Zop2.H, Zop3.H"
  public static unsafe Vector<float> Bfloat16MatrixMultiplyAccumulate(Vector<float> op1, Vector<bfloat16> op2, Vector<bfloat16> op3);


    /// Bfloat16MultiplyAddWideningToSinglePrecisionLower : BFloat16 multiply-add long to single-precision (bottom)

    /// svfloat32_t svbfmlalb[_f32](svfloat32_t op1, svbfloat16_t op2, svbfloat16_t op3) : "BFMLALB Ztied1.S, Zop2.H, Zop3.H" or "MOVPRFX Zresult, Zop1; BFMLALB Zresult.S, Zop2.H, Zop3.H"
  public static unsafe Vector<float> Bfloat16MultiplyAddWideningToSinglePrecisionLower(Vector<float> op1, Vector<bfloat16> op2, Vector<bfloat16> op3);

    /// svfloat32_t svbfmlalb_lane[_f32](svfloat32_t op1, svbfloat16_t op2, svbfloat16_t op3, uint64_t imm_index) : "BFMLALB Ztied1.S, Zop2.H, Zop3.H[imm_index]" or "MOVPRFX Zresult, Zop1; BFMLALB Zresult.S, Zop2.H, Zop3.H[imm_index]"
  public static unsafe Vector<float> Bfloat16MultiplyAddWideningToSinglePrecisionLower(Vector<float> op1, Vector<bfloat16> op2, Vector<bfloat16> op3, ulong imm_index);


    /// Bfloat16MultiplyAddWideningToSinglePrecisionUpper : BFloat16 multiply-add long to single-precision (top)

    /// svfloat32_t svbfmlalt[_f32](svfloat32_t op1, svbfloat16_t op2, svbfloat16_t op3) : "BFMLALT Ztied1.S, Zop2.H, Zop3.H" or "MOVPRFX Zresult, Zop1; BFMLALT Zresult.S, Zop2.H, Zop3.H"
  public static unsafe Vector<float> Bfloat16MultiplyAddWideningToSinglePrecisionUpper(Vector<float> op1, Vector<bfloat16> op2, Vector<bfloat16> op3);

    /// svfloat32_t svbfmlalt_lane[_f32](svfloat32_t op1, svbfloat16_t op2, svbfloat16_t op3, uint64_t imm_index) : "BFMLALT Ztied1.S, Zop2.H, Zop3.H[imm_index]" or "MOVPRFX Zresult, Zop1; BFMLALT Zresult.S, Zop2.H, Zop3.H[imm_index]"
  public static unsafe Vector<float> Bfloat16MultiplyAddWideningToSinglePrecisionUpper(Vector<float> op1, Vector<bfloat16> op2, Vector<bfloat16> op3, ulong imm_index);


    /// ConcatenateEvenInt128FromTwoInputs : Concatenate even quadwords from two inputs

    /// svbfloat16_t svuzp1q[_bf16](svbfloat16_t op1, svbfloat16_t op2) : "UZP1 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<bfloat16> ConcatenateEvenInt128FromTwoInputs(Vector<bfloat16> left, Vector<bfloat16> right);


    /// ConcatenateOddInt128FromTwoInputs : Concatenate odd quadwords from two inputs

    /// svbfloat16_t svuzp2q[_bf16](svbfloat16_t op1, svbfloat16_t op2) : "UZP2 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<bfloat16> ConcatenateOddInt128FromTwoInputs(Vector<bfloat16> left, Vector<bfloat16> right);


    /// ConditionalExtractAfterLastActiveElement : Conditionally extract element after last

    /// svbfloat16_t svclasta[_bf16](svbool_t pg, svbfloat16_t fallback, svbfloat16_t data) : "CLASTA Ztied.H, Pg, Ztied.H, Zdata.H" or "MOVPRFX Zresult, Zfallback; CLASTA Zresult.H, Pg, Zresult.H, Zdata.H"
  public static unsafe Vector<bfloat16> ConditionalExtractAfterLastActiveElement(Vector<bfloat16> mask, Vector<bfloat16> defaultValue, Vector<bfloat16> data);

    /// svbfloat16_t svclasta[_bf16](svbool_t pg, svbfloat16_t fallback, svbfloat16_t data) : "CLASTA Ztied.H, Pg, Ztied.H, Zdata.H" or "MOVPRFX Zresult, Zfallback; CLASTA Zresult.H, Pg, Zresult.H, Zdata.H"
    /// bfloat16_t svclasta[_n_bf16](svbool_t pg, bfloat16_t fallback, svbfloat16_t data) : "CLASTA Wtied, Pg, Wtied, Zdata.H" or "CLASTA Htied, Pg, Htied, Zdata.H"
  public static unsafe bfloat16 ConditionalExtractAfterLastActiveElement(Vector<bfloat16> mask, bfloat16 defaultValues, Vector<bfloat16> data);


    /// ConditionalExtractAfterLastActiveElementAndReplicate : Conditionally extract element after last

    /// svbfloat16_t svclasta[_bf16](svbool_t pg, svbfloat16_t fallback, svbfloat16_t data) : "CLASTA Ztied.H, Pg, Ztied.H, Zdata.H" or "MOVPRFX Zresult, Zfallback; CLASTA Zresult.H, Pg, Zresult.H, Zdata.H"
  public static unsafe Vector<bfloat16> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<bfloat16> mask, Vector<bfloat16> defaultScalar, Vector<bfloat16> data);


    /// ConditionalExtractLastActiveElement : Conditionally extract last element

    /// svbfloat16_t svclastb[_bf16](svbool_t pg, svbfloat16_t fallback, svbfloat16_t data) : "CLASTB Ztied.H, Pg, Ztied.H, Zdata.H" or "MOVPRFX Zresult, Zfallback; CLASTB Zresult.H, Pg, Zresult.H, Zdata.H"
  public static unsafe Vector<bfloat16> ConditionalExtractLastActiveElement(Vector<bfloat16> mask, Vector<bfloat16> defaultValue, Vector<bfloat16> data);

    /// svbfloat16_t svclastb[_bf16](svbool_t pg, svbfloat16_t fallback, svbfloat16_t data) : "CLASTB Ztied.H, Pg, Ztied.H, Zdata.H" or "MOVPRFX Zresult, Zfallback; CLASTB Zresult.H, Pg, Zresult.H, Zdata.H"
    /// bfloat16_t svclastb[_n_bf16](svbool_t pg, bfloat16_t fallback, svbfloat16_t data) : "CLASTB Wtied, Pg, Wtied, Zdata.H" or "CLASTB Htied, Pg, Htied, Zdata.H"
  public static unsafe bfloat16 ConditionalExtractLastActiveElement(Vector<bfloat16> mask, bfloat16 defaultValues, Vector<bfloat16> data);


    /// ConditionalExtractLastActiveElementAndReplicate : Conditionally extract last element

    /// svbfloat16_t svclastb[_bf16](svbool_t pg, svbfloat16_t fallback, svbfloat16_t data) : "CLASTB Ztied.H, Pg, Ztied.H, Zdata.H" or "MOVPRFX Zresult, Zfallback; CLASTB Zresult.H, Pg, Zresult.H, Zdata.H"
  public static unsafe Vector<bfloat16> ConditionalExtractLastActiveElementAndReplicate(Vector<bfloat16> mask, Vector<bfloat16> fallback, Vector<bfloat16> data);


    /// ConditionalSelect : Conditionally select elements

    /// svbfloat16_t svsel[_bf16](svbool_t pg, svbfloat16_t op1, svbfloat16_t op2) : "SEL Zresult.H, Pg, Zop1.H, Zop2.H"
  public static unsafe Vector<bfloat16> ConditionalSelect(Vector<bfloat16> mask, Vector<bfloat16> left, Vector<bfloat16> right);


    /// ConvertToBFloat16 : Floating-point convert

    /// svbfloat16_t svcvt_bf16[_f32]_m(svbfloat16_t inactive, svbool_t pg, svfloat32_t op) : "BFCVT Ztied.H, Pg/M, Zop.S" or "MOVPRFX Zresult, Zinactive; BFCVT Zresult.H, Pg/M, Zop.S"
    /// svbfloat16_t svcvt_bf16[_f32]_x(svbool_t pg, svfloat32_t op) : "BFCVT Ztied.H, Pg/M, Ztied.S" or "MOVPRFX Zresult, Zop; BFCVT Zresult.H, Pg/M, Zop.S"
    /// svbfloat16_t svcvt_bf16[_f32]_z(svbool_t pg, svfloat32_t op) : "MOVPRFX Zresult.S, Pg/Z, Zop.S; BFCVT Zresult.H, Pg/M, Zop.S"
  public static unsafe Vector<bfloat16> ConvertToBFloat16(Vector<float> value);


    /// CreateFalseMaskBFloat16 : Set all predicate elements to false

    /// svbool_t svpfalse[_b]() : "PFALSE Presult.B"
  public static unsafe Vector<bfloat16> CreateFalseMaskBFloat16();


    /// CreateTrueMaskBFloat16 : Set predicate elements to true

    /// svbool_t svptrue_pat_b8(enum svpattern pattern) : "PTRUE Presult.B, pattern"
  public static unsafe Vector<bfloat16> CreateTrueMaskBFloat16([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All);


    /// CreateWhileReadAfterWriteMask : While free of read-after-write conflicts

    /// svbool_t svwhilerw[_bf16](const bfloat16_t *op1, const bfloat16_t *op2) : "WHILERW Presult.H, Xop1, Xop2"
  public static unsafe Vector<bfloat16> CreateWhileReadAfterWriteMask(bfloat16* left, bfloat16* right);


    /// CreateWhileWriteAfterReadMask : While free of write-after-read conflicts

    /// svbool_t svwhilewr[_bf16](const bfloat16_t *op1, const bfloat16_t *op2) : "WHILEWR Presult.H, Xop1, Xop2"
  public static unsafe Vector<bfloat16> CreateWhileWriteAfterReadMask(bfloat16* left, bfloat16* right);


    /// DotProductBySelectedScalar : BFloat16 dot product

    /// svfloat32_t svbfdot_lane[_f32](svfloat32_t op1, svbfloat16_t op2, svbfloat16_t op3, uint64_t imm_index) : "BFDOT Ztied1.S, Zop2.H, Zop3.H[imm_index]" or "MOVPRFX Zresult, Zop1; BFDOT Zresult.S, Zop2.H, Zop3.H[imm_index]"
  public static unsafe Vector<float> DotProductBySelectedScalar(Vector<float> addend, Vector<bfloat16> left, Vector<bfloat16> right, [ConstantExpected] byte rightIndex);


    /// DownConvertNarrowingUpper : Down convert and narrow (top)

    /// svbfloat16_t svcvtnt_bf16[_f32]_m(svbfloat16_t even, svbool_t pg, svfloat32_t op) : "BFCVTNT Ztied.H, Pg/M, Zop.S"
    /// svbfloat16_t svcvtnt_bf16[_f32]_x(svbfloat16_t even, svbool_t pg, svfloat32_t op) : "BFCVTNT Ztied.H, Pg/M, Zop.S"
  public static unsafe Vector<bfloat16> DownConvertNarrowingUpper(Vector<float> value);


    /// DuplicateSelectedScalarToVector : Broadcast a scalar value

    /// svbfloat16_t svdup_lane[_bf16](svbfloat16_t data, uint16_t index) : "DUP Zresult.H, Zdata.H[index]" or "TBL Zresult.H, Zdata.H, Zindex.H"
    /// svbfloat16_t svdupq_lane[_bf16](svbfloat16_t data, uint64_t index) : "DUP Zresult.Q, Zdata.Q[index]" or "TBL Zresult.D, Zdata.D, Zindices_d.D"
  public static unsafe Vector<bfloat16> DuplicateSelectedScalarToVector(Vector<bfloat16> data, [ConstantExpected] byte index);


    /// ExtractAfterLastScalar : Extract element after last

    /// bfloat16_t svlasta[_bf16](svbool_t pg, svbfloat16_t op) : "LASTA Wresult, Pg, Zop.H" or "LASTA Hresult, Pg, Zop.H"
  public static unsafe bfloat16 ExtractAfterLastScalar(Vector<bfloat16> value);


    /// ExtractAfterLastVector : Extract element after last

    /// bfloat16_t svlasta[_bf16](svbool_t pg, svbfloat16_t op) : "LASTA Wresult, Pg, Zop.H" or "LASTA Hresult, Pg, Zop.H"
  public static unsafe Vector<bfloat16> ExtractAfterLastVector(Vector<bfloat16> value);


    /// ExtractLastScalar : Extract last element

    /// bfloat16_t svlastb[_bf16](svbool_t pg, svbfloat16_t op) : "LASTB Wresult, Pg, Zop.H" or "LASTB Hresult, Pg, Zop.H"
  public static unsafe bfloat16 ExtractLastScalar(Vector<bfloat16> value);


    /// ExtractLastVector : Extract last element

    /// bfloat16_t svlastb[_bf16](svbool_t pg, svbfloat16_t op) : "LASTB Wresult, Pg, Zop.H" or "LASTB Hresult, Pg, Zop.H"
  public static unsafe Vector<bfloat16> ExtractLastVector(Vector<bfloat16> value);


    /// ExtractVector : Extract vector from pair of vectors

    /// svbfloat16_t svext[_bf16](svbfloat16_t op1, svbfloat16_t op2, uint64_t imm3) : "EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3 * 2" or "MOVPRFX Zresult, Zop1; EXT Zresult.B, Zresult.B, Zop2.B, #imm3 * 2"
  public static unsafe Vector<bfloat16> ExtractVector(Vector<bfloat16> upper, Vector<bfloat16> lower, [ConstantExpected] byte index);


    /// GetActiveElementCount : Count set predicate bits

    /// uint64_t svcntp_b8(svbool_t pg, svbool_t op) : "CNTP Xresult, Pg, Pop.B"
  public static unsafe ulong GetActiveElementCount(Vector<bfloat16> mask, Vector<bfloat16> from);


    /// InsertIntoShiftedVector : Insert scalar into shifted vector

    /// svbfloat16_t svinsr[_n_bf16](svbfloat16_t op1, bfloat16_t op2) : "INSR Ztied1.H, Wop2" or "INSR Ztied1.H, Hop2"
  public static unsafe Vector<bfloat16> InsertIntoShiftedVector(Vector<bfloat16> left, bfloat16 right);


    /// InterleaveEvenInt128FromTwoInputs : Interleave even quadwords from two inputs

    /// svbfloat16_t svtrn1q[_bf16](svbfloat16_t op1, svbfloat16_t op2) : "TRN1 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<bfloat16> InterleaveEvenInt128FromTwoInputs(Vector<bfloat16> left, Vector<bfloat16> right);


    /// InterleaveInt128FromHighHalvesOfTwoInputs : Interleave quadwords from high halves of two inputs

    /// svbfloat16_t svzip2q[_bf16](svbfloat16_t op1, svbfloat16_t op2) : "ZIP2 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<bfloat16> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<bfloat16> left, Vector<bfloat16> right);


    /// InterleaveInt128FromLowHalvesOfTwoInputs : Interleave quadwords from low halves of two inputs

    /// svbfloat16_t svzip1q[_bf16](svbfloat16_t op1, svbfloat16_t op2) : "ZIP1 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<bfloat16> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<bfloat16> left, Vector<bfloat16> right);


    /// InterleaveOddInt128FromTwoInputs : Interleave odd quadwords from two inputs

    /// svbfloat16_t svtrn2q[_bf16](svbfloat16_t op1, svbfloat16_t op2) : "TRN2 Zresult.Q, Zop1.Q, Zop2.Q"
  public static unsafe Vector<bfloat16> InterleaveOddInt128FromTwoInputs(Vector<bfloat16> left, Vector<bfloat16> right);


    /// LoadVector : Unextended load

    /// svbfloat16_t svld1[_bf16](svbool_t pg, const bfloat16_t *base) : "LD1H Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]" or "LD1H Zresult.H, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<bfloat16> LoadVector(Vector<bfloat16> mask, bfloat16* address);


    /// LoadVector128AndReplicateToVector : Load and replicate 128 bits of data

    /// svbfloat16_t svld1rq[_bf16](svbool_t pg, const bfloat16_t *base) : "LD1RQH Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]" or "LD1RQH Zresult.H, Pg/Z, [Xarray, #index * 2]" or "LD1RQH Zresult.H, Pg/Z, [Xbase, #0]"
  public static unsafe Vector<bfloat16> LoadVector128AndReplicateToVector(Vector<bfloat16> mask, bfloat16* address);


    /// LoadVector256AndReplicateToVector : Load and replicate 256 bits of data

    /// svbfloat16_t svld1ro[_bf16](svbool_t pg, const bfloat16_t *base) : "LD1ROH Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]" or "LD1ROH Zresult.H, Pg/Z, [Xarray, #index * 2]" or "LD1ROH Zresult.H, Pg/Z, [Xbase, #0]"
  public static unsafe Vector<bfloat16> LoadVector256AndReplicateToVector(Vector<bfloat16> mask, bfloat16* address);


    /// LoadVectorFirstFaulting : Unextended load, first-faulting

    /// svbfloat16_t svldff1[_bf16](svbool_t pg, const bfloat16_t *base) : "LDFF1H Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]" or "LDFF1H Zresult.H, Pg/Z, [Xbase, XZR, LSL #1]"
  public static unsafe Vector<bfloat16> LoadVectorFirstFaulting(Vector<bfloat16> mask, bfloat16* address);


    /// LoadVectorNonFaulting : Unextended load, non-faulting

    /// svbfloat16_t svldnf1[_bf16](svbool_t pg, const bfloat16_t *base) : "LDNF1H Zresult.H, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<bfloat16> LoadVectorNonFaulting(bfloat16* address);


    /// LoadVectorNonTemporal : Unextended load, non-temporal

    /// svbfloat16_t svldnt1[_bf16](svbool_t pg, const bfloat16_t *base) : "LDNT1H Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]" or "LDNT1H Zresult.H, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe Vector<bfloat16> LoadVectorNonTemporal(Vector<bfloat16> mask, bfloat16* address);


    /// LoadVectorx2 : Load two-element tuples into two vectors

    /// svbfloat16x2_t svld2[_bf16](svbool_t pg, const bfloat16_t *base) : "LD2H {Zresult0.H, Zresult1.H}, Pg/Z, [Xarray, Xindex, LSL #1]" or "LD2H {Zresult0.H, Zresult1.H}, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe (Vector<bfloat16>, Vector<bfloat16>) LoadVectorx2(Vector<bfloat16> mask, bfloat16* address);


    /// LoadVectorx3 : Load three-element tuples into three vectors

    /// svbfloat16x3_t svld3[_bf16](svbool_t pg, const bfloat16_t *base) : "LD3H {Zresult0.H - Zresult2.H}, Pg/Z, [Xarray, Xindex, LSL #1]" or "LD3H {Zresult0.H - Zresult2.H}, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe (Vector<bfloat16>, Vector<bfloat16>, Vector<bfloat16>) LoadVectorx3(Vector<bfloat16> mask, bfloat16* address);


    /// LoadVectorx4 : Load four-element tuples into four vectors

    /// svbfloat16x4_t svld4[_bf16](svbool_t pg, const bfloat16_t *base) : "LD4H {Zresult0.H - Zresult3.H}, Pg/Z, [Xarray, Xindex, LSL #1]" or "LD4H {Zresult0.H - Zresult3.H}, Pg/Z, [Xbase, #0, MUL VL]"
  public static unsafe (Vector<bfloat16>, Vector<bfloat16>, Vector<bfloat16>, Vector<bfloat16>) LoadVectorx4(Vector<bfloat16> mask, bfloat16* address);


    /// PopCount : Count nonzero bits

    /// svuint16_t svcnt[_bf16]_m(svuint16_t inactive, svbool_t pg, svbfloat16_t op) : "CNT Ztied.H, Pg/M, Zop.H" or "MOVPRFX Zresult, Zinactive; CNT Zresult.H, Pg/M, Zop.H"
    /// svuint16_t svcnt[_bf16]_x(svbool_t pg, svbfloat16_t op) : "CNT Ztied.H, Pg/M, Ztied.H" or "MOVPRFX Zresult, Zop; CNT Zresult.H, Pg/M, Zop.H"
    /// svuint16_t svcnt[_bf16]_z(svbool_t pg, svbfloat16_t op) : "MOVPRFX Zresult.H, Pg/Z, Zop.H; CNT Zresult.H, Pg/M, Zop.H"
  public static unsafe Vector<ushort> PopCount(Vector<bfloat16> value);


    /// ReverseElement : Reverse all elements

    /// svbfloat16_t svrev[_bf16](svbfloat16_t op) : "REV Zresult.H, Zop.H"
  public static unsafe Vector<bfloat16> ReverseElement(Vector<bfloat16> value);


    /// Splice : Splice two vectors under predicate control

    /// svbfloat16_t svsplice[_bf16](svbool_t pg, svbfloat16_t op1, svbfloat16_t op2) : "SPLICE Ztied1.H, Pg, Ztied1.H, Zop2.H" or "MOVPRFX Zresult, Zop1; SPLICE Zresult.H, Pg, Zresult.H, Zop2.H"
  public static unsafe Vector<bfloat16> Splice(Vector<bfloat16> mask, Vector<bfloat16> left, Vector<bfloat16> right);


    /// Store : Non-truncating store

    /// void svst1[_bf16](svbool_t pg, bfloat16_t *base, svbfloat16_t data) : "ST1H Zdata.H, Pg, [Xarray, Xindex, LSL #1]" or "ST1H Zdata.H, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<bfloat16> mask, bfloat16* address, Vector<bfloat16> data);

    /// void svst2[_bf16](svbool_t pg, bfloat16_t *base, svbfloat16x2_t data) : "ST2H {Zdata0.H, Zdata1.H}, Pg, [Xarray, Xindex, LSL #1]" or "ST2H {Zdata0.H, Zdata1.H}, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<bfloat16> mask, bfloat16* address, (Vector<bfloat16> Value1, Vector<bfloat16> Value2) data);

    /// void svst3[_bf16](svbool_t pg, bfloat16_t *base, svbfloat16x3_t data) : "ST3H {Zdata0.H - Zdata2.H}, Pg, [Xarray, Xindex, LSL #1]" or "ST3H {Zdata0.H - Zdata2.H}, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<bfloat16> mask, bfloat16* address, (Vector<bfloat16> Value1, Vector<bfloat16> Value2, Vector<bfloat16> Value3) data);

    /// void svst4[_bf16](svbool_t pg, bfloat16_t *base, svbfloat16x4_t data) : "ST4H {Zdata0.H - Zdata3.H}, Pg, [Xarray, Xindex, LSL #1]" or "ST4H {Zdata0.H - Zdata3.H}, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void Store(Vector<bfloat16> mask, bfloat16* address, (Vector<bfloat16> Value1, Vector<bfloat16> Value2, Vector<bfloat16> Value3, Vector<bfloat16> Value4) data);


    /// StoreNonTemporal : Non-truncating store, non-temporal

    /// void svstnt1[_bf16](svbool_t pg, bfloat16_t *base, svbfloat16_t data) : "STNT1H Zdata.H, Pg, [Xarray, Xindex, LSL #1]" or "STNT1H Zdata.H, Pg, [Xbase, #0, MUL VL]"
  public static unsafe void StoreNonTemporal(Vector<bfloat16> mask, bfloat16* address, Vector<bfloat16> data);


    /// TransposeEven : Interleave even elements from two inputs

    /// svbfloat16_t svtrn1[_bf16](svbfloat16_t op1, svbfloat16_t op2) : "TRN1 Zresult.H, Zop1.H, Zop2.H"
  public static unsafe Vector<bfloat16> TransposeEven(Vector<bfloat16> left, Vector<bfloat16> right);


    /// TransposeOdd : Interleave odd elements from two inputs

    /// svbfloat16_t svtrn2[_bf16](svbfloat16_t op1, svbfloat16_t op2) : "TRN2 Zresult.H, Zop1.H, Zop2.H"
  public static unsafe Vector<bfloat16> TransposeOdd(Vector<bfloat16> left, Vector<bfloat16> right);


    /// UnzipEven : Concatenate even elements from two inputs

    /// svbfloat16_t svuzp1[_bf16](svbfloat16_t op1, svbfloat16_t op2) : "UZP1 Zresult.H, Zop1.H, Zop2.H"
  public static unsafe Vector<bfloat16> UnzipEven(Vector<bfloat16> left, Vector<bfloat16> right);


    /// UnzipOdd : Concatenate odd elements from two inputs

    /// svbfloat16_t svuzp2[_bf16](svbfloat16_t op1, svbfloat16_t op2) : "UZP2 Zresult.H, Zop1.H, Zop2.H"
  public static unsafe Vector<bfloat16> UnzipOdd(Vector<bfloat16> left, Vector<bfloat16> right);


    /// VectorTableLookup : Table lookup in single-vector table

    /// svbfloat16_t svtbl[_bf16](svbfloat16_t data, svuint16_t indices) : "TBL Zresult.H, Zdata.H, Zindices.H"
  public static unsafe Vector<bfloat16> VectorTableLookup(Vector<bfloat16> data, Vector<ushort> indices);

    /// svbfloat16_t svtbl2[_bf16](svbfloat16x2_t data, svuint16_t indices) : "TBL Zresult.H, {Zdata0.H, Zdata1.H}, Zindices.H"
  public static unsafe Vector<bfloat16> VectorTableLookup((Vector<bfloat16> data1, Vector<bfloat16> data2), Vector<ushort> indices);


    /// VectorTableLookupExtension : Table lookup in single-vector table (merging)

    /// svbfloat16_t svtbx[_bf16](svbfloat16_t fallback, svbfloat16_t data, svuint16_t indices) : "TBX Ztied.H, Zdata.H, Zindices.H"
  public static unsafe Vector<bfloat16> VectorTableLookupExtension(Vector<bfloat16> fallback, Vector<bfloat16> data, Vector<ushort> indices);


    /// ZipHigh : Interleave elements from high halves of two inputs

    /// svbfloat16_t svzip2[_bf16](svbfloat16_t op1, svbfloat16_t op2) : "ZIP2 Zresult.H, Zop1.H, Zop2.H"
  public static unsafe Vector<bfloat16> ZipHigh(Vector<bfloat16> left, Vector<bfloat16> right);


    /// ZipLow : Interleave elements from low halves of two inputs

    /// svbfloat16_t svzip1[_bf16](svbfloat16_t op1, svbfloat16_t op2) : "ZIP1 Zresult.H, Zop1.H, Zop2.H"
  public static unsafe Vector<bfloat16> ZipLow(Vector<bfloat16> left, Vector<bfloat16> right);


  /// total method signatures: 60
  /// total method names:      53
}

  /// Optional Entries:
  ///   public static unsafe Vector<float> Bfloat16DotProduct(Vector<float> addend, Vector<bfloat16> left, bfloat16 right); // svbfdot[_n_f32]
  ///   public static unsafe Vector<float> Bfloat16MultiplyAddWideningToSinglePrecisionLower(Vector<float> op1, Vector<bfloat16> op2, bfloat16 op3); // svbfmlalb[_n_f32]
  ///   public static unsafe Vector<float> Bfloat16MultiplyAddWideningToSinglePrecisionUpper(Vector<float> op1, Vector<bfloat16> op2, bfloat16 op3); // svbfmlalt[_n_f32]
  ///   public static unsafe bfloat16 ConditionalExtractAfterLastActiveElement(Vector<bfloat16> mask, bfloat16 defaultValue, Vector<bfloat16> data); // svclasta[_n_bf16]
  ///   public static unsafe bfloat16 ConditionalExtractAfterLastActiveElementAndReplicate(Vector<bfloat16> mask, bfloat16 defaultScalar, Vector<bfloat16> data); // svclasta[_n_bf16]
  ///   public static unsafe bfloat16 ConditionalExtractLastActiveElement(Vector<bfloat16> mask, bfloat16 defaultValue, Vector<bfloat16> data); // svclastb[_n_bf16]
  ///   public static unsafe bfloat16 ConditionalExtractLastActiveElementAndReplicate(Vector<bfloat16> mask, bfloat16 fallback, Vector<bfloat16> data); // svclastb[_n_bf16]
  ///   Total Maybe: 7

  /// Rejected:
  ///   public static unsafe ulong CountElementsInAFullVector(Vector<bfloat16> value); // svlen[_bf16]
  ///   public static unsafe Vector<bfloat16> CreateTrueMaskBFloat16(); // svptrue_b8
  ///   public static unsafe Vector<bfloat16> DuplicateSelectedScalarToVector(bfloat16 value); // svdup[_n]_bf16 or svdup[_n]_bf16_m or svdup[_n]_bf16_x or svdup[_n]_bf16_z
  ///   public static unsafe Vector<bfloat16> LoadVector(Vector<bfloat16> mask, bfloat16* address, long vnum); // svld1_vnum[_bf16]
  ///   public static unsafe Vector<bfloat16> LoadVectorFirstFaulting(Vector<bfloat16> mask, bfloat16* address, long vnum); // svldff1_vnum[_bf16]
  ///   public static unsafe Vector<bfloat16> LoadVectorNonFaulting(bfloat16* address, long vnum); // svldnf1_vnum[_bf16]
  ///   public static unsafe Vector<bfloat16> LoadVectorNonTemporal(Vector<bfloat16> mask, bfloat16* address, long vnum); // svldnt1_vnum[_bf16]
  ///   public static unsafe (Vector<bfloat16>, Vector<bfloat16>) LoadVectorx2(Vector<bfloat16> mask, bfloat16* address, long vnum); // svld2_vnum[_bf16]
  ///   public static unsafe (Vector<bfloat16>, Vector<bfloat16>, Vector<bfloat16>) LoadVectorx3(Vector<bfloat16> mask, bfloat16* address, long vnum); // svld3_vnum[_bf16]
  ///   public static unsafe (Vector<bfloat16>, Vector<bfloat16>, Vector<bfloat16>, Vector<bfloat16>) LoadVectorx4(Vector<bfloat16> mask, bfloat16* address, long vnum); // svld4_vnum[_bf16]
  ///   public static unsafe void Store(Vector<bfloat16> mask, bfloat16* base, long vnum, Vector<bfloat16> data); // svst1_vnum[_bf16]
  ///   public static unsafe void Store(Vector<bfloat16> mask, bfloat16* base, long vnum, (Vector<bfloat16> data1, Vector<bfloat16> data2)); // svst2_vnum[_bf16]
  ///   public static unsafe void Store(Vector<bfloat16> mask, bfloat16* base, long vnum, (Vector<bfloat16> data1, Vector<bfloat16> data2, Vector<bfloat16> data3)); // svst3_vnum[_bf16]
  ///   public static unsafe void Store(Vector<bfloat16> mask, bfloat16* base, long vnum, (Vector<bfloat16> data1, Vector<bfloat16> data2, Vector<bfloat16> data3, Vector<bfloat16> data4)); // svst4_vnum[_bf16]
  ///   public static unsafe void StoreNonTemporal(Vector<bfloat16> mask, bfloat16* base, long vnum, Vector<bfloat16> data); // svstnt1_vnum[_bf16]
  ///   Total Rejected: 15

  /// Total ACLE covered across API:      93

