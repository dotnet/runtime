namespace System.Runtime.Intrinsics.Arm;

/// VectorT Summary
public abstract partial class SveAes : AdvSimd /// Feature: FEAT_SVE_AES
{

  public static unsafe Vector<byte> AesInverseMixColumns(Vector<byte> value); // AESIMC

  public static unsafe Vector<byte> AesMixColumns(Vector<byte> value); // AESMC

  public static unsafe Vector<byte> AesSingleRoundDecryption(Vector<byte> left, Vector<byte> right); // AESD

  public static unsafe Vector<byte> AesSingleRoundEncryption(Vector<byte> left, Vector<byte> right); // AESE

  public static unsafe Vector<ulong> PolynomialMultiplyWideningLower(Vector<ulong> left, Vector<ulong> right); // PMULLB

  public static unsafe Vector<ulong> PolynomialMultiplyWideningUpper(Vector<ulong> left, Vector<ulong> right); // PMULLT

  /// total method signatures: 6


  /// Optional Entries:

  public static unsafe Vector<ulong> PolynomialMultiplyWideningLower(Vector<ulong> left, ulong right); // PMULLB

  public static unsafe Vector<ulong> PolynomialMultiplyWideningUpper(Vector<ulong> left, ulong right); // PMULLT

  /// total optional method signatures: 2

}


/// Full API
public abstract partial class SveAes : AdvSimd /// Feature: FEAT_SVE_AES
{
    /// AesInverseMixColumns : AES inverse mix columns

    /// svuint8_t svaesimc[_u8](svuint8_t op) : "AESIMC Ztied.B, Ztied.B"
  public static unsafe Vector<byte> AesInverseMixColumns(Vector<byte> value);


    /// AesMixColumns : AES mix columns

    /// svuint8_t svaesmc[_u8](svuint8_t op) : "AESMC Ztied.B, Ztied.B"
  public static unsafe Vector<byte> AesMixColumns(Vector<byte> value);


    /// AesSingleRoundDecryption : AES single round decryption

    /// svuint8_t svaesd[_u8](svuint8_t op1, svuint8_t op2) : "AESD Ztied1.B, Ztied1.B, Zop2.B" or "AESD Ztied2.B, Ztied2.B, Zop1.B"
  public static unsafe Vector<byte> AesSingleRoundDecryption(Vector<byte> left, Vector<byte> right);


    /// AesSingleRoundEncryption : AES single round encryption

    /// svuint8_t svaese[_u8](svuint8_t op1, svuint8_t op2) : "AESE Ztied1.B, Ztied1.B, Zop2.B" or "AESE Ztied2.B, Ztied2.B, Zop1.B"
  public static unsafe Vector<byte> AesSingleRoundEncryption(Vector<byte> left, Vector<byte> right);


    /// PolynomialMultiplyWideningLower : Polynomial multiply long (bottom)

    /// svuint64_t svpmullb_pair[_u64](svuint64_t op1, svuint64_t op2) : "PMULLB Zresult.Q, Zop1.D, Zop2.D"
  public static unsafe Vector<ulong> PolynomialMultiplyWideningLower(Vector<ulong> left, Vector<ulong> right);


    /// PolynomialMultiplyWideningUpper : Polynomial multiply long (top)

    /// svuint64_t svpmullt_pair[_u64](svuint64_t op1, svuint64_t op2) : "PMULLT Zresult.Q, Zop1.D, Zop2.D"
  public static unsafe Vector<ulong> PolynomialMultiplyWideningUpper(Vector<ulong> left, Vector<ulong> right);


  /// total method signatures: 6
  /// total method names:      6
}

  /// Optional Entries:
  ///   public static unsafe Vector<ulong> PolynomialMultiplyWideningLower(Vector<ulong> left, ulong right); // svpmullb_pair[_n_u64]
  ///   public static unsafe Vector<ulong> PolynomialMultiplyWideningUpper(Vector<ulong> left, ulong right); // svpmullt_pair[_n_u64]
  ///   Total Maybe: 2

  /// Total ACLE covered across API:      8

