namespace System.Runtime.Intrinsics.Arm;

/// VectorT Summary
public abstract partial class Sve : AdvSimd /// Feature: FEAT_SVE  Category: bitmanipulate
{
  /// T: double, long, ulong, float, sbyte, short, int, byte, ushort, uint
  public static unsafe Vector<T> DuplicateSelectedScalarToVector(Vector<T> data, [ConstantExpected] byte index); // DUP or TBL

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> ReverseBits(Vector<T> value); // RBIT // predicated, MOVPRFX

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> ReverseElement(Vector<T> value); // REV

  /// T: int, long, uint, ulong
  public static unsafe Vector<T> ReverseElement16(Vector<T> value); // REVH // predicated, MOVPRFX

  /// T: long, ulong
  public static unsafe Vector<T> ReverseElement32(Vector<T> value); // REVW // predicated, MOVPRFX

  /// T: short, int, long, ushort, uint, ulong
  public static unsafe Vector<T> ReverseElement8(Vector<T> value); // REVB // predicated, MOVPRFX

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> Splice(Vector<T> mask, Vector<T> left, Vector<T> right); // SPLICE // MOVPRFX

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> TransposeEven(Vector<T> left, Vector<T> right); // TRN1

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> TransposeOdd(Vector<T> left, Vector<T> right); // TRN2

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> UnzipEven(Vector<T> left, Vector<T> right); // UZP1

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> UnzipOdd(Vector<T> left, Vector<T> right); // UZP2

  /// T: [float, uint], [double, ulong], [sbyte, byte], [short, ushort], [int, uint], [long, ulong]
  public static unsafe Vector<T> VectorTableLookup(Vector<T> data, Vector<T2> indices); // TBL

  /// T: byte, ushort, uint, ulong
  public static unsafe Vector<T> VectorTableLookup(Vector<T> data, Vector<T> indices); // TBL

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> ZipHigh(Vector<T> left, Vector<T> right); // ZIP2

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> ZipLow(Vector<T> left, Vector<T> right); // ZIP1

  /// total method signatures: 20

}