namespace System.Runtime.Intrinsics.Arm;

/// VectorT Summary
public abstract partial class SveSha3 : AdvSimd /// Feature: FEAT_SVE_SHA3
{

  /// T: long, ulong
  public static unsafe Vector<T> BitwiseRotateLeftBy1AndXor(Vector<T> left, Vector<T> right); // RAX1

  /// total method signatures: 1

}


/// Full API
public abstract partial class SveSha3 : AdvSimd /// Feature: FEAT_SVE_SHA3
{
    /// BitwiseRotateLeftBy1AndXor : Bitwise rotate left by 1 and exclusive OR

    /// svint64_t svrax1[_s64](svint64_t op1, svint64_t op2) : "RAX1 Zresult.D, Zop1.D, Zop2.D"
  public static unsafe Vector<long> BitwiseRotateLeftBy1AndXor(Vector<long> left, Vector<long> right);

    /// svuint64_t svrax1[_u64](svuint64_t op1, svuint64_t op2) : "RAX1 Zresult.D, Zop1.D, Zop2.D"
  public static unsafe Vector<ulong> BitwiseRotateLeftBy1AndXor(Vector<ulong> left, Vector<ulong> right);


  /// total method signatures: 2
  /// total method names:      1
}


  /// Total ACLE covered across API:      2

