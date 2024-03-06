namespace System.Runtime.Intrinsics.Arm;

/// VectorT Summary
public abstract partial class Sve : AdvSimd /// Feature: FEAT_SVE  Category: firstfaulting
{

  /// T: [int, uint], [long, ulong]
  public static unsafe Vector<T> GatherVectorByteZeroExtendFirstFaulting(Vector<T> mask, Vector<T2> addresses); // LDFF1B

  /// T: uint, ulong
  public static unsafe Vector<T> GatherVectorByteZeroExtendFirstFaulting(Vector<T> mask, Vector<T> addresses); // LDFF1B

  /// T: int, uint, long, ulong
  public static unsafe Vector<T> GatherVectorByteWithByteOffsetZeroExtendFirstFaulting(Vector<T> mask, byte* address, Vector<T> offsets); // LDFF1B

  /// T: [uint, int], [int, uint], [ulong, long], [long, ulong]
  public static unsafe Vector<T> GatherVectorByteWithByteOffsetZeroExtendFirstFaulting(Vector<T> mask, byte* address, Vector<T2> offsets); // LDFF1B

  /// T: [float, uint], [int, uint], [double, ulong], [long, ulong]
  public static unsafe Vector<T> GatherVectorFirstFaulting(Vector<T> mask, Vector<T2> addresses); // LDFF1W or LDFF1D

  /// T: uint, ulong
  public static unsafe Vector<T> GatherVectorFirstFaulting(Vector<T> mask, Vector<T> addresses); // LDFF1W or LDFF1D

  /// T: [float, int], [uint, int], [float, uint], [int, uint], [double, long], [ulong, long], [double, ulong], [long, ulong]
  public static unsafe Vector<T> GatherVectorWithByteOffsetFirstFaulting(Vector<T> mask, T* address, Vector<T2> offsets); // LDFF1W or LDFF1D

  /// T: int, uint, long, ulong
  public static unsafe Vector<T> GatherVectorWithByteOffsetFirstFaulting(Vector<T> mask, T* address, Vector<T> offsets); // LDFF1W or LDFF1D

  /// T: [float, int], [uint, int], [float, uint], [int, uint], [double, long], [ulong, long], [double, ulong], [long, ulong]
  public static unsafe Vector<T> GatherVectorFirstFaulting(Vector<T> mask, T* address, Vector<T2> indices); // LDFF1W or LDFF1D

  /// T: int, uint, long, ulong
  public static unsafe Vector<T> GatherVectorFirstFaulting(Vector<T> mask, T* address, Vector<T> indices); // LDFF1W or LDFF1D

  /// T: [int, uint], [long, ulong]
  public static unsafe Vector<T> GatherVectorInt16SignExtendFirstFaulting(Vector<T> mask, Vector<T2> addresses); // LDFF1SH

  /// T: uint, ulong
  public static unsafe Vector<T> GatherVectorInt16SignExtendFirstFaulting(Vector<T> mask, Vector<T> addresses); // LDFF1SH

  /// T: int, uint, long, ulong
  public static unsafe Vector<T> GatherVectorInt16WithByteOffsetSignExtendFirstFaulting(Vector<T> mask, short* address, Vector<T> offsets); // LDFF1SH

  /// T: [uint, int], [int, uint], [ulong, long], [long, ulong]
  public static unsafe Vector<T> GatherVectorInt16WithByteOffsetSignExtendFirstFaulting(Vector<T> mask, short* address, Vector<T2> offsets); // LDFF1SH

  /// T: int, uint, long, ulong
  public static unsafe Vector<T> GatherVectorInt16SignExtendFirstFaulting(Vector<T> mask, short* address, Vector<T> indices); // LDFF1SH

  /// T: [uint, int], [int, uint], [ulong, long], [long, ulong]
  public static unsafe Vector<T> GatherVectorInt16SignExtendFirstFaulting(Vector<T> mask, short* address, Vector<T2> indices); // LDFF1SH

