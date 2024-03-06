namespace System.Runtime.Intrinsics.Arm;

/// VectorT Summary
public abstract class Sve : AdvSimd /// Feature: FEAT_SVE  Category: fp
{
  /// T: float, double
  public static unsafe Vector<T> AddRotateComplex(Vector<T> left, Vector<T> right, [ConstantExpected] byte rotation); // FCADD // predicated, MOVPRFX

  /// T: float, double
  public static unsafe Vector<T> AddSequentialAcross(Vector<T> initial, Vector<T> value); // FADDA // predicated

  /// T: [double, float], [double, int], [double, long], [double, uint], [double, ulong]
  public static unsafe Vector<T> ConvertToDouble(Vector<T2> value); // FCVT or SCVTF or UCVTF // predicated, MOVPRFX

  /// T: [int, float], [int, double]
  public static unsafe Vector<T> ConvertToInt32(Vector<T2> value); // FCVTZS // predicated, MOVPRFX

  /// T: [long, float], [long, double]
  public static unsafe Vector<T> ConvertToInt64(Vector<T2> value); // FCVTZS // predicated, MOVPRFX

  /// T: [float, double], [float, int], [float, long], [float, uint], [float, ulong]
  public static unsafe Vector<T> ConvertToSingle(Vector<T2> value); // FCVT or SCVTF or UCVTF // predicated, MOVPRFX

  /// T: [uint, float], [uint, double]
  public static unsafe Vector<T> ConvertToUInt32(Vector<T2> value); // FCVTZU // predicated, MOVPRFX

  /// T: [ulong, float], [ulong, double]
  public static unsafe Vector<T> ConvertToUInt64(Vector<T2> value); // FCVTZU // predicated, MOVPRFX

  /// T: [float, uint], [double, ulong]
  public static unsafe Vector<T> FloatingPointExponentialAccelerator(Vector<T2> value); // FEXPA

  /// T: float, double
  public static unsafe Vector<T> MultiplyAddRotateComplex(Vector<T> addend, Vector<T> left, Vector<T> right, [ConstantExpected] byte rotation); // FCMLA // predicated, MOVPRFX

  public static unsafe Vector<float> MultiplyAddRotateComplexBySelectedScalar(Vector<float> addend, Vector<float> left, Vector<float> right, [ConstantExpected] byte rightIndex, [ConstantExpected] byte rotation); // FCMLA // MOVPRFX

  /// T: float, double
  public static unsafe Vector<T> ReciprocalEstimate(Vector<T> value); // FRECPE

  /// T: float, double
  public static unsafe Vector<T> ReciprocalExponent(Vector<T> value); // FRECPX // predicated, MOVPRFX

  /// T: float, double
  public static unsafe Vector<T> ReciprocalSqrtEstimate(Vector<T> value); // FRSQRTE

  /// T: float, double
  public static unsafe Vector<T> ReciprocalSqrtStep(Vector<T> left, Vector<T> right); // FRSQRTS

  /// T: float, double
  public static unsafe Vector<T> ReciprocalStep(Vector<T> left, Vector<T> right); // FRECPS

  /// T: float, double
  public static unsafe Vector<T> RoundAwayFromZero(Vector<T> value); // FRINTA // predicated, MOVPRFX

  /// T: float, double
  public static unsafe Vector<T> RoundToNearest(Vector<T> value); // FRINTN // predicated, MOVPRFX

  /// T: float, double
  public static unsafe Vector<T> RoundToNegativeInfinity(Vector<T> value); // FRINTM // predicated, MOVPRFX

  /// T: float, double
  public static unsafe Vector<T> RoundToPositiveInfinity(Vector<T> value); // FRINTP // predicated, MOVPRFX

  /// T: float, double
  public static unsafe Vector<T> RoundToZero(Vector<T> value); // FRINTZ // predicated, MOVPRFX

  /// T: [float, int], [double, long]
  public static unsafe Vector<T> Scale(Vector<T> left, Vector<T2> right); // FSCALE // predicated, MOVPRFX

  /// T: float, double
  public static unsafe Vector<T> Sqrt(Vector<T> value); // FSQRT // predicated, MOVPRFX

  /// T: float, double
  public static unsafe Vector<T> TrigonometricMultiplyAddCoefficient(Vector<T> left, Vector<T> right, [ConstantExpected] byte control); // FTMAD // MOVPRFX

  /// T: [float, uint], [double, ulong]
  public static unsafe Vector<T> TrigonometricSelectCoefficient(Vector<T> value, Vector<T2> selector); // FTSSEL

  /// T: [float, uint], [double, ulong]
  public static unsafe Vector<T> TrigonometricStartingValue(Vector<T> value, Vector<T2> sign); // FTSMUL
}