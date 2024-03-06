namespace System.Runtime.Intrinsics.Arm;

/// VectorT Summary
public abstract partial class Sve : AdvSimd /// Feature: FEAT_SVE  Category: stores
{

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe void Store(Vector<T> mask, T* address, Vector<T> data); // ST1W or ST1D or ST1B or ST1H

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe void Store(Vector<T> mask, T* address, (Vector<T> Value1, Vector<T> Value2) data); // ST2W or ST2D or ST2B or ST2H

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe void Store(Vector<T> mask, T* address, (Vector<T> Value1, Vector<T> Value2, Vector<T> Value3) data); // ST3W or ST3D or ST3B or ST3H

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe void Store(Vector<T> mask, T* address, (Vector<T> Value1, Vector<T> Value2, Vector<T> Value3, Vector<T> Value4) data); // ST4W or ST4D or ST4B or ST4H

  /// T: [short, sbyte], [int, short], [int, sbyte], [long, short], [long, int], [long, sbyte]
  /// T: [ushort, byte], [uint, ushort], [uint, byte], [ulong, ushort], [ulong, uint], [ulong, byte]
  public static unsafe void StoreNarrowing(Vector<T> mask, T2* address, Vector<T> data); // ST1B or ST1H or ST1W

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe void StoreNonTemporal(Vector<T> mask, T* address, Vector<T> data); // STNT1W or STNT1D or STNT1B or STNT1H

  /// total method signatures: 17
}
