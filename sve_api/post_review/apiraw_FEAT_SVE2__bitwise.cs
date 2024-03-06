namespace System.Runtime.Intrinsics.Arm;

/// VectorT Summary
public abstract class Sve2 : Sve /// Feature: FEAT_SVE2  Category: bitwise
{
  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  /// op1 ^ (op2 & ~op3)
  public static unsafe Vector<T> BitwiseClearXor(Vector<T> xor, Vector<T> value, Vector<T> mask); // BCAX // MOVPRFX

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> BitwiseSelect(Vector<T> select, Vector<T> left, Vector<T> right); // BSL // MOVPRFX

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> BitwiseSelectLeftInverted(Vector<T> select, Vector<T> left, Vector<T> right); // BSL1N // MOVPRFX

    /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> BitwiseSelectRightInverted(Vector<T> select, Vector<T> left, Vector<T> right); // BSL2N // MOVPRFX

  /// T: sbyte, short, int, long
  public static unsafe Vector<T> ShiftArithmeticRounded(Vector<T> value, Vector<T> count); // SRSHL or SRSHLR // predicated, MOVPRFX

  /// T: [byte, sbyte], [ushort, short], [uint, int], [ulong, long]
  public static unsafe Vector<T> ShiftLogicalRounded(Vector<T> value, Vector<T2> count); // URSHL or URSHLR // predicated, MOVPRFX

  /// T: sbyte, short, int, long
  public static unsafe Vector<T> ShiftRightArithmeticRounded(Vector<T> value, [ConstantExpected] byte count); // SRSHR // predicated, MOVPRFX

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> ShiftRightLogicalRounded(Vector<T> value, [ConstantExpected] byte count); // URSHR // predicated, MOVPRFX

  /// T: sbyte, short, int, long
  public static unsafe Vector<T> ShiftRightArithmeticRoundedAdd(Vector<T> addend, Vector<T> value, [ConstantExpected] byte count); // SRSRA // MOVPRFX

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> ShiftRightLogicalRoundedAdd(Vector<T> addend, Vector<T> value, [ConstantExpected] byte count); // URSRA // MOVPRFX

  /// T: [sbyte, short], [short, int], [int, long], [byte, ushort], [ushort, uint], [uint, ulong]
  public static unsafe Vector<T> ShiftRightLogicalRoundedNarrowingEven(Vector<T2> value, [ConstantExpected] byte count); // RSHRNB

  /// T: [sbyte, short], [short, int], [int, long], [byte, ushort], [ushort, uint], [uint, ulong]
  public static unsafe Vector<T> ShiftRightLogicalRoundedNarrowingOdd(Vector<T> even, Vector<T2> value, [ConstantExpected] byte count); // RSHRNT

  /// T: sbyte, short, int, long
  public static unsafe Vector<T> ShiftArithmeticRoundedSaturate(Vector<T> value, Vector<T> count); // SQRSHL or SQRSHLR // predicated, MOVPRFX

  /// T: [byte, sbyte], [ushort, short], [uint, int], [ulong, long]
  public static unsafe Vector<T> ShiftLogicalRoundedSaturate(Vector<T> value, Vector<T2> count); // UQRSHL or UQRSHLR // predicated, MOVPRFX

  /// T: [sbyte, short], [short, int], [int, long]
  public static unsafe Vector<T> ShiftRightArithmeticRoundedNarrowingSaturateEven(Vector<T2> value, [ConstantExpected] byte count); // SQRSHRNB

  /// T: [sbyte, short], [short, int], [int, long]
  public static unsafe Vector<T> ShiftRightArithmeticRoundedNarrowingSaturateOdd(Vector<T> even, Vector<T2> value, [ConstantExpected] byte count); // SQRSHRNT 
  
  /// T: [sbyte, short], [short, int], [int, long], [byte, ushort], [ushort, uint], [uint, ulong]
  public static unsafe Vector<T> ShiftRightLogicalRoundedNarrowingSaturateEven(Vector<T2> value, [ConstantExpected] byte count); // UQRSHRNB

  /// T: [sbyte, short], [short, int], [int, long], [byte, ushort], [ushort, uint], [uint, ulong]
  public static unsafe Vector<T> ShiftRightLogicalRoundedNarrowingSaturateOdd(Vector<T> even, Vector<T2> value, [ConstantExpected] byte count); // UQRSHRNT

