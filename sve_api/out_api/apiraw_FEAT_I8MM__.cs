namespace System.Runtime.Intrinsics.Arm;

/// VectorT Summary
public abstract partial class SveI8mm : AdvSimd /// Feature: FEAT_I8MM
{

  public static unsafe Vector<int> DotProductSignedUnsigned(Vector<int> op1, Vector<sbyte> op2, Vector<byte> op3); // USDOT // MOVPRFX

  public static unsafe Vector<int> DotProductSignedUnsigned(Vector<int> op1, Vector<sbyte> op2, Vector<byte> op3, ulong imm_index); // SUDOT // MOVPRFX

  public static unsafe Vector<int> DotProductUnsignedSigned(Vector<int> op1, Vector<byte> op2, Vector<sbyte> op3); // USDOT // MOVPRFX

  public static unsafe Vector<int> DotProductUnsignedSigned(Vector<int> op1, Vector<byte> op2, Vector<sbyte> op3, ulong imm_index); // USDOT // MOVPRFX

  /// T: [int, sbyte], [uint, byte]
  public static unsafe Vector<T> MatrixMultiplyAccumulate(Vector<T> op1, Vector<T2> op2, Vector<T2> op3); // SMMLA or UMMLA // MOVPRFX

  public static unsafe Vector<int> MatrixMultiplyAccumulateUnsignedSigned(Vector<int> op1, Vector<byte> op2, Vector<sbyte> op3); // USMMLA // MOVPRFX

  /// total method signatures: 6


  /// Optional Entries:

  public static unsafe Vector<int> DotProductSignedUnsigned(Vector<int> op1, Vector<sbyte> op2, byte op3); // USDOT // MOVPRFX

  public static unsafe Vector<int> DotProductUnsignedSigned(Vector<int> op1, Vector<byte> op2, sbyte op3); // USDOT // MOVPRFX

  /// total optional method signatures: 2

}


/// Full API
public abstract partial class SveI8mm : AdvSimd /// Feature: FEAT_I8MM
{
    /// DotProductSignedUnsigned : Dot product (signed × unsigned)

    /// svint32_t svsudot[_s32](svint32_t op1, svint8_t op2, svuint8_t op3) : "USDOT Ztied1.S, Zop3.B, Zop2.B" or "MOVPRFX Zresult, Zop1; USDOT Zresult.S, Zop3.B, Zop2.B"
  public static unsafe Vector<int> DotProductSignedUnsigned(Vector<int> op1, Vector<sbyte> op2, Vector<byte> op3);

    /// svint32_t svsudot_lane[_s32](svint32_t op1, svint8_t op2, svuint8_t op3, uint64_t imm_index) : "SUDOT Ztied1.S, Zop2.B, Zop3.B[imm_index]" or "MOVPRFX Zresult, Zop1; SUDOT Zresult.S, Zop2.B, Zop3.B[imm_index]"
  public static unsafe Vector<int> DotProductSignedUnsigned(Vector<int> op1, Vector<sbyte> op2, Vector<byte> op3, ulong imm_index);


    /// DotProductUnsignedSigned : Dot product (unsigned × signed)

    /// svint32_t svusdot[_s32](svint32_t op1, svuint8_t op2, svint8_t op3) : "USDOT Ztied1.S, Zop2.B, Zop3.B" or "MOVPRFX Zresult, Zop1; USDOT Zresult.S, Zop2.B, Zop3.B"
  public static unsafe Vector<int> DotProductUnsignedSigned(Vector<int> op1, Vector<byte> op2, Vector<sbyte> op3);

    /// svint32_t svusdot_lane[_s32](svint32_t op1, svuint8_t op2, svint8_t op3, uint64_t imm_index) : "USDOT Ztied1.S, Zop2.B, Zop3.B[imm_index]" or "MOVPRFX Zresult, Zop1; USDOT Zresult.S, Zop2.B, Zop3.B[imm_index]"
  public static unsafe Vector<int> DotProductUnsignedSigned(Vector<int> op1, Vector<byte> op2, Vector<sbyte> op3, ulong imm_index);


    /// MatrixMultiplyAccumulate : Matrix multiply-accumulate

    /// svint32_t svmmla[_s32](svint32_t op1, svint8_t op2, svint8_t op3) : "SMMLA Ztied1.S, Zop2.B, Zop3.B" or "MOVPRFX Zresult, Zop1; SMMLA Zresult.S, Zop2.B, Zop3.B"
  public static unsafe Vector<int> MatrixMultiplyAccumulate(Vector<int> op1, Vector<sbyte> op2, Vector<sbyte> op3);

    /// svuint32_t svmmla[_u32](svuint32_t op1, svuint8_t op2, svuint8_t op3) : "UMMLA Ztied1.S, Zop2.B, Zop3.B" or "MOVPRFX Zresult, Zop1; UMMLA Zresult.S, Zop2.B, Zop3.B"
  public static unsafe Vector<uint> MatrixMultiplyAccumulate(Vector<uint> op1, Vector<byte> op2, Vector<byte> op3);


    /// MatrixMultiplyAccumulateUnsignedSigned : Matrix multiply-accumulate (unsigned × signed)

    /// svint32_t svusmmla[_s32](svint32_t op1, svuint8_t op2, svint8_t op3) : "USMMLA Ztied1.S, Zop2.B, Zop3.B" or "MOVPRFX Zresult, Zop1; USMMLA Zresult.S, Zop2.B, Zop3.B"
  public static unsafe Vector<int> MatrixMultiplyAccumulateUnsignedSigned(Vector<int> op1, Vector<byte> op2, Vector<sbyte> op3);


  /// total method signatures: 7
  /// total method names:      4
}

  /// Optional Entries:
  ///   public static unsafe Vector<int> DotProductSignedUnsigned(Vector<int> op1, Vector<sbyte> op2, byte op3); // svsudot[_n_s32]
  ///   public static unsafe Vector<int> DotProductUnsignedSigned(Vector<int> op1, Vector<byte> op2, sbyte op3); // svusdot[_n_s32]
  ///   Total Maybe: 2

  /// Total ACLE covered across API:      9

