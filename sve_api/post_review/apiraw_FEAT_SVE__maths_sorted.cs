namespace System.Runtime.Intrinsics.Arm;

public abstract partial class Sve : AdvSimd
{
  /// T: float, double, sbyte, short, int, long
  public static unsafe Vector<T> Abs(Vector<T> value);

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> AbsoluteDifference(Vector<T> left, Vector<T> right);

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> Add(Vector<T> left, Vector<T> right);

  /// T: float, double, long, ulong
  public static unsafe Vector<T> AddAcross(Vector<T> value);

  /// T: [long, sbyte], [long, short], [long, int], [ulong, byte], [ulong, ushort], [ulong, uint]
  public static unsafe Vector<T> AddAcross(Vector<T2> value);

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> AddSaturate(Vector<T> left, Vector<T> right);

  /// T: float, double, int, long, uint, ulong
  public static unsafe Vector<T> Divide(Vector<T> left, Vector<T> right);

  /// T: [int, sbyte], [long, short], [uint, byte], [ulong, ushort]
  public static unsafe Vector<T> DotProduct(Vector<T> addend, Vector<T2> left, Vector<T2> right);

  /// T: [int, sbyte], [long, short], [uint, byte], [ulong, ushort]
  public static unsafe Vector<T> DotProductBySelectedScalar(Vector<T> addend, Vector<T2> left, Vector<T2> right, [ConstantExpected] byte rightIndex);

  /// T: float, double
  public static unsafe Vector<T> FusedMultiplyAdd(Vector<T> addend, Vector<T> left, Vector<T> right);

  /// T: float, double
  public static unsafe Vector<T> FusedMultiplyAddBySelectedScalar(Vector<T> addend, Vector<T> left, Vector<T> right, [ConstantExpected] byte rightIndex);

  /// T: float, double
  public static unsafe Vector<T> FusedMultiplyAddNegated(Vector<T> addend, Vector<T> left, Vector<T> right);

  /// T: float, double
  public static unsafe Vector<T> FusedMultiplySubtract(Vector<T> minuend, Vector<T> left, Vector<T> right);

  /// T: float, double
  public static unsafe Vector<T> FusedMultiplySubtractBySelectedScalar(Vector<T> minuend, Vector<T> left, Vector<T> right, [ConstantExpected] byte rightIndex);

  /// T: float, double
  public static unsafe Vector<T> FusedMultiplySubtractNegated(Vector<T> minuend, Vector<T> left, Vector<T> right);

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> Max(Vector<T> left, Vector<T> right);

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> MaxAcross(Vector<T> value);

  /// T: float, double
  public static unsafe Vector<T> MaxNumber(Vector<T> left, Vector<T> right);

  /// T: float, double
  public static unsafe Vector<T> MaxNumberAcross(Vector<T> value);

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> Min(Vector<T> left, Vector<T> right);

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> MinAcross(Vector<T> value);

  /// T: float, double
  public static unsafe Vector<T> MinNumber(Vector<T> left, Vector<T> right);

  /// T: float, double
  public static unsafe Vector<T> MinNumberAcross(Vector<T> value);

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> Multiply(Vector<T> left, Vector<T> right);

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> MultiplyAdd(Vector<T> addend, Vector<T> left, Vector<T> right);

  /// T: float, double
  public static unsafe Vector<T> MultiplyBySelectedScalar(Vector<T> left, Vector<T> right, [ConstantExpected] byte rightIndex);

  /// T: float, double
  public static unsafe Vector<T> MultiplyExtended(Vector<T> left, Vector<T> right);

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> MultiplySubtract(Vector<T> minuend, Vector<T> left, Vector<T> right);

  /// T: float, double, sbyte, short, int, long
  public static unsafe Vector<T> Negate(Vector<T> value);

  /// T: int, long
  public static unsafe Vector<T> SignExtend16(Vector<T> value);

  public static unsafe Vector<long> SignExtend32(Vector<long> value);

  /// T: short, int, long
  public static unsafe Vector<T> SignExtend8(Vector<T> value);

  /// T: [short, sbyte], [int, short], [long, int]
  public static unsafe Vector<T> SignExtendWideningLower(Vector<T2> value);

  /// T: [short, sbyte], [int, short], [long, int]
  public static unsafe Vector<T> SignExtendWideningUpper(Vector<T2> value);

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> Subtract(Vector<T> left, Vector<T> right);

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> SubtractSaturate(Vector<T> left, Vector<T> right);

  /// T: uint, ulong
  public static unsafe Vector<T> ZeroExtend16(Vector<T> value);

  public static unsafe Vector<ulong> ZeroExtend32(Vector<ulong> value);

  /// T: ushort, uint, ulong
  public static unsafe Vector<T> ZeroExtend8(Vector<T> value);

  /// T: [ushort, byte], [uint, ushort], [ulong, uint]
  public static unsafe Vector<T> ZeroExtendWideningLower(Vector<T2> value);

  /// T: [ushort, byte], [uint, ushort], [ulong, uint]
  public static unsafe Vector<T> ZeroExtendWideningUpper(Vector<T2> value);
}