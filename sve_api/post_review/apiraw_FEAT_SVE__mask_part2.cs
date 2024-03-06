namespace System.Runtime.Intrinsics.Arm;

/// VectorT Summary
public abstract partial class Sve : AdvSimd /// Feature: FEAT_SVE  Category: mask
{
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
  public static unsafe Vector<T> ExtractAfterLastVector(Vector<T> value); // LASTA // predicated

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe T ExtractLastScalar(Vector<T> value); // LASTB // predicated
  public static unsafe Vector<T> ExtractLastVector(Vector<T> value); // LASTB // predicated

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> ExtractVector(Vector<T> upper, Vector<T> lower, [ConstExpected] byte index); // EXT // MOVPRFX

  public static unsafe Vector<byte> CreateFalseMaskByte(); // PFALSE
  public static unsafe Vector<sbyte> CreateFalseMaskSByte(); // PFALSE
  // repeat for Int16, UInt16, Int32, UInt32, Int64, UInt64, Single, Double

  /// T: byte, ushort, uint, ulong
  public static unsafe Vector<T> CreateMaskForNextActiveElement(Vector<T> totalMask, Vector<T> fromMask); // PNEXT

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> CreateMaskForFirstActiveElement(Vector<T> totalMask, Vector<T> fromMask); // PFIRST

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe bool TestAnyTrue(Vector<T> leftMask, Vector<T> rightMask); // PTEST

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe bool TestFirstTrue(Vector<T> leftMask, Vector<T> rightMask); // PTEST

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe bool TestLastTrue(Vector<T> leftMask, Vector<T> rightMask); // PTEST

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> CreateBreakPropagateMask(Vector<T> totalMask, Vector<T> fromMask); // BRKN // predicated

  public static unsafe Vector<byte> CreateTrueMaskByte([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // PTRUE
  public static unsafe Vector<sbyte> CreateTrueMaskSByte([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // PTRUE
  // repeat for Int16, UInt16, Int32, UInt32, Int64, UInt64, Single, Double
}