  /// T: [byte, short], [ushort, int], [uint, long]
  public static unsafe Vector<T> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedEven(Vector<T2> value, [ConstantExpected] byte count); // SQRSHRUNB

  /// T: [byte, short], [ushort, int], [uint, long]
  public static unsafe Vector<T> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedOdd(Vector<T> even, Vector<T2> value, [ConstantExpected] byte count); // SQRSHRUNT

  /// T: sbyte, short, int, long
  public static unsafe Vector<T> ShiftArithmeticSaturate(Vector<T> value, Vector<T> count); // SQSHL or SQSHLR // predicated, MOVPRFX

  /// T: [byte, sbyte], [ushort, short], [uint, int], [ulong, long]
  public static unsafe Vector<T> ShiftLeftLogicalSaturate(Vector<T> value, Vector<T2> count); // UQSHL or UQSHLR // predicated, MOVPRFX

  /// T: [byte, sbyte], [ushort, short], [uint, int], [ulong, long]
  public static unsafe Vector<T> ShiftLeftLogicalSaturateUnsigned(Vector<T2> value, [ConstantExpected] byte count); // SQSHLU // predicated, MOVPRFX

  /// T: [sbyte, short], [short, int], [int, long], [byte, ushort], [ushort, uint], [uint, ulong]
  public static unsafe Vector<T>  ShiftRightArithmeticNarrowingSaturateEven(Vector<T2> value, [ConstantExpected] byte count); // SQSHRNB or UQSHRNB

  /// T: [sbyte, short], [short, int], [int, long], [byte, ushort], [ushort, uint], [uint, ulong]
  public static unsafe Vector<T>  ShiftRightArithmeticNarrowingSaturateOdd(Vector<T> even, Vector<T2> value, [ConstantExpected] byte count); // SQSHRNT or UQSHRNT

  /// T: [byte, short], [ushort, int], [uint, long]
  public static unsafe Vector<T> ShiftRightArithmeticNarrowingSaturateUnsignedEven(Vector<T2> value, [ConstantExpected] byte count); // SQSHRUNB

  /// T: [byte, short], [ushort, int], [uint, long]
  public static unsafe Vector<T> ShiftRightArithmeticNarrowingSaturateUnsignedOdd(Vector<T> even, Vector<T2> value, [ConstantExpected] byte count); // SQSHRUNT

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> ShiftLeftAndInsert(Vector<T> left, Vector<T> right, [ConstantExpected] byte shift); // SLI

  /// T: [short, sbyte], [int, short], [long, int], [ushort, byte], [uint, ushort], [ulong, uint]
  public static unsafe Vector<T> ShiftLeftLogicalWideningEven(Vector<T2> value, [ConstantExpected] byte count); // SSHLLB or USHLLB

  /// T: [short, sbyte], [int, short], [long, int], [ushort, byte], [uint, ushort], [ulong, uint]
  public static unsafe Vector<T> ShiftLeftLogicalWideningOdd(Vector<T2> value, [ConstantExpected] byte count); // SSHLLT or USHLLT

  /// T: sbyte, short, int, long
  public static unsafe Vector<T> ShiftRightArithmeticAdd(Vector<T> addend, Vector<T> value, [ConstantExpected] byte count); // SSRA // MOVPRFX
  
  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> ShiftRightLogicalAdd(Vector<T> addend, Vector<T> value, [ConstantExpected] byte count); // USRA // MOVPRFX

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> ShiftRightAndInsert(Vector<T> left, Vector<T> right, [ConstantExpected] byte shift); // SRI

  /// T: [sbyte, short], [short, int], [int, long], [byte, ushort], [ushort, uint], [uint, ulong]
  public static unsafe Vector<T> ShiftRightLogicalNarrowingEven(Vector<T2> value, [ConstantExpected] byte count); // SHRNB

  /// T: [sbyte, short], [short, int], [int, long], [byte, ushort], [ushort, uint], [uint, ulong]
  public static unsafe Vector<T> ShiftRightLogicalNarrowingOdd(Vector<T> even, Vector<T2> value, [ConstantExpected] byte count); // SHRNT

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> Xor(Vector<T> value1, Vector<T> value2, Vector<T> value3); // EOR3 // MOVPRFX

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> XorRotateRight(Vector<T> left, Vector<T> right, [ConstantExpected] byte count]); // XAR // MOVPRFX

  /// total method signatures: 33
}
