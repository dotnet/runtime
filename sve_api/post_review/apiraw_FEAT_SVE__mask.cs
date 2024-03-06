namespace System.Runtime.Intrinsics.Arm;

/// VectorT Summary
public abstract partial class Sve : AdvSimd /// Feature: FEAT_SVE  Category: mask
{
  /// T: float, double
  public static unsafe Vector<T> AbsoluteCompareGreaterThan(Vector<T> left, Vector<T> right); // FACGT // predicated

  /// T: float, double
  public static unsafe Vector<T> AbsoluteCompareGreaterThanOrEqual(Vector<T> left, Vector<T> right); // FACGE // predicated

  /// T: float, double
  public static unsafe Vector<T> AbsoluteCompareLessThan(Vector<T> left, Vector<T> right); // FACGT // predicated

  /// T: float, double
  public static unsafe Vector<T> AbsoluteCompareLessThanOrEqual(Vector<T> left, Vector<T> right); // FACGE // predicated

  /// T: float, double, int, long, uint, ulong
  public static unsafe Vector<T> Compact(Vector<T> mask, Vector<T> value); // COMPACT

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> CompareEqual(Vector<T> left, Vector<T> right); // FCMEQ or CMPEQ // predicated

  /// T: [sbyte, long], [short, long], [int, long]
  public static unsafe Vector<T> CompareEqual(Vector<T> left, Vector<T2> right); // CMPEQ // predicated

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> CompareGreaterThan(Vector<T> left, Vector<T> right); // FCMGT or CMPGT or CMPHI // predicated

  /// T: [sbyte, long], [short, long], [int, long], [byte, ulong], [ushort, ulong], [uint, ulong]
  public static unsafe Vector<T> CompareGreaterThan(Vector<T> left, Vector<T2> right); // CMPGT or CMPHI // predicated

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> CompareGreaterThanOrEqual(Vector<T> left, Vector<T> right); // FCMGE or CMPGE or CMPHS // predicated

  /// T: [sbyte, long], [short, long], [int, long], [byte, ulong], [ushort, ulong], [uint, ulong]
  public static unsafe Vector<T> CompareGreaterThanOrEqual(Vector<T> left, Vector<T2> right); // CMPGE or CMPHS // predicated

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> CompareLessThan(Vector<T> left, Vector<T> right); // FCMGT or CMPGT or CMPHI // predicated

  /// T: [sbyte, long], [short, long], [int, long], [byte, ulong], [ushort, ulong], [uint, ulong]
  public static unsafe Vector<T> CompareLessThan(Vector<T> left, Vector<T2> right); // CMPLT or CMPLO // predicated

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> CompareLessThanOrEqual(Vector<T> left, Vector<T> right); // FCMGE or CMPGE or CMPHS // predicated

  /// T: [sbyte, long], [short, long], [int, long], [byte, ulong], [ushort, ulong], [uint, ulong]
  public static unsafe Vector<T> CompareLessThanOrEqual(Vector<T> left, Vector<T2> right); // CMPLE or CMPLS // predicated

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> CompareNotEqualTo(Vector<T> left, Vector<T> right); // FCMNE or CMPNE // predicated

  /// T: [sbyte, long], [short, long], [int, long]
  public static unsafe Vector<T> CompareNotEqualTo(Vector<T> left, Vector<T2> right); // CMPNE // predicated

  /// T: float, double
  public static unsafe Vector<T> CompareUnordered(Vector<T> left, Vector<T> right); // FCMUO // predicated

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> ConditionalExtractAfterLastActiveElement(Vector<T> mask, Vector<T> defaultScalar, Vector<T> data); // CLASTA // MOVPRFX
  public static unsafe Vector<T> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<T> mask, Vector<T> defaultValues, Vector<T> data); // CLASTA // MOVPRFX
  public static unsafe T ConditionalExtractAfterLastActiveElement(Vector<T> mask, T defaultValue, Vector<T> data); // CLASTA // MOVPRFX

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> ConditionalExtractLastActiveElement(Vector<T> mask, Vector<T> defaultScalar, Vector<T> data); // CLASTB // MOVPRFX
  public static unsafe Vector<T> ConditionalExtractLastActiveElementAndReplicate(Vector<T> mask, Vector<T> defaultValues, Vector<T> data); // CLASTB // MOVPRFX
  public static unsafe T ConditionalExtractLastActiveElement(Vector<T> mask, T defaultValue, Vector<T> data); // CLASTB // MOVPRFX
}