  /// T: [int, uint], [long, ulong]
  public static unsafe Vector<T> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<T> mask, Vector<T2> addresses); // LDFF1H

  /// T: uint, ulong
  public static unsafe Vector<T> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<T> mask, Vector<T> addresses); // LDFF1H

  /// T: int, uint, long, ulong
  public static unsafe Vector<T> GatherVectorUInt16WithByteOffsetZeroExtendFirstFaulting(Vector<T> mask, ushort* address, Vector<T> offsets); // LDFF1H

  /// T: [uint, int], [int, uint], [ulong, long], [long, ulong]
  public static unsafe Vector<T> GatherVectorUInt16WithByteOffsetZeroExtendFirstFaulting(Vector<T> mask, ushort* address, Vector<T2> offsets); // LDFF1H

  /// T: int, uint, long, ulong
  public static unsafe Vector<T> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<T> mask, ushort* address, Vector<T> indices); // LDFF1H

  /// T: [uint, int], [int, uint], [ulong, long], [long, ulong]
  public static unsafe Vector<T> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<T> mask, ushort* address, Vector<T2> indices); // LDFF1H

  public static unsafe Vector<long> GatherVectorInt32SignExtendFirstFaulting(Vector<long> mask, Vector<ulong> addresses); // LDFF1SW

  public static unsafe Vector<ulong> GatherVectorInt32SignExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> addresses); // LDFF1SW

  /// T: long, ulong
  public static unsafe Vector<T> GatherVectorInt32WithByteOffsetSignExtendFirstFaulting(Vector<T> mask, int* address, Vector<T> offsets); // LDFF1SW

  /// T: [ulong, long], [long, ulong]
  public static unsafe Vector<T> GatherVectorInt32WithByteOffsetSignExtendFirstFaulting(Vector<T> mask, int* address, Vector<T2> offsets); // LDFF1SW

  /// T: long, ulong
  public static unsafe Vector<T> GatherVectorInt32SignExtendFirstFaulting(Vector<T> mask, int* address, Vector<T> indices); // LDFF1SW

  /// T: [ulong, long], [long, ulong]
  public static unsafe Vector<T> GatherVectorInt32SignExtendFirstFaulting(Vector<T> mask, int* address, Vector<T2> indices); // LDFF1SW

  public static unsafe Vector<long> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<long> mask, Vector<ulong> addresses); // LDFF1W

  public static unsafe Vector<ulong> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> addresses); // LDFF1W

  /// T: long, ulong
  public static unsafe Vector<T> GatherVectorUInt32WithByteOffsetZeroExtendFirstFaulting(Vector<T> mask, uint* address, Vector<T> offsets); // LDFF1W

  /// T: [ulong, long], [long, ulong]
  public static unsafe Vector<T> GatherVectorUInt32WithByteOffsetZeroExtendFirstFaulting(Vector<T> mask, uint* address, Vector<T2> offsets); // LDFF1W

  /// T: long, ulong
  public static unsafe Vector<T> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<T> mask, uint* address, Vector<T> indices); // LDFF1W

  /// T: [ulong, long], [long, ulong]
  public static unsafe Vector<T> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<T> mask, uint* address, Vector<T2> indices); // LDFF1W

  /// T: [int, uint], [long, ulong]
  public static unsafe Vector<T> GatherVectorSByteSignExtendFirstFaulting(Vector<T> mask, Vector<T2> addresses); // LDFF1SB

  /// T: uint, ulong
  public static unsafe Vector<T> GatherVectorSByteSignExtendFirstFaulting(Vector<T> mask, Vector<T> addresses); // LDFF1SB

  /// T: int, uint, long, ulong
  public static unsafe Vector<T> GatherVectorSByteSignExtendFirstFaulting(Vector<T> mask, sbyte* address, Vector<T> offsets); // LDFF1SB

  /// T: [uint, int], [int, uint], [ulong, long], [long, ulong]
  public static unsafe Vector<T> GatherVectorSByteSignExtendFirstFaulting(Vector<T> mask, sbyte* address, Vector<T2> offsets); // LDFF1SB

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> GetFfr(); // RDFFR // predicated

  /// T: short, int, long, ushort, uint, ulong
  public static unsafe Vector<T> LoadVectorByteZeroExtendFirstFaulting(byte* address); // LDFF1B // predicated

  /// T: float, double, sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe Vector<T> LoadVectorFirstFaulting(T* address); // LDFF1W or LDFF1D or LDFF1B or LDFF1H // predicated

  /// T: int, long, uint, ulong
  public static unsafe Vector<T> LoadVectorInt16SignExtendFirstFaulting(short* address); // LDFF1SH // predicated

  /// T: int, long, uint, ulong
  public static unsafe Vector<T> LoadVectorUInt16ZeroExtendFirstFaulting(ushort* address); // LDFF1H // predicated

  /// T: long, ulong
  public static unsafe Vector<T> LoadVectorInt32SignExtendFirstFaulting(int* address); // LDFF1SW // predicated

  /// T: long, ulong
  public static unsafe Vector<T> LoadVectorUInt32ZeroExtendFirstFaulting(uint* address); // LDFF1W // predicated

  /// T: short, int, long, ushort, uint, ulong
  public static unsafe Vector<T> LoadVectorSByteSignExtendFirstFaulting(sbyte* address); // LDFF1SB // predicated

  /// T: sbyte, short, int, long, byte, ushort, uint, ulong
  public static unsafe void SetFfr(Vector<T> value); // WRFFR

  /// total method signatures: 72

}
