namespace System.Runtime.Intrinsics.Arm;

/// VectorT Summary
public abstract partial class Sve : AdvSimd /// Feature: FEAT_SVE  Category: scatterstores
{

  /// T: [float, uint], [int, uint], [uint, uint], [double, ulong], [long, ulong], [ulong, ulong]
  public static unsafe void Scatter(Vector<T> mask, Vector<T2> addresses, Vector<T> data); // ST1W or ST1D

  /// T: [float, int], [int, int], [uint, int], [float, uint], [int, uint], [uint, uint], [double, long], [long, long], [ulong, long], [double, ulong], [long, ulong], [ulong, ulong]
  public static unsafe void Scatter(Vector<T> mask, T* address, Vector<T2> indices, Vector<T> data); // ST1W or ST1D

scatter16

  public static unsafe void Scatter16BitNarrowing(Vector<int> mask, Vector<uint> addresses, Vector<int> data); // ST1H

  public static unsafe void Scatter16BitNarrowing(Vector<int> mask, short* address, Vector<int> indices, Vector<int> data); // ST1H

  public static unsafe void Scatter16BitNarrowing(Vector<uint> mask, ushort* address, Vector<int> indices, Vector<uint> data); // ST1H

  public static unsafe void Scatter16BitNarrowing(Vector<long> mask, Vector<ulong> addresses, Vector<long> data); // ST1H

  public static unsafe void Scatter16BitNarrowing(Vector<long> mask, short* address, Vector<long> indices, Vector<long> data); // ST1H

  public static unsafe void Scatter16BitNarrowing(Vector<ulong> mask, ushort* address, Vector<long> indices, Vector<ulong> data); // ST1H

  public static unsafe void Scatter16BitNarrowing(Vector<uint> mask, Vector<uint> addresses, Vector<uint> data); // ST1H

  public static unsafe void Scatter16BitNarrowing(Vector<int> mask, short* address, Vector<uint> indices, Vector<int> data); // ST1H

  public static unsafe void Scatter16BitNarrowing(Vector<uint> mask, ushort* address, Vector<uint> indices, Vector<uint> data); // ST1H

  public static unsafe void Scatter16BitNarrowing(Vector<ulong> mask, Vector<ulong> addresses, Vector<ulong> data); // ST1H

  public static unsafe void Scatter16BitNarrowing(Vector<long> mask, short* address, Vector<ulong> indices, Vector<long> data); // ST1H

  public static unsafe void Scatter16BitNarrowing(Vector<ulong> mask, ushort* address, Vector<ulong> indices, Vector<ulong> data); // ST1H

  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<int> mask, short* address, Vector<int> offsets, Vector<int> data); // ST1H

  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<uint> mask, ushort* address, Vector<int> offsets, Vector<uint> data); // ST1H

  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<long> mask, short* address, Vector<long> offsets, Vector<long> data); // ST1H

  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<ulong> mask, ushort* address, Vector<long> offsets, Vector<ulong> data); // ST1H

  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<int> mask, short* address, Vector<uint> offsets, Vector<int> data); // ST1H

  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<uint> mask, ushort* address, Vector<uint> offsets, Vector<uint> data); // ST1H

  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<long> mask, short* address, Vector<ulong> offsets, Vector<long> data); // ST1H

  public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<ulong> mask, ushort* address, Vector<ulong> offsets, Vector<ulong> data); // ST1H

scatter32

  public static unsafe void Scatter32BitNarrowing(Vector<long> mask, Vector<ulong> addresses, Vector<long> data); // ST1W

  public static unsafe void Scatter32BitNarrowing(Vector<long> mask, int* address, Vector<long> indices, Vector<long> data); // ST1W

  public static unsafe void Scatter32BitNarrowing(Vector<ulong> mask, uint* address, Vector<long> indices, Vector<ulong> data); // ST1W

  public static unsafe void Scatter32BitNarrowing(Vector<ulong> mask, Vector<ulong> addresses, Vector<ulong> data); // ST1W

  public static unsafe void Scatter32BitNarrowing(Vector<long> mask, int* address, Vector<ulong> indices, Vector<long> data); // ST1W

  public static unsafe void Scatter32BitNarrowing(Vector<ulong> mask, uint* address, Vector<ulong> indices, Vector<ulong> data); // ST1W

  public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<long> mask, int* address, Vector<long> offsets, Vector<long> data); // ST1W

  public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<ulong> mask, uint* address, Vector<long> offsets, Vector<ulong> data); // ST1W

  public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<long> mask, int* address, Vector<ulong> offsets, Vector<long> data); // ST1W

  public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<ulong> mask, uint* address, Vector<ulong> offsets, Vector<ulong> data); // ST1W


scatter8

  public static unsafe void Scatter8BitNarrowing(Vector<int> mask, Vector<uint> addresses, Vector<int> data); // ST1B

  public static unsafe void Scatter8BitNarrowing(Vector<long> mask, Vector<ulong> addresses, Vector<long> data); // ST1B

  public static unsafe void Scatter8BitNarrowing(Vector<uint> mask, Vector<uint> addresses, Vector<uint> data); // ST1B

  public static unsafe void Scatter8BitNarrowing(Vector<ulong> mask, Vector<ulong> addresses, Vector<ulong> data); // ST1B

  public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<int> mask, sbyte* address, Vector<int> offsets, Vector<int> data); // ST1B

  public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<uint> mask, byte* address, Vector<int> offsets, Vector<uint> data); // ST1B

  public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<long> mask, sbyte* address, Vector<long> offsets, Vector<long> data); // ST1B

  public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<ulong> mask, byte* address, Vector<long> offsets, Vector<ulong> data); // ST1B

  public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<int> mask, sbyte* address, Vector<uint> offsets, Vector<int> data); // ST1B

  public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<uint> mask, byte* address, Vector<uint> offsets, Vector<uint> data); // ST1B

  public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<long> mask, sbyte* address, Vector<ulong> offsets, Vector<long> data); // ST1B

  public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<ulong> mask, byte* address, Vector<ulong> offsets, Vector<ulong> data); // ST1B


  /// T: [float, int], [int, int], [uint, int], [float, uint], [int, uint], [uint, uint], [double, long], [long, long], [ulong, long], [double, ulong], [long, ulong], [ulong, ulong]
  public static unsafe void ScatterWithByteOffsets(Vector<T> mask, T* address, Vector<T2> offsets, Vector<T> data); // ST1W or ST1D

  /// total method signatures: 62
}
