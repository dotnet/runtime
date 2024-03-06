namespace System.Runtime.Intrinsics.Arm;

/// VectorT Summary
public abstract partial class SveF32mm : AdvSimd /// Feature: FEAT_F32MM
{

  public static unsafe Vector<float> MatrixMultiplyAccumulate(Vector<float> op1, Vector<float> op2, Vector<float> op3); // FMMLA // MOVPRFX

  /// total method signatures: 1

}


/// Full API
public abstract partial class SveF32mm : AdvSimd /// Feature: FEAT_F32MM
{
    /// MatrixMultiplyAccumulate : Matrix multiply-accumulate

    /// svfloat32_t svmmla[_f32](svfloat32_t op1, svfloat32_t op2, svfloat32_t op3) : "FMMLA Ztied1.S, Zop2.S, Zop3.S" or "MOVPRFX Zresult, Zop1; FMMLA Zresult.S, Zop2.S, Zop3.S"
  public static unsafe Vector<float> MatrixMultiplyAccumulate(Vector<float> op1, Vector<float> op2, Vector<float> op3);


  /// total method signatures: 1
  /// total method names:      1
}


  /// Total ACLE covered across API:      1

