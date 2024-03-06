namespace System.Runtime.Intrinsics.Arm;

/// VectorT Summary
public abstract partial class Sm4 : AdvSimd /// Feature: FEAT_SM4
{

  public static unsafe Vector128<uint> Sm4EncryptionAndDecryption(Vector128<uint> a, Vector128<uint> b); // SM4E

  public static unsafe Vector128<uint> Sm4KeyUpdates(Vector128<uint> a, Vector128<uint> b); // SM4EKEY

  /// total method signatures: 2

}


/// Full API
public abstract partial class Sm4 : AdvSimd /// Feature: FEAT_SM4
{
    /// Sm4EncryptionAndDecryption : SM4 Encode takes input data as a 128-bit vector from the first source SIMD&FP register, and four iterations of the round key held as the elements of the 128-bit vector in the second source SIMD&FP register. It encrypts the data by four rounds, in accordance with the SM4 standard, returning the 128-bit result to the destination SIMD&FP register.

    /// uint32x4_t vsm4eq_u32(uint32x4_t a, uint32x4_t b) : "SM4E Vd.4S,Vn.4S"
  public static unsafe Vector128<uint> Sm4EncryptionAndDecryption(Vector128<uint> a, Vector128<uint> b);


    /// Sm4KeyUpdates : SM4 Key takes an input as a 128-bit vector from the first source SIMD&FP register and a 128-bit constant from the second SIMD&FP register. It derives four iterations of the output key, in accordance with the SM4 standard, returning the 128-bit result to the destination SIMD&FP register.

    /// uint32x4_t vsm4ekeyq_u32(uint32x4_t a, uint32x4_t b) : "SM4EKEY Vd.4S,Vn.4S,Vm.4S"
  public static unsafe Vector128<uint> Sm4KeyUpdates(Vector128<uint> a, Vector128<uint> b);


  /// total method signatures: 2
  /// total method names:      2
}


  /// Total ACLE covered across API:      2

