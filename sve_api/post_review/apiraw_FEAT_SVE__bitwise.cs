namespace System.Runtime.Intrinsics.Arm;

/// VectorT Summary
public abstract class Sve : AdvSimd /// Feature: FEAT_SVE  Category: bitwise
{
  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> And(Vector<T> left, Vector<T> right); // AND // predicated, MOVPRFX

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> AndAcross(Vector<T> value); // ANDV // predicated

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> AndNot(Vector<T> left, Vector<T> right); // NAND // predicated

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> BitwiseClear(Vector<T> left, Vector<T> right); // BIC // predicated, MOVPRFX

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> BooleanNot(Vector<T> value); // CNOT // predicated, MOVPRFX

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> InsertIntoShiftedVector(Vector<T> left, T right); // INSR

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> Not(Vector<T> value); // NOT or EOR // predicated, MOVPRFX

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> Or(Vector<T> left, Vector<T> right); // ORR // predicated, MOVPRFX

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> OrAcross(Vector<T> value); // ORV // predicated

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> OrNot(Vector<T> left, Vector<T> right); // NOR or ORN // predicated

  /// T: [sbyte, byte], [short, ushort], [int, uint], [long, ulong], [sbyte, ulong], [short, ulong], [int, ulong], [byte, ulong], [ushort, ulong], [uint, ulong]
  public static unsafe Vector<T> ShiftLeftLogical(Vector<T> left, Vector<T2> right); // LSL or LSLR // predicated, MOVPRFX

  /// T: byte, ushort, uint, ulong
  public static unsafe Vector<T> ShiftLeftLogical(Vector<T> left, Vector<T> right); // LSL or LSLR // predicated, MOVPRFX

  /// T: [sbyte, byte], [short, ushort], [int, uint], [long, ulong], [sbyte, ulong], [short, ulong], [int, ulong]
  public static unsafe Vector<T> ShiftRightArithmetic(Vector<T> left, Vector<T2> right); // ASR or ASRR // predicated, MOVPRFX

  /// T: sbyte, short, int, long
  public static unsafe Vector<T> ShiftRightArithmeticForDivide(Vector<T> value, [ConstantExpected] byte control); // ASRD // predicated, MOVPRFX

  /// T: byte, ushort, uint, ulong
  public static unsafe Vector<T> ShiftRightLogical(Vector<T> left, Vector<T> right); // LSR or LSRR // predicated, MOVPRFX

  /// T: [byte, ulong], [ushort, ulong], [uint, ulong]
  public static unsafe Vector<T> ShiftRightLogical(Vector<T> left, Vector<T2> right); // LSR // predicated, MOVPRFX

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> Xor(Vector<T> left, Vector<T> right); // EOR // predicated, MOVPRFX

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> XorAcross(Vector<T> value); // EORV // predicated

}