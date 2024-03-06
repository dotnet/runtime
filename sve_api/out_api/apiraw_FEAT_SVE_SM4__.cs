namespace System.Runtime.Intrinsics.Arm;

/// VectorT Summary
public abstract partial class SveSm4 : AdvSimd /// Feature: FEAT_SVE_SM4
{

  public static unsafe Vector<uint> Sm4EncryptionAndDecryption(Vector<uint> left, Vector<uint> right); // SM4E

  public static unsafe Vector<uint> Sm4KeyUpdates(Vector<uint> left, Vector<uint> right); // SM4EKEY

  /// total method signatures: 2

}


/// Full API
public abstract partial class SveSm4 : AdvSimd /// Feature: FEAT_SVE_SM4
{
    /// Sm4EncryptionAndDecryption : SM4 encryption and decryption

    /// svuint32_t svsm4e[_u32](svuint32_t op1, svuint32_t op2) : "SM4E Ztied1.S, Ztied1.S, Zop2.S"
  public static unsafe Vector<uint> Sm4EncryptionAndDecryption(Vector<uint> left, Vector<uint> right);


    /// Sm4KeyUpdates : SM4 key updates

    /// svuint32_t svsm4ekey[_u32](svuint32_t op1, svuint32_t op2) : "SM4EKEY Zresult.S, Zop1.S, Zop2.S"
  public static unsafe Vector<uint> Sm4KeyUpdates(Vector<uint> left, Vector<uint> right);


  /// total method signatures: 2
  /// total method names:      2
}


  /// Total ACLE covered across API:      2

