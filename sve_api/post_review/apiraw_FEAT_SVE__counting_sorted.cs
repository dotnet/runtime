namespace System.Runtime.Intrinsics.Arm;

/// VectorT Summary
public abstract partial class Sve : AdvSimd /// Feature: FEAT_SVE  Category: counting
{
  public static unsafe ulong Count16BitElements([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // CNTH
  public static unsafe ulong Count32BitElements([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // CNTW
  public static unsafe ulong Count64BitElements([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // CNTD
  public static unsafe ulong Count8BitElements([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // CNTB

  /// T: byte, ushort, uint, ulong, sbyte, short, int, long, float, double
  public static unsafe ulong GetActiveElementCount(Vector<T> mask, Vector<T> from); // CNTP

  /// T: [byte, sbyte], [ushort, short], [uint, int], [ulong, long]
  public static unsafe Vector<T> LeadingSignCount(Vector<T2> value); // CLS // predicated, MOVPRFX

  /// T: [byte, sbyte], [ushort, short], [uint, int], [ulong, long]
  public static unsafe Vector<T> LeadingZeroCount(Vector<T2> value); // CLZ // predicated, MOVPRFX

  /// T: byte, ushort, uint, ulong
  public static unsafe Vector<T> LeadingZeroCount(Vector<T> value); // CLZ // predicated, MOVPRFX

  /// T: [uint, float], [ulong, double], [byte, sbyte], [ushort, short], [uint, int], [ulong, long]
  public static unsafe Vector<T> PopCount(Vector<T2> value); // CNT // predicated, MOVPRFX

  /// T: byte, ushort, uint, ulong
  public static unsafe Vector<T> PopCount(Vector<T> value); // CNT // predicated, MOVPRFX

  public static unsafe int SaturatingDecrementBy16BitElementCount(int value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // SQDECH

  public static unsafe long SaturatingDecrementBy16BitElementCount(long value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // SQDECH

  public static unsafe uint SaturatingDecrementBy16BitElementCount(uint value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // UQDECH

  public static unsafe ulong SaturatingDecrementBy16BitElementCount(ulong value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // UQDECH

  /// T: short, ushort
  public static unsafe Vector<T> SaturatingDecrementBy16BitElementCount(Vector<T> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // SQDECH or UQDECH // MOVPRFX

  public static unsafe int SaturatingDecrementBy32BitElementCount(int value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // SQDECW

  public static unsafe long SaturatingDecrementBy32BitElementCount(long value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // SQDECW

  public static unsafe uint SaturatingDecrementBy32BitElementCount(uint value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // UQDECW

  public static unsafe ulong SaturatingDecrementBy32BitElementCount(ulong value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // UQDECW

  /// T: int, uint
  public static unsafe Vector<T> SaturatingDecrementBy32BitElementCount(Vector<T> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // SQDECW or UQDECW // MOVPRFX

  public static unsafe int SaturatingDecrementBy64BitElementCount(int value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // SQDECD

  public static unsafe long SaturatingDecrementBy64BitElementCount(long value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // SQDECD

  public static unsafe uint SaturatingDecrementBy64BitElementCount(uint value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // UQDECD

  public static unsafe ulong SaturatingDecrementBy64BitElementCount(ulong value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // UQDECD

  /// T: long, ulong
  public static unsafe Vector<T> SaturatingDecrementBy64BitElementCount(Vector<T> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // SQDECD or UQDECD // MOVPRFX

  public static unsafe int SaturatingDecrementBy8BitElementCount(int value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // SQDECB

  public static unsafe long SaturatingDecrementBy8BitElementCount(long value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // SQDECB

  public static unsafe uint SaturatingDecrementBy8BitElementCount(uint value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // UQDECB

  public static unsafe ulong SaturatingDecrementBy8BitElementCount(ulong value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // UQDECB

  /// T: byte, ushort, uint, ulong
  public static unsafe int SaturatingDecrementByActiveElementCount(int value, Vector<T> from); // SQDECP

  /// T: byte, ushort, uint, ulong
  public static unsafe long SaturatingDecrementByActiveElementCount(long value, Vector<T> from); // SQDECP

  /// T: byte, ushort, uint, ulong
  public static unsafe uint SaturatingDecrementByActiveElementCount(uint value, Vector<T> from); // UQDECP

  /// T: byte, ushort, uint, ulong
  public static unsafe ulong SaturatingDecrementByActiveElementCount(ulong value, Vector<T> from); // UQDECP

  /// T: short, int, long, ushort, uint, ulong
  public static unsafe Vector<T> SaturatingDecrementByActiveElementCount(Vector<T> value, Vector<T> from); // SQDECP or UQDECP // MOVPRFX

  public static unsafe int SaturatingIncrementBy16BitElementCount(int value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // SQINCH

  public static unsafe long SaturatingIncrementBy16BitElementCount(long value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // SQINCH

  public static unsafe uint SaturatingIncrementBy16BitElementCount(uint value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // UQINCH

  public static unsafe ulong SaturatingIncrementBy16BitElementCount(ulong value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // UQINCH

  /// T: short, ushort
  public static unsafe Vector<T> SaturatingIncrementBy16BitElementCount(Vector<T> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // SQINCH or UQINCH // MOVPRFX

  public static unsafe int SaturatingIncrementBy32BitElementCount(int value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // SQINCW

  public static unsafe long SaturatingIncrementBy32BitElementCount(long value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // SQINCW

  public static unsafe uint SaturatingIncrementBy32BitElementCount(uint value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // UQINCW

  public static unsafe ulong SaturatingIncrementBy32BitElementCount(ulong value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // UQINCW

  /// T: int, uint
  public static unsafe Vector<T> SaturatingIncrementBy32BitElementCount(Vector<T> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // SQINCW or UQINCW // MOVPRFX

  public static unsafe int SaturatingIncrementBy64BitElementCount(int value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // SQINCD

  public static unsafe long SaturatingIncrementBy64BitElementCount(long value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // SQINCD

  public static unsafe uint SaturatingIncrementBy64BitElementCount(uint value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // UQINCD

  public static unsafe ulong SaturatingIncrementBy64BitElementCount(ulong value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // UQINCD

  /// T: long, ulong
  public static unsafe Vector<T> SaturatingIncrementBy64BitElementCount(Vector<T> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // SQINCD or UQINCD // MOVPRFX

  public static unsafe int SaturatingIncrementBy8BitElementCount(int value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // SQINCB

  public static unsafe long SaturatingIncrementBy8BitElementCount(long value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // SQINCB

  public static unsafe uint SaturatingIncrementBy8BitElementCount(uint value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // UQINCB

  public static unsafe ulong SaturatingIncrementBy8BitElementCount(ulong value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All); // UQINCB

  /// T: byte, ushort, uint, ulong
  public static unsafe int SaturatingIncrementByActiveElementCount(int value, Vector<T> from); // SQINCP

  /// T: byte, ushort, uint, ulong
  public static unsafe long SaturatingIncrementByActiveElementCount(long value, Vector<T> from); // SQINCP

  /// T: byte, ushort, uint, ulong
  public static unsafe uint SaturatingIncrementByActiveElementCount(uint value, Vector<T> from); // UQINCP

  /// T: byte, ushort, uint, ulong
  public static unsafe ulong SaturatingIncrementByActiveElementCount(ulong value, Vector<T> from); // UQINCP

  /// T: short, int, long, ushort, uint, ulong
  public static unsafe Vector<T> SaturatingIncrementByActiveElementCount(Vector<T> value, Vector<T> from); // SQINCP or UQINCP // MOVPRFX
}

  // All patterns used by PTRUE.
  public enum SveMaskPattern : byte
  {
    LargestPowerOf2 = 0,   // The largest power of 2.
    VectorCount1 = 1,    // 1 element.
    VectorCount2 = 2,    // 2 elements.
    VectorCount3 = 3,    // 3 elements.
    VectorCount4 = 4,    // 4 elements.
    VectorCount5 = 5,    // 5 elements.
    VectorCount6 = 6,    // 6 elements.
    VectorCount7 = 7,    // 7 elements.
    VectorCount8 = 8,    // 8 elements.
    VectorCount16 = 9,   // 16 elements.
    VectorCount32 = 10,  // 32 elements.
    VectorCount64 = 11,  // 64 elements.
    VectorCount128 = 12, // 128 elements.
    VectorCount256 = 13, // 256 elements.
    LargestMultipleOf4 = 29,  // The largest multiple of 4.
    LargestMultipleOf3 = 30,  // The largest multiple of 3.
    All  = 31    // All available (implicitly a multiple of two).
  };
