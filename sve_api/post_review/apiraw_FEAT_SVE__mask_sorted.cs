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

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe T ConditionalExtractAfterLastActiveElement(Vector<T> mask, T defaultValue, Vector<T> data); // CLASTA // MOVPRFX

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<T> mask, Vector<T> defaultValues, Vector<T> data); // CLASTA // MOVPRFX

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> ConditionalExtractLastActiveElement(Vector<T> mask, Vector<T> defaultScalar, Vector<T> data); // CLASTB // MOVPRFX

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe T ConditionalExtractLastActiveElement(Vector<T> mask, T defaultValue, Vector<T> data); // CLASTB // MOVPRFX

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> ConditionalExtractLastActiveElementAndReplicate(Vector<T> mask, Vector<T> defaultValues, Vector<T> data); // CLASTB // MOVPRFX

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> ConditionalSelect(Vector<T> mask, Vector<T> left, Vector<T> right); // SEL

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> CreateBreakAfterMask(Vector<T> totalMask, Vector<T> fromMask); // BRKA // predicated

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> CreateBreakAfterPropagateMask(Vector<T> mask, Vector<T> left, Vector<T> right); // BRKPA

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> CreateBreakBeforeMask(Vector<T> totalMask, Vector<T> fromMask); // BRKB // predicated

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> CreateBreakBeforePropagateMask(Vector<T> mask, Vector<T> left, Vector<T> right); // BRKPB

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> CreateBreakPropagateMask(Vector<T> totalMask, Vector<T> fromMask); // BRKN // predicated

  public static unsafe Vector<byte> CreateFalseMaskByte(); // PFALSE
  public static unsafe Vector<sbyte> CreateFalseMaskSByte(); // PFALSE
  // repeat for Int16, UInt16, Int32, UInt32, Int64, UInt64, Single, Double

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> CreateMaskForFirstActiveElement(Vector<T> totalMask, Vector<T> fromMask); // PFIRST

  /// T: byte, ushort, uint, ulong
  public static unsafe Vector<T> CreateMaskForNextActiveElement(Vector<T> totalMask, Vector<T> fromMask); // PNEXT

  public static unsafe Vector<byte> CreateTrueMaskByte([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // PTRUE
  public static unsafe Vector<sbyte> CreateTrueMaskSByte([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // PTRUE
  // repeat for Int16, UInt16, Int32, UInt32, Int64, UInt64, Single, Double

  /// T: byte, ushort, uint, ulong
  public static unsafe Vector<T> CreateWhileLessThanMask{8,16,32,64}Bit(int left, int right); // WHILELT

  /// T: byte, ushort, uint, ulong
  public static unsafe Vector<T> CreateWhileLessThanMask{8,16,32,64}Bit(long left, long right); // WHILELT

  /// T: byte, ushort, uint, ulong
  public static unsafe Vector<T> CreateWhileLessThanMask{8,16,32,64}Bit(uint left, uint right); // WHILELO

  /// T: byte, ushort, uint, ulong
  public static unsafe Vector<T> CreateWhileLessThanMask{8,16,32,64}Bit(ulong left, ulong right); // WHILELO

  /// T: byte, ushort, uint, ulong
  public static unsafe Vector<T> CreateWhileLessThanOrEqualMask{8,16,32,64}Bit(int left, int right); // WHILELE

  /// T: byte, ushort, uint, ulong
  public static unsafe Vector<T> CreateWhileLessThanOrEqualMask{8,16,32,64}Bit(long left, long right); // WHILELE

  /// T: byte, ushort, uint, ulong
  public static unsafe Vector<T> CreateWhileLessThanOrEqualMask{8,16,32,64}Bit(uint left, uint right); // WHILELS

  /// T: byte, ushort, uint, ulong
  public static unsafe Vector<T> CreateWhileLessThanOrEqualMask{8,16,32,64}Bit(ulong left, ulong right); // WHILELS

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe T ExtractAfterLastScalar(Vector<T> value); // LASTA // predicated

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> ExtractAfterLastVector(Vector<T> value); // LASTA // predicated

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe T ExtractLastScalar(Vector<T> value); // LASTB // predicated

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> ExtractLastVector(Vector<T> value); // LASTB // predicated

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> ExtractVector(Vector<T> upper, Vector<T> lower, [ConstExpected] byte index); // EXT // MOVPRFX

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe bool TestAnyTrue(Vector<T> leftMask, Vector<T> rightMask); // PTEST

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe bool TestFirstTrue(Vector<T> leftMask, Vector<T> rightMask); // PTEST

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe bool TestLastTrue(Vector<T> leftMask, Vector<T> rightMask); // PTEST

}
